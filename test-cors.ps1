# Script para probar la configuración de CORS

Write-Host "=== Prueba de Configuración CORS ===" -ForegroundColor Cyan
Write-Host ""

$apiUrl = "http://localhost:5000/api/webhook"
$allowedOrigin = "http://localhost:5111"
$deniedOrigin = "http://malicious-site.com"

Write-Host "1. Probando Preflight Request con origen permitido..." -ForegroundColor Yellow
Write-Host "   Origen: $allowedOrigin" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri $apiUrl `
        -Method OPTIONS `
        -Headers @{
        "Origin"                         = $allowedOrigin
        "Access-Control-Request-Method"  = "POST"
        "Access-Control-Request-Headers" = "Content-Type"
    } `
        -UseBasicParsing `
        -ErrorAction Stop

    Write-Host "   ✓ Status Code: $($response.StatusCode)" -ForegroundColor Green
    
    $corsHeaders = $response.Headers | Where-Object { $_.Key -like "*Access-Control*" }
    if ($corsHeaders) {
        Write-Host "   ✓ Headers CORS encontrados:" -ForegroundColor Green
        foreach ($header in $corsHeaders) {
            Write-Host "     - $($header.Key): $($header.Value)" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Host "   ✗ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "2. Probando Preflight Request con origen NO permitido..." -ForegroundColor Yellow
Write-Host "   Origen: $deniedOrigin" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri $apiUrl `
        -Method OPTIONS `
        -Headers @{
        "Origin"                         = $deniedOrigin
        "Access-Control-Request-Method"  = "POST"
        "Access-Control-Request-Headers" = "Content-Type"
    } `
        -UseBasicParsing `
        -ErrorAction Stop

    Write-Host "   ⚠ Status Code: $($response.StatusCode)" -ForegroundColor Yellow
    
    $allowOriginHeader = $response.Headers["Access-Control-Allow-Origin"]
    if (-not $allowOriginHeader) {
        Write-Host "   ✓ CORS bloqueado correctamente (sin header Access-Control-Allow-Origin)" -ForegroundColor Green
    }
    else {
        Write-Host "   ✗ ADVERTENCIA: Origen no permitido pero CORS no bloqueó" -ForegroundColor Red
        Write-Host "     Access-Control-Allow-Origin: $allowOriginHeader" -ForegroundColor Red
    }
}
catch {
    Write-Host "   ✓ Solicitud bloqueada (esperado): $($_.Exception.Message)" -ForegroundColor Green
}

Write-Host ""
Write-Host "3. Probando POST Request con origen permitido..." -ForegroundColor Yellow
Write-Host "   Origen: $allowedOrigin" -ForegroundColor Gray

$testPayload = @{
    test      = "data"
    timestamp = (Get-Date).ToString("o")
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri $apiUrl `
        -Method POST `
        -Headers @{
        "Origin"       = $allowedOrigin
        "Content-Type" = "application/json"
    } `
        -Body $testPayload `
        -UseBasicParsing `
        -ErrorAction Stop

    Write-Host "   ✓ Status Code: $($response.StatusCode)" -ForegroundColor Green
    
    $allowOriginHeader = $response.Headers["Access-Control-Allow-Origin"]
    if ($allowOriginHeader -eq $allowedOrigin) {
        Write-Host "   ✓ Header Access-Control-Allow-Origin correcto: $allowOriginHeader" -ForegroundColor Green
    }
}
catch {
    Write-Host "   ⚠ Error: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   (Esto puede ser normal si el endpoint requiere autenticación)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== Fin de las pruebas ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "NOTA: Para que estas pruebas funcionen correctamente:" -ForegroundColor Yellow
Write-Host "  1. La API debe estar ejecutándose" -ForegroundColor Gray
Write-Host "  2. Los cambios de CORS deben estar aplicados" -ForegroundColor Gray
Write-Host "  3. Reinicia la API si hiciste cambios en la configuración" -ForegroundColor Gray
