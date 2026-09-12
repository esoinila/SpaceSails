using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1063 · <b>SEALED ≠ FULL</b> — the one disappointment this arc spends on purpose.
///
/// <para>The issue, under the heading <i>Scully protection (mandatory)</i>: <i>"Every burial reads as
/// renovation to a reasonable person. And we SPEND one disappointment on purpose: at least once, the captain
/// forces a sealed thing early and it is exactly, boringly empty — sealed ≠ full — so the pattern never
/// hardens into proof."</i></para>
///
/// <h3>What the pattern was, before this file</h3>
/// <para>Every sealed door in this game pays. Force one and a chamber appends with a landmark over it and a
/// discovery cache in the middle of it, every time, on every site, for ever. That is a rule a player learns
/// in two presses, and once it is learned a seal stops being a question: <b>a closed door becomes evidence
/// that something is behind it</b>. Which is exactly the inference #672 forbids the world to hand out. The
/// horror of this arc is that the captain can never prove the set is a set; a mechanic that guarantees every
/// seal is full is the game quietly proving it for him.</para>
///
/// <h3>The shape of the spend</h3>
/// <list type="bullet">
/// <item><b>Exactly one, per captain-lifetime.</b> Not a rate and not a die per door — a rate is a second
/// pattern to learn ("about one in six are empty") and it would make emptiness ordinary, which is the
/// opposite of the point. One room, once, and then never again: the memory of having been wrong once is what
/// keeps the pattern from hardening, and a second one would start a new pattern of its own.</item>
/// <item><b>EARLY, before the burial threshold of its ground.</b> The disappointment is only protection if
/// it is spent before there is anything to protect: a captain who has already watched a ground get filled in
/// and then finds an empty recess reads the emptiness as part of the story. Forced on a ground nobody has
/// opened and nobody has buried, it is just a room that was closed for no reason, which is the whole
/// lesson.</item>
/// <item><b>A leaf, never a way on.</b> Only a door with nothing nested behind it
/// (<see cref="ExpeditionRegions.LeafDoorIds"/>) can be the empty one, so the spend costs a cache and never
/// costs a route.</item>
/// <item><b>Never the kept specimen.</b> #1082's preserved doorway on the listed bottom is a leaf that opens
/// on nothing and is uncaptioned, and it stays exactly as it is: it is not forced, it is not a seal, it says
/// nothing, and this file cannot reach it — the two live in different buildings and the only thing they
/// share is a silence.</item>
/// </list>
///
/// <h3>Which one, and how it stays the same one</h3>
/// <para><b>The choice is seeded and the spend is remembered, and those are two different facts.</b> Each
/// ground nominates one of its leaves off <see cref="DiceRule"/> — a pure function of the body id, so the
/// same world always nominates the same door and two captains comparing notes agree. The captain then spends
/// the disappointment on <b>the first nominated leaf he actually forces</b>, and the key of that one door is
/// written down (<see cref="Key"/>) and carried in the save. After that <see cref="WouldBeEmpty"/> answers no
/// everywhere for ever, and <see cref="IsSpentOn"/> — which reads the written-down key and nothing else — is
/// what every later replay of that site asks. So the empty room stays empty across a reload, a re-compose and
/// a revisit, and no second room can ever become one.</para>
///
/// <para><b>Scully law (#672).</b> The one line is the captain's own register, said once, and it explains
/// nothing: it offers the mundane reading itself (<i>"a room somebody closed because there was nothing in
/// it, which is a reason"</i>) and there is no second reading underneath it, because there genuinely is
/// nothing there. §8's reserved word does not appear and nothing here settles which reading of §10 is
/// true.</para>
///
/// <para>Pure and deterministic, like everything else in Core.</para>
/// </summary>
public static class EmptySeal
{
    /// <summary>#1063 · The note kind the book files it under — the <b>door</b> glyph the sealed doors
    /// already wear, deliberately and not a new one. It is a door, and it was forced, and that is all that
    /// happened: filing it under a mark of its own would be the book telling the captain the emptiness was
    /// significant, which is the one thing it must not do.</summary>
    public const string Glyph = "⚙";

    /// <summary>#1063 · <b>THE LINE, SAID ONCE, IN THE CAPTAIN'S REGISTER.</b> Authored (Fable, canon pass
    /// for slice 2), verbatim; no word of it is composed here and none may be added to it. The second
    /// sentence is the entire protection: it supplies the boring explanation itself, out loud, in the
    /// captain's own voice, so the player has heard the mundane reading from the one witness who has been
    /// arguing against it.</summary>
    public const string Line =
        "Sealed, and empty. A room somebody closed because there was nothing in it, which is a reason.";

