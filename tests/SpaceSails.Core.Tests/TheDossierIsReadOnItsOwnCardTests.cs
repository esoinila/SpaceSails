using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #774 · THE ASSEMBLY SAID FOUR SENTENCES UNDER ITS OWN CARD.
///
/// <para>Verified by the #768/#773 crew and deliberately left out of that PR: <c>AssembleSomebody</c> raised
/// the dossier card and <i>then</i> fired two to four <c>ShowAndFile</c> lines beneath it. Every one of them
/// played into the HUD while a full-screen backdrop stood in front of the HUD — filed durably, readable
/// nowhere — and the captain closed the card onto silence.</para>
///
/// <para><b>Why #768's hold is the wrong remedy, stated once so nobody re-proposes it.</b> A hold releases
/// ONE winner. These are a same-rank SEQUENCE composed in one breath, so the survivor would be decided by
/// which line happened to be appended last: the ordering-as-contract bug #693 killed, re-lit. The remedy is
/// #736's law instead — compose the sentences ONTO the card the act raised, the one layer a backdrop cannot
/// blur. A card has a REGION rather than a slot, so all four fit and there is no winner to pick.</para>
///
/// <para>Which turns the ordering question from "who survives" into "what does a captain read first", and
/// that is a question about MEANING. <see cref="FieldDossier.Beat"/> answers it by declaration and
/// <see cref="FieldDossier.InTheOrderTheyAreRead"/> enforces it, so the order the sayings were composed in
/// can never decide the order they are read in — which the permutation guard below pins.</para>
///
/// <para>The client half (the card's own subtree, the seam that knows about it, the dev seat that reaches
/// the four-sentence assembly) is <c>TheDossierCardCarriesItsOwnSayingsTests</c> in the Client suite.</para>
/// </summary>
public sealed class TheDossierIsReadOnItsOwnCardTests
{
    /// <summary>Real ground, and enough of it that the rare shapes turn up on their own. The rolls are keyed
    /// on (body, salt, room), so a sweep over three sites is a sweep over three different populations of
    /// stranger rather than the same one counted three times.</summary>
    private static IEnumerable<(string Body, string Salt, int Room)> EveryKitThisGroundCanAssemble()
    {
        foreach ((string body, string salt) in new[]
                 {
                     ("miranda", "RidgeCamp"), ("luna", "MassDriverRuins"), ("titan", "QuietBasin"),
                     ("phobos", "IceFissure"), ("ganymede", "RidgeCamp"), ("triton", "DerelictPad"),
                 })
        {
            for (int room = 0; room < 60; room++)
            {
                yield return (body, salt, room);
            }
        }
    }

    // ── The acceptance: nothing the assembly says is left off the card ────────────────────────────────

    /// <summary>#774's own acceptance. Every sentence the assembly produces is READABLE on the card the
    /// assembly raises, at the moment it is raised. RED on the shipped composition, where the card carried
    /// <c>Compiled</c> and nothing else and all four sentences went to the pulse behind it.</summary>
    [Fact]
    public void EverySentenceTheAssemblyProducesIsOnTheCardItRaises()
    {
        var lost = new List<string>();
        int assemblies = 0, sentences = 0;

        foreach ((string body, string salt, int room) in EveryKitThisGroundCanAssemble())
        {
            IReadOnlyList<FieldDossier.Saying> debrief = FieldDossier.Debrief(body, salt, room);
            string card = FieldDossier.DebriefBlock(debrief);
            assemblies++;
            sentences += debrief.Count;

            foreach (FieldDossier.Saying said in debrief)
            {
                if (!card.Contains(said.Text, StringComparison.Ordinal))
                {
                    lost.Add($"{body}/{salt} room {room}: the {said.Beat} sentence " +
                        $"'{Short(said.Text)}' is filed and is not on the card — it played under the " +
                        "dossier's own backdrop and was never read (#774).");
                }
            }
        }

        // Anti-vacuity: a sweep that assembled nobody, or that only ever produced one sentence, would pass
        // this by describing a world that cannot tell pass from fail.
        Assert.True(assemblies > 300, $"only {assemblies} assemblies swept — this guard is proving nothing.");
        Assert.True(sentences > assemblies * 2,
            $"{sentences} sentences over {assemblies} assemblies — a sweep with no multi-saying assembly " +
            "in it cannot see the bug #774 is about.");

        Assert.True(lost.Count == 0,
            $"{lost.Count} of {sentences} sentence(s) the assembly says are not on its card:\n  " +
            string.Join("\n  ", lost.Take(6)));
    }

