using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #1202 slice 1 · <b>CARRY THE PRESS.</b> A named journalist books passage at a haven bar, rides to a
/// landing site on a moon the captain already flies to, walks the ground behind him sharing his air
/// arithmetic, digs up (with him) a buried word from a source who would not meet her, comes home, pays,
/// and three days later the wire prints HER story — wrong in the way reporting is wrong, attributed to a
/// source who was the captain, never naming him.
///
/// <para>Owner, ruling GO on 2026-09-28: <i>"it is cool thought to later read the news about how it is
/// reported."</i> The report is the reward. Nothing in the world changes because the story ran — no heat, no
/// confirmation, only the wire and the book (§13.8: the press may speculate, the game never confirms).</para>
///
/// <h3>What lives here</h3>
///
/// <para>Every word (Fable canon, verbatim, enumerated by <see cref="AllProse"/>), the destination seeded
/// off the contract, the tin's spot seeded off the contract and the ground, the three clocks (the story
/// three sim-days after the turn-in, the floor's reaction the day after that), and the contract's own
/// state as one line of text — <see cref="Passage"/> — so it rides the quest record the vault already
/// writes and no new page field or vault section exists for it.</para>
/// </summary>
public static class CarryThePress
{
    // ── WHO ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The contract's giver, in the ledger's own shout-name idiom ("MADAM COIL").</summary>
    public const string Giver = "RAUHA LIND";

    /// <summary>Her overhead plate on the ground.</summary>
    public const string Plate = "Lind";

    /// <summary>The name the wire prints at the foot of her story, and the one the ✂ CLIP files it under.</summary>
    public const string Byline = "R. Lind";

    /// <summary>The card's title, and the ledger row's.</summary>
    public const string CardTitle = "CARRY THE PRESS";

    /// <summary>The satchel id of the buried word. The prose is rebuilt from it at read time.</summary>
    public const string NoteId = "press-note";

    /// <summary>The glyph her two book entries are filed under.</summary>
    public const string Glyph = "📰";

    /// <summary>Is this paper the tin?</summary>
    public static bool IsTheNote(string? paperId) =>
        string.Equals(paperId, NoteId, StringComparison.Ordinal);

    /// <summary>The satchel row the tin goes into the sleeve as.</summary>
    public static Satchel.Item TheNote() => new(Satchel.Kind.Paper, NoteId);

    // ── THE LINES (verbatim, #1202 · Fable, 2026-09-28) ─────────────────────────────────────────────────

    /// <summary>The offer card. <c>{site}</c> is the ground, in the book's own spelling.</summary>
    public const string OfferCard =
        "Passage to {site}, one bunk, one recorder. I file on the cycler window whether I am back or not, so it "
        + "is in your interest that I am back. Officially I am doing the tariff pool. Officially I am always "
        + "doing the tariff pool.";

    /// <summary>The terms line on the card. <c>{cr}</c> is the purse.</summary>
    public const string TermsLine = "{cr} on the return, and the story is not for sale.";

    /// <summary>Taken (pulse, once).</summary>
    public const string TakenLine =
        "She stows one bag and the recorder. 'Do not tell me anything you would mind reading.'";

    /// <summary>Landing at her ground (field book). <c>{spot}</c> is the game's own words for the tin's spot.</summary>
    public const string LandingLine =
        "Somebody left her a word here, she says, reading it off the recorder: where the ground is soft, {spot}. "
        + "She does not say who.";

    /// <summary>On the walk — told once, the first time the captain stops with her behind him.</summary>
    public const string WalkLine = "'Same tank as yours,' she says, 'and half your patience.'";

    /// <summary>The tin, as the sleeve reads it.</summary>
    public const string TinText =
        "A tin, wax-sealed, in a hand that did not want to be recognised: 'Not the count. The difference.'";

    /// <summary>Told at the dig.</summary>
    public const string DigLine = "She reads it twice and does not put it in the recorder.";

    /// <summary>Liftoff with her aboard (pulse).</summary>
    public const string LiftoffLine = "She sleeps the whole burn back. The recorder does not.";

    /// <summary>Turn-in at a haven (paid; pulse).</summary>
    public const string TurnInLine =
        "She pays for the bunk in credits and for the rest with a nod. 'Watch the wire. Three days. Do not "
        + "write to me.'";

