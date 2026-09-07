using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// <b>THE PINS, AND THE FACTS THEY BUY</b> — the assertion half of
/// <see cref="EveryRoundFingerprintsTheSameTests"/>.
///
/// <para>What this part owns is the written-down hashes and the five laws stated against them: every case
/// hashes to what it hashed to on the old code, the same case walked twice is the same round, every arm of
/// the chain is walked by some case, the two that coexist and the one that is now a law, and the hold arm
/// that is unreachable with a guard that says so. The bench and the cases themselves are in the file that
/// carries the class docblock.</para>
/// </summary>
public sealed partial class EveryRoundFingerprintsTheSameTests
{
    // ── THE PINS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// What each case's transcript hashed to on the 175-line <c>AdvancePatrol</c> of <c>d064085</c>, before a
    /// line of it was moved. A change here is either a behaviour change in the round or a change to the floor
    /// the round is walked on; both are things a human should have to look at.
    ///
    /// <para><b>#920 · TWO OF THE THIRTEEN WERE RE-RECORDED, AND THIS IS THE THIRD KIND OF CHANGE.</b> This
    /// transcript writes down EVERY field and property of a <c>Guard</c>, so a lane that changes a piece of
    /// state the round never reads still moves a hash — the digest is a fingerprint of the man, not only of
    /// what he did. #920 zeroed <c>WalkUpFor</c> when the walk-up ends at card reach, and the two cases with a
    /// card in them moved: <i>he hails you and you walk away from it</i> (286 frames) and <i>he crosses the
    /// floor, reads your papers, and walks you to the car</i> (1,686). 1,972 frames — the exact number
    /// <see cref="TheTwoThatCoexistAndTheOneThatIsNowALaw"/> had been tallying.</para>
    ///
    /// <para>It was proved a state change and not a behaviour change the only honest way: both transcripts
    /// were written out whole and compared LINE BY LINE. All thirteen have the same line count either way;
    /// eleven are byte-identical; and on every one of the 1,972 lines that differ, deleting the
    /// <c>WalkUpFor=…</c> token from both sides makes the two lines byte-identical — every other field, the
    /// route and its cursor, the card, the pulse, the log and the end-of-case "what this case moved on the
    /// page" section included. Nothing else moved by a digit. <b>If either digest below ever moves again, that
    /// is not this lane's kind of change and the same line-by-line diff is what settles it.</b></para>
    ///
    /// <h3>#804 · EIGHT OF THE FOURTEEN WERE RE-RECORDED, AND THE SAME DIFF SETTLED IT</h3>
    ///
    /// <para>The canon pass (2026-09-02) rewrote four authored sentences and turned the escort round: the
    /// captain is walked <b>in front of</b> the guard now, not in his wake. All fourteen transcripts were
    /// written out whole on the base (<c>2b94e25</c>) and on the lane and compared line by line, and the eight
    /// that moved fall into exactly two groups:</para>
    ///
    /// <list type="bullet">
    /// <item><b>FIVE ARE PROSE AND NOTHING ELSE.</b> <i>he hails you and you walk away from it</i>, and the
    /// four cubicle cases. Drop every line carrying one of the four rewritten sentences — the same COUNT of
    /// them on each side (858 / 411 / 400 / 411 / 200) — and the remainder is byte-identical. Not one
    /// coordinate, clock, arm or decision moved.</item>
    /// <item><b>THREE WALK AN ESCORT</b> — <i>reads your papers and walks you to the car</i>, <i>does not
    /// press the button for your floor</i>, <i>comes at a run and he has you</i> — and the escort is where the
    /// geometry changed. Every differing line in the three is the captain's own position, a guard's numbers on
    /// the frames beside it, or a pulse whose clock has shifted with them; the first difference in each is the
    /// frame the walk back begins, and everything before it is byte-identical. The DECISIONS are unchanged and
    /// were counted rather than eyeballed: <c>escorts=</c>, <c>walkedAway=</c>, <c>ride=</c> and the arm each
    /// man takes have identical tallies on both sides in all three. The one clock that moved is the kick-out
    /// case's, by <b>eight frames of 1,800</b> (0.13 s) — the same events, in the same order, arriving a
    /// heartbeat apart because the man being walked out is now taking the corner first.</item>
    /// </list>
    ///
    /// <para>The other six — the empty floor, the surface, both plain rounds, <i>by the time he moves you are
    /// gone</i> and the gunshot — are byte-identical and their digests are untouched.</para>

