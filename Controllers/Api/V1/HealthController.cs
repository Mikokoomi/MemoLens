using MemoLens.Models.Api;
using Microsoft.AspNetCore.Mvc;

namespace MemoLens.Controllers.Api.V1;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public HealthController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> Get()
    {
        var response = new ApiResponse<object>
        {
            Success = true,
            Message = "API MemoLens đang hoạt động.",
            Data = new
            {
                AppName = "MemoLens",
                ApiVersion = "v1",
                Environment = _environment.EnvironmentName,
                ServerTimeUtc = DateTime.UtcNow
            }
        };

        return Ok(response);
    }
}
