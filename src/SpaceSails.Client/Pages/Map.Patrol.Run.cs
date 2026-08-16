using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Patrol (#870 lane 6′c; the header note lives in Map.Patrol.cs) — the PAGE's half of the other branch: the four things the landing, the gun, the surface frame and the droid buffer still ask the round for by name.
public sealed partial class Map
{
    /// <inheritdoc cref="Patrol.IssueTheSitePass"/>
    private void IssueTheSitePass(SurfaceExcursion ex) => _patrol.IssueTheSitePass(ex);

    /// <inheritdoc cref="Patrol.SomebodySawThat"/>
    private void SomebodySawThat(PatrolBeat.Provocation why) => _patrol.SomebodySawThat(why);

    /// <inheritdoc cref="Patrol.TheKickedOutPlate"/>
    private (float X, float Y, string Text, float Px, int Tone)[]? TheKickedOutPlate(SurfaceExcursion ex) =>
        _patrol.TheKickedOutPlate(ex);

    /// <inheritdoc cref="Patrol.FillPatrolDroids"/>
    private void FillPatrolDroids(DeckPlan.Droid[] buffer, int firstSlot) =>
        _patrol.FillPatrolDroids(buffer, firstSlot);
}
