using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #326 · <b>THE RETREAT-LINE DOCTRINE — nothing comes between the captain and the shuttle.</b>
///
/// <para>Owner, live 2026-07-18: <i>"I think the securibots most important job is not let anything come
/// between me and the shuttle :-D"</i>, and, making the second half of it concrete the same day: <i>"I think
/// the bot at surface should act a bit like a body guard. Protect the path to the ship at about half way so
/// there is always a way to retreat back to safety (until bullets run out) :-D"</i></para>
///
/// <para>Two rules come out of those two sentences, and this file is both of them and nothing else. The
/// client owns the live list, the frame and the walking; <see cref="SentryBot"/> owns the magazine, the arc
/// and the husk. What was missing was the only thing either of them could not answer on its own: <b>which
/// Old One matters</b>, and <b>where a bodyguard stands</b>.</para>
///
/// <h3>1 · Target priority is the LINE, not the nearest</h3>
/// <para>A deployed bot used to shoot whatever shambled closest to ITSELF. That is the doctrine of a turret,
/// and it loses the game the pack is actually playing: the many-law's flanking bias (<c>EncircleBias</c>)
/// aims the pack a little toward the way out, so the ones that will corner a captain are exactly the ones
/// that are NOT coming straight at the gun. Under a nearest-first rule the bot spends its 99 digits on the
/// shambler in front of it while the two cutting the corridor walk past unengaged, and the cornering
/// loss-condition (#313) has no purchasable counter at all.</para>
/// <para>So: <b>anything standing in the corridor outranks anything that is not</b>, and only then does
/// nearest decide. Ties fall back to nearest exactly as before, and a world with nothing in the corridor
/// behaves to the last bit the way it did before this file existed.</para>
///
/// <h3>2 · The escort stance — hold the midpoint</h3>
/// <para><see cref="HoldingPoint"/> is the bodyguard's post: the middle of the captain→home line, recomputed
/// every frame as the captain moves, and nudged along the line to the nearest spot a body actually fits when
/// the middle of it is inside a wall. The client walks the bot there with #324's own bump-and-slide, so an
/// escort obeys the maze the same way every other body on the ground does.</para>
///
/// <para><b>Why halfway.</b> From the middle, the bot's arc covers the largest slice of the retreat corridor
/// reachable from EITHER direction — the captain can always fall back THROUGH the guarded zone rather than
/// to a fixed post they may already be cut off from. Any other fraction is a post that is good against one
/// half of the field and useless against the other.</para>
///
/// <para><b>Deliberately no formation AI.</b> The issue's own clause 4: <i>"Crude implementation is correct:
/// line-segment proximity for 'threatens the corridor', the existing bump-and-slide movement for the escort
/// repositioning — no formation AI."</i> Two bots holding the line both walk to the same midpoint and shove
/// past each other; that is a layered corridor by accident, which is all it was ever asked to be.</para>
///
/// <para>Pure, deterministic, no clocks, no randomness — so the priority, the post and the wall case all pin
/// in a Core test rather than being watched for on a screen.</para>
/// </summary>
public static class SentryDoctrine
{
    // ── THE TWO WORDS THE VERB SAYS ───────────────────────────────────────────────────────────────────

    /// <summary>#326 · The first of the two stances, in the owner's own words (<i>"[T] deploy here / hold my
    /// line home"</i>). Set it down where you stand and it holds that arc — the behaviour #314 shipped, and
    /// still the right answer when the thing worth guarding is a hole in the ground rather than a walk.</summary>
    public const string DeployHereLabel = "deploy here";

    /// <summary>#326 · The second stance, and the one the issue is titled after. The bot stops being a post
    /// and becomes a bodyguard: it walks to <see cref="HoldingPoint"/> and keeps walking there for as long as
    /// the captain keeps moving, which is until the counter reads 00.</summary>
    public const string HoldMyLineHomeLabel = "hold my line home";

