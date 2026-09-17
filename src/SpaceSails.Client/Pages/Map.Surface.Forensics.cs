using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: #316 laws 1 (second half), 3 and 4 — what somebody ELSE leaves on our ground, and what our own
// firefight tells them.
//
// ─────────────────────────────────────────────────────────────────────────────────────────────────────
// Owner, live 2026-07-18: "If we find already shot Reevers at a site then we know that somebody else has
// been there to hide, pick-up, search etc :-D It serves as a clue."
//
// #1105 shipped the half a captain writes himself: his own downed Old Ones persist on the ground with where
// and when, survive lift-off and a vault round-trip, and read their age on the underfoot pulse (#1127's three
// bands). What that could never produce is the thing the owner actually described — evidence of SOMEBODY
// ELSE — because the only event that has ever happened at one of our caches while we were away is the
// watchdog economy's discovery roll, and that roll's whole output was a squawk and a deletion. The chest
// vanished from the ledger, the ✗ vanished from the ground, and a captain who flew back to the spot found
// clean regolith and no way at all to tell a robbery from a mis-remembered map.
//
// So: when the roll lands, THE GROUND CARRIES IT. Not as decoration — law 4 forbids that, and it is the
// right law: a scene generated fresh each time it is drawn looks perfect once and betrays itself on the
// second visit. Core turns the settled roll into the marks it must have left (RivalVisit), this page writes
// them to the same ledger the captain's own husks ride, and the vault carries them because the captain is in
// ORBIT when it happens and can only ever meet it on a later trip.
//
// And the symmetry, which is the design (law 3): the husks HE left raise the odds for every chest on that
// ground. The ledger does not record whose fight it was — and neither does whoever is out there reading it.
// That anonymity is law 1, not a shortcut: a field of husks says somebody was here, and never who.
// ─────────────────────────────────────────────────────────────────────────────────────────────────────
public partial class Map
{
    /// <summary>
    /// WHICH GROUND A CHEST IS UNDER, as the ground ledger spells it — the site's layout salt, the same
    /// string an excursion carries (<c>ex.Site.LayoutSalt</c>) and the same one every husk key on that ground
    /// was built from.
    ///
    /// <para>Null when the chest is BODY-WIDE: a legacy save or a rumour map, minted before a cache recorded
    /// which of the body's 2–4 landing sites (#320/#650) it went into. We genuinely do not know which ground
    /// that chest is under, and a guess would scatter a firefight across a site the captain has never walked
    /// — which is exactly the bug #650 fixed for the ✗ itself. No site, no evidence.</para>
    /// </summary>
    private static string? GroundSaltFor(TreasureCache cache) =>
        cache.SiteIndex is { } i ? LandingSites.At(cache.BodyId, i).LayoutSalt : null;

    /// <summary>
    /// #316 law 3 · HOW LOUD THIS GROUND IS — the husks already lying on the site a chest is under, off the
    /// ship's ground ledger. The one input the discovery odds take from the world rather than from the chest,
    /// and the reason a sentry loadout is an information choice: <i>the quiet dig leaves nothing; the loud
    /// stand leaves a signpost.</i>
    ///
    /// <para>Zero for a body-wide chest, which is the honest answer rather than a soft one: without a site
    /// there is no ground to count the bodies on.</para>
    /// </summary>
    private int TheFightThisGroundCarries(TreasureCache cache) =>
        GroundSaltFor(cache) is { } salt ? _groundMemory.HuskCountAt(cache.BodyId, salt) : 0;

    /// <summary>The same count for the ground the captain is STANDING ON — what the shovel's own line has to
    /// quote, because a chest going into this regolith right now will be rolled against exactly this.</summary>
    private int TheFightThisGroundCarries(SurfaceExcursion ex) =>
        _groundMemory.HuskCountAt(ex.Stop.Body.Id, ex.Site.LayoutSalt);

