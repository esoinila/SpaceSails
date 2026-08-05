using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #709 · THE FIRST PEOPLE IN THE HIVE — and they are all outsiders, exactly like you.
///
/// <para>Owner, 2026-08-05: <i>"we should have people in the bar... we have cover story"</i> and, immediately
/// after, the ruling that shapes the whole thing: <i>"for now let's keep the people in B1."</i></para>
///
/// <h3>Why B1 only, and why that is the design rather than a limitation</h3>
///
/// <para>The Hive's abandoned tone is doing real work — stripped rooms, lights left on, nobody paying — and
/// staff on every floor would spend it. Confining them to the top pressurised floor buys three things at
/// once:</para>
///
/// <list type="bullet">
/// <item>The job-seeker cover (#618) acquires a <b>natural expiry</b>. It holds exactly as far as the floor
/// where an outsider plausibly belongs, which is the world's own shape answering "what blows the cover"
/// instead of a rule we invented.</item>
/// <item><b>Descent becomes the horror gradient.</b> Each floor down is quieter and the last person you saw
/// is further behind you. Corridor length was never going to do that; a population falling to zero does it
/// for free.</item>
/// <item>The empty floors below read as <b>absence</b> rather than as unfinished content. Once a captain has
/// seen this building with people in it, B7 is a floor somebody left.</item>
/// </list>
///
/// <h3>They are in the upper canteen because its own sign says they may be</h3>
///
/// <para>#707 stencilled that room <c>CANTEEN 1 · CARRIERS &amp; CONTRACTORS · NO PASS REQUIRED</c> before
/// anybody had thought about who sits in it, and it turns out to have decided the cast. The people here are
/// hauliers, fitters, agency temps and drivers — <b>outsiders with no more right to be in the building than
/// the captain has.</b> That is precisely why nobody asks anybody for a card at band 0, and it is what makes
/// the cover work: not because it is a good lie, but because <i>everyone else's is equally thin.</i></para>
///
/// <h3>The laws</h3>
///
/// <list type="number">
/// <item><b>Top pressurised floor only.</b> Never the staff mess deeper down (that room is pass-only and its
/// people are a different question), never a washroom, never anywhere else. The owner's ruling, enforced here
/// rather than in the renderer.</item>
/// <item><b>Seeded off the site's own id</b>, so a moon has the people it has — the same regulars on every
/// visit and in every session, like everything else down here.</item>
/// <item><b>Nobody explains anything.</b> §13.8 holds hardest in the one room where somebody could talk. The
/// talk is about freight, signatures, shifts, pay and the machines. Not one line says what the facility is
/// for, and the closest any of them comes is a remark about hiring that only becomes horrifying if the player
/// has assembled something the game never states.</item>
/// </list>
///
/// <para>Pure and world-blind: the client asks who is sitting down and draws them, and never keeps an opinion
/// of its own about whether a room has people in it.</para>
/// </summary>
public static class CanteenRegulars
{
    /// <summary>The glyph a regular's plate and their filed line both carry — a person, at console size.</summary>
    public const string Glyph = "◈";

    /// <summary>The most who will ever be in the room at once. Three is a canteen with people in it; six is a
    /// crowd, and a crowd in a clandestine basement is a different building than the one we are describing.
    /// It is additionally clamped by how many tables the room actually has (#707 places those).</summary>
    public const int MostAtOnce = 3;

    /// <summary>One authored regular. <see cref="Plate"/> is what stands over them in the room;
    /// <see cref="Line"/> is what they say when a captain stops at the table.</summary>
    /// <param name="Plate">Who they read as, at a glance, before anybody speaks.</param>
    /// <param name="Line">What they actually say. One breath, because a stranger in a canteen gets one.</param>
    public readonly record struct Character(string Plate, string Line);

    /// <summary>Somebody sitting at a table, placed.</summary>
    /// <param name="X">The table's centre, in the surface's own coordinates.</param>
    /// <param name="Y">The table's centre.</param>
    /// <param name="Plate">Who they read as.</param>
    /// <param name="Line">What they say.</param>
    public readonly record struct Seated(double X, double Y, string Plate, string Line);

