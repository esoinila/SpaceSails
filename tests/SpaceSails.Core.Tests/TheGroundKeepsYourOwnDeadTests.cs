using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #563 · <b>BREADCRUMBS COME FROM YOUR OWN LINEAGE FIRST</b> — the Core half.
///
/// <para>Owner ruling, 2026-09-13, closing the last of #563's three open questions, verbatim: <i>"I love the
/// own lineage. If not enough material, fill in with strangers, preferably NPCs we know something
/// about."</i> And the loop it feeds, #455: <i>"after pirate insurance rebirth you can come see if your loot
/// is still there."</i></para>
///
/// <h3>What can go silently wrong here, which is what each case is for</h3>
/// <list type="bullet">
///   <item>the grave is written for a death that HAS no ground (an impact, the void) and a later captain
///   walks up to a suit lying where his predecessor flew a ship into a moon;</item>
///   <item>the grave rides the registry but not the file, so the one place this feature can be met — a LATER
///   trip, in a LATER session — never happens;</item>
///   <item>the registry's wire format moves for every save in the world, graves or no graves;</item>
///   <item>the note RESTATES the roster's line or the death card's clause instead of calling them, and the
///   day either is rewritten the book quietly disagrees with the card that taught it;</item>
///   <item>the stranger is a generated nobody, or a dead man, or the machine bolted to the galley counter —
///   all three of which read as a name and none of which is one.</item>
/// </list>
///
/// <para><b>Every case below was watched RED</b> by breaking the shipped rule it guards; the pull request
/// lists which break reddened which name.</para>
/// </summary>
public class TheGroundKeepsYourOwnDeadTests
{
    private sealed class MemStore : ISlotStore
    {
        private readonly Dictionary<string, string> _map = new(StringComparer.Ordinal);
        public string? Read(string key) => _map.TryGetValue(key, out string? v) ? v : null;
        public void Write(string key, string value) => _map[key] = value;
        public void Clear(string key) => _map.Remove(key);
        public string Raw => _map[GameThreadRegistry.RegistryKey];
    }

    private const string Body = "phobos";

    /// <summary>A real landing site's real salt, asked of the generator rather than typed in — the fifth
    /// named bug class is a world that cannot tell pass from fail, and a made-up salt is exactly that.</summary>
    private static string SaltAt(int index) => LandingSites.At(Body, index).LayoutSalt;

    private static CaptainGrave GraveAt(DeathCause cause, int siteIndex = 0, double x = 12.5, double y = -7.25)
        => new(Body, SaltAt(siteIndex), x, y, cause);

    // ── 1 · THE RECORD, AND THE FILE IT HAS TO SURVIVE ───────────────────────────────────────────────

    /// <summary>The whole feature is a LATER trip, so the record is worth nothing until it has been through
    /// the store and come back. Written through the shipping succession, read back through a SECOND registry
    /// over the same store — a fresh object with no memory of the first.</summary>
    [Fact]
    public void TheGround_RoundTripsTheRegistry()
    {
        var store = new MemStore();
        new GameThreadRegistry(store).Touch("u-1", "Phobos", 3, 1000);

        CaptainGrave grave = GraveAt(DeathCause.Reevers, siteIndex: 1, x: -4.5, y: 88.25);
        Assert.NotNull(new GameThreadRegistry(store).IssueSuccessor("u-1", 42, grave));

        RetiredCaptain back = Assert.Single(new GameThreadRegistry(store).Get("u-1")!.Retired);
        Assert.Equal(grave, back.Grave);
        Assert.Equal(42, back.SimDay);

        // …and the tile is the one the ground's own lattice puts that spot on, not a number stored beside it.
        Assert.Equal(SurfaceTiles.At(-4.5, 88.25), back.Grave!.Tile);
    }

