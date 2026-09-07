using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #615 · <b>A FIND IS A DECISION, NOT AN AUTOMATIC PICKUP.</b> Owner: <i>"should we have like keep / leave
/// option when we find stuff?"</i>
///
/// <para>Searching a room used to transfer whatever was in it on the frame the sentence printed. These pin
/// the four things that must be true of the question now that it is asked: it is offered over everything
/// that costs a compartment, it is <b>still</b> offered when the pocket is full (which is the one case the
/// whole issue is about), it is never offered over the way down, and the room keeps what is declined —
/// through a save, a lift-off and a return.</para>
///
/// <para><b>The world here can tell pass from fail.</b> Two of these would have been green against the OLD
/// behaviour as well — a find that is silently taken is also a find whose room is empty afterwards — so each
/// one is written against the pair it has to separate: a full pocket versus an empty one, a key versus a
/// paper, a room kept from versus a room walked past. Every one of them was watched RED with the law it
/// guards reverted; the reversions are quoted in the PR body.</para>
/// </summary>
public sealed class AFindIsADecisionTests
{
    private const string Body = "luna";
    private const string FindId = "hive:luna:-3:2";

    private static readonly UndergroundComplex.AuthorityCard Card = new(Body, 1);

    /// <summary>A sleeve with no room left in it — twelve sheets, which is the compartment's whole
    /// capacity. Built by asking <see cref="Satchel"/> rather than by counting to twelve here, so a
    /// capacity change moves this fixture with it instead of quietly making it a fixture of eleven.</summary>
    private static IReadOnlyList<Satchel.Item> AFullSleeve()
    {
        IReadOnlyList<Satchel.Item> pocket = [];
        for (int i = 0; Satchel.SpaceLeft(pocket, Satchel.Compartment.Sleeve) > 0; i++)
        {
            pocket = Satchel.Add(pocket, new Satchel.Item(Satchel.Kind.Paper, $"filler-{i}"));
        }

        Assert.True(Satchel.IsFull(pocket, Satchel.Kind.Paper));
        return pocket;
    }

    // ── The question ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every haul that costs the captain a compartment gets asked; the three that cost nothing do
    /// not. Walked over the whole enum rather than over a list written here, so the day somebody adds a
    /// seventh thing a room can hold, this goes red until they have decided which side of the line it is
    /// on.</summary>
    [Fact]
    public void EveryHaulThatCostsACompartmentIsADecisionAndNoOtherOneIs()
    {
        var expected = new Dictionary<UndergroundComplex.Haul, bool>
        {
            [UndergroundComplex.Haul.Nothing] = false,     // nothing to decide about
            [UndergroundComplex.Haul.Equipment] = false,   // carried out and sold; never enters a pocket
            [UndergroundComplex.Haul.Records] = true,
            [UndergroundComplex.Haul.Dirt] = true,
            [UndergroundComplex.Haul.Relic] = true,
            [UndergroundComplex.Haul.Key] = false,         // the way down (#1069), and the wallet never fills
        };

        foreach (UndergroundComplex.Haul haul in Enum.GetValues<UndergroundComplex.Haul>())
        {
            Assert.True(expected.ContainsKey(haul),
                $"{haul} is a haul nobody has decided about — is it a decision or is it not?");

            Satchel.Item? offered = UndergroundComplex.WhatTheRoomHandsOver(haul, Card, FindId);
            Assert.Equal(expected[haul], KeepOrLeave.IsADecision(haul, offered));
        }
    }

    /// <summary>
    /// <b>THE FULL POCKET IS ASKED TOO — and this is the case the issue was filed about.</b>
    ///
    /// <para>The two halves are asserted side by side because they are the pair that can come apart: Core's
    /// pickup refuses the sheet (<c>Take</c> null, the room not emptied) at the exact same moment the
    /// question must still be put. A gate that read capacity would answer "no decision here" for the one
    /// captain whose decision matters, and would do it silently.</para>
    /// </summary>
    [Fact]
    public void AFullSleeveIsStillAskedAlthoughThePickupWouldRefuseIt()
    {
        IReadOnlyList<Satchel.Item> stuffed = AFullSleeve();

        UndergroundComplex.Pickup refused = UndergroundComplex.WhatGoesInThePocket(
            UndergroundComplex.Haul.Records, Body, minted: null, FindId, stuffed);
        Assert.Null(refused.Take);
        Assert.False(refused.RoomEmptied);

        Satchel.Item? offered =
            UndergroundComplex.WhatTheRoomHandsOver(UndergroundComplex.Haul.Records, null, FindId);
        Assert.NotNull(offered);
        Assert.True(KeepOrLeave.IsADecision(UndergroundComplex.Haul.Records, offered),
            "a captain with a full sleeve is the captain the question is worth asking");

        // …and the empty pocket answers the same, which is what makes the assertion above about the GATE
        // rather than about a room that happens to hold paper.
        Assert.True(KeepOrLeave.IsADecision(UndergroundComplex.Haul.Records,
            UndergroundComplex.WhatTheRoomHandsOver(UndergroundComplex.Haul.Records, null, FindId)));
    }

