namespace SpaceSails.Core;

/// <summary>
/// #537 slice 3 · CLIMBING IN. Owner, on the best use of a smuggler's hole:
/// <i>"we smuggle our selves past inspection at that hot ship 😎"</i>, and before that, in the sentence that
/// filed the lane: <i>"the search tool could create an E-interaction to break into the place or hide."</i>
///
/// <para>Slices 1 and 2 made the void a PRIZE: knock, force, take. This makes it a PLACE. A found void that a
/// captain can only be handed the contents of is a chest; a found void a captain can fold into and pull the
/// plate to behind them is the scene the owner actually asked for — boots on your own deck plating while
/// somebody unhurried opens every hatch you own.</para>
///
/// <h3>The hiding is not a special case. It is the walls doing their job.</h3>
/// <para>#324's law — <i>the maze must be law for everyone</i> — is why this needed almost no new rule.
/// <see cref="InspectionTeam.Sees"/> already ends in <c>SurfaceCollision.HasLineOfSight</c>, so a captain
/// behind a fitted plate is unseen by geometry rather than by a flag, exactly as the shut cubicle (#821)
/// hides a captain from a round by appending a wall rather than by inventing a stealth stat. That is the
/// whole of the hiding, and it is free.</para>
///
/// <h3>So the DESIGN is entirely in what gives you away</h3>
/// <para>If nothing did, a captain who found a void would simply be immune to the sweep, and the scene
/// #538 built would stop being one. Three things do, and each is a fact about the world rather than a die:</para>
/// <list type="number">
/// <item><b>You are not in it.</b> Standing in a corridor, you are seen exactly as you always were. Named
/// here rather than left implicit so the law has a case that must go wrong.</item>
/// <item><b>They watched you get in.</b> A lamp on the plate at the moment it closes is the cubicle's own
/// rule (<c>CubicleLock.WaitsAtTheDoor</c>): a professional who saw the door shut does not need to see
/// through it. Wait for the cone to pass; do not climb in under it.</item>
/// <item><b>THE CUT IS STILL WARM.</b> The plate is pulled to over a hole you made with a rig ten seconds
/// ago — bright metal, slag, and heat coming off it. A sweeper whose lamp lands on that plate inside
/// <see cref="CutStaysWarmSeconds"/> opens it. Past that it is one more scarred wall on a scarred ship.</item>
/// </list>
///
/// <para><b>Why the warm cut is the good one.</b> It makes the interesting mistake possible and legible: a
/// captain who hears a boat mate on and cuts a hole to hide in has made things worse, because the cut is the
/// evidence. Cut early, go quiet, and let it cool — which is advice a player can work out from the words
/// alone, and then feel clever about. It is a clock, so it is deterministic, and the clock is shown.</para>
///
/// <para><b>And noise is deliberately NOT on the list.</b> It does not need to be: a racket already sends
/// them to the PLACE (<c>AlertSweepersToNoise</c> → <c>Investigating</c>), which puts a lamp on the plate,
/// which asks the warm-cut question. Making noise a fourth tell would double-count the one thing the sweep
/// already models best, and would take the payoff off a cold cut — which is the reward this whole rule is
/// built to hand out.</para>
///
/// <para>Pure and deterministic, like everything else in Core.</para>
/// </summary>
public static class HullStowage
{
    // ── WHETHER A CAPTAIN FITS ──────────────────────────────────────────────────────────────────────────

    /// <summary>Whether there is room in there for a person, and if not, why not.</summary>
    public enum Fit
    {
        /// <summary>A section of the shielding band: <c>WreckLayout.ShieldingDepth</c> deep, which is a
        /// body and a half. Tight, and a person folds.</summary>
        Fits,

        /// <summary>A technical run inside a bulkhead: <c>WreckLayout.BulkheadDepth</c> — 1.2 du against a
        /// 1.4 du body. Papers and a rack of keys go in there. You do not.</summary>
        TheRunIsTooNarrow,
    }

    /// <summary>
    /// IS THIS ONE BIG ENOUGH TO BE IN? And the answer is the owner's own heuristic, already sitting in
    /// <see cref="HullSounding.VoidFor"/> as the reason there are two kinds of hiding place at all:
    /// <i>"Still a room with a wall to technical space is a good bet on large enough hiding space."</i>
    ///
    /// <para>A bet, not a rule — so a captain who wants somewhere to BE has a reason to prefer one clue over
    /// another, and sometimes forces a plate and finds a slot he cannot get into. That refusal is the
    /// mechanic, not a shortfall in it: the alternative is every void being a room, which makes the shielding
    /// band and the bulkhead run the same thing wearing two names.</para>
    /// </summary>
    public static Fit RoomForACaptain(in HullSounding.HiddenVoid hidden) =>
        hidden.Outboard ? Fit.Fits : Fit.TheRunIsTooNarrow;

