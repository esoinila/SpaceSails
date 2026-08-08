using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #756 · THE COUNTER TAKES ORDERS, AND THE HALL WEARS ITS ART — the client half.
///
/// <para>Owner, live playtest 2026-08-08, walked to the B1 cantina hall's counter: <i>"HOW DO I ORDER A
/// DRINK FROM THE BAR?????? I walk to the bar to buy a drink... not possible... WHY?"</i> — and, on the same
/// floor, <i>"let's put todo to have gen-AI Bar image on the background like we have in space ports."</i></para>
///
/// <para>Core owns whether a fixture serves and which picture a hall wears (<c>TheCounterTakesOrdersTests</c>
/// over there). What is left to get wrong is the WIRING, which is exactly what was wrong for two issues: the
/// counter existed, the card existed, the menu existed, and the key that joins them did not. So these read
/// the press path and the drawn frame.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCounterTakesOrdersTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private const int WidthPx = 1280;
    private const int HeightPx = 560;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    // ── Reading the shipped source, the way the #736 guards do ──────────────────────────────────────────
    //
    // Transcribed rather than asked for: a test that asked the page for its own call graph could not notice
    // the call going away, which is the entire failure this file exists to catch.
    private static string Pages(string file)
    {
        string here = AppContext.BaseDirectory;
        for (DirectoryInfo? d = new(here); d is not null; d = d.Parent)
        {
            string candidate = Path.Combine(d.FullName, "src", "SpaceSails.Client", "Pages", file);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }
        }
        throw new FileNotFoundException($"could not find src/SpaceSails.Client/Pages/{file} from {here}");
    }

    private static string Method(string file, string signature)
    {
        string src = Pages(file);
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{file} no longer has `{signature}` where this guard can read it.");
        int end = src.IndexOf("\n    private ", at + 1, StringComparison.Ordinal);
        return src[at..(end > at ? end : src.Length)];
    }

    [Fact]
    public void PressingEAtTheCounterOpensTheServiceCard()
    {
        // (a) THE BUG ITSELF. HiveAmenityInteract is what [E] at a counter reaches (Map.Deck's dispatch,
        // ConsoleKind.HiveAmenity) and for two issues its whole answer was a paragraph. It must now ASK CORE
        // whether this fixture serves and, when it does, OPEN THE CARD.
        //
        // Red proof: delete the `OpenCounterService(counter);` line — or the `CounterService.For(` question
        // in front of it — and this fails naming what went missing.
        string press = Method("Map.Surface.cs", "private void HiveAmenityInteract()");

        Assert.Contains("CounterService.For(", press, StringComparison.Ordinal);
        Assert.Contains("OpenCounterService(", press, StringComparison.Ordinal);

        // …and it must ask about the amenity the captain is actually standing at, not about the floor. The
        // press already matched `a` by position; asking with anything else would open the wrong room's card.
        Assert.Contains("CounterService.For(ex.Stop.Body.Id, a.Use)", press, StringComparison.Ordinal);

        // The room's own paragraph still lands, and lands FILED rather than flashed — a pulse raised in the
        // same breath as a card plays under that card's blur (#686/#736), which is a line not said.
        Assert.Contains("FileNote(", press, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCardItOpensIsTheOneTheHavenBarsAlreadyOpen()
    {
        // The one-seam law, read off the source. If OpenCounterService ever grows its own dialog state, the
        // B1 counter and the Tilt bar stop being one machine and this repo grows its sixth twice-told truth.
        string open = Method("Map.Quests.cs", "private void OpenCounterService(");

        Assert.Contains("_barMenu = counter;", open, StringComparison.Ordinal);
        Assert.Contains("_barNotice = counter.Greeting;", open, StringComparison.Ordinal);

        // It opens ON the menu. The reported bug is a captain who could not see anything to order; a card
        // that opens closed is that bug wearing a card.
        Assert.Contains("_showBarMenu = true;", open, StringComparison.Ordinal);

        // And it is not a second implementation of the purchase: nothing here spends, pours or debits.
        Assert.DoesNotContain("_credits", open, StringComparison.Ordinal);
        Assert.DoesNotContain("PourRum(", open, StringComparison.Ordinal);
    }

    [Fact]
    public void WhatAPurchaseAnswersIsDrawnOnTheCardThatWasPressed()
    {
        // (b) #736's law, for the verb this issue adds. BuyDrink is the one handler both counters share, so
        // its answer has to land in the slot the open card draws — and the card has to draw it.
        string buy = Method("Map.Quests.cs", "private void BuyDrink(");

        Assert.Contains("_barNotice = receipt;", buy, StringComparison.Ordinal);
        Assert.Contains("_barNotice =", buy, StringComparison.Ordinal);

        // One price, asked of Core, used by the debit. A handler that debited a different number than the
        // button printed is bug class five with a receipt attached.
        Assert.Contains("drink.PriceAt(keep.DrinkPrice)", buy, StringComparison.Ordinal);
        Assert.Contains("_credits -= cost;", buy, StringComparison.Ordinal);

        // Food is not a pour: a tray must not route through the one wobble law.
        Assert.Contains("DrinkCategory.Food", buy, StringComparison.Ordinal);

        // …and the slot it writes into is rendered INSIDE the counter card's own block, not in a banner
        // behind it. Cut the block the way the #736 guards cut theirs: from its own `@if (` to the next.
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("@if (_deckMode && _barMenu is { } keep)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the counter card block this guard knows how to find.");
        int end = razor.IndexOf("\n    @if (", start + 10, StringComparison.Ordinal);
        string block = razor[start..(end > start ? end : razor.Length)];

        Assert.Contains("_barNotice", block, StringComparison.Ordinal);
        Assert.Contains("@(() => BuyDrink(d))", block, StringComparison.Ordinal);

        // The button's own label, its enabled-ness and the debit all read the SAME number.
        Assert.Contains("d.PriceAt(keep.DrinkPrice)", block, StringComparison.Ordinal);
        Assert.Contains("_credits < cost", block, StringComparison.Ordinal);

        // Nobody is behind the B1 counter, so the two verbs that need a person are gated (#618 canon).
        Assert.Contains("keep.SelfService", block, StringComparison.Ordinal);

        // And the desk the captain is standing at is drawn on the panel they are standing at it through.
        Assert.Contains("keep.DeskArtUrl", block, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDevStartWalksTheCaptainToTheFixtureAndNotToACoordinate()
    {
        // Owner's standing rule: every new feature ships with a URL that demos it. And it must reach the
        // fixture by ASKING which one serves — a hand-typed square would drift the day the hall is recarved.
        string stand = Method("Map.Surface.cs", "private void StandAtTheCounterIfAsked(");

        Assert.Contains("CounterService.For(", stand, StringComparison.Ordinal);
        Assert.Contains("StandCaptainAt(a.X, a.Y", stand, StringComparison.Ordinal);
        Assert.Contains("_counterCheat", stand, StringComparison.Ordinal);

        Assert.Contains(DevStarts.All, e => e.Url == "/map?counter=1");
    }

    // ── (c) THE HALL WEARS ITS ART ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheHallFloorPlanCarriesTheBackdropOverTheHallsOwnBox()
    {
        // The PLAN half. HiveInterior handed DeckPlan a bare `[]` for backdrops on every floor of every site
        // ever generated, so the biggest social room in the game drew as grid.
        var wrong = new List<string>();
        int painted = 0;
        int headOffices = 0;

        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            DeckPlan deck = DeckFor(body, level);
            UndergroundComplex.Amenity canteen = UndergroundComplex
                .Build(body, level, Field).Amenities
                .Single(a => a.Use == UndergroundComplex.Comfort.UpperCanteen);
            UndergroundComplex.Hall hall = canteen.Hall
                ?? throw new InvalidOperationException($"{body} B{-level} carved no hall");

            // THE HEAD OFFICE IS NOT A BRANCH OFFICE, and this guard found that out the hard way: the ice
            // moon's B1 is a DINING ROOM · GUESTS & DEPUTATIONS (#411), not CANTEEN 1, and its picture has
            // not been shot. Its floor must stay bare rather than borrow a branch canteen's frame — the
            // #755 art is a room with poured pillars and working people in it, and the head office
            // outclasses the branch offices in their own vocabulary.
            if (UndergroundComplex.IsHeadOffice(body))
            {
                if (deck.Backdrops.Length != 0)
                {
                    wrong.Add($"  {body} B{-level}: the HEAD OFFICE's dining room is wearing a branch canteen's art.");
                }
                headOffices++;
                continue;
            }

            if (deck.Backdrops.Length != 1)
            {
                wrong.Add($"  {body} B{-level}: {deck.Backdrops.Length} backdrop(s) on the hall floor, wanted 1.");
                continue;
            }

            DeckPlan.Backdrop bd = deck.Backdrops[0];
            if (bd.Url != UndergroundComplex.CantinaHallArtUrl)
            {
                wrong.Add($"  {body} B{-level}: the floor wears “{bd.Url}”.");
            }

            // The rectangle is the hall's OWN box — top-left at (X0, Y1), because deck +y is up and that is
            // the ship's own convention (the CANTINA's backdrop Y is ShipLayout's y1). A renderer measuring
            // its own rectangle for a room Core carved is §13.15's second cause.
            if (Math.Abs(bd.X - hall.X0) > 0.01f
                || Math.Abs(bd.Y - hall.Y1) > 0.01f
                || Math.Abs(bd.W - (hall.X1 - hall.X0)) > 0.01f
                || Math.Abs(bd.H - (hall.Y1 - hall.Y0)) > 0.01f)
            {
                wrong.Add(
                    $"  {body} B{-level}: art drawn at ({bd.X:F2},{bd.Y:F2}) {bd.W:F2}×{bd.H:F2} over a hall "
                    + $"of ({hall.X0:F2},{hall.Y1:F2}) {hall.X1 - hall.X0:F2}×{hall.Y1 - hall.Y0:F2}.");
            }

            if (bd.W <= 0 || bd.H <= 0)
            {
                wrong.Add($"  {body} B{-level}: the backdrop has no area — it would draw nothing at all.");
            }

            if (Math.Abs(bd.Alpha - UndergroundComplex.HallArtAlpha) > 0.001f)
            {
                wrong.Add($"  {body} B{-level}: alpha {bd.Alpha}, and the building says {UndergroundComplex.HallArtAlpha}.");
            }

            painted++;

            // The negative control, on the same site: an ordinary floor with no hall wears nothing. Without
            // this, a renderer that painted EVERY floor would pass everything above.
            DeckPlan lobby = DeckFor(body, -2);
            if (lobby.Backdrops.Length != 0)
            {
                wrong.Add($"  {body} B2: {lobby.Backdrops.Length} backdrop(s) on a floor with no hall on it.");
            }
        }

        Assert.True(painted >= 5, $"only {painted} painted halls in the sweep — this guard swept nothing.");
        Assert.True(headOffices >= 1, "the sweep never met a head office, so its bare-floor arm proved nothing.");
        Assert.True(wrong.Count == 0, $"the hall's art is wrong:{Environment.NewLine}{string.Join(Environment.NewLine, wrong)}");
    }

    [Fact]
    public void TheRendererActuallyDrawsIt_UnderEverythingElse()
    {
        // The RENDERER half — "the plan has to flag it AND the renderer has to honour the flag", the house's
        // own third-idiom law. A backdrop in the plan that DeckView never reached would be a green test over
        // the very bare grid the owner reported.
        DeckPlan deck = DeckFor("europa", UndergroundComplex.TopPressurisedFloor("europa")!.Value);
        List<Mark> marks = Frame(deck);

        List<Mark> images = [.. marks.Where(m => m.Kind == "image")];
        Assert.True(images.Count == 1, $"{images.Count} image(s) drawn on the hall floor, wanted exactly 1.");
        Assert.Equal(UndergroundComplex.CantinaHallArtUrl, images[0].Url);
        Assert.True(images[0].W > 0 && images[0].H > 0, "the art was drawn with no area.");
        Assert.True(Math.Abs(images[0].Alpha - UndergroundComplex.HallArtAlpha) < 0.001f,
            $"drawn at alpha {images[0].Alpha}.");

        // LEGIBILITY IS THE WHOLE CONSTRAINT. The list is in draw order, so "the picture is under the room"
        // is simply: no wall, no plate and no console dot was laid down before it.
        int firstImage = marks.FindIndex(m => m.Kind == "image");
        int firstStroke = marks.FindIndex(m => m.Kind is "polyline" or "text" or "circle");
        Assert.True(firstStroke > firstImage,
            $"the first vector mark landed at {firstStroke} and the art at {firstImage} — the art is drawing OVER the room.");

        // And the room is still there to be drawn over: a frame with nothing in it would pass the line above.
        Assert.True(marks.Count(m => m.Kind == "polyline") > 20,
            "the hall frame has almost no strokes in it — this guard is measuring an empty screen.");

        // Negative control: an ordinary floor draws no picture at all.
        Assert.DoesNotContain(Frame(DeckFor("europa", -2)), m => m.Kind == "image");
    }

    // ── The recording pen ───────────────────────────────────────────────────────────────────────────────
    //
    // The house's own RecordingRenderer idiom (TheHallsAreDrawnInAThirdIdiomTests), widened to keep the url,
    // the size and the alpha — because "an image was drawn" is not the claim being made here. Which picture,
    // how big and how opaque are the three things this issue is about.
    private sealed record Mark(string Kind, float X, float Y, string? Url, float W, float H, float Alpha);

    private sealed class RecordingRenderer : IRenderer
    {
        public List<Mark> Marks { get; } = [];

        private readonly List<string> _images = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Marks.Clear();

        public void EndFrame()
        {
        }

        public int RegisterImage(string url)
        {
            int at = _images.IndexOf(url);
            if (at < 0)
            {
                _images.Add(url);
                at = _images.Count - 1;
            }
            return at + 1;
        }

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new("circle", x, y, null, r, r, 1f));

        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new("polyline", pts.Length > 0 ? pts[0] : 0f, pts.Length > 1 ? pts[1] : 0f, null, 0f, 0f, 1f));

        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new("polygon", pts.Length > 0 ? pts[0] : 0f, pts.Length > 1 ? pts[1] : 0f, null, 0f, 0f, 1f));

        public void DrawText(
            float x, float y, string text, RgbaColor c,
            string font = "12px sans-serif", TextAlign align = TextAlign.Left) =>
            Marks.Add(new("text", x, y, text, 0f, 0f, 1f));

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) =>
            Marks.Add(new("image", x, y, UrlOf(id), w, h, a));

        public void DrawImageSlice(
            int id, float sx, float sy, float sw, float sh, float dx, float dy, float dw, float dh, float a = 1f) =>
            Marks.Add(new("slice", dx, dy, UrlOf(id), dw, dh, a));

        private string? UrlOf(int id) => id >= 1 && id <= _images.Count ? _images[id - 1] : null;
    }

    private static List<Mark> Frame(DeckPlan plan)
    {
        (double ax, double ay) = HiveInterior.SpawnOn(Field);
        var pen = new RecordingRenderer();
        new DeckView(pen).Draw(
            plan, WidthPx, HeightPx, 0,
            new DeckView.State(ax, ay, 0, 0, 0, ShuttleAway: false, ElectricUniverse: false, Dark: false),
            0, 0, null);
        return pen.Marks;
    }
}
