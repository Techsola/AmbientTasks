Param(
    [switch] $Release,
    [string] $SigningCertThumbprint,
    [string] $TimestampServer
)

$ErrorActionPreference = 'Stop'

# Options
$configuration = 'Release'
$artifactsDir = Join-Path (Resolve-Path .) 'artifacts'
$packagesDir = Join-Path $artifactsDir 'Packages'
$testResultsDir = Join-Path $artifactsDir 'Test results'
$logsDir = Join-Path $artifactsDir 'Logs'

# Detection
. $PSScriptRoot\build\Get-DetectedCiVersion.ps1
$versionInfo = Get-DetectedCiVersion -Release:$Release
Update-CiServerBuildName $versionInfo.ProductVersion
Write-Host "Building using version $($versionInfo.ProductVersion)"

$dotnetArgs = @(
    '--configuration', $configuration
    '/p:RepositoryCommit=' + $versionInfo.CommitHash
    '/p:Version=' + $versionInfo.ProductVersion
    '/p:PackageVersion=' + $versionInfo.PackageVersion
    '/p:FileVersion=' + $versionInfo.FileVersion
)

# Build
dotnet build /bl:"$logsDir\build.binlog" @dotnetArgs
if ($LastExitCode) { exit 1 }

if ($SigningCertThumbprint) {
    . build\SignTool.ps1
    SignTool $SigningCertThumbprint $TimestampServer (
        Get-ChildItem src\AmbientTasks\bin\$configuration -Recurse -Include AmbientTasks.dll)
}

# Pack
Remove-Item -Recurse -Force $packagesDir -ErrorAction Ignore

dotnet pack --no-build --output $packagesDir /bl:"$logsDir\pack.binlog" @dotnetArgs
if ($LastExitCode) { exit 1 }

if ($SigningCertThumbprint) {
    # Waiting for 'dotnet sign' to become available (https://github.com/NuGet/Home/issues/7939)
    $nuget = 'tools\nuget.exe'
    if (-not (Test-Path $nuget)) {
        New-Item -ItemType Directory -Force -Path tools
        Invoke-WebRequest -Uri https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile $nuget
    }

     # Workaround for https://github.com/NuGet/Home/issues/10446
    foreach ($extension in 'nupkg', 'snupkg') {
        & $nuget sign $packagesDir\*.$extension -CertificateFingerprint $SigningCertThumbprint -Timestamper $TimestampServer
    }
}

# Test
if ($env:CODECOV_TOKEN) {
    dotnet tool install Codecov.Tool --tool-path tools
    $codecov = 'tools\codecov'
}

Remove-Item -Recurse -Force $testResultsDir -ErrorAction Ignore

dotnet test --no-build --configuration $configuration --logger trx --results-directory $testResultsDir /p:AltCover=true /p:AltCoverXmlReport="$testResultsDir\coverage.xml" /bl:"$logsDir\test.binlog"
if ($LastExitCode) { $testsFailed = true }

if ($env:CODECOV_TOKEN) {
    foreach ($coverageFile in Get-ChildItem "$testResultsDir\coverage.*.xml") {
        $tfm = $coverageFile.Name.Substring(
            'coverage.'.Length,
            $coverageFile.Name.Length - 'coverage.'.Length - '.xml'.Length)

        & $codecov --name $tfm --file $coverageFile --token $env:CODECOV_TOKEN
        if ($LastExitCode) { exit 1 }
    }
}

if ($testsFailed) { exit 1 }
