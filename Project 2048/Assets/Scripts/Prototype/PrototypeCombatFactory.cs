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
            quickStab.availability = SkillAvailability.Shared;

            var lightShot = CreateSkill("light-shot", "Light Shot", SkillType.Attack, cost: 6, power: 60, targetAttackModifier: 0, selfDefenseBonus: 0, "Deal power 60 light damage.");
            lightShot.effectKind = SkillEffectKind.BasicAttack;
            lightShot.availability = SkillAvailability.PlayerOnly;

            var heavyStrike = CreateSkill("heavy-strike", "Heavy Strike", SkillType.Attack, cost: 8, power: 80, targetAttackModifier: 0, selfDefenseBonus: 0, "Deal power 80 damage.");
            heavyStrike.effectKind = SkillEffectKind.BasicAttack;
            heavyStrike.availability = SkillAvailability.Shared;

            var gatherLight = CreateSkill("gather-light", "Gather Light", SkillType.Attack, cost: 6, power: 0, targetAttackModifier: 0, selfDefenseBonus: 0, "Charge this turn. At the next player turn start, automatically use a power 120 light attack.");
            gatherLight.effectKind = SkillEffectKind.ChargeAttack;
            gatherLight.chargedPower = 120;
            gatherLight.availability = SkillAvailability.PlayerOnly;

            var lowStance = CreateSkill("low-stance", "Low Stance", SkillType.Defense, cost: 4, power: 30, targetAttackModifier: 0, selfDefenseBonus: 0, "Gain 30 shield.");
            lowStance.effectKind = SkillEffectKind.BasicDefense;
            lowStance.availability = SkillAvailability.Shared;

            var lightGuard = CreateSkill("light-guard", "Light Guard", SkillType.Defense, cost: 6, power: 60, targetAttackModifier: 0, selfDefenseBonus: 0, "Gain 60 shield.");
            lightGuard.effectKind = SkillEffectKind.BasicDefense;
            lightGuard.availability = SkillAvailability.PlayerOnly;

            var shieldBash = CreateSkill("shield-bash", "Shield Bash", SkillType.Attack, cost: 5, power: 60, targetAttackModifier: 0, selfDefenseBonus: 0, "Use current shield as the attacking stat to deal power 60 damage. Shield is not consumed.");
            shieldBash.effectKind = SkillEffectKind.ShieldScalingAttack;
            shieldBash.damageStatSource = DamageStatSource.ShieldHp;
            shieldBash.availability = SkillAvailability.Shared;

            var shieldBurst = CreateSkill("shield-burst", "Shield Burst", SkillType.Attack, cost: 7, power: 100, targetAttackModifier: 0, selfDefenseBonus: 0, "Use current shield as the attacking stat to deal power 100 damage, then lose all shield.");
            shieldBurst.effectKind = SkillEffectKind.ShieldBurstAttack;
            shieldBurst.damageStatSource = DamageStatSource.ShieldHp;
            shieldBurst.consumesAllShield = true;
            shieldBurst.availability = SkillAvailability.Shared;

            var ironWall = CreateSkill("iron-wall", "Iron Wall", SkillType.Defense, cost: 6, power: 0, targetAttackModifier: 0, selfDefenseBonus: 0, "Raise defense stage by 2.");
            ironWall.effectKind = SkillEffectKind.DefenseStageUp;
            ironWall.selfDefenseStageModifier = 2;
            ironWall.availability = SkillAvailability.Shared;

            var bodyPress = CreateSkill("body-press", "Body Press", SkillType.Attack, cost: 7, power: 80, targetAttackModifier: 0, selfDefenseBonus: 0, "Use defense instead of attack to deal power 80 damage.");
            bodyPress.effectKind = SkillEffectKind.DefenseScalingAttack;
            bodyPress.damageStatSource = DamageStatSource.DefensePower;
            bodyPress.availability = SkillAvailability.Shared;

            var flash = CreateSkill("flash", "Flash", SkillType.Debuff, cost: 5, power: 0, targetAttackModifier: 0, selfDefenseBonus: 0, "Lower enemy attack stage by 1.");
            flash.effectKind = SkillEffectKind.AttackStageDown;
            flash.targetAttackStageModifier = -1;
            flash.availability = SkillAvailability.Shared;

            var bleedingCut = CreateBleedingCut(cost: 6);
            var poisonCoat = CreatePoisonCoat(cost: 5);
            var openWound = CreateOpenWound(cost: 7);
            var execute = CreateExecute(cost: 6);
            var sealSkill = CreateSealSkill(cost: 6);
            var crackBrand = CreateCrackBrand(cost: 5);

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
                bleedingCut,
                poisonCoat,
                openWound,
                execute,
                sealSkill,
                crackBrand,
            };

            var player = ScriptableObject.CreateInstance<PlayerSO>();
            player.name = "PrototypePlayer";
            player.maxHp = 240;
            player.attackPower = 10;
            player.baseDefensePower = 2;
            player.criticalChance = 0.1f;
            player.criticalDamageMultiplier = 1.5f;
            player.initialBoardMoveCount = 4;
            player.boardMoveCountBonus = 0;
            player.startingSkills = new List<SkillSO>
            {
                lightShot,
                lowStance,
                flash,
                gatherLight,
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
            enemy.maxHp = seed.Strength == EnemyAiStrength.Enhanced ? 210 : 160;
            enemy.attackPower = seed.Strength == EnemyAiStrength.Enhanced ? 6 : 5;
            enemy.baseDefensePower = seed.Strength == EnemyAiStrength.Enhanced ? 2 : 1;
            enemy.defensePower = seed.Strength == EnemyAiStrength.Enhanced ? 4 : 3;
            enemy.debuffPower = 1;
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
                seed.Strength == EnemyAiStrength.Enhanced ? "heavy-strike" : "quick-stab",
                "빛 발사",
                SkillType.Attack,
                cost: 0,
                power: seed.Strength == EnemyAiStrength.Enhanced ? 80 : 60,
                targetAttackModifier: 0,
                selfDefenseBonus: 0,
                "적 기본 공격.");
            attack.effectKind = SkillEffectKind.BasicAttack;
            attack.availability = SkillAvailability.Shared;
            attack.skillName = seed.Strength == EnemyAiStrength.Enhanced ? "Heavy Strike" : "Quick Stab";

            var guard = CreateSkill(
                "low-stance",
                "빛 방어",
                SkillType.Defense,
                cost: 0,
                power: seed.Strength == EnemyAiStrength.Enhanced ? 60 : 40,
                targetAttackModifier: 0,
                selfDefenseBonus: 0,
                "적 기본 보호.");
            guard.effectKind = SkillEffectKind.BasicDefense;
            guard.availability = SkillAvailability.Shared;
            guard.skillName = "Low Stance";

            var debuff = CreateSkill(
                "howl",
                seed.DebuffPattern == EnemyDebuffPattern.DarknessThenFear ? "울부짖기" : "공포",
                SkillType.Debuff,
                cost: 0,
                power: 0,
                targetAttackModifier: 0,
                selfDefenseBonus: 0,
                "플레이어 방어력을 낮춘다.");
            debuff.effectKind = SkillEffectKind.DefenseStageDown;
            debuff.targetDefenseStageModifier = seed.Strength == EnemyAiStrength.Enhanced ? -2 : -1;
            debuff.availability = SkillAvailability.Shared;
            debuff.skillName = "Howl";

            var bleedingCut = CreateBleedingCut(cost: 0);
            var poisonCoat = CreatePoisonCoat(cost: 0);
            var openWound = CreateOpenWound(cost: 0);
            var execute = CreateExecute(cost: 0);
            var sealSkill = CreateSealSkill(cost: 0);
            var crackBrand = CreateCrackBrand(cost: 0);
            var blackCorrosion = CreateBlackCorrosion();

            if (seed.ActionBias == EnemyAiActionBias.AttackHeavy)
            {
                var rush = CreateSkill(
                    "dark-shackle",
                    "촉수 치기",
                    SkillType.Attack,
                    cost: 0,
                    power: seed.Strength == EnemyAiStrength.Enhanced ? 60 : 40,
                    targetAttackModifier: 0,
                    selfDefenseBonus: 0,
                    "강하게 공격하고 다음 보드 이동을 줄인다.");
                rush.effectKind = SkillEffectKind.BoardMovePenaltyAttack;
                rush.nextBoardMoveCountModifier = -1;
                rush.availability = SkillAvailability.EnemyOnly;
                rush.skillName = "Dark Shackle";
                return seed.Strength == EnemyAiStrength.Enhanced
                    ? new List<SkillSO> { execute, rush, bleedingCut, guard }
                    : new List<SkillSO> { attack, rush, bleedingCut, guard };
            }

            if (seed.ActionBias == EnemyAiActionBias.DefenseHeavy)
            {
                var thorn = CreateSkill(
                    "thorn-guard",
                    "가시 방어",
                    SkillType.Defense,
                    cost: 0,
                    power: seed.Strength == EnemyAiStrength.Enhanced ? 60 : 40,
                    targetAttackModifier: 0,
                    selfDefenseBonus: 0,
                    "보호막과 반사 피해.");
                thorn.effectKind = SkillEffectKind.ThornGuard;
                thorn.selfThornRetaliationDamage = 40;
                thorn.availability = SkillAvailability.Shared;
                thorn.skillName = "Thorn Guard";
                return seed.Strength == EnemyAiStrength.Enhanced
                    ? new List<SkillSO> { thorn, sealSkill, blackCorrosion, attack }
                    : new List<SkillSO> { thorn, crackBrand, guard, attack };
            }

            return seed.Strength == EnemyAiStrength.Enhanced
                ? new List<SkillSO> { poisonCoat, openWound, execute, guard }
                : new List<SkillSO> { poisonCoat, openWound, attack, guard };
        }

        private static SkillSO CreateBleedingCut(int cost)
        {
            var skill = CreateSkill("bleeding-cut", "Bleeding Cut", SkillType.Attack, cost, 50, 0, 0, "Deal power 50 damage and inflict bleed.");
            skill.effectKind = SkillEffectKind.BleedAttack;
            skill.statusDuration = 2;
            skill.statusDamage = 20;
            skill.availability = SkillAvailability.Shared;
            return skill;
        }

        private static SkillSO CreatePoisonCoat(int cost)
        {
            var skill = CreateSkill("poison-coat", "Poison Coat", SkillType.Attack, cost, 30, 0, 0, "Deal power 30 damage and inflict poison.");
            skill.effectKind = SkillEffectKind.PoisonAttack;
            skill.statusDuration = 3;
            skill.statusMaxHpDamagePercent = 0.05f;
            skill.availability = SkillAvailability.Shared;
            return skill;
        }

        private static SkillSO CreateOpenWound(int cost)
        {
            var skill = CreateSkill("open-wound", "Open Wound", SkillType.Attack, cost, 70, 0, 0, "Deal power 70 damage, stronger against poison or bleed.");
            skill.effectKind = SkillEffectKind.OpenWoundAttack;
            skill.conditionalPowerBonus = 50;
            skill.statusDuration = 1;
            skill.availability = SkillAvailability.Shared;
            return skill;
        }

        private static SkillSO CreateExecute(int cost)
        {
            var skill = CreateSkill("execute", "Execute", SkillType.Attack, cost, 40, 0, 0, "Deal power 40 damage, doubled against low health targets.");
            skill.effectKind = SkillEffectKind.ExecuteAttack;
            skill.conditionalHpThreshold = 0.3f;
            skill.conditionalPowerBonus = 40;
            skill.availability = SkillAvailability.Shared;
            return skill;
        }

        private static SkillSO CreateSealSkill(int cost)
        {
            var skill = CreateSkill("seal-skill", "Seal", SkillType.Debuff, cost, 0, 0, 0, "Seal the target's last non-basic skill next turn.");
            skill.effectKind = SkillEffectKind.SealSkill;
            skill.statusDuration = 1;
            skill.availability = SkillAvailability.Shared;
            return skill;
        }

        private static SkillSO CreateCrackBrand(int cost)
        {
            var skill = CreateSkill("crack-brand", "Crack Brand", SkillType.Debuff, cost, 0, 0, 0, "Mark the target so the next hit deals bonus damage.");
            skill.effectKind = SkillEffectKind.CrackBrand;
            skill.statusDamage = 40;
            skill.availability = SkillAvailability.Shared;
            return skill;
        }

        private static SkillSO CreateBlackCorrosion()
        {
            var skill = CreateSkill("black-corrosion", "Black Corrosion", SkillType.Debuff, 0, 0, 0, 0, "Reduce the player's next cost gain by 3.");
            skill.effectKind = SkillEffectKind.CostGainDown;
            skill.nextCostGainModifier = -3;
            skill.availability = SkillAvailability.EnemyOnly;
            return skill;
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
            ConfigureReusableVfx(skill);
            return skill;
        }

        private static void ConfigureReusableVfx(SkillSO skill)
        {
            switch (skill.skillId)
            {
                case "quick-stab":
                    ConfigureReusableVfx(skill, SkillVfxFamily.SlashArc, new Color(1f, 1f, 1f), new Color(0.75f, 0.82f, 0.9f), 0.7f, 0.8f);
                    break;
                case "heavy-strike":
                    ConfigureReusableVfx(skill, SkillVfxFamily.SpikedBurst, new Color(1f, 0.72f, 0.08f), new Color(0.72f, 0.04f, 0.02f), 1.3f, 1.35f);
                    break;
                case "flow-strike":
                    ConfigureReusableVfx(skill, SkillVfxFamily.SlashArc, new Color(0.25f, 0.68f, 1f), new Color(0.78f, 0.95f, 1f));
                    break;
                case "tentacle-strike":
                    ConfigureReusableVfx(skill, SkillVfxFamily.TentacleWhip, new Color(0.2f, 0.04f, 0.28f), new Color(0.55f, 0.18f, 0.72f), 1.1f, 1.1f);
                    break;
                case "reckless-blow":
                    ConfigureReusableVfx(skill, SkillVfxFamily.FlameBurst, new Color(1f, 0.28f, 0.04f), new Color(0.52f, 0.035f, 0.015f), 0.95f, 1.1f);
                    break;
                case "light-shot":
                    ConfigureReusableVfx(skill, SkillVfxFamily.LightProjectile, new Color(1f, 0.96f, 0.62f), Color.white, intensity: 1.1f);
                    break;
                case "gather-light":
                    ConfigureReusableVfx(skill, SkillVfxFamily.LightBeam, new Color(0.86f, 0.96f, 1f), Color.white, 1.8f, 1.4f);
                    break;
                case "light-guard":
                    ConfigureReusableVfx(skill, SkillVfxFamily.ShieldDome, new Color(1f, 0.86f, 0.28f), new Color(0.45f, 0.86f, 1f), 1.2f, 1.15f);
                    break;
                case "light-recover":
                    ConfigureReusableVfx(skill, SkillVfxFamily.BuffAura, new Color(1f, 0.84f, 0.25f), new Color(1f, 1f, 0.88f), intensity: 1.1f);
                    break;
                case "light-echo":
                    ConfigureReusableVfx(skill, SkillVfxFamily.SupportFire, new Color(1f, 0.76f, 0.18f), new Color(1f, 0.95f, 0.58f), 1f, 1.2f, 3);
                    break;
                case "light-split":
                    ConfigureReusableVfx(skill, SkillVfxFamily.BuffAura, new Color(1f, 0.8f, 0.22f), Color.white, repeatCount: 2);
                    break;
                case "low-stance":
                    ConfigureReusableVfx(skill, SkillVfxFamily.ShieldDome, new Color(0.58f, 0.68f, 0.82f), new Color(0.82f, 0.9f, 1f), 0.9f, 0.85f);
                    break;
                case "thorn-guard":
                    ConfigureReusableVfx(skill, SkillVfxFamily.ShieldDome, new Color(0.05f, 0.22f, 0.16f), new Color(0.46f, 0.1f, 0.08f), 1.1f, 1.15f);
                    break;
                case "shield-bash":
                    ConfigureReusableVfx(skill, SkillVfxFamily.ShieldDome, new Color(0.28f, 0.58f, 1f), new Color(0.78f, 0.86f, 0.95f), intensity: 1.05f);
                    break;
                case "shield-burst":
                    ConfigureReusableVfx(skill, SkillVfxFamily.ShieldDome, new Color(0.72f, 0.9f, 1f), new Color(0.2f, 0.46f, 1f), 1.4f, 1.3f);
                    break;
                case "iron-wall":
                    ConfigureReusableVfx(skill, SkillVfxFamily.BuffAura, new Color(0.42f, 0.55f, 0.72f), new Color(0.82f, 0.86f, 0.9f));
                    break;
                case "body-press":
                    ConfigureReusableVfx(skill, SkillVfxFamily.ImpactBurst, new Color(0.62f, 0.62f, 0.62f), new Color(0.28f, 0.34f, 0.42f), 1.2f, 1.2f);
                    break;
                case "flash":
                    ConfigureReusableVfx(skill, SkillVfxFamily.DebuffWave, Color.white, new Color(1f, 0.9f, 0.32f), intensity: 1.2f);
                    break;
                case "howl":
                    ConfigureReusableVfx(skill, SkillVfxFamily.DebuffWave, new Color(0.55f, 0.28f, 0.82f), new Color(0.5f, 0.5f, 0.58f), 1.1f);
                    break;
                case "intimidating-shot":
                    ConfigureReusableVfx(skill, SkillVfxFamily.DebuffWave, new Color(0.58f, 0.04f, 0.04f), new Color(0.12f, 0.02f, 0.02f), intensity: 1.1f);
                    break;
                case "life-drain":
                    ConfigureReusableVfx(skill, SkillVfxFamily.DrainTether, new Color(0.18f, 0.82f, 0.34f), new Color(0.02f, 0.08f, 0.04f), intensity: 1.1f);
                    break;
                case "blood-fang":
                    ConfigureReusableVfx(skill, SkillVfxFamily.DrainTether, new Color(0.86f, 0.04f, 0.05f), new Color(0.28f, 0f, 0.02f), 1.1f, 1.15f);
                    break;
                case "bioluminescence":
                    ConfigureReusableVfx(skill, SkillVfxFamily.DrainTether, new Color(0.22f, 1f, 0.88f), Color.white, 1.4f, 1.5f);
                    break;
                case "counter":
                    ConfigureReusableVfx(skill, SkillVfxFamily.CounterReady, new Color(0.95f, 0.08f, 0.04f), new Color(1f, 0.42f, 0.12f), intensity: 1.2f);
                    break;
                case "endure":
                    ConfigureReusableVfx(skill, SkillVfxFamily.CounterReady, new Color(0.92f, 0.92f, 0.9f), new Color(0.55f, 0.56f, 0.58f));
                    break;
                case "focus-breath":
                    ConfigureReusableVfx(skill, SkillVfxFamily.BuffAura, Color.white, new Color(0.55f, 0.82f, 1f), 0.8f, 0.9f);
                    break;
                case "sharp-senses":
                    ConfigureReusableVfx(skill, SkillVfxFamily.BuffAura, new Color(0.62f, 0.24f, 1f), new Color(0.45f, 0.86f, 1f), intensity: 1.15f);
                    break;
                case "bleeding-cut":
                    ConfigureReusableVfx(skill, SkillVfxFamily.BloodFountainSlash, new Color(0.95f, 0.02f, 0.04f), new Color(0.34f, 0f, 0.015f), 1.1f, 1.3f);
                    break;
                case "poison-coat":
                    ConfigureReusableVfx(skill, SkillVfxFamily.SlashArc, new Color(0.22f, 0.82f, 0.22f), new Color(0.08f, 0.24f, 0.06f), 0.95f, 1.05f);
                    break;
                case "open-wound":
                    ConfigureReusableVfx(skill, SkillVfxFamily.ImpactBurst, new Color(0.95f, 0.18f, 0.1f), new Color(0.38f, 0.02f, 0.02f), 1.2f, 1.15f);
                    break;
                case "overburn":
                    ConfigureReusableVfx(skill, SkillVfxFamily.FlameBurst, new Color(1f, 0.42f, 0.08f), new Color(0.45f, 0.04f, 0.02f), 1.2f, 1.45f);
                    break;
                case "execute":
                    ConfigureReusableVfx(skill, SkillVfxFamily.SlashArc, new Color(0.95f, 0.95f, 0.95f), new Color(0.18f, 0.02f, 0.02f), 1.25f, 1.2f);
                    break;
                case "seal-skill":
                    ConfigureReusableVfx(skill, SkillVfxFamily.DebuffWave, new Color(0.65f, 0.58f, 0.95f), new Color(0.16f, 0.08f, 0.32f), 1f, 1.05f);
                    break;
                case "crack-brand":
                    ConfigureReusableVfx(skill, SkillVfxFamily.DebuffWave, new Color(0.9f, 0.42f, 0.14f), new Color(0.26f, 0.06f, 0.02f), 1.05f, 1.15f);
                    break;
                case "darkness":
                    ConfigureReusableVfx(skill, SkillVfxFamily.BoardDisturb, new Color(0.02f, 0.02f, 0.04f), new Color(0.28f, 0.08f, 0.45f), intensity: 1.1f);
                    break;
                case "deep-darkness":
                    ConfigureReusableVfx(skill, SkillVfxFamily.BoardDisturb, new Color(0f, 0f, 0.02f), new Color(0.22f, 0.02f, 0.38f), 1.4f, 1.4f);
                    break;
                case "dark-shackle":
                    ConfigureReusableVfx(skill, SkillVfxFamily.DarkChainBurst, new Color(0.02f, 0.02f, 0.04f), new Color(0.5f, 0.04f, 0.18f), 1.2f, 1.2f);
                    break;
                case "black-pressure":
                    ConfigureReusableVfx(skill, SkillVfxFamily.BoardDisturb, new Color(0.01f, 0.01f, 0.03f), new Color(0.04f, 0.12f, 0.32f), 1.1f, 1.15f);
                    break;
                case "black-corrosion":
                    ConfigureReusableVfx(skill, SkillVfxFamily.BoardDisturb, new Color(0.01f, 0.04f, 0.03f), new Color(0.05f, 0.2f, 0.12f), 1.1f, 1.15f);
                    break;
            }
        }

        private static void ConfigureReusableVfx(
            SkillSO skill,
            SkillVfxFamily family,
            Color primaryColor,
            Color secondaryColor,
            float scale = 1f,
            float intensity = 1f,
            int repeatCount = 1)
        {
            skill.vfxFamily = family;
            skill.vfxPrimaryColor = primaryColor;
            skill.vfxSecondaryColor = secondaryColor;
            skill.vfxScale = Mathf.Max(0.01f, scale);
            skill.vfxIntensity = Mathf.Max(0f, intensity);
            skill.vfxRepeatCount = Mathf.Max(1, repeatCount);
        }
    }
}
