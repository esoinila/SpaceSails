using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #992 · <b>EVERY POP-UP CAN BE DISMISSED.</b> Owner ruling, 2026-08-24, verbatim:
/// <i>"As a general ruling there should not be a pop-up that cannot be closed or minimized."</i>
///
/// <para>A ruling that reads like one sentence and touches eighty-nine surfaces is a ruling that needs a law
/// rather than a sweep, because the sweep is right on the afternoon it is run and wrong by the next feature.
/// This file is the law, and it is three assertions that do three different jobs. None of them is the other
/// two, and the third one is the only one that is expensive.</para>
///
/// <list type="number">
/// <item><b><see cref="NoSurfaceInTheSourceEscapesTheRegister"/> — the completeness guard.</b> It reads the
/// markup as TYPED and requires every overlay-rooted element in it to be in <see cref="TheRegister"/>. It is
/// cheap, it is total, and it is the one that catches the pop-up nobody has written a driver for yet: a new
/// full-viewport gate joins <c>Map.razor</c>, the register does not mention its class, and the law fails
/// naming it. It does not need the surface to be reachable, or raised, or even finished.</item>
/// <item><b><see cref="NoSurfaceOnTheScreenEscapesTheRegister"/> — the same guard from the other side.</b>
/// The source scan reads class ATTRIBUTES, so a class list assembled in C# and splatted in would slip past
/// it. This one walks the render tree across <see cref="EveryDeskBootsTests"/>'s own world matrix and asks
/// the same question of what was actually DRAWN.</item>
/// <item><b><see cref="EveryPopUpTheBenchCanRaiseOffersAWayOut"/> — the law itself, proved by pressing.</b>
/// It raises each surface, finds every control inside it, and <b>presses them</b> through the renderer's own
/// event channel to see which ones make the surface go away.</item>
/// </list>
///
/// <h3>Why the third one presses instead of reading</h3>
///
/// <para>Because the shape of this bug is a control that LOOKS like a way out and is not wired to one, or a
/// surface whose only way out is a backdrop nobody can see. A law that searched the markup for a ✕ would
/// pass on a ✕ wired to nothing; a law that called <c>CloseStoryCard</c> by name would prove a method clears
/// a field and say nothing about whether any control on the screen reaches it. So the bench dispatches a real
/// click at the handler id the render tree wrote, and the verdict is read off the NEXT render.</para>
///
/// <para>It also means the law cannot be lied to about the critical-decision exception. Fable's reading of the
/// ruling is that a modal which asks the captain to DECIDE something may have no ✕, because every answer it
/// offers is itself a close — a decision is a dismissal. That is a real exception and the BUSTED demand, the
/// arrival brake and the walk-in all live in it. But "all its buttons close it" is a claim about behaviour,
/// and the way this repository has been burned before is by a guard that took such a claim from a list. So
/// <see cref="Verdict.EveryControlCloses"/> is not read from the register — the register says only which
/// surfaces are ALLOWED to earn it — it is established by pressing every control in turn and watching.</para>
///
/// <h3>What this law does not reach, said out loud</h3>
///
/// <para><b>Geometry.</b> "Inside the viewport" is a question about layout, and there is no layout here — the
/// bench renders a tree, not a page. What is asserted instead is the two things that are true off-browser and
/// that every real out-of-viewport failure has had underneath it: the control is INSIDE the surface's own
/// subtree (so it cannot be a ✕ belonging to the panel behind), and it is not <c>d-none</c>. The pixels are
/// <c>SpaceSails.UiGate</c>'s job and are noted in the PR as the follow-up.</para>
///
/// <para><b>Reach.</b> Some surfaces need a world this bench cannot build — a wreck alongside, a demand from a
/// collector, a keypad mid-crack. Those rows are in the register (so guard 1 and guard 2 cover them) with
/// <see cref="PopUp.Raise"/> null and a stated reason, and
/// <see cref="TheUndrivenListOnlyEverGetsShorter"/> pins how many there are. The number can go down without
/// anybody's permission and cannot go up without a deliberate edit to a written-down count.</para>
/// </summary>
public sealed class EveryPopUpCanBeDismissedTests
{
    // ── What the law calls a pop-up ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// How a surface is RECOGNISED as a pop-up without anybody having to remember to say so.
    ///
    /// <para>Two halves, and the split is deliberate. The first is STRUCTURAL: this codebase names a
    /// full-viewport gate <c>*-backdrop</c> or <c>*-overlay</c>, and any new one that follows the house
    /// naming is caught by the completeness guards the day it is typed, with no edit here. The second is a
    /// NAMED list of the families that predate that convention — the anchored deck cards, the tucking
    /// instruments, the plate — because a rule cannot be inferred from names that were chosen before it
    /// existed, and guessing at them would be a recogniser that quietly matched the wrong things.</para>
    /// </summary>
    private static bool IsAPopUpRoot(string cssClass) =>
        cssClass.EndsWith("-backdrop", StringComparison.Ordinal)
        || cssClass.EndsWith("-overlay", StringComparison.Ordinal)
        || cssClass.EndsWith("-modal", StringComparison.Ordinal)
        || TheFamiliesThatPredateTheNaming.Contains(cssClass);

