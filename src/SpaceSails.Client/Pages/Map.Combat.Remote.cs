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

// Subject: the captain's handset — the boat's power and her clocks, and #803's designate flow. Part of Map.Combat (#870 split; the header note lives in Map.Combat.cs).
public partial class Map
{

    /// <summary>Whether the captain's remote is open. Owner: <i>"Maybe small button to open the sentry settings
    /// pop-up 😎"</i>, and then the better name for it — <i>"Captain's remote"</i>.</summary>
    private bool _showCaptainsRemote;

    /// <summary>The captain's standing order for the boat. Owner: <i>"Shuttle should also power down to make it
    /// less of an anomaly."</i> A lit shuttle clamped to a dead hull is the anomaly an inspection team notices long
    /// before it notices a man.</summary>
    private bool _boatOrderedCold;

    /// <summary>
    /// Where she actually is between cold (0) and flying (1) — a dial, not a countdown, so that reversing the
    /// order keeps the progress already made. Owner: <i>"A timer on the map about the cool down and warm up"</i>,
    /// which only makes sense if both directions take real time.
    /// </summary>
    private double _boatWarmth = 1.0;

    private SilentRunning.BoatState BoatState => SilentRunning.StateOf(_boatWarmth, _boatOrderedCold);

    private double BoatSecondsLeft => SilentRunning.SecondsLeft(_boatWarmth, _boatOrderedCold);

    /// <summary>Whether her clocks are worth a line on the map — i.e. anything other than a warm boat, which is
    /// the boring default and would only clutter the strip.</summary>
    private bool BoatClockWorthShowing => BoatState != SilentRunning.BoatState.Warm;

    /// <summary>#736 · What the last switch on the remote answered, said on the remote. Three of the four
    /// switches here order something that is somewhere ELSE — bots across the field, a boat on the pad, a
    /// sounder in your own fist — so the sentence IS the feedback, and it was pulsed under the remote's own
    /// backdrop. Cleared whenever the handset is taken out or pocketed.</summary>
    private string? _remoteOutcome;

    private void OpenCaptainsRemote()
    {
        _showCaptainsRemote = true;
        _remoteOutcome = null;
        CloseDesignate();
        RendererInterop.PlayCue("board");
    }

    private void CloseCaptainsRemote()
    {
        _showCaptainsRemote = false;
        _remoteOutcome = null;
        CloseDesignate();
    }

    // ── #803 · DESIGNATE: THE CAPTAIN AIMS A GUN BY HAND ──────────────────────────────────────────────
    //
    // Owner, 2026-08-09: "we will need to use the handheld captain's control to set the guns / gun to fire
    // at something manually, that UI is missing."
    //
    // It belongs on THIS handset and nowhere else. The other three switches on it order something that is
    // somewhere else — bots across the field, a boat on the pad, a sounder in your own fist — and this is
    // the same gesture with a target on the end of it. Three presses, and each one is a decision the captain
    // makes out loud: WHICH GUN, WHICH THING, and SAY WHEN.
    //
    // Nothing here is autopilot and nothing here is allowed to become it. The sentries' own volley picks
    // targets inside an arc because that is what a machine on watch does; this picks one thing, once,
    // because a captain decided. The orbit-keeping law does not reach it (#803's own ruling), and neither
    // does weapons-tight: an order that safes the machines' triggers has never touched the captain's, which
    // is the whole authority idiom this game runs on (SentryBot.WeaponsTightLine says so in as many words).

    /// <summary>Whether the handset is showing the designate flow instead of its switches.</summary>
    private bool _designateOpen;

    /// <summary>The gun the captain picked, or null while they are still picking one.</summary>
    private string? _designateUnit;

    /// <summary>What that gun can see, rebuilt the moment a gun is picked. Refusing targets are IN this list
    /// on purpose: a lock the story needs shut has to be pressable so it can SAY so, and a target that is
    /// simply missing from a list teaches a captain nothing except that the game is arbitrary.</summary>
    private readonly List<DesignateTarget> _designateTargets = [];

    /// <summary>One thing a gun can see: what is painted on it, where it is, how far, what it would cost,
    /// and whether there is anything on it worth a round.</summary>
    public sealed record DesignateTarget(
        string Sign, double X, double Y, double RangeDu, int Cost, bool Shootable);

    private void CloseDesignate()
    {
        _designateOpen = false;
        _designateUnit = null;
        _designateTargets.Clear();
    }

