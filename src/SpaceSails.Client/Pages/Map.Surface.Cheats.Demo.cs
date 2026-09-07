using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// THE RIGS — the boots that do not merely stand the captain somewhere but set a scene up around him: the
/// designate demo (a shutter, a sentry one round short of a hasp, and a find loose in the pocket), the
/// goods car, the far side of the green, and the monolith's attentive window.
///
/// <para>The shutter is the shutter, the rounds are ordinary rounds, and the six that come off the drum
/// are the six the law charges. A cheat that shows a tester a different scene is worse than no cheat at
/// all.</para>
///
/// <para>Split out of <c>Map.Surface.Cheats.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    private void RigTheDesignateDemoIfAsked(SurfaceExcursion ex)
    {
        if (!_designateCheat)
        {
            return;
        }

        SurfaceBot? gun = ex.Bots.FirstOrDefault(b => !SurfaceArrival.IsDoorSentry(b.Unit));
        if (gun is not null)
        {
            // Where the [T] key would have put it: at the captain's own boots. Not a hand-picked spot two du
            // to one side — that could be inside the car's divider on some floors, and a cheat that stands a
            // machine in a wall is a cheat that playtests a world nobody can reach.
            gun.Deployed = true;
            gun.X = _avatarX;
            gun.Y = _avatarY;
            gun.AimX = gun.X;
            gun.AimY = gun.Y - 1;
            gun.Rounds = DesignateDemoRoundsInDrum;
            gun.AmmoId = Core.Ammunition.Issue.Id;
        }

        _satchel = [.. Core.Satchel.Add(_satchel,
            new Core.Satchel.Item(Core.Satchel.Kind.Rounds, Core.Ammunition.Issue.Id, DesignateDemoRoundsInPocket))];

        RebuildSurfaceDeck();
        ShowPulseMessage(
            $"🧪 DEV ?designate=1: {gun?.Unit ?? "your sentry"} is SET DOWN beside you reading "
            + $"{SentryBot.Readout(DesignateDemoRoundsInDrum)}, and there are {DesignateDemoRoundsInPocket} "
            + "loose rounds in your pocket. Press I over the bot to hand-load it, then 📻 Remote → "
            + $"{ShootTheLock.OpenLabel} and point it at the shutter.");
    }

    /// <summary>#803 QA · One short of a hasp, so the demo cannot be completed without hand-loading.</summary>
    private const int DesignateDemoRoundsInDrum = ShootTheLock.RoundsPerHasp - 1;

    /// <summary>#803 QA · A hut's find, in the pocket — the size <c>SurfaceOutpost.CacheRounds</c> deals in.</summary>
    private const int DesignateDemoRoundsInPocket = 12;

    private bool _designateCheat;

    /// <summary>#801 QA · <c>?goodscar=1</c> — booted at the SECOND CAR. Set in Map.Sim's cheat parse.</summary>
    private bool _goodsCarCheat;

    /// <summary>#801 QA · <c>?parkback=1</c> — booted on the gravel facing the BACK-OF-HOUSE doors. Set in
    /// Map.Sim's cheat parse.</summary>
    private bool _parkBackCheat;

    /// <summary>#801 QA · Stand the captain at the GOODS CAR, at the blind end of the main corridor.
    ///
    /// <para>On the car's own published doorstep (<see cref="UndergroundComplex.Shaft.Landing"/>), never on
    /// a coordinate measured off a screenshot — the two alcoves hang off opposite faces of the spine and a
    /// number typed here would be right for one of them and inside a wall for the other.</para></summary>
    private void StandAtTheGoodsCarIfAsked(SurfaceExcursion ex)
    {
        if (!_goodsCarCheat || ex.Floor >= 0)
        {
            return;
        }

        foreach (UndergroundComplex.Shaft car in
            UndergroundComplex.ShaftsOn(MoonSurface.ExpeditionField()))
        {
            if (car.Kind != UndergroundComplex.ShaftKind.Service)
            {
                continue;
            }
            (double cx, double cy) = car.Landing;
            StandCaptainAt(cx, cy, "you walk the corridor to its blind end and the other car is standing there");
            ShowPulseMessage(
                "🧪 DEV ?goodscar=1: THE SECOND CAR. Press E: it runs these four floors, it does not climb "
                + "out, and the cage is at the other end of the corridor.");
            return;
        }
    }

    /// <summary>#801 QA · Stand the captain on the gravel in front of the park's FAR wall, facing the
    /// back-of-house doors — the far side the owner said should not be the edge.</summary>
    private void StandBehindTheParkIfAsked(SurfaceExcursion ex)
    {
        if (!_parkBackCheat || ex.Floor >= 0)
        {
            return;
        }

        if (UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Park
            is not { } green || green.Rooms.Count == 0)
        {
            return;
        }

        // In front of the middle door, on the park's own side of it — the walk is a good ten du back from
        // this wall, so this is the promenade the masts stand on and not a bed.
        UndergroundComplex.BackRoom middle = green.Rooms[green.Rooms.Count / 2];
        double doorX = (middle.Door.X1 + middle.Door.X2) / 2.0;
        double inward = green.Contains(doorX, middle.Door.Y1 + 4.0) ? 4.0 : -4.0;
        StandCaptainAt(doorX, middle.Door.Y1 + inward,
            "you cross the gravel to the far wall, and it is a row of doors");
        ShowPulseMessage(
            "🧪 DEV ?parkback=1: THE FAR SIDE OF THE GREEN. Doors in the wall that used to be the horizon — "
            + "walk through one.");
    }

    // #649: /map?watchers=1 opens the monolith's attentive window and shortens the dwell to a couple of
    // seconds. It does NOT change what happens — the beat, the variant roll and the (zero) cost are the
    // ones a captain gets — because a cheat that shows you a different scene is worse than no cheat at all.
    private bool _watchersCheat;
}
