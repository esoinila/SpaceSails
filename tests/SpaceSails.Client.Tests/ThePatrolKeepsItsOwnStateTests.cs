using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 6′a · THE PATROL KEEPS ITS OWN STATE.
///
/// <para>The round is six partials of <c>Map</c> — <c>Map.Patrol.cs</c> and its <c>.Round</c>,
/// <c>.Challenge</c>, <c>.Escort</c>, <c>.Hide</c> and <c>.Run</c> — and between them they hold
/// <b>twenty-two fields</b>: the guards themselves, the beat they walk, the escort's four, what the watch
/// remembers, the kick-out's three, the Fletch wallet's three, the hide's one line, and two dev cheats. Four
/// times the seat's five, which is why #870's 6′ ladder exists at all.</para>
///
/// <para>Because they are partials of one very large page, every other partial in the client — and the
/// markup — could reach straight past the family's vocabulary and read those fields raw. This guard says it
/// may not. A bench asks whether anybody is walking the floor; a bin asks whether the rota has eyes on the
/// captain; a sweep team asks the round for its contacts; the vault asks for the paper trail. None of them
/// touches a field.</para>
///
/// <h3>The three facts, and why each is here</h3>
///
/// <list type="number">
/// <item><b>Nobody outside the six names a raw patrol field</b> — not in a <c>.cs</c> partial, not in the
/// <c>.razor</c> markup, and <b>not in a comment</b>: a comment that has to name another family's private
/// field is the same coupling wearing a hat.</item>
/// <item><b>The six family files are all really there.</b> A path that matches nothing exempts nothing. If
/// 6′b moves the family onto a <c>Patrol</c> object, this reddens rather than quietly widening.</item>
/// <item><b>The names are still inside the family, and on the running page.</b> The anti-vacuous half,
/// asked twice — of the text, and of <see cref="Pages.Map"/> itself by reflection. Without it a rename or a
/// deletion makes fact 1 pass by leaving nothing to find, which is this repo's fifth named bug class.</item>
/// </list>
///
/// <para>Proven RED on the tree before a single call site was edited: twenty raw reads, at seventeen sites,
/// in eight files. The verbatim output is in #870 lane 6′a's PR body.</para>
///
/// <para>6′b is the lane this shape protects: the twenty-two become properties on one <c>Patrol</c> object,
/// and this guard ratchets from "outside the family" to "nowhere at all", exactly as the seat's did between
/// #902 and #904.</para>
/// </summary>
public sealed class ThePatrolKeepsItsOwnStateTests
{
    /// <summary>The six partials the patrol's state belongs to, relative to <c>src/SpaceSails.Client</c>.
    /// Lane 5b's split; the header note lives in the first of them.</summary>
    private static readonly string[] TheFamily =
    [
        "Pages/Map.Patrol.cs",
        "Pages/Map.Patrol.Challenge.cs",
        "Pages/Map.Patrol.Escort.cs",
        "Pages/Map.Patrol.Hide.cs",
        "Pages/Map.Patrol.Round.cs",
        "Pages/Map.Patrol.Run.cs",
    ];

    /// <summary>Every field the six keep, and one line each on what it is. Measured off the tree, not off
    /// the spec: the LANE 6′ SPEC listed twenty and the family really carries twenty-two — <c>_escortCar</c>
    /// (declared in the same block as the other three escort fields) and <c>_walkedPastSaid</c> (declared in
    /// <c>Map.Patrol.Hide.cs</c> rather than in the core file) were both missing from that list.</summary>
    private static readonly (string Field, string What)[] TheTwentyTwo =
    [
        ("_guards", "the men on the floor"),
        ("_patrolReadables", "what a held man could be reading off this floor's walls"),
        ("_patrolBeat", "the stops, in the order this watch walks them"),
        ("_patrolFloorSeconds", "how long the captain has been on this floor"),
        ("_patrolHeardAgo", "how long since the boots were mentioned"),
        ("_escort", "the guard walking the captain back to the car"),
        ("_escortDue", "the guard whose walk back has not started yet"),
        ("_escortCar", "where the walk back ends"),
        ("_escortSeconds", "how long the walk back has been going"),
        ("_escortSaidPumps", "whether the small talk has landed"),
        ("_patrolWatch", "which watch the two counters belong to"),
        ("_escortsThisWatch", "how many times you have been walked back this watch"),
        ("_walkedAwayThisWatch", "how many hails you have walked away from this watch"),
        ("_kickOutDue", "whether this walk ends at the sky"),
        ("_kickOutRideDue", "the ride up, armed rather than taken"),
        ("_kickedOutPlateFor", "how long the KICKED OUT plate stays painted"),
        ("_paperInHand", "the paper that goes into his hand when he arrives"),
        ("_walletFanOpen", "is the fan up"),
        ("_shownBook", "the captain's own paper trail"),
        ("_walkedPastSaid", "whether the round has already been heard going past this hide"),
        ("_patrolCheat", "?patrol=N"),
        ("_badgeCheat", "?badge=1"),
    ];