    /// <summary>The anti-vacuity claim on its own, asserted rather than assumed: the ground this suite sweeps
    /// really does produce assemblies that say two, three and four things, so the ordering laws below are
    /// laws about a real sequence and not about a list of one.</summary>
    [Fact]
    public void TheGroundProducesAssembliesOfTwoThreeAndFourSentences()
    {
        var byCount = new Dictionary<int, int>();
        foreach ((string body, string salt, int room) in EveryKitThisGroundCanAssemble())
        {
            int n = FieldDossier.Debrief(body, salt, room).Count;
            byCount[n] = byCount.TryGetValue(n, out int had) ? had + 1 : 1;
        }

        foreach (int n in new[] { 2, 3, 4 })
        {
            Assert.True(byCount.TryGetValue(n, out int seen) && seen > 5,
                $"only {(byCount.TryGetValue(n, out int c) ? c : 0)} assemblies of {n} sentence(s) in the " +
                "whole sweep — the multi-saying case #774 is about is not being exercised. " +
                $"Counts: {string.Join(", ", byCount.OrderBy(p => p.Key).Select(p => $"{p.Key}→{p.Value}"))}");
        }

        // Two is the floor and four is the ceiling: who they were and who is waiting are unconditional, the
        // family's knowledge and the in are one-in-three each.
        Assert.Equal(2, byCount.Keys.Min());
        Assert.Equal(4, byCount.Keys.Max());
    }

    // ── The order, and where it comes from ────────────────────────────────────────────────────────────

    /// <summary>The sentences are read in the order their MEANING declares — you work out who before you can
    /// carry news of them, you learn who is waiting before what that person knows, and the in comes last
    /// because it is the only one that is not about the dead. Pinned as: the beats a card carries are
    /// strictly ascending in <see cref="FieldDossier.Beat"/>'s declared order, every time.</summary>
    [Fact]
    public void TheSentencesAreReadInTheOrderTheirMEANINGDeclares()
    {
        var wrong = new List<string>();
        int swept = 0;

        foreach ((string body, string salt, int room) in EveryKitThisGroundCanAssemble())
        {
            IReadOnlyList<FieldDossier.Saying> debrief = FieldDossier.Debrief(body, salt, room);
            swept++;
            for (int i = 1; i < debrief.Count; i++)
            {
                if (debrief[i - 1].Beat >= debrief[i].Beat)
                {
                    wrong.Add($"{body}/{salt} room {room}: {debrief[i - 1].Beat} is read before " +
                        $"{debrief[i].Beat} — the card is in composition order, not reading order (#774).");
                }
            }
        }

        Assert.True(swept > 300, $"only {swept} assemblies swept — this guard is proving nothing.");
        Assert.True(wrong.Count == 0, string.Join("\n  ", wrong.Take(6)));
    }

