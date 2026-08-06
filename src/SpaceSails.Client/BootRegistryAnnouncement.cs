namespace SpaceSails.Client;

/// <summary>
/// #726 · Says a dev registry to the browser console ONCE, and again only when it has changed.
/// </summary>
/// <remarks>
/// <para>
/// The dockable-berth roster (#288) is a bench convenience: a printed menu of the ids
/// <c>/map?dock=&lt;id&gt;</c> accepts, so nobody has to guess one. It reads as boot-time output — and it
/// is, per Map component — but the Map component is created afresh by the router on every arrival at
/// <c>/map</c>, so a session that walks Home → Map → Home → Map prints the same unchanged list a fourth
/// time. Measured on a Release build: berth line 1, 2, 3, 4 across three SPA round trips, while Blazor's
/// own once-per-start "Debugging hotkey" line stayed at 1 throughout. A console that repeats itself
/// teaches the bench to stop reading it.
/// </para>
/// <para>
/// The roster is NOT constant, so "print once, ever" would be a lie: <c>?scenario=</c> loads a different
/// world with different berths, and the cheats bolt extra bodies on. So the rule is "print when it is
/// news": remember what was last said and stay quiet while the answer is the same one. A second lap round
/// the same world is silent; a different world announces itself.
/// </para>
/// </remarks>
internal sealed class BootRegistryAnnouncement
{
    private string? _lastSaid;

    /// <summary>
    /// True when <paramref name="line"/> differs from the last line this announcer approved — and
    /// remembers it as the new last. False (and remembers nothing new) when it is a repeat.
    /// </summary>
    public bool IsNews(string line)
    {
        if (string.Equals(_lastSaid, line, StringComparison.Ordinal))
        {
            return false;
        }

        _lastSaid = line;
        return true;
    }
}
