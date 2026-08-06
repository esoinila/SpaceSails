using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

// #726 · The deck-audit test project (tests/SpaceSails.Client.Tests, in the solution since #573) exists to
// audit the SHIPPING objects instead of copies of them — that is the whole reason it references the client.
// The once-per-news console announcer is an internal detail of this app, but a guard that reasoned about its
// own re-implementation of the rule would prove nothing, so the test assembly is let in to drive the real one.
[assembly: InternalsVisibleTo("SpaceSails.Client.Tests")]

// SpaceSails.Client is a Blazor WebAssembly app: every line of it only ever runs inside a browser.
// Marking the whole assembly lets the platform-compatibility analyzer know call sites into the
// System.Runtime.InteropServices.JavaScript ([JSImport]/[JSExport]) interop in Rendering/ are safe,
// instead of requiring an OperatingSystem.IsBrowser() guard at every call site (appropriate for
// library code that might also run server-side, which this project never does).
[assembly: SupportedOSPlatform("browser")]
