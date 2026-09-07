using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 · <b>THE FAMILY, READ OFF WHAT WAS ACTUALLY DRAWN</b> — the render half of
/// <see cref="TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests"/>.
///
/// <para>What this part owns is every card the bench can raise wearing the shell's own foot: the
/// <c>.view-object</c> family itself, wave 4's cards that BORROWED the family's foot and now wear the
/// shell's while keeping their own name, and the bar table, which wears it and still says standing up costs
/// nothing. Reading the same family off the markup as TYPED is in the file that carries the class
/// docblock.</para>
/// </summary>
public sealed partial class TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests
{
    // ── The family, read off what was actually drawn ──────────────────────────────────────────────────

    /// <summary>
    /// …AND THE FOOT IS A DIRECT CHILD OF THE CARD, ON EVERY ONE THE BENCH CAN RAISE.
    ///
    /// <para>The guard above proves the SHAPE of the markup; this proves the DOM the shape produces, which is
    /// the thing #735's selector actually reads. A shell drawn <c>Hosted</c> instead of <c>Bare</c> would
    /// pass the guard above word for word and put a <c>display: contents</c> div between the card and its
    /// button — correct-looking markup, a silently unstuck foot (#997 wave 2 §2).</para>
    ///
    /// <para>Five of the twelve, and the other seven are named rather than skipped: the archive vision, the
    /// castaway, the wreck look, the kiosk card, the souvenir, the rep's pitch and the walk-in are gated on
    /// records this bench cannot build off-browser (a dice throw against a wreck, a walk up to a prop, a
    /// crossing's landing frame). They are covered by the source guard above and — for the last two — by
    /// #997 wave 2's own file.</para>
    /// </summary>
    [Theory]
    [InlineData("the story / reveal card", Docked, "_storyCard")]
    [InlineData("the captain's remote", Ashore, "_showCaptainsRemote")]
    [InlineData("the door board", Ashore, "_showDoorBoard")]
    [InlineData("the shape alarm panel", Ashore, "_showAlarmPanel")]
    [InlineData("the selfie", Ashore, "_selfieShot")]
    public async Task EveryViewObjectCardTheBenchCanRaiseWearsTheShellsOwnFoot(
        string name, string world, string gate)
    {
        using DeskBench bench = await DeskBench.BootAsync(world);
        RaiseTheCard(bench, gate);

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = painted.Root.Descendants()
            .FirstOrDefault(n => n.HasClass("view-object") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                $"{name}: setting {gate} drew nothing wearing .view-object. The driver and the markup's gate "
                + "have come apart; one of them has moved.");

        Assert.True(card.HasClass("overlay-shell"),
            $"{name}: its card is on the screen and it is not the shell's — #997 wave 3 put every card in "
            + "this family on OverlayShell, and this one has come back off it.");
        Assert.True(card.HasClass("overlay-shell-bare"),
            $"{name}: the shell drew it as something other than Bare. .view-object is a flex column whose "
            + "foot is pinned by `::deep .view-object > .view-object-close`, so any frame that contributes a "
            + "wrapper (or, worse, Hosted's `display: contents`) unsticks the way out while the markup goes "
            + "on looking right.");

        DeskBench.Painted.Node foot = card.Children.FirstOrDefault(n => n.HasClass("view-object-close"))
            ?? throw new Xunit.Sdk.XunitException(
                $"{name}: its way out is not a DIRECT child of the card. That is the exact relation #735's "
                + "sticky foot is written against — something has come between them.");

        Assert.True(foot.Handlers.ContainsKey("onclick"),
            $"{name}: the way out is drawn and nothing is wired to it — a control that LOOKS like a way out "
            + "and is not one, which is what the owner's ruling of 2026-08-24 forbids.");

        await bench.PressAsync(foot.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();
        Assert.DoesNotContain(after.Root.Descendants(), n => n.HasClass("view-object") && !n.Hidden);
    }

    private static void RaiseTheCard(DeskBench bench, string gate) => bench.Poke(gate, gate switch
    {
        "_storyCard" => ((StoryBeats.Beat Beat, string? Subject, string? Outcome)?)
            (StoryBeats.Beat.BerthGreatPort, "selene-gate", null),
        "_selfieShot" => new CapturedSelfie(
            "spot-the-tilt", "THE CAPTAIN, HERE", "Nobody will believe it. That is the point.",
            "art/selfie-the-tilt.jpg", 1, 12, "spot"),
        _ => true,
    });

    // ── #997 wave 4: the cards that BORROWED the family's foot, read off what was drawn ───────────────

    /// <summary>
    /// EVERY BORROWED FOOT IS THE SHELL'S OWN NOW — AND EVERY CARD KEPT ITS NAME.
    ///
    /// <para>Thirteen cards wore <c>.view-object-close</c> on a root that is not <c>.view-object</c>: seven
    /// vent boards, the operating log, both treasure maps, the lift's car panel, the bar table and the
    /// satchel. Wave 4 put them on the shell WITHOUT taking their roots away — a vent board is a vent board
    /// and not a view-object, the shell is the mechanism and the root class is the identity, and the alias
    /// law (#995) wants the name stable because its completeness guard reads the markup as typed.</para>
    ///
    /// <para>So this asks all four things at once, which is what a migration can break: the card still wears
    /// ITS OWN class, it is the shell's, it is <c>Bare</c> (the frame that keeps the way out a DIRECT child —
    /// <c>Hosted</c> would pass every source guard in this file and put a <c>display: contents</c> div
    /// between them), its way out still says the word it always said, and pressing it takes the card
    /// down.</para>
    ///
    /// <para><b>The wording is asserted as a literal on purpose.</b> These cards' feet are not ✕ — they say
    /// "Step away", "Not yet", "Log it", "Close". That is this family's idiom and the migration's whole
    /// claim is that not one of those words moved, so the words are written here where a change to them
    /// costs an edit to a test rather than passing unnoticed.</para>
    /// </summary>
    [Theory]
    [InlineData("the wreck's atmosphere board", FreeFlying, "vent-board", "_showVentPanel", "Step away")]
    [InlineData("her own hull's board", FreeFlying, "vent-board", "_showShipBoard", "Step away")]
    [InlineData("the hull-charge board", FreeFlying, "vent-board", "_showChargeBoard", "Step away")]
    [InlineData("her scuttling charges", FreeFlying, "vent-board", "_showShipScuttlePanel", "Step away")]
    [InlineData("the scuttling panel", FreeFlying, "vent-board", "_showScuttlePanel", "Step away")]
    [InlineData("the scuttle epitaph", FreeFlying, "vent-board", "_scuttleEpitaph", "Log it")]
    [InlineData("the treasure map", FreeFlying, "treasure-map-card", "_showWreckChoice", "Not yet")]
    [InlineData("the lift's car panel", Ashore, "lift-panel", "_showLiftPanel", "Close")]
    [InlineData("the satchel", Ashore, "satchel", "_showSatchel", "Close")]
    public async Task EveryCardThatBorrowedTheFamilysFootWearsTheShellsOwnAndKeepsItsName(
        string name, string world, string root, string gate, string wording)
    {
        using DeskBench bench = await DeskBench.BootAsync(world);
        RaiseTheBorrower(bench, gate);

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = painted.Root.Descendants()
            .FirstOrDefault(n => n.HasClass(root) && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                $"{name}: setting {gate} drew nothing wearing .{root}. The driver and the markup's gate have "
                + "come apart; one of them has moved.");

        Assert.True(card.HasClass("overlay-shell"),
            $"{name}: its card is on the screen and it is not the shell's — #997 wave 4 put every card that "
            + "borrows .view-object-close on OverlayShell, and this one has come back off it.");
        Assert.True(card.HasClass("overlay-shell-bare"),
            $"{name}: the shell drew it as something other than Bare. Bare is what keeps the way out a "
            + "DIRECT child of the card, which is the relation #735's sticky feet are written against and "
            + "the only reason .treasure-map-card > .view-object-close still reaches anything.");
        Assert.True(card.HasClass(root),
            $"{name}: the card has lost its own class. The shell is the MECHANISM and .{root} is the "
            + "IDENTITY — a vent board is a vent board, not a view-object — and #995's completeness guard "
            + "reads that name off the markup as typed.");

        DeskBench.Painted.Node foot = card.Children
            .FirstOrDefault(n => n.HasClass("view-object-close") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                $"{name}: its way out is not a DIRECT child of the card. Something has come between them, "
                + "and everything #735 pins on this family is written against that exact relation.");

        Assert.Equal(wording, foot.Name);

        Assert.True(foot.Handlers.ContainsKey("onclick"),
            $"{name}: the way out is drawn and nothing is wired to it — a control that LOOKS like a way out "
            + "and is not one, which is what the owner's ruling of 2026-08-24 forbids.");

        await bench.PressAsync(foot.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.True(after.Root.Descendants().All(n => !n.HasClass(root) || n.Hidden),
            $"{name}: \"{wording}\" was pressed and the card is still on the screen. The shell's dismiss "
            + "runs whatever OnClose is wired to and nothing else — a way out wired to something that is not "
            + "this card's close verb is a control that looks like a way out and is not one.");
    }

    /// <summary>Put one of the borrowers on the screen. Five of the nine are a single bool; the four built on
    /// a hull need the hull, and it is Core's own <see cref="Derelict.Wreck"/> rather than a stand-in, so
    /// everything the card prints past the gate is the shipping content.</summary>
    private static void RaiseTheBorrower(DeskBench bench, string gate)
    {
        if (gate is "_showVentPanel" or "_showScuttlePanel" or "_showWreckChoice")
        {
            bench.Poke("_wreck", new Derelict.Wreck(
                "shell-wave-4", "Borrowed Foot", Derelict.WreckCause.HullBreach, 250_000, 40.0));
        }

        bench.Poke(gate, gate switch
        {
            // The epitaph IS its own text, and _scuttleHeardIt stays false, which is the "Log it" face.
            "_scuttleEpitaph" => "Something in her went quiet a long way off.",
            _ => true,
        });
    }

    /// <summary>
    /// …AND THE ONE OF THE THIRTEEN THAT IS NOT A THING BUT A ROOM: THE BAR TABLE.
    ///
    /// <para>Its own theory, because it is the only borrower whose gate is a POSTURE. The card is drawn by
    /// <c>SeatedTable is { } tab</c> and only on the branch where somebody came to YOU (#865's fork: a seat
    /// you chose is a strip, a person who crosses the room to you is a card), so it is stood up the way
    /// #997 wave 3 stood up the collector's demand — the page's own state object, filled with Core's own
    /// scene, and everything past that is the shipping markup.</para>
    ///
    /// <para>The claim it proves is the same as the theory above and it matters most here: the card still
    /// wears <c>deck-offer-card table-card</c>, which is what #784's own guard reads to know the seated
    /// frame forked correctly, and its way out still says <b>Close</b> under the title the owner wrote for
    /// it — <i>"Stand up. It costs nothing and it never will."</i></para>
    /// </summary>
    [Fact]
    public async Task TheBarTableWearsTheShellsOwnFootAndStillSaysStandUpCostsNothing()
    {
        using DeskBench bench = await DeskBench.BootAsync(Ashore);

        Type talkType = typeof(Map).GetNestedType("TableTalk", BindingFlags.NonPublic | BindingFlags.Public)
                        ?? throw new InvalidOperationException(
                            "Map.TableTalk is gone — this guard cannot seat a captain without it.");
        object talk = Activator.CreateInstance(talkType, nonPublic: true)!;
        SetOn(talkType, talk, "Key", "watch:shell-wave-4:0");
        SetOn(talkType, talk, "Index", 0);
        SetOn(talkType, talk, "Who", CanteenTable.Who.Hand);
        SetOn(talkType, talk, "Plate", "THE HAND");
        SetOn(talkType, talk, "Scene", CanteenTable.SceneFor(CanteenTable.Who.Hand));
        SetOn(talkType, talk, "Solo", false);

        // #865's fork: TheyCameToYou is what makes this a CARD rather than the docked strip.
        SetOn(talkType, talk, "TheyCameToYou", true);

        object seating = bench.Peek("_seating")
                         ?? throw new InvalidOperationException("Map._seating is gone.");
        seating.GetType().GetProperty("Table", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(seating, talk);

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = painted.Root.Descendants()
            .FirstOrDefault(n => n.HasClass("table-card") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                "seating the captain at a table somebody came to drew nothing wearing .table-card. Either "
                + "the seated fork has moved or TableTalk no longer has the fields this stands it up with.");

        Assert.True(card.HasClass("overlay-shell") && card.HasClass("overlay-shell-bare"),
            "the bar table is not the shell's Bare frame. #735 pins the offer family's action rows as DIRECT "
            + "children of .deck-offer-card; anything that wraps them unsticks the lot.");
        Assert.True(card.HasClass("deck-offer-card"),
            "the table has lost the offer card's class. #784's own guard reads the string "
            + "\"deck-offer-card table-card\" out of the conversation branch to know the seated frame forked "
            + "the right way — the shell is the mechanism, that pair of names is the identity.");

        DeskBench.Painted.Node foot = card.Children
            .FirstOrDefault(n => n.HasClass("view-object-close") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                "the table's way out is not a DIRECT child of the card.");

        Assert.Equal("Close", foot.Name);
        Assert.Equal("Stand up. It costs nothing and it never will.",
                     foot.Attributes.GetValueOrDefault("title"));

        await bench.PressAsync(foot.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.True(after.Root.Descendants().All(n => !n.HasClass("table-card") || n.Hidden),
            "the table's Close was pressed and the captain is still sitting there. Standing up is the one "
            + "move this card promises costs nothing.");
    }

    private static void SetOn(Type owner, object instance, string name, object value)
    {
        PropertyInfo property = owner.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
                                ?? throw new InvalidOperationException($"TableTalk.{name} is gone.");
        (property.GetSetMethod(nonPublic: true) ?? property.SetMethod)!
            .Invoke(instance, [value]);
    }
}
