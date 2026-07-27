namespace SpaceSails.Core;

/// <summary>
/// #456 · THEY HAVE TO FIND OUT YOU ARE THERE. Owner, live 2026-07-27, on why the now-unleashed pack is
/// still fair: <i>"the thing that balances is that the reevers do not have perfect knowledge that you are
/// there in the first place. I think they can hear digging etc loud noises, but generally they have to spot
/// you by hearing or by seeing before they give chase. So when they are initially behind obstructions except
/// maybe one or two they do not participate in chasing you if they don't know where you are."</i>
///
/// <para>#453 took the leash off — every Old One can now run you to the crew-only door. What keeps that
/// honest is not a fence but IGNORANCE: sight is already blocked by stone (#324) and an unseen captain is
/// left alone (#446, the owner's feature). This adds the other half of knowing — the ear.</para>
///
/// <para>Sound is what betrays you, and it is a CHOICE you make: standing still and quiet is genuinely
/// quiet, but you did not come down here to stand still. A shovel is loud, and your own guns are louder.
/// Hearing does NOT care about walls — that is the point of it. Stone hides you from eyes, never from ears,
/// so a wall buys you sight-cover and nothing else while you are digging behind it.</para>
///
/// <para>What a Reever gains from a noise is a PLACE, not a target: it learns where the sound came from and
/// goes to look. If you are still there when it arrives, it sees you the honest way (#436's observation roll
/// will make that arrival its own beat). Move after making noise and the pack converges on nothing.</para>
/// </summary>
public static class ReeverHearing
{
    /// <summary>A sound the ground can carry. Each kind is really a loudness.</summary>
    public enum Noise
    {
        /// <summary>Boots on regolith. Barely anything — you can cross a field unheard if you make no
        /// other sound, which is what makes sneaking to a cache a real option.</summary>
        Footfall,

        /// <summary>The shovel. Sustained, rhythmic, and the reason you are down here — the signature
        /// trade of the surface: the thing worth doing is the thing that calls them.</summary>
        Digging,

        /// <summary>A chest hitting the regolith, a hatch, a body. One sharp report.</summary>
        Clatter,

        /// <summary>A sentry's volley. The loudest thing on the moon — your own guns give you away, so
        /// bringing bots buys time at the price of being FOUND (#314's "buy time, never safety", earned).</summary>
        Gunfire,
    }

    /// <summary>How far each sound carries, in deck units. FLAGGED for the owner's tuning — these are the
    /// dial that decides how much the deep field participates.</summary>
    public static double RangeOf(Noise noise) => noise switch
    {
        Noise.Footfall => 4.0,    // a whisper: only something already on top of you
        Noise.Digging => 22.0,    // carries well across open ground — a dig is a announcement
        Noise.Clatter => 12.0,    // sharp but brief
        Noise.Gunfire => 34.0,    // half the field; the loudest thing you can do
        _ => 0.0,
    };

    /// <summary>Does an Old One <paramref name="range"/> deck-units from a <paramref name="noise"/> hear it?
    ///
    /// <para>Deliberately NOT wall-aware: sound goes round stone. A captain digging behind a monolith is
    /// hidden from every eye on the field and hidden from no ear at all — which is exactly the tension the
    /// owner is balancing with, since the maze otherwise made the deep entirely safe.</para></summary>
    public static bool Hears(double range, Noise noise) =>
        range >= 0 && !double.IsNaN(range) && range <= RangeOf(noise);
}
