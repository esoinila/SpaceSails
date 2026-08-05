using System;

namespace SpaceSails.Core;

/// <summary>
/// #614 · THE THINGS IN YOUR POCKET THAT ARE WORTH LOOKING AT.
///
/// <para>Owner: <i>"we could have gen-AI images of plotwise important items… maybe they say something about
/// what door they open."</i></para>
///
/// <para>The Hive has had reveal cards for two objects since #528 — the sealed way and the authority card —
/// and both of them are things you meet in a corridor. Everything a captain actually <i>carries</i> is a row
/// of text in a list. That is backwards: the items with plot in them are precisely the ones you take home and
/// look at again, and #603 already established that looking must be free and repeatable.</para>
///
/// <para><b>The rule the second half of the ask needs.</b> "Says what door it opens" is the tempting reading
/// and it is a quest marker: an item that names its lock does the captain's thinking, and this whole facility
/// is built on the opposite law. So a card describes the <b>lock</b> the way the paperwork that issued the
/// thing would describe it — <i>runs shaft 2 of a facility that is not this one</i> — and leaves working out
/// WHICH building that is to the player. It is the same discipline
/// <see cref="UndergroundComplex.SealedWayCard"/> already keeps: say what it is, never what to do about
/// it.</para>
///
/// <para>Pure, so the copy is pinned by tests and can be re-read forever. The client only renders it.</para>
/// </summary>
public static class CarriedObject
{
    /// <summary>What an item's card is made of. Null from <see cref="Card"/> means this thing is ordinary —
    /// most of a satchel is, and a game where every object earns a full-screen card has no objects that
    /// matter.</summary>
    public readonly record struct Reveal(string ArtUrl, string Label, string Story);

    /// <summary>Whether this item is worth a card at all. Cheap enough for the satchel to ask once per row
    /// while it is drawing the list.</summary>
    public static bool WorthLookingAt(Satchel.Item item, string hereBodyId) =>
        Card(item, hereBodyId) is not null;

    /// <summary>The card for a carried thing, or null if it is ordinary.</summary>
    /// <param name="item">The thing in the pocket.</param>
    /// <param name="hereBodyId">Where the captain is standing. Only used to tell a card for THIS building
    /// from a card for another one — which is the single most useful thing an authority can tell you, and
    /// the reason #613 made a foreign card a thing you can hold in the first place.</param>
    public static Reveal? Card(Satchel.Item item, string hereBodyId)
    {
        ArgumentNullException.ThrowIfNull(hereBodyId);

        return item.Kind switch
        {
            Satchel.Kind.Authority
                when UndergroundComplex.AuthorityCard.TryParse(item.Id, out UndergroundComplex.AuthorityCard c)
                => new Reveal(
                    UndergroundComplex.AuthorityCardArtUrl(c),
                    UndergroundComplex.AuthorityCardLabel,
                    UndergroundComplex.AuthorityCardStory(c) + "\n\n" + WhatItRuns(c, hereBodyId)),

            Satchel.Kind.Rounds when item.Id == Ammunition.LabTwoStage.Id
                => new Reveal(PenetratorArtUrl, PenetratorLabel, PenetratorStory),

            Satchel.Kind.Relic => RelicReveal(item.Id),

            _ => null,
        };
    }

    /// <summary>#677 · WHICH relic-class card, asked of the find's own id.
    ///
    /// <para>There are two of these now and they could not be less alike: a band of alloy on a pallet that
    /// somebody crated, invoiced and left the lights on over, and a section of wall that nobody made. This
    /// branch used to be one constant and would have shown a photograph of a pallet to a captain standing in
    /// a gallery with nothing in it, which is the third named bug class — the sim holding one thing while the
    /// picture reports another.</para>
    ///
    /// <para>Asked of the ID rather than of a body and a level, because the satchel keeps the id and nothing
    /// else: a row that re-derived a floor's band for itself would be a second answer to a question
    /// <see cref="UndergroundComplex.FindId"/> already settled the moment the thing went in the pocket. An
    /// empty <see cref="Reveal.ArtUrl"/> is the caption-only idiom (#528, the lifeboat muster, the odd book):
    /// a card that never claims a picture rather than one that wires an unpainted file and hides it on
    /// error.</para></summary>
    public static Reveal RelicReveal(string findId) =>
        UndergroundComplex.IsHallRecord(findId)
            ? new Reveal("", UndergroundComplex.FoundRecordCardLabel, UndergroundComplex.FoundRecordCard)
            : new Reveal(CollarArtUrl, CollarLabel, CollarStory);

