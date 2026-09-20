namespace SpaceSails.Core.Interior;

/// <summary>
/// The barkeep behind a haven bar (#247, owner ashore at the Rusty Roadstead: "How do I get a drink
/// at the Rusty bar here? Did we forget to add the bar-keep :-D"). Drinking already existed <i>aboard</i>
/// (the Galley's "Pour a tot"); this is the same beat <i>ashore</i>, given a face and a name. Each
/// walkable station has one, with a NAME and a per-bar <b>house special</b> (its own drink + flavor
/// text): the Tilt pours something that leans, the Ringside something over a shard of ring ice.
///
/// <para>Pure Core data + pure purchase math (repo agreement §9), so the price/afford/receipt logic is
/// one tested truth the client leans on rather than hand-rolling; the drunkenness itself is <i>not</i>
/// re-implemented here — the client routes a poured drink through the exact same rum-tot law the Galley
/// uses (one wobble law, aboard and ashore).</para>
/// </summary>
/// <param name="House">#756 · This venue's OWN card, or null to pour the shared staples plus the house
/// special (<see cref="DrinkMenu.For"/>). The one field that lets a counter which is not a spaceport bar
/// be served by this machine instead of forking it.</param>
/// <param name="Welcome">#756 · What the counter says when you lean on it, when the default greeting is
/// wrong for the place. Null keeps the barkeep's own line.</param>
/// <param name="DeskArtUrl">#756 · The picture of THIS desk, drawn on the service card. Null draws no
/// picture, which is every haven bar today.</param>
/// <param name="SelfService">#756 · True where there is nobody behind it. Owner's canon for the Hive
/// (#618, skeleton staff): the counter does its own serving, which is worse. A self-service counter has
/// no round to stand and no keep to ask, so the card does not offer either.</param>
/// <param name="BackCounter">#763 · True where there is a second counter behind the first one and the
/// register on it is the outlaw one. Owner's own framing of the SDR kit — <i>"pirate and smuggler standard
/// issue … a purchasable lead; the cantina hall is full of people who know a frequency"</i> — and this flag
/// is where a bar admits to being one of those rooms. Exactly one venue carries it in v1, and it is the one
/// the owner walked into; it is a fact about the PLACE, so a captain who has been there knows and a captain
/// who has not does not.</param>
/// <param name="DrinkArtUrl">#247 · THE HOUSE DRINK, PHOTOGRAPHED. Owner: <i>"many different themed drinks
/// to the place and ingredients available"</i> — each bar's own pour, shot as a cocktail on a scarred steel
/// counter, each with its own tell in the glass.
///
/// <para>It hangs HERE, on the record that already names the pour, rather than on a second table keyed by
/// the same body id: the greeting, the menu row, the foot button and the receipt then read one name, one
/// line and one plate, and cannot come to two answers about what this bar pours. That is this project's
/// fifth bug class with a glass in its hand, and the cheapest way not to have it is not to write the pour
/// down twice.</para>
///
/// <para>Null draws no picture, exactly as <see cref="DeskArtUrl"/> does, and the slot
/// <c>onerror</c>-hides like every other art slot in the game. The Hive's self-serving counter leaves it
/// null: its card brought its own photographs (<see cref="Drink.ArtUrl"/>) and its "house special" is a row
/// on that card, not a seventh spaceport pour.</para></param>
public sealed record Barkeep(
    string BodyId,
    string Name,
    string BarName,
    string DrinkName,
    string DrinkFlavor,
    int DrinkPrice,
    int RoundPrice,
    IReadOnlyList<string> Rumors,
    IReadOnlyList<Drink>? House = null,
    string? Welcome = null,
    string? DeskArtUrl = null,
    bool SelfService = false,
    bool BackCounter = false,
    string? DrinkArtUrl = null)
{
    /// <summary>The barkeep's in-character welcome when you lean on the bar — or the venue's own, where
    /// the venue brought one (#756: a counter with nobody behind it cannot say "what'll it be").</summary>
    public string Greeting => Welcome
        ?? $"“What'll it be? House special's {DrinkName} — {DrinkPrice} cr a glass.”";

    /// <summary>#781 · WHICH tip this hour is — the one place the hourly rotation is worked out, so a caller
    /// that needs to know what the rumour was ABOUT (the keep's own, <see cref="TheKeep.TopicOf"/>) reads the
    /// index the sentence itself came off rather than re-deriving it beside it. Two answers to "which rumour"
    /// is this project's fifth bug class with a glass in its hand. −1 where there is nothing to say.</summary>
    public int RumorIndexAt(double simTime)
    {
        if (Rumors.Count == 0)
        {
            return -1;
        }
        int i = (int)((long)(simTime / 3600) % Rumors.Count);
        return i < 0 ? i + Rumors.Count : i;
    }

    /// <summary>A cheap tip, rotated deterministically by sim time (hourly) — flavor intel for now, the
    /// same no-wall-clock idiom the news wire and rum lines use, so it never flickers frame to frame.</summary>
    public string RumorAt(double simTime) =>
        RumorIndexAt(simTime) is int i && i >= 0 ? Rumors[i] : "The barkeep just shrugs — quiet week for gossip.";

    /// <summary>Pour the house special: debit <see cref="DrinkPrice"/> if the purse covers it. Pure —
    /// returns the new purse, whether it poured, and the in-character receipt line.</summary>
    public BarTab PourHouseSpecial(int credits) => credits >= DrinkPrice
        ? new BarTab(true, DrinkPrice, credits - DrinkPrice,
            $"🍹 {DrinkName} — {DrinkPrice} cr. {DrinkFlavor}")
        : new BarTab(false, DrinkPrice, credits,
            $"“Come back when the purse can cover {DrinkPrice} cr, spacer.”");

    /// <summary>Buy a round for the whole room — a bigger spend (<see cref="RoundPrice"/>) that the
    /// caller turns into goodwill with the regulars drinking here (#247 kin #224). Pure debit + receipt.</summary>
    public BarTab BuyRound(int credits) => credits >= RoundPrice
        ? new BarTab(true, RoundPrice, credits - RoundPrice,
            $"🍻 A round for the house — {RoundPrice} cr. Glasses go up; the room warms to you.")
        : new BarTab(false, RoundPrice, credits,
            $"“A round's {RoundPrice} cr — you're a little short, friend.”");
}

