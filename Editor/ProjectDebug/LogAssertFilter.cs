using System;
using System.Collections.Generic;

namespace DevelopmentDiagnostics.Editor.Debug
{
    internal enum LogAssertEnvironmentFilter
    {
        All,
        PlaySession,
        Editor
    }

    internal sealed class LogAssertFilter
    {
        private readonly List<ProjectDebugEntry> m_filteredEntries = new List<ProjectDebugEntry>();
        private readonly HashSet<string> m_availableTags =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private string m_searchText = string.Empty;
        private string m_selectedTag;
        private LogAssertEnvironmentFilter m_environment = LogAssertEnvironmentFilter.All;
        private bool m_showLogs = true;
        private bool m_showAsserts = true;
        private bool m_showInfo = true;
        private bool m_showWarning = true;
        private bool m_showError = true;
        private bool m_showNormalAssert = true;
        private bool m_showCriticalAssert = true;
        private int m_logEntryCount;
        private int m_assertEntryCount;
        private int m_allEntryCount;
        private int m_playEntryCount;
        private int m_editorEntryCount;

        internal IReadOnlyList<ProjectDebugEntry> FilteredEntries => m_filteredEntries;

        internal string SearchText
        {
            get => m_searchText;
            set => m_searchText = value ?? string.Empty;
        }

        internal string SelectedTag => m_selectedTag;

        internal bool HasSelectedTag => !string.IsNullOrEmpty(m_selectedTag);

        internal LogAssertEnvironmentFilter Environment
        {
            get => m_environment;
            set => m_environment = value;
        }

        internal bool ShowLogs
        {
            get => m_showLogs;
            set => m_showLogs = value;
        }

        internal bool ShowAsserts
        {
            get => m_showAsserts;
            set => m_showAsserts = value;
        }

        internal bool ShowInfo
        {
            get => m_showInfo;
            set => m_showInfo = value;
        }

        internal bool ShowWarning
        {
            get => m_showWarning;
            set => m_showWarning = value;
        }

        internal bool ShowError
        {
            get => m_showError;
            set => m_showError = value;
        }

        internal bool ShowNormalAssert
        {
            get => m_showNormalAssert;
            set => m_showNormalAssert = value;
        }

        internal bool ShowCriticalAssert
        {
            get => m_showCriticalAssert;
            set => m_showCriticalAssert = value;
        }

        internal int LogEntryCount => m_logEntryCount;

        internal int AssertEntryCount => m_assertEntryCount;

        internal int AllEntryCount => m_allEntryCount;

        internal int PlayEntryCount => m_playEntryCount;

        internal int EditorEntryCount => m_editorEntryCount;

        internal void SelectTag(string tag)
        {
            m_selectedTag = string.IsNullOrEmpty(tag) ? null : tag;
        }

        internal bool IsTagAvailable(string tag)
        {
            return !string.IsNullOrEmpty(tag) && m_availableTags.Contains(tag);
        }

        internal void Rebuild(IReadOnlyList<ProjectDebugEntry> entries)
        {
            m_filteredEntries.Clear();
            m_availableTags.Clear();
            m_logEntryCount = 0;
            m_assertEntryCount = 0;
            m_playEntryCount = 0;
            m_editorEntryCount = 0;
            m_allEntryCount = entries?.Count ?? 0;

            if (entries == null)
            {
                m_selectedTag = null;
                return;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                ProjectDebugEntry entry = entries[index];
                if (entry.Environment == ProjectDebugEnvironment.PlaySession)
                {
                    m_playEntryCount++;
                }
                else
                {
                    m_editorEntryCount++;
                }

                if (!PassesEnvironmentFilter(entry))
                {
                    continue;
                }

                if (entry.Type == ProjectDebugEntryType.Log)
                {
                    m_logEntryCount++;
                    m_availableTags.Add(entry.Tag);
                }
                else
                {
                    m_assertEntryCount++;
                }

                if (PassesFilters(entry))
                {
                    m_filteredEntries.Add(entry);
                }
            }

            if (HasSelectedTag && !m_availableTags.Contains(m_selectedTag))
            {
                m_selectedTag = null;
                RebuildFilteredEntries(entries);
            }
        }

        private void RebuildFilteredEntries(IReadOnlyList<ProjectDebugEntry> entries)
        {
            m_filteredEntries.Clear();
            for (int index = 0; index < entries.Count; index++)
            {
                ProjectDebugEntry entry = entries[index];
                if (PassesFilters(entry))
                {
                    m_filteredEntries.Add(entry);
                }
            }
        }

        private bool PassesFilters(ProjectDebugEntry entry)
        {
            if (!PassesEnvironmentFilter(entry))
            {
                return false;
            }

            if (entry.Type == ProjectDebugEntryType.Log && !m_showLogs)
            {
                return false;
            }

            if (entry.Type == ProjectDebugEntryType.Assert && !m_showAsserts)
            {
                return false;
            }

            if (!IsSeverityVisible(entry.Severity))
            {
                return false;
            }

            if (HasSelectedTag &&
                (entry.Type != ProjectDebugEntryType.Log ||
                 !string.Equals(entry.Tag, m_selectedTag, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(m_searchText))
            {
                return true;
            }

            string searchText = m_searchText.Trim();
            return ContainsIgnoreCase(entry.Message, searchText) ||
                ContainsIgnoreCase(entry.Tag, searchText) ||
                ContainsIgnoreCase(entry.MemberName, searchText) ||
                ContainsIgnoreCase(entry.FilePath, searchText);
        }

        private bool PassesEnvironmentFilter(ProjectDebugEntry entry)
        {
            switch (m_environment)
            {
                case LogAssertEnvironmentFilter.PlaySession:
                    return entry.Environment == ProjectDebugEnvironment.PlaySession;

                case LogAssertEnvironmentFilter.Editor:
                    return entry.Environment == ProjectDebugEnvironment.Editor;

                default:
                    return true;
            }
        }

        private bool IsSeverityVisible(ProjectDebugSeverity severity)
        {
            switch (severity)
            {
                case ProjectDebugSeverity.Info:
                    return m_showInfo;

                case ProjectDebugSeverity.Warning:
                    return m_showWarning;

                case ProjectDebugSeverity.Error:
                    return m_showError;

                case ProjectDebugSeverity.NormalAssert:
                    return m_showNormalAssert;

                case ProjectDebugSeverity.CriticalAssert:
                    return m_showCriticalAssert;

                default:
                    return true;
            }
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            return !string.IsNullOrEmpty(source) &&
                source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
