using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #688 · LEAVE IT HERE, AND FIND IT AGAIN. Owner, live on the deep site: <i>"The keycard story is already
/// big, but no way to drop stuff."</i>
///
/// <para>This is brand-new behaviour, so <b>these guards cannot be shown RED against the old build</b> — there
/// was no drop verb to break. Said plainly rather than dressed up: the redness in this pass lives on the
/// capacity math and on the three client source-shape guards. What these tests do is pin the one law the verb
/// must never violate, which is #615's and is older than the verb:</para>
///
/// <para><b>Leaving never destroys.</b> Within the walk, a thing set down is a find again — searching that
/// spot offers it back, and anything the satchel still cannot take stays lying there rather than evaporating
/// on the way to the ground.</para>
///
/// <para>Also pinned here: #688's door filter, <see cref="SatchelTry.CanOffer"/>. Owner: <i>"only suggest
/// those at doors. Or tools, but not just like some papers."</i></para>
/// </summary>
public sealed class WhatYouLeaveIsStillThereTests
{
    private const string Spot = "-3:4:7";

    private static Satchel.Item Paper(int i) => new(Satchel.Kind.Paper, $"hive:luna:-3:{i}");
    private static Satchel.Item Bulky(int i) => new(Satchel.Kind.Relic, $"pallet-{i}");

    private static Satchel.Item Card(string body, int band) =>
        new(Satchel.Kind.Authority, new UndergroundComplex.AuthorityCard(body, band).Id);

    [Fact]
    public void SomethingSetDownIsOfferedBackByTheNextSearchOfThatSpot()
    {
        // The round trip, end to end, as the captain performs it: it is in the pocket, they leave it, it is
        // out of the pocket and on the ground, they search the spot, it is theirs again.
        var ground = new LeftBehind();
        IReadOnlyList<Satchel.Item> pocket = Satchel.Add([], Paper(1));
        Satchel.Item leaving = pocket[0];

        pocket = Satchel.Remove(pocket, leaving.Kind, leaving.Id, leaving.Count);
        ground.Leave(Spot, leaving);

        Assert.Empty(pocket);
        Assert.True(ground.AnythingAt(Spot));

        LeftBehind.Recovery back = ground.PickUp(Spot, pocket);

        Assert.Equal(leaving, Assert.Single(back.Taken));
        Assert.Equal(1, Satchel.CountOf(back.Pocket, leaving.Kind, leaving.Id));
        Assert.Empty(back.StillThere);

        // And the ground is empty afterwards: picking a thing up twice would be a duplication bug wearing
        // this feature's clothes.
        Assert.False(ground.AnythingAt(Spot));
        Assert.Empty(ground.PickUp(Spot, back.Pocket).Taken);
    }

    [Fact]
    public void WhatWillNotFitStaysLyingThereRatherThanEvaporating()
    {
        // The case that occurs the first time a captain uses this under pressure: they leave two bulky things
        // to make room for one, take one back, and the other must still be on the floor. #615's law is not
        // "the drop is reversible", it is "leaving never destroys" — and a pickup that silently ate the
        // remainder would be the same silent drop #678 was filed on, one verb later.
        var ground = new LeftBehind();
        ground.Leave(Spot, Bulky(90));
        ground.Leave(Spot, Bulky(91));

        // A satchel with exactly one bulky space in it.
        IReadOnlyList<Satchel.Item> pocket = [];
        for (int i = 0; i < Satchel.PocketCapacity - 1; i++)
        {
            pocket = Satchel.Add(pocket, Bulky(i));
        }

        LeftBehind.Recovery back = ground.PickUp(Spot, pocket);

        Assert.Single(back.Taken);
        Assert.Single(back.StillThere);
        Assert.True(ground.AnythingAt(Spot), "the leftover was destroyed by the act of picking up its neighbour.");
        Assert.Equal(back.StillThere, ground.At(Spot));

        // Make room the way the captain would, and the remainder is still there to be had.
        IReadOnlyList<Satchel.Item> roomier = Satchel.Remove(back.Pocket, Satchel.Kind.Relic, "pallet-0");
        LeftBehind.Recovery second = ground.PickUp(Spot, roomier);
        Assert.Single(second.Taken);
        Assert.False(ground.AnythingAt(Spot));
    }

