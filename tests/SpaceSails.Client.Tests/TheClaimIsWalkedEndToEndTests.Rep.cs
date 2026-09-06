using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1151 slice 2 · <b>LODGING IT WITH HIM, IN PERSON</b> — the same form, the other host.
///
/// <para>What this part owns is the arm of the scene that goes through a man: the offer made before the
/// pitch, the counter opening on HIS card, the third press landing at his elbow, and the money arriving at
/// the next meeting rather than at this one. The rule half of the offer — when it stands and what it says
/// — is Core's, in <c>TheClaimIsLodgedWithHimTests</c>; what CANNOT be asked there is that the two hosts
/// run ONE implementation of the three presses, which is a fact about the page and is asked here.</para>
///
/// <para>The comparison that makes that meaningful is <c>THE_SAME_NumberAndTheSameNextMeeting…</c>: two
/// identical worlds diverging only in where the third press landed, compared field for field. It reads the
/// kiosk walk from <c>…Counter.cs</c> and the bench from <c>…World.cs</c> — one partial class, one
/// game.</para>
/// </summary>
public sealed partial class TheClaimIsWalkedEndToEndTests
{
    // ══ 4 · #1151 SLICE 2 · LODGING IT WITH HIM, IN PERSON ═══════════════════════════════════════════════

    /// <summary>
    /// <b>THE WHOLE SCENE AT A TABLE.</b> The hull goes at a berth with the captain ashore; he never walks to
    /// a machine; a representative finds him, offers to take it before he pitches, and the SAME three presses
    /// happen on his card. The claim is lodged, still pays nothing, and the money arrives at the next
    /// meeting — the same number the machine would have quoted, because it is the same function quoting it.
    ///
    /// <para>Every step goes through the game's own doors: <c>HisPitchGoesUp</c> is his meeting,
    /// <c>LodgeItWithHim</c> is the button on his card, and the rows are <c>TheClaimAsks</c>' own.</para>
    /// </summary>
    [Fact]
    public void THE_CASTAWAY_TakesTheRepsOfferAndTheThreePressesHappenAtHisTable()
    {
        Pages.Map map = Boot();
        ClampAtThePort(map);
        double now = (double)Read(map, "SimTime")!;
        PirateInsurance policy = NebulaRep.PolicyAfterBuying(InsuranceTier.Premium, now);
        Set(map, "_insurance", policy);
        int purse = (int)Read(map, "_credits")!;

        ArmHerCharges(map);
        WalkHimAshore(map);
        RunUntilSheGoes(map);

        var wire = (IReadOnlyList<NewsWire.NewsEvent>)Read(map, "_newsEvents")!;
        NewsWire.NewsEvent receipt = wire.First(e => e.Kind == NewsWire.NewsEventKind.HullLostAtABerth);

        // ── HE OFFERS, AND THE COUNTER IS NOT UP YET. The line is on the card; nothing has been pressed.
        Invoke(map, "HisPitchGoesUp");
        Assert.True((bool)Read(map, "TheLodgingOfferIsUp")!, "he had a loss on his wire and said nothing.");
        Assert.Null(Read(map, "TheDeskSays"));
        Assert.Null(Read(map, "_claimDesk"));

        // ── ACCEPTED. The counter opens on HIS card and raises no card of its own — a ViewObject over a man
        // standing at your table is the stacked card #777 named.
        Invoke(map, "LodgeItWithHim");
        Assert.Equal(NebulaClaims.OnApproach, (string?)Read(map, "TheDeskSays"));
        Assert.Null(Read(map, "_viewObject"));
        Assert.False((bool)Read(map, "TheClaimDeskIsUp")!, "the kiosk's card claimed a counter it is not hosting.");
        Assert.False((bool)Read(map, "TheLodgingOfferIsUp")!, "he is still offering a form you are filling in.");

        // ── THE THREE PRESSES, on his card, off the same rows the machine deals.
        Assert.Equal(0, PressesTaken(map));
        Press(map, new NebulaClaims.Ask(NebulaClaims.Press.Hull, "AURORA QUEEN", "AURORA QUEEN"));
        Assert.Equal(0, PressesTaken(map));       // out of order, and refused exactly as at a wall
        PressTheWholeCounter(map);

        // ── LODGED. His card now carries the machine's receipt, and the purse has not moved. And still no
        // ViewObject: three presses at his elbow raise no console card over him, which is the half of the
        // #777 law a check taken before the first press could not see.
        Assert.Null(Read(map, "_viewObject"));
        Assert.Equal(NebulaClaims.Presses, PressesTaken(map));
        Assert.Equal(NebulaClaims.LodgedLine, (string?)Read(map, "TheDeskSays"));
        Assert.Equal(1, (int)Read(map, "_claimsLodged")!);
        Assert.Equal(purse, (int)Read(map, "_credits")!);
        Assert.Equal(receipt.Subject, (string?)Read(map, "_lodgingOfferedFor"));

        object owed = Read(map, "_claimOwed") ?? throw new InvalidOperationException("nothing was lodged.");
        int payout = (int)Get(owed, "PayoutCr")!;
        Assert.Equal(InsuranceRule.HullClaimPayoutCr(policy, (double)Read(map, "SimTime")!), payout);
        Assert.True(payout > 0, "a Premium policy's claim is worth nothing — the guard cannot fail.");

        // ── AND THE MONEY ARRIVES AT THE NEXT MEETING, not at this one. He watched you fill it in; he did
        // not open the till.
        Invoke(map, "CloseTheRepsCard");
        Invoke(map, "HisPitchGoesUp");
        Assert.Equal(purse + payout, (int)Read(map, "_credits")!);
        Assert.Equal(NebulaClaims.RepAtThePayout, (string?)Read(map, "_repSaid"));
        Assert.Null(Read(map, "_claimOwed"));

        // …and he does not then offer to file the hull he has just paid for.
        Assert.False((bool)Read(map, "TheLodgingOfferIsUp")!);
        Invoke(map, "CloseTheRepsCard");
        Invoke(map, "HisPitchGoesUp");
        Assert.False((bool)Read(map, "TheLodgingOfferIsUp")!, "the offer came back on a hull already paid for.");
        Assert.Equal(purse + payout, (int)Read(map, "_credits")!);
    }

