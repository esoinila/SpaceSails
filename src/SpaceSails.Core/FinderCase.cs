using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #417 slice 1 · <b>THE FINDER'S CASE</b> — one whole case, seeded, pure, and built out of things the world
/// was already dealing.
///
/// <para><b>Owner, 2026-07-20:</b> <i>"We can always add private detective type missions then… just not call
/// our detective Miller 🤭"</i>. So she is not Miller. She is <b>Ilse Varga</b>, ex-harbour police at Selene
/// Gate, freelance finder, a coat worse than Fess's and a drinking excuse always ready — Fable's canon pass
/// of 2026-09-05, and every sentence below is lifted from it character for character.</para>
///
/// <h3>The graph, and the one law that shapes it</h3>
///
/// <para><b>IT INVENTS NOTHING.</b> A client port, a witness, a site, two hulls and a confrontation berth —
/// and every one of them is handed IN, off the world the game is already running. <see cref="Build"/> takes
/// lists and returns null when the world it was handed cannot furnish a case, which is the honest answer and
/// the reason there is no fallback arm anywhere in this file: a case that invented a port to finish itself
/// would be a detective story about a place the captain can never fly to.</para>
///
/// <list type="bullet">
/// <item><b>The client</b> — Varga, at a bar table at a port the world publishes. Her hook names that port,
/// and it is the only place the hook's <c>{PORT}</c> is ever filled from.</item>
/// <item><b>Lead one, a witness</b> — one of the bar's own roving regulars (<see cref="PatronRota"/>), at the
/// port that rota actually favours them at. #414's rhythm, asked rather than re-invented. <b>Slice 2a:</b>
/// he is the one lead that has to be LOOSENED — he gives up what he saw for a glass he may refuse, through
/// the bar's own offer (<see cref="WhatTheGlassDoes"/>, <c>FinderCase.TheWitness.cs</c>).</item>
/// <item><b>Lead two, a paper</b> — a find on a real body's ground, clipped into the field book under this
/// case's subjects (#1052/#934).</item>
/// <item><b>Lead three, a hull under a former name</b> — an NPC hull out of the traffic, read off her own
/// <see cref="ShipHistory"/> ledger of names (#397).</item>
/// <item><b>The red herring</b> — a SECOND hull answering to the same former name, whose chain of custody
/// (#426) clears her: her papers are older than the story.</item>
/// <item><b>The confrontation</b> — a real slot on a real port's roster (#1092/<see cref="DockRoster"/>): the
/// berth next to the one that port has always given him.</item>
/// </list>
///
/// <h3>The laws the canon pass set, and where each is kept</h3>
///
/// <list type="bullet">
/// <item><b>The case never names the Authority or the watchers</b>, and <b>the man's crime is never
/// stated</b> — a finder finds, she does not judge. Both are properties of the eleven sentences below and
/// are swept by this feature's guard.</item>
/// <item><b>Varga recurs at most once per port.</b> Kept by the room (a visit fold, exactly as the salesman's
/// and the walk-in's are), because it is a fact about a berth and not about a case.</item>
/// <item><b>The reserved word</b> of <c>docs/worldbuilding-notes.md</c> §8 is absent.</item>
/// </list>
///
/// <para>Pure and deterministic like everything else in Core: the same thread and the same world deal the
/// same case, on every machine and across a reload.</para>
/// </summary>
public static partial class FinderCase
{
    // ── WHO SHE IS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Where her book is kept in the contacts ledger. A person's row, not an outfit's — she is
    /// somebody you drank with, and the reputation this case pays lands here as goodwill.</summary>
    public const string ContactId = "finder-ilse-varga";

    /// <summary>Her name, as it goes in the book from the moment she says it.</summary>
    public const string DisplayName = "Ilse Varga";

    /// <summary>The plate the deck draws over her while she crosses the floor. Composed exactly as the
    /// salesman's and the walk-in's are (<see cref="WalkIn.Plate"/>) — a plate is what the room calls
    /// somebody, and the room does not know she is a case.</summary>
    public static string Plate => "◈ " + DisplayName.ToUpperInvariant();

    // ── WHAT SHE SAYS. Fable's canon pass, 2026-09-05, verbatim. ────────────────────────────────────────

    /// <summary>Her approach, said at a bar table before the captain can decide anything.</summary>
    public const string Approach =
        "Varga. I used to wear a badge at Selene Gate; now I find things for people who can't ask the badge. "
        + "You have a ship and no reputation to lose. Sit.";

    /// <summary>The hook, with the client port's own name in it. The brace is filled from
    /// <see cref="Case.ClientPortName"/> and from nowhere else.</summary>
    private const string HookLine =
        "A man walked off a hull at {0} and never walked into the concourse. The hull has had three names. "
        + "Find the fourth.";

