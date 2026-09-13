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

/// <summary>
/// #251 · THE ONCE-A-FRAME RECKONING — heat decay, hunter pursuit and break-off, all in SIM time so it
/// scales with warp instead of crawling at wall-clock rate; and the plotted state a pursuer actually
/// chases (#329's quantum trail).
///
/// <para><c>UpdateEncounters</c> is also where the whole-day watches are turned over — the buried-cache
/// discovery roll and #319's void watch — because a sim-day rolling past is already resolved
/// skip-proof here and nowhere else in this client.</para>
///
/// <para>Split out of <c>Map.Combat.cs</c> under #251 with no member renamed, re-scoped or re-ordered,
/// and not one field moved.</para>
/// </summary>
public partial class Map
{
    /// <summary>The player state a pursuit quantum steers at: position interpolated on this
    /// frame's trail, falling back to the live ship outside it (or with the switch off). The
    /// velocity stays the frame-end ship's — AdvanceHunter only reads it for the catch check's
    /// relative speed, where a frame of gravity barely moves the needle.</summary>
    private ShipState PlayerStateForPursuit(double stepTime)
    {
        if (!SteerHuntersByQuantumTrail || _pursuitTrail.Count < 2 || stepTime >= _pursuitTrail[^1].SimTime)
        {
            return _ship;
        }

        for (int i = _pursuitTrail.Count - 2; i >= 0; i--)
        {
            if (_pursuitTrail[i].SimTime <= stepTime)
            {
                TrajectorySample a = _pursuitTrail[i], b = _pursuitTrail[i + 1];
                double span = b.SimTime - a.SimTime;
                double f = span > 0 ? (stepTime - a.SimTime) / span : 1;
                return new ShipState(a.Position + (b.Position - a.Position) * f, _ship.Velocity, stepTime);
            }
        }

        return _ship;
    }