    /// <summary>
    /// <b>THE KEY NEVER ASKS.</b> #1069's ruling: it is the way down and not a paper.
    ///
    /// <para>Asserted against the paper standing beside it, so the test cannot pass by the gate having gone
    /// dead — and with the reason spelled out in the world rather than in the comment: the card goes to the
    /// wallet, and the wallet has no ceiling for a decision to relieve.</para>
    /// </summary>
    [Fact]
    public void TheKeyIsNotADecisionAndThePaperBesideItIs()
    {
        Satchel.Item? key = UndergroundComplex.WhatTheRoomHandsOver(UndergroundComplex.Haul.Key, Card, FindId);
        Satchel.Item? paper =
            UndergroundComplex.WhatTheRoomHandsOver(UndergroundComplex.Haul.Records, Card, FindId);

        Assert.NotNull(key);
        Assert.NotNull(paper);
        Assert.False(KeepOrLeave.IsADecision(UndergroundComplex.Haul.Key, key));
        Assert.True(KeepOrLeave.IsADecision(UndergroundComplex.Haul.Records, paper));

        // The reason, in the world: taking it costs nothing, so there is no pressure a decision could
        // resolve. A wallet that grew a ceiling would make this a different question, and this line is
        // where somebody would find that out.
        Assert.Equal(Satchel.Compartment.Wallet, Satchel.CompartmentOf(key.Value.Kind));
        Assert.False(Satchel.IsFull(AFullSleeve(), key.Value.Kind));
    }

    /// <summary>
    /// <b>THE PICKUP AND THE OFFER READ ONE TABLE.</b>
    ///
    /// <para>Written as a STRUCTURAL guard and not as a value comparison, and that is the whole lesson of it:
    /// the first draft asserted that <c>WhatGoesInThePocket(...).Take</c> equalled
    /// <c>WhatTheRoomHandsOver(...)</c>, watched it stay green against every reversion, and found out why —
    /// the two agree BY CONSTRUCTION today, so the comparison could not tell a repaired world from a broken
    /// one. It is a guard about the construction, then: the pickup owns no table of its own, and a room can
    /// never offer one object and hand over another because there is only one place that answers.</para>
    /// </summary>
    [Fact]
    public void ThePickupOwnsNoTableOfItsOwnAndAsksTheOfferInstead()
    {
        string source = CoreSource("UndergroundComplex.AuthorityCard.cs");
        string pickup = MethodBody(source, "public static Pickup WhatGoesInThePocket(");

        Assert.Contains("WhatTheRoomHandsOver(", pickup, StringComparison.Ordinal);

        // …and it MINTS NOTHING. The pickup keeps a switch of its own, and must: the sentence a find goes in
        // with is its business. What it may not keep is a second table of OBJECTS — the day one of these
        // reappears here is the day the room offers one thing and hands over another.
        Assert.DoesNotContain("new Satchel.Item(", pickup, StringComparison.Ordinal);
        Assert.Contains("new Satchel.Item(",
            MethodBody(source, "public static Satchel.Item? WhatTheRoomHandsOver("), StringComparison.Ordinal);

        // The pickup still says its own PROSE — the half that does belong to it — so this guard is not
        // passing because the method has been emptied.
        Assert.Contains("PocketFullLine", pickup, StringComparison.Ordinal);
    }

    private static string CoreSource(string file)
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
        {
            at = at.Parent;
        }

