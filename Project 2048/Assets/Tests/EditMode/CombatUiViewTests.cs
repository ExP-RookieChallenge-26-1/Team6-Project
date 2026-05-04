using NUnit.Framework;
using Project2048.Board2048;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Prototype;
using Project2048.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project2048.Tests
{
    public class CombatUiViewTests
    {
        private readonly System.Collections.Generic.List<Object> ownedObjects = new();

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

        [Test]
        public void BoardTransitionDuration_KeepsTileMovementSnappy()
        {
            Assert.That(CombatUiView.BoardTransitionDurationSeconds, Is.EqualTo(0.14f).Within(0.001f));
        }

        [Test]
        public void BoardToActionDelay_WaitsAfterMovesAreSpentBeforeShowingSkillChoices()
        {
            Assert.That(CombatUiView.BoardToActionPanelDelaySeconds, Is.GreaterThanOrEqualTo(0.35f));
        }

        [Test]
        public void CombatVfxDuration_KeepsTemporaryDebuffFeedbackShort()
        {
            Assert.That(CombatUiView.CombatVfxDurationSeconds, Is.InRange(0.45f, 0.9f));
        }

        [Test]
        public void BoardSwipeHandler_PointerSwipe_EmitsDirectionForMobileTouch()
        {
            var go = new GameObject("Swipe Handler");
            try
            {
                var handler = go.AddComponent<BoardSwipeHandler>();
                Direction? observed = null;
                handler.OnSwipe += direction => observed = direction;

                Assert.That(handler, Is.InstanceOf<IPointerDownHandler>());
                Assert.That(handler, Is.InstanceOf<IPointerUpHandler>());

                var pointerDown = (IPointerDownHandler)handler;
                var pointerUp = (IPointerUpHandler)handler;

                pointerDown.OnPointerDown(new PointerEventData(null)
                {
                    position = new Vector2(100f, 100f),
                });
                pointerUp.OnPointerUp(new PointerEventData(null)
                {
                    position = new Vector2(180f, 108f),
                });

                Assert.That(observed, Is.EqualTo(Direction.Right));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Initialize_WithMissingSerializedBattleReferences_WiresEnemyHpAndIntentByName()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var view = viewObject.AddComponent<CombatUiView>();
            var intentBubbleImage = CreateImageChild(viewObject.transform, "IntentBubble");
            var intentText = CreateTextChild(viewObject.transform.Find("IntentBubble"), "IntentBubbleText");
            var playerBattleHp = CreateImageChild(viewObject.transform, "PlayerBattleHp");
            var playerBattleHpFill = CreateImageChild(playerBattleHp.transform, "Fill");
            var playerBattleHpText = CreateTextChild(playerBattleHp.transform, "Text");
            var enemyHp = CreateImageChild(viewObject.transform, "EnemyHp");
            var enemyHpFill = CreateImageChild(enemyHp.transform, "Fill");
            var enemyHpText = CreateTextChild(enemyHp.transform, "Text");
            var boardHpFill = CreateImageChild(viewObject.transform, "HpBarFill");
            var boardHpText = CreateTextChild(viewObject.transform, "HpText");
            var actionText = CreateTextChild(viewObject.transform, "ActionDescriptionText");

            var manager = CreateOwnedGameObject("CombatManager").AddComponent<CombatManager>();
            var player = CreateOwnedGameObject("Player").AddComponent<PlayerCombatController>();
            var enemy = CreateOwnedGameObject("Enemy").AddComponent<EnemyController>();
            var bootstrap = CreateOwnedGameObject("Bootstrap").AddComponent<PrototypeCombatBootstrap>();
            SetPrivateField(bootstrap, "combatManager", manager);

            var attack = CreateSkill("attack", "검격", SkillType.Attack, cost: 0, power: 5);
            var playerData = CreatePlayerData(20, 0, attack);
            var enemyData = CreateEnemyData("슬라임", 10, 4);

            manager.SetCombatants(player, new[] { enemy });
            manager.StartCombat(new CombatSetup
            {
                playerData = playerData,
                enemyDataList = new System.Collections.Generic.List<EnemySO> { enemyData },
                boardMoveCount = 1,
            });
            manager.ResolveBoardPhase();

            view.Initialize(bootstrap);
            Assert.That(intentText.text, Is.EqualTo("공격"));
            Assert.That(intentBubbleImage.color, Is.EqualTo(new Color(0.85f, 0.12f, 0.12f, 1f)));
            AssertHpFillIsRenderable(playerBattleHpFill);
            AssertHpFillIsRenderable(enemyHpFill);
            AssertHpFillIsRenderable(boardHpFill);

            enemy.SetIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Defense,
                value = 3,
            });

            Assert.That(intentText.text, Is.EqualTo("방어"));
            Assert.That(intentBubbleImage.color, Is.EqualTo(new Color(0.12f, 0.32f, 0.90f, 1f)));

            enemy.SetIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Debuff,
                debuffType = DebuffType.Darkness,
                value = 2,
            });

            Assert.That(intentText.text, Is.EqualTo("암흑"));
            Assert.That(intentBubbleImage.color, Is.EqualTo(new Color(0.20f, 0.07f, 0.34f, 1f)));

            enemy.SetIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Debuff,
                debuffType = DebuffType.Fear,
                value = 2,
            });

            Assert.That(intentText.text, Is.EqualTo("공포"));
            Assert.That(intentBubbleImage.color, Is.EqualTo(new Color(0.45f, 0.03f, 0.06f, 1f)));

            manager.RequestUseSkillById("attack", 0);

            Assert.That(enemyHpFill.fillAmount, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(enemyHpText.text, Is.EqualTo("체력 5/10"));
            Assert.That(actionText.text, Is.EqualTo("최근 행동: 플레이어: 검격"));

            enemy.SetIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Attack,
                value = 4,
            });
            manager.RequestEndPlayerTurn();

            Assert.That(playerBattleHpFill.fillAmount, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(boardHpFill.fillAmount, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(playerBattleHpFill.rectTransform.anchorMax.x, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(boardHpFill.rectTransform.anchorMax.x, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(playerBattleHpText.text, Is.EqualTo("체력 16/20"));
            Assert.That(boardHpText.text, Is.EqualTo("체력 16/20"));
        }

        [Test]
        public void Initialize_ConfiguresAudioSourceForAudibleUiSfx()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            var source = viewObject.AddComponent<AudioSource>();
            source.playOnAwake = true;
            source.spatialBlend = 1f;
            source.volume = 0.05f;
            source.mute = true;
            source.minDistance = 1f;
            source.maxDistance = 2f;
            var view = viewObject.AddComponent<CombatUiView>();

            view.Initialize(null);

            Assert.That(source.playOnAwake, Is.False);
            Assert.That(source.spatialBlend, Is.EqualTo(0f).Within(0.001f));
            Assert.That(source.volume, Is.EqualTo(1f).Within(0.001f));
            Assert.That(source.mute, Is.False);
            Assert.That(source.minDistance, Is.GreaterThanOrEqualTo(1000f));
            Assert.That(source.maxDistance, Is.GreaterThanOrEqualTo(1000f));
            Assert.That((float)GetPrivateField(view, "soundVolumeScale"), Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void Initialize_PreservesPositiveInspectorSoundVolumeScale()
        {
            var viewObject = CreateOwnedGameObject("CombatView");
            viewObject.AddComponent<AudioSource>();
            var view = viewObject.AddComponent<CombatUiView>();
            SetPrivateField(view, "soundVolumeScale", 1.5f);

            view.Initialize(null);

            Assert.That((float)GetPrivateField(view, "soundVolumeScale"), Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void AudioRouter_EmitsHitAndMergeCuesFromCombatChanges()
        {
            var router = new PrototypeCombatAudioRouter();
            router.Reset(new CombatSnapshot
            {
                Player = new PlayerCombatSnapshot { CurrentHp = 20, MaxHp = 20 },
                Enemies = new System.Collections.Generic.List<EnemyCombatSnapshot>
                {
                    new() { EnemyIndex = 0, CurrentHp = 10, MaxHp = 10 },
                },
            });

            var cues = router.GetSnapshotCues(new CombatSnapshot
            {
                Player = new PlayerCombatSnapshot { CurrentHp = 16, MaxHp = 20 },
                Enemies = new System.Collections.Generic.List<EnemyCombatSnapshot>
                {
                    new() { EnemyIndex = 0, CurrentHp = 4, MaxHp = 10 },
                },
            });

            Assert.That(cues, Does.Contain(PrototypeCombatSoundCue.PlayerHit));
            Assert.That(cues, Does.Contain(PrototypeCombatSoundCue.EnemyHit));

            var transition = new BoardTransition();
            transition.Movements.Add(new BoardTileMovement
            {
                From = Vector2Int.zero,
                To = Vector2Int.right,
                Value = 2,
                IsMergeParticipant = true,
            });
            var mergeCues = router.GetBoardTransitionCues(transition);

            Assert.That(mergeCues, Is.EquivalentTo(new[]
            {
                PrototypeCombatSoundCue.BoardMove,
                PrototypeCombatSoundCue.BoardMerge,
            }));
        }

        [Test]
        public void AudioRouter_EmitsBoardMoveCueForNonMergeBoardMovement()
        {
            var router = new PrototypeCombatAudioRouter();
            var transition = new BoardTransition();
            transition.Movements.Add(new BoardTileMovement
            {
                From = Vector2Int.zero,
                To = Vector2Int.right,
                Value = 2,
                IsMergeParticipant = false,
            });

            var cues = router.GetBoardTransitionCues(transition);

            Assert.That(cues, Is.EquivalentTo(new[] { PrototypeCombatSoundCue.BoardMove }));
        }

        [Test]
        public void AudioRouter_EmitsHitCuesWhenBlockAbsorbsDamage()
        {
            var router = new PrototypeCombatAudioRouter();
            router.Reset(new CombatSnapshot
            {
                Phase = CombatPhase.ActionPhase,
                Player = new PlayerCombatSnapshot { CurrentHp = 20, MaxHp = 20, Block = 4 },
                Enemies = new System.Collections.Generic.List<EnemyCombatSnapshot>
                {
                    new() { EnemyIndex = 0, CurrentHp = 10, MaxHp = 10, Block = 3 },
                },
            });

            var cues = router.GetSnapshotCues(new CombatSnapshot
            {
                Phase = CombatPhase.EnemyTurn,
                Player = new PlayerCombatSnapshot { CurrentHp = 20, MaxHp = 20, Block = 1 },
                Enemies = new System.Collections.Generic.List<EnemyCombatSnapshot>
                {
                    new() { EnemyIndex = 0, CurrentHp = 10, MaxHp = 10, Block = 1 },
                },
            });

            Assert.That(cues, Does.Contain(PrototypeCombatSoundCue.PlayerHit));
            Assert.That(cues, Does.Contain(PrototypeCombatSoundCue.EnemyHit));
        }

        [Test]
        public void BattleScene_CombatUiView_HasInspectorAudioReferences()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
            var view = Object.FindFirstObjectByType<CombatUiView>(FindObjectsInactive.Include);

            Assert.That(view, Is.Not.Null);

            var serializedView = new SerializedObject(view);
            Assert.That(serializedView.FindProperty("audioSource").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedView.FindProperty("playerHitClip"), Is.Not.Null);
            Assert.That(serializedView.FindProperty("enemyHitClip"), Is.Not.Null);
            Assert.That(serializedView.FindProperty("boardMoveClip"), Is.Not.Null);
            Assert.That(serializedView.FindProperty("boardMergeClip"), Is.Not.Null);
            Assert.That(serializedView.FindProperty("soundVolumeScale").floatValue, Is.EqualTo(3f).Within(0.001f));
        }

        private GameObject CreateOwnedGameObject(string name)
        {
            var gameObject = new GameObject(name);
            ownedObjects.Add(gameObject);
            return gameObject;
        }

        private Image CreateImageChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            ownedObjects.Add(child);
            return child.GetComponent<Image>();
        }

        private TMPro.TMP_Text CreateTextChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            child.transform.SetParent(parent, false);
            ownedObjects.Add(child);
            return child.GetComponent<TMPro.TMP_Text>();
        }

        private PlayerSO CreatePlayerData(int maxHp, int attackPower, params SkillSO[] skills)
        {
            var data = ScriptableObject.CreateInstance<PlayerSO>();
            data.maxHp = maxHp;
            data.attackPower = attackPower;
            data.startingSkills = new System.Collections.Generic.List<SkillSO>(skills);
            ownedObjects.Add(data);
            return data;
        }

        private EnemySO CreateEnemyData(string enemyName, int maxHp, int attackValue)
        {
            var data = ScriptableObject.CreateInstance<EnemySO>();
            data.enemyName = enemyName;
            data.maxHp = maxHp;
            data.attackPower = attackValue;
            data.intentPattern = new System.Collections.Generic.List<EnemyIntent>
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

        private SkillSO CreateSkill(string skillId, string skillName, SkillType skillType, int cost, int power)
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.skillId = skillId;
            skill.skillName = skillName;
            skill.skillType = skillType;
            skill.cost = cost;
            skill.power = power;
            ownedObjects.Add(skill);
            return skill;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            return target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(target);
        }

        private static void AssertHpFillIsRenderable(Image fill)
        {
            Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(fill.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal));
            Assert.That(fill.fillOrigin, Is.EqualTo((int)Image.OriginHorizontal.Left));
            Assert.That(fill.rectTransform.anchorMin.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(fill.rectTransform.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(fill.raycastTarget, Is.False);
        }
    }
}
