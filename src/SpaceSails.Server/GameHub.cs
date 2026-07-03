using Microsoft.AspNetCore.SignalR;
using SpaceSails.Contracts;

namespace SpaceSails.Server;

/// <summary>
/// The command channel (plan §M9). Deliberately thin: every method delegates to
/// <see cref="SessionHost"/>, which owns all state and does all validation — a client can
/// send anything it likes; only legal effects happen.
/// </summary>
public sealed class GameHub : Hub
{
    private readonly SessionHost _session;

    public GameHub(SessionHost session)
    {
        _session = session;
    }

    public JoinResultDto Join(string callsign) => _session.Join(Context.ConnectionId, callsign);

    public void Pulse(bool accelerate, bool fine = false) => _session.Pulse(Context.ConnectionId, accelerate, fine);

    public void Vent() => _session.Vent(Context.ConnectionId);

    public void SetPlan(PlanNodeDto[] nodes) => _session.SetPlan(Context.ConnectionId, nodes);

    public void RequestWarp(int warp) => _session.RequestWarp(Context.ConnectionId, warp);

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _session.Leave(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
