using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1151 slice 3 · <b>THE UNEASE FLASHBACK, AT THE PLATE, THE SHEET AND THE BOOK WHERE IT DRAWS.</b> The fifth half of
/// <see cref="TheSideOfficesKeepTheirHoursTests"/>: the first ADJUSTED settlement of a run raises the Flashback plate
/// (generic stamp, the clause's caption), costs the nerve its dab and leaves exactly ONE record — judged AFTER the whole
/// card sequence (the eighth shape), on the plate's own title and caption, the sheet's own text and the book's own lines.
/// </summary>
public sealed partial class TheSideOfficesKeepTheirHoursTests
{
    private static (StoryBeats.Beat Beat, string? Subject, double Until)? ThePlate(Pages.Map map) =>
        ((StoryBeats.Beat, string?, double)?)Read(map, "_storyPlate");

    private static double NerveCarry(Pages.Map map) => (double)Read(map, "_nerveShockCarry")!;

    private static IEnumerable<HeldMemory.Sheet> Sheets(Pages.Map map) => (IEnumerable<HeldMemory.Sheet>)Read(map, "_heldMemories")!;

    private static HeldMemory.Sheet? TheSigning(Pages.Map map) => HeldMemory.Find(Sheets(map).ToList(), NebulaRep.SigningMemoryId);

    private static int Margins(Pages.Map map) =>
        TheSigning(map) is { } s ? s.Text.Split(ClaimInterview.MarginLine).Length - 1 : 0;

    private static int UneaseNotes(Pages.Map map) => Notes(map).Count(n => n.Text == ClaimInterview.UneaseBookLine);

    /// <summary>The first booking from <paramref name="from"/> whose REAL interview — a stray paper offered at question
    /// three, so the seeded acceptance decides — settles as <paramref name="want"/>; the clause it quotes is returned.</summary>
    private static HullClaim.Loss LossOn(ClaimInterview.Outcome want, long from, int clause = 0)
    {
        for (long when = from; ; when += 7)
        {
            var loss = new HullClaim.Loss(when, 20);
            ClaimInterview.Settlement s = ClaimInterview.Settle(loss, ClaimInterview.Q3Accepts(when));
            if (s.Outcome == want && (clause == 0 || s.Pick == clause))
            {
                return loss;
            }
        }
    }

    /// <summary>One press from seated, with the berth's own arrival plate (up from the boot) cleared: the plate under judgement is the only one.</summary>
    private static Pages.Map SeatedWithNoPlate(string name, HullClaim.Loss loss)
    {
        Pages.Map map = OnePressFromSeated(name, loss);
        Set(map, "_storyPlate", null);
        return map;
    }

    /// <summary>Settle a booked claim the real way: sit, the three questions, the stray paper at the last.</summary>
    private static void HaveTheInterview(Pages.Map map, HullClaim.Loss loss)
    {
        Sit(map);
        AnswerAll(map, loss, Row(map, AStrayPaper.Id));
        Assert.False((bool)Read(map, "TheInterviewIsUp")!);
    }

    /// <summary>A second booking on the SAME page: the loss in the book through the real writer, filed and booked, the
    /// filled form in the sleeve — one press from seated again.</summary>
    private static void BookAnother(Pages.Map map, HullClaim.Loss loss)
    {
        Invoke(map, "CloseViewObject");
        Invoke(map, "RecordTheLoss", loss);
        Register(map).Add(HullClaim.FiledTag(loss.DoneAt));
        Register(map).Add(HullClaim.BookedTag(loss.DoneAt));
        Sleeve(map).Add(new Satchel.Item(Satchel.Kind.Paper, HullClaim.FilledId(loss)));
        StandAtTheConsole(map);
    }

