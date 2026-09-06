using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #519 · WHO IS ACTUALLY IN THE ROOM AND WHERE THEY ARE SAT — the cast dealt onto a canteen's benches
/// for one watch, in table order so the answer is stable, plus the lookup that turns a plate back into
/// the character behind it. It reads the registers in <c>CanteenRegulars.cs</c> and decides nothing about
/// who the cast is. Split out of that file under #251 with no member renamed, re-scoped or re-ordered.
/// </summary>
public static partial class CanteenRegulars
{
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
    /// <param name="watch">Which shift this is — <see cref="Interior.PatronRota.WatchIndex"/> of the sim
    /// clock, and deliberately a WATCH rather than a raw time. See the class docs.</param>
    /// <param name="stoodUp">#731 · The tops whose person has GOT UP AND WALKED OFF this watch, by table
    /// ordinal. See <see cref="Tables"/> for why this exists and why it is not a second rota.</param>
    /// <param name="cameIn">#731 · …and the other direction: who has WALKED IN off the oncoming rota this
    /// watch and where they sat down. See <see cref="Tables"/>.</param>
    public static IReadOnlyList<Seated> Sitting(
        string bodyId, int level, UndergroundComplex.Amenity amenity, long watch = 0,
        IReadOnlySet<int>? stoodUp = null,
        IReadOnlyDictionary<int, string>? cameIn = null)
    {
        var sat = new List<Seated>();
        foreach ((int table, int who) in Seating(bodyId, level, amenity, watch))
        {
            // A top somebody has walked in and taken is theirs, whatever the shift dealt it: the walker
            // reached that chair on the frame it was written, and one chair holds one person.
            if (stoodUp?.Contains(table) == true || cameIn?.ContainsKey(table) == true)
            {
                continue;
            }
            (double tx, double ty) = amenity.Tables[table];
            sat.Add(new Seated(tx, ty, Cast[who].Plate, Cast[who].Line));
        }

        // …and the newcomers, in TABLE ORDER and never in the caller's dictionary order — a list whose order
        // depends on the order somebody happened to arrive is a list two frames of one watch disagree about.
        foreach ((int table, string plate) in InTableOrder(cameIn, amenity.Tables.Count))
        {
            if (ByPlate(plate) is { } newcomer)
            {
                (double tx, double ty) = amenity.Tables[table];
                sat.Add(new Seated(tx, ty, newcomer.Plate, newcomer.Line));
            }
        }

        return sat;
    }

    /// <summary>#731 · The walked-in, by the room's own table ordinal, in that ordinal's order — and never
    /// the dictionary's own, which is an implementation detail of the caller's bookkeeping and not a fact
    /// about a room. Tops outside the room are dropped rather than trusted.</summary>
    private static List<(int Table, string Plate)> InTableOrder(
        IReadOnlyDictionary<int, string>? cameIn, int tops)
    {
        var ordered = new List<(int Table, string Plate)>();
        if (cameIn is null)
        {
            return ordered;
        }

        foreach ((int table, string plate) in cameIn)
        {
            if (table >= 0 && table < tops)
            {
                ordered.Add((table, plate));
            }
        }

        ordered.Sort(static (a, b) => a.Table.CompareTo(b.Table));
        return ordered;
    }

    /// <summary>#731 · One of the authored cast by their plate, or null for a plate this room did not write.
    ///
    /// <para>The lookup exists because a walker carries a PLATE and nothing else — <c>NpcWalk</c> is
    /// world-blind and holds no cast index — so the room seating somebody who has just walked in has to get
    /// back from the name over their head to the breath they give a captain who stops at the table. A second
    /// copy of the line, carried on the walker, would be one person filed under two sentences.</para></summary>
    public static Character? ByPlate(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        foreach (Character c in Cast)
        {
            if (string.Equals(c.Plate, plate, StringComparison.Ordinal))
            {
                return c;
            }
        }

        return null;
    }
}