    /// <h3>#719 · RE-RECORDED — twelve of the fourteen, and NOT ONE FRAME OF ANY ROUND MOVED</h3>
    ///
    /// <para>The second way out cuts a pocket into the spine's face at the far blind end of every listed
    /// floor: four wall segments, one leaf, one plate. The rounds themselves are untouched by it and that was
    /// MEASURED rather than argued, twice over.</para>
    ///
    /// <para><b>The circuit is identical.</b> <c>PatrolBeat.Circuit</c> for luna B1 and B2, dumped with the
    /// carve in and with it commented out, is byte-for-byte the same list of stops at the same coordinates —
    /// the stair stands past the outermost rib, where no stop and no checkpoint reaches.</para>
    ///
    /// <para><b>And so is every frame.</b> Each case's whole transcript was written to disk both ways and
    /// diffed. In all twelve the ONLY differing lines are in the closing census of what the case moved on the
    /// page, and they are COUNTS of geometry:</para>
    /// <code>
    /// &lt;   _sightBlockers.Count: 0 → 721        &gt;   _sightBlockers.Count: 0 → 726
    /// &lt;   _sightDoorShut.Count: 0 → 66         &gt;   _sightDoorShut.Count: 0 → 67
    /// &lt;   _sightStone.Count: (not there) → 655 &gt;   _sightStone.Count: (not there) → 659
    /// &lt;   _patrolReadables.Count: 139 → 0      &gt;   _patrolReadables.Count: 141 → 0
    /// </code>
    /// <para>+4 stone (the pocket's two sides, its back, and the run of spine face the mouth splits in two),
    /// +1 shut door (the leaf), +2 readables (the leaf and its plate). Not one man's position, arm, clock,
    /// decision or line differs on any frame of any case. The two rows NOT re-recorded are the ones with no
    /// sight sets to count: the empty floor and the surface.</para>
    /// </summary>
    private static readonly Dictionary<string, string> Pinned = new(StringComparer.Ordinal)
    {
        ["an empty floor still fades the plate"] = "148ff6f3378feede544d018d3771364edfe52e86ecdf3b316d39696757114ab3",
        ["up on the surface there is no round at all"] = "343943d96209111af869ab6dbe8a2fb427427d89b9ca1eb09e9de0fa1934d8ae",
        ["two men walk the round and nobody is watching"] = "ee91c1af19ee9bfac481b1803b663ae168ab4c61e2d7195e89c49751e67af025",
        ["one man walks the round, all the way round it"] = "1584a99dedef68f26a9bede1c55c7faecc54cdf244b1d0d7e87dc9283edc9844",
        // #920 · re-recorded (was 11047c53… / e28da120…) — the walk-up clock now stops when the walk-up does,
        // and the transcript writes the clock down. 1,972 lines, WalkUpFor and nothing else. See above.
        ["he hails you and you walk away from it"] = "d2571d4d5d1e03a6974bc9816dfac58b95b4f926ef91917b5fe3a108a6dec620",
        ["he crosses the floor, reads your papers, and walks you to the car"] = "62bfac54db0b728e288caa80e87b2428fa92f2a23e76cf68ae9511d57cb5fb29",
        ["…and this time he does not press the button for your floor"] = "0189602951060c2b4607f72a210dcd853d82c01a6b5e2d72ec3e68bf33f84d47",
        ["he calls it in, comes at a run, and he has you"] = "b5f30b958844925a76d5c551e8be80fdc9748a31d7485247437872886099ef4d",
        ["he calls it in, and by the time he moves you are gone"] = "6e092abd4f8b4e53cf8f34cca65e0cdbbe875e9e56bde25c4d13eba597a42418",
        ["you duck into a cubicle and he watched the catch turn"] = "eaea42898bd99509cc4c64f45cb50e03b39a60828b6bcd1d5aca12d0fef4cd35",
        ["you duck into a cubicle and nobody saw a thing"] = "71d7e635fce30337a5e420a53f79cb12737bacaa9075e219a038ce7eee88fa94",
        ["he called it in, and then you shut a door in his face"] = "338d8a0010b3b55a9cd0e02ee1bc91c9c566e226d8e7930321478f622c81418c",
        ["you duck in while he is already walking over"] = "67b2b4ce80c4b02b200b578b1b7024663a0c1f78ff691d136a3d343b6c1f3775",

        // #618 · THE FOURTEENTH, and the ONLY digest in this file taken on new code — stated here rather than
        // left for somebody to work out from a git blame. It could not have been taken anywhere else: before
        // this lane there was nothing on this ground for a man to walk to, and the thirteen above are
        // byte-identical on it (no field was added to Guard, which is exactly why #618's three fields live on
        // the round instead of on the man).
        //
        // ANTI-VACUOUS, measured rather than argued: with the shot commented out of the staging and nothing
        // else changed, the same four hundred frames hash to
        // e75f975023f12182859b00c33b7647935431e65f328beeb98b897585145912ea. The bang is what this row is about.
        ["a gun goes off down the corridor and somebody walks over"] =
            "f303f0a8a166c56eaa599292e6d3f5c34b70541a3b611c061a15ffc3019a58f5",
    };



