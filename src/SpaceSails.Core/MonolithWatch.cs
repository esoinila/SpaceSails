using System;

namespace SpaceSails.Core;

/// <summary>
/// #649 · THE MONOLITH SITE IS A STRANGE-THINGS-HAPPEN PLACE.
///
/// <para>Owner's ruling, 2026-08-03 (<c>docs/worldbuilding-notes.md</c> §8), in his own reference:
/// <b>Babylon 5</b> — Sheridan and the giants on the playground; <i>"background puppeteers watching if their
/// kids perform in the school play."</i> And: <i>"There is more to this world than our human-AI interludes.
/// Something awesome and a little scary in a Lovecraftian way."</i></para>
///
/// <para><b>The register, stated so it can be held to.</b> Not a jump scare and not a monster: the dread of
/// SCALE and of ATTENTION. You are being watched by something that has a stake in you and will not say so.
/// It is <b>parental, not predatory</b> — a parent at the back of a school hall is not a threat, and is
/// nonetheless the reason your hands shake. Nothing here is ever hostile, nothing here ever costs the
/// captain anything, and nothing here is ever confirmed to have happened at all.</para>
///
/// <para><b>What this is NOT, and the discipline is the whole feature:</b></para>
/// <list type="bullet">
/// <item>Not a creature. There is no entity in this file — no position, no state, no thing that could be
/// shot at, fled from, or drawn on a tracker as itself. If a captain could ever point at it, the game has
/// answered the question and the answer is always smaller than the question.</item>
/// <item>Not an announcement. The world BEHAVES for one beat as though vaster things are present and mildly
/// attentive, and then it does not, and nobody remarks on it. The standing house law
/// (<c>QAHandoff-StoryArcs.md</c>): <i>"SECURITY ALERTED as a banner is the wrong shape; a lift that stops
/// answering is the right one."</i></item>
/// <item>Not explained, ever. Same canon law as the Old Ones' origin and the monolith's own card: no card
/// states what it is, no sensor returns a reading that settles it, no NPC knows. The inference is the
/// horror, and confirming it kills it.</item>
/// <item>Not a card and not a plate. This is deliberate and it is the hardest call in the feature — see
/// <see cref="Line"/>. A picture appearing is the game turning to face you and saying THIS HAPPENED.</item>
/// </list>
///
/// <para><b>The cadence law</b>, in the shape <see cref="StoryBeats"/> already uses (<i>"a card that fires
/// whenever its condition is true is how a game teaches players to dismiss cards without reading them"</i>),
/// with three gates that must all open:</para>
/// <list type="number">
/// <item><b>Place.</b> Only where the monolith stands — <see cref="Monolith.StandsOn"/>, the one predicate
/// everything asks, and only while the captain is inside its sight (<see cref="Monolith.SightRangeDu"/>).
/// This ground and no other; that is what makes it a property of the PLACE.</item>
/// <item><b>Window.</b> Roughly one visit-window in three is ATTENTIVE, seeded on the same slow clock the
/// foot-offerings use (<see cref="Monolith.EpochAt"/>). So a captain who walks out, walks back and returns
/// the same afternoon gets the same answer — the owner's object-persistence law — and most walks out here
/// are simply a long walk to a stone, which is what makes the other ones mean anything.</item>
/// <item><b>Dwell.</b> You have to STAY. <see cref="DwellSeconds"/> inside its sight before anything
/// happens, and at most once per excursion. Nothing is watching to see you arrive; it is watching to see
/// whether you stand there, which is the school play.</item>
/// </list>
/// </summary>
public static class MonolithWatch
{
    /// <summary>How long the captain must stand inside the monolith's sight before the ground does anything.
    /// Long enough that it can never be mistaken for a reaction to arriving, short enough that a captain who
    /// stopped to read the card and look at the thing has already earned it.</summary>
    public const double DwellSeconds = 40.0;

    /// <summary>One window in this many is attentive. Three is the number that makes the walk worth making
    /// twice and never worth making for this — most excursions out here are a long walk to a stone.</summary>
    public const int OneWindowIn = 3;

    /// <summary>
    /// WHAT IT COSTS: NOTHING.
    ///
    /// <para>A feel call, flagged for the owner. The nerve model already prices this place at
    /// <see cref="NerveModel.MonolithSightShock"/> — 24, the single biggest fright in a captain's life,
    /// once ever — and the ruling says the <i>place</i> should earn that number rather than the number
    /// carrying the place alone. Earning it does not mean charging again.</para>
    ///
    /// <para>The register is <b>awe, not damage</b>. Anything that takes nerve is something the captain
    /// learns to avoid, and a site you learn to avoid is a predator whatever the prose says. The world
    /// noticing you and then not hurting you is the more unsettling of the two readings, and it is the
    /// parental one: they are watching the play, not hunting the children. If the owner wants a price, the
    /// natural one is a sub-pip prickle of 2.0 — the same the foot-offerings charge — and it is one number
    /// in one place.</para>
    /// </summary>
    public static double NerveCost => 0.0;

    /// <summary>Is this a ground where it can happen at all? The monolith's own, and nowhere else in the
    /// system. One predicate, asked of the object, exactly like everything else about this place.</summary>
    public static bool CanHappenOn(string? bodyId, string? siteSalt) => Monolith.StandsOn(bodyId, siteSalt);

