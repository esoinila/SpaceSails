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
/// #997 waves 3 and 4 · <b>THE SHELL OWNS THE CARD FAMILY, ITS BORROWERS, AND THE COLLECTOR'S DEMAND.</b>
///
/// <para>The dismissibility law (<see cref="EveryPopUpCanBeDismissedTests"/>) asks whether a surface can be
/// got rid of. What a MIGRATION can break is narrower and this file asks that instead, in two halves.</para>
///
/// <list type="number">
/// <item><b>The family.</b> Twelve cards in this client are rooted on <c>.view-object</c>, and #735's law
/// pins their action row to the bottom of the scrollport with <c>::deep .view-object &gt;
/// .view-object-close</c> — a DIRECT-child relation. Ten of them hand-rolled that button until this wave.
/// The family guard reads the markup as typed and requires every one of them to be drawn through the shell,
/// so a THIRTEENTH card that hand-rolls the foot fails here with its own file and line rather than shipping
/// a card whose way out has quietly unstuck.</item>
/// <item><b>The demand.</b> <see cref="SpaceSails.Client.Components.OverlayDismiss.ByDecision"/> is the one
/// mode #997 shipped that no shipping surface had ever been drawn through. The BUSTED panel is the surface
/// it was written for, and driving it found what #997 found on the rep's card: the register's claim about it
/// was false. Its three answers do not close it — they turn its page. Every chain still ENDS in a close, and
/// that is proved here by following each one to the end rather than by believing a flag.</item>
/// <item><b>The borrowers.</b> Wave 4. Thirteen more cards wore <c>.view-object-close</c> on a root that is
/// NOT <c>.view-object</c> — seven vent boards, the operating log, both treasure maps, the lift's car panel,
/// the bar table and the satchel. They are on the shell now and they KEPT THEIR ROOTS: the shell is the
/// mechanism, the root class is the identity, and the alias law wants that name stable. The count that used
/// to hold this line has been replaced by a NAMED list with reasons, because a count says how many and never
/// says which.</item>
/// </list>
/// </summary>
public sealed class TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests
{
    // ── The family, read off the markup as typed ──────────────────────────────────────────────────────

    /// <summary>
    /// EVERY <c>.view-object</c> CARD IN THE CLIENT IS DRAWN THROUGH THE SHELL.
    ///
    /// <para>Read as TYPED, in #992's own idiom and for its reason: a card that named itself through a
    /// parameter would vanish from a guard that reads <c>class="…"</c>, so the rule is that the class stays a
    /// lowercase attribute and this walks them all.</para>
    ///
    /// <para><b>Two questions, because two things can go wrong.</b> A new card can be typed as a plain
    /// <c>&lt;div&gt;</c> (the family grows a thirteenth hand-rolled foot), or a card that HAS the shell can
    /// grow a SECOND, hand-written way out inside it — which leaves two buttons doing one job, only one of
    /// them pinned and only one of them the one the shell's audit watches.</para>
    /// </summary>
    [Fact]
    public void EveryViewObjectCardInTheClientIsDrawnThroughTheShell()
    {
        var handRolled = new List<string>();
        var twoFeet = new List<string>();

        foreach (string file in RazorFiles())
        {
            string[] lines = File.ReadAllLines(file);
            string shortName = Path.GetFileName(file);
            int insideAShell = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                bool opensAShell = lines[i].Contains("<OverlayShell", StringComparison.Ordinal);

                foreach (Match attribute in Regex.Matches(lines[i], "class=\"([^\"]*)\""))
                {
                    string[] classes = attribute.Groups[1].Value
                        .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                    string tag = TagOwning(lines, i, attribute.Index);

                    // The card root: the family's own class, on the element the foot hangs off.
                    if (classes.Contains("view-object", StringComparer.Ordinal)
                        && !string.Equals(tag, "OverlayShell", StringComparison.Ordinal))
                    {
                        handRolled.Add($"{shortName}:{i + 1}  <{tag} class=\"{attribute.Groups[1].Value}\">");
                    }

                    // A foot typed INSIDE a shell. Outside one it belongs to a card of another root — the
                    // vent boards, the satchel, the lift panel — which this wave did not migrate and which
                    // TheOtherCardsWearingTheFamilysFootOnlyEverGetFewer counts instead.
                    if (insideAShell > 0
                        && classes.Contains("view-object-close", StringComparer.Ordinal)
                        && !string.Equals(tag, "OverlayShell", StringComparison.Ordinal))
                    {
                        twoFeet.Add($"{shortName}:{i + 1}  <{tag} class=\"{attribute.Groups[1].Value}\">");
                    }
                }

                insideAShell += opensAShell ? 1 : 0;
                insideAShell -= CountOf(lines[i], "</OverlayShell>");
            }
        }

