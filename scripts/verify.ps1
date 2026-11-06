param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "DocMind.slnx"

Push-Location $repoRoot
try {
    Write-Host "Restoring solution..."
    & dotnet restore $solutionPath
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE."
    }

    Write-Host "Building solution ($Configuration)..."
    & dotnet build $solutionPath --no-restore --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }

    Write-Host "Running tests ($Configuration)..."
    & dotnet test $solutionPath --no-build --no-restore --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
