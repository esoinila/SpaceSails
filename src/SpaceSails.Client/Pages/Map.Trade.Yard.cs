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

/// <summary>
/// THE YARD — the three upgrade tracks and their doubling price, the rescue that will come and fetch a
/// stranded ship for a fee, and the one line that tells the captain what his cargo still needs from him.
///
/// <para>Split out of <c>Map.Trade.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
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
}
