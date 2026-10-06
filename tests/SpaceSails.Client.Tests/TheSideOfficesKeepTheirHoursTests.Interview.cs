using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1151 slice 2 · <b>THE INTERVIEW, AT THE PANEL WHERE IT DRAWS.</b> The fourth half of
/// <see cref="TheSideOfficesKeepTheirHoursTests"/> (the helpers are the first half's): a booked captain on her watch
/// presses the console, the one card seats him, the real row presses walk the three questions, and every outcome is
/// judged AFTER the whole card sequence — on the card's own caption and outcome region, the purse, the satchel, the
/// register and the book — never on a pulse and never mid-sequence (the eighth shape).
/// </summary>
public sealed partial class TheSideOfficesKeepTheirHoursTests
{
    private static readonly Satchel.Item AStrayPaper = new(Satchel.Kind.Paper, "spread-demo-1");

    private static DeckPlan.ConsoleSpot TheCard(Pages.Map map) => (DeckPlan.ConsoleSpot)Read(map, "_viewObject")!;

    private static bool CardIsUp(Pages.Map map) => Read(map, "_viewObject") is not null;

    private static IReadOnlyList<ClaimInterview.Show> Rows(Pages.Map map) =>
        (IReadOnlyList<ClaimInterview.Show>)Invoke(map, "TheInterviewShows")!;

    private static ClaimInterview.Show Row(Pages.Map map, string id) => Rows(map).Single(r => r.Id == id);

    private static void Press(Pages.Map map, ClaimInterview.Show show) => Invoke(map, "ShowThePaper", show);

    private static List<Satchel.Item> Sleeve(Pages.Map map) => (List<Satchel.Item>)Read(map, "_satchel")!;

    /// <summary>The first booking whose rolls make <paramref name="want"/> come out under the given question-three
    /// verdict — searched on Core's own dice, so the page is judged against the same arithmetic it runs.</summary>
    private static HullClaim.Loss LossFor(ClaimInterview.Outcome want, bool accepted, int tenths = 20)
    {
        for (long when = 100000; ; when += 7)
        {
            if (ClaimInterview.Q3Accepts(when) == accepted && ClaimInterview.OutcomeOf(when, accepted) == want)
            {
                return new HullClaim.Loss(when, tenths);
            }
        }
    }

    /// <summary>A page at The Deep with the loss in the book (through the real writer), the form filled and booked, a
    /// policy in force and her watch live — one press from seated.</summary>
    private static Pages.Map OnePressFromSeated(string name, HullClaim.Loss loss, PirateInsurance? policy = null)
    {
        SideOffice office = SideOffices.Adjuster;
        Pages.Map map = TheDeep(name);
        At(map, loss.DoneAt);
        Invoke(map, "RecordTheLoss", loss);   // the real writer: the 📋 line at the loss's own moment, and its tag
        Register(map).Add(HullClaim.FiledTag(loss.DoneAt));
        Register(map).Add(HullClaim.BookedTag(loss.DoneAt));
        Carry(map, AStrayPaper, office.TheSheet, new Satchel.Item(Satchel.Kind.Paper, HullClaim.FilledId(loss)));
        double herWatch = Within(TheOfficesWatch(map, office));
        At(map, herWatch);
        Set(map, "_insurance", policy ?? NebulaRep.PolicyAfterBuying(InsuranceTier.Premium, herWatch));
        Frames(map, 2);
        StandAtTheConsole(map);
        return map;
    }

    private static void Sit(Pages.Map map)
    {
        Invoke(map, "InteractAtConsole");
        Assert.True(CardIsUp(map), "the console press did not raise the interview card.");
    }

