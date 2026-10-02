$BaseUrl = "http://localhost:7071/api"
$FunctionKey = $env:FUNCTIONS_KEY
$Headers = @{}

if ($FunctionKey) {
  $Headers["x-functions-key"] = $FunctionKey
}

$Payload = Get-Content -Raw -Path (Join-Path $PSScriptRoot "sample-data.json") | ConvertFrom-Json
$CsvBody = ($Payload | ForEach-Object {
  "$($_.name),$($_.email)"
}) -join "`n"

Invoke-RestMethod -Method Post -Uri "$BaseUrl/ingest/file" -Headers $Headers -ContentType "application/json" -Body $CsvBody
