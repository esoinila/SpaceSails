using System.Text;

using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// THE DECK AUDIT. Owner: <i>"see the two crowded consoles at the back of our ship... fix that"</i>, and
/// then the ask that matters more than the fix — <i>"add a CI check to catch all such cases."</i>
///
/// <para>Every geometry law this game has lives in <c>SpaceSails.Core.Tests</c>, which can only see what has
/// been moved into Core. The consoles never were: they are literals in <c>DeckPlan.BuildShip</c>,
/// <c>HavenInterior</c>, <c>WreckInterior</c> and <c>MoonSurface</c>. That is precisely why four separate
/// console collisions shipped green — the scuttling panel on the nest, the dead bridge panel on the nav post,
/// the atmosphere valves on the charge dump, and the bridge repeater in the COMMS SEAT's lap.</para>
///
/// <para>So this walks the REAL plans, through the REAL <see cref="DeckPlan.NearestConsoleSpot"/> the [E] key
/// dispatches through. Two laws, and between them they cover the whole failure family:</para>
/// <list type="number">
/// <item><b>Every console can be reached.</b> There has to be somewhere the captain can stand where THIS
/// console is the one that answers. A console nobody can ever press is the bug the owner actually hit, and
/// it is invisible to a separation test — her valves cleared the radius by 0.35 du and were still
/// unreachable, because the old lookup returned the first console in array order rather than the
/// nearest.</item>
/// <item><b>No two consoles share a doorstep.</b> Labels are drawn above the dot; two of them inside a
/// couple of du are one illegible smear, which is what the owner saw twice.</item>
/// </list>
/// </summary>
public sealed class ConsoleCrowdingTests
{
    /// <summary>How finely the audit walks a console's neighbourhood, in deck units.</summary>
    private const double Step = 0.25;

    /// <summary>The smallest patch of walkable deck a console may own and still count as reachable. A
    /// 0.7 × 0.7 du spot — about the captain's own footprint, and the least that can be called "you can
    /// stand there".</summary>
    private const double MinOwnedArea = 0.5;

    /// <summary>How far apart two consoles must be for their labels to be separately readable. Well under
    /// twice the interact radius on purpose: a dense bridge (helm, nav post, scope, three desks) is a
    /// deliberate design and must stay legal — what must not happen is two dots on top of each other.</summary>
    private const double LabelClearance = 2.0;

    /// <summary>Every deck the game can put the captain on, built the way the game builds it.</summary>
    public static TheoryData<string> EveryDeck()
    {
        var data = new TheoryData<string>();
        foreach (string name in DeckNames())
        {
            data.Add(name);
        }
        return data;
    }

    private static IEnumerable<string> DeckNames()
    {
        yield return "ship";
        yield return "ship-all-hatches-dogged";

        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            yield return $"wreck:{cause}";
        }

        foreach (string id in HavenInterior.InteriorBodyIds)
        {
            yield return $"haven:{id}";
        }

