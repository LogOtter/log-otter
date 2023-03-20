using System.ComponentModel.DataAnnotations;
using CustomerApi.Events.Customers;
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

    public CreateCustomerController(EventRepository<CustomerEvent, CustomerReadModel> customerEventRepository)
    {
        _customerEventRepository = customerEventRepository;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(
        [Required] CreateCustomerRequest customerData,
        CancellationToken cancellationToken
    )
    {
        var customerUri = CustomerUri.Generate();

        var customerCreated = new CustomerCreated(customerUri, customerData.EmailAddress, customerData.FirstName, customerData.LastName);

        var customer = await _customerEventRepository.ApplyEvents(customerUri.Uri, 0, cancellationToken, customerCreated);

        return Created(customerUri.Uri, new CustomerResponse(customer));
    }
}
