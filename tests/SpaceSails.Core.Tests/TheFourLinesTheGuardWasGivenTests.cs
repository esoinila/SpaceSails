using System;
using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #804 · THE CANON PASS, HELD TO. Four sentences were authored for the round (2026-09-02) with one
/// instruction on them — <b>verbatim</b> — and a sentence that is only nearly right is the failure mode this
/// whole file exists to catch:
///
/// <list type="bullet">
/// <item><b>The ear, before any sighting.</b> <i>"Boots on shotcrete, out of step with yours."</i></item>
/// <item><b>The challenge.</b> <i>"Hold there. Floor's restricted. Show me something."</i></item>
/// <item><b>The pass.</b> <i>"Right. Keep to the lit side."</i></item>
/// <item><b>The refusal.</b> <i>"No? Then you walk ahead of me to the lift, and we don't make it a
/// thing."</i></item>
/// </list>
///
/// <para><b>Asked of the SIM, not of the constants.</b> Every claim below goes through
/// <see cref="PatrolBeat.TheGuardReads"/> and takes the string a player is actually told
/// (<see cref="PatrolBeat.Read.Told"/>, <see cref="PatrolBeat.Read.Card"/>). A test that compared
/// <see cref="PatrolBeat.AuthoredLines"/> to the constants it was copied from would be two copies of one
/// mistake agreeing with each other — the fifth bug class, verbatim.</para>
///
/// <para><b>PROVEN RED against the build of 2026-08-31</b> (the shipped feature, before this canon pass).
/// Every one of the four was absent: the ear said <i>"out of sight and in no hurry — the tread of somebody
/// walking a line they have walked all week"</i>, the card had the gesture and no words in it at all, the
/// pass said <i>"Mind the wet floor round the corner"</i>, and the refusal opened on narration with nothing
/// in the guard's mouth. <see cref="TheParaphrasesThatShippedBeforeTheCanonPassAreGone"/> pins those four
/// old strings as ABSENT, so the red is reproducible in both directions.</para>
/// </summary>
public sealed class TheFourLinesTheGuardWasGivenTests
{
    private static readonly string[] Sites = ["luna", "miranda", "titan", "europa"];

    /// <summary>The ear line's own index in the catalog. Named rather than typed at each use, so the order of
    /// the list is one fact.</summary>
    private const int Ear = 0, Challenge = 1, Pass = 2, Refusal = 3;

    /// <summary>
    /// EVERY AUTHORED LINE REACHES A PLAYER, ON EVERY PATROLLED FLOOR OF EVERY SITE, THROUGH EVERY PLATE.
    ///
    /// <para>The sweep is over plates as well as floors because the card is composed from the plate: a line
    /// that survived on one of the four and was lost on another is exactly the bug a single spot-check
    /// misses.</para>
    /// </summary>
    [Fact]
    public void TheFourAuthoredLinesAreWhatTheSimActuallyTells()
    {
        Assert.Equal(4, PatrolBeat.AuthoredLines.Count);

        // 1 · THE EAR. It is not a read at all — it is the one line said about somebody the captain cannot
        // see — so it is asked of the constant the client pulses and nothing else touches.
        Assert.Contains(
            PatrolBeat.AuthoredLines[Ear], PatrolBeat.HeardLine, StringComparison.Ordinal);

        // WHO CAN STAND OVER THE CARD. Collected from the sim's own roster rather than typed, and the count
        // is asserted, so a fifth plate cannot be added and go unread by this sweep.
        var plates = new SortedSet<string>(StringComparer.Ordinal);
        for (long watch = 0; watch < 64; watch++)
        {
            plates.Add(PatrolBeat.PlateOf("luna", -2, watch, 0));
        }
        Assert.Equal(PatrolBeat.PlateCount, plates.Count);

        var bad = new List<string>();
        int reads = 0, floors = 0;

        foreach (string site in Sites)
        {
            foreach (int level in UndergroundComplex.FloorsOf(site))
            {
                if (!PatrolBeat.IsPatrolled(site, level))
                {
                    continue;
                }
                floors++;

                int who = 0;
                foreach (string plate in plates)
                {
                    who++;

                    // 2 · THE CHALLENGE — said before anything is read, so it is on the card BOTH arms carry.
                    PatrolBeat.Read shown = PatrolBeat.TheGuardReads(site, plate, PatrolBeat.Badge(site));
                    PatrolBeat.Read empty = PatrolBeat.TheGuardReads(site, plate, null);
                    reads += 2;

                    foreach ((string arm, PatrolBeat.Read r) in
                             new[] { ("badged", shown), ("empty-handed", empty) })
                    {
                        if (!r.Card.Contains(PatrolBeat.AuthoredLines[Challenge], StringComparison.Ordinal))
                        {
                            bad.Add($"  {site} B{-level} plate {who} ({arm}): the card does not say " +
                                    $"\"{PatrolBeat.AuthoredLines[Challenge]}\" — the stop is a pantomime again.");
                        }
                    }

                    // 3 · THE PASS. Told, not merely stored: this is the string the client puts under the
                    // painting.
                    if (!shown.Satisfied
                        || !shown.Told.Contains(PatrolBeat.AuthoredLines[Pass], StringComparison.Ordinal))
                    {
                        bad.Add($"  {site} B{-level} plate {who}: this site's own pass did not end in " +
                                $"\"{PatrolBeat.AuthoredLines[Pass]}\".");
                    }

                    // 4 · THE REFUSAL, which is an ESCORT and says so in his own mouth.
                    if (empty.Satisfied
                        || !empty.Told.Contains(PatrolBeat.AuthoredLines[Refusal], StringComparison.Ordinal))
                    {
                        bad.Add($"  {site} B{-level} plate {who}: an empty wallet did not end in " +
                                $"\"{PatrolBeat.AuthoredLines[Refusal]}\".");
                    }
                }
            }
        }

        Assert.True(bad.Count == 0, $"{bad.Count} finding(s) about the canon pass:\n{string.Join("\n", bad)}");

        // A sweep over nothing is a green test that asserts nothing.
        Assert.True(floors > 8, $"only {floors} patrolled floors were read on.");
        Assert.True(reads > 60, $"only {reads} wallet reads were made.");
    }

