namespace SpaceSails.Core;

/// <summary>
/// #563 · <b>AN OLD ONE OPENS A DOOR THE SLOW WAY.</b>
///
/// <para><b>Owner ruling, 2026-09-06</b> (#563's second question — <i>do the Old Ones use doors?</i>), in
/// his own terms: they still have doors <i>a bit like we still have nostalgic gramophones; for them it is
/// legacy and nostalgia, and it lets them not reveal their true capabilities, which might scare people.</i>
/// The canon that follows from it is filed in <c>docs/worldbuilding-notes.md</c> §10 and is binding here:</para>
///
/// <list type="number">
/// <item><b>A door is a door to them.</b> An unlocked leaf is opened over a beat — the leaf moves before
/// anything comes through it — and a locked one is never broken: they wait on the far side, or go round the
/// way a walker would. What they could do to a door instead is never shown.</item>
/// <item><b>They do not see through a closed one</b> (#442, shipped in #1154). A shut leaf is therefore a
/// real delay as well as a real refuge from sight, and a locked leaf is a wall to them <i>by choice</i>.</item>
/// <item>Nothing states any of it. The leaf sliding on its own, with nothing visible behind it yet, is the
/// whole telling — so this file publishes <b>no prose at all</b>: no line, no card, no plate, not one string.</item>
/// </list>
///
/// <para><b>Why the arithmetic lives in Core.</b> Every number below is derived from something the game
/// already holds — a leaf's own length and the pace of the thing hauling it — and nothing here is a feel
/// number somebody typed. That is only checkable if it is one pure function with a test on it, rather than
/// three expressions in a frame loop that will be tuned by whoever reads one of them.</para>
/// </summary>
public static class ReeverDoor
{
    /// <summary>How near a leaf a body has to be before it can lay hands on it, <b>as a multiple of its own
    /// radius</b>.
    ///
    /// <para>TWO radii — one body-width — and it is not a new number: it is #724's own reach
    /// (<c>SurfaceCollision</c>'s <c>GapReachInRadii</c>, the width the owner named when he said <i>"if your
    /// y is a body-width off the doorway's cut"</i>), and it is the same touching law
    /// <see cref="ReeverChase.CatchRadius"/> already runs on — two <c>AvatarRadius</c> bodies in contact. An
    /// Old One can lay hands on a leaf exactly as near as it can lay hands on the captain, which is the only
    /// arm's length this game has ever had.</para>
    ///
    /// <para>It has to be a reach and not "touching the segment exactly", because the legs stop a body a
    /// radius short of a leaf and floating point decides the last digit of where. A law that fired at
    /// exactly one radius would be a law that fires or does not fire on rounding.</para></summary>
    public const double ReachInRadii = 2.0;

    /// <summary>That reach for a body of the given radius. One expression, so no caller can hold a
    /// half-remembered version of it.</summary>
    public static double Reach(double radius) => radius * ReachInRadii;

    /// <summary>
    /// <b>MAY THIS LEAF BE WORKED AT ALL?</b> Three refusals, and each of them is the ruling rather than a
    /// convenience:
    ///
    /// <list type="bullet">
    /// <item><paramref name="locked"/> — <b>never</b>. This is canon point 1's second half and the whole
    /// restraint the gramophone stands for: a locked leaf is a wall to them <i>by choice</i>. They wait on
    /// the far side (the legs' own stall) or go round (the legs' own handrail); what they could do to it
    /// instead is never shown.</item>
    /// <item><paramref name="walledUp"/> — a doorway with a wall laid across its middle (#442's
    /// <c>DoorwayIsWalledUp</c>): the ship's own hatch while the boat is docked, a dogged compartment, a
    /// haven's sealed berth. Opening the leaf would retract a picture in front of stone that still stops
    /// them — the exact lie #442 exists to kill, in the other direction.</item>
    /// <item><paramref name="interlocked"/> — a leaf in an airlock group (#462). Which leaf of a tube may
    /// stand open is a law about the group, not about the leaf, and a leaf propped by an Old One could
    /// never take its turn. The tubes are behind the crew-only lock they are held at anyway
    /// (<see cref="ReeverChase"/>), so this refuses nothing that was ever going to happen — it only refuses
    /// it <i>on purpose</i>.</item>
    /// </list>
    ///
    /// <para>Note what is NOT asked: the leaf's ink, its idiom, its floor, or who is thought to have hung
    /// it. An imported violet leaf, a machined one, and a leaf drawn in the found halls' own no-texture
    /// idiom (#716/#1082) are all a door to them and are all worked identically — which is canon point 3,
    /// legacy, and the reason a leaf down there opens at all.</para>
    /// </summary>
    public static bool MayWork(bool locked, bool interlocked, bool walledUp) =>
        !locked && !interlocked && !walledUp;

