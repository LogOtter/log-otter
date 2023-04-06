namespace LogOtter.CosmosDb.EventStore;

public interface IEventStoreReader
{
    Task<IReadOnlyCollection<IStorageEvent>> ReadStreamForwards(string streamId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<IStorageEvent>> ReadStreamForwards(
        string streamId,
        int startPosition,
        int numberOfEventsToRead,
        CancellationToken cancellationToken = default
    );

    Task<int> ReadStreamEventCount(string streamId);

    Task<IStorageEvent> ReadEventFromStream(string streamId, Guid eventId, CancellationToken cancellationToken = default);
}
