using SpaceSails.Core;

namespace SpaceSails.Labs.Lab44;

/// <summary>
/// LAB 44 · KNOCK ON THE HULL. Owner, specifying the search mechanic in three sentences: <i>"Sweeping could cost
/// time… like pumping vacuum does… idea is to know where to do it… so some logic on selection based on map or
/// other clues"</i> and <i>"Also it might be noisy to say knock on walls etc."</i>
///
/// <para>Which raises the one question the design lives or dies on: <b>is believing the clue actually worth
/// anything?</b> If sounding the whole ship is cheap, nobody measures anything and the deduction is decoration.
/// So before any of this is wired to a key, this asks the arithmetic five questions.</para>
/// </summary>
public static class Probe
{
    public static void Main()
    {
        Console.WriteLine("LAB 44 · KNOCK ON THE HULL — what it costs to search her, and what a clue is worth");
        Console.WriteLine(new string('=', 100));

        double hull = HullSounding.HullArea(
            WreckLayout.AftX, WreckLayout.BowX, WreckLayout.TopY, WreckLayout.BottomY);

        A_TheHullAndTheTwoGears(hull);
        B_WhatBlindCosts(hull);
        C_WhatTheHullHearsWhileYouLook(hull);
        D_TheMapAsTheClue(hull);
        E_DoesTheOddBandActuallyConverge();
        F_WhichHullsLie();
    }

    // ── A · the ship, and the two ways to ask her ────────────────────────────────────────────────────

    private static void A_TheHullAndTheTwoGears(double hull)
    {
        Console.WriteLine("\nA — the hull, and the two gears\n");
        Console.WriteLine($"  hull outline {WreckLayout.AftX}…{WreckLayout.BowX} du x " +
                          $"{WreckLayout.TopY}…{WreckLayout.BottomY} du = {hull:0} square du\n");

        Console.WriteLine("  gear          seconds   radius   per spot   earshot");
        foreach (HullSounding.Method m in Enum.GetValues<HullSounding.Method>())
        {
            double per = Math.PI * HullSounding.Radius(m) * HullSounding.Radius(m);
            Console.WriteLine($"  {m,-12}{HullSounding.Seconds(m),9:0.#}{HullSounding.Radius(m),9:0.#}" +
                              $"{per,11:0.#}{HullSounding.Earshot(m),10:0.#}");
        }
    }

    // ── B · sounding her end to end ──────────────────────────────────────────────────────────────────

    private static void B_WhatBlindCosts(double hull)
    {
        Console.WriteLine("\nB — what a blind, hull-wide search costs\n");
        Console.WriteLine("  gear           spots   standing still   in minutes");
        foreach (HullSounding.Method m in Enum.GetValues<HullSounding.Method>())
        {
            int spots = HullSounding.SpotsToCover(m, hull);
            double secs = HullSounding.SecondsToCover(m, hull);
            Console.WriteLine($"  {m,-12}{spots,8}{secs,15:0} s{secs / 60,12:0.0} min");
        }

        Console.WriteLine("\n  …and that is standing-still time only. It does not count walking between spots,");
        Console.WriteLine("  and it does not count what the ship does about it (see C).");
    }

    // ── C · the part that makes blind searching lethal rather than tedious ───────────────────────────

    private static void C_WhatTheHullHearsWhileYouLook(double hull)
    {
        Console.WriteLine("\nC — what she hears while you do it\n");

        // The wreck's own ear: one noise rouses at most 2 of the pack (NoiseRousesAtMost), and the authored
        // pack is 4. The sweep team has no cap at all — colleagues share a channel.
        const int rousesPerNoise = 2;
        const int authoredPack = 4;
        const int sweepTeam = InspectionTeam.TeamSize;

        Console.WriteLine("  gear          noises made   pack roused   sweeps alerted");
        foreach (HullSounding.Method m in Enum.GetValues<HullSounding.Method>())
        {
            int spots = HullSounding.SpotsToCover(m, hull);
            int roused = Math.Min(authoredPack, spots * rousesPerNoise);
            Console.WriteLine($"  {m,-12}{spots,13}{roused,14}{(spots > 0 ? sweepTeam : 0),17}");
        }

        int soundedSpots = HullSounding.SpotsToCover(HullSounding.Method.Sounder, hull);
        Console.WriteLine($"\n  FINDING: a blind sounder search is {soundedSpots} separate loud events. The pack's cap of");
        Console.WriteLine($"  {rousesPerNoise} per noise is a PACING rule, not a budget — it stops one racket summoning the ship, and");
        Console.WriteLine($"  it does nothing at all about {soundedSpots} rackets. The whole authored pack of {authoredPack} is up after");
        Console.WriteLine("  the second spot, and every sweeper aboard has walked to the first.");
    }

