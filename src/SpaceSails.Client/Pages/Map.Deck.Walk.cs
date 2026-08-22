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

// Subject: part of Map.Deck (#870 split; the header note lives in Map.Deck.cs) — the legs: #875's one predicate that both grips ask, #729's clicked route, the held keys, and the frame that spends the budget through the deck's own stepper.
public partial class Map
{
    private readonly HashSet<string> _deckKeys = [];
    private const double AvatarSpeed = 9.0; // deck units per real second

    // #313: carrying a chest on the surface slows the captain (0.8×) — still faster than the Old Ones'
    // shamble (5.6), but DROPPING it (G) restores full speed. Off-surface / empty-handed = full speed.
    private double CurrentWalkSpeed => _surface is { Carrying: true } ? AvatarSpeed * CarryChestSpeedFactor : AvatarSpeed;

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  #729 · CLICK-TO-WALK. Owner, mid-playtest: "Maybe for testing purposes an automatic walk feature
    //  with point-at to walk-to using say A* might be useful. So our testing does not hang on slow MCP
    //  speed to browser." And minutes later, the part that makes it a feature rather than scaffolding:
    //  "we could disable that later in game or consider it as a feature also… like an alternative way to
    //  move to a spot even behind automatic doors."
    //
    //  The whole design fits in one sentence: THE WORLD IS NEVER TOLD IT IS BEING DRIVEN. Core's AutoWalk
    //  hands out a direction and a distance budget; every one of those sub-steps is spent through
    //  _deckPlan.Move — the same stepper WASD spends — inside the same MoveAvatar the same frame loop
    //  calls, so the air, the nerve, the tracker, the auto-doors and the Old Ones all keep running and none
    //  of them can tell the difference. A cheat that teleported, or walked faster, would un-test exactly
    //  what walking exists to test (#600's lift: reaching is not returning).
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>#729 · The live route, or null. Session-only and never saved: it is an ORDER the captain
    /// gave, not a fact about the world, and a saved game that resumed mid-walk would be moving somebody
    /// who put the controller down.</summary>
    private AutoWalk? _autoWalk;

    /// <summary>The deck the route was planned over. A route is only meaningful on the walls it was
    /// planned against, so a floor change (the lift, a landing, a wing welded on) drops it rather than
    /// walking the captain along corridors that no longer exist.</summary>
    private DeckPlan? _autoWalkDeck;

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  #875 · TWO GRIPS, ONE WALK. Owner's ruling, 2026-08-15: "click to walk should always be on when the
    //  arrows for walking are active also. The two should be linked as alternative UI methods for walking."
    //
    //  So click-to-walk is not a mode, and there is nothing left to switch on. #729 built it behind
    //  ?autowalk=1 "until the owner rules on always-on"; this is that ruling, and the flag retires to a
    //  no-op alias (Map.Sim still reads it off the query and throws the answer away, so every old dev URL,
    //  the docs table and the UiGate boot exactly as they did).
    //
    //  ONE PREDICATE, because two authors on one law is this repo's FIRST named bug class and it had
    //  already happened right here: the key handler asked CaptainIsUnderEscort inline while the click asked
    //  a property that spelled the same law a second time with a flag and a view term mixed into it — which
    //  is how ?designate=1 came to be a floor you could cross with the arrows and not with a finger.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>#875 · <b>THE ONE PREDICATE</b> — are the captain's legs their own right now? Asked by the
    /// WASD/arrow case in <see cref="HandleDeckKey"/> and by <see cref="ClickToWalkAt"/>, and by nothing
    /// else: a grip that answered this question for itself would be free to disagree with the other one,
    /// and that disagreement is the whole of #875.
    ///
    /// <para>It is deliberately NOT a question about the VIEW. The ship's own deck has legs, the regolith
    /// has legs, a haven bar has legs, and a seat is a COST rather than a refusal — #847's press buys the stand and then walks, by
    /// either grip. The only thing in this game that takes the legs away is somebody else walking you off
    /// his floor (#833's escort), and the only precondition is that there is a deck under them at all.</para></summary>
    private bool TheCaptainsLegsAreTheirOwn => _deckMode && !CaptainIsUnderEscort;

