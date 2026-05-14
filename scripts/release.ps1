param([string]$Version)
$ErrorActionPreference = "Stop"
$name = "FleetTracker"

if (-not $Version) {
    $raw = (Select-String -Path "CHANGELOG.md" -Pattern '## \[(\d+\.\d+\.\d+)\]' |
        Select-Object -First 1).Matches.Groups[1].Value
    $Version = "v$raw"
}

$numericVersion = $Version.TrimStart('v')
Write-Host "Releasing $name $Version..."

& "$PSScriptRoot/package.ps1"

$zip = "dist/${name}_${Version}.zip"

$notes = [System.Collections.Generic.List[string]]::new()
$inSection = $false
foreach ($line in Get-Content "CHANGELOG.md") {
    if ($line -match "^## \[$([regex]::Escape($numericVersion))\]") { $inSection = $true; continue }
    if ($inSection -and $line -match "^## ") { break }
    if ($inSection) { $notes.Add($line) }
}

gh release create $Version $zip --title "$name $Version" --notes ($notes -join "`n") --target main
Write-Host "Done: https://github.com/jfmmm/SolarExpanse_FleetTracker/releases/tag/$Version"
