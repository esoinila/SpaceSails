namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// THE CAPTAIN'S SELFIE (issue #400, owner's cruise 2026-07-20): "people take selfies when they travel
// — the awesome-view places in the solar system should have a photo spot. And the captain takes them to
// make people believe he was REALLY there … the frame should place the CAPTAIN in the awesome view, not
// just show the view." (Owner's own habit: a 360° cam on a selfie stick, so every shot is a selfie.)
//
// This is the pure Core spine the client renders around:
//   • the SELFIE SPOTS — a scenic-vista catalog keyed by the same station body ids HavenInterior builds
//     (the ViewObject/plaque console idiom, #392): the Red Eye storm gallery, Ringside's ring-lip,
//     Selene's Earthrise, The Deep's edge. Each carries the vista backdrop and a boastful caption pool;
//   • the PEAK-MOMENT beats — deflection success (#399), surviving a reveal (#391), first monolith sight
//     (#391) — each a one-time themed shot with a vainer caption pool;
//   • the CAPTION PICK — pure and DETERMINISTIC (determinism is law in Core): a spot/beat + a seed always
//     yields the same house-voice line, so a test pins the exact caption and a re-view never re-rolls;
//   • the CAPTURED SELFIE — the JSON-friendly album entry the vault persists per game-thread.
//
// The album itself rides on the thread row (GameThreadInfo.Selfies) — additive registry data preserved
// across every Touch like the born-on stamp, and RESET on succession (CaptainSuccession.Succeed): a NEW
// captain inherits NONE of the old one's selfies. The wall of fame is per-life — a quiet Fail Forward
// beat (owner #398). Homage-not-reproduction: the captain's own #368 avatar is the face in the frame.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>One captured selfie in a captain's legend ledger — the boast they'll show everyone. Plain,
/// JSON-friendly data (a parameterless ctor keeps a garbled album entry from throwing the whole index).
/// <paramref name="SpotId"/> is the stable key a capture dedups on (one shot per spot/beat per life);
/// <paramref name="AvatarIndex"/> is the captain's face at the moment of the shot (so a later succession's
/// portrait never rewrites who was actually there); <paramref name="Kind"/> is "spot" or "beat".</summary>
public sealed record CapturedSelfie(
    string SpotId, string Title, string Caption, string VistaArt, int AvatarIndex, int SimDay, string Kind)
{
    /// <summary>Parameterless default for tolerant JSON round-trips.</summary>
    public CapturedSelfie() : this("", "", "", "", 0, 0, "") { }
}

/// <summary>One scenic photo spot — a vista the captain poses in front of. Keyed by the station body id
/// <c>HavenInterior</c> builds an interior for, so the console places itself off the same table. The
/// <paramref name="VistaArt"/> is the backdrop the captain's portrait disc composites onto; the caption
/// pool is boastful house voice, picked deterministically.</summary>
public sealed record SelfieSpot(
    string BodyId, string SpotId, string ConsoleLabel, string VistaArt, IReadOnlyList<string> Captions);

/// <summary>The stable ids of the peak-moment beat shots (issue #400 §3). Not station-keyed — these fire
/// off a storyboard beat, not a console — so they carry their own ids and caption pools.</summary>
public static class SelfieBeats
{
    /// <summary>"Me, personally, saving Ringside" — a successful asteroid deflection (#399).</summary>
    public const string Deflection = "beat-deflection";

    /// <summary>Standing after a horror reveal that would break a lesser nerve (#391).</summary>
    public const string RevealSurvived = "beat-reveal-survived";

    /// <summary>First human eyes on the monolith (#391 / the ancients).</summary>
    public const string FirstMonolith = "beat-first-monolith";
}

/// <summary>The pure catalog + caption engine for the captain's selfies (issue #400). Authored Core data
/// (repo agreement §9) so the client just walks up, presses E, and the shot + its boast are one tested
/// truth — never hand-rolled UI strings. All picks are deterministic (determinism is law).</summary>
public static class SelfieSpots
{
    // ── The scenic vistas (owner #400: "the awesome-view places … should have a photo spot"). Four ship,
    //    keyed to the outer havens whose windows actually look onto something worth blocking with your
    //    face. The vista art reuses the station's bar backdrop (the spinward window onto space) — no new
    //    asset needed for v1; the captain's portrait disc composites into a corner of it client-side. ──
    private static readonly SelfieSpot[] Spots =
    [
        // THE RED EYE — the storm gallery over Jupiter's Great Red Spot.
        new("red-eye", "spot-red-eye", "📸 SELFIE SPOT · THE EYE", "art/red-eye-bar.jpg",
        [
            "Me, personally, staring down a storm older than the human race. It blinked first. Absolutely no digital trickery — the captain was HERE.",
            "The Great Red Spot has raged four centuries and never once been photographed with someone this good-looking in front of it. Corrected.",
            "Pilgrims cross the whole system to look at the Eye. I gave it fifteen seconds and a better profile than it has ever had.",
        ]),

        // RINGSIDE EXCHANGE — the lip of Saturn's rings.
        new("ringside-exchange", "spot-ringside", "📸 SELFIE SPOT · THE RING-LIP", "art/ringside-bar.jpg",
        [
            "Standing on the lip of Saturn's rings like I built them myself. Frankly, I could have. No trickery — the captain was HERE.",
            "The rings are billions of years old. This photograph is the first thing about them genuinely worth keeping.",
            "Me, personally, at Ringside. The rings are merely the backdrop. I am the point of the picture.",
        ]),

        // SELENE GATE — Earthrise through the oldest gate in the system.
        new("selene-gate", "spot-selene", "📸 SELFIE SPOT · EARTHRISE", "art/selene-gate-bar.jpg",
        [
            "Earthrise over the oldest gate in the system, and me in front of it — because history needed a face and volunteered mine.",
            "Home, a blue marble in the window, and me blocking most of it. Priorities. The captain was HERE.",
            "Me, personally, at Selene Gate, where they've thrown rocks at the sky since 2119. None of them ever landed this well.",
        ]),

        // THE DEEP — the edge of the charts, off Neptune.
        new("the-deep", "spot-the-deep", "📸 SELFIE SPOT · THE EDGE", "art/the-deep-bar.jpg",
        [
            "The end of the charts, the last port before the dark — and me, obviously, further out than anyone worth photographing has ever been.",
            "Me, personally, at the edge of the known system. The dark goes on forever. So, the record shows, does my nerve.",
            "They towed this place from Mercury and sealed the reason. I flew here for the photo. Absolutely no digital trickery — the captain was HERE, at the end of everything.",
        ]),
    ];

