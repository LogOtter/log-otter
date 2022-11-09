using System.Net;
using Microsoft.Azure.Cosmos;

namespace CosmosTestHelpers;

internal class TestTransactionalBatchOperationResult : TransactionalBatchOperationResult
{
    private readonly ResponseMessage _responseMessage;

    public TestTransactionalBatchOperationResult(ResponseMessage responseMessage)
    {
        _responseMessage = responseMessage;
    }
    
    public override HttpStatusCode StatusCode => _responseMessage.StatusCode;
    public override Stream ResourceStream => _responseMessage.Content;
    public override string ETag => _responseMessage.Headers.ETag;
}