    /// <summary>
    /// <b>THE LINE IS SAID ONCE PER LOSS.</b> He offers at the meeting where the wire is fresh; the captain
    /// walks away without answering; and at the next meeting he does not ask again. The other half is the
    /// same world with the offer TAKEN, which is the guard above — so this one cannot be green because he
    /// never offers at all.
    /// </summary>
    [Fact]
    public void THE_OFFER_IsMadeOncePerLossAndNotAgainAtTheNextMeeting()
    {
        Pages.Map map = TheCastawayWithALossOnTheWire();

        Invoke(map, "HisPitchGoesUp");
        Assert.True((bool)Read(map, "TheLodgingOfferIsUp")!);

        Invoke(map, "CloseTheRepsCard");           // he is left standing there with the form in his hand
        Invoke(map, "HisPitchGoesUp");
        Assert.False((bool)Read(map, "TheLodgingOfferIsUp")!, "he asked twice about one hull.");
        Assert.Null(Read(map, "_claimDesk"));
    }

    /// <summary>
    /// <b>AND A CLAIM LODGED AT A MACHINE SPENDS HIS OFFER TOO.</b> The two hosts file the same form, so a
    /// salesman who offered to take a hull already lodged at a wall would be the firm not knowing its own
    /// paperwork — and, since the payout is per claim, a purse that fills by walking to a bar.
    /// </summary>
    [Fact]
    public void A_CLAIM_LodgedAtTheMachineIsNotSomethingHeThenOffersToFile()
    {
        Pages.Map map = TheCastawayWithALossOnTheWire();
        WalkToTheKiosk(map);
        LodgeAWholeClaim(map);
        Invoke(map, "CloseViewObject");

        Invoke(map, "HisPitchGoesUp");             // paid here, so nothing is owed at the meeting after
        Assert.False((bool)Read(map, "TheLodgingOfferIsUp")!);
        Invoke(map, "CloseTheRepsCard");
        Invoke(map, "HisPitchGoesUp");
        Assert.False((bool)Read(map, "TheLodgingOfferIsUp")!, "he offered to file a hull already filed and paid for.");
        Assert.Equal(1, (int)Read(map, "_claimsLodged")!);
    }

