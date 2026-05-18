using ForeignCitizenRules.Application;
using Microsoft.AspNetCore.Mvc;

namespace ForeignCitizenRules.Api.Controllers;

[Route("api/v1/organizations")]
public sealed class OrganizationsController(DocumentApplicationService documents) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrganizations(CancellationToken cancellationToken)
    {
        return Ok(await documents.GetOrganizationsAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrganization(OrganizationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return StatusCode(StatusCodes.Status201Created, await documents.CreateOrganizationAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return ValidationError(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateOrganization(int id, OrganizationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await documents.UpdateOrganizationAsync(id, request, cancellationToken);
            return updated == null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return ValidationError(ex);
        }
    }
}
