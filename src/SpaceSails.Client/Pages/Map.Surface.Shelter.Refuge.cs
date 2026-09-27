using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE SHELTER'S FIXTURES AND THE HIVE'S REFUGE — the locker, the tank, and the refuge lit and dark.
///
/// <para>Split out of <c>Map.Surface.Shelter.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening
/// file.</para>
/// </summary>
public partial class Map
{
    // ── #573 · THE SHELTER'S EMERGENCY LOCKER [E]. Owner, on Andy Weir's bubble shelters: they "should also
    //    contain reload to guns". A shelter stocked with air and nothing else is a tap, not a refuge. ──
    private void ShelterLockerInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.ShelterLocker })
        {
            return;
        }
        if (!ShelterUnderfoot(ex).Found)
        {
            return;
        }

        // #728 · TWO DIFFERENT NOTHINGS, and they were sharing one sentence. "Your magazines are full" is
        // true of a captain carrying two topped-up sentries and a lie to a captain carrying none — and it was
        // the second one the press answered most often, because the press is the reason you walked here.
        //
        // Asked of Core, in the same words the HUD's readout is asked (SentryBot.AnythingToFill), and NOT of
        // ex.Bots.Count — because the tube's own GATE-1 rides that list, is permanently full, and would
        // therefore have answered "everything is full" on behalf of a captain who owns nothing at all. Two
        // places deciding the same fact is how this bug got here the first time.
        if (!SentryBot.AnythingToFill(TheSlingAsTheInstrumentReadsIt(ex)))
        {
            ShowPulseMessage(SurfaceShelter.LockerNothingToFillLine);
            return;
        }

        var takers = ex.Bots.Where(b => b.Rounds < SentryBot.MaxMagazine).ToList();
        if (takers.Count == 0)
        {
            ShowPulseMessage(SurfaceShelter.LockerFullLine);
            return;
        }

        // #580 · EVERY MAGAZINE, EVERY TIME, FOR AS LONG AS YOU CARE TO STAND HERE. Owner: "we want in
        // practise unlimited reloads of rounds at the shelters not like couple mags". No drawer, no
        // reservoir, no cooldown — the press is the point of the building. What this costs is the walk here
        // and the air it took, which is where the pressure in an excursion is supposed to live.
        int loaded = 0;
        foreach (SurfaceBot bot in takers)
        {
            loaded += SentryBot.MaxMagazine - bot.Rounds;
            bot.Rounds = SentryBot.MaxMagazine;
        }

        RendererInterop.PlayCue("board");
        ShowPulseMessage(SurfaceShelter.LockerLine(loaded));
        RequestVaultSave();
    }

    private void ShelterTankInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.ShelterTank })
        {
            return;
        }

        // #573 · The rack is not a button any more — it pumps on its own for as long as a captain stands in
        // the shelter (StepSuitAir), because the owner is right that the PUMPING TIME is the honest cost:
        // "the time it takes to pump air is good incentive to not take too much". So [E] reads the gauge
        // rather than working a lever. An affordance that did nothing would be worse than none (#212), so it
        // tells you what the machine is doing and lets you decide how long to stand there.
        ShelterSpot which = ShelterUnderfoot(ex);
        if (!which.Found)
        {
            ShowPulseMessage("🫁 The rack's fitting is inside. Step in out of the vacuum.");
            return;
        }

        double held = ShelterReservoirNow(ex, which);
        ShowPulseMessage(RackGaugeLine(ex, held));
    }

    // ── #608 · THE REFUGE'S RACK [E]. The same gauge, in a poured room under a moon. ─────────────────────
    //
    // Owner: "Still for safety there would need to be a couple of places with air lock and air refilling,
    // because otherwise the elevator being busy could kill employees, and those honest criminal scientists
    // are hard to recruit :-D"
    //
    // It reads the machine rather than working a lever, for the identical reason the shelter's does: the
    // rack pumps on its own for as long as you stand in the air (StepSuitAir), and the PUMPING TIME is the
    // honest cost. An affordance that did nothing would be worse than none (#212), so [E] says what the
    // machine is doing and leaves the captain to decide how long they dare stand there — which, on a dead
    // floor with the lift several rooms away, is a much sharper decision than it is on the regolith.
    private void HiveRefugeInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.HiveRefuge })
        {
            return;
        }

        // #1149 · THE PAPER ON THE VALVE COMES FIRST, ONCE. Every refuge in the building carries an
        // inspection tag and the press that reads the rack is the press that takes it — a second console at
        // the same centre would be a second thing to walk to and would crowd the one the fan points at.
        if (TryTheInspectionTag(ex))
        {
            return;
        }

        // #619 · THERE IS NO DEAD BRANCH HERE ANY MORE. This used to answer a FAILED seal with a line at the
        // rack, which was the shape of the thing while a floor's one refuge could be the room that failed.
        // It cannot now: the welded room is a different console kind at a different spot, with its own press
        // (HiveRefugeDarkInteract), and every room that carries THIS kind is a room whose door cycles.
        //
        // #1149 · EMPTY was already off that branch and answers on the gauge below, because an empty rack is
        // not a rack with nothing behind it — it is a working cracker somebody drew right down, and
        // RackGaugeLine's own trickle line is exactly and already the sentence for that.
        int which = RefugeUnderfoot(ex);
        if (which < 0)
        {
            ShowPulseMessage("🫁 The rack's fitting is through the inner door. Step into the air.");
            return;
        }
        ShowPulseMessage(RackGaugeLine(ex, RefugeReservoirNow(ex, which)));
    }

    // ── #619 · THE REFUGE THAT FAILED [E]. A welded door, and a card. ────────────────────────────────────
    //
    // Owner, filing it: "a SECOND refuge, on one floor, that failed — that is the story. Not a dice roll on
    // every refuge. One, placed, deliberate, and never the only one on its floor, so it can never kill
    // anybody who trusted the instrument." And the 2026-09-06 ruling that says how it is told: "If for
    // dramatic suspense we need one that does not work, that is narrated, with a gen-AI image."
    //
    // So the press does one thing and then stops doing it. There is no gauge to read, no rack to stand at
    // and nothing to decide — the room is shut and it stays shut — and the whole of the telling is the card
    // (#761) plus the line the field book keeps, filed under the PLACE (#741) so it is still readable a week
    // later, which is the entire complaint #587 was filed about.
    //
    // ONCE PER CAPTAIN PER SITE is the beat's own cadence (StoryBeats.Cadence.OncePerSubject, raised with
    // the body id), not a flag kept here: a building has at most one of these, and a second press at the
    // same door is a captain reading the plate again.
    private void HiveRefugeDarkInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveRefugeDark })
        {
            return;
        }

        FileNoteAbout(
            UndergroundComplex.FailedRefugeNoteLine(ex.Stop.Body.Id, ex.Floor),
            "🫁",   // the refuge family's own mark, as every other refuge line in the book is filed
            UndergroundComplex.FailedRefugeSubjects(ex.Stop.Body.Id, ex.Floor));
        RaiseStoryBeat(StoryBeats.Beat.RefugeFailed, ex.Stop.Body.Id);
    }
}
