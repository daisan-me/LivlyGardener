$ErrorActionPreference = 'Stop'
& "$PSScriptRoot\build.ps1"
$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null
$zip = Join-Path $dist 'LivlyGardener-portable.zip'
Compress-Archive -Path "$PSScriptRoot\LivlyGardener.exe","$PSScriptRoot\ocr.ps1","$PSScriptRoot\assets","$PSScriptRoot\README.md" -DestinationPath $zip -Force
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /win32icon:"$PSScriptRoot\assets\app.ico" /optimize+ /out:"$dist\LivlyGardener-Setup.exe" /reference:System.Windows.Forms.dll /reference:System.Core.dll /reference:Microsoft.CSharp.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /resource:"$zip,package.zip" "$PSScriptRoot\src\Setup.cs"
if ($LASTEXITCODE -ne 0) { throw 'Installer build failed' }
