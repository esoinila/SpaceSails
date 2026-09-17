using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #711 slice 2 · <b>THE BOX HAS SOMEWHERE TO BE, AND SOMEBODY PAYS FOR IT.</b> The page's half of
/// <see cref="ParcelDrop"/> — the four moments Core cannot reach: the desk that names the ground, the
/// shovel that is the delivery, the desk that has money on it a day later, and the afternoon after a
/// confiscation when there is simply no row.
///
/// <h3>The pool of real ground, stated once</h3>
///
/// <para><see cref="TheLandableGround"/> is the ONLY place in this slice that decides what a destination
/// may name, and it decides it by reading the scenario the game actually loaded and filtering it with
/// <see cref="ShuttleExcursion.IsLandableSurface"/> — the same predicate the shuttle-bay destination board
/// is built out of. A job can therefore never name a place the shuttle refuses to fly to, and there is no
/// list of moons typed anywhere in this feature for a new scenario to make wrong.</para>
///
/// <h3>Nothing new is saved</h3>
///
/// <para>The parcel rides the satchel (slice 1). The delivery rides <c>TreasureCache.Deposit</c> (#319).
/// The pending payment IS that chest, due off its own burial stamp. The quiet watches after a
/// confiscation ride <c>_roomsTurnedOver</c>, the durable register the fence's one-key-per-window already
/// strikes off in. Four facts, four homes that already ride the vault, and not one new section — so
/// everything in this slice survives a save, a reload and a death without a line of vault code.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>
    /// #711 · <b>EVERY GROUND IN THIS SKY A SHUTTLE COULD BE FLOWN DOWN TO</b> — the scenario's own moons,
    /// snapshotted at boot. Empty before the world is built, which is the honest answer at that moment and
    /// the one case <see cref="ParcelDrop.For(string, IReadOnlyList{string})"/> hands back null for.
    /// </summary>
    private readonly List<string> _groundADropMayName = [];

    /// <summary>
    /// #711 · <b>TAKEN FROM THE FILE, NOT FROM THE LIVE SKY.</b> Filtered with
    /// <see cref="ShuttleExcursion.IsLandableSurface"/> — the same predicate the shuttle-bay board is built
    /// out of — so a place a job can name is a place the shuttle flies to, and there is no list of moons
    /// typed anywhere in this feature for a new scenario to make wrong.
    ///
    /// <para><b>Why the scenario and not <c>_ephemeris.Bodies</c>.</b> Four cheats hang extra bodies off the
    /// berth before the ephemeris is built (the Kepler demo rock, the expedition site, the deflection rock,
    /// the wreck) and every one of them is a <c>moon</c>. A pool that counted those would answer a DIFFERENT
    /// destination on a boot that used one — and the parcel in the captain's pocket does not know which URL
    /// he opened the tab with. The job is a promise made once at a desk, so the ground it may name is a fact
    /// about the SKY THE SCENARIO SHIPS and nothing else.</para>
    /// </summary>
    private void RememberTheGroundADropMayName(ScenarioDefinition scenario)
    {
        _groundADropMayName.Clear();
        foreach (BodyDefinition body in scenario?.Bodies ?? [])
        {
            if (ShuttleExcursion.IsLandableSurface(CircularOrbitEphemeris.KindOf(body.Kind)))
            {
                _groundADropMayName.Add(body.Id);
            }
        }
    }

    /// <summary>#711 · The pool, as Core wants it.</summary>
    private IReadOnlyList<string> TheLandableGround() => _groundADropMayName;

    /// <summary>#711 · Where the box in the captain's pocket is going, or null when there is no box.</summary>
    private ParcelDrop.Destination? TheParcelsDestination() =>
        ParcelDrop.TheParcelIn(_satchel) is { } parcel
            ? ParcelDrop.For(parcel, TheLandableGround())
            : null;

    /// <summary>#711 · The ground the job names, as the desk prints it — the body's own display name out of
    /// the ephemeris and the site's out of its board, composed by Core so the desk row and the map card
    /// spell one place one way. Empty when there is no job in hand.</summary>
    private string TheParcelsDestinationRow() =>
        TheParcelsDestination() is { } where
            ? ParcelDrop.DestinationRow(BodyName(where.BodyId), where.SiteName)
            : "";

    // ── THE SHOVEL IS THE DELIVERY ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #711 · <b>THE DROP, AT THE ONE PRESS WHERE IT HAPPENS.</b> Asked of the chest the shovel just minted
    /// rather than of the captain's pocket, because by this point the box has left the pocket and is in the
    /// ground — which is the only state in which the word <i>delivered</i> means anything.
    ///
    /// <para>Returns the tail the dig's own pulse carries, so the delivery is told ON the sentence the
    /// captain is already reading instead of as a second pulse that would overwrite it. Empty when this
    /// hole is not the hole — and a parcel buried on the wrong ground gets exactly the pulse and exactly
    /// the ✗ any other buried thing gets, because that is exactly what it is.</para>
    ///
    /// <para>The book's entry names the ground in the book's own spelling
    /// (<see cref="FieldNotes.PlaceLabel"/>) and DECLARES that place as its subject — #741's law, the
    /// author saying what its sentence is about rather than a regex going looking.</para>
    /// </summary>
    private string TheDropIsMade(TreasureCache cache)
    {
        if (!ParcelDrop.IsTheDelivery(cache, TheLandableGround()))
        {
            return "";
        }

        string place = FieldNotes.PlaceLabel(BodyName(cache.BodyId), cache.SiteName);
        FileNoteAbout(
            ParcelDrop.TheFieldBookEntry(place),
            UnlistedParcel.Glyph,
            ParcelDrop.TheFieldBookSubjects(place));

        return " " + ParcelDrop.DeliveredLine;
    }

    // ── AND A DAY LATER, ACROSS A DESK ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// #711 · <b>THE MONEY IS ON THE DESK.</b> Called at the one place the dark-web desk is opened, so the
    /// payment arrives the way the owner's brief says it must — not on a timer, not in a banner, but
    /// because the captain walked back to the counter and it was there.
    ///
    /// <para>Three things happen in this order and the order is the whole of "paid exactly once":
    /// Core names the chest, the chest is LIFTED OUT OF THE LEDGER, and only then does the coin move. The
    /// hole is what makes the payment due, so a hole that is gone is a payment that cannot come twice —
    /// there is no flag to clear and nothing to forget to clear. A reload between the burial and the desk
    /// changes nothing, because the chest was in the vault the whole time.</para>
    ///
    /// <para>And the ground keeps it: a <see cref="GroundMemory.ScarKind.Pit"/> at the ✗, stamped with the
    /// moment the payment came DUE rather than with now — the discipline <c>TheRivalsLeftTheirMarks</c>
    /// keeps, because a captain who flies back a fortnight later should read a fortnight of dust and not a
    /// fresh hole. Walk out there again and #316's own three bands will date it for him, and nothing
    /// anywhere will say who held the shovel.</para>
    /// </summary>
    private void ThePaymentIsThere()
    {
        if (_caches is null
            || ParcelDrop.ThePaymentThatIsThere(_caches.Caches, TheLandableGround(), SimTime) is not { } paid
            || _caches.Remove(paid.CacheId) is not { } lifted)
        {
            return;
        }

        _credits += paid.Amount;
        SomebodyDugIt(lifted, paid.DueAtSimTime);

        ShowPulseMessage(
            $"💳 {ParcelDrop.PaymentLine} +{paid.Amount.ToString("N0", CultureInfo.InvariantCulture)} cr");
        RequestVaultSave();
        StateHasChanged();
    }

    // ── AND THE AFTERNOONS WHEN THERE IS NO ROW ─────────────────────────────────────────────────────────

    /// <summary>
    /// #711 · <b>A MAN WITH A FORM TOOK THE BOX, SO NOBODY HAS ANYTHING FOR THIS HULL.</b> Written at the
    /// confiscation, into the captain's own durable register of ground gone through — one tag per silent
    /// watch, the shape <see cref="BlackOpsKey.ThePortHasDealtOne"/> already writes.
    ///
    /// <para>It is deliberately not a refusal and not a message. The row is simply not drawn, exactly as it
    /// is not drawn while the captain is already carrying one, and nothing anywhere says why.</para>
    /// </summary>
    public void TheDeskHasNothingForAWhile(string parcelId)
    {
        foreach (string watch in ParcelDrop.TheWatchesWithNothingOnThem(parcelId, SimTime))
        {
            _roomsTurnedOver.Add(watch);
        }
        RequestVaultSave();
    }

    /// <summary>#711 · Is this one of those watches? Read by the desk before it draws the row.</summary>
    private bool TheDeskHasNothingForThisHull() =>
        _roomsTurnedOver.Contains(ParcelDrop.NothingForThisHullOn(PatronRota.WatchIndex(SimTime)));

    // ── THE DEV DOOR ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#711 · <c>/map?parcel=1</c> — boot with the box in the pocket, standing on the ground it is
    /// for. Documented in <c>docs/testing-links-2026-09-17.md</c>.</summary>
    private bool _parcelCheat;

    /// <summary>How many consecutive windows the cheat will mint a parcel on looking for one whose ground
    /// this berth can actually reach. A parcel's destination is drawn off its own id and its id carries the
    /// watch, so successive windows are successive draws — the cheat picks a WINDOW and never a
    /// destination, which is what keeps it a real parcel rather than a forged one.</summary>
    private const int ParcelCheatWindowsTried = 64;

    /// <summary>
    /// #711 · <b>THE DEV DOOR, AND IT FORGES NOTHING.</b> Owner's law for this family
    /// (<c>Map.Surface.Cheats</c>): <i>a cheat that shows a tester a different scene is worse than no cheat
    /// at all.</i>
    ///
    /// <para>So it does not plant a destination. It mints REAL parcels the way the desk mints them
    /// (<see cref="UnlistedParcel.FromTheDesk"/>, this berth, one window after another) and stops at the
    /// first one whose own seeded ground is landable from where the ship is clamped — then rides
    /// <c>?land=</c>'s existing descent to exactly that ground. Everything the tester then does is the
    /// shipped path: the desk row, the walk, the DIG HERE press, the pulse, the book.</para>
    ///
    /// <para>Run before <c>?land=</c> fires and after the berth is clamped, because it WRITES the landing
    /// the cheat is about to take.</para>
    /// </summary>
    private void TakeAParcelForCheat()
    {
        if (!_parcelCheat || _ephemeris is null)
        {
            return;
        }
        _parcelCheat = false;

        var inReach = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (ShuttleStop stop in ShuttleDestinationsInRange())
        {
            if (stop.IsLandable)
            {
                inReach.Add(stop.Body.Id);
            }
        }

        IReadOnlyList<string> ground = TheLandableGround();
        string haven = _dockedHavenId ?? _dockBodyId ?? "";
        long watch = PatronRota.WatchIndex(SimTime);

        for (int i = 0; i < ParcelCheatWindowsTried; i++)
        {
            Satchel.Item parcel = UnlistedParcel.FromTheDesk(haven, watch + i);
            if (ParcelDrop.For(parcel, ground) is not { } where || !inReach.Contains(where.BodyId))
            {
                continue;
            }

            _satchel = [.. Satchel.Add(_satchel, parcel)];
            _landBodyCheat = where.BodyId;
            _forcedSiteIndex = where.SiteIndex;
            _landCheat = true;
            ShowPulseMessage(
                $"🧪 DEV ?parcel=1 — {UnlistedParcel.Plate} → "
                + ParcelDrop.DestinationRow(BodyName(where.BodyId), where.SiteName));
            return;
        }

        ShowPulseMessage("🧪 DEV ?parcel=1 — no drop this berth can reach; try another ?dock=");
    }
}
