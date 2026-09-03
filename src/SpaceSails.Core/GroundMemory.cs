using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #563 slice 2 · WHAT THE CAPTAIN CHANGED ON A GROUND, AND STILL FINDS CHANGED — the second half of the
/// treadmill's seed/state split, carried across visits instead of dying with the excursion.
///
/// <para>Owner, writing the structure generator's brief: <i>"we can just keep the seed and get any point on
/// the map re-calculated for use as we need it"</i> … <i>"Or we can keep stuff in mind more in detailed
/// form. As you see fit."</i> The split that answers him was named on #563 and slice 1 built the first half
/// of it:</para>
///
/// <list type="bullet">
/// <item><b>Terrain and structures: a pure function of the seed.</b> <see cref="SurfaceTiles"/> — generate a
/// tile, forget it, generate it again from its address alone, and the same bytes come back.</item>
/// <item><b>Anything the CAPTAIN changed: explicit state.</b> A forced hatch, an emptied locker, a wallet
/// somebody has already read. These cannot be recomputed from a seed <i>by definition</i>, and this is where
/// they are kept.</item>
/// </list>
///
/// <para><b>The failure this exists to stop</b> is the one the issue names and the one that would be
/// invisible: treating the second category as the first. Nothing crashes; the world quietly becomes
/// wallpaper, and a hut you shouldered open on your last trip is dogged again on this one — which reads to a
/// player as a save bug and is, in fact, exactly that. Slice 1 keyed all of it on the TILE (so forcing one
/// hut did not open every hut on the moon) but kept it on the excursion, so lifting off forgot the lot.</para>
///
/// <para><b>Why a set of strings and not a typed table.</b> The key is <c>(body, site, tile, what)</c>, and
/// every one of those is already a stable identity somewhere else in the game — the body id, the landing
/// site's layout salt, <see cref="SurfaceTiles.Address"/>. A string built from them is the same identity the
/// buried caches already survive on (<c>TreasureCache.Id</c>) and the same shape
/// <see cref="SurfaceOutpost.ConsoleId"/> already hands the client, so the vault section is one list of
/// strings, additive, and a file written before this simply lacks it and wakes with nothing marked — which
/// is the truth about a moon nobody has walked on yet.</para>
///
/// <para>Ordered on the way out, so two saves of the same world are the same file and a diff of a vault
/// means something.</para>
/// </summary>
public sealed class GroundMemory
{
    /// <summary>What a captain can do to a hut that the ground cannot work out for itself.</summary>
    public enum HutChange
    {
        /// <summary>The hatch was shouldered open. It stays open.</summary>
        Forced,

        /// <summary>The ammunition locker was lifted. It stays empty.</summary>
        Emptied,

        /// <summary>Somebody's effects have been read. They are still lying there; you have read them.</summary>
        Read,
    }

    private readonly HashSet<string> _marks = [];

    /// <summary>The key for one thing done to one hut. The tile address is always in it — a site is a
    /// lattice and "the site's hut" is not a thing any more (#563) — and so is the site salt, because a body
    /// offers several landing sites and every one of them rebuilds the same local coordinate frame.</summary>
    public static string HutKey(string bodyId, string siteSalt, SurfaceTiles.Address tile, HutChange what)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        return $"hut:{bodyId}:{siteSalt}:{tile.X}_{tile.Y}:{Word(what)}";
    }

    /// <summary>Is this already so?</summary>
    public bool Knows(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _marks.Contains(key);
    }

    /// <summary>Remember it. True if this is the first time — the caller's "did I just do this?" answer, so
    /// a press that changes nothing can stay quiet.</summary>
    public bool Remember(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _marks.Add(key);
    }

    /// <summary>Un-remember it. The one caller is a find the captain's pockets refused (#678's law: a find
    /// the satchel will not take is STILL LYING THERE), so the world and the sentence agree that nothing was
    /// consumed.</summary>
    public bool Forget(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _marks.Remove(key);
    }

    /// <summary>How many marks are held. Only the vault and the guards ask.</summary>
    public int Count => _marks.Count;

    /// <summary>Everything remembered, in a stable order.</summary>
    public IReadOnlyList<string> Stored => [.. _marks.OrderBy(m => m, StringComparer.Ordinal)];

    /// <summary>Rebuild from a vault's rows. Nulls and blanks are dropped rather than trusted — a tampered
    /// or truncated file must load as a captain who has done less, never as one who cannot load.</summary>
    public static GroundMemory Restore(IEnumerable<string>? stored)
    {
        var memory = new GroundMemory();
        foreach (string row in stored ?? [])
        {
            if (!string.IsNullOrWhiteSpace(row))
            {
                memory._marks.Add(row);
            }
        }
        return memory;
    }

    private static string Word(HutChange what) => what switch
    {
        HutChange.Forced => "forced",
        HutChange.Emptied => "emptied",
        _ => "read",
    };
}
