# Unity Development Diagnostics

Unity 프로젝트의 개발 중 상태 오류와 진단 로그를 기록하는 UPM 패키지입니다. Assert 실패는 호출 위치와 콜스택을 남기며, Editor에서 팝업으로 확인할 수 있습니다. Debug Window에서는 Assert와 Tag가 붙은 로그를 함께 조회할 수 있습니다.

## 설치

### Git 태그로 설치

Unity 프로젝트의 `Packages/manifest.json`에서 `dependencies`에 다음 항목을 추가합니다. 이미 패키지가 설치되어 있으면 해당 항목의 URL을 바꿉니다.

```json
"com.github.janbyeol.development-diagnostics": "https://github.com/Janbyeol/unity-development-diagnostics-for-me.git#v0.3.2"
```

Editor에서 설치하려면 `Window > Package Manager`를 열고 `Install package from git URL`에서 같은 URL을 입력합니다. 태그를 지정하면 팀원과 CI가 같은 버전을 사용합니다. [Unity Git 패키지 문서](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-git.html)

### 로컬 소스로 개발

패키지 소스를 수정하며 확인하려면 Git URL을 `package.json`이 있는 로컬 폴더 경로로 바꿉니다. 아래 상대 경로는 Unity 프로젝트의 `Packages` 폴더를 기준으로 합니다. 실제 폴더 배치에 맞게 조정하거나 절대 경로를 사용합니다. [Unity 로컬 패키지 문서](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-localpath.html)

```json
"com.github.janbyeol.development-diagnostics": "file:../../UnityPackages/development-diagnostics"
```

## Assert 사용

- `ProjectAssert.Normal`은 예상치 못한 상태를 기록한 뒤 호출부로 돌아옵니다. 이후 작업을 건너뛰거나 대체 경로로 처리할 수 있을 때 사용합니다.
- `ProjectAssert.Critical`은 실패를 기록한 뒤 `InvalidOperationException`을 던집니다. 참조 누락 등으로 이후 결과를 신뢰할 수 없을 때 사용합니다.
- 두 메서드 모두 `bool` 조건을 받는 오버로드와 실패 메시지만 받는 오버로드가 있습니다. 조건을 전달하면 `false`일 때 실패를 기록하고, 메시지만 전달하면 해당 호출이 바로 실패를 기록합니다.
- `context`에 관련 `Component`, `GameObject` 또는 에셋을 전달하면 Editor에서 **Ping Context**로 찾을 수 있습니다.

`UNITY_ASSERTIONS`가 없는 호출부에서는 `ProjectAssert`와 `ProjectLog` 호출 및 인자 평가가 제거됩니다. 릴리스에서도 필요한 상태 변경과 보호 분기는 호출부에 둡니다. 아래 예제에서 `return`은 릴리스 빌드에서도 필수 참조 접근을 막습니다.

아래 코드를 `PlayerSetup.cs`에 저장하고 씬 오브젝트에 컴포넌트로 추가합니다. Inspector의 `Player`에 확인할 Transform을 할당한 뒤 Play를 시작합니다.

```csharp
using DevelopmentDiagnostics.Assertions;
using DevelopmentDiagnostics.Logging;
using UnityEngine;

public sealed class PlayerSetup : MonoBehaviour
{
    [SerializeField] private Transform m_player;

    private void Awake()
    {
        if (m_player == null)
        {
            ProjectAssert.Critical("Player 참조를 할당해야 합니다.", this);
            return;
        }

        if (m_player.TryGetComponent(out Animator animator))
        {
            animator.enabled = true;
        }
        else
        {
            ProjectAssert.Normal("Animator가 없어 애니메이션 설정을 건너뜁니다.", m_player);
        }

        ProjectLog.Info("Player", "플레이어 참조를 확인했습니다.", m_player);
    }
}
```

이미 보호 분기가 있는 코드에서는 위와 같이 메시지만 받는 오버로드를 사용할 수 있습니다. 조건을 직접 전달하는 예는 `ProjectAssert.Normal(retryCount >= 0, "재시도 횟수가 음수입니다.")`입니다.

## Log 사용

