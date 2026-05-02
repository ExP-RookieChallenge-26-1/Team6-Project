using System;
using System.Collections.Generic;
using Project2048.Combat;
using Project2048.Enemy;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Prototype
{
    public sealed class PrototypeCombatLoadout : IDisposable
    {
        public PrototypeCombatLoadout(PlayerSO playerData, EnemySO enemyData, List<SkillSO> skills, bool ownsAssets)
        {
            PlayerData = playerData;
            EnemyData = enemyData;
            Skills = skills ?? new List<SkillSO>();
            OwnsAssets = ownsAssets;
        }

        public PlayerSO PlayerData { get; }
        public EnemySO EnemyData { get; }
        public List<SkillSO> Skills { get; }
        public bool OwnsAssets { get; }

        public void Dispose()
        {
            if (!OwnsAssets)
            {
                return;
            }

            if (PlayerData != null)
            {
                DestroyObject(PlayerData);
            }

            if (EnemyData != null)
            {
                DestroyObject(EnemyData);
            }

            foreach (var skill in Skills)
            {
                if (skill != null)
                {
                    DestroyObject(skill);
                }
            }
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
