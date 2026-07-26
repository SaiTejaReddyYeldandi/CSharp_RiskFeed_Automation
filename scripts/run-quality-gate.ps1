[CmdletBinding()]
param(
    [switch]$SkipIntegration
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot

try {
    dotnet restore RiskFeed.sln --locked-mode
    dotnet format RiskFeed.sln --verify-no-changes --no-restore
    dotnet build RiskFeed.sln --configuration Release --no-restore
    dotnet test tests/RiskFeed.UnitTests/RiskFeed.UnitTests.csproj `
        --configuration Release `
        --no-build `
        --logger "trx;LogFileName=unit-tests.trx" `
        /p:CollectCoverage=true `
        /p:CoverletOutputFormat=cobertura `
        /p:Threshold=80 `
        /p:ThresholdType=line

    if (-not $SkipIntegration) {
        dotnet test tests/RiskFeed.IntegrationTests/RiskFeed.IntegrationTests.csproj `
            --configuration Release `
            --no-build `
            --logger "trx;LogFileName=integration-tests.trx"
    }

    Push-Location src/RiskFeed.Web
    try {
        npm ci
        npm run typecheck
        npm run build
    }
    finally {
        Pop-Location
    }

    Write-Host "CI QUALITY GATE PASSED"
}
finally {
    Pop-Location
}
