using System.Net;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.Testing.TransactionalBatch;

internal class TestTransactionalBatchResponse : TransactionalBatchResponse
{
    private readonly List<TransactionalBatchOperationResult> _results;
    private HttpStatusCode _statusCode;

    public TestTransactionalBatchResponse()
    {
        _statusCode = HttpStatusCode.OK;
        _results = new List<TransactionalBatchOperationResult>();
    }

    public override HttpStatusCode StatusCode => _statusCode;
    public override int Count => _results.Count;
    public override TransactionalBatchOperationResult this[int index] => _results[index];
    public override IEnumerator<TransactionalBatchOperationResult> GetEnumerator() => _results.GetEnumerator();

    public override TransactionalBatchOperationResult<T> GetOperationResultAtIndex<T>(int index)
    {
        return (TransactionalBatchOperationResult<T>)_results[index];
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