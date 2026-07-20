namespace SpaceSails.Core.Tests;

/// <summary>
/// Evening wind #20 · THE INSURANCE CAPTAIN. Pins the pure succession spine: the overdraw predicate bands,
/// the repeat-avoiding new-identity roll, the retired-history append (determinism, avatar-differs, history
/// grows), the roster copy, and the registry persistence — plus the guarantee that the overdraw always
/// lands a captain in the "joined them" eligibility window, so DeathNarration's Reevers/Joined routing is
/// preserved end to end.
/// </summary>
public class CaptainSuccessionTests
{
    private sealed class MemStore : ISlotStore
    {
        private readonly Dictionary<string, string> _map = new(StringComparer.Ordinal);
        public string? Read(string key) => _map.TryGetValue(key, out string? v) ? v : null;
        public void Write(string key, string value) => _map[key] = value;
        public void Clear(string key) => _map.Remove(key);
    }

    // ── The overdraw predicate: bands the gauge into empty→true, else→false ──

    [Theory]
    [InlineData(0.0, true)]                                   // bottomed out — a further hit breaks
    [InlineData(CaptainSuccession.EmptyThreshold, true)]      // exactly at the sliver — still empty
    [InlineData(CaptainSuccession.EmptyThreshold + 0.01, false)] // a hair above — only floors, never breaks
    [InlineData(10.0, false)]                                 // fraying, not empty
    [InlineData(50.0, false)]                                 // mid-gauge
    [InlineData(NerveModel.Max, false)]                       // steady hands
    public void OverdrawQualifies_OnlyWhenAlreadyEmpty(double nerveBeforeHit, bool expected)
    {
        Assert.Equal(expected, CaptainSuccession.OverdrawQualifies(nerveBeforeHit));
    }

    [Fact]
    public void OverdrawThreshold_SitsUnderTheJoinedSliver_SoJoinedIsAlwaysEligible()
    {
        // An overdraw death is always at/under EmptyThreshold, which is at/under the Joined sliver — so the
        // eerie "joined them" reading is always a possible outcome of an overdraw (never gated out).
        Assert.True(CaptainSuccession.EmptyThreshold <= DeathNarration.JoinedNerveSliver);

        // The seeded minority still joins; the majority is taken. Routing preserved through the overdraw band.
        Assert.Equal(DeathCause.Joined,
            DeathNarration.SurfaceEnd(CaptainSuccession.EmptyThreshold, seed: 0));
        Assert.Equal(DeathCause.Reevers,
            DeathNarration.SurfaceEnd(CaptainSuccession.EmptyThreshold, seed: 1));
    }

    // ── The new-identity roll: deterministic, and the face always differs ──

    [Fact]
    public void NewIdentity_IsDeterministic()
    {
        (string Name, int Avatar) a = CaptainSuccession.NewIdentity(3, "thread-x|succ1");
        (string Name, int Avatar) b = CaptainSuccession.NewIdentity(3, "thread-x|succ1");
        Assert.Equal(a, b);
    }

    [Fact]
    public void NewIdentity_AvatarAlwaysDiffersFromThePrevious()
    {
        // Whatever face the seed would land on, the successor's must never equal the retiree's — across the
        // full roster of "previous" faces and many seeds.
        for (int prev = 1; prev <= Captains.AvatarCount; prev++)
        {
            for (int i = 0; i < 60; i++)
            {
                (_, int avatar) = CaptainSuccession.NewIdentity(prev, $"seed-{prev}-{i}");
                Assert.InRange(avatar, 1, Captains.AvatarCount);
                Assert.NotEqual(prev, avatar);
            }
        }
    }

    [Fact]
    public void NewIdentity_StepsOffTheFace_OnlyWhenTheSeedCollides()
    {
        // A seed whose derived avatar equals the previous face is stepped one forward (wrapping); a seed
        // that already differs is left alone.
        int derived = Captains.AvatarIndex("collide-seed");
        (_, int stepped) = CaptainSuccession.NewIdentity(derived, "collide-seed");
        Assert.Equal((derived % Captains.AvatarCount) + 1, stepped);

        int other = derived == 1 ? 2 : 1; // a previous face the seed did NOT derive
        (_, int kept) = CaptainSuccession.NewIdentity(other, "collide-seed");
        Assert.Equal(derived, kept);
    }

    [Fact]
    public void NewName_ComesFromTheHouseRoster()
    {
        (string name, _) = CaptainSuccession.NewIdentity(1, "any-seed");
        Assert.StartsWith("Captain ", name);
    }

    // ── Succeed: rolls the identity AND grows the history ──

    [Fact]
    public void Succeed_RollsANewIdentity_AndAppendsTheRetiree()
    {
        var current = new GameThreadInfo { Id = "t-1", CaptainName = "Captain Mabel Vane", AvatarIndex = 4 };

        GameThreadInfo next = CaptainSuccession.Succeed(current, retiredSimDay: 42);

        // The face changed (a new face in the mirror) and a name is set.
        Assert.NotEqual(4, next.AvatarIndex);
        Assert.InRange(next.AvatarIndex, 1, Captains.AvatarCount);
        Assert.StartsWith("Captain ", next.CaptainName);

        // The history grew by exactly the retiree, with their name and the day they were written off.
        Assert.Single(next.Retired);
        Assert.Equal("Captain Mabel Vane", next.Retired[0].Name);
        Assert.Equal(42, next.Retired[0].SimDay);
    }

