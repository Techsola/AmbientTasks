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
    elseif ($env.CI) {
        throw 'Build metadata detection is not implemented for this CI server.'
    }

    return $metadata
}

function Update-CiServerBuildName([Parameter(Mandatory=$true)] [string] $BuildName) {
    if ($env:TF_BUILD) {
        Write-Output "##vso[build.updatebuildnumber]$BuildName"
    }
    elseif ($env.CI) {
        throw 'Build name updating is not implemented for this CI server.'
    }
}
