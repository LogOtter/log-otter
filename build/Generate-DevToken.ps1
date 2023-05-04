$ErrorActionPreference = 'Stop'

$solutionDir = Split-Path $PSScriptRoot
$customerApiProjectDir = Join-Path $solutionDir 'sample' 'CustomerApi'
$httpPrivateEnvFilename = Join-Path $customerApiProjectDir 'http-client.private.env.json'

Write-Host 'Generating token...'

Push-Location $customerApiProjectDir
$token = & dotnet user-jwts create --name "Bob Bobertson" --role "Customers.Read" --role "Customers.ReadWrite" --role "Customers.Delete" --role "Customers.Create" --output token
Pop-Location

$httpEnvData = '{
  "dev": {
    "token": "' + $token + '"
  }
}'

Write-Host 'Storing token...'
$httpEnvData | Out-File $httpPrivateEnvFilename

Write-Host 'Done!'
