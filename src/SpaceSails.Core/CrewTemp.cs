namespace SpaceSails.Core;

/// <summary>
/// THE CREW'S REPORT ON THE CAPTAIN. Owner: <i>"let's have a Winningtemp-like crew satisfaction report on
/// the captain's desk … it is the captain's performance AS SEEN BY THE CREW. You already listed reasons the
/// crew might complain — too honest a captain takes their winnings. The crew satisfaction reports could
/// guide the player into seeking the right balance for his crew's greed/honesty ratio, that keeps him in the
/// captain's seat. Too dissatisfied crew will likely replace the captain at some point. Pirates of the
/// Caribbean has great lore about all the ways. A pistol with one shot onto a deserted island with lots of
/// rum :-D"</i>
///
/// <para>THE ONE MARKER CONSTRAINT, which he named himself (<i>"even though we move as one marker
/// everywhere"</i>): there are no individual crew aboard yet. So this is not a roster of opinions — it is a
/// TEMPERATURE, aggregate by construction, exactly like the tool it is named after. That turns out to be the
/// honest shape anyway: a real employee survey is anonymous and summarised, and nobody signs the complaint
/// that gets read out.</para>
///
/// <para>WHAT MAKES IT A GAME RATHER THAN A SCORE: honesty toward the LAW and honesty toward the CREW are
/// different virtues, and only one of them pays. File the true cause of a wreck and the crew lose the fee.
/// Strip her and lie and they eat well — and the heat that follows is also theirs. So there is no setting of
/// the dial that satisfies every line of the report, which is the owner's "right balance" made mechanical
/// rather than announced.</para>
///
/// <para>And it obeys the game's standing law: the report never tells the captain what he IS. It tells him
/// what they said. The conclusion is his to draw and he is allowed to be wrong about himself.</para>
/// </summary>
public static class CrewTemp
{
    /// <summary>What the crew are asked about. Named as a crew would name it, not as HR would.</summary>
    public enum Dimension
    {
        /// <summary>The share. The one dimension an honest captain cannot win.</summary>
        Pay,

        /// <summary>Whether the captain gets people hurt, and whether he is careful with the ship.</summary>
        Safety,

        /// <summary>Whether his word holds — TO THEM. Nothing to do with what he tells the authorities.</summary>
        TheWord,

        /// <summary>Rum, a bunk, a run ashore. Cheap to give and conspicuous to withhold.</summary>
        Rest,

        /// <summary>Whether this is going anywhere. A crew will forgive a bad month and not a pointless one.</summary>
        Prospects,
    }

    /// <summary>The heading each dimension gets on the sheet.</summary>
    public static string Heading(Dimension d) => d switch
    {
        Dimension.Pay => "THE SHARE",
        Dimension.Safety => "GETTING HOME",
        Dimension.TheWord => "THE CAPTAIN'S WORD",
        Dimension.Rest => "REST AND RUM",
        Dimension.Prospects => "WHERE WE ARE GOING",
        _ => "",
    };

