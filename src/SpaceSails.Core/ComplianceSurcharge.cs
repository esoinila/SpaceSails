namespace SpaceSails.Core;

/// <summary>
/// #553 · THE PRICE OF A THING NOBODY WILL NAME. Owner, adding the last twist to the Reever-origin arc:
/// <i>"there was some prior incident that made legal research super slow and expensive and that incentivized
/// moving to hidden places"</i> — and then, on how to deliver it: <i>"Love that referenced but not described … it
/// adds depth 👌"</i>
///
/// <para><b>So the incident is never described, and the captain pays for it every time they do something
/// properly.</b> That is the whole design. There is no card explaining what happened, no console that will tell
/// you, no lore fragment that names it. There is a line item on a receipt, citing a clause, and the clause number
/// is the only fact anybody has. Forty years of paperwork shaped around a hole where the event was.</para>
///
/// <para><b>Why it lands on the HONEST road specifically.</b> Filing a wreck report is legal work; stripping her
/// and saying nothing is not. So the captain who does it properly pays the surcharge and the captain who does not
/// never sees it — which makes the incident a thing they FEEL rather than read, and gives the game's oldest
/// salvage rule (filing never out-earns stripping in cash) an in-fiction reason it never had. The regulation is
/// why the honest road pays worse. That is the same sentence as "the regulation is why the work moved into
/// mountains", said in credits.</para>
///
/// <para>The KAAMOS discipline, applied to an economy instead of to a moon: referenced constantly, described
/// never.</para>
/// </summary>
public static class ComplianceSurcharge
{
    /// <summary>
    /// The clause everything cites and nothing explains. One stable string across the whole game — a number that
    /// recurs on unrelated paperwork is how a player works out that the same event is behind all of it, and a
    /// number that drifted would read as set dressing instead of as a fact.
    /// </summary>
    public const string Clause = "cl. 14(b)";

    /// <summary>What it takes off a legitimate payment. Small enough to be an irritation rather than a
    /// punishment — it is a tax, not a penalty, and a tax you notice every time is worth more here than one that
    /// hurts once. FLAGGED for the owner's tuning.</summary>
    public const double Rate = 0.04;

    /// <summary>Whether this way of doing business attracts it. Only the legal road does — which is the joke, and
    /// the mechanic, and the reason the honest option has always paid worse.</summary>
    public static bool AppliesTo(Derelict.SalvageChoice choice) => choice == Derelict.SalvageChoice.FileTheReport;

    /// <summary>What it costs on a given gross. Rounded to whole credits, because it appears on a receipt.</summary>
    public static int AmountOn(int gross) =>
        gross <= 0 ? 0 : (int)System.Math.Round(gross * Rate);

    /// <summary>…and what is left after it.</summary>
    public static int Deduct(int gross) => System.Math.Max(0, gross - AmountOn(gross));

    // ── What it says, and what it refuses to say ──────────────────────────────────────────────────────

    /// <summary>The line item itself. States a number and a clause and nothing else — the entire delivery of the
    /// incident is this sentence, repeated for years.</summary>
    public static string LineItem(int amount) =>
        $"⚖ compliance surcharge ({Clause}) — {amount:N0} cr";

    /// <summary>
    /// WHAT HAPPENS WHEN A CAPTAIN ASKS. Nothing. Four different people give four different non-answers, none of
    /// which is evasive — they genuinely do not know, because nobody working today was working then and the
    /// clause has simply always been there. That is more unsettling than a cover-up, and it is cheaper: a
    /// conspiracy needs conspirators, and this needs only a form.
    /// </summary>
    public static IReadOnlyList<string> WhenAsked { get; } =
    [
        "The clerk turns the form round and points at the field. It is a printed field. It has always been a " +
        "printed field. \"It goes there,\" she says, which is true and is not an answer.",

        "\"Fourteen-b?\" The harbourmaster does not look up. \"Fourteen-b is fourteen-b. It was fourteen-b when " +
        "I got here and I have been here nineteen years.\"",

        "He checks the schedule, and then the schedule's own index, and then a third book that indexes the index. " +
        "The clause is cited in all of them. None of them says what it is for.",

        "\"You want the original instrument.\" A pause. \"Everyone wants the original instrument. The archive " +
        "reference is valid and the archive is not.\"",
    ];

    /// <summary>Which non-answer this captain gets, seeded so a given berth always answers the same way — an
    /// office that changed its story would be a conspiracy, and this is worse than a conspiracy.</summary>
    public static string AskAbout(string whereId) =>
        WhenAsked[(int)(DiceRule.Seed(0UL, $"cl14b:{whereId}") % (ulong)WhenAsked.Count)];

    /// <summary>The one summary the game ever offers, and it is an accountant's summary rather than a
    /// historian's. It says what the surcharge DOES and refuses the question of what it is for.</summary>
    public const string WhatItIsLine =
        "Every legitimate filing in the system carries it. Nobody can tell you what it is for, and nobody has " +
        "been able to for a long time — the clause is older than the people administering it. What everyone " +
        "knows is what it costs, and that doing it the other way costs nothing at all.";
}
