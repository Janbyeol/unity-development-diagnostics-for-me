using System.Collections.Generic;
using DevelopmentDiagnostics.Assertions;
using DevelopmentDiagnostics.Logging;
using UnityEditor;
using UnityEngine;

namespace DevelopmentDiagnostics.Editor.Assertions
{
    [InitializeOnLoad]
    internal static class ProjectAssertEditorController
    {
        private const string k_PendingExitFailuresSessionKey =
            "DevelopmentDiagnostics.Assertions.PendingPlayExitFailures";
        private const string k_PlayModeExitInProgressSessionKey =
            "DevelopmentDiagnostics.Assertions.PlayModeExitInProgress";

        private static readonly HashSet<string> IgnoredCallSites = new HashSet<string>();
        private static readonly HashSet<string> PendingCallSites = new HashSet<string>();
        private static readonly Queue<ProjectAssertRecord> PendingFailures = new Queue<ProjectAssertRecord>();

        private static bool s_isPopupScheduled;
        private static bool s_isPopupOpen;

        static ProjectAssertEditorController()
        {
            ApplySettings();
            ProjectAssert.Failed += HandleAssertFailed;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            RestorePersistedFailures();
            ScheduleNextPopup();
        }

        internal static void ApplySettings()
        {
            ProjectAssertSettings settings = ProjectAssertSettings.instance;
            ProjectAssert.ConfigureConsoleLogging(settings.AssertLogToConsole);
            ProjectLog.ConfigureConsoleLogging(settings.ProjectLogToConsole);
        }

        internal static void ResetCurrentPlayState()
        {
            IgnoredCallSites.Clear();
            PendingCallSites.Clear();
            PendingFailures.Clear();
            ClearPersistedFailures();
        }

        private static void HandleAssertFailed(ProjectAssertionFailure failure)
        {
            ProjectAssertSettings settings = ProjectAssertSettings.instance;

            if (!ShouldShowPopup(settings.PopupMode, failure.Severity))
            {
                return;
            }

            string callSiteKey = GetCallSiteKey(failure);
            if (IgnoredCallSites.Contains(callSiteKey) || !PendingCallSites.Add(callSiteKey))
            {
                return;
            }

            ProjectAssertRecord record = new ProjectAssertRecord(failure);
            PendingFailures.Enqueue(record);

            if (IsPlayModeExitInProgress())
            {
                PersistFailure(record);
            }

            ScheduleNextPopup();
        }

        private static bool ShouldShowPopup(
            ProjectAssertPopupMode popupMode,
            ProjectAssertSeverity severity)
        {
            switch (popupMode)
            {
                case ProjectAssertPopupMode.Off:
                    return false;

                case ProjectAssertPopupMode.CriticalOnly:
                    return severity == ProjectAssertSeverity.Critical;

                case ProjectAssertPopupMode.All:
                    return true;

                default:
                    return false;
            }
        }

        private static void ScheduleNextPopup()
        {
            if (s_isPopupScheduled ||
                s_isPopupOpen ||
                PendingFailures.Count == 0 ||
                IsPlayModeExitInProgress())
            {
                return;
            }

            s_isPopupScheduled = true;
            EditorApplication.delayCall += ShowNextPopup;
        }

        private static void ShowNextPopup()
        {
            s_isPopupScheduled = false;

            ProjectAssertSettings settings = ProjectAssertSettings.instance;
            ProjectAssertRecord failure = null;
            string callSiteKey = string.Empty;
            bool foundFailure = false;

            while (PendingFailures.Count > 0)
            {
                failure = PendingFailures.Dequeue();
                callSiteKey = GetCallSiteKey(failure);
                PendingCallSites.Remove(callSiteKey);

                // NOTE: 팝업이 열린 동안 같은 호출 위치가 다시 큐에 들어올 수 있으므로 표시 직전에 다시 검사한다.
                if (IgnoredCallSites.Contains(callSiteKey) ||
                    !ShouldShowPopup(settings.PopupMode, failure.Severity))
                {
                    continue;
                }

                foundFailure = true;
                break;
            }

            if (!foundFailure)
            {
                return;
            }

            bool wasPausedBeforePopup = EditorApplication.isPaused;

            if (settings.PausePlayModeBeforePopup && EditorApplication.isPlaying)
            {
                EditorApplication.isPaused = true;
            }

            s_isPopupOpen = true;
            ProjectAssertPopupWindow.ShowPopup(
                failure,
                result => HandlePopupCompleted(
                    result,
                    callSiteKey,
                    wasPausedBeforePopup));
        }

