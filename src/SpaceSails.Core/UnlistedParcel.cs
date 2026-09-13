using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #711 slice 1 · <b>THE PARCEL THE SEARCH IS MEANT TO STOP AT.</b>
///
/// <para>Canon design (head coder, 2026-08-09), the clause this file is the whole of:</para>
///
/// <para><i>"A cover is a small stack the player assembles: the worn identity, one sacrificial truth, and
/// what it protects. Getting caught at the sacrifice COSTS something real (the fine, heat, a confiscation —
/// it must hurt, or discovery feels fake) and PAYS something real (that entity's suspicion is spent; the
/// challenge/inspection outcome for them is settled until new cause)."</i></para>
///
/// <h3>What the object is</h3>
///
/// <para>A real, petty, deliberately discoverable box: somebody's parcel, in your hold, on nobody's
/// manifest. It is <b>cargo and not evidence</b> — a <see cref="Satchel.Kind.Parcel"/>, riding the bulky
/// pocket, which <see cref="RipAndBin.IsEvidence"/> has never heard of and the document sleeve never sees.
/// That is not a detail: the whole of what makes this thing work as a sacrifice is that it is an ORDINARY
/// crime. A hauler skimming a little is the expected secret of a hauler. Nothing about a parcel suggests
/// there is a second thing to look for, which is precisely what a sheet of somebody else's paperwork in the
/// sleeve would suggest.</para>
///
/// <h3>What happens when it is found</h3>
///
/// <para>Three things and a fourth, and the fourth is the feature:</para>
///
/// <list type="number">
/// <item><b>A fine</b> — <see cref="BustedRule.BribeDemand"/>, CALLED and never restated, off the meter this
/// very outfit keeps. The inspectorate reads the same meter the collector reads.</item>
/// <item><b>The parcel is confiscated.</b> Both arms, always. A man who found a box does not hand it
/// back.</item>
/// <item><b>A little heat</b>, through <see cref="IllegalHeat.Bank"/> and nothing else — the cheapest
/// crossing on that ladder (<see cref="IllegalHeat.Crossing.AnUnlistedParcelAboard"/>).</item>
/// <item><b>The folder closes.</b> The outfit that caught you writes it down, and from then until new cause
/// their rounds have an answer for you and stop asking the question.</item>
/// </list>
///
/// <h3>New cause is a BAND, not a clock</h3>
///
/// <para>The folder holds until <see cref="IllegalHeat.StartingRung"/> — the one function in this game that
/// says a band of the meter has moved — answers higher than it did when the fine was paid. No timer is
/// invented here and none is wanted: the thing that re-opens a closed file is the captain doing something
/// else, in front of these same people, big enough to move their meter a whole band. Hours cannot do it;
/// <see cref="IllegalHeat.Cool"/> only ever walks the rung DOWN.</para>
///
/// <h3>The tell, drawn and never stated</h3>
///
/// <para>Canon, same pass: <i>"An inspector who finds the parcel and does not fine you — who just notes it
/// and keeps looking — is the scariest sentence this system can produce, and it is drawn, not stated."</i>
/// So exactly ONE outfit in a world does that (<see cref="TheOneWhoKeepsLooking"/>), chosen off the world
/// seed, and never the first one to catch the captain (<see cref="AFolderHasBeenClosedBefore"/>): the first
/// find must teach the rule, or the exception has nothing to be an exception to. On that arm there is no
/// fine and <b>no folder closes</b> — no fine, no receipt, no answer filed — and the read goes on into
/// whatever it would have read. Nothing anywhere explains it (§13.8).</para>
///
/// <h3>The law this file is written under</h3>
///
/// <para>Canon: <i>"No card names the pattern. The player who builds one discovers the mechanic by the fine
/// that closes a folder; the player who never does just thinks inspections are survivable luck."</i> Every
/// sentence here is Fable-authored and verbatim; there are three of them, they are enumerated by
/// <see cref="AllProse"/>, and no word this file can put on a screen names the shape it is part of.</para>
/// </summary>
public static class UnlistedParcel
{
    // ── THE THING ITSELF ────────────────────────────────────────────────────────────────────────────────

