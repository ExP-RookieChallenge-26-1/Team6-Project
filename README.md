# Project 2048

2048 퍼즐 보드와 턴제 전투를 결합한 Unity 팀 프로젝트다.

플레이어는 2048 보드를 움직여 행동 코스트를 만들고, 모은 코스트로 공격/방어/상태 변화 스킬을 사용한다. 적은 다음 행동을 인텐트로 예고하며, 플레이어 턴이 끝나면 예고한 행동을 실행한다. 전투에서 승리하면 보상을 선택하고 다음 스테이지 흐름으로 이어진다.

## 프로젝트 정보

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.4.5f1 |
| Unity Project Path | `Project 2048` |
| Main Scenes | `MainMenu`, `StoryScene`, `BattleScene` |
| Showcase Scene | `AttackEffectShowcase` |
| Render Pipeline | Universal Render Pipeline |
| Test Framework | Unity Test Framework |

## 실행 방법

1. 저장소를 클론한다.
2. Unity Hub에서 `Project 2048` 폴더를 연다.
3. Unity 버전이 `6000.4.5f1`인지 확인한다.
4. `Assets/Scenes/MainMenu.unity` 또는 `Assets/Scenes/BattleScene.unity`를 연다.
5. Unity Editor에서 Play를 눌러 실행한다.

## 현재 게임 흐름

```text
메인 메뉴
-> 스토리
-> 전투 씬
-> 스테이지 시작
-> 2048 보드 조작
-> 행동 코스트 획득
-> 스킬 사용
-> 적 턴 처리
-> 승리 시 보상 선택
-> 다음 스테이지 또는 결과 처리
```

`BattleScene`의 전투 진입은 `StageFlowController`가 담당한다. 이 컨트롤러는 전투 시작, 승리/패배 판정, 보상 진입, 보상 선택 후 스테이지 완료 흐름을 연결한다.

## 현재 구현 상태

- `StageFlowController`가 스테이지 단위 전투와 보상 흐름을 관리한다.
- `CombatManager`는 전투 규칙의 중심이며 `CombatSnapshot`과 command 메서드로 UI와 분리되어 있다.
- `Board2048Manager`는 보드 이동, 병합, 랜덤 타일 생성, 장애물 배치를 처리한다.
- `SkillExecutor`는 `SkillSO` 데이터를 기반으로 공격, 방어, 회복, 디버프, 보드 간섭 계열 스킬을 실행한다.
- `EnemyAiBrain`과 `EnemyIntentSystem`은 적 인텐트 선택과 실행을 담당한다.
- `RewardManager`와 `RunProgress`가 전투 결과, 보상 선택, 런 진행 상태를 관리한다.
- `ScoreManager`와 `SaveLoadManager`가 점수 계산, 최고 점수, 저장 데이터 처리를 담당한다.
- `CombatUiView`는 partial 구조로 분리되어 있으며 전투 HUD, 보드 셀, 인텐트, 로그, 테마 색상을 담당한다.
- `CombatWorldSpriteView`는 적 연출, 피격 숫자, 디버프 연출, 월드 셰이크, 오디오 라우팅을 partial 파일로 나누어 처리한다.
- `CombatProjectileEffect`, `CombatEffectBinding`, `CombatWorldVfxProfileSO`가 ScriptableObject 기반 전투 VFX 연결을 담당한다.
- `PrototypeCombatEventAudioPlayer`, `PrototypeCombatAudioRouter`, `Project2048AudioSettings`, Audio Mixer가 전투/버튼/BGM 오디오를 연결한다.

## 데이터와 콘텐츠 현황

| 항목 | 현재 수량/상태 |
|---|---|
| SkillSO assets | 41개 |
| EnemySO assets | 12개 |
| Skill reward assets | 20개 |
| Unity scenes | 4개 |
| Monster cutout images | 11개 |
| Background folders | `MainMenu`, `StoryArea`, `BattleArea`, `PresentationArea` 기준으로 정리 |

적 데이터는 `EnemySO` 에셋과 프로토타입 생성 로직을 함께 사용한다.

- 씬에 연결된 적 풀은 `BattleScene`의 `StageFlowController.enemyPool`에 들어 있다.
- 랜덤 적 생성 로직은 `PrototypeCombatFactory.CreateRandomPrototypeEnemy()`에 남아 있다.
- 현재 `BattleScene`의 `StageFlowController.randomizeEnemyOnStart` 값은 꺼져 있어, 씬의 기본 `enemyData`가 우선 사용된다.
- 랜덤 적 테스트가 필요하면 `StageFlowController`의 `Randomize Enemy On Start`를 켜고 `enemyPool` 연결을 확인한다.

