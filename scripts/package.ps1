$ErrorActionPreference = "Stop"
$name = "FleetTracker"

dotnet build -c Release
if ($LASTEXITCODE -ne 0) { exit 1 }

$version = (Select-String -Path "CHANGELOG.md" -Pattern '## \[(\d+\.\d+\.\d+)\]' |
    Select-Object -First 1).Matches.Groups[1].Value

$dist = "dist"
Remove-Item -Recurse -Force $dist -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force "$dist/BepInEx/plugins" | Out-Null
Copy-Item "bin/Release/net472/$name.dll" "$dist/BepInEx/plugins/"

$zip = "$dist/${name}_v${version}.zip"
Compress-Archive -Path "$dist/BepInEx" -DestinationPath $zip -Force
Remove-Item -Recurse -Force "$dist/BepInEx"
Write-Host "Packaged: $zip"
