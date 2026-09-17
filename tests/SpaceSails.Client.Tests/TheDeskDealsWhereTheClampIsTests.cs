using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1217 · <b>THE DESK DEALS WHERE THE CLAMP IS.</b>
///
/// <para><b>What was found by playing.</b> Boot <c>/map?dock=the-tilt</c>. The HUD banner reads
/// <i>"🧭 YOU HAVE THE SHIP — NOW: docked at The Tilt"</i>, the Captain card reads <i>Docked at The Tilt</i>,
/// the Nav card reads <i>Docked at The Tilt</i> — and the dark-web node is badged <c>offline</c> over the
/// sentence <i>"Not orbiting or docked anywhere — get to a haven or a far trading post first."</i> The sim
/// doing one thing while a sentence reports another, which is this repository's own named bug class, in the
/// one place the fiction ever sends a captain to make a quiet deal.</para>
///
/// <para><b>Why every berth, and why since the beginning.</b> <c>DarkWebCurrentBody</c> asked two questions
/// and neither of them was the clamp: the port-zone flags (<c>_docked</c>/<c>_dockBodyId</c>), which
/// <c>UpdateDockStatus</c> only ever fills with <c>earth</c>/<c>mars</c>/<c>venus</c> — and
/// <c>IntelMarket.CanTradeIntelAt</c> says a planet never deals — and a Hill-radius bind, which needs μ&gt;0
/// and every station haven in <c>sol.json</c> is mass-less by construction. So the desk has been unreachable
/// at a berth since it was written (#30, 2026-07-04): the only body in the shipping sky it ever opened at is
/// Enceladus, the one haven with mass.</para>
///
/// <para><b>Why the guard that covered it was green.</b> Two benches
/// (<c>TheCarWithPhotographsInItTests.DockTheShipWhereTheDeskWorks</c>,
/// <c>TheKeyHasOtherSourcesTests.ADeskThatWillDeal</c>) reflection-set <c>_dockBodyId</c> to a HAVEN id — a
/// field no shipping path ever puts a haven id in. The assertions were right; the world they were handed
/// could not tell pass from fail. <see cref="NoGuardHandsThePageAWorldItsOwnCodeCannotBuild"/> and
/// <see cref="TheOnlyPortZoneTheGameEverNamesIsAPlanet"/> are the two halves of making that impossible to
/// write again.</para>
///
/// <para><b>Every berth here is clamped through the shipping clamp</b> — <c>ClampOntoHaven</c>, the one door
/// the ⚓ press, the honest auto-dock and the <c>?dock=</c> boot cheat all go through — and then a real frame
/// is run so the port-zone flags are the WORLD's answer and not a test's. Nothing below writes a dock field.
/// </para>
/// </summary>
public sealed class TheDeskDealsWhereTheClampIsTests
{
    /// <summary>Every berth the game can clamp onto in the shipping sky, off the registry <c>?dock=</c> and
    /// the CI smoke sweep read — so a haven added to a scenario is swept here for free and no berth id is
    /// ever typed into this file.</summary>
    private static IReadOnlyList<CelestialBody> TheBerths
    {
        get
        {
            ICelestialEphemeris sky = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
            IReadOnlyList<CelestialBody> berths = DockableHavens.All(sky);
            Assert.True(berths.Count > 0, "sol.json has no dockable haven, so this whole file sweeps nothing.");
            return berths;
        }
    }

