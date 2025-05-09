﻿using LogOtter.CosmosDb.ContainerMock.ContainerMockData;
using Microsoft.Azure.Cosmos;
using Shouldly;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.Tests;

public class PartitionKeyHelperTests
{
    public static IEnumerable<object[]> PartitionKeys()
    {
        yield return new object[] { new PartitionKey() };
        yield return new object[] { new PartitionKey(true) };
        yield return new object[] { new PartitionKey(false) };
        yield return new object[] { new PartitionKey(double.MinValue) };
        yield return new object[] { new PartitionKey(double.MaxValue) };
        yield return new object[] { new PartitionKey(0) };
        yield return new object[] { new PartitionKey(1) };
        yield return new object[] { new PartitionKey("") };
        yield return new object[] { new PartitionKey(null) };
        yield return new object[] { new PartitionKey("Foo") };
    }

    [Theory]
    [MemberData(nameof(PartitionKeys))]
    public void CorrectlyDeserializes(PartitionKey input)
    {
        var serializedPartitionKey = input.ToString();

        var deserializedPartitionKey = PartitionKeyHelpers.FromJsonString(serializedPartitionKey);

        deserializedPartitionKey.ShouldBe(input);
    }
}
