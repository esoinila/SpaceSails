namespace SpaceSails.Core;

/// <summary>
/// #268 pay-at-the-pump — the deferred bill for an autopilot terminal match (⚓ Match &amp; clamp).
///
/// <para>The owner's ruling (live playtest 2026-07-17): "the autopilot cost should only come after the
/// flight or during it." The armed autopilot already pays as it burns — every approach/insert/trim/
/// transfer pulse comes off the tank in the live tick loop, as the burn fires. The one assist that still
/// took the whole fare at the button press was ⚓ Match &amp; clamp: it fired the redirect impulse and
/// decremented the tank in the same breath, before the ship had flown the coast to the berth. When the
/// flown approach then diverged (the #267 through-the-planet route, an abort, the captain steering off),
/// that money was kept for a leg that never delivered — "fuel stolen".</para>
///
/// <para>This ledger defers the take. The redirect impulse still fires at the press (#213's instant match
/// is preserved — the physics is unchanged), but the pulse cost is not taken then: it ACCRUES here and
/// SETTLES only when the leg delivers, i.e. the clamp completes at the same berth. An aborted or diverging
/// approach — the ship leaves the clamp envelope before ever clamping — drops the tab UNCHARGED: "an
/// aborted or diverging flight keeps money it never earned", and here it keeps none. No refund machinery,
/// because nothing unearned was taken in the first place. The affordability gate is unchanged: the caller
/// still checks the whole quote against the (effective) tank BEFORE firing — checking whether you can
/// afford the fare and actually taking it are different acts.</para>
///
/// <para>Pure and immutable so the accrual/settle/abort arithmetic is unit-testable and can never drift
/// from the client wiring.</para>
/// </summary>
/// <param name="HavenId">The berth this tab is being run up against, or null when nothing is owed.</param>
/// <param name="Pulses">Pulses fired into the match so far and awaiting settlement on the clamp.</param>
public readonly record struct MatchClampLedger(string? HavenId, int Pulses)
{
    /// <summary>Nothing on the tab.</summary>
    public static readonly MatchClampLedger Empty = new(null, 0);

    /// <summary>True when a match burn is on the tab awaiting a clamp to settle it.</summary>
    public bool Owes => HavenId is not null && Pulses > 0;

    /// <summary>Accrue a fired match burn against a berth. Re-matching the SAME berth stacks (a hot
    /// approach can need more than one redirect, each a real burn); aiming a match at a DIFFERENT berth
    /// abandons the prior tab uncharged — that approach is over, and it never clamped.</summary>
    public MatchClampLedger Accrue(string havenId, int burnPulses)
    {
        int add = burnPulses > 0 ? burnPulses : 0;
        return HavenId == havenId ? this with { Pulses = Pulses + add } : new MatchClampLedger(havenId, add);
    }

    /// <summary>Settle on a completed clamp: returns the pulses owed for THIS berth (0 for any other —
    /// clamping elsewhere still clears the abandoned tab) and the emptied ledger. The caller decrements
    /// the tank by <c>Charge</c> as the leg lands.</summary>
    public (int Charge, MatchClampLedger Next) Settle(string clampedHavenId) =>
        HavenId == clampedHavenId ? (Pulses, Empty) : (0, Empty);

    /// <summary>Drop the tab uncharged — the approach ended without delivery (the ship left the clamp
    /// envelope, armed the autopilot elsewhere, or departed on the long haul). Money the flight never
    /// earned is never taken.</summary>
    public MatchClampLedger Abort() => Empty;
}
