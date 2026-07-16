namespace SpaceSails.Core.Tests;

/// <summary>PR-D1 (docs/WednesdayPlan/UnifiedNavListNotes.md): the read-only derivation behind the
/// "flight plan" presentation and the owner's NOW status ask — one source of truth for step states
/// and the now/next line so the pilot banner, the Nav header, and the list never contradict.</summary>
public class FlightPlanStatusTests
{
    // ---- Step states ----

    [Fact]
    public void BurnState_PlannedUntilFlown_ThenDone_StaleWins()
    {
        Assert.Equal(FlightStepState.Planned, FlightPlanStatusBuilder.BurnState(stale: false, executed: false));
        Assert.Equal(FlightStepState.Done, FlightPlanStatusBuilder.BurnState(stale: false, executed: true));
        // A struck-out node reads Stale even if it happened to fire — the edit invalidated it.
        Assert.Equal(FlightStepState.Stale, FlightPlanStatusBuilder.BurnState(stale: true, executed: false));
        Assert.Equal(FlightStepState.Stale, FlightPlanStatusBuilder.BurnState(stale: true, executed: true));
    }

    [Fact]
    public void InsertionState_ArmedUntilTheApproachIsBeingFlown()
    {
        Assert.Equal(FlightStepState.Armed, FlightPlanStatusBuilder.InsertionState(flyingApproach: false));
        Assert.Equal(FlightStepState.Active, FlightPlanStatusBuilder.InsertionState(flyingApproach: true));
    }

    // ---- NOW line ----

