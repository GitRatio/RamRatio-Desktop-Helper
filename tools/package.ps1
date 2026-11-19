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
# Windows: create an additional self-contained GUI EXE carrying the original native host.
$hashTargets = @($exe, $archive)
if ($Runtime.StartsWith('win-')) {
    $uiDir = Join-Path $projectRoot 'windows-installer'
    $embeddedHost = Join-Path $uiDir 'NativeHost.Payload.exe'
    $generatedHash = Join-Path $uiDir 'PayloadHash.g.cs'
    $uiOutput = Join-Path $output 'gui'
    $guiAsset = Join-Path $projectRoot "artifacts/RamRatio-DesktopHelper-Setup-$Runtime.exe"
    try {
        Copy-Item -LiteralPath $exe -Destination $embeddedHost -Force
        $digest = (Get-FileHash -Algorithm SHA256 -LiteralPath $embeddedHost).Hash.ToUpperInvariant()
        $generated = 'namespace RamRatioInstaller; internal static class PayloadHash { internal const string Sha256 = "' + $digest + '"; }'
        [IO.File]::WriteAllText($generatedHash, $generated + [Environment]::NewLine)
        dotnet publish (Join-Path $uiDir 'RamRatio.WindowsInstaller.csproj') -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $uiOutput
        if ($LASTEXITCODE -ne 0) { throw 'Windows graphical installer build failed' }
        $guiExe = Join-Path $uiOutput 'RamRatio-DesktopHelper-Setup.exe'
        if (!(Test-Path -LiteralPath $guiExe)) { throw 'Graphical installer EXE not produced' }
        Copy-Item -LiteralPath $guiExe -Destination $guiAsset -Force
        $hashTargets += $guiAsset
    }
    finally {
        Remove-Item -LiteralPath $embeddedHost,$generatedHash -Force -ErrorAction SilentlyContinue
    }
}
# macOS: create a double-clickable .app in a DMG; it runs our tested native
# helper in the user's account via built-in AppleScript dialogs.
if ($Runtime.StartsWith('osx-')) {
    $app = Join-Path $projectRoot "artifacts/RamRatio-DesktopHelper-Setup-$Runtime.app"
    $contents = Join-Path $app 'Contents'
    $resources = Join-Path $contents 'Resources'
    $macOs = Join-Path $contents 'MacOS'
    New-Item -ItemType Directory -Force -Path $resources,$macOs | Out-Null
    Copy-Item -LiteralPath $exe -Destination (Join-Path $resources $exeName) -Force
    Copy-Item -LiteralPath (Join-Path $projectRoot 'macos-installer/launcher.sh') -Destination (Join-Path $macOs 'RamRatioSetup') -Force
    foreach ($file in @('README.md','PRIVACY.md','TERMS.md','SECURITY.md','LICENSE-HELPER.md','DOTNET-LICENSE.txt','DOTNET-THIRD-PARTY-NOTICES.txt')) {
        Copy-Item -LiteralPath (Join-Path $output $file) -Destination (Join-Path $resources $file) -Force
    }
    $plist = @'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleIdentifier</key><string>com.ratioworks.ramratio.setup</string>
  <key>CFBundleName</key><string>RamRatio Desktop Helper Setup</string>
  <key>CFBundleDisplayName</key><string>RamRatio Setup</string>
  <key>CFBundleExecutable</key><string>RamRatioSetup</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleVersion</key><string>300</string>
  <key>CFBundleShortVersionString</key><string>0.3.0</string>
</dict></plist>
'@
    [IO.File]::WriteAllText((Join-Path $contents 'Info.plist'), $plist)
    & chmod +x (Join-Path $macOs 'RamRatioSetup') (Join-Path $resources $exeName)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot mark macOS app launcher executable' }
    # ZIP is the primary low-space app bundle, preserving executable bits with ditto.
    $appZip = Join-Path $projectRoot "artifacts/RamRatio-DesktopHelper-Setup-$Runtime.app.zip"
    & ditto -c -k --sequesterRsrc --keepParent $app $appZip
    if ($LASTEXITCODE -ne 0) { throw 'macOS app ZIP build failed' }
    $hashTargets += $appZip
    # Apple's Intel runner has a constrained disk. Building a mounted DMG there
    # may fail with ENOSPC; offer the same double-clickable app as a ZIP instead.
    if ($Runtime -eq 'osx-arm64') {
        $dmg = Join-Path $projectRoot "artifacts/RamRatio-DesktopHelper-Setup-$Runtime.dmg"
        & hdiutil create -ov -volname 'RamRatio Desktop Helper Setup' -srcfolder $app -format UDZO $dmg
        if ($LASTEXITCODE -ne 0) { throw 'macOS DMG build failed' }
        $hashTargets += $dmg
    }
}
# Linux: one downloadable self-extracting .run file, no root installer;
# prefer zenity/kdialog dialogs when available, otherwise terminal prompt.
if ($Runtime.StartsWith('linux-')) {
    $runFile = Join-Path $projectRoot "artifacts/RamRatio-DesktopHelper-Setup-$Runtime.run"
    $templatePath = Join-Path $projectRoot 'linux-installer/launcher.run.in'
    $digest = (Get-FileHash -Algorithm SHA256 -LiteralPath $exe).Hash.ToLowerInvariant()
    $header = [IO.File]::ReadAllText($templatePath).Replace('__EXPECTED_SHA256__', $digest)
    if (!$header.EndsWith("`n")) { throw 'Missing binary marker newline in Linux installer' }
    [IO.File]::WriteAllText($runFile, $header, [Text.UTF8Encoding]::new($false))
    $stream = [IO.File]::Open($runFile, [IO.FileMode]::Append, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $payload = [IO.File]::ReadAllBytes($exe)
        $stream.Write($payload, 0, $payload.Length)
    } finally { $stream.Dispose() }
    & chmod +x $runFile
    if ($LASTEXITCODE -ne 0) { throw 'Linux installer chmod failed' }
    $hashTargets += $runFile
}
Get-FileHash -Algorithm SHA256 -LiteralPath $hashTargets | ForEach-Object {
    "$($_.Hash.ToLower())  $(Split-Path $_.Path -Leaf)"
} | Set-Content (Join-Path $projectRoot "artifacts/SHA256SUMS-$Runtime.txt")
