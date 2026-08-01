using System;

namespace SpaceSails.Core;

/// <summary>
/// #589 · EVERY WORLD ITS OWN COLOUR — because you should be able to tell where you are at a glance.
///
/// <para>Owner, part-way through touring the rebuilt grounds (<i>"phobos and luna look both good"</i>,
/// <i>"ganymede also looks good"</i>, <i>"titan look good also"</i>) and then, three times, the thing that
/// was missing:</para>
/// <list type="bullet">
/// <item><i>"Maybe the sites should have like different color themes ... for the building walls or something
/// to make them look little different from each others."</i></item>
/// <item><i>"like the in-situ construction materials of the walls might be planet specific ... red for mars
/// etc theming"</i> · <i>"gray for Moon"</i></item>
/// <item><i>"something to spot where we are visually"</i></item>
/// </list>
///
/// <para>The reasoning is his own from the Greenland longhouse ruling and it is airtight: <b>you build out of
/// what is under your boots.</b> A wall on Phobos is made of Phobos. So the ink a ruin is drawn in is a fact
/// about the body, not decoration — and it does the navigational job for free, because after two visits the
/// palette alone tells you which moon you are standing on before you have read a single label.</para>
///
/// <para>Kept in Core rather than the renderer for the usual reason: this is a fact about a world, the same
/// kind of fact as its geography scheme, and the renderer is only the thing that happens to draw it. It is
/// also the sort of table that WILL be wanted by a second surface (a lab, a map legend), and one copy is the
/// standing rule on this ground.</para>
/// </summary>
public static class BodyPalette
{
    /// <summary>The ink a body's in-situ stonework is drawn in: 0-255 per channel.</summary>
    public readonly record struct Ink(byte R, byte G, byte B);

    /// <summary>The default — the warm grey-brown the surfaces were all drawn in before bodies had their
    /// own. Anything unknown falls back here rather than to something loud, so a new world added without a
    /// palette entry looks ordinary instead of looking broken.</summary>
    public static readonly Ink Regolith = new(166, 150, 130);

    /// <summary>What this body's ruins are built out of, as ink.
    ///
    /// <para>Every entry is the body's real surface material, chosen so that no two neighbours in the same
    /// system read alike — the point is telling them apart, so Jupiter's three moons are deliberately spread
    /// across the range rather than all being "ice".</para></summary>
    public static Ink For(string? bodyId) => (bodyId ?? "").ToLowerInvariant() switch
    {
        // Owner's own two examples, verbatim: grey for the Moon, red for Mars' side of the system.
        "luna" => new Ink(178, 178, 182),          // anorthosite highlands — plain, cold grey
        "phobos" => new Ink(168, 104, 78),         // carbonaceous, rusted by Mars next door

        // Venus' burned rock: dark basalt with the heat still implied.
        "the-clinker" => new Ink(120, 92, 84),

        // The Jovian moons, deliberately far apart from one another.
        "europa" => new Ink(150, 190, 210),        // water ice, faintly blue
        "ganymede" => new Ink(140, 148, 168),      // dirty ice over silicate — bluish grey
        "callisto" => new Ink(112, 100, 96),       // the darkest, most cratered thing out there

        // Saturn's pair.
        "titan" => new Ink(198, 158, 96),          // tholin haze — the orange world
        "enceladus" => new Ink(224, 232, 238),     // fresh white ice, the brightest surface in the system

        // Canon ground, and the far end of the run.
        "miranda" => new Ink(166, 150, 130),       // the warm grey-brown Miranda has always been drawn in
        "triton" => new Ink(196, 172, 186),        // nitrogen frost with a pink cast

        _ => Regolith,
    };

    /// <summary>The same ink, dimmed for the lighter inner lines so a body's theme carries through the whole
    /// drawing rather than sitting only on its heaviest strokes.</summary>
    public static Ink Inner(string? bodyId)
    {
        Ink ink = For(bodyId);
        return new Ink((byte)(ink.R * 0.66), (byte)(ink.G * 0.66), (byte)(ink.B * 0.66));
    }

    /// <summary>#592 · The ink an ORDINARY door is drawn in on this world: the local stone, brightened, so a
    /// hatch reads as made of the same hill as the wall it is set in.
    ///
    /// <para>Owner: <i>"maybe the door color could also be moon / landing site specific ... some special
    /// color not distinctive to the site could then used to draw our attention to a place (like expensive
    /// door made with far away imported materials)."</i></para>
    ///
    /// <para>That turns the palette from decoration into a LANGUAGE. Once every door on a moon is the colour
    /// of that moon, the one door that is not becomes a sentence: somebody shipped materials across the
    /// system to seal this, and nobody does that for a store cupboard.</para></summary>
    public static Ink DoorInk(string? bodyId)
    {
        Ink ink = For(bodyId);
        return new Ink(Lift(ink.R), Lift(ink.G), Lift(ink.B));
    }

    private static byte Lift(byte c) => (byte)Math.Min(255, 60 + (c * 0.85));

    /// <summary>The colour of a door that was NOT made here — pale, cold and slightly wrong against every
    /// world's own stone, because it did not come from any of them.
    ///
    /// <para>Deliberately outside the whole body palette: it must never be mistakable for "we are on that
    /// moon", or the language stops working the moment a captain lands somewhere pale. Guarded.</para>
    ///
    /// <para>It was a cold blue-white first, and the guard killed it in one run: on Luna that sat 69 from the
    /// local grey and on Enceladus closer still — an "unmistakable" signal that vanished on precisely the two
    /// palest worlds. So: <b>violet</b>, because no rock anywhere is violet. It cannot be read as a world's
    /// own stone under any lighting, it is nothing else on the deck (amber ship doors, green consoles, red
    /// Old Ones, amber collectors), and it says the one thing it needs to say — this did not come from
    /// here.</para></summary>
    public static readonly Ink Imported = new(190, 90, 220);

    /// <summary>How far <see cref="Imported"/> stands off a world's own door ink — the number the guard
    /// pins, so an imported door can never be a subtle shade of local.</summary>
    public static int ImportedContrastFrom(string? bodyId)
    {
        Ink local = DoorInk(bodyId);
        return Math.Abs(local.R - Imported.R) + Math.Abs(local.G - Imported.G) + Math.Abs(local.B - Imported.B);
    }

    /// <summary>A one-word name for the material, for anywhere a caption wants to say what the wall is.</summary>
    public static string MaterialOf(string? bodyId) => (bodyId ?? "").ToLowerInvariant() switch
    {
        "luna" => "highland anorthosite",
        "phobos" => "rusted carbonaceous rubble",
        "the-clinker" => "burned basalt",
        "europa" => "water ice",
        "ganymede" => "dirty ice and silicate",
        "callisto" => "old cratered rock",
        "titan" => "tholin-stained ice",
        "enceladus" => "fresh water ice",
        "miranda" => "cliff rubble",
        "triton" => "nitrogen frost and rock",
        _ => "local regolith",
    };
}
