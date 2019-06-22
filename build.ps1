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
dotnet tool install altcover.global --tool-path tools
$altcover = 'tools\altcover'

if ($env:CODECOV_TOKEN) {
    dotnet tool install Codecov.Tool --tool-path tools
    $codecov = 'tools\codecov'
}

Remove-Item -Recurse -Force $testResultsDir -ErrorAction Ignore

foreach ($testAssembly in Get-ChildItem -Recurse -Path src\*.Tests\bin\$configuration -Include *.Tests.dll) {
    $directory = Split-Path $testAssembly
    $tfm = (Split-Path -Leaf $directory)

    $savedDirectory = '__Saved'
    Remove-Item -Recurse -Force $savedDirectory -ErrorAction Ignore
    & $altcover --inputDirectory=$directory --inplace --outputDirectory=$savedDirectory --opencover --xmlReport=$testResultsDir\coverage.$tfm.xml --assemblyExcludeFilter='AmbientTasks.Tests|NUnit3.TestAdapter'
    Remove-Item -Recurse -Force $savedDirectory -ErrorAction Ignore

    & $vstest $testAssembly /Logger:'console;verbosity=minimal' /Logger:"trx;LogFileName=$tfm.trx" /ResultsDirectory:$testResultsDir

    & $altcover runner --collect --recorderDirectory=$directory

    if ($env:CODECOV_TOKEN) {
        # Workaround for https://github.com/codecov/codecov-exe/issues/71
        $codecovFullPath = Join-Path (Get-Location) $codecov
        Push-Location $testResultsDir

        & $codecovFullPath --name $tfm --file coverage.$tfm.xml --token $env:CODECOV_TOKEN

        # Workaround for https://github.com/codecov/codecov-exe/issues/71
        Pop-Location
    }
}
