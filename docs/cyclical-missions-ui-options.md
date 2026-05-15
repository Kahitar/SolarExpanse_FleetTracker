# Cyclical Missions UI Options

Task: Yeoman #632  
Scope: analysis only; no game UI code changed.

## Clarification

The primary target is the game's native cyclical mission overview side panel/tab. The goal is to understand how FleetTracker could make that existing native overview easier to read. A fully separate FleetTracker panel remains an option, but it is not the preferred first assumption.

## Goal

The current native cyclical missions overview is hard to scan. FleetTracker can improve it by adapting the existing game panel, replacing its contents at runtime, or adding a separate companion overview if the native panel proves too brittle.

## What we know from the existing modding notes

- FleetTracker is a BepInEx/Harmony mod and can patch game methods at runtime.
- Harmony can patch native game UI methods once the relevant cyclical mission tab/window classes are identified.
- Existing game UI objects can be cloned for styling; this should help preserve the look and scale of the native side panel.
- Runtime mission data is reachable through game managers and spacecraft/company objects.
- Cyclical mission data is referenced by these known members/fields:
  - `Company.LoadCyclicalMission(...)`
  - save field `listMissionCyclical`
  - `Spacecraft.CycleMissionsData`
  - `Spacecraft.CraftCyclicalMissionController`
  - `PMMissionParameter.ForCyclicalMission`
  - `PMMissionParameter.CycleMissionsDataData`
  - cyclical mission fields seen in saves: `scIDList`, `A`, `B`, `Pause`, `Ends`, `TransferType`, `CargoStart`, `CargoEnd`, `CountMission`, `CountMax`

## Technical feasibility answer

Short answer after metadata inspection: yes, Option 1 is viable. The game has specific native cyclical mission list and row classes with stable-looking patch targets.

- The side-panel-sized mockup is feasible because FleetTracker already creates custom Unity UI at runtime: cloned game panel background, `RectTransform` sizing, scroll view, `VerticalLayoutGroup`, `ContentSizeFitter`, buttons, TextMeshPro labels, and reflection-safe data reads.
- Building that layout inside the native cyclical mission panel should be possible if we can reliably locate the panel/content container when the native tab opens.
- Option 1, patching native rows in place, is technically possible in principle with Harmony, but only confirmed after inspecting the exact native UI classes/methods. It depends on whether the game exposes a stable row rebuild method, row prefab, or predictable transform hierarchy.
- If the native rows are not stable, Option 2 is the more reliable way to achieve the same visual result: keep the native panel shell and replace only its content area with our own side-panel-sized scroll list.

Practical confidence:

| Item | Feasibility | Confidence | Notes |
| --- | --- | --- | --- |
| Mockup layout as Unity UI | Technically feasible | High | Existing FleetTracker UI code already uses the required Unity UI primitives. |
| Same layout inside native panel bounds | Technically feasible | High | Metadata shows dedicated native list/row classes and containers. |
| Patch native rows in place | Viable | Medium-high | Public `SetData(CycleMissionsData)` row methods are stable Harmony targets. |
| Preserve native edit/cancel actions | Plausible | Medium | `MissionRowCyclicalNew` has native `edit`, `delete`, `pause`, and `play` buttons; preserve rather than recreate them. |

## Native UI metadata inspection findings

Inspection method:

- Read `Assembly-CSharp.dll` through a temporary .NET metadata inspector from inside `SolarExpanse_FleetTracker`.
- Removed the temporary inspector after use; only these findings remain documented.

Relevant native classes found:

