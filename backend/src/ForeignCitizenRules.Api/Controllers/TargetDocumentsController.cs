using ForeignCitizenRules.Application;
using Microsoft.AspNetCore.Mvc;

namespace ForeignCitizenRules.Api.Controllers;

[Route("api/v1/target-documents")]
public sealed class TargetDocumentsController(DocumentApplicationService documents) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTargetDocuments(CancellationToken cancellationToken)
    {
        return Ok(await documents.GetTargetDocumentsAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTargetDocument(CreateTargetDocumentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return StatusCode(StatusCodes.Status201Created, await documents.CreateTargetDocumentAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return ValidationError(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTargetDocument(int id, CreateTargetDocumentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await documents.UpdateTargetDocumentAsync(id, request, cancellationToken);
            return updated == null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return ValidationError(ex);
        }
    }
}
