using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.HeadOffice — #411 THE TWO FLOORS WITH A BEAT ON THEM.
//
// The head office reads as the head office from its first floor (the plates, the livery, the wings, the lift
// that asks for nothing — all of that is UndergroundComplex, and none of it is narrated). What it did not
// have until here was a REASON to walk twenty-four floors, and this arc has always known exactly what is at
// the bottom of it:
//
//   B23 · THE WINTERING HALL — the room this arc was written for. Forty berths, and one more.
//   B24 · THE BERTH OFFICE   — the console that has never stopped filing, knee-deep in its own output.
//
// Both are one console apiece, appended onto the generated floor the way the secret lab's door is appended,
// so the Hive's own generator and its walkability audit are untouched. Both raise the house card and say
// nothing about what any of it means: the wintering hall's card counts, because counting is a thing the
// captain does with their own eyes, and it never says whose the forty-first is.
//
// ── AND THE 40 ─────────────────────────────────────────────────────────────────────────────────────────
//
// KaamosLore.RevealSanityShockHook = 40.0 has been "designed, never consumed" since the arc's spine landed,
// and the story-QA run named it as the standing example of a truth that lives only in a Core constant. Its
// own doc comment says it is for "reaching the wintering mind", the heaviest #391 throw in the game; the
// writers' bible's reveal is "it has kept a berth warm for you".
//
// The forty-first bed IS that sentence, as evidence. So it is wired here, once, and NOWHERE else — and it
// is flagged loudly in docs/features/kaamos-head-office.md §4 as a feel call the owner can overrule in one
// line, because 40 is ~40% of the gauge and the biggest single throw in the game (the monolith is 24).
public partial class Map
{
    /// <summary>How far along the spine from the lift the head office's one console stands. Far enough that
    /// it is not in the car's doorstep and not in the lift console's label, close enough that a captain who
    /// has just spent four days getting here does not have to search a floor for it.</summary>
    private const double HeadOfficeConsoleOffsetDu = 18.0;

    /// <summary>Which of the head office's beat floors have already spent their card and, for B23, its nerve.
    ///
    /// <para>Per THREAD, cleared with everything else on a new voyage (Map.Vault). Not per excursion: the
    /// point of the gate is that 40 nerve is paid once, and a captain who rides back down for another look
    /// is doing something deliberate and should not be charged for it again. The card itself is never out of
    /// reach — [E] at the console shows it whenever they want it.</para></summary>
    private readonly HashSet<int> _headOfficeBeatsSeen = [];

    /// <summary>Append the head office's own console to whichever of its two floors this is. Called straight
    /// after <c>HiveInterior.FloorDeck</c>, so the generated floor — and the A* audit that walks it — is
    /// untouched; this is the same append path the hidden door and the outpost hut use.</summary>
    private void ComposeHeadOfficeFloor(SurfaceExcursion ex)
    {
        if (!UndergroundComplex.IsHeadOffice(ex.Stop.Body.Id))
        {
            return;
        }

        string label =
            ex.Floor == UndergroundComplex.WinteringHallLevel ? UndergroundComplex.WinteringHallLabel
            : ex.Floor == UndergroundComplex.BerthOfficeLevel ? UndergroundComplex.BerthOfficeLabel
            : "";

        if (label.Length == 0)
        {
            return;
        }

        // On the spine, which is the one line on any of these floors that is walkable end to end and the
        // line the lift lets you out onto — so the console cannot land inside a room's wall or behind a
        // rib's sealed mouth, whatever the seed did with the rest of the floor.
        (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(MoonSurface.ExpeditionField());

        _deckPlan.AppendRegion(new DeckPlan.DeckRegion(
            [],
            [new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject,
                (float)(shaftX - HeadOfficeConsoleOffsetDu), (float)shaftY, label)],
            [],
            []));
    }

    /// <summary>Raise the beat for this floor, if it has one and has not spent it. Called from the descent
    /// so the card lands as the doors part, which is the moment it explains the most.</summary>
    private void MaybeRaiseHeadOfficeBeat(SurfaceExcursion ex)
    {
        if (!UndergroundComplex.IsHeadOffice(ex.Stop.Body.Id) || !_headOfficeBeatsSeen.Add(ex.Floor))
        {
            return;
        }

        if (ex.Floor == UndergroundComplex.WinteringHallLevel)
        {
            ShowAndFile(UndergroundComplex.WinteringHallLine, "❄");
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.WinteringHallLabel,
                UndergroundComplex.WinteringHallArtUrl, UndergroundComplex.WinteringHallCard);

            // The one place the arc's own number is spent. Core owns it; this is the only call site in the
            // game, and there must never be a second — a reveal that costs 40 twice is a reveal that costs
            // 80, and the constant would stop meaning what its doc comment says.
            ApplyNerveShock(KaamosLore.RevealSanityShockHook, UndergroundComplex.WinteringHallShockReason);
            RendererInterop.PlayCue("reveal");
            return;
        }

        if (ex.Floor == UndergroundComplex.BerthOfficeLevel)
        {
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.BerthOfficeLabel,
                UndergroundComplex.BerthOfficeArtUrl, UndergroundComplex.BerthOfficeCard);
            RendererInterop.PlayCue("reveal");
        }
    }

    /// <summary>Pressing [E] at the appended console shows the same card again — the two things down here a
    /// captain will want to look at twice. Returns false when the console under the captain is not one of
    /// ours, so the ordinary console dispatch carries on.</summary>
    private bool TryHeadOfficeConsole(SurfaceExcursion ex, string label)
    {
        if (!UndergroundComplex.IsHeadOffice(ex.Stop.Body.Id))
        {
            return false;
        }

        if (label == UndergroundComplex.WinteringHallLabel)
        {
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.WinteringHallLabel,
                UndergroundComplex.WinteringHallArtUrl, UndergroundComplex.WinteringHallCard);
            return true;
        }

        if (label == UndergroundComplex.BerthOfficeLabel)
        {
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.BerthOfficeLabel,
                UndergroundComplex.BerthOfficeArtUrl, UndergroundComplex.BerthOfficeCard);
            return true;
        }

        return false;
    }
}
