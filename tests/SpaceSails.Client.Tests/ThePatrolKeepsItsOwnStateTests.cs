using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 6′a/6′b · THE PATROL KEEPS ITS OWN STATE.
///
/// <para>The round is six partials of <c>Map</c> — <c>Map.Patrol.cs</c> and its <c>.Round</c>,
/// <c>.Challenge</c>, <c>.Escort</c>, <c>.Hide</c> and <c>.Run</c> — and between them they used to hold
/// <b>twenty-two loose fields</b>: the guards themselves, the beat they walk, the escort's five, what the
/// watch remembers, the kick-out's three, the Fletch wallet's three, the hide's one line, and two dev
/// cheats. Four times the seat's five, which is why #870's 6′ ladder exists at all.</para>
///
/// <para><b>6′a</b> stopped every file outside the family reading those fields raw: seventeen sites in eight
/// files ask a named question instead. <b>6′b — this shape — is the lane that seam was cut for.</b> The
/// twenty-two are properties on ONE object now (<c>Map.Patrol</c>, <c>Pages/Patrol/Patrol.cs</c>), the page
/// holds one <c>_patrol</c> field, and the guard ratchets from <i>"nobody outside the family"</i> to
/// <i>"nowhere at all"</i> — exactly as the seat's did between #902 and #904.</para>
///
/// <h3>The five facts, and why each is here</h3>
///
/// <list type="number">
/// <item><b>Not one of the twenty-two raw names survives anywhere in the client</b> — not in a <c>.cs</c>
/// partial, not in the <c>.razor</c> markup, and <b>not in a comment</b>: a comment that has to name a
/// field nobody has any more is a reader sent to a place that does not exist. RED by leaving one field
/// behind on the page.</item>
/// <item><b>The family's eight files are all really there.</b> A path that matches nothing exempts nothing,
/// and a text sweep over a directory that has moved proves precisely as much as an empty room.</item>
/// <item><b>All twenty-two are on the round, under the names this lane gave them</b> — asked of the TEXT of
/// <c>Patrol.cs</c> and of the running type by REFLECTION. Both, because #909 is the standing reminder that
/// a source sweep and a reflection ledger fail in different directions, and a rename that satisfies one can
/// leave the other reading a dead name.</item>
/// <item><b>The page holds exactly ONE patrol field, and it is <c>readonly</c>.</b> The state may not creep
/// back out onto the page one field at a time, and there may never be two answers to <i>who is walking this
/// floor</i>: leaving a floor EMPTIES the round, it does not swap in a second one.</item>
/// <item><b>And the fifteen questions the rest of the page asks are answered in both places</b> — on
/// <c>Patrol</c>, where they really live, and on <c>Map</c>, where a one-line forwarder keeps every 6′a
/// caller spelling them the way it always did. This is the fact 6′c relaxes, deliberately and by name,
/// rather than by deleting a row to make a sweep go quiet.</item>
/// </list>
///
/// <para>Proven RED on this branch by putting one field back on the page: facts 1, 3 and 4 all reddened, each
/// naming the field, the file and the line. The verbatim output is in #870 lane 6′b's PR body.</para>
/// </summary>
public sealed class ThePatrolKeepsItsOwnStateTests
{
    /// <summary>The eight files the patrol is, relative to <c>src/SpaceSails.Client</c>: lane 5b's six verb
    /// partials (the header note lives in the first of them) and lane 6′b's two — the state, and the
    /// <c>Guard</c> the state is made of.</summary>
    private static readonly string[] TheFamily =
    [
        "Pages/Map.Patrol.cs",
        "Pages/Map.Patrol.Challenge.cs",
        "Pages/Map.Patrol.Escort.cs",
        "Pages/Map.Patrol.Hide.cs",
        "Pages/Map.Patrol.Round.cs",
        "Pages/Map.Patrol.Run.cs",
        "Pages/Patrol/Patrol.cs",
        "Pages/Patrol/Guard.cs",
    ];

