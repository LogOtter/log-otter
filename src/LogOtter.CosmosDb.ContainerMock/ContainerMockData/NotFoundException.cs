using System.Net;

namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

public class NotFoundException : ContainerMockException
{
    public const HttpStatusCode DefaultStatusCode = HttpStatusCode.NotFound;

    public NotFoundException()
        : base(DefaultStatusCode, "Not found") { }

    public NotFoundException(string message)
        : base(DefaultStatusCode, message) { }

    public NotFoundException(string message, Exception inner)
        : base(DefaultStatusCode, message, inner) { }
}
