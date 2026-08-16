using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// Subject: every ink this deck draws in, #868's furniture family included (part of DeckView).
public sealed partial class DeckView
{
    private static readonly RgbaColor Floor = new(10, 14, 22);
    private static readonly RgbaColor HullLine = new(170, 185, 205);
    private static readonly RgbaColor InnerLine = new(110, 125, 145, 200);

    // #563 · Solid ROCK, as opposed to a made pressure boundary. Same weight as hull because it is just as
    // solid, but warm and dusty rather than cold blue-white — the difference between a monolith and a
    // bulkhead, which is the difference between standing on a moon and standing in a ship.
    // #600 · Paint on poured concrete: worn, low-contrast, and deliberately dimmer than any label that
    // means something is interactable. Signage you read at a glance and then stop seeing.
    // #612 · Owner: "they are kind of hidden now". They were — a dim blue-grey on a dark floor, which is
    // fine for a wall marking and wrong for the one plate that answers WHERE AM I. Facility signage yellow
    // now, and bright enough to be read from across a corridor without hunting for it.
    //
    // ...and he then hit it AGAIN, which is the tell: the fault was never the hue. Ink over a corridor full
    // of hull lines, doors and console glow has little contrast left to raise, because the thing it is
    // competing with is BUSY rather than bright. Text on a busy deck needs a BACKGROUND — which is exactly
    // what #348 concluded for the room labels, and this plate is signage twice their size. So the yellow
    // stays and every painted sign gets its own dark panel (see the BigLabels draw). The way a stairwell
    // actually marks a level is a painted panel, not a brighter stencil.
    private static readonly RgbaColor StencilPaint = new(240, 208, 96, 245);

    // #612 · The dark panel every painted sign sits on, so the deck behind it stops competing.
    private static readonly RgbaColor StencilPlate = new(10, 14, 20, 225);

    /// <summary>You can breathe here — the relief colour, cool and calm. A floor that still holds pressure,
    /// or a #608 refuge cut into one that does not. The SAME green the gauge's own source chip wears,
    /// because a captain who has learned a colour on one instrument must not have to learn it again on the
    /// other.</summary>
    private static readonly RgbaColor StencilAir = new(130, 214, 176, 245);

    /// <summary>And you cannot. The same amber every other "this is costing you" reads in — including the
    /// chip on the suit gauge.</summary>
    private static readonly RgbaColor StencilDead = new(232, 150, 84, 245);

    private static readonly RgbaColor StoneLine = new(166, 150, 130);

    /// <summary>#677 · THE THIRD MATERIAL — the found halls' walls, and the only ink in the game that is not
    /// a fact about anybody.
    ///
    /// <para>Hull is cold blue-white because a bulkhead is metal somebody paid for; stone is warm and dusty
    /// because it is the moon you are standing on. This is neither: a flat, chroma-free grey that belongs to
    /// no palette, no department and no body, drawn heavier than either of them and with no texture,
    /// hatching or interior line-work of any kind. <b>The absence is the style</b> (§13.20), and it is the
    /// same thing #649's slab says by having nothing drawn inside its face.</para>
    ///
    /// <para>Deliberately NOT bright. Owner: <i>"a material the light does not grip"</i> — so it sits below
    /// the hull's value rather than above it, and on a floor where the suit's cone is the whole of the seeing
    /// (#708) that is most of what a captain ever learns about it.</para></summary>
    private static readonly RgbaColor SeamlessLine = new(150, 150, 150);
    private static readonly RgbaColor WindowLine = new(80, 220, 210, 220);
    private static readonly RgbaColor ConsoleGlow = new(120, 220, 200);
    private static readonly RgbaColor ConsoleNear = new(190, 255, 220);
    private static readonly RgbaColor AvatarColor = new(255, 210, 80);
    private static readonly RgbaColor CrateColor = new(200, 160, 90, 220);
    private static readonly RgbaColor ShuttleColor = new(150, 210, 255, 220);
    private static readonly RgbaColor DroidColor = new(150, 160, 180);
    private static readonly RgbaColor ReeverColor = new(230, 80, 70);   // #295: watchdog red

