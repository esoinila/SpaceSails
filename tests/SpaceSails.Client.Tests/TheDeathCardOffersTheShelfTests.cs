using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #951 / #948 · THE DEATH CARD OFFERS THE SHELF, AND THE CAPTAIN HAS A NAME.
///
/// <para>Owner, 2026-08-21: <i>"I undocked from station and died. I want an option to load previous game
/// here like in the beginning."</i> His screenshot is the Impact freeze-frame — the periapsis ran under
/// Selene Gate's own surface — and the card carries exactly one button: <b>…wake up</b>. And, at the front
/// door: <i>"Let's have an option to change the name of our avatar."</i></para>
///
/// <h3>Why this file reads the shipping razor</h3>
/// <para>There is no bUnit here, and a Blazor page with three hundred fields is not stood up in a unit test.
/// So these guards do what the house does: they read the SHIPPING markup and the SHIPPING methods, and they
/// insist the two agree — every death panel wires a handler that really exists on <see cref="Map"/> and
/// really opens the drawer, and the drawer is really suppressed while it is open over a death card. A guard
/// that merely grepped for the word "logbook" would be the fifth named bug class in this repo: green for
/// ever on a button wired to nothing.</para>
///
/// <h3>Proven red on purpose</h3>
/// <list type="bullet">
///   <item>Deleting the button from any one of the four death panels fails
///   <c>EVERY_DEATH_PANEL_OffersTheLogbook</c> and names the panel that lost it.</item>
///   <item>Leaving the busted card's render ungated (<c>@if (_busted is { } bust)</c>) fails
///   <c>THE_DEATH_CARD_YieldsWhileTheLogbookIsOpen</c> — and in the browser the drawer opens BEHIND the card
///   that opened it, because the busted backdrop sits in the modal band above the picker's gate.</item>
///   <item>Dropping <c>_busted = null</c> from LoadSlot fails <c>LOADING_A_SAVE_EndsTheDeathItWasOpenedFrom</c>.</item>
///   <item>Removing the pencil from the desk or the roster card fails the two <c>THE_NAME</c> guards.</item>
/// </list>
/// </summary>
public class TheDeathCardOffersTheShelfTests
{
    // ── #951 ──

    /// <summary>The four panels a death can end on: the collector's freeze-frame, the impact, the regolith,
    /// and the clinic wake. Each is a `case BustedEncounter.Stage.X:` arm in Map.razor's busted switch, and
    /// each is somewhere the owner could be sitting when he decides this was not the run he meant to keep.</summary>
    private static readonly string[] DeathStages = ["FreezeFrame", "Impact", "SurfaceEnd", "Resurrected"];

    [Fact]
    public void EVERY_DEATH_PANEL_OffersTheLogbook()
    {
        string razor = Razor("Map.razor");
        string busted = BustedSwitch(razor);

        var missing = new List<string>();
        foreach (string stage in DeathStages)
        {
            string panel = StagePanel(busted, stage);
            if (!panel.Contains("OpenLogbookFromDeath", StringComparison.Ordinal))
            {
                missing.Add(stage);
            }
        }

        Assert.True(
            missing.Count == 0,
            "#951 — the owner died and the card offered him one verb. Every death panel must also offer the "
            + "logbook. These do not: " + string.Join(", ", missing));

        // And the canon beat is NOT displaced by it: the brain-backup wake is still the button beside it.
        // (The captain is revived from a backup in this fiction; #951 adds a door, it does not remove one.)
        foreach (string stage in new[] { "FreezeFrame", "Impact", "SurfaceEnd" })
        {
            Assert.Contains("BustedResurrect", StagePanel(busted, stage), StringComparison.Ordinal);
        }

        Assert.Contains("Board the rustbucket", StagePanel(busted, "Resurrected"), StringComparison.Ordinal);
    }

