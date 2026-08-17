using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Quests — #727, THE MISSION THAT LEAVES THE SHIP. The one place that says what step a
// contract is currently on, what UI LEVEL can honour it, and where the ship must go if this one cannot; and
// the one place that advances a contract when the captain does it on their own two feet.
//
// Owner, 2026-08-06: "a mission that works outside the ship UI is something new … we should filter out /
// minimize ship-specific stuff to appropriate ('cannot do in this UI level, but high level: go to Moon X')
// type info in the carried mission UI."
//
// THE LAW: one mission model, two projections. The captain's chair (Map.Quests.Ledger.QuestCards) and the
// satchel's MISSIONS pane both read _quests, and both read the CURRENT STEP out of CurrentStepOf below —
// so the two views cannot come to disagree about the same journey, and the pane's foot-level lines are the
// chair's own words byte for byte rather than a second wording of them.
public partial class Map
{
    /// <summary>The ground the captain is actually standing on, as a body id — an excursion's landing body,
    /// else the station whose deck they are walking. Null in the chair. Ids, never names: the projection
    /// compares places, and a comparison on prose is the bug this whole lane is written against.</summary>
    private string? GroundUnderfootId() => _surface?.Stop.Body.Id ?? _dockedHavenId;

    /// <summary>A body id resolved to its display name, safe before the ephemeris is up.</summary>
    private string PlaceNamed(string? id, string fallback) =>
        string.IsNullOrEmpty(id) ? fallback : _ephemeris is null ? fallback : BodyName(id);

    /// <summary>#727 · THE CURRENT STEP OF ONE CONTRACT — its words, its UI level, the ground it is
    /// actionable on, and where the ship must go if this is not that ground. Read by BOTH projections: the
    /// captain's-desk quest card takes <c>Text</c> as its status label (it always was that label — this only
    /// gave it a level), and the carried compass takes the whole thing.
    ///
    /// <para>The level is read off the (kind, state) pair — the step's own KIND — never off its prose. A
    /// classifier that grepped the sentence for "dock" would be one authored line away from telling a captain
    /// in a basement to clamp.</para>
    ///
    /// <para><c>StatusKind</c> is the desk's css slug (active / complete / paid) and rides along because it
    /// is decided by the same pair; splitting it into a second switch is how two switches drift.</para></summary>
    private (MissionStep Step, string StatusKind) CurrentStepOf(Quest q)
    {
        string station = PlaceNamed(q.DestBodyId, q.TargetCallsign);
        return (q.Kind, q.State) switch
        {
            // A tip is settled the moment it is taken — there is no step, and it never reaches the compass.
            (QuestKind.Intel, _) =>
                (new MissionStep("🕸 Tip taken", MissionUiLevel.Berth, station), "paid"),

            // The fetch's first two legs are the chair's: the scope is a ship desk, and the pickup is a
            // proximity the ship earns by coasting (CheckFetchPickup). Nothing here is a boots verb.
            (QuestKind.Fetch, QuestState.Active) when q.SourceBodyId is { } src && IsBodyHidden(src) =>
                (new MissionStep("🔭 Work the tip — scan to find her", MissionUiLevel.Ship,
                    PlaceNamed(q.SourceBodyId, q.TargetCallsign)), "active"),
            (QuestKind.Fetch, QuestState.Active) =>
                (new MissionStep("▶ Fly to the roadster, prise the wallet", MissionUiLevel.Ship,
                    PlaceNamed(q.SourceBodyId, q.TargetCallsign)), "active"),
            // …and the last leg is boots: you walk to the Fixer's table and press E on him (DeliverFetch).
            (QuestKind.Fetch, QuestState.PickedUp) =>
                (new MissionStep($"📦 Carrying — hand off at {q.TargetCallsign}", MissionUiLevel.Foot,
                    station, q.DestBodyId), "active"),

            // The break-in is boots end to end: a keypad on a deck you walk, a shelf you reach, a table you
            // walk back to. This is the arc #727 was filed for.
            (QuestKind.Crack, QuestState.Active) =>
                (new MissionStep($"▶ Crack hatch {q.TargetShipId} — code {q.Pin}", MissionUiLevel.Foot,
                    PlaceNamed(q.SourceBodyId, station), q.SourceBodyId), "active"),
            (QuestKind.Crack, QuestState.PickedUp) =>
                (new MissionStep("📦 Package lifted — take it to The Fixer", MissionUiLevel.Foot,
                    station, q.DestBodyId), "active"),

            // The cache run digs on foot and delivers by berthing — one contract, one level each way.
            (QuestKind.FetchCache, QuestState.Active) =>
                (new MissionStep($"🗺 Dig at the X on {BodyName(q.SourceBodyId ?? "")}", MissionUiLevel.Foot,
                    PlaceNamed(q.SourceBodyId, q.TargetCallsign), q.SourceBodyId), "active"),
            (QuestKind.FetchCache, QuestState.PickedUp) =>
                (new MissionStep($"📦 Chest lifted — carry it to {q.TargetCallsign}", MissionUiLevel.Ship,
                    station), "active"),

            (QuestKind.Favor, QuestState.Active) =>
                (new MissionStep($"📡 Favor owed — quiet delivery to {q.TargetCallsign}", MissionUiLevel.Ship,
                    station), "active"),

            // Hunts and cargo runs: a prey to bring down, a parcel to berth with. Both the chair's.
            (_, QuestState.Active) =>
                (new MissionStep("▶ On the hook", MissionUiLevel.Ship,
                    q.DestBodyId is not null ? station : q.TargetCallsign), "active"),
            (_, QuestState.Complete) =>
                (new MissionStep("✓ Done — collect at any berth", MissionUiLevel.Ship, AnyBerth), "complete"),
            _ => (new MissionStep("💰 Paid", MissionUiLevel.Berth, station), "paid"),
        };
    }

