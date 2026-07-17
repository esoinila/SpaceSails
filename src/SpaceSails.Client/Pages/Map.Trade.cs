using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Trade — the coin: the market and cargo hold, fuel and upgrades, rescue, the dark web,
// the trade corridors and local-space dealing. Lifted from Map.razor for #251, motion only.
public partial class Map
{

    /// <summary>One line of the Trade desk's cargo manifest panel (PR-13): what's actually in the
    /// hold, broken down by class, with its fence value via <see cref="CargoMarket"/> — a
    /// read-model over <see cref="_cargoByClass"/>, which Board() keeps in step with the existing
    /// aggregate _cargoUnits/_cargoValue totals.</summary>
    private readonly record struct CargoManifestEntry(string CargoClass, int Units, int Value);

    private IReadOnlyList<CargoManifestEntry> CargoManifest()
    {
        var list = new List<CargoManifestEntry>();
        foreach ((string cargoClass, int units) in _cargoByClass)
        {
            if (units <= 0)
            {
                continue;
            }

            list.Add(new CargoManifestEntry(cargoClass, units, units * CargoMarket.UnitValue(cargoClass)));
        }

        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        return list;
    }

    // ---- PR-5: orbital commerce — trade from orbit or course-matched with drones ----
    private SpaceSails.Client.Pages.Stations.LocalSpace? _localSpace;
    private string? _localTradeTargetId;                // LocalContact id mid-transfer, if any
    private double _localTradeProgress;                  // drone transfer progress fraction [0,1)
    private string? _localTradeMessage;
    private static readonly RgbaColor LocalContactRingColor = new(120, 200, 255, 150);
    private static readonly string[] MarketBodies = ["earth", "mars", "venus"];

    // The captain's starting book (owner: "It is an operating ship with some history" — the
    // same world-does-not-wait principle as the populated sky at t=0). The purse is the last
    // run's takings; the hold carries that gig's leftovers, mixed classes like a real
    // manifest, not a bare hull. 1,500 cr covers Earth Depot's whole stock via shuttles
    // (4×250 + 100 cr fee) with change, and stays deliberately below the 2,000 cr upgrade
    // price so the first pod run remains the tutorial's real payday.
    private const int StartingCredits = 1500;
    private static readonly (string Class, int Units)[] StartingManifest =
        [("Alloys", 2), ("Ice", 3)];

    private int _credits;
    private int _cargoUnits;
    private int _cargoValue;
    // Trade desk cargo manifest (PR-13): a per-class breakdown of the hold, alongside the
    // pre-existing aggregate _cargoUnits/_cargoValue totals every other flow (SellCargo, drone
    // trade, Adrift/hunter-catch confiscation) already reads/clears. Additive bookkeeping only —
    // Board() is still the single place cargo is granted, so this dictionary and the totals never
    // drift apart.
    private readonly Dictionary<string, int> _cargoByClass = [];
    private int _massLevel;                            // reaction-mass capacity: 250 + 150/level
    private int _sensorLevel;                          // sensor range: base × 1.4/level
    private int _holdLevel;                            // cargo hold: 10 + 10/level

    private int ReactionMassCapacity => 500 + 150 * _massLevel;
    private int CargoCapacity => 10 + 10 * _holdLevel;
    private static int UpgradePrice(int level) => 2000 * (1 << level);

    // ---- SundaySecondPlan PR-B: the sky shows its state (Sensors-desk overlays) ----
    // Trade lanes as selectable areas, the wedge the telescope is on RIGHT NOW with its sweep
    // progress, the expanding search circle where a lost dark ship must still be, and a bracket
    // flash on a tracked ship the moment its scheduled custody pass runs.

    private static readonly RgbaColor CorridorFillColor = new(80, 200, 220, 14);
    private static readonly RgbaColor CorridorSelectedFillColor = new(80, 200, 220, 42);
    private static readonly RgbaColor CorridorEdgeColor = new(80, 200, 220, 60);
    private static readonly RgbaColor CorridorLabelColor = new(140, 210, 220, 150);
    private IReadOnlyList<CorridorRegion> _mapCorridors = [];
    private double _mapCorridorsBuiltAt = double.NegativeInfinity;
    private string? _selectedCorridorKey; // the lane whose menu is open, drawn brighter

    internal static string CorridorKey(CorridorRegion lane) => $"{lane.AId}:{lane.BId}";