    /// <summary>Where the state itself is — the one file fact 3 reads, and the only file in the client that
    /// is allowed to declare any of it.</summary>
    private const string TheStateItself = "Pages/Patrol/Patrol.cs";

    /// <summary>Every field the round used to keep loose on the page, what each is, and what it is called on
    /// <c>Patrol</c> now. Measured off the tree, not off the spec: the LANE 6′ SPEC listed twenty and the
    /// family really carries twenty-two — <c>_escortCar</c> and <c>_walkedPastSaid</c> were both missing from
    /// that list.
    ///
    /// <para>The raw column is what fact 1 sweeps for and the new column is what fact 3 asks the round for,
    /// so the two halves cannot drift apart: a rename has to be written here once, in the same commit, and
    /// both facts follow it. <see cref="PatrolState"/> carries the same table for the reflection harnesses,
    /// and fact 3 asserts the two agree.</para></summary>
    private static readonly (string Raw, string On, string What)[] TheTwentyTwo =
    [
        ("_guards", "Guards", "the men on the floor"),
        ("_patrolReadables", "Readables", "what a held man could be reading off this floor's walls"),
        ("_patrolBeat", "Beat", "the stops, in the order this watch walks them"),
        ("_patrolFloorSeconds", "FloorSeconds", "how long the captain has been on this floor"),
        ("_patrolHeardAgo", "HeardAgo", "how long since the boots were mentioned"),
        ("_escort", "Escort", "the guard walking the captain back to the car"),
        ("_escortDue", "EscortDue", "the guard whose walk back has not started yet"),
        ("_escortCar", "EscortCar", "where the walk back ends"),
        ("_escortSeconds", "EscortSeconds", "how long the walk back has been going"),
        ("_escortSaidPumps", "EscortSaidPumps", "whether the small talk has landed"),
        ("_patrolWatch", "Watch", "which watch the two counters belong to"),
        ("_escortsThisWatch", "EscortsThisWatch", "how many times you have been walked back this watch"),
        ("_walkedAwayThisWatch", "WalkedAwayThisWatch", "how many hails you have walked away from this watch"),
        ("_kickOutDue", "KickOutDue", "whether this walk ends at the sky"),
        ("_kickOutRideDue", "KickOutRideDue", "the ride up, armed rather than taken"),
        ("_kickedOutPlateFor", "KickedOutPlateFor", "how long the KICKED OUT plate stays painted"),
        ("_paperInHand", "PaperInHand", "the paper that goes into his hand when he arrives"),
        ("_walletFanOpen", "WalletFanOpen", "is the fan up"),
        ("_shownBook", "ShownBook", "the captain's own paper trail"),
        ("_walkedPastSaid", "WalkedPastSaid", "whether the round has been heard going past this hide"),
        ("_patrolCheat", "RoundsCheat", "?patrol=N"),
        ("_badgeCheat", "BadgeCheat", "?badge=1"),
    ];

