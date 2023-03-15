namespace LogOtter.SimpleHealthChecks;

internal class HttpListenerFactory : IHttpListenerFactory
{
    public IHttpListener Create()
    {
        return new HttpListenerShim();
    }
}