    private void DrawTradeCorridors()
    {
        if (_ephemeris is null)
        {
            return;
        }

        if (SimTime - _mapCorridorsBuiltAt > 3600)
        {
            _mapCorridors = TradeCorridors.Regions(_ephemeris, SimTime);
            _mapCorridorsBuiltAt = SimTime;
        }

        Span<float> quad = stackalloc float[8];
        foreach (CorridorRegion lane in _mapCorridors)
        {
            Vector2d axis = lane.B - lane.A;
            if (axis.LengthSquared == 0)
            {
                continue;
            }

            Vector2d dir = axis.Normalized();
            Vector2d perp = new(-dir.Y, dir.X);
            (quad[0], quad[1]) = _camera.WorldToScreen(lane.A + perp * lane.Radius);
            (quad[2], quad[3]) = _camera.WorldToScreen(lane.B + perp * lane.Radius);
            (quad[4], quad[5]) = _camera.WorldToScreen(lane.B - perp * lane.Radius);
            (quad[6], quad[7]) = _camera.WorldToScreen(lane.A - perp * lane.Radius);

            bool selected = _selectedCorridorKey == CorridorKey(lane);
            _renderer!.DrawPolygon(quad, selected ? CorridorSelectedFillColor : CorridorFillColor,
                CorridorEdgeColor, selected ? 1.6f : 1f);

            (float mx, float my) = _camera.WorldToScreen(lane.Midpoint);
            _renderer.DrawText(mx, my - 6, lane.Name, CorridorLabelColor, "11px sans-serif", TextAlign.Center);
        }
    }

    /// <summary>The lane nearest a sky point, when it counts as "near" — the guideline hint
    /// ("this empty spot sits by the Earth–Mars lane; scan the lane instead?").</summary>
    private CorridorRegion? NearLaneFor(Vector2d point)
    {
        if (_mapCorridors.Count > 0
            && TradeCorridors.TryNearest(_mapCorridors, point, out CorridorRegion lane, out double distance)
            && distance <= lane.Radius * TradeCorridors.NearLaneFactor)
        {
            return lane;
        }

        return null;
    }

    private void SellCargo()
    {
        if (_cargoValue <= 0)
        {
            return;
        }

        _credits += _cargoValue;
        ShowPulseMessage($"Sold {_cargoUnits} units for {_cargoValue:N0} credits {PlunderLines[(int)((SimTime / 61) % PlunderLines.Length)]}");
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();
        _hotCargo.Launder(); // fenced — the hot flags go with the cargo
        AdvanceTutorial(4); // step 5: first sale
        AdvanceTutorial(StepSellHe3); // second hunt, step 6: fencing the He3 closes the tutorial
        RequestVaultSave(); // #225: a sale changed the purse and emptied the hold
    }

    // #157 "How do I fill her up?" — the FIRST place the tank is ever REFILLED (every other flow only
    // spends pulses). Buys reaction mass at the pump the ship is alongside: caps at the tank, caps at the
    // purse (buys what you can afford and says so), decrements credits by exactly the quote, and logs a
    // ledger line. The alongside gate reuses FuelReachability.AlongsidePump — the same truth the fuel
    // alarm reads — so the button and the alarm can never disagree. pulsesWanted = int.MaxValue is "fill
    // her up"; a small number is the +10 p top-up.
    private void BuyFuel(int pulsesWanted)
    {
        if (_ephemeris is null || FuelReachability.AlongsidePump(_ephemeris, _ship) is not { } pump)
        {
            return; // not alongside a pump — the button isn't shown, but never trust the caller
        }

        int capacity = ReactionMassCapacity;
        int room = capacity - _reactionMassPulses;
        if (room <= 0)
        {
            ShowPulseMessage("Tank's already topped off.");
            return;
        }

        int price = FuelMarket.PricePerPulse(_ephemeris.Position(pump.Id, SimTime).Length);
        FuelMarket.Quote quote = FuelMarket.QuoteFill(_reactionMassPulses, capacity, price, _credits, pulsesWanted);
        if (quote.Pulses <= 0)
        {
            ShowPulseMessage($"Not enough credits — reaction mass is {price} cr/pulse and you hold {_credits:N0} cr.");
            return;
        }

        _credits -= quote.Cost;
        _reactionMassPulses += quote.Pulses;
        LogAutopilotEvent($"⛽ Took on {quote.Pulses} p at {BodyName(pump.Id)} — {quote.Cost:N0} cr");
        ShowPulseMessage(quote.Pulses < room
            ? $"⛽ Bought the {quote.Pulses} p you could afford at {BodyName(pump.Id)} ({quote.Cost:N0} cr) — {capacity - _reactionMassPulses} p of room left"
            : $"⛽ Filled her up at {BodyName(pump.Id)} — {quote.Pulses} p for {quote.Cost:N0} cr");
        _fuelDirSig = null; // any cached "nearest pump" directions are stale now
    }

