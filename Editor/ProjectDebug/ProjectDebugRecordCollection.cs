using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectEX.Editor.ProjectDebug
{
    [Serializable]
    internal sealed class ProjectDebugRecordCollection
    {
        [FormerlySerializedAs("_records"),SerializeField] private List<ProjectDebugRecord> m_records = new List<ProjectDebugRecord>();

        internal List<ProjectDebugRecord> Records => m_records;
    }
}
