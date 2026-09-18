using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1199 · <b>THE TWO COIN MACHINES IN THE OBSERVATION WALK'S GALLERY</b> — the binoculars at the rail and
/// the vending machine against the back wall, and every word either of them says.
///
/// <para>Owner, 2026-09-18, verbatim: <i>"maybe even a small vending machine cafeteria with a couple of
/// tables there… the station likes to get the tourist money"</i> and <i>"maybe one of those pay-coin-to-use
/// binoculars common on sightseeing spots with gen-AI image(s) — use with E … same for the vending
/// machine."</i></para>
///
/// <h3>Why this is a class of its own and not more of <see cref="ObservationWalk"/></h3>
/// <para>That type is the ROOM and the BEAT — where the walk is, how long the wait is, what the book writes
/// when nobody is there. These are two pieces of furniture with a price on them. Keeping them apart means
/// the beat's own prose sweep still lists exactly the four strings it always listed
/// (<see cref="ObservationWalk.AllProse"/>), and a machine that takes coins can never accidentally be read
/// as part of the absence. It is also not a partial of it: splitting a static class into partial files
/// re-orders its <c>static readonly</c> initialisers by file name, which is this repository's sixth named
/// bug class and cost a frame ledger to find.</para>
///
/// <h3>The canon fences</h3>
/// <para><b>Nothing here explains anything.</b> The tracks go out and do not come back and the card does not
/// say whose they are. The buried rail carries its lamps and the card does not say who buried it. The hatch
/// nobody lists has its lamp on and the card does not say what is behind it or who is paying for the
/// electricity. §8's reserved word is not here, the Old Ones are not named, the observables are not named,
/// and the machine has no opinion — a captain who looks through a pair of coin binoculars is not being told
/// a story, they are looking at a place that has one. That is the KOSH rule (owner, 2026-09-13) with a
/// coin slot bolted to it: spent rarely, spent privately, and made a show of.</para>
/// </summary>
public static class GalleryFixtures
{
    // ── WHAT A LOOK COSTS ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>HOW A COIN IS PRICED</b> — a tenth of what standing the room a round costs, here, at this
    /// berth, floored at a credit.
    ///
    /// <para>Derived and never typed, off the one price seam this game already keeps per haven:
    /// <c>Interior.Barkeep.RoundPrice</c>, which #352 already made climb with distance from the Sun. A
    /// machine bolted to a wall in the oldest port in the system charges what that port charges for
    /// everything else, and the day a berth's economy is re-argued its coin slots move with it. A literal
    /// price here would be a second opinion about how expensive this station is.</para>
    ///
    /// <para><b>A tenth, and the floor matters more than the tenth.</b> This has to stay pocket change:
    /// a fixture whose whole play is "press it twice and look at two things" must never be a purchase a
    /// captain weighs, and the only failure mode worth designing against is a machine that is somehow free.
    /// At Selene Gate — the one berth that has a gallery — a round is 45 cr, so a look is 4.</para>
    ///
    /// <para>The card says <i>two coins</i>, and that is not a contradiction: a coin is a slug the machine
    /// takes and a credit is what the purse counts in. The fiction is about the slot; the arithmetic is
    /// about the money.</para>
    /// </summary>
    public const int CoinIsATenthOfARound = 10;

    /// <summary>#1199 · …the price itself, asked of the haven's own round. One function, both fixtures: a
    /// binocular look and a cold drink out of a slot are the same handful of change, and two prices would be
    /// two numbers to keep in step for no reason anybody could name.</summary>
    public static int CoinCr(int roundPrice) => Math.Max(1, roundPrice / CoinIsATenthOfARound);

    /// <summary>#1199 · What a slot says when the purse cannot cover it — the game's ONE machine refusal
    /// (<see cref="CoinSlot.ShortLine"/>), never a second sentence written to say the same thing, and said
    /// off-plate as a pulse because a refusal is not a moment.</summary>
    public static string ShortLine(int priceCr) => $"{priceCr} cr. {CoinSlot.ShortLine}";

