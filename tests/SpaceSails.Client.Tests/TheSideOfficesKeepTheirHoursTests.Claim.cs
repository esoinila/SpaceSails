using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1151 slice 1 · <b>THE BOOKING, IN THE ADJUSTER'S ROOM.</b> The third half of
/// <see cref="TheSideOfficesKeepTheirHoursTests"/> (the helpers are the first half's): the console on her shipped watch
/// with a filled form held, the booking's vault row (once per filled form, surviving a save and a load with a positive
/// control), the shut door's extra line, the desk's fresh blank, and the dev start. Every path that lacks a filled form
/// is the room #1365 shipped.
/// </summary>
public sealed partial class TheSideOfficesKeepTheirHoursTests
{
    private static readonly HullClaim.Loss ALoss = new(172800, 20);

    private static Satchel.Item TheFilled => new(Satchel.Kind.Paper, HullClaim.FilledId(ALoss));

    private static Pages.Map TheDeep(string name) => OnTheHotelLevel(name, AdjustersRoom.HavenId);

    private static void Carry(Pages.Map map, params Satchel.Item[] items)
    {
        var sleeve = (List<Satchel.Item>)Read(map, "_satchel")!;
        sleeve.Clear();
        sleeve.AddRange(items);
    }

    /// <summary>Stand at the booking console and assert it is what the key would answer.</summary>
    private static void StandAtTheConsole(Pages.Map map)
    {
        DeckReachability.Point book = HavenInterior.TheBookingConsoleAt(AdjustersRoom.HavenId)!.Value;
        StandAt(map, book.X, book.Y - 0.9);
        DeckPlan.ConsoleSpot? near = Deck(map).NearestConsoleSpot(book.X, book.Y - 0.9);
        Assert.Equal(HullClaim.BookLabel, near?.Label);
    }

    private static IEnumerable<FieldNote> Booked(Pages.Map map) =>
        Notes(map).Where(n => n.Text == HullClaim.BookEntryLine);

    /// <summary>
    /// <b>THE CONSOLE STANDS ONLY ON HER WATCH WITH A FILLED FORM HELD — AND THE ROOM IS THE ROOM #1365 SHIPPED
    /// OTHERWISE.</b> Over four runs' seeds (the watch is the run's): nothing held, the blank held and the filled form
    /// held, each on her watch and off it. The console is drawn on exactly one of the six; the blank-only and
    /// nothing-held rooms are the shipped room to the plan (the sheet where it lay, no console); a captain inside when
    /// her watch turns, form in hand, finds the doorway open and no console.
    ///
    /// <para><b>Proven RED</b> by the held-form clause dropped (the console drawn for a captain with nothing), by the
    /// watch clause dropped (the console drawn inside past her watch), and by the console drawn for a held BLANK.</para>
    /// </summary>
    [Fact]
    public void TheConsoleStandsOnlyOnHerWatchWithAFilledFormHeld()
    {
        SideOffice office = SideOffices.Adjuster;
        Pages.Map map = TheDeep("claim-console");
        var drawn = new HashSet<string>();

        for (int run = 0; run < 4; run++)
        {
            Set(map, "_activeThreadId", $"claim-thread-{run}");
            long his = TheOfficesWatch(map, office);
            foreach ((string held, Satchel.Item[] items) in new[]
                     {
                         ("nothing", Array.Empty<Satchel.Item>()),
                         ("blank", new[] { office.TheSheet }),
                         ("filled", new[] { TheFilled }),
                     })
            {
                foreach (bool hers in new[] { true, false })
                {
                    Carry(map, items);
                    At(map, Within(hers ? his : his + 1));
                    Frames(map, 2);
                    bool console = HavenInterior.TheBookingConsoleLiesIn(Deck(map));
                    Assert.True(
                        console == (hers && held == "filled"),
                        $"run {run}, {held}, her watch={hers}: the console is {(console ? "drawn" : "missing")}.");
                    if (held != "filled")
                    {
                        Assert.Equal(hers, HavenInterior.TheOfficeStandsOpenIn(Deck(map)));
                    }

                    drawn.Add($"{held}/{hers}/{console}");
                }
            }
        }

        Assert.Contains("filled/True/True", drawn);
        Assert.Contains("filled/False/False", drawn);

        // Inside when her watch turns, form in hand: the doorway holds (#822) and the console does not.
        long watch = TheOfficesWatch(map, office);
        Carry(map, TheFilled);
        At(map, Within(watch));
        Frames(map, 2);
        Assert.True(HavenInterior.TheBookingConsoleLiesIn(Deck(map)));
        DeckReachability.Point inside = Inside(AdjustersRoom.HavenId);
        StandAt(map, inside.X, inside.Y);
        At(map, Within(watch + 1));
        Frames(map, 2);
        Assert.True(HavenInterior.TheOfficeStandsOpenIn(Deck(map)));
        Assert.False(HavenInterior.TheBookingConsoleLiesIn(Deck(map)), "the console stands past her watch.");
    }

