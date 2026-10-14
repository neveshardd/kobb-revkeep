$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$companyRoot = [IO.Path]::GetFullPath((Join-Path $root '../..'))
$installer = Join-Path $companyRoot 'softwares/kobb-installer'
& (Join-Path $PSScriptRoot 'Build.ps1') -RevitVersion 2026
$payload = Join-Path $root 'payload'
New-Item -ItemType Directory -Path $payload -Force | Out-Null
$binaryRoot = Join-Path $root 'src/Kobb.Revkeep/bin/Release/net10.0-windows'
foreach ($name in @('Kobb.Revkeep.dll', 'Kobb.Revkeep.Core.dll', 'Kobb.Revkeep.UI.dll', 'Kobb.Revkeep.deps.json')) {
    Copy-Item -LiteralPath (Join-Path $binaryRoot $name) -Destination $payload -Force
}
& (Join-Path $installer 'scripts/Prepare-Package.ps1') -ProductId kobb-revkeep -ProductRoot $root -RevitVersions 2026
& (Join-Path $installer 'scripts/Build.ps1') -ProductId kobb-revkeep -ProductRoot $root -ProductIcon (Join-Path $root 'assets/logo-revkeep.png') -ReleasePackage
$output = Join-Path $companyRoot 'products/kobb-revkeep'
$catalog = Get-Content -LiteralPath (Join-Path $root 'product.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$version = $catalog.version
$setupName = "kobb-revkeep-setup-$version.exe"
Move-Item -LiteralPath (Join-Path $output 'kobb-installer.exe') -Destination (Join-Path $output $setupName) -Force
Set-Content -LiteralPath (Join-Path $output 'Abrir previa.cmd') -Value ('@start "" "%~dp0' + $setupName + '" --preview') -Encoding ASCII
$notice = "KOBB Revkeep $version — versão de testes para Revit 2026.5 (.NET 10). A validação dentro do Revit ainda está pendente. Mantenha product.json e payload junto ao executável."
[IO.File]::WriteAllText((Join-Path $output 'LEIA-ME.txt'), $notice, [Text.UTF8Encoding]::new($false))
$archive = Join-Path $output "kobb-revkeep-$version-testes-win-x64.zip"
Compress-Archive -LiteralPath (Join-Path $output $setupName),(Join-Path $output 'product.json'),(Join-Path $output 'payload'),(Join-Path $output 'Abrir previa.cmd'),(Join-Path $output 'LEIA-ME.txt') -DestinationPath $archive -Force
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath ($archive + '.sha256') -Value "$hash  $([IO.Path]::GetFileName($archive))" -Encoding ASCII
Write-Output $output
