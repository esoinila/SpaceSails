using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// #1074 beat 5 · THE RETURNING SHUTTLE — the client half. Core carries the whole argument (see
// ReturningShuttle.cs: the canon lines, the eligibility predicate, the phases, the hull's geometry); this file
// owns what only the Map can own: the two persisted rows, the moments the beat is asked, the board row, the
// one line on the wire, and the three fixtures aboard.
//
// THE SEAMS IT USES, each already shipped and none duplicated: the ONE ARRIVAL DOOR (TheArrivalIsRemembered)
// asks whether the beat fires; the world's slow tick (RefillTraffic's clock) brings the shuttle back and prints
// the wire's line once; the depot idiom (TheOldShip.Berthed) puts a hull on the board that never moves again;
// ComposeTheDroppedSchedule's idiom (an appended region keyed off a label) draws the hull and takes the
// presses. Nothing here reads or writes the preserved-site register, the stop register or the disclosure
// clock — the study never ends, and the site afterwards is byte-identical.
public partial class Map
{
    /// <summary>#1074 beat 5 · The preserved grounds the captain has stood on, in the order he stood on them.
    /// Persisted in the vault's ProgressSection (null while empty).</summary>
    private List<string> _shuttleSeen = [];

    /// <summary>#1074 beat 5 · The beat's one row — null until it has fired, and so also the once-per-run
    /// latch. Persisted (null while unfired).</summary>
    private ReturningShuttle.Row? _shuttle;

    /// <summary>#1074 beat 5 · <c>/map?shuttle=1</c> — the preserved site with the beat already fired and the
    /// shuttle already back. See the query parser.</summary>
    private bool _shuttleCheat;

    private IReadOnlyList<string>? ShuttleSeenRows() => _shuttleSeen.Count > 0 ? [.. _shuttleSeen] : null;

    private void RestoreShuttle(ProgressSection? progress)
    {
        _shuttleSeen = progress?.ShuttleSeen is { } seen ? [.. seen] : [];
        _shuttle = progress?.Shuttle;
    }

    /// <summary>The body a body orbits, or null.</summary>
    private string? ParentOfBody(string id) =>
        _ephemeris?.Bodies.FirstOrDefault(b => b.Id == id)?.ParentId;

    /// <summary>
    /// #1074 beat 5 · <b>THE ONE QUESTION ASKED ON AN ARRIVAL.</b> Called from <see cref="TheArrivalIsRemembered"/>
    /// — every real arrival comes through there. Two things: note that the captain is now standing on a
    /// preserved ground (the first half of the eligibility), and ask whether this arrival is the one the
    /// beat fires on.
    /// </summary>
    private void TheShuttleIsAskedAbout(string? bodyId)
    {
        if (string.IsNullOrEmpty(bodyId) || _ephemeris is null)
        {
            return;
        }

        // He has STOOD on it: the boat is down on this very ground and the ground is in care.
        if (_surface is { } ex
            && string.Equals(ex.Stop.Body.Id, bodyId, System.StringComparison.Ordinal)
            && PreservationZone.On(bodyId)
            && !_shuttleSeen.Contains(bodyId))
        {
            _shuttleSeen.Add(bodyId);
            RequestVaultSave();
        }

        string? site = ReturningShuttle.Fires(
            _shuttle, _shuttleSeen, _fieldNotes, SimTime, bodyId, ParentOfBody, PreservationZone.On);
        if (site is null)
        {
            return;
        }

        _shuttle = new ReturningShuttle.Row(site, DisclosureClock.WindowAt(SimTime));
        TheCharterHullIsOnTheBoard();
        RequestVaultSave();
    }

    /// <summary>#1074 beat 5 · The hull on the traffic board — idempotent, so a reload (which rebuilds the
    /// board from nothing) puts her back the same way the first firing did.</summary>
    private void TheCharterHullIsOnTheBoard()
    {
        if (_shuttle is not { } row || _ephemeris is null
            || _ephemeris.Bodies.All(b => b.Id != row.Body))
        {
            return;
        }

        string id = ReturningShuttle.ShipIdFor(row.Body);
        if (_npcStates.Any(n => n.Ship.Id == id))
        {
            return;
        }

        _npcStates = [.. _npcStates, new NpcState { Ship = ReturningShuttle.Parked(_ephemeris, row.Body) }];
    }

    /// <summary>
    /// #1074 beat 5 · <b>THE SHUTTLE COMES BACK</b>, on the world's slow clock. A window after the hull set
    /// down, the wire prints its one line (latched in the persisted row, because pushed events are not
    /// saved). Nothing else changes in the world: the hull does not move again, and nothing is closed.
    /// </summary>
    private void TheReturningShuttleKeepsItsHours()
    {
        if (_shuttle is null)
        {
            return;
        }

        TheCharterHullIsOnTheBoard();
        if (ReturningShuttle.WireIsOwed(_shuttle, DisclosureClock.WindowAt(SimTime)))
        {
            PushNewsEvent(NewsWire.NewsEventKind.ArcBeatBreaks, ReturningShuttle.WireLine, StopOrder.Stamp);
            _shuttle = _shuttle with { Wired = true };
            RequestVaultSave();
        }
    }

