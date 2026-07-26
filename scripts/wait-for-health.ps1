[CmdletBinding()]
param(
    [string]$Url = "http://localhost:8088/health/ready",
    [int]$Attempts = 40,
    [int]$DelaySeconds = 2
)

$ErrorActionPreference = "Stop"

for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
        if ($response.StatusCode -eq 200) {
            Write-Host "Risk feed is healthy at $Url"
            exit 0
        }
    }
    catch {
        Write-Host "Health attempt $attempt/$Attempts has not succeeded yet."
    }

    Start-Sleep -Seconds $DelaySeconds
}

Write-Error "Risk feed did not become healthy at $Url."
exit 1
