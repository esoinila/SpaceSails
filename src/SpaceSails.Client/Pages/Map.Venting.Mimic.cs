using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Venting — the mimic's geometry: the ship drawn out of her own numbers, and every name and state tag fitted into the compartment it belongs to.
public sealed partial class Map
{
    // ── The mimic's geometry, taken from the ship's own numbers ───────────────────────────────────────

    /// <summary>A deck unit as an SVG coordinate: ALWAYS invariant. Blazor renders a float attribute in the
    /// current culture, so on a Finnish browser 20.5 becomes "20,5" — which SVG reads as two numbers and the
    /// map comes apart. It would have broken for the owner and for nobody else.</summary>
    private static string Du(double v) =>
        v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The mimic's frame — the audit's own playable bounds, so the map shows exactly the ship the
    /// A* walks.</summary>
    private static string VentViewBox
    {
        get
        {
            (double minX, double minY, double maxX, double maxY) = WreckLayout.Bounds;
            return $"{Du(minX)} {Du(minY)} {Du(maxX - minX)} {Du(maxY - minY)}";
        }
    }

    /// <summary>The hull outline, built from <see cref="WreckLayout"/>'s constants rather than drawn by
    /// hand: flat transom aft, tapering bow forward. If the ship's shape ever changes, the mimic changes
    /// with it. (Symmetric about the spine, so negating Y for SVG leaves it unchanged.)</summary>
    private static string VentHullOutline
    {
        get
        {
            float taper = WreckLayout.BowX - 6;
            return string.Join(' ',
                $"{Du(WreckLayout.AftX)},{Du(WreckLayout.BottomY)}",
                $"{Du(taper)},{Du(WreckLayout.BottomY)}",
                $"{Du(WreckLayout.BowX)},2",
                $"{Du(WreckLayout.BowX)},-2",
                $"{Du(taper)},{Du(WreckLayout.TopY)}",
                $"{Du(WreckLayout.AftX)},{Du(WreckLayout.TopY)}");
        }
    }

    /// <summary>Where THIS compartment's doorway onto the spine actually is. The spine has four openings,
    /// not eight — each one serves the room above it and the room below it — so a door drawn at every
    /// compartment's midpoint would be showing the captain a way through that is not there.</summary>
    private static float VentDoorX(float x0, float x1)
    {
        foreach (float centre in WreckLayout.DoorCentres())
        {
            if (centre > x0 && centre < x1)
            {
                return centre;
            }
        }
        return (x0 + x1) / 2f;
    }

    /// <summary>The one word a compartment wears on the mimic, and the class that colours it. Empty when the
    /// room has nothing to say yet — an unread compartment should look unread.</summary>
    private (string Text, string Class) VentAreaTag(string name, HullVenting.Space space, float roomWidth)
    {
        if (space.Vented)
        {
            // The counter the owner asked for. It says how long the room has been open and DELIBERATELY not
            // how long it needs — the second number does not exist for the captain, which is the whole
            // decision. A hull that arrived vented decades ago just reads VACUUM; no clock is interesting.
            if (space.VacuumSeconds >= YearsOfVacuumSeconds)
            {
                return ("VACUUM", "vent-tag");
            }

            // FORWARD LOCKER is seven units wide and "VACUUM 00:22" is twelve characters, so the running
            // clock hung straight out over the hull (owner, mid-vent: "the vacuum text is clipped during
            // the process"). Offer the same fact at three lengths and let the room pick — the clock is the
            // part that must never be dropped, and the room's own dark fill already says vacuum.
            string clock = HullVenting.SoakLabel(space.VacuumSeconds);
            return (Longest(roomWidth, [$"VACUUM {clock}", $"VAC {clock}", clock]), "vent-tag");
        }
        // PUMPING RIGHT NOW. Owner: "we have the vacuum indicated but not the pumping on the map … should
        // there be some kind of pumping right now indicator here." Yes — with several pumps able to run at
        // once, the board is the only place that can tell you which rooms are working and how far along
        // each one is. The room says so itself, and it counts DOWN, because unlike the soak this clock has
        // a known end.
        if (PumpOn(name) is { } pumping)
        {
            string t = HullVenting.SoakLabel(pumping.SecondsLeft);
            return (Longest(roomWidth, [$"PUMPING {t}", $"PUMP {t}", t]),
                    pumping.RoughBanked ? "vent-tag banked-tag" : "vent-tag pumping-tag");
        }

        // #524 · FIRE FIRST. It is the only tag that is actively costing the captain money while they read it.
        if (_burning.TryGetValue(name, out double alight))
        {
            return (HullFire.Tag(alight), "vent-tag alive-tag");
        }

        if (space.CaptainInside)
        {
            return ("YOU", "vent-tag here-tag");
        }
        if (_ventReads.TryGetValue(name, out (DiceRoll Roll, HullVenting.LifeSign Sign) rd))
        {
            return rd.Sign switch
            {
                HullVenting.LifeSign.SomethingAlive => ("ALIVE?", "vent-tag alive-tag"),
                HullVenting.LifeSign.Empty => ("cold", "vent-tag"),
                _ => ("??", "vent-tag"),
            };
        }
        return ("", "vent-tag");
    }

