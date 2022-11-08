using CustomerApi.Controllers.Customers.Create;
using CustomerApi.Events.Customers;
using LogOtter.CosmosDb.EventStore;
using Microsoft.AspNetCore.Mvc;

namespace CustomerApi.Controllers.Customers.Get;

[ApiController]
[Route("customers")]
public class GetAllCustomersController : ControllerBase
{
    private readonly SnapshotRepository<CustomerEvent, CustomerReadModel> _customerSnapshotRepository;

    public GetAllCustomersController(
        SnapshotRepository<CustomerEvent, CustomerReadModel> customerSnapshotRepository
    )
    {
        _customerSnapshotRepository = customerSnapshotRepository;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IList<CustomerReadModel>>> GetAll()
    {
        var customerQuery = _customerSnapshotRepository.QuerySnapshots(
            CustomerReadModel.StaticPartitionKey,
            query => query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
        );

        var customerReadModels = await customerQuery.ToListAsync();

        var customers = customerReadModels
            .Select(readModel => new CustomerResponse(readModel))
            .ToList();

        return Ok(customers);
    }
}