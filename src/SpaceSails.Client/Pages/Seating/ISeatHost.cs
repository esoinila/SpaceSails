using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #870 lane 6c — WHAT A CHAIR NEEDS FROM THE PAGE, WRITTEN DOWN.
///
/// <para>6a made the rest of the client stop reading the seat's five fields raw. 6b gathered the five and
/// every question that is a pure function of them onto <c>Map.Seating</c>. This lane moves the VERBS — sitting
/// down on six kinds of furniture, getting off them, the wait beat, the moves at a top and at a counter — onto
/// the same object, and <b>this interface is the whole of what they still reach back to the page for</b>.</para>
///
/// <h3>The interface IS the finding</h3>
///
/// <para>The seat family used to be seven partials of <see cref="Map"/>, which meant it could reach anything
/// the page had: every field, every private verb, every dev cheat, with nothing written down and nothing to
/// argue with. Four issues in four days (#847/#865/#868/#881) landed in it, and each crew had to read all
/// seven files to know what one keypress did. <b>The coupling was real and it was invisible.</b> It is
/// twenty-eight members, and now it is a list.</para>
///
/// <para><b>It may only shrink.</b> <c>TheSeatKeepsItsOwnStateTests</c> asserts the count and writes it into
/// its own failure message — a ratchet, exactly like #870's size gate: taking a member off is a good day and
/// the number comes down with it; adding one is a lane of its own, argued for in a PR body, because it is the
/// chair asking the page for something new.</para>
///
/// <h3>What is deliberately NOT here</h3>
///
/// <para><b>The renderer's cue.</b> <c>RendererInterop.PlayCue</c> looks like a page service and is not one:
/// it is an <c>internal static</c> of this assembly, so every moved <c>PlayCue("reveal")</c> travels
/// character-for-character and the chair does not owe the page a sound.</para>
///
/// <para><b>Everything a seat is a GATE on rather than a part of.</b> The field book's spread register, the
/// pour window's arithmetic, the short rest's ledger, the tail reading's guard sweep, the cubicle's catch and
/// the sit-stand desk all stayed on <see cref="Map"/> — a seat is where you do those, not what they are. Where
/// a moved verb genuinely needs one of them it asks for the ANSWER (<see cref="APourInFrontOfYou"/>,
/// <see cref="RestOneSeatedBeat"/>, <see cref="TheTailReading"/>, <see cref="CubicleIsShut"/>) and never for
/// the machinery under it, which is why those four are one member each instead of the eight fields and three
/// sweeps they are made of.</para>
///
/// <para><b>Anything with an opinion about a seat.</b> Nothing on here decides where the captain may sit, what
/// a seat is worth or what one costs to leave. Every one of these is a thing the PAGE owns and the chair
/// borrows: a coordinate, a purse, a sentence, a re-render.</para>
///
/// <h3>Why it is nested, like <c>Seating</c> before it</h3>
///
/// <para><see cref="SurfaceExcursion"/> is a <c>private sealed</c> nested type of <see cref="Map"/>, and three
/// members here name it — a seat is a thing in a building and there is no building without a landing. A
/// top-level interface would have had to widen the excursion to <c>internal</c>, publishing the whole of a
/// landing to the client assembly on the lane whose point is narrowing what one chair can reach. So the
/// interface lives inside the page exactly as the seat does, and <see cref="Map"/> implements it by name:
/// <c>public partial class Map : Map.ISeatHost</c>. Nothing about the surface changes with where it is
/// declared, and the only thing that can see it is the family it was written for.</para>
/// </summary>
public partial class Map
{
    /// <inheritdoc cref="ISeatHost"/>
    private interface ISeatHost
    {
        // ── WHERE THE CAPTAIN IS, AND WHAT THEY ARE STANDING ON ───────────────────────────────────────────

        /// <summary>The excursion under way, or null on the ship. Every verb in the family opens with it: a seat
        /// is a thing in a building, and there is no building without a landing.</summary>
        SurfaceExcursion? Surface { get; }

        /// <summary>Where the body is. Read by every [E] verb — to find the console the press landed on, to pick
        /// the nearest free chair (<c>ChairYouTake</c>), to pick the end of the plank you walked up to.</summary>
        double AvatarX { get; }

        /// <inheritdoc cref="AvatarX"/>
        double AvatarY { get; }

