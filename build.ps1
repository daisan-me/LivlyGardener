$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /win32icon:"$PSScriptRoot\assets\app.ico" /optimize+ /out:"$PSScriptRoot\LivlyGardener.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll "$PSScriptRoot\src\Program.cs" "$PSScriptRoot\src\Vision.cs" "$PSScriptRoot\src\Detector.cs" "$PSScriptRoot\src\ActionWait.cs" "$PSScriptRoot\src\ModernUi.cs"
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
