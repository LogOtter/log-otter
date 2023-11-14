using System.Net;

namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

public class UniqueConstraintViolationException : ContainerMockException
{
    public const HttpStatusCode DefaultStatusCode = HttpStatusCode.Conflict;

    public UniqueConstraintViolationException()
        : base(DefaultStatusCode, "Conflict") { }

    public UniqueConstraintViolationException(string message)
        : base(DefaultStatusCode, message) { }

    public UniqueConstraintViolationException(string message, Exception inner)
        : base(DefaultStatusCode, message, inner) { }
}
