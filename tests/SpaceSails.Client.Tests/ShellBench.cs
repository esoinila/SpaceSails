#pragma warning disable BL0006 // the render tree is exactly what this bench exists to read — see DeskBench
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 · THE SHELL BENCH — <see cref="DeskBench"/>'s renderer with ONE component in it instead of the
/// whole page.
///
/// <para><b>Why not just use DeskBench.</b> Because the questions are different. DeskBench exists to prove
/// that the shipping page draws, and every one of its worlds costs a full boot; what a component test needs
/// is a shell with three parameters on it and a button pressed. So this mounts an arbitrary
/// <see cref="RenderFragment"/> and reads the tree back with DeskBench's OWN walk — the walk is now shared
/// rather than copied, so a component test and the dismissibility law can never disagree about what a
/// render tree contained.</para>
///
/// <para><b>What it keeps from DeskBench, deliberately.</b> <c>PressAsync</c> dispatches through the
/// renderer's real event channel at the handler id the tree wrote — the click a player's mouse makes, and
/// not the method reached by name. That distinction is the whole reason #992's law can be trusted, and a
/// component bench that pressed by reflection would prove the shell has a method called
/// <c>PressTheDismiss</c> and nothing at all about whether the ✕ is wired to it.</para>
/// </summary>
internal sealed class ShellBench : Renderer
{
    private readonly Stage _stage = new();
    private readonly List<Exception> _escaped = [];
    private int _rootId = -1;

    private ShellBench()
        : base(new NoServices(), NullLoggerFactory.Instance)
    {
    }

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    /// <summary>Every exception a render or a press raised. Nothing is filtered here: unlike the page, a
    /// bare component reaches for no browser, so anything that escapes is a real failure.</summary>
    public IReadOnlyList<Exception> Escaped => _escaped;

    protected override void HandleException(Exception e) => _escaped.Add(e);

    protected override Task UpdateDisplayAsync(in RenderBatch batch) => Task.CompletedTask;

    public static ShellBench Mount(RenderFragment content)
    {
        var bench = new ShellBench();
        bench._stage.Content = content;
        return bench;
    }

    /// <summary>Draw it (or draw it again) and hand back the tree. The stage re-renders the SAME fragment,
    /// so anything the fragment holds — a shell's own tucked flag, a child component's state — is state
    /// across paints rather than a fresh page each time.</summary>
    public async Task<DeskBench.Painted> RenderAsync()
    {
        if (_rootId < 0)
        {
            _rootId = AssignRootComponentId(_stage);
            await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(_rootId));
        }
        else
        {
            await Dispatcher.InvokeAsync(_stage.Refresh);
        }

        var painted = new DeskBench.Painted();
        DeskBench.Walk(GetCurrentRenderTreeFrames, _rootId, painted, new StringBuilder(), painted.Root);
        return painted;
    }

    /// <summary>
    /// Re-render the stage from INSIDE a handler — what a page does when it writes a field and calls
    /// <c>StateHasChanged</c>.
    ///
    /// <para>It matters to one test and it matters a lot. Blazor renders a batch in the order things were
    /// queued: a handler that reaches the page first queues the PAGE's render first, so a surface the page
    /// drops is disposed before its own queued render is reached. A bench that only re-rendered afterwards
    /// would leave every surface standing for one extra paint and make a "did this stay up?" audit fire on
    /// everything — which is exactly how a guard stops being able to tell pass from fail.</para>
    /// </summary>
    public void Redraw() => _stage.Refresh();

    public Task PressAsync(ulong handlerId) =>
        Dispatcher.InvokeAsync(() => DispatchEventAsync(handlerId, null, new MouseEventArgs()));

    /// <summary>Find one control by the words a player could read off it — the same reading
    /// <see cref="DeskBench.Painted.Node.Name"/> takes, and the same one #992's law presses by.</summary>
    public static DeskBench.Painted.Node? Control(DeskBench.Painted painted, string named) =>
        painted.Root.Descendants().FirstOrDefault(
            node => node.Handlers.ContainsKey("onclick")
                    && string.Equals(node.Name, named, StringComparison.Ordinal));

    /// <summary>The first element wearing a class, hidden ones included — a tucked surface is still in the
    /// tree and that is exactly what several of these tests are about.</summary>
    public static DeskBench.Painted.Node? Wearing(DeskBench.Painted painted, string css) =>
        painted.Root.SelfAndDescendants().FirstOrDefault(node => node.HasClass(css));

    /// <summary>The one component the renderer roots on: it renders whatever fragment it was handed, and
    /// re-renders it on demand. A stage rather than a fixture with parameters, because what these tests
    /// vary is the MARKUP — which shell, with what on it — and a fragment is markup.</summary>
    private sealed class Stage : IComponent
    {
        private RenderHandle _handle;

        public RenderFragment? Content { get; set; }

        public void Attach(RenderHandle handle) => _handle = handle;

        public Task SetParametersAsync(ParameterView parameters)
        {
            Refresh();
            return Task.CompletedTask;
        }

        public void Refresh() => _handle.Render(Content!);
    }

    /// <summary>A component that asks for a service gets a null and a NullReferenceException naming it,
    /// which is louder and more useful than a container that invents one. Neither primitive injects
    /// anything, and the day one starts, this is where that shows up.</summary>
    private sealed class NoServices : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