    // ── (a) THE DESK ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1217 · <b>CLAMPED ON, THE DESK DEALS — AT EVERY BERTH, NOT ONE.</b>
    ///
    /// <para>The bug was never about The Tilt; it was about the question the desk asked. So the sweep is the
    /// whole roster, and it names every berth it could not open at rather than stopping at the first, because
    /// "one of seven" and "seven of seven" are different diagnoses.</para>
    ///
    /// <para><b>Proven RED</b> with the fix reverted (<c>DarkWebCurrentBody</c> back to its own two
    /// questions): all seven berths in <c>sol.json</c> fail.</para>
    /// </summary>
    [Fact]
    public void TheDarkWebDealsAtEveryBerthTheGameCanClampOnto()
    {
        var shut = new List<string>();
        foreach (CelestialBody berth in TheBerths)
        {
            Pages.Map map = AShipTiedUpAt(berth.Id);
            if (!(bool)Invoke(map, "DarkWebCanTrade")!)
            {
                shut.Add($"{berth.Id} — \"{Invoke(map, "DarkWebDisabledReason")}\"");
            }
        }

        Assert.True(shut.Count == 0,
            "clamped on at a berth, the dark-web desk refused to deal. A berth is the ONLY place the fiction "
            + "sends a captain for an off-the-books handover, so a desk that is offline there is a desk that "
            + "is offline:\n  " + string.Join("\n  ", shut));
    }

    /// <summary>
    /// #1217 · <b>THE OFFLINE SENTENCE IS NEVER SAID TO A SHIP THAT IS TIED UP.</b>
    ///
    /// <para>Separate from the one above, and deliberately so: <c>CanTrade</c> is the badge and the rows,
    /// while <c>DisabledReason</c> is the SENTENCE — and the sentence is what was caught telling a clamped
    /// captain he was "not orbiting or docked anywhere" under a banner that said otherwise. A future refusal
    /// that is honest about the place (a berth that stops dealing for some reason of its own) would still
    /// pass the first law and fail this one, which is the point.</para>
    /// </summary>
    [Fact]
    public void TheShipIsNowhereSentenceIsNeverShownToAClampedShip()
    {
        var lying = new List<string>();
        foreach (CelestialBody berth in TheBerths)
        {
            Pages.Map map = AShipTiedUpAt(berth.Id);
            var said = (string)Invoke(map, "DarkWebDisabledReason")!;
            if (said.Contains("Not orbiting or docked anywhere", StringComparison.Ordinal))
            {
                lying.Add($"{berth.Id}: \"{said}\"");
            }
        }

        Assert.True(lying.Count == 0,
            "the desk told a ship with an arm on it that it is nowhere, while the HUD banner, the Captain "
            + "card and the Nav card all name the berth. Sentence-vs-sim:\n  " + string.Join("\n  ", lying));
    }

    /// <summary>
    /// #1217 · <b>ALL FOUR ROWS, AT EVERY BERTH.</b> The chip's buyer (#233), the inspector's card (#1149),
    /// the fence's key (#535 slice 2) and the unlisted parcel (#711 slice 2) are four separate gates that
    /// happen to share one place-question, and a fix that opened the desk but left a row behind would be a
    /// fix nobody could play.
    ///
    /// <para>Each row is asked for the thing the DESK decides — a price, a port, a parcel — not for the
    /// markup, so a row drawn and then denied still fails here.</para>
    /// </summary>
    [Fact]
    public void AllFourRowsOfTheDeskAreReachableFromEveryBerth()
    {
        var missing = new List<string>();
        foreach (CelestialBody berth in TheBerths)
        {
            Pages.Map map = AShipTiedUpAt(berth.Id);
            PutAnOpenChipJobInThePocket(map);

            if (Invoke(map, "ChipFencePrice") is null)
            {
                missing.Add($"{berth.Id}: the chip has no buyer.");
            }

            if (Invoke(map, "InspectorCardPrice") is null)
            {
                missing.Add($"{berth.Id}: the inspector's card is not for sale.");
            }

            if ((string?)Invoke(map, "TheFencesPort") != berth.Id)
            {
                missing.Add($"{berth.Id}: the fence is working from "
                    + $"{Invoke(map, "TheFencesPort") ?? "nowhere"}, not this berth.");
            }

            if (!(bool)Invoke(map, "ParcelOnOffer")!)
            {
                missing.Add($"{berth.Id}: there is no parcel on the desk.");
            }
        }

        Assert.True(missing.Count == 0,
            "a dark-web row the fiction and the link sheet both send a captain to is unreachable from a "
            + "berth:\n  " + string.Join("\n  ", missing));
    }

    // ── (b) THE LAW: WHICH WORLDS THE GAME CAN ACTUALLY BUILD ─────────────────────────────────────────

