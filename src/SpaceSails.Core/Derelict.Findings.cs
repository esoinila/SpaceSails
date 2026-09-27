namespace SpaceSails.Core;

/// <summary>
/// #251 · WHAT A BOARDING PARTY FINDS — the bridge log, the manifest, the card's footing, the lifeboat
/// cradles, and the cause each cause is mistaken for.
///
/// <para>Split out of <c>Derelict.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered. Its two art paths are <c>const</c>s, folded at compile time.</para>
/// </summary>
public static partial class Derelict
{
    /// <summary>
    /// THE BRIDGE LOG — how long she has been out here, and the shape of her last hours. This is what turns
    /// "an old wreck" into "she was lost 31 years ago and nobody came".
    ///
    /// <para><b>It lives here, beside <see cref="Evidence"/>, because the two narrate the same ship and for a
    /// long time they disagreed.</b> The log finding was a private switch in the client with no arm for the
    /// vented hull, so it fell through to <i>"The last entries are ordinary ship's business, and then there
    /// are no more"</i> — on the one ship in the fleet whose evidence says, three feet away on the same
    /// screen, that <i>"the log runs on for months after that, in one immaculate hand, signing forty names on
    /// and off watch."</i> Two places narrating one fact is the bug even while they agree; here they did not.
    /// And the words the log was supposed to speak already existed, written and Core-tested, in
    /// <see cref="HullVenting.VentedShipLogLine"/> — read by nothing.</para>
    /// </summary>
    public static string LogFinding(in Wreck w) => "🖥 " + LogCaption(w);

    /// <summary>
    /// THE ONE PICTURE THE BRIDGE LOG IS ALLOWED TO HAVE — <b>one canvas, every hull, no exceptions</b>
    /// (#654).
    ///
    /// <para>There is no per-cause switch here and there must never be one. <see cref="WreckLayout"/>'s
    /// standing law is the owner's: <i>"even without reevers we should have those tech we used here
    /// available, to not give a clue that they might not be needed"</i> — ABSENCE OF A TOOL IS INFORMATION,
    /// and so is absence of a plate. A picture that appeared on the insurance job's log and nowhere else
    /// would tell the captain the ship was a staged loss <b>before they had read a word of it</b>, and the
    /// entire <see cref="MisreadsAs"/> mechanism is built on their not knowing that yet.</para>
    ///
    /// <para>So the painting is generic — a desk, a dark screen, a chair pushed back — and the CAPTION
    /// (<see cref="LogCaption"/>) carries every difference between the ten hulls. Exactly the arrangement
    /// <c>death-derelict.jpg</c> already uses for every derelict death regardless of what killed you.</para>
    /// </summary>
    public const string LogArtFile = "art/wreck-log.jpg";

    /// <summary>The bridge log's finding without its station glyph, so the reveal card can caption itself
    /// without printing the icon twice under a title that already carries it. One text, two readers.</summary>
    public static string LogCaption(in Wreck w) => w.Cause switch
    {
        // THE SECOND MADNESS, and the only hull whose log does not end. It is deliberately not prefixed with
        // the years — saying "the log ends N years ago" and then describing eleven months of entries after
        // the ending is the exact contradiction this seam was written to stop.
        WreckCause.VentedByOneOfTheirOwn =>
            $"She was opened to space {w.YearsAdrift:N0} years ago. " + HullVenting.VentedShipLogLine,

        _ => $"The log ends {w.YearsAdrift:N0} years ago. " + (w.Cause switch
        {
            WreckCause.DriveFailure =>
                "The last hundred entries are the same restart attempt, timestamped every twenty minutes, for nine days.",
            WreckCause.LifeSupportFailure =>
                "The entries stay calm, technical and hopeful right up until they stop mid-word.",
            WreckCause.Mutiny =>
                "The last week is written in two hands that stop acknowledging each other, then one hand only.",
            WreckCause.InsuranceJob =>
                "The distress call is in the log — drafted, revised, and SAVED four hours before the emergency it describes.",
            WreckCause.NavigationalError =>
                "The last entry is a burn confirmation for a burn the fuel logs say never fired.",
            WreckCause.Piracy =>
                "The last entry is a contact report. There is no entry after it.",
            // The hole through her took the crew before the alarm finished, so the record simply stops in the
            // middle of an ordinary watch — and the deck officer's hand does not change on the way.
            WreckCause.HullBreach =>
                "The last entry is a routine trim correction, half written. Nothing before it is out of the ordinary at all.",
            // A runaway is not sudden; it is announced, and then it is argued with. The log is the argument.
            WreckCause.ReactorCascade =>
                "The last four hours are containment readings logged every two minutes by three different " +
                "watchkeepers, each set worse than the last, and none of them written in a hand that has " +
                "given up.",
            // And the one the whole misreading turns on: barricades built from the inside look like a mutiny,
            // and the log has to be readable BOTH ways or the trap is not fair. It never names what got in.
            WreckCause.Infested =>
                "The last fortnight is watch officers logging compartments as SECURED, one at a time, aft to " +
                "forward, and never once saying against what. The final entry secures the compartment it is " +
                "written in.",
            _ => "The last entries are ordinary ship's business, and then there are no more.",
        }),
    };