    /// <summary>What the rest of the page may ask instead — the round's own vocabulary, the names 6′a's
    /// seventeen sites were rewritten onto, and the list a failing message hands the next author. Every one
    /// of them is declared on <see cref="Pages.Map"/> too, as a one-line forwarder, until 6′c.</summary>
    private static readonly string[] AskTheseInstead =
    [
        "CaptainIsUnderEscort",
        "TheRoundOnFoot",
        "TheNextHideGetsItsOwnLine",
        "EverybodyForgetsTheCatch",
        "ThePaperInYourHandIs",
        "TheBookOn",
        "YourPaperTrail",
        "ForgetThePaperTrail",
        "RestoreAPaperTrailRow",
        "CloseTheWalletFan",
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

    // ── (1) THE RAW NAMES ARE GONE, EVERYWHERE ────────────────────────────────────────────────────────

    [Fact]
    public void NotOneRawPatrolFieldNameSurvivesAnywhereInTheClient()
    {
        var trespass = new List<string>();

        foreach (string path in ClientSources())
        {
            string relative = Relative(path);
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                foreach ((string raw, string on, string what) in TheTwentyTwo)
                {
                    if (Needle(raw).IsMatch(lines[i]))
                    {
                        trespass.Add($"  {relative}:{i + 1} names {raw} ({what}) — say _patrol.{on} " +
                                     $"instead — {lines[i].Trim()}");
                    }
                }
            }
        }

        Assert.True(
            trespass.Count == 0,
            "#870 lane 6′b · THE PATROL'S STATE IS THE PATROL'S, AND IT IS ONE OBJECT. These twenty-two are " +
            "properties on Map.Patrol (Pages/Patrol/Patrol.cs) and there is no field by any of these names " +
            "on the page any more:\n" +
            string.Join("\n", trespass) +
            "\n\nInside the family, reach the state through the page's one `_patrol`. From ANYWHERE else, ask " +
            "the round the question you actually mean — " + string.Join(", ", AskTheseInstead) +
            " — or tell it to do the thing. If none of them says what you need, ADD one small named member " +
            "to Patrol.cs and say in its docblock which site asked for it. Do not put a field back on the " +
            "page: that is the whole of what this lane undid.");
    }

    // ── (2) AND THE EIGHT FILES ARE REALLY THERE ──────────────────────────────────────────────────────

    [Fact]
    public void TheEightFamilyFilesAreAllReallyThere()
    {
        List<string> missing = TheFamily
            .Where(f => !File.Exists(Path.Combine(ClientRoot, f.Replace('/', Path.DirectorySeparatorChar))))
            .ToList();

        Assert.True(
            missing.Count == 0,
            "#870 lane 6′a/6′b · this guard reads eight files by path and these do not exist:\n  " +
            string.Join("\n  ", missing) +
            "\n\nA path that matches nothing proves nothing. If the family moved or was split again, " +
            "re-PATH these constants in the same commit — never delete a row to make a sweep quiet.");
    }

    // ── (3) THE TWENTY-TWO ARE ON THE ROUND, IN THE TEXT AND ON THE TYPE ───────────────────────────────

    [Fact]
    public void TheTwentyTwoAreAllOnThePatrolObject()
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags Either = Hidden | BindingFlags.Public;

        Type round = typeof(Pages.Map).GetNestedType("Patrol", Hidden)
            ?? throw new InvalidOperationException(
                "#870 lane 6′b · Map has no nested `Patrol` type at all. The round's state has gone " +
                "somewhere this guard cannot see, and every fact in this file is now vacuous.");

        string text = File.ReadAllText(
            Path.Combine(ClientRoot, TheStateItself.Replace('/', Path.DirectorySeparatorChar)));

        var gone = new List<string>();

        foreach ((string raw, string on, _) in TheTwentyTwo)
        {
            if (round.GetProperty(on, Either) is null)
            {
                gone.Add($"  {raw} → {on} is not a property on Map.Patrol at all");
            }

            if (!Needle(on).IsMatch(text))
            {
                gone.Add($"  {on} is named nowhere in {TheStateItself}");
            }
        }

        // …and the reflection harnesses' own copy of this table says the same thing. Two tables that can
        // drift apart is the law transcribed at its call sites, which is exactly what #909 was.
        foreach ((string raw, string on) in PatrolState.TheTwentyTwo)
        {
            if (!TheTwentyTwo.Any(r => r.Raw == raw && r.On == on))
            {
                gone.Add($"  PatrolState follows {raw} → {on}, and this guard does not know that pair");
            }
        }

