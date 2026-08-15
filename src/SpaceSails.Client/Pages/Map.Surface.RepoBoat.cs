using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the repo crew that comes down for the boat, and the writ it serves.
public partial class Map
{
    // ── #583 · A REPO CREW ON FOOT. Owner: "FBI does not arrest cars ... they look for the driver". ────
    //
    // They are not Old Ones and they do not behave like them: they walk, they spread out, they do not tire,
    // and what happens if one reaches you is a WRIT, not a mauling. Client-owned position, exactly like a
    // Reever's — never saved, rebuilt from the seeded roll on any reload.
    private sealed class Collector
    {
        public double X, Y, Facing;
        public double Vx, Vy;

        // The stable handedness ReeverChase.Step wants so a wall is rounded rather than dithered at. Spread
        // across the party so they flow around a slab from both ends instead of queueing at one corner.
        public int WallSide = 1;
    }

    private readonly List<Collector> _collectors = [];

    // The engine ceiling for the buffer arithmetic below (CollectorLanding.PartySize is clamped to 4).
    private const int MaxCollectors = 4;

    // Lane-1 · THE TIDE (owner, Saturday-evening playtest 2026-07-18): "even with bots there is only so
    // long time to stay there." The deep hands up a Reever at seeded, jittered intervals for the WHOLE
    // excursion — no fixed total ("reevers coming from bottom of screen without any limited number … at
    // random intervals"). This supersedes the old dig-gated linger trickle: the tide runs from the moment
    // the boots hit regolith, not only after a dig, so time in the deep field is bounded on any visit. The
    // acute ReeverRaid pack (BeginDig) still turns out ON TOP of it — the tide is the ambient pressure.
    // ── #583 · THE REPO BOAT COMES DOWN ────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-08-01: "but the heat should not target the ship when the player is not on it but only
    // target the captain... we could have some other shuttle land near ours on some sites ... that would be
    // the heat when we are on land or at a ship looting it" — and, settling it, "FBI does not arrest cars ...
    // they look for the driver".
    //
    // #580 stopped the wolves from catching an empty hull, which was right and left heat meaning nothing
    // during the part of the game the captain is actually in. This is the other half: the collectors come to
    // the person. A boat sets down between you and your ride, a crew gets out, and they walk. They cannot be
    // out-burned out here, only outwalked — and the only door that closes on them is the tube's.
    private void StepCollectors(double dtRealSeconds)
    {
        if (_surface is not { } ex || _busted is not null)
        {
            return;
        }

        double dt = Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds);
        ex.SecondsOnTheGround += dt;

        if (!ex.CollectorsComing)
        {
            return;
        }

        if (!ex.CollectorsLanded)
        {
            if (ex.SecondsOnTheGround < ex.CollectorsEtaSeconds)
            {
                return;
            }
            LandTheCollectors(ex);
            return; // one beat to read the sky before they start walking
        }

        // The maze is law for them too: they bump-and-slide on the SAME segments the captain's boots do, so
        // a building costs them the long way round exactly as it costs an Old One. Unlike an Old One there
        // is no crew-only leash — they came in their own boat and they have their own airlock.
        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        bool reachable = !CaptainBeyondReach;

