using ForeignCitizenRules.Domain;

namespace ForeignCitizenRules.Application;

public sealed class RuleBuilder
{
    public Rule BuildRule(
        string name,
        string guidanceDescription,
        Roadmap roadmap,
        TargetDocument targetDocument,
        IEnumerable<Profile> profiles)
    {
        var rule = new Rule
        {
            Name = name.Trim(),
            Roadmap = roadmap,
            TargetDocument = targetDocument,
            Guidance = new Guidance
            {
                Description = guidanceDescription.Trim()
            }
        };

        foreach (var profile in profiles)
        {
            rule.Profiles.Add(profile);
        }

        return rule;
    }
}

public sealed class ProfileBuilder
{
    public Profile BuildProfile(
        int stayDays,
        int priority,
        bool isFallback,
        IEnumerable<StayPurpose> stayPurposes,
        IEnumerable<Citizenship> citizenships,
        IEnumerable<(string Name, string? Value)> properties)
    {
        var profile = new Profile
        {
            StayDays = stayDays,
            Priority = priority,
            IsFallback = isFallback
        };

        foreach (var stayPurpose in stayPurposes)
        {
            profile.StayPurposes.Add(stayPurpose);
        }

        foreach (var citizenship in citizenships)
        {
            profile.Citizenships.Add(citizenship);
        }

        foreach (var property in properties.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
        {
            profile.Properties.Add(new ProfileProperty
            {
                Name = property.Name.Trim(),
                Value = property.Value?.Trim()
            });
        }

        return profile;
    }
}