    [Fact]
    public void THE_LOGBOOK_DOOR_IsARealHandlerThatRealyOpensTheDrawer()
    {
        // The button is wired to a method that exists, and that method opens the SAME drawer the captain's
        // desk opens — one save surface, two doors, which is #310's law and the whole point of reusing it.
        MethodInfo door = Method("OpenLogbookFromDeath");
        string body = MethodBody("OpenLogbookFromDeath");
        Assert.Contains("OpenSaveDrawer", body, StringComparison.Ordinal);
        Assert.NotNull(Method("OpenSaveDrawer"));

        // OpenSaveDrawer is what raises the flag the surface renders on, and it refreshes the shelf first so
        // the death-screen list is not a stale one from before the run that just ended.
        string open = MethodBody("OpenSaveDrawer");
        Assert.Contains("_showSaveDrawer = true", open, StringComparison.Ordinal);
        Assert.Contains("RefreshSlotList", open, StringComparison.Ordinal);
        Assert.Equal("Void", door.ReturnType.Name);
    }

    [Fact]
    public void THE_DEATH_CARD_YieldsWhileTheLogbookIsOpen()
    {
        // A z-order fact, asserted rather than hoped for. .busted-backdrop is calc(var(--z-modal) + 50) and
        // .start-picker-backdrop is calc(var(--z-desks-popups) + 100) — 1410 against 1300 — so an unguarded
        // busted card would draw straight over the drawer it just opened. The card yields instead, and comes
        // back untouched when the drawer closes, so nobody is trapped and the beat is not skipped.
        string razor = Razor("Map.razor");
        Assert.Contains("@if (_busted is { } bust && !_showSaveDrawer)", razor, StringComparison.Ordinal);

        string css = MapStylesheet.Text;
        Assert.True(
            ZIndexOf(css, ".busted-backdrop") > ZIndexOf(css, ".start-picker-backdrop"),
            "the guard above exists BECAUSE the busted card outranks the picker's gate; if that ever stops "
            + "being true the guard is cargo and should be re-reasoned, not deleted.");
    }

    [Fact]
    public void LOADING_A_SAVE_EndsTheDeathItWasOpenedFrom()
    {
        // Boarding a banked moment from the death card must actually get you out of the death: the card
        // belongs to a timeline the captain just walked out of. Without this the save loads UNDER a freeze
        // frame that is still holding, and closing the drawer puts the corpse back on the screen.
        string body = MethodBody("LoadSlot");
        Assert.Contains("_busted = null", body, StringComparison.Ordinal);
    }

    // ── #948 ──

    [Fact]
    public void THE_NAME_IsRenameableAtTheDeskAndOnTheRosterCard()
    {
        // Two doors onto ONE act. The desk is where you sit; the roster card at the front door is where the
        // generated names actually pile up ("forgetting who you are amongst the autogenerated names"), so
        // both carry the pencil and both end in Map's RenameCaptain.
        string desk = Razor(Path.Combine("Stations", "Captain.razor"));
        Assert.Contains("OnBeginRename", desk, StringComparison.Ordinal);
        Assert.Contains("OnCommitRename", desk, StringComparison.Ordinal);
        Assert.Contains("CaptainName", desk, StringComparison.Ordinal);

        string razor = Razor("Map.razor");
        Assert.Contains("BeginRenameCaptain(group.Thread.Id)", razor, StringComparison.Ordinal);
        Assert.Contains("OnBeginRename=\"() => BeginRenameCaptain(", razor, StringComparison.Ordinal);
        Assert.Contains("OnCommitRename=\"CommitRenameCaptain\"", razor, StringComparison.Ordinal);

        // Both doors land on the one Core write, and it is the registry's Rename — not a second copy of the
        // act with its own arithmetic, which is how two doors quietly stop agreeing.
        Assert.Contains("Threads.Rename(", MethodBody("RenameCaptain"), StringComparison.Ordinal);
        Assert.NotNull(typeof(GameThreadRegistry).GetMethod(nameof(GameThreadRegistry.Rename)));
    }

