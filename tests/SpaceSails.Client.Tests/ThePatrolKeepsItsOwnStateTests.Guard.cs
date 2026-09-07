using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 6′d · <b>THE GUARD IS A TYPE, AND HIS STATE IS HIS</b> — section (9) of
/// <see cref="ThePatrolKeepsItsOwnStateTests"/>, and the source walk the whole class reads with.
///
/// <para>What this part owns is the guard's own twenty-eight: every one of them on the type, with the setter
/// it should have, and none of them assigned anywhere outside <c>Guard.cs</c> — which is the same law as the
/// round's, one floor down.</para>
/// </summary>
public sealed partial class ThePatrolKeepsItsOwnStateTests
{
    // ── (9) #870 lane 6′d · THE GUARD IS A TYPE, AND HIS STATE IS HIS ─────────────────────────

    /// <summary>
    /// EVERY ONE OF THE GUARD'S TWENTY-EIGHT IS STILL THERE, UNDER THE NAME IT HAD, WITH THE SETTER IT IS
    /// SUPPOSED TO HAVE.
    ///
    /// <para>6′d turned twenty-three public mutable fields into <c>{ get; private set; }</c> properties written
    /// only by named transitions. Three harnesses read those names <b>by string</b> and would stay green while
    /// saying nothing if one were renamed or dropped, so the list lives once, in
    /// <see cref="PatrolState.TheGuardsTwentyEight"/>, and this asks the live type for every row.</para>
    ///
    /// <para><b>ANTI-VACUOUS BOTH WAYS.</b> Every name on the list must be a member; every instance member on
    /// the type must be on the list. A twenty-ninth piece of state added without a row here would be a field
    /// #906's transcript writes down and nothing names — which is how a rename gets past three snapshots.</para>
    ///
    /// <para><b>Proven RED</b> three ways in #870 lane 6′d's PR body: a row renamed, a row dropped, and a
    /// non-motion property given back its public setter.</para>
    /// </summary>
    [Fact]
    public void EveryOneOfTheGuardsTwentyEightIsOnTheTypeWithTheSetterItShouldHave()
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags Either = Hidden | BindingFlags.Public;

        Type guard = typeof(Pages.Map).GetNestedType("Guard", Hidden | BindingFlags.Public)
            ?? throw new InvalidOperationException(
                "#870 lane 6′d · Map has no nested `Guard` type at all. Every fact in this file about the "
                + "man on the round is now vacuous.");

        var wrong = new List<string>();
        var named = new HashSet<string>(StringComparer.Ordinal);

        foreach ((string name, bool settable) in PatrolState.TheGuardsTwentyEight)
        {
            named.Add(name);
            FieldInfo? asField = guard.GetField(name, Either);
            PropertyInfo? asProperty = guard.GetProperty(name, Either);

            if (asField is null && asProperty is null)
            {
                wrong.Add($"  `{name}` is not on the Guard at all — three snapshot harnesses read it by string.");
                continue;
            }

            // An `init` accessor IS a public setter as far as reflection is concerned - what marks it is a
            // modreq of IsExternalInit on the set method. DeckName and Plate are minted with the man and
            // never written again, which is neither "open" nor a transition's business.
            bool open = asField is not null
                        || (asProperty!.SetMethod is { IsPublic: true } set
                            && !set.ReturnParameter.GetRequiredCustomModifiers()
                                    .Any(t => t.FullName == "System.Runtime.CompilerServices.IsExternalInit"));
            if (settable && !open)
            {
                wrong.Add($"  `{name}` is MOTION and the stepper has to be able to write it every frame, "
                          + "and it has no setter a caller can reach.");
            }
            if (!settable && open)
            {
                wrong.Add($"  `{name}` is STATE and is open for anybody to write. It belongs to a named "
                          + "transition on the Guard; give it `{ get; private set; }` and a verb.");
            }
        }