    private static readonly HashSet<string> TheFamiliesThatPredateTheNaming = new(StringComparer.Ordinal)
    {
        "story-plate",        // the beat that rides the edge
        "deck-offer-card",    // the anchored card a room offers you
        "deck-shuttle-card",  // the hatch and the load-out
        "seated-dock",        // the sitting, as a strip
        "selfie-offer",       // the nudge
        "map-body-menu",      // the three context menus (body, contact, open sky, pick list)
        "map-dossier",        // #960's tucking card
        "map-scope",          // #963's tucking instrument
        "map-scope-tile",     // …and what it tucks into
        "map-dossier-tile",
        "jump-overlay-tile",  // #992's own
        "map-tutorial",       // the lesson checklist
        "map-dest-panel",     // the nav-target panel
        "map-adrift",         // the distress lifeline
        "dice-tray",          // the component's own card
        "map-loading",        // the boot door and the descent door
    };

    /// <summary>
    /// A surface is judged by its WHOLE class list, not one token at a time.
    ///
    /// <para>The first build of this guard asked the question of each token separately and reported
    /// <c>rep-backdrop</c> and <c>selfie-backdrop</c> as unregistered pop-ups. They are neither: they are
    /// MODIFIERS, and the elements wearing them wear <c>view-object-backdrop</c> in the same attribute — the
    /// family root, registered, and already answering the ruling for both. A guard that made one element look
    /// like two surfaces would have had somebody registering the same card three times under its variants.
    /// So: an element is a stranger only when it looks like a pop-up and NOTHING in its class list is known.
    /// </para>
    /// </summary>
    private static bool IsAnUnregisteredSurface(
        IReadOnlyCollection<string> classes, IReadOnlySet<string> registered) =>
        classes.Any(IsAPopUpRoot)
        && !classes.Any(c => registered.Contains(c) || NotPopUpsAndWhy.ContainsKey(c));

    /// <summary>
    /// Roots the recogniser finds that are NOT pop-ups, each with the reason it is not one, so that the
    /// exception is a sentence somebody had to write rather than a silent hole in a regex.
    ///
    /// <para>A surface qualifies for this list only by being something the player never has to get rid of:
    /// it is either the page's own furniture (always there, nothing raised it) or a door the boot is standing
    /// behind (there IS no game to get back to yet). Anything transient belongs in
    /// <see cref="TheRegister"/>.</para>
    /// </summary>
    private static readonly Dictionary<string, string> NotPopUpsAndWhy = new(StringComparer.Ordinal)
    {
        ["map-loading"] =
            "the boot door and the shuttle-descent door. Nothing has been raised OVER anything — there is no "
            + "game behind them yet — so there is nothing a dismiss could give back.",
        ["map-scope"] =
            "an instrument, not a pop-up: it is a permanent fixture of the Nav desk that the captain switched "
            + "to. It carries the minimise anyway (#963) and TheScopeTucksAwayTests owns it.",
        ["map-scope-tile"] = "the tucked scope. It IS a dismissal's result.",
        ["map-dossier-tile"] = "the tucked dossier. Same.",
        ["jump-overlay-tile"] = "the tucked crossing. Same.",
        ["map-dest-panel"] =
            "HUD, not a pop-up: it is the Nav desk drawing the target the captain chose, and it goes when the "
            + "target does. Nothing raised it over the game.",
        ["map-adrift"] =
            "the distress lifeline — a re-open button for the rescue offer, and a button is not a surface.",
        ["seated-dock"] =
            "#865's answer to this exact complaint, already shipped: the sitting is a STRIP precisely so it "
            + "does not blind the captain to the room. Leaving the table is a move on it.",

        // ── FABLE'S RULING, WAVE 7 (#997) ────────────────────────────────────────────────────────────
        // The front door came off TheRegister and landed here. It sat there as Exit.EveryControlCloses,
        // UNDRIVEN — nothing had ever pressed a control on it — and #1007 disproved the claim from the
        // other side, by pressing: TheFrontDoorOffersNoCloseAtAllAndNotEveryControlOnItIsOne opens the ▸
        // dev-starts chevron and the door is still standing. So the row was not a row that needed a driver;
        // it was a row in the wrong book. Moving it lowers TheUndrivenListOnlyEverGetsShorter's ceiling
        // from 15 to 14 — a deliberate edit to a written-down count, made by the wave that means to make it.
        //
        // THE KEY IS SHARED, AND THAT IS SAID OUT LOUD RATHER THAN TIDIED AWAY. Four surfaces are drawn on
        // `.start-picker-backdrop`; three of them — the logbook, the bank sheet, the import consent — ARE
        // pop-ups and keep their register rows, so the recogniser goes on finding this class registered and
        // this entry exempts nothing by itself. It is the RULING, written where the reasons live, and the
        // guard that holds it is TheFrontDoorIsNotAPopUpAndTheSheetsOverItStillAre in
        // TheShellOwnsTheDeathRowsAndTheShuttleHatchTests.
        ["start-picker-backdrop"] =
            "The front door is the game's threshold, not a pop-up over play — there is no game behind it to "
            + "return to, so a way out would be a door to nowhere. Its sheets (the logbook, the bank sheet, "
            + "the consent) are pop-ups and have their ways out.",
    };

