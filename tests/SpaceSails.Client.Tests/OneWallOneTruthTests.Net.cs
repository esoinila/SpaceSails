using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// <b>THE NET, AND THE SWEEP</b> — the two ways <see cref="OneWallOneTruthTests"/> asks its one question of
/// a whole world rather than of a wall.
///
/// <para>What this part owns is the net over the seeded random field — on open ground, under the fog, and in
/// an explored chamber, every wall the body meets is a wall the eye is shown — and then the same question
/// asked of every site of every body the game ships: each collides with exactly the walls it lists, draws
/// every wall it makes solid, and only the ONE list gets to declare a wall invisible.</para>
/// </summary>
public sealed partial class OneWallOneTruthTests
{
    // ══ THE NET ══════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #442 · THE CONTROL: on open ground, every wall the body meets is a wall the eye is shown.
    ///
    /// <para>This one should be green on today's code and is here so the rest of the file cannot pass by
    /// examining nothing — if the pen, the projection or the field generator ever stops producing walls on
    /// the glass, this goes red before the interesting guards below get a chance to pass vacuously. It is
    /// the positive control the fog test is measured against.</para>
    /// </summary>
    [Fact]
    public void OnOpenGround_EveryWallTheBodyMeets_IsAWallTheEyeIsShown()
    {
        var bad = new List<string>();
        int checked_ = 0;

        for (int seed = 1; seed <= Seeds; seed++)
        {
            DeckPlan.Wall[] walls = WallField(seed);
            DeckPlan plan = PlanOf(walls);
            (List<Stroke> strokes, DeckView.Placement place) = Frame(plan, 0, 0);

            foreach (DeckPlan.Wall w in plan.Walls)
            {
                if (!OnTheGlass(place, w))
                {
                    continue;
                }
                checked_++;
                (bool boots, bool shamble, bool round, bool eye) = TheBodyAndTheGun(plan, w);
                bool drawn = Drawn(strokes, place, w);
                if (boots && shamble && round && eye && drawn)
                {
                    continue;
                }
                bad.Add($"  seed {seed} · ({w.X1:0.##}, {w.Y1:0.##})→({w.X2:0.##}, {w.Y2:0.##}): "
                        + Dissent(boots, shamble, round, eye, drawn));
            }
        }

        Assert.True(checked_ > 200, $"the sweep only interrogated {checked_} walls — the field went quiet");
        Fail(bad, "wall(s) on open ground where the five consumers do not agree");
    }

    /// <summary>
    /// #442 · <b>THE FOG MAY CHANGE THE PAINT OR THE PHYSICS — IT MAY NOT CHANGE ONLY ONE.</b>
    ///
    /// <para>Owner, live: <i>"See the invisible wall there now?"</i> This is that wall, generated rather
    /// than hunted for. The #371 chamber overlay is laid over the middle of a seeded field in state 0
    /// (unseen); every wall inside it is then asked of all five consumers. The four Core readers say
    /// STONE — the collision list has never heard of the fog — and the pen is asked whether it drew
    /// anything.</para>
    ///
    /// <para><b>Both halves are asserted deliberately.</b> "The pen drew it" on its own would pass on a
    /// world with no stone in the region at all, which is the known local bug class — a guard handed a
    /// world that cannot tell pass from fail. So the test first insists the four bodies really are stopped
    /// there, and only then that the eye was shown what stopped them.</para>
    ///
    /// <para><b>Proven RED on today's code</b> — see the class summary and the PR body for the table it
    /// printed before the fog rule was made structural.</para>
    /// </summary>
    [Fact]
    public void UnderTheFog_AWallThatStillStopsYou_IsStillDrawn()
    {
        var bad = new List<string>();
        int inside = 0;

        for (int seed = 1; seed <= Seeds; seed++)
        {
            DeckPlan.Wall[] walls = WallField(seed);
            DeckPlan plan = PlanOf(walls);

            // One still-unseen chamber over the heart of the field (state 0 — "nobody has looked in here").
            var fog = new List<(double X0, double Y0, double X1, double Y1, int State)>
            {
                (-FieldHalf / 2, -FieldHalf / 2, FieldHalf / 2, FieldHalf / 2, 0),
            };
            (List<Stroke> strokes, DeckView.Placement place) = Frame(plan, 0, 0, Hud(fog));

            foreach (DeckPlan.Wall w in plan.Walls)
            {
                double mx = (w.X1 + w.X2) / 2.0, my = (w.Y1 + w.Y2) / 2.0;
                bool inFog = mx >= -FieldHalf / 2 && mx <= FieldHalf / 2
                          && my >= -FieldHalf / 2 && my <= FieldHalf / 2;
                if (!inFog || !OnTheGlass(place, w))
                {
                    continue;
                }
                inside++;

                (bool boots, bool shamble, bool round, bool eye) = TheBodyAndTheGun(plan, w);
                bool drawn = Drawn(strokes, place, w);
                if (boots && shamble && round && eye && drawn)
                {
                    continue;
                }
                bad.Add($"  seed {seed} · in the unseen chamber, ({w.X1:0.##}, {w.Y1:0.##})→"
                        + $"({w.X2:0.##}, {w.Y2:0.##}): " + Dissent(boots, shamble, round, eye, drawn));
            }
        }

        Assert.True(inside > 40,
            $"only {inside} wall(s) fell inside the unseen chamber — the fog test is not being handed a "
            + "world that can tell pass from fail");
        Fail(bad, "wall(s) that the fog hid from the eye and not from the body");
    }

