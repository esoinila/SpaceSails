using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// WHAT THE HUD SAYS — #440's standing prompt (ONE bright line above the keybar for the thing this
/// excursion hangs on), the channel glyph and whether the channel is aid, the key hints, and the
/// instrument column's captions.
///
/// <para>Owner, 2026-07-26: <i>"the press T to bury treasure is not advertised clearly enough on
/// surface… It is the key to survival there"</i> — said while misremembering the key, which is the proof.
/// A chest in hand is the whole reason you came and the whole thing you lose, so it gets a line that does
/// not blend into chrome and does not go away until the chest is in the ground.</para>
///
/// <para>Split out of <c>Map.Surface.Hud.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // #440 · The standing prompt: ONE bright line above the keybar for the thing this excursion hangs on.
    // Owner, 2026-07-26: "the press T to bury treasure is not advertised clearly enough on surface… It is
    // the key to survival there" — said while misremembering the key, which is the proof. A chest in hand is
    // the whole reason you came and the whole thing you lose, so it gets a line that does not blend into
    // chrome and does not go away until the chest is in the ground. It also answers WHERE, because "where
    // you stand" is the rule and nothing on screen ever said so: out on the open regolith, past the pad.
    private string? BuildStandingPrompt(SurfaceExcursion ex)
    {
        // #696 · A HOLD OUTRANKS THE CHEST. For the seconds it runs, the one thing on the screen that can
        // still be decided is whether the captain keeps their boots where they are — and the prompt says the
        // clock, because "hold position" without a number is an instruction to wait for an unknown length of
        // time while something walks towards you.
        if (_processing is { } paper)
        {
            return $"{Core.Processing.Glyph} PROCESSING — hold position " +
                $"({Core.Processing.SecondsLeft(paper.Elapsed, ProcessingSeconds):F0} s). Step away and it is lost.";
        }

        if (!ex.Carrying)
        {
            return null; // nothing owed — the ground goes quiet again
        }
        // #723 · The floor rides along, so this line stops promising a burial on a Hive corridor. Underground
        // it now reads "walk out onto the regolith" — which is the honest instruction down there, because the
        // way to bury a chest 150 m under a facility is the lift.
        return MoonSurface.IsDiggableGround(_avatarX, _avatarY, ex.Floor)
            ? "⛏ CARRYING THE CHEST — press E to BURY IT HERE"
            : "⛏ CARRYING THE CHEST — walk out onto the regolith, then E to bury it";
    }

    /// <summary>#562 + #696 · WHICH slow thing the one bar is showing. A ladder rather than four inline
    /// conditions at the call site, because the glyph, the tint and the PROGRESS all have to pick the same
    /// winner — and three copies of one precedence order is the shape that drifts.</summary>
    /// <remarks>#1016 · The hold is handed in rather than read off the excursion — it left the excursion for
    /// the page, because a captain digging at a top in a docked bar has no excursion to hang a clock on. Still
    /// static and still pure: which winner it picks, and in what order, is untouched.</remarks>
    private static string SurfaceChannelGlyph(SurfaceExcursion ex, ProcessingHold? hold) =>
        ex.Channel is not null || ex.DoorChannel is not null ? "⛏"
        // #784 · …and the seated register wears the PEN, not the camera. Core.Processing.GlyphFor is the one
        // place that choice is made — the control, the bar and the book entry all read it, so the glyph over
        // a captain's head can never say "photographing" while the sim writes into the field book (#562).
        : hold is not null ? Core.Processing.GlyphFor(hold.Work)
        : ex.RearmBotIndex is not null ? "🔫"
        : "⛏";

    /// <summary>#562 · Is the bar the ship HELPING you (cold green) or you exposing yourself (warning
    /// amber)? Only the rearm is help. The darkroom is emphatically not: standing still in the open for
    /// twenty seconds is the cost the whole mechanic is made of, and a soothing colour over it would be the
    /// picture arguing with the sim.</summary>
    /// <remarks>#1016 · The hold is handed in, for <see cref="SurfaceChannelGlyph"/>'s reason one method
    /// along. The answer is the same answer.</remarks>
    private static bool SurfaceChannelIsAid(SurfaceExcursion ex, ProcessingHold? hold) =>
        ex.Channel is null && ex.DoorChannel is null && hold is null && ex.RearmBotIndex is not null;

    // #324: the contextual surface keybar. The owner couldn't find the deploy key — so while a bot rides
    // the sling it spells out [T] deploy, and a chest in hand spells [G] drop. Affordances never hide.
    private string BuildSurfaceKeyHints(SurfaceExcursion ex)
    {
        // #488: aboard a derelict there is nothing to DIG. There is, however, very much somewhere to plant
        // a sentry — a bot holding a corridor while a compartment pumps down is the loop this whole lane is
        // for — and this bar used to say otherwise and then hide the key, which is how the owner ended up
        // pressing T at a map that showed him nothing. Affordances never hide (#212).
        if (Derelict.TryParseWreckId(ex.Stop.Body.Id, out _))
        {
            var aboard = new List<string>
            {
                "WASD — move",
                // #698 · What [E] will actually do, and the ground wins. The recovery runs ahead of console
                // dispatch (#691), so standing in the ring with "E — examine / take" on the bar is the bar
                // describing a press it is not going to get.
                StandingOnWhatYouLeft() ? LeftBehind.ReachPrompt : "E — examine / take",
            };

            // #538 · the sentry remote lives on the HUD, and it never hides: an affordance you cannot see is an
            // affordance you do not have (#212), and this is the one whose absence gets a captain shot.
            if (ex.Bots.Count > 0)
            {
                aboard.Add(_weaponsTight ? "🤖 H — WEAPONS TIGHT (press to free)" : "🤖 H — weapons tight");
            }

            if (ex.Bots.Any(b => !b.Deployed))
            {
                // #326 · TWO STANCES, BOTH NAMED. The choice is made at the press, so both halves of it have
                // to be on the bar at the moment the press is available — a stance the captain is never told
                // about is a stance he does not have (#212), and this is the one that decides whether the
                // way back to her lock stays open. The words are the owner's own.
                aboard.Add($"🤖 T — {SentryDoctrine.DeployHereLabel}");
                aboard.Add($"🤖 ⇧T — {SentryDoctrine.HoldMyLineHomeLabel}");
            }
            else if (ex.Bots.Any(b => b.Deployed &&
                     ((b.X - _avatarX) * (b.X - _avatarX)) + ((b.Y - _avatarY) * (b.Y - _avatarY))
                         <= DeckPlan.InteractRadius * DeckPlan.InteractRadius))
            {
                aboard.Add("🤖 T — pick up the sentry");
            }
            if (_satchel.Count > 0)
            {
                aboard.Add($"🎒 I — items ({_satchel.Count})");
            }

            // #537 · A VERB NOBODY IS TOLD ABOUT IS A VERB NOBODY HAS. Caught by booting the scene and
            // reading the hint bar, which is the owner's own method: the knock was bound, the clock ran, the
            // sweep team heard it — and the strip along the bottom never mentioned K existed.
            aboard.Add(IsSounding
                ? (_soundQuietly ? "✊ K — stop knocking" : "📡 K — stop sounding")
                : (_soundQuietly ? "✊ K — knock (quiet)" : "📡 K — sound the plating (loud)"));

            aboard.Add(_audioEnabled ? "🔊 M — mute" : "🔇 M — unmute");
            return string.Join(" ∙ ", aboard);
        }

        // #440: the bar must NAME the thing that matters. "E — dig / use" is honest but generic, and it was
        // generic at the one moment it should shout — with the chest in your hands (owner, 2026-07-26: "the
        // press T to bury treasure is not advertised clearly enough on surface… It is the key to survival
        // there", having misremembered the key himself). Carrying → the bar says BURY, in the imperative.
        // #698 · AND WHAT YOU PUT DOWN OUTRANKS BOTH OF THEM. Owner, on B12 of the clinic: "I dropped 3
        // files on somebody here but there was nothing marked onto the map?" — the deck now carries the
        // mark, and this is the other half: [E] answers your feet before it answers the walls (#691), so
        // inside the recovery ring the press is the pickup, whatever else the captain is holding. A bar
        // that promised BURY THE CHEST while the key handed back a folder would be the sim doing one thing
        // and a sentence reporting another, which is a bug class this repo has named.
        // #723 · …and that is precisely what this bar was doing underground. It offered "E — dig" on poured
        // rockcrete, and with a chest in the sling it shouted BURY THE CHEST HERE over a corridor where the
        // key now — correctly — does nothing at all. So the floor is asked first, of the same one fact the
        // key is gated on. Above ground nothing moves: the pad is not diggable either, but it is one step
        // from ground that is, so the chest keeps the imperative #440 asked for.
        var parts = new List<string>
        {
            "WASD — move",
            // #828 · …and the BIN says so, in the same ladder and the same order the [E] dispatch itself
            // asks: your feet first, then the bucket you are standing at, then the ground. Underground this
            // strip read "E — use" everywhere, which is exactly nothing at the one spot where the key opens
            // the sleeve over a bin — and a verb nobody is told about is a verb nobody has (#212/#537).
            StandingOnWhatYouLeft() ? LeftBehind.ReachPrompt
                : TheBinTakingYourPress() is { } atTheBin ? RipAndBin.KeyPrompt(atTheBin.Tier)
                : !MoonSurface.ShovelWorksOnThisFloor(ex.Floor) ? "E — use"
                : ex.Carrying ? "⛏ E — BURY THE CHEST HERE"
                : "E — dig / use",
        };
        bool carryingBot = ex.Bots.Any(b => !b.Deployed);
        bool deployedUnderfoot = ex.Bots.Any(b => b.Deployed &&
            ((b.X - _avatarX) * (b.X - _avatarX)) + ((b.Y - _avatarY) * (b.Y - _avatarY))
                <= DeckPlan.InteractRadius * DeckPlan.InteractRadius);
        if (carryingBot)
        {
            parts.Add("🤖 T — deploy a sentry");
        }
        else if (deployedUnderfoot)
        {
            parts.Add("🤖 T — pick up the sentry");
        }
        if (ex.Carrying)
        {
            parts.Add("G — drop the chest & sprint");
        }

        // #603 · The satchel, once there is anything in it. Owner: "the I key should be advertised in the
        // hud also like we do now for the other keys." Shown WITH the count, because the useful question at
        // a glance is not "do I have pockets" but "is there anything in them".
        if (_satchel.Count > 0)
        {
            parts.Add($"🎒 I — items ({_satchel.Count})");
        }
        parts.Add(_audioEnabled ? "🔊 M — mute" : "🔇 M — unmute"); // #338: the first-sound switch, always spelled out
        return string.Join(" ∙ ", parts);
    }

    // Lane-1 (owner, 2026-07-18: "advertise the dig and bot options in text under the motion detector"):
    // the short contextual lines seated below the tracker readout in the left instrument column. They
    // teach the two levers the surface offers — the DIG (the reason to come, the reason to hurry) and the
    // SENTRY (the thing that buys time against the tide, never safety). Kept to a couple of lines so the
    // column stays legible; empty entries are skipped by the renderer.
    private List<string> BuildTrackerCaptions(SurfaceExcursion ex, int ownMarkCount)
    {
        var lines = new List<string>();

        // #564 · The tank used to be the top line HERE, and the owner went looking for a meter under the
        // tracker and found nothing — because a line of dim 10px text among the key hints is a footnote, not
        // a gauge. It is a drawn BAR now (DeckView, fed by SurfaceHud.AirSeconds); this list is back to
        // being what it always was, the affordances.

        // #728 · …EXCEPT FOR THE OTHER CONSUMABLE, which had no line anywhere. The shelter's press has been
        // announcing "N rounds into your magazines" into a stat that appeared on exactly one surface in the
        // game — the two-digit counter painted over an already-deployed bot — so a captain who kept both in
        // the sling could read a receipt and never see the account.
        //
        // It goes FIRST and directly under the air bar because it is an INSTRUMENT, not an affordance: the
        // lines below teach keys, this one reports a quantity, and the two registers should not be shuffled
        // together. Composed in Core off the same roster the counters read (SentryBot.MagazinesReadout), so
        // the HUD and the bot over there cannot come to disagree about one number.
        //
        // First also settles what a SHORT SCREEN drops, and the answer is not arbitrary: DeckView stops
        // drawing captions once they would reach the keybar, and every affordance below is ALSO spelled out
        // along that keybar (BuildKeyBar names E, T, G and I). This line is told once, here, on the whole
        // screen. Between two tellings and one telling, the one telling keeps the top of the column.
        lines.Add(SentryBot.MagazinesReadout(TheSlingAsTheInstrumentReadsIt(ex)));

        // The dig affordance, honest to the sling (playtest bug #1 / owner ruling #9: the ground must SAY
        // what's possible). Carrying → bury anywhere you stand; empty → the beach-comber probe, a real
        // fishing expedition, never a dead end. An own ✗ in this ground always earns its own lift line.
        // #723 · …and it is only an affordance where the verb exists. This is the line that sent a captain
        // pressing [E] on a spine corridor: teaching the shovel on a floor whose ground is poured rockcrete
        // is teaching a key that will not answer. The same one fact the key and the bar are gated on.
        if (MoonSurface.ShovelWorksOnThisFloor(ex.Floor))
        {
            lines.Add(ex.Carrying
                ? "⛏ E on the regolith — bury the chest where you stand"
                : "🪛 E on the regolith — probe for shallow treasure");
        }
        if (ownMarkCount > 0)
        {
            lines.Add("🗺 E at your ✗ — dig the cache back up");
        }
        // #409: once the hidden lab door is revealed, advertise it until it's forced.
        if (ex.SecretLabDoorRevealed && !ex.SecretLabForced)
        {
            lines.Add("⚙ E at the ⚙ HIDDEN DOOR — force the secret lab open");
        }

        // The sentry affordance — spell out T while it matters (a bot in the sling to set, or ones holding
        // the line). The tide never stops, so the caption tells the truth: they buy time, not safety.
        //
        // #440 (owner, live 2026-07-26: "The T key for sentry planting is not mentioned there now on the
        // sentry line?"). It wasn't: once the LAST bot left the sling, this fell to the "N holding" line,
        // which names no key at all — and the keybar only says [T] while you happen to be standing on a
        // bot. So the moment you had committed both, the key that takes them back up vanished from the
        // screen entirely. Now T is named in EVERY state that has a bot in it, planted or slung.
        int carried = ex.Bots.Count(b => !b.Deployed);
        int deployed = ex.Bots.Count(b => b.Deployed);
        if (carried > 0)
        {
            // #326 · …and in WHICH STANCE, on the same plate and at the same moment, because the stance is
            // chosen at the press and there is no second screen to choose it on. The owner's own two
            // phrases; the count rides the first, where it always did.
            lines.Add($"🤖 T — {SentryDoctrine.DeployHereLabel} ({carried} in the sling)");
            lines.Add($"🤖 ⇧T — {SentryDoctrine.HoldMyLineHomeLabel}");
        }
        if (deployed > 0)
        {
            lines.Add($"🤖 {deployed} sentry holding — T at one to lift it · buys time, not safety");
        }

        return lines;
    }

    /// <summary>#728 · This excursion's magazines, in the shape Core reads them — one projection, so the
    /// instrument column and the shelter's press are asking about the same list rather than each building
    /// their own view of the same bots.</summary>
    private static IReadOnlyList<SentryBot.Carried> TheSlingAsTheInstrumentReadsIt(SurfaceExcursion ex) =>
        [.. ex.Bots.Select(b => new SentryBot.Carried(b.Unit, b.Rounds, b.Deployed))];
}
