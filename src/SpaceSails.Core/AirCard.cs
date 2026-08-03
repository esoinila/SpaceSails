namespace SpaceSails.Core;

/// <summary>
/// #573 · THE TANK IS GETTING LOW — the card that goes up the first time a captain's air passes the low
/// mark on a surface.
///
/// <para>Owner: <i>"So we need to communicate the air amount and launch a pop up with gen-ai image about the
/// air running out."</i> He had just died of a resource he could not see, with no warning he could act on,
/// and been told a heat-hunter shot him. The map growing gets a card; a scuttled ship gets a card; running
/// out of air — which ENDS THE RUN — got a line of dim text that scrolled past.</para>
///
/// <para>Once per captain, then the pulse line takes over. Like every other one-time beat here, its job is
/// not to announce a feature but to teach a rule the player will otherwise learn by dying: <b>the tank is
/// the clock on every walk, and the walk back is part of the walk.</b></para>
/// </summary>
public static class AirCard
{
    /// <summary>The stamp across the top.</summary>
    public const string Stamp = "🫁 THE TANK IS GETTING LOW";

    /// <summary>The line under it — what has happened, and what to do about it. Owner: <i>"the time to
    /// hurry warning to the airlock or refill"</i>. A card that only described the problem would be a
    /// prettier version of the silent timer.</summary>
    public const string Head =
        "Last third of the tank, and there is no air on this moon to help you. Start back for her tube now — "
        + "or find something out here that still holds a charge.";

    /// <summary>The art. Missing art degrades to no image, never a broken frame.</summary>
    public const string ArtFile = "art/air-running-out.jpg";

    /// <summary>The three things this teaches, in the order they change what a captain does next.</summary>
    public static IReadOnlyList<string> Beats { get; } =
    [
        "The gauge under the motion tracker has been counting the whole way out. It does not tell you how "
        + "full the tank is — that number is useless — it tells you how much FURTHER you can go and still "
        + "get home.",

        // #608/#580 · WHAT THIS BEAT USED TO SAY WAS TRUE WHEN IT WAS WRITTEN AND IS NOT NOW. It read
        // "nothing on the ground will sell you more. Her tube is the only place a suit refills" — while the
        // card's own Head, two fields up, already told the captain to "find something out here that still
        // holds a charge". Pressure refuges and survey shelters DO recharge a suit (SurfaceShelter.Transfer,
        // to two thirds and no further), and every airless floor is required to hold one. A card that
        // contradicts itself in the same breath teaches nobody anything.
        "The walk back costs the same air as the walk out, and nothing on this moon SELLS you any. Her tube "
        + "is the only thing that fills a tank to the top, which is why every excursion is a loop rather "
        + "than a line — but a shelter or a pressure refuge, if you can reach one, will put two thirds back "
        + "in and stop there. It leaves the rest for whoever comes next.",

        "When the walk home costs more than you are carrying, the suit says so ONCE, plainly. After that it "
        + "stops being a warning and starts being arithmetic.",
    ];

    /// <summary>The button — an acknowledgement, not an "OK".</summary>
    public const string Dismiss = "Noted. Turning for home shortly.";

    /// <summary>The line under the button: where the rule keeps being stated once the card is gone.</summary>
    public const string Foot =
        "The gauge sits under the motion tracker and changes colour as the margin goes: blue, amber, then red.";
}
