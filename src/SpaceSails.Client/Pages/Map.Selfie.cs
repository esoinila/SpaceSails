using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Selfie — THE CAPTAIN'S SELFIE (issue #400, owner's cruise 2026-07-20). Two ways a shot is taken:
//   • at a SELFIE SPOT console ashore (the scenic outer havens), press E and the captain poses into the
//     vista — a boastful house-voice caption, filed into the legend ledger;
//   • at a PEAK MOMENT (a deflection saved a port, first sight of the monolith, surviving a reveal), the
//     game nudges "grab a selfie for the record?" — a one-time offer that captures a vainer themed shot.
// The album rides on the thread row (GameThreadInfo.Selfies), persisted by the registry and RESET on
// succession — the wall of fame is per-life (a new captain inherits none). The pure catalog, caption pool
// and capture composition are Core (SelfieSpots); this partial is only the wiring + the framed overlay.
public partial class Map
{
    // The framed shot currently thrown up on screen (vista + the captain's portrait disc + the boast), or
    // null. Pressing E on the spot again, or Close, dismisses it — the ViewObject idiom.
    private CapturedSelfie? _selfieShot;

    // The pending peak-moment offer ("grab a selfie for the record?"), or null. A small hook: the beat
    // sites call OfferSelfie and go on with their narration — this raises the nudge, the player says yea/nay.
    private SelfieOffer? _selfieOffer;

    // Beats the player PASSED on this session — so a "no thanks" isn't re-nagged every time the same kind
    // of beat fires again. A taken shot is guarded by the album itself (AddSelfie dedups on the beat id),
    // which persists; this decline set is deliberately session-only (a fresh load may offer once more).
    private readonly HashSet<string> _selfieOffersDeclined = [];

    /// <summary>One pending peak-moment selfie offer — the beat it fires for, the vain label, and the vista
    /// backdrop the shot would compose onto (empty = the captain's own frame, no scene art).</summary>
    private readonly record struct SelfieOffer(string BeatId, string Label, string VistaArt);

    // The captain's face for a shot — the active universe's #368 avatar (the portrait that composites into
    // the frame). Falls back to a stable seeded face if the row isn't loaded yet (a pre-index legacy run).
    private int CurrentCaptainAvatar()
        => ActiveThreadInfo is { } row ? Captains.For(row).AvatarIndex : Captains.AvatarIndex(_activeThreadId);

    // The sim day stamped on a shot — "day N", the same clock the roster and saves read.
    private int SelfieSimDay() => (int)(SimTime / 86400);

    // ── The scenic photo spot (press E at the bar window) ──────────────────────────────────────────────

    /// <summary>Press E at a station's 📸 selfie spot: pose the captain into the vista, file the shot into
    /// the legend ledger, and throw the framed card up. E again (or Close) dismisses it. Idempotent per
    /// life — the album dedups on the spot id, so re-posing just re-shows the same boast (no ledger spam).</summary>
    private void CaptureSelfieAtSpot()
    {
        if (_selfieShot is not null)
        {
            _selfieShot = null; // E again closes the frame
            return;
        }

        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.SelfieSpot })
        {
            return;
        }

        if (SelfieSpots.CaptureAt(_dockedHavenId ?? "", _activeThreadId ?? "", CurrentCaptainAvatar(), SelfieSimDay())
            is not { } shot)
        {
            return; // no scenic spot at this berth (shouldn't happen — the console only places where one exists)
        }

        FileSelfie(shot);
        RendererInterop.PlayCue("reveal"); // the shutter — the same bright cue the plaque/deflection use
    }

    // ── The peak-moment offer (a small hook the beat sites call) ───────────────────────────────────────

    /// <summary>A big beat just landed (a deflection saved a port, the monolith resolved, a reveal was
    /// survived) — nudge the captain to grab a selfie for the record. One-time: skipped if the shot is
    /// already in the ledger (taken) or the captain waved this beat off this session. Keep it a small hook —
    /// the caller narrates the beat as it always did; this only raises the offer.</summary>
    private void OfferSelfie(string beatId, string vistaArt = "")
    {
        if (SelfieSpots.Beat(beatId) is not { } beat)
        {
            return;
        }

        // Already taken (the album, persisted per-life) or already waved off this session — don't re-nag.
        if (_selfieOffersDeclined.Contains(beatId)
            || (ActiveThreadInfo?.Selfies.Any(s => s.SpotId == beatId) ?? false))
        {
            return;
        }

        _selfieOffer = new SelfieOffer(beatId, beat.Label, vistaArt ?? "");
    }

    /// <summary>The offer's "grab it" — capture the themed beat shot, file it, and throw the frame up.</summary>
    private void AcceptSelfieOffer()
    {
        if (_selfieOffer is not { } offer)
        {
            return;
        }

        _selfieOffer = null;
        if (SelfieSpots.CaptureBeat(offer.BeatId, _activeThreadId ?? "", CurrentCaptainAvatar(), SelfieSimDay(), offer.VistaArt)
            is { } shot)
        {
            FileSelfie(shot);
            RendererInterop.PlayCue("reveal");
        }
    }

    /// <summary>The offer's "not now" — wave it off for this session so the same beat doesn't re-nag.</summary>
    private void DeclineSelfieOffer()
    {
        if (_selfieOffer is { } offer)
        {
            _selfieOffersDeclined.Add(offer.BeatId);
        }

        _selfieOffer = null;
    }

    /// <summary>Throw a stored shot back up (the roster's legend-ledger thumb click) — no capture, just view.</summary>
    private void ShowSelfie(CapturedSelfie shot) => _selfieShot = shot;

    /// <summary>Put the framed shot back down.</summary>
    private void CloseSelfieShot() => _selfieShot = null;

    // File a shot into the active universe's legend ledger and show it. The registry write persists it onto
    // the thread row (per-life, additive) and dedups on the spot/beat id; we refresh the cached roster so an
    // in-game ledger view reflects the new shot at once. A legacy/unindexed run just shows it (AddSelfie
    // returns null) — the boast still lands, it simply isn't kept.
    private void FileSelfie(CapturedSelfie shot)
    {
        if (!string.IsNullOrEmpty(_activeThreadId))
        {
            Threads.AddSelfie(_activeThreadId, shot);
            RefreshThreadList();
        }

        _selfieShot = shot;
    }
}
