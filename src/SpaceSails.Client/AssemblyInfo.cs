using System.Runtime.Versioning;

// SpaceSails.Client is a Blazor WebAssembly app: every line of it only ever runs inside a browser.
// Marking the whole assembly lets the platform-compatibility analyzer know call sites into the
// System.Runtime.InteropServices.JavaScript ([JSImport]/[JSExport]) interop in Rendering/ are safe,
// instead of requiring an OperatingSystem.IsBrowser() guard at every call site (appropriate for
// library code that might also run server-side, which this project never does).
[assembly: SupportedOSPlatform("browser")]
