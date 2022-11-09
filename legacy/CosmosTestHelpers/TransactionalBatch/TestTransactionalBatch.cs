using System.Text;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace CosmosTestHelpers;

public class TestTransactionalBatch : TransactionalBatch
{
    private readonly PartitionKey _partitionKey;
    private readonly ContainerMock _containerMock;
    private readonly Queue<Action<TestTransactionalBatchResponse>> _actions;
    private readonly JsonSerializer _serializer;

    public TestTransactionalBatch(PartitionKey partitionKey, ContainerMock containerMock)
    {
        _partitionKey = partitionKey;
        _containerMock = containerMock;

        _actions = new Queue<Action<TestTransactionalBatchResponse>>();
        _serializer = new JsonSerializer();
    }

    public override TransactionalBatch CreateItem<T>(
        T item,
        TransactionalBatchItemRequestOptions requestOptions = null
    )
    {
        var itemRequestOptions = CreateItemRequestOptions(requestOptions);

        _actions.Enqueue(response =>
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
            
            using var ms = new MemoryStream(bytes);
            
            var itemResponse = _containerMock
                .CreateItemStreamAsync(ms, _partitionKey, itemRequestOptions)
                .GetAwaiter()
                .GetResult();

            response.AddResult(itemResponse);
        });

        return this;
    }

    public override TransactionalBatch ReadItem(string id, TransactionalBatchItemRequestOptions requestOptions = null)
    {
        var itemRequestOptions = CreateItemRequestOptions(requestOptions);


        _actions.Enqueue(response =>
        {
            var itemResponse = _containerMock
                .ReadItemStreamAsync(id, _partitionKey, itemRequestOptions)
                .GetAwaiter()
                .GetResult();

            response.AddResult(itemResponse);
        });

        return this;
    }

    #region Not Implemented

    public override TransactionalBatch CreateItemStream(
        Stream streamPayload,
        TransactionalBatchItemRequestOptions requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override TransactionalBatch UpsertItem<T>(T item, TransactionalBatchItemRequestOptions requestOptions = null)
    {
        throw new NotImplementedException();
    }

    public override TransactionalBatch UpsertItemStream(
        Stream streamPayload,
        TransactionalBatchItemRequestOptions requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override TransactionalBatch ReplaceItem<T>(
        string id,
        T item,
        TransactionalBatchItemRequestOptions requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override TransactionalBatch ReplaceItemStream(
        string id,
        Stream streamPayload,
        TransactionalBatchItemRequestOptions requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override TransactionalBatch DeleteItem(string id, TransactionalBatchItemRequestOptions requestOptions = null)
    {
        throw new NotImplementedException();
    }

    public override TransactionalBatch PatchItem(
        string id,
        IReadOnlyList<PatchOperation> patchOperations,
        TransactionalBatchPatchItemRequestOptions requestOptions = null
    )
    {
        throw new NotImplementedException();
    }

    #endregion

    public override Task<TransactionalBatchResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(new TransactionalBatchRequestOptions(), cancellationToken);
    }

    public override Task<TransactionalBatchResponse> ExecuteAsync(
        TransactionalBatchRequestOptions requestOptions,
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

    private static ItemRequestOptions CreateItemRequestOptions(TransactionalBatchItemRequestOptions requestOptions)
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
}