using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.BarWalkers (the header note lives in Map.BarWalkers.cs) — #973 L5b · THE TOP THE CAPTAIN
// TAKES, and the one question the eighth seat asks. Which top he is standing on, taking it, the boot
// cheat that sits him at one, and `TheCaptainIsSittingAloneInTheBar` — the predicate the room's own
// approaches are gated on, because sitting alone at a bar top is a choice to be findable BY THIS ROOM.
public partial class Map
{
    /// <inheritdoc cref="Seating.TryTakeBarTop"/>
    private bool TryTakeBarTop() => _seating.TryTakeBarTop();

    // ── #973 L5b · THE EIGHTH SEAT'S ONE QUESTION ────────────────────────────────────────────────────────
    //
    // THE ANSWER'S TYPE LIVES HERE AND NOT IN `ISeatHost.cs`, and the reason is the ratchet itself: the guard
    // that counts what a chair asks the page for reads that file a DECLARATION at a time, and a positional
    // record struct is spelled exactly like a method. Declared next door it counted as a thirty-third member
    // the seat had never asked for. It is a nested type of the page either way, so the seat still names it
    // bare — where a type is declared changes nothing about who can see it.

    /// <summary>
    /// #973 L5b · A TOP IN A DOCKED STATION'S BAR, AS THE SEAT NEEDS IT — the page's whole answer to "what
    /// did that [E] land on", and nothing behind it.
    ///
    /// <para>It is an ANSWER and never machinery, which is the rule <see cref="ISeatHost"/>'s own summary
    /// states: the deck, the room's published tops and the stone the chair is sounded against are all things
    /// the PAGE owns, and a chair handed a lattice is exactly what lane 6c took away. Everything about the
    /// SITTING — which scene it is, whether it reads relaxed, how many chairs are left — the seat decides
    /// from these six facts, the same way it decides them from a <c>CanteenRegulars.TableSeat</c>.</para>
    /// </summary>
    /// <param name="Index">The top's ordinal in the room's own list.</param>
    /// <param name="Key">What every fact about this sitting is keyed on: the berth, the frozen docking watch
    /// and the ordinal. A berth has no excursion and no canteen watch, so it cannot be the Hive's key — and a
    /// key that could collide with one would file two rooms' business in one drawer.</param>
    /// <param name="Watch">The frozen docking watch, for the one question the SCENE asks of a clock: whether
    /// a sit at this hour reads relaxed (<c>SittingAlone.SitReadsAsRelaxed</c>).</param>
    /// <param name="ChairX">Where the body goes — Core's own sounding against the room's own stone
    /// (<c>HavenInterior.BesideATop</c>), never a coordinate the seat measured (§13.15).</param>
    /// <param name="ChairY"><inheritdoc cref="ChairX"/></param>
    /// <param name="Seats">How many the top seats — the room's own number.</param>
    /// <param name="Setting">Where the captain is sitting, in the scene's own words — the one clause the
    /// strip's company line is built out of. A canteen's setting is a constant; a berth's is per-station and
    /// the ship's are her own two rooms, so the finished sentence travels with the answer rather than being
    /// reassembled by a chair that would have to know which building it is in.</param>
    /// <param name="Plate">#1016 · What the panel calls this seat — <c>YOUR OWN TABLE</c> at a top,
    /// <c>YOUR OWN DESK</c> at the one in the captain's berth. Carried for the same reason
    /// <paramref name="Setting"/> is: it is the ROOM's word for its own furniture.</param>
    /// <param name="Quiet">#1016 · Whether this seat is behind a door — the one fact the exposure ladder
    /// reads (<c>Seating.SeatedIn</c>), and therefore whether the case may be spread here unconditionally. A
    /// station bar is one loud room with a window in it and answers false; a cabin has a leaf a step away
    /// and answers true.</param>
    /// <param name="Aboard">#1016 · Whether this seat is on the captain's OWN SHIP rather than ashore. Two
    /// things hang off it and nothing else does: nobody ever crosses the floor to it (there is nobody
    /// aboard to do the crossing), and the silence when you wait is the boat's own rather than a hall's.</param>
    /// <param name="Stool">#1040 · Whether this seat is a STOOL AT A COUNTER rather than a top or a desk —
    /// the one fact that moves the sitting onto the <c>BarStool</c> rung of the exposure ladder, where the
    /// gumshoe rule refuses the spread out loud. Owner: <i>"Our on ship bar can be upgraded to match the
    /// other bars."</i> It is carried and never derived from a plate or a room name, for the reason every
    /// other field on this record is: the ROOM knows what its furniture is, and a chair does not.</param>
    private readonly record struct BarTopUnderfoot(
        int Index, string Key, long Watch, double ChairX, double ChairY, int Seats, string Setting,
        string Plate, bool Quiet, bool Aboard, bool Stool = false);


