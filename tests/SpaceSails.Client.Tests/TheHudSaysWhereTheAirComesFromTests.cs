using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #612 · THE PLATE BY THE LIFT AND THE GAUGE ON THE SUIT SAY THE SAME THING, ON EVERY FLOOR.
///
/// <para>Owner, standing on a pressurised floor a hundred and fifty metres under a moon: <i>"I thought there
/// is air in the base?"</i> / <i>"where here does it say if I consume tanks or have air?"</i> — and the
/// answer was nowhere. The floor knew, the drain knew, and nothing on screen said so.</para>
///
/// <para>The fix put the answer in two places at once, which is the dangerous shape: a sign painted on the
/// wall and a chip on the instrument. So the law being guarded here is not "the sign exists" — it is
/// <b>they cannot disagree</b>. Two instruments contradicting each other about whether a captain can breathe
/// is worse than one instrument saying nothing, because the second one is trusted.</para>
///
/// <para>Walked over every floor of every clandestine site rather than one screenshot's worth, for the same
/// reason <see cref="YouCanWalkTheHiveTests"/> floods them: a look tells you about the floor you happened to
/// open.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheHudSaysWhereTheAirComesFromTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    /// <summary>The tone the renderer paints an air line in: 1 = you can breathe here, 2 = you cannot and
    /// the tank is running. Anything else is painted signage and says nothing about air.</summary>
    private const int ToneHeld = 1;
    private const int ToneRunning = 2;

    /// <summary>THE FLOOR'S OWN air line, picked out by what it SAYS rather than by its colour.
    ///
    /// <para>Tone alone would be the wrong handle and #608 is why: a refuge's own `🫁` sign is tone 1 too,
    /// and correctly so — it means "you can breathe here" and the word REFUGE says the "here" is that room
    /// and not this floor. The plate describes the LEVEL. So the level's line is found by being one of the
    /// sentences SuitAir has for a level, which is also a check in itself: a plate spelling its own words
    /// would not be found at all.</para></summary>
    private static (float X, float Y, string Text, float Px, int Tone)[] FloorAirLines(DeckPlan deck) =>
    [..
        deck.BigLabels.Where(b =>
            System.Enum.GetValues<SuitAir.Supply>().Any(s => SuitAir.PlateLine(s) == b.Text))
    ];

    [Fact]
    public void EveryFloorSaysWhetherYouAreBreathingItsAirOrYourOwn()
    {
        var bad = new List<string>();

        foreach (string body in Bodies)
        {
            // #592 · FloorsOf, not "−1 down to the depth": a site with an unlisted band has a GAP where
            // nothing was dug, and auditing floors that do not exist proves nothing about the ones that do.
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                DeckPlan deck = DeckFor(body, level);

                // What the SUIT will say while standing here — the sim's own predicate, asked exactly the
                // way Map.Surface's drain asks it of the floor.
                SuitAir.Supply floorAir = SuitAir.SourceOf(level, insideShelter: false, aboard: false);
                string expected = SuitAir.PlateLine(floorAir);
                int expectedTone = SuitAir.Drawing(floorAir) ? ToneRunning : ToneHeld;

                (float X, float Y, string Text, float Px, int Tone)[] airLines = FloorAirLines(deck);

                if (airLines.Length != 1)
                {
                    bad.Add($"  {body} B{-level}: {airLines.Length} atmosphere lines on the plate, wanted 1.");
                    continue;
                }

                (float _, float _, string text, float _, int tone) = airLines[0];
                if (text != expected)
                {
                    bad.Add($"  {body} B{-level}: plate says \"{text}\", the suit says \"{expected}\".");
                }
                if (tone != expectedTone)
                {
                    bad.Add($"  {body} B{-level}: plate painted tone {tone}, the suit is {floorAir} (tone {expectedTone}).");
                }
            }
        }

        Report(bad, "floors whose plate and whose suit disagree about the air");
    }

    [Fact]
    public void TheDepthAndTheDepartmentAreStillPAINT_NotAState()
    {
        // The tone is what the sign MEANS, and only the atmosphere line means a state. If the depth or the
        // department ever picked up a state tone they would be painted in the air colours, and a floor name
        // rendered in "you cannot breathe here" amber is the instrument lying with typography.
        var bad = new List<string>();

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                DeckPlan deck = DeckFor(body, level);
                string depth = UndergroundComplex.DepthPaint(level);
                string name = UndergroundComplex.NameOf(body, level);

                foreach ((float _, float _, string text, float _, int tone) in deck.BigLabels)
                {
                    if ((text == depth || text == name) && tone != 0)
                    {
                        bad.Add($"  {body} B{-level}: \"{text}\" is painted as a state (tone {tone}).");
                    }
                }

                // And the plate still carries all three lines — the depth, the department, and the air. A
                // dead floor also paints its refuge's own sign, which is a different label about a different
                // scope, so the count is at least three rather than exactly three.
                Assert.True(deck.BigLabels.Length >= 3,
                    $"{body} B{-level}: the plate lost a line ({deck.BigLabels.Length} big labels).");
            }
        }

        Report(bad, "signage painted as if it were a state");
    }

    [Fact]
    public void ThePLATEIsWhereTheCARIs()
    {
        // Owner, on the first cut of the depth sign: "Let's put the elevation next to the elevator... now it
        // is too far from it." The atmosphere line inherits that ruling — it is the answer to the same
        // where-am-I question, so it belongs in the same eye-line, not somewhere else on the floor.
        //
        // A line about the air, hung across the floor from the plate that carries the depth, would be a
        // fourth thing to go and look for.
        var bad = new List<string>();

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                DeckPlan deck = DeckFor(body, level);
                (float X, float Y, string Text, float Px, int Tone) air = FloorAirLines(deck).Single();
                (float X, float Y, string Text, float Px, int Tone) depth =
                    deck.BigLabels.Single(b => b.Text == UndergroundComplex.DepthPaint(level));

                if (air.X != depth.X)
                {
                    bad.Add($"  {body} B{-level}: the air line is off the depth's column ({air.X} vs {depth.X}).");
                }
                // Under the department, above the car's own plate: one ladder a captain reads downwards.
                if (air.Y >= depth.Y)
                {
                    bad.Add($"  {body} B{-level}: the air line sits above the depth ({air.Y} vs {depth.Y}).");
                }
            }
        }

        Report(bad, "plates whose air line has wandered off the lift");
    }

    private static void Report(List<string> bad, string what)
    {
        if (bad.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} {what}:");
        foreach (string line in bad.Take(40))
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }
}
