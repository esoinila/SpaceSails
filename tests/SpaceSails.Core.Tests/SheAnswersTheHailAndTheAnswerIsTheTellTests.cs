using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #534 slice 2 · <b>THE FIFTH TELL IS A SENTENCE, AND IT IS STILL NOT A VERDICT.</b>
///
/// <para>Slice 1's four tells are numbers, and its own guard reddens if <see cref="QShip"/> ever grows a
/// string. Tell (e) is prose — how she answers a hail — so it lives one file over in
/// <see cref="QShipHail"/>, and these guards hold it to the same law the numbers are held to: the two
/// answers are canon, verbatim, composed only out of what her own record already carries; asking twice
/// agrees; the game states no conclusion anywhere in either line; and there is not a third sentence in the
/// file for a future hand to hang a label on.</para>
///
/// <para><b>The canon lines are RETYPED below, not read off the type.</b> A guard that asserted the constant
/// equals itself would stay green through any rewrite of it — <i>a green number never asked of the world</i>,
/// in its exact shape. What is typed here is what the owner filed on 2026-09-05, and if the shipped line
/// drifts by one character these go red with both spellings in the message.</para>
///
/// <para><b>The anti-vacuous half:</b> every law is asked of an honest hauler and a masked hull in the same
/// breath, because "the answers differ" is only worth something if one of them is the ordinary one.</para>
/// </summary>
public sealed class SheAnswersTheHailAndTheAnswerIsTheTellTests
{
    // ── THE CANON, RETYPED ───────────────────────────────────────────────────────────────────────────

    /// <summary>The owner's 2026-09-05 line for an honest merchant, with the braces filled by hand:
    /// identity, cargo, destination, then the question.</summary>
    private const string HonestMerchantSays = "MERIDIAN here — bulk, bound Mars. What do you want?";

    /// <summary>And the masked hull's, filled the same way: an acknowledgement, intent demanded before
    /// identity given, and an instruction no hauler has ever had cause to give.</summary>
    private const string MaskedHullSays = "MERIDIAN acknowledges. State your intent and hold your vector.";

    private const string Callsign = "MERIDIAN";
    private const string Destination = "Mars";

    /// <summary>A hull off the traffic schedule's own shape, on the schedule's own id space — the mask is
    /// hashed off the id, so a bench that invented ids would be reading a world the game never builds.</summary>
    private static NpcShip Hauler(string id, bool isPod = false, bool publishes = true) =>
        new(id, Callsign, "He3", "saturn", "mars", RoutePersonality.Economical,
            DepartureTime: 0, ActivationTime: 0,
            InitialState: new ShipState(new Vector2d(1e11, 0), new Vector2d(0, 30000), 0),
            Plan: new ManeuverPlan([]), EstimatedArrivalTime: 60 * 86400,
            CargoUnits: QShip.FatHoldUnits + 3, ManeuverBudget: NpcShip.DefaultManeuverBudget,
            IsPod: isPod, PublishesTimetable: publishes);

    /// <summary>The first id on each side of the mask, found by ASKING Core rather than typed — a change to
    /// the hash cannot leave this bench quietly hailing two honest haulers.</summary>
    private static (string Masked, string Honest) TheTwoIds()
    {
        string? masked = null, honest = null;
        for (int i = 0; i < 500 && (masked is null || honest is null); i++)
        {
            string id = $"npc-{i}";
            if (QShip.IsMasked(Hauler(id))) { masked ??= id; } else { honest ??= id; }
        }

        Assert.NotNull(masked);
        Assert.NotNull(honest);
        return (masked!, honest!);
    }

    // ── LAW 1 · THE TWO ANSWERS, WORD FOR WORD ───────────────────────────────────────────────────────

    /// <summary>
    /// AN HONEST MASTER TELLS YOU WHO HE IS AND WHAT HE IS DOING, AND THEN ASKS WHAT YOU WANT. The line is
    /// the owner's, character for character, with her callsign and her port taken out of the record rather
    /// than typed into the sentence.
    ///
    /// <para><b>Proven RED</b> by moving the question to the front of the shipped line ("What do you want?
    /// {0} here — bulk, bound {1}."): the retyped canon and the composed answer came back side by side.</para>
    /// </summary>
    [Fact]
    public void TheHonestMasterAnswersInAWorkingMastersOrder()
    {
        (_, string honestId) = TheTwoIds();
        Assert.Equal(HonestMerchantSays, QShipHail.AnswerTo(Hauler(honestId), Destination));
    }

