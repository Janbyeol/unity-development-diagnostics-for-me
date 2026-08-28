# Unity Development Diagnostics

Unity 프로젝트에서 개발 중 상태 오류와 진단 로그를 빠르게 확인하기 위한 UPM 패키지입니다. 일반 릴리스 빌드에서는 `UNITY_ASSERTIONS` 조건이 빠질 때 Assert와 Log 호출 및 인자 평가가 제거됩니다.

## 설치

Unity 프로젝트의 `Packages/manifest.json`에 사용할 버전 태그와 함께 Git URL을 추가합니다.

```json
"com.github.janbyeol.development-diagnostics": "https://github.com/Janbyeol/unity-development-diagnostics-for-me.git#v0.1.1"
```

패키지를 직접 수정하며 개발할 때는 로컬 저장소 경로를 연결할 수 있습니다.

```json
"com.github.janbyeol.development-diagnostics": "file:../../UnityPackages/development-diagnostics"
```

## 포함 기능

- `ProjectAssert.Normal`과 `ProjectAssert.Critical`
- `ProjectLog.Info`, `ProjectLog.Warning`, `ProjectLog.Error`
- Assert 팝업과 프로젝트 설정
- Assert와 Log를 함께 조회하는 통합 Debug Window
- `IDebugPanel` 구현체 자동 등록

## 사용 예시

```csharp
using DevelopmentDiagnostics.Assertions;
using DevelopmentDiagnostics.Logging;

ProjectAssert.Critical(player != null, "Player 참조를 찾을 수 없습니다.", this);
ProjectLog.Info("Player", "플레이어 초기화가 완료되었습니다.", this);
```

게임별로 반복해서 사용하는 Tag는 패키지 안에 추가하지 않고 프로젝트 코드에서 별도 상수 클래스로 관리합니다.

## Debug Window 확장

프로젝트의 `Editor` 폴더에서 `IDebugPanel`을 구현하면 `Tools > Development Diagnostics > Debug Window`에 자동 등록됩니다. 구현체에는 매개변수 없는 생성자가 필요하며 `Id`는 다른 패널과 중복될 수 없습니다.

## 어셈블리

- `DevelopmentDiagnostics.Runtime`: Assert와 Log 공용 API
- `DevelopmentDiagnostics.Editor`: 팝업, 설정, 수집기와 Debug Window

Editor 어셈블리는 Runtime만 참조하며 게임플레이 코드에는 의존하지 않습니다.

## 라이선스

이 프로젝트는 [MIT License](LICENSE.md)에 따라 사용할 수 있습니다.

## AI 사용 고지

이 저장소의 코드와 문서 작성 과정에서 AI 도구인 OpenAI Codex를 사용했습니다. AI가 생성하거나 제안한 내용은 저장소 소유자가 검토하고 Unity에서 동작을 확인한 뒤 반영했습니다.
