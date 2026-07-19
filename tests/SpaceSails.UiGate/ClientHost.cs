using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SpaceSails.UiGate;

// Serves the PUBLISHED Blazor WASM client the way GitHub Pages will — so the boot the gate proves
// is the boot the owner's players get. Two modes, one shape:
//
//   * CI passes SPACESAILS_PUBLISH_DIR (a `dotnet publish` already done as a workflow step, so the
//     ~5-minute job never publishes twice).
//   * A bare local `dotnet test tests/SpaceSails.UiGate` finds no such dir and publishes once into a
//     temp folder itself — the constraint is that the gate runs with nothing but `dotnet test`.
//
// Either way the wwwroot is hosted in-process by Kestrel with UseBlazorFrameworkFiles (the standard
// standalone-WASM host recipe: correct _framework content-types + SPA fallback to index.html so
// /map?scenario=sol resolves).
internal sealed class ClientHost : IAsyncDisposable
{
    private WebApplication? _app;

    public string BaseUrl { get; private set; } = "";

    // The resolved wwwroot actually being served — exposed so the gate can inspect the artifact it
    // is about to drive (e.g. read the native-wasm size to tell an AOT payload from an interpreted
    // one, which decides which load-speed budget applies). See BootAndReachabilityTests.IsAotPayload.
    public string WwwrootPath { get; private set; } = "";

    public static async Task<ClientHost> StartAsync(TextWriter log)
    {
        string wwwroot = ResolvePublishedWwwroot(log);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            WebRootPath = wwwroot,
            ContentRootPath = wwwroot,
        });
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        // Port 0 → the OS hands us a free port, so parallel CI jobs never collide (and we never
        // stomp the owner's dev server on 5073).
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        WebApplication app = builder.Build();
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.MapFallbackToFile("index.html");

        await app.StartAsync();

        string url = app.Urls.First();
        var host = new ClientHost { _app = app, BaseUrl = url, WwwrootPath = wwwroot };
        log.WriteLine($"[client-host] serving {wwwroot} at {url}");
        return host;
    }

    private static string ResolvePublishedWwwroot(TextWriter log)
    {
        string? prebuilt = Environment.GetEnvironmentVariable("SPACESAILS_PUBLISH_DIR");
        if (!string.IsNullOrWhiteSpace(prebuilt))
        {
            string root = LocateWwwroot(prebuilt)
                ?? throw new DirectoryNotFoundException(
                    $"SPACESAILS_PUBLISH_DIR='{prebuilt}' has no wwwroot/index.html — was `dotnet publish` run?");
            log.WriteLine($"[client-host] using pre-published output: {root}");
            return root;
        }

        // No pre-publish: do it ourselves, once, into temp. Slow (~1-2 min for a Release WASM
        // build) but it keeps a bare local `dotnet test` self-contained.
        string repoRoot = RepoRoot();
        string clientProj = Path.Combine(repoRoot, "src", "SpaceSails.Client", "SpaceSails.Client.csproj");
        string outDir = Path.Combine(Path.GetTempPath(), "spacesails-uigate-publish");
        log.WriteLine($"[client-host] SPACESAILS_PUBLISH_DIR unset — publishing {clientProj} → {outDir}");

        var psi = new ProcessStartInfo("dotnet",
            $"publish \"{clientProj}\" -c Release -o \"{outDir}\" --nologo")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Could not start `dotnet publish`.");
        // Drain BOTH pipes concurrently. A Release WASM publish floods stdout (asset + brotli
        // logs); reading one stream to end while the other's OS buffer fills deadlocks the child
        // — which is exactly what hung the first run of this gate.
        Task<string> outTask = proc.StandardOutput.ReadToEndAsync();
        Task<string> errTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        string stdout = outTask.GetAwaiter().GetResult();
        string stderr = errTask.GetAwaiter().GetResult();
        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"`dotnet publish` failed (exit {proc.ExitCode}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }

        return LocateWwwroot(outDir)
            ?? throw new DirectoryNotFoundException($"Published, but found no wwwroot/index.html under {outDir}.");
    }

    // A publish `-o dir` puts the standalone app under dir/wwwroot; accept either the parent or the
    // wwwroot itself (and a couple of nested shapes) so callers can point at whichever they have.
    private static string? LocateWwwroot(string dir)
    {
        foreach (string candidate in new[] { Path.Combine(dir, "wwwroot"), dir })
        {
            if (File.Exists(Path.Combine(candidate, "index.html")))
            {
                return candidate;
            }
        }
        return null;
    }

    private static string RepoRoot()
    {
        // Walk up from the test assembly until the solution file appears.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SpaceSails.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName
            ?? throw new DirectoryNotFoundException("Could not find SpaceSails.slnx above the test assembly.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
