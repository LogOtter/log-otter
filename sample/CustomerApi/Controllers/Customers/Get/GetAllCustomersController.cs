using System.ComponentModel.DataAnnotations;
using CustomerApi.Configuration;
using CustomerApi.Events.Customers;
using LogOtter.CosmosDb;
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

        var totalCount = await _customerSnapshotRepository.CountSnapshotsAsync(
            CustomerReadModel.StaticPartitionKey,
            cancellationToken: cancellationToken
        );

        var customerQuery = _customerSnapshotRepository.QuerySnapshots(
            CustomerReadModel.StaticPartitionKey,
            query => query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Page(currentPage, pageSize),
            cancellationToken: cancellationToken
        );

        var customerReadModels = await customerQuery.ToListAsync();

        var response = new CustomersResponse(customerReadModels
            .Select(readModel => new CustomerResponse(readModel))
            .ToList());

        var totalPages = PageHelpers.CalculatePageCount(pageSize, totalCount);
        response.Links.AddPagedLinks(currentPage, totalPages, p => Url.Link(
            "GetAllCustomers",
            new { page = p }
        )!);

        return Ok(response);
    }
}