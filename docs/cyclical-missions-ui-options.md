# Cyclical Missions UI Options

Task: Yeoman #632  
Scope: analysis only; no game UI code changed.

## Goal

The current cyclical missions overview is hard to scan. FleetTracker can improve it either by adapting the game's existing cyclical mission UI or by adding a new companion overview panel.

## What we know from the existing modding notes

- FleetTracker is a BepInEx/Harmony mod and can patch game methods at runtime.
- The current reliable UI injection point is `Manager.NotificationManager.Awake`.
- Existing game UI objects can be cloned for styling; FleetTracker already uses this pattern for its own button and panel.
- Runtime mission data is reachable through game managers and spacecraft/company objects.
- Cyclical mission data is referenced by these known members/fields:
  - `Company.LoadCyclicalMission(...)`
  - save field `listMissionCyclical`
  - `Spacecraft.CycleMissionsData`
  - `Spacecraft.CraftCyclicalMissionController`
  - `PMMissionParameter.ForCyclicalMission`
  - `PMMissionParameter.CycleMissionsDataData`
  - cyclical mission fields seen in saves: `scIDList`, `A`, `B`, `Pause`, `Ends`, `TransferType`, `CargoStart`, `CargoEnd`, `CountMission`, `CountMax`

## Option 1: Patch or adapt the existing game tab

Patch the game's cyclical mission window/tab and change its generated rows in place.

Possible changes:

- Add clearer columns: route, assigned craft, state, cargo A→B, cargo B→A, cycle count, pause/end state.
- Group by route (`A ↔ B`) instead of showing every detail as equal-weight text.
- Add visual status badges such as Active, Paused, Ending, Completed.
- Add sorting/filtering if the original tab exposes row rebuild methods.

Pros:

- Keeps the feature where players already expect it.
- Can reuse the game's existing data flow and controls.
- Less duplicate UI if the original tab is structurally easy to patch.

Cons / risks:

- Requires identifying exact game UI classes and rebuild methods in `Assembly-CSharp.dll`.
- More fragile across game updates because private UI hierarchy and row names can change.
- Existing layout may constrain how much the overview can improve.
- Mistakes in a core tab can break the original cyclical mission workflow.

Best use:

- If inspection finds a small, stable row-building method for cyclical missions.

## Option 2: Add a new FleetTracker overview panel

Create a separate FleetTracker panel for cyclical missions, opened from a distinct button or from the existing FleetTracker panel.

Possible changes:

- Build a custom scrollable table with route cards or compact rows.
- Read cyclical mission state from player/company/spacecraft data by reflection.
- Keep the original game tab untouched and use the new panel as a clearer dashboard.
- Provide click-through actions later, such as opening craft/object info, if safe APIs are confirmed.

Pros:

- Lowest risk to the game's own mission UI.
- Full control over information density and layout.
- Fits the current FleetTracker architecture: injected button/panel, cloned game styling, reflection-safe data reads.
- Can be developed incrementally: read-only overview first, then interactions later.

Cons / risks:

- Duplicates some information from the original tab.
- Requires robust reflection/data normalization because cyclical mission runtime types are not fully documented yet.
- Any edit actions would need separate validation before implementation.

Best use:

- Recommended first implementation path: a read-only, custom overview panel.

## Option 3: Hybrid approach

Add a FleetTracker overview first, then optionally add a small affordance in the original cyclical mission tab later.

Possible changes:

- New overview panel provides the main readable summary.
- Existing tab remains the place for native editing/actions.
- A later patch can add an "Open Fleet overview" button near the original tab if a safe injection point is found.

Pros:

- Gives immediate readability improvement without risking mission editing.
- Leaves room to integrate with the native tab once its classes are known.
- Provides a fallback if the original tab is too brittle to modify directly.

Cons / risks:

- Two entry points may be slightly less elegant than one redesigned native tab.
- Still requires future discovery work for native-tab integration.

Best use:

- Best overall strategy: implement a standalone read-only overview first, then evaluate native-tab integration.

## Recommended next step

1. Inspect `Assembly-CSharp.dll` metadata/strings for cyclical mission UI class names and methods.
2. Identify runtime shape of cyclical mission objects (`CycleMissionsData`, controller, company list) using reflection-safe probes.
3. Implement a read-only FleetTracker overview using the mockup layout as the target.
4. Only after that, decide whether patching the original tab is worth the extra fragility.

## Candidate overview fields

Minimum useful row:

- Route: source and destination names (`A ↔ B`)
- Assigned spacecraft count and names
- Status: active / paused / ending / complete
- Transfer type
- Mission count: `CountMission / CountMax` when `CountMax` is present
- Cargo summary from `CargoStart` and `CargoEnd`

Nice-to-have later:

- Next departure / arrival if exposed by controller or associated mission info
- Warnings for missing craft, unavailable cargo, invalid endpoints, paused routes
- Filters for Active, Paused, Ending, Completed
- Click-through to involved object or spacecraft info windows
