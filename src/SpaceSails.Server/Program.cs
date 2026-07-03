using SpaceSails.Core;
using SpaceSails.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
// One shared session per server (plan §M9). Registered once, exposed both as itself (hub
// command target) and as the hosted service that runs the authoritative tick.
builder.Services.AddSingleton<SessionHost>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SessionHost>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
// Explicit routing AFTER the static middlewares: WebApplication otherwise inserts UseRouting
// at the top of the pipeline, and the endpoint that MapStaticAssets matches there collides
// with UseBlazorFrameworkFiles' inner static-file branch (500: "reached the end of the
// pipeline without executing the endpoint").
app.UseRouting();
// .NET 10 fingerprints client wwwroot assets (renderer.js -> renderer.<hash>.js) and the WASM
// boot config imports them by the fingerprinted name. MapStaticAssets serves that mapping for
// referenced projects; plain UseStaticFiles alone 404s the hashed names when the Server hosts
// the client (found by the M9 two-browser accept run).
app.MapStaticAssets();

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

app.MapHub<GameHub>("/hubs/game");

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