    /// <summary>The three questions through the real row presses, a wrong paper first at each (the question must
    /// stand), ending on <paramref name="third"/>.</summary>
    private static void AnswerAll(Pages.Map map, HullClaim.Loss loss, ClaimInterview.Show third)
    {
        Press(map, Row(map, AStrayPaper.Id));
        Assert.Equal(ClaimInterview.Q1Wrong, TheCard(map).Outcome);
        Assert.Equal(ClaimInterview.Q1Ask, TheCard(map).Caption);
        Press(map, Row(map, HullClaim.FilledId(loss)));
        Assert.Equal(ClaimInterview.Q1Right(loss.Tenths), TheCard(map).Outcome);
        Assert.Equal(ClaimInterview.Q2Ask, TheCard(map).Caption);

        Press(map, Row(map, SideOffices.Adjuster.SheetId));
        Assert.Equal(ClaimInterview.Q2Wrong, TheCard(map).Outcome);
        Assert.Equal(ClaimInterview.Q2Ask, TheCard(map).Caption);
        Press(map, Rows(map).Single(r => r.Kind == ClaimInterview.ShowKind.Policy));
        Assert.Equal(ClaimInterview.Q2Right, TheCard(map).Outcome);
        Assert.Equal(ClaimInterview.Q3Ask, TheCard(map).Caption);

        Press(map, third);
    }

    /// <summary>
    /// <b>THE WHOLE INTERVIEW, EVERY OUTCOME, JUDGED AFTER THE CARD SEQUENCE.</b> Five bookings, each chosen on Core's
    /// own dice so a different branch fires: PAID, ADJUSTED (twice: question three accepted, and an explicit nothing)
    /// and DECLINED (twice: a refused paper, and nothing). The console seats the captain on the seating line with
    /// question one as the caption and one card throughout; a wrong paper at each question says its line and the
    /// question stands; the answer to the third settles — and AFTER the sequence, on the card's own regions, the purse,
    /// the sleeve, the register and the book: PAID pays 80 cr and hands over the paid settlement; ADJUSTED pays the
    /// gross less its clause's bite and hands over the adjusted one; DECLINED pays nothing, hands the form back
    /// returned and stamps the book's loss line · DECLINED. The filled form is gone in every case, the booked row is
    /// closed, the rows are gone and ✕ closes the card.
    ///
    /// <para><b>Proven RED</b> by the outcome applied only on close (the regions read right and the purse not),
    /// DECLINED paying the gross, the filled form surviving an outcome, the booked row left open, the stamp missing,
    /// and a wrong paper that ends the question.</para>
    /// </summary>
    [Fact]
    public void EveryOutcomeIsJudgedAfterTheWholeCardSequence()
    {
        (ClaimInterview.Outcome Outcome, bool Accepted, bool Nothing)[] cases =
        [
            (ClaimInterview.Outcome.Paid, true, false),
            (ClaimInterview.Outcome.Adjusted, true, false),
            (ClaimInterview.Outcome.Adjusted, false, true),
            (ClaimInterview.Outcome.Declined, false, false),
            (ClaimInterview.Outcome.Declined, false, true),
        ];

        foreach ((ClaimInterview.Outcome want, bool accepted, bool nothing) in cases)
        {
            HullClaim.Loss loss = LossFor(want, accepted);
            Pages.Map map = OnePressFromSeated($"claim-interview-{want}-{accepted}-{nothing}", loss);
            int purse = (int)Read(map, "_credits")!;
            Assert.Single(Notes(map), n => n.Text == HullClaim.LossLine(loss.Tenths));

            Sit(map);
            Assert.Equal(AdjustersRoom.DoorPlate, TheCard(map).Label);
            Assert.Equal(ClaimInterview.SeatingLine, TheCard(map).Outcome);
            Assert.Equal(ClaimInterview.Q1Ask, TheCard(map).Caption);
            Assert.True((bool)Read(map, "TheInterviewIsUp")!);

            // Q3: any paper (the stray one), or the explicit nothing — never both: one offer.
            ClaimInterview.Show third = nothing
                ? new ClaimInterview.Show(ClaimInterview.ShowKind.Nothing, "", ClaimInterview.NothingLabel)
                : Row(map, AStrayPaper.Id);
            AnswerAll(map, loss, third);

            // ── AFTER THE SEQUENCE: the card's regions…
            ClaimInterview.Settlement s = ClaimInterview.Settle(loss, accepted && !nothing);
            Assert.Equal(want, s.Outcome);
            string q3 = nothing ? ClaimInterview.Q3NothingLine : accepted ? ClaimInterview.Q3AcceptedLine : ClaimInterview.Q3RefusedLine;
            Assert.Equal(q3, TheCard(map).Outcome);
            Assert.Equal(s.Told, TheCard(map).Caption);
            Assert.Empty(Rows(map));
            Assert.False((bool)Read(map, "TheInterviewIsUp")!);

            // …the purse, the sleeve, the register, the book.
            int expect = want switch
            {
                ClaimInterview.Outcome.Paid => 80,
                ClaimInterview.Outcome.Adjusted => 80 - ClaimInterview.BiteCr(20, s.Pick),
                _ => 0,
            };
            Assert.Equal(purse + expect, (int)Read(map, "_credits")!);
            Assert.Equal(want == ClaimInterview.Outcome.Declined, expect == 0);
            List<Satchel.Item> sleeve = Sleeve(map);
            Assert.DoesNotContain(sleeve, i => HullClaim.IsTheFilledForm(i.Id));
            Assert.Single(sleeve, i => i.Id == s.PaperId);
            Assert.Contains(AStrayPaper, sleeve);
            Assert.Contains(SideOffices.Adjuster.TheSheet, sleeve);
            Assert.Contains(ClaimInterview.SettledTag(loss.DoneAt), Register(map));
            Assert.False(ClaimInterview.IsOpen(Register(map), loss.DoneAt));
            string line = Assert.Single(Notes(map), n => n.Glyph == "📋").Text;
            Assert.Equal(want == ClaimInterview.Outcome.Declined ? HullClaim.LossLine(20) + " · DECLINED" : HullClaim.LossLine(20), line);
            Assert.Equal(want == ClaimInterview.Outcome.Declined ? ClaimInterview.ReturnedTitle : want == ClaimInterview.Outcome.Paid
                ? ClaimInterview.PaidTitle : ClaimInterview.AdjustedTitle, FieldClue.Title(s.PaperId));

            // ✕ closes it — by deciding, nothing left behind.
            Invoke(map, "CloseViewObject");
            Assert.False(CardIsUp(map));
            Assert.Null(Read(map, "_interview"));
            Assert.Equal(purse + expect, (int)Read(map, "_credits")!);
        }
    }

