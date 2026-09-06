using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #804 · <b>FIND SOMEBODY ELSE'S PASS ON ONE MOON, AND RIDE TO THE MOON IT WAS ISSUED BY.</b>
///
/// <para>Owner, in the issue's own point 4: <i>"THE BADGE, kept in the Fletch wallet: our own badge once we
/// get a gig, or FALSE IDs we discovered."</i> The judgement has existed since #836
/// (<see cref="WalletChoice.Outcome.WrongSite"/>); until this lane nothing authored ever dealt the paper it
/// was written for, so the rung was reachable only from a dev cheat.</para>
///
/// <para><b>These guards DRIVE the page.</b> They stand a live <see cref="Pages.Map"/> in the drawer's own
/// room on the mess floor, press the shipped verb, and then look at the wallet — so what is proved is the
/// game's own behaviour rather than a source file that still reads plausibly. The one source-shape guard in
/// the file is the one whose failure IS a shape: two roads to the same object.</para>
///
/// <para>Each guard below was watched go RED against a deliberate revert; the revert is named in its
/// remarks.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheFalseIdIsInTheDrawerTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "scenarios")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"no scenarios/ above {AppContext.BaseDirectory}");
    }

    /// <summary>Every ground in the shipped world a captain can actually stand on — the same rule the page's
    /// own roster uses (<c>Map.TheGroundsThisWorldHas</c>), asked of the scenario rather than of a list typed
    /// in here.</summary>
    private static List<string> ShippedGrounds() =>
        [.. TestTree.Sol.Bodies
            .Where(b => Enum.TryParse(b.Kind, ignoreCase: true, out BodyKind kind)
                        && ShuttleExcursion.IsLandableSurface(kind)
                        && !Derelict.TryParseWreckId(b.Id, out _))
            .Select(b => b.Id)];

    /// <summary>The shipped grounds that really keep a foreign pass, in the scenario's own order.</summary>
    private static List<string> GroundsThatKeepOne() =>
        [.. ShippedGrounds().Where(id => FoundPass.RoomFor(id) is not null)];

    // ── The world can tell pass from fail ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE SHIPPED WORLD REALLY CARRIES ONE — AND NOT EVERY GROUND DOES.</b>
    ///
    /// <para>This is the guard the whole feature turns on and it is not a formality: the first cut of this
    /// lane put the drawer in room 5 of the works floor, passed every rarity sweep over generated grounds,
    /// and dealt a pass on <b>none of the ten moons in sol.json</b> — a feature silently dead in the only
    /// world that ships, with everything green. (Room 5 does not exist on Enceladus, and the gate missed the
    /// rest.) A sweep over invented body ids can never see that.</para>
    ///
    /// <para><b>RED</b> by raising <see cref="FoundPass.OneInSites"/> far enough that no canon moon passes
    /// the gate, or by putting the drawer back on a room index the canon grounds do not have.</para>
    /// </summary>
    [Fact]
    public void TheShippedWorldKeepsSomeAndNotAll()
    {
        List<string> all = ShippedGrounds();
        List<string> keep = GroundsThatKeepOne();

        Assert.True(all.Count >= 8, $"only {all.Count} landable grounds in sol.json; this proves little.");
        Assert.True(keep.Count > 0,
            "not one ground in the shipped world keeps a foreign pass, so nothing a player can reach ever "
            + "deals one and WalletChoice.Outcome.WrongSite is still a rung only a cheat can climb.");
        Assert.True(keep.Count < all.Count,
            $"every one of the {all.Count} shipped grounds keeps a pass — that is a supply, not a find.");
    }

    // ── The drawer, driven ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>PRESS [E] IN THE DRAWER'S OWN ROOM AND THE PASS IS IN THE WALLET</b> — the shipped verb, on a live
    /// page, on a real moon, with nothing about the find simulated.
    ///
    /// <para>And then the two things that make it a false ID rather than a badge: it is minted for a
    /// DIFFERENT ground of this same world (so the captain can go there), and the room is struck off, so a
    /// second search does not deal a second one.</para>
    ///
    /// <para><b>RED</b> by deleting the <c>FoundPass.IsHere</c> block from <c>HiveHaulInteract</c>: the
    /// wallet comes back empty.</para>
    /// </summary>
    [Fact]
    public void TheDrawerHandsOverAPassIssuedSomewhereElse()
    {
        string here = Assert.Single(GroundsThatKeepOne().Take(1));
        (Pages.Map map, object _) = InTheDrawersRoom(here);

        Invoke(map, "HiveHaulInteract");

        Satchel.Item pass = TheOnlyPassIn(map);
        string mintedFor = Assert.IsType<string>(PatrolBeat.SiteOfBadge(pass.Id), exactMatch: false);

        Assert.NotEqual(here, mintedFor);
        Assert.Contains(mintedFor, ShippedGrounds());

        // …and the room is gone through. Searching it again deals nothing, because a drawer is not a tap.
        Invoke(map, "HiveHaulInteract");
        Assert.Single(AllPassesIn(map));
    }

    /// <summary>
    /// <b>SHOW IT HERE AND IT IS REFUSED; RIDE TO THE SITE THAT ISSUED IT AND IT IS A PASS.</b> The paper is
    /// the one the drawer really handed over, the hand is the one the wallet would really put it in
    /// (<see cref="WalletChoice.DefaultFor"/>), and the sentence is the one the card would really carry
    /// (<see cref="PatrolBeat.TheGuardReads"/>). Nothing about the read is re-implemented here.
    ///
    /// <para><b>RED</b> by having <c>FoundPass.MintedElsewhere</c> return <c>PatrolBeat.Badge(hereBodyId)</c>
    /// — the first arm stops being a refusal. (Merely dropping the roster's <i>skip this ground</i> clause is
    /// NOT enough to redden a driven case, because the draw usually lands elsewhere anyway; that clause is
    /// held by <c>TheFalseIdTests.ItIsAlwaysSomebodyElsesSite</c>, which sweeps every ground against sixty-
    /// four seeds. Watched, and written down rather than assumed.)</para>
    /// </summary>
    [Fact]
    public void ItIsRefusedOnTheMoonYouFoundItOnAndWorksOnTheMoonThatIssuedIt()
    {
        string here = GroundsThatKeepOne()[0];
        (Pages.Map map, object _) = InTheDrawersRoom(here);
        Invoke(map, "HiveHaulInteract");

        var wallet = (IReadOnlyList<Satchel.Item>)Read(map, "_satchel")!;
        Satchel.Item pass = TheOnlyPassIn(map);
        string mintedFor = PatrolBeat.SiteOfBadge(pass.Id)!;

        // What the wallet puts in the hand at a challenge, on each floor, without the captain choosing.
        Satchel.Item? shownHere = WalletChoice.DefaultFor(here, wallet, null);
        Satchel.Item? shownThere = WalletChoice.DefaultFor(mintedFor, wallet, null);
        Assert.Equal(pass, shownHere);
        Assert.Equal(pass, shownThere);

        Assert.Equal(WalletChoice.Outcome.WrongSite, WalletChoice.WhatHappens(here, -2, 0L, shownHere));
        Assert.Equal(WalletChoice.Outcome.Worked, WalletChoice.WhatHappens(mintedFor, -2, 0L, shownThere));

        Assert.False(PatrolBeat.TheGuardReads(here, -2, 0L, "A ROUND", shownHere, false).Satisfied);
        Assert.True(PatrolBeat.TheGuardReads(mintedFor, -2, 0L, "A ROUND", shownThere, false).Satisfied);

        // …and the wrong-site arm's consequence is the one that was already written. Nothing new is added to
        // what a refusal costs — the issue's own scope for this lane.
        Assert.Equal(
            PatrolBeat.EscortLine,
            PatrolBeat.TheGuardReads(here, -2, 0L, "A ROUND", shownHere, false).Consequence);
    }

    /// <summary>
    /// <b>THE PLATE IS THE MINTING SITE'S OWN FACE</b>, and it is what the captain's own record keeps about
    /// the find. #590's grammar, read off the pass that really came out of the drawer — so a captain looking
    /// at the row can tell whose building it is, which is the entire informed-choice law (#836).
    ///
    /// <para>The canon pass of 2026-09-06 moved the plate off the PULSE and left it on the book, because the
    /// authored sentence takes the one slot (#693) — <see cref="TheLineIsSaidOnceWhenItGoesIntoTheWallet"/>
    /// is that half. Both halves are still measured off the live page, not off the source.</para>
    ///
    /// <para><b>RED</b> by composing the drawer's filed line out of anything but <c>FoundPass.Plate</c>.
    /// </para>
    /// </summary>
    [Fact]
    public void ThePlateSaysWhoseBuildingItIs()
    {
        string here = GroundsThatKeepOne()[0];
        (Pages.Map map, object _) = InTheDrawersRoom(here);
        Invoke(map, "HiveHaulInteract");

        Satchel.Item pass = TheOnlyPassIn(map);
        string plate = FoundPass.Plate(pass);

        Assert.Contains(
            BodyNames.Designation(PatrolBeat.SiteOfBadge(pass.Id)), plate, StringComparison.Ordinal);
        Assert.DoesNotContain(BodyNames.Designation(here), plate, StringComparison.Ordinal);

        // …and the captain's book keeps the plate WORD FOR WORD, once — nothing composed around it.
        var filed = (IReadOnlyList<FieldNote>)Read(map, "_fieldNotes")!;
        Assert.Single(filed, n => n.Text == plate);
    }

    /// <summary>
    /// <b>THE ONE AUTHORED SENTENCE, SAID ONCE, WHERE THE PAPER GOES INTO THE WALLET.</b>
    ///
    /// <para>#1143 shipped this feature with a <c>// FABLE: line needed</c> and no sentence at all; the canon
    /// pass of 2026-09-06 wrote one, and this is where the shipped game is asked whether it says it. The page
    /// is DRIVEN — the verb is pressed in the drawer's own room — and the pulse slot is then read off the
    /// page, so the text asserted here is the text a captain gets.</para>
    ///
    /// <para><b>VERBATIM</b> against the sentence as the issue authored it, spelled out in this file rather
    /// than referred to through <c>FoundPass.TakenLine</c>: a guard that compares a constant with itself is
    /// green whatever the constant says (this repo's fifth named bug class), so the words are written out
    /// here and the constant has to match THEM.</para>
    ///
    /// <para><b>ONCE</b>, and both meanings: one line in the pulse slot for the one press, and one call in
    /// the whole client — a sentence a second seam could also say is a sentence that can arrive twice.</para>
    ///
    /// <para><b>RED</b> by changing a character of the line, by saying it anywhere else, or by putting the
    /// plate back on the pulse.</para>
    /// </summary>
    [Fact]
    public void TheLineIsSaidOnceWhenItGoesIntoTheWallet()
    {
        const string authored =
            "Somebody's old site pass, still warm from a locker. The photograph is not you. Nobody has ever checked.";

        Assert.Equal(authored, FoundPass.TakenLine);

        string here = GroundsThatKeepOne()[0];
        (Pages.Map map, object _) = InTheDrawersRoom(here);
        Invoke(map, "HiveHaulInteract");

        Assert.Equal(authored, ((PulseSlot)Read(map, "_pulse")!).Message);

        var filed = (IReadOnlyList<FieldNote>)Read(map, "_fieldNotes")!;
        Assert.Single(filed, n => n.Text == authored);

        // …and §8's reserved word and the fifteen beside it are nowhere near it. A line about a stolen pass
        // is exactly where an explanation would creep in; the reason it works is "nobody has ever checked",
        // which names nothing and nobody (#649, comprehension without acceptance).
        foreach (string bad in new[]
                 {
                     "monolith", "old one", "reever", "restore", "backup", "revive", "resurrect", "clone",
                     "slave", "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
                 })
        {
            Assert.DoesNotContain(bad, authored, StringComparison.OrdinalIgnoreCase);
        }

        // …and it is said in ONE place in the whole client. The take is the only seam that may say it.
        Assert.Single(Calls(
            string.Concat(Directory.EnumerateFiles(
                    Path.Combine(RepoRoot(), "src", "SpaceSails.Client"), "*.cs", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(File.ReadAllText)),
            "ShowAndFile(FoundPass.TakenLine"));
    }

    // ── One producer, two roads ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE CHEAT AND THE DRAWER ARE ONE MINT.</b> Source-shape, because the failure is a shape: two places
    /// that build a foreign pass would be two answers to what a false ID is, and the dev path would drift off
    /// the real one the first afternoon somebody tuned either. That drift is exactly how the reconciliation
    /// audit found this rung reachable only from <c>Map.Surface.Cheats.cs</c> in the first place.
    ///
    /// <para><b>RED</b> by having the cheat compose <c>PatrolBeat.Badge(someOtherBody)</c> of its own.</para>
    /// </summary>
    [Fact]
    public void TheDevStartAndTheDrawerGoThroughTheSameProducer()
    {
        string cheats = Source("Map.Surface.Cheats.cs");
        string hive = Source("Map.Surface.Hive.cs");
        string mint = Source("Map.FoundPass.cs");

        // The dev start reaches the mint directly; the drawer reaches it through the take that lives beside
        // it (the search verb may not add to the satchel — #615's ordering law).
        Assert.Contains("AFalseIdFoundAt(", cheats, StringComparison.Ordinal);
        Assert.Contains("TheDrawerHandsOverAFalseId(", hive, StringComparison.Ordinal);
        Assert.Contains("AFalseIdFoundAt(", mint, StringComparison.Ordinal);
        Assert.Contains("FoundPass.MintedElsewhere(", mint, StringComparison.Ordinal);

        // …and the mint is written once. A second call of Core's producer anywhere in the client would be a
        // second roster and a second answer to what a false ID is.
        Assert.Single(Calls(
            string.Concat(Directory.EnumerateFiles(
                    Path.Combine(RepoRoot(), "src", "SpaceSails.Client"), "*.cs", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(File.ReadAllText)),
            "FoundPass.MintedElsewhere("));

        // …and NOBODY else in the client mints a badge for a body that is not the one underfoot. The site's
        // own pass is `PatrolBeat.Badge(bodyId)` in two places (the gig, and the cheat that stands in for it);
        // anything else would be a second false-ID producer.
        foreach (string file in Directory.EnumerateFiles(
                     Path.Combine(RepoRoot(), "src", "SpaceSails.Client"), "*.cs",
                     SearchOption.AllDirectories))
        {
            foreach (string call in Calls(File.ReadAllText(file), "PatrolBeat.Badge("))
            {
                Assert.True(call is "bodyId" or "passBody",
                    $"{Path.GetFileName(file)} mints a pass for `{call}` — if that is not the ground "
                    + "underfoot it is a second answer to what a false ID is, and FoundPass is the only "
                    + "place allowed to give one.");
            }
        }
    }

    /// <summary>The argument of every call of <paramref name="what"/> in <paramref name="source"/>, as
    /// written. Crude on purpose: these are one-token arguments and a parser would be a second thing to keep
    /// right.</summary>
    private static IEnumerable<string> Calls(string source, string what)
    {
        int at = 0;
        while ((at = source.IndexOf(what, at, StringComparison.Ordinal)) >= 0)
        {
            int open = at + what.Length;
            int close = source.IndexOf(')', open);
            if (close > open)
            {
                yield return source[open..close].Trim();
            }
            at = open;
        }
    }

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live page standing on the drawer's own room on the mess floor of <paramref name="body"/>,
    /// with the world's real ephemeris under it — because the roster the pass is minted from is the
    /// scenario's own bodies and a page without one would deal nothing and prove nothing.</summary>
    private static (Pages.Map Map, object Ex) InTheDrawersRoom(string body)
    {
        (int Level, int RoomIndex) at = FoundPass.RoomFor(body)
            ?? throw new InvalidOperationException($"{body} keeps no pass — this bench has drifted.");

        var map = new Pages.Map();
        typeof(ComponentBase)
            .GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        Set(map, "_scenarioName", TestTree.Sol.Name);
        Set(map, "_ephemeris", ephemeris);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, at.Level);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Invoke(map, "RebuildSurfaceDeck");

        (double X, double Y) centre = UndergroundComplex
            .Build(body, at.Level, MoonSurface.ExpeditionField()).RoomCentres[at.RoomIndex];
        Set(map, "_avatarX", centre.X);
        Set(map, "_avatarY", centre.Y);

        return (map, ex);
    }

    private static IReadOnlyList<Satchel.Item> AllPassesIn(Pages.Map map) =>
        [.. ((IReadOnlyList<Satchel.Item>)Read(map, "_satchel")!)
            .Where(i => i.Kind == Satchel.Kind.Badge)];

    private static Satchel.Item TheOnlyPassIn(Pages.Map map)
    {
        IReadOnlyList<Satchel.Item> passes = AllPassesIn(map);
        Assert.True(passes.Count == 1,
            $"the wallet holds {passes.Count} passes after one press; the drawer dealt "
            + (passes.Count == 0 ? "nothing at all." : "more than one."));
        return passes[0];
    }

    /// <summary>#251 · Through <see cref="MapMarkup"/> rather than straight off disk: the Hive surface is
    /// four partials now, and a Contains over one of them would stop looking at three quarters of the
    /// drawer. Every other path this helper is handed still comes back as <c>File.ReadAllText</c>.</summary>
    private static string Source(string file) =>
        MapMarkup.Read(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .SetValue(o, value);

    private static object? Read(object o, string field) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .GetValue(o);

    private static void Invoke(object o, string method) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, null);
}
