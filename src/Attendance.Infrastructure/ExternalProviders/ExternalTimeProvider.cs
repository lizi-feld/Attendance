using System.Text.Json;
using Attendance.Application.Exceptions;
using Attendance.Application.Interfaces;
using Attendance.Infrastructure.ExternalProviders.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Attendance.Infrastructure.ExternalProviders;

/// <summary>
/// Retrieves the current Europe/Zurich time from the TimeAPI.io external service.
/// Registered as a typed <see cref="HttpClient"/> — the base address is configured during DI setup.
/// </summary>
/// <remarks>
/// <para>
/// A Polly <see cref="ResiliencePipeline{TResult}"/> with exponential back-off and jitter
/// wraps every outbound HTTP call. If all retries are exhausted, a
/// <see cref="TimeProviderException"/> is thrown to propagate the failure cleanly.
/// </para>
/// <para>
/// <b>Contract:</b> <see cref="DateTime.Now"/> and <see cref="DateTime.UtcNow"/> are
/// never used anywhere in this class or any downstream attendance code.
/// </para>
/// </remarks>
public sealed class ExternalTimeProvider : ITimeProvider
{
    private readonly HttpClient _httpClient;
    private readonly TimeProviderOptions _options;
    private readonly ILogger<ExternalTimeProvider> _logger;
    private readonly ResiliencePipeline<DateTime> _retryPipeline;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of <see cref="ExternalTimeProvider"/>.
    /// </summary>
    /// <param name="httpClient">
    /// Typed HTTP client whose base address is set to <see cref="TimeProviderOptions.BaseUrl"/>
    /// and whose timeout is set to <see cref="TimeProviderOptions.TimeoutSeconds"/> during DI registration.
    /// </param>
    /// <param name="options">Bound configuration from the <c>TimeProvider</c> appsettings section.</param>
    /// <param name="logger">Structured logger for retry and failure events.</param>
    public ExternalTimeProvider(
        HttpClient httpClient,
        IOptions<TimeProviderOptions> options,
        ILogger<ExternalTimeProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _retryPipeline = BuildRetryPipeline();
    }

    /// <inheritdoc />
    /// <exception cref="TimeProviderException">
    /// Thrown when the HTTP call fails on every retry attempt or when the response
    /// body is missing or unparsable.
    /// </exception>
    public async Task<DateTime> GetCurrentTimeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Requesting current time from external provider. TimeZone={TimeZone} BaseUrl={BaseUrl}",
            _options.TimeZone, _options.BaseUrl);

        try
        {
            var zurichTime = await _retryPipeline.ExecuteAsync(
                async ct => await FetchTimeFromApiAsync(ct),
                cancellationToken);

            _logger.LogDebug(
                "External time provider returned {ZurichTime} for TimeZone={TimeZone}",
                zurichTime, _options.TimeZone);

            return zurichTime;
        }
        catch (TimeProviderException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error fetching time from external provider after {MaxRetries} retries. BaseUrl={BaseUrl}",
                _options.MaxRetryAttempts, _options.BaseUrl);

            throw new TimeProviderException(
                $"An unexpected error occurred while retrieving the current time from the external provider ({_options.BaseUrl}).",
                ex);
        }
    }

    private async ValueTask<DateTime> FetchTimeFromApiAsync(CancellationToken cancellationToken)
    {
        var endpoint = $"api/Time/current/zone?timeZone={Uri.EscapeDataString(_options.TimeZone)}";

        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"TimeAPI.io responded with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) for timezone '{_options.TimeZone}'.",
                null,
                response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        var apiResponse = JsonSerializer.Deserialize<TimeApiResponse>(json, JsonOptions);

        if (apiResponse is null || string.IsNullOrWhiteSpace(apiResponse.DateTime))
        {
            throw new InvalidOperationException(
                $"TimeAPI.io returned an empty or invalid response body for timezone '{_options.TimeZone}'. Raw: {json}");
        }

        if (!DateTime.TryParse(apiResponse.DateTime, out var parsed))
        {
            throw new InvalidOperationException(
                $"Could not parse the 'dateTime' value returned by TimeAPI.io: '{apiResponse.DateTime}'.");
        }

        return parsed;
    }

    private ResiliencePipeline<DateTime> BuildRetryPipeline()
    {
        return new ResiliencePipelineBuilder<DateTime>()
            .AddRetry(new RetryStrategyOptions<DateTime>
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(_options.RetryDelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is HttpRequestException or InvalidOperationException),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Time provider transient failure. Attempt={Attempt}/{MaxAttempts} RetryIn={DelayMs}ms Error={Error}",
                        args.AttemptNumber + 1,
                        _options.MaxRetryAttempts,
                        (int)args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown error");

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
