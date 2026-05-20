# AGENTS.md

This project is a BepInEx/Harmony mod for Solar Expanse.

## Project Lineage

- FleetTracker was originally based on LifeSupportTracker.
- When investigating FleetTracker bugs or performance issues, it can be useful to compare against LifeSupportTracker's implementation and git history for fixes that may also apply here.

## Release Process

To release this mod:

1. Update the mod version.
   - Update the `BepInPlugin` version in `Plugin.cs`.
   - Update `CHANGELOG.md` for the release.
2. Create the release tag for the version.
3. Run the build task:
   ```bash
   mise run build
   ```
4. Run the package task:
   ```bash
   mise run package
   ```
5. Run the release task:
   ```bash
   mise run release
   ```
