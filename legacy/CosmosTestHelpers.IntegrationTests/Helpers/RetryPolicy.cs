using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace CosmosTestHelpers.IntegrationTests;

public class RetryPolicy //: ICosmosRetryPolicy
{
    private static readonly IEnumerable<TimeSpan> BackOffPolicy = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(500), retryCount: 5, fastFirst: true);

    private readonly ILogger<RetryPolicy> _logger;

    public RetryPolicy(ILogger<RetryPolicy> logger)
    {
        _logger = logger;
    }

    public async Task<T> CreateAndExecutePolicyAsync<T>(string actionName, Func<Task<T>> action)
    {
        var policy = Polly.Policy
            .Handle<CosmosException>(
                exception => exception.StatusCode == HttpStatusCode.ServiceUnavailable ||
                             exception.StatusCode == HttpStatusCode.RequestTimeout
            )
            .WaitAndRetryAsync(BackOffPolicy, (exception, timeSpan, retryCount, context) =>
            {
                if (!(exception is CosmosException)) return;

                var cosmosException = (CosmosException)exception;
                _logger.LogWarning(
                    "Failed to {actionName} on retry {retryCount} due to {exceptionType} {statusCode} - {exceptionMessage}. Attempting again.", actionName, retryCount, nameof(CosmosException), cosmosException.StatusCode, cosmosException.Message);
            });

        var result = await policy.ExecuteAndCaptureAsync(action);

        if (result.Outcome == OutcomeType.Successful)
        {
            return result.Result;
        }

        throw result.FinalException;
    }

    public T CreateAndExecutePolicy<T>(string actionName, Func<T> action)
    {
        var policy = Polly.Policy
            .Handle<CosmosException>(
                exception => exception.StatusCode == HttpStatusCode.ServiceUnavailable ||
                             exception.StatusCode == HttpStatusCode.RequestTimeout
            )
            .WaitAndRetry(5, i => TimeSpan.FromSeconds(5), (exception, timeSpan, retryCount, context) =>
            {
                if (!(exception is CosmosException)) return;

                var cosmosException = (CosmosException)exception;
                _logger.LogWarning(
                    "Failed to {actionName} on retry {retryCount} due to {exceptionType} {statusCode} - {exceptionMessage}. Attempting again.", actionName, retryCount, nameof(CosmosException), cosmosException.StatusCode, cosmosException.Message);
            });

        var result = policy.ExecuteAndCapture(action);

        if (result.Outcome == OutcomeType.Successful)
        {
            return result.Result;
        }

        throw result.FinalException;
    }
}