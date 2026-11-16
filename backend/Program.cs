using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using PensionAgent.Api.Data;
using PensionAgent.Api.Dtos;
using PensionAgent.Api.Services;

var localEnvironmentFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(localEnvironmentFile))
{
    Env.NoClobber().Load(localEnvironmentFile);
}

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var databaseUrl = builder.Configuration["DATABASE_URL"]
    ?? throw new InvalidOperationException("The DATABASE_URL environment variable is required.");
var connectionString = DatabaseConnectionString.Create(databaseUrl);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddDbContextFactory<PensionDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3)));
builder.Services.AddHttpClient("groq", client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
    client.Timeout = TimeSpan.FromSeconds(45);
});
builder.Services.AddScoped<StatePensionProvider>();
builder.Services.AddScoped<OccupationalPensionProvider>();
builder.Services.AddScoped<LifeAndHealthInsuranceProvider>();
builder.Services.AddScoped<PortfolioAggregatorService>();
builder.Services.AddScoped<CoverageGapService>();
builder.Services.AddSingleton<RetirementCalculator>();
builder.Services.AddScoped<AgenticAdvisorService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PensionDbContext>>();
    await using var dbContext = await contextFactory.CreateDbContextAsync();
    await dbContext.Database.EnsureCreatedAsync();
    await PensionSeedData.SeedAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapGet("/", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/api/portfolio/{userId?}", async (
    string? userId,
    PortfolioAggregatorService aggregator,
    CancellationToken cancellationToken) =>
{
    try
    {
        var summary = await aggregator.GetPortfolioAsync(
            string.IsNullOrWhiteSpace(userId) ? PensionSeedData.DemoUserId : userId,
            cancellationToken);
        return Results.Ok(summary);
    }
    catch (KeyNotFoundException exception)
    {
        return Results.NotFound(new { error = exception.Message });
    }
});

app.MapPost("/api/simulate", async (
    SimulationRequest request,
    PortfolioAggregatorService aggregator,
    IDbContextFactory<PensionDbContext> contextFactory,
    RetirementCalculator calculator,
    CancellationToken cancellationToken) =>
{
    if (request.RetirementAge is < 18 or > 100 || request.ExtraMonthlySaving < 0)
    {
        return Results.BadRequest(new { error = "Retirement age must be between 18 and 100 and extra monthly saving cannot be negative." });
    }

    try
    {
        var userId = string.IsNullOrWhiteSpace(request.UserId) ? PensionSeedData.DemoUserId : request.UserId;
        var summary = await aggregator.GetPortfolioAsync(userId, cancellationToken);
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var user = await dbContext.UserProfiles.AsNoTracking()
            .SingleAsync(profile => profile.UserId == userId, cancellationToken);
        var currentAge = user.Age;
        if (request.RetirementAge < currentAge)
        {
            return Results.BadRequest(new { error = "Retirement age cannot be earlier than the user's current age." });
        }

        var additionalMonthlySaving = request.ExtraMonthlySaving;
        var projection = calculator.Calculate(
            summary.TotalCapital,
            summary.MonthlyContribution + additionalMonthlySaving,
            currentAge,
            request.RetirementAge,
            0.05m);

        return Results.Ok(new SimulationResponse(
            userId,
            request.RetirementAge,
            summary.TotalCapital,
            summary.MonthlyContribution,
            additionalMonthlySaving,
            0.05m,
            projection.YearsToRetirement,
            projection.ProjectedCapital,
            projection.EstimatedMonthlyPayout));
    }
    catch (KeyNotFoundException exception)
    {
        return Results.NotFound(new { error = exception.Message });
    }
});

app.MapPost("/api/advisor/chat", async (
    AdvisorChatRequest request,
    AgenticAdvisorService advisor,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 4000)
    {
        return Results.BadRequest(new { error = "Message is required and must be no longer than 4000 characters." });
    }

    try
    {
        var result = await advisor.ChatAsync(request, cancellationToken);
        return Results.Ok(result);
    }
    catch (KeyNotFoundException exception)
    {
        return Results.NotFound(new { error = exception.Message });
    }
    catch (AdvisorUnavailableException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (HttpRequestException exception)
    {
        return Results.Problem(
            $"The advisor provider request failed: {exception.Message}",
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        return Results.Problem(
            "The advisor provider request timed out.",
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
});

app.Run();
