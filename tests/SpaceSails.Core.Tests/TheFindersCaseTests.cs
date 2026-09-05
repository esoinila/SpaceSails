using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #417 slice 1 · <b>THE FINDER'S CASE — the graph, and the promise that it invents nothing.</b>
///
/// <para>Every guard below is asked of a world this file BUILDS BY HAND: a short list of berths with real
/// tiers, a short list of hulls whose service records are the seeded ones the game actually deals, and a
/// couple of grounds. That is the whole discipline — a guard handed a world it invented itself cannot tell
/// pass from fail (this house's fifth named bug class), so the hulls' histories come out of
/// <see cref="ShipHistories.For"/> and the tiers out of <see cref="ArrivalTube.Tier"/> rather than being
/// typed here as convenient shapes.</para>
///
/// <para>Every guard was watched go RED against a revert of the behaviour it names, and the revert and the
/// failure it produced are quoted on it — the shape this ground has kept since #587's lesson: a guard that
/// has never failed is a guard nobody has checked.</para>
/// </summary>
public sealed class TheFindersCaseTests
{
    private const string ThreadId = "417ac0de5f1a4b6c8d2e9f70a1b3c5d7";

    /// <summary>§8's reserved word and the words a finder's case may never reach for. The same list this
    /// arc's other paper suites keep, because a word forbidden on one of these documents is forbidden on all
    /// of them — and the two at the end are the canon pass's own law: <b>the case never names the Authority
    /// or the watchers</b>.</summary>
    private static readonly string[] Forbidden =
    [
        "monolith", "ancient", "alien", "reever", "old one", "pre-human", "artefact", "artifact",
        "authority", "watcher", "watchers",
    ];

    // ══ THE CANON, RETYPED FROM THE ISSUE ════════════════════════════════════════════════════════════════

    /// <summary>Fable's canon pass of 2026-09-05, retyped here from issue #417 so the guards have a source
    /// the implementation cannot move. A test that asserted <c>FinderCase.Reveal == FinderCase.Reveal</c>
    /// would pass on any sentence anybody ever wrote into it.</summary>
    private static readonly string[] Authored =
    [
        "Varga. I used to wear a badge at Selene Gate; now I find things for people who can't ask the badge. "
        + "You have a ship and no reputation to lose. Sit.",

        "A man walked off a hull at {0} and never walked into the concourse. The hull has had three names. "
        + "Find the fourth.",

        "Paid. Don't thank me — the next one is worse, and you'll take it.",

        "A finder's case",

        "Three names on one hull, and a man who is none of them.",

        "Her papers are older than the story. Not this one.",

        "The fourth name is on the transponder in the next berth. He is aboard. He knows you are here.",

        "Turn him in",

        "Take the bribe",

        "Selene Gate sends a boat. Varga does not come to see it.",

        "The account clears before he does. Varga will hear; she always does.",
    ];

    // ══ THE WORLD THE GUARDS RUN IN ══════════════════════════════════════════════════════════════════════

    /// <summary>Three real berths with three real tiers — a great port, a working berth and an outpost — so
    /// the roster clause below has something to refuse as well as something to choose.</summary>
    private static IReadOnlyList<OldCrew.Berth> Berths =>
    [
        new("selene-gate", ArrivalTube.Tier.GreatPort),
        new("cinder-roost", ArrivalTube.Tier.GreatPort),
        new("the-tilt", ArrivalTube.Tier.WorkingBerth),
        new("the-deep", ArrivalTube.Tier.Outpost),
    ];

