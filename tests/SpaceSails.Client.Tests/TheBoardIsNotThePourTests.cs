using System;
using System.Collections.Generic;
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
            .Select(id => Barkeeps.For(id)!.DrinkArtUrl!)
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
            .Select(id => Path.GetFileName(Barkeeps.For(id)!.DrinkArtUrl!))
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
        Assert.Equal(1, Regex.Matches(order, @"_credits\s*-=").Count);
        Assert.Contains("plate.PriceAt(keep.DrinkPrice)", order, StringComparison.Ordinal);

        // ONE relief, at the meal step, dosed the pill's way — never off the rum spree.
        Assert.Equal(1, Regex.Matches(order, @"ApplyNerveRelief\(").Count);
        Assert.Contains("NerveModel.DrinkKind.Meal", order, StringComparison.Ordinal);
        Assert.Contains("totNumber: 1", order, StringComparison.Ordinal);
        Assert.DoesNotContain("_rumTots", order, StringComparison.Ordinal);

        // A PLATE DOES NOT TILT THE DECK (#756). Not one character of the wobble law is in here.
        Assert.DoesNotContain("PourRum(", order, StringComparison.Ordinal);
        Assert.DoesNotContain("_wobbleUntilMs", order, StringComparison.Ordinal);

        // The dice moment is Core's one rule, keyed on the three things it says it is keyed on.
        Assert.Contains("TheMenuBoard.RollTheSpecial(keep.BodyId, TheBoardsWatch, ActiveCaptainName)",
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
