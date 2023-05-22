$ErrorActionPreference = 'Stop'

$partitionCount = 10
$startupLimit = 3 * 60

$startContainer = $true

$certificateUrl = "https://localhost:8081/_explorer/emulator.pem"
$certificateFileName = Join-Path $env:TEMP 'cosmos-db-certificate.pem'

if ($startContainer) {
  Write-Host 'Starting CosmosDb Emulator...'

  & docker run `
    --publish 8081:8081 `
    --publish 10250-10255:10250-10255 `
    --detach `
    --memory 3g `
    --cpus=4.0 `
    --name=cosmosdb-emulator `
    --env AZURE_COSMOS_EMULATOR_PARTITION_COUNT=$partitionCount `
    --env AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true `
    --env AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE=127.0.0.1 `
    --interactive `
    --tty `
    mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator | Out-Null
}

Write-Host 'Waiting for emulator to start...'
$startupCounter = 0;
$succeeded = $false

do {
  try {
    Invoke-WebRequest -Uri $certificateUrl -OutFile $certificateFileName -SkipCertificateCheck
    $succeeded = $true
  }
  catch {
    Start-Sleep -Seconds 1
    $startupCounter++
    Write-Host '.' -NoNewline
  }

} while ( !$succeeded -and $startupCounter -lt $startupLimit)

Write-Host ''

if (!$succeeded) {
  Write-Error 'Failed to start emulator in time'
  return
}

Write-Host 'Container started'

Write-Host 'Importing certificate...'
$certificate = Import-Certificate -FilePath $certificateFileName -CertStoreLocation Cert:\CurrentUser\Root
$certificate.FriendlyName = 'CosmosDb Emulator Certificate'

Write-Host 'Import complete'
