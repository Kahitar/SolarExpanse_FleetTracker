# FleetTracker

A fleet overview mod for Solar Expanse.

## Features

- **Fleet overview button:** adds a draggable Fleets button and panel in the game UI.
- **See where everything is:** groups your spacecraft by ships parked at bodies, ships in flight, planned missions, and ships being built.
- **Cargo at a glance:** shows cargo for active and planned missions without opening each spacecraft one by one.
- **Construction tracking:** lists ships under construction and shows finish timing when the game exposes enough information.
- **Cyclical mission view:** shows repeating routes, cargo lanes, wait/take modes, and mission management actions in one place.
- **Useful filters:** narrow the fleet list by body, ship type, cargo, and mission state.

## Installation

1. Install BepInEx for Solar Expanse first. Use the official BepInEx installation guide:
   <https://docs.bepinex.dev/master/articles/user_guide/installation/index.html>
2. Download the latest `FleetTracker_v*.zip` from the FleetTracker releases page:
   <https://github.com/Kahitar/SolarExpanse_FleetTracker/releases/latest>
3. Open your Solar Expanse game folder.
4. Extract the zip into the game folder so `FleetTracker.dll` ends up here:

```text
Solar Expanse/BepInEx/plugins/FleetTracker.dll
```

5. Restart Solar Expanse.

## Build

Run from this directory:

```bash
mise run build
```

The build copies `FleetTracker.dll` into `$SOLAR_EXPANSE_ROOT/BepInEx/plugins`.

## Package

```bash
mise run package
```

This creates `dist/FleetTracker_v*.zip`.