        /// <summary>The deck the captain is walking on, for one question and one only:
        /// <c>NearestConsoleSpot</c> — which fixture was pressed. Every seat verb starts there and then matches
        /// the answer back to Core's own published furniture.</summary>
        DeckPlan DeckPlan { get; }

        // ── WHAT THE CAPTAIN HAS ON THEM ──────────────────────────────────────────────────────────────────

        /// <summary>The purse. A round at a table and a pour at a counter are the only two things a seat spends,
        /// and both debit before the answer, because the answer is the glass arriving.</summary>
        int Credits { get; set; }

        /// <summary>The sleeve. Read to know which moves are on offer and what may be put on the table; written
        /// by the one move that grants a chit.</summary>
        List<Core.Satchel.Item> Satchel { get; set; }

        /// <summary>The nerve gauge, as a number — one read, by <c>TableSituation</c>, because
        /// <see cref="Encounter.NerveReadsAcrossATable"/> says a table can see it on you.</summary>
        double Nerve { get; }

        // ── THE COUNTER'S OWN CARD, WHICH THE STOOL IS A POSTURE OF ───────────────────────────────────────

        /// <summary>The open barkeep card, or null when it is shut. #756's whole grammar is that a stool is not a
        /// second fixture but a posture of this one, so the stool's verbs have to be able to see it.</summary>
        Core.Interior.Barkeep? BarMenu { get; }

        /// <summary>…and the slot on it the keep speaks into. Write-only on purpose: the stool SAYS things there
        /// and never reads back what it said, and #736 was fought over exactly one card owning one notice.</summary>
        string? BarNotice { set; }

        // ── PUTTING THE BODY SOMEWHERE ────────────────────────────────────────────────────────────────────

        /// <summary>#820's snap. The ONE placement every seat kind in the game sits a captain with — it also arms
        /// the sit beat, which is why no verb here has to remember to.</summary>
        void SitCaptainOn(double x, double y);

        /// <summary>…and the step-off, which is a walk and gets the nudge's opinion on whether the room agrees.
        /// Used by the one teardown (<c>CloseTable</c>) and by the park's dev row.</summary>
        void StandCaptainAt(double x, double y, string where);

        // ── SAYING SOMETHING, AND DRAWING IT ──────────────────────────────────────────────────────────────

        /// <summary>The pulse HUD. Deliberately rare in this family (#680: never under a backdrop) — a full top's
        /// refusal, the goodbye said after the panel has gone, "off you go", and the dev rows.</summary>
        void ShowPulseMessage(string message);

        /// <summary>The page re-renders. A seat is a thing you can SEE, so every verb that changes one ends
        /// here.</summary>
        void StateHasChanged();

        /// <summary>The way home for a MOUSE press that shut the panel it was on — otherwise focus is left on a
        /// button that has stopped existing and the deck goes deaf. Found by playing it, twice.</summary>
        Task RefocusMap();

        /// <summary>The field book. The one rung at a counter and the one note a table's answer carries are
        /// written here rather than pulsed, because the book must remember what the pulse never said (#686).</summary>
        void FileNote(string text, string glyph);

        /// <summary>Mark the vault dirty. Anything a seat did that outlives standing up asks for this.</summary>
        void RequestVaultSave();

        // ── THE SYSTEMS A SEAT SPENDS THROUGH, ASKED FOR THEIR ANSWER ─────────────────────────────────────

        /// <summary>#696's hold, ended out loud with nothing filed. Three seats end privacy: standing up, somebody
        /// taking the chair opposite, somebody taking the other end of a plank.</summary>
        void AbandonProcessing(SurfaceExcursion ex, Core.Processing.Interruption why);

        /// <summary>The nerve, moved by a table going wrong — through the ordinary gauge and never anonymously
        /// (#480), which is why the label is a parameter rather than a default.</summary>
        void ApplyNerveShock(double rawAmount, string label);

        /// <summary>Is there a bought pour in front of the captain? #784's ONE reading of #756's counter, asked
        /// rather than re-measured — a second window here would let a panel say "cold glass" on a beat the rest
        /// engine had already called dry.</summary>
        bool APourInFrontOfYou { get; }

        /// <summary>…and how much of it is left, for the customer line's own figure. Same member the mechanic runs
        /// on, so the strip and the rest engine cannot come to two answers about one glass.</summary>
        double? PourSecondsLeft { get; }

