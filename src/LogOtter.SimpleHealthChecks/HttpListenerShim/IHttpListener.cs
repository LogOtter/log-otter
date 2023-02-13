namespace LogOtter.SimpleHealthChecks;

public interface IHttpListener
{
    void Configure(string scheme, string hostname, int port);
    void Start();
    void Stop();
    Task<IHttpListenerContext> GetContextAsync();
}