param(
  [string]$SourcePath = "..\openai-api-service\openapi.yaml"
)

$repoRoot = Split-Path -Parent $PSScriptRoot
$destination = Join-Path $repoRoot "openapi.yaml"

if (-not (Test-Path $SourcePath)) {
  throw "Source OpenAPI file not found: $SourcePath"
}

Copy-Item $SourcePath $destination -Force
Write-Host "Synced OpenAPI to $destination"