    /// <summary>
    /// Everything the crew form an opinion from. Deliberately plain aggregates: this is a pure function of
    /// the voyage so far, so a reload cannot re-roll the crew's mood and a test can state any ship's report
    /// exactly.
    /// </summary>
    /// <param name="HonestFilings">Wreck causes filed truthfully when a lie would have paid better. The
    /// crew know what those cost them.</param>
    /// <param name="ProfitableLies">Causes filed falsely, or hulls stripped and never reported.</param>
    /// <param name="SharePaid">Credits actually distributed, in whatever unit the ledger keeps.</param>
    /// <param name="PromisesKept">Word given to the crew and honoured — a run ashore, a share, a rescue.</param>
    /// <param name="PromisesBroken">The same, not honoured. Weighted far heavier: this is the one that ends
    /// captaincies.</param>
    /// <param name="CrewLost">People who did not come home.</param>
    /// <param name="NearMisses">Times it nearly went wrong and everyone got back — which CUTS both ways, see
    /// <see cref="Cohesion"/>.</param>
    /// <param name="TotsPoured">The galley's own counter. Cheap, and they notice.</param>
    /// <param name="DaysSinceShoreLeave">How long since anybody stood on something that was not the ship.
    /// STILL DORMANT after #1066, and deliberately so: the game counts STOPS, not days — see
    /// <see cref="WorkingStopsBetweenRunsAshore"/> and <see cref="ShoreLeavePromisesBroken"/>, which reach
    /// this sheet through <see cref="PromisesBroken"/> instead. Turning a stop count into a day count to
    /// move REST would be a number invented to fill a bar, which is the one thing this record refuses to
    /// do.</param>
    /// <param name="Heat">What the authorities think of them, which the crew carry too.</param>
    /// <param name="VentedOwnCompartment">The captain opened one of HER compartments with somebody in it.
    /// Recorded separately because it is not a shade of anything — it is its own fact.</param>
    public readonly record struct Voyage(
        int HonestFilings = 0,
        int ProfitableLies = 0,
        long SharePaid = 0,
        int PromisesKept = 0,
        int PromisesBroken = 0,
        int CrewLost = 0,
        int NearMisses = 0,
        int TotsPoured = 0,
        int DaysSinceShoreLeave = 0,
        int Heat = 0,
        bool VentedOwnCompartment = false);

    /// <summary>
    /// COHESION, and why it is not satisfaction. Owner, on soldiers: <i>"often soldiers report that they only
    /// care about the other people fighting beside them, so there probably would be a bit of that also."</i>
    ///
    /// <para>Shared hardship SURVIVED buys tolerance. It does not make the crew happy — a hard voyage is
    /// still hard — it makes them slower to act on being unhappy. Which is how a monstrous captain keeps a
    /// devoted crew, and why a decent captain who has never been through anything with them is the one who
    /// gets marooned first.</para>
    /// </summary>
    public static int Cohesion(in Voyage v) =>
        System.Math.Clamp((v.NearMisses * 6) - (v.CrewLost * 4), 0, 30);

    /// <summary>A dimension's reading: where it sits, and the line the crew wrote under it.</summary>
    /// <param name="Score">0…100, in whole points. Coarse on purpose — see <see cref="Band"/>.</param>
    public readonly record struct Reading(Dimension Dimension, int Score, string Comment);

    /// <summary>How a reading is spoken about. The number exists for ordering; the BAND is what is shown,
    /// because a crew does not have an opinion to the nearest percent — the same reason the nerve gauge is
    /// quantised into pips.</summary>
    public enum Band
    {
        Mutinous,
        Sour,
        Grumbling,
        Content,
        Devoted,
    }

    public static Band BandOf(int score) => score switch
    {
        < 20 => Band.Mutinous,
        < 40 => Band.Sour,
        < 60 => Band.Grumbling,
        < 80 => Band.Content,
        _ => Band.Devoted,
    };

    public static string BandWord(Band b) => b switch
    {
        Band.Mutinous => "mutinous",
        Band.Sour => "sour",
        Band.Grumbling => "grumbling",
        Band.Content => "content",
        Band.Devoted => "devoted",
        _ => "",
    };

    private static int Clamp(int n) => System.Math.Clamp(n, 0, 100);

    /// <summary>
    /// THE SHARE, and the one line an honest captain cannot win.
    ///
    /// <para>This is the owner's whole point made arithmetic: every honest filing is money the crew did not
    /// get, and they can count. A captain who never lies runs a poor ship, and a poor ship is a sour one,
    /// however clean his conscience.</para>
    /// </summary>
    private static Reading Pay(in Voyage v)
    {
        int score = Clamp(45
                          + (int)System.Math.Min(35, v.SharePaid / 400)
                          + (v.ProfitableLies * 5)
                          - (v.HonestFilings * 7));

        string comment = score switch
        {
            < 20 => "We are the only pirates in the belt who file paperwork for a living. The captain sleeps " +
                    "well and we do not eat.",
            < 40 => "Another one filed honest. That fee went somewhere and it was not into our hands.",
            < 60 => "The share is a share. Nobody is buying a moon with it.",
            < 80 => "We are doing all right. Ask again after the next hull.",
            _ => "Best-paid berth any of us has had. Nobody is asking where it came from.",
        };

        return new(Dimension.Pay, score, comment);
    }

