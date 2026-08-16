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
/// caller spelling them the way it always did.</item>
/// </list>
///
/// <h3>#870 lane 6′c · …AND NOW THE VERBS, AND FOUR MORE FACTS</h3>
///
/// <para>6′c moved every verb the family had — putting men on a floor, walking them, hailing, reading a
/// wallet, running, walking a captain out, throwing one back at the sky — onto the same object. <b>The whole
/// claim of that lane is that what they still need from the page is <c>IPatrolHost</c> and nothing else</b>,
/// and these four facts are that claim asked of the source rather than of a reviewer:</para>
///
/// <list type="number">
/// <item><b>HOW MANY THINGS A ROUND NEEDS FROM THE PAGE, and it may only go down.</b> A ratchet, like the
/// seat's and like #870's own size gate.</item>
/// <item><b>THE ROUND REACHES THE PAGE THROUGH ONE DOOR</b> — four sweeps over the round's own source: it
/// may not NAME a page field, every page verb it uses must be spelled <c>_host.</c> something, it may not
/// name the type <c>Map</c> at all, and the only page CONSTANTS it may read bare are the three written
/// down (a <c>const</c> is a compile-time literal, not a collaborator, and that is the one hole a nested
/// class leaves in the first three).</item>
/// <item><b>AND THE DOOR IS REALLY THERE AND REALLY USED</b> — because a rule about ABSENCE passes
/// gloriously on a tree where the thing was deleted.</item>
/// <item><b>AND THE THIRTEEN VERBS THE OUTSIDE STILL CALLS ARE FORWARDED</b>, measured one caller at a time
/// — the other twenty-nine kept no forwarder at all, which is the proof that nothing outside the family
/// gained a reach it did not have.</item>
/// </list>
///
/// <para>Proven RED on 6′b's branch by putting one field back on the page: facts 1, 3 and 4 all reddened,
/// each naming the field, the file and the line. Proven RED on 6′c's by calling one page member directly out
/// of a moved verb, by adding a twenty-second row to the host, and by taking one partial out of the sweep's
/// reach. Every verbatim output is in the two PR bodies.</para>
/// </summary>
public sealed class ThePatrolKeepsItsOwnStateTests
{
    /// <summary>The fourteen files the patrol is, relative to <c>src/SpaceSails.Client</c>.
    ///
    /// <para>#870 lane 6′c · RE-PATHED. The page's half is four forwarder partials and the host wiring;
    /// <c>Map.Patrol.Round.cs</c> and <c>Map.Patrol.Escort.cs</c> are GONE, because nothing outside the
    /// family ever asked the round to walk a leg or to walk a captain out. The round's own half is its
    /// state, the <c>Guard</c> the state is made of, the door, and six partials of verbs.</para></summary>
    private static readonly string[] TheFamily =
    [
        "Pages/Map.Patrol.cs",
        "Pages/Map.Patrol.Challenge.cs",
        "Pages/Map.Patrol.Hide.cs",
        "Pages/Map.Patrol.Run.cs",
        "Pages/Map.PatrolHost.cs",
        "Pages/Patrol/Patrol.cs",
        "Pages/Patrol/Guard.cs",
        "Pages/Patrol/IPatrolHost.cs",
        "Pages/Patrol/Patrol.Floor.cs",
        "Pages/Patrol/Patrol.Hide.cs",
        "Pages/Patrol/Patrol.Round.cs",
        "Pages/Patrol/Patrol.Challenge.cs",
        "Pages/Patrol/Patrol.Escort.cs",
        "Pages/Patrol/Patrol.Run.cs",
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
    public void TheFourteenFamilyFilesAreAllReallyThere()
    {
        List<string> missing = TheFamily
            .Where(f => !File.Exists(Path.Combine(ClientRoot, f.Replace('/', Path.DirectorySeparatorChar))))
            .ToList();

        Assert.True(
            missing.Count == 0,
            "#870 lane 6′a/6′b/6′c · this guard reads fourteen files by path and these do not exist:\n  " +
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
            "so the page keeps a one-line forwarder for each.\n\n#870 lane 6′c KEPT THAT BLOCK, and said so " +
            "rather than quietly deleting rows: IPatrolHost is the door the ROUND reaches the page through, " +
            "not a door the page reaches the round through, so pointing seventeen call sites at it would " +
            "have been a different lane in nine files this one has no business in. A member that has lost " +
            "its forwarder has lost a caller, and this fact is how you find out which.");
    }

    private static bool Declares(Type t, string member) =>
        t.GetProperty(member, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) is not null
        || t.GetMethod(member, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) is not null;

    // ── (6–9) #870 lane 6′c · AND THE ROUND REACHES THE PAGE THROUGH ONE DOOR ─────────────────────────
    //
    // 6′c moved the VERBS onto the same object — spawning a round, walking it, hailing, reading a wallet,
    // running, walking a captain out, throwing one back at the sky — and the whole claim of the lane is that
    // what they still need from the page is IPatrolHost and NOTHING else. These four facts are that claim,
    // asked of the source rather than of a reviewer.

    /// <summary>The one file that writes the coupling down.</summary>
    private const string TheHostFile = "Pages/Patrol/IPatrolHost.cs";

    /// <summary>Where the state itself is declared, which is also where the one door is.</summary>
    private const string TheDoorIsDeclaredIn = "Pages/Patrol/Patrol.cs";

    /// <summary>The round's own source: everything under <c>Pages/Patrol/</c> except the interface itself.
    /// The interface and the page's implementation of it (<c>Pages/Map.PatrolHost.cs</c>) are deliberately
    /// NOT in here — they are the door, and a door is allowed to name both rooms. <c>Guard.cs</c> IS in
    /// here: a guard is what the round is made of, and a mutable body that could reach the page would be the
    /// same hole one room along.</summary>
    private static IEnumerable<string> PatrolSources() =>
        ClientSources().Where(p =>
            Relative(p).StartsWith("Pages/Patrol/", StringComparison.Ordinal)
            && Relative(p) != TheHostFile
            && p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));

