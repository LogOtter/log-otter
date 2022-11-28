using System.ComponentModel.DataAnnotations;
using CustomerApi.Configuration;
using CustomerApi.Events.Customers;
using LogOtter.CosmosDb.EventStore;
using LogOtter.JsonHal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CustomerApi.Controllers.Customers.Get;

[ApiController]
[Route("customers")]
[Authorize(Roles = "Customers.Read,Customers.ReadWrite")]
public class GetAllCustomersController : ControllerBase
{
    private readonly SnapshotRepository<CustomerEvent, CustomerReadModel> _customerSnapshotRepository;
    private readonly IOptions<PageOptions> _pageOptions;

    public GetAllCustomersController(
        SnapshotRepository<CustomerEvent, CustomerReadModel> customerSnapshotRepository,
        IOptions<PageOptions> pageOptions)
    {
        _customerSnapshotRepository = customerSnapshotRepository;
        _pageOptions = pageOptions;
    }

    [HttpGet(Name = "GetAllCustomers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomersResponse>> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int? page,
        CancellationToken cancellationToken
    )
    {
        var currentPage = page.GetValueOrDefault(1);
        var pageSize = _pageOptions.Value.PageSize;

        var customerQuery = _customerSnapshotRepository.QuerySnapshots(
            CustomerReadModel.StaticPartitionKey,
            query => query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize + 1),
            cancellationToken: cancellationToken
        );

        var customerReadModels = await customerQuery.ToListAsync();

        var response = new CustomersResponse(customerReadModels
            .Take(pageSize)
            .Select(readModel => new CustomerResponse(readModel))
            .ToList());
        
        response.Links.AddSelfLink(Url.Link(
            "GetAllCustomers",
            new { page = currentPage }
        )!);
        
        if (currentPage > 1)
        {
            response.Links.AddPrevLink(Url.Link(
                "GetAllCustomers",
                new { page = currentPage - 1 }
            )!);
        }
        
        if (customerReadModels.Count > pageSize)
        {
            response.Links.AddNextLink(Url.Link(
                "GetAllCustomers",
                new { page = currentPage + 1 }
            )!);
        }

        return Ok(response);
    }
}