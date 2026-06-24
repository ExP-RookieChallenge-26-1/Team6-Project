# VFX 개조 1단계 구현 계획 — 데이터 모델 + 통합 플레이어

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 스킬 VFX를 "에디터 저작 프리팹 = 룩의 단일 소스 + 얇은 배치 데이터"로 재생하는 백본을 도입하되, 데이터가 비면 기존 절차 경로로 폴백해 현재 화면을 깨지 않는다.

**Architecture:** 스킬당 단일 `SkillVfxDefinition`(큐 리스트). 신규 `SkillVfxPlayer`가 트리거에 맞는 큐를 골라 placement(앵커 Player/Enemy × Feet/Body/Head + offset)에 프리팹을 스폰·재생·파괴한다. 프리팹에 `CombatProjectileEffect`가 있으면 자동 발사. `vfxDefinition`이 비면 `CombatWorldSpriteView`의 기존 경로로 폴백.

**Tech Stack:** Unity (C#), Unity Test Framework (EditMode), MCP for Unity (`run_tests`).

**선결 — 브랜치:** 현재 detached HEAD. 작업 전 `git switch -c feature/vfx-restructure-phase1` 로 브랜치를 만든다. 각 Task의 commit 스텝은 이 브랜치 기준이다. 테스트 실행은 셸이 아니라 Unity MCP `run_tests(mode="EditMode", test_names=[...])` 로 한다.

---

### Task 1: VFX 데이터 모델 타입

**Files:**
- Create: `Assets/Scripts/Skills/SkillVfxEnums.cs`
- Create: `Assets/Scripts/Skills/SkillVfxPlacement.cs`
- Create: `Assets/Scripts/Skills/SkillVfxCue.cs`
- Create: `Assets/Scripts/Skills/SkillVfxDefinition.cs`

- [ ] **Step 1: 열거형 작성** — `SkillVfxEnums.cs`

```csharp
namespace Project2048.Skills
{
    public enum SkillVfxTrigger { Activate, ChargeStart, ChargeRelease, Impact }

    public enum SkillVfxTarget { Player, Enemy }

    public enum SkillVfxVertical { Feet, Body, Head }
}
```

- [ ] **Step 2: placement 구조체 작성** — `SkillVfxPlacement.cs`

```csharp
using UnityEngine;

namespace Project2048.Skills
{
    [System.Serializable]
    public struct SkillVfxPlacement
    {
        public SkillVfxTarget target;
        public SkillVfxVertical vertical;
        public Vector3 localOffset;
    }
}
```

- [ ] **Step 3: 큐 클래스 작성** — `SkillVfxCue.cs`

```csharp
using UnityEngine;

namespace Project2048.Skills
{
    [System.Serializable]
    public sealed class SkillVfxCue
    {
        public SkillVfxTrigger trigger;
        public GameObject prefab;
        public SkillVfxPlacement placement;
        [Min(0f)] public float delaySeconds;
        [Min(0f)] public float scale = 1f;          // 1 = 프리팹 그대로
        public Color tint = Color.clear;            // clear = 프리팹 그대로
        public float lifetimeOverride = -1f;        // <=0 = 프리팹/자체 수명

        public bool HasPrefab => prefab != null;
    }
}
```

- [ ] **Step 4: 정의 클래스 작성** — `SkillVfxDefinition.cs`

```csharp
using System.Linq;
using UnityEngine;

namespace Project2048.Skills
{
    [System.Serializable]
    public sealed class SkillVfxDefinition
    {
        public SkillVfxCue[] cues = System.Array.Empty<SkillVfxCue>();

        public bool HasAnyCue => cues != null && cues.Any(c => c != null && c.HasPrefab);

        public System.Collections.Generic.IEnumerable<SkillVfxCue> CuesFor(SkillVfxTrigger trigger)
        {
            if (cues == null) yield break;
            foreach (var cue in cues)
            {
                if (cue != null && cue.HasPrefab && cue.trigger == trigger)
                {
                    yield return cue;
                }
            }
        }
    }
}
```

- [ ] **Step 5: 컴파일 확인** — Unity MCP `refresh_unity(compile="request")` 후 `read_console(types=["error"])` 가 0건인지 확인.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Skills/SkillVfxEnums.cs Assets/Scripts/Skills/SkillVfxPlacement.cs Assets/Scripts/Skills/SkillVfxCue.cs Assets/Scripts/Skills/SkillVfxDefinition.cs
git commit -m "feat(vfx): add SkillVfxDefinition data model (cues/placement/triggers)"
```

---

### Task 2: placement 해석 (단일 함수)

**Files:**
- Create: `Assets/Scripts/Presentation/SkillVfxPlayer.cs`
- Test: `Assets/Tests/EditMode/SkillVfxPlayerTests.cs`

- [ ] **Step 1: 실패 테스트 작성** — `SkillVfxPlayerTests.cs`

```csharp
using NUnit.Framework;
using Project2048.Presentation;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Tests
{
    public class SkillVfxPlayerTests
    {
        private static SpriteRenderer MakeUnitSprite(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(8, 8);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f); // 1 unit, center pivot
            return sr;
        }

        [Test]
        public void ResolvePlacement_EnemyFeet_IsBelowEnemyCenter()
        {
            var player = MakeUnitSprite("P", new Vector3(-1, 0, 0));
            var enemy = MakeUnitSprite("E", new Vector3(1, 0, 0));
            var ctx = new SkillVfxContext(player.transform, enemy.transform, SkillVfxTrigger.ChargeRelease);
            var placement = new SkillVfxPlacement { target = SkillVfxTarget.Enemy, vertical = SkillVfxVertical.Feet };

            var pos = SkillVfxPlayer.ResolvePlacementWorldPosition(placement, ctx);

            Assert.That(pos.y, Is.LessThan(enemy.bounds.center.y - 0.1f)); // 발 = 중앙보다 아래
            Assert.That(pos.x, Is.EqualTo(1f).Within(0.001f));

            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(enemy.gameObject);
        }
    }
}
```

- [ ] **Step 2: 실패 확인** — Unity MCP `run_tests(mode="EditMode", test_names=["Project2048.Tests.SkillVfxPlayerTests.ResolvePlacement_EnemyFeet_IsBelowEnemyCenter"])`. Expected: FAIL (SkillVfxPlayer/SkillVfxContext 미정의로 컴파일 실패).

- [ ] **Step 3: 최소 구현** — `SkillVfxPlayer.cs`

```csharp
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Presentation
{
    public readonly struct SkillVfxContext
    {
        public readonly Transform playerAnchor;
        public readonly Transform enemyAnchor;
        public readonly SkillVfxTrigger trigger;

        public SkillVfxContext(Transform playerAnchor, Transform enemyAnchor, SkillVfxTrigger trigger)
        {
            this.playerAnchor = playerAnchor;
            this.enemyAnchor = enemyAnchor;
            this.trigger = trigger;
        }

        public Transform AnchorFor(SkillVfxTarget target) =>
            target == SkillVfxTarget.Enemy ? enemyAnchor : playerAnchor;

        public Transform OppositeAnchorFor(SkillVfxTarget target) =>
            target == SkillVfxTarget.Enemy ? playerAnchor : enemyAnchor;
    }

    public static class SkillVfxPlayer
    {
        public static Vector3 ResolvePlacementWorldPosition(SkillVfxPlacement placement, SkillVfxContext ctx)
        {
            var anchor = ctx.AnchorFor(placement.target);
            if (anchor == null)
            {
                return placement.localOffset;
            }

            var basePos = ResolveVerticalWorldPosition(anchor, placement.vertical);
            return basePos + anchor.TransformVector(placement.localOffset);
        }

        private static Vector3 ResolveVerticalWorldPosition(Transform anchor, SkillVfxVertical vertical)
        {
            var renderer = anchor.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
            {
                return anchor.position;
            }

            var b = renderer.bounds;
            var y = vertical switch
            {
                SkillVfxVertical.Feet => b.min.y,
                SkillVfxVertical.Head => b.max.y,
                _ => b.center.y,
            };
            return new Vector3(b.center.x, y, b.center.z);
        }
    }
}
```

- [ ] **Step 4: 통과 확인** — 같은 `run_tests` 호출. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Presentation/SkillVfxPlayer.cs Assets/Tests/EditMode/SkillVfxPlayerTests.cs
git commit -m "feat(vfx): add SkillVfxPlayer placement resolution (Player/Enemy x Feet/Body/Head)"
```

