namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// THE CAPTAIN'S CROSSINGS — filed since docs/features/the-captains-character.md §3, built here.
//
// The owner said "morality map" and then "moral-o-meter", and §1 of that document ruled the first one
// right: a scalar cannot express *he has never once lied to the crew and he vented a compartment with a
// man in it*, and a number the player can watch move is a number the player will grind. So the source
// of truth is A LEDGER OF CROSSINGS — what line, in what situation, at what cost, and who saw — and any
// read-out is derived from it and never stored.
//
// #973 L5a writes the FIRST entry the ledger ever gets, and it is the one the document could not have
// guessed at: not a compartment, not a wreck report, not a promise — the captain's own face. Somebody
// who served with him asks what happened to it, and he tells them the truth, or the salesman's
// sentence, or something about a reactor seal on a run he was never on. Who he said it to is the
// WITNESS field the document calls "the whole hinge of §3", and it is filled by construction: an
// explanation is only a crossing because there was somebody to hear it.
//
// IT STAYS A LEDGER. There is no score on this type, no total, no derived tier, and no method that
// returns a number about the captain's character. One row per crossing, and the Captain's desk shows
// the rows. That absence is the feature.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>The captain's-character ledger (the-captains-character.md §3): an append-only list of the
/// lines he crossed, what it bought, and who was standing there. A map, never a meter.</summary>
public static class CaptainCrossings
{
    /// <summary>Which line was crossed. One today, and the enum exists so the next one is an addition
    /// rather than a rewrite — the document names four more that are already decision points in the code.</summary>
    public enum Kind
    {
        /// <summary>#973 L5a · Somebody who knew the old face asked what happened to it, and the captain
        /// answered. The line is honesty, and the situation is that the honest answer is unbelievable.</summary>
        OwnFaceExplained = 0,
    }

    /// <summary>The words the ledger row leads with. The line, in the vocabulary §3 asks for — a principle,
    /// not an event.</summary>
    public static string Line(Kind kind) => kind switch
    {
        _ => "what you told somebody who knew the face",
    };

    /// <summary>
    /// One crossing.
    /// </summary>
    /// <param name="Kind">Which line.</param>
    /// <param name="Situation">What was on the table — for the face, which of the three answers was given.
    /// Stored as the answer's own name so the row survives the words being rewritten.</param>
    /// <param name="Witness">Who saw. §3's hinge, and never empty for a crossing of this kind.</param>
    /// <param name="SimTime">When.</param>
    public readonly record struct Crossing(Kind Kind, string Situation, string Witness, double SimTime)
    {
        /// <summary>The opaque row the vault stores — the FACT, never the sentence.</summary>
        public string Stored =>
            $"{(int)Kind}|{Esc(Situation)}|{Esc(Witness)}|{SimTime.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}";

        /// <summary>Read one back; an unparsable row is dropped rather than thrown over.</summary>
        public static bool TryParse(string? stored, out Crossing crossing)
        {
            crossing = default;
            if (string.IsNullOrEmpty(stored))
            {
                return false;
            }

            string[] p = stored.Split('|', 4);
            if (p.Length != 4
                || !int.TryParse(p[0], out int kind) || !Enum.IsDefined((Kind)kind)
                || !double.TryParse(p[3], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double at))
            {
                return false;
            }

            crossing = new Crossing((Kind)kind, Unesc(p[1]), Unesc(p[2]), at);
            return true;
        }

        private static string Esc(string s) => s.Replace('|', '│');

        private static string Unesc(string s) => s.Replace('│', '|');
    }

    /// <summary>The face-scene's crossing, built from the answer and the person who heard it.</summary>
    public static Crossing OwnFace(OldCrewScene.Answer answer, string witnessDisplayName, double simTime) =>
        new(Kind.OwnFaceExplained, answer.ToString(), witnessDisplayName ?? "", simTime);

    /// <summary>Which answer a stored own-face row records, or null for a row this build cannot read as one.
    /// The derivation the ledger DOES allow: reading back what was written, never scoring it.</summary>
    public static OldCrewScene.Answer? AnswerOf(Crossing crossing) =>
        crossing.Kind == Kind.OwnFaceExplained
        && Enum.TryParse(crossing.Situation, out OldCrewScene.Answer answer)
            ? answer
            : null;

    /// <summary>Append a crossing. Nothing is deduped and nothing is summed: two answers to two people are
    /// two crossings, and that is what the book is for.</summary>
    public static IReadOnlyList<Crossing> Add(IReadOnlyList<Crossing> ledger, Crossing crossing)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        return [.. ledger, crossing];
    }

    /// <summary>The row the Captain's desk shows: the line, what was said, and to whom.</summary>
    public static string Row(Crossing crossing)
    {
        string said = AnswerOf(crossing) is { } answer
            ? OldCrewScene.Button(answer)
            : crossing.Situation;
        return crossing.Witness.Length > 0
            ? $"{Line(crossing.Kind)}: {said} — to {crossing.Witness}"
            : $"{Line(crossing.Kind)}: {said}";
    }

    /// <summary>The section heading the desk prints over the rows.</summary>
    public const string Heading = "⚖ Crossings";
}
