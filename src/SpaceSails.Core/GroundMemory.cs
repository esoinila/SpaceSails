using System;
using System.Collections.Generic;
using System.Globalization;
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

    // ── #316 law 1 · THE HUSKS TELL THE TALE ──────────────────────────────────────────────────────────
    //
    // Owner, live: "If we find already shot Reevers at a site then we know that somebody else has been there
    // to hide, pick-up, search etc :-D It serves as a clue."
    //
    // Which is a claim about PERSISTENCE, and the husks did not have it. They were a list on the excursion
    // record (SurfaceExcursion.Husks) written by the sentry volley and by the sweep team, and lift-off threw
    // it away with the visit: the footprints of a firefight died with the shuttle, so a captain could never
    // come back to a field and read what had happened in it — not his own stand, and certainly not somebody
    // else's. That is #563's own bug one layer out, and this is #563's own answer: it belongs on the SHIP's
    // ledger, keyed on the ground, written to the vault.
    //
    // A husk carries the SIM-TIME IT FELL, and that is the difference between this and every other mark in
    // here. The other marks are facts that do not age (a hatch is open or it is not); a husk's whole value as
    // a clue is HOW OLD IT IS, so the moment is in the key and the reading is a function of it.
    //
    // No tile field: the tile is DERIVED from the position (SurfaceTiles.At), so a husk cannot be recorded
    // on one tile and drawn on another — the fourth named bug class in this repo is exactly two answers to
    // one question.

    /// <summary>One downed Old One, where it fell and when. <see cref="FellAtSimTime"/> is the ship's sim
    /// clock in seconds — the same clock the vault, the heat decay and the hunters' fitting-out all run
    /// on.</summary>
    public readonly record struct Husk(double X, double Y, double FellAtSimTime)
    {
        /// <summary>Which tile of the lattice it is lying on. Derived, never stored: one position, one
        /// answer.</summary>
        public SurfaceTiles.Address Tile => SurfaceTiles.At(X, Y);
    }

    /// <summary>A sim day, in seconds — the unit the age bands below are measured in.</summary>
    public const double DaySeconds = 86400.0;

    /// <summary>Under this much sim time since it fell, a husk is FRESH.</summary>
    public const double FreshWithinSeconds = DaySeconds;

    /// <summary>At or over this much, it is OLD. A week is the owner's own boundary ("weeks old").</summary>
    public const double OldAfterSeconds = 7 * DaySeconds;

    /// <summary>The key for one husk. The position is rounded to a hundredth of a deck unit and the moment
    /// to the second before it goes in, so the same husk always spells the same row: a key built out of raw
    /// doubles would round-trip through the file and come back a DIFFERENT husk, which is a corpse that
    /// duplicates itself every time the game is saved.</summary>
    public static string HuskKey(string bodyId, string siteSalt, Husk husk)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        SurfaceTiles.Address tile = husk.Tile;
        return $"husk:{bodyId}:{siteSalt}:{tile.X}_{tile.Y}:{Fixed(husk.X)}_{Fixed(husk.Y)}"
             + $"@{husk.FellAtSimTime.ToString("F0", CultureInfo.InvariantCulture)}";
    }

    /// <summary>Read one back, if it is a husk on THIS ground. Core reads its own key — a transcription of
    /// the format in the client would be a second reader that agrees with the writer until the day one of
    /// them is edited. A row this build cannot parse is refused rather than guessed at, exactly as
    /// <see cref="Restore"/> drops one it cannot trust.</summary>
    public static bool TryReadHuskKey(string key, string bodyId, string siteSalt, out Husk husk)
    {
        husk = default;
        if (key is null || bodyId is null || siteSalt is null)
        {
            return false;
        }

        string prefix = $"husk:{bodyId}:{siteSalt}:";
        if (!key.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        // "<tx>_<ty>:<x>_<y>@<fell>" — the tile half is re-derived from the position and never trusted, so a
        // hand-edited file cannot put a husk on a tile it is not standing on.
        string[] halves = key[prefix.Length..].Split(':');
        if (halves.Length != 2)
        {
            return false;
        }

        string[] atAndWhen = halves[1].Split('@');
        if (atAndWhen.Length != 2)
        {
            return false;
        }

        string[] xy = atAndWhen[0].Split('_');
        if (xy.Length != 2
            || !double.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
            || !double.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
            || !double.TryParse(atAndWhen[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double fell))
        {
            return false;
        }

        husk = new Husk(x, y, fell);
        return true;
    }

    /// <summary>Every husk this ledger is holding for one landing site, in the ledger's own stable order.
    /// Built by asking Core to read its own rows, so what is drawn on a return visit is what was written on
    /// the last one and nothing else — there is no seeding here and no roll: a husk exists because something
    /// actually fell.</summary>
    public IReadOnlyList<Husk> HusksAt(string bodyId, string siteSalt)
    {
        var found = new List<Husk>();
        foreach (string row in Stored)
        {
            if (TryReadHuskKey(row, bodyId, siteSalt, out Husk husk))
            {
                found.Add(husk);
            }
        }
        return found;
    }

    // ── #316 law 1, SECOND HALF · THE MARKS THAT ARE NOT BODIES ───────────────────────────────────────
    //
    // Owner, live: "If we find already shot Reevers at a site then we know that somebody else has been there
    // to hide, pick-up, search etc" — and the issue spells out what the "etc" leaves lying: "husks near a
    // cache, disturbed ground at a dug spot, in dire cases an abandoned dry sentry bot".
    //
    // A husk is a body and carries only where and when. The other two are not bodies and are never loot: a
    // ROBBED HOLE where a ✗ used to be, and a SENTRY somebody walked away from. They age exactly as a husk
    // ages and off the same clock, so they are the same row shape with a word in it — one ledger, one vault
    // section, one reader. A separate store for them would be a second place to forget to save, which is the
    // bug the husks themselves were just dragged out of.

    /// <summary>A mark on the ground that is not a body. Two of them, and no more: the forensic vocabulary
    /// the issue itself names.</summary>
    public enum ScarKind
    {
        /// <summary>Disturbed ground at a dug spot — the hole a chest came out of.</summary>
        Pit,

        /// <summary>A sentry left standing where it ran dry, counter frozen at 00 (#314/#326).</summary>
        DryBot,
    }

    /// <summary>One scar, where it is and when it was made — the same (position, moment) pair a
    /// <see cref="Husk"/> carries, for the same reason: what a captain wants from a hole in the ground is
    /// HOW OLD IT IS.</summary>
    public readonly record struct Scar(ScarKind What, double X, double Y, double AtSimTime)
    {
        /// <summary>Which tile of the lattice it is on. Derived, never stored: one position, one answer.</summary>
        public SurfaceTiles.Address Tile => SurfaceTiles.At(X, Y);
    }

    /// <summary>The key for one scar. Same rounding law as <see cref="HuskKey"/> — a key built out of raw
    /// doubles round-trips through the file as a DIFFERENT scar, which is a hole that duplicates itself
    /// every time the game is saved.</summary>
    public static string ScarKey(string bodyId, string siteSalt, Scar scar)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        SurfaceTiles.Address tile = scar.Tile;
        return $"scar:{bodyId}:{siteSalt}:{Word(scar.What)}:{tile.X}_{tile.Y}"
             + $":{Fixed(scar.X)}_{Fixed(scar.Y)}@{scar.AtSimTime.ToString("F0", CultureInfo.InvariantCulture)}";
    }

    /// <summary>Read one back, if it is a scar on THIS ground. Core reads its own key, exactly as it reads
    /// its own husk key, and a row this build cannot parse is refused rather than guessed at.</summary>
    public static bool TryReadScarKey(string key, string bodyId, string siteSalt, out Scar scar)
    {
        scar = default;
        if (key is null || bodyId is null || siteSalt is null)
        {
            return false;
        }

        string prefix = $"scar:{bodyId}:{siteSalt}:";
        if (!key.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        // "<word>:<tx>_<ty>:<x>_<y>@<when>" — the tile half is re-derived from the position and never
        // trusted, so a hand-edited file cannot put a scar on a tile it is not on.
        string[] parts = key[prefix.Length..].Split(':');
        if (parts.Length != 3 || ScarFor(parts[0]) is not { } what)
        {
            return false;
        }

        string[] atAndWhen = parts[2].Split('@');
        if (atAndWhen.Length != 2)
        {
            return false;
        }

        string[] xy = atAndWhen[0].Split('_');
        if (xy.Length != 2
            || !double.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double sx)
            || !double.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double sy)
            || !double.TryParse(atAndWhen[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double at))
        {
            return false;
        }

        scar = new Scar(what, sx, sy, at);
        return true;
    }

    /// <summary>Every scar this ledger is holding for one landing site, in the ledger's own stable order.
    /// No seeding and no roll, exactly as <see cref="HusksAt"/>: a hole is here because something was
    /// actually dug out of it.</summary>
    public IReadOnlyList<Scar> ScarsAt(string bodyId, string siteSalt)
    {
        var found = new List<Scar>();
        foreach (string row in Stored)
        {
            if (TryReadScarKey(row, bodyId, siteSalt, out Scar scar))
            {
                found.Add(scar);
            }
        }
        return found;
    }

    /// <summary>
    /// WHAT A CAPTAIN WHO LOOKS CAN TELL, and it is the whole of #316 law 2: recency is legible. The two ends
    /// are the owner's own words — <i>"remains render with age-graded flavor in the house voice — 'still
    /// smoking' vs 'regolith-dusted, weeks old'"</i> — and the middle age is authored to sit between them
    /// (#316, 2026-09-03): the band where a captain can still tell somebody was here THIS trip, but not this
    /// hour. The dust has started and it is not yet weeks of it.
    ///
    /// <para>THREE BANDS AND NO SILENCE. Every husk answers now; the band is read off the SIM CLOCK against
    /// the moment in the ledger, so the sentence is a fact about the world rather than about the session.</para>
    /// </summary>
    public static string AgeLine(Husk husk, double nowSimTime) =>
        AgeLine(husk.FellAtSimTime, nowSimTime);

    /// <summary>The same three bands off any moment in the ledger — a husk's fall, a rival's dig. The
    /// #316 lane that made the marks did not author a fourth sentence and must not: dating a hole is the
    /// same question as dating a body, and this repo's third named bug class is two reporters of one
    /// truth.</summary>
    public static string AgeLine(double atSimTime, double nowSimTime)
    {
        double age = nowSimTime - atSimTime;
        if (age < FreshWithinSeconds)
        {
            return "Still smoking.";
        }

        return age >= OldAfterSeconds ? "Regolith-dusted. Weeks old." : "Dusted over. Days old.";
    }

    private static string Fixed(double v) =>
        Math.Round(v, 2).ToString("F2", CultureInfo.InvariantCulture);

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

    private static string Word(ScarKind what) => what == ScarKind.Pit ? "pit" : "drybot";

    /// <summary>The inverse of <see cref="Word(ScarKind)"/>. Null for a word this build does not know, so a
    /// file written by a later build loads as a captain who can see less rather than one who cannot
    /// load.</summary>
    private static ScarKind? ScarFor(string word) => word switch
    {
        "pit" => ScarKind.Pit,
        "drybot" => ScarKind.DryBot,
        _ => null,
    };
}