    [Fact]
    public void RoundsLeaveAndReturnAsTheWholeStack()
    {
        // Six rounds is one thing you are carrying (#603), so it is one thing you put down. Leaving them a
        // round at a time would be inventory management, which is the game this satchel exists not to be.
        var ground = new LeftBehind();
        var six = new Satchel.Item(Satchel.Kind.Rounds, Ammunition.LabTwoStage.Id, 6);
        ground.Leave(Spot, six);
        ground.Leave(Spot, six);

        Assert.Equal(12, Assert.Single(ground.At(Spot)).Count);

        LeftBehind.Recovery back = ground.PickUp(Spot, []);
        Assert.Equal(12, Satchel.CountOf(back.Pocket, Satchel.Kind.Rounds, Ammunition.LabTwoStage.Id));
    }

    [Fact]
    public void OneSpotIsNotAnother()
    {
        // A thing left on B3 is not waiting on the surface, and a thing left four squares east is not under
        // your boots. The spot key carries the FLOOR as well as the square, because the two grids share
        // coordinates and a captain would otherwise find a relic on a floor they had never walked.
        var ground = new LeftBehind();
        ground.Leave(LeftBehind.SpotKey(-3, 4, 7), Paper(1));

        Assert.True(ground.AnythingAt(LeftBehind.SpotKey(-3, 4, 7)));
        Assert.False(ground.AnythingAt(LeftBehind.SpotKey(0, 4, 7)));
        Assert.False(ground.AnythingAt(LeftBehind.SpotKey(-3, 5, 7)));
        Assert.False(ground.AnythingAt(LeftBehind.SpotKey(-2, 4, 7)));
    }

    [Fact]
    public void TheLineSaysWhereItWasLeft()
    {
        // Owner's ask, verbatim in effect: the line says where it was left. And it is honest about the one
        // thing a v1 drop cannot promise — that what you abandon on a rock survives the shuttle lifting.
        string said = LeftBehind.LeaveLine("📋 a shipping manifest", "on the floor of B3");
        Assert.Contains("on the floor of B3", said, StringComparison.Ordinal);
        Assert.Contains("📋 a shipping manifest", said, StringComparison.Ordinal);
        Assert.Contains("again", said, StringComparison.OrdinalIgnoreCase);

        // #688 refinement · When a gist was filed, the line says BOTH facts — where it was left, and that
        // what it said came along anyway. A book entry the captain is never told about is a feature they
        // will never find.
        string filed = LeftBehind.LeaveLine("📋 a shipping manifest", "on the floor of B3", gistFiled: true);
        Assert.Contains("on the floor of B3", filed, StringComparison.Ordinal);
        Assert.Contains("field book", filed, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("field book", said, StringComparison.OrdinalIgnoreCase);

        // Coming back for it names what came back, and names what did not — a recovery that quietly left
        // something on the floor without saying so is a silent drop by another route.
        string got = LeftBehind.FoundAgainLine(["⭕ measurements"], ["🔫 12 loose rounds"]);
        Assert.Contains("⭕ measurements", got, StringComparison.Ordinal);
        Assert.Contains("🔫 12 loose rounds", got, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(LeftBehind.FoundAgainLine([], ["⭕ measurements"])));
    }

    [Fact]
    public void LeavingADocumentFilesITSGISTAndLeavingRoundsFilesNOTHING()
    {
        // #688 refinement, owner: leaving a Paper or Dirt item files its gist to the field book before it
        // leaves the pocket. New behaviour — there was no drop verb yesterday — so this guard cannot be shown
        // RED against the old build, and saying so is worth more than pretending otherwise.
        //
        // The law it pins: EXACTLY ONE note, carrying the thing's own title, for a document; and NOTHING at
        // all for a handful of rounds, because there is no gist to ammunition and a book entry for one would
        // be a receipt.
        const string where = "on the floor of B3";
        IReadOnlyList<FieldNote> book = [];

        foreach (int n in new[] { 1, 3, 5 })
        {
            var paper = new Satchel.Item(Satchel.Kind.Paper, $"hive:luna:-3:{n}");
            string? gist = LeftBehind.GistOf(paper, where);

            Assert.NotNull(gist);
            Assert.Contains(FieldClue.Title(paper.Id), gist!, StringComparison.Ordinal);
            Assert.Contains(FieldClue.Label(FieldClue.CertaintyOf(paper.Id)), gist!, StringComparison.Ordinal);
            Assert.Contains(FieldClue.Line(FieldClue.CertaintyOf(paper.Id)), gist!, StringComparison.Ordinal);
            Assert.Contains(where, gist!, StringComparison.Ordinal);

            int before = book.Count;
            book = FieldNotes.Append(book, new FieldNote(gist!, 0.0, "Luna · the Hive", "📋"));
            Assert.Equal(before + 1, book.Count);
        }

        // A file on somebody is paper in the hand too, and it files — it simply has no title of its own,
        // because the game has never printed a heading on one.
        Assert.NotNull(LeftBehind.GistOf(new Satchel.Item(Satchel.Kind.Dirt, "hive:luna:-3:2"), where));

        // And everything that is not a document files nothing whatsoever.
        Assert.Null(LeftBehind.GistOf(new Satchel.Item(Satchel.Kind.Rounds, Ammunition.LabTwoStage.Id, 6), where));
        Assert.Null(LeftBehind.GistOf(Bulky(4), where));
        Assert.Null(LeftBehind.GistOf(Card("luna", 1), where));

        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            // Stated as the compartment law rather than as a list of kinds, so a Kind appended later cannot
            // quietly start or stop filing without somebody deciding it should.
            Assert.Equal(
                Satchel.CompartmentOf(kind) == Satchel.Compartment.Sleeve,
                LeftBehind.GistOf(new Satchel.Item(kind, "hive:luna:-3:2", 6), where) is not null);
        }
    }

