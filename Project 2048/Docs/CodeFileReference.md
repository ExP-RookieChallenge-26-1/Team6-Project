# Project 2048 코드별 설명서

이 문서는 `Assets` 아래 C# 코드 파일을 하나씩 설명한다. 처음 보는 사람이 "이 파일이 왜 있는지", "어디와 연결되는지", "고칠 때 뭘 조심해야 하는지"를 빠르게 잡는 용도다.

UI와 사운드는 이 작업의 담당 범위가 아니다. `Prototype`과 `Editor/CombatUiBuilder.cs`는 전투 루프를 눈과 귀로 확인하기 위해 임시로 이런 식으로 구현한 예시다. 정식 UI/사운드가 들어오면 전투 코어는 유지하고 이 임시 UI/사운드 쪽만 교체할 수 있다.

코스트 변환표도 테스트와 전투 루프 검증을 위한 임시 수치다. 정식 밸런스가 정해지면 `CostConverter.cs`의 표와 관련 테스트 기대값을 같이 바꾸면 된다.

## 빠른 지도

| 구역 | 파일 수 | 역할 |
|---|---:|---|
| `Assets/Scripts/Board2048` | 4 | 2048 보드, 이동, 병합, 코스트 변환 |
| `Assets/Scripts/Combat` | 9 | 전투 흐름, 플레이어 상태, snapshot, 전투 시작/종료 |
| `Assets/Scripts/Cost` | 1 | 행동 코스트 보관과 소비 |
| `Assets/Scripts/Enemy` | 11 | 적 상태, 인텐트, AI 브레인, AI 타입 표시, 디버프 |
| `Assets/Scripts/Skills` | 3 | 스킬 데이터와 실행 |
| `Assets/Scripts/Prototype` | 9 | 전투 확인용 임시 UI, 임시 사운드 cue, 임시 데이터 |
| `Assets/Editor` | 1 | 임시 UI 생성 메뉴 |
| `Assets/Tests/EditMode` | 13 | 전투 규칙과 외부 연결 계약 검증 |

## Board2048 코드

### `Assets/Scripts/Board2048/Board2048Manager.cs`

2048 보드의 실제 규칙을 담당한다.

- 4x4 `int[,]` 보드를 가진다.
- `Move(Direction)`으로 상하좌우 이동을 처리한다.
- 같은 숫자 병합, 방해 블록, 새 타일 생성, 이동 횟수 감소를 처리한다.
- 이동 횟수가 0이 되면 `OnBoardFinished`를 발생시켜 전투가 코스트 계산으로 넘어가게 한다.

연결:

- `CombatManager`가 플레이어의 보드 입력을 이 파일로 넘긴다.
- `CombatUiView`는 `BoardTransition` 이벤트를 받아 임시 타일 이동 애니메이션을 보여준다.
- `EnemyIntentSystem`의 `Darkness` 디버프가 `QueueObstacles`로 방해 블록을 예약한다.

조심할 점:

- `board[row, col]`과 `Vector2Int(x: col, y: row)` 좌표가 다르다.
- 막힌 방향 입력은 이동 횟수를 쓰면 안 된다.
- 한 번 이동한 타일이 같은 이동 안에서 두 번 합쳐지면 안 된다.
- 방해 블록 `-1`은 벽처럼 취급한다.

### `Assets/Scripts/Board2048/BoardTransition.cs`

보드가 움직일 때 UI 애니메이션에 필요한 정보를 담는 데이터 파일이다.

- `BoardTransition`은 이동 전 보드, 이동 후 보드, 방향, 이동 목록, 새 타일 목록을 담는다.
- `BoardTileMovement`는 어떤 타일이 어디서 어디로 갔는지 담는다.
- `BoardTileSpawn`은 새로 생긴 타일 위치와 값을 담는다.

연결:

- `Board2048Manager`가 이동 성공 시 만들어서 `OnBoardTransitioned`로 발행한다.
- `CombatUiView`가 이 데이터를 받아 임시 애니메이션 오버레이를 만든다.

조심할 점:

- 이 파일은 애니메이션용 설명 데이터다. 실제 보드 규칙은 `Board2048Manager.cs`에 있다.

### `Assets/Scripts/Board2048/CostConverter.cs`

보드 숫자를 행동 코스트로 바꾸는 파일이다.

- `ConvertTileToCost`는 타일 하나를 코스트로 바꾼다.
- `ConvertBoardToCost`는 보드 전체 타일의 코스트를 합산한다.
- 현재 변환표는 테스트와 전투 루프 검증을 위한 임시 수치다.

연결:

- `CombatManager.ResolveBoardPhase()`가 보드 종료 시 이 파일을 호출한다.
- `CostConverterTests.cs`가 현재 임시 표와 합산 방식을 검증한다.

조심할 점:

- 가장 큰 타일 하나만 계산하는 방식이 아니다. 보드 전체 숫자를 합산한다.
- 정식 밸런스로 바꾸면 `CostConverterTests.cs`도 같이 바꿔야 한다.

### `Assets/Scripts/Board2048/Direction.cs`

2048 이동 방향 enum이다.

- `Up`, `Down`, `Left`, `Right` 네 방향만 있다.

