using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Surface.Tiles — #563 · THE TREADMILL, on the live deck.
//
// Owner, 2026-08-01: "suppose the player wants to just walk left... can we just not add a map tile there and
// take one out from right? ... it is a bit like this Virtual reality Carpet ... you can walk in any direction
// on it but actually you can stand still in the room you play in."
//
// Core owns the model (SurfaceTiles: which tile, what is on it, generated from its address and nothing else;
// SurfaceStream: which tiles are carried and when that changes). This file is the thin client half — it
// welds the carried tiles onto the live plan and asks the stream, once a frame, whether anything moved.
//
// THE ONE PERFORMANCE LAW, and it is the whole reason the stream exists: a rebuild happens when the captain
// crosses a TILE BOUNDARY, never when the captain takes a step. DeckPlan rebuilds SurfaceCollision.WallIndex
// whenever the walls change (#448, which exists because surface geometry cost once timed the shuttle ride
// out), so a naive per-step stream would rebuild a spatial index over a thousand segments sixty times a
// second. At the deck's 9 du/s a tile boundary comes round about every thirty-five seconds of walking.
public partial class Map
{
    /// <summary>Ask the stream whether the captain has walked onto new ground, and re-weld if so.
    ///
    /// <para>Called once per surface step. The answer is false on all but a handful of frames in an entire
    /// excursion, and on those frames it costs exactly one <see cref="RebuildSurfaceDeck"/> — the same
    /// rebuild a dig or a forced hatch already costs, which is the budget this was designed to fit inside
    /// rather than a new one.</para></summary>
    private void StepGroundStream(SurfaceExcursion ex)
    {
        if (!GroundIsALattice(ex))
        {
            return;
        }

        HoldAtTheBackstop(ex);

        if (ex.Stream.Step(_avatarX, _avatarY))
        {
            RebuildSurfaceDeck();
        }
    }

    /// <summary>Is this excursion standing on the open regolith? A derelict is a ship, a Hive floor is
    /// underground, and an away-expedition site is one authored ground that ends where it ends — none of them
    /// is a lattice, and none of them should grow one.</summary>
    private static bool GroundIsALattice(SurfaceExcursion ex) =>
        ex.Floor >= 0
        && !ex.Expedition
        && !ex.Deflection
        && !Derelict.TryParseWreckId(ex.Stop.Body.Id, out _);

    /// <summary>#563 law 7 · THE BACKSTOP, enforced.
    ///
    /// <para>Ten thousand eight hundred deck units from the tube — a full tank at a walking pace, so a captain
    /// who reaches it emptied the suit getting there and crossed the point of no return at less than half the
    /// distance. Nobody meets this. It exists so that "unbounded" is a design statement about the ground the
    /// player walks and not a promise the arithmetic has to keep for ever.</para>
    ///
    /// <para>Held rather than clamped hard: the captain is walked one step back toward the tube, which is the
    /// direction they must go anyway and the direction the suit has been telling them to go for some time.</para>
    ///
    /// <para><b>And it is not silent.</b> An invisible wall that says nothing is the failure #563 opened
    /// with, and one that explains a technical limit would be worse. So the suit says it, once, through the
    /// register it has said everything else about distance through since #325 — <see cref="ShowPulseMessage"/>
    /// carrying <see cref="SuitAir.BackstopRefusal"/>, the same channel as the point-of-no-return line and the
    /// reserve. No card, no overlay, no new instrument. The sentence is true at that distance whatever else is
    /// out there: the tank really does not reach the tube from here, and it has not for five thousand du.</para>
    ///
    /// <para>The boundary is asked exactly once per step (<see cref="SurfaceEdge.BackstopVoice"/>) and its one
    /// answer decides both the line and the hold — asking twice would be two chances to disagree.</para></summary>
    private void HoldAtTheBackstop(SurfaceExcursion ex)
    {
        SurfaceEdge.BackstopVoice.Refusal refused =
            ex.Backstop.Step(ex.Stop.Body.Id, ex.Site.LayoutSalt, _avatarX, _avatarY);
        if (refused.Line is { } line)
        {
            ShowPulseMessage(line);
        }
        if (!refused.Beyond)
        {
            return;
        }

        (double hx, double hy) = SurfaceTiles.TubeMouth();
        double dx = hx - _avatarX, dy = hy - _avatarY;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        if (len <= 1e-6)
        {
            return;
        }
        _avatarX += dx / len;
        _avatarY += dy / len;
    }

