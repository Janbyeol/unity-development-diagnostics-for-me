using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DevelopmentDiagnostics.Editor.Assertions
{
    [Serializable]
    internal sealed class ProjectAssertRecordCollection
    {
        [FormerlySerializedAs("_records"),SerializeField] private List<ProjectAssertRecord> m_records = new List<ProjectAssertRecord>();

        internal List<ProjectAssertRecord> Records => m_records;
    }
}