    /// <summary>Getting home. Heat counts here too — a hunted ship is a dangerous berth whatever the pay.</summary>
    private static Reading Safety(in Voyage v)
    {
        int score = Clamp(70 - (v.CrewLost * 18) - (v.Heat * 3) - (v.VentedOwnCompartment ? 25 : 0));

        string comment = v.VentedOwnCompartment
            ? "He opened a compartment with one of us inside it. We all keep a suit closer than we used to."
            : score switch
            {
                < 20 => "We are being hunted and we are short-handed. Both of those are decisions somebody made.",
                < 40 => "Too many empty bunks and too much attention. It is not the work, it is the odds.",
                < 60 => "It has been rough. Rough is the job. Ask us again if it gets rougher.",
                < 80 => "He brings us back. That counts for more than people think.",
                _ => "Nobody has been lost. On a ship like this that is not luck, it is the captain.",
            };

        return new(Dimension.Safety, score, comment);
    }

    /// <summary>
    /// His word — TO THEM. The dimension that has nothing to do with what he tells the authorities, and the
    /// one that actually ends captaincies. Broken promises are weighted three times a kept one, because that
    /// is how trust behaves.
    /// </summary>
    private static Reading TheWord(in Voyage v)
    {
        int score = Clamp(60 + (v.PromisesKept * 8) - (v.PromisesBroken * 24));

        string comment = score switch
        {
            < 20 => "He says a thing and we look at each other. That is no way to run a ship.",
            < 40 => "He has told us one thing and done another often enough that we count now.",
            < 60 => "He mostly means it. Mostly.",
            < 80 => "He has never lied to us. What he tells the depot is his own business.",
            _ => "If he says it, it happens. That is the whole reason anyone signed.",
        };

        return new(Dimension.TheWord, score, comment);
    }

    /// <summary>Rum and a run ashore. Cheap to give, conspicuous to withhold — the dimension a captain can
    /// always fix and often forgets.</summary>
    private static Reading Rest(in Voyage v)
    {
        int score = Clamp(55 + System.Math.Min(25, v.TotsPoured * 3) - (v.DaysSinceShoreLeave / 7));

        string comment = score switch
        {
            < 20 => "Nobody has stood on anything that was not this ship for longer than anyone can remember, " +
                    "and the locker is dry.",
            < 40 => "A tot would not kill him. Neither would a night in a port with a door on it.",
            < 60 => "We have had worse. We have also had better, and recently.",
            < 80 => "The locker is open often enough. That is most of it, honestly.",
            _ => "Rum, a bunk and shore when it is due. Whatever else he is, he is not mean with it.",
        };

        return new(Dimension.Rest, score, comment);
    }

    /// <summary>Whether this is going anywhere. A crew forgives a bad month; they do not forgive a pointless
    /// one.</summary>
    private static Reading Prospects(in Voyage v)
    {
        // NEAR MISSES DELIBERATELY DO NOT APPEAR HERE. They were feeding this line, which would have made
        // shared hardship raise satisfaction — and the whole point of separating cohesion from satisfaction
        // is that surviving something together does NOT make a crew happy about it. It makes them slower to
        // act. The test for that claim is what caught me putting it in.
        int score = Clamp(50 + (v.ProfitableLies * 4) + (v.HonestFilings * 2)
                          - (v.DaysSinceShoreLeave / 10));

        string comment = score switch
        {
            < 20 => "We are flying in circles burning her reactor for nothing. Somebody should say so.",
            < 40 => "There is no plan anybody has heard. There may be one.",
            < 60 => "We are working. It is not obvious where it ends up.",
            < 80 => "Feels like we are building toward something. Nobody wants to jinx it.",
            _ => "Whatever he is doing, it is working, and we would rather be here than anywhere.",
        };

        return new(Dimension.Prospects, score, comment);
    }

