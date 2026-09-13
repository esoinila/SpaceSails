using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #563 · <b>BREADCRUMBS COME FROM YOUR OWN LINEAGE FIRST.</b>
///
/// <para>Owner ruling, 2026-09-13, closing the last of #563's three open questions — <i>strangers or your
/// own lineage?</i> — verbatim: <i>"I love the own lineage. If not enough material, fill in with strangers,
/// preferably NPCs we know something about."</i></para>
///
/// <para>And the loop it serves, from #455: <i>"after pirate insurance rebirth you can come see if your loot
/// is still there."</i> A captain who dies on a moon is replaced by the policy, and the new one flies back
/// to the same ground for the same chest. Until now the only thing waiting there was the chest. Now the
/// previous captain is waiting there too, face down, with the license still on him — and the field book
/// files the page under HIS name, so the THREADS view (#741) stacks a life under the man who lived it.</para>
///
/// <h3>Two sources, in the owner's own order</h3>
/// <list type="number">
/// <item><b>Your lineage.</b> The save's own dead captains (<see cref="RetiredCaptain.Grave"/>), which is
/// material no other universe has and no generator could invent. Every word of the note is composed out of
/// the record: the name the roster kept, the roster's own <see cref="CaptainSuccession.RetiredLine"/>, and
/// the death card's own <see cref="DeathNarration.CauseWord"/>. <b>Nothing here restates any of them</b> —
/// it calls them, so the day either is rewritten, the field book's page moves with it.</item>
/// <item><b>Strangers, where the lineage has none.</b> A named person the game has already printed —
/// never a generated nobody, which is the half of the ruling that is easy to miss and would have been the
/// cheap way to build this. The <see cref="Cast"/> below is that list, and it is short on purpose.</item>
/// </list>
///
/// <h3>The writing is procedural, which is not the same as generated</h3>
/// <para>The two sentences are authored (Fable, 2026-09-13) and are the only sentences this feature has.
/// What is procedural is everything inside them: which captain, which day, which cause, which stranger —
/// so no two universes read the same page, and no line of prose was written by a machine.</para>
///
/// <para>Pure and deterministic, like everything else in Core: same ground, same stranger, for ever.</para>
/// </summary>
public static class LineageMark
{
    // ── THE PLATE ────────────────────────────────────────────────────────────────────────────────────
    //
    // A LABEL, not prose, and deliberately the least informative label that is still true. It does not say
    // "YOUR PREDECESSOR" and it does not say "A STRANGER", because the captain has not looked yet — the
    // whole beat is walking over to find out which it is. Inference horror at its cheapest and most honest:
    // the game announces the fact of a mark and nothing about its meaning.

    /// <summary>What the mark is labelled on the ground, before anybody reads it.</summary>
    public const string Plate = "SOMEBODY WAS HERE";

    /// <summary>The book's glyph for a page about one of your OWN — the roster's own gravestone, the same
    /// one the saved-voyages rack puts in front of <see cref="CaptainSuccession.RetiredLine"/>. One glyph
    /// for one meaning across the whole game.</summary>
    public const string LineageGlyph = "🪦";

    /// <summary>The book's glyph for a page about somebody else's mark — the forensics' own, the one the
    /// husks' underfoot line already speaks in.</summary>
    public const string StrangerGlyph = "☠";

    // ── THE TWO SENTENCES ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>The page a captain files over one of his own.</b> Authored (Fable, 2026-09-13); every placeholder
    /// is filled from the record and none of it is restated here.
    ///
    /// <para><paramref name="retired"/> supplies both the name and — through
    /// <see cref="CaptainSuccession.RetiredLine"/>, CALLED and never transcribed — the roster's own line
    /// about him. The cause word is <see cref="DeathNarration.CauseWord"/>'s, which is the death card's own
    /// clause with its plate stripped off. So this method authors a shape and quotes three facts, and a
    /// guard can assert every one of them by calling the same three things.</para>
    /// </summary>
    public static string YoursNote(RetiredCaptain retired)
    {
        ArgumentNullException.ThrowIfNull(retired);
        DeathCause cause = retired.Grave?.Cause ?? DeathCause.Reevers;
        return "A suit in the regolith with the license still clipped to it. "
             + $"{retired.Name}. {CaptainSuccession.RetiredLine(retired)} — and after that, here. "
             + $"{DeathNarration.CauseWord(cause)}.";
    }

