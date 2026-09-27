using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · WHAT AN ITEM CAN BE OFFERED TO (#603) — the target a row is offered at, the sentry underfoot, and
/// the label each kind of thing wears in the satchel.
///
/// <para>Split out of <c>Map.Surface.Darkroom.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered. Its one <c>const</c> is folded at compile time.</para>
/// </summary>
public partial class Map
{
    /// <summary>#603 · What this item can be offered to right now.
    ///
    /// <para>Opened AT something — a door, a gate — everything goes to that. Opened from nowhere with the I
    /// key, most things are just a look, but a DOCUMENT can always be read as a clue, because the tracker is
    /// on the captain's arm and deciding a paper is a map is something they can do standing anywhere. That
    /// is the owner's own framing: the lead is not granted on pickup, it is granted when the player decides
    /// the paper means something.</para>
    ///
    /// <para>#688 · AT A DOOR, ONLY A KEY. Owner: <i>"Let's make a bigger story point about finding any kind
    /// of key or keycard and only suggest those at doors. Or tools, but not just like some papers."</i> The
    /// law is Core's (<see cref="SatchelTry.CanOffer"/>) and this is the one place that can route around it,
    /// so it asks. Nothing about the REFUSALS changed — a captain holding three authorities still gets every
    /// wrong-shaft and wrong-site reading #679 wrote. What stopped is the game dangling forty live offers at
    /// a bulkhead to hide the one that mattered.</para></summary>
    private (SatchelTry.Target Target, string? Context, string Label)? TargetFor(Core.Satchel.Item item)
    {
        if (_satchelTarget is { } at)
        {
            return SatchelTry.CanOffer(item.Kind, at.Target) ? at : null;
        }

        // A document can always be read — the tracker is on the captain's arm.
        if (item.Kind == Core.Satchel.Kind.Paper)
        {
            return (SatchelTry.Target.Tracker, null, "the motion tracker");
        }

        // #603 case 2 · And rounds go into a gun you are STANDING AT. Owner: "if we run empty on our
        // autoguns and have those on our inventory then from there we should be able to load them into the
        // guns." The interesting case is precisely a sentry that has run dry out in the field, away from the
        // tube — a handful is never a resupply, but it might be enough to get you home.
        if (item.Kind == Core.Satchel.Kind.Rounds && SentryUnderfoot() is { } unit)
        {
            return (SatchelTry.Target.Sentry, unit, unit);
        }

        return null;
    }

    /// <summary>
    /// #603 → #803 · The sentry within reach that would take rounds, if there is one.
    ///
    /// <para>It asked for a bot reading exactly 00, which was right while the only reason to hand a machine
    /// six rounds was that it had none. Owner, 2026-08-09: <i>"we might want to hand-load them into the bots
    /// for some special purposes, like shooting a mechanical lock."</i> A gun with eleven rounds in it and a
    /// hasp to take off wants six more, and a captain standing over it holding them was being told there was
    /// nothing to do.</para>
    ///
    /// <para>Still DEPLOYED only, and that is the division of labour rather than an oversight: the world's
    /// fixtures — the tube's belts, the shelter's press, the hut's locker — reach into the sling and fill
    /// what you carry. What you have SET DOWN is out there, and the only thing that walks rounds to it is
    /// you. The driest one wins when two are underfoot, because that is the one the captain came over
    /// for.</para></summary>
    private string? SentryUnderfoot()
    {
        if (_surface is not { } ex)
        {
            return null;
        }

        double radiusSq = DeckPlan.InteractRadius * DeckPlan.InteractRadius;
        string? best = null;
        int fewest = int.MaxValue;
        foreach (SurfaceBot b in ex.Bots)
        {
            if (!b.Deployed || b.Rounds >= SentryBot.MaxMagazine || b.Rounds >= fewest)
            {
                continue;
            }
            double dx = b.X - _avatarX, dy = b.Y - _avatarY;
            if ((dx * dx) + (dy * dy) <= radiusSq)
            {
                best = b.Unit;
                fewest = b.Rounds;
            }
        }
        return best;
    }

    /// <summary>What to write on one row of the pocket. The prose is rebuilt from the world here rather than
    /// stored, so a save can never go stale against the words.</summary>
    private static string SatchelLabel(Core.Satchel.Item item) => item.Kind switch
    {
        Core.Satchel.Kind.Authority =>
            UndergroundComplex.AuthorityCard.TryParse(item.Id, out UndergroundComplex.AuthorityCard c)
                ? UndergroundComplex.CardTitle(c)
                : "🎫 an authority card",
        // #613 · Each paper by its own name. Owner: "the operational papers could have individual short
        // titles… now they look identical in inventory." The certainty stays on the end, because that is the
        // one thing about a paper worth comparing across a pocketful of them.
        // #798 item 2 · …and a document that has come apart says WHICH PART OF ITSELF this row is, between
        // its own title and its own short word. It is a citation and not a second name (Core owns it, and
        // owns why): both halves of a split are still the document they came out of — same title, same
        // certainty, rolled off the same id — and what a captain holding two rows off one file needs is
        // which of them is the page and which is the rest. An empty string on anything still whole, so the
        // row of an unsplit paper is the row it has always been, character for character.
        Core.Satchel.Kind.Paper =>
            $"📋 {Core.FieldClue.Title(item.Id)}{Core.PageGranularity.RowCitation(item.Id)}"
                + $" — {Core.FieldClue.Label(Core.FieldClue.CertaintyOf(item.Id))}",
        Core.Satchel.Kind.Rounds => item.Id == Ammunition.LabTwoStage.Id
            ? $"🔫 {item.Count} × {Ammunition.LabTwoStage.Name}"
            : $"🔫 {item.Count} loose round{(item.Count == 1 ? "" : "s")}",

        // #614 · Named for what you actually have, which is paperwork about a thing you left in a room.
        // #677 · …and there are two of those now, told apart by the find's own id and named by the same
        // authored fragment the look-card is titled with — the odd book's idiom, and it means no row prose
        // was invented for a hall.
        Core.Satchel.Kind.Relic => UndergroundComplex.IsHallRecord(item.Id)
            ? UndergroundComplex.FoundRecordCardLabel
            : "⭕ measurements of the thing on the pallet",

        // #746 · The day-labour chit, printed as it is printed. Both ways of getting it are the same piece
        // of paper — the name in the book downstairs is not on the card, which is the whole horror of it.
        Core.Satchel.Kind.Chit => $"{CanteenTable.ChitGlyph} {CanteenTable.ChitTitle}",

        // #804 · The site's own pass, printed as it is printed. It names the SITE, which is what makes a
        // wallet of them worth carrying and what a guard on another rock reads out loud when he refuses it.
        // #605 · …AND THE TIER THAT IS ON IT. This read the SITE off the id and then composed the face at
        // BadgeTier, which was every pass there was — so a found DEPARTMENT pass would have printed GENERAL
        // HANDS in the one row a captain ever reads it in, while the man on the rota read the real thing out
        // of the same wallet. One call now: the face off the pass's own id, the same one the chooser row and
        // the drawer's own plate are composed from.
        Core.Satchel.Kind.Badge => PatrolBeat.BadgeFaceOf(item.Id) is { Length: > 0 } face
            ? $"{PatrolBeat.BadgeGlyph} {face}"
            : $"{PatrolBeat.BadgeGlyph} a site pass",

        // #763 · The kit, named as it is named. A tool this build does not know is still a tool and says so
        // rather than falling through to the default arm, which would print a receiver as a file on
        // somebody — the third named bug class, in a row of a list.
        // #537 · …and the cutting rig, which is the one tool whose COUNT is its state: the cell IS the item,
        // so the row prints what is left in it or a captain cannot tell a full rig from a last cut.
        Core.Satchel.Kind.Tool => Core.SdrScanner.IsTheKit(item) ? Core.SdrScanner.ItemName
            : Core.HullCutter.IsTheCutter(item) ? Core.HullCutter.RowLabel(item.Count)
            : "🧰 a piece of kit",

        // #535 · The key, by its own canon name and nothing else. NOT by the hull it came off — the id IS the
        // hull, and a row that printed it would stencil a ship's designation onto the one object in the game
        // whose entire value is that it leads back to nobody. It has an arm here for the kit's reason one
        // arm up: without one a code falls through to the default and reads as a file on somebody, which is
        // the third named bug class in a row of a list.
        Core.Satchel.Kind.BlackOpsKey => $"{Core.BlackOpsKey.Glyph} {Core.BlackOpsKey.Name}",

        // #711 · The parcel, by its own plate. An arm of its own for the key's reason one arm up: without
        // one a box falls through to the default and reads as a file on somebody — the third named bug
        // class, and a lie about the one object in this game whose whole worth is that it is exactly as
        // boring as it looks.
        Core.Satchel.Kind.Parcel => $"{Core.UnlistedParcel.Glyph} {Core.UnlistedParcel.Plate}",

        // #233 · The chip out of the roadster. It IS a file on somebody — that is precisely why it rides in
        // this kind — but it is a NAMED one, and the row says so, because a captain carrying two kinds of
        // leverage has to be able to tell which of them a client is asking for back.
        Core.Satchel.Kind.Dirt when Core.CompromisingChip.IsTheChip(item) => Core.CompromisingChip.RowLabel,

        // ── #798 item 2, second cut · …AND A FILE ON SOMEBODY SAYS WHICH PART OF ITSELF IT IS ───────────
        //
        // Owner: "a compromising FILE is not uniform… rip out the most compromising evidence and toss the
        // rest inconspicuously." A dossier is the object that sentence is ABOUT, and the first cut of the
        // split left it out for one reason only: a file on somebody has no title, so there was nothing for
        // the page citation to sit after. That reason does not survive being looked at. The citation is a
        // citation and not a name — what it needs in front of it is a HEADING, and this row has carried one
        // since the day the kind existed. "🗃 a file on somebody, page 2 of 3" and "🗃 a file on somebody,
        // 2 pages of 3" are two rows a captain can tell apart, which is the whole job.
        //
        // Nothing was invented to make that true: no subject name is rolled, no dossier prose is composed,
        // and no new field goes in the vault. The heading is the same string the default arm below prints
        // (they share the one constant, because two spellings of one row is how they come to disagree), and
        // Core owns the citation — the same RowCitation the paper arm asks for, empty on anything whole, so
        // an unsplit file is the row it has always been character for character.
        Core.Satchel.Kind.Dirt => AFileOnSomebody + Core.PageGranularity.RowCitation(item.Id),

        _ => AFileOnSomebody,
    };

    /// <summary>#798 item 2 · The heading a dossier row wears — the only thing a file on somebody has ever
    /// said about itself, and therefore the thing the page citation hangs on. One constant, because the
    /// Dirt arm and the catch-all above print the same words and a second spelling of them would drift.
    /// </summary>
    private const string AFileOnSomebody = "🗃 a file on somebody";
}
