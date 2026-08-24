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
/// <item>#777 · <b>…and sometimes the best surface is one that is already up.</b> A HOSTED beat
/// (<see cref="StoryBeats.Presentation.Hosted"/>) knocks on this same door and gets the same two disciplines
/// applied to it — cadence spent, seen-set filed, words logged — and then the seam raises nothing, because
/// the caller's own card is the canvas. The collector's hail is the shape's first case: its picture has been
/// on the BUSTED demand panel since #528, so an ordinary raise would have stacked a second modal showing the
/// identical painting. Hosting is how a beat gets counted as told without being told twice.
/// <para>…and the seam checks the claim rather than taking it. Hosting hands the CANVAS to the caller and
/// keeps the BOOKS here, which means the seam goes on writing "this was told" about a surface it no longer
/// owns. So a hosted beat is admitted to the books only while <see cref="TheHostIsUp"/> says its card is
/// really on the screen; a raise made without one is refused, unspent, and written into the ledger as an
/// engine fault. #761's law is that a plot-significant moment reaches the player on the surface they are
/// looking at — and a beat filed as told with nothing showing does not break that law loudly, it erases
/// the evidence that it was broken.</para></item>
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
    /// that will eventually empty itself into the player's face all at once.
    ///
    /// <para>#664 · It holds the OUTCOME too. A deferred card that arrived without its arithmetic would be the
    /// #736 bug back with a delay on it — the caption would land and the 350 cr, the die and the pack count
    /// would not.</para></summary>
    private (StoryBeats.Beat Beat, string? Subject, string? Outcome)? _deferredBeat;

    /// <summary>
    /// The card currently taking the screen, if any.
    ///
    /// <para>#664 · <c>Outcome</c> is #736's channel, arrived here from the deleted <c>Map.RevealCard.cs</c>:
    /// what the ACT that raised the card actually DID — the coin that moved, the die that was rolled, the
    /// decision the captain now has to make. The caption is fiction and the outcome is arithmetic, and until
    /// #736 the arithmetic was pulsed to the HUD <i>under this card's own backdrop</i>: in the DOM and not on
    /// the screen. Null on a card raised by something that settled nothing, which is most of them.</para>
    /// </summary>
    private (StoryBeats.Beat Beat, string? Subject, string? Outcome)? _storyCard;

    /// <summary>The plate riding the edge, and when it goes away.</summary>
    private (StoryBeats.Beat Beat, string? Subject, double UntilSimTime)? _storyPlate;

    /// <summary>
    /// Raise a beat. The ONLY entry point — a feature says what happened and nothing about presentation, so the
    /// discipline cannot be argued with locally.
    /// </summary>
    /// <param name="subject">A ship's name, a haven, a headline. Optional; every caption reads whole without it.</param>
    /// <param name="outcome">#736 · What the act that raised this beat actually settled — the credits, the die,
    /// the decision. It rides the card's own subtree, which is the one layer the backdrop cannot blur, rather
    /// than pulsing to a HUD the card is standing on top of. Null for a beat that settled nothing.</param>
    private void RaiseStoryBeat(StoryBeats.Beat beat, string? subject = null, string? outcome = null)
    {
        if (!BeatMaySpeak(beat, subject))
        {
            return;
        }

        // A card that may wait, raised while something is trying to kill the captain, waits. It is held rather
        // than dropped because these are the moments most worth reading — just not now.
        //
        // #865 · …AND A CARD RAISED WHILE THE CAPTAIN IS TAKING A CHAIR WAITS TOO, whatever it is about.
        // Owner, sitting down at a canteen top: "I sat down the table but the pop ups blocked my view of my
        // avatar sitting down." The snap onto the seat is the tiny animation #820 and #846 exist for and the
        // player is meant to WATCH it; a card over it un-ships both. This arm asks NOTHING about
        // deferrability, and that is deliberate: the danger hold is a judgement about whether a beat is
        // urgent enough to interrupt a fight, and this is a beat and a half of screen owed to a press the
        // player has just made. Nothing is dropped — the cadence is unspent until it actually speaks, and the
        // queue below serves it the moment the chair is taken.
        if (StoryBeats.PresentationOf(beat) == StoryBeats.Presentation.Card
            && (TheSitBeatIsSettling
                || (StoryBeats.DeferrableWhileInDanger(beat) && CaptainIsInDanger())))
        {
            _deferredBeat ??= (beat, subject, outcome);
            return;
        }

        ShowStoryBeat(beat, subject, outcome);
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

    /// <summary>
    /// Put it on screen, and remember that it spoke.
    ///
    /// <para>#777 · …except for a <see cref="StoryBeats.Presentation.Hosted"/> beat, where the screen is
    /// already somebody else's and putting anything on it would BE the bug. The bookkeeping above the switch
    /// and the log line below it are the whole of what the seam owes a hosted beat: it is COUNTED as told,
    /// its cadence spends, its words go in the book — and the caller's own card carries the picture. A
    /// switch rather than an if/else on purpose, and the old <c>else</c> is the reason: it made CARD the
    /// answer to every question nobody had thought of yet, so the fourth presentation somebody invents would
    /// have shipped as a full-screen modal by default. Here it matches no arm and shows nothing, which is
    /// the safe way to be wrong.</para>
    /// </summary>
    private void ShowStoryBeat(StoryBeats.Beat beat, string? subject, string? outcome = null)
    {
        // #777 follow-up · AND THE BOOKS ARE NOT COOKED. Hosting moved this beat's canvas to the caller, and
        // with it the one thing the seam could previously guarantee on its own: that a beat it counted as
        // TOLD had actually been put in front of somebody. Everything below this line writes the beat into
        // the record — the cadence spends, the seen-set files, the log says it happened — and for a hosted
        // beat every word of that is a claim about a surface this method does not own.
        //
        // The shipped edges do it right (both set the demand panel one statement before they knock), and
        // three source-shape guards say so. But "the two callers that exist today are correct" is not the
        // same law as "a hosted beat is never counted as told with nothing on the screen", and the third
        // caller is the one nobody reviews. So the seam asks, at the only gate every route to the books
        // passes through — RaiseStoryBeat comes here, and so does the deferred queue.
        if (StoryBeats.PresentationOf(beat) == StoryBeats.Presentation.Hosted && !TheHostIsUp(beat))
        {
            RefuseTheHostlessRaise(beat, subject);
            return;
        }

        _beatsSpoken[SeenKey(beat, subject)] = SimTime;

        switch (StoryBeats.PresentationOf(beat))
        {
            // #664 · The cue comes from Core now (StoryBeats.Cue), and the empty one is load-bearing: eleven
            // of these moments are a press that has already made its own noise — "board" for a find, "alarm"
            // for a hatch coming off its dogs — and a "reveal" chime layered over that is the stacked card
            // again, in the one channel the player cannot close. The eight beats that raise their own surface
            // out of nowhere still chime, exactly as before.
            case StoryBeats.Presentation.Plate:
                _storyPlate = (beat, subject, SimTime + StoryBeats.PlateSeconds);
                PlayTheBeatsCue(beat);
                break;

            case StoryBeats.Presentation.Card:
                _storyCard = (beat, subject, outcome);
                PlayTheBeatsCue(beat);
                break;

            case StoryBeats.Presentation.Hosted:
                // Nothing is raised, and nothing is played. The host is already up, already showing this
                // beat's painting, and already making its own noise — a collector's grapples arrive on the
                // "board" cue. A second "reveal" chime layered over that would be the stacked card again,
                // in the one channel the player cannot close.
                break;
        }

        // The log keeps the words even when the picture has gone, because a card is the only place some of these
        // sentences are ever written down.
        LogAutopilotEvent($"{StoryBeats.Title(beat, subject)} — {StoryBeats.Caption(beat, subject)}");
        StateHasChanged();
    }

    /// <summary>#664 · Make whatever noise Core says this beat's own surface makes, and nothing when Core says
    /// the moment has already been heard. Not a null check the caller could forget: the seam asks every
    /// time.</summary>
    private static void PlayTheBeatsCue(StoryBeats.Beat beat)
    {
        if (StoryBeats.Cue(beat) is { Length: > 0 } cue)
        {
            RendererInterop.PlayCue(cue);
        }
    }

    /// <summary>
    /// #777 follow-up · IS THE CANVAS ACTUALLY ON THE SCREEN? The client's half of
    /// <see cref="StoryBeats.HostCard"/>: Core names the host in prose because Core does not know what a card
    /// is, and this is the one place that turns that sentence into a question the running game can answer.
    ///
    /// <para>Kept as a <c>switch</c> with no default arm on purpose, and the arm order matters less than what
    /// is NOT here: there is no <c>_ => true</c>. A beat somebody marks
    /// <see cref="StoryBeats.Presentation.Hosted"/> tomorrow and forgets to answer for here is refused rather
    /// than waved through, because "I could not tell whether the player saw it" and "the player saw it" are
    /// not the same answer, and only one of them is safe to write into the record.</para>
    ///
    /// <para>The hail's host is the demand panel and not merely <i>an open BUSTED encounter</i>. That
    /// distinction is the whole of the check: the same object goes on to carry the freeze-frame, the
    /// confiscation receipt and the clinic wake, and none of those show a collector's grapples. A raise that
    /// landed on one of those stages would be counted as told over a picture of the captain dying.</para>
    /// </summary>
    private bool TheHostIsUp(StoryBeats.Beat beat) => beat switch
    {
        StoryBeats.Beat.CollectorHail => _busted is { Phase: BustedEncounter.Stage.Demand },

        // #973 L5b · her card, and not merely "somebody is walking across the bar": the beat is her ENTRANCE
        // arriving at the table, and the surface that carries its painting is the panel with her portrait on
        // it. A raise while she was still mid-stride would be counted as told over an empty chair.
        StoryBeats.Beat.WalkIn => _walkInCard is not null,

        _ => false,
    };

    /// <summary>
    /// #777 follow-up · A HOSTED RAISE WITH NO HOST UP, REFUSED LOUDLY.
    ///
    /// <para>Refused, rather than thrown: this is an engine mistake and not a player-reachable state, and a
    /// modal seam that crashes the page is a worse bug than the one it is objecting to. What matters is that
    /// the beat is <b>not counted</b> — the cadence stays unspent, the seen-set stays empty, and the beat can
    /// still be told properly the next time its host really is up. #761's law is that a plot-significant
    /// moment reaches the player on the surface they are looking at; a beat filed as told with nothing on the
    /// screen does not break that law quietly, it deletes the evidence that it was broken.</para>
    ///
    /// <para>And loudly in the ledger, which is where this codebase writes things a person is meant to find:
    /// the line names the beat, names the host Core says should have been up, and says outright that nothing
    /// was counted. A guard asserts on that sentence rather than on the absence of a card, because an empty
    /// screen is what a working seam and a broken one both look like — and a refusal that could only be
    /// observed as an absence would need a field on the component, which is a thing #905's frame ledger
    /// charges thirty re-pinned fingerprints for. The ledger already existed and is the louder surface
    /// anyway.</para>
    /// </summary>
    private void RefuseTheHostlessRaise(StoryBeats.Beat beat, string? subject)
    {
        LogAutopilotEvent(
            $"⚠ ENGINE — {beat} is a HOSTED story beat raised with no host on the screen"
            + (string.IsNullOrWhiteSpace(subject) ? "" : $" (about {subject})")
            + $". Its canvas is {StoryBeats.HostCard(beat)}; nothing was shown and NOTHING WAS COUNTED, so "
            + "the beat can still be told when that surface is really up.");
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
        // #865 · …and once the chair has actually been taken. "The strip is open with the scene by the time
        // any deferred card raises" is the second half of the sit-beat rule, and it is kept here rather than
        // by hoping the beat has run out on its own.
        if (_deferredBeat is { } waiting && !CaptainIsInDanger() && !TheSitBeatIsSettling
            && _storyCard is null && _busted is null)
        {
            _deferredBeat = null;
            ShowStoryBeat(waiting.Beat, waiting.Subject, waiting.Outcome);
        }
    }

    /// <summary>Put the card away. Focus is NOT touched here: every mouse dismissal routes through
    /// <c>Dismiss(...)</c>, which owns handing the keyboard back — the #470 seam, and the reason nine cards used
    /// to leave the deck keys dead.
    ///
    /// <para>#768 (arrived here with #664) · …and the sayings this card was standing on are freed as it goes.
    /// An event that raises a card HOLDS its ranked lines rather than pulsing them under the backdrop, so the
    /// ✕ is where they are finally said. Every road out of the card comes through here — Esc, Enter, the
    /// backdrop, the button — which is why nothing anywhere clears <c>_storyCard</c> by hand.</para></summary>
    private void CloseStoryCard()
    {
        _storyCard = null;
        ReleaseHeldSayings();
    }

    /// <summary>
    /// #992 · TAKE THE PLATE DOWN NOW. Owner ruling 2026-08-24: <i>"As a general ruling there should not be a
    /// pop-up that cannot be closed or minimized."</i>
    ///
    /// <para>The plate shipped with no way out on purpose — the comment beside its markup read <i>"It is not
    /// clickable on purpose — a thing you must dismiss is a thing that blocked you"</i> — and that reasoning
    /// held right up until a player wanted the corner back. The great port's plate is the one that made the
    /// case: <c>ArrivalTube.Title(Tier.GreatPort)</c> is literally <b>"🛬 THE LONG WALK IN"</b>, its caption is
    /// the longest of the three, and it lands bottom-left across the captain's-remote button for the whole of
    /// <c>StoryBeats.PlateSeconds</c>. Owner, live: <i>"the long walk-in there just blocks UI."</i></para>
    ///
    /// <para>So the plate keeps everything that made it a plate — it steals no keyboard, stops no world, eats
    /// no click (<c>pointer-events: none</c> stays on the panel; only the ✕ re-enables its own) and still
    /// expires on its own clock. What it gains is the one thing the ruling asks for: a way out that is EARLIER
    /// than the clock. It is a close and not a minimise because a plate has no second size — the tucked-away
    /// version of five words in a corner is no words in that corner.</para>
    ///
    /// <para>Nothing about the BOOKS moves: the cadence was spent and the seen-set filed when the beat spoke
    /// (<c>ShowStoryBeat</c>, one screen up), so a plate the captain waves away is a plate that was told. That
    /// is the same accounting a card's ✕ has always had.</para>
    /// </summary>
    private void CloseStoryPlate() => _storyPlate = null;

    /// <summary>What a card or plate is showing, resolved for the razor in one place. The subject reaches all
    /// three now (#664): two of the arcs choose their painting by the shard the captain just assembled, and a
    /// released hatch puts the room's name in its own stamp.</summary>
    private (string Title, string Art, string Caption) StoryBeatCopy(StoryBeats.Beat beat, string? subject) =>
        (StoryBeats.Title(beat, subject), StoryBeats.ArtFile(beat, subject), StoryBeats.Caption(beat, subject));
}
