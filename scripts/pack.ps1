<#
.SYNOPSIS
Tests the repository and creates all public NuGet packages.

.PARAMETER Configuration
The .NET build configuration. The default is Release.

.PARAMETER RuntimeIdentifier
The runtime identifier passed to the Native AOT smoke test.

.PARAMETER OutputDirectory
The directory that receives the NuGet packages.

.PARAMETER Version
An optional package version override.

.PARAMETER SkipTests
Skips repository tests and Native AOT verification before packing.
#>

[CmdletBinding()]
param(
    [string] $Configuration = "Release",
    [string] $RuntimeIdentifier,
    [string] $OutputDirectory = "artifacts",
    [string] $Version,
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot

if (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot $OutputDirectory
}

if (-not $SkipTests) {
    $testArguments = @("-Configuration", $Configuration)

    if (-not [string]::IsNullOrWhiteSpace($RuntimeIdentifier)) {
        $testArguments += @("-RuntimeIdentifier", $RuntimeIdentifier)
    }

    & (Join-Path $PSScriptRoot "test.ps1") @testArguments
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$packageProjects = @(
    "src/Ling.RemoteServices/Ling.RemoteServices.csproj",
    "src/Ling.RemoteServices.Client/Ling.RemoteServices.Client.csproj",
    "src/Ling.RemoteServices.AspNetCore/Ling.RemoteServices.AspNetCore.csproj"
)

Push-Location $repositoryRoot

try {
    foreach ($project in $packageProjects) {
        $packArguments = @(
            "pack",
            $project,
            "--configuration", $Configuration,
            "--output", $OutputDirectory,
            "-p:NoWarn=NU5118"
        )

        if (-not $SkipTests) {
            $packArguments += @("--no-build", "--no-restore")
        }

        if (-not [string]::IsNullOrWhiteSpace($Version)) {
            $packArguments += "-p:Version=$Version"
        }

        & dotnet @packArguments

        if ($LASTEXITCODE -ne 0) {
            throw "dotnet pack exited with code $LASTEXITCODE for '$project'."
        }
    }
}
finally {
    Pop-Location
}

Write-Host "NuGet packages were written to '$OutputDirectory'."
