using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #821 · THE CUBICLE YOU LOCK FROM INSIDE, AND THE TAP THAT HAS AN OPINION.
///
/// <para>Owner, standing in the park on the evening of 2026-08-11: <i>"let's add toilets there.. we might
/// want to hide from guards in one toilet cubicle we lock from inside :-D"</i>, and then <i>"also a function
/// to wash hands with some film noir comment at the end ... about if we ever feel like our hands are
/// clean."</i></para>
///
/// <h3>THE SENTRY'S OWN LAW, and where it is kept</h3>
///
/// <para><b>It buys time, not safety.</b> The whole of that lives in two places and neither of them is a
/// timer: <see cref="CubicleLock"/> says what a shut door MEANS, and <c>Map.Patrol.cs</c> makes the man who
/// watched you shut it walk over and stand outside. This file is only the press — it turns a catch, it
/// remembers which catches are over, and it hands the fact to the deck.</para>
///
/// <h3>The shut door is a WALL, and it is the same wall the never-opening doors use</h3>
///
/// <para>A locked cubicle is a segment in the collision field, appended by <c>HiveInterior.FloorDeck</c>
/// exactly the way <see cref="UndergroundComplex.LockedDoor"/> has been since #585 — so "the door refuses
/// entry from the outside" is a fact about the ground a body walks on rather than a check somebody
/// remembered to write at a call site. The set of shut cells is replayed onto every rebuild (#803's own
/// idiom, for its reason: a room searched or a bin used rebuilds this deck, and a door that came open again
/// while the captain sat still behind it would be the world growing its walls back).</para>
/// </summary>
public partial class Map
{
    /// <summary>#821 · Is this cell's catch over? One question, asked of the one set the deck is rebuilt
    /// from, so the door drawn shut and the door the ladder calls private are one door.</summary>
    private bool CubicleIsShut(string key) =>
        _surface is { } ex && ex.CubiclesShut.Contains(key);

    /// <summary>#821 · This floor's cubicles, in the order Core published them.</summary>
    private readonly List<(UndergroundComplex.RingRoom Room, RingOffice.Stall Cell)> _floorCubicles = [];

    /// <summary>
    /// #821 · Every cubicle on this floor. A lookup and never a measurement (§13.15): the cells and their
    /// leaves were paired by the placer that laid both.
    ///
    /// <para><b>Read ONCE per floor and kept.</b> The round asks this every frame — is the captain shut in? —
    /// and <c>UndergroundComplex.Build</c> is not free: it carves a whole floor, allocating a dozen lists and
    /// several hundred wall segments, and this repo runs in WASM where a Debug build is a hundred times
    /// slower than the machine it is written on. A per-frame floor carve is the shape of hitch #832 was paid
    /// for, and it would have been invisible to every test in the repository.</para>
    ///
    /// <para>The generator is pure and deterministic per (site, level), so a cache of it cannot go stale —
    /// which is why the key is exactly those two facts and nothing about the state of any door.</para>
    /// </summary>
    private List<(UndergroundComplex.RingRoom Room, RingOffice.Stall Cell)> CubiclesOn(SurfaceExcursion ex)
    {
        string key = $"{ex.Stop.Body.Id}|{ex.Floor}";
        if (string.Equals(_seating.CubicleFloorKey, key, StringComparison.Ordinal))
        {
            return _floorCubicles;
        }

        _seating.CubicleFloorKey = key;
        _floorCubicles.Clear();

        if (ex.Floor >= 0
            || UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Park
                is not { } green)
        {
            return _floorCubicles;
        }

        foreach (UndergroundComplex.RingRoom room in green.Frontage)
        {
            foreach (RingOffice.Stall cell in room.Cubicles)
            {
                _floorCubicles.Add((room, cell));
            }
        }
        return _floorCubicles;
    }

