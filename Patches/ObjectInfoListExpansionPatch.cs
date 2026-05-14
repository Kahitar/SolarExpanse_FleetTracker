#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.UI.Windows.Windows;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SolarExpanseFleetTracker.Patches
{
    [HarmonyPatch(typeof(ObjectInfoWindow), nameof(ObjectInfoWindow.RebuildLayout))]
    internal static class ObjectInfoListExpansionPatch
    {
        private const int ExpandedMaxRows = 1000;

        private static readonly Dictionary<Type, UiListAccess> AccessByType = new Dictionary<Type, UiListAccess>();
        private static bool loggedReflectionFailure;

        [HarmonyPostfix]
        private static void Postfix(ObjectInfoWindow __instance)
        {
            try
            {
                ExpandObjectInfoLists(__instance);
            }
            catch (Exception ex)
            {
                FleetTrackerPatch.Log.LogWarning($"[FT] ObjectInfo list expansion failed: {ex}");
            }
        }

        private static void ExpandObjectInfoLists(ObjectInfoWindow window)
        {
            if (window == null)
            {
                return;
            }

            foreach (MonoBehaviour component in window.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
            {
                if (!IsObjectInfoUiList(component))
                {
                    continue;
                }

                ExpandList(component);
            }

            RectTransform windowRect = window.transform as RectTransform;
            if (windowRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(windowRect);
            }
        }

        private static bool IsObjectInfoUiList(MonoBehaviour component)
        {
            if (component == null)
            {
                return false;
            }

            Type type = component.GetType();
            if (type.Namespace != "Game.UI.Windows.Elements.ObjectInfoElements")
            {
                return false;
            }

            return FindUiListBase(type) != null;
        }

        private static Type FindUiListBase(Type candidateType)
        {
            while (candidateType != null && candidateType != typeof(object))
            {
                if (candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == typeof(Game.UI.Windows.Elements.UIList<,>))
                {
                    return candidateType;
                }

                candidateType = candidateType.BaseType;
            }

            return null;
        }

        private static UiListAccess GetAccess(Type listType)
        {
            if (AccessByType.TryGetValue(listType, out UiListAccess cached))
            {
                return cached;
            }

            Type uiListBase = FindUiListBase(listType);
            UiListAccess access = uiListBase == null
                ? UiListAccess.Invalid
                : new UiListAccess(
                    AccessTools.Field(uiListBase, "maxRows"),
                    AccessTools.Field(uiListBase, "itemsInARow"),
                    AccessTools.Field(uiListBase, "rowHeight"),
                    AccessTools.Field(uiListBase, "scrollView"),
                    AccessTools.Property(uiListBase, "CreateRows"));

            AccessByType[listType] = access;
            return access;
        }

        private static void ExpandList(MonoBehaviour listComponent)
        {
            UiListAccess access = GetAccess(listComponent.GetType());
            if (!access.IsValid)
            {
                if (!loggedReflectionFailure)
                {
                    loggedReflectionFailure = true;
                    FleetTrackerPatch.Log.LogWarning("[FT] Could not locate UIList reflection members; object overview lists unchanged.");
                }
                return;
            }

            ScrollRect scrollView = access.ScrollView.GetValue(listComponent) as ScrollRect;
            if (scrollView == null)
            {
                return;
            }

            int activeRows = CountActiveRows(listComponent, access);
            int itemsInARow = Math.Max(1, (int)access.ItemsInARow.GetValue(listComponent));
            int visibleRows = Mathf.CeilToInt((float)activeRows / itemsInARow);
            float rowHeight = GetRowHeight(listComponent, access);

            access.MaxRows.SetValue(listComponent, ExpandedMaxRows);

            scrollView.vertical = false;
            scrollView.horizontal = false;
            if (scrollView.verticalScrollbar != null)
            {
                scrollView.verticalScrollbar.gameObject.SetActive(false);
            }

            RectTransform scrollRect = scrollView.transform as RectTransform;
            if (scrollRect != null && rowHeight > 0f)
            {
                scrollRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, visibleRows * rowHeight);
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect);
            }

            RectTransform listRect = listComponent.transform as RectTransform;
            if (listRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
            }
        }

        private static int CountActiveRows(MonoBehaviour listComponent, UiListAccess access)
        {
            IEnumerable rows = access.CreateRows.GetValue(listComponent, null) as IEnumerable;
            if (rows == null)
            {
                return 0;
            }

            int count = 0;
            foreach (object row in rows)
            {
                Component rowComponent = row as Component;
                if (rowComponent != null && rowComponent.gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        private static float GetRowHeight(MonoBehaviour listComponent, UiListAccess access)
        {
            float rowHeight = (float)access.RowHeight.GetValue(listComponent);
            if (rowHeight > 0f)
            {
                return rowHeight;
            }

            IEnumerable rows = access.CreateRows.GetValue(listComponent, null) as IEnumerable;
            if (rows == null)
            {
                return 0f;
            }

            foreach (object row in rows)
            {
                Component rowComponent = row as Component;
                RectTransform rowRect = rowComponent != null ? rowComponent.transform as RectTransform : null;
                if (rowRect != null && rowRect.rect.height > 0f)
                {
                    return rowRect.rect.height;
                }
            }

            return 0f;
        }

        private struct UiListAccess
        {
            internal static readonly UiListAccess Invalid = new UiListAccess(null, null, null, null, null);

            internal readonly FieldInfo MaxRows;
            internal readonly FieldInfo ItemsInARow;
            internal readonly FieldInfo RowHeight;
            internal readonly FieldInfo ScrollView;
            internal readonly PropertyInfo CreateRows;

            internal UiListAccess(FieldInfo maxRows, FieldInfo itemsInARow, FieldInfo rowHeight, FieldInfo scrollView, PropertyInfo createRows)
            {
                MaxRows = maxRows;
                ItemsInARow = itemsInARow;
                RowHeight = rowHeight;
                ScrollView = scrollView;
                CreateRows = createRows;
            }

            internal bool IsValid => MaxRows != null && ItemsInARow != null && RowHeight != null && ScrollView != null && CreateRows != null;
        }
    }
}
