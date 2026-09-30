using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #251 · WHAT THE CATALOGUE ANSWERS, AND THE DECK IT HANDS BACK — which havens have a deck, which have a
/// floor under it, the memo's two test-visible counts, and <see cref="DockedDeck"/>, the one door every
/// caller comes through to get a built station.
///
/// <para>Split out of <c>HavenInterior.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and NO FIELD. <c>Specs</c> and <c>Cache</c>, which these members read,
/// stay declared in the opening file in their original order — see its remarks and #1163 for why every
/// static field of this class lives there.</para>
/// </summary>
public static partial class HavenInterior
{
    /// <summary>#1112 · How many built stations the memo is holding, for the guard that holds it to its cap.
    /// Test-visible only — nothing in the game may care how warm a cache is.</summary>
    internal static int DeckCacheCount => Cache.Count;

    /// <summary>#1112 · …and the cap it is held to.</summary>
    internal static int DeckCacheCap => Cache.Cap;

    /// <summary>Does this haven have a walkable interior (so docking should weld on a tube)?</summary>
    public static bool HasInterior(string bodyId) => System.Array.Exists(Specs, s => s.BodyId == bodyId);

    /// <summary>#1253 · …and does it have a FLOOR UNDER THAT ONE? Every haven with a hub does since #1332 A. Asked here by the deck
    /// build, the cages, the page's own ride and every guard, so "this berth has a basement" is one answer
    /// rather than a body id compared in six files.</summary>
    public static bool HasLowerLevel(string bodyId) =>
        System.Array.Find(Specs, s => s.BodyId == bodyId) is { Lower: not null };

    /// <summary>#1253 · The first berth in the catalogue that HAS a floor under it, or null while no station
    /// does. Published for the boot cheat, which needs somewhere to default to: a floor cheat pointed at one
    /// of the six one-storey havens is a URL that lands on a concourse and proves nothing. Asked of the
    /// catalogue rather than spelled as an id, so the day a second station grows a basement nothing has to be
    /// told about it.
    ///
    /// <para>#1332 A · Every haven has a floor under it now, so "the first in the catalogue" would have moved
    /// the cheat's default to The Rusty Roadstead. It stays where the floors were first built and where the
    /// night that uses them runs: the station with the observation walk, whenever that station has floors,
    /// and the catalogue's first otherwise.</para></summary>
    public static string? TheHavenWithFloors =>
        HasLowerLevel(ObservationWalk.HavenId)
            ? ObservationWalk.HavenId
            : System.Array.Find(Specs, s => s.Lower is not null)?.BodyId;

    /// <summary>#1253 · Which levels this berth actually has, top down — one floor at six of the seven havens
    /// and two at Selene Gate. Published so a sweep walks the floors a station HAS rather than the floors
    /// somebody remembered to list; a test can only hold what it can enumerate.</summary>
    public static IReadOnlyList<int> LevelsOf(string bodyId) =>
        !HasInterior(bodyId) ? []
        : HasLowerLevel(bodyId) ? HavenLevels.Levels
        : [HavenLevels.Concourse];

    /// <summary>Every haven that HAS a deck, so the deck audit can walk all of them rather than the ones
    /// somebody remembered to list. A test can only hold what it can enumerate.</summary>
    public static IReadOnlyList<string> InteriorBodyIds
    {
        get
        {
            var ids = new List<string>(Specs.Length);
            foreach (StationSpec spec in Specs)
            {
                ids.Add(spec.BodyId);
            }
            return ids;
        }
    }