연결:

- UI 입력, 테스트, 보드 이동 함수가 모두 이 enum을 쓴다.

조심할 점:

- 방향을 추가하면 `Board2048Manager`의 줄 추출/쓰기 로직도 같이 바꿔야 한다.

## Combat 코드

### `Assets/Scripts/Combat/CombatManager.cs`

전투의 중심 관리자다.

- 전투 시작과 초기화를 처리한다.
- 플레이어 턴, 보드 phase, 행동 phase, 적 턴, 승리, 패배를 전환한다.
- UI가 쓸 수 있는 command 메서드와 `CombatSnapshot` 이벤트를 제공한다.
- 보드가 끝나면 코스트를 만들고, 스킬을 쓰면 코스트를 소비하고, 턴 종료 시 적 인텐트를 실행한다.
- 디버프 인텐트가 실행되면 `CombatVfxCue`를 만들어 UI에 임시 VFX 신호를 보낸다.

주요 외부 연결점:

```csharp
GetSnapshot()
OnCombatStateChanged
RequestBoardMove(Direction direction)
RequestUseSkillById(string skillId, int targetIndex)
RequestEndPlayerTurn()
```

연결:

- `Board2048Manager`, `ActionCostWallet`, `SkillExecutor`, `EnemyIntentSystem`, `DamageCalculator`를 묶는다.
- 임시 UI는 `CombatUiView`에서 이 파일의 command와 snapshot만 쓴다.
- 테스트는 `CombatManagerTests.cs`와 `CombatUiContractTests.cs`에서 전투 계약을 검증한다.

조심할 점:

- UI 배치, 버튼 색, 사운드, 저장 로직을 넣지 않는다.
- 외부 UI가 내부 컨트롤러를 직접 만지지 않아도 되도록 snapshot/command 경계를 유지한다.
- `StartCombat` 초기화 중에는 중간 snapshot이 여러 번 나가지 않게 `suppressStateNotifications`를 쓴다.

### `Assets/Scripts/Combat/CombatPhase.cs`

전투가 지금 어느 단계인지 나타내는 enum이다.

- `None`
- `CombatStart`
- `PlayerTurnStart`
- `BoardPhase`
- `ActionPhase`
- `EnemyTurn`
- `Victory`
- `Defeat`

연결:

- `CombatManager`가 phase를 바꾼다.
- `PrototypeCombatUiState`가 phase를 보고 임시 UI 패널을 고른다.
- 테스트가 phase 전환을 검증한다.

조심할 점:

- 새 phase를 추가하면 `CombatManager`, `PrototypeCombatUiState`, 테스트를 같이 확인해야 한다.

### `Assets/Scripts/Combat/CombatResult.cs`

전투 승리 시 결과를 담는 간단한 데이터다.

- 진행 턴 수
- 남은 보드 이동 횟수
- 남은 코스트
- 적 난이도 점수 합

연결:

- `CombatManager`가 승리 이벤트 `OnCombatVictory`에 넘긴다.

조심할 점:

- 현재는 결과 저장이나 보상 시스템에 연결되어 있지 않다.

### `Assets/Scripts/Combat/CombatSetup.cs`

전투를 시작할 때 필요한 입력 데이터다.

- 플레이어 데이터 `PlayerSO`
- 적 데이터 목록 `List<EnemySO>`
- 기본 보드 이동 횟수

연결:

- `PrototypeCombatBootstrap`이 만들어서 `CombatManager.StartCombat`에 넘긴다.
- 테스트도 전투를 시작할 때 이 데이터를 직접 만든다.

조심할 점:

- 적 데이터 수보다 실제 `EnemyController` 수가 적으면 `CombatManager`가 예외를 낸다.

### `Assets/Scripts/Combat/CombatSnapshot.cs`

외부 UI와 테스트가 읽는 전투 상태 묶음이다.

- `CombatSnapshot`은 현재 phase, 코스트, 보드, 플레이어, 적, 스킬 정보를 담는다.
- `PlayerCombatSnapshot`은 플레이어 HP, 공격력, 방어도, 방어 보너스를 담는다.
- `EnemyCombatSnapshot`은 적 표시 이름, HP, 방어도, 사망 여부, 인텐트를 담는다.
- `EnemyCombatSnapshot.AiProfileLabel`은 적 머리 위에 표시할 AI 타입 문구를 담는다.
- `SkillSnapshot`은 UI가 버튼을 만들 때 필요한 스킬 정보를 담는다.
- `CombatVfxCue`는 디버프 발동 VFX를 한 번만 틀 수 있게 순번, 디버프 종류, 수치를 담는다.

연결:

- `CombatManager.GetSnapshot()`이 만든다.
- UI는 이 DTO를 읽고 화면을 그린다.
- `CombatUiContractTests.cs`가 이 계약을 검증한다.

조심할 점:

- snapshot은 외부 표시용 데이터다. 외부 코드가 runtime controller를 직접 수정하게 만들면 이 경계가 깨진다.
- `LastVfxCue`는 마지막 디버프 발동 신호다. UI는 `Sequence`를 보고 이미 처리한 VFX를 다시 틀지 않아야 한다.

