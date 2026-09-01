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
        // #1038 · THE PEEK, AND IT IS FIRST BECAUSE IT IS THE ONLY THING THE CAPTAIN CAN SEE.
        //
        // Owner, verbatim: "I thing esc-key should end the peek." It goes in this chain rather than beside it
        // in OnKeyDown for the reason the chain exists — Escape has one meaning, "take the thing I am looking
        // at off my screen", and a second Escape handler somewhere else is how a key ends up doing two things
        // and lying about one of them (#997 wave 11, the click menus, where the fall-through moved the captain
        // to another desk and left the menu's gate set).
        //
        // AT THE TOP, above the 1420 band, and the argument is #1027's own. That entry moved the first-ground
        // family to the head of the chain because Esc over a VISIBLE ground lesson was peeling an INVISIBLE
        // story card underneath it. Peek is that fact taken to its limit: while it is on, EVERY surface listed
        // below is at opacity 0 and visibility hidden, so any line above this one would be a blind dismissal
        // of a card the captain cannot see — spending a told-once beat he never read. The peek is the mode he
        // is in and the only thing on the glass; ending it is the one thing the key can honestly mean. Press
        // Escape twice and the second press peels the card the first press gave him back, which is the right
        // two-step and the only one he can watch happen.
        if (_peekMap) { EndPeekMap(); return true; }
        // #735 · The told-once cards — the convergence reveal and the first-ground family (the lesson, the
        // map-just-grew card, the tube rearm, the low-air warning). Every one of them already dismisses on
        // a backdrop click and carries its own way-out button, so dismissal is allowed here and Esc was
        // simply never wired to them; a card that takes the screen and ignores the cancel key is the
        // #351 complaint again, one lane over.
        //
        // #1027 · THEY LEAD THE CHAIN NOW, AND THE REASON IS A NUMBER RATHER THAN A FEELING. They are the
        // only family in this method drawn on `.convergence-backdrop` (z 1420, the Modal band), so they
        // paint over every other card listed below — the story card included. They were listed SECOND, under
        // #528's "the story card is the most modal thing there is", which was true on the day it was written
        // and stopped being true when the first-ground family got its own band: Esc over a visible ground
        // lesson peeled an invisible story card underneath it and left the lesson sitting there. That is the
        // same shape as the bug this issue is about, one family over, so the same pass fixes it. THE ORDER
        // OF THIS CHAIN IS PAINT ORDER, TOP DOWN, and now it is that all the way through.
        if (_convergenceRevealOpen) { CloseConvergenceReveal(); return true; }
        if (_groundLessonOpen) { CloseGroundLesson(); return true; }
        if (_groundGrewOpen) { CloseGroundGrew(); return true; }
        if (_tubeRearmOpen) { CloseTubeRearm(); return true; }
        if (_airCardOpen) { CloseAirCard(); return true; }
        // #1027 · THE POCKET, AND IT IS THE FIRST TIME THE CANCEL KEY HAS REACHED IT AT ALL.
        //
        // The satchel was never in this chain: it closed on I, on its own ✕ and on its backdrop, which
        // satisfied the pop-up law (#992) and left Esc falling straight through it to whatever card was
        // underneath. Now that the pocket paints ABOVE those cards (OverlayBands.SatchelBackdrop, 1330) that
        // fall-through is no longer merely a gap — it is the bug this issue names, running backwards: Esc
        // would close the arrival card the captain cannot see and leave the satchel he is looking at.
        //
        // HERE, and not higher: the five cards above are 1420 and genuinely do cover the satchel, so they go
        // first. Everything below is 1330 or under and the satchel covers it. One line, one place, and it
        // reads off the same z-order the stylesheet does.
        if (_showSatchel) { CloseSatchel(); return true; }
        // #528: the story card opens without being asked for, over whatever the captain was already doing (a
        // bar menu, a counter, a dig) — so it leads every card the captain went and GOT. The PLATE (the edge
        // flash) is deliberately NOT listed: it steals nothing and retires itself, so there is nothing for
        // Esc to take.
        //
        // #664 · There were TWO lines here, the reveal card's and the story card's, because the fork built
        // the same card twice. There is one card now and one line.
        if (_storyCard is not null) { CloseStoryCard(); return true; }
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
        // #1021 · THE GALLEY CARD. Here, with the cards the captain RAISED — above the click menus, which are
        // the least modal thing in this chain, and below every card that opens without being asked for (a
        // story beat, a death, a question about the seat you are in), because a card you went and got is
        // never the loudest thing on the glass.
        //
        // It is in this chain and deliberately NOT in the Enter chain below. That chain's discipline is
        // "only cards that ask nothing", which this card passes — but its second half is that Enter presses
        // "the visible primary action of a card that has EXACTLY ONE", and this one has two: a way out and a
        // tot of rum. A key that poured the rum for the captain would be the forbidden shape (#735), and a
        // key that closed the card past a button it could equally have pressed would be a coin toss.
        if (_galleyCardOpen) { CloseGalleyCard(); return true; }
        // #949 · THE PLOTTING CARD, beside the galley card and for the galley card's own reason: it is a
        // card the captain went and GOT — he pressed ? — so it is never the loudest thing on the glass and
        // it sits below every card that opens without being asked for. Above the click menus, which are the
        // least modal thing in this chain.
        //
        // Its rung matters more than most, and the reason is what the card IS. A player reaches for it
        // because he is confused; the key he then reaches for is Escape, and a help card that ignored the
        // cancel key would be teaching, by its own behaviour, the opposite of everything it says. Its own
        // way out and the ? that raised it both close it too — three roads out, which for this card is the
        // right number.
        if (_navHelpOpen) { CloseNavHelp(); return true; }
        if (_showRescueOffer) { _showRescueOffer = false; return true; }
        if (_celebration is not null) { DismissCelebration(); return true; }
        // #997 wave 11 · THE FOUR CLICK MENUS — FABLE'S RULING, WAVE 11.
        //
        // #1012 migrated them into one mechanism and reported, without changing anything, that this is the
        // one family this chain has never listed: every other card in the client obeys the cancel key, and
        // these four sat there ignoring it. That is #351's own complaint — the owner's, verbatim, "No way
        // to close this dialog? Where is cancel?" — one family over, and the owner's standing pop-up ruling
        // (2026-08-24) plus plain consistency decide it. They join the law.
        //
        // WHAT IT ACTUALLY FIXES, and it is worse than "a key did nothing". Escape's fall-through is
        // SwitchDesk(Nav), so pressing it over an open menu at the Sensors desk MOVED THE CAPTAIN TO A
        // DIFFERENT DESK and left the menu's gate set. Which of the two bad endings you got depended on
        // which menu it was, and both are measured in the guard (delist a line and watch):
        //
        //   * the chooser, the body menu and the contact menu draw on Nav too, so they FOLLOWED him there —
        //     still wearing the inline anchor of a click he had made on another desk's map;
        //   * the open-sky menu draws on Sensors ONLY, so it vanished off the glass with `_skyMenuWorld`
        //     still holding a point — and came straight back the moment he returned to Sensors.
        //
        // The key did not do nothing. It did something else, and then lied about it.
        //
        // LAST IN THE CHAIN, because a click menu is the least modal thing in it: it is a list hanging off
        // a spot on the map, and anything else that is open is over it. Among the four the order is REVERSE
        // PAINT ORDER — the sky menu is written last in Map.razor and therefore drawn on top of the three
        // above it, so it is peeled first. In practice at most one is ever up (a pointer-down anywhere
        // closes the others, Map.Sim.Controls; choosing from the chooser clears the chooser on the way into
        // the menu it opens), so this order is a rule rather than a workaround — but "topmost first" has to
        // mean one thing in this file, and paint order is the only honest reading of it.
        //
        // Each calls the menu's OWN house closer, which is what the ✕ the shell draws calls too. They are
        // deliberately absent from the Enter chain below: a menu is a LIST of things the captain may do,
        // and a key that picked one of them for him is the exact thing that chain refuses to do.
        if (_skyMenuWorld is not null) { CloseSkyMenu(); return true; }
        if (_shipMenuId is not null) { CloseShipMenu(); return true; }
        if (_bodyMenuBody is not null) { CloseBodyMenu(); return true; }
        if (_pickMenu is not null) { ClosePickMenu(); return true; }
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

        // #1027 · THE POCKET STOPS THIS CHAIN AND PRESSES NOTHING, and it stops it ABOVE the stand-up
        // confirm as well as above the cards.
        //
        // It is not a row here — it is a PAGE of many controls (rip, bin, offer, turn to the notebook)
        // rather than a card with exactly one visible action, which is this key's founding refusal. But
        // falling THROUGH it would be worse than doing nothing. Below this line sit an arrival card Enter
        // would acknowledge and a seat Enter would stand the captain out of, and the pocket (1330) paints
        // over both: the key would spend a beat or take a chair while the only thing on the screen did not
        // move. Above the #784 exception on purpose — that exception is justified by the captain having
        // just asked the question with his own hand on the keyboard, and a satchel opened over the confirm
        // is a captain who has since gone and done something else.
        if (_showSatchel) { return false; }
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
        //
        // #1027 · The reorder above is mirrored here for the same reason the sentence gives: the told-once
        // family paints at 1420 and the story card at 1320, so a key that pressed the story card's way on
        // while a ground lesson stood over it would be answering a card nobody can see.
        if (_convergenceRevealOpen) { CloseConvergenceReveal(); return true; }
        if (_groundLessonOpen) { CloseGroundLesson(); return true; }
        if (_groundGrewOpen) { CloseGroundGrew(); return true; }
        if (_tubeRearmOpen) { CloseTubeRearm(); return true; }
        if (_airCardOpen) { CloseAirCard(); return true; }
        if (_storyCard is not null) { CloseStoryCard(); return true; }
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
