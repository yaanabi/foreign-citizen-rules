using ForeignCitizenRules.Domain;
using Microsoft.EntityFrameworkCore;

namespace ForeignCitizenRules.Persistence;

public sealed class RulesDbContext(DbContextOptions<RulesDbContext> options) : DbContext(options)
{
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ProfileProperty> ProfileProperties => Set<ProfileProperty>();
    public DbSet<Guidance> Guidances => Set<Guidance>();
    public DbSet<Roadmap> Roadmaps => Set<Roadmap>();
    public DbSet<TargetDocument> TargetDocuments => Set<TargetDocument>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<StayPurpose> StayPurposes => Set<StayPurpose>();
    public DbSet<Citizenship> Citizenships => Set<Citizenship>();
    public DbSet<Citizen> Citizens => Set<Citizen>();
    public DbSet<CitizenSession> CitizenSessions => Set<CitizenSession>();
    public DbSet<CitizenProfileProperty> CitizenProfileProperties => Set<CitizenProfileProperty>();
    public DbSet<CitizenRoadmapRequest> CitizenRoadmapRequests => Set<CitizenRoadmapRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Roadmap>().Property(x => x.Version).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<Rule>().Property(x => x.Name).IsRequired().HasMaxLength(200);
        modelBuilder.Entity<Profile>().Property(x => x.Priority).HasDefaultValue(0);
        modelBuilder.Entity<Profile>().Property(x => x.IsFallback).HasDefaultValue(false);
        modelBuilder.Entity<Guidance>().Property(x => x.Description).IsRequired();
        modelBuilder.Entity<TargetDocument>().Property(x => x.Name).IsRequired().HasMaxLength(200);
        modelBuilder.Entity<Organization>().Property(x => x.Name).IsRequired().HasMaxLength(200);
        modelBuilder.Entity<Organization>().Property(x => x.Address).IsRequired().HasMaxLength(300);
        modelBuilder.Entity<ProfileProperty>().Property(x => x.Name).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<ProfileProperty>().Property(x => x.Value).HasMaxLength(300);
        modelBuilder.Entity<StayPurpose>().Property(x => x.Name).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<Citizenship>().Property(x => x.Name).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<Citizen>().Property(x => x.FullName).IsRequired().HasMaxLength(200);
        modelBuilder.Entity<Citizen>().Property(x => x.Email).IsRequired().HasMaxLength(200);
        modelBuilder.Entity<Citizen>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Citizen>().Property(x => x.PasswordHash).IsRequired().HasMaxLength(200);
        modelBuilder.Entity<CitizenSession>().Property(x => x.Token).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<CitizenSession>().HasIndex(x => x.Token).IsUnique();
        modelBuilder.Entity<CitizenProfileProperty>().Property(x => x.Name).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<CitizenProfileProperty>().Property(x => x.Value).HasMaxLength(300);
        modelBuilder.Entity<CitizenRoadmapRequest>().Property(x => x.StayPurposeName).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<CitizenRoadmapRequest>().Property(x => x.CitizenshipName).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<CitizenRoadmapRequest>().Property(x => x.Status).IsRequired().HasMaxLength(30);
        modelBuilder.Entity<CitizenRoadmapRequest>().Property(x => x.Message).IsRequired().HasMaxLength(2000);
        modelBuilder.Entity<CitizenRoadmapRequest>().Property(x => x.MatchedTargetDocumentName).HasMaxLength(200);
        modelBuilder.Entity<CitizenRoadmapRequest>().Property(x => x.MatchedOrganizationsJson);
        modelBuilder.Entity<CitizenRoadmapRequest>().Property(x => x.MatchedGuidanceDescription);

        modelBuilder.Entity<Roadmap>()
            .HasMany(x => x.Rules)
            .WithOne(x => x.Roadmap)
            .HasForeignKey(x => x.RoadmapId);

        modelBuilder.Entity<Rule>()
            .HasOne(x => x.TargetDocument)
            .WithMany(x => x.Rules)
            .HasForeignKey(x => x.TargetDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Rule>()
            .HasOne(x => x.Guidance)
            .WithOne(x => x.Rule)
            .HasForeignKey<Guidance>(x => x.Id);

        modelBuilder.Entity<TargetDocument>()
            .HasMany(x => x.Organizations)
            .WithMany(x => x.TargetDocuments)
            .UsingEntity(j => j.ToTable("TargetDocumentOrganizations"));

        modelBuilder.Entity<Profile>()
            .HasMany(x => x.StayPurposes)
            .WithMany(x => x.Profiles)
            .UsingEntity(j => j.ToTable("ProfileStayPurposes"));

        modelBuilder.Entity<Profile>()
            .HasMany(x => x.Citizenships)
            .WithMany(x => x.Profiles)
            .UsingEntity(j => j.ToTable("ProfileCitizenships"));

        modelBuilder.Entity<Citizen>()
            .HasOne(x => x.Citizenship)
            .WithMany()
            .HasForeignKey(x => x.CitizenshipId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CitizenRoadmapRequest>()
            .HasOne(x => x.Rule)
            .WithMany()
            .HasForeignKey(x => x.RuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