    /// <summary>The full sheet, in the order it is read.</summary>
    public static IReadOnlyList<Reading> Readings(in Voyage v) =>
    [
        Pay(v), Safety(v), TheWord(v), Rest(v), Prospects(v),
    ];

    /// <summary>The one number at the top — a mean, so no single dimension can hide behind the others, and
    /// so a captain who is superb at four things and appalling at one still reads as a problem.</summary>
    public static int Overall(in Voyage v)
    {
        IReadOnlyList<Reading> rs = Readings(v);
        int sum = 0;
        foreach (Reading r in rs)
        {
            sum += r.Score;
        }
        return sum / rs.Count;
    }

    // ── #1066 · Shore leave, as a ledger line ─────────────────────────────────────────────────────────

    /// <summary>
    /// #1066 · HOW MANY WORKING STOPS THE ARTICLES ALLOW BETWEEN RUNS ASHORE. <b>FLAGGED for the owner's
    /// tuning</b> — this is the dial, and it is worth knowing what it currently decides: at four, a captain
    /// working the Mars/Venus/Luna circuit breaks his word on the fourth berth in a row that has no town on
    /// the other end of the gangway, and every berth after that breaks it again.
    ///
    /// <para>Raising it makes shore leave a formality; dropping it to one makes it a leash, and the crew
    /// would be right to say so. Nothing else in the game reads it.</para>
    /// </summary>
    public const int WorkingStopsBetweenRunsAshore = 4;

    /// <summary>
    /// #1066 · WHAT AN OVERDUE RUN ASHORE COSTS THE CAPTAIN'S WORD.
    ///
    /// <para><see cref="Dimension.TheWord"/> names the promises it is made of and this one is first on the
    /// list — <i>"a run ashore, a share, a rescue"</i>. So shore leave does not reach the sheet as a mood or
    /// a second dial: it reaches it as <see cref="Voyage.PromisesBroken"/>, weighted three times a kept
    /// promise, on the one dimension that <i>"actually ends captaincies"</i>. Which is exactly the edge
    /// <c>StoryBeats.Beat.CrewMeeting</c> sits on.</para>
    ///
    /// <para><b>It compounds rather than latching.</b> The stop that passes the line breaks the word once,
    /// and every stop the crew are kept aboard past it breaks it again — that is how an overdue thing
    /// behaves, and it is what makes the meeting cost more than one missed berth. And it RESETS, because
    /// this sheet is a temperature and not a court record: <i>"it is what the crew said when he was not in
    /// the room"</i>, and what they say after a night ashore is different from what they said before it.</para>
    /// </summary>
    /// <param name="workingStopsSinceShoreLeave">Consecutive berths with no run ashore. A negative count —
    /// a save written under a different dial — is not a debt.</param>
    public static int ShoreLeavePromisesBroken(int workingStopsSinceShoreLeave) =>
        System.Math.Max(0, workingStopsSinceShoreLeave - WorkingStopsBetweenRunsAshore + 1);

    /// <summary>
    /// #1066 / #761 · What the sheet prints about the counter, so the captain is told BEFORE the promise
    /// breaks and not only after. It states where the line is and where he stands against it, and — like
    /// every other line on this sheet — it never tells him what to do about it.
    /// </summary>
    public static string ShoreLeaveLine(int workingStopsSinceShoreLeave)
    {
        int stops = System.Math.Max(0, workingStopsSinceShoreLeave);
        int broken = ShoreLeavePromisesBroken(stops);

        if (broken > 0)
        {
            return $"A run ashore is overdue by {broken} berth{(broken == 1 ? "" : "s")}. " +
                   "They are counting, and it is on his word that it lands.";
        }

        if (stops == 0)
        {
            return "They were ashore at the last port. Nobody is counting.";
        }

        int left = WorkingStopsBetweenRunsAshore - stops;
        return $"{stops} working stop{(stops == 1 ? "" : "s")} since anybody stood on something that was not " +
               $"this ship. The articles say a run ashore every {WorkingStopsBetweenRunsAshore} — " +
               $"{left} to go.";
    }

