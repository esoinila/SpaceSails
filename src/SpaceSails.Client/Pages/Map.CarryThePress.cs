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
    /// #1202 · Her card, when she is the stranger at this table this watch — null, and no card, otherwise. One
    /// contract at a time: never while one of hers is still in the captain's hand.
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

    /// <summary>#1202 · Taken: she stows one bag and the recorder. Said once, at the press that took her.</summary>
    private void SheStowsOneBag() => SayItWhereTheyAreLooking(CarryThePress.TakenLine);

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

    // ── SHE WALKS THE GROUND BEHIND HIM ─────────────────────────────────────────────────────────────────

    /// <summary>The walker that is her, if she is on the ground. By errand, because her plate is her own.</summary>
    private static Walker? TheStringerAfoot(IReadOnlyList<Walker> afoot)
    {
        foreach (Walker w in afoot)
        {
            if (w.For == Errand.RidingAlong)
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>
    /// #1202 · One frame of her, called from the surface tick beside Brem Kolt's. Off her ground she is aboard
    /// and nothing is drawn. On it: her word about the tin is said and filed once; she comes down with the
    /// captain; she keeps the band behind him (<see cref="TheTailBehindYou.HoldsHisBand"/>, the coat's own
    /// arithmetic); and the first time he stops with her behind him, she says her one line about the air. She
    /// needs no air model of her own: the line says it — same tank.
    /// </summary>
    private void AdvanceTheStringer(double dtRealSeconds)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        Walker? her = TheStringerAfoot(ex.Walkers);
        if (TheStringerOnThisGround(ex) is not { } q)
        {
            if (her is not null)
            {
                ex.Walkers.Remove(her);
            }

            return;
        }

        CarryThePress.Passage p = PassageOf(q);
        if (!p.Landed)
        {
            string line = CarryThePress.Landing(TheTinsWords(q, ex));
            FileNote(line, CarryThePress.Glyph);
            ShowPulseMessage(line);
            q = RewritePassage(q, p = p with { Landed = true });
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        if (her is null)
        {
            if (ex.Walkers.Count < WalkerBand)
            {
                _ = PlanTheStringer(ex, new DeckReachability.Point(_avatarX, _avatarY), walls);
            }

            return;
        }

        if (her.Walk.State != NpcWalk.Doing.Arrived)
        {
            her.Walk.Step(Math.Min(dtRealSeconds, 0.1), walls, _avatarX, _avatarY);
            if (her.Walk.Afoot || her.Walk.State == NpcWalk.Doing.Arrived)
            {
                return;
            }

            // The ground refused her somewhere on the way. She starts again from her own feet.
            ex.Walkers.Remove(her);
            _ = PlanTheStringer(ex, new DeckReachability.Point(her.Walk.X, her.Walk.Y), walls);
            return;
        }

        double dx = her.Walk.X - _avatarX, dy = her.Walk.Y - _avatarY;
        double range = Math.Sqrt((dx * dx) + (dy * dy));

        // …and the first time he stops out on the ground with her behind him, she says it. Once.
        if (!p.Walked && TheTailBehindYou.HoldsHisBand(range)
            && MoonSurface.IsDiggableGround(_avatarX, _avatarY, ex.Floor))
        {
            SayItWhereTheyAreLooking(CarryThePress.WalkLine);
            RewritePassage(q, p with { Walked = true });
        }

        // Out of the band either way — he has walked off, or walked up to where she stands — and she takes a
        // new place in it from her own feet, the middle of the band first, so a few paces either way keep her.
        if (!TheTailBehindYou.HoldsHisBand(range))
        {
            ex.Walkers.Remove(her);
            _ = PlanTheStringer(ex, new DeckReachability.Point(her.Walk.X, her.Walk.Y), walls);
            return;
        }

        her.Walk.LookTowards(_avatarX, _avatarY);
    }

    /// <summary>
    /// Plan her next leg: from her own feet to a standing place behind the captain, inside the band — the
    /// coat's ranges and sides, in the coat's order (<see cref="TheTailBehindYou.TheRangesHeTries"/>,
    /// <see cref="TheTailBehindYou.TheSidesHeSounds"/>): the middle of the band first, so a captain who takes a
    /// few paces either way does not send her walking again.
    /// </summary>
    private bool PlanTheStringer(
        SurfaceExcursion ex, DeckReachability.Point from, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        double heading = _avatarHeading;
        foreach (double range in TheTailBehindYou.TheRangesHeTries)
        {
            foreach (double side in TheTailBehindYou.TheSidesHeSounds)
            {
                double x = _avatarX + (Math.Cos(heading + side) * range);
                double y = _avatarY + (Math.Sin(heading + side) * range);
                SpawnNudge.Result spot = SpawnNudge.Clear(x, y, DeckPlan.AvatarRadius, walls);
                if (spot.Failed)
                {
                    continue;
                }

                if (OnFoot(CarryThePress.Plate, new NpcWalk.Bound("", spot.X, spot.Y), from, walls,
                        NpcWalk.NoPersonalSpace) is { } walk)
                {
                    ex.Walkers.Add(new Walker { Walk = walk, Table = -1, For = Errand.RidingAlong });
                    StateHasChanged();
                    return true;
                }
            }
        }

        return false;
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
            OnTheWire(NewsWire.NewsEventKind.PressStoryFiled, CarryThePress.StoryAt(p)!.Value,
                CarryThePress.Story(body, p.Tin), body);
            CarryThePress.Passage next = p;
            if (!p.Printed)
            {
                FileNote(CarryThePress.StoryRanLine, CarryThePress.Glyph);
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
    /// the floor has reacted, and the book's entry is filed. It plants what play would have left and nothing
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

            // Filed: the trip made, the tin dug, and her fare paid four sim-days back.
            _satchel = [.. Core.Satchel.Add(_satchel, CarryThePress.TheNote())];
            Quest home = RewritePassage(offer, PassageOf(offer) with
            {
                Landed = true, Walked = true, Tin = true,
                TurnedIn = SimTime - CarryThePress.StoryAfterSeconds - CarryThePress.FloorAfterStorySeconds,
            });
            AdvanceMission(home, QuestState.TurnedIn);
            ThePressRunsHerStory();
            ShowPulseMessage($"🧪 DEV ?press=filed — {CarryThePress.Byline}'s story is on the wire (Galley 6, Comms ticker)");
            return;
        }

        ShowPulseMessage("🧪 DEV ?press= — no ground this berth's shuttle can reach; try another ?dock=");
    }
}
