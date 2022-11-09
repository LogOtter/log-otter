using System.Net;
using System.Runtime.Serialization;

namespace CosmosTestHelpers.ContainerMockData;

[Serializable]
public class ObjectAlreadyExistsException : ContainerMockException
{
    public const HttpStatusCode DefaultStatusCode = HttpStatusCode.Conflict;
        
    public ObjectAlreadyExistsException()
        : base(DefaultStatusCode, "Object already exists.")
    {
    }

    public ObjectAlreadyExistsException(string message) 
        : base(DefaultStatusCode, message)
    {
    }

    public ObjectAlreadyExistsException(string message, Exception inner) 
        : base(DefaultStatusCode, message, inner)
    {
    }

    protected ObjectAlreadyExistsException(
        SerializationInfo info,
        StreamingContext context) 
        : base(info, context)
    {
    }
}