    /// <summary>What the row and the plate read. Canon, verbatim, and it is the whole of the object's face:
    /// two words that say what it is and refuse to say whose.</summary>
    public const string Plate = "UNLISTED PARCEL";

    /// <summary>The glyph the satchel row wears. A box, because it is a box.</summary>
    public const string Glyph = "📦";

    /// <summary>#711 · <b>THE LOOK CARD.</b> Canon, verbatim. #614's law kept to the letter: it says what
    /// the OBJECT is and not one word about what it is for.
    ///
    /// <para>The second sentence is the entire brief for the mechanic and it reads as a shrug. A thing a
    /// hauler carries and does not list is the most ordinary contraband in this world, which is exactly why
    /// it is worth carrying.</para></summary>
    public const string LookCardLine =
        "Somebody's small parcel with nobody's name on it. The kind of thing a hauler carries and does not " +
        "list.";

    /// <summary>#711 · <b>THE FINE.</b> Canon, verbatim — the body of the story pop-up card (#684's idiom)
    /// on the arm where the fine book comes out.
    ///
    /// <para>Read what is in it. He is not angry, he is not suspicious, and he is not finished with his
    /// afternoon — he is finished with YOU. <i>Filed</i> is the word the whole design turns on, and the card
    /// never once explains why that is good news.</para></summary>
    public const string FineLine =
        "An unlisted parcel. Fined, filed, and the inspector is already looking at the next hull.";

    /// <summary>#711 · <b>THE TELL.</b> Canon, verbatim, the same card shape, and the only difference is
    /// what his hand does not do.
    ///
    /// <para>Nothing explains this line — not the card it is on, not the field book, not a plate anywhere in
    /// the game. It is the whole of the telling.</para></summary>
    public const string TellLine =
        "An unlisted parcel. The inspector notes it, does not reach for the fine book, and keeps looking.";

    /// <summary>The verb on the desk row that hands one over. Two words, in the register the desk's other
    /// three rows are written in.</summary>
    public const string DeskVerb = "📦 Take it";

    /// <summary>How the id of a parcel is spelled, so nothing that parses satchel ids can mistake one for a
    /// room key, a badge or a wreck (<c>KeepOrLeave.TryReadKey</c> walks straight past this, the way it
    /// walks past <c>port-key:</c>).</summary>
    public const string IdPrefix = "parcel:";

    /// <summary>The parcel as a thing in the pocket. Its id is the haven and the watch it was taken on, so
    /// two windows are two parcels and one window is one parcel however many times the row is looked at —
    /// the discipline <see cref="BlackOpsKey.FromTheFence"/> already keeps.</summary>
    public static Satchel.Item FromTheDesk(string havenId, long watch)
    {
        ArgumentNullException.ThrowIfNull(havenId);
        return new Satchel.Item(Satchel.Kind.Parcel, $"{IdPrefix}{havenId}@{watch}");
    }

    /// <summary>Is this row a parcel? Asked of the OBJECT rather than of an id, so every seam that meets one
    /// — the look card, the desk row, the read — recognises it the same way.</summary>
    public static bool IsAParcel(Satchel.Item item) => item.Kind == Satchel.Kind.Parcel;

    /// <summary>
    /// <b>ONE PER HULL AT A TIME.</b> Possession IS the state — no flag and no parallel ledger, the
    /// discipline <see cref="PatrolBeat.BadgeHeld"/> already keeps.
    ///
    /// <para>Asked of the KIND and never of an id, which is what makes the law hold across havens: a captain
    /// who took one at Luna and flew to Phobos is still a captain carrying a parcel, and the second desk has
    /// nothing to offer him.</para>
    /// </summary>
    public static bool Held(IReadOnlyList<Satchel.Item>? carried) =>
        Satchel.CountOf(carried, Satchel.Kind.Parcel) > 0;

    /// <summary>What the look card is titled — the glyph and the plate, composed rather than authored, so
    /// the card and the satchel row cannot come to two names for one object.</summary>
    public static string CardLabel => $"{Glyph} {Plate}";

