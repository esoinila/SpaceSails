using System;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #870 lane 6c · THE OFFICE CHAIR, THE LABORATORY STOOL AND THE CUBICLE'S PAN, as the seat's own verbs —
/// moved off <c>Map.OfficeChair.cs</c>, byte for byte except for the receiver.
///
/// <para>#817's ruling that an office table is the restaurant's table "just more rectangular", #818's stool at
/// a worktop and #821's seat behind a catch are all documented where they have always been documented:
/// <c>Map.OfficeChair.cs</c>'s own class summary. What is here is the PRESS, and what it needs from the page
/// it is drawn on is <see cref="ISeatHost"/> and nothing else.</para>
/// </summary>
public partial class Map
{
    private sealed partial class Seating
    {
        /// <summary>What an office chair's watch-scoped state is keyed on. Its own prefix, so a chair, a bench
        /// and a canteen top with the same ordinal on the same floor can never share a wait counter — three
        /// seats in three rooms, and one key for all of them would be one source consumed as if it were
        /// three.</summary>
        private static string OfficeChairKey(SurfaceExcursion ex, int roomNumber, int chairIndex) =>
            $"{ex.CanteenWatch}:{ex.Floor}:office:{roomNumber}:{chairIndex}";

        /// <summary>
        /// #817 · TAKE A CHAIR — [E] at a desk in a park-view suite, and the captain sits down.
        ///
        /// <para>WHICH chair it is comes off Core's own published list, matched to the console the press landed
        /// on — a lookup rather than a decision, and never a coordinate this file measured for itself
        /// (§13.15).</para>
        /// </summary>
        public bool TryTakeOfficeChair()
        {
            if (_host.Surface is not { } ex || ex.Floor >= 0)
            {
                return false;
            }

            // Already sitting. The press is CONSUMED — [E] is not how you stand up, "Stand up" is.
            if (Table is not null)
            {
                return true;
            }

            if (_host.DeckPlan.NearestConsoleSpot(_host.AvatarX, _host.AvatarY) is not
                { Kind: DeckPlan.ConsoleKind.HiveOfficeChair } spot)
            {
                return false;
            }

            UndergroundComplex.FloorPlan floor =
                UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());

            // ── #818 · …AND THE STOOL AT A WORKTOP TAKES THE SAME VERB ────────────────────────────────────
            //
            // Owner's kit for the rest of the building opens with the word "chairs", and a bench you cannot sit
            // at is a bench in a catalogue. It is the office chair's own seam, the same panel and the same snap
            // (#820), because it is the same posture — what a laboratory adds is the room it is in, and that is
            // a fact the sitting carries rather than a second way to sit down.
            //
            // Asked BEFORE the ring, because a chamber exists on every floor in the game and the park exists on
            // one: a floor with no block would otherwise return before this loop ever ran.
            for (int r = 0; r < floor.TheRooms.Count; r++)
            {
                UndergroundComplex.Room room = floor.TheRooms[r];
                if (room.Kind != UndergroundComplex.RoomKind.Chamber)
                {
                    continue;
                }
                foreach (RingOffice.Chair seat in room.Seats)
                {
                    if (Math.Abs(seat.X - spot.X) >= SameChairDu
                        || Math.Abs(seat.Y - spot.Y) >= SameChairDu)
                    {
                        continue;
                    }

                    SitOnThisStool(ex, in room, r, seat);
                    return true;
                }
            }

            if (floor.Park is not { } green)
            {
                return false;
            }

