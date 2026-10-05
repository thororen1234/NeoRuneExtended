<#
.SYNOPSIS
  Builds NeoRuneExtended and writes the NeoRuneExtended.Sdk, .Tool and .Templates packages to artifacts/packages.

.DESCRIPTION
  Mods find the packages through a NuGet source pointing at artifacts/packages (see docs/maintaining.md). The SDK's cached
  copy in ~/.nuget/packages is removed, so a mod's next build picks up this one even when the version didn't change.

.PARAMETER Version
  Package version (default: the one in Directory.Build.props).

.PARAMETER Bindings
  Folder with NeoRune.Game.dll and NeoRune.Game.json (default: bindings/prebuilt).
#>
param(
    [string]$Version,
    [string]$Bindings
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $Version) {
    $Version = ([xml](Get-Content "$root\Directory.Build.props")).Project.PropertyGroup.Version.'#text'
    if (-not $Version) { $Version = ([xml](Get-Content "$root\Directory.Build.props")).Project.PropertyGroup.Version }
}
if (-not $Bindings) { $Bindings = "$root\bindings\prebuilt" }

$stage = "$root\artifacts\stage"
$packages = "$root\artifacts\packages"
Remove-Item -Recurse -Force $stage -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $stage, $packages | Out-Null

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($args -join ' ') failed ($LASTEXITCODE)" }
}

Write-Host "NeoRuneExtended $Version"
Invoke-Dotnet build "$root\NeoRuneExtended.slnx" -c Release -p:Version=$Version -v q -nologo

# The SDK package: Sdk/, src/ (helpers compiled into each mod), ref/, analyzers/, tools/ (the compiler).
$sdk = "$stage\sdk"
Invoke-Dotnet publish "$root\src\NeoRuneExtended.Cli\NeoRuneExtended.Cli.csproj" -c Release -p:Version=$Version -o "$sdk\tools" -v q -nologo
Remove-Item "$sdk\tools\*.pdb" -Exclude 'neorunex.pdb', 'NeoRuneExtended.*.pdb'
Copy-Item -Recurse "$root\sdk\NeoRuneExtended.Sdk\Sdk" "$sdk\Sdk"
Copy-Item -Recurse "$root\sdk\NeoRuneExtended.Sdk\src" "$sdk\src"
New-Item -ItemType Directory -Force "$sdk\ref", "$sdk\analyzers" | Out-Null
$abstractions = "$root\src\NeoRuneExtended.Abstractions\bin\Release\netstandard2.0"
Copy-Item "$abstractions\NeoRune.Abstractions.dll", "$abstractions\NeoRune.Abstractions.xml" "$sdk\ref"
Copy-Item "$Bindings\NeoRune.Game.dll", "$Bindings\NeoRune.Game.json" "$sdk\ref"
if (Test-Path "$Bindings\NeoRune.Game.xml") { Copy-Item "$Bindings\NeoRune.Game.xml" "$sdk\ref" }
Copy-Item "$root\src\NeoRuneExtended.Analyzers\bin\Release\netstandard2.0\NeoRuneExtended.Analyzers.dll" "$sdk\analyzers"
Invoke-Dotnet pack "$root\sdk\NeoRuneExtended.Sdk\NeoRuneExtended.Sdk.csproj" -c Release -p:Version=$Version -p:StageDir="$sdk\\" -o $packages -v q -nologo

# The global tool.
Invoke-Dotnet pack "$root\src\NeoRuneExtended.Cli\NeoRuneExtended.Cli.csproj" -c Release -p:Version=$Version -o $packages -v q -nologo

# The templates, with this version as the default SDK version.
$templates = "$stage\templates"
Copy-Item -Recurse "$root\templates\content" $templates
Get-ChildItem $templates -Recurse -Filter template.json | ForEach-Object {
    (Get-Content $_.FullName -Raw).Replace('NEORUNEX_TEMPLATE_VERSION', $Version) | Set-Content $_.FullName -NoNewline
}
Invoke-Dotnet pack "$root\templates\NeoRuneExtended.Templates.csproj" -c Release -p:Version=$Version -p:StageDir="$templates\\" -o $packages -v q -nologo

# Mods restore the SDK from the NuGet cache: drop this version's cached copy so they get the new build.
$cache = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { "$env:USERPROFILE\.nuget\packages" }
foreach ($id in 'neoruneextended.sdk', 'neoruneextended.templates') {
    Remove-Item -Recurse -Force "$cache\$id\$($Version.ToLowerInvariant())" -ErrorAction SilentlyContinue
}

Get-ChildItem $packages -Filter "*.$Version.nupkg" | ForEach-Object { Write-Host "  $($_.Name)" }
