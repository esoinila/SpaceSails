using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #585 / #586 / #588 · THE THREAD THAT RUNS UNDER THE WHOLE FACILITY — a person assembled out of the
/// pieces a room leaves lying about, the detector sweeping as the captain walks, the lead it grants and the
/// moon it names, and what somebody left at the foot of the monolith. Split out of
/// <c>Map.Surface.Hive.cs</c> under #251 with no member renamed, re-scoped or re-ordered.
/// </summary>
public partial class Map
{
    // ── #588 · A PERSON, OUT OF THE PIECES ─────────────────────────────────────────────────────────────
    //
    // Owner: "when we find somebody's kit maybe we get gen ai compilation of what we discover about them...
    // nice place for world building and dropping bread crumbs about our big plot", and then the part that
    // makes it a MECHANIC rather than a lore drop — "if we know what happened to someone we get contacts
    // easily by contacting their loved ones, in some cases that might lead our gum-shoe-efforts forward."
    //
    // Three pieces of kit make a person (one is litter, two is a coincidence). The payoff is not loot: it is
    // an errand, and a name to drop, and — sometimes — somebody who has been waiting nine years for news and
    // knows something nobody would ever tell a pirate.
    //
    // ── #774 · AND THE SENTENCES ARE READ ON THE CARD, NOT UNDER IT ────────────────────────────────────
    //
    // This method used to raise the card and then fire two to four ShowAndFile lines beneath it: the person,
    // the next of kin, what the family knows, the in that fell out of the kit — every one of them pulsed
    // into the HUD while a full-screen backdrop stood in front of the HUD. All of them were filed, none of
    // them was readable, and the captain closed the card onto silence.
    //
    // #768's hold cannot settle it and was refused for exactly this case: it releases ONE winner, and these
    // are a same-rank SEQUENCE whose survivor would have been decided by which line was appended last —
    // the ordering-as-contract bug #693 killed. So the remedy is #736's law instead. The card carries them,
    // in Core's own reading order (FieldDossier.Beat), and the book keeps every one of them exactly as it
    // always did: what changed is where a sentence is READ, never what is recorded.
    private void AssembleSomebody(SurfaceExcursion ex, string body, string salt, int roomIndex)
    {
        ex.KitPieces.Add(roomIndex);

        // ?kit=1 — the picture comes together on the FIRST piece. Three papers rooms at one room in eight,
        // inside a single excursion, is the rarest thing on the regolith; the cheat moves the gate and
        // nothing else, so what a tester reads is a dossier a captain can genuinely be handed.
        int enough = _kitCheat ? 1 : FieldDossier.FragmentsToAssemble;
        if (ex.KitPieces.Count < enough || ex.DossierShown)
        {
            return;
        }
        ex.DossierShown = true;

        // The stranger is keyed on the room whose papers COMPLETED the picture, so which body you are
        // holding is a fact about where you searched — not a global roll that would have happened anyway.
        FieldDossier.Person who = FieldDossier.Who(body, salt, roomIndex);
        string place = Core.FieldNotes.PlaceLabel(ex.Stop.Body.Name, ex.Site.Name);

        // Everything this kit has to say, already in the order it is read — composed in Core beside the
        // rolls that decide whether each sentence exists at all, so the client never picks an order.
        IReadOnlyList<FieldDossier.Saying> debrief =
            FieldDossier.Debrief(body, salt, roomIndex, everySaying: _kitCheat);

        // The card, with the compiled effects AND the debrief on it. Reuses the ViewObject pop-up the
        // builder's plate and the souvenirs already use — one image surface, not a second one to keep in
        // step. The caption is the fiction; the outcome region is what you are now holding.
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            FieldDossier.ConsoleLabel, FieldDossier.ArtUrl, FieldDossier.Compiled(who, place),
            FieldDossier.DebriefBlock(debrief));

        RendererInterop.PlayCue("board");

        // The book, unchanged: the same sentences under the same glyphs in the same order they were said.
        foreach (FieldDossier.Saying said in debrief)
        {
            // #741 v1 · …and WHAT EACH ONE IS ABOUT, straight off its author. Core composed the sentence out
            // of a person, an employer and a door and declared them in the same record; the client copies
            // the field across and never once reads the words back. The badge that says a stack just grew
            // ("second entry about …") composes itself onto THIS card, inside FileNote, per #736.
            FileNoteAbout(said.Text, said.Glyph, said.Subjects);

            // #585: and what the family knows is a PLACE. This is the owner's own chain closing — "if we
            // know what happened to someone we get contacts easily by contacting their loved ones, in some
            // cases that might lead our gum-shoe-efforts forward" — arriving, eventually, at a moon on the
            // tracker. Banked at the same point in the sequence it has always stood, between the hint and
            // the in, because the field book's order is a record of when things were said.
            if (said.Beat is FieldDossier.Beat.WhatTheFamilyKnows)
            {
                GrantLabLead(DiceRule.Seed($"lead:kin:{body}:{salt}:{roomIndex}"));
            }
        }

