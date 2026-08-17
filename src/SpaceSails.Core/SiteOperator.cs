using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #760 · WHO RUNS THIS SITE. Owner's idea, 2026-08-08, streamed while the #684 ruling landed:
///
/// <para><i>"same-company labs on different sites may accept the same cards for access … An authority card
/// is standing with an operator, not a key to one door."</i></para>
///
/// <para>The estates printed on the cards have been company-shaped since #679 — PROCUREMENT · SCHEDULE C,
/// INSPECTORATE · NO STANDING — and the world did not agree with them: a card was a key to one hole under
/// one moon, and a captain who had worked two sites of the same outfit was treated as a stranger at the
/// second one. This is the missing half: <b>a site has an operator</b>, and standing is with THEM.</para>
///
/// <h3>Three calls, each overrulable in one line</h3>
/// <list type="number">
/// <item><b>An operator is a fact about the site, not about the visit.</b> Seeded off the body id like the
/// depth, the kind, the unlisted band and the halls — so the moon has the outfit it has, and two captains
/// on two saves are talking about the same company.</item>
/// <item><b>The head office's operator is never rolled.</b> There is one of it, it is the parent of nothing
/// it will name, and — the canon call — <b>it publishes no network</b>. The remote hears nothing there
/// (#649/#672: the watchers emit nothing, and neither does whoever files them).</item>
/// <item><b>The list is short and bland.</b> Four outfits, in the register every other thing this office
/// stamps is written in: a company name explains nothing. None of them says what the buildings were for
/// (§13.8), and none of them is the arc's own name.</item>
/// </list>
/// </summary>
public static class SiteOperator
{
    /// <summary>One outfit.</summary>
    /// <param name="Id">The durable key. It rides in a card id (<c>UndergroundComplex.AuthorityCard</c>) and
    /// in nothing else, so it is short, lower-case and free of the separators a card id is split on.</param>
    /// <param name="Name">What is on the letterhead of the company rather than of the office — the heading
    /// the satchel groups a wallet under.</param>
    /// <param name="PublishesNetwork">Whether this outfit answers a radio at all. The one place the remote
    /// asks before it sends (#760's SEND STANDING).</param>
    public readonly record struct Operator(string Id, string Name, bool PublishesNetwork);

    /// <summary>The four outfits a rolled site can answer to, in the order the roll indexes them. Order is
    /// part of the save-compatible identity of a site: changing it re-assigns every moon in the game.</summary>
    private static readonly Operator[] TheOperators =
    [
        new("meridian", "MERIDIAN WORKS COMPANY", PublishesNetwork: true),
        new("northfield", "NORTHFIELD SURVEY TRUST", PublishesNetwork: true),
        new("argent", "ARGENT PROVISIONING LTD", PublishesNetwork: true),
        new("holbein", "HOLBEIN & SONS (MINERALS)", PublishesNetwork: true),
    ];

    /// <summary>#411/#760 · The head office's own, and the only one that is not a roll.
    ///
    /// <para>It publishes NO NETWORK, and that absence is the same rank difference the absent gate is: every
    /// branch office answers a radio because a branch office has to be told things. This one is where the
    /// telling comes from, and it does not take messages. The name is deliberately the blandest thing in the
    /// file — it names a rank in a company structure and nothing else, because a name that named the arc
    /// would hand a captain the one thing the whole Hive is arranged around.</para></summary>
    public static readonly Operator TheParentUndertaking =
        new("parent", "THE PARENT UNDERTAKING", PublishesNetwork: false);

    /// <summary>Every outfit there is, for an audit that has to walk them all. Nothing in the game iterates
    /// this — a site gets exactly one, from <see cref="Of"/>.</summary>
    public static IReadOnlyList<Operator> All => [.. TheOperators, TheParentUndertaking];

    /// <summary>WHO RUNS THE SITE UNDER THIS BODY. The single roll — the card's standing, the satchel's
    /// heading and the remote's send all read its answer rather than rolling again.</summary>
    public static Operator Of(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // #411 · Never rolled, for the reason recorded on the type: an operator a seed could produce twice
        // would not be the parent of anything.
        if (UndergroundComplex.IsHeadOffice(bodyId))
        {
            return TheParentUndertaking;
        }

        return TheOperators[(int)(DiceRule.Seed($"hive:operator:{bodyId}") % (ulong)TheOperators.Length)];
    }

