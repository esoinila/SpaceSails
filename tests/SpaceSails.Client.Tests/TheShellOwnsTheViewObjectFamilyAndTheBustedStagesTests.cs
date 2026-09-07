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
[SlowGate] // #251 · 50 s over 23 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed partial class TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests
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
}
