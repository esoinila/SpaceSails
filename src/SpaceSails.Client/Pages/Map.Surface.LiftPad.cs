using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: #602's keypad on the car panel — the sticker, the four digits, the three tries, and the man the
// third one sends for. The panel itself is Map.Surface.Hive.cs; the arithmetic is UndergroundComplex.LiftCode.
public sealed partial class Map
{
    // ── #602 · THE PAD ON THE PANEL ────────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-08-02: "the numpad idea is to even have like a vicious security sticked warning next to it
    // and it gives you 3 tries before calling security" — overruling #590's call 3 deliberately, and the
    // argument for why that is allowed is at UndergroundComplex.LiftCode's head.
    //
    // FOUR THINGS ABOUT WHAT IS HERE, and all four are about how little of it is new:
    //
    //   1. THE PAD IS THE PAD. It is drawn with the hatch keypad's own keys and its own CSS (#736's
    //      .pin-pad-*), because a second keypad that looked different would be a building with two kinds of
    //      keypad in it, and the captain has already learnt this one on a bonded-stores hatch.
    //   2. IT IS INSIDE THE CAR PANEL, not a pop-up of its own. The panel is already an OverlayShell with a
    //      Close on it and a backdrop that dismisses (the 2026-08-24 general law: no pop-up that cannot be
    //      closed), and a modal raised over a modal to type four digits into would be #777's stacked card.
    //   3. NOTHING HERE DECIDES ANYTHING. Which row has a pad, what the code is, what the pad says, when the
    //      window forgets — every one of those is Core's (#600's rule: Core decides, the razor draws).
    //   4. NO NEW SECURITY. The third miss goes to Patrol.SecurityWasCalledTo, which is #618's walk to a
    //      place pointed at a keypad; what arrives is the GENERAL HANDS challenge that has been on these
    //      floors since #804. #618 still owes the owner a ruling on a second security body, and this lane
    //      leaves it owing.

    /// <summary>#602 · Four slots, filled left to right — the hatch pad's own display, for the reason its
    /// keys are its own keys.</summary>
    private string LiftPadDisplay =>
        _surface is { } ex
            ? string.Concat(Enumerable.Range(0, 4)
                .Select(i => i < ex.LiftPadEntry.Length ? ex.LiftPadEntry[i] : '·'))
            : "····";

    /// <summary>#602 · What the pad last answered, or null before the first press. One of Core's four
    /// plates and never a sentence: a lock does not narrate.</summary>
    private string? LiftPadSaid => _surface?.LiftPadSaid;

    /// <summary>#602 · Is the pad out? It goes dark when it calls security and comes back when the window
    /// forgets — Core's one arithmetic, asked rather than re-derived here.</summary>
    private bool LiftPadIsDark =>
        _surface is { } ex
        && UndergroundComplex.LiftCode.IsDark(ex.LiftPad, ex.SecondsOnTheGround);

    private void LiftPadPush(string digit)
    {
        if (_surface is { } ex && !LiftPadIsDark && ex.LiftPadEntry.Length < 4)
        {
            ex.LiftPadEntry += digit;
        }
    }

    private void LiftPadClear()
    {
        if (_surface is { } ex)
        {
            ex.LiftPadEntry = "";
        }
    }

    /// <summary>
    /// #602 · <b>↵.</b> The one press this whole feature is about, and the sticker on the wall said what it
    /// costs before the captain got here.
    ///
    /// <para>The entry is spent whichever way it goes — a pad that left the wrong digits sitting in the
    /// display would have the captain pressing ↵ twice on one guess and paying for it twice.</para>
    ///
    /// <para><b>The window is measured on <c>SecondsOnTheGround</c>, not on <c>SimTime</c>,</b> and that is
    /// #469's law rather than a preference: SimTime is the ship's orbital clock and it barely advances while
    /// somebody is standing on a regolith, so a ninety-second window measured on it would never close and
    /// the decay the owner ruled would silently not exist. It is not FloorSeconds either — that zeroes on
    /// every lift ride, so a captain who missed twice and rode one floor would get a free window.</para>
    /// </summary>
    private void LiftPadSubmit(UndergroundComplex.LiftStop stop)
    {
        if (_surface is not { } ex || !stop.HasPad)
        {
            return;
        }

        double now = ex.SecondsOnTheGround;
        if (UndergroundComplex.LiftCode.IsDark(ex.LiftPad, now))
        {
            return;   // the pad is out; it said so, and it says nothing further
        }

        string entry = ex.LiftPadEntry;
        ex.LiftPadEntry = "";

        if (UndergroundComplex.LiftCode.Answers(ex.Stop.Body.Id, entry))
        {
            // THIS EXCURSION ONLY. The band goes into the excursion's own set and nothing is written to the
            // vault — see the block on SurfaceExcursion.LiftCodeOpened for why that is the ruling.
            ex.LiftCodeOpened.Add(UndergroundComplex.BandOf(stop.Level));
            ex.LiftPadSaid = UndergroundComplex.LiftCode.OpenPlate;
            RendererInterop.PlayCue("board");
            return;
        }

        ex.LiftPad = UndergroundComplex.LiftCode.AWrongCode(ex.LiftPad, now);
        ex.LiftPadSaid = UndergroundComplex.LiftCode.PlateFor(ex.LiftPad, now);
        RendererInterop.PlayCue("blip");

        // …AND THE THIRD ONE SENDS FOR SOMEBODY. Nothing is said about it here — not a pulse, not a line in
        // the book, not a banner. The captain read the sticker before the first press and the pad has just
        // said SECURITY CALLED; a third sentence would be the building explaining its own consequence to
        // the person it is happening to (§13.8, and #618's own discipline about the man who simply comes).
        if (UndergroundComplex.LiftCode.SecurityIsCalled(ex.LiftPad, now))
        {
            SecurityWasCalledToThePad();
        }
    }

    /// <summary>#602 · Where the pad is, said to the round: the console the captain is standing at, which is
    /// the captain. You cannot open a car panel from across the floor.</summary>
    private void SecurityWasCalledToThePad() => _patrol.SecurityWasCalledTo(_avatarX, _avatarY);
}
