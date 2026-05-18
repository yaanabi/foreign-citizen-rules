using ForeignCitizenRules.Application;
using Microsoft.AspNetCore.Mvc;

namespace ForeignCitizenRules.Api.Controllers;

[Route("api/v1/citizens")]
public sealed class CitizensController(CitizenApplicationService citizens, RoadmapApplicationService roadmaps) : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCitizenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return StatusCode(StatusCodes.Status201Created, await citizens.RegisterAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return ValidationError(ex);
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCitizenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await citizens.LoginAsync(request, cancellationToken);
            return response == null ? Unauthorized() : Ok(response);
        }
        catch (ArgumentException ex)
        {
            return ValidationError(ex);
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var citizen = await citizens.GetCurrentAsync(BearerToken(), cancellationToken);
        return citizen == null ? Unauthorized() : Ok(citizen);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateCitizenRequest request, CancellationToken cancellationToken)
    {
        var citizen = await citizens.UpdateCurrentAsync(BearerToken(), request, cancellationToken);
        return citizen == null ? Unauthorized() : Ok(citizen);
    }

    [HttpPost("me/roadmaps")]
    public async Task<IActionResult> CreateRoadmap(CreateCitizenRoadmapRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var roadmap = await roadmaps.CreateRoadmapAsync(BearerToken(), request, cancellationToken);
            return roadmap == null ? Unauthorized() : Ok(roadmap);
        }
        catch (ArgumentException ex)
        {
            return ValidationError(ex);
        }
    }

    [HttpGet("me/roadmaps")]
    public async Task<IActionResult> GetRoadmaps(CancellationToken cancellationToken)
    {
        var items = await roadmaps.GetRoadmapsAsync(BearerToken(), cancellationToken);
        return items == null ? Unauthorized() : Ok(items);
    }
}
