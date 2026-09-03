using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Outpost — #563 · THE HUT ON THE REGOLITH.
//
// Owner, playtesting Miranda 2026-07-31: "the map is kind of boring... no door or enclosed places",
// "There should be huts . i.e lockable spaces there not just U shapes", and the line that decided what a
// hut IS: "there could be an overrun illegal space there that was abandoned ... some story that tells the
// player about the game universe."
//
// Until this, an ORDINARY landing site could not grow at all. Lab 47 measured it: Miranda, Luna, Phobos,
// Europa and Titan alike, every site answered NEVER — DeckPlan.AppendRegion was reachable only on two of
// the three expedition-site-* rocks. The owner had never seen a landing site expand because it could not
// happen where he plays. This is the lane that fixes that, on every moon, using the machinery that was
// already sitting there.
//
// The thin client: Core (SurfaceOutpost) owns presence, placement, geometry, the cover stories and the
// prose; this composes it onto the live deck, runs the force channel, and spends what is inside.
public partial class Map
{
    // #563 dev cheat (/map?outpost=1): force a hut onto whatever site this excursion lands on, so the lane
    // can be playtested without hunting for a site that seeded one. Documented in docs/testing-guide.md.
    private bool _outpostCheat;

    // #564 dev cheat (/map?air=N): start an excursion with N seconds in the tank instead of a full one, so
    // the point-of-no-return warning can be reached in a short walk rather than a six-minute one.
    /// <summary>#588 · The outpost hut's own slot in the "whose kit was this" tally. Negative so it can
    /// never collide with a ruin's room index — the hut is one room per site, the ruins are many.</summary>
    private const int OutpostKitIndex = -101;

    // #585 dev cheat (?floor=N): ride straight down to BN on landing, so a floor can be inspected without
    // finding the shed and working the lift. Clamped to the site's real depth.
    private int? _startingFloorCheat;

    private double? _airCheatSeconds;

    // #696 dev cheat (?process=N): how long one document takes to process, in sim seconds — 0 for instant.
    // Null is the shipped feel (Core.Processing.SecondsPerDocument). Twenty seconds of standing still is the
    // whole point of the mechanic and exactly the wrong thing to make a story test sit through, and a test
    // that waits out a clock designed to be FELT is a test that gets deleted the first time it flakes.
    // Combine with dock/site/land: /map?dock=the-tilt&site=0&land=1&process=0
    private double? _processCheatSeconds;

    // #585 dev cheat (?body=ID): which body ?land=1 should put the shuttle on, regardless of what happens to
    // be nearest. Without it only the berth's closest moon was ever reachable by URL.
    private string? _forcedLandingBodyId;

    // #583 dev cheat (?collectors=N): force a repo boat down N seconds into the excursion, whatever the heat
    // gauge says. The scene is deliberately rare and mid-mission, which makes it near-impossible to reach on
    // purpose — and a scene nobody can reach on demand is a scene that ships broken.
    private double? _collectorCheatSeconds;