    /// <summary>
    /// <b>A WRONG PAPER IS NEVER A GATE: TEN WRONG SHOWS AT EACH QUESTION CHANGE NOTHING BUT THE LINE.</b> The purse,
    /// the sleeve (every paper still there, the form still held), the register and the book are exactly what they were
    /// after any number of wrong papers, and the right one still moves on.
    ///
    /// <para><b>Proven RED</b> by a wrong paper consuming itself, by one costing a credit, and by the third wrong show
    /// ending the question.</para>
    /// </summary>
    [Fact]
    public void WrongPapersLeaveEverythingAsItWasAndTheQuestionStanding()
    {
        HullClaim.Loss loss = LossFor(ClaimInterview.Outcome.Adjusted, true);
        Pages.Map map = OnePressFromSeated("claim-interview-wrong", loss);
        int purse = (int)Read(map, "_credits")!;
        Sit(map);
        string[] before = [.. Sleeve(map).Select(i => i.Id)];
        string[] book = [.. Notes(map).Select(n => n.Text)];
        string[] reg = [.. Register(map).Order()];

        for (int i = 0; i < 10; i++)
        {
            Press(map, Row(map, AStrayPaper.Id));
            Assert.Equal(ClaimInterview.Q1Ask, TheCard(map).Caption);
            Assert.Equal(ClaimInterview.Q1Wrong, TheCard(map).Outcome);
        }

        Press(map, Row(map, HullClaim.FilledId(loss)));
        for (int i = 0; i < 10; i++)
        {
            Press(map, Row(map, AStrayPaper.Id));
            Assert.Equal(ClaimInterview.Q2Ask, TheCard(map).Caption);
            Assert.Equal(ClaimInterview.Q2Wrong, TheCard(map).Outcome);
        }

        Assert.Equal(purse, (int)Read(map, "_credits")!);
        Assert.Equal(before, Sleeve(map).Select(i => i.Id));
        Assert.Equal(book, Notes(map).Select(n => n.Text));
        Assert.Equal(reg, Register(map).Order());
        Press(map, Rows(map).Single(r => r.Kind == ClaimInterview.ShowKind.Policy));
        Assert.Equal(ClaimInterview.Q3Ask, TheCard(map).Caption);
        Assert.Single(Rows(map), r => r.Kind == ClaimInterview.ShowKind.Nothing);
    }

