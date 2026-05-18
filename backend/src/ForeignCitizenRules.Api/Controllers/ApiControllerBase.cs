using ForeignCitizenRules.Application;
using Microsoft.AspNetCore.Mvc;

namespace ForeignCitizenRules.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ValidationError(Exception exception)
    {
        return BadRequest(new ApiErrorResponse
        {
            Code = "validation_error",
            Message = exception.Message
        });
    }

    protected string? BearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..]
            : null;
    }
}
