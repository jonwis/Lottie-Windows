param(
    [string]$VsDevShell = "C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\Tools\Launch-VsDevShell.ps1",
    [string]$Config = "Debug",
    [string]$Platform = "x64",
    [string]$TraceDir = ".\trace",
    [string]$RunName = "LottieRuntimeComTest_E_ACCESSDENIED.run",
    [string]$InputPath = ".\nonexistent.bin",
    [string]$TtTracer = "tttracer.exe"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

& $VsDevShell -SkipAutomaticLocation

msbuild "dlls\LottieRuntimeComTest\LottieRuntimeComTest.vcxproj" `
    /p:Platform=$Platform `
    /p:Configuration=$Config `
    /t:Rebuild `
    /nologo `
    /v:minimal

New-Item -ItemType Directory -Force -Path $TraceDir | Out-Null

$ExePath = ".\dlls\LottieRuntimeComTest\$Platform\$Config\LottieRuntimeComTest.exe"
$RunPath = Join-Path $TraceDir $RunName

& $TtTracer -out $RunPath $ExePath $InputPath

wevtutil qe Application /q:"*[System[Provider[@Name='Microsoft-Windows-SideBySide']]]" /c:30 /rd:true /f:text > (Join-Path $TraceDir "sxs-events.txt")
wevtutil qe Application /q:"*[System[Provider[@Name='Microsoft-Windows-COMRuntime']]]" /c:30 /rd:true /f:text > (Join-Path $TraceDir "com-events.txt")

Write-Host "Done."
Write-Host "Trace: $RunPath"
Write-Host "SxS:   $(Join-Path $TraceDir 'sxs-events.txt')"
Write-Host "COM:   $(Join-Path $TraceDir 'com-events.txt')"