### `Assets/Scripts/Combat/DamageCalculator.cs`

피해량 계산만 담당하는 작은 파일이다.

- 플레이어 스킬 피해는 `플레이어 공격력 + 스킬 위력`이다.
- 적 피해는 현재 인텐트의 `value`다.

연결:

- `SkillExecutor`가 플레이어 공격 피해를 계산할 때 쓴다.
- `EnemyIntentSystem`이 적 공격 피해를 계산할 때 쓴다.

조심할 점:

- 정식 공식이 생기면 이 파일에 모으는 편이 낫다.

### `Assets/Scripts/Combat/PlayerCombatController.cs`

전투 중 플레이어 상태를 가진다.

- HP, 공격력, 방어도, 방어 보너스, 보드 이동 보너스를 관리한다.
- 시작 스킬 목록을 가진다.
- 피해를 받을 때 방어도를 먼저 깎고 남은 피해를 HP에 적용한다.
- 방어 스킬 사용 시 방어 보너스를 반영한다.
- 공포가 걸려 있으면 이번 턴 방어도 획득량을 고정으로 6 줄인다.

연결:

- `CombatManager`가 초기화하고 이벤트를 구독한다.
- `SkillExecutor`가 방어도와 방어 보너스를 바꾼다.
- `EnemyIntentSystem`이 피해나 디버프를 적용한다.

조심할 점:

- 다음 플레이어 턴이 시작되면 `CombatManager.StartPlayerTurn()`에서 현재 방어도만 지운다.
- 공포는 적 턴에 적용되어 다음 플레이어 턴 동안 유지되고, 플레이어가 `RequestEndPlayerTurn()`으로 턴을 넘기면 지워진다.
- 방어 보너스는 별도 값이라 턴 시작 때 자동으로 지워지지 않는다.

### `Assets/Scripts/Combat/PlayerSO.cs`

플레이어 기본 데이터를 담는 ScriptableObject다.

- 최대 HP
- 공격력
- 보드 이동 횟수 보너스
- 초상화
- 시작 스킬 목록

연결:

- `PlayerCombatController.Init`이 이 데이터를 읽는다.
- `CombatUiView`가 임시 UI 초상화 표시용으로 참조한다.

조심할 점:

- `OnValidate`가 음수 HP/공격력/보너스를 막는다.

### `Assets/Scripts/Combat/TurnController.cs`

턴 수만 관리하는 작은 클래스다.

- `Reset`으로 0으로 돌아간다.
- `StartPlayerTurn` 때 턴 수가 1 증가한다.
- `StartEnemyTurn`은 현재 비어 있다.

연결:

- `CombatManager`가 턴 전환 때 호출한다.
- 임시 UI는 턴 숫자를 표시할 때 읽는다.

조심할 점:

- 현재 턴 카운트는 플레이어 턴 기준이다.

## Cost 코드

### `Assets/Scripts/Cost/ActionCostWallet.cs`

플레이어가 현재 가진 행동 코스트를 관리한다.

- 코스트 설정, 추가, 소비, 초기화를 처리한다.
- 코스트가 바뀌면 `OnCostChanged` 이벤트를 낸다.
- 음수 코스트가 되지 않도록 막는다.

연결:

- `CombatManager`가 보드 종료 시 코스트를 설정한다.
- 스킬 사용 시 `CombatManager`가 `CanSpend`와 `Spend`를 호출한다.
- UI snapshot의 `CurrentCost`가 이 값에서 나온다.

조심할 점:

- 스킬 비용이 0이면 소비 가능하다.
- 부족한 코스트로 `Spend`를 호출하면 값이 바뀌지 않는다.

## Enemy 코드

### `Assets/Scripts/Enemy/EnemyAiActionBias.cs`

패턴이 없는 적의 공격/방어 선택 성향을 나타낸다.

- `Balanced`는 공격과 방어를 비슷하게 고른다.
- `AttackHeavy`는 공격을 더 자주 고른다.
- `DefenseHeavy`는 방어를 더 자주 고른다.

연결:

- `EnemySO.aiActionBias`에 저장된다.
- `EnemyAiBrain`이 공격/방어 가중치를 정할 때 읽는다.

조심할 점:

- 이 값은 확정 행동표가 아니라 확률 가중치다.
- 정확한 행동 순서가 필요하면 `EnemySO.intentPattern`을 써야 한다.

### `Assets/Scripts/Enemy/EnemyAiBrain.cs`

고정 인텐트 패턴이 없는 적의 다음 행동을 만든다.

- `aiDebuffInterval`마다 디버프 인텐트를 만든다.
- 디버프가 아닌 턴에는 `aiActionBias` 가중치로 공격 또는 방어를 고른다.
- `aiDebuffPattern`으로 공포/암흑 디버프 순서를 정한다.
- `aiStrength`가 강화형이면 생성된 인텐트 수치를 1.5배로 올린다.

연결:

- `EnemyIntentSystem`이 `intentPattern`이 비어 있을 때 이 파일을 호출한다.
- `EnemySO`의 AI 설정값을 읽는다.
- `EnemyAiBrainTests.cs`가 가중치, 디버프 순서, 강화형 수치를 검증한다.

