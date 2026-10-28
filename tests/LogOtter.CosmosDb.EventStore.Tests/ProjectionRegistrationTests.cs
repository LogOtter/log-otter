using LogOtter.CosmosDb.EventStore.Tests.TestEvents;
using Microsoft.Extensions.DependencyInjection;

namespace LogOtter.CosmosDb.EventStore.Tests;

public class ProjectionRegistrationTests
{
    [Fact]
    public void AddingAProjectionForAnEventThatDoesNotApplyToItThrows()
    {
        var builder = new ServiceCollection().AddCosmosDb().AddEventSourcing();

        var exception = Should.Throw<InvalidOperationException>(() =>
            builder.AddEventSource<UnrelatedEvent>("UnrelatedEvents", c => c.AddProjection<TestEventProjection>())
        );

        exception.Message.ShouldBe(
            $"{nameof(TestEventProjection)} cannot be added as a projection of {nameof(UnrelatedEvent)} as {nameof(UnrelatedEvent)} does not implement IEvent<{nameof(TestEventProjection)}>"
        );
    }

    private abstract class UnrelatedEvent;
}
