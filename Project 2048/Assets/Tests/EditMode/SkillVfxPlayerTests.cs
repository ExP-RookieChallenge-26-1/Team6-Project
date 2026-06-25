using NUnit.Framework;
using Project2048.Presentation;
using Project2048.Skills;
using UnityEngine;
using UnityEngine.VFX;

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
            var endpoint = new VfxEndpoint { actor = VfxActorRef.PrimaryTarget, socket = VfxSocket.Feet };

            var pos = SkillVfxPlayer.ResolveEndpointWorldPosition(endpoint, ctx);

            Assert.That(pos.y, Is.LessThan(enemy.bounds.center.y - 0.1f)); // feet below center
            Assert.That(pos.x, Is.EqualTo(1f).Within(0.001f));

            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(enemy.gameObject);
        }

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
                        spawnAt = new VfxEndpoint { actor = VfxActorRef.PrimaryTarget, socket = VfxSocket.Feet },
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

        [Test]
        public void Play_ParticleCue_AutoPlaysAndAppliesTint()
        {
            var player = MakeUnitSprite("P", Vector3.zero);
            var enemy = MakeUnitSprite("E", new Vector3(1, 0, 0));
            var prefab = new GameObject("ParticleFx");
            var ps = prefab.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.startColor = Color.white;
            var tint = new Color(1f, 0f, 0f, 1f);
            var def = new SkillVfxDefinition
            {
                cues = new[]
                {
                    new SkillVfxCue
                    {
                        trigger = SkillVfxTrigger.Activate,
                        prefab = prefab,
                        spawnAt = new VfxEndpoint { actor = VfxActorRef.Caster, socket = VfxSocket.Body },
                        tint = tint,
                    },
                },
            };
            var ctx = new SkillVfxContext(player.transform, enemy.transform, SkillVfxTrigger.Activate);

            var spawned = SkillVfxPlayer.Play(def, ctx, parent: null, isPlaying: false);

            Assert.That(spawned.Count, Is.EqualTo(1));
            var spawnedPs = spawned[0].GetComponent<ParticleSystem>();
            Assert.That(spawnedPs.main.startColor.color, Is.EqualTo(tint));      // tint applied
            Assert.That(spawnedPs.isPlaying, Is.True);                           // auto-played

            foreach (var go in spawned) Object.DestroyImmediate(go);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(enemy.gameObject);
        }

        [Test]
        public void Play_ClearTint_LeavesPrefabColor()
        {
            var player = MakeUnitSprite("P", Vector3.zero);
            var enemy = MakeUnitSprite("E", new Vector3(1, 0, 0));
            var prefab = new GameObject("ParticleFx2");
            var ps = prefab.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.startColor = Color.green;
            var def = new SkillVfxDefinition
            {
                cues = new[]
                {
                    new SkillVfxCue
                    {
                        trigger = SkillVfxTrigger.Activate,
                        prefab = prefab,
                        spawnAt = new VfxEndpoint { actor = VfxActorRef.Caster, socket = VfxSocket.Body },
                        tint = Color.clear,
                    },
                },
            };
            var ctx = new SkillVfxContext(player.transform, enemy.transform, SkillVfxTrigger.Activate);

            var spawned = SkillVfxPlayer.Play(def, ctx, parent: null, isPlaying: false);

            var spawnedPs = spawned[0].GetComponent<ParticleSystem>();
            Assert.That(spawnedPs.main.startColor.color, Is.EqualTo(Color.green)); // clear tint → unchanged

            foreach (var go in spawned) Object.DestroyImmediate(go);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(enemy.gameObject);
        }

        [Test]
        public void Play_ProjectileCueWithVisualEffect_SpawnsPrefab()
        {
            var player = MakeUnitSprite("P", Vector3.zero);
            var enemy = MakeUnitSprite("E", new Vector3(1, 0, 0));
            var prefab = new GameObject("VfxProjectile");
            prefab.AddComponent<VisualEffect>();
            prefab.AddComponent<CombatProjectileEffect>();
            var def = new SkillVfxDefinition
            {
                cues = new[]
                {
                    new SkillVfxCue
                    {
                        trigger = SkillVfxTrigger.Activate,
                        prefab = prefab,
                        spawnAt = new VfxEndpoint { actor = VfxActorRef.Caster, socket = VfxSocket.Body },
                    },
                },
            };
            var ctx = new SkillVfxContext(player.transform, enemy.transform, SkillVfxTrigger.Activate);

            var spawned = SkillVfxPlayer.Play(def, ctx, parent: null, isPlaying: false);

            Assert.That(spawned.Count, Is.EqualTo(1));
            Assert.That(spawned[0].GetComponent<VisualEffect>(), Is.Not.Null);
            Assert.That(spawned[0].GetComponent<CombatProjectileEffect>(), Is.Not.Null);

            foreach (var go in spawned) Object.DestroyImmediate(go);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(enemy.gameObject);
        }

        // 같은 SkillSO(정의)를 플레이어/적이 공유: 컨텍스트만 반전되면 스폰도 좌우로 미러링된다.
        [Test]
        public void SameDefinition_CasterSpawn_MirrorsWhenCasterAndTargetSwap()
        {
            var left = MakeUnitSprite("Left", new Vector3(-2, 0, 0));
            var right = MakeUnitSprite("Right", new Vector3(2, 0, 0));
            var prefab = new GameObject("CasterFx");
            var def = new SkillVfxDefinition
            {
                cues = new[]
                {
                    new SkillVfxCue
                    {
                        trigger = SkillVfxTrigger.Activate,
                        prefab = prefab,
                        spawnAt = new VfxEndpoint { actor = VfxActorRef.Caster, socket = VfxSocket.Body },
                    },
                },
            };

            // 플레이어 시전: caster=left, target=right → 스폰은 left 쪽.
            var asPlayer = SkillVfxPlayer.Play(
                def, new SkillVfxContext(left.transform, right.transform, SkillVfxTrigger.Activate), null, false);
            // 적 시전: caster=right, target=left → 데이터 동일, 컨텍스트만 반전 → 스폰은 right 쪽.
            var asEnemy = SkillVfxPlayer.Play(
                def, new SkillVfxContext(right.transform, left.transform, SkillVfxTrigger.Activate), null, false);

            Assert.That(asPlayer.Count, Is.EqualTo(1));
            Assert.That(asEnemy.Count, Is.EqualTo(1));
            Assert.That(asPlayer[0].transform.position.x, Is.EqualTo(left.bounds.center.x).Within(0.001f));
            Assert.That(asEnemy[0].transform.position.x, Is.EqualTo(right.bounds.center.x).Within(0.001f));

            foreach (var go in asPlayer) Object.DestroyImmediate(go);
            foreach (var go in asEnemy) Object.DestroyImmediate(go);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(left.gameObject);
            Object.DestroyImmediate(right.gameObject);
        }

        [Test]
        public void SkillVfxRunner_DelayedCue_SpawnsSynchronouslyInEditMode_AndReportsPlayed()
        {
            var caster = MakeUnitSprite("C", Vector3.zero);
            var targetUnit = MakeUnitSprite("T", new Vector3(2, 0, 0));
            var prefab = new GameObject("RunnerFx");
            var def = new SkillVfxDefinition
            {
                cues = new[]
                {
                    new SkillVfxCue
                    {
                        trigger = SkillVfxTrigger.Activate,
                        prefab = prefab,
                        spawnAt = new VfxEndpoint { actor = VfxActorRef.Caster, socket = VfxSocket.Body },
                        delaySeconds = 0.5f, // 에디트 모드면 지연 무시 → 즉시 스폰
                    },
                },
            };
            var runnerGo = new GameObject("Runner");
            var runner = runnerGo.AddComponent<SkillVfxRunner>();

            var played = runner.Play(
                def, new SkillVfxContext(caster.transform, targetUnit.transform, SkillVfxTrigger.Activate), runnerGo.transform);

            Assert.That(played, Is.True);
            Assert.That(runnerGo.transform.Find("RunnerFx"), Is.Not.Null);

            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(runnerGo);
            Object.DestroyImmediate(caster.gameObject);
            Object.DestroyImmediate(targetUnit.gameObject);
        }

        [Test]
        public void SkillVfxRunner_WrongTrigger_ReportsNotPlayed()
        {
            var caster = MakeUnitSprite("C", Vector3.zero);
            var prefab = new GameObject("RunnerFx2");
            var def = new SkillVfxDefinition
            {
                cues = new[] { new SkillVfxCue { trigger = SkillVfxTrigger.Activate, prefab = prefab } },
            };
            var runnerGo = new GameObject("Runner");
            var runner = runnerGo.AddComponent<SkillVfxRunner>();

            var played = runner.Play(
                def, new SkillVfxContext(caster.transform, null, SkillVfxTrigger.ChargeRelease), runnerGo.transform);

            Assert.That(played, Is.False);

            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(runnerGo);
            Object.DestroyImmediate(caster.gameObject);
        }

        // CastPoint = 랜턴 머즐. 비주얼 센터에서 타깃 쪽으로 facing 반영 → 적이 같은 스킬을 써도 자동 좌우 반전.
        [Test]
        public void CastPointSocket_FiresFromLanternMuzzle_FacingTarget()
        {
            var left = MakeUnitSprite("L", new Vector3(-2, 0, 0));
            var right = MakeUnitSprite("R", new Vector3(2, 0, 0));
            var endpoint = new VfxEndpoint { actor = VfxActorRef.Caster, socket = VfxSocket.CastPoint };

            // caster=left, target=right → 머즐이 오른쪽(+0.34)으로.
            var fromLeft = SkillVfxPlayer.ResolveEndpointWorldPosition(
                endpoint, new SkillVfxContext(left.transform, right.transform, SkillVfxTrigger.Activate));
            // caster=right, target=left → 같은 데이터, 머즐이 왼쪽(-0.34)으로 미러링.
            var fromRight = SkillVfxPlayer.ResolveEndpointWorldPosition(
                endpoint, new SkillVfxContext(right.transform, left.transform, SkillVfxTrigger.Activate));

            Assert.That(fromLeft.x, Is.EqualTo(left.bounds.center.x + 0.34f).Within(0.001f));
            Assert.That(fromLeft.y, Is.EqualTo(left.bounds.center.y + 0.36f).Within(0.001f));
            Assert.That(fromRight.x, Is.EqualTo(right.bounds.center.x - 0.34f).Within(0.001f));

            Object.DestroyImmediate(left.gameObject);
            Object.DestroyImmediate(right.gameObject);
        }

        [Test]
        public void AnchorProvider_CastPointTransform_OverridesMuzzleFallback()
        {
            var caster = MakeUnitSprite("C", new Vector3(-2, 0, 0));
            var target = MakeUnitSprite("T", new Vector3(2, 0, 0));
            var provider = caster.gameObject.AddComponent<CombatVfxAnchorProvider>();
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(caster.transform, false);
            muzzle.position = new Vector3(-1.5f, 0.5f, 0f);
            provider.castPoint = muzzle;

            var endpoint = new VfxEndpoint { actor = VfxActorRef.Caster, socket = VfxSocket.CastPoint };
            var pos = SkillVfxPlayer.ResolveEndpointWorldPosition(
                endpoint, new SkillVfxContext(caster.transform, target.transform, SkillVfxTrigger.Activate));

            // 명시 소켓 우선 → 머즐 폴백 대신 castPoint Transform 위치 사용.
            Assert.That(pos.x, Is.EqualTo(muzzle.position.x).Within(0.001f));
            Assert.That(pos.y, Is.EqualTo(muzzle.position.y).Within(0.001f));

            Object.DestroyImmediate(muzzle.gameObject);
            Object.DestroyImmediate(caster.gameObject);
            Object.DestroyImmediate(target.gameObject);
        }
    }
}