        foreach (Collector c in _collectors)
        {
            double wasX = c.X, wasY = c.Y;
            (c.X, c.Y) = CollectorLanding.Step(
                c.X, c.Y, _avatarX, _avatarY, dt, walls, DeckPlan.AvatarRadius, c.WallSide);

            // #585: the repo crew waits outside too — which is exactly what their own line already says they
            // do ("they take up positions and settle in to wait"). A writ that walks through the door would
            // make that sentence a lie, and would take the one decision out of the scene: whether to sit on
            // your air or run for the tube.
            (c.X, c.Y) = HoldOutsideShelters(c.X, c.Y);

            c.Vx = dt > 0 ? (c.X - wasX) / dt : 0;
            c.Vy = dt > 0 ? (c.Y - wasY) / dt : 0;
            if (Math.Abs(c.Vx) > 1e-6 || Math.Abs(c.Vy) > 1e-6)
            {
                c.Facing = Math.Atan2(c.Vy, c.Vx);
            }

            // A shelter is a pressure vessel, not a sanctuary — and the game says so out loud rather than
            // letting the player discover it by being taken inside one they thought was safe.
            if (!ex.CollectorShelterNoted && ShelterUnderfoot(ex) >= 0
                && CollectorLanding.HasYou(c.X, c.Y, _avatarX, _avatarY) is false
                && Distance(c.X, c.Y, _avatarX, _avatarY) < 24)
            {
                ex.CollectorShelterNoted = true;

                // #768 · HELD: the siege plate goes up two lines below, and this is the one sentence in the
                // scene that tells a captain the shelter they are standing in will not save them. It is a
                // rule of the world learned once — a Beat — and it was being said under a backdrop.
                HoldSaying(CollectorLanding.ShelterIsNotSanctuaryLine, PulseRank.Beat);

                // #528 · AND THE PICTURE DOES THE SAME JOB THE LINE DOES: it shows them SETTLED, not
                // attacking. Nothing in this frame is a fight. The clock is your tank, and what makes it
                // horrible is how comfortable everyone else looks.
                ShowRevealCard(
                    CollectorLanding.SiegePlate.Title,
                    CollectorLanding.SiegePlate.ArtFile,
                    CollectorLanding.SiegePlate.Caption);
                ReleaseHeldSayingsUnlessACardStopsTheWorld();   // #768 — it does; the shelter line waits
            }

            if (reachable && CollectorLanding.HasYou(c.X, c.Y, _avatarX, _avatarY))
            {
                TheWritIsServed(ex);
                return;
            }
        }
    }

    private static double Distance(double ax, double ay, double bx, double by)
    {
        double dx = ax - bx, dy = ay - by;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>#583 · The boat touches down, off to one side of the tube — near enough to be between you
    /// and the way home, never on top of the hatch (a boat parked on the door would end the excursion by
    /// geometry instead of by decision).</summary>
    private void LandTheCollectors(SurfaceExcursion ex)
    {
        ex.CollectorsLanded = true;
        ex.CollectorBoatX = CollectorLanding.SetsDownX(MoonSurface.SpawnX, ex.ThreatSeed);
        ex.CollectorBoatY = MoonSurface.ReeverBarrierY - 6;

        int party = CollectorLanding.PartySize(_heat.Level);
        _collectors.Clear();
        for (int i = 0; i < party && i < MaxCollectors; i++)
        {
            _collectors.Add(new Collector
            {
                X = ex.CollectorBoatX + ((i - ((party - 1) / 2.0)) * 3.5),
                Y = ex.CollectorBoatY - 2,
                Facing = -Math.PI / 2,
                WallSide = i % 2 == 0 ? 1 : -1,
            });
        }

        RendererInterop.PlayCue("alarm");

        // #768 · HELD, because the plate below goes up in the same breath and both of these would play under
        // its backdrop — the same family as the Hive's arrival, arising the same way: the world acting, not a
        // press on a pop-up. The RANK is what decides which one the captain is left with, and it is a
        // judgement about what the lines ARE: a boat you did not call setting down beside yours is a thing
        // that happened once and the book will keep (Beat); the hail that follows it is radio (Status).
        HoldSaying(CollectorLanding.ArrivalLine(ex.CollectorCallsign), PulseRank.Beat);
        if (!ex.CollectorsHailed)
        {
            ex.CollectorsHailed = true;
            HoldSaying(CollectorLanding.HailLine);
        }

        // #528 · THE ONLY WARNING THE PLAYER GETS. Four loaded lines narrate this pursuit and every one of
        // them was a toast; the arrival is the worst place for that, because after it the only information
        // in the world is a tracker fan. A sentence that fades in a second and a half is not a warning.
        // Core owns the words — and the caption is ClosingLine, which was written, reviewed and shipped and
        // then referenced by nothing at all until now.
        ShowRevealCard(
            CollectorLanding.ArrivalPlate.Title,
            CollectorLanding.ArrivalPlate.ArtFile,
            CollectorLanding.ArrivalPlate.Caption);
        ReleaseHeldSayingsUnlessACardStopsTheWorld();   // #768 — it does, so the two lines wait for the ✕

        // It is a fright, and a specific one: the ground just stopped being only about the Old Ones.
        ApplyNerveShock(4.0, "a boat you did not call, setting down beside yours");
        RequestVaultSave();
    }

    /// <summary>#583 · A hand on your carry loop, on foot, on somebody else's moon. It opens the SAME demand
    /// the same people open on your own deck — submit, bribe, or resist — because it is the same writ and
    /// they want the same thing. What is different is that you walked into it and cannot burn away.</summary>
    private void TheWritIsServed(SurfaceExcursion ex)
    {
        RendererInterop.PlayCue("board");
        ShowPulseMessage(CollectorLanding.ContactLine(ex.CollectorCallsign));

        ulong seed = DiceRule.Seed(ex.ThreatSeed, $"busted-on-foot:{(long)SimTime}");
        _busted = new BustedEncounter
        {
            HunterId = $"collector-ground:{ex.Stop.Body.Id}",
            HunterCallsign = ex.CollectorCallsign,
            Heat = Math.Max(1, _heat.Level),
            Seed = seed,
            Bribe = BustedRule.BribeDemand(Math.Max(1, _heat.Level), seed),
            Cause = DeathCause.Collector,
            DeathBodyName = ex.Stop.Body.Name,
        };

        // #777 · The same demand, so the same beat. It is HOSTED (StoryBeats.Presentation.Hosted): the seam
        // keeps the books and the panel we just opened is the canvas. Raised here as well as on her deck
        // because the hail is a thing that HAPPENS, and this is one of the two places it happens — a beat
        // wired at one of its edges is a beat that silently stops being told at the other.
        RaiseStoryBeat(StoryBeats.Beat.CollectorHail, ex.CollectorCallsign);

        RequestVaultSave();
    }
}
