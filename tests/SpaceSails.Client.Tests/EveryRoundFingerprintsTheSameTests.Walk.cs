using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// <b>WALKING ONE CASE, AND WHAT A FRAME LEAVES BEHIND</b> — the machinery under
/// <see cref="EveryRoundFingerprintsTheSameTests"/>' pins.
///
/// <para>What this part owns is how a case becomes a fingerprint: the walk itself, the census of what a
/// frame leaves behind on the page, the one renderer of a value that makes two runs comparable, and the
/// plumbing. Nothing here asserts anything — it is the instrument the facts next door are read off.</para>
/// </summary>
public sealed partial class EveryRoundFingerprintsTheSameTests
{
    // ── WALKING ONE CASE ──────────────────────────────────────────────────────────────────────────────

    private static (string Text, int Frames, IReadOnlyDictionary<string, int> Arms,
                    IReadOnlyDictionary<string, int> AlsoTrue) Walk(Case c)
    {
        (Pages.Map map, object ex) = c.Stage();
        var arms = new Dictionary<string, int>(StringComparer.Ordinal);
        var alsoTrue = new Dictionary<string, int>(StringComparer.Ordinal);
        var sb = new StringBuilder();
        sb.Append("case ").Append(c.Name).Append('\n');

        MethodInfo step = typeof(Pages.Map).GetMethod("AdvancePatrol", Hidden)
            ?? throw new InvalidOperationException("the page has no AdvancePatrol — this guard is dead.");

        Dictionary<string, string> before = EveryScalarOnThePage(map);

        for (int frame = 0; frame < c.Frames; frame++)
        {
            c.EachFrame?.Invoke(map, ex, frame);
            Census(map, ex, arms);

            double dt = Dts[frame % Dts.Length];
            step.Invoke(map, [dt]);

            sb.Append("f").Append(frame).Append(" dt=").Append(R(dt)).Append('\n');
            sb.Append(TheFloorAfterThatFrame(map, ex));
            IsHeStillOneMan(map, c, frame, alsoTrue);
        }

        sb.Append("── what this case moved on the page ──\n")
          .Append(WhatMoved(before, EveryScalarOnThePage(map)));
        return (Normalize(sb.ToString()), c.Frames, arms, alsoTrue);
    }

    /// <summary>
    /// #870 lane 6′d · AND HE IS STILL ONE MAN. <c>Guard.Check()</c> asked of every guard after every frame
    /// of every case — eight pairs of postures that may never be true together (#920 added the eighth), over
    /// 7,100 frames.
    ///
    /// <para>It rides inside the walk rather than in a case set of its own, so it costs nothing and so it
    /// cannot drift out of step with the transcripts: the frame that is pinned is the frame that is
    /// checked.</para>
    ///
    /// <para>Three pairs are tallied rather than asserted, and named here as they are on <c>Guard.Check</c>:
    /// a walk-up a bench has suspended, a stand remembered across a detour, and a spent walk-up clock. The
    /// first two are behaviour, not slips. The third was too until #920 made it a law, and its tally is kept
    /// as the second witness — <see cref="TheTwoThatCoexistAndTheOneThatIsNowALaw"/> makes each of them a
    /// number rather than a paragraph.</para>
    /// </summary>
    private static void IsHeStillOneMan(
        Pages.Map map, Case c, int frame, Dictionary<string, int> alsoTrue)
    {
        foreach (object g in Guards(map))
        {
            object? wrong = g.GetType().GetMethod("Check", Hidden)!.Invoke(g, []);
            Assert.True(wrong is null,
                $"case \"{c.Name}\", frame {frame}: {wrong}\n"
                + "Two postures that cannot coexist are both true on one guard. This is not a test that "
                + "needs relaxing: either a transition forgot one of the assignments it owns, or the arms "
                + "of the chain have been reordered under it.");

            bool held = (bool)Get(g, "Held")!, walkingUp = (bool)Get(g, "WalkingUp")!;
            Tally(alsoTrue, "a walk-up a bench has suspended", held && walkingUp);
            Tally(alsoTrue, "a stand remembered across a detour",
                (double)Get(g, "Standing")! > 0 && Get(g, "Route") is AutoWalk { Active: true });
            Tally(alsoTrue, "a spent walk-up clock", (double)Get(g, "WalkUpFor")! > 0 && !walkingUp);
        }
    }

