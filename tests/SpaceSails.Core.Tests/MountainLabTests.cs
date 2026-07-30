using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #409+ · THE MOUNTAIN LAB, AS LAWS. Owner, in five messages: <i>"We could have buildings with doors"</i>, <i>"a
/// secret lab that extends into a mountain at some reever infested site"</i>, <i>"Doors that lock is a cool feature
/// for doing a secret lab"</i>, <i>"Surely some control panels based on the vent panel can be added 🤠"</i>, and
/// <i>"Alarm system panel maybe … something to try to hack."</i>
/// </summary>
public sealed class MountainLabTests
{
    private static SecretLab.Region Lab(string body = "test-rock")
    {
        SurfaceLayout.Field field = new(-44, 34, -30, 30, 18, 0, 0);
        SecretLab.Placement p = SecretLab.For(body, field, forcePresent: true);
        return SecretLab.Build(body, field, p.DoorX, p.DoorY);
    }

    // ── Doors that lock ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A LOCK MUST BUY SOMETHING A SHUT DOOR DOES NOT, or it is decoration. So a shut door can be forced by the
    /// pack — they never operate a handle, they lean, and forty years has not made them impatient — and a locked
    /// one cannot. That single asymmetry is the whole feature.
    /// </summary>
    [Fact]
    public void ShutBuysTimeAndLockedBuysTheRoom()
    {
        Assert.True(LockedDoor.CanBeForced(LockedDoor.State.Shut));
        Assert.False(LockedDoor.CanBeForced(LockedDoor.State.Locked));
        Assert.False(LockedDoor.CanBeForced(LockedDoor.State.Open), "an open door is not forced, it is walked through");

        Assert.InRange(LockedDoor.ForceSeconds, 10, 60);
    }

    /// <summary>One straggler leaning on a door is not a crisis; the second one is what turns a delay into a
    /// countdown.</summary>
    [Fact]
    public void ItTakesMoreThanOneOfThemToWorkOnADoor()
    {
        Assert.False(LockedDoor.EnoughToForce(1));
        Assert.True(LockedDoor.EnoughToForce(LockedDoor.ForcedByAtLeast));
        Assert.InRange(LockedDoor.ForcedByAtLeast, 2, 3);
    }

    /// <summary>A shut door is a wall to the boot AND to the eye — the same rule the ship's hatches follow
    /// (#465). Opacity and solidity are different properties; a door happens to have both, and if these ever
    /// disagreed the pack would see through a door it could not open.</summary>
    [Fact]
    public void ADoorIsAsOpaqueAsItIsSolid()
    {
        foreach (LockedDoor.State s in Enum.GetValues<LockedDoor.State>())
        {
            Assert.Equal(LockedDoor.Passable(s), LockedDoor.Transparent(s));
        }
    }

    /// <summary>
    /// THE CARD WORKS BOTH WAYS, and that is what stops the lock being a free win. Without it a captain cannot
    /// lock and cannot unlock; with it they gain the power to shut the pack out permanently AND to shut
    /// themselves in permanently, in the same gesture.
    /// </summary>
    [Fact]
    public void WithoutTheCardYouCanNeitherLockNorUnlock()
    {
        Assert.False(LockedDoor.MayLock(LockedDoor.State.Shut, hasKey: false));
        Assert.True(LockedDoor.MayLock(LockedDoor.State.Shut, hasKey: true));

        Assert.False(LockedDoor.MayOpen(LockedDoor.State.Locked, hasKey: false));
        Assert.True(LockedDoor.MayOpen(LockedDoor.State.Locked, hasKey: true));

        // …and a keyed door does not move at all for somebody without it.
        Assert.Equal(LockedDoor.State.Locked, LockedDoor.Next(LockedDoor.State.Locked, hasKey: false));
    }

    /// <summary>Pressing a door walks it round a cycle that always makes sense — and never skips a state, so a
    /// captain cannot lock something by accident on the way past.</summary>
    [Fact]
    public void PressingADoorNeverSkipsAState()
    {
        Assert.Equal(LockedDoor.State.Shut, LockedDoor.Next(LockedDoor.State.Open, hasKey: true));
        Assert.Equal(LockedDoor.State.Locked, LockedDoor.Next(LockedDoor.State.Shut, hasKey: true));
        Assert.Equal(LockedDoor.State.Shut, LockedDoor.Next(LockedDoor.State.Locked, hasKey: true));
        Assert.Equal(LockedDoor.State.Open, LockedDoor.Next(LockedDoor.State.Shut, hasKey: false));
    }

