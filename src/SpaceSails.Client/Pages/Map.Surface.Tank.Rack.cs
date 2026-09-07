using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #608 · ONE RACK LAW, TWO BUILDINGS — the underground refuges are the surface shelter's mechanic
/// underground, so they are the surface shelter's CODE and not a second copy of it.
///
/// <para>Everything that decides how much air moves lives in one place, and both buildings ask it. Two
/// reservoirs answering the same question with different arithmetic is this house's most expensive shape,
/// and this is the file that refuses to have one.</para>
///
/// <para>Split out of <c>Map.Surface.Tank.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ── #608 · ONE RACK LAW, TWO BUILDINGS ──────────────────────────────────────────────────────────────
    //
    // The underground refuges are the surface shelter's mechanic, underground — so they are the surface
    // shelter's CODE, not a second copy of it. Everything that decides how much air moves lives in
    // SurfaceShelter (Produce, Transfer, the two-thirds ceiling somebody set for the next person through the
    // door) and both callers step through this one function.
    //
    // The reason it is a function rather than a comment saying "keep these in sync": this project's most
    // expensive habit is two places that have to agree and only one being changed, and the two racks are a
    // perfect candidate — they will be tuned by somebody reading one of them. #573's shelter and #608's
    // refuge now cannot drift, because there is nothing to drift.

    /// <summary>#612 + #608 · IS THE TANK RUNNING? ONE ANSWER, READ BY THE SIM AND BY THE GAUGE.
    ///
    /// <para>#612 shipped the <c>AIR: TANKS / ROOM</c> source on the hud because the owner asked <i>"where
    /// here does it say if I consume tanks or have air?"</i> — and its own issue states the hard part: the
    /// gauge <b>"has to agree with the plate by the lift on every floor. Two instruments disagreeing about
    /// whether you can breathe is worse than one instrument saying nothing."</b></para>
    ///
    /// <para>It was computed as its own expression beside <c>StepSuitAir</c>'s branches, which is the exact
    /// arrangement this project keeps paying for: two places that must agree, and only one gets changed. It
    /// did not survive its first contact with a new way to breathe. A refuge (#608) stops the drain, and the
    /// hud went on reading TANKS while the sim was not spending anything — a captain sitting in air being
    /// told, in colour, that their tank was running out.</para>
    ///
    /// <para>So the drain is gated on this and the gauge is fed from this. Anything that ever becomes a new
    /// place to breathe is added HERE, once, and both follow.</para>
    ///
    /// <para><b>And "here" is now Core</b>, because this client-side version could still only be read by a
    /// client: the plate <c>HiveInterior</c> paints by the lift was calling
    /// <c>UndergroundComplex.HoldsPressure</c> for itself and spelling its own words, which made a THIRD
    /// answer to the same question. <see cref="SuitAir.SourceOf"/> is the predicate; this method is the one
    /// place that gathers the four facts to hand it, and every surface reads what it says.</para></summary>
    /// <para><b>#621 · and the third fact was answered with the wrong world's rule.</b> "Aboard" was
    /// <c>MoonSurface.IsSafeAboard(_avatarY)</c> — the regolith's top rim at y = −20 — while a derelict's
    /// whole deck runs −9 to +9, so every point aboard every wreck said YES. The gauge told a captain
    /// standing in a hull that has held vacuum for years that they were on HER AIR and their tank was
    /// FILLING, and the drain agreed with it. <see cref="AwayTeamSide.BackAtTheShuttle"/> is the one place
    /// that knows which door you are on the far side of, and both the reach rule and this one now read
    /// it.</para>
    private SuitAir.Supply AirSupplyOf(SurfaceExcursion ex) =>
        SuitAir.SourceOf(
            ex.Stop.Body.Id,                                 // #677 which building — the halls breathe
            ex.Floor,
            StandingInTheShelter(ex),                        // #573 the deep shelter
            CaptainBeyondReach,                              // her tube — or past a wreck's lock: breathing hers
            BreathingRefugeUnderfoot(ex));                   // #608 a pressure refuge that still holds

    /// <summary>Is the tank running? The one bit of <see cref="AirSupplyOf"/>, for callers that want no
    /// more than that.</summary>
    private bool TankIsDrawing(SurfaceExcursion ex) => SuitAir.Drawing(AirSupplyOf(ex));

    /// <summary>Run one rack for <paramref name="dt"/> seconds: it makes air, it moves what it can into the
    /// suit, and the warnings re-arm if anything went in. Returns the reservoir it is left holding, and
    /// reports how much reached the tank.</summary>
    private double DrawFromRack(SurfaceExcursion ex, double held, double dt, out double pumped)
    {
        double made = SurfaceShelter.Produce(held, dt);
        pumped = SurfaceShelter.Transfer(ex.AirSeconds, made, SuitAir.TankSeconds, dt);
        if (pumped > 0)
        {
            ex.AirSeconds = SuitAir.Refill(ex.AirSeconds, pumped);
            ex.AirLowWarned = false;
            ex.AirWarned = false;
            ex.ReserveNoted = false;
        }
        return made - pumped;
    }

    /// <summary>#608 · The refuges on the floor the captain is standing on, read off the deck the renderer
    /// actually drew.
    ///
    /// <para><b>Not rebuilt from Core.</b> <c>UndergroundComplex.Build</c> is pure but not free, and this is
    /// asked every frame by the suit; more importantly, a second call would be a second answer. The consoles
    /// on <c>_deckPlan</c> ARE the refuges — <see cref="HiveInterior.FloorDeck"/> put them there off the
    /// floor plan — so the room the captain can see and the room that holds their air are the same object by
    /// construction rather than by two functions agreeing.</para></summary>
    private List<(double X, double Y)> RefugesOn()
    {
        var found = new List<(double, double)>();
        foreach (DeckPlan.ConsoleSpot spot in _deckPlan.Consoles)
        {
            if (spot.Kind == DeckPlan.ConsoleKind.HiveRefuge)
            {
                found.Add((spot.X, spot.Y));
            }
        }
        return found;
    }

    /// <summary>#608 · What the seal on this floor's refuge has done with the decades, or null off a floor
    /// that has one. Asked of Core rather than carried, because the state is a fact about the FLOOR and
    /// there is one refuge on it: <see cref="UndergroundComplex.StateOfTheRefugeOn"/> is the one answer the
    /// suit, the plate, the tracker and the panel all read.</summary>
    private UndergroundComplex.RefugeState? RefugeSealHere(SurfaceExcursion ex) =>
        ex.Floor >= 0 ? null : UndergroundComplex.StateOfTheRefugeOn(ex.Stop.Body.Id, ex.Floor);

    /// <summary>#608 · Is the captain standing in air that a refuge is providing? Both halves — inside the
    /// box AND the box still holds — because a failed refuge is a room on a dead floor and nothing else, and
    /// the gauge saying ROOM in one would be the instrument lying at the one door on the floor a captain
    /// walked a tank to reach.</summary>
    private bool BreathingRefugeUnderfoot(SurfaceExcursion ex) =>
        ex.Floor < 0
        && RefugeUnderfoot(ex) >= 0
        && RefugeSealHere(ex) is { } seal
        && UndergroundComplex.RefugeStillHolds(seal);

    /// <summary>Which refuge the captain is standing inside, or -1. Never anything but -1 above ground.
    /// GEOMETRY ONLY — whether that room has anything in it is <see cref="RefugeSealHere"/>'s business, and
    /// keeping the two apart is what lets a failed refuge still say its line at the door.</summary>
    private int RefugeUnderfoot(SurfaceExcursion ex)
    {
        if (ex.Floor >= 0)
        {
            return -1;
        }
        List<(double X, double Y)> all = RefugesOn();
        for (int i = 0; i < all.Count; i++)
        {
            if (UndergroundComplex.RefugeHolds(all[i].X, all[i].Y, _avatarX, _avatarY))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>A refuge rack's reservoir, in suit-seconds — the shelter's own story, told about a room
    /// under a moon. Keyed per FLOOR, so walking back into B7's refuge finds it as you left it and B3's is
    /// somebody else's problem.</summary>
    private double RefugeReservoirNow(SurfaceExcursion ex, int index)
    {
        if (index < 0)
        {
            return 0;
        }

        // #608 · A rack only exists behind a door that cycles. On a FAILED refuge this is zero at the
        // source rather than zero by the caller remembering to ask — a reservoir that could be read out of a
        // room nobody can get into is one refactor away from filling a tank from one.
        //
        // #1149 · …and EMPTY is no longer on that list. The rack in an empty refuge is a real, working,
        // producing rack; what it has not got is anything IN it, because a visitor took the lot.
        if (RefugeSealHere(ex) is not { } seal || !UndergroundComplex.RefugeStillHolds(seal))
        {
            return 0;
        }
        int key = RefugeKey(ex.Floor, index);
        if (ex.RefugeReservoir.TryGetValue(key, out double held))
        {
            return held;
        }

        // #573's idiom, underground: a rack that is not full means SOMEBODY WAS HERE. Down here that is a
        // colder sentence than it is on the regolith — the building has been shut for decades and the seals
        // on this room have not — and it costs nothing but a seeded roll.
        //
        // #1149 · THREE RUNGS, and the deepest one is Core's. An EMPTY refuge is the floor's own state
        // (UndergroundComplex.StateOfTheRefugeOn) and starts the rack at nothing: somebody drew it right
        // down, the plate at range says DRY, and the cracker starts giving it back the moment the captain
        // walks in. The shallower rung is the surface shelter's own roll, unchanged.
        double start = seal == UndergroundComplex.RefugeState.Empty
            ? 0
            : SurfaceShelter.SomebodyWasHere(
                    ex.Stop.Body.Id, $"{ex.Site.LayoutSalt}:hive{ex.Floor}", index)
                ? SurfaceShelter.ReservoirSeconds * 0.42
                : SurfaceShelter.ReservoirSeconds;
        ex.RefugeReservoir[key] = start;
        return start;
    }

    /// <summary>One key per refuge per floor, so B2's rack is not B3's. Same shape as
    /// <see cref="HiveInterior.RoomKey"/>, and deliberately a different dictionary.</summary>
    private static int RefugeKey(int level, int index) => (level * 1000) - index;

    /// <summary>#608 · What a rack — either rack — says when it is asked how it is doing. One reading of one
    /// machine, so the shed on the regolith and the refuge eleven floors down can never describe the same
    /// state in two different ways.</summary>
    private static string RackGaugeLine(SurfaceExcursion ex, double held) =>
        ex.AirSeconds >= SuitAir.TankSeconds * SurfaceShelter.FillToFraction
            ? SurfaceShelter.PumpDoneLine
            : held > SurfaceShelter.ReservoirSeconds * 0.1
                ? SurfaceShelter.PumpingLine
                : SurfaceShelter.TrickleLine;

    /// <summary>#573 · Raise the tank-is-low card, once per captain ever. Returns true when it went up, so
    /// the caller keeps its pulse line for every later trip.</summary>
    private bool ShowAirCardOnce()
    {
        if (_airCardSeen)
        {
            return false;
        }
        _airCardSeen = true;
        _airCardOpen = true;
        RequestVaultSave();
        StateHasChanged();
        return true;
    }

    /// <summary>#562 · Raise the tube-feeds-you card, once per captain ever. Returns true when it went up,
    /// so the caller keeps its receipt line for every later racking. The card teaches the shape of an
    /// excursion — one anchor, plan the route home — and the receipt is right for a captain who knows.</summary>
    private bool ShowTubeRearmCardOnce()
    {
        if (_tubeRearmSeen)
        {
            return false;
        }
        _tubeRearmSeen = true;
        _tubeRearmOpen = true;
        RequestVaultSave();
        StateHasChanged();
        return true;
    }
}
