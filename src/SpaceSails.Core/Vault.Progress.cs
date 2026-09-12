namespace SpaceSails.Core;

// ─── VAULT SECTION: WHAT THE CAPTAIN HAS ALREADY DONE (#225) ───
//
// ProgressSection on its own, because it is one record with a life's worth of flags on it — every beat
// that must not happen twice, every door already opened, every card already read. It was 285 lines of
// Vault.cs by itself.
// 
// Split out of Vault.cs under #251 — the whole type moved, nothing inside it re-ordered.

/// <summary>Durable player-progression flags. Today it carries just one bit — whether the
/// new-player tutorial has been engaged or completed (#292) — so a loaded save never re-greets a
/// captain who is no longer truly new. Its own section (independently optional, self-described) so
/// future progression flags can join without touching any other part of the vault.</summary>
public sealed record ProgressSection
{
    /// <summary>True once the captain has started or finished a tutorial lesson. Gates the fresh-start
    /// nav-screen promotion: an Earth start greets only a captain for whom this is still false.</summary>
    public bool TutorialPlayed { get; init; }

    /// <summary>#394 — true once this universe's crew turned an inbound rock aside from the Ringside
    /// Exchange (a full or grazing deflection). Persisted per-universe so Ringside's dedication plaque
    /// carries the appended line of gratitude forever after, on this thread and no other. Defaults false
    /// (a pre-#394 file simply lacks the field), so an unsaved port shows only its original dedication.</summary>
    public bool RingsideSaved { get; init; }

    /// <summary>#409 — the body ids where this thread has FOUND one of Dr. Vantar's secret labs (revealed its
    /// hidden door). Persisted per-universe (the vault/thread idiom) so a revisit to a known body shows the
    /// door already revealed — you remember where it is. Defaults empty (a pre-#409 file simply lacks the
    /// field), a lossless round-trip.</summary>
    public IReadOnlyList<string> SecretLabsFound { get; init; } = [];

    /// <summary>#440 — true once this captain has been shown <see cref="GroundLesson"/>, the one card that
    /// explains the surface before their first excursion. Persisted so it greets the truly new and NEVER
    /// again (#292's ruling), even across reloads. Defaults false, so a pre-#440 file — a captain who has
    /// already walked a dozen moons — gets it once on their next trip down and then never more.</summary>
    public bool GroundLessonSeen { get; init; }

    /// <summary>#563 — true once this captain has been shown <see cref="GroundGrows"/>, the card that fires
    /// the first time forcing something open makes the map itself bigger. Same law as
    /// <see cref="GroundLessonSeen"/>: it greets the truly new and never again, because after one showing
    /// the toast says everything a card would. Defaults false, so a captain who has already forced a door
    /// before this existed gets the explanation once on their next one.</summary>
    public bool GroundGrewSeen { get; init; }

    /// <summary>#562 — true once this captain has been shown <see cref="TubeRearm"/>, the card that fires
    /// the first time the ship racks a sentry magazine in her down-tube. Same law again: it teaches the
    /// shape of an excursion (the tube is the one place your sentries get fed, so every trip is a loop with
    /// one anchor) and then never speaks again, because the receipt line says the rest.</summary>
    public bool TubeRearmSeen { get; init; }

    /// <summary>#573 — true once this captain has been shown <see cref="AirCard"/>, the card that fires the
    /// first time their tank passes the low mark on a surface. Same law as its siblings: it teaches the
    /// clock once and then the pulse line carries it.</summary>
    public bool AirCardSeen { get; init; }

    /// <summary>#701 — the <see cref="OddBooks.Entry.Id"/>s this thread has already filed a gist for.
    /// Persisted per-universe (the same idiom as <see cref="SecretLabsFound"/>) because the one-shot law is
    /// about KNOWLEDGE: looking at a book again is free and always will be, but the casebook learns a thing
    /// once. Defaults empty — a pre-#701 file simply lacks the field, and a captain who has read a shelf
    /// they cannot remember reading files it again, which is the harmless direction to be wrong in.</summary>
    public IReadOnlyList<string> OddBooksRead { get; init; } = [];