    /// <summary>
    /// <b>A RELOAD REPLAYS THE SAME INTERVIEW.</b> The same booking sat on two fresh pages, and sat, abandoned (✕) and
    /// sat again on one of them, says the same question-three line and settles the same outcome, the same figure and
    /// the same paper every time — the rolls are the booking's and nothing else's. The abandoned sitting consumed
    /// nothing: the form was still held, the booked row still open, the purse unmoved.
    ///
    /// <para><b>Proven RED</b> by a roll salted from a counter or the clock (the replays differ) and by the form
    /// consumed on the ✕.</para>
    /// </summary>
    [Fact]
    public void TheSameBookingIsTheSameInterviewAfterAReloadAndAfterGettingUp()
    {
        HullClaim.Loss loss = LossFor(ClaimInterview.Outcome.Adjusted, true, 34);
        var seen = new List<(string Q3, string Told, int Purse, string Paper)>();
        for (int sitting = 0; sitting < 3; sitting++)
        {
            Pages.Map map = OnePressFromSeated($"claim-replay-{sitting}", loss);
            int purse = (int)Read(map, "_credits")!;

            if (sitting == 2)
            {
                Sit(map);
                Press(map, Row(map, HullClaim.FilledId(loss)));
                Invoke(map, "CloseViewObject");
                Assert.Null(Read(map, "_interview"));
                Assert.Single(Sleeve(map), i => i.Id == HullClaim.FilledId(loss));
                Assert.True(ClaimInterview.IsOpen(Register(map), loss.DoneAt));
                Assert.Equal(purse, (int)Read(map, "_credits")!);
            }

            Sit(map);
            Assert.Equal(ClaimInterview.SeatingLine, TheCard(map).Outcome);   // always from the seating
            AnswerAll(map, loss, Row(map, AStrayPaper.Id));
            seen.Add((
                TheCard(map).Outcome!, TheCard(map).Caption!, (int)Read(map, "_credits")! - purse,
                Sleeve(map).Single(i => ClaimInterview.IsAPaper(i.Id)).Id));
        }

        Assert.Equal(3, seen.Count);
        Assert.All(seen, s => Assert.Equal(seen[0], s));
        Assert.Equal(ClaimInterview.Q3AcceptedLine, seen[0].Q3);
    }

    /// <summary>
    /// <b>THE CONSOLE SEATS ONLY A BOOKED, UNSETTLED CLAIM — AND A SETTLED ROW STAYS CLOSED ACROSS A VAULT.</b> A filled
    /// form held but not yet booked is BOOKED by the press (the slice-1 behaviour: the booked line, no card); the next
    /// press seats. After an outcome the console seats nobody: the settled tag, saved and loaded into a fresh page
    /// that holds the very form again (an old save), still seats nobody and says the booked line instead. The control
    /// is the booked tag itself: it round-trips beside it.
    ///
    /// <para><b>Proven RED</b> by the settled tag not restored from the vault section (the fresh page seats a second
    /// interview).</para>
    /// </summary>
    [Fact]
    public void TheConsoleSeatsOnlyABookedUnsettledClaimAndTheClosedRowSurvivesAVault()
    {
        HullClaim.Loss loss = LossFor(ClaimInterview.Outcome.Declined, false);
        SideOffice office = SideOffices.Adjuster;

        // Unbooked: the first press books (no card), the second seats.
        Pages.Map map = OnePressFromSeated("claim-unbooked", loss);
        Register(map).Remove(HullClaim.BookedTag(loss.DoneAt));
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "InteractAtConsole");
        Assert.False(CardIsUp(map));
        Assert.Equal(HullClaim.BookedLine, InTheSlot(map));
        Assert.Contains(HullClaim.BookedTag(loss.DoneAt), Register(map));
        Sit(map);
        AnswerAll(map, loss, Row(map, AStrayPaper.Id));
        Invoke(map, "CloseViewObject");

