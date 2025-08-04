using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace LogOtter.HttpPatch.Tests.Api.Controllers;

public record Address([Required] string? Line1, [Required] string? Postcode);

public record TestResourcePatchRequest(
    [RequiredIfPatched] [MinLengthIfPatched(2)] OptionallyPatched<string> Name,
    OptionallyPatched<string?> Description,
    OptionallyPatched<int> Count,
    OptionallyPatched<Address> Address,
    OptionallyPatched<ResourceState> State,
    OptionallyPatched<ImmutableList<string>> People
) : BasePatchRequest;

public record TestResourceWithPropertiesPatchRequest : BasePatchRequest
{
    [RequiredIfPatched]
    [MinLengthIfPatched(2)]
    public OptionallyPatched<string> Name { get; set; }

    public OptionallyPatched<string?> Description { get; set; }
    public OptionallyPatched<int> Count { get; set; }
    public OptionallyPatched<Address> Address { get; set; }
    public OptionallyPatched<ResourceState> State { get; set; }
    public OptionallyPatched<ImmutableList<string>> People { get; set; }
}