    /// <summary>#875 · …and when they are not the captain's own, the words that say so — from ONE place, so
    /// that the key which is consumed and the click which is refused say the same sentence. They are refused
    /// by the same fact; they had better be refused in the same breath.</summary>
    private string? TheHoldOnTheLegs => CaptainIsUnderEscort ? Core.PatrolBeat.EscortHeldLine : null;

    /// <summary>#875 · Is the canvas under this pointer a FLOOR? The click grip's one extra question, and it
    /// is about the VIEW rather than about the walk: on the map that canvas is the ecliptic (hit-testing a
    /// deck click against planets that are not on screen would open a body menu over a moon floor). It can
    /// never refuse a walk: whether those legs are the captain's is <see cref="TheCaptainsLegsAreTheirOwn"/>,
    /// asked inside <see cref="ClickToWalkAt"/> so a held click can SAY so instead of being swallowed by a
    /// pointer branch.
    ///
    /// <para>#958 · It used to carry a second term for the walk-in view, which drew no floor to point at.
    /// That view is gone on the owner's ruling, so the deck plan is the only canvas a finger can aim at and
    /// the question is one term again.</para></summary>
    private bool ADeckClickIsAPlaceOnTheFloor => _deckMode;

    /// <summary>Drop the route. <paramref name="tellThem"/> only when the captain did it on purpose — a
    /// route dropped because the floor changed under it needs no receipt.</summary>
    private void CancelAutoWalk(bool tellThem)
    {
        if (_autoWalk is { Active: true } route)
        {
            route.Cancel();
            if (tellThem)
            {
                ShowPulseMessage(AutoWalk.CancelledLine);
            }
        }
        _autoWalk = null;
        _autoWalkDeck = null;
    }