    /// <summary>The parcel taken off you. Returns the wallet without it, whichever haven it came from — the
    /// find never has to know which id it is confiscating, and a captain carrying one parcel is carrying
    /// none afterwards.</summary>
    public static IReadOnlyList<Satchel.Item> Confiscated(IReadOnlyList<Satchel.Item>? carried)
    {
        var left = new List<Satchel.Item>(carried?.Count ?? 0);
        foreach (Satchel.Item item in carried ?? [])
        {
            if (!IsAParcel(item))
            {
                left.Add(item);
            }
        }
        return left;
    }

    // ── WHAT IT COSTS WHEN IT IS FOUND ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// #711 · <b>THE FINE — <see cref="BustedRule.BribeDemand"/>, CALLED, NEVER RESTATED.</b>
    ///
    /// <para>Canon says the discovery <i>must hurt, or discovery feels fake</i>, and this game has exactly
    /// one statement about what being caught by the law costs a captain in coin at their current standing.
    /// That is the bribe's own band table, and it is read here whole — heat in, amount out — rather than
    /// scaled by a fraction somebody typed. A fraction would be a second opinion about a question the bribe
    /// has already settled, which is the mistake #535 slice 2's own red-proof caught and deleted.</para>
    ///
    /// <para><b>The meter it reads is THIS OUTFIT'S</b> (<see cref="IllegalHeat.HeatAtSite"/>), not the
    /// ship's. That is the fiction rather than a quirk: the man with the form is one of these people, he is
    /// reading what these people have on file, and the same captain is quoted differently at two havens for
    /// the reason the whole meter exists. It moves with them, which is what makes a fine paid at a hot site
    /// an expensive way to close a folder and a fine paid at a cold one the bargain the design wants.</para>
    /// </summary>
    public static int TheFine(int heatAtThisOutfit, ulong seed) =>
        BustedRule.BribeDemand(heatAtThisOutfit, seed).Total;

    // ── THE ONE WHO DOES NOT REACH FOR THE FINE BOOK ────────────────────────────────────────────────────

    /// <summary>#711 · The seed tag the exception is drawn on. Its own stream, so the outfit that keeps
    /// looking never moves with the fine a captain happens to be paying at the time.</summary>
    public const string ExceptionTag = "parcel:keeps-looking";

    /// <summary>
    /// #711 · <b>EXACTLY ONE OUTFIT IN A WORLD DOES NOT REACH FOR THE FINE BOOK.</b>
    ///
    /// <para>Drawn off the WORLD SEED and off nothing else — not the site, not the watch, not the captain's
    /// heat — so it is a fact about the universe the captain is in rather than a coin flipped at the moment
    /// a box is opened. Two captains in one world meet the same one; one captain in two worlds does not.
    /// That is the difference between a tell and a random event, and it is the only reason a player can ever
    /// learn anything from it.</para>
    ///
    /// <para>The pool is <see cref="SiteOperator.All"/> — every outfit this register lists, the parent
    /// undertaking included. Null only for a world with no outfits in it at all, which no shipped ephemeris
    /// produces and a guard is entitled to hand in anyway.</para>
    /// </summary>
    public static string? TheOneWhoKeepsLooking(ulong worldSeed) =>
        TheOneWhoKeepsLooking(worldSeed, SiteOperator.All);

    /// <inheritdoc cref="TheOneWhoKeepsLooking(ulong)"/>
    public static string? TheOneWhoKeepsLooking(ulong worldSeed, IReadOnlyList<SiteOperator.Operator>? world)
    {
        if (world is not { Count: > 0 })
        {
            return null;
        }

        return world[DiceRule.Roll(DiceRule.Seed(worldSeed, ExceptionTag), world.Count).Face - 1].Id;
    }

