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
                        placement = new SkillVfxPlacement { target = SkillVfxTarget.Player, vertical = SkillVfxVertical.Body },
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
                        placement = new SkillVfxPlacement { target = SkillVfxTarget.Player, vertical = SkillVfxVertical.Body },
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
    }
}