    /// <summary>
    /// <b>THE FIRST ADJUSTED SETTLEMENT RAISES THE PLATE, THE DAB AND THE BOOK'S ENTRY — AFTER THE OUTCOME, ONCE.</b>
    /// Over six bookings (every clause, twice), each on a fresh page with no signing sheet held and the nerve bank empty:
    /// after the whole card sequence the plate is up with the Flashback beat and the clause's subject, its title the
    /// GENERIC stamp and its caption the cut's; the card still reads her ADJUSTED line (the beat did not replace it);
    /// the purse, the paper and the closed row are the outcome's; the dab banked exactly 4 in the shock carry (the gauge
    /// holds); the book holds the 📍 entry once and the register the latch; no signing sheet appeared. A SECOND adjusted
    /// settlement on the same page, plate cleared, is silent: no plate, no new note, no second dab.
    ///
    /// <para><b>Proven RED</b> by the beat on every ADJUSTED (the second settlement raises a plate), by the beat fired
    /// before the outcome (the purse and the paper are not yet the outcome's when the plate stands), by the dab dropped
    /// and by the wrong record (the margin on a captain holding no sheet).</para>
    /// </summary>
    [Fact]
    public void TheFirstAdjustedSettlementRaisesThePlateTheDabAndTheBookEntryOnce()
    {
        int run = 0;
        foreach (int clause in new[] { 1, 2, 3, 1, 2, 3 })
        {
            HullClaim.Loss loss = LossOn(ClaimInterview.Outcome.Adjusted, 100000 + (run * 5000), clause);
            Pages.Map map = SeatedWithNoPlate($"claim-unease-{run++}", loss);
            Set(map, "_nerveShockCarry", 0.0);
            double nerve = (double)Read(map, "_nerve")!;
            int purse = (int)Read(map, "_credits")!;
            Assert.Null(ThePlate(map));
            Assert.Null(TheSigning(map));

            HaveTheInterview(map, loss);

            // ── AFTER THE SEQUENCE, at the plate…
            ClaimInterview.Settlement s = ClaimInterview.Settle(loss, ClaimInterview.Q3Accepts(loss.DoneAt));
            Assert.Equal(ClaimInterview.Outcome.Adjusted, s.Outcome);
            Assert.Equal(clause, s.Pick);
            Assert.NotNull(ThePlate(map));
            var plate = ThePlate(map)!.Value;
            Assert.Equal(StoryBeats.Beat.Flashback, plate.Beat);
            Assert.Equal(ClaimInterview.FlashbackSubject, plate.Subject);
            Assert.Equal(StoryBeats.Title(StoryBeats.Beat.Flashback, NebulaRep.SigningMemoryId), StoryBeats.Title(plate.Beat, plate.Subject));
            Assert.EndsWith("A PAGE YOU DON'T REMEMBER WRITING", StoryBeats.Title(plate.Beat, plate.Subject), StringComparison.Ordinal);
            Assert.Equal(ClaimInterview.FlashbackCaption, StoryBeats.Caption(plate.Beat, plate.Subject));
            Assert.Equal(s.Told, TheCard(map).Caption);   // her ADJUSTED line is still the card's: the beat is a plate over it

            // …the outcome was applied BEFORE it (the purse, the paper, the closed row)…
            Assert.Equal(purse + 80 - ClaimInterview.BiteCr(20, s.Pick), (int)Read(map, "_credits")!);
            Assert.Single(Sleeve(map), i => i.Id == s.PaperId);
            Assert.False(ClaimInterview.IsOpen(Register(map), loss.DoneAt));

            // …the dab banked in the carry (the gauge holds), the one record is the BOOK's, the latch spent.
            Assert.Equal(ClaimInterview.UneaseNerve, NerveCarry(map), 6);
            Assert.Equal(nerve, (double)Read(map, "_nerve")!, 6);
            Assert.Equal(1, UneaseNotes(map));
            FieldNote filed = Notes(map).Single(n => n.Text == ClaimInterview.UneaseBookLine);
            Assert.Equal(HullClaim.BookGlyph, filed.Glyph);
            Assert.Equal(ClaimInterview.UneaseSubjects, filed.Subjects);   // filed under Nebula Mutual (owner ruling), not under a place
            Assert.Null(TheSigning(map));
            Assert.Contains(ClaimInterview.UneaseTag, Register(map));

            // ── A SECOND ADJUSTED SETTLEMENT IS QUOTE-ONLY: silent at the plate, the book and the carry.
            Set(map, "_storyPlate", null);
            HullClaim.Loss second = LossOn(ClaimInterview.Outcome.Adjusted, loss.DoneAt + 2500);
            BookAnother(map, second);
            int notes = Notes(map).Count();
            HaveTheInterview(map, second);
            Assert.Equal(ClaimInterview.AdjustedLine(ClaimInterview.Settle(second, ClaimInterview.Q3Accepts(second.DoneAt)).Pick), TheCard(map).Caption);
            Assert.Null(ThePlate(map));
            Assert.Equal(notes, Notes(map).Count());
            Assert.Equal(1, UneaseNotes(map));
            Assert.Equal(ClaimInterview.UneaseNerve, NerveCarry(map), 6);
            Assert.Equal(1, Register(map).Count(t => t == ClaimInterview.UneaseTag));
        }
    }

