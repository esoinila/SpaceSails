using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #535 · <b>THE CODE THAT UNHAPPENS AN ENCOUNTER.</b>
///
/// <para>Owner: <i>"In Expanse they found black ops code keys from the Tacchi that made the Mars military
/// leave them alone and delete all records of ever encountering them. That kind of keys would be very
/// valuable for pirates. 😎"</i> — and, a breath later, the better half of the idea: <i>"We could use these
/// keys to drop heat at a tight spot 😎"</i>.</para>
///
/// <h3>Two halves, and the second one is the treasure</h3>
///
/// <para>Every other way of surviving a catch leaves a mark: heat rises, a collector remembers, the wire
/// files it. A key is the only thing in this game that reaches back and removes the fact. So the object has
/// two spends and BOTH consume it: <b>present it</b> at a catch, and the encounter never happened; <b>burn
/// it cold</b> from the satchel, and a band comes off the meter of whoever is standing over you.</para>
///
/// <h3>What this file is, and what it is not</h3>
///
/// <para>Pure rules and the canon strings, nothing else. It does not spawn anything, it does not touch a
/// ledger and it does not know what a hunter is — the client spends it, exactly the way it spends every
/// other carried thing. <b>And it never says who made it.</b> That is a canon law rather than an omission:
/// a code that named its issuer would answer the one question the whole object is interesting for.</para>
///
/// <para>Canon pass, 2026-09-05 (on the issue): the name, the look-card line, the two verbs, the outcome
/// plate and the burn line are authored there and copied here verbatim. Nothing else in this feature says
/// anything at all.</para>
/// </summary>
public static class BlackOpsKey
{
    // ── THE OBJECT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Canon. The glyph it wears in a pocket, on a plate, and over the place it is lying.</summary>
    public const string Glyph = "🗝";

    /// <summary>Canon. What it is called, and the only name it ever has.</summary>
    public const string Name = "Black-ops key";

    /// <summary>Canon. The whole of the look card (#614's idiom): what the object IS, and not one word about
    /// what to do with it or whose it was.</summary>
    public const string LookCardLine =
        "A code somebody paid a great deal to make sure nobody would ever read. It works once.";

    /// <summary>Canon. The fifth exit on the BUSTED card.</summary>
    public const string PresentVerb = "Present the key";

    /// <summary>Canon. The satchel's verb, available any time the thing is in the pocket.</summary>
    public const string BurnVerb = "Burn the key";

    /// <summary>
    /// Canon. <b>THE WHOLE OF WHAT THE PLAYER IS TOLD when a key is presented</b>, and #761's law is met by
    /// it: a plate, on the card they are already looking at, at the moment it happens.
    ///
    /// <para>No sentence under it. Every other exit from a catch narrates itself — coin changes hands, a hold
    /// is emptied, somebody peels off nursing a dent — and this one is the absence of all of that. The
    /// silence IS the treasure, and a paragraph explaining that nothing was filed would be the game filing
    /// something.</para>
    /// </summary>
    public const string NoContactLoggedPlate = "NO CONTACT LOGGED";

    /// <summary>Canon. The one line a burn says, on the pulse, once.</summary>
    public const string BurnLine = "Somewhere a file closes. The key is ash.";

    /// <summary>The head of the look card: the canon name, in the plate typography every carried object's
    /// card is titled in. Not a second name — the same one, shouted.</summary>
    public static string CardLabel => Glyph + " " + Name.ToUpperInvariant();

    /// <summary>Caption-only, the deliberate no-picture idiom (#528's odd book, #537's cutting rig): a card
    /// that never claims a painting rather than one that wires an unpainted file and hides it on error.</summary>
    public static CarriedObject.Reveal Card => new(string.Empty, CardLabel, LookCardLine);

    // ── IN THE POCKET ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>One key in the satchel. Its id is <b>the hull it came off</b>, for the reason
    /// <see cref="Satchel.Add"/> makes unavoidable: only rounds stack, so two keys sharing one id would be
    /// one key, and the issue's own scarcity rule (<i>"finding two is a run to remember"</i>) would be
    /// arithmetic that could never happen.</summary>
    public static Satchel.Item FoundOn(string wreckId)
    {
        ArgumentNullException.ThrowIfNull(wreckId);
        return new Satchel.Item(Satchel.Kind.BlackOpsKey, wreckId);
    }

    /// <summary>Is this row one of them?</summary>
    public static bool IsTheKey(Satchel.Item item) => item.Kind == Satchel.Kind.BlackOpsKey;

