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
}
