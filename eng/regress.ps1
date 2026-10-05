<#
.SYNOPSIS
  Compiles mods with NeoRune 0.4 and with this compiler, and compares the generated packages byte for byte.

.DESCRIPTION
  Each mod must have been built once (its obj\...\neorune.rsp lists its sources and references). Differences are
  expected only where a change in the compiler meant them; anything else is a regression.

.PARAMETER Mods
  Mod project folders.

.PARAMETER Old
  NeoRune 0.4's compiler (neorune.dll from the NeoRune.Sdk package).
#>
param(
    [Parameter(Mandatory)][string[]]$Mods,
    [string]$Old = "$env:USERPROFILE\.nuget\packages\neorune.sdk\0.4.0\tools\neorune.dll",
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$new = "$root\src\NeoRuneExtended.Cli\bin\$Configuration\net10.0\neorunex.dll"
$work = "$root\artifacts\regress"
$failed = 0
# powershell -File passes a list as one comma-separated string.
$Mods = $Mods | ForEach-Object { $_ -split ',' } | Where-Object { $_ }

foreach ($mod in $Mods) {
    $name = Split-Path $mod -Leaf
    $rsp = Get-ChildItem "$mod\obj" -Recurse -Filter neorune.rsp -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $rsp) { Write-Host "$name : not built yet (no neorune.rsp)"; $failed++; continue }
    foreach ($v in 'old', 'new') {
        $out = "$work\$v\$name"
        if (Test-Path $out) { Remove-Item -Recurse -Force $out }
        New-Item -ItemType Directory -Force $out | Out-Null
        # Same sources and references; only the output folder changes. NeoRune's reference names its own SDK.
        $lines = Get-Content $rsp.FullName | ForEach-Object { if ($_ -like '--out=*') { "--out=$out\" } else { $_ } }
        Set-Content "$work\$v-$name.rsp" $lines
        $dll = if ($v -eq 'old') { $Old } else { $new }
        $log = & dotnet $dll "@$work\$v-$name.rsp" 2>&1
        if ($LASTEXITCODE -ne 0) { Write-Host "$name : $v compiler failed"; $log | Write-Host; $failed++ }
    }
    $oldAssets = "$work\old\$name\Assets"
    $files = @(Get-ChildItem $oldAssets -File -Recurse -ErrorAction SilentlyContinue)
    $diff = @()
    foreach ($f in $files) {
        $rel = $f.FullName.Substring($oldAssets.Length)
        $other = "$work\new\$name\Assets$rel"
        if (-not (Test-Path $other) -or (Get-FileHash $f.FullName).Hash -ne (Get-FileHash $other).Hash) { $diff += $rel }
    }
    Write-Host "$name : $($files.Count) files, $($diff.Count) differ"
    $diff | ForEach-Object { Write-Host "    $_" }
    if ($diff.Count -gt 0) { $failed++ }
}
exit $failed
