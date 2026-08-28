using System;
using System.Text;
using ProjectEX.Debugging.Assertions;
using UnityEditor;
using UnityEngine;

namespace ProjectEX.Editor.ProjectAssertions
{
    internal sealed class ProjectAssertPopupWindow : EditorWindow
    {
        private const float k_MinimumWidth = 640f;
        private const float k_MinimumHeight = 400f;
        private const float k_MaximumWidth = 1400f;
        private const float k_MaximumHeight = 900f;
        private const float k_TopStripeHeight = 8f;
        private const float k_ContentPadding = 20f;
        private const float k_ActionButtonHeight = 42f;
        private const float k_DebugPanelSplitterHeight = 7f;
        private const float k_ActionPanelSplitterHeight = 7f;
        private const float k_DefaultDetailsPanelHeight = 110f;
        private const float k_MinimumDetailsPanelHeight = 70f;
        private const float k_DefaultCallStackPanelHeight = 180f;
        private const float k_MinimumCallStackPanelHeight = 90f;
        private const float k_FixedLayoutHeight = 380f;

        private static readonly int DebugPanelSplitterControlHash =
            "ProjectAssertDebugPanelSplitter".GetHashCode();
        private static readonly int ActionPanelSplitterControlHash =
            "ProjectAssertActionPanelSplitter".GetHashCode();

        private ProjectAssertRecord m_failure;
        private Action<ProjectAssertPopupResult> m_completed;
        private Vector2 m_detailsScrollPosition;
        private Vector2 m_callStackScrollPosition;
        private float m_detailsPanelHeight = k_DefaultDetailsPanelHeight;
        private float m_callStackPanelHeight = k_DefaultCallStackPanelHeight;
        private GUIStyle m_titleStyle;
        private GUIStyle m_severityStyle;
        private GUIStyle m_sectionTitleStyle;
        private GUIStyle m_messageStyle;
        private GUIStyle m_detailsStyle;
        private bool m_hasCompleted;

        internal static void ShowPopup(
            ProjectAssertRecord failure,
            Action<ProjectAssertPopupResult> completed)
        {
            ProjectAssertSettings settings = ProjectAssertSettings.instance;
            float popupWidth = Mathf.Clamp(settings.PopupWidth, k_MinimumWidth, k_MaximumWidth);
            float popupHeight = Mathf.Clamp(settings.PopupHeight, k_MinimumHeight, k_MaximumHeight);
            Rect mainWindowPosition = EditorGUIUtility.GetMainWindowPosition();

            ProjectAssertPopupWindow window = CreateInstance<ProjectAssertPopupWindow>();
            window.titleContent = new GUIContent("Project Assert");
            window.minSize = new Vector2(k_MinimumWidth, k_MinimumHeight);
            window.maxSize = new Vector2(k_MaximumWidth, k_MaximumHeight);
            window.position = new Rect(
                mainWindowPosition.x + (mainWindowPosition.width - popupWidth) * 0.5f,
                mainWindowPosition.y + (mainWindowPosition.height - popupHeight) * 0.5f,
                popupWidth,
                popupHeight);
            window.m_failure = failure;
            window.m_completed = completed;
            window.ShowModalUtility();
        }

        private void OnGUI()
        {
            EnsureStyles();
            m_detailsPanelHeight = ClampDetailsPanelHeight(m_detailsPanelHeight);
            m_callStackPanelHeight = ClampCallStackPanelHeight(m_callStackPanelHeight);

            Color severityColor = GetSeverityColor(m_failure.Severity);
            EditorGUI.DrawRect(
                new Rect(0f, 0f, position.width, k_TopStripeHeight),
                severityColor);

            Rect contentRect = new Rect(
                k_ContentPadding,
                k_TopStripeHeight + k_ContentPadding,
                position.width - k_ContentPadding * 2f,
                position.height - k_TopStripeHeight - k_ContentPadding * 2f);

            GUILayout.BeginArea(contentRect);
            DrawHeader(severityColor);
            EditorGUILayout.Space(12f);
            DrawMessage();
            EditorGUILayout.Space(10f);
            DrawDetails();
            DrawDebugPanelSplitter();
            DrawCallStackDetails();
            DrawActionPanelSplitter();
            EditorGUILayout.Space(10f);
            DrawUtilityButtons();
            EditorGUILayout.Space(14f);
            DrawDecisionButtons();
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            // 창의 X 버튼도 안전한 기본 선택인 "한 번 무시"로 처리한다.
            Complete(ProjectAssertPopupResult.IgnoreOnce, false);
        }

