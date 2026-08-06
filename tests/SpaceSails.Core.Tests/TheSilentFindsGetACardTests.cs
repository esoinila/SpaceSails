using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #725 · THE TWO LOUDEST SILENT FINDS.
///
/// <para>Owner's audit question, walking the four handoff floors: <i>"Are we giving enough attention to
/// plot-significant finds? They should have a Gen-AI image and their own dialog by our standards."</i> Two
/// were met (THE SHAFT, DEAD AIR) and the two the handoff doc actually sends playtesters to were map
/// furniture — a wall stencil beside the shaft head, and a furnished empty room.</para>
///
/// <para>What has to be guarded is not the prose (that is authored and pinned verbatim below) but WHERE each
/// card is allowed to fire, because both triggers are the kind this ground has paid for before: one is a band
/// sum, and one is a room's box. Neither is re-derived anywhere — the plate card asks #694's own plate law,
/// and the mess room asks the same containment law the refuges use — and these say so.</para>
/// </summary>
public sealed class TheSilentFindsGetACardTests
{
    /// <summary>The rock <c>?secretlab=deep</c> parks: twenty listed floors of clinic with an unlisted band
    /// under them, pinned by <c>TheUnlistedBandTests.TheDeepCHEATRockStillHasSomethingUnderIt</c>. The site
    /// the two finds were found on, so it is the site they are guarded on.</summary>
    private const string CheatRock = "secret-lab-site-unlisted";

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>A spread of ordinary bodies — some hide a band, most do not, and the sweep needs both.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker", CheatRock,
    ];

    // ── THE PLATE ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The plate card fires on the band lobby and on no other floor of the building.
    ///
    /// <para><b>Proven RED</b> by the tempting wrong implementation — <c>IsUnlistedLobby =
    /// ShowsFacilityPlate</c>, i.e. forgetting that the plate law answers YES in two places and only one of
    /// them is the corrected wall:</para>
    /// <code>
    /// the plate card fires on the wrong floors:
    ///   luna B1: fired, wanted no
    ///   phobos B1: fired, wanted no
    ///   … (all eleven bodies)
    ///   secret-lab-site-unlisted B1: fired, wanted no
    /// </code>
    /// <para>And RED the other way by <c>level == BandTop(BandOf(level))</c> — "is this floor a band top",
    /// which #694's own doc comment rules out — which fires on twenty-four shaft heads that are not lobbies,
    /// including B5/B9/B13/B17 of the cheat rock and B21 of a site with nothing under it:</para>
    /// <code>
    ///   enceladus B21: fired, wanted no
    ///   secret-lab-site-unlisted B17: fired, wanted no
    /// </code>
    /// </summary>
    [Fact]
    public void ThePlateCardBelongsToTheBandLobbyAndToNoOtherFloor()
    {
        var wrong = new List<string>();
        int lobbies = 0, floors = 0;

        foreach (string body in Bodies)
        {
            // DERIVED, never typed. The floor the card belongs to is the top of the band nobody listed, and
            // the only place that arithmetic is done is Core's.
            int? lobby = UndergroundComplex.HasUnlistedBand(body)
                ? UndergroundComplex.BandTop(UndergroundComplex.UnlistedBandOf(body))
                : null;
            if (lobby is not null)
            {
                lobbies++;
            }

            // FloorsOf and not "−1 down to the depth": a site with an unlisted band has a GAP in it.
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                floors++;
                bool fires = UndergroundComplex.IsUnlistedLobby(body, level);
                bool want = lobby == level;
                if (fires != want)
                {
                    wrong.Add($"  {body} B{-level}: {(fires ? "fired" : "silent")}, wanted {(want ? "yes" : "no")}");
                }
            }
        }

        // A guard that swept a universe with nothing hidden in it would pass on an empty claim.
        Assert.True(lobbies >= 2, $"only {lobbies} of these bodies hide a band — this proved little.");
        Assert.True(floors > 60, $"only {floors} floors were walked — this proved little.");
        Assert.True(wrong.Count == 0,
            "the plate card fires on the wrong floors:\n" + string.Join("\n", wrong));
    }

    /// <summary>
    /// The entrance lobby is the other floor the plate law says yes to, and it is the one floor this card
    /// must never fire on: B1's plate names the building you rode down into, and the whole beat of the card
    /// is a plate that names something else. Stated on its own because it is the failure mode.
    /// </summary>
    [Fact]
    public void ItNeverFiresInTheEntranceLobbyWhereThePlateIsMerelyCorrect()
    {
        int b1 = UndergroundComplex.BandTop(0);

        foreach (string body in Bodies)
        {
            // The world is honest: B1 really is a plate floor everywhere, so "never fires here" is a claim
            // about a floor that had every chance to fire.
            Assert.True(UndergroundComplex.ShowsFacilityPlate(body, b1), $"{body} B1 shows no plate at all.");
            Assert.False(UndergroundComplex.IsUnlistedLobby(body, b1),
                $"{body} B1 raised THE PLATE — that is the building naming itself correctly, not a correction.");
        }
    }

    /// <summary>And the derived floor of the site the finds were found on, recorded now that it is derived:
    /// twenty storeys of clinic, and the corrected wall is B21. Written as an assertion about the DERIVATION
    /// rather than as the trigger, so the card still follows the building if the seeding ever moves.</summary>
    [Fact]
    public void OnTheCheatRockTheCorrectedWallIsTwentyOneFloorsDown()
    {
        Assert.True(UndergroundComplex.HasUnlistedBand(CheatRock));
        int lobby = UndergroundComplex.BandTop(UndergroundComplex.UnlistedBandOf(CheatRock));

        Assert.Equal(-21, lobby);
        Assert.True(UndergroundComplex.IsUnlistedLobby(CheatRock, lobby));
        Assert.Equal(-20, UndergroundComplex.DepthOf(CheatRock));   // twenty floors file and grade, above it
    }

    // ── THE STAFF MESS ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The mess card's room is the mess room, and standing in any other room on that floor is not standing
    /// in it.
    ///
    /// <para><b>Proven RED</b> by swapping the containment law for the plausible hand-typed one a "near
    /// enough to the machines" trigger would have used — <c>Math.Abs(x - X) &lt;= 20 &amp;&amp; Math.Abs(y - Y)
    /// &lt;= 20</c> — which reaches the room across the corridor:</para>
    /// <code>
    /// the mess room holds ground that is not the mess room:
    ///   luna B13: room centre (6.0, -117.7) is 16.2 du away and reads as inside
    ///   phobos B13: room centre (6.0, -117.7) is 16.2 du away and reads as inside
    ///   callisto B5: room centre (6.0, -133.8) is 16.2 du away and reads as inside
    /// </code>
    /// </summary>
    [Fact]
    public void TheMessRoomHoldsTheMessRoomAndNoneOfTheFloorsOtherRooms()
    {
        var wrong = new List<string>();
        int checkedRooms = 0, checkedMesses = 0;

        foreach (string body in Bodies)
        {
            if (UndergroundComplex.StaffCanteenFloor(body) is not { } level)
            {
                continue;   // a three-floor annex has one canteen, and that is the honest answer
            }

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            UndergroundComplex.Amenity mess = Assert.Single(
                floor.Amenities, a => a.Use == UndergroundComplex.Comfort.StaffCanteen);
            checkedMesses++;

            // The middle of the room is inside it. (A box that held nothing would pass every clause below.)
            Assert.True(mess.Contains(mess.X, mess.Y), $"{body} B{-level}: the mess does not hold its own centre.");

            // …and every other room on the floor is somewhere else. These are the FloorPlan's own centres,
            // not a re-walk of the generator.
            foreach ((double rx, double ry) in floor.RoomCentres)
            {
                checkedRooms++;
                if (mess.Contains(rx, ry))
                {
                    double d = Math.Sqrt(((rx - mess.X) * (rx - mess.X)) + ((ry - mess.Y) * (ry - mess.Y)));
                    wrong.Add($"  {body} B{-level}: room centre ({rx:F1}, {ry:F1}) is {d:F1} du away and reads as inside");
                }
            }

            // …as is the lift the captain walked in from, which is the one spot every excursion stands on.
            (double sx, double sy) = UndergroundComplex.ShaftAt(Field);
            if (mess.Contains(sx, sy))
            {
                wrong.Add($"  {body} B{-level}: the mess reads as holding the LIFT");
            }

            // …and the floor's other amenities, where it has any.
            foreach (UndergroundComplex.Amenity other in floor.Amenities)
            {
                if (other.Use != mess.Use && mess.Contains(other.X, other.Y))
                {
                    wrong.Add($"  {body} B{-level}: the mess reads as holding the {other.Use}");
                }
            }
        }

        Assert.True(checkedMesses >= 4, $"only {checkedMesses} sites had a staff mess — this proved little.");
        Assert.True(checkedRooms > 40, $"only {checkedRooms} other rooms were tried — this proved little.");
        Assert.True(wrong.Count == 0,
            "the mess room holds ground that is not the mess room:\n" + string.Join("\n", wrong));
    }

    /// <summary>The mess exists on exactly the floor the directory puts it on, so the card cannot be raised
    /// by an amenity the client found somewhere else. The room IS the trigger, so which floors carry one is
    /// the other half of the same law.</summary>
    [Fact]
    public void TheMessIsOnlyEverOnTheFloorTheDirectoryPutsItOn()
    {
        var wrong = new List<string>();
        int floors = 0;

        foreach (string body in Bodies)
        {
            int? want = UndergroundComplex.StaffCanteenFloor(body);
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                floors++;
                bool has = UndergroundComplex.Build(body, level, Field).Amenities
                    .Any(a => a.Use == UndergroundComplex.Comfort.StaffCanteen);
                if (has != (want == level))
                {
                    wrong.Add($"  {body} B{-level}: {(has ? "has" : "has no")} staff mess, wanted " +
                              $"{(want == level ? "one" : "none")}");
                }
            }
        }

        Assert.True(floors > 60, $"only {floors} floors were walked — this proved little.");
        Assert.True(wrong.Count == 0, "the staff mess is not where the directory puts it:\n" + string.Join("\n", wrong));
    }

    /// <summary>And the derived floor, recorded: the deepest floor the cheat rock's directory admits to that
    /// still breathes is B17, which is where the room nobody eats in is.</summary>
    [Fact]
    public void OnTheCheatRockTheMessIsSeventeenFloorsDown()
    {
        Assert.Equal(-17, UndergroundComplex.StaffCanteenFloor(CheatRock));
    }

    // ── WHAT THEY SAY, WHICH IS ONLY WHAT IS THERE ───────────────────────────────────────────────────────

    /// <summary>Both cards, verbatim. Authored by the writer and wired by hand, which is exactly the seam
    /// where a word gets "improved" — so the whole of each is pinned, not a fragment of it.</summary>
    [Fact]
    public void BothCardsSayWhatWasAuthoredAndNotAWordElse()
    {
        Assert.Equal(
            "The car opens on a lobby with no department and no livery — bare pour, one lamp, somebody's " +
            "chair. Beside the shaft there is a plate, and the plate has been done twice: a wide patch of " +
            "newer paint first, laid over something larger, and then the small name screwed on over that. " +
            "Good work, both times — a crew that stencils for a living, sent down here to change an answer. " +
            "It is not the name of anything you rode down through. You read it again. It says what it said. " +
            "Above you, twenty floors file and grade and answer to one name; the wall down here has been " +
            "corrected.",
            UndergroundComplex.UnlistedLobbyCard);

        Assert.Equal(
            "A mess for the staff: machines on the wall still holding their temperature, chairs squared to " +
            "the tables the way a crew squares them at the end of a shift it expects to repeat. The door " +
            "wanted a pass shown. Inside there is nobody to show it to, and nothing out of place — no tray " +
            "abandoned, no chair shoved back, no note. Whatever ended here was not sudden, or it was tidied. " +
            "The machines hum and keep their hours. The shift has not come, and the machines are not the " +
            "kind that wonder.",
            UndergroundComplex.StaffMessCard);
    }

    /// <summary>Neither card explains, and neither quotes the plate. The text on the plate varies by site
    /// kind, so a card that named it would be prose transcribing a sign the renderer draws — the same fact
    /// in two places — and it would also be the one sentence that turns the find into an answer.</summary>
    [Fact]
    public void NeitherCardEXPLAINSAnythingAndNeitherQuotesTheSign()
    {
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];

        foreach (string card in new[]
                 { UndergroundComplex.UnlistedLobbyCard, UndergroundComplex.StaffMessCard })
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, card, StringComparison.OrdinalIgnoreCase);
            }

            // Every facility name the plate can carry, checked against the prose the card puts beside it.
            foreach (UndergroundComplex.Kind kind in Enum.GetValues<UndergroundComplex.Kind>())
            {
                string title = UndergroundComplex.TitleOf(kind).TrimStart('▣', ' ');
                Assert.DoesNotContain(title, card, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>Each names a slot, and no two cards down here share a painting or a title — a card that
    /// borrowed a sibling's picture is #695's complaint one floor along.</summary>
    [Fact]
    public void EachSilentFindHasItsOwnSlotAndItsOwnName()
    {
        string[] art =
        [
            UndergroundComplex.UnlistedLobbyArtUrl, UndergroundComplex.StaffMessArtUrl,
            UndergroundComplex.VacuumArtUrl, UndergroundComplex.DescentArtUrl,
            UndergroundComplex.SealedWayArtUrl, UndergroundComplex.HeadOfficeArrivalArtUrl,
            UndergroundComplex.WinteringHallArtUrl, UndergroundComplex.BerthOfficeArtUrl,
        ];
        string[] labels =
        [
            UndergroundComplex.UnlistedLobbyLabel, UndergroundComplex.StaffMessLabel,
            UndergroundComplex.VacuumCardLabel, UndergroundComplex.DescentCardLabel,
            UndergroundComplex.SealedWayCardLabel, UndergroundComplex.HeadOfficeArrivalLabel,
            UndergroundComplex.WinteringHallLabel, UndergroundComplex.BerthOfficeLabel,
        ];

        foreach (string url in art)
        {
            Assert.StartsWith("art/", url, StringComparison.Ordinal);
            Assert.EndsWith(".jpg", url, StringComparison.Ordinal);
        }
        Assert.Equal(art.Length, art.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(labels.Length, labels.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(labels, string.IsNullOrWhiteSpace);
    }
}
