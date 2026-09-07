using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Components;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #992 · <b>THE REGISTER</b> — every pop-up the client draws, named, with the world that can host it and
/// the way it is allowed to end: the table <see cref="EveryPopUpCanBeDismissedTests"/>'s three guards are
/// all measurements of.
///
/// <para>What this part owns is the LIST and the drivers that raise it. Nothing here asserts anything; a row
/// is a claim about the client, and the guards next door are what make a claim cost something. What the law
/// calls a pop-up in the first place is in the file that carries the class docblock.</para>
/// </summary>
public sealed partial class EveryPopUpCanBeDismissedTests
{
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
    /// <param name="World">A URL that can host it: one of <see cref="EveryDeskBootsTests"/>'s matrix worlds,
    /// or one of the three in <see cref="TheWorldsBeyondTheMatrix"/> that this register boots for itself.
    /// <see cref="EveryWorldInTheRegisterComesFromTheMatrixOrSaysWhyNot"/> is what stops a fourth from
    /// arriving unannounced.</param>
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

    /// <summary>#997 wave 12 · The oracle's corner, as one URL. <c>?oracle=1</c> seats Static Marsh whatever
    /// her rota says AND defaults the berth to a bar; <c>?ashore=1</c> walks the last leg, which is what
    /// puts <c>_deckMode</c> true — and her card is gated on the deck. Both cheats are the documented pair
    /// (Map.Sim.World.QueryArcs: <i>"the rant, one URL and one [E]"</i>).</summary>
    private const string HerCorner = "/map?oracle=1&ashore=1";

    /// <summary>#1016 · The captain sat down at a bar table with the day's paper on it. One cheat, because
    /// the seated news panel is drawn by the SEAT and not by the berth.</summary>
    private const string BarCase = "/map?barcase=1";

    /// <summary>#997 wave 10 · The free-flying world with muscle already sent at her, so the tactical UI has
    /// something to point at. The dossier is the collector's, and there is no collector by default.</summary>
    private const string HerDossier = FreeFlying + "&target=collector";

    /// <summary>
    /// THE THREE WORLDS THIS REGISTER BOOTS FOR ITSELF, and why each is not simply a sixth row of
    /// <see cref="EveryDeskBootsTests"/>' matrix.
    ///
    /// <para><b>Why this table exists.</b> The <c>World</c> docstring used to say a row's URL came from that
    /// matrix, and for three rows it had quietly stopped being true. That is a small lie with a large shape:
    /// it is the sentence a reader trusts instead of checking, and the register's own worlds are exactly the
    /// thing a wrong-world guard (this repo's fifth named bug class) hides behind. So the exceptions are
    /// written down rather than assumed, and
    /// <see cref="EveryWorldInTheRegisterComesFromTheMatrixOrSaysWhyNot"/> makes a fourth one cost an edit
    /// here.</para>
    ///
    /// <para><b>Why they are not in the matrix instead.</b> That matrix is desk-shaped — a berth, a
    /// roadstead, a ground, a free flight — and it costs eight desk×world cells per row on a
    /// <c>[SlowGate]</c> class already at 300 s. These three are card-shaped: each exists to put ONE surface
    /// on the screen, and none of them would tell that sweep anything a desk does not already answer in the
    /// world beside it. Nothing here boots unwatched, either: an off-matrix world reaches a renderer only
    /// because a row of this register drives it, which is the last thing the guard checks.</para>
    /// </summary>
    private static readonly (string Url, string Why)[] TheWorldsBeyondTheMatrix =
    [
        (HerCorner, "seats one named NPC on the deck; the matrix has no world that seats anybody"),
        (BarCase, "seats the captain AT A TABLE; the matrix's berths put her on the concourse"),
        (HerDossier, "sends muscle at her; the matrix's free flight is unhunted"),
    ];

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

        // #638 · THE VOID'S DAY-19 CARD, in this law on its first day. The ruling binds #761 hard on this
        // lane ("the player is told clearly, thrice, in escalating register") and the third telling is a card
        // that stops the game a day before it ends — exactly the sort of surface the 2026-08-24 ruling was
        // written about. Raised through its own SHIPPING verb (the galley's discipline, #1021):
        // RaiseTheLongDarkCard is the ONE place the card is built, so a fork that stopped raising it fails
        // here rather than nowhere. Free-flying, because that is the only world the void can reach a captain
        // in — DeathNarration.CanHappen allows the cause on her own deck and nowhere else.
        new("the void's one-day-left card", "view-object-backdrop", FreeFlying, Exit.AControl,
            b => b.CallOnTheDispatcher("RaiseTheLongDarkCard")),

