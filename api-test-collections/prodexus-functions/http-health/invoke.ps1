$BaseUrl = "http://localhost:7071/api"
$FunctionKey = $env:FUNCTIONS_KEY
$Headers = @{}

if ($FunctionKey) {
  $Headers["x-functions-key"] = $FunctionKey
}

Invoke-RestMethod -Method Get -Uri "$BaseUrl/health" -Headers $Headers
