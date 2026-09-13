using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #563 · <b>A LOCKED DOOR IS TIME, NEVER A KEY.</b>
///
/// <para>Owner ruling, 2026-09-13, verbatim: <i>"I like the time instead of a key, considering we have
/// firepower and tools. We can create the same effect as needing a key by making it slow, too noisy, or
/// dangerous in other ways."</i> A key would turn a door into an inventory hunt. Three prices replace it —
/// SLOW (the shoulder), NOISY (the hold is heard) and DANGEROUS (a round, and the leaf never comes back) —
/// and this file is the law half of all three.</para>
///
/// <para><b>What this file is most careful about.</b> Every one of these guards was written to go RED on the
/// state of the tree before this lane, and the ones that could not be made to fail that way say so in their
/// own docblock and are asserted against SOURCE instead. A guard that passes on the old behaviour is the
/// fourth named bug class in this repo and it has been shipped here before (#587).</para>
/// </summary>
public sealed class ADoorIsTimeNotAKeyTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Source(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), .. parts]));

    // ── THE STATE MACHINE ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// DESTROYED IS TERMINAL, AND IT IS A HOLE. Passable and transparent forever, and there is no state,
    /// press or verb anywhere in the class that walks back out of it.
    ///
    /// <para>RED by giving <c>Next</c> a <c>State.Destroyed =&gt; State.Shut</c> arm, or by leaving
    /// <c>Passable</c> reading <c>state == State.Open</c> — either one fails here on the first assert it
    /// reaches. (Before the lane the enum had no fourth member at all, so this file does not compile against
    /// the old tree, which is the strongest form of red a new state can have.)</para>
    /// </summary>
    [Fact]
    public void DestroyedIsTerminalPassableAndTransparent()
    {
        Assert.True(LockedDoor.Passable(LockedDoor.State.Destroyed));
        Assert.True(LockedDoor.Transparent(LockedDoor.State.Destroyed));

        // Nothing presses it back into being a door.
        Assert.Equal(LockedDoor.State.Destroyed, LockedDoor.Next(LockedDoor.State.Destroyed));
        Assert.Equal(LockedDoor.State.Destroyed, LockedDoor.Shoot(LockedDoor.State.Destroyed));

        // …and no verb in the class will take it.
        Assert.False(LockedDoor.MayOpen(LockedDoor.State.Destroyed));
        Assert.False(LockedDoor.MayShut(LockedDoor.State.Destroyed));
        Assert.False(LockedDoor.MayForce(LockedDoor.State.Destroyed));
        Assert.False(LockedDoor.MayShootTheLock(LockedDoor.State.Destroyed, armed: true));
        Assert.False(LockedDoor.CanBeForced(LockedDoor.State.Destroyed));

        // THE EXHAUSTIVE HALF, which is what makes this a law rather than four examples: from EVERY state,
        // no reachable move produces a door again once it is gone.
        foreach (LockedDoor.State s in Enum.GetValues<LockedDoor.State>())
        {
            if (LockedDoor.Shoot(s) == LockedDoor.State.Destroyed)
            {
                Assert.Equal(LockedDoor.State.Destroyed, LockedDoor.Next(LockedDoor.Shoot(s)));
                Assert.True(LockedDoor.Passable(LockedDoor.Shoot(s)));
            }
        }
    }

    /// <summary>
    /// A ROUND TAKES A SHUT OR KEYED LEAF AND NOTHING ELSE. An open door has no lock to shoot and a hole has
    /// nothing left of one, so the verb refuses both — which is what stops a captain spending a round on a
    /// doorway they can already walk through.
    ///
    /// <para>RED by writing <c>Shoot</c> as <c>=&gt; State.Destroyed</c> unconditionally: the OPEN case below
    /// then comes back destroyed.</para>
    /// </summary>
    [Fact]
    public void OnlyAShutOrKeyedLeafHasALockToShoot()
    {
        Assert.Equal(LockedDoor.State.Destroyed, LockedDoor.Shoot(LockedDoor.State.Shut));
        Assert.Equal(LockedDoor.State.Destroyed, LockedDoor.Shoot(LockedDoor.State.Locked));
        Assert.Equal(LockedDoor.State.Open, LockedDoor.Shoot(LockedDoor.State.Open));

        Assert.True(LockedDoor.MayShootTheLock(LockedDoor.State.Shut, armed: true));
        Assert.True(LockedDoor.MayShootTheLock(LockedDoor.State.Locked, armed: true));
        Assert.False(LockedDoor.MayShootTheLock(LockedDoor.State.Open, armed: true));

        // …and UNARMED is not a refusal, it is the absence of the verb: nothing is offered anywhere.
        foreach (LockedDoor.State s in Enum.GetValues<LockedDoor.State>())
        {
            Assert.False(LockedDoor.MayShootTheLock(s, armed: false));
        }

        // One round. The owner's brief says one; a tariff that drifted would silently re-price the whole
        // decision, so the number is pinned rather than left to a caller.
        Assert.Equal(1, LockedDoor.RoundsToShootTheLock);
    }

    /// <summary>
    /// THE SLOW ROAD EXISTS AND IT IS THE ONLY OTHER ONE. A keyed leaf may be forced; a shut one may not be,
    /// because hands already open it and offering a 25 s hold for something free would be the control
    /// inventing a cost the world does not charge.
    ///
    /// <para>RED by writing <c>MayForce</c> as <c>state != State.Open</c>: the SHUT case fails.</para>
    /// </summary>
    [Fact]
    public void OnlyAKeyedLeafIsWorthAShoulder()
    {
        Assert.True(LockedDoor.MayForce(LockedDoor.State.Locked));
        Assert.False(LockedDoor.MayForce(LockedDoor.State.Shut));
        Assert.False(LockedDoor.MayForce(LockedDoor.State.Open));
        Assert.False(LockedDoor.MayForce(LockedDoor.State.Destroyed));
    }

    // ── NO DOOR READS A CARRIED ITEM ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE AUDIT, AS A GUARD.</b> Nothing about a door, anywhere in the tree, is decided by something in a
    /// pocket. Asserted two ways, because neither alone is enough:
    ///
    /// <list type="number">
    /// <item><b>By reflection</b> — no public member of <see cref="LockedDoor"/> takes a bool named like a
    /// key, or any parameter at all named <c>hasKey</c>. This is the half that cannot rot: a new
    /// <c>MayOpen(state, hasCard)</c> fails it the day it is written.</item>
    /// <item><b>By source</b> — <c>LockedDoor.cs</c> contains no <c>Satchel</c> or <c>CarriedObject</c>
    /// reference of any kind, so the door law cannot even SEE the pocket.</item>
    /// </list>
    ///
    /// <para>RED against the old tree on every assert: <c>MayOpen(State, bool hasKey)</c>,
    /// <c>MayLock(State, bool hasKey)</c>, <c>Next(State, bool hasKey)</c> and <c>Label(State, bool hasKey)</c>
    /// all carried the parameter this rejects.</para>
    /// </summary>
    [Fact]
    public void NoDoorIsGatedByACarriedItem()
    {
        string[] keyish = ["key", "card", "pass", "token", "badge", "item", "carried", "satchel", "pocket"];

        foreach (MethodInfo m in typeof(LockedDoor).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            foreach (ParameterInfo pi in m.GetParameters())
            {
                string name = pi.Name ?? "";
                foreach (string bad in keyish)
                {
                    Assert.False(
                        name.Contains(bad, StringComparison.OrdinalIgnoreCase),
                        $"LockedDoor.{m.Name} takes '{name}' — a door gated on something carried. " +
                        "Owner ruling 2026-09-13: a locked door is TIME, never a key.");
                }
            }
        }

        // …and the retired members are retired, by name, so a merge cannot quietly bring one back.
        foreach (string gone in new[] { "MayLock", "NoKeyLine", "LockedLine" })
        {
            Assert.Null(typeof(LockedDoor).GetMember(gone, BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault());
        }

        string src = Source("src", "SpaceSails.Core", "LockedDoor.cs");
        Assert.DoesNotContain("Satchel", src, StringComparison.Ordinal);
        Assert.DoesNotContain("CarriedObject", src, StringComparison.Ordinal);
        Assert.DoesNotContain("hasKey", src, StringComparison.Ordinal);
    }

    // ── ONE SOURCE OF TRUTH, THREE CONSTANTS ──────────────────────────────────────────────────────────

    /// <summary>
    /// <b>EACH FORCE PRICE IS SPELLED ONCE.</b> The two five-second constants are the SAME constant now, not
    /// two literals that happen to agree under a comment claiming they match — which is this repo's named
    /// "one source of truth" bug in its quietest form, and exactly what
    /// <c>SurfaceOutpost.ForceSeconds = 5.0</c> was.
    ///
    /// <para>The value equality below would pass on the old tree, so it is NOT the guard: the guard is the
    /// source assert under it, which reads the declaration itself. <b>RED</b> by restoring
    /// <c>public const double ForceSeconds = 5.0;</c> — the equality still passes and the source assert
    /// fails, which is the whole reason it is written this way.</para>
    /// </summary>
    [Fact]
    public void EachForcePriceIsSpelledOnce()
    {
        Assert.Equal(ExpeditionRegions.DoorForceSeconds, SurfaceOutpost.ForceSeconds);

        string outpost = Source("src", "SpaceSails.Core", "SurfaceOutpost.cs");
        Assert.Contains(
            "public const double ForceSeconds = ExpeditionRegions.DoorForceSeconds;", outpost,
            StringComparison.Ordinal);

        // The keyed door keeps its own, larger price: bolts a security system shot home on purpose are not a
        // seal that rotted, and collapsing the two would be the opposite mistake.
        Assert.True(LockedDoor.ForceSeconds > ExpeditionRegions.DoorForceSeconds);
        Assert.InRange(LockedDoor.ForceSeconds, 10, 60);
    }

    /// <summary>
    /// <b>AND EACH CLIENT FORCE LOOP READS THE CONSTANT ITS OWN KIND OWNS.</b> Four holds, four divisions,
    /// and not one bare number among them. Read out of the client's source because the law is about which
    /// symbol the loop names, and a value assert cannot see a symbol.
    ///
    /// <para>RED by replacing any one of those four divisors with its literal (e.g.
    /// <c>dtRealSeconds / 5.0</c>): the matching assert fails and the bare-number sweep under it catches it a
    /// second time.</para>
    /// </summary>
    [Fact]
    public void EveryForceLoopReadsItsOwnKindsConstant()
    {
        (string File, string Divisor)[] loops =
        [
            ("Map.ExpeditionRegions.cs", "dtRealSeconds / ExpeditionRegions.DoorForceSeconds"),
            ("Map.SecretLab.cs", "dtRealSeconds / ExpeditionRegions.DoorForceSeconds"),
            ("Map.Outpost.cs", "dtRealSeconds / SurfaceOutpost.ForceSeconds"),
            ("Map.LabSecurity.cs", "dtRealSeconds / LockedDoor.ForceSeconds"),
        ];

        foreach ((string file, string divisor) in loops)
        {
            string src = Source("src", "SpaceSails.Client", "Pages", file);
            Assert.Contains(divisor, src, StringComparison.Ordinal);

            // …and nobody in that file divides a channel by a number instead.
            foreach (string bare in new[] { "dtRealSeconds / 5", "dtRealSeconds / 25", "dtRealSeconds / 5.0" })
            {
                Assert.DoesNotContain(bare, src, StringComparison.Ordinal);
            }
        }
    }

    // ── NOISY ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE HOLD IS CLATTER AND THE SHOT IS GUNFIRE, ASSERTED THROUGH <see cref="ReeverHearing"/>.</b>
    /// The loudnesses are the owner's own dial and nothing here re-states them as numbers: what is pinned is
    /// the ORDER (a door is louder than a boot and quieter than a shovel; a gun is the loudest thing on the
    /// moon) and the fact that both emitters name a <c>ReeverHearing.Noise</c> rather than a radius.
    ///
    /// <para>RED for the hold by deleting <c>TheHoldIsHeard(ch);</c> from any one of the four loops — the
    /// per-loop assert fails. RED for the shot by pointing <c>ShootTheLockNow</c> at <c>Noise.Clatter</c>:
    /// the last assert fails. Before this lane the first four failed outright, because forcing a door in a
    /// field of thirty Old Ones made no sound at all.</para>
    /// </summary>
    [Fact]
    public void ForcingIsHeardAsClatterAndShootingAsGunfire()
    {
        // The one place the hold's loudness is chosen.
        string heard = Source("src", "SpaceSails.Client", "Pages", "Map.Doors.TheHoldIsHeard.cs");
        Assert.Contains("ReeverHearing.Noise.Clatter", heard, StringComparison.Ordinal);
        Assert.Contains("ch.AnchorX, ch.AnchorY", heard, StringComparison.Ordinal);

        // …and every force loop calls it, on the cadence the shovel set: inside the per-frame step.
        foreach (string file in new[]
                 {
                     "Map.ExpeditionRegions.cs", "Map.SecretLab.cs", "Map.Outpost.cs", "Map.LabSecurity.cs",
                 })
        {
            Assert.Contains("TheHoldIsHeard(ch)", Source("src", "SpaceSails.Client", "Pages", file),
                StringComparison.Ordinal);
        }

        // The shot rings the ear the game already had, at the loudest thing on it.
        string shot = Source("src", "SpaceSails.Client", "Pages", "Map.Doors.ShootTheLock.cs");
        Assert.Contains("ReeverHearing.Noise.Gunfire", shot, StringComparison.Ordinal);
        Assert.DoesNotContain("ReeverHearing.Noise.Clatter", shot, StringComparison.Ordinal);

        // THE ORDER, through the ear itself. A door heard as far as a shovel would make forcing louder than
        // digging, which is not what a man leaning on a frame sounds like.
        Assert.True(ReeverHearing.RangeOf(ReeverHearing.Noise.Footfall)
            < ReeverHearing.RangeOf(ReeverHearing.Noise.Clatter));
        Assert.True(ReeverHearing.RangeOf(ReeverHearing.Noise.Clatter)
            < ReeverHearing.RangeOf(ReeverHearing.Noise.Gunfire));

        // …and the ear is genuinely bounded on both sides, or "inside the range" would select everything and
        // the client's wake/do-not-wake guard would assert nothing (the fifth named bug class).
        double clatter = ReeverHearing.RangeOf(ReeverHearing.Noise.Clatter);
        Assert.True(ReeverHearing.Hears(clatter - 0.5, ReeverHearing.Noise.Clatter));
        Assert.False(ReeverHearing.Hears(clatter + 0.5, ReeverHearing.Noise.Clatter));
    }

    // ── WHAT IS SAID ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE TWO NEW LINES ARE THE AUTHORED ONES, VERBATIM, AND THEY ARE ENUMERATED.
    ///
    /// <para>The reflection half is the part that does not rot: every public string const on the class must
    /// appear in <see cref="LockedDoor.AllProse"/>, so a sentence added next month is swept by every canon
    /// guard in this file on the day it is written rather than whenever somebody remembers. <b>RED</b> by
    /// adding a const and not enumerating it, or by dropping either line out of <c>AllProse</c>.</para>
    /// </summary>
    [Fact]
    public void TheAuthoredLinesAreSaidVerbatimAndEnumerated()
    {
        Assert.Equal("F — SHOOT THE LOCK", LockedDoor.ShootThePlate);
        Assert.Equal(
            "The lock is gone, and the door with it as a door. Nothing behind you closes now.",
            LockedDoor.LockShotLine);

        var prose = new List<string>(LockedDoor.AllProse());
        Assert.Contains(LockedDoor.ShootThePlate, prose);
        Assert.Contains(LockedDoor.LockShotLine, prose);

        // …and EVERY string const on the class is in there, found by reflection rather than by a list
        // somebody has to remember to extend.
        foreach (FieldInfo f in typeof(LockedDoor)
                     .GetFields(BindingFlags.Public | BindingFlags.Static)
                     .Where(f => f.FieldType == typeof(string)))
        {
            Assert.Contains((string)f.GetValue(null)!, prose);
        }

        // …and every state's plate is too, so a fourth state cannot arrive unswept.
        foreach (LockedDoor.State s in Enum.GetValues<LockedDoor.State>())
        {
            Assert.Contains(LockedDoor.Label(s), prose);
        }
    }

    /// <summary>
    /// NOTHING A DOOR SAYS BREAKS CANON, AND NOTHING IT SAYS ANNOUNCES. §8's reserved word is absent; so is
    /// every word that would explain what the halls were for; and so is any sentence that tells the captain
    /// the noise has been heard — the pack arriving through the hole is the telling (#453/#456).
    ///
    /// <para>RED by planting "monolith" in <c>LockShotLine</c>, or by appending "and they heard it" to it.</para>
    /// </summary>
    [Fact]
    public void NoDoorEverExplainsOrAnnounces()
    {
        string[] forbidden =
        [
            // §8's reserved word, and the canon list.
            "monolith", "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect",
            "clone", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
            // …and the announcement, which is the other half of the same law.
            "they hear", "they heard", "alerted", "security alerted", "everything hears",
            "you have been", "detected",
        ];

        foreach (string line in LockedDoor.AllProse())
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