    // #583 · The repo crew. A cold institutional amber, deliberately NOT the Old Ones' red: what is walking
    // toward you matters, and two hostiles that read identically on the map is one hostile with two names.
    // Red is the thing that wants to eat you; amber is the thing that wants your money and has paperwork.
    private static readonly RgbaColor CollectorColor = new(226, 170, 60);

    /// <summary>#538 · A professional reads COLD — instrument white-blue, not the pack's red. Two hostile things
    /// on one deck have to be told apart at a glance, and the colour is the only thing doing that job while a
    /// captain is deciding which way to run.</summary>
    private static readonly RgbaColor SweeperColor = new(150, 205, 235);

    /// <summary>#804 · A GUARD ON A ROUND reads INSTITUTIONAL GREEN — the ink of the thing that wants to see
    /// your paperwork. It is the fifth kind of figure this deck draws and it needs its own colour for the
    /// reason the other four do: the pack is red because it wants to eat you, the repo crew amber because
    /// it wants your money, a professional cold blue because it will shoot you. A guard who read as any of
    /// those would tell a player to run from the one figure in the game whose whole design is that running
    /// is the wrong answer.</summary>
    private static readonly RgbaColor GuardColor = new(120, 200, 150);

    /// <summary>#832 · How much of a figure's ink survives out at the far end of the eye's reach — the
    /// DISTANT FIGURE tier. Faint enough that "I cannot make that out yet" is the honest reading, solid
    /// enough that it is unmistakably somebody: the failure this replaces is a marker that was fully there
    /// one deck unit and gone the next, in open air, with nothing to blame it on.</summary>
    private const double SmearInk = 0.45;

    /// <summary>
    /// #537 · The ship's own structure — closed-cell metal foam and everything packed into it.
    ///
    /// <para><b>BLACK, and that is the point.</b> Owner: <i>"the hatched line should have the line and black bg
    /// under it … I don't want to draw attention to it :-D … so we can hide things more in it."</i> The first
    /// version filled the runs a shade lighter than the deck, and that inverted the original bug rather than
    /// fixing it: instead of a black gap you could see INTO, there was a bright bar announcing exactly where
    /// every hiding place on the ship was. A structure that draws the eye is as bad as one you can see through.
    /// So it is the deck's own black with only the hatch over it — present, structural, and utterly unremarkable
    /// until somebody knocks on it.</para>
    /// </summary>
    private static readonly RgbaColor FoamFill = new(8, 11, 15, 255);

    /// <summary>…and the section hatch over it, barely there. Any brighter and the wall becomes a texture a
    /// player studies, which is the opposite of what it is for — the owner's whole note about this was that it
    /// must not draw attention, because things are meant to hide in it.</summary>
    private static readonly RgbaColor FoamHatch = new(58, 66, 76, 105);
    private static readonly RgbaColor HuskColor = new(120, 70, 60, 150); // #314: a downed Old One's husk
    private static readonly RgbaColor BotColor = new(120, 210, 160);     // #314: a live sentry, gun-green
    private static readonly RgbaColor BotDim = new(90, 100, 110);        // #314: a dry sentry, gone quiet
    private static readonly RgbaColor SegLit = new(255, 90, 70);         // #314: the 99-counter, seven-segment red
    private static readonly RgbaColor SegDim = new(90, 50, 45, 200);     // #314: a frozen 00, dim glyph
    private static readonly RgbaColor SegWarn = new(255, 185, 70);       // #314: magazine under 25 — warming amber
    private static readonly RgbaColor SegAlarm = new(255, 45, 35);       // #314: magazine under 10 — hot alarm red
    private static readonly RgbaColor ZapColor = new(180, 255, 210, 235);// #314: the sentry's zap line
    private static readonly RgbaColor TextDim = new(140, 160, 180, 170);

