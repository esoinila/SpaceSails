using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #233 · THE BLACKMAIL TWIN — what is wedged between the seats when it is not a wallet.
///
/// <para>Owner, Friday-second morning: <i>"I was thinking of trying the car mission (the other variant of
/// this could be a set of compromising data / photos between the seats)."</i></para>
///
/// <para>Canon pass (Fable, 2026-09-05). The fetch machinery is the roadster's and does not change: the
/// Fixer's tip, the scope hunt, the coast alongside, the prise. What changes is <b>what is in the car</b>,
/// and therefore what the job can be ENDED with. One roadster in <see cref="OneInEvery"/> carries this
/// instead of the hardware wallet — dealt by the same booth-stable seed idiom the wallet's own hand-off
/// address is dealt by (a slow sim-time rotation salted by the berth), so the twin is a thing the world
/// deals and never a thing a frame rolls.</para>
///
/// <h3>Three endings, three verbs the game already had</h3>
/// <list type="bullet">
/// <item><b>Give it back.</b> The contract pays exactly what the contract said. The client says
/// <see cref="ClientLine"/> — once — and never says whose photographs they are, because a client who named
/// them would be telling you what you are holding, and the whole point is that he cannot afford to.</item>
/// <item><b>Sell it.</b> The dark-web desk's buyer seam pays <see cref="FencePrice"/>, which is more than
/// the contract and is DERIVED from the contract — the market's own certainty premium on top of the job's
/// own price, never a typed number. The fence says <see cref="FenceLine"/> and never names the buyer. One
/// band of heat lands on the book of whoever runs the ground you sold it from (#715's step).</item>
/// <item><b>Bury it.</b> #223's verb, evidence off the books. The chest's manifest lists it as the chip
/// (<see cref="Manifest"/>) and it is flagged HOT there — #202's flag, on an entry that is not a cargo
/// class anybody trades. Nothing is said, because there is nobody to say it to.</item>
/// </list>
///
/// <para>Pure and deterministic like everything in Core: the same berth at the same watch deals the same
/// car, and the fence quotes the same price for the same contract, always.</para>
/// </summary>
public static class CompromisingChip
{
    // ── WHAT IT IS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Canon. The glyph it wears in a pocket and at the head of its card.</summary>
    public const string Glyph = "💾";

    /// <summary>Canon. What it is called, and the only name it ever has — on the satchel row and on the
    /// manifest of a chest it is buried in.</summary>
    public const string Name = "A data chip";

    /// <summary>Canon. The whole of the look card (#614's idiom): what the object IS, and not one word
    /// about whose it is or what to do with it.</summary>
    public const string LookCardLine =
        "Photographs. Two people who are not supposed to know each other, and a timestamp that says they did.";

    /// <summary>The satchel id. Stable and singular — there is one lost roadster, so there is one chip, and
    /// the id never carries the hull it came out of.</summary>
    public const string FindId = "roadster-data-chip";

    /// <summary>The head of the look card: the canon name in the plate typography every carried object's
    /// card is titled in. Not a second name — the same one, shouted.</summary>
    public static string CardLabel => Glyph + " " + Name.ToUpperInvariant();

    /// <summary>Caption-only, the deliberate no-picture idiom (#528's odd book, #535's key). A card of
    /// photographs that showed you the photographs would be the game naming what the client cannot.</summary>
    public static CarriedObject.Reveal Card => new(string.Empty, CardLabel, LookCardLine);

    /// <summary>The satchel row's prose, built from the two canon fragments and nothing else.</summary>
    public static string RowLabel => Glyph + " " + Name;

    // ── WHOSE POCKET, AND WHICH ONE ───────────────────────────────────────────────────────────────────

    /// <summary>Mint it. <see cref="Satchel.Kind.Dirt"/> is exactly what this is — the satchel's own words:
    /// <i>a file on somebody; leverage, and the one thing here that is spent on a PERSON rather than on a
    /// door</i>. No new kind, because there was never a gap.</summary>
    public static Satchel.Item Found() => new(Satchel.Kind.Dirt, FindId);

    /// <summary>Is this the chip? Asked of a satchel row before it is given a card or a name.</summary>
    public static bool IsTheChip(Satchel.Item item) =>
        item.Kind == Satchel.Kind.Dirt && string.Equals(item.Id, FindId, StringComparison.Ordinal);

