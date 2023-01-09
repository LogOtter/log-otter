param([switch]$SkipNpmInstall)

$ErrorActionPreference = 'Stop'

$solutionDirectory = Split-Path $PSScriptRoot

$embeddedUISrcDirectory = Join-Path $solutionDirectory 'embedded' 'LogOtter.CosmosDb.EventStore.EventStreamUI'
$distDirectory = Join-Path $embeddedUISrcDirectory 'dist'
$outputDirectory = Join-Path $solutionDirectory 'src' 'LogOtter.CosmosDb.EventStore' 'EventStreamUI' 'wwwroot'

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

$errors = @()

try {
  Write-Host 'Comparing files...'

  foreach ($expectedFileInfo in (Get-ChildItem $distDirectory -Recurse -File)) {
    $relativePath = Resolve-Path $expectedFileInfo.FullName -Relative
    Write-Host "  * $relativePath " -NoNewLine

    $expectedFile = (Get-Content $expectedFileInfo.FullName -Raw) -Replace "`r`n", "`n"
    
    $actualFilename = Join-Path $outputDirectory $relativePath
    if(-not (Test-Path $actualFilename)) {
      Write-Host '[MISSING]' -ForegroundColor Red
      $errors += "Missing $relativePath"
      continue
    }

    $actualFile = (Get-Content $actualFilename -Raw) -Replace "`r`n", "`n"

    if($actualFile -ne $expectedFile) {
      Write-Host '[OUT OF DATE]' -ForegroundColor Red
      $errors += "Contents do not match for $relativePath"
      continue
    }

    Write-Host '[SUCCESS]' -ForegroundColor Green
  }
}
finally {
  Pop-Location
}

if($errors.Count -gt 0) {
  
  Write-Host ''
  Write-Host 'Errors:'
  foreach($error in $errors) {
    Write-Host "  * $error"
  }
  Write-Host ''

  Write-Error "Embedded UI is out of date - run build/Generate-EmbeddedUI.ps1"
}

Write-Host 'Embedded UI is up to date'