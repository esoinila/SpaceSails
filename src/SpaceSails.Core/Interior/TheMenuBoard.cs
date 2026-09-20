namespace SpaceSails.Core.Interior;

/// <summary>
/// #247 · <b>THE BOARD, AND THE SPECIAL ON IT.</b> Owner, in the same breath as the drinks:
/// <i>"FOOD too — 'the Special is really a wild one usually.' Each bar gets a menu board … and THE SPECIAL
/// is the wildcard: a rotating, slightly alarming house dish with in-world provenance … Eating = the
/// meal-sized shore-leave beat beside the drink (bigger #226 relief, small cr), and ordering the Special is
/// a tiny dice moment — usually delicious, occasionally a story."</i>
///
/// <para><b>What the board is.</b> One chalked line per bar, in the same signage register the drinks card
/// already speaks — a plate of words, not a picture. This set is deliberately UNILLUSTRATED: the house
/// pours got the photographs (<see cref="Barkeep.DrinkArtUrl"/>), and a board that also carried a plate of
/// food would make the card two galleries stacked on each other. A board is a thing you read.</para>
///
/// <para><b>What ROTATES, honestly.</b> Each bar has exactly ONE Special, because that is the content the
/// owner wrote and inventing a second dish to make a carousel turn would be the game making things up
/// about its own kitchen. So what turns per watch is not the dish: it is the <see cref="PriceOn">price</see>
/// — the board is re-chalked every shift with whatever the last freighter cost — and the
/// <see cref="OutcomeOf">outcome pair</see>, which is the dice moment. The dish line stands. Say it that
/// way round and nothing on the screen ever claims a variety the kitchen does not have.</para>
///
/// <para><b>Deterministic, like every roll in this repo.</b> The price is keyed on (bodyId, watch); the
/// outcome is keyed on (bodyId, watch, captain), so two captains in the same room on the same shift can get
/// different plates of the same dish, one captain always gets the same plate twice, and neither answer ever
/// came off a clock or a <c>System.Random</c>. The watch is <see cref="PatronRota.WatchIndex"/> — the same
/// four-sim-hour shift the rota, the rumour rotation and the room itself are cut on.</para>
///
/// <para><b>Nothing here explains anything.</b> The Special is provenance and gallows humour, never a leak:
/// the Ringside will not say where the eel was caught, the Deep will not print the depth, and Selene's
/// park greens are a COUNT ("ten metres from your table, by somebody's count") and not an account of who
/// grows them or why the block has a park at all.</para>
/// </summary>
public static class TheMenuBoard
{
    /// <summary>The head the board is chalked under, in the drinks card's own signage register.</summary>
    public const string BoardHead = "🍽 THE BOARD";

    /// <summary>What the one row on it is called — every bar, every watch. The dish line underneath is the
    /// bar's own; this is the word above it, and at the Roadstead it is very nearly the whole menu.</summary>
    public const string SpecialLabel = "TODAY'S SPECIAL";

    /// <summary>The cheapest the kitchen has ever chalked a Special at, in credits. A plate is SMALL coin
    /// beside a pour on purpose (the owner's "small cr"): well under every haven's glass and a long way
    /// under a round. FLAGGED for the owner's tuning.</summary>
    public const int PriceFloor = 3;

    /// <summary>The dearest the board goes — and it is the price of the CHEAPEST house glass in the system
    /// (the Roadstead's 6 cr), never a credit over it. That is the law rather than the number: supper may
    /// never read as the luxury on a card whose other side is drink. It is the thing you eat before the
    /// pour, not instead of it. FLAGGED for the owner's tuning.</summary>
    public const int PriceCeiling = 6;

    /// <summary>The usual outcome, and the one the kitchen would like on the record.</summary>
    public const string DeliciousLine = "It is better than it had any right to be.";

    /// <summary>The other one. It is still a compliment, which is the joke and also the horror.</summary>
    public const string StoryLine = "Something in it moved, and then it was delicious.";

    /// <summary>A d20 at or above this is the STORY — a one-in-ten plate, so a captain who eats at every
    /// port meets it and a captain who eats at one probably does not. Rare enough to be a story and common
    /// enough to be reachable without a cheat. FLAGGED for the owner's tuning.</summary>
    public const int StoryOnOrAbove = 19;

    /// <summary>One bar's board: the line chalked on it, and the line the card's body reads when the plate
    /// actually lands. They are the same sentence everywhere but the Roadstead, where the board says one
    /// word and the plate says one more.</summary>
    /// <param name="BodyId">The berth this board hangs at — the same id <see cref="Barkeeps.For"/> keys on.</param>
    /// <param name="BoardLine">What the board says. Read before you order.</param>
    /// <param name="PlateLine">What the card says when it is in front of you.</param>
    public sealed record Board(string BodyId, string BoardLine, string PlateLine);

