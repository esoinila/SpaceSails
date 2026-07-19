namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// The captains' roster (owner 2026-07-19: "a list of captains in the start with gen-ai profile images
// … then under those are their slots"). Every game thread (universe) is fronted by a CAPTAIN — a name
// and one of eight portraits — so two adjacent saved voyages can never be confused for one another.
//
// Both halves of the identity are SEEDED off the thread's GUID and DETERMINISTIC, so a captain is stable
// across reloads and across machines (no RNG, no clock — determinism law). The seed is a plain FNV-1a
// hash of the id string (NOT string.GetHashCode, which is randomized per process and would give a
// different captain every boot). The registry stores the chosen name + avatar so a later re-pick sticks
// (Evening wind #20: insurance issues a new captain); a thread saved before the roster existed simply
// re-derives its stable identity here from the id. Identity is DATA, never hardcoded per universe.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>Seeds a stable captain identity (name + avatar index) from a thread GUID. Pure and
/// deterministic — the same id always yields the same captain.</summary>
public static class Captains
{
    /// <summary>How many portraits the roster holds (<c>art/captain-1.jpg</c> … <c>art/captain-8.jpg</c>).</summary>
    public const int AvatarCount = 8;

    // House-flavor given names and surnames — spacer-pirate colour, deliberately gender-mixed so any name
    // sits under any of the eight portraits. Two independent hash streams pick from each list, so the name
    // space is large (Given × Surname) and collisions between two universes are rare.
    private static readonly string[] Given =
    [
        "Mabel", "Silas", "Junia", "Orla", "Cassius", "Wren", "Doro", "Ravi",
        "Isolde", "Thane", "Nella", "Bexley", "Sable", "Corvin", "Ines", "Halcyon",
    ];

    private static readonly string[] Surname =
    [
        "Vane", "Roark", "Quill", "Ashdown", "Marrow", "Calloway", "Vex", "Stormjar",
        "Holt", "Grave", "Fenwick", "Dross", "Salter", "Voss", "Ryecroft", "Delacroix",
    ];

    /// <summary>The captain's display name seeded from <paramref name="seed"/> (the thread GUID), e.g.
    /// "Captain Mabel Vane". Empty/blank seeds fall back to a stable "Captain Nemo".</summary>
    public static string Name(string? seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
        {
            return "Captain Nemo";
        }

        uint h = Fnv1a(seed);
        // Two decorrelated draws from the one hash: the low bits pick the given name, a re-mixed copy the
        // surname, so "Mabel" is not glued to one surname across the id space.
        string given = Given[h % (uint)Given.Length];
        string surname = Surname[(h ^ 0x9E3779B9u) % (uint)Surname.Length];
        return $"Captain {given} {surname}";
    }

    /// <summary>The avatar index (1..<see cref="AvatarCount"/>) for a seed — the <c>art/captain-N.jpg</c> to
    /// front this captain with. Uses a different bit band than <see cref="Name"/> so name and face vary
    /// independently.</summary>
    public static int AvatarIndex(string? seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
        {
            return 1;
        }

        uint h = Fnv1a(seed);
        return (int)((h >> 8) % AvatarCount) + 1; // 1..AvatarCount
    }

    /// <summary>Resolve a thread's captain to render: prefer the identity STORED on the row (a minted or
    /// re-picked captain), else re-derive a stable one from the id — so a pre-roster thread still names its
    /// captain and shows a consistent face.</summary>
    public static (string Name, int AvatarIndex) For(GameThreadInfo thread)
    {
        ArgumentNullException.ThrowIfNull(thread);
        string name = thread.CaptainName is { Length: > 0 } n ? n : Name(thread.Id);
        int avatar = thread.AvatarIndex is > 0 and <= AvatarCount ? thread.AvatarIndex : AvatarIndex(thread.Id);
        return (name, avatar);
    }

    // FNV-1a (32-bit) over the UTF-16 code units of the string — a small, stable, well-distributed hash
    // that (unlike string.GetHashCode) is identical every run and every machine, so a captain is permanent.
    private static uint Fnv1a(string s)
    {
        const uint Offset = 2166136261u;
        const uint Prime = 16777619u;
        uint hash = Offset;
        foreach (char c in s)
        {
            hash = (hash ^ c) * Prime;
        }

        return hash;
    }
}
