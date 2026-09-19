using System;

namespace SpaceSails.Core;

// Subject: #605's department ladder — which floors a tier belongs on, and what cover blew FOR (part of PatrolBeat).
public static partial class PatrolBeat
{
    // ── #605 · THE DEPARTMENT LADDER ──────────────────────────────────────────────────────────────────
    //
    // Owner, naming the mechanic: "camouflage — a badge means you belong." And the three things that fall
    // out of it, all three already law before this file existed: cover is a STATE and not a roll; it blows
    // for a REASON you can name afterwards; and the way out is the lift.
    //
    // THE LADDER IS ONE OBJECT READ AT TWO RANGES, and that is the whole of the design:
    //
    //   · AT A DISTANCE — the man across the floor, the gate that wants a face (#715), the site that will
    //     not put you on its books twice — a laminate is a laminate. ANY pass of THIS site passes, whatever
    //     is printed under the site code, because nobody at twenty paces is reading the second line.
    //     That rung is <see cref="BadgeHeld"/> and it is the only one a sightline can reach.
    //
    //   · AT A CONVERSATION — the pass is in his hand, and the department is painted on the wall behind
    //     you. The tier has to FIT THE FLOOR. That rung is <see cref="ThePassFitsTheFloor"/>, asked by the
    //     one ladder every read and every filed line is composed off (WalletChoice.WhatHappens).
    //
    // WHAT A FAILED CONVERSATION COSTS IS WHAT A REFUSAL ALREADY COST: the escort, the pip, the line in a
    // book. No new punishment, no dice, no meter — #618's own canon constraint, which the shipped design
    // refuses by name.

    /// <summary>
    /// #605 · <b>DOES A DEPARTMENT OWN THIS FLOOR?</b> The rule, stated ONCE and off the floor's own plate,
    /// so nobody ever has to keep a list of floors in step with a list of buildings.
    ///
    /// <para>Three answers are NO, and each is the same answer for the same reason — <b>there is no trade
    /// here to be the wrong trade for</b>:</para>
    ///
    /// <list type="bullet">
    /// <item><b>No plate at all</b> (<see cref="ChamberFitting.DepartmentOn"/> hands back null on the band
    /// nobody listed and the galleries nobody dug). Nobody wrote down whose floor it is, so nobody can say
    /// it is not yours. Nobody walks a round down there either (<see cref="IsPatrolled"/>), so in play this
    /// clause is reached by an audit and never by a captain — it is here so the function is total.</item>
    /// <item><b>A STORE</b> (<see cref="ChamberFitting.StoresOn"/>). A store floor is THINGS rather than
    /// work: hands carry, stack and fetch, and that is the entire establishment of the place.</item>
    /// <item><b><see cref="UndergroundComplex.UnmarkedPlate"/></b> — the one plate in the branch stock that
    /// is a listed floor with no purpose written on it.</item>
    /// </list>
    ///
    /// <para><b>And it is asked of the DEPARTMENT, never of the level</b>, which is what makes it survive a
    /// deep site: the plates cycle, so B3 and B11 are both LONG STORAGE and are both a general hand's floor
    /// for the same reason rather than by two numbers somebody wrote down.</para>
    /// </summary>
    public static bool ADepartmentOwnsTheFloor(string? department) =>
        department is { Length: > 0 }
        && !ChamberFitting.StoresOn(department)
        && !string.Equals(department, UndergroundComplex.UnmarkedPlate, StringComparison.Ordinal);

