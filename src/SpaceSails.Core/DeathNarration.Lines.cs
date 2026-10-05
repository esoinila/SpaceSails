namespace SpaceSails.Core;

/// <summary>
/// #251 · WHICH LINE IS SAID — the pool for a cause, whether a cause can happen at a place, the tail, and
/// the seeded draw of the house-voice line.
///
/// <para>Split out of <c>DeathNarration.cs</c> under #251 as a pure move: two runs of the base file, no
/// member renamed, re-scoped or re-ordered. Every line pool is a <c>static readonly</c> and stays in the
/// opening file in its original order, including the two below-ground pools that sat between these runs
/// (#1163).</para>
/// </summary>
public static partial class DeathNarration
{
    private static string[] PoolFor(DeathCause cause) => cause switch
    {
        DeathCause.Scuttled => ScuttledLines,
        DeathCause.Collector => CollectorLines,
        DeathCause.Impact => ImpactLines,
        DeathCause.Reevers => ReeverLines,
        DeathCause.Joined => JoinedLines,
        DeathCause.Void => VoidLines,
        DeathCause.Suffocated => SuffocationLines,
        // #633 · Scuttled is listed ONCE, at the top of this switch, and it resolves to her own ship's pool.
        // That is the placeless answer and it is the right one now: the derelict branch of `Line(cause,
        // place, …)` catches a wreck scuttling before it ever reaches here, so what is left over is her
        // deck. (Before the branches were reunited this arm read `ScuttleLinesAboardAWreck`, because on that
        // side her ship had no such switch. Main built the switch. A cause absorbed by a `_ =>` default is
        // the shape #609 was filed about, so it stays named either way.)
        DeathCause.Inspected => InspectedLines,
        _ => CollectorLines,
    };

    /// <summary>The seeded, place-dependent house-voice line for a death — the WHAT-HAPPENED sentence(s) the
    /// resurrection card reads before the existing brain-backup copy. <paramref name="bodyName"/> names the
    /// place (the moon, the body flown into); null/blank reads with a generic place so the line is whole.
    /// Pure: same cause + same seed + same body → same line.</summary>
    /// <summary>#574 · The line for a death, told for WHERE it happened. A derelict's dead have no regolith
    /// to lie in and no tube to have been short of.</summary>
    /// <summary>#574 · Can this cause honestly happen in this place? Owner: <i>"the debt collector deaths
    /// should also only happen in those situations never in any other :-D"</i>.
    ///
    /// <para>A collector is a PERSON who came for you — a heat-hunter with a ship and a boarding volley.
    /// There is nobody aboard a dead hull and nobody on an empty moon, so that death cannot occur there, and
    /// a card claiming it would be inventing a character. Likewise an impact is the ship meeting a world at
    /// speed, which cannot happen to somebody standing on one.</para>
    ///
    /// <para>Stated here rather than trusted to callers, so it can be TESTED and so any future death lane
    /// has one place to check itself against.</para></summary>
    public static bool CanHappen(DeathCause cause, DeathPlace place) => CanHappenCore(cause, AsAHull(place));

    private static bool CanHappenCore(DeathCause cause, DeathPlace place) => cause switch
    {
        // You have to be at the controls to fly a ship into something.
        DeathCause.Impact => place == DeathPlace.OwnShip,

        // #583 · AND NOW THE COLLECTORS COME TO THE GROUND. This used to be OwnShip-only, and it was right
        // for as long as heat could only ever be delivered to a deck — the owner's own ruling that a
        // collector death "should only happen in those situations, never in any other". What changed is the
        // situation: a repo boat now sets down on the regolith and a crew walks you down on foot, because
        // "FBI does not arrest cars ... they look for the driver". Still never on a derelict — boarding a
        // wreck they are already inside is its own arrival and is not built yet.
        // #609 · nor 150 m under a moon. Same reasoning as the derelict, one shaft further: a collector is a
        // person who came for you, and nobody rides a lift they would have to call a car for to collect a
        // debt in a building that is not on any register.
        DeathCause.Collector => place is not (DeathPlace.Derelict or DeathPlace.Underground),
        // The Old Ones and the tank reach you anywhere you are out of the ship.
        DeathCause.Reevers or DeathCause.Joined or DeathCause.Suffocated => place != DeathPlace.OwnShip,

        // #621 · The void is where there is NO ground and no hull — "no beacon, no body, just the long dark",
        // "there was no ground to hit". It was falling through the `_ => true` default and so was legal on a
        // moon, inside a wreck and a hundred and fifty metres under a moon, where its own prose is nonsense.
        // That default absorbing a gap is precisely the shape #609 was filed about; the cause is stated.
        DeathCause.Void => place == DeathPlace.OwnShip,

        // #525 · The scuttling panel is bolted to somebody else's reactor, and a moon has no reactor at all
        // — so this cause is legal in a NAMED set of places and says so, rather than falling through a
        // default that would make it legal 150 m under a moon.
        //
        // #633 · That named set is now TWO. This read `place == DeathPlace.Derelict` and justified itself
        // with "your own ship has no such switch yet (that is #525's second half)" — which was true on this
        // branch and false on `main`, where #525's second half shipped: `ShipScuttle`, the charges in the
        // machinery space, and the crew's second key. Reunifying the branches without widening this would
        // have been a constant governing the wrong world (named bug class 2) on the very first own-ship
        // scuttling. There is still no reactor under a moon and none on a landing party's back.
        DeathCause.Scuttled => place is DeathPlace.Derelict or DeathPlace.OwnShip,

        // #538 · A sweep team finds you where you are not supposed to be. The sim says so itself:
        // `ChallengeRunsOut` only fires with an away excursion live (`_surface`), so there is no version of
        // this death on her own bridge — and the card's own words ("you were FOUND aboard") would be a lie
        // about the place if there were.
        DeathCause.Inspected => place != DeathPlace.OwnShip,
        _ => true,
    };

