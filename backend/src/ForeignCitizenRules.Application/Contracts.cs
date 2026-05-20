namespace ForeignCitizenRules.Application;

public sealed class CreateRuleRequest
{
    public string? Name { get; set; }
    public string? RoadmapVersion { get; set; }
    public int TargetDocumentId { get; set; }
    public GuidanceRequest? Guidance { get; set; }
    public List<ProfileRequest>? Profiles { get; set; }
}

public sealed class GuidanceRequest
{
    public string? Description { get; set; }
}

public sealed class ProfileRequest
{
    public int StayDays { get; set; }
    public int Priority { get; set; }
    public bool IsFallback { get; set; }
    public List<string>? StayPurposes { get; set; }
    public List<string>? Citizenships { get; set; }
    public List<ProfilePropertyRequest>? Properties { get; set; }
}

public sealed class ProfilePropertyRequest
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}

public sealed class RuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RoadmapVersion { get; set; }
    public GuidanceDto? Guidance { get; set; }
    public TargetDocumentDto? TargetDocument { get; set; }
    public List<ProfileDto> Profiles { get; set; } = [];
}

public sealed class GuidanceDto
{
    public string Description { get; set; } = string.Empty;
}

public sealed class TargetDocumentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<OrganizationDto> Organizations { get; set; } = [];
}

public sealed class OrganizationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public sealed class ProfileDto
{
    public int StayDays { get; set; }
    public int Priority { get; set; }
    public bool IsFallback { get; set; }
    public List<string> StayPurposes { get; set; } = [];
    public List<string> Citizenships { get; set; } = [];
    public List<ProfilePropertyDto> Properties { get; set; } = [];
}

public sealed class ProfilePropertyDto
{
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public sealed class ReferenceItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class ProfilePropertyReferenceDto
{
    public string Name { get; set; } = string.Empty;
    public List<string> Values { get; set; } = [];
}

public sealed class OrganizationRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
}

public sealed class CreateTargetDocumentRequest
{
    public string? Name { get; set; }
    public List<int>? OrganizationIds { get; set; }
}

public sealed class RegisterCitizenRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? CitizenshipName { get; set; }
}

public sealed class LoginCitizenRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public sealed class UpdateCitizenRequest
{
    public string? FullName { get; set; }
    public string? CitizenshipName { get; set; }
    public List<ProfilePropertyRequest>? Properties { get; set; }
}

public sealed class CitizenDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CitizenshipName { get; set; }
    public List<ProfilePropertyDto> Properties { get; set; } = [];
}

public sealed class LoginCitizenResponse
{
    public string Token { get; set; } = string.Empty;
    public CitizenDto Citizen { get; set; } = new();
}

public sealed class CreateCitizenRoadmapRequest
{
    public DateTime EntryDate { get; set; }
    public string? StayPurposeName { get; set; }
    public string? CitizenshipName { get; set; }
}

public sealed class CitizenRoadmapDto
{
    public int Id { get; set; }
    public int CitizenId { get; set; }
    public DateTime EntryDate { get; set; }
    public string StayPurpose { get; set; } = string.Empty;
    public string Citizenship { get; set; } = string.Empty;
    public int? RuleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? StayDays { get; set; }
    public int? DaysPassed { get; set; }
    public int? DaysRemaining { get; set; }
    public DateTime? DeadlineDate { get; set; }
    public bool IsOverdue { get; set; }
    public TargetDocumentDto? TargetDocument { get; set; }
    public GuidanceDto? Guidance { get; set; }
}

public sealed class ApiErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
}