    /// <summary>#821 · The cubicle the captain is standing IN, with its key — or null out on the floor. The
    /// box the three partitions were laid on and nothing else (<see cref="RingOffice.Stall.Contains"/>), so
    /// "inside" means the same thing to the catch, to the ladder and to any guard that proves either.</summary>
    private (UndergroundComplex.RingRoom Room, RingOffice.Stall Cell, string Key)? CubicleAround(
        SurfaceExcursion ex)
    {
        foreach ((UndergroundComplex.RingRoom room, RingOffice.Stall cell) in CubiclesOn(ex))
        {
            if (cell.Contains(_avatarX, _avatarY))
            {
                return (room, cell, HiveInterior.CubicleKey(ex.Floor, in cell));
            }
        }
        return null;
    }

    /// <summary>
    /// #821 · [E] ON A CUBICLE'S LEAF. From the inside it turns the catch; from the outside it says why it
    /// will not.
    ///
    /// <para>Which cell it is comes off Core's own published list, matched to the console the press landed on
    /// — a lookup rather than a decision, and never a coordinate this file measured for itself.</para>
    /// </summary>
    private bool TryTurnTheCatch()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return false;
        }

        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveCubicle } spot)
        {
            return false;
        }

        foreach ((UndergroundComplex.RingRoom _, RingOffice.Stall cell) in CubiclesOn(ex))
        {
            if (Math.Abs(cell.DoorX - spot.X) >= SameLeafDu
                || Math.Abs(cell.DoorY - spot.Y) >= SameLeafDu)
            {
                continue;
            }

            string key = HiveInterior.CubicleKey(ex.Floor, in cell);
            bool over = ex.CubiclesShut.Contains(key);

            // THE CATCH IS ON THE INSIDE, which is where catches are. Core owns that sentence and the rule
            // under it, so a dev row and a captain's [E] cannot come to two different answers.
            // …and INSIDE means clear of the leaf, not merely in the box: the catch lays a wall segment on
            // the opening, and a captain who turned it while standing in the doorway would be standing in
            // the thing they had just made. Core's own clearance, off Core's own body radius.
            if (!CubicleLock.MayTurnTheCatch(
                    cell.ClearOfTheLeaf(_avatarX, _avatarY, DeckPlan.AvatarRadius)))
            {
                ShowPulseMessage(
                    over ? CubicleLock.RefusedFromOutsideLine : CubicleLock.StepInsideFirstLine);
                return true;
            }

            if (over)
            {
                OpenTheCubicle(ex, key);
            }
            else
            {
                ShutTheCubicle(ex, key);
            }
            return true;
        }

        return false;
    }

    /// <summary>How close a console has to be to a published leaf to BE that leaf. A rounding tolerance and
    /// not a search radius — the console was hung on the doorway's own midpoint by <c>HiveInterior</c>, and
    /// the only thing between them is a float cast. The office chair's own number
    /// (<c>SameChairDu</c>), for its reason.</summary>
    private const double SameLeafDu = 0.5;

    /// <summary>
    /// #821 · THE CATCH GOES OVER. The plate flips to OCCUPIED, the partition becomes a wall, and — the one
    /// line of this whole feature that decides how it plays — <b>whoever was looking at you when you did it
    /// is remembered</b>.
    ///
    /// <para>That question is asked of <see cref="PatrolBeat.Notices"/>, the same predicate the challenge
    /// itself is gated on, over the same sight blockers, on this very frame. It is asked BEFORE the deck is
    /// rebuilt, because the rebuild is what puts the partition across the opening: asking afterwards would
    /// have every guard in the building answer "no" through the door they just watched you shut, which is the
    /// whole feature quietly not working.</para>
    /// </summary>
    private void ShutTheCubicle(SurfaceExcursion ex, string key)
    {
        // #870 lane 6′a · the bit that gets written is a guard's, so the round writes it — asked with this
        // file's own sight blockers, on this frame, and BEFORE the rebuild below puts the partition across.
        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();
        RememberWhoWatchedTheCatchGoOver(sight);

        ex.CubiclesShut.Add(key);
        RebuildSurfaceDeck();

        ShowPulseMessage(CubicleLock.LockedLine, PulseRank.Beat);
        LogAutopilotEvent(CubicleLock.LockedLine);
        RendererInterop.PlayCue("blip");
        StateHasChanged();
    }

    /// <summary>
    /// #821 · …AND OFF IT AGAIN. If somebody watched you shut this door and has been standing outside it
    /// since, opening it is the moment his walk-up resumes — through #833's own approach, so the card raises
    /// face to face at arm's length exactly as it does anywhere else on the floor. The lock bought the length
    /// of his patience and nothing more.
    /// </summary>
    private void OpenTheCubicle(SurfaceExcursion ex, string key)
    {
        ex.CubiclesShut.Remove(key);
        TheNextHideGetsItsOwnLine();
        RebuildSurfaceDeck();

        ShowPulseMessage(CubicleLock.UnlockedLine);
        LogAutopilotEvent(CubicleLock.UnlockedLine);

        // EVERY guard forgets, and the FIRST one who was waiting takes the stop. Both halves matter, and
        // #870 lane 6′a moved them into the round itself (Map.Patrol.Hide.cs) as one verb, where the reasons
        // are written down: forgetting swept over the whole list, and only one man handed back, because two
        // men doing one job is #777's stacked card. What happens to him is still this file's.
        Guard? waiting = EverybodyForgetsTheCatch();

        if (waiting is { } him)
        {
            ShowPulseMessage(CubicleLock.OpenedOnHimLine, PulseRank.Beat);
            LogAutopilotEvent(CubicleLock.OpenedOnHimLine);

            // #835 · AND HE GETS BACK THE RUNG HE WAS ON, never a softer one.
            //
            // A man who had called it in and was coming at a RUN is still that man: the door stopped his
            // legs, it did not un-say the radio. Hailing him here would hand a captain a way to turn a chase
            // into a polite request for papers by shutting a door on it — which is the door buying SAFETY,
            // and the one thing this feature may never do. He simply carries on (AfterYou is untouched, and
            // he is standing a pace outside), so the ladder resumes where the captain left it.
            //
            // Everybody else gets the hail, which is #833's own approach: the card raises face to face at
            // arm's length, and there is no second challenge road out of this file.
            if (!him.AfterYou)
            {
                TheHail(him);
            }
        }

        StateHasChanged();
    }

    // ── THE TAP ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #821 · [E] AT THE BASIN RUN — a short beat, one pip, and one sentence out of the authored pool.
    ///
    /// <para>Owner: <i>"also a function to wash hands with some film noir comment at the end ... about if we
    /// ever feel like our hands are clean :-D"</i></para>
    ///
    /// <para>The DELIVERY is the customer line's: a sentence set down, no card, no fanfare. Which sentence
    /// is Core's, seeded on (site, watch, beat) exactly as the canteen's regulars are, so a basin repeats
    /// itself only the way a tired thought does — and the beat is the EXCURSION'S, so pressing twice is two
    /// lines rather than one line said twice.</para>
    ///
    /// <para>The pips tick ONCE, and once per watch (<see cref="CubicleLock.WashPipsPerWatch"/>). A row of
    /// four basins is a room and not an income; a captain who stands at a tap for the rest of a shift gets
    /// the sentences and nothing else, and the sentences are the point.</para>
    /// </summary>
    private bool TryWashYourHands()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return false;
        }

        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveBasin })
        {
            return false;
        }

        int beat = ex.WashBeats;
        ex.WashBeats = beat + 1;

        // The pips, once a watch. The ledger is the excursion's own and it is a WATCH stamp rather than a
        // count, so a shift turning over pays again and nothing has to be reset by hand.
        if (ex.WashPaidWatch != ex.CanteenWatch)
        {
            ex.WashPaidWatch = ex.CanteenWatch;
            ApplyNerveRelief(CubicleLock.WashPips * NervePips.PipUnit);
        }

        string said = CubicleLock.WashLine(ex.Stop.Body.Id, ex.CanteenWatch, beat);
        ShowPulseMessage(said, PulseRank.Beat);
        LogAutopilotEvent(said);
        RendererInterop.PlayCue("blip");
        StateHasChanged();
        return true;
    }
}
