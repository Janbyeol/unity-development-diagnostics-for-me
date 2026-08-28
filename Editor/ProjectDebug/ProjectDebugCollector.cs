using System;
using System.Collections.Generic;
using DevelopmentDiagnostics.Assertions;
using DevelopmentDiagnostics.Logging;
using UnityEditor;
using UnityEngine;

namespace DevelopmentDiagnostics.Editor.Debug
{
    [InitializeOnLoad]
    internal static class ProjectDebugCollector
    {
        private const int k_MaximumEntryCount = 5000;
        private const int k_TrimEntryCount = 250;
        private const int k_MaximumPersistedEntryCountPerEnvironment = 500;
        private const string k_EntriesSessionKey = "DevelopmentDiagnostics.Debug.Entries";
        private const string k_EnvironmentSessionKey = "DevelopmentDiagnostics.Debug.Environment";
        private const string k_EditorDirectorySegment = "/Editor/";

        private static readonly List<ProjectDebugEntry> CollectedEntries = new List<ProjectDebugEntry>();
        private static readonly SortedSet<string> CollectedTags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        private static ProjectDebugEnvironment s_currentEnvironment;
        private static bool s_isAssemblyReloadInProgress;

        internal static event Action Changed;

        static ProjectDebugCollector()
        {
            s_currentEnvironment = RestoreCurrentEnvironment();
            RestorePersistedEntries();
            ProjectLog.Written += HandleLogWritten;
            ProjectAssert.Failed += HandleAssertFailed;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += HandleBeforeAssemblyReload;
        }

        internal static IReadOnlyList<ProjectDebugEntry> Entries => CollectedEntries;

        internal static IReadOnlyCollection<string> KnownTags => CollectedTags;

        internal static void Clear()
        {
            CollectedEntries.Clear();
            CollectedTags.Clear();
            ClearPersistedEntries();
            NotifyChanged();
        }

        internal static void Clear(ProjectDebugEnvironment environment)
        {
            CollectedEntries.RemoveAll(entry => entry.Environment == environment);
            RebuildCollectedTags();
            PersistEntries();
            NotifyChanged();
        }

        private static void HandleLogWritten(ProjectLogEntry entry)
        {
            AddEntry(ProjectDebugEntry.FromLog(
                entry,
                ResolveCurrentEnvironment(entry.FilePath)));
        }

        private static void HandleAssertFailed(ProjectAssertionFailure failure)
        {
            AddEntry(ProjectDebugEntry.FromAssert(
                failure,
                ResolveCurrentEnvironment(failure.FilePath)));
        }

        private static void AddEntry(ProjectDebugEntry entry)
        {
            if (CollectedEntries.Count >= k_MaximumEntryCount)
            {
                CollectedEntries.RemoveRange(0, k_TrimEntryCount);
                RebuildCollectedTags();
            }

            CollectedEntries.Add(entry);
            if (entry.Type == ProjectDebugEntryType.Log)
            {
                CollectedTags.Add(entry.Tag);
            }

            // WARNING: beforeAssemblyReload 뒤에 호출되는 OnDisable/OnDestroy 기록은
            // 여기서 다시 저장하지 않으면 새 도메인으로 복원되지 않는다.
            if (s_isAssemblyReloadInProgress)
            {
                PersistEntries();
            }

            NotifyChanged();
        }

        private static void HandleBeforeAssemblyReload()
        {
            s_isAssemblyReloadInProgress = true;
            PersistEntries();
        }

        private static ProjectDebugEnvironment ResolveCurrentEnvironment(string filePath)
        {
            // NOTE: Play 중 실행되더라도 Editor 전용 도구가 남긴 기록은 Editor 탭에서 관리한다.
            if (IsEditorSource(filePath))
            {
                return ProjectDebugEnvironment.Editor;
            }

            if (s_currentEnvironment == ProjectDebugEnvironment.PlaySession ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return ProjectDebugEnvironment.PlaySession;
            }

            return ProjectDebugEnvironment.Editor;
        }

