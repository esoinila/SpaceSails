using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1061 beat 2 · <b>THE HARDCASE ON THE MOON</b> — Brem Kolt, hazardous accounts, and the one walk in this
/// game that is a person running away from something.
///
/// <para>Owner, 2026-09-01: <i>"Maybe some hardcode salesman might be on moon also and runs away from
/// reevers in despair :-D"</i></para>
///
/// <h3>The three things that happen, and the one that says nothing</h3>
///
/// <para><b>He is out there when you come down the tube.</b> Not through a door — a moon has no doors — so
/// the fiction the seam has to carry is the joke: he found you HERE. The ledger travels and the man is
/// disposable, which is #973's amnesia fine print read from the company's side.</para>
///
/// <para><b>He crosses the regolith and pitches.</b> The card is three sentences of authored canon and a row
/// of <see cref="NebulaRep"/>'s own buttons — one firm, one policy, one set of prices. Refuse and he says the
/// third line, which is the only line in the game that tells you what the book expects of you.</para>
///
/// <para><b>And when an Old One comes into his sight he breaks and runs.</b> <i>Nothing is said.</i> No card,
/// no pulse, no plate, no caption, no note in the book. What the captain gets is a man they were talking to
/// thirty seconds ago going flat out across open ground, and whatever they make of that. He runs like a man
/// who KNOWS what they are; the company sent him anyway; and the game refuses to say either of those things
/// out loud. That silence is the beat and <c>TheHardcaseOnTheMoonTests</c> reads this file's source to keep
/// it.</para>
///
/// <h3>What is reused, said out loud, because almost all of it is</h3>
///
/// <para>He is a <see cref="Walker"/> in the excursion's own band, planned through the one <c>OnFoot</c> that
/// claims <c>Gait.Person</c>, drawn by <c>FillWalkerDroids</c> out of the slots the surface already writes.
/// His eyes are <see cref="SurfaceCollision.HasLineOfSight"/> — the identical call the Old Ones' own eyes,
/// the tube's gun and a swinging arm make. His card goes up through <c>RaiseAScrimCard</c>, so it queues
/// behind whatever the captain is already reading. His sale is
/// <see cref="NebulaRep.PolicyAfterBuying"/>. <b>Not one of those is a second copy.</b></para>
///
/// <h3>And the asymmetry is the whole scene</h3>
///
/// <para>He plans a route. The thing he is running from cannot — the Old Ones keep their stagger, which is
/// canon, and <see cref="NpcWalk.Plan"/> throws at the door on any gait but a person's. A man who can find
/// his way out, running from things that only ever come straight at you, is the picture; nothing in this
/// file explains it.</para>
///
/// <para>#251 · This file keeps what he IS and where his visit is about. The other four are named for the
/// part of his day they own: <c>.Day</c> (one frame of his very bad day — whether he is afoot, where he
/// comes from, and the post he takes beside you), <c>.Step</c> (walking him, his sightline, and the
/// running), <c>.Pitch</c> (the card at your elbow and its four answers), and <c>.Schedule</c> (the sheet
/// in the dust, and the rows the vault keeps).</para>
///
/// <para>The family declares no static field, so the #1163 initializer hazard is absent by construction.
/// No member is renamed, re-scoped or re-ordered by the cut.</para>
/// </summary>
public sealed partial class Map
{
    // ── What he is doing, and where ────────────────────────────────────────────────────────────────────

    /// <summary>Which ground this visit is about (<see cref="HardcaseRep.GroundKey"/>), or null off one. A
    /// different ground is a different visit and a man who has never met you — the same fold
    /// <c>EnsureRepVisit</c> keeps for the canteen.</summary>
    private string? _hardcaseGround;

    /// <summary>Whether the rota has him on THIS ground at all.</summary>
    private bool _hardcaseWorkingHere;

    /// <summary>#1061 dev cheat (<c>/map?kolt=1</c>, <c>/map?kolt=0</c>): force him onto this ground or off
    /// it. Null is the shipped rota. It forces WHETHER and never WHO or WHAT — his three lines, his prices
    /// and the sheet he drops are the ones a captain gets.</summary>
    private bool? _hardcaseCheat;

    /// <summary>The grounds he has already been found on, at most <see cref="HardcaseRep.GroundsAtMost"/>.
    /// The cap, and the one piece of him that rides the vault.</summary>
    private readonly List<string> _hardcaseGroundsWorked = [];

    /// <summary>Whether he has already reached the captain and pitched on this ground.</summary>
    private bool _hardcasePitched;

    /// <summary>Whether the captain has sent him away on this ground. He does not come back at you —
    /// he said what the book says and there is nothing after it.</summary>
    private bool _hardcaseRefused;

