namespace LogOtter.EventStore.CosmosDb;

public interface IChangeFeedProcessor
{
    Task Start();
    Task Stop();
}