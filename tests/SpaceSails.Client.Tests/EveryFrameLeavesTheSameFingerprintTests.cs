using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 7c · THE FRAME LEAVES THE SAME MARK IT ALWAYS DID.
///
/// <para><c>OnTick</c> was 505 lines in one method — the accumulator, the fixed-step integration, the split
/// advance onto a burn epoch, the surface-impact watch, the sweep, the reprojection cadence, the pulse expiry,
/// the shuttle run, the charge systems, the story cards, the walked frame and the HUD throttle, in one
/// straight pass. Cutting it into named phases is a refactor with nothing to hide behind: there is no unit
/// under it to test, and every one of this repo's named bug classes lives exactly here — <b>a list built by
/// appending is not a list in order</b>, and <b>the sim doing one thing while a sentence or a drawn shape
/// reports another</b>. A guard that merely re-asserted a handful of facts would pass on a build where two
/// phases had swapped, which is precisely the mistake a 505-line method invites.</para>
///
/// <h3>THE LAW: A FINGERPRINT, CAPTURED ON THE OLD CODE FIRST</h3>
///
/// <para>So the guard is a SNAPSHOT. A real <see cref="Pages.Map"/> is booted on six worlds, each is driven
/// through <c>OnTick</c> with five FIXED sequences of <c>highResTimestampMs</c> values and inputs, and
/// afterwards everything the frame wrote is serialised into one deterministic text and hashed. The hashes below were taken on the
/// commit BEFORE the split and committed on their own ("the snapshot, on the old code") so that they could
/// never be quietly re-baselined afterwards: git says which commit each number came from.</para>
///
/// <para>The text has three parts, and each one closes a different way of getting this wrong:</para>
/// <list type="number">
///   <item><b>THE LEDGER</b> — thirty-nine named readings (avatar, sim clock, accumulator, warp, the pulse
///   slot and the words in it, the nerve, the tracker, the guards' positions, the FrameGap clock, the camera,
///   the passes, the trail). Committed as readable rows in <c>Ledgers/Fingerprints.ledger.txt</c>, so a
///   red run names the ROW that moved instead of printing two hashes that differ.</item>
///   <item><b>THE SWEEP</b> — a generic walk over EVERY instance field of the component (minus the machinery
///   listed in <see cref="NotFingerprinted"/>), hashed to one line. The ledger says WHERE; the sweep says
///   NOTHING ESCAPED. A phase that writes a field nobody thought to name is still caught.</item>
///   <item><b>THE PEN</b> — every draw call the frame issued, in order, hashed. <c>DrawWalkFrame</c> paints
///   through a recording <see cref="IRenderer"/>; the map frame paints into the real
///   <see cref="CanvasRenderer"/>'s command buffer, which is read back. This is the half a state fingerprint
///   cannot see: the third named bug class is the picture disagreeing with the sim.</item>
/// </list>
///
/// <h3>WHAT IS EXCLUDED, AND WHY</h3>
///
/// <para><b>The tail of the map frame.</b> <c>CanvasRenderer.EndFrame</c> is the one line of <c>OnTick</c>'s
/// flight path that crosses into JavaScript, and <c>[JSImport]</c> throws
/// <c>PlatformNotSupportedException: System.Runtime.InteropServices.JavaScript is not supported on this
/// platform</c> on a test runner. <c>_renderer</c> is typed to the sealed <see cref="CanvasRenderer"/>, so
/// there is no seam to substitute. <see cref="World.TheMapFrameInFlight"/> therefore drives the flight branch
/// up to that flush and pins what it wrote — including the whole command buffer, which is complete by then —
/// and asserts that the flush is exactly where it stopped. The ~40 lines AFTER the flush (the scope inset, the
/// parrot, the ship's alert strip, the long-coast advert, the arrival-brake gate, the firing-solution reveal
/// and the HUD throttle) are unreachable off-browser and are NOT fingerprinted. They are named here so that
/// nobody reads the green and believes more than it says.</para>
///
/// <para><b>One wall clock, named.</b> <c>_frameServicedAtMs</c> is <c>Environment.TickCount64</c> and has to
/// be — see <see cref="AWallClockAndNothingElse"/> for #825's own reason and for what is pinned in its place.
/// It is the ONLY exclusion of its kind: there is no <c>Stopwatch</c> and no unseeded <c>Random</c> anywhere
/// in the frame, every timestamp is handed in by the bench, and the traffic is generated from the same fixed
/// seeds (42/43) the shipping boot uses.</para>
///
/// <h3>THE SAME NUMBERS ON THE MACHINE THAT MATTERS</h3>
///
/// <para>Sets and dictionaries are rendered in SORTED order, because .NET randomises string hashing per
/// process and insertion order would otherwise make the hash a coin toss between two runs on one box. Numbers
/// are written to thirteen significant digits (see <see cref="Num(double)"/>), which is orders of magnitude
/// clear of the last-bit disagreement two C runtimes can have about <c>Math.Sin</c> and still far finer than
/// anything a reordered phase could hide in. That is not a hope: the texts below were captured on
/// Windows and then reproduced BYTE FOR BYTE by the same assembly under
/// <c>mcr.microsoft.com/dotnet/sdk:10.0</c> on Linux, which is what CI runs.</para>
///
/// <h3>PROVEN ABLE TO FAIL</h3>
///
/// <para>Moving one phase of the split frame past its neighbour reddens the rows. The verbatim run is in the
/// pull request. When a row DOES go red, <c>SPACESAILS_SWEEP_DUMP=&lt;dir&gt;</c> writes the whole swept text
/// out so the offending field can be diffed rather than guessed at from a hash.</para>
///
/// <h3>#561 · THE ONE RE-PIN, AND WHAT THE DIFF HAD TO SHOW BEFORE IT WAS ALLOWED</h3>
///
/// <para>The nerve gauge's backing plate is measured to what it backs now, and the motion tracker's column
/// top is ASKED of the nerve block rather than typed at 82 (<c>HudColumn</c>) — so on the five worlds that
/// draw a gauge one rectangle is taller and, on the regolith, the fan sits 18px further down the column.
/// Twenty-five of the thirty texts moved. In every one of them the ONLY line that changed was
/// <c>walked-view pen</c>; its CALL COUNT is identical on both sides of the diff (210720, 470880, 35917, …
/// all unchanged, because the same rectangle and the same disc were drawn at a different y); and the five
/// <see cref="World.TheMapFrameInFlight"/> texts, which draw no walked view at all, are untouched. A re-pin
/// that had moved a ledger row, a sweep row or a call count would have been a different lane's bug wearing
/// this lane's clothes.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[SlowGate] // #251 · 36 s over 32 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed partial class EveryFrameLeavesTheSameFingerprintTests
{
    private const BindingFlags Hidden = TestTree.AnythingOnAnInstance;

    // ── WHERE THE PINS LIVE ───────────────────────────────────────────────────────────────────────────
    //
    // #1055 · The thirty texts that used to sit under Fingerprints/ — one file per row, each of them
    // hand-edited by every lane that moved a field — are one machine-written ledger now:
    // Ledgers/Fingerprints.ledger.txt, one row per (probe, scene). #1054's "thirty files, one line each"
    // is thirty rows in ONE probe's block, written by the re-pin command and reviewed by its report.
    //
    //   TO RE-PIN (runs the measurement, rewrites the ledger, prints the report):
    //     SPACESAILS_REPIN=1 dotnet test tests/SpaceSails.Client.Tests -c Release \
    //       --filter FullyQualifiedName~ThePinsAreRewrittenOnlyWhenAsked \
    //       --logger "console;verbosity=detailed"

    internal const string Suite = "Fingerprints";

    /// <summary>The roster block: one row per field the sweep walks, so a field joining the page reddens by
    /// NAME instead of by a count. See <see cref="TheSweepWalksTheRosterThatWasPinned"/>.</summary>
    private const string RosterProbe = "sweep roster";

    private const string StoppedAtProbe = "stopped-at";
    private const string SweepProbe = "sweep";
    private const string PenProbe = "walked-view pen";
    private const string BufferProbe = "map-frame buffer";

    /// <summary>
    /// What the ledger's own header says about where these numbers came from.
    ///
    /// <para><b>The count of LEDGER readings is COUNTED, not typed, and #1170 is why.</b> It said
    /// <i>thirty-eight</i> while <see cref="TheLedger"/> held thirty-nine — a probe had been added and the
    /// sentence describing the probes had not been re-read. #1169's stale-fact sweep caught it and could not
    /// fix it, because this text is written byte for byte into <c>Ledgers/Fingerprints.ledger.txt</c> and
    /// correcting a word here is a RE-PIN, which a documentation lane may not do. So rather than typing the
    /// right number and leaving the next one to go wrong, it is read off the array it describes: add a
    /// reading and the header says so on the next re-pin, without anybody remembering to.</para>
    ///
    /// <para>A property and not a field, deliberately — Appendix E3's lesson. A static field initialiser
    /// reading <see cref="TheLedger"/>, which is declared in the OTHER half of this partial class, would be
    /// at the mercy of the order the compiler reads the two files in; a property is evaluated when it is
    /// asked, by which time the class is initialised. It is asked once, by
    /// <c>ThePinsAreRewrittenOnlyWhenAskedTests</c>.</para>
    /// </summary>
    internal static string Preamble =>
        "SIX WORLDS × FIVE INPUT SEQUENCES — everything one frame after another writes on Pages.Map.\n"
        + "Taken on the PRE-SPLIT code (#870 lane 7c): the first twenty on b19ef16, the plasma world's four\n"
        + "on 04bb219, the warp slider's six on the commit that put the unsplit method back to capture them.\n"
        + $"Probes: `stopped-at` and the {Spelled(TheLedger.Length)} named LEDGER readings say WHERE; "
        + "`sweep` says NOTHING\n"
        + "ESCAPED (a count and a hash over every instance field of the page); `sweep roster` names those\n"
        + "fields one per row, so a field joining the page reddens by name; `walked-view pen` and\n"
        + "`map-frame buffer` are the picture, which is the half a state fingerprint cannot see.\n"
        + "The re-pin history — which lane moved which probe, and the arithmetic that proved it — is in the\n"
        + "docs on EveryFrameLeavesTheSameFingerprintTests.EveryFrameItRunsFingerprintsTheSame.";

    /// <summary>A small cardinal in the words this header is written in, because "the 39 named LEDGER
    /// readings" would change the register of a line whose whole job is to be read by a person. Above
    /// ninety-nine it hands back digits rather than inventing prose nobody has proof-read — a ledger with a
    /// hundred probes in it is a different document and should be re-worded by hand.</summary>
    private static string Spelled(int count)
    {
        string[] ones =
        [
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven",
            "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen",
        ];

        string[] tens =
        [
            "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety",
        ];

        if (count is < 0 or > 99)
        {
            return count.ToString(CultureInfo.InvariantCulture);
        }

        return count < 20 ? ones[count]
            : count % 10 == 0 ? tens[count / 10]
            : $"{tens[count / 10]}-{ones[count % 10]}";
    }

    /// <summary>One row of the matrix, named the way the ledger names it.</summary>
    private static string SceneName(World world, Sequence sequence) => $"{world}.{sequence}";

    /// <summary>One driven row: every probe it read, and the roster the sweep walked while reading them.</summary>
    private sealed record Reading(
        IReadOnlyList<(string Probe, string Value)> Rows,
        IReadOnlyList<(string Field, string Type)> Roster);

    // ── THE THIRTY ROWS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Six worlds × five sequences. Each row's readings are pinned in
    /// <c>Ledgers/Fingerprints.ledger.txt</c> under the scene name <c>&lt;world&gt;.&lt;sequence&gt;</c>
    /// (#1055; until then they were thirty separate texts under <c>Fingerprints/</c>, hand-edited by every
    /// lane that moved a field), taken on the PRE-SPLIT code — the first twenty on
    /// commit b19ef16, the plasma world's four on 04bb219, and the warp slider's six on the commit that put
    /// the unsplit method back in the tree to capture them.
    ///
    /// <para><b>#618 · ALL THIRTY WERE RE-RECORDED, AND IT IS THE STATE-SHAPE KIND OF CHANGE — the third kind
    /// <c>EveryRoundFingerprintsTheSameTests</c> names, and the same proof.</b> The sweep walks the page's
    /// fields and RENDERS each one whole, so a member added to a collaborator the page holds — here the
    /// round's three for #618 (<c>LookingIntoIt</c>, <c>TheNoise</c>, <c>ShotsAnswered</c>) — moves the
    /// sweep's hash on every row, in worlds that have no round on the floor at all and never fire anything.
    /// The field COUNT is unmoved at 657, because it counts <c>Map</c>'s own fields and #905's ratchet is
    /// untouched: no field was added to the page.</para>
    ///
    /// <para>It was proved a state change and not a behaviour change the only honest way, using this file's
    /// own <c>SPACESAILS_SWEEP_DUMP</c> hook: the whole sweep was dumped on the base (d1fbc0c) and on this
    /// lane and the sixty texts compared line by line. All thirty have the same line count either way, and on
    /// every one of them <b>exactly one line differs — <c>_patrol=</c> — and nothing in it was REMOVED or
    /// CHANGED</b>: the only difference is five tokens added, <c>LookingIntoIt=∅</c>, <c>TheNoise=(0, 0)</c>
    /// and <c>ShotsAnswered=0</c>, every one at its default, because no world here fires a gun. Every other
    /// field of the round, every guard, the escort, the wallet and the whole rest of the page are
    /// byte-identical, and so are the ledger, the pen and the canvas buffer on all thirty rows — the diff of
    /// the committed texts is 30 files, one line each, and that line is the sweep's hash.</para>
    ///
    /// <para><b>If any of these ever moves again, that is not this lane's kind of change either, and the same
    /// dump-and-diff is what settles it.</b></para>
    ///
    /// <para><b>#969 · ALL THIRTY WERE RE-RECORDED AGAIN, and this time the field COUNT itself moved — 663 →
    /// 664.</b> That is the honest signature of what #969 did: one field was added to the page,
    /// <c>_armedArrivalPassSimTime</c>, the pass epoch an arrival ARMED AT PLAN TIME was rehearsed for. It is
    /// the whole of the new state (no forked autopilot), and every world here has it at its default
    /// <c>null</c>, because none of them arms an arrival. The diff of the committed texts is again 30 files,
    /// one line each, and that line is the sweep's — the ledger, the pen, the canvas buffer, the call counts
    /// and every other row are byte-identical, which is exactly the claim: the frame's BEHAVIOUR is
    /// unchanged, only the shape of the state it sweeps.</para>
    ///
    /// <para><b>#973 L2 · AND ALL THIRTY AGAIN, with the field count 672 → 686.</b> Fourteen fields, all of
    /// them the Nebula rep's and all of them on the page: which visit's room he is remembering, the running
    /// visit count, whether the rota has him working it, the per-visit remember-you-said-no, the meeting
    /// count the bleed is clocked in, when he drifts on, which fixture he is heading for, whether he has
    /// said the one thing a rebuffed salesman says, his pitch card and the four things on it, and the
    /// <c>?rep=</c> cheat. <b>Every one of them is at its default in every world here</b>, because none of
    /// these six worlds is a hive canteen with him on the rota — the diff of the committed texts is again 30
    /// files, one line each, and that line is the sweep's own. The ledger, the pen, the canvas buffer and
    /// the call counts are byte-identical on all thirty rows, and <c>EveryFrameHashesTheSameTests</c>' draw
    /// transcripts did not move at all: he is a walker, so when he is not on a floor there is nothing extra
    /// to draw.</para>
    ///
    /// <para><b>#962 · AND ALL THIRTY ONCE MORE, field count 720 → 721.</b> The smallest version of the same
    /// signature: <b>one</b> field was added to the page, <c>_autopilotPlanBodyClearance</c> — how close the
    /// armed autopilot's rehearsed path came to each world it passed, cached at arm time beside the path and
    /// the collision pass it already cached (the #219 one-arm law), so the #180 park watchdog can ask whether
    /// the plan cleared the body the ship is BOUND to. The <c>SPACESAILS_SWEEP_DUMP</c> dump-and-diff was run
    /// on the base (35dd47a) and on this lane, and on <b>all thirty</b> rows the diff is the same single
    /// line, and it is an ADDITION, never a change or a removal:</para>
    /// <code>52a53
    /// &gt; _autopilotPlanBodyClearance=∅</code>
    /// <para>…at its default in every world, because not one of these six arms an autopilot approach. The
    /// ledger, the pen, the canvas buffer and the call counts are byte-identical on all thirty rows, and
    /// <c>EveryFrameHashesTheSameTests</c>' draw transcripts and <c>EveryRoundFingerprintsTheSameTests</c>
    /// did not move at all — the alarm this lane quiets shouts on nothing any of these worlds does.</para>
    ///
    /// <para><b>#989 · AND ALL THIRTY AGAIN, field count 721 → 724.</b> The same state-shape signature, three
    /// fields wide: the plan-SHAPE alarm's own state (<c>_shapeAlarm</c>, <c>_shapeAlarmDismissed</c>,
    /// <c>_shapeWasWellFormed</c>) — the #965 one-shot machinery, applied to the question <i>can this plan be
    /// flown as written at all</i> now that a cast off can be SCHEDULED and so can end up behind a burn. The
    /// <c>SPACESAILS_SWEEP_DUMP</c> dump-and-diff was run on the base (73e2785) and on this lane, and on
    /// <b>all thirty</b> rows the diff is the same three lines, and every one of them is an ADDITION — never
    /// a change, never a removal:</para>
    /// <code>544a545,547
    /// &gt; _shapeAlarm=∅
    /// &gt; _shapeAlarmDismissed=no
    /// &gt; _shapeWasWellFormed=yes</code>
    /// <para>The first two are at their defaults everywhere (no world here holds a malformed plan) and the
    /// third reads <c>yes</c> everywhere, which is the guard reporting that it RAN and found nothing wrong —
    /// a <c>no</c> on any of these rows would have been a real bug, not a re-pin. The ledger, the pen, the
    /// canvas buffer and the call counts are byte-identical on all thirty, the committed-text diff is 30
    /// files × 1 line and that line is the sweep's own, and no other fingerprint suite moved at all: the full
    /// client run's only red was this class's thirty rows.</para>
    ///
    /// <para><b>#953 · TWENTY WERE RE-RECORDED — not thirty, and the ten that held still are the proof.</b>
    /// The first re-pin in this ledger that is a <i>removal</i> rather than an addition, and the field count
    /// did not move at all (725 → 725): no field was added or taken away, one field's CONTENTS changed. The
    /// owner archived the ship-lane overlay (<i>"we have never used them to find anything"</i>), so
    /// <c>MapLayerTree.DefaultHidden</c> stopped seeding the one leaf that used to start hidden, and the
    /// page's <c>_hiddenLayersByDesk</c> is one entry lighter wherever a desk materialised its set. The
    /// <c>SPACESAILS_SWEEP_DUMP</c> dump-and-diff was run on the base (b4b5cb6) and on this lane, and on
    /// <b>every one of the twenty</b> the diff is the same single line:</para>
    /// <code>248c248
    /// &lt; _hiddenLayersByDesk={ShipDesk.Nav: ["routes.lanes"]}
    /// ---
    /// &gt; _hiddenLayersByDesk={ShipDesk.Nav: []}</code>
    /// <para>The other <b>ten are byte-identical</b> — all five <c>HerOwnDeckInFlight</c> rows and all five
    /// <c>TheElectricUniverse</c> ones, the worlds that never ask the Nav desk for a layer and so never build
    /// its hidden set at all. That is the honest signature of a per-desk default changing and nothing else: a
    /// lane draw that had really been ripped out of a painted frame would have moved the pen and the canvas
    /// buffer on the map worlds, and a state field added or dropped would have moved the count. The
    /// committed-text diff is 20 files, one line each, and that line is the sweep's own;
    /// <c>EveryFrameHashesTheSameTests</c>' draw transcripts and <c>EveryRoundFingerprintsTheSameTests</c> did
    /// not move at all — which is exactly the claim: <b>the archived overlay was already switched off in every
    /// one of these worlds, so deleting it changed no pixel anywhere.</b> (The two lane-cache fields kept
    /// their names for the same reason this note exists: a rename would have moved all thirty hashes and said
    /// nothing.)</para>
    ///
    /// <para><b>#973 · AND ALL THIRTY ONCE MORE, field count 725 → 733.</b> The same state-shape signature,
    /// eight fields wide: the VOID'S WEATHER (<c>Map.Weather.cs</c>) — which station this visit's bar talk
    /// belongs to, how many times each station has been stood in and which visit a line last blew through it
    /// on, whether this visit has asked, and what it is on today. The <c>SPACESAILS_SWEEP_DUMP</c>
    /// dump-and-diff was run on the base (c192ecc, after #953's own twenty were re-pinned) and on this lane,
    /// and on <b>all thirty</b> rows the diff is
    /// the same eight lines, and every one of them is an ADDITION — <b>zero lines removed, zero changed</b>:</para>
    /// <code>715a716,723
    /// &gt; _weatherAsked=no
    /// &gt; _weatherHeard={}
    /// &gt; _weatherLastSaid={}
    /// &gt; _weatherSaidId=∅
    /// &gt; _weatherShared=no
    /// &gt; _weatherSpeaker=∅
    /// &gt; _weatherStation="luna"
    /// &gt; _weatherStationVisits={"luna": 0}</code>
    /// <para>Six of the eight are at their defaults on every row — none of these worlds opens a counter, so
    /// nothing is ever drawn and nothing is ever heard. The two that are not are the visit CLOCK, and they
    /// read the ground the captain is actually standing on: the fold rides <c>EnsureRepVisit</c>, so a world
    /// on Luna counts one visit to Luna and no line is in the air on it. An empty <c>_weatherStationVisits</c>
    /// on these rows would have meant the fold never ran, which is a real bug and not a re-pin. The ledger,
    /// the pen, the canvas buffer and the call counts are byte-identical on all thirty, the committed-text
    /// diff is 30 files × 1 line and that line is the sweep's own, and the full client run's only red was
    /// this class's thirty rows.</para>
    ///
    /// <para><b>#1016 · TWENTY-FOUR WERE RE-RECORDED — not thirty, and the six that held still are half the
    /// proof. The field count did not move (736 → 736), and this is the first re-pin in this ledger that
    /// moved the PEN.</b> Owner, on 7 Deck: <i>"Why no table here to sit at?"</i>, <i>"Why no table in cabin
    /// either?"</i>, <i>"I expect to have a bar table like this in this ships galley also.... feature
    /// complete."</i> The SHIP'S OWN PLAN gained exactly three consoles — two takeable tops in her cantina
    /// (the third stands under the CANTINA desk and is refused by the deck audit's label law) and the
    /// DESK ✍ in CABIN 1. A console is DRAWN, so a frame that paints her deck paints three more
    /// fixtures.</para>
    ///
    /// <para>Three row kinds moved and no others, and each has its own arithmetic:</para>
    /// <list type="number">
    ///   <item><b><c>sweep</c>, on 24 rows.</b> The <c>SPACESAILS_SWEEP_DUMP</c> dump-and-diff was run on
    ///   the base (e2633bc) and on this lane. On every world whose <c>_deckPlan</c> is hers the diff is three
    ///   ADDED <c>ConsoleSpot</c>s and nothing else — <c>ShipDesk "DESK ✍" (13.5, −9)</c> and two
    ///   <c>BarTop "🪑 A FREE TABLE — SIT DOWN"</c> at <c>(8, 7.5)</c> and <c>(14, 7.5)</c> — with zero lines
    ///   removed and zero changed. The five <c>AHiveFloorWithAPatrol</c> rows are byte-identical, because a
    ///   hive floor's plan is not hers.</item>
    ///   <item><b><c>the seat</c>, on the four <c>ACaptainInAChair</c> rows.</b> Three fields moved onto
    ///   <c>TableTalk</c> for the seats that have no excursion behind them — <c>Aboard</c>, <c>Waits</c>,
    ///   <c>Watch</c> — and all three read their defaults on a park bench.</item>
    ///   <item><b><c>walked-view pen</c>, on the 15 rows that paint her deck</b> — every one of them by
    ///   EXACTLY +720 calls, and the three <c>APlannedRoute</c> rows (which run twice the frames) by exactly
    ///   +1440. Six calls a frame: three fixtures × a marker and a label, which is the same arithmetic
    ///   #973 L4's re-pin used one deck over. A count that moved by anything but six a frame would be a
    ///   different lane's bug wearing this lane's clothes.</item>
    /// </list>
    /// <para>No ledger row moved, no canvas buffer moved, and not one <c>AHiveFloorWithAPatrol</c> row moved
    /// at all.</para>
    ///
    /// <para><b>#957 · 25 OF THE 30 WERE RE-RECORDED, and it is the WORLD-DATA kind of change — the one kind
    /// this ledger had not seen before.</b> #957 corrected three hand-typed orbit periods in
    /// <c>scenarios/sol.json</c>: Cinder Roost, The Rusty Roadstead and The Tilt were riding rails no gravity
    /// explains — 11.8, 10.5 and 35.9 km/s about their parents where Newton allows 4.7, 1.9 and 8.5 — which
    /// is why the autopilot refused to dock at them (<c>DockRule.MatchSpeed</c> shears above 8 km/s). Three
    /// bodies are now somewhere else at every t &gt; 0, so the frame sees it. Two row kinds moved and no
    /// others, and the <c>SPACESAILS_SWEEP_DUMP</c> dump-and-diff on the old literals and the new says how
    /// little: all thirty texts have the same 743 lines and the same 742 fields, and</para>
    /// <list type="number">
    ///   <item><b><c>sweep</c>, on 25 rows</b> — <b>exactly one field line differs, <c>_passes</c></b>, and
    ///   within it exactly two of the twenty-nine passes: <c>cinder-roost</c> and <c>the-space-bar</c>. Same
    ///   bodies, same order. Nothing else in the page moved at all.</item>
    ///   <item><b><c>map-frame buffer</c>, on the five <c>TheMapFrameInFlight</c> rows</b> — the map draws the
    ///   solar system, so it draws the three berths where they now are. The float and label COUNTS are
    ///   unchanged (8364 floats, 24 labels), the label SET is unchanged at 22, and <b>the only labels that
    ///   moved are ⚓ Cinder Roost, ⚓ The Rusty Roadstead and ⚓ The Tilt</b>.</item>
    /// </list>
    /// <para><b>No ledger row moved, no <c>walked-view pen</c> moved, and no call count moved</b> — this
    /// change draws no new thing, it draws three old things in the right place. And the five
    /// <see cref="World.TheElectricUniverse"/> rows are <b>byte-identical</b>, because that world is on
    /// <c>wheel.json</c>, which #957 does not touch: the one world that could not move, did not.</para>
    ///
    /// <para><b>#954 · ALL THIRTY WERE RE-RECORDED, and this is a kind of re-pin the ledger had not seen: a
    /// BEHAVIOUR change. The frame deliberately writes a different answer.</b> Every entry above is a
    /// state-shape change, a redraw, or world data; this one moves what the frame MEANS by "nearest". #966
    /// stopped the readout flickering between Mars and the station in its Hill sphere at the range the owner
    /// photographed, with a band measured along the sightline. That band shrinks as the ship closes, so the
    /// flicker was still waiting everywhere the ship actually flies — 1,744 changes of mind in five orbits,
    /// parked 100,000 km off Earth. So a satellite now defers to its primary until the ship is inside its
    /// Hill sphere (<c>NearestRule.StandsForItself</c>), and a mass-less berth, which has no Hill sphere at
    /// all, holds the slot only when it is clamped to.</para>
    ///
    /// <para><b>Which is exactly what these thirty rows show, and it is the same substitution every time.</b>
    /// On all thirty the nearest reading was a BERTH — <c>selene-gate</c> on twenty-five, the works platform
    /// <c>satellite-factory</c> on the five <c>wheel.json</c> rows — and on all thirty it is now the planet
    /// those berths ride: <b>Earth</b>, radius 6,371,000 either way. Four ledger rows moved and no others:
    /// <c>nearest body</c>, <c>nearest body at</c> and <c>nearest body moving</c> — the slot and its
    /// kinematics, the same fact three times — and <c>sweep</c>. The field count moved 742 → 743: ONE field
    /// added to the page, <c>_neighbourhoodHavenId</c>, the berth the neighbourhood line is naming, held
    /// across frames so a planet with two of them cannot trade their names as the rails come round.</para>
    ///
    /// <para>The <c>SPACESAILS_SWEEP_DUMP</c> dump-and-diff was run on the base (b301cc3) and on this lane,
    /// and out of 743 fields <b>exactly four differ on every row</b> — <c>_nearestBody</c>,
    /// <c>_nearestBodyPosition</c> and <c>_nearestBodyVelocity</c> changed, <c>_neighbourhoodHavenId</c>
    /// added — plus, on the five <c>wheel.json</c> rows ONLY, <c>_nearestParentName</c> and
    /// <c>_nearestChildName</c> going to their defaults: that world's works platform is not a dockable
    /// berth, so with the planet in the slot there is no berth for the line to name, and it reads "Earth"
    /// where it read "Earth › Highport Satellite Works". Those five rows have <c>_nearestHaven</c> at ∅ on
    /// BOTH sides, so no anchor was offered there before and none is withheld now.</para>
    ///
    /// <para><b>What did NOT move is half the proof.</b> <c>_nearestHaven</c> is byte-identical on all thirty
    /// rows — the ⚓ hint is exactly where it was, which is the thing a captain would have noticed going. So
    /// are <c>walked-view pen</c> and <c>map-frame buffer</c>, on every row, with every call count unchanged:
    /// this change draws nothing new and moves nothing anywhere else on the page. (The scope inset, which is
    /// where the slot shows as a PICTURE, sits after the <c>EndFrame</c> flush and is not fingerprinted here
    /// — see the exclusions above; <c>NearestHoldsTheNeighbourhoodTests</c> is the guard that watches the
    /// slot itself hold still, across thirty-two posts.) A re-pin that had moved a pen, a call count, or any
    /// field outside those six would have been a different lane's bug wearing this lane's clothes.</para>
    ///
    /// <para><b>#1040 · 24 OF THE 30 WERE RE-RECORDED, and it is #1016's kind: the PEN moved, because a room
    /// grew furniture.</b> Owner, on 7 Deck: <i>"Our on ship bar can be upgraded to match the other bars...
    /// the UI represents code long time ago."</i> Her cantina got the counter its own backdrop has always
    /// drawn — a real wall you belly up to, a stool row along it, filled slabs for its top and its back-bar,
    /// the galley console off the middle of the floor, and her three tops moved under the window (all three
    /// are takeable now, where the label law used to refuse the one standing under the galley desk). CABIN 2
    /// got the desk CABIN 1 has.</para>
    ///
    /// <para><b>The diff is the old pins against the new ones — the pinned files ARE the base's
    /// fingerprints — and exactly three row kinds moved:</b></para>
    /// <list type="number">
    ///   <item><b><c>sweep</c>, on 24 rows</b>, with the field count unmoved at <b>743</b>: no page field was
    ///   added or removed, and what changed inside it is her plan, which every world whose <c>_deckPlan</c>
    ///   is hers carries. <b>All five <c>AHiveFloorWithAPatrol</c> rows are byte-identical</b>, because a
    ///   Hive floor's plan is not hers.</item>
    ///   <item><b><c>the seat</c>, on the four <c>ACaptainInAChair</c> rows that hold one</b> — 1111 → 1121
    ///   chars, which is one added <c>TableTalk</c> field at its default (<c>Stool=no</c>).
    ///   <c>ACaptainInAChair/APlannedRoute</c> is byte-identical: that row walks, and #847 stands you up
    ///   before it does.</item>
    ///   <item><b><c>walked-view pen</c>, on the 15 rows that paint her deck</b> — every one by <b>exactly
    ///   +2760 calls</b>, and the three <c>APlannedRoute</c> rows (twice the frames) by exactly +5520. That
    ///   is <b>23 marks a frame</b> over 120 frames, and every one of the 23 was MEASURED rather than
    ///   reasoned about, by re-recording the row with each piece taken out: <b>10</b> for the two filled
    ///   fittings, a fill and four keyline segments each (39397 → 38197 with the furniture dropped);
    ///   <b>3</b> for the counter's service rail, #791's own rail and ticks (38197 → 37837 with the run
    ///   dropped); <b>6</b> for three added consoles at a marker and a label each; <b>3</b> for the three
    ///   stools; and <b>1</b> for the counter's wall. The same 23 shows up next door in
    ///   <c>EveryFrameHashesTheSameTests</c>, where <b>13 cases moved and every one moved by exactly
    ///   +23</b> — her own three, the seven havens and the three excursions that carry her deck.</item>
    /// </list>
    /// <para><b>No ledger row moved, no <c>map-frame buffer</c> moved, and not one wreck, B-floor or dark
    /// row moved at all.</b></para>
    ///
    /// <para><b>#949 · ALL 30 WERE RE-RECORDED, and it is the smallest kind of re-pin there is: ONE FIELD
    /// JOINED THE PAGE and nothing else in the game moved at all.</b> The <c>?</c> on the Nav toolbar
    /// stopped opening <c>/help/nav</c> in a second tab and raises the plotting card over the map instead,
    /// so <c>Map</c> gained <c>_navHelpOpen</c> — the card's gate — and the sweep counts one more field than
    /// it did.</para>
    ///
    /// <para><b>The diff is the old pins against the new ones, and it is 30 files × 1 line:</b> exactly one
    /// row kind moved, <c>sweep</c>, on every one of the thirty, with the field count <b>743 → 744</b>. Not
    /// one other line in any of the thirty texts differs — the ledger, the seat, the walked-view pen, the
    /// map-frame buffer and every call count are byte-identical. Which is what "a card nobody has opened
    /// paints nothing" has to look like when it is true.</para>
    ///
    /// <para>Proved a state change and not a behaviour change with this file's own
    /// <c>SPACESAILS_SWEEP_DUMP</c> hook rather than inferred from the count: the whole sweep was dumped and
    /// the new field found in it. <b>It is <c>_navHelpOpen</c>, present on all thirty rows and reading
    /// <c>no</c> on every single one</b> — no world in this matrix presses <c>?</c>, and none of them has
    /// grown a card it did not have. A field count that moved by one while some OTHER field's value had
    /// also changed would be a different lane's bug wearing this lane's clothes, and the dump is what tells
    /// those two apart.</para>
    /// </summary>
    [Theory]
    [InlineData(World.HerOwnDeckInFlight, Sequence.SteadyFrames)]
    [InlineData(World.HerOwnDeckInFlight, Sequence.OneLongGap)]
    [InlineData(World.HerOwnDeckInFlight, Sequence.AHeldKey)]
    [InlineData(World.HerOwnDeckInFlight, Sequence.APlannedRoute)]
    [InlineData(World.HerOwnDeckInFlight, Sequence.AHandOnTheWarpSlider)]
    [InlineData(World.TheRegolithOnFoot, Sequence.SteadyFrames)]
    [InlineData(World.TheRegolithOnFoot, Sequence.OneLongGap)]
    [InlineData(World.TheRegolithOnFoot, Sequence.AHeldKey)]
    [InlineData(World.TheRegolithOnFoot, Sequence.APlannedRoute)]
    [InlineData(World.TheRegolithOnFoot, Sequence.AHandOnTheWarpSlider)]
    [InlineData(World.AHiveFloorWithAPatrol, Sequence.SteadyFrames)]
    [InlineData(World.AHiveFloorWithAPatrol, Sequence.OneLongGap)]
    [InlineData(World.AHiveFloorWithAPatrol, Sequence.AHeldKey)]
    [InlineData(World.AHiveFloorWithAPatrol, Sequence.APlannedRoute)]
    [InlineData(World.AHiveFloorWithAPatrol, Sequence.AHandOnTheWarpSlider)]
    [InlineData(World.ACaptainInAChair, Sequence.SteadyFrames)]
    [InlineData(World.ACaptainInAChair, Sequence.OneLongGap)]
    [InlineData(World.ACaptainInAChair, Sequence.AHeldKey)]
    [InlineData(World.ACaptainInAChair, Sequence.APlannedRoute)]
    [InlineData(World.ACaptainInAChair, Sequence.AHandOnTheWarpSlider)]
    [InlineData(World.TheMapFrameInFlight, Sequence.SteadyFrames)]
    [InlineData(World.TheMapFrameInFlight, Sequence.OneLongGap)]
    [InlineData(World.TheMapFrameInFlight, Sequence.AHeldKey)]
    [InlineData(World.TheMapFrameInFlight, Sequence.APlannedRoute)]
    [InlineData(World.TheMapFrameInFlight, Sequence.AHandOnTheWarpSlider)]
    [InlineData(World.TheElectricUniverse, Sequence.SteadyFrames)]
    [InlineData(World.TheElectricUniverse, Sequence.OneLongGap)]
    [InlineData(World.TheElectricUniverse, Sequence.AHeldKey)]
    [InlineData(World.TheElectricUniverse, Sequence.APlannedRoute)]
    [InlineData(World.TheElectricUniverse, Sequence.AHandOnTheWarpSlider)]
    public void EveryFrameItRunsFingerprintsTheSame(World world, Sequence sequence)
    {
        string scene = SceneName(world, sequence);
        IReadOnlyDictionary<string, PinLedger.Row> pinned = PinLedger.Pinned(Suite);
        Reading got = DriveAndFingerprint(world, sequence);

        // The readable half first: name the ROW that moved, rather than printing two hashes that differ.
        foreach ((string probe, string value) in got.Rows)
        {
            Assert.True(pinned.TryGetValue(PinLedger.Key(probe, scene), out PinLedger.Row was),
                $"{Suite}.ledger.txt has no `{probe}` row for {scene} — that reading is asserting nothing "
                + $"at all. Take the measurement:\n  {PinLedger.Invocation}");
            Assert.True(was.Value == value,
                $"the frame no longer leaves the same mark on {world} / {sequence}.\n"
                + $"  {probe} was: {was.Value}\n"
                + $"  {probe} now: {value}\n"
                + WhichFieldMoved(probe, got)
                + "Nothing in this lane may change what a frame writes. If a phase was reordered, put it "
                + "back; the order IS the frame. If the change is intended, re-pin BY MEASUREMENT and paste "
                + $"the printed report into the PR:\n  {PinLedger.Invocation}");
        }

        // …and no pinned row for this scene went unmeasured, which is the other direction of the same law.
        var measured = new HashSet<string>(got.Rows.Select(r => r.Probe), StringComparer.Ordinal);
        string[] unmeasured =
        [
            .. pinned.Values.Where(r => r.Scene == scene && !measured.Contains(r.Probe))
                            .Select(r => r.Probe)
        ];
        Assert.True(unmeasured.Length == 0,
            $"{unmeasured.Length} row(s) pinned for {scene} were never measured, so they are green forever: "
            + string.Join(", ", unmeasured));
    }

    /// <summary>
    /// THE SWEEP'S ROSTER — every field of the page the sweep walks, pinned BY NAME.
    ///
    /// <para>#1055 · Requirement 4 on the issue, and the reason it exists: the <c>sweep</c> row is a COUNT and
    /// a hash, so when it moves, all a crew used to be told is "744 → 745". Naming the field cost a dump on
    /// the base, a dump on the lane and a line-by-line diff — every time. The roster is that answer, pinned:
    /// one row per swept field, so the day a field joins <see cref="Pages.Map"/> this test goes red saying
    /// <c>sweep +1: _navHelpOpen</c> and the thirty sweep rows go red beside it saying the same thing.</para>
    ///
    /// <para>It is also strictly MORE than the sweep hash could ever say: a field that changes TYPE while
    /// keeping its name reddens here too.</para>
    /// </summary>
    [Fact]
    public void TheSweepWalksTheRosterThatWasPinned()
    {
        IReadOnlyDictionary<string, PinLedger.Row> pinned = PinLedger.Pinned(Suite);
        IReadOnlyList<(string Field, string Type)> roster = SweptRoster();

        Assert.True(roster.Count > 500,
            $"the sweep found only {roster.Count} field(s) on the page — a sweep that walks nothing "
            + "cannot tell pass from fail.");

        string[] appeared =
        [
            .. roster.Where(f => !pinned.ContainsKey(PinLedger.Key(RosterProbe, f.Field)))
                     .Select(f => $"{f.Field} ({f.Type})")
        ];
        var present = new HashSet<string>(roster.Select(f => f.Field), StringComparer.Ordinal);
        string[] gone =
        [
            .. pinned.Values.Where(r => r.Probe == RosterProbe && !present.Contains(r.Scene))
                            .Select(r => $"{r.Scene} ({r.Value})")
        ];
        string[] retyped =
        [
            .. roster.Where(f => pinned.TryGetValue(PinLedger.Key(RosterProbe, f.Field), out PinLedger.Row p)
                                 && p.Value != f.Type)
                     .Select(f => $"{f.Field}: {pinned[PinLedger.Key(RosterProbe, f.Field)].Value} → {f.Type}")
        ];

        Assert.True(appeared.Length == 0 && gone.Length == 0 && retyped.Length == 0,
            $"the page's swept roster moved — {roster.Count} field(s) now, "
            + $"{pinned.Values.Count(r => r.Probe == RosterProbe)} pinned:\n"
            + (appeared.Length > 0 ? $"  sweep +{appeared.Length}: {string.Join(", ", appeared)}\n" : "")
            + (gone.Length > 0 ? $"  sweep −{gone.Length}: {string.Join(", ", gone)}\n" : "")
            + (retyped.Length > 0 ? $"  retyped: {string.Join(", ", retyped)}\n" : "")
            + "That is the whole of what a state-shape change looks like. If it is intended, re-pin BY "
            + $"MEASUREMENT and paste the printed report into the PR:\n  {PinLedger.Invocation}");
    }

    /// <summary>The snapshot is worth nothing if the bench cannot tell one world from another — a guard handed
    /// a world it built itself cannot tell pass from fail (this repo's fifth named bug class). Thirty rows,
    /// thirty different fingerprints.</summary>
    [Fact]
    public void EveryRowIsADifferentFrame()
    {
        var byScene = new SortedDictionary<string, StringBuilder>(StringComparer.Ordinal);
        foreach (PinLedger.Row row in PinLedger.Read(Suite).Where(r => r.Probe != RosterProbe))
        {
            if (!byScene.TryGetValue(row.Scene, out StringBuilder? text))
            {
                byScene[row.Scene] = text = new StringBuilder();
            }
            text.Append(row.Probe).Append(" = ").Append(row.Value).Append('\n');
        }

        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string scene, StringBuilder text) in byScene)
        {
            string hash = Sha256(text.ToString());
            Assert.False(seen.TryGetValue(hash, out string? twin),
                $"{scene} and {twin} produced the SAME fingerprint — two of these rows are " +
                "driving the same frame, so one of them could never fail.");
            seen[hash] = scene;
        }
        Assert.Equal(30, seen.Count);
    }
}
