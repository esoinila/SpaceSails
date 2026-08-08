using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #757 · TAKING A TABLE, WAITING AT IT, AND WHO COMES OF IT.
///
/// <para>Owner, live in the hall: <i>"I have empty table but I cannot sit down"</i> and <i>"the normal way to
/// operate in a bar or restaurant is still not implemented."</i> Then the sharpening that decided the design:
/// <i>"Suppose I just want to sit down and wait to be disturbed?"</i> — and, streamed while it was being
/// built, the shape of the approach: <i>"a stranger may approach me and 1. ask to sit down, 2. maybe offer to
/// buy me a drink, 3. tell me what they have in mind… think Gandalf knocking on Bilbo's door."</i></para>
///
/// <h3>What a guard here can actually prove, and the trap it must not fall into</h3>
///
/// <para>The FIFTH bug class is a world that cannot tell pass from fail. A "waiting brings somebody" test is
/// vacuous unless the same world can also produce nobody, and a "nobody comes" test is vacuous unless
/// somebody could have. So the sweep below measures BOTH answers out of real
/// <see cref="DiceRule"/> rolls across the watches the game actually has, and fails if either one is
/// unreachable — which is the only way to know the threshold selects something rather than everything.</para>
/// </summary>
public sealed class YouCanTakeATableAloneTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    /// <summary>How many watches a day is — <see cref="CanteenRegulars.WatchFill"/>'s own length, so this
    /// sweep cannot fall behind the room it is measuring.</summary>
    private static int Watches => CanteenRegulars.WatchFill.Count;

    // ── THE TABLE YOU TOOK ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_TABLE_YOU_TOOK_ALONE_OffersExactlyWaitingAndLeaving()
    {
        Encounter.Scene scene = SittingAlone.TheTable();

        var ids = scene.Moves.Select(m => m.Id).ToList();
        Assert.Equal([SittingAlone.Wait, SittingAlone.Stand], ids);

        // Waiting is FREE. It is the whole of the verb, and a passive move you had to pay for would be a
        // toll on standing still.
        Encounter.Move wait = scene.Moves[0];
        Assert.Equal(Encounter.Requirement.Free, wait.Needs);
        Assert.Equal(0, wait.Credits);
        Assert.False(wait.Rolled);

        // And the framework's own free-exit law holds on a scene with nobody in it, which is the point of
        // stating that law on Scene rather than on the canteen.
        Assert.True(Encounter.CanAlwaysLeave(scene));

        // The opening is the taking. A scene whose opening line was blank would draw an empty panel.
        Assert.Equal(SittingAlone.TookTheTableLine, scene.Opening);
        Assert.False(string.IsNullOrWhiteSpace(scene.Setting));
    }

    // ── WAITING: BOTH ANSWERS EXIST, AND THE ROOM DECIDES WHICH ───────────────────────────────────────

    /// <summary>
    /// #757 · THE WAIT CAN GO BOTH WAYS — measured against real rolls, never against a comment.
    ///
    /// <para>This is the guard that stops the whole feature being vacuous. If nobody ever came, "wait" would
    /// be a button that prints flavour; if somebody always came, the wrong watch would not exist and the
    /// owner's own event — an eighty-seat room that used to be loud and now answers nothing — would be
    /// unreachable. Both must be real, on the same tops, on the watches the game has.</para>
    /// </summary>
    [Fact]
    public void WAITING_BringsSomebodyOnSomeWatchesAndNobodyOnOthers()
    {
        int came = 0, didNot = 0;
        var cameOnWatch = new HashSet<long>();
        var emptyOnWatch = new HashSet<long>();

        foreach (string body in Bodies)
        {
            for (long watch = 0; watch < Watches; watch++)
            {
                for (int top = 0; top < 12; top++)
                {
                    for (int beat = 0; beat < 6; beat++)
                    {
                        if (SittingAlone.SomebodyComes(body, -1, top, watch, beat))
                        {
                            came++;
                            cameOnWatch.Add(watch);
                        }
                        else
                        {
                            didNot++;
                            emptyOnWatch.Add(watch);
                        }
                    }
                }
            }
        }

        Assert.True(came > 50, $"only {came} approaches in the whole sweep — nobody ever crosses the room.");
        Assert.True(didNot > 50, $"only {didNot} silences in the whole sweep — waiting always pays.");

        // …and on EVERY watch both answers are reachable. A watch where one of them is impossible is a watch
        // where the wait is not a roll, it is a caption.
        Assert.Equal(Watches, cameOnWatch.Count);
        Assert.Equal(Watches, emptyOnWatch.Count);
    }

    /// <summary>The hall's own fill decides the tempo — the busy watch brings people over more often than
    /// the dead one. Stated as a comparison of measured rates rather than of the constants, because the
    /// constants are FLAGGED for the owner's tuning and this law must survive them being tuned.</summary>
    [Fact]
    public void A_BUSY_HALL_BringsSomebodyOverMoreOftenThanAnEmptyOne()
    {
        long busiest = Argmax(CanteenRegulars.WatchFill);
        long emptiest = Argmin(CanteenRegulars.WatchFill);
        Assert.NotEqual(busiest, emptiest);

        Assert.True(Approaches(busiest) > Approaches(emptiest),
            $"watch {busiest} (fill {SittingAlone.Fill(busiest)}) brings {Approaches(busiest)} people over " +
            $"and watch {emptiest} (fill {SittingAlone.Fill(emptiest)}) brings {Approaches(emptiest)} — " +
            "the room's own population is not driving who crosses it.");

        // And nothing is ever impossible: a room with people in it always has SOME chance that one of them
        // walks up. A floor of zero would be a watch on which waiting is a dead control.
        for (long watch = 0; watch < Watches; watch++)
        {
            Assert.InRange(SittingAlone.FacesThatBringSomebody(watch), 1, SittingAlone.Faces - 1);
        }
    }

    private static int Approaches(long watch)
    {
        int n = 0;
        foreach (string body in Bodies)
        {
            for (int top = 0; top < 12; top++)
            {
                for (int beat = 0; beat < 6; beat++)
                {
                    if (SittingAlone.SomebodyComes(body, -1, top, watch, beat))
                    {
                        n++;
                    }
                }
            }
        }
        return n;
    }

    private static long Argmax(IReadOnlyList<double> xs)
    {
        int best = 0;
        for (int i = 1; i < xs.Count; i++)
        {
            if (xs[i] > xs[best])
            {
                best = i;
            }
        }
        return best;
    }

    private static long Argmin(IReadOnlyList<double> xs)
    {
        int best = 0;
        for (int i = 1; i < xs.Count; i++)
        {
            if (xs[i] < xs[best])
            {
                best = i;
            }
        }
        return best;
    }

    [Fact]
    public void THE_SAME_WAIT_AtTheSameTopOnTheSameShiftIsTheSameAnswer()
    {
        // Seeded, like everything else. A captain who could stand up and sit down again for a better answer
        // would be re-pressing a die, which this game refuses everywhere.
        foreach (string body in Bodies)
        {
            for (long watch = 0; watch < Watches; watch++)
            {
                for (int beat = 0; beat < 4; beat++)
                {
                    Assert.Equal(
                        SittingAlone.SomebodyComes(body, -1, 3, watch, beat),
                        SittingAlone.SomebodyComes(body, -1, 3, watch, beat));
                }
            }
        }
    }

    /// <summary>#751/#757 · A CABINET IS THE OPPOSITE CHOICE, and the machine says so. Sitting in the hall
    /// is choosing to be findable; taking a room with a door is choosing not to be, and nobody may cross a
    /// floor they cannot see you from — on any watch, at any top, at any beat.</summary>
    [Fact]
    public void NOBODY_EVER_ComesToATableBehindADoor()
    {
        foreach (string body in Bodies)
        {
            for (long watch = 0; watch < Watches; watch++)
            {
                for (int top = 0; top < 12; top++)
                {
                    for (int beat = 0; beat < 6; beat++)
                    {
                        Assert.False(SittingAlone.SomebodyComes(body, -1, top, watch, beat, quiet: true),
                            $"{body}, watch {watch}: somebody walked into a cabinet.");
                    }
                }
            }
        }
    }

    // ── NOBODY CAME IS A TOLD OUTCOME ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// #757 · WAITING AND BEING ANSWERED WITH NOTHING IS STILL BEING ANSWERED.
    ///
    /// <para>Owner, filing it: an empty room on the wrong watch IS the event. So the silence has words on it,
    /// the words are different from beat to beat (a room that repeated one sentence would read as a stuck
    /// control), and a BUSY hall's silence and an EMPTIED hall's silence are not the same sentence — which is
    /// the only way the second one can ever land.</para>
    /// </summary>
    [Fact]
    public void NOTHING_HAPPENING_IsToldAndTheTELLING_DependsOnTheRoom()
    {
        long busiest = Argmax(CanteenRegulars.WatchFill);
        long emptiest = Argmin(CanteenRegulars.WatchFill);

        var busy = new HashSet<string>();
        var quiet = new HashSet<string>();
        for (int beat = 0; beat < 8; beat++)
        {
            string a = SittingAlone.NobodyCame(busiest, beat);
            string b = SittingAlone.NobodyCame(emptiest, beat);
            Assert.False(string.IsNullOrWhiteSpace(a), "a wait that produced nobody said nothing at all.");
            Assert.False(string.IsNullOrWhiteSpace(b), "a wait that produced nobody said nothing at all.");
            busy.Add(a);
            quiet.Add(b);
        }

        // More than one thing to say on each side — a single line repeated is a control that looks broken.
        Assert.True(busy.Count > 1, "the busy hall has one sentence about waiting.");
        Assert.True(quiet.Count > 1, "the emptied hall has one sentence about waiting.");

        // …and the two rooms never borrow each other's words. This is the assertion that makes the emptied
        // hall an EVENT rather than a mood: the sentence a captain reads on the small watch cannot be one
        // they have already read at lunchtime.
        Assert.Empty(busy.Intersect(quiet));

        // The cabinet is a third room again, for the same reason.
        var cabinet = new HashSet<string>();
        for (int beat = 0; beat < 8; beat++)
        {
            cabinet.Add(SittingAlone.NobodyCame(busiest, beat, quiet: true));
        }
        Assert.Empty(cabinet.Intersect(busy));
        Assert.Empty(cabinet.Intersect(quiet));

        // Negative and huge beat counts are somebody else's arithmetic bug, not a crash.
        Assert.False(string.IsNullOrWhiteSpace(SittingAlone.NobodyCame(-3, -7)));
        Assert.False(string.IsNullOrWhiteSpace(SittingAlone.NobodyCame(long.MaxValue, int.MaxValue)));
    }

    // ── THE LADDER ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #757 · THE APPROACH IS A COURTSHIP, AND THE RUNGS ARE DATA.
    ///
    /// <para>Owner: <i>"1. ask to sit down, 2. maybe offer to buy me a drink, 3. tell me what they have in
    /// mind."</i> Each rung must be ABSENT until the sentence it answers has been spoken (#749's law: a reply
    /// you can give before the sentence exists reads as menu, not conversation), and the whole thing must be
    /// built out of <see cref="Encounter"/> rather than out of a second engine.</para>
    /// </summary>
    [Fact]
    public void THE_APPROACH_ClimbsChairThenDrinkThenTheAskAndNoRungExistsEarly()
    {
        Encounter.Scene her = SittingAlone.TheVisitor();
        Assert.True(Encounter.CanAlwaysLeave(her));

        // Rung one, and nothing else. Before you have done anything, the only things on the table are the
        // chair, the refusal, and the door.
        var opening = Encounter.OnTheTable(her, new HashSet<string>()).Select(m => m.Id).ToList();
        Assert.Equal(
            [SittingAlone.WaveIn, SittingAlone.WaveOff, SittingAlone.Stand],
            opening);

        // Rung two arrives only once she is in the chair.
        var seated = Encounter.OnTheTable(her, new HashSet<string> { SittingAlone.WaveIn })
            .Select(m => m.Id).ToList();
        Assert.Contains(SittingAlone.LetThemBuy, seated);
        Assert.Contains(SittingAlone.NoDrink, seated);
        Assert.DoesNotContain(SittingAlone.HearThemOut, seated);

        // Rung three arrives once the drink has been ANSWERED — and either answer does it, because turning
        // a drink down is still having heard the offer. A ladder that only climbed for the polite answer
        // would be the dead wall Fail Forward exists to refuse.
        foreach (string answer in new[] { SittingAlone.LetThemBuy, SittingAlone.NoDrink })
        {
            var after = Encounter.OnTheTable(
                    her, new HashSet<string> { SittingAlone.WaveIn, answer })
                .Select(m => m.Id).ToList();
            Assert.Contains(SittingAlone.HearThemOut, after);
        }

        // Waving her off does NOT open the ladder — it is a refusal, and a refusal that unlocked the next
        // rung would make the refusal decorative.
        var refused = Encounter.OnTheTable(her, new HashSet<string> { SittingAlone.WaveOff })
            .Select(m => m.Id).ToList();
        Assert.DoesNotContain(SittingAlone.LetThemBuy, refused);
        Assert.DoesNotContain(SittingAlone.HearThemOut, refused);
    }

    [Fact]
    public void EVERY_RUNG_SaysSomethingAndOnlyTheASK_GoesInTheBook()
    {
        foreach (Encounter.Move m in SittingAlone.TheVisitor().Moves)
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Says),
                $"'{m.Label}' is a move at this table with nothing to say — #603's founding law.");
            Assert.False(m.Rolled, $"'{m.Label}' rolls; nothing in a courtship is a check in this slice.");
            Assert.Equal(0, m.Credits);

            // #757 · The note rides the MOVE, and only one move carries one. A notebook full of manners is a
            // notebook nobody reads.
            if (m.Id == SittingAlone.HearThemOut)
            {
                Assert.Equal(SittingAlone.TheAskNote, m.Note);
            }
            else
            {
                Assert.Null(m.Note);
            }
        }

        // …and the framework hands that note on, so no client author has to remember which sentence was
        // worth writing down. THE DODGE IS UNTOUCHED: a fixed outcome with no note still files nothing.
        Assert.Equal(SittingAlone.TheAskNote,
            CanteenTable.SaidPlainly(
                new Encounter.Move(SittingAlone.HearThemOut, "x",
                    Says: SittingAlone.TheAskLine, Note: SittingAlone.TheAskNote)).Note);
        Assert.Null(CanteenTable.SaidPlainly(new Encounter.Move("x", "y", Says: "z")).Note);
    }

    /// <summary>
    /// #757 · THE ASK POINTS AT SOMETHING THE GAME CAN ALREADY DELIVER.
    ///
    /// <para>A quest-giver whose hook lands on nothing is a promise the build cannot keep. Hers lands on the
    /// Hand who has been here longest (#746), who writes names on the chit (#746), which the cage's gate
    /// reads (#752) — every one of them shipped. The guard reads her sentence for the object it points at,
    /// which is as far as a content test can carry it and further than nothing.</para>
    /// </summary>
    [Fact]
    public void HER_ASK_PointsAtTheHandWhoIsAlreadyInTheRoom()
    {
        foreach (string said in new[] { SittingAlone.TheAskLine, SittingAlone.TheAskNote })
        {
            Assert.Contains("the hand who", said, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("longest", said, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("writes the names", said, StringComparison.OrdinalIgnoreCase);
            // …and the door she cannot get through, which is the one the chit opens (#752).
            Assert.Contains("cage", said, StringComparison.OrdinalIgnoreCase);
        }

        // The Hand exists, is one of the three the table scene knows how to talk to, and is drawn from the
        // very cast the hall seats. If his plate is ever edited, this goes red rather than her lead quietly
        // pointing at nobody.
        Assert.Equal(CanteenTable.Who.Hand,
            CanteenTable.WhoIs($"{CanteenRegulars.Glyph} {CanteenTable.HandPlate}"));
        Assert.Contains(CanteenRegulars.AllProse(),
            line => line.EndsWith(CanteenTable.HandPlate, StringComparison.Ordinal));
    }

    /// <summary>§13.8, in the one room where somebody could talk. Nothing she says explains what the
    /// facility is for — the same grep the rest of this room's prose walks, over THIS file's own list, so a
    /// line added tomorrow is checked tomorrow.</summary>
    [Fact]
    public void NOTHING_SHE_SAYS_ExplainsAnything()
    {
        string[] forbidden =
        [
            "old one", "reever", "restore", "backup", "kaamos", "minister",
            "alien", "ancient", "experiment", "specimen",
        ];

        int lines = 0;
        foreach (string line in SittingAlone.AllProse())
        {
            Assert.False(string.IsNullOrWhiteSpace(line), "an empty string is in the prose list.");
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
            lines++;
        }

        Assert.True(lines > 15, $"only {lines} lines walked — this grep is not reading the room.");
    }
}