    /// <summary>
    /// #1066 · CONSECUTIVE WORKING STOPS SINCE THE CREW WERE LAST ASHORE — the whole of the shore-leave
    /// mechanic, as one integer. A clamp at a great port (<see cref="ArrivalTube.Tier.GreatPort"/>) puts it
    /// back to nothing; every other berth adds one, and <see cref="CrewTemp.ShoreLeavePromisesBroken"/>
    /// turns the tally into the broken promise the crew's report reads.
    ///
    /// <para><b>Written only when somebody is counting</b>, which is the #1057/#1072 pattern and not a
    /// stylistic choice: the checksum is taken over the payload, so an extra
    /// <c>"workingStopsSinceShoreLeave": 0</c> on every save would change the digest of every vault ever
    /// written and hang the 📛 tampered marker on an honest voyage. Null means "this file never counted" —
    /// the truth about every save written before shore leave was a number, and about every ship that has
    /// just walked off a great port's gangway.
    /// <c>ShoreLeaveIsALedgerLineTests.ALegacyVaultRoundTripsByteForByteAcrossTheShoreLeaveLine</c> holds
    /// that line against a real pre-#1066 file.</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? WorkingStopsSinceShoreLeave { get; init; }

    /// <summary>
    /// #160 · WHERE THE MILK-RUN LESSON HAD GOT TO — 1..8 while the loop is being walked (the step that is
    /// current, whose line has already been said), one past the last once the coin is on the counter.
    ///
    /// <para><b>Why this one lesson vaults its place and the other three do not.</b> The milk run's eight
    /// lines are said ONCE EACH, as their step becomes the thing to do, and the canon pass' own law is that
    /// none of them is re-fired on a reload. <c>_tutorialStep</c> cannot carry that: it is deliberately not
    /// persisted — it rests at 0 for every captain who never took a lesson, which is exactly what
    /// <c>KeepTheLessonsPreyInTheWorld</c> reads — so a resumed voyage would say line 1 again at the next
    /// berth. This is the lesson's own place, and the page restores <c>_tutorialStep</c> from it.</para>
    ///
    /// <para><b>Written only when there is something to write</b>, the #1066/#1072 rule: the checksum is
    /// taken over the payload, so a <c>"milkRunLessonStep": 0</c> on every save would change the digest of
    /// every vault ever written and hang the 📛 tampered marker on an honest voyage. Null means "this
    /// captain never took the lesson", which is the truth about every file written before it existed.</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? MilkRunLessonStep { get; init; }

    /// <summary>#677 — THE DISCLOSURE CLOCK'S REGISTER: the grounds whose halls this thread has been past the
    /// seam of, and the world-side window each was opened in (<see cref="DisclosureClock"/>).
    ///
    /// <para>Persisted per-universe, the same idiom as <see cref="SecretLabsFound"/> and for a harder version
    /// of its reason. That one remembers WHERE a door is; this one remembers WHEN a ground was opened, and a
    /// clock that forgot across a reload would not be a clock — every threshold written against it would
    /// silently reset to zero the moment a captain closed the tab, and nothing on screen would ever say so.
    /// The first crossing is the one kept (<see cref="DisclosureClock.Note"/>), so a revisit can never move
    /// it.</para>
    ///
    /// <para>Null until somebody has crossed a seam — the #1057/#1072/#1066 pattern, and here for their
    /// exact reason: the checksum is taken over the payload, so an eager <c>"hallsOpened": []</c> on every
    /// save would change the digest of every vault ever written and hang the 📛 tampered marker on honest
    /// voyages (the #1078 byte guard caught precisely that when this section met the shore-leave line).
    /// A pre-#677 file simply lacks the field, and a captain who walked a gallery before this existed has
    /// their clock start on their next crossing, which is the harmless direction to be wrong in.</para></summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<HallOpeningRecord>? HallsOpened { get; init; }