    /// <summary>#1074 beat 5 · The dev cheat's own step: on the descent, once the ordinary paperwork has taken
    /// the cheat ground into care, fire the beat as if it had landed a whole window ago.</summary>
    private void TheShuttleCheatLands()
    {
        if (!_shuttleCheat || _shuttle is not null || _hallsPreserved.Count == 0)
        {
            return;
        }

        string site = _hallsPreserved[0];
        if (!_shuttleSeen.Contains(site))
        {
            _shuttleSeen.Add(site);
        }
        _shuttle = new ReturningShuttle.Row(
            site, DisclosureClock.WindowAt(SimTime) - ReturningShuttle.WindowsToReturn);
        TheCharterHullIsOnTheBoard();
        RequestVaultSave();
    }

    // ── THE HULL ON THE GROUND ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1074 beat 5 · Compose the parked hull onto the preserved ground — walls from the moment she sets
    /// down, her three fixtures only once the shuttle is back. An appended region, so the memoised base
    /// layout and the preserved site's own deck are untouched (byte-identical).
    /// </summary>
    private void ComposeTheSurveyHull(SurfaceExcursion ex)
    {
        if (_shuttle is not { } row
            || !string.Equals(row.Body, ex.Stop.Body.Id, System.StringComparison.Ordinal))
        {
            return;
        }

        ReturningShuttle.Hull? parked = MoonSurface.SurveyHullOn(
            ex.Stop.Body.Id, ex.Stop.Body.Name, OwnCachePositionsAt(ex.Stop.Body.Id, ex.Site.Index),
            ex.Site.LayoutSalt, ex.Site.Name, Monolith.EpochAt(SimTime), ex.Lab is { HasLab: true });
        if (parked is not { } hull)
        {
            return;
        }

        var walls = new List<DeckPlan.Wall>();
        foreach (SurfaceLayout.Wall w in hull.Walls)
        {
            walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, true));
        }

        var labels = new List<(float X, float Y, string Text)>
        {
            ((float)hull.PlateX, (float)hull.PlateY, ReturningShuttle.CallsignFor(row.Body)),
        };

        var consoles = new List<DeckPlan.ConsoleSpot>();
        foreach (string plate in ReturningShuttle.Fixtures(row, DisclosureClock.WindowAt(SimTime), _fieldNotes))
        {
            (double x, double y) = plate == ReturningShuttle.AirlockPlate ? hull.Airlock
                : plate == ReturningShuttle.RackPlate ? hull.Rack : hull.Log;
            consoles.Add(new(DeckPlan.ConsoleKind.ViewObject, (float)x, (float)y, plate));
        }

        _deckPlan.AppendRegion(new DeckPlan.DeckRegion([.. walls], [.. consoles], [.. labels], []));
    }

    /// <summary>
    /// #1074 beat 5 · <b>THE THREE FIXTURES ABOARD</b>, recognised by their plates as the dropped schedule's
    /// is, so no dispatch of its own. Each tells once: the airlock and the rack file their line when first
    /// pressed and say nothing after; the log desk takes the last page into the satchel and files the field
    /// book's 📍 line. Nothing here closes the question and nothing ever will.
    /// </summary>
    private bool TryTheSurveyHull(SurfaceExcursion ex, string label)
    {
        if (label is ReturningShuttle.AirlockPlate or ReturningShuttle.RackPlate)
        {
            if (ReturningShuttle.TellsOnPress(label, _fieldNotes) is { } line)
            {
                ShowAndFile(line, "🛸");
            }
            return true;
        }
        if (!string.Equals(label, ReturningShuttle.LogPlate, System.StringComparison.Ordinal))
        {
            return false;
        }

        var page = new Core.Satchel.Item(Core.Satchel.Kind.Paper, ReturningShuttle.LogPaperId);
        if (ReturningShuttle.Told(_fieldNotes, ReturningShuttle.FieldBookLine))
        {
            return true;
        }
        if (!Core.Satchel.CanTake(_satchel, page))
        {
            ShowPulseMessage(UndergroundComplex.PocketFullLine.Trim());
            return true;
        }

        _satchel = [.. Core.Satchel.Add(_satchel, page)];
        CarriedObject.Reveal read = CarriedObject.PaperReveal(ReturningShuttle.LogPaperId);
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            read.Label, read.ArtUrl, read.Story, UndergroundComplex.PaperPocketLine.Trim());

        FileNote(ReturningShuttle.FieldBookLine, ReturningShuttle.FieldBookGlyph);
        RebuildSurfaceDeck();
        RequestVaultSave();
        StateHasChanged();
        return true;
    }
}
