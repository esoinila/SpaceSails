using System.Linq;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1199 / #1062 slice 1 · <b>THE OBSERVATION WALK</b>, held to the six things it claims.
///
/// <para>Owner, 2026-09-13: <i>"we shadow somebody into a dead end and once we get there there is nothing
/// there … those moments are narratively great."</i> The moment is worth nothing if any of the following is
/// not true, so each one is a separate fact with a separate revert behind it:</para>
///
/// <list type="number">
/// <item>the room has exactly one doorway AND passes the fire code by a NAMED exception, not by a number;</item>
/// <item>every exception on that list is named and reasoned, and the underground's own answer did not move;</item>
/// <item>the notice question IS the observation roll — asserted by calling both and comparing;</item>
/// <item>the person of interest is deterministic per universe and comes from the named cast;</item>
/// <item>the beat is spent once and the spend round-trips a real save file;</item>
/// <item>the prose is enumerated, printed as authored, and free of the reserved word and the pattern.</item>
/// </list>
/// </summary>
public sealed class TheWalkIsEmptyTests
{
    // ── 1 · THE ROOM ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · ONE DOORWAY, AND THE FIRE CODE LETS IT OFF BY NAME. Three claims that only mean something
    /// together: the room has one way out, one way out is NOT enough on its own, and the thing that saves it
    /// is an entry on the exemption list rather than its size.
    ///
    /// <para><b>The anti-vacuity clause is the middle assertion.</b> Without it this test would pass on a
    /// fire code that exempted everything — the fifth named bug class, a threshold that selects the whole
    /// world. So the same door count is asked with no exemption and must be REFUSED.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>MeetsFireCode(exits, exemption)</c> written as
    /// <c>exits >= FireCodeMinExits</c> (the named exemption ignored) — <i>"one doorway is not two, so the
    /// walk only stands up if the law lets it off BY NAME"</i>.</para>
    /// </summary>
    [Fact]
    public void TheWalkHasOneDoorwayAndPassesTheFireCodeByItsNamedException()
    {
        Assert.Equal(1, ObservationWalk.Doorways);

        // It is not two, and the law says so with nothing else in its hand.
        Assert.False(
            UndergroundComplex.MeetsFireCode(
                ObservationWalk.Doorways, UndergroundComplex.FireCodeExemption.None),
            "one doorway is not two, so the walk only stands up if the law lets it off BY NAME.");

        // And no NUMBER could ever let it off: it is three times the length the dimensional exemption is
        // stated in, which is the argument for having a named one at all.
        Assert.True(ObservationWalk.LengthDu > UndergroundComplex.FireCodeSmallRoomDu,
            "a walk that was bedroom-small would need no exemption of its own — and would be no walk.");

        // The named exemption is the one thing that carries it.
        Assert.Equal(UndergroundComplex.FireCodeExemption.ObservationWalk, ObservationWalk.Exemption);
        Assert.True(UndergroundComplex.MeetsFireCode(ObservationWalk.Doorways, ObservationWalk.Exemption));
    }

    /// <summary>
    /// #822 · EVERY EXEMPTION IS NAMED AND REASONED. The list is allowed to grow; it is not allowed to grow
    /// a member somebody added without an argument, because that is how a standing law becomes a default.
    ///
    /// <para><b>Revert that reddened it:</b> the <c>ObservationWalk</c> arm deleted from
    /// <c>ReasonFor</c> so it fell to <c>_ => ""</c> — <i>"FireCodeExemption.ObservationWalk is on the list
    /// with no reason beside it"</i>.</para>
    /// </summary>
    [Fact]
    public void EveryFireCodeExemptionIsNamedAndReasoned()
    {
        var members = Enum.GetValues<UndergroundComplex.FireCodeExemption>();
        Assert.True(members.Length >= 3, "the exemption list lost a member");

        foreach (UndergroundComplex.FireCodeExemption exemption in members)
        {
            string reason = UndergroundComplex.ReasonFor(exemption);
            if (exemption == UndergroundComplex.FireCodeExemption.None)
            {
                Assert.Equal("", reason);
                Assert.False(UndergroundComplex.MeetsFireCode(1, exemption));
                continue;
            }

            Assert.False(string.IsNullOrWhiteSpace(reason),
                $"FireCodeExemption.{exemption} is on the list with no reason beside it.");
            Assert.True(UndergroundComplex.MeetsFireCode(1, exemption),
                $"FireCodeExemption.{exemption} is on the list and does not let a one-door room off.");
        }
    }