    /// <summary>Compose every hut on the ground the captain is currently carrying: the dogged hatch while it
    /// stands, the whole room once it has been forced. Called from <c>RebuildSurfaceDeck</c>, so it survives
    /// every rebuild (a dig, a lifted cache, a tile crossing) exactly like the lab does.
    ///
    /// <para>#563 · It used to compose ONE hut, because a site had one. A site is a lattice now and each tile
    /// seeds its own at the same per-area rarity, so this walks the carried chunk. Everything the captain did
    /// to a hut is looked up by that hut's TILE — force one and the one you forced is the one that opens.</para></summary>
    private void ComposeOutpost(SurfaceExcursion ex)
    {
        var walls = new List<DeckPlan.Wall>();
        var labels = new List<(float X, float Y, string Text)>();
        var consoles = new List<DeckPlan.ConsoleSpot>();

        foreach (SurfaceOutpost.Placement p in HutsInReach(ex))
        {
            SurfaceOutpost.OutpostCover cover =
                SurfaceOutpost.CoverFor(ex.Stop.Body.Id, ex.Site.LayoutSalt, p.Tile);

            if (!HutRemembers(ex, p.Tile, GroundMemory.HutChange.Forced))
            {
                // Unforced, the hut is a LANDMARK you can see from across the ground, not a secret you must
                // detect. That is deliberate: the owner's design is that a captain plans a route out and back
                // (#562), and you cannot plan toward something invisible. The hut is the reason to go; the
                // walk home is the price. Only the hatch is on the plan — the room behind it is not drawn yet.
                consoles.Add(new(DeckPlan.ConsoleKind.OutpostDoor, (float)p.DoorX, (float)p.DoorY,
                    SurfaceOutpost.DoorLabel(cover)));
                continue;
            }

            SurfaceOutpost.Region region = SurfaceOutpost.Build(ex.Stop.Body.Id, ex.Site.LayoutSalt, p);
            foreach (SurfaceLayout.Wall w in region.Walls)
            {
                walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, w.IsHull));
            }
            foreach (SurfaceLayout.Landmark m in region.Landmarks)
            {
                labels.Add(((float)m.X, (float)m.Y, m.Label));
            }
            foreach (SurfaceOutpost.OutpostConsole c in region.Consoles)
            {
                // A locker already emptied and effects already read leave nothing to press — the room keeps
                // its walls and its landmark, so it still reads as a place you have been.
                bool spent = c.Kind switch
                {
                    SurfaceOutpost.OutpostConsoleKind.AmmoCache =>
                        HutRemembers(ex, p.Tile, GroundMemory.HutChange.Emptied),
                    _ => HutRemembers(ex, p.Tile, GroundMemory.HutChange.Read),
                };
                if (spent)
                {
                    continue;
                }
                DeckPlan.ConsoleKind kind = c.Kind == SurfaceOutpost.OutpostConsoleKind.AmmoCache
                    ? DeckPlan.ConsoleKind.OutpostCache
                    : DeckPlan.ConsoleKind.OutpostEffects;
                consoles.Add(new(kind, (float)c.X, (float)c.Y, c.Label));
            }
        }

        if (walls.Count == 0 && consoles.Count == 0 && labels.Count == 0)
        {
            return;
        }
        _deckPlan.AppendRegion(new DeckPlan.DeckRegion(
            [.. walls], [.. consoles], [.. labels], []));
    }

    // ── Forcing the hatch [E]: the same channelled bar every other forced door uses, abortable by stepping
    //    away. The tracker keeps sweeping the whole time, which is the actual price of the room. ──
    private void OutpostDoorInteract()
    {
        if (_surface is not { } ex || AnySlowThingUnderYourHands)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.OutpostDoor } spot)
        {
            return;
        }
        if (HutUnderYourHand(ex, spot.X, spot.Y) is not { } hut)
        {
            return;
        }

        ex.OutpostDoorTile = hut.Tile;
        ex.OutpostDoorChannel = new DoorChannel { DoorId = "outpost", AnchorX = spot.X, AnchorY = spot.Y };
        RendererInterop.PlayCue("board");
        ShowPulseMessage(
            "⚙ Setting the bar to the hatch… hold position. It is dogged from the inside, which is its own " +
            "kind of answer. Step away to abort.");
    }

    private void StepOutpostDoorChannel(double dtRealSeconds)
    {
        if (_surface is not { OutpostDoorChannel: { } ch } ex)
        {
            return;
        }

        double dx = _avatarX - ch.AnchorX, dy = _avatarY - ch.AnchorY;
        if ((dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius)
        {
            ex.OutpostDoorChannel = null;
            ShowPulseMessage("You step back and the bar comes off. The hatch stays shut.");
            return;
        }

        ch.Progress += dtRealSeconds / SurfaceOutpost.ForceSeconds;
        if (ch.Progress >= 1.0)
        {
            ex.OutpostDoorChannel = null;
            ForceOutpostHatch(ex);
        }
    }

    /// <summary>The hatch gives and the hut joins the live plan — walls, landmark and what is still worth
    /// pressing. The map grew, so the #563 card fires here too if this is the captain's first time.</summary>
    private void ForceOutpostHatch(SurfaceExcursion ex)
    {
        if (ex.OutpostDoorTile is not { } tile)
        {
            return;
        }

        RememberHut(ex, tile, GroundMemory.HutChange.Forced);

        // #573 · THE JOB COSTS AIR. Levering a dogged hatch is not a five-second thing; the five seconds you
        // held E is the game being polite about it. The clock jumps to the far side of the work and the
        // suit is charged for the whole of it — which is what makes an eight-hour tank a real budget rather
        // than a number nobody could spend.
        ex.AirSeconds = SuitAir.Drain(ex.AirSeconds,
            SuitAir.CostOfTask(ForcingAHatchMinutes, SuitAir.Breathing.HeavyLabour));
        ShowPulseMessage(SuitAir.TaskCostLine("Working that hatch off its dogs took", ForcingAHatchMinutes));

        RebuildSurfaceDeck();   // the hatch console goes, the room arrives — one rebuild, no teleport
        RendererInterop.PlayCue("reveal");

        SurfaceOutpost.OutpostCover cover =
            SurfaceOutpost.CoverFor(ex.Stop.Body.Id, ex.Site.LayoutSalt, tile);
        ShowPulseMessage(SurfaceOutpost.ForcedLine(cover));

        // The ground just grew. On an ordinary moon this is now the FIRST way that can ever happen to a
        // captain, so the card that explains it belongs here more than anywhere.
        ShowGroundGrewCardOnce();
        RequestVaultSave();
    }

    // ── The ammunition locker [E]: rounds across the sentries you are carrying. ──
    private void OutpostCacheInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.OutpostCache } at)
        {
            return;
        }
        if (HutUnderYourHand(ex, at.X, at.Y) is not { } hut
            || HutRemembers(ex, hut.Tile, GroundMemory.HutChange.Emptied))
        {
            return;
        }

        int rounds = SurfaceOutpost.CacheRounds(ex.Stop.Body.Id, ex.Site.LayoutSalt, hut.Tile);

        // Spread across every sentry you have with you — carried OR planted. A bot standing out on the line
        // is exactly the one you want fed, and unlike the tube (#562) you are the one walking the rounds to
        // it, so there is nothing dishonest about reaching a deployed one here.
        var takers = ex.Bots.Where(b => b.Rounds < SentryBot.MaxMagazine).ToList();
        if (takers.Count == 0)
        {
            ShowPulseMessage(
                "🔫 Their locker is full of good rounds and you have nothing to put them in. " +
                "It stays where it is — you will remember where.");
            return;
        }

        int left = rounds;
        foreach (SurfaceBot bot in takers)
        {
            int take = System.Math.Min(SentryBot.MaxMagazine - bot.Rounds, (left / takers.Count) + 1);
            take = System.Math.Min(take, left);
            bot.Rounds += take;
            left -= take;
            if (left <= 0)
            {
                break;
            }
        }

        // #803 · …and then TOP THEM UP. The even spread above hands each drum a share of what is left at the
        // moment it is reached, so with two takers and forty rounds it walks off leaving nine on the shelf
        // and both magazines short — the receipt said forty, the guns got thirty-one, and nobody could see
        // the difference. The spread stays (a hut's find is meant to reach every gun, not the first one);
        // this second pass is what makes it arithmetic instead of an approximation.
        foreach (SurfaceBot bot in takers)
        {
            if (left <= 0)
            {
                break;
            }
            int take = System.Math.Min(SentryBot.MaxMagazine - bot.Rounds, left);
            bot.Rounds += take;
            left -= take;
        }

        RememberHut(ex, hut.Tile, GroundMemory.HutChange.Emptied);
        RebuildSurfaceDeck();
        RendererInterop.PlayCue("board");
        ShowPulseMessage(SurfaceOutpost.CacheLine(rounds - left));

        // #803 · Whatever the drums genuinely could not hold is a thing the captain owns, not a thing that
        // stops existing — and it is the supply the put verb spends.
        WhatTheDrumsCouldNotHold(left);
        RequestVaultSave();
    }

    // ── Somebody's effects [E]: the only story this place tells, and it tells it by what is in a wallet. ──
    private void OutpostEffectsInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.OutpostEffects } at)
        {
            return;
        }
        if (HutUnderYourHand(ex, at.X, at.Y) is not { } hut
            || HutRemembers(ex, hut.Tile, GroundMemory.HutChange.Read))
        {
            return;
        }

        SurfaceOutpost.OutpostCover cover =
            SurfaceOutpost.CoverFor(ex.Stop.Body.Id, ex.Site.LayoutSalt, hut.Tile);
        RememberHut(ex, hut.Tile, GroundMemory.HutChange.Read);
        RebuildSurfaceDeck();

        // #588 · FILED, AND IT COUNTS AS A PIECE OF SOMEBODY'S KIT. Owner, finding the hut mid-playtest: "now
        // I found something?" — and it was the one console in the game most obviously ABOUT a person that
        // neither survived being read nor contributed to working out who they were.
        //
        // It was still on the transient pulse, so the last effects of somebody who died out here faded in
        // eight seconds; and the dossier only counted ruin papers, so the literal PERSONAL EFFECTS were the
        // one kit in the game that assembled nobody. Both were oversights of mine from an hour ago, and this
        // is the console that makes the omission obvious.
        ShowAndFile(SurfaceOutpost.EffectsLine(cover), "🚹");
        AssembleSomebody(ex, ex.Stop.Body.Id, ex.Site.LayoutSalt, OutpostKitIndex);

        // #528 · AND IT LOOKS LIKE SOMETHING. The one console in the game most obviously ABOUT a person had
        // no picture of its own — the dossier's art is the dossier's. One canvas for all four covers, on the
        // wrecks' anti-tell law (Derelict.LogArtFile): the objects are the same four in every hut, and
        // EffectsLine already carries the whole of the difference.
        //
        // #736 · The plate carries EffectsLine as its outcome, because EffectsLine is the whole of what makes
        // one hut different from another and the picture is deliberately the same in all four. Filed above
        // and read here: the book keeps it, and the card the press raised says it where the eye already is.
        //
        // #664 · ONCE PER HUT, which is what the subject is for. The picture is deliberately the same in all
        // four covers, so a per-beat cadence would have had to choose between showing every hut the identical
        // frame and showing exactly one hut ever — and the second is worse, because EffectsLine is a
        // DIFFERENT person's last hour each time and it rides this card. The subject is the site, not the
        // body: two huts on one moon are two people.
        RaiseStoryBeat(StoryBeats.Beat.OutpostEffectsRead,
            $"{ex.Stop.Body.Id}#{ex.Site.LayoutSalt}#{hut.Tile.X}_{hut.Tile.Y}",
            outcome: SurfaceOutpost.EffectsLine(cover));

        // Reading somebody's last effects on a floor they did not walk off costs a little nerve. Small: the
        // place is long cold, and the captain is a pirate. It is the recognition that stings, not the fright.
        ApplyNerveShock(OutpostEffectsChill, "what was left on the floor of somebody else's bad idea");
        RequestVaultSave();
    }

    /// <summary>The nerve cost of reading the effects. Deliberately a lump and not a shock — this is a cold
    /// room and an old story, and the horror is entirely in the arithmetic the captain does themselves.</summary>
    private const double OutpostEffectsChill = 4.0;

    /// <summary>How long forcing a dogged hatch actually takes, in fiction-minutes — the figure the suit is
    /// charged for. Enough to matter on a long walk, nowhere near enough to threaten a captain who came
    /// straight out from the tube.</summary>
    private const double ForcingAHatchMinutes = 35.0;
}
