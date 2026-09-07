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
/// #997 · <b>…AND THE RULES THAT USED TO REACH THEM</b> — the stylesheet half of
/// <see cref="TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests"/>.
///
/// <para>What this part owns is the CSS: every rule whose target the shell now draws is written with
/// <c>::deep</c>, because a rule that reaches into a component's own markup from outside it is a rule that
/// stops reaching the day the markup moves — which is exactly what moving the family onto the shell
/// did.</para>
/// </summary>
public sealed partial class TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests
{
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
    ///
    /// <para><b>#997 wave 11 · IT READS TWO PAIRS OF FILES NOW, and that is what crossing a component
    /// boundary cost.</b> The dice tray is a child component with its own <c>.razor</c> and its own scoped
    /// <c>.css</c>, and the scoping rule is not a page rule — <c>.dice-tray-card[b-dicetray]</c> dies for
    /// exactly the same reason <c>.lift-panel[b-map]</c> does the moment the shell draws it. A guard that
    /// only ever read <c>Map.razor.css</c> would have been green over a tray with no border, no ground and
    /// no <c>pointer-events</c> in a backdrop that passes clicks. So the pair is the parameter.</para>
    /// </summary>
    [Theory]
    [InlineData("Pages", "Map.razor", "Map.razor.css")]
    [InlineData("Components", "DiceTray.razor", "DiceTray.razor.css")]
    public void EveryRuleWhoseTargetTheShellDrawsIsWrittenWithDeep(string folder, string page, string css)
    {
        string razor = Path.Combine(ClientSource(), folder, page);
        string sheet = Path.Combine(ClientSource(), folder, css);

        IReadOnlyList<Shell> shells = ClassesTheShellDraws(MapMarkup.Read(razor));
        Assert.True(shells.Count > 0,
            $"no <OverlayShell> in {page} names a class at all. Either the page has stopped using the "
            + "shell or this guard has stopped being able to read it — both are worth knowing.");

        var dead = new List<string>();

        foreach (string selector in Selectors(WithoutComments(MapMarkup.Read(sheet))))
        {
            if (selector.StartsWith("::deep", StringComparison.Ordinal))
            {
                continue;
            }

            string[] compounds = selector.Split([' ', '\t', '>', '+', '~'],
                                                StringSplitOptions.RemoveEmptyEntries);
            string target = compounds.LastOrDefault() ?? "";

            HashSet<string> wanted = ClassesIn(target);

            // EVERY class in the compound, not any of them. `.vent-read-line.alive` names a class the shell
            // draws (`alive`, off the operating log's own ternary) on an element the PAGE draws — together
            // they can only ever match the page's markup, which keeps the scope attribute it always had. It
            // is only a compound that COULD match the shell's own element that has to move.
            if (wanted.Count > 0 && shells.Any(shell => shell.Roots.Any(wanted.IsSubsetOf)))
            {
                dead.Add(selector);
                continue;
            }

            // …and the elements INSIDE a shell are judged with their ancestor, which the roots do not need
            // to be. A root class names one card wherever it is written; the classes on the shell's dismiss
            // are the page's ordinary vocabulary — `btn`, `card-body`, `card-header` — and half the client
            // wears them. So a rule whose target is one of those is dead only if the rule is aimed at THAT
            // shell: either it names no ancestor at all, or an ancestor of it is that shell's own root.
            // Without this, `.map-burn-quick .btn` reads as a dead rule because a convergence card's way out
            // happens to be a `.btn`, and a guard that cries wolf gets loosened rather than obeyed.
            if (wanted.Count > 0 && shells.Any(shell =>
                    shell.Inside.Any(wanted.IsSubsetOf)
                    && (compounds.Length == 1
                        || compounds[..^1].Any(above => ClassesIn(above) is { Count: > 0 } named
                                                       && shell.Roots.Any(named.IsSubsetOf)))))
            {
                dead.Add(selector);
                continue;
            }

            // …AND THE TARGET THAT NAMES NO CLASS AT ALL. #997 wave 5 walked into this one: #735 pins the
            // brief's and the reveal's action rows as `.expedition-brief-card > button:last-child`, and a
            // target of `button:last-child` carries no class for the check above to hold. It is still the
            // shell's own button — under a Bare frame the dismiss IS the card's last direct child — so the
            // rule dies exactly as loudly and this guard could not see it. A classless target hanging off a
            // shell ROOT is asked the question the other way round: everything under a Bare shell is either
            // ChildContent (the page's markup, scope attribute intact) or the shell's own dismiss, and only
            // the second of those can be reached without naming a class the page wrote.
            if (wanted.Count == 0 && compounds.Length >= 2
                && ClassesIn(compounds[^2]) is { Count: > 0 } parent
                && shells.Any(shell => shell.Roots.Any(parent.IsSubsetOf)))
            {
                dead.Add(selector);
            }
        }

        Assert.True(dead.Count == 0,
            $"{dead.Count} rule(s) in {css} target an element OverlayShell draws and are not written "
            + $"with ::deep:\n  - {string.Join("\n  - ", dead)}\n\nBlazor pins the page's scope attribute to "
            + "the LAST compound selector, and the shell's elements do not carry it — so each of these "
            + "compiles to a selector that matches nothing. The rule is present, correct and dead, which is "
            + "#996's shape and the one this migration is most able to commit. Write it "
            + "`::deep .thing { … }` (that is `[b-scope] .thing`, matched through the scoped ancestor "
            + "above it) — or, if the element really is the page's own markup inside ChildContent, work out "
            + "which end of the selector moved, because it is not this one.");
    }

