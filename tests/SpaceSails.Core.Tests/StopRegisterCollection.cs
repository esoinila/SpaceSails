using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1074 · THE PROCESS-WIDE REGISTERS, AND THE SUITES THAT INSTALL INTO THEM.
///
/// <para><see cref="StopOrder.Install"/>, <see cref="PreservationZone.Install"/> and
/// <see cref="Burial.Install"/> each replace a process-wide register, and xUnit parallelises across test
/// CLASSES while serialising within one. Two suites that each
/// install their own grounds and restore an EMPTY register afterwards can therefore blank each other's world
/// mid-guard: the second one's <c>Install([])</c> lands while the first is still asking whether its ground is
/// closed, and the first goes red on a world the second owns.</para>
///
/// <para>The registers only ever change the answer for the ids IN them, which is why each suite already walks
/// an id family of its own; what that discipline cannot cover is the RESTORE, which is global by nature. So
/// every suite that writes to these registers shares one collection and they run one at a time. It costs
/// nothing — they are fast classes — and it is the difference between a suite that is correct and a suite
/// that is correct most afternoons.</para>
///
/// <para>#1074's paper-heads lane widened it: the burial register is the same kind of ambient and
/// <c>TheBurialTests</c> and <c>TheMoneyTrailTests</c> were writing to these registers from outside the
/// collection, which left exactly the hole the collection was created to close.</para>
/// </summary>
[CollectionDefinition(Name)]
public sealed class StopRegisterCollection
{
    /// <summary>The collection's name, as a constant so the three suites cannot spell it three ways.</summary>
    public const string Name = "the stop register";
}
