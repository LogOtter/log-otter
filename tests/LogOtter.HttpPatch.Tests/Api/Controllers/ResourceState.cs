namespace LogOtter.HttpPatch.Tests.Api.Controllers;

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum ResourceState
{
    Unpublished,
    Published
}