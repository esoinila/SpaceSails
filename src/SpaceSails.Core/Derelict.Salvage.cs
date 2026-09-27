namespace SpaceSails.Core;

/// <summary>
/// #251 · THE DECISION AT THE WRECK — the <c>Wreck</c> record, the salvage choices, <c>Resolve</c>, and the
/// assessor who countersigns an honest filing (#652).
///
/// <para>Split out of <c>Derelict.cs</c> under #251 as a pure move: two runs of the base file, no member
/// renamed, re-scoped or re-ordered. The #652 banner, <c>ContactGoodwill</c> and the <c>Assessors</c>
/// table stay in the opening file: <c>Assessors</c> is a <c>static readonly</c>, and every static field of
/// this class stays there in its original order (#1163).</para>
/// </summary>
public static partial class Derelict
{
    /// <summary>One found wreck: who she was, what she was carrying, what happened, and how long she has
    /// been out here. <paramref name="AssessedValueCr"/> is the cargo's assessed worth — the number both
    /// endings are priced against.</summary>
    public readonly record struct Wreck(
        string Id,
        string ShipName,
        WreckCause Cause,
        int AssessedValueCr,
        double YearsAdrift)
    {
        /// <summary>The volume she could be hiding in, given the years — see
        /// <see cref="SearchConeRadiusMeters"/>.</summary>
        public double SearchConeMeters => SearchConeRadiusMeters(YearsAdrift);
    }

    /// <summary>What the captain decided to do about her.</summary>
    public enum SalvageChoice
    {
        /// <summary>File the accident report: the finder's fee, a bonus for reading her right, and a contact
        /// who remembers you were straight with them.</summary>
        FileTheReport,

        /// <summary>Strip her and say nothing. Everything, today — and it is all hot.</summary>
        StripAndSayNothing,
    }

    /// <summary>What a decision actually pays. <paramref name="ContactEarned"/> is the part that outlives
    /// the payout: an honest filing makes somebody who will take your call later (owner: <i>"contacts that
    /// may provide in future or fast win immediately"</i>).
    ///
    /// <para>#938 D4 · <paramref name="SurchargeCr"/> is what cl. 14(b) took out of this payment, and it is
    /// reported rather than recomputed for the same reason every other number in this file is: the receipt
    /// must show the money that is actually missing, not a second arithmetic that agrees with the first
    /// until one of them is edited. Zero on the quiet road, which is the whole joke (#553).</para></summary>
    public readonly record struct SalvageOutcome(
        int CreditsNow,
        int HeatGained,
        bool ContactEarned,
        bool CargoIsHot,
        string Line,
        int SurchargeCr = 0);

