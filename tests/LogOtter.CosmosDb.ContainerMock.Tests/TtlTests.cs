﻿using System.Net;
using System.Text;
using LogOtter.CosmosDb.ContainerMock.Tests.Helpers;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.Tests;

public class TtlTests
{
    public static IEnumerable<object?[]> CreateAndRetrieveTestCases()
    {
        yield return new object?[] { -1, 30, false };
        yield return new object?[] { 30, null, false };
        yield return new object?[] { 30, 30, false };
        yield return new object?[] { 30, -1, true };
        yield return new object?[] { 31, null, true };
        yield return new object?[] { 31, 1, false };
        yield return new object?[] { -1, 31, true };
        yield return new object?[] { 1, 31, true };
    }

    public static IEnumerable<object?[]> UpdateItemExtendExpiryTestCases()
    {
        yield return new object?[] { -1, 30, true };
        yield return new object?[] { 30, null, true };
        yield return new object?[] { 30, 30, true };
        yield return new object?[] { -1, 19, false };
        yield return new object?[] { 19, null, false };
    }

    public static IEnumerable<object?[]> ReplaceItemExtendExpiryTestCases()
    {
        yield return new object?[] { -1, 30, true };
        yield return new object?[] { 30, null, true };
        yield return new object?[] { 30, 30, true };
    }

    [Theory]
    [MemberData(nameof(CreateAndRetrieveTestCases))]
    public async Task ItemRemovedAfterTtlExpires_GetAllItems(int containerTtl, int? itemTtl, bool expectedItemExists)
    {
        var container = new ContainerMock(defaultDocumentTimeToLive: containerTtl);

        var document = new TestDocumentWithTtl
        {
            Id = "MyId",
            PartitionKey = "MyPartitionKey",
            TimeToLive = itemTtl,
        };

        await container.CreateItemAsync(document, cancellationToken: TestContext.Current.CancellationToken);

        container.AdvanceTime(30);

        var results = container.GetAllItems<TestDocumentWithTtl>().ToList();

        var expectedResultsCount = expectedItemExists ? 1 : 0;
        results.Count.ShouldBe(expectedResultsCount);
    }

    [Theory]
    [MemberData(nameof(CreateAndRetrieveTestCases))]
    public async Task ItemRemovedAfterTtlExpires_ReadItemAsync(int containerTtl, int? itemTtl, bool expectedItemExists)
    {
        var container = new ContainerMock(defaultDocumentTimeToLive: containerTtl);

        var document = new TestDocumentWithTtl
        {
            Id = "MyId",
            PartitionKey = "MyPartitionKey",
            TimeToLive = itemTtl,
        };

        await container.CreateItemAsync(document, cancellationToken: TestContext.Current.CancellationToken);

        container.AdvanceTime(30);

        Func<Task> act = async () =>
        {
            await container.ReadItemAsync<TestDocumentWithTtl>(
                document.Id,
                new PartitionKey(document.PartitionKey),
                cancellationToken: TestContext.Current.CancellationToken
            );
        };

        if (expectedItemExists)
        {
            await act.ShouldNotThrowAsync();
        }
        else
        {
            (await act.ShouldThrowAsync<CosmosException>()).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
    }

    [Theory]
    [MemberData(nameof(CreateAndRetrieveTestCases))]
    public async Task ItemRemovedAfterTtlExpires_ReadItemStreamAsync(int containerTtl, int? itemTtl, bool expectedItemExists)
    {
        var container = new ContainerMock(defaultDocumentTimeToLive: containerTtl);

        var document = new TestDocumentWithTtl
        {
            Id = "MyId",
            PartitionKey = "MyPartitionKey",
            TimeToLive = itemTtl,
        };

        await container.CreateItemAsync(document, cancellationToken: TestContext.Current.CancellationToken);

        container.AdvanceTime(30);

        var response = await container.ReadItemStreamAsync(
            document.Id,
            new PartitionKey(document.PartitionKey),
            cancellationToken: TestContext.Current.CancellationToken
        );

        var expectedStatusCode = expectedItemExists ? HttpStatusCode.OK : HttpStatusCode.NotFound;

        response.StatusCode.ShouldBe(expectedStatusCode);
    }

    [Theory]
    [MemberData(nameof(CreateAndRetrieveTestCases))]
    public async Task ItemRemovedAfterTtlExpires_GetItemLinqQueryable(int containerTtl, int? itemTtl, bool expectedItemExists)
    {
        var container = new ContainerMock(defaultDocumentTimeToLive: containerTtl);

        var document = new TestDocumentWithTtl
        {
            Id = "MyId",
            PartitionKey = "MyPartitionKey",
            TimeToLive = itemTtl,
        };

        await container.CreateItemAsync(document, cancellationToken: TestContext.Current.CancellationToken);

        container.AdvanceTime(30);

        var results = container.GetItemLinqQueryable<TestDocumentWithTtl>().Where(d => d.Id == document.Id).ToList();

        var expectedResultsCount = expectedItemExists ? 1 : 0;

        results.Count.ShouldBe(expectedResultsCount);
    }

    [Theory]
    [MemberData(nameof(CreateAndRetrieveTestCases))]
    public async Task ItemRemovedAfterTtlExpires_CountAsync(int containerTtl, int? itemTtl, bool expectedItemExists)
    {
        var container = new ContainerMock(defaultDocumentTimeToLive: containerTtl);

        var document = new TestDocumentWithTtl
        {
            Id = "MyId",
            PartitionKey = "MyPartitionKey",
            TimeToLive = itemTtl,
        };

        await container.CreateItemAsync(document, cancellationToken: TestContext.Current.CancellationToken);

        container.AdvanceTime(30);

        var count = await container.CountAsync<TestDocumentWithTtl>(document.PartitionKey, query => query);

        var expectedCount = expectedItemExists ? 1 : 0;

        count.ShouldBe(expectedCount);
    }

    [Theory]
    [MemberData(nameof(CreateAndRetrieveTestCases))]
    public async Task ItemRemovedAfterTtlExpires_QueryAsync(int containerTtl, int? itemTtl, bool expectedItemExists)
    {
        var container = new ContainerMock(defaultDocumentTimeToLive: containerTtl);

        var document = new TestDocumentWithTtl
        {
            Id = "MyId",
            PartitionKey = "MyPartitionKey",
            TimeToLive = itemTtl,
        };

        await container.CreateItemAsync(document, cancellationToken: TestContext.Current.CancellationToken);

        container.AdvanceTime(30);

        var results = container.QueryAsync<TestDocumentWithTtl, TestDocumentWithTtl>(document.PartitionKey, query => query);
        var items = await results.ToListAsync();

        var expectedResultsCount = expectedItemExists ? 1 : 0;

        items.Count.ShouldBe(expectedResultsCount);
    }

    [Theory]
    [MemberData(nameof(UpdateItemExtendExpiryTestCases))]
    public async Task ItemExistsAfterItemUpdated_UpsertItemAsync(int containerTtl, int? itemTtl, bool expectedItemExists)
    {
        var container = new ContainerMock(defaultDocumentTimeToLive: containerTtl);

        var document = new TestDocumentWithTtl
        {
            Id = "MyId",
            PartitionKey = "MyPartitionKey",
            TimeToLive = itemTtl,
        };

        await container.CreateItemAsync(document, cancellationToken: TestContext.Current.CancellationToken);

        container.AdvanceTime(20);

        await container.UpsertItemAsync(document, cancellationToken: TestContext.Current.CancellationToken);

        container.AdvanceTime(20);

        var response = await container.ReadItemStreamAsync(
            document.Id,
            new PartitionKey(document.PartitionKey),
            cancellationToken: TestContext.Current.CancellationToken
        );

        var expectedStatusCode = expectedItemExists ? HttpStatusCode.OK : HttpStatusCode.NotFound;

        response.StatusCode.ShouldBe(expectedStatusCode);
    }

    [Theory]
    [MemberData(nameof(UpdateItemExtendExpiryTestCases))]
    public async Task ItemExistsAfterItemUpdated_UpsertItemStreamAsync(int containerTtl, int? itemTtl, bool expectedItemExists)
    {
        var container = new ContainerMock(defaultDocumentTimeToLive: containerTtl);

        var document = new TestDocumentWithTtl
        {
            Id = "MyId",
            PartitionKey = "MyPartitionKey",
            TimeToLive = itemTtl,
        };

        await container.CreateItemAsync(document, cancellationToken: TestContext.Current.CancellationToken);

        container.AdvanceTime(20);

        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(document));
        await using var ms = new MemoryStream(bytes);
        await container.UpsertItemStreamAsync(ms, new PartitionKey(document.PartitionKey), cancellationToken: TestContext.Current.CancellationToken);

        container.AdvanceTime(20);

        var response = await container.ReadItemStreamAsync(
            document.Id,
            new PartitionKey(document.PartitionKey),
            cancellationToken: TestContext.Current.CancellationToken
        );

        var expectedStatusCode = expectedItemExists ? HttpStatusCode.OK : HttpStatusCode.NotFound;

        response.StatusCode.ShouldBe(expectedStatusCode);
    }

