using System.Globalization;

namespace SpaceSails.Core;

// Subject: the masked hull, tell (e) — the one tell that is prose. What a hull says back when the captain
// keys the tight-beam, and why the two answers are the same length of nothing.

/// <summary>
/// #534 slice 2 · <b>HOW SHE ANSWERS A HAIL.</b>
///
/// <para>Slice 1 built four tells and every one of them is a number, because <see cref="QShip"/> may not
/// publish prose: <i>"the scope reports a burn, the telescope reports a radiator, and the captain does the
/// arithmetic or does not."</i> The fifth tell is the exception the rule was written around — it is a
/// SENTENCE, and a sentence cannot be a member of a type whose whole law is that it holds no strings. So it
/// lives here instead, one file over, and <c>QShip</c> stays exactly as string-free as its own guard says it
/// is.</para>
///
/// <para><b>The two answers are canon</b> (owner's thread, 2026-09-05) and are reproduced below verbatim.
/// They are not two flavours of the same reply — they differ in the ORDER a master puts things in:</para>
///
/// <list type="bullet">
///   <item><b>An honest merchant</b> — <see cref="AWorkingMasterAnswers"/>. Identity, cargo, destination,
///   then the question. That is a working master's order: he tells you who he is and what he is doing
///   before he asks you anything, because on his side of the radio this is an interruption to a job.</item>
///   <item><b>The masked hull</b> — <see cref="AProcedureABeatTooClean"/>. The right words in the wrong
///   order: intent demanded BEFORE identity given, an acknowledgement rather than a greeting, and
///   <i>hold your vector</i> — which is a warship's instruction to a contact and a phrase no hauler in this
///   system has ever had cause to say. Nothing in it is a confession; all of it is procedure, and procedure
///   is exactly what a merchant crew does not have.</item>
/// </list>
///
/// <para><b>Still no verdict.</b> This is a fifth tell and not an answer: the game never labels the line, never
/// colours it, never puts a plate beside it. It reads on her file next to four numbers, and the captain does
/// the sum or does not — the same discipline #533 states and #534's slice 1 enforced. Individually deniable,
/// like all the others: a nervous master, a naval reservist, a hull that has been boarded before.</para>
///
/// <para><b>Who has nobody to answer.</b> A pod is unmanned — there is no microphone to key, so it answers
/// nothing at all (the same fact <c>EncounterRule.ComplianceOf</c> states as <c>NothingToComply</c>). And an
/// off-books hauler who is honestly what she says does not give her destination away over the air for free:
/// where she is going is the intel economy's own goods (F6/F7), bought at the dark web and not had for the
/// price of a hail. Both cases return null, and the page falls back to the two sentences it has said to them
/// since long before this issue. A MASKED hull answers whether or not she files, because her line names no
/// port and so gives none away.</para>
/// </summary>
public static class QShipHail
{
    /// <summary>
    /// The honest merchant's answer, canon and verbatim; <c>{0}</c> is her callsign off the record and
    /// <c>{1}</c> the name of the port she is bound for. Identity, cargo, destination, then the question.
    /// </summary>
    public const string AWorkingMasterAnswers = "{0} here — bulk, bound {1}. What do you want?";

    /// <summary>
    /// The masked hull's answer, canon and verbatim; <c>{0}</c> is her callsign off the record. She names no
    /// port because she has no business with one, and the sentence needs none to be wrong.
    /// </summary>
    public const string AProcedureABeatTooClean = "{0} acknowledges. State your intent and hold your vector.";

    /// <summary>
    /// What this hull says back, or null if there is nobody aboard to say it and nothing she will say.
    /// Pure and deterministic per hull — the answer is <see cref="QShip.IsMasked"/> composed with the two
    /// fields the record already carries, so asking twice always agrees and the tell cannot drift between a
    /// desk and a dossier.
    /// </summary>
    /// <param name="ship">The hull being hailed.</param>
    /// <param name="destinationName">The port she is bound for, in the name a captain would use for it —
    /// the page's own body name, because Core does not know which ephemeris is loaded.</param>
    public static string? AnswerTo(NpcShip ship, string destinationName)
    {
        if (ship.IsPod)
        {
            return null;
        }

        if (QShip.IsMasked(ship))
        {
            return string.Format(CultureInfo.InvariantCulture, AProcedureABeatTooClean, ship.Callsign);
        }

        return ship.PublishesTimetable
            ? string.Format(CultureInfo.InvariantCulture, AWorkingMasterAnswers, ship.Callsign, destinationName)
            : null;
    }
}
