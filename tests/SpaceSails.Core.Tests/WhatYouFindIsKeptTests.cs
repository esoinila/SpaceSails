using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #587 · THE FIELD BOOK. Owner, on a rebuilt site: <i>"this site looks good... maybe more tips and items.
/// Also we should maybe collect the tips to ledger if we don't show them again?"</i>
///
/// <para>Everything found on a surface used to arrive through a pulse that fades in at most eight seconds
/// and is then gone: a sentence you walked twenty minutes across a vacuum for and could never read twice.
/// The bar had the same bug and the same ruling (#347) — <i>"the words a player paid a round to hear may not
/// hide"</i>.</para>
/// </summary>
public sealed class WhatYouFindIsKeptTests
{
    private static FieldNote Note(string text, double t, string place) => new(text, t, place, "📄");

    [Fact]
    public void ANoteSurvivesBeingFiled()
    {
        IReadOnlyList<FieldNote> book = FieldNotes.Append(null, Note("a roster, half burned", 10, "Miranda · A"));
        Assert.Single(book);
        Assert.Equal("a roster, half burned", book[0].Text);
    }

    [Fact]
    public void TheBookIsCappedAndKeepsTheNEWEST()
    {
        IReadOnlyList<FieldNote> book = [];
        for (int i = 0; i < FieldNotes.Cap + 25; i++)
        {
            book = FieldNotes.Append(book, Note($"find {i}", i, "Miranda · A"));
        }

        Assert.Equal(FieldNotes.Cap, book.Count);
        Assert.Equal($"find {FieldNotes.Cap + 24}", book[^1].Text);
        Assert.DoesNotContain(book, n => n.Text == "find 0");
    }

    [Fact]
    public void PressingEATwiceOnOneFloorDoesNotFileItTwice()
    {
        // Small mercy, but the ledger is meant to be a memory of an afternoon and a doubled line reads as a
        // bug in the record rather than a fact about the world.
        IReadOnlyList<FieldNote> book = FieldNotes.Append(null, Note("the same docket", 5, "Miranda · A"));
        book = FieldNotes.Append(book, Note("the same docket", 6, "Miranda · A"));
        Assert.Single(book);

        // But the SAME text found somewhere else is a different fact and is kept.
        book = FieldNotes.Append(book, Note("the same docket", 7, "Luna · B"));
        Assert.Equal(2, book.Count);
    }

    [Fact]
    public void TheLedgerGroupsByPLACEAndLeadsWithTheMostRecentGround()
    {
        // Grouped by place rather than by kind, on purpose: "three papers and two caches" is an inventory,
        // "Miranda · The Ridge Camp" with four lines under it is a memory of an afternoon.
        IReadOnlyList<FieldNote> book = [];
        book = FieldNotes.Append(book, Note("older find", 100, "Miranda · The Ridge Camp"));
        book = FieldNotes.Append(book, Note("a Luna thing", 500, "Luna · The Depot Apron"));
        book = FieldNotes.Append(book, Note("newer find", 900, "Miranda · The Ridge Camp"));

        IReadOnlyList<FieldFinding> found = FieldNotes.PerPlace(book);

        Assert.Equal(2, found.Count);
        Assert.Equal("Miranda · The Ridge Camp", found[0].Place);      // most recently visited leads
        Assert.Equal("newer find", found[0].Lines[0].Text);            // newest first within a ground
        Assert.Equal("older find", found[0].Lines[1].Text);
        Assert.Equal("Luna · The Depot Apron", found[1].Place);
    }

    [Fact]
    public void AnEmptyBookProjectsToNothingRatherThanBlowingUp()
    {
        Assert.Empty(FieldNotes.PerPlace(null));
        Assert.Empty(FieldNotes.PerPlace([]));
    }

    [Fact]
    public void APlaceIsAlwaysNameable()
    {
        Assert.Equal("Miranda · The Wild Plain", FieldNotes.PlaceLabel("Miranda", "The Wild Plain"));
        Assert.Equal("Miranda", FieldNotes.PlaceLabel("Miranda", ""));
        Assert.Equal("somewhere", FieldNotes.PlaceLabel(null, null));
    }
}