    [Fact]
    public void Succeed_IsDeterministic_ForAThread_AndGrowsAcrossGenerations()
    {
        var start = new GameThreadInfo { Id = "t-det", CaptainName = "Captain Nemo", AvatarIndex = 2 };

        GameThreadInfo a1 = CaptainSuccession.Succeed(start, 10);
        GameThreadInfo a2 = CaptainSuccession.Succeed(start, 10);
        Assert.Equal(a1.CaptainName, a2.CaptainName);   // same thread + generation → same successor
        Assert.Equal(a1.AvatarIndex, a2.AvatarIndex);

        // A second death rolls a DIFFERENT generation seed, and history keeps growing (oldest first).
        GameThreadInfo b1 = CaptainSuccession.Succeed(a1, 20);
        Assert.Equal(2, b1.Retired.Count);
        Assert.Equal(a1.Retired[0].Name, b1.Retired[0].Name);      // the first retiree stays at the front
        Assert.Equal(a1.CaptainName, b1.Retired[1].Name);          // the second retiree is a1's captain
        Assert.NotEqual(a1.CaptainName, b1.CaptainName);           // gen 2 differs from gen 1's roll
    }

    [Fact]
    public void Succeed_UsesTheSeededIdentity_WhenNoNameIsStored()
    {
        // A pre-roster thread (no stored identity) retires the id-derived captain, and the successor still
        // avoids that derived face.
        var thin = new GameThreadInfo { Id = "legacy" };
        int derivedFace = Captains.AvatarIndex("legacy");

        GameThreadInfo next = CaptainSuccession.Succeed(thin, 5);
        Assert.Equal(Captains.Name("legacy"), next.Retired[0].Name);
        Assert.NotEqual(derivedFace, next.AvatarIndex);
    }

    // ── The roster copy ──

    [Fact]
    public void RetiredLine_ReadsUnderCaptUntilDay_StrippingTheTitle()
    {
        Assert.Equal("under Capt. Mabel Vane until day 42",
            CaptainSuccession.RetiredLine(new RetiredCaptain("Captain Mabel Vane", 42)));
        // A name without the stored title still reads whole.
        Assert.Equal("under Capt. Nemo until day 0",
            CaptainSuccession.RetiredLine(new RetiredCaptain("Nemo", 0)));
    }

    [Fact]
    public void PolicyPayoutLine_IsTheOwnersVerbatimHouseVoice()
    {
        Assert.Equal(
            "The policy pays out. A new name on the license, a new face in the mirror — the ship doesn't care.",
            CaptainSuccession.PolicyPayoutLine);
    }

    // ── The registry persists the succession and preserves it across autosaves ──

    [Fact]
    public void Registry_IssueSuccessor_PersistsNewIdentity_AndAppendsHistory()
    {
        var reg = new GameThreadRegistry(new MemStore());
        reg.Touch("thread-a", "Miranda", 12, ticks: 100);
        GameThreadInfo before = reg.Get("thread-a")!;

        GameThreadInfo? after = reg.IssueSuccessor("thread-a", retiredSimDay: 12);

        Assert.NotNull(after);
        Assert.NotEqual(before.AvatarIndex, after!.AvatarIndex);     // new face
        Assert.NotEqual(before.CaptainName, after.CaptainName);      // new name (different generation seed)
        Assert.Single(after.Retired);
        Assert.Equal(before.CaptainName, after.Retired[0].Name);     // the retiree is remembered
        Assert.Equal(12, after.Retired[0].SimDay);

        // Persisted: a fresh read sees the successor, and the thread stays active without a clock bump.
        GameThreadInfo reread = reg.Get("thread-a")!;
        Assert.Equal(after.CaptainName, reread.CaptainName);
        Assert.Equal(after.AvatarIndex, reread.AvatarIndex);
        Assert.Equal("thread-a", reg.ActiveId);
        Assert.Equal(100, reread.LastActiveTicks);                  // succession does not bump the clock
    }

    [Fact]
    public void Registry_Touch_PreservesTheRetiredHistory_AcrossAutosaves()
    {
        var reg = new GameThreadRegistry(new MemStore());
        reg.Touch("thread-b", "unknown waters", 0, ticks: 10);
        reg.IssueSuccessor("thread-b", retiredSimDay: 3);
        GameThreadInfo succeeded = reg.Get("thread-b")!;

        reg.Touch("thread-b", "The Tilt", 30, ticks: 250); // an autosave must not wipe the memory

        GameThreadInfo played = reg.Get("thread-b")!;
        Assert.Equal(succeeded.CaptainName, played.CaptainName);   // the new captain survives the touch
        Assert.Equal(succeeded.AvatarIndex, played.AvatarIndex);
        Assert.Single(played.Retired);                             // ...and so does the retiree
        Assert.Equal(succeeded.Retired[0].Name, played.Retired[0].Name);
    }

    [Fact]
    public void Registry_IssueSuccessor_UnknownThread_IsNull()
    {
        var reg = new GameThreadRegistry(new MemStore());
        Assert.Null(reg.IssueSuccessor("ghost", 0));
    }

    [Fact]
    public void RetiredHistory_SurvivesAJsonRoundTrip_ThroughTheRegistry()
    {
        // Two universes over one store, one of them succeeded twice — the whole index must serialize and
        // read back with the history intact (additive, tolerant registry JSON).
        var store = new MemStore();
        var reg = new GameThreadRegistry(store);
        reg.Touch("u1", "Miranda", 1, ticks: 10);
        reg.IssueSuccessor("u1", 1);
        reg.IssueSuccessor("u1", 2);
        reg.Touch("u2", "The Tilt", 5, ticks: 20);

        var reread = new GameThreadRegistry(store); // fresh instance → reads the persisted JSON
        Assert.Equal(2, reread.Get("u1")!.Retired.Count);
        Assert.Empty(reread.Get("u2")!.Retired);
    }
}
