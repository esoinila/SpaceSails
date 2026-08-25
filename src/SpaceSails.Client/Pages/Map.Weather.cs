using System;
using System.Collections.Generic;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #973 · THE VOID'S WEATHER — the client half of the bar talk about the walking insurance men.
///
/// <para>Owner, 2026-08-25: <i>"a great way to have fun and sell the story, it could be the thing people talk
/// about in the bars that unites them, a bit like talking about the weather on planet side."</i></para>
///
/// <para><b>Nothing new is built here.</b> The words and the whole selection law are Core's
/// (<see cref="Core.InsuranceWeather"/>); the block they land in is the counter card's shipped <i>Overheard
/// here</i> strip; the round that turns one of them into the room's shared topic is
/// <c>BuyRoundForRoom</c>'s existing loosened-tongues beat; the one note the weather can file is a
/// <see cref="HeldMemory"/> sheet through the black book's own <c>Put</c>. What this file owns is the VISIT —
/// which room the weather belongs to, how many times the captain has stood in it, and which line (if any) is
/// in the air today.</para>
///
/// <h3>Why the rep's visit clock and not a second one</h3>
///
/// <para>The rota that decides whether Harlan Fess walked this room (#976,
/// <see cref="NebulaRep.IsWorkingThisStation"/>) is keyed on the BODY and folded by
/// <c>EnsureRepVisit</c> — the one fold both rooms with a walker band already run through, the canteen floor
/// and the docked bar alike. The weather rides that same fold, so "the room he walked" and "the room talking
/// about him" cannot come to two different answers about which room, or about which visit. A second visit
/// counter beside it would be this repo's first named bug class wearing a barman's apron.</para>
///
/// <h3>What is deliberately NOT durable</h3>
///
/// <para>An insurance line never enters the durable overheard book (<c>Overhear</c>). Every other line at
/// this counter is something the captain PAID for — a round, a rumour, a glass with a contact — and the
/// owner's #212 rule keeps those forever. The weather is what the room was saying anyway. What the save does
/// carry is how OFTEN each line has been heard (so a sentence retires) and which visit it last blew through
/// each station on (so it is never two visits running) — the facts, never the sentences.</para>
/// </summary>
public partial class Map
{
    /// <summary>How many times each line has been heard, this thread. The retiring rule's whole memory.</summary>
    private readonly Dictionary<string, int> _weatherHeard = new(StringComparer.Ordinal);

    /// <summary>How many times the captain has visited each station, counting from zero — the station's OWN
    /// ordinal, because "never two visits in a row at the same station" is a fact about Ceres and not about
    /// the run of ports between two calls at it.</summary>
    private readonly Dictionary<string, int> _weatherStationVisits = new(StringComparer.Ordinal);

    /// <summary>…and the station-visit ordinal a line last surfaced at each of them on, or −1 for a station
    /// the weather has never blown through.</summary>
    private readonly Dictionary<string, int> _weatherLastSaid = new(StringComparer.Ordinal);

    /// <summary>Which station this visit's weather belongs to. Null off any ground with a walker band on
    /// it — the same fold, and the same null, <c>EnsureRepVisit</c> keeps.</summary>
    private string? _weatherStation;

    /// <summary>Whether this visit has already asked the question. Asked ONCE per visit, so a captain who
    /// leans on the counter four times is not four conversations.</summary>
    private bool _weatherAsked;

    /// <summary>The line in the air this visit, or null when the room is on something else.</summary>
    private string? _weatherSaidId;

    /// <summary>Who said it — a regular of this room, in whatever the room calls them.</summary>
    private string? _weatherSpeaker;

    /// <summary>Whether a round for the room turned it into everybody's topic.</summary>
    private bool _weatherShared;

    /// <summary>The words in the air this visit, or null. Asked of Core off the id, never kept beside it: a
    /// sentence stored next to the id that names it is two answers to one question.</summary>
    private string? TheWeatherSaid() => Core.InsuranceWeather.TextOf(_weatherSaidId);

    // ── THE VISIT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A DIFFERENT ROOM IS A DIFFERENT VISIT. Called from <c>EnsureRepVisit</c>, which is the one fold both
    /// rooms with a walker band run through — so the weather's visit and the rep's rota visit are the same
    /// visit by construction rather than by two files agreeing.
    /// </summary>
    private void EnsureTheWeathersVisit(string? stationId)
    {
        if (string.Equals(_weatherStation, stationId, StringComparison.Ordinal))
        {
            return;
        }

        _weatherStation = stationId;
        _weatherAsked = false;
        _weatherSaidId = null;
        _weatherSpeaker = null;
        _weatherShared = false;

        if (stationId is null)
        {
            return;
        }

        _weatherStationVisits[stationId] =
            (_weatherStationVisits.TryGetValue(stationId, out int been) ? been : -1) + 1;
    }