---

### Task 3: 큐 재생 (스폰·프로젝타일·오버라이드·수명)

**Files:**
- Modify: `Assets/Scripts/Presentation/SkillVfxPlayer.cs`
- Test: `Assets/Tests/EditMode/SkillVfxPlayerTests.cs`

- [ ] **Step 1: 실패 테스트 추가** — `SkillVfxPlayerTests.cs` 에 추가

```csharp
        [Test]
        public void Play_StaticCue_SpawnsPrefabAtPlacement()
        {
            var player = MakeUnitSprite("P", new Vector3(-1, 0, 0));
            var enemy = MakeUnitSprite("E", new Vector3(1, 0, 0));
            var prefab = new GameObject("BeamPrefab");
            var def = new SkillVfxDefinition
            {
                cues = new[]
                {
                    new SkillVfxCue
                    {
                        trigger = SkillVfxTrigger.ChargeRelease,
                        prefab = prefab,
                        placement = new SkillVfxPlacement { target = SkillVfxTarget.Enemy, vertical = SkillVfxVertical.Feet },
                    },
                },
            };
            var ctx = new SkillVfxContext(player.transform, enemy.transform, SkillVfxTrigger.ChargeRelease);

            var spawned = SkillVfxPlayer.Play(def, ctx, parent: null, isPlaying: false);

            Assert.That(spawned.Count, Is.EqualTo(1));
            Assert.That(spawned[0].transform.position.y, Is.LessThan(enemy.bounds.center.y - 0.1f));

            foreach (var go in spawned) Object.DestroyImmediate(go);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(enemy.gameObject);
        }

        [Test]
        public void Play_WrongTrigger_SpawnsNothing()
        {
            var player = MakeUnitSprite("P", Vector3.zero);
            var enemy = MakeUnitSprite("E", new Vector3(1, 0, 0));
            var prefab = new GameObject("Fx");
            var def = new SkillVfxDefinition
            {
                cues = new[] { new SkillVfxCue { trigger = SkillVfxTrigger.Activate, prefab = prefab } },
            };
            var ctx = new SkillVfxContext(player.transform, enemy.transform, SkillVfxTrigger.ChargeRelease);

            var spawned = SkillVfxPlayer.Play(def, ctx, parent: null, isPlaying: false);

            Assert.That(spawned.Count, Is.EqualTo(0));

            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(enemy.gameObject);
        }
```

