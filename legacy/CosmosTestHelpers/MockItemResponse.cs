using System.Net;
using Microsoft.Azure.Cosmos;

namespace CosmosTestHelpers;

public class MockItemResponse<T> : ItemResponse<T>
{
    public override T Resource { get; }

    public override HttpStatusCode StatusCode { get; }
        
    public override string ETag { get; }

    public MockItemResponse(T resource)
    {
        Resource = resource;
    }

    public MockItemResponse(T resource, string eTag)
    {
        Resource = resource;
        ETag = eTag;
    }
        
    public MockItemResponse(T resource, HttpStatusCode statusCode)
    {
        Resource = resource;
        StatusCode = statusCode;
    }
        
    public MockItemResponse(HttpStatusCode statusCode)
    {
        StatusCode = statusCode;
    }
        
    public MockItemResponse(T resource, HttpStatusCode statusCode, string eTag)
    {
        Resource = resource;
        StatusCode = statusCode;
        ETag = eTag;
    }
}