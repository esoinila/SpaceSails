using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #837 · LOAD IT FROM THE ROW — the hand-load's second grip.
///
/// <para>Owner, evening playtest with the satchel open on <i>12 loose rounds</i> (2026-08-11): <i>"I would
/// expect like load it into one or both guns."</i> And, adding the count control: <i>"and like how many
/// each .. by default I guess it would be all into one in most cases, but we should give flexibility
/// here."</i></para>
///
/// <para><b>What was wrong.</b> The put verb shipped with #803 and it worked — POSITIONALLY. You walked to a
/// set-down sentry, pressed [I] over it, and the rounds went in. Standing in a corridor looking at the
/// ammunition itself, the only word on the row was <c>read it →</c>: the verb was invisible at exactly the
/// place a player looks for it, and the gun the owner meant was on his own back, where #803 refused outright.
/// This is the same shape #828 fixed for the shredder a day earlier — a verb hiding behind a position — and
/// it gets the same answer.</para>
///
/// <h3>What this file decides, and what it does not</h3>
/// <para><b>Nothing arithmetical.</b> <see cref="SentryHandLoad"/> owns the ceiling, the kind check, the
/// reach and every word; <see cref="SentryBot.MagazineCell"/> prints every count, which is the same function
/// the MAGAZINES readout is built out of. What is left for a client is the three facts only a running world
/// has: <b>which guns are here</b>, <b>how far off they are standing</b>, and the press.</para>
///
/// <h3>The one law</h3>
/// <para><b>TWO GRIPS, ONE VERB.</b> The chooser's press and the [I]-over-the-bot press both end in
/// <c>TheRoundsGoInByHand</c>, with a unit and a count and nothing else — no second mutation, no second
/// sentence, no second sum. The guard drives each grip on identical worlds and compares the magazine and the
/// pocket.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>#837 · The stack of rounds the chooser is currently spending, or null when it is shut. The
    /// ITEM and not a copy of its count: the count is read live off the sleeve on every draw, because the
    /// page stays open across a press and a figure remembered from before one is a figure about a pocket
    /// that no longer exists.</summary>
    private Core.Satchel.Item? _loadFrom;

    /// <summary>#837 · Where each row's stepper has been dragged to, by unit. Empty means every row is at
    /// its default, and the default is ALL — the owner's ruling, and the reason the common case is one
    /// press. Entries are advisory: <see cref="SentryHandLoad.Offered"/> clamps every one of them to a
    /// ceiling it recomputes live, so a share left over from a fuller pocket can never load a round that is
    /// not there.</summary>
    private readonly Dictionary<string, int> _loadShare = new(StringComparer.Ordinal);

    /// <summary>#837 · Is the load control drawn on this row at all? Only on ROUNDS — a card, a paper and
    /// the paperwork for a buried chest are not things that go in a magazine, and on those rows this is not
    /// a refusal but a verb that does not apply (the rule the shredder follows about evidence, one control
    /// over). Core decides it, because the same question decides what the chooser may be opened on.
    ///
    /// <para>Live wherever it IS drawn and never disabled (#212/#603): with nothing in reach it opens onto
    /// the sentence that says what would fix it.</para></summary>
    private bool LoadIsOffered(Core.Satchel.Item item) =>
        _surface is not null && SentryHandLoad.IsLoadable(item.Kind);

    /// <summary>#837 · What the control says before it is pressed — the price known before the press
    /// (#696's discipline, applied to a magazine). With nothing within arm's reach it says so HERE, so the
    /// captain learns the reach rule without spending a press to find it out.</summary>
    private string LoadHint(Core.Satchel.Item item) =>
        !SentryHandLoad.IsLoadable(item.Kind) ? SentryHandLoad.NothingInReachLine
        : GunsWithinReach().Count == 0 ? SentryHandLoad.NothingInReachLine
        : SentryHandLoad.LoadHintLine;

    /// <summary>#837 · OPEN THE CHOOSER on one stack of rounds. It raises no card and takes no tab: the
    /// pocket is where the rounds are, and the page they are already looking at is where the choice
    /// belongs.</summary>
    private void OpenTheLoadChooser(Core.Satchel.Item item)
    {
        if (_surface is null || !SentryHandLoad.IsLoadable(item.Kind))
        {
            return;
        }

        _loadFrom = item;
        _loadShare.Clear();
        _satchelOutcome = null;
        _walletOpen = false;
    }

    /// <summary>#837 · …and shut it again, back to the pocket the rounds came from. Never closes the
    /// satchel: you open a chooser in order to keep going (#614's ruling one control over).</summary>
    private void CloseTheLoadChooser()
    {
        _loadFrom = null;
        _loadShare.Clear();
    }

    /// <summary>
    /// #837 · THE PAGE, or null when no chooser is open.
    ///
    /// <para>Asked fresh on every draw and nothing about it is frozen — not the pocket, not the guns, not
    /// the reach. The deck keys are live under an open satchel, so a captain can walk away from a set-down
    /// sentry mid-picker; a stored list would be the page saying one thing while the sim did another, three
    /// du down the corridor (the lesson #828 wrote down about bins, with a gun in it).</para>
    /// </summary>
    private (Core.Satchel.Item Rounds, int Pocket, string KindName, List<SentryHandLoad.Pick> Picks)?
        TheLoadChooser()
    {
        if (_loadFrom is not { } rounds || _surface is null)
        {
            return null;
        }

        int pocket = Core.Satchel.CountOf(_satchel, rounds.Kind, rounds.Id);
        if (pocket <= 0)
        {
            // The last of them went in. Nothing to choose between, and a page offering to spend an empty
            // pocket is a control that can only refuse.
            return null;
        }

        var picks = new List<SentryHandLoad.Pick>();
        foreach (SurfaceBot gun in GunsWithinReach())
        {
            picks.Add(SentryHandLoad.Offered(
                new SentryHandLoad.Aim(gun.Unit, gun.Rounds, gun.AmmoId, gun.Deployed, InReach: true),
                pocket,
                rounds.Id,
                _loadShare.TryGetValue(gun.Unit, out int wanted) ? wanted : null));
        }

        return (rounds, pocket, Ammunition.ById(rounds.Id).Name, picks);
    }

    /// <summary>
    /// #837 · THE GUNS THIS CAPTAIN CAN HAND ROUNDS TO, in the order they are offered.
    ///
    /// <para><b>The sling leads.</b> A bot riding your own back is always in reach and is the one the owner
    /// was looking at when he filed this; the ones holding a line follow, nearest first, because that is the
    /// order a captain would reach them in.</para>
    ///
    /// <para><b>The tube's own gun is not on this list.</b> GATE-1 hangs in the shuttle door, is topped back
    /// up after every volley and never runs dry — it is the boat's fixture and it is not counted against
    /// your sling anywhere else either (it is the one thing <see cref="SentryBot.MagazinesReadout"/> skips).
    /// A row for it would be a count the instrument does not have, which is precisely the second arithmetic
    /// this issue forbids.</para>
    /// </summary>
    private List<SurfaceBot> GunsWithinReach()
    {
        var reachable = new List<SurfaceBot>();
        if (_surface is not { } ex)
        {
            return reachable;
        }

        foreach (SurfaceBot gun in ex.Bots)
        {
            if (!SurfaceArrival.IsDoorSentry(gun.Unit) && !gun.Deployed && WithinHandsOf(gun))
            {
                reachable.Add(gun);
            }
        }

        var standing = new List<SurfaceBot>();
        foreach (SurfaceBot gun in ex.Bots)
        {
            if (!SurfaceArrival.IsDoorSentry(gun.Unit) && gun.Deployed && WithinHandsOf(gun))
            {
                standing.Add(gun);
            }
        }
        standing.Sort((a, b) => HowFarOff(a).CompareTo(HowFarOff(b)));

        reachable.AddRange(standing);
        return reachable;
    }

    private double HowFarOff(SurfaceBot gun)
    {
        double dx = gun.X - _avatarX, dy = gun.Y - _avatarY;
        return (dx * dx) + (dy * dy);
    }

    /// <summary>#837 · THE STEPPER. One notch either way, clamped to the row's own live ceiling — which is
    /// <see cref="SentryHandLoad.Ceiling"/>, which is <see cref="SentryHandLoad.Offer"/>, so the number under
    /// the captain's thumb can never be a number the drum would not have taken.
    ///
    /// <para>A row with a ceiling of zero has no stepper drawn and this no-ops: there is no interesting
    /// difference between nought and nought, and the row's business is its refusal.</para></summary>
    private void NudgeShare(SentryHandLoad.Pick pick, int by)
    {
        if (pick.Ceiling <= 0)
        {
            return;
        }
        _loadShare[pick.Unit] = Math.Clamp(pick.Share + by, 1, pick.Ceiling);
    }

    /// <summary>
    /// #837 · THE PRESS — and it is #803's act, reached by a second door.
    ///
    /// <para>Everything above this line is a page: rows, an order, and words asked of Core. This method adds
    /// a unit and a count to <c>TheRoundsGoInByHand</c> and then gets out of the way. It deliberately does
    /// not touch <c>_satchel</c>, the magazine or the vault — the day it does, the two grips are two features
    /// wearing one name, and the guard that drives both doors goes red on the difference.</para>
    ///
    /// <para>The chooser STAYS OPEN while there are rounds left, because that is what the owner's 8/4 is: a
    /// press into one gun, and then the rest into the other, with every figure on the page redrawn off the
    /// pocket that is actually left. It shuts itself when the pocket is empty, since there is nothing further
    /// to choose between.</para>
    /// </summary>
    private void LoadIntoIt(SentryHandLoad.Pick pick)
    {
        if (_loadFrom is not { } rounds || _surface is not { } ex)
        {
            return;
        }

        SatchelTry.Outcome outcome = TheRoundsGoInByHand(ex, rounds, pick.Unit, pick.Share);

        // Said inside the dialog, because the dialog is up: a line pulsed to the HUD from under an open
        // satchel is in the DOM and not on the screen (#680, the owner in caps).
        SayItWhereTheyAreLooking(outcome.Line);

        _loadShare.Remove(pick.Unit);
        if (Core.Satchel.CountOf(_satchel, rounds.Kind, rounds.Id) <= 0)
        {
            CloseTheLoadChooser();
        }
    }
}