## 최근 반영된 구조

- `CombatUiView`가 partial 구조로 바뀌었고, 테마 색상/상수는 `CombatUiView.Theme.cs`로 분리되었다.
- `CombatWorldSpriteView`의 오디오, 적 연출, 디버프, 피격 숫자 처리가 partial 파일로 나뉘었다.
- 배경 에셋 폴더가 영어 기준 경로로 정리되었다.
- `AttackEffectShowcase.unity`와 `SkillVfxShowcaseBuild`가 있어 스킬 VFX 쇼케이스 확인과 빌드 구성이 가능하다.
- `.ai-context` 프로젝트 분석 파일은 최신 코드 상태 기준으로 갱신되어 있다.

## 주요 폴더

| 경로 | 설명 |
|---|---|
| `Project 2048/Assets/Scenes` | 메인 메뉴, 스토리, 전투, VFX 쇼케이스 씬 |
| `Project 2048/Assets/Art/Backgrounds` | 메인 메뉴, 스토리, 전투, 프레젠테이션 배경 에셋 |
| `Project 2048/Assets/Art/Effects/SkillVFX` | 스킬 VFX 텍스처, 머티리얼, 프리팹, VFX Graph 에셋 |
| `Project 2048/Assets/Art/Monsters` | 선택 몬스터 컷아웃과 프리뷰 |
| `Project 2048/Assets/Audio` | Audio Mixer |
| `Project 2048/Assets/Sounds` | BGM, 전투 SFX, UI SFX |
| `Project 2048/Assets/Data/Skills` | 스킬 ScriptableObject 데이터 |
| `Project 2048/Assets/Data/Enemies` | 적 ScriptableObject 데이터 |
| `Project 2048/Assets/Data/Rewards` | 보상 ScriptableObject 데이터 |
| `Project 2048/Assets/Editor` | 씬/로스터/VFX/쇼케이스 빌드 보조 에디터 도구 |
| `Project 2048/Assets/Scripts/Audio` | BGM, 버튼 SFX, 오디오 설정, BGM ducking |
| `Project 2048/Assets/Scripts/Board2048` | 2048 보드 이동, 병합, 코스트 변환 |
| `Project 2048/Assets/Scripts/Combat` | 전투 흐름, 턴 전환, 전투 상태 snapshot |
| `Project 2048/Assets/Scripts/Core` | 전역 게임 컨텍스트와 게임 매니저 |
| `Project 2048/Assets/Scripts/Enemy` | 적 상태, 인텐트, AI, 디버프 |
| `Project 2048/Assets/Scripts/Flow` | 씬 전환, 스테이지 흐름, 결과 흐름 |
| `Project 2048/Assets/Scripts/Presentation` | 전투 연출, VFX, effect binding |
| `Project 2048/Assets/Scripts/Prototype` | 현재 전투 UI, 월드 스프라이트, 프로토타입 부트스트랩 |
| `Project 2048/Assets/Scripts/Rewards` | 보상 테이블, 보상 선택, 런 진행 상태 |
| `Project 2048/Assets/Scripts/SaveLoad` | 저장 경로, 저장 데이터, JSON 저장소 |
| `Project 2048/Assets/Scripts/Score` | 점수 계산과 최고 점수 기록 |
| `Project 2048/Assets/Scripts/Skills` | 공격/방어/상태 변화 스킬 데이터와 실행 |
| `Project 2048/Assets/Scripts/UI` | 메인 메뉴, 로딩, 팝업, 스토리 UI |
| `Project 2048/Assets/Tests/EditMode` | EditMode 테스트 |
| `Project 2048/Docs` | 코드 흐름과 파일별 설명 문서 |

## 역할 분담 기준

현재 코드는 초기 계획의 이름과 일부 다르다. 아래는 현재 파일 구조 기준으로 정리한 담당 영역이다.

### A: 게임 흐름 + 저장 + 점수 + 스테이지

A는 전체 연결 담당이다. 씬 전환, 게임 상태, 스테이지 진행, 점수, 세이브/로드, 게임오버/클리어 흐름을 묶어 관리한다.

