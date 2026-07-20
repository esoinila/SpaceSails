namespace SpaceSails.Core.Tests;

/// <summary>
/// #409 — THE SECRET LABS BEHIND HIDDEN DOORS. These pin the pure Core spine: the seeded, deterministic
/// hidden-door placement (rare, and rarer on ordinary moons); that the door's beach-comber square is the
/// square a probe must ping; the forced lab region — enclosed, clamped inside the field, with its distinct
/// contents (a fat cache, Vantar's log consoles including the reveal-trigger core log, the backup rig, the
/// dormant synthetic) and a walkable door→console lane; the diced reveal (both outcomes reachable, dice
/// shown, pay/pack in range); and Vantar's lore pool. Determinism is law in Core, so every one is exact.
/// </summary>
public class SecretLabTests
{
    // The same field envelope MoonSurface hands in (mirrors MoonSurface's constants).
    private static readonly SurfaceLayout.Field Env = new(
        LeftX: -44, RightX: 34, TopY: -20, BottomY: -84, LandingBandY: -27, AnchorX: -6, AnchorY: -70);

    private const double AvatarRadius = 0.7; // DeckPlan.AvatarRadius

    // ── Seeded presence: deterministic, rare, and rarer on ordinary moons than expedition sites ──

    [Fact]
    public void For_IsDeterministic_PerBodyAndField()
    {
        foreach (string id in new[] { "miranda", "luna", "phobos", "expedition-site-ruins" })
        {
            SecretLab.Placement a = SecretLab.For(id, Env);
            SecretLab.Placement b = SecretLab.For(id, Env);
            Assert.Equal(a, b); // record-struct value equality — same body, same hidden door, same odds verdict
        }
    }

    [Fact]
    public void Present_IsRare_ButReachable_AndForceOverrides()
    {
        // Across a broad sweep of ordinary body ids, SOME hide a lab and MOST do not — rare, never impossible.
        int present = 0, total = 400;
        for (int i = 0; i < total; i++)
        {
            if (SecretLab.Present($"moon-{i}"))
            {
                present++;
            }
        }
        Assert.InRange(present, 1, total / 3);            // genuinely rare
        Assert.True(SecretLab.For("moon-empty-xyz", Env, forcePresent: true).HasLab); // the cheat always lands one
    }

    [Fact]
    public void ExpeditionSites_HideLabs_MoreOften_ThanOrdinaryMoons()
    {
        // The odds constants encode "rarer on an ordinary moon" — the veterans' rumor points at the deep field.
        Assert.True(SecretLab.ExpeditionOneInN < SecretLab.OrdinaryOneInN);
    }

    // ── The hidden door sits on a beach-comber square in the deep field, and THAT square is the reveal ──

    [Fact]
    public void DoorSquare_MatchesBeachComberSquareOf_DoorPosition()
    {
        SecretLab.Placement p = SecretLab.For("phobos", Env, forcePresent: true);
        (int sx, int sy) = BeachComber.SquareOf(p.DoorX, p.DoorY);
        Assert.Equal(sx, p.DoorSquareX);
        Assert.Equal(sy, p.DoorSquareY);
        Assert.True(SecretLab.IsDoorSquare(p, sx, sy));
        Assert.False(SecretLab.IsDoorSquare(p, sx + 3, sy));
    }

    [Fact]
    public void ProximitySquare_ShrieksAroundTheDoor_ButNotFar()
    {
        SecretLab.Placement p = SecretLab.For("phobos", Env, forcePresent: true);
        Assert.True(SecretLab.IsProximitySquare(p, p.DoorSquareX, p.DoorSquareY));
        Assert.True(SecretLab.IsProximitySquare(p, p.DoorSquareX + 1, p.DoorSquareY - 1)); // a neighbour pings
        Assert.False(SecretLab.IsProximitySquare(p, p.DoorSquareX + 2, p.DoorSquareY));    // two out is silent
    }

    [Fact]
    public void DoorPosition_Sits_InTheDeepField_InsideTheFence()
    {
        foreach (string id in new[] { "miranda", "luna", "phobos", "europa", "titan", "expedition-site-tunnel" })
        {
            SecretLab.Placement p = SecretLab.For(id, Env, forcePresent: true);
            Assert.True(p.DoorY < Env.LandingBandY, $"{id}: door must be a committed walk below the landing band");
            Assert.True(p.DoorY > Env.BottomY, $"{id}: door inside the deep field");
            Assert.True(p.DoorX > Env.LeftX && p.DoorX < Env.RightX, $"{id}: door inside the fence");
        }
    }

    // ── The forced lab region: enclosed, clamped, distinct contents, walkable ──

    [Fact]
    public void Build_IsDeterministic_PerBodyAndDoor()
    {
        SecretLab.Placement p = SecretLab.For("phobos", Env, forcePresent: true);
        SecretLab.Region a = SecretLab.Build("phobos", Env, p.DoorX, p.DoorY);
        SecretLab.Region b = SecretLab.Build("phobos", Env, p.DoorX, p.DoorY);
        Assert.Equal(a.Walls, b.Walls);
        Assert.Equal(a.Consoles, b.Consoles);
        Assert.Equal(a.Scheme, b.Scheme);
    }