    [Fact]
    public void THE_NAME_ReachesTheSavesAndNotOnlyTheScreen()
    {
        // A rename that only repaints the chip is a lie the next reload exposes. Renaming the LIVE universe
        // requests an autosave, so the rolling save (and every export after it) carries the new name; and
        // BuildVault stamps the name into the payload rather than leaving it on the registry row alone.
        Assert.Contains("RequestVaultSave", MethodBody("RenameCaptain"), StringComparison.Ordinal);

        string build = MethodBody("BuildVault");
        Assert.Contains("Logbook = new LogbookSection", build, StringComparison.Ordinal);
        Assert.Contains("CaptainName = ActiveCaptainName", build, StringComparison.Ordinal);

        // And the label mirrors the payload rather than gathering the strings a second time — one truth.
        string meta = MethodBody("BuildSlotMeta");
        Assert.Contains("vault.Logbook?.CaptainName", meta, StringComparison.Ordinal);
        Assert.Contains("vault.Logbook?.Title", meta, StringComparison.Ordinal);
        Assert.Contains("vault.Logbook?.Note", meta, StringComparison.Ordinal);
    }

    [Fact]
    public void EVERY_BANKING_GESTURE_AsksForThePageFirst()
    {
        // Owner: "I would like to save this point with a comment field and a captain's name of my whim."
        // So no button may bank or export silently any more: every ⤓ bank here and the ⬇ Export this moment
        // go through the sheet. A stray @onclick straight at SaveToSlot/ExportVault would skip it.
        string razor = Razor("Map.razor");
        foreach (Match m in Regex.Matches(razor, @"@onclick=""(?:\(\) => )?(SaveToSlot|ExportVault)\b"))
        {
            Assert.Fail(
                "#948 — this gesture banks without asking for a title and a note: " + m.Value
                + ". Route it through BeginBankToSlot / BeginExportMoment.");
        }

        Assert.Contains("BeginBankToSlot(ctx.Id)", razor, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"BeginExportMoment\"", razor, StringComparison.Ordinal);
        Assert.Contains("BeginEditSlotPage(ctx.ThreadId, ctx.Id)", razor, StringComparison.Ordinal);

        // The three gestures are three arms of ONE commit, so a title typed into the sheet cannot reach the
        // slot by one route and the file by another.
        string commit = MethodBody("CommitBankPrompt");
        Assert.Contains("SaveToSlot(slot, title, note)", commit, StringComparison.Ordinal);
        Assert.Contains("ExportVault(title, note)", commit, StringComparison.Ordinal);
        Assert.Contains(".Retitle(slot, title, note)", commit, StringComparison.Ordinal);
    }

    [Fact]
    public void THE_ROW_ShowsTheTitleTheNoteAndWhoSailedIt()
    {
        // The owner's screenshot: eight rows reading "The Tilt · day 0". The row must lead with the title
        // (Core's one derivation, so the drawer and the front door cannot drift), and still carry the
        // provenance he asked to keep — captain, place, day, real-world stamp, build.
        // The closing anchor is the fragment that follows the row's own — renamed SaveLoadSurface →
        // SaveLoadInside by #997 wave 8, when the logbook took its shell and the two save surfaces stopped
        // sharing a root. Nothing about the ROW moved; only the name of what comes after it.
        string row = Between(Razor("Map.razor"), "RenderFragment<(string ThreadId, string Id, SaveSlotMeta?", "RenderFragment<bool> SaveLoadInside");
        Assert.Contains("SaveSlotLabels.TitleOf(m)", row, StringComparison.Ordinal);
        Assert.Contains("m.CaptainName", row, StringComparison.Ordinal);
        Assert.Contains("m.Where", row, StringComparison.Ordinal);
        Assert.Contains("day @m.SimDay", row, StringComparison.Ordinal);
        Assert.Contains("@m.RealTimeLabel", row, StringComparison.Ordinal);
        Assert.Contains("@m.BuildStamp", row, StringComparison.Ordinal);
        Assert.Contains("noted.Note", row, StringComparison.Ordinal);
        Assert.Contains("ToggleNote(ctx.ThreadId, ctx.Id)", row, StringComparison.Ordinal);
    }

    // ─── the shipping sources, read as they ship ───

