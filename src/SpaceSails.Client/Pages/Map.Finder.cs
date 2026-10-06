using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #417 slice 1 · <b>ILSE VARGA WORKS THE BARS.</b> A second finder at a bar table, the trail she sets, and
/// the berth it ends at.
///
/// <h3>Built out of five things that already existed, and one new sentence apiece</h3>
///
/// <para><b>The legs</b> are #973 L0's <c>ApproachTheTable</c> — the same hook the walk-in crosses the floor
/// on, and its own summary says it is <i>"the one verb any NPC in a docked station's bar uses to walk up to
/// the captain"</i>. <b>The chair</b> is the eighth seat. <b>The graph</b> is Core's
/// <see cref="FinderCase"/>, handed the world this page is running. <b>The trail's three seams</b> are the
/// bar's own patron flow (#414's rota), the ruins' own papers (#563/#603), and a hull's own ledger of names
/// (#397/#426) — no lead has a verb of its own, because a detective's work is asking the world questions it
/// already answers. <b>The words</b> are Fable's, in Core, and not one of them is typed twice.</para>
///
/// <h3>Why the leads say almost nothing</h3>
///
/// <para>Because the FIELD BOOK is the detective. Each lead answered files the case's own line under the
/// case's own subjects (#741/#934), so the THREADS page stacks four entries under <i>👤 Ilse Varga</i> and
/// the captain watches the case assemble itself out of things he went and looked at. The two moments that DO
/// speak are the two the canon pass wrote sentences for: the red herring clearing, and the reveal.</para>
///
/// <h3>…and the one lead that talks, because it is a man</h3>
///
/// <para><b>#417 slice 2a.</b> The paper and the hull are things: look at them and they are yours. The
/// witness is a person working a rota at somebody else's port, and slice 1 let him hand over what he saw
/// because the captain walked within a metre of him. He is now loosened or not loosened: he says he does not
/// work for you, he is offered a glass through the bar's own <see cref="ContactDrink.OfferDrink"/> — the
/// shipped roll, the shipped math, the shipped offer moment, accept or refuse decided before a credit moves
/// — and the lead is behind the accept. A refusal costs the WATCH and not the lead. The other two leads are
/// not touched by a word.</para>
///
/// <h3>Once per port, and never twice</h3>
///
/// <para>She keeps a VISIT FOLD, exactly as the salesman and the walk-in do: a different berth is a different
/// evening, and she asks once an evening whatever the answer was. Taking the case is what stops her asking at
/// the NEXT port — the case is one case, and a finder with two of them running is a job board.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>The graph, once the captain has TAKEN it. Null for a captain who has never sat down with her
    /// — which is almost every captain almost always.
    ///
    /// <para>Deliberately not written until the case is taken. A graph the captain looked at and walked away
    /// from is not a graph he owns, and a field that held one would quietly stop her ever coming back: the
    /// reason-to-cross-the-floor test below reads this field to decide she already has work out.</para></summary>
    private FinderCase.Case? _finderCase;

    /// <summary>…and the one she is offering at THIS table, which is a fact about this evening and is
    /// forgotten with it. Null when she has nothing to hand over.</summary>
    private FinderCase.Case? _finderOffer;

    /// <summary>Whether this evening has already ASKED the world for a case. Null is a question this berth
    /// has not put yet; false is an answer it gave (a world with nothing to find in it).
    ///
    /// <para>It is here because the question is expensive and its answer cannot change while the captain is
    /// sitting in the room: <see cref="TheCaseThisPortDeals"/> walks every body and every hull in the
    /// scenario, and the gate above it is <i>the captain is sitting alone</i> — so without this the walked
    /// frame would rebuild the whole graph sixty times a second for as long as he sat there. #731's own
    /// lesson, in a bar.</para></summary>
    private bool? _finderAskedTheWorld;

    /// <summary>…and how far down it he has got.</summary>
    private FinderCase.Progress _finderProgress = FinderCase.Progress.Fresh;

    /// <summary>Which berth this file is remembering. Null off a berth; a different berth is a different
    /// evening — the same fold the salesman and the walk-in keep.</summary>
    private string? _finderVisitBerth;

    /// <summary>Whether she has already crossed this floor this visit. Once an evening, whatever was
    /// said.</summary>
    private bool _finderAskedThisVisit;

    /// <summary>#417 slice 2a · What the WITNESS's current watch has already had out of him — his sentence,
    /// and his one glass. Her fold is a berth and his is a watch, because that is the unit each of them is
    /// actually measured in: she travels, and he works a rota (<see cref="PatronRota.WatchIndex"/>).
    ///
    /// <para>Not cleared anywhere and not stored anywhere: it is asked <see cref="FinderCase.WitnessWatch.On"/>
    /// the current watch at every door into it, and a fold asked about a watch it is not about answers as
    /// fresh.</para></summary>
    private FinderCase.WitnessWatch _finderWitnessWatch = FinderCase.WitnessWatch.Fresh;

    /// <summary>#417 dev cheat (<c>/map?finder=1</c>, <c>/map?finder=0</c>): force her on or off this berth.
    /// Null is the shipped rota. It forces WHETHER and never WHAT — the case, the hulls, the berth and the
    /// pay are the ones a captain gets.</summary>
    private bool? _finderCheat;

    /// <summary>Her card, up only while she is standing at the table, and it cannot outlive her body — the
    /// state #731's escort branch was written to refuse. The flag says whether she is here to ask or here to
    /// settle up.</summary>
    private (FinderCase.Case Case, bool Paying)? _finderCard;

    /// <summary>The reveal, up at the confrontation berth. Its own card because it is its own moment: the
    /// captain is not sitting with anybody, and the two verbs on it are the whole of the scene.</summary>
    private FinderCase.Case? _finderReveal;

    /// <summary>What the settling did, written onto the reveal card itself (#736's law) rather than pulsed
    /// under its own backdrop. Null until the captain has chosen.</summary>
    private string? _finderOutcome;

    /// <summary>Whether she has been answered, which is the same thing as her not being wanted any more.</summary>
    private bool _finderAnswered;

    /// <summary>The glyph the case wears in the field book. Named once, so the entry, the guard and any
    /// future row cannot come to three views of one mark.</summary>
    private const string FinderGlyph = "🕵";

    // ── THE VISIT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A DIFFERENT BERTH IS A DIFFERENT EVENING. The one place forgetting happens.</summary>
    private void EnsureFinderVisit(string? berth)
    {
        if (_finderVisitBerth == berth)
        {
            return;
        }

        _finderVisitBerth = berth;
        _finderOffer = null;
        _finderAskedTheWorld = null;
        _finderCard = null;
        _finderAnswered = false;
        _finderAskedThisVisit = false;
    }

    // ── ONE FRAME OF HER EVENING ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #417 · Called once a frame from the docked bar's own metabolism, beside the salesman's and the
    /// walk-in's. Does nothing at all unless the captain is in the room and sitting alone.
    /// </summary>
    private void AdvanceTheFinder(in HavenInterior.BarFloor bar)
    {
        EnsureFinderVisit(bar.BodyId);
        if (!InTheBar(in bar))
        {
            return;
        }

        // Her card cannot outlive her body. The walker list is the truth about who is at the table.
        if (_finderCard is not null && TheFinderAfoot() is null)
        {
            CloseTheFindersCard();
            return;
        }

        if (_finderAskedThisVisit || TheFinderAfoot() is not null || _barAfoot.Count >= WalkerBand
            || !TheCaptainIsSittingAloneInTheBar())
        {
            return;
        }

        if (!TheresAReasonForHerToCrossThisFloor(bar.BodyId))
        {
            return;
        }

        if (!ApproachTheTable(FinderCase.Plate, TheFinderIsStillWanted, SheReachesTheFindersTable))
        {
            _finderAskedThisVisit = true;
        }
    }

    /// <summary>
    /// #417 · <b>IS THERE ANYTHING FOR HER TO SAY AT THIS TABLE?</b> Three answers, and the order is the
    /// scene: she comes to be PAID first (a settled case is a debt she owes, and she pays it at the next bar
    /// she finds you in), she comes with a CASE when the captain has none, and otherwise she does not come —
    /// a finder with a job already running does not sit down to talk about it.
    ///
    /// <para>The cheat forces WHETHER she has anything to say and not WHAT: with no case, no settled case and
    /// no world to build one out of, there is still nothing for her to cross a floor about.</para>
    /// </summary>
    private bool TheresAReasonForHerToCrossThisFloor(string berth)
    {
        if (_finderCase is { } running)
        {
            _finderOffer =
                _finderProgress.Settled != FinderCase.Outcome.Open && !_finderProgress.PaidOff ? running : null;
            return _finderOffer is not null;
        }

        if (_finderCheat is false)
        {
            return false;
        }

        // Asked ONCE an evening. The answer is a fact about a scenario and a berth and cannot change while
        // the captain is in the chair, and the walk to it is the whole traffic list.
        if (_finderAskedTheWorld is { } asked)
        {
            return asked;
        }

        _finderOffer = TheCaseThisPortDeals(berth);
        _finderAskedTheWorld = _finderOffer is not null;
        return _finderAskedTheWorld.Value;
    }

    /// <summary>
    /// #417 · <b>THE WORLD, HANDED TO CORE.</b> Every list the case is built from is read off the running
    /// game here and nowhere else: the scenario's berths and their tiers, the traffic actually flying, the
    /// moons a shuttle can put the captain on. Core does the choosing; this method does the looking.
    /// </summary>
    private FinderCase.Case? TheCaseThisPortDeals(string berth)
    {
        if (_ephemeris is null)
        {
            return null;
        }

        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var sites = new List<FinderCase.Site>();
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            names[body.Id] = body.Name;
            if (ShuttleExcursion.IsLandableSurface(body.Kind))
            {
                sites.Add(new FinderCase.Site(body.Id, body.Name));
            }
        }

        var hulls = new List<FinderCase.Hull>(_npcStates.Length);
        foreach (NpcState npc in _npcStates)
        {
            if (StoryHulls.IsOne(npc.Ship.Id))
            {
                continue;   // #1357 - a story hull is never a finder's hull
            }

            hulls.Add(new FinderCase.Hull(
                npc.Ship.Id, npc.Ship.Callsign, ShipHistories.For(npc.Ship.Id)));
        }

        return FinderCase.Build(
            _activeThreadId ?? "", berth, OldCrew.BerthsOf(_ephemeris), names, hulls, sites);
    }

    /// <summary>The walker that is her, if she is on the floor. By plate, exactly as the walk-in is told
    /// apart: her errand is the room's ordinary <see cref="Errand.Approaching"/>.</summary>
    private Walker? TheFinderAfoot()
    {
        foreach (Walker w in _barAfoot)
        {
            if (w.For == Errand.Approaching
                && string.Equals(w.Walk.Plate, FinderCase.Plate, StringComparison.Ordinal))
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>The gate <c>ApproachTheTable</c> asks when the walk is planned, again on the frame it lands,
    /// and every frame she is standing there. The walk-in's own two answers and for her reason: before she
    /// arrives it is seated-and-alone, and once she is AT the table she is the company, so the question
    /// becomes whether the captain is still in the chair.</summary>
    private bool TheFinderIsStillWanted() =>
        !_finderAnswered
        && (_finderCard is null
            ? TheCaptainIsSittingAloneInTheBar()
            : CaptainIsSeated && SeatedTable is { Bench: false, Office: false });

    // ── SHE REACHES THE TABLE ──────────────────────────────────────────────────────────────────────────

    /// <summary>She is at your elbow. Held behind the one scrim like everybody else's card (#1052).</summary>
    private void SheReachesTheFindersTable() => RaiseAScrimCard(HerCaseGoesUp, TheFinderIsStillWanted);

    /// <summary>The card itself, once the glass is hers.</summary>
    private void HerCaseGoesUp()
    {
        if (_finderOffer is not { } c || SeatedTable is not { } t)
        {
            return;
        }

        _finderAskedThisVisit = true;
        t.Solo = false;
        t.Plate = FinderCase.Plate;
        t.Free = Math.Max(0, t.Free - 1);

        _finderCard = (c, _finderProgress.Settled != FinderCase.Outcome.Open);

        // She is a relationship from the first hello — the book knows her name before she has asked for
        // anything, which is what lets the reputation this case pays land somewhere that already exists.
        _contacts.AddGoodwill(FinderCase.ContactId, FinderCase.DisplayName, 0);
        RendererInterop.PlayCue("reveal");
        StateHasChanged();
    }

    /// <summary>Take her off the card. The body stays wherever the room has it; only the panel goes.</summary>
    private void CloseTheFindersCard()
    {
        _finderCard = null;
        StateHasChanged();
    }

    // ── THE KEEPING ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#417 · The case and the trail, as the vault stores them — two opaque rows, the house idiom.
    /// Null for a captain who has never taken one, so a file written by somebody who never met her carries no
    /// section at all.</summary>
    private FinderSection? BuildFinderSection() =>
        _finderCase is { } c && _finderProgress.HasHistory
            ? new FinderSection
            {
                Case = FinderCase.Stored(c),
                Progress = FinderCase.Stored(_finderProgress),
            }
            : null;

    /// <summary>Read them back. A pre-#417 file simply has none and wakes with no case, which is exactly what
    /// it had — and a row this build cannot parse is dropped rather than thrown over, the same tolerance the
    /// filing line and the satchel get.</summary>
    private void RestoreFinderSection(FinderSection? section)
    {
        _finderCase = null;
        _finderProgress = FinderCase.Progress.Fresh;
        _finderCard = null;
        _finderReveal = null;
        _finderOutcome = null;

        if (section is null || !FinderCase.TryRead(section.Case, out FinderCase.Case c))
        {
            return;
        }

        _finderCase = c;
        if (FinderCase.TryRead(section.Progress, out FinderCase.Progress p))
        {
            _finderProgress = p;
        }
    }
}
