namespace SpaceSails.Core;

/// <summary>
/// #251 · HOW A BEAT BEHAVES — its cadence and cooldown, how it is presented and which card hosts it,
/// whether it waits out danger, the hold flag, and its sound cue.
///
/// <para>Split out of <c>StoryBeats.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Its two fields are <c>const</c>s.</para>
/// </summary>
public static partial class StoryBeats
{
    /// <summary>How often this beat may speak.</summary>
    public static Cadence CadenceOf(Beat beat) => beat switch
    {
        Beat.FirstShotFired => Cadence.OnceEver,
        Beat.CrewDeputation => Cadence.OnceEver,   // the FIRST deputation is the beat; later ones are the sheet's job
        Beat.SailHoled => Cadence.Cooled,
        Beat.ChargeLetGo => Cadence.Cooled,
        // #541: one gangway per berth. Each place gets its establishing shot exactly once.
        Beat.BerthGreatPort or Beat.BerthWorkingBerth or Beat.BerthOutpost => Cadence.OncePerSubject,

        // ── #664 · the eleven, and why each one is the cadence it is ────────────────────────────────────
        //
        // ONCE EVER, because the picture, the caption and the arithmetic are byte-identical every time it
        // fires, so a second showing adds a dismissal and nothing else. The purge handle is the one
        // irreversible act in the game and the card is its milestone; the bounced filing is the first thing
        // the KAAMOS arc ever says to most captains; and the shelter plate is a RULE OF THE WORLD — its own
        // caller files the line at PulseRank.Beat and calls it "a rule of the world learned once".
        Beat.ArchivePurged or Beat.KaamosFilingBounced or Beat.ShelterIsNotSanctuary => Cadence.OnceEver,

        // #1199 · …and the empty walk with them, for a reason of its own. The owner's whole argument for this
        // beat is that it is spent sparingly — "used sparingly, because otherwise they lose their dramatic
        // potency" — and a card that could come up twice is the moment turning into a mechanic. The vault
        // flag already refuses a second spend; this says the same thing one floor up, so the two halves of
        // "once" cannot ever come to two answers.
        Beat.TheObservationWalk => Cadence.OnceEver,

        // #1199 (2026-09-18) · …and the two COIN MACHINES in its gallery are the opposite case, for the
        // plainest reason in this file: the captain paid. A cadence that swallowed the second press would
        // take the coins and show nothing, which is not restraint, it is a fixture that steals. EveryTime is
        // also what makes the binoculars' alternation legible at all — the second look is the one that swings
        // the optics down, and a cooled card would have hidden the half of this fixture worth finding.
        Beat.TheWalksBinoculars or Beat.TheGalleryVendor => Cadence.EveryTime,

        // COOLED, because the moment is real every time and the CARD is not. A stranger standing you a drink
        // is worth a picture the first time each evening and wallpaper by the third; a captain throwing three
        // sealed hatches in one hull in one minute has already been told what is behind them.
        Beat.StrangerStandsADrink or Beat.SealedDoorReleased => Cadence.Cooled,

        // ONCE PER SUBJECT — #541's cadence, and every one of these is about a PLACE or a THING rather than
        // about the captain. A second KAAMOS shard is a different painting and different words; a second
        // moon's buried door is a different moon. OnceEver would show one and silently swallow the rest,
        // which is the exact failure the arrival tube was written to stop.
        // #1149 · …and the refuge that failed with them, for the same clause exactly: it is about a PLACE.
        // A site carries at most one, so once per subject IS once per building — and OnceEver would show the
        // first one a captain ever walked into and silently swallow every other site's.
        Beat.KaamosShardFound or Beat.NebulaShardFound or Beat.OutpostEffectsRead
            or Beat.SecretLabDoorFound or Beat.TheDormantThingWakes
            or Beat.RefugeFailed => Cadence.OncePerSubject,

        // #973 L5b · …and the walk-in with them, for the same clause and one more. It is about a PERSON, and
        // a person who has already crossed a room to ask you for something does not do it again — the ask is
        // the whole of the scene, and the second time it would be a job board with a face on it.
        Beat.WalkIn => Cadence.OncePerSubject,

        // #973 · …and Flashback falls through to EveryTime with them, for the clause EveryTime is reserved
        // for: it is rare by its OWN nature and cannot be made repetitive by trying. A page may be read at
        // ONCE PER LIFE and never again (FilingLine.PageState.Refused is the latch), and there are only grey
        // pages to read at all after a captain has died. OncePerSubject was the near miss and it is wrong for
        // the one reason that matters here: the rebirth RE-GREYS the book, so the same page read by a later
        // captain is a different captain reaching for a different stranger's afternoon, and swallowing that
        // would silently un-illustrate every flashback after the first death.

        // …and CollectorsSetDown falls through to EveryTime with the grapples, deliberately. It is the only
        // warning the player gets — after it the only information in the world is a tracker fan — and it is
        // rare by its own nature (a heat threshold, and at most one landing per excursion), which is the
        // clause EveryTime is reserved for. A warning suppressed for being repetitive is not a warning.
        _ => Cadence.EveryTime,                    // a collector's grapples, a crew meeting, an arc breaking
    };