| Class | Relevant members | Meaning for Option 1 |
| --- | --- | --- |
| `Game.UI.Windows.Windows.MissionsWindow` | private `cycleMissionAllList`, private `cyclicalMission`, `RefreshUI()`, `ShowCyclicalMission(CycleMissionsData)`, `SetData(object)`, `Show()` | The native missions window owns a dedicated cyclical mission list and exposes refresh/show lifecycle methods that can be patched. |
| `Game.UI.Windows.Elements.MissionsElements.CycleMissionAllList` | private `missionRowCyclicalPrefab`, `missionRowCyclicalPrefabNew`, `parentForPrefab`, `parentForPrefabNew`, `grid`, `gridNew`, `listMRC`, `listMRCNew`, public `ShowCyclicalMission(CycleMissionsData)`, public `OnEnable()` | This is the strongest Option 1 target. It owns the row prefabs, row parent containers, and active row lists. |
| `Game.UI.Windows.Elements.MissionsElements.MissionRowCyclical` | private label fields `a`, `b`, `cargoStart`, `cargoEnd`, `countMission`, `ends`, `lvA`, `lvB`, `sc`, `transferType`; private `buttonDelete`; public `SetData(CycleMissionsData)` | Legacy/native row has direct fields for the dense overview labels. A postfix on `SetData` could reformat text or alter child layout. |
| `MissionRowCyclicalNew` | private `AtoB`, `BtoA`, `delete`, `edit`, `pause`, `play`, `imageShowCyclicalMission`; public `SetData(CycleMissionsData)`, public `ShowCyclicalMission()` | Newer native row already models A→B/B→A directions and has edit/delete/pause/play buttons. This is likely the current best row-level target. |
| `MissionRowNew` | fields `sourceText`, `destinationText`, `resourceText`, `actionBtn`, `cycleMissionRepeating`; public `SetMissionInfo(...)`; property `CycleMissionsData` | Direction sub-rows can likely be adjusted if `MissionRowCyclicalNew` delegates A→B/B→A rendering to them. |
| `Game.UI.Windows.Elements.PlanMissionElements.CycleMissionsData` | properties `A`, `B`, `ListSC`, `CountSC`, `CountMission`, `Pause`, `TransferType`, `CargoStart`, `CargoEnd`, `Ends`, `EndsData`, `cargoAllStart`, `cargoAllEnd`, `MissionName()` | Runtime data needed by the mockup is available from the row's `CycleMissionsData`. |
| `Game.UI.Windows.Elements.PlanMissionElements.CycleMissionManager` | public `GetAllCycleMission()`, `GetAllCycleMission(Company)`, `GetSCNameFrom(CycleMissionsData)`, `GetSCFrom(CycleMissionsData)`, `RemoveCycleMission(...)`, `RefreshAfterEdit(...)` | Provides a data source and native management actions if row reconstruction needs to query all cyclical missions. |

Option 1 viability conclusion:

- Viable: yes.
- Confidence: medium-high for read-only visual improvement.
- Best technical target: patch `CycleMissionAllList` and/or `MissionRowCyclicalNew.SetData(CycleMissionsData)`.
- Safest first implementation: postfix row setup after native `SetData`, reuse existing row objects/buttons, and only adjust visual hierarchy/text.
- Higher-risk implementation: fully replace instantiated native rows. This is possible because `CycleMissionAllList` owns prefabs and parent containers, but it risks breaking edit/delete/pause/play unless those controls are carefully preserved.

Recommended patch strategy for Option 1:

1. Patch `MissionRowCyclicalNew.SetData(CycleMissionsData)` with a Harmony postfix.
2. Read the row's private `cmd`, `AtoB`, `BtoA`, `edit`, `delete`, `pause`, and `play` fields by reflection.
3. Reformat the existing row into a compact card, preserving native buttons and their existing listeners.
4. Use `CycleMissionsData` properties for route/status/count/cargo summary.
5. Add a small marker component/name to avoid rebuilding the same row repeatedly.
6. If the active game uses the legacy row instead, apply the same pattern to `Game.UI.Windows.Elements.MissionsElements.MissionRowCyclical.SetData(CycleMissionsData)`.
7. Patch `CycleMissionAllList.OnEnable()` or `ShowCyclicalMission(...)` only if row-level postfixes are not enough to trigger refresh/reflow.

Remaining unknowns before implementation:

- Whether the running game uses `MissionRowCyclicalNew` or the legacy `MissionRowCyclical` for the current UI state (`CycleMissionAllList` has a private `newVersion` flag).
- Exact transform names/sizes of the row prefab at runtime.
- Whether TextMeshPro auto-size/layout components fight custom sizing; this can be handled during prototype by adding/adjusting layout components after `SetData`.

## Option 1: Patch the existing native side panel in place

Patch the game's cyclical mission side panel/tab and change its generated rows in place.

Possible changes:

- Keep the native panel dimensions and entry point.
- Replace dense text with compact mission cards.
- Show one route per card: `A ↔ B`.
- Use short status labels: Active, Paused, Ending, Done.
- Show assigned craft count/names in a secondary line.
- Compress cargo to two small lanes: `A→B` and `B→A`.
- Keep native edit/cancel buttons if they are part of each row and can be preserved safely.

Pros:

- Matches the user's intent most directly: improve the existing in-game overview.
- Keeps the feature where players already expect it.
- Can preserve native actions and workflows.
- Avoids adding another top-level FleetTracker UI surface.

Cons / risks:

- Requires identifying exact game UI classes and rebuild methods in `Assembly-CSharp.dll`.
- More fragile across game updates because private UI hierarchy and row names can change.
- The native side-panel width limits the amount of information visible at once.
- Mistakes in a core tab can break the original cyclical mission workflow.

Best use:

- Preferred path. Metadata inspection found dedicated cyclical mission list/row classes and public row `SetData(CycleMissionsData)` methods, which are suitable Harmony patch targets.

## Option 2: Replace the native panel content at runtime

Leave the native side-panel shell and tab opening behavior intact, but hide/remove the original row content and inject a new scrollable overview inside the same panel bounds.

Possible changes:

- Locate the native cyclical mission panel after it opens.
- Disable the original content container.
- Add a custom scroll view sized to the native side panel.
- Reuse native fonts/colors/buttons where possible.
- Keep read-only status first; reintroduce native edit actions only after method targets are confirmed.

Pros:

- Still feels like the native cyclical mission overview, and remains a practical fallback.
- Gives more layout control than patching individual original rows.
- Can be designed specifically for side-panel constraints.
- Lower risk than modifying many original row components individually.

Cons / risks:

- Still depends on finding the panel transform reliably.
- Native edit controls may be harder to preserve.
- Requires careful cleanup to avoid duplicate injected content on reopen/reload.

Best use:

- Good fallback if the original row layout is too messy but the panel container is stable.

## Option 3: Add a separate FleetTracker overview panel

Create a separate FleetTracker panel for cyclical missions, opened from FleetTracker UI or a distinct button.

Possible changes:

- Build a larger custom overview outside the native side-panel constraints.
- Read cyclical mission state from player/company/spacecraft data by reflection.
- Keep the original game tab untouched and use the new panel as a clearer dashboard.

Pros:

- Lowest risk to the game's own mission UI.
- Full control over information density and layout.
- Fits the current FleetTracker architecture: injected button/panel, cloned game styling, reflection-safe data reads.
- Can be developed incrementally as a read-only dashboard.

Cons / risks:

- Does not directly fix the native side panel.
- Duplicates information from the original tab.
- Requires a separate entry point and may feel disconnected from mission editing.

Best use:

- Safety fallback if the native panel cannot be patched reliably.

## Recommendation

Proceed in this order:

1. Implement Option 1 first by patching `MissionRowCyclicalNew.SetData(CycleMissionsData)` and, if needed, legacy `MissionRowCyclical.SetData(CycleMissionsData)`.
2. If row-level patches fight the prefab layout too much, replace the native panel's content area while keeping the native shell/entry point.
3. Only build a separate FleetTracker overview if native-panel modification is unsafe or disproportionately expensive.

## Recommended next discovery step

1. Inspect `Assembly-CSharp.dll` metadata/strings for cyclical mission UI class names and methods.
2. Find the native panel/tab GameObject path at runtime or through metadata.
3. Determine whether the panel has a stable row rebuild method, content container, or prefab row.
4. Identify runtime shape of cyclical mission objects (`CycleMissionsData`, controller, company list) using reflection-safe probes.
5. Prototype a side-panel-sized read-only row/card layout before touching edit actions.

## Candidate side-panel row fields

Minimum useful card:

- Route: source and destination names (`A ↔ B`)
- Status: active / paused / ending / complete
- Assigned spacecraft count and compact names
- Mission count: `CountMission / CountMax` when `CountMax` is present
- Cargo summary from `CargoStart` and `CargoEnd`

Nice-to-have later:

- Transfer type
- Next departure / arrival if exposed by controller or associated mission info
- Warnings for missing craft, unavailable cargo, invalid endpoints, paused routes
- Compact filters for Active, Paused, Ending, Completed
- Safe native edit/cancel buttons, if the game methods can be called without bypassing validation