- [ ] **Step 2: 실패 확인** — `run_tests(test_names=["Project2048.Tests.SkillVfxPlayerTests.Play_StaticCue_SpawnsPrefabAtPlacement","Project2048.Tests.SkillVfxPlayerTests.Play_WrongTrigger_SpawnsNothing"])`. Expected: FAIL (`Play` 미정의).

- [ ] **Step 3: `Play` 구현** — `SkillVfxPlayer.cs` 에 추가

```csharp
        public static System.Collections.Generic.List<GameObject> Play(
            SkillVfxDefinition definition,
            SkillVfxContext ctx,
            Transform parent,
            bool isPlaying)
        {
            var spawned = new System.Collections.Generic.List<GameObject>();
            if (definition == null) return spawned;

            foreach (var cue in definition.CuesFor(ctx.trigger))
            {
                // 에디트 모드/테스트: delay 무시 동기 스폰. 플레이 모드 지연은 호출부(뷰)가 코루틴으로 처리.
                var go = SpawnCue(cue, ctx, parent, isPlaying);
                if (go != null) spawned.Add(go);
            }

            return spawned;
        }

        private static GameObject SpawnCue(SkillVfxCue cue, SkillVfxContext ctx, Transform parent, bool isPlaying)
        {
            if (cue == null || !cue.HasPrefab) return null;

            var pos = ResolvePlacementWorldPosition(cue.placement, ctx);
            var instance = Object.Instantiate(cue.prefab, pos, Quaternion.identity, parent);
            instance.name = cue.prefab.name;

            if (cue.scale > 0f && !Mathf.Approximately(cue.scale, 1f))
            {
                instance.transform.localScale *= cue.scale;
            }

            var projectile = instance.GetComponentInChildren<CombatProjectileEffect>(true);
            if (projectile != null)
            {
                var targetAnchor = ctx.OppositeAnchorFor(cue.placement.target);
                projectile.LaunchFromWorldPosition(pos, targetAnchor, Vector3.zero);
            }

            var lifetime = cue.lifetimeOverride > 0f ? cue.lifetimeOverride : 0f;
            if (lifetime > 0f && isPlaying)
            {
                Object.Destroy(instance, lifetime);
            }

            return instance;
        }
```

