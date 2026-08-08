namespace SpaceSails.Core;

/// <summary>
/// #439 · THE DEV START SITES — the quick-start catalogue, in the game's own front door.
///
/// <para>Owner, 2026-07-26: <i>"We should have a developer list of these quick starts in the UI also. We can
/// later disable it. These special places to start should be shown in the UI and marked as dev start
/// sites."</i> The URL cheats (<c>?dock=</c>, <c>?bond=1</c>, <c>?site=N</c>, …) have always been the fastest
/// way to stand somewhere specific, but they lived only in <c>docs/testing-guide.md</c> — you had to know
/// them, and type them. This is that same list as DATA, so the logbook can offer it as buttons.</para>
///
/// <para>This is the catalogue only — one row per entry point, in the house voice, with the exact URL the
/// cheat parser already understands. It is deliberately pure and testable: the client renders it behind a
/// single switch (<c>Map.ShowDevStarts</c>) so the whole section can be turned off in one line when the
/// game stops wanting a service door. Adding a cheat here is adding a button; nothing else changes.</para>
///
/// <para>Keep in step with <c>docs/testing-guide.md</c> — that table is the prose twin of this list.</para>
/// </summary>
public static class DevStarts
{
    /// <summary>One quick-start: the glyph, what it is, what it puts you in the middle of, and the URL that
    /// gets you there. <paramref name="Url"/> is a boot-time cheat, so the client navigates with a FULL
    /// reload — these are read once, while the world is built.</summary>
    public readonly record struct Entry(string Icon, string Label, string Blurb, string Url);

