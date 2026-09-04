using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — #719's second way out.
public partial class Map
{
    // ── #719 · THE STAIR, AND WHY IT IS A PRESS AND NOT A PANEL ─────────────────────────────────────────
    //
    // Owner, 2026-08-05: "I wonder if there should be more than one way out of the lab, what if the elevator
    // needs maintenance break :-)" / "just stopping the elevator by remote of radio message would stop all
    // escape way too easy :-d" / "going up would use more air".
    //
    // The cage has a panel because a car has FLOORS: #600's whole fix was that the way out must never require
    // travelling further in, and the way you say that in a lift is a directory with SURFACE always on it. A
    // stair has no directory. It has one destination and it is the only one it has ever had, so the press IS
    // the climb — and a panel here would be an affordance with nothing behind it (#212), a list of one.
    //
    // IT GOES ONE WAY, which is the whole of how this ships without touching §13.5. A floor's door lets a
    // captain INTO the shaft and the shaft lets them out at the top and nowhere else — a real fire stair,
    // locked off from the stair side at every level but the discharge. Nothing can be walked DOWN it, so it
    // is not a second road past the SEALED row (#590), the ID CHECK band (#715) or a stop order's seal
    // (#1074 beat 1), and the one thing this game makes you earn stays earned. It is an escape route, not an
    // entrance. See UndergroundComplex.CarveStair, which carries the argument beside the geometry.

    /// <summary>#719 · Climb out. Core decides the price (<see cref="UndergroundComplex.ClimbAirSeconds"/>)
    /// and the arrival is the car's own arrival, minus the one sentence that names a car.
    ///
    /// <para>The tank is charged BEFORE the trip rather than after it, and that ordering is the design: the
    /// drain on the surface tick then sees the real number, so every threshold the suit already owns — the
    /// crossing line, the reserve cutting in, the low-air card, the suffocation — fires off a tank that has
    /// actually paid for the climb. A cost applied after the arrival would have been a cost the instruments
    /// find out about a frame late, which on the one resource this game kills people with is a frame too
    /// many.</para></summary>
    private void ClimbTheStairOut()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }

        // #801's lesson, said about a third machine: the SPOT decides. A press that threw the pressed console
        // away and asked Core about "the way out" would climb the stair from the cage's own doorstep.
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveStair })
        {
            return;
        }

        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        ex.AirSeconds = SuitAir.Drain(ex.AirSeconds, UndergroundComplex.ClimbAirSeconds(field, ex.Floor));
        RendererInterop.PlayCue("board");
        RideTheLiftTo(ex, 0, byStair: true);
    }
}
