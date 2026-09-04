using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Deck (#870 split; the header note lives in Map.Deck.cs) — the [E] press: what is at your feet, what bin is nearer, and then the one switch over every console kind on every deck in the game.
public partial class Map
{
    private void InteractAtConsole()
    {
        // ── #688 · WHAT IS AT YOUR FEET IS ANSWERED BEFORE WHAT IS IN THE WALLS ──
        //
        // Owner: "no way to drop stuff." The satchel can now put a thing down, and #615's law says leaving
        // must never destroy — so there has to be a way back, and E is the verb that finds things.
        //
        // It sits AHEAD of the console dispatch rather than inside the two search verbs, because a captain
        // who set a relic down while standing on a room console would otherwise be standing on top of their
        // own possession with no way to reach it. One press deals with the ground; the next one is the
        // console's, exactly as before.
        if (TryPickUpWhatYouLeft())
        {
            return;
        }

        // ── #828 · AND THE BIN TAKES THE KEY WHEN YOU ARE STANDING AT ONE ──
        //
        // Owner, evening playtest at his own table: "I think the trash could be an e-use ... where we select
        // from inventory the processed items we rip and deposit into trash."
        //
        // A bin is a plated box on the plan and deliberately not a console (#798), so its press is claimed
        // here rather than as a ConsoleKind arm. It cannot shadow anything: TheBinTakingYourPress answers
        // only when the bin is NEARER than whatever console is in reach, which is the same rule
        // NearestConsoleSpot itself had to learn — walk to a thing, and the thing you walked to answers.
        if (TryOpenTheBinOverTheSleeve())
        {
            return;
        }

        switch (_deckPlan.NearestConsole(_avatarX, _avatarY))
        {
            case DeckPlan.ConsoleKind.None:
                // No console in reach. On a surface excursion, bare-ground E is the beach-comber dig —
                // bury the carried chest here, or probe an empty hole (a no-op anywhere else).
                SurfaceGroundInteract();
                break;
            case DeckPlan.ConsoleKind.Airlock:
                // Only the bare-ship gangway raises this (the docked complex drops the console — you
                // walk the tube). So it's always the "can't go ashore here" nudge.
                ShowPulseMessage(_dockedHavenId is null
                    ? "No gangway rigged — clamp onto a haven first (⚓ Dock)."
                    : "Nothing to step into here — no deck ashore at this berth (yet).");
                break;
            case DeckPlan.ConsoleKind.BarPatron:
                TalkToStranger();
                break;
            case DeckPlan.ConsoleKind.Barkeep:
                TalkToBarkeep();
                break;
            case DeckPlan.ConsoleKind.Hatch:
                KnockOnHatch();
                break;
            case DeckPlan.ConsoleKind.Stash:
                LiftStash();
                break;
            case DeckPlan.ConsoleKind.ViewObject:
                ViewNearbyObject();
                break;
            case DeckPlan.ConsoleKind.SelfieSpot:
                CaptureSelfieAtSpot(); // #400: pose the captain into the vista, file it in the legend ledger
                break;
            case DeckPlan.ConsoleKind.Helm:
                SwitchDesk(ShipDesk.Nav);
                ShowPulseMessage("Back at the helm");
                break;
            case DeckPlan.ConsoleKind.NavPost:
                SwitchDesk(ShipDesk.Nav);
                if (!PlotMode)
                {
                    TogglePlotMode();
                }
                ShowPulseMessage("Nav post: plotting table lit");
                break;
            case DeckPlan.ConsoleKind.Scope:
                SwitchDesk(ShipDesk.Sensors);
                ShowPulseMessage("Scope alcove: sensors online");
                break;
            case DeckPlan.ConsoleKind.Cantina:
                // #1021 · THE CANTINA HANDS YOU THE CARD AND KEEPS THE ROOM. This press used to
                // SwitchDesk(ShipDesk.Galley) — which took the captain off the deck they were standing on and
                // put a photograph of a bar where the actual bar had been. Owner: "it has no gen AI or
                // visibility to the bar surroundings." Opened from here the card hangs over the cantina's own
                // art, which IS that visibility, and no new picture was drawn for it.
                //
                // It deliberately does NOT switch desks — _deckMode stays as it is and the deck is still
                // under the card. TheGalleyIsACardNotADeskTests presses this console and holds both halves.
                OpenGalleyCard();
                ShowPulseMessage("Cantina: galley's this way");
                break;
            case DeckPlan.ConsoleKind.Head:
                ShowPulseMessage(VisitHead());
                break;
            case DeckPlan.ConsoleKind.MedKit:
                ShowPulseMessage(TakePill());
                break;
            case DeckPlan.ConsoleKind.Bunk:
                // #733: the bunk can now come back empty-handed — a hull whose track reached a surface
                // during the hour is already behind the busted freeze-frame, and has nothing to say about
                // how well she slept. An empty toast over a death card is still a toast.
                if (SleepInBunk() is { Length: > 0 } rested)
                {
                    ShowPulseMessage(rested);
                }
                break;
            case DeckPlan.ConsoleKind.Vent:
                // #523 · The console's own label still says CAPACITOR, not air. What changed is that the
                // dump stopped being the whole interaction: it is one switch on a board that also shows what
                // the hull is holding and what the space around her is doing to it. VentCharge() is still the
                // act — the board is where you decide to take it.
                OpenChargeBoard();
                break;
            case DeckPlan.ConsoleKind.ShipDoor:
                ToggleShipDoorAtHand();     // her own hatches, dogged by hand at the door
                break;
            case DeckPlan.ConsoleKind.ShipValves:
                OpenShipVentPanel();        // the board, aft with the machinery (and its bridge repeater)
                break;
            case DeckPlan.ConsoleKind.ShipScuttle:
                OpenShipScuttlePanel();     // her own charges — two keys, and one of them is the crew's
                break;
            case DeckPlan.ConsoleKind.Cargo:
                ShowPulseMessage(_cargoUnits > 0
                    ? $"Hold: {_cargoUnits} units (worth {_cargoValue:N0} cr)"
                    : "Hold: empty. The fence weeps.");
                break;
            case DeckPlan.ConsoleKind.Shuttle:
                if (_captureEngaged && SelectedCaptureTarget() is { } prey)
                {
                    LaunchShuttleRun(prey);
                }
                else if (_plunderOpportunityTargetId is not null)
                {
                    // In range, but the felony isn't authorized yet — the shuttle waits on the word.
                    ShowPulseMessage("Shuttle's fuelled — but boarding's piracy. Authorize the 🏴 plunder first (the Nav HUD's asking).");
                }
                else
                {
                    ShowPulseMessage("Shuttle ready in the cradle. K-77 and R-3B standing by.");
                }
                break;
            case DeckPlan.ConsoleKind.ShuttleAirlock:
                // #531 · THE BELTS ARE ON THE BOAT. A drained sentry carried back to the lock fills here, and
                // that is the whole press — the destination list is one more press away, so nothing is taken
                // from a captain who came to leave rather than to reload.
                if (TryFillCarriedSentryAtTheLock())
                {
                    break;
                }

                if (_surface is not null)
                {
                    // #540 · ASK HER FIRST. Everything below this line is IRREVERSIBLE — the lock cycles, the
                    // scuttle resolves against whatever was still aboard — and a boat whose hatch is dogged is
                    // not going anywhere, so doing any of it before she answers would resolve a departure that
                    // never happened. Asking also STARTS her warm-up, which is the honest reading of pressing E
                    // at a lock: you tried to leave, and she has begun waking.
                    if (!BoatReadyToFly())
                    {
                        break;
                    }

                    // #488: leaving a wreck, the boat's own lock CYCLES rather than opening — it matches
                    // whatever the hull is reading first, so the shuttle's air is never once exposed to it.
                    if (OnWreck)
                    {
                        ShowPulseMessage(HullVenting.ShuttleLockLine(_spinePressurised));
                        // #488: leaving with the keys turned. Capture what was still aboard BEFORE she is
                        // gone — the question cannot be asked afterwards.
                        ResolveScuttleOnDeparture();
                    }
                    LiftOffFromSurface(); // back aboard mid-excursion: the airlock is the ride home
                }
                else
                {
                    OpenShuttleBayDoor();
                }
                break;
            case DeckPlan.ConsoleKind.DigSite:
                DigSiteInteract();
                break;
            case DeckPlan.ConsoleKind.SealedDoor:
                SealedDoorInteract(); // #371 Phase 3: force the door open — the channel that appends a region
                break;
            case DeckPlan.ConsoleKind.DiscoveryCache:
                DiscoveryCacheInteract(); // #371 Phase 3: claim a forced chamber's cache
                break;
            case DeckPlan.ConsoleKind.DrillPoint:
                DrillPointInteract(); // #394: drill the charge (a long channel), or fire it once armed
                break;
            case DeckPlan.ConsoleKind.SecretDoor when OnWreck:
                // #537: the plate a sounding found. One press, three lives — cut it, get in, get out.
                WorkTheFalsePlate();
                break;
            case DeckPlan.ConsoleKind.SecretDoor:
                SecretDoorInteract(); // #409: force the hidden lab door — the channel that appends the lab region
                break;
            case DeckPlan.ConsoleKind.LabDoor:
                // #409+ · a door in the mountain: open it, shut it, or — with Vantar's card — key it.
                WorkTheDoor(NearestLabDoorId());
                break;
            case DeckPlan.ConsoleKind.LabDoorBoard:
                OpenDoorBoard();            // the vent panel's own idiom, for doors
                break;
            case DeckPlan.ConsoleKind.LabAlarm:
                OpenAlarmPanel();           // "something to try to hack"
                break;
            case DeckPlan.ConsoleKind.LabKeyCard:
                TakeVantarsCard();
                break;
            case DeckPlan.ConsoleKind.LabCache:
                LabCacheInteract(); // #409: claim Vantar's fat one-time cache
                break;
            case DeckPlan.ConsoleKind.HiveLift:
            case DeckPlan.ConsoleKind.HiveHead:
            case DeckPlan.ConsoleKind.HiveServiceLift:
                // #585: down the shaft, or back up out of it. #801: whichever of the two cars you walked
                // to — the method asks the pressed spot, so this arm does not have to know.
                HiveLiftInteract();
                break;
            case DeckPlan.ConsoleKind.HiveStair:
                // #719: the second way out. No panel and no floor to choose — the press IS the climb, it
                // goes one way, and the tank pays for it.
                ClimbTheStairOut();
                break;
            case DeckPlan.ConsoleKind.HiveHaul:
                HiveHaulInteract();   // #585: turn over one room of the facility
                break;
            case DeckPlan.ConsoleKind.HiveSign:
                HiveSignInteract();   // #585: read a door that will not open
                break;
            case DeckPlan.ConsoleKind.HiveRefuge:
                HiveRefugeInteract(); // #608: read the pressure refuge's rack on a dead floor
                break;
            case DeckPlan.ConsoleKind.HiveAmenity:
                HiveAmenityInteract(); // #707: stand at the counter, the basins or the machines
                break;
            case DeckPlan.ConsoleKind.HiveRegular:
                // #746 · ASK TO JOIN comes first: at a table with a free seat and one of the three regulars
                // in it, [E] sits you down and opens the scene. Everybody else in #709's cast keeps their
                // one breath, which is what a canteen full of quest-givers would have cost us.
                if (!TryOpenTable())
                {
                    HiveRegularInteract(); // #709: one breath of somebody's day, and nothing else
                }
                break;
            case DeckPlan.ConsoleKind.HiveTable:
                // #757 · TAKE THE TABLE — the other half of the same verb, and the half the room refused
                // outright: owner, live in the hall, "I have empty table but I cannot sit down." Sitting
                // down alone is a choice to be FINDABLE, and WAIT is what you do once you have made it.
                TryTakeTable();
                break;
            case DeckPlan.ConsoleKind.BarTop:
            case DeckPlan.ConsoleKind.ShipDesk:
            // #1040 · …AND HER COUNTER'S STOOLS COME THROUGH IT TOO. Owner, on 7 Deck: "Our on ship bar can
            // be upgraded to match the other bars... the UI represents code long time ago." A stool is the
            // same VERB as a top — you walk up to a piece of furniture and sit on it — so it is the same
            // press and the same sitting; what a stool changes is the RUNG it leaves you on, and that
            // travels on the page's answer exactly as every other difference between these rooms does.
            case DeckPlan.ConsoleKind.ShipStool:
                // #973 L5b · TAKE A TOP IN THE DOCKED BAR — the eighth way to open a sitting in this game,
                // and the first one that is not on a surface excursion. #973 L0 wrote the gap down ("the
                // bar's seven tops are drawn dressing with no chairs and no console"); this is the press.
                // Sitting alone in a classy room is also what a walk-in is looking for.
                //
                // #1016 · …AND THE SHIP'S OWN TWO SEATS COME THROUGH THE SAME ARM. Owner, on 7 Deck: "Why
                // no table here to sit at?", "Why no table in cabin either?", "I expect to have a bar
                // table like this in this ships galley also.... feature complete." A top in her cantina
                // and the desk in her berth are the same VERB as a top in a station bar — pull the chair
                // out and sit down — so they are the same press and the same sitting, opened in the one
                // place a sitting is opened. What is different about each room is an ANSWER the page hands
                // back (`TheBarTopUnderfoot`), never a second way to sit.
                TryTakeBarTop();
                break;
            case DeckPlan.ConsoleKind.HiveBench:
                // #793 · SIT ON THE BENCH. #790 put them in the park as plates over solid steel and said
                // the verb would arrive with #778; it has. A bench with somebody already on the far end
                // still answers — half a bench is a rest, and it is the rung of the exposure ladder that
                // teaches the privacy law by refusing the spread OUT LOUD rather than by having no control.
                TryTakeBench();
                break;
            case DeckPlan.ConsoleKind.HiveOfficeChair:
                // #817 · SIT DOWN AT A DESK. Owner, live in a park-view suite: "in office people sit
                // down… Let's make some cubicles / desks / chairs we can sit in." Same posture, same
                // panel, same wait beat as a canteen top — and #820's snap, so the body ends up IN the
                // chair rather than beside it.
                TryTakeOfficeChair();
                break;
            case DeckPlan.ConsoleKind.HiveDeskEdge:
                // #869 · RAISE THE DESK. Owner, from his own electric one: "it got up- and down buttons to
                // move the table to work either with office chair, Salli standing (lab) chair or by standing
                // while using the table." A distinct press from sitting — the chair is the sit, and this is
                // the paddle you reach for on your feet.
                PressTheDeskEdge();
                break;
            case DeckPlan.ConsoleKind.HiveDeskPresets:
                // #869 · THE MEMORY BUTTONS — read what this desk remembers about somebody who is not here,
                // and lean on them after that. Owner: "love the gag of messing with somebodys desks memorized
                // height options :-D" Nothing files and nothing scores, either way.
                PressTheMemoryButtons();
                break;
            case DeckPlan.ConsoleKind.HiveCubicle:
                // #821 · TURN THE CATCH. Owner, standing in the park: "we might want to hide from guards in
                // one toilet cubicle we lock from inside :-D" — from the inside only, and from the outside
                // the door says why it will not give.
                TryTurnTheCatch();
                break;
            case DeckPlan.ConsoleKind.HiveBasin:
                // #821 · WASH YOUR HANDS. A short beat, one pip, and one line out of the authored pool —
                // "some film noir comment at the end ... about if we ever feel like our hands are clean."
                TryWashYourHands();
                break;
            case DeckPlan.ConsoleKind.HiveBoard:
                HiveBoardInteract();   // #709: one notice off the cork board — whose it is, is your problem
                break;
            case DeckPlan.ConsoleKind.MonolithFoot:
                MonolithFootInteract(); // #586: whatever somebody left at the foot this window
                break;
            case DeckPlan.ConsoleKind.RuinSalvage:
                RuinSalvageInteract(); // #573: turn over a ruin — about half hold something
                break;
            case DeckPlan.ConsoleKind.ShelterLocker:
                ShelterLockerInteract(); // #573: the shelter's emergency rounds
                break;
            case DeckPlan.ConsoleKind.ShelterTank:
                ShelterTankInteract(); // #573: charge the suit at the deep shelter
                break;
            case DeckPlan.ConsoleKind.OutpostDoor:
                OutpostDoorInteract(); // #563: force the hut's dogged hatch — the channel that appends the room
                break;
            case DeckPlan.ConsoleKind.OutpostCache:
                OutpostCacheInteract(); // #563: their ammunition locker, spread across your sentries
                break;
            case DeckPlan.ConsoleKind.OutpostEffects:
                OutpostEffectsInteract(); // #563: somebody's wallet, and the only story the place tells
                break;
            case DeckPlan.ConsoleKind.LabConsole:
                LabConsoleInteract(); // #409: read a Vantar log (the core log fires the diced reveal)
                break;
            case DeckPlan.ConsoleKind.Kiosk:
                VisitKiosk();
                break;
            case DeckPlan.ConsoleKind.WreckEvidence:
                ExamineWreckEvidence(); // #488: read the derelict off what is bolted to her deck
                break;
            case DeckPlan.ConsoleKind.WreckSalvage:
                OpenWreckChoice();          // #488: file it, or strip her and say nothing
                break;
            case DeckPlan.ConsoleKind.WreckValves:
                OpenVentPanel();            // #488: the damage-control mimic, aft where the machinery is
                break;
            case DeckPlan.ConsoleKind.WreckBridgePanel:
                TryDeadBridgePanel();       // #488: dead, and it says where the working ones are
                break;
            case DeckPlan.ConsoleKind.WreckPressureDoor:
                OpenPressureDoorCard();     // #488: ten tonnes of atmosphere in a frame
                break;
            case DeckPlan.ConsoleKind.WreckScuttle:
                OpenScuttlePanel();         // #488: making sure — the road that pays nothing
                break;
            case DeckPlan.ConsoleKind.WreckPlacard:
                ReadDamageControlPlacard(); // #488: where the valves are, told at the lock
                break;
            case DeckPlan.ConsoleKind.ArchiveNode:
                ConfrontArchiveNode();      // look at the thing in the hold — the throw, and what it gives back
                break;
            case DeckPlan.ConsoleKind.ArchiveSwitch:
                PullArchiveSwitch();        // the honest legend, with nothing in front of it
                break;
            case DeckPlan.ConsoleKind.ShelterDoor:
                // #585: a shelter door is a door, not a ride home. It has already cycled for you (proximity
                // opens it, the same as the ship's); pressing [E] on it says so and does nothing else.
                ShowPulseMessage(SurfaceShelter.DoorPressLine);
                break;
            case DeckPlan.ConsoleKind.SurfaceAirlock:
                LiftOffFromSurface();
                break;
            case DeckPlan.ConsoleKind.CommsSeat:
                SwitchDesk(ShipDesk.Comms);
                ShowPulseMessage("Comms seat: patched through");
                break;
            case DeckPlan.ConsoleKind.TacticalSeat:
                SwitchDesk(ShipDesk.WarRoom);
                ShowPulseMessage("Tactical seat: war room manned");
                break;
            case DeckPlan.ConsoleKind.TradeSeat:
                SwitchDesk(ShipDesk.Trade);
                ShowPulseMessage("Trade seat: ledgers open");
                break;
        }
    }
}
