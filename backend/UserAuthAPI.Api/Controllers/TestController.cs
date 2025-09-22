using Microsoft.AspNetCore.Mvc;

namespace UserAuthAPI.Api.Controllers;

/// <summary>
/// Test controller to verify API is working
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    /// <summary>
    /// Test endpoint to verify API is running
    /// </summary>
    /// <returns>A simple message</returns>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { Message = "API is working!", Timestamp = DateTime.UtcNow });
    }
}