        Assert.True(handRolled.Count == 0,
            $"{handRolled.Count} card(s) rooted on .view-object are NOT drawn through OverlayShell:\n  - "
            + string.Join("\n  - ", handRolled)
            + "\n\nThe family's action row is pinned by `::deep .view-object > .view-object-close`, which "
            + "needs the way out to be a DIRECT child of the card. The shell guarantees that with "
            + "Frame=\"OverlayFrame.Bare\"; a hand-rolled card guarantees it only until somebody wraps "
            + "something in it. Give it a shell (#997 wave 3) — or, if it genuinely is not a card of this "
            + "family, do not give it the family's class.");

        Assert.True(twoFeet.Count == 0,
            $"{twoFeet.Count} .view-object-close button(s) are typed by hand INSIDE a shell:\n  - "
            + string.Join("\n  - ", twoFeet)
            + "\n\nThe foot is the shell's now (`DismissClass=\"view-object-close\"`). A hand-written one "
            + "beside it is two ways out on one card, and the shell's audit only knows about its own.");
    }

    /// <summary>
    /// THE ONLY FOOT STILL TYPED BY HAND IS THE ONE STRAGGLER NAMED HERE.
    ///
    /// <para>#997 wave 3 wrote this down as a CEILING of fifteen — a number anybody could make smaller.
    /// Wave 4 made it smaller thirteen times and then replaced the number, because a count says how many and
    /// never says <b>which</b>, and which is the thing worth holding. Every remaining hand-typed
    /// <c>.view-object-close</c> in this client must be one of the stragglers named below, WITH ITS REASON.
    /// A new card that borrows the family's button without its root fails here by file and line, and the
    /// only ways to make it pass are to migrate it or to write down why it cannot be.</para>
    ///
    /// <para><b>And the fifteen were fourteen.</b> Wave 3's count read <c>class="…"</c> off every line of
    /// every <c>.razor</c>, comments included — and Map.razor's own migration comment quotes the string
    /// <c>&lt;button class="view-object-close"&gt;</c> as an illustration of what was being deleted. So one
    /// of the fifteen was a sentence ABOUT the work rather than a card. The walk below skips razor comments,
    /// which is the difference between counting markup and counting text that looks like markup.</para>
    /// </summary>
    [Fact]
    public void TheOnlyFootStillTypedByHandIsTheOneStragglerNamedHere()
    {
        var byHand = new List<string>();

        foreach (string file in RazorFiles())
        {
            string[] lines = File.ReadAllLines(file);
            string shortName = Path.GetFileName(file);
            bool inComment = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool opens = line.Contains("@*", StringComparison.Ordinal);
                bool closes = line.Contains("*@", StringComparison.Ordinal);
                bool commented = inComment || opens;
                inComment = inComment ? !closes : (opens && !closes);
                if (commented)
                {
                    continue;   // a razor comment is TEXT, whatever it looks like
                }

                foreach (Match attribute in Regex.Matches(line, "class=\"([^\"]*)\""))
                {
                    if (!attribute.Groups[1].Value
                            .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
                            .Contains("view-object-close", StringComparer.Ordinal))
                    {
                        continue;
                    }

                    if (string.Equals(TagOwning(lines, i, attribute.Index), "OverlayShell",
                                      StringComparison.Ordinal))
                    {
                        continue;   // the shell drew it; the class is the alias law's, not a hand-roll
                    }

                    byHand.Add($"{shortName}:{i + 1}  ({WhichCard(file, i + 1)})");
                }
            }
        }

        var unexplained = byHand
            .Where(found => !TheNamedStragglers.ContainsKey(CardIn(found)))
            .ToList();

        Assert.True(unexplained.Count == 0,
            $"{unexplained.Count} button(s) wear .view-object-close and were typed by hand:\n  - "
            + string.Join("\n  - ", unexplained)
            + "\n\nThe family's foot is the shell's now (Frame=\"OverlayFrame.Bare\" plus "
            + "DismissClass=\"view-object-close\"), and #997 wave 4 put every card that borrows it on the "
            + "shell but one. A NEW hand-rolled foot is a card taking the family's wording and its look "
            + "without the direct-child relation #735's sticky foot is written against. Give it a shell — "
            + "or, if it genuinely cannot take one, name it in TheNamedStragglers with the reason, which is "
            + "the edit that makes somebody say why out loud.");

        // …and the list only ever gets shorter. A straggler that HAS been migrated must leave the list, or
        // its written-down reason rots into a sentence about a card that is no longer shaped that way.
        var stale = TheNamedStragglers.Keys
            .Where(named => !byHand.Any(found => CardIn(found) == named))
            .ToList();

        Assert.True(stale.Count == 0,
            $"{stale.Count} straggler(s) are named here and no longer wear a hand-typed foot: "
            + string.Join(" · ", stale)
            + ". Take them off the list — a written-down reason for a card that has moved on is worse than "
            + "no reason at all.");
    }

    /// <summary>
    /// THE STRAGGLERS, BY NAME AND WITH THE REASON. One.
    ///
    /// <para>Wave 4 took the other thirteen: seven vent boards, the operating log, both treasure maps, the
    /// lift's car panel, the bar table and the satchel.</para>
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> TheNamedStragglers =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["the locked door"] =
                "#603's locked door is the one card in this set whose way out is not the LAST thing in it. "
                + "Its way out is one of TWO buttons standing side by side in `.locked-door-actions`, beside "
                + "the brighter `🎒 Check your items` the door exists to advertise. A Bare shell draws its "
                + "dismiss as the card's last DIRECT child — that is the whole point of the frame and the "
                + "reason every other card in this wave could take it — so shelling this one would lift the "
                + "button out of that row and drop it on its own line underneath. That is a control moving "
                + "on the screen, which this migration does not do. It gets a shell when the row does.",
        };

    /// <summary>Which CARD a hand-rolled foot belongs to: the nearest root above it that names itself. The
    /// list is keyed on the card rather than on a line number, because a line number moves whenever anything
    /// above it is edited and a reason that drifts onto the wrong card is a lie.</summary>
    private static string WhichCard(string file, int line)
    {
        string[] lines = File.ReadAllLines(file);
        for (int back = Math.Min(line - 1, lines.Length - 1); back >= 0 && line - back < 80; back--)
        {
            Match root = Regex.Match(lines[back],
                "<div class=\"(locked-door|satchel|lift-panel|vent-board|treasure-map-card|deck-offer-card)"
                + "[\" ]");
            if (root.Success)
            {
                return "the " + root.Groups[1].Value.Replace('-', ' ');
            }
        }

        return "an unnamed card";
    }

    private static string CardIn(string found) =>
        found[(found.LastIndexOf('(') + 1)..].TrimEnd(')');

    private static int CountOf(string line, string needle)
    {
        int found = 0;
        for (int at = line.IndexOf(needle, StringComparison.Ordinal);
             at >= 0;
             at = line.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            found++;
        }

        return found;
    }

    /// <summary>The element a <c>class="…"</c> belongs to: the nearest <c>&lt;</c> at or before it, looking
    /// back up the file when the attribute sits on its own line (this codebase writes long tags across
    /// several).</summary>
    private static string TagOwning(string[] lines, int line, int column)
    {
        string head = lines[line][..column];
        for (int back = line; back >= 0 && line - back < 8; back--)
        {
            int open = head.LastIndexOf('<');
            if (open >= 0)
            {
                Match named = Regex.Match(head[(open + 1)..], @"^[A-Za-z][\w.]*");
                return named.Success ? named.Value : "?";
            }

            if (head.Contains('>'))
            {
                break;   // the tag before this one closed: the attribute is loose text, not an element's
            }

            head = back > 0 ? lines[back - 1] : "";
        }

        return "?";
    }

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

        Type talkType = typeof(Map).GetNestedType("TableTalk", BindingFlags.NonPublic)
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

    // ── …and the rules that used to reach them ────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY RULE WHOSE TARGET THE SHELL NOW DRAWS IS WRITTEN WITH <c>::deep</c>.
    ///
    /// <para><b>This is #996's bug class, aimed at the migration most likely to commit it.</b> Blazor scopes
    /// a stylesheet to the component that RENDERED the element: <c>.lift-panel { … }</c> in Map.razor.css
    /// compiles to <c>.lift-panel[b-map]</c>, and the moment OverlayShell draws that div the div carries the
    /// SHELL's scope attribute instead. The rule is then present, correct, and dead. Nothing warns, nothing
    /// fails; the card simply loses its border and its width, and only a pair of eyes on the screen would
    /// ever know — which is exactly how #996 came to be holding 213 dead rules.</para>
    ///
    /// <para>So this reads the two files against each other. Every class the page writes onto an
    /// <c>&lt;OverlayShell&gt;</c> is a class the shell draws; every rule in the page's stylesheet whose
    /// TARGET — its last compound selector, the one Blazor pins the scope attribute to — names one of them
    /// must be written <c>::deep</c>, which compiles to <c>[b-map] .lift-panel</c> and matches again.</para>
    ///
    /// <para>An ANCESTOR is a different question and deliberately not asked: <c>.treasure-map-card .btn</c>
    /// still matches, because the scope lands on <c>.btn</c> — the page's own markup inside ChildContent —
    /// and the ancestor is matched by class alone. Only the target has to move.</para>
    ///
    /// <para><b>It caught three on the way in.</b> <c>.pressure-door</c>, <c>.scuttle-panel</c> and
    /// <c>.scuttle-epitaph</c> are modifier classes on vent-board roots this wave migrated, and all three
    /// were missed by hand. They carry a max-width and nothing else, so the only symptom would have been
    /// three cards quietly growing wider than they were drawn to be.</para>
    /// </summary>
    [Fact]
    public void EveryRuleWhoseTargetTheShellDrawsIsWrittenWithDeep()
    {
        string razor = Path.Combine(ClientSource(), "Pages", "Map.razor");
        string sheet = Path.Combine(ClientSource(), "Pages", "Map.razor.css");

        IReadOnlyList<IReadOnlySet<string>> drawn = ClassesTheShellDraws(File.ReadAllText(razor));
        Assert.True(drawn.Count > 0,
            "no <OverlayShell> in Map.razor names a class at all. Either the page has stopped using the "
            + "shell or this guard has stopped being able to read it — both are worth knowing.");

        var dead = new List<string>();

        foreach (string selector in Selectors(WithoutComments(File.ReadAllText(sheet))))
        {
            if (selector.StartsWith("::deep", StringComparison.Ordinal))
            {
                continue;
            }

            string target = selector
                .Split([' ', '\t', '>', '+', '~'], StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault() ?? "";

            var wanted = Regex.Matches(target, @"\.([A-Za-z0-9_-]+)")
                .Select(found => found.Groups[1].Value)
                .ToHashSet(StringComparer.Ordinal);

            // EVERY class in the compound, not any of them. `.vent-read-line.alive` names a class the shell
            // draws (`alive`, off the operating log's own ternary) on an element the PAGE draws — together
            // they can only ever match the page's markup, which keeps the scope attribute it always had. It
            // is only a compound that COULD match the shell's own element that has to move.
            if (wanted.Count > 0 && drawn.Any(wanted.IsSubsetOf))
            {
                dead.Add(selector);
            }
        }

        Assert.True(dead.Count == 0,
            $"{dead.Count} rule(s) in Map.razor.css target an element OverlayShell draws and are not written "
            + $"with ::deep:\n  - {string.Join("\n  - ", dead)}\n\nBlazor pins the page's scope attribute to "
            + "the LAST compound selector, and the shell's elements do not carry it — so each of these "
            + "compiles to a selector that matches nothing. The rule is present, correct and dead, which is "
            + "#996's shape and the one this migration is most able to commit. Write it "
            + "`::deep .thing { … }` (that is `[b-map] .thing`, matched through the page-scoped ancestor "
            + "above it) — or, if the element really is the page's own markup inside ChildContent, work out "
            + "which end of the selector moved, because it is not this one.");
    }

    /// <summary>Every class the page writes onto an <c>&lt;OverlayShell&gt;</c>. The class may be a plain
    /// list or a C# expression (the operating log picks its own between two literals), so this takes the
    /// class attribute whole and reads the names out of whichever shape it turns out to be.</summary>
    private static IReadOnlyList<IReadOnlySet<string>> ClassesTheShellDraws(string razor)
    {
        var drawn = new List<IReadOnlySet<string>>();

        for (int at = razor.IndexOf("<OverlayShell", StringComparison.Ordinal);
             at >= 0;
             at = razor.IndexOf("<OverlayShell", at + 1, StringComparison.Ordinal))
        {
            int marker = razor.IndexOf(" class=\"", at, StringComparison.Ordinal);
            if (marker < 0)
            {
                continue;
            }

            int from = marker + " class=\"".Length;
            string value = razor[from] == '@'
                ? razor[from..Balanced(razor, from)]
                : razor[from..razor.IndexOf('"', from)];

            var wearing = new HashSet<string>(StringComparer.Ordinal);
            if (value.StartsWith('@'))
            {
                // An expression: the class names are the string literals inside it, and they are all one
                // card either way — the operating log's ternary picks a warning colour, not a new element.
                foreach (Match literal in Regex.Matches(value, "\"([^\"]*)\""))
                {
                    wearing.UnionWith(literal.Groups[1].Value
                        .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries));
                }
            }
            else
            {
                wearing.UnionWith(value.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
                    .Where(css => !css.StartsWith('@')));
            }

            if (wearing.Count > 0)
            {
                drawn.Add(wearing);
            }
        }

        return drawn;
    }

    /// <summary>The end of a razor <c>@(…)</c> attribute value: the parenthesis that closes the one it opens
    /// with. Quotes inside it are part of the expression, which is precisely why the closing quote cannot be
    /// found by looking for the next one.</summary>
    private static int Balanced(string razor, int from)
    {
        int depth = 0;
        for (int at = from; at < razor.Length; at++)
        {
            depth += razor[at] == '(' ? 1 : 0;
            depth -= razor[at] == ')' ? 1 : 0;
            if (depth == 0 && razor[at] == ')')
            {
                return at + 1;
            }
        }

        return razor.IndexOf('"', from);
    }

    private static string WithoutComments(string css) =>
        Regex.Replace(css, @"/\*.*?\*/", " ", RegexOptions.Singleline);

    /// <summary>Every selector in the sheet, one per comma — what it says it is styling.</summary>
    private static IEnumerable<string> Selectors(string css)
    {
        foreach (Match rule in Regex.Matches(css, @"(?m)^([^{}@]+)\{"))
        {
            foreach (string one in rule.Groups[1].Value.Split(','))
            {
                string trimmed = one.Trim();
                if (trimmed.Length > 0)
                {
                    yield return trimmed;
                }
            }
        }
    }

    // ── The collector's demand: the ByDecision mode, on the surface it was written for ────────────────

    /// <summary>
    /// NO ✕ ON THE DEMAND, AND THAT IS THE POINT OF THE MODE.
    ///
    /// <para>A captain who has been grappled answers the collector. What the shell adds is that the absence
    /// is DECLARED — <c>ByDecision</c> draws no dismiss and audits the shape — rather than being the state a
    /// card is in because nobody wrote one, which is the difference #992 was written to find.</para>
    /// </summary>
    [Fact]
    public async Task TheCollectorsDemandIsAByDecisionShellAndOffersNoWayOutButAnAnswer()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        StageTheDemand(bench, "Demand");

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = TheBustedCard(painted)
            ?? throw new Xunit.Sdk.XunitException("staging the demand drew nothing wearing .busted-card.");

        Assert.True(card.HasClass("overlay-shell-bare"),
            "the BUSTED panel is not the shell's Bare frame. #735 pins `.busted-card > .busted-options` and "
            + "the close row; a wrapper between the card and them unsticks both.");

        Assert.DoesNotContain(card.SelfAndDescendants(), n => n.HasClass("overlay-shell-dismiss"));

        var answers = card.Descendants()
            .Where(n => n.Handlers.ContainsKey("onclick") && !n.Hidden)
            .Select(n => n.Name)
            .Where(spoken => spoken.Length > 0)
            .ToList();

        Assert.True(answers.Count >= 3,
            $"the demand offers {answers.Count} control(s). SUBMIT, BRIBE and RESIST are the panel, and a "
            + "ByDecision surface with fewer answers than it has is a surface with no way out at all.");
    }

    /// <summary>
    /// EVERY ANSWER TURNS THE PAGE, AND EVERY CHAIN ENDS IN A CLOSE.
    ///
    /// <para><b>This is the finding, proved.</b> #992's register had this panel down as
    /// <c>EveryControlCloses</c> — <i>"allowed no ✕ only because every answer it offers is itself a
    /// close"</i> — and, being undriven, nothing had ever pressed one. Not one of the three closes it.
    /// SUBMIT goes to Confiscated, BRIBE to BribedOff, RESIST to a won roll, a lost one or the Bolivia. What
    /// is true is that each is a STAGE — which is what <c>Restages</c> says by name — and that following any
    /// of them lands on a card whose single control really does end it.</para>
    ///
    /// <para>So the assertion is the honest one and it is stronger than the flag it replaces: press an
    /// answer, the world MOVED (the phase is not the phase it was), and pressing on gets out. A chain that
    /// stopped moving would spin here and fail by the ceiling rather than hang.</para>
    /// </summary>
    [Theory]
    [InlineData("SUBMIT")]
    [InlineData("BRIBE")]
    [InlineData("RESIST")]
    public async Task EveryAnswerOnTheDemandAdvancesTheWorldAndItsChainEndsInAClose(string answer)
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        StageTheDemand(bench, "Demand");

        var offered = TheBustedCard(await bench.RenderAsync())!
            .Descendants()
            .Where(n => !n.Hidden && n.Handlers.ContainsKey("onclick") && n.Name.Length > 0)
            .ToList();

        DeskBench.Painted.Node? button = offered
            .FirstOrDefault(n => n.Name.Contains(answer, StringComparison.Ordinal));

        Assert.True(button is not null,
            $"the demand has no control reading \"{answer}\" — it offers "
            + $"[{string.Join(" · ", offered.Select(n => n.Name.Split('\n')[0]))}]. The three answers ARE "
            + "the panel; if one has been renamed, this guard's name for it must move with it.");

        string before = PhaseOf(bench);
        await bench.PressAsync(button!.Handlers["onclick"]);
        await bench.RenderAsync();

        Assert.NotEqual(before, PhaseOf(bench));

        // …and now out. Every stage past the demand offers a control; press the LAST one on the card (the
        // close row is the foot of every one of them) until the panel is gone.
        for (int press = 0; press < 8 && bench.Field("_busted") is not null; press++)
        {
            DeskBench.Painted.Node? card = TheBustedCard(await bench.RenderAsync());
            if (card is null)
            {
                break;
            }

            DeskBench.Painted.Node? on = card.Descendants()
                .LastOrDefault(n => !n.Hidden
                                    && n.Handlers.ContainsKey("onclick")
                                    && n.Name.Length > 0
                                    && !n.Name.Contains("Load a saved voyage", StringComparison.Ordinal));

            Assert.True(on is not null,
                $"the {PhaseOf(bench)} stage has no control on it at all. It is drawn, it is on top, and "
                + "there is no way out of it — the shape the owner's ruling forbids, on the one panel that "
                + "is allowed no ✕.");

            await bench.PressAsync(on!.Handlers["onclick"]);
            await bench.RenderAsync();
        }

        Assert.True(bench.Field("_busted") is null,
            $"pressing \"{answer}\" started a chain that never ends. A ByDecision surface earns its missing "
            + "✕ by every answer being a way out — through however many stages, but out. This one is still "
            + $"on the screen at the {PhaseOf(bench)} stage after eight presses.");
    }

    private static string PhaseOf(DeskBench bench)
    {
        object busted = bench.Field("_busted")!;
        return busted is null
            ? "(gone)"
            : busted.GetType().GetProperty("Phase")!.GetValue(busted)!.ToString()!;
    }

    private static DeskBench.Painted.Node? TheBustedCard(DeskBench.Painted painted) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass("busted-card") && !n.Hidden);

    /// <summary>
    /// PUT A COLLECTOR'S DEMAND ON THE SCREEN.
    ///
    /// <para>The register's stated reason for leaving this row undriven was that <c>_busted</c> is a staged
    /// record built by the combat lane and that <i>"the stage machine, not the gate, is what would have to be
    /// stood up"</i>. Half of that is right and it is the half that matters here: the STAGE MACHINE is
    /// exactly what this file wants to drive, so it is stood up — the record is built with the same fields
    /// <c>ApplyHunterCatch</c> gives it (a callsign, a heat level, a folded seed and a bribe demand rolled by
    /// Core's own <see cref="BustedRule.BribeDemand"/>), and everything past that press is the shipping
    /// handler, the shipping dice and the shipping markup.</para>
    /// </summary>
    internal static void StageTheDemand(DeskBench bench, string phase)
    {
        Type card = typeof(Map).GetNestedType("BustedEncounter", BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException(
                        "Map.BustedEncounter is gone. The BUSTED panel's own state used to be called that; "
                        + "this guard cannot stage a demand without it.");

        object bust = Activator.CreateInstance(card, nonPublic: true)!;
        const ulong seed = 0xB0_57_ED_11UL;
        Set(card, bust, "HunterId", "collector-1");
        Set(card, bust, "HunterCallsign", "VULTURE ACTUAL");
        Set(card, bust, "Heat", 2);
        Set(card, bust, "Seed", seed);
        Set(card, bust, "Bribe", BustedRule.BribeDemand(2, seed));
        Set(card, bust, "Phase", Enum.Parse(card.GetNestedType("Stage")!, phase));

        bench.Poke("_showSaveDrawer", false);
        bench.Poke("_busted", bust);
    }

    private static void Set(Type card, object bust, string name, object value) =>
        card.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.SetValue(bust, value);

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────────────

    private const string FreeFlying = "/map?start=wreck";
    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";
    private const string Ashore = "/map?dock=the-tilt&site=0&land=1";

    private static IEnumerable<string> RazorFiles() =>
        Directory.EnumerateFiles(ClientSource(), "*.razor", SearchOption.AllDirectories);

    private static string ClientSource()
    {
        var at = new DirectoryInfo(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            at = at.Parent;
        }

        throw new DirectoryNotFoundException(
            "src/SpaceSails.Client is not above the test binary — this guard reads the markup as typed and "
            + "cannot do its job without it.");
    }
}