    [Fact]
    public void Region_Bounds_StayInsideTheFieldsSafeSpan()
    {
        foreach (string id in new[] { "miranda", "luna", "phobos", "europa", "titan" })
        {
            SecretLab.Placement p = SecretLab.For(id, Env, forcePresent: true);
            SecretLab.Region r = SecretLab.Build(id, Env, p.DoorX, p.DoorY);
            Assert.True(r.MinX >= Env.LeftX + SurfaceLayout.EdgeMargin - 0.01, $"{id}: left edge lane kept open");
            Assert.True(r.MaxX <= Env.RightX - SurfaceLayout.EdgeMargin + 0.01, $"{id}: right edge lane kept open");
            Assert.True(r.MinY >= Env.BottomY, $"{id}: within the deep rim");
            Assert.True(r.MaxY <= Env.LandingBandY, $"{id}: below the landing band");
        }
    }

    [Fact]
    public void Region_Has_TheDistinctLabContents()
    {
        SecretLab.Region r = SecretLab.Build("phobos", Env, SecretLab.For("phobos", Env, true).DoorX,
            SecretLab.For("phobos", Env, true).DoorY);

        Assert.Equal("VANTAR'S SECRET LAB", r.Scheme);
        Assert.Equal(SecretLab.DiscoveryCacheCredits, r.DiscoveryBonus);
        Assert.Contains(r.Consoles, c => c.Kind == SecretLab.LabConsoleKind.DiscoveryCache);
        Assert.Contains(r.Consoles, c => c.Kind == SecretLab.LabConsoleKind.BrainJar);
        Assert.Contains(r.Consoles, c => c.Kind == SecretLab.LabConsoleKind.DormantSynth);

        // Exactly one CORE log, and it points at the deepest Vantar fragment (the reveal text).
        var coreLogs = r.Consoles.Where(c => c.IsCoreLog).ToList();
        Assert.Single(coreLogs);
        Assert.Equal(SecretLab.LabConsoleKind.LoreLog, coreLogs[0].Kind);
        Assert.Equal(VantarLore.CoreIndex, coreLogs[0].LoreIndex);
    }

    [Fact]
    public void Region_IsEnclosed_AndLeaves_TheConsoleLane_Walkable()
    {
        // Every console must stand clear of every wall by more than the avatar's radius — the door→console
        // lane is genuinely walkable (the captain can reach the cache and the core log, never wall-trapped).
        foreach (string id in new[] { "miranda", "luna", "phobos", "europa", "titan", "expedition-site-wreck" })
        {
            SecretLab.Placement p = SecretLab.For(id, Env, forcePresent: true);
            SecretLab.Region r = SecretLab.Build(id, Env, p.DoorX, p.DoorY);
            foreach (SecretLab.LabConsole con in r.Consoles)
            {
                foreach (SurfaceLayout.Wall w in r.Walls)
                {
                    double d = SurfaceCollision.DistanceToSegment(con.X, con.Y, w.X1, w.Y1, w.X2, w.Y2);
                    Assert.True(d > AvatarRadius + 0.3,
                        $"{id}: console {con.Id} is crowded by a wall (clearance {d:F2})");
                }
            }
        }
    }

    // ── The diced reveal (house law: the die is shown) ──

    [Fact]
    public void RollReveal_IsDeterministic_PerSeed()
    {
        ulong seed = DiceRule.Seed("secretlab:reveal:phobos", 123);
        Assert.Equal(SecretLab.RollReveal(seed), SecretLab.RollReveal(seed));
    }

    [Fact]
    public void RollReveal_BothOutcomes_AreReachable_AndInRange()
    {
        int salvage = 0, cost = 0;
        for (int i = 0; i < 500; i++)
        {
            SecretLab.RevealRoll roll = SecretLab.RollReveal(DiceRule.Seed("reveal", i));
            Assert.InRange(roll.Face, 1, 20);
            if (roll.Outcome == SecretLab.RevealOutcome.SalvageTech)
            {
                salvage++;
                Assert.True(roll.Face >= SecretLab.SalvageMinRoll);
                Assert.InRange(roll.PayCredits, SecretLab.SalvagePayMin, SecretLab.SalvagePayMax);
                Assert.Equal(0, roll.PackSize);
                Assert.Equal(SecretLab.RevealShock, roll.NerveHit);
            }
            else
            {
                cost++;
                Assert.True(roll.Face < SecretLab.SalvageMinRoll);
                Assert.Equal(0, roll.PayCredits);
                Assert.InRange(roll.PackSize, SecretLab.WakePackMin, SecretLab.WakePackMax);
                Assert.True(roll.NerveHit > SecretLab.RevealShock); // it costs you more
            }
        }
        Assert.True(salvage > 0 && cost > 0, "both reveal outcomes must be reachable across seeds");
    }

    // ── Vantar's lore pool ──

    [Fact]
    public void VantarLore_Pool_IsPopulated_AndCoreLogIsTheDeepest()
    {
        Assert.True(VantarLore.Fragments.Length >= 4);
        Assert.All(VantarLore.Fragments, f => Assert.False(string.IsNullOrWhiteSpace(f)));
        Assert.Equal(VantarLore.Fragments.Length - 1, VantarLore.CoreIndex);
        Assert.Equal(VantarLore.Fragments[VantarLore.CoreIndex], VantarLore.CoreLog);
        Assert.Contains("DO NOT REVIVE", VantarLore.Fragments[2]);          // the backup-rig log's label
        Assert.Contains("Vantar", VantarLore.Fragments[0]);                 // the man himself
    }

    [Fact]
    public void VantarLore_Fragment_ClampsOutOfRangeIndex_NeverThrows()
    {
        Assert.Equal(VantarLore.Fragments[0], VantarLore.Fragment(-5));
        Assert.Equal(VantarLore.CoreLog, VantarLore.Fragment(999));
    }
}