    /// <summary>
    /// <b>THE BOOKING: THE LINE EVERY PRESS, THE VAULT ROW AND THE BOOK'S 📍 ENTRY ONCE PER FILLED FORM — AND IT
    /// SURVIVES A SAVE AND A LOAD.</b> Her watch, the filled form held, the console pressed three times: each press
    /// says the booked line; the register gains the booked tag once; the book gains one 📍 entry, under Nebula Mutual
    /// and The Deep, once. Then the register is saved, loaded and restored into a FRESH page — and the pin cannot pass
    /// vacuously: the control first (a tag #1365 shipped, written beside the booking, must be on the reloaded page, and
    /// the fresh page must start without the booking), then the booking's own row on the reloaded page, and a press
    /// there says the line and files NOTHING a second time.
    ///
    /// <para><b>Proven RED</b> by the booked tag never written (a second entry on the reloaded page), by the tag
    /// written but not restored from the vault section, and by the entry filed on every press.</para>
    /// </summary>
    [Fact]
    public void TheBookingIsOnePulseEveryPressOneRowAndOneEntryAndSurvivesTheVault()
    {
        SideOffice office = SideOffices.Adjuster;
        Pages.Map map = TheDeep("claim-booking");
        long his = TheOfficesWatch(map, office);
        At(map, Within(his));
        Carry(map, TheFilled);
        Frames(map, 2);
        Register(map).Add(office.PlateReadTag);   // the control's tag, written beside the claim's
        StandAtTheConsole(map);

        // The FIRST press books and says the booked line; the presses after it (slice 2) seat the interview instead.
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "InteractAtConsole");
        Assert.Equal(HullClaim.BookedLine, InTheSlot(map));
        Invoke(map, "FileNote", "an unrelated line between the presses", "·");
        Invoke(map, "InteractAtConsole");
        Assert.NotNull(Read(map, "_interview"));   // the second press SEATS the interview — it does not re-book
        Invoke(map, "CloseViewObject");

        Assert.Equal(1, Register(map).Count(t => t == HullClaim.BookedTag(ALoss.DoneAt)));
        FieldNote entry = Assert.Single(Booked(map));
        Assert.Equal("📍", entry.Glyph);
        Assert.Equal(TheSubjects(AdjustersRoom.HavenId), entry.Subjects);

        // Save, load, and restore into a fresh page.
        var section = (TurnedOverSection?)Invoke(map, "TheRoomsGoneThrough")!;
        Vault back = VaultSerializer.Load(VaultSerializer.Save(new Vault
        {
            TurnedOver = section,
            Satchel = new SatchelSection { Items = [TheFilled.Stored] },
        }));

        Pages.Map fresh = TheDeep("claim-booking-reloaded");
        Assert.DoesNotContain(HullClaim.BookedTag(ALoss.DoneAt), Register(fresh));   // a fresh page starts without it
        Assert.DoesNotContain(office.PlateReadTag, Register(fresh));
        Invoke(fresh, "RestoreTheRoomsGoneThrough", back);