    // ── The register ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>How a surface is allowed to end.</summary>
    private enum Exit
    {
        /// <summary>The ordinary case: a control inside it takes it off the screen or tucks it into a tile.
        /// The law finds that control by pressing, and it must be a control and not the backdrop.</summary>
        AControl,

        /// <summary>The critical-decision exception. Every control it offers IS a close, so it needs no ✕ —
        /// and the law proves the claim by pressing each of them rather than believing this word.</summary>
        EveryControlCloses,
    }

    /// <param name="Name">What it is called out loud, for a failure message a person can act on.</param>
    /// <param name="RootClass">The class its ROOT wears — the one the recogniser sees.</param>
    /// <param name="World">A URL from <see cref="EveryDeskBootsTests"/>'s matrix that can host it.</param>
    /// <param name="Raise">Put the page in the state that draws it, or null when this bench cannot build the
    /// world it needs. Null rows are still covered by both completeness guards.</param>
    /// <param name="WhyNotDriven">Required on a null <paramref name="Raise"/>; must be empty otherwise.</param>
    private sealed record PopUp(
        string Name,
        string RootClass,
        string World,
        Exit HowItEnds,
        Action<DeskBench>? Raise,
        string WhyNotDriven = "",
        ShipDesk At = ShipDesk.Deck);

    /// <summary>The five gates that all draw a <c>.convergence-backdrop</c>. Raise exactly one.</summary>
    private static readonly string[] TheConvergenceBand =
    [
        "_convergenceRevealOpen", "_groundLessonOpen", "_groundGrewOpen", "_tubeRearmOpen", "_airCardOpen",
    ];

    private static void OnlyThisConvergenceCard(DeskBench bench, string gate)
    {
        foreach (string other in TheConvergenceBand)
        {
            bench.Poke(other, false);
        }

        bench.Poke("_faceScene", null);   // the sixth card on that root, gated on a name rather than a bool
        bench.Poke(gate, true);
    }

    private const string FreeFlying = "/map?start=wreck";
    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";
    private const string Ashore = "/map?dock=the-tilt&site=0&land=1";

