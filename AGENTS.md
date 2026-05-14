# AGENTS.md

Important repo-specific context for future agents.

## Project

- This is a BepInEx/Harmony mod for Solar Expanse.
- The plugin GUID and Harmony ID are `com.mod.solarexpanse.fleettracker`.
- The built DLL is `FleetTracker.dll`.
- Keep coexistence with the original LifeSupportTracker in mind: do not reuse its plugin GUID, Harmony ID, DLL name, injected Unity object names, or default button position.

## Docs

- macOS/CrossOver setup and commands: `docs/mac-development.md`
- Solar Expanse runtime/modding notes discovered so far: `docs/solar-expanse-modding-notes.md`

Read those before guessing game APIs or build setup.

## Build And Deploy

- Use mise tasks, not raw `dotnet build`, for normal local work.
- `.mise.toml` pins `dotnet = "8"` and exposes executable file tasks from `scripts/`.
- `mise run build` builds `FleetTracker.csproj` and copies the DLL into the game's `BepInEx/plugins` folder.
- `mise run package` builds and creates `dist/FleetTracker_v*.zip`.
- `mise run release [version]` packages and creates a GitHub release; it requires `gh`.

## Game Path

- The project resolves game/BepInEx references through `SOLAR_EXPANSE_ROOT`.
- `SOLAR_EXPANSE_ROOT` should always point at the Solar Expanse game root, regardless of OS.
- The default value in `.mise.toml` is for macOS development with the Windows game installed in a CrossOver Steam bottle:
  `~/Library/Application Support/CrossOver/Bottles/Steam/drive_c/Program Files (x86)/Steam/steamapps/common/Solar Expanse`
- If the local game path differs, including on Windows, create `.mise.local.toml` or set the environment variable before building:

```toml
[env]
SOLAR_EXPANSE_ROOT = "/absolute/path/to/Solar Expanse"
```

- `.mise.local.toml` is intentionally gitignored.

## Release Versioning

- Unless the user specifies otherwise, increment the middle version for releases: `1.X.0`.
- Update the changelog/version before packaging or creating a release.

## Runtime Notes

- Normal BepInEx plugin DLLs are loaded at game startup. Restart Solar Expanse after rebuilding.
- Do not assume hot reload is safe unless cleanup is added for Harmony patches and injected UI objects.
- The active UI injection point is `NotificationManager.Awake`.
- The mod currently injects `modFleetTrackerButton` and `modFleetTrackerPanel`.