    /// <summary>The story, with the tin. <c>{Body}</c> is her ground's body.</summary>
    public const string StoryWithTheTin =
        "{Body} is not losing people, says a hired boat's master who asked not to be named — it is losing the "
        + "difference between the people it counts and the people it has. Officials call the figure a clerical "
        + "matter. — R. Lind, for the wire";

    /// <summary>The story, without the tin.</summary>
    public const string StoryWithoutTheTin =
        "A stringer's trip to {Body} finds nothing wrong, says the hired boat's master who took her, and nothing "
        + "right either. Officials did not return calls. — R. Lind, for the wire";

    /// <summary>The PortRag's reaction, the next sim-day, any port's rag, once.</summary>
    public const string FloorLine =
        "The floor at the Ringside Exchange is 'unmoved' by the {Body} story. Nobody on the floor has read it. "
        + "Everybody on the floor has an opinion.";

    /// <summary>The field book, when the story prints (once).</summary>
    public const string StoryRanLine = "Her story ran. It is mostly true and it is not what happened.";

    /// <summary>
    /// The one title the sleeve needs for the tin, and it is not a line of its own: it is the opening words
    /// of <see cref="TinText"/>, cut at its first comma pair, so the row in the sleeve and the page it opens
    /// are one piece of writing. FLAGGED in the PR for the canon pass — a title the tin was never given.
    /// </summary>
    public static string TinTitle => TinText[..TinText.IndexOf(", in a hand", StringComparison.Ordinal)];

