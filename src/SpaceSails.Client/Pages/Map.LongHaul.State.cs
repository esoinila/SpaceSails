using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · WHAT THE CROSSING REMEMBERS — every instance field of the long-haul family, in the one run and
/// the one order the base file kept them in.
///
/// <para>This is a partial named for a shape rather than a concern, and that is deliberate: the base
/// file declared all thirty of these fields together at its end, after the last method, so sending each
/// one off to sit beside the methods that read it would have re-ordered the class and made the cut's
/// purity proof unreadable. A pure move is measured by concatenating the partials and diffing against the
/// base body; a field that travels is a line the diff cannot account for. The fields are grouped here by
/// the same banners that grouped them there — the skip, the computed coast, the offer, the jump overlay,
/// the tucked card and the arrival brake.</para>
///
/// <para>None of them is static, which is why this partial is safe at all — see
/// <c>NoPartialClassSpreadsItsStaticFieldsTests</c>.</para>
/// </summary>
public partial class Map
{
    private bool _skipActive;                                     // the skip is fast-forwarding now
    private double _skipTargetEpoch;                              // sim-time it is aiming at
    private WarpSkip.EventKind _skipTargetKind;                   // what kind of event that is
    private string _skipTargetLabel = "";                         // one-voice words for the readout
    private int _skipWarpCommanded;                               // the warp DriveSkip last set (catch-all)
    private WarpSkip.NextEvent _skipNext = WarpSkip.NextEvent.None; // cached per frame for the button/chip
    private WarpSkip.LongCoastState _longCoast = WarpSkip.LongCoastState.Idle; // long-coast advert edge

    // #261 — the COMPUTED skip's beat: a jump-scale coast is reckoned (closed form) or chunked (yields),
    // never integrated into a frozen tab. Drives the "COAST CONSUMED" overlay while the void is consumed.
    private bool _coastSkipActive;
    private int _coastSkipDays;
    private string _coastSkipLabel = "";
    private LongHaul.Reach? _longHaulReach;           // #246: the CURRENT coast's reach — the manual-coast promise verdict ONLY
    private CelestialBody? _longHaulPlanet;           // #246: the destination's sun-orbiting planet, named in the promise/verdict
    private LongHaul.Departure? _longHaulDeparture;   // #246/#249 fix: the solved cheap departure the OFFER quotes and engages
    private string? _longHaulClearanceBlock;          // #267: cached surface-clearance refusal for the destination's departure (or null when clear)

    // #255 — the diegetic jump overlay + the re-seed guard. While _jumpInProgress the tick loop freezes
    // (OnTick returns early) so the world never integrates the void; _jumpActive drives the full-screen
    // "CROSSING THE VOID" overlay whose year counter ticks as the arrival-epoch fleet re-seeds.
    private bool _jumpActive;
    private bool _jumpInProgress;
    private int _jumpYear;
    private int _jumpTotalYears;
    private string _jumpDestName = "";
    private string _jumpFlavor = "";

    /// <summary>
    /// #992 · THE VOID CARD, TUCKED AWAY. Owner ruling 2026-08-24: <i>"As a general ruling there should not be
    /// a pop-up that cannot be closed or minimized."</i>
    ///
    /// <para><b>This overlay was the worst offender in the whole client, by a distance.</b> Every other
    /// surface in <c>Map.razor</c> has at least one control that ends it; <c>.jump-overlay</c> — used by BOTH
    /// the long-haul crossing and the computed coast skip — is <c>position: fixed; inset: 0</c> at z 1400 with
    /// a 3 px blur over the entire viewport and <b>not one button in it</b>. Its own comment said so out loud:
    /// <i>"No cancel is offered."</i> There was no ✕, no scrim click, no key: the player waited, and that was
    /// the whole of the interface.</para>
    ///
    /// <para><b>It is a MINIMISE and not a close, and the distinction is the point.</b> Cancelling a crossing
    /// is a decision about the WORLD — the tick loop is frozen behind <c>_jumpInProgress</c> precisely so the
    /// void is never integrated, and handing the player a button that abandons a re-seed half-done would be a
    /// game-design change nobody asked for. Tucking the CARD away is a decision about the SCREEN, and it
    /// changes nothing at all: the crossing runs on exactly the clock it ran on before, the year counter goes
    /// on ticking, and the arrival lands when it always landed. The captain simply gets to watch his own map
    /// while it happens instead of a blurred sheet.</para>
    ///
    /// <para>The idiom is the one the scope (#963) and the dossier (#960) already share, and the tile is read
    /// off the same fields the full card reads — so the two can never disagree about which year it is. It is
    /// remembered for the session, for the scope's own stated reason: <i>"a captain who tucks the scope away
    /// expects it to stay tucked away until he says otherwise."</i> A crossing that re-inflated a card the
    /// player had deliberately put away would be the ruling failing on the second jump instead of the
    /// first.</para>
    ///
    /// <para>#997 wave 2 · The GESTURE is no longer written here. <c>OverlayShell</c> owns the –, the tile
    /// and the round trip; this field is only what the page REMEMBERS between crossings, bound into the
    /// shell with <c>@bind-Minimized</c>. The bespoke <c>ToggleVoidCardTucked</c> that used to live on this
    /// line is deleted, along with the separate tile element it needed — a tile drawn as its own sibling had
    /// to be kept agreeing with the sheet about which year it is, and now there is nothing to agree
    /// with.</para>
    /// </summary>
    private bool _voidCardTucked;
}
