using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #653 slice 1 · THE TWO WAYS INTO A DEAD STATION, HONEST FROM DAY ONE.
///
/// <para>The survey found that <see cref="StationWreck.AccessKind.CutFace"/> and the #537
/// <see cref="HullCutter"/> are the same concept, unwired — and that a serviceable lock is the 2026-09-13
/// ruling ("a locked door is TIME, never a key") waiting for a door. This is that wiring and nothing more:
/// no second cutter, no key, no lockpick.</para>
///
/// <list type="bullet">
/// <item><b>A serviceable lock</b> admits anybody, and charges <see cref="StationAboard.LockCycleSeconds"/> of
/// the clock. Nothing is spent but time.</item>
/// <item><b>A cut face</b> that has already been cut stands open at no cost — it is permanent. One that has
/// not needs the captain's own <see cref="HullCutter"/>: a cut is spent off the cell
/// (<see cref="HullCutter.Force"/>, the satchel's own arithmetic), the clock pays
/// <see cref="HullCutter.CutSeconds"/>, and the face stays cut. With no rig the answer is the cutter's own
/// <see cref="HullCutter.NoCutterLine"/> and nothing changes.</item>
/// </list>
///
/// <para>Pure and deterministic: the same access offered the same satchel gives the same answer.</para>
/// </summary>
public static class StationEntry
{
    /// <summary>What the door did about a captain.</summary>
    /// <param name="Admitted">Whether the way in is open now.</param>
    /// <param name="Seconds">The clock this cost. Zero on every refusal and on a face already cut.</param>
    /// <param name="CutMade">A face was cut by this very act — the caller records it as permanent.</param>
    /// <param name="Carried">The satchel afterwards — assigned by the caller, never mutated here.</param>
    /// <param name="Line">The canon line for the act, or the cutter's own refusal; empty when a face already
    /// cut simply stands open.</param>
    /// <param name="CellLine">What the cell has left, in the cutter's own words; empty unless a cut was made.</param>
    public readonly record struct Order(
        bool Admitted, double Seconds, bool CutMade,
        IReadOnlyList<Satchel.Item> Carried, string Line, string CellLine);

    /// <summary>Ask the access to let this captain in.</summary>
    public static Order Enter(
        in StationWreck.Access access,
        IReadOnlyCollection<StationWreck.ModuleId>? cutFaces,
        IReadOnlyList<Satchel.Item>? carried)
    {
        IReadOnlyList<Satchel.Item> pocket = carried ?? [];

        if (access.Kind == StationWreck.AccessKind.ServiceableLock)
        {
            return new Order(true, StationAboard.LockCycleSeconds, false, pocket, StationAboard.LockLine, "");
        }

        if (cutFaces is not null && cutFaces.Contains(access.Module))
        {
            return new Order(true, 0, false, pocket, "", "");
        }

        HullCutter.Order cut = HullCutter.Force(carried);
        if (!cut.Cut)
        {
            return new Order(false, 0, false, pocket, cut.Line, "");
        }

        return new Order(true, HullCutter.CutSeconds, true, cut.Carried, StationAboard.CutFaceLine, cut.Line);
    }
}