        private static void HandlePopupCompleted(
            ProjectAssertPopupResult result,
            string callSiteKey,
            bool wasPausedBeforePopup)
        {
            s_isPopupOpen = false;

            if (result == ProjectAssertPopupResult.StopPlayMode && EditorApplication.isPlaying)
            {
                PendingCallSites.Clear();
                PendingFailures.Clear();
                EditorApplication.isPlaying = false;
            }
            else if (result == ProjectAssertPopupResult.IgnoreForCurrentSession)
            {
                IgnoredCallSites.Add(callSiteKey);
            }

            // NOTE: Assert 창이 직접 멈춘 경우에만 재개한다. 사용자가 원래 Pause한 상태는 유지한다.
            bool shouldResume = result != ProjectAssertPopupResult.StopPlayMode &&
                EditorApplication.isPlaying &&
                !wasPausedBeforePopup;
            if (shouldResume)
            {
                EditorApplication.isPaused = false;
            }

            ScheduleNextPopup();
        }

        private static string GetCallSiteKey(ProjectAssertionFailure failure)
        {
            string callSiteKey = $"{failure.FilePath}:{failure.LineNumber}";
            if (string.IsNullOrEmpty(failure.FilePath))
            {
                callSiteKey = failure.Message;
            }

            return callSiteKey;
        }

        private static string GetCallSiteKey(ProjectAssertRecord failure)
        {
            string callSiteKey = $"{failure.FilePath}:{failure.LineNumber}";
            if (string.IsNullOrEmpty(failure.FilePath))
            {
                callSiteKey = failure.Message;
            }

            return callSiteKey;
        }

        private static bool IsPlayModeExitInProgress()
        {
            return SessionState.GetBool(k_PlayModeExitInProgressSessionKey, false);
        }

        private static void PersistFailure(ProjectAssertRecord failure)
        {
            string json = SessionState.GetString(k_PendingExitFailuresSessionKey, string.Empty);
            ProjectAssertRecordCollection collection = string.IsNullOrEmpty(json)
                ? new ProjectAssertRecordCollection()
                : JsonUtility.FromJson<ProjectAssertRecordCollection>(json);

            if (collection == null)
            {
                collection = new ProjectAssertRecordCollection();
            }

            collection.Records.Add(failure);
            SessionState.SetString(
                k_PendingExitFailuresSessionKey,
                JsonUtility.ToJson(collection));
        }

        private static void PersistPendingFailures()
        {
            ProjectAssertRecordCollection collection = new ProjectAssertRecordCollection();
            foreach (ProjectAssertRecord failure in PendingFailures)
            {
                collection.Records.Add(failure);
            }

            if (collection.Records.Count == 0)
            {
                ClearPersistedFailures();
                return;
            }

            SessionState.SetString(
                k_PendingExitFailuresSessionKey,
                JsonUtility.ToJson(collection));
        }

        private static void RestorePersistedFailures()
        {
            string json = SessionState.GetString(k_PendingExitFailuresSessionKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            ProjectAssertRecordCollection collection =
                JsonUtility.FromJson<ProjectAssertRecordCollection>(json);
            ClearPersistedFailures();

            if (collection == null || collection.Records == null)
            {
                return;
            }

            // NOTE: 도메인 리로드로 사라진 정적 큐를 SessionState 스냅샷으로 다시 만든다.
            foreach (ProjectAssertRecord failure in collection.Records)
            {
                string callSiteKey = GetCallSiteKey(failure);
                if (PendingCallSites.Add(callSiteKey))
                {
                    PendingFailures.Enqueue(failure);
                }
            }
        }

        private static void ClearPersistedFailures()
        {
            SessionState.SetString(k_PendingExitFailuresSessionKey, string.Empty);
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange stateChange)
        {
            // NOTE: EnteredPlayMode에서 초기화하면 Awake/OnEnable이 예약한 팝업까지 지워질 수 있다.
            if (stateChange == PlayModeStateChange.ExitingEditMode)
            {
                SessionState.SetBool(k_PlayModeExitInProgressSessionKey, false);
                ResetCurrentPlayState();
                return;
            }

            if (stateChange == PlayModeStateChange.ExitingPlayMode)
            {
                SessionState.SetBool(k_PlayModeExitInProgressSessionKey, true);
                // NOTE: 종료 직전 대기 중이던 Assert와 이후 OnDisable/OnDestroy Assert를 모두 보존한다.
                PersistPendingFailures();

                if (s_isPopupScheduled)
                {
                    EditorApplication.delayCall -= ShowNextPopup;
                    s_isPopupScheduled = false;
                }

                return;
            }

            if (stateChange == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(k_PlayModeExitInProgressSessionKey, false);
                IgnoredCallSites.Clear();
                RestorePersistedFailures();
                ScheduleNextPopup();
            }
        }
    }
}