조심할 점:

- 고정 패턴이 있는 적은 이 브레인을 쓰지 않는다.
- 랜덤 선택은 테스트에서 `System.Random`을 주입해 재현 가능하게 검증한다.
- 현재 80:20 가중치와 1.5배 강화 수치는 프로토타입 기준이다.

### `Assets/Scripts/Enemy/EnemyAiProfileFormatter.cs`

적 AI 설정을 머리 위 표시용 한국어 문구로 바꾼다.

- 공격/방어 성향을 `공격 몰빵`, `방어 몰빵`, `밸런스`로 표시한다.
- 디버프 순서를 `공포->암흑`, `암흑->공포`로 표시한다.
- 일반형/강화형을 `일반`, `강화`로 표시한다.

연결:

- `EnemySO.GetAiProfileLabel()`이 이 파일을 호출한다.
- `CombatSnapshot.EnemyCombatSnapshot.AiProfileLabel`로 UI에 전달된다.
- `CombatUiView`가 적 이름과 함께 머리 위에 표시한다.

조심할 점:

- 이 파일은 표시 문구만 담당한다. 실제 AI 행동 선택은 `EnemyAiBrain.cs`에 있다.

### `Assets/Scripts/Enemy/EnemyAiStrength.cs`

같은 AI 설정을 쓰는 일반형과 강화형을 구분한다.

- `Normal`은 기본 수치를 그대로 쓴다.
- `Enhanced`는 생성된 공격, 방어, 디버프 수치를 1.5배로 올린다.

연결:

- `EnemySO.aiStrength`에 저장된다.
- `EnemyAiBrain`이 인텐트 수치를 만들 때 읽는다.

조심할 점:

- 강화형은 행동 성향을 바꾸는 값이 아니다. 수치만 올린다.

### `Assets/Scripts/Enemy/EnemyDebuffPattern.cs`

AI가 디버프 턴에 공포와 암흑을 어떤 순서로 낼지 나타낸다.

- `FearThenDarkness`는 공포, 암흑 순서로 반복한다.
- `DarknessThenFear`는 암흑, 공포 순서로 반복한다.

연결:

- `EnemySO.aiDebuffPattern`에 저장된다.
- `EnemyAiBrain`이 디버프 인텐트의 `DebuffType`을 정할 때 읽는다.

조심할 점:

- 디버프 자체 효과는 `EnemyIntentSystem.ApplyDebuff`에 있다.
- 이 파일은 순서만 정한다.

### `Assets/Scripts/Enemy/DebuffType.cs`

적 디버프 종류 enum이다.

- `None`
- `Fear`
- `Darkness`

연결:

- `EnemyIntent`가 디버프 타입을 가진다.
- `EnemyIntentSystem`이 타입별 효과를 실행한다.

조심할 점:

- 디버프를 추가하면 `EnemyIntentSystem.ApplyDebuff`, 텍스트 표시, 테스트를 같이 바꿔야 한다.

### `Assets/Scripts/Enemy/EnemyController.cs`

전투 중 적 하나의 상태를 관리한다.

- HP, 방어도, 공격력 보정, 현재 인텐트를 가진다.
- 피해를 받을 때 방어도부터 깎는다.
- HP가 0이 되면 `OnDead`를 발생시킨다.
- 공격력 보정이 바뀌면 공격 인텐트 표시값도 갱신한다.

연결:

- `CombatManager`가 초기화하고 이벤트를 구독한다.
- `SkillExecutor`가 적에게 피해와 공격력 보정을 준다.
- `EnemyIntentSystem`이 방어도와 인텐트를 갱신한다.

조심할 점:

- `CurrentIntent`는 `baseIntent`를 복사해서 만든 표시값이다.
- 공격력 보정은 공격 인텐트에만 반영된다.

### `Assets/Scripts/Enemy/EnemyIntent.cs`

적의 다음 행동 데이터를 담는다.

- 인텐트 타입
- 수치
- 디버프 타입
- `Clone()`으로 복사본을 만든다.

연결:

- `EnemySO.intentPattern`에 들어간다.
- `EnemyController.CurrentIntent`에 저장된다.
- `EnemyIntentSystem`이 실행한다.

조심할 점:

- 같은 인텐트 객체를 공유하면 값 변경이 번질 수 있으므로 복사해서 쓰는 구조를 유지한다.

### `Assets/Scripts/Enemy/EnemyIntentSystem.cs`

적의 다음 행동을 정하고 적 턴에 실행한다.

- `SetNextIntent`는 적 데이터의 패턴을 순서대로 반복한다.
- 패턴이 없으면 `EnemyAiBrain`으로 다음 행동을 만든다.
- `ExecuteIntent`는 공격, 방어, 디버프를 실행한다.
- `Fear`는 플레이어 방어 보너스를 낮춘다.
- `Darkness`는 다음 보드에 방해 블록을 예약한다.

연결:

- `CombatManager`가 플레이어 턴 시작 전후로 인텐트를 준비하고 적 턴에 실행한다.
- `EnemyController`에 현재 인텐트를 저장한다.
- `Board2048Manager`와 `PlayerCombatController`에 효과를 적용한다.

조심할 점:

- 인텐트는 미리 보여주는 값과 실제 실행값이 어긋나면 안 된다.
- 적마다 패턴 진행 위치를 따로 기억한다.
- 고정 패턴이 있는 적은 AI 브레인을 타지 않는다.
- 디버프 VFX 신호는 이 파일이 아니라 `CombatManager`가 snapshot 경계에서 만든다.

### `Assets/Scripts/Enemy/EnemyIntentType.cs`

적 인텐트 타입 enum이다.

- `Attack`
- `Defense`
- `Debuff`

연결:

- `EnemyIntent`, `EnemyIntentSystem`, 임시 UI 텍스트 표시가 모두 이 타입을 본다.

조심할 점:

- 타입을 추가하면 실행 로직과 표시 텍스트도 같이 필요하다.

### `Assets/Scripts/Enemy/EnemySO.cs`

적 기본 데이터를 담는 ScriptableObject다.

- 표시 이름
- 최대 HP
- 공격력
- 방어 인텐트 수치
- 디버프 인텐트 수치
- 난이도 점수
- 초상화
- 인텐트 패턴
- AI 공격/방어 성향
- AI 디버프 순서
- AI 일반형/강화형 구분
- AI 디버프 주기

연결:

- `EnemyController.Init`이 이 데이터를 읽는다.
- `EnemyIntentSystem`이 인텐트 패턴을 읽는다.
- `EnemyAiBrain`이 AI 설정값을 읽는다.
- `CombatUiView`가 임시 UI 초상화를 표시할 때 쓴다.

조심할 점:

- `intentPattern`에 값이 있으면 AI 브레인보다 고정 패턴을 우선한다.
- `OnValidate`가 음수 HP/공격력/방어력/디버프 수치/난이도 점수를 막는다.

## Skills 코드

### `Assets/Scripts/Skills/SkillExecutor.cs`

스킬 실행을 담당한다.

- 공격 스킬은 적에게 피해를 준다.
- 공격 스킬에 `targetAttackModifier`가 있으면 적 공격력을 조정한다.
- 방어 스킬은 플레이어에게 방어도를 준다.
- 방어 스킬에 `selfDefenseBonus`가 있으면 이후 방어 획득량을 조정한다.

연결:

- `CombatManager.RequestUseSkill`이 코스트 확인 후 호출한다.
- `DamageCalculator`, `PlayerCombatController`, `EnemyController`와 연결된다.

조심할 점:

- 코스트 소비는 이 파일이 아니라 `CombatManager`와 `ActionCostWallet`에서 한다.

### `Assets/Scripts/Skills/SkillSO.cs`

스킬 데이터를 담는 ScriptableObject다.

- 스킬 ID
- 표시 이름
- 스킬 타입
- 코스트
- 위력
- 대상 공격력 보정
- 자기 방어 보너스
- 아이콘
- 설명

연결:

- `PlayerSO.startingSkills`에 들어간다.
- `CombatSnapshot.SkillSnapshot`으로 UI에 노출된다.
- `SkillExecutor`가 실제 효과를 실행한다.

조심할 점:

- `skillId`는 UI command에서 찾는 키다. 중복되면 버튼 입력이 꼬일 수 있다.
- 현재 수치는 프로토타입 검증용이다.

### `Assets/Scripts/Skills/SkillType.cs`

스킬 타입 enum이다.

- `Attack`
- `Defense`
- `Debuff`
- `Heal`

연결:

- 현재 실행 구현은 `Attack`과 `Defense` 중심이다.
- 임시 UI도 공격/방어 카테고리만 보여준다.

조심할 점:

- `Debuff`, `Heal`은 enum에는 있지만 현재 실행/임시 UI 흐름이 완성되어 있지 않다.

## Prototype 코드

### `Assets/Scripts/Prototype/BoardCellView.cs`

임시 UI에서 2048 보드 칸 하나를 표시한다.

- 숫자 텍스트를 표시한다.
- 빈 칸, 일반 타일, 큰 타일, 방해 블록 색을 바꾼다.
- 병합 시 짧은 펄스 애니메이션을 실행한다.

연결:

- `CombatUiView`가 snapshot의 보드 값을 읽어 `SetValue`를 호출한다.

조심할 점:

- 이 파일은 임시 UI 표현용이다. 보드 규칙은 `Board2048Manager.cs`에 있다.

### `Assets/Scripts/Prototype/BoardSwipeHandler.cs`

임시 UI 보드 영역에서 드래그/스와이프 입력을 방향으로 바꾼다.

- 터치와 마우스 드래그를 처리한다.
- 최소 이동 거리보다 짧으면 입력으로 보지 않는다.
- 방향을 계산해서 `OnSwipe` 이벤트를 낸다.

연결:

- `CombatUiView`가 `OnSwipe`를 받아 `RequestBoardMove`를 호출한다.

조심할 점:

- PC 방향키 입력은 이 파일이 아니라 `CombatUiView.Update`에서 처리한다.

### `Assets/Scripts/Prototype/CombatUiView.cs`

전투 확인용 임시 UI의 중심 스크립트다.

