using System;
using System.IO;
using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #781 · THE KEEP AT CANTEEN 1 — the client half.
///
/// <para>Owner, live at the counter 2026-08-08: <i>"There should be a bar keep there (really like hidden
/// security guard)."</i></para>
///
/// <para>Core owns who is behind the counter on a watch, which sentence that watch wears, and whether the
/// room says anything back (<c>TheKeepAtTheCounterTests</c> over there, eleven of them). What is left to get
/// wrong is the WIRING, which is exactly what was wrong for two issues before this one: the fixture existed,
/// the card existed, and the key that joins them did not. So these read the shipped source — transcribed
/// rather than asked for, because a test that asked the page for its own call graph could not notice the
/// call going away.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheKeepAtTheCounterTests
{
    private static string Pages(string file)
    {
        string here = AppContext.BaseDirectory;
        for (DirectoryInfo? d = new(here); d is not null; d = d.Parent)
        {
            string candidate = Path.Combine(d.FullName, "src", "SpaceSails.Client", "Pages", file);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }
        }
        throw new FileNotFoundException($"could not find src/SpaceSails.Client/Pages/{file} from {here}");
    }

    private static string Method(string file, string signature)
    {
        string src = Pages(file);
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{file} no longer has `{signature}` where this guard can read it.");
        Match next = Regex.Match(src[(at + 1)..], @"\n\s*private ");
        int end = next.Success ? at + 1 + next.Index : -1;
        return src[at..(end > at ? end : src.Length)];
    }

    /// <summary>The counter card's own block in the markup, cut the way the #736 guards cut theirs.</summary>
    private static string CounterCard()
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("@if (_deckMode && _barMenu is { } keep)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the counter card block this guard knows how to find.");
        int end = razor.IndexOf("\n    @if (", start + 10, StringComparison.Ordinal);
        return razor[start..(end > start ? end : razor.Length)];
    }

    // ── THE FORK, AT THE PRESS ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PressingEAtTheCounterAsksWhoIsBehindItThisWatch()
    {
        // Red proof: open the card with `counter` again instead of the forked one and the keep never once
        // appears in the game, on any watch, with every Core guard still green.
        string press = Method("Map.Surface.Canteen.cs", "private void HiveAmenityInteract()");

        Assert.Contains("CounterService.OnWatch(counter, ex.CanteenWatch)", press, StringComparison.Ordinal);
        Assert.Contains("OpenCounterService(", press, StringComparison.Ordinal);

        // THE FROZEN WATCH AND NEVER THE CLOCK (#709's law, third named bug class): the room drawn on this
        // floor and the person who answers the press have to be one shift.
        Assert.DoesNotContain("SimTime", press, StringComparison.Ordinal);

        // …and the dev row that walks a tester to each side of the fork, because a fork whose two roads are
        // decided by the shift cannot be seen at all by somebody who can only reach one shift.
        Assert.Contains(DevStarts.All, e => e.Url == "/map?counter=1&watch=2");
        Assert.Contains(DevStarts.All, e => e.Url == "/map?counter=1&watch=5");
    }

    [Fact]
    public void TheStoolCheatSeatsYouInFrontOfWhoeverIsWorkingThatWatch()
    {
        // The same fork, on the dev road. A cheat that opened the unforked card would be testing a counter
        // that does not ship — #693's own rule one turn on.
        string stand = Method("Map.Surface.Cheats.cs", "private void StandAtTheCounterIfAsked(");

        Assert.Contains("CounterService.OnWatch(counter, ex.CanteenWatch)", stand, StringComparison.Ordinal);
        Assert.Contains("TheKeep.KeptWatch(ex.CanteenWatch)", stand, StringComparison.Ordinal);
    }

    // ── THE SECOND LINE ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheCardWearsHisGreetingAndThenTheWatchsTell()
    {
        // Canon: the tell is "shown as the counter card's SECOND line when you lean on it on a kept watch".
        // So it is drawn on THIS card, under the slot the greeting lands in — not in a banner behind it
        // (#736) and not before the sentence it follows.
        //
        // Red proof: delete the @if block and the keep is a name with no character; move it above the notice
        // and the ordering claim goes red.
        string block = CounterCard();

        Assert.Contains("TheKeepsTell() is { } tell", block, StringComparison.Ordinal);
        Assert.Contains("@tell", block, StringComparison.Ordinal);

        int notice = block.IndexOf("@if (_barNotice is { } note)", StringComparison.Ordinal);
        int tell = block.IndexOf("TheKeepsTell()", StringComparison.Ordinal);
        Assert.True(notice >= 0, "the counter card no longer draws the greeting slot.");
        Assert.True(tell > notice, "the tell is drawn ABOVE the line it is supposed to follow.");

        // It is never in quotes: nobody says it, and nobody in the room remarks on it.
        Assert.DoesNotContain("“@tell", block, StringComparison.Ordinal);

        // The two person-verbs are still gated on the ONE fact that decides them, so lifting #772's gate is
        // the fork's doing and not a second rule typed into the markup.
        Assert.Contains("keep.SelfService", block, StringComparison.Ordinal);
        Assert.Contains("Round for the room", block, StringComparison.Ordinal);
        Assert.Contains("Hear a rumor", block, StringComparison.Ordinal);

        // And no invented noun anywhere a player can see: the card's name line is Core's `the keep`, and the
        // counter's plate stays the counter's (#781's own instruction). Swept over the DRAWN markup with the
        // razor comments cut out — the owner's own direction is quoted in one of them, and a law about what
        // reaches a screen must not be a law about what a coder may write down.
        string drawn = Regex.Replace(block, @"@\*.*?\*@", " ", RegexOptions.Singleline);
        foreach (string invented in new[] { "SECURITY", "THE BARKEEP", "BAR KEEP", "HOUSE DETECTIVE" })
        {
            Assert.DoesNotContain(invented, drawn, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TheTellIsReadOffTheFrozenWatchAndOnlyWhereSomebodyIsBehindTheCounter()
    {
        // Red proof: seed it off the clock — `TheKeep.TellAt((long)(SimTime / 3600))` — and the clock claim
        // names it. Drop the ServedByTheKeep gate and #772's self-service card grows a sentence about a man
        // who is not there.
        string tell = Method("Map.Quests.Bar.cs", "private string? TheKeepsTell()");

        Assert.Contains("ex.CanteenWatch", tell, StringComparison.Ordinal);
        Assert.Contains("CounterService.ServedByTheKeep(_barMenu)", tell, StringComparison.Ordinal);
        Assert.DoesNotContain("SimTime", tell, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTime", tell, StringComparison.Ordinal);
    }

    // ── ASKING IS BEING SEEN ASKING ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void BuyingARumourFilesOneNoteAndWritesOneEntryInHisBook()
    {
        // Red proof, both ways: delete the FileNote and the captain's book forgets that they asked; delete
        // the RecordKnownTell and nothing anywhere ever leaks, with every Core guard still green.
        string ask = Method("Map.Quests.Bar.cs", "private void AskBarkeepForRumor()");
        Assert.Contains("AskingIsBeingSeenAsking(keep);", ask, StringComparison.Ordinal);

        string seen = Method("Map.Quests.Bar.cs", "private void AskingIsBeingSeenAsking(");

        // EXACTLY ONE of each. Two FileNotes would be two entries for one question, which is the shape of
        // this repo's own duplicated-write bugs.
        Assert.Equal(1, Occurrences(seen, "FileNote("));
        Assert.Contains("FileNote(TheKeep.FieldNote, TheKeep.Glyph)", seen, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(seen, "RecordKnownTell("));
        Assert.Contains("TheKeep.LedgerId", seen, StringComparison.Ordinal);

        // The SUBJECT comes off the same index the sentence came from, never re-derived beside it.
        Assert.Contains("TheKeep.TopicOf(keep.RumorIndexAt(SimTime))", seen, StringComparison.Ordinal);
        Assert.Contains("ex.CanteenWatch", seen, StringComparison.Ordinal);

        // Only at THIS counter — a rumour bought from a haven keep is a rumour bought from a haven keep.
        Assert.Contains("CounterService.ServedByTheKeep(keep)", seen, StringComparison.Ordinal);

        // …and nothing reacts. No meter moves on this beat: #715 arrives as a line in a book, and a gauge
        // here would be the announcement the canon section rules out.
        foreach (string meter in new[] { "ApplyNerveShock", "_heat", "Hostile", "AddGoodwill", "_credits" })
        {
            Assert.DoesNotContain(meter, seen, StringComparison.Ordinal);
        }
    }

    // ── AND A SHIFT LATER, THE ROOM SAYS IT BACK ────────────────────────────────────────────────────────

    [Fact]
    public void TheLeakIsAStrangersBarkRaisedWhenTheCardOpensAndOnlyOnce()
    {
        // Red proof: drop the call from OpenCounterService and the consequence is unreachable; drop the walk
        // over _overheard and the same sentence is told to the captain every time they lean on the counter.
        string open = Method("Map.Quests.Bar.cs", "private void OpenCounterService(");
        Assert.Contains("TheRoomSaysItBack(counter);", open, StringComparison.Ordinal);

        string leak = Method("Map.Quests.Bar.cs", "private void TheRoomSaysItBack(");

        Assert.Contains("CounterService.ServedByTheKeep(counter)", leak, StringComparison.Ordinal);
        Assert.Contains("TheKeep.LeakedTopic(", leak, StringComparison.Ordinal);
        Assert.Contains("ex.CanteenWatch", leak, StringComparison.Ordinal);
        Assert.DoesNotContain("SimTime", leak, StringComparison.Ordinal);

        // It is drawn from HIS book and from nothing else.
        Assert.Contains("_contacts.For(TheKeep.LedgerId).KnownTells", leak, StringComparison.Ordinal);

        // It is said by a BACKGROUND PATRON, off the crowd's own plate list, seeded per (site, watch) — the
        // keep is never quoted saying he talked.
        Assert.Contains("CanteenRegulars.StrangerPlates", leak, StringComparison.Ordinal);
        Assert.Contains("DiceRule.Seed(", leak, StringComparison.Ordinal);
        Assert.DoesNotContain("keep.Name", leak, StringComparison.Ordinal);

        // Once, asked of the durable book itself rather than of a flag kept beside it.
        Assert.Contains("foreach (Core.OverheardLine line in _overheard)", leak, StringComparison.Ordinal);
        Assert.Contains("Overhear(", leak, StringComparison.Ordinal);
    }

    private static int Occurrences(string text, string needle)
    {
        int n = 0;
        for (int at = text.IndexOf(needle, StringComparison.Ordinal); at >= 0;
             at = text.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            n++;
        }
        return n;
    }
}
