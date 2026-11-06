$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    Set-Location (Join-Path $repoRoot "tests/DocMind.Api.Tests")
    Write-Host "Restoring, building, and testing DocMind.Api..."
    dotnet test --configuration Release --logger "console;verbosity=normal"
    if ($LASTEXITCODE -ne 0) {
        throw "Verification failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
