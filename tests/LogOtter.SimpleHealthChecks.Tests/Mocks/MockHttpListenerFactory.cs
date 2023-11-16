namespace LogOtter.SimpleHealthChecks.Tests;

internal class MockHttpListenerFactory(Queue<MockHttpListenerContext> contextQueue) : IHttpListenerFactory
{
    public IHttpListener Create()
    {
        return new MockHttpListener(contextQueue);
    }
}
