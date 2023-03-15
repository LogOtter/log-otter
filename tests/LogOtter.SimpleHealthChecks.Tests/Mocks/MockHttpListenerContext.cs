namespace LogOtter.SimpleHealthChecks.Tests;

internal class MockHttpListenerContext : IHttpListenerContext
{
    public MockHttpListenerContext(MockHttpListenerRequest request)
    {
        Request = request;
        Response = new MockHttpListenerResponse();
    }

    public IHttpListenerRequest Request { get; }

    public IHttpListenerResponse Response { get; }
}