        Assert.Contains(office.PlateReadTag, Register(fresh));                       // the positive control
        Assert.Contains(HullClaim.BookedTag(ALoss.DoneAt), Register(fresh));         // the booking row itself
        Assert.True(Satchel.Item.TryParse(back.Satchel!.Items[0], out Satchel.Item held));
        Carry(fresh, held);
        At(fresh, Within(TheOfficesWatch(fresh, office)));
        Frames(fresh, 2);
        StandAtTheConsole(fresh);
        Assert.Null(Read(fresh, "_interview"));
        Invoke(fresh, "InteractAtConsole");

        Assert.NotNull(Read(fresh, "_interview"));   // the reloaded booking is a BOOKED one: the press seats, it does not re-book
        Assert.Empty(Booked(fresh));   // the register remembered: no second entry for one filled form
    }

    /// <summary>
    /// <b>ONE ENTRY PER FILLED FORM, NOT ONE PER RUN.</b> Two filled forms held: the first press books the first form
    /// (one row, one entry); a second filled form — another loss, another form — books on its own press with its own row
    /// and its own entry.
    /// </summary>
    [Fact]
    public void EachFilledFormIsBookedOnceAndEachGetsItsOwnRow()
    {
        SideOffice office = SideOffices.Adjuster;
        Pages.Map map = TheDeep("claim-two-forms");
        At(map, Within(TheOfficesWatch(map, office)));
        var second = new HullClaim.Loss(300000, 31);
        Carry(map, TheFilled);
        Frames(map, 2);
        StandAtTheConsole(map);
        Invoke(map, "InteractAtConsole");
        Assert.Single(Booked(map));

        Carry(map, TheFilled, new Satchel.Item(Satchel.Kind.Paper, HullClaim.FilledId(second)));
        Invoke(map, "FileNote", "an unrelated line between the presses", "·");
        Invoke(map, "InteractAtConsole");

        Assert.Equal(2, Booked(map).Count());
        Assert.Contains(HullClaim.BookedTag(ALoss.DoneAt), Register(map));
        Assert.Contains(HullClaim.BookedTag(second.DoneAt), Register(map));
    }

    /// <summary>
    /// <b>THE SHUT DOOR SAYS ITS SHIPPED LINE — AND, WITH A FILLED FORM HELD, ONE LINE MORE AFTER IT.</b> Off her
    /// watch at the doorstep: nothing held, the blank held and a form for somebody else's office (the forwarding note)
    /// say exactly <see cref="AdjustersRoom.ShutLine"/>; the filled form adds the canon line after it. The other
    /// office's shut door never says it.
    ///
    /// <para><b>Proven RED</b> by the extra line appended for a held blank, and by it appended at the forwarding desk.</para>
    /// </summary>
    [Fact]
    public void TheShutDoorAddsItsLineOnlyWithAFilledFormHeld()
    {
        const string Plain = "The adjuster keeps hours. The hours are kept elsewhere.";
        Pages.Map map = TheDeep("claim-shut");
        long his = TheOfficesWatch(map, SideOffices.Adjuster);
        At(map, Within(his + 1));
        Frames(map, 2);
        StandAt(map, Doorstep(AdjustersRoom.HavenId).X, Doorstep(AdjustersRoom.HavenId).Y);

        foreach ((Satchel.Item[] items, string says) in new[]
                 {
                     (Array.Empty<Satchel.Item>(), Plain),
                     (new[] { SideOffices.Adjuster.TheSheet }, Plain),
                     (new[] { SideOffices.Forwarding.TheSheet }, Plain),
                     (new[] { TheFilled },
                      Plain + " The form stays warm in the satchel. Nothing else here is."),
                 })
        {
            Carry(map, items);
            Set(map, "_pulse", PulseSlot.Empty);
            Invoke(map, "InteractAtConsole");
            Assert.Equal(says, InTheSlot(map));
        }
    }

    /// <summary>
    /// <b>A SECOND BLANK IS FETCHED THE WAY THE FIRST WAS — ONLY WHEN THERE IS SOMETHING TO CLAIM, ONE AT A TIME.</b>
    /// The sheet taken (the shipped state): with no unclaimed loss the desk stays bare on her watch whatever else is
    /// held or filed; with an unclaimed loss it lays a blank for a captain who holds none, including one still holding a
    /// filled (even booked) form for an earlier loss, and is bare again once he holds a blank.
    ///
    /// <para><b>Proven RED</b> by the unclaimed-loss clause dropped (paper laid with nothing to put on it), and by the
    /// held-blank clause dropped (a blank laid beside a blank).</para>
    /// </summary>
    [Fact]
    public void ASecondBlankComesOnlyAfterAFormHasBeenFilledAndOnlyToAnEmptyHand()
    {
        SideOffice office = SideOffices.Adjuster;
        Pages.Map map = TheDeep("claim-restock");
        At(map, Within(TheOfficesWatch(map, office)));
        Register(map).Add(office.SheetTakenTag);

        Carry(map);
        Frames(map, 2);
        Assert.False(HavenInterior.TheSheetLiesIn(Deck(map)), "the shipped bare desk was refilled for a captain who lost nothing.");

        // A loss already copied onto a form is nothing left to claim: still bare, held or not.
        Register(map).Add(HullClaim.LossTag(ALoss));
        Register(map).Add(HullClaim.FiledTag(ALoss.DoneAt));
        foreach (Satchel.Item[] held in new[] { Array.Empty<Satchel.Item>(), new[] { TheFilled } })
        {
            Carry(map, held);
            Frames(map, 2);
            Assert.False(HavenInterior.TheSheetLiesIn(Deck(map)), "paper laid with no unclaimed loss to put on it.");
        }

        // A NEW loss: a blank for an empty hand, and, booked or not, for a hand still holding the earlier filled form.
        var next = new HullClaim.Loss(300000, 31);
        Register(map).Add(HullClaim.LossTag(next));
        Carry(map);
        Frames(map, 2);
        Assert.True(HavenInterior.TheSheetLiesIn(Deck(map)), "no fresh blank for a captain with a new loss and an empty hand.");
        Carry(map, TheFilled);
        Register(map).Add(HullClaim.BookedTag(ALoss.DoneAt));
        Frames(map, 2);
        Assert.True(HavenInterior.TheSheetLiesIn(Deck(map)), "a captain with a new loss must not have to drop his filed form.");

        Carry(map, office.TheSheet);
        Frames(map, 2);
        Assert.False(HavenInterior.TheSheetLiesIn(Deck(map)), "a blank laid beside a blank.");
        Carry(map, TheFilled, office.TheSheet);
        Frames(map, 2);
        Assert.False(HavenInterior.TheSheetLiesIn(Deck(map)));

        Carry(map);
        Frames(map, 2);
        StandAt(map, Inside(AdjustersRoom.HavenId).X, Inside(AdjustersRoom.HavenId).Y);
        Invoke(map, "InteractAtConsole");
        Assert.Single((List<Satchel.Item>)Read(map, "_satchel")!, i => i == office.TheSheet);
        Assert.False(HavenInterior.TheSheetLiesIn(Deck(map)));
    }

    /// <summary>
    /// <b>THE DEV START: ONE URL, A COMPLETED MEND IN THE BOOK, THE BLANK FORM HELD, HER DOOR AJAR.</b> The row is in the
    /// front door's list, once, at <c>/map?claim=1</c>; booted at The Deep's hotel level on a watch that is NOT hers, the
    /// staging puts exactly one loss line (2.0 days, through the real writer) in the book, the blank form in the sleeve,
    /// the sheet-taken tag in the register, the door ajar at the doorstep and the console NOT drawn (nothing filled yet);
    /// and the query parse implies the dock, the walk and the ride.
    ///
    /// <para><b>Proven RED</b> by the row left out of <c>DevStarts</c>, by the cheat not forcing the door, and by the
    /// staging typed as a tag rather than written through the loss writer (no line in the book).</para>
    /// </summary>
    [Fact]
    public void TheClaimIsOneDevStartAway()
    {
        Assert.Single(DevStarts.All, e => e.Url == "/map?claim=1");

        SideOffice office = SideOffices.Adjuster;
        Pages.Map map = TheDeep("claim-dev");
        Set(map, "Navigation", new TheBootBuildsTheSameWorldTests.Bench("/map?claim=1"));
        At(map, Within(TheOfficesWatch(map, office) + 1));   // not her watch: the latch overrules
        Invoke(map, "StandAtTheOfficeIfAsked");
        Frames(map, 2);

        Assert.True(HavenInterior.TheOfficeStandsOpenIn(Deck(map)));
        FieldNote line = Assert.Single(Notes(map), n => n.Text.StartsWith("Hull holed — ", StringComparison.Ordinal));
        Assert.Equal(HullClaim.LossLine(20), line.Text);
        Assert.Single((List<Satchel.Item>)Read(map, "_satchel")!, i => i == office.TheSheet);
        Assert.Contains(office.SheetTakenTag, Register(map));
        Assert.Equal(1, Register(map).Count(t => t.StartsWith("claim:loss:", StringComparison.Ordinal)));
        Assert.False(HavenInterior.TheBookingConsoleLiesIn(Deck(map)));
        DeckReachability.Point door = Doorstep(AdjustersRoom.HavenId);
        Assert.True(
            Hypot((double)Read(map, "_avatarX")! - door.X, (double)Read(map, "_avatarY")! - door.Y) <= DeckPlan.InteractRadius,
            "the tester is not at the door.");

        // …and the query parse alone implies the dock, the walk ashore and the ride down.
        Assert.Equal("/map?claim=1", DevStarts.All.Single(e => e.Url == "/map?claim=1").Url);
        Assert.True(HullClaim.CheatIn("/map?claim=1"));
        Assert.False(HullClaim.CheatIn("/map?claim=0"));
        Assert.False(HullClaim.CheatIn("/map?office=open"));
        Assert.Equal(PreservationOffice.Cheat.Open, PreservationOffice.CheatIn("/map?claim=1"));
        Assert.Equal(PreservationOffice.Cheat.Shut, PreservationOffice.CheatIn("/map?claim=1&office=shut"));
    }

    /// <summary>
    /// <b>THE DEV START IS ONE URL ALL THE WAY: <c>/map?claim=1</c> BOOTS DOCKED AT THE DEEP, ON THE HOTEL LEVEL, WITH
    /// THE STAGING DONE.</b> The shipping page booted past the browser gate at the bare URL — no dock, no ashore, no
    /// floor in it: the page is docked at The Deep, below the concourse, the door is ajar, the one loss line is in the
    /// book and the blank form is in the sleeve. The control: the same page booted at <c>?claim=0</c> is none of that.
    ///
    /// <para><b>Proven RED</b> by the parse not implying the ride (the page boots on the concourse) and by the staging
    /// call removed (no line, no form).</para>
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task TheBareClaimUrlBootsAtTheDeepWithTheStagingDone()
    {
        PastTheGateBench.Boot control = await PastTheGateBench.BootAsync("/map?claim=0");
        Assert.Null(control.Threw);
        Assert.NotEqual(AdjustersRoom.HavenId, control.Read<string?>("_dockedHavenId"));
        Assert.DoesNotContain(
            (IEnumerable<FieldNote>)control.Field("_fieldNotes")!, n => n.Text.StartsWith("Hull holed — ", StringComparison.Ordinal));

        PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync("/map?claim=1");
        Assert.Null(booted.Threw);
        Assert.Equal(AdjustersRoom.HavenId, booted.Read<string?>("_dockedHavenId"));
        Assert.Equal(HavenLevels.ServiceLevel, booted.Read<int>("_havenFloor"));
        Assert.True(HavenInterior.TheOfficeStandsOpenIn(booted.Read<DeckPlan>("_deckPlan")));
        Assert.Equal(
            HullClaim.LossLine(20),
            Assert.Single(
                (IEnumerable<FieldNote>)booted.Field("_fieldNotes")!,
                n => n.Text.StartsWith("Hull holed — ", StringComparison.Ordinal)).Text);
        Assert.Single(booted.Read<List<Satchel.Item>>("_satchel"), i => i == SideOffices.Adjuster.TheSheet);
    }
}