    /// <summary>#1063 — THE GROUNDS THE NEIGHBOURS HAVE FILLED IN: the sites whose found halls have been
    /// buried, floored over and resurfaced (<see cref="Burial"/>).
    ///
    /// <para>Persisted for a harder version of <see cref="HallsOpened"/>'s own reason. That one remembers WHEN
    /// a ground was opened; this one remembers that a ground is GONE — and a burial that forgot across a
    /// reload would put a set of galleries back under a site the captain's own field book says were filled in.
    /// <b>The book never lies</b> is the feature's load-bearing law, and a world that could un-bury a ground
    /// by reloading is a world where the book is wrong and nothing on screen ever says which.</para>
    ///
    /// <para>Null until something has actually been filled in — the #1057/#1072/#1066/#677 pattern, and here
    /// for their exact reason: the checksum is taken over the payload, so an eager <c>"hallsBuried": []</c> on
    /// every save would change the digest of every vault ever written and hang the 📛 tampered marker on
    /// honest voyages. A pre-#1063 file simply lacks the field and loads with nothing buried, which is the
    /// truth about every voyage played before the neighbours started work.</para></summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? HallsBuried { get; init; }

    /// <summary>#1068 — THE GROUNDS THE WORLD HAS DECLINED ON, and the world-side window each declined in
    /// (<see cref="PoliteDecline"/>): the sites where one door that used to open no longer does, and where
    /// the scope's one-shot comes back with nothing.
    ///
    /// <para>The window is persisted and not just the id, and it has to be: the door is CHOSEN out of the
    /// floor's own candidates against that number, so a reload that forgot it would shut a DIFFERENT door —
    /// and a lock that moves between two visits is an event, which is exactly the fact about somebody
    /// deciding that #672's Scully law is spent on. Kept, the world declines once and stays declined, which
    /// is what a locked door is.</para>
    ///
    /// <para>Null until the world has declined somewhere — the #1057/#1072/#1066/#677/#1063 pattern, and here
    /// for their exact reason: the checksum is taken over the payload, so an eager <c>"hallsDeclined": []</c>
    /// on every save would change the digest of every vault ever written and hang the 📛 tampered marker on
    /// honest voyages. A pre-#1068 file simply lacks the field and loads with nothing declined.</para></summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<HallDeclineRecord>? HallsDeclined { get; init; }

    /// <summary>#1068 — THE GROUNDS THE HARBOUR HAS DONE ITS PAPERWORK ABOUT, the world-side window each was
    /// filed in, and whether the reassigned berth has been handed over yet (<see cref="QuietHands"/>): the
    /// third manifestation channel's two mundane deliveries, a berth and a price.
    ///
    /// <para>All three fields have to survive a reload. The window because both deliveries are CHOSEN
    /// against it — which slot, and which way the pump moved — and a price that had walked the other way
    /// after a reload would not be weather, it would be an event. The spent flag because the berth is handed
    /// over ONCE: a reassignment that came back every time the tab was closed would be exactly the farmable
    /// trigger #672 forbids, and the one direction it was ever open in.</para>
    ///
    /// <para>Null until the harbour has filed something — the #1057/#1072/#1066/#677/#1063/#1068 pattern,
    /// and here for their exact reason: the checksum is taken over the payload, so an eager
    /// <c>"hallsHandled": []</c> on every save would change the digest of every vault ever written and hang
    /// the 📛 tampered marker on honest voyages. A pre-#1068 file simply lacks the field and loads with
    /// nothing filed.</para></summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<QuietHandRecord>? HallsHandled { get; init; }
    /// <summary>#1074 — THE GROUNDS WHOSE DEEP WORKING THE AUTHORITY HAS CLOSED (<see cref="StopOrder"/>):
    /// the sites where the shaft under the listed bottom is sealed and an order is posted at the seal.
    ///
    /// <para>A bare list of ids and no window, unlike <see cref="HallsDeclined"/> — nothing about a stop is
    /// CHOSEN, so there is nothing for a number to keep stable. The seal stands in the one pocket the
    /// building has to spare and the plate says one sentence.</para>
    ///
    /// <para>Persisted for <see cref="HallsBuried"/>'s reason: a closure that forgot across a reload would
    /// re-open a shaft an office closed, and a captain's own field book would be the only thing in the world
    /// that still said otherwise. It also has to ride the same file as
    /// <see cref="HallsBuried"/> because the two are one trigger's two outcomes — a save that kept one and
    /// dropped the other would let a stopped ground be buried on the next descent.</para>
    ///
    /// <para>Null until something has actually been closed — the #1057/#1072/#1066/#677/#1063/#1068 pattern,
    /// and here for their exact reason: the checksum is taken over the payload, so an eager
    /// <c>"hallsStopped": []</c> on every save would change the digest of every vault ever written and hang
    /// the 📛 tampered marker on honest voyages. A pre-#1074 file simply lacks the field and loads with
    /// nothing stopped.</para></summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? HallsStopped { get; init; }

