using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// WHERE THE DOOR IS, AND WHETHER THERE IS ONE — the seeded placement of a body's hidden door, the
/// beach-comber square a probe must ping to reveal it, whether this body reads the company intranet, and
/// the footprint the chamber keeps out.
///
/// <para>Pure of (body id, field): the same body always answers the same way, because a secret that
/// re-rolls is a lottery rather than a place.</para>
///
/// <para>Split out of <c>SecretLab.cs</c> under #251 with no member renamed, re-scoped or re-ordered.</para>
/// </summary>
public static partial class SecretLab
{
    /// <summary>The seeded placement of a body's hidden door: whether the body hides a lab at all, the door's
    /// ground position, and the beach-comber square a probe must ping to reveal it. Pure of (body id, field).</summary>
    public readonly record struct Placement(
        bool HasLab, double DoorX, double DoorY, int DoorSquareX, int DoorSquareY);

    /// <summary>The box a door may be drawn into, for the side this body's lab grows to: the field's safe span
    /// less the WHOLE run of the mountain, which since #1383 begins <see cref="HutStandOff"/> past the door.
    /// One answer for <see cref="For"/> and for <see cref="HeadSpot"/>'s retries, so a relocated shed cannot
    /// grow its mountain out through the field edge the seed would have kept it off.</summary>
    internal static (double LoX, double HiX, double LoY, double HiY) DoorRange(string bodyId, in SurfaceLayout.Field field)
    {
        double lo = field.LeftX + SurfaceLayout.EdgeMargin;
        double hi = field.RightX - SurfaceLayout.EdgeMargin;
        bool growsRight = Frac(bodyId, "door-side") < 0.5;
        double run = TotalDepth + HutStandOff;

        return (growsRight ? lo : lo + run,
                growsRight ? hi - run : hi,
                field.BottomY + (RoomWidth / 2.0) + 2.0,
                field.AnchorY + 12.0);   // deep, well below the landing band
    }

    /// <summary>Resolve a body's hidden-door placement inside its field. <paramref name="forcePresent"/> lets
    /// the client's <c>?secretlab=1</c> cheat guarantee a lab on the test body regardless of the seed (Core
    /// stays deterministic; only the cheat overrides). Pure — the same body always answers the same way.</summary>
    public static Placement For(string bodyId, in SurfaceLayout.Field field, bool forcePresent = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // The door's ground spot: a seeded pocket in the DEEP field (a committed walk from the tube), kept clear
        // of the far edges so the whole lab has room to grow inside the safe span.
        //
        // THE RESERVATION IS THE FULL DEPTH NOW, AND THE SIDE IS SEEDED. When the lab was one chamber it fitted
        // whichever way it grew, so the old rule reserved RoomDepth on BOTH sides and let the direction fall out
        // of which half the door landed in. Three chambers run 38 du into the rock — more than half the field —
        // and reserving that on both sides inverts the range: there is no spot with 38 du spare in each
        // direction. `Region_Bounds_StayInsideTheFieldsSafeSpan` caught it on the first run, which is exactly
        // the audit doing its job.
        //
        // So the side is drawn from the seed and the position is drawn from that side's own valid range. The
        // lab still lands anywhere along the field; it simply no longer telegraphs which way it runs from where
        // its door is, which is a small improvement thrown in for free.
        (double loX, double hiX, double loY, double hiY) = DoorRange(bodyId, field);
        double doorX = Lerp(loX, hiX, Frac(bodyId, "door-x"));
        double doorY = Lerp(loY, hiY, Frac(bodyId, "door-y"));

        bool has = forcePresent || Present(bodyId);
        (int sqX, int sqY) = BeachComber.SquareOf(doorX, doorY);
        return new Placement(has, doorX, doorY, sqX, sqY);
    }