    [Theory]
    [MemberData(nameof(ReplaceItemExtendExpiryTestCases))]
    public async Task ItemExistsAfterItemUpdated_ReplaceItemAsync(int containerTtl, int? itemTtl, bool expectedItemExists)
    {
        var container = new ContainerMock(defaultDocumentTimeToLive: containerTtl);

        var document = new TestDocumentWithTtl
        {
            Id = "MyId",
            PartitionKey = "MyPartitionKey",
            TimeToLive = itemTtl,
        };

        await container.CreateItemAsync(document, cancellationToken: TestContext.Current.CancellationToken);

        container.AdvanceTime(20);

        await container.ReplaceItemAsync(
            document,
            document.Id,
            new PartitionKey(document.PartitionKey),
            cancellationToken: TestContext.Current.CancellationToken
        );

        container.AdvanceTime(20);

        var response = await container.ReadItemStreamAsync(
            document.Id,
            new PartitionKey(document.PartitionKey),
            cancellationToken: TestContext.Current.CancellationToken
        );

        var expectedStatusCode = expectedItemExists ? HttpStatusCode.OK : HttpStatusCode.NotFound;

        response.StatusCode.ShouldBe(expectedStatusCode);
    }

    [Theory]
    [MemberData(nameof(CreateAndRetrieveTestCases))]
    public async Task ItemRemovedAfterTtlExpires_CreateItemStreamAsync_GetAllItems(int containerTtl, int? itemTtl, bool expectedItemExists)
    {
        var container = new ContainerMock(defaultDocumentTimeToLive: containerTtl);

        var document = new TestDocumentWithTtl
        {
            Id = "MyId",
            PartitionKey = "MyPartitionKey",
            TimeToLive = itemTtl,
        };

        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(document));
        await using var ms = new MemoryStream(bytes);
        await container.CreateItemStreamAsync(ms, new PartitionKey(document.PartitionKey), cancellationToken: TestContext.Current.CancellationToken);

        container.AdvanceTime(30);

        var results = container.GetAllItems<TestDocumentWithTtl>().ToList();

        var expectedResultsCount = expectedItemExists ? 1 : 0;
        results.Count.ShouldBe(expectedResultsCount);
    }

    private class TestDocumentWithTtl
    {
        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; } = null!;

        [JsonProperty("ttl", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimeToLive { get; set; }
    }
}
