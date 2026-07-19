namespace SpaceSails.Core;

/// <summary>
/// #369 · Flavor for the static-charge vent (the Electric Universe hull-charge layer, M7).
/// The venting itself is set to automatic here — so every discharge rides a rotating pool of
/// cheeky one-liners in the house voice. Pure and deterministic: <see cref="LineFor"/> maps a
/// caller-supplied seed onto a fixed rotation, so the same vent always reads the same quip and
/// client (and any future server) never disagree. No randomness, no state of its own.
/// </summary>
public static class StaticCharge
{
    /// <summary>
    /// The static-charge quip pool. Owner's three seeds up top (lightly polished), then the
    /// comedy well he pointed at: feet on the plastic carpet, the doorknob dread, wool socks,
    /// and the friend's cat that leapt from a finger-to-nose spark. Cheeky, never gross.
    /// </summary>
    public static readonly string[] Lines =
    [
        "That tingle you felt just now? Maybe it wasn't the galley's special dish — maybe it was just some charge build-up.",
        "Your hair stands out a little less. You'll want hair products to keep that fluffy look from here on out.",
        "A charged stream of particles is jettisoned from the ship — you watch them glitter like magic dust into the depths of space.",
        "Somewhere a crewman shuffled across the plastic carpet in socks, and now the whole hull has opinions.",
        "The vent lets go with a soft crackle, like a doorknob you already knew was going to get you.",
        "Wool socks, dry air, and a metal ship: three ingredients, one small lightning. Vented, you're welcome.",
        "Remember the friend who booped his cat on the nose and got a spark? The cat left orbit. The ship just vents.",
        "Discharge complete. No cats were startled in the making of this vent — that we know of.",
        "The hull sheds its charge the way you'd flinch before touching a car door in winter. Bravely, and a little too late.",
        "Static gone. Feel free to high-five a shipmate now without launching them into the bulkhead.",
        "A faint blue whisper leaves the antennae. The ship exhales. You un-clench your teeth.",
        "That was the ship rubbing its socked feet one time too many. Grounded now — no zapping the quartermaster.",
        "Charge released to the void. It drifts off to go surprise someone else's cat, presumably.",
        "The hull's fluffed-up static settles like hair after you pull off a wool hat. Presentable again.",
    ];

    /// <summary>
    /// The quip for a given vent: a fixed rotation over <see cref="Lines"/>, deterministic in the
    /// seed. Negative seeds are folded back into range so any caller-counter is safe.
    /// </summary>
    public static string LineFor(int seed)
    {
        int i = ((seed % Lines.Length) + Lines.Length) % Lines.Length;
        return Lines[i];
    }
}