    /// <summary>
    /// A SAVE WITH NO GRAVES IN IT IS THE FILE ITS PREDECESSOR WROTE. The registry is the index every
    /// universe in the game is found through, and every player on earth has one; a new field that writes
    /// itself as <c>"Grave":null</c> into all of them would rewrite the lot on the first autosave.
    ///
    /// <para>Pinned as the literal bytes rather than as "it round-trips": the pre-#563 shape of a retiree is
    /// exactly <c>{"Name":…,"SimDay":…}</c>, and that is what is asserted.</para>
    /// </summary>
    [Fact]
    public void ASaveWithNoGraves_IsByteIdenticalToTheOneTheOldBuildWrote()
    {
        var store = new MemStore();
        var reg = new GameThreadRegistry(store);
        reg.Touch("u-1", "Phobos", 3, 1000);
        reg.IssueSuccessor("u-1", 42);           // a death with no ground — the ordinary case

        string wire = store.Raw;
        RetiredCaptain kept = Assert.Single(reg.Get("u-1")!.Retired);

        Assert.DoesNotContain("Grave", wire, StringComparison.Ordinal);
        Assert.Contains($"\"Retired\":[{{\"Name\":\"{kept.Name}\",\"SimDay\":42}}]", wire, StringComparison.Ordinal);

        // …and an ordinary autosave afterwards does not smuggle it in either.
        reg.Touch("u-1", "Phobos", 4, 2000);
        Assert.DoesNotContain("Grave", store.Raw, StringComparison.Ordinal);
    }

    /// <summary>A row written before any of this existed reads back as a captain with no known grave —
    /// never as a game that will not load. The vault is tolerant everywhere else and this is no
    /// exception.</summary>
    [Fact]
    public void ALegacyRow_ReadsBackWithNoGraveAtAll()
    {
        var store = new MemStore();
        store.Write(GameThreadRegistry.RegistryKey,
            "{\"Threads\":[{\"Id\":\"u-1\",\"Where\":\"Phobos\",\"SimDay\":3,\"LastActiveTicks\":10,"
            + "\"CreatedTicks\":10,\"CaptainName\":\"Captain Mabel Vane\",\"AvatarIndex\":2,"
            + "\"Retired\":[{\"Name\":\"Captain Otho Renn\",\"SimDay\":9}]}],\"ActiveId\":\"u-1\"}");

        RetiredCaptain old = Assert.Single(new GameThreadRegistry(store).Get("u-1")!.Retired);
        Assert.Equal("Captain Otho Renn", old.Name);
        Assert.Null(old.Grave);
    }

    // ── 2 · WHICH DEATHS HAVE A GROUND ───────────────────────────────────────────────────────────────

    /// <summary>
    /// THE PLACE IS WRITTEN ONLY FOR CAUSES THAT CARRY A GROUND, and the list is not kept here: the question
    /// is asked of <see cref="DeathNarration.CanHappen"/>, which already states, cause by cause and with its
    /// reasons, what can happen to a captain standing on a landing party's regolith.
    ///
    /// <para>Walked over EVERY value of the enum, so a ninth cause added next year is this test's problem
    /// rather than a player's.</para>
    /// </summary>
    [Fact]
    public void ThePlaceIsKept_OnlyForACauseThatCanHappenToALandingParty()
    {
        var thread = new GameThreadInfo { Id = "u-1", CaptainName = "Captain Mabel Vane", AvatarIndex = 1 };

        foreach (DeathCause cause in Enum.GetValues<DeathCause>())
        {
            bool onAGround = DeathNarration.CanHappen(cause, DeathPlace.LandingParty);
            RetiredCaptain kept = Assert.Single(
                CaptainSuccession.Succeed(thread, 5, GraveAt(cause)).Retired);

            Assert.Equal(onAGround, kept.Grave is not null);
            Assert.Equal(onAGround, CaptainGrave.CanRecord(cause));
        }

        // The two ends of that sweep, named, so a reader can see what it is claiming: the Old Ones reach you
        // out there; flying a ship into a moon is something you do from the controls.
        Assert.True(CaptainGrave.CanRecord(DeathCause.Reevers));
        Assert.True(CaptainGrave.CanRecord(DeathCause.Suffocated));
        Assert.False(CaptainGrave.CanRecord(DeathCause.Impact));
        Assert.False(CaptainGrave.CanRecord(DeathCause.Void));
    }

    // ── 3 · THE MARK ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>The mark is at the grave and stamped with the day the license changed hands — no scatter, no
    /// "now", and the tile derived from the position exactly as every other mark on this ground derives
    /// its own.</summary>
    [Fact]
    public void TheMark_IsWhereHeFellAndDatedWhenHeFell()
    {
        var gone = new RetiredCaptain("Captain Otho Renn", 9) { Grave = GraveAt(DeathCause.Reevers) };
        GroundMemory.Scar mark = LineageMark.MarkFor(gone);

        Assert.Equal(GroundMemory.ScarKind.Suit, mark.What);
        Assert.Equal(gone.Grave!.X, mark.X);
        Assert.Equal(gone.Grave.Y, mark.Y);
        Assert.Equal(SurfaceTiles.At(gone.Grave.X, gone.Grave.Y), mark.Tile);
        Assert.Equal(9 * GroundMemory.DaySeconds, mark.AtSimTime);

        // …and it reads its age off the ground's own three bands, never off a fourth sentence.
        Assert.Equal("Still smoking.", GroundMemory.AgeLine(mark.AtSimTime, 9.5 * GroundMemory.DaySeconds));
        Assert.Equal("Dusted over. Days old.", GroundMemory.AgeLine(mark.AtSimTime, 12 * GroundMemory.DaySeconds));
        Assert.Equal("Regolith-dusted. Weeks old.", GroundMemory.AgeLine(mark.AtSimTime, 30 * GroundMemory.DaySeconds));
    }