`ProjectLog.Info`, `Warning`, `Error`는 기록의 심각도를 나타냅니다. 세 메서드 모두 `message`와 선택적인 `context`를 받을 수 있으며, 첫 번째 인자로 `tag`를 전달할 수도 있습니다. Tag를 생략하거나 공백만 전달하면 `General`로 기록합니다. 반복해서 쓰는 Tag는 사용하는 프로젝트의 상수로 관리할 수 있습니다.

스크립트에 `using DevelopmentDiagnostics.Logging;`을 추가한 뒤 메서드 안에서 호출합니다.

```csharp
ProjectLog.Info("Inventory", "인벤토리를 열었습니다.");
ProjectLog.Warning("Inventory", "저장된 슬롯 정보에 빈 항목이 있습니다.");
ProjectLog.Error("Inventory", "인벤토리 저장에 실패했습니다.");
ProjectLog.Info("Tag를 생략한 일반 진단 메시지입니다.");
```

## Debug Window에서 기록 찾기

`Tools > Development Diagnostics > Debug Window`를 열면 `Dev Diagnostics` 창에서 Log와 Assert 기록을 볼 수 있습니다.

1. `All`, `Play`, `Editor`로 발생 환경을 고릅니다. Play가 끝나도 직전 세션의 기록을 볼 수 있으며, 다음 Play가 시작될 때 기존 Play 기록을 비웁니다.
2. `Logs`와 `Asserts`, 심각도 버튼으로 표시 대상을 좁힙니다. 특정 Tag를 고르면 그 Tag의 Log만 표시됩니다. Assert를 함께 보려면 `All Tags`를 선택합니다.
3. 검색창에서 메시지, Tag, 호출 메서드 이름, 소스 파일 경로를 검색합니다. `Auto Scroll`을 켜면 새 기록이 들어올 때 목록의 끝으로 이동합니다.
4. 행을 선택하면 메시지와 호출 위치를 볼 수 있습니다. Assert에는 콜스택도 표시됩니다. `Open Source`로 `Assets` 아래의 호출 코드를 열고, `Ping Context`로 관련 오브젝트를 선택하며, `Copy Details`로 상세 내용을 복사할 수 있습니다.

`Clear`는 현재 환경 탭의 전체 기록을 삭제합니다. `All` 탭에서는 모든 기록을 삭제합니다.

프로젝트 전용 디버그 도구는 해당 프로젝트의 `EditorWindow`로 구현할 수 있습니다. 진단 기록이 필요하면 `ProjectAssert.Failed`와 `ProjectLog.Written`을 구독하고 창을 닫을 때 구독을 해제합니다.

## Assert 팝업과 Console 설정

`Tools > Development Diagnostics > Assertion Settings`에서 프로젝트별로 설정합니다. 변경 사항은 `ProjectSettings/DevelopmentDiagnosticsSettings.asset`에 저장됩니다.

| 설정 | 동작 |
| --- | --- |
| `Popup Mode` | 기본값인 `Critical Only`는 Critical만 표시합니다. `All`은 Normal도 표시하며, `Off`는 팝업 표시를 끕니다. |
| `Pause Before Popup` | Play Mode에서 팝업을 열기 전에 일시정지합니다. 기본값은 켜짐입니다. |
| `Assert To Unity Console` | Normal은 Warning, Critical은 Assert로 Unity Console에 전달합니다. 기본값은 꺼짐입니다. |
| `Project Log To Unity Console` | Log를 해당 심각도의 Unity Console 메시지로 전달합니다. 기본값은 꺼짐입니다. |
| `Popup Width`, `Popup Height`, `Message Font Size` | 팝업 크기와 메시지 글자 크기를 조절합니다. |

Debug Window는 두 Console 전달 옵션과 별도로 기록을 수집합니다. `Raise Normal Assert`와 `Raise Critical Assert`로 현재 설정을 확인할 수 있습니다.

팝업의 `Ignore Once`는 현재 표시를 닫습니다. `Ignore This Assert This Play` 또는 `Ignore This Assert This Edit Session`은 같은 호출 위치의 팝업을 해당 세션에서 숨깁니다. `Stop Play Mode`는 실행 중인 Play를 종료하며, `Reset Session-Ignored Assertions`로 세션의 무시 설정을 초기화할 수 있습니다. Critical의 예외는 팝업에서 버튼을 누르기 전에 이미 발생합니다.