            foreach (UndergroundComplex.RingRoom room in green.Frontage)
            {
                foreach (RingOffice.Chair chair in room.Seats)
                {
                    if (Math.Abs(chair.X - spot.X) >= SameChairDu
                        || Math.Abs(chair.Y - spot.Y) >= SameChairDu)
                    {
                        continue;
                    }

                    SitInThisChair(ex, room, chair);
                    return true;
                }

                // #821 · …AND THE SEAT INSIDE A CUBICLE TAKES THE SAME VERB. Owner: <i>"Sitting available (it is
                // a toilet; the SIT verb and #820's snap apply)."</i> It is the office chair's own seam, the same
                // panel and the same snap, because it is the same posture — what a cubicle adds is a CATCH, and
                // that is a fact the sitting carries (TableTalk.CubicleKey) rather than a second way to sit down.
                foreach (RingOffice.Stall cell in room.Cubicles)
                {
                    if (Math.Abs(cell.SeatX - spot.X) >= SameChairDu
                        || Math.Abs(cell.SeatY - spot.Y) >= SameChairDu)
                    {
                        continue;
                    }

                    SitInThisCubicle(ex, room, cell);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// #821 · The one place a cubicle sitting is opened.
        ///
        /// <para>Everything about it that is NOT the office chair is one flag and one key: nobody ever comes over
        /// (<c>Office</c>, which is true of a WC for a blunter reason than it is of an empty suite), and WHICH
        /// cell this is, so the exposure ladder can ask whether the catch is over on every frame rather than
        /// once when the captain sat down.</para>
        /// </summary>
        private void SitInThisCubicle(
            SurfaceExcursion ex, in UndergroundComplex.RingRoom room, RingOffice.Stall cell)
        {
            // #820's snap, through the ONE helper every seat verb in the game sits the captain with — the sweep
            // that took the bench, the stool and the canteen chair took this seam with them, and a cubicle's pan
            // arriving after it must not reintroduce a fifth placement.
            _host.SitCaptainOn(cell.SeatX, cell.SeatY);

            Table = new TableTalk
            {
                Key = $"{ex.CanteenWatch}:{ex.Floor}:wc:{room.Number}:{cell.Index}",
                Index = CubicleLock.ApproachOrdinal(room.Number, cell.Index),
                Office = true,
                // …and standing up puts the captain back on the same square. A cubicle's pan is not a solid on
                // the plan and the seat is the very spot you were standing on to press [E], but the pair is
                // published separately all the same (RingOffice.Stall.StandAt) for the ring chair's own reason.
                StepOff = cell.StandAt,
                CubicleKey = HiveInterior.CubicleKey(ex.Floor, in cell),
                Who = CanteenTable.Who.None,
                Plate = CubicleLock.SeatPlate,
                Scene = CubicleLock.TheCubicle(),
                Seats = 1,
                Free = 0,
                // NOT the cabinet's flag. A cabinet's door was PAID for; this one is a catch on a partition, and
                // the difference is the whole of #821 — an unlocked cubicle must not be dealt the top rung of the
                // exposure ladder, and CubicleKey is what earns it once the catch is actually over.
                Quiet = false,
                Solo = true,
                Relaxed = false,
                DrinkInHand = _host.APourInFrontOfYou,
                Joined = true,
                Outcome = CubicleLock.SatDownLine,
            };

            RendererInterop.PlayCue("reveal");
            _host.StateHasChanged();
        }

        /// <summary>
        /// #818 · The one place a chamber sitting is opened — a stool at a laboratory bench, a desk in an
        /// administration room, an examination bench in a back room.
        ///
        /// <para>Everything about it that is not the ring chair is the ROOM: the key, the approach ordinal and
        /// the scene's opening line all name a poured box off a corridor rather than a suite on a garden, and
        /// #783's own reason applies — the plate beside the door is the only thing that tells one of these from
        /// another, so a captain who has walked in off a rib deserves to be told which one they are sitting
        /// in.</para>
        /// </summary>
        private void SitOnThisStool(
            SurfaceExcursion ex, in UndergroundComplex.Room room, int roomIndex, RingOffice.Chair seat)
        {
            // #820's snap, through the ONE helper every seat verb in this game sits the captain with.
            _host.SitCaptainOn(seat.X, seat.Y);

            Table = new TableTalk
            {
                // Its own prefix, so a stool, a chair, a bench and a canteen top with the same ordinal on the
                // same floor can never share a wait counter — one key for four seats would be one source
                // consumed as if it were four.
                Key = $"{ex.CanteenWatch}:{ex.Floor}:bench:{roomIndex}:{seat.Index}",
                Index = ChamberFitting.ApproachOrdinal(roomIndex, seat.Index),
                Office = true,
                StepOff = seat.StandAt,
                Who = CanteenTable.Who.None,
                Plate = ChamberFitting.SeatPlate,
                Scene = ChamberFitting.TheStool(room.Plate),
                Seats = Math.Max(1, room.Seats.Count),
                Free = Math.Max(0, room.Seats.Count - 1),
                // A room with a door and nobody in it — the ring chair's own flag, and true here for the same
                // reason: nothing at a counter can see you.
                Quiet = true,
                Solo = true,
                Relaxed = false,
                DrinkInHand = _host.APourInFrontOfYou,
                Joined = true,
                Outcome = ChamberFitting.TookAStoolLine(room.Plate),
            };

            RendererInterop.PlayCue("reveal");
            _host.StateHasChanged();
        }

        /// <summary>How close a console has to be to a published seat to BE that seat. A rounding tolerance and
        /// not a search radius: the console was hung on the chair's own coordinate by
        /// <c>HiveInterior</c>, and the only thing between them is a float cast.</summary>
        public const double SameChairDu = 0.5;

        /// <summary>The one place an office-chair sitting is opened, so a dev row and a captain's [E] can never
        /// open two different chairs.</summary>
        private void SitInThisChair(
            SurfaceExcursion ex, in UndergroundComplex.RingRoom room, RingOffice.Chair chair)
        {
            // #820 · THE SNAP. The body goes into the chair, at Core's own published seat, through the one
            // helper every seat verb in the game now sits the captain with. It was StandCaptainAt when this
            // shipped, which was right for a ring chair and only for a ring chair: that nudge walks a body out
            // of collision, and a park bench is solid on purpose — one law for four seats had to be the law that
            // works on the solid one. Nothing is lost here, because a suite's chairs are proved to be real floor
            // by Core's own guard (NoRingSuiteIsAnEmptyFloorTests) rather than by a rescue at run time.
            _host.SitCaptainOn(chair.X, chair.Y);

            Table = new TableTalk
            {
                Key = OfficeChairKey(ex, room.Number, chair.Index),
                // The APPROACH ordinal, and deliberately not the chair's own — Core owns the offset
                // (RingOffice.ApproachOrdinal) for the reason the bench's does: two seats dealt the same
                // ordinal are two seats dealt the same answer on the same shift.
                Index = RingOffice.ApproachOrdinal(room.Number, chair.Index),
                Office = true,
                // …and standing up puts the captain back on the same square, because a ring chair is not a solid
                // and the seat is the very spot you were standing on to press [E]. Core publishes the pair
                // separately all the same (RingOffice.Chair.StandAt), so the day a suite's chairs are tucked
                // under their desks this reads the new square rather than the old assumption.
                StepOff = chair.StandAt,
                Who = CanteenTable.Who.None,
                Plate = RingOffice.SeatPlate,
                // #868 · …and the scene ASKS THE ROOM IT IS IN, and then the SEAT. The garden clause is
                // reachable only where the room is dressed as a suite that faces the green
                // (RingOffice.LooksAtTheGarden) — the owner sat down in a cold room on the back street and was
                // told the whole wall in front of him was the garden — and the worktop clause only where a
                // worktop is actually within reach of this chair (RingOffice.WorksAtASurface), which the bench
                // outside the block's cubicles and the seat inside a privacy booth are not. Core owns both
                // questions; nothing here decides what a room looks at or what is standing in front of a seat.
                Scene = RingOffice.TheChair(
                    room.Plate, RingOffice.LooksAtTheGarden(in room),
                    RingOffice.WorksAtASurface(in room, in chair)),
                Seats = Math.Max(1, room.Seats.Count),
                Free = Math.Max(0, room.Seats.Count - 1),
                // A room with a door and nobody in it. Quiet is the cabinet's own flag and it is the true one
                // here for the same reason: nothing at a counter can see you, and the exposure ladder should
                // say so rather than treating a private office as a table in a bar.
                Quiet = true,
                Solo = true,
                // #783's relaxed register belongs to a bought glass in a canteen. There is no bar in an office
                // and no art of one — borrowing the picture would be showing a room the captain is not in.
                Relaxed = false,
                DrinkInHand = _host.APourInFrontOfYou,
                // Nobody to ask. The chair is simply taken, and the taking is the scene's opening line.
                Joined = true,
                Outcome = RingOffice.TookAChairLine(
                    room.Plate, RingOffice.LooksAtTheGarden(in room),
                    RingOffice.WorksAtASurface(in room, in chair)),
            };

            RendererInterop.PlayCue("reveal");
            _host.StateHasChanged();
        }
    }
}
