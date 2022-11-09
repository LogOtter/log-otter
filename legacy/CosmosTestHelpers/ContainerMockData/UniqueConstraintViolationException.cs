using System.Net;
using System.Runtime.Serialization;

namespace CosmosTestHelpers.ContainerMockData;

[Serializable]
public class UniqueConstraintViolationException : ContainerMockException
{
    public const HttpStatusCode DefaultStatusCode = HttpStatusCode.Conflict;
        
    public UniqueConstraintViolationException()
        : base(DefaultStatusCode, "Conflict")
    {
    }

    public UniqueConstraintViolationException(string message) 
        : base(DefaultStatusCode, message)
    {
    }

    public UniqueConstraintViolationException(string message, Exception inner) 
        : base(DefaultStatusCode, message, inner)
    {
    }

    protected UniqueConstraintViolationException(
        SerializationInfo info,
        StreamingContext context) 
        : base(info, context)
    {
    }
}