    [Fact]
    public void Now_Coasting_WhenNothingIsFlyingTheShip()
    {
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: false, AutopilotFlyingApproach: false, AutopilotBodyName: null,
            NextStepLabel: null, NextStepEta: null));
        Assert.Equal("NOW: coasting", s.NowLine);
        Assert.Null(s.NextLine);
    }

    [Fact]
    public void Now_ArmedButNotYetFlying_SaysCoastingArmed()
    {
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: false, AutopilotBodyName: "Titan",
            NextStepLabel: "insertion at Titan", NextStepEta: "at window"));
        // #159/#184 owner mock: the coast phase names the transfer arc and its target, in plain language.
        Assert.Equal("NOW: coasting the transfer arc to Titan", s.NowLine);
        Assert.Equal("NEXT: insertion at Titan at window", s.NextLine);
    }

    [Fact]
    public void NextRow_SpeaksTheHarborVocabularyVerbatim_OneVoice()
    {
        // #203: the banner NEXT row is the same text HarborVocabulary composes — a real orbit keeps
        // "orbit-insert (alt N km)", a μ≤0 dock haven reads "dock envelope … slow to ≤8 km/s". The
        // caller composes the label through the one truth; the builder only slots and prefixes it.
        FlightPlanStatus orbit = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: false, AutopilotBodyName: "Enceladus",
            NextStepLabel: null, NextStepEta: null,
            UpcomingSteps: new[]
            {
                new FlightPlanStep(HarborVocabulary.ArrivalStep(HarborClass.Orbit, "Enceladus", "alt 313 km"), "at window", FlightStepState.Armed),
            }));
        Assert.Equal("NEXT: orbit-insert at Enceladus (alt 313 km) at window", orbit.NextLine);

        FlightPlanStatus dock = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: false, AutopilotBodyName: "Cinder Roost",
            NextStepLabel: null, NextStepEta: null,
            UpcomingSteps: new[]
            {
                new FlightPlanStep(HarborVocabulary.ArrivalStep(HarborClass.Dock, "Cinder Roost"), "in 1 h", FlightStepState.Armed),
            }));
        Assert.Equal("NEXT: dock envelope at Cinder Roost — slow to ≤8 km/s in 1 h", dock.NextLine);
        Assert.DoesNotContain("orbit-insert", dock.NextLine!);
    }

    [Fact]
    public void Now_FlyingApproach_NamesTheAutopilotAndBody()
    {
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: true, AutopilotBodyName: "Titan",
            NextStepLabel: "insertion at Titan", NextStepEta: "at window"));
        // #171: plain language — the ship SAYS it is approaching, and the queue names the orbit-insert.
        Assert.Equal("NOW: approaching Titan — autopilot flying", s.NowLine);
    }

    [Fact]
    public void Now_Inserting_OutranksApproach()
    {
        // #171/#173: the window is open, the autopilot is circularizing — the NOW line must say
        // "inserting into orbit", not leave the captain guessing "orbit or crash?".
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: true, AutopilotBodyName: "Enceladus",
            NextStepLabel: null, NextStepEta: null,
            AutopilotInserting: true));
        Assert.Equal("NOW: inserting into orbit at Enceladus", s.NowLine);
    }

    [Fact]
    public void Now_HoldingLine_IsTheKeptOrbitNowRow_BelowDockAboveFlying()
    {
        // Coordination seam (Friday §0, priority lane): a kept orbit's verbatim line becomes the NOW
        // row, outranking every flying phase but not a dock.
        const string held = "🛰 AUTOPILOT HOLDS THE ORBIT — Enceladus, 313 km, trim ≈2 p/day";
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: true, AutopilotBodyName: "Enceladus",
            NextStepLabel: null, NextStepEta: null,
            AutopilotInserting: true, HoldingLine: held));
        Assert.Equal(held, s.NowLine);
        Assert.Equal(held, s.Rows[0].Text);

        // A dock still wins over a kept orbit.
        FlightPlanStatus docked = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: true, DockedHavenName: "Ringside",
            AutopilotArmed: false, AutopilotFlyingApproach: false, AutopilotBodyName: null,
            NextStepLabel: null, NextStepEta: null, HoldingLine: held));
        Assert.Equal("NOW: docked at Ringside", docked.NowLine);
    }

    [Fact]
    public void Now_Docked_OutranksEverything()
    {
        // Even with autopilot flags set, a clamped ship is docked — nav is locked.
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: true, DockedHavenName: "Ringside",
            AutopilotArmed: true, AutopilotFlyingApproach: true, AutopilotBodyName: "Titan",
            NextStepLabel: null, NextStepEta: null));
        Assert.Equal("NOW: docked at Ringside", s.NowLine);
    }

    // ---- NEXT line ----

    [Fact]
    public void Next_BurnWithEta_Composed()
    {
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: false, AutopilotFlyingApproach: false, AutopilotBodyName: null,
            NextStepLabel: "burn ▲ 14 p", NextStepEta: "in 2d 4h"));
        Assert.Equal("NEXT: burn ▲ 14 p in 2d 4h", s.NextLine);
    }

    [Fact]
    public void Next_LabelWithoutEta_OmitsTheTail()
    {
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: false, AutopilotFlyingApproach: false, AutopilotBodyName: null,
            NextStepLabel: "insertion at Titan", NextStepEta: null));
        Assert.Equal("NEXT: insertion at Titan", s.NextLine);
    }

    [Fact]
    public void Next_UsesUppercaseLabel_SoItReadsAsWhatHappensNext()
    {
        // Round-2 blind-UI audit (docs/MondayPonder/UIUsabilityNotes.md): the old "next: …" line was
        // not read cold as "what the ship will do next". The label is now an uppercase NEXT: cue that
        // mirrors the NOW: line, so who-is-flying + what-next read as one unit.
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: false, AutopilotFlyingApproach: false, AutopilotBodyName: null,
            NextStepLabel: "burn ▼ 3 p", NextStepEta: "in 5h"));
        Assert.StartsWith("NEXT: ", s.NextLine);
        Assert.Equal("NEXT: burn ▼ 3 p in 5h", s.NextLine);
    }

    // ---- Handback (#147): the persistent "you have the ship" line ----

    [Fact]
    public void Now_HandbackReason_SurfacesAsPersistentManualLine()
    {
        // The autopilot handed the ship back (fuel plan broken by a manual burn). Not armed, not
        // docked — the NOW line must say so, persistently, so it survives high warp.
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: false, AutopilotFlyingApproach: false, AutopilotBodyName: null,
            NextStepLabel: null, NextStepEta: null,
            HandbackReason: "autopilot handed back — fuel plan broken by manual burns"));
        Assert.Equal("NOW: manual — autopilot handed back — fuel plan broken by manual burns", s.NowLine);
    }

    [Fact]
    public void Now_ArmedAgain_OutranksAStaleHandbackReason()
    {
        // Re-arming means the ship is flying again; the armed line wins over any lingering reason.
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: false, AutopilotBodyName: "Enceladus",
            NextStepLabel: "insertion at Enceladus", NextStepEta: "at window",
            HandbackReason: "something old"));
        Assert.Equal("NOW: coasting the transfer arc to Enceladus", s.NowLine);
    }

    [Fact]
    public void Now_Docked_OutranksHandbackReason()
    {
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: true, DockedHavenName: "Ringside",
            AutopilotArmed: false, AutopilotFlyingApproach: false, AutopilotBodyName: null,
            NextStepLabel: null, NextStepEta: null,
            HandbackReason: "handed back"));
        Assert.Equal("NOW: docked at Ringside", s.NowLine);
    }

    [Fact]
    public void Now_NoHandback_StillPlainCoasting()
    {
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: false, AutopilotFlyingApproach: false, AutopilotBodyName: null,
            NextStepLabel: null, NextStepEta: null,
            HandbackReason: "   "));
        Assert.Equal("NOW: coasting", s.NowLine);
    }

    [Fact]
    public void Build_FallsBackWhenNamesMissing()
    {
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: true, DockedHavenName: null,
            AutopilotArmed: false, AutopilotFlyingApproach: false, AutopilotBodyName: null,
            NextStepLabel: null, NextStepEta: null));
        Assert.Equal("NOW: docked at haven", s.NowLine);
    }

    // ---- #159/#184: the multi-row banner list ----

    [Fact]
    public void Rows_NowIsAlwaysTheActiveFirstRow()
    {
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: false, AutopilotFlyingApproach: false, AutopilotBodyName: null,
            NextStepLabel: null, NextStepEta: null));
        Assert.Single(s.Rows);
        Assert.Equal(FlightRowKind.Now, s.Rows[0].Kind);
        Assert.True(s.Rows[0].IsActive);
        Assert.Equal(FlightStepState.Active, s.Rows[0].State);
        Assert.Equal("NOW: coasting", s.Rows[0].Text);
    }

    [Fact]
    public void Rows_LegacyNextBecomesTheSecondRow_AndNextLine()
    {
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: false, AutopilotBodyName: "Titan",
            NextStepLabel: "insertion at Titan", NextStepEta: "at window"));
        Assert.Equal(2, s.Rows.Count);
        Assert.Equal(FlightRowKind.Next, s.Rows[1].Kind);
        Assert.Equal("NEXT: insertion at Titan at window", s.Rows[1].Text);
        Assert.Equal(s.NextLine, s.Rows[1].Text);
        Assert.False(s.Rows[1].IsActive);
    }

    [Fact]
    public void Rows_UpcomingSteps_NameBurnThenApproachThenInsert_TopToBottom()
    {
        // The full planned flight spoken as rows: two pending burns queued ahead of the orbit-insert.
        // More than two rows → the banner shows its ▲▼ arrows.
        var steps = new List<FlightPlanStep>
        {
            new("burn ▲ 14 p", "in 2d 4h", FlightStepState.Planned),
            new("burn ▼ 3 p", "in 3d 1h", FlightStepState.Planned),
            new("orbit-insert at Enceladus (313 km)", "at window", FlightStepState.Armed),
        };
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: false, AutopilotBodyName: "Enceladus",
            NextStepLabel: null, NextStepEta: null,
            UpcomingSteps: steps));

        Assert.Equal(4, s.Rows.Count); // NOW + 3 queued
        Assert.Equal(FlightRowKind.Now, s.Rows[0].Kind);
        Assert.Equal("NOW: coasting the transfer arc to Enceladus", s.Rows[0].Text);
        Assert.Equal(FlightRowKind.Next, s.Rows[1].Kind);
        Assert.Equal("NEXT: burn ▲ 14 p in 2d 4h", s.Rows[1].Text);
        Assert.Equal(FlightRowKind.Later, s.Rows[2].Kind);
        Assert.Equal("THEN: burn ▼ 3 p in 3d 1h", s.Rows[2].Text);
        Assert.Equal(FlightRowKind.Later, s.Rows[3].Kind);
        Assert.Equal("THEN: orbit-insert at Enceladus (313 km) at window", s.Rows[3].Text);
        Assert.Equal(FlightStepState.Armed, s.Rows[3].State);
    }

    [Fact]
    public void Rows_ApproachAndInsert_AreSeparateRows_ClosingTheCrashOrOrbitDoubt()
    {
        // #171/#173: while flying the approach, the queue still names the orbit-insert as its own row —
        // the ship SAYS it will orbit, it is not left to be discovered.
        var steps = new List<FlightPlanStep>
        {
            new("orbit-insert at Enceladus (313 km)", "at window", FlightStepState.Armed),
        };
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: true, AutopilotBodyName: "Enceladus",
            NextStepLabel: null, NextStepEta: null,
            UpcomingSteps: steps));
        Assert.Equal("NOW: approaching Enceladus — autopilot flying", s.Rows[0].Text);
        Assert.Equal("NEXT: orbit-insert at Enceladus (313 km) at window", s.Rows[1].Text);
    }

    [Fact]
    public void Rows_BlankStepLabels_AreSkipped()
    {
        var steps = new List<FlightPlanStep>
        {
            new("   ", "in 1h", FlightStepState.Planned),
            new("orbit-insert at Titan", "at window", FlightStepState.Armed),
        };
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: false, AutopilotBodyName: "Titan",
            NextStepLabel: null, NextStepEta: null,
            UpcomingSteps: steps));
        Assert.Equal(2, s.Rows.Count); // the blank step dropped; insert promoted to NEXT
        Assert.Equal(FlightRowKind.Next, s.Rows[1].Kind);
        Assert.Equal("NEXT: orbit-insert at Titan at window", s.Rows[1].Text);
    }
}