    /// <summary>
    /// #605 · <b>WHERE GENERAL HANDS BELONG</b> — the rule the audit asked for, and it is two clauses
    /// because the building has two kinds of floor a pass does not have to argue for.
    ///
    /// <para><b>The hall floors</b> (<see cref="UndergroundComplex.IsHallFloor"/>): the bar the surface
    /// walks into and the staff mess at the bottom of the listed band. The building says so itself, on a
    /// wall, in the only warm sign it owns — <c>NO PASS REQUIRED</c> — and it has said so since #590. A
    /// round that walked a hand out of the canteen would be the sim contradicting a plate a captain can
    /// read from the doorway, on the one floor #863 put a round on precisely because it is the most ordinary
    /// place in the building.</para>
    ///
    /// <para><b>And every floor no department owns</b> (<see cref="ADepartmentOwnsTheFloor"/>). Not a list
    /// of floors: the one question, asked of the plate.</para>
    /// </summary>
    public static bool GeneralHandsBelongOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return UndergroundComplex.IsHallFloor(bodyId, level)
               || !ADepartmentOwnsTheFloor(ChamberFitting.DepartmentOn(bodyId, level));
    }

    /// <summary>
    /// #605 · <b>THE CONVERSATION RUNG: DOES THIS PASS FIT THIS FLOOR?</b> One line, and it is a ladder
    /// rather than two rules — a tier passes on its own department's floor, <b>or</b> anywhere a general
    /// hand already belongs.
    ///
    /// <para>Which is why <see cref="BadgeTier"/> needs no arm of its own. GENERAL HANDS is not a
    /// department, so it never matches a plate and falls through to the second clause, which is exactly the
    /// rule: a hand belongs in the canteen, the stores and the floors nobody wrote a purpose on, and is
    /// walked out of a laboratory. A PLANT pass belongs on PLANT — and also in the canteen, because the
    /// canteen's own sign says nobody needs a pass there at all and a refusal under it would be the building
    /// arguing with its own wall.</para>
    ///
    /// <para><b>It says nothing about WHOSE building this is.</b> The site code is asked first and
    /// elsewhere (<see cref="WalletChoice.WhatHappens"/>): #1143's false ID is another site's real pass and
    /// it still fails on the site code, at whatever tier, exactly as it shipped.</para>
    /// </summary>
    /// <param name="bodyId">The site whose floor you are standing on.</param>
    /// <param name="level">The floor (negative; −2 is B2).</param>
    /// <param name="tier">What is printed under the site code (<see cref="TierOfBadge"/>).</param>
    public static bool ThePassFitsTheFloor(string bodyId, int level, string tier)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(tier);
        return string.Equals(tier, ChamberFitting.DepartmentOn(bodyId, level), StringComparison.Ordinal)
               || GeneralHandsBelongOn(bodyId, level);
    }

    /// <summary>#605 · Is this tier one of the departments this site's own signage prints? The producer's
    /// gate, so nothing can mint a pass for a department that does not exist in the building it names.
    /// GENERAL HANDS is deliberately NOT one: it is the tier the site ISSUES, and it is not a
    /// department.</summary>
    public static bool IsADepartmentOf(string bodyId, string tier)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(tier);
        foreach (string plate in UndergroundComplex.DepartmentsFor(bodyId))
        {
            if (string.Equals(plate, tier, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    // ── #605 · AND WHEN IT BLOWS, IT BLOWS FOR A REASON ───────────────────────────────────────────────
    //
    // Owner's second property, in his own words: "It blows for a REASON you can name afterwards. Never
    // randomly. ... The player must be able to say 'it was the door', or the whole mechanic reads as
    // arbitrary punishment."
    //
    // The reason already EXISTS — WalletChoice.Outcome is the one ladder and it has recorded which rung a
    // read landed on since #836, and WalletChoice.ReasonTag has been the captain's shorthand for it since
    // the same day. What was missing is that the reason was never FILED as a thing the book is about: the
    // note went in with no subject, so the one entry a captain would want the stack of — this building,
    // every time somebody stopped me in it — joined no thread at all.

    /// <summary>#605 · The authored line, and the only prose this lane adds. Said at the moment the
    /// department mismatch is caught, and nowhere else.
    ///
    /// <para>It is eleven words and none of them is a threat, which is the register the whole feature is
    /// tuned to (§13.8). He is not accusing you. He has read a real pass, issued by this building, to a real
    /// person — and the second read is the one where the paperwork stops balancing, because what is printed
    /// on it is not what is painted on the wall behind you. Nothing explains anything and nothing is
    /// decided in it: what happens next is the escort, which is what a refusal has always cost.</para>
    ///
    /// <para>Nothing is said when cover HOLDS. Cover is a state, and silence is the reward.</para></summary>
    public const string ReadsItTwiceLine =
        "He reads the pass twice. The second time he is reading your face.";

    /// <summary>#605/#741 · What the blow's entry in the field book is ABOUT — <b>the PLACE</b>, declared by
    /// the author that writes it and never worked out afterwards from the words.
    ///
    /// <para>The place and nothing else. There is no person here for the book to be about: the man on the
    /// rota is never named and never will be (#804's register), and minting a name for him would be the game
    /// detecting. What a captain will want the stack of, two floors down, is <i>this building</i> — every
    /// time cover went in it, under one heading, in the order it happened.</para></summary>
    public static string BlowSubjects(string place) =>
        CaseSubjects.Line(CaseSubjects.Place(place ?? string.Empty));
}
