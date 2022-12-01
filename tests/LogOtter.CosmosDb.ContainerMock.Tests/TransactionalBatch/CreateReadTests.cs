using System.Net;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.Tests.TransactionalBatch;

public class CreateReadTests
{
    [Fact]
    public async Task CreateItem_ExecuteFails_Rollback()
    {
        var containerMock = new ContainerMock();
        
        var batch = containerMock.CreateTransactionalBatch(new PartitionKey("Group1"))
            .CreateItem(new TestClass { Id = "Foo1", PartitionKey = "Group1", MyValue = "Bar1" })
            .CreateItem(new TestClass { Id = "Foo2", PartitionKey = "Group1", MyValue = "Bar2" })
            .ReadItem("DoesNotExist");
        
        var response = await batch.ExecuteAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        containerMock.GetAllItems<TestClass>().Should().HaveCount(0);
    }
    
    private class TestClass
    {
        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; } = null!;

        public string? MyValue { get; set; }
    }
}