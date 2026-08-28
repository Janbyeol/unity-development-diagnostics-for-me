using System;
using ProjectEX.Debugging.Assertions;
using ProjectEX.Debugging.Logging;
using Object = UnityEngine.Object;

namespace ProjectEX.Editor.ProjectDebug
{
    internal sealed class ProjectDebugEntry
    {
        private ProjectDebugEntry(
            DateTime timestampUtc,
            ProjectDebugEntryType type,
            ProjectDebugSeverity severity,
            ProjectDebugEnvironment environment,
            string tag,
            string message,
            Object context,
            string memberName,
            string filePath,
            int lineNumber,
            string stackTrace)
        {
            TimestampUtc = timestampUtc;
            Type = type;
            Severity = severity;
            Environment = environment;
            Tag = tag ?? string.Empty;
            Message = message ?? string.Empty;
            Context = context;
            MemberName = memberName ?? string.Empty;
            FilePath = filePath ?? string.Empty;
            LineNumber = lineNumber;
            StackTrace = stackTrace ?? string.Empty;
        }

        internal DateTime TimestampUtc { get; }

        internal ProjectDebugEntryType Type { get; }

        internal ProjectDebugSeverity Severity { get; }

        internal ProjectDebugEnvironment Environment { get; }

        internal string Tag { get; }

        internal string Message { get; }

        internal Object Context { get; }

        internal string MemberName { get; }

        internal string FilePath { get; }

        internal int LineNumber { get; }

        internal string StackTrace { get; }

        internal static ProjectDebugEntry FromLog(
            ProjectLogEntry entry,
            ProjectDebugEnvironment environment)
        {
            return new ProjectDebugEntry(
                entry.TimestampUtc,
                ProjectDebugEntryType.Log,
                ConvertLogSeverity(entry.Level),
                environment,
                entry.Tag,
                entry.Message,
                entry.Context,
                entry.MemberName,
                entry.FilePath,
                entry.LineNumber,
                string.Empty);
        }

        internal static ProjectDebugEntry FromAssert(
            ProjectAssertionFailure failure,
            ProjectDebugEnvironment environment)
        {
            ProjectDebugSeverity severity = failure.Severity == ProjectAssertSeverity.Critical
                ? ProjectDebugSeverity.CriticalAssert
                : ProjectDebugSeverity.NormalAssert;

            return new ProjectDebugEntry(
                DateTime.UtcNow,
                ProjectDebugEntryType.Assert,
                severity,
                environment,
                string.Empty,
                failure.Message,
                failure.Context,
                failure.MemberName,
                failure.FilePath,
                failure.LineNumber,
                failure.StackTrace);
        }

        internal static ProjectDebugEntry Restore(
            DateTime timestampUtc,
            ProjectDebugEntryType type,
            ProjectDebugSeverity severity,
            ProjectDebugEnvironment environment,
            string tag,
            string message,
            Object context,
            string memberName,
            string filePath,
            int lineNumber,
            string stackTrace)
        {
            return new ProjectDebugEntry(
                timestampUtc,
                type,
                severity,
                environment,
                tag,
                message,
                context,
                memberName,
                filePath,
                lineNumber,
                stackTrace);
        }

        private static ProjectDebugSeverity ConvertLogSeverity(ProjectLogLevel level)
        {
            switch (level)
            {
                case ProjectLogLevel.Warning:
                    return ProjectDebugSeverity.Warning;

                case ProjectLogLevel.Error:
                    return ProjectDebugSeverity.Error;

                default:
                    return ProjectDebugSeverity.Info;
            }
        }
    }
}
