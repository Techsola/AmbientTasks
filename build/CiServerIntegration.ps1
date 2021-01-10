class BuildMetadata {
    [int] $BuildNumber
    [System.Nullable[int]] $PullRequestNumber
    [string] $BranchName
}

function Get-BuildMetadata {
    $metadata = [BuildMetadata]::new()

    if ($env:TF_BUILD) {
        $metadata.BuildNumber = $env:Build_BuildId
        $metadata.PullRequestNumber = $env:System_PullRequest_PullRequestNumber
        $metadata.BranchName = $env:Build_SourceBranchName
    }
    elseif ($env:GITHUB_ACTIONS) {
        $metadata.BuildNumber = $env:GITHUB_RUN_NUMBER

        if ($env:GITHUB_REF.StartsWith('refs/pull/')) {
            $trimmedRef = $env:GITHUB_REF.Substring('refs/pull/'.Length)
            $metadata.PullRequestNumber = $trimmedRef.Substring(0, $trimmedRef.IndexOf('/'))
            $metadata.BranchName = $env:GITHUB_BASE_REF
        } elseif ($env:GITHUB_REF.StartsWith('refs/heads/')) {
            $metadata.BranchName = $env:GITHUB_REF.Substring('refs/heads/'.Length)
        }
    }
    elseif ($env:CI) {
        throw 'Build metadata detection is not implemented for this CI server.'
    }

    return $metadata
}

function Update-CiServerBuildName([Parameter(Mandatory=$true)] [string] $BuildName) {
    if ($env:TF_BUILD) {
        Write-Output "##vso[build.updatebuildnumber]$BuildName"
    }
    elseif ($env:GITHUB_ACTIONS) {
        # GitHub Actions does not appear to have a way to dynamically update the name/number of a workflow run.
    }
    elseif ($env:CI) {
        throw 'Build name updating is not implemented for this CI server.'
    }
}