    /// <summary>Whether he has bolted. Once he has, he is gone for this excursion: a man who ran from an Old
    /// One and then strolled back to finish his pitch would be the sim contradicting the only thing this beat
    /// ever says.</summary>
    private bool _hardcaseFled;

    /// <summary>When he next decides to do something. His standing-about clock.</summary>
    private double _hardcaseMoveOnAt;

    /// <summary>Where he is standing, so the next leg begins at his own feet rather than back at the spot the
    /// ground first put him — #973 L5b's flag, paid off a second time.</summary>
    private DeckReachability.Point? _hardcaseStandingAt;

    /// <summary>His card, when it is up. Null the rest of the time, and the whole of what the scrim census
    /// asks about.</summary>
    private IReadOnlyList<NebulaRep.RepOffer>? _hardcaseCard;

    /// <summary>How long he stands about between legs. The rep's own dwell, because he is the same kind of
    /// body doing the same kind of nothing.</summary>
    private const double HardcaseDwellSeconds = 9.0;

    /// <summary>How near the captain has to be before he bothers crossing to them. Deliberately generous —
    /// this is open ground and not a room, and a man who waited for you to walk into his elbow would never
    /// pitch at all.</summary>
    private const double HardcaseApproachRangeDu = 26.0;

    /// <summary>Where he stands about, in deck units either side of the tube's own column. Far enough off
    /// the mouth that he is not in the way of a captain sprinting home, near enough that he is the first
    /// thing on the ground you see.</summary>
    private const double HardcasePostNearDu = 10.0;
    private const double HardcasePostFarDu = 18.0;

    /// <summary>How far below the top rim his post sits — inside the landing band, on the fused apron, which
    /// is the only part of a moon a man in a suit and bad shoes would choose to stand on.</summary>
    private const double HardcasePostDepthDu = 5.0;

    // ── The visit ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Which ground this VISIT is about, in his book's own spelling — null in a berth and null alongside a
    /// derelict, where there is no such ground.
    ///
    /// <para><b>It does not ask which floor the captain is on</b>, and that is the whole reason it is a
    /// different question from <see cref="HeCouldBeAfootNow"/> one line down. A lift ride to B1 and back is
    /// the same trip to the same crater: a man who had already broken and run would otherwise be forgotten
    /// by the ride down and be standing at his post again on the way up, which is the sim contradicting the
    /// only thing this beat ever says.</para>
    /// </summary>
    private string? TheGroundUnderfootForKolt() =>
        _surface is { } ex && !OnWreck
            ? HardcaseRep.GroundKey(ex.Stop.Body.Id, ex.Site.Index)
            : null;

    /// <summary>…and whether he may be ON his feet this instant, which is the floor's question.
    /// <see cref="HardcaseRep.GroundLikeThis"/> is asked rather than re-derived, so the law about WHERE he
    /// can appear has exactly one statement and the Core suite can drive it.</summary>
    private bool HeCouldBeAfootNow() =>
        _surface is { } ex && HardcaseRep.GroundLikeThis(landed: true, OnWreck, ex.Floor);

    /// <summary>
    /// A DIFFERENT GROUND IS A DIFFERENT VISIT. The one place forgetting happens; everything after it only
    /// reads.
    ///
    /// <para><b>And the SAME ground is the same afternoon, even across a lift-off.</b> A captain who pitches,
    /// declines, flies away and sets down in the same crater again is not offered the pitch a second time,
    /// and a Kolt who bolted from that crater is not standing at his post when they come back. That is
    /// deliberate and it is line three read literally: what the book expects of you is a signature on the
    /// NEXT moon, not another go at this one. The forgetting that matters — he never learns your face — is
    /// the one that happens when the ground changes, which is exactly what this fold is keyed on.</para>
    /// </summary>
    private void EnsureKoltsGround(string? ground)
    {
        if (string.Equals(_hardcaseGround, ground, StringComparison.Ordinal))
        {
            return;
        }

        _hardcaseGround = ground;
        _hardcaseCard = null;
        _hardcasePitched = false;
        _hardcaseRefused = false;
        _hardcaseFled = false;
        _hardcaseMoveOnAt = 0;
        _hardcaseStandingAt = null;

        if (ground is null)
        {
            _hardcaseWorkingHere = false;
            return;
        }

        _hardcaseWorkingHere = _hardcaseCheat
            ?? HardcaseRep.WorksThisGround(
                _activeThreadId ?? "", _surface?.Stop.Body.Id, _surface?.Site.Index ?? 0,
                _hardcaseGroundsWorked);
    }
}
