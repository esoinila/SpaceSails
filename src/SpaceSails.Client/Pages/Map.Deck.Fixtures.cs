using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Deck (#870 split; the header note lives in Map.Deck.cs) — the two things ashore that answer a press in their own right: the prop you lean into and read twice, and the locked hatch — the session's unlock set, the knock, and the keypad a crack job puts up.
public partial class Map
{
    // Doors that grow the world (Wednesday plan §3 PR-F): the set of station hatches cracked open this
    // session, as composite "<bodyId>:<hatchId>" keys. A hatch that grows a wing (HavenInterior.
    // HatchGrowsWing) welds its back room onto the deck plan when unlocked. Per-session only — the
    // owner accepted that for v1 (Wednesday plan §1); it lives beside the other session-scoped state.
    private readonly HashSet<string> _unlockedHatches = [];

    // The bare hatch ids cracked open at a given station — the subset HavenInterior.DockedDeck needs.
    private IReadOnlySet<string> UnlockedHatchesFor(string bodyId) =>
        _unlockedHatches
            .Where(k => k.StartsWith(bodyId + ":", StringComparison.Ordinal))
            .Select(k => k[(bodyId.Length + 1)..])
            .ToHashSet();

    private bool IsHatchUnlocked(string bodyId, string hatchId) =>
        _unlockedHatches.Contains($"{bodyId}:{hatchId}");

    // Crack a hatch open for the session and, if it grows a wing, weld the room on by rebuilding the
    // docked deck plan (the world literally grew a room behind you).
    private void UnlockHatch(string bodyId, string hatchId)
    {
        if (_unlockedHatches.Add($"{bodyId}:{hatchId}"))
        {
            RebuildDockedDeck();
        }
    }

    // --- Ashore quests (M-Q1): the hooded stranger at the bar table ---

    // Walk up to a booth and press E. Which patron you're next to (from their console label) sets who
    // you're dealing with and their trade — One-Eye Silas fences bounties (hunts), Madam Coil runs
    // parcels (cargo runs). If you already owe this giver a job, they just nod at it.
    // A Gen-AI image the player is currently viewing (a souvenir, a lore prop), or null. Pressing E on
    // a ViewObject console pops it up; E again (or the close button / clicking away) dismisses it.
    private DeckPlan.ConsoleSpot? _viewObject;

    // #422 arc 2 — how many times this session the captain has stopped to READ a Nebula Mutual PIRATE
    // INSURANCE poster. The cheerful sell is fragment #1, already in every port; reading it a SECOND time /
    // closely reads the grey bottom line differently and assembles `fine-print` (the fragment's own fiction:
    // "the fine print, read twice"). Run-scoped — a fresh voyage starts naive.
    private int _insurancePosterReads;

    private void ViewNearbyObject()
    {
        if (_viewObject is not null)
        {
            CloseViewObject();  // E again closes — through the one door, so #768's held line is freed here too
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is { Kind: DeckPlan.ConsoleKind.ViewObject } spot)
        {
            // #411 · the head office's two consoles show their own card again, so the beats a captain will
            // want to look at twice can be looked at twice. Asked before the plaque path, which is about a
            // dedication plate and has nothing to say about a room under the ice.
            if (_surface is { } hqEx && TryHeadOfficeConsole(hqEx, spot.Label))
            {
                return;
            }

            // #1061 beat 2 · …and the sheet a frightened man dropped in the dust, which is a PICKUP as well
            // as a read and therefore cannot fall through to the plain look below. Recognised by its own
            // plate, exactly as the two consoles above are, so this lane adds no dispatch of its own.
            if (_surface is { } dropEx && TryTheDroppedSchedule(dropEx, spot.Label))
            {
                return;
            }

            _viewObject = MaybeAppendPlaqueGratitude(spot); // #394: Ringside's plaque grows a line once saved

            // #411: reading the whole dedication plate that NAMES PROJEKTI KAAMOS (Ringside's, the one place
            // the ice-moon project is named) assembles the first shard — the listed berth nobody files for.
            if (_viewObject is { Caption: { } caption } && caption.Contains("KAAMOS", StringComparison.Ordinal))
            {
                TryAssembleKaamos("listed-berth",
                    "❄ You read the whole plate this time, not just the dedication. " +
                    Core.KaamosLore.ById("listed-berth")!.Lore);
            }

            // #422: the PIRATE INSURANCE poster (Nebula Mutual, #380/#415). The first read is the cheerful
            // sell; the SECOND time you stop at one you read the grey bottom line, and it reads differently
            // now — that assembles `fine-print`. Detected by the poster's own label so this lane touches no
            // HavenInterior code (that file is another lane's). The read count is session-scoped.
            if (_viewObject is { Label: { } label } && label.Contains("PIRATE INSURANCE", StringComparison.Ordinal))
            {
                _insurancePosterReads++;
                if (_insurancePosterReads == 1)
                {
                    // The tell one beat early (#380's law), in the captain's own voice — it used to end on a
                    // parenthesised instruction to the player. Copy in Core beside the fragment it leads to.
                    ShowPulseMessage(Core.NebulaLore.PosterFirstReadLine);
                }
                else if (_insurancePosterReads >= 2)
                {
                    TryAssembleNebula("fine-print",
                        "📋 This time you actually read the small print, the grey line no advertising should keep. " +
                        Core.NebulaLore.ById("fine-print")!.Lore);
                }

                // #973 L4 · …and a THIRD thing, for a man who has been through the clinic once. Asked LAST,
                // so the two reads above are exactly the two reads they have always been — the cheerful sell,
                // then the grey line — and a first-life captain never reaches past this call.
                ThePosterAfterARebirth();
            }

            // #973 L4 · THE THREE SMALL PLATES, detected by the ad's own WORDS (Core.StationAds) — the same
            // idiom the poster above is detected by, and for the same reason: the wall is hung in
            // HavenInterior and this file never has to be told which fixture is which.
            if (_viewObject is { Label: { } adLabel }
                && Core.StationAds.IndexOfLabel(adLabel) is { } adIndex)
            {
                TheAdIsRead(adIndex);
            }
        }
    }

    /// <summary>#768 · The card comes down and the sayings it was standing on are freed. An arrival that
    /// raises one of these — the first descent (#585), the dead-air warning (#609), the gate's own face
    /// (#684) — holds its ranked lines rather than pulsing them under the backdrop, so this is the moment
    /// the winner is finally said. Every road out of the card (Esc, Enter, E again, the backdrop, Close)
    /// comes through here.</summary>
    private void CloseViewObject()
    {
        _viewObject = null;
        ReleaseHeldSayings();
    }

    private void KnockOnHatch()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.Hatch } hatch)
        {
            return;
        }
        string id = HatchId(hatch.Label);

        // An opened expansion joint (PR-F): the hatch is already cracked and the back room is welded
        // on — there's nothing to knock on, just a doorway to step through.
        if (_dockedHavenId is { } open && HavenInterior.HatchGrowsWing(open, id) && IsHatchUnlocked(open, id))
        {
            ShowPulseMessage($"{hatch.Label} stands open — the back room's yours. Step inside. 📂");
            return;
        }

        // Is this the specific hatch a crack job sent us to (at this station)? If so, the knock isn't
        // an idle rap — it brings up the keypad.
        Quest? job = _quests.FirstOrDefault(q =>
            q.Kind == QuestKind.Crack && q.TargetShipId == id && q.SourceBodyId == _dockedHavenId);
        if (job is { State: QuestState.Active })
        {
            _pinJob = job;
            _pinHatch = hatch;
            _pinEntry = "";
            return;
        }
        if (job is not null && job.State != QuestState.Active)
        {
            ShowPulseMessage($"{hatch.Label} — already cracked. You pull it shut behind you.");
            return;
        }

        ShowPulseMessage($"{hatch.Label} — sealed. You knock; only the station's hum answers. 🔒");
    }

    // Parse a hatch label ("🔒 BONDED STORES · V-06") without leaning on the exact separator glyph. The
    // id is the last whitespace token ("V-06"); the department is the run of all-letter tokens
    // ("BONDED STORES") — which skips the emoji tag, the separator, and the id itself.
    private static string HatchId(string label)
    {
        string[] parts = label.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : label.Trim();
    }

    private static string HatchDept(string label) =>
        string.Join(' ', label.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.All(char.IsLetter)));

    // A stable 4-digit access code for a hatch — the same code every time so the Fixer can quote it and
    // it still works when you walk over. Deterministic (no RNG in player-facing quest gen).
    private static string MakePin(string hatchId) =>
        (hatchId.Sum(ch => ch) * 137 % 9000 + 1000).ToString(CultureInfo.InvariantCulture);

    // --- Keypad state for cracking a locked hatch -----------------------------------------------------
    private Quest? _pinJob;                 // the crack job whose hatch we're keying into, or null
    private DeckPlan.ConsoleSpot? _pinHatch; // the hatch being cracked (for the keypad's header)
    private string _pinEntry = "";           // digits keyed so far (max 4)

    /// <summary>#736 · What the last submitted code did, said ON the keypad. A wrong code leaves the pad up
    /// (you are meant to try again), and the buzz that told you it was wrong was pulsed to the HUD under the
    /// pad's own backdrop — the display simply blanked and nothing said why. Cleared with the pad.</summary>
    private string? _pinOutcome;

    // Four slots, filled left to right: keyed digits, then "·" placeholders.
    private string PinDisplay => string.Concat(Enumerable.Range(0, 4)
        .Select(i => i < _pinEntry.Length ? _pinEntry[i] : '·'));

    private void PinPush(string digit)
    {
        if (_pinEntry.Length < 4)
        {
            _pinEntry += digit;
        }
    }

    private void PinClear() => _pinEntry = "";

    private void CancelPin()
    {
        _pinJob = null;
        _pinHatch = null;
        _pinEntry = "";
        _pinOutcome = null;
    }

    private void SubmitPin()
    {
        if (_pinJob is not { } job)
        {
            return;
        }
        if (_pinEntry == job.Pin)
        {
            RendererInterop.PlayCue("board");
            // #736 · The pad is dismissed BY THIS PRESS, so it is dismissed FIRST and the receipt is said
            // after: a line routed to a panel that is about to be torn down is a line nobody reads. With the
            // pad gone the seam puts it on whatever is still in front of the captain, and with nothing in
            // front of them at all it puts it on the world — which is what this branch always wanted.
            CancelPin();
            if (_dockedHavenId is { } station && HavenInterior.HatchGrowsWing(station, job.TargetShipId))
            {
                // Doors that grow the world (PR-F): this hatch opens a real back room. Weld it on and
                // leave the job Active — you still have to walk in and lift the package off the shelf.
                UnlockHatch(station, job.TargetShipId);
                SayItWhereTheyAreLooking("The lock blinks green — the hatch grinds aside onto a dark back room. Something's on the shelf inside. Step in and take it. 📦");
            }
            else
            {
                // #727 · A plain lockup: the package is simply behind the panel, pocketed on the spot — a
                // step the chair issued, finished on foot at a keypad, through the one writer.
                AdvanceMission(job, QuestState.PickedUp,
                    "The lock blinks green — the hatch sighs open. You pocket the package and pull it shut behind you. 📦");
            }
        }
        else
        {
            // #736 · The pad STAYS UP on a wrong code — that is the whole point of a keypad — so the buzz is
            // said on the pad. Pulsed, it played under the pad's own backdrop and all the captain saw was
            // four dots going back to dots. The right code closes the pad above, so its receipt still pulses.
            _pinEntry = "";
            SayItWhereTheyAreLooking("The panel buzzes red — wrong code. 🔴");
        }
    }
}
