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

- Preferred path if inspection finds a stable row-building method or stable transform hierarchy for the cyclical mission panel.

## Option 2: Replace the native panel content at runtime

Leave the native side-panel shell and tab opening behavior intact, but hide/remove the original row content and inject a new scrollable overview inside the same panel bounds.

Possible changes:

- Locate the native cyclical mission panel after it opens.
- Disable the original content container.
- Add a custom scroll view sized to the native side panel.
- Reuse native fonts/colors/buttons where possible.
- Keep read-only status first; reintroduce native edit actions only after method targets are confirmed.

Pros:

- Still feels like the native cyclical mission overview.
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

1. Try to adapt the native cyclical mission side panel in place.
2. If original rows are too brittle, replace the native panel's content area while keeping the native shell/entry point.
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