    // ── What happens when it goes bad ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// HOW CLOSE THE CREW ARE TO ACTING. Owner: <i>"too dissatisfied crew will likely replace the captain at
    /// some point. Pirates of the Caribbean has great lore about all the ways. A pistol with one shot onto a
    /// deserted island with lots of rum :-D"</i>
    ///
    /// <para>Escalation is the whole point, because a mutiny that arrives without warning is a dice roll
    /// wearing a story. The crew grumble, then ask, then tell, and only then act — and the report on the desk
    /// IS the warning at every step. A captain who is surprised was not reading it.</para>
    /// </summary>
    public enum Standing
    {
        /// <summary>They would follow him into a reactor.</summary>
        Solid,

        /// <summary>Talk. It is only talk, and it is not quiet.</summary>
        Grumbling,

        /// <summary>A deputation, and a specific thing they want fixed. This is the last cheap moment.</summary>
        Petition,

        /// <summary>An ultimatum with a date on it. Fixable, expensively.</summary>
        Ultimatum,

        /// <summary>The articles are read back to him. One shot, one island, and as much rum as they can
        /// spare — which by long tradition is a great deal, and is not a kindness.</summary>
        Marooning,
    }

    /// <summary>Where the crew stand, cohesion included: shared hardship does not make them happy, it makes
    /// them SLOWER TO ACT on being unhappy.</summary>
    public static Standing StandingOf(in Voyage v)
    {
        int effective = System.Math.Clamp(Overall(v) + Cohesion(v), 0, 100);

        return effective switch
        {
            < 18 => Standing.Marooning,
            < 30 => Standing.Ultimatum,
            < 42 => Standing.Petition,
            < 58 => Standing.Grumbling,
            _ => Standing.Solid,
        };
    }

    /// <summary>What the sheet says at the top, so the escalation is never a surprise.</summary>
    public static string StandingLine(Standing s) => s switch
    {
        Standing.Solid => "The crew would sail with him again. Nobody is saying it out loud, which is how you know.",
        Standing.Grumbling => "Talk in the galley, and it stops when he walks in. That is the stage where it is free to fix.",
        Standing.Petition => "Three of them came to the door together and asked for one specific thing. Give it to them.",
        Standing.Ultimatum => "They have named a date. After it, they say, they will settle it themselves.",
        Standing.Marooning => "They have read the articles back to him. There is an island, a pistol with one " +
                              "shot in it, and more rum than a man can drink — which is the point of the rum.",
        _ => "",
    };

    /// <summary>The one thing to fix first: the lowest reading, because that is how a temperature survey is
    /// actually used — not as a grade, as a pointer.</summary>
    public static Reading WorstOf(in Voyage v)
    {
        IReadOnlyList<Reading> rs = Readings(v);
        Reading worst = rs[0];
        foreach (Reading r in rs)
        {
            if (r.Score < worst.Score)
            {
                worst = r;
            }
        }
        return worst;
    }

    /// <summary>The sheet's own header. Named the way a shipping company would name it, which is half the
    /// joke and all of the tone.</summary>
    public const string SheetTitle = "CREW TEMPERATURE — CONFIDENTIAL, UNSIGNED";

    /// <summary>Printed under the title, once, so the captain knows what he is holding.</summary>
    public const string SheetFooter =
        "Collected below decks and handed up unsigned, as the articles require. It is not a judgement of the " +
        "captain. It is what the crew said when he was not in the room.";
}
