using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 6′c · <b>THE ROUND REACHES THE PAGE THROUGH ONE DOOR</b> — sections (6) to (9) of
/// <see cref="ThePatrolKeepsItsOwnStateTests"/>.
///
/// <para>What this part owns is the seam: the round needs exactly this many things from the page, it reaches
/// the page through the host and no other way, the door is really there and really used, and the thirteen
/// verbs the outside still calls are forwarded by the page. The twenty-two fields and the laws about where
/// they live are in the file that carries the class docblock.</para>
/// </summary>
public sealed partial class ThePatrolKeepsItsOwnStateTests
{
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
        const BindingFlags Hidden = TestTree.PrivateOnAnInstance;

        Type round = typeof(Pages.Map).GetNestedType("Patrol", Hidden | BindingFlags.Public)!;
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
}