    /// <summary>
    /// #711 · <b>HAS ANYBODY EVER CLOSED A FOLDER ON THIS CAPTAIN?</b> The gate that keeps the exception off
    /// the FIRST inspection a captain meets.
    ///
    /// <para>Canon requires it: the mechanic is <i>discovered by the fine that closes a folder</i>, so the
    /// first find has to be a fine or there is no rule for the exception to break. A captain whose very
    /// first parcel was noted and walked past would have learned nothing except that inspections are
    /// arbitrary — which is the reading the whole feature is written against.</para>
    ///
    /// <para>Asked of the BOOK rather than of a counter, because the book is where it already is: any
    /// outfit's closed folder is a fine the captain has paid and a rule the captain has met.</para>
    /// </summary>
    public static bool AFolderHasBeenClosedBefore(ContactLedger book)
    {
        ArgumentNullException.ThrowIfNull(book);
        foreach (ContactHistory h in book.Entries.Values)
        {
            if (h.FolderClosed && IllegalHeat.IsAnOutfitsBook(h.ContactId))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>#711 · Does THIS outfit, catching this captain NOW, reach for the fine book? False for the
    /// one the world seed drew, and only once the captain has already been fined by somebody.</summary>
    public static bool HeReachesForTheFineBook(string operatorId, ContactLedger book, ulong worldSeed)
    {
        ArgumentNullException.ThrowIfNull(operatorId);
        ArgumentNullException.ThrowIfNull(book);

        return !(AFolderHasBeenClosedBefore(book)
                 && string.Equals(operatorId, TheOneWhoKeepsLooking(worldSeed), StringComparison.Ordinal));
    }

    // ── THE ANSWER THAT IS ON FILE ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #711 · <b>IS THIS OUTFIT'S ANSWER ALREADY FILED?</b> The one predicate, read by everything that would
    /// otherwise start a read.
    ///
    /// <para>True while their folder is closed AND their meter has not risen a whole band since the closing
    /// — <see cref="IllegalHeat.StartingRung"/>, the function this file borrows rather than a clock it
    /// invents. Hours can only ever make this MORE true (<see cref="IllegalHeat.Cool"/> walks the rung
    /// down), which is right: an answer does not go stale on its own. Only the captain can stale it.</para>
    ///
    /// <para>Asked of a BODY, like every other question in this game about what the people who own this
    /// ground think, so no caller ever re-derives the operator of a moon.</para>
    /// </summary>
    public static bool TheFolderIsClosed(ContactLedger book, string bodyId)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(bodyId);
        return TheFolderIsClosedFor(book, SiteOperator.Of(bodyId).Id);
    }

    /// <inheritdoc cref="TheFolderIsClosed(ContactLedger, string)"/>
    public static bool TheFolderIsClosedFor(ContactLedger book, string operatorId)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(operatorId);

        ContactHistory filed = book.For(IllegalHeat.LedgerId(operatorId));
        return filed.FolderClosed
               && IllegalHeat.StartingRung(filed.HeatOwed) <= filed.FolderClosedAtRung;
    }

    // ── THE FIND ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// What a read that met the parcel did.
    /// </summary>
    /// <param name="Fined">He reached for the fine book. False is the exception, and the only thing on this
    /// record that changes with it — the parcel goes either way.</param>
    /// <param name="Fine">What it cost, in credits. Zero on the exception, because there was no fine — not a
    /// small fine, none.</param>
    /// <param name="Line">The card's body: one of the two canon sentences, chosen here so no caller has to
    /// know which arm it is on.</param>
    /// <param name="TheReadGoesOn">Whether the inspection continues into whatever it would have read. True
    /// on the exception and false on the fine, which is the mechanical shape of <i>the folder closed</i>
    /// against <i>he keeps looking</i>.</param>
    public readonly record struct Found(bool Fined, int Fine, string Line, bool TheReadGoesOn);

    /// <summary>
    /// #711 · <b>THE PARCEL IS WHAT IS FOUND. ONE CALL, because the ORDER inside it is load-bearing.</b>
    ///
    /// <para>The fine is quoted off the meter as it stands BEFORE the crossing, because that is the file the
    /// man is reading when he writes the number down. The heat is banked next. The folder is closed LAST, at
    /// the band the meter stands at AFTERWARDS — so the point this very crossing added can never read as the
    /// new cause that re-opens what it just closed. Three statements in one method for the reason
    /// <see cref="ContactLedger.RecordFavourGiven"/> is one method: a caller that did them separately could
    /// be interrupted between them, and every surviving half is a lie.</para>
    ///
    /// <para><b>On the exception no folder is closed at all</b>, and that is not an oversight to be tidied
    /// up later. The fine is the receipt for the cover working; there was no fine, so there is no receipt,
    /// so there is nothing on file, so the next round asks again. The captain is told none of this.</para>
    ///
    /// <para>The parcel itself is not removed here — the wallet belongs to the caller, and
    /// <see cref="Confiscated"/> is what takes it. Core says what happened; the client's hand goes in the
    /// pocket.</para>
    /// </summary>
    /// <param name="book">The contacts ledger — #715's per-entity book, which is where this record lives.</param>
    /// <param name="bodyId">Whose ground the hull is standing on.</param>
    /// <param name="seed">The roll's seed, for the fine's own amount.</param>
    /// <param name="simTime">Now, for the crossing's stamp.</param>
    /// <param name="worldSeed">The world, for the one outfit that keeps looking.</param>
    public static Found TheParcelIsWhatIsFound(
        ContactLedger book, string bodyId, ulong seed, double simTime, ulong worldSeed)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(bodyId);

