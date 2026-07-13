using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SpaceSails.LabViz;

/// <summary>
/// The command-line entry point for lab visualizations. A lab checks <see cref="Wants"/> to decide
/// whether the user asked for a picture, and calls <see cref="Show"/> to write and open it. Every
/// line this class prints is gated behind <see cref="Wants"/>, so a lab's stdout stays
/// byte-identical to today whenever <c>--viz</c> is absent.
/// </summary>
public static class LabViz
{
    private const string VizFlag = "--viz";
    private const string OutFlag = "--viz-out=";
    private const string NoOpenFlag = "--viz-no-open";

    /// <summary>True iff <paramref name="args"/> contains <c>--viz</c>. The related <c>--viz-out=&lt;path&gt;</c>
    /// and <c>--viz-no-open</c> flags are honored by <see cref="Show"/>.</summary>
    public static bool Wants(string[] args) => Array.IndexOf(args, VizFlag) >= 0;

    /// <summary>
    /// Write <c>scene.ToHtml()</c> to <c>labviz/&lt;slug&gt;.html</c> under the current directory (or the
    /// path given by <c>--viz-out=</c>), print the path to stdout, and open it in the default browser
    /// unless <c>--viz-no-open</c> is present.
    /// </summary>
    public static void Show(VizScene scene, string[] args)
    {
        string outPath = ResolveOutPath(scene, args);
        string? dir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(outPath, scene.ToHtml());
        Console.WriteLine($"[viz] wrote {outPath}");

        if (Array.IndexOf(args, NoOpenFlag) < 0)
        {
            OpenInBrowser(Path.GetFullPath(outPath));
        }
    }

    private static string ResolveOutPath(VizScene scene, string[] args)
    {
        foreach (string arg in args)
        {
            if (arg.StartsWith(OutFlag, StringComparison.Ordinal))
            {
                return arg[OutFlag.Length..];
            }
        }

        return Path.Combine("labviz", $"{scene.Slug}.html");
    }

    private static void OpenInBrowser(string fullPath)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", fullPath);
            }
            else
            {
                Process.Start("xdg-open", fullPath);
            }
        }
        catch (Exception ex)
        {
            // Opening a browser is a convenience, never a failure mode for the lab: the file is
            // already on disk and its path was printed.
            Console.WriteLine($"[viz] could not open browser automatically ({ex.Message}); open {fullPath} manually.");
        }
    }
}
