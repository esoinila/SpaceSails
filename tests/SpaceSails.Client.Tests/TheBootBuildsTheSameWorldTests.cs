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
/// unchanged, <c>/map?scenario=sol-eu</c></b> — the one scenario in the list that has none of the three
/// berths in it. A re-pin that had moved a third field, split a hash group, or moved <c>sol-eu</c> would
/// have been a different lane's bug wearing this lane's clothes.</para>
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
            ["/map"] = "7967bfa61ca6f178dc3ecce9012775ca",
            ["/map?archive=1&land=1&nerve=2"] = "b97f121edbc2301cebe1c6bfea807a6e",
            ["/map?ashore=1&kaamos=bounce"] = "933925096fa5f0ea4a52c9f3a29c4b3f",
            ["/map?ashore=1&start=space-bar"] = "933925096fa5f0ea4a52c9f3a29c4b3f",
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
            ["/map?badge=1"] = "7166ac77b604669ea00409e10af08eb2",
            ["/map?barcase=1"] = "d3bcabff80aab642f51f96d5945cfc13",
            ["/map?bond=1"] = "6ba6b73351bea2c64586b82980de31ef",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "99fe94968b2fef23f35b126f476829ac",
            ["/map?converge=1"] = "7967bfa61ca6f178dc3ecce9012775ca",
            ["/map?counter=1"] = "d6de0e30293944446559fb80ba7456a6",
            ["/map?counter=1&watch=2"] = "2afb1bd52594988f99052ac28c99e580",
            ["/map?counter=1&watch=5"] = "ac2f10c2f051f5808405d7989ac808a2",
            ["/map?credits=1234&fuel=7&simhours=9"] = "7967bfa61ca6f178dc3ecce9012775ca",
            ["/map?credits=50000"] = "7967bfa61ca6f178dc3ecce9012775ca",
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
            ["/map?crew=meeting"] = "7967bfa61ca6f178dc3ecce9012775ca",
            ["/map?crew=petition"] = "7967bfa61ca6f178dc3ecce9012775ca",
            ["/map?death=collector&dock=selene-gate"] = "933925096fa5f0ea4a52c9f3a29c4b3f",
            ["/map?death=impact"] = "933925096fa5f0ea4a52c9f3a29c4b3f",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "5031eac596ef2ade8a778943fb3b38de",
            ["/map?deflection=1"] = "1dc487d49f24eb1c0210a1d362f1067d",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "f6860c1736e9dc0280f6845988547f38",
            ["/map?designate=1"] = "3aa892625ebe8249554959f263884a7a",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "35d2aac33d07181922c1c026a0d90b83",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "c96e8a544631352fadd123436ff433d2",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "81479dde811986c2cfd405249e188c73",
            // #997 wave 10 · see /map?start=wreck&target=collector further down — the new dev start moved
            // free-flying after a browser walk, and the reason is written there.
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "ee6e7cb0a9b1b6c8af7c65a3da48bdec",
            ["/map?dock=the-space-bar"] = "933925096fa5f0ea4a52c9f3a29c4b3f",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "fe5da53ff728cf6152cc0d61f1aa1560",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "57e110656151d82f83bfedd039eac082",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "10523ea60f957b6db3b2c10cbd814a71",
            ["/map?dock=the-tilt&site=0"] = "442929dff0f101ddeacee3d2a74d23a0",
            ["/map?dock=the-tilt&site=0&land=1"] = "d47bf95469b07c549378381ef6244cf0",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "e1e452274623ec5cf58b289942dc0110",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "5c72966e2bbfe158f2e00c91cd74ae68",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "67e31ddf85f3d313d91f54d9934e001e",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "a4e0f87ec1f85baffc234b7e297082cb",
            ["/map?dock=the-tilt&site=1"] = "7d2529081dd3199f8d611003995b2a24",
            ["/map?dock=the-tilt&start=space-bar"] = "933925096fa5f0ea4a52c9f3a29c4b3f",
            ["/map?expedition=mining"] = "7baebd3433498fa339562e9c69ad0ad2",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "7967bfa61ca6f178dc3ecce9012775ca",
            ["/map?found=1&land=1"] = "37ecb5f2d168db6890e6535d89474f52",
            ["/map?found=1&land=1&floor=17&card=all"] = "414ad582a54213296cd28ee7836fce74",
            ["/map?freight=1"] = "1d38900f215906050318305a66e9186a",
            ["/map?frontdoor=1"] = "46052a10b238b0217a503d88c47e70f4",
            ["/map?goodscar=1"] = "d1a0a4e02115956029686817544b9c29",
            ["/map?kaamos=all"] = "7967bfa61ca6f178dc3ecce9012775ca",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "24a8de94c4411a7a3dc40d185181854c",
            ["/map?kaamos=hq&land=1"] = "670d489072238ee0c73a347c95eedf75",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "b441722a930d6e0989b5919c8b5f8426",
            ["/map?nebula=all"] = "7967bfa61ca6f178dc3ecce9012775ca",
            ["/map?oldcrew=1"] = "933925096fa5f0ea4a52c9f3a29c4b3f",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "7967bfa61ca6f178dc3ecce9012775ca",
            ["/map?park=1"] = "977477e634d4b871b28b57cd8ea9aaa4",
            ["/map?park=1&spread=1"] = "c17ef3a94c23267b233c8844d72507eb",
            ["/map?parkback=1"] = "cbb1fe2ee441b3d96f6e9ec93ecf4af0",
            ["/map?parkwalk=1"] = "bb8212eb381443a6dbcdf9787ada3fa1",
            // #870 lane 6′b · RE-PINNED — see the note above `?badge=1`; same one reason.
            ["/map?patrol=2"] = "3df1514fe65101d127023521a131c291",   // #618 · see ?badge=1 above
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "c16c9881d7ff41b76057f24789fb31e0",
            ["/map?ringoffice=1"] = "7261e8743392b80e2c21ea8f5aa9a2b2",
            ["/map?rip=1"] = "1eb2b63068313a49f127cc78903bde6e",
            ["/map?scenario=..%2Foops"] = "7967bfa61ca6f178dc3ecce9012775ca",
            ["/map?scenario=sol-eu"] = "8fced2e8dbb09826da4e242e6011bf30",
            ["/map?secretlab=1"] = "9ac0aa0951145d84f1bbb02c77997441",
            ["/map?secretlab=deep&land=1&card=next"] = "a0cce964c8f32e29b18c93c90391551a",
            ["/map?secretlab=deep&land=1&floor=1"] = "7fc171c9ba10bb61694923c89d763128",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "ec070ec069bcca8e9dfb3b719a767043",
            // #841 · ?perf=1 arms a stopwatch on the DeckView and touches nothing the boot builds, so this
            // row is — and must stay — byte-for-byte the world `?secretlab=deep&land=1&floor=1` builds two
            // lines above. That it is IDENTICAL is the assertion: a measurement cheat that moved the world
            // would be measuring a world nobody plays.
            ["/map?secretlab=deep&land=1&floor=1&perf=1"] = "7fc171c9ba10bb61694923c89d763128",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "336af66d65828edd737051e9bd8036ef",
            ["/map?secretlab=deep&land=1&floor=21"] = "2376b9fc97c59fdc0239348ba65bd06a",
            ["/map?skim=saturn"] = "933925096fa5f0ea4a52c9f3a29c4b3f",
            ["/map?sling=jupiter"] = "933925096fa5f0ea4a52c9f3a29c4b3f",
            ["/map?spread=1"] = "0f0dbcf72c8a61ccab5114904c74daca",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "c170b5cf1d6ac38187b64cdcc65b973a",
            ["/map?start=wreck&fetch=active"] = "933925096fa5f0ea4a52c9f3a29c4b3f",
            ["/map?start=wreck&dest=saturn"] = "933925096fa5f0ea4a52c9f3a29c4b3f",
            // #997 wave 10 · The new dev start: ?target=collector, the dossier's own door. Read off the
            // dump and diffed against the pinned list — this is the ONLY line the dump adds, and no other
            // moved: the 32nd BootQuery field is something the PARSE answers, not a world the boot builds.
            //
            // It hashes the same as its neighbours, which is correct and worth saying out loud rather than
            // glossing: ?start= and ?target= are BOTH spent after the browser gate (ApplyTheStartPoint and
            // SeedTheArcsAndTheJobs), so this sweep sees neither the roadster nor the muscle. What these
            // URLs answer differently is pinned next door, in TheBootReadsTheSameQueryTests.
            ["/map?start=wreck&target=collector"] = "933925096fa5f0ea4a52c9f3a29c4b3f",
            ["/map?stool=1&neighbour=0"] = "8f9a749720df7a02d155c2be49e08a2b",
            ["/map?stool=1&neighbour=1"] = "d064aae774657dd16b51ed776513ac52",
            ["/map?tablescene=free&approach=1"] = "0fd25054552850b288a183a6fe1e641c",
            // #973 L2 · the Nebula rep on the rota, forced. Taken with SPACESAILS_BOOT_FINGERPRINT_DUMP and
            // diffed against the pinned list: this is the ONLY line the dump adds, and no other moved.
            ["/map?tablescene=free&rep=1&approach=0"] = "848028266c28466ab5c7230db0628e9c",
            ["/map?tablescene=free&watch=5&approach=0"] = "1842b11c6841d05de06196c9ed6d175f",
            ["/map?threads=1"] = "18ec4e4049599d4ba5f4def78cc6ab4c",
            ["/map?threads=1&watch=5"] = "fb531abf32a62bfa4933c28f1e63e1da",
            ["/map?wreck=drivefailure&land=1"] = "1a740618abc111b961a09e362e7ac0c7",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "c2c0ac96c377411e1922ede38253aaf0",
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
        "/map?scenario=sol-eu",
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

    internal const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

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

    internal static void NeverRender(Pages.Map map)
    {
        // A ComponentBase that was never attached to a renderer throws out of StateHasChanged, and the boot
        // calls it five times. Telling the component it already has a render queued is the framework's own
        // early-out; nothing else about it is faked.
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has moved.");
        pending.SetValue(map, true);
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
