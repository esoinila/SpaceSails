using System;

namespace SpaceSails.Core;

/// <summary>
/// #696 · THE DARKROOM — processing what you found takes TIME, and the air prices the WHERE.
///
/// <para>Owner, mid-run, designing the detective loop's cost model: <i>"How is our detective notebook /
/// picture taking progressing for our ability to process the files etc so we don't need carry them. That is
/// something one would do without using tanked air. It is good game mechanic... we take time to process the
/// loot."</i></para>
///
/// <h3>What was wrong with instant</h3>
/// <para>Since #691 a captain could empty a sleeve of documents into the field book with six clicks and no
/// cost at all — the gist filed, the paper dropped, the pocket free, and the world outside the dialog exactly
/// where it was when they opened it. The knowledge was the whole reward of the ground and it was being
/// collected in a frozen moment. The verb had no body.</para>
///
/// <h3>The ruling, and why there is no new meter</h3>
/// <para>Processing a find — photographing a sheet so it can be left, deciding a paper is a map — costs
/// <b>sim time, standing still</b>. That is the entire mechanic. It buys three things the game already had
/// the parts for and had never joined up:</para>
/// <list type="number">
/// <item><b>The air sim prices the location for free.</b> Out on the regolith the hold burns tank, because
/// the tank burns while time passes and nothing here is allowed to know that. In a shelter, in a refuge, on
/// a floor that still holds pressure, aboard her — the drain is already off (#573, #585, #608, #612), so the
/// same interaction costs only the seconds. <i>Read it here or haul it to safety</i> is now a real question,
/// and NOTHING in this file computes it.</item>
/// <item><b>The shelter gets a reason to be visited when you are not dying.</b> It never had one. It was a
/// place you crawled to; it is a field darkroom now.</item>
/// <item><b>Standing still is the danger.</b> The motion tracker keeps running through the hold and the Old
/// Ones shamble at 5.6 du/s, so twenty seconds is a hundred metres of somebody else's walk. The mechanic has
/// teeth without a single new enemy — the same trick #591 played with depth.</item>
/// </list>
///
/// <h3>The law this file is written under</h3>
/// <para><b>Nothing in here mentions air.</b> Not a constant, not a branch, not a comment that turns into a
/// branch. The instant this class learns to charge a tank there are two answers to "what does standing still
/// cost", and the second one will be the one that goes wrong on the floor where the first one was right —
/// which is this repo's oldest and most expensive habit. The hold passes time; <see cref="SuitAir"/> prices
/// time; they never meet.</para>
///
/// <para>Pure and deterministic: the same hold, asked twice, answers the same both times.</para>
/// </summary>
public static class Processing
{
    /// <summary>
    /// <b>The one number.</b> How long a document takes to process, in sim seconds of standing still — read
    /// by the hold, by the bar that draws it, by the line that warns you before you start, and by the cheat
    /// that switches it off. There is no second copy anywhere and there must never be one.
    ///
    /// <para>Twenty, out of the owner's fifteen-to-thirty. The bottom of that range is a pause; the top is
    /// long enough that a captain stops taking the decision seriously and starts resenting it. Twenty is a
    /// sixtieth of a tank (#564: 1200 s), so a sleeve of six papers worked through on open regolith is two
    /// minutes and a tenth of your air — a real bite that never on its own kills you — and it is long enough
    /// for the fan to sweep several times and for anything shambling at the rim to cover a hundred metres of
    /// the distance between you.</para>
    /// </summary>
    public const double SecondsPerDocument = 20.0;

    /// <summary>How far the captain may drift and still be <i>standing still</i>, in deck units. Half the
    /// interact radius: enough that a nudge or a settle does not throw away twenty seconds of work, far less
    /// than a step (the walk is 9 du/s, so one deliberate stride is out).
    ///
    /// <para>A tolerance and not zero, because a hold that aborted on floating-point noise would be a hold
    /// nobody could ever finish, and the captain would have no way to tell that from a bug.</para></summary>
    public const double StandingToleranceDu = 1.5;

    /// <summary>What the one channel bar wears while a hold runs. A camera, and not the shovel: a shovel over
    /// somebody photographing a pay sheet is the sim doing one thing while a picture reports another, which
    /// is the class of bug #562 filed this bar's glyph parameter to end.</summary>
    public const string Glyph = "📸";

    /// <summary>Which slow thing this is. Both cost the same seconds — deciding a paper is a map is exactly
    /// as much standing still as photographing one — and they are two members rather than one because the
    /// SENTENCES differ, and a captain who is told the wrong story about what their hands are doing has been
    /// lied to whatever the clock says.</summary>
    public enum Work
    {
        /// <summary>#691 · Photographing a document so the sheet can be left behind: the gist goes in the
        /// field book, the paper stays on the ground.</summary>
        File,

        /// <summary>#603 · Reading a paper as a clue — deciding it is a map and plotting it on the tracker.</summary>
        Read,
    }

