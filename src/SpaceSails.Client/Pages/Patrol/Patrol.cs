using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of the patrol (#870 lane 6′b) — THE ROUND'S OWN STATE, IN ONE OBJECT. The header note lives
// in Map.Patrol.cs; this is the twenty-two the six partials used to keep loose on the page, and every
// question whose answer is a pure function of them. Nothing here reads the world: no avatar, no deck plan,
// no excursion, no clock, no renderer. What consults any of those stayed on Map, which is what makes that
// list short enough to read.
public sealed partial class Map
{
    /// <summary>
    /// #870 lane 6′b · THE PATROL'S STATE, AS ONE OBJECT.
    ///
    /// <para>Twenty-two fields were loose on the page — four times the seat's five, which is why #870's 6′
    /// ladder exists at all. 6′a (#911) stopped the rest of the client reading them raw; this is the lane
    /// that seam was cut for. They are properties on one object now, and so is every question whose answer
    /// is a pure function of them.</para>
    ///
    /// <para><b>The verbs did not come.</b> Walking a round, hailing, reading a wallet, walking a captain
    /// out — every one of those consults the avatar, the deck plan, the excursion or the card in front of
    /// the captain, so they stay on <see cref="Map"/> and write <c>_patrol.Guards</c> where they wrote a
    /// loose field. #870's 6′c is the lane that moves them, behind an explicit host surface.</para>
    ///
    /// <para><b>And it is NESTED, for the same reason <c>Seating</c> is (#904).</b> <see cref="Guard"/> —
    /// the thing this state is mostly made of — is a private nested class of the page. A top-level
    /// <c>Patrol</c> would have had to publish it to the whole client assembly on the very lane whose point
    /// is narrowing the round's surface. Nested, the moved text needs nothing it did not already have in
    /// scope, and it is still ONE object with ONE surface.</para>
    /// </summary>
    private sealed partial class Patrol
    {
        /// <summary>#870 lane 6′c · THE ONE DOOR BACK TO THE PAGE. Everything the round's verbs need from the
        /// page they are walked on is written down on <see cref="IPatrolHost"/> and reached through here —
        /// there is no second field typed as the page, no cast back to it, and no bare page member anywhere
        /// in this object. <c>ThePatrolKeepsItsOwnStateTests</c> sweeps all of it to keep that true.</summary>
        private readonly IPatrolHost _host;

        /// <summary>#870 lane 6′c · The page hands the round the page. An instance field initialiser may not
        /// name <c>this</c>, so <c>_patrol</c> is built in <see cref="Map"/>'s own constructor — the one that
        /// already builds the seat, for exactly the same language reason.</summary>
        public Patrol(IPatrolHost host) => _host = host;

        // ── THE MEN, THE FLOOR AND THE CLOCKS ─────────────────────────────────────────

        /// <summary>Everybody walking this floor on a round. The one list — a guard is a mutable body and
        /// every part of the family holds references into it, so it is filled and cleared, never
        /// replaced.</summary>
        public List<Guard> Guards { get; } = [];

        /// <summary>#831 · Everything on this floor's walls a held man could plausibly be reading — the
        /// watchclock stations first, then the shut doors' signs and the posters (<see cref="PatrolBeat.ReadablesOn"/>).
        /// Built once with the round, for the round's own reason: it is a fact about the floor, and a stepper
        /// that searched the floor plan every frame would be Lab 45's bill paid sixty times a second.</summary>
        public List<PatrolBeat.WallThing> Readables { get; } = [];

        /// <summary>The beat: the stops, in the order this watch walks them. Built once when the floor is
        /// entered so every guard on it shares ONE route and a captain can learn it — the sweep team's rule,
        /// for its reason: being hidden from has to be legible.</summary>
        public List<PatrolBeat.Stop> Beat { get; } = [];

        /// <summary>How long the captain has been on this floor. Feeds <see cref="PatrolBeat.CanBeNoticed"/>, so
        /// stepping out of the car into somebody's face is a beat rather than an instant.</summary>
        public double FloorSeconds { get; set; }

        /// <summary>How long since the boots were last mentioned. One line, cooled — a warning, not a
        /// narrator.</summary>
        public double HeardAgo { get; set; }

        // ── #833 · THE WALK BACK, AS STATE ────────────────────────────────────────────────────────────────
        //
        // Deliberately four small fields on the page rather than a class: an escort is one guard, one destination
        // and one clock, it exists only while a floor does, and the moment it needs a type of its own it will be
        // because something other than the lift is a destination — which is a ruling nobody has made.

        /// <summary>#833 · The guard walking the captain back to the car, or null. While it is set the captain's
        /// controls are HELD (<see cref="CaptainIsUnderEscort"/>) and this guard walks the escort rather than his
        /// round.</summary>
        public Guard? Escort { get; set; }

        /// <summary>#833 · The guard whose read failed and whose walk back has not started yet. The card is up in
        /// front of the captain at that moment; the walk begins when it comes down, so the player watches the
        /// walk rather than reading about it over the top of one.</summary>
        public Guard? EscortDue { get; set; }

        /// <summary>#833 · Where the escort is going: the car's own mouth, from the one placement the sim ever
        /// puts the captain through (#681). It is the END STATE of the walk rather than the start of it.</summary>
        public (double X, double Y) EscortCar { get; set; }

        /// <summary>#833 · How long the walk back has been going. Bounded by
        /// <see cref="PatrolBeat.EscortSecondsCap"/> — past which the cut is ADMITTED rather than narrated.</summary>
        public double EscortSeconds { get; set; }

        /// <summary>#833 · Whether the small talk has landed yet. Once per escort: a man who said the same thing
        /// about the pumps twice on one corridor would be a loop, not a character.</summary>
        public bool EscortSaidPumps { get; set; }

        /// <summary>#833 · Are the captain's controls being held by somebody walking them off the floor? Read by
        /// the deck's own stepper and by its key handler — the same one answer, so the keys and the legs cannot
        /// disagree about who is steering.
        ///
        /// <para>#835 · A run is deliberately NOT in here. The controls are held for the walk out and for nothing
        /// else: a captain being come after must be able to run, or rung five ("escape is possible") would be a
        /// sentence with no keys behind it.</para></summary>
        public bool CaptainIsUnderEscort => Escort is not null;

        // ── #835 · WHAT THE WATCH REMEMBERS ───────────────────────────────────────────────────────────────
        //
        // Two counters and the shift they belong to. They are on the PAGE rather than on a guard because they
        // are facts about the captain's evening, not about a man: the owner's complaint was that the same FACE
        // had been booked four times, and the fourth guard to write it down is as entitled to know as the first.
        // They survive a floor change (the floors are one site's evening) and they turn over with the watch, the
        // same clock everything else down here turns over on.

        /// <summary>#835 · Which watch <see cref="EscortsThisWatch"/> and <see cref="WalkedAwayThisWatch"/>
        /// belong to. Anything else is last night's paperwork.</summary>
        public long Watch { get; set; } = long.MinValue;

        /// <summary>#835 · How many times a round has walked the captain back to a car this watch. The number the
        /// owner's own evening produced, and the one both halves of the escalation ask
        /// (<see cref="PatrolBeat.BookedTooOften"/>).</summary>
        public int EscortsThisWatch { get; set; }

        /// <summary>#835 · How many hails the captain has simply walked away from this watch. The first is free
        /// and stays free.</summary>
        public int WalkedAwayThisWatch { get; set; }

        /// <summary>#835 · Whether the walk in progress ends at the sky rather than at the car. Decided ONCE,
        /// where the walk begins, off the same predicate the card's own sentence was composed from — so the man
        /// who said he was not pressing the button for your floor is the man who does not press it.</summary>
        public bool KickOutDue { get; set; }

        /// <summary>#835 · The escort has reached the car and the ride up is owed. Armed rather than taken, the
        /// same way <see cref="EscortDue"/> is: the ride happens on a frame of its own, outside the loop that is
        /// walking the list of guards it is about to empty.</summary>
        public bool KickOutRideDue { get; set; }

        /// <summary>#835 · How long the KICKED OUT plate stays painted on the shed wall. Counted down on the
        /// surface, where nothing else in this file runs, and one rebuild takes it away again.</summary>
        public double KickedOutPlateFor { get; set; }

        // ── #618 · A SHOT IS HEARD, AS STATE ──────────────────────────────────────────────────────────────
        //
        // Three small members and no fourth. They are here rather than on a Guard on purpose and the reason is
        // not tidiness: #906's transcript writes down EVERY field of every guard after every frame of thirteen
        // rounds, so a field on the man would move all thirteen pinned digests for cases that have no shot in
        // them at all — a re-pin the shape of a behaviour change, which is exactly the thing those digests exist
        // to make somebody look at. Whose errand it is IS a fact about the round (the escort is held the same
        // way, by reference, and for the same reason), so it lives with the round.

        /// <summary>#618 · The one man who has heard a gun go off and is walking over to where it came from, or
        /// null. He is an ordinary <see cref="Guard.WalkingUp"/> while it lasts — the walk to a bang and the walk
        /// to a person are the same man doing the same walk — and this reference is the whole of the difference:
        /// it says where he is walking TO.</summary>
        public Guard? LookingIntoIt { get; set; }

        /// <summary>#618 · Where the round it came from landed, off the shot's own record
        /// (<see cref="GunfireHeard.Shot"/>) and never re-derived. It is the thing on the floor with a hole in
        /// it, which is what a man walking over is walking over to look at.</summary>
        public (double X, double Y) TheNoise { get; set; }

        /// <summary>#618 · How many shots on this ground the round has already answered — a cursor into the
        /// excursion's own append-only ledger, so one bang is answered once and a floor cannot be made to walk
        /// its backlog.
        ///
        /// <para>It is brought UP to the ledger's length when a floor is entered rather than zeroed, and that is
        /// the whole of "a shot on another floor is not heard here": the men on B5 are not owed an errand about
        /// a door on B3, and the shot's own coordinates are floor coordinates that would happily be in range of
        /// somebody two levels down.</para></summary>
        public int ShotsAnswered { get; set; }

        // ── #836 · THE FLETCH WALLET, AS STATE ────────────────────────────────────────────────────────────
        //
        // Owner, evening playtest 2026-08-11: "I think I should be able to pick the badge I show the guard...
        // like Fletch ... suppose we have 4 different ID's ... one of them real".
        //
        // Three fields, and the division between them is the whole of the feature: what is IN YOUR HAND (decided
        // during the approach, spent at the read), whether the fan is OPEN in front of you, and what the book
        // remembers about every paper you have ever handed anybody. The first two die with the floor; the third
        // is durable, because it is the only thing a chooser row's hint is ever derived from.

        /// <summary>#836 · The paper that goes into his hand when he arrives. Seeded at the hail from
        /// <see cref="WalletChoice.DefaultFor"/> — last shown at this site, else the real one — so a captain who
        /// ignores the fan entirely still hands over the paper a reasonable person would already be holding.</summary>
        public Satchel.Item? PaperInHand { get; set; }

        /// <summary>#836 · Is the fan up? Only ever while somebody is walking over
        /// (<see cref="WalletChoice.Fans"/> said there was a choice to make), and it comes down the moment he is
        /// at arm's length: no swapping papers in front of a guard.</summary>
        public bool WalletFanOpen { get; set; }

        /// <summary>#836 · THE CAPTAIN'S OWN PAPER TRAIL — one row per read, naming which identity was shown and
        /// how it went. Durable (it rides the vault), because <i>worked here, twice</i> is worth nothing if it
        /// forgets between excursions, and because the owner's own reading of it is the horror: <i>two names on
        /// one face across one watch is the facility's own case against you</i>, pointed back at the captain.</summary>
        public List<WalletChoice.Shown> ShownBook { get; } = [];

        // ── #821 · THE HIDE'S ONE LINE ─────────────────────────────────────────────────

        /// <summary>#821 · Whether the round has already been heard going past this hide. One line per shut
        /// door: it is the reward for having got in unseen, and a sentence repeated every time a man crosses the
        /// room would be a narrator rather than a beat.</summary>
        public bool WalkedPastSaid { get; set; }

        /// <summary>Dev cheat: <c>?patrol=N</c> forces N rounds onto whatever restricted floor you boot onto,
        /// so the scene is reachable without waiting for a watch that rolled two.</summary>
        public int? RoundsCheat { get; set; }

        /// <summary>Dev cheat: <c>?badge=1</c> mints this site's own pass at the landing, so the satisfied arm
        /// of the challenge is reachable without working the whole cage-crew lane first.</summary>
        public bool BadgeCheat { get; set; }

        // ── #870 lane 6′b · AND EVERY QUESTION WHOSE ANSWER IS ONE OF THE ABOVE ──────────────────
        //
        // Fifteen members, moved here whole from the six partials. The law that picked them is mechanical,
        // and it is the seat lane's own: a member is here IF AND ONLY IF every name in its body that is not
        // a local or a parameter is one of the twenty-two or a Core static, AND no type in its signature is
        // anything but Core's or this object's own. Everything that also reads the WORLD — the avatar, the
        // satchel, the excursion, the pen — stayed on Map, and the PR body names each of those with the
        // state it also consults. Map keeps a one-line forwarder for every one of the fifteen, because all
        // fifteen are already asked for by name from outside the family; 6′c is the lane that deletes that
        // block and moves the callers.

        /// <summary>#870 lane 6′a · EVERYBODY WALKING THIS FLOOR ON A ROUND — for the two instruments that must
        /// see them and are not part of this family: the park bench's tail-check (<c>Map.Bench.cs</c>, which
        /// stamps each of them a mover on a PUBLISHED round and therefore never a tail) and the motion fan's
        /// contact list (<c>Map.SweepTeam.cs</c>, which owes the captain a blip for a man who walks).
        ///
        /// <para>A read-only view of the one list and never a copy: both callers read positions and velocities
        /// that change every frame, and a cached copy of where everybody was is exactly the disagreement between
        /// the panel and the sim this codebase does not allow.</para></summary>
        public IReadOnlyList<Guard> TheRoundOnFoot => Guards;

        /// <summary>#870 lane 6′a · <c>?patrol=N</c> asked for N rounds on the floor it boots onto. Written by
        /// the one pass that reads the query (<c>Map.Sim.World.cs</c>) and spent by
        /// <see cref="SpawnPatrolFor"/>.</summary>
        public void ForceTheRoundsTo(int rounds) => RoundsCheat = rounds;

        /// <summary>#870 lane 6′a · Has the query already forced a round? Asked twice in the same boot pass
        /// (<c>Map.Sim.World.cs</c>): once by <c>?patrol=</c> to decide whether to imply the whole route, and
        /// once by <c>?badge=</c>, which needs a round to show its pass to and must not overwrite the count a
        /// <c>?patrol=2</c> on the same URL has already asked for.</summary>
        public bool TheQueryHasForcedARound => RoundsCheat is not null;

        /// <summary>#870 lane 6′a · …and a round to show a pass to, unless one has already been asked for.
        /// <c>?badge=1</c> implies <c>?patrol=1</c> — a pass with nobody to show it to is not a thing anybody
        /// can test — but a <c>?patrol=2</c> on the same URL wins, because overwriting it would quietly hand a
        /// tester the easier scene they did not ask for.</summary>
        public void ForceARoundIfNoneAsked() => RoundsCheat ??= 1;

        /// <summary>#870 lane 6′a · <c>?badge=1</c> — mint this site's own pass into the wallet at the landing.
        /// Written by the boot pass (<c>Map.Sim.World.cs</c>); the minting itself happens on the surface, where
        /// the site is known.</summary>
        public void MintTheSitePassAtTheLanding() => BadgeCheat = true;

        /// <summary>#870 lane 6′a · …and whether it was asked for, read by the landing that does the minting
        /// (<c>Map.Surface.Cheats.cs</c>). Deliberately NOT "does the captain hold a pass" — that is
        /// <see cref="PatrolBeat.BadgeHeld"/>, and a captain who earned one holds it with no query at
        /// all.</summary>
        public bool TheSitePassIsMintedAtTheLanding => BadgeCheat;

        /// <summary>#870 lane 6′a · THE NEXT HIDE GETS ITS OWN LINE. Told by the door coming off the catch
        /// (<c>Map.Cubicle.cs</c>'s <c>OpenTheCubicle</c>), which is the one moment a hide is over: one shut
        /// door is one sentence, and a captain who goes back in has earned hearing it again.
        ///
        /// <para>A verb rather than a setter the caller could spell the other way round. ARMING it — saying the
        /// line has already been said, for a hide that has not started — would quietly cost a player the one
        /// beat that tells them the plate did nothing for them, and nothing would report it.</para></summary>
        public void TheNextHideGetsItsOwnLine() => WalkedPastSaid = false;

        /// <summary>
        /// #870 lane 6′a · …AND EVERYBODY FORGETS IT AGAIN, which is the other half of the same ruling and
        /// belongs beside it. Asked for by <c>Map.Cubicle.cs</c>'s <c>OpenTheCubicle</c>; hands back the FIRST
        /// man who was standing there knocking, or null.
        ///
        /// <para>Both halves matter and both are why this is one member rather than two. The forgetting is swept
        /// over the whole list rather than stopped at the man who knocked, because a second guard who also
        /// watched the catch go over would otherwise keep the bit and be standing outside the NEXT cubicle the
        /// captain shut, having seen nothing at all — a hide that stopped working for reasons the player cannot
        /// read. And only ONE of them is handed back, because two men doing one job is #777's stacked card.</para>
        ///
        /// <para>What the caller does with him is the caller's: the door is what raises the challenge, and there
        /// is no road to a card in this file.</para>
        /// </summary>
        public Guard? EverybodyForgetsTheCatch()
        {
            Guard? waiting = null;
            foreach (Guard g in Guards)
            {
                waiting ??= g.Knocking ? g : null;
                g.HeForgetsTheCatch();
            }
            return waiting;
        }

        /// <summary>#870 lane 6′a · IS THIS THE PAPER ALREADY IN YOUR HAND? One row of the fan, asked by the
        /// dialog that draws it (<c>Map.razor</c>) to mark the picked row and to say so on its face.
        ///
        /// <para>Compared by KIND AND ID, never by reference: the fan is rebuilt from the satchel every time it
        /// is asked (<see cref="TheWalletFan"/>), so the row's item and the held item are equal papers and not
        /// the same object.</para></summary>
        public bool ThePaperInYourHandIs(Satchel.Item paper) =>
            PaperInHand is { } held && held.Kind == paper.Kind && held.Id == paper.Id;

        /// <summary>#870 lane 6′a · WHAT THE BOOK SAYS ABOUT HANDING THIS PAPER OVER HERE — <i>worked here,
        /// twice</i> — for the hint under a fan row (<c>Map.razor</c>). The sentence is Core's
        /// (<see cref="WalletChoice.HistoryLine"/>); what this adds is that the book it is read out of is the
        /// captain's own and nobody else has to hold it to ask.</summary>
        public string TheBookOn(Satchel.Item paper, string bodyId) =>
            WalletChoice.HistoryLine(paper, bodyId, ShownBook);

        /// <summary>#870 lane 6′a · THE CAPTAIN'S OWN PAPER TRAIL, read-only — for the vault
        /// (<c>Map.Vault.cs</c>), which writes every row's opaque <c>Stored</c> string into the save. A view of
        /// the one book and never a copy.</summary>
        public IReadOnlyList<WalletChoice.Shown> YourPaperTrail => ShownBook;

        /// <summary>#870 lane 6′a · …and emptying it before a load pours the saved rows back in
        /// (<c>Map.Vault.cs</c>). The book is <c>readonly</c> and outlives every excursion, so a load REFILLS it
        /// rather than replacing it; without this a captain would carry the last game's paperwork into the
        /// next.</summary>
        public void ForgetThePaperTrail() => ShownBook.Clear();

        /// <summary>#870 lane 6′a · …and one row back in, exactly as it was filed (<c>Map.Vault.cs</c>).
        ///
        /// <para>Deliberately NOT <see cref="FileTheNameYouGave"/>, and not <see cref="WalletChoice.Remember"/>
        /// either: filing is what happens when a guard READS a paper — it composes an outcome, folds the row
        /// into the book under Core's own rules and writes a line in the field book. A load is not a read. It
        /// puts back rows that were already filed, in the order the save has them, and a captain who reloads
        /// must not find a fresh note about a challenge that happened last week.</para></summary>
        public void RestoreAPaperTrailRow(WalletChoice.Shown row) => ShownBook.Add(row);

        /// <summary>#836 · Shutting the fan without touching it. Whatever was already in the hand stays in it —
        /// no arm of this dialog can leave a captain holding nothing they did not choose to hold.</summary>
        public void CloseTheWalletFan() => WalletFanOpen = false;
    }
}