/// <summary>
/// #690 · THE BOOK OPENS WHERE YOU ARE STANDING. Owner: <i>"should we have notes / clues section in our
/// inventory ui?"</i> — and on the register: <i>"it's like our detective notepad :-D"</i>.
///
/// <para>The satchel's NOTES tab reads the one field book (#587) and defaults to the ground underfoot,
/// because a captain at a sealed door wants what THIS building has told them and not the memoirs. The Core
/// half of that is one projection: the ledger's own grouping, filtered to a place — one grouping law, one
/// ordering, one place that can be wrong.</para>
/// </summary>
public sealed class TheBookOpensWhereYouAreStandingTests
{
    private static FieldNote Note(string text, double t, string place) => new(text, t, place, "📄");

    private static IReadOnlyList<FieldNote> Walked()
    {
        IReadOnlyList<FieldNote> book = [];
        book = FieldNotes.Append(book, Note("a roster, half burned", 100, "Miranda · The Ridge Camp"));
        book = FieldNotes.Append(book, Note("a Luna thing", 500, "Luna · The Depot Apron"));
        book = FieldNotes.Append(book, Note("bootprints, none leading away", 900, "Miranda · The Ridge Camp"));
        return book;
    }

    [Fact]
    public void ThisGroundGivesBackThisGroundsLinesNewestFirst()
    {
        IReadOnlyList<FieldNote> here = FieldNotes.Here(Walked(), "Miranda · The Ridge Camp");

        Assert.Equal(2, here.Count);
        Assert.Equal("bootprints, none leading away", here[0].Text);   // newest first, as the ledger orders
        Assert.Equal("a roster, half burned", here[1].Text);
    }

    [Fact]
    public void ItIsTheLEDGERSOwnGroupingAndNotASecondReadingOfTheLog()
    {
        // The law worth pinning: whatever PerPlace says about a ground, Here says exactly the same thing
        // about that ground. Two projections that could disagree would be two answers to "what have I found
        // here" — the repo's first named bug class aimed at a list.
        IReadOnlyList<FieldNote> book = Walked();
        foreach (FieldFinding found in FieldNotes.PerPlace(book))
        {
            Assert.Equal(
                found.Lines.Select(l => l.Text),
                FieldNotes.Here(book, found.Place).Select(l => l.Text));
        }
    }

    [Fact]
    public void AGroundThatHasToldYouNothingSaysNothingRatherThanBlowingUp()
    {
        // The empty state the tab is written around: standing somewhere the book has never been opened.
        Assert.Empty(FieldNotes.Here(Walked(), "Triton · The Sink"));
        Assert.Empty(FieldNotes.Here(null, "Miranda · The Ridge Camp"));
        Assert.Empty(FieldNotes.Here([], "Miranda · The Ridge Camp"));

        // Off a surface there is no ground to be on, and the book is honest about it rather than guessing.
        Assert.Empty(FieldNotes.Here(Walked(), null));
    }

    [Fact]
    public void APlaceIsMatchedAsTheBookWROTEItAndNotNearly()
    {
        // The filter takes a PlaceLabel. Anything else — a body with no site, a site with no body, a
        // hand-built near-miss — matches nothing, which is what makes re-deriving the string a bug you can
        // see rather than one that silently half-works.
        IReadOnlyList<FieldNote> book = Walked();

        Assert.Equal(2, FieldNotes.Here(book, FieldNotes.PlaceLabel("Miranda", "The Ridge Camp")).Count);
        Assert.Empty(FieldNotes.Here(book, "Miranda"));
        Assert.Empty(FieldNotes.Here(book, "The Ridge Camp"));
        Assert.Empty(FieldNotes.Here(book, "Miranda - The Ridge Camp"));
    }
}

/// <summary>
/// #588 · WHOSE KIT WAS THIS. Owner: <i>"when we find somebody's kit maybe we get gen ai compilation of what
/// we discover about them... like some top notch scientist being found on far away moon .... what were they
/// doing there... maybe they are in photos posing with important politicians and officials :-D"</i> — then
/// the part that makes it a mechanic: <i>"if we know what happened to someone we get contacts easily by
/// contacting their loved ones, in some cases that might lead our gum-shoe-efforts forward."</i>
/// </summary>
public sealed class WhoseKitWasThisTests
{
    [Fact]
    public void APersonIsTheSamePersonEveryTimeYouSearchThatRoom()
    {
        // Determinism is law, and here it is also fiction: two captains comparing notes about the same ruin
        // must be talking about the same dead stranger.
        for (int room = 0; room < 20; room++)
        {
            Assert.Equal(
                FieldDossier.Who("miranda", "RidgeCamp", room),
                FieldDossier.Who("miranda", "RidgeCamp", room));
        }
    }

