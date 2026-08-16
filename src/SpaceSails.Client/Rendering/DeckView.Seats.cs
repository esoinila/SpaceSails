using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// Subject: #784's seated captain and #792's chairs, taken and free (part of DeckView).
public sealed partial class DeckView
{
    // ── #784 · THE SEATED FIGURE ──────────────────────────────────────────────────────────────────────
    //
    // Three numbers, named, because a posture drawn out of literals is a posture nobody can tune. All of
    // them are fractions of DeckPlan.AvatarRadius or of the deck scale, so the seated captain grows and
    // shrinks with the standing one and the two can never drift apart the way #473's marks had.

    /// <summary>How much of the standing body a seated one takes on the floor. Less, because it is folded
    /// into a chair — and not so much less that the figure stops reading as a person.</summary>
    private const float SeatedBodyFactor = 0.74f;

    /// <summary>How far BEHIND the body the chair back is drawn, in deck units.</summary>
    private const float SeatedChairBackDu = 0.95f;

    /// <summary>How far in FRONT of the body the arms rest, in deck units — hands on the table, which is the
    /// half of the pose that says <i>at a table</i> rather than merely <i>not walking</i>.</summary>
    private const float SeatedArmsDu = 0.62f;

    /// <summary>Draw the captain sitting down: a chair back across the shoulders, a folded body, and a short
    /// bar of arms on the table in front. No heading spoke — a seated captain is going nowhere, and the
    /// spoke has meant "this way" since the mark was first drawn.</summary>
    private void DrawSeated(float ax, float ay, double headingRad, float scale)
    {
        float cos = (float)Math.Cos(headingRad), sin = (float)Math.Sin(headingRad);
        // Screen Y runs the other way to deck Y, which is why the sines are negated here exactly as they
        // are in the standing spoke above.
        (float fx, float fy) = (cos, -sin);          // forward, on screen
        (float px, float py) = (-fy, fx);            // and across it

        float back = SeatedChairBackDu * scale, arms = SeatedArmsDu * scale;
        float backHalf = (float)DeckPlan.AvatarRadius * scale;
        float armHalf = (float)DeckPlan.AvatarRadius * scale * 0.8f;

        // The chair back — a bar behind the shoulders, the one mark a standing captain never has.
        DrawSeg(
            (ax - (fx * back) - (px * backHalf), ay - (fy * back) - (py * backHalf)),
            (ax - (fx * back) + (px * backHalf), ay - (fy * back) + (py * backHalf)),
            AvatarColor, 2.5f);

        _renderer.DrawCircle(
            ax, ay, (float)DeckPlan.AvatarRadius * scale * SeatedBodyFactor, AvatarColor, AvatarColor);

        // …and the arms, on the table.
        DrawSeg(
            (ax + (fx * arms) - (px * armHalf), ay + (fy * arms) - (py * armHalf)),
            (ax + (fx * arms) + (px * armHalf), ay + (fy * arms) + (py * armHalf)),
            AvatarColor, 2f);
    }

    // ── #792 · THE SEATS, SEEN ────────────────────────────────────────────────────────────────────────
    //
    // Owner, playtest 2026-08-08: "people looking to sit down look at those like hungry wild beasts look at
    // their prey" — and the deck could not answer one of the three glances a hungry traveller takes.
    //
    // THREE GLYPHS AND NO WORDS, because #782's law is that the deck is read at phone size and this room is
    // already the densest in the game. Everything below is a fraction of the deck scale, exactly as the
    // seated captain's chair back is, so the furniture grows and shrinks with the people in it.
    //
    //   an EMPTY chair          a short bar on the table's ring, in the room's own furniture grey
    //   an OPEN chair           the same bar, heavier and in the console green — a seat at a table that
    //                           already has somebody at it, which is a different offer from an empty room
    //   a TAKEN seat            a filled body with a chair back behind its shoulders — #788's idiom for
    //                           the seated captain, at a stranger's size, so the two rhyme
    //   TALKING                 two short ticks over the top: there is a conversation here, and a lone
    //                           sitter has none. Handed down (DeckPlan.TableTop.Talking) and never read off
    //                           a plate by this file, which is the whole reason the fact exists in Core.

    /// <summary>How wide a chair is across its back — the bar an empty one is drawn as.</summary>
    private const float SeatChairDu = 0.62f;

    /// <summary>A sitter's body, at a top. Smaller than the captain's (<see cref="DeckPlan.AvatarRadius"/>)
    /// because the captain is the one figure on this deck that must never be mistaken for scenery.</summary>
    private const float SeatBodyDu = 0.30f;

    /// <summary>How far behind a sitter's shoulders their chair back is drawn.</summary>
    private const float SeatBackDu = 0.36f;

    /// <summary>The round seat of a tall chair at a counter.</summary>
    private const float StoolSeatDu = 0.40f;