    /// <summary>The hook as she says it here, at this port.</summary>
    public static string Hook(string portName) =>
        string.Format(CultureInfo.InvariantCulture, HookLine, portName);

    /// <summary>What she says the next time she is met, once the case is settled and paid.</summary>
    public const string Payoff = "Paid. Don't thank me — the next one is worse, and you'll take it.";

    // ── THE CASE'S OWN LINES ────────────────────────────────────────────────────────────────────────────

    /// <summary>The head of the lead card, and the head of the entry it leaves in the field book.</summary>
    public const string LeadTitle = "A finder's case";

    /// <summary>…and the body of it. It states the shape of the case and refuses to state what it means,
    /// which is the field book's own frame law (#587).</summary>
    public const string LeadBody = "Three names on one hull, and a man who is none of them.";

    /// <summary>The red herring, at the moment her chain of custody clears her. A pulse: nothing is decided
    /// here, a door is closed.</summary>
    public const string HerringCleared = "Her papers are older than the story. Not this one.";

    /// <summary>The reveal, at the confrontation berth. The one plot-significant telling in this case, and
    /// the card is the telling (#761).</summary>
    public const string Reveal =
        "The fourth name is on the transponder in the next berth. He is aboard. He knows you are here.";

    /// <summary>The first verb.</summary>
    public const string TurnHimIn = "Turn him in";

    /// <summary>…and the second. Two, because there is nothing to bargain about.</summary>
    public const string TakeTheBribe = "Take the bribe";

    /// <summary>What happens when he is turned in.</summary>
    public const string AfterTurningIn = "Selene Gate sends a boat. Varga does not come to see it.";

    /// <summary>…and when the bribe is taken.</summary>
    public const string AfterTheBribe =
        "The account clears before he does. Varga will hear; she always does.";

    // ── #417 SLICE 2a · THE WITNESS'S OWN THREE ─────────────────────────────────────────────────────────
    //
    // Fable's canon pass for this slice, verbatim, and the whole of what the man says. He is the one lead in
    // this case that is a PERSON rather than a place or a paper, and slice 1 let him hand the trail over the
    // moment the captain walked up to him — which made him a switch with a face painted on it. These three
    // sentences are the difference: he says the first to anybody who comes asking, and the other two are his
    // answer to a glass, which is the only currency he takes.
    //
    // They are APPENDED to AllProse rather than filed beside the case's own lines, because AllProse is read
    // POSITIONALLY by `EveryLineIsTheCanonPassVerbatim` against a copy retyped from the issue — so the
    // eleven slice 1 shipped keep the indices they were checked at, and a later slice's lines land after
    // them where that suite's own copy grows the same way.

    /// <summary>What he says when the captain reaches him with the case open and nothing in his hand. Said
    /// once a watch, because a man repeating one sentence at every press is furniture with a speaker in
    /// it.</summary>
    public const string WitnessBeforeTheGlass = "I work the rota. I don't work for you.";

    /// <summary>…and when he waves the offered glass off. Nothing files and nothing is owed: the captain can
    /// come back on a later watch and ask again.</summary>
    public const string WitnessStaysOnShift = "Keep it. I'm on shift.";

    /// <summary>…and when he takes it, in the breath before the lead goes into the book.</summary>
    public const string WitnessTakesTheGlass = "One drink. Then I never saw you.";

    /// <summary>Every player-facing sentence this case can put on a screen — the ten the canon pass authored
    /// plus the hook's template (a sentence with a port's name in the middle of it), and slice 2a's three
    /// for the witness. The same <c>AllProse</c> discipline every prose-bearing type in Core keeps, and the
    /// list the reserved-word sweep and the no-twelfth-string sweep both walk.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Approach;
        yield return HookLine;
        yield return Payoff;
        yield return LeadTitle;
        yield return LeadBody;
        yield return HerringCleared;
        yield return Reveal;
        yield return TurnHimIn;
        yield return TakeTheBribe;
        yield return AfterTurningIn;
        yield return AfterTheBribe;
        yield return WitnessBeforeTheGlass;
        yield return WitnessStaysOnShift;
        yield return WitnessTakesTheGlass;
    }

    // ── WHAT THE WORLD HANDS IN ─────────────────────────────────────────────────────────────────────────

    /// <summary>One hull the traffic is actually flying, as this file needs to read her: her stable id, the
    /// name she answers to now, and her own service record. Handed in rather than looked up, for
    /// <see cref="OldCrew.Berth"/>'s reason — a test builds a world by hand, and a guard handed a world it
    /// invented itself cannot tell pass from fail.</summary>
    public readonly record struct Hull(string Id, string Callsign, ShipHistory History);

    /// <summary>One ground a paper can be found on: the body's id and the name the book will file the find
    /// under. Both are the world's own; nothing here composes either.</summary>
    public readonly record struct Site(string BodyId, string Name);
}
