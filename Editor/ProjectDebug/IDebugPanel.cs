using System;
using UnityEngine;

namespace DevelopmentDiagnostics.Editor.Debug
{
    /// <summary>
    ///     Debug Window에 표시되는 독립 기능 패널의 최소 생명주기와 화면 출력 계약입니다.
    /// </summary>
    /// <remarks>자동 등록을 위해 구현 클래스에는 매개변수 없는 생성자가 있어야 합니다.</remarks>
    public interface IDebugPanel
    {
        /// <summary>세션 상태 저장과 패널 선택에 사용하는 고유 식별자입니다.</summary>
        /// <remarks>비어 있지 않아야 하며 다른 패널과 중복될 수 없습니다.</remarks>
        string Id { get; }

        /// <summary>Debug Window의 패널 선택 UI에 표시할 이름입니다.</summary>
        /// <remarks>비어 있지 않아야 합니다.</remarks>
        string DisplayName { get; }

        /// <summary>여러 패널을 표시할 때 사용하는 오름차순 정렬 순서입니다.</summary>
        int Order { get; }

        /// <summary>패널이 창에 연결될 때 이벤트 구독과 초기 상태를 설정합니다.</summary>
        /// <param name="requestRepaint">패널 내용이 바뀌었을 때 창을 다시 그리도록 요청하는 콜백입니다.</param>
        void OnEnable(Action requestRepaint);

        /// <summary>패널이 창에서 분리될 때 이벤트 구독과 임시 참조를 해제합니다.</summary>
        void OnDisable();

        /// <summary>현재 창 영역에 패널 UI를 그립니다.</summary>
        /// <param name="availableRect">패널이 사용할 수 있는 Editor Window 영역입니다.</param>
        void OnGUI(Rect availableRect);
    }
}