    /// <summary>The chip in a satchel, or null. The pocket read every ending asks first.</summary>
    public static Satchel.Item? InThePocket(IReadOnlyList<Satchel.Item>? carried)
    {
        foreach (Satchel.Item item in carried ?? [])
        {
            if (IsTheChip(item))
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>Out of the pocket — handed over, sold, or put in the ground. All three endings spend it,
    /// which is the point of there being three.</summary>
    public static IReadOnlyList<Satchel.Item> Spend(IReadOnlyList<Satchel.Item>? carried) =>
        Satchel.Remove(carried, Satchel.Kind.Dirt, FindId);

    // ── THE DEAL ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One roadster in four. Rare enough that the wallet is still what the job is about, common
    /// enough that a captain who runs the errand twice has seen both cars.</summary>
    public const int OneInEvery = 4;

    /// <summary>The purpose tag the deal's stream is salted with, so it never moves with the hand-off
    /// address rolled off the same booth (<c>mission-range</c>).</summary>
    public const string SeedTag = "chip-between-the-seats";

    /// <summary>The booth's seed for this deal — the wallet's own idiom, verbatim in shape: a slow sim-time
    /// rotation (thousand-second buckets) salted by a stable char-sum of the berth, so a card on a table
    /// does not flicker frame to frame and two docks deal different cars.</summary>
    public static ulong Seed(long simTimeBucket, int berthSalt) =>
        DiceRule.Seed(SeedTag, simTimeBucket, berthSalt);

    /// <summary>Does THIS roadster have the chip in it rather than the wallet? A d-<see cref="OneInEvery"/>
    /// on the booth's seed, the same one-in-N shape #535's key is placed with.</summary>
    public static bool BetweenTheSeats(ulong seed) => DiceRule.Roll(seed, OneInEvery).Face == 1;

    /// <summary>The deal from the two numbers a booth actually has.</summary>
    public static bool BetweenTheSeats(long simTimeBucket, int berthSalt) =>
        BetweenTheSeats(Seed(simTimeBucket, berthSalt));

    // ── ENDING ONE · THE COUNTER ──────────────────────────────────────────────────────────────────────

    /// <summary>Canon. What the client says when the chip comes back across the table — once, and it is the
    /// only thing he says. He does not name the two people in the photographs; a man who could afford to
    /// name them would not have sent a stranger for the car.</summary>
    public const string ClientLine = "You didn't look. Say you didn't look.";

    // ── ENDING TWO · THE DESK ─────────────────────────────────────────────────────────────────────────

    /// <summary>Canon. What the fence says. He names a buyer's APPETITE and never the buyer.</summary>
    public const string FenceLine =
        "I know a buyer for that. He'd rather it never existed, which is the best price there is.";

    /// <summary>Photographs with a timestamp are not a maybe. The dark web prices information by how sure
    /// it is (<see cref="IntelMarket.SellPrice"/>); this is the top of that scale, which is the honest
    /// reading of a thing that shows two faces and a clock.</summary>
    public const double CertainAsPhotographs = 1.0;

    /// <summary>What the desk pays, DERIVED from what the job pays: the contract's own price, plus the
    /// dark web's own premium for a certainty (<see cref="IntelMarket.SellValueFraction"/> of it, through
    /// the market's own function). Strictly more than the contract for any contract worth taking, and not a
    /// number anybody typed — reprice the market and this repricess with it.</summary>
    public static int FencePrice(int contractPay)
    {
        int pay = Math.Max(0, contractPay);
        return pay + IntelMarket.SellPrice(CertainAsPhotographs, pay);
    }

    // ── ENDING THREE · THE HOLE ───────────────────────────────────────────────────────────────────────

    /// <summary>The line on a buried chest's manifest. Listed by the chip's own canon name — the manifest
    /// takes a free string and this is the one entry in the game that is not a traded class — and flagged
    /// HOT (#202's flag), because a chest of photographs is evidence whatever the captain's heat is.</summary>
    public static CacheCargo Manifest() => new(Name, 1, Hot: true);

    // ── THE AUDIT ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every sentence this feature can put on a screen, for the audit that reads them all. Three,
    /// and two of them are somebody speaking.</summary>
    public static IEnumerable<string> EveryLine()
    {
        yield return LookCardLine;
        yield return ClientLine;
        yield return FenceLine;
    }
}