```text
GameManager
GameContext
FlowController
SceneFlowManager
StageFlowController
StageFlowState
StageResult
ScoreManager
SaveLoadManager
GameSaveData
JsonFileSaveRepository
```

`GameManager`가 전역 진입점이고, `FlowController`와 `SceneFlowManager`가 메인 메뉴, 스토리, 전투 씬 전환을 담당한다. `StageFlowController`는 전투 씬 안에서 스테이지 시작, 전투 종료, 보상, 스테이지 완료/실패 흐름을 관리한다. `ScoreManager`는 전투 결과를 받아 점수를 계산하고 로컬 최고 점수를 기록한다.

### B: 전투 규칙 + 2048 보드 + 전투 연출/오디오

B는 핵심 게임 규칙과 전투 체감 담당이다. 전투 턴, 보드 이동/병합, 코스트 변환, 스킬 실행, 적 인텐트 처리, 전투 연출, 전투 오디오를 맡는다.

```text
CombatManager
Board2048Manager
CostConverter
ActionCostWallet
SkillExecutor
EnemyIntentSystem
EnemyAiBrain
DamageCalculator
CombatWorldSpriteView
CombatWorldVfxProfileSO
CombatEffectBinding
CombatProjectileEffect
PrototypeCombatEventAudioPlayer
PrototypeCombatAudioRouter
Project2048AudioSettings
```

`CombatManager`는 전투의 중심이다. UI는 내부 객체를 직접 만지기보다 `CombatSnapshot`을 읽고 `RequestBoardMove`, `RequestUseSkillById`, `RequestEndPlayerTurn` 같은 command 메서드로 요청한다. 전투 연출은 `CombatWorldSpriteView`, `CombatProjectileEffect`, VFX binding 계열이 담당하고, 전투 오디오는 `PrototypeCombatEventAudioPlayer`, `PrototypeCombatAudioRouter`, `Project2048AudioSettings`가 담당한다. B 작업이 밀리면 `SkillSO`, `EnemySO` 같은 데이터 입력은 A나 C가 도와줄 수 있다.

### C: UI + 보상 + 데이터 에셋 세팅 보조

C는 전투 외곽 UI, 보상, ScriptableObject 데이터 에셋 세팅 보조를 맡는다.

```text
CombatUiView
BoardCellView
BoardSwipeHandler
MainMenuController
SettingPopup
LoadingUI
StoryTextView
RewardManager
SkillSO / EnemySO / PlayerSO / RewardTableSO 에셋 일부 세팅
```

현재 전투 UI는 정식 `CombatHUD`, `Board2048UI`, `RewardChoiceUI` 이름보다는 `Prototype` 폴더의 `CombatUiView`, `BoardCellView`, `BoardSwipeHandler`가 담당한다. 보상 처리는 `RewardManager`가 전투 승리 결과를 받아 보상 선택 상태를 만든다.

## 테스트

Unity Editor에서 다음 경로로 EditMode 테스트를 실행할 수 있다.

```text
Window > General > Test Runner > EditMode > Run All
```

CLI에서 컴파일만 빠르게 확인할 때는 Unity가 생성한 프로젝트 파일을 사용할 수 있다.

```powershell
dotnet build "Project 2048/Game.Core.csproj"
dotnet build "Project 2048/Game.Core.Tests.csproj"
```

주요 테스트는 `Project 2048/Assets/Tests/EditMode`에 있다.

## 참고 문서

- `Project 2048/Docs/Combat2048CodeGuide.md`: 전투 흐름을 처음 읽는 사람을 위한 설명서
- `Project 2048/Docs/CodeFileReference.md`: C# 파일별 역할과 연결 관계 정리
- `Project 2048/Docs/AudioMixerSetup.md`: Audio Mixer와 BGM ducking 구성 메모
- `Project 2048/Docs/Project2048_Balancing_Template.xlsx`: 밸런싱 입력 템플릿

## 협업 규칙

- 실제 Unity 프로젝트는 저장소 루트가 아니라 `Project 2048` 폴더다.
- Unity가 생성하는 `.meta` 파일은 에셋과 함께 커밋한다.
- `Library`, `Temp`, `Logs`, `UserSettings`, `.omx` 같은 로컬 생성 파일은 커밋하지 않는다.
- 기능 작업은 별도 브랜치에서 진행하고 PR로 리뷰받는다.
- 단순 문서 갱신이나 합의된 좁은 수정만 `main`에 직접 반영한다.