    /// <summary>
    /// <b>THE RIVALS WERE HERE, AND THE GROUND KEEPS IT.</b> Called once, by the discovery watch, the moment
    /// a cache resolves as taken — the second writer into <see cref="_groundMemory"/> and the only one that
    /// writes a ground the captain is not standing on.
    ///
    /// <para>Everything it writes comes out of <see cref="RivalVisit.LeftBehind"/>: the husks are the pack
    /// the rivals had to go through (the captain's own 2D6, thrown on the chest and the day), the hole is at
    /// the ✗ itself — <see cref="MoonSurface.CacheSpot"/>, the one projection, so the hole is where the mark
    /// was and not near it — and the full pack costs them a sentry. Nothing is invented here and nothing is
    /// rolled here.</para>
    ///
    /// <para><b>Stamped with the day it happened, not with now.</b> The watch scans every whole day the
    /// captain skipped and hands back the one the roll landed on, so a fortnight's warp comes home to a
    /// fortnight of dust rather than to a fresh kill that lies about when he was beaten to it.</para>
    /// </summary>
    private void TheRivalsLeftTheirMarks(TreasureCache cache, long foundPeriod)
    {
        if (GroundSaltFor(cache) is not { } salt)
        {
            return;   // body-wide chest: no ground to file it against (see GroundSaltFor)
        }

        (double spotX, double spotY) = MoonSurface.CacheSpot(cache);
        RivalVisit.Evidence left = RivalVisit.LeftBehind(
            cache.Id, cache.ReeverLevel, spotX, spotY, foundPeriod);

        bool wrote = false;
        foreach (GroundMemory.Husk husk in left.Husks)
        {
            wrote |= _groundMemory.Remember(GroundMemory.HuskKey(cache.BodyId, salt, husk));
        }
        foreach (GroundMemory.Scar scar in left.Scars)
        {
            wrote |= _groundMemory.Remember(GroundMemory.ScarKey(cache.BodyId, salt, scar));
        }

        if (wrote)
        {
            RequestVaultSave();   // he is in orbit; the file is the ONLY way this reaches him
        }
    }

    /// <summary>
    /// #711 slice 2 · <b>SOMEBODY DUG, AND THE GROUND KEEPS THAT TOO.</b> A dead drop that has been paid for
    /// is a hole a stranger opened: the chest is gone from the ledger and there is a
    /// <see cref="GroundMemory.ScarKind.Pit"/> where the ✗ was.
    ///
    /// <para><b>It lives in THIS file because somebody else's marks have one writer</b>
    /// (<c>TheGroundKeepsSomebodyElsesFootprintsTests</c>, #1105's law one issue on): a second minting site
    /// is a second place to forget the vault. Nothing new is invented here — the same
    /// <see cref="MoonSurface.CacheSpot"/> projection, so the hole is where the mark was and not near it,
    /// and the same <see cref="GroundSaltFor"/> refusal for a body-wide chest.</para>
    ///
    /// <para><b>Stamped with the moment it came DUE</b>, not with now, for
    /// <see cref="TheRivalsLeftTheirMarks"/>'s own reason: a captain who flies back a fortnight later should
    /// read a fortnight of dust rather than a fresh kill that lies about when he was beaten to it. Here the
    /// moment is the payment's, because a payment on the desk IS the report that the hole was opened.</para>
    ///
    /// <para>The caller saves the file — it has coin to move and a pulse to say in the same breath — so this
    /// method does not, which is the one difference from its sibling above and the reason it is written as a
    /// question the caller answers rather than as a second <c>RequestVaultSave</c>.</para>
    /// </summary>
    private void SomebodyDugIt(TreasureCache lifted, double whenSimTime)
    {
        if (GroundSaltFor(lifted) is not { } salt)
        {
            return;   // body-wide chest: no ground to file it against (see GroundSaltFor)
        }

        (double x, double y) = MoonSurface.CacheSpot(lifted);
        _groundMemory.Remember(
            GroundMemory.ScarKey(
                lifted.BodyId, salt, new GroundMemory.Scar(GroundMemory.ScarKind.Pit, x, y, whenSimTime)));
    }

    /// <summary>What the ground kept that is not a body, before the first frame of this visit is drawn — the
    /// sibling of <see cref="SeedTheHusksLeftHere"/>, in the same place and for the same reason. Core reads
    /// its own rows; nothing here knows the key format, and nothing here rolls.</summary>
    private void SeedTheScarsLeftHere(SurfaceExcursion ex)
    {
        foreach (GroundMemory.Scar scar in _groundMemory.ScarsAt(ex.Stop.Body.Id, ex.Site.LayoutSalt))
        {
            ex.Scars.Add(scar);
        }
    }

