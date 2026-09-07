using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1074 / #1108 · THE PROCESS-WIDE REGISTERS, AND THE SUITES THAT WRITE INTO THEM.
///
/// <para><see cref="StopOrder.Install"/>, <see cref="PreservationZone.Install"/>,
/// <see cref="Burial.Install"/>, <see cref="PoliteDecline.Install"/> and <see cref="QuietHands.Install"/>
/// each replace a process-wide register, and xUnit parallelises across test CLASSES while serialising within
/// one. Two suites that each install their own grounds and restore an EMPTY register afterwards can therefore
/// blank each other's world mid-guard: the second one's <c>Install([])</c> lands while the first is still
/// asking whether its ground is closed, and the first goes red on a world the second owns.</para>
///
/// <para>The registers only ever change the answer for the ids IN them, which is why each suite already walks
/// an id family of its own; what that discipline cannot cover is the RESTORE, which is global by nature. So
/// every suite that writes to these registers shares one collection and they run one at a time.</para>
///
/// <para>#1074's paper-heads lane widened it: the burial register is the same kind of ambient and
/// <c>TheBurialTests</c> and <c>TheMoneyTrailTests</c> were writing to these registers from outside the
/// collection, which left exactly the hole the collection was created to close.</para>
///
/// <para><b>#1108 · AND THEN THE OTHER HALF OF THE HOLE, WHICH IS THE ONE THAT WAS COSTING RUNS.</b> Sharing
/// a collection serialises the WRITERS against each other and does nothing at all about the READERS, and the
/// readers are the whole suite: <c>MoonSurface</c>'s surface deck asks
/// <see cref="PreservationZone.On"/>, <c>UndergroundComplex</c> asks <see cref="Burial.IsFilled"/>,
/// <see cref="StopOrder.On"/> and <see cref="PoliteDecline.On"/> in a dozen places, and
/// <c>CanteenRegulars</c> asks two of them. A guard that installs a register on a REAL body id therefore
/// moves the ground under every other class building that body's deck at that moment — which is exactly what
/// #1108 was: about one run in four, <c>EveryFrameHashesTheSameTests</c> drew 651 marks where 649 were
/// pinned, and <c>TheLiftHeadIsJustAnotherHutTests</c> measured a lift head with a preservation fence
/// accidentally welded to it, because <c>ThePreservedSiteIsStillWalkableTests</c> and
/// <c>TheMugOnTheShelfTests</c> were fencing and closing <c>luna</c> from another thread.</para>
///
/// <para>So the collection is <b>non-parallel</b> (<c>DisableParallelization</c>): while a suite in it runs,
/// nothing else in the assembly runs, which is the only thing that can protect a reader that has no idea it
/// is reading an ambient. Measured with an isolated xUnit 2.9.3 probe: four watcher classes polling a flag
/// held by a plain <c>[CollectionDefinition]</c> class all saw the overlap (4 failed / 1 passed); with
/// <c>DisableParallelization = true</c> on the same definition, none of them did (5 passed).</para>
///
/// <para><b>Why the state stays global.</b> A burial changes the SHAPE of a site and the shape of a site is
/// asked by about thirty callers — the lift panel, the remote, the sounder, the room carver, the sign writer,
/// the audits, the renderer — none of which has any business learning what a burial is (§13.15's second cause
/// is a caller reasoning about the shape of a building it does not own). That is <see cref="Burial"/>'s own
/// argument for being ambient and it is still correct; the game is single-threaded and reads it safely. It is
/// the TEST RUNNER that is parallel, so the test runner is where the cost is paid.</para>
///
/// <para><b>What it costs.</b> These classes stop overlapping the rest of the suite, so their own seconds
/// land on the wall clock instead of hiding under the slow gates. Core's nine are fast classes; the Client's
/// four include one 30 s slow gate (<c>TheDeclinedDoorIsStillAWayHomeTests</c>). That is the price of a suite
/// that is correct rather than correct most afternoons.</para>
///
/// <para><b>It is also a law, not a convention.</b> <c>TheProcessWideWritersAreSerialisedTests</c> reads both
/// suites' sources and fails on any test class that writes one of these registers (or
/// <see cref="Aerobrake.DiceEpisodeHook"/>) without carrying <c>[Collection(StopRegisterCollection.Name)]</c>
/// — which is how four classes that had drifted outside it were found.</para>
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class StopRegisterCollection
{
    /// <summary>The collection's name, as a constant so the suites cannot spell it several ways.</summary>
    public const string Name = "the stop register";
}
