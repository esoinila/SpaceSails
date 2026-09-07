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
/// #992 · <b>EVERY POP-UP CAN BE DISMISSED.</b> Owner ruling, 2026-08-24, verbatim:
/// <i>"As a general ruling there should not be a pop-up that cannot be closed or minimized."</i>
///
/// <para>A ruling that reads like one sentence and touched eighty-nine surfaces on the afternoon it was made
/// is a ruling that needs a law rather than a sweep, because the sweep is right on that afternoon and wrong
/// by the next feature. (It was: #1169 caught #992's own tally, left in <c>OverlayDismiss</c>'s docblock,
/// counting a client that had moved. The docblock points here now and carries no number of its own.) This
/// file is the law, and it is four assertions that do four different jobs. None of them is any of the
/// others, and the third one is the only one that is expensive.</para>
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
/// <item><b><see cref="NoSurfaceOnTheShellSitsOutsideTheRecognisersSight"/> — the guard on the recogniser
/// itself.</b> Guards 1 and 2 find a pop-up by the class it wears, so a surface that follows neither the
/// house naming nor a written-down family name is not caught by them — it is not SEEN by them. This one
/// answers that with the fact that is not a matter of naming: a file rendering an <see cref="OverlayShell"/>
/// is a pop-up, because the shell exists for nothing else, and it must wear a root the recogniser knows.
/// Added when #992's audit was re-run (#1170), which is also when the ternary-classed surfaces guard 1 had
/// never been able to read turned up — see <see cref="ClassListsIn"/>.</item>
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
/// <see cref="Exit.EveryControlCloses"/> is not read from the register — the register says only which
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
/// <para><b>Reach — and as of #997 wave 12 there is none left unreached.</b> A row whose surface needs a world
/// this bench cannot build may sit in the register with <see cref="PopUp.Raise"/> null and a stated reason:
/// guards 1 and 2 still cover it, but the ruling has not been PROVED of it. <b>The list is empty today</b> —
/// every surface in the register is raised and pressed — and <see cref="TheUndrivenListOnlyEverGetsShorter"/>
/// holds it there. The number can go down without anybody's permission and cannot go up without a deliberate
/// edit to a written-down count.</para>
///
/// <para>The eleven rows that emptied out of that list are worth one sentence, because their reasons all
/// failed the same way: ten of the eleven named the right GATE and the wrong OBSTACLE. "Needs Adrift, a
/// fuel-and-velocity verdict on a live sim" described two fields; "needs shuttle stops in reach" described a
/// card built to report an empty reach; "needs LoudPlanAlarm, a read against sim time" described a string.
/// A reason written on the day a row is added is a guess about the future, and nothing contradicts it while
/// nothing presses it — which is the same shape as the two false <see cref="Exit.EveryControlCloses"/> claims
/// this wave also found sitting on undriven rows.</para>
/// </summary>
[SlowGate] // #251 · 152 s over 6 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed partial class EveryPopUpCanBeDismissedTests
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
        // #1052 · …AND THE STRIP'S SECOND PIECE, the paper it raises. Named here rather than left to the
        // structural half deliberately: it is not a `-backdrop`, because it deliberately HAS none (the
        // owner's "both the newsfeed and pop-up-bar walkers visible UI at the same time"), and a surface
        // that escaped this law by not dimming the room would be the ruling of 2026-08-24 defeated by a
        // stylesheet. A raised surface answers for its way out whether or not it took the screen to say so.
        "seated-news",        // the paper the strip raises, which is also not a card
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
}