## CI에서 로그 확인

1. `Assert To Unity Console`을 켜고 `ProjectSettings/DevelopmentDiagnosticsSettings.asset`을 Unity 프로젝트 저장소에 포함합니다. 일반 진단 로그도 수집하려면 `Project Log To Unity Console`을 켭니다.
2. 테스트가 실패해도 Unity 로그 파일과 테스트 결과를 CI 아티팩트로 보관하도록 설정합니다. GameCI를 사용한다면 테스트 단계의 `artifactsPath`를 업로드하면 됩니다. [GameCI 아티팩트 문서](https://game.ci/docs/github/test-runner/#storing-test-results)
3. 실행 로그에서 `[Project Assert/Normal]`, `[Project Assert/Critical]`, `[Project Log/`를 검색합니다. 실제로 호출된 코드의 진단 메시지가 여기에 남습니다.

Unity Test Framework에서 의도적으로 `Critical` 실패를 검증할 때는 예외를 검사합니다. Console 전달을 켠 테스트에서는 `LogAssert.Expect(LogType.Assert, ...)`로 예상하는 메시지도 미리 등록합니다. [Unity Test Framework `LogAssert` 문서](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/api/UnityEngine.TestTools.LogAssert.html)

### 실패 이벤트 구독자의 예외 처리

`ProjectAssert.Failed`는 실패 시점의 구독자 목록을 등록 순서대로 호출합니다. 한 구독자가 예외를 던져도 나머지 구독자는 같은 메시지·호출 위치·콜스택을 전달받습니다. 통지가 끝나면 `Normal`은 호출부로 돌아오고, `Critical`은 원래 Assert 메시지를 가진 `InvalidOperationException`을 던집니다.

구독자 예외는 `[Project Assert/Subscriber]` 접두사와 예외 내용·스택을 포함한 Unity Warning으로 기록합니다. 이 경고는 `Assert To Unity Console` 설정과 별도로 출력됩니다. 커스텀 `ILogHandler`도 예외를 던지면 해당 경고를 남기지 못할 수 있지만, 나머지 구독자 통지와 Critical의 원래 예외 처리는 계속됩니다.

구독자 오류를 의도적으로 발생시키는 테스트에서는 `LogAssert.Expect(LogType.Warning, ...)`로 경고를 등록할 수 있습니다. 모든 로그를 검사하는 테스트는 이 경고도 예상 로그에 포함해야 합니다. `ProjectLog.Written`의 구독자 예외 처리에는 이 정책이 적용되지 않습니다.

### 패키지 회귀 테스트 실행

Unity Test Framework를 설치한 프로젝트의 `Packages/manifest.json`에 `"testables": ["com.github.janbyeol.development-diagnostics"]`를 추가하면 패키지 테스트를 활성화할 수 있습니다. Test Runner의 EditMode와 PlayMode에서 `DevelopmentDiagnostics.Tests` 테스트를 실행합니다. 구독자 예외 격리, 원래 Critical 예외와 호출 위치 보존, 경고 출력 실패, `UNITY_ASSERTIONS`가 없는 호출부의 인자 평가 제거를 확인합니다.

## 어셈블리

- `DevelopmentDiagnostics.Runtime`은 Assert와 Log 공용 API를 제공합니다. 사용자 스크립트가 자체 `.asmdef`를 사용한다면 해당 어셈블리의 참조에 추가합니다.
- `DevelopmentDiagnostics.Editor`는 팝업, 설정, 수집기와 Debug Window를 제공합니다.

Editor 어셈블리는 Runtime만 참조하며 게임플레이 코드에는 의존하지 않습니다.

## 라이선스

이 프로젝트는 [MIT License](LICENSE.md)에 따라 사용할 수 있습니다.

## AI 사용 고지

이 저장소의 코드와 문서 작성 과정에서 AI 도구인 OpenAI Codex를 사용했습니다. AI가 생성하거나 제안한 내용은 저장소 소유자가 검토하고 Unity에서 동작을 확인한 뒤 반영했습니다.
