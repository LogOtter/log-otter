﻿using System.Net;
using System.Net.Http.Json;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore.EventStreamApi.Responses;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CustomerApi.Tests.EventStoreApi;

public class RetrieveEventsFromEventStream(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task CanRetrieveEventStream()
    {
        using var customerApi = new TestCustomerApi(testOutputHelper);

        var customerUri = CustomerUri.Generate();
        await customerApi.Given.AnExistingCustomer(customerUri, emailAddress: "bob@bobertson.co.uk");

        var client = customerApi.CreateClient();
        var response = await client.GetAsync($"/logotter/api/event-streams/CustomerEvent/streams/{customerUri.ToString().Replace("/", "|")}/events");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<EventsResponse>();
        events.ShouldNotBeNull();
        events.Events.Count.ShouldBe(1);
        events.Events.Single().Description.ShouldBe("b****@bobertson.co.uk created");
    }
}
