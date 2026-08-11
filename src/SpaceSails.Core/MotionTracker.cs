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

    // ── #830 · A STANDING MAN IS NOT SILENCE ──────────────────────────────────────────────────────────
    //
    // Owner, evening playtest 2026-08-11, watching PATROL 2 in plain sight while the fan read "no movement
    // — for now":
    //
    //   "the patrol should be visible in the motion detector"
    //   "it is kind of our cheat code... if the guard is still then it would be a blurry blob"
    //   "a living thing would have to really try hard to be still and calm to avoid being shown"
    //   "still like the unsure nest blob we have in one space scene the guard should show there"
    //
    // The instrument shipped motion-only and that WAS honest arithmetic — a man at a beat stop has a
    // velocity of zero. It was the wrong law. A body at rest is not an absence: it breathes, it shifts its
    // weight, it puts a hand on a sling. What it stops doing is TRAVELLING, and travel is only the loudest
    // of the things a fan can hear.
    //
    // So the instrument grows a second KIND of return rather than a fake velocity. Faking one would have
    // been three lines and a lie — the sim would say "standing" while the fan drew a walker, which is the
    // sentence-vs-sim failure this project keeps paying for. A kind lets Core decide and the renderer draw:
    // a crisp dot for something walking, an unsure smear for something merely alive.

    /// <summary>#830 · WHAT A CONTACT DOES WHEN IT STOPS MOVING — the one fact the fan needs about it that
    /// its velocity cannot carry.
    ///
    /// <para>It is deliberately named for the DISCIPLINE and not for the biology. A set-down sentry is quiet
    /// because it is a machine; an Old One that has settled into its own deep is quiet because it is
    /// holding still on purpose (<see cref="ReeverIdle"/>'s shipped ruling, untouched here). The owner's law
    /// is that stillness is EARNED — <i>"a living thing would have to really try hard to be still and calm
    /// to avoid being shown"</i> — and a bored man on a rota has earned nothing.</para></summary>
    public enum AtRest
    {
        /// <summary>Still means still. A parked droid, a set-down sentry, a contact whose owner has declared
        /// that it is deliberately holding. Nothing comes back off the fan. FIRST so it is the zero value —
        /// a contact that never says which it is keeps the instrument's oldest behaviour.</summary>
        Quiet = 0,

        /// <summary>A body that is merely not walking. It returns the nest's own register at person scale:
        /// faint, smeared, unsure about range and bearing — you know SOMETHING is there and not exactly
        /// where.</summary>
        Restless = 1,
    }

    /// <summary>#830 · Which of the two returns the fan is holding. The distinction is Core's so the
    /// instrument can DRAW them differently; a renderer working out for itself which dots are certain is how
    /// two instruments come to disagree (#591's one-reach lesson, pointed at the fan's own output).</summary>
    public enum BlipKind
    {
        /// <summary>Something is travelling. A tight, certain dot at a bearing and a range.</summary>
        Crisp = 0,

        /// <summary>Something alive is not travelling. The unsure blob: broader than a body, fainter than a
        /// mover, and imprecise about where exactly it is standing.</summary>
        Blob = 1,
    }

    /// <summary>One thing the tracker can see: its bearing (radians, world frame — atan2(dy,dx) from the
    /// captain), its range (deck units) and WHICH KIND of return it is (#830). The kind defaults to
    /// <see cref="BlipKind.Crisp"/> so every caller written before the still-blob existed still reads as the
    /// motion return it always was.</summary>
    public readonly record struct Blip(double Bearing, double Range, BlipKind Kind = BlipKind.Crisp);

    /// <summary>An entity the tracker sweeps: where it is, how fast it is going, and — #830 — what it does
    /// when it is not going anywhere. The client feeds one per Reever (and per own droid, if any move — it
    /// is a motion tracker, not a Reever detector), and one per guard on a round, whose register is declared
    /// by <c>PatrolBeat.FanRegister</c> rather than typed at the call site.</summary>
    public readonly record struct Entity(
        double X, double Y, double Vx, double Vy, AtRest Rest = AtRest.Quiet);

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

    // ── #591 · THE FAN IS A SURFACE INSTRUMENT, AND UNDERGROUND IT SHOULD KNOW IT ────────────────────────
    //
    // Owner: "the motion tracker should be in underground visibility mode when we are deeeeeeeep under
    // surface".
    //
    // Everything about this device was written for standing on a moon under an open sky: its reach, its
    // "movement — 47 du, closing", its beacons. Hundreds of metres down inside poured walls it behaved
    // exactly the same, which is nonsense in the plainest way — a fan that hears as far through rock and
    // bulkhead as it does across open regolith.
    //
    // It also buys something the game wanted anyway. Depth already costs AIR (only the top of each shaft
    // band holds pressure) and TIME. Making the instrument worse as you descend gives depth a THIRD cost,
    // and it is the one the player can name: the deepest floors are frightening because you cannot hear
    // what is coming, and not one enemy had to be added to do it.

    /// <summary>#591 · The share of the surface fan the device keeps however deep it goes. Never zero: a
    /// dead instrument is not frightening, it is broken, and a captain who stops looking at the tracker has
    /// simply lost a gauge. This is the floor the curve leans on and never reaches.</summary>
    public const double UndergroundFloorFraction = 0.28;

    /// <summary>#591 · How many floors of descent halve what is left above the floor fraction. A curve, not
    /// a table, deliberately: <c>UndergroundComplex.DepthOf</c> is unbounded by design ("depth is free"),
    /// so anything with a bottom written into it would be wrong the first time a seed rolled deeper.</summary>
    public const double UndergroundHalvingFloors = 6.0;

    /// <summary>
    /// #591 · The fan's reach on floor <paramref name="level"/> (negative = underground, −1 is B1), given
    /// what it would reach on the surface.
    ///
    /// <para>Full reach on B1 and degrading from there. B1 is deliberately not degraded at all: it is the
    /// floor that still holds pressure, still has standing lights, and is the one place down there the
    /// building is still pretending to work — the instrument agreeing with that is the same lie the rest of
    /// the floor tells, and the lie is what makes the dark below it land.</para>
    ///
    /// <para>Strictly decreasing with depth, and it never reaches zero.</para>
    /// </summary>
    public static double UndergroundRange(double surfaceRange, int level)
    {
        if (level >= 0)
        {
            return surfaceRange;   // on the regolith, under the sky: the instrument it was built to be
        }

        double floorsBelowTheTop = -level - 1.0;   // B1 = 0, B7 = 6
        double left = System.Math.Pow(0.5, floorsBelowTheTop / UndergroundHalvingFloors);
        return surfaceRange * (UndergroundFloorFraction + ((1.0 - UndergroundFloorFraction) * left));
    }

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

    // ── #830 · THE BLOB'S OWN NUMBERS ─────────────────────────────────────────────────────────────────
    //
    // The nest aboard a derelict is the precedent the owner named ("still like the unsure nest blob we have
    // in one space scene"): a return that is always there, always in the same place, drawn far broader than
    // a body. This is that idea at PERSON scale, and each number below is the one the on-grid smudge idiom
    // already uses, so the two instruments say the same thing about uncertainty.

    /// <summary>#830 · The share of the fan's reach at which a LIVING contact holding still still comes
    /// back. Shorter than a walker's, because a chest rising is a smaller signal than a boot landing — but
    /// nowhere near short: on B2 it is some seventy deck units, more than twice the eye's own reach, so a
    /// guard at a stop is heard long before the marker could draw him. FLAGGED for the owner's tuning.</summary>
    public const double StillReachFraction = 0.6;

    /// <summary>#830 · How faint the blob paints beside a mover at the same range — the "unsure" in the
    /// owner's own word. Never zero: an instrument that hedges its way down to invisible has not told you
    /// anything.</summary>
    public const double BlobFaintness = 0.55;

    /// <summary>#830 · How wide a still return smears at point-blank range, in deck units. A body's own
    /// footprint plus the fan's doubt about it — the same order as the on-grid smudge's base radius, because
    /// they are the same claim drawn twice.</summary>
    public const double BlobBaseSpreadDu = 2.4;

    /// <summary>#830 · …and how much wider per deck unit of range. A crude fan is less sure about a far
    /// return, which is the smudge idiom's own law (<c>SmudgeRangeSpread</c>), stated once here so the fan
    /// and the deck plan cannot drift apart about how vague a thing is.</summary>
    public const double BlobSpreadPerDu = 0.10;

    /// <summary>#830 · How wide the unsure blob reads for a contact at <paramref name="range"/> — deck
    /// units of smear, growing with distance. The renderer draws a wash this wide instead of a dot, because
    /// what the captain has been handed is a REGION and drawing a point would claim a precision the
    /// instrument does not have (#488's ruling, one instrument along).</summary>
    public static double BlobSpreadDu(double range) =>
        BlobBaseSpreadDu + (System.Math.Max(0.0, range) * BlobSpreadPerDu);

    /// <summary>Read one entity relative to the captain at (<paramref name="originX"/>,
    /// <paramref name="originY"/>): a crisp <see cref="Blip"/> if it is moving; a
    /// <see cref="BlipKind.Blob"/> if it is a living thing merely holding still (#830); null only for
    /// something genuinely inert — a machine set down, or a contact whose owner has declared it is holding
    /// on purpose. The reach gate is <see cref="Sweep"/>'s, because the two kinds do not carry the same
    /// distance.</summary>
    public static Blip? Read(double originX, double originY, in Entity e)
    {
        double dx = e.X - originX, dy = e.Y - originY;
        if (IsMoving(e.Vx, e.Vy))
        {
            return new Blip(System.Math.Atan2(dy, dx), System.Math.Sqrt((dx * dx) + (dy * dy)));
        }
        if (e.Rest != AtRest.Restless)
        {
            return null;   // a machine, or something that has EARNED its quiet
        }
        return new Blip(
            System.Math.Atan2(dy, dx), System.Math.Sqrt((dx * dx) + (dy * dy)), BlipKind.Blob);
    }

    /// <summary>Sweep every entity and return a blip for each mover, nearest first. Still contacts are
    /// dropped by construction (motion only).
    ///
    /// <para>#591 · <paramref name="detectionRange"/> is what the fan can actually HEAR from here. It
    /// defaults to unbounded, which is the surface behaviour this device shipped with: a far contact still
    /// paints, pinned to the rim and faint (<see cref="BlipIntensity"/>), because that distant murmur is
    /// the whole point of the instrument. Underground the reach is real and the cut is real — a shortened
    /// fan that still painted everything on the rim would be a shortened fan in the numbers only.</para></summary>
    public static System.Collections.Generic.IReadOnlyList<Blip> Sweep(
        double originX, double originY, System.Collections.Generic.IEnumerable<Entity> entities,
        double detectionRange = double.PositiveInfinity)
    {
        System.ArgumentNullException.ThrowIfNull(entities);
        var blips = new System.Collections.Generic.List<Blip>();
        foreach (Entity e in entities)
        {
            if (Read(originX, originY, in e) is not { } b)
            {
                continue;
            }
            // #830 · The two kinds do not carry the same distance. A walker rides the whole fan; a body
            // merely holding still rides StillReachFraction of it, which is the honest shape of a smaller
            // signal — and is still, on every floor a round walks, far past anything the eye can do.
            double carries = b.Kind == BlipKind.Blob ? detectionRange * StillReachFraction : detectionRange;
            if (b.Range <= carries)
            {
                blips.Add(b);
            }
        }
        // Nearest first, and a certain return outranks an unsure one at the same range: what the readout
        // and the cadence want to name is the thing the instrument is SURE about.
        blips.Sort((a, b) => a.Range != b.Range
            ? a.Range.CompareTo(b.Range)
            : ((int)a.Kind).CompareTo((int)b.Kind));
        return blips;
    }

    /// <summary>#830 · The nearest MOVING return in a sweep, or null if the fan is holding none. This is the
    /// number the readout's "closing/drifting" is about — a blob is not going anywhere, so asking whether it
    /// is closing is asking a question with no answer.</summary>
    public static double? NearestMoving(System.Collections.Generic.IReadOnlyList<Blip>? returns)
    {
        if (returns is null)
        {
            return null;
        }
        foreach (Blip b in returns)   // Sweep sorts nearest-first, so the first crisp one is the nearest
        {
            if (b.Kind == BlipKind.Crisp)
            {
                return b.Range;
            }
        }
        return null;
    }

    /// <summary>#830 · The nearest return of EITHER kind. The cadence reads this: a living thing standing
    /// three metres away in the dark is not a resting instrument, whatever its velocity says.</summary>
    public static double? NearestReturn(System.Collections.Generic.IReadOnlyList<Blip>? returns) =>
        returns is { Count: > 0 } ? returns[0].Range : null;

    /// <summary>#830 · The ping cadence for a whole sweep — the nearest return of either kind. It reads the
    /// SAME LIST the fan draws, which is the whole of this issue's fourth law: an instrument whose pulse and
    /// whose dots were counted separately is two instruments.</summary>
    public static Cadence CadenceOf(System.Collections.Generic.IReadOnlyList<Blip>? returns) =>
        CadenceFor(NearestReturn(returns));

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

    /// <summary>#830 · What the fan says about a body it can hear but cannot hear WALKING. It hedges on
    /// purpose ("about", "not walking") because that is exactly what the instrument has: a region, a
    /// suspicion, and the certainty that whatever is in it is not a machine.</summary>
    public static string StillReadout(double range) =>
        $"something alive — about {range:F0} du, not walking";

    /// <summary>
    /// #830 law 4 · THE SENTENCE AND THE SWEEP READ THE SAME LIST.
    ///
    /// <para>Owner: <i>"'no movement — for now' may only print when the fan holds NO returns of either
    /// kind."</i> It is the sentence-vs-sim law turned on the instrument itself — the line that sent a
    /// captain walking into a corridor with a guard standing in it was not wrong about the arithmetic, it
    /// was answering a narrower question than the one the fan was drawing.</para>
    ///
    /// <para>A mover names itself first (it is the more urgent fact and the only one "closing" means
    /// anything about); failing that a still body is named as a still body; and only an EMPTY sweep is
    /// allowed the cold line.</para>
    /// </summary>
    public static string ReadoutOf(System.Collections.Generic.IReadOnlyList<Blip>? returns, bool closing) =>
        NearestMoving(returns) is { } moving ? Readout(moving, closing)
        : NearestReturn(returns) is { } still ? StillReadout(still)
        : Readout(null, closing);
}
