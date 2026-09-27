using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · WORKING A RUIN — salvage at a ruin, the tile under your hand, and the tiles underfoot.
///
/// <para>Split out of <c>Map.Surface.Shelter.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening
/// file.</para>
/// </summary>
public partial class Map
{
    // ── #573 · TURNING OVER A RUIN [E]. About half of them hold something; the rest are somebody's empty
    //    house, and finding those is what makes the others worth the air it cost to walk in. ──
    private void RuinSalvageInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.RuinSalvage } spot)
        {
            return;
        }

        // Identify WHICH ruin, and — since #563 slice 2 — ON WHICH TILE. The console sits at the building's
        // centre, which is the stable key SurfaceLayout hands out; the tile is what makes that key unique
        // once the ground is a lattice and every tile has buildings of its own.
        string body = ex.Stop.Body.Id;
        (SurfaceTiles.Address tile, int which) = RuinUnderYourHand(ex, spot.X, spot.Y);
        if (which < 0)
        {
            return;
        }

        // The tile's own contents salt, and it is used for EVERY question below rather than the site's — the
        // find, the rounds, the credits, the papers, the person they assemble into, the lead. One salt per
        // ruin, resolved once: asking some of them on the site and some on the tile is how a drawer comes to
        // hold one thing and report another.
        string salt = SurfaceTiles.ContentSalt(body, ex.Site.LayoutSalt, tile);

        string key = $"{tile.X}_{tile.Y}:{which}";
        if (!ex.RuinsSearched.Add(key))
        {
            ShowPulseMessage("You have already been through this one.");
            return;
        }

        switch (SurfaceSalvage.WhatIsInside(body, salt, which))
        {
            case SurfaceSalvage.Find.Rounds:
            {
                int rounds = SurfaceSalvage.RoundsIn(body, salt, which);
                var takers = ex.Bots.Where(b => b.Rounds < SentryBot.MaxMagazine).ToList();
                int left = rounds;
                foreach (SurfaceBot bot in takers)
                {
                    int take = Math.Min(SentryBot.MaxMagazine - bot.Rounds, left);
                    bot.Rounds += take;
                    left -= take;
                    if (left <= 0)
                    {
                        break;
                    }
                }
                RendererInterop.PlayCue("board");
                ShowPulseMessage(SurfaceSalvage.RoundsLine(rounds - left));
                WhatTheDrumsCouldNotHold(left);
                break;
            }

            case SurfaceSalvage.Find.Goods:
            {
                int credits = SurfaceSalvage.GoodsIn(body, salt, which);
                _credits += credits;
                RendererInterop.PlayCue("board");
                ShowAndFile(SurfaceSalvage.GoodsLine(credits), "💰");
                break;
            }

            // #763 · SOMEBODY'S SDR, under a bunk. The one intake outside the Hive that can be refused, so
            // it is the one that has to keep #678's law: a find the pocket will not take is STILL LYING
            // THERE. The room was marked searched a few lines above this switch, so it is unmarked again —
            // the sentence and the world must agree that nothing was consumed.
            case SurfaceSalvage.Find.Kit:
            {
                if (!Core.Satchel.CanTake(_satchel, Core.SdrScanner.TheKit))
                {
                    ex.RuinsSearched.Remove(key);
                    ShowPulseMessage(UndergroundComplex.PocketFullLine);
                    break;
                }

                _satchel = [.. Core.Satchel.Add(_satchel, Core.SdrScanner.TheKit)];
                RendererInterop.PlayCue("board");
                ShowAndFile(SurfaceSalvage.KitLine(), Core.SdrScanner.Glyph);
                break;
            }

            case SurfaceSalvage.Find.Papers:
                // Texture, never testimony (#563): a roster, a docket, a note in a locker. Nothing here
                // explains what is outside, and nothing ever will.
                // #417 · …and under the case's own headings when this is the ground a finder's case names.
                // Empty subjects everywhere else, which is exactly what ShowAndFile already files.
                ShowAndFileAbout(
                    SurfaceSalvage.PapersLine(body, salt, which), "📄", ThePapersSubjectsAt(body));
                ApplyNerveShock(2.0, "somebody else's paperwork, still where they left it");
                AssembleSomebody(ex, body, salt, which);   // #588: a person, out of the pieces

                // #585 · AND SOMETIMES A PLACE NAME. This is the thread that makes the labs findable at all:
                // a docket in a ruin, read carefully, names a moon somebody was running something on.
                if (DiceRule.Roll(DiceRule.Seed($"lead:papers:{body}:{salt}:{which}"), 3).Face == 1)
                {
                    GrantLabLead(DiceRule.Seed($"lead:pick:{body}:{salt}:{which}"));
                }
                break;

            default:
                ShowAndFile(SurfaceSalvage.EmptyRoomLine(body, salt, which), "🚪");
                break;
        }

        RebuildSurfaceDeck();
        RequestVaultSave();
    }

    /// <summary>#563 slice 2 · WHICH RUIN THE CAPTAIN'S HAND IS ON, and which tile it stands on.
    ///
    /// <para>This used to ask the HOME tile's plan and nothing else, which was right while the ground was one
    /// field. With a lattice it meant a captain standing in a ruin two tiles out pressed [E] and either got
    /// nothing (no home building at that spot) or — far worse — got the home tile's building of the same
    /// index, so the drawer reported somebody else's papers. So the search runs over the ground actually
    /// being carried, home tile included, and hands back the address as well as the index.</para>
    ///
    /// <para>The home tile is asked first and by name, because it is not in <c>Stream.Loaded</c> on a ground
    /// that is not a lattice at all — a derelict's deck and an away-expedition site still have ruins on
    /// them, and they still answer here.</para></summary>
    private (SurfaceTiles.Address Tile, int Index) RuinUnderYourHand(
        SurfaceExcursion ex, double x, double y)
    {
        string body = ex.Stop.Body.Id, salt = ex.Site.LayoutSalt;

        foreach (SurfaceTiles.Address a in TilesUnderfoot(ex))
        {
            SurfaceLayout.Plan plan = a == SurfaceTiles.Home
                ? SurfaceLayout.For(body, MoonSurface.ExpeditionField(), salt)
                : SurfaceTiles.Ground(body, salt, a);
            IReadOnlyList<(double X, double Y)> centres = plan.BuildingCentres ?? [];
            for (int i = 0; i < centres.Count; i++)
            {
                if (Math.Abs(centres[i].X - x) < 0.5 && Math.Abs(centres[i].Y - y) < 0.5)
                {
                    return (a, i);
                }
            }
        }
        return (SurfaceTiles.Home, -1);
    }

    /// <summary>The home tile, then every other tile the excursion is carrying. One list, so anything that
    /// has to find "the thing under the captain's hand" walks the same ground the renderer just drew.</summary>
    private static IEnumerable<SurfaceTiles.Address> TilesUnderfoot(SurfaceExcursion ex)
    {
        yield return SurfaceTiles.Home;
        foreach (SurfaceTiles.Address a in ex.Stream.Loaded)
        {
            if (a != SurfaceTiles.Home)
            {
                yield return a;
            }
        }
    }
}
