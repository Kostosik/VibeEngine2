[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

$projectPath = Join-Path `
    $repoRoot `
    "Engine.Hosting.Desktop\Engine.Hosting.Desktop.csproj"

$outputDirectory = Join-Path `
    $repoRoot `
    "artifacts\engine-runtime\win-x64"

if (-not (Test-Path $projectPath)) {
    throw "Engine.Hosting.Desktop project was not found: $projectPath"
}

# This directory is generated output, so stale files must not survive rebuilds.
if (Test-Path $outputDirectory) {
    Remove-Item `
        -LiteralPath $outputDirectory `
        -Recurse `
        -Force
}

New-Item `
    -ItemType Directory `
    -Path $outputDirectory `
    -Force | Out-Null

$dotnetArguments = @(
    "publish"
    $projectPath
    "--configuration"
    $Configuration
    "--runtime"
    "win-x64"
    "--self-contained"
    "false"
    "-p:CopyLocalLockFileAssemblies=true"
    "--output"
    $outputDirectory
)

Push-Location $repoRoot

try {
    & dotnet @dotnetArguments

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to publish Engine.Hosting.Desktop. Exit code: $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

$requiredFiles = @(
    "Engine.Hosting.Desktop.dll"
    "Engine.Runtime.dll"
    "Engine.Graphics.OpenGL.dll"
    "Engine.Audio.OpenAL.dll"
    "Engine.Input.SilkNet.dll"
)

$missingFiles = @(
    $requiredFiles | Where-Object {
        -not (Test-Path (Join-Path $outputDirectory $_))
    }
)

if ($missingFiles.Count -gt 0) {
    throw (
        "The engine runtime output is incomplete. Missing files: " +
        ($missingFiles -join ", ")
    )
}

Write-Host ""
Write-Host "Engine runtime distribution prepared:"
Write-Host $outputDirectory
Write-Host ""

Get-ChildItem `
    -LiteralPath $outputDirectory `
    -File `
    -Filter "*.dll" |
    Select-Object -ExpandProperty Name