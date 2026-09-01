using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit.Abstractions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1055 · THE ONE SANCTIONED RE-PIN, AND THE GATE IN FRONT OF IT.
///
/// <para>Three snapshot suites keep their pins in <c>Ledgers/*.ledger.txt</c>. This class is the only thing
/// in the repository that may rewrite one, and it does it the only honest way: it RUNS the measurement, lays
/// the ledger down from what it measured, and prints the diff-shape report the crews used to assemble by
/// hand — <i>rows moved, old → new, the delta per row</i>, and for the field sweep the NAME of the field that
/// appeared or disappeared.</para>
///
/// <h3>THE INVOCATION</h3>
/// <code>
/// SPACESAILS_REPIN=1 dotnet test tests/SpaceSails.Client.Tests -c Release \
///   --filter FullyQualifiedName~ThePinsAreRewrittenOnlyWhenAsked \
///   --logger "console;verbosity=detailed"
/// </code>
/// <para>(PowerShell: <c>$env:SPACESAILS_REPIN = "1"</c> first, and clear it afterwards.) The
/// <c>--logger</c> is not decoration: without it <c>dotnet test</c> swallows the output of a PASSING test,
/// and the report IS the deliverable. Paste it into the PR body; a re-pin is reviewed by its report, not by
/// squinting at a table of hex.</para>
///
/// <h3>AND CI NEVER RE-PINS</h3>
/// <para><see cref="PinLedger.Write"/> throws unless <c>SPACESAILS_REPIN</c> reads exactly <c>1</c>, and
/// <see cref="TheOptInIsOffUntilSomebodyTurnsItOn"/> proves it throws — on every CI build, with the ledger's
/// bytes checked before and after. A normal run only ever COMPARES: the guards go red and stay red until a
/// human has looked at what moved.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ThePinsAreRewrittenOnlyWhenAskedTests(ITestOutputHelper output)
{
    /// <summary>Every suite that keeps its pins in a ledger: its name, the note that heads its file, and the
    /// measurement that produces its rows.</summary>
    private static readonly (string Suite, string Preamble, Func<IReadOnlyList<PinLedger.Row>> Measure)[] Suites =
    [
        (EveryFrameHashesTheSameTests.Suite,
         EveryFrameHashesTheSameTests.Preamble,
         EveryFrameHashesTheSameTests.MeasureEveryRow),

        (EveryFrameLeavesTheSameFingerprintTests.Suite,
         EveryFrameLeavesTheSameFingerprintTests.Preamble,
         EveryFrameLeavesTheSameFingerprintTests.MeasureEveryRow),

        (EverySeatTheCaptainTakesFingerprintsTheSameTests.Suite,
         EverySeatTheCaptainTakesFingerprintsTheSameTests.Preamble,
         EverySeatTheCaptainTakesFingerprintsTheSameTests.MeasureEveryRow),
    ];

    private void Say(string text)
    {
        output.WriteLine(text);
        Console.WriteLine(text);
    }

    /// <summary>
    /// THE RE-PIN. Runs every suite's measurement, prints the report, writes the ledgers.
    ///
    /// <para>Without the opt-in this is not a no-op and it is not vacuous either: it asserts that the writer
    /// REFUSES, which is the behaviour CI depends on.</para>
    /// </summary>
    [Fact]
    public void TheLedgersAreRewrittenFromAFreshMeasurement()
    {
        if (Environment.GetEnvironmentVariable(PinLedger.RePinVariable) != "1")
        {
            InvalidOperationException refused = Assert.Throws<InvalidOperationException>(
                () => PinLedger.Write(Suites[0].Suite, Suites[0].Preamble, []));
            Assert.Contains(PinLedger.RePinVariable, refused.Message, StringComparison.Ordinal);
            Say($"the re-pin did not run: {PinLedger.RePinVariable} is not `1`, so every ledger was left "
                + $"exactly as it is. To re-pin:{Environment.NewLine}  {PinLedger.Invocation}");
            return;
        }

        var report = new StringBuilder();
        report.Append("#1055 · RE-PIN REPORT — every number below was MEASURED, none transcribed.")
              .Append(Environment.NewLine).Append(Environment.NewLine);

        foreach ((string suite, string preamble, Func<IReadOnlyList<PinLedger.Row>> measure) in Suites)
        {
            IReadOnlyList<PinLedger.Row> fresh = measure();
            IReadOnlyDictionary<string, PinLedger.Row> pinned =
                File.Exists(PinLedger.PathOf(suite))
                    ? PinLedger.Pinned(suite)
                    : new Dictionary<string, PinLedger.Row>(StringComparer.Ordinal);

            report.Append(PinLedger.Report(suite, pinned, fresh, Explain(suite, pinned, fresh)))
                  .Append(Environment.NewLine);

            PinLedger.Write(suite, preamble, fresh);
        }

        Say(report.ToString());
        Say("The ledgers are rewritten. `git diff tests/SpaceSails.Client.Tests/Ledgers` is one generated "
            + "hunk per probe that moved; the report above is what a reviewer reads.");
    }

    /// <summary>
    /// The sentence a crew used to have to earn with two sweep dumps and a line-by-line diff: when the field
    /// sweep moves, WHICH FIELD did it. The roster block of the fingerprints ledger already names every field
    /// the sweep walks, so the delta is a set difference and nothing more.
    /// </summary>
    private static Func<PinLedger.Row, PinLedger.Row, string?>? Explain(
        string suite,
        IReadOnlyDictionary<string, PinLedger.Row> pinned,
        IReadOnlyList<PinLedger.Row> fresh)
    {
        if (suite != EveryFrameLeavesTheSameFingerprintTests.Suite)
        {
            return null;
        }

        const string roster = "sweep roster";
        var now = new HashSet<string>(
            fresh.Where(r => r.Probe == roster).Select(r => r.Scene), StringComparer.Ordinal);
        var before = new HashSet<string>(
            pinned.Values.Where(r => r.Probe == roster).Select(r => r.Scene), StringComparer.Ordinal);

        string[] appeared = [.. now.Except(before, StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal)];
        string[] gone = [.. before.Except(now, StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal)];

        return (was, _) =>
        {
            if (was.Probe != "sweep")
            {
                return null;
            }
            if (appeared.Length == 0 && gone.Length == 0)
            {
                return "the roster held still, so no field joined or left — a field's VALUE moved "
                    + "(SPACESAILS_SWEEP_DUMP=<dir> on both sides names it)";
            }
            return string.Join("; ",
                new[]
                {
                    appeared.Length > 0 ? $"sweep +{appeared.Length}: {string.Join(", ", appeared)}" : null,
                    gone.Length > 0 ? $"sweep −{gone.Length}: {string.Join(", ", gone)}" : null,
                }.Where(s => s is not null));
        };
    }

    /// <summary>
    /// THE OPT-IN IS OFF UNLESS SOMEBODY TURNS IT ON — the guard that runs on every CI build.
    ///
    /// <para>Not "we did not call the writer": the writer is CALLED, with the variable cleared, and it has to
    /// throw. And the ledger's bytes are read before and after, because a refusal that had already truncated
    /// the file would be no refusal at all.</para>
    /// </summary>
    [Fact]
    public void TheOptInIsOffUntilSomebodyTurnsItOn()
    {
        string? asked = Environment.GetEnvironmentVariable(PinLedger.RePinVariable);
        try
        {
            Environment.SetEnvironmentVariable(PinLedger.RePinVariable, null);

            foreach ((string suite, string preamble, _) in Suites)
            {
                byte[] before = File.ReadAllBytes(PinLedger.PathOf(suite));

                InvalidOperationException refused = Assert.Throws<InvalidOperationException>(
                    () => PinLedger.Write(suite, preamble,
                        [new PinLedger.Row("a probe", "a scene", "a number nobody measured")]));

                Assert.Contains(PinLedger.RePinVariable, refused.Message, StringComparison.Ordinal);
                Assert.Equal(before, File.ReadAllBytes(PinLedger.PathOf(suite)));
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(PinLedger.RePinVariable, asked);
        }
    }

    /// <summary>
    /// EVERY LEDGER ON DISK IS EXACTLY WHAT THE WRITER WOULD LAY DOWN — the anti-hand-edit clause.
    ///
    /// <para>A ledger is machine-written or it is nothing: the whole point of #1055 is that no number in one
    /// was ever typed. So the committed file is re-rendered from its own rows and must come back byte for
    /// byte — same header, same probe-major ordering, same separator. A row moved into the wrong block by
    /// hand, a stray blank line, a re-pin pasted in by a text editor: all red here.</para>
    /// </summary>
    [Fact]
    public void EveryLedgerIsTheFileTheWriterWouldHaveWritten()
    {
        foreach ((string suite, string preamble, _) in Suites)
        {
            IReadOnlyList<PinLedger.Row> rows = PinLedger.Read(suite);
            Assert.True(rows.Count > 0, $"{suite}.ledger.txt pins nothing at all.");

            string onDisk = File.ReadAllText(PinLedger.PathOf(suite)).Replace("\r\n", "\n");
            string rendered = PinLedger.Render(suite, preamble, rows).Replace("\r\n", "\n");

            if (onDisk == rendered)
            {
                continue;
            }
            string[] a = onDisk.Split('\n'), b = rendered.Split('\n');
            int at = 0;
            while (at < Math.Min(a.Length, b.Length) && a[at] == b[at])
            {
                at++;
            }
            Assert.Fail(
                $"{suite}.ledger.txt is not the file the writer lays down — it has been hand-edited, or the "
                + $"format moved without the ledgers being re-written. First difference at line {at + 1}:\n"
                + $"  on disk: {(at < a.Length ? a[at] : "(end of file)")}\n"
                + $"  writer:  {(at < b.Length ? b[at] : "(end of file)")}\n"
                + $"Re-pin BY MEASUREMENT:\n  {PinLedger.Invocation}");
        }
    }
}