    /// <summary>#1119 item 2 · <b>THE PLACEMENT AS THIS SITE ACTUALLY BUILT IT — the one every client reader
    /// should hold.</b>
    ///
    /// <para><see cref="For"/> answers a question about a BODY: the seeded pocket the door was rolled into,
    /// which knows nothing about the shelters, the outpost hut and the monolith standing on that particular
    /// site. <see cref="HeadSpot"/> is that spot after the site has had its say, and it is where the shed is
    /// drawn (<see cref="HeadHut"/>), where the ground is kept clear (<see cref="ChamberFootprint"/>), where
    /// #625 points the tracker's ring and its rumour wash, and where the lift car sets the captain down.</para>
    ///
    /// <para>The hidden-door CONSOLE was the one thing still reading the raw spot — measured up to 235 du
    /// from the hut on 21 of 34 body × site pairs, because the raw spot is seeded per BODY and the clamp
    /// re-seeds per SITE, so when it fires it does not nudge, it RELOCATES. The instrument and the ground
    /// disagreeing is the #573 family, and #584 is the map lying; this was both at once.</para>
    ///
    /// <para>Rather than teach eight call sites to remember a second function, the excursion holds a
    /// placement that is ALREADY the resolved one — door spot and beach-comber square alike — so the console,
    /// the chamber the force appends, the alarm's doors, the plate on the card, the detector's needle and the
    /// reveal square are one fact by construction. <see cref="HasLab"/> is still the honest roll.</para></summary>
    /// <param name="siteSalt">This landing site's layout salt — the reason the answer is per-site.</param>
    public static Placement OnThisSite(
        string bodyId, string? siteSalt, in SurfaceLayout.Field field, bool forcePresent = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        Placement seeded = For(bodyId, field, forcePresent);
        (double hx, double hy) = HeadSpot(bodyId, siteSalt, field);
        (int sqX, int sqY) = BeachComber.SquareOf(hx, hy);
        return new Placement(seeded.HasLab, hx, hy, sqX, sqY);
    }

    /// <summary>Whether the seed alone (no cheat) hides a lab on this body — 1 in <see cref="ExpeditionOneInN"/>
    /// in the deep field of an away-expedition site, 1 in <see cref="OrdinaryOneInN"/> on an ordinary moon.</summary>
    public static bool Present(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int oneInN = ExpeditionSite.TryParseKind(bodyId, out _) ? ExpeditionOneInN : OrdinaryOneInN;
        return DiceRule.Roll(DiceRule.Seed($"secretlab:has:{bodyId}"), oneInN).Face == 1;
    }

    /// <summary>
    /// #1052 (L1) · <b>THE SEAM: what does this place read?</b> Whether the news a captain raises HERE is
    /// the facility's own <see cref="NewsWire.NewsScope.CompanyIntranet"/> rather than the port's rag —
    /// the one thing the wire needs to know about a hidden lab, and the whole of what this file lends it.
    ///
    /// <para>Two facts, and no more. <paramref name="insideLab"/> is the caller's own honest position: a lab
    /// canteen table stands inside a FORCED region, and the wire has no business re-deriving the geometry
    /// the client is already standing in. <see cref="Present"/> is then asked as a cross-check so a body
    /// that hides nothing can never print a company paper, with <paramref name="forcePresent"/> carrying
    /// the <c>?secretlab=1</c> cheat exactly as <see cref="For"/> does — otherwise the cheat's lab would
    /// exist on the ground and not on the noticeboard.</para>
    ///
    /// <para>L2 consumes this through <see cref="NewsWire.ScopeAt"/> (never directly): the seat verb builds a
    /// <see cref="NewsWire.NewsPlace"/> from the seated context it already holds and asks the wire for its
    /// masthead. Nothing else about a place is allowed to leak into Core.</para>
    /// </summary>
    public static bool ReadsCompanyIntranet(string bodyId, bool insideLab, bool forcePresent = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return insideLab && (forcePresent || Present(bodyId));
    }

    /// <summary>Whether a probe of (<paramref name="squareX"/>, <paramref name="squareY"/>) is close enough to
    /// the hidden door to shriek a PROXIMITY hint (the detector "very close") — the door's own square, or any
    /// of the eight around it. The exact-square case (a reveal) is <see cref="IsDoorSquare"/>.</summary>
    /// <summary>#585 · The ground the hidden chamber will occupy once it is forced open — centre and a
    /// rotation-proof radius, in the shape every other placer on this ground speaks.
    ///
    /// <para>The lab is APPENDED at runtime, from the door outward toward the field's centre. Nothing that
    /// lays buildings knew that, so once the grounds gained real structures a hut could be standing exactly
    /// where the chamber grows — and the lab would open into somebody else's wall. That is the identical
    /// failure the away-expedition rooms hit, reported by their guard as "a region wall crosses the base
    /// geography"; this is the same fix, applied before the owner goes looking for a lab rather than after
    /// he finds one wedged inside a ruin.</para>
    ///
    /// <para>Reserved on EVERY body, whether or not this one hides a lab: the door spot is seeded the same
    /// way regardless, so keeping that patch of deep field clear costs one building's worth of ground and
    /// removes the whole class.</para></summary>
    public static (double X, double Y, double R) ChamberFootprint(
        string bodyId, in SurfaceLayout.Field field, string? siteSalt = null)
    {
        // #585: reserved around the RESOLVED entrance (HeadSpot), not the raw seed — otherwise the ledger
        // keeps a patch of ground clear that the shed has already been nudged away from, and leaves the
        // ground it actually stands on unprotected. That is how the maintenance shed ended up inside an
        // outpost hut.
        (double hx, double hy) = HeadSpot(bodyId, siteSalt, field);
        return ChamberDiscs(bodyId, siteSalt, hx, hy)[0];   // the hut at the door
    }

