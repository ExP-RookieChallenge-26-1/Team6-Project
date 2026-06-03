# Combat 모듈 리팩토링 Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development 또는 superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax.

**Goal:** 전투 모듈의 거대 파일(CombatWorldSpriteView 6179줄, CombatUiView 3008줄, CombatManager 1121줄) 3개를 책임 단위로 분리. 시각적/행동 결과 무변경.

**Architecture:**
- Unity의 `partial class`로 분리(같은 클래스, 다른 파일) → `.meta` GUID 변경 없음, SerializeField 참조 보존, private 멤버 접근 가능.
- 디자인 기반 UI 원칙: Theme 색상 상수, RectTransform 레이아웃, Inspector 참조 절대 변경 금지.
- 각 task 후 Unity Editor에서 컴파일 검증 + Console 에러 0 확인.

**Tech Stack:** C# 9.0+, Unity 2022/2023, UGUI, TextMeshPro, Particle System, VisualEffectGraph.

**브랜치/안전망:** `refactor/combat-modules` 브랜치 생성, partial 분리는 task 단위 commit. 각 task는 5~30분 내 검증 가능 단위.

---

## Phase 1: CombatWorldSpriteView partial 분리 (6179줄 → 카테고리별)

### Task 1.1: refactor 브랜치 생성 + 베이스라인 컴파일 확인

**Files:**
- 변경 없음 (검증만)

- [ ] **Step 1: 브랜치 생성**
  ```bash
  git checkout -b refactor/combat-modules
  ```
- [ ] **Step 2: Unity 컴파일 상태 확인**
  - Unity MCP로 `mcpforunity://editor/state` 읽기 → `is_compiling: false`, 에러 0 확인
- [ ] **Step 3: 베이스라인 console snapshot 저장 (커밋 메시지에 첨부)**

### Task 1.2: CombatWorldSpriteView를 partial로 선언 (구조 변경 없음)

**Files:**
- Modify: `Assets/Scripts/Prototype/CombatWorldSpriteView.cs:17` → `public partial class CombatWorldSpriteView : MonoBehaviour`

- [ ] **Step 1:** `public class CombatWorldSpriteView` → `public partial class CombatWorldSpriteView`
- [ ] **Step 2: 컴파일 검증** — Unity Console 에러 0
- [ ] **Step 3: Commit**
  ```bash
  git commit -am "refactor(combat-view): mark CombatWorldSpriteView as partial"
  ```

### Task 1.3: Damage Number 영역 분리 (~280줄)

**Files:**
- Create: `Assets/Scripts/Prototype/CombatWorldSpriteView.DamageNumber.cs`
- Modify: `Assets/Scripts/Prototype/CombatWorldSpriteView.cs` (옮긴 메서드 제거)

**옮길 멤버:**
- `damageNumberPopupLayer`, `damageNumberPopups` (필드)
- `PlayDamageNumberPopupIfNeeded` (3333)
- `PlayDamageNumberUiPopup` (3350)
- `PlayDamageNumberWorldPopup` (3375)
- `ResolveDamageNumberPopupLayer` (3523)
- `DamageNumberPopupRoutine` (3557)
- `ClearDamageNumberPopups` (3595)
- 관련 const `DamageNumberPopup*` 묶음 (50–61)

- [ ] **Step 1: 새 파일 생성, partial 동일 namespace/class 선언**
- [ ] **Step 2: 메서드/필드/상수를 cut & paste (한 묶음씩)**
- [ ] **Step 3: 원본 파일에서 같은 부분 삭제**
- [ ] **Step 4: 컴파일 검증 (Unity Console)**
- [ ] **Step 5: Commit**

### Task 1.4: Enemy Effects 영역 분리 (~600줄)

**옮길 멤버:**
- `enemyDeathFadeCoroutine`, `enemyDeathFadeDelayCoroutine`, `enemyAppearIntroCoroutine`, `enemyAttackLungeCoroutine`, `enemyRendererRestLocal*`, `hasEnemyRendererRestTransform`, `lastEnemyWasDead`, `delayEnemyDeathFadeUntilRealtime`
- `PlayEnemyAppearIntro`, `PlayEnemyAttackLunge`, `PlayEnemyAttackImpactEffects`, `PlayEnemyClawSlashEffect`, `ResolveEnemyAttackDirectionSign`
- `EnemyAppearIntroRoutine`, `EnemyAttackLungeRoutine`
- `PlayEnemyDeathFadeIfNeeded`, `PlayEnemyDeathFade`, `EnemyDeathFadeDelayRoutine`, `EnemyDeathFadeRoutine`
- `ClearEnemyDeathFade`, `ClearEnemyAppearIntro`, `ClearEnemyAttackLunge`
- `PlayEnemyAppearWorldShake`, `ResolveForegroundShakeRoot`, `ReparentRendererForWorldShake`, `ClearWorldShake`, `CanAutoReparentForWorldShake`, `IsUsableShakeTarget`
- `ResolveEnemyAttackLungeTarget`, `CacheEnemyRendererRestTransform`, `RestoreEnemyRendererTransform`, `SetEnemyRendererAlpha`

