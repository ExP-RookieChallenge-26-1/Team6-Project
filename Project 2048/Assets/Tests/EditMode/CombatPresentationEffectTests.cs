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
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

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

        private GameObject CreateOwnedRectTransformObject(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        private Sprite CreateOwnedSprite(string name)
        {
            var texture = new Texture2D(8, 8);
            texture.name = $"{name}Texture";
            ownedObjects.Add(texture);

            var pixels = Enumerable.Repeat(Color.white, 64).ToArray();
            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
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

        private SkillVfxPackageSO CreateOwnedVfxPackage(SkillVfxFamily family)
        {
            var package = ScriptableObject.CreateInstance<SkillVfxPackageSO>();
            package.family = family;
            ownedObjects.Add(package);
            return package;
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

            playerRenderer.sortingOrder = 7;
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
            Assert.That(art.transform.localPosition.x, Is.GreaterThan(0.2f));
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
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_AttackImpact.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.hitEffectSprite),
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_HitImpact.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.shieldEffectSprite),
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ShieldBarrier.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.thornShieldEffectSprite),
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ThornShieldBarrier.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.magicCircleEffectSprite),
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_MagicCircle.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.flameEffectSprite),
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_FlameBurst.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.chainAttackEffectSprite),
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ChainAttack.png"));
            Assert.That(
                AssetDatabase.GetAssetPath(profile.boundChainsEffectSprite),
                Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_BoundChains.png"));
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
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_AttackImpact.png",
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_HitImpact.png",
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ShieldBarrier.png",
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ThornShieldBarrier.png",
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_MagicCircle.png",
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_FlameBurst.png",
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ChainAttack.png",
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_BoundChains.png",
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
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(path), Is.Not.Null, path);
            }
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
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ShieldBarrier.png",
                "ShieldGuardSparkles");
            AssertShieldPrefabHasDedicatedArtAndSparkles(
                profile.thornShieldEffectPrefab,
                "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ThornShieldBarrier.png",
                "ThornGuardShieldSparkles");
            AssertDesignTimePrefab(profile.magicCircleEffectPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_MagicCircle.prefab");
            AssertDesignTimePrefab(profile.flameEffectPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_FlameBurst.prefab");
            AssertDesignTimePrefab(profile.darkChainLaunchPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_DarkShackleLaunch.prefab");
            AssertDesignTimePrefab(profile.chainAttackEffectPrefab, "Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_ChainAttack.prefab");
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
                    Does.StartWith("Assets/Art/Effects/SkillVFX/Textures/"),
                    family.ToString());
                Assert.That(
                    AssetDatabase.GetAssetPath(binding.prefab),
                    Does.StartWith("Assets/Art/Effects/SkillVFX/Prefabs/"),
                    family.ToString());
            }
        }

        [Test]
        public void SkillVfxPackages_ExistForEveryReusableVfxFamily()
        {
            var expectedFamilies = System.Enum.GetValues(typeof(SkillVfxFamily))
                .Cast<SkillVfxFamily>()
                .Where(family => family != SkillVfxFamily.None)
                .OrderBy(family => family)
                .ToArray();
            var packages = AssetDatabase
                .FindAssets("t:SkillVfxPackageSO", new[] { "Assets/Art/Effects/SkillVFX/Packages" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SkillVfxPackageSO>)
                .Where(package => package != null)
                .ToArray();
            var actualFamilies = packages
                .Select(package => package.family)
                .OrderBy(family => family)
                .ToArray();

            Assert.That(actualFamilies, Is.EquivalentTo(expectedFamilies));
            Assert.That(actualFamilies.Length, Is.EqualTo(actualFamilies.Distinct().Count()));
            foreach (var package in packages)
            {
                var path = AssetDatabase.GetAssetPath(package);
                Assert.That(
                    path,
                    Is.EqualTo($"Assets/Art/Effects/SkillVFX/Packages/SkillVfx_{package.family}Package.asset"),
                    package.family.ToString());
                Assert.That(package.HasAuthoredVisual, Is.True, path);
                if (package.particleMaterial != null)
                {
                    Assert.That(
                        AssetDatabase.GetAssetPath(package.particleMaterial),
                        Does.StartWith("Assets/Art/Effects/SkillVFX/Materials/"),
                        path);
                }

                if (package.family == SkillVfxFamily.ShieldDome)
                {
                    Assert.That(
                        AssetDatabase.GetAssetPath(package.primarySprite),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ShieldBarrier.png"));
                    Assert.That(
                        AssetDatabase.GetAssetPath(package.secondarySprite),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ThornShieldBarrier.png"));
                    AssertShieldPrefabHasDedicatedArtAndSparkles(
                        package.primaryPrefab,
                        "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ShieldBarrier.png",
                        "ShieldGuardSparkles");
                    AssertShieldPrefabHasDedicatedArtAndSparkles(
                        package.secondaryPrefab,
                        "Assets/Art/Effects/SkillVFX/Textures/SkillVfx_ThornShieldBarrier.png",
                        "ThornGuardShieldSparkles");
                }

                if (package.family == SkillVfxFamily.TentacleWhip)
                {
                    Assert.That(
                        AssetDatabase.GetAssetPath(package.primarySprite),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_TentacleWhip.png"));
                    Assert.That(
                        AssetDatabase.GetAssetPath(package.primaryPrefab),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_TentacleWhip.prefab"));
                }

                if (package.family == SkillVfxFamily.SupportFire)
                {
                    Assert.That(
                        AssetDatabase.GetAssetPath(package.primaryPrefab),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_AttackImpact.prefab"));
                    Assert.That(
                        AssetDatabase.GetAssetPath(package.secondarySprite),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_MagicCircle.png"));
                    Assert.That(
                        AssetDatabase.GetAssetPath(package.secondaryPrefab),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_MagicCircle.prefab"));
                }

                if (package.family == SkillVfxFamily.FlameBurst)
                {
                    Assert.That(
                        AssetDatabase.GetAssetPath(package.primarySprite),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Textures/SkillVfx_FlameBurst.png"));
                    Assert.That(
                        AssetDatabase.GetAssetPath(package.primaryPrefab),
                        Is.EqualTo("Assets/Art/Effects/SkillVFX/Prefabs/SkillVfx_FlameBurst.prefab"));
                    Assert.That(package.localOffset.y, Is.LessThan(0f));
                }
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
                "Assets/Art/Effects/SkillVFX/Packages",
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
                "Assets/Art/Effects/SkillVFX/Packages",
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
        public void CombatWorldSpriteView_PlayerReusableSkill_AssignsGeneratedParticleMaterial()
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
            var expectedColor = new Color(0.4f, 0.8f, 1f, 0.9f);
            var magicCircleSprite = CreateOwnedSprite("MagicCircleEffectSprite");
            attack.vfxFamily = SkillVfxFamily.BuffAura;
            attack.vfxPrimaryColor = expectedColor;

            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "magicCircleEffectSprite", magicCircleSprite);
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

            var magicCircleArt = enemyRenderer.transform.Find("MagicCircleEffectArt")?.GetComponent<SpriteRenderer>();
            Assert.That(magicCircleArt, Is.Not.Null);
            Assert.That(magicCircleArt.sprite, Is.EqualTo(magicCircleSprite));
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
            var hitSprite = CreateOwnedSprite("HitImpactEffectSprite");
            var impact = CreateSkill("body-press", SkillType.Attack, cost: 0, power: 80);
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            impact.vfxFamily = SkillVfxFamily.ImpactBurst;
            impact.vfxPrimaryColor = new Color(0.9f, 0.72f, 0.32f, 1f);

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "hitEffectSprite", hitSprite);

            view.PreviewSkillEffect(impact);

            var art = enemyRenderer.transform.Find("HitImpactEffectArt")?.GetComponent<SpriteRenderer>();
            Assert.That(art, Is.Not.Null);
            Assert.That(art.sprite, Is.EqualTo(hitSprite));
            Assert.That(enemyRenderer.transform.Find("AttackEffectArt"), Is.Null);
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
                },
            };
            ownedObjects.Add(profile);

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "worldVfxProfile", profile);

            view.PreviewSkillEffect(projectile);

            var art = enemyRenderer.transform.Find("LightProjectileEffectArt")?.GetComponent<SpriteRenderer>();
            Assert.That(art, Is.Not.Null);
            Assert.That(art.sprite, Is.EqualTo(projectileSprite));
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
            var supportPackage = CreateOwnedVfxPackage(SkillVfxFamily.SupportFire);
            supportPackage.primarySprite = attackSprite;
            supportPackage.primaryPrefab = attackPrefab;
            supportPackage.secondarySprite = magicCircleSprite;
            supportPackage.secondaryPrefab = magicCirclePrefab;
            var supportFire = CreateSkill("light-echo", SkillType.Attack, cost: 0, power: 20);
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            supportFire.vfxFamily = SkillVfxFamily.SupportFire;
            supportFire.vfxPackage = supportPackage;
            supportFire.vfxPrimaryColor = new Color(0.92f, 0.86f, 0.28f, 1f);
            supportFire.vfxSecondaryColor = new Color(0.54f, 0.82f, 1f, 1f);
            supportFire.vfxRepeatCount = 3;

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            view.PreviewSkillEffect(supportFire);

            var magicCircleArt = enemyRenderer.transform.Find("MagicCircleEffectArt")?.GetComponent<SpriteRenderer>();
            Assert.That(magicCircleArt, Is.Not.Null);
            Assert.That(magicCircleArt.sprite, Is.EqualTo(magicCircleSprite));
            Assert.That(enemyRenderer.transform.Find("MagicCircleEffectArt/SupportFireMagicCirclePrefabMarker"), Is.Not.Null);

            var impactArt = enemyRenderer.transform.Find("SupportFireImpactArt")?.GetComponent<SpriteRenderer>();
            Assert.That(impactArt, Is.Not.Null);
            Assert.That(impactArt.sprite, Is.EqualTo(attackSprite));
            Assert.That(enemyRenderer.transform.Find("SupportFireImpactArt/SupportFireImpactPrefabMarker"), Is.Not.Null);
            Assert.That(enemyRenderer.transform.Find("LightEchoSupportFireParticles"), Is.Not.Null);
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
            var shieldPackage = CreateOwnedVfxPackage(SkillVfxFamily.ShieldDome);
            shieldPackage.primarySprite = shieldSprite;
            shieldPackage.primaryPrefab = shieldPrefab;
            shield.vfxPackage = shieldPackage;
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
            Assert.That(shieldArt.sortingOrder, Is.GreaterThan(playerRenderer.sortingOrder));

            var persistentShieldRoot = viewObject.transform.Find("PlayerShieldArtVfx");
            Assert.That(persistentShieldRoot, Is.Not.Null);
            var persistentShieldArt = persistentShieldRoot.Find("PlayerPersistentShieldArt")?.GetComponent<SpriteRenderer>();
            Assert.That(persistentShieldArt, Is.Not.Null);
            Assert.That(persistentShieldArt.sprite, Is.EqualTo(shieldSprite));
            Assert.That(persistentShieldArt.sortingOrder, Is.GreaterThan(playerRenderer.sortingOrder));
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
            var thornPackage = CreateOwnedVfxPackage(SkillVfxFamily.ShieldDome);
            thornPackage.primarySprite = shieldSprite;
            thornPackage.secondarySprite = thornShieldSprite;
            thornPackage.secondaryPrefab = thornShieldPrefab;
            thornGuard.vfxPackage = thornPackage;

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

            var sparkles = shieldArtRoot?.Find("ThornGuardShieldSparkles")?.GetComponent<ParticleSystem>();
            Assert.That(sparkles, Is.Not.Null);
            Assert.That(sparkles.shape.shapeType, Is.EqualTo(ParticleSystemShapeType.Circle));
            Assert.That(sparkles.shape.radius, Is.GreaterThan(0.35f));
            Assert.That(sparkles.main.startSpeed.constantMax, Is.LessThanOrEqualTo(0.18f));
        }

        [Test]
        public void CombatWorldSpriteView_ShieldBurstAttack_SpawnsBurstAndTargetImpact()
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
            Assert.That(playerRenderer.transform.Find("ShieldBurstExpansionRing"), Is.Not.Null);
            Assert.That(playerRenderer.transform.Find("ShieldBurstShardParticles"), Is.Not.Null);
            Assert.That(enemyRenderer.transform.Find("ShieldBurstImpactParticles"), Is.Not.Null);
            Assert.That(enemyRenderer.transform.Find("ShieldBurstImpactRing"), Is.Not.Null);
        }

        [Test]
        public void CombatWorldSpriteView_ChargedAttackRelease_SpawnsCoolLightBeamWithAttackImpactArt()
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
            var attackSprite = CreateOwnedSprite("SkillVfx_AttackImpact");
            charge.effectKind = SkillEffectKind.ChargeAttack;
            charge.chargedPower = 120;
            playerData.startingSkills = new List<SkillSO> { charge };
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
            Assert.That(playerRenderer.transform.Find("gather-lightChargeParticles"), Is.Not.Null);

            manager.RequestEndPlayerTurn();

            var beam = viewObject.transform.Find("ChargedLightBeam")?.GetComponent<LineRenderer>();
            Assert.That(beam, Is.Not.Null);
            Assert.That(beam.positionCount, Is.EqualTo(2));
            Assert.That(beam.sharedMaterial, Is.Not.Null);
            Assert.That(beam.startColor.b, Is.GreaterThanOrEqualTo(beam.startColor.g));
            Assert.That(beam.startColor.g, Is.GreaterThan(0.9f));
            Assert.That(beam.startWidth, Is.EqualTo(0.08f).Within(0.001f));
            Assert.That(beam.endWidth, Is.EqualTo(0.16f).Within(0.001f));
            Assert.That(beam.GetPosition(0).x, Is.GreaterThan(playerRenderer.transform.position.x));
            Assert.That(beam.GetPosition(0).y, Is.GreaterThan(playerRenderer.transform.position.y + 0.3f));
            Assert.That(beam.sortingOrder, Is.GreaterThan(enemyRenderer.sortingOrder));
            Assert.That(viewObject.transform.Find("ChargedLightBeamGlow")?.GetComponent<LineRenderer>(), Is.Not.Null);
            Assert.That(enemyRenderer.transform.Find("ChargedLightBeamImpactParticles"), Is.Not.Null);
            var art = enemyRenderer.transform.Find("ChargedLightAttackArt")?.GetComponent<SpriteRenderer>();
            Assert.That(art, Is.Not.Null);
            Assert.That(art.sprite, Is.EqualTo(attackSprite));
            Assert.That(art.sortingOrder, Is.GreaterThan(enemyRenderer.sortingOrder));
        }

        [Test]
        public void CombatWorldSpriteView_GatherLightPreview_UsesChargeParticlesInsteadOfProjectileOrAttackArt()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var projectilePrefab = CreateOwnedGameObject("GatherLightProjectilePrefab");
            var gatherLight = CreateSkill("gather-light", SkillType.Attack, cost: 0, power: 0);
            projectilePrefab.AddComponent<CombatProjectileEffect>();
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            gatherLight.effectKind = SkillEffectKind.ChargeAttack;
            gatherLight.vfxFamily = SkillVfxFamily.LightBeam;
            gatherLight.activationEffect = new CombatEffectBinding
            {
                vfxPrefab = projectilePrefab,
            };

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);

            view.PreviewSkillEffect(gatherLight);

            Assert.That(viewObject.transform.Find("ChargedLightBeam"), Is.Null);
            Assert.That(viewObject.transform.Find("ChargedLightBeamGlow"), Is.Null);
            Assert.That(enemyRenderer.transform.Find("ChargedLightAttackArt"), Is.Null);
            Assert.That(playerRenderer.transform.Find("gather-lightChargeParticles"), Is.Not.Null);
            Assert.That(viewObject.transform.Find("GatherLightProjectilePrefab(Clone)"), Is.Null);
        }

        [Test]
        public void CombatWorldSpriteView_TentacleStrikePreview_SpawnsFlexibleWhipShape()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var tentacle = CreateSkill("tentacle-strike", SkillType.Attack, cost: 0, power: 90);
            var attackSprite = CreateOwnedSprite("TentacleStrikeImpactSprite");
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            tentacle.vfxFamily = SkillVfxFamily.TentacleWhip;
            tentacle.vfxPrimaryColor = new Color(0.2f, 0.04f, 0.28f, 1f);
            tentacle.vfxSecondaryColor = new Color(0.55f, 0.18f, 0.72f, 1f);

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "attackEffectSprite", attackSprite);

            view.PreviewSkillEffect(tentacle);

            var impactArt = enemyRenderer.transform.Find("TentacleStrikeImpactArt")?.GetComponent<SpriteRenderer>();
            Assert.That(impactArt, Is.Not.Null);
            Assert.That(impactArt.sprite, Is.EqualTo(attackSprite));
            var whip = viewObject.transform.Find("TentacleStrikeWhip")?.GetComponent<LineRenderer>();
            Assert.That(whip, Is.Not.Null);
            Assert.That(whip.positionCount, Is.GreaterThan(8));
            Assert.That(whip.startWidth, Is.GreaterThan(whip.endWidth));
            Assert.That(whip.sortingOrder, Is.GreaterThan(enemyRenderer.sortingOrder));
            Assert.That(whip.transform.Find("TentacleStrikeHighlight")?.GetComponent<LineRenderer>(), Is.Not.Null);
            Assert.That(whip.transform.Cast<Transform>().Count(child => child.name.StartsWith("TentacleSuctionCup")), Is.GreaterThanOrEqualTo(3));
            var finalPoint = whip.GetPosition(whip.positionCount - 1);
            var previousPoint = whip.GetPosition(whip.positionCount - 2);
            var highestPoint = Enumerable.Range(0, whip.positionCount)
                .Select(whip.GetPosition)
                .OrderByDescending(point => point.y)
                .First();
            Assert.That(whip.GetPosition(0).x, Is.GreaterThan(playerRenderer.transform.position.x));
            Assert.That(highestPoint.y, Is.GreaterThan(finalPoint.y + 0.65f));
            Assert.That(previousPoint.y, Is.GreaterThan(finalPoint.y));
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
            var burst = enemyRenderer.transform.Find("HeavyStrikeSpikedBurst");
            Assert.That(burst, Is.Not.Null);
            var star = burst.Find("HeavyStrikeSpikedBurstStar")?.GetComponent<LineRenderer>();
            Assert.That(star, Is.Not.Null);
            Assert.That(star.positionCount, Is.GreaterThan(16));
            Assert.That(star.sortingOrder, Is.GreaterThan(enemyRenderer.sortingOrder));
            Assert.That(burst.Cast<Transform>().Count(child => child.name.StartsWith("HeavyStrikeSpikeRay")), Is.GreaterThanOrEqualTo(8));
            Assert.That(enemyRenderer.transform.Find("HeavyStrikeSpikedExplosionParticles")?.GetComponent<ParticleSystem>(), Is.Not.Null);
        }

        [Test]
        public void CombatWorldSpriteView_BleedingCutPreview_SpawnsSlashAndBloodFountain()
        {
            var viewObject = CreateOwnedGameObject("WorldSpriteView");
            var view = viewObject.AddComponent<CombatWorldSpriteView>();
            var playerRenderer = CreateOwnedGameObject("PlayerSprite").AddComponent<SpriteRenderer>();
            var enemyRenderer = CreateOwnedGameObject("EnemySprite").AddComponent<SpriteRenderer>();
            var bleedingCut = CreateSkill("bleeding-cut", SkillType.Attack, cost: 0, power: 50);
            var attackSprite = CreateOwnedSprite("BloodFountainSlashSprite");
            playerRenderer.transform.localPosition = new Vector3(-1f, 0f, 0f);
            enemyRenderer.transform.localPosition = new Vector3(1f, 0f, 0f);
            enemyRenderer.sortingOrder = 4;
            bleedingCut.vfxFamily = SkillVfxFamily.BloodFountainSlash;
            bleedingCut.vfxPrimaryColor = new Color(0.95f, 0.02f, 0.04f, 1f);
            bleedingCut.vfxSecondaryColor = new Color(0.34f, 0f, 0.015f, 1f);
            bleedingCut.vfxScale = 1.1f;
            bleedingCut.vfxIntensity = 1.3f;

            SetPrivateField(view, "playerRenderer", playerRenderer);
            SetPrivateField(view, "enemyRenderer", enemyRenderer);
            SetPrivateField(view, "attackEffectSprite", attackSprite);

            view.PreviewSkillEffect(bleedingCut);

            var impactArt = enemyRenderer.transform.Find("BloodFountainSlashArt")?.GetComponent<SpriteRenderer>();
            Assert.That(impactArt, Is.Not.Null);
            Assert.That(impactArt.sprite, Is.EqualTo(attackSprite));
            var slashRoot = enemyRenderer.transform.Find("BleedingCutSlashArc");
            Assert.That(slashRoot, Is.Not.Null);
            var slash = slashRoot.Find("BleedingCutSlashLine")?.GetComponent<LineRenderer>();
            Assert.That(slash, Is.Not.Null);
            Assert.That(slash.positionCount, Is.GreaterThan(8));
            Assert.That(slash.startWidth, Is.GreaterThan(slash.endWidth));
            Assert.That(slashRoot.Find("BleedingCutSlashEdge")?.GetComponent<LineRenderer>(), Is.Not.Null);
            var fountain = enemyRenderer.transform.Find("BleedingCutBloodFountain")?.GetComponent<ParticleSystem>();
            Assert.That(fountain, Is.Not.Null);
            Assert.That(fountain.main.startSize.constantMax, Is.LessThan(0.1f));
            Assert.That(fountain.main.maxParticles, Is.GreaterThanOrEqualTo(90));
            Assert.That(fountain.shape.radius, Is.LessThan(0.08f));
            Assert.That(enemyRenderer.transform.Find("BleedingCutBloodMist")?.GetComponent<ParticleSystem>(), Is.Not.Null);
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
            var chain = chainRoot.Find("DarkShackleChainLine")?.GetComponent<LineRenderer>();
            Assert.That(chain, Is.Not.Null);
            Assert.That(chain.positionCount, Is.GreaterThan(8));
            Assert.That(chain.GetPosition(0).x, Is.GreaterThan(playerRenderer.transform.position.x));
            Assert.That(chain.GetPosition(chain.positionCount - 1).y, Is.GreaterThan(enemyRenderer.transform.position.y));
            var head = chainRoot.Find("DarkShackleChainHead")?.GetComponent<LineRenderer>();
            Assert.That(head, Is.Not.Null);
            Assert.That(head.positionCount, Is.EqualTo(7));
            Assert.That(head.GetPosition(0).x, Is.GreaterThan(enemyRenderer.transform.position.x - 0.05f));
            Assert.That(head.GetPosition(0).y, Is.GreaterThan(enemyRenderer.transform.position.y));
            Assert.That(chainRoot.Cast<Transform>().Count(child => child.name.StartsWith("DarkShackleChainLink")), Is.GreaterThanOrEqualTo(8));
            Assert.That(enemyRenderer.transform.Find("DarkShackleImpactExplosion")?.GetComponent<ParticleSystem>(), Is.Not.Null);
            Assert.That(enemyRenderer.transform.Find("DarkShackleImpactRing")?.GetComponent<LineRenderer>(), Is.Not.Null);
            var chainAttackArt = chainRoot.Find("DarkShackleChainAttackArt")?.GetComponent<SpriteRenderer>();
            Assert.That(chainAttackArt, Is.Not.Null);
            Assert.That(chainAttackArt.sprite, Is.EqualTo(chainAttackSprite));
            Assert.That(chainAttackArt.transform.position.x, Is.GreaterThan(playerRenderer.transform.position.x));
            Assert.That(enemyRenderer.transform.Find("DarkShackleChainAttackArt"), Is.Null);
            Assert.That(chainRoot.Find("DarkShackleChainAttackArt/SharedChainAttackPrefabMarker"), Is.Not.Null);
            var boundChainsArt = enemyRenderer.transform.Find("DarkShackleBoundChainsArt")?.GetComponent<SpriteRenderer>();
            Assert.That(boundChainsArt, Is.Not.Null);
            Assert.That(boundChainsArt.sprite, Is.EqualTo(boundChainsSprite));
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
            var flamePackage = CreateOwnedVfxPackage(SkillVfxFamily.FlameBurst);
            flamePackage.primarySprite = flameSprite;
            flamePackage.primaryPrefab = flamePrefab;
            flamePackage.localOffset = new Vector3(0f, -0.42f, 0f);
            overburn.vfxPackage = flamePackage;

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
            Assert.That(slotSkillIds, Does.Contain("light-guard"));
            Assert.That(slotSkillIds, Does.Contain("dark-shackle"));
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
                Assert.That(skill.vfxPackage, Is.Not.Null, path);
                Assert.That(resolvedFamily, Is.Not.EqualTo(SkillVfxFamily.None), path);
                Assert.That(skill.vfxPackage.family, Is.EqualTo(resolvedFamily), path);
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
                    }
                }
                else if (path.EndsWith("TentacleStrike.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.TentacleWhip), path);
                }
                else if (path.EndsWith("HeavyStrike.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.SpikedBurst), path);
                }
                else if (path.EndsWith("BleedingCut.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.BloodFountainSlash), path);
                }
                else if (path.EndsWith("Overburn.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.FlameBurst), path);
                    Assert.That(skill.vfxScale, Is.GreaterThan(1f), path);
                }
                else if (path.EndsWith("RecklessBlow.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.FlameBurst), path);
                    Assert.That(skill.vfxScale, Is.LessThanOrEqualTo(1f), path);
                }
                else if (path.EndsWith("LightEcho.asset", System.StringComparison.Ordinal))
                {
                    Assert.That(resolvedFamily, Is.EqualTo(SkillVfxFamily.SupportFire), path);
                    Assert.That(skill.vfxRepeatCount, Is.GreaterThanOrEqualTo(3), path);
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
    }
}
