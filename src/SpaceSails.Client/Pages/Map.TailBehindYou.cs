using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// #1062 slice 2 · LOSING YOUR OWN TAIL — the mirror of Map.ObservationWalk.cs, in the same room, on the
// same feet, and with none of the same machinery pointed the same way.
//
// Owner, 2026-09-01, verbatim: "following one of the customers covertly without them noticing us would be
// classic spy / detective stuff :-D … or trying to lose a tail our selves :-D"
//
// WHAT IS HERE. A man who comes into the bar after the captain, takes a place he can see him from, and orders
// nothing. The two ways a captain can find that out — a chair with the door in front of it, and the same coat
// through two doorways — and the one way to be rid of him, which is to spend as long out of his sight as it
// took to notice him.
//
// #1229 · AND "A PLACE" IS TWO PLACES, because the brief said so from the start and only half of it shipped:
// "enters the room after the captain, keeps a distance band, TAKES A SEAT/STANDING SPOT WITH A SIGHTLINE TO
// HIM, orders nothing." In a room off the ring he takes a POST — inside it, against the room's own stone,
// with a line, nearest the doorway he came in by, never the counter and never a chair. Out on the ring he
// keeps the BAND, unchanged. The room chooses, by its own published walls, and the reason it has to is
// measured: a bar reaches about twenty-three deck units from its doorway and the band's far edge is thirty,
// so a man told to hold a band in one is a man the stone pushes out through the door — which is exactly what
// #1231 watched happen, three seconds at a time.
//
// WHAT IS DELIBERATELY NOT. No pathfinder (OnFoot, the one planner). No sightline (FootTail.InPlainSight →
// PatrolBeat.EyesOn → SurfaceCollision.HasLineOfSight, the one oracle, which is also the one slice 1 uses).
// NO DICE AT ALL — every question in this file is arithmetic on a range, a clock and a count of doorways,
// which is #1062's own law for this half. No new save field: whether anybody is behind you is read off
// #715's folder, which already rides the vault.
//
// AND NOTHING IS ANNOUNCED UNTIL IT IS EARNED. Until the captain has noticed him, this file says nothing at
// all: no pulse, no card, no badge, no book entry. The only thing in the game that gives him away is the
// figure on the floor, which is where a gumshoe's evidence is supposed to be.
public partial class Map
{
    // ── THE FACTS, AND NOT ONE OF THEM RIDES THE SAVE ────────────────────────────────────────────────────
    //
    // A visit's own state, exactly like the bar's feet and slice 1's tail: a different berth is a different
    // room, and a man carried across a casting-off would be somebody standing in a station he was never in.
    // WHETHER he is here at all is not kept here — it is read off the outfit's folder every frame, so there
    // is nothing to persist and nothing to migrate.

    /// <summary>#1062 · Which berth this man belongs to. Null is a room that has never had one.</summary>
    private string? _coatBerth;

    /// <summary>#1062 · Whether this visit has already put him on the floor, so a room that refused the walk
    /// is not asked again sixty times a second and a man who has come in once does not come in again.</summary>
    private bool _coatDealt;

    /// <summary>#1062 · Has the captain worked out that he is being followed? One-way for the visit: a thing
    /// you have noticed is not un-noticed by arithmetic on the next frame. It is the latch every player-facing
    /// word in this file is gated on.</summary>
    private bool _coatSeen;

    /// <summary>#1062 · Seconds of having him in front of a chair that faces the door
    /// (<see cref="TheTailBehindYou.NoticedFromTheChair"/>). Reset the moment any clause of the sit stops
    /// holding — the reading is about sitting there, not about having once sat there.</summary>
    private double _coatExposure;

    /// <summary>#1062 · …and the mirror clock: seconds he has had nothing to look at. The one that loses
    /// him.</summary>
    private double _coatBlind;

    /// <summary>#1229 · …and the third: seconds the captain has been in a DIFFERENT ROOM from him. A man
    /// posted inside a room does not leave it on the frame his subject does — he gives it one look
    /// (<see cref="TheTailBehindYou.SecondsBeforeHeFollowsYouOut"/>) and then comes out through the same
    /// doorway, which is what makes the two-door tell a SEQUENCE the captain reads rather than an accident of
    /// where the man happened to be planted beforehand. Zeroed the moment they are in one room again.</summary>
    private double _coatARoomBehind;

    /// <summary>#1229 · Is he on a POST — a standing place against the room's own stone — rather than keeping
    /// a band? Written on the frame he is SENT to one, so it is a fact about the man and not a re-derivation
    /// of the room he happens to be in this instant. A post is kept until the LINE breaks; a band is kept
    /// until the captain walks out of it. Two behaviours, one field to tell them apart.</summary>
    private bool _coatPosted;

    /// <summary>#1229 · Which doorway he last came through, by its index in the plan — the way OUT, from where
    /// he is standing. It is what a band is measured against (he keeps himself between the captain and it) and
    /// what a post is measured to (nearest it). Null until he has been in one, which is the bar's own
    /// threshold by default. A visit's state, like everything else in this file.</summary>
    private int? _coatCameInBy;

    /// <summary>#1062 · Which of the deck's own doorways he has been seen coming through, by their index in
    /// the plan. A SET, because the tell is two DISTINCT doors and a man loitering in one of them for a whole
    /// watch is a man in a doorway.</summary>
    private readonly HashSet<int> _coatDoors = [];

    /// <summary>#1062 · Whether he has given up and is on his way off the floor. Written once; the walk out is
    /// an ordinary route and ends the ordinary way.</summary>
    private bool _coatLost;

