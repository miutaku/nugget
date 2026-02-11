using Microsoft.AspNetCore.Mvc;

namespace Nugget.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public SystemController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            organizationName = _configuration["App:OrganizationName"] ?? "Nugget"
        });
    }
}
