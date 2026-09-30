using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #618 · <b>THE PASS, AND THE LIGHT.</b> The two things that happen to him when the captain changes floor:
/// going DOWN from his floor while he does not keep the way is the pass (the book's line, once per ground, by
/// whatever route got the captain there); coming UP under the lid with him on your heels puts him at the mouth
/// of the tube, in the light he was told to stay out of, where the tide decides
/// (<see cref="GateGuard.TheTideAnswers"/>).
///
/// <para><b>Told from the surface, never drawn as a death.</b> He is drawn standing at the mouth; if the tide
/// has him he is simply not drawn on the next frame, and the one line says the rest. If the tide is down he
/// turns back, and the next time the captain stands on his floor he is at his door again.</para>
/// </summary>
public partial class Map
{
    /// <summary>
    /// #618 · Called from <see cref="RideTheLiftTo"/> the moment the floor changes, whatever carried the captain
    /// — the cage, the goods car, the stair or a dev cheat.
    /// </summary>
    private void TheManAtTheDoorSeesYouGo(SurfaceExcursion ex, int fromLevel, int level)
    {
        if (level < 0)
        {
            HeGoesBackDownUntold(ex);
        }

        if (ex.Gate is not { } man || fromLevel != man.Floor)
        {
            return;
        }

        if (level < fromLevel)
        {
            ThePassIsFiledOnce(man);
            return;
        }

        if (level == 0)
        {
            HeFollowsYouIntoTheLight(ex, man);
        }
    }

    /// <summary>
    /// The first pass by any route, once per ground — badge, word, the tide, the empty chair or the round. The
    /// panel has already made sure he was not keeping the way (<see cref="PastTheManAtTheDoor"/>), so a car
    /// that went down from his floor went past him.
    /// </summary>
    private void ThePassIsFiledOnce(ManAtTheDoor man)
    {
        if (_roomsTurnedOver.Add(GateGuard.PastTag(man.Ground)))
        {
            FileNote(GateGuard.BookLine, GateGuard.BookGlyph);
            RequestVaultSave();
        }
    }

    /// <summary>He was on your heels when you came out under the lid. He comes off the floor below and stands at
    /// the mouth of the tube — the shed's own doorstep, where a landing sets a captain down — and his clock at
    /// the mouth starts.</summary>
    private static void HeFollowsYouIntoTheLight(SurfaceExcursion ex, ManAtTheDoor man)
    {
        if (!man.Following || man.Taken)
        {
            return;
        }

        man.Following = false;
        man.AtHisElbow = false;
        man.AtTheMouthFor = 0;
        man.StandingAt = null;
        if (TheManAfoot(ex.Walkers) is { } him)
        {
            ex.Walkers.Remove(him);
        }
    }

    /// <summary>
    /// #618 · <b>AT THE MOUTH OF THE TUBE.</b> One frame of him standing in the light, and the tide's answer
    /// (<see cref="GateGuard.TheTideAnswers"/>): a tide Old One on the field takes him; the mouth's time run out
    /// on a quiet field sends him back down. Either way the line is said once, here, on the surface, and he is
    /// off the regolith on the same frame. If the captain goes back down before the tide has answered, he goes
    /// back down too, and nothing is said — there was nobody up here to see which.
    /// </summary>
    private void StepHimAtTheMouth(SurfaceExcursion ex, ManAtTheDoor man, double dt)
    {
        if (man.AtTheMouthFor < 0)
        {
            return;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        if (TheManAfoot(ex.Walkers) is null)
        {
            MoonSurface.LiftHeadBox shed = MoonSurface.LiftHead(
                ex.Stop.Body.Id, ex.Site.LayoutSalt, MoonSurface.ExpeditionField());
            DeckReachability.Point mouth = ClearSpotFor(new DeckReachability.Point(shed.StepX, shed.StepY), walls)
                ?? new DeckReachability.Point(shed.StepX, shed.StepY);
            if (OnFoot(GateGuard.Plate, new NpcWalk.Bound("", mouth.X, mouth.Y), mouth, walls) is { } stand)
            {
                ex.Walkers.Insert(0, new Walker { Walk = stand, Table = -1, For = Errand.GateAtTheMouth });
                StateHasChanged();
            }
        }

        if (TheManAfoot(ex.Walkers) is { } him)
        {
            him.Walk.LookTowards(_avatarX, _avatarY);
        }

        man.AtTheMouthFor += dt;
        switch (GateGuard.TheTideAnswers(man.AtTheMouthFor, _reevers.Exists(r => r.Tide)))
        {
            case GateGuard.AtTheMouth.Taken:
                man.Taken = true;
                HeIsOffTheRegolith(ex, man);
                ShowPulseMessage(GateGuard.TideTakesHimLine, PulseRank.Beat);
                break;

            case GateGuard.AtTheMouth.GoesBack:
                HeIsOffTheRegolith(ex, man);
                ShowPulseMessage(GateGuard.TideDownLine, PulseRank.Beat);
                break;
        }
    }

    /// <summary>He leaves the mouth, one way or the other: off the band, and back to his door for when the
    /// captain is next on his floor (a taken man is never put on a floor again).</summary>
    private void HeIsOffTheRegolith(SurfaceExcursion ex, ManAtTheDoor man)
    {
        man.AtTheMouthFor = -1;
        man.StandingAt = null;
        man.Leg = RoundLeg.AtTheDoor;
        man.RoundClock = 0;
        if (TheManAfoot(ex.Walkers) is { } him)
        {
            ex.Walkers.Remove(him);
        }

        StateHasChanged();
    }

    /// <summary>The captain went back down while he was standing at the mouth: he goes back down too, and it is
    /// not told. Called from the arrival on any floor below the lid.</summary>
    private static void HeGoesBackDownUntold(SurfaceExcursion ex)
    {
        if (ex.Gate is { AtTheMouthFor: >= 0 } man)
        {
            man.AtTheMouthFor = -1;
            man.StandingAt = null;
            man.Leg = RoundLeg.AtTheDoor;
            man.RoundClock = 0;
        }
    }
}
