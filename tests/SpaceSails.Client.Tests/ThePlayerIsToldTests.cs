using System.Text;
using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #761 · THE OWNER'S LAW, AS A TEST INSTEAD OF AS A BUG FAMILY.
///
/// <para>Owner ruling, 2026-08-08: <i>"We should make sure we tell the user clearly when plot significant
/// things happen."</i> The issue that carries it says what it is for in its own first line — the rule had
/// been arriving one instance at a time, and each instance got a local guard:</para>
///
/// <list type="bullet">
/// <item>#689 / #693 — the story was told and the screen ate it (the #592 climax losing the one pulse slot
/// to the routine air line). Guarded by <c>ThePulseKeepsTheBiggestSentenceTests</c>.</item>
/// <item>#736 — the outcome landed behind the modal backdrop, in the DOM and not on the screen. Guarded by
/// the pop-up families' own tests.</item>
/// <item>#768 — the sentence lost to a card raised in the same breath. Guarded by
/// <c>TheHeldSayingOutlivesTheCardTests</c>.</item>
/// <item>#777 — a beat counted as told with no surface up at all. Guarded at the seam.</item>
/// </list>
///
/// <para>Four guards, each about the moment that produced it, and no guard about the LAW. So a feature built
/// tomorrow starts from zero again, and the fifth instance is found the way the first four were: by the
/// owner playing the game. This file is the law itself.</para>
///
/// <para><b>The law.</b> When something plot-significant happens — it changes what the captain knows, owes,
/// is owed, or can do: a reveal, a debt, a standing gained or lost, a door that will now open, somebody who
/// will now remember — the player is told <b>at the moment it happens, on the surface they are looking
/// at</b>. Not in a log. Not behind a backdrop. Not in a line that loses a race to boilerplate.</para>
///
/// <para><b>"Plot-significant" is not decided here.</b> It is <see cref="Telling.Floor"/> in Core — the top
/// two of #693's ranks — and this file derives its vocabulary from that enum rather than copying a
/// <c>&gt;= PulseRank.Beat</c> into itself. A guard that keeps its own private copy of the definition is
/// the stale mirror, and the stale mirror is how a law quietly stops being one.</para>
///
/// <para><b>What this can and cannot do.</b> It cannot decide whether a new event is plot-significant — that
/// is a judgement, and it belongs to the person writing the feature, which is why the same question is
/// written into <c>docs/testing-guide.md</c> as a standing one to ask of every change. What it CAN do, and
/// does, is hold every answer already given: every moment in <see cref="TellingTable"/> is checked against
/// the shipping source, every deliberate silence in <see cref="KnownSilences"/> is checked to still BE
/// silent, and every plot-significant pulse in the client must appear in the table — so a rank raised
/// without a row goes red, and a silence that grows a voice goes red too.</para>
/// </summary>
public sealed class ThePlayerIsToldTests
{
    // ── WHAT COUNTS AS BEING TOLD ──────────────────────────────────────────────────────────────────────

    /// <summary>The three surfaces a plot-significant moment may reach the player on, and the one it may
    /// not. They are not styles; they are three different answers to "where is the captain's eye".</summary>
    private enum Surface
    {
        /// <summary>A card or plate the moment raises for itself — the story-beat seam, or a card record the
        /// markup renders. The most modal thing in the game: it stops the world and waits.</summary>
        RaisedCard,

        /// <summary>#736's law: the answer lands on the pop-up the captain is already looking at, in the one
        /// subtree the backdrop cannot blur — either through <c>SayItWhereTheyAreLooking</c>, which picks
        /// that pop-up in z-order, or by writing the panel's own outcome field directly.</summary>
        OnModalOutcome,

        /// <summary>The HUD's one slot, at a rank that cannot be displaced by the next instrument reading
        /// (#693). The doorbell — legitimate when nothing is in front of the captain at all.</summary>
        RankedPulse,
    }

    /// <summary>A moment, and where the player reads it.</summary>
    /// <param name="Moment">What happens, in the vocabulary of the law: what changes about what the captain
    /// knows, owes, is owed, or can do.</param>
    /// <param name="Where">Which of the three surfaces carries it.</param>
    /// <param name="File">The shipping file it happens in, relative to <c>src/SpaceSails.Client/Pages</c>.</param>
    /// <param name="Method">The method it happens in. If this method is gone the row is stale and fails.</param>
    /// <param name="Proof">The exact fragment of that method that does the telling. Checked to be present,
    /// AND checked to be admissible for <paramref name="Where"/> — a row cannot claim a surface it does not
    /// reach.</param>
    private sealed record Told(string Moment, Surface Where, string File, string Method, string Proof);

