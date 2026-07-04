using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using SpaceSails.Contracts;

namespace SpaceSails.Server.Tests;

public class GameHubTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GameHubTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HubConnection Connect()
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/hubs/game"),
                o => o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler())
            .Build();
    }

    private static async Task<T> WaitFor<T>(TaskCompletionSource<T> source, int timeoutMs = 15000)
    {
        Task winner = await Task.WhenAny(source.Task, Task.Delay(timeoutMs));
        Assert.True(winner == source.Task, "Timed out waiting for a hub update.");
        return await source.Task;
    }

    [Fact]
    public async Task Join_SpawnsDockedAtEarth_AndUpdatesFlow()
    {
        await using HubConnection connection = Connect();
        var firstUpdate = new TaskCompletionSource<WorldUpdateDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<WorldUpdateDto>("Update", u => firstUpdate.TrySetResult(u));

        await connection.StartAsync();
        JoinResultDto join = await connection.InvokeAsync<JoinResultDto>("Join", "Testarossa");

        Assert.Equal(250, join.ReactionMass);
        double r = Math.Sqrt(join.PositionX * join.PositionX + join.PositionY * join.PositionY);
        Assert.InRange(r, 1.3e11, 1.7e11); // near Earth's orbit

        WorldUpdateDto update = await WaitFor(firstUpdate);
        Assert.Equal(1, update.PlayerCount);
        Assert.True(update.SimTime >= join.SimTime);
    }

    [Fact]
    public async Task Pulse_ChangesVelocity_BurnsMass_AndRespectsCooldown()
    {
        await using HubConnection connection = Connect();
        WorldUpdateDto? latest = null;
        connection.On<WorldUpdateDto>("Update", u => Volatile.Write(ref latest, u));

        await connection.StartAsync();
        JoinResultDto join = await connection.InvokeAsync<JoinResultDto>("Join", "Burner");
        double speed0 = Math.Sqrt(join.VelocityX * join.VelocityX + join.VelocityY * join.VelocityY);

        await connection.InvokeAsync("RequestWarp", 1);
        await connection.InvokeAsync("Pulse", true, false);
        await connection.InvokeAsync("Pulse", true, false); // rejected: inside the 1 s sim cooldown

        WorldUpdateDto update = await PollUntil(() => Volatile.Read(ref latest), u => u is { ReactionMass: < 250 });
        double speed1 = Math.Sqrt(update.VelocityX * update.VelocityX + update.VelocityY * update.VelocityY);

        Assert.Equal(249, update.ReactionMass); // exactly one pulse landed
        Assert.True(Math.Abs(speed1 - speed0 * 1.1) / (speed0 * 1.1) < 1e-6,
            $"Expected one +10% pulse: {speed0:F1} -> {speed1:F1} m/s.");
    }

    [Fact]
    public async Task HiddenInformation_StaysHidden_UntilInSensorRange()
    {
        // Two players join at the same spawn — well inside sensor range of each other —
        // so both should appear in each other's contacts as "player" entries.
        await using HubConnection a = Connect();
        await using HubConnection b = Connect();
        WorldUpdateDto? latestA = null, latestB = null;
        a.On<WorldUpdateDto>("Update", u => Volatile.Write(ref latestA, u));
        b.On<WorldUpdateDto>("Update", u => Volatile.Write(ref latestB, u));

        await a.StartAsync();
        await b.StartAsync();
        await a.InvokeAsync<JoinResultDto>("Join", "Alpha");
        await b.InvokeAsync<JoinResultDto>("Join", "Bravo");

        WorldUpdateDto seesBravo = await PollUntil(() => Volatile.Read(ref latestA),
            u => u is not null && u.Contacts.Any(c => c.Callsign == "Bravo"));
        Assert.Contains(seesBravo.Contacts, c => c.Kind == "player" && c.Callsign == "Bravo");

        // NPCs spawned mid-flight between Saturn and the inner system are far outside the
        // 1e11 m sensor range of an Earth spawn — the packet must simply not contain them.
        WorldUpdateDto b0 = await PollUntil(() => Volatile.Read(ref latestB), u => u is not null);
        Assert.DoesNotContain(b0.Contacts, c => c.Kind == "npc");
    }

    private static async Task<WorldUpdateDto> PollUntil(
        Func<WorldUpdateDto?> read, Func<WorldUpdateDto?, bool> condition, int timeoutMs = 20000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            WorldUpdateDto? current = read();
            if (condition(current))
            {
                return current!;
            }
            await Task.Delay(100);
        }

        Assert.Fail("Timed out waiting for hub state.");
        return null!;
    }
}
