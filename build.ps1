$ErrorActionPreference = 'Stop'

$configuration = 'Release'
$artifactsDir = Join-Path (Resolve-Path .) 'artifacts'
$packagesDir = Join-Path $artifactsDir 'Packages'
$testResultsDir = Join-Path $artifactsDir 'Test results'

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$visualStudioInstallation = & $vswhere -latest -version [16,] -requires Microsoft.Component.MSBuild -products * -property installationPath
if (!$visualStudioInstallation) { throw 'Cannot find installation of Visual Studio 2019 or newer.' }
$msbuild = Join-Path $visualStudioInstallation 'MSBuild\Current\Bin\MSBuild.exe'
$vstest = Join-Path $visualStudioInstallation 'Common7\IDE\CommonExtensions\Microsoft\TestWindow\VSTest.Console.exe'

# Build
& $msbuild /restore /p:Configuration=$configuration /v:minimal

# Pack
& $msbuild /t:pack /p:NoBuild=true /p:Configuration=$configuration /v:minimal /p:PackageOutputPath=$packagesDir

# Test
foreach ($testAssembly in Get-ChildItem -Recurse -Path src\*.Tests\bin\$configuration -Include *.Tests.dll) {
    & $vstest $testAssembly /Logger:'console;verbosity=minimal' /Logger:trx /ResultsDirectory:$testResultsDir
}
