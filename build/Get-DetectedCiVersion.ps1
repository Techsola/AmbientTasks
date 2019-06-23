. $PSScriptRoot\CiServerIntegration.ps1

function Get-VersionPrefixFromTags {
    function Get-VersionPrefix([Parameter(Mandatory=$true)] [string] $Tag) {
        # Start the search at index 6, skipping 1 for the `v` and 5 because no valid semantic version can have a suffix sooner than `N.N.N`.
        $suffixStart = $Tag.IndexOfAny(('-', '+'), 6)

        return [version] $(
            if ($suffixStart -eq -1) {
                $Tag.Substring(1)
            } else {
                $Tag.Substring(1, $suffixStart - 1)
            })
    }

    $currentTags = @(git tag --list v* --points-at head --sort=-v:refname)
    if ($currentTags.Count -gt 0) {
        # Head is tagged, so the tag is the intended CI version for this build.
        return Get-VersionPrefix $currentTags[0]
    }

    $previousTags = @(git tag --list v* --sort=-v:refname)
    if ($previousTags.Count -gt 0) {
        # Head is not tagged, so it would be greater than the most recent tagged version.
        $previousVersion = Get-VersionPrefix $previousTags[0]
        return [version]::new($previousVersion.Major, $previousVersion.Minor, $previousVersion.Build + 1)
    }

    # No release has been tagged, so the initial version should be whatever the source files currently contain.
}

function XmlPeek(
    [Parameter(Mandatory=$true)] [string] $FilePath,
    [Parameter(Mandatory=$true)] [string] $XPath,
    [HashTable] $NamespaceUrisByPrefix
) {
    $document = [xml](Get-Content $FilePath)
    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($document.NameTable)

    if ($null -ne $NamespaceUrisByPrefix) {
        foreach ($prefix in $NamespaceUrisByPrefix.Keys) {
            $namespaceManager.AddNamespace($prefix, $NamespaceUrisByPrefix[$prefix]);
        }
    }

    return $document.SelectSingleNode($XPath, $namespaceManager).Value
}

class VersionInfo {
    [string] $CommitHash
    [string] $ProductVersion
    [string] $PackageVersion
    [string] $FileVersion
}

function Get-DetectedCiVersion([switch] $Release) {
    $versionPrefix = [version](XmlPeek 'src\AmbientTasks\AmbientTasks.csproj' '/Project/PropertyGroup/Version/text()')
    $minVersionPrefix = Get-VersionPrefixFromTags
    if ($versionPrefix -lt $minVersionPrefix) { $versionPrefix = $minVersionPrefix }

    $buildMetadata = Get-BuildMetadata
    $buildNumber = $buildMetadata.BuildNumber

    $versionInfo = [VersionInfo]::new()
    $versionInfo.CommitHash = (git rev-parse head)
    $versionInfo.ProductVersion = $versionPrefix
    $versionInfo.PackageVersion = $versionPrefix
    $versionInfo.FileVersion = $versionPrefix

    if (!$buildNumber) {
        if ($Release) { throw 'Cannot release without a build number.' }
    }
    else {
        $shortCommitHash = (git rev-parse --short=8 head)

        if ($Release) {
            $versionInfo.ProductVersion += "+build.$buildNumber.commit.$shortCommitHash"
        }
        elseif ($buildMetadata.PullRequestNumber) {
            $versionInfo.ProductVersion += "-$buildNumber.pr.$($buildMetadata.PullRequestNumber)"
            $versionInfo.PackageVersion += "-$buildNumber.pr.$($buildMetadata.PullRequestNumber)"
        }
        elseif ($buildMetadata.BranchName -ne 'master') {
            $prereleaseSegment = $buildMetadata.BranchName -replace '[^a-zA-Z0-9]+', '-'

            $versionInfo.ProductVersion += "-$buildNumber.$prereleaseSegment"
            $versionInfo.PackageVersion += "-$buildNumber.$prereleaseSegment"
        }
        else {
            $versionInfo.ProductVersion += "-ci.$buildNumber+commit.$shortCommitHash"
            $versionInfo.PackageVersion += "-ci.$buildNumber"
        }

        $versionInfo.FileVersion += ".$buildNumber"
    }

    return $versionInfo
}
