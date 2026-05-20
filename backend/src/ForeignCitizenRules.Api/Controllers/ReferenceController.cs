using ForeignCitizenRules.Application;
using Microsoft.AspNetCore.Mvc;

namespace ForeignCitizenRules.Api.Controllers;

[Route("api/v1/reference")]
public sealed class ReferenceController(ReferenceApplicationService references) : ApiControllerBase
{
    [HttpGet("stay-purposes")]
    public async Task<IActionResult> GetStayPurposes(CancellationToken cancellationToken)
    {
        return Ok(await references.GetStayPurposesAsync(cancellationToken));
    }

    [HttpGet("citizenships")]
    public async Task<IActionResult> GetCitizenships(CancellationToken cancellationToken)
    {
        return Ok(await references.GetCitizenshipsAsync(cancellationToken));
    }

    [HttpGet("profile-properties")]
    public async Task<IActionResult> GetProfileProperties(CancellationToken cancellationToken)
    {
        return Ok(await references.GetProfilePropertiesAsync(cancellationToken));
    }
}
