using Project2048.Skills;
using UnityEngine;

namespace Project2048.Presentation
{
    // 전투 캐릭터의 VFX 부착 소켓(손/입/무기 머즐/피격 중심)을 명시적으로 제공한다.
    // 지정되지 않은 소켓은 SkillVfxPlayer가 스프라이트 bounds(또는 CastPoint=랜턴 머즐)로 폴백한다.
    public sealed class CombatVfxAnchorProvider : MonoBehaviour
    {
        public Transform root;
        public Transform feet;
        public Transform body;
        public Transform head;
        public Transform castPoint;   // 무기(랜턴) 머즐 — 발사 원점
        public Transform hitPoint;    // 피격 중심

        public bool TryGetSocket(VfxSocket socket, out Transform socketTransform)
        {
            socketTransform = socket switch
            {
                VfxSocket.Root => root,
                VfxSocket.Feet => feet,
                VfxSocket.Body => body,
                VfxSocket.Head => head,
                VfxSocket.CastPoint => castPoint,
                VfxSocket.HitPoint => hitPoint,
                _ => null,
            };
            return socketTransform != null;
        }
    }
}
