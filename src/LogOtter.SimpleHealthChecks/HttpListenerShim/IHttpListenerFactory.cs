namespace LogOtter.SimpleHealthChecks;

public interface IHttpListenerFactory
{
    IHttpListener Create();
}