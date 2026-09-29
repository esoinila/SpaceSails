using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// #251 · Split from Map.Seated.cs, moved verbatim: #870 lane 6c's forwarders — the stand-up confirm and
// its presses, the stand before walking, the sit beat, and every seat read the rest of the page still asks
// for by name. The pour, the short rest, the field book's register and every field stay in Map.Seated.cs.
public partial class Map
{
    // -- #870 lane 6c · THE VERBS' FORWARDERS, AND EVERY ONE OF THEM HAS A CALLER OUTSIDE THIS FAMILY -
    //
    // Thirteen members moved onto Seating in this lane's Seated group, and these are the spellings the rest
    // of the page still asks for BY NAME -- measured, not assumed. The confirm and its three presses are the
    // razor and Map.Sim's Esc/Enter chains; the stand before walking is Map.Deck.Walk's two movement paths;
    // the sit beat is armed and spent by Map.Surface.Frame; the ladder and the two spread gates are read by
    // Map.Bin, Map.RedPen, Map.Surface.Satchel and the markup -- and by the field-book register lower down
    // THIS file, which is the shape a gate should have: the register is the page's, and it asks the seat.

    /// <inheritdoc cref="Seating.AskWhetherToStandUp"/>
    private void AskWhetherToStandUp() => _seating.AskWhetherToStandUp();

    /// <inheritdoc cref="Seating.KeepYourSeat"/>
    private void KeepYourSeat() => _seating.KeepYourSeat();

    /// <inheritdoc cref="Seating.StandUpFromTable"/>
    private void StandUpFromTable()
    {
        // #973 L3 · THE TABLE IS CLEARED BY STANDING UP. A pair laid at one seat is not a pair laid at the
        // next one — and the SPREAD is seated-only by law, so a laid pair that outlived the chair would be
        // state the player could not see, could not reach and could not put down.
        ClearTheSpread();

        // #1052 · …and the PAPER goes down with the chair, for the same sentence one surface over: the news
        // panel is seated-only by law, so a gate left set would be a paper the captain could not see, could
        // not reach and could not put down — and it would fan itself open again the next time he sat.
        CloseSeatedNews();
        _seating.StandUpFromTable();
    }

    /// <inheritdoc cref="Seating.StandUpBeforeWalking"/>
    /// <remarks>#1052 · …AND THE PAPER GOES DOWN HERE TOO. This is the OTHER road out of a chair — the one
    /// #847 gave a movement input, which reaches <c>Seating.StandUpFromTable</c> directly and so never
    /// passes the forwarder above. Found by playing it: click-to-walk off a bar top with the news open left
    /// the gate set, and the panel would have fanned itself open again at the next table the captain sat
    /// down at. <c>ThePaperIsOpen</c> already keeps it off the SCREEN — this keeps the state honest as
    /// well, which is the difference between a panel that is hidden and one that is shut.</remarks>
    private bool StandUpBeforeWalking()
    {
        bool stood = _seating.StandUpBeforeWalking();
        if (stood)
        {
            CloseSeatedNews();
        }

        return stood;
    }

    /// <inheritdoc cref="Seating.StandingUpCostsARest"/>
    private bool StandingUpCostsARest => _seating.StandingUpCostsARest;

    /// <inheritdoc cref="Seating.OweTheSitBeat"/>
    private void OweTheSitBeat() => _seating.OweTheSitBeat();

    /// <inheritdoc cref="Seating.SpendTheSitBeat"/>
    private void SpendTheSitBeat(double dtRealSeconds) => _seating.SpendTheSitBeat(dtRealSeconds);

    /// <inheritdoc cref="Seating.SeatedIn"/>
    private SeatedHud.Seat? SeatedIn => _seating.SeatedIn;

    /// <inheritdoc cref="Seating.SeatedAlone"/>
    private bool SeatedAlone => _seating.SeatedAlone;