    /// <summary>
    /// #822/#1199 · AND THE BUILDING'S OWN ANSWER DID NOT MOVE. <c>Room.MeetsFireCode</c> used to spell the
    /// law out for itself; it now asks the law. Every room on every floor of every site in the sweep must
    /// still give byte-for-byte the answer the old sentence gave.
    ///
    /// <para><b>The claim that can actually fail is the ATTRIBUTION</b>, not the verdict. Comparing
    /// <c>MeetsFireCode</c> against <c>BedroomSmall || Exits >= 2</c> over the real building cannot go red
    /// however the exemption is mis-wired, because the building satisfies the law: there is no room down
    /// there with one door and a long wall for the two readings to disagree about. That is the fifth named
    /// bug class — a guard whose world cannot tell pass from fail — and it was measured here, not guessed:
    /// the first version of this test stayed GREEN with <c>Room.Exemption</c> returning
    /// <c>ObservationWalk</c> for every room in the game.</para>
    ///
    /// <para>So the law asserted is the one a mis-wiring breaks: <b>no underground room is ever let off
    /// under a name that is not its own.</b> Nothing down there is a dead end on purpose, so the only
    /// exemption any of them may claim is the dimensional one, and it must claim it exactly when it is
    /// small. Anti-vacuity is two counts that both have to move.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>Room.Exemption</c> returning
    /// <c>FireCodeExemption.ObservationWalk</c> for every room — <i>"a room down here is let off under a
    /// name that is not its own"</i>, thousands of times.</para>
    /// </summary>
    [Fact]
    public void TheUndergroundsOwnAnswerIsUnchanged()
    {
        // The law's own truth table first, where a wrong answer IS reachable.
        Assert.False(UndergroundComplex.MeetsFireCode(0, UndergroundComplex.FireCodeExemption.None));
        Assert.False(UndergroundComplex.MeetsFireCode(1, UndergroundComplex.FireCodeExemption.None));
        Assert.True(UndergroundComplex.MeetsFireCode(2, UndergroundComplex.FireCodeExemption.None));
        Assert.True(UndergroundComplex.MeetsFireCode(1, UndergroundComplex.FireCodeExemption.BedroomSmall));

        int held = 0, exempt = 0;
        var wrong = new List<string>();
        foreach (string body in new[] { "luna", "phobos", "europa", "titan", "miranda", "the-clinker" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor =
                    UndergroundComplex.Build(body, level, SurfaceLayout.DefaultField);
                foreach (UndergroundComplex.Room room in floor.TheRooms)
                {
                    UndergroundComplex.FireCodeExemption should = room.BedroomSmall
                        ? UndergroundComplex.FireCodeExemption.BedroomSmall
                        : UndergroundComplex.FireCodeExemption.None;

                    if (room.Exemption != should && wrong.Count < 8)
                    {
                        wrong.Add($"{body} B{-level} · {room.WidthDu:F1} x {room.DepthDu:F1} du is let off as "
                                  + $"{room.Exemption} when it is {should}");
                    }

                    if (room.MeetsFireCode
                        != (room.BedroomSmall || room.Exits >= UndergroundComplex.FireCodeMinExits))
                    {
                        wrong.Add($"{body} B{-level}: the verdict moved");
                    }

                    if (room.BedroomSmall)
                    {
                        exempt++;
                    }
                    else
                    {
                        held++;
                    }
                }
            }
        }

        Assert.True(held > 500, $"the sweep only held {held} rooms to the law — it is not measuring the building.");
        Assert.True(exempt > 0, "the sweep let nothing off, so the exemption is not being exercised at all.");
        Assert.True(wrong.Count == 0,
            "a room down here is let off under a name that is not its own:\n  " + string.Join("\n  ", wrong));
    }