    /// <summary>
    /// Price the captain's decision.
    ///
    /// <para>FILE THE REPORT pays <see cref="ReportFeeFraction"/> of the assessed value, plus
    /// <see cref="CorrectCauseBonusFraction"/> when <paramref name="reportedCause"/> matches what actually
    /// happened, plus <see cref="FraudBountyFraction"/> when the captain correctly names a staged loss —
    /// an underwriter pays real money for a claim they no longer have to honour. It earns a contact and
    /// takes no heat. Naming the WRONG cause still pays the finder's fee (you did find her) but earns no
    /// bonus and no contact: a bad report is worse than no report.</para>
    ///
    /// <para>STRIP AND SAY NOTHING pays the whole assessed value immediately and earns nothing else. The
    /// cargo is insured, so it is hot (<see cref="QuietSalvageHeat"/>), and a wreck that was never found
    /// makes no friends.</para>
    ///
    /// <para>Pure: same wreck + same choice + same reported cause → same outcome, always.</para>
    /// </summary>
    public static SalvageOutcome Resolve(in Wreck wreck, SalvageChoice choice, WreckCause? reportedCause)
    {
        int value = System.Math.Max(0, wreck.AssessedValueCr);

        if (choice == SalvageChoice.StripAndSayNothing)
        {
            return new SalvageOutcome(
                CreditsNow: value,
                HeatGained: QuietSalvageHeat,
                ContactEarned: false,
                CargoIsHot: true,
                Line: $"The {wreck.ShipName} was never found. Nobody thanks you, and nobody comes looking — yet.");
        }

        // #553 · AND THE HONEST ROAD PAYS THE SURCHARGE. Filing is legal work, so it attracts cl. 14(b) — the
        // line item nobody can explain, off the back of an incident nobody will describe. It is also the
        // in-fiction reason this road has always paid worse than stripping her: the regulation that drove the
        // work into mountains is the same regulation the captain is paying for here, in credits, on a receipt.
        // #938 D4: the gross is kept so the receipt can say what was taken off it. Four per cent came out of
        // every filed report since #553 and no surface in the game ever named it — the captain saw a smaller
        // number and no reason for it, which is the one shape this design must NOT have. The incident stays
        // undescribed; the deduction does not stay invisible.
        int feeGross = (int)System.Math.Round(value * ReportFeeFraction);
        int fee = ComplianceSurcharge.Deduct(feeGross);
        bool readRight = reportedCause == wreck.Cause;

        if (!readRight)
        {
            return new SalvageOutcome(
                CreditsNow: fee,
                HeatGained: 0,
                ContactEarned: false,
                CargoIsHot: false,
                Line: $"You filed on the {wreck.ShipName}, and the fee cleared. The finding did not survive review — " +
                      "a wrong cause helps nobody, and they will remember that too.",
                SurchargeCr: ComplianceSurcharge.AmountOn(feeGross));
        }

        bool fraud = wreck.Cause == WreckCause.InsuranceJob;
        int bonusGross = (int)System.Math.Round(value * (fraud ? FraudBountyFraction : CorrectCauseBonusFraction));
        int bonus = ComplianceSurcharge.Deduct(bonusGross);

        return new SalvageOutcome(
            CreditsNow: fee + bonus,
            HeatGained: 0,
            ContactEarned: true,
            CargoIsHot: false,
            Line: fraud
                ? $"You filed on the {wreck.ShipName} and named it staged. An underwriter who was about to pay out " +
                  "instead pays YOU, and will want to know what else you have seen."
                : $"You filed on the {wreck.ShipName} and read her right. The fee cleared, the finding stood, and " +
                  "somebody now owes you a straight answer.",
            SurchargeCr: ComplianceSurcharge.AmountOn(feeGross) + ComplianceSurcharge.AmountOn(bonusGross));
    }

    /// <summary>Who countersigned THIS finding. Pure and seeded from the hull's own name, so the same wreck
    /// always produces the same assessor — a captain who files on the <i>Maren Vey</i> twice does not meet
    /// two different people, and a test can state the answer exactly.</summary>
    public static (string Id, string Name) ContactFor(in Wreck wreck)
    {
        ulong seed = DiceRule.Seed("salvage-contact", 0L);
        seed = DiceRule.Seed(seed, wreck.Id.Length > 0 ? wreck.Id : wreck.ShipName);
        return Assessors[(int)(seed % (ulong)Assessors.Length)];
    }

    /// <summary>What the captain is told, at the moment the filing clears. It names the person and stops —
    /// it does not promise a mechanic, because the mechanic is that somebody is now in the book.</summary>
    public static string ContactLine(string name) =>
        $"🤝 {name} countersigned the finding and put their own name under yours. Nobody does that for a " +
        "captain they expect to hear about again in the wrong way.";

    /// <summary>The two roads, stated plainly for the choice card — the honest one and the quiet one.
    /// Quotes the actual numbers so the captain is never guessing what they are choosing between.</summary>
    public static string DescribeChoice(in Wreck wreck, SalvageChoice choice)
    {
        int value = System.Math.Max(0, wreck.AssessedValueCr);
        return choice switch
        {
            SalvageChoice.FileTheReport =>
                $"File it: {(int)System.Math.Round(value * ReportFeeFraction):N0} cr finder's fee, more if you read her " +
                "right, and somebody who takes your call afterwards.",
            SalvageChoice.StripAndSayNothing =>
                $"Strip her: {value:N0} cr today, all of it hot, and the {wreck.ShipName} stays lost.",
            _ => "",
        };
    }
}
