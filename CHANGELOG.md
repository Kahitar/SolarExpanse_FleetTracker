# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]
### Changed
- Removed the edit `/` button from cyclical mission overview rows (was not functional).

### Fixed
- Fixed cyclical mission overview ship counts showing one extra spacecraft.

## [1.3.2] - 2026-05-16
### Changed
- Moved ObjectInfo list expansion out of FleetTracker and into the standalone UXTweaks mod.

## [1.3.1] - 2026-05-15
### Changed
- Split ObjectInfo list expansion into a separate `ObjectInfoListExpansion.dll` built and packaged alongside `FleetTracker.dll`.

## [1.3.0] - 2026-05-15
### Added
- Added a FleetTracker cyclical missions tab with icon-first filters and management buttons.

### Changed
- Changed FleetTracker filter controls to use compact icon prefixes and an icon clear button.
- Changed cyclical mission cargo lanes to show wait-for-full/take-available mode icons and crew cargo count when present.
- Moved cyclical mission cargo lanes onto the route title line and replaced A/B lane labels with body icons.

### Fixed
- Fixed cyclical mission cargo lanes showing as empty when the game stores them as cyclical cargo resource/module definitions.

## [1.2.0] - 2026-05-14
### Added
- Fleet filters for body, ship type, and cargo.
- Object overview list expansion for long fleet and object panels.

### Changed
- Grouped ships at bodies into one row per body with icon-count ship summaries.
- Changed transit and planned mission rows to use ship icons in the ships column.
- Changed mission cargo display to use resource icons instead of cargo name text.
- Positioned the Fleets button left of LifeSupportTracker and opened the panel directly under the Fleets button.
- Improved dropdown usability, including keeping dropdowns open during refresh and increasing dropdown height.

## [1.0.0] - 2026-05-12
### Added
- Floating panel showing fleet status grouped by ships at bodies, ships in transit, planned missions, and ships in construction.
- Cargo manifests for active and planned missions.
- Construction completion estimates when the game exposes production queue timing.
- Draggable, resizable panel that clamps to screen bounds on resize.
- Distinct plugin ID, Harmony ID, assembly name, and UI object names so the original LifeSupportTracker mod can run alongside it.