- `CombatManager.OnCombatStateChanged`를 구독한다.
- 받은 snapshot을 화면에 그린다.
- `CombatVfxCue`를 받으면 공포/암흑별 임시 VFX를 한 번 재생한다.
- `PrototypeCombatAudioRouter`가 만든 임시 사운드 cue를 `AudioSource.PlayOneShot`으로 재생한다.
- 적 이름과 AI 타입 라벨을 적 머리 위에 표시한다.
- 적 HP 텍스트에는 방어도가 있으면 `방어 N`을 함께 표시한다.
- 전용 적 HP 텍스트가 없는 오래된 임시 씬에서는 적 머리 위 라벨에 체력과 방어도를 함께 표시한다.
- 보드, 액션 선택, 적 턴, 결과 오버레이 패널을 전환한다.
- 스킬 버튼, 턴 종료 버튼, 재시작 버튼을 command에 연결한다.
- `BoardTransition`을 받아 임시 타일 이동 애니메이션을 재생한다.

연결:

- `PrototypeCombatBootstrap`이 초기화한다.
- `CombatManager`, `BoardSwipeHandler`, `BoardCellView`, `PrototypeCombatText`, `PrototypeCombatUiState`, `PrototypeCombatAudioRouter`와 연결된다.

조심할 점:

- 이 파일은 담당 범위의 정식 UI가 아니다. 전투 루프 확인용 임시 구현이다.
- 공포/암흑 VFX도 정식 연출이 아니라 디버프 발동 확인용 임시 구현이다.
- 사운드도 정식 사운드 시스템이 아니라 플레이 확인용 임시 구현이다. 클립은 인스펙터에서 교체하고, 전투 코어에는 오디오 코드를 넣지 않는다.
- 정식 UI를 만들 때도 `CombatManager`와 연결하는 방식은 snapshot/command 구조를 유지하면 된다.

### `Assets/Scripts/Prototype/PrototypeCombatAudioRouter.cs`

전투 변화와 보드 이동 데이터를 임시 사운드 cue로 바꾼다.

- 이전 `CombatSnapshot`과 새 `CombatSnapshot`을 비교한다.
- 플레이어 HP가 줄거나 적 턴 중 플레이어 방어도가 줄면 `PlayerHit` cue를 만든다.
- 적 HP가 줄거나 적 방어도가 줄면 `EnemyHit` cue를 만든다.
- 보드 이동이 있으면 `BoardMove` cue를 만든다.
- 보드 이동 중 병합 참여 타일이 있으면 `BoardMerge` cue를 추가한다.

연결:

- `CombatUiView`가 snapshot 갱신과 `BoardTransition` 수신 시 호출한다.
- `CombatUiView`가 cue에 맞는 인스펙터 `AudioClip`을 골라 `AudioSource.PlayOneShot(clip, soundVolumeScale)`로 재생한다.
- `CombatUiViewTests.cs`가 HP/방어도 감소, 보드 이동, 병합 cue를 검증한다.

조심할 점:

- 이 파일은 정식 사운드 시스템이 아니다. 전투 코어에 사운드를 넣지 않으려고 둔 임시 라우터다.
- cue는 "무슨 일이 있었는지"만 말한다. 어떤 클립을 쓸지, 볼륨을 얼마로 할지는 `CombatUiView` 인스펙터 쪽 임시 연결이 담당한다.
- 정식 사운드가 들어오면 이 조건만 참고해서 교체하면 된다.

### `Assets/Scripts/Prototype/PrototypeCombatBootstrap.cs`

임시 전투 씬을 시작하는 부트스트랩이다.

- 필요한 `CombatManager`, `PlayerCombatController`, `EnemyController`가 없으면 런타임에 만든다.
- 지정된 Player/Enemy 데이터가 없으면 `PrototypeCombatFactory`로 임시 데이터를 만든다.
- `randomizeEnemyOnStart`가 켜져 있으면 전투 시작마다 임시 적 AI 풀에서 랜덤 적을 뽑는다.
- `StartCombat`을 호출해서 전투를 시작한다.
- 재시작도 처리한다.

연결:

- 씬의 임시 UI와 전투 코어를 이어준다.
- `CombatUiView.Initialize`를 호출한다.

조심할 점:

- 정식 게임 시작 흐름이 생기면 이 파일은 교체될 수 있다.

### `Assets/Scripts/Prototype/PrototypeCombatFactory.cs`

임시 플레이에 쓸 기본 데이터 세트를 만든다.

- 공격 스킬 3개, 방어 스킬 3개를 만든다.
- 임시 플레이어와 임시 적 데이터를 만든다.
- 12개 임시 적 AI 풀을 만든다.
- 랜덤 전투용 임시 적 하나를 만들 수 있다.
- 적 이름과 AI 브레인 설정도 임시로 넣는다.

연결:

- `PrototypeCombatBootstrap`이 데이터가 없을 때 호출한다.
- `CombatUiBuilder`도 비슷한 프로토타입 데이터를 에셋으로 만든다.

조심할 점:

- 여기 수치는 정식 밸런스가 아니라 확인용이다.
- 런타임 생성 ScriptableObject라 `PrototypeCombatLoadout.Dispose`로 정리한다.

