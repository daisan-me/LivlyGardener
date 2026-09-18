param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$previewExe = Join-Path $OutputDirectory 'UiPreview.exe'
& $compiler /nologo /target:winexe /main:LivlyGardener.UiPreview /out:$previewExe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll "$PSScriptRoot\src\Program.cs" "$PSScriptRoot\src\Vision.cs" "$PSScriptRoot\src\Detector.cs" "$PSScriptRoot\src\ActionWait.cs" "$PSScriptRoot\src\ModernUi.cs" "$PSScriptRoot\tests\UiPreview.cs"
if ($LASTEXITCODE -ne 0) { throw 'Preview build failed' }
Copy-Item -LiteralPath "$PSScriptRoot\assets" -Destination $OutputDirectory -Recurse -Force
Start-Process -FilePath $previewExe -ArgumentList ('"' + $OutputDirectory + '"') -WindowStyle Hidden -Wait