    /// <summary>THE ORDER THEY WERE COMPOSED IN CANNOT DECIDE THE ORDER THEY ARE READ IN — #693's killed
    /// contract, refused in advance. Every permutation of a real assembly's sayings is handed to the ordering
    /// rule and every one of them must come back identical. RED the moment the rule is "the list as it came".
    /// </summary>
    [Fact]
    public void TheOrderTheSentencesWereCOMPOSEDInCannotDecideTheOrderTheyAreREAD()
    {
        int permuted = 0;
        var wrong = new List<string>();

        foreach ((string body, string salt, int room) in EveryKitThisGroundCanAssemble())
        {
            IReadOnlyList<FieldDossier.Saying> debrief = FieldDossier.Debrief(body, salt, room);
            if (debrief.Count < 2)
            {
                continue;
            }

            string canonical = string.Join(" | ", debrief.Select(s => s.Beat.ToString()));
            foreach (List<FieldDossier.Saying> order in Permutations([.. debrief]))
            {
                permuted++;
                string got = string.Join(" | ",
                    FieldDossier.InTheOrderTheyAreRead(order).Select(s => s.Beat.ToString()));
                if (!string.Equals(canonical, got, StringComparison.Ordinal))
                {
                    wrong.Add($"{body}/{salt} room {room}: composed as " +
                        $"[{string.Join(" | ", order.Select(s => s.Beat))}] the card reads back [{got}] — " +
                        $"the order it was composed in decided it, and it must read [{canonical}] " +
                        "whatever order it arrived in (#774/#693).");
                }
            }
        }

        Assert.True(permuted > 2000,
            $"only {permuted} permutations swept — this guard is proving nothing.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} composition order(s) changed what the card reads:\n  " +
            string.Join("\n  ", wrong.Take(6)));
    }

    /// <summary>…and the ordering rule is not merely stable, it is the ENUM. Stated separately because a rule
    /// that sorted consistently by something else — length, glyph, alphabet — would satisfy the permutation
    /// guard above while reading the dossier out in an order nobody chose.</summary>
    [Fact]
    public void TheReadingOrderIsTheDeclaredOrderOfTheBeatsThemselves()
    {
        FieldDossier.Beat[] declared = Enum.GetValues<FieldDossier.Beat>();
        Assert.Equal(4, declared.Length);
        Assert.Equal(
            [FieldDossier.Beat.WhoTheyWere, FieldDossier.Beat.WhoIsWaiting,
             FieldDossier.Beat.WhatTheFamilyKnows, FieldDossier.Beat.TheIn],
            declared);

        // One of everything, handed over backwards.
        var backwards = declared.Reverse()
            .Select(b => new FieldDossier.Saying(b, "•", $"the {b} sentence"))
            .ToList();

        Assert.Equal(declared, FieldDossier.InTheOrderTheyAreRead(backwards).Select(s => s.Beat).ToArray());
    }

    // ── And the book is untouched ─────────────────────────────────────────────────────────────────────

    /// <summary>THE FILING IS UNCHANGED. The card is where a sentence is now READ; it is not a change to what
    /// is RECORDED. Rebuilt here from the primitives the shipped code called, in the order it called them —
    /// so a rewrite that improved a sentence on its way to the card would fail rather than quietly rewrite
    /// nine years of somebody's field book.</summary>
    [Fact]
    public void TheBookKeepsTheSameSentencesUnderTheSameGlyphsInTheSameOrder()
    {
        int checkedRooms = 0;

        foreach ((string body, string salt, int room) in EveryKitThisGroundCanAssemble())
        {
            FieldDossier.Person who = FieldDossier.Who(body, salt, room);

            // The shipped sequence, written out longhand: the person, the next of kin, what the family
            // knows (if they know anything), the in (if the kit carried one).
            var shipped = new List<(string Glyph, string Text)>
            {
                ("🗂", $"🗂 {who.Name} — a specialist in {who.Discipline}, carried out here by " +
                    $"{who.Employer}. The kit is theirs, all of it, and now you know their name."),
                ("📇", FieldDossier.KinLine(who, FieldDossier.WhoIsWaiting(body, salt, room))),
            };
            if (FieldDossier.KinKnowsSomething(body, salt, room))
            {
                shipped.Add(("🔎", FieldDossier.LeadHint(body, salt, room)));
            }
            if (FieldDossier.HasIntroduction(body, salt, room))
            {
                shipped.Add(("🎟", FieldDossier.IntroductionLine(FieldDossier.InTheKit(body, salt, room))));
            }

            IReadOnlyList<FieldDossier.Saying> debrief = FieldDossier.Debrief(body, salt, room);
            Assert.Equal(shipped.Count, debrief.Count);
            for (int i = 0; i < shipped.Count; i++)
            {
                Assert.Equal(shipped[i].Glyph, debrief[i].Glyph);
                Assert.Equal(shipped[i].Text, debrief[i].Text);
            }
            checkedRooms++;
        }

        Assert.True(checkedRooms > 300, $"only {checkedRooms} rooms checked — this guard is proving nothing.");
    }

