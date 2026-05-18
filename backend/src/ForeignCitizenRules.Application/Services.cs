using System.Security.Cryptography;
using System.Text;
using ForeignCitizenRules.Domain;
using ForeignCitizenRules.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForeignCitizenRules.Application;

public sealed class RuleApplicationService(RulesDbContext db)
{
    public async Task<RuleDto> CreateRuleAsync(CreateRuleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRule(request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var roadmap = await GetOrCreateRoadmapAsync(request.RoadmapVersion!.Trim(), cancellationToken);
        var targetDocument = await db.TargetDocuments
            .Include(x => x.Organizations)
            .SingleOrDefaultAsync(x => x.Id == request.TargetDocumentId, cancellationToken)
            ?? throw new ArgumentException("TargetDocument was not found.", nameof(request));

        var rule = new Rule
        {
            Name = request.Name!.Trim(),
            Roadmap = roadmap,
            TargetDocument = targetDocument,
            Guidance = new Guidance
            {
                Description = request.Guidance!.Description!.Trim(),
                Refusal = request.Guidance.Refusal!.Trim()
            }
        };

        foreach (var profileRequest in request.Profiles!)
        {
            var profile = new Profile { StayDays = profileRequest.StayDays };
            foreach (var stayPurposeName in profileRequest.StayPurposes!)
            {
                profile.StayPurposes.Add(await GetOrCreateStayPurposeAsync(stayPurposeName.Trim(), cancellationToken));
            }

            foreach (var citizenshipName in profileRequest.Citizenships!)
            {
                profile.Citizenships.Add(await GetOrCreateCitizenshipAsync(citizenshipName.Trim(), cancellationToken));
            }

            foreach (var property in profileRequest.Properties ?? [])
            {
                if (!string.IsNullOrWhiteSpace(property.Name))
                {
                    profile.Properties.Add(new ProfileProperty
                    {
                        Name = property.Name.Trim(),
                        Value = property.Value?.Trim()
                    });
                }
            }

            rule.Profiles.Add(profile);
        }

        db.Rules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto((await QueryRules().SingleAsync(x => x.Id == rule.Id, cancellationToken)));
    }

    public async Task<RuleDto?> GetRuleAsync(int id, CancellationToken cancellationToken = default)
    {
        var rule = await QueryRules().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return rule == null ? null : ToDto(rule);
    }

    public async Task<IReadOnlyList<RuleDto>> GetRulesAsync(string? roadmapVersion, CancellationToken cancellationToken = default)
    {
        var query = QueryRules();
        if (!string.IsNullOrWhiteSpace(roadmapVersion))
        {
            query = query.Where(x => x.Roadmap.Version == roadmapVersion);
        }

        return await query.OrderBy(x => x.Id).Select(x => ToDto(x)).ToListAsync(cancellationToken);
    }

    private IQueryable<Rule> QueryRules()
    {
        return db.Rules
            .Include(x => x.Roadmap)
            .Include(x => x.Guidance)
            .Include(x => x.TargetDocument).ThenInclude(x => x.Organizations)
            .Include(x => x.Profiles).ThenInclude(x => x.Properties)
            .Include(x => x.Profiles).ThenInclude(x => x.StayPurposes)
            .Include(x => x.Profiles).ThenInclude(x => x.Citizenships);
    }

    private async Task<Roadmap> GetOrCreateRoadmapAsync(string version, CancellationToken cancellationToken)
    {
        var roadmap = await db.Roadmaps.SingleOrDefaultAsync(x => x.Version == version, cancellationToken);
        if (roadmap != null)
        {
            return roadmap;
        }

        roadmap = new Roadmap { Version = version };
        db.Roadmaps.Add(roadmap);
        return roadmap;
    }

    private async Task<StayPurpose> GetOrCreateStayPurposeAsync(string name, CancellationToken cancellationToken)
    {
        var purpose = await db.StayPurposes.SingleOrDefaultAsync(x => x.Name == name, cancellationToken);
        if (purpose != null)
        {
            return purpose;
        }

        purpose = new StayPurpose { Name = name };
        db.StayPurposes.Add(purpose);
        return purpose;
    }

    internal async Task<Citizenship> GetOrCreateCitizenshipAsync(string name, CancellationToken cancellationToken = default)
    {
        var citizenship = await db.Citizenships.SingleOrDefaultAsync(x => x.Name == name, cancellationToken);
        if (citizenship != null)
        {
            return citizenship;
        }

        citizenship = new Citizenship { Name = name };
        db.Citizenships.Add(citizenship);
        return citizenship;
    }

    internal static RuleDto ToDto(Rule rule)
    {
        return new RuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            RoadmapVersion = rule.Roadmap.Version,
            Guidance = new GuidanceDto { Description = rule.Guidance.Description, Refusal = rule.Guidance.Refusal },
            TargetDocument = DocumentApplicationService.ToDto(rule.TargetDocument),
            Profiles = rule.Profiles.Select(p => new ProfileDto
            {
                StayDays = p.StayDays,
                StayPurposes = p.StayPurposes.Select(x => x.Name).ToList(),
                Citizenships = p.Citizenships.Select(x => x.Name).ToList(),
                Properties = p.Properties.Select(x => new ProfilePropertyDto { Name = x.Name, Value = x.Value }).ToList()
            }).ToList()
        };
    }

