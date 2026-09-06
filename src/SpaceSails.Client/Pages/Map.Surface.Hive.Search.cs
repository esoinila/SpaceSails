using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #585 · TURNING OVER ONE ROOM, AND READING WHAT IS WRITTEN ON IT — the search verb (a drawer with
/// somebody else's pass in it, a shelf with an odd book on it, the decision card that stands between a find
/// and a pocket) and the sign verb beside it: a door that never opens, read out loud, and the lock coming
/// off one when a satchel try succeeds. Split out of <c>Map.Surface.Hive.cs</c> under #251 with no member
/// renamed, re-scoped or re-ordered.
/// </summary>
public partial class Map
{
    /// <summary>#585 - Turning over one room of the facility. About a third are stripped; the rarest thing in
    /// the building is a FILE ON SOMEBODY, because it is the only haul you spend on a person.</summary>
    private void HiveHaulInteract()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveHaul } spot)
        {
            return;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        int which = -1;
        for (int i = 0; i < floor.RoomCentres.Count; i++)
        {
            if (Math.Abs(floor.RoomCentres[i].X - spot.X) < 0.5
                && Math.Abs(floor.RoomCentres[i].Y - spot.Y) < 0.5)
            {
                which = i;
                break;
            }
        }
        if (which < 0)
        {
            return;
        }

        // #678 · THE ROOM IS NOT CONSUMED UNTIL THE FIND IS. This used to be one line — test the key and mark
        // it emptied in the same breath — which is exactly why a find the pocket could not take was destroyed
        // by the act of looking at it. Now the room is only struck off once the pickup has actually happened.
        int roomKey = HiveInterior.RoomKey(ex.Floor, which);
        if (ex.HiveRoomsEmptied.Contains(roomKey))
        {
            return;
        }

        UndergroundComplex.Haul haul = UndergroundComplex.InRoom(ex.Stop.Body.Id, ex.Floor, which);

        // ── #804 · SOMEBODY ELSE'S SITE PASS, IN A DRAWER ────────────────────────────────────────────────
        //
        // Owner, in the issue's own point 4: "our own badge once we get a gig, or FALSE IDs we DISCOVERED."
        // The wallet has judged a foreign pass properly since #836 (WalletChoice.Outcome.WrongSite) and
        // nothing authored has ever dealt one, so the rung has been unreachable outside a dev cheat.
        //
        // ONE CALL, and the take itself lives in Map.FoundPass.cs beside the mint — the black-ops key's own
        // shape (TakeTheBlackOpsKey), and it is a law here rather than a preference: this file may not add to
        // the satchel and may not strike a room off, because every HAUL has to go through the KEEP/LEAVE
        // decision to reach either (#615/#678, pinned by TheRoomKeepsWhatYouWalkedPastTests and
        // TheShelfIsReadWhereItStandsTests). A pass is not a haul — it goes to the wallet, which has no
        // ceiling and nothing to weigh — so it takes the key's road and not the sleeve's.
        //
        // It sits BEFORE the odd book because both are things a would-be-empty room has instead of the empty
        // line — but they cannot both be here, and that is enforced in Core rather than by this ordering
        // (OddBooks.CouldHoldOne asks FoundPass.IsHere). Order here is legibility, not law.
        if (TheDrawerHandsOverAFalseId(ex, which))
        {
            return;
        }

        // ── #701 · THE ODD BOOK ─────────────────────────────────────────────────────────────────────────
        //
        // Owner: "a better alternative to finding an empty room. You look around but only one book catches
        // your attention." Searching a room and finding nothing is the most common outcome in this building
        // and the least written; one would-be-empty room in six now says something instead.
        //
        // It happens BEFORE the pocket, and it returns without ever reaching it, because a book is not a
        // haul: nothing goes in the satchel, no credits change hands, and — the load-bearing part — THE ROOM
        // IS NOT STRUCK OFF. The book is still on the shelf, so the console is still standing there and [E]
        // opens the card again. Re-reading is free; the casebook learns the gist once per book per thread
        // (#603's law: looking is free, knowledge is one-shot), which Core decides, not this method.
        //
        // The shelf line is the ROOM's line; the GIST is what the book is worth and is the thing the casebook
        // keeps. Filing both would put the same shelf in the book twice in two registers, and the second
        // search would file it a third time.
        if (OddBooks.Search(ex.Stop.Body.Id, ex.Floor, which, _oddBooksRead, _bookCheat) is { } shelf)
        {
            // #528's idiom, caption-only: there is no art file for these yet and one that is wired but
            // unpainted would render an img the browser hides — the lifeboat-muster precedent is a card that
            // never claims a picture at all.
            //
            // #736 · THE SHELF LINE IS ON THE CARD, not under it. The comment three lines down has said since
            // #701 that "the pulse would play under the card's own blur" — and the shelf line was pulsed on
            // the frame this card goes up, so the sentence that tells you WHY one book caught your eye was
            // the one sentence of the beat behind the frosted glass. Composed into the card's own story, the
            // way #603's document card already carries its try's answer: one object card, one text.
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                shelf.Title, null, shelf.Line + "\n\n" + shelf.Card);

            if (shelf.Gist is { } gist)
            {
                // FileNote and not ShowAndFile: the saying is on the card above, and a pulse would play under
                // the card's own blur (#686/#736).
                FileNote(gist, OddBooks.Glyph);
                _oddBooksRead = [.. shelf.Filed];
            }

            RequestVaultSave();
            return;
        }

        // Everything found down here is FILED (#587) - this is the place in the game most worth being able to
        // re-read, and a file on a harbourmaster that faded after eight seconds would be a joke.
        // #613 \u00b7 AND SAY WHETHER IT WENT INTO YOUR POCKET. Owner, after clearing a corridor: "now I used e
        // to search and then checked inventory on many rooms" / "we should tell the users if stuff is picked
        // up into inventory or not."
        //
        // The haul line describes what is in the room and stops, which was right while everything either
        // paid out in credits or was flavour. Some of it is now a THING YOU CARRY and some of it is not, and
        // the captain was opening the satchel after every single room to find out which \u2014 the game asking
        // the player to audit it.
        // \u2500\u2500 #613 \u00b7 THE CARD YOU FOUND IS ALWAYS A CARD YOU CARRY \u2500\u2500
        //
        // Owner, in a four-floor site: "now I picked authority card but it did not go to inventory."
        //
        // He was right and the cause was mine. CardInRoom returns the card for the band BELOW, and correctly
        // returns null on the bottom band \u2014 a card for a hole nobody dug would be a lie. But the client then
        // handed out a lead and put NOTHING in the pocket, while the prose went on describing a countersigned
        // card in the captain's hand. The sim did one thing and the sentence said another: this repo's third
        // named bug class, and I shipped it again.
        //
        // A card is an OBJECT. You picked it up, so you have it. When the shaft it runs is not in this
        // building it is a card for another one \u2014 which is exactly the wallet the refusal line has been
        // describing all along ("every one of them countersigned, current, and for another shaft"). Until now
        // that line was describing a thing the game could not give you. Now the deepest floor of one facility
        // hands you the way into the next, which is the best thing a bottom floor could possibly hold.
        //
        // ── #684 · AND THE LEAD IS NOT SPENT UNTIL THE CARD IS IN THE POCKET ──
        //
        // This used to call GrantLabLead here, which BANKS the lead and says it out loud — one step ahead of
        // the capacity check below. On a bottom-band Key with a full pocket the find is refused, the room is
        // not emptied and the same card is offered again on the next search (#678's law) — but the lead had
        // already been heard, and news can only be heard once. The captain kept the knowledge without ever
        // carrying the card that was supposed to be how they got it.
        //
        // The moon is only NAMED here now. It is announced further down, after the pocket has agreed.
        UndergroundComplex.AuthorityCard? found = null;
        string? farLead = null;
        if (haul == UndergroundComplex.Haul.Key)
        {
            found = UndergroundComplex.CardInRoom(ex.Stop.Body.Id, ex.Floor);
            if (found is null
                && NameAMoonWorthLookingAt(
                    DiceRule.Seed($"lead:hive-key:{ex.Stop.Body.Id}:{ex.Floor}:{which}")) is { } far)
            {
                farLead = far;
                if (UndergroundComplex.SiteHasBand(far, 0))
                {
                    found = new UndergroundComplex.AuthorityCard(far, 0);
                }
            }
        }

        // ── #678 · THE POCKET NEVER LIES ──
        //
        // Owner, after a card he had read a pickup line for turned out not to be in the satchel: "we should
        // have CI test that makes sure all picked items that sound useful are put into the inventory ... If
        // refused the item should stay where it was investigated last — not disappear like they do now, or
        // seem to."
        //
        // The composition used to live here, in the wrong order: the "Into your pocket" line was built and
        // shown, the room was already struck off, and only THEN did Satchel.Add get a chance to refuse. At
        // capacity the find was destroyed and the sentence had already claimed it. It is one pure call now
        // (UndergroundComplex.WhatGoesInThePocket), which is the only way a test can walk every haul against
        // every pocket — and it answers all three parts at once: what goes in, what is said, and whether the
        // room has been emptied at all.
        // #677 · Core mints the id, because the id says which KIND of place the thing came out of and three
        // separate seams read that later — the pocket line, the satchel row and the look-card. Composed here
        // as a string literal it was one place; the moment a second class of relic existed it would have
        // been the client teaching itself a fact about a band it does not own.
        string findId = UndergroundComplex.FindId(ex.Stop.Body.Id, ex.Floor, which);
        string room = UndergroundComplex.HaulLine(haul, ex.Stop.Body.Id, ex.Floor, which, found);
        var find = new KeepOrLeave.Pending(
            ex.Stop.Body.Id, ex.Floor, which, haul, findId, found, farLead, room);

        // ── #615 · A FIND IS A DECISION, NOT AN AUTOMATIC PICKUP ───────────────────────
        //
        // Owner: "should we have like keep / leave option when we find stuff?"
        //
        // Everything past this gate used to run on the frame the room was searched — the sentence, the
        // pocket, the strike-off, the book. With a twelve-slot sleeve that filled the captain with paper
        // they had already decided was worthless, and it took the decision away from the one system in this
        // game built entirely out of them (#603).
        //
        // Asked of what the ROOM WOULD HAND OVER and never of what the pocket would accept: a captain with
        // no room left is precisely the captain the question is worth asking, and a gate that read capacity
        // here would go quiet at the one moment it means anything. The key is exempt — it is the way down
        // and not a paper (#1069) — and the argument for that, and for the crate, is in KeepOrLeave.
        if (UndergroundComplex.WhatTheRoomHandsOver(haul, found, findId) is { } offered
            && KeepOrLeave.IsADecision(haul, offered))
        {
            // The sleeve's own row name for the thing lying there, and the room's own sentence under it. No
            // picture: the pallet's photograph and the sheet's page are what KEEPING it buys (#828's tiers),
            // and a card that read the document out before the decision would be the pickup back again.
            OfferKeepOrLeave(find, SatchelLabel(offered), null);
            return;
        }

        UndergroundComplex.Pickup pick = UndergroundComplex.WhatGoesInThePocket(
            haul, ex.Stop.Body.Id, found, findId, _satchel);

        if (!pick.RoomEmptied)
        {
            // Core's own refusal, kept because Core's own contract keeps it: every find a compartment can
            // refuse now goes through the decision above, and the one haul that reaches here carrying
            // anything is the KEY, whose wallet has no ceiling. The day a wallet grows one, this is the line
            // that stops a card being destroyed by the act of looking at it.
            //
            // Nothing changes but the sentence. The room is not struck off, so searching it again offers the
            // same find — which is the enforcement side of #615 (leaving a thing must never destroy it).
            // The deck is deliberately NOT rebuilt: the console has to still be standing there.
            ShowAndFile(room + pick.Line, "\ud83d\udd26");
            RequestVaultSave();   // the field note is a possession too (#587)
            return;
        }

        TheFindGoesInThePocket(ex, find, pick);
    }

    /// <summary>#585 - A door that never opens, read out loud. It is a WALL with a world behind it, and the
    /// game never once pretends otherwise - a door that teases would turn scale into a puzzle.</summary>
    private void HiveSignInteract()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveSign } spot)
        {
            return;
        }
        // #803 \u00b7 A door somebody took the hasp off is not a locked door any more, and the sign it wears says
        // which of the two it is. Reading it is still worth a press \u2014 the plate is the only thing in the
        // corridor that names the room \u2014 but it offers no pockets and tells no locked-door story, because
        // there is nothing left on it to try.
        if (spot.Label.StartsWith(HiveInterior.ShotOpenGlyph, StringComparison.Ordinal))
        {
            ShowPulseMessage(ShootTheLock.BehindItLine(
                spot.Label.Replace($"{HiveInterior.ShotOpenGlyph} ", "", StringComparison.Ordinal)));
            return;
        }

        string sign = spot.Label.Replace("\ud83d\udd12 ", "");

        // #528 \u00b7 THE WAY ON, CLOSED. Owner, standing at a rib's far end: "I see there is a nice lock here at
        // the end of the corridor.... maybe we could have a gen-AI image for it and a pop-up to tell the
        // story?"
        //
        // It fires at the moment it explains the most (#528's fourth rule): the captain has just walked the
        // length of a rib and met a plate with a distance painted on it. ONLY the sealed way earns a card \u2014 a
        // room door that will not open is a sign to read, not a scene, and forty of them would be a
        // slideshow. The pulse line still says its piece underneath, here and every time after.
        if (UndergroundComplex.IsSealedWay(sign) && _surface is { } sealedEx && !sealedEx.HiveSealedWayShown)
        {
            sealedEx.HiveSealedWayShown = true;
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.SealedWayCardLabel,
                UndergroundComplex.SealedWayArtUrl,
                UndergroundComplex.SealedWayCard(sign));
            ApplyNerveShock(3.0, "a corridor somebody dug for a year and then closed");
        }

        // ── #603 · AND THE DOOR TELLS YOU YOU HAVE POCKETS ──
        //
        // Owner: "we should advertise the items list on closed locked door pop-up... something like check
        // your items button there that opens inventory."
        //
        // This is the #212 law applied to the satchel: the refusal already said WHY, and a captain standing
        // in front of it with an authority in their pocket had no way to find out whether it was the one.
        // The door now offers the verb. It is also how the satchel gets discovered at all — nobody reads a
        // keybind, and everybody presses the button on the thing that just refused them.
        _lockedDoor = new LockedDoorLook(
            sign,
            UndergroundComplex.LockedLine(sign),
            // #1074 · …and it asks whether there is anything on this door to try a card AGAINST, which is one
            // Core predicate and not two: a rib's sealed mouth and the stop order's seal both answer no, and
            // the client must never be the place that decides what a sign is.
            UndergroundComplex.HasNoReader(sign)
                ? SatchelTry.Target.SealedWay
                : SatchelTry.Target.RoomDoor);
    }

    /// <summary>
    /// #803 · THE HASP COMES OFF, IN THE BUILDING'S OWN GRAMMAR.
    ///
    /// <para>The floor plan is pure and deterministic per (body, level), so the door that was shot is found
    /// where it has always been rather than remembered as a shape: the plan is rebuilt, the lock nearest the
    /// console the captain fired at is identified, and its key is written down. Every later rebuild replays
    /// it — a floor change, a searched room, a save and a load — which is the same seam an emptied room
    /// rides and the reason a shot door cannot grow its wall back behind the captain's shoulder.</para>
    ///
    /// <para>The MATCH is by geometry and by nothing else. A branch office reuses its door vocabulary, so a
    /// floor can carry two doors reading LONG STORAGE and shooting one of them is not shooting the other —
    /// keying on the sign would open both, quietly, forever.</para>
    /// </summary>
    private void TheLockComesOff(SurfaceExcursion ex, DesignateTarget target)
    {
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(
            ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());

        string? key = null;
        double bestSq = double.MaxValue;
        foreach (UndergroundComplex.LockedDoor l in floor.Locked)
        {
            double mx = (l.X1 + l.X2) / 2, my = (l.Y1 + l.Y2) / 2;
            double dx = mx - target.X, dy = my - target.Y;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < bestSq)
            {
                bestSq = d2;
                key = HiveInterior.LockKey(ex.Floor, l);
            }
        }

        // A console spot IS a lock's midpoint, so the nearest one is the one that was shot; the tolerance
        // exists only so a future renderer nudging a plate by a hair cannot silently open a different door.
        if (key is null || bestSq > 1.0)
        {
            return;
        }

        ex.LocksShotOpen.Add(key);
        RebuildSurfaceDeck();
    }

    /// <summary>#603 · The door the captain is standing at, while its pop-up is up.</summary>
    public sealed record LockedDoorLook(string Sign, string Line, SatchelTry.Target Target);

    private LockedDoorLook? _lockedDoor;

    private void CloseLockedDoor() => _lockedDoor = null;
}
