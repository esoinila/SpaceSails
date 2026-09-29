using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1202 slice 1 · <b>CARRY THE PRESS — the page's half.</b> Core owns every word, the ground, the tin's spot
/// and the three clocks (<see cref="CarryThePress"/>); this file is the moments Core cannot reach: the card at
/// the table, her walk behind the captain on her ground, the shovel that finds the tin, the liftoff, the
/// turn-in at a berth, and the wire three days later.
///
/// <h3>No field on this page</h3>
///
/// <para>Everything the contract remembers rides the quest's own free slot as one line
/// (<see cref="CarryThePress.Passage"/>), which the vault already writes as <c>QuestRecord.Fields["pin"]</c>;
/// her body on the ground is one more <see cref="Walker"/> in the excursion's existing band; the dev start is
/// read off the address bar. #905's frame ledger walks every instance field of this page, so a new one would
/// move it for a feature that is not on record — and with no contract on record, nothing here writes, draws or
/// offers anything.</para>
///
/// <h3>Nothing in the world changes because the story ran</h3>
///
/// <para>§13.8: the press may speculate, the game never confirms. The story is a line on the wire and a line in
/// the book. No heat, no contact, no flag anybody else reads.</para>
/// </summary>
public sealed partial class Map
{
    // ── THE CARD AT THE TABLE ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1202 · Her card, when she is at her own chair in this bar this watch (<c>HavenInterior.TheStringersSeat</c>,
    /// on the same one-watch-in-three cadence) — null, and no card, otherwise. One contract at a time: never
    /// while one of hers is still in the captain's hand.
    /// </summary>
    private Quest? MakePressOffer()
    {
        if (_ephemeris is null || _dockedHavenId is not { } here
            || _quests.Any(q => q.Kind == QuestKind.CarryThePress && q.State != QuestState.TurnedIn))
        {
            return null;
        }

        long watch = PatronRota.WatchIndex(SimTime);
        return CarryThePress.AtTheTable(here, watch) ? PressContractAt(here, watch) : null;
    }

    /// <summary>
    /// The contract she would book at this berth on this watch. The ground is seeded off the booth's contract
    /// (the berth and the watch it is offered on), so walking up to her twice in one watch is the same card;
    /// the pool is the scenario's own landable ground in this berth's own system when it has any (the
    /// neighbourhood law), else all of it.
    /// </summary>
    private Quest? PressContractAt(string here, long watch)
    {
        string system = SystemIdOf(here);
        List<string> near = TheLandableGround().Where(b => SystemIdOf(b) == system).ToList();
        IReadOnlyList<string> pool = near.Count > 0 ? near : TheLandableGround();
        if (CarryThePress.For($"{here}#{watch}", pool) is not { } ground)
        {
            return null;
        }

        int reward = HaulReward.ForHaul(HelioRadiusMeters(here), HelioRadiusMeters(ground.BodyId));
        string site = Core.FieldNotes.PlaceLabel(BodyName(ground.BodyId), ground.SiteName);
        return new Quest($"press-{++_questSeq}", QuestKind.CarryThePress, CarryThePress.Giver,
            "", site, CarryThePress.CardTitle, CarryThePress.Offer(site), reward,
            DestBodyId: ground.BodyId, SourceBodyId: here, Pin: new CarryThePress.Passage(ground.SiteIndex).Write());
    }

    /// <summary>#1202 · Taken: she stows one bag and the recorder. Said once, at the press that took her — and
    /// her chair at the bar is empty from that press on (<see cref="TheStringerIsElsewhere"/>), so the room is
    /// re-welded without her console.</summary>
    private void SheStowsOneBag()
    {
        SayItWhereTheyAreLooking(CarryThePress.TakenLine);
        RebuildDockedDeck();
    }

