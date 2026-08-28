using System;
using Object = UnityEngine.Object;

namespace ProjectEX.Debugging.Logging
{
    /// <summary>
    ///     한 번의 Project Log 기록과 호출 위치를 전달하는 값 타입이다.
    /// </summary>
    public readonly struct ProjectLogEntry
    {
        internal ProjectLogEntry(
            DateTime timestampUtc,
            ProjectLogLevel level,
            string tag,
            string message,
            Object context,
            string memberName,
            string filePath,
            int lineNumber)
        {
            TimestampUtc = timestampUtc;
            Level = level;
            Tag = tag;
            Message = message;
            Context = context;
            MemberName = memberName;
            FilePath = filePath;
            LineNumber = lineNumber;
        }

        /// <summary>
        ///     로그가 기록된 UTC 시각을 가져온다.
        /// </summary>
        public DateTime TimestampUtc { get; }

        /// <summary>
        ///     로그 단계를 가져온다.
        /// </summary>
        public ProjectLogLevel Level { get; }

        /// <summary>
        ///     전용 로그 창에서 필터링할 Tag를 가져온다.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        ///     개발자가 작성한 메시지를 가져온다.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     로그와 관련된 Unity 오브젝트를 가져온다.
        /// </summary>
        public Object Context { get; }

        /// <summary>
        ///     로그를 호출한 메서드 이름을 가져온다.
        /// </summary>
        public string MemberName { get; }

        /// <summary>
        ///     로그를 호출한 소스 파일 경로를 가져온다.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        ///     로그를 호출한 소스 줄 번호를 가져온다.
        /// </summary>
        public int LineNumber { get; }
    }
}