    /// <summary>
    /// A click on the deck canvas: turn the pixel into a place on the floor and set off walking.
    ///
    /// <para>The pixel → deck-unit conversion goes through <see cref="DeckView.PlacementFor"/>, the very
    /// projection the renderer draws with. Writing that arithmetic down a second time here is this repo's
    /// first named bug class (unaudited client geometry literals), and it would send the captain walking at
    /// something they never pointed at.</para>
    ///
    /// <para>The goal is SNAPPED to whatever fixture the click landed on, so arrival is adjacent to the
    /// thing rather than on top of a patch of floor near it — which is what makes [E] live the moment the
    /// walk stops, and turns "walk to the locker, press E" into two automation actions instead of forty.</para>
    /// </summary>
    private void ClickToWalkAt(double clickPx, double clickPy)
    {
        // #875 · THE ONE PREDICATE, asked by the second grip. The pointer branch that got here knows only
        // that the canvas is a floor; whether those legs are the captain's is the WALK's question, and it is
        // asked HERE — of the same property the arrow keys ask, printing the same held line from the same
        // place — so that a click cannot walk the captain out from under a man the keys are refusing to walk
        // him out from under, and so that being held is said rather than swallowed.
        if (!TheCaptainsLegsAreTheirOwn)
        {
            if (TheHoldOnTheLegs is { } held)
            {
                ShowPulseMessage(held);
            }
            return;
        }

        // The shudder shake is deliberately NOT in this pan: it is a transient throw of the whole frame,
        // not a fact about where the floor is, and a captain pointing at a doorway during a tremor means
        // the doorway.
        DeckView.Placement place = DeckView.PlacementFor(
            _deckPlan, _viewportWidth, _viewportHeight, _avatarX, _avatarY, _deckPanX, _deckPanY);
        (double gx, double gy) = place.ToDeck(clickPx, clickPy);
        (gx, gy) = SnapWalkTargetToFixture(gx, gy);

        // #847 · A CLICK IS A MOVEMENT INPUT, so it costs the stand too — the owner's ruling names both grips
        // ("WASD and click-to-walk"), and a law the keyboard obeys and the mouse walks around is half a law.
        //
        // HERE, and not a line earlier: the pixel is turned into a place on the floor FIRST, through the
        // projection that was on the glass when the captain pointed at it. The view is centred on the
        // captain, so standing up moves the camera — resolving the click afterwards would send them walking
        // at a spot a pace from the one they aimed at, which is the drawn room and the pressed room
        // disagreeing about a finger. The ROUTE is planned after, off the step-off square and off the deck
        // standing up rebuilt.
        StandUpBeforeWalking();

        var from = new DeckReachability.Point(_avatarX, _avatarY);
        var to = new DeckReachability.Point(gx, gy);
        // #866 · A FINGER, NOT A BEAT. The reach is what tells Core this goal was POINTED at: the park's
        // photograph draws gravel where the deck has a raised bed, and asked for the exact square under the
        // cursor the search could only answer "no route" — after flooding the whole floor to say it.
        //
        // #875 · <c>enabled:</c> is TRUE by the time this line runs, and the seam stays in Core on purpose:
        // the gate the parameter was built for is asked above, by the one predicate, and a caller that is
        // not this one (a test, a touch UI, a lab) still needs somewhere to say "not now".
        AutoWalk.Attempt attempt = AutoWalk.Plan(
            enabled: true, from, to, _deckPlan.CollisionField, DeckPlan.AvatarRadius,
            AutoWalk.BoundsFor(_deckPlan.CollisionSegments, from, to),
            DeckReachability.DefaultStep, AutoWalk.PointingReachDu);

        if (attempt.Route is null)
        {
            // ── #866 · A CLICK THAT PLANS NOTHING SAYS SO. ALWAYS. ──
            //
            // The refusal IS the assertion the reachability audits make, said out loud to a person. It used
            // to be spoken only if Core had filled one in, which reads as defensive and is in fact the
            // #603/#825 class waiting to happen a fourth time: the day any path through the planner comes
            // back empty-handed without prose, this branch goes silent and a silent control is
            // indistinguishable from a broken one. So the line is unconditional and Core's own canonical
            // sentence is the floor under it — there is no arrangement of the world in which the captain
            // clicks the deck, does not walk, and is told nothing.
            ShowPulseMessage(attempt.Refusal ?? AutoWalk.RefusalLine);
            return;
        }

        _autoWalk = attempt.Route;
        _autoWalkDeck = _deckPlan;

        // ── #825 · THE ORDER LANDED; THE LEGS ARE HELD. ──
        //
        // Deliberately HERE, in the branch where a route exists, and not at the top of the method: the
        // refusal branch above says exactly one thing (#866's guard counts them), and "no way through" is
        // the truer sentence about a click that planned nothing whatever the machine is doing. What this
        // branch has is a queued target the sim will consume the moment a frame arrives — which is the
        // issue's own second option, already true by construction, and worth nothing at all until somebody
        // is told it. Sixteen seconds of a motionless dot is the report that filed this issue.
        AcknowledgeHeldControls();
    }

    /// <summary>#729 · What the captain actually pointed at. A console within arm's reach of the click wins
    /// (walk to the thing, not to the tile beside it); failing that, a door close to the click is taken as
    /// "go through there", since an auto-door is a place you aim for and never an obstacle. Otherwise the
    /// bare ground, exactly where the finger went.</summary>
    private (double X, double Y) SnapWalkTargetToFixture(double gx, double gy)
    {
        if (_deckPlan.NearestConsoleSpot(gx, gy) is { } spot)
        {
            // #791 · …and on a fixture that IS a length (the bar's service run) it is the point you aimed at
            // ON it, never its middle. Clicking the far end of an eighty-du desk and being marched forty du
            // up to the plate is "walk to the thing" answered about a different thing. A point console's
            // nearest point is itself, so no other deck in the game moves a millimetre.
            return spot.NearestPointTo(gx, gy);
        }

        foreach (DeckPlan.Door door in _deckPlan.Doors)
        {
            double mx = (door.X1 + door.X2) / 2.0, my = (door.Y1 + door.Y2) / 2.0;
            if (((gx - mx) * (gx - mx)) + ((gy - my) * (gy - my)) <= DeckPlan.InteractRadius * DeckPlan.InteractRadius)
            {
                return (mx, my);
            }
        }
        return (gx, gy);
    }