        ApplyNerveShock(4.0, "a stranger's whole life, laid out on a rock");
    }

    // ── #585 · THE DETECTOR, SWEEPING ─────────────────────────────────────────────────────────────────
    //
    // Owner: "the detector should also give detecting readings near it."
    //
    // The probe was all-or-nothing — the exact square pings, its eight neighbours shriek, and the other four
    // thousand squares say nothing — so finding a lab was a lottery rather than a search. This is the
    // hot-and-cold every treasure hunt has run on forever: a reading that climbs as you close and falls away
    // as you drift, so a captain can pick a bearing, walk it, and TURN when it cools.
    //
    // It only wakes on a moon somebody has named (#585's leads). A detector that hummed everywhere would hand
    // over every lab in the system for free and make the whole clue chain pointless.
    private SecretLab.Reading _lastDetectorReading = SecretLab.Reading.Silent;

    private void StepSecretLabDetector()
    {
        if (_surface is not { } ex
            || ex.Lab is not { HasLab: true } lab
            || ex.SecretLabDoorRevealed
            || !_labLeads.Contains(ex.Stop.Body.Id))
        {
            _lastDetectorReading = SecretLab.Reading.Silent;
            return;
        }

        double dx = lab.DoorX - _avatarX, dy = lab.DoorY - _avatarY;
        SecretLab.Reading now = SecretLab.ReadingAt(Math.Sqrt((dx * dx) + (dy * dy)));
        if (now == _lastDetectorReading)
        {
            return;   // speak on the CHANGE only; a needle that narrates every frame is noise
        }

        bool warmer = now > _lastDetectorReading;
        _lastDetectorReading = now;

        string line = SecretLab.ReadingLine(now, warmer);
        if (line.Length > 0)
        {
            ShowPulseMessage(line);
            if (warmer && now >= SecretLab.Reading.Strong)
            {
                RendererInterop.PlayCue("pulse");
            }
        }
    }

    /// <summary>#585 · A clue names a moon. Called from every find in the gumshoe chain — a file in a
    /// facility, papers in a ruin, what a dead specialist's family turns out to know.</summary>
    /// <summary>Names a moon worth searching, and returns WHICH — #613, so a Key found on a bottom floor can
    /// mint the card for the shaft it points at. Returns the body even when the lead was already known: the
    /// lead is news you can only hear once, the card is an object that exists regardless.</summary>
    private string? GrantLabLead(ulong seed)
    {
        if (NameAMoonWorthLookingAt(seed) is not { } named)
        {
            return null;
        }
        AnnounceLabLead(named);
        return named;
    }

    /// <summary>#684 · WHICH moon, and NOTHING else — no lead written down, no line said, no save asked for.
    ///
    /// <para>Split out because one caller has to know the answer BEFORE it is allowed to keep it. A Key found
    /// on a bottom band mints its card for the site a lead names (#613), and that mint can be refused by a
    /// full pocket (#678) — at which point the room is not emptied and searching it again offers the same
    /// find. The lead was being banked and SAID during the naming, one step ahead of the capacity check, so a
    /// captain with no room left walked away holding the knowledge and none of the card. The sentence was
    /// composed before the act it describes, which is the exact fault #678 was filed about, surviving in the
    /// one branch of it that reached outside the pocket.</para></summary>
    private string? NameAMoonWorthLookingAt(ulong seed)
    {
        if (_surface is not { } ex)
        {
            return null;
        }

        var candidates = new List<string>();
        foreach (ShuttleStop stop in ShuttleDestinationsInRange())
        {
            if (stop.IsLandable && !Derelict.TryParseWreckId(stop.Body.Id, out _))
            {
                candidates.Add(stop.Body.Id);
            }
        }
        if (!candidates.Contains(ex.Stop.Body.Id))
        {
            candidates.Add(ex.Stop.Body.Id);
        }

        return SecretLab.MoonWorthLookingAt(candidates, seed);
    }

    /// <summary>#684 · Bank the lead and say it — once. News you can only hear the first time, which is why
    /// it must not be spent on a find the pocket then refuses.</summary>
    private void AnnounceLabLead(string named)
    {
        if (!_labLeads.Add(named))
        {
            return;
        }

        string display = ShuttleDestinationsInRange()
            .FirstOrDefault(s => s.Body.Id == named)?.Body.Name ?? named;

        // #774 · Where the captain is looking, because one of this method's callers is the dossier assembly
        // and the dossier's own card is up when it calls — a moon named into a pulse behind that backdrop is
        // the fifth sentence of the same bug. Every other caller reaches this with nothing in front of them,
        // where the seam is an ordinary pulse and indistinguishable from one.
        SayWhereTheyAreLookingAndFile(SecretLab.LeadLine(display), "🔎");
        RequestVaultSave();
    }

    // ── #586 · WHAT SOMEBODY LEFT AT THE FOOT OF THE MONOLITH [E] ──────────────────────────────────────
    //
    // Owner: "let's have gen AI image at the monolith and some items appearing there now and then ... it is
    // supposed to be impressive... now it looks like a box in closet."
    //
    // The picture is the [E] on the slab itself. THIS is the other half, and it is the half that makes the
    // place alive: a landmark that never changes is scenery you visit once. Every line here is somebody
    // ELSE's visit — a cutting rig laid down neatly, a scoured plate, bootprints that all face the slab and
    // none lead away. The monolith itself never speaks, never reacts, and is never confirmed to have noticed
    // anything, which is the whole register (see reever-origin canon: the game never explains this).
    private void MonolithFootInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.MonolithFoot })
        {
            return;
        }

        long epoch = Monolith.EpochAt(SimTime);
        Monolith.Offering left = Monolith.AtTheFoot(ex.Stop.Body.Id, ex.Site.LayoutSalt, epoch);
        if (left == Monolith.Offering.Nothing)
        {
            return;
        }

        string key = $"monolith:{epoch}";
        if (!ex.RuinsSearched.Add(key))
        {
            ShowPulseMessage("You have already looked at it. It has not changed.");
            return;
        }

        ShowAndFile(Monolith.FootLine(left, ex.Stop.Body.Id, ex.Site.LayoutSalt, epoch), "▮");

        // It costs nerve to stand here reading somebody else's last afternoon. Remains cost more.
        ApplyNerveShock(left == Monolith.Offering.Remains ? 5.0 : 2.0,
            "somebody else got this far, and this is what is left of their visit");
        RequestVaultSave();
    }
}
