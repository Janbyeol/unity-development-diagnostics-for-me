using System;
using System.Collections.Generic;
using DevelopmentDiagnostics.Assertions;
using UnityEditor;

namespace DevelopmentDiagnostics.Editor.Debug
{
    internal static class DebugPanelRegistry
    {
        internal static List<IDebugPanel> CreatePanels()
        {
            List<IDebugPanel> panels = new List<IDebugPanel>();
            HashSet<string> registeredIds = new HashSet<string>(StringComparer.Ordinal);
            TypeCache.TypeCollection panelTypes = TypeCache.GetTypesDerivedFrom<IDebugPanel>();

            for (int index = 0; index < panelTypes.Count; index++)
            {
                Type panelType = panelTypes[index];
                if (panelType.IsAbstract || panelType.IsInterface || panelType.ContainsGenericParameters)
                {
                    continue;
                }

                IDebugPanel panel = CreatePanel(panelType);
                if (panel == null)
                {
                    continue;
                }

                bool hasValidId = !string.IsNullOrWhiteSpace(panel.Id);
                ProjectAssert.Normal(
                    hasValidId,
                    $"Debug Panel '{panelType.FullName}'의 Id가 비어 있습니다. 해당 패널은 등록하지 않습니다.");
                if (!hasValidId)
                {
                    continue;
                }

                bool hasValidDisplayName = !string.IsNullOrWhiteSpace(panel.DisplayName);
                ProjectAssert.Normal(
                    hasValidDisplayName,
                    $"Debug Panel '{panelType.FullName}'의 DisplayName이 비어 있습니다. 해당 패널은 등록하지 않습니다.");
                if (!hasValidDisplayName)
                {
                    continue;
                }

                bool hasUniqueId = registeredIds.Add(panel.Id);
                ProjectAssert.Normal(
                    hasUniqueId,
                    $"Debug Panel Id '{panel.Id}'가 중복되었습니다. '{panelType.FullName}' 패널은 등록하지 않습니다.");
                if (!hasUniqueId)
                {
                    continue;
                }

                panels.Add(panel);
            }

            panels.Sort(ComparePanels);
            return panels;
        }

        private static IDebugPanel CreatePanel(Type panelType)
        {
            try
            {
                // NOTE: 패널은 Editor 전용 구현 세부사항이므로 internal 클래스도 자동 등록할 수 있게 허용한다.
                return Activator.CreateInstance(panelType, true) as IDebugPanel;
            }
            catch (Exception exception)
            {
                ProjectAssert.Normal(
                    $"Debug Panel '{panelType.FullName}'을 생성하지 못했습니다. " +
                    $"매개변수 없는 생성자가 필요합니다. 원인: {exception.GetBaseException().Message}");
                return null;
            }
        }

        private static int ComparePanels(IDebugPanel left, IDebugPanel right)
        {
            int orderComparison = left.Order.CompareTo(right.Order);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            int nameComparison = string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
            if (nameComparison != 0)
            {
                return nameComparison;
            }

            return string.Compare(left.Id, right.Id, StringComparison.Ordinal);
        }
    }
}
