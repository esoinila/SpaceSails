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

    // ── #783 · PLAIN WORDS, BECAUSE THE OWNER READ THEM COLD AND COULD NOT ────────────────────────────
    //
    // Owner, live at a table he had just sat down at: "What does the WAIT option mean here?" — twice — and
    // "Why not use words like SIT DOWN here if it means sitting down?" That is the red proof these guards
    // exist for. Every assertion below fails on the strings #778 shipped this morning.

    /// <summary>
    /// #783 · THE PROMPT AND THE BUTTONS SAY WHAT THEY DO.
    ///
    /// <para><b>Proven RED</b> on #778's own strings: <c>FreeTablePlate</c> was <c>"🪑 A FREE TABLE"</c> and
    /// the wait label was <c>"Wait"</c>, so the first two assertions here fail on that build with
    /// <i>Assert.Contains() Failure: Sub-string not found — String: "🪑 A FREE TABLE"</i>.</para>
    /// </summary>
    [Fact]
    public void THE_PROMPT_AND_THE_BUTTONS_SayWhatTheyDo()
    {
        // (1) The E-prompt names the ACTION, and never the inventory verb the first draft reached for.
        Assert.Contains("SIT DOWN", SittingAlone.FreeTablePlate, StringComparison.Ordinal);
        Assert.DoesNotContain("take the table", SittingAlone.FreeTablePlate, StringComparison.OrdinalIgnoreCase);

        // (3) …and the passive verb says what waiting is FOR. "Wait" alone reads as a loading verb.
        string wait = SittingAlone.LabelOf(SittingAlone.Wait);
        Assert.NotEqual("Wait", wait);
        Assert.Contains("SIT A WHILE", wait, StringComparison.Ordinal);
        Assert.Contains("see who comes", wait, StringComparison.Ordinal);

        // (4) "Stand up" stays — owner's own words. It was reaching the default arm and rendering as the
        // courtesy you owe SOMEBODY ELSE'S table, which is #746's line and should stay #746's.
        Assert.Equal("Stand up", SittingAlone.LabelOf(SittingAlone.Stand));
        Assert.NotEqual(SittingAlone.LabelOf(SittingAlone.Stand), CanteenTable.LabelOf(CanteenTable.Leave));

        // And nothing on the solo panel is a bare one-word verb any more — the shape of the complaint.
        foreach (Encounter.Move m in SittingAlone.TheTable().Moves)
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Label), $"{m.Id} has no label at all.");
            Assert.Contains(' ', m.Label);
        }
    }

    /// <summary>
    /// #783 · THE FIRST LINE CONFIRMS THE KEY WORKED.
    ///
    /// <para>Owner: <i>"On sitting, the panel's FIRST line must confirm the state change… so the player knows
    /// E worked BEFORE seeing more verbs."</i></para>
    ///
    /// <para><b>Proven RED</b> on #778: the opening began <i>"You take the chair with your back to the
    /// wall…"</i>, so <c>Assert.StartsWith</c> fails with <i>Expected: "You sit down. The table is yours."
    /// Actual: "You take the chair with your back to the wall…"</i>.</para>
    /// </summary>
    [Fact]
    public void SITTING_DOWN_ConfirmsTheStateChangeInItsFirstWords()
    {
        Assert.Contains("sit down", SittingAlone.SatDownLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("table is yours", SittingAlone.SatDownLine, StringComparison.OrdinalIgnoreCase);

        Assert.StartsWith(SittingAlone.SatDownLine, SittingAlone.TookTheTableLine, StringComparison.Ordinal);
        Assert.StartsWith(SittingAlone.SatDownLine, SittingAlone.TheTable().Opening, StringComparison.Ordinal);

        // …and the OTHER register confirms it in its own first words too, or the fix would hold on the wary
        // watch and quietly not on the quiet one — which is exactly how a UX repair half-ships.
        Assert.StartsWith(
            "It feels good to sit down", SittingAlone.RelaxedSitLine, StringComparison.Ordinal);
        Assert.StartsWith(
            "It feels good to sit down",
            SittingAlone.TheTable(relaxed: true).Opening,
            StringComparison.Ordinal);
    }

    // ── #783 · THE RELAXATION REGISTER ────────────────────────────────────────────────────────────────

    /// <summary>
    /// #783 · THE THREE LINES ARE THE OWNER'S, WORD FOR WORD.
    ///
    /// <para>They are canon-approved by authorship and filed on the issue for the build crew to LIFT. A
    /// paraphrase in the file is a paraphrase in the game, and nothing else in the suite would notice one —
    /// so the words themselves are pinned here.</para>
    /// </summary>
    [Fact]
    public void THE_REST_LINES_AreTheCanonOnesVerbatim()
    {
        Assert.Equal(
            "It feels good to sit down for a change. You put your boots up on the spare chair and let the " +
            "cold glass sweat into your hand, and for as long as it lasts, nobody in this building needs " +
            "anything from you.",
            SittingAlone.RelaxedSitLine);

        // …and the DRY opening, ruled at canon review: the glass in the line above is a REAL glass somebody
        // bought at the counter, so a rest with nothing in your hand gets its own words (#740 — a sentence
        // owns its own facts). Same boots, same chair, no drink anywhere in it.
        Assert.Equal(
            "It feels good to sit down for a change. You put your boots up on the spare chair, and for as " +
            "long as nobody needs you, nobody needs you.",
            SittingAlone.RelaxedSitDryLine);

        Assert.Equal(
            "The pour is cold and it is honest about what it is. Somewhere below B4, a still is doing its " +
            "quiet best for you.",
            SittingAlone.TheDrinkLine);

        Assert.Equal(
            "You put the chair back the way it was. The minute is over, and it was a good minute.",
            SittingAlone.StoodUpRelaxedLine);

        // The two openings are two openings, and each is picked by the one fact it is about.
        Assert.NotEqual(SittingAlone.RelaxedSitLine, SittingAlone.RelaxedSitDryLine);
        Assert.Equal(SittingAlone.RelaxedSitLine, SittingAlone.RelaxedOpening(true));
        Assert.Equal(SittingAlone.RelaxedSitDryLine, SittingAlone.RelaxedOpening(false));
    }

    /// <summary>
    /// #783 · WHICH REGISTER YOU GET IS THE ROOM'S ANSWER, AND BOTH ANSWERS ARE REAL.
    ///
    /// <para>Owner's condition, quoted: <i>"with a bought drink in hand, OR on a quiet watch."</i> The
    /// fifth-bug-class trap is a threshold that selects everything or nothing, so this walks the watches the
    /// game actually has and fails if either register is unreachable.</para>
    ///
    /// <para><b>Proven RED</b> by setting <c>SitReadsAsRelaxed</c> to <c>drinkInHand</c> alone: <i>no watch in the day
    /// is quiet enough to be a rest — the quiet-watch half of the owner's OR selects nothing.</i></para>
    /// </summary>
    [Fact]
    public void WHETHER_A_SIT_IS_A_REST_IsReachableBothWaysOnTheWatchesTheGameHas()
    {
        int rests = 0, watches = 0;
        long busiest = 0, quietest = 0;
        for (long w = 0; w < Watches; w++)
        {
            if (SittingAlone.SitReadsAsRelaxed(false, w))
            {
                rests++;
            }
            else
            {
                watches++;
            }
            if (SittingAlone.Fill(w) > SittingAlone.Fill(busiest))
            {
                busiest = w;
            }
            if (SittingAlone.Fill(w) < SittingAlone.Fill(quietest))
            {
                quietest = w;
            }
        }

        Assert.True(rests > 0,
            "no watch in the day is quiet enough to be a rest — the quiet-watch half of the owner's OR " +
            "selects nothing, and the relaxation register would ship unreachable without a purchase.");
        Assert.True(watches > 0,
            "every watch in the day is a rest — the wary take-line #778 shipped is now unreachable, which " +
            "is the same bug in the other direction.");

        // The busiest watch is a WATCH… until there is a glass in your hand, which is the owner's OR.
        Assert.False(SittingAlone.SitReadsAsRelaxed(false, busiest));
        Assert.True(SittingAlone.SitReadsAsRelaxed(true, busiest));

        // …and the sentences follow that one answer rather than restating it.
        Assert.Equal(SittingAlone.TookTheTableLine, SittingAlone.SatDown(false, busiest));
        Assert.Equal(
            SittingAlone.RelaxedSitLine + " " + SittingAlone.TheDrinkLine,
            SittingAlone.SatDown(true, busiest));
        Assert.Equal(SittingAlone.RelaxedSitDryLine, SittingAlone.SatDown(false, quietest));

        // THE DRINK'S OWN LINE IS ONLY SAID WHEN A DRINK WAS BOUGHT. A panel that narrated a pour nobody
        // paid for would be the sim doing one thing while a sentence reported another (bug class three).
        Assert.DoesNotContain(SittingAlone.TheDrinkLine, SittingAlone.SatDown(false, quietest),
            StringComparison.Ordinal);
        Assert.DoesNotContain(SittingAlone.TheDrinkLine, SittingAlone.SatDown(false, busiest),
            StringComparison.Ordinal);

        // …AND NEITHER IS THE GLASS IN THE OPENING (#740, canon review's ruling on this very PR). The filed
        // relaxed line names a cold glass sweating into your hand; on a quiet watch with nothing bought,
        // there is no glass, and a sentence that describes one is a sentence that does not own its facts.
        // Walk EVERY watch, both hands, so this cannot be true of the one pair the guard happened to pick.
        for (long w = 0; w < Watches; w++)
        {
            Assert.DoesNotContain("glass", SittingAlone.SatDown(false, w), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pour", SittingAlone.SatDown(false, w), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("drink", SittingAlone.SatDown(false, w), StringComparison.OrdinalIgnoreCase);

            // …and with a glass in hand the sit says so, on every watch there is, because that is the half
            // of the law that would otherwise be satisfied by a scene that never mentions a drink at all.
            Assert.Contains("glass", SittingAlone.SatDown(true, w), StringComparison.OrdinalIgnoreCase);
            Assert.Contains(SittingAlone.TheDrinkLine, SittingAlone.SatDown(true, w), StringComparison.Ordinal);
        }

        // The boots are the REST and survive both hands; only the glass is conditional.
        Assert.Contains("boots up on the spare chair", SittingAlone.RelaxedSitDryLine, StringComparison.Ordinal);
        Assert.Contains("boots up on the spare chair", SittingAlone.RelaxedSitLine, StringComparison.Ordinal);

        // And getting up is a different sentence out of a rest than out of a watch.
        Assert.Equal(SittingAlone.StoodUpRelaxedLine, SittingAlone.StoodUp(true));
        Assert.Equal(SittingAlone.StoodUpLine, SittingAlone.StoodUp(false));
        Assert.Equal(
            SittingAlone.StoodUpRelaxedLine,
            SittingAlone.TheTable(relaxed: true).Moves.Single(m => m.Id == SittingAlone.Stand).Says);
    }

    /// <summary>#783 · TWO STATES, TWO PICTURES — and the panel is told which by the same flag the prose is
    /// chosen with. A single image would have been the whole ask half-done; two images off two different
    /// answers would draw an empty chair under a sentence about boots being on it.</summary>
    [Fact]
    public void THE_TWO_STATES_WearTwoDifferentPictures()
    {
        Assert.NotEqual(SittingAlone.WaitingArtUrl, SittingAlone.RestingArtUrl);
        Assert.StartsWith("art/", SittingAlone.WaitingArtUrl, StringComparison.Ordinal);
        Assert.StartsWith("art/", SittingAlone.RestingArtUrl, StringComparison.Ordinal);

        Assert.Equal(SittingAlone.WaitingArtUrl, SittingAlone.ArtFor(false));
        Assert.Equal(SittingAlone.RestingArtUrl, SittingAlone.ArtFor(true));
    }

    /// <summary>
    /// #783/#759 · NOTHING ON A TABLE IN THIS HALL IS CLOTH.
    ///
    /// <para>Owner's no-tablecloths ruling on #759 is an ART spec — "shotcrete-spec, no tablecloths", steel
    /// tables under a riveted window wall — and #778's emptied-hall line said <i>"The linen is clean on every
    /// table you can see, and it has been clean a while"</i>. The picture and the sentence were describing two
    /// different buildings, and the sentence was the one that shipped into the player's hands.</para>
    ///
    /// <para><b>Driven, not read.</b> The forbidden words are checked against what
    /// <see cref="SittingAlone.NobodyCame"/> actually HANDS BACK over every watch the hall has and a run of
    /// beats, as well as against the <see cref="SittingAlone.AllProse"/> list — because a canon grep over a
    /// list is only as honest as the list's reachability, and this one proves the pool it is grepping is a
    /// pool the room can produce.</para>
    ///
    /// <para>#941 · WIDENED, because the first sweep was aimed at ONE pool and two other lines in the same
    /// hall still handed the player cloth: <see cref="UndergroundComplex.CantinaHallCard"/> opened on
    /// <i>"linen on the tables"</i>, and bark thirteen of <see cref="CanteenRegulars.Barks"/> was <i>"Table
    /// linen on a freight stop."</i> A canon sweep that covers the pool the last slip was found in is a
    /// guard shaped like its own bug report, so this one now walks every line the hall can say: the entry
    /// card, every bark, every stranger plate, and the waiting prose.</para>
    ///
    /// <para><b>Proven RED</b> by restoring either old line — the #778 line into <c>NobodyCameQuiet</c>, the
    /// <i>linen on the tables</i> clause into the card, or the <i>Table linen</i> bark into the pool: the
    /// assert fails with <i>Assert.DoesNotContain() Failure: Sub-string found · found: "linen"</i>.</para>
    /// </summary>
    [Fact]
    public void NOTHING_ON_A_TABLE_IN_THIS_HALL_IsCloth()
    {
        string[] forbidden =
            ["linen", "tablecloth", "table cloth", "cloth on the table", "napkin", "napery"];

        // What the room can actually say to a captain who sits and waits. Every watch the fill bill has, a
        // run of beats past the length of every pool, and both sides of the cabinet door.
        var reachable = new List<string>();
        for (long watch = 0; watch < CanteenRegulars.WatchFill.Count; watch++)
        {
            for (int beat = 0; beat < 8; beat++)
            {
                reachable.Add(SittingAlone.NobodyCame(watch, beat));
                reachable.Add(SittingAlone.NobodyCame(watch, beat, quiet: true));
            }
        }

        // ── ANTI-VACUOUS ──────────────────────────────────────────────────────────────────────────────
        //
        // A grep that never reaches the emptied hall would stay green with the linen line sitting in it. So
        // the sweep must be shown to have walked ALL THREE pools, and to have produced every single line of
        // the quiet one — the pool the slip was in.
        Assert.Contains(SittingAlone.NobodyCameBusy, line => reachable.Contains(line));
        Assert.Contains(SittingAlone.NobodyCameCabinet, line => reachable.Contains(line));
        foreach (string quiet in SittingAlone.NobodyCameQuiet)
        {
            Assert.Contains(quiet, reachable);
        }

        // ── #941 · THE REST OF THE HALL'S VOICE ───────────────────────────────────────────────────────
        //
        // The entry card the room opens with, and every line a stranger in it can say. Both slips this
        // sweep was widened for lived here, not in the waiting pool above.
        var swept = reachable
            .Concat(SittingAlone.AllProse())
            .Concat([UndergroundComplex.CantinaHallCard])
            .Concat(CanteenRegulars.AllStrangerProse())
            .ToList();

        // ── ANTI-VACUOUS, part two ────────────────────────────────────────────────────────────────────
        //
        // A sweep handed an empty card or an empty pool would grep nothing and stay green forever, so say
        // out loud that the card is THE card and that every bark the hall draws from went through.
        Assert.Contains("Carriers' canteen", UndergroundComplex.CantinaHallCard, StringComparison.Ordinal);
        Assert.Contains(UndergroundComplex.CantinaHallCard, swept);
        Assert.True(CanteenRegulars.Barks.Count >= 14,
            $"only {CanteenRegulars.Barks.Count} barks in the pool — the hall lost its voice.");
        int barksSwept = CanteenRegulars.Barks.Count(swept.Contains);
        Assert.True(barksSwept == CanteenRegulars.Barks.Count,
            $"the sweep only read {barksSwept} of {CanteenRegulars.Barks.Count} barks.");

        foreach (string line in swept)
        {
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        // And the owner-authored replacements themselves, verbatim, so a later tidy cannot paraphrase the
        // ruling back out of the room.
        Assert.Contains(
            "Nobody comes. Every table you can see is wiped steel, and it has been wiped a while.",
            SittingAlone.NobodyCameQuiet);
        Assert.Contains(
            "steel tables wiped to a shine, brass on the pillars, light somebody chose.",
            UndergroundComplex.CantinaHallCard,
            StringComparison.Ordinal);
        Assert.Contains(
            "Brass pillars on a freight stop. I stopped asking the questions I like the answers to.",
            CanteenRegulars.Barks);
    }

    /// <summary>
    /// #1016 · ABOARD, THE SENTENCES ARE THE BOAT'S OWN — no still below B4, no building, no wary watch.
    ///
    /// <para>The #1019 crew sat at a top in the ship's own cantina with a tot poured and was told about
    /// <i>"somewhere below B4, a still"</i> — a basement three hundred million kilometres from the boat
    /// (bug class three: the sim doing one thing while a sentence reports another). And on a busy rota hour
    /// the same seat read WARY — back to the wall, hands where they can be seen — in a room with nobody
    /// aboard to see them. Two laws close both: aboard always reads relaxed, and the glass aboard gets the
    /// locker's sentence.</para>
    ///
    /// <para><b>Proven RED</b> twice, one revert per law: dropping the <c>aboard</c> clause from
    /// <c>SitReadsAsRelaxed</c> fails the every-watch walk on the busiest hour; dropping the aboard fork
    /// from <c>SitLine</c> fails on the still riding aboard.</para>
    /// </summary>
    [Fact]
    public void ABOARD_TheSitIsAlwaysARestAndTheGlassIsTheLockers()
    {
        for (long w = 0; w < Watches; w++)
        {
            Assert.True(SittingAlone.SitReadsAsRelaxed(false, w, aboard: true),
                $"watch {w} puts the captain's back to the wall of his own empty cantina.");
        }

        // The clause has to be DOING something: at least one watch ashore is a watch, or the aboard arm
        // above is selecting a world where everything was already a rest (bug class five).
        Assert.Contains(false,
            Enumerable.Range(0, Watches).Select(w => SittingAlone.SitReadsAsRelaxed(false, w)));

        // With a pour, both sentences are the boat's — and neither of the shore's rides along.
        string aboardGlass = SittingAlone.SitLine(relaxed: true, drinkInHand: true, aboard: true);
        Assert.Equal(SittingAlone.RelaxedSitAboardLine + " " + SittingAlone.TheDrinkAboardLine, aboardGlass);
        Assert.DoesNotContain("B4", aboardGlass, StringComparison.Ordinal);
        Assert.DoesNotContain("building", aboardGlass, StringComparison.OrdinalIgnoreCase);

        // Dry aboard is the shipped dry line ON PURPOSE — it owns no venue, so it needs no fork. Pinned so
        // a later hand does not fork it for symmetry and hand the canon sweep a twin with nothing in it.
        Assert.Equal(
            SittingAlone.RelaxedSitDryLine,
            SittingAlone.SitLine(relaxed: true, drinkInHand: false, aboard: true));

        // Ashore is word-for-word what it was before the boat learned to say anything.
        Assert.Equal(
            SittingAlone.RelaxedSitLine + " " + SittingAlone.TheDrinkLine,
            SittingAlone.SitLine(relaxed: true, drinkInHand: true));

        // And both new sentences are in the canon sweep, so tomorrow's forbidden-word rulings read them too.
        string[] swept = [.. SittingAlone.AllProse()];
        Assert.Contains(SittingAlone.TheDrinkAboardLine, swept);
        Assert.Contains(SittingAlone.RelaxedSitAboardLine, swept);
    }
}