    /// <summary>
    /// AND HERS IS THE RIGHT WORDS IN THE WRONG ORDER. Intent demanded before identity given, an
    /// acknowledgement where a greeting belongs, and <i>hold your vector</i> — a warship's instruction to a
    /// contact, out of the mouth of something with a hold full of bulk.
    ///
    /// <para><b>Proven RED</b> by dropping the final clause from the shipped line: the two spellings were
    /// quoted back against each other.</para>
    /// </summary>
    [Fact]
    public void TheMaskedHullAnswersLikeSomethingWithAWatchbill()
    {
        (string maskedId, _) = TheTwoIds();
        Assert.Equal(MaskedHullSays, QShipHail.AnswerTo(Hauler(maskedId), Destination));
    }

    /// <summary>
    /// AND THE TWO ARE NOT THE SAME SENTENCE — which is the whole of what makes a hail worth keying. Said
    /// separately from the two verbatim guards on purpose: those two could both be satisfied by one line if
    /// a rewrite ever collapsed them, and this one could not.
    ///
    /// <para><b>Proven RED</b> by answering the masked branch with the honest line.</para>
    /// </summary>
    [Fact]
    public void TheAnswersDiffer()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Assert.NotEqual(
            QShipHail.AnswerTo(Hauler(honestId), Destination),
            QShipHail.AnswerTo(Hauler(maskedId), Destination));
    }

    /// <summary>
    /// THE NAME AND THE PORT COME OUT OF THE RECORD, NOT OUT OF THE SENTENCE. Rename the hull and re-file
    /// her destination and the answer follows both — a line with a hard-coded callsign would pass the
    /// verbatim guards above and be a different feature.
    ///
    /// <para><b>Proven RED</b> by composing the honest line with a typed callsign in place of
    /// <c>ship.Callsign</c>: the renamed hull went on introducing herself as MERIDIAN.</para>
    /// </summary>
    [Fact]
    public void SheAnswersWithHerOwnNameAndHerOwnPort()
    {
        (string maskedId, string honestId) = TheTwoIds();
        NpcShip honest = Hauler(honestId) with { Callsign = "KESTREL" };
        NpcShip masked = Hauler(maskedId) with { Callsign = "KESTREL" };

        Assert.Equal("KESTREL here — bulk, bound Ceres. What do you want?", QShipHail.AnswerTo(honest, "Ceres"));
        Assert.Equal("KESTREL acknowledges. State your intent and hold your vector.",
            QShipHail.AnswerTo(masked, "Ceres"));
    }

    // ── LAW 2 · ASKING TWICE AGREES ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// DETERMINISTIC PER HULL, over every fat id the fixed tables can mint. The desk and the dossier read the
    /// same call, so a line that wandered between two asks would put two different sentences in front of a
    /// captain about one ship — and would make the tell noise.
    ///
    /// <para>The anti-vacuous half is the count: BOTH sentences have to turn up across the id space, or this
    /// is a determinism guard on a rule that only ever says one thing.</para>
    ///
    /// <para><b>Proven RED</b> by drawing the mask from <c>Random.Shared</c> inside <c>AnswerTo</c>: hulls
    /// changed their story between the first ask and the second.</para>
    /// </summary>
    [Fact]
    public void AskingTwiceAlwaysAgrees()
    {
        int honest = 0, masked = 0;
        for (int i = 0; i < 400; i++)
        {
            NpcShip hull = Hauler($"npc-{i}");
            string? first = QShipHail.AnswerTo(hull, Destination);
            Assert.Equal(first, QShipHail.AnswerTo(hull, Destination));
            Assert.Equal(first, QShipHail.AnswerTo(Hauler($"npc-{i}"), Destination));

            if (first == MaskedHullSays) { masked++; }
            else if (first == HonestMerchantSays) { honest++; }
            else { Assert.Fail($"npc-{i} answered with something that is neither canon line: {first}"); }
        }

        Assert.True(masked > 0, "not one hull in four hundred answered the masked way — nothing was swept.");
        Assert.True(honest > 0, "every hull answered the masked way — the ordinary answer has gone missing.");
    }

    /// <summary>
    /// AND THE ANSWER IS THE MASK'S, not a second coin of its own. Whatever <see cref="QShip.IsMasked"/> says
    /// of a hull, the sentence agrees with it — otherwise the fifth tell would read against the other four
    /// and the card would be arguing with itself.
    ///
    /// <para><b>Proven RED</b> by salting the hail's own draw instead of asking <c>IsMasked</c>: hulls whose
    /// burn and radiators closed answered like a warship, and hulls whose numbers did not answered like a
    /// hauler.</para>
    /// </summary>
    [Fact]
    public void TheSentenceAgreesWithTheNumbers()
    {
        for (int i = 0; i < 400; i++)
        {
            NpcShip hull = Hauler($"npc-{i}");
            Assert.Equal(
                QShip.IsMasked(hull) ? MaskedHullSays : HonestMerchantSays,
                QShipHail.AnswerTo(hull, Destination));
        }
    }

    // ── LAW 3 · WHO HAS NOTHING TO SAY ───────────────────────────────────────────────────────────────

    /// <summary>
    /// A POD HAS NOBODY ABOARD TO KEY A MICROPHONE — the same fact <c>EncounterRule.ComplianceOf</c> states
    /// as <c>NothingToComply</c>, said on the radio. A mass-driver pod that introduced itself would be a
    /// crew where the whole game says there is none.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>IsPod</c> branch: an unmanned canister answered the hail
    /// in a working master's voice.</para>
    /// </summary>
    [Fact]
    public void APodHasNobodyToAnswerWith()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Assert.Null(QShipHail.AnswerTo(Hauler(honestId, isPod: true), Destination));
        Assert.Null(QShipHail.AnswerTo(Hauler(maskedId, isPod: true), Destination));
    }

    /// <summary>
    /// AN OFF-BOOKS HAULER DOES NOT GIVE HER PORT AWAY FOR THE PRICE OF A HAIL. Where a secretive hauler is
    /// going is the intel economy's own goods (F6/F7, bought at the dark web); the honest line names the
    /// port, so an honest hull that files nothing says nothing and the page falls back to the sentence it
    /// has said to her since long before this issue.
    ///
    /// <para><b>A masked hull still answers</b>, filed or not — her line names no port, so there is nothing
    /// in it to give away, and the tell survives on the hulls that hide their timetable. That asymmetry is
    /// the assertion, and it is why both halves are in one guard.</para>
    ///
    /// <para><b>Proven RED</b> both ways: dropping the <c>PublishesTimetable</c> branch made a secretive
    /// hauler broadcast her destination, and moving the masked branch below it left an off-books warship
    /// with nothing to say at all.</para>
    /// </summary>
    [Fact]
    public void OffTheBooksSheKeepsHerPortAndStillKeepsHerProcedure()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Assert.Null(QShipHail.AnswerTo(Hauler(honestId, publishes: false), Destination));
        Assert.Equal(MaskedHullSays, QShipHail.AnswerTo(Hauler(maskedId, publishes: false), Destination));
    }

    // ── LAW 4 · THE GAME STILL NEVER STATES IT ───────────────────────────────────────────────────────

    /// <summary>
    /// NEITHER SENTENCE SAYS WHAT SHE IS, in any spelling a hand might reach for — and §8's reserved word is
    /// nowhere near either of them. The line is a tell because it is procedure, not because it is a
    /// confession; the moment one of them contains the word, the captain stops doing the arithmetic.
    ///
    /// <para><b>Proven RED</b> by appending " Warship out." to the masked line: <c>warship</c> was named.</para>
    /// </summary>
    [Fact]
    public void NeitherAnswerSaysWhatSheIs()
    {
        (string maskedId, string honestId) = TheTwoIds();
        string masked = QShipHail.AnswerTo(Hauler(maskedId), Destination)!;
        string honest = QShipHail.AnswerTo(Hauler(honestId), Destination)!;

        foreach (string word in new[]
                 { "q-ship", "qship", "warship", "corvette", "man-of-war", "wolf", "navy", "naval", "monolith" })
        {
            Assert.DoesNotContain(word, masked, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(word, honest, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// AND THERE ARE EXACTLY TWO SENTENCES IN THE WHOLE RULE. Two constants on the type by reflection, two
    /// quoted literals in the file by source sweep — and both sweeps compare against the canon RETYPED
    /// above, so a third line cannot arrive as a label, a plate, a prefix or a fallback without naming
    /// itself here.
    ///
    /// <para>The file sweep is the half that matters: the reflection sweep cannot see a literal written
    /// inside a method body, and a verdict does not have to be a <c>const</c> to be a verdict. §8's reserved
    /// word is checked over the whole file, docblocks included, because a docblock is where a stray one
    /// would actually get in.</para>
    ///
    /// <para><b>Proven RED</b> three ways: adding <c>public const string Plate = "Q-SHIP";</c> (the
    /// reflection half named the field), returning a typed <c>"…standing by."</c> from a fourth branch (the
    /// source half quoted the line back), and putting "monolith" in the class docblock.</para>
    /// </summary>
    [Fact]
    public void TwoSentencesAndNotAThirdWordAnywhere()
    {
        const BindingFlags Public = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance
            | BindingFlags.DeclaredOnly;

        var declared = new List<string>();
        foreach (FieldInfo f in typeof(QShipHail).GetFields(Public))
        {
            if (f.FieldType == typeof(string)) { declared.Add((string)f.GetRawConstantValue()!); }
        }
        foreach (PropertyInfo p in typeof(QShipHail).GetProperties(Public))
        {
            if (p.PropertyType == typeof(string)) { declared.Add((string?)p.GetValue(null) ?? string.Empty); }
        }

        // The braces are the record's; fill them the way the rule does and the two must be the canon two.
        Assert.Equal(2, declared.Count);
        Assert.Equal(
            new[] { HonestMerchantSays, MaskedHullSays }.OrderBy(s => s, StringComparer.Ordinal).ToArray(),
            declared.Select(s => string.Format(System.Globalization.CultureInfo.InvariantCulture, s, Callsign, Destination))
                .OrderBy(s => s, StringComparer.Ordinal).ToArray());

        // …and not one quoted literal in the executable half of the file beyond those same two.
        var spoken = new List<string>();
        bool inBlockComment = false;
        int code = 0;
        foreach ((string raw, int number) in Source().Select((l, i) => (l, i + 1)))
        {
            string line = raw;
            if (inBlockComment)
            {
                int close = line.IndexOf("*/", StringComparison.Ordinal);
                if (close < 0) { continue; }
                line = line[(close + 2)..];
                inBlockComment = false;
            }

            int open = line.IndexOf("/*", StringComparison.Ordinal);
            if (open >= 0) { inBlockComment = true; line = line[..open]; }

            int slashes = line.IndexOf("//", StringComparison.Ordinal);
            if (slashes >= 0) { line = line[..slashes]; }

            if (line.Trim().Length == 0) { continue; }
            code++;

            if (line.Contains('"') && !declared.Any(line.Contains))
            {
                spoken.Add($"line {number}: {line.Trim()}");
            }
        }

        Assert.True(code >= 20, $"only {code} line(s) of code were read — the sweep found no file.");
        Assert.True(spoken.Count == 0,
            "the two canon answers are the whole of what this rule may say. Found: " + string.Join(" | ", spoken));

        Assert.DoesNotContain("monolith", string.Join("\n", Source()), StringComparison.OrdinalIgnoreCase);
    }

    private static string[] Source()
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "src", "SpaceSails.Core")))
        {
            dir = dir.Parent;
        }

        string path = System.IO.Path.Combine(
            dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary"),
            "src", "SpaceSails.Core", "QShipHail.cs");
        return System.IO.File.ReadAllLines(path);
    }
}
