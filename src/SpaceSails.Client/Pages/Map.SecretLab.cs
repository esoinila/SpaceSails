using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.SecretLab — #409 THE SECRET LABS BEHIND HIDDEN DOORS (owner, 2026-07-20, 😎: "Do we have hidden doors
// at landing sites? Secret Dr Soong Labs."). The darker cousin of the expedition's VISIBLE sealed doors: a
// door CONCEALED in the deep field — not on the ground until DISCOVERED — hiding the sealed lab of Dr. Mielos
// Vantar, a vanished cyberneticist (an original homage, never the trademark). The pure spine lives in Core
// (SecretLab / VantarLore); this thin partial wires the discovery vector (the beach-comber detector), the
// forced-door channel that APPENDS the lab region (reusing the #393 append path), the diced reveal + nerve
// hit (the #391 idiom), and the per-thread "found it" persistence (the vault/thread idiom).
public partial class Map
{
    // The labs this game-thread has FOUND (revealed the hidden door of) — persisted per-universe in the vault's
    // ProgressSection, so a revisit to a known body shows the door already revealed (you remember where it is).
    private readonly HashSet<string> _secretLabsFound = [];

    // The body the ?secretlab=1 cheat guarantees a lab on, with the door pre-revealed for fast testing.
    private string? _secretLabForceBodyId;

    // ── Discovery: does this landing hide a lab, and is its door already known? ──
    private void ResolveSecretLab(SurfaceExcursion ex)
    {
        string body = ex.Stop.Body.Id;
        bool cheat = _secretLabForceBodyId == body;
        SecretLab.Placement placement = SecretLab.For(body, MoonSurface.ExpeditionField(), forcePresent: cheat);
        if (!placement.HasLab)
        {
            ex.Lab = null;
            return;
        }
        ex.Lab = placement;
        // Pre-reveal the door on a body already found this thread (persistence pays off), or under the cheat.
        if (cheat || _secretLabsFound.Contains(body))
        {
            ex.SecretLabDoorRevealed = true;
        }
    }

    // Persist that this thread found the lab at this body — the door stays known on every future landing.
    private void MarkSecretLabFound(string bodyId)
    {
        if (_secretLabsFound.Add(bodyId))
        {
            RequestVaultSave();
        }
    }

    // ── The beach-comber detector ping: sweeping the exact hidden-door square reveals it (instant, no dig). ──
    private bool TrySecretLabDetectorReveal(SurfaceExcursion ex, int squareX, int squareY)
    {
        if (ex.Lab is not { HasLab: true } p || ex.SecretLabDoorRevealed
            || !SecretLab.IsDoorSquare(p, squareX, squareY))
        {
            return false;
        }
        ex.SecretLabDoorRevealed = true;
        ex.Swept[(squareX, squareY)] = BeachComber.Outcome.Nothing; // the square is now checked (and famous)
        MarkSecretLabFound(ex.Stop.Body.Id);
        RebuildSurfaceDeck(); // re-composes with the now-revealed ⚙ HIDDEN DOOR console on the ground
        RendererInterop.PlayCue("reveal");
        ShowPulseMessage(
            "📡 The detector SHRIEKS and holds — not a coin, not scrap: a SEALED DOOR, buried flush with the " +
            "regolith where no door has any right to be. Someone hid this. Force it open ([E] at the door) — " +
            "or walk away and pretend you never found it.");
        return true;
    }

    // A near-miss tail for the honest probe message — the detector says something big is very close.
    private string SecretLabProximityTail(SurfaceExcursion ex, int squareX, int squareY) =>
        ex.Lab is { HasLab: true } p && !ex.SecretLabDoorRevealed
            && SecretLab.IsProximitySquare(p, squareX, squareY)
            ? " 📡 — but the detector SHRIEKS: something big and metal is buried very close. Sweep the squares right around here."
            : "";

    // ── Compose: the revealed hidden door, and — once forced — the appended lab region, onto a rebuilt base. ──
    private void ComposeSecretLabSite(SurfaceExcursion ex)
    {
        if (ex.Lab is not { HasLab: true } placement)
        {
            return;
        }
        var walls = new List<DeckPlan.Wall>();
        var labels = new List<(float X, float Y, string Text)>();
        var consoles = new List<DeckPlan.ConsoleSpot>();

        if (!ex.SecretLabForced)
        {
            if (ex.SecretLabDoorRevealed)
            {
                consoles.Add(new(DeckPlan.ConsoleKind.SecretDoor,
                    (float)placement.DoorX, (float)placement.DoorY, "⚙ HIDDEN DOOR — force it"));
            }
        }
        else
        {
            AppendSecretLabGeometry(ex, placement, walls, labels, consoles);
        }

        _deckPlan.AppendRegion(new DeckPlan.DeckRegion(
            walls.ToArray(), consoles.ToArray(), labels.ToArray(), []));
    }