    /// <summary>
    /// The authored cast. Every one of them is somebody with a boring, verifiable, entirely legitimate reason
    /// to be in this building — which is the whole point of the room and the whole point of the cover.
    ///
    /// <para><b>The register test</b>, inherited from #701: nobody here is interesting. They are tired, owed
    /// money, waiting on somebody else's paperwork, or eating. A regular who was mysterious would be a
    /// quest-giver with a hat on, and the moment one of them is worth talking to for plot reasons the room
    /// stops being cover and becomes a corridor with clues in it.</para>
    /// </summary>
    private static readonly Character[] Cast =
    [
        new("◈ A CARRIER, WAITING ON A SIGNATURE",
            "Third day sat here. They'll sign it when the man who signs it gets back from wherever he is."),

        new("◈ A FITTER, OFF A MAINTENANCE CONTRACT",
            "Number two pump's been singing since spring. I've written it up four times. It's still singing."),

        new("◈ AN AGENCY TEMP, FIRST WEEK",
            "They took my name at the door and put a different one on the rota. Said it's easier that way."),

        new("◈ A DRIVER, NOT SAYING WHO FOR",
            "I bring it to the hut and I put it down. What it is after that is somebody else's job."),

        new("◈ SOMEBODY EATING, SHIFT ENDED",
            "Don't take the stew. Take anything else."),

        new("◈ A WOMAN DOING INVOICES AT A TABLE",
            "Everything down here was bought as something else. My pallet jack is a soil sampler."),

        new("◈ A HAND WHO HAS BEEN HERE LONGER THAN THE CONTRACT SAID",
            "Six weeks, it said. That was a while ago now. The money still comes, so."),

        new("◈ A MAN AT THE MACHINES, HAVING NO LUCK",
            "It takes the card and it thinks about it and then it gives you the card back. Every time."),

        new("◈ A CONTRACTOR NOBODY HAS COME FOR",
            "They're always hiring. Nobody ever says what for, and the pay clears, so nobody asks."),

        new("◈ A QUIET ONE, FACING THE DOOR",
            "..."),
    ];

    /// <summary>How many authored regulars exist. Public so a guard can pin the catalog's size without
    /// reaching into it.</summary>
    public static int CastSize => Cast.Length;

    /// <summary>Every authored plate and line, for the canon grep. Nothing in this list may explain the Old
    /// Ones, and the guard that checks it walks THIS, so a line added tomorrow is checked tomorrow.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (Character c in Cast)
        {
            yield return c.Plate;
            yield return c.Line;
        }
    }

    /// <summary>
    /// Who is sitting in this amenity, if anybody is.
    ///
    /// <para>Empty for every room that is not the upper canteen on the site's top pressurised floor — which
    /// is the owner's B1 ruling, living in Core where a test can reach it rather than as an <c>if</c> in a
    /// renderer.</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor being built.</param>
    /// <param name="amenity">The room, as Core carved it (#707) — its tables are the seats.</param>
    public static IReadOnlyList<Seated> Sitting(
        string bodyId, int level, UndergroundComplex.Amenity amenity)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // The washroom and the deep staff mess get nobody. The mess is pass-only and the people who would be
        // in it are #618's question, not this one; the washroom is the one amenity nobody sits down in.
        if (amenity.Use != UndergroundComplex.Comfort.UpperCanteen)
        {
            return [];
        }

        // And only on the floor the owner put them on. UpperCanteen is only ever carved on the top
        // pressurised floor today, so this is belt and braces — deliberately, because the day somebody
        // carves a second canteen the B1 ruling must not quietly stop being true.
        if (UndergroundComplex.TopPressurisedFloor(bodyId) != level)
        {
            return [];
        }

        int seats = Math.Min(amenity.Tables.Count, MostAtOnce);
        if (seats <= 0)
        {
            return [];
        }

        // How many turned up. At least one — the owner asked for people in the bar, and an empty canteen is
        // a thing this building already has twenty floors of.
        ulong seed = DiceRule.Seed($"hive:canteen:{bodyId}");
        int here = DiceRule.Roll(seed, seats).Face;

        var sat = new List<Seated>(here);
        var usedTables = new List<int>(here);
        var usedCast = new List<int>(here);

        for (int i = 0; i < here; i++)
        {
            int table = PickUnused(
                DiceRule.Roll(DiceRule.Seed($"hive:canteen:table:{bodyId}:{i}"), amenity.Tables.Count).Face - 1,
                amenity.Tables.Count, usedTables);
            int who = PickUnused(
                DiceRule.Roll(DiceRule.Seed($"hive:canteen:who:{bodyId}:{i}"), Cast.Length).Face - 1,
                Cast.Length, usedCast);

            (double tx, double ty) = amenity.Tables[table];
            sat.Add(new Seated(tx, ty, Cast[who].Plate, Cast[who].Line));
        }

        return sat;
    }

    /// <summary>Take the rolled index, or the next free one after it. Two people on one chair and one person
    /// said twice are the same bug wearing different clothes, and a re-roll loop on a seeded die is how a
    /// generator stops being deterministic.</summary>
    private static int PickUnused(int wanted, int count, List<int> used)
    {
        for (int step = 0; step < count; step++)
        {
            int candidate = (wanted + step) % count;
            if (!used.Contains(candidate))
            {
                used.Add(candidate);
                return candidate;
            }
        }

        used.Add(wanted);
        return wanted;
    }
}