    /// <summary>
    /// EVERY PLOT-SIGNIFICANT MOMENT THIS SWEEP COULD ENUMERATE FROM THE CODE, and the surface that tells
    /// it. One row per moment, and the comment above each block says which of the law's five clauses it is —
    /// what the captain now knows, owes, is owed, can do, or who will now remember.
    ///
    /// <para>Rows are not decoration: <see cref="EveryMomentIsToldWhereTheTableSaysItIs"/> opens each file,
    /// finds each method, and checks the proof is really in it and really reaches the surface claimed.</para>
    /// </summary>
    private static readonly Told[] TellingTable =
    [
        // ── SOMEBODY IS NOW COMING FOR YOU ─────────────────────────────────────────────────────────────
        //
        // The heat-hunter is the game's oldest consequence and the one #380 item 5 says new players could
        // not see the cause of. It is a change to what the captain CAN DO — a berth he can no longer sit
        // at — so the line is ranked rather than left to compete with the next fuel reading.
        new("a robbery buys a collector, and it is already fitting out",
            Surface.RankedPulse, "Map.Combat.cs", "SpawnHunterForHeatEvent",
            "days, not weeks.\", Telling.Floor)"),

        // The grapples land: the demand panel IS the hail's card (#777 hosted), so the beat is raised
        // through the one door and the panel carries the painting. What the captain now OWES.
        new("a collector has you, and names a price",
            Surface.RaisedCard, "Map.Combat.Busted.cs", "ApplyHunterCatch",
            "RaiseStoryBeat(StoryBeats.Beat.CollectorHail, hunter.Callsign);"),

        // ── WHAT YOU OWE, AND WHAT YOU ARE OWED ────────────────────────────────────────────────────────
        //
        // SUBMIT: the debt is collected. The hold is emptied and the heat clears to zero, and both are read
        // off the panel's own record rather than pulsed under its backdrop (#736).
        new("you submit, and the collector takes the hold",
            Surface.OnModalOutcome, "Map.Combat.Busted.cs", "BustedSubmit",
            "b.Phase = BustedEncounter.Stage.Confiscated;"),

        // BRIBE: the coin moves and the law does not. Said on the card that asked for it.
        new("you buy this patrol, and not the law",
            Surface.OnModalOutcome, "Map.Combat.Busted.cs", "BustedBribe",
            "b.ResultMessage = $\"{b.Bribe.Total:N0} cr changes hands."),

        // THE REBIRTH — the largest single change in the game to what the captain owes and is owed: a
        // clinic bill, an insurance tier consulted, a hull nobody wanted, a new face, and a book that has
        // been re-greyed at the filing line. Every one of those rides the card's own record (b.ClinicBillCr,
        // b.HullDescription, b.ClinicName, b.RebirthGlitch, b.FilingNotice) and the card is this stage.
        new("you wake in a clinic owing for the wake-up",
            Surface.RaisedCard, "Map.Combat.Busted.cs", "BustedResurrect",
            "b.Phase = BustedEncounter.Stage.Resurrected;"),

        // A POLICY BOUGHT. He says nothing new about a sale — that is the character — so the telling is the
        // card refreshing to the tier now held, which is the fact that changed.
        new("the premium is paid and the policy is the tier you now hold",
            Surface.OnModalOutcome, "Map.Rep.cs", "BuyFromTheRep",
            "_repCard = NebulaRep.PitchFor(_insurance.Tier, _repNameOnFile, bleeding: false);"),

        // ── PEOPLE WHO WILL NOW REMEMBER — the five crossings of #715 ───────────────────────────────────
        //
        // Every one of these banks heat against the outfit that runs this ground, which prices a berth and
        // reads a gate later. They are the cleanest test of the law in the game, because the same event
        // reaches five different surfaces and had to be asked about five times.
        //
        // (1) The gate refuses: a card over the lift panel, carrying the matrix's own line (#684).
        new("a gate refuses your papers, and the outfit writes it down",
            Surface.RaisedCard, "Map.Surface.Hive.cs", "PressLiftButton",
            "_viewObject = new DeckPlan.ConsoleSpot("),

        // (2) The scanner press: said where the captain is looking, through #736's seam.
        new("you press a signal that was not yours to press",
            Surface.OnModalOutcome, "Map.Scan.cs", "PressTheHit",
            "SayWhereTheyAreLookingAndFile(pressed.Line,"),

        // (3) The remote send: onto the agent's own panel, which is what is in front of him.
        new("you put this ship's name on somebody else's net",
            Surface.OnModalOutcome, "Map.Combat.Remote.cs", "SendTheStanding",
            "_remoteOutcome = sent.Line;"),

        // (4) The walk-in's setup — #761's own find. It reached no surface at all until this lane: the
        // badge that carries this fact elsewhere is drawn only on an excursion, and her second berth is a
        // bar. IllegalHeat's own sentence, at the floor, because the completion line lands in the same
        // breath at Status.
        new("she set you up, and the port has been waiting for this ship",
            Surface.RankedPulse, "Map.WalkIn.cs", "YouComeBackAndTellHer",
            "SayItWhereTheyAreLooking(IllegalHeat.TheyRememberYouHere, Telling.Floor);"),

        // (5) is the signer, and it is told nowhere — see KnownSilences.

        // ── DOORS THAT WILL NOW OPEN ───────────────────────────────────────────────────────────────────
        //
        // The card is accepted and a band of the building opens. The arrival's sayings are HELD (#768) so
        // the card cannot eat them, and the gate's own moment gets a card of its own.
        new("your card opens a floor that was shut",
            Surface.RaisedCard, "Map.Surface.Hive.cs", "RideTheLiftTo",
            "_viewObject = new DeckPlan.ConsoleSpot("),

        // The wintering hall — the sentence a whole feature was built to say, and the only CLIMAX raised
        // from the client. The owner's 2026-08-03 ruling put the head of the organization at the far end of
        // it; nothing routine may stand on top of that.
        new("you are standing in the wintering hall",
            Surface.RankedPulse, "Map.HeadOffice.cs", "MaybeRaiseHeadOfficeBeat",
            "UndergroundComplex.WinteringHallLine, \"❄\", PulseRank.Climax);"),

        // ── A PLACE TO HIDE, AND A PLACE THAT STOPS BEING ONE ──────────────────────────────────────────
        //
        // What the captain CAN DO, on a floor with a guard round on it. Ranked because these all happen
        // while an excursion is narrating its own weather.
        new("the cubicle door is shut on you and the round is outside",
            Surface.RankedPulse, "Map.Cubicle.cs", "ShutTheCubicle",
            "ShowPulseMessage(CubicleLock.LockedLine, PulseRank.Beat);"),
        new("the door you were hiding behind is opened on you",
            Surface.RankedPulse, "Map.Cubicle.cs", "OpenTheCubicle",
            "ShowPulseMessage(CubicleLock.OpenedOnHimLine, PulseRank.Beat);"),
        new("the basin takes what was on your hands",
            Surface.RankedPulse, "Map.Cubicle.cs", "TryWashYourHands",
            "ShowPulseMessage(said, PulseRank.Beat);"),
        new("he knocks, and he is not going away",
            Surface.RankedPulse, "Patrol/Patrol.Hide.cs", "WaitOutsideTheCubicle",
            "_host.ShowPulseMessage(CubicleLock.KnockLine, PulseRank.Beat);"),

        // ── THE ROUND, AND WHAT IT DECIDES ABOUT YOU ───────────────────────────────────────────────────
        new("a guard hails you and wants to see a face with the paper",
            Surface.RankedPulse, "Patrol/Patrol.Challenge.cs", "TheHail",
            "_host.ShowPulseMessage(PatrolBeat.HailLine, PulseRank.Beat);"),
        new("he calls it in, and now the floor knows",
            Surface.RankedPulse, "Patrol/Patrol.Run.cs", "TheRadioCall",
            "_host.ShowPulseMessage(PatrolBeat.CallsItInLine, PulseRank.Beat);"),
        new("he loses you, and the floor is yours again",
            Surface.RankedPulse, "Patrol/Patrol.Run.cs", "HeLosesYou",
            "_host.ShowPulseMessage(PatrolBeat.LostYouLine, PulseRank.Beat);"),
        new("your pass is revoked and you are walked out",
            Surface.RankedPulse, "Patrol/Patrol.Run.cs", "TheKickOut",
            "_host.ShowPulseMessage(closing, PulseRank.Beat);"),
        new("the escort ends and he lets go of you",
            Surface.RankedPulse, "Patrol/Patrol.Escort.cs", "WalkTheEscort",
            "_host.ShowPulseMessage(PatrolBeat.EscortDoneLine, PulseRank.Beat);"),
        new("the escort is cut short at the lift",
            Surface.RankedPulse, "Patrol/Patrol.Escort.cs", "TheCutToTheLift",
            "_host.ShowPulseMessage(PatrolBeat.EscortCutLine, PulseRank.Beat);"),

        // ── A BOAT YOU DID NOT CALL ────────────────────────────────────────────────────────────────────
        //
        // Held rather than pulsed (#768): both of these arrive with a card, and a line said under a
        // backdrop is a line said to nobody. The rank decides which of them survives the card.
        new("a boat sets down between you and the way home",
            Surface.RankedPulse, "Map.Surface.RepoBoat.cs", "LandTheCollectors",
            "HoldSaying(CollectorLanding.ArrivalLine(ex.CollectorCallsign), PulseRank.Beat);"),
        new("a shelter is a pressure vessel and not a sanctuary",
            Surface.RankedPulse, "Map.Surface.RepoBoat.cs", "StepCollectors",
            "HoldSaying(CollectorLanding.ShelterIsNotSanctuaryLine, PulseRank.Beat);"),

        // ── THE PLAN YOU MADE IS NO LONGER THE PLAN YOU HAVE ───────────────────────────────────────────
        //
        // What the captain CAN DO, and the two places the world can take it back: an arrival that will not
        // arm, and a plan whose shape has stopped being flyable. Ranked because the instruments narrate
        // continuously in exactly these two views.
        new("the arrival you armed will not arm any more",
            Surface.RankedPulse, "Map.Plot.Arrive.cs", "RefreshArriveValidity",
            "ShowPulseMessage(_arriveAlarm, PulseRank.Beat);"),
        new("the plan's shape has stopped being flyable",
            Surface.RankedPulse, "Map.Plot.CastOff.cs", "RefreshPlanShapeValidity",
            "ShowPulseMessage(_shapeAlarm, PulseRank.Beat);"),

        // ── STANDINGS ──────────────────────────────────────────────────────────────────────────────────
        //
        // The crew's, and it is the one standing in the game that is about the captain rather than about a
        // company. Both edges raise a card, because both are a room the captain walks into.
        new("the crew send a deputation, or hold a meeting you were not asked to",
            Surface.RaisedCard, "Map.CrewTemp.cs", "WatchWhereTheCrewStand",
            "RaiseStoryBeat(StoryBeats.Beat.CrewMeeting);"),

        // A straight answer, filed under a name, earns a name back. Said on the outcome card the filing
        // raises — this row's own comment in the source cites #761 already.
        new("a straight finding earns you somebody who will take your call",
            Surface.OnModalOutcome, "Map.Wreck.cs", "BookTheSalvageContact",
            "_wreckContact = name;"),

        // ── REVEALS ────────────────────────────────────────────────────────────────────────────────────
        //
        // The dossier: three papers rooms in one excursion, and a stranger comes together out of them. Four
        // to nine sentences in one breath — #768's hold releases ONE winner, so this one uses #736's law
        // instead and puts the whole debrief on the card.
        new("the papers assemble into somebody with a name and a next of kin",
            Surface.RaisedCard, "Map.Surface.Hive.cs", "AssembleSomebody",
            "_viewObject = new DeckPlan.ConsoleSpot("),

        // ── ENDINGS ────────────────────────────────────────────────────────────────────────────────────
        //
        // Three ways a voyage stops, and all three are a card because there is nothing left to play under.
        new("she goes, and you are not aboard — the castaway ending",
            Surface.RaisedCard, "Map.ShipScuttleBoard.cs", "SheGoesWithoutHim",
            "_shipEpitaph = new ShipEpitaph("),
        new("the long dark closes over her — twenty days adrift",
            Surface.RaisedCard, "Map.Void.cs", "TheVoidTakesHer",
            "_busted = new BustedEncounter"),
        new("the wreck you scuttled is gone, and you heard whether anything was aboard",
            Surface.RaisedCard, "Map.Scuttle.cs", "ResolveScuttleOnDeparture",
            "_scuttleEpitaph = Scuttle.SheGoes(Scuttle.Method.ReactorOverload, _scuttleHeardIt);"),
    ];

