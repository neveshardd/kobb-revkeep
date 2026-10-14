param(
    [ValidateSet('2025', '2026')][string]$RevitVersion = '2026',
    [string]$RevitApiDirectory = "$env:ProgramW6432/Autodesk/Revit $RevitVersion",
    [ValidateSet('auto', 'net8.0-windows', 'net10.0-windows')][string]$TargetFramework = 'auto'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dotnet = Join-Path $env:USERPROFILE '.tools/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $dotnet)) { $dotnet = 'dotnet' }
$api = Join-Path $RevitApiDirectory 'RevitAPI.dll'
if (!(Test-Path -LiteralPath $api)) { throw "Forneça as DLLs oficiais do Revit $RevitVersion em RevitApiDirectory." }
$identity = [Reflection.AssemblyName]::GetAssemblyName($api)
if ($identity.Version.Major -ne ([int]$RevitVersion - 2000)) { throw 'As DLLs fornecidas não correspondem à versão solicitada do Revit.' }
if ($TargetFramework -eq 'auto') {
    $runtime = Join-Path $RevitApiDirectory 'RevitAPI.runtimeconfig.json'
    if (!(Test-Path -LiteralPath $runtime)) { throw 'Informe TargetFramework conforme a atualização do Revit: net8.0-windows ou net10.0-windows.' }
    $tfm = (Get-Content -LiteralPath $runtime -Raw | ConvertFrom-Json).runtimeOptions.tfm
    if ($tfm -notin @('net8.0', 'net10.0')) { throw 'Runtime do Revit não reconhecido.' }
    $TargetFramework = "$tfm-windows"
}
& $dotnet build (Join-Path $root 'src/Kobb.Revkeep/Kobb.Revkeep.csproj') -c Release "-p:RevitVersion=$RevitVersion" "-p:RevitApiDirectory=$RevitApiDirectory" "-p:RevkeepTargetFramework=$TargetFramework" -v:q -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw 'A compilação do Revkeep falhou.' }
