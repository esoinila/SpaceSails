namespace SpaceSails.Core.Interior;

// Geometry primitives for a runtime-appended wing (Wednesday plan §3 PR-F / Tuesday vision §6,
// "Doors that grow the world"). Deliberately mirror the client's DeckPlan.Wall / .Door /
// .ConsoleSpot shapes field-for-field so the client can translate a DeckWing straight into deck
// geometry, while the *model* — what a wing is, which hatch grows it, how a doorway is carved —
// lives here in Core where it is pure and unit-tested. Deck units, matching DeckPlan.

/// <summary>A wall segment of an appended wing. <c>IsHull</c> walls read as station skin in the
/// first-person raycaster; interior partitions set it false.</summary>
public readonly record struct WingWall(float X1, float Y1, float X2, float Y2, bool IsWindow = false, bool IsHull = true);

/// <summary>A door across a wing threshold. A wing's connecting door is always <c>Locked=false</c>
/// (you cracked it open) so the avatar can walk through; the model asserts this in
/// <see cref="DeckExpansions.Validate"/>.</summary>
public readonly record struct WingDoor(float X1, float Y1, float X2, float Y2, bool Locked = false);

/// <summary>What an appended console does when the player presses E on it inside the new room.</summary>
public enum WingConsoleKind
{
    /// <summary>A knockable/opened hatch panel (the expansion joint itself).</summary>
    Hatch,
    /// <summary>A pickup — the fence's package on the shelf; lifting it advances the indoor quest.</summary>
    Stash,
    /// <summary>A person to talk to (a bar-style booth) found in the new room.</summary>
    Patron,
    /// <summary>A lore prop / image to view.</summary>
    ViewObject,
}

/// <summary>An interaction point inside a wing (translated to a client <c>DeckPlan.ConsoleSpot</c>).</summary>
public readonly record struct WingConsole(WingConsoleKind Kind, float X, float Y, string Label,
    string? ImageUrl = null, string? Caption = null);

/// <summary>A floor label drawn inside a wing.</summary>
public readonly record struct WingLabel(float X, float Y, string Text);

/// <summary>
/// A wing that is welded onto a station's deck plan at runtime once its hatch is unlocked — the
/// "expansion joint" of Tuesday vision §6. Geometry is data: opening the hatch appends these walls,
/// doors, consoles and labels to the live plan (per-session; the owner accepted per-session
/// persistence for v1, Wednesday plan §1). A wing is keyed to exactly one station body and one
/// hatch id, so quests can gate on the room existing and rooms can gate on quests.
/// </summary>
public sealed record DeckWing(
    string Id,
    string StationBodyId,
    string UnlockHatchId,
    string LocationName,
    IReadOnlyList<WingWall> Walls,
    IReadOnlyList<WingDoor> Doors,
    IReadOnlyList<WingConsole> Consoles,
    IReadOnlyList<WingLabel> Labels);

/// <summary>
/// The pure rules of runtime wings: which of a station's wings are currently welded on given the
/// session's unlock set, whether a hatch grows a room at all, and the geometry helper that carves a
/// walkable doorway into a previously sealed hall edge. The concrete station catalog (with real
/// coordinates authored against the hall geometry) lives in the client's <c>HavenInterior</c>; these
/// functions take that catalog as data so they stay engine-agnostic and testable.
/// </summary>
public static class DeckExpansions
{
    /// <summary>The wings of <paramref name="catalog"/> that are welded on right now: those for this
    /// station whose unlock hatch is in the session's <paramref name="unlockedHatchIds"/> set.
    /// Deterministic order (catalog order preserved).</summary>
    public static IEnumerable<DeckWing> ActiveWings(
        IEnumerable<DeckWing> catalog, string stationBodyId, IReadOnlySet<string> unlockedHatchIds)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(unlockedHatchIds);
        return catalog.Where(w => w.StationBodyId == stationBodyId && unlockedHatchIds.Contains(w.UnlockHatchId));
    }

    /// <summary>True if any wing in <paramref name="catalog"/> grows behind hatch
    /// <paramref name="hatchId"/> on this station — i.e. cracking it opens a real room, not just a
    /// "lock blinks green" flavor line. Lets the crack quest know when an unlock should weld a wing.</summary>
    public static bool GrowsBehind(IEnumerable<DeckWing> catalog, string stationBodyId, string hatchId)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return catalog.Any(w => w.StationBodyId == stationBodyId && w.UnlockHatchId == hatchId);
    }

    /// <summary>
    /// Carve a walkable doorway into a sealed hall edge running A→B: keep a wall stub at each end and
    /// open the middle span [<paramref name="gapStart"/>, <paramref name="gapEnd"/>] (fractions along
    /// A→B, 0..1), with an unlocked auto-door across the opening. The two stubs replace the edge's old
    /// solid wall so the avatar can pass; the wing's own walls close the room beyond. Pure geometry —
    /// the same call the client uses to open V-06's edge, exercised directly in tests.
    /// </summary>
    public static (WingWall StubA, WingWall StubB, WingDoor Door) CarveDoorway(
        float ax, float ay, float bx, float by, float gapStart, float gapEnd)
    {
        if (!(gapStart >= 0) || !(gapEnd <= 1) || !(gapStart < gapEnd))
        {
            throw new ArgumentOutOfRangeException(nameof(gapStart),
                $"Doorway gap must satisfy 0 <= start < end <= 1 (got {gapStart}..{gapEnd}).");
        }

        float gsx = ax + (bx - ax) * gapStart, gsy = ay + (by - ay) * gapStart;
        float gex = ax + (bx - ax) * gapEnd, gey = ay + (by - ay) * gapEnd;
        return (
            new WingWall(ax, ay, gsx, gsy, IsWindow: false, IsHull: true),
            new WingWall(gex, gey, bx, by, IsWindow: false, IsHull: true),
            new WingDoor(gsx, gsy, gex, gey, Locked: false));
    }

    /// <summary>
    /// Data-integrity check on an authored wing (used by tests and buildable as a cheap assert): a
    /// wing must name its station and hatch, carry at least one wall so the room is enclosed, and
    /// none of its connecting doors may be locked — an opened expansion joint you cannot walk through
    /// would be a bug. Returns the wing for fluent use; throws on violation.
    /// </summary>
    public static DeckWing Validate(DeckWing wing)
    {
        ArgumentNullException.ThrowIfNull(wing);
        if (string.IsNullOrEmpty(wing.StationBodyId) || string.IsNullOrEmpty(wing.UnlockHatchId))
        {
            throw new ArgumentException($"Wing '{wing.Id}' must name a station body and an unlock hatch.");
        }
        if (wing.Walls.Count == 0)
        {
            throw new ArgumentException($"Wing '{wing.Id}' has no walls — the room would not be enclosed.");
        }
        if (wing.Doors.Any(d => d.Locked))
        {
            throw new ArgumentException($"Wing '{wing.Id}' has a locked connecting door — you opened it, so it must be walkable.");
        }
        return wing;
    }
}
