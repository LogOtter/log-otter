using System.ComponentModel.DataAnnotations;
using CustomerApi.Events.Customers;
using CustomerApi.Uris;
using LogOtter.EventStore.CosmosDb;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Customers.Create;

[ApiController]
[Route("customers")]
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
    public async Task<IActionResult> CreateCustomer(
        [Required] CreateCustomerRequest customerData
    )
    {
        var customerUri = CustomerUri.Generate();

        var customerCreated = new CustomerCreated(
            customerUri,
            customerData.EmailAddress,
            customerData.FirstName,
            customerData.LastName
        );

        var customer = await _customerEventRepository.ApplyEvents(customerUri.Uri, 0, customerCreated);
        return Created(customerUri.Uri, new CustomerResponse(customer));
    }
}

public record CreateCustomerRequest([Required]string EmailAddress, [Required]string FirstName, [Required]string LastName);