    // PR-WIRE — the broke-at-a-pump borrow (the dream's anonymized gas-by-wire). A trusted, dark-web-
    // native contact we have history with will wire fuel money on a favor. Picks the most-trusted such
    // contact (most jobs done), or null if none qualifies — the button only shows when this is non-null.
    private string? BestWireLenderId()
    {
        string? best = null;
        int bestMissions = -1;
        foreach ((string id, ContactHistory h) in _contacts.Entries)
        {
            if (!FavorBank.CanWireLoan(ContactSheets.For(id), h.MissionsCompleted))
            {
                continue;
            }
            if (h.MissionsCompleted > bestMissions)
            {
                bestMissions = h.MissionsCompleted;
                best = id;
            }
        }
        return best;
    }

    // The favor line a pump borrow draws: enough to fill the room at the current price, capped so a
    // single favor never wires a fortune (BankLoanPrincipal is the ceiling — a good top-up, no more).
    private long PumpLoanPrincipal(int room, int pricePerPulse) =>
        Math.Min(BankLoanPrincipal, Math.Max(100L, (long)Math.Max(0, room) * Math.Max(1, pricePerPulse)));

    // Wire the favor at the pump and fill what it buys — the stranded captain's rescue. Books the debt
    // and raises the quiet-delivery obligation; then spends the wired coin straight into the tank.
    private void CallInFavorAtPump()
    {
        if (_ephemeris is null || FuelReachability.AlongsidePump(_ephemeris, _ship) is not { } pump)
        {
            return;
        }
        if (BestWireLenderId() is not { } lender)
        {
            return;
        }
        int room = ReactionMassCapacity - _reactionMassPulses;
        int price = FuelMarket.PricePerPulse(_ephemeris.Position(pump.Id, SimTime).Length);
        long principal = PumpLoanPrincipal(room, price);
        if (!BankBorrowFavor(lender, principal, viaWire: true))
        {
            return;
        }
        string lenderName = ContactSheets.For(lender).DisplayName;
        ShowPulseMessage($"📡 {lenderName} wires {principal:N0} cr, no questions — you owe them one quiet delivery. Fill her up.");
        BuyFuel(int.MaxValue); // spend the fresh coin straight into the tank
    }

    // The honest "how do I refuel from here" line for the Trade desk when the ship is NOT alongside a pump
    // (#157 item 3). Priced by FuelReachability against the current well so the desk always answers the
    // question — with directions, not a dead button. Cached on a coarse position/tank signature so the
    // (transfer-solving) assessment isn't recomputed every render while the captain parks in the desk.
    private string? _fuelDirLine;
    private string? _fuelDirSig;

    private string FuelDirectionsLine()
    {
        if (_ephemeris is null || _simulator is null)
        {
            return "Dock at a station or a haven to take on reaction mass.";
        }

        if (CurrentWellBodyId() is not { } well)
        {
            return "No fuel port in range — steer for a planet's stations or a haven, then dock to fill up.";
        }

        // Round the position to ~1e9 m so the pump assessment recomputes only when the ship really moves.
        string sig = $"{well}|{_reactionMassPulses}|{ReactionMassCapacity}|{(long)(_ship.Position.X / 1e9)}|{(long)(_ship.Position.Y / 1e9)}";
        if (sig != _fuelDirSig)
        {
            _fuelDirSig = sig;
            try
            {
                FuelReachability.Assessment a = FuelReachability.Assess(
                    _simulator, _ephemeris, _ship, _reactionMassPulses, ReactionMassCapacity, well);
                _fuelDirLine = a.NearestDepotBodyId is { } id && a.NearestDepotPulses != int.MaxValue
                    ? $"Nearest pump: {BodyName(id)}, about {a.NearestDepotPulses} p away — plot a course there and dock to fill up."
                    : "No pump reachable from here — burn back toward a planet's stations or a haven to refuel.";
            }
            catch
            {
                _fuelDirLine = "Dock at a station or a haven to take on reaction mass.";
            }
        }

        return _fuelDirLine ?? "Dock at a station or a haven to take on reaction mass.";
    }