    /// <summary>Two of the host's members share a spelling with a TYPE this family names constantly —
    /// <c>DeckPlan.AvatarRadius</c>, <c>Core.Satchel.Add</c> — so a bare occurrence of either is ordinarily a
    /// type reference and not a reach at the page. They are left out of the <i>say <c>_host.</c> it</i> half
    /// and out of that half only: the FIELD half still holds the line, because <c>_deckPlan</c> and
    /// <c>_satchel</c> are page fields and may not be named at all. It is the seat lane's own exemption, and
    /// it is exactly two members here as it was there.</summary>
    private static readonly HashSet<string> CollidesWithATypeName =
        new(StringComparer.Ordinal) { "DeckPlan", "Satchel" };

    /// <summary>#870 lane 6′c · THE THREE CONSTANTS OF THE PAGE THE ROUND MAY STILL READ BARE, and the list
    /// is exhaustive and enforced.
    ///
    /// <para>A nested class can see its outer class's STATIC members without any receiver at all, which is
    /// the one hole the three sweeps below cannot close by construction. These three are compile-time
    /// literals the compiler inlines — the frame clamp every surface stepper obeys, the captain's own
    /// sub-stepper's bound (which #833's one stepper is a copy of), and how many figures a round may need
    /// drawing (which the surface's slot arithmetic also reads, which is why it did not travel). There is no
    /// page object involved at runtime and no state anybody can reach through them, so they are not
    /// collaborators and they are not on the interface. Anything else would be, and this row is what makes
    /// that a fact rather than an intention.</para></summary>
    private static readonly HashSet<string> ThePageConstantsTheRoundMayRead =
        new(StringComparer.Ordinal) { "MaxSurfaceStepSeconds", "AutoWalkSubStepsPerFrame", "PatrolBand" };

    /// <summary>
    /// HOW MANY THINGS A ROUND NEEDS FROM THE PAGE — <b>and it may only ever go down</b>.
    ///
    /// <para>A ratchet, exactly like #870's own size gate and the seat's before it. The round used to be six
    /// partials of <see cref="Pages.Map"/>, which meant it could reach anything the page had: every field,
    /// every private verb, every dev cheat, with nothing written down and nothing to argue with. Six issues
    /// in a fortnight landed in it. The number below is what that came to when somebody finally counted.</para>
    ///
    /// <para>Taking a member off is a good day — lower the number in the same commit and say in the PR body
    /// which one went. RAISING it is a lane of its own, because it is the round asking the page for something
    /// new, and that is a design decision rather than a build error.</para>
    /// </summary>
    [Fact]
    public void TheRoundNeedsExactlyThisManyThingsFromThePage()
    {
        const int TheRatchet = 21;

        List<string> members = HostMembers();

        Assert.True(
            members.Count == TheRatchet,
            $"#870 lane 6′c · THE ROUND NEEDS {members.Count} THINGS FROM THE PAGE, and the ratchet says " +
            $"{TheRatchet}.\n\n" +
            (members.Count < TheRatchet
                ? "FEWER is a good day — the round stopped needing something. Lower the number here, in the " +
                  "same commit, and say in the PR body which member went and why."
                : "MORE means the round asked the page for something new. That does not go in with a passing " +
                  "build; it goes in with a PR body that argues for it. If you are reading this in the middle " +
                  "of a refactor, the answer is almost always that the verb you just moved should have asked " +
                  "for an ANSWER rather than for the machinery under it — which is why " +
                  "TheCubicleTheCaptainIsShutIn and NameOnYourOwnPapers are one member each instead of the " +
                  "stall sweep and the two thread reads they are made of.") +
            "\n\nWhat is on it today:\n  " + string.Join("\n  ", members));
    }