    /// <summary>Weld every carried tile other than the home one onto the freshly built plan.
    ///
    /// <para>The home tile is not here because it is not appended: it IS the base deck
    /// (<c>MoonSurface.SurfaceDeck</c>), byte for byte the ground this game has always laid, which is what
    /// keeps everything at the tube exactly where it was. What arrives through this door is ground that did
    /// not exist before — walls, the tile's own deep fixture, and its weather.</para>
    ///
    /// <para>Regenerated from the address every time rather than kept: that is the law the whole issue turns
    /// on (<see cref="SurfaceTiles"/>), and holding a cache here would be the one place it could quietly
    /// stop being true.</para></summary>
    private void ComposeTiles(SurfaceExcursion ex)
    {
        if (!GroundIsALattice(ex))
        {
            return;
        }

        // The stream has not been asked yet on the first build of an excursion — ask it now, so the very
        // first frame already stands in the middle of a full chunk rather than on a lone tile.
        if (ex.Stream.Loaded.Count == 0)
        {
            ex.Stream.Step(_avatarX, _avatarY);
        }

        _deckPlan.AppendRegion(TileRegion(ex.Stop.Body.Id, ex.Site.LayoutSalt, ex.Stream.Loaded));
    }

    /// <summary>EVERY CARRIED TILE'S GROUND AS ONE REGION — walls, weather, landmark labels, the doors hung
    /// in its buildings and the drawers still worth pressing.
    ///
    /// <para>Static and pure in <c>(body, salt, the addresses being carried)</c> on purpose. It is what the
    /// live deck is grown by, so it is also the only honest place to ASK what the lattice puts on the ground
    /// — a guard that reads the source can be fooled by a call that is present and does nothing, and a guard
    /// that stands up the whole page cannot be written at all. This one can simply be handed a chunk three
    /// tiles from the tube and asked what came back.</para>
    ///
    /// <para><b>The landmark singletons are deliberately absent.</b> The monolith and the hidden lab are laid
    /// by the home tile's build and stay there: #1058 made them facts about a BODY, and one per tile would
    /// not be an unbounded world — it would be a wallpaper of monoliths, out to the backstop.</para></summary>
    private static DeckPlan.DeckRegion TileRegion(
        string body, string salt, IReadOnlyList<SurfaceTiles.Address> loaded)
    {
        var walls = new List<DeckPlan.Wall>();
        var labels = new List<(float X, float Y, string Text)>();
        var scenery = new List<SurfaceScenery.Mark>();
        var doors = new List<DeckPlan.Door>();
        var consoles = new List<DeckPlan.ConsoleSpot>();

        foreach (SurfaceTiles.Address a in loaded)
        {
            if (a == SurfaceTiles.Home)
            {
                continue;
            }

            SurfaceLayout.Plan plan = SurfaceTiles.Ground(body, salt, a);
            foreach (SurfaceLayout.Wall w in plan.Walls)
            {
                // Rock, never pressure hull — the #563 ink ruling, and it applies out here for the same
                // reason it applies at the tube: nothing generated on a moon is spaceship.
                walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, false,
                    Unseen: w.Unseen, IsStone: w.IsHull));
            }
            foreach (SurfaceLayout.Landmark m in plan.Landmarks)
            {
                labels.Add(((float)m.X, (float)m.Y, m.Label));
            }
            scenery.AddRange(SurfaceTiles.Terrain(body, salt, a));

