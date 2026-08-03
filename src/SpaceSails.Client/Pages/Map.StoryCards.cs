using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #528 · THE STORY-CARD SEAM. Owner: <i>"The pattern something of importance to story telling happens we get
/// image pop up should happen universally in the game as long as it does not block the playing too much or be too
/// repetitive."</i>
///
/// <para>That is a rule about a SYSTEM, so this is the one door every moment knocks on. A feature raises a
/// <see cref="StoryBeats.Beat"/> and this decides — from Core's rules, never from the caller's opinion — whether
/// it may speak at all, whether it takes the screen or rides the edge, and whether it has to wait until nothing
/// is trying to kill the captain.</para>
///
/// <para>The two disciplines are the owner's own two constraints:</para>
/// <list type="bullet">
/// <item><b>Not too repetitive</b> — a beat fires once ever, or once and then not for a while, or every time,
/// according to its <see cref="StoryBeats.Cadence"/>. The seen-set and the clock live here; the policy does not.</item>
/// <item><b>Does not block the playing too much</b> — a PLATE never steals the keyboard and never stops the
/// world; a CARD may, and a deferrable card WAITS while the captain is in danger. The wreck lane paid for that
/// rule once already, when a full-screen tutorial let a pack kill the captain behind it.</item>
/// </list>
/// </summary>
public sealed partial class Map
{
    /// <summary>
    /// Beats this captain has already been shown, for the once-ever, cooled and once-per-subject cadences. Keyed
    /// by beat AND subject; the value is the sim time it last spoke.
    ///
    /// <para>#541 widened this key. A per-beat key was fine while every beat was about one thing that happens to
    /// a captain, and wrong the moment a beat became about a PLACE: the arrival tube would have shown one berth's
    /// gangway and then silently swallowed every other berth in the system. The cadences that do not care about
    /// the subject file under a null one, so nothing else changed behaviour.</para>
    /// </summary>
    private readonly Dictionary<(StoryBeats.Beat Beat, string? Subject), double> _beatsSpoken = [];

    /// <summary>A card waiting for a calmer moment. One at a time on purpose: a queue that can stack is a queue
    /// that will eventually empty itself into the player's face all at once.</summary>
    private (StoryBeats.Beat Beat, string? Subject)? _deferredBeat;

    /// <summary>The card currently taking the screen, if any.</summary>
    private (StoryBeats.Beat Beat, string? Subject)? _storyCard;

    /// <summary>The plate riding the edge, and when it goes away.</summary>
    private (StoryBeats.Beat Beat, string? Subject, double UntilSimTime)? _storyPlate;

    /// <summary>
    /// Raise a beat. The ONLY entry point — a feature says what happened and nothing about presentation, so the
    /// discipline cannot be argued with locally.
    /// </summary>
    /// <param name="subject">A ship's name, a haven, a headline. Optional; every caption reads whole without it.</param>
    private void RaiseStoryBeat(StoryBeats.Beat beat, string? subject = null)
    {
        if (!BeatMaySpeak(beat, subject))
        {
            return;
        }

        // A card that may wait, raised while something is trying to kill the captain, waits. It is held rather
        // than dropped because these are the moments most worth reading — just not now.
        if (StoryBeats.PresentationOf(beat) == StoryBeats.Presentation.Card
            && StoryBeats.DeferrableWhileInDanger(beat)
            && CaptainIsInDanger())
        {
            _deferredBeat ??= (beat, subject);
            return;
        }

        ShowStoryBeat(beat, subject);
    }

    /// <summary>Whether this beat is allowed to speak right now, by Core's cadence rules and nothing else.</summary>
    private bool BeatMaySpeak(StoryBeats.Beat beat, string? subject)
    {
        if (!_beatsSpoken.TryGetValue(SeenKey(beat, subject), out double last))
        {
            return true;
        }

        return StoryBeats.CadenceOf(beat) switch
        {
            StoryBeats.Cadence.OnceEver => false,
            StoryBeats.Cadence.OncePerSubject => false,   // …for THIS subject; another one is a fresh moment
            StoryBeats.Cadence.Cooled => SimTime - last >= StoryBeats.CooldownSeconds(beat),
            _ => true,
        };
    }

    /// <summary>How a beat files itself in the seen-set. Only <see cref="StoryBeats.Cadence.OncePerSubject"/>
    /// remembers WHICH one it was about; everything else files under the beat alone, so a cooled beat cannot be
    /// re-triggered simply by happening to a different ship.</summary>
    private static (StoryBeats.Beat, string?) SeenKey(StoryBeats.Beat beat, string? subject) =>
        StoryBeats.CadenceOf(beat) == StoryBeats.Cadence.OncePerSubject ? (beat, subject) : (beat, null);

    /// <summary>Put it on screen, and remember that it spoke.</summary>
    private void ShowStoryBeat(StoryBeats.Beat beat, string? subject)
    {
        _beatsSpoken[SeenKey(beat, subject)] = SimTime;

        if (StoryBeats.PresentationOf(beat) == StoryBeats.Presentation.Plate)
        {
            _storyPlate = (beat, subject, SimTime + StoryBeats.PlateSeconds);
            RendererInterop.PlayCue("reveal");
        }
        else
        {
            _storyCard = (beat, subject);
            RendererInterop.PlayCue("reveal");
        }

        // The log keeps the words even when the picture has gone, because a card is the only place some of these
        // sentences are ever written down.
        LogAutopilotEvent($"{StoryBeats.Title(beat)} — {StoryBeats.Caption(beat, subject)}");
        StateHasChanged();
    }

    /// <summary>
    /// Is something trying to kill the captain right now? Deliberately generous about danger: a held card costs
    /// nothing, and a card that lands mid-fight costs the thing the owner asked us not to spend — his attention
    /// while he is playing.
    /// </summary>
    private bool CaptainIsInDanger() =>
        _busted is not null
        || _reevers.Count > 0
        || _hunters.Exists(h => !h.BrokenOff && !h.CaughtPlayer)
        || _shipChargesSeconds is not null
        // #538: a professional with a lamp on your face is danger, and a story card over that would be the exact
        // mistake the wreck lane already paid for once.
        || AnySweeperOnTheCaptain;

    /// <summary>Serve the queue and retire the plate. Called once a frame from the sim, wherever the ship is.</summary>
    private void AdvanceStoryCards()
    {
        if (_storyPlate is { } plate && SimTime >= plate.UntilSimTime)
        {
            _storyPlate = null;
            StateHasChanged();
        }

        // A held card goes up the moment the scene is calm and nothing else is on the screen.
        if (_deferredBeat is { } waiting && !CaptainIsInDanger() && _storyCard is null && _busted is null)
        {
            _deferredBeat = null;
            ShowStoryBeat(waiting.Beat, waiting.Subject);
        }
    }

    /// <summary>Put the card away. Focus is NOT touched here: every mouse dismissal routes through
    /// <c>Dismiss(...)</c>, which owns handing the keyboard back — the #470 seam, and the reason nine cards used
    /// to leave the deck keys dead.</summary>
    private void CloseStoryCard() => _storyCard = null;

    /// <summary>What a card or plate is showing, resolved for the razor in one place.</summary>
    private (string Title, string Art, string Caption) StoryBeatCopy(StoryBeats.Beat beat, string? subject) =>
        (StoryBeats.Title(beat), StoryBeats.ArtFile(beat), StoryBeats.Caption(beat, subject));
}
