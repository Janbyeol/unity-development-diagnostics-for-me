using System.Collections.Generic;
using DevelopmentDiagnostics.Assertions;
using UnityEditor;
using UnityEngine;

namespace DevelopmentDiagnostics.Editor.Assertions
{
    internal static class ProjectAssertSettingsProvider
    {
        private const string k_SettingsPath = "Project/Development Diagnostics/Assertions";

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            SettingsProvider provider = new SettingsProvider(k_SettingsPath, SettingsScope.Project)
            {
                label = "Assertions",
                guiHandler = DrawSettings,
                keywords = new HashSet<string>(new string[]
                {
                    "Development Diagnostics",
                    "Assert",
                    "Assertions",
                    "Critical",
                    "Popup",
                    "Debug",
                    "Log",
                    "Console"
                })
            };

            return provider;
        }

        [MenuItem("Tools/Development Diagnostics/Assertion Settings")]
        private static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(k_SettingsPath);
        }

        private static void DrawSettings(string searchContext)
        {
            ProjectAssertSettings settings = ProjectAssertSettings.instance;
            SerializedObject serializedSettings = new SerializedObject(settings);

            serializedSettings.Update();

            EditorGUILayout.HelpBox(
                "ProjectAssert 팝업과 Project 디버그 API의 Unity Console 전달을 설정합니다. 기존 Debug.Assert는 자동 감지하지 않습니다.",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                serializedSettings.FindProperty("m_popupMode"),
                new GUIContent("Popup Mode", "Off / Critical Only / All 중 팝업 표시 단계를 선택합니다."));
            EditorGUILayout.PropertyField(
                serializedSettings.FindProperty("m_pausePlayModeBeforePopup"),
                new GUIContent("Pause Before Popup", "팝업을 열기 전에 Play Mode를 일시 정지합니다."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unity Console Forwarding", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedSettings.FindProperty("m_assertLogToConsole"),
                new GUIContent(
                    "Assert To Unity Console",
                    "전용 팝업 및 Debug Window와 별개로 ProjectAssert를 Unity Console에도 기록합니다."));
            EditorGUILayout.PropertyField(
                serializedSettings.FindProperty("m_projectLogToConsole"),
                new GUIContent(
                    "Project Log To Unity Console",
                    "통합 Debug Window와 별개로 ProjectLog를 Unity Console에도 기록합니다."));
            EditorGUILayout.HelpBox(
                "두 옵션은 기본적으로 꺼져 있습니다. 꺼져 있어도 Project Debug Window에는 정상적으로 기록됩니다.",
                MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            EditorGUILayout.Slider(
                serializedSettings.FindProperty("m_popupWidth"),
                640f,
                1400f,
                new GUIContent("Popup Width"));
            EditorGUILayout.Slider(
                serializedSettings.FindProperty("m_popupHeight"),
                400f,
                900f,
                new GUIContent("Popup Height"));
            EditorGUILayout.IntSlider(
                serializedSettings.FindProperty("m_messageFontSize"),
                14,
                28,
                new GUIContent("Message Font Size"));

            if (serializedSettings.ApplyModifiedProperties())
            {
                settings.SaveSettings();
                ProjectAssertEditorController.ApplySettings();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Session", EditorStyles.boldLabel);

            if (GUILayout.Button("Reset Session-Ignored Assertions"))
            {
                ProjectAssertEditorController.ResetCurrentPlayState();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Test", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Raise Normal Assert"))
            {
                ProjectAssert.Normal(
                    false,
                    "Project Assert의 Normal 단계 테스트입니다.... 신기하죠?");
            }

            if (GUILayout.Button("Raise Critical Assert"))
            {
                ProjectAssert.Critical(
                    false,
                    "Project Assert의 Critical 단계 테스트입니다... 신기하죠?");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "일반 릴리스 빌드에서는 UNITY_ASSERTIONS가 정의되지 않아 ProjectAssert 호출과 인자 평가가 제거됩니다.",
                MessageType.None);
        }
    }
}