    // The heliocentric well the ship sits in: the top-level body (a planet — direct child of the Sun)
    // above the nearest body, whose station/haven children are the pumps FuelReachability prices against.
    private string? CurrentWellBodyId()
    {
        if (_ephemeris is null || _nearestBody is null)
        {
            return null;
        }

        CelestialBody body = _nearestBody;
        while (body.ParentId is { } parentId)
        {
            CelestialBody? parent = null;
            foreach (CelestialBody candidate in _ephemeris.Bodies)
            {
                if (candidate.Id == parentId) { parent = candidate; break; }
            }

            if (parent is null || parent.ParentId is null)
            {
                break; // parent is the parentless root (the Sun) — `body` is the planet-level well
            }

            body = parent;
        }

        return body.Id;
    }

    private void BuyUpgrade(string track)
    {
        if (track == "telescope" && _telescopeLevel >= 3)
        {
            return; // MaxTracks cap: 1 + 3 upgrades = 4 telescopes
        }

        int level = track switch
        {
            "mass" => _massLevel,
            "sensor" => _sensorLevel,
            "hold" => _holdLevel,
            "telescope" => _telescopeLevel,
            _ => 0,
        };
        int price = UpgradePrice(level);
        if (_credits < price)
        {
            return;
        }

        _credits -= price;
        switch (track)
        {
            case "mass":
                _massLevel++;
                break;
            case "sensor":
                _sensorLevel++;
                RebuildSensor();
                _predictionDirty = true;
                break;
            case "hold":
                _holdLevel++;
                break;
            case "telescope":
                _telescopeLevel++;
                break;
        }

        ShowPulseMessage("Upgrade installed");
        int stepBeforeUpgrade = _tutorialStep;
        AdvanceTutorial(5); // step 6: first upgrade
        if (stepBeforeUpgrade != _tutorialStep && _tutorialStep == StepSelectFreighter)
        {
            SeedSecondHuntTarget(); // the upgrade that ENDS the first hunt spawns the gun lesson's prey
        }
    }

    // #266: the adrift rescue. The tug tops the tank; the fee is the whole hold, confiscated. The terms
    // are shown BEFORE this fires — see the rescue pop-up (AcceptRescue is its only caller now).
    private void RequestRescue()
    {
        _reactionMassPulses = ReactionMassCapacity;
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();
        ShowPulseMessage("Rescue fee: all cargo confiscated");
    }

    // The confiscation manifest the offer enumerates — the live hold, by class, with hot flagged (#266).
    private IReadOnlyList<RescueOffer.FeeLine> RescueFeeLines() =>
        CargoManifest()
            .Select(e => new RescueOffer.FeeLine(e.CargoClass, e.Units, e.Value, IsHotClass(e.CargoClass)))
            .ToList();

    private void AcceptRescue()
    {
        RequestRescue();
        _showRescueOffer = false;
        _shipAlerts.Clear(AlertKind.Adrift); // the tow has us; clear the founding alert immediately
    }

    // #175: the live "what to do to deliver" line for a cargo run, read straight off ship state so the
    // quest card and the nav-target box never lie about the next action. A STATION haven delivers on
    // ⚓ Dock inside the dock envelope; a MOON haven delivers by parking in its orbit (the lie-low
    // precedent). Reuses the DockRule envelope constants — the numbers are never re-literal'd here.
    private string CargoNextAction(CelestialBody dest)
    {
        if (IsDockableHaven(dest))
        {
            Vector2d pos = _ephemeris!.Position(dest.Id, SimTime);
            const double h = 1.0;
            Vector2d vel = (_ephemeris.Position(dest.Id, SimTime + h) - _ephemeris.Position(dest.Id, SimTime - h)) / (2 * h);
            return DockRule.InEnvelope(_ship, pos, vel, dest.BodyRadius)
                ? "in the envelope — hit ⚓ Dock to deliver 📦"
                : $"get to {dest.Name} — coast within {DockReachMeters / 1000:N0} km, ≤{DockMatchSpeedMps / 1000:N0} km/s to clamp on";
        }

        // A moon haven: no dock — parking in orbit IS the berth (IsHiddenAtHaven's lie-low rule).
        return IsBoundAtMoonHaven(dest)
            ? "in orbit — delivered ✓"
            : $"enter orbit at {dest.Name} to deliver 📦";
    }

