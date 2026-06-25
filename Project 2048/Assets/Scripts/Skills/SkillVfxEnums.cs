namespace Project2048.Skills
{
    public enum SkillVfxTrigger { Activate, ChargeStart, ChargeRelease, Impact }

    // 같은 SkillSO를 플레이어와 적이 공유한다. 시전자/주대상 기준이라 소유자에 종속되지 않는다.
    // 정수 순서는 구 SkillVfxTarget(Player=0, Enemy=1)을 보존 → 기존 .asset 데이터 호환.
    public enum VfxActorRef { Caster = 0, PrimaryTarget = 1 }

    // 앵커 위의 부착 소켓. 구 SkillVfxVertical(Feet=0, Body=1, Head=2)의 정수값을 보존하며 확장.
    public enum VfxSocket { Feet = 0, Body = 1, Head = 2, Root = 3, CastPoint = 4, HitPoint = 5 }
}
