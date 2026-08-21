<#
.SYNOPSIS
Builds and tests the repository, then verifies the Native AOT smoke application.

.PARAMETER Configuration
The .NET build configuration. The default is Release.

.PARAMETER RuntimeIdentifier
The runtime identifier used for Native AOT publishing. It is detected automatically by default.

.PARAMETER SkipNativeAot
Skips Native AOT publishing and the executable round-trip check.
#>

[CmdletBinding()]
param(
    [string] $Configuration = "Release",
    [string] $RuntimeIdentifier,
    [switch] $SkipNativeAot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $DotNetArguments
    )

    & dotnet @DotNetArguments

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet exited with code $LASTEXITCODE."
    }
}

function Get-DefaultRuntimeIdentifier {
    $architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture

    $architectureName = switch ($architecture) {
        ([System.Runtime.InteropServices.Architecture]::X64) { "x64" }
        ([System.Runtime.InteropServices.Architecture]::Arm64) { "arm64" }
        default { throw "Native AOT smoke testing does not support architecture '$architecture'." }
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        return "win-$architectureName"
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux)) {
        return "linux-$architectureName"
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)) {
        return "osx-$architectureName"
    }

    throw "Native AOT smoke testing is not supported on this operating system."
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repositoryRoot "Ling.RemoteServices.slnx"
$smokeProjectPath = Join-Path $repositoryRoot "tests/Ling.RemoteServices.NativeAotSmoke/Ling.RemoteServices.NativeAotSmoke.csproj"

Push-Location $repositoryRoot

try {
    Invoke-DotNet -DotNetArguments @("restore", $solutionPath)
    Invoke-DotNet -DotNetArguments @("build", $solutionPath, "--configuration", $Configuration, "--no-restore")
    Invoke-DotNet -DotNetArguments @("test", $solutionPath, "--configuration", $Configuration, "--no-build", "--verbosity", "normal")

    if ($SkipNativeAot) {
        Write-Host "Native AOT smoke verification was skipped."
        return
    }

    if ([string]::IsNullOrWhiteSpace($RuntimeIdentifier)) {
        $RuntimeIdentifier = Get-DefaultRuntimeIdentifier
    }

    $publishArguments = @(
        "publish",
        $smokeProjectPath,
        "--configuration", $Configuration,
        "--runtime", $RuntimeIdentifier,
        "--self-contained", "true",
        "-p:TrimmerSingleWarn=false"
    )

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        $publishArguments += "-p:OS=Windows_NT"
    }

    Invoke-DotNet -DotNetArguments $publishArguments

    $executableName = if ($RuntimeIdentifier.StartsWith("win-", [System.StringComparison]::OrdinalIgnoreCase)) {
        "Ling.RemoteServices.NativeAotSmoke.exe"
    }
    else {
        "Ling.RemoteServices.NativeAotSmoke"
    }

    $publishDirectory = Join-Path $repositoryRoot "tests/Ling.RemoteServices.NativeAotSmoke/bin/$Configuration/net8.0/$RuntimeIdentifier/publish"
    $executablePath = Join-Path $publishDirectory $executableName

    if (-not (Test-Path -LiteralPath $executablePath)) {
        throw "The Native AOT executable was not found at '$executablePath'."
    }

    $baseAddress = "http://127.0.0.1:5199"
    $standardOutputPath = Join-Path ([System.IO.Path]::GetTempPath()) "ling-remote-services-aot-$PID.log"
    $standardErrorPath = Join-Path ([System.IO.Path]::GetTempPath()) "ling-remote-services-aot-$PID.err.log"
    $processArguments = @("--urls", $baseAddress)
    $startProcessArguments = @{
        FilePath = $executablePath
        ArgumentList = $processArguments
        PassThru = $true
        RedirectStandardOutput = $standardOutputPath
        RedirectStandardError = $standardErrorPath
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        $startProcessArguments.WindowStyle = "Hidden"
    }

    $process = Start-Process @startProcessArguments

    try {
        $httpClient = [System.Net.Http.HttpClient]::new()
        $httpClient.Timeout = [TimeSpan]::FromSeconds(2)
        $responseBody = $null

        try {
            for ($attempt = 1; $attempt -le 20; $attempt++) {
                if ($process.HasExited) {
                    throw "The Native AOT smoke application exited before it became ready."
                }

                try {
                    $responseBody = $httpClient.GetStringAsync("$baseAddress/client-smoke").GetAwaiter().GetResult()
                    break
                }
                catch {
                    if ($attempt -ge 20) {
                        throw
                    }

                    Start-Sleep -Milliseconds 500
                }
            }
        }
        finally {
            $httpClient.Dispose()
        }

        if ($responseBody -notlike "*client-round-trip*") {
            throw "The Native AOT round-trip returned an unexpected response: '$responseBody'."
        }

        Write-Host "Native AOT client and server round-trip succeeded for $RuntimeIdentifier."
    }
    catch {
        if (Test-Path -LiteralPath $standardOutputPath) {
            Get-Content -LiteralPath $standardOutputPath | Write-Host
        }

        if (Test-Path -LiteralPath $standardErrorPath) {
            Get-Content -LiteralPath $standardErrorPath | Write-Host
        }

        throw
    }
    finally {
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
        }

        $process.Dispose()
        Remove-Item -LiteralPath $standardOutputPath, $standardErrorPath -Force -ErrorAction SilentlyContinue
    }
}
finally {
    Pop-Location
}
