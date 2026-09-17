using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// MissionMomentCard — the code-behind for MissionMomentCard.razor (#238).
//
// #1107 · The members live in a .cs file beside the component because the razor generator's output is NOT
// ANALYSED: every line inside an `@code { … }` block is invisible to the analysers this repo runs with
// TreatWarningsAsErrors, so a finding in there is a finding nobody is ever shown.
public partial class MissionMomentCard
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251's idiom: every [Parameter] carries a member of the page under THE MEMBER'S OWN NAME — the
    // variable the rack's `@if` binds arrives under the name the `@if` gave it, a method arrives as a
    // delegate with its own signature — which is what lets the suite's source guards read this markup
    // through MapMarkup exactly as they read the eight cards beside it.

    /// <summary>The beat itself, composed in Core (<see cref="MissionMoments"/>). Everything this card
    /// prints comes out of here; nothing is worded in the markup.</summary>
    [Parameter] public MissionMoment moment { get; set; } = default!;

    [Parameter] public EventCallback DismissMissionMoment { get; set; }

    /// <summary>The <i>show me</i> press: centres the map on the new marker and takes the card down.</summary>
    [Parameter] public EventCallback ShowMeTheMissionMoment { get; set; }

    /// <summary>#238 · <b>IS THERE ANYWHERE TO GO?</b> A reveal carries the body it charted; a pickup
    /// carries null, because the ship is already alongside the thing it just prised. Asked once, here, and
    /// answered as a pair below — the shell faults a face with no delegate behind it and a delegate with no
    /// face on it, both deliberately, so the two must move together or not at all.</summary>
    private bool SomewhereToLook => moment.ShowMeBodyId is { Length: > 0 };

    /// <summary>The second way out's label, or null when there is nowhere to look.</summary>
    private string? ShowMeFace => SomewhereToLook ? MissionMoments.ShowMeFace : null;

    /// <summary>The second way out's press, or an empty callback when there is nowhere to look — which is
    /// what makes the shell draw no button at all rather than a dead one.</summary>
    private EventCallback ShowMePress => SomewhereToLook ? ShowMeTheMissionMoment : default;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