    /// <summary>Has the captain moved off the spot they started on?</summary>
    public static bool Wandered(double anchorX, double anchorY, double x, double y)
    {
        double dx = x - anchorX, dy = y - anchorY;
        return (dx * dx) + (dy * dy) > StandingToleranceDu * StandingToleranceDu;
    }

    /// <summary>How far through, 0…1, for the bar. A hold of no length is finished the moment it starts —
    /// which is what the <c>?process=0</c> cheat asks for, and the bar must not divide by it.</summary>
    public static double Fraction(double elapsed, double seconds) =>
        seconds <= 0 ? 1.0 : Math.Clamp(elapsed / seconds, 0.0, 1.0);

    /// <summary>Seconds still to stand, never negative.</summary>
    public static double SecondsLeft(double elapsed, double seconds) => Math.Max(0.0, seconds - elapsed);

    /// <summary>Is it done? One predicate, so the bar filling and the effect firing can never disagree about
    /// what "finished" means.</summary>
    public static bool Done(double elapsed, double seconds) => elapsed >= seconds;

    /// <summary>Why a hold ended without producing anything.</summary>
    public enum Interruption
    {
        /// <summary>The captain walked off the spot.</summary>
        Walked,

        /// <summary>An air alarm fired mid-hold. It BREAKS the hold on purpose: a captain must never
        /// suffocate inside a silent one, and a warning that plays behind a progress bar the captain is
        /// watching fill is the silent timer #564 was written to forbid.</summary>
        Alarm,

        /// <summary>Something got a hand on you. Standing still is the price of the mechanic and this is
        /// what the price buys.</summary>
        Reached,

        /// <summary>The excursion ended under them — the shuttle lifted with the work half done.</summary>
        LiftedOff,
    }

    /// <summary>What the captain is told as their hands go to the paper. It names the seconds, because the
    /// decision the whole mechanic exists for — <i>here, or somewhere with air in it</i> — cannot be taken by
    /// somebody who does not know what it costs.</summary>
    public static string StartLine(Work work, string what, double seconds)
    {
        ArgumentNullException.ThrowIfNull(what);
        string clock = seconds <= 0 ? "" : $" {seconds:F0} seconds of standing still —";
        return work == Work.Read
            ? $"{Glyph} You get {what} out and start working it against the tracker's map —{clock} hold " +
                "position. Watch the fan. Step away and you have decided nothing."
            : $"{Glyph} You flatten {what} out and photograph it, page by page —{clock} hold position. " +
                "Watch the fan. Step away and nothing goes in the book.";
    }

    /// <summary>The line said INSIDE whatever surface is up while the hold runs — the satchel, if the captain
    /// opened it again to watch. #680's law is about the BLUR and not about the pulse: a progress the player
    /// cannot see is a progress that is not happening.</summary>
    public static string HoldLine(Work work, string what, double secondsLeft)
    {
        ArgumentNullException.ThrowIfNull(what);
        string verb = work == Work.Read ? "Working" : "Photographing";
        return $"{Glyph} {verb} {what} — {secondsLeft:F0} s left, and your boots have to stay where they are.";
    }

    /// <summary>What is said when a hold ends with nothing to show for it. Every one of these is a promise
    /// that the world was left exactly as it was found: no gist filed, no paper spent, no lead plotted.</summary>
    public static string AbandonedLine(Work work, string what, Interruption why)
    {
        ArgumentNullException.ThrowIfNull(what);
        string it = work == Work.Read ? "the reading" : "the exposure";
        return why switch
        {
            Interruption.Alarm =>
                $"{Glyph} The alarm goes off in your ear and {it} is spoiled — {what} back in the sleeve, " +
                "nothing filed. The suit has something to say and it wins.",
            Interruption.Reached =>
                $"{Glyph} Something gets a hand on you and {it} is gone — {what} back in the sleeve, nothing " +
                "filed. That is what standing still costs out here.",
            Interruption.LiftedOff =>
                $"{Glyph} The shuttle lifts with {it} half taken — {what} rides home in your sleeve, and " +
                "nothing about it went in the book.",
            _ =>
                $"{Glyph} You move, and {it} is spoiled — {what} back in the sleeve, nothing filed. It is " +
                "still yours; it is just still unread.",
        };
    }

    /// <summary>What a second press is told while the first one is still running. One pair of hands, one
    /// document — and the refusal is SAID, because a control that does nothing and says nothing is
    /// indistinguishable from a bug (#603).</summary>
    public static string AlreadyBusyLine(Work work, string what)
    {
        ArgumentNullException.ThrowIfNull(what);
        string doing = work == Work.Read ? "reading" : "photographing";
        return $"{Glyph} Both hands are on {what} — you are {doing} it. Finish, or walk away from it.";
    }

    /// <summary>The hint on the leave control, so the cost is known before the press rather than after it.
    /// Reads the same number the hold does, which is the whole reason this function is here rather than a
    /// figure typed into the razor.</summary>
    public static string LeaveHint(double seconds) => seconds <= 0
        ? "Leave it here — it stays where you put it"
        : $"Photograph it and leave it here — {seconds:F0} s standing still, and the tank pays for them " +
            "unless you are somewhere with air in it";
}
