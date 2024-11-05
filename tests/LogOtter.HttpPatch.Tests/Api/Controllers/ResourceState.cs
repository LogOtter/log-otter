using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace LogOtter.HttpPatch.Tests.Api.Controllers;

[JsonConverter(typeof(JsonStringEnumConverter))]
[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
public enum ResourceState
{
    Unpublished,
    Published,
}