        Assert.True(
            TheTwentyTwo.Length == PatrolState.TheTwentyTwo.Count && gone.Count == 0,
            "#870 lane 6′b · ANTI-VACUOUS HALF, asked of the text AND of the running type AND of the " +
            $"harnesses' own lookup ({TheTwentyTwo.Length} here, {PatrolState.TheTwentyTwo.Count} there):\n" +
            string.Join("\n", gone) +
            "\n\nA sweep for absence passes gloriously on a tree where the state was simply renamed or " +
            "deleted — the world can no longer tell pass from fail, which is this repo's fifth named bug " +
            "class. If a property really was renamed, write it in BOTH tables in the same commit as the " +
            "rename; PatrolState is what keeps every reflection guard in the repository off a dead name.");
    }

    // ── (4) AND THE PAGE HOLDS EXACTLY ONE OF THEM ────────────────────────────────────────────────────

    [Fact]
    public void ThePageHoldsExactlyOneRoundAndNeverSwapsIt()
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        Type round = typeof(Pages.Map).GetNestedType("Patrol", Hidden)!;
        Type guard = typeof(Pages.Map).GetNestedType("Guard", Hidden)!;

        List<FieldInfo> patrolShaped = [.. typeof(Pages.Map).GetFields(Hidden)
            .Where(f => !f.IsStatic)
            .Where(f => f.FieldType == round
                || f.FieldType == guard
                || (f.FieldType.IsGenericType && f.FieldType.GetGenericArguments().Contains(guard)))];

        Assert.True(
            patrolShaped.Count == 1
            && patrolShaped[0].Name == "_patrol"
            && patrolShaped[0].IsInitOnly,
            "#870 lane 6′b · THE PAGE HOLDS ONE ROUND, AND IT IS `private readonly Patrol _patrol`. What it " +
            "actually holds:\n  " +
            string.Join("\n  ", patrolShaped.Select(f =>
                $"{(f.IsInitOnly ? "readonly " : "")}{f.FieldType.Name} {f.Name}")) +
            "\n\nTwo reasons, and both have cost this repository an afternoon. A second field of the round's " +
            "own shape is the state creeping back onto the page one member at a time, which is what 6′b " +
            "undid. And a `_patrol` that could be RE-ASSIGNED would be a second answer to \"who is walking " +
            "this floor\" — the first named bug class, aimed at a rota. Leaving a floor EMPTIES the round " +
            "(SpawnPatrolFor); it does not swap in a different one.");
    }

    // ── (5) AND THE FIFTEEN ARE ANSWERED IN BOTH PLACES, UNTIL 6′c ────────────────────────────────────

    [Fact]
    public void TheFifteenQuestionsAreOnTheRoundAndForwardedByThePage()
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        Type round = typeof(Pages.Map).GetNestedType("Patrol", Hidden)!;
        var missing = new List<string>();

        foreach (string member in AskTheseInstead)
        {
            if (!Declares(round, member))
            {
                missing.Add($"  Map.Patrol does not answer `{member}` — this is where it really lives");
            }

            if (!Declares(typeof(Pages.Map), member))
            {
                missing.Add($"  Map does not forward `{member}` — a 6′a caller outside the family reads it");
            }
        }

        Assert.True(
            missing.Count == 0,
            "#870 lane 6′b · THE FIFTEEN, ANSWERED ON THE ROUND AND FORWARDED BY THE PAGE:\n" +
            string.Join("\n", missing) +
            "\n\nEvery one of these is called by name from a file OUTSIDE the patrol family — Map.razor, " +
            "Map.Vault.cs, Map.Bench.cs, Map.Bin.cs, Map.SweepTeam.cs, Map.Cubicle.cs, Map.Sim.World.cs, " +
            "Map.Sim.Cancel.cs, Map.Surface.Cheats.cs — and 6′b deliberately did not rewrite those callers, " +
            "so the page keeps a one-line forwarder for each. THE LANE THAT RELAXES THIS IS 6′c: it points " +
            "those callers at an explicit host surface and deletes the forwarder block whole. Until then, a " +
            "member that has lost its forwarder has lost a caller, and this fact is how you find out which.");
    }

    private static bool Declares(Type t, string member) =>
        t.GetProperty(member, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) is not null
        || t.GetMethod(member, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) is not null;

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
