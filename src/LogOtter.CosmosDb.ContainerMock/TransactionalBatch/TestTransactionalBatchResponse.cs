﻿using System.Net;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace LogOtter.CosmosDb.ContainerMock.TransactionalBatch;

internal class TestTransactionalBatchResponse(StringSerializationHelper serializationHelper) : TransactionalBatchResponse
{
    private readonly List<TransactionalBatchOperationResult> _results = new();
    private HttpStatusCode _statusCode = HttpStatusCode.OK;

    public override HttpStatusCode StatusCode => _statusCode;
    public override int Count => _results.Count;
    public override TransactionalBatchOperationResult this[int index] => _results[index];

    public override IEnumerator<TransactionalBatchOperationResult> GetEnumerator()
    {
        return _results.GetEnumerator();
    }

    public override TransactionalBatchOperationResult<T> GetOperationResultAtIndex<T>(int index)
    {
        var result = _results[index];
        if (result is TransactionalBatchOperationResult<T> typedResult)
        {
            return typedResult;
        }

        var streamReader = new StreamReader(result.ResourceStream);
        var json = streamReader.ReadToEnd();

        var resource = serializationHelper.DeserializeObject<T>(json);
        return new TestTransactionalBatchOperationResult<T>(result, resource);
    }

    public void AddResult<T>(ResponseMessage responseMessage, T item)
    {
        if (!responseMessage.IsSuccessStatusCode)
        {
            _statusCode = responseMessage.StatusCode;
        }

        _results.Add(new TestTransactionalBatchOperationResult<T>(new TestTransactionalBatchOperationResult(responseMessage), item));
    }

    public void AddResult(ResponseMessage responseMessage)
    {
        if (!responseMessage.IsSuccessStatusCode)
        {
            _statusCode = responseMessage.StatusCode;
        }

        _results.Add(new TestTransactionalBatchOperationResult(responseMessage));
    }

    public void SetException(CosmosException exception)
    {
        _statusCode = exception.StatusCode;
    }

    public void SetException(Exception exception)
    {
        _statusCode = HttpStatusCode.InternalServerError;
    }
}
