namespace LogOtter.CosmosDb.EventStore;

public class CompactionChangeFeedProcessor<TBaseEvent, TSnapshot>(StreamCompactionService<TBaseEvent, TSnapshot> compactionService)
    : IChangeFeedProcessorChangeHandler<Event<TBaseEvent>>
    where TBaseEvent : class, IEvent<TSnapshot>
    where TSnapshot : class, ISnapshot, new()
{
    public async Task ProcessChanges(IReadOnlyCollection<Event<TBaseEvent>> changes, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            if (change.Body is ICompactionRequestedEvent<TSnapshot> body)
            {
                await compactionService.CompactStream(body.EventStreamId, cancellationToken);
            }
        }
    }
}
