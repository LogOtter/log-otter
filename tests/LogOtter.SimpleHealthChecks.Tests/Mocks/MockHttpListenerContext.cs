namespace LogOtter.SimpleHealthChecks.Tests;

internal class MockHttpListenerContext(MockHttpListenerRequest request) : IHttpListenerContext
{
    public IHttpListenerRequest Request { get; } = request;

    public IHttpListenerResponse Response { get; } = new MockHttpListenerResponse();
}