        /// <summary>One seated watch-beat of short rest, and the body's footnote to it. The wait beat IS the
        /// rest (#784), so the arithmetic and its per-watch ceiling stay where the nerve and condition systems
        /// are and the seat asks for the sentence.</summary>
        string? RestOneSeatedBeat(SurfaceExcursion ex, int beatsAlreadySat);

        /// <summary>#793 · What sitting still on a bench showed you — one sentence, never a meter. The sweep under
        /// it reads every guard on the floor through the same sight blockers their markers are drawn with, which
        /// is patrol machinery and not seat machinery.</summary>
        string TheTailReading();

        /// <summary>#821 · Is this cell's catch over right now? Asked on every frame by the exposure ladder, of
        /// the one set the deck is rebuilt from — the catch can go over while the captain is already sitting on
        /// the pan.</summary>
        bool CubicleIsShut(string key);

        /// <summary>
        /// #731 · SEND SOMEBODY ACROSS THE ROOM TO THIS TABLE, on real legs, out of a door that does not open
        /// for the captain.
        ///
        /// <para>Owner, 2026-08-06: <i>"In the space bars there are lot of cases where we can have npcs arrive
        /// at bar from locked place or go to a locked place. Now it is possible to have NPC ask to sit down at
        /// our table and offer a quest! This is the classic TTRPG event."</i> The walk IS the ceremony — no
        /// banner and no blip; the room announces her by parting for her — so the arrival is a route planned
        /// over the captain's own lattice and stepped by the captain's own collision, and the card comes up
        /// when she gets there rather than on the frame the beat was spent.</para>
        ///
        /// <para><b>False means she could not be walked</b> — no locked door on this floor, no standing place
        /// at one, or no way through — and the caller must then do the arrival the way it has always been
        /// done. A refusal here is never a reason to place a body at the far end of a walk that could not be
        /// walked.</para>
        /// </summary>
        bool WalkSomebodyToYourTable(SurfaceExcursion ex, int tableIndex);

        /// <summary>
        /// #731 · …AND THE OTHER HALF OF THE SENTENCE: she is finished, so she goes, and she goes back through
        /// the door she came out of.
        ///
        /// <para>Owner, 2026-08-06: <i>"If they go behind a door that is locked to us, we use that as 'I guess
        /// that concludes the conversation' point in the plot / situation."</i> This is the TRIGGERED half of
        /// the issue's own proposal — <i>scheduled for ambience, triggered for plot beats; both through one
        /// walker</i> — and the trigger is the scene ending. Nothing is said about it: the panel goes back to
        /// being the captain's own table, and the room delivers the full stop by walking her out of it.</para>
        ///
        /// <para>The door is <see cref="Core.Interior.Egress.ArrivalDoor"/>'s, the SAME one her provenance was
        /// dealt from this watch, so she does not walk out of the building through a door she was never behind.
        /// False when she cannot be walked — no locked door, no standing place, no way through — and the
        /// caller does exactly what it always did, which is nothing.</para>
        /// </summary>
        bool WalkTheVisitorOut(SurfaceExcursion ex, int tableIndex);

        // ── THE DEV ROWS ──────────────────────────────────────────────────────────────────────────────────
        //
        // #693's rule: a scene nobody can reach on demand is a scene that ships broken. Four of these are the
        // cheats the seat's own demos are booted with, and the fifth is the sleeve those demos need filled.

        /// <summary>#784 QA · Put three real finds in the sleeve, for the rows that boot straight into a spread.</summary>
        void SeedTheSpreadFinds();

        /// <summary>#746 QA · <c>?roll=hi|lo</c> — force the BAND at the one rolled move in this family. Never the
        /// roll, so the on-screen math still reads truthfully.</summary>
        Encounter.Band? RollCheat { get; }

        /// <summary>#757 QA · <c>?approach=1|0</c> — force WHETHER anybody crosses the room to a table you took
        /// alone. Never who, and never what they want.</summary>
        bool? ApproachCheat { get; }

        /// <summary>#756 QA · <c>?neighbour=1|0</c> — the same, at the counter: whether the one beside you speaks.</summary>
        bool? NeighbourCheat { get; }

        /// <summary>#784 QA · <c>?spread=1</c> — whether this boot is one of the demos that sits the captain down
        /// with papers to work on.</summary>
        bool SpreadCheat { get; }
    }
}