        // Settled: no card, whatever is held.
        Carry(map, new Satchel.Item(Satchel.Kind.Paper, HullClaim.FilledId(loss)));
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "InteractAtConsole");
        Assert.False(CardIsUp(map));
        Assert.Equal(HullClaim.BookedLine, InTheSlot(map));

        // …and across a save and a load.
        var section = (TurnedOverSection?)Invoke(map, "TheRoomsGoneThrough")!;
        Vault back = VaultSerializer.Load(VaultSerializer.Save(new Vault { TurnedOver = section }));
        Pages.Map fresh = TheDeep("claim-settled-reloaded");
        Assert.DoesNotContain(ClaimInterview.SettledTag(loss.DoneAt), Register(fresh));
        Invoke(fresh, "RestoreTheRoomsGoneThrough", back);
        Assert.Contains(HullClaim.BookedTag(loss.DoneAt), Register(fresh));          // the control
        Assert.Contains(ClaimInterview.SettledTag(loss.DoneAt), Register(fresh));    // the closed row
        Carry(fresh, new Satchel.Item(Satchel.Kind.Paper, HullClaim.FilledId(loss)));
        At(fresh, Within(TheOfficesWatch(fresh, office)));
        Frames(fresh, 2);
        StandAtTheConsole(fresh);
        Set(fresh, "_pulse", PulseSlot.Empty);
        Invoke(fresh, "InteractAtConsole");
        Assert.False(CardIsUp(fresh));
        Assert.Equal(HullClaim.BookedLine, InTheSlot(fresh));
    }

    /// <summary>
    /// <b>NO POLICY IN FORCE IS A WRONG PAPER, NOT A LOCKED DOOR.</b> A lapsed policy still has its row (the refusal is
    /// the thing worth learning): pressing it says the Q2 wrong line and the question stands. An uninsured captain has
    /// no card to show — no policy row — and the interview stands at the ship question with every paper still a legal
    /// wrong show; ✕ gets him up with nothing consumed. (The cut names only the wallet card as the ship↔captain proof;
    /// an uninsured captain cannot finish the interview — flagged on the PR.)
    ///
    /// <para><b>Proven RED</b> by a lapsed policy answering question two, and by an uninsured captain's card throwing.</para>
    /// </summary>
    [Fact]
    public void ALapsedOrMissingPolicyIsAWrongPaperAndNeverAnException()
    {
        HullClaim.Loss loss = LossFor(ClaimInterview.Outcome.Adjusted, true);

        Pages.Map lapsed = OnePressFromSeated("claim-lapsed", loss, new PirateInsurance(InsuranceTier.Premium, 1.0));
        Sit(lapsed);
        Press(lapsed, Row(lapsed, HullClaim.FilledId(loss)));
        Press(lapsed, Rows(lapsed).Single(r => r.Kind == ClaimInterview.ShowKind.Policy));
        Assert.Equal(ClaimInterview.Q2Wrong, TheCard(lapsed).Outcome);
        Assert.Equal(ClaimInterview.Q2Ask, TheCard(lapsed).Caption);

        Pages.Map none = OnePressFromSeated("claim-uninsured", loss, PirateInsurance.Uninsured);
        Sit(none);
        Press(none, Row(none, HullClaim.FilledId(loss)));
        Assert.DoesNotContain(Rows(none), r => r.Kind == ClaimInterview.ShowKind.Policy);
        Press(none, Row(none, AStrayPaper.Id));
        Assert.Equal(ClaimInterview.Q2Wrong, TheCard(none).Outcome);
        int purse = (int)Read(none, "_credits")!;
        Invoke(none, "CloseViewObject");
        Assert.Single(Sleeve(none), i => i.Id == HullClaim.FilledId(loss));
        Assert.Equal(purse, (int)Read(none, "_credits")!);
    }

    /// <summary>
    /// <b>THE DEV START: <c>/map?claim=2</c> STAGES THE FORM FILLED AND THE BOOKING MADE — THROUGH THE REAL WRITERS.</b>
    /// On the same footing <c>?claim=1</c> stages (the completed mend's one 📋 line, the desk's blank taken), the
    /// sleeve holds the FILLED form for that loss and no blank, the loss is tagged FILED and BOOKED, the book holds the
    /// one 📍 entry, her watch is live whatever the clock says and the console is on the desk: its press seats the
    /// captain on the seating line. <c>?claim=1</c> stays slice 1's start (the blank, no booking).
    ///
    /// <para><b>Proven RED</b> by the level-2 staging dropped (the blank held, nothing booked) and by the booking typed
    /// as a tag with no book entry.</para>
    /// </summary>
    [Fact]
    public void TheInterviewIsOneDevStartAway()
    {
        SideOffice office = SideOffices.Adjuster;
        Pages.Map map = TheDeep("claim-dev-2");
        Set(map, "Navigation", new TheBootBuildsTheSameWorldTests.Bench("/map?claim=2"));
        At(map, Within(TheOfficesWatch(map, office) + 1));   // not her watch: the latch overrules
        Invoke(map, "StandAtTheOfficeIfAsked");
        Frames(map, 2);

        List<Satchel.Item> sleeve = Sleeve(map);
        Satchel.Item form = Assert.Single(sleeve, i => HullClaim.IsTheFilledForm(i.Id));
        Assert.True(HullClaim.TryReadFilled(form.Id, out HullClaim.Loss loss));
        Assert.DoesNotContain(sleeve, i => i == office.TheSheet);
        Assert.Contains(HullClaim.FiledTag(loss.DoneAt), Register(map));
        Assert.Contains(HullClaim.BookedTag(loss.DoneAt), Register(map));
        Assert.True(ClaimInterview.IsOpen(Register(map), loss.DoneAt));
        Assert.Equal(HullClaim.LossLine(20), Assert.Single(Notes(map), n => n.Glyph == "📋").Text);
        Assert.Single(Booked(map));
        Assert.True(HavenInterior.TheBookingConsoleLiesIn(Deck(map)));

        StandAtTheConsole(map);
        Set(map, "_insurance", NebulaRep.PolicyAfterBuying(InsuranceTier.Premium, (double)Read(map, "SimTime")!));
        Sit(map);
        Assert.Equal(ClaimInterview.SeatingLine, TheCard(map).Outcome);
        Assert.Equal(ClaimInterview.Q1Ask, TheCard(map).Caption);
        Assert.Single(Booked(map));   // sitting does not book again
    }

    /// <summary>
    /// <b>THE BARE <c>/map?claim=2</c> BOOTS DOCKED AT THE DEEP WITH THE FORM FILLED AND THE CLAIM BOOKED.</b> The shipping
    /// page, booted past the browser gate at the bare URL: docked at The Deep on the hotel level, the door ajar, the one
    /// loss line in the book, the filled form (and no blank) in the sleeve, the loss filed and booked, the one 📍 entry.
    /// The control: <c>?claim=1</c> still boots slice 1's start (the blank in the sleeve, nothing booked).
    ///
    /// <para><b>Proven RED</b> by the parse not accepting the 2 (the page boots on the concourse) and by the level-2
    /// staging dropped (the blank held, nothing booked).</para>
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task TheBareClaimTwoUrlBootsWithTheFormFilledAndBooked()
    {
        PastTheGateBench.Boot one = await PastTheGateBench.BootAsync("/map?claim=1");
        Assert.Null(one.Threw);
        Assert.Single(one.Read<List<Satchel.Item>>("_satchel"), i => i == SideOffices.Adjuster.TheSheet);
        Assert.DoesNotContain(one.Read<HashSet<string>>("_roomsTurnedOver"), t => t.StartsWith("claim:booked:", StringComparison.Ordinal));

        PastTheGateBench.Boot two = await PastTheGateBench.BootAsync("/map?claim=2");
        Assert.Null(two.Threw);
        Assert.Equal(AdjustersRoom.HavenId, two.Read<string?>("_dockedHavenId"));
        Assert.Equal(HavenLevels.ServiceLevel, two.Read<int>("_havenFloor"));
        Assert.True(HavenInterior.TheOfficeStandsOpenIn(two.Read<DeckPlan>("_deckPlan")));
        List<Satchel.Item> sleeve = two.Read<List<Satchel.Item>>("_satchel");
        Satchel.Item form = Assert.Single(sleeve, i => HullClaim.IsTheFilledForm(i.Id));
        Assert.DoesNotContain(sleeve, i => i == SideOffices.Adjuster.TheSheet);
        Assert.True(HullClaim.TryReadFilled(form.Id, out HullClaim.Loss loss));
        HashSet<string> register = two.Read<HashSet<string>>("_roomsTurnedOver");
        Assert.Contains(HullClaim.FiledTag(loss.DoneAt), register);
        Assert.Contains(HullClaim.BookedTag(loss.DoneAt), register);
        var notes = (IEnumerable<FieldNote>)two.Field("_fieldNotes")!;
        Assert.Single(notes, n => n.Text == HullClaim.LossLine(20));
        Assert.Single(notes, n => n.Text == HullClaim.BookEntryLine);
    }
}
