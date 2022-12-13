param([switch]$SkipNpmInstall)

$ErrorActionPreference = 'Stop'

$solutionDirectory = Split-Path $PSScriptRoot

$embeddedUISrcDirectory = Join-Path $solutionDirectory 'embedded' 'LogOtter.CosmosDb.EventStore.EventStreamUI'
$distDirectory = Join-Path $embeddedUISrcDirectory 'dist/*'
$outputDirectory = Join-Path $solutionDirectory 'src' 'LogOtter.CosmosDb.EventStore' 'EventStreamUI' 'wwwroot'

if (Test-Path $outputDirectory) {
  Remove-Item $outputDirectory -Force -Recurse
}
New-Item $outputDirectory -ItemType Directory | Out-Null

Push-Location $embeddedUISrcDirectory

try {
  if (!$SkipNpmInstall) {
    Write-Host 'Restoring npm packages...'
    & npm install
  }

  Write-Host ''
  Write-Host 'Building site...'
  & npm run build

  Write-Host ''
  Write-Host 'Copying site...'
  Copy-Item $distDirectory $outputDirectory -Recurse
}
finally {
  Pop-Location
}

Write-Host ''
Write-Host 'Done!'