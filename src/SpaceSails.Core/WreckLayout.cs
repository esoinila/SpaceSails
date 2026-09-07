namespace SpaceSails.Core;

/// <summary>
/// #488 · The derelict's GEOMETRY, in Core so it can be audited.
///
/// <para>This is the same split the surfaces already use — <see cref="SurfaceLayout"/> holds the shape in
/// Core and the client wraps it into a DeckPlan. It exists here for one reason: the owner boarded the
/// LONG SHRIFT and found the ship sealed in half by its own damage, with every build green, because the
/// layout lived in the client where no test could walk it.</para>
///
/// <para>Now <see cref="DeckReachability"/> can. A wreck whose compartments cannot be reached from the
/// airlock fails CI instead of failing a captain.</para>
///
/// <para>#251 · This file keeps HER SHAPE — the box she is drawn in, the shielding band, the nose, and
/// the outline. The other four are named for what is built on it: <c>.Structure</c> (#537's machinery
/// space and the bulkheads, and the fills that make a wall thick), <c>.Doors</c> (the spawn, the
/// compartments, the doorways, the shuttle lock and the lifeboat cradles), <c>.Walls</c> (every collision
/// segment on the wreck, and the damage that killed her), and <c>.Stations</c> (where things stand, one
/// definition each, read by BOTH the client that places a console and the audit that walks to it).</para>
///
/// <para>The family's one static field, <c>Compartments</c>, is a literal table and reads no other
/// static, so no initializer chain can be broken by the cut — see
/// <c>NoPartialClassSpreadsItsStaticFieldsTests</c>. Every other number here is a <c>const</c>, and a
/// <c>const</c> is folded at compile time and cannot be ordered wrongly at all. No member is renamed,
/// re-scoped or re-ordered.</para>
/// </summary>
public static partial class WreckLayout
{
    // The hull, in deck units. A long thin ship: bow to the right, engineering aft to the left, one spine
    // corridor down the middle, compartments hanging off it.
    public const float AftX = -34f;
    public const float BowX = 26f;
    public const float TopY = -9f;
    public const float BottomY = 9f;

    /// <summary>Half the spine corridor's height — the corridor runs from −<see cref="SpineHalfHeight"/>
    /// to +<see cref="SpineHalfHeight"/>.</summary>
    public const float SpineHalfHeight = 3f;

    /// <summary>
    /// #537 · THE SHIELDING BAND, outboard of the pressure hull the whole length of her parallel middle body.
    ///
    /// <para>Owner, on being shown that a hidden void had nowhere to physically BE — the compartments are
    /// contiguous, so every square metre of her was already spoken for: <i>"I guess making outside walls thicker
    /// (shielding etc) might offer less audited dimensions? Or having like technical plumbing space in the between
    /// walls?"</i> That is the right answer and it is also how spacecraft are actually built: whipple layers,
    /// radiation shielding, tankage, cable and plumbing runs all live between an inner pressure wall and an outer
    /// skin.</para>
    ///
    /// <para>It solves the problem structurally rather than by fiddling with numbers. A band outboard of the
    /// rooms has <b>no doorway to respect</b>, so a void can sit anywhere along her length — which the previous
    /// attempt could not manage: respecting each compartment's own door left the bow rooms with NEGATIVE margin
    /// and only the aft holds able to host anything. And it answers the owner's question directly: the room is
    /// not smaller on the inside. The space is between the walls, where it belongs.</para>
    ///
    /// <para><b>Every hull has it</b>, and that is the anti-tell rule (his own, from the valve boards): if only
    /// ships with something to hide carried a shielding band, finding a shielding band would name the ship.</para>
    /// </summary>
    public const float ShieldingDepth = 2.5f;

    /// <summary>The outer skin — the pressure hull plus her shielding. Only along the parallel middle body; the
    /// bow taper carries no band, because shielding runs down the sides of a ship and not around her nose.</summary>
    public const float OuterTopY = TopY - ShieldingDepth;

    /// <summary>…and the same on the other side.</summary>
    public const float OuterBottomY = BottomY + ShieldingDepth;

    /// <summary>Where the band runs forward to. Aft it runs to the transom.</summary>
    public const float ShieldingForwardEnd = BowX - 6;

    /// <summary>Half the flat of her nose, where the bow taper stops. Named rather than typed twice because
    /// the silhouette and the collision shell have to be cut from the same number: the shape a captain walks
    /// and the shape an instrument draws are one ship, or one of them is lying.</summary>
    public const float NoseHalfHeight = 2f;

    /// <summary>
    /// #241 · HER SILHOUETTE — the outer skin as one closed polyline, in deck units, bow to +X, tracing
    /// exactly what <see cref="Walls"/> lays: the transom, the shielding band, the forward end of the band,
    /// the bow taper, the flat of the nose, and back down the other side.
    ///
    /// <para>It exists so the SCOPE has a wreck to draw (#241 asks for "a wireframe-per-body-class seam …
    /// future wrecks and oddities each get a portrait without new plumbing") without a second set of numbers
    /// being typed into a renderer. Bug class 1 in this repo is a literal in a drawing that nothing derives
    /// or checks, found wrong three times out of three; the cure is that the picture and the deck read the
    /// same constants, so a hull that changes shape changes shape in both places or in neither.</para>
    /// </summary>
    public static IReadOnlyList<(float X, float Y)> HullOutline() =>
    [
        (TransomX, OuterTopY),
        (ShieldingForwardEnd, OuterTopY),
        (ShieldingForwardEnd, TopY),
        (BowX, -NoseHalfHeight),
        (BowX, NoseHalfHeight),
        (ShieldingForwardEnd, BottomY),
        (ShieldingForwardEnd, OuterBottomY),
        (TransomX, OuterBottomY),
        (TransomX, OuterTopY),
    ];
}