    /// <summary>
    /// #973 · WHAT THE ROOM IS TALKING ABOUT TODAY, decided the moment the counter card opens and never
    /// again this visit. Both doorways onto that card call it (<c>TalkToBarkeep</c> ashore,
    /// <c>OpenCounterService</c> underground), because the weather is a fact about the ROOM and not about
    /// which press got you looking at it.
    ///
    /// <para>Hearing a line COUNTS it, here and nowhere else — the count is what retires a sentence after
    /// <see cref="Core.InsuranceWeather.RetireAfterHearings"/> tellings, and counting it at the moment it is
    /// chosen rather than at the moment it is drawn keeps a captain who re-opens the card from wearing one
    /// line out in an afternoon.</para>
    /// </summary>
    private void TheWeatherComesIn()
    {
        if (_weatherAsked || _weatherStation is not { } station)
        {
            return;
        }

        _weatherAsked = true;

        int visit = _weatherStationVisits.TryGetValue(station, out int been) ? been : 0;
        int last = _weatherLastSaid.TryGetValue(station, out int said) ? said : -1;

        // #976's own answer about this very room, this very watch — never re-derived here.
        string? line = Core.InsuranceWeather.Draw(
            _activeThreadId ?? "", station, visit, _repWorkingHere, _weatherHeard, last);
        if (line is null)
        {
            return;
        }

        _weatherSaidId = line;
        _weatherSpeaker = WhoInThisRoomSaysIt(station, visit);
        _weatherHeard[line] = (_weatherHeard.TryGetValue(line, out int times) ? times : 0) + 1;
        _weatherLastSaid[station] = visit;

        TheCousinIsAShapeYouKnow();
        RequestVaultSave();   // #225: a line was heard, and a station remembers it was
    }

    /// <summary>
    /// WHO SAYS IT. A regular of THIS room in whatever the room calls them: one of the named faces drinking
    /// here when the deck plan drew any, and otherwise one of the crowd's own plates — the same fall-back
    /// <c>TheRoomSaysItBack</c> uses, so a line out of the crowd reads the way a line out of the crowd has
    /// read since #781.
    /// </summary>
    private string WhoInThisRoomSaysIt(string station, int visit)
    {
        var here = new List<string>();
        foreach (string giver in PresentBarPatrons())
        {
            here.Add(GiverDisplay(giver));
        }

        IReadOnlyList<string> pool = here.Count > 0 ? here : CanteenRegulars.StrangerPlates;
        int face = DiceRule.Roll(
            DiceRule.Seed($"insurance-weather:who:{station}", visit), pool.Count).Face;
        return pool[face - 1];
    }

    // ── WHERE IT SURFACES ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 · THE COUNTER CARD'S <i>OVERHEARD HERE</i> BLOCK, with today's weather in it.
    ///
    /// <para>Three lines, always three lines. An insurance line does not lengthen the strip — it takes a
    /// slot, and what it costs is whatever this counter said longest ago. After a round has made it the
    /// room's shared topic the block is the topic and the room's answer to it, which is the one time the
    /// counter's own book steps aside.</para>
    /// </summary>
    private IReadOnlyList<string> TheOverheardBlock()
    {
        string? said = TheWeatherSaid();
        if (_weatherShared && said is not null && _weatherSpeaker is { } aloud)
        {
            return Core.InsuranceWeather.SharedTopic(aloud, said);
        }

        var book = new List<string>(Core.InsuranceWeather.BlockLines);
        foreach (Core.OverheardLine line in OverheardHere(Core.InsuranceWeather.BlockLines))
        {
            book.Add(line.Text);
        }

        string? weather = said is not null && _weatherSpeaker is { } who
            ? Core.InsuranceWeather.Overheard(who, said)
            : null;

        int visit = _weatherStation is { } station && _weatherStationVisits.TryGetValue(station, out int been)
            ? been
            : 0;
        return Core.InsuranceWeather.Block(
            book, weather, DiceRule.Seed($"insurance-weather:block:{_weatherStation}", visit));
    }

    /// <summary>
    /// #973 · A ROUND MAKES IT EVERYBODY'S. <c>BuyRoundForRoom</c>'s first round loosens tongues
    /// (<see cref="RoundTips"/>, owner 2026-07-18) and what a loosened room talks about is the weather: the
    /// line already in the air gets said out loud, by name, to the whole counter.
    ///
    /// <para>It never DRAWS a line — at most one insurance line per visit stands, and a round that could
    /// conjure one would be a second source for the same sentence. The round is the amplifier.</para>
    /// </summary>
    /// <returns>What to add to the round's receipt, or the empty string when the room is on something
    /// else.</returns>
    private string TheRoundMakesItTheRoomsTopic(bool loosenTongues)
    {
        if (!loosenTongues || _weatherShared
            || TheWeatherSaid() is not { } said || _weatherSpeaker is not { } who)
        {
            return string.Empty;
        }

        _weatherShared = true;
        return "  " + string.Join("  ", Core.InsuranceWeather.SharedTopic(who, said));
    }