    /// <summary>ONE GROUND, AND NOT THE ONE NEXT DOOR. A body offers 2–4 landing sites and every one of them
    /// rebuilds the same local coordinate frame (#320/#650), so a grave filtered on the body alone would put
    /// a predecessor's body on a plain the man never walked — which is precisely the bug #650 fixed for the
    /// ✗ itself.</summary>
    [Fact]
    public void TheMark_IsOnTheRecordedGroundAndNoOther()
    {
        var gone = new RetiredCaptain("Captain Otho Renn", 9) { Grave = GraveAt(DeathCause.Reevers, siteIndex: 0) };
        IReadOnlyList<RetiredCaptain> roster = [gone];

        Assert.Single(LineageMark.BuriedOn(roster, Body, SaltAt(0)));
        Assert.Empty(LineageMark.BuriedOn(roster, Body, SaltAt(1)));
        Assert.Empty(LineageMark.BuriedOn(roster, "luna", SaltAt(0)));
        Assert.Empty(LineageMark.BuriedOn([new RetiredCaptain("Captain Nobody", 1)], Body, SaltAt(0)));
    }

    // ── 4 · THE WORDS, WHICH ARE QUOTED AND NEVER RESTATED ───────────────────────────────────────────

    /// <summary>
    /// THE NOTE CALLS THE ROSTER'S LINE AND THE DEATH CARD'S CLAUSE. Asserted by CALLING both of them, which
    /// is the point: a rewrite of either has to move this sentence, and a transcription in
    /// <see cref="LineageMark"/> would pass a test that compared two typed-in strings while the book and the
    /// card quietly disagreed.
    /// </summary>
    [Fact]
    public void TheNote_QuotesTheRosterLineAndTheCauseWord()
    {
        foreach (DeathCause cause in Enum.GetValues<DeathCause>().Where(CaptainGrave.CanRecord))
        {
            var gone = new RetiredCaptain("Captain Otho Renn", 42) { Grave = GraveAt(cause) };
            string note = LineageMark.YoursNote(gone);

            Assert.Contains(CaptainSuccession.RetiredLine(gone), note, StringComparison.Ordinal);
            Assert.Contains(DeathNarration.CauseWord(cause), note, StringComparison.Ordinal);
            Assert.Contains(gone.Name, note, StringComparison.Ordinal);

            // The whole authored shape, once, so a template edited by accident is caught as well as a
            // quotation dropped on purpose.
            Assert.Equal(
                "A suit in the regolith with the license still clipped to it. "
                + $"{gone.Name}. {CaptainSuccession.RetiredLine(gone)} — and after that, here. "
                + $"{DeathNarration.CauseWord(cause)}.",
                note);
        }
    }

    /// <summary>The cause word is the death card's own headline with the plate's lead stripped off — a
    /// projection, not a second pool. Walked over every cause, and pinned against
    /// <see cref="DeathNarration.Headline"/> by calling it.</summary>
    [Fact]
    public void TheCauseWord_IsTheDeathCardsOwnClause()
    {
        foreach (DeathCause cause in Enum.GetValues<DeathCause>())
        {
            string word = DeathNarration.CauseWord(cause);
            string plate = DeathNarration.Headline(cause);

            Assert.DoesNotContain("WHAT HAPPENED", word, StringComparison.Ordinal);
            Assert.EndsWith(word[1..], plate, StringComparison.Ordinal);
            Assert.False(word.EndsWith('.'), "the template supplies the full stop");
            Assert.True(char.IsUpper(word[0]), "the clause opens a sentence in the note");
        }

        Assert.Equal("The Old Ones took you", DeathNarration.CauseWord(DeathCause.Reevers));
        Assert.Equal("The air ran out", DeathNarration.CauseWord(DeathCause.Suffocated));
    }

