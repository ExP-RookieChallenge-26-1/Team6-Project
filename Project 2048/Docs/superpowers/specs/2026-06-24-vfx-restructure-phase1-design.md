# VFX 구조 개조 — 1단계 설계 (데이터 모델 + 통합 플레이어)

작성일: 2026-06-24
관련 메모리: vfx-restructure-goal, user-designer-programmer

## 배경 / 문제

현재 스킬 VFX는 한 스킬당 6겹 소스(`SkillSO.vfx`(SkillVfxTuning), `vfxPackage`, `vfxFamily`,
`activationEffect`, 흩어진 `vfxScale/Intensity/...`, `CombatWorldVfxProfileSO` 바인딩+뷰 SerializeField)로
겹쳐 있고, 속성마다 우선순위 캐스케이드(~20개 복제 리졸버)가 `CombatWorldSpriteView.cs`(~6700줄)에
손으로 복제돼 있다. "design-time"이라는 이름과 달리 실제로는 런타임 절차적 생성이며, 에디터 빌더가
프리팹을 코드로 찍어내고 매 실행마다 스킬 값을 덮어써 디자이너가 수정할 수 없다.

감사에서 식별한 이상-가능성 핫스팟: 속성별 우선순위 불일치, tuning 존재만으로 기본값 강제 덮어쓰기,
`HasAnySetting` 게이트 함정, 리졸버 오버로드 2벌, 매직스트링 파티클 폴백, 패밀리 머티리얼 폴백,
Rebuild가 인스펙터 편집 덮어쓰기, 패밀리 출처 3중화, 위치/크기값 코드 하드코딩.

## 목표 / 범위 (전면 개조의 1단계)

- 스킬당 단일 `SkillVfxDefinition`(큐 리스트) + 단일 해석 + 프리팹-우선/폴백 플레이어.
- **디자인타임 기반**: 프리팹이 룩의 단일 소스, 데이터는 얇음, 런타임은 Instantiate→Play→Destroy.
- **하위호환**: `vfxDefinition`이 비어 있으면 기존 절차적 경로로 폴백 → 현재 화면 안 깨짐.
- 역할: 엔지니어링은 시스템(데이터 모델 + 플레이어 + 마이그레이션 + 폴백). 프리팹 룩 저작은 디자이너.

비범위(다음 단계): 2단계 = 패밀리 enum switch→데이터 디스크립터 완전 제거, 매직스트링 제거, 폴백 삭제.
3단계 = 에디터 빌더 1회성화, 레거시 필드 제거.

## 1. 데이터 모델 (승인됨)

```
[Serializable] class SkillVfxDefinition { SkillVfxCue[] cues; }   // 단순 스킬은 큐 1개

[Serializable] class SkillVfxCue {
    SkillVfxTrigger   trigger;       // Activate / ChargeStart / ChargeRelease / Impact
    GameObject        prefab;        // 🎨 에디터 저작 룩 (파티클/VFX그래프/프로젝타일)
    SkillVfxPlacement placement;
    float             delaySeconds;  // 트리거 후 추가 지연
    float             scale;         // 0|1 = 그대로
    Color             tint;          // clear = 그대로
    float             lifetimeOverride; // <=0 = 프리팹/자체 수명
}

[Serializable] struct SkillVfxPlacement {
    SkillVfxTarget   target;    // Player / Enemy
    SkillVfxVertical vertical;  // Feet / Body / Head → bounds 하단/중앙/상단
    Vector3          localOffset;
}
```

**거동은 프리팹이 선언**: 투사체는 데이터 플래그 없이, 프리팹에 `CombatProjectileEffect`가 붙어 있으면
플레이어가 반대 앵커로 자동 발사. 정적 이펙트는 앵커에 스폰.

빛 모으기 예시(마이그레이션 후): ①ChargeStart 버프 @Player/Body ②ChargeRelease 파이어볼 @Player→Enemy
③ChargeRelease 빔 @Enemy/Feet (delay=파이어볼 travel).

## 2. 런타임 플레이어 + 해석

- 신규 진입점 `SkillVfxPlayer.Play(SkillVfxDefinition, SkillVfxContext)`, context = { playerAnchor, enemyAnchor, trigger }.
- 트리거에 맞는 큐만 골라 `delaySeconds` 후 재생.
- placement 해석은 **단일 함수** `ResolvePlacementWorldPosition(placement, ctx)`:
  target→앵커, vertical→bounds.min/center/max.y, + localOffset. (기존 ~20 리졸버 대체, 신규 경로 한정)
- 프리팹 스폰 → `CombatProjectileEffect` 있으면 반대 앵커로 Launch, 없으면 정적.
- 오버라이드(scale/tint/lifetime)는 비었으면 프리팹 그대로.
- 수명 종료 시 Destroy. 코루틴 지연은 플레이 모드 한정, 에디트 모드/프리뷰는 동기.

## 3. SkillSO 통합 + 마이그레이션

- `SkillSO.vfxDefinition` 추가. 기존 `vfx/vfxPackage/vfxFamily/vfxPrimaryColor/...`는 `[Obsolete]` + 읽기 유지.
- 1회성 에디터 마이그레이션 메뉴: 추론 가능한 부분을 큐로 변환(activationEffect.vfxPrefab→Activate 큐 등).
  변환 불가/다단계(충전)는 비워 폴백 유지.
- 진입점: 기존 `PlaySkillPresentationEffect`/충전 start·release/impact 지점에서 trigger 부여 후
  `SkillVfxPlayer.Play` 우선 시도, 큐 없으면 기존 절차 경로.

## 4. 에러 처리 / 엣지

- prefab null 큐: 건너뛰고 경고 1회. 전체 cues 비면 폴백.
- 앵커 스프라이트 없음: bounds 못 구하면 `anchor.position`으로 degrade.
- 프로젝타일인데 타깃 없음: 정적 스폰으로 degrade.

## 5. 테스트

- placement 해석 단위테스트: Player/Enemy × Feet/Body/Head → 기대 bounds 위치(스프라이트 픽스처).
- 큐 트리거링: 트리거별로 맞는 큐만 재생.
- 프로젝타일 감지: `CombatProjectileEffect` 프리팹 → Launch 호출.
- 폴백: cues 비면 기존 경로 동작, 기존 EditMode 스위트 회귀 없음.
