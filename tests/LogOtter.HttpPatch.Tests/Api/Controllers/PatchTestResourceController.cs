using Microsoft.AspNetCore.Mvc;

namespace LogOtter.HttpPatch.Tests.Api.Controllers;

[ApiController]
public class PatchTestResourceController(TestDataStore dataStore) : ControllerBase
{
    [HttpPatch("/constructor/test-resource/{resourceId}")]
    public IActionResult Patch(string resourceId, [FromBody] TestResourcePatchRequest request)
    {
        var testResource = dataStore.GetResource(resourceId);

        if (request.Name.IsIncludedInPatch)
        {
            testResource = testResource with { Name = request.Name.Value! };
        }

        if (request.Description.IsIncludedInPatch)
        {
            testResource = testResource with { Description = request.Description.Value! };
        }

        if (request.Count.IsIncludedInPatch)
        {
            testResource = testResource with { Count = request.Count.Value };
        }

        if (request.Address.IsIncludedInPatch)
        {
            testResource = testResource with { Address = request.Address.Value! };
        }

        if (request.State.IsIncludedInPatch)
        {
            testResource = testResource with { State = request.State.Value };
        }

        if (request.People.IsIncludedInPatch)
        {
            testResource = testResource with { People = request.People.Value! };
        }

        dataStore.UpsertResource(testResource);
        return Ok(testResource);
    }

    [HttpPatch("/properties/test-resource/{resourceId}")]
    public IActionResult Patch(string resourceId, [FromBody] TestResourceWithPropertiesPatchRequest request)
    {
        var testResource = dataStore.GetResource(resourceId);

        if (request.Name.IsIncludedInPatch)
        {
            testResource = testResource with { Name = request.Name.Value! };
        }

        if (request.Description.IsIncludedInPatch)
        {
            testResource = testResource with { Description = request.Description.Value! };
        }

        if (request.Count.IsIncludedInPatch)
        {
            testResource = testResource with { Count = request.Count.Value };
        }

        if (request.Address.IsIncludedInPatch)
        {
            testResource = testResource with { Address = request.Address.Value! };
        }

        if (request.State.IsIncludedInPatch)
        {
            testResource = testResource with { State = request.State.Value };
        }

        if (request.People.IsIncludedInPatch)
        {
            testResource = testResource with { People = request.People.Value! };
        }

        dataStore.UpsertResource(testResource);
        return Ok(testResource);
    }
}
