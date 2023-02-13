namespace LogOtter.SimpleHealthChecks.Tests;

internal class MockHttpListenerFactory : IHttpListenerFactory
{
    private readonly Queue<MockHttpListenerContext> _contextQueue;

    public MockHttpListenerFactory(Queue<MockHttpListenerContext> contextQueue)
    {
        _contextQueue = contextQueue;
    }

    public IHttpListener Create()
    {
        return new MockHttpListener(_contextQueue);
    }
}