**Files:**
- Create: `Assets/Scripts/Prototype/CombatWorldSpriteView.Enemy.cs`

- [ ] Step 1~5: 동일 패턴 (cut → paste → 삭제 → 컴파일 → commit)

### Task 1.5: Shield/ThornGuard 효과 분리 (~700줄)

**옮길 멤버:**
- `activePlayerShieldArtVfx`, `activePlayerThornGuardVfx`, runtime shield 재료/asset 캐시
- `PlayShieldImpactEffectIfNeeded`, `PlayShieldImpactArtPulse`
- `PlayShieldCircleSkillParticleEffect`, `PlayThornGuardCircleSkillParticleEffect`
- `PlayShieldAttackSkillEffect`, `PlayShieldBashSkillEffect`, `PlayShieldBurstSkillEffect`
- `PlayShieldAttackImpactAfterDelayRoutine`, `SpawnShieldAttackImpact`
- `CreatePlayerShieldArtVfxRoot`, `CreateThornGuardShieldVfxRoot`
- `SpawnShieldVfxGraphLayer`, `SpawnShieldCircleLine`
- `ResolveShieldImpactParticleMaterial`, `ResolveShieldVfxGraphAsset`, `ResolveShieldEffectSprite`
- `UpdatePlayerShieldArtVfx`, `UpdatePlayerThornGuardVfx`, `PlayPlayerThornGuardHitPulseIfNeeded`
- `ClearActivePlayerShieldArtVfx`, `ClearActivePlayerThornGuardVfx`
- Shield 관련 상수 묶음

**Files:**
- Create: `Assets/Scripts/Prototype/CombatWorldSpriteView.Shield.cs`

- [ ] Step 1~5: 동일

### Task 1.6: 스킬 효과 분리 (~1500줄)

**옮길 멤버:**
- `PlayFlameBurstSkillParticleEffect`, `PlaySupportFireSkillParticleEffect`
- `PlaySpikedBurstSkillEffect`, `PlayBloodFountainSlashSkillEffect`
- `PlayDarkShackleSkillEffect`, `ConfigureDarkShackleLinkLine`, `SpawnDarkShackleImpactEffects`, `AnimateDarkShackleChainRoutine`
- `PlayLanternSkillLaunchCue`, `PlayChargedLightBeamEffect` (2개 오버로드), `SpawnChargedLightAttackArt`, `SpawnChargedLightBeamLine`, `ResolveChargedLightBeamMaterial`
- `PlayTentacleStrikeSkillEffect`, `ConfigureTentacleLine`, `ConfigureTentacleCupLine`
- 관련 const 묶음

**Files:**
- Create: `Assets/Scripts/Prototype/CombatWorldSpriteView.SkillEffects.cs`

- [ ] Step 1~5: 동일

### Task 1.7: Debuff 효과 분리 (~150줄)

**옮길 멤버:**
- `lastPlayedEnemyDebuffVfxSequence`
- `PlayEnemyDebuffCastEffectIfNeeded`, `PlayDebuffTargetEffectAfterCast`
- `SpawnDebuffTargetParticlesAfterDelay`, `SpawnDebuffCastParticles`
- `ResolveDebuffParticleColor`, `ResolveDebuffParticleLifetimeSeconds`, `ResolveDebuffParticleMaterial`

**Files:**
- Create: `Assets/Scripts/Prototype/CombatWorldSpriteView.Debuff.cs`

- [ ] Step 1~5: 동일

### Task 1.8: Particle/Line 공용 helper 분리 (~700줄)

**옮길 멤버:**
- `CreateWorldSkillLine`, `CreateLocalSkillLine`
- `SpawnParticleBurst` (2개 오버로드), `CreateFallbackParticleSystem`
- `FadeLineRendererRoutine`
- `ResolveRuntimeSkillParticleMaterial`, `DestroyRuntimeParticleMaterials`
- `PlayReusableSkillParticleEffect`, `PlayAttackArtForReusableSkill`, `ResolveAttackEffectSprite`
- 관련 const

**Files:**
- Create: `Assets/Scripts/Prototype/CombatWorldSpriteView.VfxHelpers.cs`

- [ ] Step 1~5: 동일

### Task 1.9: Audio 영역 분리 (~100줄)

**옮길 멤버:**
- `PlayCombatantActionAudioEffect`, `PlayCombatantActionAudioEffectAfterDelay`, `PlayCombatantActionAudioEffectNow`
- `EnsureAudioSource`, `ResolveAudioRouting`, `DuckBgmForImportantSfx`

**Files:**
- Create: `Assets/Scripts/Prototype/CombatWorldSpriteView.Audio.cs`

- [ ] Step 1~5: 동일

### Task 1.10: 런타임 검증 (Play mode test)