    /// <summary>The card dresses a sentence, the book does not. Three of the four sentences are authored with
    /// their own glyph at the head; the family's lead is bare prose that the book files under 🔎, and the
    /// card gives it the same glyph so the four rows read as one list. No sentence is invented here and none
    /// is edited: the filed text appears in the block verbatim.</summary>
    [Fact]
    public void TheCardAddsAGlyphAndNeverAWord()
    {
        int rows = 0;

        foreach ((string body, string salt, int room) in EveryKitThisGroundCanAssemble())
        {
            IReadOnlyList<FieldDossier.Saying> debrief = FieldDossier.Debrief(body, salt, room);
            string[] block = FieldDossier.DebriefBlock(debrief).Split("\n\n");

            Assert.True(debrief.Count == block.Length,
                $"{body}/{salt} room {room}: the assembly says {debrief.Count} sentence(s) and the card " +
                $"carries {block.Length} — the card is not the carrier of what was said (#774).");
            for (int i = 0; i < debrief.Count; i++)
            {
                Assert.StartsWith(debrief[i].Glyph, block[i], StringComparison.Ordinal);
                Assert.EndsWith(debrief[i].Text, block[i], StringComparison.Ordinal);

                // Everything the card shows beyond the filed sentence is the glyph and one space, or nothing.
                string added = block[i][..(block[i].Length - debrief[i].Text.Length)];
                Assert.True(added.Length == 0 || added == debrief[i].Glyph + " ",
                    $"the card put '{added}' in front of a filed sentence — the dossier is not allowed to " +
                    "write prose of its own (#774).");
                rows++;
            }
        }

        Assert.True(rows > 800, $"only {rows} rows checked — this guard is proving nothing.");
    }

    // ── The dev seat ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>#774's demo, in Core: <c>?kit=1</c>'s forced rolls reach the FOUR-sentence assembly — the one
    /// the card had to be able to carry — on any room at all. A scene nobody can reach on demand is a scene
    /// that ships broken, and this one is three papers rooms and two one-in-three rolls deep.</summary>
    [Fact]
    public void TheDevSeatReachesTheFourSentenceAssemblyOnAnyRoom()
    {
        foreach ((string body, string salt, int room) in EveryKitThisGroundCanAssemble().Take(40))
        {
            IReadOnlyList<FieldDossier.Saying> debrief =
                FieldDossier.Debrief(body, salt, room, everySaying: true);

            Assert.Equal(Enum.GetValues<FieldDossier.Beat>(), debrief.Select(s => s.Beat).ToArray());

            // And it is the SEEDED dossier for that room, not an invented one: the cheat moves the gates and
            // nothing behind them, so a tester reads a card a captain can genuinely be handed.
            Assert.Equal(
                FieldDossier.WhoTheyWereLine(FieldDossier.Who(body, salt, room)),
                debrief[0].Text);
            Assert.Equal(FieldDossier.LeadHint(body, salt, room), debrief[2].Text);
        }
    }

    // ── #805 · THE KIN WHO SHARED THE GRAVE'S NAME ────────────────────────────────────────────────────

