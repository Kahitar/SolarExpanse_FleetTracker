# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]
### Added
- Added a FleetTracker cyclical missions tab with icon-first filters and management buttons.

### Changed
- Changed FleetTracker filter controls to use compact icon prefixes and an icon clear button.

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