    // ── 2 · THE TAIL ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>THE NOTICE QUESTION IS THE OBSERVATION ROLL</b>, and this asserts it by CALLING both and
    /// comparing the answers — not by reading the source, and not by re-deriving the odds here (a test that
    /// re-derived them would be the second authority the law forbids, and would agree with a bug).
    ///
    /// <para>Swept over ranges from point-blank to past the long look, over still / walking / running, and
    /// over a run of sim seconds long enough to turn the look cadence over many times — so the comparison
    /// covers rolled looks, un-rolled looks inside a cadence and the no-sightline refusal.</para>
    ///
    /// <para>Anti-vacuity: the sweep must contain at least one look that actually cast a die and at least
    /// one that noticed, or it has compared two silences.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>TheTail.Notice</c> passing
    /// <c>ReeverObservation.Doing.Digging</c> instead of <c>Nothing</c> — a modifier the world cannot
    /// produce on a concourse, and the sweep disagreed on 400-odd looks.</para>
    /// </summary>
    [Fact]
    public void TheNoticeQuestionCallsTheObservationRollTheGameAlreadyHas()
    {
        ulong seed = TheTail.SeedFor(ObservationWalk.HavenId, "GILT-EYE");
        int rolled = 0, noticed = 0, compared = 0;

        foreach (bool line in new[] { true, false })
        {
            foreach (double range in new[] { 1.0, 4.0, 9.0, 18.0, 30.0, 64.0 })
            {
                foreach (double speed in new[] { 0.0, 2.0, 9.0 })
                {
                    long index = long.MinValue;
                    for (int tick = 0; tick < 40; tick++)
                    {
                        double at = tick * 0.4;

                        ReeverObservation.Glance mine =
                            TheTail.Notice(line, false, seed, at, index, range, speed);
                        ReeverObservation.Glance theirs = ReeverObservation.Look(
                            line, false, seed, at, index,
                            new ReeverObservation.View(range, speed, ReeverObservation.Doing.Nothing));

                        // Field by field, and not record equality: a Glance carries the modifier LIST the
                        // roll was cast with, and two lists built from identical numbers are two objects.
                        // Comparing the records would have compared references and failed on a rule that is
                        // in fact identical — which is the opposite failure from the one this test is for.
                        Assert.Equal(theirs.State, mine.State);
                        Assert.Equal(theirs.LookIndex, mine.LookIndex);
                        Assert.Equal(theirs.Rolled, mine.Rolled);
                        Assert.Equal(theirs.Roll?.Face, mine.Roll?.Face);
                        Assert.Equal(theirs.Roll?.Seed, mine.Roll?.Seed);
                        Assert.Equal(theirs.Roll?.Total, mine.Roll?.Total);
                        Assert.Equal(
                            theirs.Roll?.Modifiers.Select(m => $"{m.Label}{m.Value}"),
                            mine.Roll?.Modifiers.Select(m => $"{m.Label}{m.Value}"));
                        compared++;

                        if (mine.Rolled)
                        {
                            rolled++;
                        }

                        if (TheTail.HasNoticed(in mine))
                        {
                            noticed++;
                        }

                        index = mine.LookIndex;
                    }
                }
            }
        }

        Assert.True(compared > 1000, "the sweep is too small to have covered the cadence.");
        Assert.True(rolled > 100, "no die was ever cast — the sweep compared two silences.");
        Assert.True(noticed > 0, "nobody was ever noticed — the roll cannot tell pass from fail here.");
    }

    /// <summary>
    /// #1062/#1074 · THE PERSON OF INTEREST IS DETERMINISTIC PER UNIVERSE AND COMES FROM THE NAMED CAST.
    /// #1074's first law forbids a named watcher's man, so the only cast a tail may draw on is the one the
    /// room already has — and it must name the same person on every call, on every runtime, for ever.
    ///
    /// <para>At the haven that has the walk it must also be the regular the rota is MOST likely to seat
    /// there, or the tail names somebody who is hardly ever in the room to be followed.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the choice made by a plain seeded roll over the roster instead
    /// of by affinity — <i>"the tail names a regular the rota does not favour here"</i>.</para>
    /// </summary>
    [Fact]
    public void ThePersonOfInterestIsDeterministicAndFromTheNamedCast()
    {
        foreach (string body in new[] { ObservationWalk.HavenId, "red-eye", "the-deep", "cinder-roost" })
        {
            string who = TheTail.ThePersonOfInterest(body);
            Assert.Contains(who, PatronRota.Roster);
            Assert.Equal(who, TheTail.ThePersonOfInterest(body));

            double best = PatronRota.Roster.Max(r => PatronRota.Affinity(r, body));
            Assert.Equal(best, PatronRota.Affinity(who, body));
        }

        // …and the cast is a cast: the four ports above must not all name one person, or the "named cast"
        // half of this law is decoration.
        Assert.True(
            new[] { ObservationWalk.HavenId, "red-eye", "the-deep", "cinder-roost" }
                .Select(TheTail.ThePersonOfInterest).Distinct(StringComparer.Ordinal).Count() > 1,
            "every port named the same person — the roster is not being read.");

        // The tail says nothing, ever. There is no prose in it to sweep and that is the point.
        Assert.Empty(TheTail.AllProse());
    }