    /// <summary>#442 · …and the same for a chamber that HAS been walked but is out of sight (state 1). It
    /// draws dim rather than not at all today, so this is a second control: the dim path already gets the
    /// rule right, which is what makes the state-0 path's <c>continue</c> a bug rather than a design.
    /// </summary>
    [Fact]
    public void InAnExploredChamber_TheWallsAreStillDrawn()
    {
        var bad = new List<string>();
        int inside = 0;
        for (int seed = 1; seed <= Seeds; seed++)
        {
            DeckPlan plan = PlanOf(WallField(seed));
            var fog = new List<(double X0, double Y0, double X1, double Y1, int State)>
            {
                (-FieldHalf / 2, -FieldHalf / 2, FieldHalf / 2, FieldHalf / 2, 1),
            };
            (List<Stroke> strokes, DeckView.Placement place) = Frame(plan, 0, 0, Hud(fog));
            foreach (DeckPlan.Wall w in plan.Walls)
            {
                double mx = (w.X1 + w.X2) / 2.0, my = (w.Y1 + w.Y2) / 2.0;
                if (mx < -FieldHalf / 2 || mx > FieldHalf / 2 || my < -FieldHalf / 2 || my > FieldHalf / 2
                    || !OnTheGlass(place, w))
                {
                    continue;
                }
                inside++;
                if (!Drawn(strokes, place, w))
                {
                    bad.Add($"  seed {seed} · ({w.X1:0.##}, {w.Y1:0.##})→({w.X2:0.##}, {w.Y2:0.##}) "
                            + "is solid in an explored chamber and the pen laid nothing.");
                }
            }
        }
        Assert.True(inside > 40, $"only {inside} wall(s) fell inside the explored chamber");
        Fail(bad, "wall(s) an explored chamber made solid and did not draw");
    }

    private static DeckView.SurfaceHud Hud(
        IReadOnlyList<(double X0, double Y0, double X1, double Y1, int State)> fog) =>
        new(DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
            Blips: [], Cadence: 0, Readout: "", CacheMarks: [], Nerve: 100, NerveReadout: "",
            DarkRegions: fog);

    // ══ THE SWEEP — EVERY SITE OF EVERY BODY ═════════════════════════════════════════════════════════

    /// <summary>#585/#320 · Every landable body in the scenario. Mirrored from
    /// <see cref="EverySiteMeetsTheSpecTests"/>, which explains why it is a hand-kept list (the scenario is
    /// a wwwroot JSON the test host cannot reliably locate) and why a new moon must be added to it.
    /// </summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static IEnumerable<(string Body, LandingSite Site)> EverySite() =>
        Bodies.SelectMany(b => LandingSites.For(b).Select(s => (b, s)));