    /// <summary>THE CARGO MANIFEST: what she was carrying and what it is worth — the number both endings are
    /// priced on. Beside the log for the same reason the log is beside the evidence.</summary>
    public static string ManifestFinding(in Wreck w) => "📦 " + ManifestCaption(w);

    /// <summary>THE CARGO MANIFEST'S ONE PICTURE, on the same all-ten-or-none law as
    /// <see cref="LogArtFile"/> — and here the law bites hardest, because this is the station where the
    /// fraud lives. The seals in the painting are neither intact nor re-set; they are just seals. Whether
    /// they were opened is a sentence, and the sentence is the captain's to read.</summary>
    public const string ManifestArtFile = "art/wreck-manifest.jpg";

    /// <summary>The manifest's finding without its station glyph — see <see cref="LogCaption"/>.</summary>
    public static string ManifestCaption(in Wreck w) =>
        $"The manifest assesses her cargo at {w.AssessedValueCr:N0} cr" + (w.Cause switch
        {
            WreckCause.InsuranceJob =>
                " — countersigned twice, by the same hand, and the cargo seals have been opened and re-set.",
            WreckCause.Piracy =>
                ". The near hold is empty; the deep hold is exactly as listed. Whoever boarded her was in a hurry.",
            // "Nobody has been here" was printed on this hull too, on a ship whose crew went on drawing
            // stores against this manifest for eleven months after they were dead. The stores are the tell,
            // and it is the captain's job to notice that the arithmetic is fiction.
            WreckCause.VentedByOneOfTheirOwn =>
                ". It is all still aboard — and the stores pages run on past the last of it, drawn down " +
                "week by week in the same steady hand, long after there was anybody to eat it.",
            _ => ". It is all still aboard. Nobody has come for it.",
        });

    /// <summary>The line under her name on the decision card: how long, how much, and the fact both roads are
    /// priced against. It said <i>"and nobody has been aboard since"</i> on every hull in the fleet —
    /// including <see cref="WreckCause.Piracy"/>, where the evidence two stations away is an airlock cycled
    /// from OUTSIDE and a hold stripped to bare frames, and including <see cref="WreckCause.Infested"/>,
    /// where something has been aboard the whole time. What is true of every wreck is not that nobody has
    /// been aboard; it is that nobody came back for her.</summary>
    public static string CardFooting(in Wreck w) =>
        $"Lost {w.YearsAdrift:N0} years. Cargo assessed at {w.AssessedValueCr:N0} cr, and nobody has come " +
        "back for it.";

