using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace DevelopmentDiagnostics.Assertions
{
    /// <summary>
    ///     Unity 프로젝트의 코드·프리팹·씬 구성 오류를 개발 중에 발견하는 공용 Assert 진입점이다.
    /// </summary>
    public static class ProjectAssert
    {
        private static bool s_logToConsole;

        /// <summary>
        ///     Assert 실패가 발생했을 때 발행된다. Editor 팝업처럼 표시 방식이 필요한 도구가 구독한다.
        ///     구독자 예외는 Warning으로 기록하고 격리하며, 나머지 구독자에게도 실패를 전달한다.
        /// </summary>
        public static event Action<ProjectAssertionFailure> Failed;

        /// <summary>
        ///     Assert 실패를 Unity Console에도 기록할지 설정한다.
        ///     Normal은 Warning으로, Critical은 Assert로 기록한다.
        /// </summary>
        /// <param name="logToConsole">실패를 Unity Console에도 기록하려면 참이다.</param>
        public static void ConfigureConsoleLogging(bool logToConsole)
        {
            s_logToConsole = logToConsole;
        }

        /// <summary>
        ///     필수 조건이 거짓이면 Normal 단계 Assert를 보고한다.
        ///     UNITY_ASSERTIONS가 없는 일반 릴리스 빌드에서는 호출과 인자 평가가 제거된다.
        /// </summary>
        /// <param name="condition">참이면 통과하고, 거짓이면 Assert를 발생시킬 조건이다.</param>
        /// <param name="message">조건 위반 원인을 설명하는 메시지다.</param>
        /// <param name="context">Assert와 관련된 Unity 오브젝트다. 팝업의 Ping Context에 사용된다.</param>
        /// <param name="memberName">호출한 메서드 이름이다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="filePath">호출한 소스 파일 경로다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="lineNumber">호출한 소스 줄 번호다. 컴파일러가 자동으로 채운다.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void Normal(
            bool condition,
            string message,
            Object context = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (condition)
            {
                return;
            }

            Report(
                ProjectAssertSeverity.Normal,
                message,
                context,
                memberName,
                filePath,
                lineNumber);
        }

        /// <summary>
        ///     현재 코드 경로가 실패 상태임을 Normal 단계 Assert로 보고한다.
        ///     UNITY_ASSERTIONS가 없는 일반 릴리스 빌드에서는 호출과 인자 평가가 제거된다.
        /// </summary>
        /// <param name="message">실패 원인을 설명하는 메시지다.</param>
        /// <param name="context">Assert와 관련된 Unity 오브젝트다. 팝업의 Ping Context에 사용된다.</param>
        /// <param name="memberName">호출한 메서드 이름이다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="filePath">호출한 소스 파일 경로다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="lineNumber">호출한 소스 줄 번호다. 컴파일러가 자동으로 채운다.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void Normal(
            string message,
            Object context = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Report(
                ProjectAssertSeverity.Normal,
                message,
                context,
                memberName,
                filePath,
                lineNumber);
        }

        /// <summary>
        ///     필수 조건이 거짓이면 Critical 단계 Assert를 보고한 뒤 예외를 던진다.
        ///     UNITY_ASSERTIONS가 없는 일반 릴리스 빌드에서는 호출과 인자 평가가 제거된다.
        /// </summary>
        /// <param name="condition">참이면 통과하고, 거짓이면 Assert를 발생시킬 조건이다.</param>
        /// <param name="message">조건 위반 원인을 설명하는 메시지다.</param>
        /// <param name="context">Assert와 관련된 Unity 오브젝트다. 팝업의 Ping Context에 사용된다.</param>
        /// <param name="memberName">호출한 메서드 이름이다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="filePath">호출한 소스 파일 경로다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="lineNumber">호출한 소스 줄 번호다. 컴파일러가 자동으로 채운다.</param>
        /// <exception cref="InvalidOperationException">조건이 거짓인 경우 발생한다.</exception>
        [Conditional("UNITY_ASSERTIONS")]
        public static void Critical(
            bool condition,
            string message,
            Object context = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (condition)
            {
                return;
            }

            Report(
                ProjectAssertSeverity.Critical,
                message,
                context,
                memberName,
                filePath,
                lineNumber);
        }

        /// <summary>
        ///     현재 코드 경로를 Critical 단계 Assert 실패로 보고한 뒤 예외를 던진다.
        ///     UNITY_ASSERTIONS가 없는 일반 릴리스 빌드에서는 호출과 인자 평가가 제거된다.
        /// </summary>
        /// <param name="message">실패 원인을 설명하는 메시지다.</param>
        /// <param name="context">Assert와 관련된 Unity 오브젝트다. 팝업의 Ping Context에 사용된다.</param>
        /// <param name="memberName">호출한 메서드 이름이다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="filePath">호출한 소스 파일 경로다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="lineNumber">호출한 소스 줄 번호다. 컴파일러가 자동으로 채운다.</param>
        /// <exception cref="InvalidOperationException">이 메서드가 호출되면 발생한다.</exception>
        [Conditional("UNITY_ASSERTIONS")]
        public static void Critical(
            string message,
            Object context = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Report(
                ProjectAssertSeverity.Critical,
                message,
                context,
                memberName,
                filePath,
                lineNumber);
        }

        private static void Report(
            ProjectAssertSeverity severity,
            string message,
            Object context,
            string memberName,
            string filePath,
            int lineNumber)
        {
            string stackTrace = CaptureStackTrace();
            ProjectAssertionFailure failure = new ProjectAssertionFailure(
                message,
                severity,
                context,
                memberName,
                filePath,
                lineNumber,
                stackTrace);

            if (s_logToConsole)
            {
                string consoleMessage = $"[Project Assert/{severity}] {message}";
                if (severity == ProjectAssertSeverity.Critical)
                {
                    Debug.LogAssertion(consoleMessage, context);
                }
                else
                {
                    Debug.LogWarning(consoleMessage, context);
                }
            }

            NotifyFailed(failure);

            if (severity == ProjectAssertSeverity.Critical)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void NotifyFailed(ProjectAssertionFailure failure)
        {
            Action<ProjectAssertionFailure> failedHandler = Failed;
            if (failedHandler == null)
            {
                return;
            }

            foreach (Action<ProjectAssertionFailure> handler in failedHandler.GetInvocationList())
            {
                try
                {
                    handler(failure);
                }
                catch (Exception exception)
                {
                    LogSubscriberException(exception, failure.Context);
                }
            }
        }

        private static void LogSubscriberException(Exception exception, Object context)
        {
            try
            {
                // NOTE: 수집기 오류는 Console 전달 설정과 별도로 남긴다.
                // Warning을 사용해 CI에서 원래 Critical 실패를 추가 오류 로그로 가리지 않는다.
                Debug.LogWarning($"[Project Assert/Subscriber] Failed subscriber threw an exception:\n{exception}", context);
            }
            catch (Exception)
            {
                // NOTE: 커스텀 로그 핸들러의 예외도 구독자 통지와 원래 Critical 예외를 막지 않아야 한다.
            }
        }

        private static string CaptureStackTrace()
        {
            // 성능 메모: 성공한 Assert에서는 호출되지 않는다. 실패가 매 프레임 반복되면
            // 스택 프레임 조회와 문자열 할당도 반복되므로 상태가 비정상으로 바뀌는 시점에 보고한다.
            StackTrace stackTrace = new StackTrace(true);
            StackFrame[] frames = stackTrace.GetFrames();
            if (frames == null || frames.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (StackFrame frame in frames)
            {
                MethodBase method = frame.GetMethod();
                if (method == null || method.DeclaringType == typeof(ProjectAssert))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                string declaringTypeName = method.DeclaringType != null
                    ? method.DeclaringType.FullName
                    : "<unknown>";
                builder.Append(declaringTypeName);
                builder.Append('.');
                builder.Append(method.Name);
                builder.Append("()");

                string frameFilePath = frame.GetFileName();
                int frameLineNumber = frame.GetFileLineNumber();
                if (!string.IsNullOrEmpty(frameFilePath))
                {
                    builder.Append(" (at ");
                    builder.Append(frameFilePath.Replace('\\', '/'));
                    if (frameLineNumber > 0)
                    {
                        builder.Append(':');
                        builder.Append(frameLineNumber);
                    }

                    builder.Append(')');
                }
            }

            return builder.ToString();
        }
    }
}
