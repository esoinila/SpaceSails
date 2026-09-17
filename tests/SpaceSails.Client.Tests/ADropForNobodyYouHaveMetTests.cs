using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #711 slice 2 / #319 / #794 · <b>THE CLIENT HALF: THE SKY IS REAL, THE SHOVEL IS THE DELIVERY, AND THE
/// MONEY IS AT ONE DOOR.</b>
///
/// <para><b>What this file can and cannot prove.</b> The same split slice 1's own client bench keeps: the
/// page is a partial class on a razor component no test can instantiate, so the JUDGEMENT is driven end to
/// end in the Core suite and what is pinned here is the wiring the page owns and Core cannot see — plus the
/// one thing only this project can ask, because only this project loads the sky: that every ground a job
/// can name is really in <c>scenarios/sol.json</c> and really landable there.</para>
///
/// <para>Every guard names the revert that was made in the source and watched go red.</para>
/// </summary>
public sealed class ADropForNobodyYouHaveMetTests
{
    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestTree.RepoRoot(), .. parts]));

    /// <summary>The CODE, with the design record taken out of it — these files are half comment by weight
    /// and every name a guard counts is discussed in prose beside the line that uses it.</summary>
    private static string Code(string source)
    {
        string noBlock = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(noBlock, "//[^\n]*", " ");
    }

    private static int Count(string haystack, string needle)
    {
        int n = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal);
             i >= 0;
             i = haystack.IndexOf(needle, i + 1, StringComparison.Ordinal))
        {
            n++;
        }
        return n;
    }

    // ── (1) EVERY GROUND A JOB CAN NAME IS IN THE SKY THE GAME SHIPS ────────────────────────────────────

    /// <summary>
    /// <b>THE DESTINATION EXISTS IN <c>sol.json</c> AND IS LANDABLE THERE.</b> The whole point of handing
    /// the pool in rather than letting Core invent one, asked of the file the game actually ships: every
    /// parcel the desk can mint is driven against the scenario's own landable bodies, and the ground it
    /// names is looked up BY ID in the parsed file and checked to be a moon.
    ///
    /// <para>THE WORLD CAN TELL PASS FROM FAIL: <c>sol.json</c> really does carry bodies that are NOT
    /// landable — planets, stations, the sun — so an assertion that the named id is a moon is an assertion
    /// something in this file could fail.</para>
    ///
    /// <para><b>RED</b> by having the generator compose an id of its own
    /// (<c>new Destination(bodyId + "-drop", …)</c>): <i>a job named a body the shipped sky does not
    /// have</i>. That the CLIENT hands in the landable ones is the next guard's business — this one is the
    /// promise kept against the real file: whatever pool of the sky's own moons goes in, what comes out is
    /// one of them, on a ground that body really offers.</para>
    /// </summary>
    [Fact]
    public void EveryGroundAJobCanNameIsInTheShippedSkyAndIsLandable()
    {
        ScenarioDefinition sky = TestTree.Sol;

        var landable = new List<string>();
        var kindById = new Dictionary<string, BodyKind>(StringComparer.Ordinal);
        foreach (BodyDefinition body in sky.Bodies)
        {
            BodyKind kind = CircularOrbitEphemeris.KindOf(body.Kind);
            kindById[body.Id] = kind;
            if (ShuttleExcursion.IsLandableSurface(kind))
            {
                landable.Add(body.Id);
            }
        }

        Assert.True(landable.Count >= 8, $"the shipped sky has only {landable.Count} landable bodies.");
        Assert.True(kindById.Count > landable.Count,
            "every body in the shipped sky is landable — this guard could not tell pass from fail.");

        int seen = 0;
        var named = new HashSet<string>(StringComparer.Ordinal);
        foreach (string haven in DockableHavens.AllIds(CircularOrbitEphemeris.FromScenario(sky)))
        {
            for (long watch = 0; watch < 8; watch++)
            {
                Satchel.Item parcel = UnlistedParcel.FromTheDesk(haven, watch);
                ParcelDrop.Destination where = ParcelDrop.For(parcel, landable)
                    ?? throw new Xunit.Sdk.XunitException("a real parcel against the shipped sky named nowhere.");

                Assert.True(kindById.ContainsKey(where.BodyId),
                    $"a job named a body the shipped sky does not have: {where.BodyId}");
                Assert.True(ShuttleExcursion.IsLandableSurface(kindById[where.BodyId]),
                    $"a job named ground no shuttle can be set down on: {where.BodyId}");
                Assert.InRange(where.SiteIndex, 0, LandingSites.Count(where.BodyId) - 1);
                Assert.Equal(LandingSites.At(where.BodyId, where.SiteIndex).Name, where.SiteName);

                named.Add($"{where.BodyId}#{where.SiteIndex}");
                seen++;
            }
        }

        Assert.True(seen > 50, $"this sweep is {seen} parcels wide — too small to mean anything.");
        Assert.True(named.Count > 10,
            $"the shipped sky only ever produced {named.Count} distinct grounds — this bench could not see "
            + "a constant answer.");
    }

    /// <summary>
    /// <b>THE POOL IS THE SCENARIO'S, TAKEN BEFORE THE CHEATS HANG THEIR ROCKS ON IT.</b> Four cheats append
    /// a body to the scenario before the ephemeris is built and every one of them is a <c>moon</c>; a pool
    /// read off the built sky would answer a different destination for a boot that used one, and the box in
    /// the captain's pocket does not know which URL opened the tab.
    ///
    /// <para><b>RED</b> by moving <c>RememberTheGroundADropMayName(scenario)</c> below
    /// <c>AppendTheBodiesTheCheatsAskFor</c>: <i>the drop's pool is taken after the cheats have appended
    /// their rocks</i>. <b>RED</b> by reading <c>_ephemeris.Bodies</c> in <c>TheLandableGround</c>: <i>the
    /// drop's pool is read off the built sky</i>.</para>
    /// </summary>
    [Fact]
    public void ThePoolIsTheScenariosAndIsTakenBeforeTheCheats()
    {
        string world = Code(Read("src", "SpaceSails.Client", "Pages", "Map.Sim.World.cs"));
        string drop = Code(Read("src", "SpaceSails.Client", "Pages", "Map.ParcelDrop.cs"));

        int taken = world.IndexOf("RememberTheGroundADropMayName(scenario)", StringComparison.Ordinal);
        int appended = world.IndexOf("AppendTheBodiesTheCheatsAskFor(scenario", StringComparison.Ordinal);
        Assert.True(taken > 0, "nothing takes the drop's pool at boot at all.");
        Assert.True(appended > 0, "the cheat append moved — this guard is watching a line that is gone.");
        Assert.True(taken < appended,
            "the drop's pool is taken after the cheats have appended their rocks.");

        // …and the pool itself is the SCENARIO's bodies, filtered by the shuttle board's own predicate.
        Assert.Contains("scenario?.Bodies", drop, StringComparison.Ordinal);
        Assert.Contains("ShuttleExcursion.IsLandableSurface", drop, StringComparison.Ordinal);
        Assert.DoesNotContain("_ephemeris.Bodies", drop, StringComparison.Ordinal);
        Assert.DoesNotContain("_ephemeris?.Bodies", drop, StringComparison.Ordinal);

        // …and it decides what a moon is with Core's one answer, never a string of its own.
        Assert.Contains("CircularOrbitEphemeris.KindOf", drop, StringComparison.Ordinal);
        Assert.DoesNotContain("\"moon\"", drop, StringComparison.Ordinal);
    }

    // ── (2) THE ROW IS THE JOB ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE JOB LIVES ON THE ROW THE BOX CAME ACROSS — NO PANEL, NO QUEST ENTRY, NO SECOND SCREEN.</b>
    /// The desk gains one block that draws only while a parcel is carried; it reads Core's instruction and
    /// Core's destination line, and composes neither; and the parameter chain is walked whole — Map.razor →
    /// FlowColumn → DeskPanels → DarkWeb — because a row wired at three of four levels is a row nobody ever
    /// sees, and the razor generator would not say so.
    ///
    /// <para><b>RED</b> by dropping the <c>TheParcelsDestinationRow</c> wiring from Map.razor: <i>the job
    /// row is wired at 3 of 4 levels</i>. <b>RED</b> by having the desk compose the ground itself: <i>the
    /// desk describes a place it does not own</i>.</para>
    /// </summary>
    [Fact]
    public void TheDeskRowIsWhereTheJobLives()
    {
        string desk = Read("src", "SpaceSails.Client", "Pages", "Stations", "DarkWeb.razor");
        string map = Read("src", "SpaceSails.Client", "Pages", "Map.ParcelDrop.cs");

        Assert.Contains("@if (!string.IsNullOrEmpty(ParcelDestinationRow))", desk, StringComparison.Ordinal);
        Assert.Contains("ParcelDrop.TheInstruction", desk, StringComparison.Ordinal);
        Assert.Contains("@ParcelDestinationRow", desk, StringComparison.Ordinal);

        // The desk never composes a place, a price or a promise of its own about the job.
        Assert.DoesNotContain("ParcelDrop.DestinationRow", desk, StringComparison.Ordinal);
        Assert.DoesNotContain("LandingSites", desk, StringComparison.Ordinal);

        // Still no price on the row, then or now: the only credit typography on this desk belongs to the
        // three rows that really do move coin.
        Assert.DoesNotContain("ParcelPrice", desk, StringComparison.Ordinal);

        // The line is composed ONCE, by Core, off the body's own display name and the site's own.
        Assert.Contains("ParcelDrop.DestinationRow(BodyName(where.BodyId), where.SiteName)", map,
            StringComparison.Ordinal);

        foreach ((string file, string needle) in new[]
        {
            (Path.Combine("Pages", "Map.razor"), "TheParcelsDestinationRow=\"@TheParcelsDestinationRow\""),
            (Path.Combine("Pages", "Map", "FlowColumn.razor"), "TheParcelsDestinationRow=\"@TheParcelsDestinationRow\""),
            (Path.Combine("Pages", "Map", "FlowColumn.razor.cs"), "Func<string> TheParcelsDestinationRow"),
            (Path.Combine("Pages", "Map", "DeskPanels.razor.cs"), "Func<string> TheParcelsDestinationRow"),
            (Path.Combine("Pages", "Map", "DeskPanels.razor"), "ParcelDestinationRow=\"@TheParcelsDestinationRow()\""),
            (Path.Combine("Pages", "Stations", "DarkWeb.razor.cs"), "string ParcelDestinationRow"),
        })
        {
            Assert.True(
                Read("src", "SpaceSails.Client", file).Contains(needle, StringComparison.Ordinal),
                $"the job row is wired at 3 of 4 levels — {file} has no {needle}.");
        }
    }

    // ── (3) THE SHOVEL IS THE DELIVERY, ON #319's OWN PATH ──────────────────────────────────────────────

    /// <summary>
    /// <b>ONE SHOVEL, ONE HOLE, ONE PULSE.</b> The delivery is asked of the chest <c>_caches.Bury</c> just
    /// minted — so it is #319's record and not a second one — and its line rides the sentence the captain is
    /// already reading rather than a second pulse that would overwrite it (#774's one-line law).
    ///
    /// <para>The file mints exactly ONE cache, which is the guard against the tempting second one: a
    /// "delivery cache" beside the buried chest would be a second ledger, a second vault section and a
    /// second rebirth for one hole.</para>
    ///
    /// <para><b>RED</b> by raising the delivery as its own <c>ShowPulseMessage</c>: <i>the delivery is told
    /// on a second pulse</i> — the dig's own sentence is overwritten and the ✗, the odds and the way home
    /// are never read. <b>RED</b> by minting a cache of its own in <c>TheDropIsMade</c>: <i>the drop mints a
    /// second chest</i>.</para>
    /// </summary>
    [Fact]
    public void TheDeliveryIsTheSameShovelAndTheSameHole()
    {
        string dig = Code(Read("src", "SpaceSails.Client", "Pages", "Map.Surface.Dig.cs"));
        string drop = Code(Read("src", "SpaceSails.Client", "Pages", "Map.ParcelDrop.cs"));

        // The dig calls it, once, ON the pulse it was always going to say.
        Assert.Equal(1, Count(dig, "TheDropIsMade(cache)"));
        Assert.Contains("the shuttle.{TheDropIsMade(cache)}", dig, StringComparison.Ordinal);

        // One hole: the drop mints NO chest of its own — it asks the one the shovel already made. A
        // "delivery cache" beside the buried chest would be a second ledger, a second vault section and a
        // second rebirth for one hole.
        Assert.DoesNotContain("_caches.Bury(", drop, StringComparison.Ordinal);
        Assert.DoesNotContain("new TreasureCache", drop, StringComparison.Ordinal);
        Assert.DoesNotContain("CacheLedger", drop, StringComparison.Ordinal);
        // …and the delivery is a TAIL that is RETURNED, never a pulse of its own — and it is said once in
        // the whole file, so it cannot be both.
        Assert.Contains("return \" \" + ParcelDrop.DeliveredLine;", drop, StringComparison.Ordinal);
        Assert.Equal(1, Count(drop, "ParcelDrop.DeliveredLine"));

        // The book's entry declares its subject (#741) rather than leaving a regex to find one — and the
        // subject is minted by the CORE author beside the sentence, never here. The client tree is swept for
        // a page that mints one by `TheThreadsPageIsInTheSatchelTests`, which caught this crew doing it.
        Assert.Contains("ParcelDrop.TheFieldBookSubjects(place)", drop, StringComparison.Ordinal);
        Assert.DoesNotContain("CaseSubjects.", drop, StringComparison.Ordinal);
        Assert.Contains("FieldNotes.PlaceLabel(BodyName(cache.BodyId), cache.SiteName)", drop,
            StringComparison.Ordinal);
    }

    // ── (4) THE MONEY IS AT ONE DOOR ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE PAYMENT ARRIVES AT THE DESK, AND THE DESK HAS EXACTLY ONE DOOR.</b> Three routes select the
    /// dark-web node (the comms tree, the flow column's copy of it, the ledger's <i>"→ dark web"</i> link)
    /// and all three write one member, so the read cannot be leaked past by a route that forgot to ask —
    /// the discipline <c>SwitchDesk</c> keeps for the desks themselves.
    ///
    /// <para>And the chest is LIFTED in the same breath the coin moves, which is the whole of "paid exactly
    /// once": no flag is written, so no flag can be forgotten.</para>
    ///
    /// <para><b>RED</b> by calling <c>ThePaymentIsThere</c> from the tree's own click instead of from the
    /// property: <i>there is more than one door onto the desk</i>. <b>RED</b> by crediting the purse without
    /// <c>_caches.Remove</c>: <i>the desk pays without lifting the chest</i>.</para>
    /// </summary>
    [Fact]
    public void ThePaymentArrivesAtTheOneDoorAndLiftsTheChest()
    {
        string alerts = Code(Read("src", "SpaceSails.Client", "Pages", "Map.Alerts.cs"));
        string drop = Code(Read("src", "SpaceSails.Client", "Pages", "Map.ParcelDrop.cs"));

        // ONE door: the selection is a property, and the payment is asked there and nowhere else.
        Assert.Contains("private string? _commsSelectedId", alerts, StringComparison.Ordinal);
        Assert.Contains("ThePaymentIsThere();", alerts, StringComparison.Ordinal);
        Assert.Equal(1, Count(alerts, "ThePaymentIsThere();"));

        // …and it only fires when the desk is being OPENED, never on a re-render of the node already on.
        Assert.Contains("value is \"darkweb\" && _commsSelectedIdValue is not \"darkweb\"", alerts,
            StringComparison.Ordinal);

        foreach (string file in new[]
        {
            Path.Combine("Pages", "Map.Trade.DarkWeb.cs"),
            Path.Combine("Pages", "Map", "DeskPanels.razor"),
            Path.Combine("Pages", "Map", "FlowColumn.razor.cs"),
            Path.Combine("Pages", "Map", "DeskPanels.razor.cs"),
        })
        {
            string source = Read("src", "SpaceSails.Client", file);
            Assert.False(source.Contains("ThePaymentIsThere", StringComparison.Ordinal),
                $"there is more than one door onto the desk — {file} asks for the payment itself.");
        }

        // The coin moves and the chest comes out of the ground, in that one method.
        Assert.Contains("_caches.Remove(paid.CacheId)", drop, StringComparison.Ordinal);
        Assert.Contains("_credits += paid.Amount;", drop, StringComparison.Ordinal);
        Assert.Contains("ParcelDrop.PaymentLine", drop, StringComparison.Ordinal);

        // …and the hole a stranger left is dated with the moment it came DUE, never with now — and it is
        // MINTED IN THE ONE FILE that mints somebody else's marks (#1105's law, enforced next door by
        // `TheGroundKeepsSomebodyElsesFootprintsTests`, which caught this crew opening a second site).
        Assert.Contains("SomebodyDugIt(lifted, paid.DueAtSimTime)", drop, StringComparison.Ordinal);
        Assert.DoesNotContain("GroundMemory.ScarKey", drop, StringComparison.Ordinal);

        string forensics = Code(Read("src", "SpaceSails.Client", "Pages", "Map.Surface.Forensics.cs"));
        Assert.Contains("private void SomebodyDugIt(TreasureCache lifted, double whenSimTime)", forensics,
            StringComparison.Ordinal);
        Assert.Contains("GroundMemory.ScarKind.Pit", forensics, StringComparison.Ordinal);
        Assert.Contains("GroundSaltFor(lifted)", forensics, StringComparison.Ordinal);
        Assert.Contains("MoonSurface.CacheSpot(lifted)", forensics, StringComparison.Ordinal);

        // Nothing here introduces a payer: no contact row is written and no name is filed.
        Assert.DoesNotContain("ContactLedger", drop, StringComparison.Ordinal);
        Assert.DoesNotContain("RecordCompletion", drop, StringComparison.Ordinal);
    }

    // ── (5) A CONFISCATION IS AN ABSENCE ────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE BOX IS READ BEFORE THE POCKET IS EMPTIED, AND WHAT FOLLOWS IS AN ABSENCE.</b> The quiet's
    /// length is seeded off the parcel that was lost, so the id has to be taken while it is still there —
    /// an order that is load-bearing and invisible: a line read one statement later finds nothing, the
    /// quiet is never written, and the desk is open again the moment the captain walks back.
    ///
    /// <para>And what the captain meets is the row NOT BEING DRAWN — the same clause shape as "he is already
    /// carrying one" — never a refusal, never a greyed button, and never a sentence.</para>
    ///
    /// <para><b>RED</b> by reading the parcel id after <c>UnlistedParcel.Confiscated</c>: <i>the box is read
    /// after the pocket is emptied</i>. <b>RED</b> by dropping
    /// <c>!TheDeskHasNothingForThisHull()</c> from <c>ParcelOnOffer</c>: <i>the desk had work the afternoon
    /// the box was carried off</i>.</para>
    /// </summary>
    [Fact]
    public void AConfiscationIsAnAbsenceAndNotARefusal()
    {
        string challenge = Code(Read("src", "SpaceSails.Client", "Pages", "Patrol", "Patrol.Challenge.cs"));
        string offer = Code(Read("src", "SpaceSails.Client", "Pages", "Map.UnlistedParcel.cs"));
        string drop = Code(Read("src", "SpaceSails.Client", "Pages", "Map.ParcelDrop.cs"));

        int readIt = challenge.IndexOf("ParcelDrop.TheParcelIn(_host.Satchel)?.Id", StringComparison.Ordinal);
        int emptied = challenge.IndexOf("UnlistedParcel.Confiscated(_host.Satchel)", StringComparison.Ordinal);
        Assert.True(readIt > 0, "the confiscation never reads which box it took.");
        Assert.True(emptied > 0, "the confiscation moved — this guard is watching a line that is gone.");
        Assert.True(readIt < emptied, "the box is read after the pocket is emptied.");
        Assert.Equal(1, Count(challenge, "_host.TheDeskHasNothingForAWhile(carriedOff)"));

        // The desk simply does not draw the row, in the offer's own vocabulary.
        Assert.Contains("&& !TheDeskHasNothingForThisHull()", offer, StringComparison.Ordinal);

        // Nothing anywhere says why: the quiet writes no pulse, no card and no field note.
        string quiet = drop.Split("TheDeskHasNothingForAWhile")[1].Split("TheDeskHasNothingForThisHull")[0];
        Assert.DoesNotContain("ShowPulseMessage", quiet, StringComparison.Ordinal);
        Assert.DoesNotContain("FileNote", quiet, StringComparison.Ordinal);
        Assert.DoesNotContain("ViewObject", quiet, StringComparison.Ordinal);

        // …and it rides the durable register the fence's own one-per-window already rides, not a new field.
        Assert.Contains("_roomsTurnedOver.Add(watch)", drop, StringComparison.Ordinal);
    }

    // ── (6) AND A DEATH CHANGES NOTHING ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE SUCCESSOR INHERITS BOTH HALVES, BECAUSE THE FACELESS PAYER CANNOT TELL.</b> A judgement call
    /// stated as a guard: an undelivered parcel rides the satchel through the wake, and a delivered one
    /// waiting to be paid rides the cache ledger — and the wake clears NEITHER. It clears the purse, the
    /// hold, the hot flags, the upgrades and the nerve, and it has never cleared what a captain has in his
    /// coat or what is buried off-ship (<c>BustedRule.ResurrectionKit</c>: <i>buried/banked survives
    /// untouched because it lives off-ship</i>).
    ///
    /// <para>That is the smallest honest behaviour and it is also the right fiction: the counterparty never
    /// saw a face, so a new face at the same desk is the same hull keeping the same appointment. No code was
    /// written for this; this guard is what says so on purpose rather than by accident.</para>
    ///
    /// <para><b>RED</b> by clearing the satchel in the wake (<c>_satchel = [];</c>): <i>the wake clears the
    /// coat</i>. <b>RED</b> by clearing the caches there: <i>the wake clears the ground</i>.</para>
    /// </summary>
    [Fact]
    public void ADeathDoesNotReachTheCoatOrTheGround()
    {
        string wake = Code(Read("src", "SpaceSails.Client", "Pages", "Map.Combat.Busted.Wake.cs"));

        // The wake really does clear things — so a "clears nothing" assertion is one this world could fail.
        Assert.Contains("_cargoByClass.Clear();", wake, StringComparison.Ordinal);
        Assert.Contains("_credits = afterBill;", wake, StringComparison.Ordinal);

        Assert.DoesNotContain("_satchel", wake, StringComparison.Ordinal);
        Assert.DoesNotContain("_caches", wake, StringComparison.Ordinal);
        Assert.DoesNotContain("_roomsTurnedOver", wake, StringComparison.Ordinal);

        // And the read that pays is over the ledger the wake leaves alone — a payment owed to a dead
        // captain is a payment owed to whoever digs the hole, which is nobody's problem but the payer's.
        var pool = new List<string> { "phobos", "luna", "miranda", "titan" };
        Satchel.Item parcel = UnlistedParcel.FromTheDesk("the-tilt", 3);
        ParcelDrop.Destination where = ParcelDrop.For(parcel, pool)!.Value;

        var ground = new CacheLedger();
        ground.Load(new TreasureCache(
            "cache:heir", where.BodyId, "the monolith", "anti-spinward", 40, 0, [], 0.0, "you", true,
            SiteIndex: where.SiteIndex, Buried: true, Deposit: [parcel]));

        double due = ParcelDrop.PayableAt(0.0, parcel.Id);
        Assert.NotNull(ParcelDrop.ThePaymentThatIsThere(ground.Caches, pool, due));
        Assert.Equal(ParcelDrop.ThePayment(parcel.Id),
            ParcelDrop.ThePaymentThatIsThere(ground.Caches, pool, due)!.Value.Amount);
    }

    // ── (7) THE DEV DOOR FORGES NOTHING ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b><c>?parcel=1</c> MINTS A REAL PARCEL AND CHOOSES ONLY A WINDOW.</b> The family's own law
    /// (<c>Map.Surface.Cheats</c>): <i>a cheat that shows a tester a different scene is worse than no cheat
    /// at all.</i> So the door does not plant a destination — it mints through
    /// <see cref="UnlistedParcel.FromTheDesk"/>, walks windows until one names ground this berth can reach,
    /// and then rides <c>?land=</c>'s existing descent.
    ///
    /// <para>It runs BEFORE the landing fires, because it writes the landing.</para>
    ///
    /// <para><b>RED</b> by constructing the destination by hand rather than reading the parcel's:
    /// <i>the dev door plants a ground</i>. <b>RED</b> by moving <c>TakeAParcelForCheat()</c> below the
    /// <c>if (_landCheat)</c>: <i>the parcel cheat runs after the landing it is supposed to aim</i>.</para>
    /// </summary>
    [Fact]
    public void TheDevDoorMintsARealParcel()
    {
        string drop = Code(Read("src", "SpaceSails.Client", "Pages", "Map.ParcelDrop.cs"));
        string start = Code(Read("src", "SpaceSails.Client", "Pages", "Map.Sim.World.Start.cs"));
        string query = Code(Read("src", "SpaceSails.Client", "Pages", "Map.Sim.World.QueryArcs.cs"));
        string links = Read("docs", "testing-links-2026-09-17.md");

        Assert.Contains("parcel=", query, StringComparison.Ordinal);
        Assert.Contains("UnlistedParcel.FromTheDesk(haven, watch + i)", drop, StringComparison.Ordinal);
        Assert.Contains("ParcelDrop.For(parcel, ground)", drop, StringComparison.Ordinal);

        // The cheat rides the shipped descent rather than a path of its own.
        Assert.Contains("_landCheat = true;", drop, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginSurfaceExcursion", drop, StringComparison.Ordinal);

        int minted = start.IndexOf("TakeAParcelForCheat();", StringComparison.Ordinal);
        int lands = start.IndexOf("if (_landCheat)", StringComparison.Ordinal);
        Assert.True(minted > 0, "nothing takes the parcel for the cheat at all.");
        Assert.True(lands > 0, "the land cheat moved — this guard is watching a line that is gone.");
        Assert.True(minted < lands, "the parcel cheat runs after the landing it is supposed to aim.");

        // …and a dev door nobody wrote down is a dev door nobody uses.
        Assert.Contains("parcel=1", links, StringComparison.Ordinal);
        Assert.Contains("#711 slice 2", links, StringComparison.Ordinal);
    }
}
