namespace SpaceSails.Core;

/// <summary>
/// MAKING SURE — the third road off a wreck, for a captain who wants nothing from her except that she stop
/// existing.
///
/// <para>Owner, 2026-07-29: <i>"we could have a self-destruct option in the ship, like reactor overload …
/// in cases where we want to be sure."</i> The other two roads both assume you want something FROM her —
/// a fee and a contact, or the cargo. This one costs you every credit on the hull, which is the only
/// reason it can mean anything.</para>
///
/// <para><b>It never pays.</b> The moment certainty has a price in credits it stops being a decision and
/// becomes an exploit, and the fork already runs on "stripping pays more today". Certainty is the product.</para>
///
/// <para><b>And it is also the coverup move.</b> Ending a ship is how you end the story she carries. The
/// game does not get to judge which one the captain just did, and never says.</para>
///
/// <para>See <c>docs/features/making-sure.md</c>.</para>
/// </summary>
public static class Scuttle
{
    /// <summary>How a hull is ended. Four methods, and they are genuinely different choices — see
    /// <see cref="Available"/> for what gates each one, and <see cref="Speed"/> for who ever finds out.</summary>
    public enum Method
    {
        /// <summary>Set the reactor to run away with her. The ONLY method that works on a ship whose drive
        /// is dead, and the only one with a clock: you set it, and then you get off her.</summary>
        ReactorOverload,

        /// <summary>Point her in and let go. Years, no witnesses, no debris, no record anywhere but your
        /// own log — the most deniable thing a captain can do to a ship. Costs the most to arrange:
        /// falling into the Sun is one of the hardest things to do in a solar system.</summary>
        SunCourse,

        /// <summary>Drop her on a dead rock. Cheap, and she is still THERE — somebody with a good track and
        /// a shovel finds her one day, and everything you were ending is in the debris field.</summary>
        SurfaceCourse,

        /// <summary>Take her in too steep. Total and verifiable, and visible: a hull entering an atmosphere
        /// is a light somebody sees. Everyone knows something burned; nobody can say what.</summary>
        AtmosphereBurn,
    }

    /// <summary>How long the method takes to finish the job, in plain words. The reactor is the only one
    /// the away team is present for.</summary>
    public static string Speed(Method m) => m switch
    {
        Method.ReactorOverload => "minutes",
        Method.SunCourse => "years",
        Method.SurfaceCourse => "months",
        Method.AtmosphereBurn => "weeks",
        _ => "",
    };

    /// <summary>What the hull has to still be capable of. Gated by what KILLED her, so the cause taxonomy
    /// keeps earning its keep — a ship cannot be scuttled by the very system that failed in her.</summary>
    /// <param name="cause">What killed her.</param>
    /// <param name="rockInReach">A dead surface she could be aimed at.</param>
    /// <param name="atmosphereInReach">A body with air in it she could be taken into.</param>
    /// <param name="tanksAreGood">Enough left aboard her to arrange the hardest departure of all.</param>
    public static IReadOnlyList<Method> Available(
        Derelict.WreckCause cause, bool rockInReach, bool atmosphereInReach, bool tanksAreGood)
    {
        var methods = new List<Method>();

        // Her reactor has to still be a reactor. On a hull that already ran away with herself there is
        // nothing left to overload — she has done this.
        if (cause != Derelict.WreckCause.ReactorCascade)
        {
            methods.Add(Method.ReactorOverload);
        }

        // Everything else needs a drive that answers, which is precisely what a drive failure is not.
        bool driveAnswers = cause != Derelict.WreckCause.DriveFailure;
        if (driveAnswers && tanksAreGood)
        {
            methods.Add(Method.SunCourse);
        }
        if (driveAnswers && rockInReach)
        {
            methods.Add(Method.SurfaceCourse);
        }
        if (driveAnswers && atmosphereInReach)
        {
            methods.Add(Method.AtmosphereBurn);
        }

        return methods;
    }

