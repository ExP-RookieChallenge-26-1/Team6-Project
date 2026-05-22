using NUnit.Framework;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Prototype;
using Project2048.Rewards;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Tests
{
    public class PrototypeCombatTextTests
    {
        [Test]
        public void FormatSkillLabel_ShowsSlotCostPowerAndUsability()
        {
            var skill = new SkillSnapshot
            {
                DisplayName = "빛 발사",
                Cost = 9,
                Power = 4,
            };

            Assert.That(PrototypeCombatText.FormatSkillLabel(1, skill, canAfford: true), Is.EqualTo("2. 빛 발사\n코스트 9 / 위력 4 / 사용 가능"));
            Assert.That(PrototypeCombatText.FormatSkillLabel(1, skill, canAfford: false), Is.EqualTo("2. 빛 발사\n코스트 9 / 위력 4 / 코스트 부족"));
            Assert.That(PrototypeCombatText.FormatEmptySkillSlotLabel(3), Is.EqualTo("4. 빈 슬롯"));
        }

        [Test]
        public void FormatSkillReplacementLabel_ShowsWhichSlotWillBeForgotten()
        {
            var skill = new SkillSnapshot
            {
                DisplayName = "가시 방어",
                Cost = 9,
                Power = 5,
            };

            Assert.That(PrototypeCombatText.FormatSkillReplacementLabel(2, skill), Is.EqualTo("3. 가시 방어\n잊고 새 기술 배우기"));
        }

        [Test]
        public void FormatIntent_UsesFourIntentLabels()
        {
            Assert.That(PrototypeCombatText.FormatIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Attack,
                value = 7,
            }), Is.EqualTo("공격"));

            Assert.That(PrototypeCombatText.FormatIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Defense,
                value = 3,
            }), Is.EqualTo("보호"));

            Assert.That(PrototypeCombatText.FormatIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Debuff,
                debuffType = DebuffType.Darkness,
                value = 2,
            }), Is.EqualTo("섬광"));

            Assert.That(PrototypeCombatText.FormatIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Debuff,
                debuffType = DebuffType.Fear,
                value = 2,
            }), Is.EqualTo("공포"));
        }

        [Test]
        public void FormatIntents_ShowsUpToThreeIntentLines()
        {
            var intents = new[]
            {
                new EnemyIntent { displayName = "빛 발사", intentType = EnemyIntentType.Attack },
                new EnemyIntent { displayName = "가시 방어", intentType = EnemyIntentType.Defense },
                new EnemyIntent { displayName = "공포", intentType = EnemyIntentType.Debuff },
                new EnemyIntent { displayName = "네번째", intentType = EnemyIntentType.Attack },
            };

            Assert.That(PrototypeCombatText.FormatIntents(intents), Is.EqualTo("빛 발사\n가시 방어\n공포"));
        }

        [Test]
        public void FormatActionDescription_LabelsLatestAction()
        {
            Assert.That(PrototypeCombatText.FormatActionDescription("플레이어: 빛 발사"), Is.EqualTo("최근 행동: 플레이어: 빛 발사"));
            Assert.That(PrototypeCombatText.FormatActionDescription(null), Is.EqualTo("최근 행동: 대기"));
        }

        [Test]
        public void FormatEnemyTurnAction_ShowsTheResolvedEnemyAction()
        {
            Assert.That(PrototypeCombatText.FormatEnemyTurnAction("고슴도치 공격"), Is.EqualTo("적 행동\n고슴도치 공격"));
            Assert.That(PrototypeCombatText.FormatEnemyTurnAction(null), Is.EqualTo("적 행동 대기 중"));
        }

        [Test]
        public void FormatDebuffVfxLabel_ExplainsTemporaryDebuffFeedback()
        {
            Assert.That(PrototypeCombatText.FormatDebuffVfxLabel(new CombatVfxCue
            {
                DebuffType = DebuffType.Fear,
                Value = 6,
            }), Is.EqualTo("공포: 방어력 -6"));

            Assert.That(PrototypeCombatText.FormatDebuffVfxLabel(new CombatVfxCue
            {
                DebuffType = DebuffType.Darkness,
                Value = 3,
            }), Is.EqualTo("섬광: 방해 블록 +3"));
        }

        [Test]
        public void FormatEnemyHeader_ShowsAiProfileAboveEnemy()
        {
            Assert.That(
                PrototypeCombatText.FormatEnemyHeader("고슴도치", "AI: 방어 위주 / 엘리트"),
                Is.EqualTo("고슴도치\nAI: 방어 위주 / 엘리트"));
        }

        [Test]
        public void FormatHp_UsesCompactHpNumbersForBars()
        {
            Assert.That(PrototypeCombatText.FormatEnemyHp(18, 32, 0), Is.EqualTo("18/32"));
            Assert.That(PrototypeCombatText.FormatPlayerHp(16, 20, 0), Is.EqualTo("16/20"));
        }

        [Test]
        public void FormatEnemyHeader_CanIncludeHpWhenDedicatedEnemyHpTextIsMissing()
        {
            Assert.That(
                PrototypeCombatText.FormatEnemyHeader(
                    "고슴도치",
                    "AI: 방어 위주 / 일반",
                    "32/32"),
                Is.EqualTo("고슴도치\nAI: 방어 위주 / 일반\n32/32"));
        }

        [Test]
        public void FormatResultTitle_MatchesPrototypeResultUiCopy()
        {
            Assert.That(PrototypeCombatText.FormatResultTitle(CombatPhase.Victory), Is.EqualTo("승리"));
            Assert.That(PrototypeCombatText.FormatResultTitle(CombatPhase.Defeat), Is.EqualTo("패배"));
        }

        [Test]
        public void FormatRewardChoice_UsesRogueliteRewardCopy()
        {
            var reward = ScriptableObject.CreateInstance<BattleRewardSO>();
            try
            {
                reward.rewardKind = RewardChoiceKind.TemporaryBoardMoveCount;
                reward.temporaryBoardMoveCountBonus = 2;

                Assert.That(PrototypeCombatText.FormatRewardChoice(reward), Is.EqualTo("다음 전투 이동 횟수 +2"));
            }
            finally
            {
                Object.DestroyImmediate(reward);
            }
        }
    }
}