    /// <summary>
    /// AND THE MOMENTS THAT SAY NOTHING ON PURPOSE — each with the design that chose the silence, and each
    /// asserted to still BE silent by <see cref="EverySilenceIsStillSilent"/>. A row here is a claim about
    /// the shipped code, not a permanent excuse: give one of these a voice and this file goes red and asks
    /// for the row to move into <see cref="TellingTable"/> instead.
    ///
    /// <para>Four of the six are one design — the body-disposal arc's channels, which the owner's own
    /// horror depends on being unannounced. Their source comments say so in almost identical words, and
    /// those comments are the authority for these rows.</para>
    /// </summary>
    private static readonly (string Moment, string File, string Method, string Why)[] KnownSilences =
    [
        // #1068 · "No card, no pulse, no beat, no nerve shock, no marker, no line." The world declining to
        // answer is the whole device: a leaf that does not open and a pass that never lands.
        ("the world declines, politely and without saying so",
            "Map.Decline.cs", "TheWorldDeclines",
            "#1068 — the decline IS the absence of an answer; a line about it would be the answer"),

        // #1068 channel 3 · The harbour moves its paperwork overnight, by people who do not know why.
        ("a berth is reassigned and a pump repriced overnight",
            "Map.QuietHands.cs", "TheQuietHandsMove",
            "#1068 — the captain is meant to notice the price, never to be told about the hand"),

        // #1074 beat 1 · The office closes the working and files a reason nobody can argue with.
        ("the working is closed and the shaft sealed below the listed bottom",
            "Map.Stop.cs", "TheOfficeClosesTheWorking",
            "#1074 — it is found by going back, which is the beat"),

        // #1074 beat 2 · The cheapest hide is official care.
        ("the site passes into official care and is fenced",
            "Map.Preserve.cs", "TheSitePassesIntoCare",
            "#1074 — the fence and the sign are the telling; a pulse would name the hand behind them"),

        // #1063 · The ground was filled in while you were away. The one voice it has is a cheerful line in
        // a newspaper, days later and at a desk — which is a reading, never a telling at the moment.
        ("the ground you opened is filled in while you are away",
            "Map.Burial.cs", "BuryWhatWasOpened",
            "#1063 — three pieces of ordinary paperwork and a mason, and nothing else"),

        // #1066 · The clamp already says its piece and the tube's plate is already on the screen; a third
        // sentence in the same moment is the stacked-card failure. Where the tally stands is on the crew
        // sheet, and the CONSEQUENCE is told — see the crew-standing row in the table above.
        ("the berth the crew got is written into the shore-leave tally",
            "Map.CrewTemp.cs", "NoteTheBerthTheCrewGot",
            "#1066 — the tally is read on the sheet; the broken promise raises a card of its own"),

        // #761 · AND THE ONE THAT IS A DEBT, NOT A DECISION. The signer files on you at this berth and
        // banks a real unit of heat, and nothing anywhere says so — no card, no pulse, no note, not even a
        // log line. Whether the captain is MEANT not to know is the owner's call and not a guard's: Rusty
        // Meg's counter has the identical shape one file over and says outright that nothing reacts, and
        // this one says nothing at all. Marked FABLE at the call site; filed here so it cannot be lost.
        ("the man who signed files on you at this berth",
            "Map.OldCrew.cs", "TheSignerReports",
            "#761 FABLE: line needed, or a comment saying the silence is the design"),
    ];

