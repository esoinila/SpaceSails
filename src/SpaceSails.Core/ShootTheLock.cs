using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #803 · POINTING A GUN AT SOMETHING, ON PURPOSE. Owner, 2026-08-09: <i>"we will need to use the handheld
/// captain's control to set the guns / gun to fire at something manually, that UI is missing"</i> — and the
/// first thing worth pointing one at: <i>"shooting a mechanical lock"</i>.
///
/// <para><b>This is the scrappy opposite of autopilot law.</b> Nothing here aims itself, nothing here
/// decides. A sentry's own volley (<see cref="SentryBot.Step"/>) is a machine choosing targets inside an
/// arc; this is a captain choosing one thing, once, and saying when. The orbit-keeping ruling does not
/// reach it and is not meant to.</para>
///
/// <h3>Which locks</h3>
/// <para>The game's own grammar had already made this call, in two sentences written years apart and never
/// put side by side:</para>
/// <list type="bullet">
/// <item>A room door — <c>SatchelTry.AtRoomDoor</c>: <i>"The lock is <b>mechanical</b> and it was turned by
/// somebody who then walked away with the key."</i> That is a hasp. A hasp is hardware, and hardware
/// breaks.</item>
/// <item>A sealed way — <c>SatchelTry.AtSealedWay</c>: <i>"There is no reader on this. No slot, no plate,
/// no panel — the bolts go through the frame and into the rock, and they were tightened from a side you are
/// not on."</i> That is not a lock. It is a wall with a sign on it (#590 call 2), and a wall does not care
/// what you shoot it with.</item>
/// </list>
/// <para>So the honest set is: <b>you can shoot a LOCK. You cannot shoot a WALL, and you cannot shoot a
/// READER</b> — the last one because breaking a reader does not make it say yes, it only stops it from ever
/// saying anything. Everything this file classifies, it classifies by POSITIVE recognition (the sign is in
/// a door vocabulary, or it is the goods hoist's own plate); an unrecognised sign is refused rather than
/// waved through, so a lock nobody thought about can never become shootable by accident.</para>
///
/// <h3>What it costs</h3>
/// <para>Rounds, and NOISE. The rounds are the tether the whole ammunition lane runs on (#562), and the
/// noise is the part that makes this a decision: a gun going off indoors is heard by everything with ears
/// and the world writes it down (<see cref="GunfireHeard"/>). #618 · <b>And it is priced now.</b> The pack's
/// own ear was always rung; the guards' lane is what has arrived — a shot within earshot of a man on a
/// patrolled floor takes him off his round to come and look at where it came from, and costs a rung of that
/// site's own outfit's memory (<see cref="IllegalHeat.Crossing.ShotOnTheirFloor"/>). Neither of those reaches
/// back into this file: the fact was recorded from the first shot, so nothing had to be re-derived later from
/// a screen that has since scrolled, and pricing it needed no new record.</para>
/// </summary>
public static class ShootTheLock
{
    /// <summary>What a thing with a sign on it turns out to be, when somebody points a gun at it.</summary>
    public enum Verdict
    {
        /// <summary>A hasp, a bolt, a roller shutter — hardware somebody turned, holding a door shut.
        /// Hardware breaks.</summary>
        MechanicalLock,

        /// <summary>Nothing on it to break. The sealed ways: bolts through the frame and into the rock,
        /// tightened from the far side. A round takes the paint off and changes nothing.</summary>
        NoLockToBreak,

        /// <summary>It reads rather than locks. Shooting a reader does not persuade it; it only makes sure
        /// it will never say yes to anybody.</summary>
        ReadsRatherThanLocks,
    }

    /// <summary>
    /// What one lock costs a magazine. Six flat cracks at a hasp — enough that a captain feels the drum
    /// move and cannot open a corridor of doors on a hut's find, few enough that <i>"sometimes we find like
    /// 6 rounds"</i> is exactly one door. FLAGGED for the owner's tuning; it is the only number in this
    /// feature that is a feel call rather than a consequence.
    /// </summary>
    public const int RoundsPerHasp = 6;

