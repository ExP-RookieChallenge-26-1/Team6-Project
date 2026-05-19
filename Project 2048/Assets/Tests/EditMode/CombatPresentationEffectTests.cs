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
            rewardManager.BindCombat(manager);
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

            rewardManager.BindCombat(manager);
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
            Assert.That(((RectTransform)popup).anchoredPosition.y, Is.GreaterThan(0f));
        }

        [Test]
        public void CombatWorldSpriteView_PlayerSkillHpDamage_ShowsUnsignedDamageNumberAtEnemyBody()
        {
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
            Assert.That(((RectTransform)popup).anchoredPosition.y, Is.GreaterThan(0f));
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
                new Color(0.62f, 0.92f, 1f, 0.96f));
            AssertColorApproximately(
                ResolveMaterialColor(profile.fearDebuffCastEffect.particleMaterial),
                new Color(0.75f, 0.05f, 0.16f, 0.95f));
            AssertColorApproximately(
                ResolveMaterialColor(profile.darknessDebuffCastEffect.particleMaterial),
                new Color(0.24f, 0.10f, 0.48f, 0.95f));
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
        public void PrototypeCombatEventAudioProfile_ContainsResultAndRewardClips()
        {
            var profile = AssetDatabase.LoadAssetAtPath<PrototypeCombatEventAudioProfileSO>(
                "Assets/Data/Prototype/Presentation/PrototypeCombatEventAudioProfile.asset");

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.Resolve(PrototypeCombatEventSoundCue.Victory).sfxClip, Is.Not.Null);
            Assert.That(profile.Resolve(PrototypeCombatEventSoundCue.Defeat).sfxClip, Is.Not.Null);
            Assert.That(profile.Resolve(PrototypeCombatEventSoundCue.RewardRest).sfxClip, Is.Not.Null);
            Assert.That(profile.Resolve(PrototypeCombatEventSoundCue.RewardEnhance).sfxClip, Is.Not.Null);
        }

        [Test]
        public void PrototypeEnemyAssets_HaveAppearActionEffectClips()
        {
            var enemyGuids = AssetDatabase.FindAssets("t:EnemySO", new[] { "Assets/Data/Prototype/Enemies" });

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
            var enemyGuids = AssetDatabase.FindAssets("t:EnemySO", new[] { "Assets/Data/Prototype/Enemies" });

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
            var enemyGuids = AssetDatabase.FindAssets("t:EnemySO", new[] { "Assets/Data/Prototype/Enemies" });

            Assert.That(enemyGuids.Length, Is.GreaterThanOrEqualTo(12));
            foreach (var guid in enemyGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enemy = AssetDatabase.LoadAssetAtPath<EnemySO>(path);
                var attackEffect = enemy != null ? enemy.FindActionEffect(CombatActionIds.Attack) : null;
                var hitEffect = enemy != null ? enemy.FindActionEffect(CombatActionIds.Hit) : null;

                Assert.That(attackEffect, Is.Not.Null, path);
                Assert.That(attackEffect.sfxClip, Is.Not.Null, path);
                Assert.That(hitEffect, Is.Not.Null, path);
                Assert.That(hitEffect.sfxClip, Is.Not.Null, path);
                Assert.That(
                    AssetDatabase.GetAssetPath(hitEffect.sfxClip),
                    Does.StartWith("Assets/Sounds/MonsterHitSfx/"),
                    path);
            }
        }

        [Test]
        public void PrototypeBoardTileEffects_UsesMergeClipAndGranderTuningForLargerTiles()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BoardTileEffectProfileSO>(
                "Assets/Data/Prototype/Presentation/PrototypeBoardTileEffects.asset");

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
                "Assets/Data/Prototype/Presentation/PrototypeBoardTileEffects.asset");

            Assert.That(profile, Is.Not.Null);
            foreach (var tileValue in new[] { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048 })
            {
                var effect = profile.ResolveMergeEffect(tileValue);

                Assert.That(effect, Is.Not.Null, tileValue.ToString());
                Assert.That(effect.sfxClip, Is.Not.Null, tileValue.ToString());
            }
        }

        [Test]
        public void PrototypeSkillAssets_HaveActivationEffectClips()
        {
            var skillGuids = AssetDatabase.FindAssets("t:SkillSO", new[] { "Assets/Data/Prototype/Skills" });

            Assert.That(skillGuids.Length, Is.EqualTo(6));
            foreach (var guid in skillGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var skill = AssetDatabase.LoadAssetAtPath<SkillSO>(path);

                Assert.That(skill?.activationEffect, Is.Not.Null, path);
                Assert.That(skill.activationEffect.sfxClip, Is.Not.Null, path);
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

        private static void AssertColorApproximately(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }
    }
}