    /// <summary>
    /// The docked complex for a body — ship + tube + hall + bar as one walkable plan — or null if that
    /// haven has no deck to walk. <paramref name="unlockedHatchIds"/> is the session's set of cracked
    /// hatch ids for this station (bare ids like "V-06"); any that grow a wing weld their back room on.
    /// <paramref name="simTime"/> is the docking watch: the seated regulars' rota (<see cref="PatronRota"/>)
    /// is resolved at this clock, so who's at the bar and which chair they took is baked for this visit —
    /// re-dock a watch later and the room reads different. Built once per (station, unlock-state, watch),
    /// lazily, and shared.
    /// </summary>
    /// <param name="forceOracle">The <c>?oracle=1</c> seat cheat (#428): plant the oracle's corner console
    /// whatever her rota says this watch. Part of the cache key — a deck built before the cheat was armed
    /// can never be handed back for a forced boot.</param>
    /// <param name="fillWalkers">#973 L0 · The page's own walker band, written into the slots after the room's
    /// seated figures — handed <c>(buffer, firstSlot)</c> on every frame the plan is drawn. Null for a deck
    /// nobody is walking across, which is every caller that only wants the geometry (and every test that has
    /// always asked for one).
    ///
    /// <para><b>A plan with a filler is NOT cached, and that is deliberate.</b> The cache is shared process-wide
    /// and xUnit runs test classes in parallel — the concurrent dictionary above exists because two of them
    /// building haven decks at once corrupted it. A delegate closed over ONE page, handed back to a second page
    /// out of a shared cache, would be one buffer written by two rooms: the named bug class, with the flakiest
    /// possible symptom. Building costs a few hundred objects and happens twice per docking, so there is
    /// nothing to save here anyway.</para></param>
    /// <param name="churn">#731 · What this evening has done to the room — who has walked out and who has come
    /// in and sat down (<see cref="RoomChurn"/>). Null, or a churn with nothing in it, is the rota's own
    /// answer. A churn that HAS something in it is part of the cache key, for the reason the watch is: two
    /// rooms with different people in them are two rooms.</param>
    /// <param name="tier">#380 item 10 · Which tube this berth earned (<see cref="ArrivalTube.TierFor"/>), so the
    /// customs desk at the immigration gate can say what the gate is for. It is a PARAMETER and not something
    /// this file works out, because the tier is derived from the scenario's traffic and this renderer has no
    /// ephemeris — passing the page's own answer in is what makes the desk and the arrival plate one reading of
    /// one berth rather than two. Null is "nobody asked": the desk is left off, which is what every caller that
    /// only wants the geometry has always got. Part of the cache key, for the reason the watch is.</param>
    /// <param name="level">#1253 · WHICH FLOOR OF THE STATION. <see cref="HavenLevels.Concourse"/> — the
    /// default — is the deck this game has always built, to the byte: every caller that has ever asked for a
    /// docked deck reaches exactly the plan it did before, and the six havens with no lower level answer the
    /// concourse whatever is asked of them. <see cref="HavenLevels.ServiceLevel"/> builds the floor under it
    /// at the one station that has one (HavenInterior.Lower.cs).
    ///
    /// <para>It is part of the cache key for the reason the watch and the churn are: two floors are two
    /// rooms, and a memo that served whichever was built first is the audit's own named prediction about
    /// what "just drop in a second plan" costs.</para></param>
    /// <param name="office">#1332 C · How the Preservation office's door stands, at the one station whose hotel
    /// level has it (<see cref="HasTheOffice"/>) — asked only below the concourse and ignored everywhere else, so
    /// every other floor of every other station is the plan it always was. <see cref="OfficeDoor.Shut"/>, the
    /// default, is the plan every caller that only wants the geometry has always got. Part of the memo's key.</param>
    public static DeckPlan? DockedDeck(string bodyId, IReadOnlySet<string>? unlockedHatchIds = null, double simTime = 0,
        bool forceOracle = false, System.Action<DeckPlan.Droid[], int>? fillWalkers = null,
        RoomChurn? churn = null, ArrivalTube.Tier? tier = null, int level = HavenLevels.Concourse,
        OfficeDoor office = OfficeDoor.Shut)
    {
        if (System.Array.Find(Specs, s => s.BodyId == bodyId) is not { } spec)
        {
            return null;
        }

        // #1253 · THE FLOOR UNDER IT, at the one station that has one. Asked FIRST and answered on its own
        // road, because nothing below the concourse shares a line with it: no tube, no bar, no ring of other
        // captains' berths, no rota and no wing. A station with no basement answers the concourse for any
        // level at all, which is what keeps every older caller and every other haven exactly as they were.
        if (level != HavenLevels.Concourse && spec.Lower is { } lower)
        {
            // #1332 C · A station with no office has one way to draw its hotel level, whatever is asked.
            OfficeDoor door = lower.Office is null ? OfficeDoor.Shut : office;
            if (fillWalkers is not null)
            {
                return BuildLowerComplex(spec, lower, fillWalkers, door);
            }

            return Cache.GetOrBuild(
                door == OfficeDoor.Shut ? $"{bodyId}@lower" : $"{bodyId}@lower+{door}",
                () => BuildLowerComplex(spec, lower, null, door));
        }

        IReadOnlyList<DeckWing> active = unlockedHatchIds is null
            ? []
            : DeckExpansions.ActiveWings(WingCatalog(bodyId), bodyId, unlockedHatchIds).ToList();
        if (fillWalkers is not null)
        {
            return BuildComplex(spec, active, simTime, forceOracle, fillWalkers, churn, tier);
        }
        long watch = PatronRota.WatchIndex(simTime);
        string wingKey = active.Count == 0
            ? bodyId
            : bodyId + "|" + string.Join(",", active.Select(w => w.UnlockHatchId).OrderBy(s => s, System.StringComparer.Ordinal));
        string room = churn is { Anything: true } c ? "+" + c.Signature : "";
        string gate = tier is { } t ? "+" + t : "";
        string key = $"{wingKey}@{watch}{(forceOracle ? "+oracle" : "")}{room}{gate}"; // the seated-regular rota re-rolls each watch, so it keys the cache
        // #1112 · Held to a cap, and on overflow the memo starts fresh. A rebuilt deck is the deck that was
        // thrown away — every input to BuildComplex here is in the key — so an eviction costs the few hundred
        // objects of one build and nothing else.
        return Cache.GetOrBuild(key, () => BuildComplex(spec, active, simTime, forceOracle, null, churn, tier));
    }
}