    // Map a forced lab region's walls/landmark/consoles onto the deck lists (honouring a looted cache).
    private void AppendSecretLabGeometry(
        SurfaceExcursion ex, in SecretLab.Placement placement,
        List<DeckPlan.Wall> walls, List<(float X, float Y, string Text)> labels, List<DeckPlan.ConsoleSpot> consoles)
    {
        SecretLab.Region region = SecretLab.Build(ex.Stop.Body.Id, MoonSurface.ExpeditionField(), placement.DoorX, placement.DoorY);
        foreach (SurfaceLayout.Wall w in region.Walls)
        {
            walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, w.IsHull));
        }
        foreach (SurfaceLayout.Landmark m in region.Landmarks)
        {
            labels.Add(((float)m.X, (float)m.Y, m.Label));
        }
        foreach (SecretLab.LabConsole rc in region.Consoles)
        {
            if (rc.Kind == SecretLab.LabConsoleKind.DiscoveryCache && ex.SecretLabCacheLooted)
            {
                continue; // the fat cache is one-time — drop it once claimed
            }
            DeckPlan.ConsoleKind kind = rc.Kind == SecretLab.LabConsoleKind.DiscoveryCache
                ? DeckPlan.ConsoleKind.LabCache
                : DeckPlan.ConsoleKind.LabConsole;
            consoles.Add(new(kind, (float)rc.X, (float)rc.Y, rc.Label));
        }
    }

    // ── Forcing the hidden door [E]: a channeled progress bar (the #393 door-force idiom), abortable. ──
    private void SecretDoorInteract()
    {
        if (_surface is not { } ex || ex.AnyChannel || ex.Lab is not { HasLab: true })
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.SecretDoor } spot)
        {
            return;
        }
        ex.SecretLabDoorChannel = new DoorChannel { DoorId = "secretlab", AnchorX = spot.X, AnchorY = spot.Y };
        RendererInterop.PlayCue("board");
        ShowPulseMessage("⚙ Setting your shoulder to the hidden door… hold position. Whatever's behind it has waited a long time. Step away to abort.");
    }

    private void StepSecretLabDoorChannel(double dtRealSeconds)
    {
        if (_surface is not { SecretLabDoorChannel: { } ch } ex)
        {
            return;
        }
        double dx = _avatarX - ch.AnchorX, dy = _avatarY - ch.AnchorY;
        if ((dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius)
        {
            ex.SecretLabDoorChannel = null;
            ShowPulseMessage("You step back — the hidden door holds. It stays sealed. (Some doors are a mercy shut.)");
            return;
        }
        ch.Progress += dtRealSeconds / ExpeditionRegions.DoorForceSeconds;
        if (ch.Progress >= 1.0)
        {
            ex.SecretLabDoorChannel = null;
            ForceSecretLabDoor(ex);
        }
    }

    // The door gives — the lab APPENDS to the live plan (walls + benches/pods/spine + Vantar's consoles), and
    // crossing that threshold into what shouldn't exist is itself a small chill (the big reveal is the core log).
    private void ForceSecretLabDoor(SurfaceExcursion ex)
    {
        if (ex.Lab is not { HasLab: true } placement)
        {
            return;
        }
        ex.SecretLabForced = true;

        var walls = new List<DeckPlan.Wall>();
        var labels = new List<(float X, float Y, string Text)>();
        var consoles = new List<DeckPlan.ConsoleSpot>();
        AppendSecretLabGeometry(ex, placement, walls, labels, consoles);
        _deckPlan.AppendRegion(new DeckPlan.DeckRegion(
            walls.ToArray(), consoles.ToArray(), labels.ToArray(), []));
        _deckPlan.RemoveConsoleAt((float)placement.DoorX, (float)placement.DoorY, DeckPlan.ConsoleKind.SecretDoor, 0.3);

        _nerve = NerveModel.Shock(_nerve, SecretLabEntryChill); // the cold breath of the place
        RequestVaultSave();
        RendererInterop.PlayCue("reveal");
        ShowPulseMessage(
            "⚙ The seal cracks — stale, chemical air, decades unbreathed. Benches. Stasis pods. A spine of dead " +
            "servers. Someone LIVED down here, working. Read the logs ([E] the screens) — and mind the core log.");
    }

    /// <summary>The small nerve chill of crossing into the lab (owner: "entering the lab … is a nerve hit").
    /// A lump, not the big reveal — the core log deals that. FLAGGED for tuning.</summary>
    private const double SecretLabEntryChill = 7.0;

    // ── Claiming Vantar's fat one-time cache [E]. ──
    private void LabCacheInteract()
    {
        if (_surface is not { } ex || ex.Lab is not { HasLab: true } || ex.SecretLabCacheLooted)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.LabCache } spot)
        {
            return;
        }
        ex.SecretLabCacheLooted = true;
        _credits += SecretLab.DiscoveryCacheCredits;
        _deckPlan.RemoveConsoleAt(spot.X, spot.Y, DeckPlan.ConsoleKind.LabCache, 0.3);
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
        ShowPulseMessage(
            $"🗝 Vantar's cache — prototype lattices, cold-storage samples, a career's worth of forbidden work. " +
            $"+{SecretLab.DiscoveryCacheCredits:N0} cr. The veterans were right about this place.");
    }

    // ── Reading a Vantar log [E]. The CORE log fires the diced reveal + the nerve hit (the #391 idiom). ──
    private void LabConsoleInteract()
    {
        if (_surface is not { } ex || ex.Lab is not { HasLab: true } placement)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.LabConsole } spot)
        {
            return;
        }
        SecretLab.Region region = SecretLab.Build(ex.Stop.Body.Id, MoonSurface.ExpeditionField(), placement.DoorX, placement.DoorY);
        SecretLab.LabConsole? match = null;
        foreach (SecretLab.LabConsole c in region.Consoles)
        {
            if (((c.X - spot.X) * (c.X - spot.X)) + ((c.Y - spot.Y) * (c.Y - spot.Y)) <= 0.5)
            {
                match = c;
                break;
            }
        }
        if (match is not { } con)
        {
            return;
        }

        string fragment = VantarLore.Fragment(con.LoreIndex);
        if (con.IsCoreLog)
        {
            FireSecretLabReveal(ex, fragment);
            return;
        }
        ex.SecretLabLogsRead.Add(con.Id);
        RendererInterop.PlayCue("board");
        ShowPulseMessage($"🖥 {fragment}");
    }

    // The reveal (owner: "finding what shouldn't exist is a nerve hit + a diced outcome — salvage the tech for
    // pay, or it salvages you"). Dice shown — house law. Fires once; re-reading the core log only re-shows it.
    private void FireSecretLabReveal(SurfaceExcursion ex, string coreLogText)
    {
        if (ex.SecretLabRevealFired)
        {
            RendererInterop.PlayCue("board");
            ShowPulseMessage($"🖥 {coreLogText}");
            return;
        }
        ex.SecretLabRevealFired = true;
        ex.SecretLabLogsRead.Add("lab-log-core");

        SecretLab.RevealRoll roll = SecretLab.RollReveal(
            DiceRule.Seed($"secretlab:reveal:{ex.Stop.Body.Id}", (long)SimTime));
        _nerve = NerveModel.Shock(_nerve, roll.NerveHit);

        string dice = $"🎲 d20: {roll.Face} (≥{SecretLab.SalvageMinRoll} salvages)";
        if (roll.Outcome == SecretLab.RevealOutcome.SalvageTech)
        {
            _credits += roll.PayCredits;
            RendererInterop.PlayCue("reveal");
            ShowPulseMessage(
                $"🖥 {coreLogText}\n\n{dice} — you keep your head and strip the rig for the good stuff. " +
                $"+{roll.PayCredits:N0} cr for the salvaged tech. Your hands aren't quite steady, but they're yours.");
        }
        else
        {
            SpawnReevers(roll.PackSize);
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage(
                $"🖥 {coreLogText}\n\n{dice} — and behind you the dormant thing's eyes come open. " +
                $"{roll.PackSize} of them, standing off their benches. It salvages YOU. Get to the tube — RUN.");
        }
        RequestVaultSave(); // the nerve moved (and maybe the purse) — persist it
    }
}
