namespace SpaceSails.Core;

/// <summary>
/// PR-313 · The surface-excursion motion tracker (owner: "let's borrow the motion detector ... to see
/// the Reevers further away, so we don't have to run so fast"). A crude handheld sweep — a generic
/// sci-fi device, no film in the name — that reads MOVEMENT, not presence: it paints a blip for every
/// contact that is actually moving, by bearing and range, including well beyond the visible grid edge,
/// and says nothing at all about a contact holding still. The dread survives the tool: a quiet tracker
/// is not a safe moon, only a patient one.
///
/// <para>Pure and deterministic so the sweep, the motion gate and the ping cadence can all be pinned in
/// tests. Deck units, matching <see cref="OverlayBands"/>-compliant HUD placement the client draws in
/// the corner (never over the surface grid — the dig-channel watch law, #313).</para>
/// </summary>
public static class MotionTracker
{
    /// <summary>Below this speed (deck-units/second) a contact reads as STILL and the tracker is blind
    /// to it — motion only. Small enough that a shambling Old One always paints, large enough that render
    /// jitter never conjures a phantom blip.</summary>
    public const double StillSpeed = 0.15;

    /// <summary>One thing the tracker can see: its bearing (radians, world frame — atan2(dy,dx) from the
    /// captain) and its range (deck units). Only moving contacts are ever emitted.</summary>
    public readonly record struct Blip(double Bearing, double Range);

    /// <summary>An entity the tracker sweeps: where it is and how fast it is going. The client feeds one
    /// per Reever (and per own droid, if any move — it is a motion tracker, not a Reever detector).</summary>
    public readonly record struct Entity(double X, double Y, double Vx, double Vy);

    /// <summary>How fast the blips pulse — the audible/visual cadence that quickens as the nearest mover
    /// closes. The client maps this to a blink rate (and, TODO, an audio ping when an audio system
    /// exists — no audio today).</summary>
    public enum Cadence
    {
        /// <summary>Nothing moving in reach — the tracker rests.</summary>
        Silent,
        /// <summary>A mover far out — a slow, occasional blip.</summary>
        Distant,
        /// <summary>Closing — a steady pulse.</summary>
        Closing,
        /// <summary>On top of you — a frantic pulse.</summary>
        Imminent,
    }

    /// <summary>True if this contact is moving fast enough for the tracker to see it.</summary>
    public static bool IsMoving(double vx, double vy) => (vx * vx) + (vy * vy) > StillSpeed * StillSpeed;

    // ── #338 "The long ear": the tracker hears them long before you see them. ──────────────────────
    // Owner design intent (2026-07-18): "the tracker warns us long before we see any Reevers on the map …
    // the tracker range in the movie was like 100-200 meters … the silent horror is there as Reevers come
    // to see what landed." The instrument's whole purpose is the dread-gap: signal FIRST, sight much later.

    /// <summary>#338 law 1: the tracker's detection reach as a MULTIPLE of the eye's reach — the owner
    /// asked for order 3-5× the viewport half-extent, so a blip at the fan's edge is a long shamble away
    /// from ever cresting into view. Owner-tunable; the single knob that widens or narrows the dread-gap.
    /// (At the shamble of <c>Map.ReeverSpeed</c> a larger multiple is more seconds of warning — nudge it
    /// up if the owner wants the fuller "30-60 seconds out" the movie sells.)</summary>
    public const double VisualRangeMultiple = 4.0;

    /// <summary>The tracker's outer detection radius (deck units) for a viewport whose visible half-extent
    /// toward the horizon is <paramref name="visualHalfExtentDu"/>: that half-extent ×
    /// <see cref="VisualRangeMultiple"/>. A contact at this range sits on the fan's rim and is nowhere near
    /// visible on the grid yet (#338 law 1); it is also the range the client maps to the ring edge.</summary>
    public static double DetectionRange(double visualHalfExtentDu) =>
        System.Math.Max(1.0, visualHalfExtentDu) * VisualRangeMultiple;

    /// <summary>The faintest a far blip ever paints, as a fraction of a point-blank one — a contact out on
    /// the rim is a whisper, never nothing (#338: "blips at extreme range render faint/small").</summary>
    public const double FaintFloor = 0.18;

