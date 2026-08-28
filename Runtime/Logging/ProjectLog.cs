using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace ProjectEX.Debugging.Logging
{
    /// <summary>
    ///     Project EX의 개발 로그를 기록하는 공용 진입점이다.
    /// </summary>
    public static class ProjectLog
    {
        private const string k_DefaultTag = "General";

        private static bool s_logToConsole;

        /// <summary>
        ///     새 로그가 기록됐을 때 발행된다. 전용 로그 창 같은 표시 도구가 구독한다.
        /// </summary>
        public static event Action<ProjectLogEntry> Written;

        /// <summary>
        ///     Project Log를 Unity Console에도 기록할지 설정한다.
        /// </summary>
        /// <param name="logToConsole">Unity Console에도 기록하려면 참이다.</param>
        public static void ConfigureConsoleLogging(bool logToConsole)
        {
            s_logToConsole = logToConsole;
        }

        /// <summary>
        ///     일반 개발 정보를 기록한다.
        ///     UNITY_ASSERTIONS가 없는 일반 릴리스 빌드에서는 호출과 인자 평가가 제거된다.
        /// </summary>
        /// <param name="message">기록할 메시지다.</param>
        /// <param name="context">로그와 관련된 Unity 오브젝트다.</param>
        /// <param name="memberName">호출한 메서드 이름이다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="filePath">호출한 소스 파일 경로다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="lineNumber">호출한 소스 줄 번호다. 컴파일러가 자동으로 채운다.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void Info(
            string message,
            Object context = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Write(
                ProjectLogLevel.Info,
                k_DefaultTag,
                message,
                context,
                memberName,
                filePath,
                lineNumber);
        }

        /// <summary>
        ///     필터 Tag가 지정된 일반 개발 정보를 기록한다.
        ///     UNITY_ASSERTIONS가 없는 일반 릴리스 빌드에서는 호출과 인자 평가가 제거된다.
        /// </summary>
        /// <param name="tag">전용 로그 창에서 필터링할 분류다.</param>
        /// <param name="message">기록할 메시지다.</param>
        /// <param name="context">로그와 관련된 Unity 오브젝트다.</param>
        /// <param name="memberName">호출한 메서드 이름이다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="filePath">호출한 소스 파일 경로다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="lineNumber">호출한 소스 줄 번호다. 컴파일러가 자동으로 채운다.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void Info(
            string tag,
            string message,
            Object context = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Write(
                ProjectLogLevel.Info,
                tag,
                message,
                context,
                memberName,
                filePath,
                lineNumber);
        }

        /// <summary>
        ///     확인이 필요하지만 실행을 계속할 수 있는 상태를 기록한다.
        ///     UNITY_ASSERTIONS가 없는 일반 릴리스 빌드에서는 호출과 인자 평가가 제거된다.
        /// </summary>
        /// <param name="message">기록할 메시지다.</param>
        /// <param name="context">로그와 관련된 Unity 오브젝트다.</param>
        /// <param name="memberName">호출한 메서드 이름이다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="filePath">호출한 소스 파일 경로다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="lineNumber">호출한 소스 줄 번호다. 컴파일러가 자동으로 채운다.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void Warning(
            string message,
            Object context = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Write(
                ProjectLogLevel.Warning,
                k_DefaultTag,
                message,
                context,
                memberName,
                filePath,
                lineNumber);
        }

        /// <summary>
        ///     필터 Tag가 지정된 확인 필요 상태를 기록한다.
        ///     UNITY_ASSERTIONS가 없는 일반 릴리스 빌드에서는 호출과 인자 평가가 제거된다.
        /// </summary>
        /// <param name="tag">전용 로그 창에서 필터링할 분류다.</param>
        /// <param name="message">기록할 메시지다.</param>
        /// <param name="context">로그와 관련된 Unity 오브젝트다.</param>
        /// <param name="memberName">호출한 메서드 이름이다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="filePath">호출한 소스 파일 경로다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="lineNumber">호출한 소스 줄 번호다. 컴파일러가 자동으로 채운다.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void Warning(
            string tag,
            string message,
            Object context = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Write(
                ProjectLogLevel.Warning,
                tag,
                message,
                context,
                memberName,
                filePath,
                lineNumber);
        }

        /// <summary>
        ///     기능 실패나 복구 불가능한 작업 결과를 기록한다.
        ///     UNITY_ASSERTIONS가 없는 일반 릴리스 빌드에서는 호출과 인자 평가가 제거된다.
        /// </summary>
        /// <param name="message">기록할 메시지다.</param>
        /// <param name="context">로그와 관련된 Unity 오브젝트다.</param>
        /// <param name="memberName">호출한 메서드 이름이다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="filePath">호출한 소스 파일 경로다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="lineNumber">호출한 소스 줄 번호다. 컴파일러가 자동으로 채운다.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void Error(
            string message,
            Object context = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Write(
                ProjectLogLevel.Error,
                k_DefaultTag,
                message,
                context,
                memberName,
                filePath,
                lineNumber);
        }

        /// <summary>
        ///     필터 Tag가 지정된 기능 실패 상태를 기록한다.
        ///     UNITY_ASSERTIONS가 없는 일반 릴리스 빌드에서는 호출과 인자 평가가 제거된다.
        /// </summary>
        /// <param name="tag">전용 로그 창에서 필터링할 분류다.</param>
        /// <param name="message">기록할 메시지다.</param>
        /// <param name="context">로그와 관련된 Unity 오브젝트다.</param>
        /// <param name="memberName">호출한 메서드 이름이다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="filePath">호출한 소스 파일 경로다. 컴파일러가 자동으로 채운다.</param>
        /// <param name="lineNumber">호출한 소스 줄 번호다. 컴파일러가 자동으로 채운다.</param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void Error(
            string tag,
            string message,
            Object context = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Write(
                ProjectLogLevel.Error,
                tag,
                message,
                context,
                memberName,
                filePath,
                lineNumber);
        }

        private static void Write(
            ProjectLogLevel level,
            string tag,
            string message,
            Object context,
            string memberName,
            string filePath,
            int lineNumber)
        {
            ProjectLogEntry entry = new ProjectLogEntry(
                DateTime.UtcNow,
                level,
                NormalizeTag(tag),
                message,
                context,
                memberName,
                filePath,
                lineNumber);

            if (s_logToConsole)
            {
                WriteToConsole(entry);
            }

            Action<ProjectLogEntry> writtenHandler = Written;
            writtenHandler?.Invoke(entry);
        }

        private static void WriteToConsole(ProjectLogEntry entry)
        {
            string formattedMessage = $"[Project Log/{entry.Level}/{entry.Tag}] {entry.Message}";

            switch (entry.Level)
            {
                case ProjectLogLevel.Info:
                    Debug.Log(formattedMessage, entry.Context);
                    break;

                case ProjectLogLevel.Warning:
                    Debug.LogWarning(formattedMessage, entry.Context);
                    break;

                case ProjectLogLevel.Error:
                    Debug.LogError(formattedMessage, entry.Context);
                    break;

                default:
                    Debug.Log(formattedMessage, entry.Context);
                    break;
            }
        }

        private static string NormalizeTag(string tag)
        {
            return string.IsNullOrWhiteSpace(tag)
                ? k_DefaultTag
                : tag.Trim();
        }
    }
}
