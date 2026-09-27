using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE ONE LADDER — the outcomes of showing a paper, whether the cover blew, and what happens.
///
/// <para>Split out of <c>WalletChoice.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class WalletChoice
{
    // ── THE ONE LADDER ────────────────────────────────────────────────────────────────────────────────

    /// <summary>How a read goes, as a FACT rather than as a sentence. The card's prose and the book's note
    /// are both composed off this, so the thing the captain is told and the thing their book keeps are one
    /// answer to one question.</summary>
    public enum Outcome
    {
        /// <summary>Nothing came out of the wallet at all.</summary>
        NothingShown = 0,

        /// <summary>He read it, and it was for here, and he walked on.</summary>
        Worked = 1,

        /// <summary>A pass, read properly, issued for a building that is not this one.</summary>
        WrongSite = 2,

        /// <summary>A real paper and real cover — for somewhere else. The chit, on a floor that is not the
        /// cage.</summary>
        WrongPaper = 3,

        /// <summary>#1149 · The inspector's card, on a floor and a watch that honour it — a floor carrying a
        /// refuge, or a site with an inspection on its roster (<see cref="Inspectorate.HonouredAt"/>). He
        /// reads it and walks on, and for the rest of the excursion the building's gates treat the captain as
        /// the inspection that is happening.</summary>
        Inspection = 4,

        /// <summary>#1149 · The same card, read properly, on a site that is not expecting anybody.
        /// <see cref="WrongSite"/>'s cousin: the paper is genuine and the man is unmoved, because the process
        /// is strict and nobody inspects unannounced.</summary>
        NoInspectionDue = 5,

        /// <summary>#605 · <b>THIS BUILDING'S OWN PASS, ON A FLOOR ITS TIER DOES NOT COVER.</b> The
        /// department ladder's own rung: the site code is right, the laminate is real, the face is yours —
        /// and what is printed under the site code is not what is painted on the wall behind you
        /// (<see cref="PatrolBeat.ThePassFitsTheFloor"/>). It is the rung a captain earns by walking a
        /// GENERAL HANDS pass onto a laboratory floor, and the one a found department pass answers.</summary>
        WrongDepartment = 6,
    }

    /// <summary>#605 · <b>DID COVER BLOW?</b> The one predicate, off the one ladder, so the card's arm, the
    /// escort, the pip and the line the book files can never come to different answers about the same read.
    /// Two rungs are a man walking on; every other rung is a man who is not satisfied.</summary>
    public static bool CoverBlew(Outcome how) => how is not (Outcome.Worked or Outcome.Inspection);

    /// <summary>
    /// WHAT THE MAN MAKES OF THE PAPER IN HIS HAND. The whole judgement, and it looks at exactly one paper —
    /// the one that was handed over.
    ///
    /// <para><b>It may never sort the wallet.</b> The read this replaced walked the satchel and answered with
    /// the best thing in it, which meant a captain who chose the bad paper was quietly saved by the sim. A
    /// guard reads what you give him.</para>
    /// </summary>
    /// <param name="bodyId">The site whose floor you are standing on.</param>
    /// <param name="level">#1149 · The floor you are standing on. It decides exactly one rung — the
    /// inspector's card, which is honoured on a floor that carries a refuge whatever the roster says — and it
    /// is REQUIRED rather than defaulted, because a caller that could quietly omit it would be a caller
    /// handed a world that cannot tell that rung from a refusal.</param>
    /// <param name="watch">#1149 · The frozen watch, for the site's inspection roster
    /// (<see cref="Inspectorate.InspectionIsDue"/>). Every other rung ignores it.</param>
    /// <param name="shown">The paper handed over, or null when nothing was.</param>
    public static Outcome WhatHappens(string bodyId, int level, long watch, Satchel.Item? shown)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        if (shown is not { } paper)
        {
            return Outcome.NothingShown;
        }

        if (paper.Kind == Satchel.Kind.Chit)
        {
            return Outcome.WrongPaper;
        }

        // #1149 · THE ONE PAPER THAT IS NOT ABOUT A BUILDING. Asked before the site code is read, because
        // there is no site code on it to read: the INSPECTORATE issues above the complexes, so the question
        // is not WHOSE building this is but whether this floor and this watch will honour an inspector.
        if (Inspectorate.IsTheCard(paper))
        {
            return Inspectorate.HonouredAt(bodyId, level, watch)
                ? Outcome.Inspection
                : Outcome.NoInspectionDue;
        }

        if (paper.Kind != Satchel.Kind.Badge || PatrolBeat.SiteOfBadge(paper.Id) is not { Length: > 0 } site)
        {
            // Something that is not a paper this floor has ever heard of. It is the same rung as an empty
            // hand, and it is the honest one: the sentence is about what he can make of it, not about what
            // you are carrying.
            return Outcome.NothingShown;
        }

        if (!string.Equals(site, bodyId, StringComparison.Ordinal))
        {
            return Outcome.WrongSite;
        }

        // #605 · …AND THEN THE DEPARTMENT LADDER, which is the only thing this rung gained. The site code
        // is right, so he is holding a pass this building issued — and now the thing a man reading a card
        // at arm's length can do that nobody across a floor can: he looks at what is printed under it, and
        // at the plate on the wall behind you. One question, asked of PatrolBeat, so the tier's rule lives
        // beside the badge rather than inside the wallet.
        return PatrolBeat.ThePassFitsTheFloor(bodyId, level, PatrolBeat.TierOfBadge(paper.Id))
            ? Outcome.Worked
            : Outcome.WrongDepartment;
    }
}