    /// <summary>Why a method is not on the table. A greyed control that does not say why is a bug wearing a
    /// tooltip's clothes.</summary>
    public static string Refusal(Method m, Derelict.WreckCause cause) => m switch
    {
        Method.ReactorOverload when cause == Derelict.WreckCause.ReactorCascade =>
            "There is nothing here to overload. She has already done this, and the aft third is the proof.",
        Method.SunCourse when cause == Derelict.WreckCause.DriveFailure =>
            "The drive does not answer. It is the reason she is out here at all.",
        Method.SunCourse =>
            "Her tanks will not buy it. Falling into the Sun is the most expensive place to go in this " +
            "system, and she has been coasting for forty years.",
        Method.SurfaceCourse when cause == Derelict.WreckCause.DriveFailure =>
            "The drive does not answer. It is the reason she is out here at all.",
        Method.SurfaceCourse =>
            "Nothing dead and close enough to drop her on.",
        Method.AtmosphereBurn when cause == Derelict.WreckCause.DriveFailure =>
            "The drive does not answer. It is the reason she is out here at all.",
        Method.AtmosphereBurn =>
            "Nothing in reach with air in it to burn her up.",
        _ => "",
    };

    // ── The clock ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How long the overload gives the away team to be somewhere else.
    ///
    /// <para>Tuned to the WALK, not to drama: long enough to get from the machinery spaces aft to the lock
    /// at the bow at a walk, and short enough that the doors you dogged and the compartments you vented are
    /// suddenly your problem. Your own housekeeping is either a clear road out or a maze you built for
    /// yourself. FLAGGED for the owner's tuning.</para></summary>
    public const double OverloadSeconds = 90.0;

    /// <summary>Past this, the panel stops offering to stop it. The point of no return exists so that
    /// setting it is a decision and not a toggle.</summary>
    public const double AbortWindowSeconds = 60.0;

    /// <summary>Whether the countdown can still be called off.</summary>
    public static bool CanAbort(double secondsLeft) => secondsLeft > OverloadSeconds - AbortWindowSeconds;

    /// <summary>The line at the panel when the keys turn.</summary>
    public const string ArmedLine =
        "Both keys turn together and something under the deck plates changes its mind about what it is for. " +
        "The note of the ship comes up half a tone. You have about a minute and a half, and the way out is " +
        "the way you came in.";

    /// <summary>The line when a captain thinks better of it, while there is still a panel to think at.</summary>
    public const string AbortedLine =
        "You back the keys out. The note falls away and the ship goes back to being a dead thing you are " +
        "standing in. Nothing about her has improved.";

    /// <summary>Past the abort window and still aboard. Not a warning — a statement.</summary>
    public const string PastRecallLine =
        "The panel will not take the keys back. Whatever happens now happens on its own schedule.";

    // ── The ship says it herself ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE DEAD SHIP'S PA. Owner: <i>"it would likely talk through the ship comms."</i>
    ///
    /// <para>Of course it would — an overload announcement is not a courtesy, it is a safety system, and the
    /// one thing aboard still able to power a speaker is the exact thing that is about to fail. So a hull
    /// that has been silent for forty years starts talking, in a voice recorded by somebody long dead, to a
    /// crew that has not been aboard since.</para>
    ///
    /// <para>Flat, procedural, and addressed to ALL HANDS — which is the horror in it. It is not talking to
    /// the captain. It is doing the job it was installed to do, forty years late, for people who are not
    /// coming.</para>
    /// </summary>
    public static string? PaCall(double previousSeconds, double secondsLeft)
    {
        // Fires once, on the crossing.
        static bool Crossed(double from, double to, double mark) => from > mark && to <= mark;

        if (Crossed(previousSeconds, secondsLeft, OverloadSeconds - 0.01))
        {
            return "📢 ATTENTION. CONTAINMENT WITHDRAWAL INITIATED. ALL HANDS ABANDON. — the ship says it " +
                   "in a voice somebody recorded a long time ago, to a crew that has not been aboard since.";
        }
        if (Crossed(previousSeconds, secondsLeft, 60))
        {
            return "📢 SIXTY SECONDS. ALL HANDS ABANDON. MUSTER AT YOUR ASSIGNED BOAT. — the cradles she is " +
                   "telling them to muster at are the ones you have already counted.";
        }
        if (Crossed(previousSeconds, secondsLeft, 30))
        {
            return "📢 THIRTY SECONDS. — the same voice, at the same volume. It has no idea how this went.";
        }
        if (Crossed(previousSeconds, secondsLeft, 10))
        {
            return "📢 TEN SECONDS. CLEAR THE HULL. — you are the only thing aboard her that can.";
        }
        // THE COUNT IS WRONG. Owner: "like in the movie Spaceballs where it counted down and skipped one
        // number as a joke :-D" — an affectionate nod (Spaceballs, 1987, Mel Brooks / MGM), and nothing of
        // theirs is used: this is our ship, our failure, our line.
        //
        // It earns its place because it is not only a joke. A forty-year-old announcement system getting
        // the arithmetic wrong is the last thing a captain wants to hear while running, and it says
        // something true about the hull: HER SYSTEMS CANNOT BE TRUSTED, and the clock on your own suit
        // can. The real timer never wavers — only she does.
        if (Crossed(previousSeconds, secondsLeft, 5))
        {
            return "📢 THREE SECONDS. — she skipped one. Forty years cold and the count is wrong, and for " +
                   "half a step you believe her instead of your own clock.";
        }
        return null;
    }