    /// <summary>How many sub-steps one frame may spend. A frame's budget is speed × dt (dt clamped to
    /// 0.1 s), and no sub-step is longer than <see cref="AutoWalk.MaxSubStepDu"/>, so the very worst frame
    /// the client can hand out needs five of these. Eight is the headroom, and it exists only so a bug
    /// upstream can never turn one frame into an unbounded loop.</summary>
    private const int AutoWalkSubStepsPerFrame = 8;

    // Movement keys are held-state (smooth walk); E interacts; Q returns to the map. Returns
    // true when the key was consumed by the deck so it can't also fire a thrust pulse.
    private bool HandleDeckKey(string key)
    {
        switch (key)
        {
            case "w" or "W" or "ArrowUp":
            case "a" or "A" or "ArrowLeft":
            case "s" or "S" or "ArrowDown":
            case "d" or "D" or "ArrowRight":
                // #833 · NOT WHILE SOMEBODY IS WALKING YOU OFF HIS FLOOR. The escort is the one stretch of
                // this game where the captain's legs are not his own, and it is a few seconds long. The press
                // is CONSUMED and answered in the guard's own words: a key that did nothing and said nothing
                // reads as a broken key.
                //
                // First, and above the seat below, because this press is REFUSED rather than charged for —
                // a captain the guard is holding must not pay for a stand he is not going to be allowed to
                // walk off.
                //
                // #875 · …and asked through THE ONE PREDICATE that ClickToWalkAt asks, printing the line
                // from the one place that owns it. This used to be an inline CaptainIsUnderEscort here and
                // a differently-spelled property over there — two authors on one law, which is this repo's
                // first named bug class and the reason a floor could be crossed by arrow and not by finger.
                if (!TheCaptainsLegsAreTheirOwn)
                {
                    if (TheHoldOnTheLegs is { } held)
                    {
                        ShowPulseMessage(held);
                    }
                    return true;
                }
                // #729 · THE KEYS ALWAYS WIN, and they win HERE — on the press itself, before the frame
                // that follows it spends a single sub-step of the route. Cancelling anywhere further down
                // (in MoveAvatar, say) would let the walk finish the leg it was on, and "it kept going for
                // half a second after I grabbed the controls" is exactly the feel this must never have.
                CancelAutoWalk(true);
                _deckKeys.Add(Canonical(key));
                // #847 · …AND IF THE CAPTAIN IS SITTING ON SOMETHING, THAT IS WHAT THE PRESS COSTS. Owner:
                // "the keys simply cost you the stand first, which is how chairs work." #784 raised a
                // pop-up here and consumed the press; the ruling replaced the question with the act, for
                // every seat kind at once — the stool included, which is the gap #847 was filed on.
                //
                // Nothing has walked yet: the step is spent in MoveAvatar on the NEXT frame, which refuses
                // outright while any seat is still open, so the stand always lands first and the walk sets
                // off from the seat's own step-off square. Recorded above rather than below only so that the
                // stand's own line is the last thing said — a route-cancel notice raised afterwards would
                // print over "off you go".
                StandUpBeforeWalking();
                // #825 · …AND IF THE MACHINE IS NOT HANDING OUT FRAMES, SAY THAT LAST. A held key is walked
                // by the same clamped frame the clicked route is, so it buys the same 0.1 s of legs however
                // long the gap was, and a captain leaning on W watching a motionless dot has been told the
                // key is broken. Same clock, same threshold, same sentence as the click — one stall, one
                // line, whichever grip the hand is on.
                AcknowledgeHeldControls();
                return true;
            case "q" or "Q":
                SwitchDesk(ShipDesk.Nav); // the deck is continuous now — Q always steps up to the helm
                return true;
            case "e" or "E":
                InteractAtConsole();
                return true;
            case "b" or "B":
                // PR-WIRE: bank at the contact's table — deposit, withdraw or borrow (in person).
                OpenBankAtBar();
                return true;
            case "g" or "G":
                // #313: the panic drop — ditch the chest to sprint full speed, recover it later.
                DropChest();
                return true;
            case "i" or "I":
                // #603 · THE SATCHEL. Owner: "the I key should be advertised in the hud also like we do now
                // for the other keys." Opened from nowhere in particular it is just a look at what you are
                // carrying; opened from a locked door it is a list of things to TRY.
                //
                // #688 · And it SHUTS with the same key. Owner: "If I press I when inventory is open, let's
                // close it then." One line of feel, and the kind that is invisible until you are in a
                // corridor with a pack coming and the pocket you opened by reflex will not go away by the
                // same reflex.
                if (_surface is not null)
                {
                    ToggleSatchel();
                }
                return true;
            case "h" or "H":
                // #538 · THE REMOTE IS IN YOUR HAND, NOT ON THE BRIDGE. Owner: "the remote to sentries should be
                // in the mobile hud not at captains desk" — and of course: you give this order folded into a
                // hole with somebody else's boots on the deck plating, nowhere near a desk. H for HOLD.
                if (_surface is not null)
                {
                    ToggleWeaponsTight();
                }
                return true;
            case "k" or "K":
                // #537 · KNOCK. Owner: "a combi of detect tool that scans on timer like 5 seconds at a spot",
                // "it might be noisy to say knock on walls etc." K starts a sounding where you stand — a clock
                // that dies the moment you walk away, and a racket the hull hears either way.
                if (_surface is not null && OnWreck)
                {
                    ToggleSounding();
                }
                return true;
            case "t" or "T":
                // #314: set down a carried sentry at your feet (or pick up one you're standing on).
                if (_surface is not null)
                {
                    DeployOrRetrieveSentry();
                }
                return true;
            default:
                return false;
        }
    }