### `Assets/Scripts/Prototype/PrototypeCombatLoadout.cs`

임시로 만든 ScriptableObject 묶음을 관리한다.

- 플레이어 데이터, 적 데이터, 스킬 목록을 담는다.
- `ownsAssets`가 true면 Dispose 때 생성한 오브젝트를 제거한다.

연결:

- `PrototypeCombatFactory`가 만든 결과를 담는다.
- `PrototypeCombatBootstrap`이 전투 재시작이나 종료 시 정리한다.

조심할 점:

- 플레이 중이면 `Destroy`, 에디터 모드면 `DestroyImmediate`를 쓴다.

### `Assets/Scripts/Prototype/PrototypeCombatText.cs`

임시 UI에 표시할 한국어 문구를 만든다.

- 코스트 문구
- HP 문구
- 최근 행동 문구
- 남은 이동 횟수 문구
- 스킬 버튼 문구
- 적 인텐트 문구
- 결과 화면 문구

연결:

- `CombatUiView`가 화면 텍스트를 만들 때 사용한다.
- `PrototypeCombatTextTests.cs`가 문구 규칙을 검증한다.

조심할 점:

- 정식 UI 문구가 생기면 이 파일은 교체될 수 있다.

### `Assets/Scripts/Prototype/PrototypeCombatUiState.cs`

임시 UI의 화면 모드를 결정한다.

- 보드 화면
- 액션 카테고리 화면
- 액션 스킬 목록 화면
- 적 턴 화면

연결:

- `CombatUiView`가 snapshot을 받을 때 `Sync`를 호출한다.
- 선택한 공격/방어 카테고리에 맞는 스킬 목록을 필터링한다.

조심할 점:

- 전투 phase와 UI 패널 모드를 분리해 둔 파일이다. 전투 규칙은 여기 넣지 않는다.

## Editor 코드

### `Assets/Editor/CombatUiBuilder.cs`

에디터 메뉴로 임시 전투 UI를 씬에 만들어 주는 파일이다.

- 메뉴: `Project2048/Generate Combat UI`
- Canvas, 보드, 버튼, 패널, 결과 오버레이를 만든다.
- `CombatUiView`에 필요한 serialized reference를 연결한다.
- 정식 사운드나 클립 배정은 만들지 않는다. 샘플 씬의 임시 사운드는 `CombatUiView` 인스펙터에서 직접 연결한다.
- 임시 PlayerSO, EnemySO, SkillSO 에셋도 준비한다.
- EventSystem과 InputSystem UI 모듈을 보장한다.

연결:

- `CombatUiView`, `BoardCellView`, `BoardSwipeHandler`, `PrototypeCombatBootstrap`이 붙은 오브젝트를 만든다.

조심할 점:

- 이 파일도 정식 UI 담당 범위가 아니다. 전투를 눈으로 확인하기 위한 임시 생성 도구다.
- 정식 UI가 들어오면 이 생성 방식은 버려도 된다.
- 사운드까지 자동 생성하는 도구가 아니므로, 필요한 경우 샘플 씬에서 `AudioSource`와 임시 클립을 따로 꽂는다.

## EditMode 테스트 코드

### `Assets/Tests/EditMode/ActionCostWalletTests.cs`

코스트 부족 시 `Spend`가 실패하고 현재 코스트를 바꾸지 않는지 검증한다.

보호하는 규칙:

- 코스트가 부족한 스킬 사용은 값 변경 없이 실패해야 한다.

### `Assets/Tests/EditMode/Board2048ManagerTests.cs`

2048 보드 규칙을 검증한다.

보호하는 규칙:

- 한 번 이동 안에서 같은 타일이 두 번 병합되지 않는다.
- 방해 블록 너머로 병합되지 않는다.
- 방해 블록 전 구간에서는 정상 병합된다.
- 예약된 방해 블록은 다음 보드 초기화 때 배치된다.

### `Assets/Tests/EditMode/Board2048TransitionTests.cs`

보드 이동 애니메이션용 데이터가 제대로 나오는지 검증한다.

보호하는 규칙:

- 이동/병합 정보가 `BoardTransition`에 들어간다.
- 보드가 움직이지 않으면 transition을 발행하지 않는다.

### `Assets/Tests/EditMode/CombatManagerTests.cs`

전투 핵심 흐름을 검증한다.

보호하는 규칙:

- 마지막 적을 죽이면 승리 이벤트가 발생한다.
- 턴 종료 시 적 공격이 실행되고 다음 플레이어 턴으로 넘어간다.
- 방어 보너스가 이후 방어 스킬에 누적 적용된다.
- 다음 플레이어 턴이 시작되면 현재 방어도는 지워진다.

### `Assets/Tests/EditMode/CombatUiContractTests.cs`

UI가 전투 내부 객체를 직접 잡지 않고 snapshot/command만으로 전투를 조작할 수 있는지 검증한다.

보호하는 규칙:

