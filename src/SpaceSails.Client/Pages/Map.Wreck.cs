using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #488 · THE SALVAGE RUN. Owner: <i>"let's make the wreck case. A kind of salvage run and exploration …
/// the accident investigation might be something we could even get paid from … or we might want to loot it
/// all and never tell anyone."</i>
///
/// <para>The loop: a derelict hangs in shuttle range, the ship HOLDS on her (<see cref="LoiterKeeping"/> —
/// free, because Lab 40 says a co-orbital hold costs nothing), the away team boards, walks her, reads the
/// evidence off what is bolted to the deck, and then decides. File the report and take a finder's fee and
/// a contact; or strip her and say nothing.</para>
///
/// <para>Everything mechanical lives in <see cref="Derelict"/> (Core, pure, tested). This file is the
/// client's half: spawn her, let the captain look, and spend what Core decides.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>How many hold units a stripped wreck's cargo rides home as. It is the VALUE that matters
    /// (Core priced it); the units are so the hold, the collector and the fence all see something real.</summary>
    private const int SalvageCargoUnits = 3;

    /// <summary>The cargo class stripped salvage is stamped under — its own name, because "where did you
    /// get this" is exactly the question it should invite.</summary>
    private const string SalvageCargoClass = "salvage";

    /// <summary>The wreck currently in reach, if any — seeded from her id, so she is the same wreck every
    /// time anyone looks at her.</summary>
    private Derelict.Wreck? _wreck;

    /// <summary>Which evidence stations the away team has actually read. Naming the cause is only allowed
    /// from what has been LOOKED AT — the investigation is legwork, not a guess.</summary>
    private readonly HashSet<string> _wreckExamined = [];

    /// <summary>Set once she has been filed or stripped — she is finished either way.</summary>
    private bool _wreckSalvaged;

    /// <summary>The open decision card, when the captain is standing at the cargo deciding.</summary>
    private bool _showWreckChoice;

    /// <summary>What the captain believes happened — picked on the choice card from the causes their
    /// evidence actually supports. Null until they commit to a reading.</summary>
    private Derelict.WreckCause? _wreckReported;

    /// <summary>The outcome card after the decision lands.</summary>
    private Derelict.SalvageOutcome? _wreckOutcome;

    /// <summary>What the away team is standing and looking at — the wreck's own portrait of how she died,
    /// raised when the cause's station is read.</summary>
    private readonly record struct WreckLook(string Title, string Art, string Caption);

    private WreckLook? _wreckLook;

    private void CloseWreckLook() => _wreckLook = null;

    /// <summary>Is the away team currently inside a derelict (rather than on a moon)?</summary>
    private bool OnWreck =>
        _surface is { } ex && Derelict.TryParseWreckId(ex.Stop.Body.Id, out _);

    // ── Reading her ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Press E at an evidence station: read what is actually there. Each station can only tell you
    /// what it tells you — the cause's own damage says the most, the log and the manifest are the
    /// corroboration that lets a careful captain catch a wreck that lies.</summary>
    private void ExamineWreckEvidence()
    {
        if (_wreck is not { } w
            || _deckPlan.NearestConsoleSpot(_avatarX, _avatarY)
                is not { Kind: DeckPlan.ConsoleKind.WreckEvidence } spot)
        {
            return;
        }

        string id = EvidenceIdFor(spot.Label);
        bool fresh = _wreckExamined.Add(id);

        ShowPulseMessage(id switch
        {
            "cause" => $"🔎 {Derelict.Evidence(w.Cause)}",
            "log" => Derelict.LogFinding(w),
            "manifest" => ManifestFindingWithTheClue(w),
            _ => "🔎 Nothing here but cold deck plate.",
        });

        // The cause's own station is the one you STAND AND LOOK at, so it gets the wreck's portrait —
        // eight ships that died eight different ways should not all read the same. The card shows the
        // EVIDENCE, never the conclusion: naming what it means is still the captain's job.
        if (id == "cause")
        {
            // #488: THE SAME ROOM, AFTER THE VACUUM HAD IT. Owner: "should we have a different pic after
            // vent cycle… one with claw marks :-D" — the proof the soak could never offer, because the
            // counter only ever gave a number and the instrument never would say what was in there. Keeps
            // the rule: what you find is claw marks and a collapsed nest, never the thing that made them.
            bool cleared = CauseRoomIsFinished()
                           && Derelict.ArtFileCleared(w.Cause) is { Length: > 0 };

            string art = cleared ? Derelict.ArtFileCleared(w.Cause) : Derelict.ArtFile(w.Cause);
            string caption = cleared ? Derelict.EvidenceCleared(w.Cause) : Derelict.Evidence(w.Cause);

            if (art.Length > 0)
            {
                _wreckLook = new WreckLook(spot.Label.Replace("✔ ", ""), art, caption);
            }
        }

        // #654 · AND THE OTHER TWO STATIONS, ON EVERY HULL. The log and the manifest are not colour: they
        // are the CORROBORATION that lets a careful captain catch a wreck that lies, and reading both is the
        // one thing that takes the decoy off the choice card. They were a pulse line that faded in a second
        // and a half while the damage two rooms away got a full plate.
        //
        // ONE generic painting each, and the same one on all ten hulls — see Derelict.LogArtFile for why
        // that is not laziness. The caption does every bit of the differentiating.
        else if (id is "log" or "manifest")
        {
            _wreckLook = new WreckLook(
                spot.Label.Replace("✔ ", ""),
                id == "log" ? Derelict.LogArtFile : Derelict.ManifestArtFile,
                id == "log" ? Derelict.LogCaption(w) : Derelict.ManifestCaption(w));
        }

        if (fresh)
        {
            RendererInterop.PlayCue("reveal");
            RebuildWreckDeck();   // the station now reads ✔
        }
    }

    // The station's id, recovered from its label (the label carries a ✔ once read).
    private static string EvidenceIdFor(string label) =>
        label.Contains("LOG", StringComparison.OrdinalIgnoreCase) ? "log"
        : label.Contains("MANIFEST", StringComparison.OrdinalIgnoreCase) ? "manifest"
        : "cause";

    // The bridge log and the manifest USED TO BE TWO PRIVATE SWITCHES HERE, out of reach of any test and out
    // of sight of Derelict.Evidence — which narrates the same ship. They disagreed: the vented hull had no
    // arm in the log switch, so the station printed "the log ends N years ago … and then there are no more"
    // while her evidence, on the same screen, said the log runs on for months in one immaculate hand. The
    // words the log should have spoken were already written, and Core-tested, in HullVenting.VentedShipLogLine
    // — read by nothing at all. Both switches now live in Core beside the evidence they must agree with.
    //
    // #633 · AND THE SWITCHES CAME BACK ON `main`, WITH ONE THING IN THEM THAT IS NOT PROSE. While this
    // branch was moving the words into Core, `main` was adding #537's search clue to the client's copy: a
    // hull that hides a void books one compartment longer than her deck plan draws it, and reading the
    // manifest properly is what hands you that discrepancy. The prose stays in Core, where the tests can see
    // it agree with the evidence; the CLUE stays here, because it is about THIS boarding's hidden void and
    // nothing in Core knows that. One fact, one owner, each.

    /// <summary>
    /// #537 · THE MANIFEST IS WHERE THE LIE IS. Core writes the document; this adds what reading it properly
    /// tells you about the hull you are standing in. On a clean hull the frame numbers match down the page
    /// and it is an honest dead end, which it has to be: a document that only speaks up when there is
    /// something to find is a pointer, not a clue.
    /// </summary>
    private string ManifestFindingWithTheClue(in Derelict.Wreck w)
    {
        string cargo = Derelict.ManifestFinding(w);
        _manifestRead = true;

        if (_hullVoid is not { } hidden)
        {
            return cargo + " Her shielding is booked section by section, and every section holds the same.";
        }

        HullSounding.Discrepancy clue = HullSounding.AsDiscrepancy(hidden);

        LogAutopilotEvent(HullSounding.ClueLine(clue));
        LogAutopilotEvent(HullSounding.BlindSearchLine(
            SoundingGear,
            HullSounding.HullArea(WreckLayout.AftX, WreckLayout.BowX, WreckLayout.TopY, WreckLayout.BottomY)));

        return cargo + " " + HullSounding.ClueLine(clue);
    }

    /// <summary>The causes the captain may put their name to: the ones their evidence supports. Reading
    /// only the damage lets you name the obvious answer — and a wreck that LIES will hand you the wrong
    /// one. The log and the manifest are what let you tell the difference.</summary>
    private IReadOnlyList<Derelict.WreckCause> WreckCandidateCauses()
    {
        if (_wreck is not { } w)
        {
            return [];
        }

        var options = new List<Derelict.WreckCause> { w.Cause };
        if (Derelict.MisreadsAs(w.Cause) is { } decoy)
        {
            options.Add(decoy);
        }

        // Corroboration narrows it: once BOTH the log and the manifest have been read, the decoy is off
        // the table — the captain has done the work and can see through the dressing.
        if (_wreckExamined.Contains("log") && _wreckExamined.Contains("manifest"))
        {
            options.RemoveAll(c => c != w.Cause);
        }

        options.Sort();
        return options;
    }

    /// <summary>Has the away team looked at enough to file at all? One station is a glance; the cause plus
    /// one corroboration is a finding.</summary>
    private bool CanFileWreckReport => _wreckExamined.Count >= 2;

    // ── The decision ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Press E at the cargo: open the two roads.</summary>
    private void OpenWreckChoice()
    {
        if (_wreck is null || _wreckSalvaged)
        {
            return;
        }
        _showWreckChoice = true;
        _wreckReported = WreckCandidateCauses().Count == 1 ? WreckCandidateCauses()[0] : null;
    }

    private void CloseWreckChoice() => _showWreckChoice = false;

    /// <summary>Commit. Core prices it; this spends it.</summary>
    private void ResolveWreck(Derelict.SalvageChoice choice)
    {
        if (_wreck is not { } w || _wreckSalvaged)
        {
            return;
        }

        // #524 · SHE IS WORTH WHAT IS LEFT OF HER. A fire eats an equal share per compartment while the
        // captain decides what to do about it, so the payout resolves against the burnt-down hull rather than
        // the one the manifest remembers. Derelict.Resolve stays pure; the wreck is a record struct, so what
        // it is handed is simply a smaller ship.
        Derelict.Wreck burnt = w with { AssessedValueCr = SalvageValueNow(w) };
        Derelict.SalvageOutcome outcome = Derelict.Resolve(burnt, choice, _wreckReported);
        if (burnt.AssessedValueCr < w.AssessedValueCr)
        {
            LogAutopilotEvent(HullFire.CostLine(w.AssessedValueCr, burnt.AssessedValueCr));
        }

        _credits += outcome.CreditsNow;
        if (outcome.HeatGained > 0)
        {
            _heat = EncounterRule.RaiseHeat(_heat, outcome.HeatGained, SimTime);
        }
        if (outcome.CargoIsHot)
        {
            // It is somebody's INSURED cargo and it is aboard us now — so it rides in the hold as hot,
            // through the same ledger a plundered pod does. A collector who stops us will know it on sight.
            int room = Math.Max(0, CargoCapacity - _cargoUnits);
            int taken = Math.Min(SalvageCargoUnits, room);
            if (taken > 0)
            {
                _cargoUnits += taken;
                _cargoValue += outcome.CreditsNow;
                _hotCargo.Stamp(SalvageCargoClass, taken, _heat.Level);
            }
        }

        _wreckSalvaged = true;
        _showWreckChoice = false;
        _wreckOutcome = outcome;

        // THE CREW WERE WATCHING. Owner: "too honest a captain takes their winnings." This is the exact
        // moment that opinion is formed — the one decision on the ship where doing the right thing is
        // visibly worse paid — so the crew's sheet reads it here rather than keeping a ledger of its own.
        //
        // Honest means: filed, AND filed as what she actually was. Filing her under a cause you know is
        // wrong is the profitable road wearing the paperwork's clothes, and the crew are not fooled by it
        // even when the depot is.
        if (choice == Derelict.SalvageChoice.FileTheReport && _wreckReported == w.Cause)
        {
            NoteHonestFiling();
        }
        else
        {
            NoteProfitableLie();
        }

        LogAutopilotEvent(choice == Derelict.SalvageChoice.FileTheReport
            ? $"📋 Filed on the {w.ShipName} — {outcome.CreditsNow:N0} cr."
            : $"🏴 Stripped the {w.ShipName} — {outcome.CreditsNow:N0} cr, and she stays lost.");

        RendererInterop.PlayCue(outcome.ContactEarned ? "reveal" : "board");
        RebuildWreckDeck();
        RequestVaultSave();
    }

    private void DismissWreckOutcome() => _wreckOutcome = null;

    // ── The hull ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Put what got in aboard her — deep aft, around the nest, already aware.
    ///
    /// <para>Deliberate, not ambient: <c>StepTide</c> refuses to run on a wreck because there is no ground
    /// for anything to claw out of, and a hull that quietly filled with Old Ones would tell a different
    /// story than her evidence does. What is aboard a wreck gets put there ON PURPOSE — and this is the
    /// purpose. They know the airlock, which is the only way out, so the walk back is the encounter.</para></summary>
    /// <summary>Whether the compartment the cause's own evidence stands in has been opened to space AND left
    /// there long enough for the vacuum to finish. That is the condition for the after-picture: not "you
    /// pulled the handle", but "you waited."</summary>
    private bool CauseRoomIsFinished()
    {
        if (_wreck is not { } w)
        {
            return false;
        }

        DeckReachability.Point at = WreckLayout.CauseStation(w.Cause);
        foreach ((string name, float x0, float x1, bool top) in WreckLayout.Compartments)
        {
            bool inX = at.X >= x0 && at.X <= x1;
            bool inY = top ? at.Y < -WreckLayout.SpineHalfHeight : at.Y > WreckLayout.SpineHalfHeight;
            if (inX && inY && _ventSpaces.TryGetValue(name, out HullVenting.Space s))
            {
                return s.Vented && HullVenting.SoakComplete(s);
            }
        }
        return false;
    }

    /// <summary>The middle of a named compartment — where a noise made in it comes from.</summary>
    private static (double X, double Y) RoomCentre(string name)
    {
        (string _, float x0, float x1, bool top) = System.Array.Find(
            WreckLayout.Compartments, c => c.Name == name);
        return ((x0 + x1) / 2.0, top ? -6.0 : 6.0);
    }

    /// <summary>Which compartment a point on the deck is in, or null for the spine.</summary>
    private static string? RoomAt(double x, double y)
    {
        foreach ((string name, float x0, float x1, bool top) in WreckLayout.Compartments)
        {
            bool inX = x >= x0 && x <= x1;
            bool inY = top ? y < -WreckLayout.SpineHalfHeight : y > WreckLayout.SpineHalfHeight;
            if (inX && inY)
            {
                return name;
            }
        }
        return null;
    }

    /// <summary>How long the hull stays quiet before the first one stirs. Long enough that the away team
    /// gets to read a station or two and start believing her. FLAGGED for tuning.</summary>
    private const double WreckPackFirstWakeMs = 35_000;

    /// <summary>And they come round in ones and twos, not as a pack — so the ship wakes AROUND the captain
    /// rather than at them.</summary>
    private const double WreckPackWakeStaggerMs = 22_000;

    /// <summary>How close the lamp has to be for a hibernating one to be drawn at all. Owner: they do not
    /// show on the map "unless they are within our observed vision space" — so what puts a dormant contact
    /// on screen is not the tracker (it is not moving) but the captain's own eyes.</summary>
    private const double DormantSightRange = 9.0;

    /// <summary>
    /// #488 · THE NOISE YOU MAKE. Owner: <i>"I like them to be unaware"</i> — and, crucially, <i>"that
    /// unawareness was a solution to them all charging the player at once… instead they come one or two at
    /// a time as they see you as you explore deeper."</i>
    ///
    /// <para>So this is built to protect that pacing, not to spend it. A pump, a blown compartment, a
    /// cracked valve, an undogged hatch, the overload PA — each one reaches the NEAREST contacts and no more
    /// than <see cref="NoiseRousesAtMost"/> of them. The rest sleep through it. Making a racket can never
    /// summon the ship; it can only ever cost you one or two, which is the same rate exploring deeper costs
    /// you, and that is the rate this hull is tuned to.</para>
    ///
    /// <para>And what they get is a PLACE, never a target — the idiom already in this codebase: "they know
    /// the DIG, not the captain; the ear hands out a place rather than a target." They walk to where the
    /// sound came from. If the captain has moved on, they arrive at nothing, and the tracker's ghost sits
    /// burning over an empty compartment.</para>
    /// </summary>
    private void MakeNoiseAboard(double x, double y, double earshot)
    {
        if (!OnWreck)
        {
            return;
        }

        // #538 · COLLEAGUES HEAR ABOUT IT. The sweep team is alerted FIRST and separately, because the two ears
        // are different instruments: the pack's is capped at the nearest two and does not care who else is aboard,
        // while three professionals share a channel — if one hears it, they all hear about it. And this must run
        // even when the pack is empty, which the old early return quietly prevented.
        AlertSweepersToNoise(x, y);

        if (_reevers.Count == 0)
        {
            return;
        }

        int roused = 0;
        foreach (Reever r in _reevers.OrderBy(r =>
                     ((r.X - x) * (r.X - x)) + ((r.Y - y) * (r.Y - y))))
        {
            if (roused >= NoiseRousesAtMost)
            {
                break;
            }

            double dx = r.X - x, dy = r.Y - y;
            if ((dx * dx) + (dy * dy) > earshot * earshot)
            {
                continue;   // out of earshot: it sleeps through whatever you just did
            }

            if (r.Dormant)
            {
                // Loud enough to shorten forty years by a couple of minutes.
                r.WakeAtMs = System.Math.Min(r.WakeAtMs, (_lastTimestampMs ?? 0) + 1_500);
                roused++;
                continue;
            }

            // Awake and idle: it goes to look at the sound. Not at you — at the place.
            r.LastSeenX = x;
            r.LastSeenY = y;
            r.EverSeen = true;
            r.Idle = false;
            roused++;
        }
    }

    /// <summary>The hard cap that keeps noise from undoing what unawareness bought. Two, ever — the same
    /// rate walking deeper costs you.</summary>
    private const int NoiseRousesAtMost = 2;

    /// <summary>How far a working pump or a blown hatch carries through a dead hull.</summary>
    private const double LoudEarshot = 26.0;

    /// <summary>And a quieter piece of work — a hatch dogged by hand, a valve cracked.</summary>
    private const double QuietEarshot = 13.0;

    /// <summary>One comes round. The first is the one that matters: it is the moment the ship stops being a
    /// salvage job, and it arrives as an instrument appearing rather than an announcement.</summary>
    private void WakeTheSleeper(Reever r)
    {
        r.Dormant = false;
        r.VisibleOnMap = true;
        r.Idle = false;

        // WAKING IS NOT KNOWING. It comes round where it lay, with no idea anyone is aboard — it has to
        // find you the same way anything else does, by laying eyes on you. Handing it the captain's exact
        // position here is what made six of them converge from behind closed doors the moment they stirred.

        bool first = !_anythingHasWokenAboard;
        _anythingHasWokenAboard = true;

        ShowPulseMessage(first
            ? "🕷 Something that has not moved in forty years moves. It does not stretch and it does not " +
              "hurry — it simply stops being furniture, and starts being aimed at you."
            : "🕷 Another one comes round, somewhere behind you.");

        ApplyNerveShock(NervePips.SightingPips * (int)NervePips.PipUnit,
            first ? "she was not empty after all" : "another one is up");
        RendererInterop.PlayCue("alarm");
    }

    /// <summary>Whether anything aboard has woken yet — the first one gets the line that matters.</summary>
    private bool _anythingHasWokenAboard;

    /// <summary>Whether the motion fan has come up on this hull. It appears the first time anything moves
    /// and then stays — an ear does not un-hear — so the appearing itself is the warning.</summary>
    private bool _wreckTrackerLive;

    private void SpawnWreckPack(int count)
    {
        for (int i = 0; i < count && _reevers.Count < ReeverEngineCeiling; i++)
        {
            // Spread along the aft spine and the deep compartments, so they come up the corridor rather
            // than appearing on top of the away team.
            double x = -28 + (i * 5);
            double y = i % 2 == 0 ? 0 : (i % 4 == 1 ? -5 : 5);

            // NEVER SPAWN ONE INTO A ROOM IT CANNOT LEAVE. Owner, on the cheat now reaching any hull:
            // "will they be in closed room maybe :-D" — and on a vented wreck every hatch starts dogged, so
            // a pack placed in a compartment would sit there forever and the ship would read falsely quiet.
            // Anything whose room is shut at spawn goes into the spine instead.
            //
            // The captain trapping one LATER is a different thing entirely, and a legitimate play: dog the
            // hatch on a room with something in it and it cannot get out — but you cannot get in, and you
            // will never be certain what you shut the door on.
            if (y != 0 && RoomAt(x, y) is { } room
                && _ventSpaces.TryGetValue(room, out HullVenting.Space s)
                && HullVenting.DoorwayBlocked(s, _spinePressurised))
            {
                y = 0;
            }
            _reevers.Add(new Reever
            {
                X = x,
                Y = y,
                Facing = 0,
                JitterSeed = ((_surface?.ThreatSeed ?? 0UL) * 0x9E3779B97F4A7C15UL) + (ulong)i + 1UL,
                // They know where the door is — it is the only one — so they converge on the airlock, and
                // the captain is between them and it.
                // THEY DO NOT KNOW YOU ARE ABOARD. Owner, jumped by all six: "how did they detect me so
                // early so well from behind doors" — because they were spawned already hunting, with the
                // airlock as a known target. The rule this codebase already had is the right one: one that
                // has never laid eyes on the captain keeps its own ground. They have been asleep for forty
                // years; nobody has told them anything.
                EverSeen = false,

                // She reads EMPTY when you board her. Every one of them is folded down somewhere aft,
                // hibernating, and comes round on its own clock — staggered, so the hull wakes in ones and
                // twos rather than all at once. Not drawn, not moving, and therefore not on a motion
                // tracker: the first sign is the fan coming up on a ship you were told was dead.
                Dormant = true,
                VisibleOnMap = false,
                WakeAtMs = (_lastTimestampMs ?? 0)
                           + WreckPackFirstWakeMs
                           + (i * WreckPackWakeStaggerMs)
                           + (DiceRule.Roll(DiceRule.Seed("wake", (long)i, (long)(_surface?.ThreatSeed ?? 0UL)), 20).Face * 900.0),
            });
        }
    }

    /// <summary>Whether the shuttle the captain is about to load is crossing to a DERELICT rather than
    /// going down to a world. The boarding card was written for the regolith and said so in every line —
    /// burying a chest, three seeded landing sites with names like The Quiet Basin, a shovel and a pickaxe
    /// — all offered for a dead ship with a steel deck (owner, on the Cold Harvest: <i>"it shows me three
    /// options on where to board … those are good for Miranda etc moons but … you know"</i>).</summary>
    private bool BoardingAWreck =>
        _boardTarget is { } target && Derelict.TryParseWreckId(target.Body.Id, out _);

    /// <summary>Rebuild the derelict's walkable interior — the ✔ marks and the vanished salvage console
    /// are state, so the deck is rebuilt whenever they change.</summary>
    private void RebuildWreckDeck()
    {
        if (_wreck is not { } w || !OnWreck)
        {
            return;
        }

        _deckPlan = WreckInterior.WreckDeck(
            w, _wreckExamined, _wreckSalvaged, 3 + ReeverEngineCeiling, FillSurfaceDroids,
            HeldDoors(), BlockedDoors(), _archiveAboard, _archivePurged);
    }

    /// <summary>The wreck's own header line, and the loiter promise under it — the reason the away team is
    /// relaxed enough to read a manifest instead of running for the shuttle.</summary>
    private string WreckHoldLine()
    {
        LoiterKeeping.Quote q = LoiterKeeping.Assess(0, 0, 0);
        return q.Line;
    }
}
