namespace SpaceSails.Core;

/// <summary>
/// SplitMix64 PRNG. Integer math only, so the same seed yields the same sequence on every
/// runtime and platform — System.Random makes no such cross-version promise, and determinism
/// is law in Core. Good enough statistically for traffic generation; not for cryptography.
/// </summary>
public sealed class DeterministicRandom(ulong seed)
{
    private ulong _state = seed;

    public ulong NextUInt64()
    {
        ulong z = _state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>Uniform double in [0, 1) with 53 bits of precision.</summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
    public int NextInt(int minInclusive, int maxExclusive) =>
        minInclusive + (int)(NextUInt64() % (ulong)(maxExclusive - minInclusive));

    /// <summary>Uniform double in [min, max).</summary>
    public double NextDouble(double min, double max) => min + NextDouble() * (max - min);
}