    // ── THE HOLE, AS GEOMETRY ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Half the width of the hole the rig leaves in the pressure hull, deck units. 1.2 against the captain's
    /// 0.7 radius leaves a hair over half a body-width of clearance either side — a squeeze that a person
    /// gets through and that reads on the deck plan as a cut rather than as a doorway. Below the radius it
    /// would be a hole nobody can use, which is this repo's own <c>DoorHalfWidth</c> mistake (a 2 du gap
    /// minus a 1.4 du body left a 0.6 du slot) waiting to be made twice.
    /// </summary>
    public const double PlateHalfWidth = 1.2;

    /// <summary>
    /// A VOID THAT IS KNOWN, as the geometry the deck is built from. The captain draws what he knows: until
    /// a plate has been cut this record does not exist, the band is one filled run end to end, and the map
    /// says nothing. Afterwards the pocket is walls and space like any other part of the ship.
    /// </summary>
    /// <param name="X0">Aft end of the pocket.</param>
    /// <param name="X1">Forward end.</param>
    /// <param name="Top">Which side of the keel — which of the two shielding bands it is cut into.</param>
    /// <param name="PlateX">Where the hole is, along the pressure hull.</param>
    /// <param name="PlateShut">The plate is fitted back into its hole. True while a captain is folded in
    /// behind it, which is why hiding needs no rule of its own: a shut plate is a wall, and walls are law.</param>
    public readonly record struct OpenVoid(double X0, double X1, bool Top, double PlateX, bool PlateShut);

    /// <summary>Whether a point is inside the pocket — used by the deck's own location line, by the rule
    /// below, and by the audits. Inclusive of the band's two faces, because a body standing against one is
    /// still in there.</summary>
    public static bool InThePocket(in OpenVoid pocket, double x, double y)
    {
        if (x < pocket.X0 || x > pocket.X1)
        {
            return false;
        }

        return pocket.Top
            ? y <= WreckLayout.TopY && y >= WreckLayout.OuterTopY
            : y >= WreckLayout.BottomY && y <= WreckLayout.OuterBottomY;
    }

    // ── WHAT GIVES A STOWAWAY AWAY ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HOW LONG A CUT STAYS WARM. Forty seconds, and the number is the whole decision: a sweep team crosses
    /// the hull at <see cref="InspectionTeam.SweepSpeed"/> 3.5 du/s, so forty seconds is about 140 du of
    /// walking — most of a lap of a 60 du ship. Cut while their boat is mating and the cut is warm when they
    /// reach you; cut before you ever heard of them and it is cold long since.
    ///
    /// <para>It is deliberately longer than <see cref="InspectionTeam.SearchSeconds"/> (12) so that a captain
    /// who makes a racket, hides, and waits out one investigation is NOT automatically safe — the noise gets
    /// them to the wall while the cut is still bright. FLAGGED for the owner's tuning.</para>
    /// </summary>
    public const double CutStaysWarmSeconds = 40.0;

    /// <summary>Whether the cut would still read as fresh to somebody looking straight at it.</summary>
    public static bool CutIsWarm(double secondsSinceTheCut) =>
        secondsSinceTheCut < CutStaysWarmSeconds;

    /// <summary>What gave the captain away, or nothing.</summary>
    public enum Tell
    {
        /// <summary>They walked past. The best thing that happens in this scene and the whole point of it.</summary>
        None,

        /// <summary>Not in the void at all — seen in a corridor, exactly as always. The sweep is unchanged
        /// by any of this for a captain who is standing in it.</summary>
        StandingInTheOpen,

        /// <summary>In the void with the plate off behind you. A hole in a wall with a person in it is not a
        /// hiding place; it is a person in a hole.</summary>
        TheHoleIsOpenBehindYou,

        /// <summary>A lamp was on the plate as it closed. The cubicle's rule: they cannot see through it and
        /// they do not have to.</summary>
        TheyWatchedYouGetIn,

        /// <summary>The plate is fitted, and the cut round it is still bright. This is the one worth losing
        /// to, because it is the one you could have avoided by cutting sooner.</summary>
        TheCutIsStillWarm,
    }