        foreach (MemberInfo m in guard.GetFields(Either).Cast<MemberInfo>().Concat(guard.GetProperties(Either)))
        {
            if (m.Name.Contains('<', StringComparison.Ordinal) || named.Contains(m.Name))
            {
                continue;
            }
            wrong.Add($"  `{m.Name}` is state on the Guard that this census has never heard of. #906 writes "
                      + "it into every pinned transcript; add a row for it rather than leaving it unnamed.");
        }

        Assert.True(wrong.Count == 0,
            $"#870 lane 6′d · the Guard and the census of him disagree, {wrong.Count} way(s):\n"
            + string.Join("\n", wrong)
            + "\n\nThe list is PatrolState.TheGuardsTwentyEight. Re-spell it in the same commit as the "
            + "rename; do not delete a row to make this quiet.");
    }

    /// <summary>
    /// AND NOT ONE OF THEM IS WRITTEN ANYWHERE BUT ON THE MAN.
    ///
    /// <para>On the base of this lane there were <b>100 lines</b> across the round's seven verb files that
    /// assigned a guard's state directly — a scatter of <c>g.Field = …</c> in whichever verb noticed, which is
    /// exactly how <c>Held</c> and <c>AfterYou</c> came to be able to contradict each other. There are none.
    /// The five MOTION fields are deliberately exempt: the stepper writes position and velocity every frame
    /// and that is not state, it is where he is.</para>
    ///
    /// <para>The compiler enforces this too (the twenty-three are <c>private set</c>), which is the point of
    /// doing it that way round — this sweep is what makes the rule <i>readable</i>, and what catches the day
    /// somebody adds a setter back rather than adding a verb. The two are checked together on purpose: a
    /// keyword picked to keep a guard from noticing is the fifth bug class in a tidy hat.</para>
    /// </summary>
    [Fact]
    public void NoGuardStateIsAssignedOutsideGuardCs()
    {
        string[] motion = ["X", "Y", "Facing", "Vx", "Vy"];
        string[] state =
        [
            .. PatrolState.TheGuardsTwentyEight
                .Where(row => !row.Settable && !row.Name.Equals("DeckName", StringComparison.Ordinal)
                              && !row.Name.Equals("Plate", StringComparison.Ordinal))
                .Select(row => row.Name),
        ];

        // Longest first: alternation is ORDERED, so `AfterYou` would swallow the head of `AfterYouFor` and
        // this sweep would report a field it had mis-read.
        var write = new Regex(
            @"(?<![\w.])\w+\.(" + string.Join("|", state.OrderByDescending(n => n.Length))
            + @")\s*(\+\+|\+=|-=|\?\?=|=(?!=))",
            RegexOptions.CultureInvariant);
        var bump = new Regex(
            @"\+\+\w+\.(" + string.Join("|", state.OrderByDescending(n => n.Length)) + @")\b",
            RegexOptions.CultureInvariant);

        string family = Path.Combine(ClientRoot, "Pages", "Patrol");
        Assert.True(Directory.Exists(family), $"the round's own directory is not at {family}.");

        var trespass = new List<string>();
        foreach (string path in Directory.EnumerateFiles(family, "*.cs").OrderBy(p => p, StringComparer.Ordinal))
        {
            if (Path.GetFileName(path).Equals("Guard.cs", StringComparison.Ordinal))
            {
                continue;
            }
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                if (write.IsMatch(lines[i]) || bump.IsMatch(lines[i]))
                {
                    trespass.Add($"  {Relative(path)}:{i + 1}  {lines[i].Trim()}");
                }
            }
        }

        Assert.True(trespass.Count == 0,
            $"#870 lane 6′d · {trespass.Count} line(s) write a guard's STATE from outside Guard.cs:\n"
            + string.Join("\n", trespass)
            + "\n\nA guard's state changes through a named transition on the Guard — HeCallsItIn, HeGivesUp, "
            + "HeArrivesAtTheStop, HeForgetsTheCatch and the rest. If none of them says what you mean, ADD "
            + "one, with the whole scatter of assignments it owns inside it, and say in its docblock which "
            + "verb asked for it. The five motion fields (" + string.Join(", ", motion) + ") stay open, "
            + "because where a body IS is not a posture.");
    }

    private static List<string> HostMembers()
    {
        var found = new List<string>();
        string path = Path.Combine(ClientRoot, TheHostFile.Replace('/', Path.DirectorySeparatorChar));
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0
                || line.StartsWith("//", StringComparison.Ordinal)
                || line.StartsWith("using", StringComparison.Ordinal)
                || line.StartsWith("namespace", StringComparison.Ordinal)
                || line.StartsWith("public partial class", StringComparison.Ordinal)
                || line.StartsWith("private interface", StringComparison.Ordinal)
                || line is "{" or "}")
            {
                continue;
            }

            Match verb = Regex.Match(line, @"(\w+)\s*\([^()]*\);$");
            if (verb.Success)
            {
                found.Add(verb.Groups[1].Value + " (verb)");
                continue;
            }

            Match read = Regex.Match(line, @"(\w+)\s*\{\s*(?:get|set)[^}]*\}$");
            if (read.Success)
            {
                found.Add(read.Groups[1].Value + " (read)");
            }
        }
        return found;
    }

    /// <summary>Every field the PAGE keeps, by name, read off every client partial that is not the round's
    /// own. This is the set the round may not name: a patrol that can reach a page field is a patrol with no
    /// surface at all, which is the state 6′a, 6′b and 6′c were written to leave behind.</summary>
    private static List<string> PageFields() => PageDeclarations(@"\b(_\w+)\b", constants: false);

    /// <summary>…and every <c>const</c> THE PAGE ITSELF keeps, for the one hole a nested class leaves
    /// open: only a partial of <c>Map</c> counts, because only <c>Map</c>'s own statics are in scope
    /// inside a type nested in it. <c>DeckPlan.AvatarRadius</c> is a constant of the renderer's deck and
    /// is reached the way every other type in this assembly is — by naming it.</summary>
    private static List<string> PageConsts() => PageDeclarations(@"\bconst\s+[\w<>?,.\[\]]+\s+(\w+)", constants: true);

    private static List<string> PageDeclarations(string shape, bool constants)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in ClientSources())
        {
            string rel = Relative(path);
            if (rel.StartsWith("Pages/Patrol/", StringComparison.Ordinal)
                || !rel.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (constants
                && !File.ReadAllText(path).Contains("partial class Map", StringComparison.Ordinal))
            {
                continue;   // only the page's own statics are in scope inside a type nested in it
            }

            foreach (string line in File.ReadAllLines(path))
            {
                // A DECLARATION and never a use: at the class's own indent, ending its statement on its own
                // line, and not an expression-bodied anything. One line can declare several.
                if (!Regex.IsMatch(line, @"^ {4}(?:private|internal|protected|public)\b")
                    || !line.Contains(';', StringComparison.Ordinal)
                    || line.Contains("=>", StringComparison.Ordinal)
                    || (constants != line.Contains(" const ", StringComparison.Ordinal)))
                {
                    continue;
                }
                foreach (Match m in Regex.Matches(line, shape))
                {
                    names.Add(m.Groups[1].Value);
                }
            }
        }
        return names.OrderBy(n => n, StringComparer.Ordinal).ToList();
    }

    // ── WHERE THE SOURCE IS ───────────────────────────────────────────────────────────────────────────

    /// <summary>Every shipped client source: the C# partials and the markup. Anything the compiler reads,
    /// this guard reads — <c>obj/</c> and <c>bin/</c> excluded, because a generated copy of a file is not a
    /// second author naming the field.</summary>
    private static IEnumerable<string> ClientSources() =>
        Directory.EnumerateFiles(ClientRoot, "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || p.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .Where(p => !Segments(p).Any(s =>
                s.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || s.Equals("bin", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p => p, StringComparer.Ordinal);

    private static string[] Segments(string path) =>
        Relative(path).Split('/');

    private static string Relative(string path) =>
        Path.GetRelativePath(ClientRoot, path).Replace('\\', '/');

    private static string ClientRoot { get; } =
        Path.Combine(RepoRoot(), "src", "SpaceSails.Client");

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
