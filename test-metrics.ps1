# Script para probar los endpoints de métricas del Circuit Breaker
# Asegúrate de tener la API corriendo en https://localhost:7001

$baseUrl = "https://localhost:7001"
$username = "admin"
$password = "Admin123!"

Write-Host "🔐 Autenticando..." -ForegroundColor Cyan

# 1. Login para obtener el token
$loginBody = @{
    username = $username
    password = $password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json" `
        -SkipCertificateCheck
    
    $token = $loginResponse.token
    Write-Host "✅ Autenticación exitosa" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "❌ Error en autenticación: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type"  = "application/json"
}

# 2. Obtener estado completo con métricas
Write-Host "📊 Obteniendo estado del Circuit Breaker..." -ForegroundColor Cyan
try {
    $statusResponse = Invoke-RestMethod -Uri "$baseUrl/api/circuitbreaker/whatsapp/status" `
        -Method Get `
        -Headers $headers `
        -SkipCertificateCheck
    
    Write-Host "✅ Estado del Circuit Breaker:" -ForegroundColor Green
    Write-Host "   Estado: $($statusResponse.state)" -ForegroundColor Yellow
    Write-Host "   Mensaje: $($statusResponse.message)" -ForegroundColor White
    Write-Host ""
    
    Write-Host "📈 Métricas Totales:" -ForegroundColor Cyan
    Write-Host "   Total Requests: $($statusResponse.totalMetrics.totalRequests)" -ForegroundColor White
    Write-Host "   Successful: $($statusResponse.totalMetrics.successfulRequests)" -ForegroundColor Green
    Write-Host "   Failed: $($statusResponse.totalMetrics.failedRequests)" -ForegroundColor Red
    Write-Host "   Success Rate: $($statusResponse.totalMetrics.successRate)" -ForegroundColor Green
    Write-Host "   Error Rate: $($statusResponse.totalMetrics.errorRate)" -ForegroundColor Red
    Write-Host ""
    
    Write-Host "⏱️  Métricas de Latencia:" -ForegroundColor Cyan
    Write-Host "   Average: $($statusResponse.latencyMetrics.averageMs) ms" -ForegroundColor White
    Write-Host "   Median: $($statusResponse.latencyMetrics.medianMs) ms" -ForegroundColor White
    Write-Host "   P95: $($statusResponse.latencyMetrics.p95Ms) ms" -ForegroundColor Yellow
    Write-Host "   P99: $($statusResponse.latencyMetrics.p99Ms) ms" -ForegroundColor Yellow
    Write-Host "   Min: $($statusResponse.latencyMetrics.minMs) ms" -ForegroundColor Green
    Write-Host "   Max: $($statusResponse.latencyMetrics.maxMs) ms" -ForegroundColor Red
    Write-Host ""
    
    Write-Host "🔄 Eventos del Circuit Breaker:" -ForegroundColor Cyan
    Write-Host "   Times Opened: $($statusResponse.circuitBreakerEvents.timesOpened)" -ForegroundColor Red
    Write-Host "   Times Half-Opened: $($statusResponse.circuitBreakerEvents.timesHalfOpened)" -ForegroundColor Yellow
    Write-Host "   Total Timeouts: $($statusResponse.circuitBreakerEvents.totalTimeouts)" -ForegroundColor Red
    Write-Host "   Total Retries: $($statusResponse.circuitBreakerEvents.totalRetries)" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "📅 Actividad Reciente (últimos $($statusResponse.recentActivity.windowMinutes) minutos):" -ForegroundColor Cyan
    Write-Host "   Recent Requests: $($statusResponse.recentActivity.recentRequests)" -ForegroundColor White
    Write-Host "   Recent Success Rate: $($statusResponse.recentActivity.recentSuccessRate)" -ForegroundColor Green
    Write-Host "   Recent Error Rate: $($statusResponse.recentActivity.recentErrorRate)" -ForegroundColor Red
    Write-Host ""
    
    if ($statusResponse.topErrors -and $statusResponse.topErrors.PSObject.Properties.Count -gt 0) {
        Write-Host "❌ Top Errores:" -ForegroundColor Cyan
        foreach ($errorItem in $statusResponse.topErrors.PSObject.Properties) {
            Write-Host "   $($errorItem.Name): $($errorItem.Value)" -ForegroundColor Red
        }
        Write-Host ""
    }
    
}
catch {
    Write-Host "❌ Error al obtener estado: $_" -ForegroundColor Red
}

# 3. Obtener configuración
Write-Host "⚙️  Obteniendo configuración del Circuit Breaker..." -ForegroundColor Cyan
try {
    $configResponse = Invoke-RestMethod -Uri "$baseUrl/api/circuitbreaker/whatsapp/config" `
        -Method Get `
        -Headers $headers `
        -SkipCertificateCheck
    
    Write-Host "✅ Configuración:" -ForegroundColor Green
    Write-Host "   Failure Rate Threshold: $($configResponse.configuration.failureRateThreshold)" -ForegroundColor White
    Write-Host "   Duration of Break: $($configResponse.configuration.durationOfBreak)" -ForegroundColor White
    Write-Host "   Max Retries: $($configResponse.configuration.maxRetries)" -ForegroundColor White
    Write-Host "   Timeout: $($configResponse.configuration.timeout)" -ForegroundColor White
    Write-Host ""
    
}
catch {
    Write-Host "❌ Error al obtener configuración: $_" -ForegroundColor Red
}

# 4. Obtener métricas para Prometheus
Write-Host "📊 Obteniendo métricas para Prometheus..." -ForegroundColor Cyan
try {
    $metricsResponse = Invoke-RestMethod -Uri "$baseUrl/api/circuitbreaker/whatsapp/metrics" `
        -Method Get `
        -Headers $headers `
        -SkipCertificateCheck
    
    Write-Host "✅ Métricas Prometheus:" -ForegroundColor Green
    Write-Host "   whatsapp_requests_total: $($metricsResponse.whatsapp_requests_total)" -ForegroundColor White
    Write-Host "   whatsapp_success_rate: $($metricsResponse.whatsapp_success_rate)%" -ForegroundColor Green
    Write-Host "   whatsapp_error_rate: $($metricsResponse.whatsapp_error_rate)%" -ForegroundColor Red
    Write-Host "   whatsapp_latency_average_ms: $($metricsResponse.whatsapp_latency_average_ms)" -ForegroundColor White
    Write-Host "   whatsapp_latency_p95_ms: $($metricsResponse.whatsapp_latency_p95_ms)" -ForegroundColor Yellow
    Write-Host "   whatsapp_latency_p99_ms: $($metricsResponse.whatsapp_latency_p99_ms)" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "📝 Métricas completas guardadas en: metrics_output.json" -ForegroundColor Cyan
    $metricsResponse | ConvertTo-Json -Depth 10 | Out-File -FilePath "metrics_output.json" -Encoding UTF8
    
}
catch {
    Write-Host "❌ Error al obtener métricas: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "✅ Script completado" -ForegroundColor Green
Write-Host "💡 Tip: Puedes usar estos endpoints para integrar con Prometheus, Grafana, o Application Insights" -ForegroundColor Cyan
