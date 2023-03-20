using System.Text;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace LogOtter.CosmosDb.ContainerMock.TransactionalBatch;

public class TestTransactionalBatch : Microsoft.Azure.Cosmos.TransactionalBatch
{
    private readonly Queue<Action<TestTransactionalBatchResponse>> _actions;
    private readonly ContainerMock _containerMock;
    private readonly PartitionKey _partitionKey;

    public TestTransactionalBatch(PartitionKey partitionKey, ContainerMock containerMock)
    {
        _partitionKey = partitionKey;
        _containerMock = containerMock;

        _actions = new Queue<Action<TestTransactionalBatchResponse>>();
    }

    public override Microsoft.Azure.Cosmos.TransactionalBatch CreateItem<T>(T item, TransactionalBatchItemRequestOptions? requestOptions = null)
    {
        var itemRequestOptions = CreateItemRequestOptions(requestOptions);

        _actions.Enqueue(response =>
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));

            using var ms = new MemoryStream(bytes);

            var itemResponse = _containerMock.CreateItemStreamAsync(ms, _partitionKey, itemRequestOptions).GetAwaiter().GetResult();

            response.AddResult(itemResponse, item);
        });

        return this;
    }

    public override Microsoft.Azure.Cosmos.TransactionalBatch ReadItem(string id, TransactionalBatchItemRequestOptions? requestOptions = null)
    {
        var itemRequestOptions = CreateItemRequestOptions(requestOptions);

        _actions.Enqueue(response =>
        {
            var itemResponse = _containerMock.ReadItemStreamAsync(id, _partitionKey, itemRequestOptions).GetAwaiter().GetResult();

            response.AddResult(itemResponse);
        });

        return this;
    }

    public override Task<TransactionalBatchResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(null, cancellationToken);
    }

    public override Task<TransactionalBatchResponse> ExecuteAsync(
        TransactionalBatchRequestOptions? requestOptions,
        CancellationToken cancellationToken = default
    )
    {
        var snapshot = _containerMock.CreateSnapshot();

        var response = new TestTransactionalBatchResponse();

        while (_actions.Any())
        {
            var action = _actions.Dequeue();
            try
            {
                action(response);
            }
            catch (Exception ex)
            {
                response.SetException(ex);
                break;
            }

            if (!response.IsSuccessStatusCode)
            {
                break;
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            _containerMock.RestoreSnapshot(snapshot);
        }

        return Task.FromResult<TransactionalBatchResponse>(response);
    }

    private static ItemRequestOptions? CreateItemRequestOptions(TransactionalBatchItemRequestOptions? requestOptions)
    {
        return requestOptions == null
            ? null
            : new ItemRequestOptions
            {
                AddRequestHeaders = requestOptions.AddRequestHeaders,
                Properties = requestOptions.Properties,
                IndexingDirective = requestOptions.IndexingDirective,
                IfMatchEtag = requestOptions.IfMatchEtag,
                IfNoneMatchEtag = requestOptions.IfNoneMatchEtag,
                EnableContentResponseOnWrite = requestOptions.EnableContentResponseOnWrite
            };
    }

    #region Not Implemented

    public override Microsoft.Azure.Cosmos.TransactionalBatch CreateItemStream(
        Stream streamPayload,
        TransactionalBatchItemRequestOptions? requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override Microsoft.Azure.Cosmos.TransactionalBatch UpsertItem<T>(T item, TransactionalBatchItemRequestOptions? requestOptions = null)
    {
        throw new NotImplementedException();
    }

    public override Microsoft.Azure.Cosmos.TransactionalBatch UpsertItemStream(
        Stream streamPayload,
        TransactionalBatchItemRequestOptions? requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override Microsoft.Azure.Cosmos.TransactionalBatch ReplaceItem<T>(
        string id,
        T item,
        TransactionalBatchItemRequestOptions? requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override Microsoft.Azure.Cosmos.TransactionalBatch ReplaceItemStream(
        string id,
        Stream streamPayload,
        TransactionalBatchItemRequestOptions? requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override Microsoft.Azure.Cosmos.TransactionalBatch DeleteItem(string id, TransactionalBatchItemRequestOptions? requestOptions = null)
    {
        throw new NotImplementedException();
    }

    public override Microsoft.Azure.Cosmos.TransactionalBatch PatchItem(
        string id,
        IReadOnlyList<PatchOperation> patchOperations,
        TransactionalBatchPatchItemRequestOptions? requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    #endregion
}
