# macOS Development Setup

This repo is configured to build a Windows/CrossOver Solar Expanse BepInEx mod
from macOS. The output DLL is managed .NET IL, so it can be compiled on macOS
as long as the build references the exact game and BepInEx assemblies from the
CrossOver bottle.

## Required Tools

Install mise, then install the repo tools:

```bash
mise install
```

The repo currently pins:

- `dotnet = "8"`

Optional but useful tools:

```bash
brew install git ripgrep gh
dotnet tool install --global ilspycmd
```

`gh` is only needed for `mise run release`.

## Game Path

The build expects `SOLAR_EXPANSE_ROOT` to point at the Solar Expanse game
root inside the CrossOver bottle. The default macOS path is defined in `.mise.toml`:

```text
~/Library/Application Support/CrossOver/Bottles/Steam/drive_c/Program Files (x86)/Steam/steamapps/common/Solar Expanse
```

If your bottle or Steam library differs, create `.mise.local.toml`:

```toml
[env]
SOLAR_EXPANSE_ROOT = "/absolute/path/to/Solar Expanse"
```

That local file is ignored by git.

The expected files under that root are:

- `BepInEx/core/BepInEx.dll`
- `BepInEx/core/0Harmony.dll`
- `Solar Expanse_Data/Managed/Assembly-CSharp.dll`
- Unity managed assemblies in `Solar Expanse_Data/Managed/`

## Common Commands

Build and copy the DLL into the CrossOver game folder:

```bash
mise run build
```

Build and create a distributable BepInEx zip:

```bash
mise run package
```

Create a GitHub release from the latest `CHANGELOG.md` version:

```bash
mise run release
```

Pass a specific version with or without the `v` prefix:

```bash
mise run release 1.0.0
mise run release v1.0.0
```

## Logs

After starting the game through CrossOver, follow BepInEx logs from macOS:

```bash
tail -f "$SOLAR_EXPANSE_ROOT/BepInEx/LogOutput.log"
```

## Notes

- `Microsoft.NETFramework.ReferenceAssemblies` is referenced by the project so
  `net472` can build on macOS without installing Windows targeting packs.
- The `.csproj` validates `SOLAR_EXPANSE_ROOT` before resolving references,
  so a wrong bottle path should fail with a direct error.
- `mise run build` copies `FleetTracker.dll` directly into
  `$SOLAR_EXPANSE_ROOT/BepInEx/plugins`.
