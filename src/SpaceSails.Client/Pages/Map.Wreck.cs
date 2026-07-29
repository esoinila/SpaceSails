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
            "log" => LogFinding(w),
            "manifest" => ManifestFinding(w),
            _ => "🔎 Nothing here but cold deck plate.",
        });

        // The cause's own station is the one you STAND AND LOOK at, so it gets the wreck's portrait —
        // eight ships that died eight different ways should not all read the same. The card shows the
        // EVIDENCE, never the conclusion: naming what it means is still the captain's job.
        if (id == "cause" && Derelict.ArtFile(w.Cause) is { Length: > 0 })
        {
            _wreckLook = new WreckLook(spot.Label.Replace("✔ ", ""), Derelict.ArtFile(w.Cause), Derelict.Evidence(w.Cause));
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

    // The bridge log: how long she has been out here, and the shape of her last hours. This is what turns
    // "an old wreck" into "she was lost 31 years ago and nobody came" — the search-cone fiction, on a desk.
    private static string LogFinding(in Derelict.Wreck w) =>
        $"🖥 The log ends {w.YearsAdrift:N0} years ago. " +
        (w.Cause switch
        {
            Derelict.WreckCause.DriveFailure =>
                "The last hundred entries are the same restart attempt, timestamped every twenty minutes, for nine days.",
            Derelict.WreckCause.LifeSupportFailure =>
                "The entries stay calm, technical and hopeful right up until they stop mid-word.",
            Derelict.WreckCause.Mutiny =>
                "The last week is written in two hands that stop acknowledging each other, then one hand only.",
            Derelict.WreckCause.InsuranceJob =>
                "The distress call is in the log — drafted, revised, and SAVED four hours before the emergency it describes.",
            Derelict.WreckCause.NavigationalError =>
                "The last entry is a burn confirmation for a burn the fuel logs say never fired.",
            Derelict.WreckCause.Piracy =>
                "The last entry is a contact report. There is no entry after it.",
            _ => "The last entries are ordinary ship's business, and then there are no more.",
        });

    // The manifest: what she was carrying and what it is worth — the number both endings are priced on.
    private static string ManifestFinding(in Derelict.Wreck w) =>
        $"📦 The manifest assesses her cargo at {w.AssessedValueCr:N0} cr" +
        (w.Cause == Derelict.WreckCause.InsuranceJob
            ? " — countersigned twice, by the same hand, and the cargo seals have been opened and re-set."
            : w.Cause == Derelict.WreckCause.Piracy
                ? ". The near hold is empty; the deep hold is exactly as listed. Whoever boarded her was in a hurry."
                : ". It is all still aboard. Nobody has been here.");

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

        Derelict.SalvageOutcome outcome = Derelict.Resolve(w, choice, _wreckReported);

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
    private void SpawnWreckPack(int count)
    {
        for (int i = 0; i < count && _reevers.Count < ReeverEngineCeiling; i++)
        {
            // Spread along the aft spine and the deep compartments, so they come up the corridor rather
            // than appearing on top of the away team.
            double x = -28 + (i * 5);
            double y = i % 2 == 0 ? 0 : (i % 4 == 1 ? -5 : 5);
            _reevers.Add(new Reever
            {
                X = x,
                Y = y,
                Facing = 0,
                JitterSeed = ((_surface?.ThreatSeed ?? 0UL) * 0x9E3779B97F4A7C15UL) + (ulong)i + 1UL,
                // They know where the door is — it is the only one — so they converge on the airlock, and
                // the captain is between them and it.
                EverSeen = true,
                LastSeenX = WreckLayout.SpawnX,
                LastSeenY = WreckLayout.SpawnY,
            });
        }
    }

    /// <summary>Rebuild the derelict's walkable interior — the ✔ marks and the vanished salvage console
    /// are state, so the deck is rebuilt whenever they change.</summary>
    private void RebuildWreckDeck()
    {
        if (_wreck is not { } w || !OnWreck)
        {
            return;
        }

        _deckPlan = WreckInterior.WreckDeck(w, _wreckExamined, _wreckSalvaged, 3, FillSurfaceDroids, HeldDoors(), BlockedDoors());
    }

    /// <summary>The wreck's own header line, and the loiter promise under it — the reason the away team is
    /// relaxed enough to read a manifest instead of running for the shuttle.</summary>
    private string WreckHoldLine()
    {
        LoiterKeeping.Quote q = LoiterKeeping.Assess(0, 0, 0);
        return q.Line;
    }
}
