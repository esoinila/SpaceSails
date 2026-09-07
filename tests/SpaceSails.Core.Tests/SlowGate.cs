using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #251 (item 4) · THE SLOW GATE — the mark that says "this class is one of the ones that costs
/// minutes", so a crew's inner loop can leave it out and the merge gate cannot.
///
/// <para><b>Why this exists.</b> Measured on 2026-09-02 at e7c1915, the whole suite is 5,759 tests
/// and 3,552 seconds of test time. 2,886 of those tests — half of them — finish in under a
/// millisecond and account for 0.0% of the clock. 292 tests (5.1%) hold 94.2% of it. The suite is
/// not slow; a small, nameable set of gates inside it is slow, and everybody else pays for them on
/// every red-proof cycle.</para>
///
/// <para><b>What it changes: nothing.</b> A trait is metadata. A tagged test runs exactly when it
/// ran before, in the same order, asserting the same thing — CI runs the whole suite and always
/// will. The only thing this buys is the ability to say <c>--filter "speed!=slow"</c> on a dev box
/// and get the rule checks back in seconds instead of six minutes. Nothing is deleted, nothing is
/// weakened, and the roster in <see cref="TheSlowGateRosterTests"/> makes every tag visible in
/// review.</para>
///
/// <para><b>The cut is ten seconds of CLASS total</b> — not per test. xUnit parallelises across
/// test classes and serialises within one, which is why each assembly's wall clock in the baseline
/// run was, to within two seconds, the cost of its single slowest class: Core ran 5 m 51 s and
/// <c>ZubrinTrafficTests</c> alone is 349 s; Client ran 5 m 1 s and <c>EveryDeskBootsTests</c>
/// alone is 300 s. A class is the unit the scheduler can actually remove, so a class is the unit
/// that carries the mark. See <see cref="TheSlowGateRosterTests"/> for the roster and the numbers.
/// </para>
///
/// <para>Full instructions — the fast invocation, the full invocation, and what the fast run does
/// not tell you — are in <c>docs/testing-guide.md</c>, Appendix C.</para>
/// </summary>
[TraitDiscoverer("SpaceSails.Core.Tests.SlowGateDiscoverer", "SpaceSails.Core.Tests")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class SlowGateAttribute : Attribute, ITraitAttribute
{
}

/// <summary>
/// #251 (item 4) · Turns <see cref="SlowGateAttribute"/> into the trait <c>speed=slow</c>, which is
/// what <c>dotnet test --filter</c> can actually see. xUnit v2 requires a discoverer for any trait
/// attribute that is not <c>[Trait]</c> itself; this is the whole of it.
///
/// <para>It lives in each test assembly rather than in one shared place because the two test
/// assemblies cannot see each other — <c>SpaceSails.Core.Tests</c> references Core,
/// <c>SpaceSails.Client.Tests</c> references the Client, and neither references the other. The
/// trait NAME and VALUE are the contract, and they are identical in both, so one filter covers
/// both.</para>
/// </summary>
public sealed class SlowGateDiscoverer : ITraitDiscoverer
{
    /// <summary>The trait key every fast run filters on.</summary>
    public const string Key = "speed";

    /// <summary>The trait value a slow gate carries.</summary>
    public const string Value = "slow";

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        yield return new KeyValuePair<string, string>(Key, Value);
    }
}
