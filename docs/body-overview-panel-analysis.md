# Body overview panel: removing nested list scroll bars

## Question

Can the body/object overview sub-panels such as **Facilities** and **Resources** show all rows instead of being capped at two rows with their own scrollbar?

## Finding

Yes. The restriction is not hard-coded into the row creation logic. It is controlled by the shared game UI base class:

- `Game.UI.Windows.Elements.UIList<T, TData>`
- private fields: `scrollView`, `maxRows`, `itemsInARow`, `rowHeight`, `listViewExpanded`
- method: `ConformSizeAndScrollbarsToVisibleContent()`

The relevant decompiled logic is:

```csharp
int visibleRows = Mathf.CeilToInt(activeRowCount / itemsInARow);
if (!listViewExpanded)
{
    scrollRectHeight = Mathf.Min(maxRows, visibleRows) * rowHeight;
}
scrollView.verticalScrollbar.gameObject.SetActive(scrollView.vertical);
```

The object/body overview window calls this sizing method for its sub-lists in `ObjectInfoWindow.RebuildLayout()`:

- `facilityList`
- `resourcesListExplore`
- `resourcesList`
- `rocketList`
- `launchVehicleList`
- `missionsList`

For the screenshot issue, the most relevant concrete list types are:

- `Game.UI.Windows.Elements.ObjectInfoElements.UIFacilityList`
- `Game.UI.Windows.Elements.ObjectInfoElements.UIResorcesList` (game spelling)
- `Game.UI.Windows.Elements.ObjectInfoElements.UIExploredResourcesList`

## Implementation approach

FleetTracker now adjusts the object overview list instances after the game populates/rebuilds them:

1. Patch `ObjectInfoWindow.RebuildLayout()` with a postfix.
2. For the target child `UIList<,>` components in that `ObjectInfoWindow`, reflect the private base fields.
3. Set `maxRows` to a high value.
4. Set `scrollView.vertical = false` and hide/disable the vertical scrollbar.
5. Set the scroll view `RectTransform` height to `activeRows * rowHeight`.
6. Call `LayoutRebuilder.ForceRebuildLayoutImmediate()` on the affected list/window root.

This keeps the outer `scrollRectAll` as the only scrollbar and removes the nested scrollbar behavior.

## Risk and notes

- This is feasible as a mod; no game asset editing appears necessary.
- Use reflection because the controlling fields are private on the generic `UIList<T, TData>` base class.
- The implemented patch is scoped to object overview lists under `Game.UI.Windows.Elements.ObjectInfoElements`.
- Very long lists will make the object overview content much taller. The outer body overview scrollbar remains necessary.
- `UIFacilityList.FrameActive()` and `UIResorcesList.FrameActive()` temporarily resize lists during drag/drop; the postfix should re-apply after rebuilds and may also need hooks on `FrameActive`/`FrameDeActive` if drag/drop exposes regressions.
- Implemented in `ObjectInfoListExpansion/ObjectInfoListExpansionPatch.cs` and built as `ObjectInfoListExpansion.dll`.

## Conclusion

The change is possible and implemented as a scoped Harmony/reflection patch. Test Facilities, Resources, Explored Resources, spacecraft, launch vehicle, and mission sections in-game.