    // ── What you hear ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE CONFIRMATION, AND IT NEVER TELLS YOU WHAT IT WAS. Owner: <i>"We should get some kind of pop-up
    /// with gen AI about hearing clawing of horrible sounds when we went :-D … a kind of confirmation that
    /// we did something that made a difference."</i>
    ///
    /// <para>Which is exactly right, and the shape has to be <b>confirmation without revelation</b>. The
    /// whole mechanic is built on an instrument that says something is warm and moving and has never once
    /// been able to say WHAT. If the ending finally showed you, it would spend the thing the entire lane
    /// was made of — and it would spend it at the exact moment the captain can no longer go and check.</para>
    ///
    /// <para>So: you hear it. Through the hull, through the shuttle's structure, on a channel nobody was
    /// transmitting on. You will know there was something, you will know it is not out here any more, and
    /// you will never know what you did.</para>
    /// </summary>
    public static string SheGoes(Method m, bool somethingWasStillAliveAboard)
    {
        string end = m switch
        {
            Method.ReactorOverload =>
                "She goes all at once and mostly inward — a white seam down her spine, and then she is a " +
                "shell and a slow expanding nothing, and your window is too bright to look at for a second " +
                "and a half.",
            Method.SunCourse =>
                "She lights her drive for eleven minutes and then never does anything again. In some number " +
                "of years she will stop existing, and nobody will be watching when she does.",
            Method.SurfaceCourse =>
                "She goes down over the terminator, and the flash on that dead grey ground is the last new " +
                "thing that will happen there for a long time.",
            Method.AtmosphereBurn =>
                "She comes in too steep on purpose. For about ninety seconds she is the brightest thing in " +
                "that sky, and then she is a long streak of nothing at all.",
            _ => "",
        };

        if (!somethingWasStillAliveAboard)
        {
            return end + " The hull channel stays quiet the whole way. Whatever was aboard her had already " +
                   "gone out with the air, and you did this to an empty ship.";
        }

        // The payoff. Heard, never seen; named, never identified.
        return end +
               "\n\nAnd on the way — before she went, on the hull channel nobody was transmitting on — you " +
               "hear it. Clawing. Not a scrabble: a deliberate, unhurried WORKING at something, the sound " +
               "of a thing that has opened doors before trying the one nearest to it, and then the one " +
               "after that, and then, much faster, the one after that.\n\nIt stops when she does.\n\n" +
               "You will not find out what it was. You will not have to.";
    }

    /// <summary>What the nerve gets back for being certain. The only relief in the wreck lane that cannot
    /// be bought in a bar — and it is deliberately larger when there WAS something, because that is the
    /// captain finally being allowed to stop wondering. FLAGGED for tuning.</summary>
    public static int Relief(bool somethingWasStillAliveAboard) => somethingWasStillAliveAboard ? 3 : 1;

    /// <summary>What she was worth, and what you are giving up by being certain. Shown on the panel BEFORE
    /// the keys turn, because a cost the captain finds out about afterwards is not a cost, it is a
    /// punishment.</summary>
    public static string PriceLine(long assessedValue, long feeIfFiled) =>
        $"There is {assessedValue:N0} cr of cargo in her holds and {feeIfFiled:N0} cr of finder's fee in " +
        "her paperwork. Neither survives this. Nobody pays you to be sure.";
}
