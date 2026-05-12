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
- PR 작성 시 저장소의 `.github/pull_request_template.md` 양식을 사용합니다.

## PR에 주로 넣는 내용

README와 PR에는 보통 아래 항목을 넣습니다.

- 작업 개요: 이번 변경을 한 줄로 요약
- 변경 내용: 실제로 추가/수정/삭제한 핵심 항목
- 테스트 방법: Unity Play, EditMode 테스트, 직접 확인한 기능
- 영향 범위: UI, Scene, Input, Settings, Game Flow 등
- 스크린샷/영상: 화면이나 연출이 바뀐 경우
- 참고 사항: 리뷰어가 알아야 할 제약이나 후속 작업