    /// <summary>
    /// What THESE rounds cost against a lock. A two-stage penetrator does its work on the far side of the
    /// first thing it meets (#603) — which is precisely what a hasp is — so one is enough and the round's
    /// own <c>Penetrates</c> is what says so. Nothing here special-cases an id: the property that makes the
    /// round good at this is the property that is read.
    /// </summary>
    public static int RoundsFor(Ammunition.Kind kind) => kind.Penetrates > 1 ? 1 : RoundsPerHasp;

    /// <summary>
    /// WHICH GUNS A CAPTAIN MAY POINT. Three conditions, and each one is the feature rather than a
    /// safety rail:
    /// <list type="bullet">
    /// <item><b>Set down.</b> A bot in the sling is a machine you are carrying, not a machine holding a
    /// position — there is nothing at the far end of the handset to command.</item>
    /// <item><b>Something in the drum.</b> A gun reading 00 is the whole reason the put verb exists, and
    /// offering it here would be the handset promising what the magazine cannot pay for.</item>
    /// <item><b>Yours.</b> The tube's own GATE-1 hangs in the shuttle door, is topped back up after every
    /// volley and is "never counted against the sling" by its own law — the same exclusion the #797
    /// magazines readout makes, so the instrument and the handset cannot disagree about what you own.</item>
    /// </list>
    /// </summary>
    public static bool MayPoint(bool deployed, int rounds, bool isDoorSentry) =>
        deployed && rounds > 0 && !isDoorSentry;

    /// <summary>Read the sign and say what is holding that door.</summary>
    public static Verdict Judge(string? sign)
    {
        if (sign is not { Length: > 0 })
        {
            return Verdict.NoLockToBreak;
        }
        // #1074 · …and the order's seal answers with them. It is asked through UndergroundComplex.HasNoReader
        // rather than through IsSealedWay alone so that this file and the satchel cannot come to two opinions
        // about what a captain is standing in front of. A stop order is a leaf welded shut and stamped by an
        // office: there is no hasp on it, and the honest refusal is the sealed way's, not the reader's.
        if (UndergroundComplex.HasNoReader(sign))
        {
            return Verdict.NoLockToBreak;
        }
        if (UndergroundComplex.IsFreightShutter(sign) || UndergroundComplex.IsDoorSign(sign))
        {
            return Verdict.MechanicalLock;
        }
        return Verdict.ReadsRatherThanLocks;
    }

    /// <summary>Is this one worth a round at all?</summary>
    public static bool IsShootable(string? sign) => Judge(sign) == Verdict.MechanicalLock;

    /// <summary>Why not, in the door's own terms. A lock the story needs shut stays shut AND SAYS SO — this
    /// is that sentence, and it never hints that some other round, gun or angle would do it.</summary>
    public static string RefusalLine(string sign)
    {
        ArgumentNullException.ThrowIfNull(sign);
        return Judge(sign) switch
        {
            Verdict.NoLockToBreak =>
                $"🔒 There is nothing on {sign} to break. No hasp, no bolt you can reach — the frame is " +
                "pinned into the rock from a side you are not on. A round would take the paint off it and " +
                "tell every ear in the building where you are standing.",
            Verdict.ReadsRatherThanLocks =>
                $"🔒 {sign} reads; it does not latch. Putting a round through the panel does not make it " +
                "say yes — it makes sure it never says anything to anybody again, including you.",
            _ => $"🔓 {sign} is a hasp. It would go.",
        };
    }

    // ── AIMING ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How far in front of the lock the gun actually has to see: a hand's width on its own side of
    /// the door.
    ///
    /// <para>Not a nicety. A lock's console spot sits ON the door, and the door has a wall segment lying on
    /// exactly the same line (the building draws every door that will not open with real stone behind it),
    /// so a sight test aimed at the lock's own centre is a test aimed INTO the wall it is asking about — it
    /// returns "no line" for every lock in the game, forever, and every guard written against it passes on
    /// a world where nothing is ever shootable. The gun sees the FACE of the door; that is what it shoots
    /// at, and that is what this offset is.</para></summary>
    public const double StandOffDu = 1.0;