    private static readonly PopUp[] TheRegister =
    [
        // ── The three #992 fixed: the surfaces that had NO way out at all ────────────────────────────
        new("the story / reveal PLATE (incl. the great port's own \"THE LONG WALK IN\")",
            "story-plate", Docked, Exit.AControl,
            b => b.Poke("_storyPlate",
                ((StoryBeats.Beat Beat, string? Subject, double UntilSimTime)?)
                (StoryBeats.Beat.BerthGreatPort, "selene-gate", 1e9))),

        new("the void sheet — CROSSING THE VOID (long haul)",
            "jump-overlay", FreeFlying, Exit.AControl,
            b =>
            {
                b.Poke("_voidCardTucked", false);
                b.Poke("_jumpTotalYears", 6);
                b.Poke("_jumpYear", 2);
                b.Poke("_jumpDestName", "Barnard's Reach");
                b.Poke("_jumpFlavor", "the bus does not stop out here");
                b.Poke("_jumpActive", true);
            }),

        new("the void sheet — COAST CONSUMED (computed skip)",
            "jump-overlay", FreeFlying, Exit.AControl,
            b =>
            {
                b.Poke("_voidCardTucked", false);
                b.Poke("_jumpActive", false);
                b.Poke("_coastSkipDays", 40);
                b.Poke("_coastSkipLabel", "the long coast");
                b.Poke("_coastSkipActive", true);
            }),

        new("the face scene — \"you look different\" (answer phase)",
            "convergence-backdrop", Docked, Exit.AControl,
            b =>
            {
                foreach (string other in TheConvergenceBand)
                {
                    b.Poke(other, false);
                }

                b.Poke("_faceSceneReply", null);
                b.Poke("_faceScene", OldCrew.LedgerPrefix + "maren");
            }),

        // ── The card families, one row each per gate the bench can reach ──────────────────────────────
        new("the story / reveal CARD", "view-object-backdrop", Docked, Exit.AControl,
            b => b.Poke("_storyCard",
                ((StoryBeats.Beat Beat, string? Subject, string? Outcome)?)
                (StoryBeats.Beat.BerthGreatPort, "selene-gate", null))),

        new("the satchel / notebook / spread", "view-object-backdrop", Ashore, Exit.AControl,
            b => b.Poke("_showSatchel", true)),

        new("the ship's own atmosphere board", "view-object-backdrop", Docked, Exit.AControl,
            b => b.Poke("_showShipBoard", true)),

        new("the hull-charge board", "view-object-backdrop", Docked, Exit.AControl,
            b => b.Poke("_showChargeBoard", true)),

        new("her scuttling charges", "view-object-backdrop", Docked, Exit.AControl,
            b => b.Poke("_showShipScuttlePanel", true)),

        new("the shape alarm panel", "view-object-backdrop", Ashore, Exit.AControl,
            b => b.Poke("_showAlarmPanel", true)),

        new("the door board", "view-object-backdrop", Ashore, Exit.AControl,
            b => b.Poke("_showDoorBoard", true)),

        new("the captain's remote", "view-object-backdrop", Ashore, Exit.AControl,
            b => b.Poke("_showCaptainsRemote", true)),

        // FOUR surfaces share .start-picker-backdrop — the front door, the logbook, the bank sheet and the
        // import consent — so this row puts the other three down. It matters more here than anywhere else
        // because the law PRESSES EVERY CONTROL, and one of the logbook's own controls (⤓ bank here) raises
        // a sibling on the same root class. Without the reset the law pressed "bank here", then re-raised,
        // then pressed "Close", and reported a logbook that would not close — when what was left on the
        // screen was the bank sheet the earlier press had opened. Read the failure carefully rather than
        // loosening the law: the law was right that something wearing that class was still up.
        new("the saves dialog / logbook", "start-picker-backdrop", Docked, Exit.AControl,
            b =>
            {
                b.Poke("_showStartPicker", false);
                b.Poke("_bankPrompt", null);
                b.Poke("_importConfirming", false);
                b.Poke("_showSaveDrawer", true);
            }),

        // ── The convergence band ─────────────────────────────────────────────────────────────────────
        // FIVE SURFACES SHARE .convergence-backdrop, and a landing raises the ground lesson by itself — so
        // each of these rows puts its four siblings down before it puts itself up. Without that the law
        // reads the earliest one in the tree and reports whichever card it happened to find, which is one
        // source answering for another: this repository's first named bug class, in a test.
        new("the Convergence", "convergence-backdrop", Docked, Exit.AControl,
            b => OnlyThisConvergenceCard(b, "_convergenceRevealOpen")),

        new("the ground lesson", "convergence-backdrop", Ashore, Exit.AControl,
            b => OnlyThisConvergenceCard(b, "_groundLessonOpen")),

        new("the map just got bigger", "convergence-backdrop", Ashore, Exit.AControl,
            b => OnlyThisConvergenceCard(b, "_groundGrewOpen")),

        new("the tube rearm card", "convergence-backdrop", Ashore, Exit.AControl,
            b => OnlyThisConvergenceCard(b, "_tubeRearmOpen")),

        new("the air-running-out card", "convergence-backdrop", Ashore, Exit.AControl,
            b => OnlyThisConvergenceCard(b, "_airCardOpen")),

        // ── Chrome that is still a pop-up ────────────────────────────────────────────────────────────
        // …and the checklist is drawn INSIDE the Nav desk's own column, so the law has to sit down at Nav
        // before it can ask the question. Found by the law rather than assumed: the first build poked the
        // gate at the Deck and reported "raising it drew NOTHING wearing .map-tutorial", which is the
        // wrong-world complaint doing exactly the job it is there for.
        new("the help / lesson checklist", "map-tutorial", Docked, Exit.AControl,
            b => b.Poke("_showTutorial", true), At: ShipDesk.Nav),

        // ── In the register, not yet driven. Each names the world this bench cannot build. ───────────
        // #997 wave 3 · DRIVEN NOW — and it is entered at the COLLECTOR'S TERMS rather than at the demand,
        // which is a finding rather than a dodge.
        //
        // The demand's three answers do not close this panel; they turn its page. SUBMIT goes to
        // Confiscated, BRIBE to BribedOff, RESIST to a won roll, a lost one or the Bolivia — so this row's
        // old EveryControlCloses was the same false claim #997 found on the rep's card, believed for the
        // same reason: nothing had ever pressed it. What is true is that BUSTED is a STAGED decision, which
        // is what OverlayShell's `Restages` says by name, and that every chain ends on a card whose single
        // control really is a way out.
        //
        // This law knows two Exits and has no word for a stage, so the row asks it the question it CAN
        // answer — of the Confiscated card, where "Take the hit" is the one control and it closes — and the
        // staged claim is proved next door by pressing every answer of every stage and following each chain
        // to its end: TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests.
        new("the BUSTED / death panel (the collector's terms)", "busted-backdrop", FreeFlying,
            Exit.EveryControlCloses,
            b => TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests.StageTheDemand(b, "Confiscated")),
        new("the arrival-brake card", "deck-offer-card", FreeFlying, Exit.EveryControlCloses, null,
            "needs _brakeGate.Asking, which is ArrivalBrake.Advance's own timing verdict on a ship that is "
            + "coming in hot — a sim state, not a field."),
        // #997 wave 2 · DRIVEN NOW, and the reason it could not be before was half right. Walking her across
        // a floor is genuinely out of reach off-browser — but her CARD is gated on one field, and the law's
        // question is about the card. So the row raises the card the same way the walk-in guard raises her
        // mid-crossing state, and the ruling is proved of her by pressing rather than believed from a list.
        // Her exit moved from EveryControlCloses to AControl in the same breath: #997 gave her the shell's
        // dismiss, which is a way out that is not one of her two answers.
        new("the walk-in HOSTED card", "view-object-backdrop", Docked, Exit.AControl,
            b => b.Poke("_walkInCard", (WalkIn.Who?)WalkIn.Who.Ilse)),
        new("the rep's pitch (Harlan Fess)", "view-object-backdrop", Docked, Exit.EveryControlCloses, null,
            "same room, same reason — his card is raised on the landing frame of a crossing."),
        new("the rescue / tow offer", "rescue-backdrop", FreeFlying, Exit.EveryControlCloses, null,
            "needs Adrift, which is a fuel-and-velocity verdict on a live sim."),
        new("the loud plan alarm", "rescue-backdrop", FreeFlying, Exit.AControl, null,
            "needs LoudPlanAlarm, a read of a plan's own break against sim time."),
        new("the hatch keypad", "pin-backdrop", Ashore, Exit.AControl, null,
            "needs a staged crack job with a hatch and a pin."),
        new("the mission celebrations, briefs and reveals", "mission-celebration-backdrop", Ashore,
            Exit.AControl, null,
            "five gates on one root class, each fed by a completed contract, an expedition or a wreck."),
        new("the expedition reveal", "expedition-reveal-backdrop", Ashore, Exit.AControl, null,
            "raised by an expedition region resolving; needs the away lane run."),
        new("the operating-log card", "vent-read-backdrop", FreeFlying, Exit.AControl, null,
            "needs a wreck alongside with a read room in _ventReads."),
        new("the pick-candidate chooser and the three context menus", "map-body-menu", Docked,
            Exit.AControl, null,
            "needs a pointer hit against a drawn body, contact or patch of sky."),
        new("the dice tray", "dice-tray", Docked, Exit.AControl, null,
            "a child component gated on a DiceTray.Event handed down from a roll."),
        new("the shuttle-bay hatch and the load-out", "deck-shuttle-card", Ashore, Exit.AControl, null,
            "needs shuttle stops in reach of the berth — the bench's own documented horizon (see DeskBench)."),
        new("the selfie offer", "selfie-offer", Ashore, Exit.EveryControlCloses, null,
            "raised by walking into a view worth a photograph."),
        // #997 wave 10 · DRIVEN NOW, and by a URL rather than by a poke. The reason this row sat undriven
        // was true when it was written — the dossier is gated on a tactical target, and the only two roads
        // to one were a contact in sensor reach or a collector bought by a robbery, neither of which a URL
        // could reach. `?target=` is that road (Map.Npc.SeedTargetCheat), so the WORLD raises the card and
        // the driver's whole job is to put it back up between presses: the law re-raises per control, and
        // the ✕ it presses first drops the target the boot set.
        //
        // The driver calls the SHIPPING cheat rather than poking `_interestTargetId`, which is the stronger
        // of the two: a cheat that stopped raising this card would fail here rather than nowhere.
        new("the target dossier", "map-dossier", Docked + "&target=collector", Exit.AControl,
            PointTheGlassAtHerAgain, At: ShipDesk.Nav),
    ];