        // #1021 · THE GALLEY, WHICH IS A POP-UP NOW AND SO ARRIVES IN THIS LAW ON ITS FIRST DAY. Owner, of
        // the full-screen desk it replaces: "this UI MUST GO!… keep the features but I want it done in
        // pop-up style like the work the case is." A desk answered to nobody here; a card answers to the
        // ruling of 2026-08-24 like every other card, and it is raised through the SHIPPING desk switch
        // rather than by poking `_galleyCardOpen` — SwitchDesk is documented as the one place a desk switch
        // happens, all three of the card's doors funnel through it, and a fork that stopped raising the card
        // then fails in this law rather than nowhere.
        //
        // Exit.AControl and not EveryControlCloses, deliberately: "Pour a tot" is a control on this card and
        // is not a way out, which is exactly the claim the decision exception would have made falsely.
        //
        // THE OPEN VERB AND NOT THE TOGGLE, and the difference is this law's own mechanics rather than a
        // preference. Raise() runs again before EVERY press, so a driver wired to the 6-key's toggle would
        // have SHUT the card between finding its ✕ and pressing it, and reported a card whose way out does
        // nothing. (Found by running it.) The toggle is a real behaviour and it is proved next door, by
        // typing the key: TheGalleyIsACardNotADeskTests.
        new("the galley card — the news wire and the rum locker", "view-object-backdrop", Docked,
            Exit.AControl,
            b => b.CallOnTheDispatcher("OpenGalleyCard")),

        // #1052 · THE PAPER AT THE TABLE — in this law on its first day, and the first surface in the
        // register that answers it WITHOUT a scrim. Owner: "Just hope there is a way to have both the
        // newsfeed and pop-up-bar walkers visible UI at the same time", so the panel is docked beside a live
        // room rather than raised over a dimmed one. That is a reason to be in this register and never a
        // reason to be out of it: a surface a captain raised is a surface a captain must be able to put down.
        //
        // The world is #1016's own dev row — a free top in a docked berth's bar — because the verb is
        // SEAT-tied and the panel does not exist without a chair under it. Raised through the SHIPPING open
        // verb, and the OPEN and not the toggle, for the mechanical reason written on the galley's row: this
        // law re-raises before every press, and a driver wired to a toggle would shut the panel between
        // finding its way out and pressing it.
        //
        // Exit.AControl: its ✕ is the shell's own dismiss, and the KEY is proved next door by typing it
        // (TheNewsIsASeatVerbTests.EscapeTakesThePaperFirstAndTheChairSecond).
        new("the paper at the table — the docked news panel", "seated-news", BarCase,
            Exit.AControl,
            b => b.CallOnTheDispatcher("OpenSeatedNews")),

        // #949 · THE PLOTTING CARD, in this law on its first day and raised through its SHIPPING open verb
        // rather than by poking the gate — the galley's own discipline (#1021): all three of its doors (the
        // toolbar ?, the ? key, the Escape rung) funnel through OpenNavHelp, so a fork that stopped raising
        // the card fails here rather than nowhere. The OPEN and not the toggle, for the mechanical reason
        // written on the galley's row: Raise() runs again before every press, and a driver wired to a toggle
        // would shut the card between finding its way out and pressing it.
        //
        // At Nav, because Nav is the desk whose toolbar carries the ? — the card itself hangs over any desk,
        // but a guard should ask its question in the room the player is standing in.
        //
        // Exit.AControl: its two <a> links are not controls this law can press (no onclick), and its way out
        // is the shell's own dismiss. The KEY is proved next door, by typing it —
        // TheHelpCardTeachesTodaysPanelTests.
        new("the plotting help card", "view-object-backdrop", Docked, Exit.AControl,
            b => b.CallOnTheDispatcher("OpenNavHelp"), At: ShipDesk.Nav),

