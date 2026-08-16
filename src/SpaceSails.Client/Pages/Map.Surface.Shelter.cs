using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the beacons, the ruins, the emergency locker and the shelter rack.
public partial class Map
{
    // ── #573 · THE SHELTER'S CHARGING RACK [E]. The only place outside her tube that refills a suit, and
    //    therefore the only reason the deep field is worth crossing rather than merely looking at. ──
    /// <summary>#573 · The fixed places the fan should point at: the way home, and every shelter. Bearings
    /// and ranges from the captain, so the tracker answers "which way" for somewhere that does not move.</summary>
    private List<(double Bearing, double Range, bool IsHome, bool IsLab)> BuildBeacons(SurfaceExcursion ex)
    {
        var list = new List<(double, double, bool, bool)>();
        if (Derelict.TryParseWreckId(ex.Stop.Body.Id, out _))
        {
            return list;   // a hull has neither a tube mouth nor a shelter
        }

        void Add(double x, double y, bool home, bool lab = false)
        {
            double dx = x - _avatarX, dy = y - _avatarY;
            list.Add((Math.Atan2(dy, dx), Math.Sqrt((dx * dx) + (dy * dy)), home, lab));
        }

        // ── #591 · UNDERGROUND, THE BEACONS ARE DIFFERENT PLACES ──
        //
        // Owner, on B1: "now that we are underground the elevator would be nice to be on the motion detector,
        // the surface hut's are really not that relevant down here".
        //
        // He is right, and it is the same fault as the reach: these are SURFACE beacons. The tube mouth and
        // the shelters are up a lift shaft and several hundred metres of rock away, so painting them on the
        // fan down here is not merely useless — it is actively wrong, because the way home ring is the one
        // the captain reads when the air gets short and it would be pointing at a hut they cannot reach.
        //
        // Down here the places worth a ring are the CARS, and they are the same ones every floor. They are
        // the way home in the only sense that matters underground, so they take the HOME flag and the calm
        // colour that goes with it — a place, not a thing that moves.
        //
        // #801 · Plural, and off the one list. This said "there is exactly ONE place worth a ring" and drew
        // it from ShaftAt; a captain being hunted toward the goods car would have had the instrument telling
        // them the way home was a hundred and seventy du behind them, which is the map lying (#573) in the
        // one place it costs a life.
        if (ex.Floor < 0)
        {
            foreach (UndergroundComplex.Shaft car in
                UndergroundComplex.ShaftsOn(MoonSurface.ExpeditionField()))
            {
                (double carX, double carY) = car.Landing;
                Add(carX,
                    carY + ((car.Kind == UndergroundComplex.ShaftKind.Cage ? 1 : -1)
                        * (UndergroundComplex.CorridorHalf + 1.5)),
                    home: true);
            }

            // ── #608 · AND THE REFUGES, WHICH ARE THIS FLOOR'S SHELTERS ──
            //
            // Owner, exactly: "and those need to show in the motion detector, not the surface ones, when you
            // are 150 meters below surface."
            //
            // That sentence is the whole rule and both halves of it are load-bearing. The surface shelters
            // are up a shaft and several hundred metres of rock away, so painting them here would be the map
            // lying (#573) in its most expensive form — a ring on the instrument a captain would spend the
            // last of a tank walking toward. They are already gone, and they stay gone: this branch never
            // touches SheltersOn.
            //
            // What replaces them is the thing the ring MEANS. On the regolith a not-home ring is "air you
            // can reach that is not the ship"; on a dead floor that is the refuge, and it deserves the same
            // colour because it is the same promise. It also answers #608's hardest requirement — "a refuge
            // you discover AFTER you needed it is a cruelty" — without a map, a paper or a tutorial: the
            // instrument the captain already watches simply has it on it.
            //
            // Nothing is painted on a floor that holds pressure, because there is nothing to point at: the
            // whole floor is the refuge, and a ring saying "air, 40 du that way" while you are standing in
            // air is an instrument disagreeing with the room.
            foreach ((double rx, double ry) in RefugesOn())
            {
                Add(rx, ry, home: false);
            }
            return list;
        }

        Add(MoonSurface.SpawnX, MoonSurface.SpawnY, home: true);
        foreach (SurfaceStructure.Spec shelter in SheltersOn(ex))
        {
            Add(shelter.CentreX, shelter.CentreY, home: false);
        }

        // #585/#584 · AND THE LIFT HEAD, once the door is known. Owner, standing in a ruin that happened to
        // have a violet door: "it should be this space? but how do I get in this has purple door and is not
        // emergency shelter?"
        //
        // Two failures behind that one sentence. First, the HUD has been saying "E at the ⊙ HIDDEN DOOR —
        // force the secret lab open" while nothing anywhere says WHERE it is (#584, filed before he hit it and
        // then hit anyway). A prompt you cannot act on is worse than silence.
        //
        // Second, mine and worse: I gave imported violet to shelters (always), to about one ruin door in
        // seven, AND to the lift head — so a colour that was supposed to mean "somebody shipped this here"
        // now means "some doors", and the one door it most needed to distinguish was lost among them. A
        // signal that fires on three unrelated things is not a signal.
        //
        // The beacon is the honest fix: the ground can carry as many violet doors as the fiction wants, and
        // the INSTRUMENT says which one is the way down.
        if (ex.SecretLabDoorRevealed && ex.Lab is { HasLab: true } lab)
        {
            Add(lab.DoorX, lab.DoorY, home: false, lab: true);
        }

        return list;
    }