    /// <summary>#997 wave 10 · Re-point the tactical UI at the collector <c>?target=collector</c> already
    /// sent, through the cheat's own contact-id road — so no second hunter is spawned per press and the
    /// path this exercises is the one a dev URL takes.</summary>
    private static void PointTheGlassAtHerAgain(DeskBench bench)
    {
        var muscle = (List<HunterState>)bench.Peek("_hunters")!;
        Assert.True(muscle.Count > 0,
            "?target=collector sent no muscle at all, so there is no dossier for this row to raise. Either "
            + "SpawnHunterForHeatEvent found nothing policed within reach of Selene Gate — which would be a "
            + "world change worth knowing about — or the cheat has stopped calling it.");
        bench.CallOnTheDispatcher("SeedTargetCheat", muscle[0].Id);
    }

    // ── Guard 1 · the completeness guard, read off the markup as typed ────────────────────────────────

    /// <summary>
    /// EVERY OVERLAY ROOT IN THE SOURCE IS IN THE REGISTER.
    ///
    /// <para>This is the guard that makes the law survive the next feature. A pop-up joins the client by
    /// somebody typing a <c>class="…-backdrop"</c> into a razor file; from that moment this fails, naming the
    /// class and the file, until the surface is entered in <see cref="TheRegister"/> — at which point guards 2
    /// and 3 start asking it the real question.</para>
    /// </summary>
    [Fact]
    public void NoSurfaceInTheSourceEscapesTheRegister()
    {
        var registered = TheRegister.Select(p => p.RootClass).ToHashSet(StringComparer.Ordinal);
        var strangers = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (string file in RazorFiles())
        {
            foreach (string[] classes in ClassListsIn(File.ReadAllText(file)))
            {
                if (IsAnUnregisteredSurface(classes, registered))
                {
                    strangers.TryAdd(string.Join(' ', classes.Where(IsAPopUpRoot)), Path.GetFileName(file));
                }
            }
        }

        Assert.True(strangers.Count == 0,
            $"{strangers.Count} surface(s) in the markup look like a pop-up and are not in the register, so "
            + "nothing has ever asked them the owner's question (2026-08-24: \"there should not be a pop-up "
            + "that cannot be closed or minimized\"). Add a row to TheRegister — or, if it is not a pop-up at "
            + "all, a sentence to NotPopUpsAndWhy saying why:\n  - "
            + string.Join("\n  - ", strangers.Select(s => $"{s.Key}  ({s.Value})")));
    }

