using System.ComponentModel.DataAnnotations;
using LogOtter.Hub.Models;
using LogOtter.Hub.Services;
using LogOtter.JsonHal;
using Microsoft.AspNetCore.Mvc;

namespace LogOtter.Hub.Controllers;

[ApiController]
[Route("api/event-streams")]
public class EventStreamsController(EventStreamCache eventStreamCache) : Controller
{
    private const int PageSize = 20;

    [HttpGet(Name = "GetEventStreams")]
    public async Task<ActionResult<EventStreamsResponse>> GetAll([FromQuery] [Range(1, int.MaxValue)] int? page)
    {
        var currentPage = page ?? 1;

        var definitions = await eventStreamCache.GetEventStreamDefinitions();

        var response = new EventStreamsResponse(definitions.Skip((currentPage - 1) * PageSize).Take(PageSize).ToList());

        var totalPages = PageHelpers.CalculatePageCount(PageSize, definitions.Count);
        response.Links.AddPagedLinks(currentPage, totalPages, p => Url.Link("GetEventStreams", new { page = p })!);

        return Ok(response);
    }

    [HttpGet("{eventStreamName}")]
    public async Task<ActionResult<EventStreamDefinition?>> Get([FromRoute] string eventStreamName)
    {
        var definition = await eventStreamCache.GetEventStreamDefinition(eventStreamName);

        if (definition == null)
        {
            return NotFound();
        }

        return Ok(definition);
    }
}