    /// <summary>
    /// <b>THE DAB IS REAL: A BANK ALREADY NEAR A PIP SPENDS ONE, UNDER THE CUT'S REASON.</b> The positive control for
    /// the carry judgement above (which could pass on an inert call): with 6 banked the unease's 4 makes a whole pip —
    /// the nerve falls by one pip, the carry is spent to nothing and the ledger's line names "the clause answered in your
    /// own hand".
    ///
    /// <para><b>Proven RED</b> by the shock label changed and by the dab dropped (the pip does not fall).</para>
    /// </summary>
    [Fact]
    public void TheDabSpendsAPipUnderTheCutsReasonWhenTheBankIsFull()
    {
        HullClaim.Loss loss = LossOn(ClaimInterview.Outcome.Adjusted, 300000);
        Pages.Map map = SeatedWithNoPlate("claim-unease-pip", loss);
        Set(map, "_nerve", NervePips.FromPips(NervePips.MaxPips));
        Set(map, "_nerveShockCarry", 6.0);

        HaveTheInterview(map, loss);

        Assert.Equal(NervePips.FromPips(NervePips.MaxPips - 1), (double)Read(map, "_nerve")!, 6);
        Assert.Equal(0.0, NerveCarry(map), 6);
        var ledger = (IEnumerable<NervePips.Event>)Read(map, "_nerveLedger")!;
        Assert.Contains(ledger, e => e.Label == "the clause answered in your own hand" && e.Delta == -1);
    }

    /// <summary>
    /// <b>WITH THE SIGNING SHEET HELD, THE ONE RECORD IS ITS MARGIN LINE — ONCE EVER, SURVIVING THE FILE.</b> The sheet
    /// (filed through the one real writer) grows the margin line on the first ADJUSTED settlement and the book gets NO
    /// 📍 entry; a second settlement leaves the sheet as it was. The sheet's row goes through the vault's own writer and
    /// loader into a fresh page — the margin comes back whole and once (positive control: the file text carries it, and a
    /// page restored from a file WITHOUT it holds none) — and a second ADJUSTED settlement on the reloaded page, the run's
    /// register carried across, raises no plate and appends nothing. The sheet's writer re-run (the poster's and the rep's
    /// door) keeps the margin.
    ///
    /// <para><b>Proven RED</b> by the margin appended on every settlement (twice), by the wrong record (the book entry
    /// beside a held sheet) and by the writer's margin guard dropped (the re-run wipes the company's note).</para>
    /// </summary>
    [Fact]
    public void WithTheSigningSheetHeldTheMarginIsAppendedOnceAndSurvivesTheVault()
    {
        HullClaim.Loss loss = LossOn(ClaimInterview.Outcome.Adjusted, 400000);
        Pages.Map map = SeatedWithNoPlate("claim-unease-sheet", loss);
        Invoke(map, "FileTheSigningSheet");
        string before = TheSigning(map)!.Value.Text;
        Assert.Equal(NebulaRep.SigningMemoryFor(0), before);
        Assert.Equal(0, Margins(map));

        HaveTheInterview(map, loss);

        Assert.Equal(1, Margins(map));
        Assert.Equal(ClaimInterview.WithTheMargin(before), TheSigning(map)!.Value.Text);
        Assert.Equal(0, UneaseNotes(map));
        Assert.NotNull(ThePlate(map));
        Assert.Equal(ClaimInterview.FlashbackSubject, ThePlate(map)!.Value.Subject);

        // A second settlement: the sheet is exactly as it was.
        Set(map, "_storyPlate", null);
        HullClaim.Loss second = LossOn(ClaimInterview.Outcome.Adjusted, 450000);
        BookAnother(map, second);
        HaveTheInterview(map, second);
        Assert.Equal(1, Margins(map));
        Assert.Equal(0, UneaseNotes(map));
        Assert.Null(ThePlate(map));

        // The sheet's own writer, re-run (the poster, the rep): same afternoon, the margin kept.
        Invoke(map, "FileTheSigningSheet");
        Assert.Equal(1, Margins(map));
        Assert.Equal(ClaimInterview.WithTheMargin(before), TheSigning(map)!.Value.Text);

        // ── The vault round trip: write the sections, load the file, restore on a fresh page.
        var written = new Vault { HeldMemories = (HeldMemoriesSection?)Invoke(map, "BuildHeldMemoriesSection") };
        string json = VaultSerializer.Save(written);
        Assert.Contains("Read to claimant in full", json, StringComparison.Ordinal);   // positive control: the file holds it
        Vault back = VaultSerializer.Load(json);
        Assert.False(back.Tampered);

        HullClaim.Loss third = LossOn(ClaimInterview.Outcome.Adjusted, 500000);
        Pages.Map reloaded = SeatedWithNoPlate("claim-unease-sheet-reloaded", third);
        Assert.Null(TheSigning(reloaded));
        Invoke(reloaded, "RestoreOldCrewSections", back);
        Assert.Equal(1, Margins(reloaded));
        Assert.Equal(ClaimInterview.WithTheMargin(before), TheSigning(reloaded)!.Value.Text);
        foreach (string tag in Register(map))
        {
            Register(reloaded).Add(tag);   // the run's register comes back with the file, as it does
        }

        HaveTheInterview(reloaded, third);
        Assert.Equal(1, Margins(reloaded));
        Assert.Equal(0, UneaseNotes(reloaded));
        Assert.Null(ThePlate(reloaded));

        // The control's other side: a file WITHOUT the margin restores a sheet without it.
        var bare = new Vault
        {
            HeldMemories = new HeldMemoriesSection { Sheets = [TheSigning(map)!.Value.Stored.Replace(ClaimInterview.MarginLine, "")] },
        };
        Pages.Map unmarked = SeatedWithNoPlate("claim-unease-sheet-bare", LossOn(ClaimInterview.Outcome.Adjusted, 550000));
        Invoke(unmarked, "RestoreOldCrewSections", VaultSerializer.Load(VaultSerializer.Save(bare)));
        Assert.Equal(0, Margins(unmarked));
    }

