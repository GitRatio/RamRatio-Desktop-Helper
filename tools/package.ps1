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