    /// <summary>#573 · Your own buried caches, as marks on the fan — but ONLY once they are inside its
    /// reach. The range gate is the entire design: a field this size made finding your own ✗ a real task,
    /// and an instrument that always knew where it was would take that task straight back off you.</summary>
    private List<(double Bearing, double Range)> BuildCacheBeacons()
    {
        var list = new List<(double, double)>();
        foreach ((double mx, double my, bool _) in _hudMarks)
        {
            double dx = mx - _avatarX, dy = my - _avatarY;
            double range = Math.Sqrt((dx * dx) + (dy * dy));
            if (range <= CacheDetectRangeDu)
            {
                list.Add((Math.Atan2(dy, dx), range));
            }
        }
        return list;
    }

    /// <summary>How close a buried cache has to be before the fan admits to it.</summary>
    private const double CacheDetectRangeDu = 55.0;

    /// <summary>#573 · What the captain has been TOLD, as opposed to what they can see — a wide, soft wash
    /// rather than a mark. A tip narrows a search; it does not end one, and a dot would claim a precision
    /// the information does not have.</summary>
    private List<(double Bearing, double Range, double Spread)> BuildRumours(SurfaceExcursion ex)
    {
        var list = new List<(double, double, double)>();
        if (ex.Lab is not { HasLab: true } lab || ex.SecretLabDoorRevealed)
        {
            return list;
        }
        // #585: a tip you were GIVEN counts, not only a place you have already been. This used to read only
        // _secretLabsFound, so the wash helped on a return visit and did nothing on the first — the one visit
        // where a captain actually needs help. The clue chain had no way into the instrument at all.
        if (!_labLeads.Contains(ex.Stop.Body.Id) && !_secretLabsFound.Contains(ex.Stop.Body.Id))
        {
            return list;   // nobody has tipped you about this one; there is nothing to be vague about
        }

        double dx = lab.DoorX - _avatarX, dy = lab.DoorY - _avatarY;
        list.Add((Math.Atan2(dy, dx), Math.Sqrt((dx * dx) + (dy * dy)), RumourSpreadDu));
        return list;
    }

    /// <summary>How wide a rumour reads on the fan, in deck units. Big — it is a tip, not a fix.</summary>
    private const double RumourSpreadDu = 45.0;

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

        // Identify WHICH ruin by its position — the console sits at the building's centre, which is the
        // stable key SurfaceLayout hands out.
        string body = ex.Stop.Body.Id, salt = ex.Site.LayoutSalt;
        SurfaceLayout.Plan plan = SurfaceLayout.For(body, MoonSurface.ExpeditionField(), salt);
        IReadOnlyList<(double X, double Y)> centres = plan.BuildingCentres ?? [];

        int which = -1;
        for (int i = 0; i < centres.Count; i++)
        {
            if (Math.Abs(centres[i].X - spot.X) < 0.5 && Math.Abs(centres[i].Y - spot.Y) < 0.5)
            {
                which = i;
                break;
            }
        }
        if (which < 0)
        {
            return;
        }

        string key = $"{which}";
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

            case SurfaceSalvage.Find.Papers:
                // Texture, never testimony (#563): a roster, a docket, a note in a locker. Nothing here
                // explains what is outside, and nothing ever will.
                ShowAndFile(SurfaceSalvage.PapersLine(body, salt, which), "📄");
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
        int whichLocker = ShelterUnderfoot(ex);
        if (whichLocker < 0)
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
        int which = ShelterUnderfoot(ex);
        if (which < 0)
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

        int which = RefugeUnderfoot(ex);
        if (which < 0)
        {
            ShowPulseMessage("🫁 The rack's fitting is through the inner door. Step into the air.");
            return;
        }
        ShowPulseMessage(RackGaugeLine(ex, RefugeReservoirNow(ex, which)));
    }
}
