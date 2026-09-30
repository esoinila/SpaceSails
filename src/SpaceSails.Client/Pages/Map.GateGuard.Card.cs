using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #618 · <b>THE MAN AT THE DOOR — HIS CARD.</b> Reaching him while he keeps the door raises the card the
/// ground's other finds are told on (<c>_viewObject</c>, through #1052's arbiter, so it queues behind a card
/// that is already up and never stacks). Its moves are drawn in the card's own subtree, the #746 stop's way, and
/// pressing one REPLACES the row with his answer — one card, never two.
///
/// <para><b>No flag says the card is his.</b> It is his when the card up is titled
/// <see cref="GateGuard.CardTitle"/> on the floor he keeps and he still keeps it; the moment anything else is
/// true (the card is closed, he has been passed, he has come off the wall) the moves are simply not there.
/// Closing the card is walking off, and nothing is said about it.</para>
/// </summary>
public partial class Map
{
    /// <summary>
    /// The captain has reached him. The card goes up once per approach — walking off past
    /// <see cref="GateGuard.LeftHimDu"/> and coming back is a second approach — and the approach line is on the
    /// first card of the excursion only; after that the card is his title and his moves, and nothing more.
    /// </summary>
    private void TheCaptainReachesHim(SurfaceExcursion ex, ManAtTheDoor man, Walker him)
    {
        if (man.AtHisElbow)
        {
            if (!Within(_avatarX, _avatarY, him.Walk.X, him.Walk.Y, GateGuard.LeftHimDu))
            {
                man.AtHisElbow = false;
            }

            return;
        }

        if (!Within(_avatarX, _avatarY, him.Walk.X, him.Walk.Y, GateGuard.ReachDu))
        {
            return;
        }

        man.AtHisElbow = true;
        RaiseAScrimCard(
            () => HisCardGoesUp(man),
            () => _surface is { } still && ReferenceEquals(still, ex) && TheManKeepsTheWayDown(still));
    }

    /// <summary>The card itself, once the glass is his. The approach line is spent here and nowhere else, so a
    /// card that waited behind another and was then no longer wanted has told nothing.</summary>
    private void HisCardGoesUp(ManAtTheDoor man)
    {
        string caption = man.ApproachTold ? "" : GateGuard.ApproachLine;
        man.ApproachTold = true;
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY, GateGuard.CardTitle, "", caption);
        RendererInterop.PlayCue("reveal");
        StateHasChanged();
    }

    /// <summary>Is his card up, unanswered, with him still in front of the door? What the move row is drawn on.</summary>
    private bool TheManAtTheDoorWaits =>
        _viewObject is { Label: GateGuard.CardTitle, Outcome: null }
        && _surface is { } ex && TheManKeepsTheWayDown(ex) && ex.Gate is { Following: false };

    /// <summary>The moves on his card, and only the ones that exist (<see cref="GateGuard.MovesOnTheCard"/>).</summary>
    private IReadOnlyList<Encounter.Move> TheManAtTheDoorsMoves() =>
        _surface is { Gate: { } man } ex
            ? GateGuard.MovesOnTheCard(
                GateGuard.ThePassThatPasses(ex.Stop.Body.Id, man.Floor, ex.CanteenWatch, _satchel) is not null,
                _roomsTurnedOver.Contains(GateGuard.FaceTag(man.Ground)))
            : [];

    /// <summary>
    /// #618 · <b>THE CAPTAIN ANSWERS HIM.</b> One door for every button. Each answer is his line, verbatim, put
    /// on the card in place of the row it was pressed from; none of them is filed, and none of them costs heat,
    /// a crossing or a witness — the badge and the word leave no mark. The book's line is the pass's, not the
    /// move's (<c>Map.GateGuard.Mouth.cs</c>).
    /// </summary>
    private void AnswerTheManAtTheDoor(string moveId)
    {
        if (!TheManAtTheDoorWaits || _surface is not { Gate: { } man } ex || _viewObject is not { } card)
        {
            return;
        }

        bool onOffer = false;
        foreach (Encounter.Move m in TheManAtTheDoorsMoves())
        {
            onOffer |= string.Equals(m.Id, moveId, System.StringComparison.Ordinal);
        }

        if (!onOffer)
        {
            return;
        }

        string said;
        switch (moveId)
        {
            case GateGuard.ShowMove:
                man.Passed = true;
                said = GateGuard.BadgeLine;
                break;

            case GateGuard.TalkMove when man.Visit.TalkWorks:
                man.Passed = true;
                said = GateGuard.TalkWorkedLine;
                break;

            case GateGuard.TalkMove:
                _roomsTurnedOver.Add(GateGuard.FaceTag(man.Ground));
                RequestVaultSave();
                said = GateGuard.TalkFailedLine;
                break;

            default:
                man.Following = true;
                said = GateGuard.NoiseLine;
                break;
        }

        _viewObject = card with { Outcome = said };
        StateHasChanged();
    }
}
