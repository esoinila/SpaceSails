using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 (item 4) · THE SLOW GATE — the mark that says "this class is one of the ones that costs
/// minutes", so a crew's inner loop can leave it out and the merge gate cannot.
///
/// <para>This is the Client-side twin of <c>SpaceSails.Core.Tests.SlowGateAttribute</c>. The two
/// test assemblies cannot see each other — Core.Tests references Core, Client.Tests references the
/// Client, and neither references the other — so the attribute is declared in both. What travels
/// between them is not the type but the TRAIT: both discoverers emit <c>speed=slow</c>, so a single
/// <c>--filter "speed!=slow"</c> covers the whole solution.</para>
///
/// <para><b>What it changes: nothing.</b> A trait is metadata. A tagged test runs exactly when it
/// ran before, asserting the same thing — CI runs the whole suite and always will. See
/// <see cref="TheSlowGateRosterTests"/> for this assembly's roster and the measured numbers behind
/// it, and <c>docs/testing-guide.md</c> Appendix C for the invocations.</para>
/// </summary>
[TraitDiscoverer("SpaceSails.Client.Tests.SlowGateDiscoverer", "SpaceSails.Client.Tests")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class SlowGateAttribute : Attribute, ITraitAttribute
{
}

/// <summary>
/// #251 (item 4) · Turns <see cref="SlowGateAttribute"/> into the trait <c>speed=slow</c>, which is
/// what <c>dotnet test --filter</c> can actually see. xUnit v2 requires a discoverer for any trait
/// attribute that is not <c>[Trait]</c> itself; this is the whole of it.
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
