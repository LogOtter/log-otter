using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class StorageEventSerializationTests
{
    private static readonly JsonSerializer JsonSerializer = new() { Formatting = Formatting.Indented };
    private static readonly DateTimeOffset CreatedOn = DateTimeOffset.Parse("2023-04-04T09:00:00+00:00");
    private static readonly Guid EventId = Guid.Parse("66EB0B9D-6806-48EC-97D2-2EDFB2C2DF88");
    private static readonly Dictionary<string, string> Metadata = new() { { "Author", "Bob Bobertson" } };

    private const string StreamId = "/streamId";

    private const string ExampleSerializedJson = """
        {
          "id": "/streamId:0",
          "StreamId": "/streamId",
          "BodyType": "TestEventHappened",
          "EventBody": {
            "Description": "Something happened"
          },
          "Metadata": {
            "Author": "Bob Bobertson"
          },
          "EventNumber": 0,
          "EventId": "66eb0b9d-6806-48ec-97d2-2edfb2c2df88",
          "CreatedOn": "2023-04-04T09:00:00+00:00"
        }
        """;

    private const string Description = "Something happened";

    private record TestEventHappened(string Description);

    [Fact]
    public void CanSerializeStorageEventToJson()
    {
        var eventData = new EventData<TestEventHappened>(EventId, new TestEventHappened(Description), Metadata);
        var storageEvent = new StorageEvent<TestEventHappened>(StreamId, eventData, 0, CreatedOn);

        var serializationTypeMap = new SimpleSerializationTypeMap(new[] { typeof(TestEventHappened) });
        var converter = new StorageEventJsonConverter<TestEventHappened>(serializationTypeMap);
        using var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        converter.WriteJson(jsonWriter, storageEvent, JsonSerializer);
        var json = stringWriter.ToString();

        json.Replace("\r", "").Should().BeEquivalentTo(ExampleSerializedJson.Replace("\r", ""));
    }

    [Fact]
    public void CanDeserializeStorageEventFromJson()
    {
        var serializationTypeMap = new SimpleSerializationTypeMap(new[] { typeof(TestEventHappened) });
        var converter = new StorageEventJsonConverter<TestEventHappened>(serializationTypeMap);

        using var stringReader = new StringReader(ExampleSerializedJson);
        using var jsonReader = new JsonTextReader(stringReader);
        var storageEvent =
            (StorageEvent<TestEventHappened>)converter.ReadJson(jsonReader, typeof(StorageEvent<TestEventHappened>), null, JsonSerializer);

        storageEvent.StreamId.Should().Be(StreamId);
        storageEvent.EventBody.Should().BeOfType<TestEventHappened>();
        storageEvent.EventBody.Should().BeEquivalentTo(new { Description });
        storageEvent.Metadata.Should().BeEquivalentTo(Metadata);
        storageEvent.EventNumber.Should().Be(0);
        storageEvent.EventId.Should().Be(EventId);
        storageEvent.CreatedOn.Should().Be(CreatedOn);
    }
}