    /// <inheritdoc cref="Seating.SeatedCustomerLine"/>
    private string? SeatedCustomerLine() => _seating.SeatedCustomerLine();

    /// <inheritdoc cref="Seating.CanSpreadTheCaseHere"/>
    private bool CanSpreadTheCaseHere => _seating.CanSpreadTheCaseHere;

    /// <inheritdoc cref="Seating.CaptainIsSeatedAnywhere"/>
    private bool CaptainIsSeatedAnywhere => _seating.CaptainIsSeatedAnywhere;

    /// <inheritdoc cref="Seating.SpreadRefusal"/>
    private string? SpreadRefusal => _seating.SpreadRefusal;

    // -- #870 lane 6b - THE FORWARDERS, AND THEY ARE TEMPORARY -----------------------------------------
    //
    // Fifteen members moved onto Seating in this lane, and every one of them was already being asked for BY
    // NAME - by the markup, by the cancel chain, by the surface tick, by the deck's own state hand-off.
    // Those call sites are not the subject of 6b, so not one of them changed: each name still answers here,
    // and each answer is one hop.
    //
    // They keep the accessibility they had, which is what proves nothing outside the family gained a reach
    // it did not have. #870's 6c is where this block is DELETED and the callers ask _seating directly - it
    // is one contiguous block for exactly that reason. Do not add a forwarder for a NEW question: add the
    // question to Seating and call it through _seating, the way this lane's own family verbs now do.

    /// <inheritdoc cref="Seating.CaptainIsSeated"/>
    public bool CaptainIsSeated => _seating.CaptainIsSeated;

    /// <inheritdoc cref="Seating.SeatedTable"/>
    private TableTalk? SeatedTable => _seating.SeatedTable;

    /// <inheritdoc cref="Seating.CaptainIsRestingAtATable"/>
    public bool CaptainIsRestingAtATable => _seating.CaptainIsRestingAtATable;

    /// <inheritdoc cref="Seating.SeatedOnABenchInTheOpen"/>
    private bool SeatedOnABenchInTheOpen => _seating.SeatedOnABenchInTheOpen;

    /// <inheritdoc cref="Seating.SeatedIsDocked"/>
    private bool SeatedIsDocked => _seating.SeatedIsDocked;

    /// <inheritdoc cref="Seating.SeatedIsAConversation"/>
    private bool SeatedIsAConversation => _seating.SeatedIsAConversation;

    /// <inheritdoc cref="Seating.SeatedWithCompany"/>
    private bool SeatedWithCompany => _seating.SeatedWithCompany;

    /// <inheritdoc cref="Seating.SeatedCompanyLine"/>
    private string? SeatedCompanyLine() => _seating.SeatedCompanyLine();

    /// <inheritdoc cref="Seating.SeatedOverheardLine"/>
    private string? SeatedOverheardLine() => _seating.SeatedOverheardLine();

    /// <inheritdoc cref="Seating.CaptainIsOnAStool"/>
    private bool CaptainIsOnAStool => _seating.CaptainIsOnAStool;

    /// <inheritdoc cref="Seating.SeatedStoolPlate"/>
    private string? SeatedStoolPlate => _seating.SeatedStoolPlate;

    /// <inheritdoc cref="Seating.TableMovesOnTheTable"/>
    private IReadOnlyList<Encounter.Move> TableMovesOnTheTable() => _seating.TableMovesOnTheTable();

    /// <inheritdoc cref="Seating.StoolMovesOnTheTable"/>
    private IReadOnlyList<Encounter.Move> StoolMovesOnTheTable() => _seating.StoolMovesOnTheTable();

    /// <inheritdoc cref="Seating.TheStandUpConfirmIsUp"/>
    private bool TheStandUpConfirmIsUp => _seating.TheStandUpConfirmIsUp;

    /// <inheritdoc cref="Seating.TheSitBeatIsSettling"/>
    private bool TheSitBeatIsSettling => _seating.TheSitBeatIsSettling;
}
