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
/// <para><b>This file is committed alone, ahead of the split</b> — every digest below was taken on the
/// 1,058-line <c>Draw</c> exactly as it shipped, so there is no chance of pinning what the new code happens
/// to do. The proof that these pins can go RED is run against the split itself and recorded in the PR.</para>
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
            Beacons: [(1.0, 20.0, true, false), (2.5, 33.0, false, true)],
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

    private static readonly Dictionary<string, (int Calls, string Sha)> Pinned = new(StringComparer.Ordinal)
    {
        ["ship · under way"] = (335, "84f477c2696097c1d9f85a65a9a5969d549d108741bc97113a1897553440ec4c"),
        ["ship · docked, the shuttle away and the machine stalling"] = (308, "af920816b92c1be3ad51dbaed0da98c66be8afba53bf88d59019325d56a44d85"),
        ["ship · hatches dogged, seated at the table"] = (309, "580fc08e861399765b2791b5d4bfbfe1c2d9eba77a850ba1af032100af727564"),
        ["haven · the-space-bar"] = (346, "40fa9ce0068ad55d780febb3fd948fe3b799f1c0ca0baf880fd553899464371e"),
        ["haven · cinder-roost"] = (350, "d50505ed61f142e79c2846fd35710cabdfb1f538326a68604d22dbad4a37c036"),
        ["haven · ringside-exchange"] = (350, "4851d548022be2d57c0e6ce09d16b9d68223f997951d4c5026741635f312abc5"),
        ["haven · the-tilt"] = (344, "34207d0f92537af1e4c259473323ae11a7915a169b6c32d86c7872f44e2455ad"),
        ["haven · selene-gate"] = (348, "6ed52f7a714ce0849eee6efe4cd51faddb7413a5a53f2a61fddd8603aaaf9909"),
        ["haven · red-eye"] = (346, "b16f67dbb88f45a585b1bc47ec6ddcb965e199505b6c9d8aec79fed2d0550842"),
        ["haven · the-deep"] = (350, "43c9a94c0ee1d22dc1bb2212c9951306fb1c5029126a98212b55efc4ad55059c"),
        ["wreck · HullBreach"] = (592, "a6861bad89c60f79b415fdbe913c809cfba6f6d832c8ee1a689764f37019f482"),
        ["luna B1"] = (1591, "6ed0aaccf49a5d11ac851766f70f176ffb5da2ced290023149e7eddcabf0b4f4"),
        ["luna B2"] = (426, "2a74f1342bd3e2eeced21aac071fc9b844481a9e6abb26823b31389b599901d8"),
        ["luna B3"] = (315, "757c755407eea1e5f88998b04094bcef4c3fbe73bf2dadd025e3a8a5e6853287"),
        ["luna B4"] = (316, "9da4200e95d79865e0b1cd4b5e354048c946d1ef9af83bc0c9e35a056a635ee5"),
        ["luna B5"] = (317, "61c5c7f7d467f8bf2240a380db3856330cf1fd2ca1e977074c93e628d22e802a"),
        ["luna B6"] = (367, "6725be0d8c203d597d15a2e0a1633ff93053ad69b66e136f6b2905ef0790b094"),
        ["luna B7"] = (317, "97bcd99a12895c2b88bce6d7974681000a61dac10abd33531e9eb79a4bbad0b8"),
        ["luna B8"] = (329, "d269691759e77f1497ca718b0508a424fdfd3fa6423ea5a9392511fd512e9f11"),
        ["luna B9"] = (491, "a2d2322ba0c252607852934f51c9e8a84f2736a942866c6536c5827ca5776a5d"),
        ["luna B10"] = (456, "a87406306627f3937849f2838735ee8208a1e9906943d4833b85b2d2365d8a8a"),
        ["luna B11"] = (289, "54e471c8a79a250ad4bdccd6fe12c8633b9038661df32585f582240f158fd74f"),
        ["luna B12"] = (309, "50a2f7952d013e4ef93a399e73d438d3461c8e80821db7bb1c745fd51ab31542"),
        ["luna B13"] = (421, "219604d9c664bd15c413eb683a09473a980a281ddfa76d83e4b4c323d798b6a8"),
        ["luna B1 · the lamps are all there is"] = (128, "7116fa86b505d015afd4218dce97075de9237b309a0b617a2eb441248e4af643"),
        ["luna B1 · sat down at a desk"] = (1607, "0d183fb7256795d857d94850773f34f6e34798a8fca992944f565524ebeb29ae"),
        ["luna B1 · the round is out, and the buzzer went"] = (1617, "7f30e140ef53fd173f50118a55df71ccbc1346c658b339dbcc40f02e31ff0b90"),
        ["phobos B1"] = (1563, "c6a9b5b9c54e74c0664d7567b2bfe1ced5007156be80236adb1b90b0c0567ae9"),
        ["titan B1"] = (1512, "e79db73c682aa4615097549d375f442e45fcadb734d8b06ebc847538dc555f51"),
        ["surface · luna site 0"] = (3771, "6405eda6ad8f3486a553cce6f7b5fa95afe7c0dd4d346721e5a84db3544c5941"),
        ["surface · luna site 0, the whole excursion"] = (3881, "e9e22a17582dffa7d4f6a9041d421d89a12b04b54ea6d231c45dc14f467c1c3c"),
        ["surface · luna site 0, dark, and the fan hears something"] = (266, "d3ea7f061938e922b2128a6620dd755fef86f998bd474e140e6db30f46a7a25a"),
        ["surface · titan site 0, a derelict's instruments (none)"] = (4364, "734c0557356b6bdae5c2fcccce2866fbdc25db91973235a118bffd32682a41d3"),
    };

    /// <summary>
    /// EVERY CASE DRAWS THE FRAME IT DREW BEFORE THE SPLIT — same calls, same order, same numbers.
    /// </summary>
    [Fact]
    public void EveryCaseDrawsTheFrameThatWasPinnedOnTheOldCode()
    {
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

            if (!Pinned.TryGetValue(name, out (int Calls, string Sha) pin))
            {
                fresh.Add($"        [\"{name}\"] = ({drew}, \"{sha}\"),");
                continue;
            }
            if (pin.Calls != drew || !string.Equals(pin.Sha, sha, StringComparison.Ordinal))
            {
                wrong.Add($"  {name} — {drew} call(s), sha256 {sha}"
                    + $"{Environment.NewLine}      pinned {pin.Calls} call(s), sha256 {pin.Sha}");
            }
        }

        Assert.True(fresh.Count == 0,
            $"{fresh.Count} case(s) have no pin. Paste these into Pinned:{Environment.NewLine}"
            + string.Join(Environment.NewLine, fresh));
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} case(s) draw a different frame than the one pinned on the old code:"
            + Environment.NewLine + string.Join(Environment.NewLine, wrong));

        Assert.True(cases >= 20, $"only {cases} frame(s) were drawn — this sweep proves little.");
        Assert.True(calls > 20_000, $"only {calls} mark(s) were laid in all — this sweep proves little.");
        Assert.Equal(cases, Pinned.Count);
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