    private static void ValidateRule(CreateRuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Rule name is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.RoadmapVersion)) throw new ArgumentException("Roadmap version is required.", nameof(request));
        if (request.TargetDocumentId <= 0) throw new ArgumentException("TargetDocumentId is required.", nameof(request));
        if (request.Guidance == null || string.IsNullOrWhiteSpace(request.Guidance.Description) || string.IsNullOrWhiteSpace(request.Guidance.Refusal)) throw new ArgumentException("Guidance with description and refusal is required.", nameof(request));
        if (request.Profiles == null || request.Profiles.Count == 0) throw new ArgumentException("At least one Profile is required.", nameof(request));

        foreach (var profile in request.Profiles)
        {
            if (profile.StayDays <= 0) throw new ArgumentException("Profile.StayDays must be greater than 0.", nameof(request));
            if (profile.StayPurposes == null || profile.StayPurposes.Count == 0) throw new ArgumentException("Profile must have at least one StayPurpose.", nameof(request));
            if (profile.Citizenships == null || profile.Citizenships.Count == 0) throw new ArgumentException("Profile must have at least one Citizenship.", nameof(request));
        }
    }
}

public sealed class DocumentApplicationService(RulesDbContext db)
{
    public async Task<IReadOnlyList<OrganizationDto>> GetOrganizationsAsync(CancellationToken cancellationToken = default)
    {
        return await db.Organizations.OrderBy(x => x.Name).Select(x => ToDto(x)).ToListAsync(cancellationToken);
    }

    public async Task<OrganizationDto> CreateOrganizationAsync(OrganizationRequest request, CancellationToken cancellationToken = default)
    {
        ValidateOrganization(request);
        var organization = new Organization { Name = request.Name!.Trim(), Address = request.Address!.Trim() };
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(organization);
    }

    public async Task<OrganizationDto?> UpdateOrganizationAsync(int id, OrganizationRequest request, CancellationToken cancellationToken = default)
    {
        ValidateOrganization(request);
        var organization = await db.Organizations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (organization == null)
        {
            return null;
        }

        organization.Name = request.Name!.Trim();
        organization.Address = request.Address!.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(organization);
    }

    public async Task<IReadOnlyList<TargetDocumentDto>> GetTargetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        return await db.TargetDocuments
            .Include(x => x.Organizations)
            .OrderBy(x => x.Name)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<TargetDocumentDto> CreateTargetDocumentAsync(CreateTargetDocumentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateTargetDocument(request);
        var document = new TargetDocument { Name = request.Name!.Trim() };
        await AttachOrganizationsAsync(document, request.OrganizationIds!, cancellationToken);
        db.TargetDocuments.Add(document);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(document);
    }

    public async Task<TargetDocumentDto?> UpdateTargetDocumentAsync(int id, CreateTargetDocumentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateTargetDocument(request);
        var document = await db.TargetDocuments.Include(x => x.Organizations).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (document == null)
        {
            return null;
        }

        document.Name = request.Name!.Trim();
        document.Organizations.Clear();
        await AttachOrganizationsAsync(document, request.OrganizationIds!, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(document);
    }

    private async Task AttachOrganizationsAsync(TargetDocument document, List<int> organizationIds, CancellationToken cancellationToken)
    {
        var ids = organizationIds.Distinct().ToList();
        var organizations = await db.Organizations.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        if (organizations.Count != ids.Count)
        {
            throw new ArgumentException("One or more organizations were not found.");
        }

        foreach (var organization in organizations)
        {
            document.Organizations.Add(organization);
        }
    }

    internal static OrganizationDto ToDto(Organization organization) => new()
    {
        Id = organization.Id,
        Name = organization.Name,
        Address = organization.Address
    };

    internal static TargetDocumentDto ToDto(TargetDocument document) => new()
    {
        Id = document.Id,
        Name = document.Name,
        Organizations = document.Organizations.OrderBy(x => x.Name).Select(ToDto).ToList()
    };

    private static void ValidateOrganization(OrganizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Organization name is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Address)) throw new ArgumentException("Organization address is required.", nameof(request));
    }

    private static void ValidateTargetDocument(CreateTargetDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Target document name is required.", nameof(request));
        if (request.OrganizationIds == null || request.OrganizationIds.Count == 0) throw new ArgumentException("At least one organization is required.", nameof(request));
    }
}

