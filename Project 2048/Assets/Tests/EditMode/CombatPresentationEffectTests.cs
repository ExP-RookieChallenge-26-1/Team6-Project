using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Project2048.Board2048;
using Project2048.Enemy;
using Project2048.Presentation;
using Project2048.Prototype;
using UnityEngine;

namespace Project2048.Tests
{
    public class CombatPresentationEffectTests
    {
        private readonly List<Object> ownedObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var ownedObject in ownedObjects)
            {
                if (ownedObject != null)
                {
                    Object.DestroyImmediate(ownedObject);
                }
            }

            ownedObjects.Clear();
        }

        private GameObject CreateOwnedGameObject(string name)
        {
            var gameObject = new GameObject(name);
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        [Test]
        public void EnemySo_ResolvesActionEffectByActionId()
        {
            var enemy = ScriptableObject.CreateInstance<EnemySO>();
            var attackEffect = new CombatEffectBinding
            {
                volumeScale = -2f,
            };
            enemy.actionEffects = new List<CombatantActionEffectBinding>
            {
                new()
                {
                    actionId = CombatActionIds.Attack,
                    effect = attackEffect,
                },
            };
            ownedObjects.Add(enemy);

            Assert.That(enemy.FindActionEffect(CombatActionIds.Attack), Is.SameAs(attackEffect));
            Assert.That(enemy.FindActionEffect(" missing "), Is.Null);
            Assert.That(attackEffect.EffectiveVolumeScale, Is.Zero);
        }

        [Test]
        public void BoardTileEffectProfile_UsesSpecificMergeEffectBeforeFallback()
        {
            var profile = ScriptableObject.CreateInstance<BoardTileEffectProfileSO>();
            var fallbackPrefab = CreateOwnedGameObject("DefaultMergeVfx");
            var merge2048Prefab = CreateOwnedGameObject("Merge2048Vfx");
            var fallback = new CombatEffectBinding
            {
                vfxPrefab = fallbackPrefab,
            };
            var merge2048 = new CombatEffectBinding
            {
                vfxPrefab = merge2048Prefab,
            };
            profile.defaultMergeEffect = fallback;
            profile.mergeEffects = new List<BoardTileMergeEffectBinding>
            {
                new()
                {
                    tileValue = 2048,
                    effect = merge2048,
                },
            };
            ownedObjects.Add(profile);

            Assert.That(profile.ResolveMergeEffect(2048), Is.SameAs(merge2048));
            Assert.That(profile.ResolveMergeEffect(128), Is.SameAs(fallback));
        }

        [Test]
        public void AudioRouter_BuildsTileEffectCuesForEachMoveAndEachMergedResult()
        {
            var router = new PrototypeCombatAudioRouter();
            var transition = new BoardTransition();
            transition.Movements.Add(new BoardTileMovement
            {
                Value = 2,
                From = new Vector2Int(0, 0),
                To = new Vector2Int(0, 0),
                IsMergeParticipant = true,
                ResultValue = 4,
            });
            transition.Movements.Add(new BoardTileMovement
            {
                Value = 2,
                From = new Vector2Int(1, 0),
                To = new Vector2Int(0, 0),
                IsMergeParticipant = true,
                ResultValue = 4,
            });

            var cues = router.GetBoardTileEffectCues(transition);

            Assert.That(cues.Count(cue => cue.CueType == BoardTileEffectCueType.Move), Is.EqualTo(2));
            Assert.That(cues.Count(cue => cue.CueType == BoardTileEffectCueType.Merge), Is.EqualTo(1));

            var mergeCue = cues.Single(cue => cue.CueType == BoardTileEffectCueType.Merge);
            Assert.That(mergeCue.TileValue, Is.EqualTo(4));
            Assert.That(mergeCue.Position, Is.EqualTo(new Vector2Int(0, 0)));
        }
    }
}
