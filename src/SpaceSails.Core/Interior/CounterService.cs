namespace SpaceSails.Core.Interior;

/// <summary>
/// #756 · THE COUNTER TAKES ORDERS. Owner, live playtest 2026-08-08, standing at the B1 cantina hall's
/// counter with a purse in his pocket: <i>"HOW DO I ORDER A DRINK FROM THE BAR?????? I walk to the bar to
/// buy a drink... not possible... WHY?"</i> — and, closing the design, <i>"we could copy a lot of those
/// from say the Tilt bar at Uranus."</i>
///
/// <para><b>So nothing here is a dialog, a purchase, or a receipt.</b> All of that already exists and is
/// tested: <see cref="Barkeep"/> holds the purchase math, <see cref="DrinkMenu"/> holds the card, and the
/// client's counter card renders both. This file is the <i>content</i> half of that one seam — the venue
/// the machine is pointed at. The Tilt pours the Sideways Blue at 7 cr with a keep's chatter; this counter
/// pours company coffee with nobody behind it, and the machinery never learns the difference.</para>
///
/// <para><b>Nobody is behind it</b> (#618, skeleton staff): the counter does its own serving, which on this
/// rock is somehow worse. So <see cref="Barkeep.SelfService"/> is set, and the card drops the two verbs
/// that need a person — standing a round, and asking what they have heard.</para>
///
/// <para>#781 · <b>…on the DEAD watches.</b> The paragraph above was never the whole truth, only the
/// small-hours one: on the living watches there is a keep at the front of this counter
/// (<see cref="OnWatch"/>, <see cref="TheKeep"/>), and the reading above stands word for word on the watches
/// he is not working. What he actually is, the game never says.</para>
///
/// <para><b>The menu's law</b> (owner ruling, the menu's voice, final): every item jokes about what is
/// BELOW, because gallows humour about the deep is this hall's way of never asking about it — and no item
/// may ever be RIGHT about it. Not one name or line here states a true fact about the lower bands. Comedy
/// as camouflage, never as leak. A captain who has been down reads the same card differently, and that
/// difference is free storytelling this text never acknowledges.</para>
/// </summary>
public static class CounterService
{
    /// <summary>What the counter charges for a glass — the fallback rate for anything on the card that
    /// does not name its own price. Deliberately a shade under a spaceport bar: this is a canteen with
    /// ideas, not the Ringside.</summary>
    public const int HouseRate = 7;

    /// <summary>What a round would cost here, kept in step with <c>CanteenTable.RoundPrice</c> — the price
    /// the tables already charge for three glasses walking over from this very counter. Unused while the
    /// counter is self-service (there is no room to stand a round FOR), and carried anyway so the day the
    /// verb arrives it does not arrive at a different price than the tables quote.</summary>
    public const int RoundRate = 12;

    /// <summary>#780 · Where the card's photographs live. Five of the six were shot; COMPANY COFFEE was not,
    /// and stays a line of text on purpose — it is the one thing on this card that is not a treat, and a
    /// glossy plate of it would be the only joke here that landed the wrong way.</summary>
    private const string ArtDir = "art/";

    /// <summary>The card under the glass on the counter top, in the order it reads: the two honest things
    /// first, then the tray, then the three you were warned about.</summary>
    private static readonly Drink[] BranchCard =
    [
        new("b1-company-coffee", "COMPANY COFFEE", DrinkCategory.Food,
            "Bottomless, and on the account. Nobody has found the bottom.", 2),

        new("b1-cage-breakfast", "CAGE CREW'S BREAKFAST", DrinkCategory.Food,
            "Fried, anonymous, and engineered to stay down in low gravity.", 6,
            ArtDir + "food-cage-breakfast.jpg"),

        new("b1-subbasement-stew", "SUB-BASEMENT STEW", DrinkCategory.Food,
            "Ingredients sourced from as far down as we are willing to say.", 5,
            ArtDir + "food-subbasement-stew.jpg"),

        new("b1-local-pour", "THE LOCAL POUR", DrinkCategory.Specialty,
            "Distilled on site. The still is somewhere below B4, which is also all anybody knows about "
            + "below B4.", 7,
            ArtDir + "drink-local-pour.jpg"),

        new("b1-bottom-shelf", "THE BOTTOM SHELF", DrinkCategory.Gin,
            "We don't go down there. The bottle does.", 9,
            ArtDir + "drink-bottom-shelf.jpg"),

        new("b1-long-drop", "THE LONG DROP", DrinkCategory.Gin,
            "A double. Goes down easy. Comes up never.", 12,
            ArtDir + "drink-long-drop.jpg"),
    ];

