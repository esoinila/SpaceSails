using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;
/// <summary>
/// LIFTOFF — board the shuttle and go home. Player-initiated ONLY: nothing on the moon ever
/// self-resolves, and that includes leaving it.
///
/// <para>Split out of <c>Map.Surface.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ── Liftoff: board the shuttle (player-initiated ONLY — nothing self-resolves). ──

    private void LiftOffFromSurface()
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // #540 · AND THIS IS THE ONE THAT MATTERS. Owner: "its doors open once the warm up is complete", and then
        // the scene it is for — "Under a swarm of reevers with slinged autoguns that wait can feel really long
        // time 😎". Departing the SYSTEM was already gated; the ride HOME from a hull was not, which is exactly
        // where a captain meets the clock: standing at the lock, in the open, waiting to be let in.
        if (!BoatReadyToFly())
        {
            return;
        }

        ex.Channel = null;
        bool escapedWithWatchdogs = _reevers.Count > 0;
        TreasureCache? buried = ex.Cache;
        bool droppedAndLeft = ex.ChestDropped; // read before the excursion (and its dropped pile) is folded away
        // #455 rule 2 · …and a chest left on the regolith now stays there as a REAL cache, in the open, on
        // the harder roll. See LeaveTheDroppedChestInTheOpen (Map.Surface.Dig) for why that is the build the
        // rule was asking for rather than a nicety.
        TreasureCache? dropped = LeaveTheDroppedChestInTheOpen(ex);

        // #314: carried sentries come home (with their drained magazines); any left DEPLOYED on the
        // ground is abandoned — a write-off with a ledger line (#119 voice). Retrieve them before liftoff
        // to keep them.
        int abandoned = 0;
        foreach (SurfaceBot b in ex.Bots)
        {
            if (SurfaceArrival.IsDoorSentry(b.Unit))
            {
                continue; // #461: the tube's own gun. Never yours to carry home, never a write-off.
            }
            if (b.Deployed)
            {
                abandoned++;
                LogAutopilotEvent(SentryBot.AbandonLedgerLine(b.Unit, b.Rounds));
            }
            else
            {
                _shipBots.Add(new ShipBot(b.Unit, b.Rounds));
            }
        }

        // #370: an away-team gig settles its payout on the ride home — the fat base plus banked discoveries,
        // docked for any scientist lost to the dark (ExpeditionReward). Narrated, then the gig is closed.
        bool settledExpedition = ex.Expedition && SettleExpedition(ex);
        // #394: lifting off the deflection rock. If the charge fired it settles its heroic pay; if it never
        // fired (an abort), the rock is left on its line — the impact resolves and the port takes it.
        bool settledDeflection = ex.Deflection && SettleDeflection(ex);

        // #583 · IF THEY WERE STILL COMING, YOU GOT AWAY — and the game says so, because an escape that is
        // narrated as nothing is indistinguishable from an escape that never happened. The heat is untouched:
        // outwalking a writ is not settling one, and they know the ship and they will know the next port.
        //
        // #731 · …and a crew who are WALKING HOME were not outwalked. Once the writ is settled the escape
        // line is a lie of the same family as the one that sentence exists to prevent: the captain paid, or
        // fought clear, and watched them go. Nothing is said in that case, which is the correct amount.
        bool outwalkedTheWrit = ex.CollectorsLanded && !ex.CollectorsGoingHome && _busted is null;

        // #696 · A HOLD THAT THE SHUTTLE ENDS IS STILL AN INTERRUPTION, AND IT IS SAID. Nothing to undo —
        // the sleeve was never emptied and the book was never written in — but a captain who lifted off in
        // the middle of photographing a file has to be told the file is still in their pocket and still
        // unread, or the first thing they do at the desk is look for a gist that was never filed.
        //
        // Only here, and deliberately not on the death paths that also clear _surface: a captain being
        // narrated through the four-stage freeze does not need a line about their paperwork.
        ProcessingIsInterrupted(Core.Processing.Interruption.LiftedOff);

        _surface = null;
        // #612: the next landing works its own air out from scratch, so no crossing line is ever inherited
        // from the last moon.
        _airSupplyNoted = null;
        _reevers.Clear();
        _collectors.Clear();
        _lastNearestReeverRange = null;

        if (outwalkedTheWrit)
        {
            ShowPulseMessage(CollectorLanding.EscapedLine);
        }

        SetDeckForDock(ex.RestoreHavenId); // rebuild the ship/complex; folds the surface away
        (_avatarX, _avatarY, _avatarHeading) = (-6, -6.5, Math.PI / 2); // step off into the bay
        RendererInterop.PlayCue("board");

        string botTail = abandoned > 0
            ? $" {abandoned} sentry bot{(abandoned == 1 ? "" : "s")} left behind — written off."
            : "";

        // #313 · THE CHEST YOU DROPPED AND NEVER WENT BACK FOR. Dropping it (G) says "come back for it when
        // the ground's clear", and inside the excursion that is exactly true — walk over the spot and it is
        // back in the sling.
        //
        // #455 rule 2 · Lift off without it and it is no longer simply gone. It stays where it fell, in the
        // open, on the harder roll — a real chest in the ledger with a real map card, which is what makes
        // "buried beats dropped, by a lot" a thing the game can be wrong about instead of a thing it merely
        // says. The coin and cargo therefore DO leave the books now (#648's honest "nothing left the books"
        // line was honest about a world that no longer exists), so the news is what the chest reads as.
        string dropTail = dropped is { } open
            ? $" 🧰 You lifted off without the chest you dropped — it stays where it fell. {open.SafetyWith(TheFightThisGroundCarries(open)).Sentence} Map filed (🗺)."
            : droppedAndLeft
                ? " 🧰 You lifted off without the chest you dropped — but it was empty, so nothing was left out there."
                : "";
        if ((buried ?? dropped) is { } cache)
        {
            _treasureMapCard = cache;
            RendererInterop.PlayCue("reveal");
            string tail = escapedWithWatchdogs
                ? $" {cache.ReeverLevel} Old One(s) haunt this ground now — the best kind of lock."
                : "";
            ShowPulseMessage(buried is not null
                ? $"🛸 Lifted off {ex.Stop.Body.Name}. Map filed (🗺).{tail}{botTail}"
                : $"🛸 Lifted off {ex.Stop.Body.Name}.{tail}{botTail}{dropTail}");
        }
        else if (!settledExpedition && !settledDeflection) // an away-gig settle already spoke its payout line
        {
            string tail = escapedWithWatchdogs ? " You outran the Old Ones." : "";
            ShowPulseMessage($"🛸 Back aboard from {ex.Stop.Body.Name}.{tail}{botTail}{dropTail}");
        }
    }
}