    private static void Tally(Dictionary<string, int> into, string what, bool it)
    {
        if (it)
        {
            into[what] = into.GetValueOrDefault(what) + 1;
        }
    }

    /// <summary>Which arm each guard would take on the frame about to be spent. A replica of the chain's own
    /// tests over the page's own private answers — for the coverage fact and for nothing else.</summary>
    private static void Census(Pages.Map map, object ex, Dictionary<string, int> arms)
    {
        var guards = Guards(map);
        if (guards.Count == 0
            || (int)ex.GetType().GetProperty("Floor")!.GetValue(ex)! >= 0)
        {
            return;
        }

        object? escort = Get(map, "_escort");
        object? hide = Invoke(map, "TheCubicleTheCaptainIsShutIn", ex);
        bool sitting = (bool)Get(map, "SeatedOnABenchInTheOpen")!;
        var sight = (IReadOnlyList<SurfaceCollision.Segment>)Invoke(map, "SightBlockers")!;
        double ax = (double)Get(map, "_avatarX")!, ay = (double)Get(map, "_avatarY")!;

        for (int i = 0; i < guards.Count; i++)
        {
            object g = guards[i];
            FootTail.Mover afoot = PatrolBeat.OnTheRound(
                i, (double)Get(g, "X")!, (double)Get(g, "Y")!);
            bool held = FootTail.MustHold(sitting, ax, ay, in afoot, sight);

            string arm =
                ReferenceEquals(g, escort) ? "the escort"
                : hide is not null && CubicleLock.WaitsAtTheDoor((bool)Get(g, "SawYouShutIt")!)
                    ? "the knock at the door"
                : (bool)Get(g, "AfterYou")! ? "the run"
                : hide is not null && (bool)Get(g, "WalkingUp")! ? "lost to a door"
                : held ? "the cover act"
                : (bool)Get(g, "WalkingUp")! ? "the walk-up"
                : "the round's own leg";
            arms[arm] = arms.GetValueOrDefault(arm) + 1;
        }
    }

    // ── WHAT A FRAME LEAVES BEHIND ────────────────────────────────────────────────────────────────────

