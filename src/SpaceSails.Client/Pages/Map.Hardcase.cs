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

    // ── One frame of his very bad day ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called once a frame from the surface tick, immediately after the pack has been stepped — so what he
    /// decides about is a field whose Old Ones have already moved this frame, exactly as the rep decides
    /// about a floor whose bodies have.
    /// </summary>
    private void AdvanceTheHardcase(double dtRealSeconds)
    {
        string? ground = TheGroundUnderfootForKolt();
        EnsureKoltsGround(ground);

        if (ground is null || !_hardcaseWorkingHere || !HeCouldBeAfootNow())
        {
            // Off the regolith — a lift ride, a hull, a berth. He does not follow you down a shaft, and a
            // body left in the excursion's band would be drawn standing in a corridor of B1. What he has
            // already DONE on this ground is remembered, because riding a lift is not leaving the crater.
            TakeKoltOffTheGround();
            return;
        }

        if (_surface is not { } ex)
        {
            return;
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        Walker? afoot = TheHardcaseAfoot(ex.Walkers);

        if (afoot is null)
        {
            if (_hardcaseCard is not null)
            {
                // His card cannot outlive his body.
                CloseTheHardcasesCard();
            }

            _ = SendTheHardcaseIn(ex, walls);
            return;
        }

        StepTheHardcase(ex, afoot, Math.Min(dtRealSeconds, 0.1), walls);
    }

    /// <summary>The walker that is him, if he is on the ground. By errand, because his plate is his own.</summary>
    private static Walker? TheHardcaseAfoot(IReadOnlyList<Walker> afoot)
    {
        foreach (Walker w in afoot)
        {
            if (w.For is Errand.HardcaseWaiting or Errand.HardcasePitching or Errand.HardcaseFleeing)
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>Take him off whatever ground he was on and forget where his feet were. Called the instant
    /// the captain is somewhere he cannot be.</summary>
    private void TakeKoltOffTheGround()
    {
        if (_surface is { } ex && TheHardcaseAfoot(ex.Walkers) is { } who)
        {
            ex.Walkers.Remove(who);
        }

        _hardcaseStandingAt = null;
    }

    /// <summary>
    /// PUT HIM ON THE GROUND, or move him along it. Written as a fall-through rather than a state machine
    /// because each clause is a reason the one under it does not apply: he has bolted, so nothing; he is due
    /// nowhere yet, so nothing; the captain is out here and worth crossing to, so he crosses; otherwise he
    /// stands at his post.
    /// </summary>
    private bool SendTheHardcaseIn(SurfaceExcursion ex, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (_hardcaseFled || ex.Walkers.Count >= WalkerBand || SimTime < _hardcaseMoveOnAt)
        {
            return false;
        }

        if (HeCrossesToTheCaptain(walls)
            && ThePostBesideTheCaptain(walls) is { } beside)
        {
            return PlanKolt(ex, walls, beside, Errand.HardcasePitching, NpcWalk.NoPersonalSpace);
        }

        if (HisPostOn(ex, walls) is not { } post || KoltIsAlreadyStandingAt(post))
        {
            return false;
        }

        // #1061 · HIS FIRST LEG BEGINS OFF TO THE SIDE, not on the spot it ends at. A walk of no length is
        // a teleport with a plate on it and the lattice has nothing to plan for it — so the one walk of a
        // visit that has no feet to start from starts a little further out along the apron, and the captain
        // who looks up sees a man WALKING UP rather than a man who was suddenly standing there.
        _hardcaseStandingAt ??= WhereHeComesFromOn(post, walls);

        return PlanKolt(ex, walls, post, Errand.HardcaseWaiting, NpcWalk.PersonalSpaceInRadii);
    }

    /// <summary>Where he was before he was anywhere: the same apron, further out along it, away from the
    /// tube's own column so his approach reads as coming in off the ground rather than out of the ship.
    /// Falls back to the post itself when the stone allows nowhere else — a man standing at his post is a
    /// worse beat than a man walking to it, and no beat at all is worse than both.</summary>
    private static DeckReachability.Point WhereHeComesFromOn(
        DeckReachability.Point post, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        double outward = post.X >= field.HomeX ? 1 : -1;
        double x = Math.Clamp(
            post.X + (outward * HardcasePostNearDu),
            field.LeftX + SurfaceLayout.EdgeMargin, field.RightX - SurfaceLayout.EdgeMargin);

        SpawnNudge.Result spot = SpawnNudge.Clear(x, post.Y, DeckPlan.AvatarRadius, walls);
        return spot.Failed ? post : new DeckReachability.Point(spot.X, spot.Y);
    }

    /// <summary>Is the captain somebody he can pitch to right now? Out of the tube, on the regolith, not
    /// already pitched at, not already told no — and near enough that crossing to them is a walk rather than
    /// an expedition.</summary>
    private bool HeCrossesToTheCaptain(IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        // #621 · THE CAPTAIN IS OUT OF THE TUBE — asked of AwayTeamSide through the page's own answer, and
        // never of the MOON'S top rim. That rim is y > −20, and a derelict's whole deck runs from −9 to +9,
        // so a moon constant asked here would read "safely aboard" on every square of every hull. It is the
        // named bug class this repository has shipped four times, and ADerelictIsNotAMoonTests caught this
        // lane trying to ship it a fifth.
        if (_hardcasePitched || _hardcaseRefused || CaptainBeyondReach)
        {
            return false;
        }

        if (_hardcaseStandingAt is not { } here)
        {
            return false;   // he has not set foot on the ground yet; the post comes first
        }

        double dx = here.X - _avatarX, dy = here.Y - _avatarY;
        return (dx * dx) + (dy * dy) <= HardcaseApproachRangeDu * HardcaseApproachRangeDu
               && SurfaceCollision.HasLineOfSight(here.X, here.Y, _avatarX, _avatarY, walls);
    }

    /// <summary>Where he stands to talk to you: at your elbow, on ground a body fits on. The captain's own
    /// interact reach, offset toward wherever he is coming from, and nudged clear by the same
    /// <see cref="SpawnNudge"/> every placement in this game goes through.</summary>
    private DeckReachability.Point? ThePostBesideTheCaptain(IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (_hardcaseStandingAt is not { } from)
        {
            return null;
        }

        double dx = from.X - _avatarX, dy = from.Y - _avatarY;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        if (len < 1e-6)
        {
            return null;
        }

        double reach = DeckPlan.AvatarRadius * NpcWalk.PersonalSpaceInRadii;
        SpawnNudge.Result spot = SpawnNudge.Clear(
            _avatarX + (dx / len * reach), _avatarY + (dy / len * reach), DeckPlan.AvatarRadius, walls);
        return spot.Failed ? null : new DeckReachability.Point(spot.X, spot.Y);
    }

    /// <summary>
    /// WHERE HE WAITS. A seeded spot on the fused apron, inside the landing band, off the tube's own column
    /// so he is never standing in the way home.
    ///
    /// <para>Deterministic in the ground: the same crater puts him in the same place on every machine and
    /// after a reload. The envelope is <c>MoonSurface.ExpeditionField</c>'s — the one the generator itself
    /// lays a site inside — so nothing here is a second opinion about where a moon ends.</para>
    /// </summary>
    private DeckReachability.Point? HisPostOn(
        SurfaceExcursion ex, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        string ground = HardcaseRep.GroundKey(ex.Stop.Body.Id, ex.Site.Index);
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();

        int side = DiceRule.Roll(DiceRule.Seed($"hardcase:post:side:{ground}"), 2).Face == 1 ? -1 : 1;
        int paces = DiceRule.Roll(
            DiceRule.Seed($"hardcase:post:out:{ground}"),
            (int)(HardcasePostFarDu - HardcasePostNearDu) + 1).Face - 1;

        double x = Math.Clamp(
            field.HomeX + (side * (HardcasePostNearDu + paces)),
            field.LeftX + SurfaceLayout.EdgeMargin, field.RightX - SurfaceLayout.EdgeMargin);
        double y = MoonSurface.SurfaceTopY - HardcasePostDepthDu;

        SpawnNudge.Result spot = SpawnNudge.Clear(x, y, DeckPlan.AvatarRadius, walls);
        return spot.Failed ? null : new DeckReachability.Point(spot.X, spot.Y);
    }

    /// <summary>Is he already where the next stop is? A walk of no length is a teleport with a plate on it.</summary>
    private bool KoltIsAlreadyStandingAt(DeckReachability.Point to) =>
        _hardcaseStandingAt is { } here
        && ((here.X - to.X) * (here.X - to.X)) + ((here.Y - to.Y) * (here.Y - to.Y))
           < DeckPlan.AvatarRadius * DeckPlan.AvatarRadius;

    /// <summary>Plan one of his walks. He sets off from his own feet once he is on the ground, and from his
    /// post the first time — which is what "he was already out here when you came down" means as a
    /// coordinate.</summary>
    private bool PlanKolt(
        SurfaceExcursion ex, IReadOnlyList<SurfaceCollision.Segment> walls,
        DeckReachability.Point to, Errand errand, double berth,
        double pace = NpcWalk.PaceDu)
    {
        DeckReachability.Point from = _hardcaseStandingAt ?? to;
        if (OnFoot(HardcaseRep.Plate, new NpcWalk.Bound("", to.X, to.Y), from, walls, berth, pace)
            is not { } walk)
        {
            // The ground does not connect the two. He is not placed at the far end anyway — that is the
            // reachability audit's own verdict, and the honest answer is that he is not there.
            return false;
        }

        ex.Walkers.Add(new Walker { Walk = walk, Table = -1, For = errand });
        _hardcaseStandingAt = from;
        StateHasChanged();
        return true;
    }

    // ── Stepping him ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// One frame of him. Two of his three errands end standing up (he arrives and then he is THERE); the
    /// third ends with him off the ground and nothing said about it.
    ///
    /// <para><b>The break is asked FIRST, on every frame, whatever he is doing.</b> A man who finished
    /// crossing the ground before he was allowed to notice what had crested the rim would be the sim waiting
    /// its turn to be frightened.</para>
    /// </summary>
    private void StepTheHardcase(
        SurfaceExcursion ex, Walker who, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (who.For != Errand.HardcaseFleeing && OneIsInHisSight(who.Walk.X, who.Walk.Y, walls))
        {
            HeBreaksAndRuns(ex, who, walls);
            return;
        }

        if (who.Walk.State != NpcWalk.Doing.Arrived)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            HeIsStandingAt(who);
            if (who.Walk.Afoot)
            {
                return;
            }

            if (who.Walk.State != NpcWalk.Doing.Arrived || who.For == Errand.HardcaseFleeing)
            {
                // The ground refused him, or the run is over. Either way he comes off it — and for the run
                // there is deliberately nothing else: no line, no card, no note.
                ex.Walkers.Remove(who);
                _hardcaseMoveOnAt = SimTime + HardcaseDwellSeconds;
                StateHasChanged();
                return;
            }

            if (who.For == Errand.HardcasePitching)
            {
                HeReachesYou();
            }
            else
            {
                _hardcaseMoveOnAt = SimTime + HardcaseDwellSeconds;
            }

            who.Walk.LookTowards(_avatarX, _avatarY);
            StateHasChanged();
            return;
        }

        // Standing. He is taken off the list the moment he is due somewhere else and put back on it by the
        // planner — the rep's own idiom, which is what keeps the next leg beginning at his feet instead of
        // out on the apron he first walked in from.
        if (SimTime >= _hardcaseMoveOnAt && HeIsDueSomewhereElse(who, walls))
        {
            HeIsStandingAt(who);
            ex.Walkers.Remove(who);
            StateHasChanged();
            return;
        }

        who.Walk.LookTowards(_avatarX, _avatarY);
    }

    /// <summary>Is the man standing there finished standing there? Two answers and they are the two halves of
    /// his day: at his post he is due to cross the moment the captain is out on the ground worth crossing to,
    /// and at the captain's elbow he is due back at his post the moment the card is down — because a man who
    /// has said his piece and been answered has no further business at your shoulder.</summary>
    private bool HeIsDueSomewhereElse(Walker who, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        who.For switch
        {
            Errand.HardcaseWaiting => HeCrossesToTheCaptain(walls),
            // …and BOTH halves are asked, because a card the arbiter is still holding behind somebody else's
            // scrim (#1052) has not gone up yet. A man who walked off while his own pitch was queued would
            // leave the beat to be raised over an empty patch of regolith.
            Errand.HardcasePitching => _hardcasePitched && _hardcaseCard is null,
            _ => false,
        };

    /// <summary>Remember where his feet are, so the next leg begins at them.</summary>
    private void HeIsStandingAt(Walker who) =>
        _hardcaseStandingAt = new DeckReachability.Point(who.Walk.X, who.Walk.Y);

    // ── The eyes, and the running ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>IS ONE OF THEM IN HIS SIGHTLINE?</b> Range and a clear line, and both of them are quoted rather
    /// than chosen: <see cref="HardcaseRep.SeesOneAtDu"/> is Core's own ruling about when an Old One stops
    /// being scenery, and the line is <see cref="SurfaceCollision.HasLineOfSight"/> — the identical call
    /// <c>StepReevers</c> makes in the other direction, over the identical collision field.
    ///
    /// <para>A DORMANT one counts. It is folded down in the dust and it has not moved in forty years, and
    /// that is exactly the thing you run from if you know what it is — which is the whole tell, and it is
    /// why no line may ever explain it.</para>
    /// </summary>
    private bool OneIsInHisSight(double x, double y, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        double r2 = HardcaseRep.SeesOneAtDu * HardcaseRep.SeesOneAtDu;
        foreach (Reever r in _reevers)
        {
            double dx = r.X - x, dy = r.Y - y;
            if ((dx * dx) + (dy * dy) <= r2
                && SurfaceCollision.HasLineOfSight(x, y, r.X, r.Y, walls))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <b>HE BREAKS.</b> The papers go, and he goes — flat out, on the lattice, at
    /// <see cref="HardcaseRep.DespairPaceDu"/>, to whichever end of the ground is furthest from the thing he
    /// has just seen.
    ///
    /// <para><b>Nothing is said and nothing is filed.</b> Not here, not at the far end of the run, not when
    /// he comes off the ground. The only record of it in the whole game is the sheet lying in the dust, and
    /// that sheet is about prices.</para>
    ///
    /// <para>If the lattice cannot get him anywhere he still drops the papers and still comes off the ground.
    /// A man who could not find a way out is not a man who stood politely still.</para>
    /// </summary>
    private void HeBreaksAndRuns(
        SurfaceExcursion ex, Walker who, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        _hardcaseFled = true;
        if (_hardcaseCard is not null)
        {
            CloseTheHardcasesCard();
        }

        TheScheduleFalls(ex, who.Walk.X, who.Walk.Y);

        DeckReachability.Point from = new(who.Walk.X, who.Walk.Y);
        ex.Walkers.Remove(who);
        _hardcaseStandingAt = from;

        if (TheFarEndOfTheGround(walls) is { } away
            && PlanKolt(ex, walls, away, Errand.HardcaseFleeing,
                        NpcWalk.NoPersonalSpace, HardcaseRep.DespairPaceDu))
        {
            return;
        }

        _hardcaseStandingAt = null;
        StateHasChanged();
    }

    /// <summary>Which way is away. The two ends of the landing band, and the one further from the nearest Old
    /// One wins — cheap, deterministic, and the same answer the captain would give looking at the same
    /// field.</summary>
    private DeckReachability.Point? TheFarEndOfTheGround(IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        double y = MoonSurface.SurfaceTopY - HardcasePostDepthDu;
        double left = field.LeftX + SurfaceLayout.EdgeMargin;
        double right = field.RightX - SurfaceLayout.EdgeMargin;

        double nearest = double.PositiveInfinity;
        double nearestX = field.HomeX;
        foreach (Reever r in _reevers)
        {
            double d = Math.Abs(r.X - field.HomeX);
            if (d < nearest)
            {
                nearest = d;
                nearestX = r.X;
            }
        }

        double pick = Math.Abs(left - nearestX) >= Math.Abs(right - nearestX) ? left : right;
        SpawnNudge.Result spot = SpawnNudge.Clear(pick, y, DeckPlan.AvatarRadius, walls);
        return spot.Failed ? null : new DeckReachability.Point(spot.X, spot.Y);
    }

    // ── The pitch ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>He is at your elbow. Held behind the scrim if something is already in front of the captain,
    /// for #1052's reason — his legs already brought him here, so the beat happened and must not be dropped;
    /// and asked again on the way out, because a captain who has walked back up the tube in the meantime is
    /// not standing there to be sold to.</summary>
    private void HeReachesYou() =>
        RaiseAScrimCard(KoltsPitchGoesUp, () => !CaptainBeyondReach && !_hardcaseFled);

    /// <summary>The pitch itself, once the glass is his — and the moment the ground goes in his book, because
    /// being found on a ground is what spends one of the two.</summary>
    private void KoltsPitchGoesUp()
    {
        _hardcasePitched = true;
        _hardcaseCard = HardcaseRep.OffersFor(_insurance.Tier);
        RememberHeWasFoundHere();

        // He is a relationship like every other name in the book, and the book knows him from the first
        // hello — even though he will not know you at the next one.
        _contacts.AddGoodwill(HardcaseRep.ContactId, HardcaseRep.DisplayName, 0);
        RendererInterop.PlayCue("reveal");
        StateHasChanged();
    }

    /// <summary>#1061 · The ground goes in the book that caps him at two, and the vault is asked to keep it.
    /// Written through <see cref="HardcaseRep.WithGroundWorked"/> so the cap is enforced and written by one
    /// function rather than by two that have to agree.</summary>
    private void RememberHeWasFoundHere()
    {
        if (_hardcaseGround is not { } ground)
        {
            return;
        }

        IReadOnlyList<string> book = HardcaseRep.WithGroundWorked(_hardcaseGroundsWorked, ground);
        if (book.Count == _hardcaseGroundsWorked.Count)
        {
            return;
        }

        _hardcaseGroundsWorked.Clear();
        _hardcaseGroundsWorked.AddRange(book);
        RequestVaultSave();
    }

    /// <summary>Take the card down. His body stays wherever it is standing; only the panel goes.</summary>
    private void CloseTheHardcasesCard()
    {
        _hardcaseCard = null;
        StateHasChanged();
    }

    /// <summary>
    /// THE CAPTAIN ANSWERS. One door for every button, so a move's meaning cannot drift from its words — and
    /// the buttons are <see cref="NebulaRep"/>'s, so a sale signed on a moon is the same sale signed in a
    /// concourse.
    /// </summary>
    private void AnswerTheHardcase(NebulaRep.RepMove move)
    {
        if (_hardcaseCard is null)
        {
            return;
        }

        switch (move)
        {
            case NebulaRep.RepMove.BuyBasic:
                SignWithKolt(InsuranceTier.Basic);
                break;

            case NebulaRep.RepMove.BuyPremium:
                SignWithKolt(InsuranceTier.Premium);
                break;

            default:
                TellKoltNo();
                break;
        }
    }

    /// <summary>
    /// NO — and he says the third line. It is pulsed rather than carded because the card is going: he has
    /// been answered, and the answer is the end of the conversation.
    ///
    /// <para>He does not come back at the captain on this ground. That is line three read literally: the
    /// book's expectation is about the NEXT moon, not about asking again on this one.</para>
    /// </summary>
    private void TellKoltNo()
    {
        _hardcaseRefused = true;
        ShowPulseMessage(HardcaseRep.OnRefusal);
        CloseTheHardcasesCard();
        _hardcaseMoveOnAt = SimTime + HardcaseDwellSeconds;
    }

    /// <summary>A SALE, through the one seam a policy is ever set by.</summary>
    private void SignWithKolt(InsuranceTier tier)
    {
        int price = NebulaRep.PremiumFor(tier);
        if (_credits < price)
        {
            // The page's own voice and not his. He has no line for a captain who cannot pay, and putting
            // words in his mouth here would be the one place in this beat a hardcase got a fourth sentence.
            ShowPulseMessage("Not enough credits for that premium.");
            return;
        }

        _credits -= price;
        _insurance = NebulaRep.PolicyAfterBuying(tier, SimTime);
        _contacts.ApplyCredit(
            HardcaseRep.ContactId, HardcaseRep.DisplayName,
            new CreditTransaction(CreditKind.Premium, 0, SimTime, $"{tier} premium · {price} cr"));
        _contacts.AddGoodwill(HardcaseRep.ContactId, HardcaseRep.DisplayName, 1);
        LogAutopilotEvent(HardcaseRep.SaleLedgerNote(tier, price, ActiveCaptainName));
        RequestVaultSave();

        _hardcaseCard = HardcaseRep.OffersFor(_insurance.Tier);
        StateHasChanged();
    }

    // ── The sheet in the dust ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The papers go where his feet were. Once per excursion — he only ever breaks once — and the deck is
    /// rebuilt so the mark is on the ground the same frame he stops being on it.
    ///
    /// <para><b>It is kept on the EXCURSION and NOT in <see cref="LeftBehind"/>.</b> That store is the
    /// obvious home for a thing lying on a floor, and every sentence it prints says <i>"where YOU left
    /// it"</i> (<c>LeftBehind.ReachPrompt</c>, <c>FoundAgainLine</c>). Printing those over somebody else's
    /// dropped paperwork would be the game reporting one world while the sim held another — this
    /// repository's third named bug class, in the one register this whole beat is about.</para>
    ///
    /// <para>Excursion-scoped for the store's own #688 reason, though: the world does not keep a ledger of
    /// every piece of paper anybody has ever shed on a moon, and a schedule still lying in the dust three
    /// visits later would be the permanence that ruling deliberately declined. Within the walk it is exactly
    /// where it fell.</para>
    /// </summary>
    private void TheScheduleFalls(SurfaceExcursion ex, double x, double y)
    {
        if (ex.HardcaseDrop is not null || ex.HardcaseScheduleTaken)
        {
            return;
        }

        ex.HardcaseDrop = new DeckReachability.Point(x, y);
        RebuildSurfaceDeck();
    }

    /// <summary>
    /// #1061 · THE MARK ON THE REGOLITH — the sheet, drawn where it fell, as a <c>ViewObject</c> console.
    ///
    /// <para>The appended-region idiom (#698's own, and the hidden door's and the outpost hut's before it):
    /// no walls, no collision, nothing a captain can be pinned against. It carries the plate the sheet is
    /// called by, so the [E] offer over it, the head of the card it opens and the row it becomes in the
    /// sleeve are the same four words.</para>
    /// </summary>
    private void ComposeTheDroppedSchedule(SurfaceExcursion ex)
    {
        if (ex.HardcaseScheduleTaken
            || ex.HardcaseDrop is not { } spot
            || ex.Floor != HardcaseRep.SurfaceFloor)
        {
            return;
        }

        _deckPlan.AppendRegion(new DeckPlan.DeckRegion(
            [],
            [new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)spot.X, (float)spot.Y,
                HardcaseRep.ScheduleLabel)],
            [],
            []));
    }

    /// <summary>
    /// #1061 · <b>PICKING IT UP.</b> Recognised by the console's own plate, the way the head office's beats
    /// and the insurance poster already are, so this lane adds no dispatch of its own.
    ///
    /// <para>Three things happen and they happen in the order #678's law requires: the sleeve is asked
    /// FIRST, and a sleeve that will not take it leaves the sheet lying exactly where it is and says so in
    /// Core's own words. Only once it has actually gone in does the card go up and the book get its
    /// entry.</para>
    ///
    /// <para><b>The entry is the payoff.</b> It is filed with two subjects — the letterhead and the ground —
    /// so it lands on the same red-pen thread as everything else the captain has written down about Nebula
    /// Mutual (#898). The schedule prices what it never names, and the book is where that becomes
    /// legible.</para>
    /// </summary>
    /// <returns>Whether this press was the sheet's.</returns>
    private bool TryTheDroppedSchedule(SurfaceExcursion ex, string label)
    {
        if (!string.Equals(label, HardcaseRep.ScheduleLabel, StringComparison.Ordinal)
            || ex.HardcaseScheduleTaken)
        {
            return false;
        }

        var sheet = new Core.Satchel.Item(Core.Satchel.Kind.Paper, HardcaseRep.ScheduleFindId);
        if (!Core.Satchel.CanTake(_satchel, sheet))
        {
            ShowPulseMessage(UndergroundComplex.PocketFullLine.Trim());
            return true;
        }

        _satchel = [.. Core.Satchel.Add(_satchel, sheet)];
        ex.HardcaseScheduleTaken = true;
        ex.HardcaseDrop = null;

        CarriedObject.Reveal read = CarriedObject.PaperReveal(HardcaseRep.ScheduleFindId);
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            read.Label, read.ArtUrl, read.Story, UndergroundComplex.PaperPocketLine.Trim());

        FileNoteAbout(
            HardcaseRep.ScheduleBody, HardcaseRep.ScheduleGlyph,
            HardcaseRep.ScheduleSubjects(ex.Site.Name));

        RebuildSurfaceDeck();
        RequestVaultSave();
        StateHasChanged();
        return true;
    }

    // ── The vault ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The rows the file keeps, or null when he has never been found anywhere — see
    /// <see cref="InsuranceWeatherSection.Hardcase"/> for why an empty list is not written.</summary>
    private IReadOnlyList<string>? TheGroundsKoltHasWorked() =>
        _hardcaseGroundsWorked.Count == 0 ? null : [.. _hardcaseGroundsWorked];

    /// <summary>Read them back, tolerantly: a blank row is dropped and the cap is re-applied on the way in,
    /// so an edited file cannot hand this build a third moon.</summary>
    private void RestoreTheGroundsKoltHasWorked(IReadOnlyList<string>? rows)
    {
        _hardcaseGroundsWorked.Clear();
        foreach (string row in rows ?? [])
        {
            if (string.IsNullOrWhiteSpace(row))
            {
                continue;
            }

            IReadOnlyList<string> book = HardcaseRep.WithGroundWorked(_hardcaseGroundsWorked, row);
            _hardcaseGroundsWorked.Clear();
            _hardcaseGroundsWorked.AddRange(book);
        }

        // A load is a different universe arriving; whatever he was doing on the last one is not a fact
        // about this one.
        _hardcaseGround = null;
        _hardcaseWorkingHere = false;
        _hardcaseCard = null;
        _hardcaseStandingAt = null;
    }
}