    /// <summary>
    /// HOW MANY BOATS LEFT HER — the count you take off the wall. Gated by what killed her, because whether
    /// anybody got off is one of the loudest facts a wreck has, and the causes already disagree about it:
    ///
    /// <list type="bullet">
    /// <item><b>InsuranceJob</b> — every cradle empty, and the clamps neatly stowed rather than blown.
    /// Nobody left in a hurry. This is already what <see cref="Evidence"/> says about her.</item>
    /// <item><b>Infested / VentedByOneOfTheirOwn / LifeSupportFailure</b> — NONE. Nobody got off, which
    /// means everybody aboard is still aboard, and the away team is walking through the reason.</item>
    /// <item><b>Mutiny</b> — some. One side left; the other did not.</item>
    /// <item><b>Everything else</b> — seeded, because "did anyone make it" should not be predictable from
    /// the cause alone.</item>
    /// </list>
    ///
    /// <para>Deterministic off the wreck id, so the same hull always reads the same from the doorway.</para>
    /// </summary>
    public static int LifeboatsLaunched(string wreckId, WreckCause cause, int cradles)
    {
        switch (cause)
        {
            case WreckCause.InsuranceJob:
                return cradles;                 // all of them, and tidily
            case WreckCause.Infested:
            case WreckCause.VentedByOneOfTheirOwn:
            case WreckCause.LifeSupportFailure:
                return 0;                       // nobody got off
            case WreckCause.Mutiny:
                return System.Math.Max(1, cradles / 3);
            default:
                ulong h = StableHash.Of($"{wreckId}|cradles");
                return (int)(h % (ulong)(cradles + 1));
        }
    }

    /// <summary>What the row of cradles says, in the house voice: the count, never the conclusion. "Every
    /// one of them gone" and "not one of them launched" are the same sentence structure on purpose — the
    /// captain decides which is worse.</summary>
    public static string CradleReading(int launched, int cradles)
    {
        int left = cradles - launched;

        if (launched == 0)
        {
            return $"All {cradles} cradles are still occupied. Not one boat left this ship. Whatever " +
                   "happened aboard her, nobody aboard decided it was worth leaving over — or nobody was " +
                   "left who could.";
        }
        if (left == 0)
        {
            return $"All {cradles} cradles are empty, and the release clamps are stowed rather than blown. " +
                   "Everybody got off, and they did it without hurrying.";
        }
        return $"{launched} of {cradles} cradles are empty; {left} boat{(left == 1 ? " is" : "s are")} still " +
               "clamped in. Somebody left. Somebody did not.";
    }

    /// <summary>Some wrecks lie. A cause the evidence can be honestly MISREAD as — the wrong answer an
    /// investigator would reach in a hurry, which is what makes reading one a skill and not a lookup.
    /// Null when the wreck is unambiguous.</summary>
    public static WreckCause? MisreadsAs(WreckCause cause) => cause switch
    {
        // A staged loss is DRESSED as an ordinary failure — that is the entire point of staging it.
        WreckCause.InsuranceJob => WreckCause.DriveFailure,
        // A breach and a runaway both end in a hull nobody wants to stand in.
        WreckCause.ReactorCascade => WreckCause.HullBreach,
        // An intact ship with a dead crew reads as a mutiny to anyone who wants a story.
        WreckCause.LifeSupportFailure => WreckCause.Mutiny,
        // Barricades built from the INSIDE look exactly like a crew that fell out — and this is the most
        // expensive misreading in the game. File "mutiny" on an infested hull and the next crew goes in
        // unarmed on the strength of your report.
        WreckCause.Infested => WreckCause.Mutiny,
        // A ship that never burned looks like a ship that could not.
        WreckCause.NavigationalError => WreckCause.DriveFailure,
        // Doors shut, air gone: that reads as the air plant failing, and it reads that way easily. Unlike
        // every other entry here, this misreading is not a mistake the WRECK makes — the evidence is plain
        // at the valve board. It is the one the CAPTAIN is invited to make, because naming it truly means
        // naming a dead crewmember on circumstantial evidence, while the comfortable answer pays the same
        // and leaves the thing in her hold out of the record entirely.
        WreckCause.VentedByOneOfTheirOwn => WreckCause.LifeSupportFailure,
        _ => null,
    };
}
