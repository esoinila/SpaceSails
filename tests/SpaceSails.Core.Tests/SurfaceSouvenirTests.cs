namespace SpaceSails.Core.Tests;

/// <summary>
/// The surface souvenir kiosk's tee, keyed to the moon underfoot (#379). The owner walked up to the
/// Ganymede kiosk and it "sold me a Miranda souvenir T :-D". These pin the fix: the item and gag name
/// the walked body, the gag is generated (so any future landable moon works with no table), the pick is
/// deterministic per body, Miranda keeps its canon line, and nothing ever leaks a stray brace.
/// </summary>
public class SurfaceSouvenirTests
{
    [Fact]
    public void TeeItem_NamesTheWalkedBody_Uppercase()
    {
        Assert.Equal("a GANYMEDE souvenir tee", SurfaceSouvenir.TeeItem("Ganymede"));
        Assert.Equal("a LUNA souvenir tee", SurfaceSouvenir.TeeItem("Luna"));
        Assert.Equal("a MIRANDA souvenir tee", SurfaceSouvenir.TeeItem("Miranda")); // canon shape preserved
    }

    [Fact]
    public void TeeGag_DropsInTheBodyName()
    {
        string gag = SurfaceSouvenir.TeeGag("ganymede", "Ganymede");
        Assert.Contains("Ganymede", gag);
        Assert.DoesNotContain("Miranda", gag); // the bug: Ganymede must not sell Miranda's shirt
    }

    [Fact]
    public void Miranda_KeepsItsCanonGag()
    {
        Assert.Equal(SurfaceSouvenir.MirandaGag, SurfaceSouvenir.TeeGag("miranda", "Miranda"));
        Assert.Equal(SurfaceSouvenir.MirandaGag, SurfaceSouvenir.TeeGag("MIRANDA", "Miranda")); // id case-insensitive
    }

    [Fact]
    public void TeeGag_IsDeterministic_PerBody()
    {
        Assert.Equal(SurfaceSouvenir.TeeGag("titan", "Titan"), SurfaceSouvenir.TeeGag("titan", "Titan"));
        Assert.Equal(SurfaceSouvenir.TeeGag("phobos", "Phobos"), SurfaceSouvenir.TeeGag("phobos", "Phobos"));
    }

    [Fact]
    public void EveryVariant_SubstitutesCleanly_NoLeakedBraces()
    {
        // Any future landable moon works by construction: every seeded variant drops the name in and
        // leaves no stray brace behind, whatever the index (it wraps).
        for (int v = -3; v < SurfaceSouvenir.VariantCount + 3; v++)
        {
            string gag = SurfaceSouvenir.GagVariant(v, "Ganymede");
            Assert.Contains("Ganymede", gag);
            Assert.DoesNotContain("{", gag);
            Assert.DoesNotContain("}", gag);
        }
    }

    [Fact]
    public void GagPool_HasSeveralVariants()
    {
        // "a couple of seeded variants" — more than one, so bodies don't all read identically.
        Assert.True(SurfaceSouvenir.VariantCount >= 2);
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int v = 0; v < SurfaceSouvenir.VariantCount; v++)
        {
            seen.Add(SurfaceSouvenir.GagVariant(v, "Io"));
        }
        Assert.Equal(SurfaceSouvenir.VariantCount, seen.Count); // each variant is distinct copy
    }

    [Fact]
    public void DifferentBodies_CanDrawDifferentVariants()
    {
        // The per-body hash spreads across the pool — not every pair differs, but the stream isn't stuck
        // on one line for every moon.
        var lines = new System.Collections.Generic.HashSet<string>();
        foreach (string id in new[] { "ganymede", "titan", "phobos", "luna", "io", "europa", "callisto", "rhea" })
        {
            lines.Add(SurfaceSouvenir.TeeGag(id, id));
        }
        Assert.True(lines.Count >= 2);
    }
}
