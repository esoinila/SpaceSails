using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #247 · <b>THE BOARD IS NOT THE POUR — the client half.</b>
///
/// <para>Core owns the seven house drinks, the seven boards, the price per watch and the d20
/// (<c>EveryBarPoursItsOwnTests</c> over there). What is left to get wrong is the WIRING, and this lane has
/// two places to get it wrong that a Core test cannot see.</para>
///
/// <para><b>One: the plates.</b> Seven photographs were shot for seven pours, and every art slot in this
/// game <c>onerror</c>-hides — so a file that never landed, or a manifest that never learned the file's
/// name, costs a picture silently and nothing goes red. That is #528's law, and it applies to a drinks card
/// the same as to a story beat.</para>
///
/// <para><b>Two: supper must not be a pour.</b> #756 settled that a tray does not tilt the deck, and #247
/// adds the first piece of food in the game that reaches the nerve. So the one handler that orders it has
/// to spend the purse ONCE, dose the relief the pill's way, and never go anywhere near <c>PourRum</c> — and
/// the only way to state that as a law is to read the handler.</para>
/// </summary>
public sealed class TheBoardIsNotThePourTests
{
    private static readonly string[] Bars =
        ["the-space-bar", "cinder-roost", "ringside-exchange", "the-tilt", "selene-gate", "red-eye", "the-deep"];

    // ── Reading the shipped source, the way the #736 guards do ──────────────────────────────────────────
    private static string Pages(string file)
    {
        string here = AppContext.BaseDirectory;
        for (DirectoryInfo? d = new(here); d is not null; d = d.Parent)
        {
            string candidate = Path.Combine(d.FullName, "src", "SpaceSails.Client", "Pages", file);
            if (File.Exists(candidate))
            {
                return MapMarkup.Read(candidate);
            }
        }

        throw new FileNotFoundException($"could not find src/SpaceSails.Client/Pages/{file} from {here}");
    }

