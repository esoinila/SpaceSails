using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// EVERY SCENE THE GAME CAN PUT THE CAPTAIN IN, built the way the game builds it — one table, so two audits can
/// never disagree about what "every scene" means.
///
/// <para>Extracted when the second audit arrived. Owner, 2026-07-30: <i>"Starting every scene and seeing if it has
/// all the parts in right place is the best way to find bugs 😎👍"</i> — which is true in this repo to a degree that
/// is almost embarrassing, so it is worth having CI do it rather than remembering to. Two audits read this list now
/// (crowding and inventory) and a third will read it later; a copy-pasted scene table would have gone stale the
/// first time a hull was added.</para>
/// </summary>
public static class Scenes
{
    /// <summary>A sample of walked ground. The surface layout is procedural per body, so a handful of real ones is
    /// the honest cover — any of them putting a dig site on a kiosk is the same bug.</summary>
    private static readonly string[] SurfaceBodies = ["luna", "phobos", "titan", "enceladus"];

    public static IEnumerable<string> Names()
    {
        yield return "ship";
        yield return "ship-all-hatches-dogged";

        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            yield return $"wreck:{cause}";
        }

        foreach (string id in HavenInterior.InteriorBodyIds)
        {
            yield return $"haven:{id}";

            // #1253 · …AND EVERY FLOOR OF IT. One station has a level under its concourse, and a floor
            // nobody audits is a floor that ships with its parts in the wrong places — which is the owner's
            // whole method. The levels are asked of the room rather than listed, so the day a second station
            // grows a basement it is swept without anybody being told.
            foreach (int level in HavenInterior.LevelsOf(id))
            {
                if (level != HavenLevels.Concourse)
                {
                    yield return $"haven:{id}:{level}";
                }
            }
        }

        // #320 · EVERY SITE, not just site 0. Owner: "We should test those sites with direct opens to them via
        // URL parameters to find out the usual issues." He is right that the sites were the least-booted scenes
        // in the game — and the audit had the same blind spot as the playtesting did, walking only the canon
        // ground of each body. A seeded site is a DIFFERENT deck plan on the same body, so a kiosk on a dig
        // site or an unreachable tube can hide on site 2 of a moon whose site 0 is spotless.
        foreach (string id in SurfaceBodies)
        {
            for (int site = 0; site < LandingSites.Count(id); site++)
            {
                yield return $"surface:{id}:{site}";
            }
        }
    }

    public static TheoryData<string> Every()
    {
        var data = new TheoryData<string>();
        foreach (string name in Names())
        {
            data.Add(name);
        }
        return data;
    }

    /// <summary>Built by name so xUnit prints WHICH scene failed instead of an object hash.</summary>
    public static DeckPlan Build(string name)
    {
        if (name == "ship")
        {
            return DeckPlan.Ship;
        }

        if (name == "ship-all-hatches-dogged")
        {
            var shut = new HashSet<string>(StringComparer.Ordinal);
            foreach (ShipLayout.Room room in ShipLayout.Rooms)
            {
                shut.Add(room.Name);
            }
            return DeckPlan.ShipWith(shut);
        }

        if (name.StartsWith("wreck:", StringComparison.Ordinal))
        {
            var cause = Enum.Parse<Derelict.WreckCause>(name["wreck:".Length..]);
            var wreck = new Derelict.Wreck("audit-hull", "Audited Hull", cause, 250_000, 40.0);
            return WreckInterior.WreckDeck(
                wreck, new HashSet<string>(StringComparer.Ordinal), salvaged: false,
                droidCount: 0, fillDroids: static (_, _) => { });
        }

        if (name.StartsWith("haven:", StringComparison.Ordinal))
        {
            // #1253 · "haven:<id>" is the concourse, as it always was; "haven:<id>:<level>" is a floor of it.
            string[] berth = name["haven:".Length..].Split(':');
            DeckPlan? deck = HavenInterior.DockedDeck(
                berth[0],
                level: berth.Length > 1
                    ? int.Parse(berth[1], System.Globalization.CultureInfo.InvariantCulture)
                    : HavenLevels.Concourse);
            Assert.NotNull(deck);
            return deck;
        }

        string[] parts = name["surface:".Length..].Split(':');
        string body = parts[0];
        LandingSite site = LandingSites.At(body, parts.Length > 1 ? int.Parse(parts[1]) : 0);

        return MoonSurface.SurfaceDeck(
            body, body, [], droidCount: 0, fillDroids: static (_, _) => { },
            siteSalt: site.LayoutSalt, siteName: site.Name);
    }

    /// <summary>Which family a scene belongs to — the inventory audit asks a different question of a haven than
    /// of a derelict.</summary>
    public static string FamilyOf(string name) =>
        name.StartsWith("wreck:", StringComparison.Ordinal) ? "wreck"
        // #1253 · A floor UNDER a concourse is its own family. It is a haven and it is not a bar: no
        // counter, no regulars, no tube and no gangway — so the parts its family needs are its own, and
        // asking it for a barkeep would be asking a service corridor to be a room it is not.
        : name.StartsWith("haven:", StringComparison.Ordinal)
            ? name.Count(c => c == ':') > 1 ? "haven-lower" : "haven"
        : name.StartsWith("surface:", StringComparison.Ordinal) ? "surface"
        : "ship";
}
