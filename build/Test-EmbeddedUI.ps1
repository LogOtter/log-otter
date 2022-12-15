param([switch]$SkipNpmInstall)

$ErrorActionPreference = 'Stop'

$solutionDirectory = Split-Path $PSScriptRoot

$embeddedUISrcDirectory = Join-Path $solutionDirectory 'embedded' 'LogOtter.CosmosDb.EventStore.EventStreamUI'
$distDirectory = Join-Path $embeddedUISrcDirectory 'dist'
$outputDirectory = Join-Path $solutionDirectory 'src' 'LogOtter.CosmosDb.EventStore' 'EventStreamUI' 'wwwroot'

Write-Host 'Computing current embedded UI hash...'
$childHash = (Get-ChildItem $outputDirectory -Recurse -File | Get-FileHash -Algorithm MD5).Hash | Out-String
$currentHash = Get-FileHash -InputStream ([IO.MemoryStream]::new([char[]]$childHash))

Push-Location $embeddedUISrcDirectory

try {
  if (!$SkipNpmInstall) {
    Write-Host ''
    Write-Host 'Restoring npm packages...'
    & npm install
  }

  Write-Host ''
  Write-Host 'Building site...'
  & npm run build

  Write-Host 'Computing new embedded UI hash...'
  $childHash = (Get-ChildItem $distDirectory -Recurse -File | Get-FileHash -Algorithm MD5).Hash | Out-String
  $newHash = Get-FileHash -InputStream ([IO.MemoryStream]::new([char[]]$childHash))
}
finally {
  Pop-Location
}

if ($newHash -ne $currentHash) {
  Write-Error 'Embedded UI is out of date - run build/Generate-EmbeddedUI.ps1'
}
else {
  Write-Host 'Embedded UI is up to date'
}

