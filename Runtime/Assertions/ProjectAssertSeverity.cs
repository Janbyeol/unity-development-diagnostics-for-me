namespace ProjectEX.Debugging.Assertions
{
    /// <summary>
    /// Assert 실패가 게임 실행에 미치는 심각도를 나타낸다.
    /// </summary>
    public enum ProjectAssertSeverity
    {
        /// <summary>
        /// 잘못된 상태이지만 다른 기능을 확인하기 위해 실행을 이어갈 수 있다.
        /// </summary>
        Normal,

        /// <summary>
        /// 계속 실행하면 결과를 신뢰하기 어렵거나 추가 오류가 연쇄적으로 발생할 수 있다.
        /// </summary>
        Critical
    }
}
