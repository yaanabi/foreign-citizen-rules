using ForeignCitizenRules.Application;
using Microsoft.AspNetCore.Mvc;

namespace ForeignCitizenRules.Api.Controllers;

[Route("api/v1/rules")]
public sealed class RulesController(RuleApplicationService rules) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateRule(CreateRuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await rules.CreateRuleAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetRule), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return ValidationError(ex);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRule(int id, CancellationToken cancellationToken)
    {
        var rule = await rules.GetRuleAsync(id, cancellationToken);
        return rule == null
            ? NotFound(new ApiErrorResponse { Code = "not_found", Message = "Rule was not found.", Details = new { id } })
            : Ok(rule);
    }

    [HttpGet]
    public async Task<IActionResult> GetRules([FromQuery] string? roadmapVersion, CancellationToken cancellationToken)
    {
        return Ok(await rules.GetRulesAsync(roadmapVersion, cancellationToken));
    }
}
