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

// Subject: part of Map.Sim (#870 split; the header note lives in Map.Sim.cs) — the raw pointer, wheel and slider input that steers the map, and #875's deck click that is a place on the floor.
public partial class Map
{

    private bool _dragMoved;
    private double _downClientX, _downClientY;

    private int WarpSliderValue => (int)Math.Round(Math.Log10(Math.Clamp(Warp, 1, 10000)) * 25);

    private void OnWarpSliderInput(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int t))
        {
            SetWarp((int)Math.Round(Math.Pow(10, t / 25.0)));
        }
    }

    private void SetWarp(int level)
    {
        // #172: the captain's hand on the warp slider wins — cancel any skip (keep the level they chose).
        if (_skipActive)
        {
            _skipActive = false;
            LogAutopilotEvent("⏭ skip stopped — the captain set the warp");
        }
        PlotMode = false;
        Warp = level;
        Paused = false;
    }

    // Unpausing from inside plot mode is "press play": leave plotting properly (restores warp)
    // instead of running the sim with the plot card still open.
    private void TogglePause()
    {
        StopSkip(); // #172: pausing is the captain's hand — let go of any skip first.
        if (PlotMode && Paused)
        {
            ExitPlotMode();
            return;
        }

        Paused = !Paused;
    }

    private void ToggleFollow()
    {
        FollowShip = !FollowShip;
        if (FollowShip)
        {
            _followDest = false; // #956: one camera, one thing to follow
        }
    }

    private void OnWheel(WheelEventArgs e)
    {
        double factor = e.DeltaY > 0 ? 1.15 : 1 / 1.15;
        _camera.ZoomBy(factor, e.OffsetX, e.OffsetY);
    }

    // #237 — the wheel-free zoom: one REAL step per press (×1.6, vs the wheel's 1.15 crawl),
    // toward the viewport centre so the button never yanks the view sideways.
    private void ZoomStep(bool zoomIn) =>
        _camera.ZoomBy(zoomIn ? 1 / 1.6 : 1.6, _viewportWidth / 2.0, _viewportHeight / 2.0);

    private void OnPointerDown(PointerEventArgs e)
    {
        // #729 · ON THE WALKED DECK, A CLICK IS A PLACE ON THE FLOOR. The canvas underneath is the same
        // element the ecliptic uses, so without this the picker below would hit-test the click against
        // planets and contacts that are not on screen at all and open a body menu over a moon floor.
        //
        // #875 · The question here is only WHICH SURFACE THE POINTER IS OVER, never whether the captain may
        // walk — a click that lands on a floor belongs to the floor even when the legs are held, or the
        // escort would be answered by a planet menu opening over a corridor. What that click then does is
        // ClickToWalkAt's, through the one predicate the arrow keys ask.
        if (ADeckClickIsAPlaceOnTheFloor)
        {
            _suppressClickMenu = false; // no map menu can be open over a moon floor — never eat the first click
            _dragging = true;
            _dragMoved = false;
            _lastPointerX = e.ClientX;
            _lastPointerY = e.ClientY;
            _downClientX = e.ClientX;
            _downClientY = e.ClientY;
            return;
        }

        // A click that only dismisses an open menu must not immediately open the next one.
        _suppressClickMenu = _bodyMenuBody is not null || _shipMenuId is not null
            || _skyMenuWorld is not null || _pickMenu is not null;

        if (_bodyMenuBody is not null)
        {
            CloseBodyMenu(); // any click on the map dismisses an open planet menu
        }

        if (_shipMenuId is not null)
        {
            CloseShipMenu(); // same rule for the contact menu
        }

        if (_skyMenuWorld is not null)
        {
            CloseSkyMenu();
        }

        if (_pickMenu is not null)
        {
            ClosePickMenu();
        }

        if (TrySelectNodeAt(e.OffsetX, e.OffsetY))
        {
            return; // clicked a thrust node: select it, don't start a drag
        }

        // The unified picker: one candidate under the click acts directly (old behavior); a
        // stack of neighbors opens the chooser instead of silently taking the topmost.
        List<PickCandidate> picks = CollectPointCandidates(e.OffsetX, e.OffsetY, PickRadiusPx);
        if (picks.Count == 1)
        {
            OpenPickCandidateAt(picks[0], e.OffsetX, e.OffsetY);
            return;
        }

        if (picks.Count > 1)
        {
            OpenPickMenu(picks, e.OffsetX, e.OffsetY);
            return;
        }

        _dragging = true;
        _dragMoved = false;
        _lastPointerX = e.ClientX;
        _lastPointerY = e.ClientY;
        _downClientX = e.ClientX;
        _downClientY = e.ClientY;
    }

    private void OnPointerMove(PointerEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        double dx = e.ClientX - _lastPointerX;
        double dy = e.ClientY - _lastPointerY;
        _lastPointerX = e.ClientX;
        _lastPointerY = e.ClientY;
        if (Math.Abs(e.ClientX - _downClientX) + Math.Abs(e.ClientY - _downClientY) > 5)
        {
            _dragMoved = true; // a real pan, not a click with hand tremor
        }

        // In the top-down deck view the drag moves the DECK plan (its bow hides under the HUD
        // panel otherwise); in first person and on the map it pans the camera as before.
        if (_deckMode && !_fpMode)
        {
            _deckPanX += dx;
            _deckPanY += dy;
            return;
        }

        _camera.PanByPixels(dx, dy);
        FollowShip = false; // manual pan disengages follow-ship, same as most space-game maps.
        _followDest = false; // …and follow-destination with it (#956): a hand on the map outranks both.
    }

    private void OnPointerUp(PointerEventArgs e)
    {
        // SundaySecondPlan PR-C: on the Sensors desk, EMPTY sky answers a click too — but only
        // a genuine click (no pan movement, and not the click that dismissed another menu).
        bool click = _dragging && !_dragMoved && !_suppressClickMenu;
        _dragging = false;

        // #729 · …and a genuine click on that floor is a walk order — #875: on every walked view, always,
        // exactly where the arrow keys are. A DRAG is still a pan of the deck plan (OnPointerMove), so the
        // captain can shove the view around without setting off across the room.
        if (click && ADeckClickIsAPlaceOnTheFloor)
        {
            ClickToWalkAt(e.OffsetX, e.OffsetY);
            return;
        }

        if (!click || _activeDesk != ShipDesk.Sensors || _deckMode)
        {
            return;
        }

        // Near-miss forgiveness: gather what sits within the loose radius; the empty-sky scan joins the
        // chooser at the bottom.
        //
        // #953 · A WHOLE LANE IS NOT A THING YOU CAN PICK ANY MORE. It used to be — a click near a corridor
        // offered the corridor itself — and the owner's ruling is that this was never worth an entry in the
        // chooser: "At least the routes as a whole should not even be selectable, since they just colour the
        // page in that option. We could have the A⇒B pairs as selectable here instead. That ship lanes
        // feature needs re-design." The redesign is a separate ruling; what happens NOW is that the lane
        // stops answering clicks, so a click near one means the thing you were actually aiming at.
        List<PickCandidate> near = CollectPointCandidates(e.OffsetX, e.OffsetY, PickNearRadiusPx);
        if (near.Count == 0)
        {
            OpenSkyMenu(e.OffsetX, e.OffsetY);
            return;
        }

        near.Add(new PickCandidate('K', "", "scan this patch of sky", "🔭"));
        OpenPickMenu(near, e.OffsetX, e.OffsetY);
    }

    private void CenterShipOnMap()
    {
        FollowShip = true;
        _followDest = false; // #956: centring on the ship is the other follow standing down
    }
}