    // ── THE VOCABULARY, DERIVED AND NEVER COPIED ───────────────────────────────────────────────────────

    /// <summary>The rank tokens a <see cref="Surface.RankedPulse"/> row may prove itself with, built FROM
    /// <see cref="Telling.IsPlotSignificant"/> rather than typed out. Add a rank above the floor in Core and
    /// this list grows on its own; move the floor and it shrinks.</summary>
    private static readonly string[] PlotSignificantRankTokens =
    [
        "Telling.Floor",
        .. Enum.GetValues<PulseRank>().Where(r => r.IsPlotSignificant()).Select(r => $"PulseRank.{r}"),
    ];

    /// <summary>Fragments that mean "the player was told", whatever surface it was. Used only for the
    /// coarse question — was this method mute? — never to decide WHICH surface, which the rows do.</summary>
    private static readonly string[] TellingCalls =
    [
        "RaiseStoryBeat(", "SayItWhereTheyAreLooking(", "SayWhereTheyAreLookingAndFile(",
        "ShowPulseMessage(", "ShowAndFile(", "ShowAndFileAbout(", "HoldAndFile(", "HoldSaying(",
        "ShowGroundGrewCardOnce(", "SayAtTheCounter(", "RaiseAScrimCard(",
    ];

    /// <summary>Fragments that only write something DOWN. A method whose whole voice is one of these is a
    /// method that told the book and not the captain — the exact shape #761 is against.</summary>
    private static readonly string[] LogCalls = ["LogAutopilotEvent(", "FileNote(", "FileNoteAbout("];

