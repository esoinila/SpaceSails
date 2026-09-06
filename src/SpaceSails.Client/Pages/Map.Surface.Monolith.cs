using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — #586/#649 · THE MONOLITH
// DWELL, the ground's one strange thing and the only tenant of the Old Ones' file that is not an Old One.
// Nothing is watching to see you arrive; it is watching to see whether you STAY. Three gates, all of them
// Core's: the place (the monolith's own ground, and inside its sight), the window, and the dwell — and
// `SeesMonolith`/`DistanceToAnchorSquared` are the two questions the nerve asks about the slab, from the
// same point, so the sight beat and the arrival beat can never disagree about where the thing is.
public partial class Map
{
    // #586 · IN SIGHT OF THE MONOLITH — and only where the monolith actually IS.
    //
    // This used to be pure distance to MoonSurface.AnchorX/Y, which is the DEEP ANCHOR of every ground
    // there is: every seeded site puts its own fixture there (Luna's mass-driver muzzle, a plinth
    // elsewhere), so walking up to any of them fired the once-in-a-life Lovecraftian hit — 24 nerve, the
    // line "👁 The monolith resolves out of the dark", and the FirstMonolith selfie against the monolith
    // plate — over a broken launch machine. And _monolithSeen is kept FOR LIFE, so the captain who did
    // that could never be shown the real slab's beat again. Constant governing the wrong thing, and the
    // sentence disagreeing with the sim, in one line.
    //
    // Monolith.StandsOn is the same predicate the renderer builds the slab's card from, so the beat cannot
    // drift from the object again.
    /// <summary>#649 · THE DWELL, AND THE ONE STRANGE THING.
    ///
    /// <para>Three gates, all of them Core's (<see cref="MonolithWatch"/>): the PLACE (the monolith's own
    /// ground, and inside its sight), the WINDOW (about one visit-window in three is attentive, on the same
    /// slow clock the foot-offerings use, so it holds still for a whole excursion), and the DWELL — you have
    /// to STAY. Nothing is watching to see you arrive. It is watching to see whether you stand there.</para>
    ///
    /// <para>Walking out of sight resets the clock, which is the difference between standing at a thing and
    /// passing it. Once per excursion at most, and the beat costs the captain nothing —
    /// <see cref="MonolithWatch.NerveCost"/> carries the reasoning and is the one number to change.</para>
    ///
    /// <para>Deliberately NOT a story card or a plate. The picture idiom (#528) is the right instrument for
    /// almost everything and the wrong one here: a frame around a thing says THIS IS A THING, and the whole
    /// ruling is that anything happening near this stone stays deniable.</para></summary>
    private void StepMonolithWatch(double dtRealSeconds)
    {
        if (_surface is not { } ex || !MonolithWatch.CanHappenOn(ex.Stop.Body.Id, ex.Site.LayoutSalt))
        {
            return;
        }

        if (!SeesMonolith())
        {
            ex.MonolithDwellSeconds = 0;   // you walked away; standing at a thing is not passing it
            return;
        }

        ex.MonolithDwellSeconds += dtRealSeconds;

        // ?watchers=1 — the beat is rare BY DESIGN (one window in three, then forty seconds of standing
        // still), which makes it the exact shape of scene Map.Sim's own rule is about: "a scene nobody can
        // reach on demand is a scene that ships broken." The cheat opens the window and shortens the dwell;
        // it does not change what happens, so what a tester sees is what a captain sees.
        double dwell = _watchersCheat ? MonolithWatch.DwellSeconds * 0.05 : MonolithWatch.DwellSeconds;
        if (ex.MonolithWatchSpent || ex.MonolithDwellSeconds < dwell)
        {
            return;
        }

        long epoch = Monolith.EpochAt(SimTime);
        if (!_watchersCheat && !MonolithWatch.AttentiveIn(ex.Stop.Body.Id, ex.Site.LayoutSalt, epoch))
        {
            ex.MonolithWatchSpent = true;   // this window is not one of them; do not keep asking
            return;
        }

        ex.MonolithWatchSpent = true;
        MonolithWatch.What what = MonolithWatch.Which(
            ex.Stop.Body.Id, ex.Site.LayoutSalt, epoch, packOnTheField: _reevers.Count > 0);
        ShowAndFile(MonolithWatch.Line(what), MonolithWatch.Glyph);

        // NerveCost is 0.0 and the call is left in on purpose: the number is a feel call the owner may want
        // to make, and a call site that has to be re-found is a decision that quietly never gets made.
        if (MonolithWatch.NerveCost > 0)
        {
            ApplyNerveShock(MonolithWatch.NerveCost, "something out here was paying attention");
        }
    }

    /// <summary>How far the captain is from the deep anchor, squared. One expression, because the sight beat
    /// and the arrival beat must measure from the same point or they can disagree about where the thing
    /// is.</summary>
    private double DistanceToAnchorSquared()
    {
        double dx = _avatarX - MoonSurface.AnchorX;
        double dy = _avatarY - MoonSurface.AnchorY;
        return (dx * dx) + (dy * dy);
    }

    private bool SeesMonolith()
    {
        if (_surface is not { } ex || !Monolith.StandsOn(ex.Stop.Body.Id, ex.Site.LayoutSalt))
        {
            return false;
        }
        return DistanceToAnchorSquared() <= Monolith.SightRangeDu * Monolith.SightRangeDu;
    }
}