        // #997 wave 12 · THE STATION ORACLE'S CARD — the surface #1170 found had no row of its own.
        //
        // It was never unregistered: it wears `deck-offer-card`, which the arrival-brake row registers, so
        // both completeness guards have always covered it. What it had never been was ASKED — the pressing
        // law had no row to raise it from, and unlike the eleven undriven rows nothing SAID so, because a
        // reason is a field on a row and there was no row. #1170 found it while re-running #992's audit,
        // through the paren-aware class scan: her root class is one arm of a ternary (the hush is a class
        // on the ROOT), which is the shape the old regex could not read.
        //
        // Raised through the SHIPPING verb, and it is the E-key's own: TalkToOracle is where the BarPatron
        // console routes, so a fork that stopped opening her corner fails here rather than nowhere. It is
        // also idempotent in the way this law needs (the galley's discipline, #1021): it sets `_oracleOpen`
        // true and draws her opening rant only when `_oracleLine` is null, so re-raising between presses
        // cannot shut the card the way a toggle would.
        //
        // Exit.AControl and NOT EveryControlCloses, established by pressing rather than assumed: 🌀 Keep
        // listening turns the dial one line on and 🥃 Buy her a drink widens the channel, and both leave
        // her card standing. `Done` — the shell's own dismiss (#997 wave 9) — is the way out.
        new("the station oracle's card (Solenne \"Static\" Marsh)", "deck-offer-card", HerCorner,
            Exit.AControl,
            b => b.CallOnTheDispatcher("TalkToOracle")),

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