    private static string TheFloorAfterThatFrame(Pages.Map map, object ex)
    {
        var sb = new StringBuilder();
        var guards = Guards(map);
        sb.Append("  captain ").Append(R(Get(map, "_avatarX"))).Append(' ')
          .Append(R(Get(map, "_avatarY"))).Append('\n');
        sb.Append("  guards ").Append(guards.Count).Append('\n');

        for (int i = 0; i < guards.Count; i++)
        {
            object g = guards[i];
            sb.Append("  [").Append(i).Append(']');
            foreach (MemberInfo m in GuardMembers(g.GetType()))
            {
                sb.Append(' ').Append(m.Name).Append('=').Append(R(ValueOf(g, m)));
            }
            sb.Append('\n');
        }

        object? escort = Get(map, "_escort");
        object? due = Get(map, "_escortDue");
        sb.Append("  escort=").Append(IndexOf(guards, escort))
          .Append(" due=").Append(IndexOf(guards, due))
          .Append(" car=").Append(R(Get(map, "_escortCar")))
          .Append(" seconds=").Append(R(Get(map, "_escortSeconds")))
          .Append(" pumps=").Append(R(Get(map, "_escortSaidPumps")))
          .Append('\n');
        sb.Append("  kickOut=").Append(R(Get(map, "_kickOutDue")))
          .Append(" ride=").Append(R(Get(map, "_kickOutRideDue")))
          .Append(" plate=").Append(R(Get(map, "_kickedOutPlateFor")))
          .Append(" escorts=").Append(R(Get(map, "_escortsThisWatch")))
          .Append(" walkedAway=").Append(R(Get(map, "_walkedAwayThisWatch")))
          .Append('\n');
        sb.Append("  floorSeconds=").Append(R(Get(map, "_patrolFloorSeconds")))
          .Append(" heardAgo=").Append(R(Get(map, "_patrolHeardAgo")))
          .Append(" fan=").Append(R(Get(map, "_walletFanOpen")))
          .Append(" paper=").Append(R(Get(map, "_paperInHand")))
          .Append(" walkedPast=").Append(R(Get(map, "_walkedPastSaid")))
          .Append(" shownBook=").Append(((ICollection)Get(map, "_shownBook")!).Count)
          .Append('\n');
        sb.Append("  card=").Append(R(Get(map, "_viewObject"))).Append('\n');
        sb.Append("  pulse=").Append(R(Get(map, "_pulse"))).Append('\n');

        var events = (IEnumerable)Get(map, "_autopilotEvents")!;
        foreach (object row in events)
        {
            sb.Append("  log ").Append(R(row)).Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Every scalar on the whole page, by name — read once before a case and once after.
    ///
    /// <para>#870 lane 6′b · …and the round's own twenty-two, which are not fields on the page any more
    /// (<see cref="PatrolState"/>). They are read through the same classification and written down under the
    /// name they had, because THEY are the scalars a patrol frame actually moves: dropping them would have
    /// left this census diffing everything except the subject of the file. Not one pinned line changed.
    /// </para></summary>
    private static Dictionary<string, string> EveryScalarOnThePage(Pages.Map map)
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (FieldInfo f in typeof(Pages.Map)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            Note(seen, f.Name, f.GetValue(map));
        }
        foreach ((string raw, _) in PatrolState.TheTwentyTwo)
        {
            Assert.True(PatrolState.TryFollow(map, raw, out object? onTheRound),
                $"`{raw}` was not followed onto the round — this census is reading a dead name.");
            Note(seen, raw, onTheRound);
        }
        return seen;
    }

    /// <summary>One reading, classified the one way — a scalar by its value, a collection by its count, and
    /// anything else not at all.</summary>
    private static void Note(Dictionary<string, string> seen, string name, object? v)
    {
        if (v is null)
        {
            seen[name] = "-";
        }
        else if (v is bool or int or long or double or float or string or Enum or decimal)
        {
            seen[name] = R(v);
        }
        else if (v is ICollection col)
        {
            seen[name + ".Count"] = col.Count.ToString(Inv);
        }
    }

    /// <summary>
    /// WHAT THIS CASE MOVED ON THE PAGE — the whole scalar surface of <see cref="Pages.Map"/>, before against
    /// after, with only the differences written down.
    ///
    /// <para>The DIFFERENCE rather than the census is the point. A census of every field would catch the same
    /// side effects, and it would also change every hash in this file the day a lane somewhere else on the
    /// page adds, renames or retires a field the round has never heard of — which is what happened the first
    /// time this guard met CI, on the morning #870's seat lane landed. A field this method never writes
    /// contributes nothing here whether it exists or not; a field it DOES write is named, with both values,
    /// and moves the hash.</para>
    /// </summary>
    private static string WhatMoved(
        IReadOnlyDictionary<string, string> before, IReadOnlyDictionary<string, string> after)
    {
        var sb = new StringBuilder();
        foreach (string name in before.Keys.Concat(after.Keys).Distinct().OrderBy(n => n, StringComparer.Ordinal))
        {
            string was = before.GetValueOrDefault(name, "(not there)");
            string now = after.GetValueOrDefault(name, "(not there)");
            if (!string.Equals(was, now, StringComparison.Ordinal))
            {
                sb.Append("  ").Append(name).Append(": ").Append(was).Append(" → ").Append(now).Append('\n');
            }
        }
        return sb.ToString();
    }

    private static IEnumerable<MemberInfo> GuardMembers(Type guard) =>
        guard.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => !f.Name.Contains('<', StringComparison.Ordinal))
            .Cast<MemberInfo>()
            .Concat(guard.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetIndexParameters().Length == 0))
            .OrderBy(m => m.Name, StringComparer.Ordinal);

    private static object? ValueOf(object o, MemberInfo m) =>
        m is FieldInfo f ? f.GetValue(o) : ((PropertyInfo)m).GetValue(o);

    private static string IndexOf(IReadOnlyList<object> guards, object? one)
    {
        if (one is null)
        {
            return "-";
        }
        for (int i = 0; i < guards.Count; i++)
        {
            if (ReferenceEquals(guards[i], one))
            {
                return i.ToString(Inv);
            }
        }
        return "gone";
    }

    // ── WRITING A VALUE DOWN ──────────────────────────────────────────────────────────────────────────

    private static string R(object? v) => v switch
    {
        null => "-",
        double d => d.ToString("R", Inv),
        float fl => fl.ToString("R", Inv),
        bool b => b ? "yes" : "no",
        AutoWalk route =>
            $"route[{route.Route.Count} pts, cursor {R(Get(route, "_cursor"))}, "
            + $"active {R(route.Active)}, arrived {R(route.Arrived)}, cancelled {R(route.Cancelled)}, "
            + $"snagged {R(Get(route, "_snagged"))}, "
            + $"{string.Join(" ", route.Route.Select(p => p.ToString()))}]",
        AutoWalk.Planner plan => $"plan[{plan.From} → {plan.To}, done {R(plan.Done)}]",
        IFormattable other => other.ToString(null, Inv),
        _ => v.ToString() ?? "-",
    };

    /// <summary>The one normalisation, and the reason is in the class docblock: six decimals, so a platform's
    /// libm last bit cannot redden a guard about a code move.</summary>
    private static readonly Regex Numbers = new(
        @"-?\d+\.\d+(?:[eE][-+]?\d+)?|-?\d+[eE][-+]?\d+", RegexOptions.Compiled);

    private static string Normalize(string raw) => Numbers.Replace(raw, m =>
    {
        double v = double.Parse(m.Value, NumberStyles.Float, Inv);
        double r = Math.Round(v, 6, MidpointRounding.AwayFromZero);
        return (r == 0 ? 0 : r).ToString("0.######", Inv);
    });

    private static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#870 lane 6′b · The twenty-two patrol fields live on the page's <c>_patrol</c>
    /// object now, so the lookup follows them there (<see cref="PatrolState"/>); every assertion and
    /// every pinned line below still asks for the state by the name it was written with.</summary>
    private static object? Get(object o, string name)
    {
        if (PatrolState.TryFollow(o, name, out object? onTheRound))
        {
            return onTheRound;
        }
        FieldInfo? field = o.GetType().GetField(name, Hidden);
        if (field is not null)
        {
            return field.GetValue(o);
        }
        PropertyInfo? prop = o.GetType().GetProperty(name, Hidden);
        Assert.True(prop is not null, $"`{name}` is not on {o.GetType().Name} — this guard is reading a dead name.");
        return prop!.GetValue(o);
    }

    /// <inheritdoc cref="Get"/>
    private static void Set(object o, string field, object? value)
    {
        if (!PatrolState.TrySet(o, field, value))
        {
            o.GetType().GetField(field, Hidden)!.SetValue(o, value);
        }
    }

    /// <summary>#870 lane 6′c · A VERB FOLLOWS THE WAY A READ ALREADY DID. The round's verbs moved onto
    /// <c>Map.Patrol</c>, so this looks on the page first (thirteen of them are still forwarded there under
    /// their old spellings, and a forwarder is the same call) and then on the round hanging off it. Where it
    /// looks is <see cref="PatrolState"/>'s to know, exactly as <c>_guards</c> and <c>_escort</c> are — one
    /// copy of "where the round is now", which is why #909's bug class cannot come back through here.</summary>
    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        (object Target, MethodInfo Call)? found = PatrolState.Verb(map, method);
        Assert.True(
            found is not null,
            $"neither the page nor its round has `{method}` — this guard is reading a dead name.");
        return found!.Value.Call.Invoke(found.Value.Target, args);
    }
}
