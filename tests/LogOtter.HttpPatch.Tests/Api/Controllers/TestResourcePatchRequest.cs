using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace LogOtter.HttpPatch.Tests.Api.Controllers;

public record Address(
    [Required]
    string? Line1,
    [Required]
    string? Postcode);

public record TestResourcePatchRequest(
    [RequiredIfPatched]
    [MinLengthIfPatched(2)]
    OptionallyPatched<string> Name,
    OptionallyPatched<string?> Description,
    OptionallyPatched<int> Count,
    OptionallyPatched<Address> Address,
    OptionallyPatched<ResourceState> State,
    OptionallyPatched<ImmutableList<string>> People) : BasePatchRequest;
