using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #649 · THE MONOLITH SITE IS A STRANGE-THINGS-HAPPEN PLACE — and nothing out here ever says what it was.
///
/// <para>Owner's ruling, 2026-08-03, in his own reference: <b>Babylon 5</b>, Sheridan and the giants on the
/// playground — <i>"background puppeteers watching if their kids perform in the school play."</i> Awesome
/// and a little scary, Lovecraftian, <b>parental rather than predatory</b>. And per standing canon,
/// <i>"anything that happens near it is reported by the world in a way that stays deniable."</i></para>
///
/// <para>That last clause is a testable law and this is the test. It is the same one
/// <c>TheHiveTests.NothingDownHereEXPLAINSAnything</c> holds the underground to, and the same one the
/// monolith's own card has been held to since #586: the game may DESCRIBE and may not ACCOUNT FOR. A single
/// sentence anywhere in this feature that names an agent, states a purpose, or resolves what happened would
/// undo the whole thing, and it would do it quietly.</para>
/// </summary>
public sealed class NothingOutHereEverSaysWhatItWasTests
{
    private static IEnumerable<MonolithWatch.What> Everything() =>
        Enum.GetValues<MonolithWatch.What>();

    /// <summary>NOTHING IS NAMED, NOTHING IS ACCOUNTED FOR.
    ///
    /// <para>The forbidden list is in three parts. The first is <b>agency</b> — any word that turns a fact
    /// about the world into a subject with intentions. The second is <b>explanation</b> — the connectives
    /// that make a sentence into an account of itself. The third is <b>confirmation</b>: a sensor, a
    /// reading, a log that settles it. Any one of them and the inference stops being the horror.</para></summary>
    [Fact]
    public void NoLineNamesAnAgentExplainsItselfOrConfirmsAnything()
    {
        string[] agency =
        [
            "watcher", "watching", "watched", "presence", "entity", "creature", "being", "figure",
            "alien", "ancient one", "old ones did", "god", "they want", "they know", "they came",
            "it wants", "watching you", "someone is", "something is here", "aware of you", "chose",
        ];
        string[] explanation =
        [
            "because", "in order to", "which means", "that means", "the reason", "so that", "explains",
        ];
        string[] confirmation =
        [
            "the sensor confirms", "the reading", "scan shows", "the survey found", "it is a ",
            "turns out", "proves", "confirmed",
        ];

        foreach (MonolithWatch.What what in Everything())
        {
            string line = MonolithWatch.Line(what);
            foreach (string bad in agency.Concat(explanation).Concat(confirmation))
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>EVERY VARIANT IS A FACT ABOUT THE WORLD, AND IT IS WORTH THE WALK. A beat with a thin line is
    /// a beat that fires and is forgotten, which for something this rare is the same as not shipping it.</summary>
    [Fact]
    public void EveryVariantHasSomethingWorthStandingStillFor()
    {
        Assert.True(Everything().Count() >= 4, "too few variants — the rare thing becomes the same thing.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (MonolithWatch.What what in Everything())
        {
            string line = MonolithWatch.Line(what);
            Assert.True(line.Length > 140, $"{what} has no line worth standing still for.");
            Assert.True(seen.Add(line), $"{what} shares its line with another variant.");
        }
    }

    /// <summary>IT IS PARENTAL, NOT PREDATORY. The feel call, written down where a change to it is a
    /// decision somebody made rather than a number that drifted: this costs the captain NOTHING. The nerve
    /// model already prices the place at 24 — the biggest single fright in a captain's life — and a site
    /// that also bills you for standing in it is a site you learn to avoid, whatever the prose says.</summary>
    [Fact]
    public void ItNeverCostsTheCaptainAnything()
    {
        Assert.Equal(0.0, MonolithWatch.NerveCost);

        // And it is not a fright in the model's own terms either: whatever it ever costs must stay far below
        // the one-time sighting, or the place stops being awe and becomes attrition.
        Assert.True(MonolithWatch.NerveCost < NerveModel.MonolithSightShock / 4.0,
            "the watch costs a serious fraction of the monolith's own once-in-a-life shock — that is a predator.");
    }

    /// <summary>ONLY THIS GROUND. It is a property of the PLACE, so it must be impossible anywhere else —
    /// including on the monolith's own moon at a different landing site, which is where every previous
    /// version of this mistake has lived (#648: the once-in-a-life beat firing at Luna's launch muzzle).</summary>
    [Fact]
    public void ItCanOnlyEverHappenWhereTheMonolithStands()
    {
        string[] landable =
        [
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
        ];

        var canHappen = new List<string>();
        foreach (string body in landable)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                if (MonolithWatch.CanHappenOn(body, site.LayoutSalt))
                {
                    canHappen.Add($"{body}/{site.Name}");
                }
            }
        }

        Assert.Equal([$"{Monolith.BodyId}/{LandingSites.At(Monolith.BodyId, 0).Name}"], canHappen);

        // And it asks the one predicate rather than keeping an opinion of its own — the whole lesson of #648.
        foreach (string body in landable)
        {
            Assert.Equal(Monolith.StandsOn(body, ""), MonolithWatch.CanHappenOn(body, ""));
        }
    }

    /// <summary>THE CADENCE LAW: RARE, AND IT HOLDS STILL WHILE YOU ARE STANDING IN IT.
    ///
    /// <para><see cref="StoryBeats"/>' own reasoning, applied to a beat that is not a card: <i>"a card that
    /// fires whenever its condition is true is how a game teaches players to dismiss cards without reading
    /// them."</i> Most walks out to the stone must be a long walk to a stone.</para></summary>
    [Fact]
    public void MostVisitWindowsAreJustALongWalkToAStone()
    {
        const int windows = 600;
        int attentive = 0;
        for (long epoch = 0; epoch < windows; epoch++)
        {
            if (MonolithWatch.AttentiveIn(Monolith.BodyId, "", epoch))
            {
                attentive++;
            }
        }

        Assert.True(attentive > windows / 8, "the ground is never attentive — the beat is dead.");
        Assert.True(attentive < windows / 2, "the ground is attentive most of the time — it is wallpaper.");

        // Deterministic, and it holds still for a whole excursion: the same window always gives the same
        // answer, so a captain who walks out, walks back and returns the same afternoon is not re-rolled at.
        // (Object persistence — the owner's law for this ground since #586.)
        for (long epoch = 0; epoch < 40; epoch++)
        {
            Assert.Equal(
                MonolithWatch.AttentiveIn(Monolith.BodyId, "", epoch),
                MonolithWatch.AttentiveIn(Monolith.BodyId, "", epoch));
        }

        // The dwell is a real wait: long enough that it can never read as a reaction to arriving, and short
        // enough to fit inside a tank of air with the walk home still in it.
        Assert.True(MonolithWatch.DwellSeconds >= 20.0, "the ground reacts to you arriving — that is a trigger.");
        Assert.True(MonolithWatch.DwellSeconds < SuitAir.TankSeconds / 4.0,
            "standing still for the beat costs most of a tank — nobody will ever see it.");
    }

    /// <summary>A BEAT ABOUT THE PACK NEEDS A PACK. Narrating Old Ones that are not on the field is the sim
    /// doing one thing while the sentence says another — the third named bug class on this project, and the
    /// commonest. So the variant is skipped rather than told, and the roll still lands somewhere.</summary>
    [Fact]
    public void ThePackVariantIsNeverToldToAnEmptyField()
    {
        for (long epoch = 0; epoch < 200; epoch++)
        {
            Assert.NotEqual(MonolithWatch.What.ThePackGoesStill,
                MonolithWatch.Which(Monolith.BodyId, "", epoch, packOnTheField: false));
        }

        // With a pack on the ground it is reachable — a variant that can never be drawn is a variant that is
        // not really there, which is the "designed, never consumed" failure one level down.
        Assert.Contains(
            Enumerable.Range(0, 200)
                .Select(e => MonolithWatch.Which(Monolith.BodyId, "", e, packOnTheField: true)),
            w => w == MonolithWatch.What.ThePackGoesStill);
    }

    /// <summary>EVERY VARIANT IS REACHABLE. The roll must actually reach all of them, or the rarest thing in
    /// the game has variants nobody will ever be shown — a green world that cannot tell pass from fail.</summary>
    [Fact]
    public void EveryVariantCanActuallyBeDrawn()
    {
        var drawn = new HashSet<MonolithWatch.What>();
        for (long epoch = 0; epoch < 400; epoch++)
        {
            drawn.Add(MonolithWatch.Which(Monolith.BodyId, "", epoch, packOnTheField: true));
        }

        Assert.Equal(Everything().OrderBy(w => w), drawn.OrderBy(w => w));
    }
}
