namespace SpaceSails.Client.Rendering;

// Subject: ONE KIND PER VERB — every press this game can offer at a fixture (part of DeckPlan).
//
// The list is long on purpose and it grows the way it does on purpose: nearly every entry below carries
// the argument for why it is its OWN kind rather than a re-used neighbour, and the argument is always the
// same one — two fixtures share a kind only when they are the same VERB. A stool and a canteen top are
// both somewhere to sit and are two kinds, because a stool seats you with your back to the room. A goods
// car and a cage are both lifts and are two kinds, because only one of them runs the gate. Merge two and
// the dispatch answers the wrong question at one of them, which is a bug no test shape can see.
//
// #251 · MOVED HERE BY PURE MOTION out of `DeckPlan.cs` — see the note at the head of that file for why
// it was cut and what "pure motion" is holding across the family. The order of the members is part of
// the record: they are appended in the order the game grew, and #633's five are appended rather than
// merged into the run above them so the reunification stays legible in the list itself.

public sealed partial class DeckPlan
{
    public enum ConsoleKind { None, Helm, NavPost, Scope, Vent, Cargo, Shuttle, Cantina, CommsSeat, TacticalSeat, TradeSeat, Head, Airlock, BarPatron, Hatch, ViewObject, Stash, ShuttleAirlock, Barkeep, DigSite, SurfaceAirlock, ShelterDoor, Kiosk, MedKit, Bunk, SealedDoor, DiscoveryCache, DrillPoint, SecretDoor, LabCache, LabConsole, SelfieSpot, WreckEvidence, WreckSalvage, WreckValves, WreckBridgePanel, WreckPressureDoor, WreckScuttle, WreckPlacard, ShipDoor, ShipValves,
        // #563 · The outpost hut: its dogged hatch (force it, the room appends), the ammunition locker
        // inside, and whoever's effects are still on the floor.
        OutpostDoor, OutpostCache, OutpostEffects,
        // #573 · The deep shelter's charging rack — the only place outside her tube that refills a suit.
        ShelterTank, ShelterLocker, RuinSalvage,
        // #586 · Whatever somebody left at the foot of the monolith this visit-window.
        MonolithFoot,
        // #585 · THE HIVE: the lift car, a room to search, a door that never opens, and the
        // camouflaged lift head that is the only part of the whole facility above ground.
        HiveLift, HiveHaul, HiveSign, HiveHead,
        // #801 · THE SECOND CAR. Its own kind rather than a second HiveLift, because the two are not the
        // same machine: the cage climbs out and runs the gate, the goods car does neither, and a [E] press
        // that could not tell them apart would open the cage's panel at the other end of the building.
        HiveServiceLift,
        // #719 · THE SERVICE STAIR. Its own kind for the goods car's own reason, one step further: this is
        // not a machine at all. There is no panel to open and no floor to choose — the press is the climb,
        // it goes one way, and it is paid for out of the tank. A [E] that could not tell it from a car would
        // offer a captain a lift directory at the top of a flight of stairs.
        HiveStair,
        // #608 · The pressure refuge's rack. Its OWN kind rather than a re-used ShelterTank, because the two
        // buildings share the air law and nothing else: the surface rack sits beside an ammunition press in
        // a regolith drum that a whole site has several of, and this is a poured room a safety inspectorate
        // made somebody build, one per dead floor, with nothing in it but air. One kind per verb.
        HiveRefuge,
        // #707 · The canteen counter, the basin run, the bank of machines. ONE kind for all three, because
        // they are one verb — stand in a room somebody ate or washed in, and read what is left of it — and
        // Core already carries which of the three it is, in the plate and the fixture name. Three kinds
        // would be three cases in the dispatch all answering the same way.
        HiveAmenity,
        // #709 · Somebody sitting at one of the canteen's tables — the Hive's FIRST people. Its own kind
        // rather than a re-used BarPatron: the topside patron is welded to contacts, bonds and the
        // round-buying economy (Map.Quests), and none of that is true of a haulier who has been waiting three
        // days for a signature. One kind per verb — stop at a table, hear one breath of somebody's working day.
        HiveRegular,
        // #757 · A canteen top with NOBODY at it. Its own kind rather than a HiveRegular with no plate,
        // because it is a different VERB: HiveRegular is "ask somebody whether you may join them", and this
        // is "take the table" — the normal way to operate in a bar, and the one the room refused outright
        // until #757 (owner, live in the hall: "I have empty table but I cannot sit down"). An empty top
        // carried no console at all, which is why [E] answered nothing there: not a refusal anybody could
        // read, an absence.
        HiveTable,
        // #709 · The cork board on the canteen wall. Its own kind and NOT a HiveSign: a sign is a door that
        // will not open and says what is behind it, and this is paper somebody pinned up — read one notice at
        // a time, filed, and worth coming back to when you have met the people in the room.
        HiveBoard,
        // #793 · A STEEL BENCH IN THE PARK. Its own kind and not a HiveTable: #790 shipped the benches as
        // labelled fixtures with nothing to press, and a bench is a different seat from a canteen top in the
        // one way this game cares about — it has two ends and no chair opposite, so somebody who takes the
        // other end is sitting BESIDE you rather than starting a conversation. Owner: "it is a good gumshoe
        // move to see if anyone is following us by foot" / "if we get the whole bench to ourselves."
        HiveBench,
        // #817 · A CHAIR AT A DESK IN A PARK-VIEW SUITE. Its own kind and not a HiveTable: a canteen top is
        // a place in a room full of people, where sitting down is a choice to be FINDABLE and somebody may
        // come over. An office chair is in a room with a door, in a building whose staff are somewhere else
        // — the same posture, the same panel, the same wait beat, and nobody ever arrives. Owner, live in
        // one of these on a bare deck: "in office people sit down … Let's make some cubicles / desks /
        // chairs we can sit in."
        HiveOfficeChair,
        // #973 L5b · A TOP IN A DOCKED STATION'S BAR, WITH NOBODY AT IT. Its own kind and not a re-used
        // HiveTable, for the reason every other split in this list is: HiveTable is matched back against
        // `CanteenRegulars.Tables` on a floor of the Hive, and a berth has no floor, no canteen watch and no
        // excursion at all — one kind serving both would put a press in a room it cannot ask a question
        // about. #973 L0's own file said what was missing out loud: "the bar's seven tops are drawn dressing
        // with no chairs and no console", so [E] at one answered nothing. This is the console.
        BarTop,
        // #1016 · …AND THE SAME CONSOLE IS THE SHIP'S OWN CANTINA TOPS. Owner, on 7 Deck: "Why no table
        // here to sit at?" and "I expect to have a bar table like this in this ships galley also....
        // feature complete." Her three drawn tops were dressing with nothing over them, which is the SAME
        // absence #973 L0 wrote down one room over — so it is the same kind, the same verb and the same
        // sitting, and the page's one answer (`TheBarTopUnderfoot`) says which room the press was in.
        //
        // #1016 · THE DESK IN THE CAPTAIN'S BERTH. Its own kind and not a re-used BarTop, for the reason
        // every split in this list is: it is a different FIXTURE in a different room — a desk in a berth
        // with a door, not a top in a room with a counter — and the two must be able to disagree about
        // their plate, their setting, their privacy rung and what the room says when nobody comes. It is
        // still the same VERB (the sitting is opened in the one place sittings are opened), which is why
        // the dispatch arm below it is shared rather than copied.
        ShipDesk,
        // #1040 · THE STOOL ROW AT HER OWN COUNTER. Its own kind and not a re-used BarTop, for the reason
        // every split in this list is: a stool is a different FIXTURE — a tall backless seat bolted in a row
        // along a counter, where your back is to the room — and the two must be able to disagree about their
        // plate, their setting, their silence and, above all, their RUNG. A top seats you on the hall-table
        // rung; a stool seats you on the BAR STOOL rung, where the gumshoe rule refuses the spread out loud.
        // It is still the same VERB (the sitting is opened in the one place sittings are opened), which is
        // why the dispatch arm below it is shared rather than copied. Owner, on 7 Deck: "Our on ship bar can
        // be upgraded to match the other bars... the UI represents code long time ago."
        ShipStool,
        // THE ARCHIVE NODE (docs/features/the-archive-node.md): the column you go and look at, and the
        // handle stencilled on its housing. TWO kinds for one object, because they are two different
        // decisions — looking costs a throw, and pulling must stay possible without one.
        ArchiveNode, ArchiveSwitch,
        // #633 · …and `main`'s five, appended rather than merged into the run above, so the reunification is
        // legible in the list itself. HER OWN SCUTTLING CHARGES (the derelicts have carried a panel since
        // #488; a ship is a ship), and #538's lab security: the door, the board that governs it, the alarm
        // and the key card that answers to it.
        ShipScuttle, LabDoor, LabDoorBoard, LabAlarm, LabKeyCard,
        // #698 · WHAT THE CAPTAIN THEMSELVES PUT DOWN. Owner, on B12 of the clinic: "I dropped 3 files on
        // somebody here but there was nothing marked onto the map?" One mark per SPOT — never per item —
        // appended onto whatever deck the excursion is standing on, and the only console in the game whose
        // [E] is answered before the dispatch ever reaches this enum (#691: your feet before the walls).
        LeftBehind,
        // #821 · THE PUBLIC WASHROOM ON THE PARK BLOCK: a WC cubicle's own leaf (press it from inside and
        // the catch goes over), and the basin run, which is a fixture with a SPAN on it the way the bar desk
        // is — one console down the whole length of the porcelain rather than a tap you have to find.
        HiveCubicle, HiveBasin,
        // #869 · THE SIT-STAND DESK, as its two controls. Two kinds and not one, because they are two
        // different decisions on one piece of furniture: the UP/DOWN paddle at one end of the desk's own
        // front face (press it and the motor runs), and the PRESET buttons at the other (read what they
        // remember, or lean on them). Neither is a seat — the chair is the sit, and this is a press you make
        // standing up — and one kind for both would file the pettiest verb in the game under "raise the desk".
        HiveDeskEdge, HiveDeskPresets,
        // #535 · THE BLACK-OPS KEY, in somebody's kit in the crew spaces of a hull that fought. Its own kind
        // and not a re-used RuinSalvage, for the reason every other split in this list is: a ruin's drawer is
        // a weighted roll with five faces and an empty room among them, and this is one object that either is
        // aboard or is not. One kind per verb — pick up the thing, and look at it.
        WreckKey }

}
