namespace SpaceSails.Core;

/// <summary>
/// #251 · WHAT EACH ONE SHOWS AND SAYS — the reveal plate, the canvases, the art, the title and the
/// caption.
///
/// <para>Split out of <c>StoryBeats.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class StoryBeats
{
    // ── What each one shows and says ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// #664 · THE ELEVEN ADOPTED BEATS DO NOT GET NEW WORDS, THEY GET A DOOR. Every one of them already had a
    /// <see cref="RevealPlate"/> in Core — a title, a painting and a caption — written beside the rule that
    /// decides the moment happened, because that was #634's law before this file existed. Copying those
    /// strings into the three switches below would have made a second source of truth for the same sentence,
    /// which is the drift this project keeps paying for; so the switches ASK, and this is the one place that
    /// knows which plate belongs to which beat.
    ///
    /// <para>Two of them are keyed by <paramref name="subject"/> rather than fixed, and that is the shape the
    /// old card had that a per-beat table could not: a KAAMOS or NEBULA shard's plate is chosen by the
    /// fragment the captain just assembled, so the picture cannot be shown for a shard nobody found. With no
    /// subject they resolve to nothing at all, which is honest — there is no such thing as "the KAAMOS card"
    /// in general. <see cref="Canvases"/> is how a sweep asks what the whole pool can name.</para>
    /// </summary>
    private static RevealPlate? PlateOf(Beat beat, string? subject) => beat switch
    {
        Beat.ArchivePurged => ArchiveNode.PurgedPlate,
        Beat.StrangerStandsADrink => StrangerBond.CognacPlate,
        Beat.KaamosShardFound => KaamosLore.PlateFor(subject ?? ""),
        Beat.KaamosFilingBounced => KaamosLore.BouncePlate,
        Beat.NebulaShardFound => NebulaLore.PlateFor(subject ?? ""),
        Beat.OutpostEffectsRead => SurfaceOutpost.EffectsPlate,
        Beat.SecretLabDoorFound => SecretLab.DoorPlate,
        Beat.TheDormantThingWakes => SecretLab.TheyStandPlate,
        Beat.ShelterIsNotSanctuary => CollectorLanding.SiegePlate,
        Beat.CollectorsSetDown => CollectorLanding.ArrivalPlate,
        Beat.SealedDoorReleased => NestPlates.Released,

        // #1151 · …and the claim's own desk, through the same door. Its three strings are authored beside the
        // rule that decides a claim happened, exactly as the eleven above are, so this file learns the
        // sentence rather than keeping a second copy of it.
        Beat.TheClaim => NebulaClaims.DeskPlate,

        _ => null,
    };

    /// <summary>
    /// #664 · EVERY CANVAS THIS BEAT CAN NAME — one for a fixed beat, the whole authored pool for one whose
    /// picture is chosen by its subject.
    ///
    /// <para><c>StoryArtPresentTests</c> used to sweep <see cref="ArtFile(Beat)"/>, which was the whole truth
    /// while every beat had exactly one painting. It is not any more: a subjectless <c>KaamosShardFound</c>
    /// has no canvas, and a sweep that took the empty string for an answer would have quietly stopped
    /// guarding the two arcs the moment they arrived. This is what the sweep asks instead, so adding a
    /// twelfth plate to a pool still means painting it or being told.</para>
    /// </summary>
    public static IEnumerable<string> Canvases(Beat beat) => beat switch
    {
        Beat.KaamosShardFound => KaamosLore.AllPlates.Select(p => p.Value.ArtFile).Distinct(StringComparer.Ordinal),
        Beat.NebulaShardFound => NebulaLore.AllPlates.Select(p => p.Value.ArtFile).Distinct(StringComparer.Ordinal),
        // #973 L5b · both women's portraits, so the manifest sweep sees the one that is not on screen too.
        Beat.WalkIn => WalkIn.AllPortraits,

        _ => [ArtFile(beat)],
    };

    /// <summary>The painting for this beat. Named here so the manifest and the code cannot drift; a file that
    /// has not been painted yet simply does not render, and the words carry it.</summary>
    /// <param name="subject">#664 · Which one this instance is about, for the beats whose canvas is chosen by
    /// it. Ignored by every beat that has one painting.</param>
    public static string ArtFile(Beat beat, string? subject = null) => beat switch
    {
        Beat.FirstShotFired => "art/first-shot.jpg",
        Beat.SailHoled => "art/sail-holed.jpg",
        Beat.CollectorHail => "art/collector-hail.jpg",
        Beat.CrewDeputation => "art/crew-deputation.jpg",
        Beat.CrewMeeting => "art/crew-meeting.jpg",
        Beat.ArcNewsBreaks => "art/arc-news.jpg",
        Beat.ChargeLetGo => "art/charge-let-go.jpg",
        Beat.FireAboard => "art/fire-aboard.jpg",
        // #973 · One plate for every flashback, and one style: bleached to white, a single object left in
        // focus. Fixed rather than keyed by the memory id on purpose — a memory is not a place, and painting
        // one canvas per ledger row is a pool nobody could ever finish.
        Beat.Flashback => "art/flashback.jpg",
        // #973 L5b · her portrait, which is also what her card draws. One painting for both women would be
        // one woman, so the canvas is chosen by the subject the beat was raised with.
        Beat.WalkIn => WalkIn.PortraitArt(subject),
        // #541: the tube's own canvases live with the tube, so the tier rule and the picture cannot disagree.
        Beat.BerthGreatPort => ArrivalTube.ArtFile(ArrivalTube.Tier.GreatPort),
        Beat.BerthWorkingBerth => ArrivalTube.ArtFile(ArrivalTube.Tier.WorkingBerth),
        Beat.BerthOutpost => ArrivalTube.ArtFile(ArrivalTube.Tier.Outpost),
        // #1149 · One canvas for every failed refuge in the game, and fixed rather than keyed by the site:
        // what the picture shows is not a moon, it is a room and what was done to its door.
        Beat.RefugeFailed => "art/refuge-failed.jpg",
        // #1199 · The walk itself, and it is the SAME canvas the room lays under its own glass floor
        // (ObservationWalk.ArtUrl) rather than a second painting of one view. The card is the absence, so
        // the picture has to be the place the captain is standing in with nobody in it.
        Beat.TheObservationWalk => ObservationWalk.ArtUrl,
        // #1199 (2026-09-18) · The binoculars' canvas is chosen by the SUBJECT, which is the look the
        // machine gave this press — the one channel this method already has for a beat with more than one
        // painting, used for the reason it exists. The machine's own type decides; nothing is decided here.
        Beat.TheWalksBinoculars => GalleryFixtures.ArtFor(LookIn(subject)),
        Beat.TheGalleryVendor => GalleryFixtures.CafeteriaArtUrl,

        _ => PlateOf(beat, subject)?.ArtFile ?? "",
    };

    /// <summary>#1199 (2026-09-18) · Which way the walk's binoculars were pointed, read back off the beat's
    /// SUBJECT. The subject travels as the enum member's own name and is parsed here, in one place, so the
    /// picture and the words cannot come to two answers — and an unparseable one reads as the view OUT,
    /// which is the look the machine gives first and therefore the safe way to be wrong.</summary>
    private static GalleryFixtures.Look LookIn(string? subject) =>
        Enum.TryParse(subject, out GalleryFixtures.Look look) ? look : GalleryFixtures.Look.Out;

    /// <summary>The title: it names the place and the verb, never the outcome. "WHAT THE VACUUM LEFT", not
    /// "salvage complete".</summary>
    /// <param name="subject">#664 · The room, the shard, the place — for the beats whose stamp names it.</param>
    public static string Title(Beat beat, string? subject = null) => beat switch
    {
        Beat.FirstShotFired => "🔫 THE FIRST ROUND YOU EVER FIRED",
        Beat.SailHoled => "🎯 HER SAIL IS GONE",
        Beat.CollectorHail => "⛓ GRAPPLES",
        Beat.CrewDeputation => "🧑‍🔧 A DEPUTATION",
        Beat.CrewMeeting => "🕯 THE MEETING YOU WERE NOT ASKED TO",
        Beat.ArcNewsBreaks => "📰 THE STORY BREAKS",
        Beat.ChargeLetGo => "⚡ SHE LETS GO",
        Beat.FireAboard => "🔥 THERE IS FIRE IN HER",
        // #973 · The stamp is the mark and the label the ledger row already wears, said louder. The subject
        // is the memory id and is deliberately NOT in the stamp: an entry key is bookkeeping, and a card that
        // put one on the screen would be showing the player the filing system instead of the memory.
        // #620 · …except the pendant's, which was never a page (Fable's canon addendum, verbatim). Same mark.
        Beat.Flashback when Keepsake.FlashbackTitle(subject) is { } pendantTitle => FilingLine.Mark + " " + pendantTitle,
        Beat.Flashback => FilingLine.Mark + " " + "A PAGE YOU DON'T REMEMBER WRITING",
        // #973 L5b · the stamp names the door, because the door is what the room looked at.
        Beat.WalkIn => "🚪 THE ROOM LOOKS AT THE DOOR",
        Beat.BerthGreatPort => ArrivalTube.Title(ArrivalTube.Tier.GreatPort),
        Beat.BerthWorkingBerth => ArrivalTube.Title(ArrivalTube.Tier.WorkingBerth),
        Beat.BerthOutpost => ArrivalTube.Title(ArrivalTube.Tier.Outpost),
        // #1149 · Authored canon (2026-09-06), verbatim, behind the refuge family's own glyph.
        Beat.RefugeFailed => "🫁 THE REFUGE THAT FAILED",
        // #1199 · Authored canon (2026-09-13), verbatim, behind the eye every watched-from-somewhere beat
        // already wears. It names the ROOM and not the event: a title that announced what had happened would
        // be the show telling the joke ahead of the picture.
        Beat.TheObservationWalk => ObservationWalk.Glyph + " " + ObservationWalk.CardTitle,
        // #1199 (2026-09-18) · Each machine's card is titled with the PLATE bolted to the machine, which is
        // the walk's own habit one room over: a title that announced what you were about to see would be the
        // show telling the joke before the picture. One string, read off the fixture, never retyped.
        Beat.TheWalksBinoculars => GalleryFixtures.BinocularsTitle,
        Beat.TheGalleryVendor => GalleryFixtures.VendorTitle,

        // #664 · The one adopted beat whose stamp names its subject: "🕷 DEEP HOLD — IT OPENS BOTH WAYS". The
        // two halves are joined in NestPlates so they cannot drift apart in two files, exactly as the after-
        // card's are; with no compartment named it falls back to the bare stamp rather than inventing a room.
        Beat.SealedDoorReleased => string.IsNullOrWhiteSpace(subject)
            ? NestPlates.Released.Title
            : NestPlates.ReleasedTitle(subject!),

        _ => PlateOf(beat, subject)?.Title ?? "",
    };

    /// <summary>
    /// The caption: it describes what is there and STOPS. This is the hardest discipline in the whole idiom and
    /// the reason the vented-room card works — the gouges cross the deck toward a sealed hatch and stop there,
    /// and nobody tells you what that means.
    /// </summary>
    /// <param name="subject">A ship's name, a haven, a headline — whatever this instance is about. Optional; the
    /// lines are written to read whole without it.</param>
    public static string Caption(Beat beat, string? subject = null)
    {
        string it = string.IsNullOrWhiteSpace(subject) ? "her" : subject!;

        return beat switch
        {
            Beat.FirstShotFired =>
                "The breech is still warm and nobody on the gun deck is looking at the target — they are looking " +
                "at each other. Whatever you were before this watch, the log now says otherwise.",

            Beat.SailHoled =>
                $"{it} is intact everywhere that does not matter. The sail is blown out mid-span, silvered film " +
                "peeling away in the vacuum, and her windows are still lit from the inside.",

            Beat.CollectorHail =>
                $"Grapples come across the frame from somewhere you were not watching, and {it} fills the window " +
                "with running lights the colour of a docking clamp. Nobody aboard her is in a hurry.",

            Beat.CrewDeputation =>
                "Three of them in the corridor outside your door, hats in hands, one with his knuckles up and " +
                "not knocking yet. They have clearly agreed who is going to say it.",

            Beat.CrewMeeting =>
                "The cantina at an odd watch, lamps down, five of them round one table and a chair pulled out " +
                "that nobody is sitting in. Not one of them looks at the door — which is how you know they heard " +
                "you coming.",

            Beat.ArcNewsBreaks =>
                $"The concourse screen is mid-broadcast and the room has turned up to watch it. One figure walks " +
                $"away from the screen instead of toward it, because {it} is not news to them.",

            // Lab 43 corrected this line's last clause. A discharge is 85,514× DIMMER than her own reflected
            // sunlight — nobody watches it through a telescope. She is not brighter; she is LOUDER, and every
            // receiver in the volume gets that for free without pointing anything at her.
            Beat.ChargeLetGo =>
                "A blue-white core sits on the mast for a moment with filaments raking off it into the dark, and " +
                "then there is nothing on the hull at all. Nobody saw that. Everything with a receiver heard it.",

            // #541: the tube's words live with the tube. The subject is the berth's name, and every line reads
            // whole without it — the tier is what the plate is about.
            Beat.BerthGreatPort => ArrivalTube.Caption(ArrivalTube.Tier.GreatPort) + " " +
                                   ArrivalTube.WalkLine(ArrivalTube.Tier.GreatPort),
            Beat.BerthWorkingBerth => ArrivalTube.Caption(ArrivalTube.Tier.WorkingBerth) + " " +
                                      ArrivalTube.WalkLine(ArrivalTube.Tier.WorkingBerth),
            Beat.BerthOutpost => ArrivalTube.Caption(ArrivalTube.Tier.Outpost) + " " +
                                 ArrivalTube.WalkLine(ArrivalTube.Tier.Outpost),

            // #973 · The caption for EVERY flashback plate — the signing one included (#973 L2). Fable's
            // line, verbatim; the FABLE marker L1 left here is answered and gone.
            // #973 L5b · …except the one whose subject is `since`, which is a page about a WOMAN and not
            // about a desk. Chosen by the subject, the way a shard's plate already is, and null for every
            // other memory — so the signing's sentence stays the sentence for all of them.
            Beat.Flashback when WalkIn.FlashbackCaption(subject) is { } sinceLine => sinceLine,

            // #620 · …and the one whose subject is the pendant's face, whose caption is Fable's first-opening
            // canon, verbatim and entire, read off the keepsake's own type so the plate and the shelf cannot
            // come to two accounts of one locket.
            Beat.Flashback when Keepsake.FlashbackCaption(subject) is { } pendantLine => pendantLine,

            Beat.Flashback =>
                "Bleached to the bone. A pen on a steel desk, every scratch in it sharp; behind it the room, " +
                "the chair, the one at the far side of the desk, all gone to white. Only the thing that was " +
                "in the hand survives the light.",

            // #973 L5b · Fable's own line for the moment, verbatim and whole: the room notices her before the
            // captain does, and nothing else about her is said. The subject is her id and is deliberately not
            // in the sentence — a caption that named her would be the seam introducing somebody the player is
            // about to be introduced to.
            Beat.WalkIn => WalkIn.TheRoomLooks,

            Beat.FireAboard =>
                "Forty years, and a pocket of her atmosphere was still shut in with something that would burn. " +
                "The light of it comes down the spine ahead of the heat, and every hatch anybody left open is a " +
                "road it already knows.",

            // #619 · Authored canon (2026-09-20), verbatim and entire, replacing the #1149 line written
            // against a card that was raised from inside the room. Three flat observations in the order a
            // captain standing at the door makes them — the weld, the gauge, the rack and the reservoir —
            // and the last clause is the only thing the card is allowed to conclude, which is a thing that
            // did NOT happen. It never says what came through, it never names the inspector, it never says
            // how many there were, and it never says which way anybody went. The Scully law (§13.8) at the
            // one door in the building where the temptation to explain is worst.
            Beat.RefugeFailed =>
                "The door is welded from the inside, and the weld is careful. The gauge beside it reads " +
                "what the room has, which is nothing. On the rack outside are more suits than this floor " +
                "ever had staff, and the reservoir on the deck was emptied by somebody who then did not " +
                "leave.",

            // #1199 · Authored canon (2026-09-13), verbatim and entire, read off the room's own type so the
            // card and the place cannot come to two accounts of one tube. Two sentences: the room, and then
            // the room running out. Nothing is explained and nothing is named — the card IS the absence.
            Beat.TheObservationWalk => ObservationWalk.CardBody,

            // #1199 (2026-09-18) · The two coin machines, authored (Fable, owner's own commission), verbatim
            // and entire, read off the fixture's own type. The binoculars' body is chosen by the same subject
            // that chose the painting, one line above — a card whose words and whose picture came to two
            // different views of which way the optics were pointed is this repository's "the sim doing one
            // thing while a SENTENCE reports another" class, at a coin slot.
            Beat.TheWalksBinoculars => GalleryFixtures.BodyFor(LookIn(subject)),
            Beat.TheGalleryVendor => GalleryFixtures.VendorBody,

            // #664 · The adopted eleven read their caption off the same Core plate their title and their
            // painting come from. Not one word of these was retyped here: `KaamosLore.PlateFor` and the nine
            // named constants beside it are still the only place they are written down.
            _ => PlateOf(beat, subject)?.Caption ?? "",
        };
    }
}