    /// <summary>#973 · …and the warm seam names it. When a shared fright opens a stranger (#429's
    /// <c>StrangerBond</c>) in a room where a line is in the air, the bond's toast carries what the two of
    /// them were already listening to. The TOAST only: the bond's durable book line is untouched, because
    /// hearing the weather writes nothing anywhere.</summary>
    private string TheWeatherIsWhatTheyWereTalkingAbout() =>
        TheWeatherSaid() is { } said && _weatherSpeaker is { } who
            ? " " + Core.InsuranceWeather.AsHeard(who, said)
            : string.Empty;

    // ── THE ONE PLACE IT TOUCHES THE ARC ───────────────────────────────────────────────────────────────

    /// <summary>
    /// #973 · THE COUSIN WHO LAPSED. The one line of the eight that writes anything down, and only for a
    /// captain already holding the fleet-day page — the page the service filed, which no rebirth can grey
    /// (<see cref="OldCrewScene.SummerPartyId"/>). He knows the shape of a policy running out because he is
    /// carrying the one piece of his own past that did not.
    ///
    /// <para>A sheet in the black book through the book's own <see cref="HeldMemory.Put"/>: marked HIS,
    /// because the man telling it was there and the captain was not; tagged money, because a premium that
    /// ran out is a story about money whatever else it is about; and the text is the line exactly as it was
    /// heard, with the name of whoever said it.</para>
    /// </summary>
    private void TheCousinIsAShapeYouKnow()
    {
        bool holdsTheFleetDay =
            HeldMemory.Find(_heldMemories, OldCrewScene.SummerPartyId) is not null;
        if (!Core.InsuranceWeather.FilesANote(_weatherSaidId, holdsTheFleetDay)
            || TheWeatherSaid() is not { } said
            || _weatherSpeaker is not { } who)
        {
            return;
        }

        if (HeldMemory.Find(_heldMemories, Core.InsuranceWeather.LapsedCousinSheetId) is not null)
        {
            return;   // a second telling of the same story is the same story
        }

        _heldMemories = HeldMemory.Put(_heldMemories, new HeldMemory.Sheet(
            Core.InsuranceWeather.LapsedCousinSheetId,
            Core.InsuranceWeather.LapsedCousinMark,
            Core.InsuranceWeather.LapsedCousinTag,
            Core.InsuranceWeather.AsHeard(who, said),
            [who],
            SimTime,
            Filed: false,
            HandedBy: who));
    }

    // ── THE KEEPING ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#973 · The weather, as the vault stores it — counts and ordinals, never a sentence. Null when
    /// nothing has ever been heard and no station has ever been stood in, so a captain who has not been to a
    /// bar writes no section at all.</summary>
    private InsuranceWeatherSection? BuildTheWeatherSection()
    {
        if (_weatherHeard.Count == 0 && _weatherStationVisits.Count == 0)
        {
            return null;
        }

        var heard = new List<string>(_weatherHeard.Count);
        foreach (KeyValuePair<string, int> line in _weatherHeard)
        {
            heard.Add($"{line.Key}|{line.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        var stations = new List<string>(_weatherStationVisits.Count);
        foreach (KeyValuePair<string, int> station in _weatherStationVisits)
        {
            int last = _weatherLastSaid.TryGetValue(station.Key, out int said) ? said : -1;
            stations.Add(string.Join('|',
                station.Key,
                station.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                last.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        heard.Sort(StringComparer.Ordinal);       // a stable file, whatever order the dictionary walked in
        stations.Sort(StringComparer.Ordinal);
        return new InsuranceWeatherSection { Heard = heard, Stations = stations };
    }

    /// <summary>Read it back. A row this build cannot parse is dropped rather than thrown over — the same
    /// tolerance the satchel, the filing marks and the held memories get.</summary>
    private void RestoreTheWeatherSection(InsuranceWeatherSection? section)
    {
        _weatherHeard.Clear();
        _weatherStationVisits.Clear();
        _weatherLastSaid.Clear();
        _weatherStation = null;
        _weatherAsked = false;
        _weatherSaidId = null;
        _weatherSpeaker = null;
        _weatherShared = false;

        foreach (string row in section?.Heard ?? [])
        {
            string[] p = row.Split('|');
            if (p.Length == 2 && p[0].Length > 0
                && int.TryParse(p[1], System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int times)
                && times > 0)
            {
                _weatherHeard[p[0]] = times;
            }
        }

        foreach (string row in section?.Stations ?? [])
        {
            string[] p = row.Split('|');
            if (p.Length == 3 && p[0].Length > 0
                && int.TryParse(p[1], System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int visits)
                && int.TryParse(p[2], System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int last)
                && visits >= 0)
            {
                _weatherStationVisits[p[0]] = visits;
                _weatherLastSaid[p[0]] = last;
            }
        }
    }
}