    /// <summary>#614 · THE LOCK, NOT THE DOOR.
    ///
    /// <para>This is the whole of what the owner asked for and the whole of what has to be withheld. The card
    /// states, flatly, which shaft of which kind of building the authority runs — because that is printed on
    /// it and a captain holding it can read. What it never does is name the moon, put a mark on the tracker,
    /// or tell you to go there. A card that did any of those would convert an object into an objective.</para>
    ///
    /// <para>The good case is the foreign one. #613 made a Key found at the bottom of a facility issue the
    /// card for a DIFFERENT facility, so a captain can now be carrying a wallet of live authorities for
    /// shafts they have not found. Saying so out loud — <i>this is not for this building</i> — is the line
    /// that turns that wallet from an inventory oddity into the reason to keep flying.</para>
    ///
    /// <para>#679 · AND IT NAMES THE SITE. The first cut of this card said <i>"nothing on it says where that
    /// is"</i>, which was true of the card as it was then printed and became a LIE the moment #679 put the
    /// site designation on the face — the sentence and the object disagreeing, which is this repo's third
    /// named bug class and the one it has shipped most often. The owner's ruling is that a wallet has to be
    /// sortable; what is still withheld is everything a tracker could act on. A site code is not a plan.</para></summary>
    public static string WhatItRuns(UndergroundComplex.AuthorityCard card, string hereBodyId)
    {
        ArgumentNullException.ThrowIfNull(hereBodyId);

        if (string.Equals(card.BodyId, hereBodyId, StringComparison.Ordinal))
        {
            return $"It runs shaft {card.Band + 1}. Of this building — the one you are standing in, the one " +
                "whose lift will not admit that shaft exists. The card is older than the omission.";
        }

        return $"It runs shaft {card.Band + 1} of {BodyNames.Designation(card.BodyId)} SITE, which is not " +
            "this one.\n\n" +
            "That is the whole of what the office was prepared to print: a site code, in its own register, " +
            "on the assumption that anybody holding the card already knew which door it was for. No plan, no " +
            "line to walk, and not a word about where on that ground the head stands — but the site has a " +
            "name, the name is a place, and as far as that building's gates are concerned this card is " +
            "still good.";
    }

    // ── The penetrator ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Art slot: <c>art/the-penetrator.jpg</c>.</summary>
    public const string PenetratorArtUrl = "art/the-penetrator.jpg";

    /// <summary>Card title for the two-stage round.</summary>
    public const string PenetratorLabel = "🔫 SIGNED FOR AS CONSUMABLES";

    /// <summary>#614 · The round, described by what it was built to defeat — never by what it was issued
    /// against, because that is the answer the whole facility is arranged around not giving.
    ///
    /// <para>The minimum range is a FACT ABOUT THE ROUND and lives in <see cref="Ammunition"/>; it is said
    /// here in the object's own voice rather than restated as a number, so there is exactly one place in the
    /// codebase where that number is decided.</para></summary>
    public const string PenetratorStory =
        "Heavier than it should be for the length. The casing is turned to a tolerance nobody uses on " +
        "anything meant for vermin, and there is a seam a third of the way back where the second stage " +
        "sits.\n\n" +
        "The first stage opens the hole. The second one goes through it, and through whatever is standing " +
        "behind it, and keeps going. It is a round designed on the assumption that the things being shot at " +
        "would be in a QUEUE.\n\n" +
        "It has a minimum range, and the reason is written into the geometry: the second stage does not " +
        "separate until the round is clear of the muzzle by some distance, and inside that distance you are " +
        "firing a solid object at something close enough to share the consequences.\n\n" +
        "The crate it came out of is stencilled for a bench supply account. Somebody ordered a case of these " +
        "on a form that also covered gloves and lamp oil, and nobody upstairs asked a question about it, " +
        "because upstairs was not reading the forms — upstairs was reading the totals.";

    // ── The collar ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Art slot: <c>art/the-collar.jpg</c>.</summary>
    public const string CollarArtUrl = "art/the-collar.jpg";

    /// <summary>Card title for the annular thing.</summary>
    public const string CollarLabel = "⭕ MEASURED FOR SOMETHING";

    /// <summary>#614 · THE ONE OBJECT THAT IS ONLY A MEASUREMENT. Owner: <i>"kind of horror theme in a
    /// Lovecraft way … like finding a massive collar designed for Cthulhu's neck :D"</i>
    ///
    /// <para>Everything frightening about it is arithmetic. There is no creature in the art, no bones, no
    /// log, no note, and above all <b>no explanation</b> — canon (§13.8) holds hardest exactly here, because
    /// this is the most tempting object in the game to explain the Old Ones with. It does not.</para>
    ///
    /// <para>The horror is the WEAR. A thing this size might be machinery. Machinery does not have polish on
    /// the inside face in the pattern of something that moved against it for years.</para></summary>
    public const string CollarStory =
        "You cannot carry it. You have measured it, photographed it, and taken a scraping, and that is what " +
        "is in your pocket.\n\n" +
        "It is a band of dark alloy on an ordinary pallet, and the pallet is the only thing in the room that " +
        "tells you how big it is. Machined inside and out. Fixing points at even spacing all the way round, " +
        "cut for a load applied from within.\n\n" +
        "The inner face is polished. Not finished — POLISHED, in a broad band around the middle, the way a " +
        "handrail goes bright where hands go and nowhere else. Something was in this, and it was in it long " +
        "enough, and it moved.\n\n" +
        "There is no tag, no stencil, no part number, and no paperwork anywhere on this floor that mentions " +
        "it. Everything else down here was signed for twice. This was not signed for at all.";
}