    // ── THE SEVEN BOARDS, Fable-authored, verbatim (#247) ───────────────────────────────────────────────
    //
    // Provenance and gallows humour, one line each, and not one of them right about anything that matters.
    // The Roadstead's is the owner's own joke: "the Space Bar special is just labeled SPECIAL and the
    // barkeep won't elaborate" — so the board is one word and the plate is four, and neither is an answer.
    private static readonly Board[] All =
    [
        new("the-space-bar", "SPECIAL.", "It is the Special."),
        new("cinder-roost",
            "Roost hen, brined in cloud water. It never saw the ground either.",
            "Roost hen, brined in cloud water. It never saw the ground either."),
        new("ringside-exchange",
            "Hydroponic eel, Ringside style — caught it ourselves, don't ask where.",
            "Hydroponic eel, Ringside style — caught it ourselves, don't ask where."),
        new("the-tilt",
            "Everything frozen. The thawing lamp is rented by the minute.",
            "Everything frozen. The thawing lamp is rented by the minute."),
        new("selene-gate",
            "Bean stew with the park's greens — grown ten metres from your table, by somebody's count.",
            "Bean stew with the park's greens — grown ten metres from your table, by somebody's count."),
        new("red-eye",
            "Storm noodles. The broth is whatever the last freighter brought.",
            "Storm noodles. The broth is whatever the last freighter brought."),
        new("the-deep",
            "Pressure-cooked anything. It comes up from a depth we do not print.",
            "Pressure-cooked anything. It comes up from a depth we do not print."),
    ];

    /// <summary>The board at a berth, or null where there is no kitchen behind the counter. Every haven bar
    /// has one; the Hive's self-serving counter does not, because its card under the glass IS its food and a
    /// board beside it would be the same room claiming two kitchens.</summary>
    public static Board? For(string? bodyId) =>
        bodyId is null ? null : Array.Find(All, b => b.BodyId == bodyId);

    /// <summary>Every board — for tests and the canon prose sweep.</summary>
    public static IReadOnlyList<Board> AllBoards => All;

    /// <summary>What the Special costs on this watch at this bar: a seeded value in
    /// [<see cref="PriceFloor"/>, <see cref="PriceCeiling"/>], deterministic on (bodyId, watch). This is the
    /// honest half of the rotation — the board is re-chalked every shift and the number on it is the thing
    /// that moved.</summary>
    public static int PriceOn(string bodyId, long watch) =>
        DiceRule.RollAmount(
            DiceRule.Seed($"board-price:{bodyId ?? string.Empty}", watch), PriceFloor, PriceCeiling).Total;

    /// <summary>The Special as an orderable item, priced for this watch — a <see cref="DrinkCategory.Food"/>
    /// <see cref="Drink"/> so that it is bought through the counter's ONE purchase seam
    /// (<see cref="Drink.PriceAt"/> over the purse) rather than through a second till written beside it.
    ///
    /// <para>It carries its own <see cref="Drink.Price"/> rather than falling back to the house glass rate,
    /// because a plate is not a pour and this is exactly the field #756 grew for a card whose coffee is 2 cr
    /// and whose double is 12. No <see cref="Drink.ArtUrl"/>: the board is words.</para>
    ///
    /// <para>It is deliberately NOT on <see cref="DrinkMenu.For"/> and NOT in <see cref="DrinkMenu.Catalog"/>
    /// — a contact's favourite drink may never come back a bowl of noodles, and the drinks card is the
    /// drinks card.</para></summary>
    public static Drink? SpecialOn(string bodyId, long watch) =>
        For(bodyId) is { } board
            ? new Drink($"board:{board.BodyId}", SpecialLabel, DrinkCategory.Food,
                        board.BoardLine, PriceOn(board.BodyId, watch))
            : null;

    /// <summary>The dice moment, cast: one d20 keyed on (bodyId, watch, captain). Returned whole so the card
    /// can SHOW the face — the homage's whole point is that the player watches the number come up.</summary>
    public static DiceRoll RollTheSpecial(string bodyId, long watch, string? captain) =>
        DiceRule.Roll(DiceRule.Seed(
            $"board-special:{bodyId ?? string.Empty}:{(captain ?? string.Empty).ToUpperInvariant()}", watch));

    /// <summary>True when the roll came up the STORY rather than the usual. One rule, read by the line
    /// below and by the guard, so "which outcome was it" has exactly one answer.</summary>
    public static bool IsAStory(DiceRoll roll) => roll.Total >= StoryOnOrAbove;

    /// <summary>The outcome line for a cast roll: usually <see cref="DeliciousLine"/>, occasionally
    /// <see cref="StoryLine"/>.</summary>
    public static string OutcomeOf(DiceRoll roll) => IsAStory(roll) ? StoryLine : DeliciousLine;
}