    [Fact]
    public void DifferentRoomsHoldDifferentPeople()
    {
        var names = new HashSet<string>();
        for (int room = 0; room < 30; room++)
        {
            names.Add(FieldDossier.Who("miranda", "RidgeCamp", room).Name);
        }
        Assert.True(names.Count > 10, "every ruin on the moon holds the same corpse.");
    }

    [Fact]
    public void TheKinSHARESTheFamilyName()
    {
        // The detail that makes the connection obvious on sight and needs no sentence explaining it.
        for (int room = 0; room < 20; room++)
        {
            FieldDossier.Person who = FieldDossier.Who("miranda", "RidgeCamp", room);
            FieldDossier.NextOfKin kin = FieldDossier.WhoIsWaiting("miranda", "RidgeCamp", room);

            string family = who.Name[(who.Name.IndexOf(' ') + 1)..];
            Assert.EndsWith(family, kin.Name, System.StringComparison.Ordinal);
            Assert.NotEqual(who.Name, kin.Name);
        }
    }

    [Fact]
    public void NotEveryKindnessPaysOutAndNotEveryKitCarriesAnIn()
    {
        // A game where every decent act is rewarded is not offering a choice, and an "in" you find every
        // time is a menu option rather than a discovery.
        int leads = 0, ins = 0;
        const int rooms = 300;
        for (int room = 0; room < rooms; room++)
        {
            if (FieldDossier.KinKnowsSomething("miranda", "RidgeCamp", room)) { leads++; }
            if (FieldDossier.HasIntroduction("miranda", "RidgeCamp", room)) { ins++; }
        }

        Assert.InRange(leads, rooms / 8, rooms / 2);
        Assert.InRange(ins, rooms / 8, rooms / 2);
    }

    [Fact]
    public void TheCompiledCardNEVERExplainsWhatTheOldOnesAre()
    {
        // THE canon rule (owner ruling 2026-07-30): the Old Ones are failed restores procured at scale, and
        // that is never stated in a card and never confirmed by a sensor. A dossier may show a continuity
        // researcher shaking hands with a ministry delegation. It may not join the dots — the player does
        // that, or does not.
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "workforce", "slave"];

