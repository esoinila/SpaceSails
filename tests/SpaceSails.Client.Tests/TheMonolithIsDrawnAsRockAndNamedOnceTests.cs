using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1411 · R1-F32 and R1-F81. The monolith is drawn as rock (a flat fill, no section hatch — the hatch is the
/// idiom for CUT material), and each landmark's name is printed once, at its console.
///
/// <para>Proven red on revert: drop <c>IsStone: true</c> from the monolith's Structure in
/// <c>MoonSurface.Layout.cs</c> and the first law goes red; drop the <c>labels.RemoveAll</c> and the second goes
/// red for both grounds.</para>
/// </summary>
public sealed class TheMonolithIsDrawnAsRockAndNamedOnceTests
{
    private static DeckPlan GroundOf(string body) =>
        MoonSurface.SurfaceDeck(body, body, [], 0, (_, _) => { }, "", "");

    [Fact]
    public void TheMonolithsMassIsStoneAndNotCutFoam()
    {
        DeckPlan plan = GroundOf(Monolith.BodyId);
        DeckPlan.Structure mass = Assert.Single(plan.Structures);
        Assert.True(mass.IsStone, "the monolith's mass would be hatched as cut material, on the object whose card says 'No seam'.");
    }

    [Theory]
    [InlineData("phobos")]
    [InlineData("miranda")]
    public void ALandmarkNameIsPrintedOnceNotInTheMassAndAgainAtTheConsole(string body)
    {
        DeckPlan plan = GroundOf(body);
        string[] named = plan.Consoles.Where(c => c.Label.Contains("THE MONOLITH") || c.Label.Contains("THE FALSE SLAB"))
            .Select(c => c.Label).ToArray();
        Assert.NotEmpty(named);   // the ground has its landmark console, or this proves nothing
        foreach (string label in named)
        {
            Assert.DoesNotContain(plan.RoomLabels, l => l.Text == label);
        }
    }

    [Theory]
    [InlineData("phobos")]
    [InlineData("luna")]
    public void TheShipsOwnRoomNamesStayDrawnOnTheRegolith(string body)
    {
        DeckPlan plan = GroundOf(body);
        Assert.Contains(plan.RoomLabels, l => l.Text.Contains("CANTINA"));
        Assert.Contains(plan.RoomLabels, l => l.Text.Contains("SHUTTLE BAY"));
    }
}
