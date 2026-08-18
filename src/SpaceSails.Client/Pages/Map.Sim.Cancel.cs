using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Sim (#870 split; the header note lives in Map.Sim.cs) — #351's Esc and Enter chains — which of the open cards a cancel or a confirm reaches first.
public partial class Map
{

    // #351 — the audit's keyboard cancel path: dismiss the top-most open deck/flight overlay, reusing
    // each card's existing house closer (a ✕/Cancel/Done button already lives on every one of these). The
    // order is most-modal first, so a stacked moment (a contact-drink offer sitting atop the bar menu)
    // peels one layer at a time. Deliberately EXCLUDED: the shuttle boarding panel (_boardTarget — another
    // lane is reworking it) and the save/start drawers (the scenario-starts region keeps its own chrome).
    // Returns true when it consumed the key by closing something.
    private bool TryDismissTopOverlay()
    {
        // #528: the story card is the most modal thing there is — it opens without being asked for, over
        // whatever the captain was already doing (a bar menu, a counter, a dig). Esc takes it FIRST, or the
        // key would peel the card underneath it and leave the picture sitting there. The PLATE (the edge
        // flash) is deliberately NOT listed: it steals nothing and retires itself, so there is nothing for
        // Esc to take.
        //
        // #664 · There were TWO lines here, the reveal card's and the story card's, because the fork built
        // the same card twice. There is one card now and one line.
        if (_storyCard is not null) { CloseStoryCard(); return true; }
        // #735 · The told-once cards — the convergence reveal and the first-ground family (the lesson, the
        // map-just-grew card, the tube rearm, the low-air warning). Every one of them already dismisses on
        // a backdrop click and carries its own way-out button, so dismissal is allowed here and Esc was
        // simply never wired to them; a card that takes the screen and ignores the cancel key is the
        // #351 complaint again, one lane over.
        if (_convergenceRevealOpen) { CloseConvergenceReveal(); return true; }
        if (_groundLessonOpen) { CloseGroundLesson(); return true; }
        if (_groundGrewOpen) { CloseGroundGrew(); return true; }
        if (_tubeRearmOpen) { CloseTubeRearm(); return true; }
        if (_airCardOpen) { CloseAirCard(); return true; }
        // #784 · The stand-up confirm sits ABOVE the table it is asking about, and Esc means KEEP YOUR SEAT.
        // Owner: "one press confirms, Esc keeps you seated." Listed here rather than under the table so the
        // cancel key cannot answer the question by doing the thing the question is about.
        if (TheStandUpConfirmIsUp) { KeepYourSeat(); return true; }
        // #746 · The table. Above the bar cards for the reason the whole scene turns on: LEAVING IS FREE and
        // always available, and a keyboard cancel that could not reach the one panel whose design law is
        // "you may always stand up" would be the game contradicting itself with a keystroke.
        // #784 phase two · AND ESC ASKS BEFORE IT TAKES THE SEAT. Once the frame stopped dimming the room,
        // "cancel" stopped being a way OUT of a card — there is no card — and became a press that silently
        // spends the watch you sat for and the breath you got back. So a docked seat routes the cancel key
        // into the confirm, and standing up stays one decision taken once (#788). A CONVERSATION keeps the
        // old behaviour exactly: leaving is free and always available, and a card you cannot Esc out of would
        // be the game contradicting its own law.
        //
        // #847 · This is the confirm's LAST raiser, and it is the right one. WASD used to arrive here too;
        // the owner ruled that a movement key is a decision already taken ("must stand up before walking"),
        // so the keys pay for the stand instead of asking about it. Esc says nothing about where the captain
        // is going, which is exactly why it is still worth a question.
        if (SeatedIsDocked) { AskWhetherToStandUp(); return true; }
        if (CaptainIsSeated) { CloseTable(); return true; }
        if (_pendingContactDrink is not null) { CancelContactDrinkOffer(); return true; }
        if (_patronDrink is not null) { ClosePatronTable(); return true; }
        if (_pendingOffer is not null) { DeclineOffer(); return true; }
        if (_bankSession is not null) { CloseBank(); return true; }
        if (_barMenu is not null) { CloseBarkeep(); return true; }
        // #425 · The oracle's corner card was the ONE bar card this chain never knew about (story pass
        // 2026-08-02). She belongs to the same mutually-exclusive doorway family as the counter and the
        // patron's table — both of which open by shutting her — so Esc peeled every card in the bar except
        // hers, which sat there ignoring the key while everything else obeyed it. Her ✕ was always the
        // "Done" button; this just lets the house key close her too (#351's family).
        if (_oracleOpen) { CloseOracle(); return true; }
        if (_shuttleBayStops is not null) { CloseShuttleBayDoor(); return true; }
        if (_pinJob is not null) { CancelPin(); return true; }
        if (_expeditionRevealCard is not null) { _expeditionRevealCard = null; return true; }
        if (_expeditionBriefCard is not null) { _expeditionBriefCard = null; return true; }
        if (_treasureMapCard is not null) { _treasureMapCard = null; return true; }
        // #488: the operating-log card sits ON TOP of the valve board, so Esc must take it first — the
        // board underneath is still the thing the captain came to use.
        if (_ventReadCard is not null) { CloseVentReadCard(); return true; }
        if (_showVentPanel) { CloseVentPanel(); return true; }
        // The vision card sits above everything on a wreck — it is the loudest thing that can happen in that
        // hold, and it opens without being asked for.
        if (_archiveCard is not null) { CloseArchiveCard(); return true; }
        if (_wreckLook is not null) { CloseWreckLook(); return true; }
        if (_wreckOutcome is not null) { DismissWreckOutcome(); return true; }
        if (_showWreckChoice) { CloseWreckChoice(); return true; }
        if (_kioskCard is not null) { CloseKioskCard(); return true; }
        // #836 · The wallet, fanned while a guard crosses the floor. Esc means KEEP THE ONE YOU HAVE — the
        // #784 discipline, one system along: the cancel key may shut the question but it may never answer
        // it, and the paper already in the hand is not a choice this key made. It is deliberately NOT in the
        // Enter chain below for the same reason: a fan is a question, and Enter answers only cards that ask
        // nothing.
        if (WalletFanIsUp) { CloseTheWalletFan(); return true; }
        if (_viewObject is not null) { CloseViewObject(); return true; }
        if (_showRescueOffer) { _showRescueOffer = false; return true; }
        if (_celebration is not null) { DismissCelebration(); return true; }
        return false;
    }

    // #735 · THE KEYBOARD'S WAY ON. Esc above is the keyboard CANCEL; this is the keyboard YES — Enter
    // presses the visible primary action of a card that has exactly one.
    //
    // It exists because of the bug that named the issue: a story card grew taller than the screen, its one
    // button rendered below the fold, and the player was stuck on it until they resized the browser. The
    // card family's layout law (Map.razor.css, #735) keeps that button on the screen; this is the second
    // road to the same button, and the one a keyboard — or a test harness — can take.
    //
    // Two disciplines, both of them refusals:
    //
    //   * ONLY CARDS THAT ASK NOTHING. A card with SUBMIT / BRIBE / RESIST on it is a question, and a key
    //     that answers a question for the captain is worse than no key at all. Those fall through and Enter
    //     does nothing. This is where somebody will one day be tempted to add a "default" — don't.
    //   * THE DEATH CARD IS ACKNOWLEDGED, NOT DISMISSED. It is listed here and deliberately NOT in the Esc
    //     chain above: it carries no ✕ and its backdrop swallows clicks, because the game does not let you
    //     wave a death away. Enter presses the button that is already the only way on.
    private bool TryConfirmTopOverlay()
    {
        // The death card draws above everything in the game, so it answers the key before anything else.
        if (_busted is { } bust)
        {
            switch (bust.Phase)
            {
                // The freeze beat — one button, "…wake up", and it is the only road out of the sepia.
                case BustedEncounter.Stage.FreezeFrame:
                case BustedEncounter.Stage.Impact:
                case BustedEncounter.Stage.SurfaceEnd:
                    BustedResurrect();
                    return true;
                case BustedEncounter.Stage.ResistLost:
                    BustedResistLostConfirm();
                    return true;
                // The wake, the receipt, and the three ways a catch can end: one acknowledgement each. The
                // restore card (Resurrected) is the tall one this whole issue is about.
                case BustedEncounter.Stage.Resurrected:
                case BustedEncounter.Stage.Confiscated:
                case BustedEncounter.Stage.BribedOff:
                case BustedEncounter.Stage.ResistWon:
                case BustedEncounter.Stage.Fled:
                    CloseBusted();
                    return true;
                // Demand and Bolivia are questions. Enter does not answer them.
                default:
                    return false;
            }
        }

        // #784 · THE ONE QUESTION THIS KEY IS ALLOWED TO ANSWER, and the exception is worth stating rather
        // than smuggling. Every other card in this method asks nothing; the stand-up confirm asks something.
        // It is here because of WHERE IT CAME FROM: it is raised by a KEY (#847 left Esc on the docked strip
        // as its one raiser), so the captain's hands are already on the keyboard, and a confirm reachable
        // only by mouse would strand somebody who had just pressed cancel. The doing-nothing default is still
        // SEATED — Esc and every other key leave the chair where it is — so Enter confirms the thing the
        // captain just asked for rather than deciding something for them. FLAGGED for the owner: this is a
        // judgement call.
        if (TheStandUpConfirmIsUp) { StandUpFromTable(); return true; }
        // …then the same order the Esc chain reads in, so "the top-most card" means one thing in this file
        // and not two. Only the single-action cards are listed; every card that offers a CHOICE is absent
        // on purpose, and its absence is the feature.
        if (_storyCard is not null) { CloseStoryCard(); return true; }
        if (_convergenceRevealOpen) { CloseConvergenceReveal(); return true; }
        if (_groundLessonOpen) { CloseGroundLesson(); return true; }
        if (_groundGrewOpen) { CloseGroundGrew(); return true; }
        if (_tubeRearmOpen) { CloseTubeRearm(); return true; }
        if (_airCardOpen) { CloseAirCard(); return true; }
        if (_expeditionRevealCard is not null) { _expeditionRevealCard = null; return true; }
        if (_expeditionBriefCard is not null) { _expeditionBriefCard = null; return true; }
        if (_treasureMapCard is not null) { _treasureMapCard = null; return true; }
        if (_ventReadCard is not null) { CloseVentReadCard(); return true; }
        if (_archiveCard is not null) { CloseArchiveCard(); return true; }
        if (_wreckLook is not null) { CloseWreckLook(); return true; }
        if (_wreckOutcome is not null) { DismissWreckOutcome(); return true; }
        if (_kioskCard is not null) { CloseKioskCard(); return true; }
        if (_viewObject is not null) { CloseViewObject(); return true; }
        if (_celebration is not null) { DismissCelebration(); return true; }
        return false;
    }
}