    /// <summary>One method's body, cut from the signature to the next member. The same cut
    /// <c>TheCounterTakesOrdersTests</c> makes, said again here rather than shared, because a helper that
    /// silently returned the whole file would widen every <c>DoesNotContain</c> below into nothing.</summary>
    private static string Method(string file, string signature)
    {
        string src = Pages(file);
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"Pages/{file} no longer has `{signature}` where this guard can read it.");
        Match next = Regex.Match(src[(at + 1)..], @"\n\s*private ");
        int end = next.Success ? at + 1 + next.Index : -1;
        string body = src[at..(end > at ? end : src.Length)];
        Assert.True(body.Length < src.Length,
                    $"the cut for `{signature}` swallowed the whole file — every DoesNotContain below is asleep");
        return body;
    }

    private static string ArtDirectory()
    {
        for (DirectoryInfo? at = new(AppContext.BaseDirectory); at is not null; at = at.Parent)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client", "wwwroot", "art");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException($"could not find the client's art folder above {AppContext.BaseDirectory}");
    }

    private static string RepoRoot() =>
        Directory.GetParent(Path.GetDirectoryName(ArtDirectory())!)!.Parent!.Parent!.FullName;

    /// <summary>Every composition spec the project keeps, as one blob — the folder, not a list, so a
    /// manifest written tomorrow counts today.</summary>
    private static string AllManifests() =>
        string.Concat(Directory.EnumerateFiles(Path.Combine(RepoRoot(), "docs"), "art-manifest-*.md")
                               .Order(StringComparer.Ordinal)
                               .Select(File.ReadAllText));

    // ── THE PLATES ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#528's law, applied to the drinks card: the FOLDER and the CODE agree. A pour that names a
    /// photograph nobody shot renders as a row with a gap where a picture should be — and never says so,
    /// because the slot hides itself.</summary>
    [Fact]
    public void EveryHouseDrinksPlateIsActuallyInTheFolder()
    {
        string art = ArtDirectory();
        List<string> missing = [.. Bars
            .Select(id => Barkeeps.For(id)!.DrinkArtUrl ?? $"<{id} names no plate at all>")
            .Where(url => !File.Exists(Path.Combine(art, Path.GetFileName(url))))];

        Assert.True(missing.Count == 0,
                    "house pours naming a photograph that is not in wwwroot/art: " + string.Join(", ", missing));
    }

    /// <summary>…AND THE PAPERWORK AGREES TOO. A picture in the folder with no entry in any manifest is a
    /// picture nobody can reproduce — the exact drift #528 was filed for, one lane later and with a glass in
    /// its hand.</summary>
    [Fact]
    public void EveryHouseDrinksPlateIsSpecifiedInAManifest()
    {
        string manifests = AllManifests();
        List<string> unspecified = [.. Bars
            .Select(id => Path.GetFileName(Barkeeps.For(id)!.DrinkArtUrl ?? $"<{id} names no plate at all>"))
            .Where(file => !manifests.Contains(file, StringComparison.Ordinal))];

        Assert.True(unspecified.Count == 0,
                    "plates in the folder and in the code that no art manifest specifies: "
                    + string.Join(", ", unspecified));
    }

    // ── SUPPER IS NOT A POUR ────────────────────────────────────────────────────────────────────────────

    /// <summary>THE PURSE MOVES ONCE, AND THE NERVE MOVES THE PILL'S WAY.
    ///
    /// <para>Red proof: put a second <c>_credits -=</c> in the handler, or dose the relief at
    /// <c>_rumTots</c>, or route the plate through <c>PourRum</c>, and this names what went wrong.</para></summary>
    [Fact]
    public void OrderingTheSpecialSpendsOnceAndNeverPours()
    {
        string order = Method("Map.Quests.Bar.Drinks.cs", "private void OrderTheSpecial()");

        // ONE debit, and it is the one the card's own row priced.
        Assert.Single(Regex.Matches(order, @"_credits\s*-="));
        Assert.Contains("plate.PriceAt(keep.DrinkPrice)", order, StringComparison.Ordinal);

        // ONE relief, at the meal step, dosed the pill's way — never off the rum spree.
        Assert.Single(Regex.Matches(order, @"ApplyNerveRelief\("));
        Assert.Contains("NerveModel.DrinkKind.Meal", order, StringComparison.Ordinal);
        Assert.Contains("totNumber: 1", order, StringComparison.Ordinal);
        Assert.DoesNotContain("_rumTots", order, StringComparison.Ordinal);

        // A PLATE DOES NOT TILT THE DECK (#756). Not one character of the wobble law is in here.
        Assert.DoesNotContain("PourRum(", order, StringComparison.Ordinal);
        Assert.DoesNotContain("_wobbleUntilMs", order, StringComparison.Ordinal);

        // The dice moment is Core's one rule, keyed on the three things it says it is keyed on.
        Assert.Contains("TheMenuBoard.RollTheSpecial(keep.BodyId, BarWatch, ActiveCaptainName)",
                        order, StringComparison.Ordinal);

        // #736 · Every answer lands in the slot the open card draws, refusals included — and the spend is
        // banked, because a purse that moved and was not saved is a purse that did not move.
        Assert.Equal(3, Regex.Matches(order, @"_barNotice\s*=").Count);
        Assert.Contains("RequestVaultSave();", order, StringComparison.Ordinal);
    }

    /// <summary>ONE SPECIAL A SITTING, and the refusal is SAID. #212/#603: an affordance never hides and a
    /// refusal is never a control that will not press — so the button is drawn unconditionally and both
    /// "already eaten" and "purse too short" answer out loud in the keep's own voice.</summary>
    [Fact]
    public void TheSecondPlateOfASittingIsRefusedOutLoudAndNotByAGreyedButton()
    {
        string order = Method("Map.Quests.Bar.Drinks.cs", "private void OrderTheSpecial()");
        Assert.Contains("_specialThisVisit", order, StringComparison.Ordinal);

        // The visit's own fold clears it, so walking out and back in is a fresh sitting — and leaving the
        // line out of EnsureBarVisit would strand a captain on one plate for a whole session.
        Assert.Contains("_specialThisVisit = false;",
                        Method("Map.Quests.Bar.cs", "private void EnsureBarVisit()"), StringComparison.Ordinal);

        // The board's own button carries no `disabled=` — refusals are words, not a dead control.
        string board = TheBoardBlock();
        Assert.Contains("OrderTheSpecial", board, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=", board, StringComparison.Ordinal);
    }

    /// <summary>THE BOARD KEEPS THE ROOM'S OWN TIME — the FROZEN docking watch, never the live clock.
    ///
    /// <para>#709's law, and load-bearing twice here. The chairs, the rota, the rumour and the door a man
    /// comes out of are all read off <c>_dockVisitSimTime</c>, so a board on <c>SimTime</c> would be the one
    /// thing in the room keeping a different time. And it is the difference between a bug and no bug: a card
    /// stays open across sim-seconds — at warp, across whole watches — so a price PRINTED off the live clock
    /// and a price DEBITED off it one press later are two different numbers, on the same button.</para>
    ///
    /// <para>Red proof: swap <c>BarWatch</c> for <c>PatronRota.WatchIndex(SimTime)</c> in either reader and
    /// this names it.</para></summary>
    [Fact]
    public void TheBoardIsChalkedForTheWatchTheRoomWasWeldedAt()
    {
        string src = Pages("Map.Quests.Bar.Drinks.cs");
        int at = src.IndexOf("private Core.Drink? TheSpecialOnTheBoard", StringComparison.Ordinal);
        Assert.True(at >= 0, "the board no longer resolves its Special where this guard can read it");

        // Both halves — the row's price and the plate's roll — ask BarWatch, and neither asks the clock.
        string board = src[at..];
        Assert.Contains("TheMenuBoard.SpecialOn(keep.BodyId, BarWatch)", board, StringComparison.Ordinal);
        Assert.Contains("TheMenuBoard.RollTheSpecial(keep.BodyId, BarWatch,", board, StringComparison.Ordinal);
        Assert.DoesNotContain("WatchIndex(SimTime)", board, StringComparison.Ordinal);

        // …and BarWatch is the frozen one the whole bar already shares, not a second copy grown here.
        Assert.Contains("private long BarWatch => PatronRota.WatchIndex(_dockVisitSimTime);",
                        Pages("Map.BarWalkers.cs"), StringComparison.Ordinal);
    }

    /// <summary>THE RARE PLATE IS REACHABLE ON DEMAND, AND THE CHEAT STILL SHOWS THE REAL DIE.
    ///
    /// <para>#693's rule: a scene nobody can reach on demand is a scene that ships broken. This one needs a
    /// lever more than <c>?tender=flash</c> did, because the outcome is seeded on the CAPTAIN as well as the
    /// bar and the watch — so no URL can be written that shows a tester the rare line, and a one-in-ten
    /// sentence with no lever at all is an authored beat said into the dark.</para>
    ///
    /// <para>And it must force the ROLL and never the content: the die is still CAST
    /// (<c>RollTheSpecial</c> is called unconditionally) and the face printed on the receipt is that real
    /// face, not a number typed in for a tester.</para></summary>
    [Fact]
    public void TheStoryOutcomeHasALever_AndTheLeverDoesNotForgeTheDie()
    {
        // The boot reads it…
        string boot = Pages("Map.Sim.World.QueryArcs.cs");
        Assert.Contains("StartsWith(\"special=\"", boot, StringComparison.Ordinal);
        Assert.Contains("_specialStoryCheat", boot, StringComparison.Ordinal);

        // …and exactly one place spends it: the OUTCOME's reading of the roll, never the roll itself.
        string order = Method("Map.Quests.Bar.Drinks.cs", "private void OrderTheSpecial()");
        Assert.Contains("TheMenuBoard.OutcomeOf(roll, _specialStoryCheat)", order, StringComparison.Ordinal);
        Assert.DoesNotContain("_specialStoryCheat ?", order, StringComparison.Ordinal);
        Assert.Contains("(d20 {roll.Face})", order, StringComparison.Ordinal);

        // Core's own half: the lever moves the READING and leaves the face alone.
        DiceRoll plain = TheMenuBoard.RollTheSpecial("ringside-exchange", 3, "ADA LOVELACE");
        Assert.Equal(TheMenuBoard.StoryLine, TheMenuBoard.OutcomeOf(plain, forceStory: true));
        Assert.Equal(plain.Face, TheMenuBoard.RollTheSpecial("ringside-exchange", 3, "ADA LOVELACE").Face);

        // And the guide writes it down, which is where a tester finds it.
        string guide = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "testing-guide.md"));
        Assert.Contains("?special=story", guide, StringComparison.Ordinal);
    }

    // ── THE ROW THAT SHOWS THE ROTATION MUST ACTUALLY SHOW IT ───────────────────────────────────────────

    /// <summary>
    /// #1272 · <b>A TESTING LINK THAT DEMONSTRATES A ROTATION HAS TO PICK A BAR WHERE IT TURNS.</b>
    ///
    /// <para><b>The sighting.</b> §8's price-rotation row was written against the Ringside Exchange, because
    /// that is the bar the two rows above it use, and it sent a tester to <c>&amp;simhours=5</c>, <c>9</c>
    /// and <c>13</c>. Driven on 2026-09-20 the button read <b>3 cr at all three</b>. Nothing is wrong with
    /// the roll — <c>PriceOn</c> draws from four values over a three-shift window and the Ringside genuinely
    /// chalks 3 cr for four shifts running — but a tester who follows the row exactly watches the number sit
    /// still and files "the price rotation is dead". The feature was fine; the link was the bug.</para>
    ///
    /// <para><b>What is pinned.</b> Not the sentence, and not a number typed twice. The row is READ — which
    /// bar it sends a tester to, which watches it sends them to, and which three prices it promises — and
    /// every one of those is re-derived from <see cref="TheMenuBoard.PriceOn"/> and
    /// <see cref="PatronRota.WatchIndex"/> here. A row that names a bar and three watches whose prices do
    /// not all differ cannot pass, whichever bar or watches somebody picks next, and neither can a row whose
    /// promised numbers are not the ones the rule chalks.</para>
    ///
    /// <para>RED PROOF: put the row back to <c>dock=ringside-exchange</c> with <c>simhours=5/9/13</c> and
    /// this fails, naming the three identical prices.</para>
    /// </summary>
    [Fact]
    public void TheRotationRowSendsATesterToABarWhosePriceActuallyMoves()
    {
        string path = Path.Combine(RepoRoot(), "docs", "testing-links-2026-09-17.md");
        string[] rows = [.. File.ReadAllLines(path)
            .Where(l => l.Contains("the price is what rotates", StringComparison.Ordinal))];
        string row = Assert.Single(rows);

        // WHICH BAR. Every `dock=` in the row has to name the same one, or the row is sending a tester to
        // two counters and the numbers below belong to neither.
        string[] docks = [.. Regex.Matches(row, @"dock=([a-z0-9-]+)").Select(m => m.Groups[1].Value).Distinct()];
        string bar = Assert.Single(docks);
        Assert.True(Bars.Contains(bar),
                    $"§8's price-rotation row sends a tester to `dock={bar}`, which is not one of the seven "
                    + "haven bars — there is no board behind that counter to chalk a price on (#1272).");

        // WHICH WATCHES. Three distinct `simhours=`, and `simhours` is the sim clock in HOURS, so the watch
        // is PatronRota's own arithmetic and never 4 typed in here.
        long[] watches = [.. Regex.Matches(row, @"simhours=(\d+)")
            .Select(m => PatronRota.WatchIndex(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) * 3600.0))
            .Distinct()];
        Assert.True(watches.Length == 3,
                    $"§8's price-rotation row names {watches.Length} distinct watch(es) — the row is about "
                    + "three shifts, and a tester cannot see a number rotate across fewer (#1272).");

        int[] chalked = [.. watches.Select(w => TheMenuBoard.PriceOn(bar, w))];

        // THE LAW. Three shifts, three different numbers — otherwise the link demonstrates the opposite of
        // what it claims, which is exactly what shipped.
        Assert.True(chalked.Distinct().Count() == 3,
                    $"§8's price-rotation row sends a tester to `{bar}` at watches "
                    + $"{string.Join(", ", watches)} — and the board chalks {string.Join(" → ", chalked)} cr "
                    + "there, so the number they are told to watch does not move across all three. The roll "
                    + "is not broken; the row picked the wrong bar. Pick one whose three linked watches "
                    + "differ, and write those numbers into the row (#1272).");

        // …AND THE ROW SAYS WHICH NUMBERS, so "it did not move" is a finding and not a shrug.
        Match promised = Regex.Match(row, @"(\d+) cr → (\d+) cr → (\d+) cr");
        Assert.True(promised.Success,
                    "§8's price-rotation row does not spell the three prices a tester should see (`n cr → n "
                    + "cr → n cr`). Without them the row cannot be followed: any number at all looks right "
                    + $"(#1272). The rule chalks {string.Join(" → ", chalked)} cr at `{bar}`.");

        int[] said = [.. promised.Groups.Values.Skip(1)
            .Select(g => int.Parse(g.Value, CultureInfo.InvariantCulture))];
        Assert.True(said.SequenceEqual(chalked),
                    $"§8's price-rotation row promises {string.Join(" → ", said)} cr at `{bar}`, and "
                    + $"TheMenuBoard.PriceOn chalks {string.Join(" → ", chalked)} cr on the watches the row "
                    + "links to. A testing link that quotes a number the game does not show is worse than "
                    + "one that quotes none (#1272).");
    }

    // ── THE BOARD IS ON THE SCREEN ──────────────────────────────────────────────────────────────────────

    /// <summary>The board's block, cut out of the composed page. Sliced by its own two markers so every
    /// claim below is about the board and not about the card it hangs in.</summary>
    private static string TheBoardBlock()
    {
        string page = Pages("Map.razor");
        int at = page.IndexOf("class=\"bar-board\"", StringComparison.Ordinal);
        Assert.True(at >= 0, "the composed page no longer draws a `.bar-board` anywhere — #247's board is gone");
        int open = page.LastIndexOf("@if (TheBoardLine", at, StringComparison.Ordinal);
        Assert.True(open >= 0, "the board is drawn without asking whether this counter HAS a kitchen");
        int close = page.IndexOf("<div class=\"deck-offer-actions\">", at, StringComparison.Ordinal);
        Assert.True(close > at, "the board is no longer above the card's pinned foot");
        return page[open..close];
    }

    /// <summary>#780's law, kept: THE BOARD IS IN THE CARD'S BODY, ABOVE THE PINNED FOOT — never after
    /// <c>.deck-offer-actions</c>, whose sticky 12rem shadow-scrim darkens whatever slides under it. That
    /// is exactly how six priced items once ended up in the DOM, hit-testable, and read by a captain as a
    /// greyed panel behind glass. Prose goes in the body.</summary>
    [Fact]
    public void TheBoardIsDrawnInTheBodyAboveTheFoot_AndNotUnderTheScrim()
    {
        string page = Pages("Map.razor");
        int board = page.IndexOf("class=\"bar-board\"", StringComparison.Ordinal);
        int foot = page.IndexOf("<div class=\"deck-offer-actions\">", board, StringComparison.Ordinal);
        Assert.True(board >= 0 && foot > board, "the board must be typed BEFORE the card's pinned foot");
    }

    /// <summary>IT IS A DIFFERENT KITCHEN, SO IT IS NOT BEHIND THE DRINKS MENU'S TOGGLE. A captain who has
    /// not pressed "See the menu" must still see that this bar feeds people — the board is a board, chalked
    /// where you can read it.</summary>
    [Fact]
    public void TheBoardDoesNotHideBehindTheDrinksMenuToggle()
    {
        string board = TheBoardBlock();
        Assert.DoesNotContain("_showBarMenu", board, StringComparison.Ordinal);
    }

    /// <summary>THE ROW IS PRICED OFF CORE, FOR THIS WATCH — one number, read once, so the label and the
    /// debit cannot come from two different shifts. And the board is WORDS: no art slot in it at all, which
    /// is the one way to be sure the unillustrated set never looks broken.</summary>
    [Fact]
    public void TheRowPricesItselfOffCoreAndCarriesNoPicture()
    {
        string board = TheBoardBlock();
        Assert.Contains("special.PriceAt(keep.DrinkPrice)", board, StringComparison.Ordinal);
        Assert.Contains("TheMenuBoard.SpecialLabel", board, StringComparison.Ordinal);
        Assert.Contains("TheMenuBoard.BoardHead", board, StringComparison.Ordinal);
        Assert.DoesNotContain("<img", board, StringComparison.Ordinal);
    }

    /// <summary>THE POUR'S PLATE REACHES THE ROW. The drinks card has drawn <c>Drink.ArtUrl</c> since #780;
    /// what #247 adds is that a haven bar's specialty now HAS one. Pinned end to end — the keep's field, the
    /// menu item Core builds from it, and the markup that draws it — because the slot hides itself and a
    /// picture that silently stopped arriving is exactly the failure nobody notices.</summary>
    [Fact]
    public void TheHousePoursPlateIsOnTheMenuRowItBelongsTo()
    {
        foreach (string id in Bars)
        {
            Barkeep keep = Barkeeps.For(id)!;
            Drink special = Assert.Single(DrinkMenu.For(keep), d => d.Category == DrinkCategory.Specialty);
            Assert.Equal(keep.DrinkArtUrl, special.ArtUrl);
        }

        string page = Pages("Map.razor");
        Assert.Contains("d.ArtUrl is { } dishArt", page, StringComparison.Ordinal);
        Assert.Contains("class=\"bar-menu-art\"", page, StringComparison.Ordinal);
    }
}
