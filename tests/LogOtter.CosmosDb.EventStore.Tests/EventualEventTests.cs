using FluentAssertions;
using Xunit;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class EventualEventTests
{
    [Fact]
    public async Task InOrderEvents()
    {
        using var setup = new TestSetup();
        var eventualEventRepository = setup.EventualEventRepository;

        var messageId = Guid.NewGuid().ToString();

        var messageEvents = new[]
        {
            new MessageEvent(Guid.NewGuid().ToString(), messageId, new DateTimeOffset(2023, 08, 22, 01, 00, 00, TimeSpan.Zero), "Sent"),
            new MessageEvent(Guid.NewGuid().ToString(), messageId, new DateTimeOffset(2023, 08, 22, 01, 00, 00, TimeSpan.Zero), "Delivered"),
            new MessageEvent(Guid.NewGuid().ToString(), messageId, new DateTimeOffset(2023, 08, 22, 02, 00, 00, TimeSpan.Zero), "Opened"),
            new MessageEvent(Guid.NewGuid().ToString(), messageId, new DateTimeOffset(2023, 08, 22, 02, 10, 00, TimeSpan.Zero), "Clicked")
        };

        await eventualEventRepository.ApplyEvents(messageId, messageEvents);

        var message = await eventualEventRepository.Get(messageId);

        message.Should().NotBeNull();
        message!.Id.Should().Be(messageId);
        message.State.Should().Be("Clicked");
    }

    [Fact]
    public async Task OutOfOrderEvents()
    {
        using var setup = new TestSetup();
        var eventualEventRepository = setup.EventualEventRepository;

        var messageId = Guid.NewGuid().ToString();

        var messageEvents = new[]
        {
            new MessageEvent(Guid.NewGuid().ToString(), messageId, new DateTimeOffset(2023, 08, 22, 02, 00, 00, TimeSpan.Zero), "Opened"),
            new MessageEvent(Guid.NewGuid().ToString(), messageId, new DateTimeOffset(2023, 08, 22, 01, 00, 00, TimeSpan.Zero), "Sent"),
            new MessageEvent(Guid.NewGuid().ToString(), messageId, new DateTimeOffset(2023, 08, 22, 02, 10, 00, TimeSpan.Zero), "Clicked"),
            new MessageEvent(Guid.NewGuid().ToString(), messageId, new DateTimeOffset(2023, 08, 22, 01, 00, 00, TimeSpan.Zero), "Delivered")
        };

        await eventualEventRepository.ApplyEvents(messageId, messageEvents);

        var message = await eventualEventRepository.Get(messageId);

        message.Should().NotBeNull();
        message!.Id.Should().Be(messageId);
        message.State.Should().Be("Clicked");
    }

    [Fact]
    public async Task OutOfOrderEvents_SeparateApplyEvents()
    {
        using var setup = new TestSetup();
        var eventualEventRepository = setup.EventualEventRepository;

        var messageId = Guid.NewGuid().ToString();

        var messageEvents = new[]
        {
            new MessageEvent(Guid.NewGuid().ToString(), messageId, new DateTimeOffset(2023, 08, 22, 02, 00, 00, TimeSpan.Zero), "Opened"),
            new MessageEvent(Guid.NewGuid().ToString(), messageId, new DateTimeOffset(2023, 08, 22, 01, 00, 00, TimeSpan.Zero), "Sent"),
            new MessageEvent(Guid.NewGuid().ToString(), messageId, new DateTimeOffset(2023, 08, 22, 02, 10, 00, TimeSpan.Zero), "Clicked"),
            new MessageEvent(Guid.NewGuid().ToString(), messageId, new DateTimeOffset(2023, 08, 22, 01, 00, 00, TimeSpan.Zero), "Delivered")
        };

        foreach (var messageEvent in messageEvents)
        {
            await eventualEventRepository.ApplyEvents(messageId, messageEvent);
        }

        var message = await eventualEventRepository.Get(messageId);

        message.Should().NotBeNull();
        message!.Id.Should().Be(messageId);
        message.State.Should().Be("Clicked");
    }
}