    // ── READING THE SHIPPED SOURCE ─────────────────────────────────────────────────────────────────────

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string PagesDir() =>
        Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages");

    private static string ReadPage(string relative) =>
        File.ReadAllText(Path.Combine(PagesDir(), relative.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>
    /// COMMENTS ARE NOT CODE. Every question below is asked of what the method DOES, and a prose paragraph
    /// explaining that a moment is deliberately unannounced must never be mistaken for the announcement —
    /// nor for its absence. Strings are left alone: an authored sentence is the very thing being placed.
    /// </summary>
    private static string WithoutComments(string source)
    {
        var kept = new StringBuilder(source.Length);
        bool inLine = false, inBlock = false, inString = false, inVerbatim = false, inChar = false;

        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];
            char next = i + 1 < source.Length ? source[i + 1] : '\0';

            if (inLine)
            {
                if (c is '\n') { inLine = false; kept.Append(c); }
                continue;
            }
            if (inBlock)
            {
                if (c is '*' && next is '/') { inBlock = false; i++; }
                continue;
            }
            if (inString || inChar)
            {
                kept.Append(c);
                if (!inVerbatim && c is '\\' && next is not '\0') { kept.Append(next); i++; continue; }
                if (inVerbatim && c is '"' && next is '"') { kept.Append(next); i++; continue; }
                if (inString && c is '"') { inString = false; inVerbatim = false; }
                else if (inChar && c is '\'') { inChar = false; }
                continue;
            }

            if (c is '/' && next is '/') { inLine = true; continue; }
            if (c is '/' && next is '*') { inBlock = true; i++; continue; }
            if (c is '@' && next is '"') { inString = true; inVerbatim = true; kept.Append(c).Append(next); i++; continue; }
            if (c is '$' && next is '"') { inString = true; kept.Append(c).Append(next); i++; continue; }
            if (c is '"') { inString = true; kept.Append(c); continue; }
            if (c is '\'') { inChar = true; kept.Append(c); continue; }
            kept.Append(c);
        }

        return kept.ToString();
    }

    /// <summary>The half-open source range of <paramref name="method"/>'s body, or null when no method of
    /// that name has one. Deliberately literal: it finds the name, balances the parameter list, and takes
    /// the block that follows — an expression-bodied member (which has no block) is not a body this law can
    /// read, and returning null for it is the honest answer rather than a guess.</summary>
    private static (int Start, int End)? BodyRange(string source, string method)
    {
        foreach (Match m in Regex.Matches(source, $@"\b{Regex.Escape(method)}\s*\("))
        {
            int i = m.Index + m.Length - 1;      // at the '('
            int depth = 0;
            for (; i < source.Length; i++)
            {
                if (source[i] is '(') { depth++; }
                else if (source[i] is ')') { depth--; if (depth == 0) { i++; break; } }
            }

            while (i < source.Length && char.IsWhiteSpace(source[i])) { i++; }
            if (i >= source.Length || source[i] is not '{')
            {
                continue;                        // a call, or an expression-bodied member: not this
            }

            int open = i, braces = 0;
            for (; i < source.Length; i++)
            {
                if (source[i] is '{') { braces++; }
                else if (source[i] is '}') { braces--; if (braces == 0) { return (open, i + 1); } }
            }
        }

        return null;
    }

