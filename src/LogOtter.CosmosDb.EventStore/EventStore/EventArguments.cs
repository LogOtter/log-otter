namespace LogOtter.CosmosDb.EventStore;

internal static class EventArguments
{
    public const string AtLeastOneEventRequired =
        "At least one event must be provided when applying events, make sure you pass at least one event, or an IEnumerable or array with at least 1 event in it.";

    public static List<T> ToNonEmptyListOrThrow<T>(IEnumerable<T> events)
    {
        var eventList = events.ToList();
        return eventList.Count == 0 ? throw new ArgumentException(AtLeastOneEventRequired, nameof(events)) : eventList;
    }
}