    private void MoveAvatar(double dtRealSeconds)
    {
        // ── #784/#847 · A CAPTAIN IN A SEAT DOES NOT WALK ──
        //
        // The key handler above pays for the stand on the press itself, and this is the second half of the
        // same law rather than a duplicate of it: a key HELD BEFORE the captain sat down is still in the held
        // set, and every route the auto-walk is mid-way through is still a route. Refusing at the key alone
        // would let a captain sit down mid-stride and keep going, chair and all — which is exactly the "the
        // sim did one thing while the picture said another" this project has paid for three times.
        //
        // #847 · AND IT IS THE ANY-SEAT QUESTION, not the table's. CaptainIsSeated is #788's TABLE flag and a
        // stool has never been in it, so until the owner's ruling this frame walked a captain along the
        // counter while the counter card still had them sitting on a stool — invisible until #820 put the dot
        // ON the seat, and then unmistakable. One flag, every seat kind, and the ONLY way a body leaves a
        // seat is StandUpBeforeWalking and the seat's own teardown under it.
        if (CaptainIsSeatedAnywhere)
        {
            return;
        }

        // ── #833 · …AND A CAPTAIN BEING WALKED OUT DOES NOT STEER ──
        //
        // Same law, one posture over, and the same second half of it: the key handler consumes the press, and
        // this refuses the HELD key and the route already in flight. The escort moves the body itself
        // (Map.Patrol.WalkTheEscort) through this file's own DeckPlan.Move, so the walls, the air and the fan
        // all keep working — the only thing taken is the steering.
        if (CaptainIsUnderEscort)
        {
            return;
        }

        // #825 · THE HONEST CLAMP, and now a NAMED one. A frame that spanned sixteen real seconds buys this
        // much walking and no more — spending the whole gap would resolve a hundred and forty deck units of
        // movement in one axis-separated probe, i.e. through a bulkhead, with the air bill, the nerve, the
        // tracker and the Old Ones all skipped over it. What was wrong was never the clamp; it was that the
        // dropped time was thrown away in silence. The number lives in Core beside the stall threshold that
        // is derived from it (FrameGap), so the banner that reports the gap and the frame that eats it are
        // one fact rather than two literals that happen to agree today.
        double dt = Math.Min(dtRealSeconds, FrameGap.SpentPerFrameSeconds);

        // Three tots of rum and the deck tilts (M21): the heading sways for a while. Purely
        // cosmetic mischief — collision and interaction are unaffected.
        double wobble = (_lastTimestampMs ?? 0) < _wobbleUntilMs
            ? Math.Sin((_lastTimestampMs ?? 0) * 0.004) * 0.9 * dt
            : 0;

        // ── #729 · THE ROUTE, WALKED THROUGH THE SAME LEGS ──
        //
        // Everything below this block is the hand-walk, and everything in it is the auto-walk, and the only
        // difference between them is where (dx, dy) came from. The move itself is _deckPlan.Move either way,
        // the frame it happens in is this one either way, and StepSurface runs immediately after either way
        // — which is why the air bill, the Old Ones' closing, the tracker's ring and the doors' proximity
        // all come out identical. There is no "auto-walk mode" anywhere else in this codebase, deliberately.
        //
        // The frame's budget (speed × dt) is spent in sub-steps rather than one long move, because Move
        // resolves a diagonal axis-separately and a long one probes a corner that is not on the planned
        // line at all. No wobble is applied: the rum tilts a HEADING somebody is holding, and a route is
        // not steered by hand.
        if (_autoWalk is { Active: true } route && ReferenceEquals(_autoWalkDeck, _deckPlan))
        {
            double budget = CurrentWalkSpeed * dt;
            for (int spent = 0; spent < AutoWalkSubStepsPerFrame && budget > 1e-9; spent++)
            {
                if (!route.TryStep(_avatarX, _avatarY, budget, out double sdx, out double sdy))
                {
                    break;
                }

                (double wasX, double wasY) = (_avatarX, _avatarY);
                (_avatarX, _avatarY) = _deckPlan.Move(_avatarX, _avatarY, sdx, sdy);
                if (Math.Abs(_avatarX - wasX) + Math.Abs(_avatarY - wasY) <= 1e-9)
                {
                    // The plan and the ground disagreed. Stop and SAY so — a walk that grinds in place
                    // against a wall is a bug wearing a feature's clothes, and the captain would spend a
                    // tank of air watching it happen.
                    route.Snag();
                    ShowPulseMessage(AutoWalk.SnagLine);
                    break;
                }

                _avatarHeading = Math.Atan2(sdy, sdx);
                budget -= Math.Sqrt((sdx * sdx) + (sdy * sdy));
            }

            RefreshAshore();
            if (!route.Active)
            {
                _autoWalk = null;
                _autoWalkDeck = null;
            }
            return;
        }

        if (_autoWalk is not null && !ReferenceEquals(_autoWalkDeck, _deckPlan))
        {
            CancelAutoWalk(false);   // the floor changed under the route (the lift, a landing, a new wing)
        }

        double dx = 0, dy = 0;
        if (_deckKeys.Contains("w")) dy += 1;   // +Y = port (up on screen)
        _ = wobble; // top-down: applied to the move vector below
        if (_deckKeys.Contains("s")) dy -= 1;
        if (_deckKeys.Contains("a")) dx -= 1;
        if (_deckKeys.Contains("d")) dx += 1;
        if (dx == 0 && dy == 0)
        {
            return;
        }

        double norm = Math.Sqrt(dx * dx + dy * dy);
        double step2 = CurrentWalkSpeed * dt;
            if (wobble != 0 && (dx != 0 || dy != 0))
            {
                double a = Math.Sin((_lastTimestampMs ?? 0) * 0.004) * 0.45;
                (dx, dy) = (dx * Math.Cos(a) - dy * Math.Sin(a), dx * Math.Sin(a) + dy * Math.Cos(a));
            }
        (_avatarX, _avatarY) = _deckPlan.Move(_avatarX, _avatarY, dx / norm * step2, dy / norm * step2);
        _avatarHeading = Math.Atan2(dy, dx);
        RefreshAshore();
    }
}