    // The ledger's "→ dark web" link: switch to Comms and select the dark-web node (the market view).
    private void OpenDarkWebFromLedger()
    {
        SwitchDesk(ShipDesk.Comms);
        _commsSelectedId = "darkweb";
    }

    // ---- PR-6: the dark space web ----

    /// <summary>The body the player is currently orbiting (bound, M20 sense) or docked at — null
    /// if neither, in which case the dark web has nowhere to set up shop.</summary>
    private CelestialBody? DarkWebCurrentBody()
    {
        if (_ephemeris is null)
        {
            return null;
        }

        if (_docked && _dockBodyId is not null)
        {
            foreach (CelestialBody b in _ephemeris.Bodies)
            {
                if (b.Id == _dockBodyId) return b;
            }
        }

        if (_nearestBody is { ParentId: not null } nb)
        {
            CelestialBody? parent = null;
            foreach (CelestialBody candidate in _ephemeris.Bodies)
            {
                if (candidate.Id == nb.ParentId) { parent = candidate; break; }
            }

            if (parent is not null)
            {
                double hill = OrbitRule.HillRadius(nb, parent.Mu);
                if (OrbitRule.IsBound(_ship, _nearestBodyPosition, _nearestBodyVelocity, nb, hill))
                {
                    return nb;
                }
            }
        }

        return null;
    }

    private bool DarkWebCanTrade()
    {
        if (DarkWebCurrentBody() is not { } body)
        {
            return false;
        }

        return IntelMarket.CanTradeIntelAt(body, _ephemeris!.Position(body.Id, SimTime).Length);
    }

    private string DarkWebDisabledReason()
    {
        if (DarkWebCurrentBody() is not { } body)
        {
            return "Not orbiting or docked anywhere — get to a haven or a far trading post first.";
        }

        return IntelMarket.CanTradeIntelAt(body, _ephemeris!.Position(body.Id, SimTime).Length)
            ? ""
            : $"{body.Name} doesn't deal in stolen timetables — try a haven or a station past Mars.";
    }

    private double DarkWebDistanceFromEarth()
    {
        if (_ephemeris is null || DarkWebCurrentBody() is not { } body)
        {
            return 0;
        }

        Vector2d earth = _ephemeris.Position("earth", SimTime);
        Vector2d here = _ephemeris.Position(body.Id, SimTime);
        return (here - earth).Length;
    }

    // Thin, read-only projection of the off-the-books NPCs the market knows about — same
    // philosophy as TrackingCandidates(): the dark web component never sees Map.razor's own
    // NpcState type.
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.MarketShip> DarkWebMarketShips()
    {
        var ships = new List<SpaceSails.Client.Pages.Stations.DarkWeb.MarketShip>();
        foreach (NpcState npc in _npcStates)
        {
            if (!npc.Ship.PublishesTimetable && npc.Active && !npc.Arrived)
            {
                ships.Add(new SpaceSails.Client.Pages.Stations.DarkWeb.MarketShip(
                    npc.Ship.Id, npc.Ship.Callsign, npc.Ship.CargoClass, npc.Ship.CargoUnits, RouteLabel(npc.Ship)));
            }
        }

        return ships;
    }

    // PR-WIRE — the wire-capable contacts for the dark-web favor-bank panel: every contact we have
    // history with whose character sheet banks over the wire (ruling 6). In-person-only contacts (the
    // hermit, the Magpie) are excluded — you bank those across their table, not the dark web.
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.WireContact> DarkWebWireContacts()
    {
        var rows = new List<SpaceSails.Client.Pages.Stations.DarkWeb.WireContact>();
        foreach ((string id, ContactHistory h) in _contacts.Entries)
        {
            ContactSheet sheet = ContactSheets.For(id);
            if (!sheet.CanWire)
            {
                continue;
            }
            rows.Add(new SpaceSails.Client.Pages.Stations.DarkWeb.WireContact(id, sheet.DisplayName, h.CreditBalance));
        }
        rows.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
        return rows;
    }

