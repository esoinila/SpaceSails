using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: #238 — THE MISSION EVENT, AT THE SMALLER SIZE. The celebration family's second and third members
// and the ONE SEAM they both go through.
//
// Owner, mid-car-hunt: "Is there a big pop-up when we find the car in the scans? It is a kind of mission
// event like when we get paid?" — there wasn't. The reveal was a marker and a checklist tick, and the
// proximity pickup was worse: he grabbed the roadster's wallet mid-coast and HAD TO ASK whether the loot was
// aboard, because the only thing that changed on the screen was the Captain chip's next line. His own rule
// of thumb out of that session, and the whole of why this file exists:
//
//     "if the player can ask 'did that just happen?', the game owed them a moment."
//
// WHAT A MOMENT IS, and it is #185's grammar at a smaller size: a card the world stops behind, the warp
// yanked to 1× so the beat cannot slip past at 10,000×, a ledger receipt that outlives the card, the bird,
// and — for a reveal — a `show me` press that puts the new marker under the camera.
//
// ONE SEAM, not a second feature per arc. Every caller composes a pure Core MissionMoment and hands it to
// TheMomentTheyEarnedIt; nothing downstream asks WHICH arc raised it. That is what makes the next hidden
// target (a secret station, a #223 rumour cache when discovery scanning arrives) two lines at a call site
// rather than a fourth member of the family with its own opinions about warp.
public partial class Map
{
    // One card at a time, extras queued — the celebration's own shape (Map.Quests.Ledger), because two
    // beats can land in one tick (a scan that resolves two hidden bodies in one disc) and a second card
    // overwriting the first would lose exactly the moment this feature is about.
    private MissionMoment? _missionMoment;
    private readonly Queue<MissionMoment> _missionMomentQueue = new();

    // The receipts, newest first — the autopilot ledger's own shape (Map.Autopilot.StandDown), read out at
    // the Captain's desk by LedgerTips(). A card is dismissed in two seconds; this is what the captain can
    // go back and check, which is the literal answer to "did that just happen?".
    private readonly List<(double SimTime, string Text)> _missionMomentLedger = [];

    // #238 · ONCE PER TARGET, FOREVER. RevealBody is already idempotent, so this is not the reveal's only
    // latch — it is the seam's own, so that the law holds for any FUTURE caller that is not the scan (a
    // rumour cache, a station that reveals on a rumour) without each of them having to remember it.
    private readonly HashSet<string> _momentsRaisedFor = [];

    /// <summary>#238 · <b>RAISE THE BEAT.</b> The one door. Composes nothing and decides nothing about
    /// words — the <see cref="MissionMoment"/> arrives finished out of Core — and does the four things that
    /// make a transition a MOMENT rather than a state change.
    ///
    /// <para><b>The bird goes first, and that is deliberate.</b> Owner, endorsing his own idea: <i>"Love
    /// that thought of Parrot reacting to the glint from telescope"</i> — the perch is at the scope alcove,
    /// so the squawk lands half a beat before the card and the crew reacts before the paperwork does.</para>
    ///
    /// <para><b>Warp to 1×</b> is the #147 stand-down's own line, for the #147 stand-down's own reason,
    /// written down there: <i>"the drop must not slip past unseen at 10,000× warp"</i>. A found-her moment
    /// the captain warped through is the silent reveal with a card nobody saw.</para></summary>
    private void TheMomentTheyEarnedIt(MissionMoment moment)
    {
        if (moment.ShowMeBodyId is { Length: > 0 } target && !_momentsRaisedFor.Add(target))
        {
            return;
        }

        if (moment.ParrotSquawk is { } squawk)
        {
            // force: the bird's cooldown is an ALARM brake — it exists so three instrument warnings in a
            // row do not become three squawks. A payoff line four beats in the making is not that, and the
            // payday's own squawk has been forced since #185.
            SquawkNow(squawk, _lastTimestampMs ?? 0, force: true);
        }

        _missionMomentLedger.Insert(0, (SimTime, moment.Headline));
        Warp = 1;
        _effectiveWarp = 1;
        _missionMomentQueue.Enqueue(moment);
        ShowNextMissionMoment();
    }

    // ── WHAT THE REVEAL SAYS, AND WHO SAYS IT ─────────────────────────────────────────────────────────