    /// <summary>
    /// #805 · <b>A NEXT-OF-KIN IS FAMILY, NEVER THE SAME PERSON.</b>
    ///
    /// <para>Flagged by the #741/#800 crew off a real board: <see cref="FieldDossier.WhoIsWaiting"/> drew the
    /// kin's given name from the deceased's own pool with nothing held out, so some seeded rooms handed back
    /// somebody waiting for news of a person with their IDENTICAL full name. <c>luna / DepotApron / room 2</c>
    /// is the one that was walked into.</para>
    ///
    /// <para>It costs more than a dull name would, because of where those two names END UP: the red-pen
    /// board, the one surface in this game where a captain goes hunting for a repeated name on purpose.
    /// #741's spotting law is that the catches must be REAL — a collision manufactured by the draw is a false
    /// positive planted in the middle of the evidence.</para>
    ///
    /// <para>The sweep is over every seeded room this game can put a stranger in: every body in Sol, every
    /// landing site that body offers (site 0's empty salt included), and rooms well past any layout's count.
    /// The surname must still MATCH — sharing it is the whole point, and a "fix" handing the kin a fresh
    /// family name would pass a collision check while quietly deleting the family.</para>
    ///
    /// <para><b>Proven RED</b> by restoring the undrawn pick (<c>Pick(Given, tag + ":given")</c>): see the PR
    /// body for the run and the rooms it named.</para>
    /// </summary>
    [Fact]
    public void NoSeededRoom_HandsBackAKinWithTheDeceasedsOwnFullName()
    {
        var collisions = new List<string>();
        var strangers = new List<string>();
        int rooms = 0;

        foreach (CelestialBody body in CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol()).Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body.Id))
            {
                for (int room = 0; room < 80; room++)
                {
                    rooms++;
                    FieldDossier.Person who = FieldDossier.Who(body.Id, site.LayoutSalt, room);
                    FieldDossier.NextOfKin kin = FieldDossier.WhoIsWaiting(body.Id, site.LayoutSalt, room);

                    if (string.Equals(who.Name, kin.Name, StringComparison.Ordinal))
                    {
                        collisions.Add($"{body.Id}/{site.LayoutSalt}/{room} → {who.Name}");
                    }

                    // …and they are still FAMILY. The surname is the sentence nobody has to write.
                    Assert.Equal(Surname(who.Name), Surname(kin.Name));
                    strangers.Add(who.Name);
                }
            }
        }

        // The sweep actually saw the ground — a room count of zero passes every assertion above.
        Assert.True(rooms > 500, $"the sweep only visited {rooms} rooms; it cannot have seen the ground.");
        Assert.True(strangers.Distinct(StringComparer.Ordinal).Count() > 20,
            "every room produced the same stranger — this sweep is one seed wearing a loop.");

        Assert.True(collisions.Count == 0,
            "rooms whose next-of-kin has the dead person's exact full name (a false rhyme on the red-pen " +
            $"board, #741): {string.Join(", ", collisions.Take(12))}" +
            (collisions.Count > 12 ? $" …and {collisions.Count - 12} more" : ""));
    }

    /// <summary>#805's own repro, pinned by name so the issue's sentence stays runnable even if the sweep
    /// above is ever refactored. They are family; they are not the same person.</summary>
    [Fact]
    public void TheReportedRoom_LunaDepotApronTwo_IsTwoDifferentPeople()
    {
        FieldDossier.Person who = FieldDossier.Who("luna", "DepotApron", 2);
        FieldDossier.NextOfKin kin = FieldDossier.WhoIsWaiting("luna", "DepotApron", 2);

        Assert.NotEqual(who.Name, kin.Name);
        Assert.Equal(Surname(who.Name), Surname(kin.Name));
    }

    // ── helpers ───────────────────────────────────────────────────────────────────────────────────────

    private static string Surname(string full) =>
        full[(full.IndexOf(' ', StringComparison.Ordinal) + 1)..];

    private static string Short(string s) => s.Length <= 46 ? s : s[..46] + "…";

    private static IEnumerable<List<FieldDossier.Saying>> Permutations(List<FieldDossier.Saying> items)
    {
        if (items.Count <= 1)
        {
            yield return items;
            yield break;
        }
        for (int i = 0; i < items.Count; i++)
        {
            var rest = new List<FieldDossier.Saying>(items);
            FieldDossier.Saying head = rest[i];
            rest.RemoveAt(i);
            foreach (List<FieldDossier.Saying> tail in Permutations(rest))
            {
                var one = new List<FieldDossier.Saying> { head };
                one.AddRange(tail);
                yield return one;
            }
        }
    }
}