    private static IReadOnlyDictionary<string, string> Names => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["selene-gate"] = "Selene Gate",
        ["cinder-roost"] = "Cinder Roost",
        ["the-tilt"] = "The Tilt",
        ["the-deep"] = "The Deep",
        ["miranda"] = "Miranda",
        ["callisto"] = "Callisto",
    };

    private static IReadOnlyList<FinderCase.Site> Sites =>
    [
        new("miranda", "Miranda"),
        new("callisto", "Callisto"),
    ];

    /// <summary>
    /// A traffic wave whose hulls carry the service records the GAME deals them, not records typed here.
    ///
    /// <para>Wide enough that two of them have carried one name: the pool is twenty-four names and each hull
    /// draws nought to three of them, so a couple of dozen hulls is comfortably past the birthday line. The
    /// count is asserted by <see cref="TheWorldTheseGuardsRunInReallyHasACaseInIt"/> rather than assumed,
    /// which is the difference between a bench and a wish.</para>
    /// </summary>
    private static IReadOnlyList<FinderCase.Hull> Traffic(int count = 24)
    {
        var hulls = new List<FinderCase.Hull>(count);
        for (int i = 0; i < count; i++)
        {
            string id = $"finder-hull-{i}";
            hulls.Add(new FinderCase.Hull(id, $"LARK {i}", ShipHistories.For(id)));
        }

        return hulls;
    }

    private static FinderCase.Case TheCase(string port = "selene-gate", string thread = ThreadId) =>
        FinderCase.Build(thread, port, Berths, Names, Traffic(), Sites)
        ?? throw new InvalidOperationException("this world dealt no case — the bench is broken, not the code.");

    // ══ THE BENCH IS A WORLD AND NOT A WISH ══════════════════════════════════════════════════════════════

    /// <summary>
    /// THE HULLS THIS SUITE RUNS ON REALLY DO SHARE A NAME, and the sharing is the game's own seeding rather
    /// than something this file arranged. Without this clause every guard under it could be passing on a
    /// world with no case in it at all, by way of the <c>throw</c> in the bench.
    /// </summary>
    [Fact]
    public void TheWorldTheseGuardsRunInReallyHasACaseInIt()
    {
        IReadOnlyList<FinderCase.Hull> hulls = Traffic();
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        var shared = new List<string>();

        foreach (FinderCase.Hull hull in hulls)
        {
            foreach (string name in hull.History.BareFormerNames)
            {
                if (seen.TryGetValue(name, out string? first) && first != hull.Id)
                {
                    shared.Add(name);
                }
                else
                {
                    seen[name] = hull.Id;
                }
            }
        }

        Assert.True(shared.Count > 0,
            $"{hulls.Count} seeded hulls and no two of them ever answered to one name — this bench has no "
            + "case in it, so nothing below it is proving anything.");
    }

    // ══ THE GRAPH ════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE SAME THREAD AND THE SAME WORLD DEAL THE SAME CASE</b> — repo agreement §9, and the reason a
    /// captain who reloads is not handed a different hull to look for.
    ///
    /// <para>And a DIFFERENT thread deals a different one, which is the half that catches a "deterministic"
    /// builder that ignored its seed: a graph that came back identical for every universe would pass the
    /// first clause perfectly.</para>
    ///
    /// <para><b>Watched RED:</b> the thread dropped out of <c>TheTwoHulls</c>' seed
    /// (<c>$"finder|hulls|{clientPortId}"</c>) — <i>"two universes were handed the same case"</i>.</para>
    /// </summary>
    [Fact]
    public void TheSameWorldDealsTheSameCaseAndAnotherUniverseDealsAnother()
    {
        Assert.Equal(TheCase(), TheCase());

        FinderCase.Case other = TheCase(thread: "0f1e2d3c4b5a69788796a5b4c3d2e1f0");
        Assert.True(
            other.HullId != TheCase().HullId
            || other.WitnessId != TheCase().WitnessId
            || other.BerthPortId != TheCase().BerthPortId
            || other.PayCredits != TheCase().PayCredits,
            "two universes were handed the same case — the builder is ignoring its seed.");
    }

    /// <summary>
    /// <b>EVERY NODE OF THE CASE IS SOMETHING THE WORLD DEALT.</b> The law the whole file is written under:
    /// a client port, a witness port, a ground, two hulls and a confrontation berth, each of them checked
    /// back against the list it had to come out of.
    ///
    /// <para><b>Watched RED:</b> <c>TheWitness</c> made to hand back <c>"the-lost-station"</c> when no berth
    /// is favoured — <i>"the witness drinks at the-lost-station, which is not a berth this world has"</i>.
    /// And again with <c>TheConfrontationPort</c> falling back to <c>berths[0].Id</c> ignoring the roster
    /// clause, which reddens the neighbouring-slot guard below instead.</para>
    /// </summary>
    [Fact]
    public void TheCaseInventsNothing()
    {
        FinderCase.Case c = TheCase();
        string[] ports = [.. Berths.Select(b => b.Id)];
        string[] hulls = [.. Traffic().Select(h => h.Id)];

        Assert.Contains(c.ClientPortId, ports);
        Assert.Contains(c.WitnessPortId, ports);
        Assert.Contains(c.BerthPortId, ports);
        Assert.Contains(c.PaperSiteBodyId, Sites.Select(s => s.BodyId));
        Assert.Contains(c.HullId, hulls);
        Assert.Contains(c.HerringHullId, hulls);
        Assert.Contains(c.WitnessId, PatronRota.Roster);
        Assert.NotEqual(c.HullId, c.HerringHullId);

        // …and the names it prints are the world's own spelling, never an id.
        Assert.Equal(Names[c.ClientPortId], c.ClientPortName);
        Assert.Equal(Names[c.PaperSiteBodyId], c.PaperSiteName);
    }

    /// <summary>
    /// <b>A WORLD THAT CANNOT FURNISH A CASE IS HANDED NONE.</b> Four refusals, one per missing part, and
    /// every one of them is an honest absence rather than an invented node.
    ///
    /// <para><b>Watched RED:</b> a fallback added to <c>TheTwoHulls</c> that paired the first hull with
    /// itself when nothing shared a name — <i>"Assert.Null() Failure · a world of maiden hulls dealt a case
    /// anyway"</i>.</para>
    /// </summary>
    [Fact]
    public void AWorldWithNothingToFindDealsNoCase()
    {
        // Maiden hulls only: nobody has ever carried anybody else's name.
        FinderCase.Hull[] maidens =
            [.. Traffic(60).Where(h => !h.History.HasFormerNames).Take(6)];
        Assert.True(maidens.Length >= 2, "the bench found fewer than two maiden hulls to make this world of.");
        Assert.Null(FinderCase.Build(ThreadId, "selene-gate", Berths, Names, maidens, Sites));

        // No ground for a paper to be on.
        Assert.Null(FinderCase.Build(ThreadId, "selene-gate", Berths, Names, Traffic(), []));

        // A world of outposts: one collar apiece, so there is no NEXT berth for the reveal to point at.
        IReadOnlyList<OldCrew.Berth> outposts = [new("the-deep", ArrivalTube.Tier.Outpost)];
        Assert.Null(FinderCase.Build(ThreadId, "the-deep", outposts, Names, Traffic(), Sites));

        // A port the world has no name for. The hook's brace has nothing honest to put in it.
        Assert.Null(FinderCase.Build(ThreadId, "nowhere", Berths, Names, Traffic(), Sites));
    }

    // ══ THE RED HERRING ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>BOTH HULLS HAVE ANSWERED TO THE NAME, AND THE RECORD CLEARS THE ONE WITH THE OLDER PAPERS.</b>
    /// This is the whole of the red herring, asserted off the hulls' own ledgers of names and their own yard
    /// records — never off anything this case wrote down about them.
    ///
    /// <para><b>Watched RED:</b> <c>TheTwoHulls</c> made to keep the pair the way round it found them
    /// (dropping the <c>TheRecordClears</c> swap) — <i>"the hull the case is about was cleared by her own
    /// record: laid down 2274, against the herring's 2301"</i>.</para>
    /// </summary>
    [Fact]
    public void TheHerringCarriesTheNameAndHerOwnRecordClearsHer()
    {
        FinderCase.Case c = TheCase();
        FinderCase.Hull suspect = Traffic().First(h => h.Id == c.HullId);
        FinderCase.Hull herring = Traffic().First(h => h.Id == c.HerringHullId);

        Assert.Contains(c.FormerName, suspect.History.BareFormerNames);
        Assert.Contains(c.FormerName, herring.History.BareFormerNames);

        Assert.True(FinderCase.TheRecordClears(herring, suspect),
            $"the herring is not cleared by her own record: {herring.Id} laid down "
            + $"{herring.History.Year}, against the suspect's {suspect.History.Year}.");
        Assert.False(FinderCase.TheRecordClears(suspect, herring),
            "the hull the case is about was cleared by her own record.");
    }

    /// <summary>
    /// <b>THE CUSTODY RULE, ASKED DIRECTLY, AND IT CAN ANSWER EITHER WAY.</b> The clause that stops
    /// <see cref="TheHerringCarriesTheNameAndHerOwnRecordClearsHer"/> from being a guard on a rule that only
    /// ever says yes: two records that differ in exactly one field, and the verdict follows the field.
    ///
    /// <para><b>Watched RED:</b> <c>TheRecordClears</c>' year comparison flipped to <c>&gt;</c> —
    /// <i>"Assert.True() Failure · the older papers did not clear her"</i>.</para>
    /// </summary>
    [Fact]
    public void TheOlderPapersAreTheOnesThatClear()
    {
        var old = new FinderCase.Hull("a", "A",
            new ShipHistory("Koski & Daughters Orbital Yards (Rauma Crater, Luna)", 2270,
                            ["ex-HALCYON (mail packet, Luna run)"], 2, "worn"));
        var young = old with { Id = "b", Callsign = "B", History = old.History with { Year = 2312 } };

        Assert.True(FinderCase.TheRecordClears(old, young), "the older papers did not clear her.");
        Assert.False(FinderCase.TheRecordClears(young, old), "the younger papers cleared her anyway.");

        // Same year: the deeper chain of owners is the older ownership of the name.
        FinderCase.Hull shallow = young with { History = young.History with { Year = 2270, OwnersDeep = 1 } };
        Assert.True(FinderCase.TheRecordClears(old, shallow));
        Assert.False(FinderCase.TheRecordClears(shallow, old));

        // No paper at all clears nobody — an absence of a record cannot be a record older than the story.
        var unpapered = new FinderCase.Hull("c", "C", new ShipHistory("", 0, ["ex-HALCYON (impounded)"], 0, ""));
        Assert.False(FinderCase.TheRecordClears(unpapered, old));
        Assert.True(FinderCase.TheRecordClears(old, unpapered));
    }

    // ══ THE OTHER NODES ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE CONFRONTATION IS THE SLOT NEXT TO THE ONE THAT PORT HAS ALWAYS GIVEN HIM</b>, on a roster with
    /// room for a neighbour — never an outpost, whose one collar has no next berth for the reveal to name.
    ///
    /// <para><b>Watched RED:</b> the <c>+ 1</c> dropped from <c>TheConfrontationPort</c> —
    /// <i>"the fourth name is tied up in the captain's own slot"</i>.</para>
    /// </summary>
    [Fact]
    public void TheFourthNameIsInTheBerthNextToYours()
    {
        foreach (OldCrew.Berth port in Berths)
        {
            if (!Names.ContainsKey(port.Id))
            {
                continue;
            }

            FinderCase.Case c = TheCase(port.Id);
            OldCrew.Berth at = Berths.First(b => b.Id == c.BerthPortId);
            int slots = DockRoster.BerthsAt(at.Tier);

            Assert.True(slots >= FinderCase.BerthsAConfrontationNeeds,
                $"{c.BerthPortId} keeps {slots} berth(s) — there is no next berth here.");
            Assert.InRange(c.BerthSlot, 0, slots - 1);
            Assert.NotEqual(DockRoster.OrdinaryBerth(at.Id, slots), c.BerthSlot);
            Assert.Equal((DockRoster.OrdinaryBerth(at.Id, slots) + 1) % slots, c.BerthSlot);
        }
    }

    /// <summary>
    /// <b>THE WITNESS DRINKS WHERE HER OWN ROTA PUTS HER.</b> The point of leaning on #414's rhythm rather
    /// than posting somebody somewhere: no berth in the world is a better bet for this regular than the one
    /// the case names.
    ///
    /// <para><b>Watched RED:</b> <c>TheWitness</c> made to take the FIRST berth in the list instead of the
    /// best-affinity one — <i>"THE FIXER is sought at cinder-roost (0.88) but the case sends the captain to
    /// selene-gate (0.32)"</i>.</para>
    /// </summary>
    [Fact]
    public void TheWitnessIsSoughtWhereTheRotaActuallyFavoursThem()
    {
        FinderCase.Case c = TheCase();
        double named = PatronRota.Affinity(c.WitnessId, c.WitnessPortId);

        foreach (OldCrew.Berth b in Berths)
        {
            Assert.True(PatronRota.Affinity(c.WitnessId, b.Id) <= named,
                $"{c.WitnessId} is a better bet at {b.Id} "
                + $"({PatronRota.Affinity(c.WitnessId, b.Id)}) than at the case's {c.WitnessPortId} "
                + $"({named}).");
        }
    }

    // ══ THE PAY ══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>BOTH SETTLINGS PAY THE FINDING; ONLY ONE OF THEM MOVES HEAT.</b> The arithmetic, asserted against
    /// the case's own numbers and against the meter's own step — never against a figure typed here.
    ///
    /// <para><b>Watched RED:</b> the bribe arm's <c>HeatPoints</c> set to <c>0</c> — <i>"Assert.Equal()
    /// Failure · Expected: 4 · Actual: 0"</i>, the bribe becoming free; and the turn-in arm handed
    /// <c>IllegalHeat.ABand</c> — <i>"turning a man in burned the port"</i>.</para>
    /// </summary>
    [Fact]
    public void TurningHimInCostsNothingAndTheBribeCostsABand()
    {
        FinderCase.Case c = TheCase();

        FinderCase.Payment turned = FinderCase.PayFor(c, FinderCase.Outcome.TurnedIn);
        Assert.Equal(c.PayCredits, turned.Credits);
        Assert.Equal(c.PayReputation + FinderCase.ReputationForTurningHimIn, turned.Reputation);
        Assert.Equal(0, turned.HeatPoints);

        FinderCase.Payment bribed = FinderCase.PayFor(c, FinderCase.Outcome.Bribed);
        Assert.Equal(c.PayCredits + c.BribeCredits, bribed.Credits);
        Assert.Equal(c.PayReputation, bribed.Reputation);
        Assert.Equal(IllegalHeat.ABand, bribed.HeatPoints);

        // The bribe is the richer purse and the poorer standing. That is the whole choice.
        Assert.True(bribed.Credits > turned.Credits);
        Assert.True(bribed.Reputation < turned.Reputation);

        // An unsettled case pays nothing at all and burns nobody.
        Assert.Equal(new FinderCase.Payment(0, 0, 0), FinderCase.PayFor(c, FinderCase.Outcome.Open));

        // …and the fee is inside its own published band, so a case is never a career.
        Assert.InRange(c.PayCredits, FinderCase.FeeFloorCredits, FinderCase.FeeCeilingCredits);
        Assert.Equal(c.PayCredits * FinderCase.BribeIsThisManyFees, c.BribeCredits);
    }

    /// <summary>A band is the METER's step and not a number this case typed — the sentence "one whole band"
    /// and the sentence "one rung warier at their gate" have to stay the same sentence.</summary>
    [Fact]
    public void ABandIsTheMetersOwnStep()
    {
        Assert.Equal(IllegalHeat.HeatPerRung, IllegalHeat.ABand);
        Assert.Equal(1, IllegalHeat.StartingRung(IllegalHeat.ABand));
        Assert.Equal(0, IllegalHeat.StartingRung(IllegalHeat.ABand - 1));
    }

    // ══ THE WORDS ════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE ELEVEN SENTENCES ARE THE CANON PASS, CHARACTER FOR CHARACTER</b>, checked against the copy
    /// retyped at the top of this file rather than against the constants themselves.
    ///
    /// <para><b>Watched RED:</b> the em-dash in <see cref="FinderCase.Payoff"/> replaced with a hyphen —
    /// <i>"Assert.Equal() Failure: Strings differ · ↓ (pos 21) · Paid. Don't thank me - the next one…"</i>.</para>
    /// </summary>
    [Fact]
    public void EveryLineIsTheCanonPassVerbatim()
    {
        string[] shipped = [.. FinderCase.AllProse()];
        Assert.Equal(Authored.Length, shipped.Length);
        for (int i = 0; i < Authored.Length; i++)
        {
            Assert.Equal(Authored[i], shipped[i]);
        }

        // …and the hook fills its brace with a port and nothing else.
        Assert.Equal(
            "A man walked off a hull at Selene Gate and never walked into the concourse. The hull has had "
            + "three names. Find the fourth.",
            FinderCase.Hook("Selene Gate"));
        Assert.Contains("Selene Gate", TheCase().TheHook);
    }

    /// <summary>
    /// <b>AND THERE IS NO TWELFTH STRING.</b> The source of both case files is read and every string literal
    /// in them is either one of the eleven, or a piece of plumbing that never reaches a screen — an id, a
    /// seed tag, a format figure. A sentence somebody adds tomorrow lands here on the day it is typed.
    ///
    /// <para><b>Watched RED:</b> a cheerful <c>"She is already gone."</c> added to <c>OutcomeLine</c>'s
    /// default arm — <i>"FinderCase.cs authors a sentence the canon pass never wrote: \"She is already
    /// gone.\""</i>.</para>
    /// </summary>
    [Fact]
    public void TheCaseAuthorsNothingTheCanonPassDidNot()
    {
        // Her NAME is the twelfth thing the canon pass wrote and the one that is not a sentence — it is a
        // heading, a plate and a ledger row. Asserted against the issue's spelling rather than waved through.
        Assert.Equal("Ilse Varga", FinderCase.DisplayName);

        var canon = new HashSet<string>(FinderCase.AllProse(), StringComparer.Ordinal)
        {
            FinderCase.DisplayName,
        };
        List<string> invented = [];

        foreach (string file in (string[])["FinderCase.cs", "FinderCase.Keeping.cs"])
        {
            foreach (string literal in Literals(CoreSource(file)))
            {
                // Plumbing: ids, seed tags, separators, figures and glyph fragments. A sentence has a space
                // in it and at least one word of more than three letters — the shape that reaches a screen.
                if (canon.Contains(literal) || !LooksLikeASentence(literal))
                {
                    continue;
                }

                invented.Add(literal);
            }
        }

        Assert.True(invented.Count == 0,
            "the finder's case authors sentences the canon pass never wrote:\n  "
            + string.Join("\n  ", invented.Select(s => $"\"{s}\"")));
    }

    /// <summary>
    /// <b>THE RESERVED WORD IS ABSENT, AND SO ARE THE AUTHORITY AND THE WATCHERS.</b> §8's word never
    /// appears; nor does the case ever name who would come for the man, which is the canon pass's own law
    /// and the reason the reveal says <i>a boat</i> and the crime is never stated.
    /// </summary>
    [Fact]
    public void TheCaseNamesNeitherTheReservedWordNorWhoComesForHim()
    {
        foreach (string line in FinderCase.AllProse())
        {
            foreach (string word in Forbidden)
            {
                Assert.False(line.Contains(word, StringComparison.OrdinalIgnoreCase),
                    $"a finder's line reaches for \"{word}\": {line}");
            }
        }
    }

    // ══ THE KEEPING ══════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE CASE AND HOW FAR DOWN IT HE HAS GOT SURVIVE A ROUND TRIP</b>, and a row this build cannot read
    /// is dropped rather than half-read.
    ///
    /// <para><b>Watched RED:</b> <c>Stored(in Case)</c> made to omit <c>BribeCredits</c> —
    /// <i>"Assert.True() Failure · a stored case came back unreadable"</i> (the width check refusing the
    /// short row), and with the width constant lowered to match, <i>"Assert.Equal() Failure · Expected:
    /// Case { …, BribeCredits = 1996 } · Actual: Case { …, BribeCredits = 0 }"</i>.</para>
    /// </summary>
    [Fact]
    public void TheCaseAndItsProgressComeBackOffTheVault()
    {
        FinderCase.Case c = TheCase();
        Assert.True(FinderCase.TryRead(FinderCase.Stored(c), out FinderCase.Case back),
                    "a stored case came back unreadable.");
        Assert.Equal(c, back);

        var walked = new FinderCase.Progress(
            Taken: true, WitnessHeard: true, PaperFound: true, HullRead: true,
            HerringCleared: true, Revealed: true, Settled: FinderCase.Outcome.Bribed, PaidOff: false);
        Assert.True(FinderCase.TryRead(FinderCase.Stored(walked), out FinderCase.Progress p));
        Assert.Equal(walked, p);
        Assert.True(walked.TrailWalked);
        Assert.True(walked.HasHistory);

        // A fresh book writes nothing anybody has to read back.
        Assert.False(FinderCase.Progress.Fresh.HasHistory);
        Assert.False(FinderCase.Progress.Fresh.TrailWalked);

        // …and the trail is not walked on two leads out of three.
        Assert.False((walked with { HullRead = false }).TrailWalked);

        // Rows this build cannot parse are refused, never half-read.
        Assert.False(FinderCase.TryRead("", out FinderCase.Case _));
        Assert.False(FinderCase.TryRead((string?)null, out FinderCase.Case _));
        Assert.False(FinderCase.TryRead("onetwothree", out FinderCase.Case _));
        Assert.False(FinderCase.TryRead("", out FinderCase.Progress _));
    }

    /// <summary>The whole vault carries it, so a captain halfway down a trail is halfway down it after a
    /// reload — and a file written before the finder existed loads with no case at all.</summary>
    [Fact]
    public void TheVaultCarriesTheFindersSection()
    {
        FinderCase.Case c = TheCase();
        var vault = new Vault
        {
            Finder = new FinderSection
            {
                Case = FinderCase.Stored(c),
                Progress = FinderCase.Stored(new FinderCase.Progress(
                    true, true, false, false, false, false, FinderCase.Outcome.Open, false)),
            },
        };

        Vault back = VaultSerializer.Load(VaultSerializer.Save(vault));
        Assert.NotNull(back.Finder);
        Assert.True(FinderCase.TryRead(back.Finder!.Case, out FinderCase.Case read));
        Assert.Equal(c, read);
        Assert.True(FinderCase.TryRead(back.Finder!.Progress, out FinderCase.Progress p));
        Assert.True(p.Taken);
        Assert.True(p.WitnessHeard);
        Assert.False(p.PaperFound);

        // A file from before the finder: no section, no case, and nothing thrown.
        Assert.Null(VaultSerializer.Load(VaultSerializer.Save(new Vault())).Finder);
    }

    // ══ READING THE SHIPPED SOURCE ═══════════════════════════════════════════════════════════════════════

    private static string CoreSource(string file)
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
        {
            at = at.Parent;
        }

        return File.ReadAllText(Path.Combine(
            at?.FullName ?? throw new DirectoryNotFoundException("no repo root above the test binary"),
            "src", "SpaceSails.Core", file));
    }

    /// <summary>
    /// Every string literal in a source file, comments and doc-comments stripped first — a paragraph
    /// explaining a sentence must never be mistaken for one.
    ///
    /// <para><b>Adjacent literals joined with <c>+</c> are ONE literal</b>, because that is what the compiler
    /// makes of them and what the player reads. A sweep that took them apart would report five fragments for
    /// two sentences and would let a whole authored line hide in a continuation — which is exactly what this
    /// guard did on its first run, and it is why it is written down here.</para>
    /// </summary>
    private static IEnumerable<string> Literals(string source)
    {
        string code = Regex.Replace(source, @"//[^\n]*", "");
        code = Regex.Replace(code, @"/\*.*?\*/", "", RegexOptions.Singleline);

        const string One = "\"(?:[^\"\\\\\\n]|\\\\.)*\"";
        foreach (Match m in Regex.Matches(code, $@"{One}(?:\s*\+\s*{One})*"))
        {
            var whole = new System.Text.StringBuilder();
            foreach (Match part in Regex.Matches(m.Value, One))
            {
                whole.Append(part.Value[1..^1].Replace("\\\"", "\"", StringComparison.Ordinal));
            }

            yield return whole.ToString();
        }
    }

    /// <summary>Does this literal look like something a player could read? A space in it and a word longer
    /// than three letters — which is true of every one of the eleven and of none of the ids, seed tags and
    /// separators the file is otherwise made of.</summary>
    private static bool LooksLikeASentence(string literal) =>
        literal.Contains(' ', StringComparison.Ordinal)
        && literal.Split(' ').Any(w => w.Trim('.', ',', ';', '—', '"').Length > 3);
}