    /// <summary>
    /// AND THE PARAPHRASES ARE GONE. The other half of the red: the four sentences the feature shipped with
    /// before the canon pass may not survive anywhere in what a player is told, because a build carrying both
    /// is a build where one of them is dead prose nobody noticed.
    ///
    /// <para>This is the direction that fails LOUDLY on the base branch — every string below is present
    /// there — which is what makes the pair reproducible rather than a claim about a diff.</para>
    /// </summary>
    [Fact]
    public void TheParaphrasesThatShippedBeforeTheCanonPassAreGone()
    {
        string[] shipped =
        [
            "out of sight and in no hurry",              // the ear
            "What you get is a hand out, palm up",       // the challenge, when it had no words in it
            "Mind the wet floor round the corner",       // the pass
        ];

        foreach (string old in shipped)
        {
            Assert.DoesNotContain(old, PatrolBeat.HeardLine, StringComparison.Ordinal);
            Assert.DoesNotContain(old, PatrolBeat.SatisfiedLine, StringComparison.Ordinal);
            Assert.DoesNotContain(old, PatrolBeat.EscortLine, StringComparison.Ordinal);
            Assert.DoesNotContain(
                old, PatrolBeat.ChallengeCard("◈ A CONTRACT GUARD, WALKING THE ROUND"),
                StringComparison.Ordinal);
        }

        // …and the refusal opens on HIM rather than on a narrator. The shipped line began "He walks you back
        // to the car himself"; the authored one begins in quotation marks, because the escort is offered
        // rather than performed.
        Assert.StartsWith("👮 \"" + PatrolBeat.AuthoredLines[Refusal], PatrolBeat.EscortLine,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// THE VACUITY PAIR, WHICH IS ALSO THE FEATURE'S OWN LAW: a captain who belongs is not challenged into
    /// anything, and a captain who does not is — on the same floors, through the same call, with only the
    /// paper in his hand different.
    ///
    /// <para>Both halves are swept, and the count of each is asserted, so a run in which one arm never
    /// happened cannot pass. A guard that satisfied everybody and a guard that refused everybody would both
    /// be green against either half alone.</para>
    /// </summary>
    [Fact]
    public void TheBadgedCaptainIsWavedOnAndTheEmptyHandedOneIsWalkedOut()
    {
        var bad = new List<string>();
        int waved = 0, walked = 0;

        foreach (string site in Sites)
        {
            foreach (int level in UndergroundComplex.FloorsOf(site))
            {
                if (!PatrolBeat.IsPatrolled(site, level))
                {
                    continue;
                }

                string plate = PatrolBeat.PlateOf(site, level, 0, 0);

                // HE BELONGS: this site's own pass, on a floor this site patrols.
                PatrolBeat.Read ok = PatrolBeat.TheGuardReads(site, plate, PatrolBeat.Badge(site));
                if (!ok.Satisfied)
                {
                    bad.Add($"  {site} B{-level}: a man on the site's own books was refused.");
                }
                else if (ok.Consequence is not null)
                {
                    bad.Add($"  {site} B{-level}: a pass that WORKED still cost something " +
                            $"(\"{ok.Consequence}\") — a challenge answered is not a challenge.");
                }
                else
                {
                    waved++;
                }

                // HE DOES NOT: nothing in the wallet a palm is for.
                PatrolBeat.Read no = PatrolBeat.TheGuardReads(site, plate, null);
                if (no.Satisfied)
                {
                    bad.Add($"  {site} B{-level}: an empty wallet satisfied him.");
                }
                else if (no.Consequence != PatrolBeat.EscortLine)
                {
                    bad.Add($"  {site} B{-level}: the refusal's consequence was not the escort.");
                }
                else
                {
                    walked++;
                }
            }
        }

        Assert.True(bad.Count == 0, $"{bad.Count} finding(s):\n{string.Join("\n", bad)}");
        Assert.True(waved > 8, $"only {waved} floors waved a badged captain on.");
        Assert.Equal(waved, walked);

        // …and the two arms are DIFFERENT sentences. A build where both said the same thing would pass every
        // count above.
        string one = PatrolBeat.TheGuardReads("luna", "◈ A CONTRACT GUARD, WALKING THE ROUND",
            PatrolBeat.Badge("luna")).Told;
        string other = PatrolBeat.TheGuardReads("luna", "◈ A CONTRACT GUARD, WALKING THE ROUND", null).Told;
        Assert.NotEqual(one, other);
    }
}