    /// <summary>Is this visit-window an attentive one? Deterministic per (body, site, window), off the shared
    /// dice engine — never <see cref="Random"/> and never the clock — so the answer holds still for a whole
    /// excursion and is the same on a revisit inside the window.</summary>
    public static bool AttentiveIn(string bodyId, string siteSalt, long epoch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        return DiceRule.Roll(DiceRule.Seed($"monolith:watch:{bodyId}:{siteSalt}:{epoch}"), OneWindowIn).Face == 1;
    }

    /// <summary>What the ground does. Every one of these is a fact about the WORLD — a shadow, a print, a
    /// tide, a count — and not one of them is a thing that could be met.</summary>
    public enum What
    {
        /// <summary>Every shadow on the field lies one way. The slab's lies another.</summary>
        TheShadowsDisagree,

        /// <summary>Your own bootprints, ahead of you, going the way you are going.</summary>
        YourOwnPrintsAhead,

        /// <summary>Every Old One on the ground stops at once and faces the stone.</summary>
        ThePackGoesStill,

        /// <summary>A tide in the dust, on a moon with no water, no air and nothing to pull it.</summary>
        ATideInTheDust,

        /// <summary>The tracker paints one contact more than there is anything to paint.</summary>
        OneContactTooMany,

        /// <summary>The light drops a third, evenly, everywhere, for the length of one breath.</summary>
        TheLightDips,
    }

    /// <summary>Which one this window holds. Deterministic on the same key as
    /// <see cref="AttentiveIn"/>.
    ///
    /// <para><paramref name="packOnTheField"/> is asked rather than assumed: <see cref="What.ThePackGoesStill"/>
    /// is about the Old Ones DOING something, and on an empty field there is nothing to do it. A beat that
    /// narrates a pack that is not there is the sim doing one thing while the sentence says another — the
    /// third named bug class on this project, and the one it keeps paying for. So the roll steps past it.</para></summary>
    public static What Which(string bodyId, string siteSalt, long epoch, bool packOnTheField)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        var all = Enum.GetValues<What>();
        int at = DiceRule.Roll(DiceRule.Seed($"monolith:watch:what:{bodyId}:{siteSalt}:{epoch}"), all.Length).Face - 1;
        for (int step = 0; step < all.Length; step++)
        {
            What candidate = all[(at + step) % all.Length];
            if (candidate != What.ThePackGoesStill || packOnTheField)
            {
                return candidate;
            }
        }

        return What.TheShadowsDisagree;
    }

    /// <summary>
    /// What the captain is told, which is only ever WHAT THE WORLD DID.
    ///
    /// <para><b>Why these are lines and not a card with a picture.</b> Every other big moment in this game
    /// gets a canvas, and that is the owner's own law (#528). This one must not, and the reason is the
    /// ruling that created it: <i>"anything that happens near it is reported by the world in a way that
    /// stays deniable."</i> A plate is a frame, and a frame around a thing says THIS IS A THING. Worse, one
    /// canvas across six variants becomes the picture a player learns to read as <i>the watchers again</i> —
    /// confirmation by repetition, which is the same failure as a card that explains, arriving by a slower
    /// road. The nearest thing this ground already has — what somebody left at the foot of the stone, the
    /// beat the owner asked for by name — is text, and it works.</para>
    ///
    /// <para>Every line obeys three rules, and a guard greps for all three: nothing is named, nothing is
    /// accounted for, and nothing resolves. The last one is the hardest to write and is why each of these
    /// ends on the captain's own arithmetic rather than on an event.</para>
    /// </summary>
    public static string Line(What what) => what switch
    {
        What.TheShadowsDisagree =>
            "👁 Your shadow, the marker stubs' shadows, the drift streaks in the dust: every one of them " +
            "lies the same way. The slab's does not. You look up, and there is one sun, where it has been " +
            "all afternoon. When you look down again they agree, and you could not say which of them moved.",

        What.YourOwnPrintsAhead =>
            "👣 There is a line of bootprints ahead of you, running on toward the stone. Six, and then the " +
            "swept ground takes them. They are your tread, your stride and your weight, the dust in them is " +
            "not settled yet, and you have not been that way.",

        What.ThePackGoesStill =>
            "🦴 Every Old One on this field has stopped. Not crouched, not casting about — stopped, upright, " +
            "square on to the stone, all of them, on the same second. Whatever this is, you are not in it. " +
            "It lasts about as long as it takes you to decide not to move either.",

        What.ATideInTheDust =>
            "🌊 The dust goes out from the foot of it and comes back, once, the whole width of the swept " +
            "ground — a slow swell of regolith with no water under it, no air over it and nothing near " +
            "enough to pull it. It passes under your boots and you feel nothing at all through the soles.",

        What.OneContactTooMany =>
            "📡 The tracker paints one contact more than there are things on this field. It does not close, " +
            "it does not drift, and it is not standing where anything is standing. The next sweep has the " +
            "right count, and so does the one after that, and so, the log says, did the one before.",

        _ =>
            "🕯 The light goes down by about a third — evenly, everywhere, the shadows staying exactly where " +
            "they were — for as long as it takes to breathe out once. Nothing crossed the sun. You were " +
            "looking straight at it.",
    };

    /// <summary>The glyph the field note files under. Not a name for anything: an eye, because looking is
    /// the only verb in the whole feature.</summary>
    public const string Glyph = "👁";
}
