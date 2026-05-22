using ForeignCitizenRules.Domain;

namespace ForeignCitizenRules.Application;

public sealed class RuleBuilder
{
    private readonly Rule _rule = new();

    public RuleBuilder SetName(string name)
    {
        _rule.Name = name.Trim();
        return this;
    }

    public RuleBuilder AddProfile(Profile profile)
    {
        _rule.Profiles.Add(profile);
        return this;
    }

    public RuleBuilder AddDocument(TargetDocument document)
    {
        _rule.TargetDocument = document;
        return this;
    }

    public RuleBuilder AddGuidance(Guidance guidance)
    {
        _rule.Guidance = guidance;
        return this;
    }

    public RuleBuilder AddRoadmap(Roadmap roadmap)
    {
        _rule.Roadmap = roadmap;
        return this;
    }

    public Rule GetRule()
    {
        return _rule;
    }
}

public sealed class ProfileBuilder
{
    private readonly Profile _profile = new();

    public ProfileBuilder SetStayDays(int days)
    {
        _profile.StayDays = days;
        return this;
    }

    public ProfileBuilder SetPriority(int priority)
    {
        _profile.Priority = priority;
        return this;
    }

    public ProfileBuilder SetFallback(bool isFallback)
    {
        _profile.IsFallback = isFallback;
        return this;
    }

    public ProfileBuilder AddStayPurpose(StayPurpose stayPurpose)
    {
        _profile.StayPurposes.Add(stayPurpose);
        return this;
    }

    public ProfileBuilder AddCitizenship(Citizenship citizenship)
    {
        _profile.Citizenships.Add(citizenship);
        return this;
    }

    public ProfileBuilder AddProfileProperty(string name, string? value)
    {
        _profile.Properties.Add(new ProfileProperty
        {
            Name = name.Trim(),
            Value = value?.Trim()
        });

        return this;
    }

    public Profile GetProfile()
    {
        return _profile;
    }
}
