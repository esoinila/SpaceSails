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

        // ── #411 · THE HEAD OFFICE IS NOT A ROLL. ────────────────────────────────────────────────────────
        //
        // Every other clandestine site in the game is a one-in-forty fact about a moon. The one under the
        // ice is a fact about the CAPTAIN: it is there when the berth-code has resolved and the hull is on
        // the board, and it is not there otherwise — not sealed, not refused, not hinted at. Featureless ice
        // and a good view.
        //
        // That refusal-by-ABSENCE is the whole reason the arrival lands, and it is the honest reading of an
        // arc every one of whose shards is about a filing, a window, a berth or a manifest and not one of
        // which is about fuel: nobody is stopping you going to Enceladus. What nobody can do is be EXPECTED
        // there. (docs/features/kaamos-head-office.md §1.)
        //
        // And when it IS there, the door is already known — the arc sold you the coordinate over a counter
        // (`bought-coordinate`, "you have the where and the when"), so making the captain sweep a 310 × 260
        // field with a detector for a door they were handed would be the game forgetting its own fiction.
        if (UndergroundComplex.IsHeadOffice(body))
        {
            if (!UndergroundComplex.HeadOfficePresent(body, _kaamos.CanReachEnceladus) && !cheat)
            {
                ex.Lab = null;
                return;
            }

            ex.Lab = SecretLab.For(body, MoonSurface.ExpeditionField(), forcePresent: true);
            ex.SecretLabDoorRevealed = true;
            return;
        }

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

        // #528 · THE GROUND STOPS BEING GROUND. The only find in the beach-comber lane that is not a thing
        // you pick up, and it was a sentence that faded in a second and a half — under a decision (force it,
        // or walk away and pretend you never found it) that is one of the sharpest in the game. Core owns
        // the words; the picture shows the door and nothing about what is behind it.
        //
        // #736 · And the sentence that names the DECISION ("force it open — or walk away") rides the card,
        // because the card comes up on the same sweep and the decision is the thing the captain has to read.
        //
        // #664 · ONCE PER MOON. There is one lab per body, so this cannot repeat on the same subject anyway —
        // what the cadence buys is the guarantee that the NEXT moon's buried door is still a moment, which is
        // exactly what a once-ever card would have taken away. Deferrable: the ground is quiet when a
        // detector finds this, and if it is not, the decision it names will keep.
        RaiseStoryBeat(StoryBeats.Beat.SecretLabDoorFound, ex.Stop.Body.Id,
            outcome: "📡 The detector SHRIEKS and holds — not a coin, not scrap: a SEALED DOOR, buried flush " +
                "with the regolith where no door has any right to be. Someone hid this. Force it open ([E] " +
                "at the door) — or walk away and pretend you never found it.");
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

        // Nothing to add until the door is at least revealed — don't grow the plan (or its region count) on a
        // rebuild for a lab still fully hidden.
        if (walls.Count == 0 && consoles.Count == 0 && labels.Count == 0)
        {
            return;
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
            if (rc.Kind == SecretLab.LabConsoleKind.KeyCard && _hasVantarCard)
            {
                continue;   // taken; the chair it hung on stays, the card does not
            }

            DeckPlan.ConsoleKind kind = rc.Kind switch
            {
                SecretLab.LabConsoleKind.DiscoveryCache => DeckPlan.ConsoleKind.LabCache,
                // #409+ · the two panels and the card get their own kinds, because they are three different
                // verbs and a captain must be able to tell which one they are standing at before pressing E.
                SecretLab.LabConsoleKind.DoorBoard => DeckPlan.ConsoleKind.LabDoorBoard,
                SecretLab.LabConsoleKind.AlarmPanel => DeckPlan.ConsoleKind.LabAlarm,
                SecretLab.LabConsoleKind.KeyCard => DeckPlan.ConsoleKind.LabKeyCard,
                _ => DeckPlan.ConsoleKind.LabConsole,
            };
            consoles.Add(new(kind, (float)rc.X, (float)rc.Y, rc.Label));
        }

        // ── THE DOORS. A shut one is a WALL, which is the whole of #465 applied on the ground: opacity and
        //    solidity are different properties and a door happens to have both, so a closed door is added as a
        //    wall segment and an open one is not. The console is always there, or a captain could not reopen
        //    what they shut.
        foreach (SecretLab.LabDoor d in region.Doors)
        {
            LockedDoor.State state = _labDoors.TryGetValue(d.Id, out LockedDoor.State s) ? s : LockedDoor.State.Shut;

            if (!LockedDoor.Passable(state))
            {
                walls.Add(new((float)d.X, (float)(d.Y - LabDoorHalf), (float)d.X, (float)(d.Y + LabDoorHalf),
                              false, false));
            }

            consoles.Add(new(DeckPlan.ConsoleKind.LabDoor, (float)d.X, (float)(d.Y - LabDoorHalf - 0.9),
                             $"{LockedDoor.Label(state, _hasVantarCard)} · {d.Deeper}"));
        }

        // ── #822 · AND THE CRAWL, which is the same law one more time: shut is a WALL. Owner's ruling on
        //    the fire-code sweep — "the second exit is itself hidden ... two doors nobody can see." It is
        //    NOT a lab door: it never appears on the board, the lockdown cannot throw it, and it carries no
        //    plate naming what is on the other side. A captain meets it by walking to the back wall of the
        //    deepest room in the mountain and finding that the rock there answers.
        foreach (SecretLab.HiddenWay way in region.TheHidden)
        {
            if (ex.SecretLabCrawlForced)
            {
                continue;   // forced: the plug is gone and the mountain has a back door
            }
            walls.Add(new((float)way.Plug.X1, (float)way.Plug.Y1,
                          (float)way.Plug.X2, (float)way.Plug.Y2, false, way.Plug.IsHull));
            consoles.Add(new(DeckPlan.ConsoleKind.SecretDoor, (float)way.X, (float)way.Y,
                             "⚙ THE ROCK HERE RINGS HOLLOW — set your shoulder to it"));
        }
    }

    /// <summary>Half the height of a lab doorway. The same 1.6 the hidden door's own gap uses, so every opening
    /// in the mountain is the same size and a captain never has to judge one by eye.</summary>
    private const double LabDoorHalf = 1.6;

    // ── Forcing the hidden door [E]: a channeled progress bar (the #393 door-force idiom), abortable. ──
    private void SecretDoorInteract()
    {
        if (_surface is not { } ex || AnySlowThingUnderYourHands || ex.Lab is not { HasLab: true })
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.SecretDoor } spot)
        {
            return;
        }
        // #822 · The same console kind serves the crawl at the back of the heart, and it is told apart by
        // WHERE THE CAPTAIN IS rather than by a second kind: the front door's own spot is removed the moment
        // it is forced, so inside the lab this is the only one of these there is. One verb, one channel, one
        // progress bar — the owner's hidden second exit reusing the hidden first exit's whole flow.
        bool crawl = ex.SecretLabForced;
        ex.SecretLabDoorChannel = new DoorChannel
        {
            DoorId = crawl ? "labcrawl" : "secretlab", AnchorX = spot.X, AnchorY = spot.Y,
        };
        RendererInterop.PlayCue("board");
        ShowPulseMessage(crawl
            ? "⚙ Setting your shoulder to the hollow rock… hold position. It moves. Somebody meant it to. Step away to abort."
            : "⚙ Setting your shoulder to the hidden door… hold position. Whatever's behind it has waited a long time. Step away to abort.");
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
            if (ch.DoorId == "labcrawl")
            {
                ForceLabCrawl(ex);
            }
            else
            {
                ForceSecretLabDoor(ex);
            }
        }
    }

    // #822 · The crawl gives. The plug comes out of the geometry on the next compose — the deck is rebuilt
    // rather than appended to, because this is a wall going AWAY and every other force in this file is a
    // region arriving. RebuildSurfaceDeck replays the whole site, crawl included, which is the same path a
    // bury/lift/drop already takes.
    private void ForceLabCrawl(SurfaceExcursion ex)
    {
        if (ex.Lab is not { HasLab: true } placement)
        {
            return;
        }
        ex.SecretLabCrawlForced = true;

        SecretLab.Region region = SecretLab.Build(
            ex.Stop.Body.Id, MoonSurface.ExpeditionField(), placement.DoorX, placement.DoorY);
        string line = region.TheHidden.Count > 0 ? region.TheHidden[0].Line : "";

        RebuildSurfaceDeck();
        RequestVaultSave();
        RendererInterop.PlayCue("reveal");
        ShowPulseMessage($"⛏ The plate shifts and grates aside. {line} It goes OUT.");
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

        // #409+ · A DOOR TAKEN OFF ITS FRAME IS WHAT THE HOUSE IS LISTENING FOR. Forcing, not opening — a door
        // worked properly is a door the system has no opinion about. This is also where the doors are given
        // their forty-years-ago state (shut, not keyed) and where the muscle stands up.
        ArmTheLabAlarm(SecretLab.Build(
            ex.Stop.Body.Id, MoonSurface.ExpeditionField(), placement.DoorX, placement.DoorY).Doors);

        var walls = new List<DeckPlan.Wall>();
        var labels = new List<(float X, float Y, string Text)>();
        var consoles = new List<DeckPlan.ConsoleSpot>();
        AppendSecretLabGeometry(ex, placement, walls, labels, consoles);
        _deckPlan.AppendRegion(new DeckPlan.DeckRegion(
            walls.ToArray(), consoles.ToArray(), labels.ToArray(), []));
        _deckPlan.RemoveConsoleAt((float)placement.DoorX, (float)placement.DoorY, DeckPlan.ConsoleKind.SecretDoor, 0.3);

        ApplyNerveShock(SecretLabEntryChill, "the cold breath of the place behind the door");
        RequestVaultSave();
        RendererInterop.PlayCue("reveal");
        ShowPulseMessage(
            "⚙ The seal cracks — stale, chemical air, decades unbreathed. Benches. Stasis pods. A spine of dead " +
            "servers. Someone LIVED down here, working. Read the logs ([E] the screens) — and mind the core log.");

        // #563 · Unlike the expedition door, this toast is kept ALWAYS — it is a story beat about what is in
        // the room, and the card is a mechanics lesson about the map growing. Different jobs, so the card
        // rides on top the first time rather than replacing anything. (It may well be a captain's first
        // expansion ever: the lab is the ONLY way an ordinary moon can grow at all today.)
        ShowGroundGrewCardOnce();
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

        // #411: the log that gestures at the sealed ice-moon project (VantarLore.KaamosHook) is also a KAAMOS
        // intel shard. Reading it the first time files vantar-log to the ledger; a re-read shows the log plainly.
        //
        // The log NEVER names the project — "a moon off the charts, a project that runs on in the cold with
        // the lights off" is the whole point of VantarLore's fragment 4, and the connection to the plate at
        // Ringside is the player's to make. This line used to make it for them ("This is a piece of PROJEKTI
        // KAAMOS"), which is the announcing shape the house forbids. It files the shard and says no more.
        if (con.LoreIndex == VantarLore.KaamosHook
            && TryAssembleKaamos("vantar-log",
                $"🖥 {fragment}   ❄ Filed to the Captain's ledger. " +
                KaamosLore.ById("vantar-log")!.Lore))
        {
            return;
        }

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
        ApplyNerveShock(roll.NerveHit, "what the lab was built to keep");

        string dice = $"🎲 d20: {roll.Face} (≥{SecretLab.SalvageMinRoll} salvages)";
        if (roll.Outcome == SecretLab.RevealOutcome.SalvageTech)
        {
            _credits += roll.PayCredits;
            RendererInterop.PlayCue("reveal");
            ShowPulseMessage(
                $"🖥 {coreLogText}   ▪   {dice} — you keep your head and strip the rig for the good stuff. " +
                $"+{roll.PayCredits:N0} cr for the salvaged tech. Your hands aren't quite steady, but they're yours.");
            // #400 §3: you looked the thing in the eye and kept your head — the record wants your face doing it.
            // Same fault as the monolith beat (#480 art pass): this shipped with no backdrop, so "STILL
            // STANDING" posed the captain against nothing at all. The lab's own cold room is the vista.
            OfferSelfie(SelfieBeats.RevealSurvived, "art/selfie-reveal-survived.jpg");
        }
        else
        {
            SpawnReevers(roll.PackSize);
            RendererInterop.PlayCue("alarm");

            // #528 · THE HIVE'S LOUDEST MOMENT, and it had no frame at all — the other branch of this very
            // roll ends in a painted selfie against this same room. Raised only here, strictly AFTER the D20
            // has resolved and been shown, so it can never be a tell: the captain already knows which way it
            // went before the picture arrives. The salvage branch keeps its selfie and gets no card, because
            // a card on both would be a card on a card.
            //
            // #736 · The die, the count and the order to RUN ride the card. The house law is that the die is
            // SHOWN; a d20 shown under a backdrop's blur is a die nobody can argue with, which is the exact
            // thing showing it exists to prevent.
            //
            // #664 · AND IT IS THE CLEAREST `DeferrableWhileInDanger = false` IN THE GAME. The statement above
            // this comment is SpawnReevers — so CaptainIsInDanger() is true at the instant of the raise, every
            // single time, and a deferrable card here would not "wait for a calmer moment", it would wait for
            // the pack to be dead and then tell the captain to RUN. Once per moon, because there is one lab
            // per body and the next moon's is a fresh roll with a different die on it.
            RaiseStoryBeat(StoryBeats.Beat.TheDormantThingWakes, ex.Stop.Body.Id,
                outcome: $"🖥 {coreLogText}   ▪   {dice} — and behind you the dormant thing's eyes come open. " +
                    $"{roll.PackSize} of them, standing off their benches. It salvages YOU. Get to the tube — RUN.");
        }
        RequestVaultSave(); // the nerve moved (and maybe the purse) — persist it
    }
}