    // ── D · the deduction, off her own drawn rectangles ──────────────────────────────────────────────

    private static void D_TheMapAsTheClue(double hull)
    {
        Console.WriteLine("\nD — the map as the clue: where her books do not balance\n");

        IReadOnlyList<HullSounding.Discrepancy> balanced = HullSounding.Discrepancies(
            WreckLayout.Compartments, WreckLayout.SpineHalfHeight,
            WreckLayout.TopY, WreckLayout.BottomY);

        Console.WriteLine($"  the SHIPPED wreck layout: {balanced.Count} discrepancies");
        Console.WriteLine("  → her books balance exactly, which is the right baseline: a wreck with nothing hidden");
        Console.WriteLine("    in her must measure clean, or the clue cries wolf on every hull in the game.\n");

        // The same hull with a bulkhead moved: NEAR HOLD shortened by four frames, which is what a void built
        // into the aft end of it would look like on the plans.
        (string, float, float, bool)[] doctored =
        [
            ("BRIDGE", 13f, WreckLayout.BowX - 6, true),
            ("CREW SPACES", 0f, 13f, true),
            ("FORWARD LOCKER", 13f, WreckLayout.BowX - 6, false),
            ("LIFEBOAT CRADLES", 0f, 13f, false),
            ("DEEP HOLD", -15f, 0f, true),
            ("NEAR HOLD", -11f, 0f, false),          // four frames short of its counterpart
            ("ENGINEERING", WreckLayout.AftX, -15f, true),
            ("REACTOR SPACES", WreckLayout.AftX, -15f, false),
        ];

        IReadOnlyList<HullSounding.Discrepancy> found = HullSounding.Discrepancies(
            doctored, WreckLayout.SpineHalfHeight, WreckLayout.TopY, WreckLayout.BottomY);

        Console.WriteLine("  the same hull with NEAR HOLD's bulkhead moved four frames forward:\n");
        foreach (HullSounding.Discrepancy d in found)
        {
            Console.WriteLine($"    {d.Reason}");
            Console.WriteLine($"      -> band x {d.X0:0.#}…{d.X1:0.#} on the {(d.Top ? "top" : "bottom")} side, " +
                              $"{d.AreaSquareDu:0} square du to search\n");
        }

        if (found.Count == 0)
        {
            Console.WriteLine("    (none — the doctored layout did not register, which would be the bug)\n");
            return;
        }

        double clue = found[0].AreaSquareDu;
        Console.WriteLine("  gear             blind   on the clue   speed-up   noise saved");
        foreach (HullSounding.Method m in Enum.GetValues<HullSounding.Method>())
        {
            double blind = HullSounding.SecondsToCover(m, hull);
            double clued = HullSounding.SecondsToCover(m, clue);
            int noiseSaved = HullSounding.SpotsToCover(m, hull) - HullSounding.SpotsToCover(m, clue);
            Console.WriteLine($"  {m,-12}{blind,10:0} s{clued,12:0} s" +
                              $"{HullSounding.CluedSpeedup(m, hull, clue),10:0.0}x{noiseSaved,12} spots");
        }
    }

    // ── E · does the third answer actually help? ─────────────────────────────────────────────────────

