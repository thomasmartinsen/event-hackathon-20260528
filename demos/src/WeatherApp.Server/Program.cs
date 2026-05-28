using Microsoft.AspNetCore.OutputCaching;
using WeatherApp.Server.Triage;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddOutputCache();
}
else
{
    builder.AddRedisClientBuilder("cache")
        .WithOutputCache();
}

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<ForecastStore>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseOutputCache();

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { status = "ok" }));

api.MapGet("weatherforecast", (ForecastStore store) => store.List())
.CacheOutput(p => p.Expire(TimeSpan.FromSeconds(5)))
.WithName("GetWeatherForecast");

api.MapGet("weatherforecast/{id:int}", (int id, ForecastStore store) =>
{
    var forecast = store.Get(id);
    return forecast is null ? Results.NotFound() : Results.Ok(forecast);
});

api.MapPost("weatherforecast", (CreateWeatherForecastRequest request, ForecastStore store) =>
{
    var validationErrors = ValidateCreateRequest(request);
    if (validationErrors is not null)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var created = store.Create(request);
    return Results.Created($"/api/weatherforecast/{created.Id}", created);
});

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

static Dictionary<string, string[]>? ValidateCreateRequest(CreateWeatherForecastRequest request)
{
    var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

    if (request.Date is null)
    {
        errors["date"] = ["The date field is required."];
    }

    if (request.TemperatureC < -100 || request.TemperatureC > 100)
    {
        errors["temperatureC"] = ["The temperatureC field must be between -100 and 100."];
    }

    if (string.IsNullOrWhiteSpace(request.Summary))
    {
        errors["summary"] = ["The summary field is required."];
    }
    else if (request.Summary.Length > 100)
    {
        errors["summary"] = ["The summary field must be 100 characters or fewer."];
    }

    if (!string.IsNullOrWhiteSpace(request.TriageStatus)
        && request.TriageStatus is not ("new" or "reviewed" or "escalated"))
    {
        errors["triageStatus"] = ["The triageStatus field must be one of: new, reviewed, escalated."];
    }

    return errors.Count == 0 ? null : errors;
}

public partial class Program;

internal sealed record WeatherForecastResponse(int Id, DateOnly Date, int TemperatureC, string Summary, string TriageStatus)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal sealed record CreateWeatherForecastRequest(DateOnly? Date, int TemperatureC, string? Summary, string? TriageStatus);

internal sealed class ForecastStore
{
    private readonly Lock syncRoot = new();
    private readonly List<WeatherForecastResponse> forecasts =
    [
        new(1, new DateOnly(2026, 5, 1), -8, "Bracing", "new"),
        new(2, new DateOnly(2026, 5, 2), 3, "Cool", "new"),
        new(3, new DateOnly(2026, 5, 3), 14, "Mild", "reviewed"),
        new(4, new DateOnly(2026, 5, 4), 29, "Warm", "new"),
        new(5, new DateOnly(2026, 5, 5), 41, "Scorching", ForecastTriageEvaluator.Evaluate(41, "Scorching").TriageStatus)
    ];

    private int nextId = 6;

    public IReadOnlyList<WeatherForecastResponse> List()
    {
        lock (syncRoot)
        {
            return forecasts
                .OrderBy(forecast => forecast.Date)
                .ThenBy(forecast => forecast.Id)
                .ToArray();
        }
    }

    public WeatherForecastResponse? Get(int id)
    {
        lock (syncRoot)
        {
            return forecasts.FirstOrDefault(forecast => forecast.Id == id);
        }
    }

    public WeatherForecastResponse Create(CreateWeatherForecastRequest request)
    {
        lock (syncRoot)
        {
            var forecast = new WeatherForecastResponse(
                nextId++,
                request.Date!.Value,
                request.TemperatureC,
                request.Summary!.Trim(),
                string.IsNullOrWhiteSpace(request.TriageStatus) ? "new" : request.TriageStatus);

            forecasts.Add(forecast);
            return forecast;
        }
    }
}