- `GetSnapshot`이 UI에 필요한 상태를 담는다.
- `RequestBoardMove`가 보드 phase를 끝내고 코스트를 만든다.
- `RequestUseSkillById`가 ID 기반 입력으로 스킬을 쓴다.
- `RequestEndPlayerTurn` 후 적 행동 문구가 snapshot에 반영된다.
- 적 턴이 끝나면 다음 플레이어 턴 snapshot에 다음 인텐트가 반영된다.
- 공포/암흑 디버프가 실행되면 `LastVfxCue`가 snapshot에 실린다.
- 공포는 플레이어 상태효과에만 표시되고 적 상태효과에는 표시되지 않는다.
- 암흑 디버프 후 다음 보드에 방해 블록이 배치된다.

### `Assets/Tests/EditMode/CombatUiViewTests.cs`

임시 UI의 입력, 타이밍 상수, 임시 사운드 연결을 검증한다.

보호하는 규칙:

- 타일 이동 애니메이션 시간이 너무 길지 않다.
- 보드 종료 후 액션 패널로 넘어가기 전에 짧은 지연이 있다.
- 디버프 임시 VFX가 짧은 피드백 시간 안에 끝난다.
- 임시 `AudioSource`가 2D UI 피드백용 설정으로 맞춰진다.
- 인스펙터의 양수 `soundVolumeScale`은 런타임에서 덮어쓰지 않는다.
- HP/방어도 감소와 보드 이동/병합이 임시 사운드 cue로 변환된다.
- 모바일/마우스 스와이프가 방향 입력을 낸다.

### `Assets/Tests/EditMode/CostConverterTests.cs`

코스트 변환표와 보드 전체 합산 방식을 검증한다.

보호하는 규칙:

- 현재 임시 코스트 표가 코드와 맞다.
- 보드 전체 타일을 합산한다.
- 빈 칸, 방해 블록, 잘못된 값은 0으로 처리한다.

조심할 점:

- 정식 밸런스로 코스트 표를 바꾸면 이 테스트 기대값도 같이 바꾼다.

### `Assets/Tests/EditMode/EnemyDebuffTests.cs`

적 디버프 효과를 검증한다.

보호하는 규칙:

- `Fear`는 플레이어 방어 획득량을 낮춘다.
- `Darkness`는 보드에 방해 블록을 예약한다.

### `Assets/Tests/EditMode/EnemyAiBrainTests.cs`

적 AI 브레인 선택 규칙을 검증한다.

보호하는 규칙:

- `intentPattern`이 있으면 AI보다 고정 패턴을 먼저 쓴다.
- 패턴이 비어 있으면 AI가 공격/방어/디버프를 만든다.
- 공격 몰빵과 방어 몰빵 설정은 실제 선택 분포를 바꾼다.
- 강화형 설정은 생성된 인텐트 수치를 올린다.

### `Assets/Tests/EditMode/PlayerCombatControllerTests.cs`

플레이어 데이터가 컨트롤러에 저장되는지 검증한다.

보호하는 규칙:

- `PlayerSO`가 UI 초상화 등 데이터 바인딩에 남아 있어야 한다.

### `Assets/Tests/EditMode/PrototypeCombatFactoryTests.cs`

임시 전투 데이터 생성 규칙을 검증한다.

보호하는 규칙:

- 임시 로드아웃에 공격 3개, 방어 3개가 들어간다.
- 임시 스킬 이름이 한국어 단계 이름으로 만들어진다.
- 임시 적 AI 풀은 12개이며 일반형 8개, 강화형 4개로 구성된다.
- 임시 적 AI 풀에는 공격 몰빵, 방어 몰빵, 밸런스와 두 디버프 순서가 포함된다.

### `Assets/Tests/EditMode/PrototypeCombatTextTests.cs`

임시 UI 문구 포맷을 검증한다.

보호하는 규칙:

- 스킬 버튼 문구에 단계, 이름, 코스트, 위력이 나온다.
- 적 인텐트 문구가 한국어로 나온다.
- 적 머리 위 표시 문구에 AI 타입이 함께 나온다.
- 적 HP 문구에 방어도 표시가 함께 나온다.
- 공포/암흑 임시 VFX 문구가 한국어로 나온다.
- HP 상태효과 루트는 `BattleScene`에 미리 둔 `PlayerBattleStatusEffects`, `PlayerBoardStatusEffects`, `EnemyStatusEffects` RectTransform을 우선 쓴다. 각 HP 배경의 방어도 위치는 같은 부모 아래의 `BlockIcon`으로 조절한다.
- 최근 행동과 결과 제목 문구가 맞다.

### `Assets/Tests/EditMode/PrototypeCombatUiStateTests.cs`

전투 phase에 따라 임시 UI 패널 상태가 맞게 바뀌는지 검증한다.

보호하는 규칙:

- 보드 phase에서는 보드 화면만 보인다.
- 액션 phase에서는 카테고리 선택 또는 스킬 목록 화면이 나온다.
- 선택한 카테고리의 스킬만 보인다.

## 문서 읽는 순서

처음에는 아래 순서로 보면 된다.

1. `Docs/Combat2048CodeGuide.md`
2. 이 문서의 `CombatManager.cs` 섹션
3. `Board2048Manager.cs` 섹션
4. `CostConverter.cs` 섹션
5. `SkillExecutor.cs` 섹션
6. `EnemyIntentSystem.cs` 섹션
7. 필요한 테스트 파일 섹션
