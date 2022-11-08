﻿using System.Net;
using CustomerApi.Controllers.Customers.Create;
using CustomerApi.Events.Customers;
using CustomerApi.Uris;
using HttpPatch;
using LogOtter.EventStore.CosmosDb;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Customers.Patch;

[ApiController]
[Route("customers")]
public class PatchCustomerController : ControllerBase
{
    private readonly EventRepository<CustomerEvent, CustomerReadModel> _customerEventRepository;

    public PatchCustomerController(EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository)
    {
        _customerEventRepository = customerEventRepository;
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> Patch(
        [FromRoute] string id,
        [FromBody] CustomerPatchRequest patchRequest
    )
    {
        if (!Id.TryParse(id, out var customerId))
        {
            return NotFound();
        }

        var customerUri = new CustomerUri(customerId);

        var customerReadModel = await _customerEventRepository.Get(customerUri.Uri, includeDeleted: true);

        if (customerReadModel == null)
        {
            return NotFound();
        }

        var events = new List<CustomerEvent>();

        if (patchRequest.EmailAddress.IsIncludedInPatch)
        {
            events.Add(new CustomerEmailAddressChanged(
                customerUri,
                customerReadModel.EmailAddress,
                patchRequest.EmailAddress.Value!,
                DateTimeOffset.UtcNow
            ));
        }

        if (patchRequest.FirstName.IsIncludedInPatch || patchRequest.LastName.IsIncludedInPatch)
        {
            var newFirstName = patchRequest.FirstName.GetValueIfIncludedOrDefault(customerReadModel.FirstName);
            var newLastName = patchRequest.LastName.GetValueIfIncludedOrDefault(customerReadModel.LastName);

            events.Add(new CustomerNameChanged(
                customerUri,
                customerReadModel.FirstName,
                newFirstName,
                customerReadModel.LastName,
                newLastName,
                DateTimeOffset.UtcNow
            ));
        }

        var updatedCustomer =
            await _customerEventRepository.ApplyEvents(customerUri.Uri, customerReadModel.Revision, events.ToArray());

        return Ok(new CustomerResponse(updatedCustomer));
    }
}

public record CustomerPatchRequest(
    [RequiredIfPatched] OptionallyPatched<string> EmailAddress,
    [RequiredIfPatched] OptionallyPatched<string> FirstName,
    [RequiredIfPatched] OptionallyPatched<string> LastName
) : BasePatchRequest;