    // ── #563 · AND THE ONES WHO CAME BEFORE YOU ──────────────────────────────────────────────────────
    //
    // Owner ruling, 2026-09-13, closing the last of #563's three open questions: "I love the own lineage.
    // If not enough material, fill in with strangers, preferably NPCs we know something about."
    //
    // And the loop it feeds, #455 in the owner's own words: "after pirate insurance rebirth you can come
    // see if your loot is still there." He flies back for the chest. What is waiting beside it now is the
    // man who buried it, still wearing the license — because the policy issued this captain over that one's
    // body, on this ground, on a day the roster wrote down.
    //
    // NOT SEEDED FROM THE GROUND LEDGER, and that is the design rather than an economy: the roster already
    // holds the fact (RetiredCaptain.Grave, written once at the succession), and a scar row beside it would
    // be two records of one death in the save file for the rest of that universe's life — the fourth named
    // bug class, where it is most expensive. The mark is DERIVED here, on arrival, so it is exactly as
    // durable as the roster and can never drift from it.

    /// <summary>
    /// The suits this captain's own line left on this ground, out of the thread's roster and onto the
    /// visit's mark list. Called at arrival beside <see cref="SeedTheHusksLeftHere"/> and
    /// <see cref="SeedTheScarsLeftHere"/>, for the reason they are: a ground painted ahead of the seeding is
    /// a field a captain walks into clean and then watches sprout bodies.
    ///
    /// <para>Nothing here rolls and nothing here writes. Core reads the roster
    /// (<see cref="LineageMark.BuriedOn"/>); this only carries the answer onto the list the renderer
    /// walks.</para>
    /// </summary>
    private void SeedTheLineageLyingHere(SurfaceExcursion ex)
    {
        foreach (RetiredCaptain gone in LineageMark.BuriedOn(
            ActiveThreadInfo?.Retired, ex.Stop.Body.Id, ex.Site.LayoutSalt))
        {
            ex.Scars.Add(LineageMark.MarkFor(gone));
        }
    }

    /// <summary>
    /// #563 · <b>WHO A MARK BELONGS TO, AND THEREFORE WHAT THE BOOK WRITES ABOUT IT.</b> The owner's own
    /// order, in one method: <b>your own lineage first</b>, and a stranger only where the lineage has none.
    ///
    /// <para>A suit is always one of yours — it was derived from your own roster, so it cannot be anything
    /// else. Anything else on this ground (the rivals' hole, the sentry they walked away from) gets a NAME
    /// only when no predecessor of yours died here: a tag reading a stranger's name over your own
    /// predecessor's grave would be the fill-in overwriting the material it exists to stand in for.</para>
    ///
    /// <para><b>The name is drawn, never invented</b> (<see cref="LineageMark.StrangerFor"/>): a member of
    /// the game's own named cast, seeded off the body and the site so one ground always answers one name.
    /// And it changes NOTHING about the mark — not where it is, not when it happened, not the odds it feeds.
    /// #316's marks are untouched; this reads a name onto one.</para>
    ///
    /// <para><b>This page declares nothing.</b> Per #741 a subject comes from the AUTHOR and never from the
    /// prose, so <see cref="LineageMark"/> hands the words, the glyph and the subjects over together
    /// (<see cref="LineageMark.Page"/>) and all this method does is decide WHICH page. Nothing in the client
    /// mints a subject: the filing funnel's own sweep (<c>TheFilingFunnelCarriesTheAuthorsSubjects</c>)
    /// walks every file under <c>src/SpaceSails.Client</c> and fails the build if any of them so much as
    /// SPELLS one of the three minting calls — this comment included, which is how it should be.</para>
    /// </summary>
    private LineageMark.Page? WhatThisMarkSays(SurfaceExcursion ex, GroundMemory.Scar scar)
    {
        IReadOnlyList<RetiredCaptain> mine =
            LineageMark.BuriedOn(ActiveThreadInfo?.Retired, ex.Stop.Body.Id, ex.Site.LayoutSalt);

        if (scar.What == GroundMemory.ScarKind.Suit)
        {
            // WHICH of them is at your feet: the one whose grave is at this exact spot. The scar was built
            // out of that grave's own position, so this is an identity and not a proximity — two captains
            // who happened to die on one tile still get one page each.
            RetiredCaptain? here = mine.FirstOrDefault(
                c => c.Grave is { } g && g.X == scar.X && g.Y == scar.Y);
            return here is null ? null : LineageMark.YoursPage(here);
        }

        if (mine.Count > 0)
        {
            return null;   // your own line died on this ground — the strangers do not fill in over them
        }

        return LineageMark.StrangerPage(LineageMark.StrangerFor(ex.Stop.Body.Id, ex.Site.LayoutSalt));
    }

