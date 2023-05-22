using System.ComponentModel.DataAnnotations;
using CustomerApi.Events.Customers;
using CustomerApi.Services;
using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Customers.Create;

[ApiController]
[Route("customers")]
[Authorize(Roles = "Customers.Create")]
public class CreateCustomerController : ControllerBase
{
    private readonly EventRepository<CustomerEvent, CustomerReadModel> _customerEventRepository;
    private readonly EmailAddressReservationService _emailAddressReservationService;

    public CreateCustomerController(
        EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository,
        EmailAddressReservationService emailAddressReservationService
    )
    {
        _customerEventRepository = customerEventRepository;
        _emailAddressReservationService = emailAddressReservationService;
    }

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
            await _emailAddressReservationService.ReserveEmailAddress(customerData.EmailAddress);
        }
        catch (EmailAddressInUseException)
        {
            return Conflict("Email address already in use");
        }

        var customerUri = CustomerUri.Generate();

        var customerCreated = new CustomerCreated(customerUri, customerData.EmailAddress, customerData.FirstName, customerData.LastName);

        var customer = await _customerEventRepository.ApplyEvents(customerUri.Uri, 0, cancellationToken, customerCreated);

        return Created(customerUri.Uri, new CustomerResponse(customer));
    }
}