    /// <summary>#338 law 1 ("blips at extreme range render faint/small, firming as they close — the fan
    /// itself tells distance"). A 0..1 firmness for a contact at <paramref name="range"/> within a
    /// <paramref name="detectionRange"/>: 1 point-blank, easing linearly toward <see cref="FaintFloor"/> at
    /// the rim and holding that floor beyond it. The client scales the blip's size and alpha by this so the
    /// sweep grades from a distant murmur to an insistent near dot — distance read straight off the fan.</summary>
    public static double BlipIntensity(double range, double detectionRange)
    {
        if (detectionRange <= 0)
        {
            return 1.0;
        }
        double t = System.Math.Clamp(range / detectionRange, 0.0, 1.0); // 0 = point-blank, 1 = on the rim
        return FaintFloor + ((1.0 - FaintFloor) * (1.0 - t));
    }

    /// <summary>#338: how many of <paramref name="entities"/> are MOVING and inside
    /// <paramref name="detectionRange"/> of the origin — the count the tracker actually HEARS (a mover
    /// still off the far end of the long ear does not count). Drives the first-contact chirp edge.</summary>
    public static int DetectedMovingCount(double originX, double originY,
        System.Collections.Generic.IEnumerable<Entity> entities, double detectionRange)
    {
        System.ArgumentNullException.ThrowIfNull(entities);
        double r2 = detectionRange * detectionRange;
        int n = 0;
        foreach (Entity e in entities)
        {
            if (!IsMoving(e.Vx, e.Vy))
            {
                continue;
            }
            double dx = e.X - originX, dy = e.Y - originY;
            if ((dx * dx) + (dy * dy) <= r2)
            {
                n++;
            }
        }
        return n;
    }

    // ── #338 addendum (owner, 2026-07-18 — THE GAME'S FIRST SOUND): the first-contact chirp. "Some kind of
    // sound on the first detected Reever … even if the device is slung the sound would tell that something
    // is up." Edge-triggered on the 0→N transition, re-arming only after the fan has been genuinely clear
    // for a while (no re-chirp spam as one contact flickers at extreme range). The Core edge/hysteresis is
    // pure and pinned here; the actual tone is the client's Web Audio layer (manual to verify). ─────────

    /// <summary>How long the fan must be genuinely clear of movers before the first-contact chirp re-arms
    /// (seconds) — the hysteresis that keeps a single contact jittering at extreme range from re-chirping.
    /// Owner: "re-arms only after the fan has been genuinely clear for a while."</summary>
    public const double ChirpReArmSeconds = 4.0;

    /// <summary>The first-contact chirp's edge state. <see cref="Armed"/> = a fresh contact would chirp;
    /// <see cref="ClearSeconds"/> = how long the fan has been empty of movers. Pure so the 0→N edge and the
    /// re-arm hysteresis pin in a test — the tone itself is client-side.</summary>
    public readonly record struct ChirpState(bool Armed, double ClearSeconds)
    {
        /// <summary>The excursion's opening state: armed and long-clear, so the very first mover chirps.</summary>
        public static ChirpState Fresh => new(Armed: true, ClearSeconds: ChirpReArmSeconds);
    }

    /// <summary>Advance the chirp edge one frame. <paramref name="movingContacts"/> is how many movers the
    /// tracker currently hears (see <see cref="DetectedMovingCount"/>), <paramref name="dtSeconds"/> the
    /// frame time. Returns the next state and whether the chirp fires THIS frame — true only on the 0→N
    /// transition while armed. Firing disarms until the fan has been clear for
    /// <see cref="ChirpReArmSeconds"/>; a contact that lingers never re-chirps.</summary>
    public static (ChirpState Next, bool Chirp) StepChirp(ChirpState prev, int movingContacts, double dtSeconds)
    {
        if (movingContacts > 0)
        {
            bool chirp = prev.Armed;
            return (new ChirpState(Armed: false, ClearSeconds: 0.0), chirp);
        }
        double clear = prev.ClearSeconds + System.Math.Max(0.0, dtSeconds);
        bool armed = prev.Armed || clear >= ChirpReArmSeconds;
        return (new ChirpState(armed, clear), false);
    }

