namespace SpaceSails.Core.Interior;

/// <summary>
/// The barkeep behind a haven bar (#247, owner ashore at the Rusty Roadstead: "How do I get a drink
/// at the Rusty bar here? Did we forget to add the bar-keep :-D"). Drinking already existed <i>aboard</i>
/// (the Galley's "Pour a tot"); this is the same beat <i>ashore</i>, given a face and a name. Each
/// walkable station has one, with a NAME and a per-bar <b>house special</b> (its own drink + flavor
/// text): the Tilt pours something cold and blue, the Ringside something with a ring in it.
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
    bool SelfService = false)
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
    private static readonly Barkeep[] All =
    [
        // The Rusty Roadstead — Mars. The one the owner walked into.
        new("the-space-bar", "“Rusty” Meg Calloway", "THE ROADSTEAD BAR",
            "the Rusted Bolt",
            "Rocket-grade whiskey with a curl of iron filings settling red as the dust outside. Goes down like a hull breach — you'll feel it.",
            6, 40,
            [
                "“Dockmaster's been slow stamping papers — grease him and you'll clear customs by supper.”",
                "“Word is a cherry-red wreck's drifting sunward. Salvage boys are scared of it. Make of that what you will.”",
                "“Most guests stay two weeks. You look like a one-drink-and-gone sort.”",
            ]),

        // The Cinder Lounge — in Venus' clouds.
        new("cinder-roost", "Ember Vance", "THE CINDER LOUNGE",
            "the Sulphur Sour",
            "Citrus and brimstone, served smoking under a freshly struck match. Mind the sulphur, spacer — it bites going down and again coming up.",
            7, 45,
            [
                "“Bonded Stores hatch out on the concourse? Cracked more than once, they say. Nobody heard it from me.”",
                "“A runner called the Magpie flits through here — never sits still. Catch them if the watch is right.”",
                "“The clouds pay well and cost worse. Same as the drinks.”",
            ]),

        // The Ringside Bar — Saturn's rings.
        new("ringside-exchange", "Cassini “Cass” Roe", "THE RINGSIDE BAR",
            "the Ringside",
            "Poured over a single hand-cut ice ring calved off Saturn's own — drink it before the ring melts and the ring's the best part.",
            8, 50,
            [
                "“Trade fast — the rings don't wait, and neither does the fence upstairs.”",
                "“Freighters cut the gap dark this season. A ghost or two you'll never see on the board.”",
                "“Tip the ring back at the end. Bad luck to leave it, out here.”",
            ]),

        // The Earthrise Bar — Selene Gate, in orbit off Luna. The oldest port in the system (#352, owner
        // playtest 2026-07-18: "there is nothing here to walk to"). Customs that have seen everything; an
        // old-timer keep who pours something pale as Earthlight and remembers every face that came through.
        new("selene-gate", "Marisol “Mare” Okonkwo", "THE EARTHRISE BAR",
            "the Earthshine",
            "Pale gin gone silver under the bar light, poured slow as the Earth turns in the window. The oldest recipe on the oldest port — smooth going down, and it remembers everything.",
            7, 45,
            [
                "“Oldest gate in the system, this. Customs has stamped worse than you and smiled doing it.”",
                "“Half of Earth's traffic pauses here before the long fall out-system. You'd be amazed what they leave on the bar.”",
                "“Look up — that's home in the window. Everyone drinks a little slower once they've seen it.”",
            ]),

        // The Stormwatch Bar — The Red Eye, in orbit off Jupiter. The storm-watcher port (#352 follow-
        // through, night shift 2026-07-18→19); pilgrims come to stare at the Great Red Spot and the keep
        // pours something that swirls red and never quite settles, same as the storm out the window.
        new("red-eye", "Galiana “Gale” Marek", "THE STORMWATCH BAR",
            "the Eyewall",
            "A slow red swirl that never quite settles — the same storm as the Spot outside, poured into a glass and still turning. It's been going four centuries; give it an hour and you'll watch it all night.",
            8, 50,
            [
                "“Pilgrims come to stare at the Spot. Stay long enough and it stares back — that's when they leave.”",
                "“Anything red in the window, anything red in the glass. We keep it simple this far out.”",
                "“The storm's older than every soul who ever docked here. Humbling, if you let it be.”",
            ]),

        // The Deep End — The Deep, out at Neptune, the farthest port in the system (#352 follow-through,
        // night shift 2026-07-18→19). Cold, half-empty, frost on the pipes; the keep is the one who never
        // left, and pours the last honest drink before the long dark.
        new("the-deep", "Elias “Rime” Kaddour", "THE DEEP END",
            "the Deep Freeze",
            "Poured over a shard of pipe-frost and left to fog the glass — colder than the window, darker than the water. The last honest drink before the long fall out-system, and Rime pours it slow because out here there's nowhere else to be.",
            8, 50,
            [
                "“End of every road, this. Past the window it's just dark all the way down.”",
                "“Half the berths froze shut seasons back. Quiet suits some folk — suits me.”",
                "“Mind the frost on the rail. Everything out here's colder than it looks, drinks included.”",
            ]),

        // The Tilt Bar — out at Uranus, where everything's sideways.
        new("the-tilt", "Halden Frost", "THE TILT BAR",
            "the Sideways Blue",
            "A tall glass of something electric-blue and iceberg-cold, poured at a tilt to serve — because everything's sideways out here and the drink knows it.",
            7, 45,
            [
                "“Everything's sideways this far out. Your credits included — spend 'em before they roll off.”",
                "“Somebody's always looking to crack a lockup here. The codes get around.”",
                "“Cold enough that the Blue stays blue. Warms up, it goes green — don't ask.”",
            ]),
    ];

    /// <summary>The barkeep for a station body, or null if that berth has no bar (no walkable interior).</summary>
    public static Barkeep? For(string bodyId) => Array.Find(All, b => b.BodyId == bodyId);

    /// <summary>Every barkeep — for tests and any "who tends where" listing.</summary>
    public static IReadOnlyList<Barkeep> AllBarkeeps => All;
}