    /// <summary>
    /// <b>THE BEAT — how long the leaf takes, and where the number comes from.</b>
    ///
    /// <para>The captain never sees this door move: it is automatic, it reads him at
    /// <c>DeckPlan.DoorOpenRadius</c> and it is already open by the time he arrives. It does not read an Old
    /// One — the shelters' own law is that <i>the door reads a SUIT</i> (#585, and the shuttle's crew-only
    /// hatch before it) — so an Old One that wants a leaf open has to <b>haul it</b>.</para>
    ///
    /// <para>Hauling it is a walk: the leaf must travel <paramref name="leafLength"/>, its own width, and
    /// the only pace an Old One has is <paramref name="pace"/>, the pace it shambles at. Distance over
    /// speed. Nothing is typed and nothing is tuned: a wide leaf takes longer than a narrow one because it
    /// is wider, and re-tuning the shamble re-tunes this without anybody remembering to.</para>
    ///
    /// <para>On the shipping numbers that is a four-deck-unit leaf at 5.6 du/s ≈ 0.71 s — comfortably longer
    /// than the 0.44 s the captain would have spent crossing the same doorway, which is the "slow way" the
    /// ruling asks for, and long enough that the leaf is visibly moving with nothing behind it.</para>
    ///
    /// <para>A leaf with no width, or a pace of nothing, has nothing to haul and takes no time. That is
    /// honest rather than defensive: no plan in the game hangs a zero-width door, and a beat of zero on one
    /// that did would be a door that was already open.</para>
    /// </summary>
    public static double HaulSeconds(double leafLength, double pace) =>
        leafLength > 0 && pace > 0 ? leafLength / pace : 0;

    /// <summary>How far the leaf has slid, 0 (shut) to 1 (standing open) — the drawn half of the beat, and
    /// the whole telling. Clamped both ends so a long frame cannot overshoot the picture.
    ///
    /// <para>A leaf with no beat recorded against it is one <b>nobody has touched</b>, and that is every door
    /// in the game on an ordinary frame — so it reads SHUT. (It was briefly written to read 1 there, on the
    /// reasoning that a leaf with nothing to travel is already open; the locked-leaf guard caught it, and it
    /// would have drawn every hatch in the game standing retracted.) The genuinely zero-width leaf is
    /// distinguished by having had time banked against it, and it is over the instant it is touched.</para>
    /// </summary>
    public static double Opening(double hauledSeconds, double haulSeconds) =>
        haulSeconds > 0 ? System.Math.Clamp(hauledSeconds / haulSeconds, 0, 1)
        : hauledSeconds > 0 ? 1
        : 0;

    /// <summary>Is the leaf all the way over? Once it is, it <b>stays</b> over: they do not close doors
    /// behind them, and the leaf standing open on an empty doorway a hundred deck units back is the only
    /// sentence this whole feature ever says.</summary>
    public static bool Opened(double hauledSeconds, double haulSeconds) =>
        hauledSeconds >= haulSeconds;
}
