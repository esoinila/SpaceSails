using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #251 · THE WELD'S FURNISHING STEPS — the bar's own fixtures, the concourse, the tables, the backdrops,
/// the wings, the free tops and the gallery: the seven steps of <see cref="BuildComplex"/> that dress the
/// structure the first six laid.
///
/// <para>Split out of <c>HavenInterior.Build.Steps.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered, and no field.</para>
/// </summary>
public static partial class HavenInterior
{
    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static void SetTheBarsOwnFixtures(StationSpec spec, List<DeckPlan.ConsoleSpot> consoles, float serviceX, float serviceY, string keepLabel)
    {
        consoles.AddRange(new DeckPlan.ConsoleSpot[]
        {
            // The Magpie's bar stop — a roaming patron (PR-F). They aren't always here; walk up and the
            // game reads their rota, so an empty chair means they've drifted off (bar → gone → back room).
            new(DeckPlan.ConsoleKind.BarPatron, (float)MagpieBarPost.X, (float)MagpieBarPost.Y, "◈ THE MAGPIE"),
            // #247 — the barkeep service console, ON the desk drawn in this bar's art (owner 2026-07-18,
            // "Evening wind": "the bar-keep service position … needs to be AT that desk … the bar to be on
            // top of the bar in the picture"). It sits at the desk's service point (S) — down the LEFT,
            // mid-depth — on the players' (hall-door) side of the counter wall, so the captain bellies up
            // from below and the [E] radius leans in for the house special. Kept > InteractRadius from
            // One-Eye Silas's stool (−9, HallTopY+6) so E never grabs the wrong regular.
            new(DeckPlan.ConsoleKind.Barkeep, serviceX, serviceY, keepLabel),
            // The gift shop: walk up, press E, view the Gen-AI souvenir + its location gag. Kept clear
            // of the bar patrons (Coil at x14) so E doesn't grab the wrong console.
            new(DeckPlan.ConsoleKind.ViewObject, 6, HallTopY + 3, "👕 SOUVENIR TEE", spec.TshirtArt, spec.Gag),
            new(DeckPlan.ConsoleKind.ViewObject, 9.5f, HallTopY + 3, "🧲 FRIDGE MAGNET", spec.MagnetArt,
                $"A little {spec.Name} to stick on the fridge back home."),
            // The second PIRATE INSURANCE poster, in the BAR wing (#380 item 1 — the pair banked for this
            // lane). Where a spacer nurses a drink and does the grim arithmetic, Nebula Mutual pitches the
            // hard sell: "DIED BROKE? WALK IT OFF." On the starboard wall, clear of Coil's stool (x14, +6)
            // and the back-room hatch. [E] pops the poster + the sales-voice caption. Grok-generated art.
            new(DeckPlan.ConsoleKind.ViewObject, BarRight - 2.5f, HallTopY + 14, "📋 PIRATE INSURANCE",
                "art/poster-pirate-insurance-2.jpg",
                "“DIED BROKE? WALK IT OFF.” Nebula Mutual covers the clinic bill so the void doesn't keep "
                + "you — one premium, and a shot nerve or a Reever's hand is just a bad night, not the last "
                + "one. The hoards you buried outlive the hull; the policy outlives the captain. Underwritten "
                + "by Nebula Mutual — “We Bring You Back Meaner.”"),
        });
        // 📸 THE SELFIE SPOT (issue #400, owner's cruise 2026-07-20: "the awesome-view places … should
        // have a photo spot … the frame should place the CAPTAIN in the awesome view"). The scenic outer
        // havens (Red Eye storm gallery, Ringside's ring-lip, Selene's Earthrise, The Deep's edge) each get
        // a console at the bar's spinward window — walk up, press E, and the captain poses into the vista
        // with a boastful house-voice caption, filed into the legend ledger. Reuses the ViewObject/plaque
        // console idiom (#392); a dedicated kind routes E to the capture instead of the passive viewer.
        // Placed at the starboard end of the big window — clear of the barkeep desk (down the left), the
        // gift-shop consoles (x 6/9.5, +3), the STOREROOM hatch (BarRight−2, +11), and the second insurance
        // poster (BarRight−2.5, +14) — so [E] never grabs the wrong console whichever chair the rota fills.
        if (SpaceSails.Core.SelfieSpots.For(spec.BodyId) is { } selfieSpot)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.SelfieSpot, BarRight - 5, BarTopY - 2,
                selfieSpot.ConsoleLabel, selfieSpot.VistaArt));
        }
    }

    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static void HangTheConcourse(StationSpec spec, ArrivalTube.Tier? tier, List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels)
    {
        // The station's DEDICATION PLAQUE (owner's cruise ruling, 2026-07-19, photographing their ship's
        // Aker Finnyards builder's plate: "We could gen-AI the ships and docks some space-dock plaques …
        // add some depth to the world (worldbuilding)"). One addition here seeds every port — walk off
        // the tube, and it stands in the concourse on your port side, clear of the tube path (x 1..4), the
        // immigration desk, and every ring hatch. [E] pops the plate + its dedication in the house voice
        // (Core Plaques). Selene / Red Eye / Deep carry Grok plate art; the rest fall back to the text
        // alone until their easel is painted (the souvenir onerror-hide fallback idiom).
        if (Plaques.For(spec.BodyId) is { } plaque)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.ViewObject, HallCenterX - 6, HallCenterY - 5,
                plaque.ConsoleLabel, plaque.ArtUrl, plaque.Lore));
        }

        // The LIFEBOAT STATION (owner worldbuilding addendum, 2026-07-19: "Safety equipment is also cool.
        // Lifeboats at station maybe."). A battered muster point across the concourse from the plaque, on
        // the starboard side — clear of the tube path (x 1..4), the immigration desk, the plaque, and every
        // ring hatch. A wall label marks the muster; [E] pops the muster card (per-port stale inspection
        // date, and an asterisk that does the work). Text-only for now — the art easel is a follow-up.
        labels.Add((HallCenterX + 9, HallCenterY - 6.5f, Plaques.LifeboatLabel));
        consoles.Add(new(DeckPlan.ConsoleKind.ViewObject, HallCenterX + 9, HallCenterY - 5,
            Plaques.LifeboatLabel, null, Plaques.LifeboatMuster(spec.BodyId)));

        // ── #380 item 10 · THE CUSTOMS DESK SAYS WHAT THE GATE IS FOR ───────────────────────────────────
        //
        // The audit's last open complaint, and it was about a PROMISE: a counter, a gate, a signed authority
        // and an officer standing at it set an expectation of being CHECKED, and the captain walked through
        // carrying whatever he liked, every time, for ever. The rule was already written down (the arrival
        // plate's ArrivalTube.WalkLine) and the sweep was already built (#537/#538) — aboard somebody else's
        // hull, never at a port's own gate. What was missing was the officer's own sentence.
        //
        // It is a ViewObject card in the plaque/lifeboat idiom, on the OFFICER'S OWN SQUARE — CustomsDesk,
        // the same constant FillComplexDroids stands him on, so the man and the card can never drift a du
        // apart. The words come from Core, per tier, out of the same switch WalkLine lives in: the desk and
        // the plate he read ninety seconds ago are one reading of one berth. No line at an outpost means no
        // console at an outpost — there is no queue and no officer there to have an opinion.
        //
        // Clearance: over an interact radius from the plaque (−3.5, 35), the lifeboat (11.5, 35), the poster,
        // the three ad plates and every ring hatch — the same rule the whole concourse is placed by, and
        // asserted at each fixture's own square in TheWallsAreHungAndReadTests.
        if (tier is { } berth && ArrivalTube.CustomsLine(berth) is { } stamped)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.ViewObject, CustomsDesk.X, CustomsDesk.Y,
                ArrivalTube.CustomsLabel, null, stamped));
        }

        // ── #1151 · THE CLAIMS KIOSK ───────────────────────────────────────────────────────────────────
        //
        // Owner ruling, 2026-09-06 on #525: insurance does not know automatically unless we die — everything
        // else is a claim, and claims are lodged at "automatic kiosks scattered through the system".
        //
        // Scattered is the word, and it is why this is not at every port. Every great port has one because
        // that is where the traffic is; a dealt share of the working berths have one because the company
        // decided once whether that wall was worth it and has not revisited it since; an outpost has none for
        // the customs desk's own reason — there is no concourse there to stand a machine in. The rule is
        // NebulaClaims.AKioskStands, asked here and by every guard, so the concourse and the tests read one
        // answer rather than two that agree today.
        //
        // What it raises is not a wall plate. The machine takes three things off the captain, in order, so
        // this places the fixture wearing its plate and the page owns the press (Map.Claims.Kiosk.cs, routed
        // by the label exactly as the head office's two consoles are).
        //
        // Clearance: port side and mid-hall, over an interact radius from the plaque (−3.5, 35), the poster
        // (−8.5, 46), the northern ad plates and the tube path (x 1..4), and well inside the 12-gon's
        // apothem — asserted at the square itself in the guards, never trusted from this comment.
        if (tier is { } claimBerth && NebulaClaims.AKioskStands(claimBerth, spec.BodyId))
        {
            consoles.Add(new(DeckPlan.ConsoleKind.ViewObject, HallCenterX - 11, HallCenterY - 1,
                NebulaClaims.KioskPlate, null, NebulaClaims.OnApproach));
        }

        // PIRATE INSURANCE — the Gen-AI dock poster (#380 item 1: pre-seed the brain-backup / Pirate
        // Insurance premise with port advertising, so a new player meets the fiction BEFORE the death card,
        // not on it; owner 2026-07-19: "we should explain Pirate insurance … advertisements about it as Gen
        // AI at every dockable port"). One addition here seeds all eight ports (the shared hall build, the
        // ViewObject console idiom the plaque/souvenirs use). Port-side of the concourse, above the plaque,
        // clear of the tube path (x 1..4), the immigration desk, the plaque, the lifeboat, and every ring
        // hatch. [E] pops the poster ("OUR RATES ARE A STEAL") + the sales-voice caption. Art is Grok-made.
        consoles.Add(new(DeckPlan.ConsoleKind.ViewObject, HallCenterX - 11, HallCenterY + 6,
            "📋 PIRATE INSURANCE", "art/poster-pirate-insurance-1.jpg",
            "“OUR RATES ARE A STEAL.” Pirate Insurance from Nebula Mutual: brain-backup rebirth, a rustbucket "
            + "gassed and waiting, no awkward questions at the clinic. Die uninsured and you still wake — just "
            + "meaner and broker. Ask your dockmaster before the collectors ask about you. Underwritten by "
            + "Nebula Mutual — “We Bring You Back Meaner.”"));

        // #973 L4 · THE THREE SMALL PLATES, hung round the same concourse the poster hangs in. A text plate
        // in the poster's own idiom — no canvas, exactly as the lifeboat muster above carries none: three
        // more paintings for three one-line ads would be a pool of art bought to say very little.
        //
        // The captain reads the WHOLE of each one walking past (the label IS the advertising), and [E] gives
        // it back on a card so the words can be read twice — which matters, because the third one read is
        // the one that finishes a memory (`StationAds`). Detected by the ad's own text, so this file never
        // learns what any of them is FOR.
        //
        // NO CAPTION, and that came out of looking at the card in a browser: a caption repeating the title
        // word for word read as a stutter — the surface saying one thing twice and meaning it once. The
        // plate is one sentence; the card is that sentence held closer, and there is nothing under it.
        //
        // Placed on the northern half of the concourse, where nothing else stands: the poster and the plaque
        // are port-side and low, the lifeboat is starboard and low, the tube path is x 1..4 and southern.
        // Every one is at least 5 du from every other console on this deck, so [E] can never grab the wrong
        // fixture — the same clearance rule the second poster and the selfie spot are placed by.
        (float X, float Y)[] adSites =
        [
            (HallCenterX + 8.5f, HallCenterY + 4),
            (HallCenterX + 3, HallCenterY + 9),
            (HallCenterX - 4, HallCenterY + 8),
        ];
        for (int adIdx = 0; adIdx < adSites.Length && adIdx < SpaceSails.Core.StationAds.Ads.Count; adIdx++)
        {
            SpaceSails.Core.StationAds.Ad ad = SpaceSails.Core.StationAds.Ads[adIdx];
            consoles.Add(new(DeckPlan.ConsoleKind.ViewObject, adSites[adIdx].X, adSites[adIdx].Y, ad.Label));
        }
    }

    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static List<DeckPlan.TableTop> SetTheTables(StationSpec spec, DeckPlan ship)
    {
        // Seven tables spread across the big room — the rota seats present regulars at some of them this
        // watch, the rest stand open (an empty chair = someone's drifted off) — plus the ship's cantina.
        var tables = new List<DeckPlan.TableTop>(ship.Tables);
        foreach ((float X, float Y) top in BarTops)
        {
            tables.Add(new(top.X, top.Y));
        }

        // #1199 (2026-09-18) · …AND THE GALLERY'S TWO STEEL TABLES. Drawn tops like every other top on this
        // deck, and appended to the DRAWN list only — never to BarFloor.Tops, which is what the room's own
        // walkers cross the floor to. A regular who wandered out to the end of the observation walk for a
        // sit-down would be the one thing this whole feature cannot survive: the gallery is a room nobody is
        // in, and that is the entire content of the beat it carries.
        foreach (DeckReachability.Point top in GalleryTops(spec.BodyId))
        {
            tables.Add(new((float)top.X, (float)top.Y));
        }

        return tables;
    }

    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static List<DeckPlan.Backdrop> HangTheBackdrops(StationSpec spec, DeckPlan ship)
    {
        var backdrops = new List<DeckPlan.Backdrop>(ship.Backdrops)
        {
            // Concourse art across the round hall — sized ~16:9 to match the image so the domed ceiling
            // isn't stretched; fills the hall's width, floor showing at the very top/bottom.
            new(spec.HallArt, HallCenterX - 16, HallCenterY + 9, 32, 18, 0.95f),
            new(spec.BarArt, BarLeft, BarTopY, BarRight - BarLeft, BarTopY - HallTopY, 0.95f),
        };

        // #1199 · THE GLASS FLOOR. The drop, laid UNDER the walk in the same backdrop grammar the hall and
        // the bar already get their art through — a canvas over a rectangle, at an alpha. There is no
        // per-room floor fill token in this pen and this room does not need one invented: what a transparent
        // floor looks like from above is the thing that is under it, and that is a picture.
        //
        // Held back from the bar's 0.95 so the deck's own floor still reads through it. The captain is
        // walking ON something; he can simply see past it, which is the entire feature of the room.
        if (HasObservationWalk(spec.BodyId))
        {
            backdrops.Add(new(
                ObservationWalk.ArtUrl,
                TheWalk.BlindX, TheWalk.NorthJambY,
                TheWalk.MouthX - TheWalk.BlindX, TheWalk.NorthJambY - TheWalk.SouthJambY,
                0.55f));

            // #1199 (2026-09-18) · …AND THE GALLERY GETS ITS OWN CANVAS, which is the room itself.
            //
            // The tube's plate is a drop seen down a 24 × 3.5 slot, and it is already stretched nearly seven
            // to one to fill that; the crossbar is a different shape again and would have been the same
            // picture at a visibly different stretch two du apart. So the gallery takes a plate painted FOR
            // it, at the same 0.55 the tube's drop is held back to, so the deck's own floor still reads
            // through it and the captain is plainly walking ON something.
            //
            // #1199 (2026-09-18, PLAYED) · AND IT IS THE PORTRAIT ONE. Owner, on the merged room: "the hat
            // is drawn TALL … and the 16:9 cafeteria plate is stretched ~3:1 into it — the vending machines
            // read as tall slivers." The tube runs due west, so this rectangle is 8 ACROSS by 24 ALONG —
            // 0.333 — and a 16:9 canvas laid in it is squeezed to under a fifth of its width. The landscape
            // plate stays where landscape belongs (the vending card and the seated panel); the FLOOR wears
            // the 9:16 one, which is the room's own shape and needs no rotation to sit right — the pen has
            // no angle to give it (DrawImage takes x, y, w, h and an alpha), and a 9:16 canvas is already
            // long in the axis this room is long in. TheRoomAndItsPlateAreTheSameShapeTests measures both.
            backdrops.Add(new(
                GalleryFixtures.CafeteriaFloorArtUrl,
                TheGallery.WestX, TheGallery.NorthY,
                TheGallery.EastX - TheGallery.WestX, TheGallery.NorthY - TheGallery.SouthY,
                0.55f));
        }

        return backdrops;
    }

    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static void WeldTheWings(IReadOnlyList<DeckWing> activeWings, List<DeckPlan.Wall> walls, List<DeckPlan.Door> doors, List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels)
    {
        // Weld on each active wing's geometry (Wednesday plan §3 PR-F): walls, any doors, consoles
        // (translated to deck console kinds), and floor labels. The doorway into each was already
        // carved above; here the room itself grows.
        foreach (DeckWing wing in activeWings)
        {
            foreach (WingWall w in wing.Walls)
            {
                walls.Add(new(w.X1, w.Y1, w.X2, w.Y2, w.IsWindow, w.IsHull));
            }
            foreach (WingDoor d in wing.Doors)
            {
                doors.Add(new(d.X1, d.Y1, d.X2, d.Y2, d.Locked));
            }
            foreach (WingConsole c in wing.Consoles)
            {
                consoles.Add(new(MapConsoleKind(c.Kind), c.X, c.Y, c.Label, c.ImageUrl, c.Caption));
            }
            foreach (WingLabel l in wing.Labels)
            {
                labels.Add((l.X, l.Y, l.Text));
            }
        }
    }

    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static void OfferTheFreeTops(List<DeckPlan.ConsoleSpot> consoles)
    {
        // ── #973 L5b · A TOP THE CAPTAIN CAN TAKE ───────────────────────────────────────────────────────
        //
        // #973 L0 found the gap and wrote it down: every one of the seven ways to open a sitting in this game
        // was gated on a SurfaceExcursion, a berth has none, and so "the bar's seven tops are drawn dressing
        // with no chairs and no console" — [E] at one answered nothing, which is an absence rather than a
        // refusal and is the one kind of no a player cannot read (#757's own lesson, in the other room).
        //
        // A console goes on every top the room has not already given to somebody: the regulars the rota
        // seated this watch, the Magpie at their stop, the oracle in her corner. Asked of the console list
        // ITSELF, after everything else is in it, so the answer cannot drift from the room — a second table
        // of who is sitting where would be this file's oldest bug class with a stranger in the captain's
        // chair. Within an interact radius of an existing console is "somebody's", because that is exactly
        // the distance at which [E] would grab the wrong one.
        foreach ((float X, float Y) top in BarTops)
        {
            bool somebodysAlready = false;
            foreach (DeckPlan.ConsoleSpot spot in consoles)
            {
                double dx = spot.X - top.X;
                double dy = spot.Y - top.Y;
                if ((dx * dx) + (dy * dy) <= DeckPlan.InteractRadius * DeckPlan.InteractRadius)
                {
                    somebodysAlready = true;
                    break;
                }
            }

            if (!somebodysAlready)
            {
                consoles.Add(new(DeckPlan.ConsoleKind.BarTop, top.X, top.Y, BarTopLabel));
            }
        }
    }

    /// <summary>#251 · Extracted from <see cref="BuildComplex"/>, statements verbatim and in their original
    /// order — one step of the weld.</summary>
    private static List<DeckPlan.FurnitureSpot> FitTheGallery(StationSpec spec, DeckPlan ship, List<DeckPlan.ConsoleSpot> consoles)
    {
        // ── #1199 (2026-09-18) · THE GALLERY'S OWN FIXTURES ─────────────────────────────────────────────
        //
        // Owner, live: "a small vending machine cafeteria with a couple of tables there… the station likes to
        // get the tourist money", and "maybe one of those pay-coin-to-use binoculars … use with E … same for
        // the vending machine."
        //
        // Three presses and two blocks, and every coordinate comes off the room's own published geometry
        // (HavenInterior.cs) rather than out of this loop — so the guards that hold a fixture to being inside
        // the gallery, out of the rail band or clear of its neighbours are reading the same numbers the deck
        // was built from. A top here carries the SAME label the bar's free tops wear, because it is the same
        // verb and the same sitting; what it is not is the bar (see GalleryTops).
        var furniture = new List<DeckPlan.FurnitureSpot>(ship.Furniture);
        if (HasObservationWalk(spec.BodyId))
        {
            if (TheBinocularsAt(spec.BodyId) is { } glasses)
            {
                consoles.Add(new(DeckPlan.ConsoleKind.CoinBinoculars,
                    (float)glasses.X, (float)glasses.Y, GalleryFixtures.BinocularsPlate));
            }

            foreach (DeckReachability.Point vendor in TheVendorsAt(spec.BodyId))
            {
                consoles.Add(new(DeckPlan.ConsoleKind.CoinVendor,
                    (float)vendor.X, (float)vendor.Y, GalleryFixtures.VendorPlate));
            }

            foreach ((double X0, double Y0, double X1, double Y1) block in
                     TheVendingMachineBlocks(spec.BodyId))
            {
                furniture.Add(new((float)block.X0, (float)block.Y0, (float)block.X1, (float)block.Y1, 0));
            }

            foreach (DeckReachability.Point top in GalleryTops(spec.BodyId))
            {
                consoles.Add(new(DeckPlan.ConsoleKind.BarTop, (float)top.X, (float)top.Y, BarTopLabel));
            }
        }

        return furniture;
    }
}
