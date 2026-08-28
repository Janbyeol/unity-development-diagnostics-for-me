using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace DevelopmentDiagnostics.Editor.Debug
{
    [Serializable]
    internal sealed class ProjectDebugRecord
    {
        [FormerlySerializedAs("_timestampUtcTicks"),SerializeField] private long m_timestampUtcTicks;
        [FormerlySerializedAs("_type"),SerializeField] private ProjectDebugEntryType m_type;
        [FormerlySerializedAs("_severity"),SerializeField] private ProjectDebugSeverity m_severity;
        [FormerlySerializedAs("_environment"),SerializeField] private ProjectDebugEnvironment m_environment;
        [FormerlySerializedAs("_tag"),SerializeField] private string m_tag;
        [FormerlySerializedAs("_message"),SerializeField] private string m_message;
        [FormerlySerializedAs("_memberName"),SerializeField] private string m_memberName;
        [FormerlySerializedAs("_filePath"),SerializeField] private string m_filePath;
        [FormerlySerializedAs("_lineNumber"),SerializeField] private int m_lineNumber;
        [FormerlySerializedAs("_stackTrace"),SerializeField] private string m_stackTrace;
        [FormerlySerializedAs("_contextEntityId"),SerializeField] private string m_contextEntityId;
        [FormerlySerializedAs("_contextGlobalObjectId"),SerializeField] private string m_contextGlobalObjectId;

        [NonSerialized] private Object m_context;

        internal ProjectDebugRecord(ProjectDebugEntry entry)
        {
            m_timestampUtcTicks = entry.TimestampUtc.Ticks;
            m_type = entry.Type;
            m_severity = entry.Severity;
            m_environment = entry.Environment;
            m_tag = entry.Tag;
            m_message = entry.Message;
            m_memberName = entry.MemberName;
            m_filePath = entry.FilePath;
            m_lineNumber = entry.LineNumber;
            m_stackTrace = entry.StackTrace;
            m_context = entry.Context;

            if (entry.Context != null)
            {
                m_contextEntityId = EntityId.ToULong(entry.Context.GetEntityId()).ToString();
                m_contextGlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(entry.Context).ToString();
            }
        }

        internal ProjectDebugEntry ToEntry()
        {
            long timestampTicks = Math.Max(
                DateTime.MinValue.Ticks,
                Math.Min(DateTime.MaxValue.Ticks, m_timestampUtcTicks));
            DateTime timestampUtc = new DateTime(timestampTicks, DateTimeKind.Utc);

            return ProjectDebugEntry.Restore(
                timestampUtc,
                m_type,
                m_severity,
                m_environment,
                m_tag,
                m_message,
                Context,
                m_memberName,
                m_filePath,
                m_lineNumber,
                m_stackTrace);
        }

        private Object Context
        {
            get
            {
                if (m_context != null)
                {
                    return m_context;
                }

                if (ulong.TryParse(m_contextEntityId, out ulong contextEntityId))
                {
                    m_context = EditorUtility.EntityIdToObject(EntityId.FromULong(contextEntityId));
                }

                if (m_context == null &&
                    !string.IsNullOrEmpty(m_contextGlobalObjectId) &&
                    GlobalObjectId.TryParse(m_contextGlobalObjectId, out GlobalObjectId globalObjectId))
                {
                    m_context = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
                }

                return m_context;
            }
        }
    }
}
