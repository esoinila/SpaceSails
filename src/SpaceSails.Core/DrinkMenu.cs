using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// What a drink is FOR, mechanically — the little channel of info the owner asked the menu to open
/// (Sunday morning ruling 2026-07-19: "we want the drinks to give that little channel of info to
/// us… when we offer a drink the person we offer it to tells us something with the drink they
/// choose"). The category is the channel: the pour a contact reaches for colours WHAT they let slip.
/// </summary>
public enum DrinkCategory
{
    /// <summary>Clear and sharp — the business drink. A gin loosens the harder, more actionable tell:
    /// a proposition, a price, a name off the books.</summary>
    Gin,

    /// <summary>Easy and loud — the small-talk drink. A beer never opens the vault, but a talkative
    /// spacer always names one real fact between the foam.</summary>
    Beer,

    /// <summary>The house's own pour, mixed nowhere else — the LOCAL drink. Reaching for the specialty
    /// is a nod to the neighbourhood, and it loosens neighbourhood gossip: who runs what through THIS
    /// port.</summary>
    Specialty,
}

/// <summary>
/// One drink on a bar's menu (#4–#5, SundayMorningWind). A stable id, the name the card shows, the
/// <see cref="DrinkCategory"/> that decides which channel of info it opens, and a colourful flavour
/// line in the loving Leisure-Suit-Larry key the owner asked for (homage, never reproduction — the
/// house voice does the lounge-lizard wink itself). Pure data — a <c>readonly record struct</c> so a
/// save layer or a test handles it flat.
/// </summary>
public readonly record struct Drink(string Id, string Name, DrinkCategory Category, string Flavor);

/// <summary>
/// The talking drinks menu (owner, Sunday morning 2026-07-19: "The drinks menu should have more than
/// one drink-type… Space Gin, Space Beer (with colorful description in the style of Leisure Suit
/// Larry game) and some local specialty… we get the character favorite known to us when we offer").
///
/// <para>Two shared staples pour at every bar — <see cref="SpaceGin"/> and <see cref="SpaceBeer"/> —
/// plus a harder shared <see cref="RocketFuel"/> for the ones who mean it, and then each bar's OWN
/// <see cref="SpecialtyOf">house special</see>, kept exactly as the <see cref="Barkeep"/> already
/// pours it (the Rusted Bolt, the Earthshine, the Eyewall…). Pure Core data so the menu, the
/// favourites, and the choice-as-tell are one tested truth the client renders.</para>
/// </summary>
public static class DrinkMenu
{
    /// <summary>Space Gin — the sharp one. Business gets honest over it.</summary>
    public static readonly Drink SpaceGin = new(
        "space-gin", "Space Gin", DrinkCategory.Gin,
        "Clear as a customs officer's conscience and twice as sharp. Distilled somewhere it's rude to ask about, poured over a cube of genuine comet, and it goes down like a docking clamp that missed. One sip and you're suddenly the most interesting spacer in the room — or so the gin insists. Straighten your collar, captain: business is about to get honest.");

    /// <summary>Space Beer — the loud one. A talkative spacer always names one real fact.</summary>
    public static readonly Drink SpaceBeer = new(
        "space-beer", "Space Beer", DrinkCategory.Beer,
        "Amber, loud, and utterly without shame — the working spacer's confidant. The head foams like a decompression warning and the bubbles have opinions. It won't make you clever, but it'll make you talkative, and somewhere between the second and third glass a fellow always lets slip the one little fact he came in swearing to keep.");

    /// <summary>Rocket Fuel — the hard one. The fixers knock it back and mean every word after.</summary>
    public static readonly Drink RocketFuel = new(
        "rocket-fuel", "Rocket Fuel", DrinkCategory.Gin,
        "They don't call it that because it's subtle. A shot of something that started life as engine cleaner and got ideas above its station — clear, oily, and technically flammable, which the bar-keep will demonstrate for a tip. It strips the varnish off a bad week and the good sense off a careful one. The fixers drink it neat and mean every word they say after.");

    /// <summary>The staples poured at every bar, in menu order — the shared cast the owner named, plus
    /// the harder pour for good measure.</summary>
    public static readonly IReadOnlyList<Drink> Staples = [SpaceGin, SpaceBeer, RocketFuel];