    /// <summary>How long a cooled beat holds its tongue. Tuned so a beat cannot punctuate the same fight twice
    /// and cannot become the wallpaper of a long run. FLAGGED for the owner's tuning.</summary>
    public static double CooldownSeconds(Beat beat) => beat switch
    {
        Beat.SailHoled => 6 * 60.0,
        Beat.ChargeLetGo => 10 * 60.0,

        // #664 · A stranger's cognac is a whole bar visit apart; a sealed hatch is long enough that the three
        // doors of one sweep give one card and not three, and short enough that the NEXT hull is a fresh
        // fright. FLAGGED for the owner's tuning, like the two above.
        Beat.StrangerStandsADrink => 15 * 60.0,
        Beat.SealedDoorReleased => 5 * 60.0,

        _ => 0.0,
    };

    /// <summary>Card, plate, or hosted. The rule of thumb: if the player was already standing still, they can
    /// have a modal; if something is moving toward them, they get a plate; and if the moment already HAS a
    /// card — the caller's own, showing this beat's painting — the beat is hosted and the seam shows
    /// nothing (#777).</summary>
    public static Presentation PresentationOf(Beat beat) => beat switch
    {
        Beat.FirstShotFired => Presentation.Plate,   // it happens mid-fight; it must not take the keyboard
        Beat.SailHoled => Presentation.Plate,
        Beat.ChargeLetGo => Presentation.Plate,
        // #541: scene-setting, never a decision — a docking must not wait for anybody to read anything.
        Beat.BerthGreatPort or Beat.BerthWorkingBerth or Beat.BerthOutpost => Presentation.Plate,
        // #777: the grapples arrive AS the BUSTED demand panel, which has been showing this beat's painting
        // since #528. A card here would be a second modal over the first, with the same picture on it.
        Beat.CollectorHail => Presentation.Hosted,

        // #973 L5b · HOSTED, and the host is HER OWN CARD. She is standing at the table with her portrait on
        // the screen and her two lines under it by the time this beat is raised; a card here would be the
        // same face twice on one screen, which is exactly what #777 named. The seam still spends the cadence,
        // files the seen-set and writes the words into the ledger — which is the whole of what a hosted beat
        // is for.
        Beat.WalkIn => Presentation.Hosted,

        // #973 · A PLATE, and deliberately not a card. The captain is at the Captain's desk with the ledger
        // open, clicking grey rows; a full-screen modal over that is a dismissal between every click, which
        // is the "too repetitive" half of the owner's law arriving through the back door. The plate rides the
        // edge for its seven seconds while the book stays open and readable underneath — and the page the
        // captain just won back is right there to be read, which is the whole reason they clicked.
        Beat.Flashback => Presentation.Plate,

        // #664 · All eleven of the adopted moments fall through to CARD, and that is not an oversight: every
        // one of them was already a full-screen modal under the other system, and every one of them is a
        // moment where the world has just stopped for the captain anyway — a handle pulled, a shard laid on
        // a table, a hatch coming off its dogs. What changes is not whether they take the screen; it is
        // WHEN, and how often.
        _ => Presentation.Card,                      // the deputation, the meeting, the news
    };

