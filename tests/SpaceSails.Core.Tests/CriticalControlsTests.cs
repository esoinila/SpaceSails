namespace SpaceSails.Core.Tests;

using SpaceSails.Core;
using static SpaceSails.Core.OverlayLayout;

/// <summary>
/// #299 — the registry grown past the single lifeline (#293). Every always-must-be-pressable affordance —
/// the distress/safety family (rescue reopen, the tow-offer accept, the warning shot) and the #212
/// affordances (dock/clamp release, autopilot disengage, the long-haul engage, the context menu's first
/// action) — is put under the same reachability law at every viewport from 1280×800 down to 320×480. Each
/// control's z-index comes from <see cref="OverlayBands"/>, which <see cref="CssZBandSyncTests"/> proves the
/// CSS mirrors — so this gate and the stylesheet can never silently disagree.
/// </summary>
public class CriticalControlsTests
{
    public static TheoryData<string, double, double> ControlsBySize()
    {
        (double W, double H)[] sizes =
        [
            (1280, 800), // desktop
            (1024, 768), // small laptop
            (390, 844),  // phone portrait
            (844, 390),  // phone landscape
            (320, 480),  // the smallest supported canvas
        ];
        TheoryData<string, double, double> data = new();
        foreach (CriticalControls.Control c in CriticalControls.Roster)
        {
            foreach ((double w, double h) in sizes)
            {
                data.Add(c.Name, w, h);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ControlsBySize))]
    public void EveryCriticalControl_IsReachable_AtEverySize(string name, double w, double h)
    {
        CriticalControls.Control control = CriticalControls.Roster.Single(c => c.Name == name);
        Rect viewport = new(0, 0, w, h);

        Overlay ctrl = control.Build(viewport);
        ReachResult r = Evaluate(ctrl, viewport, control.CoRaised(viewport));

        Assert.True(r.Ok,
            $"{name} ({control.Family}) unreachable at {w:F0}x{h:F0}: {r.Verdict} " +
            $"(free {r.FreeWidth:F0}x{r.FreeHeight:F0}; occluded by [{string.Join(", ", r.OccludedBy)}])");
    }

    [Fact]
    public void TheSafetyFamily_IsPresent_SoTheMinimumBarIsCovered()
    {
        // The distress/safety family is the non-negotiable minimum (#299): rescue, answer the tow, de-escalate.
        string[] safety = CriticalControls.Roster
            .Where(c => c.Family == CriticalControls.SafetyFamily)
            .Select(c => c.Name)
            .ToArray();

        Assert.Contains("rescue-reopen-pill", safety);
        Assert.Contains("rescue-offer-accept", safety);
        Assert.Contains("warning-shot", safety);
    }

    [Fact]
    public void EveryControl_DrawsItsZIndex_FromABandConstant()
    {
        // No control may carry a hand-picked z-value; the set of legal z-indexes is exactly the band scale.
        HashSet<int> bandValues =
        [
            OverlayBands.MapDestPanel, OverlayBands.DeskLayer, OverlayBands.MapBodyMenu, OverlayBands.MapDossier,
            OverlayBands.MapHud, OverlayBands.MapTopstack, OverlayBands.ParrotPerch, OverlayBands.DeckViewToggle,
            OverlayBands.MapLayers, OverlayBands.MapLoading, OverlayBands.DeckPulseToast, OverlayBands.DeckOfferCard,
            OverlayBands.DeckShuttleCard, OverlayBands.StartPickerBackdrop, OverlayBands.ViewObjectBackdrop,
            OverlayBands.PinBackdrop, OverlayBands.MapAdrift, OverlayBands.RescueBackdrop,
            OverlayBands.MissionCelebrationBackdrop, OverlayBands.JumpOverlay, OverlayBands.BustedBackdrop,
        ];
        Rect viewport = new(0, 0, 1280, 800);

        foreach (CriticalControls.Control c in CriticalControls.Roster)
        {
            int z = c.Build(viewport).ZIndex;
            Assert.True(bandValues.Contains(z), $"{c.Name} z-index {z} is not an OverlayBands layer value");
        }
    }
}
