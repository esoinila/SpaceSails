using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #725 · THE TWO SILENT FINDS, RAISED — AND RAISED ONCE.
///
/// <para>Core owns WHERE each card belongs (<c>TheSilentFindsGetACardTests</c>). What is left to get wrong is
/// the wiring, and there are exactly two ways to get it wrong: the card is never raised at all — which is the
/// state the issue was filed about, a painted canvas and a floor nobody is told anything on — or it is raised
/// on every frame, which for a room-entry trigger polled off the avatar's position is not a hypothetical. A
/// modal that reopens itself the moment you close it is worse than silence.</para>
///
/// <para><b>Why the shape of the source and not a driven page.</b> The once-ness IS a source property here:
/// there is no seam to call, only a bool on the excursion read in one place and written in the same block.
/// This project cannot instantiate <c>Map</c> (a Blazor component with private state), so the honest thing to
/// audit is the shipped text of the one block that raises each card — and that text is exactly what runs. The
/// geometry half below is not a text scan: it asks the real deck.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheSilentFindsAreRaisedOnceTests
{
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

    /// <summary>Every line of shipping client code, one file at a time — obj/ and bin/ skipped, because a
    /// generated copy of a razor file would count every call twice.</summary>
    private static IEnumerable<(string File, string Text)> ClientSource()
    {
        string dir = Path.Combine(RepoRoot(), "src", "SpaceSails.Client");
        foreach (string file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
        {
            char s = Path.DirectorySeparatorChar;
            if (Path.GetExtension(file) is not (".cs" or ".razor")
                || file.Contains($"{s}obj{s}", StringComparison.Ordinal)
                || file.Contains($"{s}bin{s}", StringComparison.Ordinal))
            {
                continue;
            }
            yield return (file, File.ReadAllText(file));
        }
    }

    private static List<(string File, int At)> Raises(string constant)
    {
        var found = new List<(string, int)>();
        foreach ((string file, string text) in ClientSource())
        {
            foreach (Match m in Regex.Matches(text, $@"UndergroundComplex\.{constant}\b"))
            {
                found.Add((file, m.Index));
            }
        }
        return found;
    }

    /// <summary>
    /// Each card is raised, and from exactly one place.
    ///
    /// <para><b>Proven RED</b> both ways. Delete the <c>_viewObject = …</c> block in
    /// <c>Map.Surface.RideTheLiftTo</c> and this reports <c>UnlistedLobbyLabel is raised from 0 place(s)</c> —
    /// which is the issue's own finding, a painted canvas with nobody to show it. Paste the mess block a
    /// second time and it reports 2.</para>
    /// </summary>
    [Fact]
    public void EachSilentFindIsRaisedFromExactlyOnePlace()
    {
        foreach (string constant in new[] { "UnlistedLobbyLabel", "StaffMessLabel" })
        {
            List<(string File, int At)> at = Raises(constant);
            Assert.True(at.Count == 1,
                $"UndergroundComplex.{constant} is raised from {at.Count} place(s) in the client " +
                $"[{string.Join(", ", at.Select(a => Path.GetFileName(a.File)))}] — wanted exactly one. " +
                "Zero is a painted canvas nobody shows; two is the same beat fired twice.");
        }

        // The scan can find things that are definitely there, so a zero above is a finding rather than a
        // broken sweep (the house's fifth bug class).
        Assert.NotEmpty(Raises("VacuumCardLabel"));
        Assert.NotEmpty(Raises("WinteringHallLabel"));
    }

    /// <summary>
    /// …and each raise is fenced by its own once-per-excursion latch, read and written in the same block.
    ///
    /// <para><b>Proven RED</b> by deleting <c>ex.HiveStaffMessShown</c> from the mess block's condition — the
    /// room-entry poll then reopens the card on the very next frame, forever, and this reports the block as
    /// unlatched.</para>
    /// </summary>
    [Fact]
    public void EachSilentFindIsFencedByItsOwnLatch()
    {
        foreach ((string constant, string latch) in new[]
                 {
                     ("UnlistedLobbyLabel", "HiveUnlistedPlateShown"),
                     ("StaffMessLabel", "HiveStaffMessShown"),
                 })
        {
            (string file, int at) = Assert.Single(Raises(constant));
            string text = File.ReadAllText(file);

            // The enclosing member, taken by its declaration rather than by a character window: a window
            // wide enough to hold the raise is a window that quietly widens every time the method grows,
            // and a latch three methods away is not a fence.
            int from = text.LastIndexOf("\n    private ", at, StringComparison.Ordinal);
            int to = text.IndexOf("\n    private ", at, StringComparison.Ordinal);
            Assert.True(from >= 0, $"could not find the member that raises {constant} in {file}.");
            string block = text[from..(to < 0 ? text.Length : to)];

            // Written, and read. Both halves are asserted separately because each alone is a shipped bug:
            // a latch nobody writes never closes, and a latch nobody reads closes over nothing. The read is
            // counted rather than matched on "!ex.…" — the plate fences with a negation inside an `if` and
            // the mess with a positive in an early-return pattern, and both are correct.
            Assert.True(block.Contains($"ex.{latch} = true", StringComparison.Ordinal),
                $"the raise of {constant} never SPENDS ex.{latch} — a latch that is read and not written " +
                "is a latch that never closes, and this card would fire on every arrival (and, for the " +
                "mess, on every frame you stand in the room).");
            Assert.True(
                Regex.Matches(block, $@"\bex\.{latch}\b").Count >= 2,
                $"the raise of {constant} spends ex.{latch} without ever reading it — an unread latch " +
                "fences nothing.");

            // …and it is its OWN latch: two cards sharing one flag would mean seeing the plate silences the
            // mess, which is the kind of bug nobody would ever report because nothing looks broken.
            string other = latch == "HiveStaffMessShown" ? "HiveUnlistedPlateShown" : "HiveStaffMessShown";
            Assert.DoesNotContain($"ex.{other} = true", block, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// AND THE BOX IS THE ROOM. The mess card's trigger is a containment test, so it is worth asking the deck
    /// the captain's boots actually collide with: the machines the game draws for the mess stand inside the
    /// card's box, and the floor's other amenity consoles do not.
    ///
    /// <para>This is the half a text scan cannot do, and it is the half that would go wrong quietly — a box
    /// that missed the room would leave the card unreachable with every test above still green.</para>
    ///
    /// <para><b>Proven RED</b> by swapping <c>Amenity.Contains</c> for the hand-typed "near enough" box a
    /// room-entry trigger invites (<c>&lt;= 20</c> either way), which reaches the search console of the room
    /// across the corridor:</para>
    /// <code>
    /// luna B13: 2 of 18 consoles fall inside the mess card's box
    ///           [HiveHaul "🔦 SEARCH THE ROOM", HiveAmenity "🍽 THE MACHINES"] — wanted exactly the mess's own machines.
    /// </code>
    /// </summary>
    [Fact]
    public void TheMessCardsBoxHoldsTheMachinesTheDeckDraws()
    {
        string[] bodies = ["luna", "phobos", "titan", "enceladus", "miranda", "secret-lab-site-unlisted"];
        int checkedFloors = 0, checkedConsoles = 0;

        foreach (string body in bodies)
        {
            if (UndergroundComplex.StaffCanteenFloor(body) is not { } level)
            {
                continue;
            }

            SurfaceLayout.Field field = MoonSurface.ExpeditionField();
            UndergroundComplex.Amenity mess = Assert.Single(
                UndergroundComplex.Build(body, level, field).Amenities,
                a => a.Use == UndergroundComplex.Comfort.StaffCanteen);

            DeckPlan deck = HiveInterior.FloorDeck(body, level, field, 0, (_, _) => { }, []);
            Assert.NotEmpty(deck.Consoles);

            // EVERY console the floor draws, not just the amenities: the machines the mess's own [E] verb
            // sits on must be inside the card's box, and every other thing a captain can press on that
            // floor — every room, every locked door's plate, the lift itself — must be outside it. A box
            // that reached the room across the corridor would open the mess card in somebody else's office.
            var inside = new List<string>();
            foreach (DeckPlan.ConsoleSpot spot in deck.Consoles)
            {
                checkedConsoles++;
                if (mess.Contains(spot.X, spot.Y))
                {
                    inside.Add($"{spot.Kind} \"{spot.Label}\"");
                }
            }

            Assert.True(inside.Count == 1,
                $"{body} B{-level}: {inside.Count} of {deck.Consoles.Length} consoles fall inside the mess " +
                $"card's box [{string.Join(", ", inside)}] — wanted exactly the mess's own machines.");
            Assert.Contains("HiveAmenity", inside[0], StringComparison.Ordinal);
            checkedFloors++;
        }

        Assert.True(checkedFloors >= 4, $"only {checkedFloors} floors had a staff mess — this proved little.");
        Assert.True(checkedConsoles >= 4, $"only {checkedConsoles} consoles were tried — this proved little.");
    }
}