            // #563 slice 2 · AND WHAT IS IN THEM. Slice 1 welded a tile's WALLS and stopped there, so every
            // building more than one tile from the tube was a thick-walled room with an open gap where its
            // door belongs and nothing inside — word for word the report #573 was filed about ("there seemed
            // to be shelter like spaces that were just missing the services and the doors"), re-shipped one
            // tile out. The whole point of walking is that there is something out there: owner, on the
            // structure generator, "the idea is that we can then use those places to have supplies and clues
            // we can find on the way to somewhere."
            //
            // Both halves are the HOME TILE'S OWN QUESTION (SurfaceTiles.Doors / .Drawers, which the home
            // build also asks), never a second opinion about doors and drawers.
            foreach (SurfaceTiles.HungDoor d in SurfaceTiles.Doors(body, salt, a))
            {
                doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, Imported: d.Imported));
            }
            foreach (SurfaceTiles.Drawer drawer in SurfaceTiles.Drawers(body, salt, a))
            {
                consoles.Add(new(DeckPlan.ConsoleKind.RuinSalvage,
                    (float)drawer.X, (float)drawer.Y, SurfaceSalvage.LabelFor(drawer.Find)));
            }

            // ── #563 slice 3 · AND THE SHELTERS, which slice 2 deliberately left at home ────────────────
            //
            // Not for scope: the rack state was keyed on a bare index into ONE site's list, and a list that
            // spanned a moving chunk would have re-pointed every reservoir on a tile crossing — in the one
            // system where getting it wrong empties a rack somebody walked to on their last two hundred
            // seconds of air. That keying is addressed now (Map.Surface.Racks.cs), so the second place on a
            // moon that refills a suit can be a fact about the GROUND rather than about the ground beside
            // the ship.
            //
            // It is also what makes an unbounded world walkable rather than merely large. #562 priced
            // distance in the walk back and #564 priced it in air; a lattice whose only air was at the tube
            // would be a lattice a captain may look at and never enter. SurfaceShelter.CountFor has been per
            // AREA since #585, so a tile out here carries exactly the density the field under the tube always
            // had — the ground simply carries on, which is the whole of #563's decision.
            //
            // Furnished through MoonSurface's own body and never a second copy of it: #573's scar is
            // precisely a builder and an instrument that disagreed about how many shelters there were.
            foreach (SurfaceStructure.Spec shelter in SurfaceTiles.Shelters(body, salt, a))
            {
                MoonSurface.FurnishShelter(shelter, walls, doors, consoles, labels);
            }

            if (SurfaceTiles.NorthRim(a) is { } rim)
            {
                walls.Add(new((float)rim.X1, (float)rim.Y1, (float)rim.X2, (float)rim.Y2,
                    false, false, Unseen: true));
            }
        }

        return new DeckPlan.DeckRegion(
            [.. walls], [.. consoles], [.. labels], [], null, [.. scenery], [.. doors]);
    }

    // ── #563 slice 2 · THE HUTS STAY AS YOU LEFT THEM, ACROSS VISITS ────────────────────────────────────
    //
    //  Slice 1 keyed everything a captain did to a hut on the TILE, which stopped one forced hatch opening
    //  every hut on the moon. It kept those three sets on the EXCURSION, which meant lifting off forgot the
    //  lot: a hut shouldered open on Tuesday was dogged again on Wednesday, and the ammunition you had
    //  already taken was back on the shelf. That is the same "the world quietly becomes wallpaper" failure
    //  one layer out — it does not crash, it just reads as a save bug, because it is one.
    //
    //  So the state lives on the SHIP's ledger (GroundMemory, persisted in the vault) rather than on the
    //  visit, keyed on (body, site, tile, what). The excursion holds no copy of it at all — a cached copy of
    //  a fact is a second source of that fact, and this is exactly the class of state that must not have two.

    /// <summary>Has the captain already done this to the hut on this tile?</summary>
    private bool HutRemembers(SurfaceExcursion ex, SurfaceTiles.Address tile, GroundMemory.HutChange what) =>
        _groundMemory.Knows(
            GroundMemory.HutKey(ex.Stop.Body.Id, ex.Site.LayoutSalt, tile, what));

    /// <summary>Remember that they have. True the first time, which is what lets a press that changes
    /// nothing stay quiet.</summary>
    private bool RememberHut(SurfaceExcursion ex, SurfaceTiles.Address tile, GroundMemory.HutChange what) =>
        _groundMemory.Remember(
            GroundMemory.HutKey(ex.Stop.Body.Id, ex.Site.LayoutSalt, tile, what));

    /// <summary>Everything this captain has done to the ground of every moon they have walked, kept across
    /// excursions and across the whole voyage. See <see cref="GroundMemory"/>.</summary>
    private GroundMemory _groundMemory = new();

    // ── #316 law 1 · THE HUSKS ARE ON THE SAME LEDGER, AND FOR THE SAME REASON ──────────────────────────
    //
    //  Owner, live: "If we find already shot Reevers at a site then we know that somebody else has been there
    //  to hide, pick-up, search etc :-D It serves as a clue." A clue is a thing you come BACK to, and the
    //  husks were the one mark in this game that could not be come back to: a list on the excursion, written
    //  by the sentry volley and the sweep team, thrown away with the visit. Lift off, land again, and the
    //  field where four Old Ones went down was clean regolith.
    //
    //  So a husk on the regolith is a ground mark like any other — the identical (body, site, tile) key, the
    //  identical vault section, the identical law that the ledger is the one source. What it carries that no
    //  other mark does is WHEN, because its value as evidence is its age.

    /// <summary>Does the GROUND out here keep husks? Only the regolith does. A poured floor hundreds of
    /// metres down and somebody else's steel deck are both real places to be shot on, and neither of them is
    /// a landing site's tile lattice — a husk recorded there would be filed against a coordinate frame it
    /// was never measured in.</summary>
    private bool TheGroundKeepsHusksHere(SurfaceExcursion ex) => ex.Floor == 0 && !OnWreck;

    /// <summary>
    /// ONE OLD ONE GOES DOWN. The single writer, and every path that drops one comes through here — the
    /// sentry volley, and the sweep team's professionals. It does two things and they cannot come apart: the
    /// visit gets the mark it draws, and the GROUND gets the row it keeps.
    /// </summary>
    private void AHuskFallsAt(SurfaceExcursion ex, double x, double y)
    {
        var husk = new GroundMemory.Husk(x, y, SimTime);
        ex.Husks.Add(husk);

        if (!TheGroundKeepsHusksHere(ex))
        {
            return;
        }

        if (_groundMemory.Remember(GroundMemory.HuskKey(ex.Stop.Body.Id, ex.Site.LayoutSalt, husk)))
        {
            RequestVaultSave();   // the footprints have to survive the shuttle, which means surviving the file
        }
    }

    /// <summary>What the last visit left lying here, before the first frame of this one is drawn. The same
    /// place and the same reason <see cref="SeedTurnedOverRooms"/> runs: a ground painted ahead of the
    /// seeding would be a field a captain walks into clean and then watches sprout corpses.
    ///
    /// <para>Core reads its own rows (<see cref="GroundMemory.TryReadHuskKey"/>); nothing here knows the key
    /// format. And nothing here rolls: a husk is on this ground because something actually fell on it.</para>
    /// </summary>
    private void SeedTheHusksLeftHere(SurfaceExcursion ex)
    {
        foreach (GroundMemory.Husk husk in _groundMemory.HusksAt(ex.Stop.Body.Id, ex.Site.LayoutSalt))
        {
            ex.Husks.Add(husk);
        }
    }

    /// <summary>
    /// #316 law 2 · READING THE SCENE. Owner: <i>"on visiting, remains render with age-graded flavor in the
    /// house voice — 'still smoking' vs 'regolith-dusted, weeks old' — so recency is legible to a captain who
    /// looks."</i> Walk onto the pile and the pile says how long it has been there; the words are Core's
    /// (<see cref="GroundMemory.AgeLine"/>), off the sim time in the ledger.
    ///
    /// <para><b>Why the walk and not [E].</b> A press would have been the natural verb — the ground already
    /// answers one ahead of the consoles (#688) — but [E] on the regolith is <c>BURY THE CHEST HERE</c>, and a
    /// husk that ate that press would be a corpse a captain can never bury a chest beside. The pulse is also
    /// the idiom the husks already speak in, and it is what the issue actually asks for: legible <i>on
    /// visiting</i>, not on a deliberate verb.</para>
    ///
    /// <para>THE WHOLE PILE IN REACH IS ONE LOOK: every husk within the reach is marked read and the FRESHEST
    /// of them supplies the sentence, so four bodies in a heap are one line about the most recent death
    /// rather than four lines in four frames. Latched per husk per visit (<c>HusksRead</c>).</para>
    /// </summary>
    private void CheckHusksUnderfoot()
    {
        // `_viewObject` for the sibling polls' reason (#680): a pulse played under an open card renders
        // beneath its backdrop.
        if (_surface is not { } ex || ex.Husks.Count == 0 || _viewObject is not null)
        {
            return;
        }

        GroundMemory.Husk? freshest = null;
        var reached = new List<string>();
        foreach (GroundMemory.Husk husk in ex.Husks)
        {
            double dx = husk.X - _avatarX;
            double dy = husk.Y - _avatarY;
            if ((dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius)
            {
                continue;
            }

            // The ledger's own key identifies it, so the latch and the store name the same corpse. A husk on
            // a steel deck has no ledger row of its own; the key is still the thing that tells two of them
            // apart, so it is built the same way and simply never written.
            string id = GroundMemory.HuskKey(ex.Stop.Body.Id, ex.Site.LayoutSalt, husk);
            if (!ex.HusksRead.Add(id))
            {
                continue;
            }

            reached.Add(id);
            if (freshest is not { } far || husk.FellAtSimTime > far.FellAtSimTime)
            {
                freshest = husk;
            }
        }

        if (reached.Count == 0 || freshest is not { } read)
        {
            return;
        }

        // A middle-aged husk has no sentence yet (GroundMemory.AgeLine returns null, and the marker there
        // says why). It is still MARKED read: the silence is the reading, and asking again every frame for a
        // line that does not exist would be a poll with no end.
        if (GroundMemory.AgeLine(read, SimTime) is { } line)
        {
            ShowPulseMessage($"☠ {line}");
        }
    }

    /// <summary>Where this tile's hut stands, resolved once and remembered for the visit. A cache of a pure
    /// function — dropping it would cost a regeneration and change nothing, which is exactly the property
    /// that makes it safe to keep.</summary>
    private static SurfaceOutpost.Placement HutOn(
        SurfaceExcursion ex, SurfaceTiles.Address a, bool forcePresent)
    {
        if (ex.Huts.TryGetValue(a, out SurfaceOutpost.Placement cached))
        {
            return cached;
        }
        SurfaceOutpost.Placement p = SurfaceOutpost.ForTile(
            ex.Stop.Body.Id, ex.Site.LayoutSalt, a, forcePresent && a == SurfaceTiles.Home);
        ex.Huts[a] = p;
        return p;
    }

    /// <summary>Every hut on the ground the captain is currently carrying, in chunk order.</summary>
    private IEnumerable<SurfaceOutpost.Placement> HutsInReach(SurfaceExcursion ex)
    {
        foreach (SurfaceTiles.Address a in ex.Stream.Loaded)
        {
            SurfaceOutpost.Placement p = HutOn(ex, a, _outpostCheat);
            if (p.HasOutpost)
            {
                yield return p;
            }
        }
    }

    /// <summary>Which hut the console under the captain's hand belongs to — the nearest one whose own
    /// geometry owns that spot. A site has many huts now, so "the hut" is a question with an answer rather
    /// than an assumption.</summary>
    private SurfaceOutpost.Placement? HutUnderYourHand(SurfaceExcursion ex, double x, double y)
    {
        SurfaceOutpost.Placement? best = null;
        double bestSq = double.MaxValue;
        foreach (SurfaceOutpost.Placement p in HutsInReach(ex))
        {
            // The room's own middle: the hatch is on one face, the interactables inside, so measuring to the
            // centre answers for both without needing to know which console was pressed.
            double cx = p.DoorX + (p.ExtendDir * 3.0), cy = p.DoorY;
            double dx = x - cx, dy = y - cy;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < bestSq)
            {
                (best, bestSq) = (p, d2);
            }
        }
        return best;
    }
}
