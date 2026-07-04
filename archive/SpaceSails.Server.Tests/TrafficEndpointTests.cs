using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SpaceSails.Server.Tests;

public class TrafficEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TrafficEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Traffic_ReturnsPublicBoardData_AndIsDeterministic()
    {
        using HttpClient client = _factory.CreateClient();

        string first = await client.GetStringAsync("/api/traffic?seed=42&count=6");
        string second = await client.GetStringAsync("/api/traffic?seed=42&count=6");

        Assert.Equal(first, second); // same seed, same board — byte-identical

        using JsonDocument doc = JsonDocument.Parse(first);
        JsonElement ships = doc.RootElement;
        Assert.Equal(6, ships.GetArrayLength());

        foreach (JsonElement ship in ships.EnumerateArray())
        {
            Assert.False(string.IsNullOrEmpty(ship.GetProperty("callsign").GetString()));
            Assert.False(string.IsNullOrEmpty(ship.GetProperty("cargoClass").GetString()));
            // Hidden info stays hidden: the board must never leak plans or live state.
            Assert.False(ship.TryGetProperty("plan", out _));
            Assert.False(ship.TryGetProperty("initialState", out _));
        }
    }

    [Fact]
    public async Task Traffic_DifferentSeeds_DifferentSchedules()
    {
        using HttpClient client = _factory.CreateClient();

        string a = await client.GetStringAsync("/api/traffic?seed=1&count=6");
        string b = await client.GetStringAsync("/api/traffic?seed=2&count=6");

        Assert.NotEqual(a, b);
    }
}
