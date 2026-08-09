using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #728 · THE SHELTER SPOKE INTO INVISIBLE POCKETS.
///
/// <para>From the 2026-08-06 smoke run on THE WILD PLAIN, with the owner watching live and supplying the
/// punchline himself: <i>"the air fill and the reload functionality are not marked quite so clearly as they
/// could be… on shelters I always forget which is which. Maybe we could rename those."</i> When the owner of
/// the ammunition design cannot tell the ammunition fixture from the air fixture, the plates have failed.</para>
///
/// <para>Three legibility debts, none of them a bug in the simulation — every part of it was doing exactly
/// what #580 ruled it should:</para>
/// <list type="number">
/// <item>the wall press announced <i>"70 rounds into your magazines"</i> and it was TRUE, and there was
/// <b>nowhere on the on-foot screen</b> a captain could look to see a magazine;</item>
/// <item>two fixtures a metre apart, both mute, the whole burden on a 🫁 and a 🔫 read at a run;</item>
/// <item>the first-ground card promised <i>"loose rounds"</i> in your pockets, and no code path in this game
/// has ever put a round in a pocket.</item>
/// </list>
///
/// <para>So these guards are about SENTENCES AND PLATES, which is the register the whole ticket lives in —
/// and each one is written to go red on the copy that shipped before it.</para>
/// </summary>
public class TheShelterSaysWhatItDoesTests
{
    // ── 1 · THE MAGAZINES READOUT ─────────────────────────────────────────────────────────────────────

    /// <summary>Every roster the game can hand the instrument, swept — because the failure this replaces was
    /// not "the line is wrong", it was "there is no line", and a guard that checks one hand-typed roster
    /// cannot tell a readout fed by the world from a readout fed by a literal.</summary>
    private static IEnumerable<SentryBot.Carried[]> EveryRosterTheGroundCanHold()
    {
        yield return [];

        foreach (int rounds in new[] { 0, 1, 12, 45, 64, 98, SentryBot.MaxMagazine })
        {
            foreach (bool deployed in new[] { false, true })
            {
                yield return [new SentryBot.Carried(SentryBot.RosterUnits[0], rounds, deployed)];
            }
        }

        foreach (int first in new[] { 0, 7, 64, SentryBot.MaxMagazine })
        {
            foreach (int second in new[] { 0, 33, SentryBot.MaxMagazine })
            {
                yield return
                [
                    new SentryBot.Carried(SentryBot.RosterUnits[0], first, Deployed: false),
                    new SentryBot.Carried(SentryBot.RosterUnits[1], second, Deployed: true),
                ];
            }
        }
    }

    [Fact]
    public void EverySentryOnTheGroundIsNamedWithItsOwnFigureAndItsOwnCeiling()
    {
        // #740's law, applied to the other consumable: THE FIGURE NAMES WHAT IT IS. A bare "64" is a number
        // a captain has to convert before they can act on it; "64/99" against a MAGAZINES heading is the
        // quantity said out loud. The digits are SentryBot.Readout's — the same two the counter painted over
        // a deployed bot wears — so the HUD and the machine standing over there cannot come to disagree.
        var bad = new List<string>();

        foreach (SentryBot.Carried[] roster in EveryRosterTheGroundCanHold())
        {
            string line = SentryBot.MagazinesReadout(roster);

            if (roster.Length == 0)
            {
                continue;
            }

            foreach (SentryBot.Carried bot in roster)
            {
                string figure = $"{SentryBot.Readout(bot.Rounds)}/{SentryBot.MaxMagazine}";
                if (!line.Contains(bot.Unit, StringComparison.Ordinal))
                {
                    bad.Add($"  {Describe(roster)}: {bot.Unit} is not named — \"{line}\"");
                }
                if (!line.Contains(figure, StringComparison.Ordinal))
                {
                    bad.Add($"  {Describe(roster)}: {bot.Unit} holds {figure} and the line does not say so — \"{line}\"");
                }
            }
        }

        Report(bad, "rosters the magazines readout misreports");
    }