    /// <summary>
    /// #777 · WHOSE CARD IS THE CANVAS. A <see cref="Presentation.Hosted"/> beat is only honest if some
    /// surface really does show it, so the host is named here in the same file as the cadence and the art —
    /// one place, and a beat that claims a host it does not have is a beat nobody can find.
    ///
    /// <para>Prose rather than a type on purpose: the host is a card in the client and Core does not know
    /// what a card is. The client's guards read this the way the art manifest reads
    /// <see cref="ArtFile"/> — as the sentence a human checks the markup against.</para>
    /// </summary>
    /// <returns>The host's name, or an empty string for a beat the seam raises itself.</returns>
    public static string HostCard(Beat beat) => beat switch
    {
        Beat.CollectorHail => "the BUSTED demand panel (Map.razor, BustedEncounter.Stage.Demand)",

        // #973 L5b · her card, raised on the frame she reaches the table and taken down when she leaves it.
        Beat.WalkIn => "the WALK-IN card (Map.razor, Map.WalkIn.cs · _walkInCard)",

        _ => "",
    };

    /// <summary>
    /// Whether a card may WAIT for a safer moment rather than landing now.
    ///
    /// <para>The expensive lesson this exists for: a full-screen tutorial card once let a pack of Reevers kill
    /// the captain behind it, because the world kept running under the modal. A story card must never be the
    /// reason somebody died — so a deferrable one queues until the scene is calm, and the moments that ARE the
    /// danger (a collector already has you) do not defer, because deferring them would be absurd.</para>
    /// </summary>
    /// <para>#777 · A HOSTED beat never defers either, and for a second reason on top of the first: there is
    /// nothing to hold. Its surface is a card the caller is raising right now, so "later" would mean showing
    /// the words after the picture they belong to has gone.</para>
    /// <para>#664 · AND FOUR OF THE ADOPTED ELEVEN SAY NO FOR THE SAME TWO REASONS THE HAIL DOES. Three of
    /// them RAISE the danger one statement before they knock — the pack comes off its benches, the pack comes
    /// out of the hatch, the collectors are already walking — so <c>CaptainIsInDanger()</c> is true at the
    /// instant of the raise and a deferrable card there does not wait for a calmer moment: it waits for the
    /// fight to end and then explains a thing that is already over. And two of them ARE the warning: the
    /// shelter card is the only sentence in the game that says a pressure vessel will not save you, and the
    /// arrival card is, in its own caller's words, <i>"THE ONLY WARNING THE PLAYER GETS"</i>. A warning held
    /// back until it is safe to read is not a warning, it is a receipt.</para>
    /// <para>#865's sit-beat hold is a different rule and still covers all eleven: that arm asks nothing
    /// about deferrability, because it is a beat and a half of screen owed to a press the player just made.</para>
    public static bool DeferrableWhileInDanger(Beat beat) => beat switch
    {
        Beat.CollectorHail => false,   // this IS the danger; it cannot wait for a better time

        // #664 · the pack is standing off its benches / coming through the hatch as this is raised
        Beat.TheDormantThingWakes or Beat.SealedDoorReleased => false,

        // #664 · the warning, which is worth nothing after the thing it warns about
        Beat.ShelterIsNotSanctuary or Beat.CollectorsSetDown => false,

        _ => PresentationOf(beat) == Presentation.Card,
    };

    /// <summary>How long a plate stays up. Long enough to read the caption at a glance, short enough that it is
    /// gone before it becomes furniture.</summary>
    public const double PlateSeconds = 7.0;

    /// <summary>
    /// #1148 · <b>HOLD THE BEATS WHILE A GATE IS DRIVING A BOARD.</b> The URL key, named once so the parse,
    /// the docs table and the browser gate cannot drift about how it is spelled: <c>?holdbeats=1</c>.
    ///
    /// <para><b>What went wrong.</b> The UiGate's canaries script <i>open a board, press its way out</i>, and
    /// nothing in them quiesces this seam. In a loaded 13-minute serial run a card's cadence came due while
    /// the charge board was open, the card's <c>.view-object-backdrop</c> went over the board's own
    /// <i>Step away</i>, and Playwright waited sixty seconds for a button a modal was standing on. The same
    /// class passed alone at the base (35 s) and alone at that head (29 s): the gate was measuring the
    /// story's timing rather than the board.</para>
    ///
    /// <para><b>What it does, and the one thing it must never do.</b> Held, a CARD is DEFERRED — into the
    /// same one-at-a-time queue #865's sit-beat hold and the danger hold already use, and for the same
    /// reason: the cadence is unspent until the beat actually speaks, so nothing is dropped and the beat is
    /// still owed. #761's law is that a plot-significant moment reaches the player, and a test flag that
    /// could DELETE one would be a gate quietly editing the game it is measuring. Deferral, never a drop.</para>
    ///
    /// <para><b>Cards only, deliberately.</b> A PLATE eats no click (<c>pointer-events: none</c>) and steals
    /// no keyboard, so it never blocked anything; and the gate next door
    /// (<c>HudCollisionTests.The_story_plate_never_covers_the_plotting_panel</c>) exists to catch one lying
    /// on the plotting panel, which a hold would silently un-test.</para>
    /// </summary>
    public const string HoldQueryFlag = "holdbeats";

