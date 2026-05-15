#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolarExpanseFleetTracker.Patches
{
    [HarmonyPatch]
    internal static class CyclicalMissionOverviewPatch
    {
        private const string MarkerName = "modFleetTrackerCyclicalMissionCard";
        private const float CardHeight = 148f;

        private static readonly string[] ButtonFieldNames = { "edit", "delete", "pause", "play", "buttonDelete", "actionBtn" };
        private static readonly Dictionary<Type, RowAccess> AccessByType = new Dictionary<Type, RowAccess>();
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            string[] typeNames =
            {
                "MissionRowCyclicalNew",
                "Game.UI.Windows.Elements.MissionsElements.MissionRowCyclical"
            };

            foreach (string typeName in typeNames)
            {
                Type type = AccessTools.TypeByName(typeName);
                MethodInfo method = type == null ? null : type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(candidate => candidate.Name == "SetData"
                        && candidate.GetParameters().Length == 1
                        && candidate.GetParameters()[0].ParameterType.FullName == "Game.UI.Windows.Elements.PlanMissionElements.CycleMissionsData");

                if (method != null)
                {
                    yield return method;
                }
            }
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, object __0)
        {
            try
            {
                if (__instance is Component row)
                {
                    RebuildRow(row, __0 ?? GetRowData(row));
                }
            }
            catch (Exception ex)
            {
                FleetTrackerPatch.Log.LogWarning($"[FT] Cyclical mission row SetData patch failed: {ex}");
            }
        }

        internal static bool RebuildRow(Component row, object data)
        {
            if (row == null)
            {
                return false;
            }

            data = data ?? GetRowData(row) ?? FindAncestorRowData(row.transform);
            if (data == null)
            {
                data = new RowTextFallback(row);
            }

            RowAccess access = GetAccess(row.GetType());
            List<Button> nativeButtons = GetNativeButtons(row, access);
            TMP_FontAsset font = FindFont(row);

            RectTransform rowRect = row.transform as RectTransform;
            if (rowRect != null)
            {
                rowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, CardHeight);
                rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, CardHeight);
            }

            LayoutElement rowLayout = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            rowLayout.minHeight = CardHeight;
            rowLayout.preferredHeight = CardHeight;
            rowLayout.flexibleHeight = 0f;

            GameObject card = FindDirectChild(row.transform, MarkerName);
            if (card == null)
            {
                card = CreateCard(row.transform);
            }
            else
            {
                DetachNativeButtons(row.transform, nativeButtons);
                ClearChildren(card.transform);
            }

            BuildCard(card.transform, data, nativeButtons, font);
            HideNativeVisualChildren(row.transform, card.transform);

            RebuildParentLayouts(card.transform as RectTransform);
            if (rowRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowRect);
            }

            return true;
        }

        private static GameObject CreateCard(Transform parent)
        {
            GameObject card = new GameObject(MarkerName, typeof(RectTransform));
            card.transform.SetParent(parent, false);
            card.transform.SetAsLastSibling();

            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);

            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.035f, 0.047f, 0.065f, 0.94f);
            bg.raycastTarget = false;

            VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return card;
        }

        private static void BuildCard(Transform parent, object data, List<Button> nativeButtons, TMP_FontAsset font)
        {
            GameObject header = MakeRow(parent, "Header", 8f);
            header.GetComponent<LayoutElement>().preferredHeight = 30f;

            MakeIconBadge(header.transform, "⇄", font, new Color(0.14f, 0.42f, 0.72f, 0.95f), 28f);

            GameObject titleStack = new GameObject("TitleStack", typeof(RectTransform));
            titleStack.transform.SetParent(header.transform, false);
            LayoutElement titleLayout = titleStack.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;
            titleLayout.preferredHeight = 30f;
            VerticalLayoutGroup titleGroup = titleStack.AddComponent<VerticalLayoutGroup>();
            titleGroup.spacing = 0f;
            titleGroup.childControlWidth = true;
            titleGroup.childControlHeight = true;
            titleGroup.childForceExpandWidth = true;
            titleGroup.childForceExpandHeight = false;

            TextMeshProUGUI route = MakeText(titleStack.transform, "Route", font, 15f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            route.text = $"{FormatEndpoint(GetMember(data, "A"))} ↔ {FormatEndpoint(GetMember(data, "B"))}";
            route.color = new Color(0.94f, 0.97f, 1f, 1f);
            route.GetComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI subtitle = MakeText(titleStack.transform, "Subtitle", font, 10f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            subtitle.text = Shorten(CleanTypeName(Convert.ToString(GetMember(data, "TransferType") ?? "Cyclical transfer")), 42);
            subtitle.color = new Color(0.48f, 0.66f, 0.82f, 1f);

            TextMeshProUGUI status = MakePill(header.transform, font);
            status.text = FormatStatus(data);
            ApplyPillStyle(status, GetStatusColor(data));

            GameObject metrics = MakeRow(parent, "Metrics", 6f);
            metrics.GetComponent<LayoutElement>().preferredHeight = 34f;
            MakeMetricChip(metrics.transform, "SC", FormatCraftCompact(data), font, new Color(0.12f, 0.22f, 0.34f, 0.9f));
            MakeMetricChip(metrics.transform, "#", FormatRunCount(data), font, new Color(0.16f, 0.18f, 0.27f, 0.9f));
            MakeMetricChip(metrics.transform, "END", FormatEndCompact(data), font, new Color(0.18f, 0.16f, 0.24f, 0.9f));

            GameObject cargoRow = MakeRow(parent, "Cargo", 8f);
            cargoRow.GetComponent<LayoutElement>().preferredHeight = 40f;
            MakeCargoBox(cargoRow.transform, "A → B", FormatCargo(GetMember(data, "CargoStart"), GetMember(data, "cargoAllStart")), font);
            MakeCargoBox(cargoRow.transform, "B → A", FormatCargo(GetMember(data, "CargoEnd"), GetMember(data, "cargoAllEnd")), font);

            if (nativeButtons.Count > 0)
            {
                PlaceNativeActions(parent, nativeButtons);
            }
        }

        private static void MakeIconBadge(Transform parent, string icon, TMP_FontAsset font, Color color, float size)
        {
            GameObject badge = new GameObject("IconBadge", typeof(RectTransform));
            badge.transform.SetParent(parent, false);
            Image bg = badge.AddComponent<Image>();
            bg.color = color;
            bg.raycastTarget = false;
            LayoutElement layout = badge.AddComponent<LayoutElement>();
            layout.minWidth = size;
            layout.preferredWidth = size;
            layout.minHeight = size;
            layout.preferredHeight = size;

            TextMeshProUGUI text = MakeText(badge.transform, "Icon", font, 15f, FontStyles.Bold, TextAlignmentOptions.Center);
            text.text = icon;
            text.color = new Color(0.9f, 0.97f, 1f, 1f);
            RectTransform rect = text.transform as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            LayoutElement textLayout = text.GetComponent<LayoutElement>();
            textLayout.ignoreLayout = true;
        }

        private static void MakeMetricChip(Transform parent, string icon, string value, TMP_FontAsset font, Color bgColor)
        {
            GameObject chip = new GameObject("Metric" + icon, typeof(RectTransform));
            chip.transform.SetParent(parent, false);
            Image bg = chip.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = false;
            LayoutElement chipLayout = chip.AddComponent<LayoutElement>();
            chipLayout.flexibleWidth = 1f;
            chipLayout.preferredHeight = 34f;

            HorizontalLayoutGroup layout = chip.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI iconText = MakeText(chip.transform, "Icon", font, 10f, FontStyles.Bold, TextAlignmentOptions.Center);
            iconText.text = icon;
            iconText.color = new Color(0.52f, 0.78f, 1f, 1f);
            iconText.GetComponent<LayoutElement>().preferredWidth = 24f;

            TextMeshProUGUI text = MakeText(chip.transform, "Value", font, 11f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            text.text = value;
            text.color = new Color(0.88f, 0.93f, 0.98f, 1f);
            text.GetComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static void MakeCargoBox(Transform parent, string label, string value, TMP_FontAsset font)
        {
            GameObject box = new GameObject("Cargo" + label, typeof(RectTransform));
            box.transform.SetParent(parent, false);
            Image bg = box.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.105f, 0.135f, 0.92f);
            bg.raycastTarget = false;
            LayoutElement boxLayout = box.AddComponent<LayoutElement>();
            boxLayout.flexibleWidth = 1f;
            boxLayout.preferredHeight = 40f;

            VerticalLayoutGroup layout = box.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 4, 4);
            layout.spacing = 0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI title = MakeText(box.transform, "CargoTitle", font, 10f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            title.text = "▸ " + label;
            title.color = new Color(0.58f, 0.82f, 1f, 1f);

            TextMeshProUGUI text = MakeText(box.transform, "CargoValue", font, 11f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            text.text = value;
            text.color = new Color(0.86f, 0.9f, 0.92f, 1f);
        }

        private static void PlaceNativeActions(Transform parent, List<Button> nativeButtons)
        {
            GameObject actions = MakeRow(parent, "NativeActions", 5f);
            actions.GetComponent<LayoutElement>().preferredHeight = 24f;
            LayoutElement spacer = new GameObject("Spacer", typeof(RectTransform)).AddComponent<LayoutElement>();
            spacer.transform.SetParent(actions.transform, false);
            spacer.flexibleWidth = 1f;

            foreach (Button button in nativeButtons)
            {
                if (button == null)
                {
                    continue;
                }

                button.transform.SetParent(actions.transform, false);
                button.gameObject.SetActive(true);
                button.transform.SetAsLastSibling();
                RectTransform buttonRect = button.transform as RectTransform;
                if (buttonRect != null)
                {
                    buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
                    buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                    buttonRect.pivot = new Vector2(0.5f, 0.5f);
                    buttonRect.sizeDelta = new Vector2(26f, 22f);
                }

                LayoutElement le = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = false;
                le.minWidth = 26f;
                le.preferredWidth = 26f;
                le.minHeight = 22f;
                le.preferredHeight = 22f;
                le.flexibleWidth = 0f;
                le.flexibleHeight = 0f;
            }
        }

        private static GameObject MakeRow(Transform parent, string name, float spacing)
        {
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = name == "Header" ? 22f : 18f;
            return row;
        }

        private static TextMeshProUGUI MakeText(Transform parent, string name, TMP_FontAsset font, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = Mathf.Ceil(size + 4f);
            le.preferredHeight = Mathf.Ceil(size + 4f);
            return text;
        }

        private static TextMeshProUGUI MakePill(Transform parent, TMP_FontAsset font)
        {
            TextMeshProUGUI pill = MakeText(parent, "Status", font, 11f, FontStyles.Bold, TextAlignmentOptions.Center);
            LayoutElement layout = pill.GetComponent<LayoutElement>();
            layout.minWidth = 58f;
            layout.preferredWidth = 58f;
            layout.flexibleWidth = 0f;
            return pill;
        }

        private static void ApplyPillStyle(TextMeshProUGUI pill, Color color)
        {
            pill.color = color;
            Image bg = pill.gameObject.AddComponent<Image>();
            bg.color = new Color(color.r * 0.20f, color.g * 0.20f, color.b * 0.20f, 0.95f);
            bg.raycastTarget = false;
            pill.raycastTarget = false;
        }

        private static RowAccess GetAccess(Type rowType)
        {
            if (AccessByType.TryGetValue(rowType, out RowAccess access))
            {
                return access;
            }

            access = new RowAccess
            {
                ButtonFields = ButtonFieldNames
                    .Select(name => AccessTools.Field(rowType, name))
                    .Where(field => field != null)
                    .ToArray()
            };
            AccessByType[rowType] = access;
            return access;
        }

        private static List<Button> GetNativeButtons(Component row, RowAccess access)
        {
            List<Button> buttons = new List<Button>();
            foreach (FieldInfo field in access.ButtonFields)
            {
                Button button = field.GetValue(row) as Button;
                if (button != null && !buttons.Contains(button))
                {
                    buttons.Add(button);
                }
            }
            return buttons;
        }

        private static void DetachNativeButtons(Transform row, List<Button> nativeButtons)
        {
            foreach (Button button in nativeButtons)
            {
                if (button != null && button.transform.IsChildOf(row))
                {
                    button.transform.SetParent(row, false);
                }
            }
        }

        private static void HideNativeVisualChildren(Transform row, Transform card)
        {
            for (int i = 0; i < row.childCount; i++)
            {
                Transform child = row.GetChild(i);
                if (child != card)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static TMP_FontAsset FindFont(Component row)
        {
            TextMeshProUGUI existing = row.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
            return existing != null ? existing.font : null;
        }

        private static string FormatEndpoint(object endpoint)
        {
            if (endpoint == null)
            {
                return "?";
            }

            object value = GetMember(endpoint, "ObjectName") ?? GetMember(endpoint, "Name") ?? GetMember(endpoint, "objectName");
            string text = value == null ? endpoint.ToString() : value.ToString();
            return Shorten(CleanTypeName(text), 18);
        }

        private static string FormatCraft(object data)
        {
            int count = ToInt(GetMember(data, "CountSC"));
            object list = GetMember(data, "ListSC");
            List<string> names = Enumerate(list).Select(FormatSpacecraft).Where(s => !string.IsNullOrEmpty(s)).Take(3).ToList();
            string suffix = names.Count > 0 ? ": " + string.Join(", ", names.ToArray()) : string.Empty;
            int extra = count > names.Count ? count - names.Count : 0;
            if (extra > 0)
            {
                suffix += $" +{extra}";
            }
            return $"Craft {count}{suffix}";
        }

        private static string FormatCraftCompact(object data)
        {
            int count = ToInt(GetMember(data, "CountSC"));
            object list = GetMember(data, "ListSC");
            string first = Enumerate(list).Select(FormatSpacecraft).FirstOrDefault(s => !string.IsNullOrEmpty(s));
            if (!string.IsNullOrEmpty(first))
            {
                return count > 1 ? $"{count} · {first}" : first;
            }
            return count > 0 ? count.ToString() : "—";
        }

        private static string FormatRunCount(object data)
        {
            int missionCount = ToInt(GetMember(data, "CountMission"));
            return missionCount > 0 ? missionCount.ToString() : "—";
        }

        private static string FormatEndCompact(object data)
        {
            string ends = Convert.ToString(GetMember(data, "Ends") ?? string.Empty);
            if (string.IsNullOrEmpty(ends) || ends == "None")
            {
                return "∞";
            }

            object endsData = GetMember(data, "EndsData");
            string text = endsData == null ? CleanTypeName(ends) : CleanTypeName(endsData.ToString());
            return Shorten(text, 12);
        }

        private static string FormatSpacecraft(object spacecraft)
        {
            object name = Invoke(spacecraft, "GetSpacecraftName") ?? GetMember(spacecraft, "spacecraftName") ?? GetMember(spacecraft, "Name");
            return name == null ? null : Shorten(CleanTypeName(name.ToString()), 16);
        }

        private static string FormatCounts(object data)
        {
            int missionCount = ToInt(GetMember(data, "CountMission"));
            string transferType = Shorten(CleanTypeName(Convert.ToString(GetMember(data, "TransferType") ?? "Transfer")), 24);
            string ends = Convert.ToString(GetMember(data, "Ends") ?? string.Empty);
            object endsData = GetMember(data, "EndsData");
            string endText = string.IsNullOrEmpty(ends) || ends == "None" ? "No end set" : $"Ends {CleanTypeName(ends)}";
            if (endsData != null)
            {
                endText += $" {Shorten(CleanTypeName(endsData.ToString()), 18)}";
            }
            return $"Runs {missionCount} • {transferType} • {endText}";
        }

        private static string FormatStatus(object data)
        {
            bool paused = ToBool(GetMember(data, "Pause"));
            if (paused)
            {
                return "Paused";
            }

            string ends = Convert.ToString(GetMember(data, "Ends") ?? string.Empty);
            if (!string.IsNullOrEmpty(ends) && ends != "None")
            {
                return "Ending";
            }

            return "Active";
        }

        private static Color GetStatusColor(object data)
        {
            return ToBool(GetMember(data, "Pause"))
                ? new Color(1f, 0.78f, 0.36f, 1f)
                : new Color(0.43f, 1f, 0.62f, 1f);
        }

        private static string FormatCargo(object primary, object fallback)
        {
            string text = CargoToString(primary);
            if (string.IsNullOrWhiteSpace(text))
            {
                text = CargoToString(fallback);
            }
            return string.IsNullOrWhiteSpace(text) ? "—" : Shorten(text, 30);
        }

        private static string CargoToString(object cargo)
        {
            if (cargo == null)
            {
                return null;
            }

            if (cargo is string s)
            {
                return CleanTypeName(s);
            }

            List<object> items = Enumerate(cargo).ToList();
            if (items.Count > 0 && !(cargo is UnityEngine.Object))
            {
                return string.Join(", ", items.Select(item => CleanTypeName(Convert.ToString(item))).Where(item => !string.IsNullOrWhiteSpace(item)).Take(3).ToArray());
            }

            object name = GetMember(cargo, "CargoName") ?? GetMember(cargo, "Name") ?? GetMember(cargo, "ResourceName");
            object count = GetMember(cargo, "Count") ?? GetMember(cargo, "Quantity") ?? GetMember(cargo, "amount");
            if (name != null && count != null)
            {
                return $"{CleanTypeName(name.ToString())} ×{count}";
            }
            if (name != null)
            {
                return CleanTypeName(name.ToString());
            }

            string raw = cargo.ToString();
            return raw == cargo.GetType().FullName ? null : CleanTypeName(raw);
        }

        private static IEnumerable<object> Enumerate(object value)
        {
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                foreach (object item in enumerable)
                {
                    yield return item;
                }
            }
        }

        private static object GetMember(object instance, string name)
        {
            if (instance == null)
            {
                return null;
            }

            Type type = instance.GetType();
            PropertyInfo property = AccessTools.Property(type, name);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(instance, null);
            }

            FieldInfo field = AccessTools.Field(type, name);
            return field == null ? null : field.GetValue(instance);
        }

        private static object Invoke(object instance, string name)
        {
            if (instance == null)
            {
                return null;
            }

            MethodInfo method = AccessTools.Method(instance.GetType(), name, Type.EmptyTypes);
            return method == null ? null : method.Invoke(instance, null);
        }

        private static int ToInt(object value)
        {
            if (value == null)
            {
                return 0;
            }
            try { return Convert.ToInt32(value); }
            catch { return 0; }
        }

        private static bool ToBool(object value)
        {
            if (value == null)
            {
                return false;
            }
            try { return Convert.ToBoolean(value); }
            catch { return false; }
        }

        private static string CleanTypeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            int dot = value.LastIndexOf('.');
            if (dot >= 0 && dot < value.Length - 1 && value.Take(dot).All(c => char.IsLetterOrDigit(c) || c == '.'))
            {
                return value.Substring(dot + 1);
            }
            return value.Trim();
        }

        private static string Shorten(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
            {
                return value;
            }
            return value.Substring(0, Math.Max(1, max - 1)) + "…";
        }

        private static GameObject FindDirectChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        private static void RebuildParentLayouts(RectTransform start)
        {
            Transform current = start;
            int depth = 0;
            while (current != null && depth < 5)
            {
                RectTransform rect = current as RectTransform;
                if (rect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }
                current = current.parent;
                depth++;
            }
        }

        private struct RowAccess
        {
            internal FieldInfo[] ButtonFields;
        }

        internal static bool IsCyclicalMissionRow(Component component)
        {
            if (component == null)
            {
                return false;
            }

            string fullName = component.GetType().FullName;
            return fullName == "MissionRowCyclicalNew"
                || fullName == "Game.UI.Windows.Elements.MissionsElements.MissionRowCyclical"
                || fullName == "MissionRowNew";
        }

        internal static object GetRowData(Component row)
        {
            return GetMember(row, "cmd")
                ?? GetMember(row, "cycleMissionsData")
                ?? GetMember(row, "CycleMissionsData")
                ?? GetMember(row, "data")
                ?? GetMember(row, "Data");
        }

        internal static object FindAncestorRowData(Transform transform)
        {
            Transform current = transform;
            int depth = 0;
            while (current != null && depth < 5)
            {
                foreach (Component component in current.GetComponents<Component>())
                {
                    object data = GetRowData(component);
                    if (data != null)
                    {
                        return data;
                    }
                }

                current = current.parent;
                depth++;
            }

            return null;
        }

        private sealed class RowTextFallback
        {
            private readonly Component row;

            internal RowTextFallback(Component row)
            {
                this.row = row;
            }

            public object A => FindText("source", "from", "a", "start");
            public object B => FindText("destination", "target", "to", "b", "end");
            public object ListSC => null;
            public object CountSC => 0;
            public object CountMission => FindText("count", "mission");
            public object Pause => false;
            public object TransferType => FindText("transfer", "type");
            public object CargoStart => FindText("resource", "cargo", "start");
            public object CargoEnd => FindText("resource", "cargo", "end");
            public object Ends => null;
            public object EndsData => null;
            public object cargoAllStart => null;
            public object cargoAllEnd => null;

            private string FindText(params string[] tokens)
            {
                TextMeshProUGUI[] labels = row.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
                foreach (TextMeshProUGUI label in labels)
                {
                    string name = label.name ?? string.Empty;
                    if (tokens.Any(token => name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                        && !string.IsNullOrWhiteSpace(label.text))
                    {
                        return label.text;
                    }
                }

                foreach (TextMeshProUGUI label in labels)
                {
                    if (!string.IsNullOrWhiteSpace(label.text))
                    {
                        return label.text;
                    }
                }

                return "?";
            }
        }
    }

    [HarmonyPatch]
    internal static class MissionRowNewCyclicalOverviewPatch
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            Type type = AccessTools.TypeByName("MissionRowNew");
            if (type == null)
            {
                yield break;
            }

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(candidate => candidate.Name == "SetMissionInfo"))
            {
                yield return method;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            try
            {
                if (__instance is Component row)
                {
                    CyclicalMissionOverviewPatch.RebuildRow(row, CyclicalMissionOverviewPatch.GetRowData(row) ?? CyclicalMissionOverviewPatch.FindAncestorRowData(row.transform));
                }
            }
            catch (Exception ex)
            {
                FleetTrackerPatch.Log.LogWarning($"[FT] MissionRowNew cyclical overview patch failed: {ex}");
            }
        }
    }

    [HarmonyPatch]
    internal static class CycleMissionAllListOverviewPatch
    {
        private static bool loggedLifecyclePatch;

        internal static IEnumerable<MethodBase> TargetMethods()
        {
            Type type = AccessTools.TypeByName("Game.UI.Windows.Elements.MissionsElements.CycleMissionAllList");
            if (type == null)
            {
                yield break;
            }

            MethodInfo onEnable = AccessTools.Method(type, "OnEnable");
            if (onEnable != null)
            {
                yield return onEnable;
            }

            MethodInfo show = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(candidate => candidate.Name == "ShowCyclicalMission");
            if (show != null)
            {
                yield return show;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            try
            {
                if (!(__instance is Component list))
                {
                    return;
                }

                int rebuilt = 0;
                foreach (MonoBehaviour component in list.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
                {
                    if (CyclicalMissionOverviewPatch.IsCyclicalMissionRow(component)
                        && CyclicalMissionOverviewPatch.RebuildRow(component, CyclicalMissionOverviewPatch.GetRowData(component)))
                    {
                        rebuilt++;
                    }
                }

                if (rebuilt == 0)
                {
                    foreach (MonoBehaviour component in list.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
                    {
                        object data = CyclicalMissionOverviewPatch.GetRowData(component) ?? CyclicalMissionOverviewPatch.FindAncestorRowData(component.transform);
                        if (data != null && CyclicalMissionOverviewPatch.RebuildRow(component, data))
                        {
                            rebuilt++;
                            break;
                        }
                    }
                }

                RectTransform rect = list.transform as RectTransform;
                if (rect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }

                if (!loggedLifecyclePatch)
                {
                    loggedLifecyclePatch = true;
                    FleetTrackerPatch.Log.LogInfo($"[FT] CycleMissionAllList overview patch active; rebuilt {rebuilt} existing rows.");
                }
            }
            catch (Exception ex)
            {
                FleetTrackerPatch.Log.LogWarning($"[FT] CycleMissionAllList overview patch failed: {ex}");
            }
        }
    }
}
