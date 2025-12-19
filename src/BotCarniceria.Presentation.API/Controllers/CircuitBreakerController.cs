using BotCarniceria.Infrastructure.Resilience;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotCarniceria.Presentation.API.Controllers;

/// <summary>
/// Controlador para monitorear el estado del Circuit Breaker de WhatsApp
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CircuitBreakerController : ControllerBase
{
    private readonly ILogger<CircuitBreakerController> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CircuitBreakerController(
        ILogger<CircuitBreakerController> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Obtiene el estado actual del Circuit Breaker de WhatsApp
    /// Incluye métricas de latencia, tasa de error, y eventos del Circuit Breaker
    /// </summary>
    /// <returns>Estado y métricas completas del Circuit Breaker</returns>
    [HttpGet("whatsapp/status")]
    public IActionResult GetWhatsAppCircuitBreakerStatus()
    {
        try
        {
            // Obtener el servicio decorado
            var whatsAppService = _serviceProvider.GetService<Core.Application.Interfaces.IWhatsAppService>();
            
            if (whatsAppService is WhatsAppServiceWithCircuitBreaker circuitBreakerService)
            {
                var metrics = circuitBreakerService.GetMetrics();
                var state = circuitBreakerService.GetCircuitBreakerState();

                return Ok(new
                {
                    CircuitBreakerActive = true,
                    State = state,
                    Message = GetStateMessage(state),
                    
                    // Métricas generales
                    TotalMetrics = new
                    {
                        TotalRequests = metrics.TotalRequests,
                        SuccessfulRequests = metrics.SuccessfulRequests,
                        FailedRequests = metrics.FailedRequests,
                        SuccessRate = $"{metrics.SuccessRate:F2}%",
                        ErrorRate = $"{metrics.ErrorRate:F2}%"
                    },
                    
                    // Métricas de latencia
                    LatencyMetrics = new
                    {
                        AverageMs = $"{metrics.AverageLatencyMs:F2}",
                        MedianMs = $"{metrics.MedianLatencyMs:F2}",
                        P95Ms = $"{metrics.P95LatencyMs:F2}",
                        P99Ms = $"{metrics.P99LatencyMs:F2}",
                        MinMs = $"{metrics.MinLatencyMs:F2}",
                        MaxMs = $"{metrics.MaxLatencyMs:F2}"
                    },
                    
                    // Eventos del Circuit Breaker
                    CircuitBreakerEvents = new
                    {
                        TimesOpened = metrics.CircuitBreakerOpenCount,
                        TimesHalfOpened = metrics.CircuitBreakerHalfOpenCount,
                        TotalTimeouts = metrics.TimeoutCount,
                        TotalRetries = metrics.RetryCount
                    },
                    
                    // Métricas de ventana reciente (últimos 5 minutos)
                    RecentActivity = new
                    {
                        WindowMinutes = metrics.RecentWindowMinutes,
                        RecentRequests = metrics.RecentRequests,
                        RecentSuccessRate = $"{metrics.RecentSuccessRate:F2}%",
                        RecentErrorRate = $"{metrics.RecentErrorRate:F2}%"
                    },
                    
                    // Top errores
                    TopErrors = metrics.TopErrors,
                    
                    Timestamp = metrics.Timestamp
                });
            }

            return Ok(new
            {
                CircuitBreakerActive = false,
                Message = "Circuit Breaker no está configurado para WhatsApp Service"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estado del Circuit Breaker");
            return StatusCode(500, new { Error = "Error al obtener estado del Circuit Breaker" });
        }
    }

    /// <summary>
    /// Obtiene información detallada sobre la configuración del Circuit Breaker
    /// </summary>
    [HttpGet("whatsapp/config")]
    public IActionResult GetWhatsAppCircuitBreakerConfig()
    {
        try
        {
            var whatsAppService = _serviceProvider.GetService<Core.Application.Interfaces.IWhatsAppService>();
            
            if (whatsAppService is WhatsAppServiceWithCircuitBreaker circuitBreakerService)
            {
                var metrics = circuitBreakerService.GetMetrics();

                return Ok(new
                {
                    Configuration = new
                    {
                        FailureRateThreshold = $"{metrics.SuccessRate}%",
                        DurationOfBreak = "30 segundos",
                        MaxRetries = 3,
                        Timeout = "10 segundos"
                    },
                    Description = new
                    {
                        Purpose = "Prevenir cascadas de fallos en la API de WhatsApp",
                        Behavior = "El circuito se abre cuando la tasa de fallos supera el umbral configurado",
                        Recovery = "Después del período de ruptura, el circuito entra en estado half-open para probar la recuperación"
                    }
                });
            }

            return NotFound(new { Message = "Circuit Breaker no configurado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener configuración del Circuit Breaker");
            return StatusCode(500, new { Error = "Error al obtener configuración" });
        }
    }

    /// <summary>
    /// Obtiene métricas en formato simple para integración con Prometheus u otros sistemas de monitoreo
    /// </summary>
    [HttpGet("whatsapp/metrics")]
    public IActionResult GetWhatsAppMetrics()
    {
        try
        {
            var whatsAppService = _serviceProvider.GetService<Core.Application.Interfaces.IWhatsAppService>();
            
            if (whatsAppService is WhatsAppServiceWithCircuitBreaker circuitBreakerService)
            {
                var metrics = circuitBreakerService.GetMetrics();

                return Ok(new
                {
                    // Contadores
                    whatsapp_requests_total = metrics.TotalRequests,
                    whatsapp_requests_successful = metrics.SuccessfulRequests,
                    whatsapp_requests_failed = metrics.FailedRequests,
                    whatsapp_circuit_breaker_opened_total = metrics.CircuitBreakerOpenCount,
                    whatsapp_circuit_breaker_half_opened_total = metrics.CircuitBreakerHalfOpenCount,
                    whatsapp_timeouts_total = metrics.TimeoutCount,
                    whatsapp_retries_total = metrics.RetryCount,
                    
                    // Tasas
                    whatsapp_success_rate = metrics.SuccessRate,
                    whatsapp_error_rate = metrics.ErrorRate,
                    whatsapp_recent_success_rate = metrics.RecentSuccessRate,
                    whatsapp_recent_error_rate = metrics.RecentErrorRate,
                    
                    // Latencia (en milisegundos)
                    whatsapp_latency_average_ms = metrics.AverageLatencyMs,
                    whatsapp_latency_median_ms = metrics.MedianLatencyMs,
                    whatsapp_latency_p95_ms = metrics.P95LatencyMs,
                    whatsapp_latency_p99_ms = metrics.P99LatencyMs,
                    whatsapp_latency_min_ms = metrics.MinLatencyMs,
                    whatsapp_latency_max_ms = metrics.MaxLatencyMs,
                    
                    // Metadata
                    timestamp = metrics.Timestamp.ToString("o"),
                    recent_window_minutes = metrics.RecentWindowMinutes,
                    recent_requests = metrics.RecentRequests
                });
            }

            return NotFound(new { Message = "Circuit Breaker no configurado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener métricas");
            return StatusCode(500, new { Error = "Error al obtener métricas" });
        }
    }

    private static string GetStateMessage(string state)
    {
        return state switch
        {
            "Closed" => "✅ Sistema operando normalmente. Todas las llamadas a WhatsApp API están permitidas.",
            "Open" => "⚠️ ALERTA: Circuit Breaker ABIERTO. Las llamadas a WhatsApp API están bloqueadas para prevenir cascada de fallos.",
            "HalfOpen" => "🔄 Circuit Breaker en estado de prueba. Verificando si WhatsApp API se ha recuperado.",
            "Isolated" => "🔒 Circuit Breaker aislado manualmente.",
            _ => "Estado desconocido"
        };
    }
}
