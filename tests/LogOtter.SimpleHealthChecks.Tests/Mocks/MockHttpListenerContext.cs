namespace LogOtter.SimpleHealthChecks.Tests;

internal class MockHttpListenerContext : IHttpListenerContext
{
    public IHttpListenerRequest Request { get; }

    public IHttpListenerResponse Response { get; }

    public MockHttpListenerContext(MockHttpListenerRequest request)
    {
        Request = request;
        Response = new MockHttpListenerResponse();
    }
}
