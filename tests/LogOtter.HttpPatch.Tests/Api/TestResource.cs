using System.Collections.Immutable;
using LogOtter.HttpPatch.Tests.Api.Controllers;

namespace LogOtter.HttpPatch.Tests.Api;

public record TestResource(string Id, string Name, string? Description, int Count, Address Address, ResourceState State, ImmutableList<string> People);