function ValidateMetadata(
    [Parameter(Mandatory=$true)] [string] $ProductVersion,
    [switch] $Release
) {
    $lastReleasedVersion = XmlPeek src\AmbientTasks\AmbientTasks.csproj '/Project/PropertyGroup/Version/text()'

    if ($Release) {
        $productVersionWithoutBuildMetadata = $ProductVersion.Substring(0, $ProductVersion.IndexOf('+'))
        if ($lastReleasedVersion -ne $productVersionWithoutBuildMetadata) {
            throw 'The version must be updated in the .csproj to do a release build.'
        }
    }

    $changelogHeaderLines = Select-String -Path CHANGELOG.md -Pattern ('## [' + $lastReleasedVersion + ']') -SimpleMatch
    if ($changelogHeaderLines.Count -ne 1) {
        throw "There must be exactly one entry in CHANGELOG.md for version $lastReleasedVersion."
    }

    $urlAnchor = $changelogHeaderLines[0].Line.Substring('## '.Length).Replace(' ', '-') -replace '[^-\w]', ''
    $requiredReleaseNotesLink = "https://github.com/Techsola/AmbientTasks/blob/v$lastReleasedVersion/CHANGELOG.md#$urlAnchor"
    $packageReleaseNotes = XmlPeek src\AmbientTasks\AmbientTasks.csproj '/Project/PropertyGroup/PackageReleaseNotes/text()'

    if (-not $packageReleaseNotes.Contains($requiredReleaseNotesLink)) {
        throw 'Package release notes in .csproj must contain this URL: ' + $requiredReleaseNotesLink
    }

    if ($packageReleaseNotes.Length -ne $packageReleaseNotes.Trim().Length) {
        throw 'Package release notes must not begin or end with whitespace.'
    }

    foreach ($line in $packageReleaseNotes.Split(@("`r`n", "`r", "`n"), [StringSplitOptions]::None)) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            if ($line.Length -ne 0) {
                throw 'Package release notes must not have whitespace-only lines.'
            }
        } elseif ($line.Length -ne $line.TrimEnd().Length -and $line.Length -ne $line.TrimEnd().Length + 2) {
            throw 'Package release notes must not have trailing whitespace.'
        } elseif ($line.Length -ne $line.TrimStart().Length) {
            throw 'Package release notes must not be indented.'
        }
    }
}