    /// <summary>The point on the door's face a gun at (<paramref name="gunX"/>, <paramref name="gunY"/>)
    /// must have a clear line to.</summary>
    public static (double X, double Y) AimPointOn(double lockX, double lockY, double gunX, double gunY)
    {
        double dx = gunX - lockX, dy = gunY - lockY;
        double d = Math.Sqrt((dx * dx) + (dy * dy));
        if (d <= 0.0001)
        {
            return (lockX, lockY);
        }
        return (lockX + (dx / d * StandOffDu), lockY + (dy / d * StandOffDu));
    }

    /// <summary>
    /// Can this gun, standing here, put rounds into that lock? The SAME two questions a sentry asks of an
    /// Old One — inside the arc (<see cref="SentryBot.RangeDeckUnits"/>) and with stone out of the way
    /// (<c>SurfaceCollision.HasLineOfSight</c>) — because a captain who has learned what a sentry can cover
    /// has learned this too, and two range laws would be two answers to one question.
    ///
    /// <para>Range is measured to the LOCK. Sight is measured to its FACE (<see cref="AimPointOn"/>).</para>
    /// </summary>
    public static bool CanBreak(
        double gunX, double gunY, double lockX, double lockY,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        if (!SentryBot.InRange(gunX, gunY, lockX, lockY))
        {
            return false;
        }
        (double ax, double ay) = AimPointOn(lockX, lockY, gunX, gunY);
        return SurfaceCollision.HasLineOfSight(gunX, gunY, ax, ay, walls);
    }

    /// <summary>How far the lock is, for the readout and for the round's own arming rule.</summary>
    public static double RangeDu(double gunX, double gunY, double lockX, double lockY)
    {
        double dx = lockX - gunX, dy = lockY - gunY;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    // ── THE ORDER ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The settled result of one designated shot.</summary>
    /// <param name="Fired">False is a refusal, and <paramref name="Line"/> says which one.</param>
    /// <param name="RoundsSpent">Taken off the drum. Zero on a refusal — a gun that will not fire does not
    /// pay for the trigger pull.</param>
    /// <param name="Magazine">What the counter reads afterwards.</param>
    /// <param name="Broken">Whether the lock is now open. Only ever true when <paramref name="Fired"/> is.</param>
    public readonly record struct Order(bool Fired, int RoundsSpent, int Magazine, bool Broken, string Line);

    /// <summary>
    /// Fire on a lock. Pure: the caller owns the drum, the door and the noise, and every one of them moves
    /// off this one answer so the sim and the sentence cannot come apart.
    ///
    /// <para>Refusals, in the order they are asked:</para>
    /// <list type="number">
    /// <item>The thing is not a lock — the door's own refusal (<see cref="RefusalLine"/>).</item>
    /// <item>The round will not fire at that range — <c>Ammunition.MayFire</c>'s own words, unchanged. A
    /// two-stage penetrator fired at a door six du away goes off level with the captain, and the round says
    /// so better than a new sentence would.</item>
    /// <item>Not enough left in the drum.</item>
    /// </list>
    /// </summary>
    public static Order Fire(string unit, string sign, int rounds, Ammunition.Kind kind, double rangeDu)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(sign);
        int have = Math.Clamp(rounds, 0, SentryBot.MaxMagazine);

        if (!IsShootable(sign))
        {
            return new(false, 0, have, false, RefusalLine(sign));
        }

        (bool allowed, string? refusal) = Ammunition.MayFire(kind, rangeDu);
        if (!allowed)
        {
            return new(false, 0, have, false, refusal!);
        }

        int cost = RoundsFor(kind);
        if (have < cost)
        {
            return new(false, 0, have, false,
                $"🔫 {unit} has {SentryBot.Readout(have)} left and a hasp takes {cost}. Walk some rounds " +
                "out to it, or find another way in.");
        }

        return new(true, cost, have - cost, true, BrokenLine(unit, sign, cost, kind));
    }

