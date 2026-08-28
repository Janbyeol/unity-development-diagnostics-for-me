using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace DevelopmentDiagnostics.Editor.Assertions
{
    [FilePath(
        "ProjectSettings/DevelopmentDiagnosticsSettings.asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal sealed class ProjectAssertSettings : ScriptableSingleton<ProjectAssertSettings>
    {
        [SerializeField] private ProjectAssertPopupMode m_popupMode = ProjectAssertPopupMode.CriticalOnly;
        [SerializeField] private bool m_pausePlayModeBeforePopup = true;
        [SerializeField] private bool m_assertLogToConsole;
        [SerializeField] private bool m_projectLogToConsole;
        [SerializeField] private float m_popupWidth = 1280f;
        [SerializeField] private float m_popupHeight = 720f;
        [SerializeField] private int m_messageFontSize = 18;

        internal ProjectAssertPopupMode PopupMode => m_popupMode;

        internal bool PausePlayModeBeforePopup => m_pausePlayModeBeforePopup;

        internal bool AssertLogToConsole => m_assertLogToConsole;

        internal bool ProjectLogToConsole => m_projectLogToConsole;

        internal float PopupWidth => m_popupWidth;

        internal float PopupHeight => m_popupHeight;

        internal int MessageFontSize => m_messageFontSize;

        internal void SaveSettings()
        {
            Save(true);
        }
    }
}
