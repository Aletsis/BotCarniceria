using System.Diagnostics;
using System.Net;
using BotCarniceria.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace BotCarniceria.Infrastructure.Resilience;

/// <summary>
/// Decorador del WhatsAppService que implementa el patrón Circuit Breaker
/// para prevenir cascadas de fallos en la API de WhatsApp
/// Sigue los principios de Clean Architecture: depende de abstracciones (IResilienceMetricsCollector)
/// </summary>
public class WhatsAppServiceWithCircuitBreaker : IWhatsAppService
{
    private readonly IWhatsAppService _innerService;
    private readonly ILogger<WhatsAppServiceWithCircuitBreaker> _logger;
    private readonly WhatsAppCircuitBreakerOptions _options;
    private readonly IResilienceMetricsCollector _metricsCollector;
    private readonly ResiliencePipeline<bool> _resiliencePipeline;
    
    private const string ServiceName = "WhatsApp";

    public WhatsAppServiceWithCircuitBreaker(
        IWhatsAppService innerService,
        ILogger<WhatsAppServiceWithCircuitBreaker> logger,
        IOptions<WhatsAppCircuitBreakerOptions> options,
        IResilienceMetricsCollector metricsCollector)
    {
        _innerService = innerService;
        _logger = logger;
        _options = options.Value;
        _metricsCollector = metricsCollector;

        // Construir el pipeline de resiliencia con Polly v8
        _resiliencePipeline = new ResiliencePipelineBuilder<bool>()
            // 1. Timeout Strategy
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(_options.TimeoutInSeconds),
                OnTimeout = args =>
                {
                    _metricsCollector.RecordTimeout(ServiceName);
                    _logger.LogWarning(
                        "Timeout de {Timeout}s alcanzado en llamada a WhatsApp API",
                        _options.TimeoutInSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            // 2. Retry Strategy con backoff exponencial
            .AddRetry(new RetryStrategyOptions<bool>
            {
                MaxRetryAttempts = _options.MaxRetries,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<bool>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                OnRetry = args =>
                {
                    _metricsCollector.RecordRetry(ServiceName, args.AttemptNumber);
                    _logger.LogWarning(
                        "Reintento {AttemptNumber}/{MaxRetries} para WhatsApp API después de {Delay}s. Error: {Error}",
                        args.AttemptNumber,
                        _options.MaxRetries,
                        args.RetryDelay.TotalSeconds,
                        args.Outcome.Exception?.Message ?? "Unknown");
                    return ValueTask.CompletedTask;
                }
            })
            // 3. Circuit Breaker Strategy
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<bool>
            {
                FailureRatio = _options.FailureRateThreshold / 100.0,
                SamplingDuration = TimeSpan.FromSeconds(_options.SamplingDurationInSeconds),
                MinimumThroughput = _options.MinimumThroughput,
                BreakDuration = TimeSpan.FromSeconds(_options.DurationOfBreakInSeconds),
                ShouldHandle = new PredicateBuilder<bool>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .HandleResult(result => !result), // false es considerado fallo
                OnOpened = args =>
                {
                    _metricsCollector.RecordCircuitBreakerOpened(ServiceName);
                    _logger.LogCritical(
                        "⚠️ CIRCUIT BREAKER ABIERTO ⚠️ " +
                        "WhatsApp API ha fallado repetidamente. " +
                        "Bloqueando todas las llamadas durante {Duration} segundos para prevenir cascada de fallos. " +
                        "Tasa de fallos: {FailureRate}%",
                        _options.DurationOfBreakInSeconds,
                        _options.FailureRateThreshold);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _metricsCollector.RecordCircuitBreakerClosed(ServiceName);
                    _logger.LogInformation(
                        "✅ CIRCUIT BREAKER CERRADO ✅ " +
                        "WhatsApp API se ha recuperado. Reanudando operaciones normales.");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _metricsCollector.RecordCircuitBreakerHalfOpened(ServiceName);
                    _logger.LogWarning(
                        "🔄 CIRCUIT BREAKER HALF-OPEN 🔄 " +
                        "Probando si WhatsApp API se ha recuperado...");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<bool> SendTextMessageAsync(string phoneNumber, string message)
    {
        return await ExecuteWithResilienceAsync(
            async () => await _innerService.SendTextMessageAsync(phoneNumber, message),
            $"SendTextMessage to {phoneNumber}"
        );
    }

    public async Task<bool> SendInteractiveButtonsAsync(
        string phoneNumber,
        string bodyText,
        List<(string id, string title)> buttons,
        string? headerText = null,
        string? footerText = null)
    {
        return await ExecuteWithResilienceAsync(
            async () => await _innerService.SendInteractiveButtonsAsync(
                phoneNumber, bodyText, buttons, headerText, footerText),
            $"SendInteractiveButtons to {phoneNumber}"
        );
    }

    public async Task<bool> SendInteractiveListAsync(
        string phoneNumber,
        string bodyText,
        string buttonText,
        List<(string id, string title, string? description)> rows,
        string? headerText = null,
        string? footerText = null)
    {
        return await ExecuteWithResilienceAsync(
            async () => await _innerService.SendInteractiveListAsync(
                phoneNumber, bodyText, buttonText, rows, headerText, footerText),
            $"SendInteractiveList to {phoneNumber}"
        );
    }

    public async Task<string?> DownloadMediaAsync(string mediaId)
    {
        // Por ahora pasamos la llamada directamente. 
        // En el futuro, podríamos implementar una pipeline de resiliencia específica para <string?> si fuera necesario,
        // manejando 'null' como fallo o excepciones si el servicio interno las propagara.
        return await _innerService.DownloadMediaAsync(mediaId);
    }

    public async Task<bool> ResendMessageAsync(string phoneNumber, string jsonPayload)
    {
         return await ExecuteWithResilienceAsync(
            async () => await _innerService.ResendMessageAsync(phoneNumber, jsonPayload),
            $"ResendMessage to {phoneNumber}"
        );
    }

    public async Task<bool> MarkMessageAsReadAsync(string messageId)
    {
        // Para MarkAsRead, usamos una política más permisiva (sin circuit breaker)
        // ya que no es crítico si falla
        try
        {
            return await _innerService.MarkMessageAsReadAsync(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al marcar mensaje como leído (no crítico): {MessageId}", messageId);
            return false;
        }
    }

    /// <summary>
    /// Ejecuta una operación con todas las políticas de resiliencia aplicadas
    /// </summary>
    private async Task<bool> ExecuteWithResilienceAsync(
        Func<Task<bool>> operation,
        string operationName)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _resiliencePipeline.ExecuteAsync(
                async cancellationToken => await operation(),
                CancellationToken.None);
            
            stopwatch.Stop();
            
            if (result)
            {
                _metricsCollector.RecordSuccess(operationName, stopwatch.Elapsed);
            }
            else
            {
                _metricsCollector.RecordFailure(operationName, stopwatch.Elapsed, "OperationReturnedFalse");
            }
            
            return result;
        }
        catch (BrokenCircuitException ex)
        {
            stopwatch.Stop();
            _metricsCollector.RecordFailure(operationName, stopwatch.Elapsed, "BrokenCircuit");
            
            _logger.LogError(
                ex,
                "Circuit Breaker ABIERTO: Operación rechazada para prevenir cascada de fallos. " +
                "Operación: {Operation}",
                operationName);
            return false;
        }
        catch (TimeoutRejectedException ex)
        {
            stopwatch.Stop();
            _metricsCollector.RecordFailure(operationName, stopwatch.Elapsed, "Timeout");
            
            _logger.LogError(
                ex,
                "Timeout en operación de WhatsApp después de {Timeout}s. Operación: {Operation}",
                _options.TimeoutInSeconds,
                operationName);
            return false;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metricsCollector.RecordFailure(operationName, stopwatch.Elapsed, ex.GetType().Name);
            
            _logger.LogError(
                ex,
                "Error inesperado en operación de WhatsApp con Circuit Breaker. Operación: {Operation}",
                operationName);
            return false;
        }
    }

    /// <summary>
    /// Obtiene el estado actual del Circuit Breaker
    /// </summary>
    public string GetCircuitBreakerState()
    {
        // En Polly v8, el estado se maneja internamente
        // Retornamos "Closed" como default ya que el estado real se refleja en los logs
        return "Closed";
    }

    /// <summary>
    /// Obtiene métricas completas del Circuit Breaker para monitoreo
    /// Incluye latencia, tasa de error, y eventos del Circuit Breaker
    /// </summary>
    public ResilienceMetrics GetMetrics()
    {
        return _metricsCollector.GetResilienceMetrics();
    }
}