    /// <summary>
    /// #1202 · IS SHE SOMEWHERE OTHER THAN HER OWN CHAIR? Aboard, while a contract of hers is in the captain's
    /// hand (taken and not yet paid off); or at her pages at the gallery's far table, on the berth and for the
    /// span <c>AdvanceTheStringerAtHerPages</c> draws her there. Either way her bar seat is empty.
    /// </summary>
    private bool TheStringerIsElsewhere()
    {
        foreach (Quest q in _quests)
        {
            if (q.Kind == QuestKind.CarryThePress && q.State != QuestState.TurnedIn)
            {
                return true;
            }
        }

        return string.Equals(_dockedHavenId, SpikeIt.Haven, StringComparison.Ordinal)
            && TheSpikeInHand() is { } spiked && !CarryThePress.StoryIsDue(PassageOf(spiked), SimTime);
    }

    // ── THE CONTRACT'S LINE OF STATE ────────────────────────────────────────────────────────────────────

    private static CarryThePress.Passage PassageOf(Quest q) => CarryThePress.Passage.Read(q.Pin);

    /// <summary>Write a contract's state back. The quest is a record, so the new line goes in as a copy in the
    /// same slot — state and all — and the copy is returned for the caller to go on with.</summary>
    private Quest RewritePassage(Quest q, CarryThePress.Passage p)
    {
        int i = _quests.FindIndex(x => ReferenceEquals(x, q));
        Quest next = q with { Pin = p.Write() };
        if (i >= 0)
        {
            _quests[i] = next;
        }

        RequestVaultSave();
        return next;
    }