    /// <summary>The first one in the pocket, or null. The one a catch spends, and the one the client asks
    /// about to decide whether the fifth exit is drawn at all.</summary>
    public static Satchel.Item? InThePocket(IReadOnlyList<Satchel.Item>? carried)
    {
        foreach (Satchel.Item item in carried ?? [])
        {
            if (IsTheKey(item))
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>How many are carried. Both spends take exactly one.</summary>
    public static int CountIn(IReadOnlyList<Satchel.Item>? carried) =>
        Satchel.CountOf(carried, Satchel.Kind.BlackOpsKey);

    /// <summary>Spend one — the same call for both spends, so a presentation and a burn can never come to
    /// disagree about what "consumed" means.</summary>
    public static IReadOnlyList<Satchel.Item> Spend(IReadOnlyList<Satchel.Item>? carried, Satchel.Item key) =>
        Satchel.Remove(carried, Satchel.Kind.BlackOpsKey, key.Id);

    // ── WHERE THEY LIE ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #535 · <b>ONLY ON A HULL THAT FOUGHT</b>, which in this build means exactly one thing: the hull
    /// #538's black-ops sweep team comes aboard to make stop existing as evidence
    /// (<see cref="Derelict.WreckCause.InsuranceJob"/> — the one cause <c>Map.Surface</c> spawns them on).
    ///
    /// <para>Canon, 2026-09-05: <i>"dealt by the wreck salvage roll on hulls that fought (the black-ops
    /// sweep's own kind, #538's hulls and the Q-ship class when #534 builds) — never on a merchant, never
    /// bought."</i> The Q-ship class is not a cause yet, so this switch has one arm and will grow a second
    /// the day #534 lands; it is written as a switch rather than an equality for that reason.</para>
    ///
    /// <para><b>And never <see cref="Derelict.WreckCause.Piracy"/>, which is the arm somebody will reach
    /// for.</b> That hull was boarded and stripped in a hurry with her deep hold untouched — she is the
    /// merchant the canon rules out, read from the other end.</para>
    /// </summary>
    public static bool CauseMayCarryOne(Derelict.WreckCause cause) => cause switch
    {
        Derelict.WreckCause.InsuranceJob => true,
        _ => false,
    };

    /// <summary>One eligible hull in this many is actually carrying one. FLAGGED for the owner's tuning, and
    /// the only number in this file — the scarcity the whole design rests on lives here and nowhere else.
    ///
    /// <para>It sits on top of the cause gate, not beside it, so the rate a captain actually meets is this
    /// one THROUGH the frequency of the sweep's own hull class. <c>TheBlackOpsKeyTests</c> measures both.</para></summary>
    public const int OneInEligibleHulls = 5;

    /// <summary>
    /// <b>THE WRECK SALVAGE ROLL.</b> Is there a key on THIS hull? Seeded off her id, so a reload finds the
    /// same ship, a captain who has heard a rumour about a hull can go and look, and leaving and coming back
    /// is not a re-roll.
    ///
    /// <para><b>The id goes into the seed WHOLE</b> — <see cref="DiceRule.Seed(string, long[])"/> folds the
    /// characters itself. It is not hashed with <c>string.GetHashCode</c> first, which is the tempting one
    /// line and is randomised per process in .NET: a roll seeded that way is a different roll every time the
    /// tab is reloaded, so the ship a rumour named would be carrying a key this afternoon and not tomorrow.
    /// Measured, not assumed — the first cut of this method did it that way, and
    /// <c>TheRarityIsTheOneThatWasMeasured</c> reported two different rates on two consecutive runs.</para>
    /// </summary>
    public static bool IsAboard(string wreckId, Derelict.WreckCause cause)
    {
        ArgumentNullException.ThrowIfNull(wreckId);
        if (!CauseMayCarryOne(cause))
        {
            return false;
        }

        return DiceRule.Roll(DiceRule.Seed($"black-ops-key-aboard:{wreckId}"), OneInEligibleHulls).Face == 1;
    }

    // ── WHAT A BURN COSTS THE FILE ──────────────────────────────────────────────────────────────────────

    /// <summary>#535/#938 · The reason written into an outfit's book when a key is burned against it. Not
    /// prose and never shown: <see cref="IllegalHeat.Scrub"/> wants a caller who says WHY, so an edit in that
    /// ledger can be told apart from an hour of absence.</summary>
    public const string ScrubReason = "a key burned cold";

    /// <summary>Every sentence this feature can put on a screen, for the audit that reads them all. Two, and
    /// the plate is one of them.</summary>
    public static IEnumerable<string> EveryLine()
    {
        yield return LookCardLine;
        yield return BurnLine;
        yield return NoContactLoggedPlate;
    }
}