        for (int room = 0; room < 40; room++)
        {
            FieldDossier.Person who = FieldDossier.Who("miranda", "RidgeCamp", room);
            string card = FieldDossier.Compiled(who, "Miranda · The Ridge Camp");
            string kin = FieldDossier.KinLine(who, FieldDossier.WhoIsWaiting("miranda", "RidgeCamp", room));

            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, card, System.StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(bad, kin, System.StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void TheCardIsWorthTheWalk()
    {
        FieldDossier.Person who = FieldDossier.Who("miranda", "", 3);
        string card = FieldDossier.Compiled(who, "Miranda · The Wild Plain");

        Assert.Contains(who.Name, card, System.StringComparison.Ordinal);
        Assert.Contains("Miranda · The Wild Plain", card, System.StringComparison.Ordinal);
        Assert.True(card.Length > 600, "the compiled card is too thin to be worth assembling.");
        Assert.EndsWith("art/dossier-effects.jpg", FieldDossier.ArtUrl, System.StringComparison.Ordinal);
    }

    [Fact]
    public void AnIntroductionNamesAPhraseAPersonAndAPlace()
    {
        // "Ins to people and places" — an in that does not say WHERE it works is a fortune cookie.
        for (int room = 0; room < 20; room++)
        {
            FieldDossier.Introduction intro = FieldDossier.InTheKit("miranda", "RidgeCamp", room);
            Assert.False(string.IsNullOrWhiteSpace(intro.Phrase));
            Assert.False(string.IsNullOrWhiteSpace(intro.Whom));
            Assert.False(string.IsNullOrWhiteSpace(intro.Where));

            // And the phrase is the dead person's own name — the quiet, unremarked-on part.
            Assert.Contains(FieldDossier.Who("miranda", "RidgeCamp", room).Name, intro.Phrase,
                System.StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ThreePiecesMakeAPerson()
    {
        // One is litter, two is a coincidence.
        Assert.Equal(3, FieldDossier.FragmentsToAssemble);
    }
}

/// <summary>#590 · OUT ON THE GROUND IS NOT A DEEP SITE. Owner, on a moon: <i>"the hull flexing feeling
/// buildings should not come when on planet / moon ... we should have place specific ones for the sites, not
/// generic ones of the ship playing on site."</i>
///
/// <para>The setting flag was already right — a surface excursion selected <c>DeepSite</c>. The WORDS were
/// wrong, which is the harder bug: every deep-site line names a hull, a room and other people, because it was
/// written for a sealed site with a crew in it. On a moon it therefore read as the ship's voice, which is
/// exactly what it was.</para></summary>
public sealed class TheGroundHasItsOwnVoiceTests
{
    [Fact]
    public void RegolithGetsItsOwnPoolAndNotTheDeepSitesOne()
    {
        var outside = HullShudder.LinesFor(HullShudder.Setting.Regolith);
        var inside = HullShudder.LinesFor(HullShudder.Setting.DeepSite);

        Assert.NotEmpty(outside);
        Assert.True(outside.All(l => !inside.Contains(l)),
            "the ground is still speaking the sealed site's lines.");
    }

    [Fact]
    public void NoRegolithLineMentionsAHullARoomOrACrowd()
    {
        // The specific complaint, as a law. Out there the captain is outside all three of those things.
        string[] forbidden =
            ["hull", "the room", "the bar", "the deck", "the crew", "everyone", "everybody", "every head"];

        foreach (string line in HullShudder.LinesFor(HullShudder.Setting.Regolith))
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, System.StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void AndNothingOutThereHappensTOGETHER()
    {
        // The unison beat is the whole shape of this mechanic, and on a moon there is nobody to be in unison
        // with. That is not a reason to drop the beat — it is the best thing that could happen to it — but a
        // line that says "together" out there is describing a crowd that is not present.
        foreach (string line in HullShudder.LinesFor(HullShudder.Setting.Regolith))
        {
            Assert.DoesNotContain("together", line, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("as one", line, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("in unison", line, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ASEALEDSiteStillGetsItsCrowdedVoice()
    {
        // The other half must survive: a lab or a wreck DOES have a hull, a room and people in it.
        Assert.Equal(HullShudder.Setting.DeepSite,
            HullShudder.SettingOutside(onRegolith: false, deepSite: true, haven: false));
        Assert.Equal(HullShudder.Setting.Regolith,
            HullShudder.SettingOutside(onRegolith: true, deepSite: true, haven: false));

        // And nothing about the ship or the bar changed.
        Assert.Equal(HullShudder.Setting.Ship,
            HullShudder.SettingOutside(onRegolith: false, deepSite: false, haven: false));
        Assert.Equal(HullShudder.Setting.Haven,
            HullShudder.SettingOutside(onRegolith: false, deepSite: false, haven: true));
    }
}

/// <summary>#589 · EVERY WORLD ITS OWN COLOUR. Owner, mid-tour: <i>"the in-situ construction materials of the
/// walls might be planet specific ... red for mars etc theming"</i> / <i>"gray for Moon"</i> / <i>"something
/// to spot where we are visually"</i>.</summary>
public sealed class EveryWorldItsOwnColourTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    [Fact]
    public void NoTwoWorldsAreDrawnInTheSameInk()
    {
        // The whole requirement is "something to spot where we are visually". Two moons sharing an ink is
        // two moons you cannot tell apart, which is the state we started from.
        var seen = new HashSet<(byte, byte, byte)>();
        foreach (string body in Bodies)
        {
            BodyPalette.Ink ink = BodyPalette.For(body);
            Assert.True(seen.Add((ink.R, ink.G, ink.B)), $"{body} shares its ink with another world.");
        }
    }

    [Fact]
    public void NeighboursInOneSystemAreTOLDApartTheHardest()
    {
        // Jupiter's three are the real test: they are reached from one berth, so they are the pair-wise
        // comparison a player actually makes.
        (string A, string B)[] neighbours =
            [("europa", "ganymede"), ("ganymede", "callisto"), ("europa", "callisto"), ("titan", "enceladus")];

        foreach ((string a, string b) in neighbours)
        {
            BodyPalette.Ink x = BodyPalette.For(a), y = BodyPalette.For(b);
            int spread = System.Math.Abs(x.R - y.R) + System.Math.Abs(x.G - y.G) + System.Math.Abs(x.B - y.B);
            Assert.True(spread > 40, $"{a} and {b} are within {spread} of one another — same system, same look.");
        }
    }

    [Fact]
    public void TheOwnersTwoExamplesAreHonoured()
    {
        // "gray for Moon" — the Moon reads neutral: no channel stands out from the others.
        BodyPalette.Ink luna = BodyPalette.For("luna");
        int lunaSpread = System.Math.Max(luna.R, System.Math.Max(luna.G, luna.B))
            - System.Math.Min(luna.R, System.Math.Min(luna.G, luna.B));
        Assert.True(lunaSpread <= 12, "Luna is not grey.");

        // "red for mars etc" — Phobos, the body you actually land on in Mars' system, reads warm.
        BodyPalette.Ink phobos = BodyPalette.For("phobos");
        Assert.True(phobos.R > phobos.G && phobos.G > phobos.B, "Phobos does not read as rusted.");
    }

    [Fact]
    public void AnUnknownWorldFallsBackToPlainRegolithRatherThanSomethingLoud()
    {
        // A body added without a palette entry should look ORDINARY, not broken.
        Assert.Equal(BodyPalette.Regolith, BodyPalette.For("a-moon-nobody-has-added-yet"));
        Assert.Equal(BodyPalette.Regolith, BodyPalette.For(null));
        Assert.Equal("local regolith", BodyPalette.MaterialOf("a-moon-nobody-has-added-yet"));
    }

    [Fact]
    public void EveryWorldCanSayWhatItsWallsAreMadeOf()
    {
        foreach (string body in Bodies)
        {
            Assert.False(string.IsNullOrWhiteSpace(BodyPalette.MaterialOf(body)));
            Assert.NotEqual("local regolith", BodyPalette.MaterialOf(body));
        }
    }
}

/// <summary>#592 · THE PALETTE AS A LANGUAGE. Owner: <i>"maybe the door color could also be moon / landing
/// site specific ... some special color not distinctive to the site could then used to draw our attention to
/// a place (like expensive door made with far away imported materials)."</i>
///
/// <para>Once every door on a moon is the colour of that moon, the one door that is not becomes a sentence.
/// The whole mechanic rests on the imported ink being unmistakable against every world — if it is ever a
/// subtle shade of local, the language stops working exactly where it matters.</para></summary>
public sealed class AnImportedDoorIsASentenceTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    [Fact]
    public void ImportedStandsWellClearOfEVERYWorldsOwnDoors()
    {
        // The load-bearing assertion. Enceladus is the hard case — the brightest, palest surface in the
        // system — and if the signal survives there it survives anywhere.
        foreach (string body in Bodies)
        {
            int contrast = BodyPalette.ImportedContrastFrom(body);
            Assert.True(contrast > 110,
                $"an imported door on {body} is only {contrast} from the local stone — it reads as ordinary.");
        }
    }

    [Fact]
    public void ImportedIsNotAnyWorldsCOLOURSoItCannotSayWhereYouAre()
    {
        // It must never be mistakable for "we are on that moon", or a captain landing somewhere pale would
        // read every door as imported and the signal would inverate into noise.
        foreach (string body in Bodies)
        {
            Assert.NotEqual(BodyPalette.For(body), BodyPalette.Imported);
            Assert.NotEqual(BodyPalette.DoorInk(body), BodyPalette.Imported);
        }
    }

    [Fact]
    public void ADoorReadsAsTheHillItIsSetInOnlyBrighter()
    {
        // A hatch should look made of the same rock as the wall around it — related, not identical, or the
        // building stops reading as one object.
        foreach (string body in Bodies)
        {
            BodyPalette.Ink wall = BodyPalette.For(body);
            BodyPalette.Ink door = BodyPalette.DoorInk(body);

            Assert.True(door.R >= wall.R && door.G >= wall.G && door.B >= wall.B,
                $"{body}: the door is darker than the wall it is set in.");
            Assert.NotEqual(wall, door);
        }
    }

    [Fact]
    public void TheDoorInkStillTellsWorldsApart()
    {
        // The #589 requirement has to survive the brightening: two moons whose DOORS match are two moons you
        // cannot tell apart the moment you are standing at a building, which is most of the time.
        var seen = new HashSet<(byte, byte, byte)>();
        foreach (string body in Bodies)
        {
            BodyPalette.Ink d = BodyPalette.DoorInk(body);
            Assert.True(seen.Add((d.R, d.G, d.B)), $"{body}'s doors match another world's.");
        }
    }
}
