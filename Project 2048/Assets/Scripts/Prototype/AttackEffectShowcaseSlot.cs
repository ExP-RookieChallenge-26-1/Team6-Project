using Project2048.Skills;
using TMPro;
using UnityEngine;

namespace Project2048.Prototype
{
    [ExecuteAlways]
    public sealed class AttackEffectShowcaseSlot : MonoBehaviour
    {
        [SerializeField] private SkillSO skill;
        [SerializeField] private CombatWorldSpriteView worldView;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField, Min(0.25f)] private float replayIntervalSeconds = 2.35f;
        [SerializeField, Min(0f)] private float initialReplayDelaySeconds;

        private float nextReplayTime;

        public SkillSO Skill => skill;

        public void Configure(
            SkillSO assignedSkill,
            CombatWorldSpriteView assignedWorldView,
            TMP_Text assignedTitleText,
            TMP_Text assignedDetailText,
            float assignedReplayIntervalSeconds,
            float assignedInitialReplayDelaySeconds)
        {
            skill = assignedSkill;
            worldView = assignedWorldView;
            titleText = assignedTitleText;
            detailText = assignedDetailText;
            replayIntervalSeconds = Mathf.Max(0.25f, assignedReplayIntervalSeconds);
            initialReplayDelaySeconds = Mathf.Max(0f, assignedInitialReplayDelaySeconds);
            RefreshLabels();
        }

        private void OnEnable()
        {
            RefreshLabels();
            nextReplayTime = Application.isPlaying ? Time.time + initialReplayDelaySeconds : 0f;
        }

        private void OnValidate()
        {
            RefreshLabels();
        }

        private void Update()
        {
            RefreshLabels();
            if (!Application.isPlaying || skill == null || worldView == null)
            {
                return;
            }

            if (Time.time >= nextReplayTime)
            {
                Replay();
            }
        }

        [ContextMenu("Replay Effect")]
        public void Replay()
        {
            if (!Application.isPlaying || skill == null || worldView == null)
            {
                return;
            }

            worldView.PreviewSkillEffect(skill);
            nextReplayTime = Time.time + Mathf.Max(0.25f, replayIntervalSeconds);
        }

        private void RefreshLabels()
        {
            if (titleText != null)
            {
                titleText.text = skill != null ? skill.skillName : "Skill VFX";
            }

            if (detailText != null)
            {
                detailText.text = skill != null
                    ? $"{skill.skillId}  /  {skill.vfxFamily}"
                    : string.Empty;
            }
        }
    }
}