    /// <summary>This bar's house special as a menu <see cref="Drink"/>, lifted verbatim from the
    /// <see cref="Barkeep"/> so the name and flavour the keep already pours are the same one the menu
    /// shows. Categorised <see cref="DrinkCategory.Specialty"/> — the local channel.</summary>
    public static Drink SpecialtyOf(Barkeep keep)
    {
        ArgumentNullException.ThrowIfNull(keep);
        return new Drink($"special:{keep.BodyId}", keep.DrinkName, DrinkCategory.Specialty, keep.DrinkFlavor);
    }

    /// <summary>The full menu at one bar: the shared staples, then this bar's local specialty last (the
    /// house's own pour reads at the bottom of the card, where a special belongs).</summary>
    public static IReadOnlyList<Drink> For(Barkeep keep)
    {
        ArgumentNullException.ThrowIfNull(keep);
        return [.. Staples, SpecialtyOf(keep)];
    }

    /// <summary>Every drink anywhere in the system — the shared staples plus every bar's specialty, one
    /// deduped catalog. The stable pool a contact's FAVOURITE is drawn from, so a favourite is the same
    /// pour no matter which bar you meet them in. Ordered (staples first, then specialties by body id)
    /// so the seeded favourite pick is deterministic across sessions.</summary>
    public static IReadOnlyList<Drink> Catalog
    {
        get
        {
            var all = new List<Drink>(Staples);
            foreach (Barkeep keep in Barkeeps.AllBarkeeps.OrderBy(b => b.BodyId, StringComparer.Ordinal))
            {
                Drink special = SpecialtyOf(keep);
                if (!all.Any(d => d.Id == special.Id))
                {
                    all.Add(special);
                }
            }

            return all;
        }
    }

    /// <summary>Find a drink by id anywhere in the catalog (the load path — a saved favourite id back to
    /// its full <see cref="Drink"/>). Null when the id names no drink we still pour.</summary>
    public static Drink? ById(string? drinkId)
    {
        if (string.IsNullOrEmpty(drinkId))
        {
            return null;
        }

        foreach (Drink d in Catalog)
        {
            if (d.Id == drinkId)
            {
                return d;
            }
        }

        return null;
    }
}

/// <summary>
/// Every named contact has a FAVOURITE drink (owner: "we get the character favorite known to us when
/// we offer"). It is derived deterministically from the contact id, so it never changes across
/// sessions — but the known cast get AUTHORED anchors where flavour demands one, which is also how
/// "the barkeep's special is somebody's favourite by construction" holds: One-Eye Silas drinks the
/// Rusted Bolt, Gilt-Eye drinks the Earthshine that remembers everything. Pure and testable.
/// </summary>
public static class DrinkFavorites
{
    // Authored anchors for the known cast, matched by the same shout-name keyword ContactSheets uses.
    // Each maps a contact to a drink id in DrinkMenu.Catalog; an anchor that names a specialty makes
    // that bar's house special somebody's favourite by construction (the owner's ask).
    private static readonly (string Keyword, string DrinkId)[] Anchors =
    [
        // One-Eye Silas — the gruff bounty fence at the Roadstead bar drinks the local iron whiskey.
        ("SILAS", "special:the-space-bar"),
        // Gilt-Eye — the appraising intel dealer favours the Earthshine, "the oldest recipe… it
        // remembers everything." A drink for someone who trades in what people forget they said.
        ("GILT", "special:selene-gate"),
        // Madam Coil — warm-underworld, runs quiet parcels; the gin, where business gets honest.
        ("COIL", "space-gin"),
        // The Fixer — clipped, off-the-books; Rocket Fuel, neat, and means every word after.
        ("FIXER", "rocket-fuel"),
        // The Magpie — flighty, never sits still; a beer's small talk between flits.
        ("MAGPIE", "space-beer"),
    ];

    /// <summary>The contact's favourite drink. An authored anchor wins where one exists (the known
    /// cast, flavour-true); otherwise it is a stable seeded pick from <see cref="DrinkMenu.Catalog"/>,
    /// folded from the contact id so it is identical every session.</summary>
    public static Drink FavoriteFor(string contactId)
    {
        IReadOnlyList<Drink> catalog = DrinkMenu.Catalog;
        string shout = (contactId ?? string.Empty).ToUpperInvariant();

        foreach ((string keyword, string drinkId) in Anchors)
        {
            if (shout.Contains(keyword, StringComparison.Ordinal) && DrinkMenu.ById(drinkId) is { } anchored)
            {
                return anchored;
            }
        }

        ulong seed = DiceRule.Seed(0UL, $"favorite:{shout}");
        int idx = new DeterministicRandom(seed).NextInt(0, catalog.Count);
        return catalog[idx];
    }

