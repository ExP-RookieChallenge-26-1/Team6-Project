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
        public void FormatSkillLabel_ShowsNameCostAndAffordability()
        {
            var skill = new SkillSnapshot
            {
                DisplayName = "빛 발사",
                Cost = 9,
                Power = 40,
            };

            Assert.That(PrototypeCombatText.FormatSkillLabel(1, skill, canAfford: true), Is.EqualTo("빛 발사\n코스트 0/9"));
            Assert.That(PrototypeCombatText.FormatSkillLabel(1, skill, canAfford: false), Is.EqualTo("빛 발사\n코스트 0/9 - 부족"));
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
            }), Is.EqualTo("공포: 공격 랭크 -6"));

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
        public void FormatResultDescription_ShowsOutcomeHintOnly()
        {
            Assert.That(
                PrototypeCombatText.FormatResultDescription(new CombatSnapshot { Phase = CombatPhase.Victory }),
                Is.EqualTo("보상을 선택하세요"));
            Assert.That(
                PrototypeCombatText.FormatResultDescription(new CombatSnapshot { Phase = CombatPhase.Defeat }),
                Is.EqualTo("전투를 다시 시도하세요"));
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

        [Test]
        public void FormatSkillTooltip_UsesCardStyleWithoutForeignNameOrChangeLabel()
        {
            var skill = new SkillSnapshot
            {
                DisplayName = "방패 밀치기",
                Cost = 5,
                Power = 60,
                Description = "현재 보호막 수치로 계산하여 적에게 위력 60 피해를 준다.",
            };

            Assert.That(
                PrototypeCombatText.FormatSkillTooltip(skill),
                Is.EqualTo("방패 밀치기\n전투 / 공격   위력 60   명중 100   코스트 5\n현재 보호막 수치로 계산하여 적에게 위력 60 피해를 준다."));
            Assert.That(PrototypeCombatText.FormatSkillTooltip(skill), Does.Not.Contain("변경점"));
            Assert.That(PrototypeCombatText.FormatSkillTooltip(skill), Does.Not.Contain("Solar"));
        }

        [Test]
        public void FormatRewardTooltip_UsesSkillDescriptionForSkillRewards()
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            var reward = ScriptableObject.CreateInstance<BattleRewardSO>();
            try
            {
                skill.skillName = "육중한 압박";
                skill.cost = 7;
                skill.power = 80;
                skill.description = "공격력 대신 방어력으로 계산하여 적에게 위력 80 피해를 준다.";
                reward.rewardKind = RewardChoiceKind.LearnSkill;
                reward.skillToLearn = skill;

                Assert.That(PrototypeCombatText.FormatRewardTooltip(reward), Does.Contain("육중한 압박"));
                Assert.That(PrototypeCombatText.FormatRewardTooltip(reward), Does.Contain("방어력으로 계산"));
            }
            finally
            {
                Object.DestroyImmediate(reward);
                Object.DestroyImmediate(skill);
            }
        }
    }
}