    // Thin, read-only projection of the tracking-post ledger + live NPC state, for the dark
    // web's sell/tight-beam/laser-ranging sections.
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.TrackedShipInfo> DarkWebTrackedShips()
    {
        var list = new List<SpaceSails.Client.Pages.Stations.DarkWeb.TrackedShipInfo>();
        if (_trackingPost is null)
        {
            return list;
        }

        foreach (TrackedTarget entry in _trackingPost.Entries)
        {
            NpcState? npc = null;
            foreach (NpcState candidate in _npcStates)
            {
                if (candidate.Ship.Id == entry.ShipId) { npc = candidate; break; }
            }

            Vector2d position = npc?.State.Position ?? entry.LastObservation.Position;
            Vector2d velocity = npc?.State.Velocity ?? entry.LastObservation.Velocity;
            list.Add(new SpaceSails.Client.Pages.Stations.DarkWeb.TrackedShipInfo(
                entry.ShipId,
                npc?.Ship.Callsign ?? entry.ShipId,
                npc?.Ship.CargoClass ?? "Unknown",
                npc?.Ship.CargoUnits ?? 0,
                entry.EffectiveQuality(SimTime),
                position,
                velocity,
                npc?.Ship.PublishesTimetable ?? true,
                npc is not null ? BodyName(npc.Ship.DestinationId) : "unknown"));
        }

        return list;
    }

    // ---- PR-5: orbital commerce — trade from orbit or course-matched with drones ----

    // The context body Local Space shows contacts for: the body the ship is actually bound to, or
    // (per the owner's "orbiting/near a body" phrasing) whatever body is nearest when not bound —
    // so the panel still has something useful to show while just cruising close to a bus stop.
    private string? LocalSpaceBodyId => _orbitedBodyId ?? _nearestBody?.Id;

    // Thin, read-only projection of the live NPC/depot list for CommerceRule — mirrors
    // TrackingCandidates()'s role for the tracking post.
    private IReadOnlyList<CommerceRule.LocalShip> LocalShips()
    {
        var ships = new List<CommerceRule.LocalShip>(_npcStates.Length);
        foreach (NpcState npc in _npcStates)
        {
            // StepNpcs's despawn check flags a depot "Arrived" the instant it's created (its
            // destination IS its own body, always within despawn tolerance) — correct for the
            // traffic board's status column, wrong here: a depot is a perpetual fixture, never
            // actually gone. Only exclude Arrived for ships that can genuinely leave.
            if (npc.Arrived && npc.Ship.DepotBodyId is null)
            {
                continue;
            }

            ships.Add(new CommerceRule.LocalShip(npc.Ship.Id, npc.Ship.Callsign, npc.State, npc.Ship.DepotBodyId,
                npc.Ship.CargoClass, RemainingStock(npc)));
        }

        return ships;
    }

    /// <summary>What a post still has for sale: its manifest minus what the player has bought,
    /// zero once boarded (plundered empty is empty).</summary>
    private static int RemainingStock(NpcState npc) =>
        npc.Boarded ? 0 : Math.Max(0, npc.Ship.CargoUnits - npc.CargoSoldToPlayer);

    private IReadOnlyList<CommerceRule.LocalContact> LocalContacts()
    {
        if (_ephemeris is null)
        {
            return [];
        }

        // M29: what's "here" (keyed to the context body) UNION everything the shuttles could
        // reach — a slow pass millions of km from a station is a real trading opportunity now.
        var contacts = new List<CommerceRule.LocalContact>();
        var seen = new HashSet<string>();
        if (LocalSpaceBodyId is { } bodyId)
        {
            foreach (CommerceRule.LocalContact c in CommerceRule.ContactsAt(_ephemeris, LocalShips(), SimTime, bodyId))
            {
                if (seen.Add(c.Id))
                {
                    contacts.Add(c);
                }
            }
        }

        foreach (CommerceRule.LocalContact c in CommerceRule.ContactsWithinShuttleRange(_ephemeris, LocalShips(), SimTime, _ship))
        {
            if (seen.Add(c.Id))
            {
                contacts.Add(c);
            }
        }

        return contacts;
    }