    /// <summary>How far over a top the conversation ticks are struck, and how long they are.</summary>
    private const float TalkTickDu = 0.55f, TalkRiseDu = 0.85f;

    /// <summary>A chair nobody is in, at a table nobody is at — furniture, at the weight of the furniture
    /// line it stands on. Its own constant rather than <see cref="InnerLine"/> itself: a seat is a thing the
    /// room can be asked about, and a guard counting empty chairs must be able to tell one from the several
    /// hundred inner walls drawn in the same grey.</summary>
    private static readonly RgbaColor SeatEmpty = new(126, 142, 164, 210);

    /// <summary>…and a chair nobody is in at a table somebody IS at. The console green this deck has used
    /// for "you may walk up to this" since the first spot was drawn, so an invitation is not a new word.</summary>
    private static readonly RgbaColor SeatOpen = new(120, 220, 200, 235);

    /// <summary>Somebody is in it. Warm, and deliberately not the captain's amber — one figure on this deck
    /// is you.</summary>
    private static readonly RgbaColor SeatTaken = new(228, 196, 150, 240);

    /// <summary>#791 · How often a SERVING TICK is struck across a service rail, and how far it reaches
    /// either side of it. Five du is about two customers' shoulders — near enough that no stretch of the
    /// desk reads as unattended, far enough that eighty du of bar is a marked edge rather than a comb.</summary>
    private const float ServiceTickDu = 5.0f, ServiceTickHalfDu = 0.45f;

    /// <summary>#791 · …and the cap struck across each END of the run, twice a tick's reach, so the desk's
    /// service has a beginning and an end instead of fading into the room.</summary>
    private const float ServiceCapHalfDu = 0.95f;

    /// <summary>
    /// #791 · THE DESK'S SERVICE RAIL — a fixture that is a RUN, drawn as the length it is.
    ///
    /// <para>Owner, live at the B1 bar: <i>"we should probably have service on the whole length indicated
    /// somehow."</i> A rail down the run, a cap at each end, and a serving tick struck across it every
    /// <see cref="ServiceTickDu"/> — so the eye reads ORDER ANYWHERE ALONG HERE from anywhere beside the
    /// desk, in the console ink this deck has meant "you may walk up to this" in since the first spot was
    /// drawn. It is #795's own discipline one fixture along: a language of marks rather than of words.</para>
    ///
    /// <para><b>It decides nothing.</b> The two ends are the interaction point's own, which are Core's
    /// <c>Hall.Service</c>'s own — so what is lit is what answers, to the deck unit. A pen that worked out
    /// where a bar's service starts would be the drawn room and the pressed room disagreeing, which is this
    /// project's third named bug class and the reason this issue exists at all.</para>
    /// </summary>
    private void DrawServiceRun(
        in DeckPlan.ConsoleSpot fixture, RgbaColor ink, bool near,
        Func<double, double, (float X, float Y)> project)
    {
        (float ax, float ay) = fixture.End0;
        (float bx, float by) = fixture.End1;
        double dx = bx - ax, dy = by - ay;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        if (len < 1e-6)
        {
            return;
        }

        double ux = dx / len, uy = dy / len;
        double nx = -uy, ny = ux;                 // the rail's own perpendicular, in deck units
        float rail = near ? 2.5f : 1.4f;

        DrawSeg(project(ax, ay), project(bx, by), ink, rail);

        for (int end = 0; end < 2; end++)
        {
            double px = end == 0 ? ax : bx, py = end == 0 ? ay : by;
            DrawSeg(
                project(px - (nx * ServiceCapHalfDu), py - (ny * ServiceCapHalfDu)),
                project(px + (nx * ServiceCapHalfDu), py + (ny * ServiceCapHalfDu)), ink, rail);
        }

        int ticks = Math.Max(1, (int)(len / ServiceTickDu));
        for (int t = 1; t < ticks; t++)
        {
            double s = len * t / ticks;
            double px = ax + (ux * s), py = ay + (uy * s);
            DrawSeg(
                project(px - (nx * ServiceTickHalfDu), py - (ny * ServiceTickHalfDu)),
                project(px + (nx * ServiceTickHalfDu), py + (ny * ServiceTickHalfDu)),
                ink, near ? 2f : 1.1f);
        }
    }