    private static string Razor(string relative)
        => MapMarkup.Read(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", relative));

    private static MethodInfo Method(string name)
        => typeof(Map).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
           ?? throw new InvalidOperationException($"Map has no method '{name}' — the razor wires one that does not exist.");

    /// <summary>The source text of one method on <see cref="Map"/>, from its signature to the matching
    /// brace — found across the page's partials rather than in a named one, so moving a method between
    /// Map.Vault.cs and Map.Logbook.cs (as #948 did, to stay under the 1500-line line) cannot quietly turn
    /// a guard green by making it read a file the method is no longer in.</summary>
    private static string MethodBody(string method)
    {
        string dir = Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages");
        string[] partials = Directory.GetFiles(dir, "Map.*.cs", SearchOption.TopDirectoryOnly);
        string src = partials.FirstOrDefault(f => Declares(File.ReadAllText(f), method)) is { } hit
            ? File.ReadAllText(hit)
            : throw new InvalidOperationException($"no Map partial declares '{method}'");
        // The DECLARATION, not a call inside somebody else's expression body. The character class
        // deliberately excludes "=" and "(", because on this file's first run
        // `private Task ContinueFromSave() => LoadSlot(Slots.Newest()?.Id);` matched as LoadSlot's own
        // signature — and since an expression body has no brace, the reader then walked on and handed back
        // the NEXT method's body entirely. A guard reading the wrong method is a guard that cannot fail.
        Match sig = Declaration(src, method);
        int open = src.IndexOf('{', sig.Index + sig.Length);
        Assert.True(open > 0, $"'{method}' has no braced body");

        int depth = 0;
        for (int i = open; i < src.Length; i++)
        {
            if (src[i] == '{') depth++;
            else if (src[i] == '}' && --depth == 0)
            {
                return src[open..(i + 1)];
            }
        }

        throw new InvalidOperationException($"unbalanced braces reading '{method}'");
    }

    private static bool Declares(string src, string method) => Declaration(src, method).Success;

    private static Match Declaration(string src, string method) => Regex.Match(
        src,
        @"^\s*(?:private|internal|public|protected)[\w\s<>,\?\[\]\.]*\s" + Regex.Escape(method) + @"\s*\(",
        RegexOptions.Multiline);

    private static string BustedSwitch(string razor)
        // #251 item 1: the end anchor was the CONVERGENCE comment, and that paragraph has gone to live
        // with the surface it describes (Pages/Map/ConvergenceRevealCard.razor). Anchoring a slice on
        // prose was always the weaker choice; the guard that opens the next surface is the real edge.
        => Between(razor, "@if (_busted is { } bust", "@if (_convergenceRevealOpen)");

    /// <summary>One `case BustedEncounter.Stage.X:` arm, up to its `break;`.</summary>
    private static string StagePanel(string busted, string stage)
    {
        string head = $"case BustedEncounter.Stage.{stage}:";
        int at = busted.IndexOf(head, StringComparison.Ordinal);
        Assert.True(at >= 0, $"Map.razor has no busted panel for stage {stage}");
        int end = busted.IndexOf("break;", at, StringComparison.Ordinal);
        Assert.True(end > at, $"the {stage} panel never breaks");
        return busted[at..end];
    }

    private static string Between(string text, string start, string end)
    {
        int a = text.IndexOf(start, StringComparison.Ordinal);
        Assert.True(a >= 0, $"anchor not found: {start}");
        int b = text.IndexOf(end, a, StringComparison.Ordinal);
        Assert.True(b > a, $"closing anchor not found: {end}");
        return text[a..b];
    }

    /// <summary>The numeric z-index of a CSS rule, resolving the two band variables app.css defines.</summary>
    private static int ZIndexOf(string css, string selector)
    {
        int at = css.IndexOf(selector + " {", StringComparison.Ordinal);
        Assert.True(at >= 0, $"no CSS rule for {selector}");
        int close = css.IndexOf('}', at);
        Match z = Regex.Match(css[at..close], @"z-index:\s*calc\(var\((--z-[a-z-]+)\)\s*\+\s*(\d+)\)");
        Assert.True(z.Success, $"{selector} has no banded z-index");
        int band = z.Groups[1].Value switch
        {
            "--z-modal" => 1360,
            "--z-desks-popups" => 1200,
            "--z-distress-lifeline" => 1340,
            "--z-map-chrome" => 10,
            _ => throw new InvalidOperationException("unknown z band " + z.Groups[1].Value),
        };
        return band + int.Parse(z.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SpaceSails.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary.");
    }
}
