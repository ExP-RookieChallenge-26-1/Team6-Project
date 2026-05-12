# Project 2048

2048 퍼즐 보드와 턴제 전투를 결합한 Unity 팀 프로젝트입니다.

플레이어는 2048 보드를 움직여 행동 코스트를 만들고, 모은 코스트로 공격/방어 스킬을 사용합니다. 적은 다음 행동을 미리 보여주며, 플레이어 턴이 끝나면 예고한 인텐트를 실행합니다.

## 프로젝트 정보

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.4.5f1 |
| Unity Project Path | `Project 2048` |
| Main Scenes | `MainMenu`, `StoryScene`, `BattleScene` |
| Render Pipeline | Universal Render Pipeline |
| Test Framework | Unity Test Framework |

## 실행 방법

1. 저장소를 클론합니다.
2. Unity Hub에서 `Project 2048` 폴더를 엽니다.
3. Unity 버전이 `6000.4.5f1`인지 확인합니다.
4. `Assets/Scenes/MainMenu.unity` 또는 `Assets/Scenes/BattleScene.unity`를 엽니다.
5. Unity Editor에서 Play를 눌러 실행합니다.

## 게임 흐름

```text
메인 메뉴
-> 스토리
-> 전투 씬
-> 2048 보드 조작
-> 행동 코스트 획득
-> 스킬 사용
-> 적 턴 처리
-> 승리/패배 및 보상
```

## 주요 폴더

| 경로 | 설명 |
|---|---|
| `Project 2048/Assets/Scenes` | 메인 메뉴, 스토리, 전투 씬 |
| `Project 2048/Assets/Scripts/Board2048` | 2048 보드 이동, 병합, 코스트 변환 |
| `Project 2048/Assets/Scripts/Combat` | 전투 흐름, 턴 전환, 전투 상태 snapshot |
| `Project 2048/Assets/Scripts/Enemy` | 적 상태, 인텐트, AI, 디버프 |
| `Project 2048/Assets/Scripts/Skills` | 공격/방어 스킬 데이터와 실행 |
| `Project 2048/Assets/Scripts/Flow` | 씬 전환과 게임 흐름 |
| `Project 2048/Assets/Scripts/UI` | 메인 메뉴, 로딩, 팝업, 스토리 UI |
| `Project 2048/Assets/Scripts/Presentation` | 전투 연출, 사운드, VFX 연결 |
| `Project 2048/Assets/Tests/EditMode` | EditMode 테스트 |
| `Project 2048/Docs` | 코드 흐름과 파일별 설명 문서 |

## 역할 분담

현재 코드는 초기 계획의 이름과 일부 다릅니다. 아래는 현재 파일 구조 기준으로 정리한 담당 영역입니다.

### A: 게임 흐름 + 저장 + 점수 + 스테이지

A는 전체 연결 담당입니다. 씬 전환, 게임 상태, 스테이지 진행, 점수, 세이브/로드, 게임오버/클리어 흐름을 묶어 관리합니다.

```text
GameManager
GameContext
FlowController
SceneFlowManager
StageFlowController
ScoreManager
SaveLoadManager (예정)
GameOver / Clear Flow (예정)
```

현재 구현된 흐름은 `GameManager`가 전역 진입점이고, `FlowController`와 `SceneFlowManager`가 메인 메뉴, 스토리, 전투 씬 전환을 담당합니다. `ScoreManager`는 전투 결과를 받아 점수를 계산하고 로컬 최고 점수를 기록합니다. `StageFlowController`와 저장/불러오기 흐름은 아직 확장 여지가 남아 있습니다.

### B: 전투 규칙 + 2048 보드

B는 핵심 게임 규칙 담당입니다. 전투 턴, 보드 이동/병합, 코스트 변환, 스킬 실행, 적 인텐트 처리를 맡습니다.

```text
CombatManager
TurnController
Board2048Manager
CostConverter
ActionCostWallet
SkillExecutor
EnemyIntentSystem
EnemyAiBrain
DamageCalculator
```

`CombatManager`는 전투의 중심입니다. UI는 내부 객체를 직접 만지기보다 `CombatSnapshot`을 읽고 `RequestBoardMove`, `RequestUseSkillById`, `RequestEndPlayerTurn` 같은 command 메서드로 요청합니다. B 작업이 밀리면 `SkillSO`, `EnemySO` 같은 데이터 입력은 A나 C가 도와줄 수 있습니다.

### C: UI + 보상 + 오디오 + 데이터 에셋 세팅 보조

C는 UI만 담당하는 영역이 아닙니다. 보상, 오디오, 전투 연출, ScriptableObject 데이터 에셋 세팅 보조까지 함께 맡습니다.

```text
CombatUiView
BoardCellView
BoardSwipeHandler
MainMenuController
SettingPopup
LoadingUI
StoryTextView
RewardManager
PrototypeCombatEventAudioPlayer
PrototypeCombatAudioRouter
CombatWorldSpriteView
SkillSO / EnemySO / PlayerSO / RewardTableSO 에셋 일부 세팅
```

현재 전투 UI는 정식 `CombatHUD`, `Board2048UI`, `RewardChoiceUI` 이름보다는 `Prototype` 폴더의 `CombatUiView`, `BoardCellView`, `BoardSwipeHandler`가 담당합니다. 보상 처리는 `RewardManager`가 전투 승리 이벤트를 받아 보상 선택 상태를 만들고, 오디오는 `PrototypeCombatEventAudioPlayer`와 `PrototypeCombatAudioRouter`가 전투 이벤트와 연출 데이터를 받아 재생합니다.

## 테스트

Unity Editor에서 다음 경로로 EditMode 테스트를 실행할 수 있습니다.

```text
Window > General > Test Runner > EditMode > Run All
```

주요 테스트는 `Project 2048/Assets/Tests/EditMode`에 있습니다.

## 참고 문서

- `Project 2048/Docs/Combat2048CodeGuide.md`: 전투 흐름을 처음 읽는 사람을 위한 설명서
- `Project 2048/Docs/CodeFileReference.md`: C# 파일별 역할과 연결 관계 정리

## 협업 규칙

- 실제 Unity 프로젝트는 루트가 아니라 `Project 2048` 폴더입니다.
- Unity가 생성하는 `.meta` 파일은 에셋과 함께 커밋합니다.
- `Library`, `Temp`, `Logs`, `UserSettings` 같은 로컬 생성 파일은 커밋하지 않습니다.
- 기능 작업은 별도 브랜치에서 진행하고 PR로 리뷰받습니다.
