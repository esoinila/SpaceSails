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
}