    /// <summary>Her contract, if the captain is standing on her ground with her still aboard — the one place
    /// she is ever drawn.</summary>
    private Quest? TheStringerOnThisGround(SurfaceExcursion ex)
    {
        if (!HardcaseRep.GroundLikeThis(landed: true, OnWreck, ex.Floor))
        {
            return null;
        }

        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.CarryThePress, State: QuestState.Active }
                && q.DestBodyId == ex.Stop.Body.Id && PassageOf(q).Site == ex.Site.Index)
            {
                return q;
            }
        }

        return null;
    }

    // ── THE TIN ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Her word for where the tin is — the game's own words for a buried thing's spot.</summary>
    private static string TheTinsWords(Quest q, SurfaceExcursion ex) =>
        CarryThePress.TheTin(q.Id, ex.Stop.Body.Id, ex.Site.Index).BearingLine;

    /// <summary>The square the tin lies under on this ground: the first of Core's seeded squares the stone
    /// allows a body to stand on. Null when the ground allows none.</summary>
    private (int X, int Y)? TheTinsSquare(Quest q, SurfaceExcursion ex)
    {
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        foreach ((int X, int Y) square in CarryThePress.TinSquares(q.Id, ex.Stop.Body.Id, MoonSurface.ExpeditionField()))
        {
            (double x, double y) = BeachComber.SquareCenter(square.X, square.Y);
            if (!SurfaceCollision.Blocked(x, y, DeckPlan.AvatarRadius, walls))
            {
                return square;
            }
        }

        return null;
    }

    /// <summary>
    /// #1202 · THE SHOVEL AT THE SPOT. Asked by the probe the moment its channel completes — the existing
    /// shovel, the existing 2D6, the existing bar; nothing announces the tin beyond her one instruction. True
    /// when this hole was the tin's: the tin goes into the sleeve, the contract remembers it, and the dig's
    /// line is the tin's own text and hers.
    /// </summary>
    private bool TheTinComesUp(SurfaceExcursion ex, int squareX, int squareY)
    {
        if (TheStringerOnThisGround(ex) is not { } q || PassageOf(q).Tin
            || TheTinsSquare(q, ex) is not { } tin || !CarryThePress.FindsTheTin(tin, squareX, squareY))
        {
            return false;
        }

        Core.Satchel.Item note = CarryThePress.TheNote();
        if (!Core.Satchel.CanTake(_satchel, note))
        {
            return false;
        }

        _satchel = [.. Core.Satchel.Add(_satchel, note)];
        RewritePassage(q, PassageOf(q) with { Tin = true });
        RendererInterop.PlayCue("reveal");
        ShowPulseMessage($"⛏ {CarryThePress.TinText} {CarryThePress.DigLine}");
        return true;
    }

    // ── THE WAY HOME ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#1202 · Lifting off her ground with her aboard: the trip is done, and she sleeps the burn back.
    /// Called at the liftoff press, after the liftoff's own line.</summary>
    private void SheSleepsTheBurnBack(SurfaceExcursion ex)
    {
        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.CarryThePress, State: QuestState.Active }
                && q.DestBodyId == ex.Stop.Body.Id && PassageOf(q).Site == ex.Site.Index)
            {
                AdvanceMission(q, QuestState.Complete, CarryThePress.LiftoffLine);
                RequestVaultSave();
                return;
            }
        }
    }

    /// <summary>#1202 · Clamped at a haven with her trip done: she pays, and the wire's clock starts. Called at
    /// the clamp, beside the cargo runs it settles.</summary>
    private void HerFareAtTheBerth()
    {
        for (int i = 0; i < _quests.Count; i++)
        {
            Quest q = _quests[i];
            if (q is not { Kind: QuestKind.CarryThePress, State: QuestState.Complete })
            {
                continue;
            }

            _credits += q.Reward;
            Quest paid = RewritePassage(q, PassageOf(q) with { TurnedIn = SimTime });
            AdvanceMission(paid, QuestState.TurnedIn,
                $"{CarryThePress.TurnInLine} +{q.Reward.ToString("N0", CultureInfo.InvariantCulture)} cr");
        }
    }

    // ── THE WIRE ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1202 · HER STORY, ON THE WIRE. Pushed events are not saved, so this runs on every sim advance and puts
    /// each paid contract's story back on the wire, dated when it ran, whenever it is due and not already
    /// there — exactly one of the two, chosen by the tin, and never before three sim-days after she paid. The
    /// book's entry is filed the first time and never again (the contract remembers), and the floor's reaction
    /// goes on the port rags a day later the same way.
    /// </summary>
    private void ThePressRunsHerStory()
    {
        for (int i = 0; i < _quests.Count; i++)
        {
            Quest q = _quests[i];
            if (q is not { Kind: QuestKind.CarryThePress, State: QuestState.TurnedIn, DestBodyId: { } bodyId })
            {
                continue;
            }

            CarryThePress.Passage p = PassageOf(q);
            if (!CarryThePress.StoryIsDue(p, SimTime))
            {
                continue;
            }

            string body = BodyName(bodyId);
            CarryThePress.Passage next = p;

            // #1202 slice 2 · …unless somebody got between her and the wire (Map.SpikeIt). The window decides
            // once: SPIKED prints nothing and files the hole under #1063's absence mark; ALTERED prints the
            // client's sentence under her byline; LATE — and every story nobody spiked — runs as she wrote it.
            SpikeIt.Outcome outcome = TheWindowDecides(ref next);
            if (outcome == SpikeIt.Outcome.Spiked)
            {
                if (!next.Printed)
                {
                    FileNote(SpikeIt.Spiked(body), MissingMiddle.Glyph);
                    next = next with { Printed = true };
                }

                if (next != p)
                {
                    RewritePassage(q, next);
                }

                continue;
            }

            OnTheWire(NewsWire.NewsEventKind.PressStoryFiled, CarryThePress.StoryAt(p)!.Value,
                outcome == SpikeIt.Outcome.Altered ? SpikeIt.Altered(body) : CarryThePress.Story(body, p.Tin), body);
            if (!next.Printed)
            {
                FileNote(outcome == SpikeIt.Outcome.Altered ? SpikeIt.AlteredEntry : CarryThePress.StoryRanLine,
                    CarryThePress.Glyph);
                next = next with { Printed = true };
            }

            if (CarryThePress.FloorIsDue(p, SimTime))
            {
                OnTheWire(NewsWire.NewsEventKind.PressFloorReaction, CarryThePress.FloorAt(p)!.Value,
                    CarryThePress.Floor(body), body);
                next = next with { Floored = true };
            }

            if (next != p)
            {
                RewritePassage(q, next);
            }
        }
    }

    /// <summary>One dated line on the wire, once: a line already there is left alone.</summary>
    private void OnTheWire(NewsWire.NewsEventKind kind, double at, string headline, string body)
    {
        foreach (NewsWire.NewsEvent evt in _newsEvents)
        {
            if (evt.Kind == kind && string.Equals(evt.Subject, headline, StringComparison.Ordinal))
            {
                return;
            }
        }

        _newsEvents.Insert(0, new NewsWire.NewsEvent(kind, at, headline, body));
        if (_newsEvents.Count > MaxNewsEvents)
        {
            _newsEvents.RemoveRange(MaxNewsEvents, _newsEvents.Count - MaxNewsEvents);
        }
    }

    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>How many of this berth's watches the dev start tries for a ground the shuttle can reach.</summary>
    private const int PressCheatWatchesTried = 64;

    /// <summary>
    /// #1202 QA · <c>?dock=&lt;berth&gt;&amp;press=1</c> — her contract taken at this berth, she is aboard, and
    /// her ground (one the shuttle can reach from here) is named on the ledger row and on the DEV line;
    /// <c>&amp;press=filed</c> — the same trip, tin dug, paid four sim-days ago, so her story is on the wire and
    /// the floor has reacted, and the book's entry is filed; <c>&amp;press=pending</c> — the same trip, paid this
    /// instant, so her story is three sim-days off with no spike against it and a dark-web desk carries SPIKE IT
    /// (#1202 QA, 2026-09-29). It plants what play would have left and nothing
    /// more; everything after it is the shipped path.
    /// </summary>
    private void TakeThePressForCheat()
    {
        CarryThePress.Cheat cheat = Navigation is { } address ? CarryThePress.CheatIn(address.Uri) : CarryThePress.Cheat.None;
        if (cheat == CarryThePress.Cheat.None || _ephemeris is null || DarkWebCurrentBody()?.Id is not { } here)
        {
            return;
        }

        var inReach = new HashSet<string>(StringComparer.Ordinal);
        foreach (ShuttleStop stop in ShuttleDestinationsInRange())
        {
            if (stop.IsLandable)
            {
                inReach.Add(stop.Body.Id);
            }
        }

        long watch = PatronRota.WatchIndex(SimTime);
        for (int i = 0; i < PressCheatWatchesTried; i++)
        {
            if (PressContractAt(here, watch + i) is not { DestBodyId: { } body } offer || !inReach.Contains(body))
            {
                continue;
            }

            _quests.Add(offer);
            if (cheat == CarryThePress.Cheat.Aboard)
            {
                CarryThePress.Passage p = PassageOf(offer);
                TreasureCache tin = CarryThePress.TheTin(offer.Id, body, p.Site);
                ShowPulseMessage($"🧪 DEV ?press=1 — {CarryThePress.Plate} aboard for {offer.TargetCallsign}; "
                    + $"the tin: {tin.BearingLine} (?site={p.Site})");
                return;
            }

            // Filed: the trip made, the tin dug, and her fare paid four sim-days back. Pending: the same trip,
            // her fare paid this instant — the story three sim-days off, nothing against it.
            _satchel = [.. Core.Satchel.Add(_satchel, CarryThePress.TheNote())];
            Quest home = RewritePassage(offer, PassageOf(offer) with
            {
                Landed = true, Walked = true, Tin = true,
                TurnedIn = cheat == CarryThePress.Cheat.Pending
                    ? SimTime
                    : SimTime - CarryThePress.StoryAfterSeconds - CarryThePress.FloorAfterStorySeconds,
            });
            AdvanceMission(home, QuestState.TurnedIn);
            if (cheat == CarryThePress.Cheat.Pending)
            {
                ShowPulseMessage($"🧪 DEV ?press=pending — {CarryThePress.Byline}'s {BodyName(body)} story is "
                    + $"{SpikeIt.WatchesUntil(CarryThePress.StoryAt(PassageOf(home))!.Value, SimTime)} watches off; "
                    + "open Comms → dark web for SPIKE IT");
                return;
            }

            ThePressRunsHerStory();
            ShowPulseMessage($"🧪 DEV ?press=filed — {CarryThePress.Byline}'s story is on the wire (Galley 6, Comms ticker)");
            return;
        }

        ShowPulseMessage("🧪 DEV ?press= — no ground this berth's shuttle can reach; try another ?dock=");
    }
}
