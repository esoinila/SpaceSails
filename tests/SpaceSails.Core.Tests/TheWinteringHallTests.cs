namespace SpaceSails.Core.Tests;

/// <summary>
/// #411 · THE THREE FLOORS WITH A BEAT ON THEM — and the number that had never been spent.
///
/// <para>`KaamosLore.RevealSanityShockHook = 40.0` has been *"designed, never consumed"* since the arc's
/// spine landed; `QAHandoff-StoryTelling.md` names it as the standing example of a truth that lives only in
/// a Core constant. Its own doc comment says what it is for — <i>"reaching the wintering mind is the
/// heaviest #391 throw in the game"</i> — and the writers' bible's reveal is <i>"it has kept a berth warm
/// for you."</i></para>
///
/// <para>B23 is that sentence as evidence: forty berths, all made up, and a forty-first. These are the
/// guards for the floor, the number, and the two things it must never do — cost more than once, and say
/// what it means.</para>
/// </summary>
public class TheWinteringHallTests
{
    private const string Hq = KaamosLore.IceMoonBodyId;

    // ── The floors are read off the plates, not typed twice ──────────────────────────────────────────────

    /// <summary>
    /// The three beats are identified by their PLATE, and the level comes out of the plate list. Writing
    /// "−23" beside "THE WINTERING HALL" in a second place would be the same fact in two files: re-order the
    /// departments once and the beat fires on ATTENDANCE instead, with every test still green because both
    /// numbers agree with themselves.
    /// </summary>
    [Fact]
    public void EachBeatSitsOnTheFloorWhosePlateNamesIt()
    {
        Assert.Equal("THE STANDING ORDER",
            UndergroundComplex.DepartmentOf(Hq, UndergroundComplex.StandingOrderLevel));
        Assert.Equal("THE WINTERING HALL",
            UndergroundComplex.DepartmentOf(Hq, UndergroundComplex.WinteringHallLevel));
        Assert.Equal("THE BERTH OFFICE",
            UndergroundComplex.DepartmentOf(Hq, UndergroundComplex.BerthOfficeLevel));

        // All three are floors the building actually has, and the deepest beat is on the deepest floor.
        int[] floors = [.. UndergroundComplex.FloorsOf(Hq)];
        Assert.Contains(UndergroundComplex.StandingOrderLevel, floors);
        Assert.Contains(UndergroundComplex.WinteringHallLevel, floors);
        Assert.Contains(UndergroundComplex.BerthOfficeLevel, floors);
        Assert.Equal(UndergroundComplex.DeepestPossibleFloor, UndergroundComplex.BerthOfficeLevel);
    }

