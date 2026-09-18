param([string]$FixtureDirectory)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /optimize+ /target:exe /main:DetectorTests /out:"$PSScriptRoot\DetectorTests.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll "$PSScriptRoot\src\Program.cs" "$PSScriptRoot\src\Vision.cs" "$PSScriptRoot\src\Detector.cs" "$PSScriptRoot\src\ActionWait.cs" "$PSScriptRoot\src\ModernUi.cs" "$PSScriptRoot\tests\DetectorTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test build failed' }
if ($FixtureDirectory) { & "$PSScriptRoot\DetectorTests.exe" $FixtureDirectory } else { & "$PSScriptRoot\DetectorTests.exe" }
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
