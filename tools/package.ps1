param([ValidateSet('win-x64','win-arm64','osx-x64','osx-arm64','linux-x64','linux-arm64')][string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$output = Join-Path $projectRoot "artifacts/$Runtime"
dotnet publish (Join-Path $projectRoot 'desktop-helper/RamRatio.Helper.csproj') -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -o $output
if ($LASTEXITCODE -ne 0) { throw 'Helper build failed' }
$exeName = if ($Runtime.StartsWith('win-')) { 'RamRatio.DesktopHelper.Setup.exe' } else { 'RamRatio.DesktopHelper.Setup' }
$exe = Join-Path $output $exeName
foreach ($doc in @('PRIVACY.md','TERMS.md','SECURITY.md','LICENSE-HELPER.md')) { Copy-Item -LiteralPath (Join-Path $projectRoot $doc) -Destination $output }
Copy-Item -LiteralPath (Join-Path $projectRoot 'desktop-helper/README.md') -Destination (Join-Path $output 'README.md')
$files = @($exeName,'README.md','PRIVACY.md','TERMS.md','SECURITY.md','LICENSE-HELPER.md')
$assets = Get-Content -LiteralPath (Join-Path $projectRoot 'desktop-helper/obj/project.assets.json') -Raw | ConvertFrom-Json
$runtimePackage = "Microsoft.NETCore.App.Runtime.$Runtime"
$dependency = $assets.project.frameworks.'net10.0'.downloadDependencies | Where-Object { $_.name -eq $runtimePackage } | Select-Object -First 1
if (!$dependency) { throw 'Cannot identify runtime license version' }
$runtimeVersion = $dependency.version.Trim('[',']').Split(',')[0].Trim()
$runtimeFolder = $null
foreach ($packageRoot in $assets.packageFolders.PSObject.Properties.Name) {
    $candidate = Join-Path $packageRoot "$($runtimePackage.ToLower())/$runtimeVersion"
    if (Test-Path -LiteralPath (Join-Path $candidate 'LICENSE.TXT')) { $runtimeFolder = $candidate; break }
}
if (!$runtimeFolder) { throw 'Runtime license files missing' }
Copy-Item -LiteralPath (Join-Path $runtimeFolder 'LICENSE.TXT') -Destination (Join-Path $output 'DOTNET-LICENSE.txt')
Copy-Item -LiteralPath (Join-Path $runtimeFolder 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $output 'DOTNET-THIRD-PARTY-NOTICES.txt')
$files += @('DOTNET-LICENSE.txt','DOTNET-THIRD-PARTY-NOTICES.txt')
if (!$Runtime.StartsWith('win-')) {
    $launcher = if ($Runtime.StartsWith('osx-')) { 'Setup.command' } else { 'Setup.sh' }
    $script = '#!/bin/sh' + "`n" + 'set -eu' + "`n" + 'cd -- "$(dirname -- "$0")"' + "`n" + 'exec ./RamRatio.DesktopHelper.Setup' + "`n"
    [IO.File]::WriteAllText((Join-Path $output $launcher), $script)
    $files += $launcher
    & chmod +x $exe (Join-Path $output $launcher)
    if ($LASTEXITCODE -ne 0) { throw 'Package Unix executables on Unix to preserve permissions' }
    $archive = Join-Path $projectRoot "artifacts/RamRatio-DesktopHelper-$Runtime.tar.gz"
    & tar -czf $archive -C $output @files
    if ($LASTEXITCODE -ne 0) { throw 'Archive failed' }
} else {
    $archive = Join-Path $projectRoot "artifacts/RamRatio-DesktopHelper-$Runtime.zip"
    Compress-Archive -LiteralPath ($files | ForEach-Object { Join-Path $output $_ }) -DestinationPath $archive -Force
}
Get-FileHash -Algorithm SHA256 -LiteralPath $exe,$archive | ForEach-Object { "$($_.Hash.ToLower())  $(Split-Path $_.Path -Leaf)" } | Set-Content (Join-Path $projectRoot "artifacts/SHA256SUMS-$Runtime.txt")