    // M29: the tier the deal would move by, for a given contact — chip, badge, button and
    // break-off all agree because they all ask this.
    private CommerceRule.TradeMode TradeModeFor(CommerceRule.LocalContact contact)
    {
        string? partnerOrbitBodyId = contact.Kind == CommerceRule.LocalContactKind.Ship ? null : LocalSpaceBodyId;
        return CommerceRule.Classify(_ship, contact.Position, contact.Velocity, _orbitedBodyId, partnerOrbitBodyId);
    }

    private void StartLocalTrade(string contactId)
    {
        if (_localTradeTargetId is not null)
        {
            return;
        }

        if (_cargoValue <= 0)
        {
            // The button used to silently no-op here — never a mute click again.
            ShowPulseMessage("Hold empty — nothing to sell. Buy from a post or board a prize first.");
            return;
        }

        _localTradeTargetId = contactId;
        _localTradeIsBuy = false;
        _localTradeProgress = 0;
        CommerceRule.TradeMode mode = CommerceRule.TradeMode.DroneMatch;
        foreach (CommerceRule.LocalContact c in LocalContacts())
        {
            if (c.Id == contactId) { mode = TradeModeFor(c); break; }
        }

        _localTradeMessage = mode == CommerceRule.TradeMode.Shuttle
            ? "Shuttles away — long corridor, cargo transfer in progress 🚀"
            : "Drones away — cargo transfer in progress";
    }

    // ---- Buying (owner: "How do I buy anything from Earth Depot?"): the honest other half.
    // Same transfer clock, envelope rules and break-off as selling; the units and the price
    // are locked when the shuttles leave, credits change hands when they're back. ----
    private bool _localTradeIsBuy;
    private int _localBuyUnits;
    private int _localBuyCostCr;
    private string _localBuyClass = "";

    private void StartLocalBuy(string contactId)
    {
        if (_localTradeTargetId is not null)
        {
            return;
        }

        CommerceRule.LocalContact? contact = null;
        foreach (CommerceRule.LocalContact c in LocalContacts())
        {
            if (c.Id == contactId) { contact = c; break; }
        }

        if (contact is not { CargoClass: { } cargoClass, CargoUnits: > 0 } post)
        {
            ShowPulseMessage("Nothing left on that post's manifest");
            return;
        }

        CommerceRule.TradeMode mode = TradeModeFor(post);
        if (mode == CommerceRule.TradeMode.None)
        {
            ShowPulseMessage($"Out of reach — close within {FormatDistance(CommerceRule.ShuttleRangeMeters)} under {CommerceRule.ShuttleMaxRelativeSpeed / 1000:F0} km/s rel");
            return;
        }

        int units = CommerceRule.MaxBuyableUnits(
            mode, post.CargoUnits, CargoCapacity - _cargoUnits, _credits, CargoMarket.UnitValue(cargoClass));
        if (units <= 0)
        {
            ShowPulseMessage(CargoCapacity - _cargoUnits <= 0
                ? "Hold full — sell or fence something first"
                : "Not enough credits for even one unit plus the ferry fee");
            return;
        }

        _localTradeTargetId = contactId;
        _localTradeIsBuy = true;
        _localTradeProgress = 0;
        _localBuyUnits = units;
        _localBuyClass = cargoClass;
        _localBuyCostCr = CommerceRule.BuyCostCr(mode, units, CargoMarket.UnitValue(cargoClass));
        _localTradeMessage = mode == CommerceRule.TradeMode.Shuttle
            ? $"Shuttles away — buying {units}u {cargoClass} for {_localBuyCostCr:N0} cr 🚀"
            : $"{(mode == CommerceRule.TradeMode.SameOrbit ? "Dockside crew" : "Drones")} loading {units}u {cargoClass} — {_localBuyCostCr:N0} cr";
    }

    private void CancelLocalTrade(string message)
    {
        _localTradeTargetId = null;
        _localTradeProgress = 0;
        _localTradeMessage = message;
    }