    /// <summary>
    /// #1217 · <b>THE ONLY THING <c>_dockBodyId</c> EVER HOLDS IS A PLANET WITH A MARKET ON IT.</b>
    ///
    /// <para>Three fields, three questions, and the whole bug was a reader that could not tell them apart:
    /// <c>_dockedHavenId</c> is <b>at a berth</b> (the arm is out), <c>_docked</c>/<c>_dockBodyId</c> is
    /// <b>inside a planetary market's catchment</b> (0.067 AU of earth/mars/venus — the ship still flies
    /// freely there), and the Hill bind is <b>in orbit</b>. This pins what the real paths can produce, so a
    /// bench that hands the page a haven id in the market slot is a bench asking about a game nobody
    /// ships.</para>
    ///
    /// <para><b>Four triples exist and this walks every one of them:</b> adrift (nothing set), inside a
    /// planet's zone but flying, clamped at an OUTER berth (too far from any market planet), and clamped at
    /// an INNER berth — where both are true at once, because Selene Gate, Cinder Roost and The Space Bar all
    /// sit deep inside their planet's catchment. That fourth one is why the two flags cannot simply be
    /// merged.</para>
    /// </summary>
    [Fact]
    public void TheOnlyPortZoneTheGameEverNamesIsAPlanet()
    {
        ICelestialEphemeris sky = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        var marketBodies = new HashSet<string>(
            (string[])typeof(Pages.Map)
                .GetField("MarketBodies", TestTree.AnythingAtAll)!
                .GetValue(null)!,
            StringComparer.Ordinal);

        var seen = new SortedSet<string>(StringComparer.Ordinal);
        var wrong = new List<string>();

        void Record(string how, Pages.Map map)
        {
            bool docked = (bool)Read(map, "_docked")!;
            var body = (string?)Read(map, "_dockBodyId");
            var berth = (string?)Read(map, "_dockedHavenId");

            if (docked != (body is not null))
            {
                wrong.Add($"{how}: _docked={docked} but _dockBodyId={body ?? "null"} — "
                    + "UpdateDockStatus writes the pair together, so they cannot disagree.");
            }

            if (body is not null && !marketBodies.Contains(body))
            {
                wrong.Add($"{how}: _dockBodyId=\"{body}\", which is not one of the market bodies "
                    + $"({string.Join("/", marketBodies)}). That slot is a PLANETARY MARKET, never a berth.");
            }

            if (berth is not null)
            {
                CelestialBody? tiedTo = sky.Bodies.FirstOrDefault(b => b.Id == berth);
                if (tiedTo is null)
                {
                    wrong.Add($"{how}: _dockedHavenId=\"{berth}\" names no body in the sky.");
                }
                else if (!DockableHavens.IsDockable(tiedTo))
                {
                    wrong.Add($"{how}: _dockedHavenId=\"{berth}\" is not a berth a clamp can be thrown onto.");
                }
            }

            seen.Add((berth is not null, docked) switch
            {
                (false, false) => "adrift: nothing set",
                (false, true) => "in a planet's port zone, flying",
                (true, false) => "clamped at an outer berth",
                (true, true) => "clamped at an inner berth, inside its planet's port zone",
            });
        }

        Pages.Map far = Boot("dock-law");
        Set(far, "_ship", new ShipState(new Vector2d(9e12, 9e12), Vector2d.Zero, 0.0));
        Frame(far);
        Record("out past everything", far);

        Pages.Map spawn = Boot("dock-law");
        Frame(spawn);
        Record("the Earth spawn", spawn);

        foreach (CelestialBody berth in TheBerths)
        {
            Record($"clamped at {berth.Id}", AShipTiedUpAt(berth.Id));
        }

        Assert.True(wrong.Count == 0, "a shipping path built a dock state the law says it cannot:\n  "
            + string.Join("\n  ", wrong));

        Assert.Equal(
            new[]
            {
                "adrift: nothing set",
                "clamped at an inner berth, inside its planet's port zone",
                "clamped at an outer berth",
                "in a planet's port zone, flying",
            },
            seen.ToArray());
    }

