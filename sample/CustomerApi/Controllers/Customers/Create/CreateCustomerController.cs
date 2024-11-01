using System.ComponentModel.DataAnnotations;
using System.Net;
using CustomerApi.Events.Customers;
using CustomerApi.Services;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

namespace CustomerApi.Controllers.Customers.Create;

[ApiController]
[Route("customers")]
[Authorize(Roles = "Customers.Create")]
public class CreateCustomerController(
    EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository,
    EmailAddressReservationService emailAddressReservationService
) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(
        [Required] CreateCustomerRequest customerData,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await emailAddressReservationService.ReserveEmailAddress(customerData.EmailAddress);
        }
        catch (EmailAddressInUseException)
        {
            return Conflict("Email address already in use");
        }

        var customerUri = CustomerUri.Generate();

        var customerCreated = new CustomerCreated(customerUri, customerData.EmailAddress, customerData.FirstName, customerData.LastName);

        try
        {
            var customer = await customerEventRepository.ApplyEvents(customerUri.Uri, 0, cancellationToken, customerCreated);
            return Created(customerUri.Uri, new CustomerResponse(customer));
        }
        catch (ConcurrencyException)
        {
            return Conflict("Wrong revision");
        }
    }
}