- [ ] **Step 1:** Unity Editor Play → Battle Scene 진입
- [ ] **Step 2:** 각 스킬 발동, 적 등장/사망, 디버프, 데미지 팝업 시각 확인
- [ ] **Step 3:** Console 에러/경고 0 확인
- [ ] **Step 4:** Final commit + Phase 1 PR

---

## Phase 2: CombatUiView 영역별 분리 (3008줄)

### Task 2.1: 베이스라인 캡처

- [ ] **Step 1:** Unity Play → 전투 진입, 각 패널 스크린샷 5장 저장 (top bar, board, action, enemy turn, result/reward)
- [ ] **Step 2:** `docs/refactor-baselines/combat-ui/`에 저장

### Task 2.2: partial 선언

- [ ] **Step 1:** `public class CombatUiView` → `public partial class CombatUiView`
- [ ] **Step 2:** 컴파일 검증 + commit

### Task 2.3~2.9: 영역별 분리 (각 task = 1 [Header] 영역)

각 task별로 다음을 생성:
- `CombatUiView.TopBar.cs`
- `CombatUiView.BattleScene.cs` (Player/Enemy portrait, HP, intent bubble, status effects, tooltip)
- `CombatUiView.BottomPanels.cs`
- `CombatUiView.BoardPanel.cs` (board cells, swipe, animation overlay)
- `CombatUiView.ActionPanel.cs` (cost, skill slots, help icon)
- `CombatUiView.EnemyTurnPanel.cs`
- `CombatUiView.ResultRewardOverlay.cs`
- `CombatUiView.Theme.cs` (Theme* 색상 상수 — UI 디자인 기반 원칙: 값 변경 금지)

각 task 절차:
- [ ] **Step 1:** [Header] 영역에 속하는 SerializeField, private state, 메서드를 새 partial 파일로 이동
- [ ] **Step 2:** 원본에서 제거
- [ ] **Step 3:** Unity Console 에러 0
- [ ] **Step 4:** Play mode에서 해당 패널 스크린샷 → 베이스라인과 픽셀 비교
- [ ] **Step 5:** Commit

### Task 2.10: 시각 회귀 최종 검증

- [ ] **Step 1:** Phase 2.1 베이스라인과 현재 스크린샷 비교 (디자인 변경 0)
- [ ] **Step 2:** Phase 2 PR

---

## Phase 3: CombatManager 책임 분리 (1121줄)

### Task 3.1: SnapshotBuilder 추출 (~150줄 예상)

`CombatManager.GetSnapshot()` 및 snapshot 조립 로직을 `CombatSnapshotBuilder` 일반 클래스로 추출 (CombatManager가 소유, public API 무변경).

**Files:**
- Create: `Assets/Scripts/Combat/CombatSnapshotBuilder.cs`
- Modify: `CombatManager.cs` (snapshot 조립 → builder 호출로 교체)

- [ ] **Step 1:** Builder 클래스 생성, 필드 주입 생성자
- [ ] **Step 2:** CombatManager에서 snapshot 만드는 코드 → builder.Build() 호출로 교체
- [ ] **Step 3:** Snapshot 비교 테스트(EditMode) — 베이스라인과 동일
- [ ] **Step 4:** Commit

### Task 3.2: VfxCueDispatcher 추출 (~100줄)

`lastVfxCue`, `vfxCueSequence`, VFX cue 발행 로직을 별도 클래스로 추출.

**Files:**
- Create: `Assets/Scripts/Combat/CombatVfxCueDispatcher.cs`

- [ ] Step 1~4 동일

### Task 3.3: ActionDescriptionLog 추출 (~30줄)

`lastActionDescription` 및 갱신 로직을 별도 클래스로 추출.

- [ ] Step 1~4 동일

### Task 3.4: Phase 3 검증

- [ ] **Step 1:** Play mode에서 전체 전투 1회 클리어 → 행동/스킬/적 인텐트 정상 동작
- [ ] **Step 2:** Console 에러 0
- [ ] **Step 3:** Phase 3 PR

---

## Self-Review

**Spec coverage:**
- 사용자가 선택한 "3개 다 순차적 진행" → Phase 1~3로 대응 ✓
- "UI 디자인 기반" → Phase 2에서 Theme 상수/RectTransform/Inspector 참조 보존 명시 ✓
- "코드 리팩토링" → partial 분리(시각/행동 무변경) ✓

**Placeholder scan:** "구현은 비슷하게" 등 단어 없음 ✓

**Type consistency:** partial 분리는 동일 클래스라 타입 일관성 자동 ✓

---

## 진행 노트

- `Project 2048/Assets/Editor/`의 menu/scriptable object editor가 CombatWorldSpriteView 내부 멤버를 reflection으로 참조하는지 사전 점검 필요(없으면 위험 0).
- 각 task 후 Unity Editor가 자동 재컴파일하므로 `is_compiling: false` 대기 필수.
