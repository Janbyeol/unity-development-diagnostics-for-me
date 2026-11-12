using UnityEditor;
using UnityEngine;

namespace DevelopmentDiagnostics.Editor.Debug
{
    /// <summary>
    ///     Project Log와 Assert 기록을 보여주는 Editor Window입니다.
    /// </summary>
    internal sealed class ProjectDebugWindow : EditorWindow
    {
        private LogAssertPanel m_logAssertPanel;

        [MenuItem("Tools/Development Diagnostics/Debug Window")]
        private static void OpenWindow()
        {
            ProjectDebugWindow window = GetWindow<ProjectDebugWindow>();
            window.titleContent = new GUIContent("Dev Diagnostics");
            window.minSize = new Vector2(760f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            m_logAssertPanel?.OnDisable();
            m_logAssertPanel = new LogAssertPanel();
            m_logAssertPanel.OnEnable(Repaint);
        }

        private void OnDisable()
        {
            m_logAssertPanel?.OnDisable();
            m_logAssertPanel = null;
        }

        private void OnGUI()
        {
            m_logAssertPanel?.OnGUI(new Rect(0f, 0f, position.width, position.height));
        }
    }
}
