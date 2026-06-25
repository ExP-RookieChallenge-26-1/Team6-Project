using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Project2048.Board2048;
using Project2048.Combat;
using Project2048.Core;
using Project2048.Enemy;
using Project2048.Flow;
using Project2048.Presentation;
using Project2048.Prototype;
using Project2048.Rewards;
using Project2048.Skills;
using Project2048.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace Project2048.Tests
{
    public class CombatPresentationEffectTests
    {
        private readonly List<Object> ownedObjects = new();

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

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

        private GameObject CreateOwnedRectTransformObject(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        private Sprite CreateOwnedSprite(string name)
        {
            return CreateOwnedSprite(name, new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateOwnedSprite(string name, Vector2 pivot)
        {
            var texture = new Texture2D(8, 8);
            texture.name = $"{name}Texture";
            ownedObjects.Add(texture);

            var pixels = Enumerable.Repeat(Color.white, 64).ToArray();
            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), pivot, 8f);
            sprite.name = name;
            ownedObjects.Add(sprite);
            return sprite;
        }

        private GameObject CreateOwnedSpritePrefab(string name, Sprite sprite, string markerName)
        {
            var prefab = CreateOwnedGameObject(name);
            var renderer = prefab.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            var marker = new GameObject(markerName);
            marker.transform.SetParent(prefab.transform, false);
            return prefab;
        }

        private GameObject CreateOwnedShieldPrefab(string name, Sprite sprite, string sparkleName)
        {
            var prefab = CreateOwnedGameObject(name);
            var renderer = prefab.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            var sparkles = new GameObject(sparkleName, typeof(ParticleSystem));
            sparkles.transform.SetParent(prefab.transform, false);
            sparkles.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            var particles = sparkles.GetComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = true;
            main.loop = true;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.052f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.64f;
            shape.radiusThickness = 0.18f;

            return prefab;
        }

        private static SkillVfxTuning CreateOwnedVfxTuning(SkillVfxFamily family)
        {
            return new SkillVfxTuning { family = family };
        }

        private static void AssertPopupIsNearButNotCentered(
            RectTransform popup,
            RectTransform popupLayer,
            Camera camera,
            Vector3 targetWorldCenter)
        {
            var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, targetWorldCenter);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                popupLayer,
                screenPoint,
                null,
                out var centerPoint);

            var distanceFromCenter = Vector2.Distance(popup.anchoredPosition, centerPoint);
            var offset = popup.anchoredPosition - centerPoint;
            Assert.That(Mathf.Abs(offset.x), Is.GreaterThan(8f));
            Assert.That(Mathf.Abs(offset.y), Is.GreaterThan(8f));
            Assert.That(distanceFromCenter, Is.GreaterThan(40f));
            Assert.That(distanceFromCenter, Is.LessThan(170f));
        }

        private static void AssertDamageNumberPopupIsReadable(TMPro.TMP_Text text)
        {
            Assert.That(text.color.r, Is.GreaterThan(0.95f));
            Assert.That(text.color.g, Is.GreaterThan(0.72f));
            Assert.That(text.color.b, Is.LessThan(0.2f));
            Assert.That(text.outlineColor, Is.EqualTo((Color32)Color.white));
            Assert.That(text.outlineWidth, Is.GreaterThanOrEqualTo(0.18f));
            Assert.That(text.fontMaterial.IsKeywordEnabled(TMPro.ShaderUtilities.Keyword_Glow), Is.True);
            Assert.That(text.fontMaterial.GetColor(TMPro.ShaderUtilities.ID_GlowColor).a, Is.GreaterThan(0.45f));
            Assert.That(text.fontMaterial.GetFloat(TMPro.ShaderUtilities.ID_GlowOuter), Is.GreaterThanOrEqualTo(0.28f));
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
        public void CombatEffectBinding_NormalizesPitchRangeAndStoresVfxOffset()
        {
            var effect = new CombatEffectBinding
            {
                minPitch = 1.2f,
                maxPitch = 0.8f,
                localOffset = new Vector3(0.2f, 1.1f, 0f),
            };

            Assert.That(effect.EffectiveMinPitch, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(effect.EffectiveMaxPitch, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(effect.ResolvePitch(), Is.InRange(0.8f, 1.2f));
            Assert.That(effect.localOffset, Is.EqualTo(new Vector3(0.2f, 1.1f, 0f)));
        }

        [Test]
        public void CombatWorldSpriteView_EnsureAudioSourceRestoresAudibleDefaults()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var source = viewObject.AddComponent<AudioSource>();
            source.playOnAwake = true;
            source.spatialBlend = 1f;
            source.volume = 0f;
            source.mute = true;
            source.loop = true;
            source.minDistance = 1f;
            source.maxDistance = 2f;
            var view = viewObject.AddComponent<CombatWorldSpriteView>();

            typeof(CombatWorldSpriteView)
                .GetMethod(
                    "EnsureAudioSource",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(view, null);

            Assert.That(source.playOnAwake, Is.False);
            Assert.That(source.spatialBlend, Is.EqualTo(0f).Within(0.001f));
            Assert.That(source.volume, Is.EqualTo(1f).Within(0.001f));
            Assert.That(source.mute, Is.False);
            Assert.That(source.loop, Is.False);
            Assert.That(source.minDistance, Is.GreaterThanOrEqualTo(10000f));
            Assert.That(source.maxDistance, Is.GreaterThanOrEqualTo(10000f));
            Assert.That(source.rolloffMode, Is.EqualTo(AudioRolloffMode.Linear));
        }

        [Test]
        public void BoardTileEffectProfile_UsesExplicitMergeEffectOnly()
        {
            var profile = ScriptableObject.CreateInstance<BoardTileEffectProfileSO>();
            var merge2048Prefab = CreateOwnedGameObject("Merge2048Vfx");
            var merge2048 = new CombatEffectBinding
            {
                vfxPrefab = merge2048Prefab,
            };
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
            Assert.That(profile.ResolveMergeEffect(128), Is.Null);
        }

        [Test]
        public void AudioRouter_BuildsOneMoveCuePerBoardMoveAndEachMergedResult()
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

            Assert.That(cues.Count(cue => cue.CueType == BoardTileEffectCueType.Move), Is.EqualTo(1));
            Assert.That(cues.Count(cue => cue.CueType == BoardTileEffectCueType.Merge), Is.EqualTo(1));

            var mergeCue = cues.Single(cue => cue.CueType == BoardTileEffectCueType.Merge);
            Assert.That(mergeCue.TileValue, Is.EqualTo(4));
            Assert.That(mergeCue.Position, Is.EqualTo(new Vector2Int(0, 0)));
        }

        [Test]
        public void CombatEventAudioPlayer_TracksCombatAndRewardEventCues()
        {
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var rewardManager = CreateOwnedGameObject("RewardManager").AddComponent<RewardManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var audioPlayer = CreateOwnedGameObject("CombatEventAudio").AddComponent<PrototypeCombatEventAudioPlayer>();
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 99);
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var reward = CreateReward(healPercentOfMaxHp: 0.3f, extraBoardMoveCount: 2);
            var runProgress = new RunProgress();

            rewardManager.Initialize(runProgress, CreateRewardTable(reward));
            SetPrivateField(bootstrap, "combatManager", manager);
            SetPrivateField(bootstrap, "rewardManager", rewardManager);

            audioPlayer.Initialize(bootstrap);
            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
                runProgress = runProgress,
            });
            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);
            Assert.That(audioPlayer.LastPlayedCue, Is.EqualTo(PrototypeCombatEventSoundCue.Victory));

            rewardManager.OfferReward(new CombatResult(), player);
            rewardManager.ChooseEnhance(player);

            Assert.That(audioPlayer.LastPlayedCue, Is.EqualTo(PrototypeCombatEventSoundCue.RewardEnhance));
        }

        [Test]
        public void CombatEventAudioPlayer_TracksDefeatEventCue()
        {
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var rewardManager = CreateOwnedGameObject("RewardManager").AddComponent<RewardManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var audioPlayer = CreateOwnedGameObject("CombatEventAudio").AddComponent<PrototypeCombatEventAudioPlayer>();
            var playerData = CreatePlayerData(maxHp: 10, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 20);

            SetPrivateField(bootstrap, "combatManager", manager);
            SetPrivateField(bootstrap, "rewardManager", rewardManager);

            audioPlayer.Initialize(bootstrap);
            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.BoardManager.SetBoardState(new[,]
            {
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
            }, 0);
            manager.ResolveBoardPhase();
            manager.RequestEndPlayerTurn();

            Assert.That(audioPlayer.LastPlayedCue, Is.EqualTo(PrototypeCombatEventSoundCue.Defeat));
        }

        [UnityTest]
        public IEnumerator CombatWorldSpriteView_StartCombat_PlaysEnemyIntroBeforeAppearEffectFromEnemySo()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var appearClip = AudioClip.Create("EnemyAppear", 512, 1, 44100, false);
            ownedObjects.Add(appearClip);
            enemyData.actionEffects = new List<CombatantActionEffectBinding>
            {
                new()
                {
                    actionId = CombatActionIds.Appear,
                    effect = new CombatEffectBinding
                    {
                        sfxClip = appearClip,
                        minPitch = 0.8f,
                        maxPitch = 0.8f,
                    },
                },
            };

            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            Assert.That(viewObject.transform.Find("CombatEffectAudio"), Is.Null);
            Assert.That(enemyRenderer.transform.localPosition.x, Is.GreaterThan(0f));

            yield return new WaitForSecondsRealtime(CombatWorldSpriteView.EnemyAppearIntroDurationSeconds + 0.1f);

            Assert.That(viewObject.transform.Find("CombatEffectAudio"), Is.Not.Null);
            Assert.That(enemyRenderer.transform.localPosition, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void CombatWorldSpriteView_EnemyAppearShake_UsesLongerStrongerTuning()
        {
            var magnitudeField = typeof(CombatWorldSpriteView).GetField(
                "EnemyAppearWorldShakeMagnitude",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            Assert.That(CombatWorldSpriteView.EnemyAppearWorldShakeDurationSeconds, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(magnitudeField, Is.Not.Null);
            Assert.That((float)magnitudeField.GetValue(null), Is.EqualTo(0.13f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator CombatWorldSpriteView_EnemyAppear_UsesAssignedWorldShakeBriefly()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var shakeTarget = CreateOwnedGameObject("AssignedWorldShakeRoot");
            var worldShake = shakeTarget.AddComponent<WorldShake>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);

            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "worldShake", worldShake);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            if (!Application.isPlaying)
            {
                yield break;
            }

            Assert.That(shakeTarget.transform.localPosition, Is.EqualTo(Vector3.zero));

            yield return new WaitForSecondsRealtime(CombatWorldSpriteView.EnemyAppearIntroDurationSeconds + 0.1f);

            Assert.That(shakeTarget.transform.localPosition, Is.Not.EqualTo(Vector3.zero));

            yield return new WaitForSecondsRealtime(CombatWorldSpriteView.EnemyAppearWorldShakeDurationSeconds + 0.1f);

            Assert.That(shakeTarget.transform.localPosition, Is.EqualTo(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator CombatWorldSpriteView_EnemyAppear_ShakesForegroundButKeepsBackgroundStill()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var backgroundRenderer = CreateOwnedGameObject("BackgroundSprite").AddComponent<SpriteRenderer>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);

            backgroundRenderer.transform.SetParent(viewObject.transform, false);
            playerRenderer.transform.SetParent(viewObject.transform, false);
            enemyRenderer.transform.SetParent(viewObject.transform, false);
            SetPrivateField(view, "backgroundRenderer", backgroundRenderer);
            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            if (!Application.isPlaying)
            {
                yield break;
            }

            var foregroundRoot = playerRenderer.transform.parent;
            Assert.That(foregroundRoot, Is.Not.Null);
            Assert.That(foregroundRoot.name, Is.EqualTo("ForegroundShakeRoot"));
            Assert.That(enemyRenderer.transform.parent, Is.EqualTo(foregroundRoot));
            Assert.That(foregroundRoot.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(backgroundRenderer.transform.parent, Is.EqualTo(viewObject.transform));
            Assert.That(backgroundRenderer.transform.localPosition, Is.EqualTo(Vector3.zero));

            yield return new WaitForSecondsRealtime(CombatWorldSpriteView.EnemyAppearIntroDurationSeconds + 0.1f);

            Assert.That(foregroundRoot.localPosition, Is.Not.EqualTo(Vector3.zero));
            Assert.That(backgroundRenderer.transform.localPosition, Is.EqualTo(Vector3.zero));

            yield return new WaitForSecondsRealtime(CombatWorldSpriteView.EnemyAppearWorldShakeDurationSeconds + 0.1f);

            Assert.That(foregroundRoot.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(backgroundRenderer.transform.localPosition, Is.EqualTo(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator CombatWorldSpriteView_EnemyAppear_DoesNotMovePhysicsRendererIntoWorldShakeRoot()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            playerRenderer.gameObject.AddComponent<Rigidbody2D>();
            playerRenderer.gameObject.AddComponent<BoxCollider2D>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);

            playerRenderer.transform.SetParent(viewObject.transform, false);
            enemyRenderer.transform.SetParent(viewObject.transform, false);
            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });

            if (!Application.isPlaying)
            {
                yield break;
            }

            var foregroundRoot = enemyRenderer.transform.parent;
            Assert.That(foregroundRoot, Is.Not.Null);
            Assert.That(foregroundRoot.name, Is.EqualTo("ForegroundShakeRoot"));
            Assert.That(playerRenderer.transform.parent, Is.EqualTo(viewObject.transform));
            Assert.That(foregroundRoot.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(playerRenderer.transform.localPosition, Is.EqualTo(Vector3.zero));

            yield return new WaitForSecondsRealtime(CombatWorldSpriteView.EnemyAppearIntroDurationSeconds + 0.1f);

            Assert.That(foregroundRoot.localPosition, Is.Not.EqualTo(Vector3.zero));
            Assert.That(playerRenderer.transform.localPosition, Is.EqualTo(Vector3.zero));

            yield return new WaitForSecondsRealtime(CombatWorldSpriteView.EnemyAppearWorldShakeDurationSeconds + 0.1f);

            Assert.That(foregroundRoot.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(playerRenderer.transform.localPosition, Is.EqualTo(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator CombatWorldSpriteView_EnemyAttack_LungesTowardPlayerAndSpawnsPlayerShieldParticles()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 2);

            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            player.AddBlock(4);
            view.Initialize(bootstrap);

            var restX = enemyRenderer.transform.localPosition.x;
            manager.RequestEndPlayerTurn();

            Assert.That(playerRenderer.transform.Find("ShieldImpactParticles"), Is.Not.Null);
            if (!Application.isPlaying)
            {
                yield break;
            }

            yield return null;

            Assert.That(enemyRenderer.transform.localPosition.x, Is.LessThan(restX));

            yield return new WaitForSecondsRealtime(CombatWorldSpriteView.EnemyAttackLungeDurationSeconds + 0.1f);

            Assert.That(enemyRenderer.transform.localPosition.x, Is.EqualTo(restX).Within(0.001f));
        }

        [Test]
        public void CombatWorldSpriteView_EnemyAttack_UsesAuthoredAttackEffectArtAtPlayer()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 3);
            var attackSprite = CreateOwnedSprite("AttackEffectSprite");
            var bottomPivotPlayerSprite = CreateOwnedSprite("BottomPivotPlayerSprite", Vector2.zero);

            playerRenderer.sortingOrder = 7;
            playerRenderer.sprite = bottomPivotPlayerSprite;
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "attackEffectSprite", attackSprite);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            manager.RequestEndPlayerTurn();

            var art = playerRenderer.transform.Find("EnemyAttackArt")?.GetComponent<SpriteRenderer>();
            Assert.That(art, Is.Not.Null);
            Assert.That(art.sprite, Is.EqualTo(attackSprite));
            Assert.That(art.transform.localPosition.x, Is.GreaterThan(0.65f));
            Assert.That(art.transform.localPosition.y, Is.GreaterThan(0.55f));
            Assert.That(art.transform.localScale.x, Is.GreaterThan(4.5f));
            Assert.That(Mathf.DeltaAngle(art.transform.localEulerAngles.z, 90f), Is.EqualTo(0f).Within(0.001f));
            Assert.That(art.sortingOrder, Is.GreaterThan(playerRenderer.sortingOrder));
            Assert.That(playerRenderer.transform.Find("EnemyClawSlash2D"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_PlayerAttackAgainstEnemyBlock_SpawnsEnemyShieldParticles()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 1);

            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            enemy.AddBlock(5);
            view.Initialize(bootstrap);

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);

            var particles = enemyRenderer.transform.Find("ShieldImpactParticles")?.GetComponent<ParticleSystem>();
            Assert.That(particles, Is.Not.Null);
            Assert.That(particles.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Sphere));

            var profile = Resources.Load<CombatWorldVfxProfileSO>("PrototypeCombatWorldVfxProfile");
            Assert.That(profile, Is.Not.Null);
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            Assert.That(renderer.sharedMaterial, Is.EqualTo(profile.shieldImpactEffect.particleMaterial));
        }

        [Test]
        public void CombatWorldSpriteView_PlayerHpDamage_ShowsUnsignedDamageNumberAtPlayerBody()
        {
            Random.InitState(2048);
            var canvasObject = CreateOwnedRectTransformObject("CombatCanvas");
            canvasObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var camera = CreateOwnedGameObject("MainCamera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 3);

            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            manager.RequestEndPlayerTurn();

            var popupLayer = canvasObject.transform.Find("DamageNumberPopupLayer");
            var popup = popupLayer?.Find("DamageNumberPopup");
            var text = popup != null ? popup.GetComponent<TMPro.TextMeshProUGUI>() : null;
            Assert.That(playerRenderer.transform.Find("DamageNumberPopup"), Is.Null);
            Assert.That(popupLayer, Is.Not.Null);
            Assert.That(text, Is.Not.Null);
            Assert.That(text.text, Is.EqualTo("3"));
            Assert.That(text.text, Does.Not.StartWith("-"));
            AssertDamageNumberPopupIsReadable(text);
            AssertPopupIsNearButNotCentered((RectTransform)popup, (RectTransform)popupLayer, camera, playerRenderer.transform.position);
        }

        [Test]
        public void CombatWorldSpriteView_PlayerSkillHpDamage_ShowsUnsignedDamageNumberAtEnemyBody()
        {
            Random.InitState(2048);
            var canvasObject = CreateOwnedRectTransformObject("CombatCanvas");
            canvasObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var camera = CreateOwnedGameObject("MainCamera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 4);

            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);

            var popupLayer = canvasObject.transform.Find("DamageNumberPopupLayer");
            var popup = popupLayer?.Find("DamageNumberPopup");
            var text = popup != null ? popup.GetComponent<TMPro.TextMeshProUGUI>() : null;
            Assert.That(enemyRenderer.transform.Find("DamageNumberPopup"), Is.Null);
            Assert.That(popupLayer, Is.Not.Null);
            Assert.That(text, Is.Not.Null);
            Assert.That(text.text, Is.EqualTo("4"));
            Assert.That(text.text, Does.Not.StartWith("-"));
            AssertDamageNumberPopupIsReadable(text);
            AssertPopupIsNearButNotCentered((RectTransform)popup, (RectTransform)popupLayer, camera, enemyRenderer.transform.position);
        }

        [UnityTest]
        public IEnumerator CombatWorldSpriteView_EnemyHitWithAuthoredVisual_DelaysHitSfxUntilVisualEnds()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 4);
            var hitClip = AudioClip.Create("EnemyHitDelayed", 512, 1, 44100, false);
            var hitVfxPrefab = CreateOwnedGameObject("EnemyHitVfx");
            ownedObjects.Add(hitClip);
            enemyData.actionEffects = new List<CombatantActionEffectBinding>
            {
                new()
                {
                    actionId = CombatActionIds.Hit,
                    effect = new CombatEffectBinding
                    {
                        sfxClip = hitClip,
                        vfxPrefab = hitVfxPrefab,
                        minPitch = 0.8f,
                        maxPitch = 0.8f,
                        autoDestroySeconds = 0.15f,
                    },
                },
            };

            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);
            Assert.That(enemyRenderer.transform.Find("EnemyHitVfx(Clone)"), Is.Not.Null);
            Assert.That(viewObject.transform.Find("CombatEffectAudio"), Is.Null);

            yield return new WaitForSecondsRealtime(0.1f);

            Assert.That(viewObject.transform.Find("CombatEffectAudio"), Is.Null);

            yield return new WaitForSecondsRealtime(0.1f);

            Assert.That(viewObject.transform.Find("CombatEffectAudio"), Is.Not.Null);
        }

        [Test]
        public void CombatWorldSpriteView_EnemyHitWithoutAuthoredVisual_PlaysHitSfxImmediately()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 4);
            var hitClip = AudioClip.Create("EnemyHitImmediate", 512, 1, 44100, false);
            ownedObjects.Add(hitClip);
            enemyData.actionEffects = new List<CombatantActionEffectBinding>
            {
                new()
                {
                    actionId = CombatActionIds.Hit,
                    effect = new CombatEffectBinding
                    {
                        sfxClip = hitClip,
                        minPitch = 0.8f,
                        maxPitch = 0.8f,
                    },
                },
            };

            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);
            Assert.That(viewObject.transform.Find("CombatEffectAudio"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator CombatWorldSpriteView_EnemyDebuffIntent_SpawnsDebuffCastParticlesFromEnemyThenPlayer()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Debuff,
                    debuffType = DebuffType.Fear,
                    value = 1,
                },
            };

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            manager.RequestEndPlayerTurn();

            var enemyParticles = enemyRenderer.transform.Find("FearDebuffCastParticles")?.GetComponent<ParticleSystem>();
            Assert.That(enemyParticles, Is.Not.Null);
            Assert.That(playerRenderer.transform.Find("FearDebuffCastParticles"), Is.Null);
            Assert.That(enemyParticles.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Circle));
            Assert.That(enemyParticles.velocityOverLifetime.enabled, Is.False);
            Assert.That(enemyParticles.rotationOverLifetime.enabled, Is.True);
            AssertColorApproximately(enemyParticles.main.startColor.color, Color.white);

            var profile = Resources.Load<CombatWorldVfxProfileSO>("PrototypeCombatWorldVfxProfile");
            Assert.That(profile, Is.Not.Null);
            var renderer = enemyParticles.GetComponent<ParticleSystemRenderer>();
            Assert.That(renderer.sharedMaterial, Is.EqualTo(profile.fearDebuffCastEffect.particleMaterial));

            yield return new WaitForSecondsRealtime(CombatWorldSpriteView.DebuffCastParticleLifetimeSeconds * 0.5f);

            Assert.That(playerRenderer.transform.Find("FearDebuffCastParticles"), Is.Null);

            yield return new WaitForSecondsRealtime(CombatWorldSpriteView.DebuffCastParticleLifetimeSeconds * 0.5f + 0.05f);

            var playerParticles = playerRenderer.transform.Find("FearDebuffCastParticles")?.GetComponent<ParticleSystem>();
            Assert.That(playerParticles, Is.Not.Null);
            Assert.That(playerParticles.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Circle));
        }

        [Test]
        public void CombatWorldSpriteView_EnemyDebuffIntent_WithAuthoredVfxStillSpawnsFearParticles()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var authoredVfxPrefab = CreateOwnedGameObject("AuthoredFearVfxPrefab");
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            enemyData.actionEffects = new List<CombatantActionEffectBinding>
            {
                new()
                {
                    actionId = CombatActionIds.DebuffFear,
                    effect = new CombatEffectBinding
                    {
                        vfxPrefab = authoredVfxPrefab,
                        autoDestroySeconds = 0f,
                    },
                },
            };
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Debuff,
                    debuffType = DebuffType.Fear,
                    value = 1,
                },
            };

            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            manager.RequestEndPlayerTurn();

            Assert.That(enemyRenderer.transform.Find("FearDebuffCastParticles"), Is.Not.Null);
        }

        [Test]
        public void PrototypeCombatWorldVfxProfile_AssignsShieldAndCcParticleMaterials()
        {
            var profile = Resources.Load<CombatWorldVfxProfileSO>("PrototypeCombatWorldVfxProfile");

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.attackEffectSprite, Is.Not.Null);
            Assert.That(profile.hitEffectSprite, Is.Not.Null);
            Assert.That(profile.shieldEffectSprite, Is.Not.Null);
            Assert.That(profile.thornShieldEffectSprite, Is.Not.Null);
            Assert.That(profile.magicCircleEffectSprite, Is.Not.Null);
            Assert.That(profile.flameEffectSprite, Is.Not.Null);
            Assert.That(profile.chainAttackEffectSprite, Is.Not.Null);
            Assert.That(profile.boundChainsEffectSprite, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(profile.attackEffectSprite),
                Is.EqualTo("Assets/Art/Source/ExP/Effects/Effect_Attack.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.hitEffectSprite),
                Is.EqualTo("Assets/Art/Source/ExP/Effects/Effect_HitImpact.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.shieldEffectSprite),
                Is.EqualTo("Assets/Art/Source/ExP/Effects/Effect_Shield.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.thornShieldEffectSprite),
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ThornShieldBarrier.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.magicCircleEffectSprite),
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_MagicCircle.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.flameEffectSprite),
                Is.EqualTo("Assets/Art/Source/ExP/Effects/Effect_Flame.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.chainAttackEffectSprite),
                Is.EqualTo("Assets/Art/Source/ExP 1/Effect_사슬공격.PNG"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.boundChainsEffectSprite),
                Is.EqualTo("Assets/Art/Source/ExP 1/Effect_묶인사슬.PNG"));
            Assert.That(profile.shieldImpactEffect.particleMaterial, Is.Not.Null);
            Assert.That(profile.fearDebuffCastEffect.particleMaterial, Is.Not.Null);
            Assert.That(profile.darknessDebuffCastEffect.particleMaterial, Is.Not.Null);
            Assert.That(profile.shieldImpactEffect.swirl, Is.False);
            Assert.That(profile.fearDebuffCastEffect.swirl, Is.True);
            Assert.That(profile.darknessDebuffCastEffect.swirl, Is.True);
            Assert.That(profile.shieldImpactEffect.useParticleColor, Is.False);
            Assert.That(profile.fearDebuffCastEffect.useParticleColor, Is.False);
            Assert.That(profile.darknessDebuffCastEffect.useParticleColor, Is.False);
            Assert.That(profile.shieldImpactEffect.EffectiveStartSize, Is.EqualTo(0.22f).Within(0.001f));
            Assert.That(profile.fearDebuffCastEffect.EffectiveStartSize, Is.EqualTo(0.28f).Within(0.001f));
            Assert.That(profile.darknessDebuffCastEffect.EffectiveStartSize, Is.EqualTo(0.28f).Within(0.001f));

            AssertColorApproximately(
                ResolveMaterialColor(profile.shieldImpactEffect.particleMaterial),
                new Color(0.687759f, 0.903759f, 0.961359f, 0.96f));
            AssertColorApproximately(
                ResolveMaterialColor(profile.fearDebuffCastEffect.particleMaterial),
                new Color(0.597893f, 0.093893f, 0.173093f, 0.95f));
            AssertColorApproximately(
                ResolveMaterialColor(profile.darknessDebuffCastEffect.particleMaterial),
                new Color(0.216816f, 0.116016f, 0.389616f, 0.95f));
        }

        [Test]
        public void CombatUiView_IntentIconMapping_UsesFearIconForChangeIntent()
        {
            var view = CreateOwnedGameObject("CombatUiView").AddComponent<CombatUiView>();
            var attackSprite = CreateOwnedSprite("AttackIntentSprite");
            var defenseSprite = CreateOwnedSprite("DefenseIntentSprite");
            var fearSprite = CreateOwnedSprite("FearIntentSprite");
            SetPrivateField(view, "attackIntentSprite", attackSprite);
            SetPrivateField(view, "defenseIntentSprite", defenseSprite);
            SetPrivateField(view, "fearIntentSprite", fearSprite);

            var resolveIntentIcon = typeof(CombatUiView).GetMethod(
                "ResolveIntentIcon",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(resolveIntentIcon, Is.Not.Null);
            Assert.That(
                resolveIntentIcon.Invoke(view, new object[] { new EnemyIntent { intentType = EnemyIntentType.Attack } }),
                Is.EqualTo(attackSprite));
            Assert.That(
                resolveIntentIcon.Invoke(view, new object[] { new EnemyIntent { intentType = EnemyIntentType.Defense } }),
                Is.EqualTo(defenseSprite));
            Assert.That(
                resolveIntentIcon.Invoke(view, new object[] { new EnemyIntent { intentType = EnemyIntentType.Debuff } }),
                Is.EqualTo(fearSprite));
        }

        [Test]
        public void ArtTeamAssets_AreImportedAsSpritesInEnglishNamedFolders()
        {
            var spritePaths = new[]
            {
                "Assets/Art/Source/ExP/Effects/Effect_Attack.png",
                "Assets/Art/Source/ExP/Effects/Effect_HitImpact.png",
                "Assets/Art/Source/ExP/Effects/Effect_Shield.png",
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ThornShieldBarrier.png",
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_MagicCircle.png",
                "Assets/Art/Source/ExP/Effects/Effect_Flame.png",
                "Assets/Art/Source/ExP 1/Effect_사슬공격.PNG",
                "Assets/Art/Source/ExP 1/Effect_묶인사슬.PNG",
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_TentacleWhip.png",
                "Assets/Art/UI/IntentIcons/Ui_Attack.png",
                "Assets/Art/UI/IntentIcons/Ui_Defense.png",
                "Assets/Art/UI/IntentIcons/Ui_Fear.png",
                "Assets/Art/UI/Controls/Ui_Pause.png",
                "Assets/Art/UI/Controls/Ui_Settings.png",
                "Assets/Art/UI/Controls/Ui_SelectButton.png",
                "Assets/Art/UI/Controls/Ui_CancelButton.png",
                "Assets/Art/UI/Board/Ui_Tile.png",
                "Assets/Art/UI/Board/Ui_CurrentPosition.png",
                "Assets/Art/UI/Windows/Ui_PauseWindow.png",
            };

            foreach (var path in spritePaths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path)
                    ?? AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().FirstOrDefault();
                Assert.That(sprite, Is.Not.Null, path);
            }
        }

        [Test]
        public void SkillVfxSceneSprites_UseExpectedImports()
        {
            AssertMultipleSpriteImport(
                "Assets/Art/Source/ExP 1/Effect_사슬공격.PNG",
                449f,
                229f,
                new Vector2(224.5f, 114.5f));
            AssertMultipleSpriteImport(
                "Assets/Art/Source/ExP 1/Effect_묶인사슬.PNG",
                407f,
                298f,
                new Vector2(203.5f, 149f));
            AssertSingleFullRectSpriteImport(
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_TentacleWhip.png",
                450f,
                800f);
        }

        [Test]
        public void FireVfxGraph_ExposesAmberControls()
        {
            const string path = "Assets/VFX Test/Prefab/vfxgraph_Fire.vfx";
            var graphAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
            var graphText = System.IO.File.ReadAllText(path).Replace("\r\n", "\n");
            var exposedProperties = new[]
            {
                "AmberRate",
                "AmberVelocityMin",
                "AmberVelocityMax",
                "AmberLifetimeMin",
                "AmberLifetimeMax",
                "AmberSizeMin",
                "AmberSizeMax",
            };

            Assert.That(graphAsset, Is.Not.Null);
            foreach (var exposedProperty in exposedProperties)
            {
                Assert.That(graphText, Does.Contain($"m_ExposedName: {exposedProperty}"));
                Assert.That(graphText, Does.Contain($"- name: {exposedProperty}"));
            }

            AssertVfxGraphLink(graphText, "8926484042661616002", "8926484042661614909");
            AssertVfxGraphLink(graphText, "8926484042661616004", "8926484042661614927");
            AssertVfxGraphLink(graphText, "8926484042661616009", "8926484042661614932");
            AssertVfxGraphLink(graphText, "8926484042661616014", "8926484042661614943");
            AssertVfxGraphLink(graphText, "8926484042661616016", "8926484042661614944");
            AssertVfxGraphLink(graphText, "8926484042661616018", "8926484042661614964");
            AssertVfxGraphLink(graphText, "8926484042661616020", "8926484042661614965");
        }

        [Test]
        public void ChainAttackProjectileVfxGraph_ExpandsOriginalChainTextureToFullLength()
        {
            const string path = "Assets/Art/Effects/SkillVFX/Graphs/SkillVfx_ChainAttackProjectile.vfx";
            var graphAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
            var graphText = System.IO.File.ReadAllText(path).Replace("\r\n", "\n");

            Assert.That(graphAsset, Is.Not.Null);
            Assert.That(graphText, Does.Contain("m_Name: SkillVfx_ChainAttackProjectile"));
            Assert.That(graphText, Does.Contain("\"guid\":\"0f723c449d8f40c44b1c7d85e4e722e3\""));
            Assert.That(graphText, Does.Contain("m_SerializableObject: 48"));
            Assert.That(graphText, Does.Contain("m_SerializableObject: 0.32"));
            Assert.That(graphText, Does.Contain("\"value\":0.10000000149011612"));
            Assert.That(graphText, Does.Contain("\"value\":1.0"));
            Assert.That(graphText, Does.Contain("\"r\":1.0,\"g\":1.0,\"b\":1.0,\"a\":1.0"));
            Assert.That(graphText, Does.Contain("\"alpha\":1.0"));
        }

        [Test]
        public void PrototypeCombatWorldVfxProfile_AssignsDesignTimeEffectPrefabs()
        {
            var profile = Resources.Load<CombatWorldVfxProfileSO>("PrototypeCombatWorldVfxProfile");

            Assert.That(profile, Is.Not.Null);
            AssertDesignTimePrefab(profile.attackEffectPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_AttackImpact.prefab");
            AssertDesignTimePrefab(profile.hitEffectPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_HitImpact.prefab");
            AssertDesignTimePrefab(profile.shieldEffectPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_ShieldBarrier.prefab");
            AssertDesignTimePrefab(profile.thornShieldEffectPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_ThornShieldBarrier.prefab");
            AssertShieldPrefabHasDedicatedArtAndSparkles(
                profile.shieldEffectPrefab,
                "Assets/Art/Source/ExP/Effects/Effect_Shield.png",
                "ShieldGuardSparkles");
            AssertShieldPrefabHasDedicatedArtAndSparkles(
                profile.thornShieldEffectPrefab,
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ThornShieldBarrier.png",
                "ThornGuardShieldSparkles");
            AssertDesignTimePrefab(profile.magicCircleEffectPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_MagicCircle.prefab");
            AssertDesignTimePrefab(profile.flameEffectPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_FlameBurst.prefab");
            AssertDesignTimePrefab(profile.darkChainLaunchPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_DarkShackleLaunch.prefab");
            AssertDesignTimePrefab(profile.chainAttackEffectPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_ChainAttackProjectile.prefab");
            Assert.That(profile.chainAttackEffectPrefab.GetComponent<VisualEffect>(), Is.Not.Null);
            AssertDesignTimePrefab(profile.boundChainsEffectPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_BoundChains.prefab");

            var launchLines = profile.darkChainLaunchPrefab.GetComponentsInChildren<LineRenderer>(true);
            Assert.That(launchLines.Any(line => line.name == "DarkShackleChainLine"), Is.True);
            Assert.That(launchLines.Any(line => line.name == "DarkShackleChainHead"), Is.True);
            Assert.That(launchLines.Count(line => line.name.StartsWith("DarkShackleChainLink")), Is.EqualTo(24));
        }

        [Test]
        public void PrototypeCombatWorldVfxProfile_AssignsDesignTimeBindingsForEverySkillVfxFamily()
        {
            var profile = Resources.Load<CombatWorldVfxProfileSO>("PrototypeCombatWorldVfxProfile");

            Assert.That(profile, Is.Not.Null);

            var expectedFamilies = System.Enum.GetValues(typeof(SkillVfxFamily))
                .Cast<SkillVfxFamily>()
                .Where(family => family != SkillVfxFamily.None)
                .OrderBy(family => family)
                .ToArray();
            var actualFamilies = profile.designTimeBindings
                .Where(binding => binding != null)
                .Select(binding => binding.family)
                .OrderBy(family => family)
                .ToArray();

            Assert.That(actualFamilies, Is.EquivalentTo(expectedFamilies));
            Assert.That(actualFamilies.Length, Is.EqualTo(actualFamilies.Distinct().Count()));

            foreach (var family in expectedFamilies)
            {
                var binding = profile.ResolveDesignTimeBinding(family);
                Assert.That(binding, Is.Not.Null, family.ToString());
                Assert.That(binding.sprite, Is.Not.Null, family.ToString());
                Assert.That(binding.prefab, Is.Not.Null, family.ToString());
                Assert.That(
                    AssetDatabase.GetAssetPath(binding.sprite),
                    Does.Match(@"^Assets/Art/(Effects/SkillVFX/Textures/|Source/)"),
                    family.ToString());
                Assert.That(
                    AssetDatabase.GetAssetPath(binding.prefab),
                    Does.StartWith("Assets/Art/Effects/SkillVFX/Prefabs/"),
                    family.ToString());
            }

            var slashArcBinding = profile.ResolveDesignTimeBinding(SkillVfxFamily.SlashArc);
            Assert.That(slashArcBinding.localOffset.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(slashArcBinding.radiusMultiplier, Is.EqualTo(3.24f).Within(0.001f));
            Assert.That(slashArcBinding.tintWhiteBlend, Is.EqualTo(0f).Within(0.001f));
            Assert.That(slashArcBinding.alpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                AssetDatabase.GetAssetPath(slashArcBinding.prefab),
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_AttackImpact.prefab"));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.LightProjectile).localOffset.x, Is.GreaterThan(0f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.LightProjectile).radiusMultiplier, Is.EqualTo(2.76f).Within(0.001f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.LightBeam).localOffset.x, Is.GreaterThan(0f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.LightBeam).radiusMultiplier, Is.EqualTo(3.54f).Within(0.001f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.BuffAura).localOffset.x, Is.GreaterThan(0f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.BuffAura).radiusMultiplier, Is.EqualTo(2.56f).Within(0.001f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.CounterReady).localOffset.x, Is.GreaterThan(0f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.CounterReady).radiusMultiplier, Is.EqualTo(2.36f).Within(0.001f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.SupportFire).radiusMultiplier, Is.EqualTo(2.46f).Within(0.001f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.ImpactBurst).alpha, Is.LessThan(0.75f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.ImpactBurst).radiusMultiplier, Is.EqualTo(3.24f).Within(0.001f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.DebuffWave).radiusMultiplier, Is.EqualTo(3.06f).Within(0.001f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.SpikedBurst).radiusMultiplier, Is.EqualTo(3.36f).Within(0.001f));
            Assert.That(profile.ResolveDesignTimeBinding(SkillVfxFamily.BloodFountainSlash).radiusMultiplier, Is.EqualTo(3.24f).Within(0.001f));
        }

        [Test]
        public void PrototypeSkillAssets_EmbedReusableVfxTuningForEveryReusableFamily()
        {
            var packageGuids = AssetDatabase.IsValidFolder("Assets/Art/Effects/SkillVFX/Packages")
                ? AssetDatabase.FindAssets(
                    "t:SkillVfxPackageSO",
                    new[] { "Assets/Art/Effects/SkillVFX/Packages" })
                : System.Array.Empty<string>();
            Assert.That(packageGuids, Is.Empty);

            var skills = AssetDatabase
                .FindAssets("t:SkillSO", new[] { "Assets/Data/Skills" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SkillSO>)
                .Where(skill => skill != null)
                .ToArray();

            Assert.That(skills.Length, Is.EqualTo(41));
            foreach (var skill in skills)
            {
                var path = AssetDatabase.GetAssetPath(skill);
                Assert.That(skill.vfxPackage, Is.Null, path);
                Assert.That(skill.vfx, Is.Not.Null, path);
                Assert.That(skill.vfx.HasAnySetting, Is.True, path);
                Assert.That(skill.vfx.family, Is.EqualTo(skill.ResolveVfxFamily()), path);
                Assert.That(skill.vfx.HasAuthoredVisual, Is.True, path);
            }
        }
        [Test]
        public void ArtAndSoundAssetPaths_UseAsciiEnglishNames()
        {
            var paths = AssetDatabase.FindAssets(string.Empty, new[] { "Assets/Art", "Assets/Sounds" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.Any(character => character > 127))
                .OrderBy(path => path)
                .ToArray();

            Assert.That(paths, Is.Empty, string.Join("\n", paths));
        }

        [Test]
        public void SkillVfxAssets_UseFlatTypeBucketsWithoutFamilyDummyFolders()
        {
            foreach (var requiredFolder in new[]
            {
                "Assets/Art/Effects/SkillVFX/Materials",
                "Assets/Art/Effects/SkillVFX/Prefabs",
                "Assets/Art/Effects/SkillVFX/Textures",
                "Assets/Art/Effects/SkillVFX/Shaders",
                "Assets/Art/Effects/SkillVFX/Resources/Shaders",
            })
            {
                Assert.That(AssetDatabase.IsValidFolder(requiredFolder), Is.True, requiredFolder);
            }

            foreach (var obsoleteFolder in new[]
            {
                "Assets/Art/Effects/SkillVFX/Attack",
                "Assets/Art/Effects/SkillVFX/Common",
                "Assets/Art/Effects/SkillVFX/Effects",
                "Assets/Art/Effects/SkillVFX/HolyFireball",
                "Assets/Art/Effects/SkillVFX/Packages",
                "Assets/Art/Effects/SkillVFX/Shield",
                "Assets/Art/Effects/SkillVFX/SkillSO",
                "Assets/Art/Effects/SkillVFX/Resources/Effects",
                "Assets/Art/Effects/SkillVFX/Resources/VFX",
            })
            {
                Assert.That(AssetDatabase.IsValidFolder(obsoleteFolder), Is.False, obsoleteFolder);
            }

            foreach (var flatBucket in new[]
            {
                "Assets/Art/Effects/SkillVFX/Materials",
                "Assets/Art/Effects/SkillVFX/Prefabs",
                "Assets/Art/Effects/SkillVFX/Textures",
                "Assets/Art/Effects/SkillVFX/Shaders",
            })
            {
                Assert.That(AssetDatabase.GetSubFolders(flatBucket), Is.Empty, flatBucket);
            }
        }

        [UnityTest]
        public IEnumerator PrototypeCombatBootstrap_AutoStart_WaitsForFlowGameStarted()
        {
            var flow = CreateOwnedGameObject("Flow").AddComponent<FlowController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);

            flow.Initialized(new GameContext());
            SetPrivateField(bootstrap, "playerData", playerData);
            SetPrivateField(bootstrap, "enemyData", enemyData);
            SetPrivateField(bootstrap, "randomizeEnemyOnStart", false);

            yield return null;

            Assert.That(bootstrap.CombatManager.CurrentPhase, Is.EqualTo(CombatPhase.None));

            flow.CompleteBattleSceneLoad();
            yield return null;

            Assert.That(bootstrap.CombatManager.CurrentPhase, Is.EqualTo(CombatPhase.BoardPhase));
        }

        [UnityTest]
        public IEnumerator BattleSceneBinder_CompletesBattleLoadAfterLoadingUiHides()
        {
            var flow = CreateOwnedGameObject("Flow").AddComponent<FlowController>();
            var loadingComponentObject = CreateOwnedGameObject("LoadingUI");
            var loadingUI = loadingComponentObject.AddComponent<LoadingUI>();
            var loadingRoot = CreateOwnedGameObject("LoadingRoot");
            var binder = CreateOwnedGameObject("BattleSceneBinder").AddComponent<BattleSceneBinder>();
            var gameStarted = false;

            flow.Initialized(new GameContext());
            flow.OnGameStarted += () => gameStarted = true;
            loadingRoot.SetActive(true);
            SetPrivateField(loadingUI, "root", loadingRoot);
            SetPrivateField(binder, "flowController", flow);

            yield return null;
            yield return null;

            Assert.That(gameStarted, Is.False);

            loadingRoot.SetActive(false);
            yield return null;

            Assert.That(gameStarted, Is.True);
        }

        [UnityTest]
        public IEnumerator BattleSceneBinder_AllowsStandalonePrototypeSceneWithoutFlow()
        {
            CreateOwnedGameObject("BattleSceneBinder").AddComponent<BattleSceneBinder>();

            yield return null;
            yield return null;

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CombatWorldSpriteView_PlayerSkill_PlaysActivationEffectFromSkillSo()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 1);
            var activationClip = AudioClip.Create("SkillActivation", 512, 1, 44100, false);
            ownedObjects.Add(activationClip);
            attack.activationEffect = new CombatEffectBinding
            {
                sfxClip = activationClip,
                minPitch = 0.8f,
                maxPitch = 0.8f,
            };

            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);
            Assert.That(viewObject.transform.Find("CombatEffectAudio"), Is.Not.Null);
        }

        [Test]
        public void CombatWorldSpriteView_PreviewSkill_PlaysPlayerAttackAnimationBeforeAnySkill()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerObject = CreateOwnedGameObject("player_all");
            var animator = playerObject.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/Art/Monsters/player/PlayerAnim.controller");
            var defense = CreateSkill("preview-defense", SkillType.Defense, cost: 0, power: 0);
            Assert.That(controller, Is.Not.Null);
            animator.runtimeAnimatorController = controller;

            SetPrivateField(view, "playerActorRoot", playerObject.transform);
            SetPrivateField(view, "playerAnimator", animator);

            animator.Rebind();
            animator.Update(0f);
            view.PreviewSkillEffect(defense);
            animator.Update(0f);

            Assert.That(
                animator.GetCurrentAnimatorStateInfo(0).shortNameHash,
                Is.EqualTo(Animator.StringToHash("Attack")));
        }

        [Test]
        public void CombatWorldSpriteView_CloseRangeSkills_UseFasterPlayerAttackAnimation()
        {
            var resolveSpeed = typeof(CombatWorldSpriteView).GetMethod(
                "ResolvePlayerAttackAnimationSpeedMultiplier",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var slash = CreateSkill("quick-stab", SkillType.Attack, cost: 0, power: 40);
            var heavy = CreateSkill("heavy-strike", SkillType.Attack, cost: 0, power: 80);
            var shield = CreateSkill("light-guard", SkillType.Defense, cost: 0, power: 20);
            slash.vfxFamily = SkillVfxFamily.SlashArc;
            heavy.vfxFamily = SkillVfxFamily.SpikedBurst;
            shield.vfxFamily = SkillVfxFamily.ShieldDome;

            Assert.That(resolveSpeed, Is.Not.Null);
            Assert.That((float)resolveSpeed.Invoke(null, new object[] { slash }), Is.EqualTo(1f).Within(0.001f));
            Assert.That((float)resolveSpeed.Invoke(null, new object[] { heavy }), Is.GreaterThan(1f));
            Assert.That((float)resolveSpeed.Invoke(null, new object[] { shield }), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void CombatWorldSpriteView_PlayerReusableSkill_AssignsGeneratedParticleMaterial()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 1);
            var expectedColor = new Color(0.4f, 0.8f, 1f, 0.9f);
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            attack.vfxFamily = SkillVfxFamily.BuffAura;
            attack.vfxPrimaryColor = expectedColor;

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);

            var particles = enemyRenderer.transform.Find("BuffAuraSkillParticles")?.GetComponent<ParticleSystem>();
            Assert.That(particles, Is.Not.Null);
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            Assert.That(renderer.sharedMaterial, Is.Not.Null);
            AssertColorApproximately(ResolveMaterialColor(renderer.sharedMaterial), expectedColor);
            Assert.That(particles.main.startSize.constant, Is.LessThanOrEqualTo(0.22f));

            var magicCircleArt = playerRenderer.transform.Find("BuffAuraEffectArt")?.GetComponent<SpriteRenderer>();
            Assert.That(magicCircleArt, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(magicCircleArt.sprite),
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_MagicCircle.png"));
            Assert.That(magicCircleArt.transform.position.x, Is.GreaterThan(playerRenderer.transform.position.x));
            Assert.That(enemyRenderer.transform.Find("BuffAuraEffectArt"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_BuffAuraNonHeal_UsesSwirlParticlesWithoutHealingVfx()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var profile = ScriptableObject.CreateInstance<CombatWorldVfxProfileSO>();
            var healingPrefab = CreateOwnedGameObject("SupportBuffHealingPrefab");
            var buff = CreateSkill("iron-wall", SkillType.Defense, cost: 0, power: 0);
            ownedObjects.Add(profile);
            healingPrefab.AddComponent<ParticleSystem>();
            profile.supportBuffVisualEffectPrefab = healingPrefab;
            buff.effectKind = SkillEffectKind.DefenseStageUp;
            buff.vfxFamily = SkillVfxFamily.BuffAura;
            buff.vfxPrimaryColor = new Color(0.45f, 0.55f, 0.67f, 1f);

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "worldVfxProfile", profile);

            view.PreviewSkillEffect(buff);

            Assert.That(playerRenderer.transform.Find("BuffAuraSkillParticles")?.GetComponent<ParticleSystem>(), Is.Not.Null);
            Assert.That(playerRenderer.transform.Find("BuffAuraHealingVisualEffect"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_BuffAuraHeal_UsesHealingVfxWithoutSwirlParticles()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var healingPrefab = CreateOwnedGameObject("SupportBuffHealingPrefab");
            var heal = CreateSkill("light-recover", SkillType.Heal, cost: 0, power: 0);
            healingPrefab.AddComponent<ParticleSystem>();
            heal.effectKind = SkillEffectKind.Heal;
            heal.healPercentOfMaxHp = 0.25f;
            heal.vfxFamily = SkillVfxFamily.BuffAura;
            heal.vfx = CreateOwnedVfxTuning(SkillVfxFamily.BuffAura);
            heal.vfx.secondaryPrefab = healingPrefab;
            heal.vfxPrimaryColor = new Color(0.95f, 0.84f, 0.42f, 1f);

            SetPrivateField(view, "playerRenderer", playerRenderer);

            view.PreviewSkillEffect(heal);

            Assert.That(playerRenderer.transform.Find("BuffAuraHealingVisualEffect")?.GetComponent<ParticleSystem>(), Is.Not.Null);
            Assert.That(playerRenderer.transform.Find("BuffAuraSkillParticles"), Is.Null);
            Assert.That(playerRenderer.transform.Find("BuffAuraEffectArt"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_EndurePreview_UsesWhiteHealingVfxOnly()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var healingPrefab = CreateOwnedGameObject("EndureHealingPrefab");
            healingPrefab.AddComponent<ParticleSystem>();
            var endure = CreateSkill("endure", SkillType.Defense, cost: 0, power: 0);
            endure.effectKind = SkillEffectKind.Endure;
            endure.vfxFamily = SkillVfxFamily.CounterReady;
            endure.vfx = CreateOwnedVfxTuning(SkillVfxFamily.CounterReady);
            endure.vfx.secondaryPrefab = healingPrefab;
            endure.vfxPrimaryColor = Color.white;
            endure.vfxSecondaryColor = Color.white;
            endure.activationEffect.particleEffect = new CombatParticleEffectBinding
            {
                objectName = "EndureSkillParticles",
                useParticleColor = true,
                particleColor = Color.white,
            };

            SetPrivateField(view, "playerRenderer", playerRenderer);

            view.PreviewSkillEffect(endure);

            var healing = playerRenderer.transform.Find("CounterReadyHealingVisualEffect");
            Assert.That(healing, Is.Not.Null);
            var healingParticles = healing.GetComponent<ParticleSystem>();
            Assert.That(healingParticles, Is.Not.Null);
            AssertColorApproximately(healingParticles.main.startColor.color, Color.white);
            Assert.That(healing.localPosition.y, Is.GreaterThan(0.2f));
            Assert.That(playerRenderer.transform.Find("CounterReadySkillParticles"), Is.Null);
            Assert.That(playerRenderer.transform.Find("CounterReadyEffectArt"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_PlayerReusableSkill_UsesSoBoundParticleMaterial()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 1);
            var material = new Material(Shader.Find("Sprites/Default"))
            {
                name = "SkillSoBoundParticles",
            };
            ownedObjects.Add(material);
            var expectedColor = new Color(0.4f, 0.8f, 1f, 0.9f);
            attack.vfxFamily = SkillVfxFamily.BuffAura;
            attack.vfxPrimaryColor = expectedColor;
            attack.activationEffect.particleEffect = new CombatParticleEffectBinding
            {
                objectName = "SoBoundSkillParticles",
                particleMaterial = material,
                useParticleColor = true,
                particleColor = expectedColor,
                lifetimeSeconds = 0.65f,
                burstCount = 11,
                startSpeed = 0.44f,
                startSize = 0.18f,
            };

            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);

            var particles = enemyRenderer.transform.Find("SoBoundSkillParticles")?.GetComponent<ParticleSystem>();
            Assert.That(particles, Is.Not.Null);
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            Assert.That(particles.main.startSize.constant, Is.EqualTo(0.18f).Within(0.001f));
        }

        [Test]
        public void CombatWorldSpriteView_ImpactBurstPreview_UsesHitImpactArt()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var impact = CreateSkill("body-press", SkillType.Attack, cost: 0, power: 80);
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            impact.vfxFamily = SkillVfxFamily.ImpactBurst;
            impact.vfxPrimaryColor = new Color(0.9f, 0.72f, 0.32f, 1f);

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            view.PreviewSkillEffect(impact);

            var art = enemyRenderer.transform.Find("HitImpactEffectArt")?.GetComponent<SpriteRenderer>();
            Assert.That(art, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(art.sprite),
                Is.EqualTo("Assets/Art/Source/ExP/Effects/Effect_HitImpact.png"));
            Assert.That(art.color.a, Is.LessThan(0.75f));
            Assert.That(art.transform.localPosition.x, Is.LessThan(0f));
            Assert.That(enemyRenderer.transform.Find("ImpactBurstCloseRangeImpactParticles"), Is.Not.Null);
            Assert.That(enemyRenderer.transform.Find("AttackEffectArt"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("ImpactBurstSkillParticles"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_SlashArcPreview_UsesExpAttackBetweenCombatantsAndHitOnEnemy()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var attackSprite = CreateOwnedSprite("SlashArcAttackBeamSprite");
            var hitSprite = CreateOwnedSprite("SlashArcHitImpactSprite");
            var slash = CreateSkill("quick-stab", SkillType.Attack, cost: 0, power: 40);
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            slash.vfxFamily = SkillVfxFamily.SlashArc;
            slash.vfx = CreateOwnedVfxTuning(SkillVfxFamily.SlashArc);
            slash.vfx.primarySprite = attackSprite;
            slash.vfxPrimaryColor = new Color(0.2f, 0.6f, 1f, 1f);

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "hitEffectSprite", hitSprite);

            view.PreviewSkillEffect(slash);

            var beam = viewObject.transform.Find("SlashArcAttackBeamArt")?.GetComponent<SpriteRenderer>();
            Assert.That(beam, Is.Not.Null);
            Assert.That(beam.sprite, Is.EqualTo(attackSprite));
            AssertColorApproximately(beam.color, Color.white);
            Assert.That(beam.bounds.center.x, Is.GreaterThan(playerRenderer.transform.position.x));
            Assert.That(beam.bounds.center.x, Is.LessThan(enemyRenderer.transform.position.x));
            Assert.That(beam.transform.localScale.x, Is.EqualTo(beam.transform.localScale.y).Within(0.001f));

            var impactArt = enemyRenderer.transform.Find("HitImpactEffectArt")?.GetComponent<SpriteRenderer>();
            Assert.That(impactArt, Is.Not.Null);
            Assert.That(impactArt.sprite, Is.EqualTo(hitSprite));
            AssertColorApproximately(impactArt.color, Color.white);
            Assert.That(impactArt.bounds.center.x, Is.EqualTo(enemyRenderer.transform.position.x).Within(0.001f));
            Assert.That(impactArt.transform.localScale.x, Is.EqualTo(impactArt.transform.localScale.y).Within(0.001f));
            Assert.That(beam.transform.localScale.x, Is.LessThan(impactArt.transform.localScale.x * 0.5f));
            Assert.That(enemyRenderer.transform.Find("HeavyStrikeSpikedBurstArt"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("HeavyStrikeSpikedBurst"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("SlashArcSkillParticles"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_SlashArcSource_UsesAuthoredPlayerFrontOffset()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var source = CreateOwnedGameObject("Source").transform;
            var target = CreateOwnedGameObject("Target").transform;
            var slash = CreateSkill("quick-stab", SkillType.Attack, cost: 0, power: 40);
            source.position = new Vector3(-1f, 0f, 0f);
            target.position = new Vector3(1f, 0f, 0f);
            slash.vfx = CreateOwnedVfxTuning(SkillVfxFamily.SlashArc);
            slash.vfx.localOffset = new Vector3(0f, 0.22f, 0f);

            var method = typeof(CombatWorldSpriteView).GetMethod(
                "ResolveSlashSkillSourcePosition",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            var playerCast = (Vector3)method.Invoke(view, new object[] { slash, source, target });
            Assert.That(playerCast.x, Is.EqualTo(source.position.x + 0.68f).Within(0.001f));
            Assert.That(playerCast.y, Is.EqualTo(source.position.y + 0.22f).Within(0.001f));

            source.position = new Vector3(1f, 0f, 0f);
            target.position = new Vector3(-1f, 0f, 0f);

            var enemyCast = (Vector3)method.Invoke(view, new object[] { slash, source, target });
            Assert.That(enemyCast.x, Is.EqualTo(source.position.x - 0.68f).Within(0.001f));
            Assert.That(enemyCast.y, Is.EqualTo(source.position.y + 0.22f).Within(0.001f));
        }

        [Test]
        public void CombatWorldSpriteView_LightProjectilePreview_UsesProfileDesignTimeArt()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var profile = ScriptableObject.CreateInstance<CombatWorldVfxProfileSO>();
            var projectileSprite = CreateOwnedSprite("LightProjectileDesignTimeSprite");
            var projectile = CreateSkill("light-projectile", SkillType.Attack, cost: 0, power: 24);
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            projectile.vfxFamily = SkillVfxFamily.LightProjectile;
            projectile.vfxPrimaryColor = new Color(0.88f, 0.96f, 1f, 1f);
            profile.designTimeBindings = new[]
            {
                new SkillVfxDesignTimeBinding
                {
                    family = SkillVfxFamily.LightProjectile,
                    sprite = projectileSprite,
                    localOffset = new Vector3(0f, 0.16f, 0f),
                    radiusMultiplier = 3f,
                },
            };
            ownedObjects.Add(profile);

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "worldVfxProfile", profile);

            view.PreviewSkillEffect(projectile);

            var art = playerRenderer.transform.Find("LightProjectileEffectArt")?.GetComponent<SpriteRenderer>();
            Assert.That(art, Is.Not.Null);
            Assert.That(art.sprite, Is.EqualTo(projectileSprite));
            Assert.That(art.transform.position.x, Is.GreaterThan(playerRenderer.transform.position.x));
            Assert.That(art.transform.localScale.x, Is.GreaterThan(4f));
            Assert.That(enemyRenderer.transform.Find("LightProjectileEffectArt"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("LightProjectileSkillParticles"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_ThornGuardPreview_UsesThornShieldArt()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var genericShieldSprite = CreateOwnedSprite("GenericShieldPreviewSprite");
            var thornShieldSprite = CreateOwnedSprite("ThornShieldPreviewSprite");
            var thornGuard = CreateSkill("thorn-guard", SkillType.Defense, cost: 0, power: 40);
            thornGuard.effectKind = SkillEffectKind.ThornGuard;
            thornGuard.selfThornRetaliationDamage = 40;
            thornGuard.vfxFamily = SkillVfxFamily.ShieldDome;
            thornGuard.vfxPrimaryColor = new Color(0.05f, 0.22f, 0.16f, 1f);
            thornGuard.vfxSecondaryColor = new Color(0.46f, 0.1f, 0.08f, 1f);
            thornGuard.activationEffect.particleEffect = new CombatParticleEffectBinding
            {
                objectName = "ThornGuardPreviewParticles",
                useParticleColor = true,
                particleColor = thornGuard.vfxPrimaryColor,
            };

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "shieldEffectSprite", genericShieldSprite);
            SetPrivateField(view, "thornShieldEffectSprite", thornShieldSprite);

            view.PreviewSkillEffect(thornGuard);

            var shieldRoot = viewObject.transform.Find("ThornGuardShieldVfx");
            Assert.That(shieldRoot, Is.Not.Null);
            var shieldArt = shieldRoot.Find("ThornGuardShieldArt")?.GetComponent<SpriteRenderer>();
            Assert.That(shieldArt, Is.Not.Null);
            Assert.That(shieldArt.sprite, Is.EqualTo(thornShieldSprite));
            Assert.That(shieldArt.sprite, Is.Not.EqualTo(genericShieldSprite));
        }

        [Test]
        public void CombatWorldSpriteView_SupportFirePreview_UsesMagicCircleAndAttackImpactArt()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var attackSprite = CreateOwnedSprite("SupportFireImpactSprite");
            var magicCircleSprite = CreateOwnedSprite("SupportFireMagicCircleSprite");
            var attackPrefab = CreateOwnedSpritePrefab(
                "SupportFireImpactPrefab",
                attackSprite,
                "SupportFireImpactPrefabMarker");
            var magicCirclePrefab = CreateOwnedSpritePrefab(
                "SupportFireMagicCirclePrefab",
                magicCircleSprite,
                "SupportFireMagicCirclePrefabMarker");
            var supportPackage = CreateOwnedVfxTuning(SkillVfxFamily.SupportFire);
            supportPackage.primarySprite = attackSprite;
            supportPackage.primaryPrefab = attackPrefab;
            supportPackage.secondarySprite = magicCircleSprite;
            supportPackage.secondaryPrefab = magicCirclePrefab;
            supportPackage.radiusMultiplier = 2.46f;
            var supportFire = CreateSkill("support-fire-test", SkillType.Attack, cost: 0, power: 20);
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            supportFire.vfxFamily = SkillVfxFamily.SupportFire;
            supportFire.vfx = supportPackage;
            supportFire.vfxPrimaryColor = new Color(0.92f, 0.86f, 0.28f, 1f);
            supportFire.vfxSecondaryColor = new Color(0.54f, 0.82f, 1f, 1f);
            supportFire.vfxRepeatCount = 3;

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            view.PreviewSkillEffect(supportFire);

            var magicCircleArt = playerRenderer.transform.Find("MagicCircleEffectArt")?.GetComponent<SpriteRenderer>();
            Assert.That(magicCircleArt, Is.Not.Null);
            Assert.That(magicCircleArt.sprite, Is.EqualTo(magicCircleSprite));
            Assert.That(magicCircleArt.transform.position.x, Is.GreaterThan(playerRenderer.transform.position.x));
            Assert.That(magicCircleArt.transform.localScale.x, Is.GreaterThan(2.8f));
            Assert.That(playerRenderer.transform.Find("MagicCircleEffectArt/SupportFireMagicCirclePrefabMarker"), Is.Not.Null);
            Assert.That(enemyRenderer.transform.Find("MagicCircleEffectArt"), Is.Null);

            var impactArt = enemyRenderer.transform.Find("SupportFireImpactArt")?.GetComponent<SpriteRenderer>();
            Assert.That(impactArt, Is.Not.Null);
            Assert.That(impactArt.sprite, Is.EqualTo(attackSprite));
            Assert.That(impactArt.transform.localScale.x, Is.GreaterThan(3.5f));
            Assert.That(enemyRenderer.transform.Find("SupportFireImpactArt/SupportFireImpactPrefabMarker"), Is.Not.Null);
            Assert.That(enemyRenderer.transform.Find("LightEchoSupportFireParticles"), Is.Not.Null);
        }

        [Test]
        public void CombatWorldSpriteView_LightEchoPreview_UsesYellowBuffParticlesOnly()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var attackSprite = CreateOwnedSprite("IgnoredLightEchoImpactSprite");
            var magicCircleSprite = CreateOwnedSprite("IgnoredLightEchoMagicCircleSprite");
            var attackPrefab = CreateOwnedSpritePrefab(
                "IgnoredLightEchoImpactPrefab",
                attackSprite,
                "IgnoredLightEchoImpactPrefabMarker");
            var magicCirclePrefab = CreateOwnedSpritePrefab(
                "IgnoredLightEchoMagicCirclePrefab",
                magicCircleSprite,
                "IgnoredLightEchoMagicCirclePrefabMarker");
            var particlePrefab = CreateOwnedGameObject("LightEchoParticlePrefab").AddComponent<ParticleSystem>();
            var lightEcho = CreateSkill("light-echo", SkillType.Defense, cost: 0, power: 0);
            var yellow = new Color(0.94f, 0.76f, 0.34f, 1f);
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            lightEcho.effectKind = SkillEffectKind.NextAttackPowerMultiplier;
            lightEcho.vfxFamily = SkillVfxFamily.SupportFire;
            lightEcho.vfx = CreateOwnedVfxTuning(SkillVfxFamily.SupportFire);
            lightEcho.vfx.primarySprite = attackSprite;
            lightEcho.vfx.primaryPrefab = attackPrefab;
            lightEcho.vfx.secondarySprite = magicCircleSprite;
            lightEcho.vfx.secondaryPrefab = magicCirclePrefab;
            lightEcho.vfxPrimaryColor = yellow;
            lightEcho.activationEffect.particleEffect = new CombatParticleEffectBinding
            {
                objectName = "LightEchoSkillParticles",
                particlePrefab = particlePrefab,
                useParticleColor = true,
                particleColor = yellow,
                lifetimeSeconds = 0.55f,
                burstCount = 130,
                startSpeed = 1.05f,
                startSize = 0.14f,
                swirl = false,
            };

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            view.PreviewSkillEffect(lightEcho);

            var particles = playerRenderer.transform.Find("LightEchoSkillParticles")?.GetComponent<ParticleSystem>();
            Assert.That(particles, Is.Not.Null);
            AssertColorApproximately(particles.main.startColor.color, yellow);
            Assert.That(playerRenderer.transform.Find("MagicCircleEffectArt"), Is.Null);
            Assert.That(playerRenderer.transform.Find("SupportFireImpactArt"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("SupportFireImpactArt"), Is.Null);
            Assert.That(playerRenderer.transform.Find("LightEchoSupportFireParticles"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("LightEchoSupportFireParticles"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_PlayerShieldSkill_UsesShieldArtAndSmallSparklesOnly()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var shield = CreateSkill("low-stance", SkillType.Defense, cost: 0, power: 30);
            var bottomPivotPlayerSprite = CreateOwnedSprite("BottomPivotPlayerSprite", Vector2.zero);
            shield.vfxFamily = SkillVfxFamily.ShieldDome;
            shield.vfxPrimaryColor = new Color(0.58f, 0.68f, 0.82f, 1f);
            shield.vfxSecondaryColor = new Color(0.82f, 0.9f, 1f, 1f);
            shield.activationEffect.particleEffect = new CombatParticleEffectBinding
            {
                objectName = "LowStanceSkillParticles",
                useParticleColor = true,
                particleColor = shield.vfxPrimaryColor,
            };
            var shieldSprite = CreateOwnedSprite("ShieldEffectSprite");
            var shieldPrefab = CreateOwnedShieldPrefab("ShieldGuardPrefab", shieldSprite, "ShieldGuardSparkles");
            var shieldPackage = CreateOwnedVfxTuning(SkillVfxFamily.ShieldDome);
            shieldPackage.primarySprite = shieldSprite;
            shieldPackage.primaryPrefab = shieldPrefab;
            shield.vfx = shieldPackage;
            playerData.portrait = bottomPivotPlayerSprite;
            playerRenderer.sortingOrder = 6;

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "shieldEffectSprite", shieldSprite);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkill(shield), Is.True);

            Assert.That(playerRenderer.transform.Find("ShieldLightCircleRing"), Is.Null);
            Assert.That(playerRenderer.transform.Find("ShieldLightCircleHalo"), Is.Null);
            Assert.That(playerRenderer.transform.Find("ShieldLightCircleVfxGraph"), Is.Null);
            Assert.That(playerRenderer.transform.Find("ShieldLightDomeParticles"), Is.Null);
            Assert.That(playerRenderer.transform.Find("ShieldLightCircleParticles"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("ShieldLightCircleRing"), Is.Null);
            Assert.That(playerRenderer.transform.Find("ShieldGuardSparkles"), Is.Null);

            var shieldArtRoot = playerRenderer.transform.Find("ShieldGuardArt");
            var sparkles = shieldArtRoot?.Find("ShieldGuardSparkles")?.GetComponent<ParticleSystem>();
            Assert.That(sparkles, Is.Not.Null);
            Assert.That(sparkles.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Circle));
            Assert.That(sparkles.shape.radius, Is.GreaterThan(0.35f));
            Assert.That(sparkles.main.startSize.constantMax, Is.LessThanOrEqualTo(0.06f));

            var shieldArt = shieldArtRoot?.GetComponent<SpriteRenderer>();
            Assert.That(shieldArt, Is.Not.Null);
            Assert.That(shieldArt.sprite, Is.EqualTo(shieldSprite));
            Assert.That(shieldArt.sortingOrder, Is.GreaterThanOrEqualTo(playerRenderer.sortingOrder + 12));
            Assert.That(shieldArtRoot.localPosition.x, Is.LessThan(0.45f));
            Assert.That(shieldArtRoot.localPosition.y, Is.GreaterThan(0.5f));
            Assert.That(shieldArt.transform.localScale.x, Is.GreaterThan(2.4f));

            var persistentShieldRoot = viewObject.transform.Find("PlayerShieldArtVfx");
            Assert.That(persistentShieldRoot, Is.Not.Null);
            var persistentShieldArt = persistentShieldRoot.Find("PlayerPersistentShieldArt")?.GetComponent<SpriteRenderer>();
            Assert.That(persistentShieldArt, Is.Not.Null);
            Assert.That(persistentShieldArt.sprite, Is.EqualTo(shieldSprite));
            Assert.That(persistentShieldArt.sortingOrder, Is.GreaterThanOrEqualTo(playerRenderer.sortingOrder + 12));
            Assert.That(persistentShieldArt.transform.localPosition.x, Is.LessThan(0.45f));
            Assert.That(persistentShieldRoot.position.y, Is.GreaterThan(playerRenderer.transform.position.y + 0.5f));
            Assert.That(persistentShieldArt.transform.localScale.x, Is.GreaterThan(2.6f));
        }

        [Test]
        public void CombatWorldSpriteView_ThornGuardShieldSkill_UsesThornShieldArtAndSmallSparklesOnly()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var thornGuard = CreateSkill("thorn-guard", SkillType.Defense, cost: 0, power: 40);
            var bottomPivotPlayerSprite = CreateOwnedSprite("ThornBottomPivotPlayerSprite", Vector2.zero);
            thornGuard.effectKind = SkillEffectKind.ThornGuard;
            thornGuard.selfThornRetaliationDamage = 40;
            thornGuard.vfxFamily = SkillVfxFamily.ShieldDome;
            thornGuard.vfxPrimaryColor = new Color(0.05f, 0.22f, 0.16f, 1f);
            thornGuard.vfxSecondaryColor = new Color(0.46f, 0.1f, 0.08f, 1f);
            var shieldSprite = CreateOwnedSprite("GenericShieldSprite");
            var thornShieldSprite = CreateOwnedSprite("ThornGuardShieldSprite");
            var thornShieldPrefab = CreateOwnedShieldPrefab(
                "ThornGuardShieldPrefab",
                thornShieldSprite,
                "ThornGuardShieldSparkles");
            var thornPackage = CreateOwnedVfxTuning(SkillVfxFamily.ShieldDome);
            thornPackage.primarySprite = shieldSprite;
            thornPackage.secondarySprite = thornShieldSprite;
            thornPackage.secondaryPrefab = thornShieldPrefab;
            thornGuard.vfx = thornPackage;
            playerData.portrait = bottomPivotPlayerSprite;

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "shieldEffectSprite", shieldSprite);
            SetPrivateField(view, "thornShieldEffectSprite", thornShieldSprite);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkill(thornGuard), Is.True);

            var shieldRoot = viewObject.transform.Find("ThornGuardShieldVfx");
            Assert.That(shieldRoot, Is.Not.Null);
            Assert.That(shieldRoot.parent, Is.EqualTo(viewObject.transform));
            Assert.That(playerRenderer.transform.Find("ThornGuardSpikedCircleRing"), Is.Null);
            Assert.That(shieldRoot.Find("ThornGuardSpikedCircleRing"), Is.Null);
            Assert.That(shieldRoot.Find("ThornGuardDarkInnerCircle"), Is.Null);
            Assert.That(shieldRoot.Find("ThornGuardTriangleSpikes"), Is.Null);
            Assert.That(shieldRoot.Find("ThornGuardSpikedCircleVfxGraph"), Is.Null);
            Assert.That(shieldRoot.Find("ThornGuardSpikeParticles"), Is.Null);
            Assert.That(shieldRoot.Find("ThornGuardSpikedCircleParticles"), Is.Null);
            Assert.That(shieldRoot.Find("ThornGuardShieldSparkles"), Is.Null);

            var shieldArtRoot = shieldRoot.Find("ThornGuardShieldArt");
            var shieldArt = shieldArtRoot?.GetComponent<SpriteRenderer>();
            Assert.That(shieldArt, Is.Not.Null);
            Assert.That(shieldArt.sprite, Is.EqualTo(thornShieldSprite));
            Assert.That(shieldArt.sortingOrder, Is.GreaterThanOrEqualTo(playerRenderer.sortingOrder + 12));
            Assert.That(shieldArtRoot.localPosition.x, Is.LessThan(0.45f));
            Assert.That(shieldRoot.position.y, Is.GreaterThan(playerRenderer.transform.position.y + 0.5f));
            Assert.That(shieldArt.transform.localScale.x, Is.LessThan(1.9f));

            var sparkles = shieldArtRoot?.Find("ThornGuardShieldSparkles")?.GetComponent<ParticleSystem>();
            Assert.That(sparkles, Is.Not.Null);
            Assert.That(sparkles.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Circle));
            Assert.That(sparkles.shape.radius, Is.GreaterThan(0.35f));
            Assert.That(sparkles.main.startSpeed.constantMax, Is.LessThanOrEqualTo(0.18f));
        }

        [Test]
        public void CombatWorldSpriteView_PlayerShieldArt_KeepsFirstStyleButThornGuardOverridesUntilShieldBreaks()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 80, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var lowStance = CreateSkill("low-stance", SkillType.Defense, cost: 0, power: 20);
            var thornGuard = CreateSkill("thorn-guard", SkillType.Defense, cost: 0, power: 30);
            var shieldSprite = CreateOwnedSprite("PersistentGenericShieldSprite");
            var thornShieldSprite = CreateOwnedSprite("PersistentThornShieldSprite");
            var thornShieldPrefab = CreateOwnedShieldPrefab(
                "PersistentThornShieldPrefab",
                thornShieldSprite,
                "PersistentThornShieldSparkles");
            var thornPackage = CreateOwnedVfxTuning(SkillVfxFamily.ShieldDome);
            thornPackage.secondarySprite = thornShieldSprite;
            thornPackage.secondaryPrefab = thornShieldPrefab;
            lowStance.vfxFamily = SkillVfxFamily.ShieldDome;
            thornGuard.effectKind = SkillEffectKind.ThornGuard;
            thornGuard.selfThornRetaliationDamage = 12;
            thornGuard.vfxFamily = SkillVfxFamily.ShieldDome;
            thornGuard.vfx = thornPackage;

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "shieldEffectSprite", shieldSprite);
            SetPrivateField(view, "thornShieldEffectSprite", thornShieldSprite);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkill(lowStance), Is.True);
            Assert.That(viewObject.transform.Find("PlayerShieldArtVfx"), Is.Not.Null);
            Assert.That(viewObject.transform.Find("ThornGuardShieldVfx"), Is.Null);

            manager.ClearSkillPresentationLock();
            Assert.That(manager.RequestUseSkill(thornGuard), Is.True);
            var thornRoot = viewObject.transform.Find("ThornGuardShieldVfx");
            Assert.That(viewObject.transform.Find("PlayerShieldArtVfx"), Is.Null);
            Assert.That(thornRoot, Is.Not.Null);
            Assert.That(thornRoot.Find("ThornGuardShieldArt")?.GetComponent<SpriteRenderer>()?.sprite, Is.EqualTo(thornShieldSprite));

            manager.ClearSkillPresentationLock();
            Assert.That(manager.RequestUseSkill(lowStance), Is.True);
            Assert.That(viewObject.transform.Find("PlayerShieldArtVfx"), Is.Null);
            Assert.That(viewObject.transform.Find("ThornGuardShieldVfx"), Is.Not.Null);

            player.TakeDamage(player.ShieldHp);
            manager.ClearSkillPresentationLock();
            Assert.That(viewObject.transform.Find("PlayerShieldArtVfx"), Is.Null);
            Assert.That(viewObject.transform.Find("ThornGuardShieldVfx"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_ShieldBurstAttack_FliesShieldAndSpawnsEasyExplosion()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 120, attackValue: 0);
            var shieldBurst = CreateSkill("shield-burst", SkillType.Attack, cost: 0, power: 100);
            shieldBurst.effectKind = SkillEffectKind.ShieldBurstAttack;
            shieldBurst.consumesAllShield = true;
            shieldBurst.vfxFamily = SkillVfxFamily.ShieldDome;
            shieldBurst.vfxPrimaryColor = new Color(0.72f, 0.9f, 1f, 1f);
            shieldBurst.vfxSecondaryColor = new Color(0.2f, 0.46f, 1f, 1f);
            shieldBurst.vfxScale = 1.4f;
            shieldBurst.vfxIntensity = 1.3f;
            shieldBurst.vfx = CreateOwnedVfxTuning(SkillVfxFamily.ShieldDome);
            var explosionPrefab = CreateOwnedGameObject("EasyExplosionPrefab");
            explosionPrefab.AddComponent<ParticleSystem>();
            shieldBurst.vfx.secondaryPrefab = explosionPrefab;
            var shieldSprite = CreateOwnedSprite("ShieldBurstSprite");

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "shieldEffectSprite", shieldSprite);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            player.AddBlock(40);

            Assert.That(manager.RequestUseSkill(shieldBurst, enemy), Is.True);

            var burstArt = viewObject.transform.Find("ShieldBurstGuardArt")?.GetComponent<SpriteRenderer>();
            Assert.That(burstArt, Is.Not.Null);
            Assert.That(burstArt.sprite, Is.EqualTo(shieldSprite));
            Assert.That(viewObject.transform.Find("ShieldBurstEasyExplosion"), Is.Not.Null);
            Assert.That(playerRenderer.transform.Find("ShieldBurstExpansionRing"), Is.Null);
            Assert.That(playerRenderer.transform.Find("ShieldBurstShardParticles"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("ShieldBurstImpactParticles"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("ShieldBurstImpactRing"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_ChargedAttackRelease_OnlyKeepsVerticalBeamWithoutChargedBeam()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 50, attackValue: 0);
            var charge = CreateSkill("gather-light", SkillType.Attack, cost: 0, power: 0);
            var projectilePrefab = CreateOwnedGameObject("GatherLightProjectilePrefab");
            var verticalBeamPrefab = CreateOwnedGameObject("GatherLightVerticalBeamPrefab");
            var wrongReleasePrefab = CreateOwnedGameObject("WrongShieldReleasePrefab");
            var attackSprite = CreateOwnedSprite("SkillVfx_AttackImpact");
            var bottomPivotPlayerSprite = CreateOwnedSprite("BottomPivotPlayerSprite", Vector2.zero);
            var centerPivotEnemySprite = CreateOwnedSprite("CenterPivotEnemySprite");
            projectilePrefab.AddComponent<CombatProjectileEffect>();
            wrongReleasePrefab.AddComponent<ParticleSystem>();
            charge.effectKind = SkillEffectKind.ChargeAttack;
            charge.chargedPower = 120;
            charge.vfx = CreateOwnedVfxTuning(SkillVfxFamily.LightBeam);
            charge.vfx.secondaryPrefab = verticalBeamPrefab;
            charge.vfx.radiusMultiplier = 3.54f;
            charge.vfxDefinition = new SkillVfxDefinition
            {
                cues = new[]
                {
                    new SkillVfxCue
                    {
                        trigger = SkillVfxTrigger.ChargeRelease,
                        prefab = wrongReleasePrefab,
                        spawnAt = new VfxEndpoint { actor = VfxActorRef.PrimaryTarget, socket = VfxSocket.Body },
                    },
                },
            };
            charge.activationEffect = new CombatEffectBinding
            {
                vfxPrefab = projectilePrefab,
            };
            playerData.startingSkills = new List<SkillSO> { charge };
            playerRenderer.sprite = bottomPivotPlayerSprite;
            enemyRenderer.sprite = centerPivotEnemySprite;
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 5;

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "attackEffectSprite", attackSprite);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();
            view.Initialize(bootstrap);

            Assert.That(manager.RequestUseSkillById("gather-light"), Is.True);
            Assert.That(viewObject.transform.Find("ChargedLightBeam"), Is.Null);
            Assert.That(viewObject.transform.Find("ChargedLightBeamGlow"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("ChargedLightAttackArt"), Is.Null);
            Assert.That(playerRenderer.transform.Find("ChargedLightAttackArt"), Is.Null);
            Assert.That(playerRenderer.transform.Find("GatherLightBuffParticles"), Is.Not.Null);
            Assert.That(GameObject.Find("GatherLightProjectilePrefab(Clone)"), Is.Null);

            manager.RequestEndPlayerTurn();

            // 수평 충전 빔 효과(빔 라인 + 글로우 + 머즐/임팩트 파티클 + 임팩트 아트)는 제거되었다.
            Assert.That(viewObject.transform.Find("ChargedLightBeam"), Is.Null);
            Assert.That(viewObject.transform.Find("ChargedLightBeamGlow"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("ChargedLightBeamImpactParticles"), Is.Null);
            Assert.That(playerRenderer.transform.Find("ChargedLightAttackArt"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("ChargedLightAttackArt"), Is.Null);

            // 랜턴 발사 트레일도 제거되고, 화면에 남는 빔은 버티컬 빔뿐이다.
            Assert.That(viewObject.transform.Find("LightBeamLanternLaunchTrail"), Is.Null);
            Assert.That(viewObject.transform.Find("WrongShieldReleasePrefab"), Is.Null);
            Assert.That(GameObject.Find("GatherLightProjectilePrefab(Clone)"), Is.Not.Null);
            var verticalBeam = viewObject.transform.Find("GatherLightVerticalBeam");
            Assert.That(verticalBeam, Is.Not.Null);
            Assert.That(verticalBeam.position.x, Is.EqualTo(enemyRenderer.transform.position.x).Within(0.001f));
            Assert.That(verticalBeam.position.y, Is.EqualTo(enemyRenderer.transform.position.y - 0.7f).Within(0.001f));
        }

        [Test]
        public void CombatWorldSpriteView_GatherLightPreview_ShowsChargeThenReleasedLightAttack()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var projectilePrefab = CreateOwnedGameObject("GatherLightProjectilePrefab");
            var verticalBeamPrefab = CreateOwnedGameObject("GatherLightVerticalBeamPrefab");
            var gatherLight = CreateSkill("gather-light", SkillType.Attack, cost: 0, power: 0);
            var centerPivotEnemySprite = CreateOwnedSprite("PreviewCenterPivotEnemySprite");
            projectilePrefab.AddComponent<CombatProjectileEffect>();
            enemyRenderer.sprite = centerPivotEnemySprite;
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            gatherLight.effectKind = SkillEffectKind.ChargeAttack;
            gatherLight.vfx = CreateOwnedVfxTuning(SkillVfxFamily.LightBeam);
            gatherLight.vfx.secondaryPrefab = verticalBeamPrefab;
            gatherLight.vfx.radiusMultiplier = 3.54f;
            gatherLight.activationEffect = new CombatEffectBinding
            {
                vfxPrefab = projectilePrefab,
            };

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            view.PreviewSkillEffect(gatherLight);

            Assert.That(CombatWorldSpriteView.GatherLightPreviewReleaseDelaySeconds, Is.EqualTo(2f).Within(0.001f));
            Assert.That(viewObject.transform.Find("ChargedLightBeam"), Is.Null);
            Assert.That(viewObject.transform.Find("ChargedLightBeamGlow"), Is.Null);
            // 수평 빔/랜턴 트레일 대신 홀리 파이어볼 투사체를 발사하고, 버티컬 빔이 남는다.
            Assert.That(viewObject.transform.Find("LightBeamLanternLaunchTrail"), Is.Null);
            Assert.That(GameObject.Find("GatherLightProjectilePrefab(Clone)"), Is.Not.Null);
            var verticalBeam = viewObject.transform.Find("GatherLightVerticalBeam");
            Assert.That(verticalBeam, Is.Not.Null);
            Assert.That(verticalBeam.position.x, Is.EqualTo(enemyRenderer.transform.position.x).Within(0.001f));
            Assert.That(verticalBeam.position.y, Is.EqualTo(enemyRenderer.transform.position.y - 0.7f).Within(0.001f));
            Assert.That(playerRenderer.transform.Find("GatherLightBuffParticles"), Is.Not.Null);
        }

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

            // 빈 vfxDefinition → 새 큐 경로 미진입 → 기존 절차 경로로 폴백(예외 없이 동작).
            Assert.That(skill.vfxDefinition.HasAnyCue, Is.False);
            Assert.DoesNotThrow(() => view.PreviewSkillEffect(skill));
        }

        [Test]
        public void CombatWorldSpriteView_VfxDefinitionActivateCue_SpawnsAuthoredPrefab()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var cuePrefab = CreateOwnedGameObject("AuthoredVfxPrefab");
            var skill = CreateSkill("authored", SkillType.Attack, cost: 0, power: 10);
            skill.vfxDefinition = new SkillVfxDefinition
            {
                cues = new[]
                {
                    new SkillVfxCue
                    {
                        trigger = SkillVfxTrigger.Activate,
                        prefab = cuePrefab,
                        spawnAt = new VfxEndpoint { actor = VfxActorRef.PrimaryTarget, socket = VfxSocket.Body },
                    },
                },
            };
            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            // 저작된 vfxDefinition 큐가 뷰를 통해 실제로 스폰됨(새 데이터 경로 end-to-end).
            view.PreviewSkillEffect(skill);

            Assert.That(viewObject.transform.Find("AuthoredVfxPrefab"), Is.Not.Null);
        }

        [Test]
        public void CombatWorldSpriteView_EnemySkillPresentation_MirrorsCasterAndTargetPlacements()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var casterCuePrefab = CreateOwnedGameObject("EnemyCasterCuePrefab");
            var targetCuePrefab = CreateOwnedGameObject("EnemyTargetCuePrefab");
            var skill = CreateSkill("enemy-authored", SkillType.Attack, cost: 0, power: 10);
            playerRenderer.transform.position = new Vector3(-1.5f, 0f, 0f);
            enemyRenderer.transform.position = new Vector3(1.25f, 0f, 0f);
            skill.vfxDefinition = new SkillVfxDefinition
            {
                cues = new[]
                {
                    new SkillVfxCue
                    {
                        trigger = SkillVfxTrigger.Activate,
                        prefab = casterCuePrefab,
                        spawnAt = new VfxEndpoint { actor = VfxActorRef.Caster, socket = VfxSocket.Body },
                    },
                    new SkillVfxCue
                    {
                        trigger = SkillVfxTrigger.Activate,
                        prefab = targetCuePrefab,
                        spawnAt = new VfxEndpoint { actor = VfxActorRef.PrimaryTarget, socket = VfxSocket.Body },
                    },
                },
            };
            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            var method = typeof(CombatWorldSpriteView).GetMethod(
                "PlayEnemySkillPresentationEffect",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var lifetime = (float)method.Invoke(view, new object[] { skill, true });

            var casterCue = viewObject.transform.Find("EnemyCasterCuePrefab");
            var targetCue = viewObject.transform.Find("EnemyTargetCuePrefab");
            Assert.That(lifetime, Is.GreaterThan(0f));
            Assert.That(casterCue, Is.Not.Null);
            Assert.That(targetCue, Is.Not.Null);
            Assert.That(casterCue.position.x, Is.EqualTo(enemyRenderer.transform.position.x).Within(0.001f));
            Assert.That(targetCue.position.x, Is.EqualTo(playerRenderer.transform.position.x).Within(0.001f));
        }

        [Test]
        public void CombatWorldSpriteView_EnemyBuffAuraMagicCircle_MirrorsTowardPlayer()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var magicCircleSprite = CreateOwnedSprite("EnemyBuffMagicCircleSprite");
            var buff = CreateSkill("enemy-buff", SkillType.Defense, cost: 0, power: 0);
            playerRenderer.transform.position = new Vector3(-1.5f, 0f, 0f);
            enemyRenderer.transform.position = new Vector3(1.25f, 0f, 0f);
            buff.vfxFamily = SkillVfxFamily.BuffAura;
            buff.vfxPrimaryColor = new Color(0.45f, 0.55f, 0.67f, 1f);

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "magicCircleEffectSprite", magicCircleSprite);

            var method = typeof(CombatWorldSpriteView).GetMethod(
                "PlayEnemySkillPresentationEffect",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var lifetime = (float)method.Invoke(view, new object[] { buff, false });

            var magicCircleArt = enemyRenderer.transform.Find("BuffAuraEffectArt")?.GetComponent<SpriteRenderer>();
            Assert.That(lifetime, Is.GreaterThan(0f));
            Assert.That(magicCircleArt, Is.Not.Null);
            Assert.That(magicCircleArt.transform.localPosition.x, Is.LessThan(0f));
            Assert.That(magicCircleArt.transform.position.x, Is.LessThan(enemyRenderer.transform.position.x));
            Assert.That((magicCircleArt.transform.localRotation * Vector3.right).x, Is.LessThan(0f));
            Assert.That(playerRenderer.transform.Find("BuffAuraEffectArt"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_TentacleStrikePreview_SpawnsAnimatorWhipAtCasterThenImpactAtTarget()
        {
            const string PrefabPath = "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_TentacleWhip.prefab";
            const string ControllerPath = "Assets/VFX Test/Effect_촉수_0.controller";
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRoot = CreateOwnedGameObject("player_all");
            var playerRenderer = CreateOwnedGameObject("Body").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var tentacle = CreateSkill("tentacle-strike", SkillType.Attack, cost: 0, power: 90);
            playerRenderer.sprite = CreateOwnedSprite("LayeredPlayerBodySprite");
            playerRenderer.sortingOrder = 6;
            playerRoot.transform.localPosition = new Vector3(-1f, 0f, 0f);
            playerRenderer.transform.SetParent(playerRoot.transform, false);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            tentacle.vfxFamily = SkillVfxFamily.TentacleWhip;
            tentacle.vfx = CreateOwnedVfxTuning(SkillVfxFamily.TentacleWhip);
            tentacle.vfx.primaryPrefab = prefab;
            tentacle.vfx.localOffset = new Vector3(-0.38f, 0.46f, 0f);
            tentacle.vfxScale = 1.2f;

            Assert.That(prefab, Is.Not.Null);

            SetPrivateField(view, "playerActorRoot", playerRoot.transform);
            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            view.PreviewSkillEffect(tentacle);

            var whipRoot = viewObject.transform.Find("TentacleStrikeWhip");
            Assert.That(whipRoot, Is.Not.Null);
            var animator = whipRoot.GetComponentInChildren<Animator>();
            var renderer = whipRoot.GetComponentInChildren<SpriteRenderer>();
            var prefabRenderer = prefab.GetComponentInChildren<SpriteRenderer>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController), Is.EqualTo(ControllerPath));
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.color, Is.EqualTo(prefabRenderer.color));
            Assert.That(whipRoot.position.x, Is.EqualTo(playerRenderer.bounds.center.x - 0.38f).Within(0.001f));
            Assert.That(whipRoot.position.y, Is.EqualTo(playerRenderer.bounds.center.y + 0.46f).Within(0.001f));
            Assert.That(Mathf.Abs(whipRoot.localScale.x), Is.EqualTo(Mathf.Abs(prefab.transform.localScale.x) * 1.2f).Within(0.001f));
            Assert.That(whipRoot.localScale.y, Is.EqualTo(prefab.transform.localScale.y * 1.2f).Within(0.001f));
            Assert.That(renderer.bounds.min.x, Is.LessThan(playerRenderer.bounds.center.x));
            Assert.That(renderer.bounds.max.x, Is.GreaterThan(playerRenderer.bounds.center.x));
            Assert.That(renderer.sortingOrder, Is.EqualTo(playerRenderer.sortingOrder + 12));
            Assert.That(whipRoot.GetComponent<LineRenderer>(), Is.Null);
            Assert.That(whipRoot.Find("TentacleStrikeHighlight"), Is.Null);
            Assert.That(whipRoot.Cast<Transform>().Any(child => child.name.StartsWith("TentacleSuctionCup")), Is.False);
            Assert.That(enemyRenderer.transform.Find("HeavyStrikeSpikedBurst"), Is.Not.Null);
        }

        [Test]
        public void SkillVfxTentacleWhipPrefab_UsesExpAnimatorTentacle()
        {
            const string PrefabPath = "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_TentacleWhip.prefab";
            const string ExpTentaclePath = "Assets/Art/Source/ExP 1/Effect_촉수.png";
            const string ControllerPath = "Assets/VFX Test/Effect_촉수_0.controller";
            const string AnimationPath = "Assets/VFX Test/Tentacle Attack.anim";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab, Is.Not.Null);
            var renderer = prefab.GetComponent<SpriteRenderer>();
            var animator = prefab.GetComponent<Animator>();
            var hasSpriteSkin = prefab.GetComponents<Component>()
                .Any(component => component != null && component.GetType().Name == "SpriteSkin");
            var hasProceduralTentacleComponent = prefab.GetComponents<Component>()
                .Any(component => component != null && component.GetType().Name == "TentacleBoneStrikeEffect");
            Assert.That(renderer, Is.Not.Null);
            Assert.That(hasSpriteSkin, Is.True);
            Assert.That(animator, Is.Not.Null);
            Assert.That(hasProceduralTentacleComponent, Is.False);
            Assert.That(AssetDatabase.GetAssetPath(renderer.sprite), Is.EqualTo(ExpTentaclePath));
            Assert.That(renderer.color, Is.EqualTo(Color.white));
            Assert.That(prefab.transform.localScale.x, Is.EqualTo(0.3234f).Within(0.0001f));
            Assert.That(prefab.transform.localScale.y, Is.EqualTo(0.3234f).Within(0.0001f));
            Assert.That(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController), Is.EqualTo(ControllerPath));
            Assert.That(
                animator.runtimeAnimatorController.animationClips.Select(AssetDatabase.GetAssetPath),
                Does.Contain(AnimationPath));
        }

        [Test]
        public void CombatWorldSpriteView_HeavyStrikePreview_SpawnsSpikedBurst()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var heavyStrike = CreateSkill("heavy-strike", SkillType.Attack, cost: 0, power: 80);
            var hitSprite = CreateOwnedSprite("HeavyStrikeHitImpactSprite");
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            heavyStrike.vfxFamily = SkillVfxFamily.SpikedBurst;
            heavyStrike.vfxPrimaryColor = new Color(1f, 0.72f, 0.08f, 1f);
            heavyStrike.vfxSecondaryColor = new Color(0.72f, 0.04f, 0.02f, 1f);
            heavyStrike.vfxScale = 1.3f;
            heavyStrike.vfxIntensity = 1.35f;

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "hitEffectSprite", hitSprite);

            view.PreviewSkillEffect(heavyStrike);

            var impactArt = enemyRenderer.transform.Find("HeavyStrikeSpikedBurstArt")?.GetComponent<SpriteRenderer>();
            Assert.That(impactArt, Is.Not.Null);
            Assert.That(impactArt.sprite, Is.EqualTo(hitSprite));
            Assert.That(impactArt.transform.localPosition.x, Is.LessThan(0f));
            Assert.That(impactArt.transform.localScale.x, Is.LessThan(3f));
            var burst = enemyRenderer.transform.Find("HeavyStrikeSpikedBurst");
            Assert.That(burst, Is.Not.Null);
            Assert.That(burst.localPosition.x, Is.LessThan(0f));
            var star = burst.Find("HeavyStrikeSpikedBurstStar")?.GetComponent<LineRenderer>();
            Assert.That(star, Is.Not.Null);
            Assert.That(star.positionCount, Is.GreaterThan(16));
            Assert.That(star.sortingOrder, Is.GreaterThan(enemyRenderer.sortingOrder));
            Assert.That(star.GetPosition(1).magnitude, Is.LessThan(0.36f));
            Assert.That(burst.Cast<Transform>().Count(child => child.name.StartsWith("HeavyStrikeSpikeRay")), Is.GreaterThanOrEqualTo(8));
            Assert.That(enemyRenderer.transform.Find("HeavyStrikeSpikedExplosionParticles")?.GetComponent<ParticleSystem>(), Is.Not.Null);
        }

        [Test]
        public void CombatWorldSpriteView_FireballPreview_LaunchesFireballProjectileAndImpactExplosion()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var fireballSkill = CreateSkill("fireball", SkillType.Attack, cost: 0, power: 50);
            var flamePackage = CreateOwnedVfxTuning(SkillVfxFamily.FlameBurst);
            var fireballPrefab = CreateOwnedGameObject("FireballPrefab");
            var explosionPrefab = CreateOwnedGameObject("LayeredExplosionPrefab");
            fireballPrefab.AddComponent<CombatProjectileEffect>();
            explosionPrefab.AddComponent<LayeredExplosionEffect>();
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            flamePackage.projectilePrefab = fireballPrefab;
            flamePackage.secondaryPrefab = explosionPrefab;
            fireballSkill.effectKind = SkillEffectKind.OverburnAttack;
            fireballSkill.vfxFamily = SkillVfxFamily.FlameBurst;
            fireballSkill.vfx = flamePackage;
            fireballSkill.vfxPrimaryColor = new Color(1f, 0.42f, 0.06f, 1f);
            fireballSkill.vfxSecondaryColor = new Color(0.74f, 0.12f, 0.02f, 1f);
            fireballSkill.vfxScale = 1.2f;
            fireballSkill.vfxIntensity = 1.45f;
            fireballSkill.activationEffect = new CombatEffectBinding
            {
                vfxPrefab = fireballPrefab,
                autoDestroySeconds = 1.55f,
            };

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            view.PreviewSkillEffect(fireballSkill);

            var fireball = Object.FindObjectsByType<CombatProjectileEffect>(FindObjectsInactive.Exclude)
                .SingleOrDefault(projectile => projectile.name == "FireballPrefab(Clone)");
            var explosion = enemyRenderer.transform.Find("LayeredExplosionPrefab(Clone)")
                ?.GetComponentInChildren<LayeredExplosionEffect>();
            Assert.That(fireball, Is.Not.Null);
            Assert.That(explosion, Is.Not.Null);
            Assert.That(fireball.transform.parent, Is.Null);
            Assert.That(fireball.transform.position.x, Is.GreaterThan(playerRenderer.transform.position.x));
            Assert.That(explosion.transform.position.x, Is.EqualTo(enemyRenderer.transform.position.x).Within(0.001f));
            Assert.That(explosion.transform.position.y, Is.GreaterThan(enemyRenderer.transform.position.y));
            Object.DestroyImmediate(fireball.gameObject);
        }

        [Test]
        public void SkillVfxPlayer_BodyPlacement_UsesChildRendererBounds()
        {
            var playerRoot = CreateOwnedGameObject("PlayerRoot");
            var body = CreateOwnedGameObject("Body").AddComponent<SpriteRenderer>();
            body.sprite = CreateOwnedSprite("BodySprite");
            body.transform.SetParent(playerRoot.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);

            var endpoint = new VfxEndpoint
            {
                actor = VfxActorRef.Caster,
                socket = VfxSocket.Body,
                localOffset = new Vector3(0.18f, 0.32f, 0f),
            };
            var position = SkillVfxPlayer.ResolveEndpointWorldPosition(
                endpoint,
                new SkillVfxContext(playerRoot.transform, null, SkillVfxTrigger.Activate));

            Assert.That(position.y, Is.EqualTo(1.32f).Within(0.001f));
            Assert.That(position.y, Is.GreaterThan(playerRoot.transform.position.y + 0.9f));
        }

        [Test]
        public void CombatWorldSpriteView_BurstFireballPreview_LaunchesFireballAndImpactExplosion()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var burstFireball = CreateSkill("burst-fireball", SkillType.Attack, cost: 0, power: 70);
            var flamePackage = CreateOwnedVfxTuning(SkillVfxFamily.FlameBurst);
            var fireballPrefab = CreateOwnedGameObject("FireballPrefab");
            var explosionPrefab = CreateOwnedGameObject("LayeredExplosionPrefab");
            fireballPrefab.AddComponent<CombatProjectileEffect>();
            explosionPrefab.AddComponent<LayeredExplosionEffect>();
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            flamePackage.projectilePrefab = fireballPrefab;
            flamePackage.secondaryPrefab = explosionPrefab;
            burstFireball.effectKind = SkillEffectKind.OverburnAttack;
            burstFireball.vfxFamily = SkillVfxFamily.FlameBurst;
            burstFireball.vfx = flamePackage;
            burstFireball.activationEffect = new CombatEffectBinding
            {
                vfxPrefab = fireballPrefab,
                autoDestroySeconds = 1.55f,
            };

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            view.PreviewSkillEffect(burstFireball);

            var fireball = Object.FindObjectsByType<CombatProjectileEffect>(FindObjectsInactive.Exclude)
                .SingleOrDefault(projectile => projectile.name == "FireballPrefab(Clone)");
            var explosion = enemyRenderer.transform.Find("LayeredExplosionPrefab(Clone)")
                ?.GetComponentInChildren<LayeredExplosionEffect>();
            Assert.That(fireball, Is.Not.Null);
            Assert.That(explosion, Is.Not.Null);
            Assert.That(explosion.transform.position.x, Is.EqualTo(enemyRenderer.transform.position.x).Within(0.001f));
            Assert.That(explosion.transform.position.y, Is.GreaterThan(enemyRenderer.transform.position.y));
            Object.DestroyImmediate(fireball.gameObject);
        }

        [Test]
        public void CombatWorldSpriteView_DarkShacklePreview_LaunchesChainAttackArtFromLanternThenBindsTarget()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var darkShackle = CreateSkill("dark-shackle", SkillType.Attack, cost: 0, power: 40);
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            darkShackle.vfxFamily = SkillVfxFamily.DarkChainBurst;
            darkShackle.vfxPrimaryColor = new Color(0.02f, 0.02f, 0.04f, 1f);
            darkShackle.vfxSecondaryColor = new Color(0.5f, 0.04f, 0.18f, 1f);
            darkShackle.vfxScale = 1.2f;
            darkShackle.vfxIntensity = 1.2f;
            var chainAttackSprite = CreateOwnedSprite("ChainAttackEffectSprite");
            var boundChainsSprite = CreateOwnedSprite("BoundChainsEffectSprite");
            var chainAttackPrefab = CreateOwnedSpritePrefab(
                "SharedChainAttackPrefab",
                chainAttackSprite,
                "SharedChainAttackPrefabMarker");
            var boundChainsPrefab = CreateOwnedSpritePrefab(
                "SharedBoundChainsPrefab",
                boundChainsSprite,
                "SharedBoundChainsPrefabMarker");
            var profile = ScriptableObject.CreateInstance<CombatWorldVfxProfileSO>();
            profile.chainAttackEffectSprite = chainAttackSprite;
            profile.boundChainsEffectSprite = boundChainsSprite;
            profile.chainAttackEffectPrefab = chainAttackPrefab;
            profile.boundChainsEffectPrefab = boundChainsPrefab;
            profile.designTimeBindings = new[]
            {
                new SkillVfxDesignTimeBinding
                {
                    family = SkillVfxFamily.DarkChainBurst,
                    sprite = chainAttackSprite,
                    prefab = chainAttackPrefab,
                    localOffset = Vector3.zero,
                    radiusMultiplier = 0.95f,
                    sortingOffset = 15,
                    tintWhiteBlend = 0.14f,
                    alpha = 0.78f,
                    rotationDegrees = -4f,
                },
            };
            ownedObjects.Add(profile);

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "worldVfxProfile", profile);

            view.PreviewSkillEffect(darkShackle);

            var chainRoot = viewObject.transform.Find("DarkShackleChainLaunch");
            Assert.That(chainRoot, Is.Not.Null);
            Assert.That(chainRoot.Find("DarkShackleChainLine"), Is.Null);
            Assert.That(chainRoot.Find("DarkShackleChainHead"), Is.Null);
            Assert.That(chainRoot.Cast<Transform>().Count(child => child.name.StartsWith("DarkShackleChainLink")), Is.Zero);
            Assert.That(enemyRenderer.transform.Find("DarkShackleImpactExplosion"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("DarkShackleImpactRing"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("DarkShackleImpactSparks"), Is.Null);
            var chainProjectile = chainRoot.Find("DarkShackleChainProjectileVfx");
            Assert.That(chainProjectile, Is.Not.Null);
            var chainAttackArt = chainProjectile.GetComponent<SpriteRenderer>();
            Assert.That(chainAttackArt, Is.Not.Null);
            Assert.That(chainAttackArt.sprite, Is.EqualTo(chainAttackSprite));
            AssertColorApproximately(chainAttackArt.color, Color.white);
            Assert.That(chainAttackArt.transform.position.x, Is.GreaterThan(playerRenderer.transform.position.x));
            Assert.That(chainAttackArt.transform.localScale.x, Is.GreaterThan(1f));
            Assert.That(enemyRenderer.transform.Find("DarkShackleChainAttackArt"), Is.Null);
            Assert.That(chainRoot.Find("DarkShackleChainProjectileVfx/SharedChainAttackPrefabMarker"), Is.Not.Null);
            var impactDust = enemyRenderer.transform.Find("DarkShackleImpactDust")?.GetComponent<ParticleSystem>();
            Assert.That(impactDust, Is.Not.Null);
            AssertColorApproximately(impactDust.main.startColor.color, Color.white);
            AssertColorApproximately(
                ResolveMaterialColor(impactDust.GetComponent<ParticleSystemRenderer>().sharedMaterial),
                Color.white);
            Assert.That(enemyRenderer.transform.Cast<Transform>().Count(child => child.name == "DarkShackleBoundChainsArt"), Is.EqualTo(1));
            var boundChainsArt = enemyRenderer.transform.Find("DarkShackleBoundChainsArt")?.GetComponent<SpriteRenderer>();
            Assert.That(boundChainsArt, Is.Not.Null);
            Assert.That(boundChainsArt.sprite, Is.EqualTo(boundChainsSprite));
            AssertColorApproximately(boundChainsArt.color, Color.white);
            Assert.That(boundChainsArt.transform.localScale.x, Is.GreaterThan(chainAttackArt.transform.localScale.x));
            Assert.That(boundChainsArt.transform.localScale.x, Is.GreaterThan(2.2f));
            Assert.That(boundChainsArt.transform.localScale.x, Is.LessThan(3f));
            Assert.That(enemyRenderer.transform.Find("DarkShackleBoundChainsArt/SharedBoundChainsPrefabMarker"), Is.Not.Null);
        }

        [Test]
        public void CombatWorldSpriteView_FlameBurstPreview_SpawnsRisingFire()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var overburn = CreateSkill("overburn", SkillType.Attack, cost: 0, power: 50);
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            overburn.vfxFamily = SkillVfxFamily.FlameBurst;
            overburn.vfxPrimaryColor = new Color(1f, 0.42f, 0.08f, 1f);
            overburn.vfxSecondaryColor = new Color(0.45f, 0.04f, 0.02f, 1f);
            overburn.vfxScale = 1.2f;
            overburn.vfxIntensity = 1.45f;
            var flameSprite = CreateOwnedSprite("FlameBurstEffectSprite");
            var flamePrefab = CreateOwnedSpritePrefab(
                "FlameBurstPrefab",
                flameSprite,
                "FlameBurstPrefabMarker");
            var explosionPrefab = CreateOwnedGameObject("LayeredExplosionPrefab");
            explosionPrefab.AddComponent<LayeredExplosionEffect>();
            var flamePackage = CreateOwnedVfxTuning(SkillVfxFamily.FlameBurst);
            flamePackage.primarySprite = flameSprite;
            flamePackage.primaryPrefab = flamePrefab;
            flamePackage.secondaryPrefab = explosionPrefab;
            flamePackage.localOffset = new Vector3(0f, -0.42f, 0f);
            overburn.vfx = flamePackage;

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            view.PreviewSkillEffect(overburn);

            var flameArt = enemyRenderer.transform.Find("FlameBurstArt")?.GetComponent<SpriteRenderer>();
            Assert.That(flameArt, Is.Not.Null);
            Assert.That(flameArt.sprite, Is.EqualTo(flameSprite));
            Assert.That(flameArt.transform.localPosition.y, Is.LessThan(0f));
            Assert.That(enemyRenderer.transform.Find("FlameBurstArt/FlameBurstPrefabMarker"), Is.Not.Null);

            var tongues = enemyRenderer.transform.Find("FlameBurstFlameTongues");
            Assert.That(tongues, Is.Not.Null);
            Assert.That(tongues.localPosition.y, Is.EqualTo(-0.42f).Within(0.001f));
            Assert.That(tongues.Cast<Transform>().Count(child => child.name.StartsWith("FlameBurstFlameTongue")), Is.GreaterThanOrEqualTo(5));
            var tongue = tongues.Find("FlameBurstFlameTongue1")?.GetComponent<LineRenderer>();
            Assert.That(tongue, Is.Not.Null);
            Assert.That(tongue.positionCount, Is.GreaterThan(8));
            Assert.That(tongue.sortingOrder, Is.GreaterThan(enemyRenderer.sortingOrder));

            var flame = enemyRenderer.transform.Find("FlameBurstFlameParticles")?.GetComponent<ParticleSystem>();
            Assert.That(flame, Is.Not.Null);
            Assert.That(flame.transform.localPosition.y, Is.LessThan(0f));
            Assert.That(flame.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Cone));
            Assert.That(flame.emission.rateOverTime.constant, Is.GreaterThan(0f));
            Assert.That(flame.velocityOverLifetime.enabled, Is.True);
            Assert.That(enemyRenderer.transform.Find("FlameBurstEmbers")?.GetComponent<ParticleSystem>(), Is.Not.Null);
            Assert.That(enemyRenderer.transform.Find("FlameBurstSmoke")?.GetComponent<ParticleSystem>(), Is.Not.Null);
            var explosion = enemyRenderer.transform.Find("LayeredExplosionPrefab(Clone)")
                ?.GetComponentInChildren<LayeredExplosionEffect>();
            Assert.That(explosion, Is.Not.Null);
            Assert.That(explosion.transform.position.x, Is.EqualTo(enemyRenderer.transform.position.x).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator CombatWorldSpriteView_PlayerProjectileSkill_DelaysActivationSfx()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var attack = CreateSkill("attack", SkillType.Attack, cost: 0, power: 1);
            var activationClip = AudioClip.Create("DelayedSkillActivation", 512, 1, 44100, false);
            var projectilePrefab = CreateOwnedGameObject("ProjectilePrefab");
            projectilePrefab.AddComponent<CombatProjectileEffect>();
            ownedObjects.Add(activationClip);
            attack.activationEffect = new CombatEffectBinding
            {
                sfxClip = activationClip,
                vfxPrefab = projectilePrefab,
                minPitch = 0.8f,
                maxPitch = 0.8f,
                sfxDelaySeconds = 0.15f,
            };

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            Assert.That(manager.RequestUseSkill(attack, enemy), Is.True);
            Assert.That(viewObject.transform.Find("CombatEffectAudio"), Is.Null);

            yield return new WaitForSecondsRealtime(0.2f);

            Assert.That(viewObject.transform.Find("CombatEffectAudio"), Is.Not.Null);
        }

        [Test]
        public void CombatWorldSpriteView_EnemyDefenseIntent_PlaysDefendEffectFromEnemySo()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var defendClip = AudioClip.Create("EnemyDefend", 512, 1, 44100, false);
            ownedObjects.Add(defendClip);
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Defense,
                    value = 5,
                },
            };
            enemyData.actionEffects = new List<CombatantActionEffectBinding>
            {
                new()
                {
                    actionId = CombatActionIds.Defend,
                    effect = new CombatEffectBinding
                    {
                        sfxClip = defendClip,
                    },
                },
            };

            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            manager.RequestEndPlayerTurn();

            Assert.That(enemy.Block, Is.EqualTo(5));
            Assert.That(viewObject.transform.Find("CombatEffectAudio"), Is.Not.Null);
        }

        [Test]
        public void CombatWorldSpriteView_EnemyDebuffIntent_PlaysAttackEffectSfxFromEnemySo()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            var playerData = CreatePlayerData(maxHp: 20, attackPower: 2);
            var enemyData = CreateEnemyData(maxHp: 10, attackValue: 0);
            var attackClip = AudioClip.Create("EnemyAttackVoice", 512, 1, 44100, false);
            ownedObjects.Add(attackClip);
            enemyData.intentPattern = new List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Debuff,
                    debuffType = DebuffType.Fear,
                    value = 1,
                },
            };
            enemyData.actionEffects = new List<CombatantActionEffectBinding>
            {
                new()
                {
                    actionId = CombatActionIds.Attack,
                    effect = new CombatEffectBinding
                    {
                        sfxClip = attackClip,
                        minPitch = 0.8f,
                        maxPitch = 0.8f,
                    },
                },
            };

            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(bootstrap, "combatManager", manager);

            manager.SetCombatants(player, new[] { enemy });
            view.Initialize(bootstrap);
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            manager.RequestEndPlayerTurn();

            var audio = viewObject.transform.Find("CombatEffectAudio")?.GetComponent<AudioSource>();
            Assert.That(audio, Is.Not.Null);
            Assert.That(audio.pitch, Is.EqualTo(0.8f).Within(0.001f));
        }

        [Test]
        public void BattleScene_CombatEventAudioPlayer_UsesEventAudioProfileAsset()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");

            var audioPlayer = Object.FindAnyObjectByType<PrototypeCombatEventAudioPlayer>(FindObjectsInactive.Include);

            Assert.That(audioPlayer, Is.Not.Null);
            Assert.That(audioPlayer.GetComponent<CombatUiView>(), Is.Null);

            var serializedPlayer = new SerializedObject(audioPlayer);
            Assert.That(serializedPlayer.FindProperty("audioSource").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedPlayer.FindProperty("eventAudioProfile").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedPlayer.FindProperty("victoryClip"), Is.Null);
            Assert.That(serializedPlayer.FindProperty("defeatClip"), Is.Null);
            Assert.That(serializedPlayer.FindProperty("restRewardClip"), Is.Null);
            Assert.That(serializedPlayer.FindProperty("enhanceRewardClip"), Is.Null);
            Assert.That(serializedPlayer.FindProperty("volumeScale"), Is.Null);
        }

        [Test]
        public void BattleScene_HasBattleSceneBinderForLoadingCompletion()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");

            var binder = Object.FindAnyObjectByType<BattleSceneBinder>(FindObjectsInactive.Include);
            var bootstrap = Object.FindAnyObjectByType<PrototypeCombatBootstrap>(FindObjectsInactive.Include);

            Assert.That(binder, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);
        }

        [Test]
        public void BattleScene_DoesNotContainPrototypeVfxTextPlaceholder()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");

            Assert.That(GameObject.Find("PrototypeVfxText"), Is.Null);
        }

        [Test]
        public void AttackEffectShowcaseScene_IncludesEveryAuthoredSkillVfxSlot()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/AttackEffectShowcase.unity");

            var expectedSkillIds = AssetDatabase
                .FindAssets("t:SkillSO", new[] { "Assets/Data/Skills" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SkillSO>)
                .Where(skill => skill != null && skill.ResolveVfxFamily() != SkillVfxFamily.None)
                .Select(skill => skill.skillId)
                .OrderBy(skillId => skillId)
                .ToArray();
            var slots = Object.FindObjectsByType<AttackEffectShowcaseSlot>(
                FindObjectsInactive.Include);
            var slotSkillIds = slots
                .Select(slot => slot.Skill != null ? slot.Skill.skillId : null)
                .Where(skillId => !string.IsNullOrEmpty(skillId))
                .OrderBy(skillId => skillId)
                .ToArray();
            var worldVfxProfile = AssetDatabase.LoadAssetAtPath<CombatWorldVfxProfileSO>(
                "Assets/Art/Effects/SkillVFX/Resources/PrototypeCombatWorldVfxProfile.asset");
            var worldViews = Object.FindObjectsByType<CombatWorldSpriteView>(
                FindObjectsInactive.Include);
            var groups = Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include)
                .Count(item => item.name.StartsWith("Group_", System.StringComparison.Ordinal));
            var misleadingEditableVfxGroups = Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include)
                .Count(item =>
                    item.name.StartsWith("EditableVFX__", System.StringComparison.Ordinal) ||
                    item.name.StartsWith("_EditableVfxPrefabRef__", System.StringComparison.Ordinal));

            Assert.That(slots.Length, Is.EqualTo(expectedSkillIds.Length));
            Assert.That(slotSkillIds, Is.EquivalentTo(expectedSkillIds));
            Assert.That(worldVfxProfile, Is.Not.Null);
            Assert.That(worldViews.Length, Is.EqualTo(expectedSkillIds.Length));
            foreach (var worldView in worldViews)
            {
                var serialized = new SerializedObject(worldView);
                Assert.That(
                    serialized.FindProperty("worldVfxProfile")?.objectReferenceValue,
                    Is.EqualTo(worldVfxProfile),
                    worldView.name);
            }

            Assert.That(groups, Is.EqualTo(12));
            Assert.That(misleadingEditableVfxGroups, Is.Zero);
            foreach (var slot in slots)
            {
                var serialized = new SerializedObject(slot);
                var runtimePrefabs = serialized.FindProperty("runtimeVfxPrefabs");
                Assert.That(runtimePrefabs, Is.Not.Null, slot.name);
                Assert.That(runtimePrefabs.arraySize, Is.GreaterThan(0), slot.name);
                for (var i = 0; i < runtimePrefabs.arraySize; i++)
                {
                    var prefab = runtimePrefabs.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.That(prefab, Is.Not.Null, $"{slot.name} runtime prefab {i}");
                    Assert.That(AssetDatabase.GetAssetPath(prefab), Does.StartWith("Assets/"), prefab.name);
                }
            }

            Assert.That(slotSkillIds, Does.Contain("light-guard"));
            Assert.That(slotSkillIds, Does.Contain("dark-shackle"));
        }

        [Test]
        public void AttackEffectShowcaseScene_GroupsSlotsByCurrentVisualCategory()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/AttackEffectShowcase.unity");

            var root = GameObject.Find("AttackEffectShowcaseRoot")?.transform;
            var expectedFamiliesByGroup = new Dictionary<string, SkillVfxFamily[]>
            {
                ["Group_10_slash"] = new[] { SkillVfxFamily.SlashArc },
                ["Group_20_flame"] = new[] { SkillVfxFamily.FlameBurst },
                ["Group_30_light"] = new[] { SkillVfxFamily.LightProjectile, SkillVfxFamily.LightBeam, SkillVfxFamily.SupportFire },
                ["Group_40_shield"] = new[] { SkillVfxFamily.ShieldDome },
                ["Group_50_impact"] = new[] { SkillVfxFamily.ImpactBurst, SkillVfxFamily.SpikedBurst },
                ["Group_60_buff"] = new[] { SkillVfxFamily.BuffAura },
                ["Group_65_counter"] = new[] { SkillVfxFamily.CounterReady },
                ["Group_70_debuff"] = new[] { SkillVfxFamily.DebuffWave },
                ["Group_80_drain"] = new[] { SkillVfxFamily.DrainTether },
                ["Group_90_darkness"] = new[] { SkillVfxFamily.BoardDisturb },
                ["Group_100_tentacle"] = new[] { SkillVfxFamily.TentacleWhip },
                ["Group_120_chain"] = new[] { SkillVfxFamily.DarkChainBurst },
            };
            var slotsByGroup = expectedFamiliesByGroup.Keys.ToDictionary(
                groupName => groupName,
                _ => new List<AttackEffectShowcaseSlot>());
            string currentGroup = null;

            Assert.That(root, Is.Not.Null);
            foreach (Transform child in root)
            {
                if (child.name.StartsWith("Group_", System.StringComparison.Ordinal))
                {
                    currentGroup = child.name;
                    Assert.That(expectedFamiliesByGroup.ContainsKey(currentGroup), Is.True, currentGroup);
                    continue;
                }

                var slot = child.GetComponent<AttackEffectShowcaseSlot>();
                if (slot == null)
                {
                    continue;
                }

                Assert.That(currentGroup, Is.Not.Null, slot.name);
                Assert.That(slot.Skill, Is.Not.Null, slot.name);
                var family = slot.Skill.ResolveVfxFamily();
                Assert.That(expectedFamiliesByGroup[currentGroup], Does.Contain(family), $"{slot.Skill.skillId} in {currentGroup}");
                slotsByGroup[currentGroup].Add(slot);
            }

            foreach (var pair in slotsByGroup)
            {
                Assert.That(pair.Value, Is.Not.Empty, pair.Key);
            }

            Assert.That(
                slotsByGroup["Group_10_slash"].Select(slot => slot.Skill.skillId).ToArray(),
                Is.EqualTo(new[] { "flow-strike", "quick-stab" }));
            Assert.That(
                slotsByGroup["Group_20_flame"].Select(slot => slot.Skill.skillId).ToArray(),
                Is.EqualTo(new[] { "fireball", "burst-fireball", "burn-out", "overburn", "reckless-blow" }));
            Assert.That(
                slotsByGroup["Group_80_drain"].Select(slot => slot.Skill.skillId).ToArray(),
                Is.EqualTo(new[] { "bioluminescence", "life-drain", "poison-coat" }));
        }

        [Test]
        public void AttackEffectShowcaseScene_UsesCompactPreviewActorLayout()
        {
            var previewPlayerScale = new Vector3(0.52f, 0.52f, 0.52f);
            var previewEnemyScale = new Vector3(0.34f, 0.34f, 0.34f);
            var defaultPlayerLocalPosition = new Vector3(-1.08f, -0.34f, 4f);
            var shieldPlayerLocalPosition = new Vector3(-1.08f, -0.48f, 4f);
            var defaultEnemyLocalPosition = new Vector3(1.12f, 0.2f, 4f);
            var shieldEnemyLocalPosition = new Vector3(1.12f, 0.04f, 4f);

            EditorSceneManager.OpenScene("Assets/Scenes/AttackEffectShowcase.unity");

            var worldViews = Object.FindObjectsByType<CombatWorldSpriteView>(
                FindObjectsInactive.Include);
            var virtualAllies = Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include)
                .Count(item => item.name == "VirtualAlly");

            Assert.That(worldViews.Length, Is.GreaterThan(0));
            Assert.That(virtualAllies, Is.Zero);
            foreach (var worldView in worldViews)
            {
                var slot = worldView.GetComponent<AttackEffectShowcaseSlot>();
                var serialized = new SerializedObject(worldView);
                var playerRoot = serialized
                    .FindProperty("playerActorRoot")
                    .objectReferenceValue as Transform;
                var enemyRenderer = serialized
                    .FindProperty("enemyRenderer")
                    .objectReferenceValue as SpriteRenderer;

                Assert.That(playerRoot, Is.Not.Null, worldView.name);
                Assert.That(playerRoot.name, Is.EqualTo("player_all"), worldView.name);
                Assert.That(enemyRenderer, Is.Not.Null, worldView.name);
                Assert.That(slot, Is.Not.Null, worldView.name);

                var isShieldSlot = slot.Skill != null &&
                    slot.Skill.ResolveVfxFamily() == SkillVfxFamily.ShieldDome;
                AssertVector3Approximately(
                    playerRoot.localPosition,
                    isShieldSlot ? shieldPlayerLocalPosition : defaultPlayerLocalPosition,
                    $"{worldView.name} player position");
                AssertVector3Approximately(
                    playerRoot.localScale,
                    previewPlayerScale,
                    $"{worldView.name} player scale");
                AssertVector3Approximately(
                    enemyRenderer.transform.localPosition,
                    isShieldSlot ? shieldEnemyLocalPosition : defaultEnemyLocalPosition,
                    $"{worldView.name} enemy position");
                AssertVector3Approximately(
                    enemyRenderer.transform.localScale,
                    previewEnemyScale,
                    $"{worldView.name} enemy scale");
            }
        }

        [Test]
        public void PrototypeCombatEventAudioProfile_ContainsResultAndRewardClips()
        {
            var profile = AssetDatabase.LoadAssetAtPath<PrototypeCombatEventAudioProfileSO>(
                "Assets/Data/Presentation/PrototypeCombatEventAudioProfile.asset");

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.Resolve(PrototypeCombatEventSoundCue.Victory).sfxClip, Is.Not.Null);
            Assert.That(profile.Resolve(PrototypeCombatEventSoundCue.Defeat).sfxClip, Is.Not.Null);
            Assert.That(profile.Resolve(PrototypeCombatEventSoundCue.RewardRest).sfxClip, Is.Not.Null);
            Assert.That(profile.Resolve(PrototypeCombatEventSoundCue.RewardEnhance).sfxClip, Is.Not.Null);
        }

        [Test]
        public void PrototypeEnemyAssets_HaveAppearActionEffectClips()
        {
            var enemyGuids = AssetDatabase.FindAssets("t:EnemySO", new[] { "Assets/Data/Enemies" });

            Assert.That(enemyGuids.Length, Is.GreaterThanOrEqualTo(12));
            foreach (var guid in enemyGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enemy = AssetDatabase.LoadAssetAtPath<EnemySO>(path);
                var effect = enemy != null ? enemy.FindActionEffect(CombatActionIds.Appear) : null;

                Assert.That(effect, Is.Not.Null, path);
                Assert.That(effect.sfxClip, Is.Not.Null, path);
            }
        }

        [Test]
        public void PrototypeEnemyAssets_HaveDefendActionEffectClips()
        {
            var enemyGuids = AssetDatabase.FindAssets("t:EnemySO", new[] { "Assets/Data/Enemies" });

            Assert.That(enemyGuids.Length, Is.GreaterThanOrEqualTo(12));
            foreach (var guid in enemyGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enemy = AssetDatabase.LoadAssetAtPath<EnemySO>(path);
                var effect = enemy != null ? enemy.FindActionEffect(CombatActionIds.Defend) : null;

                Assert.That(effect, Is.Not.Null, path);
                Assert.That(effect.sfxClip, Is.Not.Null, path);
            }
        }

        [Test]
        public void PrototypeEnemyAssets_HaveAttackAndHitActionEffectClips()
        {
            var enemyGuids = AssetDatabase.FindAssets("t:EnemySO", new[] { "Assets/Data/Enemies" });

            Assert.That(enemyGuids.Length, Is.GreaterThanOrEqualTo(12));
            foreach (var guid in enemyGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enemy = AssetDatabase.LoadAssetAtPath<EnemySO>(path);
                var appearEffect = enemy != null ? enemy.FindActionEffect(CombatActionIds.Appear) : null;
                var attackEffect = enemy != null ? enemy.FindActionEffect(CombatActionIds.Attack) : null;
                var hitEffect = enemy != null ? enemy.FindActionEffect(CombatActionIds.Hit) : null;

                Assert.That(appearEffect, Is.Not.Null, path);
                Assert.That(attackEffect, Is.Not.Null, path);
                Assert.That(attackEffect.sfxClip, Is.Not.Null, path);
                Assert.That(hitEffect, Is.Not.Null, path);
                Assert.That(hitEffect.sfxClip, Is.Not.Null, path);
                Assert.That(
                    AssetDatabase.GetAssetPath(attackEffect.sfxClip),
                    Does.StartWith("Assets/Sounds/MonsterAttackSfx/"),
                    path);
                Assert.That(attackEffect.EffectiveMinPitch, Is.EqualTo(appearEffect.EffectiveMinPitch).Within(0.0001f), path);
                Assert.That(attackEffect.EffectiveMaxPitch, Is.EqualTo(appearEffect.EffectiveMaxPitch).Within(0.0001f), path);
                Assert.That(
                    AssetDatabase.GetAssetPath(hitEffect.sfxClip),
                    Does.Match(@"^Assets/Sounds/GameplaySfx/enemy_hit_0[1-3]\.mp3$"),
                    path);
            }
        }

        [Test]
        public void PrototypeEnemyAssets_MonsterOneUsersHaveBoostedVoiceActionVolumes()
        {
            var expectedVolumes = new Dictionary<string, (float appear, float attack, float hit)>
            {
                ["05.asset"] = (1.5f, 1.38f, 1.17f),
                ["08.asset"] = (1.23f, 1.1316f, 0.9594f),
                ["12.asset"] = (1.23f, 1.1316f, 0.9594f),
            };

            foreach (var pair in expectedVolumes)
            {
                var path = $"Assets/Data/Enemies/{pair.Key}";
                var enemy = AssetDatabase.LoadAssetAtPath<EnemySO>(path);

                Assert.That(enemy, Is.Not.Null, path);
                Assert.That(enemy.FindActionEffect(CombatActionIds.Appear).EffectiveVolumeScale, Is.EqualTo(pair.Value.appear).Within(0.0001f), path);
                Assert.That(enemy.FindActionEffect(CombatActionIds.Attack).EffectiveVolumeScale, Is.EqualTo(pair.Value.attack).Within(0.0001f), path);
                Assert.That(enemy.FindActionEffect(CombatActionIds.Hit).EffectiveVolumeScale, Is.EqualTo(pair.Value.hit).Within(0.0001f), path);
            }
        }

        [Test]
        public void PrototypeBoardTileEffects_UsesMergeClipAndGranderTuningForLargerTiles()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BoardTileEffectProfileSO>(
                "Assets/Data/Presentation/PrototypeBoardTileEffects.asset");

            Assert.That(profile, Is.Not.Null);

            var smallMerge = profile.ResolveMergeEffect(2);
            var largeMerge = profile.ResolveMergeEffect(2048);

            Assert.That(smallMerge, Is.Not.Null);
            Assert.That(largeMerge, Is.Not.Null);
            Assert.That(smallMerge.sfxClip, Is.Not.Null);
            Assert.That(largeMerge.sfxClip, Is.EqualTo(smallMerge.sfxClip));
            Assert.That(largeMerge.EffectiveVolumeScale, Is.GreaterThan(smallMerge.EffectiveVolumeScale));
            Assert.That(largeMerge.EffectiveMaxPitch, Is.LessThan(smallMerge.EffectiveMaxPitch));
        }

        [Test]
        public void PrototypeBoardTileEffects_DefinesEverySupportedMergeTile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BoardTileEffectProfileSO>(
                "Assets/Data/Presentation/PrototypeBoardTileEffects.asset");

            Assert.That(profile, Is.Not.Null);
            foreach (var tileValue in new[] { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048 })
            {
                var effect = profile.ResolveMergeEffect(tileValue);

                Assert.That(effect, Is.Not.Null, tileValue.ToString());
                Assert.That(effect.sfxClip, Is.Not.Null, tileValue.ToString());
            }
        }

        [Test]
        public void PrototypeCombatWorldVfxProfile_UsesDesignTimeParticlePrefabs()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CombatWorldVfxProfileSO>(
                "Assets/Art/Effects/SkillVFX/Resources/PrototypeCombatWorldVfxProfile.asset");

            Assert.That(profile, Is.Not.Null);
            AssertDesignTimeParticlePrefab(profile.defaultSkillParticlePrefab, "default skill particle prefab");
            AssertDesignTimeParticlePrefab(profile.swirlSkillParticlePrefab, "swirl skill particle prefab");
            AssertDesignTimeParticlePrefab(profile.shieldImpactEffect.particlePrefab, "shield impact particle prefab");
            AssertDesignTimeParticlePrefab(profile.fearDebuffCastEffect.particlePrefab, "fear debuff particle prefab");
            AssertDesignTimeParticlePrefab(profile.darknessDebuffCastEffect.particlePrefab, "darkness debuff particle prefab");
            AssertDesignTimeParticlePrefab(profile.ResolveParticlePrefab("FlameBurstEmbers", false), "FlameBurstEmbers");
            AssertDesignTimeParticlePrefab(profile.ResolveParticlePrefab("ChargedLightBeamImpactParticles", true), "ChargedLightBeamImpactParticles");
        }

        [Test]
        public void PrototypeSkillAssets_HaveReusableVfxMetadata()
        {
            var skillGuids = AssetDatabase.FindAssets("t:SkillSO", new[] { "Assets/Data/Skills" });

            Assert.That(skillGuids.Length, Is.EqualTo(41));
            foreach (var guid in skillGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var skill = AssetDatabase.LoadAssetAtPath<SkillSO>(path);

                Assert.That(skill, Is.Not.Null, path);
                var resolvedFamily = skill.ResolveVfxFamily();
                Assert.That(skill.vfxPackage, Is.Null, path);
                Assert.That(skill.vfx, Is.Not.Null, path);
                Assert.That(skill.vfx.HasAnySetting, Is.True, path);
                Assert.That(resolvedFamily, Is.Not.EqualTo(SkillVfxFamily.None), path);
                Assert.That(skill.vfx.family, Is.EqualTo(resolvedFamily), path);
                if (skill.vfx.particlePrefab != null)
                {
                    AssertDesignTimeParticlePrefab(skill.vfx.particlePrefab, path);
                }

                Assert.That(skill.vfxScale, Is.GreaterThan(0f), path);
                Assert.That(skill.vfxIntensity, Is.GreaterThan(0f), path);
                Assert.That(skill.vfxRepeatCount, Is.GreaterThanOrEqualTo(1), path);
                Assert.That(skill.activationEffect, Is.Not.Null, path);
                Assert.That(skill.activationEffect.sfxClip, Is.Not.Null, path);
                Assert.That(
                    AssetDatabase.GetAssetPath(skill.activationEffect.sfxClip),
                    Does.Match(@"^Assets/Sounds/GameplaySfx/(player_attack_0[1-5]|player_defense_0[1-3]|skill_buff(_0[1-2])?|skill_heal)\.mp3$"),
                    path);
                Assert.That(skill.activationEffect.particleEffect, Is.Not.Null, path);
                if (skill.activationEffect.particleEffect.particlePrefab != null)
                {
                    AssertDesignTimeParticlePrefab(skill.activationEffect.particleEffect.particlePrefab, path);
                }

                Assert.That(skill.activationEffect.particleEffect.particleMaterial, Is.Not.Null, path);
                Assert.That(skill.activationEffect.particleEffect.useParticleColor, Is.True, path);
                Assert.That(
                    AssetDatabase.GetAssetPath(skill.activationEffect.particleEffect.particleMaterial),
                    Does.StartWith("Assets/Art/Effects/SkillVFX/Materials/"),
                    path);
                if (path.EndsWith("LightShot.asset", System.StringComparison.Ordinal) ||
                    path.EndsWith("GatherLight.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(skill.activationEffect?.vfxPrefab, Is.Not.Null, path);
                    Assert.That(skill.activationEffect.EffectiveAutoDestroySeconds, Is.EqualTo(1.55f).Within(0.0001f), path);
                    Assert.That(skill.activationEffect.EffectiveSfxDelaySeconds, Is.EqualTo(0.3f).Within(0.0001f), path);
                    if (path.EndsWith("GatherLight.asset", System.StringComparison.Ordinal))
                    {
                        Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.LightBeam), path);
                        Assert.That(skill.vfx.secondaryPrefab, Is.Not.Null, path);
                        Assert.That(
                            AssetDatabase.GetAssetPath(skill.vfx.secondaryPrefab),
                            Is.EqualTo("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_GatherLightVerticalBeam.prefab"),
                            path);
                    }
                }
                else if (path.EndsWith("LightRecover.asset", System.StringComparison.Ordinal) ||
                    path.EndsWith("FocusBreath.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.BuffAura), path);
                    Assert.That(skill.vfx.secondaryPrefab, Is.Not.Null, path);
                    Assert.That(
                        AssetDatabase.GetAssetPath(skill.vfx.secondaryPrefab),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_BuffAuraHealing.prefab"),
                        path);
                    if (path.EndsWith("FocusBreath.asset", System.StringComparison.Ordinal))
                    {
                        AssertColorApproximately(skill.vfxPrimaryColor, new Color(0.622f, 0.902f, 1f, 1f));
                        AssertColorApproximately(skill.vfxSecondaryColor, new Color(0.94f, 1f, 1f, 1f));
                    }
                }
                else if (path.EndsWith("FlowStrike.asset", System.StringComparison.Ordinal) ||
                    path.EndsWith("QuickStab.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.SlashArc), path);
                    Assert.That(
                        AssetDatabase.GetAssetPath(skill.vfx.primarySprite),
                        Is.EqualTo("Assets/Art/Source/ExP/Effects/Effect_Attack.png"),
                        path);
                    Assert.That(
                        AssetDatabase.GetAssetPath(skill.vfx.primaryPrefab),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_AttackImpact.prefab"),
                        path);
                    AssertVector3Approximately(skill.vfx.localOffset, new Vector3(0f, 0.16f, 0f), path);
                    Assert.That(skill.vfx.radiusMultiplier, Is.EqualTo(3.24f).Within(0.001f), path);
                    Assert.That(skill.vfx.tintWhiteBlend, Is.EqualTo(0f).Within(0.001f), path);
                    Assert.That(skill.vfx.alpha, Is.EqualTo(1f).Within(0.001f), path);
                    Assert.That(skill.vfx.rotationDegrees, Is.EqualTo(0f).Within(0.001f), path);
                    AssertColorApproximately(skill.vfxPrimaryColor, Color.white);
                    AssertColorApproximately(skill.vfxSecondaryColor, Color.white);
                }
                else if (path.EndsWith("TentacleStrike.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.TentacleWhip), path);
                    AssertVector3Approximately(skill.vfx.localOffset, new Vector3(-0.38f, 0.46f, 0f), path);
                    Assert.That(skill.vfx.tintWhiteBlend, Is.EqualTo(0f).Within(0.001f), path);
                    Assert.That(skill.vfx.alpha, Is.EqualTo(1f).Within(0.001f), path);
                    Assert.That(skill.vfxScale, Is.EqualTo(1.2f).Within(0.001f), path);
                }
                else if (path.EndsWith("HeavyStrike.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.SpikedBurst), path);
                }
                else if (skill.skillId == "fireball" ||
                    skill.skillId == "burst-fireball" ||
                    skill.skillId == "burn-out")
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.FlameBurst), path);
                    AssertFlameBurstTuningHasImpactExplosion(skill, path);
                    Assert.That(skill.activationEffect?.vfxPrefab, Is.Not.Null, path);
                    Assert.That(
                        AssetDatabase.GetAssetPath(skill.activationEffect.vfxPrefab),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_FireballProjectile.prefab"),
                        path);
                    Assert.That(
                        skill.activationEffect.vfxPrefab.GetComponentInChildren<CombatProjectileEffect>(true),
                        Is.Not.Null,
                        path);
                }
                else if (path.EndsWith("Overburn.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.FlameBurst), path);
                    AssertFlameBurstTuningHasImpactExplosion(skill, path);
                    Assert.That(skill.vfxScale, Is.GreaterThan(1f), path);
                }
                else if (path.EndsWith("RecklessBlow.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.FlameBurst), path);
                    AssertFlameBurstTuningHasImpactExplosion(skill, path);
                    Assert.That(skill.vfxScale, Is.LessThanOrEqualTo(1f), path);
                }
                else if (path.EndsWith("LightEcho.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.SupportFire), path);
                    Assert.That(skill.vfx.primarySprite, Is.Null, path);
                    Assert.That(skill.vfx.primaryPrefab, Is.Null, path);
                    Assert.That(skill.vfx.secondarySprite, Is.Null, path);
                    Assert.That(skill.vfx.secondaryPrefab, Is.Null, path);
                    Assert.That(skill.vfxRepeatCount, Is.EqualTo(1), path);
                }
                else if (path.EndsWith("Endure.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.CounterReady), path);
                    Assert.That(skill.vfx.primarySprite, Is.Null, path);
                    Assert.That(skill.vfx.primaryPrefab, Is.Null, path);
                    Assert.That(skill.vfx.secondaryPrefab, Is.Not.Null, path);
                    Assert.That(
                        AssetDatabase.GetAssetPath(skill.vfx.secondaryPrefab),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_BuffAuraHealing.prefab"),
                        path);
                    AssertColorApproximately(skill.vfxPrimaryColor, Color.white);
                    AssertColorApproximately(skill.vfxSecondaryColor, Color.white);
                }
                else if (path.EndsWith("DarkShackle.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.DarkChainBurst), path);
                }
            }
        }

        private PlayerSO CreatePlayerData(int maxHp, int attackPower)
        {
            var data = ScriptableObject.CreateInstance<PlayerSO>();
            data.maxHp = maxHp;
            data.attackPower = attackPower;
            ownedObjects.Add(data);
            return data;
        }

        private EnemySO CreateEnemyData(int maxHp, int attackValue)
        {
            var data = ScriptableObject.CreateInstance<EnemySO>();
            data.maxHp = maxHp;
            data.attackPower = attackValue;
            data.intentPattern = new List<EnemyIntent>
            {
                new()
                {
                    intentType = EnemyIntentType.Attack,
                    value = attackValue,
                },
            };
            ownedObjects.Add(data);
            return data;
        }

        private SkillSO CreateSkill(string skillId, SkillType skillType, int cost, int power)
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.skillId = skillId;
            skill.skillType = skillType;
            skill.cost = cost;
            skill.power = power;
            ownedObjects.Add(skill);
            return skill;
        }

        private BattleRewardSO CreateReward(float healPercentOfMaxHp, int extraBoardMoveCount)
        {
            var reward = ScriptableObject.CreateInstance<BattleRewardSO>();
            reward.healPercentOfMaxHp = healPercentOfMaxHp;
            reward.extraBoardMoveCount = extraBoardMoveCount;
            ownedObjects.Add(reward);
            return reward;
        }

        private RewardTableSO CreateRewardTable(BattleRewardSO reward)
        {
            var table = ScriptableObject.CreateInstance<RewardTableSO>();
            table.rewards = new List<BattleRewardSO> { reward };
            ownedObjects.Add(table);
            return table;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static Color ResolveMaterialColor(Material material)
        {
            Assert.That(material, Is.Not.Null);
            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            return material.HasProperty("_Color") ? material.GetColor("_Color") : material.color;
        }

        private static void AssertDesignTimePrefab(GameObject prefab, string expectedPath)
        {
            Assert.That(prefab, Is.Not.Null, expectedPath);
            Assert.That(AssetDatabase.GetAssetPath(prefab), Is.EqualTo(expectedPath));
        }

        private static void AssertDesignTimeParticlePrefab(ParticleSystem prefab, string context)
        {
            Assert.That(prefab, Is.Not.Null, context);
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            Assert.That(prefabPath, Does.StartWith("Assets/Art/Effects/SkillVFX/Prefabs/"), context);
            Assert.That(prefab.GetComponent<SkillVfxParticleBurstPrefab>(), Is.Not.Null, context);
        }

        private static void AssertSingleFullRectSpriteImport(string path, float expectedWidth, float expectedHeight)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            var importerSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(importerSettings);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
            Assert.That(importerSettings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.That(sprite, Is.Not.Null, path);
            Assert.That(sprite.rect.x, Is.EqualTo(0f).Within(0.001f), path);
            Assert.That(sprite.rect.y, Is.EqualTo(0f).Within(0.001f), path);
            Assert.That(sprite.rect.width, Is.EqualTo(expectedWidth).Within(0.001f), path);
            Assert.That(sprite.rect.height, Is.EqualTo(expectedHeight).Within(0.001f), path);
            Assert.That(sprite.pivot.x, Is.EqualTo(expectedWidth * 0.5f).Within(0.001f), path);
            Assert.That(sprite.pivot.y, Is.EqualTo(expectedHeight * 0.5f).Within(0.001f), path);
        }

        private static void AssertMultipleSpriteImport(string path, float expectedWidth, float expectedHeight, Vector2 expectedPivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple), path);

            var sprite = AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().SingleOrDefault();
            Assert.That(sprite, Is.Not.Null, path);
            Assert.That(sprite.rect.width, Is.EqualTo(expectedWidth).Within(0.001f), path);
            Assert.That(sprite.rect.height, Is.EqualTo(expectedHeight).Within(0.001f), path);
            Assert.That(sprite.pivot.x, Is.EqualTo(expectedPivot.x).Within(0.001f), path);
            Assert.That(sprite.pivot.y, Is.EqualTo(expectedPivot.y).Within(0.001f), path);
        }

        private static void AssertVfxGraphLink(string graphText, string outputSlotId, string inputSlotId)
        {
            var inputSlotMarker = $"--- !u!114 &{inputSlotId}\n";
            var inputSlotIndex = graphText.IndexOf(inputSlotMarker, System.StringComparison.Ordinal);
            Assert.That(inputSlotIndex, Is.GreaterThanOrEqualTo(0), inputSlotId);
            var nextSlotIndex = graphText.IndexOf("\n--- !u!114 &", inputSlotIndex + inputSlotMarker.Length, System.StringComparison.Ordinal);
            var inputSlotBlock = nextSlotIndex >= 0
                ? graphText.Substring(inputSlotIndex, nextSlotIndex - inputSlotIndex)
                : graphText.Substring(inputSlotIndex);

            Assert.That(
                graphText,
                Does.Contain($"- outputSlot: {{fileID: {outputSlotId}}}\n      inputSlot: {{fileID: {inputSlotId}}}"));
            Assert.That(inputSlotBlock, Does.Contain($"- {{fileID: {outputSlotId}}}"));
        }

        private static void AssertFlameBurstTuningHasImpactExplosion(SkillSO skill, string context)
        {
            Assert.That(skill.vfx, Is.Not.Null, context);
            Assert.That(
                AssetDatabase.GetAssetPath(skill.vfx.primarySprite),
                Is.EqualTo("Assets/Art/Source/ExP/Effects/Effect_Flame.png"),
                context);
            Assert.That(
                AssetDatabase.GetAssetPath(skill.vfx.primaryPrefab),
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_FlameBurst.prefab"),
                context);
            Assert.That(
                skill.vfxDefinition.cues.Select(cue => AssetDatabase.GetAssetPath(cue.prefab)).ToArray(),
                Does.Contain("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_FlameImage.prefab"),
                context);
            Assert.That(skill.vfx.secondaryPrefab, Is.Not.Null, context);
            Assert.That(
                skill.vfx.secondaryPrefab.GetComponentInChildren<LayeredExplosionEffect>(true),
                Is.Not.Null,
                context);
        }

        private static void AssertShieldPrefabHasDedicatedArtAndSparkles(
            GameObject prefab,
            string expectedSpritePath,
            string sparkleName)
        {
            Assert.That(prefab, Is.Not.Null, expectedSpritePath);
            var renderer = prefab.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null, expectedSpritePath);
            Assert.That(AssetDatabase.GetAssetPath(renderer.sprite), Is.EqualTo(expectedSpritePath));

            var sparkles = prefab.transform.Find(sparkleName)?.GetComponent<ParticleSystem>();
            Assert.That(sparkles, Is.Not.Null, sparkleName);
            Assert.That(sparkles.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Circle));
            Assert.That(sparkles.shape.radius, Is.GreaterThan(0.35f));
            Assert.That(sparkles.main.startSpeed.constantMax, Is.LessThanOrEqualTo(0.12f));
            Assert.That(sparkles.main.startSize.constantMax, Is.LessThanOrEqualTo(0.06f));
            Assert.That(sparkles.GetComponent<ParticleSystemRenderer>().sharedMaterial, Is.Not.Null);

            foreach (var legacyName in new[]
            {
                "ShieldLightCircleRing",
                "ShieldLightCircleHalo",
                "ShieldLightCircleVfxGraph",
                "ShieldLightDomeParticles",
                "ShieldLightCircleParticles",
                "ThornGuardSpikedCircleRing",
                "ThornGuardDarkInnerCircle",
                "ThornGuardTriangleSpikes",
                "ThornGuardSpikedCircleVfxGraph",
                "ThornGuardSpikeParticles",
                "ThornGuardSpikedCircleParticles",
            })
            {
                Assert.That(prefab.transform.Find(legacyName), Is.Null, legacyName);
            }
        }

        private static float ResolveLineRadiusSpread(LineRenderer line)
        {
            Assert.That(line, Is.Not.Null);

            var minRadius = float.MaxValue;
            var maxRadius = float.MinValue;
            for (var i = 0; i < line.positionCount; i++)
            {
                var position = line.GetPosition(i);
                var radius = new Vector2(position.x, position.y).magnitude;
                minRadius = Mathf.Min(minRadius, radius);
                maxRadius = Mathf.Max(maxRadius, radius);
            }

            return maxRadius - minRadius;
        }

        private static void AssertColorApproximately(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }

        private static void AssertVector3Approximately(
            Vector3 actual,
            Vector3 expected,
            string context)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f), $"{context} x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f), $"{context} y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.001f), $"{context} z");
        }
    }
}
