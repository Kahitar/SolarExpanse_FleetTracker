#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using CustomUpdate;
using Game;
using Game.Info;
using Game.ObjectInfoDataScripts;
using Game.UI;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolarExpanseFleetTracker.UI
{
    internal static class FleetTrackerInjector
    {
        private static readonly FieldInfo FieldShowBtn =
            typeof(NotificationManager).GetField("showNotificationHistory",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldHistoryGO =
            typeof(NotificationManager).GetField("notificationHistory",
                BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void Inject(NotificationManager nm, ManualLogSource log)
        {
            try
            {
                Button showBtn = FieldShowBtn?.GetValue(nm) as Button;
                if (showBtn == null) { log.LogError("[FT] showNotificationHistory not found"); return; }

                GameObject historyGO = FieldHistoryGO?.GetValue(nm) as GameObject;
                if (historyGO == null) { log.LogError("[FT] notificationHistory GO not found"); return; }

                RectTransform showBtnRT = showBtn.GetComponent<RectTransform>();
                Canvas btnCanvas = showBtn.GetComponentInParent<Canvas>();
                if (btnCanvas == null) { log.LogError("[FT] could not find canvas"); return; }

                TMP_FontAsset fontAsset = FindFontAsset(nm, historyGO, log);

                GameObject panelGO = UnityEngine.Object.Instantiate(historyGO, btnCanvas.transform);
                panelGO.name = "modFleetTrackerPanel";
                panelGO.transform.SetAsLastSibling();
                RectTransform panelRT = panelGO.GetComponent<RectTransform>();
                panelRT.anchorMin = new Vector2(0.5f, 0.5f);
                panelRT.anchorMax = new Vector2(0.5f, 0.5f);
                panelRT.pivot = new Vector2(0f, 1f);
                panelRT.sizeDelta = new Vector2(860f, 360f);
                panelRT.anchoredPosition = new Vector2(-9999f, -9999f);

                LayoutElement panelLE = panelGO.AddComponent<LayoutElement>();
                panelLE.ignoreLayout = true;

                Image bgSource = null;
                foreach (Image img in panelGO.GetComponentsInChildren<Image>(includeInactive: true))
                    if (img.sprite != null) { bgSource = img; break; }

                Image panelBg = panelGO.GetComponent<Image>() ?? panelGO.AddComponent<Image>();
                if (bgSource != null)
                {
                    panelBg.sprite = bgSource.sprite;
                    panelBg.color = bgSource.color;
                    panelBg.type = bgSource.type;
                    panelBg.material = bgSource.material;
                }
                else
                {
                    panelBg.color = new Color(0.07f, 0.08f, 0.10f, 0.96f);
                }
                panelBg.raycastTarget = true;

                for (int i = panelGO.transform.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.Destroy(panelGO.transform.GetChild(i).gameObject);

                foreach (CanvasGroup cg in panelGO.GetComponents<CanvasGroup>())
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                foreach (ScrollRect sr in panelGO.GetComponents<ScrollRect>())
                    UnityEngine.Object.DestroyImmediate(sr);
                foreach (LayoutGroup lg in panelGO.GetComponents<LayoutGroup>())
                    UnityEngine.Object.DestroyImmediate(lg);
                ContentSizeFitter existingCSF = panelGO.GetComponent<ContentSizeFitter>();
                if (existingCSF != null) UnityEngine.Object.DestroyImmediate(existingCSF);

                GameObject viewportGO = new GameObject("ScrollViewport", typeof(RectTransform));
                viewportGO.transform.SetParent(panelGO.transform, false);
                RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
                viewportRT.anchorMin = Vector2.zero;
                viewportRT.anchorMax = Vector2.one;
                viewportRT.pivot = new Vector2(0.5f, 0.5f);
                viewportRT.offsetMin = new Vector2(8f, 8f);
                viewportRT.offsetMax = new Vector2(-22f, -8f);
                viewportGO.AddComponent<RectMask2D>();

                GameObject contentGO = MakeScrollContent("ScrollContent", viewportGO.transform, 1f, 4);
                RectTransform contentRT = contentGO.GetComponent<RectTransform>();

                GameObject scrollbarGO = new GameObject("Scrollbar", typeof(RectTransform));
                scrollbarGO.transform.SetParent(panelGO.transform, false);
                RectTransform scrollbarRT = scrollbarGO.GetComponent<RectTransform>();
                scrollbarRT.anchorMin = new Vector2(1f, 0f);
                scrollbarRT.anchorMax = new Vector2(1f, 1f);
                scrollbarRT.pivot = new Vector2(1f, 0.5f);
                scrollbarRT.sizeDelta = new Vector2(6f, -16f);
                scrollbarRT.anchoredPosition = new Vector2(-8f, 0f);
                Image scrollbarBg = scrollbarGO.AddComponent<Image>();
                Scrollbar scrollbar = scrollbarGO.AddComponent<Scrollbar>();
                scrollbar.direction = Scrollbar.Direction.BottomToTop;

                GameObject slidingAreaGO = new GameObject("SlidingArea", typeof(RectTransform));
                slidingAreaGO.transform.SetParent(scrollbarGO.transform, false);
                RectTransform slidingAreaRT = slidingAreaGO.GetComponent<RectTransform>();
                slidingAreaRT.anchorMin = Vector2.zero;
                slidingAreaRT.anchorMax = Vector2.one;
                slidingAreaRT.sizeDelta = Vector2.zero;
                slidingAreaRT.anchoredPosition = Vector2.zero;

                GameObject handleGO = new GameObject("Handle", typeof(RectTransform));
                handleGO.transform.SetParent(slidingAreaGO.transform, false);
                RectTransform handleRT = handleGO.GetComponent<RectTransform>();
                handleRT.anchorMin = Vector2.zero;
                handleRT.anchorMax = Vector2.one;
                handleRT.sizeDelta = Vector2.zero;
                Image handleImg = handleGO.AddComponent<Image>();

                scrollbar.handleRect = handleRT;
                scrollbar.targetGraphic = handleImg;
                CopyGameScrollbarStyle(scrollbarBg, handleImg);

                ScrollRect scrollRect = panelGO.AddComponent<ScrollRect>();
                scrollRect.viewport = viewportRT;
                scrollRect.content = contentRT;
                scrollRect.verticalScrollbar = scrollbar;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.scrollSensitivity = 30f;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;

                GameObject resizeHandleGO = new GameObject("ResizeHandle", typeof(RectTransform));
                resizeHandleGO.transform.SetParent(panelGO.transform, false);
                RectTransform resizeRT = resizeHandleGO.GetComponent<RectTransform>();
                resizeRT.anchorMin = new Vector2(0f, 0f);
                resizeRT.anchorMax = new Vector2(1f, 0f);
                resizeRT.pivot = new Vector2(0.5f, 1f);
                resizeRT.sizeDelta = new Vector2(0f, 10f);
                resizeRT.anchoredPosition = Vector2.zero;
                resizeHandleGO.AddComponent<Image>().color = Color.clear;
                resizeHandleGO.AddComponent<ResizeHandle>().PanelRT = panelRT;

                panelGO.SetActive(false);

                FleetTrackerPanel tracker = panelGO.AddComponent<FleetTrackerPanel>();
                tracker.ContentParent = contentGO.transform;
                tracker.FontAsset = fontAsset;
                tracker.TrackerLog = log;
                tracker.PanelRT = panelRT;
                tracker.ScrollRectRef = scrollRect;

                GameObject indicatorGO = new GameObject("modFleetTrackerButton", typeof(RectTransform));
                indicatorGO.transform.SetParent(btnCanvas.transform, false);
                indicatorGO.transform.SetAsLastSibling();

                LayoutElement indicatorLE = indicatorGO.AddComponent<LayoutElement>();
                indicatorLE.ignoreLayout = true;

                RectTransform indicatorRT = indicatorGO.GetComponent<RectTransform>();
                indicatorRT.anchorMin = new Vector2(0.5f, 0.5f);
                indicatorRT.anchorMax = new Vector2(0.5f, 0.5f);
                indicatorRT.pivot = new Vector2(0f, 1f);
                indicatorRT.sizeDelta = new Vector2(145f, 30f);
                indicatorRT.anchoredPosition = new Vector2(-9999f, -9999f);

                Image bg = indicatorGO.AddComponent<Image>();
                Image origBtnImg = showBtn.GetComponent<Image>();
                if (origBtnImg != null)
                {
                    bg.sprite = origBtnImg.sprite;
                    bg.type = origBtnImg.type;
                    bg.color = origBtnImg.color;
                }
                else bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

                TextMeshProUGUI indicatorLabel = MakeButtonLabel(indicatorGO, fontAsset);

                DraggableMover mover = indicatorGO.AddComponent<DraggableMover>();
                mover.Bg = bg;
                mover.NormalColor = bg.color;
                mover.HoverColor = bg.color * 1.3f;
                mover.PressColor = bg.color * 0.7f;
                mover.PanelRT = panelRT;
                mover.PanelGO = panelGO;
                mover.ShowBtnRT = showBtnRT;
                mover.Log = log;

                mover.OnClick = () =>
                {
                    bool open = panelGO.activeSelf;
                    if (!open)
                    {
                        panelGO.SetActive(true);
                        mover.PlacePanelUnderButton();
                        scrollRect.verticalNormalizedPosition = 1f;
                        tracker.RefreshRows();
                    }
                    else
                    {
                        panelGO.SetActive(false);
                    }
                };

                tracker.IndicatorLabel = indicatorLabel;
                tracker.IndicatorRT = indicatorRT;
                tracker.Mover = mover;

                log.LogInfo("[FT] Injection complete");
            }
            catch (Exception e)
            {
                log.LogError($"[FT] Inject exception: {e}");
            }
        }

        private static GameObject MakeScrollContent(string name, Transform parent, float spacing, int padding)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = spacing;
            vlg.padding = new RectOffset(padding, padding, padding, padding);

            ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            return go;
        }

        private static TextMeshProUGUI MakeButtonLabel(GameObject parent, TMP_FontAsset font)
        {
            GameObject labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(parent.transform, false);
            RectTransform lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = "<color=#55D5FF>●</color>  FLEETS";
            tmp.fontSize = 11f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void CopyGameScrollbarStyle(Image track, Image handle)
        {
            Scrollbar src = Resources.FindObjectsOfTypeAll<Scrollbar>()
                .FirstOrDefault(sb => sb.handleRect != null && sb.GetComponent<Image>() != null);
            if (src == null)
            {
                track.color = new Color(0.06f, 0.12f, 0.14f, 0.9f);
                handle.color = new Color(0.05f, 0.62f, 0.68f, 0.9f);
                return;
            }

            Image srcTrack = src.GetComponent<Image>();
            Image srcHandle = src.handleRect.GetComponent<Image>();
            if (srcTrack != null)
            {
                track.sprite = srcTrack.sprite;
                track.color = srcTrack.color;
                track.type = srcTrack.type;
            }
            if (srcHandle != null)
            {
                handle.sprite = srcHandle.sprite;
                handle.color = srcHandle.color;
                handle.type = srcHandle.type;
            }
        }

        private static TMP_FontAsset FindFontAsset(NotificationManager nm, GameObject historyGO, ManualLogSource log)
        {
            TextMeshProUGUI src = historyGO.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
            if (src?.font != null) return src.font;
            try
            {
                FieldInfo prefabField = typeof(NotificationManager).GetField("notificationUIPrefab",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                object prefab = prefabField?.GetValue(nm);
                if (prefab != null)
                {
                    FieldInfo textField = prefab.GetType().GetField("text", BindingFlags.Instance | BindingFlags.NonPublic);
                    src = textField?.GetValue(prefab) as TextMeshProUGUI;
                    if (src?.font != null) return src.font;
                }
            }
            catch (Exception e) { log.LogWarning($"[FT] font fallback: {e.Message}"); }
            return null;
        }
    }

    internal class DraggableMover : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal Action OnClick;
        internal Image Bg;
        internal Color NormalColor;
        internal Color HoverColor;
        internal Color PressColor;
        internal RectTransform ShowBtnRT;
        internal ManualLogSource Log;
        internal RectTransform PanelRT;
        internal GameObject PanelGO;

        private const float ButtonGap = 10f;
        private const float ReservedLifeSupportButtonWidth = 150f;
        private RectTransform _rt;
        private Canvas _canvas;
        private RectTransform _canvasRT;
        private Vector2 _dragStartAnchoredPos;
        private Vector2 _pressScreenPos;
        private Vector2 _lastCanvasSize;
        private bool _userMoved;
        private bool _positionedAgainstLifeSupport;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _canvasRT = _canvas?.GetComponent<RectTransform>();
        }

        private IEnumerator Start()
        {
            yield return null;
            PositionNextToKnownButtons();
        }

        private void Update()
        {
            if (_canvasRT == null) return;
            Vector2 sz = _canvasRT.rect.size;
            if (sz != _lastCanvasSize)
            {
                _lastCanvasSize = sz;
                if (!_userMoved) PositionNextToKnownButtons();
                else ClampButton();
                PlacePanelUnderButton();
            }

            if (!_userMoved && !_positionedAgainstLifeSupport && PositionLeftOfLifeSupportButton())
                PlacePanelUnderButton();
        }

        private void PositionNextToKnownButtons()
        {
            if (PositionLeftOfLifeSupportButton()) return;
            PositionWithLifeSupportSlotReserved();
        }

        private bool PositionLeftOfLifeSupportButton()
        {
            RectTransform lifeSupportRT = FindLifeSupportButton();
            if (lifeSupportRT == null || _rt == null || _canvasRT == null) return false;
            Camera cam = _canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : _canvas?.worldCamera;

            Vector3[] corners = new Vector3[4];
            lifeSupportRT.GetWorldCorners(corners);

            Vector2 btnTopLeft;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRT, new Vector2(corners[1].x, corners[1].y), cam, out btnTopLeft))
            {
                Log?.LogWarning("[FT] LifeSupport RectTransformUtility failed");
                return false;
            }

            _rt.anchoredPosition = new Vector2(btnTopLeft.x - ButtonGap - _rt.sizeDelta.x, btnTopLeft.y);
            _positionedAgainstLifeSupport = true;
            ClampButton();
            return true;
        }

        private RectTransform FindLifeSupportButton()
        {
            if (_canvas == null || _rt == null) return null;

            foreach (RectTransform rt in _canvas.GetComponentsInChildren<RectTransform>(includeInactive: true))
            {
                if (rt == null || rt == _rt) continue;
                string name = rt.gameObject.name ?? "";
                if (name.Equals("modLifeSupportButton", StringComparison.OrdinalIgnoreCase) ||
                    name.IndexOf("LifeSupport", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (rt.GetComponent<Button>() != null) return rt;
                }
            }

            foreach (TextMeshProUGUI label in _canvas.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
            {
                if (label == null || string.IsNullOrEmpty(label.text)) continue;
                if (label.text.IndexOf("LIFE SUPPORT", StringComparison.OrdinalIgnoreCase) < 0) continue;
                Button button = label.GetComponentInParent<Button>();
                RectTransform buttonRT = button != null ? button.GetComponent<RectTransform>() : null;
                if (buttonRT != null && buttonRT != _rt) return buttonRT;
            }

            return null;
        }

        private void PositionWithLifeSupportSlotReserved()
        {
            if (ShowBtnRT == null || _rt == null || _canvasRT == null) return;
            Camera cam = _canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : _canvas?.worldCamera;

            Vector3[] corners = new Vector3[4];
            ShowBtnRT.GetWorldCorners(corners);

            Vector2 btnTopLeft;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRT, new Vector2(corners[1].x, corners[1].y), cam, out btnTopLeft))
            {
                Log?.LogWarning("[FT] RectTransformUtility failed - keeping parked position");
                return;
            }

            float x = btnTopLeft.x - ButtonGap - ReservedLifeSupportButtonWidth - ButtonGap - _rt.sizeDelta.x;
            _rt.anchoredPosition = new Vector2(x, btnTopLeft.y - 5f);
            ClampButton();
        }

        internal void PlacePanelUnderButton()
        {
            if (PanelGO == null || !PanelGO.activeSelf || PanelRT == null || _rt == null) return;
            Vector2 p = new Vector2(
                _rt.anchoredPosition.x,
                _rt.anchoredPosition.y - _rt.sizeDelta.y - 4f);
            ClampPanel(ref p);
            PanelRT.anchoredPosition = p;
        }

        public void OnPointerEnter(PointerEventData e) { if (Bg) Bg.color = HoverColor; }
        public void OnPointerExit(PointerEventData e) { if (Bg) Bg.color = NormalColor; }

        public void OnPointerDown(PointerEventData e)
        {
            _pressScreenPos = e.position;
            if (Bg) Bg.color = PressColor;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (Bg) Bg.color = HoverColor;
            if (Vector2.Distance(e.position, _pressScreenPos) < EventSystem.current.pixelDragThreshold)
                OnClick?.Invoke();
        }

        public void OnBeginDrag(PointerEventData e)
        {
            _userMoved = true;
            _dragStartAnchoredPos = _rt.anchoredPosition;
        }

        public void OnDrag(PointerEventData e)
        {
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            _rt.anchoredPosition = _dragStartAnchoredPos + (e.position - _pressScreenPos) / scale;
            ClampButton();
            PlacePanelUnderButton();
        }

        public void OnEndDrag(PointerEventData e)
        {
            ClampButton();
            PlacePanelUnderButton();
            if (Bg) Bg.color = NormalColor;
        }

        private void ClampButton()
        {
            if (_canvasRT == null || _rt == null) return;
            Rect cr = _canvasRT.rect;
            Vector2 s = _rt.sizeDelta;
            Vector2 p = _rt.anchoredPosition;
            p.x = Mathf.Clamp(p.x, cr.xMin, cr.xMax - s.x);
            p.y = Mathf.Clamp(p.y, cr.yMin + s.y, cr.yMax);
            _rt.anchoredPosition = p;
        }

        private void ClampPanel(ref Vector2 p)
        {
            if (_canvasRT == null || PanelRT == null) return;
            Rect cr = _canvasRT.rect;
            Vector2 s = PanelRT.sizeDelta;
            p.x = Mathf.Clamp(p.x, cr.xMin, cr.xMax - s.x);
            p.y = Mathf.Clamp(p.y, cr.yMin + s.y, cr.yMax);
        }
    }

    internal class FleetTrackerPanel : MonoBehaviour
    {
        internal Transform ContentParent;
        internal TMP_FontAsset FontAsset;
        internal ManualLogSource TrackerLog;
        internal TextMeshProUGUI IndicatorLabel;
        internal RectTransform PanelRT;
        internal RectTransform IndicatorRT;
        internal DraggableMover Mover;
        internal ScrollRect ScrollRectRef;

        private float _refreshTimer;
        private const float RefreshInterval = 5.0f;
        private const string FilterAllBodies = "All bodies";
        private const string FilterAllShips = "All ships";
        private const string FilterAllCargo = "All cargo";
        private string _bodyFilter = FilterAllBodies, _shipFilter = FilterAllShips, _cargoFilter = FilterAllCargo;
        private static readonly Color TextColor = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color MutedColor = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color HeaderColor = new Color(0.62f, 0.62f, 0.62f);
        private static readonly Color SectionColor = new Color(0.38f, 0.68f, 0.75f);
        private static readonly Dictionary<string, string> ResourceSpriteByKey =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["id_resource_alloy"] = "resource_definition_id_resource_alloy",
                ["id_resource_antimatter"] = "resource_definition_id_resource_antimatter",
                ["id_resource_chips"] = "resource_definition_id_resource_chips",
                ["id_resource_co2"] = "resource_definition_id_resource_co2",
                ["id_resource_consumergoods"] = "resource_definition_id_resource_consumergoods",
                ["id_resource_energy"] = "resource_definition_id_resource_energy",
                ["id_resource_fuel"] = "resource_definition_id_resource_fuel",
                ["id_resource_glass"] = "resource_definition_id_resource_glass",
                ["id_resource_hel3"] = "resource_definition_id_resource_HEL3",
                ["id_resource_human"] = "resource_definition_id_resource_human",
                ["id_resource_hydrogen"] = "resource_definition_id_resource_hydrogen",
                ["id_resource_metal"] = "resource_definition_id_resource_metal",
                ["id_resource_nitrogen"] = "resource_definition_id_resource_nitrogen",
                ["id_resource_noblegas"] = "resource_definition_id_resource_noblegas",
                ["id_resource_oxygen"] = "resource_definition_id_resource_oxygen",
                ["id_resource_plastic"] = "resource_definition_id_resource_plastic",
                ["id_resource_raremetal"] = "resource_definition_id_resource_raremetal",
                ["id_resource_silicon"] = "resource_definition_id_resource_silicon",
                ["id_resource_steel"] = "resource_definition_id_resource_steel",
                ["id_resource_supply"] = "resource_definition_id_resource_supply",
                ["id_resource_uran"] = "resource_definition_id_resource_uran",
                ["id_resource_volatile"] = "resource_definition_id_resource_volatile",
                ["id_resource_water"] = "resource_definition_id_resource_water"
            };
        private static readonly Dictionary<string, string> ResourceKeyByName =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["alloys"] = "id_resource_alloy",
                ["antimatter"] = "id_resource_antimatter",
                ["co2"] = "id_resource_co2",
                ["consumergoods"] = "id_resource_consumergoods",
                ["electronics"] = "id_resource_chips",
                ["chips"] = "id_resource_chips",
                ["fissiles"] = "id_resource_uran",
                ["uranium"] = "id_resource_uran",
                ["fuel"] = "id_resource_fuel",
                ["glass"] = "id_resource_glass",
                ["helium3"] = "id_resource_hel3",
                ["hel3"] = "id_resource_hel3",
                ["humans"] = "id_resource_human",
                ["crew"] = "id_resource_human",
                ["hydrogen"] = "id_resource_hydrogen",
                ["metals"] = "id_resource_metal",
                ["metal"] = "id_resource_metal",
                ["nitrogen"] = "id_resource_nitrogen",
                ["noblegas"] = "id_resource_noblegas",
                ["noblegases"] = "id_resource_noblegas",
                ["oxygen"] = "id_resource_oxygen",
                ["polymer"] = "id_resource_plastic",
                ["plastic"] = "id_resource_plastic",
                ["power"] = "id_resource_energy",
                ["energy"] = "id_resource_energy",
                ["raremetals"] = "id_resource_raremetal",
                ["raremetal"] = "id_resource_raremetal",
                ["silicon"] = "id_resource_silicon",
                ["steel"] = "id_resource_steel",
                ["supply"] = "id_resource_supply",
                ["supplies"] = "id_resource_supply",
                ["volatiles"] = "id_resource_volatile",
                ["volatile"] = "id_resource_volatile",
                ["water"] = "id_resource_water"
            };
        private const BindingFlags AnyMember =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        private void Update()
        {
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= RefreshInterval)
            {
                _refreshTimer = 0f;
                RefreshRows();
            }
        }

        internal void RefreshRows()
        {
            try
            {
                if (ContentParent == null) return;

                for (int i = ContentParent.childCount - 1; i >= 0; i--)
                    Destroy(ContentParent.GetChild(i).gameObject);

                FleetSnapshot snapshot = BuildSnapshot();
                List<string> bodyOptions = BuildBodyFilterOptions(snapshot);
                List<string> shipOptions = BuildShipFilterOptions(snapshot);
                List<string> cargoOptions = BuildCargoFilterOptions(snapshot);
                NormalizeFilters(bodyOptions, shipOptions, cargoOptions);
                ApplyFilters(snapshot);

                AddTitleRow($"FLEET STATUS  ({snapshot.TotalShips} {Plural(snapshot.TotalShips, "ship", "ships")}, {snapshot.TotalMissions} {Plural(snapshot.TotalMissions, "mission", "missions")}, {snapshot.TotalConstruction} building)");
                AddFilterRow(bodyOptions, shipOptions, cargoOptions);

                if (!string.IsNullOrEmpty(snapshot.Message))
                {
                    AddMessageRow(snapshot.Message);
                    UpdateIndicator(snapshot.TotalShips);
                    return;
                }

                if (snapshot.AtBodies.Count > 0)
                {
                    AddSectionSeparator($"SHIPS AT BODIES ({snapshot.AtBodies.Sum(r => r.Count)})");
                    AddHeaderRow(
                        Col("BODY", 190f, 0f, TextAlignmentOptions.MidlineLeft),
                        Col("SHIPS", 0f, 1f, TextAlignmentOptions.MidlineLeft, 220f));
                    foreach (BodyFleetGroup row in snapshot.AtBodies)
                        BuildAtBodyRow(row);
                }

                if (snapshot.InTransit.Count > 0)
                {
                    AddSectionSeparator($"SHIPS IN TRANSIT ({snapshot.InTransit.Count})");
                    AddHeaderRow(
                        Col("ROUTE", 225f, 0f, TextAlignmentOptions.MidlineLeft),
                        Col("SHIPS", 155f, 0f, TextAlignmentOptions.MidlineLeft),
                        Col("ARRIVAL", 130f, 0f, TextAlignmentOptions.MidlineRight),
                        Col("CARGO", 0f, 1f, TextAlignmentOptions.MidlineLeft, 200f));
                    foreach (MissionFleetRow row in snapshot.InTransit)
                        BuildTransitRow(row, planned: false);
                }

                if (snapshot.Planned.Count > 0)
                {
                    AddSectionSeparator($"PLANNED TRANSITS ({snapshot.Planned.Count})");
                    AddHeaderRow(
                        Col("ROUTE", 205f, 0f, TextAlignmentOptions.MidlineLeft),
                        Col("SHIPS", 135f, 0f, TextAlignmentOptions.MidlineLeft),
                        Col("START", 115f, 0f, TextAlignmentOptions.MidlineRight),
                        Col("ARRIVAL", 115f, 0f, TextAlignmentOptions.MidlineRight),
                        Col("CARGO", 0f, 1f, TextAlignmentOptions.MidlineLeft, 190f));
                    foreach (MissionFleetRow row in snapshot.Planned)
                        BuildTransitRow(row, planned: true);
                }

                if (snapshot.Construction.Count > 0)
                {
                    AddSectionSeparator($"SHIPS IN CONSTRUCTION ({snapshot.Construction.Sum(r => r.Count)})");
                    AddHeaderRow(
                        Col("BODY", 170f, 0f, TextAlignmentOptions.MidlineLeft),
                        Col("SHIP", 80f, 0f, TextAlignmentOptions.MidlineLeft),
                        Col("FINISH", 130f, 0f, TextAlignmentOptions.MidlineRight),
                        Col("STATUS", 0f, 1f, TextAlignmentOptions.MidlineRight, 110f));
                    foreach (ConstructionFleetRow row in snapshot.Construction)
                        BuildConstructionRow(row);
                }

                if (snapshot.AtBodies.Count == 0 &&
                    snapshot.InTransit.Count == 0 &&
                    snapshot.Planned.Count == 0 &&
                    snapshot.Construction.Count == 0)
                {
                    AddMessageRow("No player fleet data found.");
                }

                UpdateIndicator(snapshot.TotalShips);
                LayoutRebuilder.ForceRebuildLayoutImmediate(ContentParent as RectTransform);
            }
            catch (Exception e)
            {
                TrackerLog.LogError($"[FT] RefreshRows exception: {e}");
            }
        }

        private FleetSnapshot BuildSnapshot()
        {
            FleetSnapshot snapshot = new FleetSnapshot();
            Company player = MonoBehaviourSingleton<GameManager>.Instance?.Player;
            if (player == null)
            {
                snapshot.Message = "Not in game yet.";
                return snapshot;
            }

            DateTime? now = GetCurrentTime();
            List<ObjectInfo> allObjects = MonoBehaviourSingleton<ObjectInfoManager>.Instance?.allObjectInfos;
            if (allObjects == null)
            {
                snapshot.Message = "No object data.";
                return snapshot;
            }

            BuildShipsAtBodies(snapshot, player);
            BuildMissionRows(snapshot, player, now);
            BuildConstructionRows(snapshot, player, allObjects, now);

            snapshot.AtBodies.Sort((a, b) =>
                string.Compare(a.BodyName, b.BodyName, StringComparison.OrdinalIgnoreCase));
            foreach (BodyFleetGroup group in snapshot.AtBodies)
                group.Ships.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            snapshot.InTransit.Sort((a, b) => NullableDateCompare(a.Arrival, b.Arrival));
            snapshot.Planned.Sort((a, b) => NullableDateCompare(a.Start, b.Start));
            snapshot.Construction.Sort((a, b) => NullableDateCompare(a.Finish, b.Finish));

            RecalculateTotals(snapshot);
            return snapshot;
        }

        private void ApplyFilters(FleetSnapshot snapshot)
        {
            bool bodyActive = _bodyFilter != FilterAllBodies;
            bool shipActive = _shipFilter != FilterAllShips;
            bool cargoActive = _cargoFilter != FilterAllCargo;

            snapshot.AtBodies.RemoveAll(row =>
            {
                if (bodyActive && !SameFilter(row.BodyName, _bodyFilter)) return true;
                if (cargoActive) return true;
                if (shipActive) row.Ships.RemoveAll(ship => !SameFilter(ship.DisplayName, _shipFilter));
                return shipActive && row.Ships.Count == 0;
            });
            snapshot.InTransit.RemoveAll(row => !MissionMatchesFilters(row, bodyActive, shipActive, cargoActive));
            snapshot.Planned.RemoveAll(row => !MissionMatchesFilters(row, bodyActive, shipActive, cargoActive));
            snapshot.Construction.RemoveAll(row =>
                (bodyActive && !SameFilter(row.BodyName, _bodyFilter)) ||
                (shipActive && !SameFilter(row.ShipTypeName, _shipFilter)) ||
                cargoActive);

            RecalculateTotals(snapshot);
        }

        private bool MissionMatchesFilters(MissionFleetRow row, bool bodyActive, bool shipActive, bool cargoActive)
        {
            if (bodyActive && !SameFilter(row.OriginName, _bodyFilter) && !SameFilter(row.TargetName, _bodyFilter)) return false;
            if (shipActive && !row.Ships.Any(ship => SameFilter(ship.DisplayName, _shipFilter))) return false;
            if (cargoActive && !row.CargoLabels.Any(label => SameFilter(label, _cargoFilter))) return false;
            return true;
        }

        private static void RecalculateTotals(FleetSnapshot snapshot)
        {
            snapshot.TotalShips = snapshot.AtBodies.Sum(r => r.Count) + snapshot.InTransit.Sum(r => r.ShipCount) + snapshot.Planned.Sum(r => r.ShipCount);
            snapshot.TotalMissions = snapshot.InTransit.Count + snapshot.Planned.Count;
            snapshot.TotalConstruction = snapshot.Construction.Sum(r => r.Count);
        }

        private void NormalizeFilters(List<string> bodies, List<string> ships, List<string> cargo)
        {
            if (!bodies.Any(option => SameFilter(option, _bodyFilter))) _bodyFilter = FilterAllBodies;
            if (!ships.Any(option => SameFilter(option, _shipFilter))) _shipFilter = FilterAllShips;
            if (!cargo.Any(option => SameFilter(option, _cargoFilter))) _cargoFilter = FilterAllCargo;
        }

        private static List<string> BuildBodyFilterOptions(FleetSnapshot snapshot)
        {
            SortedSet<string> values = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (BodyFleetGroup row in snapshot.AtBodies) AddOption(values, row.BodyName);
            foreach (MissionFleetRow row in snapshot.InTransit.Concat(snapshot.Planned))
            {
                AddOption(values, row.OriginName);
                AddOption(values, row.TargetName);
            }
            foreach (ConstructionFleetRow row in snapshot.Construction) AddOption(values, row.BodyName);
            return WithAll(FilterAllBodies, values);
        }

        private static List<string> BuildShipFilterOptions(FleetSnapshot snapshot)
        {
            SortedSet<string> values = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (BodyFleetGroup row in snapshot.AtBodies)
                foreach (ShipIconCount ship in row.Ships) AddOption(values, ship.DisplayName);
            foreach (MissionFleetRow row in snapshot.InTransit.Concat(snapshot.Planned))
                foreach (ShipIconCount ship in row.Ships) AddOption(values, ship.DisplayName);
            foreach (ConstructionFleetRow row in snapshot.Construction) AddOption(values, row.ShipTypeName);
            return WithAll(FilterAllShips, values);
        }

        private static List<string> BuildCargoFilterOptions(FleetSnapshot snapshot)
        {
            SortedSet<string> values = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (MissionFleetRow row in snapshot.InTransit.Concat(snapshot.Planned))
                foreach (string label in row.CargoLabels) AddOption(values, label);
            return WithAll(FilterAllCargo, values);
        }

        private static void AddOption(SortedSet<string> values, string value)
        {
            if (!string.IsNullOrEmpty(value)) values.Add(value);
        }

        private static List<string> WithAll(string allLabel, SortedSet<string> values)
        {
            List<string> result = new List<string> { allLabel };
            result.AddRange(values);
            return result;
        }

        private static bool SameFilter(string left, string right) => string.Equals(left ?? "", right ?? "", StringComparison.OrdinalIgnoreCase);

        private void BuildShipsAtBodies(FleetSnapshot snapshot, Company player)
        {
            var allShips = MonoBehaviourSingleton<ShipManager>.Instance?.ListAllSpaceShip;
            if (allShips == null) return;

            Dictionary<string, BodyFleetGroup> grouped = new Dictionary<string, BodyFleetGroup>();
            foreach (Spacecraft sc in allShips)
            {
                if (sc == null || !IsPlayerShip(sc, player)) continue;
                snapshot.TotalShips++;

                if (IsTransitPhase(sc.CurrentPhase)) continue;

                ObjectInfo body = Safe(() => sc.CurrentlyOnThisObject) ?? Safe(() => sc.MissionStart);
                if (body == null) continue;

                string typeName = GetSpacecraftTypeName(sc.spacecraftType, Safe(() => sc.GetSpacecraftName()));
                string shipSprite = ReadStringMember(sc.spacecraftType, "SpriteId");
                string bodySprite = body.ImagePlanetUI?.name ?? "";
                string key = $"{body.ObjectName}\u001f{bodySprite}";

                if (!grouped.TryGetValue(key, out BodyFleetGroup row))
                {
                    row = new BodyFleetGroup
                    {
                        Body = body,
                        BodyName = body.ObjectName,
                        BodySpriteName = bodySprite
                    };
                    grouped[key] = row;
                }
                AddShipCount(row.Ships, typeName, shipSprite);
            }
            snapshot.AtBodies.AddRange(grouped.Values);
        }

        private void BuildMissionRows(FleetSnapshot snapshot, Company player, DateTime? now)
        {
            List<MissionInfo> missions = MonoBehaviourSingleton<MissionInfoManager>.Instance?.ListMissionInfo;
            if (missions == null) return;

            HashSet<MissionInfo> seen = new HashSet<MissionInfo>();
            foreach (MissionInfo mi in missions)
            {
                if (mi == null || !seen.Add(mi)) continue;
                if (ReadBoolMember(mi, "cancel") || ReadBoolMember(mi, "complete")) continue;
                if (!MissionBelongsToPlayer(mi, player)) continue;

                ObjectInfo origin = Safe(() => mi.start);
                ObjectInfo target = Safe(() => mi.target);
                if (origin == null || target == null) continue;

                DateTime launch = Safe(() => mi.DateLaunch);
                DateTime arrival = Safe(() => mi.DateArrive);
                if (launch == default(DateTime) || arrival == default(DateTime)) continue;

                List<object> craftInfos = GetMissionCraftInfos(mi);
                List<ShipIconCount> ships = GetCraftIconCounts(craftInfos);

                MissionFleetRow row = new MissionFleetRow
                {
                    Mission = mi,
                    Origin = origin,
                    Target = target,
                    OriginName = origin.ObjectName,
                    TargetName = target.ObjectName,
                    OriginSpriteName = origin.ImagePlanetUI?.name ?? "",
                    TargetSpriteName = target.ImagePlanetUI?.name ?? "",
                    Ships = ships,
                    Start = launch,
                    Arrival = arrival,
                    Cargo = FormatCargoIcons(ReadMemberValue(mi, "cargoAll", "CargoAll"), out List<string> cargoLabels),
                    CargoLabels = cargoLabels,
                    OpenTarget = FindOpenSpacecraft(craftInfos)
                };

                if (now.HasValue && launch > now.Value)
                    snapshot.Planned.Add(row);
                else if (!now.HasValue || arrival >= now.Value)
                    snapshot.InTransit.Add(row);
            }
        }

        private void BuildConstructionRows(FleetSnapshot snapshot, Company player, List<ObjectInfo> allObjects, DateTime? now)
        {
            foreach (ObjectInfo oi in allObjects)
            {
                ObjectInfoData data = Safe(() => oi.GetObjectInfoData(player));
                if (data == null) continue;

                IEnumerable constructs = InvokeMember(data, "GetListRocketConstruct") as IEnumerable;
                if (constructs == null) continue;

                Dictionary<object, Queue<object>> productionItems = BuildProductionItemQueues(data);
                Dictionary<string, ConstructionFleetRow> grouped = new Dictionary<string, ConstructionFleetRow>();

                foreach (object construct in constructs)
                {
                    if (construct == null) continue;

                    object spacecraftType = ReadMemberValue(construct, "SpacecraftType", "spacecraftType");
                    if (spacecraftType == null) continue;

                    object productionType = InvokeMember(construct, "FindProductionItemType");
                    object productionItem = DequeueMatchingProductionItem(productionItems, productionType);

                    DateTime? finish = GetConstructionFinishDate(data, productionItem, construct, now);
                    string status = GetConstructionStatus(productionItem);
                    string typeName = ReadDisplayName(spacecraftType);
                    if (string.IsNullOrEmpty(typeName)) typeName = ReadStringMember(construct, "SpaceCraftName", "spaceCraftName", "GetSpacecraftName");
                    if (string.IsNullOrEmpty(typeName)) typeName = "Spacecraft";

                    string shipSprite = ReadStringMember(spacecraftType, "SpriteId");
                    string bodySprite = oi.ImagePlanetUI?.name ?? "";
                    string key = $"{oi.ObjectName}\u001f{typeName}\u001f{FormatDate(finish, now)}\u001f{status}";

                    if (!grouped.TryGetValue(key, out ConstructionFleetRow row))
                    {
                        row = new ConstructionFleetRow
                        {
                            Body = oi,
                            BodyName = oi.ObjectName,
                            BodySpriteName = bodySprite,
                            ShipTypeName = typeName,
                            ShipSpriteName = shipSprite,
                            Finish = finish,
                            Status = status,
                            Count = 0
                        };
                        grouped[key] = row;
                    }
                    row.Count++;
                }

                snapshot.Construction.AddRange(grouped.Values);
            }
        }

        private Dictionary<object, Queue<object>> BuildProductionItemQueues(ObjectInfoData data)
        {
            Dictionary<object, Queue<object>> result = new Dictionary<object, Queue<object>>(ReferenceEqualityComparer.Instance);
            IEnumerable items = InvokeMember(data, "GetProductionItemSCLV") as IEnumerable;
            if (items == null) return result;

            foreach (object item in items)
            {
                if (item == null || ReadBoolMember(item, "FinishConstructionBool")) continue;
                object productionType = ReadMemberValue(item, "ProductionItemType", "productionItemType")
                    ?? InvokeMember(item, "FindProductionItemType");
                if (productionType == null) continue;
                if (!result.TryGetValue(productionType, out Queue<object> queue))
                {
                    queue = new Queue<object>();
                    result[productionType] = queue;
                }
                queue.Enqueue(item);
            }
            return result;
        }

        private object DequeueMatchingProductionItem(Dictionary<object, Queue<object>> productionItems, object productionType)
        {
            if (productionType == null) return null;
            if (!productionItems.TryGetValue(productionType, out Queue<object> queue)) return null;
            return queue.Count > 0 ? queue.Dequeue() : null;
        }

        private DateTime? GetConstructionFinishDate(ObjectInfoData data, object productionItem, object construct, DateTime? now)
        {
            DateTime? finish = null;
            if (productionItem != null)
            {
                finish = ToDateTime(ReadMemberValue(productionItem, "WhenBuild", "whenBuild"));
                if (!finish.HasValue)
                    finish = ToDateTime(InvokeMember(data, "WhenBuild", productionItem));
            }

            if (finish.HasValue) return finish;

            double days = ToDouble(InvokeMember(construct, "TimeToBuildInDays"));
            if (now.HasValue && days > 0) return now.Value.AddDays(days);
            return null;
        }

        private string GetConstructionStatus(object productionItem)
        {
            if (productionItem == null) return "planned";
            bool started = ReadBoolMember(productionItem, "StartBuild", "startBuild");
            double progress = ToDouble(ReadMemberValue(productionItem, "BuildProgress", "buildProgress"));
            if (!started) return "queued";
            return progress > 0 ? $"building {FormatPercent(progress)}" : "building";
        }

        private static bool IsTransitPhase(Spacecraft.EPhase phase)
        {
            return phase == Spacecraft.EPhase.Fly ||
                   phase == Spacecraft.EPhase.Launch ||
                   phase == Spacecraft.EPhase.Landing;
        }

        private static bool IsPlayerShip(Spacecraft sc, Company player)
        {
            try
            {
                Company company = sc.GetCompany();
                return company != null && (company.IsPlayer || ReferenceEquals(company, player));
            }
            catch { return false; }
        }

        private bool MissionBelongsToPlayer(MissionInfo mi, Company player)
        {
            Company company = ReadMemberValue(mi, "company", "Company") as Company;
            if (company != null) return company.IsPlayer || ReferenceEquals(company, player);

            foreach (object craft in GetMissionCraftInfos(mi))
            {
                Company craftCompany = InvokeMember(craft, "GetCompany") as Company;
                if (craftCompany != null && (craftCompany.IsPlayer || ReferenceEquals(craftCompany, player)))
                    return true;
            }
            return false;
        }

        private List<object> GetMissionCraftInfos(MissionInfo mi)
        {
            List<object> result = new List<object>();
            AddEnumerable(result, ReadMemberValue(mi, "ListSpacecraftInfo2", "listSpacecraftInfo2"));
            AddSingle(result, ReadMemberValue(mi, "spacecraftInfo2", "SpacecraftInfo2"));
            AddSingle(result, ReadMemberValue(mi, "spacecraftInfo", "SpacecraftInfo"));
            return DistinctByReference(result);
        }

        private static void AddEnumerable(List<object> result, object maybeEnumerable)
        {
            if (maybeEnumerable == null || maybeEnumerable is string) return;
            if (!(maybeEnumerable is IEnumerable enumerable)) return;
            foreach (object item in enumerable)
                AddSingle(result, item);
        }

        private static void AddSingle(List<object> result, object item)
        {
            if (item != null) result.Add(item);
        }

        private static List<object> DistinctByReference(List<object> items)
        {
            List<object> result = new List<object>();
            HashSet<object> seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (object item in items)
                if (item != null && seen.Add(item)) result.Add(item);
            return result;
        }

        private string SummarizeCraftInfos(List<object> craftInfos)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (object craft in craftInfos)
            {
                string name = GetCraftTypeName(craft);
                if (string.IsNullOrEmpty(name)) continue;
                counts[name] = counts.TryGetValue(name, out int count) ? count + 1 : 1;
            }
            return string.Join(", ", counts
                .OrderBy(kv => kv.Key)
                .Select(kv => kv.Value > 1 ? $"{kv.Value}x {kv.Key}" : kv.Key));
        }

        private List<ShipIconCount> GetCraftIconCounts(List<object> craftInfos)
        {
            List<ShipIconCount> ships = new List<ShipIconCount>();
            foreach (object craft in craftInfos)
            {
                string name = GetCraftTypeName(craft);
                string spriteName = GetCraftSpriteName(craft);
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(spriteName)) continue;
                AddShipCount(ships, string.IsNullOrEmpty(name) ? "Spacecraft" : name, spriteName);
            }
            ships.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            return ships;
        }

        private string GetCraftTypeName(object craft)
        {
            if (craft == null) return "";

            Spacecraft sc = craft as Spacecraft
                ?? ReadMemberValue(craft, "spacecraft", "Spacecraft", "SpacecraftBuild") as Spacecraft;
            if (sc != null)
                return GetSpacecraftTypeName(sc.spacecraftType, Safe(() => sc.GetSpacecraftName()));

            object spacecraftType = ReadMemberValue(craft, "SpacecraftType", "spacecraftType");
            if (spacecraftType != null)
                return ReadDisplayName(spacecraftType);

            string viaMethod = ReadStringMember(craft, "GetSpacecraftName");
            if (!string.IsNullOrEmpty(viaMethod)) return viaMethod;
            return ReadDisplayName(craft);
        }

        private string GetCraftSpriteName(object craft)
        {
            if (craft == null) return "";

            Spacecraft sc = craft as Spacecraft
                ?? ReadMemberValue(craft, "spacecraft", "Spacecraft", "SpacecraftBuild") as Spacecraft;
            if (sc != null)
                return ReadStringMember(sc.spacecraftType, "SpriteId");

            object spacecraftType = ReadMemberValue(craft, "SpacecraftType", "spacecraftType");
            if (spacecraftType != null)
                return ReadStringMember(spacecraftType, "SpriteId");

            return ReadStringMember(craft, "SpriteId");
        }

        private Spacecraft FindOpenSpacecraft(List<object> craftInfos)
        {
            foreach (object craft in craftInfos)
            {
                Spacecraft sc = craft as Spacecraft
                    ?? ReadMemberValue(craft, "spacecraft", "Spacecraft", "SpacecraftBuild") as Spacecraft;
                if (sc != null) return sc;
            }
            return null;
        }

        private string FormatCargoIcons(object cargoAll, out List<string> labels)
        {
            labels = new List<string>();
            if (cargoAll == null) return EmptyCargoText();

            List<string> parts = new List<string>();
            AddCargoList(parts, labels, ReadMemberValue(cargoAll, "listCargo", "listCargoData"));
            AddCargoList(parts, labels, ReadMemberValue(cargoAll, "listCargoToOrbit", "listCargoDataToOrbit"));
            AddCargoList(parts, labels, ReadMemberValue(cargoAll, "listCargoGravityAssists", "listCargoGravityAssists"));
            AddCargoItem(parts, labels, ReadMemberValue(cargoAll, "cargoFuel", "CargoFuel"), fuel: true);

            if (parts.Count == 0) return EmptyCargoText();
            return string.Join(" ", parts.Take(8));
        }

        private void AddCargoList(List<string> parts, List<string> labels, object listObject)
        {
            if (listObject == null || listObject is string) return;
            if (!(listObject is IEnumerable enumerable)) return;
            foreach (object item in enumerable)
                AddCargoItem(parts, labels, item, fuel: false);
        }

        private void AddCargoItem(List<string> parts, List<string> labels, object item, bool fuel)
        {
            if (item == null) return;

            double mass = ToDouble(ReadMemberValue(item, "cargoMass", "CargoMass"));
            long crew = ToLong(ReadMemberValue(item, "crewValue", "CrewValue"));
            object resourceType = ReadMemberValue(item, "resourceType", "ResourceType");
            object moduleData = ReadMemberValue(item, "moduleData", "ModuleData");

            if (mass <= 0 && crew <= 0) return;

            string icon = ResolveCargoIcon(resourceType);
            if (string.IsNullOrEmpty(icon)) icon = ResolveCargoIcon(moduleData);
            if (string.IsNullOrEmpty(icon) && fuel) icon = ResourceSpriteTagForKey("id_resource_fuel");
            if (string.IsNullOrEmpty(icon) && crew > 0) icon = ResourceSpriteTagForKey("id_resource_human");
            if (string.IsNullOrEmpty(icon)) return;

            if (!parts.Contains(icon)) parts.Add(icon);

            string label = ReadDisplayName(resourceType);
            if (string.IsNullOrEmpty(label)) label = ReadDisplayName(moduleData);
            if (string.IsNullOrEmpty(label) && fuel) label = "Fuel";
            if (string.IsNullOrEmpty(label) && crew > 0) label = "Crew";
            if (!string.IsNullOrEmpty(label) && !labels.Contains(label)) labels.Add(label);
        }

        private static string EmptyCargoText() => "<color=#666666>empty</color>";

        private static string ResolveCargoIcon(object cargoDefinition)
        {
            if (cargoDefinition == null) return "";

            foreach (string iconMember in new[] { "IconString", "GetIconString", "IconWithLinkString", "GetIconWithLinkString" })
            {
                string iconText = ReadStringMember(cargoDefinition, iconMember);
                string spriteTag = ExtractFirstSpriteTag(iconText);
                if (!string.IsNullOrEmpty(spriteTag)) return spriteTag;
            }

            string spriteId = ReadStringMember(cargoDefinition, "SpriteId", "spriteId", "SpriteID", "spriteID");
            if (!string.IsNullOrEmpty(spriteId))
            {
                string spriteTag = ExtractFirstSpriteTag(spriteId);
                if (!string.IsNullOrEmpty(spriteTag)) return spriteTag;
                if (spriteId.StartsWith("id_resource_", StringComparison.OrdinalIgnoreCase))
                    return ResourceSpriteTagForKey(spriteId);
                return SpriteTag(spriteId);
            }

            string key = ReadResourceKey(cargoDefinition);
            if (!string.IsNullOrEmpty(key)) return ResourceSpriteTagForKey(key);

            string displayName = ReadDisplayName(cargoDefinition);
            if (ResourceKeyByName.TryGetValue(NormalizeResourceName(displayName), out string mappedKey))
                return ResourceSpriteTagForKey(mappedKey);

            return "";
        }

        private static string ReadResourceKey(object obj)
        {
            foreach (string member in new[]
            {
                "Id", "ID", "id", "IDSave", "idSave", "ResourceDefinitionIDSave",
                "resourceDefinitionIDSave", "IdResourceName", "idResourceName", "NameResources"
            })
            {
                string value = ReadStringMember(obj, member);
                if (string.IsNullOrEmpty(value)) continue;
                int slash = value.LastIndexOf('/');
                if (slash >= 0 && slash < value.Length - 1) value = value.Substring(slash + 1);
                if (value.StartsWith("id_resource_", StringComparison.OrdinalIgnoreCase)) return value;
            }
            return "";
        }

        private static string ExtractFirstSpriteTag(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int start = text.IndexOf("<sprite", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return "";
            int end = text.IndexOf(">", start, StringComparison.Ordinal);
            return end < 0 ? "" : text.Substring(start, end - start + 1).Trim();
        }

        private static string ResourceSpriteTagForKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (ResourceSpriteByKey.TryGetValue(key, out string spriteName))
                return SpriteTag(spriteName);
            if (key.StartsWith("id_resource_", StringComparison.OrdinalIgnoreCase))
                return SpriteTag($"resource_definition_{key}");
            return "";
        }

        private static string SpriteTag(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return "";
            return $"<sprite name={spriteName}>";
        }

        private static string NormalizeResourceName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return new string(value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static DateTime? GetCurrentTime()
        {
            try
            {
                TimeController tc = MonoBehaviourSingleton<TimeController>.Instance;
                if (tc != null) return tc.CurrentTime;
            }
            catch { }
            return null;
        }

        private static int NullableDateCompare(DateTime? a, DateTime? b)
        {
            if (a.HasValue && b.HasValue) return a.Value.CompareTo(b.Value);
            if (a.HasValue) return -1;
            if (b.HasValue) return 1;
            return 0;
        }

        private void BuildAtBodyRow(BodyFleetGroup row)
        {
            GameObject rowGO = MakeRowContainer($"Body_{row.BodyName}", 24f);
            MakeClickable(rowGO, () => OpenObject(row.Body));
            AddColumn(rowGO.transform, 190f, 0f, TextAlignmentOptions.MidlineLeft, FormatBody(row.BodySpriteName, row.BodyName), 130f).color = Color.white;
            AddColumn(rowGO.transform, 0f, 1f, TextAlignmentOptions.MidlineLeft, FormatShipIconCounts(row.Ships), 220f).color = TextColor;
        }

        private void BuildTransitRow(MissionFleetRow row, bool planned)
        {
            GameObject rowGO = MakeRowContainer($"Mission_{row.OriginName}_{row.TargetName}", 22f);
            if (row.Mission != null)
                MakeClickable(rowGO, () => OpenMission(row.Mission, row.OpenTarget));
            else if (row.OpenTarget != null)
                MakeClickable(rowGO, () => OpenSpacecraft(row.OpenTarget));

            string route = $"{FormatBody(row.OriginSpriteName, row.OriginName)} <color=#777777>-></color> {FormatBody(row.TargetSpriteName, row.TargetName)}";
            AddColumn(rowGO.transform, planned ? 205f : 225f, 0f, TextAlignmentOptions.MidlineLeft, route, 160f).color = Color.white;
            AddColumn(rowGO.transform, planned ? 135f : 155f, 0f, TextAlignmentOptions.MidlineLeft, FormatShipIconCounts(row.Ships), 110f).color = TextColor;
            if (planned)
                AddColumn(rowGO.transform, 115f, 0f, TextAlignmentOptions.MidlineRight, FormatDate(row.Start, GetCurrentTime())).color = TextColor;
            AddColumn(rowGO.transform, planned ? 115f : 130f, 0f, TextAlignmentOptions.MidlineRight, FormatDate(row.Arrival, GetCurrentTime())).color = TextColor;
            AddColumn(rowGO.transform, 0f, 1f, TextAlignmentOptions.MidlineLeft, row.Cargo, planned ? 190f : 200f).color = TextColor;
        }

        private void BuildConstructionRow(ConstructionFleetRow row)
        {
            GameObject rowGO = MakeRowContainer($"Build_{row.BodyName}_{row.ShipTypeName}", 22f);
            MakeClickable(rowGO, () => OpenObject(row.Body));
            AddColumn(rowGO.transform, 170f, 0f, TextAlignmentOptions.MidlineLeft, FormatBody(row.BodySpriteName, row.BodyName), 125f).color = Color.white;
            AddColumn(rowGO.transform, 80f, 0f, TextAlignmentOptions.MidlineLeft, FormatShipIconCount(row.ShipSpriteName, row.Count), 60f).color = TextColor;
            AddColumn(rowGO.transform, 130f, 0f, TextAlignmentOptions.MidlineRight, FormatDate(row.Finish, GetCurrentTime())).color = TextColor;
            AddColumn(rowGO.transform, 0f, 1f, TextAlignmentOptions.MidlineRight, row.Status, 110f).color = TextColor;
        }

        private void AddFilterRow(List<string> bodyOptions, List<string> shipOptions, List<string> cargoOptions)
        {
            GameObject rowGO = MakeRowContainer("FilterRow", 30f);
            HorizontalLayoutGroup hlg = rowGO.GetComponent<HorizontalLayoutGroup>();

            GameObject spacer = new GameObject("FilterSpacer", typeof(RectTransform));
            spacer.transform.SetParent(rowGO.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;

            AddDropdown(rowGO.transform, "Body", bodyOptions, _bodyFilter, value => { _bodyFilter = value; RefreshRows(); });
            AddDropdown(rowGO.transform, "Ship", shipOptions, _shipFilter, value => { _shipFilter = value; RefreshRows(); });
            AddDropdown(rowGO.transform, "Cargo", cargoOptions, _cargoFilter, value => { _cargoFilter = value; RefreshRows(); });
            AddClearFilterButton(rowGO.transform);
        }

        private void AddDropdown(Transform parent, string label, List<string> options, string selected, Action<string> onSelected)
        {
            GameObject root = new GameObject($"{label}Filter", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            root.AddComponent<LayoutElement>().preferredWidth = 135f;
            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.10f, 0.12f, 0.95f);

            TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();
            int selectedIndex = Mathf.Max(0, options.FindIndex(option => SameFilter(option, selected)));
            dropdown.options = options.Select(option => new TMP_Dropdown.OptionData(option)).ToList();
            dropdown.value = selectedIndex;
            dropdown.targetGraphic = bg;

            TextMeshProUGUI caption = AddDropdownText(root.transform, "Label", $"{label}: {options[selectedIndex]}", TextAlignmentOptions.MidlineLeft);
            caption.margin = new Vector4(6, 0, 16, 0);
            dropdown.captionText = caption;
            dropdown.template = BuildDropdownTemplate(root.transform, out TextMeshProUGUI itemText);
            dropdown.itemText = itemText;
            dropdown.onValueChanged.AddListener(index =>
            {
                if (index < 0 || index >= options.Count) return;
                onSelected(options[index]);
            });
        }

        private RectTransform BuildDropdownTemplate(Transform parent, out TextMeshProUGUI itemText)
        {
            GameObject templateGO = new GameObject("Template", typeof(RectTransform));
            templateGO.transform.SetParent(parent, false);
            RectTransform templateRT = templateGO.GetComponent<RectTransform>();
            templateRT.anchorMin = new Vector2(0f, 0f);
            templateRT.anchorMax = new Vector2(1f, 0f);
            templateRT.pivot = new Vector2(0.5f, 1f);
            templateRT.sizeDelta = new Vector2(0f, 140f);
            templateRT.anchoredPosition = new Vector2(0f, -2f);
            templateGO.SetActive(false);
            Image templateBg = templateGO.AddComponent<Image>();
            templateBg.color = new Color(0.05f, 0.06f, 0.08f, 0.98f);
            templateGO.AddComponent<ScrollRect>();

            GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(templateGO.transform, false);
            RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();

            GameObject contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = Vector2.zero;
            VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = templateGO.GetComponent<ScrollRect>();
            scroll.viewport = viewportRT;
            scroll.content = contentRT;
            scroll.horizontal = false;
            scroll.vertical = true;

            GameObject itemGO = new GameObject("Item", typeof(RectTransform));
            itemGO.transform.SetParent(contentGO.transform, false);
            itemGO.AddComponent<LayoutElement>().preferredHeight = 22f;
            Toggle toggle = itemGO.AddComponent<Toggle>();
            Image itemBg = itemGO.AddComponent<Image>();
            itemBg.color = Color.clear;
            toggle.targetGraphic = itemBg;
            itemText = AddDropdownText(itemGO.transform, "Item Label", "Option", TextAlignmentOptions.MidlineLeft);
            itemText.margin = new Vector4(6, 0, 6, 0);
            return templateRT;
        }

        private TextMeshProUGUI AddDropdownText(Transform parent, string name, string text, TextAlignmentOptions alignment)
        {
            GameObject labelGO = new GameObject(name, typeof(RectTransform));
            labelGO.transform.SetParent(parent, false);
            RectTransform rt = labelGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
            if (FontAsset != null) tmp.font = FontAsset;
            tmp.text = text;
            tmp.fontSize = 9.5f;
            tmp.color = TextColor;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.alignment = alignment;
            return tmp;
        }

        private void AddClearFilterButton(Transform parent)
        {
            GameObject buttonGO = new GameObject("ClearFilters", typeof(RectTransform));
            buttonGO.transform.SetParent(parent, false);
            buttonGO.AddComponent<LayoutElement>().preferredWidth = 70f;
            Image bg = buttonGO.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.16f, 0.95f);
            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = bg;
            AddDropdownText(buttonGO.transform, "Label", "Clear", TextAlignmentOptions.Center);
            button.onClick.AddListener(() =>
            {
                _bodyFilter = FilterAllBodies;
                _shipFilter = FilterAllShips;
                _cargoFilter = FilterAllCargo;
                RefreshRows();
            });
        }

        private void AddTitleRow(string text)
        {
            GameObject go = new GameObject("TitleRow", typeof(RectTransform));
            go.transform.SetParent(ContentParent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 28f;
            TextMeshProUGUI lbl = go.AddComponent<TextMeshProUGUI>();
            if (FontAsset != null) lbl.font = FontAsset;
            lbl.text = text;
            lbl.fontSize = 12f;
            lbl.fontStyle = FontStyles.Bold;
            lbl.color = Color.white;
            lbl.enableWordWrapping = false;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            lbl.margin = new Vector4(6, 4, 6, 0);
        }

        private void AddSectionSeparator(string label)
        {
            GameObject sep = new GameObject("SectionSep", typeof(RectTransform));
            sep.transform.SetParent(ContentParent, false);
            sep.AddComponent<LayoutElement>().preferredHeight = 1f;
            sep.AddComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f, 1f);

            GameObject go = new GameObject("SectionLabel", typeof(RectTransform));
            go.transform.SetParent(ContentParent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 20f;
            TextMeshProUGUI lbl = go.AddComponent<TextMeshProUGUI>();
            if (FontAsset != null) lbl.font = FontAsset;
            lbl.text = label;
            lbl.fontSize = 10f;
            lbl.fontStyle = FontStyles.Bold;
            lbl.color = SectionColor;
            lbl.enableWordWrapping = false;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            lbl.margin = new Vector4(6, 2, 6, 0);
        }

        private void AddHeaderRow(params ColumnSpec[] columns)
        {
            GameObject rowGO = MakeRowContainer("HeaderRow", 18f);
            foreach (ColumnSpec col in columns)
                AddColumn(rowGO.transform, col.Width, col.Flex, col.Align, col.Text, col.MinWidth).color = HeaderColor;
        }

        private void AddMessageRow(string text)
        {
            GameObject go = new GameObject("MsgRow", typeof(RectTransform));
            go.transform.SetParent(ContentParent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 22f;
            TextMeshProUGUI lbl = go.AddComponent<TextMeshProUGUI>();
            if (FontAsset != null) lbl.font = FontAsset;
            lbl.text = text;
            lbl.fontSize = 11f;
            lbl.color = MutedColor;
            lbl.enableWordWrapping = false;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            lbl.margin = new Vector4(6, 0, 6, 0);
        }

        private GameObject MakeRowContainer(string name, float height)
        {
            GameObject rowGO = new GameObject(name, typeof(RectTransform));
            rowGO.transform.SetParent(ContentParent, false);
            rowGO.AddComponent<LayoutElement>().preferredHeight = height;
            HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 4f;
            hlg.padding = new RectOffset(6, 6, 0, 0);
            return rowGO;
        }

        private TextMeshProUGUI AddColumn(Transform parent, float preferredWidth, float flexibleWidth,
            TextAlignmentOptions align, string text, float minWidth = -1f)
        {
            GameObject go = new GameObject("Col", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            if (minWidth >= 0f) le.minWidth = minWidth;
            le.preferredWidth = preferredWidth;
            le.flexibleWidth = flexibleWidth;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (FontAsset != null) tmp.font = FontAsset;
            tmp.text = text;
            tmp.fontSize = 10.5f;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void MakeClickable(GameObject rowGO, Action onClick)
        {
            Image img = rowGO.GetComponent<Image>() ?? rowGO.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            Button btn = rowGO.GetComponent<Button>() ?? rowGO.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        private void OpenObject(ObjectInfo oi)
        {
            if (oi == null) return;
            try { UIManager.Instance.Open(EWindowType.ObjectInfo, oi); }
            catch (Exception e) { TrackerLog.LogWarning($"[FT] object click: {e.Message}"); }
        }

        private void OpenMission(MissionInfo mi, Spacecraft fallback)
        {
            if (mi == null)
            {
                OpenSpacecraft(fallback);
                return;
            }

            try
            {
                object windowType = FindWindowType("MissionInfo", "Mission", "Missions");
                if (windowType != null && InvokeOpenWindow(windowType, mi)) return;

                TrackerLog.LogWarning("[FT] mission click: mission window type not found");
            }
            catch (Exception e)
            {
                TrackerLog.LogWarning($"[FT] mission click: {e.Message}");
            }

            OpenSpacecraft(fallback);
        }

        private void OpenSpacecraft(Spacecraft sc)
        {
            if (sc == null) return;
            try { UIManager.Instance.Open(EWindowType.SpaceCraftInfo, sc); }
            catch (Exception e) { TrackerLog.LogWarning($"[FT] ship click: {e.Message}"); }
        }

        private static object FindWindowType(params string[] names)
        {
            foreach (string name in names)
            {
                try
                {
                    if (Enum.IsDefined(typeof(EWindowType), name))
                        return Enum.Parse(typeof(EWindowType), name);
                }
                catch { }
            }
            return null;
        }

        private static bool InvokeOpenWindow(object windowType, object payload)
        {
            UIManager ui = UIManager.Instance;
            if (ui == null || windowType == null || payload == null) return false;

            Type uiType = ui.GetType();
            foreach (MethodInfo method in uiType.GetMethods(AnyMember))
            {
                if (!string.Equals(method.Name, "Open", StringComparison.OrdinalIgnoreCase)) continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2) continue;
                if (!parameters[0].ParameterType.IsInstanceOfType(windowType)) continue;
                if (!parameters[1].ParameterType.IsInstanceOfType(payload) && parameters[1].ParameterType != typeof(object)) continue;

                method.Invoke(ui, new[] { windowType, payload });
                return true;
            }
            return false;
        }

        private void UpdateIndicator(int shipCount)
        {
            if (IndicatorLabel == null) return;
            IndicatorLabel.text = shipCount > 0
                ? $"<color=#55D5FF>●</color>  FLEETS <color=#888888>{shipCount}</color>"
                : "<color=#55D5FF>●</color>  FLEETS";
        }

        private static ColumnSpec Col(string text, float width, float flex, TextAlignmentOptions align, float minWidth = -1f)
            => new ColumnSpec { Text = text, Width = width, Flex = flex, Align = align, MinWidth = minWidth };

        private static string FormatBody(string spriteName, string name)
        {
            string icon = !string.IsNullOrEmpty(spriteName) ? $"<sprite name={spriteName}> " : "";
            return $"{icon}{name}";
        }

        private static string FormatShip(string spriteName, string name)
        {
            string icon = !string.IsNullOrEmpty(spriteName) ? $"<sprite name={spriteName}> " : "";
            return $"{icon}{name}";
        }

        private static string FormatShipIconCounts(IEnumerable<ShipIconCount> ships)
        {
            List<string> parts = new List<string>();
            foreach (ShipIconCount ship in ships)
                parts.Add(FormatShipIconCount(ship.SpriteName, ship.Count));
            return parts.Count > 0 ? string.Join("  ", parts) : "<color=#777777>?</color>";
        }

        private static string FormatShipIconCount(string spriteName, int count)
        {
            string icon = !string.IsNullOrEmpty(spriteName)
                ? $"<sprite name={spriteName}>"
                : "<color=#777777>?</color>";
            return $"{icon}<color=#A8A8A8>{Mathf.Max(1, count)}</color>";
        }

        private static void AddShipCount(List<ShipIconCount> ships, string displayName, string spriteName)
        {
            string key = $"{displayName}\u001f{spriteName}";
            ShipIconCount existing = ships.FirstOrDefault(s => s.Key == key);
            if (existing == null)
            {
                existing = new ShipIconCount
                {
                    Key = key,
                    DisplayName = displayName,
                    SpriteName = spriteName,
                    Count = 0
                };
                ships.Add(existing);
            }
            existing.Count++;
        }

        private static string GetSpacecraftTypeName(object spacecraftType, string fallback)
        {
            string name = ReadDisplayName(spacecraftType);
            if (!string.IsNullOrEmpty(name)) return name;
            return string.IsNullOrEmpty(fallback) ? "Spacecraft" : fallback;
        }

        private static string ReadDisplayName(object obj)
        {
            if (obj == null) return "";
            if (obj is string s) return s;

            foreach (string name in new[] { "Name", "NameRocketType", "ObjectName", "SpaceCraftName", "spaceCraftName", "spacecraftName" })
            {
                string value = ReadStringMember(obj, name);
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return "";
        }

        private static string ReadStringMember(object obj, params string[] names)
        {
            foreach (string name in names)
            {
                object value;
                if (name.StartsWith("Get", StringComparison.Ordinal))
                    value = InvokeMember(obj, name);
                else
                    value = ReadMemberValue(obj, name);
                if (value != null) return value.ToString();
            }
            return "";
        }

        private static object ReadMemberValue(object obj, params string[] names)
        {
            if (obj == null) return null;
            Type type = obj.GetType();
            foreach (string name in names)
            {
                try
                {
                    PropertyInfo prop = type.GetProperty(name, AnyMember);
                    if (prop != null && prop.GetIndexParameters().Length == 0)
                        return prop.GetValue(obj, null);
                }
                catch { }
                try
                {
                    FieldInfo field = type.GetField(name, AnyMember);
                    if (field != null) return field.GetValue(obj);
                }
                catch { }
            }
            return null;
        }

        private static object InvokeMember(object obj, string name, params object[] args)
        {
            if (obj == null) return null;
            Type type = obj.GetType();
            try
            {
                foreach (MethodInfo method in type.GetMethods(AnyMember))
                {
                    if (!string.Equals(method.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != args.Length) continue;
                    return method.Invoke(obj, args);
                }
            }
            catch { }
            return null;
        }

        private static bool ReadBoolMember(object obj, params string[] names)
        {
            object value = ReadMemberValue(obj, names);
            if (value is bool b) return b;
            if (value == null) return false;
            try { return Convert.ToBoolean(value, CultureInfo.InvariantCulture); }
            catch { return false; }
        }

        private static double ToDouble(object value)
        {
            if (value == null) return 0;
            if (value is double d) return d;
            if (value is float f) return f;
            if (value is int i) return i;
            if (value is long l) return l;
            try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static long ToLong(object value)
        {
            if (value == null) return 0;
            if (value is long l) return l;
            if (value is int i) return i;
            try { return Convert.ToInt64(value, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static DateTime? ToDateTime(object value)
        {
            if (value is DateTime dt && dt != default(DateTime)) return dt;
            return null;
        }

        private static string FormatDate(DateTime? date, DateTime? now)
        {
            if (!date.HasValue) return "?";
            string text = date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (!now.HasValue) return text;

            double days = (date.Value - now.Value).TotalDays;
            if (days > 0.5) return $"{text} <color=#777777>{days:F0}d</color>";
            if (days >= -0.5) return $"{text} <color=#777777>today</color>";
            return $"{text} <color=#777777>{Math.Abs(days):F0}d ago</color>";
        }

        private static string FormatTons(double tons)
        {
            if (tons >= 1_000_000) return $"{tons / 1_000_000:F1}MT";
            if (tons >= 1_000) return $"{tons / 1_000:F1}KT";
            if (tons < 10) return $"{tons:F1}T";
            return $"{tons:F0}T";
        }

        private static string FormatPercent(double progress)
        {
            double normalized = progress <= 1.0 ? progress * 100.0 : progress;
            return $"{Mathf.Clamp((float)normalized, 0f, 100f):F0}%";
        }

        private static string Plural(int count, string singular, string plural)
            => count == 1 ? singular : plural;

        private static T Safe<T>(Func<T> getter)
        {
            try { return getter(); }
            catch { return default(T); }
        }

        private struct ColumnSpec
        {
            public string Text;
            public float Width;
            public float Flex;
            public float MinWidth;
            public TextAlignmentOptions Align;
        }

        private sealed class FleetSnapshot
        {
            public string Message;
            public int TotalShips;
            public int TotalMissions;
            public int TotalConstruction;
            public readonly List<BodyFleetGroup> AtBodies = new List<BodyFleetGroup>();
            public readonly List<MissionFleetRow> InTransit = new List<MissionFleetRow>();
            public readonly List<MissionFleetRow> Planned = new List<MissionFleetRow>();
            public readonly List<ConstructionFleetRow> Construction = new List<ConstructionFleetRow>();
        }

        private sealed class BodyFleetGroup
        {
            public ObjectInfo Body;
            public string BodyName;
            public string BodySpriteName;
            public readonly List<ShipIconCount> Ships = new List<ShipIconCount>();
            public int Count => Ships.Sum(s => s.Count);
        }

        private sealed class ShipIconCount
        {
            public string Key;
            public string DisplayName;
            public string SpriteName;
            public int Count;
        }

        private sealed class MissionFleetRow
        {
            public MissionInfo Mission;
            public ObjectInfo Origin;
            public ObjectInfo Target;
            public string OriginName;
            public string TargetName;
            public string OriginSpriteName;
            public string TargetSpriteName;
            public List<ShipIconCount> Ships = new List<ShipIconCount>();
            public DateTime? Start;
            public DateTime? Arrival;
            public string Cargo;
            public List<string> CargoLabels = new List<string>();
            public int ShipCount => Ships.Sum(s => s.Count);
            public Spacecraft OpenTarget;
        }

        private sealed class ConstructionFleetRow
        {
            public ObjectInfo Body;
            public string BodyName;
            public string BodySpriteName;
            public string ShipTypeName;
            public string ShipSpriteName;
            public DateTime? Finish;
            public string Status;
            public int Count;
        }
    }

    internal class ResizeHandle : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        IDragHandler
    {
        internal RectTransform PanelRT;
        private const float MinHeight = 220f;
        private const float MinWidth = 700f;
        private static Texture2D _cursor;
        private Canvas _canvas;
        private bool _dragging;
        private Vector2 _dragStartScreen;
        private Vector2 _dragStartSize;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_cursor == null) _cursor = BuildCursor();
        }

        public void OnPointerEnter(PointerEventData e) =>
            Cursor.SetCursor(_cursor, new Vector2(16, 16), CursorMode.Auto);

        public void OnPointerExit(PointerEventData e)
        {
            if (!_dragging) Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        public void OnPointerDown(PointerEventData e)
        {
            _dragging = true;
            _dragStartScreen = e.position;
            _dragStartSize = PanelRT.sizeDelta;
        }

        public void OnPointerUp(PointerEventData e)
        {
            _dragging = false;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        public void OnDrag(PointerEventData e)
        {
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            Vector2 delta = (e.position - _dragStartScreen) / scale;
            float width = Mathf.Max(MinWidth, _dragStartSize.x + delta.x);
            float height = Mathf.Max(MinHeight, _dragStartSize.y - delta.y);
            PanelRT.sizeDelta = new Vector2(width, height);
        }

        private static Texture2D BuildCursor()
        {
            const int S = 32;
            const int cx = 15;
            Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            Color[] px = new Color[S * S];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            void Dot(int x, int y, Color c) { if (x >= 0 && x < S && y >= 0 && y < S) px[y * S + x] = c; }
            void Line(int x, bool outline)
            {
                Color core = Color.white;
                Color ol = Color.black;
                for (int y = 9; y < S - 9; y++) Dot(x, y, outline ? ol : core);
            }

            Line(cx - 1, true); Line(cx + 1, true); Line(cx, false);

            for (int i = 0; i < 6; i++)
            {
                int y = S - 3 - i;
                for (int x = cx - i; x <= cx + i; x++) Dot(x, y, Color.white);
                Dot(cx - i - 1, y, Color.black);
                Dot(cx + i + 1, y, Color.black);
            }
            for (int x = cx - 1; x <= cx + 1; x++) Dot(x, S - 2, Color.black);

            for (int i = 0; i < 6; i++)
            {
                int y = 2 + i;
                for (int x = cx - i; x <= cx + i; x++) Dot(x, y, Color.white);
                Dot(cx - i - 1, y, Color.black);
                Dot(cx + i + 1, y, Color.black);
            }
            for (int x = cx - 1; x <= cx + 1; x++) Dot(x, 1, Color.black);

            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }

    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
    }
}