    /// <summary>The shot itself. It is told as an EVENT and not as a receipt, because a gun going off
    /// indoors is the plot-significant thing here (#761) — the door coming open is only the consequence.</summary>
    public static string BrokenLine(string unit, string sign, int cost, Ammunition.Kind kind)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(sign);
        string burst = cost == 1
            ? $"{unit} fires once. The {kind.Name} goes through the plate and does its work behind it"
            : $"{unit} fires — {cost} flat cracks in a corridor with a ceiling on it";
        return
            $"🔫 {burst}, and the hasp comes off {sign}. The door stands a hand's width open on its own " +
            "weight. The sound goes down the spine and keeps going for longer than you expect it to.";
    }

    /// <summary>What is on the other side of a door somebody shut and walked away from — said once, when it
    /// swings. It describes the room and accounts for nothing: no department is explained, nothing down here
    /// says what any of it was for (§13.8).</summary>
    public static string BehindItLine(string sign)
    {
        ArgumentNullException.ThrowIfNull(sign);
        return UndergroundComplex.IsFreightShutter(sign)
            ? "🕳 The shutter goes up half its height and jams. Behind it is a steel car with a floor of " +
              "dried mud in it and a hand-rail worn bright on one side. Nothing has been carried anywhere " +
              "in it for a long time."
            : $"🕳 {sign}, open. Bare floor, bare walls, and four rows of bolt holes where shelving was " +
              "taken out. Whatever this department did, it did not leave it in here.";
    }

    // ── WHAT THE HANDSET SAYS ─────────────────────────────────────────────────────────────────────────

    /// <summary>The mode's own name on the remote, and what it is for.</summary>
    public const string PanelTitle = "🎯 DESIGNATE";

    /// <summary>Under the title. States the whole idiom in one breath, including the part a captain has to
    /// accept before they press anything.</summary>
    public const string PanelBlurb =
        "Pick a gun you have set down, pick something it can see, and say when. Nothing here aims itself, " +
        "and everything with ears hears it.";

    /// <summary>The button that opens the mode, on the handset beside the other three switches.</summary>
    public const string OpenLabel = "🎯 Designate a target";

    /// <summary>…and the way back out of it.</summary>
    public const string BackLabel = "◀ Back to the switches";

    /// <summary>One gun's row: who it is, what it is holding, and what that means.</summary>
    public static string GunRow(string unit, int rounds, Ammunition.Kind kind) =>
        $"{unit} · {SentryBot.Readout(rounds)}/{SentryBot.MaxMagazine} · {kind.Name}";

    /// <summary>
    /// One target's row: what is painted on it, how far off it is, and what pressing it will do.
    ///
    /// <para>A row for something with nothing on it to break must NOT quote a price. Six rounds beside a
    /// sealed way would be the panel offering a trade the world will refuse — and a captain who presses it
    /// expecting to spend six rounds and gets a sentence has been misled by the control rather than taught
    /// by it. It still presses, and it still answers; what it does not do is lie about the tariff.</para>
    /// </summary>
    public static string TargetRow(string sign, double rangeDu, int cost) =>
        IsShootable(sign)
            ? $"🔒 {sign} · {rangeDu:F0} du · {cost} round{(cost == 1 ? "" : "s")}"
            : $"🔒 {sign} · {rangeDu:F0} du · nothing on it to break";

    /// <summary>The trigger.</summary>
    public static string FireLabel(int cost) => $"🔫 FIRE — {cost} round{(cost == 1 ? "" : "s")}";

    /// <summary>Nothing of the captain's is standing anywhere. Names the fact and the fix.</summary>
    public const string NothingSetDownLine =
        "🎯 Nothing of yours is standing anywhere down here. A gun in the sling is a gun you are carrying — " +
        "set one down and it becomes a gun you can point.";

    /// <summary>Everything set down reads 00. The other half of the same sentence, and the reason the put
    /// verb exists.</summary>
    public const string EverythingDryLine =
        "🎯 Every gun you have set down reads 00. Pointing one is the only part of this that still works — " +
        "walk it some rounds first.";

    /// <summary>A gun with nothing it could break in front of it. Says what "in view" means, because the
    /// answer is the same one the captain already knows from watching a sentry hold a lane.</summary>
    public static string NothingInViewLine(string unit) =>
        $"🎯 {unit} has nothing in front of it worth a round. It has to be inside the gun's own arc and in " +
        "its line — stone stops the gun exactly the way it stops you.";
}