    /// <summary>
    /// #792/#793 · ONE SEAT WITH NO BACK ON IT — a stool at the counter, or one end of a park bench.
    ///
    /// <para>Two glyphs, and they are the counter's own: a filled round seat where somebody is, a hollow one
    /// where nobody is, and the hollow one goes <see cref="SeatOpen"/> green when the seat NEXT to it is
    /// taken — the invitation ink #792 gave every other sittable place. There is deliberately no third
    /// glyph and no chair back: a bar stool does not have one and neither does a plank.</para>
    ///
    /// <para>It is one method because it is one question. Written twice it would be two answers to
    /// "is this seat free", drawn in two places, and the day the ink is tuned only one of them would move.</para>
    /// </summary>
    /// <param name="sx">Where it is, in pixels.</param>
    /// <param name="sy">The same.</param>
    /// <param name="taken">Somebody is on this seat.</param>
    /// <param name="neighbourIsThere">Somebody is on this run of seats — the counter, or this bench.</param>
    /// <param name="scale">Deck units to pixels.</param>
    private void DrawBacklessSeat(float sx, float sy, bool taken, bool neighbourIsThere, float scale)
    {
        if (taken)
        {
            _renderer.DrawCircle(sx, sy, StoolSeatDu * scale, SeatTaken, SeatTaken, 1.5f);
            return;
        }
        _renderer.DrawCircle(
            sx, sy, StoolSeatDu * scale, null,
            neighbourIsThere ? SeatOpen : SeatEmpty,
            neighbourIsThere ? 2.4f : 1.5f);
    }

    /// <summary>#793 · How far behind a held figure's shoulders the STOPPED bar is struck — the same
    /// distance a sitter's chair back stands at, because it is the same statement in the same ink: this one
    /// has settled.</summary>
    private const float HeldBarDu = SeatBackDu;

    /// <summary>Draw the chairs round one top, and whoever is in them. Nothing here decides anything: the
    /// seat count, the occupancy, the HEADCOUNT and the conversation all arrive on <paramref name="top"/>
    /// from the room that owns them (#788's one-reach lesson, applied to everybody who is not the
    /// captain).</summary>
    private void DrawSeatsRound(float cx, float cy, in DeckPlan.TableTop top, float scale)
    {
        if (top.Seats <= 0 || top.Seating.Count == 0)
        {
            return;
        }

        // How far out this top's chairs stand, read off the chairs themselves rather than off a radius kept
        // here — the tick that says PEOPLE ARE TALKING is struck clear of the seat backs, so it has to know
        // how wide the ring it is clearing actually is.
        float ring = 0f;

        // #820 · WHERE THE CHAIRS ARE AND WHO IS IN THEM — HANDED DOWN, one entry per seat, exactly as a
        // stool's spot and a bench end have been since #792 and #793.
        //
        // This loop used to work the ring out for itself, out of a radius and an angle it kept privately.
        // That was survivable while nobody sat in these chairs; #820 seats the CAPTAIN in one, and a place
        // a body is put must not be a place a renderer invented. So the positions and the party's
        // occupancy both arrive on the plan (#823's spread walk included — three on the same contract at a
        // six-top leave a gap between each of them) and nothing below decides anything.
        //
        // A top whose seat count and chair list disagree draws what it was HANDED rather than what it was
        // told to expect: a pen that filled in the difference would be back to deriving.
        foreach (DeckPlan.TableChair chair in top.Seating)
        {
            // Only the projection is this file's: screen Y runs the other way to deck Y.
            float ox = chair.X - top.X, oy = chair.Y - top.Y;
            float outward = MathF.Sqrt((ox * ox) + (oy * oy));
            ring = MathF.Max(ring, outward);
            float ux = outward < 1e-4f ? 1f : ox / outward, uy = outward < 1e-4f ? 0f : -oy / outward;
            (float px, float py) = (-uy, ux);   // along the chair's back, across the radius
            float half = SeatChairDu * scale / 2f;

            float sx = cx + (ox * scale), sy = cy - (oy * scale);

            if (chair.Taken)
            {
                float back = (outward + SeatBackDu) * scale;
                DrawSeg(
                    (cx + (ux * back) - (px * half), cy + (uy * back) - (py * half)),
                    (cx + (ux * back) + (px * half), cy + (uy * back) + (py * half)),
                    SeatTaken, 2f);
                _renderer.DrawCircle(sx, sy, SeatBodyDu * scale, SeatTaken, SeatTaken, 1f);
                continue;
            }

            bool invites = top.Occupied;
            DrawSeg(
                (sx - (px * half), sy - (py * half)), (sx + (px * half), sy + (py * half)),
                invites ? SeatOpen : SeatEmpty, invites ? 2.4f : 1.5f);
        }

        // …and whether there is anything to overhear. Struck over the top rather than on it, so it survives
        // the plate the console draws through the same coordinate.
        if (top.Talking)
        {
            float rise = cy - ((ring + TalkRiseDu) * scale);
            float tick = TalkTickDu * scale;
            DrawSeg((cx - (0.34f * scale), rise), (cx - (0.34f * scale) + (0.18f * scale), rise - tick),
                SeatTaken, 2f);
            DrawSeg((cx + (0.10f * scale), rise), (cx + (0.10f * scale) + (0.18f * scale), rise - tick),
                SeatTaken, 2f);
        }
    }
}