    /// <summary>True when <paramref name="drink"/> is this contact's favourite (compared by id).</summary>
    public static bool IsFavorite(string contactId, Drink drink) => FavoriteFor(contactId).Id == drink.Id;
}

/// <summary>
/// The choice IS the tell (owner: "when we offer a drink the person we offer it to tells us something
/// with the drink they choose"). Given a menu, a contact reaches — deterministically — for a pour:
/// their favourite if this bar pours it, else the nearest thing to it, else a stable seeded pick. What
/// they reach for is the channel their tell rides (<see cref="Drink.Category"/>).
/// </summary>
public static class DrinkChoice
{
    /// <summary>Which drink this contact takes when offered from <paramref name="menu"/>. Their
    /// favourite if it is on the menu; failing that the nearest pour in the same category; failing that
    /// a stable seeded pick off the menu. Deterministic from the contact id, so the same contact at the
    /// same bar always reaches for the same glass.</summary>
    public static Drink ChoosesDrink(string contactId, IReadOnlyList<Drink> menu)
    {
        ArgumentNullException.ThrowIfNull(menu);
        if (menu.Count == 0)
        {
            return DrinkMenu.SpaceBeer; // an empty menu never happens in play; a safe, cheap default.
        }

        Drink favorite = DrinkFavorites.FavoriteFor(contactId);

        // 1) The favourite itself, if this bar pours it.
        foreach (Drink d in menu)
        {
            if (d.Id == favorite.Id)
            {
                return d;
            }
        }

        // 2) The nearest pour in the same category (a gin drinker takes another gin).
        var sameCategory = new List<Drink>();
        foreach (Drink d in menu)
        {
            if (d.Category == favorite.Category)
            {
                sameCategory.Add(d);
            }
        }

        IReadOnlyList<Drink> pool = sameCategory.Count > 0 ? sameCategory : menu;
        ulong seed = DiceRule.Seed(0UL, $"choose:{(contactId ?? string.Empty).ToUpperInvariant()}");
        return pool[new DeterministicRandom(seed).NextInt(0, pool.Count)];
    }
}

/// <summary>
/// The little channel of info a chosen drink opens (owner: "that little channel of info"). The concrete
/// intel is live and stays client-side (a ghost's route, a heat warning — it reads real game state);
/// what Core owns is WHICH channel a drink opens and the in-character lead-in that frames it, so the
/// mapping from a pour to a kind of tell is one tested truth.
/// </summary>
public static class DrinkTell
{
    /// <summary>The channel a drink's category opens: the specialty loosens LOCAL rumour, a beer names a
    /// plain FACT over small talk, a gin (or the harder stuff) opens the sharper BUSINESS tell.</summary>
    public static TellChannel ChannelFor(Drink drink) => drink.Category switch
    {
        DrinkCategory.Specialty => TellChannel.LocalRumor,
        DrinkCategory.Beer => TellChannel.SmallTalk,
        _ => TellChannel.Business,
    };

    /// <summary>The in-character lead-in a contact says as they lift the glass they chose — the frame the
    /// client hangs the concrete tell on. Pure, deterministic flavour keyed to the drink and the name.</summary>
    public static string LeadFor(Drink drink, string display) => drink.Category switch
    {
        DrinkCategory.Specialty =>
            $"{display} takes the {drink.Name} — the local pour — and leans into the neighbourhood's own gossip:",
        DrinkCategory.Beer =>
            $"{display} takes a {drink.Name}, easy and loud, and lets one plain fact slip between the foam:",
        _ =>
            $"{display} takes the {drink.Name}, and over something this sharp the talk turns to business:",
    };
}

/// <summary>Which kind of tell a chosen drink opens — the channel the owner asked the menu to give us.</summary>
public enum TellChannel
{
    /// <summary>Neighbourhood gossip, loosened by the house's own pour (a Specialty).</summary>
    LocalRumor,

    /// <summary>A single plain fact, named over easy small talk (a Beer).</summary>
    SmallTalk,

    /// <summary>The sharper, actionable tell — a name, a route, a proposition (a Gin / the hard stuff).</summary>
    Business,
}