    /// <summary>The door tells the captain what it will do BEFORE they press it, in every state — an affordance
    /// you cannot read is one you do not have (#212).</summary>
    [Fact]
    public void EveryDoorStateSaysWhatPressingItDoes()
    {
        foreach (LockedDoor.State s in Enum.GetValues<LockedDoor.State>())
        {
            foreach (bool key in new[] { true, false })
            {
                Assert.False(string.IsNullOrWhiteSpace(LockedDoor.Label(s, key)));
            }
        }

        Assert.Contains("lock", LockedDoor.Label(LockedDoor.State.Shut, true), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("open", LockedDoor.Label(LockedDoor.State.Shut, false), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Shutting one names what it buys AND what it does not. A door that advertised only the good half
    /// would be the game lying to a captain about a decision.</summary>
    [Fact]
    public void ShuttingADoorAdmitsItOnlyBuysTime() =>
        Assert.Contains("for a while", LockedDoor.ShutLine, StringComparison.OrdinalIgnoreCase);

    // ── Into the mountain ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SHE GOES IN THREE CHAMBERS DEEP, with a door between each. One chamber was a vault; three with doors
    /// between them is a place you go INTO — which is what makes a locked door behind you mean anything at all.
    /// </summary>
    [Fact]
    public void TheLabRunsThreeChambersIntoTheRock()
    {
        SecretLab.Region region = Lab();

        Assert.Equal(3, SecretLab.ChamberNames.Count);
        Assert.Equal(2, region.Doors.Count);
        Assert.Equal(SecretLab.ChamberNames.Count, region.Doors.Count + 1);

        // Each door leads into a named chamber, deeper than the last.
        Assert.Equal(SecretLab.ChamberNames[1], region.Doors[0].Deeper);
        Assert.Equal(SecretLab.ChamberNames[2], region.Doors[1].Deeper);
    }

    /// <summary>
    /// AND IT STAYS INSIDE THE GROUND IT WAS GIVEN. The first build of three chambers ran 38 du into the rock
    /// while the placement still reserved 16, so the deep end came out through the field edge — caught by
    /// <c>Region_Bounds_StayInsideTheFieldsSafeSpan</c> on its first run. The reservation and the build read the
    /// same total now, and the same seed decides which way it grows.
    /// </summary>
    [Fact]
    public void EveryDoorAndConsoleLandsInsideTheLabsOwnBounds()
    {
        foreach (string body in new[] { "test-rock", "luna", "miranda", "phobos", "titan", "some-other-rock" })
        {
            SecretLab.Region region = Lab(body);

            foreach (SecretLab.LabDoor d in region.Doors)
            {
                Assert.InRange(d.X, region.MinX - 0.01, region.MaxX + 0.01);
            }

            foreach (SecretLab.LabConsole c in region.Consoles)
            {
                Assert.InRange(c.X, region.MinX - 0.01, region.MaxX + 0.01);
            }
        }
    }

    /// <summary>
    /// THE TWO PANELS ARE TOGETHER, IN THE MIDDLE. The owner asked for both in the same breath — a control board
    /// off the vent panel, and an alarm to hack — and they belong in one room: a captain who reaches the clean
    /// room can throw every door in the mountain from one wall AND argue with the thing that is counting. Putting
    /// them at opposite ends would have made each one a separate errand under a countdown.
    /// </summary>
    [Fact]
    public void BothPanelsAreInTheSameChamberAndItIsNotTheDeepestOne()
    {
        SecretLab.Region region = Lab();

        SecretLab.LabConsole board = region.Consoles.Single(c => c.Kind == SecretLab.LabConsoleKind.DoorBoard);
        SecretLab.LabConsole alarm = region.Consoles.Single(c => c.Kind == SecretLab.LabConsoleKind.AlarmPanel);
        SecretLab.LabConsole card = region.Consoles.Single(c => c.Kind == SecretLab.LabConsoleKind.KeyCard);

        Assert.Equal(board.X, alarm.X, 3);

        // …and the CARD is deeper than both, because a captain who ran at the first alarm must not have it.
        Assert.True(Math.Abs(card.X - region.MinX) > Math.Abs(board.X - region.MinX)
                    || Math.Abs(card.X - region.MaxX) > Math.Abs(board.X - region.MaxX),
                    "the card must be deeper into the rock than the panels");
    }

    // ── The alarm, and hacking it ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A BARE-HANDED CAPTAIN SHOULD USUALLY FAIL, and everything that helps is something they DID earlier. That
    /// is what makes the roll a report on the approach rather than a coin flip at a panel.
    /// </summary>
    [Fact]
    public void WalkingStraightToThePanelIsABadBet()
    {
        LabSecurity.Approach cold = new(HasKeyCard: false, LogsRead: 0, AlarmAlreadyRunning: true,
                                        NerveBand: 2, CarryingASentry: false);
        LabSecurity.Approach prepared = new(HasKeyCard: true, LogsRead: 3, AlarmAlreadyRunning: false,
                                            NerveBand: 4, CarryingASentry: true);

        int coldStack = LabSecurity.Modifiers(cold).Sum(m => m.Value);
        int preparedStack = LabSecurity.Modifiers(prepared).Sum(m => m.Value);

        Assert.True(coldStack < 0, "arriving cold, mid-countdown, must be a penalty");
        Assert.True(preparedStack >= 8, "doing the work must be worth most of the gap to the target");
        Assert.InRange(LabSecurity.HackTarget, 12, 17);
    }

    /// <summary>Reading everything is not a skeleton key — the last logs teach nothing the first two did not, so
    /// the modifier caps. Without the cap, "read all four" would dominate every other preparation.</summary>
    [Fact]
    public void HisOwnLogsHelpAndThenStopHelping()
    {
        int Two() => LabSecurity.Modifiers(new(false, 2, false, 4, false)).Sum(m => m.Value);
        int Four() => LabSecurity.Modifiers(new(false, 4, false, 4, false)).Sum(m => m.Value);

        Assert.Equal(Two(), Four());
    }

    /// <summary>
    /// A FAILED HACK MUST COST, or the panel is a slot machine you pull until it pays. It takes real seconds off
    /// the countdown, so the second attempt is worse than the first and the third may not exist.
    /// </summary>
    [Fact]
    public void FailingCostsTimeSoThePanelIsNotASlotMachine()
    {
        Assert.True(LabSecurity.FailureCostsSeconds > 0);

        double first = LabSecurity.SecondsLeftAfter(elapsed: 0, failures: 0);
        double second = LabSecurity.SecondsLeftAfter(elapsed: 0, failures: 1);
        double third = LabSecurity.SecondsLeftAfter(elapsed: 0, failures: 2);

        Assert.True(second < first && third < second);

        // …and there are only so many tries in it, whatever the dice say.
        Assert.True(LabSecurity.ArmedSeconds / LabSecurity.FailureCostsSeconds <= 4,
                    "if the countdown affords more than a few attempts, the die stops mattering");
        Assert.Equal(0, LabSecurity.SecondsLeftAfter(elapsed: 0, failures: 99));
    }

    /// <summary>The countdown is long enough to cross the lab and think, short enough that finishing a log first
    /// is a gamble rather than a plan.</summary>
    [Fact]
    public void TheCountdownIsAWalkAndAThoughtAndNotMuchMore() =>
        Assert.InRange(LabSecurity.ArmedSeconds, 45, 120);

    /// <summary>
    /// THE PANEL IS OFFERED, NOT SPRUNG. The house law is that the die is SHOWN — so a captain reads the target
    /// and their own stack and may decide NOT to roll, which is the only thing that makes a bad roll their own.
    /// </summary>
    [Fact]
    public void ThePanelStatesTheTargetAndTheStackBeforeAnybodyRolls()
    {
        string offer = LabSecurity.OfferLine(LabSecurity.HackTarget, 5);

        Assert.Contains(LabSecurity.HackTarget.ToString(), offer, StringComparison.Ordinal);
        Assert.Contains("+5", offer, StringComparison.Ordinal);
        Assert.Contains("costs you time", offer, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AND LOCKDOWN IS THE POINT OF ALL OF IT. The owner's cool feature turned against him: every door keyed at
    /// once, including the way out — and the only thing that opens them is in the deepest room. The two lines
    /// have to say plainly which of those two situations the captain is in.
    /// </summary>
    [Fact]
    public void LockdownTellsYouWhichSideOfItYouAreOn()
    {
        Assert.Contains("every door", LabSecurity.LockdownLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("came in through", LabSecurity.LockdownLine, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("you can walk past it", LabSecurity.LockdownWithTheCardLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("where you did not go", LabSecurity.LockdownWithoutTheCardLine, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The same seed and the same approach always roll the same — determinism is law, and a hack a
    /// player could re-roll by reloading would not be a decision.</summary>
    [Fact]
    public void TheHackIsSeededAndNeverReRollable()
    {
        LabSecurity.Approach a = new(true, 1, false, 3, false);
        ulong seed = DiceRule.Seed("lab-hack", 7);

        Assert.Equal(LabSecurity.Attempt(seed, a).Total, LabSecurity.Attempt(seed, a).Total);
        Assert.NotEqual(LabSecurity.Attempt(seed, a).Total,
                        LabSecurity.Attempt(DiceRule.Seed("lab-hack", 8), a).Total);
    }
}
