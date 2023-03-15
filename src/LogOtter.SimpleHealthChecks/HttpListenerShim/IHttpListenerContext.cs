namespace LogOtter.SimpleHealthChecks;

public interface IHttpListenerContext
{
    IHttpListenerRequest Request { get; }
    IHttpListenerResponse Response { get; }
}
