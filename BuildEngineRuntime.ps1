[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("win-x64")]
    [string]$RuntimeIdentifier = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..")
)

$projectPath = Join-Path `
    $repoRoot `
    "Engine.Hosting.Desktop\Engine.Hosting.Desktop.csproj"

$outputDirectory = Join-Path `
    $repoRoot `
    "artifacts\engine-runtime\$RuntimeIdentifier"

if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "Desktop host project was not found: $projectPath"
}

$requiredAssemblies = @(
    "Engine.Hosting.Desktop.dll"
    "Engine.Runtime.dll"
    "Engine.Graphics.OpenGL.dll"
    "Engine.Audio.OpenAL.dll"
    "Engine.Input.SilkNet.dll"
)

Write-Host "VibeEngine2 runtime build"
Write-Host "Configuration: $Configuration"
Write-Host "Runtime:       $RuntimeIdentifier"
Write-Host "Output:        $outputDirectory"
Write-Host ""

# Remove only the generated runtime output so obsolete DLLs
# from a previous build cannot remain in the distribution.
if (Test-Path -LiteralPath $outputDirectory) {
    Remove-Item `
        -LiteralPath $outputDirectory `
        -Recurse `
        -Force
}

$outputParent = Split-Path -Parent $outputDirectory

New-Item `
    -ItemType Directory `
    -Path $outputParent `
    -Force | Out-Null

& dotnet publish `
    $projectPath `
    --configuration $Configuration `
    --runtime $RuntimeIdentifier `
    --self-contained false `
    "-p:CopyLocalLockFileAssemblies=true" `
    --output $outputDirectory

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$missingAssemblies = @(
    $requiredAssemblies | Where-Object {
        -not (
            Test-Path -LiteralPath (
                Join-Path $outputDirectory $_
            ) -PathType Leaf
        )
    }
)

if ($missingAssemblies.Count -gt 0) {
    throw (
        "Runtime build finished, but required assemblies are missing: " +
        ($missingAssemblies -join ", ")
    )
}

Write-Host ""
Write-Host "Runtime distribution created successfully." `
    -ForegroundColor Green

Write-Host "Location: $outputDirectory"
Write-Host "Assemblies: $(
    (Get-ChildItem -LiteralPath $outputDirectory -Filter '*.dll' -File).Count
)"