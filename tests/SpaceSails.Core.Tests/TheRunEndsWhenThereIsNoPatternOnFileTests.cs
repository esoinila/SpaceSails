namespace SpaceSails.Core.Tests;

/// <summary>
/// #640 · THE FIRST PERMADEATH THIS GAME HAS EVER HAD, and the Core half of it.
///
/// <para>Owner ruling 2026-09-17, option A: a captain who pulls the purge handle on a cold-archive node
/// holding their OWN pattern has nothing on file, so their next death is the last one — no successor, no
/// clinic, and <see cref="ArchiveNode.NoRestoreLine"/> finally says something true. That line shipped
/// authored, wording-tested and READ BY NOTHING for two months, precisely because printing POLICY CLOSED
/// on a card that then resurrected you is the sentence-vs-sim bug class this project has paid for three
/// times. These guards hold the three things that make the sentence true and keep it true.</para>
///
/// <list type="number">
/// <item><b>Only your own jar closes it.</b> A stranger's costs nothing you will ever learn about; a
/// delinquent's pays a heat thread off.</item>
/// <item><b>The flag survives the save.</b> The handle and the death can be hours and a reload apart —
/// that IS the beat — so a bit that did not round-trip would silently give the captain their life back.</item>
/// <item><b>A closed thread is closed.</b> Continue may not lead into a run with no captain in it, the
/// shelf must still list it, and no other universe may be touched.</item>
/// </list>
/// </summary>
public class TheRunEndsWhenThereIsNoPatternOnFileTests
{
    private sealed class MemStore : ISlotStore
    {
        private readonly Dictionary<string, string> _map = new(StringComparer.Ordinal);
        public string? Read(string key) => _map.TryGetValue(key, out string? v) ? v : null;
        public void Write(string key, string value) => _map[key] = value;
        public void Clear(string key) => _map.Remove(key);
    }

    // ── 1 · Only your own pattern closes the policy. ──

    [Theory]
    [InlineData(ArchiveNode.Resident.Stranger, false)]
    [InlineData(ArchiveNode.Resident.DelinquentSubscriber, false)]
    [InlineData(ArchiveNode.Resident.YourOwn, true)]
    public void OnlyYourOwnJarClosesThePolicy(ArchiveNode.Resident who, bool closes)
    {
        Assert.Equal(closes, ArchiveNode.ClosesThePolicy(who));
    }