    /// <summary>The register may not carry a row nobody can act on: a row with no driver must say why, and a
    /// row with a driver must not pretend it has a reason not to.</summary>
    [Fact]
    public void EveryRowInTheRegisterIsHonestAboutItself()
    {
        var wrong = TheRegister
            .Where(p => (p.Raise is null) == (p.WhyNotDriven.Length == 0))
            .Select(p => p.Raise is null
                ? $"{p.Name}: no driver and no reason given"
                : $"{p.Name}: has a driver AND a reason not to be driven")
            .ToList();

        Assert.True(wrong.Count == 0, string.Join("\n  - ", wrong));
    }

    /// <summary>
    /// THE UNDRIVEN LIST ONLY EVER GETS SHORTER.
    ///
    /// <para>A register row with no driver is covered by the two completeness guards and by nothing else — it
    /// is named, but the ruling has not been PROVED of it. That is an honest state to be in and a dishonest
    /// one to drift in, so the count is written down. Anybody may make it smaller; making it bigger costs an
    /// edit to this number and a line in a diff.</para>
    /// </summary>
    [Fact]
    public void TheUndrivenListOnlyEverGetsShorter()
    {
        // #997 wave 7 · FIFTEEN BECAME FOURTEEN, and by a row LEAVING rather than by one being driven.
        // The front door was never a pop-up; it is the threshold, and it now says so in NotPopUpsAndWhy in
        // Fable's own words (Fable's ruling, wave 7 — see the entry there for the reason and for why the
        // class it is keyed on is still registered by the three real sheets that share it). Lowering a
        // ceiling is always allowed; this one is lowered because the row it counted is gone.
        //
        // #997 wave 10 · FOURTEEN BECAME TWELVE, and only ONE of those two steps is an achievement.
        //
        // The target dossier is driven now, and it is the first reason on this list that turned out to be a
        // missing DEV DOOR rather than a world the bench cannot build. Its row said "needs a tactical
        // target"; that was true, and it is answerable from a URL now (?target=, Map.Npc.SeedTargetCheat),
        // so the ruling is proved of that card by pressing rather than named and left.
        //
        // The other step is bookkeeping, and it is said out loud rather than pocketed: the ceiling has been
        // 14 with only 13 rows under it since wave 7, so it carried a spare notch nobody had used. A
        // ceiling with slack in it cannot catch the next row that creeps under it — which is this number's
        // whole job — so it is pulled down onto the count. It is TIGHT now: undrive any single row and this
        // goes red, which is the red proof wave 10's PR quotes.
        int undriven = TheRegister.Count(p => p.Raise is null);
        Assert.True(undriven <= 12,
            $"{undriven} register rows have no driver, and the written-down ceiling is 12. If a row genuinely "
            + "cannot be raised off-browser, lower the ceiling is wrong — raise it deliberately and say so in "
            + "the commit; the point of the number is that it cannot creep.");
    }

