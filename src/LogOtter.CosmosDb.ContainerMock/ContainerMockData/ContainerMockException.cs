using System.Net;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

public class ContainerMockException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ContainerMockException(HttpStatusCode statusCode)
    {
        StatusCode = statusCode;
    }

    public ContainerMockException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public ContainerMockException(HttpStatusCode statusCode, string message, Exception inner)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }

    public CosmosException ToCosmosException()
    {
        return new CosmosException(Message, StatusCode, 0, string.Empty, 0);
    }
}
