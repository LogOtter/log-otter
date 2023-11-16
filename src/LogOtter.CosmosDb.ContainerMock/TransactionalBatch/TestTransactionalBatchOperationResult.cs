using System.Net;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.ContainerMock.TransactionalBatch;

internal class TestTransactionalBatchOperationResult(ResponseMessage responseMessage) : TransactionalBatchOperationResult
{
    public override HttpStatusCode StatusCode => responseMessage.StatusCode;
    public override Stream ResourceStream => responseMessage.Content;
    public override string ETag => responseMessage.Headers.ETag;
}

internal class TestTransactionalBatchOperationResult<T>(TransactionalBatchOperationResult result, T resource) : TransactionalBatchOperationResult<T>
{
    private readonly string _eTag = result.ETag;
    private readonly Stream _resourceStream = result.ResourceStream;
    private readonly HttpStatusCode _statusCode = result.StatusCode;

    public sealed override T Resource { get; set; } = resource;

    public override HttpStatusCode StatusCode => _statusCode;

    public override string ETag => _eTag;

    public override Stream ResourceStream => _resourceStream;
}
