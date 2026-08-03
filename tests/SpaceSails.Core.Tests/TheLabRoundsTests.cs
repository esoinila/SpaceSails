using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #603 · THE ROUNDS FROM AN ILLEGAL LAB. Owner, across a design run:
///
/// <para><i>"some special ammo that only uses one round per reever but is dangerous to fire to too near
/// targets"</i> · <i>"those rounds would go through several reevers if in group also"</i> · <i>"I like the
/// choice of variety"</i> · <i>"the rounds ... make one wonder the purpose they were needed in the first
/// place ... like what monster / opponent needs such :-D"</i></para>
///
/// <para>All of it is one round: it arms after travel and does its work on the far side of the first thing
/// it meets. That is why one is enough, why a queue goes down together, and why it must never be fired at
/// something already on top of you.</para>
/// </summary>
public sealed class TheLabRoundsTests
{
    private static SentryBot.Deployed Gun(double x, double y, int rounds = 20) =>
        new("K-77", x, y, rounds);

    private static SentryBot.Target At(double x, double y) => new(x, y, 0);

    [Fact]
    public void VarietyNotUpgrade_TheLabRoundIsWORSESomewhere()
    {
        // The owner's ruling, and the thing that keeps this from being loot: a round is defined by WHERE IT
        // IS WRONG. If the lab round were better everywhere, nobody would ever carry anything else and the
        // choice the satchel exists for would evaporate.
        Assert.True(Ammunition.LabTwoStage.MinimumRangeDu > 0,
            "the lab round is safe at every range — that is an upgrade, not a variety.");
        Assert.Equal(0, Ammunition.Issue.MinimumRangeDu);

        // And better where it is better, or there is no reason to want it.
        Assert.True(Ammunition.LabTwoStage.HitsToKill < Ammunition.Issue.HitsToKill);
        Assert.True(Ammunition.LabTwoStage.Penetrates > Ammunition.Issue.Penetrates);
    }

    [Fact]
    public void OneRoundPerReever()
    {
        // Owner: "only uses one round per reever". Issue ball grinds; this does not.
        var bots = new[] { Gun(0, 0) };
        var pack = new[] { At(12, 0) };

        SentryBot.Volley lab = SentryBot.Step(bots, pack, null, [Ammunition.LabTwoStage]);
        Assert.Single(lab.Husks);
        Assert.Equal(19, lab.Bots[0].Rounds);   // exactly one round spent

        SentryBot.Volley issue = SentryBot.Step(bots, pack, null, [Ammunition.Issue]);
        Assert.Empty(issue.Husks);              // issue needs more than one hit
    }

    [Fact]
    public void ItGoesTHROUGHAQueue()
    {
        // Owner: "those rounds would go through several reevers if in group also". Four in a line down a
        // corridor, one shot.
        var bots = new[] { Gun(0, 0) };
        var queue = new[] { At(10, 0), At(13, 0), At(16, 0), At(19, 0) };

        SentryBot.Volley v = SentryBot.Step(bots, queue, null, [Ammunition.LabTwoStage]);

        Assert.Equal(4, v.Husks.Count);
        Assert.Equal(19, v.Bots[0].Rounds);   // still ONE round for the lot
    }

    [Fact]
    public void ButNotThroughAPackThatSpreadOut()
    {
        // The character of the round, and the reason it is a choice rather than a win button: it rewards a
        // corridor and does nothing in the open. A pack fanned across a field is four separate shots.
        var bots = new[] { Gun(0, 0) };
        var spread = new[] { At(10, 0), At(11, 9), At(12, -9), At(13, 14) };

        SentryBot.Volley v = SentryBot.Step(bots, spread, null, [Ammunition.LabTwoStage]);
        Assert.Single(v.Husks);
    }

    [Fact]
    public void ItNeverReachesBACKWARDSAlongTheLine()
    {
        // Penetration is strictly beyond the first target. Something standing between the gun and its target
        // is not "behind" anything, and a round that hit it would be hitting the thing the gun was avoiding.
        var bots = new[] { Gun(0, 0) };
        var targets = new[] { At(16, 0), At(8, 0) };   // the second is CLOSER

        SentryBot.Volley v = SentryBot.Step(bots, targets, null, [Ammunition.LabTwoStage]);

        // It engages the nearest it can see (8), and there is nothing beyond it in line except 16 — which is
        // behind, so both go. What must never happen is the reverse: engaging 16 and reaching back to 8.
        Assert.Equal(2, v.Husks.Count);
        Assert.Equal(19, v.Bots[0].Rounds);
    }

    [Fact]
    public void ADryOrIssueLoadedGunIsExactlyWhatItAlwaysWas()
    {
        // Every existing caller passes no ammunition at all, so the default must be byte-for-byte the old
        // behaviour or this change has quietly retuned the whole game.
        var bots = new[] { Gun(0, 0) };
        var pack = new[] { At(10, 0), At(13, 0) };

        SentryBot.Volley bare = SentryBot.Step(bots, pack);
        SentryBot.Volley issue = SentryBot.Step(bots, pack, null, [Ammunition.Issue]);

        Assert.Equal(bare.Husks.Count, issue.Husks.Count);
        Assert.Equal(bare.Bots[0].Rounds, issue.Bots[0].Rounds);
        Assert.Equal(bare.Shots, issue.Shots);
    }

    [Fact]
    public void ARefusalNearTheTargetSaysWHYAndOffersTheOverride()
    {
        // Owner's ruling: "the gun complains but also gives override option to just fire". So the refusal
        // must NAME the reason and point at the choice — an interlock that just sits there is
        // indistinguishable from a broken gun.
        (bool allowed, string? refusal) = Ammunition.MayFire(Ammunition.LabTwoStage, rangeDu: 3);
        Assert.False(allowed);
        Assert.NotNull(refusal);
        Assert.Contains("arms after travel", refusal!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("anyway", refusal!, StringComparison.OrdinalIgnoreCase);

        // And it is allowed the moment there is room for it to work.
        Assert.True(Ammunition.MayFire(Ammunition.LabTwoStage, rangeDu: 20).Allowed);

        // Issue ball is never refused — it is the thing you fall back to when something is on top of you.
        for (double r = 0; r < 30; r += 3)
        {
            Assert.True(Ammunition.MayFire(Ammunition.Issue, r).Allowed);
        }
    }

    [Fact]
    public void TheSpecificationIsEVIDENCEAndExplainsNothing()
    {
        // Owner: "the rounds from an illegal lab can do weird things and make one wonder the purpose they
        // were needed in the first place ... like what monster / opponent needs such :-D"
        //
        // The round describes ITSELF — what it does, what it is proofed to — and never its target. That is
        // the inference-horror rule (docs/art-manifest-hive.md): the object testifies, nothing narrates.
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];

        foreach (Ammunition.Kind kind in Ammunition.All)
        {
            foreach (string text in new[] { kind.Spec, kind.Name, Ammunition.FoundLine(kind, 6) })
            {
                foreach (string bad in forbidden)
                {
                    Assert.DoesNotContain(bad, text, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        // And the lab round's spec raises the question without answering it: it says what it is PROOFED
        // for, which is the fact that does the work.
        Assert.Contains("mass bracket", Ammunition.LabTwoStage.Spec, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnUnknownRoundIsIssueRatherThanACrash()
    {
        // An edited or future save should never cost a captain their game.
        Assert.Equal(Ammunition.Issue, Ammunition.ById("something-nobody-has-heard-of"));
        Assert.Equal(Ammunition.Issue, Ammunition.ById(null));
        Assert.Equal(Ammunition.LabTwoStage, Ammunition.ById("lab-2stage"));
    }
}