    /// <summary>#238 · The beat for a hidden body that has just been charted. ONE shape, parameterised by
    /// the target's name and a bearing phrase — the owner's instruction, verbatim: <i>"Same beat for ANY
    /// hidden-target reveal (future secret stations, #223 rumour caches when discovery scanning arrives),
    /// one event shape."</i>
    ///
    /// <para>The roadster forks on the two things that are HERS and not a category's: the owner's own name
    /// for her in the shout (<i>the roadster</i>, not the chart's "Derelict Roadster") and the bird, whose
    /// payoff line belongs to the car gag and to nothing else. A secret station reveals through the
    /// identical call with no bird and its own address.</para></summary>
    private MissionMoment RevealMomentFor(string bodyId) =>
        bodyId == Derelict.RoadsterBodyId
            ? MissionMoments.Reveal(Derelict.RoadsterRevealName, Derelict.RoadsterBearingPhrase, bodyId,
                                    Parrot.Squawk.CarGlimpsed, _parrotCounter)
            : MissionMoments.Reveal(BodyName(bodyId), RevealBearingPhrase(bodyId), bodyId);

    /// <summary>#238 · <b>WHERE SHE IS, IN THE SIM'S OWN WORDS.</b> The owner's example bearing is
    /// "sunward of Mars", and for the roadster that is a hand-authored FACT about a hand-placed wreck
    /// (<see cref="Derelict.RoadsterBearingPhrase"/>) — nothing computes it, which is exactly why it is
    /// spelled once and read here rather than re-typed into a fifth sentence.
    ///
    /// <para>Every other body gets the phrase the charts already use for it: the system it rides in, off
    /// <see cref="PlanetLevelAncestor"/> — the same walk <see cref="BodyAddress"/> does for every offer
    /// blurb and ledger line, so the reveal and the address cannot come to disagree about where a thing is.
    /// A body with no planet above it rides the ROOT's system, and the root is read off the ephemeris
    /// rather than named here: no geometry is invented in this method, and none should be.</para></summary>
    private string RevealBearingPhrase(string bodyId)
    {
        CelestialBody? body = BodyById(bodyId);
        CelestialBody? planet = PlanetLevelAncestor(body);
        string system = planet is not null && planet.Id != bodyId
            ? planet.Name
            : _ephemeris?.Bodies.FirstOrDefault(b => b.ParentId is null)?.Name ?? "";
        return system.Length == 0 ? "" : $"{system.ToUpperInvariant()} system";
    }

    /// <summary>Surface the next queued beat — the celebration's <c>ShowNextCelebration</c>, one card at a
    /// time.</summary>
    private void ShowNextMissionMoment()
    {
        if (_missionMoment is not null || _missionMomentQueue.Count == 0)
        {
            return;
        }

        _missionMoment = _missionMomentQueue.Dequeue();

        // NO CUE OF ITS OWN, deliberately. Both raisers already play the sound of the thing that happened —
        // the reveal its "reveal", the prise its "board" — at the same instant, and a card that added a
        // third noise on top would be the game clearing its throat before speaking.
        StateHasChanged();
    }

    /// <summary>Close it. The general UI law (#824) in its plainest form: this card closes by DISMISS or by
    /// <i>show me</i>, and both of them are controls on the card.</summary>
    private void DismissMissionMoment()
    {
        _missionMoment = null;
        ShowNextMissionMoment(); // a scan that resolved two contacts owes the captain both cards
    }

    /// <summary>#238 · <b>SHOW ME.</b> The owner's own label, and the press that makes the card worth
    /// raising over a banner: the map camera goes to the new marker, so "there she is" is answered by
    /// actually showing her.
    ///
    /// <para>The centring is the nav search's, move for move (Map.UiState.NavSearch) — follow-ship and
    /// follow-destination are let go first, because either of them would snap the view back to the ship on
    /// the very next frame and the press would read as broken.</para></summary>
    private void ShowMeTheMissionMoment()
    {
        if (_missionMoment is { ShowMeBodyId: { Length: > 0 } id } && _ephemeris is not null)
        {
            FollowShip = false;
            _followDest = false;
            _camera.CenterOn(_ephemeris.Position(id, SimTime));
        }

        DismissMissionMoment();
    }
}
