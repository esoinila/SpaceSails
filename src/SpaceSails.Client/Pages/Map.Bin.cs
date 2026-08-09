using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #798 · RIP IT AND BIN IT — the third answer, beside take and leave.
///
/// <para>Owner, live in play (2026-08-09): <i>"Now the option is to photograph the papers and leave them
/// there… I would not like to leave them at table in canteen… we need option to destroy them by ripping and
/// binning etc?"</i> And, filing it as the next thing to build: <i>"those trash cans are needed so we get
/// rid of the processed materials without connecting them to us too clearly, like leaving them to the
/// table."</i></para>
///
/// <para>The phase-two loop (#784) ends by producing a liability. You sit, you dig, the book takes the gist
/// — and then you are holding an original that is worth nothing to you and a great deal to anybody who finds
/// it in your coat or on your table. <b>Sit, dig, book, BIN</b> is the whole shape, and until this file ran
/// the last rung did not exist.</para>
///
/// <h3>What this file decides, and what it does not</h3>
/// <para>Core (<see cref="RipAndBin"/>) owns the ladder, the reach and every word. The generator owns where
/// the bins are. What is left for a client is the three facts only a running world has: <b>which bin the
/// captain is standing at</b>, <b>whether anybody was looking</b>, and the act itself.</para>
///
/// <h3>The one law</h3>
/// <para><b>THE BOOK NEVER UNLEARNS.</b> Destroying an original touches the sleeve and nothing else — not a
/// filed note, not a red thread between two of them (#741), not the seated register's own written-up set.
/// What the captain dug out of a sheet is in the book in their own hand, and a hand-written page does not
/// come apart because the paper it was copied from did. Every line below that could break that law is
/// commented with the reason it does not.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Which (body, floor) <see cref="_binsOnThisFloor"/> was built for — the memo's key, because
    /// two sites number their floors the same way and B1 of one is not B1 of the other.</summary>
    private string? _binsBuiltFor;

    /// <summary>#798 · The bins on the floor under the captain's boots.
    ///
    /// <para>Memoised, and it has to be: the hint on every satchel row asks for it on every render, and a
    /// floor plan is a whole building's worth of arithmetic. The memo is keyed on the thing that changes it
    /// and nothing else — a floor's bins are a pure function of (body, level), exactly like its walls.</para></summary>
    private IReadOnlyList<RipAndBin.Bin> _binsOnThisFloor = [];

    /// <summary>#798 · Somewhere to put a paper, here. Empty everywhere but underground: the ship has a
    /// recycler nobody has modelled and the regolith has nothing at all, and a verb that quietly worked in
    /// places with no fixture would be the sim doing one thing while the world showed another.</summary>
    private IReadOnlyList<RipAndBin.Bin> BinsHere()
    {
        if (_surface is not { Floor: < 0 } ex)
        {
            (_binsBuiltFor, _binsOnThisFloor) = (null, []);
            return [];
        }

        string key = $"{ex.Stop.Body.Id}:{ex.Floor}";
        if (_binsBuiltFor != key)
        {
            _binsBuiltFor = key;
            _binsOnThisFloor = UndergroundComplex
                .Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).TheBins;
        }
        return _binsOnThisFloor;
    }

    /// <summary>#798 · The bin the captain is actually standing at, or null. Core's one answer
    /// (<see cref="RipAndBin.NearestWithinReach"/>), asked with the client's one fact — where the boots
    /// are — so the control that is drawn, the hint that explains it and the act that fires can never come
    /// to three different views of which bucket this is.</summary>
    private RipAndBin.Bin? BinWithinReach() =>
        RipAndBin.NearestWithinReach(_avatarX, _avatarY, BinsHere());

    /// <summary>#798 · Is the shredder offered on this row at all? Only for EVIDENCE — a card, a handful of
    /// rounds and the paperwork for something too big to lift are not things anybody reads over your
    /// shoulder, and the control follows the verb the way the spread tab follows the posture: on a stack of
    /// rounds this is not a refusal, it is a page that does not apply.
    ///
    /// <para>Live wherever it is drawn, and never disabled (#212/#603): standing in a corridor with no bin
    /// it answers with the sentence that says what would fix it, which is how the law is learned.</para></summary>
    private static bool RipIsOffered(Core.Satchel.Item item) => RipAndBin.IsEvidence(item.Kind);

    /// <summary>#798 · What the control says before it is pressed — the bucket and its bet when it will
    /// work, the refusal when it will not. The price of a press is known before the press (#696's own
    /// discipline, one control over).</summary>
    private string RipHint(Core.Satchel.Item item) =>
        !RipAndBin.IsEvidence(item.Kind) ? RipAndBin.NotEvidenceLine
        : BinWithinReach() is { } bin ? $"{RipAndBin.Hint(bin.Tier)} {RipAndBin.TierBet(bin.Tier)}"
        : RipAndBin.NoBinLine;

    /// <summary>
    /// #798 · THE ACT.
    ///
    /// <para>Both gates are asked out loud, in the order a captain meets them — is this a thing you tear up,
    /// and is there anything here to put it in — because a control that does nothing and says nothing is
    /// indistinguishable from a bug, and this ground has shipped that mistake twice in a week.</para>
    ///
    /// <para>It is deliberately NOT <see cref="LeaveItem"/> with a different sentence. Leaving is #615's law
    /// — <i>leaving never destroys</i>, the thing lies on the square you are standing on and
    /// <c>TryPickUpWhatYouLeft</c> hands it straight back. This is the opposite verb, and the difference is
    /// the whole feature: nothing is written to the ground, so nothing can be picked up again, by you or by
    /// anybody else.</para>
    /// </summary>
    private void RipItUp(Core.Satchel.Item item)
    {
        if (_surface is null)
        {
            return;
        }

        if (!RipAndBin.IsEvidence(item.Kind))
        {
            SayItWhereTheyAreLooking(RipAndBin.NotEvidenceLine);
            return;
        }

        if (BinWithinReach() is not { } bin)
        {
            SayItWhereTheyAreLooking(RipAndBin.NoBinLine);
            return;
        }

        // The label is read BEFORE the sleeve loses the row, because SatchelLabel rebuilds the prose from
        // the world and the item, and a sentence about a thing you are no longer carrying is a sentence
        // about nothing.
        string label = SatchelLabel(item);

        // ── THE EVIDENCE GOES, AND NOTHING ELSE DOES ────────────────────────────────────────────────────
        //
        // ONE line, touching ONE list. _fieldNotes, _caseThreads and ex.WrittenUpProperly are not mentioned
        // in this method and must never be: the book keeps what you dug out of the sheet, the red lines you
        // drew between entries stay drawn, and the seated register still knows this document was written up.
        // Nothing here goes to the ground either (#615's Leave is the other verb) — the sheet stops existing.
        _satchel = [.. Core.Satchel.Remove(_satchel, item.Kind, item.Id, item.Count)];

        // …and the ACT is filed, with the BUCKET named in it. The three tiers do the same thing today; the
        // word on this line is the entire difference a later arc has to read back when it decides whether
        // anything comes of it. It is a fact about what the captain did and never a verdict about what it
        // bought them, because nothing in this game knows that yet.
        FileNote(RipAndBin.DisposalNote(label, bin.Tier), RipAndBin.Glyph);

        string said = RipAndBin.RippedLine(label, bin.Tier);
        if (WhoIsWatchingYouRip() is { } who)
        {
            // #715's per-entity memory, arriving as one line in a book rather than as a meter — the same
            // shape #804's escort files, and for the same reason: a meter would be the announcement the
            // canon section rules out. Nothing reacts. Somebody simply knows.
            FileNote(RipAndBin.SeenNote(who), RipAndBin.SeenGlyph);
            said = $"{said} {RipAndBin.SeenLine(who)}";
        }

        // Said where the captain is actually looking (#680/#736) — the satchel stays OPEN through this, so
        // a line sent to the HUD would be in the DOM and under the backdrop's blur.
        SayItWhereTheyAreLooking(said);
        RequestVaultSave();
    }

    /// <summary>
    /// #798 · WAS ANYBODY LOOKING?
    ///
    /// <para>Owner: <i>"Ripping is a visible ACT: done at a bar desk or a watched table, it is a memory —
    /// 'the new face tore something up and binned it'."</i></para>
    ///
    /// <para><b>The LADDER is Core's</b> (<see cref="RipAndBin.WhoSaw"/>) and this method decides only what
    /// its four flags MEAN in a running world — the same division #784 draws for the spread, where the
    /// client says what "seated" and "alone" are and never what a seat is FOR. A ladder spelled out at a
    /// call site is a ladder nobody can test both directions of.</para>
    ///
    /// <para>The last flag is asked with a LINE OF SIGHT rather than a radius — the same wall law the
    /// captain, the pack, the sweep team and the rounds all obey — which is what buys a cabinet its privacy
    /// without one clause here knowing what a cabinet is.</para>
    /// </summary>
    private RipAndBin.Watcher? WhoIsWatchingYouRip()
    {
        if (_surface is not { Floor: < 0 } ex)
        {
            return null;
        }

        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();

        // The rota: the same predicate the challenge itself runs on (PatrolBeat.Notices), including the
        // grace off the car — a guard who has not registered you yet has not seen you do anything.
        bool rota = false;
        if (PatrolBeat.CanBeNoticed(_patrolFloorSeconds))
        {
            foreach (Guard g in _guards)
            {
                rota |= PatrolBeat.Notices(g.X, g.Y, _avatarX, _avatarY, sight);
            }
        }

        // …and anybody at a seat who can see your hands. Core's own list of who is sitting where, off the
        // frozen watch the room was drawn with, so the people who saw it are the people on the floor.
        bool overlooked = false;
        foreach (UndergroundComplex.Amenity a in
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
        {
            foreach (CanteenRegulars.TableSeat top in
                CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch))
            {
                overlooked |= top.Taken
                    && PatrolBeat.EyesOn(top.X, top.Y, _avatarX, _avatarY, RipAndBin.OverlookedDu, sight);
            }
        }

        return RipAndBin.WhoSaw(
            rota,
            // The counter is unconditional, and it is the same ruling SeatedSpread makes about the same
            // seat: a bar top is in full view of the keep — who is security (#781) — and of everyone
            // waiting to be served. You cannot even spread the case here; tearing something up is louder.
            atTheCounter: SeatedIn == SeatedHud.Seat.BarStool,
            companyAtTheTable: _table is not null && !SeatedAlone,
            overlooked);
    }
}
