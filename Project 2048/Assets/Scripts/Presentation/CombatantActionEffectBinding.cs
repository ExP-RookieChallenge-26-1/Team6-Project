using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project2048.Presentation
{
    [Serializable]
    public class CombatantActionEffectBinding
    {
        public string actionId;
        public CombatEffectBinding effect = new();

        public static CombatEffectBinding Find(
            IEnumerable<CombatantActionEffectBinding> actionEffects,
            string actionId)
        {
            if (actionEffects == null || string.IsNullOrWhiteSpace(actionId))
            {
                return null;
            }

            CombatEffectBinding selectedEffect = null;
            var matchingEffectCount = 0;
            foreach (var binding in actionEffects)
            {
                if (binding == null ||
                    binding.effect == null ||
                    string.IsNullOrWhiteSpace(binding.actionId))
                {
                    continue;
                }

                if (string.Equals(binding.actionId.Trim(), actionId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    matchingEffectCount++;
                    if (matchingEffectCount == 1 || UnityEngine.Random.Range(0, matchingEffectCount) == 0)
                    {
                        selectedEffect = binding.effect;
                    }
                }
            }

            return selectedEffect;
        }
    }
}