    // Heat decay, hunter pursuit and break-off — all in sim time (like NPC stepping), so it
    // scales naturally with warp instead of crawling at wall-clock rate.
    private void UpdateEncounters()
    {
        if (_ephemeris is null)
        {
            return;
        }

        // PR-BUSTED: while a boarding pop-up is open, encounters freeze — the captain is making a
        // choice at 1×, no new hunter runs him down over the top of it.
        if (_busted is not null)
        {
            return;
        }

        // #175: settle any moon-haven cargo run whose ship is parked in orbit — the owner who was
        // ALREADY orbiting Enceladus when the parcel loaded gets paid here, since no dock event fires.
        CompleteBoundCargoRunQuests();

        // #223: resolve the buried-cache discovery roll as sim time rolls past whole days — rivals find
        // our hoards on a slow roll whether we're flying, warping, or docked.
        RunCacheDiscoveryWatch();

        // #638: and the other whole-day watch — the void's. Same cadence and the same skip-proofing, because
        // a countdown that a warp jump can leap over is not a countdown (Map.Void).
        RunTheVoidWatch();

        bool wasHidden = !double.IsNaN(_hiddenAtHavenSinceSimTime);
        bool hidden = IsHiddenAtHaven();

        // Rising edge of "hidden at a haven" — whether you orbited a haven moon or clamped onto a
        // dock. Drop the quiet news line the regulars notice, and advance the haven lesson. (Moved
        // here from the orbit-bind loop so a mass-less dock, which never binds, still triggers it.)
        if (hidden && !wasHidden && _nearestBody is { IsHaven: true } arrivedHaven)
        {
            PushNewsEvent(NewsWire.NewsEventKind.OrbitEnteredHaven, arrivedHaven.Name);
            AdvanceTutorial(StepInsertHaven);

            // Easter egg: settle in at The Rusty Roadstead and the bird cracks wise about a break.
            if (arrivedHaven.Id == "the-space-bar")
            {
                SquawkNow(Parrot.Squawk.SpaceBarBreak, _lastTimestampMs ?? 0, force: true);
            }
        }

        _hiddenAtHavenSinceSimTime = hidden
            ? (wasHidden ? _hiddenAtHavenSinceSimTime : SimTime)
            : double.NaN;
        double hiddenDuration = hidden ? SimTime - _hiddenAtHavenSinceSimTime : 0;

        _heat = EncounterRule.DecayHeat(_heat, SimTime, hidden);

        // #715 · …and the OTHER heat, which is a DIFFERENT NUMBER WITH A DIFFERENT HOLDER. The line above
        // is what the law thinks of a hull; this is what one company thinks of a captain, and the two are
        // never read off each other — the guard next door raises either one and proves the other did not
        // move. It cools in ABSENCE and in nothing else (the owner's own word: "get out"), so the outfit
        // whose ground is underfoot is handed in and is the one outfit this call does not cool.
        //
        // It creates nothing: a captain who has never crossed anybody has an empty book after ten
        // thousand frames, which is what keeps #905's fingerprints where they were.
        IllegalHeat.Cool(_contacts, TheOutfitUnderfoot, SimTime);

        // PR-BUSTED (ruling §5.1): when heat fully cools, the stolen cargo launders — the evidence
        // leaves the books. And at each UPWARD heat crossing the parrot names the confiscation exposure
        // (owner: "Heat two, captain — they'll take a third of the purse if they catch us!"), riding the
        // same #166 alert edges the rest of the ship's voice does.
        if (_heat.Level == 0 && _hotCargo.Any)
        {
            _hotCargo.Launder();
            ShowPulseMessage("The trail's cold — your hot cargo just became honest freight again.");
        }

        if (_heat.Level > _lastAnnouncedHeat)
        {
            SquawkNow(Parrot.Squawk.Busted, _lastTimestampMs ?? 0, BustedRule.ExposurePhrase(_heat.Level), force: true);
        }

        // #380 item 1: the FIRST time heat reaches 1, advertise the safety net one beat before the death card
        // would have to. Fires whatever raised the heat (a robbery, a Reever's hand), once per run.
        if (!_heatInsuranceAdvised && _heat.Level >= 1)
        {
            _heatInsuranceAdvised = true;
            ShowPulseMessage("Word of advice, captain — your brain-backup's current and the pirate-insurance stake is paid. Getting caught is expensive. Getting killed is survivable.");
        }

        _lastAnnouncedHeat = _heat.Level;

        // #580 · NOBODY IS AT THE CONTROLS. While the captain is walking a moon, the ship is a docked hull
        // with the lights on and no one aboard — so the wolves hold station instead of closing, and cannot
        // catch her. See EncounterRule.HoldStation for the owner's ruling; the short of it is that heat is
        // the CAPTAIN's, and a game where a good long excursion means coming home to a boarding party is a
        // game about guarding a parking lot.
        //
        // #1151 · …AND AN EXCURSION IS ONLY ONE OF THE THREE WAYS HE IS NOT ON HER. The owner's ruling on
        // #525 makes the paragraph above the general law rather than one case of it — the process demands a
        // ship and a captain in one place — so the question is asked through the one predicate that knows
        // all three signals (TheMasterIsAboardHer, Map.Claims.Presence.cs). On `_surface is null` alone a
        // hunter could still reach a hull whose master was standing in a bar past a mated gangway, which is
        // #1138's own reading of what being aboard means, applied to the people who want her.
        //
        // #1151 slice 4 · …AND THE MAN ALREADY WAITING GOES FIRST. Two lines, in this order, and the order is
        // the law. A writ that waited is served the moment its own conditions are met — he is back at their
        // berth, on her — and only then is the sky asked whether anybody else may proceed, because serving
        // clears the file and the answer changes on the same frame. Ask the other way round and the collector
        // who has been standing on that ramp since the ending would be made to defer to his own writ.
        TheWaitingWritIsServed();

        // ONE WRIT, NOT A QUEUE. `TheProcessMayProceed` is the presence law AND the file: a second collector
        // who runs the same hull down while a writ is out does not open a second process over the top of the
        // first — he holds station, the shape this sim already gives a pursuer who cannot proceed.
        bool anybodyMayProceed = TheProcessMayProceed();

        for (int i = _hunters.Count - 1; i >= 0; i--)
        {
            HunterState hunter = _hunters[i];
            if (!anybodyMayProceed)
            {
                _hunters[i] = EncounterRule.HoldStation(hunter, SimTime);
                continue;
            }

            while (hunter.State.SimTime < SimTime && !hunter.CaughtPlayer && !hunter.BrokenOff)
            {
                double stepTime = Math.Min(SimTime, hunter.State.SimTime + EncounterRule.HunterStepSeconds);
                hunter = EncounterRule.AdvanceHunter(hunter, PlayerStateForPursuit(stepTime), stepTime);
                if (hidden)
                {
                    hunter = EncounterRule.ApplyBreakOff(hunter, hiddenDuration);
                }
            }

            if (hunter.CaughtPlayer)
            {
                ApplyHunterCatch(hunter);
                _hunters.RemoveAt(i);
            }
            else if (hunter.BrokenOff)
            {
                ShowPulseMessage($"{hunter.Callsign} loses your scent — safe at anchor.");
                SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
                _hunters.RemoveAt(i);
            }
            else
            {
                _hunters[i] = hunter;
            }
        }

        // Haven tutorial completes when the heat your piracy earned has fully cooled (the haven's
        // 4x decay is what gets you there in reasonable time — the lesson is "lying low works").
        if (_tutorialStep == StepCoolHeat && _heat.Level == 0)
        {
            AdvanceTutorial(StepCoolHeat);
            ShowPulseMessage("The trail's gone cold — you've learned to lie low. The haven kept you.");
        }
    }
}
