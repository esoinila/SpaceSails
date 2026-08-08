using System;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #784 · SITTING DOWN IS A STATE THE GAME CAN SEE — phase one.
///
/// <para>Owner, live 2026-08-08, three rulings in the same minutes over the #778 table:</para>
///
/// <list type="bullet">
/// <item><i>"Let's make the graphics say I am sitting down at the avatar level — like different graphics
/// etc."</i> → <see cref="CaptainIsSeated"/> rides to <see cref="Rendering.DeckView.State"/> and the deck
/// draws a different figure. A glance says sitting, with no panel text involved.</item>
/// <item><i>"before moving I have to stand up… so if I try to move when sitting down it should ask with a
/// pop-up whether I want to stand up again."</i> → <see cref="_standUpAsk"/>, raised by WASD and by nothing
/// else.</item>
/// <item><i>"Sitting down relaxes and heals"</i> / <i>"it is like short rest in TTRPG."</i> →
/// <see cref="RestOneSeatedBeat"/>, which is <see cref="ShortRest"/>'s arithmetic spent through the nerve
/// and condition systems that already exist.</item>
/// </list>
///
/// <h3>Deliberately NOT here (phase two, and other people's lanes)</h3>
///
/// <para>The live world behind the panel, the walker who crosses the hall for real (#731), and the spread /
/// thread-drawing tabletop (#741) are all the modal frame coming off, and none of them is in this file. What
/// IS here is everything that does not need the walker machinery — which is why the seated FLAG is a
/// property other lanes can read rather than a private field: <see cref="CaptainIsRestingAtATable"/> is the
/// one question "is the captain resting" has an answer to, and #783's panel picks its state image off it
/// rather than re-deriving a second answer.</para>
///
/// <para>The write gate is a PREDICATE and the write itself is one item at a time, on purpose: the owner's
/// phase-two addendum puts a per-item timer bar in the digging idiom in front of exactly this act, and a
/// gate written as "do the whole satchel now" would have had to be taken apart first.</para>
/// </summary>
public partial class Map
{
    /// <summary>#784 QA · <c>?hurt=N</c> — how many of <see cref="CaptainCondition.MaxHits"/> the captain
    /// steps out of the boat already carrying. Set in Map.Sim's cheat parse; read once, where an excursion's
    /// blow count begins. Null off the cheat, in which case a captain lands unmarked exactly as before.
    /// <para>It never seeds the fifth blow: booting a tester straight into a death card is not a demo.</para></summary>
    private int? _hurtCheat;

    // ── IS THE CAPTAIN SITTING DOWN? ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// #784 · THE ONE ANSWER. The table panel being open IS the captain being in the chair — #757 built it
    /// that way (the seat is the spot you walked to; nothing teleports anybody onto furniture) and this does
    /// not invent a parallel flag beside it, because a second answer to "are you sitting down" would be this
    /// repo's first named bug class aimed at a posture.
    /// </summary>
    public bool CaptainIsSeated => _table is not null;

    /// <summary>
    /// #784/#783 · IS THE CAPTAIN RESTING — the question a panel asks when it is choosing which picture to
    /// draw. A table you took ALONE is a rest; a table with somebody talking across it is a conversation,
    /// whatever your legs are doing.
    /// </summary>
    public bool CaptainIsRestingAtATable => _table is { Solo: true };

    // ── THE POUR IN FRONT OF YOU ──────────────────────────────────────────────────────────────────────

    /// <summary>How long after a pour the glass is still a glass, in real seconds. The counter is a walk
    /// away from the tables (#756 put them in one room), so this has to outlast crossing the hall and
    /// choosing a top; it is not the 90 s SPREE window <see cref="PourRum"/> keeps, which is a different
    /// question about a different thing. FLAGGED for the owner's tuning.</summary>
    private const double DrinkInHandSeconds = 300.0;

