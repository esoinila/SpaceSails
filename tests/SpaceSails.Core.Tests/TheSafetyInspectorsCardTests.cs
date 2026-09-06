using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1149 slice 2 · <b>THE SAFETY INSPECTOR'S CARD, HELD TO ITS OWN LAWS.</b>
///
/// <para>Slice 1 put an unsigned inspection tag on the valve of every refuge in the game. This is the man
/// the tag is about, and the whole feature is one object in the Fletch wallet with an issuer that is no
/// site (<see cref="Inspectorate"/>). Five laws, and every one of them is asked of a world wide enough to
/// tell pass from fail:</para>
///
/// <list type="number">
/// <item>honoured at the refuge floors of EVERY site, on every watch;</item>
/// <item>away from a refuge floor the ROSTER decides, and the world really contains both answers;</item>
/// <item>a site whose refuge failed is always due — which is what makes the card found in that refuge good
/// on the ground it was found on, without the card remembering anything;</item>
/// <item>the gates open for one excursion, and the ID CHECK band defers with them;</item>
/// <item>the price is derived, the three strings are the only prose, the reserved word is nowhere, and the
/// card is never consumed.</item>
/// </list>
/// </summary>
public sealed class TheSafetyInspectorsCardTests
{
    /// <summary>The scenario's own moons plus a wide net of generated ids — the discipline
    /// <c>TheRefugesUndergroundTests</c> keeps: the scenario ten prove the game people play, the generated
    /// ninety prove the GENERATOR, and a law that only holds on the shipped list is not a law.</summary>
    private static IEnumerable<string> ManySites()
    {
        foreach (string body in new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
        })
        {
            yield return body;
        }
        for (int i = 0; i < 90; i++)
        {
            yield return $"generated-moon-{i}";
        }
    }

    /// <summary>A spread of watches, so nothing here is a claim about one afternoon.</summary>
    private static IEnumerable<long> ManyWatches()
    {
        for (long w = 0; w < 24; w++)
        {
            yield return w;
        }
    }

    // ── (1) THE REFUGE FLOORS, ALWAYS ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>AN INSPECTOR MAY LOOK AT ANY REFUGE.</b> Every refuge floor of every site in the net, on
    /// twenty-four consecutive watches, answers <see cref="WalletChoice.Outcome.Inspection"/> — whatever the
    /// roster says, because that is what the word means and what the tag on every valve in the game says he
    /// does.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>RefugeOnThePlan</c> clause out of
    /// <see cref="Inspectorate.HonouredAt"/>, which leaves the roster deciding on refuge floors too:</para>
    /// <code>
    /// 3122 of 3744 refuge floor/watch pair(s) break the law: the inspector's card is honoured at every
    /// refuge floor of every site
    /// </code>
    /// </summary>
    [Fact]
    public void TheCardIsHonouredAtEveryRefugeFloorOfEverySiteOnEveryWatch()
    {
        var bad = new List<string>();
        int seen = 0;

        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.RefugeOnThePlan(body, level))
                {
                    continue;
                }
                foreach (long watch in ManyWatches())
                {
                    seen++;
                    WalletChoice.Outcome how =
                        WalletChoice.WhatHappens(body, level, watch, Inspectorate.Card);
                    if (how != WalletChoice.Outcome.Inspection)
                    {
                        bad.Add($"  {body} B{-level} watch {watch}: {how}");
                    }
                }
            }
        }

        Report(bad, seen, "the inspector's card is honoured at every refuge floor of every site", 1000);
    }

    // ── (2) AND AWAY FROM ONE, THE ROSTER ────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>NOBODY INSPECTS UNANNOUNCED — and the world contains both answers.</b> On floors that carry no
    /// refuge the read follows <see cref="Inspectorate.InspectionIsDue"/> exactly, and the sweep is only
    /// allowed to make that claim if it actually met a healthy number of each: a bench where every site was
    /// due (or none was) cannot tell a bet from a ticket, which is this house's fifth named bug class.
    ///
    /// <para><b>Proven RED</b> by making <see cref="Inspectorate.HonouredAt"/> answer true unconditionally:
    /// </para>
    /// <code>
    /// 1810 of 2160 pressurised floor/watch pair(s) break the law: away from a refuge floor the roster
    /// decides
    /// </code>
    /// </summary>
    [Fact]
    public void AwayFromARefugeFloorTheRosterDecidesAndBothAnswersReallyHappen()
    {
        var bad = new List<string>();
        int seen = 0, due = 0, notDue = 0;

        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.RefugeOnThePlan(body, level))
                {
                    continue;
                }
                foreach (long watch in ManyWatches())
                {
                    seen++;
                    bool expected = Inspectorate.InspectionIsDue(body, watch);
                    if (expected)
                    {
                        due++;
                    }
                    else
                    {
                        notDue++;
                    }

                    WalletChoice.Outcome how =
                        WalletChoice.WhatHappens(body, level, watch, Inspectorate.Card);
                    WalletChoice.Outcome want = expected
                        ? WalletChoice.Outcome.Inspection
                        : WalletChoice.Outcome.NoInspectionDue;
                    if (how != want)
                    {
                        bad.Add($"  {body} B{-level} watch {watch}: {how}, and the roster says {want}");
                    }
                }
            }
        }

        Assert.True(due > 100 && notDue > 100,
            $"the sweep met {due} due and {notDue} not-due floor/watch pairs — a bench that only ever saw "
            + "one answer cannot tell a bet from a ticket.");
        Report(bad, seen, "away from a refuge floor the roster decides", 1000);
    }

    /// <summary>
    /// <b>A FOUND CARD IS ALWAYS DUE AT ITS OWN SITE</b>, and the card is not what remembers it — the
    /// GROUND is. A site whose one refuge failed has an inspector who came and never signed for the seal he
    /// replaced, and it is still expecting the rest of that visit; so every watch is due there, forever, for
    /// any card the captain brings.
    ///
    /// <para>The control is the other half and it is what makes this a guard: sites with no failed refuge
    /// must NOT be always-due, or the clause is decorative.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the <c>FailedRefugeFloorOf</c> clause from
    /// <see cref="Inspectorate.InspectionIsDue"/>:</para>
    /// <code>
    /// 23 site(s) with a failed refuge are not expecting their inspector on every watch
    /// </code>
    /// </summary>
    [Fact]
    public void ASiteWhoseRefugeFailedIsAlwaysExpectingItsInspector()
    {
        var bad = new List<string>();
        int withAStory = 0, without = 0, withoutAlwaysDue = 0;

        foreach (string body in ManySites())
        {
            bool failed = UndergroundComplex.FailedRefugeFloorOf(body) is not null;
            bool always = ManyWatches().All(w => Inspectorate.InspectionIsDue(body, w));

            if (failed)
            {
                withAStory++;
                if (!always)
                {
                    bad.Add($"  {body}: its refuge failed and it is not expecting anybody.");
                }
            }
            else
            {
                without++;
                if (always)
                {
                    withoutAlwaysDue++;
                }
            }
        }

        Assert.True(withAStory >= 10 && without >= 10,
            $"the net has {withAStory} site(s) with a failed refuge and {without} without — it cannot tell "
            + "the clause from its absence.");
        Assert.True(withoutAlwaysDue == 0,
            $"{withoutAlwaysDue} site(s) with no failed refuge are due on EVERY watch — the roster is not a "
            + "roster, and the bet is not a bet.");
        Assert.True(bad.Count == 0,
            $"{bad.Count} site(s) with a failed refuge are not expecting their inspector on every watch:"
            + Environment.NewLine + string.Join(Environment.NewLine, bad.Take(10)));

        // …and standing in the failed refuge itself, the card reads as an inspection on any watch — which is
        // the two clauses meeting, and the sentence the whole first road is written to make true.
        foreach (string body in ManySites())
        {
            if (UndergroundComplex.FailedRefugeFloorOf(body) is not { } level)
            {
                continue;
            }
            foreach (long watch in ManyWatches())
            {
                Assert.Equal(
                    WalletChoice.Outcome.Inspection,
                    WalletChoice.WhatHappens(body, level, watch, Inspectorate.Card));
            }
        }
    }

    // ── (3) THE GATES, FOR ONE EXCURSION ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE SEALED ROW OPENS AND THE ID CHECK ROW DEFERS — while an inspection is running, and only
    /// then.</b> Both halves are asked of the same site with the same wallet, differing in exactly one
    /// boolean, so a panel that could not tell the two apart goes red on one of them.
    ///
    /// <para>Also asked: the keypad comes off the row an inspection opened (an affordance with nothing behind
    /// it, #212), and the row does NOT claim a countersignature was read
    /// (<c>GateOpenedByRidingTo</c>).</para>
    ///
    /// <para><b>Proven RED</b> by dropping <c>inspectionOpens</c> out of the panel's <c>opens</c>:</para>
    /// <code>
    /// generated-moon-0 B1: an inspection is running and the gate still reads ↓ THE OTHER SHAFT — SEALED
    /// </code>
    /// </summary>
    [Fact]
    public void TheGatesOpenWhileAnInspectionIsRunningAndCloseWhenItIsOver()
    {
        var bad = new List<string>();
        int seen = 0;

        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.LiftStop? shut = TheGateRow(
                    UndergroundComplex.LiftPanel(body, level, [], [Inspectorate.Card]));
                if (shut is not { Refusal: not null } closed || !closed.HasPad)
                {
                    continue;   // not the SEALED row this law is about
                }

                seen++;
                UndergroundComplex.LiftStop open = TheGateRow(
                    UndergroundComplex.LiftPanel(
                        body, level, [], [Inspectorate.Card], 0, null, inspectionRunning: true))!.Value;

                if (open.Refusal is not null)
                {
                    bad.Add($"  {body} B{-level}: an inspection is running and the gate still reads "
                        + $"{open.Name}");
                    continue;
                }
                if (open.HasPad)
                {
                    bad.Add($"  {body} B{-level}: the gate is open and the keypad is still bolted to it.");
                }
                if (!open.OpenedByInspection || open.OpenedBy is not { Length: > 0 } said
                    || !said.Contains(Inspectorate.Plate, StringComparison.Ordinal))
                {
                    bad.Add($"  {body} B{-level}: the row does not say what opened it ({open.OpenedBy}).");
                }
                if (UndergroundComplex.GateOpenedByRidingTo(body, level, open.Level, []) is not null)
                {
                    bad.Add($"  {body} B{-level}: the ride narrates a countersignature nobody read.");
                }
            }
        }

        Report(bad, seen, "a SEALED gate opens while an inspection is running", 20);
    }

    /// <summary>
    /// <b>…AND THE HEAT GATE DEFERS TO IT.</b> #715's ID CHECK row — an outfit that remembers this captain
    /// wanting the face as well as the paper — steps aside for an inspection in progress, and steps back the
    /// moment it is over. The wallet is the countersignature card plus the inspector's card, because the ID
    /// CHECK row only exists at all once the paper is already good.
    ///
    /// <para><b>Proven RED</b> by dropping <c>!inspectionRunning</c> out of the panel's
    /// <c>wantsAFace</c>:</para>
    /// <code>
    /// 0 gate(s) deferred to an inspection — the ID CHECK row is not the row this guard thinks it is.
    /// </code>
    /// </summary>
    [Fact]
    public void TheIdCheckRowDefersToAnInspectionAndOnlyForThatTrip()
    {
        int deferred = 0, shutAgain = 0;
        int heat = IllegalHeat.TheGateWantsAFaceAt;

        foreach (string body in ManySites())
        {
            if (UndergroundComplex.IsHeadOffice(body))
            {
                continue;
            }
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                int band = UndergroundComplex.NextShaftBelow(body, level) ?? -1;
                if (band < 0)
                {
                    continue;
                }
                string cardId = new UndergroundComplex.AuthorityCard(body, band).Id;
                Satchel.Item[] wallet = [Inspectorate.Card];

                UndergroundComplex.LiftStop? hot = TheGateRow(
                    UndergroundComplex.LiftPanel(body, level, [cardId], wallet, heat));
                if (hot is not { } asked
                    || asked.Refusal != IllegalHeat.TheGateWantsAFaceLine)
                {
                    continue;
                }

                shutAgain++;
                UndergroundComplex.LiftStop during = TheGateRow(
                    UndergroundComplex.LiftPanel(
                        body, level, [cardId], wallet, heat, null, inspectionRunning: true))!.Value;

                // …and the row NAMES THE PAPER THAT IS ACTUALLY GOOD, which is the half the row's own
                // #715 clause is about: while the gate is asking for a face it names nothing, and the
                // moment it stops asking, the captain is told about the countersignature he is holding —
                // the deeper permission, and the one that will still be in his wallet tomorrow.
                if (during.Refusal is null
                    && during.OpenedBy == UndergroundComplex.CardTitle(
                        new UndergroundComplex.AuthorityCard(body, band)))
                {
                    deferred++;
                }
            }
        }

        Assert.True(shutAgain >= 10,
            $"only {shutAgain} ID CHECK row(s) were found in the whole net — this bench is not standing in "
            + "front of #715's gate at all.");
        Assert.True(deferred == shutAgain,
            $"{deferred} of {shutAgain} gate(s) deferred to an inspection — the heat gate is meant to step "
            + "aside for the trip and step back afterwards.");
    }

    // ── (4) THE PRICE ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE FENCE'S PRICE IS DERIVED AND NOT A NUMBER ANYBODY TYPED.</b> Three inputs, all somebody
    /// else's: what a document costs across this desk, how many tiers of pass this game has, and the fence's
    /// own markup. The composition is asserted, and then the market's own constants are asked to move it —
    /// which is the property a typed price does not have.
    ///
    /// <para><b>Proven RED</b> by replacing the body of <see cref="Inspectorate.FencePrice"/> with a
    /// plausible round number:</para>
    /// <code>
    /// Assert.Equal() Failure · Expected: 1560 · Actual: 1500
    /// </code>
    /// </summary>
    [Fact]
    public void TheFencePriceIsDerivedFromTheMarketAndTheTopOfThePassLadder()
    {
        int worth = IntelMarket.BasePrice * Inspectorate.TiersOfPass;

        Assert.Equal(CanteenTable.OverqualifiedBand + 1, Inspectorate.TiersOfPass);
        Assert.Equal(CompromisingChip.FencePrice(worth), Inspectorate.FencePrice);

        // The markup is REAL: the fence wants more than the paper is worth, by the market's own fraction.
        Assert.True(Inspectorate.FencePrice > worth,
            $"the fence sells at {Inspectorate.FencePrice} cr for a {worth} cr document — there is no "
            + "markup in this price at all.");
        Assert.Equal(
            worth + IntelMarket.SellPrice(CompromisingChip.CertainAsPhotographs, worth),
            Inspectorate.FencePrice);

        // …and the source carries no digit of it. Every number in this file is somebody else's constant.
        string src = SourceOf("src", "SpaceSails.Core", "Inspectorate.cs");
        Assert.DoesNotContain(
            Inspectorate.FencePrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
            WithoutComments(src), StringComparison.Ordinal);
    }

    // ── (5) THE PROSE, THE RESERVED WORD, THE VAULT, AND WHAT NEVER HAPPENS ──────────────────────────────

    /// <summary>
    /// <b>THREE SENTENCES, VERBATIM, AND THERE IS NO FOURTH.</b> The canon pass of 2026-09-06 authored a
    /// plate, a look card and one thing a man says; this asserts all three letter for letter, asserts that
    /// <see cref="Inspectorate.AllProse"/> is exactly them, and then reads the SLICE'S OWN FILES for string
    /// literals — because reflection catches a new <c>const</c> and does not catch a sentence typed straight
    /// into a method, which is how prose actually gets into this codebase.
    ///
    /// <para><b>Proven RED</b> by pulsing a line of my own from <c>BuyTheInspectorCard</c>:</para>
    /// <code>
    /// the card's own files carry a sentence nobody authored: "He does not ask what you want it for."
    /// </code>
    /// </summary>
    [Fact]
    public void TheThreeAuthoredStringsAreVerbatimAndThereIsNoFourth()
    {
        Assert.Equal("INSPECTORATE", Inspectorate.Plate);
        Assert.Equal(
            "Inspectorate credentials. The photograph has been replaced once already.",
            Inspectorate.LookCardLine);
        Assert.Equal("Inspection. Nobody told us. Nobody ever does.", Inspectorate.HonouredLine);

        Assert.Equal(
            new[] { Inspectorate.Plate, Inspectorate.LookCardLine, Inspectorate.HonouredLine },
            Inspectorate.AllProse().ToArray());

        // …and they are what the game actually SAYS, off the sim rather than off the constants: the face of
        // the card, the look card, and the read.
        Assert.Equal(Inspectorate.Plate, PatrolBeat.BadgeTitle(Inspectorate.IssuerId));
        Assert.Equal(Inspectorate.Plate, WalletChoice.Claims(Inspectorate.Card));
        CarriedObject.Reveal look = Assert.IsType<CarriedObject.Reveal>(
            CarriedObject.Card(Inspectorate.Card, "luna"));
        Assert.Equal(Inspectorate.LookCardLine, look.Story);
        Assert.Contains(Inspectorate.Plate, look.Label, StringComparison.Ordinal);

        (string body, int level, long watch) = ARefugeFloor();
        Assert.Equal(
            Inspectorate.HonouredLine,
            PatrolBeat.TheGuardReads(body, level, watch, "A ROUND", Inspectorate.Card, false).Line);

        // THE SOURCE SWEEP. Any string literal in the slice's own two files that reads like a SENTENCE —
        // a space in it and a full stop at the end — has to be one of the two authored ones.
        var sentences = new List<string>();
        foreach (string file in new[]
        {
            SourceOf("src", "SpaceSails.Core", "Inspectorate.cs"),
            SourceOf("src", "SpaceSails.Client", "Pages", "Map.Inspectorate.cs"),
        })
        {
            foreach (Match m in Regex.Matches(WithoutComments(file), "\"(?:[^\"\\\\\\n]|\\\\.)*\""))
            {
                string text = m.Value.Trim('"');
                if (text.Contains(' ', StringComparison.Ordinal)
                    && text.EndsWith('.')
                    && text != Inspectorate.LookCardLine
                    && text != Inspectorate.HonouredLine)
                {
                    sentences.Add(m.Value);
                }
            }
        }

        Assert.True(sentences.Count == 0,
            "the card's own files carry a sentence nobody authored: "
            + string.Join(", ", sentences)
            + " — a beat that wants a line gets a // FABLE: marker rather than one typed by this crew.");
    }

    /// <summary>§8's reserved word is nowhere near this feature — asked of every sentence it can put on a
    /// screen, composed the way the game composes them rather than read off the constants.</summary>
    [Fact]
    public void TheReservedWordIsNowhereInIt()
    {
        (string body, int level, long watch) = ARefugeFloor();
        var said = new List<string>(Inspectorate.AllProse())
        {
            PatrolBeat.BadgeTitle(Inspectorate.IssuerId),
            WalletChoice.Claims(Inspectorate.Card),
            FoundPass.Plate(Inspectorate.Card),
            CarriedObject.Card(Inspectorate.Card, body)!.Value.Label,
            CarriedObject.Card(Inspectorate.Card, body)!.Value.Story,
            PatrolBeat.TheGuardReads(body, level, watch, "A ROUND", Inspectorate.Card, false).Told,
            WalletChoice.ShownNote(
                Inspectorate.Card, body, level, WalletChoice.Outcome.Inspection, "SOMEBODY"),
            WalletChoice.ShownNote(
                Inspectorate.Card, body, level, WalletChoice.Outcome.NoInspectionDue, "SOMEBODY"),
            WalletChoice.ReasonTag(WalletChoice.Outcome.Inspection),
            WalletChoice.ReasonTag(WalletChoice.Outcome.NoInspectionDue),
        };

        Assert.True(said.Count > 8, "this sweep is too small to mean anything.");
        foreach (string line in said)
        {
            Assert.DoesNotContain("monolith", line, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// <b>IT IS A POSSESSION AND IT IS NEVER SPENT.</b> Round-tripped through the row's own storage and
    /// through the whole vault — a card that evaporated when the shuttle lifted would not be a possession —
    /// and then read fifty times over, on both arms of the ladder, without going anywhere.
    ///
    /// <para><b>RED</b> by minting the card under an id the badge parser cannot read back.</para>
    /// </summary>
    [Fact]
    public void ItSurvivesTheVaultAndIsNeverConsumed()
    {
        Satchel.Item card = Inspectorate.Card;
        Assert.True(Satchel.Item.TryParse(card.Stored, out Satchel.Item read));
        Assert.Equal(card, read);

        var saved = new Vault
        {
            Version = Vault.CurrentVersion,
            Satchel = new SatchelSection { Items = [card.Stored] },
        };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(saved));

        string stored = Assert.Single(loaded.Satchel!.Items);
        Assert.True(Satchel.Item.TryParse(stored, out Satchel.Item back));
        Assert.Equal(card, back);
        Assert.True(Inspectorate.IsTheCard(back));
        Assert.True(Inspectorate.Held([back]));

        // …and reading it, either way, takes nothing off anybody: the ladder is pure and the wallet it is
        // asked about is the same wallet afterwards. The bet is paid in escorts, never in laminate.
        (string body, int level, _) = ARefugeFloor();
        IReadOnlyList<Satchel.Item> wallet = [back];
        for (long watch = 0; watch < 50; watch++)
        {
            WalletChoice.WhatHappens(body, level, watch, back);
            PatrolBeat.TheGuardReads(body, level, watch, "A ROUND", back, false);
            Assert.True(Inspectorate.Held(wallet));
            Assert.Single(WalletChoice.Fan(body, wallet));
        }
    }

    /// <summary>The card is in the guard's fan and is judged by the ladder, so it can actually be chosen and
    /// handed over — the seam #836 built. And it is never MINTED as a false ID: <c>FoundPass</c> draws only
    /// from the grounds a world has, and the issuer is not one.</summary>
    [Fact]
    public void ItIsAPaperAGuardWouldReadAndNeverAFalseId()
    {
        Assert.True(WalletChoice.AGuardWouldReadIt(Inspectorate.Card.Kind));

        string[] world = ["inspector-world-a", "inspector-world-b", Inspectorate.IssuerId];
        for (ulong seed = 0; seed < 64; seed++)
        {
            Satchel.Item? minted = FoundPass.MintedElsewhere(world[0], world[..2], seed);
            Assert.False(minted is { } m && Inspectorate.IsTheCard(m));
        }
    }

    // ── the bench's plumbing ─────────────────────────────────────────────────────────────────────────────

    /// <summary>A real refuge floor of a real site, on a watch. Never a hand-typed level: a guard handed a
    /// floor the generator never made is a guard that cannot tell pass from fail.</summary>
    private static (string Body, int Level, long Watch) ARefugeFloor()
    {
        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.RefugeOnThePlan(body, level))
                {
                    return (body, level, 3L);
                }
            }
        }
        throw new InvalidOperationException("no site in the net has a refuge on any floor.");
    }

    /// <summary>The panel's gate row — the one that names the shaft below — or null where the panel has
    /// none. Read off the shipped panel rather than re-derived here.</summary>
    private static UndergroundComplex.LiftStop? TheGateRow(
        IReadOnlyList<UndergroundComplex.LiftStop> panel)
    {
        foreach (UndergroundComplex.LiftStop stop in panel)
        {
            if (stop.Name.StartsWith("↓ THE OTHER SHAFT", StringComparison.Ordinal))
            {
                return stop;
            }
        }
        return null;
    }

    private static string WithoutComments(string source)
    {
        string noBlock = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(noBlock, @"//[^\n]*", " ");
    }

    private static string SourceOf(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SpaceSails.slnx")))
        {
            dir = dir.Parent;
        }
        Assert.True(dir is not null, $"no repo root above {AppContext.BaseDirectory}");

        string path = Path.Combine(new[] { dir!.FullName }.Concat(parts).ToArray());
        Assert.True(File.Exists(path), $"{path} is gone — this guard is watching a file that moved.");
        return File.ReadAllText(path);
    }

    private static void Report(List<string> bad, int seen, string law, int atLeast)
    {
        Assert.True(seen >= atLeast, $"only {seen} case(s) were walked — this proved nothing about {law}.");
        if (bad.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} of {seen} case(s) break the law: {law}");
        foreach (string line in bad.Take(20))
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }
}