    // ── THE BINOCULARS ────────────────────────────────────────────────────────────────────────────────

    /// <summary>#1199 · What is stencilled on the binoculars. The fixture's own name and nothing else, in
    /// the plate grammar every other console on this deck wears.</summary>
    public const string BinocularsPlate = "🔭 COIN BINOCULARS";

    /// <summary>#1199 · The card's title, which is the fixture's plate. The room's own habit — the walk's
    /// card is titled with the room's name — because a title that announced what you were about to see would
    /// be the show telling the joke before the picture.</summary>
    public const string BinocularsTitle = BinocularsPlate;

    /// <summary>#1199 · WHICH WAY THE OPTICS ARE POINTED. Two, and the machine alternates between them —
    /// never rolls. See <see cref="LookAt"/> for why.</summary>
    public enum Look
    {
        /// <summary>Out through the west glass: the grey, the Earth, and the tracks.</summary>
        Out = 0,

        /// <summary>Down through the glass floor: the crater wall, the buried rail, the hatch.</summary>
        Down = 1,
    }

    /// <summary>
    /// #1199 · <b>WHICH LOOK THIS PRESS BUYS</b> — the first is out, the second is down, and after that it
    /// goes round again.
    ///
    /// <para>Deterministic and not random, and the difference is the whole design. A roll would mean a
    /// captain could pay twice and see the same thing twice, which turns a fixture that has two things to
    /// show into a slot machine; worse, it would mean the DROP — the one of the two that has anything in it
    /// worth noticing — could be missed by a player who pressed twice and got unlucky. Alternating says: put
    /// in the coins again and the optics swing. It is the machine's own behaviour rather than the world's
    /// opinion of you, which is also why it takes no seed, no body id and no clock.</para>
    /// </summary>
    /// <param name="looksTaken">How many times this captain has used these binoculars this visit.</param>
    public static Look LookAt(int looksTaken) =>
        int.IsEvenInteger(looksTaken) ? Look.Out : Look.Down;

    /// <summary>#1199 · The plate under the card, per look. The view OUT is the walk's own canvas family and
    /// the view DOWN is its own painting, because they are two different things seen through one pair of
    /// optics and one picture for both would be the fixture lying about what it does.</summary>
    public static string ArtFor(Look look) => look switch
    {
        Look.Down => BinocularsDownArtUrl,
        _ => BinocularsOutArtUrl,
    };

    /// <summary>#1199 · Earthrise over regolith, and one line of boot tracks going out that does not come
    /// back this way.</summary>
    public const string BinocularsOutArtUrl = "art/binoculars-earthrise.jpg";

    /// <summary>#1199 · Straight down through the glass floor: the crater wall falling into shadow, service
    /// lamps on a buried rail, and a sealed hatch with its lamp still lit.</summary>
    public const string BinocularsDownArtUrl = "art/binoculars-drop.jpg";

    /// <summary>
    /// #1199 · <b>THE VIEW OUT</b>, authored (Fable), verbatim.
    ///
    /// <para>Three observations and a fourth that is about the machine. The tracks are the only thing in it
    /// that is strange, and the sentence that carries them is the flattest one in the paragraph: they go
    /// out, they do not come back this way. No conclusion is offered and none is available — <i>this way</i>
    /// is doing real work, because the captain cannot see the rest of the moon from here and the card knows
    /// it. The eyepiece is dirty because nobody comes.</para>
    /// </summary>
    public const string BinocularsOutBody =
        "Two coins. The optics are older than the glass they look through. Out there: the grey, the Earth, "
        + "and one line of tracks going out that does not come back this way. Nobody has cleaned the "
        + "eyepiece in a long time.";

