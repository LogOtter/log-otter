using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

public class RetryPolicy //: ICosmosRetryPolicy
{
    private static readonly IEnumerable<TimeSpan> BackOffPolicy = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(500), 5, fastFirst: true);

    private readonly ILogger<RetryPolicy> _logger;

    public RetryPolicy(ILogger<RetryPolicy> logger)
    {
        _logger = logger;
    }

    public async Task<T> CreateAndExecutePolicyAsync<T>(string actionName, Func<Task<T>> action)
    {
        var policy = Policy
                     .Handle<CosmosException>(exception => exception.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.RequestTimeout)
                     .WaitAndRetryAsync(
                         BackOffPolicy,
                         (exception, _, retryCount, _) =>
                         {
                             if (exception is not CosmosException cosmosException)
                             {
                                 return;
                             }

                             _logger.LogWarning(
                                 "Failed to {ActionName} on retry {RetryCount} due to {ExceptionType} {StatusCode} - {ExceptionMessage}. Attempting again",
                                 actionName,
                                 retryCount,
                                 nameof(CosmosException),
                                 cosmosException.StatusCode,
                                 cosmosException.Message);
                         });

        var result = await policy.ExecuteAndCaptureAsync(action);

        if (result.Outcome == OutcomeType.Successful)
        {
            return result.Result;
        }

        throw result.FinalException;
    }
}