    /// <summary>
    /// <b>ONE FORM, TWO HOSTS.</b> Two presses at a machine on the concourse, then a man at a table — and it
    /// is the SAME form: the counter picks up where it was left, the third press lands at his elbow, and one
    /// claim comes out of it. Two implementations would give the captain two counters and two claims here.
    /// </summary>
    [Fact]
    public void A_FORM_BegunAtAMachineIsTheFormHeIsWatchingYouFinish()
    {
        Pages.Map map = TheCastawayWithALossOnTheWire();
        WalkToTheKiosk(map);
        Invoke(map, "ViewNearbyObject");

        Press(map, TheRows(map)[0]);                                                   // the policy
        Press(map, TheRows(map).Single(r => r.Offer == (string)Invoke(map, "ShipNameNow")!));  // her name
        Assert.Equal(2, PressesTaken(map));

        Invoke(map, "CloseViewObject");
        Invoke(map, "HisPitchGoesUp");
        Assert.True((bool)Read(map, "TheLodgingOfferIsUp")!);
        Invoke(map, "LodgeItWithHim");

        // Not restarted. He is holding the form with two presses already on it.
        Assert.Equal(2, PressesTaken(map));
        Assert.Equal(NebulaClaims.OnApproach, (string?)Read(map, "TheDeskSays"));

        Press(map, TheRows(map)[0]);                                                   // the wire entry
        Assert.Equal(NebulaClaims.Presses, PressesTaken(map));
        Assert.Equal(1, (int)Read(map, "_claimsLodged")!);
        Assert.NotNull(Read(map, "_claimOwed"));
    }

    /// <summary>
    /// <b>THE SAME NUMBER, THE SAME COUNTER, THE SAME NEXT MEETING.</b> Two identical worlds diverging only
    /// in WHERE the third press landed. Everything downstream of it is compared field for field, because
    /// "shared by construction" is a claim about behaviour and this is the only way to ask it.
    /// </summary>
    [Fact]
    public void THE_SAME_NumberAndTheSameNextMeetingWhicheverHostTookThePress()
    {
        Pages.Map atAWall = TheCastawayWithALossOnTheWire();
        WalkToTheKiosk(atAWall);
        LodgeAWholeClaim(atAWall);
        Invoke(atAWall, "CloseViewObject");

        Pages.Map atATable = TheCastawayWithALossOnTheWire();
        Invoke(atATable, "HisPitchGoesUp");
        Invoke(atATable, "LodgeItWithHim");
        PressTheWholeCounter(atATable);

        Assert.Equal((int)Read(atAWall, "_claimsLodged")!, (int)Read(atATable, "_claimsLodged")!);
        Assert.Equal(
            (int)Get(Read(atAWall, "_claimOwed")!, "PayoutCr")!,
            (int)Get(Read(atATable, "_claimOwed")!, "PayoutCr")!);
        Assert.Equal(
            (string)Get(Read(atAWall, "_claimOwed")!, "HullName")!,
            (string)Get(Read(atATable, "_claimOwed")!, "HullName")!);

        int wallPurse = (int)Read(atAWall, "_credits")!;
        int tablePurse = (int)Read(atATable, "_credits")!;
        Invoke(atAWall, "HisPitchGoesUp");
        Invoke(atATable, "CloseTheRepsCard");
        Invoke(atATable, "HisPitchGoesUp");
        Assert.Equal((int)Read(atAWall, "_credits")! - wallPurse, (int)Read(atATable, "_credits")! - tablePurse);
        Assert.True((int)Read(atATable, "_credits")! - tablePurse > 0, "neither world paid — the guard cannot fail.");
        Assert.Equal((string?)Read(atAWall, "_repSaid"), (string?)Read(atATable, "_repSaid"));
    }

