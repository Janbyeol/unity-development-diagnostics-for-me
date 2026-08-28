using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DevelopmentDiagnostics.Editor.Debug
{
    /// <summary>
    ///     설치된 디버그 패널의 생명주기와 화면 출력을 소유하는 공용 Editor Window입니다.
    /// </summary>
    internal sealed class ProjectDebugWindow : EditorWindow
    {
        private const float k_PanelToolbarHeight = 22f;

        private readonly List<IDebugPanel> m_panels = new List<IDebugPanel>();

        [SerializeField] private string m_activePanelId;
        private IDebugPanel m_activePanel;
        private string[] m_panelNames = System.Array.Empty<string>();

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
            DiscoverPanels();
        }

        private void OnDisable()
        {
            m_activePanel?.OnDisable();
            m_activePanel = null;
            m_panels.Clear();
            m_panelNames = System.Array.Empty<string>();
        }

        private void OnGUI()
        {
            if (m_panels.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "등록된 Debug Panel이 없습니다. IDebugPanel 구현체를 추가해 주세요.",
                    MessageType.Info);
                return;
            }

            if (m_activePanel == null)
            {
                ActivatePanel(m_panels[0]);
            }

            DrawPanelToolbar();

            float toolbarHeight = m_panels.Count > 1 ? k_PanelToolbarHeight : 0f;
            Rect panelRect = new Rect(0f, toolbarHeight, position.width, position.height - toolbarHeight);
            m_activePanel.OnGUI(panelRect);
        }

        private void DiscoverPanels()
        {
            m_activePanel?.OnDisable();
            m_activePanel = null;
            m_panels.Clear();
            m_panels.AddRange(DebugPanelRegistry.CreatePanels());
            m_panelNames = new string[m_panels.Count];
            for (int index = 0; index < m_panels.Count; index++)
            {
                m_panelNames[index] = m_panels[index].DisplayName;
            }

            IDebugPanel selectedPanel = FindPanel(m_activePanelId);
            ActivatePanel(selectedPanel ?? (m_panels.Count > 0 ? m_panels[0] : null));
        }

        private void DrawPanelToolbar()
        {
            if (m_panels.Count <= 1)
            {
                return;
            }

            int activePanelIndex = 0;
            for (int index = 0; index < m_panels.Count; index++)
            {
                if (ReferenceEquals(m_panels[index], m_activePanel))
                {
                    activePanelIndex = index;
                }
            }

            int selectedPanelIndex = GUILayout.Toolbar(
                activePanelIndex,
                m_panelNames,
                EditorStyles.toolbarButton,
                GUILayout.Height(k_PanelToolbarHeight));
            if (selectedPanelIndex != activePanelIndex)
            {
                ActivatePanel(m_panels[selectedPanelIndex]);
            }
        }

        private IDebugPanel FindPanel(string panelId)
        {
            for (int index = 0; index < m_panels.Count; index++)
            {
                if (m_panels[index].Id == panelId)
                {
                    return m_panels[index];
                }
            }

            return null;
        }

        private void ActivatePanel(IDebugPanel panel)
        {
            if (ReferenceEquals(panel, m_activePanel))
            {
                return;
            }

            m_activePanel?.OnDisable();
            m_activePanel = panel;
            m_activePanelId = panel?.Id;
            m_activePanel?.OnEnable(Repaint);
            Repaint();
        }
    }
}
