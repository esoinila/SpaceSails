using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #534 slice 2 · <b>THE CAPTAIN KEYS THE TIGHT-BEAM AND HER FILE GROWS A SENTENCE.</b>
///
/// <para>The Core bench proves the two answers. This one proves the PAGE, because tell (e) only exists if a
/// captain can put it beside the other four: the seam is the Comms desk's own 📻 Hail — the one channel he
/// has ever had for asking a hull her intentions — and what she says has to arrive on the dossier card where
/// the four numbers already are, <b>with nothing around it</b>.</para>
///
/// <para>Two hulls with the same callsign, the same manifest and mirrored geometry are hailed through the
/// page's own <c>CommsHail</c>, and then the page's own <c>DossierFor</c> is asked for both files. Slice 1's
/// sweep says the UNHAILED files differ in exactly one row; this one says the HAILED files differ in exactly
/// two, and that the second one is her own sentence and not a word the game added to it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class SheAnswersTheHailAndTheCardCarriesItTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

    /// <summary>Well inside the tight-beam's 5×10¹⁰ m reach and well outside the boarding envelope: the
    /// pre-commit range the whole read is about.</summary>
    private const double Range = 2.0e9;

    /// <summary>Past it, for the hull the captain cannot raise at all.</summary>
    private const double OutOfBeam = ActiveSensors.TightBeamMaxRangeMeters * 1.2;

    /// <summary>The canon lines, RETYPED with their braces filled from this bench's own record — never read
    /// off the constants they are checking. Her callsign is the hull's; Mars is what sol.json calls the port
    /// she is bound for, which is the composition the page has to get right.</summary>
    private const string HonestMerchantSays = "MERIDIAN here — bulk, bound Mars. What do you want?";
    private const string MaskedHullSays = "MERIDIAN acknowledges. State your intent and hold your vector.";

    // ── LAW 1 · THE HAIL, ON THE CHANNEL THE CAPTAIN ALREADY HAD ─────────────────────────────────────

    /// <summary>
    /// TWO HULLS, ONE BUTTON, TWO ANSWERS. The page's own 📻 Hail is pressed on each of them, and what comes
    /// back is the canon pair — hers composed with her name and her port off the record, and the other one's
    /// naming no port at all.
    ///
    /// <para><b>Proven RED</b> by handing <c>QShipHail.AnswerTo</c> a typed destination instead of
    /// <c>BodyName(npc.Ship.DestinationId)</c>: the honest master announced he was bound for "mars", the raw
    /// body id, and the retyped canon came back beside it.</para>
    /// </summary>
    [Fact]
    public void SheAnswersTheDesksOwnHailAndTheHaulerBesideHerDoesNot()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Map map = TwoHulls(maskedId, honestId, Range);

        Assert.Equal(MaskedHullSays, HailAndReadTheDesk(map, maskedId));
        Assert.Equal(HonestMerchantSays, HailAndReadTheDesk(map, honestId));
    }

    // ── LAW 2 · AND IT GOES ON HER FILE ──────────────────────────────────────────────────────────────

    /// <summary>
    /// THE FIFTH TELL LANDS BESIDE THE OTHER FOUR — and not before the captain has actually asked. An
    /// unhailed hull's file says nothing about how she answers, which is the same "a tell may need a
    /// completed pass" gate the two glass rows run on, with this pass flown by the captain rather than the
    /// telescope.
    ///
    /// <para><b>Proven RED</b> both ways: writing the answer into <c>_hailAnswers</c> from <c>DossierFor</c>
    /// itself put her sentence on a card nobody had hailed, and dropping the <c>_hailAnswers</c> lookup out
    /// of <c>DossierFor</c> left the card empty after the button had been pressed.</para>
    /// </summary>
    [Fact]
    public void HerFileSaysNothingUntilTheCaptainAsks()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Map map = TwoHulls(maskedId, honestId, Range);

        Assert.Null(Said(File(map, maskedId)));
        Assert.Null(Said(File(map, honestId)));

        HailAndReadTheDesk(map, maskedId);
        HailAndReadTheDesk(map, honestId);

        Assert.Equal(MaskedHullSays, Said(File(map, maskedId)));
        Assert.Equal(HonestMerchantSays, Said(File(map, honestId)));
    }

    /// <summary>
    /// AND THE HAILED FILES STILL DIFFER IN NOTHING BUT WHAT SHE IS MEASURED AT AND WHAT SHE SAID. Slice 1's
    /// sweep asserts the unhailed pair differ in exactly one row; this is the same reflection sweep after
    /// both hulls have been raised on the radio, and the answer has to be exactly two. If a future hand puts
    /// a flag, a word or a colour on her when she answers, the sweep names the row it went into.
    ///
    /// <para><b>Proven RED</b> by appending a marker to the masked hull's <c>Detail</c> when she has been
    /// hailed: the sweep came back with three rows.</para>
    /// </summary>
    [Fact]
    public void AHailAddsHerSentenceAndNothingElse()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Map map = TwoHulls(maskedId, honestId, Range);
        HailAndReadTheDesk(map, maskedId);
        HailAndReadTheDesk(map, honestId);

        object masked = File(map, maskedId);
        object honest = File(map, honestId);

        var differ = new List<string>();
        PropertyInfo[] rows = masked.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.True(rows.Length >= 13, $"the dossier has only {rows.Length} row(s) — nothing was swept.");

        foreach (PropertyInfo row in rows)
        {
            if (!Equals(row.GetValue(masked), row.GetValue(honest)))
            {
                differ.Add(row.Name);
            }
        }

        Assert.Equal(["HailAnswer", "Reading"], differ.OrderBy(n => n, StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// A HULL THE CAPTAIN CANNOT RAISE SAID NOTHING, and her file must not pretend otherwise. Out past the
    /// tight-beam's reach the desk answers with its own range notice — that is the RADIO talking, not the
    /// ship — and a notice on her card would be the game putting words in a hull's mouth.
    ///
    /// <para><b>Proven RED</b> by writing <c>_commsHailAnswer</c> into <c>_hailAnswers</c> unconditionally:
    /// "out of tight-beam range." turned up on her dossier as though she had said it.</para>
    /// </summary>
    [Fact]
    public void OutOfReachIsTheRadioTalkingAndNotTheShip()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Map map = TwoHulls(maskedId, honestId, OutOfBeam);

        foreach (string id in new[] { maskedId, honestId })
        {
            string desk = HailAndReadTheDesk(map, id);
            Assert.DoesNotContain("hold your vector", desk, StringComparison.Ordinal);
            Assert.Null(Said(File(map, id)));
        }
    }

    // ── LAW 3 · THE CARD ADDS NOTHING TO WHAT SHE SAID ───────────────────────────────────────────────

    /// <summary>
    /// NO LABEL, NO COLOUR, NO ICON — the answer is the whole of what the card has to say about it, exactly
    /// as the four numbers above it are. The markup between the measured/claimed block and the autosteal box
    /// is read for real: it must draw <c>dossier.HailAnswer</c>, and the element that draws it may not carry
    /// a Bootstrap colour, a weight, a heading or a glyph. (The verdict words themselves are swept over the
    /// same region by slice 1's <c>TheCardNeverSaysWhatSheIs</c>, which now covers this line for free.)
    ///
    /// <para><b>Proven RED</b> by drawing the line as
    /// <c>&lt;div class="text-warning"&gt;📻 she said: @answered&lt;/div&gt;</c>: the colour, the glyph and
    /// the label were all named.</para>
    /// </summary>
    [Fact]
    public void TheCardPutsNothingAroundTheAnswer()
    {
        string razor = System.IO.File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map", "DossierCard.razor")).Replace("\r\n", "\n");

        int drawn = razor.IndexOf("@if (dossier.HailAnswer", StringComparison.Ordinal);
        Assert.True(drawn > 0, "the dossier card does not draw the hail answer at all.");

        int block = razor.IndexOf("📐 Measured · claimed", StringComparison.Ordinal);
        int autosteal = razor.IndexOf("The autosteal criterion box", StringComparison.Ordinal);
        Assert.True(drawn > block && drawn < autosteal,
            "the fifth tell belongs with the other four — inside the region slice 1's verdict sweep reads.");

        int end = razor.IndexOf("@if (dossier.IsPrey)", StringComparison.Ordinal);
        Assert.True(end > drawn, "the hail block's far fence has moved — this guard is reading the wrong lines.");
        string markup = razor[drawn..end];

        foreach (string dressing in new[]
                 { "text-warning", "text-success", "text-danger", "text-info", "text-secondary",
                   "fw-bold", "badge", "📻", "⚠", "📐", "🐺" })
        {
            Assert.DoesNotContain(dressing, markup, StringComparison.Ordinal);
        }
    }

    // ─────────────────────────── the bench ───────────────────────────

    /// <summary>The first id on each side of the mask, asked of Core rather than typed.</summary>
    private static (string Masked, string Honest) TheTwoIds()
    {
        string? masked = null, honest = null;
        for (int i = 0; i < 500 && (masked is null || honest is null); i++)
        {
            string id = $"npc-{i}";
            if (QShip.IsMasked(HullNamed(id))) { masked ??= id; } else { honest ??= id; }
        }

        Assert.NotNull(masked);
        Assert.NotNull(honest);
        return (masked!, honest!);
    }

    private static NpcShip HullNamed(string id) =>
        new(id, "MERIDIAN", "He3", "saturn", "mars", RoutePersonality.Economical,
            DepartureTime: 0, ActivationTime: 0,
            InitialState: new ShipState(Vector2d.Zero, Vector2d.Zero, 0),
            Plan: new ManeuverPlan([]), EstimatedArrivalTime: 60 * 86400,
            CargoUnits: QShip.FatHoldUnits + 3, ManeuverBudget: NpcShip.DefaultManeuverBudget, IsPod: false);

    /// <summary>Press the desk's own 📻 Hail on one hull and read what the desk shows back.</summary>
    private static string HailAndReadTheDesk(Map map, string id)
    {
        object npc = ((object[])Get(map, "_npcStates")!)
            .First(n => ((NpcShip)Member(n, "Ship")!).Id == id);
        Invoke(map, "CommsHail", npc);
        return (string?)Get(map, "_commsHailAnswer") ?? string.Empty;
    }

    private static string? Said(object dossier) =>
        (string?)dossier.GetType().GetProperty("HailAnswer")!.GetValue(dossier);

    /// <summary>Two hulls mirrored about a captain parked off Mars — same range, same speed, opposite
    /// beams — so every geometric row of the two dossiers is equal by construction.</summary>
    private static Map TwoHulls(string maskedId, string honestId, double range)
    {
        var map = new Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_npcSimulator", new Simulator(ephemeris, TrafficSchedule.NpcTimeStep));

        Vector2d here = ephemeris.Position("mars", 0) + new Vector2d(0, 3.0e9);
        Set(map, "_ship", new ShipState(here, new Vector2d(0, 24000), 0));

        Type stateType = typeof(Map).GetNestedType("NpcState")!;
        Array hulls = Array.CreateInstance(stateType, 2);
        hulls.SetValue(Hull(stateType, maskedId, here + new Vector2d(range, 0)), 0);
        hulls.SetValue(Hull(stateType, honestId, here - new Vector2d(range, 0)), 1);
        Set(map, "_npcStates", hulls);
        return map;
    }

    private static object Hull(Type stateType, string id, Vector2d position)
    {
        object npc = Activator.CreateInstance(stateType)!;
        var velocity = new Vector2d(0, 24000);
        var state = new ShipState(position, velocity, 0);
        stateType.GetField("Ship")!.SetValue(npc, HullNamed(id));
        stateType.GetField("State")!.SetValue(npc, state);
        stateType.GetField("Active")!.SetValue(npc, true);
        stateType.GetField("CurrentlyObserved")!.SetValue(npc, true);
        stateType.GetField("LastObservation")!.SetValue(npc, new Observation(id, 0, position, velocity));
        return npc;
    }

    /// <summary>The page's own dossier for one hull — the shipping method, not a stand-in.</summary>
    private static object File(Map map, string id) =>
        Invoke(map, "DossierFor", id) ?? throw new InvalidOperationException($"no dossier for {id}");

    private static object? Member(object o, string name)
    {
        Type t = o.GetType();
        return t.GetProperty(name)?.GetValue(o)
            ?? (t.GetField(name) ?? throw new InvalidOperationException($"no member {name} on {t.Name}")).GetValue(o);
    }

    private static object? Get(object o, string field) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .GetValue(o);

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .SetValue(o, value);

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "scenarios")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary");
    }
}
