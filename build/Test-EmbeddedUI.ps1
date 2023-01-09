param([switch]$SkipNpmInstall)

$ErrorActionPreference = 'Stop'

$solutionDirectory = Split-Path $PSScriptRoot

$embeddedUISrcDirectory = Join-Path $solutionDirectory 'embedded' 'LogOtter.CosmosDb.EventStore.EventStreamUI'
$distDirectory = Join-Path $embeddedUISrcDirectory 'dist'
$outputDirectory = Join-Path $solutionDirectory 'src' 'LogOtter.CosmosDb.EventStore' 'EventStreamUI' 'wwwroot'

$currentHashes = @()
$generatedHashes = @()

Push-Location $outputDirectory

try {
  Write-Host 'Computing current embedded UI hash...'
  foreach ($file in (Get-ChildItem $outputDirectory -Recurse -File)) {
    $currentHashes += @{ 
      Filename = Resolve-Path $file.FullName -Relative
      Hash = ($file | Get-FileHash ).Hash 
    }
  }
} 
finally {
  Pop-Location
}

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
}
finally {
  Pop-Location
}

Push-Location $distDirectory

try {
  Write-Host 'Computing new embedded UI hash...'
  foreach ($file in (Get-ChildItem $distDirectory -Recurse -File)) {
    $generatedHashes += @{ 
      Filename = Resolve-Path $file.FullName -Relative
      Hash = ($file | Get-FileHash ).Hash 
    }
  }
}
finally {
  Pop-Location
}

Write-Host ''
Write-Host 'Comparing hashes...'

if ($generatedHashes.Count -ne $currentHashes.Count) {
  Write-Error 'Embedded UI is out of date (different files) - run build/Generate-EmbeddedUI.ps1'
  return
}

foreach($hash in $currentHashes) {
  Write-Host "Checking $($hash.Filename)..."
  $matchingHash = $generatedHashes | where { $_.Filename -eq $hash.Filename }

  if($matchingHash -eq $null) {
    Write-Error "Embedded UI is out of date (missing $($hash.Filename)) - run build/Generate-EmbeddedUI.ps1"
  }

  if($matchingHash.Hash -ne $hash.Hash) {
    Write-Error "Embedded UI is out of date (different hash for $($hash.Filename)) - run build/Generate-EmbeddedUI.ps1"
  }
}

Write-Host 'Embedded UI is up to date'