using System;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #326 · <b>THE ESCORT STANCE — the bot walks the captain's line home.</b>
///
/// <para>Owner, live 2026-07-18: <i>"I think the bot at surface should act a bit like a body guard. Protect
/// the path to the ship at about half way so there is always a way to retreat back to safety (until bullets
/// run out) :-D"</i></para>
///
/// <para>A sentry has been a POST since #314 — you set it down, it holds that arc, and it is still standing
/// there when the fight has moved forty units up-field and you are the wrong side of it. That is the right
/// object for guarding a hole in the ground and the wrong one for guarding a walk, and the walk is what the
/// whole excursion loop is made of. So the verb now offers two stances at the press, in the owner's own two
/// phrases (<see cref="SentryDoctrine.DeployHereLabel"/> / <see cref="SentryDoctrine.HoldMyLineHomeLabel"/>),
/// and this file is the second one.</para>
///
/// <h3>What is here, and what is emphatically not</h3>
/// <para><b>Not the geometry.</b> Where a bodyguard stands is <see cref="SentryDoctrine.HoldingPoint"/> and
/// how it gets there a step at a time is <see cref="SentryDoctrine.StepToward"/> — both pure, both pinned in
/// Core. What is left for a page is the three facts only a running world has: <b>where the way home is on
/// THIS ground</b>, <b>which bots were set down holding the line</b>, and the frame.</para>
///
/// <para><b>No formation AI</b>, on the issue's own clause 4. Two bots in the escort stance both walk at the
/// same midpoint and shove past one another on #324's collision; that is a layered corridor by accident,
/// which is all it was ever asked to be.</para>
///
/// <para><b>Same magazine law, no exception.</b> Nothing here touches a drum. An escort fires through
/// <c>StepSentries</c> exactly as a planted bot does, drains the same round per pull, and freezes at 00 — and
/// at 00 it stops walking too, which is the whole promise expiring in the one place a captain is already
/// watching. A dry escort is then a bot standing still on the retreat line with a frozen counter, which is
/// the mark #316 already draws and the write-off <c>SentryBot.AbandonLedgerLine</c> already prints. Nothing
/// new is minted for it: the most eloquent husk in the game is one the game already had.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>
    /// #326 · <b>THE WAY HOME on this ground</b> — the far end of the retreat line.
    ///
    /// <para>Aboard a derelict it is her own airlock; on the regolith it is the square just outside the
    /// tube's surface door, <c>MoonSurface.SpawnX/SpawnY</c> — where the boots actually land, which is the
    /// same point the suit's tank and the tracker price the route to (<c>SurfaceTiles.TubeMouth</c>'s own
    /// note, and #1094's reconciliation of the two ends of one tube). The corridor a captain walks is the one
    /// their instruments already measure, or the bodyguard is guarding a different road from the one on the
    /// gauge.</para>
    /// </summary>
    private (double X, double Y) TheWayHome => OnWreck
        ? (WreckLayout.SpawnX, WreckLayout.SpawnY)
        : (MoonSurface.SpawnX, MoonSurface.SpawnY);

    /// <summary>
    /// #326 · <b>THE RETREAT LINE, or null where there is not one.</b>
    ///
    /// <para>Null underground, and that is a rule rather than a shortcut: <see cref="TheWayHome"/> names a
    /// door on the SURFACE map, and a captain eleven floors down the Hive is not walking to it — the lift is
    /// their way out and it is somewhere else entirely. Handing those coordinates to a bot standing in a
    /// corridor of B11 would post it at the midpoint of a line drawn across two different maps, which is this
    /// repository's second named bug class (a constant governing the wrong thing) with the serial numbers
    /// filed off. With no line, <c>SentryDoctrine.Pick</c> falls back to nearest and an escort holds where it
    /// stands — the #314 post, which is the honest answer down there.</para>
    /// </summary>
    private SentryDoctrine.RetreatLine? TheRetreatLine
    {
        get
        {
            if (_surface is not { } ex || (!OnWreck && ex.Floor < 0))
            {
                return null;
            }
            (double X, double Y) home = TheWayHome;
            return new SentryDoctrine.RetreatLine(_avatarX, _avatarY, home.X, home.Y);
        }
    }

    /// <summary>
    /// #326 · One frame of every bodyguard's walk.
    ///
    /// <para>Called before <c>StepSentries</c> so a bot shoots from where it has just arrived rather than
    /// from where it stood last frame — the alternative is the sim doing one thing while the drawn zap line
    /// says another, which this project has paid for three times in one afternoon.</para>
    ///
    /// <para><b>It walks at the captain's own pace</b> (<see cref="AvatarSpeed"/>, reused rather than
    /// re-typed). The post is the MIDDLE of a line with the captain on one end, so it moves at half his
    /// speed; a bodyguard given the same legs therefore always closes on its station, and one given less
    /// would fall further behind for as long as the captain kept walking, which is exactly the run in which
    /// the guarantee matters. No new number: an escort that cannot keep up with the man it is escorting is
    /// not an escort.</para>
    ///
    /// <para>Dry bots are skipped, so a spent escort stops where it stands: <b>the countdown IS the retreat
    /// timer</b>, and when it reads 00 the corridor is naked and the machine that was holding it is a mark on
    /// the ground.</para>
    /// </summary>
    private void StepEscorts(double dtRealSeconds)
    {
        if (_surface is not { } ex || ex.Bots.Count == 0 || TheRetreatLine is not { } line)
        {
            return;
        }

        double step = AvatarSpeed * Math.Min(dtRealSeconds, MaxSurfaceStepSeconds);
        if (step <= 0)
        {
            return;
        }

        // #324's own segments — the list the captain collides with and the Old Ones shamble against. A
        // bodyguard that walked through the maze the captain has to go round would be guarding a corridor
        // that does not exist.
        var walls = _deckPlan.CollisionField;

        foreach (SurfaceBot bot in ex.Bots)
        {
            if (!bot.Deployed || !bot.HoldsTheLine || SentryBot.IsDry(bot.Rounds))
            {
                continue;
            }

            (double X, double Y) post = SentryDoctrine.HoldingPoint(line, DeckPlan.AvatarRadius, walls);
            (bot.X, bot.Y) = SentryDoctrine.StepToward(
                bot.X, bot.Y, post, step, DeckPlan.AvatarRadius, walls);
        }
    }
}
