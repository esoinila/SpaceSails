namespace SpaceSails.Core.Tests;

/// <summary>
/// THE CAPTAIN'S SELFIE (issue #400). Pins the pure spine: the scenic-spot catalog, the DETERMINISTIC
/// house-voice caption pick (a spot/beat + a seed always boasts the same line), the composed capture, and
/// the album's persistence contract — round-trips onto the thread row, dedups per spot/beat, survives an
/// autosave <see cref="GameThreadRegistry.Touch"/>, and RESETS on succession so a new captain inherits
/// none (the wall of fame is per-life).
/// </summary>
public class SelfieTests
{
    private sealed class MemStore : ISlotStore
    {
        private readonly Dictionary<string, string> _map = new(StringComparer.Ordinal);
        public string? Read(string key) => _map.TryGetValue(key, out string? v) ? v : null;
        public void Write(string key, string value) => _map[key] = value;
        public void Clear(string key) => _map.Remove(key);
    }

    // ── The scenic-spot catalog: the four awesome-view havens carry a spot; the rest don't ──

    [Theory]
    [InlineData("red-eye")]
    [InlineData("ringside-exchange")]
    [InlineData("selene-gate")]
    [InlineData("the-deep")]
    public void ScenicHavens_CarryASelfieSpot(string bodyId)
    {
        Assert.True(SelfieSpots.HasSpot(bodyId));
        SelfieSpot spot = SelfieSpots.For(bodyId)!;
        Assert.Equal(bodyId, spot.BodyId);
        Assert.NotEmpty(spot.SpotId);
        Assert.StartsWith("📸", spot.ConsoleLabel);
        Assert.NotEmpty(spot.VistaArt);
        Assert.NotEmpty(spot.Captions);
    }

    [Theory]
    [InlineData("cinder-roost")]      // a haven, but no scenic window
    [InlineData("the-space-bar")]
    [InlineData("not-a-place")]
    [InlineData(null)]
    public void NonScenicBerths_HaveNoSpot(string? bodyId)
    {
        Assert.False(SelfieSpots.HasSpot(bodyId));
        Assert.Null(SelfieSpots.For(bodyId));
    }