    /// <summary>Read one entity relative to the captain at (<paramref name="originX"/>,
    /// <paramref name="originY"/>): a <see cref="Blip"/> if it is moving, else null (still → invisible).</summary>
    public static Blip? Read(double originX, double originY, in Entity e)
    {
        if (!IsMoving(e.Vx, e.Vy))
        {
            return null;
        }
        double dx = e.X - originX, dy = e.Y - originY;
        return new Blip(System.Math.Atan2(dy, dx), System.Math.Sqrt((dx * dx) + (dy * dy)));
    }

    /// <summary>Sweep every entity and return a blip for each mover, nearest first. Still contacts are
    /// dropped by construction (motion only).</summary>
    public static System.Collections.Generic.IReadOnlyList<Blip> Sweep(
        double originX, double originY, System.Collections.Generic.IEnumerable<Entity> entities)
    {
        System.ArgumentNullException.ThrowIfNull(entities);
        var blips = new System.Collections.Generic.List<Blip>();
        foreach (Entity e in entities)
        {
            if (Read(originX, originY, in e) is { } b)
            {
                blips.Add(b);
            }
        }
        blips.Sort((a, b) => a.Range.CompareTo(b.Range));
        return blips;
    }

    /// <summary>The ping cadence for the nearest moving contact's range, or <see cref="Cadence.Silent"/>
    /// when nothing moves. Bands: ≤6 du → imminent, ≤18 du → closing, farther → distant.</summary>
    public static Cadence CadenceFor(double? nearestRange) => nearestRange switch
    {
        null => Cadence.Silent,
        <= 6.0 => Cadence.Imminent,
        <= 18.0 => Cadence.Closing,
        _ => Cadence.Distant,
    };

    /// <summary>PR-330 · The largest tracker radius up to <paramref name="desired"/> that still fits the
    /// left-edge column beneath the SANITY plate at this viewport — so a small screen SHRINKS the disc
    /// proportionally rather than clipping it (owner: bigger, and fully visible). Floored so it never
    /// collapses to nothing.</summary>
    public static double TrackerRadius(double width, double height, double columnTop, double desired)
    {
        const double margin = 10, leftInset = 18, labelReserve = 18, readoutReserve = 24, minR = 44;
        double horiz = (width - margin - leftInset - 12) / 2.0;
        double vert = (height - margin - columnTop - labelReserve - readoutReserve) / 2.0;
        double r = System.Math.Min(desired, System.Math.Min(horiz, vert));
        return System.Math.Max(minR, r);
    }

    /// <summary>PR-330 · The tracker's screen anchor (owner: "let's put the motion under the sanity
    /// meter"): a left-edge instrument column, the disc centred directly BELOW the SANITY plate. Pure so
    /// the placement clamps like a menu would — the whole widget (its caption above, its readout below)
    /// is kept inside the viewport, shifting up at small heights so the bottom keybar is never buried.
    /// <paramref name="columnTop"/> is the y the column may start at (the SANITY plate's bottom + gap).</summary>
    public static (double Cx, double Cy) TrackerAnchor(double width, double height, double radius, double columnTop)
    {
        const double margin = 10, leftInset = 18, labelReserve = 18, readoutReserve = 24;
        double cx = System.Math.Min(leftInset + radius + 6, System.Math.Max(margin, width - margin - radius - 6));
        double halfTop = radius + labelReserve;   // disc + the caption above it
        double halfBot = radius + readoutReserve;  // disc + the readout below it
        double loCy = margin + halfTop;
        double hiCy = height - margin - halfBot;
        double cy = System.Math.Max(loCy, columnTop + halfTop);
        if (hiCy >= loCy)
        {
            cy = System.Math.Min(cy, hiCy);
        }
        return (cx, cy);
    }

    /// <summary>The house-voice readout for the tracker's nearest contact: "movement — 40 du, closing".
    /// <paramref name="closing"/> reflects whether the nearest range is shrinking (client-computed from
    /// the last sweep). No movers → the honest, cold "no movement — for now".</summary>
    public static string Readout(double? nearestRange, bool closing) => nearestRange is { } r
        ? $"movement — {r:F0} du, {(closing ? "closing" : "drifting")}"
        : "no movement — for now";
}