    /// <summary>#574 · The closing beat of a death card. <i>"The freeze-frame holds"</i> was hard-appended in
    /// the markup to EVERY death — it is BUSTED's own language, about a collector's gun-camera still, and it
    /// was being read out over a captain who suffocated alone on a moon with nobody watching. Same failure
    /// as the borrowed prose, one line further down the card.</summary>
    public static string Tail(DeathCause cause, DeathPlace place) => TailCore(cause, AsAHull(place));

    private static string TailCore(DeathCause cause, DeathPlace place) => (cause, place) switch
    {
        (DeathCause.Collector, _) => " The freeze-frame holds.",
        (_, DeathPlace.Derelict) => " Her log will not mention it.",
        (DeathCause.Suffocated, _) => " The gauge is still counting down in the dark.",
        (_, DeathPlace.LandingParty) => " The ground keeps what it is given.",
        _ => "",
    };

    public static string Line(DeathCause cause, DeathPlace placeAsGiven, ulong seed, string? bodyName)
    {
        // #653 · A dead station's suffocation has a pool of its own (a station is not a ship); every other death there reads
        // as aboard a hull, and the tail and the art stand.
        if (placeAsGiven == DeathPlace.Station && cause == DeathCause.Suffocated)
        {
            return SuffocationLinesAboardAStation[(int)(seed % (ulong)SuffocationLinesAboardAStation.Length)]
                .Replace("{body}", string.IsNullOrWhiteSpace(bodyName) ? "that station" : bodyName!);
        }

        DeathPlace place = AsAHull(placeAsGiven);
        if (place == DeathPlace.Derelict)
        {
            string[]? aboard = cause switch
            {
                DeathCause.Reevers => ReeverLinesAboardAWreck,
                DeathCause.Joined => JoinedLinesAboardAWreck,
                DeathCause.Suffocated => SuffocationLinesAboardAWreck,
                DeathCause.Scuttled => ScuttleLinesAboardAWreck,
                _ => null,
            };
            if (aboard is not null)
            {
                string t = aboard[(int)(seed % (ulong)aboard.Length)];
                return t.Replace("{body}", string.IsNullOrWhiteSpace(bodyName) ? "that hull" : bodyName!);
            }
        }

        // #609 · A death in the facility gets facility words. The away-team pool talks about regolith, a
        // suit and the walk back to the tube — none of which is where the captain is standing.
        if (place == DeathPlace.Underground)
        {
            string[] below = cause == DeathCause.Suffocated ? SuffocationLinesBelow : ReeverLinesBelow;
            return below[(int)(seed % (ulong)below.Length)]
                .Replace("{body}", string.IsNullOrWhiteSpace(bodyName) ? "that moon" : bodyName!);
        }

        // #583 · A collector catch on the GROUND gets ground words. The ship pool talks about a boarding
        // volley and a last stand, which is a different death entirely.
        if (place == DeathPlace.LandingParty && cause == DeathCause.Collector)
        {
            string onFoot = CollectorLinesOnFoot[(int)(seed % (ulong)CollectorLinesOnFoot.Length)];
            return onFoot.Replace("{body}", string.IsNullOrWhiteSpace(bodyName) ? "that ground" : bodyName!);
        }

        return Line(cause, seed, bodyName);
    }

    public static string Line(DeathCause cause, ulong seed, string? bodyName)
    {
        string[] pool = PoolFor(cause);
        string template = pool[(int)(seed % (ulong)pool.Length)];
        string body = string.IsNullOrWhiteSpace(bodyName) ? "that world" : bodyName!;
        string where = string.IsNullOrWhiteSpace(bodyName) ? "in open space" : $"off {bodyName}";
        return template.Replace("{body}", body).Replace("{where}", where);
    }
}