    // Drone transfer progress accrues in REAL time (M14's boarding-shuttle pattern) so warping
    // doesn't fast-forward a transfer. Breaks off — progress lost — the moment the envelope
    // (CommerceRule.CanTrade) stops holding, e.g. the target ship burns away mid-transfer.
    private void UpdateLocalTrade(double dtRealSeconds)
    {
        if (_localTradeTargetId is not { } targetId)
        {
            return;
        }

        if (_ephemeris is null || (!_localTradeIsBuy && _cargoValue <= 0))
        {
            CancelLocalTrade("Drone transfer aborted");
            return;
        }

        CommerceRule.LocalContact? target = null;
        foreach (CommerceRule.LocalContact c in LocalContacts())
        {
            if (c.Id == targetId) { target = c; break; }
        }

        if (target is not { } contact || (contact.Actions & CommerceRule.ActionKind.Trade) == 0)
        {
            CancelLocalTrade("Drones lost the contact — transfer aborted");
            return;
        }

        CommerceRule.TradeMode mode = TradeModeFor(contact);
        if (mode == CommerceRule.TradeMode.None)
        {
            CancelLocalTrade("Envelope lost — shuttles and drones recalled, transfer aborted");
            return;
        }

        bool shuttle = mode == CommerceRule.TradeMode.Shuttle;
        double relSpeed = (_ship.Velocity - contact.Velocity).Length;
        double distance = (_ship.Position - contact.Position).Length;
        int transferUnits = _localTradeIsBuy ? _localBuyUnits : _cargoUnits;
        double seconds = CommerceRule.TransferSeconds(mode, relSpeed, distance, transferUnits);
        _localTradeProgress += Math.Clamp(dtRealSeconds, 0, 0.1) / seconds;

        if (_localTradeProgress >= 1)
        {
            if (_localTradeIsBuy)
            {
                CompleteLocalBuy(contact, mode);
            }
            else
            {
                CompleteLocalSell(mode, shuttle);
            }

            _localTradeTargetId = null;
            _localTradeProgress = 0;
            RendererInterop.PlayCue("board");
        }
        else
        {
            _localTradeMessage = _localTradeIsBuy
                ? $"{(shuttle ? "Shuttles hauling" : "Loading")} {_localBuyUnits}u {_localBuyClass} — {(int)(_localTradeProgress * 100)}%"
                : $"{(shuttle ? "Shuttles flying the corridor" : "Drones ferrying")} — {(int)(_localTradeProgress * 100)}%";
        }
    }

    private void CompleteLocalSell(CommerceRule.TradeMode mode, bool shuttle)
    {
        int units = _cargoUnits;
        int payout = CommerceRule.SellPayoutCr(mode, units, _cargoValue);
        int fee = CommerceRule.TransferFeeCr(mode, units);
        _credits += payout;
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();
        _localTradeMessage = fee > 0
            ? $"{(shuttle ? "Shuttles" : "Drones")} delivered {units} units — {payout:N0} cr after the {fee:N0} cr ferry fee"
            : $"Delivered {units} units for {payout:N0} credits";
        AdvanceTutorial(4); // step 5: first sale (same milestone the dock's SellCargo hits)
        AdvanceTutorial(StepSellHe3); // second hunt, step 6: fencing the He3 closes the tutorial
    }

    private void CompleteLocalBuy(CommerceRule.LocalContact contact, CommerceRule.TradeMode mode)
    {
        // Re-check at handover: credits may have been spent and the manifest may have been
        // plundered while the shuttles were flying. Take what is still takeable, pay for that.
        NpcState? seller = FindNpc(contact.Id);
        int stock = seller is null ? contact.CargoUnits : RemainingStock(seller);
        int units = CommerceRule.MaxBuyableUnits(
            mode, Math.Min(stock, _localBuyUnits), CargoCapacity - _cargoUnits, _credits,
            CargoMarket.UnitValue(_localBuyClass));
        if (units <= 0)
        {
            _localTradeMessage = "Shuttles returned empty — the deal fell through at handover";
            return;
        }

        int cost = CommerceRule.BuyCostCr(mode, units, CargoMarket.UnitValue(_localBuyClass));
        _credits -= cost;
        _cargoUnits += units;
        _cargoValue += units * CargoMarket.UnitValue(_localBuyClass);
        _cargoByClass[_localBuyClass] = _cargoByClass.GetValueOrDefault(_localBuyClass) + units;
        if (seller is not null)
        {
            seller.CargoSoldToPlayer += units;
        }

        int fee = CommerceRule.TransferFeeCr(mode, units);
        _localTradeMessage = fee > 0
            ? $"Bought {units}u {_localBuyClass} for {cost:N0} cr ({fee:N0} cr of it ferry fee)"
            : $"Bought {units}u {_localBuyClass} dockside for {cost:N0} cr — no ferry fee";
    }
}
