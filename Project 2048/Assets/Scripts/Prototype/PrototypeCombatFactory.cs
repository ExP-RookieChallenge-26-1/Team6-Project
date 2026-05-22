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
            var quickStab = CreateSkill("quick-stab", "Quick Stab", SkillType.Attack, cost: 4, power: 40, targetAttackModifier: 0, selfDefenseBonus: 0, "Deal power 40 damage.");
            quickStab.effectKind = SkillEffectKind.BasicAttack;

            var lightShot = CreateSkill("light-shot", "Light Shot", SkillType.Attack, cost: 6, power: 60, targetAttackModifier: 0, selfDefenseBonus: 0, "Deal power 60 light damage.");
            lightShot.effectKind = SkillEffectKind.BasicAttack;

            var heavyStrike = CreateSkill("heavy-strike", "Heavy Strike", SkillType.Attack, cost: 8, power: 80, targetAttackModifier: 0, selfDefenseBonus: 0, "Deal power 80 damage.");
            heavyStrike.effectKind = SkillEffectKind.BasicAttack;

            var gatherLight = CreateSkill("gather-light", "Gather Light", SkillType.Attack, cost: 6, power: 0, targetAttackModifier: 0, selfDefenseBonus: 0, "Charge this turn. At the next player turn start, automatically use a power 120 light attack.");
            gatherLight.effectKind = SkillEffectKind.ChargeAttack;
            gatherLight.chargedPower = 120;

            var lowStance = CreateSkill("low-stance", "Low Stance", SkillType.Defense, cost: 4, power: 30, targetAttackModifier: 0, selfDefenseBonus: 0, "Gain 30 shield.");
            lowStance.effectKind = SkillEffectKind.BasicDefense;

            var lightGuard = CreateSkill("light-guard", "Light Guard", SkillType.Defense, cost: 6, power: 60, targetAttackModifier: 0, selfDefenseBonus: 0, "Gain 60 shield.");
            lightGuard.effectKind = SkillEffectKind.BasicDefense;

            var shieldBash = CreateSkill("shield-bash", "Shield Bash", SkillType.Attack, cost: 5, power: 60, targetAttackModifier: 0, selfDefenseBonus: 0, "Use current shield as the attacking stat to deal power 60 damage. Shield is not consumed.");
            shieldBash.effectKind = SkillEffectKind.ShieldScalingAttack;
            shieldBash.damageStatSource = DamageStatSource.ShieldHp;

            var shieldBurst = CreateSkill("shield-burst", "Shield Burst", SkillType.Attack, cost: 7, power: 100, targetAttackModifier: 0, selfDefenseBonus: 0, "Use current shield as the attacking stat to deal power 100 damage, then lose all shield.");
            shieldBurst.effectKind = SkillEffectKind.ShieldBurstAttack;
            shieldBurst.damageStatSource = DamageStatSource.ShieldHp;
            shieldBurst.consumesAllShield = true;

            var ironWall = CreateSkill("iron-wall", "Iron Wall", SkillType.Defense, cost: 6, power: 0, targetAttackModifier: 0, selfDefenseBonus: 0, "Raise defense stage by 2.");
            ironWall.effectKind = SkillEffectKind.DefenseStageUp;
            ironWall.selfDefenseStageModifier = 2;

            var bodyPress = CreateSkill("body-press", "Body Press", SkillType.Attack, cost: 7, power: 80, targetAttackModifier: 0, selfDefenseBonus: 0, "Use defense instead of attack to deal power 80 damage.");
            bodyPress.effectKind = SkillEffectKind.DefenseScalingAttack;
            bodyPress.damageStatSource = DamageStatSource.DefensePower;

            var flash = CreateSkill("flash", "Flash", SkillType.Debuff, cost: 5, power: 0, targetAttackModifier: 0, selfDefenseBonus: 0, "Lower enemy attack stage by 1.");
            flash.effectKind = SkillEffectKind.AttackStageDown;
            flash.targetAttackStageModifier = -1;

            var skills = new List<SkillSO>
            {
                quickStab,
                lightShot,
                heavyStrike,
                gatherLight,
                lowStance,
                lightGuard,
                shieldBash,
                shieldBurst,
                ironWall,
                bodyPress,
                flash,
            };

            var player = ScriptableObject.CreateInstance<PlayerSO>();
            player.name = "PrototypePlayer";
            player.maxHp = 100;
            player.attackPower = 2;
            player.baseDefensePower = 2;
            player.criticalChance = 0.1f;
            player.criticalDamageMultiplier = 1.5f;
            player.initialBoardMoveCount = 4;
            player.boardMoveCountBonus = 0;
            player.startingSkills = new List<SkillSO>
            {
                gatherLight,
                lightGuard,
                flash,
                quickStab,
            };

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
            enemy.baseDefensePower = seed.Strength == EnemyAiStrength.Enhanced ? 2 : 1;
            enemy.defensePower = seed.Strength == EnemyAiStrength.Enhanced ? 4 : 3;
            enemy.debuffPower = 1;
            enemy.difficultyScore = seed.Strength == EnemyAiStrength.Enhanced ? 2 : 1;
            enemy.criticalChance = seed.Strength == EnemyAiStrength.Enhanced ? 0.08f : 0.05f;
            enemy.criticalDamageMultiplier = 1.5f;
            enemy.intentPattern = new List<EnemyIntent>();
            enemy.aiActionBias = seed.ActionBias;
            enemy.aiDebuffPattern = seed.DebuffPattern;
            enemy.aiStrength = seed.Strength;
            enemy.aiDebuffInterval = 3;
            enemy.encounterRank = seed.Strength == EnemyAiStrength.Enhanced
                ? EnemyEncounterRank.Elite
                : EnemyEncounterRank.Normal;
            enemy.aiComplexity = seed.Strength == EnemyAiStrength.Enhanced
                ? EnemyAiComplexity.Normal
                : EnemyAiComplexity.Simple;
            enemy.actionsPerTurn = EnemySO.ResolveDefaultActionsPerTurn(enemy.aiComplexity);
            enemy.skills = CreateEnemySkills(seed);
            enemy.canUseThornGuard = seed.ActionBias == EnemyAiActionBias.DefenseHeavy;
            enemy.canUseBullRush = seed.ActionBias == EnemyAiActionBias.AttackHeavy;
            return enemy;
        }

        private static List<SkillSO> CreateEnemySkills(EnemyProfileSeed seed)
        {
            var attack = CreateSkill(
                "enemy-light-shot",
                "빛 발사",
                SkillType.Attack,
                cost: 0,
                power: seed.Strength == EnemyAiStrength.Enhanced ? 80 : 60,
                targetAttackModifier: 0,
                selfDefenseBonus: 0,
                "적 기본 공격.");
            attack.effectKind = SkillEffectKind.BasicAttack;

            var guard = CreateSkill(
                "enemy-light-guard",
                "빛 방어",
                SkillType.Defense,
                cost: 0,
                power: seed.Strength == EnemyAiStrength.Enhanced ? 60 : 40,
                targetAttackModifier: 0,
                selfDefenseBonus: 0,
                "적 기본 보호.");
            guard.effectKind = SkillEffectKind.BasicDefense;

            var debuff = CreateSkill(
                seed.DebuffPattern == EnemyDebuffPattern.DarknessThenFear ? "enemy-howl" : "enemy-fear",
                seed.DebuffPattern == EnemyDebuffPattern.DarknessThenFear ? "울부짖기" : "공포",
                SkillType.Debuff,
                cost: 0,
                power: 0,
                targetAttackModifier: 0,
                selfDefenseBonus: 0,
                "플레이어 방어력을 낮춘다.");
            debuff.effectKind = SkillEffectKind.DefenseStageDown;
            debuff.targetDefenseStageModifier = seed.Strength == EnemyAiStrength.Enhanced ? -2 : -1;

            if (seed.ActionBias == EnemyAiActionBias.AttackHeavy)
            {
                var rush = CreateSkill(
                    "enemy-tentacle-strike",
                    "촉수 치기",
                    SkillType.Attack,
                    cost: 0,
                    power: seed.Strength == EnemyAiStrength.Enhanced ? 110 : 90,
                    targetAttackModifier: 0,
                    selfDefenseBonus: 0,
                    "강하게 공격하고 다음 보드 이동을 줄인다.");
                rush.effectKind = SkillEffectKind.BoardMovePenaltyAttack;
                rush.nextBoardMoveCountModifier = -1;
                return new List<SkillSO> { attack, rush, guard, debuff };
            }

            if (seed.ActionBias == EnemyAiActionBias.DefenseHeavy)
            {
                var thorn = CreateSkill(
                    "enemy-thorn-guard",
                    "가시 방어",
                    SkillType.Defense,
                    cost: 0,
                    power: seed.Strength == EnemyAiStrength.Enhanced ? 60 : 40,
                    targetAttackModifier: 0,
                    selfDefenseBonus: 0,
                    "보호막과 반사 피해.");
                thorn.effectKind = SkillEffectKind.ThornGuard;
                thorn.selfThornRetaliationDamage = 40;
                return new List<SkillSO> { attack, thorn, guard, debuff };
            }

            return new List<SkillSO> { attack, guard, debuff };
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
            skill.isEnemySkill = skillId.StartsWith("enemy-", System.StringComparison.Ordinal);
            skill.cost = cost;
            skill.power = power;
            skill.targetAttackModifier = targetAttackModifier;
            skill.selfDefenseBonus = selfDefenseBonus;
            skill.description = description;
            return skill;
        }
    }
}