        foreach (string id in SurfaceBodies)
        {
            yield return $"surface:{id}";
        }
    }

    /// <summary>A sample of walked ground. The surface layout is procedural per body, so a handful of real
    /// ones is the honest cover — and any of them putting a dig site on a kiosk is the same bug.</summary>
    private static readonly string[] SurfaceBodies = ["luna", "phobos", "titan", "enceladus"];

    /// <summary>Built by name so xUnit can print WHICH deck failed instead of an object hash.</summary>
    private static DeckPlan Build(string name)
    {
        if (name == "ship")
        {
            return DeckPlan.Ship;
        }

        if (name == "ship-all-hatches-dogged")
        {
            var shut = new HashSet<string>(StringComparer.Ordinal);
            foreach (ShipLayout.Room room in ShipLayout.Rooms)
            {
                shut.Add(room.Name);
            }
            return DeckPlan.ShipWith(shut);
        }

        if (name.StartsWith("wreck:", StringComparison.Ordinal))
        {
            var cause = Enum.Parse<Derelict.WreckCause>(name["wreck:".Length..]);
            var wreck = new Derelict.Wreck("audit-hull", "Audited Hull", cause, 250_000, 40.0);
            return WreckInterior.WreckDeck(
                wreck, new HashSet<string>(StringComparer.Ordinal), salvaged: false,
                droidCount: 0, fillDroids: static (_, _) => { });
        }

        if (name.StartsWith("haven:", StringComparison.Ordinal))
        {
            string id = name["haven:".Length..];
            DeckPlan? deck = HavenInterior.DockedDeck(id);
            Assert.NotNull(deck);
            return deck;
        }

        string body = name["surface:".Length..];
        return MoonSurface.SurfaceDeck(
            body, body, [], droidCount: 0, fillDroids: static (_, _) => { });
    }

    /// <summary>
    /// LAW ONE — every console has somewhere to be pressed from.
    ///
    /// <para>This is the law that was missing. The valves passed every separation test on the ship and were
    /// still impossible to open, because reachability is not distance: it is whether any point exists where
    /// this console beats every other console in range. A console that loses everywhere is drawn, labelled,
    /// glowing, and dead.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryDeck))]
    public void EveryConsoleCanBeReached(string deckName)
    {
        DeckPlan plan = Build(deckName);
        var dead = new StringBuilder();

        for (int i = 0; i < plan.Consoles.Length; i++)
        {
            DeckPlan.ConsoleSpot console = plan.Consoles[i];
            double owned = OwnedArea(plan, console);
            if (owned < MinOwnedArea)
            {
                dead.AppendLine(
                    $"  '{console.Label}' ({console.Kind}) at ({console.X:0.##}, {console.Y:0.##}) owns "
                    + $"{owned:0.00} du² of standing room — {Shadow(plan, console)}");
            }
        }

        Assert.True(dead.Length == 0,
                    $"{deckName}: consoles nobody can press —\n{dead}");
    }

    /// <summary>
    /// LAW TWO — no two consoles share a doorstep.
    ///
    /// <para>Reachability is not enough on its own: two dots 1.4 du apart are both reachable and still
    /// unreadable, because each draws its own label above itself. This is the law the owner has been
    /// enforcing by eye, twice, and it belongs in CI.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryDeck))]
    public void NoTwoConsolesShareADoorstep(string deckName)
    {
        DeckPlan plan = Build(deckName);
        var crowded = new StringBuilder();

        for (int i = 0; i < plan.Consoles.Length; i++)
        {
            for (int j = i + 1; j < plan.Consoles.Length; j++)
            {
                DeckPlan.ConsoleSpot a = plan.Consoles[i], b = plan.Consoles[j];
                double apart = Distance(a.X, a.Y, b.X, b.Y);
                if (apart < LabelClearance)
                {
                    crowded.AppendLine(
                        $"  '{a.Label}' and '{b.Label}' are {apart:0.00} du apart at "
                        + $"({a.X:0.##}, {a.Y:0.##}) / ({b.X:0.##}, {b.Y:0.##})");
                }
            }
        }

        Assert.True(crowded.Length == 0,
                    $"{deckName}: consoles crowding each other's labels —\n{crowded}");
    }

    /// <summary>
    /// And the guarantee the whole thing rests on: what the renderer lights is what the key presses.
    ///
    /// <para><c>DeckView</c> asks <see cref="DeckPlan.NearestConsoleSpot"/> for the console to draw the [E]
    /// over; <c>InteractAtConsole</c> asks it for the console to act on. This walks the deck and proves the
    /// answer is single-valued — which is only interesting because it was NOT, for as long as the lookup
    /// returned array order: the prompt was drawn over every console in range while one arbitrary one
    /// answered.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryDeck))]
    public void ThePromptAndTheKeyAgreeEverywhere(string deckName)
    {
        DeckPlan plan = Build(deckName);

        foreach (DeckPlan.ConsoleSpot console in plan.Consoles)
        {
            // Stand exactly ON a console: it must be the one that answers. Anything else means the captain
            // is standing on a panel that will not open — which is where this whole night started.
            DeckPlan.ConsoleSpot? answering = plan.NearestConsoleSpot(console.X, console.Y);
            Assert.NotNull(answering);

            double atMe = Distance(console.X, console.Y, answering.Value.X, answering.Value.Y);
            Assert.True(atMe < 1e-6,
                        $"{deckName}: standing on '{console.Label}' ({console.X:0.##}, {console.Y:0.##}) the "
                        + $"key answers '{answering.Value.Label}' ({answering.Value.X:0.##}, "
                        + $"{answering.Value.Y:0.##}) instead.");
        }
    }

    /// <summary>How much walkable deck this console owns: points the captain can stand on, within reach of
    /// it, with a clear line to it, from which [E] answers THIS console.</summary>
    private static double OwnedArea(DeckPlan plan, in DeckPlan.ConsoleSpot console)
    {
        double reach = DeckPlan.InteractRadius;
        int owned = 0;

        for (double x = console.X - reach; x <= console.X + reach; x += Step)
        {
            for (double y = console.Y - reach; y <= console.Y + reach; y += Step)
            {
                if (Distance(x, y, console.X, console.Y) > reach)
                {
                    continue;
                }
                if (!CanStand(plan, x, y) || !HasClearLine(plan, console, x, y))
                {
                    continue;
                }
                if (plan.NearestConsoleSpot(x, y) is not { } answering)
                {
                    continue;
                }
                if (Distance(answering.X, answering.Y, console.X, console.Y) < 1e-6)
                {
                    owned++;
                }
            }
        }

        return owned * Step * Step;
    }

    /// <summary>Which console is taking this one's ground — the sentence that makes a red build actionable
    /// instead of a puzzle.</summary>
    private static string Shadow(DeckPlan plan, in DeckPlan.ConsoleSpot console)
    {
        DeckPlan.ConsoleSpot? answering = plan.NearestConsoleSpot(console.X, console.Y);
        if (answering is not { } other || Distance(other.X, other.Y, console.X, console.Y) < 1e-6)
        {
            return "no console takes it, so it is walled in or off the deck.";
        }

        return $"'{other.Label}' answers even from on top of it "
               + $"({Distance(other.X, other.Y, console.X, console.Y):0.00} du away).";
    }

    /// <summary>Can the captain's body occupy this point? The avatar's own collision law, not a copy.</summary>
    private static bool CanStand(DeckPlan plan, double x, double y) =>
        !SurfaceCollision.Blocked(x, y, DeckPlan.AvatarRadius, plan.CollisionField);

    /// <summary>Is there a wall between the console and where the captain is standing? Keeps the audit from
    /// crediting a console with floor on the far side of a bulkhead — including the open deck outside the
    /// hull, which collides with nothing at all.</summary>
    private static bool HasClearLine(DeckPlan plan, in DeckPlan.ConsoleSpot console, double x, double y)
    {
        double dx = x - console.X, dy = y - console.Y;
        double span = Math.Sqrt((dx * dx) + (dy * dy));
        if (span < 1e-9)
        {
            return true;
        }

        return !plan.CastRay(console.X, console.Y, dx / span, dy / span,
                             out double hit, out _, out _, out _)
               || hit >= span;
    }

    private static double Distance(double x1, double y1, double x2, double y2)
    {
        double dx = x1 - x2, dy = y1 - y2;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