    [Fact]
    public void MovingARoundMovesTheLINE_SoTheReadoutCannotBeAPaintedNumber()
    {
        // THE PROPERTY THAT MATTERS, and the one a "does it contain 64/99" assertion cannot see: a readout
        // that ignored its argument and printed a constant would satisfy every containment check written
        // about a full magazine. So walk the whole magazine, one round at a time, and require the sentence to
        // be DIFFERENT at every depth — the instrument is fed by the world or it is decoration.
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int rounds = 0; rounds <= SentryBot.MaxMagazine; rounds++)
        {
            string line = SentryBot.MagazinesReadout(
                [new SentryBot.Carried(SentryBot.RosterUnits[0], rounds, Deployed: false)]);

            if (seen.TryGetValue(line, out int already))
            {
                Assert.Fail(
                    $"the readout says the same thing at {already} rounds and at {rounds}: \"{line}\" — " +
                    "a magazines line that does not move when the magazine does is a painted number.");
            }
            seen[line] = rounds;
        }
    }

    [Fact]
    public void ASlungSentryAndAPlantedOneAreToldApart()
    {
        // The locker fills BOTH (Map.Surface.ShelterLockerInteract spreads across every bot you have with
        // you, planted or slung), so both belong in the account the receipt pays into. But a captain reading
        // "R-3B 00/99" needs to know whether the dry one is on their back or standing out in the dark, which
        // is the difference between a walk to the press and a walk into the pack.
        string slung = SentryBot.MagazinesReadout(
            [new SentryBot.Carried("K-77", 40, Deployed: false)]);
        string planted = SentryBot.MagazinesReadout(
            [new SentryBot.Carried("K-77", 40, Deployed: true)]);

        Assert.NotEqual(slung, planted);
    }

    [Fact]
    public void TheInstrumentNeverGoesQUIET_NotEvenWithNothingToReport()
    {
        // #212's law: an instrument that vanishes when the news is bad is the worst shape a HUD has. A
        // captain who brought no sentry down is exactly the captain who most needs to be told why the wall
        // press is about to do nothing — and "there is nothing to fill" is a fact, not an absence.
        foreach (SentryBot.Carried[] roster in EveryRosterTheGroundCanHold())
        {
            string line = SentryBot.MagazinesReadout(roster);
            Assert.False(string.IsNullOrWhiteSpace(line),
                $"{Describe(roster)}: the magazines readout said nothing at all.");
        }

        Assert.Equal(SentryBot.NoMagazinesLine, SentryBot.MagazinesReadout([]));
        Assert.Equal(SentryBot.NoMagazinesLine, SentryBot.MagazinesReadout(null));

        // …and it says WHY, rather than printing an empty ledger and leaving the captain to guess.
        Assert.Contains("sentry", SentryBot.NoMagazinesLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheTubesOwnGunIsNotInYourLedger()
    {
        // FOUND BY BOOTING THE SCENE, not by reasoning — the owner's method, and it paid on the first
        // screenshot: /map?…&shelter=1&mags=12 drew
        //
        //   🔫 MAGAZINES · K-77 12/99 in the sling · R-3B 12/99 in the sling · GATE-1 99/99 set down
        //
        // GATE-1 is the shuttle door's built-in gun. It is topped back up after every volley, it never runs
        // dry, and SurfaceArrival's own words are that it is "never counted against the sling ... out of the
        // 'bots you carry' arithmetic". A ledger of what you are carrying that lists a permanent 99/99 you
        // neither bought nor can spend misreports both halves at once.
        var withTheHouseGun = new SentryBot.Carried[]
        {
            new(SentryBot.RosterUnits[0], 12, Deployed: false),
            new(SurfaceArrival.DoorSentryUnit, SurfaceArrival.DoorSentryRounds, Deployed: true),
        };

        string line = SentryBot.MagazinesReadout(withTheHouseGun);
        Assert.DoesNotContain(SurfaceArrival.DoorSentryUnit, line, StringComparison.Ordinal);
        Assert.Contains($"{SentryBot.RosterUnits[0]} 12/{SentryBot.MaxMagazine}", line, StringComparison.Ordinal);

        // …and the WORSE half of the same fact: the tube gun is always full, so a captain who brought
        // nothing down at all had a permanently-full magazine standing in his ledger answering "everything
        // is full" on his behalf — which is #728's own lie wearing a different hat, at the press.
        SentryBot.Carried[] onlyTheHouseGun =
            [new(SurfaceArrival.DoorSentryUnit, SurfaceArrival.DoorSentryRounds, Deployed: true)];

        Assert.Equal(SentryBot.NoMagazinesLine, SentryBot.MagazinesReadout(onlyTheHouseGun));
        Assert.False(SentryBot.AnythingToFill(onlyTheHouseGun),
            "the shelter's press would tell a captain carrying nothing that his magazines are full.");
        Assert.False(SentryBot.AnythingToFill([]));
        Assert.False(SentryBot.AnythingToFill(null));
        Assert.True(SentryBot.AnythingToFill(withTheHouseGun));
    }

    [Fact]
    public void TheInstrumentAndThePressAgreeAboutEverySlingThereIs()
    {
        // ONE FACT, ONE ANSWER. The readout says "none down here" exactly when the press says "nothing to
        // fill", because they ask the same Core question — and the day they stop agreeing is the day a
        // captain reads an empty ledger and is then told his magazines are full, which is where this ticket
        // started. Swept over every roster, with and without the house gun.
        var bad = new List<string>();

        foreach (SentryBot.Carried[] roster in EveryRosterTheGroundCanHold())
        {
            foreach (bool houseGun in new[] { false, true })
            {
                SentryBot.Carried[] onTheGround = houseGun
                    ? [.. roster, new SentryBot.Carried(
                        SurfaceArrival.DoorSentryUnit, SurfaceArrival.DoorSentryRounds, Deployed: true)]
                    : roster;

                bool saysNone = SentryBot.MagazinesReadout(onTheGround) == SentryBot.NoMagazinesLine;
                bool anything = SentryBot.AnythingToFill(onTheGround);

                if (saysNone == anything)
                {
                    bad.Add($"  {Describe(onTheGround)}: the readout says " +
                            $"{(saysNone ? "NONE" : "something")} and the press says " +
                            $"{(anything ? "something to fill" : "nothing to fill")}.");
                }
            }
        }

        Report(bad, "slings the instrument and the press disagree about");
    }

    // ── 2 · THE FIXTURES THAT DID NOT SAY WHAT THEY DO ────────────────────────────────────────────────

    /// <summary>A plate with its leading glyph struck off — which is the plate as somebody reads it at a run
    /// on a 9px monospace label over grey regolith, because that is the size and the surface these are read
    /// at and the emoji is the first thing to become a smudge.</summary>
    private static string WithoutTheGlyph(string plate) =>
        new string([.. plate.Where(c => char.IsAscii(c))]).Trim();

    [Fact]
    public void NeitherShelterPlateNeedsItsGlyphToSayWhichOneItIs()
    {
        // THE OWNER'S OWN REPRO, as an assertion. "🫁 CHARGING RACK" and "🔫 EMERGENCY LOCKER" were two
        // found-object nouns, correct and evocative, that named a rack and a locker and NOT the thing either
        // one gives you — so the glyph carried the whole of the meaning and the glyph is what is lost.
        //
        // The nouns are not the fix and are deliberately still here: a charging rack is what whoever left
        // this out here would have called it, and renaming the fixtures would spend the fiction to buy the
        // clarity. What was added is the VERB — the same move UndergroundComplex.RefugeGlyph already makes
        // by putting REFUGE · in front of its claim so the scope is part of the promise.
        string tank = WithoutTheGlyph(SurfaceShelter.TankLabel);
        string locker = WithoutTheGlyph(SurfaceShelter.LockerLabel);

        Assert.Contains("TANK", tank, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MAGAZINE", locker, StringComparison.OrdinalIgnoreCase);

        // And neither plate may claim the other's resource: two fixtures a metre apart that both mention
        // both would be a worse muddle than the silence this replaces.
        Assert.DoesNotContain("MAGAZINE", tank, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TANK", locker, StringComparison.OrdinalIgnoreCase);

        // The found-object nouns survive. This is the half a rename would have cost.
        Assert.Contains("RACK", tank, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LOCKER", locker, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheDoorAlreadySaidItsOwnScope_AndKeepsIt()
    {
        // The one plate on this building that was never part of the problem, pinned so a sweep does not
        // "finish the job" on it. A shelter's sign has said PRESSURISED since #573 because a captain who
        // cannot find air is not being teased — that IS its function, stated.
        Assert.Contains("PRESSURISED", SurfaceShelter.DoorLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThePressHasADifferentAnswerForFullAndForNothingToFill()
    {
        // THE THIRD NAMED BUG CLASS, at the one fixture whose whole job is ammunition: the sim doing one
        // thing while a sentence reports another. Both nothings reached LockerFullLine, so a captain who had
        // brought no sentry down at all was told his magazines were FULL — and that is the case the press
        // answers most often, because the press is why you walked here.
        //
        // The outpost hut's locker has always got this right ("you have nothing to put them in"); the
        // shelter's had not.
        Assert.NotEqual(SurfaceShelter.LockerFullLine, SurfaceShelter.LockerNothingToFillLine);

        Assert.Contains("full", SurfaceShelter.LockerFullLine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("full", SurfaceShelter.LockerNothingToFillLine, StringComparison.OrdinalIgnoreCase);

        // …and the empty-handed answer says what is missing, which is the sentry rather than the ammunition.
        Assert.Contains("sentry", SurfaceShelter.LockerNothingToFillLine, StringComparison.OrdinalIgnoreCase);
    }

    // ── 3 · THE COPY THAT PROMISED ROUNDS IT NEVER HOLDS ──────────────────────────────────────────────

    /// <summary>Every receipt in the game for rounds a captain has just come into. All three, so the law is
    /// about WHAT HAPPENS TO A FOUND ROUND rather than about one sentence somebody edited.</summary>
    private static IEnumerable<(string Where, string Line)> EveryReceiptForFoundRounds()
    {
        yield return ("the shelter's wall press", SurfaceShelter.LockerLine(70));
        yield return ("the outpost hut's locker", SurfaceOutpost.CacheLine(45));
        yield return ("the half-shut drawer in a ruin", SurfaceSalvage.RoundsLine(12));
    }

    [Fact]
    public void NoRoundEverGoesInAPocket_AndNothingSaysItDoes()
    {
        // #740's law — THE SENTENCE OWNS ITS FACTS — pointed at the first-ground card.
        //
        // The card told a new captain that "loose rounds" go in their pockets. Nothing in this game has ever
        // put a round in a pocket: the shelter press, the outpost locker and the drawer in a ruin all rack
        // straight across the sentries, by #580's ruling that ammunition is not inventory and the scarcity
        // lives in the WALK. So the promise was never keepable — and its cost was exact: the owner opened
        // the satchel on the spot where a press had just paid him seventy rounds, read "Empty", and filed
        // suspected theft against a feature that was working perfectly.
        //
        // Guarded as the AGREEMENT between the card and the receipts, not as a phrase ban: if a round ever
        // does start riding the satchel, the receipt for it will say so and this test will say where.
        var bad = new List<string>();

        foreach ((string where, string line) in EveryReceiptForFoundRounds())
        {
            foreach (string pocketWord in new[] { "pocket", "satchel", "bag" })
            {
                if (line.Contains(pocketWord, StringComparison.OrdinalIgnoreCase))
                {
                    bad.Add($"  {where} says the rounds go in a {pocketWord}: \"{line}\"");
                }
            }
        }

        GroundLesson.Lever pocket = Assert.Single(GroundLesson.Levers, l => l.Key == "I");
        if (bad.Count == 0
            && pocket.What.Contains("rounds", StringComparison.OrdinalIgnoreCase)
            && !pocket.What.Contains("magazine", StringComparison.OrdinalIgnoreCase))
        {
            bad.Add(
                "  the first-ground card lists rounds among what the pocket holds while no receipt in the " +
                $"game puts one there: \"{pocket.What}\"");
        }

        Report(bad, "sentences that disagree about where a found round ends up");
    }

    [Fact]
    public void TheCardStillPointsSomewhereForTheRoundsItStoppedPromising()
    {
        // Deleting the word would have been the cheap fix and the wrong one: "loose rounds" was doing real
        // work on that card — it was the one line telling a new captain that ammunition is a thing out here
        // at all. So the sentence keeps the subject and corrects the destination, which is the shape #740
        // asks for: name the quantity you are actually talking about.
        GroundLesson.Lever pocket = Assert.Single(GroundLesson.Levers, l => l.Key == "I");

        Assert.Contains("magazine", pocket.What, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sentr", pocket.What, StringComparison.OrdinalIgnoreCase);

        // …and the three things #603's own guard put on this lever are untouched by the edit.
        Assert.Contains("stays yours", pocket.What, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("try", pocket.What, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("door", pocket.What, StringComparison.OrdinalIgnoreCase);
    }

    // ── the demo, because a fix nobody can stand in front of is a fix nobody checks ────────────────────

    [Fact]
    public void TheShelterHasItsOwnDevStartRow()
    {
        // This project's own rule, written beside the cheat parser: "a scene nobody can reach on demand is a
        // scene that ships broken." The shelters seed DEEP by design (SurfaceShelter.PlacesOn keeps them out
        // of the landing band), so every look at these plates cost a two-minute walk across 310 x 260 du —
        // which is how a fixture pair the owner himself designed went unread for three months.
        DevStarts.Entry row = Assert.Single(
            DevStarts.All, e => e.Url.Contains("shelter=1", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("mags=", row.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("land=1", row.Url, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(row.Blurb));
    }

    private static string Describe(IReadOnlyList<SentryBot.Carried> roster) =>
        roster.Count == 0
            ? "an empty sling"
            : string.Join(", ", roster.Select(b => $"{b.Unit}@{b.Rounds}{(b.Deployed ? " (set down)" : "")}"));

    private static void Report(List<string> bad, string what)
    {
        if (bad.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} {what}:");
        foreach (string line in bad.Take(30))
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }
}
