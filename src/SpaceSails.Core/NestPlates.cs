namespace SpaceSails.Core;

/// <summary>
/// #488 / #528 · THE NEST, BEFORE AND AFTER — the two plates for the one room on an infested hull that the
/// whole vacuum mechanic is about.
///
/// <para>The AFTER card has existed since #488 and is the house's type specimen for a reveal: you vent the
/// nest, you walk back in, and the room shows you what the vacuum left. Nothing announces it; the payoff is
/// earned by looking.</para>
///
/// <para>The BEFORE card did not exist — and its painting did. <c>art/vented-nest-intact.jpg</c> shipped in
/// <c>wwwroot/art</c> with nothing in the codebase pointing at it: the nest alive, grown up over the racks,
/// its mouths open. So the setup for the game's best payoff was missing while the picture of it sat on
/// disk. #380's law is exactly this — <i>an event must introduce its fiction one beat earlier</i> — and
/// "what the vacuum left" only lands as hard as it does if you saw what was there before.</para>
///
/// <para>Both texts live here rather than in the client because the after-card's copy was a literal in
/// <c>Map.Venting</c>, and a sentence built in the client is a sentence that can drift away from the sim
/// (#634's law). Neither says what the thing is. The game does not know either.</para>
/// </summary>
public static class NestPlates
{
    /// <summary>The nest as you first find it — raised once per hull, the first time the captain stands in
    /// the nest compartment while it is still infested and still holding air. Evidence only: the room, the
    /// growth, and one arithmetic fact about the doorway. Nothing about what made it.</summary>
    public static readonly RevealPlate Live = new(
        "🕷 THE NEST — WHAT GREW IN HERE",
        "art/vented-nest-intact.jpg",
        "It has grown up over the racks and taken the shape of them, pale and layered and dry, with mouths " +
        "in it that go back further than your lamp does. The deck in front of it has been worn smooth. " +
        "Nothing about this room was built; all of it was made, patiently, by something that was not in a " +
        "hurry and has not been disturbed in forty years.");

    /// <summary>The nest after the vacuum has finished with it. The title is completed with the compartment
    /// name by the caller, because the room is the subject of the sentence.</summary>
    public static readonly RevealPlate Dead = new(
        "WHAT THE VACUUM LEFT",
        "art/vented-nest-dead.jpg",
        "The nest is collapsed and hollow, frost-shattered off the racks, and nothing in it is intact. " +
        "Deep parallel gouges cross the deck toward a sealed hatch and stop there. Whatever spent forty " +
        "years working at that door is not working at it now.");

    /// <summary>The after-card's stamp for a named compartment — <c>"💨 DEEP HOLD — WHAT THE VACUUM LEFT"</c>.
    /// Built here so the two halves of the title cannot drift apart in two files.</summary>
    public static string DeadTitle(string compartment) => $"💨 {compartment} — {Dead.Title}";
}