    /// <summary>#1383 · Every disc the lab claims when its shed stands at (<paramref name="hx"/>,
    /// <paramref name="hy"/>): [0] the hut and the antechamber (the old <see cref="ChamberFootprint"/>), then
    /// one each for the clean room and the heart. Pure of where the shed ends up, so <see cref="HeadSpot"/> can
    /// ask whether a candidate spot would put the MOUNTAIN across something, not just the shed.</summary>
    private static List<(double X, double Y, double R)> ChamberDiscs(
        string bodyId, string? siteSalt, double hx, double hy)
    {
        // #1383 · The way the chamber grows is Build's own seed, not a guess from which half of the field the
        // shed ended up in: HeadSpot may relocate the shed across the middle, and a claim laid on the wrong
        // side of it reserves ground the chamber never touches and leaves the ground it does touch to
        // whatever the next placer likes.
        double dir = Frac(bodyId, "door-side") < 0.5 ? 1.0 : -1.0;

        // The hut standing ON the door, and nothing else of it: the claim that has always been here, #606's
        // reach. (#587 · a keep-out is a claim on ground, and an over-claim quietly costs the world its
        // variety - SeededBodies_AllDifferFromEachOther and SiteSalt_ParameterizesTheGround both went red
        // when this was one fat disc round hut AND chamber, so the rooms below are separate small discs and
        // the open ground between the shed and the mountain is NOT claimed.)
        double hut = SurfaceStructure.EnvelopeOf(HeadHutAt(bodyId, siteSalt, 0, 0)).Reach;
        var discs = new List<(double X, double Y, double R)> { (hx, hy, hut) };

        // The antechamber: RoomDepth x RoomWidth, which begins HutStandOff past the door (#1383).
        double chamber = Math.Sqrt(((RoomDepth / 2.0) * (RoomDepth / 2.0)) + ((RoomWidth / 2.0) * (RoomWidth / 2.0)));
        discs.Add((hx + (dir * (HutStandOff + (RoomDepth / 2.0))), hy, chamber));

        // …and the two deeper rooms, one disc each, the smallest that holds it.
        double farCx = hx + (dir * (HutStandOff + RoomDepth));
        for (int i = 0; i < 2; i++)
        {
            double mid = farCx + (dir * DeepChamberDepth * (i + 0.5));
            double halfWidth = RoomWidth / 2.0 * (i == 0 ? 0.8 : 0.62);
            double halfDepth = DeepChamberDepth / 2.0;
            discs.Add((mid, hy, Math.Sqrt((halfDepth * halfDepth) + (halfWidth * halfWidth))));
        }
        return discs;
    }

    /// <summary>Whether the lab, if its shed stood at (<paramref name="hx"/>, <paramref name="hy"/>), would
    /// have any of its rooms across one of <paramref name="standing"/> (raw keep-out discs). Used by
    /// <see cref="HeadSpot"/> so the shed is moved for the mountain behind it and not only for itself.</summary>
    private static bool MountainClashes(
        string bodyId, string? siteSalt, double hx, double hy, List<(double X, double Y, double R)> standing)
    {
        foreach ((double dx0, double dy0, double dr) in ChamberDiscs(bodyId, siteSalt, hx, hy))
        {
            foreach ((double sx, double sy, double sr) in standing)
            {
                double dx = dx0 - sx, dy = dy0 - sy;
                if (Math.Sqrt((dx * dx) + (dy * dy)) < dr + sr)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>#1383 · The ground the MOUNTAIN occupies (the antechamber, the clean room and the heart), as
    /// discs along the spine that starts at <see cref="ChamberFootprint"/>'s hut. The claim used to stop at the antechamber, so a
    /// seeded hut could be standing across the clean room and the control panels, the alarm and the card sat
    /// behind a stranger's wall. One disc per chamber, so the claim follows the mountain and does not swallow
    /// the field.</summary>
    public static IReadOnlyList<(double X, double Y, double R)> DeepChamberFootprints(
        string bodyId, in SurfaceLayout.Field field, string? siteSalt = null)
    {
        (double hx, double hy) = HeadSpot(bodyId, siteSalt, field);
        List<(double X, double Y, double R)> all = ChamberDiscs(bodyId, siteSalt, hx, hy);
        return all.GetRange(1, all.Count - 1);
    }
}
