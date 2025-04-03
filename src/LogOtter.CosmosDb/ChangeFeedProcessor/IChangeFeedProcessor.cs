namespace LogOtter.CosmosDb;

public interface IChangeFeedProcessor
{
    string Name { get; }
    Task Start();
    Task Stop();
}
