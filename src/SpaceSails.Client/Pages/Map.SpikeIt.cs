using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpaceSails.Client.Pages.Stations;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1202 slice 2 · <b>SPIKE IT — the page's half.</b> Core owns every word, the purse, the table, her cadence,
/// the two papers, the two moves and the window's one decision (<see cref="SpikeIt"/>); this file is the moments
/// Core cannot reach: the row at the dark-web desk, her at her table in Selene Gate's gallery, the two moves on
/// that table's card, the desk's pulse after the window, and the dev start.
///
/// <h3>No field on this page</h3>
///
/// <para>Everything the contract remembers rides slice 1's one line in the quest's free slot
/// (<see cref="CarryThePress.Passage"/>, which the vault already writes as <c>QuestRecord.Fields["pin"]</c>); her
/// body at the table is one more <see cref="Walker"/> in the docked room's existing band, and her own clock is
/// that walker's own <see cref="Walker.PassHeld"/> (a berth's sim clock does not run while the captain walks it);
/// the dev start is read off the address bar. With no contract taken, nothing here writes, draws or changes a
/// card.</para>
///
/// <h3>The game never confirms</h3>
///
/// <para>§13.8. Only the wire and the ledger change: a story prints or does not, the book files one line, and the
/// desk pays or notes. No heat, no goodwill, no line anywhere that says which version was true.</para>
/// </summary>
public sealed partial class Map
{
    // ── THE ROW AT THE DESK ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Her contract whose story is still pending and has no spike against it — the one the row is
    /// about — or null.</summary>
    private Quest? TheStoryStillToRun()
    {
        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.CarryThePress, State: QuestState.TurnedIn, DestBodyId: not null }
                && PassageOf(q) is { Spike: false } p
                && CarryThePress.StoryAt(p) is { } at && SimTime < at)
            {
                return q;
            }
        }

        return null;
    }

    /// <summary>The contract with a spike against it that the window has not decided yet, or null.</summary>
    private Quest? TheSpikeInHand()
    {
        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.CarryThePress, State: QuestState.TurnedIn, DestBodyId: not null }
                && PassageOf(q) is { Spike: true, Outcome: SpikeIt.Outcome.None })
            {
                return q;
            }
        }

        return null;
    }

    /// <summary>
    /// #1202 slice 2 · <b>SPIKE IT</b>, as the desk draws it — null, and no row, unless a story of hers is
    /// pending, no spike is already in hand, the desk trades, and the pocket has room for the client's page.
    /// Absent otherwise, never greyed.
    /// </summary>
    private DarkWeb.SpikeOffer? SpikeOnOffer()
    {
        if (!DarkWebCanTrade() || TheSpikeInHand() is not null
            || TheStoryStillToRun() is not { DestBodyId: { } bodyId } q)
        {
            return null;
        }

        string body = BodyName(bodyId);
        return Core.Satchel.CanTake(_satchel, SpikeIt.TheSwap(body))
            ? new DarkWeb.SpikeOffer(SpikeIt.Row(body, SpikeIt.Purse(q.Reward)))
            : null;
    }

    /// <summary>#1202 slice 2 · Taking it: the client's page lands in the satchel, the contract remembers, and the
    /// terms are said once.</summary>
    private void TakeTheSpike()
    {
        if (SpikeOnOffer() is null || TheStoryStillToRun() is not { DestBodyId: { } bodyId } q)
        {
            return;
        }

        string body = BodyName(bodyId);
        _satchel = [.. Core.Satchel.Add(_satchel, SpikeIt.TheSwap(body))];
        RewritePassage(q, PassageOf(q) with { Spike = true });
        ShowPulseMessage(SpikeIt.Taken(body));
        StateHasChanged();
    }

    // ── HER, AT HER TABLE ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The walker that is her at the gallery, if she is on this floor.</summary>
    private Walker? TheStringerAtHerPages()
    {
        foreach (Walker w in _barAfoot)
        {
            if (w.For == Errand.AtHerPages)
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>Is she away from her table? Walking to the machine, standing at it, or walking back. A body that
    /// is not on the floor at all (a full band) is not away: nothing is ever offered over a table nobody left.</summary>
    private static bool SheIsAway(Walker? her) =>
        her is not null && !(her.Table == SpikeIt.HerTable && her.Walk.State == NpcWalk.Doing.Arrived);

    /// <summary>Her chair: the first side of her table the stone allows once the side a captain sits on is set
    /// aside, so the two of them are never on one square.</summary>
    private static DeckReachability.Point? HerChair(DeckReachability.Point top, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        BesideThisTop(top, walls) is { } his
            ? HavenInterior.BesideATop(top, BesideATopDu / 2.0, walls, his)
            : null;

    /// <summary>
    /// #1202 slice 2 · One frame of her at the gallery, called from the docked room's concourse frame. She is on
    /// the floor only at Selene Gate, only while a spike is in hand and its window has not come: at her table
    /// writing, and on her own clock up to the machine behind it and back. The pages stay on the table; nothing
    /// announces the gap. Her line is said once, the first time the captain is in the gallery while she writes.
    /// </summary>
    private void AdvanceTheStringerAtHerPages(in HavenInterior.BarFloor bar)
    {
        Walker? her = TheStringerAtHerPages();
        if (!string.Equals(bar.BodyId, SpikeIt.Haven, StringComparison.Ordinal)
            || TheSpikeInHand() is not { } q || CarryThePress.StoryIsDue(PassageOf(q), SimTime))
        {
            if (her is not null)
            {
                _barAfoot.Remove(her);
                StateHasChanged();
            }

            TheRecorderIsLeftOnTheTable(bar.BodyId);
            return;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        IReadOnlyList<DeckReachability.Point> tops = HavenInterior.GalleryTops(bar.BodyId);
        IReadOnlyList<DeckReachability.Point> machines = HavenInterior.TheVendorsAt(bar.BodyId);
        if (tops.Count <= SpikeIt.HerTable || machines.Count <= SpikeIt.HerTable
            || HerChair(tops[SpikeIt.HerTable], walls) is not { } chair)
        {
            return;
        }

        if (her is null)
        {
            if (_barAfoot.Count < WalkerBand
                && OnFoot(CarryThePress.Plate, new NpcWalk.Bound("", chair.X, chair.Y), chair, walls,
                    NpcWalk.NoPersonalSpace) is { } sitting)
            {
                _barAfoot.Add(new Walker { Walk = sitting, Table = SpikeIt.HerTable, For = Errand.AtHerPages });
                StateHasChanged();
            }

            return;
        }

        CarryThePress.Passage p = PassageOf(q);
        if (!p.Seen && !SheIsAway(her)
            && HavenInterior.InTheGallery(bar.BodyId, _avatarX, _avatarY, _havenFloor))
        {
            SayItWhereTheyAreLooking(SpikeIt.AtHerTableLine);
            RewritePassage(q, p with { Seen = true });
        }

        if (her.Walk.State != NpcWalk.Doing.Arrived)
        {
            return;
        }

        bool atHerTable = her.Table == SpikeIt.HerTable;
        if (her.PassHeld < (atHerTable ? SpikeIt.WritesFor(q.Id) : SpikeIt.FeedsFor(q.Id)))
        {
            return;
        }

        DeckReachability.Point to = atHerTable ? machines[SpikeIt.HerTable] : chair;
        if (OnFoot(CarryThePress.Plate, new NpcWalk.Bound("", to.X, to.Y),
                new DeckReachability.Point(her.Walk.X, her.Walk.Y), walls, NpcWalk.NoPersonalSpace) is { } leg)
        {
            int at = _barAfoot.IndexOf(her);
            _barAfoot[at] = new Walker { Walk = leg, Table = atHerTable ? -1 : SpikeIt.HerTable, For = Errand.AtHerPages };
            StateHasChanged();
        }
        else
        {
            her.PassHeld = 0;   // the stone refused the leg; she writes (or stands) one more spell and tries again.
        }
    }

    /// <summary>One frame of her feet, from the room's own stepper. The route running out is not an ending for
    /// her: she stays where she got to, and her own clock runs while she stands there. A leg the ground refused
    /// takes her off the floor; the next frame puts her back at her table.</summary>
    /// <returns>Whether anything the room draws changed.</returns>
    private bool StepTheStringerAtHerPages(Walker w, double dt, IReadOnlyList<SurfaceCollision.Segment> walls, int index)
    {
        if (w.Walk.State == NpcWalk.Doing.Arrived)
        {
            w.PassHeld += dt;
            return false;
        }

        w.Walk.Step(dt, walls, _avatarX, _avatarY);
        if (w.Walk.Afoot)
        {
            return false;
        }

        if (w.Walk.State != NpcWalk.Doing.Arrived)
        {
            _barAfoot.RemoveAt(index);
        }

        return true;
    }

    /// <summary>
    /// #1202 slice 2 · After a spiked window, the first time the captain is in the gallery: she is not at the
    /// table, and the recorder is. Said once; she is simply not drawn again.
    /// </summary>
    private void TheRecorderIsLeftOnTheTable(string berth)
    {
        if (!string.Equals(berth, SpikeIt.Haven, StringComparison.Ordinal)
            || !HavenInterior.InTheGallery(berth, _avatarX, _avatarY, _havenFloor))
        {
            return;
        }

        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.CarryThePress, State: QuestState.TurnedIn }
                && PassageOf(q) is { Spike: true, Outcome: SpikeIt.Outcome.Spiked, Gone: false } p)
            {
                SayItWhereTheyAreLooking(SpikeIt.GoneLine);
                RewritePassage(q, p with { Gone = true });
                return;
            }
        }
    }

    // ── THE TWO MOVES ON HER TABLE'S CARD ───────────────────────────────────────────────────────────────

    /// <summary>Which of the two moves is on offer to the captain at this sitting, or null — asked of the seat's
    /// own key and ordinal, never of a position.</summary>
    private string? ThePagesMoveHere(TableTalk t)
    {
        if (!AtTheGallerysTable(t) || t.Index != SpikeIt.HerTable || TheSpikeInHand() is not { } q)
        {
            return null;
        }

        CarryThePress.Passage p = PassageOf(q);
        if (CarryThePress.StoryIsDue(p, SimTime))
        {
            return null;
        }

        bool holdsTheSwap = _satchel.Any(i => i.Kind == Core.Satchel.Kind.Paper && SpikeIt.IsTheSwap(i.Id));
        bool holdsThePages = _satchel.Any(i => i.Kind == Core.Satchel.Kind.Paper && SpikeIt.IsThePages(i.Id));
        string? move = SpikeIt.MoveOnOffer(p.Pages, SheIsAway(TheStringerAtHerPages()), t.Solo && !t.SharedSeat,
            holdsTheSwap, holdsThePages);

        // A pocket too full to take her pages is a state the canon wrote no line for: the move is simply absent.
        return move == SpikeIt.TakeThePages && !Core.Satchel.CanTake(_satchel, SpikeIt.ThePages()) ? null : move;
    }

    /// <summary>
    /// #1202 slice 2 · Each frame while the captain is at a gallery table: her table's card carries TAKE THE
    /// PAGES or LEAVE YOUR PAGE exactly while it should, and is the plain table's card otherwise. With no spike
    /// in hand it never touches the card at all.
    /// </summary>
    private void KeepThePagesHonest()
    {
        if (_seating.Table is not { } t || !AtTheGallerysTable(t) || t.Scene.Id != SittingAlone.TheTable().Id)
        {
            return;
        }

        string? move = ThePagesMoveHere(t);
        if (SpikeIt.Offers(t.Scene) != move)
        {
            t.Scene = SpikeIt.TheTable(t.Scene, move);
            StateHasChanged();
        }
    }

    /// <summary>
    /// #1202 slice 2 · <b>TAKE THE PAGES</b> and <b>LEAVE YOUR PAGE</b>, taken ahead of the seat's own dispatch
    /// because they move things the seat does not own: the papers in the satchel, the contract's line, the book.
    /// The line is said on the card, where the captain is looking. True when the press was one of the two,
    /// whatever came of it.
    /// </summary>
    private bool ThePagesAreTouched(string moveId)
    {
        if (moveId is not (SpikeIt.TakeThePages or SpikeIt.LeaveYourPage))
        {
            return false;
        }

        if (_seating.Table is not { } t || !AtTheGallerysTable(t))
        {
            return true;
        }

        if (ThePagesMoveHere(t) != moveId || TheSpikeInHand() is not { } q)
        {
            // She came back while the hand was on its way, or the pocket changed: the move goes, and nothing is
            // said. A move that is not there is not a sentence.
            t.Scene = SpikeIt.TheTable(t.Scene, ThePagesMoveHere(t));
            StateHasChanged();
            return true;
        }

        CarryThePress.Passage p = PassageOf(q);
        if (moveId == SpikeIt.TakeThePages)
        {
            _satchel = [.. Core.Satchel.Add(_satchel, SpikeIt.ThePages())];
            RewritePassage(q, p with { Pages = SpikeIt.Pages.Taken });
            FileNote(SpikeIt.TookThePages(SpikeIt.WatchesUntil(CarryThePress.StoryAt(p)!.Value, SimTime)),
                CarryThePress.Glyph);
            t.Outcome = SpikeIt.TakeThePagesLine;
        }
        else
        {
            Core.Satchel.Item swap = _satchel.First(i => i.Kind == Core.Satchel.Kind.Paper && SpikeIt.IsTheSwap(i.Id));
            _satchel = [.. Core.Satchel.Remove(_satchel, Core.Satchel.Kind.Paper, swap.Id)];
            _satchel = [.. Core.Satchel.Remove(_satchel, Core.Satchel.Kind.Paper, SpikeIt.PagesId)];
            RewritePassage(q, p with { Pages = SpikeIt.Pages.Swapped });
            t.Outcome = SpikeIt.LeaveYourPageLine;
        }

        t.Scene = SpikeIt.TheTable(t.Scene, ThePagesMoveHere(t));
        StateHasChanged();
        return true;
    }

    // ── THE WINDOW ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1202 slice 2 · What the window made of a story with a spike against it, decided ONCE (the first time the
    /// story is due) off where her pages are, and remembered on the contract. A story with no spike against it
    /// runs as slice 1 wrote it, which is what <see cref="SpikeIt.Outcome.Late"/> means too.
    /// </summary>
    private static SpikeIt.Outcome TheWindowDecides(ref CarryThePress.Passage next)
    {
        if (!next.Spike)
        {
            return SpikeIt.Outcome.Late;
        }

        if (next.Outcome == SpikeIt.Outcome.None)
        {
            next = next with { Outcome = SpikeIt.AtTheWindow(next.Pages) };
        }

        return next.Outcome;
    }

    /// <summary>
    /// #1202 slice 2 · <b>AND THE DESK, AFTER THE WINDOW.</b> Called where the dark-web desk is opened, beside the
    /// parcel's payment and the geocache's escrow: the next desk pulse after a window pays the full purse on 💳
    /// with no words when nothing ran, half when her name ran over the client's sentence, and nothing at all —
    /// one line where the money would be — when her story ran as she wrote it. Once per contract.
    /// </summary>
    private void TheSpikeIsSettled()
    {
        ThePressRunsHerStory();
        foreach (Quest q in _quests)
        {
            if (q is not { Kind: QuestKind.CarryThePress, State: QuestState.TurnedIn }
                || PassageOf(q) is not { Spike: true, Paid: false } p || p.Outcome == SpikeIt.Outcome.None)
            {
                continue;
            }

            int pays = SpikeIt.Pays(p.Outcome, SpikeIt.Purse(q.Reward));
            _credits += pays;
            ShowPulseMessage(p.Outcome == SpikeIt.Outcome.Late
                ? $"💳 {SpikeIt.LateLine}"
                : $"💳 +{pays.ToString("N0", CultureInfo.InvariantCulture)} cr");
            RewritePassage(q, p with { Paid = true });
            StateHasChanged();
            return;
        }
    }

    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1202 slice 2 QA · <c>?dock=selene-gate&amp;ashore=1&amp;spike=1</c> — her story pending (she paid a sim-day
    /// ago), the spike taken, the client's page in the satchel, the captain stood in the gallery; she is at her
    /// table from the first frame. <c>…&amp;spike=spiked</c> — the same contract four sim-days on, her pages in the
    /// satchel: the window has passed spiked, the book has its absence mark, the gallery says the recorder is
    /// left, and the desk's next pulse pays. It plants what play would have left and nothing more; everything
    /// after it is the shipped path. Called right after <c>?ashore=1</c> has walked the captain into the hall.
    /// </summary>
    private void SpikeItIfAsked()
    {
        SpikeIt.Cheat cheat = Navigation is { } address ? SpikeIt.CheatIn(address.Uri) : SpikeIt.Cheat.None;
        if (cheat == SpikeIt.Cheat.None)
        {
            return;
        }

        if (_ephemeris is null || _dockedHavenId is not { } here
            || PressContractAt(here, PatronRota.WatchIndex(SimTime)) is not { DestBodyId: { } bodyId } offer)
        {
            ShowPulseMessage("🧪 DEV ?spike= — no berth or no ground for her story here; try &dock=selene-gate&ashore=1");
            return;
        }

        string body = BodyName(bodyId);
        _quests.Add(offer);
        _satchel = [.. Core.Satchel.Add(_satchel, SpikeIt.TheSwap(body))];
        double ago = cheat == SpikeIt.Cheat.Spiked
            ? CarryThePress.StoryAfterSeconds + CarryThePress.FloorAfterStorySeconds
            : CarryThePress.FloorAfterStorySeconds;
        Quest home = RewritePassage(offer, PassageOf(offer) with
        {
            Landed = true, Walked = true, Tin = true, TurnedIn = SimTime - ago, Spike = true,
            Pages = cheat == SpikeIt.Cheat.Spiked ? SpikeIt.Pages.Taken : SpikeIt.Pages.OnHerTable,
        });
        AdvanceMission(home, QuestState.TurnedIn);
        if (cheat == SpikeIt.Cheat.Spiked)
        {
            _satchel = [.. Core.Satchel.Add(_satchel, SpikeIt.ThePages())];
            ThePressRunsHerStory();
        }

        IReadOnlyList<DeckReachability.Point> vendors = HavenInterior.TheVendorsAt(here);
        if (OnTheConcourse && vendors.Count > 0)
        {
            DeckReachability.Point island = vendors[^1];
            StandCaptainAt(island.X, island.Y, "you come out of the tube into the gallery");
        }

        ShowPulseMessage(cheat == SpikeIt.Cheat.Spiked
            ? $"🧪 DEV ?spike=spiked — the {body} story did not run; the recorder is on the far table; open Comms → dark web for the 💳"
            : $"🧪 DEV ?spike=1 — the {body} story is {SpikeIt.WatchesUntil(CarryThePress.StoryAt(PassageOf(home))!.Value, SimTime)} watches off; "
              + $"{CarryThePress.Plate} writes at gallery table {SpikeIt.HerTable} (the far one); sit there and wait for her to feed the machine");
    }
}