    /// <summary>
    /// #563 · <b>THE PRESS THAT READS A MARK.</b> Answered at the captain's FEET, ahead of the console
    /// dispatch, in the same place and by #688's argument: a captain standing on a thing cannot be made to
    /// walk off it to look at it.
    ///
    /// <para><b>It answers ONCE and then gets out of the way.</b> [E] on the regolith is BURY THE CHEST
    /// (#440), and a mark that ate that press for ever would be a grave a captain can never bury a chest
    /// beside — which is exactly the complaint #316 records against putting the husks' own reading on this
    /// key. One press per mark, and the ground is his again.</para>
    ///
    /// <para><b>The latch is a ground mark like any other</b> (<see cref="GroundMemory.ReadKey"/>) rather
    /// than a per-visit flag, because the thing it guards is durable: latched on the visit, the same
    /// sentence would be filed again next trip and the trip after, until the predecessor's THREADS stack
    /// was six copies of one line. That is not a book, it is a stutter.</para>
    ///
    /// <para><b>And the age comes from the ground, not from here</b> — <see cref="GroundMemory.AgeLine(
    /// double, double)"/>, #1127's own three bands off the sim clock, the same sentence a husk underfoot
    /// speaks. Dating a suit is the same question as dating a body, and this repo's third named bug class is
    /// two reporters of one truth.</para>
    ///
    /// <para>No pop-up (the general UI law of 2026-08-24): the line goes to the pulse and into the book, in
    /// the idiom the forensics already speak in.</para>
    /// </summary>
    private bool TryReadTheMarkAtYourFeet()
    {
        if (_surface is not { } ex || TheMarkTakingYourPress() is not { } scar
            || WhatThisMarkSays(ex, scar) is not { } page)
        {
            return false;
        }

        _groundMemory.Remember(ReadLatchFor(ex, scar));
        // The author's words, with the GROUND's own age line after them — #1127's three bands, the same
        // sentence a husk underfoot speaks. Dating a suit is the same question as dating a body.
        var said = new LineageMark.Page(
            $"{page.Text} {GroundMemory.AgeLine(scar.AtSimTime, SimTime)}", page.Glyph, page.Subjects);
        ShowPulseMessage($"{said.Glyph} {said.Text}");
        FileNoteAbout(said.Text, said.Glyph, said.Subjects);
        RequestVaultSave();   // the latch has to survive the shuttle, which means surviving the file
        return true;
    }

    /// <summary>
    /// #563 · <b>WHICH MARK — IF ANY — THIS PRESS BELONGS TO.</b> One question, asked by the key and by the
    /// keybar, because a bar that promised a reading the key would not give is the sim doing one thing while
    /// a sentence reports another — the bug class this repository has named and paid for three times.
    ///
    /// <para>Null when there is nothing in reach, when the mark has already been read
    /// (<see cref="GroundMemory.ReadKey"/>), or when it has nothing to say — a rival's hole on a ground your
    /// own line died on stays anonymous, which is the owner's own order of precedence.</para>
    /// </summary>
    private GroundMemory.Scar? TheMarkTakingYourPress()
    {
        if (_surface is not { } ex)
        {
            return null;
        }

        foreach (GroundMemory.Scar scar in ex.Scars)
        {
            double dx = scar.X - _avatarX;
            double dy = scar.Y - _avatarY;
            if ((dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius
                || _groundMemory.Knows(ReadLatchFor(ex, scar))
                || WhatThisMarkSays(ex, scar) is null)
            {
                continue;
            }

            return scar;
        }

        return null;
    }

    /// <summary>The "already read" key for one mark. The ledger's own scar key identifies it, so the latch
    /// and any stored row name the same thing — and a suit, which has no stored row at all because it is
    /// derived from the roster, is still told apart from its neighbours by exactly the same string.</summary>
    private static string ReadLatchFor(SurfaceExcursion ex, GroundMemory.Scar scar) =>
        GroundMemory.ReadKey(GroundMemory.ScarKey(ex.Stop.Body.Id, ex.Site.LayoutSalt, scar));
}