- [ ] **Step 4: 통과 확인** — 같은 `run_tests` 두 테스트 PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Presentation/SkillVfxPlayer.cs Assets/Tests/EditMode/SkillVfxPlayerTests.cs
git commit -m "feat(vfx): SkillVfxPlayer cue playback with projectile detection + overrides"
```

---

### Task 4: SkillSO에 vfxDefinition 추가

**Files:**
- Modify: `Assets/Scripts/Skills/SkillSO.cs` (Header "Skill VFX" 영역, 현재 69~78행 근처)

- [ ] **Step 1: 필드 추가** — `public SkillVfxTuning vfx = new();` 바로 위에 추가

```csharp
        [Header("Skill VFX (new — design-time prefab based)")]
        public SkillVfxDefinition vfxDefinition = new();
```

- [ ] **Step 2: 컴파일 확인** — `refresh_unity(compile="request")` → `read_console(types=["error"])` 0건.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Skills/SkillSO.cs
git commit -m "feat(vfx): add SkillSO.vfxDefinition field (kept alongside legacy vfx for fallback)"
```

---

### Task 5: CombatWorldSpriteView 통합 (Activate/ChargeStart/ChargeRelease + 폴백)

**Files:**
- Modify: `Assets/Scripts/Prototype/CombatWorldSpriteView.cs`
- Test: `Assets/Tests/EditMode/CombatPresentationEffectTests.cs`

> 통합 원칙: 각 트리거 지점에서 먼저 `skill.vfxDefinition`의 해당 트리거 큐가 있으면 `SkillVfxPlayer.Play`로 재생하고, 큐가 하나라도 재생됐으면 그 트리거의 기존 절차 경로를 **건너뛴다**. 큐가 없으면 기존 경로 그대로(폴백).

- [ ] **Step 1: 폴백 회귀 가드 테스트(실패 작성)** — `CombatPresentationEffectTests.cs` 에 추가. vfxDefinition이 빈 스킬은 기존 경로로 동작함을 고정.

```csharp
        [Test]
        public void CombatWorldSpriteView_EmptyVfxDefinition_FallsBackToProceduralPath()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var skill = CreateSkill("slash", SkillType.Attack, cost: 0, power: 10);
            skill.vfx = CreateOwnedVfxTuning(SkillVfxFamily.SlashArc);
            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            Assert.That(skill.vfxDefinition.HasAnyCue, Is.False); // 폴백 조건
            Assert.DoesNotThrow(() => view.PreviewSkillEffect(skill)); // 기존 경로로 안전 동작
        }
```

- [ ] **Step 2: 실패/현상 확인** — `run_tests(test_names=["Project2048.Tests.CombatPresentationEffectTests.CombatWorldSpriteView_EmptyVfxDefinition_FallsBackToProceduralPath"])`. Expected: 컴파일은 되나 의도 확인용. (이미 PASS면 폴백 진입점 추가 후에도 PASS 유지가 목표.)

- [ ] **Step 3: 통합 진입점 추가** — `PlaySkillPresentationEffect`(현재 352행) 최상단에 Activate 큐 우선 처리, 충전 분기에 ChargeStart/ChargeRelease 큐 우선 처리. 헬퍼 추가:

```csharp
        private SkillVfxContext BuildSkillVfxContext(SkillSO skill, SkillVfxTrigger trigger)
        {
            var playerAnchor = ResolvePlayerAnchor() ?? transform;
            var enemyAnchor = enemyRenderer != null ? enemyRenderer.transform : transform;
            return new SkillVfxContext(playerAnchor, enemyAnchor, trigger);
        }

        // true면 해당 트리거를 vfxDefinition이 처리했으니 기존 경로 skip
        private bool TryPlayDefinitionCues(SkillSO skill, SkillVfxTrigger trigger)
        {
            if (skill == null || skill.vfxDefinition == null || !skill.vfxDefinition.HasAnyCue)
            {
                return false;
            }

            var ctx = BuildSkillVfxContext(skill, trigger);
            var spawned = SkillVfxPlayer.Play(skill.vfxDefinition, ctx, transform, Application.isPlaying);
            return spawned.Count > 0;
        }
```

그리고 `PlaySkillPresentationEffect` 본문에서 비충전 경로 진입 직전:

```csharp
            if (!isChargeAttack && TryPlayDefinitionCues(skill, SkillVfxTrigger.Activate))
            {
                if (delayEnemyDeathFade) DelayEnemyDeathFadeForSkillEffect(skill, effect);
                return;
            }
```

