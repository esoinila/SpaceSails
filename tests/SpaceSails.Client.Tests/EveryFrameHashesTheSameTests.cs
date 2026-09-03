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
/// #870 lane 7b · THE FRAME IS PINNED — every mark <see cref="DeckView.Draw"/> lays, in the order it lays
/// them, hashed and written down.
///
/// <para>Lane 7 splits methods rather than moving them, and a method that draws cannot be proved pure by
/// counting its members: the whole content of <c>Draw</c> is <b>which marks, in which order</b> — order IS
/// the picture, because a wall drawn after a room label paints over the label. So the guard is a SNAPSHOT,
/// taken on the old code before a line of it moved: a transcribing pen sits where the canvas sits, every
/// call is written down whole (every coordinate at round-trip precision, every ink, every font, every
/// string), the transcript is sha256'd, and the digest is pinned here per case.</para>
///
/// <para><b>What a hash proves that a test cannot.</b> A hand-written assertion says "the bench is drawn":
/// it is blind to the four hundred marks either side of it, and blind to the order. This says the frame is
/// the frame — one mark moved, one pass reordered, one colour off by an alpha step, and the digest is a
/// different number. It is the only assertion that can watch a thousand-line method be taken apart.</para>
///
/// <para><b>Determinism.</b> Every case is drawn at a FIXED <c>simTime</c> and a fixed canteen watch, so the
/// breathing prompts, the reactor throb, the fan's pulse and the magazine pop all land on one phase; the
/// figures on the two peopled cases are planted by a fixed filler rather than by a schedule. Nothing here
/// reads a clock or a seed the caller does not hand it. Every value that reaches the pen is a <c>float</c>
/// (<see cref="IRenderer"/> takes nothing wider), so a platform's last-bit difference in
/// <c>Math.Sin</c>/<c>Cos</c>/<c>Atan2</c> is lost in the narrowing from double long before it can reach the
/// transcript — which is why one digest can be pinned for Windows and for the Linux runner alike.</para>
///
/// <para><b>This file was committed alone, ahead of the split</b> — every digest below was taken on the
/// 1,058-line <c>Draw</c> exactly as it shipped, so there was no chance of pinning what the new code
/// happens to do.</para>
///
/// <para><b>Proven able to fail.</b> Two passes were swapped in the conductor — the doors drawn before the
/// walls instead of after — and 32 of the 33 cases went red:</para>
/// <code>
/// 32 case(s) draw a different frame than the one pinned on the old code:
///   ship · under way — 335 call(s), sha256 c2a11fc5f12e8d9ca5ac1718090f1929ab48d5d68a10d839168a8e19f52a905b
///       pinned 335 call(s), sha256 84f477c2696097c1d9f85a65a9a5969d549d108741bc97113a1897553440ec4c
///   luna B1 — 1591 call(s), sha256 cce9a1fe2bd3df44ea9003076b0d17e83dd1b737919f04f6eff999a6e928fef2
///       pinned 1591 call(s), sha256 6ed0aaccf49a5d11ac851766f70f176ffb5da2ced290023149e7eddcabf0b4f4
/// </code>
/// <para>The one that stayed green is <c>luna B1 · the lamps are all there is</c>, and it is honest that it
/// did: on a floor lit only by the suit lamp the mask discards the marks BOTH those passes lay, so swapping
/// them really does draw the same frame. It is the reason there is a lit case for every dark one.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class EveryFrameHashesTheSameTests
{
    private const int WidthPx = 1200, HeightPx = 700;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    // ── THE PEN THAT WRITES EVERYTHING DOWN ───────────────────────────────────────────────────────────
    //
    // Not the recording pen the other deck guards use — those keep a shortlist of the marks they care about.
    // This one keeps the CALL, whole and in order: the primitive, every coordinate at "R" (round-trip, so
    // two floats that differ anywhere in their mantissa transcribe differently), the fill, the stroke, the
    // width, the font, the alignment and the text. There is nothing about a frame it cannot see.

    private sealed class TranscribingPen : IRenderer
    {
        private readonly StringBuilder _log = new();
        private readonly Dictionary<string, int> _images = new(StringComparer.Ordinal);

        public int Calls { get; private set; }

        public string Transcript => _log.ToString();

        private static string F(float v) => v.ToString("R", CultureInfo.InvariantCulture);

        private static string C(RgbaColor c) =>
            string.Create(CultureInfo.InvariantCulture, $"{c.R},{c.G},{c.B},{c.A}");

        private static string C(RgbaColor? c) => c is { } k ? C(k) : "-";

        private static string Pts(ReadOnlySpan<float> xy)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < xy.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                sb.Append(F(xy[i]));
            }
            return sb.ToString();
        }

        private void Say(string line)
        {
            _log.Append(line).Append('\n');
            Calls++;
        }

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) =>
            Say($"begin {widthPx} {heightPx} {C(background)}");

        public void EndFrame() => Say("end");

        public int RegisterImage(string url)
        {
            if (!_images.TryGetValue(url, out int id))
            {
                id = _images.Count + 1;
                _images[url] = id;
            }
            Say($"register {id} {url}");
            return id;
        }

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Say($"circle {F(x)} {F(y)} {F(r)} {C(fill)} {C(stroke)} {F(w)}");

        public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float w = 1f) =>
            Say($"polyline {C(stroke)} {F(w)} [{Pts(pointsXY)}]");

        public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Say($"polygon {C(fill)} {C(stroke)} {F(w)} [{Pts(pointsXY)}]");

        public void DrawText(float x, float y, string text, RgbaColor color,
                             string font = "12px sans-serif", TextAlign align = TextAlign.Left) =>
            Say($"text {F(x)} {F(y)} {C(color)} {align} <{font}> \"{text}\"");

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) =>
            Say($"image {id} {F(x)} {F(y)} {F(w)} {F(h)} {F(a)}");

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) =>
            Say($"slice {id} {F(sx)} {F(sy)} {F(sw)} {F(sh)} {F(x)} {F(y)} {F(w)} {F(h)} {F(a)}");
    }

    // ── THE WORLDS, AND WHAT THE CAPTAIN IS DOING IN THEM ──────────────────────────────────────────────

    /// <summary>One frame's worth of arguments: exactly what the page hands the pen on a real tick.</summary>
    private sealed record Shot(
        DeckPlan Plan, DeckView.State State, double SimTime,
        double PanX = 0, double PanY = 0, DeckView.SurfaceHud? Surface = null,
        double? NpcHoldTime = null, bool CrewGlance = false);

    /// <summary>The everything hud — every optional list on the surface overlay filled, so no branch of the
    /// excursion's half of the frame goes unwatched. Its numbers are arbitrary and fixed; what matters is
    /// that they are the SAME numbers before and after the split.</summary>
    private static DeckView.SurfaceHud TheWholeExcursion(bool instruments = true) =>
        new(
            DigProgress: 0.42,
            HasDroppedChest: true, DropX: 6.0, DropY: -3.0,
            Blips: [(0.3, 12.0, false), (2.1, 30.0, true), (-1.2, 46.0, false)],
            Cadence: 3,
            Readout: "CONTACT · 12 m",
            CacheMarks: [(-4.0, 2.0, false), (9.0, -6.0, true)],
            Nerve: 62.0,
            NerveReadout: "STEADY",
            Instruments: instruments,
            Smudges: [(3.0, 4.0, 2.5), (-8.0, -5.0, 4.0)],
            Ghosts: [(-7.0, 1.0, 0.6)],
            Countdown: (2.0, 2.0, "07"),
            Bots:
            [
                (5.0, 5.0, "23", false, true, 8.0, 9.0),
                (-5.0, 5.0, "08", false, false, 0.0, 0.0),
                (-5.0, -5.0, "00", true, false, 0.0, 0.0),
            ],
            Husks: [(1.0, -8.0)],
            KeyHints: "[T] deploy ∙ [G] drop",
            OrbitComms: "ORBIT HOLDING · 41 min",
            OrbitSeverity: 1,
            CommsState: 1,
            TrackerCaptions: ["dig here", "sentry ready"],
            SweptSquares: [(2.0, 2.0, false), (3.0, 3.0, true)],
            DarkRegions: [(-10.0, -10.0, -2.0, -2.0, 0), (2.0, 2.0, 10.0, 10.0, 1)],
            Echoes: [(0.0, 5.0, 0.4)],
            StandingPrompt: "BURY THE CHEST — [G]",
            BloodSplash: 0.55,
            Beacons: [(1.0, 20.0, true, false, false), (2.5, 33.0, false, true, false)],
            CacheBeacons: [(2.0, 15.0)],
            Rumours: [(3.0, 25.0, 0.4)],
            AirSeconds: 240,
            AirDistanceHome: 55,
            ChannelGlyph: "⛏",
            ChannelIsAid: false,
            FanReach: 40,
            TrackerPlace: "B14 · ARCHIVE",
            AirSupply: SuitAir.Supply.Tanks);

    /// <summary>#804/#832/#583/#538/#424 · a deck with one of EVERY kind of figure on it, planted rather
    /// than scheduled so the frame is the same one every run: a guard on his round, an Old One, a repo crew
    /// on foot, a sweeper carrying a lamp, two working crew (who catch each other's eye), a held figure and
    /// a smeared one at the far end of the eye.</summary>
    private static Action<double, DeckPlan.Droid[]> Everybody(double x, double y) =>
        (_, into) =>
        {
            into[0] = new DeckPlan.Droid(x + 3, y + 2, 0.4, "PATROL 2");
            into[1] = new DeckPlan.Droid(x - 4, y + 1, 2.1, "Reever");
            into[2] = new DeckPlan.Droid(x + 5, y - 3, 1.0, "Collector");
            into[3] = new DeckPlan.Droid(x - 6, y - 2, 3.0, "SWEEP-1");
            into[4] = new DeckPlan.Droid(x + 1, y + 6, 5.2, "Barkeep");
            into[5] = new DeckPlan.Droid(x - 2, y + 7, 0.9, "Dock-hand");
            into[6] = new DeckPlan.Droid(x + 7, y + 1, 0.9, "Silas", Held: true);
            into[7] = new DeckPlan.Droid(x - 9, y + 4, 2.6, "PATROL 3", Smeared: true);
        };

    private static DeckPlan HiveFloor(string body, int level, long watch = 0) =>
        HiveInterior.FloorDeck(body, level, Field, 0, static (_, _) => { }, [], watch);

    /// <summary>Which floors are drawn. Luna is the head office, so its band is the deepest and widest
    /// vocabulary of floor the generator has — park block, canteen hall, labs, stores, the lot.</summary>
    private static IEnumerable<int> LunaFloors() => UndergroundComplex.FloorsOf("luna");

    public static IEnumerable<string> Names()
    {
        yield return "ship · under way";
        yield return "ship · docked, the shuttle away and the machine stalling";
        yield return "ship · hatches dogged, seated at the table";

        foreach (string id in HavenInterior.InteriorBodyIds)
        {
            yield return $"haven · {id}";
        }

        yield return "wreck · HullBreach";

        foreach (int level in LunaFloors())
        {
            yield return $"luna B{-level}";
        }

        yield return "luna B1 · the lamps are all there is";
        yield return "luna B1 · sat down at a desk";
        yield return "luna B1 · the round is out, and the buzzer went";
        yield return "phobos B1";
        yield return "titan B1";

        yield return "surface · luna site 0";
        yield return "surface · luna site 0, the whole excursion";
        yield return "surface · luna site 0, dark, and the fan hears something";
        yield return "surface · titan site 0, a derelict's instruments (none)";
    }

    private static Shot Set(string name)
    {
        // The ship, the wrecks and the regolith are Scenes' own plans — the very ones every other deck audit
        // walks, so this guard and those cannot come to disagree about what a scene is.
        if (name.StartsWith("ship · ", StringComparison.Ordinal))
        {
            DeckPlan ship = Scenes.Build(
                name.Contains("dogged", StringComparison.Ordinal) ? "ship-all-hatches-dogged" : "ship");
            return name switch
            {
                "ship · under way" => new Shot(
                    ship,
                    new DeckView.State(-2.5, 1.5, 0.7, CargoUnits: 7, Charge: 0.62,
                        ShuttleAway: false, ElectricUniverse: true,
                        ShowNerve: true, NerveCompact: true, Nerve: 71, NerveReadout: "STEADY",
                        HitsTaken: 2, NerveFlash: "it laid hands on you  −1",
                        NerveLedger: ["the dark  −2", "a drink  +1"]),
                    SimTime: 1234.5),
                "ship · docked, the shuttle away and the machine stalling" => new Shot(
                    ship,
                    new DeckView.State(4.0, -3.0, 2.4, CargoUnits: 0, Charge: 0.0,
                        ShuttleAway: true, ElectricUniverse: false, Docked: true,
                        StallBanner: "THE WORLD IS BEHIND — 0.8 s since the last tick"),
                    SimTime: 500.0, PanX: 30, PanY: -18),
                _ => new Shot(
                    ship,
                    new DeckView.State(0.0, 0.0, 1.0, CargoUnits: 3, Charge: 0.2,
                        ShuttleAway: false, ElectricUniverse: false, Seated: true),
                    SimTime: 9000.0, NpcHoldTime: 8888.0),
            };
        }

        if (name.StartsWith("haven · ", StringComparison.Ordinal))
        {
            DeckPlan haven = Scenes.Build($"haven:{name["haven · ".Length..]}");
            return new Shot(
                haven,
                new DeckView.State(haven.SpawnX, haven.SpawnY, 0.3, 0, 0,
                    ShuttleAway: false, ElectricUniverse: false,
                    ShowNerve: true, NerveCompact: true, Nerve: 44, NerveReadout: "FRAYED"),
                SimTime: 2400.0, CrewGlance: true);
        }

        if (name.StartsWith("wreck · ", StringComparison.Ordinal))
        {
            DeckPlan wreck = Scenes.Build($"wreck:{name["wreck · ".Length..]}");
            return new Shot(
                wreck,
                new DeckView.State(wreck.SpawnX, wreck.SpawnY, 1.9, 0, 0,
                    ShuttleAway: false, ElectricUniverse: false),
                SimTime: 777.0);
        }

        if (name.StartsWith("surface · ", StringComparison.Ordinal))
        {
            string body = name.Contains("titan", StringComparison.Ordinal) ? "titan" : "luna";
            DeckPlan ground = Scenes.Build($"surface:{body}:0");
            var stand = new DeckView.State(ground.SpawnX, ground.SpawnY, 0.9, 0, 0,
                ShuttleAway: false, ElectricUniverse: false,
                Nerve: 58, NerveReadout: "FRAYED", ShowNerve: true, HitsTaken: 1,
                Dark: name.Contains("dark", StringComparison.Ordinal));
            return name switch
            {
                "surface · luna site 0" => new Shot(ground, stand, SimTime: 3600.0),
                "surface · titan site 0, a derelict's instruments (none)" => new Shot(
                    ground, stand, SimTime: 61.0, Surface: TheWholeExcursion(instruments: false)),
                _ => new Shot(ground, stand, SimTime: 4321.0, Surface: TheWholeExcursion()),
            };
        }

        // The Hive. A floor is built at watch 0 so the canteen's occupancy is one fixed shift.
        (double sx, double sy) = HiveInterior.SpawnOn(Field);

        if (name == "luna B1 · the round is out, and the buzzer went")
        {
            DeckPlan peopled = HiveInterior.FloorDeck("luna", -1, Field, 8, Everybody(sx, sy), [], 0);
            return new Shot(
                peopled,
                new DeckView.State(sx, sy, 0.0, 0, 0, ShuttleAway: false, ElectricUniverse: false),
                SimTime: 1500.0, NpcHoldTime: 1400.0, CrewGlance: true);
        }

        if (name == "luna B1 · the lamps are all there is")
        {
            return new Shot(
                HiveFloor("luna", -1),
                new DeckView.State(sx, sy, 2.2, 0, 0, ShuttleAway: false, ElectricUniverse: false,
                    Dark: true, ShowNerve: true, NerveCompact: true, Nerve: 30, NerveReadout: "RATTLED"),
                SimTime: 640.0);
        }

        if (name == "luna B1 · sat down at a desk")
        {
            return new Shot(
                HiveFloor("luna", -1),
                new DeckView.State(sx, sy, 1.1, 0, 0, ShuttleAway: false, ElectricUniverse: false,
                    Seated: true, ShowNerve: true, NerveCompact: true, Nerve: 88, NerveReadout: "STEADY"),
                SimTime: 205.0, PanX: -40, PanY: 25);
        }

        string plainBody = name.Split(' ')[0];
        int plainLevel = -int.Parse(name.Split('B')[1], CultureInfo.InvariantCulture);
        return new Shot(
            HiveFloor(plainBody, plainLevel),
            new DeckView.State(sx, sy, 0.6, 0, 0, ShuttleAway: false, ElectricUniverse: false),
            SimTime: 880.0);
    }

    /// <summary>One frame, drawn by the real <see cref="DeckView"/> onto the transcribing pen.</summary>
    private static (int Calls, string Sha) Frame(string name)
    {
        Shot shot = Set(name);
        var pen = new TranscribingPen();
        DeckView.State state = shot.State;
        new DeckView(pen).Draw(
            shot.Plan, WidthPx, HeightPx, shot.SimTime, in state,
            shot.PanX, shot.PanY, shot.Surface, shot.NpcHoldTime, shot.CrewGlance);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(pen.Transcript));
        return (pen.Calls, Convert.ToHexString(digest).ToLowerInvariant());
    }

    // ── THE PINS, TAKEN ON THE OLD CODE ───────────────────────────────────────────────────────────────
    //
    // Captured on 183c614 — the 1,058-line Draw, before a line of it moved — and committed on its own,
    // ahead of the split. A number here is not a preference: it is what the game drew that day. If one
    // changes, either the picture changed or the code that draws it did, and both are the thing this guard
    // exists to notice.
    //
    // #1055 · THE NUMBERS THEMSELVES NOW LIVE IN Ledgers/FrameHashes.ledger.txt, two rows per case —
    // `calls | <case>` and `sha256 | <case>` — machine-written by the re-pin command and never transcribed
    // by hand. Nothing about the assertions below moved; only the pins' home did. The re-pin history that
    // used to sit above the table is kept verbatim underneath, because the arithmetic in it is the reason
    // each number is what it is, and a ledger row cannot carry a paragraph.
    //
    //   TO RE-PIN (runs the measurement, rewrites the ledger, prints the report):
    //     SPACESAILS_REPIN=1 dotnet test tests/SpaceSails.Client.Tests -c Release \
    //       --filter FullyQualifiedName~ThePinsAreRewrittenOnlyWhenAsked \
    //       --logger "console;verbosity=detailed"

    /// <summary>The ledger this suite's pins live in — <c>Ledgers/FrameHashes.ledger.txt</c>.</summary>
    internal const string Suite = "FrameHashes";

    /// <summary>The two things measured per case. A case's marks and its digest move together, but they are
    /// separate rows because a report that says "+23 calls" is a report a person can decompose, and a report
    /// that says only "the hash changed" is not.</summary>
    private const string CallsProbe = "calls", ShaProbe = "sha256";

    /// <summary>What the ledger's own header says about where these numbers came from.</summary>
    internal const string Preamble =
        "EVERY MARK DeckView.Draw LAYS, IN THE ORDER IT LAYS THEM — hashed, per case.\n"
        + "Captured on 183c614, the 1,058-line Draw, before a line of it moved (#870 lane 7b).\n"
        + "`calls` is how many marks the frame laid; `sha256` is the transcript of all of them, whole and in\n"
        + "order, at round-trip precision. The re-pin history — which lane moved which rows and by what\n"
        + "arithmetic — is in the class docs of EveryFrameHashesTheSameTests.";

    // ── #758 · THE FIVE HALL FLOORS WERE RE-PINNED, AND THE CALL COUNTS PROVE WHY ─────────────────────
    //
    // The cabinets carry a privacy glyph on their plate now (CabinetPrivacy.PlateFor), so the three floors
    // in this list that have a CANTEEN HALL in them — luna B1 in its three states, phobos B1, titan B1 —
    // draw a different STRING at the same coordinate. Every one of the five kept its call count to the mark
    // (1591 / 1607 / 1617 / 1563 / 1512, unchanged); nothing was added to the frame, moved in it or taken
    // out of it, and every other row in this table is byte-identical. That is the whole of the change, and
    // it is what a re-pin is allowed to look like.

    // ── #561 · THE FOURTEEN FRAMES WITH A NERVE GAUGE IN THEM WERE RE-PINNED, AND THE COUNTS PROVE WHY ──
    //
    // The gauge's backing plate is measured to what it backs now (HudColumn) instead of typed at h + 42, so
    // it is drawn 18px taller on the regolith and 15px taller aboard; and the motion tracker's column top is
    // ASKED of the nerve block instead of typed at 82, so on the surface frames the whole fan steps 18px
    // down the column. Both are the SAME rectangle and the SAME disc, at a different y.
    //
    // Every one of the fourteen kept its call count to the mark — 335 / 346 / 350 / 350 / 344 / 348 / 346 /
    // 350 / 128 / 1607 / 3771 / 3881 / 266 / 4364, unchanged — and the fourteen are exactly the cases whose
    // State carries ShowNerve. Nothing was added to a frame, taken out of one, or moved in one; every other
    // row in this table is byte-identical. That is what a re-pin is allowed to look like.

    // #973 L5b · SEVEN HAVEN ROWS RE-PINNED, and they are the only rows that moved. Every docked bar grew a
    // console the room never had — a top the captain can take (`DeckPlan.ConsoleKind.BarTop`) on each of its
    // tops that the rota has not already given to somebody — so each of those frames draws a handful more pen
    // calls than it did and nothing else about any of them changed. Six to eight more calls per haven, which
    // is exactly the number of free tops that bar has this watch, and not one ship, wreck, hive or park row
    // moved at all. That is what a re-pin is allowed to look like.

    // ── #973 L4 · THE SAME SEVEN ROWS, RE-PINNED AGAIN ON TOP OF L5b, AND THE COUNTS PROVE WHY ────────
    //
    // Three NEBULA MUTUAL wall plates are hung in every docked station's CONCOURSE (`StationAds`, hung in
    // HavenInterior beside the PIRATE INSURANCE poster that has stood there since #380). Each is one more
    // ViewObject fixture, and a fixture is drawn as a marker and a label — so every one of the seven halls
    // gained EXACTLY SIX calls over L5b's numbers: 352→358, 356→362, 358→364, 352→358, 356→362, 356→362,
    // 358→364. The arithmetic is the proof, and it is a different arithmetic from L5b's: that lane's rows
    // moved by the number of FREE TOPS a bar had that watch, which differs per haven; these move by six
    // everywhere, because three plates × two calls does not depend on who is drinking. Not one ship, wreck,
    // hive or park row moved at all — the rest of this table is byte-identical across both lanes. Had a
    // count moved by anything but six here, it would be a different lane's bug wearing this lane's clothes.

    // ── #986 F2 · FOUR ROWS RE-PINNED, EACH BY EXACTLY ONE CALL ──────────────────────────────────────
    //
    // #327's orbit line and #825's stall banner are drawn on a backing plate now (SpaceSails.Core.CommsBand),
    // the way every other line the #324 visibility law protects already was. A plate is ONE FillRect, so a row
    // that prints something in that top-centre band gains exactly one call and nothing else: 308→309,
    // 3881→3882, 266→267, 4364→4365. The arithmetic is the proof — and so is the shape of the rest of this
    // table, because a frame with nothing to say up there draws no plate and did not move: not one haven, not
    // one wreck, not one B-floor, not "surface · luna site 0" (3771, which carries no orbit line). The four
    // that moved are the docked ship with the machine stalling and the three excursions that are calling home.
    // Had a fifth row moved, or any of these moved by two, it would be a different bug wearing this one's
    // clothes. (The companion texts under Fingerprints/ moved on the same day and in the same single row —
    // `walked-view pen`, one more call per frame that draws a band — 17 of the 30, no ledger row, no sweep row.)

    // ── #1016 · THIRTEEN ROWS RE-PINNED, EACH BY EXACTLY SIX CALLS ───────────────────────────────────
    //
    // Owner, on 7 Deck: "Why no table here to sit at?", "Why no table in cabin either?", "I expect to have a
    // bar table like this in this ships galley also.... feature complete." Her cantina's three drawn tops
    // were dressing with no console over them and her cabins had a bunk and nothing else, so the SHIP's own
    // plan gained exactly three consoles: two takeable tops (the third stands under the CANTINA desk and is
    // refused by the deck audit's label law) and the DESK ✍ in CABIN 1.
    //
    // A fixture is drawn as a marker and a label — two calls — so three of them is SIX, and every row that
    // moved moved by six and by nothing else: 335→341, 309→315 (×2), 358→364 (×2), 362→368 (×3), 364→370
    // (×2), 3771→3777, 3882→3888, 4365→4371. It is the same arithmetic #973 L4's re-pin used, on the other
    // deck: three plates × two calls, independent of who is in the room.
    //
    // AND THE SHAPE OF THE REST OF THE TABLE IS THE OTHER HALF OF THE PROOF. Every row that moved is a deck
    // the SHIP's plan is part of — her own three, the seven havens (a docked complex is her plan with a
    // station welded onto it, seeded from `DeckPlan.Ship.Consoles`) and the three excursions whose field
    // carries her down-tube. Not one wreck, not one B-floor, not the dark-lamp row and not "surface · luna
    // site 0, dark, and the fan hears something" moved at all, because none of them is her. Had a hive floor
    // moved here, or any of these moved by anything but six, it would be a different lane's bug wearing this
    // lane's clothes.

    // ── #1039 · THREE ROWS RE-PINNED, EACH BY EXACTLY FOUR CALLS — AND THE FOUR ARE TWO LIES ─────────
    //
    // Owner, walking the Tilt's ground (#1015): "The magazine count follows the walker here…" — a 99 welded
    // under the ship's orbit line with no sentry anywhere near it. #986 F2's band-avoidance re-seated a plate
    // that would reach into the comms band onto a FIXED screen row, and a fixed row on a FollowCam frame is a
    // row glued to the captain. So a counter is now drawn only where its own sentry's MARK is drawn.
    //
    // THIS SNAPSHOT WAS DRAWING TWO OF THEM. `TheWholeExcursion` carries three bots — (5,5), (−5,5), (−5,−5)
    // — and the captain stands at MoonSurface.SpawnY = −21.5. At 1200×700 the scale is 18.75 and the origin
    // 350 + (−21.5 × 18.75) = −53.1, so the two bots at deck y = +5 project to y = −146.9: a hundred and
    // forty-seven pixels ABOVE the top of the glass, marks nobody could ever have seen, each wearing a plate
    // parked at CommsBand.ReservedBottom. Only the dry "00" bot at y = −5 (y = 40.6) was ever really there.
    //
    // A plate is a fill and its digits — two calls — so two phantoms is FOUR, and every row that moved moved
    // by four and by nothing else: 3888→3884, 267→263, 4371→4367. And the shape of the rest of the table is
    // the other half of the proof: the only rows that moved are the three that hand Draw a Bots list.
    // "surface · luna site 0" carries no surface hud at all and is unchanged at 3777; not one ship, haven,
    // wreck or B-floor row moved, because none of them has a sentry on it. Had a fourth row moved, or any of
    // these moved by two, it would be a different bug wearing this one's clothes.

    /// <summary>Every case drawn, as ledger rows — the measurement the re-pin command runs and writes
    /// down, and the same measurement the guard below compares against what is written down.</summary>
    internal static IReadOnlyList<PinLedger.Row> MeasureEveryRow()
    {
        var rows = new List<PinLedger.Row>();
        foreach (string name in Names())
        {
            (int calls, string sha) = Frame(name);
            rows.Add(new PinLedger.Row(CallsProbe, name, calls.ToString(CultureInfo.InvariantCulture)));
            rows.Add(new PinLedger.Row(ShaProbe, name, sha));
        }
        return rows;
    }

    /// <summary>
    /// EVERY CASE DRAWS THE FRAME IT DREW BEFORE THE SPLIT — same calls, same order, same numbers.
    /// </summary>
    [Fact]
    public void EveryCaseDrawsTheFrameThatWasPinnedOnTheOldCode()
    {
        IReadOnlyDictionary<string, PinLedger.Row> pinned = PinLedger.Pinned(Suite);
        var wrong = new List<string>();
        var fresh = new List<string>();
        int cases = 0, calls = 0;

        foreach (string name in Names())
        {
            cases++;
            (int drew, string sha) = Frame(name);
            calls += drew;

            // An anti-vacuity clause that fires FIRST: a case that draws nothing would otherwise pin a
            // digest of the empty string and stay green through anything.
            Assert.True(drew > 20, $"'{name}' laid only {drew} mark(s) — that frame proves nothing.");

            if (!pinned.TryGetValue(PinLedger.Key(CallsProbe, name), out PinLedger.Row pinCalls)
                || !pinned.TryGetValue(PinLedger.Key(ShaProbe, name), out PinLedger.Row pinSha))
            {
                fresh.Add($"  {name} — {drew} call(s), sha256 {sha}");
                continue;
            }
            if (pinCalls.Value != drew.ToString(CultureInfo.InvariantCulture)
                || !string.Equals(pinSha.Value, sha, StringComparison.Ordinal))
            {
                wrong.Add($"  {name} — {drew} call(s), sha256 {sha}"
                    + $"{Environment.NewLine}      pinned {pinCalls.Value} call(s), sha256 {pinSha.Value}");
            }
        }

        Assert.True(fresh.Count == 0,
            $"{fresh.Count} case(s) have no pin in {Suite}.ledger.txt — a ledger row is never typed in by "
            + $"hand, it is measured:{Environment.NewLine}  {PinLedger.Invocation}{Environment.NewLine}"
            + string.Join(Environment.NewLine, fresh));
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} case(s) draw a different frame than the one pinned on the old code:"
            + Environment.NewLine + string.Join(Environment.NewLine, wrong)
            + Environment.NewLine + Environment.NewLine
            + "If the change is intended, re-pin BY MEASUREMENT and paste the printed report into the PR:"
            + Environment.NewLine + "  " + PinLedger.Invocation);

        Assert.True(cases >= 20, $"only {cases} frame(s) were drawn — this sweep proves little.");
        // #563 · The floor came down from 20,000 with the viewport cull, and this is a re-measurement rather
        // than a nudge to get green: the sweep laid 25,062 marks and now lays 12,776, because every one of the
        // missing ones was drawn past the edge of the glass. The floor's job is to catch a sweep that has
        // stopped drawing anything, so it goes just under what the sweep genuinely lays.
        Assert.True(calls > 10_000, $"only {calls} mark(s) were laid in all — this sweep proves little.");

        // …and the ledger holds two rows for every case and not one row more: a pin for a case that is no
        // longer drawn is a number nothing measures, and it would sit there green forever.
        Assert.Equal(cases * 2, pinned.Count);
    }

    /// <summary>The same frame drawn twice in the same process is the same frame — the clause that catches a
    /// pen with memory in it, or a plan built off an unordered set, before it can make the pins flaky.</summary>
    [Fact]
    public void TheSameFrameDrawnTwiceIsTheSameFrame()
    {
        foreach (string name in Names())
        {
            (int a, string sha) = Frame(name);
            (int b, string again) = Frame(name);
            Assert.Equal(a, b);
            Assert.Equal(sha, again);
        }
    }
}