    /// <summary>Where a finished contract is cashed: anywhere with a berth, which is a real destination and
    /// not a place, so it is named once here rather than typed into the collapse.</summary>
    private const string AnyBerth = "any berth";

    /// <summary>#727 · Every contract still owed, in the captain's-desk order (newest work on top), projected
    /// as the compass's input. NOTHING is filtered by KIND — a mission the pane quietly dropped would be the
    /// two-lists bug with better manners. Only settled work (turned in, paid) falls away, because a paid job
    /// is not a thing you are down here for.</summary>
    private IReadOnlyList<CarriedMission> LiveMissions()
    {
        var live = new List<CarriedMission>();
        foreach (Quest q in Enumerable.Reverse(_quests))
        {
            if (q.State == QuestState.TurnedIn)
            {
                continue;
            }

            (MissionStep step, _) = CurrentStepOf(q);
            live.Add(new CarriedMission(q.Title, q.Blurb, step));
        }

        return live;
    }

    /// <summary>#727 · The carried view: the SECOND projection of the one model. One line per live mission,
    /// the foot-level step spelled out where you stand, everything else collapsed to "⛵ return to the ship
    /// — next: X". Core decides all of that (MissionProjection); this hands it the facts and the ground.</summary>
    private IReadOnlyList<CompassLine> CompassOnFoot() =>
        MissionProjection.OnFoot(LiveMissions(), GroundUnderfootId());

    // ── THE ONE WRITER ──────────────────────────────────────────────────────────────────────────────────
    //
    // #727 · Arriving on foot completes a step the CHAIR issued, and it does it through the same door the
    // chair's own legs go through. Before this there were seventeen scattered `q.State = …` assignments and
    // seventeen private opinions about how to say so; a step finished in a basement and a step finished in
    // the chair were two features that happened to move the same enum.

    /// <summary>#727 + #736 · Advance one contract and SAY SO WHERE THE CAPTAIN IS LOOKING. The single writer
    /// every live transition goes through — chair-side and boots-side alike — so "the pane completed it" and
    /// "the desk completed it" cannot become two code paths that drift.
    ///
    /// <para>The beat rides <see cref="SayItWhereTheyAreLooking"/> rather than the HUD pulse, which is #736's
    /// law: a captain who finished this step standing in front of a card, a panel or their own open satchel
    /// would otherwise read the result through two millimetres of frosted glass. With nothing in front of
    /// them the seam falls through to the pulse, which is exactly right — a line about the world, said on the
    /// world.</para>
    ///
    /// <para>Idempotent by the caller's own match: every site tests the state it is moving OFF before it
    /// calls, so a per-tick check fires once.</para>
    ///
    /// <para><paramref name="beat"/> is null only where the transition's beat is a POP-UP of its own — the
    /// payout fanfare — because a card is already in front of everything a banner could hide behind.</para></summary>
    private void AdvanceMission(Quest q, QuestState to, string? beat = null)
    {
        q.State = to;
        if (beat is { Length: > 0 })
        {
            SayItWhereTheyAreLooking(beat);
        }
    }
}