    /// <summary>
    /// #1148 · Does this URL carry <see cref="HoldQueryFlag"/>? Asked of the LIVE address rather than read
    /// into the boot's <c>BootQuery</c> holder, for the reason <c>?perf=1</c> (#841) is read the same way one
    /// file over: it changes nothing about the world — no body, no berth, no cheat — so it has no business in
    /// the holder that pins what the parse ANSWERED, and a field for it on the page would move thirty pinned
    /// frame fingerprints (#905) to carry a value that is <c>False</c> in every one of them.
    ///
    /// <para>Takes a whole URL or a bare query; a fragment is not the query. Values are the <c>1|true|yes</c>
    /// the client's own dev cheats accept, so the key spelled with any other value is NOT a hold — a flag
    /// that held on anything at all could not tell its own pass from its own fail.</para>
    ///
    /// <para>Allocation-free on purpose: while a beat is held this is asked on every frame.</para>
    /// </summary>
    public static bool HeldIn(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        int question = url.IndexOf('?', StringComparison.Ordinal);
        ReadOnlySpan<char> query = question < 0 ? url.AsSpan() : url.AsSpan(question + 1);
        int fragment = query.IndexOf('#');
        if (fragment >= 0)
        {
            query = query[..fragment];
        }

        while (!query.IsEmpty)
        {
            int amp = query.IndexOf('&');
            ReadOnlySpan<char> pair = amp < 0 ? query : query[..amp];
            query = amp < 0 ? default : query[(amp + 1)..];

            if (!pair.StartsWith(HoldQueryFlag + "=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ReadOnlySpan<char> said = pair[(HoldQueryFlag.Length + 1)..];
            if (said.Equals("1", StringComparison.OrdinalIgnoreCase)
                || said.Equals("true", StringComparison.OrdinalIgnoreCase)
                || said.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// #664 · THE NOISE THE SURFACE MAKES, decided here for the same reason the picture and the cadence are.
    ///
    /// <para>The seam used to chime <c>"reveal"</c> for every card and plate it raised, which was right while
    /// every beat in the file was raised by the seam alone. The eleven moments adopted from the deleted
    /// reveal-card system are not: each of them is a press or an event that <b>already makes its own
    /// noise</b>, chosen by the moment — <c>"board"</c> for a find, <c>"alarm"</c> for a hatch coming off its
    /// dogs and a pack coming through it — and the old card was deliberately silent so as not to flatten
    /// three different moments into one chime. Layering a second cue over that is the stacked-card mistake in
    /// the one channel the player cannot close, which is the argument
    /// <see cref="Presentation.Hosted"/> already makes in this file.</para>
    ///
    /// <para>So: an empty string means <i>the act that raised this beat has already been heard</i>, and it is
    /// a statement about the moment rather than a client's opinion about the seam — which is why it lives
    /// here beside the cadence and not in an argument the caller passes.</para>
    /// </summary>
    public static string Cue(Beat beat) => beat switch
    {
        Beat.ArchivePurged or Beat.StrangerStandsADrink or Beat.KaamosShardFound or Beat.KaamosFilingBounced
            or Beat.NebulaShardFound or Beat.OutpostEffectsRead or Beat.SecretLabDoorFound
            or Beat.TheDormantThingWakes or Beat.ShelterIsNotSanctuary or Beat.CollectorsSetDown
            or Beat.SealedDoorReleased => "",
        _ => "reveal",
    };
}