    /// <summary>
    /// The compartment's name (and its one-word state) as SVG. Razor reserves <c>&lt;text&gt;</c> for its own
    /// control flow, so the labels are built here and injected as markup. Names are drawn INSIDE the room they
    /// belong to — owner: <i>"a map with named sections so if you don't remember the name of the room you
    /// still know to vent the right place."</i>
    ///
    /// <para>TURN THE NAME SIDEWAYS WHEN THE ROOM IS TALL AND NARROW. Owner, looking at her berths on the new
    /// board: <i>"the cabins map here could use font rotation to fit the texts?"</i> — CABIN 3 / CABIN 2 /
    /// CABIN 1 / THE HEAD are 3.5 du wide and 7 tall, so four names ran into each other and read as
    /// "CABIŇABINABINTHE". A berth has plenty of room; it just has it in the other direction.</para>
    /// </summary>
    /// <param name="roomHeight">How tall the compartment is. Zero means "do not consider rotating" — a caller
    /// that has not thought about it gets the old behaviour rather than a surprise.</param>
    private static string VentAreaLabelSvg(
        string label, float cx, float cy, float roomWidth, (string Text, string Class) tag,
        float roomHeight = 0f)
    {
        System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
        string x = cx.ToString("0.##", inv);

        // FIT THE NAME TO THE ROOM. "FORWARD LOCKER" is fourteen characters in a seven-unit compartment,
        // and at one size for every room it ran straight over its neighbours (owner, mid-playtest: "maybe
        // some text overlap on map on smaller rooms"). Wrap onto two lines first, because a name at a
        // readable size on two lines beats a name shrunk to fit on one; only then shrink.
        float avail = System.Math.Max(2f, roomWidth - LabelPadding);

        string[] lines = [label];
        if (Widest(lines) * BaseLabelSize > avail && label.Contains(' ', System.StringComparison.Ordinal))
        {
            lines = label.Split(' ');
        }

        // THE SIDEWAYS CASE — a LAST resort, tried only after wrapping has failed. ENGINE ROOM is 10 × 20 and
        // taller than it is wide, but it breaks happily onto two lines and reads better that way; a berth is
        // 3.5 × 7 and CABIN 2 cannot be broken into anything that fits across it, so it turns. The test is
        // therefore "can the WRAPPED name fit flat", not "is this room tall".
        if (roomHeight > roomWidth
            && Widest(lines) * BaseLabelSize > avail
            && (roomHeight - LabelPadding) > avail)
        {
            return SidewaysLabelSvg(label, cx, cy, roomHeight, tag, inv);
        }

        float size = System.Math.Min(BaseLabelSize, avail / System.Math.Max(1f, Widest(lines)));
        float lead = size * 1.25f;
        float top = cy - ((lines.Length - 1) * lead / 2f);

        var svg = new System.Text.StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            svg.Append(inv, $"""<text x="{x}" y="{(top + (i * lead)).ToString("0.##", inv)}" """)
               .Append(inv, $"""{FontSize(size, inv)} text-anchor="middle">""")
               .Append(System.Net.WebUtility.HtmlEncode(lines[i]))
               .Append("</text>");
        }

        if (tag.Text.Length > 0)
        {
            // Backstop: even the shortest candidate has to fit a narrow room, so shrink rather than clip.
            float tagSize = System.Math.Min(TagLabelSize, avail / System.Math.Max(1f, tag.Text.Length * 0.6f));
            string ty = (cy + 2.1f).ToString("0.##", inv);
            svg.Append(inv, $"""<text class="{tag.Class}" x="{x}" y="{ty}" """)
               .Append(inv, $"""{FontSize(tagSize, inv)} text-anchor="middle">""")
               .Append(System.Net.WebUtility.HtmlEncode(tag.Text))
               .Append("</text>");
        }

