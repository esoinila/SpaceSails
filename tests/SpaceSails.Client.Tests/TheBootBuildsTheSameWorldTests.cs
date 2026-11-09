using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 7a · THE BOOT BUILDS THE SAME WORLD IT ALWAYS DID.
///
/// <para><b>Why this file exists.</b> <c>BootTheWorldAsync</c> was one method of 1,656 lines: the whole
/// <c>?query</c> cheat surface, the scenario load, the appended cheat bodies, the ephemeris, four traffic
/// planners, the berth roster, the start and the renderer wiring, in one straight pass. Splitting it is a
/// BEHAVIOUR-BEARING refactor — a method is being cut, not a file — and the only honest way to cut it is
/// to write down what it builds FIRST, from the old code, and then require the new code to build it byte
/// for byte.</para>
///
/// <para><b>What a fingerprint is here.</b> Two <see cref="Pages.Map"/>s: one that never booted, one that
/// booted at the URL under test. Every instance field whose rendered value DIFFERS between them is the
/// boot's own work — the ship, the ephemeris, the traffic, the purse and the hold, the camera, the start
/// picker, the scenario name, and every one of the four dozen <c>_…Cheat</c> flags the query sets. Those
/// differences are rendered to a stable text (invariant culture, reals to five significant figures —
/// see <see cref="Real"/> for why not every bit — sets sorted, long
/// primitive arrays folded to a digest) and hashed. Diffing against a virgin component rather than naming
/// fields by hand is deliberate: a field the boot starts writing tomorrow enters the fingerprint on its
/// own, and a field the boot never touches never enters it at all.</para>
///
/// <para><b>Where the fingerprint is taken, and why not further.</b> At the throw. <c>#737</c>'s own
/// comment calls <c>abandoned.ThrowIfCancellationRequested()</c> before <c>CanvasRenderer</c> "THE LAST
/// GATE BEFORE THE DOM", and one line above it the boot awaits <c>RendererInterop.EnsureModuleLoadedAsync</c>
/// — <c>JSHost.ImportAsync</c>, which off a browser cannot be reached at all. That is not a limitation of
/// this bench, it is the documented shape of the page: <c>TheBootStopsWhenYouLeaveTests</c> asserts the
/// same wall from the other side ("a boot NOBODY left still goes all the way to the BROWSER"), by proving
/// the unabandoned boot THROWS. So the fingerprint covers the boot up to that gate — the query parse, the
/// berth defaults, the scenario load, every appended cheat body, the ephemeris, the ship, the purse, the
/// hold, the four traffic planners and the camera. What the boot does AFTER the gate (the start point and
/// the cheats that need a live world) is unreachable off-browser in any harness, and travels as a verbatim
/// move instead — see the PR body's line-multiset proof.</para>
///
/// <para><b>The parse loop's own answer</b> — the locals the 1,150-line <c>?query</c> chain writes, which
/// are invisible in the fields above until something after the gate consumes them — is pinned separately
/// by <see cref="TheBootReadsTheSameQueryTests"/>.</para>
///
/// <para><b>The pins were re-captured</b> each time <see cref="Real"/> changed, and always on the OLD
/// one-method boot — never on the new code. The recipe, run again after the base moved: the SHIPPING
/// <c>Map.Sim.World.cs</c> off <c>origin/our-own-ship-has-compartments</c> (1,680 lines, the ladder
/// entire) written back over this branch's copy, the six split partials DELETED, the dump taken, the
/// split restored. Nothing had to be set aside to compile it: the two sibling guards that name the new
/// members name them in a docblock and in a reflected string, neither of which is a compile-time
/// reference. <b>All 75 hashes came back identical to the pinned list, and 59 of the 75 were distinct
/// on the old code exactly as they are on the new.</b> The rounding folded no two worlds together at
/// any precision either — <b>59 of 75 distinct from G12 all the way down to G2</b>. All three REDs
/// below were re-measured under it and reddened the same URLs in the same numbers.</para>
///
/// <para><b>Red proof, twice, both quoted verbatim in the PR body.</b> Swap two stages in the conductor
/// and the hashes move. Moving <c>PointTheCameraAtHer</c> above <c>LayTheShipDownWithHerHistory</c> —
/// the camera aimed at a ship that has not been laid down yet — reddens <b>75 of 75</b>. Moving
/// <c>RaiseTheFrontDoorWhileTheReactorWarms</c> above <c>DefaultABerthForTheCheatsThatNeedOne</c> —
/// so the front door is decided before the cheats that need a berth have invented theirs — reddens
/// exactly <b>4 of 75</b>, and they are exactly the four URLs where a cheat has to invent a berth
/// (<c>?bond=1</c> twice, <c>?ashore=1</c>, <c>?death=impact</c>). Both numbers matter: the first says
/// the sweep sees the whole boot, the second says it is not merely sensitive to everything.</para>
///
/// <h3>#323 · RE-PINNED, 54 OF 88 — AND EVERY ONE OF THEM BY ONE FIELD</h3>
///
/// <para>#323 made the front door's raise ask the <b>civilian</b> question (<c>Map.TheQueryIsCivilian</c>:
/// a query goes through the logbook iff it carries nothing but <c>scenario</c>) instead of asking whether
/// four particular cheat keys were absent. This sweep's fingerprint is taken at the browser gate, which is
/// where the raise has always happened and four stages before any start is applied — so it sees the door's
/// state <i>before</i> <c>StartDockedAtHaven</c> would have lowered it again, and every URL whose answer to
/// the new question differs moves.</para>
///
/// <para><b>Proved with this file's own <c>SPACESAILS_BOOT_FINGERPRINT_DUMP</c> hook</b>, run on the base
/// and on the change and diffed line by line. The base dump is <b>2,135</b> lines and the new one is
/// <b>2,081</b>; the diff is <b>54 lines, all removals, and every single one of them is
/// <c>_showStartPicker</c></b>. Not one other field line moved — not a body, not a berth, not a purse, not
/// a traffic plan. They are removals rather than changes because this fingerprint is a DIFF against a
/// virgin page: the door is now shut on these URLs exactly as it is on a page that never booted, so the
/// field stops differing and drops out of the text altogether.</para>
///
/// <para>The 54 are exactly the URLs that <b>carry a cheat and name no berth in the query itself</b> —
/// <c>?converge=1</c>, <c>?credits=50000</c>, <c>?kaamos=all</c>, <c>?park=1</c>, <c>?secretlab=1</c>,
/// <c>?found=1&amp;land=1</c>, <c>?threads=1</c> and the rest. The 34 that did NOT move are the other two
/// kinds, and both are the law working: every <c>?dock=</c>/<c>?start=</c>/<c>?sling=</c>/<c>?skim=</c> URL
/// (the old condition already shut the door on those, and the civilian rule shuts it too), and the
/// genuinely civilian ones — <c>/map</c> and <c>/map?scenario=..%2Foops</c> (then also <c>?scenario=sol-eu</c>, a sky since retired) —
/// whose door is open before and after. A re-pin that had moved <c>/map</c> itself would have been this
/// lane breaking the front door it was written to protect.</para>
///
/// <para><b>…and the second of the two REDs above is now HISTORY</b>, left standing as history rather than
/// quietly deleted. It was a true measurement of the boot as it stood: the raise read <c>q.DockCheat</c>,
/// so a berth a cheat had not yet invented moved four doors. The raise no longer reads the parse at all,
/// so re-running that swap today would redden nothing — a reader who took it for a live claim would be
/// measuring a dependency that has been removed. #323's own red is quoted in its PR body and is the
/// <i>change</i> rather than a swap: restoring the old four-key condition reddens exactly these 54.</para>
///
/// <h3>#957 · RE-PINNED, 81 OF 82 — AND IT IS THE WORLD-DATA KIND OF CHANGE</h3>
///
/// <para>#957 corrected three hand-typed orbit periods in <c>scenarios/sol.json</c>: Cinder Roost, The
/// Rusty Roadstead and The Tilt were on rails no gravity explains, whipping round their parents at 11.8,
/// 10.5 and 35.9 km/s where Newton allows 4.7, 1.9 and 8.5 — which is why the autopilot could not dock at
/// them (the clamp shears above 8 km/s). The boot loads the scenario, so the fingerprint moves.</para>
///
/// <para><b>It was proved a data change and not a behaviour change with this file's own
/// <c>SPACESAILS_BOOT_FINGERPRINT_DUMP</c> hook</b>, run on the old literals and on the new and diffed line
/// by line. Both dumps are 1,913 lines. <b>Exactly two field lines per URL differ, and no others:</b></para>
/// <list type="bullet">
///   <item><b><c>_ephemeris</c></b> — the three <c>OrbitPeriod=</c> tokens themselves, and nothing else in
///   the body table: same bodies, same order, same radii, same phases, same μ.</item>
///   <item><b><c>_npcStates</c></b> — <b>3 of the 34</b> NPC records, and in each one the only thing that
///   moved is <c>InitialState.Velocity</c>; every <c>Position</c> is byte-identical, because at t=0 the
///   rails have not turned yet. They are the three traders parked at the three berths, and they were being
///   flung along at the berth's fake rail speed: 46,101 → 39,349 m/s at Cinder Roost, 34,020 → 25,880 at
///   The Rusty Roadstead, and 41,344 → 14,401 at The Tilt — a ship "parked" at Uranus had been travelling
///   faster than Mercury.</item>
/// </list>
///
/// <para>Two structural facts say the boot itself did not move. <b>The equality partition is preserved
/// exactly</b>: 63 distinct hashes before and 63 after, over the same 82 URLs, grouping the same URLs
/// together — the boot has not started distinguishing or conflating any two worlds. And <b>one URL is
/// unchanged, <c>/map?scenario=sol-eu</c></b> — the one scenario in the list that had none of the three
/// berths in it (the sky was retired on 2026-11-09 and the row dropped; see the re-pin note at the foot of
/// this block). A re-pin that had moved a third field or split a hash group would have been a different
/// lane's bug wearing this lane's clothes.</para>
///
/// <h3>2026-11-09 · ONE SKY LEFT — ALL 123 HASHES RE-PINNED, BY THE SAME ONE CAUSE</h3>
///
/// <para>The Wheel, Sol (Electric) and Miners' Folly scenarios and the Electric Universe layer were deleted,
/// and with them the fields the fingerprint text printed: the page's <c>_plasma</c> and the charge-board
/// state, and the <c>Charge=0</c> in every printed <c>ShipState</c>. So the text moved for every URL and
/// every one of the 123 hashes moved, with <b>nothing else in it differing</b> — the ship's position,
/// velocity and clock print byte-identical. The proof is the structure, not the hex: the equal-hash
/// grouping is unchanged at <b>68 groups</b>, so no two worlds were conflated or split, and the dump (run with
/// <c>SPACESAILS_BOOT_FINGERPRINT_DUMP</c>) contains no <c>Charge</c> at all. The <c>?scenario=sol-eu</c> row
/// is gone with its sky; <c>?scenario=..%2Foops</c> stays, as a sanitizer row that now simply boots Sol.</para>
///
/// <h3>#640 · EIGHTY-FIVE UNCHANGED, ONE ADDED</h3>
///
/// <para>The dev door onto the death where nobody comes (<c>?nopattern=1&amp;death=impact</c>) joins the
/// sweep, and <b>exactly one line moved: its own, and it was an ADDITION.</b> That is what a new URL is
/// supposed to look like here, and it is the structural proof that <c>?nopattern=</c> built no new world:
/// the flag it sets lives on the NEBULA progress holder, which this fingerprint does not read, and the key
/// appends no body, moves no berth and spends nothing — so it hashes to the byte identically to
/// <c>?death=impact</c>. The query sweep next door re-pinned all 86 for the fourth time, for the reason
/// written in its own docblock: a new <c>BootQuery</c> field joins the text of every URL.</para>
///
/// <h3>#711 slice 2 · EIGHTY-SIX RE-PINNED, AND EVERY ONE MOVED BY THE SAME ONE LINE</h3>
///
/// <para>A dead drop has to know which ground a job may name, and it takes that from the SCENARIO —
/// snapshotted at boot, one line above the cheats that append moons of their own
/// (<c>Map.RememberTheGroundADropMayName</c>). A new page field joins the fingerprint of every URL, so all
/// 86 hashes move; nothing about the world does.</para>
///
/// <para><b>Proved with this file's own <c>SPACESAILS_BOOT_FINGERPRINT_DUMP</c> hook</b>, run on
/// <c>origin/our-own-ship-has-compartments</c> and on this branch and diffed line by line. 1,995 lines
/// before, 2,081 after: <b>86 added lines, ZERO removed and ZERO changed</b> — one per URL, and it is the
/// same line every time:</para>
///
/// <code>_groundADropMayName = ["the-clinker", "luna", "phobos", "europa", "ganymede", "callisto",
/// "titan", "enceladus", "miranda", "triton"]</code>
///
/// <para>The ten moons <c>sol.json</c> ships, in the file's own order, on every boot — the cheat URLs
/// included, which is the whole claim the snapshot makes. And the structural fact that says the boot itself
/// did not move: <b>the equality partition is preserved exactly — 63 distinct hashes before and 63
/// after</b>, over the same 86 URLs, grouping the same URLs together. No world was split off from another
/// and none was folded into one.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[SlowGate] // #251 · 148 s over 3 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheBootBuildsTheSameWorldTests
{
    /// <summary>Set to a file path to DUMP every URL's rendered fingerprint text instead of asserting —
    /// how the hashes below were captured off the old code. Never set in CI.</summary>
    private const string DumpVariable = "SPACESAILS_BOOT_FINGERPRINT_DUMP";

    // ── The pinned world, one sha256 (first 32 hex) per URL, captured from the OLD one-method boot ───────
    private static readonly IReadOnlyDictionary<string, string> TheWorldEachUrlBuilds =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["/map"] = "f42ee34e9f03f95028f1c4fe714af227",
            ["/map?archive=1&land=1&nerve=2"] = "003c75153e82a40fd22db0f5c023eb9c",
            ["/map?ashore=1&kaamos=bounce"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?ashore=1&start=space-bar"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #870 lane 6′b · RE-PINNED, and these two rows only. The patrol's twenty-two fields
            // became properties on one `_patrol` object, and this sweep diffs a booted page against a
            // virgin one — so the only URLs it can move are the ones where the boot writes patrol state
            // at all, which is exactly `?badge=1` and `?patrol=2`. Dumped from both trees with
            // SPACESAILS_BOOT_FINGERPRINT_DUMP: 2 of 75 rows differ, and in the built world the pair
            // `_badgeCheat = yes` / `_patrolCheat = 2` is now the one line
            // `_patrol = Patrol(BadgeCheat=…, RoundsCheat=…, …)`, carrying the same two values. The
            // verbatim diff is in #870 lane 6′b's PR body. Nothing about what the boot DOES moved.
            //
            // #870 lane 6′c · RE-PINNED AGAIN, THE SAME TWO ROWS AND FOR THE SAME KIND OF REASON. The
            // round's VERBS moved onto Patrol, and two of them are READS the page forwards to —
            // `TheWalletFan` and `WalletFanIsUp`. This sweep renders an object's PUBLIC members, so those
            // two now appear inside the one `_patrol = Patrol(…)` line and nowhere else. PROVEN, rather
            // than asserted: with those two spelled `internal` instead (identical reach inside a private
            // nested class, invisible to a BindingFlags.Public sweep) both URLs dump the OLD hashes back,
            // exactly — 41e90d67… and 0cbbfc12…. The whole difference is two entries:
            //     …TheSitePassIsMintedAtTheLanding=true,  TheWalletFan=[],  WalkedAwayThisWatch=0,
            //     …WalkedPastSaid=false,  WalletFanIsUp=false,  WalletFanOpen=false, …
            // and the fix was NOT to hide from the sweep. A guard that sees more of the world is doing its
            // job; a keyword chosen to keep it from noticing is this repo's fifth named bug class wearing
            // a tidy hat. Nothing about what the boot DOES moved: the same query writes the same two values.
            //
            // #618 · RE-PINNED A THIRD TIME, THE SAME TWO ROWS, THE SAME KIND OF REASON — and the reason it
            // is still only these two is the reason the note above gives: they are the only URLs where the
            // boot writes patrol state at all. The round grew three members for a shot being heard
            // (LookingIntoIt, TheNoise, ShotsAnswered) and this sweep renders the whole object, so they show
            // up inside the one `_patrol = Patrol(…)` line. Dumped from both trees with
            // SPACESAILS_BOOT_FINGERPRINT_DUMP and compared line by line: both dumps are 1,825 lines, exactly
            // TWO of them differ, both are `_patrol`, and in neither was anything REMOVED or CHANGED — the
            // whole difference is
            //     …KickedOutPlateFor=0,  LookingIntoIt=∅,  PaperInHand=∅, …
            //     …RoundsCheat=1,  ShotsAnswered=0,  ShownBook=[],  TheNoise=(0, 0), …
            // every one at its default, because a boot has not fired anything. Nothing about what the boot
            // DOES moved: the same query writes the same values it always did.
            //
            // #746 · RE-PINNED A FOURTH TIME, AND A THIRD ROW JOINS THEM. The checkpoint is an
            // ENCOUNTER now, so the round grew the state one needs — StopUnderway, the two sets a man
            // remembers you by (AskedTheWay, FumbledAtTheStop), EscortIsFree, NameInTheBookDue and the
            // ?roll= cheat — and this sweep renders the whole object inside the one `_patrol = Patrol(…)`
            // line. Every one of them is at its DEFAULT in all three dumps, because a boot has not
            // stopped anybody: nothing about what these queries DO moved. The third row is the new one
            // and it is new for a different reason, said below on its own line.
            ["/map?badge=1"] = "d99d5be4832e235a9236ebae4dfeb4ea",
            ["/map?barcase=1"] = "1fbc41ee5adabef43b98e8aeee4de5c7",
            ["/map?bond=1"] = "c9aaf8d2f8cfe4651e412096e0759dbe",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "685f161a5b3a51ffb4e6493173ce8abb",
            // #1151 slice 1 · ?claim=1: MEASURED with SPACESAILS_BOOT_FINGERPRINT_DUMP; 1 new, 0 moved.
            ["/map?claim=1"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #1151 slice 2 · ?claim=2: MEASURED with SPACESAILS_BOOT_FINGERPRINT_DUMP; 1 new, 0 moved (the same world as ?claim=1: the
            // staging is the page's, written when the floor is first drawn, never at parse time).
            ["/map?claim=2"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #1151 slice 3 · ?claim=3 and ?claim=3&signing=1: MEASURED with SPACESAILS_BOOT_FINGERPRINT_DUMP; 2 new, 0 moved (the same world again: the
            // fixed booking moment and the held sheet are the page's staging, written when the floor is first drawn).
            ["/map?claim=3"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?claim=3&signing=1"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?converge=1"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?counter=1"] = "9bc0c82585919e4daf5d18aec21b597c",
            ["/map?counter=1&watch=2"] = "4ccaff4902dedbb851d17417010f06ce",
            ["/map?counter=1&watch=5"] = "61399344d77b43a6c21743c54ff1c1b2",
            ["/map?credits=1234&fuel=7&simhours=9"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?credits=50000"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #663 · …and it hashes the BARE WORLD, which is correct and worth saying out loud: ?crew= grants
            // two counters on the crew sheet, appends no body, moves no berth and spends no money, and it is
            // seeded in SeedTheArcsAndTheJobs — well past the browser gate this sweep stops at. What the URL
            // ANSWERS differently is pinned next door in TheBootReadsTheSameQueryTests; what it LEADS to is
            // proved on the shipping sheet, in TheCrewSheetCountsTheDeadTests.
            // #1066 · the meeting's door, one landing down the same staircase — and it hashes the bare world
            // to the BYTE, identically to ?crew=petition beneath it. That is the sentence above said twice:
            // both doors grant counters only, in SeedTheArcsAndTheJobs, long past the browser gate this
            // sweep stops at. Where the two DO diverge is next door in TheBootReadsTheSameQueryTests, whose
            // holder carries "meeting" rather than "petition".
            ["/map?crew=meeting"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?crew=petition"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?death=collector&dock=selene-gate"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?death=impact"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "2576c66d224f442454cfe7a92c8e9414",
            ["/map?deflection=1"] = "602d0868efde57a78c1dfe6804696732",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "300e76d9f00a0f0ebf26752f0ce517e1",
            ["/map?designate=1"] = "ea11f5afb74199317b83d219a31acfbc",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "7835bd152a9ba4f59851d7f95da94e97",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "ce7d18a56c11f6a047ae27fedaf7629a",
            // #1253 · DOWN BELOW's own door. It builds the FRONT DOOR'S world to the byte, and that is
            // right rather than suspicious: this sweep stops at the browser gate, and `?ashore=1` and
            // `?havenfloor=` both do their work after it — the walk and the ride happen to a world that is
            // already built. What the two of them ANSWER is pinned next door, in
            // TheBootReadsTheSameQueryTests, where this URL is its own distinct reading.
            ["/map?dock=selene-gate&ashore=1&havenfloor=-1"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #1332 B · the garden behind glass: the front door's world, like every ashore row — dumped, not typed.
            ["/map?dock=selene-gate&ashore=1&garden=1"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #1332 A · EVERY HUB IS A LOBBY — the six floors the other hubs grew, one dev row each. Dumped with
            // SPACESAILS_BOOT_FINGERPRINT_DUMP, not typed: each builds the front door's world for the reason
            // Selene Gate's row above does, and no other row of the dump moved.
            ["/map?dock=the-space-bar&ashore=1&havenfloor=-1"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #1332 B · the garden behind glass: the front door's world, like every ashore row — dumped, not typed.
            ["/map?dock=the-space-bar&ashore=1&garden=1"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=cinder-roost&ashore=1&havenfloor=-1"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=ringside-exchange&ashore=1&havenfloor=-1"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #1332 C · the Preservation office, shut and on the clerk's watch: MEASURED with SPACESAILS_BOOT_FINGERPRINT_DUMP; 2 new, 0 moved.
            ["/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=open"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=shut"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #1332 D/E · the forwarding desk and the adjuster's room, shut and ajar: MEASURED with SPACESAILS_BOOT_FINGERPRINT_DUMP; 4 new, 0 moved.
            ["/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=open"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=shut"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=the-deep&ashore=1&havenfloor=-1&office=open"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=the-deep&ashore=1&havenfloor=-1&office=shut"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=the-tilt&ashore=1&havenfloor=-1"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=red-eye&ashore=1&havenfloor=-1"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=the-deep&ashore=1&havenfloor=-1"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #794 slice 2 · the chalk mark's two dev rows, moved from ?park=1 to the gallery with the drop.
            // ?chalk= writes no world and nothing the parse answers (it is read off the address bar after
            // ?ashore=1 walks the captain in), so both are the berth's own line — the park's two rows went,
            // these two came, and no other line moved.
            ["/map?dock=selene-gate&ashore=1&chalk=1"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=selene-gate&ashore=1&chalk=wiped"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #1202 slice 2 · the spike's two dev rows. ?spike= writes no world and nothing the parse answers (it is
            // read off the address bar once ashore) — the chalk rows' own reason, and their pin. No other line moved.
            ["/map?dock=selene-gate&ashore=1&spike=1"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #1202 slice 3 · the spike composed with ?tailed=1. Measured: the one field the dump adds over the spike=1
            // row is `_tailedCheat = true` (the parse sets it before the gate); no other line moved.
            ["/map?dock=selene-gate&ashore=1&spike=1&tailed=1"] = "289b7a76906458fb0f3e7cac5ca2df08",
            ["/map?dock=selene-gate&ashore=1&spike=spiked"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #1202 slice 4 · ?spike=paid — the same latch, read once ashore: measured, the spike rows' own line. No other line moved.
            ["/map?dock=selene-gate&ashore=1&spike=paid"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "58f5dfc8d998ea6425cefa3d6e6a8d3b",
            // #319 slice 2 · the geocache sale's two dev rows. ?geocache= writes no world and nothing the parse
            // answers (it is read off the address bar once the berth is clamped) — dumped and diffed: these two
            // lines are the only thing the dump added, and no other line moved.
            ["/map?dock=selene-gate&geocache=1"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=selene-gate&geocache=lifted"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #1202 · the stringer's two dev rows. ?press= writes no world and nothing the parse answers (it is
            // read off the address bar once the berth is clamped) — the geocache rows' own reason, and their pin.
            ["/map?dock=selene-gate&press=1"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=selene-gate&press=filed"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #1202 slice 2 QA · ?press=pending — the same latch, read after the clamp: the berth's own line, measured. No other line moved.
            ["/map?dock=selene-gate&press=pending"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #997 wave 10 · see /map?start=wreck&target=collector further down — the new dev start moved
            // free-flying after a browser walk, and the reason is written there.
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "94ad2494a6f1a3e87001c1d1be3706c7",
            ["/map?dock=the-space-bar"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "e1711c7606adef860877ecfb3a7b6fbc",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "456ee3726993c7f3a84dc280bb4c3d54",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "4400ee60114173973b4e4aeced5aff82",
            ["/map?dock=the-tilt&site=0"] = "60ee784f5515eb47e80d36f358032193",
            ["/map?dock=the-tilt&site=0&land=1"] = "a84a6dcc0f5ccf021ace147108aee666",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "cba6fdf7df7f6d69b96cb2568f3efb47",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "d100118340445efd9ba3c8d7d7d95396",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "772b4af5f689ee40e71533107a88606a",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "715cd6a7c4330aa11130c2d686a69654",
            ["/map?dock=the-tilt&site=1"] = "548eb3ef7cfa5252698914066895909b",
            ["/map?dock=the-tilt&start=space-bar"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?expedition=mining"] = "ee2cb74d0284c7a4bb022349b455ba2d",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?found=1&land=1"] = "fadf506892995e6f2c1375582201fa3c",
            ["/map?found=1&land=1&floor=17&card=all"] = "1c486100e55fae952da90a2a99a830f4",
            ["/map?freight=1"] = "ab56cca97c690e04e4eb6a4c9ba54372",
            ["/map?frontdoor=1"] = "50d4866d453bbc75d7deb7be2b06ddfb",
            ["/map?goodscar=1"] = "102acd75466426195bafc567761fb298",
            ["/map?kaamos=all"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "89f0f96480f41ac8a34ea515e93bb31f",
            ["/map?kaamos=hq&land=1"] = "2576c66d224f442454cfe7a92c8e9414",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "149f78881b73616355af8fa90d832124",
            ["/map?nebula=all"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #640 · the door onto the death where nobody comes, and it hashes IDENTICALLY to
            // ?death=impact above — which is correct and worth saying out loud. ?nopattern=1 closes the
            // policy on the live NebulaProgress, which is a holder this fingerprint does not read, and it
            // appends no body, moves no berth and spends no money. What the URL ANSWERS differently is
            // pinned next door in TheBootReadsTheSameQueryTests; what it LEADS to is proved on the
            // shipping card, in TheDeathWhereNobodyComesTests, which presses the real "…wake up".
            ["/map?nopattern=1&death=impact"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?oldcrew=1"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?park=1"] = "674111051f177dc183a39637c6b6c114",
            // #759 · TWO NEW ROWS, AND NOTHING ELSE MOVED. `?parkphase=` jumps the sim clock so the park at
            // THIS site is at a chosen point of its own grow-cycle — and because it is a clock jump, these
            // are the only two URLs in the catalogue whose booted world differs from `?park=1`'s at all.
            // Dumped with SPACESAILS_BOOT_FINGERPRINT_DUMP and diffed against the pinned dictionary:
            // 86 rows in, 88 rows out, the two below ADDED and not one existing row changed by a character.
            // The five fields this lane puts on the page (the beat's three-field memory of what the two
            // clocks said at the gate, and the cheat's two) are all at their defaults on a fresh boot, and
            // this sweep diffs a booted page against a virgin one — so they are invisible to it by
            // construction, which is why `?park=1` itself is byte-identical.
            ["/map?park=1&parkphase=morning"] = "d05a03bf04f5f90adf32fbb1668ac7d4",
            ["/map?park=1&parkphase=night"] = "a52fef043c902472cab024b2b03c590c",
            ["/map?park=1&spread=1"] = "8acdfbafdddf9265535843fc866c6725",
            ["/map?parkback=1"] = "ac1b9986f9b76a1c77a8f60480f5022f",
            ["/map?parkwalk=1"] = "033730111f5aea070b7942bfea36d103",
            // #870 lane 6′b · RE-PINNED — see the note above `?badge=1`; same one reason.
            ["/map?patrol=2"] = "ceeeafaaaecbe135b0aeac89856234b4",   // #618/#746 · see ?badge=1 above
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "6bf776e9f53078c0f56723bc6a6e0f70",
            ["/map?ringoffice=1"] = "01a606f19f86caec8d7bf297b9af63b6",
            ["/map?rip=1"] = "3f2f4eecda490d5b54c24fa7e5d9eafb",
            ["/map?scenario=..%2Foops"] = "f42ee34e9f03f95028f1c4fe714af227",
            ["/map?secretlab=1"] = "15ff13d20c76750c0fa871513dcc266e",
            // #618 · the man at the door, three windows: MEASURED with SPACESAILS_BOOT_FINGERPRINT_DUMP; 3 new, 0 moved.
            ["/map?secretlab=1&land=1&guard=absent"] = "0ee0cd9e4da2748937c19a5da55b6434",
            ["/map?secretlab=1&land=1&guard=posted"] = "0ee0cd9e4da2748937c19a5da55b6434",
            ["/map?secretlab=1&land=1&guard=round"] = "0ee0cd9e4da2748937c19a5da55b6434",
            ["/map?secretlab=deep&land=1&card=next"] = "f58ef5db10d49fb040a1933af7c78afd",
            ["/map?secretlab=deep&land=1&floor=1"] = "39b6e467188e2ca0743d1ea2992e2e05",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "bcf7faad2581fad19ba95067e02b197c",
            // #841 · ?perf=1 arms a stopwatch on the DeckView and touches nothing the boot builds, so this
            // row is — and must stay — byte-for-byte the world `?secretlab=deep&land=1&floor=1` builds two
            // lines above. That it is IDENTICAL is the assertion: a measurement cheat that moved the world
            // would be measuring a world nobody plays.
            ["/map?secretlab=deep&land=1&floor=1&perf=1"] = "39b6e467188e2ca0743d1ea2992e2e05",
            // #746 · THE THIRD ROW, and the only one of the eighty-eight that moved for a reason other
            // than a default. `?roll=` reaches the ROUND now as well as the table — the stop is an
            // encounter and a cheat that forced one scene's bands and not the other's would leave a
            // tester unable to walk the four outcomes of the stop #746 is named after — so this boot
            // writes `RollCheat=NoAnd` where it used to write `RollCheat=∅`. That is the WHOLE
            // difference, and it is the thing the query is for.
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "a5aa564eb223bca2663df86b6bc0836b",
            ["/map?secretlab=deep&land=1&floor=21"] = "e86b8752591d5978bc66e6b5b6c686c0",
            // #619 · the fourth cheat rock. Measured, never transcribed from a guess: the dump run put one
            // new row in this dictionary and moved none of the other 88.
            ["/map?secretlab=sealed&land=1&floor=3"] = "ea34e6864ebe02ce567c6cfcd5035883",
            ["/map?shuttle=1&land=1"] = "fb076e31c84937aeb3a12592371cd599",
            ["/map?skim=saturn"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?sling=jupiter"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?spread=1"] = "f63f4ec2f6da8c8198d51c33691f658e",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "c5a02113efa8bb5948cf2674da21a9f5",
            ["/map?start=wreck&fetch=active"] = "005aecc39f1af93b358bb50d3e2e2996",
            ["/map?start=wreck&dest=saturn"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #997 wave 10 · The new dev start: ?target=collector, the dossier's own door. Read off the
            // dump and diffed against the pinned list — this is the ONLY line the dump adds, and no other
            // moved: the 32nd BootQuery field is something the PARSE answers, not a world the boot builds.
            //
            // It hashes the same as its neighbours, which is correct and worth saying out loud rather than
            // glossing: ?start= and ?target= are BOTH spent after the browser gate (ApplyTheStartPoint and
            // SeedTheArcsAndTheJobs), so this sweep sees neither the roadster nor the muscle. What these
            // URLs answer differently is pinned next door, in TheBootReadsTheSameQueryTests.
            ["/map?start=wreck&target=collector"] = "005aecc39f1af93b358bb50d3e2e2996",
            // #653 · the dead station's dev door: MEASURED with SPACESAILS_BOOT_FINGERPRINT_DUMP; 1 new, 0 moved, 0 gone — and this row RE-MEASURED when the station was named Ledger Point (the body name is in the built world).
            ["/map?station=1&land=1"] = "903f994bafaaf24f97913ef26e83a5bc",
            ["/map?stool=1&neighbour=0"] = "a116fa255d2b834b9747aa60203b63a2",
            ["/map?stool=1&neighbour=1"] = "49f877362ac37c6464bd46667e36cb06",
            ["/map?tablescene=free&approach=1"] = "40f2f7e4c7e0b201172a5ab82679b9f5",
            // #973 L2 · the Nebula rep on the rota, forced. Taken with SPACESAILS_BOOT_FINGERPRINT_DUMP and
            // diffed against the pinned list: this is the ONLY line the dump adds, and no other moved.
            ["/map?tablescene=free&rep=1&approach=0"] = "70759768d232417c5cdb1f1daa86808d",
            ["/map?tablescene=free&watch=5&approach=0"] = "bf900f43c49bad991bf6ef4b76fbef60",
            ["/map?threads=1"] = "e29dcb9d17e08ccd78c4f9503271ac06",
            ["/map?threads=1&watch=5"] = "a58954d30f0c58f8151ff28046fa90f7",
            ["/map?wreck=drivefailure&land=1"] = "47912eb80107eb2dd1e327a27952f20e",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "24062082a8a66003b4fec1f0da066841",
        };

    /// <summary>The bare front door, plus every dev URL the game itself offers, plus a set of hand-picked
    /// combinations that exercise the query keys no DevStart happens to use and the reading orders the
    /// boot's own comments call load-bearing (<c>?dock=</c> before <c>?start=</c>, <c>?found=</c> implying
    /// <c>?secretlab=</c>, <c>?threads=</c> implying <c>?spread=</c> implying <c>?tablescene=</c>).</summary>
    public static IEnumerable<string> EveryBootUrl() =>
        new[] { "/map" }
            .Concat(SpaceSails.Core.DevStarts.All.Select(e => e.Url))
            .Concat(HandPicked)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(u => u, StringComparer.Ordinal);

    private static readonly string[] HandPicked =
    [
        // the keys that write only a local — invisible in the fields, and the reason the query guard next
        // door exists — booted anyway, because they also steer which bodies get appended and where.
        "/map?credits=1234&fuel=7&simhours=9",
        "/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest",
        "/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1",
        "/map?sling=jupiter",
        "/map?skim=saturn",
        "/map?start=wreck&fetch=active",
        // #956 · the nav destination's own dev door — the key that lets a browser gate reach a state whose
        // only other road is a body menu drawn on the CANVAS, where Playwright has no DOM to click. Like
        // ?start= and ?target= beside it, it is SPENT AFTER the gate this sweep stops at (its work happens in
        // SeedTheArcsAndTheJobs), so the world here is its neighbours' world and the line hashes the same —
        // said out loud rather than glossed. What the URL ANSWERS differently is pinned next door, in
        // TheBootReadsTheSameQueryTests; what it BUILDS is proved where it can be, on the screen, in
        // TheFollowDestButtonIsRealTests.
        "/map?start=wreck&dest=saturn",
        // the orders the boot calls load-bearing
        "/map?dock=the-tilt&start=space-bar",
        "/map?ashore=1&start=space-bar",
        "/map?death=suffocated&dock=the-tilt&land=1",
        "/map?death=impact",
        "/map?found=1&land=1&floor=17&card=all",
        "/map?threads=1&watch=5",
        // the sanitizers, and a query of nothing this page has ever heard of
        "/map?scenario=..%2Foops",
        // …every key handed nothing. NOT `?scenario=`: an empty scenario name passes the slug check
        // (vacuously — "" is all-ASCII-letters-or-digits) and the boot then fetches `scenarios/.json`,
        // 404s and dies before it builds anything. That is the shipped behaviour on this base and this
        // lane does not change it; it is written up in the PR body instead of pinned here.
        "/map?start=&dock=&fuel=&nerve=&site=&land=",
        "/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0",
        // the excursion dials nothing else sets
        "/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low",
        "/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1",
        "/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4",
        "/map?archive=1&land=1&nerve=2",
        "/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1",
        "/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all",
        "/map?kaamos=pod&nebula=adjuster&arrivalphase=7",
    ];

    // ── The guard ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EveryBootUrlBuildsTheWorldItAlwaysBuilt()
    {
        string? dump = Environment.GetEnvironmentVariable(DumpVariable);
        var dumped = new StringBuilder();
        var wrong = new List<string>();
        var seen = new List<string>();

        foreach (string url in EveryBootUrl())
        {
            string rendered = await TheWorldBuiltBy(url);
            string hash = Sha256(rendered);
            seen.Add(url);

            if (dump is not null)
            {
                dumped.Append("            [\"").Append(url).Append("\"] = \"").Append(hash).Append("\",\n");
                File.AppendAllText(dump + ".full", $"───── {url}\n{rendered}\n\n");
                continue;
            }

            Assert.True(TheWorldEachUrlBuilds.ContainsKey(url),
                $"{url} is not pinned. A new boot URL must be fingerprinted before it can be trusted.");
            if (!string.Equals(TheWorldEachUrlBuilds[url], hash, StringComparison.Ordinal))
            {
                wrong.Add($"{url}\n  pinned {TheWorldEachUrlBuilds[url]}\n  built  {hash}\n"
                    + Indent(rendered));
            }
        }

        if (dump is not null)
        {
            File.WriteAllText(dump, dumped.ToString());
            return;
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} of {seen.Count} boot URLs no longer build the world they built before the "
            + "split. Every line below is a field whose value the boot changed:\n\n"
            + string.Join("\n\n", wrong));
    }

    [Fact]
    public void ThePinnedListIsTheWHOLEDevStartCatalogue()
    {
        // The fifth bug class, applied to this file: a fingerprint sweep over an empty list is green and
        // says nothing. Every URL the game's own front door offers has to be in the pinned dictionary, and
        // the dictionary may not carry a URL nobody boots.
        string[] booted = [.. EveryBootUrl()];

        Assert.All(SpaceSails.Core.DevStarts.All.Select(e => e.Url).Distinct(StringComparer.Ordinal),
            url => Assert.Contains(url, booted, StringComparer.Ordinal));
        Assert.Equal(
            booted.OrderBy(u => u, StringComparer.Ordinal),
            TheWorldEachUrlBuilds.Keys.OrderBy(u => u, StringComparer.Ordinal));
        Assert.True(booted.Length >= 60, $"only {booted.Length} boot URLs — the sweep has shrunk.");
    }

    [Fact]
    public async Task TheFingerprintCanTellTwoWORLDSApart()
    {
        // …and the other half of the same law: a fingerprint that answered the same thing for every URL
        // would pin nothing at all. Two boots that build genuinely different worlds must differ, and the
        // SAME boot twice must not — which is also this sweep's determinism proof.
        string bare = await TheWorldBuiltBy("/map");
        string again = await TheWorldBuiltBy("/map");
        string elsewhere = await TheWorldBuiltBy("/map?dock=the-tilt&site=0&land=1");

        Assert.Equal(Sha256(bare), Sha256(again));
        Assert.NotEqual(Sha256(bare), Sha256(elsewhere));
    }

    // ── The bench ────────────────────────────────────────────────────────────────────────────────────

    internal const BindingFlags Hidden = TestTree.AnythingOnAnInstance;

    /// <summary>Boot the SHIPPING component at <paramref name="url"/> and render everything it changed.
    ///
    /// <para>The boot ends in a throw, always and by design: its last stage names DOM by id through
    /// <c>renderer.js</c> and there is no browser under a test runner. That throw is the fingerprint's
    /// horizon, not its failure — but a throw from anywhere EARLIER would be a boot that never built a
    /// world, so the world the bench renders is checked for a pulse before it is hashed.</para></summary>
    private static async Task<string> TheWorldBuiltBy(string url)
    {
        var booted = new Pages.Map();
        NeverRender(booted);
        Hand(booted, "Http", ScenariosFromDisk());
        Hand(booted, "Navigation", new Bench(url));

        MethodInfo boot = typeof(Pages.Map).GetMethod("BootTheWorldAsync", Hidden)
            ?? throw new InvalidOperationException("Map has no BootTheWorldAsync to fingerprint.");
        try
        {
            await (Task)boot.Invoke(booted, [CancellationToken.None])!;
        }
        catch (TargetInvocationException)
        {
            // the browser gate, reached synchronously
        }
        catch (Exception)
        {
            // the browser gate, reached from a continuation
        }

        Assert.True(Field(booted, "_ephemeris") is not null,
            $"{url}: the boot stopped before it built an ephemeris — this is not the browser gate, it is a "
            + "world that was never built, and hashing it would pin a boot that fell over.");

        return Rendered(booted);
    }

    /// <summary>The two services the bench hands the component. They are not the boot's work and one of
    /// them is the URL itself, so they are held out of the fingerprint by name.</summary>
    private static readonly HashSet<string> TheBenchsOwnDoing =
        new(StringComparer.Ordinal) { "<Http>k__BackingField", "<Navigation>k__BackingField" };

    /// <summary>Every instance field the boot CHANGED, in name order, one per line.</summary>
    private static string Rendered(Pages.Map booted)
    {
        var virgin = new Pages.Map();
        var lines = new List<string>();
        foreach (FieldInfo field in typeof(Pages.Map)
                     .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                     .Where(f => !TheBenchsOwnDoing.Contains(f.Name))
                     .OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            string before = Render(Value(field, virgin));
            string after = Render(Value(field, booted));
            if (!string.Equals(before, after, StringComparison.Ordinal))
            {
                lines.Add($"{field.Name} = {after}");
            }
        }
        return string.Join("\n", lines);
    }

    private static object? Value(FieldInfo field, object on)
    {
        try
        {
            return field.GetValue(on);
        }
        catch (Exception ex)
        {
            return $"<unreadable: {ex.GetType().Name}>";
        }
    }

    private static object? Field(Pages.Map map, string name) =>
        typeof(Pages.Map).GetField(name, Hidden)?.GetValue(map);

    // ── The renderer: a value to a stable string, the same way every time ────────────────────────────

    /// <summary>#870 lane 7a · A REAL NUMBER, TO FIVE SIGNIFICANT FIGURES — and this number is MEASURED,
    /// not chosen.
    ///
    /// <para><b>Why not <c>"R"</c>.</b> It WAS <c>"R"</c> (round-trip, every bit), and the pins were captured
    /// on Windows. The ubuntu runner then reddened all 75, and the diff was ONE field — <c>_npcStates</c>,
    /// what the four planners in <c>PlanTheTrafficAsync</c> produce. First it looked like last-bit noise:</para>
    ///
    /// <code>
    ///   windows  X=-26868.865694231867  Y=-5381.603949383356   (the 14th significant digit)
    ///   linux    X=-26868.865694231936  Y=-5381.60394938325
    /// </code>
    ///
    /// <para>…so this rounded to twelve figures, and CI reddened all 75 AGAIN, because the truth is worse
    /// than noise: the planners run an ITERATIVE solve, and a last-bit difference in a seed does not stay
    /// last-bit. The same field, second run:</para>
    ///
    /// <code>
    ///   windows  Velocity.Length=6476.52754287   (the SEVENTH significant digit)
    ///   linux    Velocity.Length=6476.52736563
    /// </code>
    ///
    /// <para><b>So the tolerance was measured rather than guessed.</b> Every number in all 75 fingerprints
    /// was compared, Windows against the runner's own logged output — 113,436 numbers, the two texts
    /// identical token for token once every number is blanked: <b>447 of them differ, and the worst
    /// disagreement is 6.19e-8 relative</b> (<c>/map?scenario=sol-eu</c>, <c>39860689.3333</c> against
    /// <c>39860686.8659</c>) — the two machines agree to about 7.2 significant figures and no further.
    /// Re-quantising both sides of that real pair at each precision:</para>
    ///
    /// <code>
    ///   G12  75 of 75 URLs still differ      G6   0   ← the first that holds
    ///   G8   75                              G5   0   ← chosen: 160x the worst divergence
    ///   G7   74                              G4   0
    /// </code>
    ///
    /// <para><b>Five, and not six, because a hash cannot carry a tolerance.</b> Rounding is not comparison:
    /// two values that agree to six figures can still straddle a rounding boundary and render differently,
    /// and the chance of that scales with (divergence / grid). At G6 the grid is 1e-6 against a 6.19e-8
    /// worst case — sixteen times, which passed here but is one unlucky number away from a flaky merge gate.
    /// G5's grid is 1e-5: <b>a hundred and sixty times the worst divergence this world has ever shown.</b></para>
    ///
    /// <para><b>And it costs nothing.</b> The sweep answers <b>59 distinct worlds of 75 at every precision
    /// from G12 down to G2</b> — the worlds this guard tells apart differ by whole berths and whole ships,
    /// never by a digit. The three REDs in the PR body move a camera 1.5e11 metres, replace a berth with the
    /// front door, and flip a bool. There is no refactor of a method that changes a world by one part in a
    /// hundred thousand and nowhere else.</para>
    ///
    /// <para>Applied to <c>float</c> too, so there is one rule and not two. <c>NaN</c> and the infinities
    /// keep their own words, and <c>-0</c> is folded to <c>0</c> so a sign no arithmetic can be held to
    /// cannot redden a run.</para>
    /// </summary>
    private static string Real(double d) =>
        double.IsNaN(d) || double.IsInfinity(d)
            ? d.ToString(CultureInfo.InvariantCulture)
            : (d == 0 ? 0d : d).ToString("G5", CultureInfo.InvariantCulture);

    internal static string Render(object? value) =>
        Render(value, depth: 0, new HashSet<object>(ReferenceEqualityComparer.Instance));

    private static string Render(object? value, int depth, HashSet<object> walking)
    {
        switch (value)
        {
            case null: return "∅";
            case string s: return $"\"{s}\"";
            case bool b: return b ? "true" : "false";
            case double d: return Real(d);
            case float f: return Real(f);
            case decimal m: return m.ToString(CultureInfo.InvariantCulture);
            case Enum e: return e.ToString();
            case DateTime dt: return dt.ToString("O", CultureInfo.InvariantCulture);
            case TimeSpan ts: return ts.ToString("c", CultureInfo.InvariantCulture);
            case IFormattable n when value.GetType().IsPrimitive:
                return n.ToString(null, CultureInfo.InvariantCulture);
        }

        Type type = value.GetType();
        if (depth >= 6)
        {
            return $"<{type.Name}…>";
        }
        if (!type.IsValueType && !walking.Add(value))
        {
            return $"<{type.Name} again>";
        }

        try
        {
            if (value is IDictionary map)
            {
                var pairs = new List<string>();
                foreach (DictionaryEntry entry in map)
                {
                    pairs.Add($"{Render(entry.Key, depth + 1, walking)}: {Render(entry.Value, depth + 1, walking)}");
                }
                pairs.Sort(StringComparer.Ordinal);
                return $"{{{string.Join(", ", pairs)}}}";
            }

            if (value is IEnumerable list)
            {
                var items = new List<string>();
                foreach (object? item in list)
                {
                    items.Add(Render(item, depth + 1, walking));
                }
                // a set has no order of its own, so give it one; a list's order IS its content.
                if (IsASet(type))
                {
                    items.Sort(StringComparer.Ordinal);
                }
                // a long run of numbers is folded to a digest so the text stays a text.
                return items.Count > 64
                    ? $"[{items.Count} × {Sha256(string.Join(",", items))}]"
                    : $"[{string.Join(", ", items)}]";
            }

            if (IsOurs(type))
            {
                var members = new List<string>();
                foreach (PropertyInfo property in type
                             .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                             .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                             .OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    members.Add($"{property.Name}={Read(() => property.GetValue(value), depth, walking)}");
                }
                foreach (FieldInfo field in type
                             .GetFields(BindingFlags.Instance | BindingFlags.Public)
                             .OrderBy(f => f.Name, StringComparer.Ordinal))
                {
                    members.Add($"{field.Name}={Read(() => field.GetValue(value), depth, walking)}");
                }
                return $"{type.Name}({string.Join(", ", members)})";
            }

            return $"<{type.FullName}>";
        }
        finally
        {
            if (!type.IsValueType)
            {
                walking.Remove(value);
            }
        }
    }

    private static string Read(Func<object?> get, int depth, HashSet<object> walking)
    {
        try
        {
            return Render(get(), depth + 1, walking);
        }
        catch (Exception ex)
        {
            return $"<threw {ex.GetType().Name}>";
        }
    }

    private static bool IsASet(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType
            && i.GetGenericTypeDefinition() == typeof(ISet<>));

    /// <summary>Ours gets walked; everything else is named and left alone — a <c>CancellationTokenSource</c>
    /// or an <c>HttpClient</c> has no content a fingerprint wants and plenty it could not repeat.
    ///
    /// <para>The bench's OWN types are excluded by name, not by accident: this project's namespace also
    /// begins with <c>SpaceSails</c>, and walking the injected <c>NavigationManager</c> would put the URL
    /// under test INSIDE the fingerprint — which would make every URL's hash trivially unique and the
    /// whole sweep a test that cannot fail.</para>
    ///
    /// <para>Tuples are walked too. Two of the boot's fields (<c>_pendingExpeditionCheat</c>,
    /// <c>_pendingDeflectionCheat</c>) are tuples of twelve and four, and a rule that named them by type
    /// would answer the same thing for every rock the deflection cheat ever spawns.</para></summary>
    private static bool IsOurs(Type type) =>
        (type.Namespace?.StartsWith("SpaceSails", StringComparison.Ordinal) == true
            && type.Namespace?.StartsWith("SpaceSails.Client.Tests", StringComparison.Ordinal) != true)
        || type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true
        || type.FullName?.StartsWith("System.Tuple`", StringComparison.Ordinal) == true;

    // ── The services the page injects ────────────────────────────────────────────────────────────────

    /// <summary>#1135 · Widened from <c>Pages.Map</c> to any <c>ComponentBase</c>. It was never about the
    /// page: what it turns off is a base-class early-out, and the tracking post now needs it for the same
    /// reason the page always did — a bench that reaches a desk method by name is driving a component no
    /// renderer has ever attached, and the desk's own <c>StateHasChanged</c> (#1135's fix for the handlers
    /// pressed through a lambda) throws there exactly as the boot's five calls did.</summary>
    internal static void NeverRender(ComponentBase component)
    {
        // A ComponentBase that was never attached to a renderer throws out of StateHasChanged, and the boot
        // calls it five times. Telling the component it already has a render queued is the framework's own
        // early-out; nothing else about it is faked.
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has moved.");
        pending.SetValue(component, true);
    }

    internal static void Hand(Pages.Map map, string property, object service) =>
        typeof(Pages.Map).GetProperty(property, Hidden)!.SetValue(map, service);

    internal static HttpClient ScenariosFromDisk() =>
        new(new FromDisk()) { BaseAddress = new Uri("http://localhost/") };

    internal sealed class Bench : NavigationManager
    {
        public Bench(string url) => Initialize("http://localhost/", "http://localhost" + url);
    }

    /// <summary>The real scenario files, off the real repo — the page fetches <c>scenarios/&lt;name&gt;.json</c>
    /// and a scenario nobody shipped must 404 here exactly as it would in the browser.</summary>
    private sealed class FromDisk : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string relative = request.RequestUri!.AbsolutePath.TrimStart('/');
            string path = Path.Combine(RepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));
            return Task.FromResult(File.Exists(path)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(File.ReadAllText(path)) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    internal static string RepoRoot()
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

    internal static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..32].ToLowerInvariant();

    private static string Indent(string text) =>
        string.Join("\n", text.Split('\n').Select(l => "    " + l));
}