    /// <summary>
    /// #1199 · <b>THE VIEW DOWN</b>, authored (Fable), verbatim.
    ///
    /// <para>The same flat register pointed at the floor. A rail somebody buried and a hatch nobody lists,
    /// both still lit, and not one word about who or why. The last sentence is the captain's own, and it is
    /// the only thing in this whole fixture that says the look mattered: <i>you look for longer than the
    /// coins bought</i>.</para>
    /// </summary>
    public const string BinocularsDownBody =
        "Two coins, and the optics swing down. Under the floor the wall falls away into shadow; a rail "
        + "somebody buried still carries its lamps, and at the bottom a hatch nobody lists has its lamp on. "
        + "You look for longer than the coins bought.";

    /// <summary>#1199 · …and per look, in one place, so the card and any guard read one answer.</summary>
    public static string BodyFor(Look look) => look switch
    {
        Look.Down => BinocularsDownBody,
        _ => BinocularsOutBody,
    };

    /// <summary>
    /// #1199 · <b>WHAT THE BOOK WRITES ABOUT THE BINOCULARS</b> — once per captain, authored, verbatim.
    ///
    /// <para>A gist and not a transcript. The two things worth coming back to are the tracks and the hatch,
    /// and the entry is the shortest sentence that keeps both without joining them: they are two things seen
    /// through one pair of optics on one afternoon, and the book does not have an opinion about whether they
    /// are the same fact. THE BOOK NEVER LIES (#1063) and this is the whole of what was seen.</para>
    /// </summary>
    public const string BinocularsNoteLine =
        "the walk's binoculars — the tracks that go out, the hatch that keeps its lamp";

    /// <summary>#1199 · What the book files that note UNDER — the PLACE, minted here beside the sentence
    /// that names it, which is #741's law. The walk, and not the person: the binoculars are bolted to a
    /// room, and what they show is about the moon rather than about anybody in the bar.</summary>
    public static string BinocularsSubjects() =>
        CaseSubjects.Line(CaseSubjects.Place(ObservationWalk.Plate));

    // ── THE VENDING MACHINE ───────────────────────────────────────────────────────────────────────────

    /// <summary>#1199 · What is stencilled on a machine. Both of them wear it: they are the same machine
    /// twice, which is what a bank of vending machines is.</summary>
    public const string VendorPlate = "🥤 VENDING MACHINE";

    /// <summary>#1199 · The card's title, which is the fixture's plate — the binoculars' own argument.</summary>
    public const string VendorTitle = VendorPlate;

    /// <summary>#1199 · The plate under it: the cafeteria as it stands, two machines and two steel tables,
    /// the rail and the glass and the Earth beyond. Also the GALLERY's own backdrop
    /// (<see cref="CafeteriaArtUrl"/>) — one canvas, because what the captain is looking at on the card is
    /// the room he is standing in.</summary>
    public const string CafeteriaArtUrl = "art/gallery-cafeteria.jpg";

    /// <summary>
    /// #1199 · <b>WHAT COMES OUT OF THE MACHINE</b>, authored (Fable), verbatim.
    ///
    /// <para>One sentence of machine and one of the thing it gave you, and the last four words are the whole
    /// point of the room: <i>somebody stocked it</i>. Nobody is here, nobody has ever been here while the
    /// captain was, and the shelves are full. It explains nothing and it is not meant to be sinister; a
    /// station restocks its machines. It is simply the only evidence in the room that the station knows the
    /// room exists.</para>
    /// </summary>
    public const string VendorBody =
        "The machine takes the coin, thinks about it, and drops something wrapped in a language you don't "
        + "read. It is sweet and it is cold, and somebody stocked it.";

    // ── THE SWEEP ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#1199 · Every player-facing string these two fixtures publish, and there are no others. The
    /// <c>AllProse</c> discipline every prose-bearing type in Core keeps, and the list the reserved-word and
    /// pattern-word sweeps walk. The refusal is in it because a refusal is a sentence a player reads.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return BinocularsPlate;
        yield return BinocularsOutBody;
        yield return BinocularsDownBody;
        yield return BinocularsNoteLine;
        yield return VendorPlate;
        yield return VendorBody;
        yield return CoinSlot.ShortLine;
    }
}