    /// <summary>
    /// #1217 · <b>NO GUARD IN THIS SUITE MAY HAND THE PAGE A PORT ZONE BY HAND.</b>
    ///
    /// <para><c>_docked</c> and <c>_dockBodyId</c> have exactly one writer in the whole client —
    /// <c>UpdateDockStatus</c>, which runs every tick and only ever names a market planet. A test that writes
    /// them by reflection is a test that has invented a world, and the two that did invented the SAME
    /// impossible one (a haven id in the market slot) and between them kept #1217 green for ten weeks.</para>
    ///
    /// <para>The honest way to be in a port zone is to put the ship there and let the tick answer — which is
    /// what <see cref="TheOnlyPortZoneTheGameEverNamesIsAPlanet"/> does, in four lines. <c>_dockedHavenId</c>
    /// is deliberately NOT swept: it is a berth the clamp sets and a great many scene guards legitimately
    /// stand a ship at one, and every triple that produces is a triple the game builds.</para>
    /// </summary>
    [Fact]
    public void NoGuardHandsThePageAWorldItsOwnCodeCannotBuild()
    {
        string here = Path.Combine(TestTree.RepoRoot(), "tests", "SpaceSails.Client.Tests");
        var byHand = new Regex("\"_docked\"|\"_dockBodyId\"", RegexOptions.CultureInvariant);
        var offenders = new List<string>();

        foreach (string file in Directory.EnumerateFiles(here, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || string.Equals(Path.GetFileName(file), nameof(TheDeskDealsWhereTheClampIsTests) + ".cs", StringComparison.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (byHand.IsMatch(lines[i]) && lines[i].Contains("Set(", StringComparison.Ordinal))
                {
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}  {lines[i].Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "a guard wrote the port-zone flags by hand. They have ONE writer in the client "
            + "(Map.Docking.Run.UpdateDockStatus) and it only ever names earth/mars/venus, so a hand-set "
            + "value is a world the game cannot build — which is exactly how #1217 shipped green. Put the "
            + "ship where you mean and run a frame instead:\n  " + string.Join("\n  ", offenders));
    }

    // ── THE WORLD ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A ship tied up at a berth, the way the game ties one up: <c>ClampOntoHaven</c> — the one door the ⚓
    /// press, the honest auto-dock and the <c>?dock=</c> boot start all go through — and then a real frame,
    /// so the port-zone flags are whatever the WORLD says they are at that berth and not something this file
    /// decided. Nothing here writes a dock field.
    /// </summary>
    private static Pages.Map AShipTiedUpAt(string berthId)
    {
        Pages.Map map = Boot("darkweb-desk");
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody dock = sky.Bodies.First(b => b.Id == berthId);

        Invoke(map, "ClampOntoHaven", dock, sky.Position(berthId, (double)Read(map, "SimTime")!), null);
        Frame(map);

        Assert.Equal(berthId, (string?)Read(map, "_dockedHavenId"));
        return map;
    }

    /// <summary>The compromising chip in the pocket with its fetch job still open — the state the chip's row
    /// is drawn for. Built out of the shipping objects (<c>CompromisingChip.Found</c>, the page's own
    /// <c>Quest</c>), so the row is asked about the chip the game makes.</summary>
    private static void PutAnOpenChipJobInThePocket(Pages.Map map)
    {
        var satchel = (List<Satchel.Item>)Read(map, "_satchel")!;
        Set(map, "_satchel", Satchel.Add(satchel, CompromisingChip.Found()).ToList());

        var quests = (List<Pages.Map.Quest>)Read(map, "_quests")!;
        quests.Add(new Pages.Map.Quest(
            "chip-bench", Pages.Map.QuestKind.Fetch, "THE FIXER", "", "The Fixer",
            "Fetch the roadster's lost wallet", "[bench]", 900,
            DestBodyId: "the-space-bar", SourceBodyId: Derelict.RoadsterBodyId,
            Pin: CompromisingChip.FindId)
        {
            State = Pages.Map.QuestState.PickedUp,
        });

        Assert.NotNull(Read(map, "TheChipInThePocket"));
    }
}