    // ── THE CORRIDOR ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #326 · <b>HOW WIDE THE RETREAT CORRIDOR IS — half the bot's own engagement arc, 11 du.</b>
    ///
    /// <para>It has to be derived from <see cref="SentryBot.RangeDeckUnits"/> because the corridor is a claim
    /// about what THIS machine can do about a threat, and the arc is the only measure of that the bot owns.
    /// And it has to be strictly INSIDE the arc, which is the whole reason it is a half rather than the arc
    /// itself: a half-width equal to the range would mark every target the bot can shoot as a corridor
    /// threat, the priority rule would select everything, and the guard behind it would be this repository's
    /// fifth named bug class — a green test whose world cannot tell pass from fail.</para>
    ///
    /// <para><b>Half, and not some other fraction, for a reason with geometry in it.</b> A bot posted ON the
    /// line covers a disc of radius R. Widen the corridor to <c>w</c> and the stretch of the LINE it still
    /// covers at that width shrinks to <c>2·√(R² − w²)</c> — width is bought with length, and a bodyguard who
    /// buys too much width ends up covering a wide slab of ground and none of the road. At <c>w = R/2</c> the
    /// bot keeps <c>√3/2 ≈ 87 %</c> of its reach pointed down the corridor while watching a lane 22 du across:
    /// the last width at which the thing is still guarding a ROAD. Push it to <c>w = R</c> and the covered
    /// length is zero, which is the arithmetic saying the same thing the paragraph above says.</para>
    ///
    /// <para>22 du across is also a lane a captain can be cut off inside — the Old Ones' own bodies are 1.4 du
    /// wide and a bad roll fields six of them — so the width is not a formality either.</para>
    /// </summary>
    public const double CorridorHalfWidthDu = SentryBot.RangeDeckUnits / 2.0;

    /// <summary>#326 · The captain, and the way home. Both ends are live: the captain moves every frame, and
    /// the home end is whichever door this ground's boots came out of (the tube mouth on a moon, her airlock
    /// aboard a wreck) — the client supplies it, because it is the one fact about the ground that Core has no
    /// business guessing.</summary>
    public readonly record struct RetreatLine(double CaptainX, double CaptainY, double HomeX, double HomeY);

    /// <summary>#326 · Is this thing standing in the corridor? Line-segment proximity and nothing cleverer —
    /// the issue's own clause 4. A body beyond either END of the line is measured to the end (that is what
    /// <see cref="SurfaceCollision.DistanceToSegment"/> does), which is right: something lurking past the tube
    /// mouth is not on the road home, it is at the door.</summary>
    public static bool ThreatensTheLine(in RetreatLine line, double x, double y) =>
        SurfaceCollision.DistanceToSegment(
            x, y, line.CaptainX, line.CaptainY, line.HomeX, line.HomeY) <= CorridorHalfWidthDu;

    // ── WHICH ONE THE BOT SHOOTS ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #326 · <b>PICK THE TARGET.</b> Among everything this bot can actually engage — alive, inside
    /// <see cref="SentryBot.RangeDeckUnits"/>, and with no stone in the way (#437) — anything standing in the
    /// retreat corridor outranks anything that is not, and nearest breaks it from there. Returns the index
    /// into <paramref name="reevers"/>, or -1 when there is nothing it can see.
    ///
    /// <para><paramref name="line"/> null is the old world exactly: no corridor, so nothing threatens it, so
    /// every candidate is equal on the first term and the pick is the nearest visible one — bit for bit the
    /// selection <see cref="SentryBot.Step"/> made before this file existed.</para>
    ///
    /// <para><paramref name="alive"/> is the volley's own running board: a target downed earlier in the same
    /// tick is off it, so no shot is wasted on a corpse. Null means everything listed is standing.</para>
    /// </summary>
    public static int Pick(
        in SentryBot.Deployed bot,
        IReadOnlyList<SentryBot.Target> reevers,
        IReadOnlyList<bool>? alive,
        RetreatLine? line,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        ArgumentNullException.ThrowIfNull(reevers);

        int best = -1;
        double bestSq = double.MaxValue;
        bool bestIsInTheCorridor = false;

        for (int j = 0; j < reevers.Count; j++)
        {
            if (alive is not null && j < alive.Count && !alive[j])
            {
                continue;
            }

            double dx = reevers[j].X - bot.X, dy = reevers[j].Y - bot.Y;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 > SentryBot.RangeDeckUnits * SentryBot.RangeDeckUnits)
            {
                continue;
            }
            if (!SurfaceCollision.HasLineOfSight(bot.X, bot.Y, reevers[j].X, reevers[j].Y, walls))
            {
                continue;   // #437: a gun that shoots through stone undoes the maze law
            }

            bool inTheCorridor = line is { } l && ThreatensTheLine(l, reevers[j].X, reevers[j].Y);

            // The doctrine, in two comparisons. A corridor threat beats a non-threat at ANY range — that is
            // the whole ticket — and within one class, nearest wins with ties broken by index (strict <, so
            // the first index stands).
            bool better = inTheCorridor != bestIsInTheCorridor
                ? inTheCorridor
                : d2 < bestSq;
            if (better)
            {
                best = j;
                bestSq = d2;
                bestIsInTheCorridor = inTheCorridor;
            }
        }

