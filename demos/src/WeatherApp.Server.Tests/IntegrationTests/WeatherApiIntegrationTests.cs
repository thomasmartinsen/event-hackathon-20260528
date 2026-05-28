using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WeatherApp.Server.Tests.IntegrationTests;

public sealed class WeatherApiIntegrationTests
{
    [Fact]
    public async Task GetHealth_ReturnsHealthyResponse()
    {
        await using var factory = new WeatherAppFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsDeterministicForecasts()
    {
        await using var factory = new WeatherAppFactory();
        using var client = factory.CreateClient();

        var forecasts = await client.GetFromJsonAsync<List<WeatherForecastContract>>("/api/weatherforecast");

        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Count);
        Assert.Collection(
            forecasts,
            forecast => AssertForecast(forecast, 1, new DateOnly(2026, 5, 1), -8, "Bracing", "new"),
            forecast => AssertForecast(forecast, 2, new DateOnly(2026, 5, 2), 3, "Cool", "new"),
            forecast => AssertForecast(forecast, 3, new DateOnly(2026, 5, 3), 14, "Mild", "reviewed"),
            forecast => AssertForecast(forecast, 4, new DateOnly(2026, 5, 4), 29, "Warm", "new"),
            forecast => AssertForecast(forecast, 5, new DateOnly(2026, 5, 5), 41, "Scorching", "escalated"));
    }

    [Fact]
    public async Task PostWeatherForecast_ReturnsCreatedForecast()
    {
        await using var factory = new WeatherAppFactory();
        using var client = factory.CreateClient();

        var payload = new CreateWeatherForecastContract(new DateOnly(2026, 6, 1), 12, "Breezy", null);
        using var response = await client.PostAsJsonAsync("/api/weatherforecast", payload);
        var created = await response.Content.ReadFromJsonAsync<WeatherForecastContract>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("/api/weatherforecast/6", response.Headers.Location!.ToString());
        AssertForecast(created, 6, new DateOnly(2026, 6, 1), 12, "Breezy", "new");
    }

    private static void AssertForecast(
        WeatherForecastContract forecast,
        int id,
        DateOnly date,
        int temperatureC,
        string summary,
        string triageStatus)
    {
        Assert.Equal(id, forecast.Id);
        Assert.Equal(date, forecast.Date);
        Assert.Equal(temperatureC, forecast.TemperatureC);
        Assert.Equal(summary, forecast.Summary);
        Assert.Equal(triageStatus, forecast.TriageStatus);
        Assert.Equal(32 + (int)(temperatureC / 0.5556), forecast.TemperatureF);
    }

    private sealed class WeatherAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
        }
    }

    private sealed record WeatherForecastContract(
        int Id,
        DateOnly Date,
        int TemperatureC,
        int TemperatureF,
        string Summary,
        string TriageStatus);

    private sealed record CreateWeatherForecastContract(
        DateOnly Date,
        int TemperatureC,
        string Summary,
        string? TriageStatus);
}