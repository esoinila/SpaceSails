using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Tests;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #563 · <b>CANON POINT 3 — the found halls have doors for the same reason: legacy.</b>
///
/// <para>Owner ruling, 2026-09-06: <i>the found halls have doors (the seamless leafs of #716/#1082) for the
/// same reason — legacy; that is why a leaf down there opens at all, and why nothing down there ever needed
/// to.</i> And the lane's own instruction: <b>nothing else changes down there.</b></para>
///
/// <para>So this file pins the state of the halls against the new law rather than adding to them, and it is
/// worth saying exactly what that state IS, because it is easy to assume otherwise:</para>
///
/// <list type="bullet">
/// <item><b>The galleries hang no leaf at all.</b> #677's rule, in its own words: <i>"a gallery has no door in
/// it, only a way through"</i> — an imported violet leaf down there would say, in the one channel the game
/// reserves for it, that somebody shipped it in and fitted it. The wall simply stops.</item>
/// <item><b>The one leaf drawn in the halls' own material is the kept specimen</b> (#1063/#1082) — a single
/// old door at the back of a recess on the listed bottom of a filled ground, in the third idiom, on a floor
/// that is otherwise entirely poured. Its own card is that it <i>does not open</i>.</item>
/// </list>
///
/// <para>Against the doors ruling that comes out as: the specimen is <b>a wall to them by choice</b>, refused
/// for the ruling's own reason (it is locked) and not by an accident of where its stone happens to lie. And
/// because <see cref="ReeverDoor.MayWork"/> asks nothing about ink, idiom, floor or who is thought to have
/// hung a leaf (pinned by the driven idiom case in
/// <see cref="AnOldOneOpensTheDoorTheSlowWayTests"/>), the day a seamless leaf IS hung unlocked it will open
/// the slow way with no new code and no new decision. That is what "legacy" buys.</para>
///
/// <para>Nothing shambles down here either (#585 clears the pack on every underground frame), so this is a
/// guard about the LAW's answer, not about a chase — which is exactly the shape "nothing else changes down
/// there" has.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
// Writes the burial register, which UndergroundComplex reads ambiently: a writer of a process-wide register
// runs alone (#1108).
[Collection(StopRegisterCollection.Name)]
public sealed class TheHallsOwnLeafIsAWallByChoiceTests
{
    private const int Probes = 4000;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    private static List<string> Grounds()
    {
        var found = new List<string>();
        for (int i = 0; i < Probes && found.Count < 12; i++)
        {
            string body = $"halls-leaf-{i}";
            if (UndergroundComplex.HasFoundBand(body))
            {
                found.Add(body);
            }
        }
        Xunit.Assert.True(found.Count >= 12, $"only {found.Count} ground(s) in the sweep had halls.");
        return found;
    }

    private sealed class Buried : IDisposable
    {
        public Buried(string body) =>
            Burial.Install([body], [new DisclosureClock.Opening(body, 0)]);

        public void Dispose() => Burial.Install([], []);
    }

    /// <summary>
    /// NOTHING ELSE CHANGES DOWN THERE — because there is nothing down there for the law to change. Every
    /// floor past the seam hangs zero leafs, which is #677's rule and is the reason canon point 3 is about
    /// why a leaf down there <i>would</i> open rather than about one that does.
    ///
    /// <para><b>Proven RED</b> by hanging one doorway on a found floor.</para>
    /// </summary>
    [Xunit.Fact]
    public void TheGalleriesHangNoLeafAtAll_SoTheDoorsRulingChangesNothingDownThere()
    {
        var sb = new StringBuilder();
        int floors = 0;

        foreach (string body in Grounds())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.IsFound(body, level))
                {
                    continue;
                }
                floors++;
                DeckPlan deck = DeckFor(body, level);
                if (deck.Doors.Length != 0)
                {
                    sb.AppendLine($"  {body} B{-level}: {deck.Doors.Length} leaf/leafs in the galleries — "
                        + "#677 says a way is a GAP, not a leaf.");
                }
            }
        }

        Xunit.Assert.True(floors >= 12,
            $"only {floors} found floor(s) were reached — a sweep that visits nothing passes this for the "
            + "wrong reason.");
        Xunit.Assert.True(sb.Length == 0, "Leafs where the halls have none:\n" + sb);
    }

    /// <summary>
    /// THE ONE LEAF DRAWN IN THEIR OWN MATERIAL IS A WALL TO THEM <b>BY CHOICE</b>. The kept specimen carries
    /// a seamless wall and a door on the very same segment; the door is LOCKED, so
    /// <see cref="ReeverDoor.MayWork"/> refuses it — and it must be refused for that reason and not merely
    /// because stone happens to lie across it, because "they never break a locked one" is the restraint the
    /// gramophone stands for, and a refusal that only worked while the wall was there would evaporate the day
    /// somebody drew the specimen differently.
    ///
    /// <para><b>Proven RED</b> by emitting the specimen leaf unlocked.</para>
    /// </summary>
    [Xunit.Fact]
    public void TheKeptSpecimenIsLocked_SoItIsNeverWorkedAndNeverBroken()
    {
        var sb = new StringBuilder();
        int kept = 0;

        foreach (string body in Grounds())
        {
            int bottom = UndergroundComplex.DepthOf(body);
            using var _ = new Buried(body);

            DeckPlan deck = DeckFor(body, bottom);
            DeckPlan.Wall[] seamless = [.. deck.Walls.Where(w => w.IsSeamless)];
            if (seamless.Length != 1)
            {
                sb.AppendLine($"  {body} B{-bottom}: {seamless.Length} seamless wall(s) — the filled ground "
                    + "keeps exactly one specimen, and this file is about that leaf.");
                continue;
            }

            DeckPlan.Wall face = seamless[0];
            DeckPlan.Door[] onIt = [.. deck.Doors.Where(d =>
                Math.Abs(d.X1 - face.X1) < 0.01 && Math.Abs(d.Y1 - face.Y1) < 0.01
                && Math.Abs(d.X2 - face.X2) < 0.01 && Math.Abs(d.Y2 - face.Y2) < 0.01)];
            if (onIt.Length != 1)
            {
                sb.AppendLine($"  {body} B{-bottom}: {onIt.Length} leaf/leafs on the specimen's own segment — "
                    + "the specimen is a wall AND a door drawn over it, and without the door there is no "
                    + "leaf here to rule on.");
                continue;
            }

            kept++;
            DeckPlan.Door leaf = onIt[0];

            if (!leaf.Locked)
            {
                sb.AppendLine($"  {body} B{-bottom}: the kept specimen is not locked — an Old One would haul "
                    + "it open, and its whole card is that it does not open.");
            }
            if (ReeverDoor.MayWork(leaf.Locked, leaf.Interlock != 0, deck.DoorwayIsWalledUp(leaf)))
            {
                sb.AppendLine($"  {body} B{-bottom}: the law would work the kept specimen.");
            }
            // …and the refusal is the LOCK's, not the wall's. Asked with the wall taken out of the question.
            if (ReeverDoor.MayWork(leaf.Locked, leaf.Interlock != 0, walledUp: false))
            {
                sb.AppendLine($"  {body} B{-bottom}: the specimen is only spared by the stone behind it. A "
                    + "locked leaf is a wall to them BY CHOICE — that is the ruling, and it must not depend "
                    + "on how the object happens to be drawn.");
            }
        }

        Xunit.Assert.True(kept >= 12, $"only {kept} specimen(s) were examined.");
        Xunit.Assert.True(sb.Length == 0, "The halls' own leaf:\n" + sb);
    }
}
