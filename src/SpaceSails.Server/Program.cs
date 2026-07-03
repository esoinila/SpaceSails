using SpaceSails.Core;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

// The public departures board (plan §M5: "NPC planner in Server host"). Deliberately returns
// only what a harbor board would publish — never the flight plan or live state; hidden info
// stays hidden (plan §M9), and observation is the client's sensor problem.
app.MapGet("/api/traffic", (ulong seed, int count) =>
{
    string path = Path.Combine(AppContext.BaseDirectory, "scenarios", "sol.json");
    var ephemeris = CircularOrbitEphemeris.FromScenario(ScenarioLoader.LoadFile(path));
    IReadOnlyList<NpcShip> ships = TrafficSchedule.Generate(ephemeris, seed, Math.Clamp(count, 1, 32));

    return Results.Ok(ships.Select(s => new
    {
        s.Id,
        s.Callsign,
        s.CargoClass,
        s.OriginId,
        s.DestinationId,
        s.DepartureTime,
    }));
});

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
