using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// Subject: A LEAF SOMETHING IS HAULING OPEN BY HAND (part of DeckPlan).
//
// #563 · THE OLD ONES USE DOORS (owner ruling, 2026-09-06 — see Core ReeverDoor and worldbuilding-notes
// §10). An unlocked leaf opens for them over a beat, and it stays open behind them.
//
// WHY THE STATE IS HERE, ON THE PLAN, AND NOT IN THE REEVER LOOP THAT DRIVES IT. Map.Surface.Reevers.cs
// carries a warning written by whoever tried this before, in the body of IsDoorShut, and it is the whole
// reason for this file:
//
//     "I briefly opened doors here for Reevers too … and it broke the invariant this method exists to
//      hold: the RENDERER decides a door is open from the CAPTAIN's distance and nothing else. Adding a
//      second opener here made the sim treat a door as open while the deck drew it shut, so a gun fired
//      through a door the player could see was closed. … If Reevers are ever to work doors, the RENDERER
//      has to learn it at the same moment — one source of truth or none."
//
// This is that one source. The sim writes it; the sight/round list (IsDoorShut) and the pen (DrawTheDoors)
// both read it; neither owns it. There is no second answer anywhere for what stands open.
//
// IT IS NOT SAVED, AND THAT IS DELIBERATE. It lives exactly as long as the plan does, which is exactly as
// long as the pack does — a Reever's position has never been saved either (ReeverChase: "movement is
// real-time client cosmetic"). A leaf hauled open by something that is not there any more, on a deck
// rebuilt from the seed, would be a fact about nobody.
public sealed partial class DeckPlan
{
    // Parallel to Doors, grown with it. Doors only ever APPEND (DeckPlan.Regions AppendRegion — "existing
    // entries keep their indices"), so an index taken this frame means the same leaf next frame, and a
    // welded tile arrives with its own leafs shut, which is what a tile a hundred du away should be.
    private double[] _leafHauled = [];   // seconds of hauling banked on this leaf, this hold
    private double[] _leafBeat = [];     // the beat it is being hauled against (ReeverDoor.HaulSeconds)
    private bool[] _leafOpened = [];     // …and the ones that are all the way over, for good

    // #563 · …AND THE ONES THAT ARE NOT DOORS ANY MORE. Owner ruling, 2026-09-13: a locked door is TIME,
    // never a key — and one of the times you may spend is a round. A shot leaf is terminal: passable and
    // transparent forever, never shut, never locked, never hauled. It lives HERE for the reason every word
    // of this file's header gives: the sim writes it, the sight list and the pen both read it, and there is
    // no second answer anywhere for what stands open. A destroyed leaf drawn shut would be exactly the bug
    // #465, #1099 and #1154 were each paid for.
    private bool[] _leafShot = [];

    private void FitLeafState()
    {
        if (_leafHauled.Length == Doors.Length)
        {
            return;
        }
        System.Array.Resize(ref _leafHauled, Doors.Length);
        System.Array.Resize(ref _leafBeat, Doors.Length);
        System.Array.Resize(ref _leafOpened, Doors.Length);
        System.Array.Resize(ref _leafShot, Doors.Length);
    }

    /// <summary>#563 · Has somebody put a round through this lock? Terminal, and the one thing on a leaf that
    /// nothing walks back.</summary>
    public bool LeafIsShot(int index)
    {
        FitLeafState();
        return (uint)index < (uint)_leafShot.Length && _leafShot[index];
    }

    /// <summary>#563 · <b>Shoot the lock.</b> The leaf is gone as a leaf: it counts as held open from this
    /// instant, so the same frame's legs, sight, rounds and pen all read a doorway with nothing in it.
    /// Returns false when there was nothing to shoot — a leaf already destroyed — so a caller cannot spend a
    /// second round on the same hole.</summary>
    public bool ShootTheLeaf(int index)
    {
        FitLeafState();
        if ((uint)index >= (uint)_leafShot.Length || _leafShot[index])
        {
            return false;
        }
        _leafShot[index] = true;
        _leafOpened[index] = true;   // held open for good, by the one mechanism that already means that
        _leafHauled[index] = 0;
        return true;
    }

    /// <summary>Has something hauled this leaf all the way over? It stays over: <b>they do not close doors
    /// behind them</b>, which is the tell — a leaf standing open on an empty doorway, with nothing near it,
    /// is the only sentence this feature ever says.</summary>
    public bool LeafHeldOpen(int index)
    {
        FitLeafState();
        return (uint)index < (uint)_leafOpened.Length && _leafOpened[index];
    }

    /// <summary>How far this leaf has slid, 0 (shut) to 1 (open) — for the pen, which is the half of the
    /// beat the player is meant to see: the leaf moving before anything comes through it.</summary>
    public double LeafOpening(int index)
    {
        FitLeafState();
        if ((uint)index >= (uint)_leafOpened.Length)
        {
            return 0;
        }
        return _leafOpened[index] ? 1 : ReeverDoor.Opening(_leafHauled[index], _leafBeat[index]);
    }

    /// <summary>Something is hauling this leaf this frame: bank <paramref name="dtSeconds"/> against
    /// <paramref name="beatSeconds"/>, and when the beat is spent the leaf is over for good.</summary>
    public void HaulLeaf(int index, double dtSeconds, double beatSeconds)
    {
        FitLeafState();
        if ((uint)index >= (uint)_leafOpened.Length || _leafOpened[index])
        {
            return;
        }
        _leafBeat[index] = beatSeconds;
        _leafHauled[index] += dtSeconds > 0 ? dtSeconds : 0;
        if (ReeverDoor.Opened(_leafHauled[index], beatSeconds))
        {
            _leafOpened[index] = true;
        }
    }

    /// <summary>Nobody is at this leaf this frame — it slides back. Half a beat is not banked for later:
    /// the beat is the time something spends with its hands on the door, and a thing that walked off did
    /// not spend it. (A leaf already over is not affected; nothing shuts one.)</summary>
    public void LetGoOfLeaf(int index)
    {
        FitLeafState();
        if ((uint)index < (uint)_leafOpened.Length)
        {
            _leafHauled[index] = 0;
        }
    }
}