    [Fact]
    public void AtADoorOnlyAKeyIsOfferable()
    {
        // #688 · Owner: "Let's make a bigger story point about finding any kind of key or keycard and only
        // suggest those at doors. Or tools, but not just like some papers." A shipping manifest carrying a
        // live "try it →" at a bulkhead was never a story; it was forty offers dangled at a wall to hide the
        // one that mattered.
        foreach (SatchelTry.Target target in Enum.GetValues<SatchelTry.Target>())
        {
            foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
            {
                bool offerable = SatchelTry.CanOffer(kind, target);

                if (SatchelTry.IsDoor(target))
                {
                    Assert.Equal(kind == Satchel.Kind.Authority, offerable);
                }
                else
                {
                    // Away from a door nothing narrows — this is a rule about DOORS, not a new law about
                    // pockets. The tracker and the dry sentry decide for themselves, and they always did.
                    Assert.True(offerable, $"{kind} stopped being offerable to {target}, which #688 never asked for.");
                }
            }
        }

        // Rounds stay inert at a door DELIBERATELY: shooting a lock open (#610) is an owner call nobody has
        // made, and an offerable round here would pre-wire the answer to it.
        Assert.False(SatchelTry.CanOffer(Satchel.Kind.Rounds, SatchelTry.Target.RoomDoor));
        Assert.True(SatchelTry.CanOffer(Satchel.Kind.Rounds, SatchelTry.Target.Sentry));

        // The three doors, named, so a target added later has to come past this list.
        Assert.True(SatchelTry.IsDoor(SatchelTry.Target.ShaftGate));
        Assert.True(SatchelTry.IsDoor(SatchelTry.Target.SealedWay));
        Assert.True(SatchelTry.IsDoor(SatchelTry.Target.RoomDoor));
        Assert.False(SatchelTry.IsDoor(SatchelTry.Target.Tracker));
        Assert.False(SatchelTry.IsDoor(SatchelTry.Target.Sentry));
    }

    [Fact]
    public void EveryRefusalTheDoorsUsedToGiveIsSTILLTHERE()
    {
        // What changed is what the satchel OFFERS, not one syllable of what it says. The wrong-shaft and
        // wrong-site readings (#679) are the best storytelling the Hive has, and a captain who has an
        // authority still gets every one of them. This guard exists because the tempting way to implement
        // #688 is to make the non-key branches of SatchelTry return nothing — which would delete the
        // refusals along with the offers.
        foreach (SatchelTry.Target target in Enum.GetValues<SatchelTry.Target>())
        {
            foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
            {
                SatchelTry.Outcome said = SatchelTry.Offer(
                    new Satchel.Item(kind, kind == Satchel.Kind.Authority ? Card("titan", 0).Id : "x", 6),
                    target, Card("luna", 1).Id);
                Assert.False(string.IsNullOrWhiteSpace(said.Line),
                    $"{kind} offered to {target} answers with nothing — a silent refusal is indistinguishable " +
                    "from a bug, which is #603's founding law.");
            }
        }

        // And the wrong-card readings specifically: same site wrong shaft, and another site entirely.
        Assert.Contains("shaft number is not", SatchelTry.Offer(
            Card("luna", 2), SatchelTry.Target.ShaftGate, Card("luna", 1).Id).Line,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("somebody else's business", SatchelTry.Offer(
            Card("titan", 0), SatchelTry.Target.ShaftGate, Card("luna", 1).Id).Line,
            StringComparison.OrdinalIgnoreCase);
    }
}
