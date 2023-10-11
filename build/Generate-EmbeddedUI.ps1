param([switch]$SkipNpmInstall)

$ErrorActionPreference = 'Stop'

$solutionDirectory = Split-Path $PSScriptRoot

$embeddedUISrcDirectory = Join-Path $solutionDirectory 'embedded' 'LogOtter.CosmosDb.EventStore.EventStreamUI'
$distDirectory = Join-Path $embeddedUISrcDirectory 'dist/*'

$outputDirectories = @()
$outputDirectories += Join-Path $solutionDirectory 'src' 'LogOtter.CosmosDb.EventStore' 'EventStreamUI' 'wwwroot'
$outputDirectories += Join-Path $solutionDirectory 'src' 'LogOtter.Hub' 'wwwroot'

foreach ($outputDirectory in $outputDirectories) {
  if (Test-Path $outputDirectory) {
    Remove-Item $outputDirectory -Force -Recurse
  }
  New-Item $outputDirectory -ItemType Directory | Out-Null
}

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
  $count = 1
  foreach ($outputDirectory in $outputDirectories) {
    Write-Host "Copying site ($count/$($outputDirectories.Count))..."
    Copy-Item $distDirectory $outputDirectory -Recurse
    $count++
  }
}
finally {
  Pop-Location
}

Write-Host ''
Write-Host 'Done!'
