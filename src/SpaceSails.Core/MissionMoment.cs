namespace SpaceSails.Core;

/// <summary>#238 · WHICH MEMBER OF THE FAMILY THIS IS. The event family #185 started has three members now
/// and one grammar; the kind is what a card, a cue and a guard branch on, never the prose.</summary>
public enum MissionMomentKind
{
    /// <summary>The scope found something that was not on the charts (#238 item 1).</summary>
    Reveal,

    /// <summary>A quest step that changed by PROXIMITY or AUTOMATION — nobody pressed anything
    /// (#238 item 2).</summary>
    Pickup,
}

/// <summary>
/// #238 · <b>THE MISSION EVENT, AT THE SMALLER SIZE.</b> The celebration's scout siblings: the moment the
/// scope resolves a hidden target, and the moment a step finishes itself while the captain was looking at
/// something else.
///
/// <para>Owner, mid-car-hunt: <i>"Is there a big pop-up when we find the car in the scans? It is a kind of
/// mission event like when we get paid?"</i> — There wasn't; the reveal was a quiet marker and a checklist
/// tick. And proven live a second time when he grabbed the roadster's wallet mid-coast and <b>had to ask
/// whether the loot was aboard</b>: the proximity pickup fired silently and only the Captain chip's next
/// line changed. His own rule of thumb out of that session: <i>"if the player can ask 'did that just
/// happen?', the game owed them a moment."</i></para>
///
/// <para><b>One payload, like <see cref="MissionCelebration"/> is one payload.</b> The pop-up, the ledger
/// receipt, the parrot and the tests all read the SAME record, so a beat cannot be told two ways. It is a
/// pure value — no ship, no ephemeris, no clock — which is what lets a Core test compose the whole event
/// shape out of a name and a bearing and know it is the shape the client will draw.</para>
///
/// <para><paramref name="ShowMeBodyId"/> is the map marker the <i>show me</i> press jumps the camera to, or
/// null when there is nothing to go and look at (a pickup happens at arm's length — the ship is already
/// there). It is an ID and never a name: the camera compares places, and a comparison on prose is the bug
/// this repo's compass lane was written against.</para>
/// </summary>
/// <param name="Kind">Reveal or pickup — see <see cref="MissionMomentKind"/>.</param>
/// <param name="Headline">The loud line, composed by <see cref="MissionMoments"/> and never typed twice.</param>
/// <param name="ParrotSquawk">Which squawk the bird gives, or null where the arc owns no line for this beat.
/// The KIND travels beside the words because the client's bubble, cue and cooldown are all driven off the
/// kind; a beat that set the bubble straight from a string would be that whole mechanism forked.</param>
/// <param name="ParrotLine">The same squawk's words, composed once here — <see cref="MissionCelebration"/>
/// carries its song the same way, so the card and the bubble cannot end up quoting two different lines.</param>
/// <param name="ShowMeBodyId">The body the <i>show me</i> press centres the map on, or null.</param>
public readonly record struct MissionMoment(
    MissionMomentKind Kind,
    string Headline,
    Parrot.Squawk? ParrotSquawk,
    string? ParrotLine,
    string? ShowMeBodyId);

/// <summary>#238 · Builds a <see cref="MissionMoment"/>, and owns every word one can say. Two templates and
/// a handful of constants: the family is ONE SHAPE parameterised by the target's name and a bearing phrase
/// the sim already computes, which is the whole of what makes the next hidden target — a secret station, a
/// #223 rumour cache when discovery scanning arrives — a caller rather than a feature.</summary>
public static class MissionMoments
{
    /// <summary>The reveal's glyph. The scope's own, already the one the wreck-reveal line wore.</summary>
    public const string RevealGlyph = "🔭";

    /// <summary>The pickup's glyph — the wallet's, already the one the fetch's receipt wore.</summary>
    public const string PickupGlyph = "💾";

    /// <summary>The press that jumps the map camera onto the new marker (owner's own label, #238,
    /// lower-case and verbatim).</summary>
    public const string ShowMeFace = "show me";

    /// <summary>What the way out says on a beat with nowhere to go. The house's plain one.</summary>
    public const string DismissFace = "Close";

    /// <summary>The Captain's-desk ledger section these receipts file under — the established
    /// glyph-plus-one-word idiom of "🛰 Autopilot" and "🏴 Plunder" beside it.</summary>
    public const string LedgerTitle = "🎯 Mission";

    /// <summary>#238 · <b>THE REVEAL HEADLINE.</b> Owner's example is the roadster
    /// (<i>"🔭 THERE SHE IS — the roadster, sunward of Mars"</i>) and his instruction is that the SAME beat
    /// serves any hidden-target reveal, so the example IS the template: the roadster's card is this method
    /// called with the roadster's name and the roadster's bearing phrase, with nothing hand-written in
    /// between. A secret station reveals through the identical line.</summary>
    public static string RevealHeadline(string name, string bearing) =>
        $"{RevealGlyph} THERE SHE IS — {name}, {bearing}";

    /// <summary>#238 · <b>THE PICKUP HEADLINE.</b> The wallet's is this with the owner's own four words for
    /// where it was (<see cref="Derelict.WalletBetweenTheSeats"/>); anything else names itself.</summary>
    public static string PickupHeadline(string item) => $"{PickupGlyph} GOT IT — {item}";

    /// <summary>Compose the reveal beat. <paramref name="bearing"/> comes from the sim — the phrase the tip,
    /// the ledger and the offer card already say about this body — and is never invented here: Core has no
    /// ephemeris and inventing geometry in a prose builder is how a sentence starts disagreeing with the
    /// map it is about.</summary>
    public static MissionMoment Reveal(
        string name, string bearing, string bodyId, Parrot.Squawk? parrot = null, int parrotCounter = 0) =>
        new(MissionMomentKind.Reveal, RevealHeadline(name, bearing),
            parrot, WordsFor(parrot, parrotCounter), bodyId);

    /// <summary>Compose the pickup beat. No <c>show me</c>: the thing is aboard and the ship is alongside
    /// it, so a camera jump would be a button that moves nothing.</summary>
    public static MissionMoment Pickup(
        string item, Parrot.Squawk? parrot = null, int parrotCounter = 0) =>
        new(MissionMomentKind.Pickup, PickupHeadline(item),
            parrot, WordsFor(parrot, parrotCounter), ShowMeBodyId: null);

    /// <summary>The bird's words for a beat, through the one channel that owns them — so the card and the
    /// speech bubble are the same sentence rather than two copies of it.</summary>
    private static string? WordsFor(Parrot.Squawk? parrot, int counter) =>
        parrot is { } kind ? Parrot.Line(kind, counter) : null;

    /// <summary>Every sentence this feature can put on a screen, for the audit that reads them all. The two
    /// headlines are templates, so they are swept as composed with their own canon arguments.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return RevealHeadline(Derelict.RoadsterRevealName, Derelict.RoadsterBearingPhrase);
        yield return PickupHeadline(Derelict.WalletBetweenTheSeats);
        yield return PickupHeadline(CompromisingChip.PickupItem);
        yield return ShowMeFace;
        yield return DismissFace;
        yield return LedgerTitle;
    }
}