        string operatorId = SiteOperator.Of(bodyId).Id;
        bool fined = HeReachesForTheFineBook(operatorId, book, worldSeed);
        int fine = fined ? TheFine(IllegalHeat.HeatAt(book, operatorId), seed) : 0;

        IllegalHeat.Bank(
            book, IllegalHeat.Charge(bodyId, IllegalHeat.Crossing.AnUnlistedParcelAboard), simTime);

        if (fined)
        {
            book.CloseTheFolder(
                IllegalHeat.LedgerId(operatorId),
                SiteOperator.ById(operatorId) is { } outfit ? outfit.Name : operatorId,
                IllegalHeat.StartingRung(IllegalHeat.HeatAt(book, operatorId)));
        }

        return new Found(fined, fine, fined ? FineLine : TellLine, TheReadGoesOn: !fined);
    }

    // ── HOW IT IS TOLD ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #711 · <b>THE FIND, AS THE CARD THE ROUND ALREADY RAISES.</b> The same
    /// <see cref="PatrolBeat.Read"/> record, the same label, the same painting — because it is the same
    /// thirty seconds with the same man in it, and a second card shape for it would be the stacked-card
    /// mistake #777 named wearing a new coat.
    ///
    /// <para>On the fine, <see cref="PatrolBeat.Read.Satisfied"/> is TRUE and there is no consequence: he is
    /// already looking at the next hull, so nothing is escorted, nothing is refused, and the round picks up
    /// where it left off. That is the payoff stated in the only vocabulary the client has for it.</para>
    /// </summary>
    public static PatrolBeat.Read TheFineIsTold(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        return new PatrolBeat.Read(
            true, FineLine, PatrolBeat.ChallengeLabel, PatrolBeat.ChallengeCard(plate));
    }

    /// <summary>
    /// #711 · <b>…AND THE EXCEPTION, WHICH IS THE SAME CARD WITH THE READ STILL ON IT.</b> The tell goes
    /// FIRST — it is what happened first — and then the read the captain was always going to get, verbatim,
    /// on the same card, with whatever that read costs still riding on it.
    ///
    /// <para>Composed rather than re-authored, so the wallet ladder keeps its single source: every outcome
    /// #804 and #1149 can produce reads exactly as it reads on any other afternoon, with one sentence in
    /// front of it that explains nothing.</para>
    /// </summary>
    public static PatrolBeat.Read TheReadGoesOnAfterIt(PatrolBeat.Read wallet) =>
        wallet with { Line = TellLine + "\n\n" + wallet.Line };

    /// <summary>#711 · What the field book keeps of a fine. A fact, never a mechanic — the
    /// <see cref="PatrolBeat.EscortNote"/> idiom — and it does not say the word <i>settled</i> or anything
    /// like it, because the book is the captain's own hand and the captain has not been told.</summary>
    public const string FineNote =
        "A parcel found aboard that was on no manifest. Paid the fine on the spot and watched it carried off.";

    /// <summary>#711 · …and what it keeps of the other one. The same register, and the difference is the
    /// whole of what the book is allowed to notice.</summary>
    public const string TellNote =
        "A parcel found aboard that was on no manifest. Carried off, and no fine written.";

    /// <summary>Every sentence this feature can put on a screen, for the audit that reads them all. Five:
    /// the three canon lines, the desk's verb, and the two the field book keeps.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Plate;
        yield return LookCardLine;
        yield return FineLine;
        yield return TellLine;
        yield return DeskVerb;
        yield return FineNote;
        yield return TellNote;
    }
}