    /// <summary>
    /// <b>A REBIRTH'S RE-FILING KEEPS THE MARGIN, WITH THE REBORN LINE.</b> The captain has buried one of himself (a thread
    /// row planted, as the rep's bench does) and holds a sheet already carrying the margin; the sheet's one writer re-runs:
    /// the text is the reborn canonical text plus the margin, one of each line.
    ///
    /// <para><b>Proven RED</b> by the writer's margin guard dropped (the re-filing wipes the margin).</para>
    /// </summary>
    [Fact]
    public void AReFilingAfterARebirthKeepsTheMarginAndTheRebornLine()
    {
        HullClaim.Loss loss = LossOn(ClaimInterview.Outcome.Adjusted, 700000);
        Pages.Map map = SeatedWithNoPlate("claim-unease-reborn", loss);
        Set(map, "_activeThreadId", "unease-thread");
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)
        [
            new GameThreadInfo { Id = "unease-thread", Retired = [new RetiredCaptain("Someone Who Died", 12)] },
        ]);
        Set(map, "_heldMemories", new List<HeldMemory.Sheet>
        {
            new(NebulaRep.SigningMemoryId, HeldMemory.Mark.Mine, HeldMemory.Theory.Money,
                ClaimInterview.WithTheMargin(NebulaRep.SigningMemoryFor(0)), [], 5.0),
        });

        Invoke(map, "FileTheSigningSheet");

        string text = TheSigning(map)!.Value.Text;
        Assert.Equal(ClaimInterview.WithTheMargin(NebulaRep.SigningMemoryFor(1)), text);
        Assert.Equal(1, Margins(map));
        Assert.Equal(1, text.Split(NebulaRep.SigningMemoryReborn).Length - 1);
    }

    /// <summary>
    /// <b>PAID AND DECLINED NEVER RAISE IT — AND NEITHER SPENDS THE LATCH.</b> Over four bookings of each, with the
    /// signing sheet held: after the whole sequence there is no plate, no tag, no margin, no 📍 entry and no dab; and the
    /// same page's first ADJUSTED settlement afterwards still raises the beat (the latch was never spent).
    ///
    /// <para><b>Proven RED</b> by the beat on DECLINED and by the beat on PAID.</para>
    /// </summary>
    [Fact]
    public void PaidAndDeclinedStaySilentAndLeaveTheLatchUnspent()
    {
        int run = 0;
        foreach (ClaimInterview.Outcome quiet in new[] { ClaimInterview.Outcome.Paid, ClaimInterview.Outcome.Declined })
        {
            for (int i = 0; i < 4; i++)
            {
                HullClaim.Loss loss = LossOn(quiet, 600000 + (run * 3000) + (i * 40000));
                Pages.Map map = SeatedWithNoPlate($"claim-unease-quiet-{run++}", loss);
                Invoke(map, "FileTheSigningSheet");
                Set(map, "_nerveShockCarry", 0.0);

                HaveTheInterview(map, loss);

                Assert.Equal(quiet, ClaimInterview.Settle(loss, ClaimInterview.Q3Accepts(loss.DoneAt)).Outcome);
                Assert.Null(ThePlate(map));
                Assert.DoesNotContain(ClaimInterview.UneaseTag, Register(map));
                Assert.Equal(0, Margins(map));
                Assert.Equal(0, UneaseNotes(map));
                Assert.Equal(0.0, NerveCarry(map), 6);

                // The latch is unspent: the first ADJUSTED settlement afterwards raises the beat.
                HullClaim.Loss adjusted = LossOn(ClaimInterview.Outcome.Adjusted, loss.DoneAt + 2500);
                BookAnother(map, adjusted);
                HaveTheInterview(map, adjusted);
                Assert.Equal(ClaimInterview.FlashbackSubject, ThePlate(map)!.Value.Subject);
                Assert.Equal(1, Margins(map));
            }
        }
    }

    /// <summary>
    /// <b>THE DEV START: <c>/map?claim=3</c> SETTLES ADJUSTED BY THE REAL INTERVIEW, WHICHEVER WAY QUESTION THREE GOES.</b>
    /// Booted through the real staging at the pinned moment, the captain sits and answers — an explicit nothing, a stray
    /// paper — and the REAL outcome is ADJUSTED both times (no forced roll anywhere): the plate, the dab's carry and the
    /// book's 📍 entry. <c>&amp;signing=1</c> stages the sheet held and the same interview grows its margin line instead.
    ///
    /// <para><b>Proven RED</b> by the pinned moment moved to a DECLINED one (no plate), by the booking left on the clock's
    /// moment (the verdict floats) and by the signing flag ignored (the book entry, no margin).</para>
    /// </summary>
    [Fact]
    public void TheDevStartSettlesAdjustedByTheRealInterviewBothWays()
    {
        foreach (bool signing in new[] { false, true })
        {
            foreach (bool nothing in new[] { true, false })
            {
                string url = signing ? "/map?claim=3&signing=1" : "/map?claim=3";
                SideOffice office = SideOffices.Adjuster;
                Pages.Map map = TheDeep($"claim-dev-3-{signing}-{nothing}");
                Set(map, "Navigation", new TheBootBuildsTheSameWorldTests.Bench(url));
                At(map, Within(TheOfficesWatch(map, office) + 1));
                Invoke(map, "StandAtTheOfficeIfAsked");
                Frames(map, 2);

                Satchel.Item form = Assert.Single(Sleeve(map), i => HullClaim.IsTheFilledForm(i.Id));
                Assert.True(HullClaim.TryReadFilled(form.Id, out HullClaim.Loss loss));
                Assert.Equal(ClaimInterview.Claim3When, loss.DoneAt);
                Assert.Equal(signing, TheSigning(map) is not null);
                Assert.Equal(0, Margins(map));
                Set(map, "_nerveShockCarry", 0.0);

                StandAtTheConsole(map);
                Sit(map);
                Press(map, Row(map, HullClaim.FilledId(loss)));
                Press(map, Rows(map).Single(r => r.Kind == ClaimInterview.ShowKind.Policy));
                ClaimInterview.Show third = nothing
                    ? new ClaimInterview.Show(ClaimInterview.ShowKind.Nothing, "", ClaimInterview.NothingLabel)
                    : Rows(map).First(r => r.Kind == ClaimInterview.ShowKind.Paper);
                Press(map, third);

                // The REAL outcome, read off the card and the sleeve — not asked of a forced roll.
                string told = TheCard(map).Caption!;
                Assert.StartsWith("Adjusted and paid. The deduction is Clause ", told, StringComparison.Ordinal);
                Assert.Contains(Sleeve(map), i => i.Id.StartsWith("claim-settlement:adjusted:", StringComparison.Ordinal));
                Assert.NotNull(ThePlate(map));
                Assert.Equal(ClaimInterview.FlashbackSubject, ThePlate(map)!.Value.Subject);
                Assert.Equal(ClaimInterview.UneaseNerve, NerveCarry(map), 6);
                Assert.Equal(signing ? 1 : 0, Margins(map));
                Assert.Equal(signing ? 0 : 1, UneaseNotes(map));
            }
        }
    }
}