    /// <summary>Every sentence this file can put on a screen, for the canon sweeps.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return OfferCard;
        yield return TermsLine;
        yield return TakenLine;
        yield return LandingLine;
        yield return WalkLine;
        yield return TinText;
        yield return DigLine;
        yield return LiftoffLine;
        yield return TurnInLine;
        yield return StoryWithTheTin;
        yield return StoryWithoutTheTin;
        yield return FloorLine;
        yield return StoryRanLine;
    }

    // ── THE LINES, FILLED ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The offer card for a ground, in the book's own spelling (<see cref="FieldNotes.PlaceLabel"/>).</summary>
    public static string Offer(string site) => OfferCard.Replace("{site}", site, StringComparison.Ordinal);

    /// <summary>The terms line for a purse.</summary>
    public static string Terms(int credits) =>
        TermsLine.Replace("{cr}", credits.ToString("N0", CultureInfo.InvariantCulture) + " cr", StringComparison.Ordinal);

    /// <summary>Her line at the ground, naming the tin's spot in the game's own words.</summary>
    public static string Landing(string spot) => LandingLine.Replace("{spot}", spot, StringComparison.Ordinal);

    /// <summary>The story the wire prints. EXACTLY ONE of the two, chosen by the tin.</summary>
    public static string Story(string bodyName, bool withTheTin) =>
        (withTheTin ? StoryWithTheTin : StoryWithoutTheTin).Replace("{Body}", bodyName, StringComparison.Ordinal);

    /// <summary>The floor's reaction, a day later.</summary>
    public static string Floor(string bodyName) => FloorLine.Replace("{Body}", bodyName, StringComparison.Ordinal);

    // ── WHEN SHE IS AT THE TABLE ────────────────────────────────────────────────────────────────────────

    /// <summary>One watch in this many, per berth, she is the stranger at the table.</summary>
    public const int OneWatchIn = 3;

    /// <summary>Is she drinking here this watch? Seeded off the berth and the watch, so the offer is present
    /// or absent and never flickers.</summary>
    public static bool AtTheTable(string? berthId, long watch) =>
        berthId is { Length: > 0 }
        && DiceRule.Roll(DiceRule.Seed($"press:table:{berthId}", watch), OneWatchIn).Face == 1;

    // ── WHERE SHE IS GOING ──────────────────────────────────────────────────────────────────────────────

    /// <summary>The seed tag the destination is drawn on.</summary>
    public const string DestinationTag = "press:destination";

    /// <summary>
    /// Her ground, seeded off the contract and nothing else — the <see cref="ParcelDrop.For(string, IReadOnlyList{string})"/>
    /// shape. The pool is handed in (the scenario's own landable bodies, from the file) and sorted ordinally
    /// before the roll so the answer does not depend on the order a scenario lists its bodies in. Null only
    /// when the pool is empty.
    /// </summary>
    public static ParcelDrop.Destination? For(string? questId, IReadOnlyList<string>? landableBodyIds)
    {
        if (questId is null || landableBodyIds is not { Count: > 0 })
        {
            return null;
        }

        var pool = new List<string>(landableBodyIds);
        pool.Sort(StringComparer.Ordinal);

        string bodyId = pool[DiceRule.Roll(DiceRule.Seed($"{DestinationTag}:{questId}"), pool.Count).Face - 1];
        int siteIndex =
            DiceRule.Roll(DiceRule.Seed($"{DestinationTag}:{questId}:site"), LandingSites.Count(bodyId)).Face - 1;
        return new ParcelDrop.Destination(bodyId, siteIndex, LandingSites.At(bodyId, siteIndex).Name);
    }

    // ── THE TIN ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The tin, minted the way every rumour map's chest is minted (<see cref="CacheMint.Bury"/>), so its
    /// spot is named in the game's own words — <see cref="TreasureCache.BearingLine"/>. It is never put in
    /// the captain's hoard: nothing announces it beyond her one instruction, and a row in the ledger's 🗺
    /// section would be a second announcement.
    /// </summary>
    public static TreasureCache TheTin(string questId, string bodyId, int siteIndex) =>
        CacheMint.Bury($"press-tin:{questId}", bodyId, 0, 0, [], 0, questId, playerOwned: false,
            siteIndex: siteIndex, buried: true, deposit: [TheNote()]);

    /// <summary>How far the tin lies from the tube column, at most, either side (deck units).</summary>
    public const double TinSpreadDu = 24.0;

    /// <summary>How far below the landing band the tin lies, nearest and furthest (deck units). Near the pad
    /// on purpose: the search is the tank's arithmetic, not the width of the moon.</summary>
    public const double TinNearDu = 8.0;
    public const double TinFarDu = 26.0;

    /// <summary>How many probe squares off the tin's own square a shovel still finds it (Chebyshev).</summary>
    public const int TinReachSquares = 2;

    /// <summary>
    /// The squares the tin may lie under, in the order they are tried: a seeded first square, then rings
    /// outward, only where the ground is soft (<see cref="BeachComber.Roll"/> is not bedrock) and inside the
    /// diggable field. The caller takes the first one its own walls allow, so the answer is one square per
    /// ground per contract, on every machine and after a reload.
    /// </summary>
    public static IEnumerable<(int X, int Y)> TinSquares(string questId, string bodyId, SurfaceLayout.Field field)
    {
        int spread = (int)(TinSpreadDu * 2) + 1;
        int depth = (int)(TinFarDu - TinNearDu) + 1;
        double dx = DiceRule.Roll(DiceRule.Seed($"press:tin:x:{questId}:{bodyId}"), spread).Face - 1 - TinSpreadDu;
        double dy = TinNearDu + DiceRule.Roll(DiceRule.Seed($"press:tin:y:{questId}:{bodyId}"), depth).Face - 1;
        (int cx, int cy) = BeachComber.SquareOf(field.HomeX + dx, field.LandingBandY - dy);

        const int Rings = 8;
        for (int r = 0; r <= Rings; r++)
        {
            for (int ix = -r; ix <= r; ix++)
            {
                for (int iy = -r; iy <= r; iy++)
                {
                    if (Math.Max(Math.Abs(ix), Math.Abs(iy)) != r)
                    {
                        continue;
                    }

                    int sx = cx + ix, sy = cy + iy;
                    (double x, double y) = BeachComber.SquareCenter(sx, sy);
                    if (y >= field.LandingBandY - 1 || y <= field.BottomY + 1
                        || x <= field.LeftX + SurfaceLayout.EdgeMargin || x >= field.RightX - SurfaceLayout.EdgeMargin
                        || BeachComber.Roll(bodyId, sx, sy).IsTooHard)
                    {
                        continue;
                    }

                    yield return (sx, sy);
                }
            }
        }
    }

    /// <summary>Does a probe of this square find the tin under that one?</summary>
    public static bool FindsTheTin((int X, int Y) tin, int probeX, int probeY) =>
        Math.Max(Math.Abs(tin.X - probeX), Math.Abs(tin.Y - probeY)) <= TinReachSquares;

    // ── THE CLOCKS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>"Watch the wire. Three days." The story prints this long after the turn-in.</summary>
    public const double StoryAfterSeconds = 3 * 86400.0;

    /// <summary>…and the floor has an opinion the next sim-day.</summary>
    public const double FloorAfterStorySeconds = 86400.0;

    /// <summary>When her story prints, or null before she has paid.</summary>
    public static double? StoryAt(in Passage p) => p.TurnedIn is { } t ? t + StoryAfterSeconds : null;

    /// <summary>When the floor reacts, or null before she has paid.</summary>
    public static double? FloorAt(in Passage p) => StoryAt(p) is { } s ? s + FloorAfterStorySeconds : null;

    /// <summary>Is the story on the wire by now? Never before three sim-days after the turn-in.</summary>
    public static bool StoryIsDue(in Passage p, double now) => StoryAt(p) is { } s && now >= s;

    /// <summary>Is the floor's reaction on the rags by now?</summary>
    public static bool FloorIsDue(in Passage p, double now) => FloorAt(p) is { } f && now >= f;

    // ── THE CONTRACT'S OWN STATE ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Everything a contract has to remember between the table and the wire, as one line of text so it rides
    /// the quest record's existing free slot (<c>QuestRecord.Fields["pin"]</c>) — no page field, no vault
    /// section, and a reader that does not know a key carries it.
    /// </summary>
    /// <param name="Site">Which of the body's grounds is hers.</param>
    /// <param name="Landed">Her line at the ground has been said and filed.</param>
    /// <param name="Walked">Her line on the walk has been said.</param>
    /// <param name="Tin">The tin has been dug.</param>
    /// <param name="TurnedIn">The sim-time she paid, or null.</param>
    /// <param name="Printed">The story's book entry has been filed.</param>
    /// <param name="Floored">The floor's reaction has been pushed once.</param>
    public readonly record struct Passage(
        int Site, bool Landed = false, bool Walked = false, bool Tin = false,
        double? TurnedIn = null, bool Printed = false, bool Floored = false)
    {
        /// <summary>Written as one line.</summary>
        public string Write() =>
            string.Create(CultureInfo.InvariantCulture,
                $"site={Site};landed={B(Landed)};walked={B(Walked)};tin={B(Tin)};in={(TurnedIn is { } t ? t.ToString("R", CultureInfo.InvariantCulture) : "")};printed={B(Printed)};floor={B(Floored)}");

        private static string B(bool b) => b ? "1" : "0";

        /// <summary>Read one back. Anything missing reads as not-yet, which is the tolerant default.</summary>
        public static Passage Read(string? line)
        {
            var p = new Passage(0);
            foreach (string pair in (line ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = pair.IndexOf('=');
                if (eq < 0)
                {
                    continue;
                }

                string key = pair[..eq], value = pair[(eq + 1)..];
                p = key switch
                {
                    "site" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) => p with { Site = s },
                    "landed" => p with { Landed = value == "1" },
                    "walked" => p with { Walked = value == "1" },
                    "tin" => p with { Tin = value == "1" },
                    "in" when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double t) => p with { TurnedIn = t },
                    "printed" => p with { Printed = value == "1" },
                    "floor" => p with { Floored = value == "1" },
                    _ => p,
                };
            }

            return p;
        }
    }

    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>What <c>press=</c> asks for.</summary>
    public enum Cheat
    {
        /// <summary>Nothing asked.</summary>
        None,
        /// <summary><c>&amp;press=1</c> — the contract taken, she is aboard, her ground named.</summary>
        Aboard,
        /// <summary><c>&amp;press=filed</c> — she has paid long enough ago that her story is on the wire.</summary>
        Filed,
    }

    /// <summary>Read <c>press=</c> off an address, the <see cref="GeocacheSale.CheatIn"/> way: a dev latch has
    /// no business in a page field the frame ledger walks.</summary>
    public static Cheat CheatIn(string? uri)
    {
        int q = uri?.IndexOf('?', StringComparison.Ordinal) ?? -1;
        if (uri is null || q < 0)
        {
            return Cheat.None;
        }

        foreach (string pair in uri[(q + 1)..].Split('&', '#'))
        {
            if (pair.StartsWith("press=", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair["press=".Length..]).ToLowerInvariant() switch
                {
                    "1" or "true" or "yes" => Cheat.Aboard,
                    "filed" => Cheat.Filed,
                    _ => Cheat.None,
                };
            }
        }

        return Cheat.None;
    }
}
