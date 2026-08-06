namespace SpaceSails.Client.Tests;

/// <summary>
/// #726 · A CONSOLE THAT REPEATS ITSELF STOPS BEING READ.
///
/// <para>Filed off a smoke test as "one navigation printed the boot log a dozen times". Measured on a
/// Release build, driven headless, counting raw <c>Runtime.consoleAPICalled</c> events: one page load
/// prints the berth roster <b>once</b> and Blazor's own "Debugging hotkey" line <b>once</b> — so twelve of
/// each in one second was never twelve inits (twelve full WASM boots cannot fit in a second on a page whose
/// boot pegs the main thread for tens of seconds); it was twelve reads of one console.</para>
///
/// <para>But the smell underneath it was real, and the same run measured it: walking Home → Map → Home →
/// Map printed the berth roster 1, 2, 3, 4 while the hotkey line stayed at 1. The router builds a fresh
/// <c>Map</c> component on every arrival, and the #288 registry print rides that per-component boot path.
/// The list had not changed; the console said it four times anyway.</para>
///
/// <para>The fix is not "print once, ever" — the roster is a function of the world, and
/// <c>?scenario=</c> loads a different one — it is "print when it is news". This guard drives the SHIPPING
/// announcer (<c>Map.BerthRosterAnnouncement</c> is the very object the page consults), not a copy of the
/// rule, so it cannot pass while the page does something else.</para>
///
/// <para>To watch it go red on the old behaviour, make <c>BootRegistryAnnouncement.IsNews</c>
/// <c>return true;</c> unconditionally — that is exactly the unguarded <c>Console.WriteLine</c> this
/// replaced — and the repeat cases below fail.</para>
/// </summary>
public sealed class TheBootLogSaysItOnceTests
{
    private const string SolRoster = "[SpaceSails] Dockable berths — /map?dock=<id>: cinder-roost, selene-gate, the-space-bar";
    private const string OtherRoster = "[SpaceSails] Dockable berths — /map?dock=<id>: cinder-roost, selene-gate";

    [Fact]
    public void TheFirstArrivalAnnouncesTheRoster()
    {
        var announcer = new BootRegistryAnnouncement();

        Assert.True(announcer.IsNews(SolRoster));
    }

    [Fact]
    public void FourLapsRoundTheSAMEWorldAnnounceItOnce()
    {
        // The measured repro: Home → Map → Home → Map → … Four Map components, one unchanged roster.
        var announcer = new BootRegistryAnnouncement();

        int announced = 0;
        for (int arrival = 0; arrival < 4; arrival++)
        {
            if (announcer.IsNews(SolRoster))
            {
                announced++;
            }
        }

        Assert.Equal(1, announced);
    }

    [Fact]
    public void ADIFFERENTWorldStillAnnouncesItself()
    {
        // ?scenario= loads a different set of berths, and the cheats bolt extra bodies on. Silence there
        // would be the opposite bug: a bench guessing ids again because the console withheld the new ones.
        var announcer = new BootRegistryAnnouncement();
        announcer.IsNews(SolRoster);

        Assert.True(announcer.IsNews(OtherRoster));
    }

    [Fact]
    public void AndComingBACKToTheFirstWorldAnnouncesItAgain()
    {
        // "News" is measured against the last thing SAID, not against everything ever said — otherwise the
        // announcer would have to grow a set that never shrinks, for a line nobody re-reads anyway.
        var announcer = new BootRegistryAnnouncement();
        announcer.IsNews(SolRoster);
        announcer.IsNews(OtherRoster);

        Assert.True(announcer.IsNews(SolRoster));
    }

    [Fact]
    public void ThePageConsultsAnAnnouncerThatOUTLIVESTheComponent()
    {
        // The whole point: the router discards the Map instance on every navigation, so an announcer that
        // lived on the instance would remember nothing and the console would repeat exactly as before. This
        // asserts against the real field the page reads — a per-instance announcer cannot satisfy it.
        Assert.NotNull(Pages.Map.BerthRosterAnnouncement);

        string roster = $"[SpaceSails] berth roster for {nameof(ThePageConsultsAnAnnouncerThatOUTLIVESTheComponent)}";
        Assert.True(Pages.Map.BerthRosterAnnouncement.IsNews(roster));
        Assert.False(Pages.Map.BerthRosterAnnouncement.IsNews(roster));
    }
}
