using System.Text.Json.Serialization;

namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────────
// #563 · WHERE THE LAST CAPTAIN ACTUALLY FELL
//
// Owner ruling, 2026-09-13, closing the third of #563's three open questions — strangers or your own
// lineage: "I love the own lineage. If not enough material, fill in with strangers, preferably NPCs we know
// something about."
//
// The roster has remembered WHO held the license before since Evening wind #20 — a name and a day, and the
// one line that reads them out ("under Capt. Mabel Vane until day 42"). What it has never remembered is
// WHERE, and without a where there is nothing on any ground to walk up to. A breadcrumb that comes from
// your own lineage is a POSITION or it is nothing.
//
// So this is the smallest fact that turns the roster into a place: the ground the death happened on, the
// spot on it, and the cause the death card already classified. Nothing else. The words a later captain
// reads are composed from it at reading time (LineageMark) and are never stored.
//
// ── THE TILE IS DERIVED AND NEVER STORED ─────────────────────────────────────────────────────────────
// The issue asks for "body id, site, tile — the same keying GroundMemory uses", and the same keying is
// precisely what this does: GroundMemory's own husks and scars carry a POSITION and expose the tile as
// `SurfaceTiles.At(X, Y)`, with the reason written out beside them — "the tile is DERIVED from the
// position, so a husk cannot be recorded on one tile and drawn on another; the fourth named bug class in
// this repo is exactly two answers to one question." A stored tile field here would be that bug, on a
// record that lives in the save file for the rest of the universe's life.
//
// ── WHICH DEATHS HAVE A GROUND AT ALL ────────────────────────────────────────────────────────────────
// Not a list maintained here. DeathNarration.CanHappen already states, cause by cause and with its
// reasons, which deaths can happen to somebody standing on a landing party's regolith — a collector on
// foot can (#583), an impact is the ship meeting a world at speed and cannot, the void has no ground by
// definition, a scuttling is bolted to a reactor and a moon has none. CanRecord asks that same one
// question rather than keeping a second list beside it.
// ─────────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>#563 · The ground a retired captain died on: which body, which of its landing sites, the spot in
/// that site's own coordinate frame, and what the death card called the cause. Null on every retiree the
/// game recorded before this existed, and on every death that did not happen on a ground.</summary>
/// <param name="BodyId">The body's id — the first half of every <see cref="GroundMemory"/> key.</param>
/// <param name="SiteSalt">The landing site's layout salt (<c>ex.Site.LayoutSalt</c>) — the second half. A
/// body offers 2–4 sites (#320/#650) and each rebuilds the same local frame, so a spot without a site is a
/// spot on four different grounds at once.</param>
/// <param name="X">Where, in the site's frame. The tile is read off it, never stored beside it.</param>
/// <param name="Y">See <paramref name="X"/>.</param>
/// <param name="Cause">What the death card classified it as — the one thing in the game that has ever
/// answered "what happened to him", and what <see cref="DeathNarration.CauseWord"/> reads back.</param>
public sealed record CaptainGrave(
    string BodyId, string SiteSalt, double X, double Y, DeathCause Cause)
{
    /// <summary>Parameterless default for tolerant JSON round-trips, exactly as
    /// <see cref="RetiredCaptain"/> keeps one: a garbled row reads as blanks rather than throwing the whole
    /// registry index and taking every universe in it with it.</summary>
    public CaptainGrave() : this("", "", 0, 0, DeathCause.Reevers) { }

    /// <summary>Which tile of the lattice he is lying on. Derived, never stored: one position, one
    /// answer — the law <see cref="GroundMemory.Husk"/> and <see cref="GroundMemory.Scar"/> are both built
    /// on.</summary>
    [JsonIgnore]
    public SurfaceTiles.Address Tile => SurfaceTiles.At(X, Y);

    /// <summary>Is this a grave on THIS ground? The one predicate the excursion asks of the roster, so
    /// nothing outside here ever compares the two halves of the key by hand.</summary>
    public bool IsOn(string bodyId, string siteSalt) =>
        string.Equals(BodyId, bodyId, StringComparison.Ordinal)
        && string.Equals(SiteSalt, siteSalt, StringComparison.Ordinal);

    /// <summary>
    /// <b>DOES THIS DEATH HAVE A GROUND TO LEAVE A MARK ON?</b> Asked of the cause alone, and answered by
    /// the rule that already owns the question: a death the card itself says cannot happen to a landing
    /// party did not happen to one.
    ///
    /// <para>The caller still has to be standing on regolith — the cause is necessary, never sufficient (a
    /// captain can suffocate 150 m under a moon, and there is no regolith down there to lie in).</para>
    /// </summary>
    public static bool CanRecord(DeathCause cause) =>
        DeathNarration.CanHappen(cause, DeathPlace.LandingParty);
}
