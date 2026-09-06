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
using SpaceSails.Core;

namespace SpaceSails.Client.Pages.Stations;

// NewsWirePanel — the code-behind for NewsWirePanel.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of NewsWirePanel.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class NewsWirePanel
{
    private const double SecondsPerDay = 86400.0;

    /// <summary>Which reading of the wire this is. Named on the tag rather than inferred from the caller,
    /// because a component that guessed at its own shape would be a component whose two consumers could
    /// silently swap boxes when somebody moved one of them.</summary>
    public enum WireShape
    {
        /// <summary>Today's headline over the dated scrollback — the pop-up card's long read.</summary>
        Wire,

        /// <summary>The one-line strip across the top of the comms room.</summary>
        Ticker,
    }

    [Parameter] public WireShape Shape { get; set; } = WireShape.Wire;

    /// <summary>The blended news feed (player events + ambient flavor), newest first — built by Map.razor's
    /// NewsFeed helper so the card and the ticker always read from one source of truth. Handed in WHOLE:
    /// how many of it to show is <see cref="ItemCount"/>'s business and therefore this component's, which is
    /// the only way the two shapes can be told apart by a parameter rather than by their callers' slicing.
    /// </summary>
    [Parameter, EditorRequired] public IReadOnlyList<NewsWire.NewsItem> Feed { get; set; } = [];

    /// <summary>How many lines this reading shows. The ticker takes the freshest few (Map's
    /// CommsTickerItemCount); the card takes the long scrollback (GalleyFeedItemCount). How many DAYS of
    /// ambient flavor were blended in behind them is the caller's — it is a question about the feed, asked
    /// before it gets here.</summary>
    [Parameter] public int ItemCount { get; set; } = int.MaxValue;

    /// <summary>The current sim-time the day labels are relative to. Unused by the ticker, which prints no
    /// dates — required all the same, because a wire that could be drawn without one is a wire whose next
    /// shape gets its dates wrong.</summary>
    [Parameter, EditorRequired] public double SimTime { get; set; }

    /// <summary>
    /// #1052 (L2) · <b>WHAT A ROW CAN DO, HANDED IN BY WHOEVER IS DRAWING THE WIRE.</b>
    ///
    /// <para>The seated panel puts a <c>✂ Clip</c> on every line; the galley card and the comms ticker put
    /// nothing on any line, and pass nothing here. So the two shipped consumers draw exactly the markup they
    /// drew before this lane, and the third one gains its control WITHOUT a second reading of the feed
    /// living next door — which is the whole reason #1021 made this a component rather than a file move.
    /// </para>
    ///
    /// <para>A <c>RenderFragment&lt;NewsItem&gt;</c> and not a <c>Clip</c> callback: filing a headline into
    /// a field book is the PAGE's business (it owns the book, the place-name and the vault save), and a
    /// component that grew a verb of its own would be a second author of a note.</para>
    /// </summary>
    [Parameter] public RenderFragment<NewsWire.NewsItem>? RowAction { get; set; }

    /// <summary>The row's control, or nothing at all. Written once so the headline row and the dated rows
    /// cannot come to offer different controls — they are the same story, one of them merely newer.</summary>
    private RenderFragment TheRowsOwnControl(NewsWire.NewsItem item) =>
        RowAction is null ? _ => { } : RowAction(item);

    private IReadOnlyList<NewsWire.NewsItem> Shown =>
        Feed.Count <= ItemCount ? Feed : Feed.Take(ItemCount).ToList();

    /// <summary>"today" / "yesterday" / "Nd ago", relative to the current sim-day — pure arithmetic, no
    /// wall-clock, matching the determinism rule the wire itself follows.</summary>
    private string DayLabel(double itemSimTime)
    {
        long today = (long)Math.Floor(SimTime / SecondsPerDay);
        long day = (long)Math.Floor(itemSimTime / SecondsPerDay);
        long delta = today - day;
        return delta <= 0 ? "today" : delta == 1 ? "yesterday" : $"{delta}d ago";
    }
}
