#undef UNITY_ASSERTIONS

using DevelopmentDiagnostics.Assertions;

namespace DevelopmentDiagnostics.Tests
{
    internal static class ProjectAssertNoSymbolProbe
    {
        internal static int Run()
        {
            int evaluations = 0;
            ProjectAssert.Normal(++evaluations > 0, (++evaluations).ToString());
            ProjectAssert.Normal((++evaluations).ToString());
            ProjectAssert.Critical(++evaluations > 0, (++evaluations).ToString());
            ProjectAssert.Critical((++evaluations).ToString());
            return evaluations;
        }
    }
}