    private static DeckPlan DeckFor(string body, LandingSite site) =>
        MoonSurface.SurfaceDeck(body, body, [], 0, (_, _) => { }, site.LayoutSalt, site.Name);

    /// <summary>
    /// #442 · <b>THE TWO LISTS ARE ONE LIST, ON EVERY GROUND IN THE GAME.</b>
    ///
    /// <para><c>DeckPlan</c> derives <c>CollisionSegments</c> from <c>Walls</c> in its constructor and
    /// grows both together in <c>AppendRegion</c>, so the identity is structural — and this is the guard
    /// that keeps it structural, swept across every seeded site of every body rather than asserted once on
    /// a synthetic plan. Index-parallel and coordinate-identical: not "the same count", which a shuffle
    /// would pass.</para>
    /// </summary>
    [Fact]
    public void EverySiteOfEveryBody_CollidesWithExactlyTheWallsItLists()
    {
        var bad = new List<string>();
        int sites = 0, walls = 0;

        foreach ((string body, LandingSite site) in EverySite())
        {
            DeckPlan plan = DeckFor(body, site);
            sites++;
            if (plan.CollisionSegments.Length != plan.Walls.Length)
            {
                bad.Add($"  {body}/{site.Name}: {plan.Walls.Length} wall(s) and "
                        + $"{plan.CollisionSegments.Length} collision segment(s).");
                continue;
            }
            for (int i = 0; i < plan.Walls.Length; i++)
            {
                DeckPlan.Wall w = plan.Walls[i];
                SurfaceCollision.Segment s = plan.CollisionSegments[i];
                walls++;
                if (Math.Abs(s.X1 - w.X1) > 1e-9 || Math.Abs(s.Y1 - w.Y1) > 1e-9
                    || Math.Abs(s.X2 - w.X2) > 1e-9 || Math.Abs(s.Y2 - w.Y2) > 1e-9)
                {
                    bad.Add($"  {body}/{site.Name} wall {i}: drawn ({w.X1:0.##}, {w.Y1:0.##})→"
                            + $"({w.X2:0.##}, {w.Y2:0.##}) but collided ({s.X1:0.##}, {s.Y1:0.##})→"
                            + $"({s.X2:0.##}, {s.Y2:0.##}).");
                }
            }
        }

        Assert.True(sites >= 20, $"the sweep only saw {sites} site(s) — the site list went quiet");
        Assert.True(walls > 5000, $"the sweep only saw {walls} wall(s) — the grounds stopped generating");
        Fail(bad, "site(s) whose collision list is not its wall list");
    }

    /// <summary>
    /// #442 · <b>THE SWEEP: every site of every body draws every wall it makes solid.</b>
    ///
    /// <para>Owner: <i>"And test those on multiple landing site so they really match the, the graphics and
    /// barrier."</i> — with the site sweep raised to a hard criterion once #320 gave a body 2–4 grounds:
    /// <i>"Sweep every site of every landable body … Include site 0 explicitly."</i> Site 0 is the canon
    /// ground (Miranda's maze, Luna's mass-driver ruins) and is the first entry
    /// <see cref="LandingSites.For"/> hands back, so it is in here by construction.</para>
    ///
    /// <para>The one exemption is <see cref="DeckPlan.Wall.Unseen"/>, and it is exempt because it is
    /// <b>declared on the wall itself, in the same list</b> — the field's own envelope, which the owner
    /// asked never be advertised (<i>"if our space has limits for some technical reasons then let's not
    /// advertise it"</i>), and the interior hatching of a solid whose outline IS drawn (#649). That is the
    /// whole distinction #442 is about: a barrier may be invisible, but only by saying so in the one list —
    /// never because a second system decided not to paint it. <see cref="OnlyTheOneList_GetsToDeclareAWallInvisible"/>
    /// holds the exemption itself honest.</para>
    /// </summary>
    [Fact]
    public void EverySiteOfEveryBody_DrawsEveryWallItMakesSolid()
    {
        var bad = new List<string>();
        int sites = 0, onGlass = 0;

        foreach ((string body, LandingSite site) in EverySite())
        {
            DeckPlan plan = DeckFor(body, site);
            (List<Stroke> strokes, DeckView.Placement place) = Frame(plan, plan.SpawnX, plan.SpawnY);
            sites++;
            int here = 0;

            for (int i = 0; i < plan.Walls.Length; i++)
            {
                DeckPlan.Wall w = plan.Walls[i];
                if (w.Unseen || !OnTheGlass(place, w))
                {
                    continue;
                }
                onGlass++;
                here++;
                if (Drawn(strokes, place, w))
                {
                    continue;
                }
                bad.Add($"  {body}/{site.Name} wall {i}: solid from ({w.X1:0.##}, {w.Y1:0.##}) to "
                        + $"({w.X2:0.##}, {w.Y2:0.##}), on the glass, and the pen laid nothing.");
            }

            if (here == 0)
            {
                bad.Add($"  {body}/{site.Name}: not one wall was on the glass — this ground was not audited.");
            }
        }

        Assert.True(sites >= 20, $"the sweep only saw {sites} site(s) — the site list went quiet");
        Assert.True(onGlass > 300, $"only {onGlass} wall(s) were on the glass across the whole sweep");
        Fail(bad, "wall(s) a landing site makes solid and never draws");
    }