        private static bool IsEditorSource(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            string normalizedPath = filePath.Replace('\\', '/');
            return normalizedPath.StartsWith("Editor/", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.IndexOf(k_EditorDirectorySegment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.ExitingEditMode)
            {
                SetCurrentEnvironment(ProjectDebugEnvironment.PlaySession);
                // NOTE: 직전 Play 기록은 다음 Play 세션이 시작될 때만 제거한다.
                Clear(ProjectDebugEnvironment.PlaySession);
                return;
            }

            if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                SetCurrentEnvironment(ProjectDebugEnvironment.PlaySession);
                return;
            }

            if (stateChange == PlayModeStateChange.ExitingPlayMode)
            {
                // NOTE: 종료 중 OnDisable/OnDestroy 기록도 같은 Play 세션으로 분류한다.
                SetCurrentEnvironment(ProjectDebugEnvironment.PlaySession);
                PersistEntries();
                return;
            }

            if (stateChange == PlayModeStateChange.EnteredEditMode)
            {
                SetCurrentEnvironment(ProjectDebugEnvironment.Editor);
                PersistEntries();
            }
        }

        private static ProjectDebugEnvironment RestoreCurrentEnvironment()
        {
            int storedValue = SessionState.GetInt(k_EnvironmentSessionKey, -1);
            if (Enum.IsDefined(typeof(ProjectDebugEnvironment), storedValue))
            {
                return (ProjectDebugEnvironment)storedValue;
            }

            return EditorApplication.isPlayingOrWillChangePlaymode
                ? ProjectDebugEnvironment.PlaySession
                : ProjectDebugEnvironment.Editor;
        }

        private static void SetCurrentEnvironment(ProjectDebugEnvironment environment)
        {
            s_currentEnvironment = environment;
            SessionState.SetInt(k_EnvironmentSessionKey, (int)environment);
        }

        private static void PersistEntries()
        {
            ProjectDebugRecordCollection collection = new ProjectDebugRecordCollection();
            int editorEntryCount = 0;
            int playEntryCount = 0;

            for (int index = CollectedEntries.Count - 1; index >= 0; index--)
            {
                ProjectDebugEntry entry = CollectedEntries[index];
                if (entry.Environment == ProjectDebugEnvironment.Editor)
                {
                    if (editorEntryCount >= k_MaximumPersistedEntryCountPerEnvironment)
                    {
                        continue;
                    }

                    editorEntryCount++;
                }
                else
                {
                    if (playEntryCount >= k_MaximumPersistedEntryCountPerEnvironment)
                    {
                        continue;
                    }

                    playEntryCount++;
                }

                collection.Records.Add(new ProjectDebugRecord(entry));
            }

            // NOTE: 뒤에서부터 수집한 스냅샷을 원래 발생 순서로 되돌린다.
            collection.Records.Reverse();
            SessionState.SetString(k_EntriesSessionKey, JsonUtility.ToJson(collection));
        }

        private static void RestorePersistedEntries()
        {
            string json = SessionState.GetString(k_EntriesSessionKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            ProjectDebugRecordCollection collection;
            try
            {
                collection = JsonUtility.FromJson<ProjectDebugRecordCollection>(json);
            }
            catch (ArgumentException)
            {
                ClearPersistedEntries();
                return;
            }

            if (collection == null || collection.Records == null)
            {
                ClearPersistedEntries();
                return;
            }

            foreach (ProjectDebugRecord record in collection.Records)
            {
                if (record == null)
                {
                    continue;
                }

                CollectedEntries.Add(record.ToEntry());
            }

            RebuildCollectedTags();
        }

        private static void ClearPersistedEntries()
        {
            SessionState.SetString(k_EntriesSessionKey, string.Empty);
        }

        private static void RebuildCollectedTags()
        {
            CollectedTags.Clear();
            foreach (ProjectDebugEntry entry in CollectedEntries)
            {
                if (entry.Type == ProjectDebugEntryType.Log)
                {
                    CollectedTags.Add(entry.Tag);
                }
            }
        }

        private static void NotifyChanged()
        {
            Action changedHandler = Changed;
            changedHandler?.Invoke();
        }
    }
}