    /// <summary>The catalogue, grouped loosely by what you are going to look at: the grounds and the
    /// away-team gigs first (where the walking happens), then the set pieces, then the long stories.</summary>
    public static IReadOnlyList<Entry> All { get; } =
    [
        // --- The regolith: excursions, the Old Ones, and the ground itself -------------------------------
        // #649 · THE MONOLITH'S OWN GROUND, and it is not Miranda's. The word was reserved for the one
        // object, and the one object stands where every treasure map has said it does since #164.
        new("▮", "Phobos — THE MONOLITH",
            "Off The Space Bar, straight down onto the Stickney rim: open regolith and the one thing in the system nobody can account for. The long walk, the card on [E], and whatever somebody left at its foot this window (#649).",
            "/map?dock=the-space-bar&body=phobos&site=0&land=1"),
        new("👁", "Phobos — something is paying attention",
            "The monolith's ground with its attentive window forced open and the dwell cut to a couple of seconds. Walk down the shadow, stand at the stone, and wait. Rare by design — one visit-window in three, and then only if you stay (#649).",
            "/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"),
        new("🛬", "Miranda — the false-slab maze",
            "Docked at The Tilt with Miranda in shuttle reach. Walk to the shuttle bay, board, and set down on the canon ground: the maze, the stacked slab at the heart of it, and the Old Ones under it.",
            "/map?dock=the-tilt&site=0"),
        new("🌑", "Miranda — the Shadowed Rille",
            "The same moon, a different world: site 1 re-seeds the ground into a gully of permanent night. The A/B for \"a body is a world, not a level\" (#320).",
            "/map?dock=the-tilt&site=1"),
        new("🪂", "Miranda — land me straight on the ground",
            "Rides the shuttle down the instant the world is ready — the real descent, the real ground, no walk to the hatch and no boarding panel. The one-URL way to playtest anything on a surface (#464).",
            "/map?dock=the-tilt&site=0&land=1"),
        new("🧟", "Miranda — jumped the moment you land",
            "The canon ground with FOUR Old Ones set down on top of you, already aware. The chase, the pack spacing and the exchange — block rolls, blood, the five blows — inside seconds instead of a long walk and a lucky tide (#453).",
            "/map?dock=the-tilt&site=0&land=1&reevers=4"),
        // #585 · THE SURFACE TOUR. Owner: "let's go over all the sites we have not yet tested with the
        // url-arguments" — which was impossible until ?body= existed, because ?land=1 takes the first
        // landable body in shuttle reach and from The Tilt that is always Miranda. One per system here; the
        // full 27-site list lives in docs/testing-guide.md.
        new("🌍", "Luna — the mass-driver ruins",
            "Earth's moon from Selene Gate: long launch rails and strip foundations instead of a maze. The A/B against Miranda for \"a body is a world, not a level\" — and the first ground on this tour nobody has ever walked (#585).",
            "/map?dock=selene-gate&body=luna&site=1&land=1"),
        new("🥔", "Phobos — the Ice Fissure",
            "Mars' potato from The Rusty Roadstead. A crack in the crust breathing old cold, now with real rooms to walk into (#585).",
            "/map?dock=the-space-bar&body=phobos&site=1&land=1"),
        new("🪐", "Ganymede — the Ridge Camp",
            "Off The Red Eye: the bones of an old survey camp on a Jovian moon. Somebody left in a hurry (#585).",
            "/map?dock=red-eye&body=ganymede&site=1&land=1"),
        new("💍", "Titan — the Quiet Basin",
            "From Ringside Exchange: a low bowl of settled dust. Too quiet, if you stand still long enough to notice (#585).",
            "/map?dock=ringside-exchange&body=titan&site=1&land=1"),
        new("🧊", "Triton — the Derelict Pad",
            "The far end of the run, off The Deep. A landing stage gone to rust and silence — something set down here and stayed (#585).",
            "/map?dock=the-deep&body=triton&site=2&land=1"),

        new("⛏", "An away-team gig, already accepted",
            "A mining rock parked in shuttle range with the job on the books — the shortest road to boots on the regolith (#370).",
            "/map?expedition=mining"),
        new("🔬", "The secret lab behind the hidden door",
            "A landable rock in shuttle range hiding a Vantar lab, its hidden door already found — force it and read what shouldn't exist (#409).",
            "/map?secretlab=1"),
        // #677 · The one place in the game a captain could otherwise never find. About one site in fifty has
        // galleries under it and the way in is a card eleven floors down, so the front door gets a button:
        // owner's own rule for this section, "these special places to start should be shown in the UI".
        new("🕳", "The halls nobody dug",
            "Set down at a lift head with the whole wallet, over a laboratory, over a clinic nobody listed, over four floors of rock, over something that was already there (#677).",
            "/map?found=1&land=1"),
        // #709 / #694 · The same rule one floor up, and one floor down. The canteen is the only room in the
        // building with people in it, and B21 is the only place the facility plate ever changes its mind —
        // neither should cost a twenty-floor lift ride to find.
        new("🍸", "The canteen on B1, with people in it",
            "150 m down, in the one room this facility admits outsiders to: carriers and contractors at the tables, a cork board on the wall, no pass required (#709).",
            "/map?secretlab=deep&land=1&floor=1"),
        // #693 · The card's own row, which until here nobody could look at without a real Key hunt. #692
        // shipped the gated button, the promise it makes and the beat it pays off, and closed with the note
        // that none of the three had been seen in a browser — "a scene nobody can reach on demand is a scene
        // that ships broken", written into the same file that then could not follow it.
        // #756 · The counter that takes orders. Owner walked to this exact fixture in a live playtest and
        // could not buy a thing; the fix is worth nothing if the next tester has to make the same walk to
        // check it. Stands you AT the service spot, purse and all, one press from the card.
        new("🍹", "The counter, ready to order",
            "B1 of a deep site, standing at THE COUNTER. Press E: the service card opens on the menu — "
            + "coffee, a fry-up, and three pours that joke about the deep. Buying answers on the card (#756).",
            "/map?counter=1"),
        // #756 · THE HIGH CHAIRS. Owner, live: "there should be high chairs so sitting at the bar desk is
        // also possible." Two rows, for #757's own reason: whether the one beside you turns is a seeded roll
        // sitting behind a seeded occupancy, so without a lever the she-speaks beat is reachable only by
        // luck and the told silence only by more of it. Both halves are the feature.
        new("🪑🍸", "Up on a stool, and somebody turns",
            "B1 of a deep site, already seated at THE COUNTER: the card wears the window wall and the park "
            + "beyond it, the menu is still under your elbow, and WAIT turns the one on the next stool — she "
            + "never asks to sit, because she is already sitting (#756).",
            "/map?stool=1&neighbour=1"),
        new("🪑🤐", "…and the counter where nobody turns",
            "The same stool with the neighbour forced OFF. Wait as long as you like: the seat beside you "
            + "answers, in words, that nothing is going to happen. That is the beat, not a bug (#756).",
            "/map?stool=1&neighbour=0"),
        new("🎫", "The lift row the card unlocks",
            "B1 of a deep site with exactly the authority the gate downstairs reads already in the wallet. Walk to the car: the sealed row now names the paper in your pocket, and the beat lands when the doors open (#693, #689).",
            "/map?secretlab=deep&land=1&floor=1&card=next"),
        // #768 · THE MOMENT A CARD STANDS ON A SENTENCE. The same paper as the row above, pressed from the
        // SURFACE instead of from B1 — which is the one ride that raises the first-descent card (#585) and
        // crosses a gate (#689) on the same arrival. It could not be reached on demand at all before #693's
        // ?card= cheat, and it is the exact frame the owner filed the issue about.
        new("🃏", "The card that lands on top of the beat",
            "The surface of a deep site with the authority the first gate reads already in the wallet. Into the shed, press the row that is no longer sealed, and ride from daylight straight through the gate: the first-descent card stops the world — and the gate's own beat is waiting on the pulse when you close it, instead of having played under it (#768, #693).",
            "/map?secretlab=deep&land=1&card=next"),
        // #757 · The table the owner could not sit down at, and the two answers waiting at it can give. Both
        // rows exist for the same reason #693's does: whether anybody crosses the room is a seeded roll at
        // one top on one shift, so without a lever the somebody-comes beat is reachable only by luck and the
        // told nobody-came outcome only by more of it.
        new("🪑", "A table with nobody at it — take it, and wait",
            "B1's canteen with the captain standing at a FREE top. [E] takes the table; WAIT holds it. Somebody is forced to cross the room on the next wait: she asks for the chair, offers the round, and then says what she came over for (#757).",
            "/map?tablescene=free&approach=1"),
        new("🪑🕳", "…and the watch where nobody comes",
            "The same table on the small watch with the approach forced OFF. Wait as long as you like: an eighty-seat hall answers, in words, that nothing is going to happen. That is the beat, not a bug (#757).",
            "/map?tablescene=free&watch=5&approach=0"),
        new("▣", "The sign that says a different building",
            "B21 of a twenty-floor clinic — the unlisted band's own lobby, where the plate beside the shaft names something else entirely (#694, #592).",
            "/map?secretlab=deep&land=1&floor=21"),

        // --- The bar, the room, and the people in it ------------------------------------------------------
        new("🥂", "The cognac on the fright",
            "Docked at the Roadstead bar with the next ambient scare forced to OPEN a stranger — the hero beat of the stranger-bond (#429).",
            "/map?bond=1"),
        new("🍸", "The Rusty Roadstead",
            "Clamped on at the bar with the regulars at the tables — drinks, contacts, the barkeep, and the station Oracle ranting in the corner.",
            "/map?dock=the-space-bar"),

        // --- Set pieces --------------------------------------------------------------------------------
        new("☄", "The rock that must not arrive",
            "The asteroid-deflection gig accepted, the rock inbound, ship docked at Ringside — the whole clock running (#394).",
            "/map?deflection=1"),
        new("💰", "A fat purse",
            "The Sol start with 50,000 credits in the purse, for pricing anything without grinding for it.",
            "/map?credits=50000"),

        // --- The long stories, without the playthrough ---------------------------------------------------
        new("❄", "PROJEKTI KAAMOS — the front door",
            "Docked and standing in the bar, with the freight agent whose docket the board keeps returning in the room. Take the job and the arc opens with no shard in hand (#635).",
            "/map?ashore=1&kaamos=bounce"),
        new("🧊", "PROJEKTI KAAMOS — the head office",
            "The whole route already ridden: every shard, the berth-code resolved, the supply run filed, and the ship let go alongside the ice moon, boots on the ground (#411).",
            "/map?kaamos=hq&land=1"),
        new("🛰", "PROJEKTI KAAMOS — the unlucky arrival phase",
            "The head office reached on ARRIVAL PHASE 2 of 24 — the one placement in twenty-four that used to walk the parked hull into Enceladus at +9.54 h while the captain was 23 floors down. Ride it down, read the hall, come back up to a ship that is still there (#742).",
            "/map?kaamos=hq&arrivalphase=2&land=1&floor=23"),
        new("🌑", "PROJEKTI KAAMOS — assembled",
            "Every fragment of the sealed ice-moon plot already gathered: the intel readout, the reach notice, the berth-code (#411).",
            "/map?kaamos=all"),
        // #663 / #652 · The two rows this lane owes a playtester, because a moment nobody can open on purpose
        // is a moment that ships broken (Map.Sim's own rule). The first is the arc-news beat on arc 2; the
        // second is the honest road's lasting half, which until now was a sentence with nobody behind it.
        new("🧠", "NEBULA MUTUAL — assembled, and it BREAKS",
            "Arc 2 complete: the cold archive, the brain-backup's true origin, the policy's true terms — and the moment the world notices, on the wire, in a filing clerk's flattest voice (#422, #663).",
            "/map?nebula=all"),
        new("🤝", "The honest road, and who signs it",
            "Aboard a derelict that died of a drive failure. Read two stations, then the cargo console: FILE THE REPORT, name the cause right, and the assessor who countersigns it has a name and stays on your contact ledger (#488, #652).",
            "/map?wreck=drivefailure&land=1"),
        new("🛰", "THE CONVERGENCE",
            "Just enough of BOTH arcs to fire the marquee one-time reveal from a single URL — the two rabbit holes crossing (#422).",
            "/map?converge=1"),
    ];

    /// <summary>The banner the UI wears over the list, so a dev start is never mistaken for a real voyage.</summary>
    public const string SectionTitle = "⚙ Dev start sites";

    /// <summary>Why these exist, in one line, under the title.</summary>
    public const string SectionBlurb =
        "Service entrances — they drop you straight into one thing to look at. Not part of the game, and not a saved voyage.";
}