        // ── The rows that were named and not asked, and are asked now. ───────────────────────────
        // #997 wave 12 · THIS SECTION USED TO BE HEADED "in the register, not yet driven". It is not any
        // more, and the story of how it emptied is the one thing worth keeping from it: ELEVEN rows sat
        // here, each with a sentence naming the world this bench could not build, and TEN of those eleven
        // sentences named the right gate and the wrong obstacle. What they had in common is that nobody had
        // gone and looked — a reason written on the day a row is added is a guess about the future, and it
        // is believed for exactly as long as nothing presses it. That is the same shape as the false
        // EveryControlCloses claims this wave also found: the register is where claims go to be proved, and
        // a claim that a row cannot be proved is still a claim.
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
        // #997 wave 12 · DRIVEN NOW, and the old reason was half right in the way that matters. The GATE
        // is a field (`_brakeGate`), so poking it to Asking always drew the card — that was never the
        // problem. The problem was the ANSWER: FireArrivalBrake returns early when BrakeWindowBody() is
        // null, so under a poked gate "🔥 Fire the brake" LOOKS like a control that is not a way out, and
        // this row's EveryControlCloses would have failed on a world that was simply wrong rather than on a
        // bug. The guard next door says exactly that and asks the Fire half in SOURCE instead
        // (TheDossiersFileScrollsUnderItsHeadAndTheDeckCardsTakeTheShellTests).
        //
        // What nobody had looked at is what BrakeWindowBody() actually reads: `_brakeArrivalBodyId`, a plain
        // string field that the arrival writes, resolved against the ephemeris the boot already built. Name
        // a body that IS in that ephemeris and Fire runs its whole path — so the window is reachable, and
        // the ByDecision claim can be held the only honest way, by pressing both answers.
        //
        // The gate is set through the SHIPPING transition rather than by constructing a Phase:
        // ArrivalBrake.Advance(Closed, windowOpen: true) is the per-frame law's own answer to "the window
        // just opened", so a rule change that stopped opening the ask fails here rather than nowhere.
        //
        // At Nav, because the card is gated on `!_deckMode` and SwitchDesk(Deck) — this register's default
        // — puts the captain on the walkable deck. Found by running it: at the Deck the driver raised
        // nothing at all, which is the wrong-world complaint doing its job.
        new("the arrival-brake card", "deck-offer-card", FreeFlying, Exit.EveryControlCloses,
            b =>
            {
                b.Poke("_brakeArrivalBodyId", "luna");   // a body the FreeFlying ephemeris really carries
                b.Poke("_brakeDestName", "Luna");
                b.Poke("_brakeGate", ArrivalBrake.Advance(ArrivalBrake.Gate.Closed, windowOpen: true));
            }, At: ShipDesk.Nav),
        // #997 wave 2 · DRIVEN NOW, and the reason it could not be before was half right. Walking her across
        // a floor is genuinely out of reach off-browser — but her CARD is gated on one field, and the law's
        // question is about the card. So the row raises the card the same way the walk-in guard raises her
        // mid-crossing state, and the ruling is proved of her by pressing rather than believed from a list.
        // Her exit moved from EveryControlCloses to AControl in the same breath: #997 gave her the shell's
        // dismiss, which is a way out that is not one of her two answers.
        new("the walk-in HOSTED card", "view-object-backdrop", Docked, Exit.AControl,
            b => b.Poke("_walkInCard", (WalkIn.Who?)WalkIn.Who.Ilse)),
        // #997 wave 12 · DRIVEN NOW — and driving it settles a claim that has been FALSE IN THIS REGISTER
        // SINCE #997 FIXED IT IN THE MARKUP. RepCard.razor's own comment says so out loud: "#992's register
        // has this card down as a critical-DECISION modal … and that row was never driven, so nothing ever
        // pressed it. Read AnswerTheRep: THREE of his six moves leave the card up." #997 gave him a `Step
        // away` — the shell's Close — and nobody came back to move the row off EveryControlCloses, because
        // an undriven row is a row nothing can contradict.
        //
        // Pressed, it is exactly what that comment predicted: `Not today` and `Step away` end the card;
        // buying a tier refreshes the pitch to the tier you now hold, and "I already have a policy" sets his
        // reply beside it — both leave him standing. So the honest Exit is AControl, which is the same
        // correction the walk-in's row took in wave 2, for the same reason.
        //
        // Raised the walk-in's way: the card is a field, and this law's question is about the card. The
        // CONTENT is still the shipping content — NebulaRep.PitchFor is the one place a pitch is built and
        // all three of his seams call it. His crossing of the floor is proved next door, on its own legs
        // (TheRepCrossesTheFloorTests, TheSalesmanWorksTheRoomTests).
        new("the rep's pitch (Harlan Fess)", "view-object-backdrop", Docked, Exit.AControl,
            b => b.Poke("_repCard",
                        (NebulaRep.RepPitch?)NebulaRep.PitchFor(InsuranceTier.Basic, "Vane"))),
        // #997 wave 12 · DRIVEN NOW. `Adrift` is not a verdict on a live sim at all — it is
        // `_reactionMassPulses == 0 && !_docked` (Map.Sim.cs), two fields; and a guard next door has been
        // emptying the tank of a flying ship to raise this very card since wave 5
        // (TheShellOwnsTheBeatCardsTests.TheTowOfferOffersNoWayOutButAnAnswerAndBothAnswersAreOne). The
        // reason on this row was never re-read against the code it described.
        //
        // At Nav, because the whole block it lives in is gated on `_activeDesk == ShipDesk.Nav`.
        new("the rescue / tow offer", "rescue-backdrop", FreeFlying, Exit.EveryControlCloses,
            b =>
            {
                b.Poke("_reactionMassPulses", 0);   // …and FreeFlying is not docked: that is Adrift
                b.Poke("_showRescueOffer", true);
            }, At: ShipDesk.Nav),
        // #997 wave 12 · DRIVEN NOW. `LoudPlanAlarm` reads no clock: it is the standing shape alarm or the
        // standing arrive alarm, whichever is undismissed (Map.Plot.CastOff.cs) — four fields, with the
        // shape one outranking the other, and the shape one is the arm this row raises. What is a live read
        // is the PLAN CHECK that sets `_shapeAlarm`; what this law asks about is the card the alarm draws.
        //
        // The alarm's own voice is the page's and not this test's: LoudPlanAlarmHail and LoudPlanAlarmQuote
        // are chosen by which arm is standing, so what is poked here is only the break's summary line.
        new("the loud plan alarm", "rescue-backdrop", FreeFlying, Exit.AControl,
            b =>
            {
                b.Poke("_shapeAlarm", "step 2 · a burn this ship cannot make");
                b.Poke("_shapeAlarmDismissed", false);
            }, At: ShipDesk.Nav),
        // #997 wave 12 · DRIVEN NOW. The staging the old reason asked for is real — KnockOnHatch wants the
        // captain standing at the ONE hatch a crack job named, at the station that gave it — but all of that
        // is the ROAD to the pad, and the pad is gated on `_pinJob` alone. Its header reads
        // `_pinHatch?.Label ?? "LOCKED HATCH"`, which is the markup itself saying that the hatch is
        // decoration to this question and the job is not.
        //
        // The pin is set and the entry cleared on every raise, so pressing a digit key cannot leave four of
        // them behind and pop the hatch out from under a later press.
        new("the hatch keypad", "pin-backdrop", Ashore, Exit.AControl,
            b =>
            {
                b.Poke("_pinJob", new Map.Quest("pop-up-law", Map.QuestKind.Crack, "the Fixer", "V-06", "",
                                                "crack the bonded stores", "key it in", 0, Pin: "1234"));
                b.Poke("_pinEntry", "");
            }),
        // #997 wave 12 · DRIVEN NOW, through the celebration — the member of the family whose whole content
        // is Core data (MissionCelebration + Celebrations.GiverThanks), so nothing past the gate is invented
        // here. The old reason counted the gates correctly and drew the wrong conclusion from the count: a
        // root class shared by several surfaces is a reason to raise exactly ONE of them, which is what the
        // convergence band's rows have done since day one — never a reason to raise none.
        //
        // The siblings need no putting down, and that is a fact about the world rather than an omission:
        // nothing in the Ashore boot raises any of them, so a fresh bench has the root class to itself. The
        // family's other surfaces are pressed one by one next door (TheShellOwnsTheBeatCardsTests).
        new("the mission celebrations, briefs and reveals", "mission-celebration-backdrop", Ashore,
            Exit.AControl,
            b => b.Poke("_celebration", (MissionCelebration?)new MissionCelebration(
                "THE ICE RUN", "Madam Coil", Celebrations.GiverThanks("Madam Coil"), 4_200, 3,
                "the bird sings the payday"))),
        // #997 wave 12 · DRIVEN NOW. The away lane is what FILLS this card; it is not what DRAWS it —
        // `_expeditionRevealCard` is a record of four values and the gate is that field being non-null. The
        // panel wears BOTH `mission-celebration-backdrop` and its own root, which is why the family is
        // registered twice over and why each row raises its own surface on its own bench.
        new("the expedition reveal", "expedition-reveal-backdrop", Ashore, Exit.AControl,
            b => b.Poke("_expeditionRevealCard", (Map.ExpeditionRevealCard?)new Map.ExpeditionRevealCard(
                ExpeditionSiteKind.MysticalRuins, "THE LEANING MAST",
                "it was never a mast", "the ground remembers what stood on it"))),
        // #997 wave 12 · DRIVEN NOW, and this is the one row whose old reason was TRUE and still left it
        // sitting: FreeFlying IS a wreck alongside, and `_ventReads` is a dictionary a driver may write. The
        // reading itself is Core's own HullVenting.Read of a Space, so what the card prints is a real roll
        // against a real compartment rather than a hand-made LifeSign.
        new("the operating-log card", "vent-read-backdrop", FreeFlying, Exit.AControl,
            b =>
            {
                const string room = "THE PUMP ROOM";
                var reads = (Dictionary<string, (DiceRoll Roll, HullVenting.LifeSign Sign)>)
                    b.Peek("_ventReads")!;
                reads[room] = HullVenting.Read(
                    7, new HullVenting.Space(room, DoorShut: true, Vented: false, Infested: true,
                                             HoldsSurvivor: false));
                b.Poke("_ventReadCard", room);
            }),
        // #997 wave 12 · DRIVEN NOW, through the CHOOSER — the member of this family whose gate is a list
        // the page hands itself. A pointer hit is how that list gets BUILT in play; it is not what the menu
        // is drawn on, and the law's question is about the menu. The candidate is the page's own sky row,
        // character for character (Map.Sim.Controls.cs).
        //
        // Its rows are answers that do end it, and its ✕ is the shell's — which is the distinction
        // PickMenuPanel's own comment argues at length: a menu is not a question, so "none of them" has to
        // stay sayable. Exit.AControl, and pressing finds all three controls ending it, which reads that
        // comment's rule the strong way round rather than the weak one.
        new("the pick-candidate chooser and the three context menus", "map-body-menu", Docked,
            Exit.AControl,
            b => b.Poke("_pickMenu",
                        new List<Map.PickCandidate> { new('K', "", "scan this patch of sky", "🔭") }),
            At: ShipDesk.Nav),
        // #997 wave 11 · DRIVEN NOW. The reason this row carried was true about the CARD and wrong about the
        // road to it: the tray is gated on a DiceEvent handed down from a roll, and #305 wrote exactly one
        // seam for handing one down — RaiseDiceEvent, the entry every dice-scripted system is meant to
        // adopt. So the driver takes that seam rather than poking `_diceTrayEvent` behind it, which is the
        // stronger of the two: a shared entry that stopped reaching the tray fails in this law rather than
        // nowhere. Nothing about the cast needs a sim — a DiceEvent is pure Core data.
        //
        // It is also the first row on this list whose surface is drawn by a CHILD COMPONENT, and the law
        // needed nothing new to reach it: DeskBench's walk follows a Component frame into its own tree, so
        // the tray's card has always been visible to guards 1 and 2. Only the driver was missing.
        new("the dice tray", "dice-tray", Docked, Exit.AControl,
            b => b.Call("RaiseDiceEvent",
                        TheChecklistAndTheTrayTakeTheShellAndEscapeClosesTheMenusTests.ARoll())),
        // #997 wave 12 · DRIVEN NOW, and the old reason had the horizon right and the gate wrong. The board
        // is drawn on `_shuttleBayStops is { }` — a LIST, possibly an EMPTY one — and not on that list
        // holding anything: ShuttleBayCard's own note says the hatch reports its reach honestly and that
        // nothing in range does not keep the door shut. The bench's documented empty reach is therefore a
        // thing this card exists to say, not a wall in front of it.
        //
        // Raised through the SHIPPING verb: OpenShuttleBayDoor is where the bay hatch's interaction lands,
        // so a fork that stopped opening the door fails here rather than nowhere — and it asks
        // ShuttleDestinationsInRange for real, so whatever the reach is at this berth is what the card gets.
        // (At the Tilt it comes back with Miranda in it, which is worth knowing: the bench's horizon is
        // `?land=1` with no `?dock=`, and this row names its berth.)
        //
        // THE LOAD-OUT IS PUT DOWN FIRST, and that is the start-picker's lesson arriving at a second family
        // rather than a tidiness. Two surfaces wear `.deck-shuttle-card` — the board and the load-out — and
        // BOARDING one of the board's rows is what raises the other (`_shuttleBayStops = null; _boardTarget
        // = stop` — the panel replaces the board). So the law pressed "🛸 Board for Miranda", re-raised,
        // pressed "Close hatch", and reported a hatch that would not close, when what was left on the screen
        // was the load-out the earlier press had opened. Read that failure the way the logbook's row says to
        // read its own: the law was right that something wearing that class was still up.
        new("the shuttle-bay hatch and the load-out", "deck-shuttle-card", Ashore, Exit.AControl,
            b =>
            {
                b.Poke("_boardTarget", null);   // the OTHER surface on this root — see above
                b.CallOnTheDispatcher("OpenShuttleBayDoor");
            }),
        // #997 wave 12 · DRIVEN NOW. Walking into the view is out of reach off-browser and always will be;
        // the NUDGE it leaves behind is `_selfieOffer`, three strings, and that nudge is the surface the
        // ruling is about.
        //
        // Its EveryControlCloses survives the pressing, and one detail is said out loud here rather than
        // discovered later: `Take the shot` ends in RendererInterop.PlayCue, a [JSImport] and therefore
        // DeskBench's documented browser gate — but it throws AFTER `_selfieOffer` is cleared, so the card
        // really is gone, and this law reads the render, which is the whole of its question. The throw lands
        // in the renderer's error channel, which this law does not read; the guard that DOES read it names
        // this same press as the one it cannot make
        // (TheDossiersFileScrollsUnderItsHeadAndTheDeckCardsTakeTheShellTests).
        new("the selfie offer", "selfie-offer", Ashore, Exit.EveryControlCloses,
            b => b.Poke("_selfieOffer",
                        (Map.SelfieOffer?)new Map.SelfieOffer("beat-the-long-fall", "the long fall", ""))),
        // #997 wave 10 · DRIVEN NOW, and by a URL rather than by a poke. The reason this row sat undriven
        // was true when it was written — the dossier is gated on a tactical target, and the only two roads
        // to one were a contact in sensor reach or a collector bought by a robbery, neither of which a URL
        // could reach. `?target=` is that road (Map.Npc.SeedTargetCheat), so the WORLD raises the card and
        // the driver's whole job is to put it back up between presses: the law re-raises per control, and
        // the ✕ it presses first drops the target the boot set.
        //
        // The driver calls the SHIPPING cheat rather than poking `_interestTargetId`, which is the stronger
        // of the two: a cheat that stopped raising this card would fail here rather than nowhere.
        new("the target dossier", "map-dossier", HerDossier, Exit.AControl,
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
            + "SpawnHunterForHeatEvent found nothing policed within reach of the wreck — which would be a "
            + "world change worth knowing about — or the cheat has stopped calling it.");
        bench.CallOnTheDispatcher("SeedTargetCheat", muscle[0].Id);
    }
}