    /// <summary>#1062 · <b>WHERE HE LAST HAD THE CAPTAIN.</b> The only thing he knows when the stone comes
    /// between them, and the only place a man who has lost you has any reason to walk to. NaN is a man who
    /// has not had you yet.</summary>
    private double _coatLastX = double.NaN;

    /// <summary>#1062 · <inheritdoc cref="_coatLastX"/></summary>
    private double _coatLastY = double.NaN;

    /// <summary>#1062 · Which ports this VISIT burned, so a burn is never told to the captain in the same
    /// breath as the act that made it. The burn itself is durable (it is a tag in the register that already
    /// rides the vault); this is only "not yet", and it is forgotten on casting off — which is exactly what
    /// <i>the next time he comes back</i> means, and is why the durable half needs no visit stamp in it.</summary>
    private readonly HashSet<string> _coatBurnedThisVisit = new(StringComparer.Ordinal);

    /// <summary>#1062 QA · <c>?tailed=1</c> — put a man behind the captain at this berth whatever the folder
    /// says. Set in the cheat parse. Null is "ask the world", which is what a captain gets.</summary>
    private bool? _tailedCheat;

    // ── IS ANYBODY BEHIND YOU ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>THE TRIGGER, AND IT IS SOMEBODY ELSE'S BOOK.</b>
    ///
    /// <para>Read off #715's folder for whoever runs this berth (<see cref="IllegalHeat.HeatAtSite"/> — the
    /// one call every effect in this game asks that question with), at the band where an outfit stops
    /// treating a hull as paperwork and starts wanting a face. Plus slice 1's own failure: a captain who was
    /// CLOCKED following one of this bar's regulars has advertised what he does for a living, and the evening
    /// answers.</para>
    ///
    /// <para><b>The audit's correction, recorded.</b> #1062 offers #804's suspicion ladder as the other
    /// candidate, and ashore there is no such thing: that ladder is an escort count on a patrolled floor of
    /// an underground complex, it is spawned and stepped only from a surface excursion, and a berth has
    /// neither. What a berth does have is the folder — which is the better trigger anyway, because it is the
    /// only one of the three that is written down somewhere a captain can do something about.</para>
    /// </summary>
    private bool TheCoatIsBehindYou(string berth) =>
        _tailedCheat
        ?? TheTailBehindYou.Follows(IllegalHeat.HeatAtSite(_contacts, berth), _walkNoticed);

    // ── ONE FRAME ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · One frame of the man behind you. Called from <c>AdvanceBarWalkers</c> beside the room's own
    /// metabolism, and it does nothing at all at a berth whose outfit has nothing written down.
    ///
    /// <para>Three refusals before anything happens, in the order that costs least: nobody is owed a look at
    /// this captain; he has already been lost (once is once — a man who has been shaken does not come back
    /// this visit); the captain is not in the room yet, because the whole shape of the beat is that he comes
    /// in AFTER you.</para>
    /// </summary>
    private void AdvanceTheCoat(in HavenInterior.BarFloor bar)
    {
        if (_coatLost || !TheCoatIsBehindYou(bar.BodyId))
        {
            return;
        }

        if (_coatDealt || !InTheBar(in bar))
        {
            return;
        }

        if (_barAfoot.Count >= WalkerBand)
        {
            // A full band is NOT NOW rather than NO — the room's own leavers hold slots for a few seconds at
            // a time and then give them back. Marking him dealt here would let a busy instant cancel the
            // whole thing for the visit, which is slice 1's lesson read straight across.
            return;
        }

        // He comes in the way you came in — the room's own published doorway, never a coordinate typed into a
        // client file. #1229 · and he is dealt on the CONCOURSE SIDE of it (HavenInterior.TheDoorstepOutside
        // TheBar) and walks in, because HavenInterior.BarThreshold is where `?ashore=1` stands the CAPTAIN: a
        // man dealt there is dealt on the captain's feet, and a tail standing on your toes on the frame you
        // boot is not a tail. He comes in AFTER you, which is the whole shape of the beat, and now the code
        // says so as well as the docblock.
        (double outsideX, double outsideY) = HavenInterior.TheDoorstepOutsideTheBar;
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        var doorstep = new DeckReachability.Point(outsideX, outsideY);

        _coatDealt = true;

        if (WhereHeStands(in bar, walls) is not { } spot
            || OnFoot(TheTailBehindYou.Plate, new NpcWalk.Bound("", spot.X, spot.Y), doorstep, walls)
               is not { } walk)
        {
            return;   // the stone allows him nowhere to stand, or there is no route. Nobody comes in.
        }

        _barAfoot.Add(new Walker { Walk = walk, Table = -1, For = Errand.BehindYou, Who = "" });
        StateHasChanged();
    }

    /// <summary>#1062 · CASTING OFF IS THE ROOM FORGETTING, here as everywhere else on this deck — called
    /// from <c>ForgetTheBarsFeet</c>, the one place that knows a berth has changed. An exposure clock carried
    /// across a casting-off would be a captain half-way to noticing a man at a station he has left.</summary>
    private void ForgetTheCoat(string? berth)
    {
        if (_coatBerth == berth)
        {
            return;
        }

        _coatBerth = berth;
        _coatDealt = false;
        _coatSeen = false;
        _coatExposure = 0;
        _coatBlind = 0;
        _coatARoomBehind = 0;
        _coatPosted = false;
        _coatCameInBy = null;
        _coatLost = false;
        _coatLastX = double.NaN;
        _coatLastY = double.NaN;
        _coatDoors.Clear();
        _coatBurnedThisVisit.Clear();
    }
}