    /// <summary>
    /// #784 · Is there a bought pour in front of the captain? The one discoverable fact #756's counter
    /// leaves behind is the tot — <see cref="PourRum"/> is the single funnel every purchase in the game goes
    /// through — so this reads that and nothing else.
    ///
    /// <para>Two honest gaps, both filed rather than faked. FOOD does not route through <c>PourRum</c> (a
    /// fry-up does not tilt the deck, #756), so a meal buys no multiplier today. And a DRUNK captain gets
    /// none either: <see cref="NerveModel.DrunkAt"/> is the game's one drunkenness law and it already says a
    /// third tot stops helping — a rest is not the place to invent a second opinion about that.</para>
    /// </summary>
    private bool APourInFrontOfYou =>
        _rumTots > 0
        && !NerveModel.DrunkAt(_rumTots)
        && (_lastTimestampMs ?? 0) - _lastRumMs < DrinkInHandSeconds * 1000.0;

    // ── THE SHORT REST ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #784 · ONE SEATED WATCH-BEAT OF SHORT REST.
    ///
    /// <para>Called from the WAIT beat and from nowhere else, because the wait IS the beat: #757 already
    /// decided that holding a table advances a counter the room keeps, and hanging the rest off a second
    /// clock would make "how long have you been sitting there" two different numbers.</para>
    ///
    /// <para>The ledger is per WATCH and not per table — <see cref="ShortRest.NervePipCapPerWatch"/> is a
    /// ceiling on the SHIFT, so a captain who hops from top to top to reset it would be re-pressing their
    /// way out of a cap, which is the same abuse #757 closed on the approach roll.</para>
    ///
    /// <para>Both halves are spent through the systems that already own them: the nerve through
    /// <see cref="ApplyNerveRelief"/> (whole pips, named in the ledger — #480 forbids moving the gauge
    /// anonymously), the blow through the excursion's own <c>HitsTaken</c>, which is the number the
    /// condition marker, the block roll and the breathing rate all read.</para>
    /// </summary>
    /// <returns>The body's footnote to this beat, or null when it had nothing to say.</returns>
    private string? RestOneSeatedBeat(SurfaceExcursion ex, int beatsAlreadySat)
    {
        long watch = ex.CanteenWatch;
        ex.RestPipsEased.TryGetValue(watch, out int pipsSoFar);
        ex.RestHitsKnit.TryGetValue(watch, out int hitsSoFar);

        bool drink = APourInFrontOfYou;
        ShortRest.Eased eased = ShortRest.Beat(
            beatsAlreadySat, drink, pipsSoFar, hitsSoFar, ex.HitsTaken);

        if (eased.NervePips > 0)
        {
            ex.RestPipsEased[watch] = pipsSoFar + eased.NervePips;
            ApplyNerveRelief(eased.NervePips * NervePips.PipUnit);
        }
        if (eased.Hits > 0)
        {
            ex.RestHitsKnit[watch] = hitsSoFar + eased.Hits;
            ex.HitsTaken = Math.Max(0, ex.HitsTaken - eased.Hits);
        }

        return ShortRest.Line(in eased, drink);
    }

    /// <summary>#784 · The room's answer, with the body's footnote after it when the beat gave something
    /// back. One sentence and then one clause: the silence at your table is the EVENT (#757) and it keeps
    /// the lead, because a rest that pushed in front of the room's own answer would be the mechanic talking
    /// over the scene.</summary>
    private static string WithTheBodysFootnote(string saidByTheRoom, string? saidByTheBody) =>
        saidByTheBody is { Length: > 0 } ? $"{saidByTheRoom} {saidByTheBody}" : saidByTheRoom;

    // ── STANDING UP IS A DECISION ─────────────────────────────────────────────────────────────────────

    /// <summary>#784 · Whether the little stand-up confirm is up. Its own flag and not a mode on the table:
    /// it is a question ABOUT the table, and it has to be able to go away without the table going with
    /// it.</summary>
    private bool _standUpAsk;

    /// <summary>#784 · WASD in a chair. The keys are CONSUMED — nothing walks, nothing is queued, and the
    /// held-key set never learns the press happened — and the question goes up instead.
    ///
    /// <para>The whole point is the investment underneath: a stray key must not throw away the watch you
    /// spent being findable (#757) or the breath you have got back sitting there (#784). So the answer to
    /// "the captain pressed W while seated" is a sentence, not a step.</para></summary>
    private void AskWhetherToStandUp()
    {
        if (!_standUpAsk)
        {
            _standUpAsk = true;
            RendererInterop.PlayCue("reveal");
            StateHasChanged();
        }
    }

