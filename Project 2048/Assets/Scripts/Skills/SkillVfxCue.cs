using UnityEngine;
using UnityEngine.Serialization;

namespace Project2048.Skills
{
    [System.Serializable]
    public sealed class SkillVfxCue
    {
        public SkillVfxTrigger trigger;
        public GameObject prefab;

        // 모든 VFX의 스폰 지점(필수). 구 placement를 1:1 이관.
        [FormerlySerializedAs("placement")]
        public VfxEndpoint spawnAt;

        // 투사체·빔·흡수처럼 "이동하는" VFX만 도착점을 가진다.
        // 고정 이펙트(버프/피격)는 useDestination=false → spawnAt에 머문다.
        public bool useDestination;
        public VfxEndpoint destination;

        public VfxFlipMode flipMode;
        public VfxAuthoredFacing authoredFacing;
        public VfxAttachMode attachMode;

        [Min(0f)] public float delaySeconds;        // 연출 잔상용만. 판정 동기화에는 쓰지 말 것(→ Impact 트리거 사용).
        [Min(0f)] public float scale = 1f;           // 1 = use prefab as-is
        public Color tint = Color.clear;             // clear = use prefab as-is
        public float lifetimeOverride = -1f;         // <=0 = prefab/self lifetime

        public bool HasPrefab => prefab != null;
    }
}
