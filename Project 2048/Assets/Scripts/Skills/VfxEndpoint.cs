using UnityEngine;
using UnityEngine.Serialization;

namespace Project2048.Skills
{
    // VFX의 한 끝점: 어느 액터(시전자/주대상)의 어느 소켓에서, 얼마만큼 오프셋된 지점인가.
    // spawnAt(필수)과 destination(투사체·빔 등 선택)에 동일하게 쓰인다.
    [System.Serializable]
    public struct VfxEndpoint
    {
        [FormerlySerializedAs("target")]
        public VfxActorRef actor;

        [FormerlySerializedAs("vertical")]
        public VfxSocket socket;

        public Vector3 localOffset;

        public bool mirrorOffsetXWithCastDirection;
    }
}