    /// <summary><b>The page a captain files over somebody else's.</b> Authored (Fable, 2026-09-13). The one
    /// procedural part is WHO, and who is always somebody this game has already named.</summary>
    public static string StrangerNote(string who)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(who);
        return $"Not one of yours. The tag on it reads {who}, and the tag is older than the dust.";
    }

    // ── THE CAST ─────────────────────────────────────────────────────────────────────────────────────
    //
    // "Preferably NPCs we know something about" is the owner's own qualifier, and it is the whole design: a
    // tag reading a name you have heard at a bar is a thread; a tag reading a name a generator made up is
    // set dressing that pretends to be a thread. So the pool is the game's NAMED cast, read from the files
    // that print those names, and it is filtered by two rules that are both about not lying:
    //
    //   • NOT THE DEAD. Hollis Grey (OldCrew, Living: false) is a face in a photograph and has been for
    //     years; a tag of his on a moon would be the game inventing an event in its own past.
    //   • NOT A MACHINE. B-7V is bolted to the galley counter and sold as one lot with it (#1022). Nothing
    //     about him could be out here, and the joke would cost the character.
    //
    // And one exclusion that is pure canon rather than a rule: HARLAN FESS IS NOT IN THIS LIST. Kolt's own
    // file says the company sends Kolt "where Harlan Fess will not go" — so a tag reading Fess on a moon
    // would contradict, in one word, the reason the other salesman exists.
    //
    // Every entry is READ from the type that owns the name (there is no second spelling of anybody in
    // here), and the living filter is READ from OldCrew's own flag rather than a hand-kept copy of it.

    /// <summary>
    /// The people whose name a tag out here may honestly read: named, alive, and plausibly capable of
    /// having walked an airless ground. Stable order, because the draw is an index into it and a reordering
    /// would silently re-point every mark in every save.
    /// </summary>
    public static IReadOnlyList<string> Cast { get; } =
    [
        // The hazardous-accounts man, and the only member of this cast the game has ever actually put on
        // regolith: HardcaseRep.GroundLikeThis gates his whole beat to floor 0 on a moon (#1043).
        HardcaseRep.RepName,

        // The finder who works the cases a badge cannot (#1136 / FinderCase) — off the books by
        // construction, which is most of the argument for her being out here.
        FinderCase.DisplayName,

        // The station oracle, who wintered a season on a cold-storage barge in an ice moon's shadow
        // (OracleRant) — she has been further out in the cold than anybody else on this list.
        OracleRant.FullName,

        // …and the old crew (#973 L5), the six who are still alive. They crewed a survey tender on the
        // KAAMOS supply chain together, which is precisely a job that puts people on grounds like this one.
        .. OldCrew.Pool.Where(s => s.Living).Select(s => s.Name),
    ];

    /// <summary>
    /// <b>WHOSE TAG THIS GROUND CARRIES.</b> Seeded off the ground itself — the body and the landing site,
    /// the same two halves every <see cref="GroundMemory"/> key is built from — so one site always answers
    /// the same name, two visits agree, and a save reloaded ten times is not ten different strangers.
    ///
    /// <para>Deterministic through <see cref="DiceRule.Seed(string, long[])"/>, per the Core law. No clock,
    /// no RNG, and nothing here rolls anything the world had not already settled: the MARK was placed by
    /// the watchdog economy days ago (<see cref="RivalVisit"/>); this only reads a name onto it.</para>
    /// </summary>
    public static string StrangerFor(string bodyId, string siteSalt)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        ulong seed = DiceRule.Seed($"lineage-stranger:{bodyId}:{siteSalt}");
        return Cast[(int)(seed % (ulong)Cast.Count)];
    }

    // ── THE MARK ITSELF ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The mark one grave leaves on its ground: a <see cref="GroundMemory.ScarKind.Suit"/> where he fell,
    /// stamped with the START OF THE DAY HE DIED — the same convention <see cref="RivalVisit.MomentOf"/>
    /// keeps, so the three age bands (<see cref="GroundMemory.AgeLine(double, double)"/>) read the real
    /// elapsed time rather than "now".
    ///
    /// <para>DERIVED, NEVER STORED. The roster's grave is the record; writing a scar row beside it would be
    /// two answers to one question, in the save file, for ever.</para>
    /// </summary>
    public static GroundMemory.Scar MarkFor(RetiredCaptain retired)
    {
        ArgumentNullException.ThrowIfNull(retired);
        CaptainGrave grave = retired.Grave
            ?? throw new ArgumentException("A captain with no grave has no mark.", nameof(retired));
        return new GroundMemory.Scar(GroundMemory.ScarKind.Suit, grave.X, grave.Y, WhenFell(retired));
    }

    /// <summary>The sim moment a mark is stamped with, for a captain the insurance wrote off on day N: the
    /// START of that day, in seconds — <see cref="GroundMemory.DaySeconds"/>, the same unit the age bands
    /// are measured in and the same convention <see cref="RivalVisit.MomentOf"/> keeps.
    ///
    /// <para>The day is the finest resolution the roster has ever kept (<see cref="RetiredCaptain.SimDay"/>
    /// is an int), and inventing a more precise moment here would be a number the world cannot back. A
    /// captain who reads the mark the same afternoon gets "Still smoking", which is true.</para></summary>
    public static double WhenFell(RetiredCaptain retired)
    {
        ArgumentNullException.ThrowIfNull(retired);
        return Math.Max(0, retired.SimDay) * GroundMemory.DaySeconds;
    }

    /// <summary>Every grave in this roster that is on THIS ground, oldest captain first (the order the
    /// roster keeps them in). The one query an excursion asks of the lineage.</summary>
    public static IReadOnlyList<RetiredCaptain> BuriedOn(
        IReadOnlyList<RetiredCaptain>? retired, string bodyId, string siteSalt)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        var found = new List<RetiredCaptain>();
        foreach (RetiredCaptain one in retired ?? [])
        {
            if (one.Grave is { } g && g.IsOn(bodyId, siteSalt))
            {
                found.Add(one);
            }
        }
        return found;
    }

    /// <summary>The <c>AllProse</c> discipline every prose-bearing type in Core keeps: everything this file
    /// can ever say, for the sweeps (§8's reserved word, the house voice). The two sentences are shown with
    /// stand-in fills because their real fills come out of a save.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Plate;

        // Every cause that can leave a mark, because the cause word is the one part of that sentence the
        // sweep has not already read somewhere else — a pool walked to one entry is a pool half-swept.
        foreach (DeathCause cause in Enum.GetValues<DeathCause>())
        {
            if (!CaptainGrave.CanRecord(cause))
            {
                continue;
            }
            yield return YoursNote(new RetiredCaptain("Captain Mabel Vane", 42)
            {
                Grave = new CaptainGrave("phobos", "site-0", 0, 0, cause),
            });
        }

        foreach (string who in Cast)
        {
            yield return StrangerNote(who);
        }
    }
}
