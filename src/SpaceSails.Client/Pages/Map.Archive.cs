using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// THE ARCHIVE NODE — the client lane. See <c>docs/features/the-archive-node.md</c>; the rules are all in
/// <see cref="ArchiveNode"/> and nothing here decides anything the pure file could have decided.
///
/// <para>Three layers, and the shape of this file is the shape of them:</para>
/// <list type="number">
/// <item><b>The dwell.</b> A field you cross, in a compartment with something you want. No prompt, no
/// dialog — the pip row ticks while you decide whether you are finished in this room. The captain sets
/// their own dose, and the interesting decision is the one the game does not ask you to make.</item>
/// <item><b>The confrontation.</b> Arm's length forces a throw (or [E] does). Visible arithmetic, a band, a
/// vision, a bill in whole pips.</item>
/// <item><b>The switch.</b> An honest legend, drawn on the deck, with nothing in front of it.</item>
/// </list>
///
/// <para><b>The one thing to be careful about when editing this file:</b> the reason the column and the
/// handle are two consoles 3.5 du apart is that a captain must be able to pull the handle WITHOUT paying a
/// throw, and never find out what they did. Put the handle on the confrontation card and the whole joke is
/// gone. <c>TheArchiveNodeIsOnTheDeckTests</c> walks the route and fails if that ever stops being true.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>This hull is carrying one. Decided once, on boarding, by Core off the wreck's own id.</summary>
    private bool _archiveAboard;

    /// <summary>The handle has been pulled on this hull.</summary>
    private bool _archivePurged;

    /// <summary>How many times this captain has already faced THIS node. Feeds the throw (every look makes
    /// the next one worse) AND the vision pool (a captain works down it), so it is the one counter the whole
    /// encounter is built on.</summary>
    private int _archiveConfrontations;

    /// <summary>Whether the "something down here is still warm" beat has been spoken on this boarding. The
    /// fiction arrives ONE BEAT EARLY — before the pips move, before anything is touched.</summary>
    private bool _archiveFieldNoticed;

    /// <summary>Whether the captain was at arm's length last frame. Crossing IN is what forces the throw;
    /// standing there must not force another one every frame, and walking back out re-arms it.</summary>
    private bool _archiveAtArmsLength;

    /// <summary>The open card — a band, its arithmetic, and whatever the archive gave back.</summary>
    private ArchiveCard? _archiveCard;

    /// <summary>What one look produced. Presentation only: every string on it came from Core.</summary>
    /// <param name="Collar">What the jar's collar reads — present ONLY on the bands that got close enough to
    /// read it (<see cref="ArchiveNode.ReadsTheCollar"/>). The price of knowing whose pattern is in there is
    /// a bad throw, never a good one.</param>
    private readonly record struct ArchiveCard(
        string Title, string Art, string BandLine, string Lore, string Dice, string? Collar);

    private void CloseArchiveCard() => _archiveCard = null;

    // ── Boarding: is there one aboard her? ────────────────────────────────────────────────────────────

    /// <summary>Called once per boarding, beside <c>PrepareVenting</c>. Core owns the answer — seeded off her
    /// id, so a reload finds the same ship and a rumour about a hull is worth something.</summary>
    private void PrepareArchiveNode(in Derelict.Wreck wreck)
    {
        _archiveAboard = _archiveCheat || ArchiveNode.IsAboard(wreck.Id, wreck.Cause);
        _archivePurged = false;
        _archiveConfrontations = 0;
        _archiveFieldNoticed = false;
        _archiveAtArmsLength = false;
        _archiveCard = null;
    }

    /// <summary>Set by <c>?archive=1</c>. It does NOT invent a node on an ineligible hull — it boots you onto
    /// the one cause that always carries one (<see cref="ArchiveCheatWreck"/>), and this flag only says "and
    /// do not let the seeded 1-in-3 take it away from me if the base branch ever widens the rule."</summary>
    private bool _archiveCheat;

    /// <summary>THE HULL <c>?archive=1</c> BOOTS YOU ONTO — the ship one of her own opened to space, which is
    /// the one cause where Core guarantees a node because the node is why she died.
    ///
    /// <para>One function, called by the query parser AND by the guard, so the cheat cannot promise a scene
    /// the world does not seed. "A scene nobody can reach on demand is a scene that ships broken" is only
    /// true if the demand actually works.</para></summary>
    public static Derelict.Wreck ArchiveCheatWreck() => CheatWreck(ArchiveCheatCause);

    /// <summary>The cause <c>?archive=1</c> forces. Named rather than inlined so the guard reads the same
    /// constant the parser does.</summary>
    public const Derelict.WreckCause ArchiveCheatCause = Derelict.WreckCause.VentedByOneOfTheirOwn;

    // ── Layer 1: the dwell ────────────────────────────────────────────────────────────────────────────

    /// <summary>Is the captain standing in the field this frame? Read by the nerve step, which is the only
    /// thing that spends anything — this answers a question and never has a side effect.</summary>
    private bool InArchiveField =>
        _archiveAboard && !_archivePurged && OnWreck
        && ArchiveNode.InField(_avatarX - WreckLayout.ArchiveStation.X,
                               _avatarY - WreckLayout.ArchiveStation.Y);

    /// <summary>The two edges the walk loop cares about: the first crossing INTO the field (which is where
    /// the fiction arrives, one beat before the first pip) and the crossing into arm's length (which is
    /// where the throw is forced, because the design says approaching it is enough — you do not have to
    /// press anything to be looked at).</summary>
    private void StepArchiveNode()
    {
        if (!_archiveAboard || _archivePurged || !OnWreck)
        {
            _archiveAtArmsLength = false;
            return;
        }

        double dx = _avatarX - WreckLayout.ArchiveStation.X;
        double dy = _avatarY - WreckLayout.ArchiveStation.Y;

        if (!ArchiveNode.InField(dx, dy))
        {
            _archiveAtArmsLength = false;   // leaving re-arms the approach
            return;
        }

        if (!_archiveFieldNoticed)
        {
            _archiveFieldNoticed = true;
            ShowPulseMessage(ArchiveNode.FieldEntryLine);
        }

        bool close = ArchiveNode.AtArmsLength(dx, dy);
        if (close && !_archiveAtArmsLength)
        {
            ConfrontArchiveNode();
        }
        _archiveAtArmsLength = close;
    }

    // ── Layer 2: the confrontation ────────────────────────────────────────────────────────────────────

    /// <summary>Look at it. Forced by walking to arm's length, or asked for with [E] at the column.</summary>
    private void ConfrontArchiveNode()
    {
        if (_wreck is not { } w || !_archiveAboard)
        {
            return;
        }
        if (_archivePurged)
        {
            ShowPulseMessage("❄ " + ArchiveNode.ColdColumnLine);
            return;
        }

        var facts = new ArchiveNode.Confrontation(
            PipsNow: NervePips.PipsOf(_nerve),
            // An away team crosses to a derelict in suits — that is why the excursion has a tank at all.
            Suited: true,
            PriorConfrontations: _archiveConfrontations,
            // Understanding is the injury: the more of arc 2 the captain has assembled, the more of what
            // they are looking at they are equipped to recognise.
            NebulaShardsHeld: _nebula.Count,
            HasEverDied: HasEverDiedThisThread);

        (DiceRoll roll, ArchiveNode.Band band, ArchiveNode.Vision vision) =
            ArchiveNode.Confront(w.Id, facts);

        // The counter goes up BEFORE anything is spent, so a look that costs you the last of your nerve
        // still counts as a look — and so the next throw is the one the modifier table promises.
        _archiveConfrontations++;

        // The bill, in whole pips, named. Nothing may move the gauge anonymously (#480's law), and the
        // ledger line has to be the one the card just claimed.
        ApplyNerveShock(ArchiveNode.PipCost(band) * NervePips.PipUnit, ArchiveBillLine(band));

        _archiveCard = new ArchiveCard(
            Title: vision.Title,
            Art: vision.ArtFile,
            BandLine: ArchiveNode.BandLine(band),
            Lore: vision.Lore,
            Dice: roll.Describe(),
            Collar: ArchiveNode.ReadsTheCollar(band)
                ? ArchiveNode.CollarLine(ArchiveNode.ResidentOf(w.Id))
                : null);

        // THE TRADE THE FEATURE EXISTS FOR: nerve into arc-2 knowledge. Silent — the card is the delivery,
        // and a pulse would fire behind it. See ArchiveShard for which visions carry a shard and why.
        if (band != ArchiveNode.Band.LookedAway && ArchiveShard(vision.Id) is { } shardId)
        {
            AssembleNebulaSilently(shardId);
        }

        RendererInterop.PlayCue(band == ArchiveNode.Band.LookedAway ? "board" : "alarm");
        RequestVaultSave();   // the nerve moved, and maybe a shard landed
    }

    /// <summary>What the ledger says the pips went on. One line per band, and each one describes what
    /// actually happened rather than "archive node −3": the gauge and the sentence are the same event.</summary>
    private static string ArchiveBillLine(ArchiveNode.Band band) => band switch
    {
        ArchiveNode.Band.LookedAway => "you nearly looked at the thing in the hold",
        ArchiveNode.Band.Saw => "you looked at the thing in the hold",
        ArchiveNode.Band.Noticed => "the thing in the hold looked back",
        _ => "you were inside the thing in the hold",
    };

    /// <summary>
    /// WHICH ARC-2 SHARD A VISION HANDS OVER — the node's whole reason to exist as a delivery system, per
    /// the feature doc §6: <i>"fragments … gain a route that does not depend on being in a bar."</i>
    ///
    /// <para>Two of the five, and only two. The pool in <see cref="NebulaLore"/> is NOT extended — adding
    /// entries would move <see cref="NebulaLore.IntelNeededToUnlock"/>'s arithmetic under a shipped arc — so
    /// the node is a second ROUTE to existing shards rather than a sixth clause. Which two is decided by
    /// what the picture is evidence FOR:</para>
    /// <list type="bullet">
    /// <item><c>archive-rows</c> → <c>fine-print</c>. The poster's grey line says the thing they keep is the
    /// PATTERN. This is the warehouse it is kept in, and the captain has now been in it.</item>
    /// <item><c>archive-intake</c> → <c>clinic-ledger</c>. Two patterns come off a subscriber who lay down
    /// once; one is filed and one is billed. The second page of the bill is that arithmetic, from the
    /// clerk's side.</item>
    /// </list>
    ///
    /// <para>And three that hand over nothing, deliberately. <c>archive-handlers</c> is the third-arc seed —
    /// a question planted in a mechanic, and a shard would file it as an answer. <c>archive-wintering</c> is
    /// the KAAMOS rhyme, delivered as a picture rather than as a clause. <c>archive-your-rack</c> is not lore
    /// about the corporation at all; it is the player's own save looking back, and there is no ledger entry
    /// that would not cheapen it.</para>
    /// </summary>
    private static string? ArchiveShard(string visionId) => visionId switch
    {
        "archive-rows" => "fine-print",
        "archive-intake" => "clinic-ledger",
        _ => null,
    };

    /// <summary>Has the insurance ever paid out on this thread? The archive has something of yours on file
    /// only once it has been asked for a withdrawal — which is what gates the last vision and what softens
    /// the throw for a captain it has never had one from.</summary>
    private bool HasEverDiedThisThread =>
        (ActiveThreadInfo?.Retired.Count ?? 0) > 0 || _rebirthsSeen > 0;

    // ── Layer 3: the switch ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// PULL IT. No confirmation, no restatement, no softening — <see cref="ArchiveNode.SwitchLegend"/> is
    /// stencilled on the housing and drawn on the deck, and the label IS the confirmation dialog. If this
    /// method ever grows an "are you sure?", the feature is gone.
    ///
    /// <para>And note what does NOT happen here: nothing is said about whose pattern it was. A captain who
    /// did not pay to read the collar never learns, and the record of what they did is the silence
    /// afterwards. The delinquent's collector losing its file is real and is not announced either — the
    /// heat gauge simply reads lower the next time anyone looks at it. That is the whole shape.</para>
    /// </summary>
    private void PullArchiveSwitch()
    {
        if (_wreck is not { } w || !_archiveAboard || _archivePurged)
        {
            return;
        }

        _archivePurged = true;

        // Relief is real and that is the trap.
        ApplyNerveRelief(ArchiveNode.PurgeRelief * NervePips.PipUnit);

        // A lapsed subscriber's collector has nothing left to recover — a real payment for the reckless
        // road, so the handle is not merely a punishment. Silently: see the summary.
        if (ArchiveNode.ResidentOf(w.Id) == ArchiveNode.Resident.DelinquentSubscriber)
        {
            _heat = EncounterRule.RaiseHeat(_heat, -PurgeHeatCleared, SimTime);
        }

        ShowPulseMessage("⏻ " + ArchiveNode.PurgeLine);
        RendererInterop.PlayCue("board");

        // #528 · The one irreversible act in the game, and it was a toast. A plate is not a confirmation
        // dialog — it fires AFTER the handle has gone over, which is the one place a picture can be as quiet
        // as this feature needs and still exist. The four visions each got a painting on the way in; the
        // moment the noise stops had nothing. Core owns the words, and they name nobody, as always.
        ShowRevealCard(
            ArchiveNode.PurgedPlate.Title,
            ArchiveNode.PurgedPlate.ArtFile,
            ArchiveNode.PurgedPlate.Caption);
        RebuildWreckDeck();   // the legend still reads, and now it reads "pulled"
        RequestVaultSave();
    }

    /// <summary>How much of a heat thread a purged delinquent takes with it. One level — the same unit the
    /// rest of the heat lane raises in, so it is a real thread going away and not a jackpot. FLAGGED for the
    /// owner's tuning.</summary>
    private const int PurgeHeatCleared = 1;
}