    [Fact]
    public void EverySpot_HasAUniqueSpotId()
    {
        var ids = SelfieSpots.AllSpots.Select(s => s.SpotId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    // ── Caption determinism: the same seed always boasts the same line, and it comes FROM the pool ──

    [Fact]
    public void PickCaption_IsDeterministic_AndFromThePool()
    {
        IReadOnlyList<string> pool = SelfieSpots.For("ringside-exchange")!.Captions;
        string a = SelfieSpots.PickCaption(pool, "thread-77|spot-ringside");
        string b = SelfieSpots.PickCaption(pool, "thread-77|spot-ringside");
        Assert.Equal(a, b);            // stable across calls (no RNG, no clock)
        Assert.Contains(a, pool);      // always a real authored boast
    }

    [Fact]
    public void PickCaption_PinsExactLine_ForAKnownSeed()
    {
        // A regression pin: if the pool or the hash ever shifts, this line moves and the test shouts.
        IReadOnlyList<string> pool = SelfieSpots.For("red-eye")!.Captions;
        string picked = SelfieSpots.PickCaption(pool, "abc123|spot-red-eye");
        Assert.Contains(picked, pool);
        // Re-derive independently to prove it isn't just echoing itself.
        Assert.Equal(pool[IndexFor("abc123|spot-red-eye", pool.Count)], picked);
    }

    [Fact]
    public void PickCaption_EmptyPool_IsEmptyString()
        => Assert.Equal("", SelfieSpots.PickCaption([], "anything"));

    // ── The composed capture: spot and beat both stamp the caption, vista and the captain's face ──

    [Fact]
    public void CaptureAt_ComposesTheShot_ForAScenicSpot()
    {
        CapturedSelfie shot = SelfieSpots.CaptureAt("selene-gate", "u1", avatarIndex: 5, simDay: 12)!;
        SelfieSpot spot = SelfieSpots.For("selene-gate")!;
        Assert.Equal(spot.SpotId, shot.SpotId);
        Assert.Equal(spot.VistaArt, shot.VistaArt);
        Assert.Equal(5, shot.AvatarIndex);
        Assert.Equal(12, shot.SimDay);
        Assert.Equal("spot", shot.Kind);
        Assert.Equal(SelfieSpots.PickCaption(spot.Captions, "u1|" + spot.SpotId), shot.Caption);
    }

    [Fact]
    public void CaptureAt_NonScenicBerth_IsNull()
        => Assert.Null(SelfieSpots.CaptureAt("cinder-roost", "u1", 1, 0));

    [Theory]
    [InlineData(SelfieBeats.Deflection)]
    [InlineData(SelfieBeats.RevealSurvived)]
    [InlineData(SelfieBeats.FirstMonolith)]
    public void CaptureBeat_ComposesAThemedShot(string beatId)
    {
        CapturedSelfie shot = SelfieSpots.CaptureBeat(beatId, "u9", avatarIndex: 3, simDay: 40, vistaArt: "art/x.jpg")!;
        Assert.Equal(beatId, shot.SpotId);
        Assert.Equal("beat", shot.Kind);
        Assert.Equal("art/x.jpg", shot.VistaArt);
        Assert.Equal(3, shot.AvatarIndex);
        (string Label, IReadOnlyList<string> Captions) beat = SelfieSpots.Beat(beatId)!.Value;
        Assert.Equal(beat.Label, shot.Title);
        Assert.Contains(shot.Caption, beat.Captions);
    }

    [Fact]
    public void CaptureBeat_UnknownBeat_IsNull()
        => Assert.Null(SelfieSpots.CaptureBeat("beat-nope", "u1", 1, 0));

    // ── The album's persistence contract on the thread row ──

    [Fact]
    public void AddSelfie_RoundTrips_AcrossAFreshRegistryOverTheSameStore()
    {
        var store = new MemStore();
        var reg = new GameThreadRegistry(store);
        reg.Touch("uni", "Selene Gate", simDay: 3, ticks: 100);

        CapturedSelfie shot = SelfieSpots.CaptureAt("selene-gate", "uni", 4, 3)!;
        Assert.NotNull(reg.AddSelfie("uni", shot));

        // A brand-new registry object reading the same store must see the filed selfie (real persistence).
        var reloaded = new GameThreadRegistry(store);
        GameThreadInfo row = reloaded.Get("uni")!;
        Assert.Single(row.Selfies);
        Assert.Equal(shot.SpotId, row.Selfies[0].SpotId);
        Assert.Equal(shot.Caption, row.Selfies[0].Caption);
        Assert.Equal(4, row.Selfies[0].AvatarIndex);
    }

    [Fact]
    public void AddSelfie_DedupsBySpotId_OneShotPerLife()
    {
        var store = new MemStore();
        var reg = new GameThreadRegistry(store);
        reg.Touch("uni", "The Deep", 1, 10);

        CapturedSelfie shot = SelfieSpots.CaptureAt("the-deep", "uni", 2, 1)!;
        reg.AddSelfie("uni", shot);
        reg.AddSelfie("uni", shot);           // re-posing the same spot
        reg.AddSelfie("uni", shot with { Caption = "different words, same spot" });

        Assert.Single(reg.Get("uni")!.Selfies); // still one — the ledger doesn't spam
    }

    [Fact]
    public void AddSelfie_UnknownThread_IsNull()
        => Assert.Null(new GameThreadRegistry(new MemStore()).AddSelfie("ghost",
            SelfieSpots.CaptureBeat(SelfieBeats.Deflection, "ghost", 1, 0)!));

    [Fact]
    public void Touch_PreservesTheAlbum_LikeAnAutosave()
    {
        var store = new MemStore();
        var reg = new GameThreadRegistry(store);
        reg.Touch("uni", "Ringside", 1, 10);
        reg.AddSelfie("uni", SelfieSpots.CaptureAt("ringside-exchange", "uni", 1, 1)!);

        // An autosave moves the label/clock but must never wipe the legend ledger.
        reg.Touch("uni", "Ringside Exchange", 2, 20);
        Assert.Single(reg.Get("uni")!.Selfies);
    }

    // ── Per-life reset: a successor inherits NONE of the retiree's selfies, while the memory grows ──

    [Fact]
    public void Succession_ResetsTheAlbum_ButGrowsTheRetiredMemory()
    {
        var store = new MemStore();
        var reg = new GameThreadRegistry(store);
        reg.Touch("uni", "The Deep", 5, 10);
        reg.AddSelfie("uni", SelfieSpots.CaptureAt("the-deep", "uni", 2, 5)!);
        reg.AddSelfie("uni", SelfieSpots.CaptureBeat(SelfieBeats.FirstMonolith, "uni", 2, 5)!);
        Assert.Equal(2, reg.Get("uni")!.Selfies.Count);

        GameThreadInfo successor = reg.IssueSuccessor("uni", retiredSimDay: 6)!;

        Assert.Empty(successor.Selfies);         // the new captain's wall of fame starts blank
        Assert.Single(successor.Retired);        // but the buried captain is remembered
    }

    [Fact]
    public void Succeed_Pure_ClearsSelfies()
    {
        var row = new GameThreadInfo
        {
            Id = "uni",
            CaptainName = "Captain Mabel Vane",
            AvatarIndex = 3,
            Selfies = [SelfieSpots.CaptureBeat(SelfieBeats.Deflection, "uni", 3, 2)!],
        };

        GameThreadInfo next = CaptainSuccession.Succeed(row, retiredSimDay: 4);
        Assert.Empty(next.Selfies);
    }

    // Independent re-derivation of the pool index for the pin test (mirrors SelfieSpots' FNV-1a).
    private static int IndexFor(string seed, int count)
    {
        uint h = 2166136261u;
        foreach (char c in seed)
        {
            h = (h ^ c) * 16777619u;
        }

        return (int)(h % (uint)count);
    }
}