        Assert.NotNull(at);
        return File.ReadAllText(Path.Combine(at.FullName, "src", "SpaceSails.Core", file));
    }

    /// <summary>The text of one method, brace-counted from its signature — enough for "does THIS method
    /// mention that", which a whole-file search is not.</summary>
    private static string MethodBody(string source, string signature)
    {
        int at = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"could not find `{signature}` — the guard is reading a method that has moved");

        int open = source.IndexOf('{', at);
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

    // ── The words ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>TWO WORDS AND ONE AUTHORED SENTENCE, AND THEY ARE THE ONLY THREE.</b>
    ///
    /// <para>Everything else this feature says out loud is somebody else's sentence, already written and
    /// already shipped — the room's haul line, Core's full-pocket refusal, the sleeve's row name. So the law
    /// is that this type publishes exactly three strings; a fourth one appearing here is prose that nobody
    /// authored, which is the one thing a lane like this may not ship.</para>
    ///
    /// <para>The third arrived the way the law requires and not around it: #615 shipped LEAVE with a
    /// <c>// FABLE: line needed</c> marker and no sentence, Fable authored one on the issue's closing pass,
    /// and it is pinned character for character by
    /// <see cref="TheLeaveLine_IsTheAuthoredSentenceAndClaimsNothingMoved"/>. That pairing is what keeps
    /// this guard from being a counter: the count says nothing new may appear, and the other says what the
    /// one that did appear must be.</para>
    ///
    /// <para>Read by reflection rather than from a list, because a list would have to be edited by the same
    /// hand that added the string.</para>
    /// </summary>
    [Fact]
    public void TheOnlyWordsThisFeatureAddsAreKeepAndLeaveAndTheLeaveLine()
    {
        string[] published = [.. typeof(KeepOrLeave)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .OrderBy(s => s, StringComparer.Ordinal)];

        Assert.Equal(["Keep", "Leave", KeepOrLeave.LeftWhereItLies], published);

        // Plate-idiom nouns: what the hand does, and nothing about what the thing is. No glyph, no
        // sentence, no punctuation — the room's own line is already on the card above them.
        foreach (string word in new[] { KeepOrLeave.KeepLabel, KeepOrLeave.LeaveLabel })
        {
            Assert.DoesNotContain(' ', word);
            Assert.True(word.All(char.IsAsciiLetter), $"\"{word}\" is not one plain word");
        }

        // …and the sentence is a sentence, not a third button: it would be on the card as a label the
        // moment it read like one.
        Assert.Contains(' ', KeepOrLeave.LeftWhereItLies);
        Assert.EndsWith(".", KeepOrLeave.LeftWhereItLies, StringComparison.Ordinal);
    }

    /// <summary>§8's reserved word, and the fifteen beside it. This feature's own strings say nothing about
    /// what any of this is FOR, which is the canon rule the whole ground is under — and here it is nearly
    /// free, because a type that publishes two verbs cannot explain anything.</summary>
    [Fact]
    public void NothingThisFeatureSaysExplainsWhatThePlaceWasFor()
    {
        string[] forbidden =
        [
            "monolith", "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect",
            "clone", "slave", "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
        ];

        foreach (string word in new[] { KeepOrLeave.KeepLabel, KeepOrLeave.LeaveLabel })
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, word, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ── The key the register is written with ──────────────────────────────────────────────────────────

    /// <summary>A room under one moon is not a room under another. The excursion's own set is keyed on floor
    /// and index alone, which is fine for a walk and would be a catastrophe in a save: B3's fourth room
    /// striking off B3's fourth room everywhere in the solar system.</summary>
    [Fact]
    public void TheRegisterKeyNamesTheSiteAsWellAsTheRoom()
    {
        string here = KeepOrLeave.RoomKey("luna", -3, 2);
        string there = KeepOrLeave.RoomKey("phobos", -3, 2);
        Assert.NotEqual(here, there);

        Assert.True(KeepOrLeave.TryReadKey(here, "luna", out int level, out int room));
        Assert.Equal(-3, level);
        Assert.Equal(2, room);

        Assert.False(KeepOrLeave.TryReadKey(here, "phobos", out _, out _));
        Assert.False(KeepOrLeave.TryReadKey(there, "luna", out _, out _));
    }

    /// <summary>The writer and the reader are one pair. Walked over floors and indices rather than asserted
    /// on one, because an off-by-one in the split would survive a single happy case.</summary>
    [Fact]
    public void EveryRoomKeyReadsBackAsTheRoomItWasWrittenFor()
    {
        for (int level = -14; level <= 0; level++)
        {
            for (int room = 0; room < 6; room++)
            {
                string key = KeepOrLeave.RoomKey(Body, level, room);
                Assert.True(KeepOrLeave.TryReadKey(key, Body, out int back, out int index));
                Assert.Equal(level, back);
                Assert.Equal(room, index);
            }
        }
    }

    /// <summary>A key this build cannot read is refused rather than half-read. It costs one room its strike
    /// off; a half-read one would strike off a DIFFERENT room, which is worse than forgetting.</summary>
    [Fact]
    public void ARowThisBuildCannotReadIsRefusedOutright()
    {
        Assert.False(KeepOrLeave.TryReadKey("luna|nonsense|2", Body, out _, out _));
        Assert.False(KeepOrLeave.TryReadKey("luna|-3", Body, out _, out _));
        Assert.False(KeepOrLeave.TryReadKey("luna|-3|2|4", Body, out _, out _));
        Assert.False(KeepOrLeave.TryReadKey("", Body, out _, out _));
    }

    /// <summary>#615 · <b>THE LEAVE LINE IS FABLE'S, VERBATIM.</b> Authored on the issue's closing pass
    /// (2026-09-03) for the one <c>// FABLE: line needed</c> this feature shipped with, so the guard is the
    /// sentence character for character.
    ///
    /// <para>And it checks what the line must NOT be, which is the half that can tell pass from fail. It may
    /// not borrow <see cref="LeftBehind"/>'s wording — <i>where you left it</i> is about a thing that was in
    /// a hand and put down, and a sheet nobody picked up was never left anywhere — because that is the exact
    /// lie <see cref="KeepOrLeave"/>'s own docblock refused to tell and the reason this verb never writes to
    /// that store. It may not name the reserved word (worldbuilding-notes §8), and it may not report that
    /// the find was taken, dropped or lost: nothing moved.</para></summary>
    [Fact]
    public void TheLeaveLine_IsTheAuthoredSentenceAndClaimsNothingMoved()
    {
        Assert.Equal("Left where it lies. The room will still have it.", KeepOrLeave.LeftWhereItLies);

        foreach (string forbidden in new[]
        {
            "monolith", "where you left it", "dropped", "taken", "lost", "gone",
        })
        {
            Assert.DoesNotContain(forbidden, KeepOrLeave.LeftWhereItLies, StringComparison.OrdinalIgnoreCase);
        }
    }
}
