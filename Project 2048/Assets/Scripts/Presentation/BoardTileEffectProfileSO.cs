using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project2048.Presentation
{
    [CreateAssetMenu(menuName = "Game/Presentation/Board Tile Effect Profile")]
    public class BoardTileEffectProfileSO : ScriptableObject
    {
        public CombatEffectBinding moveEffect = new();
        public CombatEffectBinding defaultMergeEffect = new();
        public List<BoardTileMergeEffectBinding> mergeEffects = new();

        public CombatEffectBinding ResolveMoveEffect()
        {
            return moveEffect;
        }

        public CombatEffectBinding ResolveMergeEffect(int tileValue)
        {
            foreach (var entry in mergeEffects)
            {
                if (entry != null && entry.tileValue == tileValue && entry.effect != null && entry.effect.HasAnyAsset)
                {
                    return entry.effect;
                }
            }

            return defaultMergeEffect;
        }

        private void OnValidate()
        {
            if (mergeEffects == null)
            {
                mergeEffects = new List<BoardTileMergeEffectBinding>();
                return;
            }

            foreach (var entry in mergeEffects)
            {
                entry?.Normalize();
            }
        }
    }

    [Serializable]
    public class BoardTileMergeEffectBinding
    {
        public int tileValue = 2;
        public CombatEffectBinding effect = new();

        public void Normalize()
        {
            tileValue = Mathf.Clamp(NearestPowerOfTwo(tileValue), 2, 2048);
        }

        private static int NearestPowerOfTwo(int value)
        {
            if (value <= 2)
            {
                return 2;
            }

            var power = 2;
            while (power < value && power < 2048)
            {
                power *= 2;
            }

            var previous = Mathf.Max(2, power / 2);
            return value - previous <= power - value ? previous : power;
        }
    }
}