    /// <summary>What the rest of the page may ask instead — the family's own vocabulary, the names this
    /// lane's seventeen sites were rewritten onto, and the list a failing message hands the next author.
    /// </summary>
    private static readonly string[] AskTheseInstead =
    [
        "CaptainIsUnderEscort",
        "TheRoundOnFoot",
        "TheRoundHasEyesOnYou",
        "RememberWhoWatchedTheCatchGoOver",
        "EverybodyForgetsTheCatch",
        "TheNextHideGetsItsOwnLine",
        "ThePaperInYourHandIs",
        "TheBookOn",
        "YourPaperTrail",
        "ForgetThePaperTrail",
        "RestoreAPaperTrailRow",
        "ForceTheRoundsTo",
        "TheQueryHasForcedARound",
        "ForceARoundIfNoneAsked",
        "MintTheSitePassAtTheLanding",
        "TheSitePassIsMintedAtTheLanding",
    ];

    /// <summary>Word-anchored, so <c>_escortDue</c> and <c>_escortCar</c> are not <c>_escort</c>, and
    /// <c>_patrolBeatCheat</c> would not be <c>_patrolBeat</c>. In .NET an underscore is a word character,
    /// so <c>\b_escort\b</c> is exactly "this name and no longer one".</summary>
    private static Regex Needle(string name) =>
        new(@"\b" + Regex.Escape(name) + @"\b", RegexOptions.CultureInvariant);

    // ── (1) NOBODY OUTSIDE THE SIX ────────────────────────────────────────────────────────────────────

    [Fact]
    public void NobodyOutsideThePatrolFamilyNamesARawPatrolField()
    {
        var trespass = new List<string>();

        foreach (string path in ClientSources())
        {
            string relative = Relative(path);
            if (TheFamily.Contains(relative, StringComparer.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                foreach ((string field, string what) in TheTwentyTwo)
                {
                    if (Needle(field).IsMatch(lines[i]))
                    {
                        trespass.Add($"  {relative}:{i + 1} names {field} ({what}) — {lines[i].Trim()}");
                    }
                }
            }
        }

        Assert.True(
            trespass.Count == 0,
            "#870 lane 6′a · THE PATROL'S STATE IS THE PATROL'S. These twenty-two fields belong to the six " +
            "patrol partials and are read out here raw:\n" +
            string.Join("\n", trespass) +
            "\n\nAsk the family the question you actually mean instead — " +
            string.Join(", ", AskTheseInstead) +
            " — or tell it to do the thing. If none of them says what you need, ADD one small named member " +
            "to Map.Patrol.cs (or to the part of the family that owns the beat) and say in its docblock " +
            "which site asked for it. Do not widen this guard's exemption list: it is the patrol family, " +
            "and it is six files.");
    }

    // ── (2) AND THE SIX ARE REALLY THERE ──────────────────────────────────────────────────────────────

    [Fact]
    public void TheSixFamilyFilesAreAllReallyThere()
    {
        List<string> missing = TheFamily
            .Where(f => !File.Exists(Path.Combine(ClientRoot, f.Replace('/', Path.DirectorySeparatorChar))))
            .ToList();

        Assert.True(
            missing.Count == 0,
            "#870 lane 6′a · this guard exempts six files by path and these do not exist:\n  " +
            string.Join("\n  ", missing) +
            "\n\nA path that matches nothing exempts nothing and proves nothing. If the family moved or was " +
            "split again, re-PATH these constants in the same commit — never delete a row to make the sweep " +
            "quiet.");
    }

    // ── (3) AND IT IS NOT VACUOUS ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheTwentyTwoNamesAreStillInsideTheFamilyAndOnThePage()
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        string family = string.Join(
            "\n",
            TheFamily.Select(f =>
                File.ReadAllText(Path.Combine(ClientRoot, f.Replace('/', Path.DirectorySeparatorChar)))));

        var gone = new List<string>();

        foreach ((string field, _) in TheTwentyTwo)
        {
            if (!Needle(field).IsMatch(family))
            {
                gone.Add($"  {field} is named nowhere in the six family files");
            }

            if (typeof(Pages.Map).GetField(field, Hidden) is null)
            {
                gone.Add($"  {field} is not a field on the running Map at all");
            }
        }

        Assert.True(
            gone.Count == 0,
            "#870 lane 6′a · ANTI-VACUOUS HALF, asked of the text AND of the running type:\n" +
            string.Join("\n", gone) +
            "\n\nA sweep for absence passes gloriously on a tree where the state was simply renamed or " +
            "deleted — the world can no longer tell pass from fail, which is this repo's fifth named bug " +
            "class. If a field really did move (6′b puts all twenty-two on a Patrol object), update this " +
            "list in the same commit as the move.");
    }

    // ── WHERE THE SOURCE IS ───────────────────────────────────────────────────────────────────────────

    /// <summary>Every shipped client source: the C# partials and the markup. Anything the compiler reads,
    /// this guard reads — <c>obj/</c> and <c>bin/</c> excluded, because a generated copy of a file is not a
    /// second author naming the field.</summary>
    private static IEnumerable<string> ClientSources() =>
        Directory.EnumerateFiles(ClientRoot, "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || p.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .Where(p => !Segments(p).Any(s =>
                s.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || s.Equals("bin", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p => p, StringComparer.Ordinal);

    private static string[] Segments(string path) =>
        Relative(path).Split('/');

    private static string Relative(string path) =>
        Path.GetRelativePath(ClientRoot, path).Replace('\\', '/');

    private static string ClientRoot { get; } =
        Path.Combine(RepoRoot(), "src", "SpaceSails.Client");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SpaceSails.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary.");
    }
}
