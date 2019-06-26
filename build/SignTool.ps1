function Find-SignTool {
    $sdk = Get-ChildItem 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SDKs\Windows' |
        ForEach-Object { Get-ItemProperty $_.PSPath } |
        Where-Object InstallationFolder -ne $null |
        Sort-Object { [version]$_.ProductVersion } |
        Select-Object -Last 1

    if (!$sdk) { throw 'Cannot find a Windows SDK installation that has signtool.exe.' }

    $version = [version]$sdk.ProductVersion;
    $major = $version.Major;
    $minor = [Math]::Max($version.Minor, 0);
    $build = [Math]::Max($version.Build, 0);
    $revision = [Math]::Max($version.Revision, 0);

    return Join-Path $sdk.InstallationFolder "bin\$major.$minor.$build.$revision\x64\signtool.exe"
}

function SignTool(
    [Parameter(Mandatory=$true)] [string] $CertificateThumbprint,
    [Parameter(Mandatory=$true)] [string] $TimestampServer,
    [Parameter(Mandatory=$true)] [string[]] $Files
) {
    & (Find-SignTool) sign /sha1 $CertificateThumbprint /fd SHA256 /tr $TimestampServer @Files
    if ($LastExitCode) { exit 1 }
}
