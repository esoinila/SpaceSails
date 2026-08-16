namespace SpaceSails.Client.Pages;

/// <summary>
/// #870 lane 6′c — WHERE THE PAGE BUILDS ITS COLLABORATORS, AND THE WHOLE FILE IS FOUR LINES.
///
/// <para>Two of this page's families are objects with an explicit surface now rather than a heap of loose
/// fields: the seat (<see cref="Seating"/>, behind <see cref="ISeatHost"/>) and the round
/// (<see cref="Patrol"/>, behind <see cref="IPatrolHost"/>). Each is handed the page it works on, and an
/// instance field initialiser may not name <c>this</c> — so each is assigned in a constructor.</para>
///
/// <para><b>And a class gets exactly one parameterless constructor</b>, which is why this file exists at all.
/// 6c put the assignment in <c>Seating/Seating.cs</c>; the moment the round needed the same treatment, that
/// one constructor had to name a field of the PATROL family from inside a file of the SEAT family — which is
/// precisely what the seat's own "reaches the page through one door" sweep is there to catch. The choice was
/// an exemption in a guard or a neutral file for the one line that belongs to neither family. This is the
/// neutral file, and the next collaborator adds one line to it and touches nobody's sweep.</para>
///
/// <para><b>Nothing else may happen here.</b> A Blazor component is constructed by the framework before its
/// parameters are set and before any injection; everything this page actually does starts at
/// <c>OnInitialized</c> / <c>OnAfterRenderAsync</c>.</para>
/// </summary>
public sealed partial class Map
{
    /// <inheritdoc cref="Map"/>
    public Map()
    {
        _seating = new Seating(this);
        _patrol = new Patrol(this);
    }
}