        private void EnsureStyles()
        {
            if (m_titleStyle != null)
            {
                m_messageStyle.fontSize = ProjectAssertSettings.instance.MessageFontSize;
                return;
            }

            // NOTE: GUI.skin 기반 스타일은 OnGUI가 시작된 뒤에만 안전하게 만들 수 있다.
            m_titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleLeft
            };
            m_severityStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleRight
            };
            m_sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11
            };
            m_messageStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = ProjectAssertSettings.instance.MessageFontSize,
                wordWrap = true,
                richText = false,
                padding = new RectOffset(12, 12, 12, 12)
            };
            m_detailsStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                padding = new RectOffset(8, 8, 8, 8)
            };
        }

        private void DrawHeader(Color severityColor)
        {
            m_titleStyle.normal.textColor = severityColor;
            m_severityStyle.normal.textColor = severityColor;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("ASSERTION FAILED", m_titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(m_failure.Severity.ToString().ToUpperInvariant(), m_severityStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMessage()
        {
            GUILayout.Label("MESSAGE", m_sectionTitleStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(m_failure.Message, m_messageStyle, GUILayout.MinHeight(86f));
            EditorGUILayout.EndVertical();
        }

        private void DrawDetails()
        {
            GUILayout.Label("DETAILS", m_sectionTitleStyle);

            string detailsText = BuildDetailsText();
            float contentWidth = Mathf.Max(100f, position.width - 64f);
            float contentHeight = Mathf.Max(
                m_detailsPanelHeight - 8f,
                m_detailsStyle.CalcHeight(
                    new GUIContent(detailsText),
                    contentWidth));

            m_detailsScrollPosition = EditorGUILayout.BeginScrollView(
                m_detailsScrollPosition,
                EditorStyles.helpBox,
                GUILayout.Height(m_detailsPanelHeight));
            EditorGUILayout.SelectableLabel(
                detailsText,
                m_detailsStyle,
                GUILayout.Height(contentHeight));
            EditorGUILayout.EndScrollView();
        }

        private void DrawDebugPanelSplitter()
        {
            Rect splitterRect = GUILayoutUtility.GetRect(
                0f,
                k_DebugPanelSplitterHeight,
                GUILayout.ExpandWidth(true));
            int controlId = GUIUtility.GetControlID(
                DebugPanelSplitterControlHash,
                FocusType.Passive,
                splitterRect);
            Event currentEvent = Event.current;

            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);

            if (currentEvent.type == EventType.Repaint)
            {
                bool isActive = GUIUtility.hotControl == controlId;
                bool isHovered = splitterRect.Contains(currentEvent.mousePosition);
                Color splitterColor = isActive || isHovered
                    ? new Color(0.30f, 0.60f, 0.90f, 0.85f)
                    : new Color(0.45f, 0.45f, 0.45f, 0.55f);
                Rect lineRect = new Rect(
                    splitterRect.x,
                    splitterRect.center.y - 1f,
                    splitterRect.width,
                    2f);
                EditorGUI.DrawRect(lineRect, splitterColor);
            }

            switch (currentEvent.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (currentEvent.button != 0 || !splitterRect.Contains(currentEvent.mousePosition))
                    {
                        break;
                    }

                    if (currentEvent.clickCount >= 2)
                    {
                        ResizeDetailsAgainstCallStack(
                            k_DefaultDetailsPanelHeight - m_detailsPanelHeight);
                    }
                    else
                    {
                        GUIUtility.hotControl = controlId;
                    }

                    currentEvent.Use();
                    Repaint();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlId)
                    {
                        break;
                    }

                    ResizeDetailsAgainstCallStack(currentEvent.delta.y);
                    currentEvent.Use();
                    Repaint();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != controlId)
                    {
                        break;
                    }

                    GUIUtility.hotControl = 0;
                    currentEvent.Use();
                    Repaint();
                    break;
            }
        }

        private void DrawCallStackDetails()
        {
            GUILayout.Label("CALL STACK", m_sectionTitleStyle);

            string callStackText = BuildCallStackText();
            float contentWidth = Mathf.Max(100f, position.width - 64f);
            float contentHeight = Mathf.Max(
                k_MinimumCallStackPanelHeight - 8f,
                m_detailsStyle.CalcHeight(
                    new GUIContent(callStackText),
                    contentWidth));

            m_callStackScrollPosition = EditorGUILayout.BeginScrollView(
                m_callStackScrollPosition,
                EditorStyles.helpBox,
                GUILayout.Height(m_callStackPanelHeight));
            EditorGUILayout.SelectableLabel(
                callStackText,
                m_detailsStyle,
                GUILayout.Height(contentHeight));
            EditorGUILayout.EndScrollView();
        }

        private void DrawActionPanelSplitter()
        {
            Rect splitterRect = GUILayoutUtility.GetRect(
                0f,
                k_ActionPanelSplitterHeight,
                GUILayout.ExpandWidth(true));
            int controlId = GUIUtility.GetControlID(
                ActionPanelSplitterControlHash,
                FocusType.Passive,
                splitterRect);
            Event currentEvent = Event.current;

            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);

            if (currentEvent.type == EventType.Repaint)
            {
                bool isActive = GUIUtility.hotControl == controlId;
                bool isHovered = splitterRect.Contains(currentEvent.mousePosition);
                Color splitterColor = isActive || isHovered
                    ? new Color(0.30f, 0.60f, 0.90f, 0.85f)
                    : new Color(0.45f, 0.45f, 0.45f, 0.55f);
                Rect lineRect = new Rect(
                    splitterRect.x,
                    splitterRect.center.y - 1f,
                    splitterRect.width,
                    2f);
                EditorGUI.DrawRect(lineRect, splitterColor);
            }

            switch (currentEvent.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (currentEvent.button != 0 || !splitterRect.Contains(currentEvent.mousePosition))
                    {
                        break;
                    }

                    if (currentEvent.clickCount >= 2)
                    {
                        m_callStackPanelHeight = ClampCallStackPanelHeight(
                            k_DefaultCallStackPanelHeight);
                    }
                    else
                    {
                        GUIUtility.hotControl = controlId;
                    }

                    currentEvent.Use();
                    Repaint();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlId)
                    {
                        break;
                    }

                    m_callStackPanelHeight = ClampCallStackPanelHeight(
                        m_callStackPanelHeight + currentEvent.delta.y);
                    currentEvent.Use();
                    Repaint();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != controlId)
                    {
                        break;
                    }

                    GUIUtility.hotControl = 0;
                    currentEvent.Use();
                    Repaint();
                    break;
            }
        }

        private void ResizeDetailsAgainstCallStack(float requestedDelta)
        {
            float minimumDelta = k_MinimumDetailsPanelHeight - m_detailsPanelHeight;
            float maximumDelta = m_callStackPanelHeight - k_MinimumCallStackPanelHeight;
            float appliedDelta = Mathf.Clamp(
                requestedDelta,
                minimumDelta,
                maximumDelta);

            m_detailsPanelHeight += appliedDelta;
            m_callStackPanelHeight -= appliedDelta;
        }

        private float ClampDetailsPanelHeight(float panelHeight)
        {
            float maximumDetailsPanelHeight = Mathf.Max(
                k_MinimumDetailsPanelHeight,
                position.height - k_MinimumCallStackPanelHeight - k_FixedLayoutHeight);
            return Mathf.Clamp(
                panelHeight,
                k_MinimumDetailsPanelHeight,
                maximumDetailsPanelHeight);
        }

        private float ClampCallStackPanelHeight(float panelHeight)
        {
            float maximumCallStackPanelHeight = Mathf.Max(
                k_MinimumCallStackPanelHeight,
                position.height - m_detailsPanelHeight - k_FixedLayoutHeight);
            return Mathf.Clamp(
                panelHeight,
                k_MinimumCallStackPanelHeight,
                maximumCallStackPanelHeight);
        }

        private void DrawUtilityButtons()
        {
            EditorGUILayout.BeginHorizontal();

            bool canOpenSource = TryGetAssetPath(m_failure.FilePath, out string assetPath);
            using (new EditorGUI.DisabledScope(!canOpenSource))
            {
                if (GUILayout.Button("Open Source", GUILayout.Height(28f)))
                {
                    MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                    if (script != null)
                    {
                        AssetDatabase.OpenAsset(script, m_failure.LineNumber);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(m_failure.Context == null))
            {
                if (GUILayout.Button("Ping Context", GUILayout.Height(28f)))
                {
                    Selection.activeObject = m_failure.Context;
                    EditorGUIUtility.PingObject(m_failure.Context);
                }
            }

            if (GUILayout.Button("Copy Details", GUILayout.Height(28f)))
            {
                EditorGUIUtility.systemCopyBuffer =
                    $"{m_failure.Message}\n\n{BuildDetailsText()}\n\nCall Stack:\n{BuildCallStackText()}";
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDecisionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            Color originalBackgroundColor = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.72f, 0.72f, 0.72f);
            if (GUILayout.Button("Ignore Once", GUILayout.Height(k_ActionButtonHeight)))
            {
                Complete(ProjectAssertPopupResult.IgnoreOnce);
            }

            GUI.backgroundColor = new Color(1f, 0.72f, 0.25f);
            string ignoreSessionButtonLabel = EditorApplication.isPlaying
                ? "Ignore This Assert This Play"
                : "Ignore This Assert This Edit Session";
            if (GUILayout.Button(ignoreSessionButtonLabel, GUILayout.Height(k_ActionButtonHeight)))
            {
                Complete(ProjectAssertPopupResult.IgnoreForCurrentSession);
            }

            GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
            string stopButtonLabel = EditorApplication.isPlaying ? "Stop Play Mode" : "Close";
            if (GUILayout.Button(stopButtonLabel, GUILayout.Height(k_ActionButtonHeight)))
            {
                ProjectAssertPopupResult result = EditorApplication.isPlaying
                    ? ProjectAssertPopupResult.StopPlayMode
                    : ProjectAssertPopupResult.IgnoreOnce;
                Complete(result);
            }

            GUI.backgroundColor = originalBackgroundColor;
            EditorGUILayout.EndHorizontal();
        }

        private string BuildDetailsText()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Severity: ");
            builder.AppendLine(m_failure.Severity.ToString());
            builder.Append("Method: ");
            builder.AppendLine(m_failure.MemberName);
            builder.Append("Location: ");
            builder.Append(m_failure.FilePath);
            builder.Append(':');
            builder.AppendLine(m_failure.LineNumber.ToString());

            if (m_failure.Context != null)
            {
                builder.Append("Context: ");
                builder.AppendLine(m_failure.Context.name);
            }

            return builder.ToString();
        }

        private string BuildCallStackText()
        {
            return m_failure.StackTrace ?? string.Empty;
        }

        private static Color GetSeverityColor(ProjectAssertSeverity severity)
        {
            return severity == ProjectAssertSeverity.Critical
                ? new Color(1f, 0.25f, 0.22f)
                : new Color(1f, 0.67f, 0.18f);
        }

        private static bool TryGetAssetPath(string filePath, out string assetPath)
        {
            assetPath = string.Empty;
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            string normalizedPath = filePath.Replace('\\', '/');
            if (normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                assetPath = normalizedPath;
                return true;
            }

            int assetsIndex = normalizedPath.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex < 0)
            {
                return false;
            }

            assetPath = normalizedPath.Substring(assetsIndex + 1);
            return true;
        }

        private void Complete(
            ProjectAssertPopupResult result,
            bool closeWindow = true)
        {
            if (m_hasCompleted)
            {
                return;
            }

            m_hasCompleted = true;
            Action<ProjectAssertPopupResult> completed = m_completed;
            m_completed = null;

            if (closeWindow)
            {
                Close();
            }

            completed?.Invoke(result);
        }
    }
}
