using System.Collections.Generic;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Prototype
{
    public static class PrototypeCombatFactory
    {
        private readonly struct EnemyProfileSeed
        {
            public EnemyProfileSeed(
                string name,
                EnemyAiActionBias actionBias,
                EnemyDebuffPattern debuffPattern,
                EnemyAiStrength strength)
            {
                Name = name;
                ActionBias = actionBias;
                DebuffPattern = debuffPattern;
                Strength = strength;
            }

            public string Name { get; }
            public EnemyAiActionBias ActionBias { get; }
            public EnemyDebuffPattern DebuffPattern { get; }
            public EnemyAiStrength Strength { get; }
        }

        public static PrototypeCombatLoadout CreateDefaultLoadout()
        {
            var attack1 = CreateSkill(
                "attack_1",
                "1단계 공격",
                SkillType.Attack,
                cost: 5,
                power: 3,
                targetAttackModifier: 0,
                selfDefenseBonus: 0,
                "기본 공격.");
            var attack2 = CreateSkill(
                "attack_2",
                "2단계 공격",
                SkillType.Attack,
                cost: 8,
                power: 4,
                targetAttackModifier: -2,
                selfDefenseBonus: 0,
                "공격하고 적 공격력을 낮춘다.");
            var attack3 = CreateSkill(
                "attack_3",
                "3단계 공격",
                SkillType.Attack,
                cost: 12,
                power: 8,
                targetAttackModifier: 0,
                selfDefenseBonus: 0,
                "강한 공격.");
            var defense1 = CreateSkill(
                "defense_1",
                "1단계 방어",
                SkillType.Defense,
                cost: 5,
                power: 3,
                targetAttackModifier: 0,
                selfDefenseBonus: 0,
                "방어도 3을 얻는다.");
            var defense2 = CreateSkill(
                "defense_2",
                "2단계 방어",
                SkillType.Defense,
                cost: 8,
                power: 4,
                targetAttackModifier: 0,
                selfDefenseBonus: 2,
                "방어도를 얻고 이후 획득 방어도를 증가시킨다.");
            var defense3 = CreateSkill(
                "defense_3",
                "3단계 방어",
                SkillType.Defense,
                cost: 12,
                power: 10,
                targetAttackModifier: 0,
                selfDefenseBonus: 0,
                "강한 방어.");

            var skills = new List<SkillSO>
            {
                attack1,
                attack2,
                attack3,
                defense1,
                defense2,
                defense3,
            };

            var player = ScriptableObject.CreateInstance<PlayerSO>();
            player.name = "PrototypePlayer";
            player.maxHp = 30;
            player.attackPower = 2;
            player.boardMoveCountBonus = 0;
            player.startingSkills = new List<SkillSO>(skills);

            var enemy = CreateRandomPrototypeEnemy();

            return new PrototypeCombatLoadout(player, enemy, skills, ownsAssets: true);
        }

        public static EnemySO CreateRandomPrototypeEnemy()
        {
            var roster = GetEnemyProfileSeeds();
            var index = Random.Range(0, roster.Length);
            return CreateEnemy(roster[index]);
        }

        public static List<EnemySO> CreatePrototypeEnemyRoster()
        {
            var enemies = new List<EnemySO>();
            foreach (var seed in GetEnemyProfileSeeds())
            {
                enemies.Add(CreateEnemy(seed));
            }

            return enemies;
        }

        private static EnemyProfileSeed[] GetEnemyProfileSeeds()
        {
            return new[]
            {
                new EnemyProfileSeed("붉은 송곳니", EnemyAiActionBias.AttackHeavy, EnemyDebuffPattern.FearThenDarkness, EnemyAiStrength.Normal),
                new EnemyProfileSeed("검은 송곳니", EnemyAiActionBias.AttackHeavy, EnemyDebuffPattern.DarknessThenFear, EnemyAiStrength.Normal),
                new EnemyProfileSeed("공포 파수꾼", EnemyAiActionBias.DefenseHeavy, EnemyDebuffPattern.FearThenDarkness, EnemyAiStrength.Normal),
                new EnemyProfileSeed("암흑 파수꾼", EnemyAiActionBias.DefenseHeavy, EnemyDebuffPattern.DarknessThenFear, EnemyAiStrength.Normal),
                new EnemyProfileSeed("황혼 추적자", EnemyAiActionBias.Balanced, EnemyDebuffPattern.FearThenDarkness, EnemyAiStrength.Normal),
                new EnemyProfileSeed("그림자 추적자", EnemyAiActionBias.Balanced, EnemyDebuffPattern.DarknessThenFear, EnemyAiStrength.Normal),
                new EnemyProfileSeed("분노의 발톱", EnemyAiActionBias.AttackHeavy, EnemyDebuffPattern.FearThenDarkness, EnemyAiStrength.Normal),
                new EnemyProfileSeed("칠흑 방패", EnemyAiActionBias.DefenseHeavy, EnemyDebuffPattern.DarknessThenFear, EnemyAiStrength.Normal),
                new EnemyProfileSeed("강화 붉은 송곳니", EnemyAiActionBias.AttackHeavy, EnemyDebuffPattern.FearThenDarkness, EnemyAiStrength.Enhanced),
                new EnemyProfileSeed("강화 검은 송곳니", EnemyAiActionBias.AttackHeavy, EnemyDebuffPattern.DarknessThenFear, EnemyAiStrength.Enhanced),
                new EnemyProfileSeed("강화 공포 파수꾼", EnemyAiActionBias.DefenseHeavy, EnemyDebuffPattern.FearThenDarkness, EnemyAiStrength.Enhanced),
                new EnemyProfileSeed("강화 그림자 추적자", EnemyAiActionBias.Balanced, EnemyDebuffPattern.DarknessThenFear, EnemyAiStrength.Enhanced),
            };
        }

        private static EnemySO CreateEnemy(EnemyProfileSeed seed)
        {
            var enemy = ScriptableObject.CreateInstance<EnemySO>();
            enemy.name = seed.Name;
            enemy.enemyName = seed.Name;
            enemy.maxHp = seed.Strength == EnemyAiStrength.Enhanced ? 40 : 32;
            enemy.attackPower = seed.Strength == EnemyAiStrength.Enhanced ? 6 : 5;
            enemy.defensePower = seed.Strength == EnemyAiStrength.Enhanced ? 4 : 3;
            enemy.debuffPower = 1;
            enemy.difficultyScore = seed.Strength == EnemyAiStrength.Enhanced ? 2 : 1;
            enemy.intentPattern = new List<EnemyIntent>();
            enemy.aiActionBias = seed.ActionBias;
            enemy.aiDebuffPattern = seed.DebuffPattern;
            enemy.aiStrength = seed.Strength;
            enemy.aiDebuffInterval = 3;
            return enemy;
        }

        private static SkillSO CreateSkill(
            string skillId,
            string skillName,
            SkillType skillType,
            int cost,
            int power,
            int targetAttackModifier,
            int selfDefenseBonus,
            string description)
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.name = skillId;
            skill.skillId = skillId;
            skill.skillName = skillName;
            skill.skillType = skillType;
            skill.cost = cost;
            skill.power = power;
            skill.targetAttackModifier = targetAttackModifier;
            skill.selfDefenseBonus = selfDefenseBonus;
            skill.description = description;
            return skill;
        }
    }
}
