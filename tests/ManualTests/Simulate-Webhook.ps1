$apiUrl = "http://localhost:5091/api/webhook"
$payloadPath = ".\payload.json"

if (-not (Test-Path $payloadPath)) {
    Write-Error "Payload file not found at $payloadPath"
    exit 1
}

$payload = Get-Content -Raw $payloadPath
# Remove BOM if present, though -Raw usually handles it well, encoding can be tricky.
# Invoke-RestMethod handles UTF8.

try {
    Write-Host "Sending POST request to $apiUrl..."
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Body $payload -ContentType "application/json"
    Write-Host "Response:" -ForegroundColor Green
    $response | Format-List
}
catch {
    Write-Error "Request failed: $_"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Error "Error Body: $errorBody"
    }
}