    /// <summary>
    /// THE WHOLE OF THE HIDE, IN ONE FUNCTION. Every input is a fact the sweep already computes:
    /// <paramref name="theyCanSeeYou"/> and <paramref name="theirLampIsOnThePlate"/> are two calls to
    /// <see cref="InspectionTeam.Sees"/> with the same walls list, and the seconds are a clock.
    ///
    /// <para>Order matters and is deliberate. Being SEEN outranks everything, so no arrangement of hiding
    /// state can make a visible captain invisible — the failure mode a stealth rule has to be built against.
    /// Then the two tells that are about the hole rather than about you.</para>
    /// </summary>
    /// <param name="insideTheVoid">The captain is in the pocket.</param>
    /// <param name="plateShut">…with the plate fitted back into the hole behind them.</param>
    /// <param name="theyWatchedYouGetIn">This sweeper had the plate in their lamp as it closed.</param>
    /// <param name="secondsSinceTheCut">How long ago the rig went through it.</param>
    /// <param name="theirLampIsOnThePlate">The plate is in their cone, in reach, in line of sight.</param>
    /// <param name="theyCanSeeYou">The captain's own body is.</param>
    public static Tell WhatGivesYouAway(
        bool insideTheVoid,
        bool plateShut,
        bool theyWatchedYouGetIn,
        double secondsSinceTheCut,
        bool theirLampIsOnThePlate,
        bool theyCanSeeYou)
    {
        if (theyCanSeeYou)
        {
            return insideTheVoid ? Tell.TheHoleIsOpenBehindYou : Tell.StandingInTheOpen;
        }

        if (!insideTheVoid)
        {
            return Tell.None;
        }

        if (!plateShut && theirLampIsOnThePlate)
        {
            return Tell.TheHoleIsOpenBehindYou;
        }

        if (theyWatchedYouGetIn)
        {
            return Tell.TheyWatchedYouGetIn;
        }

        return theirLampIsOnThePlate && CutIsWarm(secondsSinceTheCut)
            ? Tell.TheCutIsStillWarm
            : Tell.None;
    }

    /// <summary>Whether that tell ends the hide. Separated from the tell itself because
    /// <see cref="Tell.None"/> is a result worth logging and the caller should not have to compare against
    /// an enum member to know it got away with it.</summary>
    public static bool Caught(Tell tell) => tell != Tell.None;

    // ── WHAT IS SAID ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The prompt at an opened plate a captain can get into.</summary>
    public const string OfferLine =
        "🕳 Fold in and pull the plate to after you. It is a coffin-width of shielding band and it is not " +
        "comfortable, which is not the same as it being a bad idea.";

    /// <summary>…and at one they cannot.</summary>
    public const string TooNarrowLine =
        "🕳 A hand's width of pipework and a run of cable. You could hide a rifle in there. You are not a rifle.";

    /// <summary>Getting in.</summary>
    public const string ClimbedInLine =
        "🕳 You go in shoulder-first and get the plate back into its own hole from the inside. It sits a " +
        "millimetre proud, the way a thing does when it has been cut out once. Then it is dark, and close, " +
        "and the ship is a sound rather than a place.";

    /// <summary>…and back out.</summary>
    public const string ClimbedOutLine =
        "🕳 The plate comes away and the corridor is very bright and very wide. You are standing in a room " +
        "again, which is a strange thing to be relieved about.";

    /// <summary>What the plate console is called, in each of its three lives. One console, three faces — the
    /// deck plan's own idiom for a fixture whose verb changes with its state.</summary>
    public static string PlateLabel(bool opened, bool inside) =>
        !opened ? "🕳 THE FALSE PLATE"
        : inside ? "🕳 THE PLATE — PUSH IT OFF"
        : "🕳 THE VOID — GET IN";

    /// <summary>What the deck calls the pocket once it is known. Not a compartment name, because it is not a
    /// compartment: it is the sentence the manifest refused to write.</summary>
    public const string PocketName = "A SPACE NOBODY DREW";

    /// <summary>The clock strip's own words for a cut that has not gone cold — the thing the captain is
    /// waiting on, said plainly, because a hidden timer is not a decision.</summary>
    public const string WarmCutWatchLine = "the cut is still bright";

    /// <summary>What each tell sounds like when it happens. The two that are about the hole say what they
    /// SAW, never what they concluded — a sweeper who announces "there is a stowaway behind this plate" has
    /// done the player's thinking for them.</summary>
    public static string TellLine(Tell tell, string callsign) => tell switch
    {
        Tell.StandingInTheOpen =>
            $"🕶 {callsign}'s lamp comes round the frame and stops on you, standing in a corridor on somebody " +
            "else's ship.",
        Tell.TheHoleIsOpenBehindYou =>
            $"🕶 {callsign} does not even slow down. There is a plate lying on the deck and a hole in the " +
            "shielding with a face in it.",
        Tell.TheyWatchedYouGetIn =>
            $"🕶 {callsign} walks to the plate and stands in front of it without hurrying. They were looking " +
            "at this wall when it closed. Whatever else they are, they are not stupid.",
        Tell.TheCutIsStillWarm =>
            $"🕶 The lamp settles on the plate and stays there. {callsign}, to somebody on a channel you " +
            "cannot hear: this one has been cut, and it is still warm. A glove comes up and takes hold of it.",
        _ =>
            "🕶 The lamp crosses the plate, holds a moment on nothing in particular, and goes on down the " +
            $"compartment. {callsign} tries a hatch two frames along and leaves it as it was found.",
    };

    /// <summary>The line worth the whole feature — what a captain hears from inside, and the reason the void
    /// is a place rather than a container.</summary>
    public const string TheyWalkedPastLine =
        "🕳 Boots on the deck plating, one metre and a plate's thickness away. Somebody tries the hatch you " +
        "came in through. Then the sound goes forward, unhurried, and keeps going.";
}