    // ── 5 · THE STRANGERS WHO FILL IN ────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE FILL-IN IS SOMEBODY THIS GAME HAS NAMED, and the same somebody every time you come back.
    ///
    /// <para>The owner's qualifier is the design — <i>"preferably NPCs we know something about"</i> — so the
    /// pool is asserted against the files that PRINT those names, by calling them. A generated nobody would
    /// read as a name and be nothing.</para>
    /// </summary>
    [Fact]
    public void TheStranger_IsDeterministicPerGroundAndAlwaysSomebodyTheGameHasNamed()
    {
        Assert.Equal(LineageMark.StrangerFor(Body, SaltAt(0)), LineageMark.StrangerFor(Body, SaltAt(0)));

        // Walked over real grounds — bodies and their real sites — rather than over made-up keys.
        var drawn = new List<string>();
        foreach (string body in new[] { "phobos", "deimos", "luna", "io", "europa", "titan" })
        {
            for (int i = 0; i < 3; i++)
            {
                string who = LineageMark.StrangerFor(body, LandingSites.At(body, i).LayoutSalt);
                Assert.Contains(who, LineageMark.Cast);
                drawn.Add(who);
            }
        }

        // …and the draw actually spreads. One name for every ground in the solar system would pass every
        // other assertion in this method and be a bug you could only see by playing.
        Assert.True(drawn.Distinct().Count() > 1, "every ground in the game names the same stranger");
    }

    /// <summary>
    /// THE CAST IS ALIVE, HUMAN, AND CANON. Three exclusions, each of which would read perfectly and be a
    /// lie: Hollis Grey has been a face in a photograph for years (<c>OldCrew</c>'s own <c>Living</c> flag,
    /// read rather than copied), B-7V is bolted to the galley counter and sold as one lot with it (#1022),
    /// and the company sends Kolt where Harlan Fess will not go — so a tag reading Fess on a moon
    /// contradicts, in one word, the reason the other salesman exists.
    /// </summary>
    [Fact]
    public void TheCast_ExcludesTheDeadTheMachineAndTheManWhoWouldNotCome()
    {
        foreach (OldCrew.Shipmate dead in OldCrew.Pool.Where(s => !s.Living))
        {
            Assert.DoesNotContain(dead.Name, LineageMark.Cast);
        }

        Assert.DoesNotContain(LineageMark.Cast, n => TheTender.Plate.Contains(n, StringComparison.Ordinal));
        Assert.DoesNotContain(NebulaRep.RepName, LineageMark.Cast);

        // …and every living member of that crew IS in it, so the filter is a filter and not an accident.
        foreach (OldCrew.Shipmate living in OldCrew.Pool.Where(s => s.Living))
        {
            Assert.Contains(living.Name, LineageMark.Cast);
        }

        Assert.Contains(HardcaseRep.RepName, LineageMark.Cast);   // the one the game has actually put on regolith
        Assert.Contains(FinderCase.DisplayName, LineageMark.Cast);
        Assert.Contains(OracleRant.FullName, LineageMark.Cast);
    }

    // ── 6 · THE PROSE DISCIPLINE ─────────────────────────────────────────────────────────────────────

    /// <summary>Everything this file can say is enumerated, and §8's reserved word is nowhere in it. The
    /// sweep has to see the cause word of EVERY recordable cause and the name of EVERY member of the cast,
    /// because a pool walked to one entry is a pool half-swept.</summary>
    [Fact]
    public void TheTemplates_AreEnumeratedAndNeverSpeakTheReservedWord()
    {
        string[] said = [.. LineageMark.AllProse()];

        Assert.Contains(LineageMark.Plate, said);
        Assert.DoesNotContain(said, s => s.Contains("monolith", StringComparison.OrdinalIgnoreCase));

        foreach (DeathCause cause in Enum.GetValues<DeathCause>().Where(CaptainGrave.CanRecord))
        {
            Assert.Contains(said, s => s.Contains(DeathNarration.CauseWord(cause), StringComparison.Ordinal));
        }
        foreach (string who in LineageMark.Cast)
        {
            Assert.Contains(LineageMark.StrangerNote(who), said);
        }
    }