충전 분기(`if (isChargeAttack)`) 안에서 기존 `PlayChargeAttackStartEffect` 호출을 큐 우선으로 감싼다:

```csharp
                if (!TryPlayDefinitionCues(skill, SkillVfxTrigger.ChargeStart))
                {
                    PlayChargeAttackStartEffect(skill, effect, sourceAnchor);
                }
```

그리고 `HandlePlayerChargedAttackReleased`(현재 705행)에서 release 큐 우선:

```csharp
            if (!TryPlayDefinitionCues(skill, SkillVfxTrigger.ChargeRelease))
            {
                PlayGatherLightReleasedAttackEffect(skill, targetTransform, playAttackAnimation: true);
            }
```
(여기서 `skill = ResolvePlayerChargedLightSkill(skillName)` 를 먼저 변수로 받아 재사용.)

- [ ] **Step 4: 컴파일 + 전체 회귀 확인** — `refresh_unity(compile="request")` → `read_console(types=["error"])` 0건. 그 다음 `run_tests(test_names=["Project2048.Tests.CombatPresentationEffectTests"])` 실행, **1단계 시작 전 baseline 대비 신규 실패 0** 확인 (기존 pre-existing 실패 제외).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Prototype/CombatWorldSpriteView.cs Assets/Tests/EditMode/CombatPresentationEffectTests.cs
git commit -m "feat(vfx): route skills through SkillVfxDefinition with fallback to procedural path"
```

---

### Task 6: 빛 모으기를 데이터로 마이그레이션 (검증용 첫 사례)

**Files:**
- Modify: `Assets/Data/Skills/GatherLight.asset` (Unity 인스펙터에서 `vfxDefinition.cues` 3개 작성)
- Test: `Assets/Tests/EditMode/CombatPresentationEffectTests.cs`

> 이 Task는 새 모델이 실제 스킬을 코드 변경 없이 표현할 수 있음을 증명한다. GatherLight의 3큐를 인스펙터로 채우고, 채운 뒤에는 기존 절차 경로 대신 큐 경로로 동작함을 테스트로 고정한다.

- [ ] **Step 1: 인스펙터로 큐 작성** — GatherLight.asset `vfxDefinition.cues`:
  - cue0: trigger=ChargeStart, prefab=버프 파티클 프리팹, placement=Player/Body
  - cue1: trigger=ChargeRelease, prefab=HolyFireball_Attack3, placement=Player/Body
  - cue2: trigger=ChargeRelease, prefab=SkillVfx_GatherLightVerticalBeam, placement=Enemy/Feet, delaySeconds=0.55

- [ ] **Step 2: 큐 경로 동작 테스트(작성)** — 채운 정의로 release 시 버티컬 빔이 적 발 근처에 스폰됨을 확인하는 EditMode 테스트 추가(스프라이트 픽스처 사용, placement 검증은 Task 2와 동일 패턴).

- [ ] **Step 3: 실행/통과 확인** — `run_tests(test_names=[해당 테스트])` PASS.

- [ ] **Step 4: Commit**

```bash
git add Assets/Data/Skills/GatherLight.asset Assets/Tests/EditMode/CombatPresentationEffectTests.cs
git commit -m "feat(vfx): migrate GatherLight to data-driven vfxDefinition cues"
```

---

## 자가검토 메모

- **스펙 커버리지:** §1 데이터모델→Task1, §2 플레이어/해석→Task2·3, §3 통합/마이그레이션→Task4·5·6, §4 에러처리→Task2·3의 degrade 경로(앵커 없음/프로젝타일 타깃 없음), §5 테스트→각 Task의 EditMode 테스트. 1회성 에디터 마이그레이션 "메뉴 유틸"은 1단계에선 GatherLight 수동 마이그레이션(Task6)으로 대체, 일괄 메뉴화는 3단계로 미룸.
- **타입 일관성:** `SkillVfxPlayer.Play(definition, ctx, parent, isPlaying)`, `ResolvePlacementWorldPosition(placement, ctx)`, `SkillVfxContext(playerAnchor, enemyAnchor, trigger)` 시그니처가 Task 전반에서 일치.
- **플레이스홀더:** 없음(코드 스텝은 전부 실제 코드). Task6 Step2 테스트 본문은 Task2 패턴 재사용으로 명시.