    /// <summary>Open the mode. If there is nothing to point, the panel says which of the two nothings it is —
    /// no gun set down, or every gun set down reading 00 — because those are different problems with
    /// different answers and one sentence for both is the bug #728 was filed about.</summary>
    private void OpenDesignate()
    {
        _designateOpen = true;
        _designateUnit = null;
        _designateTargets.Clear();
        _remoteOutcome = null;

        if (DesignatableGuns().Count == 0)
        {
            _remoteOutcome = AnythingSetDownAtAll()
                ? ShootTheLock.EverythingDryLine
                : ShootTheLock.NothingSetDownLine;
        }
        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    private void BackToTheSwitches()
    {
        CloseDesignate();
        _remoteOutcome = null;
        StateHasChanged();
    }

    // ── #760 · SEND STANDING: the handset's fifth switch, and the only one that talks to a stranger ──────
    //
    // Owner, 2026-08-08: "Where that standing exists, the ship remote … can use it over the air: send
    // messages that open places and enable things — a door unbolted before you reach it, a lift enabled from
    // the surface. The wallet is who you claim to be in person; the remote is who your ship claims to be on
    // the operator's network."
    //
    // It belongs on THIS handset for the reason the other four do: every switch on it orders something that
    // is somewhere else, and this one is that gesture pointed at a company rather than at a machine.
    //
    // The page decides nothing. Which gate, whether it opens, what it costs and every word of the answer are
    // Core's (RemoteSend) — this method presses the button and shows what came back, which is #736's law: the
    // sentence IS the feedback, said on the pop-up that is up.
    private void SendTheStanding()
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // #715 · The outfit's own memory goes IN, and what the send costs comes back out. Above the meter's
        // top rung the net has nothing to say to this ship's name at all, which is the one effect in that
        // feature that takes a verb away — and it is owed to this outfit and to nobody else.
        RemoteSend.Sent sent =
            RemoteSend.Send(ex.Stop.Body.Id, ex.Floor, _satchel, IllegalHeat.HeatAtSite(_contacts, ex.Stop.Body.Id));
        _remoteOutcome = sent.Line;

        // The book keeps it whatever the screen does (#693) — a line saying who was addressed and what came
        // back. And #715's meter is built now, so the charge Core publishes is BANKED rather than filed and
        // forgotten: one call, against the outfit named on the charge, never against the moon.
        FileNote(sent.Line, sent.Worked ? "📡" : "🔒");
        BankTheCrossing(sent.Charge);

        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    /// <summary>
    /// The guns a captain may point: SET DOWN, and with something in the drum.
    ///
    /// <para>Both halves are the feature. A bot in the sling is a machine you are carrying, not a machine
    /// holding a position — and a bot reading 00 is the whole reason the put verb exists, so offering it
    /// here would be the handset promising something the magazine cannot pay for.</para>
    ///
    /// <para>The tube's own GATE-1 is not one of yours. It hangs in the shuttle door, is topped back up
    /// after every volley and is "never counted against the sling" by its own law — the same exclusion the
    /// #797 magazines readout makes, asked through the same Core question, so the instrument and the handset
    /// can never disagree about what the captain owns.</para></summary>
    private List<SurfaceBot> DesignatableGuns()
    {
        var guns = new List<SurfaceBot>();
        if (_surface is not { } ex)
        {
            return guns;
        }
        foreach (SurfaceBot b in ex.Bots)
        {
            if (ShootTheLock.MayPoint(b.Deployed, b.Rounds, SurfaceArrival.IsDoorSentry(b.Unit)))
            {
                guns.Add(b);
            }
        }
        return guns;
    }

    /// <summary>Is anything of the captain's standing out there at all, whatever its counter reads? The
    /// question that tells the two nothings apart.</summary>
    private bool AnythingSetDownAtAll()
    {
        if (_surface is not { } ex)
        {
            return false;
        }
        foreach (SurfaceBot b in ex.Bots)
        {
            if (b.Deployed && !SurfaceArrival.IsDoorSentry(b.Unit))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Pick the gun, and see what it can see.</summary>
    private void PickTheGun(string unit)
    {
        _designateUnit = unit;
        _remoteOutcome = null;
        ScanForLocks(unit);
        if (_designateTargets.Count == 0)
        {
            _remoteOutcome = ShootTheLock.NothingInViewLine(unit);
        }
        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    /// <summary>
    /// What this gun has in front of it. The signs on the deck are the truth about what is standing there
    /// (the renderer hangs one console on every door that will not open), and the two questions asked of
    /// each are the SAME two a sentry asks of an Old One — inside the arc, and with stone out of the way.
    /// One range law, one sight law: a captain who has learned what a sentry covers has learned this.
    /// </summary>
    private void ScanForLocks(string unit)
    {
        _designateTargets.Clear();
        if (_surface is not { } ex)
        {
            return;
        }
        SurfaceBot? gun = ex.Bots.FirstOrDefault(b => b.Unit == unit && b.Deployed);
        if (gun is null)
        {
            return;
        }

        Ammunition.Kind kind = Ammunition.ById(gun.AmmoId);
        IReadOnlyList<SurfaceCollision.Segment> walls = SightBlockers();

        foreach (DeckPlan.ConsoleSpot spot in _deckPlan.Consoles)
        {
            if (spot.Kind != DeckPlan.ConsoleKind.HiveSign)
            {
                continue;
            }
            // A door that is already open is not a target. It wears the hole rather than the padlock, which
            // is the deck saying which of the two it is — the same string the renderer wrote.
            if (spot.Label.StartsWith(HiveInterior.ShotOpenGlyph, StringComparison.Ordinal))
            {
                continue;
            }
            string sign = spot.Label.Replace("🔒 ", "", StringComparison.Ordinal);
            if (!ShootTheLock.CanBreak(gun.X, gun.Y, spot.X, spot.Y, walls))
            {
                continue;
            }
            _designateTargets.Add(new DesignateTarget(
                sign, spot.X, spot.Y,
                ShootTheLock.RangeDu(gun.X, gun.Y, spot.X, spot.Y),
                ShootTheLock.RoundsFor(kind),
                ShootTheLock.IsShootable(sign)));
        }

        _designateTargets.Sort((a, b) => a.RangeDu.CompareTo(b.RangeDu));
    }

    /// <summary>
    /// SAY WHEN. The one press that spends something.
    ///
    /// <para>Every consequence hangs off the single Core answer (<see cref="ShootTheLock.Fire"/>): the drum,
    /// the door, the ear and the record all move off the same yes, so the sim and the sentence cannot come
    /// apart — which on this repo is the third named bug class and the reason this is written as one call
    /// rather than as four conditions that agree today.</para></summary>
    private void FireOnTheLock(DesignateTarget target)
    {
        if (_surface is not { } ex || _designateUnit is not { } unit)
        {
            return;
        }
        SurfaceBot? gun = ex.Bots.FirstOrDefault(b => b.Unit == unit && b.Deployed);
        if (gun is null)
        {
            _remoteOutcome = "🎯 That gun is not standing there any more.";
            CloseDesignate();
            StateHasChanged();
            return;
        }

        // ASKED AGAIN AT THE TRIGGER, not trusted from the scan. The list on the panel is a snapshot taken
        // when the gun was picked, and the world has had every frame since then to change: a door between
        // them can have cycled shut behind the captain's back. A shot resolved off a stale row is the sim
        // and the screen disagreeing about what a gun can see, which is the one thing this feature must not
        // do — and re-asking costs one line.
        IReadOnlyList<SurfaceCollision.Segment> walls = SightBlockers();
        if (!ShootTheLock.CanBreak(gun.X, gun.Y, target.X, target.Y, walls))
        {
            _remoteOutcome = ShootTheLock.NothingInViewLine(gun.Unit);
            RendererInterop.PlayCue("block");
            ScanForLocks(unit);
            StateHasChanged();
            return;
        }

        Ammunition.Kind kind = Ammunition.ById(gun.AmmoId);
        ShootTheLock.Order order = ShootTheLock.Fire(
            gun.Unit, target.Sign, gun.Rounds, kind,
            ShootTheLock.RangeDu(gun.X, gun.Y, target.X, target.Y));

        if (!order.Fired)
        {
            // A refusal costs nothing and is said on the handset, where the press was made (#769).
            _remoteOutcome = order.Line;
            RendererInterop.PlayCue("block");
            StateHasChanged();
            return;
        }

        gun.Rounds = order.Magazine;
        gun.AimX = target.X;
        gun.AimY = target.Y;
        gun.FiringUntilMs = (_lastTimestampMs ?? 0) + 400;

        // The lock is open in the building's own grammar: the wall behind it goes, the leaf becomes a way
        // through, and the plate stays readable wearing a hole instead of a padlock.
        TheLockComesOff(ex, target);

        // THE NOISE. Two things happen and they are deliberately different: the pack's ear is rung the way
        // every other loud act on this ground rings it (a PLACE to walk to, #456), and the FACT is filed —
        // who fired, at what, where, when, how many.
        //
        // #618 · AND THE ROUND READS THAT LEDGER NOW. This line is the whole of the publication: nothing new
        // is pressed, nothing new is drawn, and this method gained no call. The round asks the excursion once
        // a frame (Patrol.TheRoundHearsAShot), so the man who was standing thirty du away starts walking over
        // on the next one — and if he sees the captain on the way, it is the same hail it has always been.
        MakeNoise(target.X, target.Y, ReeverHearing.Noise.Gunfire);
        var shot = new GunfireHeard.Shot(
            gun.Unit, target.Sign, target.X, target.Y, SimTime, order.RoundsSpent);
        ex.ShotsHeard = [.. GunfireHeard.File(ex.ShotsHeard, shot)];

        RendererInterop.PlayCue("fire");

        // ── TOLD, WHERE THEY ARE LOOKING, IN ONE BREATH (#761 / #769 / #774) ──
        //
        // A gun going off indoors is the plot-significant thing here, and this event has three things to say
        // at once: the shot, what was behind the door, and — the first time only — what the noise actually
        // cost. Said as THREE calls they would be three writes to one slot and the captain would read the
        // last one; a region has room for all of them, which is the whole of #774. So the paragraphs are
        // composed here and the outcome is written once, and the book keeps each of them separately because
        // a record is a list of facts and not a paragraph.
        var said = new List<string> { order.Line, ShootTheLock.BehindItLine(target.Sign) };
        if (!ex.GunfireWarned)
        {
            ex.GunfireWarned = true;
            said.Add(GunfireHeard.WhatItCostLine);
        }
        SayItWhereTheyAreLooking(string.Join("\n\n", said));

        FileNote(order.Line, "🔫");
        FileNote(GunfireHeard.BookLine(shot), "🔫");

        // #835 · …AND WHETHER ANYBODY ON THE ROTA WAS LOOKING AT YOU WHILE YOU DID IT. The comment above says
        // "nothing in this build reads the ledger; #804 prices it" — and this is not that: the ledger is
        // still unread, and a bang heard three corridors away still buys nothing. This is the one man who was
        // watching your hands, and it is the only crime these floors have a verb for. Below the surface only,
        // his eye only; on a moon's own regolith there is nobody to see it.
        SomebodySawThat(Core.PatrolBeat.Provocation.SeenAtTheHasp);

        // The list the captain is looking at is now wrong — that door is a doorway. Rescan rather than
        // patch: one source for what a gun can see, asked again.
        ScanForLocks(unit);
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>
    /// Reverse the standing order for the boat, from the remote, wherever the captain is standing. Owner:
    /// <i>"Shuttle warm up should work from remote"</i> — so this is the ONE place the boat is commanded, and the
    /// lock merely reports what it finds.
    /// </summary>
    private void ToggleBoatPower()
    {
        _boatOrderedCold = !_boatOrderedCold;

        SayItWhereTheyAreLooking(_boatOrderedCold ? SilentRunning.BoatDarkLine : SilentRunning.BoatWakingLine);
        LogAutopilotEvent(_boatOrderedCold
            ? $"🛸 Rigging the boat down — {SilentRunning.SecondsLeft(_boatWarmth, true):0} s to quiet."
            : $"🛸 Bringing the boat up — {SilentRunning.SecondsLeft(_boatWarmth, false):0} s to her hatch.");

        // Said once, the first time a captain orders her up while something else is already counting: this is the
        // scene the owner sketched — "self destruct tics down but shuttle still warms up".
        if (!_boatOrderedCold && !_twoClocksSaid && SomethingElseIsCountingDown)
        {
            _twoClocksSaid = true;
            LogAutopilotEvent(SilentRunning.TwoClocksLine);
        }

        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    private bool _twoClocksSaid;

    /// <summary>Whether a hull clock is already running against her warm-up — the overload on either keel.</summary>
    private bool SomethingElseIsCountingDown => _scuttleSecondsLeft is not null || _shipChargesSeconds is not null;

    /// <summary>
    /// Run the boat's dial in whichever direction her standing order points. Ticked wherever the captain is,
    /// because a captain who ordered her up and then went back for one more crate should find her open.
    /// </summary>
    private void AdvanceBoatSpinUp(double dtSeconds)
    {
        SilentRunning.BoatState was = BoatState;
        _boatWarmth = SilentRunning.AdvanceWarmth(_boatWarmth, _boatOrderedCold, dtSeconds);
        SilentRunning.BoatState now = BoatState;

        if (now == was)
        {
            return;
        }

        // Arriving at either end is worth saying out loud: one of them opens a hatch, the other closes it.
        ShowPulseMessage(SilentRunning.SpinUpLabel(_boatWarmth, _boatOrderedCold));
        if (now == SilentRunning.BoatState.Warm)
        {
            RendererInterop.PlayCue("door");
        }
    }

    /// <summary>Whether the boat will take the captain anywhere right now — asked by the lock before it offers to
    /// leave. This is where the warm-up is CHARGED, which is the owner's own framing.</summary>
    private bool BoatReadyToFly()
    {
        if (SilentRunning.ReadyToFly(_boatWarmth, _boatOrderedCold))
        {
            return true;
        }

        if (_boatOrderedCold)
        {
            // Asked for a ride while she is going or gone dark: waking her IS the answer, and it starts now.
            ToggleBoatPower();
            return false;
        }

        ShowPulseMessage(SilentRunning.NotARideYetLine(BoatSecondsLeft));
        RendererInterop.PlayCue("block");
        return false;
    }
}