    // ── 3 · THE WAIT ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · THE WAIT IS A FRACTION OF THE ROOM'S OWN WATCH AND NOT A NUMBER OF ITS OWN.
    ///
    /// <para><b>A value assertion cannot carry this one, and that was measured.</b> A wait written as a
    /// literal is the same number as its fraction on the day it ships, so a plain
    /// <c>Assert.Equal(…Seconds, …)</c> stayed green on exactly the revert this guard exists to catch. What
    /// is wrong with a literal is not its value, it is that it stops meaning anything about a watch the day
    /// a watch changes length — so the thing asserted has to be the DERIVATION, which lives in the source.
    /// Both halves are here: the numbers agree, and the expression that makes them agree is the rota's own.</para>
    ///
    /// <para><b>#1199, second pass — WHOSE fraction it is.</b> It was the escort's
    /// (<see cref="Escort.PatienceFraction"/>), read from the other side of the same fiction, and the owner
    /// played it at warp 1 and said to shorten it. It is now the walk's own — see
    /// <see cref="TheWaitCanBeSatThroughAtWarpOne"/> for the band it is chosen against and
    /// <see cref="TheEscortsOwnPatienceDidNotMove"/> for the half of this change that is about everything it
    /// did NOT touch.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <c>TheWaitSeconds => 180.0</c> — <i>"the wait is a number of
    /// its own"</i>.</para>
    /// </summary>
    [Fact]
    public void TheWaitIsAWatchFractionAndNeverASecondConstant()
    {
        Assert.Equal(PatronRota.WatchSeconds * ObservationWalk.WaitFraction, ObservationWalk.TheWaitSeconds);
        Assert.True(ObservationWalk.TheWaitSeconds > 0);
        Assert.True(ObservationWalk.TheWaitSeconds < PatronRota.WatchSeconds,
            "a wait as long as a whole watch is not a fraction of one — the room would forget first.");

        string src = File.ReadAllText(
            Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Core", "ObservationWalk.cs"));
        Assert.Contains(
            "TheWaitSeconds => Interior.PatronRota.WatchSeconds * WaitFraction;", src, StringComparison.Ordinal);
    }

    /// <summary>
    /// #1199 · <b>IT CAN BE SAT THROUGH AT WARP 1, AND THAT IS THE WHOLE OF THE OWNER'S ASK.</b>
    ///
    /// <para>Every NPC-patience clock in this game is SIM time, and warp 1 is one sim second per second of
    /// the player's own evening — so the wall-clock reading of this constant is not a derived curiosity, it
    /// is the thing the owner played and the thing he asked to change. A BAND and not a point: long enough
    /// that the captain walks the tube, gets out to the rail and stands there a moment before the game admits
    /// nobody is coming, and short enough to be sat through rather than warped past.</para>
    ///
    /// <para><b>And where it is spent.</b> The walk is only dealt after last call
    /// (<see cref="Egress.LastCallFraction"/> of the shift), which leaves a quarter of a watch on the far
    /// side of it. The old wait was itself a quarter-watch — it filled that entire remainder exactly, which
    /// is the arithmetic that made an hour of wall clock the ordinary case rather than the worst one.</para>
    ///
    /// <para><b>Red:</b> put the wait back on <see cref="Escort.PatienceFraction"/> and both assertions here
    /// redden — 3,600 s of wall clock against a 300 s ceiling, and a wait ten times the room it is spent
    /// in.</para>
    /// </summary>
    [Fact]
    public void TheWaitCanBeSatThroughAtWarpOne()
    {
        const double simSecondsPerWallSecondAtWarpOne = 1.0;
        double wallClockSeconds = ObservationWalk.TheWaitSeconds / simSecondsPerWallSecondAtWarpOne;

        Assert.InRange(wallClockSeconds, 90, 300);

        double afterLastCall = PatronRota.WatchSeconds * (1.0 - Egress.LastCallFraction);
        Assert.True(ObservationWalk.TheWaitSeconds * 10 < afterLastCall,
            $"the wait is {ObservationWalk.TheWaitSeconds} s and the room has only {afterLastCall} s left "
            + "after last call — a wait that fills the window it is spent in is a beat the watch ends before "
            + "the captain does.");
    }

    /// <summary>
    /// #1199 · <b>AND NO OTHER PATIENCE CLOCK MOVED WITH IT.</b> The walk's wait used to BE
    /// <see cref="Escort.PatienceSeconds"/>, so the cheapest way to shorten it was to turn
    /// <see cref="Escort.PatienceFraction"/> down — and that would have quietly given every escort in the
    /// game three minutes of patience instead of an hour, out of a lane whose whole subject is one tube on
    /// one station. This is the law that says it did not happen.
    ///
    /// <para><b>Red:</b> <c>PatienceFraction = ObservationWalk.WaitFraction</c> — the shortcut — reddens
    /// here, and <see cref="TheWaitIsAWatchFractionAndNeverASecondConstant"/> beside it stays green, which is
    /// the argument for this test existing at all.</para>
    /// </summary>
    [Fact]
    public void TheEscortsOwnPatienceDidNotMove()
    {
        Assert.Equal(0.25, Escort.PatienceFraction);
        Assert.Equal(PatronRota.WatchSeconds * 0.25, Escort.PatienceSeconds);
        Assert.True(ObservationWalk.WaitFraction < Escort.PatienceFraction,
            "the walk is supposed to be the SHORT one — if it is not, this lane did nothing.");
        Assert.NotEqual(Escort.PatienceSeconds, ObservationWalk.TheWaitSeconds);
    }

    // ── 4 · SPENT ONCE, AND IT RIDES THE FILE ───────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · SPENT ONCE PER UNIVERSE, AND THE SPEND ROUND-TRIPS A REAL SAVE. The owner's whole argument is
    /// that the beat is used sparingly; a spend a reload forgot would hand the captain a second one.
    ///
    /// <para><b>Revert that reddened it:</b> <c>WouldSpend</c> ignoring <c>spentOn</c> — <i>"a spend that
    /// can happen twice is a mechanic, not a moment"</i>.</para>
    /// </summary>
    [Fact]
    public void TheBeatIsSpentOnceAndTheSpendRoundTripsTheVault()
    {
        string who = TheTail.ThePersonOfInterest(ObservationWalk.HavenId);
        string key = ObservationWalk.Key(ObservationWalk.HavenId, who);

        Assert.True(ObservationWalk.WouldSpend(ObservationWalk.HavenId, null));
        Assert.False(ObservationWalk.WouldSpend(ObservationWalk.HavenId, key),
            "a spend that can happen twice is a mechanic, not a moment.");
        Assert.False(ObservationWalk.WouldSpend("red-eye", null),
            "only one station in the game has the room.");

        Assert.False(ObservationWalk.IsSpent(null));
        Assert.True(ObservationWalk.IsSpent(key));
        Assert.True(ObservationWalk.IsSpentOn(key, ObservationWalk.HavenId, who));
        Assert.False(ObservationWalk.IsSpentOn(key, ObservationWalk.HavenId, "SOMEBODY ELSE"));

        // The sighting is owed once, and only after a spend.
        Assert.False(ObservationWalk.SightingIsOwed(null, null));
        Assert.True(ObservationWalk.SightingIsOwed(key, null));
        Assert.False(ObservationWalk.SightingIsOwed(key, "red-eye"));

        // …and both facts survive REAL bytes, through the serializer a save actually goes through.
        var vault = new Vault
        {
            Version = Vault.CurrentVersion,
            SavedSimTime = 12345.0,
            Progress = new ProgressSection
            {
                ObservationWalkSpentOn = key,
                ObservationWalkSightingAt = "red-eye",
            },
        };

        Vault back = VaultSerializer.Load(VaultSerializer.Save(vault));
        Assert.False(back.Tampered);
        Assert.Equal(key, back.Progress?.ObservationWalkSpentOn);
        Assert.Equal("red-eye", back.Progress?.ObservationWalkSightingAt);

        // A file written before this shipped carries neither field and loads with the beat unspent.
        Vault old = VaultSerializer.Load(VaultSerializer.Save(new Vault
        {
            Version = Vault.CurrentVersion, SavedSimTime = 1.0, Progress = new ProgressSection(),
        }));
        Assert.Null(old.Progress?.ObservationWalkSpentOn);
        Assert.Null(old.Progress?.ObservationWalkSightingAt);
    }

    // ── 5 · THE PROSE ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199/#741 · THE NOTE SAYS WHAT WAS SEEN AND IS FILED UNDER THE PERSON. The book's promise
    /// (<see cref="CaseSubjects.Person"/>) is that the sentence PRINTS the name it is filed under, and the
    /// note is four flat observations with no conclusion in it.
    ///
    /// <para><b>Revert that reddened it:</b> the subject minted as
    /// <c>CaseSubjects.Person(name.ToLowerInvariant())</c> — the sentence no longer printed what it was
    /// filed under, which is the exact promise #741 is built on.</para>
    /// </summary>
    [Fact]
    public void TheNoteSaysWhatWasSeenAndIsFiledUnderThePersonWhoPrintsInIt()
    {
        string who = TheTail.ThePersonOfInterest(ObservationWalk.HavenId);
        string note = ObservationWalk.NoteLine(who);

        Assert.Contains(who, note, StringComparison.Ordinal);
        Assert.Equal(CaseSubjects.Line(CaseSubjects.Person(who)), ObservationWalk.Subjects(who));

        // The promise, read the way the book reads it back.
        FieldNote filed = new(note, 1.0, "SELENE GATE", ObservationWalk.Glyph, ObservationWalk.Subjects(who));
        IReadOnlyList<CaseSubjects.Subject> on = CaseSubjects.On(in filed);
        Assert.Single(on);
        Assert.Equal(CaseSubjects.Kind.Person, on[0].Of);
        Assert.Contains(on[0].Name, note, StringComparison.Ordinal);

        // The note draws no conclusion. It is the only thing standing between this beat and an explanation.
        foreach (string settles in new[]
        {
            "vanish", "disappear", "impossible", "must have", "somehow", "there is no way", "explain",
        })
        {
            Assert.DoesNotContain(settles, note, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// #1199/#672 · NOTHING THIS BEAT PUBLISHES NAMES THE RESERVED THING OR THE PATTERN. §8's reserved word
    /// and the fifteen beside it, plus the words that would turn an absence into a claim — and the card's
    /// own title and caption are swept THROUGH <see cref="StoryBeats"/>, because that is where the player
    /// reads them.
    ///
    /// <para>Anti-vacuity: the prose list must have exactly the five authored strings in it. A feature that
    /// grew a sixth would be a sentence somebody wrote to fill a gap, which is how this beat dies.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the word <i>"impossible"</i> planted in the card body —
    /// <i>"a #1199 string settles what it must leave open"</i>.</para>
    /// </summary>
    [Fact]
    public void NoStringThisBeatPublishesNamesTheReservedThingOrThePattern()
    {
        string who = TheTail.ThePersonOfInterest(ObservationWalk.HavenId);

        string[] said =
        [
            .. ObservationWalk.AllProse(who),
            StoryBeats.Title(StoryBeats.Beat.TheObservationWalk),
            StoryBeats.Caption(StoryBeats.Beat.TheObservationWalk),
        ];

        Assert.Equal(5, ObservationWalk.AllProse(who).Count());

        // The card the player reads is the room's own authored text and never a second copy of it.
        Assert.Contains(ObservationWalk.CardTitle, StoryBeats.Title(StoryBeats.Beat.TheObservationWalk),
            StringComparison.Ordinal);
        Assert.Equal(ObservationWalk.CardBody, StoryBeats.Caption(StoryBeats.Beat.TheObservationWalk));
        Assert.Equal(ObservationWalk.ArtUrl, StoryBeats.ArtFile(StoryBeats.Beat.TheObservationWalk));
        Assert.Equal(StoryBeats.Cadence.OnceEver, StoryBeats.CadenceOf(StoryBeats.Beat.TheObservationWalk));

        string[] forbidden =
        [
            // §8's reserved word and the fifteen beside it.
            "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
            "artifact", "civilisation", "civilization", "millennia", "aeon", "eon",
            // …and the pattern this beat exists to leave un-named.
            "vanish", "disappear", "observable", "impossible", "hidden door", "secret",
        ];

        var named = new List<string>();
        foreach (string text in said)
        {
            foreach (string word in forbidden)
            {
                if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    named.Add($"\"{word}\" in \"{text}\"");
                }
            }
        }

        Assert.True(named.Count == 0,
            "a #1199 string settles what it must leave open:\n  " + string.Join("\n  ", named));
    }
}
