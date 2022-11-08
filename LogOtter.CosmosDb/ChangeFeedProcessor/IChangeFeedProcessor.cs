namespace LogOtter.CosmosDb;

public interface IChangeFeedProcessor
{
    Task Start();
    Task Stop();
}