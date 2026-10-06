using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1151 slice 1 · <b>THE LOSS, THE FORM, THE BOOKING — THE WORDS, THE ARITHMETIC AND THE REGISTER.</b> Fable canon
/// (the brief cut and its 2026-10-07 amendment: the loss is TIME, not credits), verbatim and typed here from the issue
/// so the guards have a source the implementation cannot move.
/// </summary>
public sealed class HullClaimTests
{
    private static readonly Satchel.Item Blank = SideOffices.Adjuster.TheSheet;

    /// <summary>
    /// <b>THE CANON, TO THE BYTE.</b> Every sentence the slice speaks, full-Assert.Equal, retyped from the issue.
    ///
    /// <para><b>Proven RED</b> by a hyphen for the loss line's em dash, a dropped full stop in the filled form's
    /// document and a ninth string in <c>AllProse</c>.</para>
    /// </summary>
    [Fact]
    public void TheCanonIsPinnedToTheByte()
    {
        Assert.Equal(
            "Hull holed — 2.0 days under sail-mend, and the sky kept its schedule without you. "
            + "The policy calls lost days claimable. Claimable is not the same as paid.",
            HullClaim.LossLine(20));
        Assert.Equal("A claim form, filled", HullClaim.FilledTitle);
        Assert.Equal(
            "Loss: hull, holed. Lost to the mend: 3.4 days under way. Claimant: the captain of record. "
            + "The remaining boxes want codes the desk does not have, and the desk suspects that is their purpose.",
            HullClaim.FilledDocument(34));
        Assert.Equal("✍ Copy the loss onto the claim form", HullClaim.VerbLabel);
        Assert.Equal("🧾 Book the claim — [E]", HullClaim.BookLabel);
        Assert.Equal(
            "Booked for her watch. The cold rooms will keep you exactly as patient as you arrive.",
            HullClaim.BookedLine);
        Assert.Equal(
            "A claim, booked. Nebula Mutual resurrects a man without a form; a sail wants three. "
            + "Somebody designed that, and it was not the sail.",
            HullClaim.BookEntryLine);
        Assert.Equal("The form stays warm in the satchel. Nothing else here is.", HullClaim.ShutExtraLine);
        Assert.Equal("📋", HullClaim.LossGlyph);
        Assert.Equal("📍", HullClaim.BookGlyph);
        Assert.Equal(8, HullClaim.AllProse().Count());
        Assert.Equal(8, HullClaim.AllProse().Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// <b>NOT ONE WORD SPENDS A RESERVED WORD, AND NOT ONE NAMES ANYBODY (the house sweep).</b> No word of the arc the
    /// captain has not yet earned, no regular's name, and no digit anywhere but the figure itself — {n}, the days the
    /// mend took, the one number the canon puts in a sentence.
    ///
    /// <para><b>Proven RED</b> by "restore" in the booking line, and by a digit in the book entry.</para>
    /// </summary>
    [Fact]
    public void NotOneWordSpendsAReservedWordOrNamesAnybody()
    {
        string[] reserved =
        [
            "monolith", "reever", "old one", "old ones", "ancient", "alien", "not ours", "not natural",
            "restore", "backup", "kaamos", "minister", "donor", "they were people", "whose", "who made",
        ];

        foreach (string line in HullClaim.AllProse())
        {
            foreach (string word in reserved)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }

            foreach (string regular in PatronRota.Roster)
            {
                Assert.DoesNotContain(regular, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        string[] withTheFigure = [HullClaim.LossLine(20), HullClaim.FilledDocument(20)];
        foreach (string line in HullClaim.AllProse().Except(withTheFigure))
        {
            Assert.DoesNotContain(line, char.IsDigit);
        }

        foreach (string line in withTheFigure)
        {
            Assert.Equal("2.0", new string([.. line.Where(c => char.IsDigit(c) || c == '.')]).Trim('.'));
        }
    }

    /// <summary>The loss line files under Nebula Mutual and the place of repair — and under nothing else.</summary>
    [Fact]
    public void TheLossIsFiledUnderNebulaMutualAndThePlace()
    {
        Assert.Equal(
            CaseSubjects.Line(CaseSubjects.Office("Nebula Mutual"), CaseSubjects.Place("The Deep")),
            HullClaim.SubjectsFor("The Deep"));
    }

    // ── THE DAYS ARE COMPUTED, NEVER ASSUMED ────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>{n} IS THE MEND'S REAL ELAPSED TIME, FIRST HOLE TO CLEAR — AND A RE-HOLE INSIDE THE WINDOW EXTENDS THE SAME
    /// LOSS.</b> Swept over several first-hole times and re-hole offsets, never sampled: an untouched window is the two
    /// days the page's constant says; a window holed again half a day in runs to two days past THAT hole and the loss is
    /// 2.5 days, not 2.0 (the assumed figure) and not 2.0 + 2.0 (a second loss).
    ///
    /// <para><b>Proven RED</b> by <c>Hole</c> ignoring an open window (a fresh window every time: 2.0, and the first-hole
    /// time lost), and by <c>DaysLost</c> returning the constant.</para>
    /// </summary>
    [Fact]
    public void TheDaysAreTheMendsOwnElapsedTimeAndAReHoleExtendsTheSameLoss()
    {
        const double Window = 2 * 86400.0;
        foreach (double first in new[] { 0.0, 1234.5, 86400.0 * 7, 3.0e6 })
        {
            HullClaim.Mend plain = HullClaim.Hole(null, first, Window);
            Assert.Equal(new HullClaim.Mend(first, first + Window), plain);
            Assert.True(HullClaim.IsDone(plain, first + Window));
            Assert.False(HullClaim.IsDone(plain, first + Window - 1));
            Assert.Equal(20, HullClaim.TenthsOf(HullClaim.DaysLost(plain, first + Window)));

            foreach (double offsetDays in new[] { 0.25, 0.5, 1.0, 1.9 })
            {
                double again = first + (offsetDays * 86400.0);
                HullClaim.Mend extended = HullClaim.Hole(plain, again, Window);
                Assert.Equal(first, extended.HoledAt);
                Assert.Equal(again + Window, extended.ClearsAt);
                Assert.Equal(
                    HullClaim.TenthsOf(2.0 + offsetDays),
                    HullClaim.TenthsOf(HullClaim.DaysLost(extended, extended.ClearsAt)));
            }
        }

        // …and a mend that cleared a frame LATE reports the days it really took.
        HullClaim.Mend late = HullClaim.Hole(null, 0, Window);
        Assert.Equal(21, HullClaim.LossOf(late, Window + 8640).Tenths);
        Assert.Equal((long)(Window + 8640), HullClaim.LossOf(late, Window + 8640).DoneAt);
    }

    [Theory]
    [InlineData(20, "2.0")]
    [InlineData(34, "3.4")]
    [InlineData(5, "0.5")]
    [InlineData(105, "10.5")]
    public void TheDaysReadWithOneDecimalInEveryCulture(int tenths, string text)
    {
        Assert.Equal(text, HullClaim.DaysText(tenths));
    }

    // ── THE REGISTER ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The loss, the filed and the booked tags — spelled out, because they ride the vault.</summary>
    [Fact]
    public void TheTagsAreWhatTheVaultCarries()
    {
        var loss = new HullClaim.Loss(172800, 20);
        Assert.Equal("claim:loss:172800:20", HullClaim.LossTag(loss));
        Assert.Equal("claim:filed:172800", HullClaim.FiledTag(172800));
        Assert.Equal("claim:booked:172800", HullClaim.BookedTag(172800));
        Assert.Equal("claim-form-filled:172800:20", HullClaim.FilledId(loss));
    }

    /// <summary>
    /// <b>THE NEWEST UNCLAIMED LOSS IS THE ONE COPIED; A FILED ONE IS NOT UNCLAIMED.</b> Three losses, the newest
    /// filed: the desk copies the second; with that filed too, the first; with all filed, nothing — over the register in
    /// every insertion order (a set has no order the answer may lean on).
    /// </summary>
    [Fact]
    public void TheNewestUnclaimedLossIsTheOneCopied()
    {
        HullClaim.Loss a = new(100, 20), b = new(200, 25), c = new(300, 31);
        string[][] orders =
        [
            [HullClaim.LossTag(a), HullClaim.LossTag(b), HullClaim.LossTag(c)],
            [HullClaim.LossTag(c), HullClaim.LossTag(a), HullClaim.LossTag(b)],
            [HullClaim.LossTag(b), HullClaim.LossTag(c), HullClaim.LossTag(a)],
        ];
        foreach (string[] order in orders)
        {
            var register = new HashSet<string>(order, StringComparer.Ordinal);
            Assert.Equal(c, HullClaim.NewestUnclaimed(register));
            register.Add(HullClaim.FiledTag(c.DoneAt));
            Assert.Equal(b, HullClaim.NewestUnclaimed(register));
            Assert.Equal([a, b], HullClaim.Unclaimed(register));
            register.Add(HullClaim.FiledTag(b.DoneAt));
            Assert.Equal(a, HullClaim.NewestUnclaimed(register));
            register.Add(HullClaim.FiledTag(a.DoneAt));
            Assert.Null(HullClaim.NewestUnclaimed(register));
        }

        Assert.Null(HullClaim.NewestUnclaimed([]));
        Assert.Null(HullClaim.NewestUnclaimed(["claim:loss:not-a-number:20", "claim:loss:1", "claim:filed:5"]));
    }

    /// <summary>
    /// <b>THE VERB IS ON OFFER ONLY WITH BOTH A BLANK FORM AND AN UNCLAIMED LOSS.</b> The four corners of the two
    /// conditions; and the filled form's presence alone (no blank) is not an offer.
    /// </summary>
    [Fact]
    public void TheVerbNeedsTheBlankAndTheLoss()
    {
        string[] withLoss = [HullClaim.LossTag(new(100, 20))];
        Satchel.Item[] withBlank = [Blank];
        Satchel.Item[] withFilled = [new(Satchel.Kind.Paper, HullClaim.FilledId(new(100, 20)))];

        Assert.True(HullClaim.CanFillTheForm(withBlank, withLoss));
        Assert.False(HullClaim.CanFillTheForm(withBlank, []));
        Assert.False(HullClaim.CanFillTheForm([], withLoss));
        Assert.False(HullClaim.CanFillTheForm([], []));
        Assert.False(HullClaim.CanFillTheForm(withFilled, withLoss));
        Assert.False(HullClaim.CanFillTheForm(withBlank, [.. withLoss, HullClaim.FiledTag(100)]));
    }

    /// <summary>The fill replaces the blank with the filled form of the NEWEST loss and touches nothing else held.</summary>
    [Fact]
    public void TheFillMakesTheBlankIntoTheFilledFormOfTheNewestLoss()
    {
        var other = new Satchel.Item(Satchel.Kind.Paper, "some-other-paper");
        string[] register = [HullClaim.LossTag(new(100, 20)), HullClaim.LossTag(new(900, 27))];

        (IReadOnlyList<Satchel.Item> after, HullClaim.Loss used) = HullClaim.Fill([other, Blank], register)!.Value;

        Assert.Equal(new HullClaim.Loss(900, 27), used);
        Assert.Equal(["some-other-paper", "claim-form-filled:900:27"], after.Select(i => i.Id));
        Assert.False(HullClaim.HoldsTheBlank(after));
        Assert.True(HullClaim.HoldsAFilledForm(after));
        Assert.Equal(new HullClaim.Loss(900, 27), HullClaim.TheFilledFormHeld(after));
        Assert.Null(HullClaim.Fill([other], register));
        Assert.Null(HullClaim.Fill([Blank], []));
    }

    // ── THE PAPER READS AS A PAPER ──────────────────────────────────────────────────────────────────────

    /// <summary>The filled form is ONE authored sheet: its title and document from its own id, never torn into pages.</summary>
    [Fact]
    public void TheFilledFormIsAnAuthoredSheetThatReadsEveryWay()
    {
        string id = HullClaim.FilledId(new(1000, 34));
        Assert.True(HullClaim.IsTheFilledForm(id));
        Assert.False(HullClaim.IsTheFilledForm(AdjustersRoom.SheetId));
        Assert.False(HullClaim.IsTheFilledForm("claim-form-filled:x:y"));
        Assert.False(HullClaim.IsTheFilledForm(null));

        Assert.True(FieldClue.IsAuthored(id));
        Assert.Equal("A claim form, filled", FieldClue.Title(id));
        Assert.Equal(HullClaim.FilledDocument(34), FieldClue.Document(id));
        Assert.Equal(1, PageGranularity.PagesIn(id));

        CarriedObject.Reveal read = CarriedObject.PaperReveal(id);
        Assert.Equal("A claim form, filled", read.Label);
        Assert.Equal(HullClaim.FilledDocument(34), read.Story);
        Assert.Equal("", read.ArtUrl);
    }

    // ── THE VAULT: A ROUND TRIP WITH A POSITIVE CONTROL ─────────────────────────────────────────────────

    /// <summary>
    /// <b>THE REGISTER AND THE FILLED FORM SURVIVE A SAVE AND A LOAD — AND THE PIN CANNOT PASS VACUOUSLY.</b> The #1365
    /// lesson: a pin on a reloaded page that asks "is the row there?" is green on a page that never loaded anything.
    /// So the reloaded vault is first asked a POSITIVE CONTROL (an unrelated tag written beside the claim's, and the
    /// filled form in the satchel section, must come back), and only then the claim's own facts: the loss is FILED
    /// (nothing left to copy), the form is held, and the booking row is present exactly once.
    ///
    /// <para><b>Proven RED</b> by the save dropping the register (control fails first), and by <c>BookedTag</c> being
    /// written under a different prefix than the one the page reads.</para>
    /// </summary>
    [Fact]
    public void TheBookingRowSurvivesAVaultRoundTripWithAPositiveControl()
    {
        var loss = new HullClaim.Loss(172800, 20);
        string[] written =
        [
            HullClaim.LossTag(loss), HullClaim.FiledTag(loss.DoneAt), HullClaim.BookedTag(loss.DoneAt),
            "adjuster:plate-read",   // the positive control: a tag #1365 shipped, written beside the claim's
        ];
        var filled = new Satchel.Item(Satchel.Kind.Paper, HullClaim.FilledId(loss));

        Vault back = VaultSerializer.Load(VaultSerializer.Save(new Vault
        {
            TurnedOver = new TurnedOverSection { Rooms = written },
            Satchel = new SatchelSection { Items = [filled.Stored] },
        }));

        // The control first: if nothing round-tripped, stop here.
        Assert.Contains("adjuster:plate-read", back.TurnedOver!.Rooms);
        Assert.Equal(written.OrderBy(s => s, StringComparer.Ordinal), back.TurnedOver!.Rooms.OrderBy(s => s, StringComparer.Ordinal));
        Assert.Single(back.Satchel!.Items);

        IReadOnlyList<string> rooms = back.TurnedOver!.Rooms;
        Assert.Equal(1, rooms.Count(t => t == HullClaim.BookedTag(loss.DoneAt)));
        Assert.Null(HullClaim.NewestUnclaimed(rooms));
        Assert.True(Satchel.Item.TryParse(back.Satchel!.Items[0], out Satchel.Item held));
        Assert.Equal(loss, HullClaim.TheFilledFormHeld([held]));
    }

    /// <summary>The same round trip WITHOUT a filing: the loss comes back unclaimed (so the control is not a constant).</summary>
    [Fact]
    public void AnUnfiledLossComesBackUnclaimed()
    {
        var loss = new HullClaim.Loss(86400, 21);
        Vault back = VaultSerializer.Load(VaultSerializer.Save(new Vault
        {
            TurnedOver = new TurnedOverSection { Rooms = [HullClaim.LossTag(loss)] },
        }));

        Assert.Equal(loss, HullClaim.NewestUnclaimed(back.TurnedOver!.Rooms));
    }
}