    /// <summary>#1063 · The line as the screen and the book both take it, behind its kind. <b>Not new
    /// prose</b> and not a second authoring: it is the glyph and the authored sentence, the same composition
    /// <c>UndergroundComplex.MaintenanceLedgerLine</c> already ships in, written once here so the pulse the
    /// captain reads and the entry the book keeps cannot become two slightly different strings.</summary>
    public const string Said = Glyph + " " + Line;

    /// <summary>#1063 · How the one spent seal is written down: the ground and the door, joined by a
    /// character neither of them can contain (a door id is one of this file's own authored ids and a body id
    /// is not permitted a pipe anywhere else in the save either). One string, so the save carries one field
    /// and the answer to "was it this door" is a comparison rather than a parse.</summary>
    public static string Key(string bodyId, string doorId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(doorId);
        return $"{bodyId}|{doorId}";
    }

    /// <summary>#1063 · Which leaf THIS ground nominates, or null where the kind seals no leaf at all. A pure
    /// function of the body id: seeded, so the choice is a fact about the world rather than about the order
    /// somebody walked it, and stable, so a reload nominates what it nominated before.</summary>
    public static string? TheNominatedLeaf(ExpeditionSiteKind kind, string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        IReadOnlyList<string> leaves = ExpeditionRegions.LeafDoorIds(kind);
        if (leaves.Count == 0)
        {
            return null;
        }
        return leaves[(int)(DiceRule.Roll(DiceRule.Seed($"emptyseal:{bodyId}"), leaves.Count).Face - 1)];
    }

    /// <summary>#1063 · <b>IS THIS THE ONE?</b> — asked at the moment a seal gives, and the only place the
    /// disappointment can be spent.
    ///
    /// <para>Three conditions and every one of them is a refusal: it has not been spent
    /// (<paramref name="spentOn"/> is the key already written down, null while the captain still has it to
    /// spend), the ground is <b>before its burial threshold</b> — nobody has opened it and nobody has filled
    /// it in, so the emptiness cannot be read as part of a story — and this door is the leaf the ground
    /// nominates.</para></summary>
    /// <param name="spentOn">The key of the one seal this captain has already found empty, or null.</param>
    public static bool WouldBeEmpty(
        ExpeditionSiteKind kind, string bodyId, string doorId, string? spentOn)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(doorId);

        if (spentOn is not null)
        {
            return false;   // spent once, never repeated
        }
        if (Burial.IsFilled(bodyId) || Burial.WorksAreOn(bodyId))
        {
            return false;   // not early any more: this ground already has a story to be read into
        }
        return string.Equals(TheNominatedLeaf(kind, bodyId), doorId, StringComparison.Ordinal);
    }

    /// <summary>#1063 · Is this the seal the captain already found empty? The <b>only</b> question every
    /// later reading asks — the compose that replays a site on a revisit, the append that grows the live
    /// plan, the fan's own bounds — so the room a captain walks back into is the room he walked out of, and
    /// the written-down key is the single source of that truth.</summary>
    public static bool IsSpentOn(string? spentOn, string bodyId, string doorId) =>
        spentOn is not null && string.Equals(spentOn, Key(bodyId, doorId), StringComparison.Ordinal);

    /// <summary>
    /// #1063 · <b>WHAT AN EMPTY SEAL OPENS ON.</b> The same chamber, with everything taken out of it: no
    /// cache, no nested door, no landmark over it, and no bonus banked to the gig.
    ///
    /// <para><b>The walls are untouched, and that matters.</b> The recess is real ground — it appends, it
    /// collides, it is born dark and it lights when the captain looks into it, exactly like every other
    /// forced room. An empty seal that appended nothing at all would read as a bug or as a refusal; the beat
    /// only works if the captain walks in, stands in the middle of it, and there is simply nothing there.
    /// (This is also why the leaf restriction above is not optional: the far wall of a leaf chamber is
    /// solid, so the recess is closed and there is visibly nothing further.)</para>
    ///
    /// <para>The scheme label goes with the rest. It is the only string a region carries and it is a NAME —
    /// <i>THE SEALED LOCKER</i>, <i>THE SIDE VAULT</i> — and naming a room somebody closed because it was
    /// empty would be the house dressing a nothing as a something.</para>
    /// </summary>
    public static ExpeditionRegions.Region Hollow(in ExpeditionRegions.Region region) =>
        region with { Scheme = "", Landmarks = [], Consoles = [], DiscoveryBonus = 0 };

    /// <summary>#1063 · Every player-facing string this beat publishes — one, and it is authored. The same
    /// <c>AllProse</c> discipline every prose-bearing type in Core keeps, and the list the reserved-word
    /// sweep walks.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Line;
    }
}
