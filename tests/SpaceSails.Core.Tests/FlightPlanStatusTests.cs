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
        Assert.Equal("NOW: coasting — autopilot armed for Titan", s.NowLine);
        Assert.Equal("NEXT: insertion at Titan at window", s.NextLine);
    }

    [Fact]
    public void Now_FlyingApproach_NamesTheAutopilotAndBody()
    {
        FlightPlanStatus s = FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: false, DockedHavenName: null,
            AutopilotArmed: true, AutopilotFlyingApproach: true, AutopilotBodyName: "Titan",
            NextStepLabel: "insertion at Titan", NextStepEta: "at window"));
        Assert.Equal("NOW: autopilot approach → Titan", s.NowLine);
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
        Assert.Equal("NOW: coasting — autopilot armed for Enceladus", s.NowLine);
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
}
