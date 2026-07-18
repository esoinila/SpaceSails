namespace SpaceSails.Core;

/// <summary>
/// The one enumerable registry of dockable station havens (#288): every mass-less haven a ship can
/// throw a clamp onto — <c>IsHaven</c> AND <c>Mu &lt;= 0</c>. Moon havens (μ&gt;0, e.g. Enceladus) are
/// hidden-at-by-orbit, not clamped, so they are deliberately excluded — the same split the client's
/// <c>IsDockableHaven</c> draws. Both the <c>?dock=&lt;id&gt;</c> boot cheat's id list (console-logged
/// on boot, documented in the testing guide) and the CI docked-start smoke sweep read this, so no
/// bench ever has to guess a berth id and every new haven added to a scenario is swept for free.
/// </summary>
public static class DockableHavens
{
    /// <summary>True when <paramref name="body"/> is a berth you clamp onto (⚓): a mass-less haven.</summary>
    public static bool IsDockable(CelestialBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return body is { IsHaven: true, Mu: <= 0 };
    }

    /// <summary>Every dockable station haven in the scenario, in body-list order.</summary>
    public static IReadOnlyList<CelestialBody> All(ICelestialEphemeris ephemeris)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        return [.. ephemeris.Bodies.Where(IsDockable)];
    }

    /// <summary>The ids of every dockable station haven — the <c>?dock=&lt;id&gt;</c> menu.</summary>
    public static IReadOnlyList<string> AllIds(ICelestialEphemeris ephemeris)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        return [.. ephemeris.Bodies.Where(IsDockable).Select(b => b.Id)];
    }
}
