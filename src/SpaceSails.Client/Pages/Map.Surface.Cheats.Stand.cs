using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// THE LAST LEG, WALKED FOR YOU — <c>?reevers=N</c>, and the four boots that put the captain in front of
/// the thing under test: a counter that takes orders (#756), a shelter (#728, the longest walk on the
/// ground, because shelters are seeded DEEP by design), the park, and the garden walk.
///
/// <para>Split out of <c>Map.Surface.Cheats.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // #458: how many Old Ones /map?reevers=N asks for on the first landing. 0 = the cheat is off. They are
    // roused in the DEEP and come to you (#461) — never set down on the landing pad, which read as the Old
    // Ones somehow knowing where the shuttle would touch down.
    private int _reeverAmbushCheat;

    /// <summary>#756 QA · <c>?counter=1</c> — the whole route to a counter that takes orders, booted. Set in
    /// Map.Sim's cheat parse; read here, where the last leg is walked.</summary>
    private bool _counterCheat;

    /// <summary>#774 QA · <c>?kit=1</c> — the field dossier assembles on the FIRST piece of somebody's kit,
    /// and with every saying it can carry. Set in Map.Sim's cheat parse; read in
    /// <see cref="AssembleSomebody"/>, which is the one place both gates are asked about.</summary>
    private bool _kitCheat;

    /// <summary>#756 QA · Stand the captain AT THE COUNTER when <c>?counter=1</c> asked for it.
    ///
    /// <para>The amenity's own published spot, which is the service side of the counter and the very square
    /// the console dot is drawn on — so this walks the captain to the fixture rather than to a coordinate
    /// somebody measured off a picture of it. Through <c>StandCaptainAt</c>, so the pad-crew net (#681) has
    /// its say if the hall ever carves a table onto that square.</para>
    ///
    /// <para>#827 · That spot is the counter's own <c>ServiceRun.StandX/StandY</c> now — published rather
    /// than implied, and in exactly the place it always was. What moved onto the desk is the [E] RUN, which
    /// is the desk's front face, and the rail the deck draws down it.</para></summary>
    private void StandAtTheCounterIfAsked(SurfaceExcursion ex)
    {
        if (!_counterCheat || ex.Floor >= 0)
        {
            return;
        }

        foreach (UndergroundComplex.Amenity a in
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
        {
            if (Core.Interior.CounterService.For(ex.Stop.Body.Id, a.Use) is not { } counter)
            {
                continue;
            }

            StandCaptainAt(a.X, a.Y, "you step up to the counter");

            // #756 · …and ?stool=1 walks the last, last leg: the card is opened and a stool is taken, so the
            // posture this issue is about is one URL away rather than one URL and two presses. Through the
            // very handlers [E] and the button reach, so what a tester lands in is what a captain gets —
            // including WHICH seat is free, which is the room's answer off the frozen watch and never the
            // cheat's own.
            if (_stoolCheat)
            {
                // #781 · Through Core's own fork, so ?stool=1&watch=2 seats a tester in front of the KEEP and
                // ?stool=1&watch=5 in front of the machine — which is the difference this issue is about, and
                // it must not be a thing only the real press path knows.
                OpenCounterService(Core.Interior.CounterService.OnWatch(counter, ex.CanteenWatch));
                TakeAStool();
                ShowPulseMessage(
                    "🧪 DEV ?stool=1: up on a stool at THE COUNTER, menu and all. Press Wait — and add "
                    + "?neighbour=1 to make the one beside you turn, or ?neighbour=0 to hear the silence.");
                return;
            }

            ShowPulseMessage(
                Core.Interior.TheKeep.KeptWatch(ex.CanteenWatch)
                    ? "🧪 DEV ?counter=1: THE COUNTER, in reach, on a watch somebody is working it. Press E "
                      + "to open the card — then Hear a rumour, and come back on a later watch."
                    : "🧪 DEV ?counter=1: THE COUNTER, in reach, on a dead watch — it serves itself. Press E "
                      + "to open the card, and add ?watch=2 to find somebody behind it.");
            return;
        }
    }

    /// <summary>#728 QA · <c>?shelter=1</c> — the boots set down at a shelter's door. Set in Map.Sim's cheat
    /// parse; read in <see cref="StandAtTheShelterIfAsked"/>, the last leg of the landing.</summary>
    private bool _shelterCheat;

    /// <summary>#728 QA · <c>?mags=N</c> — what each sentry is holding as it comes down the tube. Set in
    /// Map.Sim's cheat parse; applied in <see cref="BeginSurfaceExcursion"/> at the one place a magazine
    /// crosses into an excursion, and never later — the readout, the receipts and both of the locker's
    /// refusals all read this number, and a cheat that wrote it afterwards would show a tester one captain in
    /// the instrument and a different one at the press.</summary>
    private int? _magazineCheat;

    /// <summary>#728 QA · Stand the captain at a SHELTER when <c>?shelter=1</c> asked for it.
    ///
    /// <para>A pace outside the door of the first shelter in the site's own stable list, facing it — the same
    /// ruling as <c>?secretlab=1</c>'s doorstep drop. Not inside: the proximity cycle, the arrival line and
    /// the pressure crossing are all part of what a tester came to look at, and a drop into the drum would
    /// skip every one of them. The door's spot is asked of the building
    /// (<c>SurfaceStructure.Build</c>) rather than measured off a picture of it, and the step outward is
    /// taken along the door's own outward normal, so a seeded angle cannot put the boots in a wall.</para></summary>
    private void StandAtTheShelterIfAsked(SurfaceExcursion ex)
    {
        if (!_shelterCheat || ex.Floor < 0)
        {
            return;
        }

        foreach ((ShelterSpot _, SurfaceStructure.Spec shelter) in SheltersInReach(ex))
        {
            foreach (SurfaceStructure.Doorway door in SurfaceStructure.Build(shelter).Doorways)
            {
                double outX = door.CentreX - shelter.CentreX, outY = door.CentreY - shelter.CentreY;
                double reach = Math.Sqrt((outX * outX) + (outY * outY));
                if (reach <= 1e-6)
                {
                    continue;
                }

                const double APaceClear = 4.0;
                ShowPulseMessage(
                    "🧪 DEV ?shelter=1: set down at a SHELTER. Walk in — the rack fills your tank, the "
                    + "press fills your magazines, and the readout under the tracker says what is in them.");
                StandCaptainAt(
                    door.CentreX + (outX / reach * APaceClear),
                    door.CentreY + (outY / reach * APaceClear),
                    "the shuttle sets you down at the shelter's door");
                return;
            }
        }

        ShowPulseMessage("🧪 DEV ?shelter=1: this ground has no shelter with a door to stand at.");
    }

    /// <summary>#759 QA · <c>?park=1</c> — the route to the park, booted. Set in Map.Sim's cheat parse.</summary>
    private bool _parkCheat;

    /// <summary>#759 QA · Stand the captain INSIDE THE PARK when <c>?park=1</c> asked for it.
    ///
    /// <para>On the park's own published plate spot — just inside the gate, looking down the first bend of
    /// the gravel — rather than at a coordinate somebody read off a screenshot. The attendance note fires on
    /// the very next tick, which is the point of the row.</para></summary>
    private void StandInTheParkIfAsked(SurfaceExcursion ex)
    {
        if (!_parkCheat || ex.Floor >= 0)
        {
            return;
        }

        if (UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Park
            is not { } green)
        {
            return;
        }

        // #793 · …and ?park=1&spread=1 goes one leg further — onto a BENCH, with three finds in the sleeve
        // and the whole plank to yourself. Through the same handler [E] reaches, at one of the room's own
        // benches: a dev row that assembled its own sitting would demonstrate a bench that does not ship.
        if (SitOnAFreeBenchIfAsked(in green))
        {
            return;
        }

        ShowPulseMessage(
            "🧪 DEV ?park=1: THE PARK. Green underfoot, the window wall back to the bar, beds and benches "
            + "down the curve — press E at one to SIT DOWN (#793).");
        StandCaptainAt(green.X, green.Y, "you step through the gate onto the gravel");
    }

    /// <summary>#775 QA · <c>?frontdoor=1</c> — booted on the MAIN CORRIDOR, at the hall's own entrance.
    /// Set in Map.Sim's cheat parse.</summary>
    private bool _frontDoorCheat;

    /// <summary>#775 QA · <c>?freight=1</c> — booted at the GOODS HOIST. Set in Map.Sim's cheat parse.</summary>
    private bool _freightCheat;

    /// <summary>#775 QA · <c>?parkwalk=1</c> — booted on the main corridor at the mouth of the garden
    /// walk. Set in Map.Sim's cheat parse.</summary>
    private bool _parkWalkCheat;

    /// <summary>#775 QA · Stand the captain on the SPINE, at the mouth of a gate down through the near band.
    ///
    /// <para>#813 · This used to hunt for <i>the one gate no rib stands on</i>, because that was what a
    /// garden walk WAS: a passage cut off the spine on the one column the seeded ribs had left free. The
    /// Manhattan carve took the seeding away — a rib on B1 runs DOWN only if it is one of the block's own
    /// gate columns — so "the gate no rib stands on" now selects NOTHING, and a row that selects nothing
    /// leaves the captain wherever the lift left them and tells nobody. That is the sharpest shape a dev
    /// row goes wrong in: the code still runs, the URL still parses, and the loop simply never fires.</para>
    ///
    /// <para>What it asks for now is the LAW rather than the accident: a gate in the park's NEAR wall — one
    /// that is horizontal and stands on the park's own near line (<c>green.Y1</c>), which is exactly the
    /// definition of "you reach it by walking off the spine" — taken off the room's published
    /// <see cref="UndergroundComplex.Park.Ways"/> list, never off a coordinate measured from a picture. The
    /// tester still starts on the main corridor, because the feature is a CROSSING: walk down, over the
    /// gravel, and out of any one of the other five.</para></summary>
    private void StandAtTheGardenWalkIfAsked(SurfaceExcursion ex)
    {
        if (!_parkWalkCheat || ex.Floor >= 0)
        {
            return;
        }

        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, field);
        if (floor.Park is not { } green)
        {
            return;
        }
        (_, double shaftY) = UndergroundComplex.ShaftAt(field);

        foreach (SurfaceLayout.Doorway gate in green.Ways)
        {
            // A NEAR gate. The two on the back street are horizontal too but stand on the park's FAR line,
            // and the west and east gates are vertical — none of the three is a thing you find by walking
            // the spine, which is the leg this row exists to start.
            if (Math.Abs(gate.Y1 - gate.Y2) > 0.001 || Math.Abs(gate.Y1 - green.Y1) > 0.001)
            {
                continue;
            }

            // Just inside the spine, at the gate column's mouth: the corridor face is CorridorHalf off the
            // shaft line, so a step short of it is a captain standing in the main corridor looking down it.
            double gx = (gate.X1 + gate.X2) / 2.0;
            double standY = shaftY + (green.Y1 < shaftY ? -1.5 : 1.5);
            StandCaptainAt(gx, standY, "you stop on the main corridor at the mouth of the walk");
            ShowPulseMessage(
                "🧪 DEV ?parkwalk=1: a GATE THROUGH THE BLOCK, off the main corridor. Walk down it, cross "
                + "the gravel, and come out somewhere else entirely — the green is the middle of a city "
                + "block now and it is crossed from all FOUR sides: two gates off the spine, two off the "
                + "back street, and one through each end of the block.");
            return;
        }
    }
}
