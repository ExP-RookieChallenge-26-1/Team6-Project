using NUnit.Framework;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Prototype;
using Project2048.Skills;

namespace Project2048.Tests
{
    public class PrototypeCombatTextTests
    {
        [Test]
        public void FormatSkillLabel_ShowsTierNameCostAndPower()
        {
            var skill = new SkillSnapshot
            {
                DisplayName = "2단계 공격",
                Cost = 8,
                Power = 4,
            };

            Assert.That(PrototypeCombatText.FormatSkillLabel(1, skill), Is.EqualTo("2단계 · 2단계 공격 (코스트 8 / 위력 4)"));
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
            }), Is.EqualTo("방어"));

            Assert.That(PrototypeCombatText.FormatIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Debuff,
                debuffType = DebuffType.Darkness,
                value = 2,
            }), Is.EqualTo("암흑"));

            Assert.That(PrototypeCombatText.FormatIntent(new EnemyIntent
            {
                intentType = EnemyIntentType.Debuff,
                debuffType = DebuffType.Fear,
                value = 2,
            }), Is.EqualTo("공포"));
        }

        [Test]
        public void FormatActionDescription_LabelsLatestAction()
        {
            Assert.That(PrototypeCombatText.FormatActionDescription("플레이어: 1단계 공격"), Is.EqualTo("최근 행동: 플레이어: 1단계 공격"));
            Assert.That(PrototypeCombatText.FormatActionDescription(null), Is.EqualTo("최근 행동: 대기"));
        }

        [Test]
        public void FormatEnemyTurnAction_ShowsTheResolvedEnemyAction()
        {
            Assert.That(PrototypeCombatText.FormatEnemyTurnAction("슬라임: 공격"), Is.EqualTo("적 턴\n슬라임: 공격"));
            Assert.That(PrototypeCombatText.FormatEnemyTurnAction(null), Is.EqualTo("적 턴\n행동 대기 중"));
        }

        [Test]
        public void FormatDebuffVfxLabel_ExplainsTemporaryDebuffFeedback()
        {
            Assert.That(PrototypeCombatText.FormatDebuffVfxLabel(new CombatVfxCue
            {
                DebuffType = DebuffType.Fear,
                Value = 2,
            }), Is.EqualTo("공포: 방어도 획득 절반"));

            Assert.That(PrototypeCombatText.FormatDebuffVfxLabel(new CombatVfxCue
            {
                DebuffType = DebuffType.Darkness,
                Value = 3,
            }), Is.EqualTo("암흑: 방해 블록 +3"));
        }

        [Test]
        public void FormatEnemyHeader_ShowsAiProfileAboveEnemy()
        {
            Assert.That(
                PrototypeCombatText.FormatEnemyHeader("그림자 늑대", "AI: 공격 몰빵 / 암흑->공포 / 강화"),
                Is.EqualTo("그림자 늑대\nAI: 공격 몰빵 / 암흑->공포 / 강화"));
        }

        [Test]
        public void FormatEnemyHp_ShowsBlockWhenEnemyIsDefending()
        {
            Assert.That(PrototypeCombatText.FormatEnemyHp(18, 32, 0), Is.EqualTo("체력 18/32"));
            Assert.That(PrototypeCombatText.FormatEnemyHp(32, 32, 5), Is.EqualTo("체력 32/32"));
        }

        [Test]
        public void FormatPlayerHp_ShowsBlockWhenDamageWouldBeAbsorbed()
        {
            Assert.That(PrototypeCombatText.FormatPlayerHp(16, 20, 0), Is.EqualTo("체력 16/20"));
            Assert.That(PrototypeCombatText.FormatPlayerHp(20, 20, 3), Is.EqualTo("체력 20/20"));
        }

        [Test]
        public void FormatEnemyHeader_CanIncludeHpWhenDedicatedEnemyHpTextIsMissing()
        {
            Assert.That(
                PrototypeCombatText.FormatEnemyHeader(
                    "그림자 늑대",
                    "AI: 방어 몰빵 / 공포->암흑 / 일반",
                    "체력 32/32"),
                Is.EqualTo("그림자 늑대\nAI: 방어 몰빵 / 공포->암흑 / 일반\n체력 32/32"));
        }

        [Test]
        public void FormatResultTitle_MatchesPrototypeResultUiCopy()
        {
            Assert.That(PrototypeCombatText.FormatResultTitle(CombatPhase.Victory), Is.EqualTo("클리어!"));
            Assert.That(PrototypeCombatText.FormatResultTitle(CombatPhase.Defeat), Is.EqualTo("죽었습니다!"));
        }
    }
}