    /// <summary>
    /// #973 L5b · <b>WHICH BAR TOP THAT [E] LANDED ON, AND WHERE A BODY SITS AT IT.</b> The page's whole
    /// answer to the eighth sitting site (<c>Seating.BarTop.cs</c>), and the one member #973 L5b added to
    /// <see cref="ISeatHost"/>.
    ///
    /// <para>The shape is <c>TryTakeTable</c>'s, one room over: find the console the press landed on, match
    /// it back against the room's OWN published list rather than against anything measured here, and hand
    /// back the ordinal. What is different is what a berth does not have — no excursion, no canteen watch, no
    /// <c>CanteenRegulars.Tables</c> — so the key is the bar's own (berth · docking watch · ordinal) and the
    /// chair is <see cref="HavenInterior.BesideATop"/>'s, the same sounding a walker crossing to this top
    /// uses.</para>
    ///
    /// <para><b>The chair is sounded with nobody excluded, and that is the right way round.</b> The captain
    /// is the FIRST body at this top — the woman who crosses the floor to it afterwards is the one who has to
    /// be told about him, which is what <see cref="BesideThisTopClearOfTheCaptain"/> is for.</para>
    ///
    /// <para><b>#1016 · ONE MEMBER, THREE ROOMS.</b> Owner, on 7 Deck: <i>"Why no table here to sit at?"</i>,
    /// <i>"Why no table in cabin either?"</i>, <i>"I expect to have a bar table like this in this ships
    /// galley also.... feature complete."</i> Her cantina tops and her cabin desk are the same VERB as a top
    /// in a station bar, so they are answered by the same member rather than by a ninth thing a chair asks
    /// the page for — the ratchet on <see cref="ISeatHost"/> says the list may only shrink, and this lane had
    /// no argument for growing it. The ship's half of the answer lives next door in
    /// <c>Map.ShipSeats.cs</c>.</para>
    ///
    /// <para><b>And the two rooms can be on ONE DECK at the same time</b>, which is why the fall-through is
    /// written the way it is rather than as an <c>else</c>. A docked complex is welded onto the ship's own
    /// plan and keeps every console she has (<c>HavenInterior.BuildComplex</c> seeds itself from
    /// <c>DeckPlan.Ship.Consoles</c>), so a captain clamped on can walk down the tube and sit in his own
    /// cantina — and a press there is a <c>BarTop</c> that matches no top the BAR published. Answering null
    /// on that would be the ship's seats going dead the moment she docked, which is the one state a player
    /// would find in the first minute.</para>
    /// </summary>
    private BarTopUnderfoot? TheBarTopUnderfoot()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { } spot
            || spot.Kind is not (DeckPlan.ConsoleKind.BarTop or DeckPlan.ConsoleKind.ShipDesk
                                 or DeckPlan.ConsoleKind.ShipStool))
        {
            return null;
        }

        if (spot.Kind == DeckPlan.ConsoleKind.BarTop && TheDockedBar() is { } bar)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
            for (int i = 0; i < bar.Tops.Count; i++)
            {
                DeckReachability.Point top = bar.Tops[i];
                if (Math.Abs(top.X - spot.X) >= 0.5 || Math.Abs(top.Y - spot.Y) >= 0.5)
                {
                    continue;
                }

                // No place at it, so there is no seat here — answered as an absence rather than by sitting
                // the captain down inside the counter, which is §13.15's own sentence about measured
                // coordinates.
                if (BesideThisTop(top, walls) is not { } chair)
                {
                    return null;
                }

                return new BarTopUnderfoot(
                    i, $"bar:{bar.BodyId}:{BarWatch}:{i}", BarWatch, chair.X, chair.Y,
                    HavenInterior.BarTopSeats,
                    SittingAlone.BarSetting(HavenInterior.BarNameOf(bar.BodyId) ?? ""),
                    SittingAlone.OwnTablePlate,
                    // A station bar is one loud room with a window in it: no cabinets, no curtains, nothing
                    // to dog. And it is ashore, so the room's own people can and do cross the floor to it.
                    Quiet: false, Aboard: false);
            }
        }

        return TheShipsOwnSeatUnderfoot(spot);
    }

    /// <summary>#1016 QA · <c>?barcase=1</c> — the owner's own bug, in one URL. Set in Map.Sim's cheat
    /// parse, which turns <c>?ashore=1</c> on with it: a case worked in a bar needs a bar to be standing
    /// in.</summary>
    private bool _barCaseCheat;

    /// <summary>
    /// #1016 QA · <b>SIT THE CAPTAIN AT A TOP IN THE DOCKED BAR, WITH PAPERS IN THE SLEEVE.</b>
    ///
    /// <para>The exact seat the owner filed this issue from — a takeable top in a station bar, a sleeve with
    /// something in it, and the strip's <b>Work the case</b> button one press away. Until #973 L5b there was
    /// no such seat to boot into, and until this issue there was nothing behind the button when you got
    /// there; the shortest honest route to the bug was launch, dock, walk ship → airlock → tube →
    /// immigration hall → bar, find a free top, and already be carrying paperwork off a moon. That is not a
    /// route anybody re-runs, which is a large part of why a dead button survived a whole lane.</para>
    ///
    /// <para>It walks the LAST leg only, and through the room's own verb: the tops are the bar's published
    /// list, the chair is <c>HavenInterior.BesideATop</c>'s sounding (asked inside <c>TheBarTopUnderfoot</c>,
    /// never measured here), and the sit itself is <c>Seating.TryTakeBarTop</c> — the same [E] a player
    /// presses. A cheat that assembled its own sitting would be demonstrating a seat that does not ship,
    /// which is this repo's first named bug class wearing a dev row.</para>
    ///
    /// <para>Called straight after the ashore walk, which is what puts a deck under the captain's feet.</para>
    /// </summary>
    private void SitAtABarTopIfAsked()
    {
        if (!_barCaseCheat)
        {
            return;
        }

        if (TheDockedBar() is not { } bar || bar.Tops.Count == 0)
        {
            ShowPulseMessage(
                "🧪 DEV ?barcase=1: this berth has no bar with tops in it. Try &dock=the-space-bar.");
            return;
        }

        SeedTheSpreadFinds();

        // A bar top is drawn and does not collide (#973 L5b), so the console under the captain's feet is the
        // one they are standing on — which is exactly the question [E] asks of the room.
        foreach (DeckReachability.Point top in bar.Tops)
        {
            StandCaptainAt(top.X, top.Y, "you stop at a free top");
            if (TryTakeBarTop())
            {
                ShowPulseMessage(
                    "🧪 DEV ?barcase=1: sat at a top in "
                    + (HavenInterior.BarNameOf(bar.BodyId) ?? "the bar")
                    + " with three finds in the sleeve — and NO excursion under you (#1016). Press \"Work "
                    + "the case\" on the strip, then dig a paper: the bar fills on the strip, the book takes "
                    + "the entry, and the register remembers it across a save.");
                return;
            }
        }

        ShowPulseMessage("🧪 DEV ?barcase=1: no top in this room had a place at it to stand.");
    }

    /// <summary>
    /// #973 L0 · THE CAPTAIN, ALONE, AT A TOP IN THIS BAR — the seat family's own two answers, asked and never
    /// re-derived.
    ///
    /// <para><b>It is false at every berth in the game today, and that is not a bug in this line.</b> There is
    /// no way to sit down in a docked bar: all seven sitting sites are gated on a <c>SurfaceExcursion</c> and a
    /// berth has none. So Fess works his beat ashore and never pitches, which is honest — a salesman
    /// teleporting a card at a captain who cannot sit down is the opposite of the practice the owner asked
    /// for. When the haven bar grows a top the captain can take, this one predicate starts answering true and
    /// the rest of the walk is already built.</para>
    ///
    /// <para><b>#1016 · AND A SEAT ON YOUR OWN BOAT IS NOT A SEAT IN THIS BAR.</b> A docked complex is the
    /// ship's own plan with a station welded onto it, so a captain clamped on can walk down the tube and take
    /// a top in his own cantina — and every other clause here would say yes to that. The two walkers who read
    /// this predicate are both gated on <see cref="InTheBar"/> as well and would not have moved; the flag is
    /// added anyway, because a predicate that is only right because of where its callers happen to be checked
    /// is the shape of a bug rather than of a law. Sitting alone is a choice to be findable BY THIS ROOM.</para>
    /// </summary>
    private bool TheCaptainIsSittingAloneInTheBar() =>
        CaptainIsSeated && SeatedAlone
        && SeatedTable is { Bench: false, Office: false, Aboard: false };
}