/// <summary>The outcome of a bar purchase: did it pour, what it cost, the purse after, and the line the
/// barkeep says. A <c>readonly record struct</c> — pure value, easy to assert in a test.</summary>
public readonly record struct BarTab(bool Poured, int Cost, int RemainingCredits, string Line);

/// <summary>
/// The barkeeps of the walkable havens (#247). Keyed by the station's body id — the same ids
/// <c>HavenInterior</c> builds interiors for. One barkeep per bar; each has a name and its own house
/// special. Note there are four <i>places</i> (the Rusty Roadstead a.k.a. "The Space Bar" is one bar,
/// not two): the id <c>the-space-bar</c> is the Rusty Roadstead's berth.
/// </summary>
public static class Barkeeps
{
    // ── #247 · EVERY BAR POURS ITS OWN ──────────────────────────────────────────────────────────────────
    //
    // Owner, filing the scope: "many different themed drinks to the place and ingredients available… The
    // ingredient list doubles as worldbuilding — every cocktail is a geography lesson."
    //
    // So the seven house pours below are re-authored to say WHERE THEY CAME FROM. A Hellas still, lime grown
    // in cloud condensate, one shard of ring ice, a spoon of park brine, polar ice that took longer to get
    // here than you did, a hydroponic tomato, a drop of ink bitters. Each line is three ingredients and one
    // fact about the place, and not one of them explains anything: the coin on the Ringside counter is never
    // accounted for, the Cinder keep will not name the bitters, and the Deep's walls are the ice.
    //
    // THIS IS THE ONE PLACE THE POUR IS WRITTEN DOWN. The name and the line below are what the greeting
    // quotes, what the menu row prints, what the foot button says and what the receipt reads back — and now
    // also what the plate beside the row is a picture of (DrinkArtUrl). A second table keyed by the same
    // body id would have been two answers to "what does this bar pour", which is exactly the bug class this
    // repo has already named twice.
    private static readonly Barkeep[] All =
    [
        // The Rusty Roadstead — Mars. The one the owner walked into.
        new("the-space-bar", "“Rusty” Meg Calloway", "THE ROADSTEAD BAR",
            "DUST DEVIL",
            "Mescal off a Hellas still, a red chilli tincture, salt from the flats. It is served in a heavy glass because the light ones leave.",
            6, 40,
            [
                "“Dockmaster's been slow stamping papers — grease him and you'll clear customs by supper.”",
                "“Word is a cherry-red wreck's drifting sunward. Salvage boys are scared of it. Make of that what you will.”",
                "“Most guests stay two weeks. You look like a one-drink-and-gone sort.”",
            ],
            // #763 · …and there is a second counter behind this one. Owner's own room: the salvage boys, the
            // dockmaster's price, and the people who know a frequency all drink here.
            BackCounter: true,
            DrinkArtUrl: "art/drink-space-bar.jpg"),

        // The Cinder Lounge — in Venus' clouds.
        new("cinder-roost", "Ember Vance", "THE CINDER LOUNGE",
            "SULPHUR SOUR",
            "Cane spirit, lime grown in cloud condensate, and a yellow bitters the keep will not name. Drink it before the colour settles.",
            7, 45,
            [
                "“Bonded Stores hatch out on the concourse? Cracked more than once, they say. Nobody heard it from me.”",
                "“A runner called the Magpie flits through here — never sits still. Catch them if the watch is right.”",
                "“The clouds pay well and cost worse. Same as the drinks.”",
            ],
            DrinkArtUrl: "art/drink-cinder-roost.jpg"),

        // The Ringside Bar — Saturn's rings.
        new("ringside-exchange", "Cassini “Cass” Roe", "THE RINGSIDE BAR",
            "RINGSIDE",
            "Rye over one shard of ring ice, and a coin on the counter you did not order. Everybody leaves the coin.",
            8, 50,
            [
                "“Trade fast — the rings don't wait, and neither does the fence upstairs.”",
                "“Freighters cut the gap dark this season. A ghost or two you'll never see on the board.”",
                "“Tip the ring back at the end. Bad luck to leave it, out here.”",
            ],
            DrinkArtUrl: "art/drink-ringside.jpg"),

        // The Earthrise Bar — Selene Gate, in orbit off Luna. The oldest port in the system (#352, owner
        // playtest 2026-07-18: "there is nothing here to walk to"). Customs that have seen everything; an
        // old-timer keep who pours the one drink in the system whose ice is older than the crossing.
        new("selene-gate", "Marisol “Mare” Okonkwo", "THE EARTHRISE BAR",
            "EARTHRISE",
            "Regolith-filtered vodka, curaçao off the Earth freight, one lump of polar ice that took longer to get here than you did.",
            7, 45,
            [
                "“Oldest gate in the system, this. Customs has stamped worse than you and smiled doing it.”",
                "“Half of Earth's traffic pauses here before the long fall out-system. You'd be amazed what they leave on the bar.”",
                "“Look up — that's home in the window. Everyone drinks a little slower once they've seen it.”",
            ],
            DrinkArtUrl: "art/drink-earthrise.jpg"),

        // The Stormwatch Bar — The Red Eye, in orbit off Jupiter. The storm-watcher port (#352 follow-
        // through, night shift 2026-07-18→19); pilgrims come to stare at the Great Red Spot, and the keep
        // times their drink against a storm that has not moved in four centuries.
        new("red-eye", "Galiana “Gale” Marek", "THE STORMWATCH BAR",
            "THE BLINK",
            "Gin, a hydroponic tomato, cracked black pepper. Finish it before the Spot moves; the Spot does not move.",
            8, 50,
            [
                "“Pilgrims come to stare at the Spot. Stay long enough and it stares back — that's when they leave.”",
                "“Anything red in the window, anything red in the glass. We keep it simple this far out.”",
                "“The storm's older than every soul who ever docked here. Humbling, if you let it be.”",
            ],
            DrinkArtUrl: "art/drink-red-eye.jpg"),

        // The Deep End — The Deep, out at Neptune, the farthest port in the system (#352 follow-through,
        // night shift 2026-07-18→19). Cold, half-empty, frost on the pipes; the keep is the one who never
        // left, and serves the last honest drink before the long dark without a cube in it.
        new("the-deep", "Elias “Rime” Kaddour", "THE DEEP END",
            "TRENCH",
            "Dark rum, cold tea, a drop of ink bitters, no ice. The walls are the ice.",
            8, 50,
            [
                "“End of every road, this. Past the window it's just dark all the way down.”",
                "“Half the berths froze shut seasons back. Quiet suits some folk — suits me.”",
                "“Mind the frost on the rail. Everything out here's colder than it looks, drinks included.”",
            ],
            DrinkArtUrl: "art/drink-the-deep.jpg"),

        // The Tilt Bar — out at Uranus, where everything's sideways. Owner, eight game-years from home with
        // the wallet delivered and nothing to pour: "Oh the Tilt bar should have drinks available also :-D"
        new("the-tilt", "Halden Frost", "THE TILT BAR",
            "THE LIST",
            "Aquavit, a spoon of park brine, in a glass that leans. Out here everything leans; the glass is only honest.",
            7, 45,
            [
                "“Everything's sideways this far out. Your credits included — spend 'em before they roll off.”",
                "“Somebody's always looking to crack a lockup here. The codes get around.”",
                "“Drink it while it leans. Stand the glass up straight and you'll be wearing it.”",
            ],
            DrinkArtUrl: "art/drink-the-tilt.jpg"),
    ];

    /// <summary>The barkeep for a station body, or null if that berth has no bar (no walkable interior).</summary>
    public static Barkeep? For(string bodyId) => Array.Find(All, b => b.BodyId == bodyId);

    /// <summary>Every barkeep — for tests and any "who tends where" listing.</summary>
    public static IReadOnlyList<Barkeep> AllBarkeeps => All;
}
