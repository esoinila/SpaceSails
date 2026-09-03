using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #615 · <b>LEAVE MUST NOT DESTROY, AND KEEP MUST NOT COME BACK.</b> Owner: <i>"LEAVE must not destroy it.
/// The room remembers (#573: a place you have been stays a place you have been), so a captain can come back
/// for the paper they walked past when their pockets are empty again."</i>
///
/// <para>These run on the deck a captain's boots actually collide with — <see cref="HiveInterior.FloorDeck"/>
/// as the excursion builds it — and on the vault as the page actually writes it, because what is left to get
/// wrong is the WIRING between three registers: the room's own haul (pure, seeded, in Core), the excursion's
/// live emptied set (per floor, per walk, read by the deck builder) and the durable register that rides the
/// save. A find that survives in one of the three and not the others is a promise the game cannot keep.</para>
///
/// <h3>Why the round trip, and why it is paired</h3>
/// <para>Before this lane the durable register did not exist: a facility re-filled itself the moment the
/// shuttle lifted, so <i>"is the paper still there when I come back?"</i> was true of a room the captain had
/// EMPTIED as well as one they had walked past. A guard asking only that question would have been green
/// against a world that could not tell the two apart — this repository's known bug class, wearing a test's
/// clothes. So every assertion below is written as a PAIR: the room walked past against the room kept from,
/// on the same floor, through the same save.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRoomKeepsWhatYouWalkedPastTests
{
    private const string Body = "luna";

    /// <summary>The floor and the field the excursion itself uses. Nothing here invents a world.</summary>
    private static DeckPlan Floor(int level, IReadOnlyCollection<int> emptied) =>
        HiveInterior.FloorDeck(Body, level, MoonSurface.ExpeditionField(), 0, (_, _) => { }, emptied);

    private static int SearchConsoles(DeckPlan deck) =>
        deck.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.HiveHaul);

    private static bool ThereIsAConsoleOn(DeckPlan deck, int level, int roomIndex)
    {
        UndergroundComplex.FloorPlan plan =
            UndergroundComplex.Build(Body, level, MoonSurface.ExpeditionField());
        (double x, double y) = plan.RoomCentres[roomIndex];
        return deck.Consoles.Any(c => c.Kind == DeckPlan.ConsoleKind.HiveHaul
            && Math.Abs(c.X - x) < 0.5 && Math.Abs(c.Y - y) < 0.5);
    }

    /// <summary>The first room on this site that holds a find worth deciding about — searched for rather
    /// than written down, because a hard-coded floor and index is a fixture that stops being a case the day
    /// the haul table moves, and this whole file would then be asserting nothing.</summary>
    private static (int Level, int Room) ARoomWorthDecidingAbout()
    {
        for (int level = -1; level >= -14; level--)
        {
            UndergroundComplex.FloorPlan plan =
                UndergroundComplex.Build(Body, level, MoonSurface.ExpeditionField());
            for (int room = 0; room < plan.RoomCentres.Count; room++)
            {
                UndergroundComplex.Haul haul = UndergroundComplex.InRoom(Body, level, room);
                Satchel.Item? offered = UndergroundComplex.WhatTheRoomHandsOver(
                    haul, UndergroundComplex.CardInRoom(Body, level), UndergroundComplex.FindId(Body, level, room));
                if (KeepOrLeave.IsADecision(haul, offered))
                {
                    return (level, room);
                }
            }
        }

        throw new InvalidOperationException($"no room under {Body} holds a find that is a decision");
    }

    /// <summary>The seeding the page does when the shuttle lands, run through Core's own reader — the SAME
    /// call <c>Map.SeedTurnedOverRooms</c> makes, so this is the wiring and not a copy of it.</summary>
    private static HashSet<int> SeedFrom(IEnumerable<string> register, string bodyId)
    {
        var live = new HashSet<int>();
        foreach (string key in register)
        {
            if (KeepOrLeave.TryReadKey(key, bodyId, out int level, out int room))
            {
                live.Add(HiveInterior.RoomKey(level, room));
            }
        }

        return live;
    }

    // ── The pair ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE WHOLE LOOP, BOTH ANSWERS, ONE SAVE.
    ///
    /// <para>Two rooms on one floor. The captain KEEPS from one and walks past the other; the register is
    /// written, saved, loaded and seeded exactly the way the page does it; and the floor is rebuilt. The room
    /// walked past still has its console and still holds the same find. The room kept from has neither.</para>
    ///
    /// <para>The two assertions are in one test on purpose: apart, either of them is satisfiable by a world
    /// that has forgotten everything, and together they are not.</para>
    /// </summary>
    [Fact]
    public void TheRoomYouWalkedPastStillHoldsItAndTheRoomYouKeptFromDoesNot()
    {
        (int level, int walkedPast) = ARoomWorthDecidingAbout();
        UndergroundComplex.FloorPlan plan =
            UndergroundComplex.Build(Body, level, MoonSurface.ExpeditionField());
        int keptFrom = Enumerable.Range(0, plan.RoomCentres.Count).First(i => i != walkedPast);

        // KEEP writes the room down. LEAVE writes nothing at all — that IS the implementation.
        var register = new HashSet<string> { KeepOrLeave.RoomKey(Body, level, keptFrom) };

        // …and the shuttle lifts. The register goes through the file the page actually writes.
        string json = VaultSerializer.Save(new Vault
        {
            TurnedOver = new TurnedOverSection { Rooms = [.. register] },
        });
        Vault back = VaultSerializer.Load(json);
        Assert.False(back.Tampered);

        DeckPlan floorOnReturn = Floor(level, SeedFrom(back.TurnedOver!.Rooms, Body));

        Assert.True(ThereIsAConsoleOn(floorOnReturn, level, walkedPast),
            "the paper the captain walked past has to still be there to walk back to");
        Assert.False(ThereIsAConsoleOn(floorOnReturn, level, keptFrom),
            "a room already emptied handing out its file a second time is a facility that farms");

        // And what it still holds is the same find, not a re-roll that happened to be paper again: the
        // room's haul is a pure function of the world, so the object offered on the return is the object
        // that was offered before the shuttle lifted.
        UndergroundComplex.Haul haul = UndergroundComplex.InRoom(Body, level, walkedPast);
        Satchel.Item? still = UndergroundComplex.WhatTheRoomHandsOver(
            haul, UndergroundComplex.CardInRoom(Body, level),
            UndergroundComplex.FindId(Body, level, walkedPast));
        Assert.NotNull(still);
        Assert.True(KeepOrLeave.IsADecision(haul, still),
            "the room walked past has to offer the same two verbs it offered the first time");
    }

    /// <summary>An empty register leaves the building exactly as it has always been drawn — every room
    /// offering its search. The base case the pair above is measured against, so a floor that had stopped
    /// drawing consoles altogether could not make either half of it green.</summary>
    [Fact]
    public void AFacilityNobodyHasWalkedDrawsEverySearchItAlwaysDid()
    {
        (int level, _) = ARoomWorthDecidingAbout();
        UndergroundComplex.FloorPlan plan =
            UndergroundComplex.Build(Body, level, MoonSurface.ExpeditionField());

        Assert.Equal(plan.RoomCentres.Count, SearchConsoles(Floor(level, SeedFrom([], Body))));
        Assert.True(plan.RoomCentres.Count > 1, "this floor has to have two rooms for the pair test to pair");
    }

    /// <summary>A room under one moon never strikes off the room with the same index under another. The
    /// excursion's own key is floor-and-index alone, which is right for a walk and would be a catastrophe in
    /// a save — so this is the one property the durable key exists for, asserted on the deck rather than on
    /// the string.</summary>
    [Fact]
    public void ARoomEmptiedUnderOneMoonIsStillFullUnderAnother()
    {
        (int level, int room) = ARoomWorthDecidingAbout();
        var register = new HashSet<string> { KeepOrLeave.RoomKey("phobos", level, room) };

        DeckPlan luna = Floor(level, SeedFrom(register, Body));
        Assert.True(ThereIsAConsoleOn(luna, level, room),
            "a room emptied under Phobos struck off a room under Luna");
    }

    /// <summary>The register survives a save written by a build that has never heard of it, and a row this
    /// build cannot read costs one room its strike-off and nothing more. Tolerance both directions is the
    /// vault's forever-promise and this section is not allowed to be the exception.</summary>
    [Fact]
    public void AnOlderSaveWakesWithAnEmptyRegisterAndAnUnreadableRowIsSurvived()
    {
        (int level, int room) = ARoomWorthDecidingAbout();

        Vault old = VaultSerializer.Load(VaultSerializer.Save(new Vault()));
        Assert.Null(old.TurnedOver);
        Assert.True(ThereIsAConsoleOn(Floor(level, SeedFrom(old.TurnedOver?.Rooms ?? [], Body)), level, room));

        var mixed = new[] { "who-even-knows", KeepOrLeave.RoomKey(Body, level, room) };
        Assert.False(ThereIsAConsoleOn(Floor(level, SeedFrom(mixed, Body)), level, room));
    }

    // ── The wiring ────────────────────────────────────────────────────────────────────────────────────

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Pages(string glob) => string.Concat(
        Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), glob)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>
    /// <b>THE DECISION IS ASKED BEFORE THE POCKET IS.</b>
    ///
    /// <para>The one ordering that makes this feature true, and the one a later edit can silently invert:
    /// the search verb must reach the offer before it reaches the pickup. If <c>WhatGoesInThePocket</c> ran
    /// first the sheet would be in the sleeve while the card was still asking, which is the automatic pickup
    /// with a dialog painted over it.</para>
    /// </summary>
    [Fact]
    public void TheSearchVerbOffersTheDecisionBeforeItAsksThePocket()
    {
        string hive = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Surface.Hive.cs"));

        int offer = hive.IndexOf("OfferKeepOrLeave(", StringComparison.Ordinal);
        int pocket = hive.IndexOf("WhatGoesInThePocket(", StringComparison.Ordinal);
        int add = hive.IndexOf("Core.Satchel.Add(", StringComparison.Ordinal);

        Assert.True(offer > 0, "the search verb no longer offers the decision at all");
        Assert.True(pocket > offer,
            "the pocket is asked before the captain is — the find is taken while the card is still asking");
        Assert.True(add < 0,
            "the search verb adds to the satchel directly; the one add belongs behind the decision");
    }

    /// <summary>
    /// <b>A FULL SLEEVE OPENS THE SLEEVE.</b> Never a silent drop and never a silent refusal — #678's
    /// founding sin in both directions. The KEEP branch, on a refusal, has to land on the compartments with
    /// Core's own words on them, and it must not clear the pending find: the captain is making room, not
    /// giving up.
    /// </summary>
    [Fact]
    public void KeepingWithAFullSleeveOpensThePocketsAndKeepsTheFindWaiting()
    {
        string keep = MethodBody(Pages("Map.Surface.KeepOrLeave.cs"), "private void KeepTheFind()");

        Assert.Contains("OpenSatchel()", keep, StringComparison.Ordinal);
        Assert.Contains("PocketFullLine", keep, StringComparison.Ordinal);

        // The refusal RETURNS before the card comes down, so pressing Keep again after making room finishes
        // the same find. Closing the card is what clears the pending one (#768's one door, and #615's own
        // rule that closing is Leave), so the marker to order against is that close.
        int refusal = keep.IndexOf("OpenSatchel()", StringComparison.Ordinal);
        int closes = keep.IndexOf("CloseViewObject()", StringComparison.Ordinal);
        Assert.True(closes > refusal,
            "a refused Keep takes the card down — the captain makes room and the find is already gone");
        Assert.Contains("return;", keep[refusal..closes], StringComparison.Ordinal);

        // …and it never clears the field by hand, which would both dodge #768's release and make the
        // ordering above meaningless.
        Assert.DoesNotContain("_pendingFind = null", keep, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>LEAVING TOUCHES NOTHING.</b> The verb's whole implementation is putting the card down. A Leave that
    /// had learned to write to the satchel, to the ground store or to the turned-over register would be a
    /// leave that destroys, one edit at a time.
    /// </summary>
    [Fact]
    public void LeavingWritesToNothingAtAll()
    {
        string leave = MethodBody(Pages("Map.Surface.KeepOrLeave.cs"), "private void LeaveTheFind()");

        foreach (string forbidden in new[]
        {
            "Satchel.Add", "Satchel.Remove", "TheRoomHasBeenGoneThrough", "Ground.Leave",
            "HiveRoomsEmptied", "_roomsTurnedOver",
        })
        {
            Assert.DoesNotContain(forbidden, leave, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// <b>…BUT IT SAYS SO, ONCE, AND ONLY ON THE PULSE.</b> #615 shipped this verb with a
    /// <c>// FABLE: line needed</c> and no sentence at all, which made LEAVE the one control on this ground
    /// that answers a press with silence — indistinguishable, from the captain's side, from a swallowed
    /// keypress.
    ///
    /// <para>Three separable things, and each is a different way to get it wrong. It says the AUTHORED line
    /// and never a literal of its own. It says it EXACTLY ONCE, so a decline is one sentence rather than a
    /// pair racing for the pulse's one slot. And it says it on the pulse and files NOTHING: the casebook
    /// holds what the captain now knows, and a book of the papers they declined is a book of things that did
    /// not happen. The say is ordered after the close, because the last write to the one slot wins and a
    /// card coming down is entitled to write there.</para>
    /// </summary>
    [Fact]
    public void LeavingSaysTheAuthoredLineOnceAndFilesNothing()
    {
        string leave = MethodBody(Pages("Map.Surface.KeepOrLeave.cs"), "private void LeaveTheFind()");

        Assert.Equal(1, Occurrences(leave, "ShowPulseMessage(KeepOrLeave.LeftWhereItLies)"));
        Assert.Equal(1, Occurrences(leave, "ShowPulseMessage("));

        foreach (string filed in new[] { "ShowAndFile", "FileNote", "\"" })
        {
            Assert.DoesNotContain(filed, leave, StringComparison.Ordinal);
        }

        int closes = leave.IndexOf("CloseViewObject()", StringComparison.Ordinal);
        int says = leave.IndexOf("ShowPulseMessage(", StringComparison.Ordinal);
        Assert.True(closes >= 0 && says > closes,
            "the line is said after the card comes down — the pulse keeps one slot and the last write wins");
    }

    /// <summary>The card carries both verbs, wired to the two methods, and it carries no third word of its
    /// own. Read off the razor, because a control that exists in C# and not in the markup is a decision the
    /// captain is never offered.</summary>
    [Fact]
    public void TheCardRendersBothVerbsAndNothingElse()
    {
        string razor = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor"));

        // THE GATE ITSELF, and not merely the two controls behind it. The first version of this guard read
        // only the labels and the handlers, and stayed green against a card whose region had been switched
        // off at the `@if` — the controls were in the file and unreachable on the screen, which is a
        // decision the captain is never offered wearing a passing test.
        Assert.Contains("@if (TheFindIsWaitingOnAnAnswer)", razor, StringComparison.Ordinal);
        Assert.Contains("KeepOrLeave.KeepLabel", razor, StringComparison.Ordinal);
        Assert.Contains("KeepOrLeave.LeaveLabel", razor, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"KeepTheFind\"", razor, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"LeaveTheFind\"", razor, StringComparison.Ordinal);

        // The literals themselves never appear in the markup: the words are Core's, said once.
        Assert.DoesNotContain(">Keep<", razor, StringComparison.Ordinal);
        Assert.DoesNotContain(">Leave<", razor, StringComparison.Ordinal);
    }

    /// <summary>Closing the card answers the question. Every road out of a <c>ViewObject</c> goes through
    /// <c>CloseViewObject</c>, so that is where the pending find has to be dropped — which is also what lets
    /// the two-verb card obey the general closing law with no special case in it.</summary>
    [Fact]
    public void ClosingTheCardIsTheSameAnswerAsLeaving()
    {
        string fixtures = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Deck.Fixtures.cs"));

        Assert.Contains("_pendingFind = null",
            MethodBody(fixtures, "private void CloseViewObject()"), StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>ONE METHOD STRIKES A ROOM OFF, AND IT WRITES BOTH REGISTERS.</b>
    ///
    /// <para>The live set the deck reads and the durable set the save carries have to move together, or a
    /// captain gets one answer while walking and another after a lift-off. There are exactly two writes to
    /// the live set in the whole client: the seeding, which restores it FROM the durable one, and the
    /// strike-off, which writes THROUGH to it. Any third one is a room emptied behind the register's back.
    /// </para>
    /// </summary>
    [Fact]
    public void OneMethodStrikesARoomOffAndItWritesBothRegisters()
    {
        string surface = Pages("Map.Surface*.cs");

        Assert.Equal(2, Occurrences(surface, "HiveRoomsEmptied.Add("));
        Assert.Equal(1, Occurrences(
            MethodBody(surface, "private void SeedTurnedOverRooms("), "HiveRoomsEmptied.Add("));

        string strikeOff = MethodBody(surface, "private void TheRoomHasBeenGoneThrough(");
        Assert.Equal(1, Occurrences(strikeOff, "HiveRoomsEmptied.Add("));
        Assert.Contains("_roomsTurnedOver.Add(", strikeOff, StringComparison.Ordinal);

        // …and the durable register has exactly two writers, the same shape as the live set above: the
        // strike-off, and the LOAD that fills it from the vault. A third would be a room recorded as
        // emptied somewhere the deck is still drawing a console on.
        Assert.Equal(2, Occurrences(surface, "_roomsTurnedOver.Add("));
        Assert.Equal(1, Occurrences(
            MethodBody(surface, "private void RestoreTheRoomsGoneThrough("), "_roomsTurnedOver.Add("));
    }

    private static int Occurrences(string text, string needle)
    {
        int n = 0;
        for (int at = text.IndexOf(needle, StringComparison.Ordinal); at >= 0;
             at = text.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            n++;
        }

        return n;
    }

    /// <summary>The text of one method, brace-counted from its signature. Crude and adequate: what these
    /// guards need is "does THIS method mention that", and a whole-file search would let a mention three
    /// methods away keep a broken one green.</summary>
    private static string MethodBody(string source, string signature)
    {
        int at = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"could not find `{signature}` — the guard is reading a method that has moved");

        int open = source.IndexOf('{', at);
        Assert.True(open > 0, $"`{signature}` has no body");

        int depth = 0;
        for (int i = open; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}' && --depth == 0)
            {
                return source[open..(i + 1)];
            }
        }

        throw new InvalidOperationException($"`{signature}` never closes");
    }
}