    // ── THE FACTS ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>EVERY ROUND FINGERPRINTS THE SAME. The whole of the guard: drive each case through the fixed
    /// dt sequence and hash what the floor did.</summary>
    [Fact]
    public void EveryCaseHashesToWhatItHashedToOnTheOldCode()
    {
        var wrong = new List<string>();
        foreach (Case c in EveryCase())
        {
            (string text, int frames, _, _) = Walk(c);
            string got = Sha256(text);
            if (!Pinned.TryGetValue(c.Name, out string? want) || !string.Equals(want, got, StringComparison.Ordinal))
            {
                wrong.Add($"  {c.Name} — {frames} frame(s), sha256 {got}\n      pinned {want ?? "(nothing)"}");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} case(s) walk a different round than the one pinned on the old code:\n"
            + string.Join("\n", wrong));
    }

    /// <summary>…and it hashes the same TWICE, in one process, from two fresh floors. The clause that catches
    /// a plan built off an unordered set, a cache with memory in it, or a round that reads a clock.</summary>
    [Fact]
    public void TheSameCaseWalkedTwiceIsTheSameRound()
    {
        foreach (Case c in EveryCase())
        {
            Assert.Equal(Sha256(Walk(c).Text), Sha256(Walk(c).Text));
        }
    }

    /// <summary>THE CASE SET ENTERS EVERY ARM IT CAN. Six of the seven arms of the chain, each named, each
    /// entered by at least one case — otherwise the pins above would be a guard that cannot tell pass from
    /// fail on the arms nobody walked.
    ///
    /// <para>The census is taken by this file, from the page's own private answers (the escort, the hide, the
    /// hold law) and a replica of the chain's own tests. It is deliberately not used for anything but
    /// coverage: it is one frame behind on the arms armed inside a frame, which does not matter for counting
    /// and would matter for pinning.</para></summary>
    [Fact]
    public void EveryArmOfTheChainIsWalkedBySomeCase()
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Case c in EveryCase())
        {
            foreach ((string arm, int n) in Walk(c).Arms)
            {
                seen[arm] = seen.TryGetValue(arm, out int had) ? had + n : n;
            }
        }

        foreach (string arm in new[]
        {
            "the escort", "the knock at the door", "the run", "lost to a door",
            "the walk-up", "the round's own leg",
        })
        {
            Assert.True(seen.GetValueOrDefault(arm) > 0,
                $"no case in this file ever takes the `{arm}` arm — the pins say nothing about it.\n"
                + "walked: " + string.Join(", ", seen.Select(kv => $"{kv.Key}×{kv.Value}")));
        }

        // …and the one that is not in that list is not in it for a reason, which the next fact states.
        Assert.Equal(0, seen.GetValueOrDefault("the cover act"));
    }

    /// <summary>
    /// #870 lane 6′d · …AND THE THINGS THAT DO COEXIST. The other half of
    /// <see cref="IsHeStillOneMan"/>, and the half that keeps it honest: <c>Guard.Check()</c> is a list of
    /// eight pairs it says never happen, and a list like that is worth exactly as much as the pairs it
    /// LEAVES OFF are real.
    ///
    /// <para><b>#920 · The third one is gone from the list, which is the whole of this lane.</b> The spent
    /// walk-up clock — <c>WalkUpFor</c> left standing on a man who has stopped walking up — was 6′d's filed
    /// finding at <b>1,972</b> guard-frames of these thirteen cases: two of the three roads out of a walk-up
    /// zeroed it and the road THROUGH THE CARD did not. <c>Guard.HeStopsWalkingUp</c> zeroes it now and
    /// <c>Guard.Check</c> asserts it, so the tally below is 0 and stays 0. It is kept as a NUMBER rather than
    /// deleted on purpose: the day somebody drops the clause from <c>Check</c>, this line is what still
    /// counts the frames, and a law with a second witness is a law that cannot be quietly relaxed.</para>
    /// </summary>
    [Fact]
    public void TheTwoThatCoexistAndTheOneThatIsNowALaw()
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Case c in EveryCase())
        {
            foreach ((string what, int n) in Walk(c).AlsoTrue)
            {
                seen[what] = seen.GetValueOrDefault(what) + n;
            }
        }

        string counted = string.Join(", ", seen.Select(kv => $"{kv.Key}×{kv.Value}"));

        // #920 · THE ONE THAT USED TO REALLY HAPPEN — 1,972 of these frames carried a walk-up clock on a man
        // who was not walking up, and now none of them do. Guard.Check asserts it, so IsHeStillOneMan would
        // already have thrown on the frame it went wrong; this is the second witness, and it is the one that
        // still counts if the clause is ever dropped from Check.
        Assert.Equal(0, seen.GetValueOrDefault("a spent walk-up clock"));

        // …and the two that DO coexist, each for its own reason, each stated rather than left silent.
        //
        // THE BENCH one is a law: TheHoldArmIsUnreachableAndTheGuardSaysSo below proves MustHold is false
        // for every guard on every floor however the captain sits, so a suspended walk-up cannot arise.
        Assert.Equal(0, seen.GetValueOrDefault("a walk-up a bench has suspended"));

        // THE STAND one is NOT a law — it is reachable BY CONSTRUCTION and no case in this file reaches it.
        // #920 checked, because a pair observed zero times looks exactly like a pair that cannot happen: a
        // man arrives at a stop (HeArrivesAtTheStop — five seconds owed, no route), the captain shuts a
        // cubicle on him while he stands, and WaitOutsideTheCubicle hands him a route to that door on a frame
        // where nothing has touched the stand, because Standing is spent only by HeSpendsAFrameStanding and
        // that is the ROUND's arm, not the arm he is in. So it is tallied here and deliberately NOT asserted
        // by Guard.Check: an invariant nothing has ever walked is a clause waiting to go red on somebody
        // else's afternoon. A case that reaches it is worth adding, and this number is where it shows up.
        Assert.Equal(0, seen.GetValueOrDefault("a stand remembered across a detour"));
    }

    /// <summary>#793 · THE HOLD ARM CANNOT BE ENTERED FROM THIS METHOD'S OWN INPUTS, and that is the shipped
    /// law rather than a gap in the case set: a guard is always <c>OnAPublishedRound</c>, a published round
    /// can never be <c>IsTailing</c>, so <c>MustHold</c> is false for every guard on every floor however the
    /// captain sits. The day something down here does tail the captain, this fact goes red and the case set
    /// owes the cover act a case.</summary>
    [Fact]
    public void TheHoldArmIsUnreachableAndTheGuardSaysSo()
    {
        for (int i = 0; i < PatrolBeat.MostOnAFloor; i++)
        {
            FootTail.Mover afoot = PatrolBeat.OnTheRound(i, 10.0, 10.0);
            Assert.True(afoot.OnAPublishedRound, "a guard is no longer stamped as a published round.");
            Assert.False(FootTail.IsTailing(in afoot));
            Assert.False(FootTail.MustHold(true, 10.2, 10.2, in afoot, null),
                "a guard CAN be held now — the cover-act arm is reachable and this file owes it a case.");
        }
    }
}
