using System.Net;
using System.Runtime.Serialization;

namespace CosmosTestHelpers.ContainerMockData;

[Serializable]
public class ETagMismatchException : ContainerMockException
{
    public const HttpStatusCode DefaultStatusCode = HttpStatusCode.PreconditionFailed;
        
    public ETagMismatchException()
        : base(DefaultStatusCode, "Precondition failed")
    {
    }

    public ETagMismatchException(string message) 
        : base(DefaultStatusCode, message)
    {
    }

    public ETagMismatchException(string message, Exception inner) 
        : base(DefaultStatusCode, message, inner)
    {
    }

    protected ETagMismatchException(
        SerializationInfo info,
        StreamingContext context) 
        : base(info, context)
    {
    }
}