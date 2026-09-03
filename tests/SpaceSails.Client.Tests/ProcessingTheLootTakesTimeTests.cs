using System;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #696 · THE DARKROOM, CLIENT SIDE. Owner, mid-run: <i>"we take time to process the loot."</i>
///
/// <para><b>Why source-shape.</b> The reasoning #680, #688, #690 and #697 already wrote down: what is under
/// test here is not a value a Core function returns — that half is
/// <c>WeTakeTimeToProcessTheLootTests</c> — it is which press starts a clock, which state transitions the
/// clock is allowed to perform on its own, which system is permitted to interrupt it, and above all what the
/// darkroom is FORBIDDEN to mention.</para>
///
/// <para><b>The one that matters most is <see cref="TheDarkroomHasNeverHeardOfTheTank"/>.</b> The owner's
/// ruling is that the air prices the location <i>for free</i>: a hold on the regolith burns tank because
/// time burns tank, and a hold in a shelter costs nothing because the drain is already off there. That is
/// only true while nothing in the hold knows a tank exists. A single <c>ex.AirSeconds -=</c> in
/// <c>BeginProcessing</c> would produce a build that looks correct out on the regolith and charges a captain
/// for standing in a pressurised refuge — the fifth named bug class exactly: a guard handed the wrong world.
/// The naive always-burn shape was transcribed in and watched go red.</para>
///
/// <para><b>Red.</b> Every test in this file was run against the three shipped client files as they stood at
/// cbffb36 (the build that shipped #697), with the new Core class present but unused: <b>12 of 12 red</b>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ProcessingTheLootTakesTimeTests
{
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
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Pages(string file) =>
        MapMarkup.Read(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    /// <summary>#870 · The sim page is nine partials by subject now, so "the sim" a guard reads over is all
    /// of them — exactly the text it read out of one file before the split.</summary>
    private static string Sim() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Sim*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>#870 · The surface page is fifteen partials by subject now, so "the surface" a guard counts
    /// over is all of them — exactly the text it read out of one file before the split.</summary>
    private static string Surface() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Surface*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>One method's body, cut at the next member declaration — the idiom #680's guards introduced
    /// and every satchel guard since has used.</summary>
    private static string Method(string file, string signature)
    {
        string src = Pages(file);
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{file} no longer has `{signature}` where this guard can read it.");
        int end = src.IndexOf("\n    private ", at + 1, StringComparison.Ordinal);
        return src[at..(end > at ? end : src.Length)];
    }

    /// <summary>The satchel block of Map.razor — the cut #688, #690 and #697 all make.</summary>
    private static string SatchelBlock()
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("@if (_showSatchel)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the satchel block this guard knows how to find.");
        int end = razor.IndexOf("_viewObject is { }", start, StringComparison.Ordinal);
        Assert.True(end > start, "Map.razor's satchel block no longer ends where this guard expects.");
        return razor[start..end];
    }

    /// <summary>Code without prose. A law about what a method may MENTION has to be about its code, or the
    /// comment explaining why two systems never meet would itself break the rule.</summary>
    private static string CodeOnly(string source)
    {
        var kept = new System.Text.StringBuilder();
        foreach (string line in source.Split('\n'))
        {
            int slashes = line.IndexOf("//", StringComparison.Ordinal);
            kept.Append(slashes >= 0 ? line[..slashes] : line).Append('\n');
        }
        return kept.ToString();
    }

    // ── THE PRESS STARTS A CLOCK ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void LeavingADocumentStartsAHoldInsteadOfFilingItWhereItStands()
    {
        // Owner's ruling, first half: filing a document's gist on leave is seconds of standing still. Before
        // #696 this method did the whole job inline — ground, pocket, book, sentence — in one frame.
        string leave = Method("Map.Surface.Satchel.cs", "private void LeaveItem(Core.Satchel.Item item)");

        Assert.Contains("BeginProcessing(", leave, StringComparison.Ordinal);
        Assert.Contains("Core.Processing.Work.File", leave, StringComparison.Ordinal);

        // The branch is Core's own question and not a kind list retyped here: GistOf is what decides a thing
        // is a DOCUMENT, and a second answer would put a clock in front of a handful of rounds the first
        // time a kind moved compartment (#688).
        Assert.Contains("LeftBehind.GistOf(", leave, StringComparison.Ordinal);

        // And the press itself no longer moves anything. The instant-drop half lives in SetItDown, which is
        // what the far end of the hold calls — same code, later.
        string code = CodeOnly(leave);
        Assert.DoesNotContain("Satchel.Remove(", code, StringComparison.Ordinal);
        Assert.DoesNotContain("FileNote(", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Ground.Leave(", code, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadingAPaperAsAClueCostsTheSameSeconds()
    {
        // Owner's ruling, second half: "Same duration on reading-a-paper-as-a-clue — deciding a paper is a
        // map takes the same standing-still seconds." A game that charged for filing and gave the clue read
        // away free would teach the captain to read everything on the spot and file nothing, which deletes
        // the decision the cost model was built to create.
        string tryIt = Method("Map.Surface.Darkroom.cs", "private void TryItem(Core.Satchel.Item item)");

        Assert.Contains("BeginProcessing(", tryIt, StringComparison.Ordinal);
        Assert.Contains("Core.Processing.Work.Read", tryIt, StringComparison.Ordinal);
        Assert.Contains("SatchelTry.Target.Tracker", tryIt, StringComparison.Ordinal);

        // The clock is in front of THIS press and not inside the shared ending — a hold bolted into
        // TheOfferIsAnswered would put twenty seconds in front of a wallet fan at a door as well (#697).
        string ending = CodeOnly(Method("Map.Surface.Darkroom.cs", "private void TheOfferIsAnswered("));
        Assert.DoesNotContain("BeginProcessing(", ending, StringComparison.Ordinal);
    }

    [Fact]
    public void NothingIsSpentUntilTheFarEnd()
    {
        // The whole of "an interruption loses nothing", made structural rather than promised: the document is
        // still in the sleeve for the entire hold, because the press that started it removed nothing, filed
        // nothing and put nothing on the ground. There is no clean-up path to get wrong.
        string begin = CodeOnly(Method("Map.Surface.Darkroom.cs", "private void BeginProcessing("));

        Assert.DoesNotContain("Satchel.Remove(", begin, StringComparison.Ordinal);
        Assert.DoesNotContain("FileNote(", begin, StringComparison.Ordinal);
        Assert.DoesNotContain("Ground.Leave(", begin, StringComparison.Ordinal);
        Assert.DoesNotContain("GrantLabLead(", begin, StringComparison.Ordinal);

        // One pair of hands: a second press while a hold runs is refused and SAID (#603 — a control that does
        // nothing and says nothing is indistinguishable from a bug). This is also what stops a document being
        // filed twice by a captain who reopens the pocket and presses the row again.
        Assert.Contains("AlreadyBusyLine", begin, StringComparison.Ordinal);

        // And a shovel already in the ground is the same objection in a different glyph. Every slow thing on
        // this surface draws the ONE progress bar (#562), so two at once is a captain watching a clock that
        // belongs to something else — which is this repo's third named bug class with a timer on it. The
        // exclusion runs both ways: the hold asks the property, and the property counts the hold.
        //
        // #1016 · THE PROPERTY MOVED UP ONE LEVEL AND THE LAW DID NOT. The hold left the excursion for the
        // page (a top in a docked bar has no excursion, and the dig has to take its seconds there too), so
        // the excursion's own AnyChannel cannot count it any more — and a captain in a berth has no
        // excursion to ask in the first place. `AnySlowThingUnderYourHands` is the ground's channels OR the
        // one hold, and it is what every starter asks now. Both halves of the exclusion are still pinned:
        // the hold asks the property, and the property counts the hold.
        Assert.Contains("AnySlowThingUnderYourHands", begin, StringComparison.Ordinal);
        Assert.Matches(
            new System.Text.RegularExpressions.Regex(
                @"AnySlowThingUnderYourHands\s*=>[^;]*_processing is not null",
                System.Text.RegularExpressions.RegexOptions.Singleline),
            Surface());
    }

    [Fact]
    public void TheFarEndFiresTheEffectTheGameAlreadyHad()
    {
        // #697's law, one lane later: a hand-written second copy of an ending is this repo's first named bug
        // class aimed at a state transition. The hold ends through the SAME two methods a press ended through
        // before it existed, with the same arguments.
        string done = Method("Map.Surface.Darkroom.cs", "private void CompleteProcessing(");

        Assert.Contains("TheOfferIsAnswered(", done, StringComparison.Ordinal);
        Assert.Contains("SetItDown(", done, StringComparison.Ordinal);

        string code = CodeOnly(done);
        Assert.DoesNotContain("GrantLabLead(", code, StringComparison.Ordinal);
        Assert.DoesNotContain("FileNote(", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Satchel.Remove(", code, StringComparison.Ordinal);

        // And the clock is cleared BEFORE the effect fires, so the effect runs in a world with no hold in it.
        // A leave that re-entered LeaveItem would otherwise meet its own clock and refuse itself — the
        // document set down nowhere, and the captain looking at a pocket that still holds it.
        int cleared = code.IndexOf("_processing = null", StringComparison.Ordinal);
        int fired = code.IndexOf("SetItDown(", StringComparison.Ordinal);
        Assert.True(cleared >= 0 && cleared < fired,
            "CompleteProcessing fires the effect before clearing the hold — the far end can meet its own " +
            "clock and refuse itself (#696).");
    }

    // ── THE LAW THE WHOLE RULING RESTS ON ──────────────────────────────────────────────────────────────

    [Fact]
    public void TheDarkroomHasNeverHeardOfTheTank()
    {
        // Owner: "in a shelter / refuge: time passes, tank does not"; "aboard ship: free air". Note what the
        // ruling does NOT say: it never asks for a branch. The air sim has priced location correctly on four
        // kinds of ground since #612, and the ONLY way for the hold to inherit that for free is to know
        // nothing about it — which is a property of this code and can be enforced as one.
        //
        // Watched go RED against the naive always-burn shape (`ex.AirSeconds = SuitAir.Drain(ex.AirSeconds,
        // dtRealSeconds * SuitAir.Breathing.Rate(...))` inside StepProcessing), which is a build that looks
        // perfectly correct on the regolith and charges a captain for standing in a pressurised refuge.
        foreach (string signature in new[]
        {
            "private void BeginProcessing(",
            "private void StepProcessing(",
            "private void CompleteProcessing(",
            "private void AbandonProcessing(",
        })
        {
            string code = CodeOnly(Method("Map.Surface.Darkroom.cs", signature));
            foreach (string forbidden in new[] { "AirSeconds", "SuitAir", "AirSupplyOf", "TankIsDrawing" })
            {
                Assert.False(code.Contains(forbidden, StringComparison.Ordinal),
                    $"`{signature}` mentions {forbidden} — the hold has started pricing itself, and the " +
                    "shelter/refuge/aboard cases stop being free the way the owner ruled they are (#696).");
            }
        }

        // And the hold is stepped from the ordinary surface tick, alongside the suit's own step, with the
        // same dt. That is the entire mechanism by which "out here it burns tank" is true: sim time passes,
        // and the thing that prices sim time carries on doing its job.
        string step = Pages("Map.Surface.Frame.cs");
        int air = step.IndexOf("StepSuitAir(dtRealSeconds);", StringComparison.Ordinal);
        int hold = step.IndexOf("StepProcessing(dtRealSeconds);", StringComparison.Ordinal);
        Assert.True(air >= 0 && hold > air,
            "the darkroom hold is not stepped after the tank on the same tick — either it is not wired into " +
            "StepSurface at all, or a hold can finish a frame the suit was never charged for (#696).");
    }

    // ── AND THE THINGS THAT TAKE IT AWAY FROM YOU ──────────────────────────────────────────────────────

    [Fact]
    public void WalkingOffTheSpotAbandonsIt()
    {
        string step = Method("Map.Surface.Darkroom.cs", "private void StepProcessing(");

        // The tolerance is Core's, asked here — a stand-still radius written out inline would be the fifth
        // named bug class with a hold on the end of it.
        Assert.Contains("Core.Processing.Wandered(", step, StringComparison.Ordinal);
        Assert.Contains("Interruption.Walked", step, StringComparison.Ordinal);

        // Riding the lift counts as walking away. A captain who steps into the car with a sheet half
        // photographed is not standing on the floor they started on, and a hold that survived a floor change
        // would file a gist naming a room the captain is no longer in (#691's line says WHERE).
        // #1016 · …asked of the ONE floor answer (`TheFloorUnderfoot`), because a hold can now run where
        // there is no excursion to have a floor: a berth deck and the captain's own boat are not floors of a
        // building and both answer zero, exactly as the docked bar's own walkers do. Same law, one hop.
        Assert.Contains("TheFloorUnderfoot != hold.Floor", step, StringComparison.Ordinal);
    }

    [Fact]
    public void AnAirAlarmBreaksTheHoldRatherThanPlayingUnderIt()
    {
        // #564's founding rule: air must never be a silent timer that kills you. A warning that fires while
        // the captain is watching a progress bar fill is that timer in a costume — so the bar goes away and
        // the alarm has the screen. The dependency points ONE way: the suit interrupts the darkroom.
        string suit = Method("Map.Surface.Tank.cs", "private void StepSuitAir(double dtRealSeconds)");

        Assert.Contains("ProcessingIsInterrupted(", suit, StringComparison.Ordinal);
        Assert.Contains("Interruption.Alarm", suit, StringComparison.Ordinal);

        // All three thresholds, not just the loud one. The point-of-no-return line is the one with a decision
        // still in it, and it is the one most worth not burying.
        Assert.Contains("SuitAir.CrossingWarning", suit, StringComparison.Ordinal);
        Assert.Contains("SuitAir.ReserveEngagedLine", suit, StringComparison.Ordinal);
        Assert.Contains("SuitAir.RunningLow(", suit, StringComparison.Ordinal);
    }

    [Fact]
    public void BeingREACHEDBreaksTheHoldAndSoDoesTheShuttleLifting()
    {
        // "The motion tracker keeps running during the hold (you are stationary and vulnerable — the mechanic
        // has teeth without any new enemy)." The teeth are only real if something arriving actually costs you
        // the exposure. Placed on the SWING and not on the wound: a captain who turns a blow aside has still
        // had an arm come through the space they were photographing into.
        string surface = Surface();
        Assert.Contains("Core.Processing.Interruption.Reached", surface, StringComparison.Ordinal);

        // And the excursion ending is an interruption like any other — announced, because a captain who
        // lifted off mid-exposure will otherwise go looking at the desk for a gist that was never filed.
        int lifted = surface.IndexOf("Core.Processing.Interruption.LiftedOff", StringComparison.Ordinal);
        int gone = surface.IndexOf("\n        _surface = null;", StringComparison.Ordinal);
        Assert.True(lifted >= 0 && gone > lifted,
            "the shuttle lifting does not abandon a running hold before the excursion is thrown away — the " +
            "captain is told nothing and the clock dies with the object (#696).");
    }

    // ── AND IT IS ALL VISIBLE ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheOneBarSaysWHICHSlowThingThisIs()
    {
        // #562's ruling, one channel later: a shovel drawn over somebody photographing a pay sheet is the sim
        // doing one thing while a picture reports another. The hold rides the ONE progress bar the surface has
        // always had — a second bar would be a second answer to "what is the captain busy with".
        string hud = Pages("Map.Surface.Hud.cs");
        Assert.Contains("Core.Processing.Fraction(paper.Elapsed, ProcessingSeconds)", hud, StringComparison.Ordinal);
        // #1016 · …fed by the ONE hold, which is the page's now rather than the excursion's.
        Assert.Contains("_processing is { } paper", hud, StringComparison.Ordinal);

        string glyph = Method("Map.Surface.Hud.cs", "private static string SurfaceChannelGlyph(");
        Assert.Contains("Core.Processing.Glyph", glyph, StringComparison.Ordinal);

        // Not cold-green. The rearm is the ship helping you; standing still in the open for twenty seconds is
        // the cost the mechanic is made of, and a soothing colour over it would be the picture arguing with
        // the sim.
        string aid = Method("Map.Surface.Hud.cs", "private static bool SurfaceChannelIsAid(");
        // #1016 · The hold is handed IN rather than read off the excursion (it is the page's now, because
        // a dig at a top in a docked bar has no excursion under it). The method is still static and still
        // pure, which is the part of this law worth holding.
        Assert.Contains("hold is null", aid, StringComparison.Ordinal);

        // And the bright line above the keybar outranks the chest while a hold runs: for those seconds the
        // only thing left to decide is whether the boots stay put, and "hold position" without a number is an
        // instruction to wait an unknown length of time while something walks towards you.
        string prompt = Method("Map.Surface.Hud.cs", "private string? BuildStandingPrompt(");
        Assert.Contains("_processing is { } paper", prompt, StringComparison.Ordinal);
        Assert.Contains("Core.Processing.SecondsLeft(", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePocketSpeaksForItselfWhileTheClockRuns()
    {
        // #680/#686's law: nothing important behind a blur. The hold SHUTS the satchel on the way in — the
        // vulnerability is the mechanic and a captain cannot watch a motion tracker through a backdrop — but
        // nothing stops them pressing I again, and a pocket that says nothing about the document currently
        // under their own hands is the dialog lying about itself.
        string satchel = SatchelBlock();
        Assert.Contains("ProcessingUnderway()", satchel, StringComparison.Ordinal);

        // And the outcome of a finished leave is said wherever the captain is actually looking, which after
        // #696 is a fork rather than a fact: the dialog if they reopened it, the HUD if they did not.
        string setDown = Method("Map.Surface.Satchel.cs", "private void SetItDown(");
        Assert.Contains("SayItWhereTheyAreLooking(", setDown, StringComparison.Ordinal);

        string say = Method("Map.Surface.Satchel.cs", "private void SayItWhereTheyAreLooking(");
        Assert.Contains("_showSatchel", say, StringComparison.Ordinal);
        Assert.Contains("_satchelOutcome", say, StringComparison.Ordinal);
        Assert.Contains("ShowPulseMessage(", say, StringComparison.Ordinal);

        // The hold shuts the pocket. Written down because it deliberately REVERSES #691's "the satchel stays
        // open" call, and a future pass that restores that line without reading this one would put the fan
        // back behind the blur for the twenty seconds it matters most.
        Assert.Contains("CloseSatchel();", Method("Map.Surface.Darkroom.cs", "private void BeginProcessing("),
            StringComparison.Ordinal);
    }

    [Fact]
    public void TheClockIsONENumberAndTheHintReadsIt()
    {
        // The owner's guard, verbatim: "Duration is a Core constant read by both the sim and any UI hint (one
        // source)." The client's ONE reference to it is the ProcessingSeconds property; everything else — the
        // bar, the prompt, the start line, the satchel hint — reads that.
        string surface = Surface();
        int references = 0;
        for (int at = surface.IndexOf("SecondsPerDocument", StringComparison.Ordinal); at >= 0;
             at = surface.IndexOf("SecondsPerDocument", at + 1, StringComparison.Ordinal))
        {
            references++;
        }
        Assert.True(references == 1,
            $"the client names Processing.SecondsPerDocument {references} times — one of them is a second " +
            "source, and the day the feel is tuned only one of them will move (#696).");

        Assert.Contains("private double ProcessingSeconds =>", surface, StringComparison.Ordinal);
        Assert.Contains("_processCheatSeconds ?? Core.Processing.SecondsPerDocument", surface,
            StringComparison.Ordinal);

        // The hint on the 🫳 control is composed in Core off that same number, so a satchel promising twenty
        // seconds of a fifteen-second hold is not expressible. And it asks GistOf, the same question the press
        // branches on, so the rows that carry a clock are exactly the rows the hint warns about.
        Assert.Contains("LeaveHintFor(item)", SatchelBlock(), StringComparison.Ordinal);
        string hint = Method("Map.Surface.Darkroom.cs", "private string LeaveHintFor(");
        Assert.Contains("LeftBehind.GistOf(", hint, StringComparison.Ordinal);
        Assert.Contains("Core.Processing.LeaveHint(ProcessingSeconds)", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void TheQaCheatSwitchesTheClockOffAndNothingElse()
    {
        // "a scene nobody can reach on demand is a scene that ships broken" — the house rule written beside
        // these cheats — with its converse: a clock designed to be FELT is a clock no story test should have
        // to sit through, and a test that waits one out is a test that gets deleted the first time it flakes.
        string sim = Sim();
        Assert.Contains("pair.StartsWith(\"process=\"", sim, StringComparison.Ordinal);
        Assert.Contains("_processCheatSeconds = Math.Max(0, hold);", sim, StringComparison.Ordinal);

        // And a zero-length hold resolves on the press rather than one frame later, because a frame is a tick
        // of air out on the regolith and a cheat that quietly charged for it would make the guard above flap.
        string begin = Method("Map.Surface.Darkroom.cs", "private void BeginProcessing(");
        Assert.Contains("ProcessingSeconds <= 0", begin, StringComparison.Ordinal);
        Assert.Contains("CompleteProcessing(hold);", begin, StringComparison.Ordinal);

        // There is deliberately no cheat for what the hold costs in AIR, because nothing computes that.
        string cheats = CodeOnly(sim);
        Assert.DoesNotContain("_processAirCheat", cheats, StringComparison.Ordinal);
    }
}
