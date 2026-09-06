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
using SpaceSails.Client.Components;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;

namespace SpaceSails.Client.Components;

// CappedScrollPanel — the code-behind for CappedScrollPanel.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of CappedScrollPanel.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class CappedScrollPanel
{
    /// <summary>The part that keeps its measured height: every row above the scroller. Rendered as the
    /// panel's own direct children — no wrapper — because the head is not one element in any panel that
    /// has ever needed this, and a wrapper would break every `.panel > *` rule written against it.</summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>The part that gives, and scrolls.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Whether the body element is drawn at all. The Plotting panel has an empty state — "No steps yet.
    /// Press ⚓ + Cast off…" — which belongs in the HEAD, and a body element drawn anyway would stand an
    /// empty scroller its own floor tall underneath it. Explicit rather than "is the fragment null",
    /// because a fragment that renders nothing is not a null fragment and cannot be told apart from one.
    /// </summary>
    [Parameter] public bool HasContent { get; set; } = true;

    /// <summary>The class the SCROLLING element wears, so a page keeps the name its own rules and its own
    /// browser-level guards already point at (<c>.map-plot-nodes</c> is located by name in
    /// PlotPanelFitsTheWindowTests).</summary>
    [Parameter] public string? BodyClass { get; set; }

    /// <summary>
    /// How short the body is allowed to get before it stops giving and the panel pages instead. #993's
    /// 6rem is the measured floor for a flight plan — roughly two steps and a scrollbar — and a panel that
    /// did not name one could be squeezed to nothing by a tall enough head on a short enough window.
    /// </summary>
    [Parameter] public string BodyFloor { get; set; } = "6rem";

    /// <summary>
    /// #993 · OPENING A CHILD SCROLLS ITS ROW INTO VIEW. The identity of whatever is open inside the body,
    /// as a string; when it CHANGES between paints, <see cref="OpenChildSelector"/> is scrolled just
    /// inside its scrollers.
    ///
    /// <para>A comparison rather than a flag, and that is #993's own finding rather than a preference: the
    /// Plotting panel's editor opens from five places — the row toggle, a fresh burn, the cast-off pair, a
    /// ribbon-node click and an arrival — and a flag set at four of them is a flag somebody forgets at the
    /// fifth. Asking "did it change since the last paint" cannot be forgotten anywhere.</para>
    /// </summary>
    [Parameter] public string? OpenChild { get; set; }

    /// <summary>The CSS selector for the open child's row. A selector rather than an ElementReference
    /// because exactly one row carries the open class at a time, and the class the stylesheet already keys
    /// off is the one honest handle on it.</summary>
    [Parameter] public string? OpenChildSelector { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? Extra { get; set; }

    /// <summary>What the last paint already scrolled to. Starts at a sentinel rather than null so that a
    /// panel drawn with something ALREADY open scrolls to it on its first paint — null would read as "same
    /// as before" and leave a restored plan's open editor below the fold.</summary>
    private string? _scrolledTo = "";

    private string HostClass =>
        Extra is not null && Extra.TryGetValue("class", out object? css) ? css?.ToString() ?? "" : "";

    private IReadOnlyDictionary<string, object>? Splat =>
        Extra is null || !Extra.ContainsKey("class")
            ? Extra
            : Extra.Where(pair => pair.Key != "class").ToDictionary(pair => pair.Key, pair => pair.Value);

    /// <summary>The floor travels as a custom property so the arithmetic stays in ONE stylesheet: the page
    /// says how short is too short, the component says what to do about it.</summary>
    private string FloorStyle => $"--capped-scroll-floor: {BodyFloor};";

    protected override void OnAfterRender(bool firstRender)
    {
        if (string.Equals(OpenChild, _scrolledTo, StringComparison.Ordinal))
        {
            return;
        }

        _scrolledTo = OpenChild;
        if (OpenChild is { Length: > 0 } && OpenChildSelector is { Length: > 0 } selector)
        {
            RendererInterop.ScrollIntoView(selector);
        }
    }
}