    private static void E_DoesTheOddBandActuallyConverge()
    {
        Console.WriteLine("\nE — is the ODD reading worth having, or is two answers enough?\n");
        Console.WriteLine("  Two strategies for a void somewhere along a band of ship:");
        Console.WriteLine("    COVER   — no third answer to lean on, so tile the whole band with overlapping discs.");
        Console.WriteLine("    STRIDE  — step by the ODD band's own width; the first non-Solid note closes it in 2 more.\n");

        Console.WriteLine("  gear          band     cover      stride    winner");
        foreach (HullSounding.Method m in Enum.GetValues<HullSounding.Method>())
        {
            double reach = HullSounding.Radius(m);
            double stride = reach * HullSounding.OddBandFactor * 2;

            foreach (double bandWidth in new[] { 6.0, 12.0, 24.0, 48.0, 60.0 })
            {
                int cover = HullSounding.SpotsToCover(m, bandWidth * 6.0);
                int strideWorst = WorstStrideSpots(m, bandWidth, stride);

                string winner = strideWorst < cover ? "STRIDE"
                              : strideWorst > cover ? "cover"
                              : "tie";
                Console.WriteLine($"  {m,-12}{bandWidth,5:0}{cover,10} sp{strideWorst,10} sp    {winner}");
            }
        }

        Console.WriteLine("\n  FINDING — AND IT REFUTES WHAT I EXPECTED. The ODD band is not a general win, and on the");
        Console.WriteLine("  sounder at narrow bands it actively LOSES: its disc (r=4) is already big against a 24 du");
        Console.WriteLine("  band, so tiling costs 3 spots while striding-and-closing costs 4. The third answer only");
        Console.WriteLine("  pays where the reach is small against the distance — which is exactly the KNUCKLES case,");
        Console.WriteLine("  and exactly the case a captain is in when something is awake aboard and the loud tool is");
        Console.WriteLine("  not an option.");
        Console.WriteLine("\n  So ODD is not decoration and it is not a universal speed-up: it is what makes the QUIET");
        Console.WriteLine("  gear usable. Keep it, and do not sell it as a general convergence.");
    }

    /// <summary>Worst case over every void position: stride the band, close in on the first non-Solid note.</summary>
    private static int WorstStrideSpots(HullSounding.Method m, double bandWidth, double stride)
    {
        double reach = HullSounding.Radius(m);
        int worst = 0;

        for (double vx = 0; vx <= bandWidth; vx += 0.1)
        {
            int spots = 0;
            bool found = false;
            for (double x = reach; x <= bandWidth + stride && !found; x += stride)
            {
                spots++;
                HullSounding.Reading r = HullSounding.Read(m, x, 0, vx, 0);
                if (r == HullSounding.Reading.Hollow)
                {
                    found = true;
                }
                else if (r == HullSounding.Reading.Odd)
                {
                    spots += 2;   // within 1.75 reaches — two closing spots settle it
                    found = true;
                }
            }
            // A stride that can MISS is worth knowing about: charge it the whole band rather than pretending.
            worst = Math.Max(worst, found ? spots : HullSounding.SpotsToCover(m, bandWidth * 6.0) + spots);
        }

        return worst;
    }

    // ── F · which of the shipped hulls actually hide something ───────────────────────────────────────

    private static void F_WhichHullsLie()
    {
        Console.WriteLine();
        Console.WriteLine("F — which of the seeded hulls hide a void, and where");
        Console.WriteLine();

        int lying = 0, total = 0;
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            if (Derelict.SeededWithCause(cause) is not { } w)
            {
                continue;
            }
            total++;

            HullSounding.HiddenVoid? hidden = HullSounding.VoidFor(
                w.Id, WreckLayout.Compartments, WreckLayout.SpineHalfHeight,
                WreckLayout.TopY, WreckLayout.BottomY);

            if (hidden is not { } v)
            {
                Console.WriteLine($"  {cause,-24} {w.ShipName,-22} honest");
                continue;
            }

            lying++;
            Console.WriteLine($"  {cause,-24} {w.ShipName,-22} LIES: {v.X1 - v.X0:0.#} frames of shielding " +
                              $"outboard of {v.NearRoom} ({v.X0:0.#}…{v.X1:0.#}, " +
                              $"{(v.Top ? "top" : "bottom")}) — knock at ({v.PlateX:0.#}, {v.PlateY:0.#})");
        }

        Console.WriteLine();
        Console.WriteLine($"  {lying} of {total} seeded hulls hide something (target: about one in five).");

        // The golden value the determinism law pins. Printed here so the law can be re-derived rather than
        // guessed at if the placement ever changes on purpose.
        HullSounding.HiddenVoid? g = HullSounding.VoidFor(
            "golden-hull", WreckLayout.Compartments, WreckLayout.SpineHalfHeight,
            WreckLayout.TopY, WreckLayout.BottomY);
        Console.WriteLine();
        Console.WriteLine("  GOLDEN 'golden-hull' -> " +
                          (g is { } gv
                              ? $"{gv.NearRoom} outboard={gv.Outboard} plate=({gv.PlateX:0.###},{gv.PlateY:0.###}) " +
                                $"band={gv.X0:0.###}…{gv.X1:0.###} area={gv.AreaSquareDu:0.###}"
                              : "honest"));
        Console.WriteLine("  A void on every wreck makes measuring routine; the appeal is that most ships are");
        Console.WriteLine("  exactly what they look like.");
    }
}
