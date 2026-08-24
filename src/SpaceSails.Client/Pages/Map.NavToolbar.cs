using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: the Nav toolbar as a place a captain's eye lands — which button is the loud one, what it says
// it will do when you hover it, and what the camera is following.
//
// #962/#963, owner's playtest 2026-08-22: "Here the smallest button is the most important one… Plot. Plot
// should stand out among the buttons here. It is the heart of the navigation process. Now it is too well
// hidden." And, of the row it competes with: "Why have the scope enable/disable button competing for
// attention at the top of the screen… This should help us highlight the NAV button and the Add Burn buttons
// as the keys to navigation." The Scope toggle left the row (see Map.Deck.Scope), and what remains here is
// the other half: Plot is dressed as the primary action and SAYS what pressing it will do right now.
public partial class Map
{
    /// <summary>#963 — the hover on Plot, read off live state rather than a fixed sentence. The owner asked
    /// for exactly this: "We should have hover-on explanation of what we are plotting, when we press plot."
    /// A course is plotted TO somewhere, so when there is a somewhere, the tooltip names it.</summary>
    private string PlotButtonTip()
    {
        if (PlotMode)
        {
            return "Back to flying live — the sky starts moving again and the plotting table closes.";
        }

        string what = _destinationBodyId is { } destId
            ? $"Plot a course to {BodyName(destId)}"
            : "Plot a course";

        string aim = _destinationBodyId is null
            ? " Pick a destination on the map first and this aims at it."
            : "";

        return $"{what} — the sky pauses so you can scrub the path ahead, add burns at the scrub, and arm the arrival.{aim}";
    }

    /// <summary>#956 — the camera follows the DESTINATION instead of the ship. Owner: "Let's have a follow
    /// nav destination option here in addition to follow ship." Mutually exclusive with
    /// <see cref="FollowShip"/>: two follows are one fight over the same camera.</summary>
    private bool _followDest;

    /// <summary>Only offered when there IS a destination to follow — the button is disabled rather than
    /// hidden (#212: a control that vanishes teaches nothing), and it stands down on its own the moment the
    /// destination is cleared.</summary>
    private bool CanFollowDestination => _destinationBodyId is not null;

    private void ToggleFollowDest()
    {
        if (!CanFollowDestination)
        {
            return;
        }

        _followDest = !_followDest;
        if (_followDest)
        {
            FollowShip = false;
        }
    }

    /// <summary>Where the followed destination is right now, or null when nothing is set (or the world is
    /// not built yet). The one place the follow-dest camera reads.</summary>
    private Vector2d? FollowedDestinationPosition() =>
        _followDest && _destinationBodyId is { } id && _ephemeris is not null
            ? _ephemeris.Position(id, SimTime)
            : null;

    /// <summary>The tooltip on the Follow dest button — it names the body, or says why it is greyed.</summary>
    private string FollowDestTip() =>
        _destinationBodyId is { } id
            ? $"Keep the camera on {BodyName(id)} — the navigation target — instead of on the ship"
            : "No navigation target set — pick one on the map and the camera can ride it";

    /// <summary>#960 — the target dossier and the navigation-target panel both want the bottom centre, and in
    /// the owner's screenshot the dossier sat ON TOP of the nav panel's text and buttons. When both are up
    /// the dossier is RAISED to ride above the nav panel: the panel with the buttons keeps the floor.</summary>
    private bool DossierIsStacked => PlotMode && _destinationBodyId is not null && _activeDesk == ShipDesk.Nav;

    /// <summary>#960 — the dossier's own minimize, the same gesture the scope now has (owner: "option to
    /// minimize a window into a sugarcube tile and back would avoid the moving-windows can of worms").
    /// Per-session, like the scope's.
    ///
    /// <para>#997 · …and "the same gesture the scope now has" is finally one gesture rather than two that
    /// resemble each other: the OverlayShell owns it, this field is what the card is BOUND to, and the
    /// toggle that used to sit here is gone.</para></summary>
    private bool _dossierMinimized;

    /// <summary>#963 — the hover on a find-a-target row. The owner's question about the small glyph beside
    /// Ganymede ("Is there ground visitable at these places… what is the small symbol… it should have some
    /// kind of text pop-up?") is answered where he asked it: a landable row spells the mark out, everything
    /// else keeps the plain "jump here".</summary>
    private static string NavSearchRowTip(NavSearchRow row) =>
        row.Flavor.Contains("🛬", StringComparison.Ordinal)
            ? $"{row.Name} — 🛬 landable: a surface you can go down to. Ride the shuttle from the bay and walk the ground. " +
              "Click to jump the map here."
            : "Jump here — frame the map on it and centre the camera";
}