        return best;
    }

    // ── WHERE A BODYGUARD STANDS ──────────────────────────────────────────────────────────────────────

    /// <summary>#326 · How finely the line is sounded when the midpoint is inside a wall. The post is walked
    /// out from the middle in steps of this fraction of the line's length, one hand then the other, so the
    /// answer is the NEAREST legal point along the line rather than the first convenient one.</summary>
    private const double PostProbeFraction = 0.01;

    /// <summary>#326 · Halfway. The fraction the owner named — <i>"Protect the path to the ship at about half
    /// way"</i> — written down once so the post and every guard measure the same middle.</summary>
    public const double PostFraction = 0.5;

    /// <summary>
    /// #326 · <b>THE BODYGUARD'S POST</b> — the midpoint of the captain→home line, or, when a body of
    /// <paramref name="radius"/> will not fit there, the nearest point ALONG THE LINE that it will.
    ///
    /// <para>The search walks outward from the middle in both directions together and takes the first free
    /// offset, so what comes back is genuinely the nearest legal point and not merely a legal one. Exact ties
    /// go to the CAPTAIN'S side: a bodyguard forced off its mark steps back toward the body it is guarding,
    /// which is the only side of that coin worth taking.</para>
    ///
    /// <para>When the whole line is stone — a captain sealed off from the tube entirely — the midpoint comes
    /// back unchanged and the bot walks at it and grinds on the wall. That is honest: there is no post,
    /// because there is no corridor, and inventing a route round the outside would be the formation AI clause
    /// 4 rules out.</para>
    /// </summary>
    public static (double X, double Y) HoldingPoint(
        in RetreatLine line, double radius, IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        double cx = line.CaptainX, cy = line.CaptainY;
        double dx = line.HomeX - cx, dy = line.HomeY - cy;
        (double X, double Y) At(double t) => (cx + (dx * t), cy + (dy * t));

        (double X, double Y) middle = At(PostFraction);
        if (!SurfaceCollision.Blocked(middle.X, middle.Y, radius, walls))
        {
            return middle;
        }

        for (double step = PostProbeFraction; step <= PostFraction; step += PostProbeFraction)
        {
            (double X, double Y) toward = At(PostFraction - step);      // the captain's side, first
            if (!SurfaceCollision.Blocked(toward.X, toward.Y, radius, walls))
            {
                return toward;
            }
            (double X, double Y) away = At(PostFraction + step);        // …then the door's
            if (!SurfaceCollision.Blocked(away.X, away.Y, radius, walls))
            {
                return away;
            }
        }

        return middle;
    }

    /// <summary>
    /// #326 · One frame of an escort's walk: <paramref name="stepDu"/> of movement from where it stands
    /// toward <paramref name="post"/>, through #324's bump-and-slide on the same segments every other body
    /// collides with.
    ///
    /// <para><see cref="SurfaceCollision.Gait.Person"/>, and the reason is written in that enum: a boarding
    /// trooper is <i>"anything else on somebody's payroll that has to look like it belongs in a corridor"</i>.
    /// The stagger is the Old Ones' and it is theirs on the owner's own ruling — <i>"Lets not help reevers
    /// move in any easier"</i> — which cuts exactly the other way for the machine the captain paid for.</para>
    ///
    /// <para>Overshoot is clamped: a bot within one step of its post arrives ON it rather than oscillating
    /// across it, which matters because the post moves every frame the captain does.</para>
    /// </summary>
    public static (double X, double Y) StepToward(
        double x, double y, (double X, double Y) post, double stepDu, double radius,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        double dx = post.X - x, dy = post.Y - y;
        double gap = Math.Sqrt((dx * dx) + (dy * dy));
        if (gap <= 1e-9 || stepDu <= 0)
        {
            return (x, y);
        }
        double take = Math.Min(stepDu, gap);
        return SurfaceCollision.Slide(
            x, y, dx / gap * take, dy / gap * take, radius, walls, SurfaceCollision.Gait.Person);
    }
}