    /// <summary>
    /// <b>THE UNDERWRITER ON THE REGOLITH MAKES THE SAME OFFER.</b> Brem Kolt is the same firm and takes the
    /// same form — the offer rule asks nothing about WHICH of them is at the table, because a rule about
    /// that would be the mirrored constant said about a person. He does not pay: the money arrives when a
    /// representative next finds you, and that is Harlan Fess's meeting.
    /// </summary>
    [Fact]
    public void KOLT_OffersTheSameFormAndTheMoneyStillArrivesAtFesssMeeting()
    {
        Pages.Map map = TheCastawayWithALossOnTheWire();

        Invoke(map, "KoltsPitchGoesUp");
        Assert.True((bool)Read(map, "TheLodgingOfferIsUp")!, "the underwriter read the wire and said nothing.");
        Invoke(map, "LodgeItWithHim");
        PressTheWholeCounter(map);

        Assert.Equal(1, (int)Read(map, "_claimsLodged")!);
        int purse = (int)Read(map, "_credits")!;
        Invoke(map, "CloseTheHardcasesCard");
        Assert.Equal(purse, (int)Read(map, "_credits")!);   // he closed his case and paid nobody

        Invoke(map, "HisPitchGoesUp");
        Assert.True((int)Read(map, "_credits")! > purse, "Fess never paid what Kolt took.");
        Assert.Equal(NebulaClaims.RepAtThePayout, (string?)Read(map, "_repSaid"));
    }

    /// <summary>
    /// <b>A FINISHED FORM GOES WITH THE CONVERSATION; A HALF-FILLED ONE DOES NOT.</b> The counter is the
    /// page's state and not the fixture's, so walking away mid-form and coming back — to him or to a wall —
    /// finds the same two presses. A lodged one is business the firm has closed.
    /// </summary>
    [Fact]
    public void A_HALF_FilledFormOutlivesTheMeetingAndALodgedOneDoesNot()
    {
        Pages.Map map = TheCastawayWithALossOnTheWire();
        Invoke(map, "HisPitchGoesUp");
        Invoke(map, "LodgeItWithHim");
        Press(map, TheRows(map)[0]);
        Assert.Equal(1, PressesTaken(map));

        Invoke(map, "CloseTheRepsCard");
        Assert.Equal(1, PressesTaken(map));                 // he keeps the form; so does the machine

        Invoke(map, "HisPitchGoesUp");
        PressTheWholeCounter(map);
        Assert.Equal(NebulaClaims.Presses, PressesTaken(map));
        Invoke(map, "CloseTheRepsCard");
        Assert.Null(Read(map, "_claimDesk"));
    }

    /// <summary>
    /// <b>AND THE OFFER IS DRAWN BEFORE THE PITCH, ON BOTH CARDS.</b> Read off the composed page, because
    /// the sim can say the offer stands and the markup can still put it under the buttons that dismiss the
    /// card — the third named bug class exactly, and #761's telling law failing without a symptom a test of
    /// the state could see.
    /// </summary>
    [Fact]
    public void THE_OFFER_IsDrawnAboveEveryLineTheRepsOpenWith()
    {
        string page = MapMarkup.Text;

        int fessOffer = page.IndexOf("NebulaClaims.LodgeWithMe", StringComparison.Ordinal);
        int fessPitch = page.IndexOf("@pitch.Line", StringComparison.Ordinal);
        Assert.True(fessOffer >= 0 && fessPitch > fessOffer,
            "Harlan Fess pitches before he mentions the hull you lost.");

        int koltOffer = page.IndexOf("NebulaClaims.LodgeWithMe", fessOffer + 1, StringComparison.Ordinal);
        int koltOpener = page.IndexOf("HardcaseRep.Opener", StringComparison.Ordinal);
        Assert.True(koltOffer >= 0 && koltOpener > koltOffer,
            "Brem Kolt opens on hazard before he mentions the hull you lost.");

        // …and all three cards reach the counter through the page's OWN members — read off Map.razor itself,
        // where the invocations are, because the composed text above has spliced each surface in over them.
        // This is what makes the two hosts one implementation rather than two that agree today.
        string invocations = File.ReadAllText(MapMarkup.PagePath);
        foreach (string door in new[] { "PressTheClaim=\"@PressTheClaim\"", "TheClaimAsks=\"@TheClaimAsks\"" })
        {
            Assert.Equal(3, CountOf(invocations, door));   // the kiosk's card, Fess's card, Kolt's card
        }

        // …and all three then draw the rows by CALLING those same members, rather than one of them growing a
        // list of its own that happens to look the same.
        Assert.Equal(3, CountOf(page, "in TheClaimAsks())"));
        Assert.Equal(3, CountOf(page, "PressTheClaim("));
    }

    private static int CountOf(string text, string needle)
    {
        int found = 0;
        for (int at = text.IndexOf(needle, StringComparison.Ordinal);
             at >= 0;
             at = text.IndexOf(needle, at + 1, StringComparison.Ordinal))
        {
            found++;
        }

        return found;
    }
}
