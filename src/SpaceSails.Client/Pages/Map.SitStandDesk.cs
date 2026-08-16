using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #869 · THE DESK STILL RISES — the press at the edge, and the one at the memory buttons.
///
/// <para>Owner, 2026-08-13, from his own 160 x 90 cm electric desk: <i>"it got up- and down buttons to move
/// the table to work either with office chair, Salli standing (lab) chair or by standing while using the
/// table. Surely in futuristic lab they would have similar spec tables to ensure good ergonomics :-D"</i> —
/// and, blessing the second half: <i>"love the gag of messing with somebodys desks memorized height options
/// :-D"</i>.</para>
///
/// <h3>The grip: two controls, one key, and which is which</h3>
///
/// <para>Core hangs two consoles on every sit-stand desk, at the two ends of the desk's own front face
/// (<see cref="SitStandDesk.TheEdge"/> / <see cref="SitStandDesk.TheButtons"/>), and [E] does one thing at
/// each:</para>
///
/// <list type="bullet">
/// <item><b>The desk edge</b> — the UP/DOWN paddle. Press it and the desk rises; press it again and it goes
/// back down. Strictly alternating, per desk, and the state is watch-scoped exactly as a seat's is.</item>
/// <item><b>The memory buttons</b> — the presets. The FIRST press READS them (the card: what this desk
/// remembers about the person who is not here); every press after that LEANS on them, which is the prank.
/// Look, and then meddle: the order is the gumshoe's, and it is the one thing about this feature that is a
/// judgement call rather than a ruling — see the PR body.</item>
/// </list>
///
/// <para><b>The press at a desk is not a press at a chair.</b> The chair is the sit (#817/#818's
/// <c>HiveOfficeChair</c>); this is a press you make standing up, at the furniture rather than at the seat,
/// and it is why the two controls take the ENDS of the face — a control on the same square as the seat is a
/// coin toss about which console answers.</para>
///
/// <h3>What it costs, and what it pays</h3>
///
/// <para>Nothing, in both directions, and deliberately. Nothing files, nothing scores, no beat is spent, no
/// watch turns, and nobody in-world ever remarks that the desks are nice or that somebody has been at the
/// presets. It is the fans-still-turning beat, furniture-sized — and for the prank, the pettiest verb in the
/// game, which costs and pays nothing because that IS the joke.</para>
/// </summary>
public partial class Map
{
    /// <summary>#869 · Which desks are currently UP. Keyed per floor and per desk, so a raised desk on B2 is
    /// not a raised desk on B3 and two desks in one room are two desks. Run-scoped, like the poster read
    /// count next door: the motor is not a fact anybody records.</summary>
    private readonly HashSet<string> _desksRaised = [];

    /// <summary>#869 · …and which sets of memory buttons have been READ. What turns the next press from the
    /// gumshoe's look into the prank — see the class summary on the order.</summary>
    private readonly HashSet<string> _presetsRead = [];

    /// <summary>How close a console has to be to a published control to BE that control. The rounding
    /// tolerance <c>SameChairDu</c> is, and for the same reason: the console was hung on Core's own
    /// coordinate and the only thing between them is a float cast.</summary>
    private const double SameControlDu = Seating.SameChairDu;

    /// <summary>One desk's key. The floor and the room and the fitting — never the coordinate, because a
    /// key made of a position is a key that changes when somebody moves a wall.</summary>
    private static string DeskKey(SurfaceExcursion ex, int roomIndex, int fittingIndex) =>
        $"{ex.Floor}:desk:{roomIndex}:{fittingIndex}";

    /// <summary>
    /// #869 · [E] AT THE DESK'S EDGE — the motor runs, and says the one line it has to say.
    ///
    /// <para>The line comes off <see cref="SitStandDesk.LineFor"/> and is never typed here, so the copy the
    /// owner authored has exactly one home.</para>
    /// </summary>
    private void PressTheDeskEdge()
    {
        if (WhichDesk(DeckPlan.ConsoleKind.HiveDeskEdge) is not { } at)
        {
            return;
        }

        bool raised = !_desksRaised.Contains(at.Key);
        if (raised)
        {
            _desksRaised.Add(at.Key);
        }
        else
        {
            _desksRaised.Remove(at.Key);
        }

        ShowPulseMessage(SitStandDesk.LineFor(raised));
    }

    /// <summary>
    /// #869 · [E] AT THE MEMORY BUTTONS — read them once, and lean on them after that.
    ///
    /// <para>The READ is a card, because it is evidence and evidence is something you stop and look at: the
    /// <c>ViewObject</c> pop-up the posters and the monolith already use, with no picture, which the card
    /// renders caption-only exactly as the lifeboat muster does. The PRANK is a pulse, because it is
    /// something you DO in passing and then walk away from — and it files nothing, scores nothing and is
    /// never mentioned again by anything.</para>
    /// </summary>
    private void PressTheMemoryButtons()
    {
        if (WhichDesk(DeckPlan.ConsoleKind.HiveDeskPresets) is not { } at)
        {
            return;
        }

        // A card is already up: [E] closes it, through the one door every road out of a card comes through
        // (#768's held lines are freed there). Otherwise a second press would raise the prank behind a
        // backdrop nobody can see past.
        if (_viewObject is not null)
        {
            CloseViewObject();
            return;
        }

        if (_presetsRead.Add(at.Key))
        {
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, at.Spot.X, at.Spot.Y,
                SitStandDesk.ButtonsPlate, null, SitStandDesk.ReadLine);
            RendererInterop.PlayCue("reveal");
            StateHasChanged();
            return;
        }

        ShowPulseMessage(SitStandDesk.PrankLine);
    }

    /// <summary>Which desk the captain is standing at, or null. The console the press landed on, matched
    /// back to Core's own published fitting — a lookup rather than a decision, and never a coordinate this
    /// file measured for itself (§13.15).</summary>
    private (string Key, DeckPlan.ConsoleSpot Spot)? WhichDesk(DeckPlan.ConsoleKind kind)
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return null;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { } spot || spot.Kind != kind)
        {
            return null;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());

        for (int r = 0; r < floor.TheRooms.Count; r++)
        {
            UndergroundComplex.Room room = floor.TheRooms[r];
            if (room.Kind != UndergroundComplex.RoomKind.Chamber)
            {
                continue;
            }

            IReadOnlyList<RingOffice.Fixture> fittings = room.Furniture;
            for (int f = 0; f < fittings.Count; f++)
            {
                RingOffice.Fixture desk = fittings[f];
                if (!SitStandDesk.HasAMotor(desk.Kind))
                {
                    continue;
                }
                foreach (RingOffice.Chair seat in room.Seats)
                {
                    if (SitStandDesk.DeskFor(fittings, in seat) != f)
                    {
                        continue;
                    }

                    (double cx, double cy) = kind == DeckPlan.ConsoleKind.HiveDeskEdge
                        ? SitStandDesk.TheEdge(in desk, in seat)
                        : SitStandDesk.TheButtons(in desk, in seat);
                    if (Math.Abs(cx - spot.X) < SameControlDu && Math.Abs(cy - spot.Y) < SameControlDu)
                    {
                        return (DeskKey(ex, r, f), spot);
                    }
                    break;
                }
            }
        }

        return null;
    }
}
