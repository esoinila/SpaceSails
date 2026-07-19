namespace SpaceSails.Core;

/// <summary>
/// The captain's own justification for a self-poured drink (owner ruling 2026-07-19: "Captain (Haddock
/// cough) needs excuse to drink :-D"). Whenever the captain pours a tot for THEMSELVES — the galley
/// locker, the bar's house special, a pour off the "See the menu" panel (#365) — the notice line carries
/// a rotating, seeded EXCUSE: the captain's own declared reason, blustery sea-dog bluster in the house
/// voice. A shared drink with a contact needs no excuse (that one has company), and the med-bay pill
/// stays clinical.
///
/// HOMAGE, not reproduction (the owner's standing law): this is OUR captain in a sea-dog register that
/// nods to a certain barnacle-cursing skipper — original lines only, no borrowed text, names, or
/// likeness. Nothing mechanical rides on the line; it is pure flavour on the EXISTING pulse.
/// </summary>
public static class DrinkExcuses
{
    /// <summary>The shelf of excuses. Every line is the captain talking themselves into it — self-poured,
    /// self-justified. Kept unique and never blank (see the tests) so the pool always reads fresh.</summary>
    public static readonly IReadOnlyList<string> Lines = new[]
    {
        "Strictly medicinal — the motion tracker prescribed it, and I do not argue with an instrument.",
        "One does not insult the keep by refusing an honest pour. That way lies mutiny.",
        "The orbit's holding steady. A thing that good deserves a toast, and I am nothing if not grateful.",
        "It's for the nerve, blast it. The nerve wrote me a note, and the note said POUR.",
        "A captain never lets a glass stand lonely — bad for morale, worse for the glass, and frankly rude.",
        "Purely navigational. A steady hand wants steady ballast, and ballast is precisely what this is.",
        "The void is cold, the coffee's colder, and a captain must warm SOMETHING from the inside out.",
        "I drink to the crew's good health. That they aren't here to share it is their loss, not my fault.",
        "Doctor's orders — and as there's no doctor aboard, I am the doctor, and I have ordered it.",
        "It settles the gyros. My gyros. Every last gyro I've got, settled the moment the cork comes free.",
        "There's an old spacer's superstition that an untouched bottle brings ill luck. I take no chances.",
        "The log wants a clear head, and NOTHING clears a head like knowing where the next one's coming from.",
        "It's a celebration. Of what? Docked, undocked, a Tuesday — the calendar always coughs up a reason.",
        "A dram keeps the space-scurvy off. I read that somewhere reputable. Or meant to. Close enough.",
        "Ten thousand thundering meteors, a captain has earned this — and I'll not be lectured by a tin of pills.",
        "For the cold, the dark, and the long watch. Mostly the cold — but the dark does strengthen my case.",
    };

    /// <summary>Pick the captain's excuse for a pour from a caller's seed (sim-time salted upstream), so
    /// the same pour always speaks the same reason. Pure and deterministic — flavour only.</summary>
    public static string LineFor(ulong seed) => Lines[(int)(seed % (ulong)Lines.Count)];
}
