using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>Part of <see cref="HavenInterior"/> (the header note lives in HavenInterior.cs) — THE WELD.
/// Every station shares this one geometry: the ship, the umbilical, the twelve-sided immigration hall
/// with ten other berths' hatches sealed, the wide door, and the bar — assembled into a single
/// <see cref="DeckPlan"/> in one coordinate space, with whatever wings are active welded on at runtime.
/// <c>FillComplexDroids</c> closes it with the eleven figures the docked complex draws before any walker,
/// each placed off the same constants and the same rota the rest of this class answers with. Two methods
/// and no fields at all, which is why this is the half that could be moved.</summary>
public static partial class HavenInterior
{
    private static DeckPlan BuildComplex(StationSpec spec, IReadOnlyList<DeckWing> activeWings, double simTime,
        bool forceOracle = false, System.Action<DeckPlan.Droid[], int>? fillWalkers = null,
        RoomChurn? churn = null, ArrivalTube.Tier? tier = null)
    {
        DeckPlan ship = DeckPlan.Ship;
        bool backRoomOpen = activeWings.Count > 0; // the Magpie's back-room stop is reachable once a wing is welded on

        // Hatch ids whose edge has grown a wing — carve a doorway there instead of a sealed wall.
        var openHatchIds = new HashSet<string>(activeWings.Select(w => w.UnlockHatchId));

        // The bare ship seals its airlock hatch (x 1..4); the complex opens it and mates the tube.
        var hatch = new DeckPlan.Wall(1, ShipHatchY, 4, ShipHatchY, false, true);

        var walls = new List<DeckPlan.Wall>(ship.Walls.Where(w => !w.Equals(hatch)));
        // Seed from the ship's own doors so the shuttle-bay airlock (#163) travels with the ship into
        // every docked complex — that is the captain's ride home, so the return hop is never stranded.
        var doors = new List<DeckPlan.Door>(ship.Doors);
        var labels = new List<(float X, float Y, string Text)>(ship.RoomLabels);

        // Tube: the umbilical from the ship's hatch up to the hall's south edge.
        walls.Add(new(TubeLeft, ShipHatchY, TubeLeft, HallBottomY, false, true));
        walls.Add(new(TubeRight, ShipHatchY, TubeRight, HallBottomY, false, true));
        doors.Add(new(TubeLeft, ShipHatchY + 1, TubeRight, ShipHatchY + 1)); // ship-end auto door
        doors.Add(new(TubeLeft, HallBottomY - 1, TubeRight, HallBottomY - 1)); // hall-end auto door

        // The round hall ring. Vertices at (15 + 30k)°, so edges are centred on the compass points;
        // edge 8 faces south (our tube) and edge 2 faces north (the bar). Every other edge is a
        // sealed berth — a real wall with a cold "locked" hatch drawn on it and a BERTH sign inside.
        var v = new (float X, float Y)[HallSides];
        for (int k = 0; k < HallSides; k++)
        {
            v[k] = HallVertex(k);
        }

        // The ring's sealed edges: a few other captains' berths and the station's own departments,
        // nearly all locked to us — so the concourse reads as one hub of a much bigger complex. Each
        // is a numbered Hatch console: walk up and it names itself + shows locked; press E to knock.
        // A cracked hatch that grows a wing is drawn open (📂) and its edge is a real doorway.
        string[] ringTags =
        [
            "⚓ BERTH", "🔒 CUSTOMS", "🔒 HABITAT RING", "⚓ BERTH", "🔒 MEDBAY",
            "🔒 BONDED STORES", "⚓ BERTH", "🔒 DOCKMASTER", "🔒 TRANSIT", "🔒 SECURITY",
        ];
        var hatches = new List<DeckPlan.ConsoleSpot>();
        int sealedIdx = 0;
        for (int k = 0; k < HallSides; k++)
        {
            (float X, float Y) a = v[k], b = v[(k + 1) % HallSides];
            if (k == 8) // south edge: our tube mouth (gap x 1..4)
            {
                walls.Add(new(a.X, a.Y, TubeLeft, a.Y, false, true));
                walls.Add(new(TubeRight, b.Y, b.X, b.Y, false, true));
            }
            else if (k == 2) // north edge: the wide door to the bar (gap x BarDoorLeft..BarDoorRight)
            {
                walls.Add(new(a.X, a.Y, BarDoorRight, a.Y, false, true));
                walls.Add(new(BarDoorLeft, b.Y, b.X, b.Y, false, true));
                doors.Add(new(BarDoorLeft, a.Y, BarDoorRight, a.Y)); // wide auto door
            }
            else // a sealed berth / department — or an opened expansion joint
            {
                string tag = ringTags[sealedIdx % ringTags.Length];
                string id = $"{spec.Authority[0]}-{k:D2}"; // e.g. M-05: findable, distinct per station
                sealedIdx++;
                float px = HallCenterX + ((a.X + b.X) / 2 - HallCenterX) * 0.9f;
                float py = HallCenterY + ((a.Y + b.Y) / 2 - HallCenterY) * 0.9f;
                if (openHatchIds.Contains(id))
                {
                    // Cracked: carve a walkable doorway (two stubs + an unlocked auto-door), and draw
                    // the panel open (📂). The wing's own walls, added below, close the room beyond.
                    (WingWall stubA, WingWall stubB, WingDoor door) =
                        DeckExpansions.CarveDoorway(a.X, a.Y, b.X, b.Y, 0.30f, 0.70f);
                    walls.Add(new(stubA.X1, stubA.Y1, stubA.X2, stubA.Y2, false, true));
                    walls.Add(new(stubB.X1, stubB.Y1, stubB.X2, stubB.Y2, false, true));
                    doors.Add(new(door.X1, door.Y1, door.X2, door.Y2)); // unlocked — you walk through
                    string dept = string.Join(' ', tag.Split(' ').Where(t => t.All(char.IsLetter)));
                    hatches.Add(new(DeckPlan.ConsoleKind.Hatch, px, py, $"📂 {dept} · {id}"));
                }
                else
                {
                    // Sealed: a real wall with a cold locked hatch drawn on it and a knockable panel.
                    walls.Add(new(a.X, a.Y, b.X, b.Y, false, true));
                    doors.Add(new(Lerp(a.X, b.X, 0.25f), Lerp(a.Y, b.Y, 0.25f),
                                  Lerp(a.X, b.X, 0.75f), Lerp(a.Y, b.Y, 0.75f), Locked: true));
                    hatches.Add(new(DeckPlan.ConsoleKind.Hatch, px, py, $"{tag} · {id}"));
                }
            }
        }

        // Immigration desk (Total Recall): two counters with a central GATE aligned to the tube, so
        // you walk straight off the umbilical through the checkpoint. Officer to one side.
        float deskY = HallBottomY + 6;
        walls.Add(new(-7, deskY, 1, deskY, false, false)); // counter, port of the gate
        walls.Add(new(4, deskY, 9, deskY, false, false));  // counter, starboard of the gate (gate gap x 1..4)
        labels.Add((HallCenterX, HallBottomY + 7.5f, $"{spec.Authority} IMMIGRATION"));
        labels.Add((HallCenterX, HallBottomY + 2.5f, spec.Quip));
        // #380 item 10 — the officer at that gate is not mute any more. He stands at CustomsDesk and the
        // console that gives him his sentence goes on the same square, below, where the concourse's other
        // fixtures are hung. See ArrivalTube.CustomsLine for what he says and why it is per-tier.
        //
        // A big lobby welcome poster so you know at a glance which port you're standing in.
        labels.Add((HallCenterX, HallCenterY + 8, $"★  WELCOME TO {spec.Name}  ★"));
        labels.Add((HallCenterX, HallCenterY + 3, $"⚓ {spec.Authority} ORBIT"));

        // The bar, off the hall's north door.
        walls.Add(new(BarLeft, HallTopY, BarDoorLeft, HallTopY, false, true));   // bar floor wall, port of the door
        walls.Add(new(BarDoorRight, HallTopY, BarRight, HallTopY, false, true)); // bar floor wall, starboard of the door
        walls.Add(new(BarLeft, HallTopY, BarLeft, BarTopY, false, true));
        walls.Add(new(BarRight, HallTopY, BarRight, BarTopY, false, true));
        walls.Add(new(BarLeft, BarTopY, BarRight, BarTopY, true, true)); // spinward window onto space
        labels.Add((HallCenterX, BarTopY - 6.5f, spec.BarName));
        labels.Add((8f, HallTopY + 1.5f, "🎁 GIFT SHOP")); // every place has one (owner)

        // #247 — the bar counter, and the BARKEEP behind it. Owner ashore at the Rusty Roadstead: "How
        // do I get a drink at the Rusty bar here? Did we forget to add the bar-keep :-D". The counter is
        // a real wall (you belly up, you don't walk through it); the barkeep console sits on the players'
        // side of it, so E leans in for the house special. The keep's name + drink come from Core.
        //
        // 2026-07-18 ("Evening wind" plan) — the per-image correction. The first pass shared ONE counter
        // for all four bars and pinned it three du off the far wall (BarTopY − 3), which dropped the keep
        // and the pacing droid up in the window/ceiling band of every backdrop. The owner ruled per-image:
        // "the bar-keep service position … needs to be AT that desk … not the middle of the empty floor …
        // Not on top of a window — and the bar to be on top of the bar in the picture." So each bar now
        // reads its desk off its OWN art (Core BarDesks), and the counter is placed there — down the LEFT,
        // mid-depth, where every backdrop actually draws it. The service point (S) is the [E] spot on the
        // players' side; the counter wall sits just BEHIND it (toward the window) and the droid paces
        // behind that (see FillComplexDroids). A safe fallback keeps any unlisted bar sane.
        BarDesk desk = BarDesks.For(spec.BodyId) ?? DefaultBarDesk(spec.BodyId);
        float serviceX = desk.ServiceX;
        float serviceY = HallTopY + desk.ServiceYOffset;   // mid-depth on the desk, clear of the window
        float counterY = serviceY + 1f;                     // the counter wall, one du behind the service line
        walls.Add(new(serviceX - desk.CounterHalfWidth, counterY, serviceX + desk.CounterHalfWidth, counterY, false, false)); // waist-high bar counter, on the pictured desk
        Barkeep? keep = Barkeeps.For(spec.BodyId);
        string keepLabel = keep is { } bk ? $"🍺 BARKEEP · {bk.Name}" : "🍺 BARKEEP";

        // Two locked back-room hatches off the bar — more of the place you can't get into (yet), and since
        // #973 L0 the two leaves people come OUT of. The records are BarBackRoomLeaves' and not typed again
        // here: a walker's plate and the plate the captain is refused at are one string, by construction.
        UndergroundComplex.LockedDoor[] backRooms = BarBackRoomLeaves(spec.Authority[0]);
        foreach (UndergroundComplex.LockedDoor leaf in backRooms)
        {
            doors.Add(new((float)leaf.X1, (float)leaf.Y1, (float)leaf.X2, (float)leaf.Y2, Locked: true));
            // The knockable panel sits two du INTO the room from its leaf, on whichever side wall it hangs on.
            float inward = leaf.X1 <= BarLeft ? 2f : -2f;
            hatches.Add(new(DeckPlan.ConsoleKind.Hatch,
                (float)leaf.X1 + inward, (float)((leaf.Y1 + leaf.Y2) / 2), leaf.Sign));
        }

        // The bar's regulars (issue #410): no longer four names nailed to four fixed chairs in every bar.
        // The rota (ResolveRegulars → PatronRota) decides, for THIS station and THIS docking watch, which
        // of the four are drinking here and which chair each took — so a present regular gets a BarPatron
        // console at their seeded seat, and an absent one leaves an empty chair (no console: E finds
        // nothing, they've drifted off — opportunity/dread, not a bug). Contacts stay keyed by the ◈ label
        // id, never by seat, so the drink/rumor/pick systems work whichever chair fills. Drop the ship's ⚓.
        // …and #731's churn over the top of it: a regular who stood up and walked out of the cellar door has
        // no console at his chair any more, and one who came out of it and sat down has one at his. Asked
        // once, here, so the consoles, the droids and the barkeep's line cannot come to three views.
        IReadOnlyList<SeatedRegular> regulars = ResolveRegulars(spec.BodyId, simTime, churn);
        var consoles = new List<DeckPlan.ConsoleSpot>(ship.Consoles.Where(c => c.Kind != DeckPlan.ConsoleKind.Airlock));
        foreach (SeatedRegular r in regulars)
        {
            if (r.Present)
            {
                consoles.Add(new(DeckPlan.ConsoleKind.BarPatron, (float)r.X, (float)r.Y, r.Label));
            }
        }

        // The station oracle (issue #425), if she's tuned to this bar this watch. A BarPatron console in
        // the port-back corner; the client's E-router matches her by name (OracleRant.Nickname) and hands
        // off to the oracle flow, never the generic quest-giver path. Absent watches leave the stool empty.
        bool oracleHere = OraclePresent(spec.BodyId, simTime, forceOracle);
        if (oracleHere)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.BarPatron, OracleCorner.X, OracleCorner.Y,
                SpaceSails.Core.OracleRant.ConsoleLabel));
        }
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

        consoles.AddRange(hatches); // the ring departments + bar back-rooms, as knockable locked hatches

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

        // Seven tables spread across the big room — the rota seats present regulars at some of them this
        // watch, the rest stand open (an empty chair = someone's drifted off) — plus the ship's cantina.
        var tables = new List<DeckPlan.TableTop>(ship.Tables);
        foreach ((float X, float Y) top in BarTops)
        {
            tables.Add(new(top.X, top.Y));
        }

        var backdrops = new List<DeckPlan.Backdrop>(ship.Backdrops)
        {
            // Concourse art across the round hall — sized ~16:9 to match the image so the domed ceiling
            // isn't stretched; fills the hall's width, floor showing at the very top/bottom.
            new(spec.HallArt, HallCenterX - 16, HallCenterY + 9, 32, 18, 0.95f),
            new(spec.BarArt, BarLeft, BarTopY, BarRight - BarLeft, BarTopY - HallTopY, 0.95f),
        };

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

        return new DeckPlan(walls.ToArray(), consoles.ToArray(), labels.ToArray(), backdrops.ToArray(),
            spawnX: 2.5, spawnY: 6, // aboard, in the airlock corridor, facing up the tube
            // #973 L0 · …and the WALKER BAND after the room's own seated figures, when somebody is walking this
            // deck. The offset is stated once (SeatedFigureCount) and the width once (Egress.BandSlots); the
            // two times this game threw IndexOutOfRangeException at the renderer, it was because a band's
            // width and a buffer's length were two opinions about one number.
            droidCount: SeatedFigureCount + (fillWalkers is null ? 0 : Egress.BandSlots),
            fillDroids: (simTime, buffer) =>
            {
                FillComplexDroids(simTime, buffer, backRoomOpen, serviceX, serviceY, regulars, oracleHere);
                fillWalkers?.Invoke(buffer, SeatedFigureCount);
            },
            location: (x, y) => x < -14.5 && y is > 15 and < 37 ? "BONDED STORES · BACK ROOM"
                              : y > HallTopY ? spec.BarName
                              : y > HallBottomY ? $"{spec.Authority} IMMIGRATION"
                              : y > ShipHatchY ? "GANGWAY"
                              : DeckPlan.Ship.Location(x, y),
            doors: doors.ToArray(), shipFixtures: true, followCam: true, tables: tables.ToArray(),
            // #1040 · …AND THE SHIP'S OWN COUNTER TRAVELS WITH HER. A docked complex is her plan with a
            // station welded onto it, and her walls, doors, consoles, labels, backdrops and tops are all
            // seeded from it above. Her stool row and her counter's fill were the two she would have arrived
            // without — so the moment she clamped on, the seats [E] still answers at would have stopped
            // being drawn: the walked room and the drawn room disagreeing, which is this repository's third
            // named bug class with a bar stool under it.
            stools: ship.Stools, furniture: ship.Furniture);
    }

    // Ship's three droids, the immigration officer, the four seated bar regulars (issue #410, roved by the
    // rota — each at their seeded seat this watch, or parked off-frame when they've drifted off), and —
    // index 8 — the roaming Magpie, placed by their sim-time rota. Shared across every station (one
    // geometry); deterministic in sim time, stateless. The <paramref name="regulars"/> seating is captured
    // at build time (fixed for the visit), so the droids sit exactly where their consoles do; only the
    // thermal jitter and the Magpie/barkeep pace read the live clock.
    private static void FillComplexDroids(double simTime, DeckPlan.Droid[] buffer, bool backRoomOpen,
        double barkeepX, double barkeepServiceY, IReadOnlyList<SeatedRegular> regulars, bool oracleHere)
    {
        DeckPlan.Ship.FillDroids(simTime, buffer); // fills [0..3)
        double sway = 0.05 * System.Math.Sin(simTime * 0.0009);
        buffer[3] = new DeckPlan.Droid(CustomsDesk.X, CustomsDesk.Y, -System.Math.PI / 2, "Customs"); // officer beside the gate

        // The four regulars sit at [4..8). A present one gets a tiny seeded thermal shuffle around their
        // seated anchor + a look-around facing twitch (ReeverIdle, #390) so they read alive, not carved;
        // an away one is parked far off-frame (their chair is simply empty this watch). Roster order is
        // stable, so index 4+i is the i-th regular whether or not they're here.
        for (int i = 0; i < 4; i++)
        {
            int slot = 4 + i;
            if (i < regulars.Count && regulars[i].Present)
            {
                SeatedRegular r = regulars[i];
                (double jx, double jy) = SpaceSails.Core.ReeverIdle.JitterAt(r.Seed, simTime);
                double face = r.Facing + SpaceSails.Core.ReeverIdle.FacingTwitchAt(r.Seed, simTime);
                buffer[slot] = new DeckPlan.Droid(r.X + jx, r.Y + jy, face, r.ShortName);
            }
            else
            {
                buffer[slot] = new DeckPlan.Droid(-9999, -9999, 0, i < regulars.Count ? regulars[i].ShortName : "Regular");
            }
        }

        NpcPost m = ResolveMagpie(simTime, backRoomOpen);
        buffer[8] = m.Present
            ? new DeckPlan.Droid(m.X + sway, m.Y, m.FacingRad, "Magpie")
            : new DeckPlan.Droid(-9999, -9999, 0, "Magpie"); // out of reach this watch — off-frame

        // #247 — the barkeep, pacing their patch BEHIND the counter (owner: "a barkeep pacing their bar
        // area is fine"; and 2026-07-18, "Evening wind": "in all bars that have a bar-desk in their
        // graphics the barkeep is positioned behind the bar desk"). No rota (they don't leave the bar): a
        // deterministic sine sweep, the same idiom as the seated regulars' sway. Centred on THIS bar's
        // service point (BarDesks), one du further back than the counter wall — so the keep works the far
        // side of the desk drawn in the art, never the window band the first pass parked them in. Facing
        // south (−π/2), across the bar toward the captain.
        double pace = 1.5 * System.Math.Sin(simTime * 0.00035);
        buffer[9] = new DeckPlan.Droid(barkeepX + pace, barkeepServiceY + 2, -System.Math.PI / 2, "Barkeep");

        // #425 — the station oracle, hunched over her corner drink when the rota has her here this watch.
        // A seeded thermal shuffle + facing twitch (ReeverIdle) so she reads alive, muttering at the wall;
        // parked far off-frame on the watches she's drifted off (her stool simply empty, no console). Index
        // 10, the buffer's last complex slot (droidCount 11).
        if (oracleHere)
        {
            ulong oseed = RegularSeed("STATION-ORACLE", PatronRota.WatchIndex(simTime));
            (double ojx, double ojy) = SpaceSails.Core.ReeverIdle.JitterAt(oseed, simTime);
            double oface = -System.Math.PI / 2 + SpaceSails.Core.ReeverIdle.FacingTwitchAt(oseed, simTime);
            buffer[10] = new DeckPlan.Droid(OracleCorner.X + ojx, OracleCorner.Y + ojy, oface, "Oracle");
        }
        else
        {
            buffer[10] = new DeckPlan.Droid(-9999, -9999, 0, "Oracle");
        }
    }
}