    // ── The peak-moment beat pools (owner #400 §3: "the vainer the caption, the better"). ──
    private static readonly Dictionary<string, (string Label, IReadOnlyList<string> Captions)> Beats = new()
    {
        [SelfieBeats.Deflection] = ("📸 THE HERO SHOT",
        [
            "Me, personally, saving Ringside — one hand on the plunger, one eye on the lens. The rock never stood a chance, and neither did the shot.",
            "They'll say a whole crew turned that stone aside. Look closely at the frame: it was mostly me, and here is the proof.",
            "The asteroid came for the Exchange. I detonated it, then posed. In that order, barely. Absolutely no digital trickery.",
        ]),
        [SelfieBeats.RevealSurvived] = ("📸 STILL STANDING",
        [
            "I looked the thing in the eye and I am still here. Here is my face doing it — steady as a docking clamp. No trickery.",
            "Whatever that was, it did not take me. Documented, dated, and frankly my best angle under pressure.",
            "Me, personally, surviving what breaks lesser captains. Say it happened all you like — I have the photograph.",
        ]),
        [SelfieBeats.FirstMonolith] = ("📸 FIRST CONTACT",
        [
            "First human eyes on the monolith, and the first to think: this needs me in the frame.",
            "Me, personally, meeting the ancients. They said nothing. I smiled anyway, for the record.",
            "The oldest thing in the system, and now the second-oldest — because I am clearly in front of it. The captain was HERE.",
        ]),
    };

    /// <summary>The selfie spot at a station body, or null if that berth has no scenic vista.</summary>
    public static SelfieSpot? For(string? bodyId) =>
        bodyId is null ? null : Array.Find(Spots, s => s.BodyId == bodyId);

    /// <summary>Does this station have a scenic photo spot (so <c>HavenInterior</c> should place a console)?</summary>
    public static bool HasSpot(string? bodyId) => For(bodyId) is not null;

    /// <summary>Every authored selfie spot — for tests and any "where can I pose" listing.</summary>
    public static IReadOnlyList<SelfieSpot> AllSpots => Spots;

    /// <summary>The console label + caption pool for a peak-moment beat, or null for an unknown beat id.</summary>
    public static (string Label, IReadOnlyList<string> Captions)? Beat(string beatId) =>
        Beats.TryGetValue(beatId, out (string Label, IReadOnlyList<string> Captions) b) ? b : null;

    /// <summary>Deterministically pick one caption from a pool for a <paramref name="seed"/> (typically the
    /// thread id folded with the spot id) — pure, so the same captain at the same spot always boasts the
    /// same line and a re-view never re-rolls. Empty pool → empty string.</summary>
    public static string PickCaption(IReadOnlyList<string> pool, string seed)
    {
        ArgumentNullException.ThrowIfNull(pool);
        if (pool.Count == 0)
        {
            return "";
        }

        return pool[(int)(Fnv1a(seed ?? "") % (uint)pool.Count)];
    }

    /// <summary>Compose the captured selfie for a station photo spot: the deterministic caption, the vista
    /// backdrop, and the captain's face at the moment of the shot. Pure — the client persists the result
    /// into the album. Null if the body has no spot.</summary>
    public static CapturedSelfie? CaptureAt(string bodyId, string threadId, int avatarIndex, int simDay)
    {
        if (For(bodyId) is not { } spot)
        {
            return null;
        }

        string caption = PickCaption(spot.Captions, $"{threadId}|{spot.SpotId}");
        return new CapturedSelfie(spot.SpotId, spot.ConsoleLabel, caption, spot.VistaArt,
            avatarIndex, Math.Max(0, simDay), "spot");
    }

    /// <summary>Compose the captured selfie for a peak-moment beat: the deterministic themed caption, an
    /// optional vista backdrop (the beat's setting), and the captain's face. Pure. Null for an unknown
    /// beat id.</summary>
    public static CapturedSelfie? CaptureBeat(string beatId, string threadId, int avatarIndex, int simDay,
        string vistaArt = "")
    {
        if (Beat(beatId) is not { } b)
        {
            return null;
        }

        string caption = PickCaption(b.Captions, $"{threadId}|{beatId}");
        return new CapturedSelfie(beatId, b.Label, caption, vistaArt ?? "", avatarIndex, Math.Max(0, simDay), "beat");
    }

    // FNV-1a (32-bit), the same stable, process-independent hash the roster identity uses — so a caption
    // pick is canon across reloads and machines, never per-run noise.
    private static uint Fnv1a(string s)
    {
        uint h = 2166136261u;
        foreach (char c in s)
        {
            h = (h ^ c) * 16777619u;
        }

        return h;
    }
}
