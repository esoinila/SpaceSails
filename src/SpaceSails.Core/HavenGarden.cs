using System;
using System.Collections.Generic;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #1332 B · <b>THE GARDEN BEHIND GLASS — THE WORDS.</b>
///
/// <para>Owner, 2026-09-29: <i>"the little garden on space ports could be used to produce salad, coffee etc.
/// comforts for levels sufficient for the restaurant."</i> Every haven with a bar grows one small pressurised
/// green off its concourse: four beds and a bench, no sky, no walker, nothing to take. It exists so that story
/// can plant a meeting there later (#1332 C, #1062, #1202), and so the station reads as a place people
/// live.</para>
///
/// <para>Everything a captain can read about it is here and nowhere else — Fable canon, verbatim; crews do not
/// write canon. WHICH havens have one and WHERE it stands is the client's (<c>HavenInterior</c>'s geometry):
/// this file knows nothing about any station except whether its bar has a board, because the menu line is
/// chalked on the board and a counter with no kitchen behind it has nothing to say about salad.</para>
/// </summary>
public static class HavenGarden
{
    /// <summary>#1332 B · <b>THE ROOM'S ONE LABEL</b>, in the plate idiom — a room plate on the concourse level,
    /// exactly as <see cref="ObservationWalk.Plate"/> is, and never a second floor label. Fable canon,
    /// verbatim.</summary>
    public const string Plate = "GARDEN · GROWERS ONLY AFTER SECOND WATCH";

    /// <summary>#1332 B · The first bed from the door. Fable canon, verbatim.</summary>
    public const string LettucePlate = "LETTUCE · 14 DAYS";

    /// <summary>#1332 B · The second. Fable canon, verbatim.</summary>
    public const string CoffeePlate = "COFFEE · DO NOT PICK";

    /// <summary>#1332 B · The third. Fable canon, verbatim.</summary>
    public const string BasilPlate = "BASIL";

    /// <summary>#1332 B · The fourth, furthest from the door. Fable canon, verbatim.</summary>
    public const string TomatoPlate = "TOMATO · STAKED";

    /// <summary>#1332 B · <b>THE FOUR BEDS, IN THIS ORDER FROM THE DOOR.</b> The order is canon too, so it is a
    /// list and never four fields a builder could lay in whichever order it liked. The beds are PLATES, not
    /// consoles (Kosh): nothing here is pressed, picked or explained.</summary>
    public static IReadOnlyList<string> BedPlates { get; } = [LettucePlate, CoffeePlate, BasilPlate, TomatoPlate];

    /// <summary>#1332 B · <b>THE BAR'S MENU LINE</b> — one sentence added to the board where the house line
    /// lives, per haven, verbatim. It is the garden's whole reason to exist, stated where a customer reads it
    /// and nowhere else.</summary>
    public const string MenuLine = "Salad from the garden. Coffee when the coffee is ready.";

    /// <summary>#1332 B · <b>THE FIRST TIME THE CAPTAIN ENTERS THE GARDEN AT A HAVEN</b>, told once — a pulse,
    /// on a free slot, never again at that station, filed nowhere. Fable canon, verbatim.</summary>
    public const string FirstVisitLine = "Warm, wet, and quiet. Somebody comes here on purpose.";

    /// <summary>#1332 · <b>SITTING DOWN ON THE GARDEN'S BENCH</b> — the scene's opening, told as the captain takes
    /// the plank. The park's own sitting speaks of gravel and a run of walk, and the garden has neither. Fable
    /// canon, verbatim.</summary>
    public const string SatLine = "Warm on the back of the neck. The lettuce does not care who you are.";

    /// <summary>#1332 · <b>SITTING A WHILE AND NOBODY CAME</b> — the garden's own silence, the answer to every
    /// fruitless SIT A WHILE on its bench. One line and never a pool: the garden has no walker and no figure, so
    /// nothing in it changes from one wait to the next. Fable canon, verbatim.</summary>
    public const string NobodyCameLine = "Nobody came. The tomatoes went on being staked.";

    /// <summary>#1332 · The garden bench's scene id — the park's own two moves under the garden's name, so the
    /// wait beat can tell whose silence to say without a new field on the sitting.</summary>
    public const string BenchSceneId = "garden:bench";

    /// <summary>
    /// #1332 · <b>THE GARDEN'S BENCH, AS A SCENE</b> — the park bench's own (<see cref="ParkBenches.TheBench"/>:
    /// the same move ids, the same labels, the same stand-up line) with the garden's id, the room's plate as its
    /// setting and the garden's sitting as its opening. Nobody is ever on the far end, so there is no shared
    /// form.
    /// </summary>
    public static Encounter.Scene TheBench() =>
        ParkBenches.TheBench(shared: false) with { Id = BenchSceneId, Setting = Plate, Opening = SatLine };

    /// <summary>#1332 · Is this sitting the garden's bench? Asked by the wait beat, which says
    /// <see cref="NobodyCameLine"/> here and the park's own pool (<see cref="ParkBenches.NobodyCame"/>) on every
    /// other bench.</summary>
    public static bool IsTheGardensBench(Encounter.Scene bench) => bench.Id == BenchSceneId;

    /// <summary>#1332 B · The menu line at this berth's board, or null where the counter has no board (and so no
    /// kitchen to send salad to). Asked of <see cref="TheMenuBoard.For"/> so the board and the line are read
    /// off one list: a bar that grew a board would grow the line with it.</summary>
    public static string? MenuLineAt(string? bodyId) =>
        TheMenuBoard.For(bodyId) is null ? null : MenuLine;

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Plate;
        foreach (string bed in BedPlates)
        {
            yield return bed;
        }

        yield return MenuLine;
        yield return FirstVisitLine;
        yield return SatLine;
        yield return NobodyCameLine;
    }
}
