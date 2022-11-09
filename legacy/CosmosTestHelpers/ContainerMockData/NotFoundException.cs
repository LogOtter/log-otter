using System.Net;
using System.Runtime.Serialization;

namespace CosmosTestHelpers.ContainerMockData;

[Serializable]
public class NotFoundException : ContainerMockException
{
    public const HttpStatusCode DefaultStatusCode = HttpStatusCode.NotFound;
        
    public NotFoundException()
        : base(DefaultStatusCode, "Not found")
    {
    }

    public NotFoundException(string message) 
        : base(DefaultStatusCode, message)
    {
    }

    public NotFoundException(string message, Exception inner) 
        : base(DefaultStatusCode, message, inner)
    {
    }

    protected NotFoundException(
        SerializationInfo info,
        StreamingContext context) 
        : base(info, context)
    {
    }
}