    /// <summary>The whole card, for tests and the canon prose sweep.</summary>
    public static IReadOnlyList<Drink> Card => BranchCard;

    /// <summary>The counter of a branch office's upper canteen, standing ready to serve itself.</summary>
    private static readonly Barkeep BranchCounter = new(
        "hive-upper-canteen",
        "THE COUNTER",
        "CANTEEN 1",
        "THE LOCAL POUR",
        "Distilled on site. The still is somewhere below B4, which is also all anybody knows about below B4.",
        HouseRate,
        RoundRate,
        [],
        House: BranchCard,
        Welcome:
            "The counter serves itself: a reader, a warm plate, and the card under glass. Nobody comes "
            + "out to take the order and nobody ever has.",
        DeskArtUrl: "art/b1-bar-desk.jpg",
        SelfService: true);

    /// <summary>Who — or what — serves at this amenity, or null where the fixture is not a counter that
    /// takes orders. Keyed the same way every other fact about these rooms is: the site and which of the
    /// three comforts it is, asked of the building rather than worked out by the caller.
    ///
    /// <para>Only the UPPER canteen serves. Canteen 2 is machines and a pass check (#709's opposite
    /// economics — no till, no booze on display) and a washroom is a washroom. The head office's own
    /// SIDEBOARD gets its own card when its content is written; until then it keeps the read verb it has
    /// always had, because a counter that opens an empty menu is worse than a counter that does not open.</para></summary>
    public static Barkeep? For(string bodyId, UndergroundComplex.Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return use == UndergroundComplex.Comfort.UpperCanteen && !UndergroundComplex.IsHeadOffice(bodyId)
            ? BranchCounter
            : null;
    }

    // ── #781 · …AND WHO IS BEHIND IT, WHICH IS A FACT ABOUT THE WATCH ───────────────────────────────────
    //
    // Owner, live at this fixture: "There should be a bar keep there (really like hidden security guard)."
    //
    // ONE RECORD, DERIVED FROM THE OTHER. The kept counter is BranchCounter `with` four fields changed — the
    // name, the greeting, the rumours, and whether anybody is behind it. Everything else is the same object:
    // the same card under the glass, the same prices, the same desk in the photograph. So "the keep pours the
    // same coffee" is structural rather than a promise somebody kept while typing a second literal, and the
    // dead-watch reading below is not a copy of #772's counter — it IS #772's counter, byte for byte.

    /// <summary>#781 · The counter with somebody behind it. #618's "nobody is back there" stays true of the
    /// ROOM BEHIND: the keep works the front, and is never seen entering or leaving it.</summary>
    private static readonly Barkeep KeptCounter = BranchCounter with
    {
        Name = TheKeep.Name,
        Welcome = TheKeep.Welcome,
        Rumors = TheKeep.Rumors,
        SelfService = false,
    };

    /// <summary>
    /// #781 · WHO IS SERVING AT THIS COUNTER ON THIS WATCH.
    ///
    /// <para>The living watches get the keep and the two person-verbs #772 gated away — a round for the room,
    /// and asking what he has heard. The dead ones get the self-service counter exactly as it has always been:
    /// the very instance, so a guard can assert byte-identity rather than compare two lists of strings and
    /// hope.</para>
    ///
    /// <para>Anything that is not this counter comes back untouched, so a haven bar handed through here is a
    /// haven bar — the fork is about ONE fixture and must never become a rule about barkeeps.</para>
    /// </summary>
    public static Barkeep OnWatch(Barkeep counter, long watch)
    {
        ArgumentNullException.ThrowIfNull(counter);
        return counter == BranchCounter && TheKeep.KeptWatch(watch) ? KeptCounter : counter;
    }

    /// <summary>The same question asked of the building rather than of a record already in hand — the
    /// three-argument twin of <see cref="For(string, UndergroundComplex.Comfort)"/>, for callers that have a
    /// site and a watch and no counter yet.</summary>
    public static Barkeep? For(string bodyId, UndergroundComplex.Comfort use, long watch) =>
        For(bodyId, use) is { } counter ? OnWatch(counter, watch) : null;

    /// <summary>Is THE KEEP the one behind this card? Asked here rather than by comparing a name at a call
    /// site, because a string compare against "the keep" is a second answer to a question this file owns —
    /// and it would say yes to any venue that ever named a barkeep the same thing.</summary>
    public static bool ServedByTheKeep(Barkeep? keep) => keep is not null && keep == KeptCounter;
}