    private static bool Mentions(string body, IEnumerable<string> tokens) =>
        tokens.Any(t => body.Contains(t, StringComparison.Ordinal));

    /// <summary>Is the fragment a row offers really a reach to the surface the row claims? This is the check
    /// that stops a row from being a wish: a card row must set something the MARKUP renders (or knock on the
    /// beat seam), a modal row must land on a pop-up, and a ranked row must carry a rank the floor
    /// admits.</summary>
    private static bool ProofReaches(Surface where, string proof, string markup) => where switch
    {
        Surface.RaisedCard =>
            proof.Contains("RaiseStoryBeat(", StringComparison.Ordinal)
            || CardFieldIn(proof) is { } field && markup.Contains(field, StringComparison.Ordinal),

        Surface.OnModalOutcome =>
            proof.Contains("SayItWhereTheyAreLooking(", StringComparison.Ordinal)
            || proof.Contains("SayWhereTheyAreLookingAndFile(", StringComparison.Ordinal)
            || proof.Contains("SayAtTheCounter(", StringComparison.Ordinal)
            || CardFieldIn(proof) is { } outcome && markup.Contains(outcome, StringComparison.Ordinal),

        Surface.RankedPulse => Mentions(proof, PlotSignificantRankTokens),

        _ => false,
    };

    /// <summary>The state a proof writes, if it writes one — <c>_shipEpitaph</c>, <c>b.Phase = …Confiscated</c>,
    /// <c>_repCard</c>. Whether the markup actually draws it is the caller's question, and it is the whole
    /// point: a field nothing renders is a card nobody sees.</summary>
    private static string? CardFieldIn(string proof)
    {
        Match assigned = Regex.Match(proof, @"(?:^|\s)(_[A-Za-z0-9_]+|[a-z]\.[A-Za-z0-9_]+)\s*=\s*([^;]*)");
        if (!assigned.Success)
        {
            return null;
        }

        // `b.Phase = BustedEncounter.Stage.Confiscated` — the stage IS the card, so that is what the markup
        // has to be showing. Anything else is named by the field it writes.
        Match stage = Regex.Match(assigned.Groups[2].Value, @"Stage\.[A-Za-z0-9_]+");
        if (stage.Success)
        {
            return stage.Value;
        }

        // `b.ResultMessage` is the panel record's own field, and the markup reads it off whatever local it
        // unpacked the record into — so the receiver is the caller's business and the MEMBER is the claim.
        string written = assigned.Groups[1].Value;
        return written.Contains('.') ? written[(written.IndexOf('.') + 1)..] : written;
    }

    // ── THE LAW ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE WHOLE OF IT, ROW BY ROW: the method is still there, the telling is still in it, the telling
    /// really reaches the surface the row names, and the method is not mute.
    ///
    /// <para>All four failures are collected before anything is asserted, because a law that reports its
    /// first violation and stops teaches people to fix them one commit at a time.</para>
    /// </summary>
    [Fact]
    public void EveryMomentIsToldWhereTheTableSaysItIs()
    {
        string markup = ReadPage("Map.razor");
        List<string> broken = [];

        foreach (Told row in TellingTable)
        {
            string source = WithoutComments(ReadPage(row.File));
            if (BodyRange(source, row.Method) is not { } at)
            {
                broken.Add($"{row.File}::{row.Method} — no such method any more, so the row for "
                    + $"\"{row.Moment}\" is a mirror of code that is gone");
                continue;
            }

            string body = source[at.Start..at.End];

            if (!body.Contains(row.Proof, StringComparison.Ordinal))
            {
                broken.Add($"{row.File}::{row.Method} — \"{row.Moment}\" is no longer told by "
                    + $"`{row.Proof}`; find where it is told now and correct the row");
                continue;
            }

            if (!ProofReaches(row.Where, row.Proof, markup))
            {
                broken.Add($"{row.File}::{row.Method} — \"{row.Moment}\" claims {row.Where} but "
                    + $"`{row.Proof}` does not reach it (a card field the markup never renders, a pop-up "
                    + $"nothing writes, or a pulse below {Telling.Floor})");
                continue;
            }

            if (!Mentions(body, TellingCalls) && CardFieldIn(row.Proof) is null)
            {
                broken.Add($"{row.File}::{row.Method} — \"{row.Moment}\" reaches no surface at all; "
                    + "the book is not the player");
            }
        }

        Assert.True(broken.Count == 0,
            "the owner's law (#761): a plot-significant moment is told at the moment it happens, on the "
            + "surface the captain is looking at.\n  " + string.Join("\n  ", broken));
    }