    // #348 (owner, 2026-07-18: "make these room texts have better contrast … the Med Bay should stand out
    // from the cabins more … make it the shiny clean room that stands out from the bunk rooms. Like the
    // exception that makes the role.. it can look old and used but clean."). The room labels used to draw
    // in the dim grey TextDim, which the cabin art JPGs swallowed. Now every room label rides a subtle
    // dark backing plate (the house sentry-counter / SANITY-plate idiom) under a brighter fill, so the
    // schematic reads over the panels. MED BAY is the deliberate exception — the one clean room among the
    // grubby bunks: a whiter, cooler label on a cleaner plate with a thin cyan-white keyline.
    private static readonly RgbaColor RoomLabelText = new(214, 228, 242, 245);    // brighter than the old TextDim
    private static readonly RgbaColor RoomLabelPlate = new(8, 12, 18, 170);       // subtle dark backing, reads over art
    private static readonly RgbaColor MedBayText = new(240, 250, 255, 252);       // clean-room white, faint cool cast
    private static readonly RgbaColor MedBayPlate = new(16, 26, 32, 165);         // a cleaner, cooler plate than the bunks
    private static readonly RgbaColor MedBayKeyline = new(150, 222, 236, 155);    // the tidy edge — a thin cyan-white keyline
    // #371 Phase 3 · expedition fog-of-war palette. An UNSEEN forced chamber is a dark hatched void (unknown
    // ground behind a freshly-forced door); an EXPLORED one (seen, now out of sight) draws in a cold dim
    // slate; a VISIBLE one draws normally. Echoes ripple in the tracker's own green — "movement was here".
    // #708 · PITCH. Not the deck's near-black Floor (10,14,22) and not an alpha over it: a floor with no
    // fixtures on an airless world has nothing to scatter light, so what the lamp misses is not dark grey,
    // it is nothing. Opaque, so no console glow, no plate and no hull line can bleed through it.
    private static readonly RgbaColor Pitch = new(0, 0, 0, 255);

    // ── #868 · THE FURNITURE INKS ────────────────────────────────────────────────────────────────────────
    //
    // Owner, reading a back-of-house room off the plan: "could the table just be a DIFFERENT COLOR RECTANGLE
    // in front of the chair, so arms (and papers) could rest on it?" — so the whole requirement is that a
    // fixture is not the colour of the floor and not the colour of a wall. Three tones and no more, because
    // the plan is crude on purpose (the TTRPG aesthetic is canon) and a legend nobody was given is not
    // information: a SURFACE you work at, a run of things KEPT, and something you SIT on.
    //
    // Warm against a cold deck. Everything down here is grey-blue stone and cyan glass, so furniture reads as
    // the one warm family on the floor without a single one of them competing with the amber avatar or the
    // yellow stencils, both of which are far brighter.
    private static readonly RgbaColor SurfaceFill = new(96, 78, 58, 235);   // a worktop, a desk, a counter
    private static readonly RgbaColor StorageFill = new(64, 72, 90, 235);   // shelving, racking, a kitchenette
    private static readonly RgbaColor SeatingFill = new(88, 66, 74, 235);   // a bench
    private static readonly RgbaColor FurnitureEdge = new(196, 176, 148, 200);

    private static readonly RgbaColor VoidFill = new(4, 7, 12, 214);
    private static readonly RgbaColor VoidHatch = new(34, 46, 62, 90);
    private static readonly RgbaColor VoidText = new(90, 110, 135, 150);
    private static readonly RgbaColor ExploredWall = new(74, 90, 112, 140);
    private static readonly RgbaColor ExploredText = new(120, 140, 162, 120);
    private static readonly RgbaColor EchoColor = new(120, 200, 150, 255);

    private static readonly RgbaColor DoorShut = new(255, 180, 90, 220);   // amber airlock door, closed
    private static readonly RgbaColor DoorOpen = new(255, 180, 90, 90);    // retracted leaves, faded
    private static readonly RgbaColor DoorLocked = new(120, 140, 170, 210);// another berth's sealed hatch
    private const double DoorOpenRadius = DeckPlan.DoorOpenRadius; // #465: one number, shared with sight
}
