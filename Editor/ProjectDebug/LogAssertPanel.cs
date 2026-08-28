using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace DevelopmentDiagnostics.Editor.Debug
{
    internal sealed class LogAssertPanel : IDebugPanel
    {
        private enum EnvironmentFilter
        {
            All,
            PlaySession,
            Editor
        }

        private const string k_AllTagsLabel = "All Tags";
        private const float k_RowHeight = 34f;
        private const float k_SeverityStripeWidth = 4f;
        private const float k_DefaultDetailHeight = 230f;
        private const float k_MinimumDetailHeight = 140f;
        private const float k_MinimumListHeight = 160f;
        private const float k_FixedLayoutHeight = 96f;
        private const float k_DetailSplitterHeight = 7f;

        private static readonly int DetailSplitterControlHash =
            "ProjectDebugDetailSplitter".GetHashCode();

        private readonly List<ProjectDebugEntry> m_filteredEntries = new List<ProjectDebugEntry>();

        private Vector2 m_listScrollPosition;
        private Vector2 m_messageScrollPosition;
        private Vector2 m_metadataScrollPosition;
        private ProjectDebugEntry m_selectedEntry;
        private string m_searchText = string.Empty;
        private string m_selectedTag = k_AllTagsLabel;
        private EnvironmentFilter m_environmentFilter = EnvironmentFilter.All;
        private bool m_showLogs = true;
        private bool m_showAsserts = true;
        private bool m_showInfo = true;
        private bool m_showWarning = true;
        private bool m_showError = true;
        private bool m_showNormalAssert = true;
        private bool m_showCriticalAssert = true;
        private bool m_autoScroll = true;
        private bool m_scrollToBottom;
        private int m_logEntryCount;
        private int m_assertEntryCount;
        private int m_allEntryCount;
        private int m_playEntryCount;
        private int m_editorEntryCount;
        [FormerlySerializedAs("_detailHeight"),SerializeField] private float m_detailHeight = k_DefaultDetailHeight;
        private GUIStyle m_rowTextStyle;
        private GUIStyle m_rowMetadataStyle;
        private GUIStyle m_detailMessageStyle;
        private Action m_requestRepaint;
        private Rect m_availableRect;

        /// <inheritdoc />
        public string Id => "log-assert";

        /// <inheritdoc />
        public string DisplayName => "Logs / Asserts";

        /// <inheritdoc />
        public int Order => 0;

        /// <inheritdoc />
        public void OnEnable(Action requestRepaint)
        {
            m_requestRepaint = requestRepaint;
            ProjectDebugCollector.Changed += HandleEntriesChanged;
            m_scrollToBottom = true;

            if (m_detailHeight < k_MinimumDetailHeight)
            {
                m_detailHeight = k_DefaultDetailHeight;
            }
        }

        /// <inheritdoc />
        public void OnDisable()
        {
            ProjectDebugCollector.Changed -= HandleEntriesChanged;
            m_requestRepaint = null;
        }

        /// <inheritdoc />
        public void OnGUI(Rect availableRect)
        {
            m_availableRect = availableRect;
            EnsureStyles();
            m_detailHeight = ClampDetailHeight(m_detailHeight);
            RebuildFilteredEntries();
            ValidateSelection();

            DrawMainToolbar();
            DrawFilterToolbar();
            DrawColumnHeader();
            DrawEntryList();
            DrawDetailSplitter();
            DrawSelectedEntry();
        }

        private void EnsureStyles()
        {
            if (m_rowTextStyle != null)
            {
                return;
            }

            m_rowTextStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(6, 4, 0, 0)
            };
            m_rowMetadataStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(4, 4, 0, 0)
            };
            m_detailMessageStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 15,
                wordWrap = true,
                padding = new RectOffset(8, 8, 8, 8)
            };

            Color primaryTextColor = EditorGUIUtility.isProSkin
                ? new Color(0.88f, 0.88f, 0.88f)
                : new Color(0.12f, 0.12f, 0.12f);

            Color metadataTextColor = EditorGUIUtility.isProSkin
                ? new Color(0.72f, 0.72f, 0.72f)
                : new Color(0.28f, 0.28f, 0.28f);

            m_rowTextStyle.normal.textColor = primaryTextColor;
            m_rowMetadataStyle.normal.textColor = metadataTextColor;
            m_detailMessageStyle.normal.textColor = primaryTextColor;
        }

        private void DrawMainToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUIContent clearContent = new GUIContent(
                "Clear",
                m_environmentFilter == EnvironmentFilter.All
                    ? "모든 Project Debug 기록을 삭제합니다."
                    : "현재 환경 탭의 Project Debug 기록을 삭제합니다.");
            if (GUILayout.Button(clearContent, EditorStyles.toolbarButton, GUILayout.Width(52f)))
            {
                m_selectedEntry = null;
                ClearSelectedEnvironment();
            }

            int selectedEnvironmentIndex = GUILayout.Toolbar(
                (int)m_environmentFilter,
                new[]
                {
                    $"All ({m_allEntryCount})",
                    $"Play ({m_playEntryCount})",
                    $"Editor ({m_editorEntryCount})"
                },
                EditorStyles.toolbarButton,
                GUILayout.Width(240f));
            if (selectedEnvironmentIndex != (int)m_environmentFilter)
            {
                SelectEnvironmentFilter((EnvironmentFilter)selectedEnvironmentIndex);
            }

            GUILayout.Space(6f);
            m_showLogs = GUILayout.Toggle(
                m_showLogs,
                $"Logs ({m_logEntryCount})",
                EditorStyles.toolbarButton,
                GUILayout.Width(88f));
            m_showAsserts = GUILayout.Toggle(
                m_showAsserts,
                $"Asserts ({m_assertEntryCount})",
                EditorStyles.toolbarButton,
                GUILayout.Width(96f));

            GUILayout.FlexibleSpace();

            m_searchText = GUILayout.TextField(
                m_searchText,
                EditorStyles.toolbarSearchField,
                GUILayout.MinWidth(160f),
                GUILayout.MaxWidth(320f));

            if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(24f)))
            {
                m_searchText = string.Empty;
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilterToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            m_showInfo = DrawSeverityToggle(m_showInfo, "Info", ProjectDebugSeverity.Info);
            m_showWarning = DrawSeverityToggle(m_showWarning, "Warning", ProjectDebugSeverity.Warning);
            m_showError = DrawSeverityToggle(m_showError, "Error", ProjectDebugSeverity.Error);
            m_showNormalAssert = DrawSeverityToggle(
                m_showNormalAssert,
                "Normal Assert",
                ProjectDebugSeverity.NormalAssert);
            m_showCriticalAssert = DrawSeverityToggle(
                m_showCriticalAssert,
                "Critical Assert",
                ProjectDebugSeverity.CriticalAssert);

            GUILayout.Space(8f);

            if (GUILayout.Button(m_selectedTag, EditorStyles.toolbarDropDown, GUILayout.Width(150f)))
            {
                ShowTagMenu();
            }

            GUILayout.FlexibleSpace();
            m_autoScroll = GUILayout.Toggle(
                m_autoScroll,
                "Auto Scroll",
                EditorStyles.toolbarButton,
                GUILayout.Width(82f));

            EditorGUILayout.EndHorizontal();
        }

        private bool DrawSeverityToggle(
            bool currentValue,
            string label,
            ProjectDebugSeverity severity)
        {
            Color originalColor = GUI.contentColor;
            GUI.contentColor = GetSeverityColor(severity);
            bool newValue = GUILayout.Toggle(
                currentValue,
                label,
                EditorStyles.toolbarButton,
                GUILayout.MinWidth(58f));
            GUI.contentColor = originalColor;
            return newValue;
        }

        private void DrawColumnHeader()
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, 22f);
            EditorGUI.DrawRect(headerRect, EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.16f, 0.16f)
                : new Color(0.78f, 0.78f, 0.78f));

            float x = headerRect.x + k_SeverityStripeWidth + 4f;
            GUI.Label(new Rect(x, headerRect.y, 88f, headerRect.height), "TIME", EditorStyles.miniBoldLabel);
            x += 88f;
            GUI.Label(new Rect(x, headerRect.y, 110f, headerRect.height), "LEVEL", EditorStyles.miniBoldLabel);
            x += 110f;
            GUI.Label(new Rect(x, headerRect.y, 130f, headerRect.height), "TAG", EditorStyles.miniBoldLabel);
            x += 130f;
            GUI.Label(
                new Rect(x, headerRect.y, headerRect.xMax - x, headerRect.height),
                "MESSAGE",
                EditorStyles.miniBoldLabel);
        }

        private void DrawEntryList()
        {
            Rect listRect = GUILayoutUtility.GetRect(
                100f,
                10000f,
                160f,
                10000f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            GUI.Box(listRect, GUIContent.none, EditorStyles.helpBox);

            float contentHeight = Mathf.Max(listRect.height, m_filteredEntries.Count * k_RowHeight);
            Rect contentRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, listRect.width - 16f),
                contentHeight);

            if (m_scrollToBottom && m_autoScroll)
            {
                m_listScrollPosition.y = Mathf.Max(0f, contentHeight - listRect.height);
                m_scrollToBottom = false;
            }

            m_listScrollPosition = GUI.BeginScrollView(
                listRect,
                m_listScrollPosition,
                contentRect);

            int firstVisibleIndex = Mathf.Max(0, Mathf.FloorToInt(m_listScrollPosition.y / k_RowHeight));
            int visibleRowCount = Mathf.CeilToInt(listRect.height / k_RowHeight) + 1;
            int lastVisibleIndex = Mathf.Min(
                m_filteredEntries.Count,
                firstVisibleIndex + visibleRowCount);

            for (int index = firstVisibleIndex; index < lastVisibleIndex; index++)
            {
                Rect rowRect = new Rect(
                    0f,
                    index * k_RowHeight,
                    contentRect.width,
                    k_RowHeight);
                DrawEntryRow(m_filteredEntries[index], rowRect, index);
            }

            GUI.EndScrollView();

            if (m_filteredEntries.Count == 0)
            {
                GUI.Label(listRect, "표시할 Project Log 또는 Assert가 없습니다.", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DrawEntryRow(
            ProjectDebugEntry entry,
            Rect rowRect,
            int index)
        {
            Color backgroundColor = index % 2 == 0
                ? GetRowBackgroundColor(0.03f)
                : GetRowBackgroundColor(0.07f);

            if (ReferenceEquals(entry, m_selectedEntry))
            {
                backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.20f, 0.38f, 0.58f)
                    : new Color(0.42f, 0.66f, 0.88f);
            }

            EditorGUI.DrawRect(rowRect, backgroundColor);
            EditorGUI.DrawRect(
                new Rect(rowRect.x, rowRect.y, k_SeverityStripeWidth, rowRect.height),
                GetSeverityColor(entry.Severity));

            float x = rowRect.x + k_SeverityStripeWidth + 4f;
            GUI.Label(
                new Rect(x, rowRect.y, 88f, rowRect.height),
                entry.TimestampUtc.ToLocalTime().ToString("HH:mm:ss.fff"),
                m_rowMetadataStyle);
            x += 88f;
            GUI.Label(
                new Rect(x, rowRect.y, 110f, rowRect.height),
                GetSeverityLabel(entry.Severity),
                m_rowMetadataStyle);
            x += 110f;
            GUI.Label(
                new Rect(x, rowRect.y, 130f, rowRect.height),
                entry.Type == ProjectDebugEntryType.Log ? entry.Tag : "Assert",
                m_rowMetadataStyle);
            x += 130f;
            GUI.Label(
                new Rect(x, rowRect.y, Mathf.Max(0f, rowRect.xMax - x), rowRect.height),
                ToSingleLine(entry.Message),
                m_rowTextStyle);

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && rowRect.Contains(currentEvent.mousePosition))
            {
                m_selectedEntry = entry;
                m_messageScrollPosition = Vector2.zero;
                m_metadataScrollPosition = Vector2.zero;

                if (currentEvent.clickCount == 2)
                {
                    OpenSource(entry);
                }

                currentEvent.Use();
                RequestRepaint();
            }
        }

        private void DrawSelectedEntry()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(m_detailHeight));

            if (m_selectedEntry == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("항목을 선택하면 메시지와 호출 위치가 표시됩니다.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            Color originalColor = GUI.contentColor;
            GUI.contentColor = GetSeverityColor(m_selectedEntry.Severity);
            GUILayout.Label(GetSeverityLabel(m_selectedEntry.Severity), EditorStyles.boldLabel);
            GUI.contentColor = originalColor;
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                m_selectedEntry.Type == ProjectDebugEntryType.Log
                    ? $"Tag: {m_selectedEntry.Tag}"
                    : "Project Assert",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            float availablePanelHeight = Mathf.Max(76f, m_detailHeight - 62f);
            float messagePanelHeight = Mathf.Max(34f, availablePanelHeight * 0.4f);
            float metadataPanelHeight = Mathf.Max(
                42f,
                availablePanelHeight - messagePanelHeight);

            DrawMessagePanel(m_selectedEntry, messagePanelHeight);
            DrawMetadataPanel(m_selectedEntry, metadataPanelHeight);

            DrawDetailButtons(m_selectedEntry);
            EditorGUILayout.EndVertical();
        }

        private void DrawMessagePanel(
            ProjectDebugEntry entry,
            float panelHeight)
        {
            float contentWidth = Mathf.Max(100f, m_availableRect.width - 64f);
            float contentHeight = Mathf.Max(
                panelHeight - 8f,
                m_detailMessageStyle.CalcHeight(
                    new GUIContent(entry.Message),
                    contentWidth));

            m_messageScrollPosition = EditorGUILayout.BeginScrollView(
                m_messageScrollPosition,
                GUILayout.Height(panelHeight));
            EditorGUILayout.SelectableLabel(
                entry.Message,
                m_detailMessageStyle,
                GUILayout.Height(contentHeight));
            EditorGUILayout.EndScrollView();
        }

        private void DrawMetadataPanel(
            ProjectDebugEntry entry,
            float panelHeight)
        {
            string detailsText = BuildDetailsText(entry);
            float contentWidth = Mathf.Max(100f, m_availableRect.width - 64f);
            float contentHeight = Mathf.Max(
                panelHeight - 8f,
                EditorStyles.textArea.CalcHeight(
                    new GUIContent(detailsText),
                    contentWidth));

            m_metadataScrollPosition = EditorGUILayout.BeginScrollView(
                m_metadataScrollPosition,
                GUILayout.Height(panelHeight));
            EditorGUILayout.SelectableLabel(
                detailsText,
                EditorStyles.textArea,
                GUILayout.Height(contentHeight));
            EditorGUILayout.EndScrollView();
        }

        private void DrawDetailSplitter()
        {
            Rect splitterRect = GUILayoutUtility.GetRect(
                0f,
                k_DetailSplitterHeight,
                GUILayout.ExpandWidth(true));
            int controlId = GUIUtility.GetControlID(
                DetailSplitterControlHash,
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
                        m_detailHeight = ClampDetailHeight(k_DefaultDetailHeight);
                    }
                    else
                    {
                        GUIUtility.hotControl = controlId;
                    }

                    currentEvent.Use();
                    RequestRepaint();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlId)
                    {
                        break;
                    }

                    // 구분선을 위로 끌면 디테일 영역이 커지므로 Y 이동량을 반대로 적용한다.
                    m_detailHeight = ClampDetailHeight(m_detailHeight - currentEvent.delta.y);
                    currentEvent.Use();
                    RequestRepaint();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != controlId)
                    {
                        break;
                    }

                    GUIUtility.hotControl = 0;
                    currentEvent.Use();
                    RequestRepaint();
                    break;
            }
        }

        private void DrawDetailButtons(ProjectDebugEntry entry)
        {
            EditorGUILayout.BeginHorizontal();

            bool canOpenSource = TryGetAssetPath(entry.FilePath, out string assetPath);
            using (new EditorGUI.DisabledScope(!canOpenSource))
            {
                if (GUILayout.Button("Open Source", GUILayout.Height(28f)))
                {
                    OpenSource(entry, assetPath);
                }
            }

            using (new EditorGUI.DisabledScope(entry.Context == null))
            {
                if (GUILayout.Button("Ping Context", GUILayout.Height(28f)))
                {
                    Selection.activeObject = entry.Context;
                    EditorGUIUtility.PingObject(entry.Context);
                }
            }

            if (GUILayout.Button("Copy Details", GUILayout.Height(28f)))
            {
                EditorGUIUtility.systemCopyBuffer = $"{entry.Message}\n\n{BuildDetailsText(entry)}";
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RebuildFilteredEntries()
        {
            m_filteredEntries.Clear();
            m_logEntryCount = 0;
            m_assertEntryCount = 0;
            m_playEntryCount = 0;
            m_editorEntryCount = 0;
            IReadOnlyList<ProjectDebugEntry> entries = ProjectDebugCollector.Entries;
            m_allEntryCount = entries.Count;

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

            if (m_selectedTag != k_AllTagsLabel &&
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
            switch (m_environmentFilter)
            {
                case EnvironmentFilter.PlaySession:
                    return entry.Environment == ProjectDebugEnvironment.PlaySession;

                case EnvironmentFilter.Editor:
                    return entry.Environment == ProjectDebugEnvironment.Editor;

                default:
                    return true;
            }
        }

        private void SelectEnvironmentFilter(EnvironmentFilter environmentFilter)
        {
            m_environmentFilter = environmentFilter;
            m_selectedEntry = null;
            m_listScrollPosition = Vector2.zero;
            m_scrollToBottom = true;
            RequestRepaint();
        }

        private void ClearSelectedEnvironment()
        {
            switch (m_environmentFilter)
            {
                case EnvironmentFilter.PlaySession:
                    ProjectDebugCollector.Clear(ProjectDebugEnvironment.PlaySession);
                    break;

                case EnvironmentFilter.Editor:
                    ProjectDebugCollector.Clear(ProjectDebugEnvironment.Editor);
                    break;

                default:
                    ProjectDebugCollector.Clear();
                    break;
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

        private void ShowTagMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent(k_AllTagsLabel),
                m_selectedTag == k_AllTagsLabel,
                () => SelectTag(k_AllTagsLabel));

            foreach (string tag in ProjectDebugCollector.KnownTags)
            {
                if (!IsTagKnownInCurrentEnvironment(tag))
                {
                    continue;
                }

                string capturedTag = tag;
                menu.AddItem(
                    new GUIContent(capturedTag),
                    string.Equals(m_selectedTag, capturedTag, StringComparison.OrdinalIgnoreCase),
                    () => SelectTag(capturedTag));
            }

            menu.ShowAsContext();
        }

        private void SelectTag(string tag)
        {
            m_selectedTag = tag;
            RequestRepaint();
        }

        private void ValidateSelection()
        {
            if (m_selectedEntry != null && !ProjectDebugCollector.Entries.Contains(m_selectedEntry))
            {
                m_selectedEntry = null;
            }

            if (m_selectedTag != k_AllTagsLabel && !IsTagKnownInCurrentEnvironment(m_selectedTag))
            {
                m_selectedTag = k_AllTagsLabel;
            }
        }

        private bool IsTagKnownInCurrentEnvironment(string tag)
        {
            IReadOnlyList<ProjectDebugEntry> entries = ProjectDebugCollector.Entries;
            for (int index = 0; index < entries.Count; index++)
            {
                ProjectDebugEntry entry = entries[index];
                if (entry.Type == ProjectDebugEntryType.Log &&
                    PassesEnvironmentFilter(entry) &&
                    string.Equals(entry.Tag, tag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleEntriesChanged()
        {
            m_scrollToBottom = true;
            RequestRepaint();
        }

        private float ClampDetailHeight(float detailHeight)
        {
            float maximumDetailHeight = Mathf.Max(
                k_MinimumDetailHeight,
                m_availableRect.height - k_MinimumListHeight - k_FixedLayoutHeight);
            return Mathf.Clamp(
                detailHeight,
                k_MinimumDetailHeight,
                maximumDetailHeight);
        }

        private void RequestRepaint()
        {
            m_requestRepaint?.Invoke();
        }

        private static void OpenSource(ProjectDebugEntry entry)
        {
            if (TryGetAssetPath(entry.FilePath, out string assetPath))
            {
                OpenSource(entry, assetPath);
            }
        }

        private static void OpenSource(ProjectDebugEntry entry, string assetPath)
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (script != null)
            {
                AssetDatabase.OpenAsset(script, entry.LineNumber);
            }
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

        private static string BuildDetailsText(ProjectDebugEntry entry)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Type: ");
            builder.AppendLine(entry.Type.ToString());
            builder.Append("Environment: ");
            builder.AppendLine(GetEnvironmentLabel(entry.Environment));
            builder.Append("Level: ");
            builder.AppendLine(GetSeverityLabel(entry.Severity));

            if (entry.Type == ProjectDebugEntryType.Log)
            {
                builder.Append("Tag: ");
                builder.AppendLine(entry.Tag);
            }

            builder.Append("Time: ");
            builder.AppendLine(entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"));
            builder.Append("Method: ");
            builder.AppendLine(entry.MemberName);
            builder.Append("Location: ");
            builder.Append(entry.FilePath);
            builder.Append(':');
            builder.AppendLine(entry.LineNumber.ToString());

            if (entry.Context != null)
            {
                builder.Append("Context: ");
                builder.AppendLine(entry.Context.name);
            }

            if (entry.Type == ProjectDebugEntryType.Assert && !string.IsNullOrEmpty(entry.StackTrace))
            {
                builder.AppendLine();
                builder.AppendLine("Call Stack:");
                builder.Append(entry.StackTrace);
            }

            return builder.ToString();
        }

        private static string GetEnvironmentLabel(ProjectDebugEnvironment environment)
        {
            return environment == ProjectDebugEnvironment.PlaySession
                ? "Play"
                : "Editor";
        }

        private static string GetSeverityLabel(ProjectDebugSeverity severity)
        {
            switch (severity)
            {
                case ProjectDebugSeverity.NormalAssert:
                    return "NORMAL ASSERT";

                case ProjectDebugSeverity.CriticalAssert:
                    return "CRITICAL ASSERT";

                default:
                    return severity.ToString().ToUpperInvariant();
            }
        }

        private static Color GetSeverityColor(ProjectDebugSeverity severity)
        {
            switch (severity)
            {
                case ProjectDebugSeverity.Info:
                    return EditorGUIUtility.isProSkin
                        ? new Color(0.56f, 0.78f, 1f)
                        : new Color(0.10f, 0.38f, 0.68f);

                case ProjectDebugSeverity.Warning:
                case ProjectDebugSeverity.NormalAssert:
                    return new Color(1f, 0.67f, 0.18f);

                case ProjectDebugSeverity.Error:
                case ProjectDebugSeverity.CriticalAssert:
                    return new Color(1f, 0.30f, 0.28f);

                default:
                    return Color.white;
            }
        }

        private static Color GetRowBackgroundColor(float difference)
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.18f + difference, 0.18f + difference, 0.18f + difference)
                : new Color(0.92f - difference, 0.92f - difference, 0.92f - difference);
        }

        private static string ToSingleLine(string message)
        {
            return string.IsNullOrEmpty(message)
                ? string.Empty
                : message.Replace('\r', ' ').Replace('\n', ' ');
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            return !string.IsNullOrEmpty(source) &&
                source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
