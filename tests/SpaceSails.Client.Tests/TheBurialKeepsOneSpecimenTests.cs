using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1063 · CLAUSE FOUR OF THE ERASURE, ON SCREEN — <b>one specimen is kept</b>.
///
/// <para>Owner's research, harvested into the issue: <i>a short stair down to a single old door, preserved for
/// display.</i> The world keeps one souvenir of what it will not admit existed, and nobody finds that
/// strange.</para>
///
/// <para>Core decides that there is one and where (<c>TheBurialTests</c>); this file is the other half, which
/// is the half #708's opening failure was about — <b>a flag nobody draws</b>. The leaf has to arrive in the
/// deck in the found band's own third idiom (<see cref="DeckPlan.Wall.IsSeamless"/>, which belongs to no
/// palette at all) on a floor that is otherwise entirely poured, and it has to arrive as a door that is drawn
/// shut AND a wall a body cannot cross — which is what a display piece that does not open IS.</para>
///
/// <para>The world these guards run in is DERIVED and never typed, off an id family no other suite asks
/// about, and it is put back in a <c>finally</c>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBurialKeepsOneSpecimenTests
{
    private const int Probes = 4000;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    /// <summary>Grounds with halls, derived off this file's own id family. Asserted to be a real population:
    /// an empty one would pass every negative law here for the wrong reason.</summary>
    private static List<string> Grounds()
    {
        var found = new List<string>();
        for (int i = 0; i < Probes && found.Count < 12; i++)
        {
            string body = $"burial-drawn-{i}";
            if (UndergroundComplex.HasFoundBand(body))
            {
                found.Add(body);
            }
        }
        Xunit.Assert.True(found.Count >= 12, $"only {found.Count} drawn burial grounds had halls.");
        return found;
    }

    private sealed class Buried : IDisposable
    {
        public Buried(string body) =>
            Burial.Install([body], [new DisclosureClock.Opening(body, 0)]);

        public void Dispose() => Burial.Install([], []);
    }

    /// <summary>
    /// #1063 · THE LEAF IS DRAWN, IT IS DRAWN IN THE THIRD IDIOM, AND IT IS DRAWN SHUT.
    ///
    /// <para><b>Reverts that reddened it:</b> the specimen block dropped from <c>HiveInterior.FloorDeck</c> —
    /// <i>"the listed bottom of a filled ground draws 0 seamless wall(s)"</i>; and the leaf emitted with
    /// <c>IsHull: true</c> — <i>"a seamless wall also claims a palette"</i>.</para>
    /// </summary>
    [Xunit.Fact]
    public void TheKeptDoorIsDrawnInTheHallsOwnMaterialAndDoesNotOpen()
    {
        var sb = new StringBuilder();
        int drawn = 0;

        foreach (string body in Grounds())
        {
            int bottom = UndergroundComplex.DepthOf(body);
            using var _ = new Buried(body);

            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                DeckPlan deck = DeckFor(body, level);
                DeckPlan.Wall[] seamless = [.. deck.Walls.Where(w => w.IsSeamless)];

                if (level != bottom)
                {
                    if (seamless.Length != 0)
                    {
                        sb.AppendLine($"  {body} B{-level}: {seamless.Length} seamless wall(s) on a floor "
                            + "that keeps no specimen.");
                    }
                    continue;
                }

                if (seamless.Length != 1)
                {
                    sb.AppendLine($"  {body} B{-level}: the listed bottom of a filled ground draws "
                        + $"{seamless.Length} seamless wall(s), and it keeps exactly one door.");
                    continue;
                }

                drawn++;
                DeckPlan.Wall leaf = seamless[0];

                // It takes no palette. A wall that were also hull or stone would say who built it, which is
                // the one thing this object must never say.
                if (leaf.IsHull || leaf.IsStone || leaf.IsWindow)
                {
                    sb.AppendLine($"  {body} B{-level}: the kept door claims a palette.");
                }

                // It is the segment Core published, and not one drawn from the same two corners a second
                // time — the mirrored-constant bug with a one-line head start.
                UndergroundComplex.Specimen kept =
                    UndergroundComplex.SpecimenOn(body, level, Field)!.Value;
                if (Math.Abs(leaf.X1 - kept.X1) > 1e-3 || Math.Abs(leaf.Y1 - kept.Y1) > 1e-3
                    || Math.Abs(leaf.X2 - kept.X2) > 1e-3 || Math.Abs(leaf.Y2 - kept.Y2) > 1e-3)
                {
                    sb.AppendLine($"  {body} B{-level}: the drawn leaf is not the published one.");
                }

                // And it is a door that does not open: drawn shut, on the very segment the wall is on, so
                // the picture and the collision field cannot come to two opinions about it.
                bool shut = deck.Doors.Any(d =>
                    d.Locked && Math.Abs(d.X1 - leaf.X1) < 1e-3 && Math.Abs(d.Y1 - leaf.Y1) < 1e-3
                    && Math.Abs(d.X2 - leaf.X2) < 1e-3 && Math.Abs(d.Y2 - leaf.Y2) < 1e-3);
                if (!shut)
                {
                    sb.AppendLine($"  {body} B{-level}: the kept door is not drawn as a door that is shut.");
                }
            }
        }

        Xunit.Assert.True(drawn >= 12, $"only {drawn} specimen(s) were drawn — this proves little.");
        Xunit.Assert.True(sb.Length == 0, "the kept specimen is drawn wrong:\n" + sb);
    }

    /// <summary>
    /// #1063 · THE VACUITY TWIN — in a world where nothing has been filled in, not one listed floor in the
    /// game draws a seamless wall, and the found band keeps the idiom entirely to itself.
    ///
    /// <para><b>Revert that reddened it:</b> <c>HasSpecimenOn</c> without its <c>IsFilled</c> clause —
    /// <i>"a ground nobody buried draws a kept door on burial-drawn-…"</i>.</para>
    /// </summary>
    [Xunit.Fact]
    public void WithNothingBuriedNoListedFloorDrawsTheThirdIdiom()
    {
        Xunit.Assert.Empty(Burial.Filled);

        int floors = 0;
        foreach (string body in Grounds())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.IsFound(body, level))
                {
                    continue;   // the galleries are the one place the idiom belongs
                }
                floors++;
                Xunit.Assert.DoesNotContain(DeckFor(body, level).Walls, w => w.IsSeamless);
            }
        }
        Xunit.Assert.True(floors > 40, $"only {floors} listed floor(s) in the sweep.");
    }
}
