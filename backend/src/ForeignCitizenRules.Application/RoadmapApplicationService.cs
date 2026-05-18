using System.Text;
using ForeignCitizenRules.Domain;
using ForeignCitizenRules.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForeignCitizenRules.Application;

public sealed class RoadmapApplicationService(RulesDbContext db, CitizenApplicationService citizens)
{
    public async Task<CitizenRoadmapDto?> CreateRoadmapAsync(string? token, CreateCitizenRoadmapRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var citizen = await citizens.FindCitizenByTokenAsync(token, cancellationToken);
        if (citizen == null)
        {
            return null;
        }

        var entryDate = request.EntryDate.Date;
        var stayPurposeName = request.StayPurposeName!.Trim();
        var citizenshipName = citizen.Citizenship.Name;
        var match = await FindMatchingProfileAsync(entryDate, stayPurposeName, citizen, cancellationToken);
        var message = match == null ? "Подходящее правило не найдено." : BuildRoadmapMessage(entryDate, match.Profile, match.Rule);

        var history = new CitizenRoadmapRequest
        {
            Citizen = citizen,
            EntryDate = entryDate,
            StayPurposeName = stayPurposeName,
            CitizenshipName = citizenshipName,
            Rule = match?.Rule,
            CreatedAt = DateTime.UtcNow,
            Status = match == null ? "not_found" : "matched",
            Message = message
        };

        db.CitizenRoadmapRequests.Add(history);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(history, match);
    }

    public async Task<IReadOnlyList<CitizenRoadmapDto>?> GetRoadmapsAsync(string? token, CancellationToken cancellationToken = default)
    {
        var citizen = await citizens.FindCitizenByTokenAsync(token, cancellationToken);
        if (citizen == null)
        {
            return null;
        }

        var requests = await db.CitizenRoadmapRequests
            .Include(x => x.Rule).ThenInclude(x => x!.Roadmap)
            .Include(x => x.Rule).ThenInclude(x => x!.Guidance)
            .Include(x => x.Rule).ThenInclude(x => x!.TargetDocument).ThenInclude(x => x.Organizations)
            .Include(x => x.Rule).ThenInclude(x => x!.Profiles).ThenInclude(x => x.Properties)
            .Include(x => x.Rule).ThenInclude(x => x!.Profiles).ThenInclude(x => x.StayPurposes)
            .Include(x => x.Rule).ThenInclude(x => x!.Profiles).ThenInclude(x => x.Citizenships)
            .Where(x => x.CitizenId == citizen.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return requests
            .Select(x => ToDto(x, FindMatchingProfile(x.Rule, x.EntryDate, x.StayPurposeName, x.CitizenshipName, citizen.Properties)))
            .ToList();
    }

    private async Task<RoadmapMatch?> FindMatchingProfileAsync(DateTime entryDate, string stayPurposeName, Citizen citizen, CancellationToken cancellationToken)
    {
        var citizenshipName = citizen.Citizenship.Name;
        var daysInCountry = GetDaysInCountry(entryDate);
        var rules = await db.Rules
            .Include(x => x.Guidance)
            .Include(x => x.TargetDocument).ThenInclude(x => x.Organizations)
            .Include(x => x.Profiles).ThenInclude(x => x.Properties)
            .Include(x => x.Profiles).ThenInclude(x => x.StayPurposes)
            .Include(x => x.Profiles).ThenInclude(x => x.Citizenships)
            .Where(x => x.Profiles.Any(p =>
                p.StayDays >= daysInCountry &&
                p.StayPurposes.Any(sp => sp.Name == stayPurposeName) &&
                p.Citizenships.Any(c => c.Name == citizenshipName)))
            .ToListAsync(cancellationToken);

        return rules
            .Select(rule => FindMatchingProfile(rule, entryDate, stayPurposeName, citizenshipName, citizen.Properties))
            .Where(match => match != null)
            .OrderBy(match => match!.Profile.StayDays)
            .ThenByDescending(match => match!.Profile.Properties.Count)
            .ThenBy(match => match!.Rule.Id)
            .FirstOrDefault();
    }

    private static RoadmapMatch? FindMatchingProfile(Rule? rule, DateTime entryDate, string stayPurposeName, string citizenshipName, ICollection<CitizenProfileProperty> citizenProperties)
    {
        if (rule == null)
        {
            return null;
        }

        var daysInCountry = GetDaysInCountry(entryDate);
        var profile = rule.Profiles
            .Where(p =>
                p.StayDays >= daysInCountry &&
                p.StayPurposes.Any(sp => sp.Name == stayPurposeName) &&
                p.Citizenships.Any(c => c.Name == citizenshipName) &&
                RulePropertiesMatch(p.Properties, citizenProperties))
            .OrderBy(p => p.StayDays)
            .ThenByDescending(p => p.Properties.Count)
            .FirstOrDefault();

        return profile == null ? null : new RoadmapMatch(rule, profile);
    }

    private static bool RulePropertiesMatch(ICollection<ProfileProperty> ruleProperties, ICollection<CitizenProfileProperty> citizenProperties)
    {
        var citizenPairs = new HashSet<string>(citizenProperties
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => PropertyKey(x.Name, x.Value)));

        return ruleProperties
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .All(x => citizenPairs.Contains(PropertyKey(x.Name, x.Value)));
    }