    // ── Guard 2 · the same question, asked of what was drawn ──────────────────────────────────────────

    /// <summary>
    /// EVERY OVERLAY ROOT THAT REACHES THE SCREEN IS IN THE REGISTER.
    ///
    /// <para>Guard 1 reads class ATTRIBUTES out of the source, so a class list assembled in C# — a splatted
    /// dictionary, a name built from data, a <c>@(cond ? "a-backdrop" : "")</c> — would slip past it. This
    /// walks what the renderer actually emitted, across the same world matrix
    /// <see cref="EveryDeskBootsTests"/> sweeps, and asks the same question of the DOM.</para>
    /// </summary>
    [Fact]
    public async Task NoSurfaceOnTheScreenEscapesTheRegister()
    {
        var registered = TheRegister.Select(p => p.RootClass).ToHashSet(StringComparer.Ordinal);
        var strangers = new SortedSet<string>(StringComparer.Ordinal);

        foreach (string url in new[] { FreeFlying, Docked, Ashore })
        {
            using DeskBench bench = await DeskBench.BootAsync(url);

            foreach (ShipDesk desk in DeskBench.TabBarOrder)
            {
                await bench.SwitchAsync(desk);
                DeskBench.Painted painted = await bench.RenderAsync();

                foreach (string[] classes in painted.Root.Descendants().Select(n => n.Classes.ToArray())
                             .Concat(painted.MarkupBlobs.SelectMany(ClassListsIn)))
                {
                    if (IsAnUnregisteredSurface(classes, registered))
                    {
                        strangers.Add(
                            $"{string.Join(' ', classes.Where(IsAPopUpRoot))}  (drawn at {url} · {desk})");
                    }
                }
            }
        }

        Assert.True(strangers.Count == 0,
            $"{strangers.Count} surface(s) reached the screen wearing a pop-up root class that the register "
            + "does not know:\n  - " + string.Join("\n  - ", strangers));
    }

    // ── Guard 3 · the law itself, proved by pressing ──────────────────────────────────────────────────

    /// <summary>
    /// THE RULING, AS A LAW: raise it, press everything in it, and see what makes it go.
    ///
    /// <para>Every driveable row in the register is opened, and every visible control inside the surface's own
    /// subtree is pressed in turn — each from a freshly re-raised surface, so one control's press cannot be
    /// mistaken for another's. A control counts as a way out when the next render no longer shows the surface,
    /// shows it <c>d-none</c>, or shows it wearing a tile class: closed and minimised are both dismissals, and
    /// the owner's ruling names both.</para>
    ///
    /// <para>The BACKDROP is deliberately not counted. Most cards in this codebase close when the scrim behind
    /// them is clicked, and that is a fine convenience and a poor affordance — it is invisible, and a player
    /// looking at a card with three answers on it and no ✕ has no way to learn it is there. So a surface whose
    /// only exit is its own root's <c>onclick</c> fails this law, which is exactly what the face scene's
    /// answer phase did before #992 gave it a ✕.</para>
    /// </summary>
    [Fact]
    public async Task EveryPopUpTheBenchCanRaiseOffersAWayOut()
    {
        var wrong = new List<string>();
        int proved = 0;

        // ONE BENCH PER SURFACE, and it is not a tidiness preference — the first build shared a bench across
        // every row in a world and the law lied to itself. Five surfaces in this client are rooted on
        // .convergence-backdrop; the face scene's last press leaves _faceScene set (an answer is not a
        // close), so the NEXT row raised the Convergence, pressed its ✕, and then found the face scene still
        // wearing the class it was looking for and scored the Convergence as unclosable. A law that reports a
        // surface by the class of a different surface is this repository's first named bug class wearing a
        // backdrop. A fresh page per row costs a second and cannot be confused.
        foreach (PopUp popUp in TheRegister.Where(p => p.Raise is not null))
        {
            using DeskBench bench = await DeskBench.BootAsync(popUp.World);
            await bench.SwitchAsync(popUp.At);

            proved++;
            foreach (string complaint in await WhatIsWrongWith(bench, popUp))
            {
                wrong.Add($"{popUp.Name}: {complaint}");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} of {proved} raised pop-ups broke the owner's ruling of 2026-08-24 (\"there should "
            + $"not be a pop-up that cannot be closed or minimized\"):\n  - " + string.Join("\n  - ", wrong));
    }

