using UnityEngine;

namespace Project2048.Presentation
{
    public sealed class SkillVfxVisualRoot : MonoBehaviour
    {
        public Transform visualRoot;

        public Transform VisualTransform => visualRoot != null ? visualRoot : transform;
    }
}
