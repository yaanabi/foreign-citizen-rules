using ForeignCitizenRules.Application;
using ForeignCitizenRules.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("RulesDb")
    ?? "Host=localhost;Port=5432;Database=foreign_citizen_rules;Username=postgres;Password=postgres";

builder.Services.AddDbContext<RulesDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<RuleApplicationService>();
builder.Services.AddScoped<DocumentApplicationService>();
builder.Services.AddScoped<ReferenceApplicationService>();
builder.Services.AddScoped<CitizenApplicationService>();
builder.Services.AddScoped<RoadmapApplicationService>();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

public partial class Program;
