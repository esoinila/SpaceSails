using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #238 · <b>THERE SHE IS, AND GOT IT — on the screen.</b>
///
/// <para>Owner, mid-car-hunt: <i>"Is there a big pop-up when we find the car in the scans? It is a kind of
/// mission event like when we get paid?"</i> The reveal was a marker and a checklist tick. And at the other
/// end of the same job, proven live: he prised the roadster's wallet mid-coast and had to ASK whether the
/// loot was aboard, because the only lasting change on the screen was the Captain chip's next line.</para>
///
/// <para>The event SHAPE is measured in Core (<c>ThereSheIsTests</c> there). What is measured here is what
/// Core cannot see and what the owner actually complained about: that the card comes up, that the warp is
/// yanked out from under it so it cannot slip past at 10,000×, that <c>show me</c> moves the camera, that
/// the same target never gets two moments, that the pickup fires one at all, that the bird belongs to the
/// car and to nothing else, and that the receipt outlives the card.</para>
///
/// <para>And one source guard with a register in it: EVERY quest transition in the client is classified as a
/// press or as something that happens by itself, and every one that happens by itself either goes through
/// this seam or names — here, in writing — the louder surface that already covers it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ThereSheIsTests
{
    /// <summary>A world where she is still HIDDEN: the fetch job injected at its first stage, which seeds
    /// the transponder fix and charts nothing. The premise is asserted in every guard that uses it rather
    /// than trusted — a world that had already revealed her is this repo's fifth named bug class wearing a
    /// URL.</summary>
    private const string BeforeTheScan = "/map?fetch=intel";

    // ── THE REVEAL ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE FIND IS A MOMENT, AND THE WARP LETS GO OF IT. The owner's question, answered in three assertions:
    /// the card is up, it says his sentence, and the sim is back at 1× so he is looking at it.
    ///
    /// <para><b>Proven RED</b> three ways, one per assertion: delete the <c>TheMomentTheyEarnedIt</c> call
    /// from <c>RevealBody</c> (no card — the shipped behaviour, which is the bug); compose the headline from
    /// <c>BodyName</c> instead of <c>Derelict.RoadsterRevealName</c> (the card says "Derelict Roadster"); and
    /// delete the two warp lines from the seam (the sim is still at 10,000× with the card on the glass,
    /// which is the reveal sliding past in a third of a second with a pop-up nobody saw).</para>
    /// </summary>
    [Fact]
    public async Task TheRevealRaisesTheBeatAndPullsTheWarpBackToOne()
    {
        DeskBench bench = await Booted(BeforeTheScan);
        Assert.True(IsHidden(bench, Derelict.RoadsterBodyId), "premise: she is off the charts before the scan");

        bench.Poke("Warp", 10_000);
        Chart(bench, Derelict.RoadsterBodyId);

        MissionMoment beat = TheBeat(bench);
        Assert.Equal("🔭 THERE SHE IS — the roadster, sunward of Mars", beat.Headline);
        Assert.Equal(1, (int)bench.Peek("Warp")!);
        Assert.Equal(1, (int)bench.Peek("_effectiveWarp")!);
    }

    /// <summary>
    /// THE BIRD SAW HER FIRST, and it is the car's line. Owner, endorsing his own idea: <i>"Love that
    /// thought of Parrot reacting to the glint from telescope."</i>
    ///
    /// <para><b>Proven RED</b> by passing <c>Parrot.Squawk.CarFound</c> in <c>RevealMomentFor</c>: the bubble
    /// and the card both say "You found your CAAAR!" four beats before the punchline is due, and this fails
    /// on both.</para>
    /// </summary>
    [Fact]
    public async Task TheBirdSquawksTheGlintLineWhenSheIsCharted()
    {
        DeskBench bench = await Booted(BeforeTheScan);
        Chart(bench, Derelict.RoadsterBodyId);

        Assert.Equal("DUDE. THERE. Is. The CAR!", TheBeat(bench).ParrotLine);
        Assert.Equal("DUDE. THERE. Is. The CAR!", bench.Peek("_parrotSquawk"));
    }

    /// <summary>
    /// …AND THE BIRD BELONGS TO THE CAR. A hidden body that is not the roadster charts through the same
    /// seam, gets the same card, and gets no squawk at all — because the payoff line is the car gag's and
    /// the joke does not survive being general (the same ruling #233 made about the set-up).
    ///
    /// <para><b>Proven RED</b> by defaulting the parrot argument in <c>RevealMomentFor</c>'s second arm to
    /// <c>CarGlimpsed</c>: a secret station starts shouting about a car.</para>
    /// </summary>
    [Fact]
    public async Task OnlyTheCarGetsTheBirdsPayoffLine()
    {
        DeskBench bench = await Booted(BeforeTheScan);
        string other = SomeOtherHiddenBody(bench);

        Chart(bench, other);

        MissionMoment beat = TheBeat(bench);
        Assert.Null(beat.ParrotSquawk);
        Assert.Null(beat.ParrotLine);
        Assert.StartsWith("🔭 THERE SHE IS — ", beat.Headline, StringComparison.Ordinal);
        Assert.Null(bench.Peek("_parrotSquawk"));
    }

    /// <summary>
    /// ONE TARGET, ONE MOMENT, FOREVER. A second reveal of the same body — a repeat scan, a save reloaded
    /// over a live session, any future caller that is not the telescope — raises nothing. Asserted through
    /// the QUEUE as well as the card, because a beat that got queued behind a dismissed one would come up
    /// afterwards and be the same bug with a delay on it.
    ///
    /// <para><b>Proven RED</b> by deleting the <c>_momentsRaisedFor</c> latch from the seam: the second
    /// charting queues a second identical card.</para>
    /// </summary>
    [Fact]
    public async Task TheSameTargetNeverGetsASecondMoment()
    {
        DeskBench bench = await Booted(BeforeTheScan);

        Chart(bench, Derelict.RoadsterBodyId);
        bench.CallOnTheDispatcher("DismissMissionMoment");
        Assert.Null(bench.Peek("_missionMoment"));

        // Un-latch the reveal itself so the SEAM is what is being measured and not RevealBody's own set.
        ((HashSet<string>)bench.Peek("_revealedBodyIds")!).Remove(Derelict.RoadsterBodyId);
        Chart(bench, Derelict.RoadsterBodyId);

        Assert.Null(bench.Peek("_missionMoment"));
        Assert.Empty((System.Collections.IEnumerable)bench.Peek("_missionMomentQueue")!);
    }

    /// <summary>
    /// …AND THE BOOT CHEATS THAT PARK YOU ALONGSIDE HER STILL SAY NOTHING. <c>?start=wreck</c> begins
    /// co-moving beside the roadster and charts her quietly; a found-her fanfare for a body you were sitting
    /// on at the title screen is the joke played on the wrong person.
    ///
    /// <para><b>Proven RED</b> by raising the beat unconditionally in <c>RevealBody</c> rather than inside
    /// the <c>announce</c> branch: the card is on the glass before the player has touched anything.</para>
    /// </summary>
    [Fact]
    public async Task AQuietChartingRaisesNoBeat()
    {
        DeskBench bench = await Booted("/map?start=wreck");

        Assert.False(IsHidden(bench, Derelict.RoadsterBodyId), "premise: the start charted her");
        Assert.Null(bench.Peek("_missionMoment"));
    }

    // ── SHOW ME ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>SHOW ME PUTS THE NEW MARKER UNDER THE CAMERA</b>, and it is pressed the way a player presses it:
    /// through the renderer's own event channel at the handler id the tree wrote, not by calling a method
    /// whose name begins with Show. A label wired to nothing is exactly how a card ends up with a button
    /// that does not work.
    ///
    /// <para>The two follows are let go first, or the very next frame would snap the view back to the ship
    /// and the press would read as broken — so both are asserted, not just the centre.</para>
    ///
    /// <para><b>Proven RED</b> two ways: delete the <c>_camera.CenterOn</c> line (the centre never moves);
    /// and delete the two <c>Follow…</c> lines (the centre moves and the follow flags are still set, which
    /// is the press that undoes itself 200 ms later).</para>
    /// </summary>
    [Fact]
    public async Task ShowMePutsTheNewMarkerUnderTheCamera()
    {
        DeskBench bench = await Booted(BeforeTheScan);
        bench.Poke("FollowShip", true);
        bench.Poke("_followDest", true);
        Chart(bench, Derelict.RoadsterBodyId);

        DeskBench.Painted painted = await bench.RenderAsync();
        ulong press = TheWayOut(painted, MissionMoments.ShowMeFace);
        Assert.True(press != 0, "the card drew no `show me` press at all");
        await bench.PressAsync(press);

        Assert.Null(bench.Peek("_missionMoment"));
        Assert.False((bool)bench.Peek("FollowShip")!);
        Assert.False((bool)bench.Peek("_followDest")!);

        var camera = (Camera)bench.Peek("_camera")!;
        Vector2d her = TheWorld(bench).Position(Derelict.RoadsterBodyId, (double)bench.Peek("SimTime")!);
        Assert.True((camera.Center - her).Length < 1.0,
            $"the camera is {(camera.Center - her).Length:0} m off the marker `show me` promised to show");
    }

    /// <summary>
    /// AND THE CARD CAN ALWAYS BE CLOSED (#824 · the general UI law). The dismiss is pressed the same way,
    /// and the register next door (<see cref="EveryPopUpCanBeDismissedTests"/>) holds the whole family to
    /// the same rule through its own root class — this is the beat's own copy of the question, asked where
    /// the beat is raised by the shipping reveal rather than by a field poke.
    ///
    /// <para><b>Proven RED</b> by wiring <c>OnClose</c> to nothing: the shell's own design-fault check trips
    /// first, which is the stronger failure — it names the surface.</para>
    /// </summary>
    [Fact]
    public async Task TheBeatCanBeClosed()
    {
        DeskBench bench = await Booted(BeforeTheScan);
        Chart(bench, Derelict.RoadsterBodyId);

        DeskBench.Painted painted = await bench.RenderAsync();
        ulong press = TheWayOut(painted, MissionMoments.DismissFace);
        Assert.True(press != 0, "the card drew no way out");
        await bench.PressAsync(press);

        Assert.Null(bench.Peek("_missionMoment"));
        Assert.Empty(bench.EscapedPastTheGate);
    }

    /// <summary>
    /// A PICKUP OFFERS NO `show me`, because the ship is alongside the thing it just prised and a camera
    /// jump would be a button that moves nothing. It still closes.
    ///
    /// <para><b>Proven RED</b> by having <c>MissionMoments.Pickup</c> carry a body id: the press appears on
    /// a card whose subject is in the captain's own pocket.</para>
    /// </summary>
    [Fact]
    public async Task ThePickupCardOffersNowhereToLook()
    {
        DeskBench bench = await Booted(BeforeTheScan);
        bench.CallOnTheDispatcher("TheMomentTheyEarnedIt",
            MissionMoments.Pickup(Derelict.WalletBetweenTheSeats, Parrot.Squawk.CarFound));

        DeskBench.Painted painted = await bench.RenderAsync();
        Assert.Equal(0ul, TheWayOut(painted, MissionMoments.ShowMeFace));
        Assert.True(TheWayOut(painted, MissionMoments.DismissFace) != 0, "the card drew no way out");
    }

    // ── THE PICKUP, ON ITS OWN EDGE ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE BEAT THE OWNER HAD TO ASK FOR.</b> The ship is alongside the roadster with the fetch job
    /// Active; one tick of the pickup check and the wallet is aboard — and it SAYS SO, in his words.
    ///
    /// <para>Driven through <c>CheckFetchPickup</c>, the method the sim tick calls, in a world the cheat
    /// puts alongside her — so what is proven is the shipping edge and not a hand-set field.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the <c>TheMomentTheyEarnedIt</c> call from
    /// <c>CheckFetchPickup</c>: the quest still advances, the banner still fires, and the card never comes
    /// up — which is the session the owner reported, exactly.</para>
    /// </summary>
    [Fact]
    public async Task ThePrisedWalletRaisesTheBeatAndTheBirdLandsThePunchline()
    {
        DeskBench bench = await Booted("/map?start=wreck&fetch=active");

        bench.CallOnTheDispatcher("CheckFetchPickup");

        MissionMoment beat = TheBeat(bench);
        Assert.Equal("💾 GOT IT — the wallet, from between the seats", beat.Headline);
        Assert.Equal(MissionMomentKind.Pickup, beat.Kind);
        Assert.Equal("You found your CAAAR!", beat.ParrotLine);
        Assert.Equal("You found your CAAAR!", bench.Peek("_parrotSquawk"));
    }

    /// <summary>
    /// THE TWIN PRISES LOUDLY TOO. One car in four has photographs in it instead of a wallet; a twin that
    /// came aboard in silence while its sibling got a moment would be the owner asking "did that just
    /// happen?" of the one car in four that carries the whole arc.
    ///
    /// <para>The card names the object by its CANON name and stops — what is on the chip is still only ever
    /// seen at 🔍 in the satchel, which is #233's own ruling and is asserted here so this beat cannot become
    /// the leak.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the call from <c>TheChipComesAboard</c>.</para>
    /// </summary>
    [Fact]
    public async Task ThePrisedChipRaisesTheBeatWithoutSayingWhatIsOnIt()
    {
        DeskBench bench = await Booted("/map?start=wreck&fetch=active-chip");

        bench.CallOnTheDispatcher("CheckFetchPickup");

        MissionMoment beat = TheBeat(bench);
        Assert.Equal("💾 GOT IT — the data chip, from between the seats", beat.Headline);
        Assert.DoesNotContain("Photograph", beat.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(CompromisingChip.FindId, beat.Headline, StringComparison.Ordinal);
    }

    // ── THE RECEIPT ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE CARD IS GONE IN TWO SECONDS; THE LINE IS NOT. "Did that just happen?" is a question asked
    /// AFTERWARDS, so the beat files a ledger receipt under its own section at the Captain's desk — and the
    /// section is its own and not the autopilot's, because a reveal is not a handback.
    ///
    /// <para><b>Proven RED</b> by deleting the <c>_missionMomentLedger.Insert</c> line: the card still comes
    /// up and there is nothing to go back to, which is half the complaint left unfixed.</para>
    /// </summary>
    [Fact]
    public async Task TheBeatLeavesAReceiptTheCaptainCanGoBackTo()
    {
        DeskBench bench = await Booted(BeforeTheScan);
        Chart(bench, Derelict.RoadsterBodyId);
        bench.CallOnTheDispatcher("DismissMissionMoment");

        object[] tips = ((System.Collections.IEnumerable)bench.Call("LedgerTips")!).Cast<object>().ToArray();
        List<(string Title, string Line)> rows = tips.Select(Row).ToList();

        Assert.Contains(rows, r => r.Title == MissionMoments.LedgerTitle
                                   && r.Line == "🔭 THERE SHE IS — the roadster, sunward of Mars");
        Assert.DoesNotContain(rows, r => r.Title == "🛰 Autopilot"
                                         && r.Line.Contains("THERE SHE IS", StringComparison.Ordinal));
    }

    // ── THE REGISTER: WHAT HAPPENS BY ITSELF ──────────────────────────────────────────────────────────

    /// <summary>How a quest transition is REACHED. The whole of what #238 item 2 turns on.</summary>
    private enum Trigger
    {
        /// <summary>The captain pressed something and this is its answer: an accept, a hand-off at a table,
        /// a keypad, a shovel, a clamp. The press IS the acknowledgement; nothing is owed.</summary>
        APress,

        /// <summary>Nobody pressed anything about THIS: a range closed, or a tick noticed. Owner's rule —
        /// <i>"if the player can ask 'did that just happen?', the game owed them a moment."</i></summary>
        ByItself,
    }

    /// <param name="Method">The client method the <c>AdvanceMission</c> call sits in.</param>
    /// <param name="How">Press or by-itself.</param>
    /// <param name="Covered">For a by-itself row: the seam it calls, or the LOUDER surface that already
    /// covers it. Empty on a press row.</param>
    private sealed record Transition(string Method, Trigger How, string Covered = "");

    /// <summary>
    /// #238 · <b>EVERY QUEST TRANSITION IN THE CLIENT, CLASSIFIED.</b> The audit the owner's second report
    /// asked for, written down rather than remembered: <i>"Every quest-state transition that happens by
    /// PROXIMITY or AUTOMATION gets a beat."</i>
    ///
    /// <para>A press row is a transition the captain reached by pressing the thing it is about — an offer
    /// card, a person at a table, a keypad, a shovel, a clamp — and the press is its own receipt. A
    /// by-itself row is one nobody pressed anything about, and each of those names what covers it: the new
    /// seam, or the louder member of this same family that is already on the glass when it fires.</para>
    /// </summary>
    private static readonly Transition[] TheTransitions =
    [
        // ── The two the owner caught: a range closed while he was looking somewhere else ──────────────
        new("CheckFetchPickup", Trigger.ByItself, "TheMomentTheyEarnedIt"),
        new("TheChipComesAboard", Trigger.ByItself, "TheMomentTheyEarnedIt"),

        // ── Two more that happen by themselves, and are already the loudest thing on the screen ───────
        new("CompleteHuntQuests", Trigger.ByItself,
            "the kill itself — a holed or boarded prey is a combat surface, not a silent tick"),
        new("CompleteBoundCargoRunQuests", Trigger.ByItself,
            "PayCompletedQuests — the #185 celebration card, the family's LOUDEST member, in the same call"),

        // ── Presses: the captain reached each of these by pressing the thing it is about ──────────────
        new("AcceptOffer", Trigger.APress),
        new("DeliverFetch", Trigger.APress),
        new("DeliverCrack", Trigger.APress),
        new("CompleteCargoRunQuests", Trigger.APress),
        new("CompleteFetchCacheFor", Trigger.APress),
        new("PayCompletedQuests", Trigger.APress),
        new("LiftStash", Trigger.APress),
        new("HandTheChipBack", Trigger.APress),
        new("SellTheChipToTheFence", Trigger.APress),
        new("TheChipGoesInTheChest", Trigger.APress),
        new("YouFindWhatSheAskedFor", Trigger.APress),
        new("YouComeBackAndTellHer", Trigger.APress),
        new("SubmitPin", Trigger.APress),
    ];

    /// <summary>
    /// THE TABLE IS COMPLETE, AND IT IS COMPLETE AGAINST THE SOURCE. Every method in the client that calls
    /// the one quest writer is in the register above, and every row in the register is a method that still
    /// calls it. A transition added tomorrow fails here by name, which is the moment somebody has to decide
    /// whether the captain would have to ask about it.
    ///
    /// <para><b>Proven RED</b> both ways: delete a row and the sweep names the unclassified method; add a
    /// row for a method that does not call the writer and the sweep names the stale one.</para>
    /// </summary>
    [Fact]
    public void EveryQuestTransitionInTheClientIsClassified()
    {
        HashSet<string> inSource = MethodsThatAdvanceAMission();
        HashSet<string> registered = TheTransitions.Select(t => t.Method).ToHashSet(StringComparer.Ordinal);

        Assert.True(inSource.SetEquals(registered),
            $"unclassified: [{string.Join(", ", inSource.Except(registered).Order())}]; "
            + $"stale rows: [{string.Join(", ", registered.Except(inSource).Order())}]");
    }

    /// <summary>
    /// AND EVERY TRANSITION THAT HAPPENS BY ITSELF GOES THROUGH THE ONE SEAM — or names, in the register
    /// above, the louder surface that already covers it. Read off the SOURCE: the seam call has to be in the
    /// same method body as the transition, because a beat raised somewhere else is a beat that fires on a
    /// different edge.
    ///
    /// <para><b>Proven RED</b> by deleting <c>TheMomentTheyEarnedIt</c> from <c>CheckFetchPickup</c> — the
    /// shipped behaviour the owner reported — which fails here naming the method and the row.</para>
    /// </summary>
    [Fact]
    public void EveryTransitionThatHappensByItselfGoesThroughTheOneSeamOrSaysWhatCoversIt()
    {
        const string Seam = "TheMomentTheyEarnedIt";
        var missing = new List<string>();

        foreach (Transition t in TheTransitions.Where(t => t.How == Trigger.ByItself))
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Covered),
                $"{t.Method} happens by itself and the register says nothing about what covers it.");

            if (t.Covered != Seam)
            {
                continue;   // covered by a louder surface, named in the register and read by a person
            }

            if (!BodyOf(t.Method).Contains(Seam + "(", StringComparison.Ordinal))
            {
                missing.Add(t.Method);
            }
        }

        Assert.True(missing.Count == 0,
            $"these advance a quest by themselves and raise no beat: {string.Join(", ", missing)}");
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Boot a world and DRAW IT ONCE. The first paint is what assigns the page's render handle, and
    /// every beat this file raises ends in <c>StateHasChanged</c> — so a guard that drove the page before the
    /// first render would fail inside Blazor rather than on its own assertion, which is a failure that says
    /// nothing about the feature.</summary>
    private static async Task<DeskBench> Booted(string url)
    {
        DeskBench bench = await DeskBench.BootAsync(url);
        await bench.RenderAsync();
        return bench;
    }

    /// <summary>Chart a hidden body through the shipping reveal — the method the completed/mid-pass scan
    /// calls (Map.Npc.Tasking.OnAreaScanCovered), with the announcing reveal's own arguments.</summary>
    private static void Chart(DeskBench bench, string bodyId) =>
        bench.CallOnTheDispatcher("RevealBody", bodyId, (string)bench.Call("WreckRevealMessage", bodyId)!, true);

    private static MissionMoment TheBeat(DeskBench bench) =>
        (MissionMoment)(bench.Peek("_missionMoment")
                        ?? throw new InvalidOperationException("no beat is on the glass — the card never came up."));

    private static bool IsHidden(DeskBench bench, string bodyId) => (bool)bench.Call("IsBodyHidden", bodyId)!;

    private static ICelestialEphemeris TheWorld(DeskBench bench) => (ICelestialEphemeris)bench.Peek("_ephemeris")!;

    /// <summary>A hidden body that is NOT the roadster.
    ///
    /// <para>The scenario's own hidden set is the roadster and nothing else today, so the second target is
    /// MADE here — a real charted body of the built world, moved into the same <c>_hiddenBodyIds</c> set the
    /// scenario loads. That is what the file is: data. Taking a live body and hiding it is the honest way to
    /// stand up the arm that #223's caches and the secret stations will arrive through, and it fails loudly
    /// if the world stops having one rather than passing on an empty reveal (the fifth named bug class).</para>
    /// </summary>
    private static string SomeOtherHiddenBody(DeskBench bench)
    {
        var hidden = (HashSet<string>)bench.Peek("_hiddenBodyIds")!;
        string? other = hidden.Where(id => id != Derelict.RoadsterBodyId).Order(StringComparer.Ordinal)
            .FirstOrDefault();
        if (other is not null)
        {
            return other;
        }

        CelestialBody? candidate = TheWorld(bench).Bodies
            .FirstOrDefault(b => b.Id != Derelict.RoadsterBodyId && b.ParentId is not null && b.IsHaven);
        Assert.False(candidate is null, "the built world has no second body to hide — the general arm is untestable.");
        hidden.Add(candidate!.Id);
        Assert.True(IsHidden(bench, candidate.Id), "premise: the second target really is off the charts");
        return candidate.Id;
    }

    /// <summary>The press-id of the card's way out reading <paramref name="face"/>, or 0 if it drew none.
    /// Looked up inside the beat's own subtree, so a button belonging to a panel behind it cannot answer.</summary>
    private static ulong TheWayOut(DeskBench.Painted painted, string face) =>
        painted.Root.SelfAndDescendants()
            .Where(n => n.HasClass("mission-moment"))
            .SelectMany(card => card.SelfAndDescendants())
            .Where(n => n.Element == "button" && !n.Hidden
                        && string.Equals(n.Spoken.Trim(), face, StringComparison.Ordinal)
                        && n.Handlers.ContainsKey("onclick"))
            .Select(n => n.Handlers["onclick"])
            .FirstOrDefault();

    private static (string Title, string Line) Row(object tip)
    {
        Type t = tip.GetType();
        string title = (string)t.GetProperty("Title")!.GetValue(tip)!;
        var lines = (IEnumerable<string>)t.GetProperty("Lines")!.GetValue(tip)!;
        return (title, lines.FirstOrDefault() ?? "");
    }

    // ── Reading the client's own source ───────────────────────────────────────────────────────────────

    private static readonly Regex TheWriter = new(@"AdvanceMission\(", RegexOptions.Compiled);

    private static readonly Regex AMethodHead = new(
        @"^\s{4}(?:private|internal|public|protected)[^;=]*?\b(\w+)\s*\([^;]*$|^\s{4}(?:private|internal|public|protected)[^;=]*?\b(\w+)\s*\(",
        RegexOptions.Compiled);

    /// <summary>Every method in the client whose body contains an <c>AdvanceMission(</c> call, read off the
    /// source. Methods are cut on the four-space signature indent every member of these partials is written
    /// at — the same reading the suite's other source guards take of this page.</summary>
    private static HashSet<string> MethodsThatAdvanceAMission()
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach ((string method, string body) in EveryMethodInThePages())
        {
            if (TheWriter.IsMatch(body) && method != "AdvanceMission")
            {
                found.Add(method);
            }
        }

        return found;
    }

    private static string BodyOf(string method)
    {
        foreach ((string name, string body) in EveryMethodInThePages())
        {
            if (name == method)
            {
                return body;
            }
        }

        throw new InvalidOperationException($"no method named {method} in the client's pages — the register is stale.");
    }

    private static IEnumerable<(string Method, string Body)> EveryMethodInThePages()
    {
        foreach (string file in Directory.EnumerateFiles(
                     Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "*.cs",
                     SearchOption.AllDirectories))
        {
            string[] lines = File.ReadAllLines(file);
            string? current = null;
            var body = new List<string>();

            foreach (string line in lines)
            {
                Match head = AMethodHead.Match(line);
                if (head.Success && !line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    if (current is not null)
                    {
                        yield return (current, string.Join('\n', body));
                    }

                    current = head.Groups[1].Success ? head.Groups[1].Value : head.Groups[2].Value;
                    body.Clear();
                }

                body.Add(line);
            }

            if (current is not null)
            {
                yield return (current, string.Join('\n', body));
            }
        }
    }

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }

            at = at.Parent;
        }

        throw new InvalidOperationException("could not find the repository root from the test assembly.");
    }
}
