using SpaceSails.Client.Rendering;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #870 lane 6d · THE ONE PLACE A SITTING IS OPENED.
///
/// <para>Six verbs in this family sit the captain down — on a park bench, on a cubicle's pan, on a stool at a
/// chamber worktop, in a chair in a ring office, at a free canteen top, and at a top somebody is already at —
/// and every one of them ended the same three lines: the seat into the field, the reveal cue, a draw. Six
/// copies of a tail is six chances for one of them to drift, and this repository has a named bug class for a
/// law transcribed at its call sites. So the tail is written down once and the six sites keep only what is
/// genuinely theirs: <b>which seat this is</b>.</para>
///
/// <para>#820's snap deliberately does NOT come in here. Every one of the six calls
/// <c>_host.SitCaptainOn</c> before it builds the record — two of them only if the furniture published a
/// chair to take — and that call is the seat's own coordinate, not a shared step. Folding it in would have
/// meant either a nullable parameter re-spelling a conditional the site already states, or moving the
/// placement AFTER the record is built, because a method's arguments are evaluated before its body. Both are
/// behaviour questions dressed as tidiness, and the answer to a behaviour question in a refactoring lane is
/// to leave the line where it is.</para>
/// </summary>
public partial class Map
{
    private sealed partial class Seating
    {
        /// <summary>
        /// Take this seat: the sitting becomes the open one, the room plays the reveal, and the page draws.
        ///
        /// <para>The cue is <c>reveal</c> at every seat in the game and always has been — a chair, a plank, a
        /// pan and a canteen top are one POSTURE (the whole reason there is one <c>TableTalk</c> and not five
        /// records), so they are one sound. A seat that wants a different noise is a design decision and
        /// belongs in an argument, not in a seventh copy of these three lines.</para>
        ///
        /// <para>The draw is unconditional and last, for the reason every one of the six had it there: the
        /// panel the captain is about to read is a function of <see cref="Table"/>, so the frame that asks
        /// for it has to be the frame after the seat is in.</para>
        /// </summary>
        private void TakeThisSeat(TableTalk seat)
        {
            Table = seat;
            RendererInterop.PlayCue("reveal");
            _host.StateHasChanged();
        }
    }
}