    /// <summary>#1074 beat 2 — THE GROUNDS THE AUTHORITY HAS TAKEN INTO CARE (<see cref="PreservationZone"/>):
    /// the closed workings that are now fenced, signed and permanently under study.
    ///
    /// <para>A bare list of ids and no window, for <see cref="HallsStopped"/>'s reason: nothing about a zone
    /// is CHOSEN, so there is nothing for a number to keep stable.</para>
    ///
    /// <para><b>It has to ride the same file as <see cref="HallsStopped"/></b>, and harder than that pair
    /// needed each other: a zone stands on a CLOSED working, so a save that kept the fence and dropped the
    /// order would come back to a site fenced against a shaft that was open again — the drawing and the
    /// building disagreeing about one place. And nothing ever removes an id: the study does not end, so a
    /// reload that quietly un-preserved a site would be the one mechanical fact of the beat going missing.
    /// </para>
    ///
    /// <para>Null until something has actually been fenced — the #1057/#1072/#1066/#677/#1063/#1068/#1074
    /// pattern, and here for their exact reason: the checksum is taken over the payload, so an eager
    /// <c>"hallsPreserved": []</c> on every save would change the digest of every vault ever written and
    /// hang the 📛 tampered marker on honest voyages. A pre-#1074 file simply lacks the field and loads with
    /// nothing under study.</para></summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? HallsPreserved { get; init; }

    /// <summary>
    /// #525 · <b>THE COLLAR A DECLARED OVERLOAD CLEARED</b>, or null in every voyage where nobody has turned
    /// both keys against his own hull while clamped to somebody's ring — which is almost every voyage.
    ///
    /// <para><b>It rides the file because backing the keys out does not put it back.</b> A harbour that moved
    /// two hulls off a collar on the strength of an announcement has moved them; the captain changing his
    /// mind afterwards is not information anybody down there has. A consequence a reload undid would make the
    /// whole scene free — arm at a berth, watch the room empty, call it off, pay nothing.</para>
    ///
    /// <para><b>Written only when there is something to write</b>, the #1057/#1072/#1066/#1063/#1074 law: the
    /// checksum is taken over the payload, so an eager row on every save would change the digest of every
    /// vault ever written and hang the 📛 tampered marker on an honest voyage.</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ClearedCollarRecord? CollarCleared { get; init; }

