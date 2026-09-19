using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1199 (2026-09-18) · <b>THE TWO COIN MACHINES IN THE GALLERY</b>, and the beat that still ends at the rail
/// now the rail is out in the crossbar.
///
/// <para>Owner, live: <i>"maybe one of those pay-coin-to-use binoculars common on sightseeing spots with
/// gen-AI image(s) — use with E … same for the vending machine"</i>, and <i>"the station likes to get the
/// tourist money."</i></para>
///
/// <para>Driven through the press a player makes — the deck's own [E] dispatch — rather than by calling the
/// two handlers, so a fixture wired to the wrong arm shows up here as an absence rather than passing.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheGallerysCoinMachinesTests
{
    private const BindingFlags Hidden = TestTree.AnythingAtAll;
    private const string ThreadId = "c81e4a0c39d24e5ba8027c6f1d3e54ff";

    private static string Berth => ObservationWalk.HavenId;

    // ── THE BEAT STILL ENDS WHERE THE PERSON WENT ────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>THE WAIT IS SERVED AT THE GALLERY'S RAIL — AND NOT SHORT OF IT.</b> The rail is out in the
    /// crossbar now, so "he went all the way out to look" means crossing the whole T. A captain who stops at
    /// the throat where the leg meets the hat has not looked yet, and the beat does not fire for him.
    ///
    /// <para>That is the room's own promise kept through a change of shape: the card was always the captain's
    /// to REACH, which is why the walk had to be a room first.</para>
    ///
    /// <para><b>Revert that reddens it:</b> <c>TheRailAt</c> pointed back into the tube — the beat fires from
    /// the throat and the first assert names it.</para>
    /// </summary>
    [Fact]
    public void TheRailIsInTheGalleryAndTheBeatIsNotServedShortOfIt()
    {
        DeckReachability.Point rail = HavenInterior.TheRailAt(Berth)!.Value;
        DeckReachability.Point throat = HavenInterior.TheThroatAt(Berth)!.Value;

        Assert.True(HavenInterior.InTheGallery(Berth, rail.X, rail.Y),
            "the rail is not in the gallery — the wait is being served in the tube.");
        Assert.True(HavenInterior.InTheObservationWalk(Berth, throat.X, throat.Y));

        // Far enough apart that reaching one is not reaching the other: the beat's own gate is an interact
        // radius round the rail, and the throat has to be outside it or the walk in means nothing.
        double dx = rail.X - throat.X, dy = rail.Y - throat.Y;
        Assert.True(Math.Sqrt((dx * dx) + (dy * dy)) > DeckPlan.InteractRadius,
            "the throat is inside the beat's own reach of the rail — the captain never has to walk in.");

        string person = TheTail.ThePersonOfInterest(Berth);
        Pages.Map map = ATailAfoot(person);
        for (int i = 0; i < 900 && double.IsNaN((double)Field(map, "_walkGoneSince")!); i++)
        {
            RunFrames(map, 1);
        }

        Assert.False(double.IsNaN((double)Field(map, "_walkGoneSince")!), "nobody ever went.");
        Set(map, "SimTime", (double)Field(map, "SimTime")! + ObservationWalk.TheWaitSeconds + 1);

        StandCaptainAt(map, throat.X, throat.Y);
        RunFrames(map, 2);
        Assert.Null(Field(map, "_storyCard"));

        StandCaptainAt(map, rail.X, rail.Y);
        RunFrames(map, 2);
        Assert.NotNull(Field(map, "_storyCard"));
    }

    /// <summary>
    /// #1199 · <b>GILT-EYE'S ROUTE ENDS INSIDE THE GALLERY.</b> He gets up from the top the rota seated him
    /// at and the walk he is plotted is a walk to the crossbar — not to the mouth, not to the middle of the
    /// tube — and the frame he comes off the floor on, he is standing in the gallery.
    ///
    /// <para><b>Revert that reddens it:</b> <c>SendThemOutOntoTheWalk</c> aimed at
    /// <c>TheWalksMouthAt</c> — the route ends on the concourse side of the room and both asserts name it.</para>
    /// </summary>
    [Fact]
    public void TheRouteOfThePersonOfInterestEndsInTheGallery()
    {
        string person = TheTail.ThePersonOfInterest(Berth);
        Pages.Map map = ATailAfoot(person);

        object who = ThePersonAfoot(map, person)
            ?? throw new InvalidOperationException("nobody is afoot.");
        object walk = Get(who, "Walk")!;

        // Where the legs are AIMED, asked of the walker's own route rather than of the constant that made it.
        var route = (System.Collections.IEnumerable)Get(walk, "Route")!;
        object? last = null;
        foreach (object leg in route)
        {
            last = leg;
        }

        Assert.NotNull(last);
        double lx = Convert.ToDouble(Get(last!, "X"), System.Globalization.CultureInfo.InvariantCulture);
        double ly = Convert.ToDouble(Get(last!, "Y"), System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(HavenInterior.InTheGallery(Berth, lx, ly),
            $"the route ends at ({lx:0.0},{ly:0.0}), which is not in the gallery.");

        // …and he vanishes THERE: run him to the end of it and the last place he stood is in the crossbar.
        double vx = 0, vy = 0;
        for (int i = 0; i < 3000; i++)
        {
            if (ThePersonAfoot(map, person) is not { } still)
            {
                break;
            }

            object w = Get(still, "Walk")!;
            vx = Convert.ToDouble(Get(w, "X"), System.Globalization.CultureInfo.InvariantCulture);
            vy = Convert.ToDouble(Get(w, "Y"), System.Globalization.CultureInfo.InvariantCulture);
            RunFrames(map, 1);
        }

        Assert.Null(ThePersonAfoot(map, person));
        Assert.True(HavenInterior.InTheGallery(Berth, vx, vy),
            $"he came off the floor at ({vx:0.0},{vy:0.0}), which is not in the gallery.");
    }

    // ── THE COIN MACHINES ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>TWO COINS, AND THE OPTICS SWING ON THE SECOND PRESS.</b> Driven through the deck's own [E]:
    /// the coin leaves the purse exactly once per press, the first look is out over the grey and the second
    /// is straight down through the floor, and the card's PICTURE and WORDS agree about which it was.
    ///
    /// <para><b>Revert that reddens it:</b> <c>LookAt</c> made to return <c>Out</c> always — the second card
    /// shows the earthrise and the alternation assert names it. And the coin line deleted — the purse assert
    /// goes red on the first press.</para>
    /// </summary>
    [Fact]
    public void TheBinocularsTakeTheCoinAndAlternateDeterministically()
    {
        Pages.Map map = AshoreAt(Berth);
        int price = GalleryFixtures.CoinCr(Barkeeps.For(Berth)!.RoundPrice);
        Assert.True(price >= 1, "a look is free.");
        Set(map, "_credits", 10 * price);

        DeckReachability.Point glasses = HavenInterior.TheBinocularsAt(Berth)!.Value;

        var seen = new List<string>();
        for (int press = 0; press < 4; press++)
        {
            int before = (int)Field(map, "_credits")!;
            StandCaptainAt(map, glasses.X, glasses.Y);
            Invoke(map, "InteractAtConsole");

            Assert.Equal(before - price, (int)Field(map, "_credits")!);

            object card = Field(map, "_storyCard")
                ?? throw new InvalidOperationException($"press {press} raised no card.");
            // The card rides as a value tuple, so its SUBJECT is Item2 — named tuple elements are a compiler
            // fiction and there is no `Subject` on the runtime type to read.
            string subject = (string)Get(card, "Item2")!;
            seen.Add(subject);

            GalleryFixtures.Look look = Enum.Parse<GalleryFixtures.Look>(subject);
            Assert.Equal(GalleryFixtures.ArtFor(look),
                StoryBeats.ArtFile(StoryBeats.Beat.TheWalksBinoculars, subject));
            Assert.Equal(GalleryFixtures.BodyFor(look),
                StoryBeats.Caption(StoryBeats.Beat.TheWalksBinoculars, subject));

            Invoke(map, "CloseStoryCard");
        }

        // Out, down, out, down — never a roll, and never the same one twice running.
        Assert.Equal(
            [
                GalleryFixtures.Look.Out.ToString(), GalleryFixtures.Look.Down.ToString(),
                GalleryFixtures.Look.Out.ToString(), GalleryFixtures.Look.Down.ToString(),
            ],
            seen);

        // …and the book has the gist ONCE, filed under the PLACE.
        var book = (IReadOnlyList<FieldNote>)Field(map, "_fieldNotes")!;
        FieldNote note = Assert.Single(book);
        Assert.Equal(GalleryFixtures.BinocularsNoteLine, note.Text);
        Assert.Equal(GalleryFixtures.BinocularsSubjects(), note.Subjects);
    }

    /// <summary>#1199 · <b>THE VENDING MACHINE TAKES THE SAME COIN AND SAYS THE ONE LINE.</b> Both machines
    /// answer, because two machines that are the same machine twice must not disagree about anything.</summary>
    [Fact]
    public void BothVendingMachinesTakeTheCoinAndDropSomething()
    {
        Pages.Map map = AshoreAt(Berth);
        int price = GalleryFixtures.CoinCr(Barkeeps.For(Berth)!.RoundPrice);
        Set(map, "_credits", 10 * price);

        foreach (DeckReachability.Point machine in HavenInterior.TheVendorsAt(Berth))
        {
            int before = (int)Field(map, "_credits")!;
            StandCaptainAt(map, machine.X, machine.Y);
            Invoke(map, "InteractAtConsole");

            Assert.Equal(before - price, (int)Field(map, "_credits")!);
            object card = Field(map, "_storyCard")
                ?? throw new InvalidOperationException("the machine took the coin and showed nothing.");
            Assert.Contains("TheGalleryVendor", card.ToString(), StringComparison.Ordinal);
            Assert.Equal(GalleryFixtures.VendorBody,
                StoryBeats.Caption(StoryBeats.Beat.TheGalleryVendor));
            Invoke(map, "CloseStoryCard");
        }
    }

    /// <summary>
    /// #1199 · <b>AN EMPTY PURSE IS REFUSED, OFF-PLATE, AND NOTHING IS TAKEN.</b> No card, no charge, and the
    /// words are the game's one machine refusal — the souvenir kiosk's own since #379, shared rather than
    /// re-written.
    ///
    /// <para><b>Revert that reddens it:</b> the short-purse arm deleted from <c>TheSlotTakesIt</c> — the
    /// credits go negative and a card comes up over an empty pocket.</para>
    /// </summary>
    [Fact]
    public void AnEmptyPurseIsRefusedWithoutACardAndWithoutACharge()
    {
        Pages.Map map = AshoreAt(Berth);
        int price = GalleryFixtures.CoinCr(Barkeeps.For(Berth)!.RoundPrice);
        Set(map, "_credits", price - 1);

        foreach (DeckReachability.Point spot in
                 new[] { HavenInterior.TheBinocularsAt(Berth)!.Value }
                     .Concat(HavenInterior.TheVendorsAt(Berth)))
        {
            Set(map, "_pulse", default(PulseSlot));
            StandCaptainAt(map, spot.X, spot.Y);
            Invoke(map, "InteractAtConsole");

            Assert.Equal(price - 1, (int)Field(map, "_credits")!);
            Assert.Null(Field(map, "_storyCard"));
            Assert.Equal(GalleryFixtures.ShortLine(price), PulseSaying(map));
            Assert.Contains(CoinSlot.ShortLine, PulseSaying(map)!, StringComparison.Ordinal);
        }

        // …and the book is untouched: a refusal is not something that happened.
        Assert.Empty((IReadOnlyList<FieldNote>)Field(map, "_fieldNotes")!);
    }

    /// <summary>#1199 · THE FOUR PLATES ARE ON DISK AND IN THE MANIFEST. The art is wired to the code by
    /// name, and a canvas listed nowhere is a canvas nobody will ever re-make when it is lost.</summary>
    [Fact]
    public void TheGallerysPlatesAreShippedAndListed()
    {
        string root = RepoRoot();
        string manifest = File.ReadAllText(Path.Combine(root, "docs", "art-manifest-visions.md"));

        foreach (string url in new[]
                 {
                     GalleryFixtures.BinocularsOutArtUrl,
                     GalleryFixtures.BinocularsDownArtUrl,
                     GalleryFixtures.CafeteriaArtUrl,
                     GalleryFixtures.CafeteriaFloorArtUrl,
                 })
        {
            Assert.True(
                File.Exists(Path.Combine(root, "src", "SpaceSails.Client", "wwwroot", url.Replace('/', Path.DirectorySeparatorChar))),
                $"{url} is referenced by the code and is not on disk.");
            Assert.Contains(url, manifest, StringComparison.Ordinal);
        }
    }

    /// <summary>#1199 · A SEAT IN THE GALLERY LOOKS OUT OF IT. The stool's own rule (#756/#759): standing you
    /// are looking at the room, and sitting down the window is in front of you. Asked of the seat the page
    /// hands the sitting, so a table that lost its view would show up as a null here.</summary>
    [Fact]
    public void ATableInTheGalleryCarriesTheViewAsItsSeatedArt()
    {
        Pages.Map map = AshoreAt(Berth);
        foreach (DeckReachability.Point top in HavenInterior.GalleryTops(Berth))
        {
            StandCaptainAt(map, top.X, top.Y);
            object seat = Invoke(map, "TheBarTopUnderfoot")
                ?? throw new InvalidOperationException($"no seat at the gallery table at {top.X:0.0},{top.Y:0.0}.");

            Assert.Equal(GalleryFixtures.CafeteriaArtUrl, (string?)Get(seat, "Window"));
            Assert.Equal(SittingAlone.BarSetting(ObservationWalk.Plate), (string?)Get(seat, "Setting"));
            Assert.StartsWith("gallery:", (string)Get(seat, "Key")!, StringComparison.Ordinal);
        }

        // …and a top in the BAR is unchanged: it is still the bar's own setting, and it has no window.
        HavenInterior.BarFloor bar = HavenInterior.BarBand(Berth)!.Value;
        DeckPlan deck = HavenInterior.DockedDeck(Berth)!;
        DeckReachability.Point freeTop = bar.Tops.First(t => deck.Consoles.Any(c =>
            c.Kind == DeckPlan.ConsoleKind.BarTop
            && Math.Abs(c.X - t.X) < 0.5 && Math.Abs(c.Y - t.Y) < 0.5));
        StandCaptainAt(map, freeTop.X, freeTop.Y);
        object barSeat = Invoke(map, "TheBarTopUnderfoot")
            ?? throw new InvalidOperationException("no free top in the bar to compare against.");
        Assert.Null((string?)Get(barSeat, "Window"));
        Assert.Equal(SittingAlone.BarSetting(HavenInterior.BarNameOf(Berth)), (string?)Get(barSeat, "Setting"));
    }

    // ── PLUMBING (the walk's own harness, borrowed) ──────────────────────────────────────────────────────

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SpaceSails.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("no repository root above the test binary.");
    }

    private static Pages.Map AshoreAt(string berth, long watch = 0)
    {
        var map = new Pages.Map();
        Set(map, "SimTime", watch * PatronRota.WatchSeconds);
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_dockedHavenId", berth);
        Set(map, "_deckMode", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Set(map, "_repCheat", (bool?)false);
        Invoke(map, "SetDeckForDock", berth);
        Invoke(map, "StandAtTheBarThreshold");
        return map;
    }

    private static Pages.Map ATailAfoot(string person)
    {
        for (int evening = 0; evening < 48; evening++)
        {
            Pages.Map map = AshoreAt(ObservationWalk.HavenId, evening);
            var watch = (long)Invoke(map, "get_BarWatch")!;
            Set(map, "SimTime",
                (watch * PatronRota.WatchSeconds) + (PatronRota.WatchSeconds * Egress.LastCallFraction) + 1);
            StandCaptainAt(map, 2.5, 6);

            for (int i = 0; i < 40; i++)
            {
                RunFrames(map, 1);
                if (ThePersonAfoot(map, person) is { } who
                    && Get(who, "For")!.ToString() == "WalkingTheRoute")
                {
                    return map;
                }
            }
        }

        throw new InvalidOperationException("forty-eight evenings and the tail never set off.");
    }

    private static object? ThePersonAfoot(Pages.Map map, string person)
    {
        foreach (object who in (System.Collections.IList)Field(map, "_barAfoot")!)
        {
            if (string.Equals((string)Get(who, "Who")!, person, StringComparison.Ordinal))
            {
                return who;
            }
        }

        return null;
    }

    private static void StandCaptainAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
        Set(map, "_lookPrevAvatarX", x);
        Set(map, "_lookPrevAvatarY", y);
    }

    private static void RunFrames(Pages.Map map, int frames, double dt = 0.1)
    {
        for (int i = 0; i < frames; i++)
        {
            Set(map, "SimTime", (double)Field(map, "SimTime")! + dt);
            Set(map, "_lastTimestampMs", (double?)(((double?)Field(map, "_lastTimestampMs") ?? 0) + (dt * 1000)));
            Invoke(map, "AdvanceBarWalkers", dt);
        }
    }

    private static string? PulseSaying(Pages.Map map)
    {
        object pulse = Field(map, "_pulse")!;
        return pulse.GetType().GetProperty("Message", Hidden)!.GetValue(pulse) as string;
    }

    private static FieldInfo FieldOf(string name) =>
        typeof(Pages.Map).GetField(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no `{name}` — this guard is reading a dead name.");

    private static object? Field(Pages.Map map, string name) => FieldOf(name).GetValue(map);

    private static void Set(Pages.Map map, string name, object? value) => FieldOf(name).SetValue(map, value);

    private static object? Get(object o, string member) =>
        o.GetType().GetProperty(member, Hidden)?.GetValue(o)
        ?? o.GetType().GetField(member, Hidden)?.GetValue(o);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo call = typeof(Pages.Map).GetMethod(method, Hidden)
            ?? throw new InvalidOperationException($"Map has no `{method}` — this guard is reading a dead name.");
        try
        {
            return call.Invoke(map, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
