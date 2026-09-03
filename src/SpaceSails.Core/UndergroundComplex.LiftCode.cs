using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    /// <summary>
    /// #602 · <b>THE PAD BESIDE THE LIFT PANEL, AND THE STICKER THAT TELLS YOU WHAT IT COSTS.</b>
    ///
    /// <para>Owner, 2026-08-02: <i>"the numpad idea is to even have like a vicious security sticked warning
    /// next to it and it gives you 3 tries before calling security"</i> — and, the same day, the shape of the
    /// counter: <i>"the alert resets if you just walk away… I bet their employees try their luck all the time
    /// … but if you repeat the try soon after before the reset then the security patrol comes"</i>.</para>
    ///
    /// <para><b>WHY A PAD IS ALLOWED HERE AT ALL,</b> when #590's call 3 refused one. A lock you can ATTEMPT
    /// turns a wall into a puzzle, and a building full of attemptable walls is a lock hunt rather than a
    /// place. The sticker is what removes the puzzle: the building has stated the stake before the first
    /// press, so entering a guess is not problem-solving, it is gambling with a price you were told. And
    /// three tries is uncrackable BY CONSTRUCTION — nobody enumerates ten thousand codes three at a time — so
    /// the code can only ever come from having FOUND it, which is the condition #602 set for allowing this.
    /// The full overrule is written up at call 3 in <c>UndergroundComplex.AuthorityCard.cs</c>.</para>
    ///
    /// <para><b>THE TWO RULES HOLD EACH OTHER UP, and this is the comment the owner asked for at the
    /// constant.</b> The code being FINDABLE ONLY is what makes <see cref="WindowSeconds"/> harmless —
    /// unlimited windowed attempts against a number you cannot reason about is still zero progress. And the
    /// window is what makes the sticker fair rather than punitive, because a building whose staff idly try
    /// the pad in passing cannot summon a patrol every third lifetime attempt. <b>If anyone ever makes
    /// <see cref="CodeFor"/> deducible, the reset becomes an exploit and this whole file has to be re-argued
    /// from the top.</b></para>
    ///
    /// <para><b>WHAT THE PAD DOES NOT TOUCH.</b> A SECTOR door has no reader and never gets one (#590 call
    /// 2), and neither does a stop order's welded leaf (<see cref="Signs.HasNoReader"/>) — a pad is bolted
    /// beside a LOCK, and those are not locks. This pad lives on a row the panel already draws and already
    /// refuses, and on exactly one of them per building (<see cref="PadBand"/>).</para>
    /// </summary>
    public static class LiftCode
    {
        // ── THE ONE GATE THAT HAS A PAD ON IT ─────────────────────────────────────────────────────────────

        /// <summary>
        /// The band the pad's gate leads INTO — the first one, the gate off the floors the day crew works,
        /// the same gate the day-labour chit opens (#752).
        ///
        /// <para><b>ONE PAD PER BUILDING, and it is a fiction rather than a scope cut.</b> Owner's own note
        /// on who these locks are for: <i>"a lab boss would like their special lair access to be limited off
        /// from their honest criminal worker scientists"</i>. A pad exists where STAFF have to move — the
        /// working floors, where a code gets written on a bit of paper because thirty people need it — and it
        /// stops existing the moment you are past them. Everything deeper stays exactly the card-only gate
        /// #590 built, so the deeper you go the less the building negotiates, which is the sentence the whole
        /// descent is trying to say.</para>
        ///
        /// <para>It also keeps the pad honest about its own paper. The code is seeded ONCE per site and
        /// written down ONCE per site (<see cref="PaperRoomFor"/>), so every pad in the game has a findable
        /// answer. A pad on a gate with no paper anywhere would be a lock that can only ever be gambled at,
        /// which is the "puzzle with a punishment attached" #602's issue body names as worse than either.</para>
        /// </summary>
        public const int PadBand = 1;

        /// <summary>#602 · Which room of this site the code paper is left in, or null where the site has no
        /// pad to open.
        ///
        /// <para>The works floor — <see cref="TopPressurisedFloor"/>, the floor with the plant and the
        /// canteen on it — because that is where the people who need the code actually are. <b>NOT ROOM
        /// 0</b>, which is the standing rule for a seeded paper (see <c>MoneyTrailRoomFor</c>): a paper in
        /// room 0 is a paper the first search on the floor is guaranteed to turn up, and a find you cannot
        /// miss is not a find. Room 0 of this floor is #1063's maintenance ledger and rooms 1–3 are #1074's
        /// cost-centre line items, so this takes the next one along and cannot collide with any of them.</para>
        ///
        /// <para>Null at the head office, which has no gate on any floor (#411), and null on a site with no
        /// second band, where there is nothing for a code to open.</para></summary>
        public static (int Level, int RoomIndex)? PaperRoomFor(string bodyId)
        {
            ArgumentNullException.ThrowIfNull(bodyId);
            return !IsHeadOffice(bodyId) && SiteHasBand(bodyId, PadBand)
                   && TopPressurisedFloor(bodyId) is { } works
                ? (works, PaperRoom)
                : null;
        }

        /// <summary>The room index <see cref="PaperRoomFor"/> designates. Named rather than typed at the
        /// call site, for the reason every other designated room is named.</summary>
        public const int PaperRoom = 4;

        // ── THE CODE ──────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// #602 · The four digits this site's pad answers to.
        ///
        /// <para><b>SEEDED, AND THEREFORE FOUND RATHER THAN DERIVED.</b> There is no pattern in it, nothing
        /// on any plate near the door hints at it, and the only place it is written is the paper in
        /// <see cref="PaperRoomFor"/>'s room. That is not a nicety — it is the load-bearing half of the
        /// argument at the top of this class. Read the note on <see cref="WindowSeconds"/> before changing
        /// anything about this function.</para></summary>
        public static string CodeFor(string bodyId)
        {
            ArgumentNullException.ThrowIfNull(bodyId);
            return (1000 + (int)(DiceRule.Seed($"hive:liftcode:{bodyId}") % 9000))
                .ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Does this entry open this site's pad? One question, asked by the pad and by the guard
        /// that proves the paper and the pad agree — a second spelling of "is this the code" is the thing
        /// that drifts.</summary>
        public static bool Answers(string bodyId, string? entry)
        {
            ArgumentNullException.ThrowIfNull(bodyId);
            return entry is not null && string.Equals(entry, CodeFor(bodyId), StringComparison.Ordinal);
        }

        // ── WHAT IS WRITTEN, VERBATIM ─────────────────────────────────────────────────────────────────────

        /// <summary>#602 · <b>THE STICKER.</b> Canon, verbatim, and the reason a pad is allowed to exist at
        /// all: the affordance states its own cost, before the first press, every time. It is a plate — the
        /// building shouting at its own staff — and it is readable on the panel whether or not the captain
        /// has ever found a code.</summary>
        public const string Sticker = "THREE WRONG ENTRIES CALL SECURITY. THE PAD REMEMBERS.";

        /// <summary>#602 · The pad's answer to a right code. A plate, not a sentence: a lock does not
        /// narrate.</summary>
        public const string OpenPlate = "OPEN";

        /// <summary>#602 · The first miss inside the window. It COUNTS ALOUD, which is the owner's second
        /// requirement — a stake nobody can see is not a stake.</summary>
        public const string WrongOnePlate = "WRONG · 1";

        /// <summary>#602 · The second. The third press is now a real moment, which is the whole of what this
        /// plate is for.</summary>
        public const string WrongTwoPlate = "WRONG · 2";

        /// <summary>#602 · The third, and the pad says the one thing it promised on the sticker. Nothing else
        /// is said anywhere — no banner, no line explaining what is coming, and nothing at all when it
        /// arrives (§13.8, and #618's own discipline about the man who simply walks over).</summary>
        public const string SecurityCalledPlate = "SECURITY CALLED";

        /// <summary>#602 · <b>THE CODE PAPER,</b> canon verbatim, with the number this site actually uses in
        /// the place the sentence keeps for it. The sentence is fixed and only the four digits move.
        ///
        /// <para><i>Do not write this down</i> is the paper's own joke and the reason it is worth finding: it
        /// is somebody's handwriting, on the works floor, ignoring the instruction it is repeating.</para></summary>
        public static string PaperLine(string bodyId)
        {
            ArgumentNullException.ThrowIfNull(bodyId);
            return $"Lift code, lower band: {CodeFor(bodyId)}. Do not write this down.";
        }

        /// <summary>#602 · What the sheet is called in a pocket. The paper's own first clause, verbatim off
        /// <see cref="PaperLine"/> and without the digits — a satchel row that shouted the code would put the
        /// answer in the inventory, where the captain never had to find it.</summary>
        // FABLE: line needed — a pocket title for the code paper, if this verbatim fragment is not wanted.
        public const string PaperTitle = "Lift code, lower band";

        /// <summary>#602 · Is this find the code paper, and whose site's? Asked of the id, so the readers
        /// that meet a paper away from its room (<c>FieldClue.Title</c>, <c>FieldClue.Document</c>) never
        /// have to take a room apart to recognise one — the discipline <c>AuthoredPaperOf</c> keeps.</summary>
        public static string? PaperIn(string? findId)
        {
            if (findId is null || RoomOfFind(findId) is not { } at)
            {
                return null;
            }
            return PaperRoomFor(at.BodyId) is { } kept
                   && at.Level == kept.Level && at.RoomIndex == kept.RoomIndex
                ? at.BodyId
                : null;
        }

        // ── THE WINDOW ────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// #602 · <b>HOW LONG THE PAD REMEMBERS.</b> Ninety seconds from the FIRST miss, and misses outside
        /// it never happened.
        ///
        /// <para>Owner's ruling, and the fiction is what makes it obviously right: a building whose staff try
        /// their luck in passing cannot call a patrol every third lifetime attempt. It is calibrated for its
        /// own population — it tolerates the curious and reacts to the persistent. One try on your way past
        /// is a bored technician; three in ninety seconds is somebody working the problem.</para>
        ///
        /// <para><b>TIME ONLY, AND DISTANCE NEVER.</b> His phrasing had both (<i>walk away</i> and <i>before
        /// the reset</i>); this takes the clock alone, because a building that tracked how far you wandered
        /// would be cleverer than this building has any business being, and a clock is the one thing a player
        /// can feel without being told.</para>
        ///
        /// <para><b>DO NOT TUNE THIS WITHOUT READING <see cref="CodeFor"/>.</b> A resettable counter looks
        /// farmable and is not, for one reason only: the code is found and never derived. The day somebody
        /// makes it deducible, this constant becomes an exploit.</para></summary>
        public const double WindowSeconds = 90.0;

        /// <summary>#602 · What the sticker promises. Three, because three is small enough that nobody
        /// enumerates a keypad with it.</summary>
        public const int TriesBeforeSecurity = 3;

        /// <summary>
        /// #602 · <b>WHAT THE PAD REMEMBERS,</b> and the whole of it: when the run of misses started, and how
        /// many are standing in it.
        ///
        /// <para>ONE clock, not two. The third miss restarts it (see <see cref="AWrongCode"/>), so the dark
        /// runs for a window from the moment security was called and the count is zero on the far side of it
        /// — which means "how many misses stand" and "is the pad dark" are the same arithmetic asked twice
        /// rather than two states that can disagree.</para></summary>
        public readonly record struct Pad(double FirstMissAt, int Misses)
        {
            /// <summary>A pad nobody has touched. <see cref="double.NegativeInfinity"/> rather than zero:
            /// zero is a real instant on an excursion clock, and a pad that started the excursion one miss
            /// in would be a building remembering something that never happened.</summary>
            public static Pad Fresh => new(double.NegativeInfinity, 0);
        }

        /// <summary>#602 · Has the window closed? Everything else here is written in terms of this, so the
        /// forgetting happens in exactly one place.</summary>
        public static bool Forgotten(Pad pad, double now) => now - pad.FirstMissAt >= WindowSeconds;

        /// <summary>#602 · How many misses stand against the captain AT THIS INSTANT. Zero once the window
        /// has closed, whatever the stored count says — a bored technician's try in passing is not held
        /// against him, and the stored number is never read without this question in front of it.</summary>
        public static int MissesAt(Pad pad, double now) => Forgotten(pad, now) ? 0 : pad.Misses;

        /// <summary>
        /// #602 · A WRONG CODE WENT IN. The window's whole arithmetic, in one function.
        ///
        /// <para>A miss with nothing standing opens a fresh window at <paramref name="now"/>. A miss inside
        /// one adds to it. And the miss that reaches <see cref="TriesBeforeSecurity"/> <b>restarts the
        /// clock</b>, which is what gives the pad its dark: security was called at this instant, so the pad
        /// is out for a window from this instant, and when that window closes the count goes to zero with it.
        /// </para></summary>
        public static Pad AWrongCode(Pad pad, double now)
        {
            int standing = MissesAt(pad, now) + 1;
            return standing >= TriesBeforeSecurity ? new(now, TriesBeforeSecurity)
                : standing <= 1 ? new(now, 1)
                : pad with { Misses = standing };
        }

        /// <summary>#602 · Did that miss call security? Asked of the pad AFTER <see cref="AWrongCode"/> has
        /// been applied — the one question the summons hangs off.</summary>
        public static bool SecurityIsCalled(Pad pad, double now) =>
            MissesAt(pad, now) >= TriesBeforeSecurity;

        /// <summary>#602 · Is the pad dark? The same question as <see cref="SecurityIsCalled"/> and
        /// deliberately the same arithmetic: the pad goes out when it calls and comes back when it forgets.
        /// Named separately because the UI asks a different thing of it than the summons does, and a razor
        /// that asked "has security been called" to decide whether to draw keys would be the client deciding
        /// what a state MEANS.</summary>
        public static bool IsDark(Pad pad, double now) => SecurityIsCalled(pad, now);

        /// <summary>#602 · What the pad says, or null before anything has been pressed and once the window
        /// has closed on it. The four plates and no fifth — a pad that grew a sentence would be a lock
        /// narrating.</summary>
        public static string? PlateFor(Pad pad, double now) => MissesAt(pad, now) switch
        {
            1 => WrongOnePlate,
            2 => WrongTwoPlate,
            >= TriesBeforeSecurity => SecurityCalledPlate,
            _ => null,
        };
    }
}
