using UnityEngine;

namespace DevelopmentDiagnostics.Assertions
{
    /// <summary>
    /// 한 번의 Assert 실패와 발생 위치를 전달하는 값 타입이다.
    /// </summary>
    public readonly struct ProjectAssertionFailure
    {
        /// <summary>
        /// Assert 실패 정보를 생성한다.
        /// </summary>
        /// <param name="message">조건 위반 원인을 설명하는 메시지다.</param>
        /// <param name="severity">게임 실행에 미치는 실패 심각도다.</param>
        /// <param name="context">실패와 관련된 Unity 오브젝트다.</param>
        /// <param name="memberName">Assert를 호출한 메서드 이름이다.</param>
        /// <param name="filePath">Assert를 호출한 소스 파일 경로다.</param>
        /// <param name="lineNumber">Assert를 호출한 소스 줄 번호다.</param>
        /// <param name="stackTrace">Assert 호출까지 이어진 전체 메서드 호출 경로다.</param>
        internal ProjectAssertionFailure(
            string message,
            ProjectAssertSeverity severity,
            Object context,
            string memberName,
            string filePath,
            int lineNumber,
            string stackTrace)
        {
            Message = message;
            Severity = severity;
            Context = context;
            MemberName = memberName;
            FilePath = filePath;
            LineNumber = lineNumber;
            StackTrace = stackTrace;
        }

        /// <summary>
        /// 개발자가 작성한 실패 원인 설명을 가져온다.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 실패의 심각도를 가져온다.
        /// </summary>
        public ProjectAssertSeverity Severity { get; }

        /// <summary>
        /// 실패와 관련된 Unity 오브젝트를 가져온다.
        /// </summary>
        public Object Context { get; }

        /// <summary>
        /// Assert가 호출된 메서드 이름을 가져온다.
        /// </summary>
        public string MemberName { get; }

        /// <summary>
        /// Assert가 호출된 소스 파일 경로를 가져온다.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Assert가 호출된 소스 파일의 줄 번호를 가져온다.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Assert 호출까지 이어진 전체 메서드 호출 경로를 가져온다.
        /// </summary>
        public string StackTrace { get; }
    }
}