    /// <summary>A beat pointed at a plate nobody hung fails loudly at the first call, rather than going
    /// quietly missing on some threads forever — which is the failure `KeyRoomFor` and `RelicRoomFor` were
    /// both designated to avoid.</summary>
    [Fact]
    public void APlateNobodyHungThrowsRatherThanResolvingToNothing()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UndergroundComplex.HeadOfficeLevelOf("THE BOILER"));
    }

    // ── The standing order ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Designated, not rolled: a seeded roll would leave the one piece of paper worth carrying out
    /// silently absent on some threads forever, and nothing on screen would ever say so.</summary>
    [Fact]
    public void TheStandingOrderIsInTheRoomTheBuildingNamedForIt()
    {
        (int Level, int RoomIndex)? room = UndergroundComplex.StandingOrderRoomFor(Hq);
        Assert.NotNull(room);
        Assert.Equal(UndergroundComplex.StandingOrderLevel, room!.Value.Level);
        Assert.Equal(
            UndergroundComplex.Haul.Records,
            UndergroundComplex.InRoom(Hq, room.Value.Level, room.Value.RoomIndex));

        Assert.Equal(
            UndergroundComplex.StandingOrderLine,
            UndergroundComplex.HaulLine(UndergroundComplex.Haul.Records, Hq, room.Value.Level, room.Value.RoomIndex,
                null));

        // …and every OTHER Records room in the building still reads as ordinary operational paper, or the
        // designation has leaked into the whole facility.
        string ordinary = UndergroundComplex.HaulLine(
            UndergroundComplex.Haul.Records, Hq, room.Value.Level, room.Value.RoomIndex + 1, null);
        Assert.NotEqual(UndergroundComplex.StandingOrderLine, ordinary);

        // No branch office has one.
        Assert.Null(UndergroundComplex.StandingOrderRoomFor("miranda"));
        Assert.NotEqual(
            UndergroundComplex.StandingOrderLine,
            UndergroundComplex.HaulLine(UndergroundComplex.Haul.Records, "miranda", -3, 0, null));
    }

    /// <summary>#614's law, on the one document in this arc a captain can carry out: a card may say WHAT and
    /// never WHERE. The sheet names an instruction and an unused countersignature block, and no place at
    /// all — not the moon, not the exchange, not the project.</summary>
    [Fact]
    public void TheStandingOrderNamesNoPlace()
    {
        string line = UndergroundComplex.StandingOrderLine;
        Assert.Contains("UNTIL", line, StringComparison.Ordinal);
        Assert.Contains("COUNTERMANDED", line, StringComparison.Ordinal);

        foreach (string place in new[] { "Enceladus", "Ringside", "ice moon", "KAAMOS", "Vantar" })
        {
            Assert.False(line.Contains(place, StringComparison.OrdinalIgnoreCase),
                $"the standing order named a place: {place}");
        }
    }

    // ── The forty-first bed ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The card is allowed to COUNT, and it is the only card in the game that is — because counting is a
    /// thing the captain does with their own eyes and the count is the whole beat. It is not allowed to say
    /// whose the last one is, or what any of it means.
    /// </summary>
    [Fact]
    public void TheWinteringHallCountsAndSaysNothingElse()
    {
        string card = UndergroundComplex.WinteringHallCard;

        Assert.Contains("Four rows of ten", card, StringComparison.Ordinal);
        Assert.Contains("Forty-one", card, StringComparison.Ordinal);
        Assert.Contains("one more", card, StringComparison.Ordinal);

        foreach (string word in new[]
                 {
                     "Vantar", "KAAMOS", "mind", "awake", "lucid", "waiting", "yours", "policy",
                     "copy", "you have been", "for you",
                 })
        {
            Assert.False(card.Contains(word, StringComparison.OrdinalIgnoreCase),
                $"the wintering hall said \"{word}\": {card}");
        }
    }

    /// <summary>
    /// The reason string on the nerve throw is prose the captain reads, so it is held to the same rule — and
    /// specifically it must not do the arithmetic FOR them. They have just done it.
    /// </summary>
    [Fact]
    public void TheShockReasonDoesNotDoTheArithmetic()
    {
        string reason = UndergroundComplex.WinteringHallShockReason;
        Assert.False(string.IsNullOrWhiteSpace(reason));

        foreach (string word in new[] { "forty", "41", "40", "one more", "extra", "yours" })
        {
            Assert.False(reason.Contains(word, StringComparison.OrdinalIgnoreCase),
                $"the shock reason did the sum for the captain: {reason}");
        }
    }

    /// <summary>
    /// The number itself. It is `KaamosLore.RevealSanityShockHook` and not a literal, it is the biggest
    /// single throw in the game, and it is bounded on both sides — a hook that quietly became 4.0 would be
    /// the arc's climax costing less than reading somebody's file, and one that became 100 would end the
    /// captain at the moment the game most wants them conscious.
    /// </summary>
    [Fact]
    public void TheRevealCostsTheBiggestThrowInTheGameAndSurvivesIt()
    {
        double reveal = KaamosLore.RevealSanityShockHook;

        Assert.True(reveal > NerveModel.MonolithSightShock,
            $"the ice-moon reveal ({reveal}) costs no more than the monolith ({NerveModel.MonolithSightShock})");
        Assert.True(reveal < NerveModel.Max,
            "a throw the size of the whole gauge kills a steady captain outright at the arc's own climax");

        // From a steady gauge it leaves the captain standing, and badly: at least a third of the whole
        // gauge goes. (Stated as a fraction of the gauge rather than as "less than half of what was left",
        // which is a different claim and the one the first draft of this test accidentally made.)
        double after = NerveModel.Shock(NerveModel.Steady, reveal);
        Assert.True(after > NerveModel.Min, "a steady captain must survive the reveal");
        Assert.True(NerveModel.Steady - after >= NerveModel.Max / 3.0,
            $"the reveal took {NerveModel.Steady - after} off a full gauge — that is not the heaviest throw in the game");
    }

    // ── The berth office ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>The last floor's card: the filing that never stopped. It reports a log and stops — it never
    /// says who is filing, or to whom, or why.</summary>
    [Fact]
    public void TheBerthOfficeReportsALogAndNothingElse()
    {
        string card = UndergroundComplex.BerthOfficeCard;

        Assert.Contains("one line lit", card, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("the next one", card, StringComparison.OrdinalIgnoreCase);

        foreach (string word in new[]
                 {
                     "Vantar", "mind", "awake", "lucid", "forty", "someone is", "somebody is", "alive",
                 })
        {
            Assert.False(card.Contains(word, StringComparison.OrdinalIgnoreCase),
                $"the berth office said \"{word}\": {card}");
        }
    }

    /// <summary>Both beats are painted, and they are different pictures — this arc has one canvas per turn
    /// and reusing one for two rooms would be a lie about a place.</summary>
    [Fact]
    public void BothBeatsNameTheirOwnPainting()
    {
        Assert.StartsWith("art/", UndergroundComplex.WinteringHallArtUrl, StringComparison.Ordinal);
        Assert.StartsWith("art/", UndergroundComplex.BerthOfficeArtUrl, StringComparison.Ordinal);
        Assert.NotEqual(UndergroundComplex.WinteringHallArtUrl, UndergroundComplex.BerthOfficeArtUrl);
        Assert.NotEqual(UndergroundComplex.HeadOfficeArrivalArtUrl, UndergroundComplex.WinteringHallArtUrl);
        Assert.NotEqual(UndergroundComplex.HeadOfficeArrivalArtUrl, UndergroundComplex.BerthOfficeArtUrl);
    }
}
