using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;
/// <summary>
/// #251 · WHO IS ON THE GROUND, AND WHAT THEY ARE DOING — the nested types the excursion is played with:
/// how many figures a surface plan has to be able to draw (#633, as the sum of its bands rather than a
/// number somebody typed), the slot ledger the patrol and the walkers take theirs from, the Old One, the
/// surface bot, and the four timed channels (a dig, a door, a drill, and #696's darkroom hold).
///
/// <para>Split out of <c>Map.Surface.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    /// <summary>
    /// #633 · HOW MANY FIGURES A SURFACE PLAN HAS TO BE ABLE TO DRAW, in one place, as the sum of its bands
    /// rather than a number somebody typed. The two branches each maintained this expression separately and
    /// each dropped the other's band: one read <c>3 + ReeverEngineCeiling + MaxCollectors</c>, the other
    /// <c>3 + ReeverEngineCeiling + InspectionTeam.TeamSize</c>. Both were correct on their own branch and
    /// both are wrong now, which is precisely why the sum lives here and every caller reads it.
    ///
    /// <para>The bands, in buffer order: the crew (3), the pack (<see cref="ReeverEngineCeiling"/>), the repo
    /// crew (<see cref="MaxCollectors"/>), the sweep team (<c>InspectionTeam.TeamSize</c>), #804's
    /// rounds (<see cref="PatrolBand"/>), and #731's walkers (<see cref="WalkerBand"/>).
    /// <see cref="FillSurfaceDroids"/> writes them at exactly these offsets.</para></summary>
    private const int SurfaceDroidCount =
        3 + ReeverEngineCeiling + MaxCollectors + InspectionTeam.TeamSize + PatrolBand + WalkerBand;

    /// <summary>#804 · Where the rounds' slots start. Stated as the sum of every band before it, so a fifth
    /// filler cannot quietly overwrite a fourth — which is precisely the bug #633 paid for.</summary>
    private const int PatrolFirstSlot =
        3 + ReeverEngineCeiling + MaxCollectors + InspectionTeam.TeamSize;

    /// <summary>#731 · …and where the walkers' slots start: after the rounds, by the same arithmetic and for
    /// the same reason. A guard and a regular finishing a drink are two different kinds of person and never
    /// share a slot.</summary>
    private const int WalkerFirstSlot =
        3 + ReeverEngineCeiling + MaxCollectors + InspectionTeam.TeamSize + PatrolBand;

    internal sealed class Reever
    {
        public double X, Y, Facing, Vx, Vy;
        public int HitsTaken;   // #314: rounds a sentry has ground into it (downs at RoundsPerReever)

        // Lane-1: a TIDE Reever (clawed up from the deep edge, owner 2026-07-18) versus a dig-roll pack
        // member. The tide holds to its home range (never ventures near the landing); the pack chases to
        // the very crew-only door. Same creature, two leashes.
        public bool Tide;

        // #324: crude line-of-sight memory. A Reever only tracks the captain's LIVE position while it can
        // SEE them (no wall between); blind, it shambles to where it last laid eyes, then leans on the tube
        // choke. Duck behind a wall and it loses your live position — the maze becomes a real instrument.
        public double LastSeenX, LastSeenY;
        public bool EverSeen;

        // #436 · THE OBSERVATION ROLL's own two fields. EverSeen above is now the FIXED state of
        // ReeverObservation.Watch and keeps every property it had (one-way, for the excursion, granted by the
        // ear as well as by the eye); these are the two things a contact has to carry BETWEEN looks.
        //
        // Stirred is the fear window: stone has stopped standing between you, the head is up and turned your
        // way, and it has NOT committed — so it does not latch, and backing behind stone puts the head back
        // down. Drawn as a pose change on the existing mark and never spoken (canon, 2026-09-05).
        //
        // LastLookIndex is the cadence's carried state: a die is cast when this number turns over, so a
        // sightline that opens and shuts inside one look never gets one cast at it. long.MinValue is "has
        // never looked", and it must not be 0 — index 0 is a REAL look, the first one of an excursion, and a
        // contact born believing it had already taken that look would silently skip it.
        public bool Stirred;
        public long LastLookIndex = long.MinValue;

        // Thermal motion (owner, cruise 2026-07-19: "the reevers could be more active, like little thermal
        // motion so they don't just stay still"). A STILL Old One — pinned by a sentry, held at its tide
        // leash, or idling on a stalled chase — shivers around a FIXED anchor instead of standing statue.
        // Idle latches the still state and captures the anchor exactly once, so the mean-zero shuffle
        // (ReeverIdle.JitterAt) never creeps the resting spot; JitterSeed fixes this contact's phase so no
        // two shiver in lockstep. Cleared the frame it makes real progress again (back to a live chase).
        public bool Idle;
        public double AnchorX, AnchorY;
        public ulong JitterSeed;

        // #371 Phase 3 (expedition fog of war): is this Old One drawn on the walked MAP right now? True on
        // open ground the ship overwatches; false behind cover (a wall between it and the captain) on an
        // expedition site — the motion tracker still HEARS it (untouched), so a wall-hidden mover reads only
        // as a blip and, when it slips from sight while moving, leaves a fading echo. Always true off an
        // expedition site (no fog there). Client-only, like the position itself.
        public bool VisibleOnMap = true;

        /// <summary>
        /// #488 · HIBERNATING. Owner: <i>"could we have like slumbering reevers that are not immediately
        /// active but begin to wake up once we board the ship … they would not show on map before they
        /// become active … unless they are within our observed vision space … maybe they can hybernate
        /// somehow the 40 years."</i>
        ///
        /// <para>It is the only honest answer to how anything is still aboard after forty years on a hull
        /// with no air plant and nothing to eat — and it is already the reason the vacuum soak has to be
        /// long: the ENCYSTED kind "has done this before and is in no hurry". Same animal, same trick.</para>
        ///
        /// <para>A dormant one does not move, so it is invisible to a MOTION tracker for free. It is not
        /// drawn either — unless the captain can actually SEE it, which is the moment the lamp finds
        /// something folded in a corner that has not moved in four decades and is about to.</para>
        /// </summary>
        public bool Dormant;

        /// <summary>When this one comes round on its own. Noise aboard pulls it earlier.</summary>
        public double WakeAtMs;

        /// <summary>Where a woken-but-unaware one is currently wandering to aboard a wreck, and until when.
        /// Not a search — it does not know there is anyone to search for.</summary>
        public double ProwlX, ProwlY, ProwlUntilMs;

        /// <summary>How long THIS one has been standing in vacuum. Owner: "I pumped the near hold to vacuum
        /// but there are still reevers in it?" — because the kill only ever fired at the instant a room's
        /// soak completed, so anything already inside, or that walked in afterwards, was untouched and a
        /// room at hard vacuum was scenery. Exposure is per-contact now, and it accrues wherever it stands.</summary>
        public double VacuumSeconds;

        // #453: this contact's own swing clock and swing count. Each Old One winds up separately (so a
        // crowd is not a blender) and each swing seeds its own die, so a long fight never repeats a line.
        public double LastSwingMs = double.NegativeInfinity;
        public int Swings;
    }

    // #314: a sentry on the surface — carried in the sling or deployed and holding the line, with its
    // dwindling magazine. Deployed bots fire the SentryBot volley; a firing bot flags a brief zap line.
    public sealed class SurfaceBot
    {
        public required string Unit { get; init; }
        public int Rounds { get; set; }
        public bool Deployed { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double AimX { get; set; }
        public double AimY { get; set; }
        public double FiringUntilMs { get; set; }

        // #326 · WHICH STANCE IT WAS SET DOWN IN. False is the #314 post — it holds the arc it was put on,
        // for ever, which is the right object for guarding a hole in the ground. True is the bodyguard: it
        // walks to the middle of the captain→home line every frame (Map.Surface.Escort) until its counter
        // reads 00, and then it stops where it stands. Chosen at the press, never changed afterwards —
        // shoulder it and set it down again if you want the other one.
        public bool HoldsTheLine { get; set; }

        // #603 · WHAT IS IN IT. Owner: "those rounds might be special types even." A magazine is no longer
        // just a count — it is a count OF SOMETHING, because the lab round clears a line with one shot and
        // will kill you at arm's length, and issue ball does neither.
        public string AmmoId { get; set; } = Core.Ammunition.Issue.Id;
    }

    // The three things a channeled dig can be (beach-comber kit): bury a carried chest where you stand,
    // lift an own cache back up at its ✗, or probe an empty hole to try your luck (the fishing expedition).
    public enum DigKind { Bury, Lift, Probe }

    public sealed class DigChannel
    {
        public double Progress;       // 0..1
        public DigKind Kind;          // bury / lift / probe
        public string? CacheId;       // the cache being lifted (null for a bury or a probe)
        public double AnchorX, AnchorY; // where the shovel bit in — stepping away from HERE aborts, and a
                                        // bury records this spot as the ✗ (free-form, playtest bug #5)
        public int SquareX, SquareY;  // the probe's beach-comber square (unused for bury/lift)
        public ReeverRoll Roll;       // rolled at channel START so the threat can interrupt the bar
        public bool Rolled;           // reevers spawned for this channel
    }

    // #371 Phase 3 · the forced-door channel (owner's "progress bar of forcing a door to open"). Parallel to
    // DigChannel but its own act: several real seconds of shoulder-to-the-door, abortable by stepping away
    // from the door, watched while the away clock ticks (no fresh Reever roll — the site's own diced beats
    // are the threat). On completion the door's REGION APPENDS to the live map.
    public sealed class DoorChannel
    {
        public double Progress;         // 0..1
        public required string DoorId;  // the sealed door being forced (outer or nested)
        public double AnchorX, AnchorY; // the door console — stepping away from HERE aborts
    }

    // #394 · THE DRILLING. The channel that sinks the charge into the rock — parallel to the door-force
    // channel but MUCH longer (DeflectionGig.RockProfile.DrillSeconds, per rock type) and, unlike a door,
    // its Progress PERSISTS across re-channels: a drill-snap complication backs the progress up, and the
    // captain sets the shoulder again from there. Abortable by stepping away from the drill point.
    public sealed class DrillChannel
    {
        public double AnchorX, AnchorY; // the drill point — stepping away from HERE pauses the bore
    }

    // ── #696 · THE DARKROOM CHANNEL ────────────────────────────────────────────────────────────────────
    //
    // Owner: "That is something one would do without using tanked air... we take time to process the loot."
    //
    // The third channel, and deliberately the same shape as the other two: an anchor you have to stand on, a
    // clock, and an effect that fires ONLY at the far end. What makes it different from a dig is that it is
    // silent — a captain photographing a pay sheet is not swinging a shovel — so the teeth are not noise,
    // they are the twenty seconds themselves, watched on the fan.
    //
    // NOTE WHAT IS NOT ON THIS CLASS: anything about air. The hold passes sim time and StepSuitAir prices
    // sim time, and the two never speak. A tank field here would be a second answer to a question that is
    // already answered correctly in one place, on four different kinds of ground (#573/#585/#608/#612).
    private sealed class ProcessingHold
    {
        /// <summary>Which slow thing — the sentences differ, the clock does not.</summary>
        public required Core.Processing.Work Work { get; init; }

        /// <summary>The document under the captain's hands. It is STILL IN THE SATCHEL: nothing is removed
        /// and nothing is filed until the far end, which is what makes "an interruption loses nothing"
        /// structural rather than a promise about tidying up afterwards.</summary>
        public required Core.Satchel.Item Item { get; init; }

        /// <summary>What to call it on screen, composed once at the start, so the abandon line and the start
        /// line cannot disagree about what is in the captain's hands.</summary>
        public required string Label { get; init; }

        /// <summary>Where the boots were when the hold started, and which floor they were on. Drifting off
        /// this spot — or riding the lift — abandons it.</summary>
        public double AnchorX { get; init; }
        public double AnchorY { get; init; }
        public int Floor { get; init; }

        /// <summary>Sim seconds stood so far.</summary>
        public double Elapsed { get; set; }

        /// <summary>#691 · Where the thing is being set down, in the captain's own words, captured at the
        /// START. The captain cannot move during a hold, so this can never go stale — and re-deriving it off
        /// the avatar at the far end would be a second answer to a question already asked.</summary>
        public string Standing { get; init; } = "";

        /// <summary>#603 · What the paper is being read AT, for the clue path. Null on a leave.</summary>
        public (SatchelTry.Target Target, string? Context, string Label)? At { get; init; }
    }

}
