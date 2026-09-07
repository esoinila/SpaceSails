namespace SpaceSails.Core;

/// <summary>
/// #537 · AND SHE IS LONGER THAN HER ROOMS — the machinery space aft of the last bulkhead, the
/// bulkheads themselves, and the fills that make a wall a thickness rather than a line.
///
/// <para>Owner, on being told the side band was the fix: <i>"It would make sense that the walls that can
/// hold vacuum are not thin and all kinds of tech needs to exist on the ship somewhere"</i>, and then the
/// shortcut — <i>"That padding to every wall is a whole job in itself. Just make every ship longer. 😅"</i>
/// A ship is not a row of rooms with a skin painted on. <b>The compartments do not move</b>; only the
/// shell goes further.</para>
///
/// <para>Split out of <c>WreckLayout.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class WreckLayout
{
    /// <summary>
    /// #537 · AND SHE IS LONGER THAN HER ROOMS. Owner, on being told the side band was the fix: <i>"It would make
    /// sense that the walls that can hold vacuum are not thin and all kinds of tech needs to exist on the ship
    /// somewhere"</i>, and then the shortcut — <i>"That padding to every wall is a whole job in itself. Just make
    /// every ship longer. 😅"</i>
    ///
    /// <para>He is right on both counts, and they are the same point twice: a ship is not a row of rooms with a
    /// skin painted on. She is rooms, plus everything that makes the rooms work — plant, tankage, the drive, the
    /// runs between them — and until now her compartments went edge to edge and the drive lived nowhere at all.
    /// So the transom moves aft of the last bulkhead and the gap is MACHINERY SPACE: unassigned, unaudited, and
    /// exactly where a ship's tech actually is.</para>
    ///
    /// <para><b>The compartments do not move.</b> <see cref="AftX"/> stays the aft edge of ENGINEERING and REACTOR
    /// SPACES; only the shell goes further. Anything else would have re-cut eight rooms to buy one space.</para>
    /// </summary>
    public const float MachineryDepth = 8f;

    /// <summary>The transom — aft of the last bulkhead by a machinery space, not flush with it.</summary>
    public const float TransomX = AftX - MachineryDepth;

    /// <summary>
    /// #537 · AND HER INTERIOR BULKHEADS ARE NOT LINES EITHER. Owner, after the shielding band shipped, giving the
    /// reason he had wanted padding on the INSIDE walls all along: <i>"the reason I wanted padding on interior
    /// walls was to not make finding the hidden spaces too easy. Still a room with a wall to technical space is a
    /// good bet on large enough hiding space. 😎👍"</i>
    ///
    /// <para>He is right and it is the sharper half of the idea. A shielding band on the OUTSIDE ONLY is itself a
    /// tell: a captain learns in one boarding that hidden space is always outboard, never knocks anywhere else,
    /// and the deduction collapses into a reflex. Give every transverse bulkhead its own thin technical run and a
    /// void can be almost anywhere — so the clue has to be read rather than guessed at.</para>
    ///
    /// <para><b>And the heuristic survives, which is the good bit.</b> A bulkhead run is
    /// <see cref="BulkheadDepth"/> deep against the band's <see cref="ShieldingDepth"/>, so an outboard wall
    /// really is the better bet for anything BIG — a folded gun mount, a cold locker with somebody in it — while a
    /// bulkhead will take papers and a rack of keys and nothing else. Where a thing can be hidden is decided by
    /// what it is, which is exactly "a good bet" rather than a rule.</para>
    /// </summary>
    public const float BulkheadDepth = 1.2f;

    /// <summary>
    /// #537 · WHERE HER STRUCTURE IS, as filled rectangles — the shielding band and every bulkhead's technical
    /// run. Owner, looking at the deck after the padding shipped: <i>"we should cover those narrow spaces … all
    /// of them … if we can see into them from the hall then they don't hide anything."</i>
    ///
    /// <para>He is exactly right and it was a bad miss. The runs were drawn as two lines with the gap between
    /// them left BLACK — the same black as a room — so a captain could read every hiding place off the map
    /// without knocking on anything. The whole search collapses: the clue is redundant, the sounder is a
    /// formality, and the noise it costs buys nothing. A hidden space that is drawn as a space is not hidden.</para>
    ///
    /// <para>So the runs are FILLED, and they read as what they are: steel, tankage and pipework with a ship
    /// built round them. A void inside one looks exactly like every other stretch of it until somebody knocks —
    /// which is the entire mechanic, and it did not work until now.</para>
    /// </summary>
    public static IEnumerable<(float X0, float Y0, float X1, float Y1)> StructuralFills() =>
        StructuralFills(null);

    /// <summary>
    /// #537 slice 3 · …AND WHAT A CAPTAIN HAS ALREADY CUT INTO. The fill is the ship's own ignorance made
    /// visible: every run is drawn solid because a captain who has not knocked on it has no reason to think
    /// it is anything else. Once a plate has come out, the section behind it is space he has stood in, and
    /// the map draws it as space — the rest of the band stays hatched, because the rest of the band is still
    /// only a guess.
    ///
    /// <para>The band is split around the pocket rather than dropped: cutting one section of shielding does
    /// not tell a captain anything about the sixty frames either side of it, and a map that quietly opened
    /// the whole run would be the map knowing more than the man drawing it.</para>
    /// </summary>
    public static IEnumerable<(float X0, float Y0, float X1, float Y1)> StructuralFills(
        HullStowage.OpenVoid? opened)
    {
        // The shielding band, both sides, the length of the parallel middle body.
        foreach (bool top in new[] { true, false })
        {
            float y0 = top ? OuterTopY : BottomY;
            float y1 = top ? TopY : OuterBottomY;

            if (opened is not { } pocket || pocket.Top != top)
            {
                yield return (TransomX, y0, ShieldingForwardEnd, y1);
                continue;
            }

            // Aft of the pocket, then forward of it. Either stretch can be nothing at all when the void sits
            // hard against one end of the band, and a zero-width fill is a drawing bug rather than a cover.
            if (pocket.X0 > TransomX + 0.01)
            {
                yield return (TransomX, y0, (float)pocket.X0, y1);
            }
            if (pocket.X1 < ShieldingForwardEnd - 0.01)
            {
                yield return ((float)pocket.X1, y0, ShieldingForwardEnd, y1);
            }
        }

        // …and every interior bulkhead's own run.
        float half = BulkheadDepth / 2f;
        foreach (bool top in new[] { true, false })
        {
            float yIn = top ? -SpineHalfHeight : SpineHalfHeight;
            float yOut = top ? TopY : BottomY;
            foreach (float x in InteriorBulkheads(top))
            {
                yield return (x - half, System.Math.Min(yIn, yOut), x + half, System.Math.Max(yIn, yOut));
            }
        }
    }

    /// <summary>The transverse bulkhead positions that have a room on BOTH sides — the ones with a technical run
    /// inside them. The hull's own ends are not in here: they have the machinery space and the bow behind them.</summary>
    public static IEnumerable<float> InteriorBulkheads(bool top)
    {
        var seen = new HashSet<float>();
        var ends = new HashSet<float> { AftX, BowX - 6 };

        foreach ((string _, float x0, float x1, bool isTop) in Compartments)
        {
            if (isTop != top)
            {
                continue;
            }
            foreach (float x in new[] { x0, x1 })
            {
                if (!ends.Contains(x) && seen.Add(x))
                {
                    yield return x;
                }
            }
        }
    }
}
