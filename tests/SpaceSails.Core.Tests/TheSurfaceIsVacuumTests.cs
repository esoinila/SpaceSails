using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #802 · THE SURFACE IS VACUUM, AND EVERY LABEL HAS TO SAY SO.
///
/// <para>Owner, 2026-08-09: <i>"the surface should be vacuum / unbreathable. I think it might have been
/// marked as breathable in the elevator buttons (not sure). Being unbreathable makes the breathing so much
/// more scary."</i> He was right, and he was right about the exact place: <c>LiftPanel</c> typed
/// <c>Pressurised: true</c> into its SURFACE row, so the panel drew <c>🫁 air</c> over the one button every
/// captain presses on the way out and titled it <i>holds pressure</i>.</para>
///
/// <para><b>Nothing else in the game ever agreed with it.</b> The drain hands back
/// <see cref="SuitAir.Supply.Tanks"/> at level 0 and spends the tank from the first step; the plate by the
/// car prints <c>NO ATMOSPHERE · TANK RUNNING</c>; <c>AirCard</c> says <i>"there is no air on this moon to
/// help you"</i>. One sentence, one world of its own — the house bug class, and the reason a sweep like this
/// one asks the SIM, the PANEL and the HUD's own words in the same loop and refuses to let any of the three
/// answer alone.</para>
///
/// <para><b>Why the guard is a sweep over every row and not just the surface.</b> The bug was not a wrong
/// value, it was a TYPED value: a row that answered for itself instead of asking
/// <see cref="UndergroundComplex.HoldsPressure"/>. <see cref="EveryButtonOnEveryPanelAgreesWithTheOnePressureFact"/>
/// is the shape of that mistake rather than its symptom, so the next hand-written row goes red wherever it
/// is written.</para>
/// </summary>
public sealed class TheSurfaceIsVacuumTests
{
    /// <summary>Every body the game lands on, plus the three cheat sites — including the one rock with
    /// GALLERIES under it, without which every sweep here audits a universe where the found band does not
    /// exist and passes for the wrong reason.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    /// <summary>The surface, and the two levels above it a caller might ever hand in (the regolith is
    /// "0 or above" everywhere in this codebase).</summary>
    private static readonly int[] AboveGround = [0, 1, 2];

    [Fact]
    public void NoBodyHoldsPressureOnTheGROUNDItIsLANDEDOn()
    {
        // The one pressure fact, asked about the one floor everybody starts and ends on. This is what the
        // panel row now reads, so it is where the law is pinned.
        foreach (string body in Bodies)
        {
            foreach (int level in AboveGround)
            {
                Assert.False(
                    UndergroundComplex.HoldsPressure(body, level),
                    $"{body} at level {level}: the regolith is claiming to hold air.");
            }
        }
    }

    [Fact]
    public void TheSIMSpendsTANKOnEverySURFACE()
    {
        // The sim's half of the agreement — asked exactly as the drain asks it, standing on open ground:
        // no shelter drum underfoot, not past her tube's door, no refuge (there are none up here).
        foreach (string body in Bodies)
        {
            SuitAir.Supply supply = SuitAir.SourceOf(body, 0, insideShelter: false, aboard: false);

            Assert.Equal(SuitAir.Supply.Tanks, supply);
            Assert.True(SuitAir.Drawing(supply), $"{body}: the tank is parked on the surface.");
        }
    }

    [Fact]
    public void TheLIFTPANELSaysDEADAIROverSURFACEFromEveryFloorOfEveryBody()
    {
        // THE BUG, stated as the law it broke. Ride to the panel from anywhere in any facility: the row that
        // takes you home must never promise air on the ground it takes you to.
        //
        // RED-PROOF: put `Pressurised: true` back into LiftPanel's first row and this fails on every body.
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.LiftStop surface =
                    Assert.Single(UndergroundComplex.LiftPanel(body, level, []), s => s.Level == 0);

                Assert.False(
                    surface.Pressurised,
                    $"{body} B{-level}: the panel's SURFACE button says the surface holds air.");
            }
        }
    }

    [Fact]
    public void EveryButtonOnEveryPanelAgreesWithTheOnePressureFact()
    {
        // …and not only the surface one. A row that decides for itself is the mistake; this is the mistake's
        // shape, so a hand-written answer anywhere on the panel goes red here.
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.LiftStop stop in UndergroundComplex.LiftPanel(body, level, []))
                {
                    Assert.Equal(UndergroundComplex.HoldsPressure(body, stop.Level), stop.Pressurised);
                }
            }
        }
    }

    [Fact]
    public void ThePANELThePLATEAndTheGAUGESayTheSameThingAboutTheSURFACE()
    {
        // The three instruments a captain standing on the regolith can read, in one loop. Two of them being
        // right was never the standard: the whole issue is one of them being wrong on its own.
        foreach (string body in Bodies)
        {
            SuitAir.Supply supply = SuitAir.SourceOf(body, 0, insideShelter: false, aboard: false);

            // The plate by the car, in SuitAir's own words.
            Assert.Equal("NO ATMOSPHERE · TANK RUNNING", SuitAir.PlateLine(supply));

            // The HUD chip under the tracker — the word and the glyph the gauge paints.
            Assert.Equal("TANKS", SuitAir.SourceLabel(supply));
            Assert.Equal("▼", SuitAir.SourceGlyph(supply));

            // And the button. Same fact, three surfaces, no way for one to drift.
            UndergroundComplex.LiftStop surface =
                Assert.Single(UndergroundComplex.LiftPanel(body, -1, []), s => s.Level == 0);
            Assert.Equal(SuitAir.Drawing(supply), !surface.Pressurised);
        }
    }

    [Fact]
    public void SteppingOutOfTheCarOntoTheSURFACEIsACROSSINGIntoVACUUM()
    {
        // The sentence the crossing says, since the panel now tells the truth before the ride as well as
        // after it. #740's law: the sentence owns its facts, and this one is the tank cutting in.
        Assert.Contains("VACUUM", SuitAir.SupplyChangedLine(SuitAir.Supply.Tanks));

        // …and the pressurised line — the one the surface row used to imply — is the OTHER world's.
        Assert.DoesNotContain("VACUUM", SuitAir.SupplyChangedLine(SuitAir.Supply.Room));
    }

    [Fact]
    public void TheOnlyWaysToBreatheAboveGroundAreADRUMAndHERTUBE()
    {
        // The surface is vacuum; a shelter's drum and the ship are not the surface. Stated so the guard
        // above can never be satisfied by an accident that also broke the two places air legitimately is.
        foreach (string body in Bodies)
        {
            Assert.Equal(
                SuitAir.Supply.Room,
                SuitAir.SourceOf(body, 0, insideShelter: true, aboard: false));
            Assert.Equal(
                SuitAir.Supply.Ship,
                SuitAir.SourceOf(body, 0, insideShelter: false, aboard: true));
        }
    }
}
