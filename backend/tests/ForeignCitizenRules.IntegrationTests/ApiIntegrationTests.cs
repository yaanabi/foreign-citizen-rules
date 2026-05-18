using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ForeignCitizenRules.Application;
using ForeignCitizenRules.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ForeignCitizenRules.IntegrationTests;

[TestClass]
public sealed class ApiIntegrationTests
{
    private static readonly string DatabaseName = "foreign_citizen_rules_test_" + Guid.NewGuid().ToString("N");
    private static readonly string ConnectionString = $"Host=localhost;Port=5433;Database={DatabaseName};Username=postgres;Password=postgres";
    private static RulesApiFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        _factory = new RulesApiFactory(ConnectionString);
        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        db.Database.EnsureDeleted();
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task FullScenario_ShouldCreateRuleLoginAndReturnRoadmap()
    {
        var token = Guid.NewGuid().ToString("N")[..6];
        var organization = await CreateOrganization(token);
        var document = await CreateTargetDocument(token, organization.Id);

        var ruleRequest = new CreateRuleRequest
        {
            Name = "Rule-" + token,
            RoadmapVersion = "v1",
            TargetDocumentId = document.Id,
            Guidance = new GuidanceRequest
            {
                Description = "Пройти медицинское освидетельствование.",
                Refusal = "Правило не подходит."
            },
            Profiles =
            [
                new ProfileRequest
                {
                    StayDays = 30,
                    StayPurposes = ["WORK-" + token],
                    Citizenships = ["KZ-" + token],
                    Properties = [new ProfilePropertyRequest { Name = "isHighQualifiedSpecialist", Value = "true" }]
                }
            ]
        };

        var createRuleResponse = await _client.PostAsJsonAsync("/api/v1/rules", ruleRequest);
        Assert.AreEqual(HttpStatusCode.Created, createRuleResponse.StatusCode);
        var rule = await createRuleResponse.Content.ReadFromJsonAsync<RuleDto>();
        Assert.IsNotNull(rule);

        var login = await RegisterAndLogin(token, "KZ-" + token);
        await UpdateCitizenProperties(login.Token, [new ProfilePropertyRequest { Name = "isHighQualifiedSpecialist", Value = "true" }]);

        var roadmap = await CreateRoadmap(login.Token, DateTime.UtcNow.Date, "WORK-" + token);

        Assert.AreEqual("matched", roadmap.Status);
        Assert.AreEqual(rule.Id, roadmap.RuleId);
        Assert.AreEqual(30, roadmap.StayDays);
        Assert.IsFalse(roadmap.IsOverdue);
        Assert.IsTrue(roadmap.Message.Contains("Дата въезда"));
        Assert.IsNotNull(roadmap.TargetDocument);
    }

    [TestMethod]
    public async Task Roadmap_ShouldNotMatch_WhenCitizenPropertyIsMissing()
    {
        var token = Guid.NewGuid().ToString("N")[..6];
        var organization = await CreateOrganization("missing-" + token);
        var document = await CreateTargetDocument("missing-" + token, organization.Id);
        await _client.PostAsJsonAsync("/api/v1/rules", new CreateRuleRequest
        {
            Name = "Rule-missing-" + token,
            RoadmapVersion = "v1",
            TargetDocumentId = document.Id,
            Guidance = new GuidanceRequest { Description = "Do", Refusal = "No" },
            Profiles =
            [
                new ProfileRequest
                {
                    StayDays = 30,
                    StayPurposes = ["WORK-" + token],
                    Citizenships = ["KZ-" + token],
                    Properties = [new ProfilePropertyRequest { Name = "hasBenefit", Value = "true" }]
                }
            ]
        });

        var login = await RegisterAndLogin("missing-" + token, "KZ-" + token);
        var roadmap = await CreateRoadmap(login.Token, DateTime.UtcNow.Date, "WORK-" + token);

        Assert.AreEqual("not_found", roadmap.Status);
        Assert.IsNull(roadmap.RuleId);
        Assert.IsNull(roadmap.TargetDocument);
    }

    private static async Task<OrganizationDto> CreateOrganization(string token)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/organizations", new OrganizationRequest
        {
            Name = "Org-" + token,
            Address = "Address-" + token
        });
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrganizationDto>())!;
    }

    private static async Task<TargetDocumentDto> CreateTargetDocument(string token, int organizationId)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/target-documents", new CreateTargetDocumentRequest
        {
            Name = "Document-" + token,
            OrganizationIds = [organizationId]
        });
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TargetDocumentDto>())!;
    }

    private static async Task<LoginCitizenResponse> RegisterAndLogin(string token, string citizenshipName)
    {
        var email = "citizen-" + token + "@mail.test";
        var register = await _client.PostAsJsonAsync("/api/v1/citizens/register", new RegisterCitizenRequest
        {
            FullName = "Citizen " + token,
            Email = email,
            Password = "password",
            CitizenshipName = citizenshipName
        });
        Assert.AreEqual(HttpStatusCode.Created, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/v1/citizens/login", new LoginCitizenRequest
        {
            Email = email,
            Password = "password"
        });
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
        return (await login.Content.ReadFromJsonAsync<LoginCitizenResponse>())!;
    }

    private static async Task UpdateCitizenProperties(string token, List<ProfilePropertyRequest> properties)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/citizens/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new UpdateCitizenRequest { Properties = properties });
        var response = await _client.SendAsync(request);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<CitizenRoadmapDto> CreateRoadmap(string token, DateTime entryDate, string stayPurposeName)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/citizens/me/roadmaps");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new CreateCitizenRoadmapRequest
        {
            EntryDate = entryDate,
            StayPurposeName = stayPurposeName
        });
        var response = await _client.SendAsync(request);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CitizenRoadmapDto>())!;
    }

    private sealed class RulesApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:RulesDb"] = connectionString
                });
            });
        }
    }
}
