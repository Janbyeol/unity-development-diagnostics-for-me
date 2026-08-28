namespace DevelopmentDiagnostics.Logging
{
    /// <summary>
    ///     Project Log 기록의 중요도를 나타낸다.
    /// </summary>
    public enum ProjectLogLevel
    {
        /// <summary>
        ///     일반 개발 정보다.
        /// </summary>
        Info,

        /// <summary>
        ///     확인이 필요하지만 실행을 계속할 수 있는 상태다.
        /// </summary>
        Warning,

        /// <summary>
        ///     기능 실패나 복구 불가능한 작업 결과다.
        /// </summary>
        Error
    }
}