    /// <summary>
    /// AND NOTHING IN THE ROUND REACHES THE PAGE ANY OTHER WAY.
    ///
    /// <para>Four sweeps over the round's own source, and between them they are the whole of the claim. No
    /// file of the round may NAME a field of the page — not <c>_surface</c>, not <c>_avatarX</c>, not a dev
    /// cheat — every one of the page's verbs it does use must be spelled <c>_host.</c> something, it may not
    /// name the type <c>Map</c> at all, and the only page CONSTANTS it may read bare are the three written
    /// down above.</para>
    ///
    /// <para><b>It reads CODE, not prose.</b> Doc comments and whole-line comments are skipped on purpose:
    /// the moved docblocks travelled byte-identical, which is this lane's own discipline, and several of them
    /// name a page member in a sentence ABOUT the coupling. The coupling is the interface; what this guard is
    /// about is what the code reaches for.</para>
    ///
    /// <para><b>Proven RED</b> by calling one page member directly out of a moved verb — verbatim in #870
    /// lane 6′c's PR body.</para>
    /// </summary>
    [Fact]
    public void TheRoundReachesThePageThroughTheHostAndNoOtherWay()
    {
        var trespass = new List<string>();
        List<string> fields = PageFields();
        List<string> consts = PageConsts();
        List<string> host = HostMembers()
            .Select(m => m.Split(' ')[0])
            .Where(n => !CollidesWithATypeName.Contains(n))
            .ToList();

        foreach (string path in PatrolSources())
        {
            string[] lines = File.ReadAllLines(path);

            // Where the round itself begins. Above it is the file's own `public sealed partial class Map { … }`
            // scaffolding — that is the PAGE talking about the round, which is allowed.
            int body = Array.FindIndex(lines, l =>
                l.Contains("class Patrol", StringComparison.Ordinal)
                || l.Contains("class Guard", StringComparison.Ordinal));

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    continue;   // prose about the coupling is not the coupling
                }

                // AND THE PAGE'S OWN TYPE IS NOT NAMED IN HERE AT ALL. This is the clause that shuts the
                // door rather than merely papering it: every way round the other sweeps — a second field
                // typed as the page, a cast of the host back to it, a parameter, a local — has to write the
                // word `Map` somewhere, and after that the round can reach anything again through a receiver
                // no sweep has heard of. The seat lane found this by TRYING it: a planted `_page.Show…(…)`
                // walked straight past the host sweep, because that name IS qualified — just not by a door.
                if (body >= 0 && i > body && Regex.IsMatch(line, @"(?<![\w.])Map\b"))
                {
                    trespass.Add(
                        $"  {Relative(path)}:{i + 1} names the PAGE'S OWN TYPE inside the round — {line.Trim()}");
                }

                foreach (string field in fields)
                {
                    if (Needle(field).IsMatch(line))
                    {
                        trespass.Add(
                            $"  {Relative(path)}:{i + 1} names the page's own {field} — {line.Trim()}");
                    }
                }

                // …AND THE STATIC HOLE, which is the one a nested class leaves open: `Map`'s own constants
                // are in scope in here with no receiver at all, so the three that are allowed are named and
                // everything else is a reach.
                foreach (string name in consts)
                {
                    if (ThePageConstantsTheRoundMayRead.Contains(name))
                    {
                        continue;
                    }
                    foreach (Match m in Regex.Matches(line, @"(?<!\w)" + Regex.Escape(name) + @"\b"))
                    {
                        if (line[..m.Index].EndsWith(".", StringComparison.Ordinal))
                        {
                            continue;   // a member of something else entirely, not the page's own
                        }
                        trespass.Add(
                            $"  {Relative(path)}:{i + 1} reads the page's own const {name} — {line.Trim()}");
                    }
                }

                foreach (string name in host)
                {
                    foreach (Match m in Regex.Matches(line, @"(?<!\w)" + Regex.Escape(name) + @"\b"))
                    {
                        string before = line[..m.Index];
                        if (before.EndsWith("_host.", StringComparison.Ordinal)
                            || before.EndsWith(".", StringComparison.Ordinal))
                        {
                            continue;   // through the one door, or a member of something else entirely
                        }
                        trespass.Add(
                            $"  {Relative(path)}:{i + 1} says {name} bare — say _host.{name} — {line.Trim()}");
                    }
                }
            }
        }

        Assert.True(
            trespass.Count == 0,
            "#870 lane 6′c · THE ROUND REACHES THE PAGE THROUGH ONE DOOR, and these lines go round it:\n" +
            string.Join("\n", trespass) +
            $"\n\nThe door is `_host`, and what is behind it is written down in {TheHostFile}. If the thing " +
            "you need is not on it, do not reach past it: ask the page for the ANSWER rather than for the " +
            "machinery — that is why TheCubicleTheCaptainIsShutIn and NameOnYourOwnPapers are one member " +
            "each instead of a stall sweep and two reads of the captain-thread registry. If it really does " +
            "belong on the interface, add it AND raise the ratchet in " +
            "TheRoundNeedsExactlyThisManyThingsFromThePage, in a PR body that argues for it.");
    }

    /// <summary>The anti-vacuous half of the sweep above, and it is the same shape as this file's other one:
    /// a rule about ABSENCE passes gloriously on a tree where the thing was simply deleted. So the interface
    /// has to be really there at the path this guard names, the verbs have to really be in the files the
    /// sweep reads, the door has to really be a field of the round, and the door has to be really USED.</summary>
    [Fact]
    public void TheDoorIsReallyThereAndReallyUsed()
    {
        Assert.True(
            File.Exists(Path.Combine(ClientRoot, TheHostFile.Replace('/', Path.DirectorySeparatorChar))),
            $"#870 lane 6′c · this guard names one file by path and it does not exist: {TheHostFile}. A path " +
            "that matches nothing exempts nothing and proves nothing. Re-PATH it; never delete the row.");

        List<string> round = PatrolSources().Select(Relative).ToList();
        Assert.True(
            round.Count >= 8,
            $"#870 lane 6′c · the round is supposed to be its state, the Guard it is made of and six partials " +
            $"of verbs, and the sweep can only see {round.Count} file(s). It is reading almost nothing, which " +
            "means it is proving almost nothing.\n  " + string.Join("\n  ", round));

        Assert.Contains(
            "private readonly IPatrolHost _host;",
            File.ReadAllText(
                Path.Combine(ClientRoot, TheDoorIsDeclaredIn.Replace('/', Path.DirectorySeparatorChar))),
            StringComparison.Ordinal);

        int through = PatrolSources().Sum(p => Regex.Matches(File.ReadAllText(p), @"\b_host\.").Count);
        Assert.True(
            through >= 100,
            $"#870 lane 6′c · the round goes through its host {through} times, and it landed at well over " +
            "that. Either a whole verb group has left the object, or somebody found another way to the page " +
            "— and the sweep above cannot tell those two apart, which is why this row exists to notice.");
    }

    /// <summary>#870 lane 6′c · THE THIRTEEN VERBS THE OUTSIDE STILL CALLS BY NAME, forwarded by the page and
    /// answered by the round.
    ///
    /// <para>Every verb the family had was counted before it was moved — one caller at a time, over every
    /// <c>.cs</c> and <c>.razor</c> outside the family — and only these thirteen had one. The other
    /// twenty-nine kept no forwarder at all, and two whole page partials went with them
    /// (<c>Map.Patrol.Round.cs</c>, <c>Map.Patrol.Escort.cs</c>): nothing outside the round has ever asked it
    /// to walk a leg or to walk a captain out. <b>That asymmetry is the proof</b> that the move narrowed the
    /// family's surface instead of merely relocating it.</para></summary>
    [Fact]
    public void TheThirteenVerbsTheOutsideStillCallsAreForwardedByThePage()
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        Type round = typeof(Pages.Map).GetNestedType("Patrol", Hidden)!;
        var missing = new List<string>();

        foreach (string verb in TheThirteenForwardedVerbs)
        {
            if (!Declares(round, verb))
            {
                missing.Add($"  Map.Patrol does not do `{verb}` — 6′c moved every verb there");
            }

            if (!Declares(typeof(Pages.Map), verb))
            {
                missing.Add($"  Map does not forward `{verb}` — a caller outside the family asks for it");
            }
        }

        Assert.True(
            missing.Count == 0,
            "#870 lane 6′c · THE THIRTEEN, DONE BY THE ROUND AND FORWARDED BY THE PAGE:\n" +
            string.Join("\n", missing) +
            "\n\nEach one has a real caller outside the patrol family — Map.Surface.Hive.cs, " +
            "Map.Surface.Frame.cs, Map.Surface.Hud.cs, Map.Bin.cs, Map.Cubicle.cs, Map.Combat.Remote.cs, " +
            "Map.Sim.Cancel.cs and Map.razor — and the forwarder is what lets those callers keep the " +
            "spelling they always had. A verb with NO outside caller kept no forwarder, deliberately: if " +
            "you are adding a row here, first check that the caller is real, because the number in this " +
            "list is a measurement rather than a habit.");
    }

    /// <summary>The thirteen, in the order the page's four remaining partials declare them.</summary>
    private static readonly string[] TheThirteenForwardedVerbs =
    [
        "TheRoundHasEyesOnYou",
        "SpawnPatrolFor",
        "AdvancePatrol",
        "RememberWhoWatchedTheCatchGoOver",
        "TheHail",
        "TheWalletFan",
        "WalletFanIsUp",
        "ChooseThePaper",
        "IssueTheSitePass",
        "SomebodySawThat",
        "TheKickedOutPlate",
        "FillPatrolDroids",
        "EverybodyForgetsTheCatch",
    ];

    /// <summary>Every member declared on the host interface, as <c>Name (kind)</c>. Read off the SOURCE
    /// rather than off the running type, so the number in the ratchet is a count of what somebody wrote down
    /// and had to look at.</summary>
    private static List<string> HostMembers()
    {
        var found = new List<string>();
        string path = Path.Combine(ClientRoot, TheHostFile.Replace('/', Path.DirectorySeparatorChar));
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0
                || line.StartsWith("//", StringComparison.Ordinal)
                || line.StartsWith("using", StringComparison.Ordinal)
                || line.StartsWith("namespace", StringComparison.Ordinal)
                || line.StartsWith("public partial class", StringComparison.Ordinal)
                || line.StartsWith("private interface", StringComparison.Ordinal)
                || line is "{" or "}")
            {
                continue;
            }

            Match verb = Regex.Match(line, @"(\w+)\s*\([^()]*\);$");
            if (verb.Success)
            {
                found.Add(verb.Groups[1].Value + " (verb)");
                continue;
            }

            Match read = Regex.Match(line, @"(\w+)\s*\{\s*(?:get|set)[^}]*\}$");
            if (read.Success)
            {
                found.Add(read.Groups[1].Value + " (read)");
            }
        }
        return found;
    }

    /// <summary>Every field the PAGE keeps, by name, read off every client partial that is not the round's
    /// own. This is the set the round may not name: a patrol that can reach a page field is a patrol with no
    /// surface at all, which is the state 6′a, 6′b and 6′c were written to leave behind.</summary>
    private static List<string> PageFields() => PageDeclarations(@"\b(_\w+)\b", constants: false);

    /// <summary>…and every <c>const</c> THE PAGE ITSELF keeps, for the one hole a nested class leaves
    /// open: only a partial of <c>Map</c> counts, because only <c>Map</c>'s own statics are in scope
    /// inside a type nested in it. <c>DeckPlan.AvatarRadius</c> is a constant of the renderer's deck and
    /// is reached the way every other type in this assembly is — by naming it.</summary>
    private static List<string> PageConsts() => PageDeclarations(@"\bconst\s+[\w<>?,.\[\]]+\s+(\w+)", constants: true);

    private static List<string> PageDeclarations(string shape, bool constants)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in ClientSources())
        {
            string rel = Relative(path);
            if (rel.StartsWith("Pages/Patrol/", StringComparison.Ordinal)
                || !rel.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (constants
                && !File.ReadAllText(path).Contains("partial class Map", StringComparison.Ordinal))
            {
                continue;   // only the page's own statics are in scope inside a type nested in it
            }

            foreach (string line in File.ReadAllLines(path))
            {
                // A DECLARATION and never a use: at the class's own indent, ending its statement on its own
                // line, and not an expression-bodied anything. One line can declare several.
                if (!Regex.IsMatch(line, @"^ {4}(?:private|internal|protected|public)\b")
                    || !line.Contains(';', StringComparison.Ordinal)
                    || line.Contains("=>", StringComparison.Ordinal)
                    || (constants != line.Contains(" const ", StringComparison.Ordinal)))
                {
                    continue;
                }
                foreach (Match m in Regex.Matches(line, shape))
                {
                    names.Add(m.Groups[1].Value);
                }
            }
        }
        return names.OrderBy(n => n, StringComparer.Ordinal).ToList();
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