    private static CitizenRoadmapDto ToDto(CitizenRoadmapRequest request, RoadmapMatch? match)
    {
        var metrics = match == null ? null : BuildMetrics(request.EntryDate, match.Profile);
        return new CitizenRoadmapDto
        {
            Id = request.Id,
            CitizenId = request.CitizenId,
            EntryDate = request.EntryDate,
            StayPurpose = request.StayPurposeName,
            Citizenship = request.CitizenshipName,
            RuleId = request.RuleId,
            Status = request.Status,
            Message = request.Message,
            StayDays = metrics?.StayDays,
            DaysPassed = metrics?.DaysPassed,
            DaysRemaining = metrics?.DaysRemaining,
            DeadlineDate = metrics?.DeadlineDate,
            IsOverdue = metrics?.IsOverdue ?? false,
            TargetDocument = match?.Rule.TargetDocument == null ? null : DocumentApplicationService.ToDto(match.Rule.TargetDocument),
            Guidance = match?.Rule.Guidance == null ? null : new GuidanceDto { Description = match.Rule.Guidance.Description, Refusal = match.Rule.Guidance.Refusal }
        };
    }

    private static string BuildRoadmapMessage(DateTime entryDate, Profile profile, Rule rule)
    {
        var metrics = BuildMetrics(entryDate, profile);
        var result = new StringBuilder();

        result.AppendLine($"Дата въезда: {entryDate:dd.MM.yyyy}");
        result.AppendLine($"Срок: {metrics.StayDays} дней");
        result.AppendLine($"Крайний срок: {metrics.DeadlineDate:dd.MM.yyyy}");

        if (metrics.IsOverdue)
        {
            result.AppendLine($"Срок истек! Просрочено на {-metrics.DaysRemaining} дней.");
        }
        else
        {
            result.AppendLine($"На прохождение медицинского освидетельствования осталось дней: {metrics.DaysRemaining}.");
            result.AppendLine("Медицинские организации, в которые можно обратиться:");
            foreach (var organization in rule.TargetDocument.Organizations.OrderBy(x => x.Name))
            {
                result.AppendLine($"{organization.Name}: {organization.Address}");
            }
        }

        return result.ToString().TrimEnd();
    }

    private static RoadmapMetrics BuildMetrics(DateTime entryDate, Profile profile)
    {
        var daysPassed = Math.Max(0, (DateTime.UtcNow.Date - entryDate.Date).Days);
        return new RoadmapMetrics(profile.StayDays, daysPassed, profile.StayDays - daysPassed, entryDate.Date.AddDays(profile.StayDays));
    }

    private static int GetDaysInCountry(DateTime entryDate) => Math.Max(1, (DateTime.UtcNow.Date - entryDate.Date).Days + 1);

    private static string PropertyKey(string name, string? value) => $"{name.Trim()}\u001f{value?.Trim() ?? string.Empty}";

    private static void ValidateRequest(CreateCitizenRoadmapRequest request)
    {
        if (request.EntryDate == default) throw new ArgumentException("EntryDate is required.", nameof(request));
        if (request.EntryDate.Date > DateTime.UtcNow.Date) throw new ArgumentException("EntryDate cannot be in the future.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.StayPurposeName)) throw new ArgumentException("StayPurposeName is required.", nameof(request));
    }

    private sealed record RoadmapMatch(Rule Rule, Profile Profile);
    private sealed record RoadmapMetrics(int StayDays, int DaysPassed, int DaysRemaining, DateTime DeadlineDate)
    {
        public bool IsOverdue => DaysRemaining < 0;
    }
}
