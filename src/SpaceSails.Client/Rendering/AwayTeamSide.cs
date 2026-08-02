using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #621 · IS THE CAPTAIN BACK AT THEIR OWN SHUTTLE — asked ONCE, for whichever thing they are standing on.
///
/// <para>Two features need this fact and each one used to work it out for itself. <c>CaptainBeyondReach</c>
/// (nothing lays a hand on you past the door) had already been fixed to ask the wreck's own question; the
/// air supply had not, and still asked the MOON's:</para>
///
/// <code>SuitAir.SourceOf(ex.Floor, StandingInTheShelter(ex), MoonSurface.IsSafeAboard(_avatarY), …)</code>
///
/// <para><see cref="MoonSurface.IsSafeAboard"/> asks whether the captain is above the regolith's top rim at
/// y = −20. <b>A derelict's entire deck runs from −9 to +9</b> (<see cref="WreckLayout.TopY"/> …
/// <see cref="WreckLayout.BottomY"/>), so every square metre of every wreck answered YES — and the suit
/// concluded the captain was up her tube, breathing hers. Aboard a hull that has held vacuum for years the
/// gauge read <i>"AIR 6m00 · FILLING — you are on her air, not the tank"</i>, the one-shot line announced
/// <i>"🫁 HER AIR. The tank stops and starts filling — the only place in the world it does"</i>, and the
/// tank genuinely refilled. The sim did one thing and the sentence said another, over a world where the
/// game's own death prose reads <i>"in vacuum she has held for years. Her air went out a long time before
/// yours did."</i></para>
///
/// <para>It also quietly killed a whole death: <see cref="DeathCause.Suffocated"/> can never fire on a
/// derelict if the tank never drains there, so <c>DeathPlace.Derelict</c>'s suffocation card — prose, tail
/// and picture — was unreachable code that nobody could have found by playing.</para>
///
/// <para><b>The fifth occurrence of the named pattern: a MOON constant governing a SHIP.</b> It hides so
/// well because the moon's number is not absurd for a wreck — it is merely satisfied everywhere, so the
/// feature silently never fires and nothing ever errors. The cure for a bug that keeps coming back is not a
/// fifth careful call site; it is one function, so there is nothing left to get wrong.</para>
///
/// <para>#637 · And two more were found asking it afterwards, which is the argument made again: the NERVE
/// (a derelict was never exposed ground, so a haunted hull cost nothing) and the COMMS (the link never
/// degraded aboard, at any depth). Both now come here. The guard that closes the pattern rather than the
/// call sites is <c>ADerelictIsNotAMoonTests</c>, which reads the live client source and fails on any file
/// outside this one that mentions <see cref="MoonSurface.IsSafeAboard"/> in code at all — because a rule
/// enforced on a function a caller is free not to use is not enforced.</para>
/// </summary>
public static class AwayTeamSide
{
    /// <summary>
    /// True when the captain is on the away team's own side of the door — up the ship's tube on a moon, or
    /// past the shuttle's lock aboard a derelict. Behind that door is her air, her guns and the crew-only
    /// hatch nothing uninvited opens; in front of it the clock is real.
    /// </summary>
    /// <param name="onWreck">Is the excursion inside somebody else's hull rather than on a surface?</param>
    /// <param name="avatarX">Deck-unit X — the axis the wreck's lock stands on.</param>
    /// <param name="avatarY">Deck-unit Y — the axis the moon's tube mouth stands on.</param>
    /// <param name="avatarRadius">The captain's own half-width, so "past" means the whole of them.</param>
    public static bool BackAtTheShuttle(bool onWreck, double avatarX, double avatarY, double avatarRadius) =>
        onWreck
            ? WreckLayout.PastTheLock(avatarX, avatarRadius)
            : MoonSurface.IsSafeAboard(avatarY);

    /// <summary>
    /// HOW FAR PAST THAT DOOR THEY HAVE WALKED — 0 at the threshold, 1 as deep as this place goes.
    ///
    /// <para>The same fact as <see cref="BackAtTheShuttle"/> asked with a number instead of a yes: on a moon
    /// the depth axis is Y, from the regolith's top rim down to the monolith; aboard a derelict it is X,
    /// from the away team's own lock aft to the transom. Two different axes, one question, and — the point
    /// of putting it here — one place that knows which axis this world uses.</para>
    ///
    /// <para>#637 · Written because <c>CommsOnsetBias</c> measured the moon's axis aboard a hull. A wreck's
    /// whole deck sits above y = −20, so the early return fired at every point of every derelict and the
    /// deep-in-a-dead-hull comms drop — arguably the best place in the game for one — could never happen.
    /// The number was not wrong; it was answered before the question was asked.</para>
    /// </summary>
    public static double HowFarInside(bool onWreck, double avatarX, double avatarY)
    {
        (double threshold, double deepest, double at) = onWreck
            ? (WreckLayout.ShuttleLockX, WreckLayout.AftX, avatarX)
            : (MoonSurface.SurfaceTopY, MoonSurface.MonolithY, avatarY);

        double span = threshold - deepest;
        return span > 0 ? System.Math.Clamp((threshold - at) / span, 0.0, 1.0) : 0.0;
    }

    /// <summary>
    /// THE ONSET MULTIPLIER ON THE DOWNLINK, for whichever thing they are standing on. 0.5× back at their
    /// own door (a drop there would be silly), 1× at the threshold, up to 2× as deep as the place goes.
    ///
    /// <para>It lives beside the predicate it is built from rather than in the page, because the page is
    /// where it learned to ask the moon.</para>
    /// </summary>
    public static double CommsOnsetBias(bool onWreck, double avatarX, double avatarY, double avatarRadius) =>
        BackAtTheShuttle(onWreck, avatarX, avatarY, avatarRadius)
            ? 0.5
            : 1.0 + HowFarInside(onWreck, avatarX, avatarY);
}
