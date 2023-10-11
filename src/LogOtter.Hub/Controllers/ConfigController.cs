using Microsoft.AspNetCore.Mvc;

namespace LogOtter.Hub.Controllers;

[ApiController]
[Route("config")]
public class ConfigController : Controller
{
    [HttpGet]
    public IActionResult Get()
    {
        var config = new { ApiBaseUrl = "/api" };

        return Ok(config);
    }
}
