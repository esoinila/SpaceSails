using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #973 L2 · HARLAN FESS ON HIS ROUNDS — the salesman, and the four verbs the old crew will inherit.
///
/// <para>Owner: <i>"the salesmen are a low-stakes place to practise NPC interaction (they move about, they
/// try to sell, nothing is at stake)."</i> So he is deliberately built out of nothing new. He is a
/// <see cref="Walker"/> like the haulier and the sweep team, planned through the one <c>OnFoot</c> that
/// claims the person's gait, drawn out of the walker band, and stepped by the same frame that steps
/// everybody else. What this file adds is the four verbs — <b>approach, address, withdraw, remember you said
/// no</b> — and every rule behind them is a pure function in <see cref="NebulaRep"/>.</para>
///
/// <h3>Where he works, and why it is the canteen floor</h3>
///
/// <para>The brief said "a concourse or bar deck", and in this game that room is the hive's upper canteen:
/// it is the only deck with a walker band, a counter, tops the captain can actually sit alone at, and doors
/// somebody can walk in from. A docked station's bar has posters and a barkeep and no seating and no
/// walkers at all — a salesman there could only teleport at a captain who cannot sit down, which is the
/// opposite of the practice the owner asked for. The presence rota is therefore keyed on the BODY being
/// visited rather than on a berth id; the law ("at most one place in three, never two visits running") is
/// unchanged and lives in Core.</para>
///
/// <para>#251 · This file keeps what he IS — his dwell, his reach, his memory of you, the name on his
/// file, and the one place forgetting happens (a different ground is a different visit, and a different
/// visit is a man who has never met you). The other four are named for the part of his day they own:
/// <c>.Day</c> (one frame of it — whether he is here, where he goes next, and when his shift is over),
/// <c>.Round</c> (#1061's marks, frozen to the watch that dealt them), <c>.Step</c> (walking him, and
/// #973's rule that his errand ends STANDING UP), and <c>.Pitch</c> (he is at your elbow, and he is
/// delighted).</para>
///
/// <para>The family declares no static field, so the #1163 initializer hazard is absent by construction.
/// No member is renamed, re-scoped or re-ordered by the cut.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>How long he stands at a fixture before drifting to the next one. Long enough to read as a
    /// man waiting for somebody to look up, short enough that the room has motion in it.</summary>
    private const double RepDwellSeconds = 9.0;

    /// <summary>How close the captain has to pass a rebuffed Fess before he says the only thing he has left
    /// to say. The captain's own interact reach, so "walking past him" means what it means everywhere else.</summary>
    private const double RepPassingReachDu = DeckPlan.InteractRadius;

    /// <summary>Which visit's room he is remembering. Null off the ground; a different body is a new
    /// visit — the same fold <c>EnsureBarVisit</c> keeps for the station bar.</summary>
    private string? _repVisitBody;

    /// <summary>The running count of ground visits this thread has made. The rota's clock.</summary>
    private int _repVisitIndex = -1;

    /// <summary>Whether the rota has him working THIS visit at all.</summary>
    private bool _repWorkingHere;

    /// <summary>#973 L2 dev cheat (<c>/map?rep=1</c>, <c>/map?rep=0</c>): force him on or off this ground.
    /// Null is the shipped rota. It forces WHETHER and never WHO or WHAT — the pitch, the prices, the
    /// rarity of the bleed and the once-per-life page are all the ones a captain gets.</summary>
    private bool? _repCheat;

    /// <summary>Remember-you-said-no, and only until the doors shut.</summary>
    private NebulaRepVisit _repMemory = NebulaRepVisit.Fresh;

    /// <summary>How many times he has reached a table and pitched, this thread. The bleed's clock.</summary>
    private int _repMeetings;

    /// <summary>When he next drifts to another fixture.</summary>
    private double _repMoveOnAt;

    /// <summary>Whether he has already said the one thing a rebuffed salesman says, this visit.</summary>
    private bool _repSaidPassing;

    /// <summary>The pitch card, when he is standing at the table with it. Null the rest of the time, and
    /// the whole of what <c>TheHostIsUp</c> asks about.</summary>
    private NebulaRep.RepPitch? _repCard;

    /// <summary>The name he read off the file for this pitch — the captain's, or a dead one's.</summary>
    private string _repNameOnFile = "";

    /// <summary>Whether this pitch is a bleed, which is the only thing that puts the extra button up.</summary>
    private bool _repBleeding;

    /// <summary>What he last said back, under the pitch. Cleared when he goes.</summary>
    private string? _repSaid;

    // ── #1061 · HE WORKS THE ROOM ──────────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-09-01: "let's at some point work on those A* walking insurance salesmen at stations :-D"
    //
    // Until this lane his beat was a ring of FURNITURE — the counter, the ends of two or three tops — walked
    // round for ever with nothing at the far end of any of it. The room contained a man drifting. What it
    // contains now is a man SELLING: he crosses to somebody else's table, stands there for a beat of patter,
    // and goes on to the next mark, and when the room is worked he leaves through a leaf that does not open
    // for the captain, like anybody whose shift has ended. Not one word is said at any of those tables — the
    // pause IS the patter (§13.8), and the point of the whole beat is that a captain sitting two tops away
    // WATCHES THE PITCH COMING.
    //
    // The round itself is Core's (Egress.Marks), frozen to the watch, so it is the same tables in the same
    // order on every machine and across a reload.

    /// <summary>#1061 · The round he is working: whose tables, in what order, and how long each pause lasts.
    /// Null is a question this visit has not asked yet; empty is an answer it gave (a room with nobody in it
    /// but the captain).</summary>
    private IReadOnlyList<Egress.Patter>? _repRound;

    /// <summary>#1061 · Which watch that round belongs to. A shift turning over is the room forgetting —
    /// #731's own law — so the people he was working went home and he starts on the ones who are here now.</summary>
    private long _repRoundWatch = long.MinValue;

    /// <summary>#1061 · How many of the round's marks he has actually finished. The counter does not count:
    /// nobody is sitting at it, and the floor under <see cref="Egress.MarksBeforeTheTable"/> is a floor about
    /// PEOPLE the captain has watched him work.</summary>
    private int _repMarksWorked;

    /// <summary>#1061 · Whether he has already stood at the counter this visit — the one stop on his round
    /// that is furniture, kept because it is where he says he will be and because a room with nobody in it
    /// still gets a man walking into it.</summary>
    private bool _repStoodAtTheCounter;

    /// <summary>#1061 · Where he was standing when the last pause ended.
    ///
    /// <para>The next leg begins THERE and not back at a doorstep, which is #973 L5b's own flag paid off for
    /// the salesman: <i>"a player watching the counter would see her vanish from it and come back out of the
    /// cellar. That is a worse lie than not retrying."</i> Null before his first walk of a visit, which is the
    /// one walk that really does begin at a door.</para></summary>
    private DeckReachability.Point? _repStandingAt;

    /// <summary>#1061 · His shift here is over. The room is worked, he has gone out through a leaf, and he
    /// does not come back — until the watch turns over, when it is a different room full of people.</summary>
    private bool _repShiftOver;

    /// <summary>#973 L2 · Which life the signing flashback has already come back in, or 0 for none.
    ///
    /// <para>The beat's own cadence is <c>EveryTime</c> — L1 chose it for the LEDGER, where the once-per-page
    /// latch is <c>FilingLine.PageState.Refused</c> and a rebirth re-greys the book. The rep has no page and
    /// no latch, so his once-per-life is kept here: the day you signed comes back to a captain once, and the
    /// NEXT captain gets it back because it is a different man reaching for it.</para></summary>
    private int _repSigningToldInLife;

    /// <summary>The name the file has. It is the captain's, except on the rare watch it is not.</summary>
    private string RepNameOnFile(bool bleeding) =>
        bleeding && ActiveThreadInfo?.Retired is { Count: > 0 } retired
            ? Captains.CleanName(retired[^1].Name)
            : ActiveCaptainName;

    /// <summary>How many captains this thread has buried.</summary>
    private int RetiredCaptainCount => ActiveThreadInfo?.Retired.Count ?? 0;

    /// <summary>Which life the captain is on, counting from one — the flashback subject's stamp.</summary>
    private int CaptainsLife => RetiredCaptainCount + 1;

    // ── The visit ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A DIFFERENT GROUND IS A DIFFERENT VISIT, and a different visit is a man who has never met you. The
    /// one place forgetting happens; everything else in this file only ever reads the memory.
    /// </summary>
    private void EnsureRepVisit(string? bodyId)
    {
        if (_repVisitBody == bodyId)
        {
            return;
        }

        _repVisitBody = bodyId;

        // #973 · …and the VOID'S WEATHER folds with him. One fold, because the rota that decides whether he
        // walked this room and the rule that decides whether the room is talking about him have to mean the
        // same room and the same visit — see Map.Weather.cs.
        EnsureTheWeathersVisit(bodyId);

        _repCard = null;
        _repSaid = null;
        _repBleeding = false;
        _repSaidPassing = false;
        _repMoveOnAt = 0;
        ForgetTheRound();

        if (bodyId is null)
        {
            _repWorkingHere = false;
            return;
        }

        _repVisitIndex++;
        _repMemory = _repMemory.AtVisit(_repVisitIndex);
        _repWorkingHere = _repCheat
            ?? NebulaRep.IsWorkingThisStation(_activeThreadId ?? "", bodyId, _repVisitIndex);
    }
}
