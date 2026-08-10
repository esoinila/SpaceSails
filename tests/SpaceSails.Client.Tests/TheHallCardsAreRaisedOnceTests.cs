using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #751 · THE HALL AND THE CABINET, RAISED — AND RAISED ONCE.
///
/// <para>#725's family, extended to the two rooms the hall rule adds. Core owns WHERE each card belongs
/// (<c>TheCantinaHallTests</c>); what is left to get wrong is the wiring, and it is the same two ways: a card
/// that is never raised at all — a painted canvas and a room nobody is told anything in — or one raised on
/// every frame, which for a room-entry trigger polled off the avatar's position is not a hypothetical.</para>
///
/// <para>And one that is new here: <b>the cabinet's card and the cabinet's field note must not race.</b> They
/// are one event remembered two ways, so they hang off ONE latch. Two latches would eventually show a captain
/// the card without the book keeping it, and a save in between would make that permanent.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheHallCardsAreRaisedOnceTests
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
    /// <para>Zero is the state #725 was filed about — a painted canvas nobody shows — and two is the same
    /// beat fired twice. The message names the count and the files, so either failure reads as a finding
    /// rather than as a puzzle.</para>
    /// </summary>
    [Fact]
    public void EachHallCardIsRaisedFromExactlyOnePlace()
    {
        foreach (string constant in new[] { "CantinaHallLabel", "CabinetLabel" })
        {
            List<(string File, int At)> at = Raises(constant);
            Assert.True(at.Count == 1,
                $"UndergroundComplex.{constant} is raised from {at.Count} place(s) in the client " +
                $"[{string.Join(", ", at.Select(a => Path.GetFileName(a.File)))}] — wanted exactly one. " +
                "Zero is a painted canvas nobody shows; two is the same beat fired twice.");
        }

        // The scan can find things that are definitely there, so a zero above is a finding rather than a
        // broken sweep (the house's fifth bug class).
        Assert.NotEmpty(Raises("StaffMessLabel"));
        Assert.NotEmpty(Raises("WinteringHallLabel"));
    }

    /// <summary>
    /// …each behind its own once-per-excursion latch, and the cabinet's NOTE files off the same latch as its
    /// card.
    ///
    /// <para><b>Proven RED</b> by deleting <c>ex.HiveCabinetShown</c> from the poll's condition — the
    /// room-entry poll then reopens the card on the very next frame, forever, and files the same line into
    /// the field book on every one of them:</para>
    /// <code>
    /// the raise of CabinetLabel never SPENDS ex.HiveCabinetShown — a latch that is read and not written is
    /// a latch that never closes, and this card would fire on every frame the captain stands in the room.
    /// </code>
    /// </summary>
    [Fact]
    public void EachHallCardIsFencedByItsOwnLatchAndTheCabinetsNoteSharesIt()
    {
        foreach ((string constant, string latch) in new[]
                 {
                     ("CantinaHallLabel", "HiveCantinaHallShown"),
                     ("CabinetLabel", "HiveCabinetShown"),
                 })
        {
            (string file, int at) = Assert.Single(Raises(constant));
            string text = File.ReadAllText(file);

            int from = text.LastIndexOf("\n    private ", at, StringComparison.Ordinal);
            int to = text.IndexOf("\n    private ", at, StringComparison.Ordinal);
            Assert.True(from >= 0, $"could not find the member that raises {constant} in {file}.");
            string block = text[from..(to < 0 ? text.Length : to)];

            Assert.True(block.Contains($"ex.{latch} = true", StringComparison.Ordinal),
                $"the raise of {constant} never SPENDS ex.{latch} — a latch that is read and not written " +
                "is a latch that never closes, and this card would fire on every frame the captain stands " +
                "in the room.");
            Assert.True(
                Regex.Matches(block, $@"\bex\.{latch}\b").Count >= 2,
                $"the raise of {constant} spends ex.{latch} without ever reading it — an unread latch " +
                "fences nothing.");

            // …and it is ITS OWN latch. Both cards are raised inside one poll (you cannot be in a cabinet
            // without having walked the hall), so "the other flag is not in this method" would be a guard
            // about layout rather than about fencing. What must hold is that the raise sits inside an `if`
            // on its own latch, with nothing else's raise between the two — two cards sharing one flag
            // would mean seeing the hall silences the cabinet, and nothing would look broken.
            int fence = text.LastIndexOf($"!ex.{latch}", at, StringComparison.Ordinal);
            Assert.True(fence >= from,
                $"{constant} is raised without ever asking whether ex.{latch} is still unspent.");

            string other = latch == "HiveCabinetShown" ? "HiveCantinaHallShown" : "HiveCabinetShown";
            Assert.DoesNotContain($"ex.{other} = true", text[fence..at], StringComparison.Ordinal);
        }

        // THE NOTE AND THE CARD ARE ONE EVENT. The book's line is filed inside the very block the cabinet's
        // latch fences, so there is no second flag for a save to disagree with.
        (string cabFile, int cabAt) = Assert.Single(Raises("CabinetLabel"));
        string source = File.ReadAllText(cabFile);
        int start = source.LastIndexOf("if (!ex.HiveCabinetShown", cabAt, StringComparison.Ordinal);
        Assert.True(start >= 0, "the cabinet card is no longer fenced by an if on its own latch.");
        string cabinetBlock = source[start..];
        int end = cabinetBlock.IndexOf("\n    }", StringComparison.Ordinal);
        cabinetBlock = end > 0 ? cabinetBlock[..end] : cabinetBlock;

        Assert.Contains("UndergroundComplex.CabinetNote", cabinetBlock, StringComparison.Ordinal);
        Assert.Contains("FileNote(", cabinetBlock, StringComparison.Ordinal);
        // FileNote and NOT ShowAndFile: a pulse played under an open card's backdrop renders behind its
        // blur, which is #680 exactly.
        Assert.DoesNotContain("ShowAndFile(", cabinetBlock, StringComparison.Ordinal);
    }

    /// <summary>
    /// …AND THE POLL DOES NOT LAY A FLOOR OUT EVERY TICK. Both cards are room-entry triggers, which means
    /// they are polled off the avatar's position — and <c>UndergroundComplex.Build</c> lays a whole floor
    /// out. #725's own poll caches against (body, floor) for exactly this reason; this one has to as well,
    /// because it now asks about a room thirty times the floor area of the module.
    /// </summary>
    [Fact]
    public void ThePollAsksTheGeneratorOncePerFloorAndNotOncePerTick()
    {
        (string file, int at) = Assert.Single(Raises("CantinaHallLabel"));
        string text = File.ReadAllText(file);
        int from = text.LastIndexOf("\n    private ", at, StringComparison.Ordinal);
        int to = text.IndexOf("\n    private ", at, StringComparison.Ordinal);
        string block = text[from..(to < 0 ? text.Length : to)];

        Assert.Contains("UndergroundComplex.Build(", block, StringComparison.Ordinal);
        Assert.Contains("_hallFor != (ex.Stop.Body.Id, ex.Floor)", block, StringComparison.Ordinal);
        Assert.Contains("_hallFor = (ex.Stop.Body.Id, ex.Floor)", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// AND THE BOXES ARE THE ROOMS. The hall's card fires in the hall and the cabinet's inside a cabinet,
    /// and neither box holds anything it should not — measured on the deck the captain's boots collide with.
    /// </summary>
    [Fact]
    public void TheHallsBoxIsTheHallAndTheCabinetsBoxIsTheCabinet()
    {
        string[] bodies = ["luna", "phobos", "titan", "miranda", "secret-lab-site-unlisted"];
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        int checkedHalls = 0;

        foreach (string body in bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            UndergroundComplex.Amenity hallRoom = Assert.Single(
                UndergroundComplex.Build(body, level, field).Amenities,
                a => a.Use == UndergroundComplex.Comfort.UpperCanteen);
            UndergroundComplex.Hall hall = Assert.NotNull(hallRoom.Hall);
            checkedHalls++;

            DeckPlan deck = HiveInterior.FloorDeck(body, level, field, 0, (_, _) => { }, []);

            // The lift is not in the hall — a card that fired where the doors open would fire before the
            // captain had walked anywhere.
            DeckPlan.ConsoleSpot lift = Assert.Single(
                deck.Consoles, c => c.Kind == DeckPlan.ConsoleKind.HiveLift);
            Assert.False(hallRoom.Contains(lift.X, lift.Y), $"{body} B{-level}: the hall holds the LIFT.");

            // Nor is the washroom, nor any room the floor still has.
            foreach (UndergroundComplex.Amenity other in
                UndergroundComplex.Build(body, level, field).Amenities)
            {
                if (other.Use != hallRoom.Use)
                {
                    Assert.False(hallRoom.Contains(other.X, other.Y),
                        $"{body} B{-level}: the hall's box holds the {other.Use}.");
                }
            }

            // The three cabinets are inside the hall and outside each other — three cards would fire on one
            // step otherwise, and the beat is spent once.
            foreach (UndergroundComplex.Cabinet a in hall.Cabinets)
            {
                Assert.True(hallRoom.Contains(a.X, a.Y));
                foreach (UndergroundComplex.Cabinet b in hall.Cabinets)
                {
                    if (a.Number != b.Number)
                    {
                        Assert.False(a.Contains(b.X, b.Y),
                            $"{body} B{-level}: cabinets {a.Number} and {b.Number} overlap.");
                    }
                }

                // …and CabinetAt, which is the containment law the poll actually asks, agrees.
                Assert.Equal(a.Number, hall.CabinetAt(a.X, a.Y)!.Value.Number);
            }

            // The middle of the hall floor is NOT in any cabinet, so walking the room does not raise the
            // cabinet's card by accident.
            Assert.Null(hall.CabinetAt(hallRoom.X, hallRoom.Y));
            foreach ((double tx, double ty) in hallRoom.Tables)
            {
                Assert.Null(hall.CabinetAt(tx, ty));
            }
        }

        Assert.True(checkedHalls >= 4, $"only {checkedHalls} halls were checked — this proved little.");
    }
}
