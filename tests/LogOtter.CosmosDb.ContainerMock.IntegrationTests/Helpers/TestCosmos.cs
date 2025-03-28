﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

public sealed class TestCosmos(CosmosClient client, CosmosClientOptions cosmosClientOptions) : IDisposable
{
    private static readonly IEnumerable<TimeSpan> BackOffPolicy = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(500), 5, fastFirst: true);

    private Container? _realContainer;
    private Container? _testContainer;

    public void Dispose()
    {
        client.Dispose();
    }

    public async Task SetupAsync(string partitionKeyPath, UniqueKeyPolicy? uniqueKeyPolicy = null)
    {
        var response = await CreateCosmosContainer(partitionKeyPath, uniqueKeyPolicy);
        _realContainer = response.Container;
        _testContainer = new ContainerMock(partitionKeyPath, uniqueKeyPolicy, cosmosSerializationOptions: cosmosClientOptions.SerializerOptions);
    }

    public async Task<(CosmosException? realException, CosmosException? testException)> SetupAsyncProducesExceptions(
        string partitionKeyPath,
        UniqueKeyPolicy? uniqueKeyPolicy = null
    )
    {
        CosmosException? realException = null,
            testException = null;

        try
        {
            var response = await CreateCosmosContainer(partitionKeyPath, uniqueKeyPolicy);
            _realContainer = response.Container;
        }
        catch (CosmosException ex)
        {
            realException = ex;
        }

        try
        {
            _testContainer = new ContainerMock(partitionKeyPath, uniqueKeyPolicy);
        }
        catch (CosmosException ex)
        {
            testException = ex;
        }

        return (realException, testException);
    }

    public async Task<(string testETag, string realETag)> GivenAnExistingItem<T>(T item)
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        var realItem = await _realContainer.UpsertItemAsync(item);
        var testItem = await _testContainer.UpsertItemAsync(item);

        return (testItem.ETag, realItem.ETag);
    }

    [SuppressMessage("", "CA1031", Justification = "Need to catch exceptions")]
    public async Task<(IList<T>? realResults, IList<T>? testResults)> WhenExecutingAQuery<T>(
        string? partitionKey,
        Func<IQueryable<T>, IQueryable<T>>? query = null,
        CosmosLinqSerializerOptions? linqSerializerOptions = null
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        Exception? realException = null,
            testException = null;
        IList<T>? realQuery = null,
            inMemoryQuery = null;

        try
        {
            if (query != null)
            {
                realQuery = await _realContainer.QueryAsync<T>(partitionKey, query, linqSerializerOptions).ToListAsync();
            }
            else
            {
                realQuery = await _realContainer.QueryAsync<T>(partitionKey, linqSerializerOptions).ToListAsync();
            }
        }
        catch (Exception ex)
        {
            realException = ex;
        }

        try
        {
            if (query != null)
            {
                inMemoryQuery = await _testContainer.QueryAsync<T>(partitionKey, query, linqSerializerOptions).ToListAsync();
            }
            else
            {
                inMemoryQuery = await _testContainer.QueryAsync<T>(partitionKey, linqSerializerOptions).ToListAsync();
            }
        }
        catch (Exception ex)
        {
            testException = ex;
        }

        if (realException != null || testException != null)
        {
            throw new CosmosEquivalencyException(realException, testException, realQuery, inMemoryQuery);
        }

        return (realQuery, inMemoryQuery);
    }

    [SuppressMessage("", "CA1031", Justification = "Need to catch exceptions")]
    public async Task<(int? realResults, int? testResults)> WhenCountingAQuery<T>(
        string? partitionKey,
        Func<IQueryable<T>, IQueryable<T>>? query = null
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        Exception? realException = null,
            testException = null;
        int? realQuery = null,
            inMemoryQuery = null;

        try
        {
            if (query != null)
            {
                realQuery = await _realContainer.CountAsync(partitionKey, query);
            }
            else
            {
                realQuery = await _realContainer.CountAsync(partitionKey);
            }
        }
        catch (Exception ex)
        {
            realException = ex;
        }

        try
        {
            if (query != null)
            {
                inMemoryQuery = await _testContainer.CountAsync(partitionKey, query);
            }
            else
            {
                inMemoryQuery = await _testContainer.CountAsync(partitionKey);
            }
        }
        catch (Exception ex)
        {
            testException = ex;
        }

        if (realException != null || testException != null)
        {
            throw new CosmosEquivalencyException(realException, testException, realQuery, inMemoryQuery);
        }

        return (realQuery, inMemoryQuery);
    }

    [Obsolete("Testing old query method, until the CreateItemLinqQueryable method is removed")]
    public (IQueryable<T> realResults, IQueryable<T> testResults) WhenExecutingAQuery<T>(Func<IQueryable<T>, IQueryable<T>> query)
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var realQueryable = _realContainer.GetItemLinqQueryable<T>(true);
        var realQuery = query(realQueryable);

        var inMemoryQueryable = _testContainer.GetItemLinqQueryable<T>();
        var inMemoryQuery = query(inMemoryQueryable);

        return (realQuery, inMemoryQuery);
    }

    [Obsolete("Testing old query method, until the CreateItemLinqQueryable method is removed")]
    [SuppressMessage("", "CA1031", Justification = "Need to know if any exception occurs")]
    public (TResult? realResults, Exception? realException, TResult? testResults, Exception? testException) WhenExecutingAQuery<TIn, TResult>(
        Func<IQueryable<TIn>, TResult> query
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        Exception? realException = null,
            testException = null;
        TResult? realQuery = default,
            inMemoryQuery = default;

        try
        {
            var realQueryable = _realContainer.GetItemLinqQueryable<TIn>(true);
            realQuery = query(realQueryable);
        }
        catch (Exception ex)
        {
            realException = ex;
        }

        try
        {
            var inMemoryQueryable = _testContainer.GetItemLinqQueryable<TIn>();
            inMemoryQuery = query(inMemoryQueryable);
        }
        catch (Exception ex)
        {
            testException = ex;
        }

        return (realQuery, realException, inMemoryQuery, testException);
    }

    public async Task<(ItemResponse<T> realResult, ItemResponse<T> testResult)> WhenCreating<T>(
        T testModel,
        PartitionKey? partitionKey = null,
        ItemRequestOptions? testRequestOptions = null,
        ItemRequestOptions? realRequestOptions = null
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        var realResult = await _realContainer.CreateItemAsync(testModel, partitionKey, realRequestOptions);
        var testResult = await _testContainer.CreateItemAsync(testModel, partitionKey, testRequestOptions);
        return (realResult, testResult);
    }

    public async Task<(CosmosException? realException, CosmosException? testException)> WhenCreatingProducesException<T>(
        T testModel,
        PartitionKey? partitionKey = null,
        ItemRequestOptions? testRequestOptions = null,
        ItemRequestOptions? realRequestOptions = null
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        CosmosException? real = null;
        CosmosException? test = null;

        try
        {
            await _realContainer.CreateItemAsync(testModel, partitionKey, realRequestOptions);
        }
        catch (CosmosException exc)
        {
            real = exc;
        }

        try
        {
            await _testContainer.CreateItemAsync(testModel, partitionKey, testRequestOptions);
        }
        catch (CosmosException exc)
        {
            test = exc;
        }

        return (real, test);
    }

    public async Task<(ItemResponse<T> realResult, ItemResponse<T> testResult)> WhenUpserting<T>(
        T testModel,
        ItemRequestOptions? testRequestOptions = null,
        ItemRequestOptions? realRequestOptions = null
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        var realResult = await _realContainer.UpsertItemAsync(testModel, requestOptions: realRequestOptions);
        var testResult = await _testContainer.UpsertItemAsync(testModel, requestOptions: testRequestOptions);
        return (realResult, testResult);
    }

    public async Task<(ItemResponse<T> realResult, ItemResponse<T> testResult)> WhenReplacing<T>(
        T testModel,
        string id,
        ItemRequestOptions? testRequestOptions = null,
        ItemRequestOptions? realRequestOptions = null
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        var realResult = await _realContainer.ReplaceItemAsync(testModel, id, requestOptions: realRequestOptions);
        var testResult = await _testContainer.ReplaceItemAsync(testModel, id, requestOptions: testRequestOptions);
        return (realResult, testResult);
    }

    public async Task<(ItemResponse<T> realResult, ItemResponse<T> testResult)> WhenDeleting<T>(
        string id,
        PartitionKey partitionKey,
        ItemRequestOptions? testRequestOptions = null,
        ItemRequestOptions? realRequestOptions = null
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        var realResult = await _realContainer.DeleteItemAsync<T>(id, partitionKey, realRequestOptions);
        var testResult = await _testContainer.DeleteItemAsync<T>(id, partitionKey, testRequestOptions);
        return (realResult, testResult);
    }

    public async Task<(ResponseMessage realResult, ResponseMessage testResult)> WhenDeletingStream(
        string id,
        PartitionKey partitionKey,
        ItemRequestOptions? testRequestOptions = null,
        ItemRequestOptions? realRequestOptions = null
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        var realResult = await _realContainer.DeleteItemStreamAsync(id, partitionKey, realRequestOptions);
        var testResult = await _testContainer.DeleteItemStreamAsync(id, partitionKey, testRequestOptions);
        return (realResult, testResult);
    }

    public async Task<(ResponseMessage realResult, ResponseMessage testResult)> WhenUpsertingStream(
        Stream stream,
        PartitionKey partitionKey,
        ItemRequestOptions? testRequestOptions = null,
        ItemRequestOptions? realRequestOptions = null
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        var realResult = await _realContainer.UpsertItemStreamAsync(stream, partitionKey, realRequestOptions);
        var testResult = await _testContainer.UpsertItemStreamAsync(stream, partitionKey, testRequestOptions);
        return (realResult, testResult);
    }

    public async Task<(ResponseMessage realResult, ResponseMessage testResult)> WhenCreatingStream(MemoryStream stream, PartitionKey partitionKey)
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        var realResult = await _realContainer.CreateItemStreamAsync(stream, partitionKey);
        var testResult = await _testContainer.CreateItemStreamAsync(stream, partitionKey);
        return (realResult, testResult);
    }

    public async Task<(CosmosException? realException, CosmosException? testException)> WhenUpsertingProducesException<T>(
        T testModel,
        ItemRequestOptions? testRequestOptions = null,
        ItemRequestOptions? realRequestOptions = null
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        CosmosException? real = null;
        CosmosException? test = null;

        try
        {
            await _realContainer.UpsertItemAsync(testModel, requestOptions: realRequestOptions);
        }
        catch (CosmosException exc)
        {
            real = exc;
        }

        try
        {
            await _testContainer.UpsertItemAsync(testModel, requestOptions: testRequestOptions);
        }
        catch (CosmosException exc)
        {
            test = exc;
        }

        return (real, test);
    }

    public async Task<(CosmosException? realException, CosmosException? testException)> WhenReplacingProducesException<T>(
        T testModel,
        string id,
        ItemRequestOptions? testRequestOptions = null,
        ItemRequestOptions? realRequestOptions = null
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        CosmosException? real = null;
        CosmosException? test = null;

        try
        {
            await _realContainer.ReplaceItemAsync(testModel, id, requestOptions: realRequestOptions);
        }
        catch (CosmosException exc)
        {
            real = exc;
        }

        try
        {
            await _testContainer.ReplaceItemAsync(testModel, id, requestOptions: testRequestOptions);
        }
        catch (CosmosException exc)
        {
            test = exc;
        }

        return (real, test);
    }

    public async Task<(CosmosException? realException, CosmosException? testException)> WhenDeletingProducesException<T>(
        string id,
        PartitionKey partitionKey,
        ItemRequestOptions? testRequestOptions = null,
        ItemRequestOptions? realRequestOptions = null
    )
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        CosmosException? real = null;
        CosmosException? test = null;

        try
        {
            await _realContainer.DeleteItemAsync<T>(id, partitionKey, realRequestOptions);
        }
        catch (CosmosException exc)
        {
            real = exc;
        }

        try
        {
            await _testContainer.DeleteItemAsync<T>(id, partitionKey, testRequestOptions);
        }
        catch (CosmosException exc)
        {
            test = exc;
        }

        return (real, test);
    }

    [SuppressMessage("", "CA1031", Justification = "I want to catch all exceptions.")]
    public async Task<(Exception? realException, Exception? testException)> WhenReadItemProducesException<T>(string? id)
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        Exception? real = null;
        Exception? test = null;

        try
        {
            await _realContainer.ReadItemAsync<T>(id, PartitionKey.None);
        }
        catch (Exception exc)
        {
            real = exc;
        }

        try
        {
            await _testContainer.ReadItemAsync<T>(id, PartitionKey.None);
        }
        catch (Exception exc)
        {
            test = exc;
        }

        return (real, test);
    }

    [SuppressMessage("", "CA1031", Justification = "I want to catch all exceptions.")]
    public async Task<(Exception? realException, Exception? testException)> WhenReadItemStreamProducesException(string? id)
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        Exception? real = null;
        Exception? test = null;

        try
        {
            await _realContainer.ReadItemStreamAsync(id, PartitionKey.None);
        }
        catch (Exception exc)
        {
            real = exc;
        }

        try
        {
            await _testContainer.ReadItemStreamAsync(id, PartitionKey.None);
        }
        catch (Exception exc)
        {
            test = exc;
        }

        return (real, test);
    }

    public async Task<(ResponseMessage realException, ResponseMessage testException)> WhenReadItemStream(string id)
    {
        if (_realContainer == null || _testContainer == null)
        {
            throw new Exception("Call SetupAsync() first");
        }

        var real = await _realContainer.ReadItemStreamAsync(id, PartitionKey.None);
        var test = await _testContainer.ReadItemStreamAsync(id, PartitionKey.None);

        return (real, test);
    }

    public async Task CleanupAsync()
    {
        if (_realContainer != null)
        {
            await _realContainer.DeleteContainerAsync();
        }
    }

    private async Task<ContainerResponse> CreateCosmosContainer(string partitionKeyPath, UniqueKeyPolicy? uniqueKeyPolicy)
    {
        // HACK: Since we can't run the CosmosDb in TestContainers on GitHub actions due to a bug in the emulator
        // and running on real cosmos uses too much of the control plane RU/s, we'll split the tests between different databases.
        var dbNameIndex = new Random().Next(1, 10);
        var dbName = typeof(TestCosmos).Assembly.GetName().Name + "_" + dbNameIndex.ToString("00");

        var database = (await client.CreateDatabaseIfNotExistsAsync(dbName, throughput: null)).Database;

        var containerProperties = new ContainerProperties
        {
            Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            PartitionKeyPath = partitionKeyPath,
        };

        if (uniqueKeyPolicy != null)
        {
            containerProperties.UniqueKeyPolicy = uniqueKeyPolicy;
        }

        var policy = Policy
            .Handle<CosmosException>(exception => exception.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(BackOffPolicy);

        var result = await policy.ExecuteAndCaptureAsync(async () =>
        {
            var response = await database.CreateContainerAsync(containerProperties);
            return response;
        });

        if (result.Outcome == OutcomeType.Successful)
        {
            return result.Result;
        }

        throw result.FinalException;
    }
}
