using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace LogOtter.Hub.Controllers;

[ApiController]
[Route("api/version")]
public class VersionController : Controller
{
    [HttpGet]
    public IActionResult Get()
    {
        var packageVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        var response = new VersionResponse(packageVersion, 1);

        return Ok(response);
    }
}

internal record VersionResponse(string PackageVersion, int ApiVersion);