        return svg.ToString();
    }

    /// <summary>
    /// THE NAME TURNED SIDEWAYS, reading bottom-to-top up a tall narrow compartment.
    ///
    /// <para>Rotating about the room's own centre means the name stays centred by construction, and the
    /// fitting arithmetic is the same arithmetic as the flat case with the room's HEIGHT standing in for its
    /// width. The state tag rides beside it: under <c>rotate(-90)</c> a local offset of (0, d) lands at
    /// (d, 0), so the same "one line below" offset the flat label uses puts the tag alongside the name here,
    /// which is where there is room for it.</para>
    /// </summary>
    private static string SidewaysLabelSvg(
        string label, float cx, float cy, float roomHeight, (string Text, string Class) tag,
        System.Globalization.CultureInfo inv)
    {
        float along = System.Math.Max(2f, roomHeight - LabelPadding);
        float size = System.Math.Min(BaseLabelSize, along / System.Math.Max(1f, Widest([label])));

        string x = cx.ToString("0.##", inv);
        string y = cy.ToString("0.##", inv);
        string pivot = $"rotate(-90 {x} {y})";

        var svg = new System.Text.StringBuilder();
        svg.Append(inv, $"""<text x="{x}" y="{y}" transform="{pivot}" """)
           .Append(inv, $"""{FontSize(size, inv)} text-anchor="middle">""")
           .Append(System.Net.WebUtility.HtmlEncode(label))
           .Append("</text>");

        if (tag.Text.Length > 0)
        {
            float tagSize = System.Math.Min(TagLabelSize, along / System.Math.Max(1f, tag.Text.Length * 0.6f));
            string ty = (cy + SidewaysTagOffset).ToString("0.##", inv);
            svg.Append(inv, $"""<text class="{tag.Class}" x="{x}" y="{ty}" transform="{pivot}" """)
               .Append(inv, $"""{FontSize(tagSize, inv)} text-anchor="middle">""")
               .Append(System.Net.WebUtility.HtmlEncode(tag.Text))
               .Append("</text>");
        }

        return svg.ToString();
    }

    /// <summary>How far the state tag sits from a sideways name. Smaller than the flat case's 2.1: after the
    /// rotation this is a sideways offset, and the rooms that get rotated are the narrow ones.</summary>
    private const float SidewaysTagOffset = 1.15f;

    /// <summary>
    /// The font size as an INLINE STYLE rather than a presentation attribute.
    ///
    /// <para>This is why the fitting arithmetic above had never actually done anything. A CSS declaration
    /// beats an SVG presentation attribute, and the stylesheet says
    /// <c>.vent-map ::deep .vent-area text { font-size: 1.55px }</c> — so every carefully shrunk
    /// <c>font-size="0.83"</c> was computed, written into the DOM, and then ignored by the browser. The
    /// wreck's compartments are wide enough that nobody could see it; her berths are 3.5 du and it showed up
    /// as four names printed on top of each other. An inline style wins, so the number now reaches the
    /// glyphs.</para>
    /// </summary>
    private static string FontSize(float size, System.Globalization.CultureInfo inv) =>
        $"""style="font-size:{size.ToString("0.##", inv)}px" """;

    /// <summary>The longest of these that actually fits inside the compartment at the tag's own size. The
    /// candidates must be ordered fullest-first and the LAST one must always fit — it is the fallback, so
    /// it should carry only the part that cannot be dropped.</summary>
    private static string Longest(float roomWidth, string[] candidates)
    {
        float avail = System.Math.Max(2f, roomWidth - LabelPadding);
        foreach (string c in candidates)
        {
            if (c.Length * 0.6f * TagLabelSize <= avail)
            {
                return c;
            }
        }
        return candidates[^1];
    }

    /// <summary>The label size a roomy compartment gets. Narrow ones come down from here, never up.</summary>
    private const float BaseLabelSize = 1.55f;

    /// <summary>The state tag's size — kept in step with the <c>.vent-tag</c> rule in the stylesheet, since
    /// the fitting arithmetic has to know what the browser will actually draw.</summary>
    private const float TagLabelSize = 1.35f;

    /// <summary>Clearance left inside each compartment's bulkheads, in deck units.</summary>
    private const float LabelPadding = 1.2f;

    /// <summary>Width of the longest line in EM, for a monospace face (advance ≈ 0.6 em per character).
    /// Multiply by a font size to get deck units.</summary>
    private static float Widest(string[] lines)
    {
        int longest = 0;
        foreach (string s in lines)
        {
            longest = System.Math.Max(longest, s.Length);
        }
        return longest * 0.6f;
    }

    /// <summary>The compartments the board lists, aft to bow so the mimic reads like the ship does.</summary>
    private static IEnumerable<(string Name, bool Top)> VentBoardRow(bool top) =>
        WreckLayout.Compartments
            .Where(c => c.Top == top)
            .OrderBy(c => c.X0)
            .Select(c => (c.Name, c.Top));
}