    private static async Task<IEnumerable<string>> WhatIsWrongWith(DeskBench bench, PopUp popUp)
    {
        DeskBench.Painted.Node? surface = await Raise(bench, popUp);
        if (surface is null)
        {
            // The gate was set and nothing appeared. Never a pass: a law that shrugged here would be a guard
            // handed the wrong world — this repository's fifth named bug class — and it would go on passing
            // for every surface that had quietly stopped rendering.
            return [$"raising it drew NOTHING wearing .{popUp.RootClass}. The register's driver and the "
                    + "markup's gate have come apart; one of them has moved."];
        }

        // Every control the surface owns, the root itself excluded: the backdrop's own onclick is the
        // invisible exit this law does not accept as the only one.
        var controls = surface.Descendants()
            .Where(n => n.Handlers.ContainsKey("onclick") && !n.Hidden)
            .Select(n => n.Name)
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (controls.Count == 0)
        {
            return [$"it has no control in it at all. It is drawn, it is on top, and there is no way out of "
                    + "it — the exact shape the ruling forbids."];
        }

        var closers = new List<string>();
        foreach (string control in controls)
        {
            if (await PressingItEndsTheSurface(bench, popUp, control))
            {
                closers.Add(control);
            }
        }

        if (closers.Count == 0)
        {
            return [$"none of its {controls.Count} control(s) took it off the screen when pressed — "
                    + $"[{string.Join(" · ", controls)}]. Whatever they do, none of them is a way out."];
        }

        if (popUp.HowItEnds == Exit.EveryControlCloses && closers.Count != controls.Count)
        {
            return [$"it is registered as a critical-DECISION modal — allowed no ✕ only because every answer "
                    + $"it offers is itself a close — but pressing them proved otherwise: "
                    + $"[{string.Join(" · ", controls.Except(closers, StringComparer.Ordinal))}] left it up. "
                    + "Either give it a ✕ or make every answer end it."];
        }

        return [];
    }

    /// <summary>Put the surface up and hand back its root node, or null when nothing was drawn.</summary>
    private static async Task<DeskBench.Painted.Node?> Raise(DeskBench bench, PopUp popUp)
    {
        popUp.Raise!(bench);
        DeskBench.Painted painted = await bench.RenderAsync();
        return TheSurface(painted, popUp.RootClass);
    }

    private static DeskBench.Painted.Node? TheSurface(DeskBench.Painted painted, string rootClass) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass(rootClass) && !n.Hidden);

    /// <summary>
    /// Raise it again, find the named control, PRESS IT, and re-read the page.
    ///
    /// <para>Re-raised per press on purpose: pressing a control that closes the surface and then looking for
    /// the next one would find nothing and score every later control as a non-closer, which would fail the
    /// decision exception on every modal that has one.</para>
    /// </summary>
    private static async Task<bool> PressingItEndsTheSurface(DeskBench bench, PopUp popUp, string control)
    {
        DeskBench.Painted.Node? surface = await Raise(bench, popUp);
        DeskBench.Painted.Node? button = surface?.Descendants()
            .FirstOrDefault(n => !n.Hidden
                                 && n.Handlers.ContainsKey("onclick")
                                 && string.Equals(n.Name, control, StringComparison.Ordinal));

        if (button is null)
        {
            return false;
        }

        await bench.PressAsync(button.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();
        DeskBench.Painted.Node? still = TheSurface(after, popUp.RootClass);

        // Gone, hidden, or tucked into a tile. #963's scope and #960's dossier both minimise by staying in
        // the tree, so "it is still in the DOM" is not the question — "is it still taking the screen" is.
        return still is null || still.Classes.Any(c => c.EndsWith("-tile", StringComparison.Ordinal));
    }

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every <c>class="…"</c> in a run of text, as its own token list. Used on the source and on the
    /// static-markup blobs the render tree hands out, which are the same shape.</summary>
    private static IEnumerable<string[]> ClassListsIn(string text) =>
        Regex.Matches(text, "class=\"([^\"]*)\"")
            .Select(m => m.Groups[1].Value
                .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

    private static IEnumerable<string> RazorFiles() =>
        Directory.EnumerateFiles(ClientSource(), "*.razor", SearchOption.AllDirectories);

    /// <summary>The shipping client's source, found from the test binary the way this repo's other
    /// source-shape guards find it — up out of bin/ and across.</summary>
    private static string ClientSource()
    {
        var at = new DirectoryInfo(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            at = at.Parent;
        }

        throw new DirectoryNotFoundException(
            "src/SpaceSails.Client is not above the test binary — this guard reads the markup as typed and "
            + "cannot do its job without it.");
    }
}