    /// <summary>
    /// THE RATCHET FORWARD: every plot-significant pulse in the shipping client is in the table.
    ///
    /// <para>This is the half that cannot be argued with. A rank is a claim that a line matters, so a line
    /// that claims it and is not written down here is a moment nobody has asked the law's question about —
    /// and the answer arrives the way the first four instances did, from the owner, playing.</para>
    ///
    /// <para>It resolves by RANGE rather than by name: the token has to fall inside the body of a method
    /// some row names, so moving a ranked line out of a listed method and into an unlisted one is caught.</para>
    /// </summary>
    [Fact]
    public void EveryPlotSignificantPulseInTheClientHasARow()
    {
        List<string> unlisted = [];

        foreach (string file in Directory.EnumerateFiles(PagesDir(), "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(PagesDir(), file).Replace(Path.DirectorySeparatorChar, '/');
            string source = WithoutComments(File.ReadAllText(file));

            List<(int Start, int End)> claimed =
                [.. TellingTable.Where(r => r.File == relative)
                    .Select(r => BodyRange(source, r.Method))
                    .Where(b => b is not null)
                    .Select(b => b!.Value)];

            foreach (string token in PlotSignificantRankTokens)
            {
                for (int at = source.IndexOf(token, StringComparison.Ordinal);
                     at >= 0;
                     at = source.IndexOf(token, at + 1, StringComparison.Ordinal))
                {
                    if (!claimed.Any(c => at >= c.Start && at < c.End))
                    {
                        int line = source.Take(at).Count(ch => ch == '\n') + 1;
                        unlisted.Add($"{relative}:{line} — `{token}`");
                    }
                }
            }
        }

        Assert.True(unlisted.Count == 0,
            "a line has been given a plot-significant rank and no row in ThePlayerIsToldTests says what "
            + "moment it is or where the player reads it. Add the row — the rank is the claim, this is "
            + $"where the claim is kept:\n  {string.Join("\n  ", unlisted)}");
    }

    /// <summary>
    /// THE RATCHET BACKWARD: a silence that grows a voice must stop being filed as a silence.
    ///
    /// <para>Same shape as #663's orphan list and for the same reason. Six of these seven are a design the
    /// owner's horror depends on; the seventh is a debt. Either way the row is a claim about today's code,
    /// and a list that keeps saying "nothing is said here" after somebody wrote a line is a TODO with no
    /// owner.</para>
    /// </summary>
    [Fact]
    public void EverySilenceIsStillSilent()
    {
        List<string> spoke = [];

        foreach ((string moment, string file, string method, string why) in KnownSilences)
        {
            string source = WithoutComments(ReadPage(file));
            if (BodyRange(source, method) is not { } at)
            {
                spoke.Add($"{file}::{method} — no such method, so this silence is about code that is gone ({why})");
                continue;
            }

            string body = source[at.Start..at.End];
            string[] found = [.. TellingCalls.Where(t => body.Contains(t, StringComparison.Ordinal))];
            if (found.Length > 0)
            {
                spoke.Add($"{file}::{method} — \"{moment}\" is filed as deliberately unannounced ({why}) "
                    + $"and now says something: {string.Join(", ", found)}. Move it into TellingTable");
            }
        }

        Assert.True(spoke.Count == 0, string.Join("\n  ", spoke));
    }

    /// <summary>
    /// AND THE BEATS, WHICH ARE THE OTHER HALF OF THE LAW AND ARE ENUMERABLE.
    ///
    /// <para>A <see cref="StoryBeats.Beat"/> reaches the player through the one seam, and the seam decides
    /// the surface from <see cref="StoryBeats.PresentationOf"/> — a card, a plate at the edge, or a host's
    /// own card (#777). All three are surfaces; there is no fourth today. The failure this pins is the one
    /// the seam's own comment names: <i>"it matches no arm and shows nothing, which is the safe way to be
    /// wrong"</i> — safe for the engine, and for #761 the worst possible outcome, because the beat still
    /// spends its cadence and still writes its words into the log. A moment told once, into a book, and
    /// never again.</para>
    ///
    /// <para>So: every presentation the enum can name has an arm in <c>ShowStoryBeat</c>'s switch. Invent a
    /// fourth and this goes red on its first run, which is the day it is invented rather than the day
    /// somebody plays it.</para>
    /// </summary>
    [Fact]
    public void EveryWayABeatCanBePresentedPutsItOnASurface()
    {
        string seam = WithoutComments(ReadPage("Map.StoryCards.cs"));
        (int Start, int End) at = BodyRange(seam, "ShowStoryBeat")
            ?? throw new InvalidOperationException("ShowStoryBeat is gone — the story-beat seam has moved");
        string body = seam[at.Start..at.End];

        List<string> unshown = [.. Enum.GetValues<StoryBeats.Presentation>()
            .Where(p => !body.Contains($"case StoryBeats.Presentation.{p}:", StringComparison.Ordinal))
            .Select(p => p.ToString())];

        Assert.True(unshown.Count == 0,
            "a beat can be presented in a way the seam's switch has no arm for, so it spends its cadence, "
            + "writes its words to the log and puts NOTHING in front of the player — #761's law broken in "
            + $"the one place that also erases the evidence: {string.Join(", ", unshown)}");
    }

    /// <summary>
    /// THE FLOOR IS CORE'S, AND THIS FILE ONLY READS IT.
    ///
    /// <para>Everything above is decided by <see cref="PlotSignificantRankTokens"/>, and if that list were
    /// ever hand-typed the guard would keep enforcing a definition the game had moved on from. Pin both
    /// ends: it is derived from the enum, and <see cref="PulseRank.Status"/> — the default every unthought
    /// line in the game carries — is not in it.</para>
    /// </summary>
    [Fact]
    public void ThePlotSignificantRanksAreCoresAndNotACopy()
    {
        Assert.Equal(PulseRank.Beat, Telling.Floor);
        Assert.Contains("PulseRank.Beat", PlotSignificantRankTokens);
        Assert.Contains("PulseRank.Climax", PlotSignificantRankTokens);
        Assert.DoesNotContain("PulseRank.Status", PlotSignificantRankTokens);
        Assert.False(PulseRank.Status.IsPlotSignificant());

        Assert.Equal(
            Enum.GetValues<PulseRank>().Count(r => r >= Telling.Floor) + 1,   // …+ "Telling.Floor" itself
            PlotSignificantRankTokens.Length);
    }

    // ── THE PROOF THAT THIS WORLD CAN TELL PASS FROM FAIL ───────────────────────────────────────────────

    /// <summary>
    /// A GUARD THAT CANNOT GO RED IS A GREEN NUMBER NOBODY ASKED THE WORLD FOR — this house's fifth named
    /// bug class, and the reason every claim above is asked of synthetic code here as well as of the
    /// shipped kind. Four situations, and the checker has to disagree with itself across them.
    /// </summary>
    [Fact]
    public void TheLawCanActuallyFail()
    {
        const string told = """
            class X {
                private void ThePortRemembersYou()
                {
                    BankTheCrossing(charge);
                    SayItWhereTheyAreLooking(IllegalHeat.TheyRememberYouHere, Telling.Floor);
                }
            }
            """;
        const string logged = """
            class X {
                private void ThePortRemembersYou()
                {
                    BankTheCrossing(charge);
                    LogAutopilotEvent($"They remember you here.");
                }
            }
            """;

        // The extractor finds a body at all, and finds the right one.
        Assert.NotNull(BodyRange(told, "ThePortRemembersYou"));
        Assert.Null(BodyRange(told, "ThereIsNoSuchMethod"));

        (int Start, int End) toldAt = BodyRange(told, "ThePortRemembersYou")!.Value;
        (int Start, int End) loggedAt = BodyRange(logged, "ThePortRemembersYou")!.Value;

        // Mute versus told: the whole distinction the law rests on.
        Assert.True(Mentions(told[toldAt.Start..toldAt.End], TellingCalls));
        Assert.False(Mentions(logged[loggedAt.Start..loggedAt.End], TellingCalls));
        Assert.True(Mentions(logged[loggedAt.Start..loggedAt.End], LogCalls));

        // A rank below the floor does not buy a ranked row, and a card field nothing renders does not buy a
        // card row. Both are the ways a row could otherwise pass on a lie.
        Assert.True(ProofReaches(Surface.RankedPulse, "ShowPulseMessage(line, PulseRank.Climax);", ""));
        Assert.False(ProofReaches(Surface.RankedPulse, "ShowPulseMessage(line);", ""));
        Assert.False(ProofReaches(Surface.RankedPulse, "ShowPulseMessage(line, PulseRank.Status);", ""));
        Assert.True(ProofReaches(Surface.RaisedCard, "_shipEpitaph = new ShipEpitaph(", "@if (_shipEpitaph is"));
        Assert.False(ProofReaches(Surface.RaisedCard, "_shipEpitaph = new ShipEpitaph(", "nothing renders it"));

        // Comments are not code — a paragraph explaining a silence must not read as a telling.
        Assert.DoesNotContain(
            "ShowPulseMessage(", WithoutComments("// we could ShowPulseMessage(x) here but do not\nvoid f(){}"));
        Assert.Contains("\"// not a comment\"", WithoutComments("var s = \"// not a comment\"; // this is"));
    }

    /// <summary>And the sweep itself is asked the question <c>EveryStoryBeatHasACallerTests</c> asks of its
    /// own: a scan that silently reads nothing passes every claim above on an empty world.</summary>
    [Fact]
    public void TheSweepReallyReadsTheShippedPages()
    {
        Assert.True(TellingTable.Length >= 25, "the table has been emptied rather than corrected");
        Assert.Contains("SpaceSails", ReadPage("Map.Combat.Busted.cs"));

        string hive = WithoutComments(ReadPage("Map.Surface.Hive.cs"));
        Assert.NotNull(BodyRange(hive, "PressLiftButton"));
        Assert.NotNull(BodyRange(hive, "AssembleSomebody"));
    }
}