    /// <summary>
    /// #1151 · <b>HOW MANY CLAIMS THIS CAPTAIN HAS LODGED</b> — the counter the unease is measured on, and
    /// the Nebula arc's (#422) on-ramp: the file the captain is building on himself.
    ///
    /// <para>Per thread and never put back by a rebirth, because that is the whole point of it: a file does
    /// not forget you died. <see cref="NebulaClaims.TheDeskComesBack"/> reads it counting the lodging that is
    /// happening, so the second claim is the first one that comes with a memory.</para>
    ///
    /// <para><b>Written only when somebody has claimed</b>, the #1057/#1066/#1074 law: the checksum is taken
    /// over the payload, so an eager <c>"claimsLodged": 0</c> on every save would change the digest of every
    /// vault ever written and hang the 📛 tampered marker on an honest voyage.</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? ClaimsLodged { get; init; }

    /// <summary>
    /// #1151 slice 2 · <b>THE LOSS THE REP'S OFFER HAS BEEN SPENT ON</b>, or null while he has said nothing.
    ///
    /// <para><see cref="NebulaClaims.LodgeWithMe"/> is said ONCE PER LOSS — the canon's own word — and it is
    /// spent either by his saying it or by a claim being lodged against that loss at a machine, because the
    /// two hosts file the same form. It rides the file for <see cref="ClaimOwed"/>'s exact reason: a latch a
    /// reload forgot would be a salesman offering to file a hull that has already been filed, and paid for.</para>
    ///
    /// <para><b>Written only when he has spent it</b>, the #1057/#1066/#1074 law: the checksum is taken over
    /// the payload, so an eager <c>"lodgingOfferedFor": null</c> would change the digest of every vault ever
    /// written and hang the 📛 tampered marker on an honest voyage.</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? LodgingOfferedFor { get; init; }

    /// <summary>
    /// #1151 · <b>THE CLAIM THAT IS LODGED AND NOT YET PAID</b>, or null when the captain is owed nothing.
    ///
    /// <para>It rides the file for the reason the whole beat exists: the payout does not arrive at the desk,
    /// it arrives when a representative next finds you, and "next" can be a week of play and a reload away. A
    /// claim a reload forgot would be a claim the company forgot, which is the one thing this company never
    /// does.</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public LodgedClaimRecord? ClaimOwed { get; init; }

    /// <summary>
    /// #1151 · <b>THE WRIT THAT CANNOT PROCEED WITHOUT THE MASTER</b>, or null when nobody is waiting.
    ///
    /// <para>#1090's break-off used to be the end of a pursuer: the ship stopped existing, so the arithmetic
    /// of chasing her stopped working and the roster was emptied. It is not the end of the PROCESS — a
    /// collector's process demands a ship and a captain in one place, and a captain who got clear is a thing
    /// that process is still waiting for. So the pursuer comes off the sky and goes onto the file, at the
    /// port that serves the ground it happened over.</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public PendingWritRecord? WritPending { get; init; }

    /// <summary>
    /// #1063 slice 2 · <b>THE ONE SEAL THIS CAPTAIN HAS FOUND EMPTY</b> (<see cref="EmptySeal.Key"/>), or
    /// null while he still has the disappointment to spend — which is most of every voyage.
    ///
    /// <para>It rides the file for a harder version of <see cref="HallsBuried"/>'s reason. A spend a reload
    /// forgot would put the cache BACK into a room the captain's own field book says was bare, and the book
    /// being the only witness is the whole of #1063; worse, it would let him find a SECOND empty room later,
    /// and two of them is a rate. The point of the beat is that it happens once — a captain who can work out
    /// how often a seal is empty can work out what a full one proves, and that is the inference #672 exists
    /// to refuse.</para>
    ///
    /// <para><b>Written only when it has been spent</b>, the #1057/#1066/#1074 law: the checksum is taken
    /// over the payload, so an eager <c>"emptySealSpentOn": null</c> on every save would change the digest of
    /// every vault ever written and hang the 📛 tampered marker on an honest voyage. A pre-#1063 file simply
    /// lacks the field and loads with the disappointment unspent, which is the truth about every voyage
    /// played before there was one to spend.</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? EmptySealSpentOn { get; init; }
}
