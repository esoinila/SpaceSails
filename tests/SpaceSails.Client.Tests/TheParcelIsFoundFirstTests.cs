using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #711 slice 1 · <b>THE CLIENT HALF: A HAVEN HANDS ONE OVER, A ROUND FINDS IT FIRST, AND A SETTLED OUTFIT
/// STOPS ASKING.</b>
///
/// <para><b>What this file can and cannot prove.</b> The same split every patrol guard in this suite keeps
/// (<see cref="TheGuardsCatchYouTests"/>, <c>TheHeatIsBankedOnceTests</c>) and for the same reason: the
/// round lives in a partial class on a razor page no test can instantiate. The JUDGEMENT is Core's and is
/// driven end to end in <c>TheFineThatClosesAFolderTests</c>; what is pinned here is the wiring the page
/// owns and Core cannot see — the ORDER the parcel is asked about in, the arm that ends the read, the
/// silence where an answer is already on file, and the desk row that hands one over without moving
/// coin.</para>
///
/// <para>Every one of the four was reverted in the page and watched go red; the reverts are named on each
/// guard.</para>
/// </summary>
public sealed class TheParcelIsFoundFirstTests
{
    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestTree.RepoRoot(), .. parts]));

    private static int Count(string haystack, string needle)
    {
        int n = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal);
             i >= 0;
             i = haystack.IndexOf(needle, i + 1, StringComparison.Ordinal))
        {
            n++;
        }
        return n;
    }

    private static string Challenge() =>
        Read("src", "SpaceSails.Client", "Pages", "Patrol", "Patrol.Challenge.cs");

    /// <summary>The CODE, with the design record taken out of it. These files are half comment by weight and
    /// every name this guard counts is discussed in prose beside the line that uses it — so a count over the
    /// raw text would be a count of the explanation, and would go green the day somebody deleted the call and
    /// left the paragraph.</summary>
    private static string Code(string source)
    {
        string noBlock = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(noBlock, "//[^\n]*", " ");
    }

    // ── (1) A HAVEN HANDS ONE OVER ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>ONE ROW ON ONE DESK, GATED WHERE IT APPLIES, AND NOT A CREDIT MOVES THROUGH IT.</b>
    ///
    /// <para>Four clauses, and each is a thing that would be wrong in a different way: the row is on the
    /// dark-web desk (not the cargo market, where everything is on a manifest by construction); it is drawn
    /// only where it applies rather than shown and denied (#212); it is capped at ONE PER HULL through
    /// <see cref="UnlistedParcel.Held"/> rather than a flag of its own; and the press moves no coin, because
    /// nobody pays a hauler up front for a box they are not listing.</para>
    ///
    /// <para>The parameter chain is walked whole — Map.razor → FlowColumn → DeskPanels → DarkWeb — because a
    /// row wired at three of four levels is a row nobody can press, and the razor generator would not say
    /// so.</para>
    ///
    /// <para><b>RED</b> by dropping <c>!UnlistedParcel.Held(_satchel)</c> from <c>ParcelOnOffer</c>: <i>the
    /// desk's row is not capped at one per hull</i>. <b>RED</b> by dropping the <c>ParcelOnOffer</c> wiring
    /// from Map.razor: <i>the parcel row is wired at 3 of 4 levels</i>. And <b>RED</b> by making the press
    /// charge for it (<c>_credits -= 50;</c>): <i>coin moves through the parcel's row</i>.</para>
    /// </summary>
    [Fact]
    public void TheDeskHandsOneOverAndNoCoinMoves()
    {
        string map = Read("src", "SpaceSails.Client", "Pages", "Map.UnlistedParcel.cs");
        string desk = Read("src", "SpaceSails.Client", "Pages", "Stations", "DarkWeb.razor");

        // It is the DARK-WEB desk's row and nowhere else — the one place in the game where "unlisted" is
        // a true description of a handover.
        Assert.Contains("@if (ParcelOnOffer)", desk, StringComparison.Ordinal);
        Assert.Contains("UnlistedParcel.Plate", desk, StringComparison.Ordinal);
        Assert.Contains("UnlistedParcel.LookCardLine", desk, StringComparison.Ordinal);
        Assert.Contains("UnlistedParcel.DeskVerb", desk, StringComparison.Ordinal);

        // The desk never composes prose or arithmetic of its own about a thing it does not own.
        Assert.DoesNotContain("Unlisted parcel", desk, StringComparison.Ordinal);

        // Drawn where it applies: the desk open, nothing already aboard, and room in the pocket.
        Assert.Contains("DarkWebCanTrade()", map, StringComparison.Ordinal);
        Assert.Contains("!UnlistedParcel.Held(_satchel)", map, StringComparison.Ordinal);
        Assert.Contains("Core.Satchel.CanTake(_satchel, parcel)", map, StringComparison.Ordinal);

        // …and ONE PER HULL is the possession, never a flag: nothing in the page keeps a second answer.
        Assert.Equal(0, Count(Code(map), "_parcelTaken"));
        Assert.Equal(0, Count(Code(map), "ThisPortHasDealt"));

        // NOT A CREDIT MOVES. The purse appears in this file exactly once — in PayTheFine, which is the
        // fine and not the counter — and never inside the taking.
        int taking = map.IndexOf("private void TakeTheUnlistedParcel()", StringComparison.Ordinal);
        Assert.True(taking > 0, "the taking verb is gone — this guard is watching a method that moved.");
        Assert.Equal(0, Count(Code(map[taking..]), "_credits"));

        // The chain, all four levels of it.
        foreach ((string what, string[] path) in new (string, string[])[]
        {
            ("Map.razor", ["src", "SpaceSails.Client", "Pages", "Map.razor"]),
            ("FlowColumn.razor", ["src", "SpaceSails.Client", "Pages", "Map", "FlowColumn.razor"]),
            ("DeskPanels.razor", ["src", "SpaceSails.Client", "Pages", "Map", "DeskPanels.razor"]),
        })
        {
            string text = Read(path);
            Assert.True(
                text.Contains("ParcelOnOffer", StringComparison.Ordinal)
                && text.Contains("TakeTheUnlistedParcel", StringComparison.Ordinal),
                $"{what} does not carry the parcel row — it is wired at fewer than 4 levels and cannot be "
                + "pressed.");
        }
    }

    // ── (2) THE ROUND FINDS IT FIRST ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE PARCEL IS ASKED ABOUT BEFORE THE WALLET IS, AND ON THE FINE THE READ IS OVER.</b>
    ///
    /// <para>Canon: <i>"Layer 1's whole job is to be the floor the search stops at."</i> A search that read
    /// the wallet and then found the box would be a search that did not stop at it — and, mechanically,
    /// would file a name the captain never gave and walk a ladder that never happened.</para>
    ///
    /// <para>So three positions are pinned, in order, inside <c>TheRoundStopsAtYou</c>: the parcel gate, the
    /// return on the arm where the read does not go on, and only then <c>WalletChoice.WhatHappens</c> and
    /// <c>PatrolBeat.TheGuardReads</c>. Adjacency is not enough on its own — what is asserted is ORDER, which
    /// is the only thing that can be wrong here.</para>
    ///
    /// <para><b>RED</b> by moving the parcel block below <c>PatrolBeat.TheGuardReads</c>: <i>the round reads
    /// the wallet before it looks in the hold</i>. And <b>RED</b> by deleting the
    /// <c>if (!parcel.Value.TheReadGoesOn) return;</c>: <i>a fined captain is still walked through the
    /// wallet ladder</i>.</para>
    /// </summary>
    [Fact]
    public void TheParcelIsAskedAboutBeforeTheWallet()
    {
        string challenge = Challenge();

        int stops = challenge.IndexOf("private void TheRoundStopsAtYou(", StringComparison.Ordinal);
        Assert.True(stops > 0, "the read is gone — this guard is watching a method that moved.");
        string body = challenge[stops..];

        int found = body.IndexOf("UnlistedParcel.Held(_host.Satchel)", StringComparison.Ordinal);
        int over = body.IndexOf("if (!parcel.Value.TheReadGoesOn)", StringComparison.Ordinal);

        // #746 · THE SAME LAW, AND IT IS STRONGER THAN IT WAS. The stop is an ENCOUNTER now: the arrival
        // raises the SCENE and the wallet is not read until SHOW THE PASS is pressed. So the thing that has
        // to come after the parcel gate is the scene being raised at all — and the two ladder calls are no
        // longer LATER in this method, they are not in it, which is a claim the old ordering could not make.
        int scene = body.IndexOf("StopUnderway = new Stop", StringComparison.Ordinal);

        Assert.True(found > 0, "the round never looks for a parcel at all.");
        Assert.True(over > found, "there is no arm on which the fine ends the read.");
        Assert.True(
            scene > over,
            "the round opens the scene before it looks in the hold — the search did not stop at the floor "
            + "it was built to stop at.");
        Assert.True(
            body.IndexOf("WalletChoice.WhatHappens(", StringComparison.Ordinal) < 0
            && body.IndexOf("PatrolBeat.TheGuardReads(", StringComparison.Ordinal) < 0,
            "the arrival walks the wallet ladder — on the fine, that is a name the captain never gave.");

        // …and the fine is told on the ROUND'S OWN card: the same label, the same painting, one card.
        Assert.Contains("UnlistedParcel.TheFineIsTold(g.Plate)", challenge, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.ChallengeArtUrl, told.Card, told.Told", challenge, StringComparison.Ordinal);

        // The judgement is Core's and only Core's: the page calls the one function and composes no arm of
        // its own — no second heat bank, no second folder write, no fine arithmetic.
        string code = Code(challenge);
        Assert.Equal(1, Count(code, "UnlistedParcel.TheParcelIsWhatIsFound("));
        Assert.Equal(0, Count(code, "BribeDemand"));
        Assert.Equal(0, Count(code, "CloseTheFolder"));
        Assert.Equal(0, Count(code, "IllegalHeat.Bank("));

        // …and the box goes either way, through Core's own confiscation rather than a hand-rolled Remove.
        Assert.Contains("UnlistedParcel.Confiscated(_host.Satchel)", challenge, StringComparison.Ordinal);
        int confiscates = challenge.IndexOf("UnlistedParcel.Confiscated(", StringComparison.Ordinal);
        int onlyOnAFine = challenge.IndexOf("if (found.Fined)", StringComparison.Ordinal);
        Assert.True(
            confiscates > 0 && onlyOnAFine > confiscates,
            "the box is only taken on one arm — a man who found it does not hand it back.");
    }

    // ── (3) THE EXCEPTION CONTINUES THE READ, ON ONE CARD ───────────────────────────────────────────────

    /// <summary>
    /// <b>THE TELL RIDES THE FRONT OF THE CARD THE CAPTAIN WAS ALWAYS GOING TO GET.</b> Composed after the
    /// ladder has answered and before the card goes up, so everything under it — the verdict, the
    /// consequence, the pip, the escort — lands exactly as it lands on any other afternoon.
    ///
    /// <para>Two cards on one screen would be the stacked-card mistake #777 named, and on this beat it would
    /// also be the game flagging that something unusual just happened — in the one feature whose whole
    /// premise is that nobody ever says so.</para>
    ///
    /// <para><b>RED</b> by raising a second <c>ViewObject</c> for the tell instead of composing it in:
    /// <i>the tell is composed after the card it belongs on has gone up</i>. And <b>RED</b> by hoisting the
    /// composition above <c>PatrolBeat.TheGuardReads</c>, where there is no read yet to continue.</para>
    /// </summary>
    [Fact]
    public void TheTellRidesOneCardAndTheReadGoesOn()
    {
        string challenge = Challenge();

        Assert.Equal(1, Count(Code(challenge), "UnlistedParcel.TheReadGoesOnAfterIt("));

        // #746 · RE-PATHED. The card the captain was always going to get is the SCENE's card now — the
        // opening in the amber row and the four moves under it — so the tell rides the front of THAT, which
        // is the first thing the captain reads at this stop and still the only card raised on this road.
        int opens = challenge.IndexOf("PatrolBeat.Read opening = new(", StringComparison.Ordinal);
        int continues = challenge.IndexOf("UnlistedParcel.TheReadGoesOnAfterIt(", StringComparison.Ordinal);
        int cardUp = challenge.IndexOf(
            "opening.Label, PatrolBeat.ChallengeArtUrl, opening.Card, opening.Told", StringComparison.Ordinal);

        Assert.True(opens > 0, "the arrival composes no opening for the tell to ride the front of.");
        Assert.True(continues > opens, "there is nothing yet for the tell to continue.");
        Assert.True(
            cardUp > continues,
            "the tell is composed after the card it belongs on has gone up — the captain never sees it.");

        // ONE card per stop, on both arms: the tell's ViewObject write is the read's own, and the fine's is
        // the only other one in the method.
        int stops = challenge.IndexOf("private void TheRoundStopsAtYou(", StringComparison.Ordinal);
        int endsThere = challenge.IndexOf(
            "private UnlistedParcel.Found TheParcelIsWhatHeFinds(", StringComparison.Ordinal);
        Assert.True(stops > 0 && endsThere > stops);
        Assert.Equal(1, Count(challenge[stops..endsThere], "_host.ViewObject = new DeckPlan.ConsoleSpot("));

        // …and the fine's arm raises exactly one of its own, in the method that owns that arm.
        Assert.Equal(1, Count(challenge[endsThere..], "_host.ViewObject = new DeckPlan.ConsoleSpot("));
    }

    // ── (4) A SETTLED OUTFIT PASSES WITHOUT A READ, AND SAYS NOTHING ────────────────────────────────────

    /// <summary>
    /// <b>WHERE THE ANSWER IS ON FILE, THE ROUND DOES NOT STOP — AND NOTHING ANYWHERE SAYS SO.</b>
    ///
    /// <para>Canon: <i>"answers are never re-questioned without new cause."</i> It is asked at the SIGHTING,
    /// not inside the read, because a man who has an answer for you does not walk over and then decline to
    /// ask. And it is a SILENCE: a card, a pulse or a field-book line here would be the building explaining
    /// to the captain that his cover is working, which §13.8 forbids and which this feature is written
    /// against harder than anything else in the game.</para>
    ///
    /// <para><b>RED</b> by deleting the gate: <i>the sighting loop does not ask whether the answer is
    /// already filed</i>. And <b>RED</b> by adding a line to it (<c>_host.ShowPulseMessage(...)</c>):
    /// <i>the settled pass says something out loud</i>.</para>
    /// </summary>
    [Fact]
    public void ASettledOutfitPassesWithoutAReadAndInSilence()
    {
        string challenge = Challenge();

        int loop = challenge.IndexOf(
            "private void StopTheRoundIfAnybodySeesYou(", StringComparison.Ordinal);
        Assert.True(loop > 0, "the sighting loop is gone — this guard is watching a method that moved.");

        int gate = challenge.IndexOf("UnlistedParcel.TheFolderIsClosed(book,", StringComparison.Ordinal);
        Assert.True(
            gate > loop,
            "the sighting loop does not ask whether the answer is already filed.");

        // It is asked ONCE, in the sighting loop, and never inside the read — two askers would be two
        // opinions about whether a man walks over.
        Assert.Equal(1, Count(Code(challenge), "UnlistedParcel.TheFolderIsClosed("));

        // …and it is a SILENCE. Nothing between the gate and the return says a word.
        int returns = challenge.IndexOf("return;", gate, StringComparison.Ordinal);
        Assert.True(returns > gate, "the settled gate does not return — the round stops anyway.");
        string quiet = challenge[gate..returns];
        foreach (string speaking in new[]
        {
            "ShowPulseMessage", "FileNote", "LogAutopilotEvent", "ViewObject =", "PlayCue",
        })
        {
            Assert.DoesNotContain(speaking, quiet, StringComparison.Ordinal);
        }
    }

    // ── (5) THE WORLD SEED AND THE PURSE ARE THE PAGE'S, HANDED DOWN ────────────────────────────────────

    /// <summary>
    /// <b>ONE ANSWER TO "WHICH WORLD IS THIS", AND THE ROUND DOES NOT COMPOSE IT.</b> The seed is folded
    /// once on the page off the active game thread — a thread IS a universe here, since a new voyage clears
    /// the contacts book — and handed down through <c>IPatrolHost</c>. A patrol that derived its own would be
    /// two readers of one fact, which is this ground's fifth named bug class.
    ///
    /// <para><b>RED</b> by folding a second seed inside the patrol (<c>DiceRule.Seed(0UL, bodyId)</c> in
    /// place of <c>_host.WorldSeed</c>): <i>the round composes its own idea of which world this is</i>.</para>
    /// </summary>
    [Fact]
    public void TheWorldSeedIsThePagesAndTheRoundOnlyReadsIt()
    {
        string challenge = Challenge();
        string map = Read("src", "SpaceSails.Client", "Pages", "Map.UnlistedParcel.cs");
        string host = Read("src", "SpaceSails.Client", "Pages", "Patrol", "IPatrolHost.cs");

        Assert.Contains("ulong WorldSeed { get; }", host, StringComparison.Ordinal);
        Assert.Contains("void PayTheFine(int credits);", host, StringComparison.Ordinal);

        // Folded ONCE, on the page, off the thread.
        Assert.Contains("_activeThreadId", map, StringComparison.Ordinal);
        Assert.Equal(1, Count(Code(map), "public ulong WorldSeed =>"));

        // The round reads it and composes nothing.
        string code = Code(challenge);
        Assert.Contains("_host.WorldSeed", code, StringComparison.Ordinal);
        Assert.Equal(0, Count(code, "_activeThreadId"));
        Assert.Equal(1, Count(code, "DiceRule.Seed("));

        // …and the fine leaves the purse through the page's own verb, once.
        Assert.Equal(1, Count(code, "_host.PayTheFine("));
        Assert.Equal(0, Count(code, "_credits"));

        // The page floors at nothing and does NOT borrow the confiscation's mercy floor, which was written
        // for a seizure and not for a form — a constant quoted where it was not written is how a mirrored
        // constant starts, and this ground keeps a table of those.
        Assert.Contains("System.Math.Max(0, _credits - credits)", map, StringComparison.Ordinal);
        Assert.DoesNotContain("MinBerthFeeCr", Code(map), StringComparison.Ordinal);
    }
}
