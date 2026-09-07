using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #804 · <b>THE FALSE ID, CLIENT SIDE: one roster, one mint, and nothing else.</b>
///
/// <para>Owner, in the issue's own point 4: <i>"THE BADGE, kept in the Fletch wallet: our own badge once we
/// get a gig, or FALSE IDs we discovered."</i></para>
///
/// <para>This file exists so that the two places a foreign pass can arrive — the drawer on a mess
/// floor and the <c>?badge=1</c> dev start — reach the same producer through the same list. Core owns the
/// question (<see cref="FoundPass.MintedElsewhere"/>); it does not own the answer to <i>which places exist
/// in this world</i>, and neither does either caller. If the cheat built its own roster it would be
/// playtesting a wallet the game cannot deal, which is the whole reason the audit found the WrongSite rung
/// reachable only from a cheat in the first place.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>
    /// <b>EVERY GROUND IN THIS WORLD A SITE COULD HAVE BEEN ISSUED BY</b> — the moons, off the ephemeris
    /// itself.
    ///
    /// <para><b>Not <c>ShuttleDestinationsInRange</c>,</b> which is the tempting one and is a fact about
    /// where the ship is parked this afternoon. A pass is issued by a BUILDING; which buildings exist cannot
    /// depend on the sky, or the same drawer on the same moon would hold a different site's pass depending on
    /// how the captain flew in. (The producer sorts the list before drawing from it, so order cannot decide
    /// either — but membership would have, and no sort fixes that.)</para>
    ///
    /// <para>Landability is <see cref="ShuttleExcursion.IsLandableSurface"/>'s answer and not a second one
    /// composed here, and a wreck's own body id is not a ground at all.</para>
    /// </summary>
    private List<string> TheGroundsThisWorldHas()
    {
        var sites = new List<string>();
        if (_ephemeris is null)
        {
            return sites;
        }

        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (ShuttleExcursion.IsLandableSurface(body.Kind)
                && !Derelict.TryParseWreckId(body.Id, out _))
            {
                sites.Add(body.Id);
            }
        }
        return sites;
    }

    /// <summary>#804 · <b>THE ONE MINT.</b> A pass issued somewhere that is not <paramref name="hereBodyId"/>,
    /// or null when this world has nowhere else to have issued one. Both the drawer and the dev start call
    /// this and nothing else, so the path a tester walks and the path a captain walks cannot drift.</summary>
    private Satchel.Item? AFalseIdFoundAt(string hereBodyId, ulong seed) =>
        FoundPass.MintedElsewhere(hereBodyId, TheGroundsThisWorldHas(), seed);

    /// <summary>
    /// #804 · <b>TAKE IT.</b> True when this room was the drawer and the press has been answered — the room
    /// searched, or the pocket's refusal said — so the search verb returns and the haul path is never
    /// reached. False in every other room, which is nearly all of them.
    ///
    /// <para><b>It lives here rather than in the search verb</b>, the way <c>TakeTheBlackOpsKey</c> does, and
    /// that is a law rather than tidiness: <c>Map.Surface.Hive.cs</c> may not add to the satchel and may not
    /// strike a room off, because every HAUL has to reach both through #615's KEEP/LEAVE decision. A pass is
    /// not a haul — the wallet has no ceiling and nothing to weigh, so there is nothing to decide about —
    /// and putting its add in that file would quietly repeal the ordering law for the sheets that do.</para>
    ///
    /// <para>#678's law is kept even so: a pocket that refuses does not consume the find. The wallet cannot
    /// fill today, so this is the line that stops a pass being destroyed by the act of looking at it the day
    /// it grows a ceiling — the same insurance the Key's own arm carries.</para>
    /// </summary>
    private bool TheDrawerHandsOverAFalseId(SurfaceExcursion ex, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        if (!FoundPass.IsHere(ex.Stop.Body.Id, ex.Floor, roomIndex)
            || AFalseIdFoundAt(
                   ex.Stop.Body.Id,
                   DiceRule.Seed($"false-id:whose:{ex.Stop.Body.Id}:{ex.Floor}:{roomIndex}"))
               is not { } pass)
        {
            return false;
        }

        if (!Core.Satchel.CanTake(_satchel, pass))
        {
            ShowPulseMessage(UndergroundComplex.PocketFullLine);
            return true;
        }

        _satchel = [.. Core.Satchel.Add(_satchel, pass)];
        TheRoomHasBeenGoneThrough(ex, ex.Floor, roomIndex);
        RendererInterop.PlayCue("board");

        // #804's canon pass (Fable, 2026-09-06) · THE ONE SENTENCE, SAID ONCE, ON THE PULSE. This is the
        // moment the `FABLE: line needed` in FoundPass.Plate asked for — the paper going into the wallet —
        // and FoundPass.TakenLine is the whole of what is authored about it. Nothing is composed at this
        // seam: the constant is SAID, so the guard that reads it verbatim is reading the shipped words.
        //
        // The PLATE is the clerical half, and it goes straight to the book rather than to the pulse. Two
        // sentences in one breath would contest #693's single slot, and the site code would win nothing by
        // beating the sentence; filed, it keeps the captain's own record naming whose building this is
        // (#836's informed choice) — and it is what the row in the satchel wears for ever after, which is
        // why the sentence is not repeated there.
        ShowAndFile(FoundPass.TakenLine, PatrolBeat.BadgeGlyph);
        FileNote(FoundPass.Plate(pass), PatrolBeat.BadgeGlyph);

        RebuildSurfaceDeck();
        RequestVaultSave();
        return true;
    }
}
