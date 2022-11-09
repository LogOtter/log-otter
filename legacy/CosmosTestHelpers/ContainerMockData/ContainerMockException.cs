using System.Net;
using System.Runtime.Serialization;
using Microsoft.Azure.Cosmos;

namespace CosmosTestHelpers.ContainerMockData;

[Serializable]
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

    protected ContainerMockException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }

    public CosmosException ToCosmosException()
    {
        return new CosmosException(Message, StatusCode, 0, string.Empty, 0);
    }
}