    /// <summary>One <c>&lt;OverlayShell&gt;</c> as the markup names it: the class lists its ROOT can wear
    /// (open, tucked, stacked) and the class lists it writes onto the elements INSIDE it.</summary>
    private sealed record Shell(
        IReadOnlyList<IReadOnlySet<string>> Roots,
        IReadOnlyList<IReadOnlySet<string>> Inside);

    /// <summary>The class names in one compound selector.</summary>
    private static HashSet<string> ClassesIn(string compound) =>
        Regex.Matches(compound, @"\.([A-Za-z0-9_-]+)")
            .Select(found => found.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Every class the page writes onto an element an <c>&lt;OverlayShell&gt;</c> draws. The value may be a
    /// plain list or a C# expression (the operating log picks its own between two literals; the deflection
    /// storyboard interpolates its button's colour), so this takes each attribute whole and reads the names
    /// out of whichever shape it turns out to be.
    ///
    /// <para><b>The root is not the only element the shell draws, and #997 wave 5 is where that started to
    /// matter.</b> <c>class="…"</c> names the card; <c>DismissClass</c> names the button the shell puts
    /// inside it, and by wave 5 that button is wearing <c>convergence-close</c>, <c>btn</c> and the rest of
    /// the page's own vocabulary — every one of them a name the page's stylesheet has rules about. A reader
    /// that saw only the root would have gone on passing while
    /// <c>.convergence-card &gt; .convergence-close</c> quietly stopped reaching anything, which is this
    /// guard failing in precisely the way it exists to catch. So every <c>…Class</c> attribute on the tag is
    /// read, and each one is a separate element with its own structural class beside it.</para>
    /// </summary>
    private static IReadOnlyList<Shell> ClassesTheShellDraws(string razor)
    {
        // The structural class the shell writes alongside each of the page's named ones, so a rule reaching
        // for `.overlay-shell-dismiss` is judged by the same question as one reaching for `.btn`.
        var alongside = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["class"] = "overlay-shell",
            ["TileClass"] = "overlay-shell",
            ["StackedClass"] = "overlay-shell",
            ["DismissClass"] = "overlay-shell-dismiss",
            ["CloseClass"] = "overlay-shell-close",
            // #997 wave 7 · the second way out of a Bare foot, and the row the page names for the shell to
            // draw them into. Both are elements the shell emits wearing the page's own vocabulary
            // (`busted-logbook`, `deck-shuttle-actions`), so both are read here for the same reason wave 5
            // had to start reading DismissClass: a reader that saw only the root would go on passing while
            // `.busted-logbook` and `.deck-shuttle-actions` quietly stopped reaching anything.
            ["BesideClass"] = "overlay-shell-beside",
            ["WaysClass"] = "overlay-shell-ways",
            ["TileButtonClass"] = "overlay-shell-tile-btn",
            ["HeadClass"] = "overlay-shell-head",
            ["TitleClass"] = "overlay-shell-title",
            ["ToolsClass"] = "overlay-shell-tools",
            ["BodyClass"] = "overlay-shell-body",
            ["ChoicesClass"] = "overlay-shell-choices",
            ["ChoiceClass"] = "overlay-shell-choice",
        };

        var shells = new List<Shell>();

        for (int at = razor.IndexOf("<OverlayShell", StringComparison.Ordinal);
             at >= 0;
             at = razor.IndexOf("<OverlayShell", at + 1, StringComparison.Ordinal))
        {
            string tag = TheTagAt(razor, at);
            var roots = new List<IReadOnlySet<string>>();
            var inside = new List<IReadOnlySet<string>>();

            foreach (Match attribute in Regex.Matches(tag, @"(?<![\w-])([A-Za-z]+)="""))
            {
                string name = attribute.Groups[1].Value;
                if (!alongside.TryGetValue(name, out string? structural))
                {
                    continue;
                }

                int from = attribute.Index + attribute.Length;
                string value = from < tag.Length && tag[from] == '@'
                    ? tag[from..Balanced(tag, from)]
                    : tag[from..Math.Max(from, tag.IndexOf('"', from))];

                var wearing = new HashSet<string>(StringComparer.Ordinal) { structural };
                if (value.StartsWith('@'))
                {
                    // An expression: the class names are the string literals inside it, and they are all
                    // one element either way — the operating log's ternary picks a warning colour and the
                    // storyboard's picks a button colour, not a new element.
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

                if (wearing.Count <= 1)
                {
                    continue;   // the structural class alone: the page named nothing here
                }

                if (name is "class" or "TileClass" or "StackedClass")
                {
                    roots.Add(wearing);
                }
                else
                {
                    inside.Add(wearing);
                }
            }

            if (roots.Count > 0 || inside.Count > 0)
            {
                shells.Add(new Shell(roots, inside));
            }
        }

        return shells;
    }

    /// <summary>The text of the <c>&lt;OverlayShell …&gt;</c> tag beginning at <paramref name="at"/>: up to
    /// the first <c>&gt;</c> that is not inside a quoted value, so a tag's attributes can never be read off
    /// the tag after it. (The old reader searched the whole file forward from each shell for
    /// <c>class="</c>, which was harmless only while every shell had one.)</summary>
    private static string TheTagAt(string razor, int at)
    {
        bool quoted = false;
        for (int scan = at; scan < razor.Length; scan++)
        {
            quoted ^= razor[scan] == '"';
            if (!quoted && razor[scan] == '>')
            {
                return razor[at..scan];
            }
        }

        return razor[at..];
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
}
