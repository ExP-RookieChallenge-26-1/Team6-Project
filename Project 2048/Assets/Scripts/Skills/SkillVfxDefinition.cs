using System.Linq;
using UnityEngine;

namespace Project2048.Skills
{
    [System.Serializable]
    public sealed class SkillVfxDefinition
    {
        public SkillVfxCue[] cues = System.Array.Empty<SkillVfxCue>();

        public bool HasAnyCue => cues != null && cues.Any(c => c != null && c.HasPrefab);

        public bool HasCuesFor(SkillVfxTrigger trigger) =>
            cues != null && cues.Any(c => c != null && c.HasPrefab && c.trigger == trigger);

        public System.Collections.Generic.IEnumerable<SkillVfxCue> CuesFor(SkillVfxTrigger trigger)
        {
            if (cues == null) yield break;
            foreach (var cue in cues)
            {
                if (cue != null && cue.HasPrefab && cue.trigger == trigger)
                {
                    yield return cue;
                }
            }
        }
    }
}
