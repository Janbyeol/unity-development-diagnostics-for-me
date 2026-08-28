using System;
using DevelopmentDiagnostics.Assertions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace DevelopmentDiagnostics.Editor.Assertions
{
    [Serializable]
    internal sealed class ProjectAssertRecord
    {
        [FormerlySerializedAs("_message"),SerializeField] private string m_message;
        [FormerlySerializedAs("_severity"),SerializeField] private ProjectAssertSeverity m_severity;
        [FormerlySerializedAs("_memberName"),SerializeField] private string m_memberName;
        [FormerlySerializedAs("_filePath"),SerializeField] private string m_filePath;
        [FormerlySerializedAs("_lineNumber"),SerializeField] private int m_lineNumber;
        [FormerlySerializedAs("_stackTrace"),SerializeField] private string m_stackTrace;
        [FormerlySerializedAs("_contextEntityId"),SerializeField] private string m_contextEntityId;
        [FormerlySerializedAs("_contextGlobalObjectId"),SerializeField] private string m_contextGlobalObjectId;

        [NonSerialized] private Object m_context;

        internal ProjectAssertRecord(ProjectAssertionFailure failure)
        {
            m_message = failure.Message;
            m_severity = failure.Severity;
            m_memberName = failure.MemberName;
            m_filePath = failure.FilePath;
            m_lineNumber = failure.LineNumber;
            m_stackTrace = failure.StackTrace;
            m_context = failure.Context;

            if (failure.Context != null)
            {
                m_contextEntityId = EntityId.ToULong(failure.Context.GetEntityId()).ToString();
                m_contextGlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(failure.Context).ToString();
            }
        }

        internal string Message => m_message;

        internal ProjectAssertSeverity Severity => m_severity;

        internal string MemberName => m_memberName;

        internal string FilePath => m_filePath;

        internal int LineNumber => m_lineNumber;

        internal string StackTrace => m_stackTrace;

        internal Object Context
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
