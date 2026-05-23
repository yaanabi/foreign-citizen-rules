namespace ForeignCitizenRules.Domain;

public sealed class Roadmap
{
    public int Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public ICollection<Rule> Rules { get; set; } = new HashSet<Rule>();
}

public sealed class Rule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RoadmapId { get; set; }
    public int TargetDocumentId { get; set; }
    public Roadmap Roadmap { get; set; } = null!;
    public TargetDocument TargetDocument { get; set; } = null!;
    public Guidance Guidance { get; set; } = null!;
    public ICollection<Profile> Profiles { get; set; } = new HashSet<Profile>();
    public string RoadmapVersion => Roadmap.Version;
    public string GuidanceDescription => Guidance.Description;
    public string TargetDocumentName => TargetDocument.Name;
}

public sealed class Profile
{
    public int Id { get; set; }
    public int RuleId { get; set; }
    public int StayDays { get; set; }
    public int Priority { get; set; }
    public bool IsFallback { get; set; }
    public Rule Rule { get; set; } = null!;
    public ICollection<ProfileProperty> Properties { get; set; } = new HashSet<ProfileProperty>();
    public ICollection<StayPurpose> StayPurposes { get; set; } = new HashSet<StayPurpose>();
    public ICollection<Citizenship> Citizenships { get; set; } = new HashSet<Citizenship>();
}

public sealed class ProfileProperty
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public Profile Profile { get; set; } = null!;
}

public sealed class StayPurpose
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Profile> Profiles { get; set; } = new HashSet<Profile>();
}

public sealed class Citizenship
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Profile> Profiles { get; set; } = new HashSet<Profile>();
}

public sealed class TargetDocument
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Organization> Organizations { get; set; } = new HashSet<Organization>();
    public ICollection<Rule> Rules { get; set; } = new HashSet<Rule>();
}

public sealed class Organization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public ICollection<TargetDocument> TargetDocuments { get; set; } = new HashSet<TargetDocument>();
}

public sealed class Guidance
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public Rule Rule { get; set; } = null!;
}

public sealed class Citizen
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int CitizenshipId { get; set; }
    public Citizenship Citizenship { get; set; } = null!;
    public ICollection<CitizenSession> Sessions { get; set; } = new HashSet<CitizenSession>();
    public ICollection<CitizenProfileProperty> Properties { get; set; } = new HashSet<CitizenProfileProperty>();
    public ICollection<CitizenRoadmapRequest> RoadmapRequests { get; set; } = new HashSet<CitizenRoadmapRequest>();
}

public sealed class CitizenSession
{
    public int Id { get; set; }
    public int CitizenId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Citizen Citizen { get; set; } = null!;
}

public sealed class CitizenProfileProperty
{
    public int Id { get; set; }
    public int CitizenId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public Citizen Citizen { get; set; } = null!;
}

public sealed class CitizenRoadmapRequest
{
    public int Id { get; set; }
    public int CitizenId { get; set; }
    public DateTime EntryDate { get; set; }
    public string StayPurposeName { get; set; } = string.Empty;
    public string CitizenshipName { get; set; } = string.Empty;
    public int? RuleId { get; set; }
    public int? MatchedStayDays { get; set; }
    public int? MatchedDaysPassed { get; set; }
    public int? MatchedDaysRemaining { get; set; }
    public DateTime? MatchedDeadlineDate { get; set; }
    public string? MatchedTargetDocumentName { get; set; }
    public string? MatchedOrganizationsJson { get; set; }
    public string? MatchedGuidanceDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Citizen Citizen { get; set; } = null!;
    public Rule? Rule { get; set; }
}
