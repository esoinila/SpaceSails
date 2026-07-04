using SpaceSails.Core;
using SpaceSails.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
// No-op unless APPLICATIONINSIGHTS_CONNECTION_STRING is set (ACA secret in production).
builder.Services.AddApplicationInsightsTelemetry();
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

// Pre-flight berth check: catching AddressInUseException later still lets the hosting
// logger dump a stack trace first. Probing the port up front keeps the failure to one
// friendly paragraph (owner request, M18: "something non-exceptiony").
string urls = app.Configuration["urls"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "";
foreach (string url in urls.Split(';', StringSplitOptions.RemoveEmptyEntries))
{
    if (Uri.TryCreate(url.Replace("+", "localhost").Replace("*", "localhost"), UriKind.Absolute, out Uri? uri))
    {
        try
        {
            var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, uri.Port);
            probe.Start();
            probe.Stop();
        }
        catch (System.Net.Sockets.SocketException)
        {
            PrintPortTaken(uri.Port);
            Environment.Exit(1);
        }
    }
}

try
{
    app.Run();
}
catch (IOException ex) when (ex.InnerException is Microsoft.AspNetCore.Connections.AddressInUseException)
{
    PrintPortTaken(0); // backstop for binds the pre-flight probe could not model
    Environment.Exit(1);
}

static void PrintPortTaken(int port)
{
    string berth = port > 0 ? $"Port {port} is" : "The port is";
    Console.Error.WriteLine();
    Console.Error.WriteLine($"  ⚓ {berth} already taken — another SpaceSails (or something else) is moored there.");
    Console.Error.WriteLine("     Either play in the browser tab that instance is serving, stop it, or run on");
    Console.Error.WriteLine($"     a different berth:  dotnet run --project src/SpaceSails.Server --urls http://localhost:{(port > 0 ? port + 1 : 5296)}");
    Console.Error.WriteLine("     (Tip: ./run.ps1 finds a free port for you.)");
    Console.Error.WriteLine();
}

public partial class Program;
