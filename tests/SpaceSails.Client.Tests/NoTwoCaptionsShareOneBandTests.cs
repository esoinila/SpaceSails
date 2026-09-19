using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1218 · <b>NO TWO CAPTIONS SHARE ONE BAND.</b> A rect-intersection sweep over every line of text
/// <see cref="DeckView.Draw"/> lays, at four fixed boots — the regolith with your own ✗ on it, a derelict's
/// DEEP HOLD, the ship's own deck, and B1's park.
///
/// <para><b>Why a sweep and not four assertions.</b> The owner filed this from play, three times in two days,
/// on three different floors: <i>"yours · something walks near it"</i> printed through <i>"🗺 DIG AT THE X"</i>
/// at his own cache; five captions and a sentry's drum plate stacked into one unreadable pile in the DEEP
/// HOLD of <i>SECOND MARRIAGE</i>; and in the park <i>A STEEL BENCH — SIT DOWN</i> over
/// <i>SLOP · FOOD WASTE ONLY · NO TRAYS</i>. Every one of them was a DIFFERENT pair of drawers, and each
/// would have needed its own assertion that only somebody who had already seen the bug would think to write.
/// A sweep needs nobody to have seen it: it reads every caption the frame actually laid and asks whether any
/// two of them are in the same place.</para>
///
/// <para><b>The pen measures, it does not trust.</b> Rects come off the real
/// <see cref="IRenderer.DrawText"/> calls — the baseline the renderer asked for, the font it asked for, the
/// alignment it asked for — and are measured through <see cref="CommsBand.WidthOf(string, double)"/>, the
/// same monospace arithmetic the comms band's own plate is cut to. The recording is at the PEN, which is the
/// one place every caption in the game goes through; instrumenting call sites would have measured only the
/// call sites somebody remembered.</para>
///
/// <para><b>It can fail, and it was watched failing.</b> Reverted onto the four shipped literals
/// (<c>sy - 12</c>, <c>sy - 10</c>, <c>dy - 0.9f*scale</c>, <c>ay - 1.6f*scale</c>) this sweep names the
/// pile-ups the owner photographed, the sentry's drum plate among them. The report is in the PR body.</para>
///
/// <para><b>And it cannot pass vacuously.</b> A world that drew no captions would otherwise sail through an
/// intersection sweep with nothing to intersect, so every world states the fewest captions it must lay, and
/// the strings themselves are pinned (<c>Ledgers/Captions.ledger.txt</c>) — this lane moves where a caption
/// sits and may never move what one SAYS.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class NoTwoCaptionsShareOneBandTests
{
    private const int WidthPx = 1200, HeightPx = 700;

    /// <summary>The ledger this suite's pins live in — <c>Ledgers/Captions.ledger.txt</c>.</summary>
    internal const string Suite = "Captions";

    private const string StringsProbe = "caption strings";

    /// <summary>What the ledger's own header says about where these numbers came from.</summary>
    internal const string Preamble =
        "EVERY CAPTION DeckView.Draw LAYS AT FOUR FIXED BOOTS — the SET of strings, sorted, hashed.\n"
        + "#1218's lane moves where a caption sits; it may never move what one says. A row is\n"
        + "`<count> caption(s), sha256 <digest of the sorted strings>`, so a caption that changed its\n"
        + "WORDS is red here even though the sweep beside it is still green.";

    // ── THE INK BOX OF A LINE OF MONOSPACE ────────────────────────────────────────────────────────────
    //
    // Canvas draws from the ALPHABETIC BASELINE, so a caption's box is the em split either side of the y the
    // renderer was handed. Read from MarkBand rather than typed here, and that is deliberate rather than
    // tidy: the guard and the seater have to agree about what "occupied" MEANS, or one of them is measuring a
    // rectangle the other never reserved. What this sweep proves is not that the arithmetic is right — it is
    // that every caption on the deck went THROUGH that arithmetic, which is exactly what the five hand-typed
    // lifts did not.

    private const double CapHeightRatio = MarkBand.CapHeightRatio;
    private const double DescenderRatio = MarkBand.DescenderRatio;

    /// <summary>One line of text the frame laid, and the rectangle its ink occupies.</summary>
    internal readonly record struct Caption(string Text, float X, float Y, double Px, TextAlign Align)
    {
        public double Width => CommsBand.WidthOf(Text, Px);

        public double Left => Align switch
        {
            TextAlign.Center => X - (Width / 2),
            TextAlign.Right => X - Width,
            _ => X,
        };

        public double Right => Left + Width;

        public double Top => Y - (Px * CapHeightRatio);

        public double Bottom => Y + (Px * DescenderRatio);

        public bool Meets(in Caption other) =>
            Left < other.Right && other.Left < Right && Top < other.Bottom && other.Top < Bottom;

        public string Where => string.Create(
            CultureInfo.InvariantCulture,
            $"\"{Text}\" at ({X:0.#}, {Y:0.#}) · {Px:0.#}px · x {Left:0.#}…{Right:0.#} y {Top:0.#}…{Bottom:0.#}");
    }

    /// <summary>
    /// THE PEN THAT KEEPS THE CAPTIONS. Every primitive but text is dropped on the floor: this guard is about
    /// what the frame SAYS and where it says it, and a pen that also kept nine hundred polylines would be a
    /// pen whose failures nobody could read.
    /// </summary>
    private sealed class CaptionPen : IRenderer
    {
        private readonly List<Caption> _said = [];

        public IReadOnlyList<Caption> Said => _said;

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => _said.Clear();

        public void EndFrame()
        {
        }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f)
        {
        }

        public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float w = 1f)
        {
        }

        public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float w = 1f)
        {
        }

        public void DrawText(float x, float y, string text, RgbaColor color,
                             string font = "12px sans-serif", TextAlign align = TextAlign.Left)
        {
            if (!string.IsNullOrEmpty(text))
            {
                _said.Add(new Caption(text, x, y, PixelsOf(font), align));
            }
        }

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f)
        {
        }

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f)
        {
        }

        /// <summary>The declared size out of a CSS font string — <c>"bold 10px monospace"</c> is ten.</summary>
        private static double PixelsOf(string font)
        {
            int px = font.IndexOf("px", StringComparison.Ordinal);
            if (px <= 0)
            {
                return 12.0;
            }
            int from = px;
            while (from > 0 && (char.IsAsciiDigit(font[from - 1]) || font[from - 1] == '.'))
            {
                from--;
            }
            return double.TryParse(font[from..px], NumberStyles.Float, CultureInfo.InvariantCulture, out double n)
                ? n
                : 12.0;
        }
    }

    // ── THE FOUR BOOTS ────────────────────────────────────────────────────────────────────────────────

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>One world, the captain's place in it, and the fewest captions it must lay for the sweep over
    /// it to have proved anything.</summary>
    private sealed record Boot(string Name, DeckPlan Plan, DeckView.State State, double SimTime,
        DeckView.SurfaceHud? Surface, int AtLeast);

    /// <summary>#1218 · YOUR OWN ✗, AND THE CONSOLE THAT IS THE SAME POINT AS IT. MoonSurface.Layout seeds a
    /// <c>DigSite</c> console at every own cache's own <c>(x, y)</c>, so the forensic line over a haunted mark
    /// and the console's plate are one anchor BY CONSTRUCTION and not by an accident of where you buried.
    /// </summary>
    private const double CacheX = 4.0, CacheY = -19.5;

    /// <summary>The park's own gate spot — where <c>?park=1</c> stands a tester (Map.Surface.Cheats.Stand),
    /// asked of Core rather than typed, so this boot is the floor the owner was standing on.</summary>
    private static (double X, double Y) TheParkGate()
    {
        UndergroundComplex.Park green = UndergroundComplex.Build("luna", -1, Field).Park
            ?? throw new InvalidOperationException("luna B1 has no park — this boot proves nothing.");
        return (green.X, green.Y);
    }

    private static IEnumerable<Boot> Boots()
    {
        // ── THE REGOLITH, WITH YOUR OWN HAUNTED ✗ ON IT ───────────────────────────────────────────────
        LandingSite site = LandingSites.At("luna", 0);
        DeckPlan ground = MoonSurface.SurfaceDeck(
            "luna", "Luna", [("mine-1", CacheX, CacheY, 2)],
            droidCount: 0, fillDroids: static (_, _) => { },
            siteSalt: site.LayoutSalt, siteName: site.Name);

        yield return new Boot(
            "the regolith · your own ✗",
            ground,
            new DeckView.State(CacheX, CacheY + 1.0, 1.4, 0, 0,
                ShuttleAway: false, ElectricUniverse: false,
                Nerve: 58, NerveReadout: "FRAYED", ShowNerve: true, HitsTaken: 1),
            SimTime: 4321.0,
            new DeckView.SurfaceHud(
                DigProgress: 0.42,
                HasDroppedChest: true, DropX: CacheX + 0.4, DropY: CacheY - 0.4,
                Blips: [(0.3, 12.0, false)],
                Cadence: 3,
                Readout: "CONTACT · 12 m",
                CacheMarks: [(CacheX, CacheY, true)],
                Nerve: 58,
                NerveReadout: "FRAYED",
                Instruments: true,
                KeyHints: "[T] deploy ∙ [G] drop",
                OrbitComms: "ORBIT HOLDING · 41 min",
                OrbitSeverity: 1,
                SweptSquares: [(CacheX + 2, CacheY, false)],
                StandingPrompt: "BURY THE CHEST — [G]",
                ChannelGlyph: "⛏",
                AirSeconds: 240,
                AirDistanceHome: 55,
                FanReach: 40),
            AtLeast: 6);

        // ── A DERELICT'S DEEP HOLD, WITH TWO SENTRIES SET DOWN IN IT ──────────────────────────────────
        //
        // The owner's own frame: the hold's room name, its landmark line, the column's plate, the cargo's
        // plate and two drum readouts, all inside a couple of deck units of each other. The hull is the
        // INFESTED cause because that is the one whose nest is in this room, and the node is aboard because
        // that is what puts the SUBSTRATE SPAR line over the column.
        var hull = new Derelict.Wreck("second-marriage", "Second Marriage", Derelict.WreckCause.Infested,
            250_000, 40.0);
        DeckPlan hold = WreckInterior.WreckDeck(
            hull, new HashSet<string>(StringComparer.Ordinal), salvaged: false,
            droidCount: 0, fillDroids: static (_, _) => { }, archiveAboard: true);

        (double hx, double hy) = (WreckLayout.ArchiveStation.X, WreckLayout.ArchiveStation.Y);

        yield return new Boot(
            "a derelict · the DEEP HOLD",
            hold,
            new DeckView.State(hx, hy + 1.2, 4.7, 0, 0,
                ShuttleAway: false, ElectricUniverse: false),
            SimTime: 777.0,
            new DeckView.SurfaceHud(
                DigProgress: -1,
                HasDroppedChest: false, DropX: 0, DropY: 0,
                Blips: [],
                Cadence: 0,
                Readout: "",
                CacheMarks: [],
                Nerve: 44,
                NerveReadout: "FRAYED",
                Instruments: false,
                Bots:
                [
                    (hx - 1.5, hy + 0.4, "99", false, false, 0.0, 0.0),
                    (hx + 2.0, hy - 0.6, "77", false, false, 0.0, 0.0),
                ],
                KeyHints: "[T] deploy ∙ [E] work",
                AirSeconds: 300,
                AirDistanceHome: 30,
                TrackerPlace: "DEEP HOLD",
                AirSupply: SuitAir.Supply.Tanks),
            AtLeast: 6);

        // ── HER OWN DECK ──────────────────────────────────────────────────────────────────────────────
        yield return new Boot(
            "the ship · her own deck",
            Scenes.Build("ship"),
            new DeckView.State(-2.5, 1.5, 0.7, CargoUnits: 7, Charge: 0.62,
                ShuttleAway: false, ElectricUniverse: true,
                ShowNerve: true, NerveCompact: true, Nerve: 71, NerveReadout: "STEADY"),
            SimTime: 1234.5,
            Surface: null,
            AtLeast: 10);

        // ── B1'S PARK, AT THE GATE ────────────────────────────────────────────────────────────────────
        (double px, double py) = TheParkGate();
        yield return new Boot(
            "luna B1 · the park",
            HiveInterior.FloorDeck("luna", -1, Field, 0, static (_, _) => { }, [], 0),
            new DeckView.State(px, py, 0.6, 0, 0, ShuttleAway: false, ElectricUniverse: false,
                ShowNerve: true, NerveCompact: true, Nerve: 80, NerveReadout: "STEADY"),
            SimTime: 880.0,
            Surface: null,
            AtLeast: 8);
    }

    private static IReadOnlyList<Caption> CaptionsOf(Boot boot)
    {
        var pen = new CaptionPen();
        DeckView.State state = boot.State;
        new DeckView(pen).Draw(boot.Plan, WidthPx, HeightPx, boot.SimTime, in state,
            0, 0, boot.Surface, null, false);
        return pen.Said;
    }

    // ── THE EXEMPTIONS, AND THE ARGUMENT FOR EACH ─────────────────────────────────────────────────────
    //
    // Two, and each is an overlay somebody DECIDED on rather than two authors colliding.
    //
    // 1 · A MARK IS NOT A CAPTION (MarkBand.IsAMark). One glyph on the ground — the ✗ at a cache, the × on a
    //     husk, the · of a swept square or a movement echo, the 🧰 of a dropped chest, a lifeboat cradle's ▮ —
    //     stands FOR a thing instead of naming one. It is the BODY, and a caption drawn over a body is the
    //     entire idiom of this deck plan; the swept grid is even drawn first on purpose, "so every other
    //     ground mark sits on top" (MarkTheGround's own banner). Exempting it is not letting a pile-up
    //     through: the words over these marks are seated in the band book like every other caption, and a
    //     glyph that jumped a row to avoid the ground under it would be the picture moving the world.
    //
    // 2 · THE SHIP'S OWN TOP-CENTRE BAND (SpaceSails.Core.CommsBand, the #324/#986 F2 law). The orbit line
    //     and the stall banner are chrome at a DECLARED reservation, drawn last, on their own opaque plate —
    //     the plate exists precisely so "never buried" is true of them. A world label that projects up there
    //     is COVERED by an instrument that has the floor, which is a different thing from two captions
    //     competing for one row, and moving the world's labels out of it would be re-laying every deck in the
    //     game to answer a bug nobody has reported. Named here so it is a decision and not an oversight; the
    //     honest follow-up is to extend CommsBand's reserve to world captions the way it already covers the
    //     sentries, and that is its own lane.
    //
    // Everything else is a pile-up, a plate over a room's name included: a room's name is what the room is
    // CALLED, and a caption that buries it has taken the floor's one landmark away.

    private static bool InTheShipsOwnBand(in Caption c) => c.Bottom <= CommsBand.ReservedBottom;

    private static bool AnIntentionalOverlay(in Caption a, in Caption b) =>
        MarkBand.IsAMark(a.Text) || MarkBand.IsAMark(b.Text)
        || InTheShipsOwnBand(in a) || InTheShipsOwnBand(in b);

    /// <summary>
    /// ZERO CAPTIONS LAND ON TOP OF ANOTHER CAPTION, in any of the four worlds.
    /// </summary>
    [Fact]
    public void NoCaptionIsDrawnOverAnother()
    {
        var pileUps = new List<string>();
        int worlds = 0, captions = 0;

        foreach (Boot boot in Boots())
        {
            worlds++;
            IReadOnlyList<Caption> said = CaptionsOf(boot);
            captions += said.Count;

            // The anti-vacuity clause, and it fires FIRST: an empty caption list has nothing to intersect
            // and would pass this sweep for ever.
            Assert.True(said.Count >= boot.AtLeast,
                $"'{boot.Name}' laid only {said.Count} caption(s) — fewer than the {boot.AtLeast} this world "
                + "must say, so the sweep over it proves nothing.");

            for (int i = 0; i < said.Count; i++)
            {
                for (int j = i + 1; j < said.Count; j++)
                {
                    Caption a = said[i], b = said[j];
                    if (!a.Meets(in b) || AnIntentionalOverlay(in a, in b))
                    {
                        continue;
                    }
                    pileUps.Add($"  {boot.Name}{Environment.NewLine}      {a.Where}"
                        + $"{Environment.NewLine}      {b.Where}");
                }
            }
        }

        Assert.True(pileUps.Count == 0,
            $"{pileUps.Count} pair(s) of captions are drawn on top of each other:"
            + Environment.NewLine + string.Join(Environment.NewLine, pileUps)
            + Environment.NewLine + Environment.NewLine
            + "#1218's ruling: a mark's second line FOLDS INTO ITS PLATE — one plate per anchor, the plate "
            + "grows a second row. Never a second caption at a second hand-typed lift.");

        Assert.Equal(4, worlds);
        Assert.True(captions > 40, $"only {captions} caption(s) were laid in all — this sweep proves little.");
    }

    // ── AND THE WORDS THEMSELVES DID NOT MOVE ─────────────────────────────────────────────────────────

    /// <summary>Every caption drawn, as ledger rows — the set of strings per world, sorted and hashed.</summary>
    internal static IReadOnlyList<PinLedger.Row> MeasureEveryRow()
    {
        var rows = new List<PinLedger.Row>();
        foreach (Boot boot in Boots())
        {
            IReadOnlyList<Caption> said = CaptionsOf(boot);
            string[] words = [.. said.Select(c => c.Text).OrderBy(t => t, StringComparer.Ordinal)];
            byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", words)));
            rows.Add(new PinLedger.Row(StringsProbe, boot.Name,
                string.Create(CultureInfo.InvariantCulture,
                    $"{words.Length} caption(s), sha256 {Convert.ToHexString(digest).ToLowerInvariant()}")));
        }
        return rows;
    }

    /// <summary>
    /// THE SET OF CAPTION STRINGS PER WORLD IS UNCHANGED — the clause that keeps a placement lane out of the
    /// prose. Player-facing words are the owner's; this lane may move a caption's ROW and nothing else.
    /// </summary>
    [Fact]
    public void EveryWorldSaysExactlyWhatItSaidBefore()
    {
        IReadOnlyDictionary<string, PinLedger.Row> pinned = PinLedger.Pinned(Suite);
        var wrong = new List<string>();
        var fresh = new List<string>();

        foreach (PinLedger.Row row in MeasureEveryRow())
        {
            if (!pinned.TryGetValue(row.Key, out PinLedger.Row was))
            {
                fresh.Add("  " + row);
                continue;
            }
            if (!string.Equals(was.Value, row.Value, StringComparison.Ordinal))
            {
                wrong.Add($"  {row.Scene}{Environment.NewLine}      was: {was.Value}"
                    + $"{Environment.NewLine}      now: {row.Value}");
            }
        }

        Assert.True(fresh.Count == 0,
            $"{fresh.Count} world(s) have no pin in {Suite}.ledger.txt — a ledger row is measured, never "
            + $"typed:{Environment.NewLine}  {PinLedger.Invocation}{Environment.NewLine}"
            + string.Join(Environment.NewLine, fresh));
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} world(s) SAY something different than they did:"
            + Environment.NewLine + string.Join(Environment.NewLine, wrong)
            + Environment.NewLine + Environment.NewLine
            + "Caption text is player-facing prose and is not this lane's to move. If the change is "
            + $"intended and owner-sanctioned, re-pin BY MEASUREMENT:{Environment.NewLine}  "
            + PinLedger.Invocation);

        Assert.Equal(4, pinned.Count);
    }
}