    /// <summary>An outfit by its key, for reading a standing back off a card id. Anything this build does not
    /// know is not invented — the caller decides what an unreadable standing means, and every one of them
    /// decides the same thing: it is not ours.</summary>
    public static Operator? ById(string? operatorId)
    {
        if (operatorId is null)
        {
            return null;
        }
        foreach (Operator op in All)
        {
            if (string.Equals(op.Id, operatorId, StringComparison.Ordinal))
            {
                return op;
            }
        }
        return null;
    }

    // ── #760 · THE POCKET DOES NOT FILL YET ─────────────────────────────────────────────────────────────
    //
    // The issue asks for a folder tree — company as the folder, per-site grants beneath — "if the pocket
    // fills". It has not. A tree over four cards is a filing cabinet built for a wallet, so v1 is the
    // smallest true version of the same idea: the wallet already folds (#697), and inside the fold the cards
    // sit under the name of whoever gave them to you, WHEN THERE ARE TWO OR MORE NAMES.
    //
    // One operator gets no heading, and that is not a saving — a heading over a list that is entirely one
    // thing is a label that cannot be read for information, which is the same sin as #697's folder of one.

    /// <summary>#760 · A heading and the cards under it.</summary>
    /// <param name="Operator">Whose standing these are.</param>
    /// <param name="Heading">The line drawn over them.</param>
    /// <param name="Cards">The authorities themselves, in the order the captain found them.</param>
    public readonly record struct Folder(
        Operator Operator, string Heading, IReadOnlyList<Satchel.Item> Cards);

    /// <summary>The glyph a company heading wears — a building, because the thing being named is an outfit
    /// and not an office.</summary>
    public const string HeadingGlyph = "🏢";

    /// <summary>What a standing this build cannot place is grouped under. It never impersonates one of the
    /// outfits above, for the nameless card face's reason one layer down.</summary>
    public const string UnplaceableStandingName = "AN OUTFIT THIS REGISTER DOES NOT LIST";

    /// <summary>#760 · THE ACCESSES, GROUPED — or nothing at all.
    ///
    /// <para>Returns one folder per operator, in the order their first card turns up in the pocket, and
    /// returns an <b>empty list</b> where the whole wallet answers to one outfit. The empty answer is the
    /// feature: the client draws headings when there are headings worth drawing and its flat list otherwise,
    /// and neither it nor a test has to re-derive "is this worth grouping".</para>
    ///
    /// <para>Anything that is not an authority is not in the wallet and is ignored, so a caller may hand over
    /// the whole pocket.</para></summary>
    public static IReadOnlyList<Folder> Accesses(IReadOnlyList<Satchel.Item>? carried)
    {
        var order = new List<string>();
        var byOperator = new Dictionary<string, List<Satchel.Item>>(StringComparer.Ordinal);

        foreach (Satchel.Item item in carried ?? [])
        {
            if (item.Kind != Satchel.Kind.Authority)
            {
                continue;
            }

            // A card this build cannot even read — an edited save, or a later build's authority — is grouped
            // and NOT dropped. The pocket showing five cards flat and four cards grouped would be the dialog
            // eating a possession to tidy a heading, which is #678's law with a filing cabinet on top of it.
            string key = UndergroundComplex.AuthorityCard.TryParse(item.Id, out UndergroundComplex.AuthorityCard card)
                ? card.OperatorId
                : string.Empty;
            if (!byOperator.TryGetValue(key, out List<Satchel.Item>? cards))
            {
                byOperator[key] = cards = [];
                order.Add(key);
            }
            cards.Add(item);
        }

        if (order.Count < 2)
        {
            return [];
        }

        var folders = new List<Folder>(order.Count);
        foreach (string key in order)
        {
            // A standing naming an outfit this build has never heard of — an edited save, or a later build's
            // company. It is grouped under what it says about itself and NOT quietly filed under somebody
            // real, because a heading that puts a stranger's card under a company you have standing with is
            // the pocket lying about who owes you a door.
            Operator op = ById(key) ?? new Operator(key, UnplaceableStandingName, PublishesNetwork: false);
            folders.Add(new Folder(op, $"{HeadingGlyph} {op.Name}", byOperator[key]));
        }
        return folders;
    }
}