    /// <summary>
    /// #442 · <b>THE EXEMPTION, HELD HONEST.</b> A wall may be invisible only by declaring it in the one
    /// list — and every such declaration on every real ground must be one of the two the game means:
    ///
    /// <list type="bullet">
    /// <item>the FIELD'S OWN ENVELOPE, which stops you at the rim of the world and is deliberately never
    /// painted (owner: <i>"let's not advertise it, more like hide that fact"</i>); or</item>
    /// <item>the INTERIOR HATCHING OF A SOLID — strokes inside a mass whose outline the eye is shown, so
    /// the picture already says "you cannot be here" before the hatch ever stops anybody (#649).</item>
    /// </list>
    ///
    /// <para>A third kind would be an invisible wall in open ground, which is the bug. The test states it
    /// positively: an unseen wall's midpoint must lie either outside the field's walkable envelope or
    /// inside a drawn <see cref="DeckPlan.Structure"/> — the filled mass the pen paints.</para>
    /// </summary>
    [Fact]
    public void OnlyTheOneList_GetsToDeclareAWallInvisible()
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        var bad = new List<string>();
        int unseen = 0;

        foreach ((string body, LandingSite site) in EverySite())
        {
            DeckPlan plan = DeckFor(body, site);
            foreach (DeckPlan.Wall w in plan.Walls)
            {
                if (!w.Unseen)
                {
                    continue;
                }
                unseen++;
                double mx = (w.X1 + w.X2) / 2.0, my = (w.Y1 + w.Y2) / 2.0;

                // The rim of the world: at or beyond the field's own bound, in any direction.
                bool atTheRim = mx <= field.LeftX + 1 || mx >= field.RightX - 1
                             || my >= field.TopY - 1 || my <= field.BottomY + 1;

                // …or inside something the eye is shown as a filled mass.
                bool insideADrawnSolid = plan.Structures.Any(s =>
                    mx >= Math.Min(s.X0, s.X1) - 0.5 && mx <= Math.Max(s.X0, s.X1) + 0.5
                    && my >= Math.Min(s.Y0, s.Y1) - 0.5 && my <= Math.Max(s.Y0, s.Y1) + 0.5);

                if (!atTheRim && !insideADrawnSolid)
                {
                    bad.Add($"  {body}/{site.Name}: an UNSEEN wall at ({mx:0.##}, {my:0.##}) that is "
                            + "neither the field's rim nor inside a drawn solid — an invisible wall in the "
                            + "open.");
                }
            }
        }

        Assert.True(unseen > 0,
            "no ground declared a single invisible wall — either the sweep is empty or the idiom is gone "
            + "and this guard is now asserting nothing");
        Fail(bad, "invisible wall(s) that no drawn thing accounts for");
    }
}
