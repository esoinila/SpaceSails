using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — #251 · split from
// Map.Surface.Reevers.cs, moved verbatim: `ApplyIdleShiver`, the thermal twitch of one standing still;
// `StepTide` and `SpawnTideReever`, the deep filling the field one contact at a time; and `ReeverSeed` and
// `WatchdogLevelAt`, the two facts a ground remembers about its own watchdogs. The shamble itself,
// `StepReevers`, stays in Map.Surface.Reevers.cs.
public partial class Map
{
    // Thermal motion (owner, cruise 2026-07-19: "the reevers could be more active, like little thermal
    // motion so they don't just stay still"). Shiver a STILL Old One around its fixed anchor: a tiny,
    // seeded, mean-zero positional shuffle (ReeverIdle.JitterAt) plus a slow facing twitch. The shuffle is
    // wall-slid from the anchor with the SAME bump-and-slide the shamble uses, so it can never wedge the
    // body through stone even a hair. Velocity is the caller's to zero (option a keeps the fan honest);
    // this only moves the cosmetic position and facing, never the anchor.
    private void ApplyIdleShiver(Reever r, IReadOnlyList<SurfaceCollision.Segment> walls, double radius,
        double t, double baseFacing)
    {
        (double jx, double jy) = ReeverIdle.JitterAt(r.JitterSeed, t);
        // #724 · A SHIVER LOOKS FOR NOTHING. The gait is Stagger both because this is an Old One (the
        // owner's ruling) and because a cosmetic mean-zero twitch that could funnel itself sideways into a
        // doorway would be a still body slowly walking off through the door it happened to be idling beside.
        (r.X, r.Y) = SurfaceCollision.Slide(
            r.AnchorX, r.AnchorY, jx, jy, radius, walls, SurfaceCollision.Gait.Stagger);
        r.Facing = baseFacing + ReeverIdle.FacingTwitchAt(r.JitterSeed, t);
    }
    private void StepTide(double dtRealSeconds)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // #488: the tide is Old Ones clawing UP OUT OF THE REGOLITH. A derelict is a steel hull in vacuum —
        // there is no ground for them to come out of, and a wreck that quietly filled with Reevers would be
        // a different (and unearned) story than the one her evidence tells. Whatever is aboard a wreck gets
        // put there on purpose, not by the ground's own cadence.
        if (SiteRoute.IsHull(ex.Stop.Body.Id))
        {
            return;
        }
        // #318-style guard: clamp the frame delta before it feeds the accumulator so a background-tab
        // resume (rAF suspended, a multi-second delta) can't spawn a wall of Reevers in one frame — and
        // resolve at most MaxTideSpawnsPerFrame claw-outs this frame, letting any backlog trail over the
        // next few. TideSeconds only ever grows by a clamped ≤0.1 s, so in practice this loops 0–1 times.
        ex.TideSeconds += Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds);
        if (ex.TideNextGap <= 0.0)
        {
            ex.TideNextGap = ReeverTide.NextGap(ex.ThreatSeed, ex.TideSpawnIndex);
        }

        int resolved = 0;
        while (ex.TideSeconds >= ex.TideNextGap && resolved < MaxTideSpawnsPerFrame)
        {
            resolved++;
            ex.TideSeconds -= ex.TideNextGap;
            // The engine ceiling is a perf guard, not a gameplay cap: at the ceiling the claw-out is
            // skipped this beat but the tide clock rolls right on, so the deep resumes handing them up the
            // instant a sentry drops one and frees a slot.
            if (_reevers.Count < ReeverEngineCeiling)
            {
                SpawnTideReever(ex);
            }
            ex.TideSpawnIndex++;
            ex.TideNextGap = ReeverTide.NextGap(ex.ThreatSeed, ex.TideSpawnIndex);
        }

        // Don't bank unbounded seconds while pinned at the ceiling — hold at a single gap's worth so the
        // tide resumes promptly (not in a sudden flood) once a slot frees.
        if (_reevers.Count >= ReeverEngineCeiling && ex.TideSeconds > ex.TideNextGap)
        {
            ex.TideSeconds = ex.TideNextGap;
        }

        SayTheFirstStirWhenTheSlotIsFree(ex);
    }

    // One tide Reever claws out of the deep edge at its seeded spawn point and begins to shamble up the
    // field. Silent by design — the motion tracker is the warning, not a klaxon (owner: "they should show
    // in the motion detector long before on the map"); only the first of an excursion earns a line so the
    // player learns the deep is alive. Marked Tide so StepReevers leashes it to the home range.
    private void SpawnTideReever(SurfaceExcursion ex)
    {
        (double x, double y) = MoonSurface.TideSpawnPoint(ex.ThreatSeed, ex.TideSpawnIndex, _avatarX, _avatarY);
        _reevers.Add(new Reever
        {
            // #563 · Facing THE CAPTAIN, not "up". It used to claw out of the bottom rim and could only ever
            // be looking one way; it rises on a ring around the captain now, so half of them would have been
            // born with their back to the only thing on the moon they care about.
            X = x, Y = y, Facing = Math.Atan2(_avatarY - y, _avatarX - x), Tide = true,
            // A distinct phase per tide contact (the spawn index, salted apart from the pack stream) so a
            // deep field of leash-held Old Ones all shiver independently at their home range.
            JitterSeed = (ex.ThreatSeed * 0xD1B54A32D192ED03UL) + (ulong)ex.TideSpawnIndex + 1UL,
        });

        // #459: a tide Reever claws out UNAWARE — it holds the deep it rose into until it sees or hears you
        // (#446's feature; the deep fills up and you meet it by venturing down). But if you are digging right
        // now, it rose into the sound of a shovel: replay the noise so anything in earshot — including the
        // one that just arrived — learns where the hole is. Otherwise MakeNoise only ever reached the Old
        // Ones that already existed when you started digging, and every latecomer was born deaf to it.
        if (ex.Channel is { } digging)
        {
            MakeNoise(digging.AnchorX, digging.AnchorY, ReeverHearing.Noise.Digging);
        }

    }

    /// <summary>
    /// #380 item 3 · THE FIRST STIR, SAID WHEN THE SLOT IS FREE. The one-time tide notice is the natural slot to
    /// say what a Reever IS — the first time the deep stirs, name the Old Ones and the escape (fleeing works;
    /// they want YOU, not loot).
    ///
    /// <para>#1202 · Fable's ruling, 2026-09-29: this first stir is AMBIENCE, not a warning — the tide Reever
    /// rises unaware, deep, and the tracker is the warning. So it pays the courtesy a stringer's word pays: if
    /// the pulse slot is busy it asks again next frame, it never outranks anything and it is never dropped. It
    /// used to be written the moment the first tide Reever rose, and on a stringer's ground it cut her word
    /// about the tin off 1.5 s into a 7.6 s read. Real danger lines (a Reever at range, the tide turning) keep
    /// their rank and still interrupt; they are not this line.</para>
    /// </summary>
    private void SayTheFirstStirWhenTheSlotIsFree(SurfaceExcursion ex)
    {
        if (!TheFirstStirIsWaiting(ex) || _pulse.Message is not null)
        {
            return;
        }

        ex.TideAnnounced = true;
        ShowPulseMessage(FirstStirLine);
    }

    /// <summary>#1202 · The first tide Reever has risen and the stir has not been said yet: it is waiting for the
    /// slot. A stringer's line about the tank waits behind it, so on her ground the order is the suit, her word,
    /// the stir, her tank.</summary>
    private bool TheFirstStirIsWaiting(SurfaceExcursion ex) => !ex.TideAnnounced && _reevers.Exists(r => r.Tide);

    /// <summary>The first stir's words (#380 item 3), unchanged.</summary>
    private const string FirstStirLine =
        "〜 The tracker stirs — something's moving in the deep, far below. The regolith never stays empty for long. Don't linger. Reevers — the Old Ones. They don't want your loot; they want YOU. Grab what you came for and run.";
    // Seed the 2D6 from place + integer-second instant — deterministic, replayable in a test.
    private ulong ReeverSeed(string bodyId) => DiceRule.Seed($"reever:{bodyId}", (long)SimTime);

    // The highest watchdog presence standing over any chest already at this body (the ground's memory).
    private int WatchdogLevelAt(string bodyId)
    {
        int level = 0;
        foreach (TreasureCache c in _caches.CachesAt(bodyId))
        {
            level = Math.Max(level, c.ReeverLevel);
        }
        return level;
    }
}
