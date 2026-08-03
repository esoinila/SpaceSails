namespace SpaceSails.Core;

/// <summary>
/// #562 · THE TUBE FEEDS YOU — the card that goes up the first time the ship racks a sentry magazine while
/// the captain stands in her down-tube.
///
/// <para>Owner, playtesting Miranda with both sentries shouldered and dry: <i>"The gun reload at airlock is
/// not working here now… I carry both guns but they are not being reloaded."</i> Then: <i>"I expect them to
/// be reloaded at that tube I was at… maybe some progress bar for each reload but still. We need to
/// communicate the reload to user also… maybe a gen AI image about it the first time."</i></para>
///
/// <para>The card's real job is not to announce a feature. It is to teach the <b>shape of an excursion</b>,
/// because putting the reload at a PLACE is what gives the surface a shape at all — owner: <i>"the reload
/// forces the player to plan their routes … and keep their supply line safe for retreat to reload"</i>, and
/// the tube is therefore <i>"the invisible tether to players distance."</i> A captain who understands that
/// plans a loop. One who does not walks until something kills them.</para>
///
/// <para>Once per captain, then the receipt line takes over — you know where the ammo comes from now.</para>
/// </summary>
public static class TubeRearm
{
    /// <summary>The stamp across the top of the card.</summary>
    public const string Stamp = "🔫 THE TUBE FEEDS YOU";

    /// <summary>The line under the stamp — what just happened, in one breath.</summary>
    public const string Head =
        "Stand in her tube with a shouldered sentry and the ship racks it. One magazine at a time, billed to the purse.";

    /// <summary>The art that rides the card. Missing art degrades to no image, never a broken frame.</summary>
    public const string ArtFile = "art/tube-rearm.jpg";

    /// <summary>The three things this teaches, in the order they change how you play.</summary>
    public static IReadOnlyList<string> Beats { get; } =
    [
        "It happens on its own — no key, no panel. Walk in dry, wait while the bar fills, walk out fed. "
        + "Step out early and nothing is lost; the rounds already racked are already in the magazine.",

        // #580/#563 · "the only place your sentries get fed" was true in #562 and stopped being true the
        // moment the ground grew things to find in: a shelter's emergency press reloads magazines in full,
        // every time you ask (SurfaceShelter.LockerRounds), and an outpost hut's ammunition locker spreads
        // its rounds across the bots you brought. The anchor is still the anchor — what makes the tube
        // special is that it is the one that is ALWAYS there and comes to you. The rest you have to find.
        "The rounds are cheap on purpose. What going deep really costs you is the WALK BACK — this tube is "
        + "the one supply point that is always where you left it, so every excursion is a loop with one "
        + "anchor. There is other ammunition out on the ground, in shelters and in other people's lockers, "
        + "but you have to find it first and carry it to the bot yourself.",

        "So plan the route out with the route home in it. The far end of a site is not where the ground "
        + "stops; it is where the magazine and whatever is following you say turn around.",
    ];

    /// <summary>The button that dismisses it — an acknowledgement, not an "OK".</summary>
    public const string Dismiss = "Understood. Keep the road home open.";

    /// <summary>The line under the button: where the same service is bought when you are not on a surface.</summary>
    public const string Foot =
        "A haven's armory racks the whole roster at once, at the same price per round — the same law, another door.";
}
