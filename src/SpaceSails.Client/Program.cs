using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpaceSails.Client;
using SpaceSails.Client.Rendering;

// THE BLACK BOX GOES IN BEFORE THE ENGINE STARTS (owner's playtest, 2026-08-22). Two of his screenshots
// end at Blazor's textless "An unhandled error has occurred. Reload." — the console had the real text and
// the console died with the tab. CrashLog turns whatever escaped into a note the Captain's desk can show
// and the owner can paste into an issue; it must be listening before anything is running to escape it.
CrashLog.Install();

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
