# Changelog

이 패키지의 주요 변경 사항을 기록합니다.

## [0.3.2] - 2026-11-13

- `ProjectAssert.Failed` 구독자별 예외를 격리하여 한 구독자의 오류가 나머지 구독자 통지를 중단하지 않도록 수정했습니다.
- 구독자 오류가 있어도 Normal은 정상 복귀하고 Critical은 원래 Assert 메시지의 `InvalidOperationException`을 던집니다.
- 구독자 오류의 예외 내용과 스택을 Console 전달 설정과 별도로 Warning에 기록합니다. 경고 출력 중 발생하는 커스텀 로그 핸들러 예외도 격리합니다.
- EditMode 및 PlayMode에서 실행할 수 있는 구독자 예외 처리 회귀 테스트와 사용 설명을 추가했습니다.

## [0.1.0] - 2026-08-28

- ProjectAssert와 Project Log Runtime API를 독립 어셈블리로 분리했습니다.
- Assert 팝업, 설정과 통합 Debug Window를 Editor 어셈블리로 분리했습니다.
- `IDebugPanel` 기반 자동 패널 등록을 추가했습니다.
- 사용하는 게임 프로젝트의 전용 Log Tag와 디버그 명령을 패키지에서 제외했습니다.