    /// <summary>Esc, the ✕, and the "stay where you are" button all come through here — keeping your seat is
    /// one act however you do it, and it is free.</summary>
    private void KeepYourSeat()
    {
        _standUpAsk = false;
        StateHasChanged();
    }

    /// <summary>The one press that stands you up. Through <see cref="CloseTable"/>, which is #757's own
    /// single way out of a table — a hand-written copy of standing up is this repo's first named bug class,
    /// and the day leaving a table costs something, it must cost it here too.</summary>
    private void StandUpFromTable()
    {
        _standUpAsk = false;
        if (_table is null)
        {
            return;
        }
        CloseTable();
        ShowPulseMessage(SeatedPosture.StoodUpToWalkLine);
        StateHasChanged();
    }

    /// <summary>Whether the confirm should say what standing up COSTS. Only when there is something left to
    /// lose — a captain who has taken everything a short rest has is not being warned about anything.</summary>
    private bool StandingUpCostsARest =>
        _surface is { } ex
        && CaptainIsRestingAtATable
        && (!ex.RestPipsEased.TryGetValue(ex.CanteenWatch, out int pips)
            || pips < ShortRest.NervePipCapPerWatch);

    // ── THE FIELD BOOK'S SECOND REGISTER ──────────────────────────────────────────────────────────────

    /// <summary>
    /// #784 · WRITE IT UP PROPERLY — the deliberate entry, and the posture is the gate.
    ///
    /// <para>Owner's law: <i>"writing things down requires sitting down to be properly done."</i> Standing,
    /// the book keeps taking exactly what it has always taken — the automatic gist-once jot #696 files when
    /// a document is photographed and left behind, which is untouched by this method and by this issue. What
    /// a table buys is not a better fact: it is the SHEET STILL BEING IN YOUR POCKET afterwards, which is
    /// the difference between a scrawl on a moving knee and a copy made in your own hand.</para>
    ///
    /// <para>The refusal is SAID (#603/#680): a control that does nothing and says nothing is
    /// indistinguishable from a bug, and this one is drawn everywhere so that the law can be learned by
    /// pressing it once.</para>
    /// </summary>
    private void WriteItUp(Core.Satchel.Item item)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // The gate, asked of Core. The client decides only what "seated" means; it never decides what
        // seated is FOR.
        if (SeatedPosture.RefusalIfStanding(CaptainIsSeated) is { } refusal)
        {
            SayItWhereTheyAreLooking(refusal);
            return;
        }

        // Whether there is anything to write is the SAME question the standing register asks (#696's own
        // "is there a gist" is asked once, by Core), so the two registers can never disagree about which
        // things are documents.
        string standing = WhereYouAreStanding();
        if (LeftBehind.GistOf(item, standing) is not { Length: > 0 } gist)
        {
            return;
        }

        string key = $"{item.Kind}:{item.Id}";
        if (!ex.WrittenUpProperly.Add(key))
        {
            SayItWhereTheyAreLooking(SeatedPosture.AlreadyWrittenLine);
            return;
        }

        // FileNote and not ShowAndFile: the saying happens on the surface the captain is actually looking
        // at, which with a table panel up is the panel and never the pulse under its blur (#680/#686).
        FileNote(gist, SeatedPosture.WriteGlyph);
        SayItWhereTheyAreLooking(SeatedPosture.WrittenUpLine(SatchelLabel(item), gist));
        RequestVaultSave();
    }

    /// <summary>Is the pen live on this row? A thing with no gist is not a document, and a document already
    /// in the book in your own hand is done — but a captain on their FEET still gets the control, because
    /// the refusal is how the law is taught.</summary>
    private bool CanWriteUp(Core.Satchel.Item item) =>
        _surface is { } ex
        && LeftBehind.GistOf(item, WhereYouAreStanding()) is { Length: > 0 }
        && !ex.WrittenUpProperly.Contains($"{item.Kind}:{item.Id}");

    /// <summary>What the pen's tooltip says — the hint when it will work, the refusal when it will not, so
    /// the price of a press is known before the press (#696's own discipline, one control over).</summary>
    private string WriteUpHint(Core.Satchel.Item item) =>
        !CanWriteUp(item) ? SeatedPosture.AlreadyWrittenLine
        : SeatedPosture.RefusalIfStanding(CaptainIsSeated) ?? SeatedPosture.WriteHint;
}