    // ── 7 · WHAT MUST NOT HAVE MOVED ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE THIRD MARK KIND DID NOT DISTURB THE OTHER TWO. <c>Word(ScarKind)</c> was a ternary and is now a
    /// switch; as a ternary a suit would have spelled itself <c>"drybot"</c> and two different marks on one
    /// spot would have shared one row. Pinned by round-tripping each kind through the ledger's own key.
    /// </summary>
    [Fact]
    public void TheNewMarkKind_MovesNothingTheForensicsAlreadyPin()
    {
        var keys = new List<string>();
        foreach (GroundMemory.ScarKind kind in Enum.GetValues<GroundMemory.ScarKind>())
        {
            // Three kinds on ONE spot at ONE moment: the only thing that can tell them apart is the word,
            // which is exactly what the ternary got wrong.
            var scar = new GroundMemory.Scar(kind, 3.0, 4.0, 100.0);
            string key = GroundMemory.ScarKey(Body, SaltAt(0), scar);
            Assert.True(GroundMemory.TryReadScarKey(key, Body, SaltAt(0), out GroundMemory.Scar back));
            Assert.Equal(kind, back.What);
            keys.Add(key);
        }

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());

        // …and the latch over a mark is that mark's own key and never a neighbour's.
        Assert.Equal(keys.Count, keys.Select(GroundMemory.ReadKey).Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(GroundMemory.ReadKey(keys[0]), keys);

        // …and the rival visit's own arithmetic is untouched: the marks it leaves, the roll behind them and
        // the moment they are stamped with are all still what its own guards pin. Asked of the shipping
        // method rather than restated.
        RivalVisit.Evidence left = RivalVisit.LeftBehind("chest-1", reeverLevel: 2, 10, 10, periodIndex: 5);
        Assert.Equal(GroundMemory.ScarKind.Pit, left.Pit.What);
        Assert.Equal(RivalVisit.MomentOf(5), left.AtSimTime);
        Assert.Equal(RivalVisit.HusksLeftBy(left.Roll), left.Husks.Count);
        Assert.DoesNotContain(left.Scars, s => s.What == GroundMemory.ScarKind.Suit);
    }

    // ── 8 · REBIRTH PARITY (#455 / #1072) ────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE LOOP THE OWNER ASKED FOR, GUARDED RATHER THAN BUILT.</b> #455: <i>"after pirate insurance
    /// rebirth you can come see if your loot is still there."</i>
    ///
    /// <para>Nothing new is wired for it — the chest already survives its captain (<c>TreasureCache</c> lives
    /// off-ship and the wake never touches it) and the grave now survives him too. What this pins is that the
    /// two address THE SAME GROUND: a cache carries a body and a SITE INDEX, a grave carries a body and a
    /// SITE SALT, and if those two ever stopped resolving to one place the mark and the money would sit on
    /// different plains of the same moon and the loop would silently be nothing.</para>
    /// </summary>
    [Fact]
    public void TheMarkAndTheCacheHeBuried_AreOnOneGround()
    {
        const int site = 1;
        var buried = new TreasureCache(
            "cache-1", Body, "the leaning slab", "N by NE", 40, 900, [],
            BuriedSimTime: 42 * GroundMemory.DaySeconds, Owner: "Captain Otho Renn", PlayerOwned: true,
            SiteIndex: site, Buried: true);
        var gone = new RetiredCaptain("Captain Otho Renn", 42) { Grave = GraveAt(DeathCause.Reevers, site) };

        // The one resolution the client makes (GroundSaltFor) — a cache's site index to the ground ledger's
        // salt — lands on the very salt the grave was written with.
        Assert.Equal(gone.Grave!.SiteSalt, LandingSites.At(buried.BodyId, buried.SiteIndex!.Value).LayoutSalt);
        Assert.True(gone.Grave.IsOn(buried.BodyId, LandingSites.At(buried.BodyId, site).LayoutSalt));

        // …so the successor walking that ground finds the mark on it, and the chest is still his to dig.
        Assert.Single(LineageMark.BuriedOn([gone], buried.BodyId, gone.Grave.SiteSalt));
        Assert.True(buried.PlayerOwned);

        // And the coexistence is on ONE ground and not on all of them: a chest he buried on the site next
        // door is a different plain of the same moon, and the mark does not follow it there.
        var elsewhere = buried with { Id = "cache-2", SiteIndex = 0 };
        Assert.False(gone.Grave.IsOn(
            elsewhere.BodyId, LandingSites.At(elsewhere.BodyId, elsewhere.SiteIndex!.Value).LayoutSalt));
        Assert.Empty(LineageMark.BuriedOn(
            [gone], elsewhere.BodyId, LandingSites.At(elsewhere.BodyId, 0).LayoutSalt));
    }
}