public sealed class ReferenceApplicationService(RulesDbContext db)
{
    public async Task<IReadOnlyList<ReferenceItemDto>> GetStayPurposesAsync(CancellationToken cancellationToken = default)
    {
        return await db.StayPurposes.OrderBy(x => x.Name).Select(x => new ReferenceItemDto { Id = x.Id, Name = x.Name }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReferenceItemDto>> GetCitizenshipsAsync(CancellationToken cancellationToken = default)
    {
        return await db.Citizenships.OrderBy(x => x.Name).Select(x => new ReferenceItemDto { Id = x.Id, Name = x.Name }).ToListAsync(cancellationToken);
    }
}

public sealed class CitizenApplicationService(RulesDbContext db)
{
    public async Task<CitizenDto> RegisterAsync(RegisterCitizenRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRegister(request);
        var email = NormalizeEmail(request.Email!);
        if (await db.Citizens.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new ArgumentException("Citizen with this email already exists.", nameof(request));
        }

        var ruleService = new RuleApplicationService(db);
        var citizen = new Citizen
        {
            FullName = request.FullName!.Trim(),
            Email = email,
            PasswordHash = HashPassword(request.Password!),
            Citizenship = await ruleService.GetOrCreateCitizenshipAsync(request.CitizenshipName!.Trim(), cancellationToken)
        };
        db.Citizens.Add(citizen);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(citizen);
    }

    public async Task<LoginCitizenResponse?> LoginAsync(LoginCitizenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Email and password are required.", nameof(request));
        }

        var email = NormalizeEmail(request.Email);
        var passwordHash = HashPassword(request.Password);
        var citizen = await db.Citizens
            .Include(x => x.Citizenship)
            .Include(x => x.Properties)
            .SingleOrDefaultAsync(x => x.Email == email && x.PasswordHash == passwordHash, cancellationToken);

        if (citizen == null)
        {
            return null;
        }

        var session = new CitizenSession
        {
            Citizen = citizen,
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        db.CitizenSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return new LoginCitizenResponse { Token = session.Token, Citizen = ToDto(citizen) };
    }

    public async Task<CitizenDto?> GetCurrentAsync(string? token, CancellationToken cancellationToken = default)
    {
        var citizen = await FindCitizenByTokenAsync(token, cancellationToken);
        return citizen == null ? null : ToDto(citizen);
    }

    public async Task<CitizenDto?> UpdateCurrentAsync(string? token, UpdateCitizenRequest request, CancellationToken cancellationToken = default)
    {
        var citizen = await FindCitizenByTokenAsync(token, cancellationToken);
        if (citizen == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            citizen.FullName = request.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.CitizenshipName))
        {
            var ruleService = new RuleApplicationService(db);
            citizen.Citizenship = await ruleService.GetOrCreateCitizenshipAsync(request.CitizenshipName.Trim(), cancellationToken);
        }

        if (request.Properties != null)
        {
            citizen.Properties.Clear();
            foreach (var property in request.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                citizen.Properties.Add(new CitizenProfileProperty { Name = property.Name!.Trim(), Value = property.Value?.Trim() });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(citizen);
    }

    internal async Task<Citizen?> FindCitizenByTokenAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var session = await db.CitizenSessions
            .Include(x => x.Citizen).ThenInclude(x => x.Citizenship)
            .Include(x => x.Citizen).ThenInclude(x => x.Properties)
            .SingleOrDefaultAsync(x => x.Token == token && x.ExpiresAt > DateTime.UtcNow, cancellationToken);

        return session?.Citizen;
    }

    internal static CitizenDto ToDto(Citizen citizen) => new()
    {
        Id = citizen.Id,
        FullName = citizen.FullName,
        Email = citizen.Email,
        CitizenshipName = citizen.Citizenship.Name,
        Properties = citizen.Properties.Select(x => new ProfilePropertyDto { Name = x.Name, Value = x.Value }).ToList()
    };

    private static void ValidateRegister(RegisterCitizenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) throw new ArgumentException("FullName is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Email)) throw new ArgumentException("Email is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Password)) throw new ArgumentException("Password is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.CitizenshipName)) throw new ArgumentException("CitizenshipName is required.", nameof(request));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }
}