    [Fact]
    public void TheCollarCanStillBeReadBeforeYouPull()
    {
        // The ruling is only defensible because the information was always purchasable: a confrontation at
        // the Noticed band or worse reads the jar's collar, and on your own jar the collar says your own
        // policy number. If that ever stops being true, option A stops being a gamble and becomes a trap.
        Assert.True(ArchiveNode.ReadsTheCollar(ArchiveNode.Band.Noticed));
        Assert.True(ArchiveNode.ReadsTheCollar(ArchiveNode.Band.Inside));
        Assert.Contains("your own policy number",
            ArchiveNode.CollarLine(ArchiveNode.Resident.YourOwn), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NothingOnTheHandleWarnsYou()
    {
        // The owner's standing instruction for this beat: the label was always honest, so nothing is added
        // to it. The legend says what the handle does and stops; the line the captain reads at the moment
        // of pulling names no resident and forecasts nothing.
        Assert.Equal("PURGE NODE — RESIDENT PATTERN NOT RECOVERABLE", ArchiveNode.SwitchLegend);

        // What must stay out is any statement about WHOSE pattern it was or what it will cost the captain
        // later. ("That is the part to be careful about" stays: it is about the relief, which is the trap,
        // and it says nothing about the policy.)
        foreach (string word in new[]
                 {
                     "warning", "are you sure", "your own", "policy", "insurance", "resurrect",
                     "rebirth", "last", "permanent",
                 })
        {
            Assert.DoesNotContain(word, ArchiveNode.PurgeLine, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(word, ArchiveNode.PurgedPlate.Caption, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── 2 · The flag survives the save, because the handle and the death are far apart. ──

    [Fact]
    public void ThePurgedPolicySurvivesTheVault()
    {
        var pulled = new NebulaProgress();
        pulled.Assemble("fine-print");
        Assert.True(pulled.MarkPolicyClosed());
        Assert.False(pulled.MarkPolicyClosed()); // idempotent: one handle, one closure

        NebulaSection section = VaultMapper.ToSection(pulled);
        Assert.True(section.PolicyClosed);

        var reloaded = new NebulaProgress();
        VaultMapper.Apply(section, reloaded);

        Assert.True(reloaded.PolicyClosed);
        Assert.True(reloaded.Has("fine-print")); // and the rest of the arc came back with it
    }

    [Fact]
    public void ASaveWrittenBeforeTheRulingStillHasAPatternOnFile()
    {
        // Every vault on anybody's machine before 2026-09-17. A default that read "closed" would end
        // thousands of live runs on the next death, which is the one thing worse than the beat not shipping.
        var progress = new NebulaProgress();
        VaultMapper.Apply(null, progress);
        Assert.False(progress.PolicyClosed);

        var legacy = new NebulaProgress();
        VaultMapper.Apply(new NebulaSection { AssembledFragmentIds = ["fine-print"] }, legacy);
        Assert.False(legacy.PolicyClosed);
    }

    [Fact]
    public void ANewVoyageHasAPatternOnFileAgain()
    {
        var progress = new NebulaProgress();
        progress.MarkPolicyClosed();
        progress.Assemble("fine-print");

        progress.Clear(); // a new voyage is a new universe, and its captain never touched that handle

        Assert.False(progress.PolicyClosed);
        Assert.Equal(0, progress.Count);
    }

    // ── 3 · A closed thread is closed — and nothing else is. ──

    private static GameThreadRegistry TwoThreads(out string doomed, out string other)
    {
        var registry = new GameThreadRegistry(new MemStore());
        other = "thread-other";
        doomed = "thread-doomed";
        registry.Touch(other, "Selene Gate", 3, 1_000);
        registry.Touch(doomed, "a hull that died before you were born", 9, 2_000);
        return registry;
    }

    [Fact]
    public void ClosingAThreadTakesItOffContinueAndLeavesItOnTheShelf()
    {
        GameThreadRegistry registry = TwoThreads(out string doomed, out string other);
        Assert.Equal(doomed, registry.Active()?.Id); // the run we are in, before the last death

        GameThreadInfo? closed = registry.Close(doomed);
        Assert.True(closed?.Ended);

        // Continue does not lead into a dead run…
        Assert.Equal(other, registry.Active()?.Id);
        Assert.Equal(other, registry.Newest()?.Id);

        // …and the shelf still has it, with everything on it.
        Assert.Contains(registry.List(), t => t.Id == doomed && t.Ended);
        Assert.Equal("a hull that died before you were born", registry.Get(doomed)?.Where);
    }

    [Fact]
    public void TheOtherUniverseIsNotTouched()
    {
        GameThreadRegistry registry = TwoThreads(out string doomed, out string other);
        GameThreadInfo before = registry.Get(other)!;

        registry.Close(doomed);

        GameThreadInfo after = registry.Get(other)!;
        Assert.False(after.Ended);
        Assert.Equal(before.Where, after.Where);
        Assert.Equal(before.SimDay, after.SimDay);
        Assert.Equal(before.LastActiveTicks, after.LastActiveTicks);
        Assert.Equal(before.CreatedTicks, after.CreatedTicks);
        Assert.Equal(before.CaptainName, after.CaptainName);
        Assert.Equal(before.AvatarIndex, after.AvatarIndex);
        Assert.Equal(before.Retired.Count, after.Retired.Count);
        Assert.Equal(before.Selfies.Count, after.Selfies.Count);
    }

    [Fact]
    public void ClosingTheOnlyThreadLeavesNothingToContinue()
    {
        var registry = new GameThreadRegistry(new MemStore());
        registry.Touch("only", "the last berth", 1, 100);

        registry.Close("only");

        Assert.Null(registry.Active());
        Assert.Null(registry.Newest());
        Assert.Single(registry.List()); // the captain is still on the shelf; there is simply no run
    }

    [Fact]
    public void NoLaterWritePutsTheRunBackOnItsFeet()
    {
        // The client puts its autosave pen down the moment the thread closes — but a registry that lost the
        // bit on the next Touch would be a second answer to the same question, and the one write that got
        // through would quietly hand Continue a universe with no captain in it.
        GameThreadRegistry registry = TwoThreads(out string doomed, out _);
        registry.Close(doomed);

        registry.Touch(doomed, "somewhere else entirely", 12, 9_999);

        Assert.True(registry.Get(doomed)?.Ended);
        Assert.NotEqual(doomed, registry.Active()?.Id);
    }

    [Fact]
    public void ClosingTwiceEndsOneRun()
    {
        GameThreadRegistry registry = TwoThreads(out string doomed, out _);
        GameThreadInfo? first = registry.Close(doomed);
        GameThreadInfo? again = registry.Close(doomed);

        Assert.Equal(first?.Id, again?.Id);
        Assert.True(again?.Ended);
        Assert.Equal(first?.LastActiveTicks, again?.LastActiveTicks); // and no clock moved on the second pull
        Assert.Single(registry.List(), t => t.Ended);
    }

    [Fact]
    public void ClosingAThreadThatIsNotThereIsNotAnError()
    {
        // A legacy, unindexed run has no row to close. The card still reads the line, because the line is
        // about the policy and not about the bookkeeping.
        GameThreadRegistry registry = TwoThreads(out _, out _);
        Assert.Null(registry.Close("no-such-thread"));
    }

    [Fact]
    public void AShelfWithNoEndedThreadIsWrittenExactlyAsItWasBefore()
    {
        // The registry is the index every universe is found through, and #563 made the rule: a new field
        // that is false is not written at all, so this build's registry file is byte-identical to its
        // predecessor's for every player who never pulled that handle.
        var store = new MemStore();
        var registry = new GameThreadRegistry(store);
        registry.Touch("plain", "Selene Gate", 2, 500);

        string? written = store.Read(GameThreadRegistry.RegistryKey);
        Assert.NotNull(written);
        Assert.DoesNotContain("Ended", written, StringComparison.Ordinal);

        registry.Close("plain");
        Assert.Contains("Ended", store.Read(GameThreadRegistry.RegistryKey)!, StringComparison.Ordinal);
    }
}
