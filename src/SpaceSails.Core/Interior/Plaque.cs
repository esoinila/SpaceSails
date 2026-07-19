namespace SpaceSails.Core.Interior;

/// <summary>
/// A commemorative plaque — the ship's builder's plate and every haven's dedication plaque
/// (worldbuilding depth). Pure authored Core data (repo agreement §9) so the client just walks up,
/// presses E, and pops the plate's image + its dedication in the house voice; the text is one tested
/// truth, not hand-rolled UI strings.
///
/// <para>Owner ruling (the cruise, 2026-07-19, photographing their ship's Aker Finnyards builder's
/// plate): <i>"We could gen-AI the ships and docks some space-dock plaques … add some depth to the
/// world (worldbuilding)."</i></para>
///
/// <para>Owner addendum (2026-07-19, aboard the Victoria I): <i>"This Victoria I once sailed as
/// premium roro ship between Finland and Sweden, then it was bought by Tallink and now shows its age,
/// though the history is still there of its glory days."</i> — so a ship's builder's-plate card
/// carries a SERVICE HISTORY: the hull USED to be something. That is the canon these cards establish.</para>
///
/// <para>Owner worldbuilding addendum (2026-07-19, watching the Victoria I's rusted old radar bolted
/// beside its new domed one): <i>"Old tech was left rusting and not rotating as domed new radar was
/// installed next to it. Having the station built somewhere unexpected and far away adds mystique. Why
/// is it here now. What was that strange project name at ice moon that is mentioned. Local build is not
/// so surprising but good so the exceptions stand out. Safety equipment is also cool. Lifeboats at
/// station maybe."</i> Woven in text-only: (1) LAYERED TECH ERAS — dead old gear left honored beside
/// its replacement; (2) BUILD ORIGIN — most ports say plainly they were built locally, so the ONE
/// exception (The Deep, towed from sunside) carries the mystique; (3) THE ICE-MOON PROJECT — a sealed
/// Saturn-moon project, <b>PROJEKTI KAAMOS</b> (Finnish: the polar night), named on Ringside and echoed
/// unnamed on The Deep, never explained anywhere (Enceladus is shuttle-unreachable — the plate is the
/// only place it surfaces); (4) LIFEBOATS — a muster card, <see cref="LifeboatMuster"/>.</para>
/// </summary>
public sealed record Plaque(string Id, string ConsoleLabel, string ArtUrl, string Lore);

/// <summary>
/// The commemorative plaques of the world (owner's cruise ruling, 2026-07-19). One <see cref="Ship"/>
/// builder's plate bolted to the player's own hull, and one dedication plaque per walkable haven —
/// keyed by the same station body ids <c>HavenInterior</c> builds interiors for. Selene Gate, The Red
/// Eye, and The Deep carry Grok-generated plate art; the other four are wired to their easel slot and
/// fall back to just the dedication text until the art lands (follow-up lane).
/// </summary>
public static class Plaques
{
    /// <summary>
    /// The player's ship — her builder's plate and service history (canon set here). Koski &amp;
    /// Daughters Orbital Yards, Rauma Crater, Luna; Hull No. 77, laid down 2341. The service-history
    /// beat is the Victoria I addendum: she used to be something, and the wear is the proof.
    /// </summary>
    public static readonly Plaque Ship = new(
        "ship", "⚜ BUILDER'S PLATE", "art/plaque-ship.jpg",
        "Koski & Daughters Orbital Yards — Rauma Crater, Luna. Hull No. 77, laid down 2341. " +
        "In her glory days she ran the premium Luna–Mars mail packet, her name lit on every departures " +
        "board from Selene Gate to the Roadstead — fast, proud, and always on the tick. The shine wore " +
        "off; she was sold on, and sold on again, each owner asking a little more of her and spending a " +
        "little less. She shows her age now, but the history is still in her frames. Her keel plate — " +
        "every scratch since the last owner is yours.");

    // The haven dedication plaques. Selene Gate / The Red Eye / The Deep carry delivered Grok plate art;
    // the Roadstead, Cinder Roost, Ringside and Tilt slots point at their easel path and fall back to the
    // dedication text alone until that art is painted (the souvenir onerror-hide fallback idiom). BUILD
    // ORIGIN is stated plainly for the locally-built ports so The Deep's towed-from-sunside exception
    // stands out (owner addendum, 2026-07-19).
    private static readonly Plaque[] Stations =
    [
        // The Rusty Roadstead — Mars, the crossroads where the long-haul crews lay over. Built locally
        // (plain), with a layered-tech beat. (Text-only for now.)
        new("the-space-bar", "⚜ DEDICATION PLAQUE", "art/plaque-roadstead.jpg",
            "THE RUSTY ROADSTEAD — Mars orbit, the roadstead where every long-haul crew lays over and " +
            "half of them never quite leave. Raised in Mars orbit from Mars iron, plate over plate, the " +
            "way a roadstead should be. The first mooring mast still stands out on the ring, dead and " +
            "unlit, honored where it froze — nobody's taken it down in two hundred years. Most guests " +
            "stay two weeks. This plate has kept its post since the first two weeks."),

        // Cinder Roost — Venus cloud-mining (worldbuilding notes §Venus/He3 pantry). Built locally.
        // (Text-only for now.)
        new("cinder-roost", "⚜ DEDICATION PLAQUE", "art/plaque-cinder.jpg",
            "CINDER ROOST — slung in the high acid cloud-deck of Venus, where the skimmer rigs work the " +
            "sulphur haze for what the furnace-world can be made to spare. Built in these clouds by the " +
            "crews that mine them, from hull stock the rigs floated up themselves — nothing here came from " +
            "anywhere else, and it shows in the welds. Mind the sulphur, spacer. The Roost was raised by " +
            "people who learned to breathe carefully and charge accordingly."),

        // Ringside Exchange — Saturn, the He3 clearing-house (worldbuilding notes §4). Built locally, and
        // the one place PROJEKTI KAAMOS is named — the sealed ice-moon (Enceladus) project, never
        // explained (owner addendum, 2026-07-19). (Text-only for now.)
        new("ringside-exchange", "⚜ DEDICATION PLAQUE", "art/plaque-ringside.jpg",
            "RINGSIDE EXCHANGE — hung at the lip of Saturn's rings, clearing-house for the He3 that fuels " +
            "the whole out-system trade. Forged here from ring-ice and Saturn steel, in the shadow of the " +
            "planet it serves. Her first commission was the KAAMOS supply run out to the ice moon; the " +
            "berth for it is still on the board, still listed, and nobody has filed for it in a long time. " +
            "Trade fast — the rings don't wait. Some cargoes, it seems, are content to."),

        // The Tilt — Uranus, the sideways world. Built locally, with a layered-tech beat. (Text-only.)
        new("the-tilt", "⚜ DEDICATION PLAQUE", "art/plaque-tilt.jpg",
            "THE TILT — in orbit off Uranus, the world that fell over and kept on going. Bolted together " +
            "right here, sideways like everything else this far out, by crews who stopped noticing which " +
            "way was up. The old horizon gyro still spins in its cage by the lock, thirty years dead and " +
            "never removed — it lies about down, but it lies the same way every time, so they keep it. " +
            "Everything's sideways out here. The Tilt made a home of it, and dares you to stand up straight."),

        // Selene Gate — Luna, the first port of the Moon, the mass-driver age (worldbuilding notes §1).
        // Built locally (of course — it IS the Moon), with a layered-tech beat. Grok plate art delivered.
        new("selene-gate", "⚜ DEDICATION PLAQUE", "art/plaque-selene.jpg",
            "SELENE GATE — commissioned 2119, the first port ever cut into the Moon, out of the Moon's own " +
            "grey stone. From these locks the mass-drivers threw Luna's ore sunward, ton upon ton, and " +
            "every hull that flies today was poured from what they flung. The original beacon mast still " +
            "stands dark beside the array that replaced it — the oldest gate keeps its old bones. " +
            "Dedicated to the driver crews who built the sky by throwing rocks at it."),

        // The Red Eye — Jupiter, the storm-watcher port; the pilgrim tradition, and the Victoria-I radar
        // image made literal (the dead storm-glass beside the live dome). Grok plate art delivered.
        new("red-eye", "⚜ DEDICATION PLAQUE", "art/plaque-redeye.jpg",
            "THE RED EYE — raised in Jupiter orbit, from Jovian yard-stock, to keep the long watch on the " +
            "Great Red Spot. The first storm-glass still hangs dead in its cradle beside the dome that " +
            "took its place — nobody rotates it now, and nobody will take it down. Four centuries the storm " +
            "has turned, and longer still, they say, it will turn after us; pilgrims touch this plate before " +
            "they look, and touch it again before they leave. No storm outlasts the watcher."),

        // The Deep — Neptune, the last port; the road ends, the watch goes on. THE BUILD-ORIGIN EXCEPTION:
        // not built here — assembled sunside and towed the whole system out, reason sealed. Carries the
        // unnamed echo of the ice-moon berth (KAAMOS, named only on Ringside). Grok plate art delivered.
        new("the-deep", "⚜ DEDICATION PLAQUE", "art/plaque-deep.jpg",
            "THE DEEP — the last port in the system, moored in Neptune's cold orbit where the charts run " +
            "out. This hall was not built here. Its frame was assembled sunside, in the Mercury yards, and " +
            "towed out on a slow-haul that outlived the crew who launched it — the reason is filed under " +
            "seal, and the seal has held. Its manifest still lists a berth at the ice moon; nobody files " +
            "for that one either. The road ends here. The watch goes on."),
    ];

    /// <summary>The dedication plaque for a station body, or null if that berth has no walkable haven.</summary>
    public static Plaque? For(string bodyId) => Array.Find(Stations, p => p.Id == bodyId);

    /// <summary>Every haven dedication plaque — for tests and any "what's dedicated where" listing.</summary>
    public static IReadOnlyList<Plaque> AllStationPlaques => Stations;

    // --- Lifeboats (owner addendum, 2026-07-19: "Safety equipment is also cool. Lifeboats at station
    //     maybe."). A muster card on every hall — one addition seeds all ports, like the plaques. The
    //     "LAST INSPECTED" date is seeded per port (deterministic, comfortably stale) so each reads as its
    //     own neglected bay, and the asterisk carries the whole joke. Text-only for now (art is a follow-up
    //     easel item).

    /// <summary>The lifeboat-bay console/wall label (the muster point in every haven hall).</summary>
    public const string LifeboatLabel = "🛟 LIFEBOAT STATION";

    /// <summary>The lifeboat muster card for a haven, with a deterministic, comfortably-stale "last
    /// inspected" date seeded off the station id — so the bay reads as its own long-uninspected corner,
    /// and never flickers between sessions. The asterisk does the work.</summary>
    public static string LifeboatMuster(string bodyId)
    {
        uint h = StableHash(bodyId);
        int year = 2333 + (int)(h % 8u);          // 2333..2340 — stale against a ~2341+ "now"
        int month = 1 + (int)(h / 8u % 12u);      // 1..12
        int day = 1 + (int)(h / 128u % 28u);      // 1..28 (calendar-safe)
        string date = $"{day:D2}.{month:D2}.{year}";
        return $"LIFEBOAT MUSTER · CAPACITY 40 · LAST INSPECTED {date}. In the event of pressure loss, " +
               "proceed calmly to the boats and strap in — they have never once been needed.* " +
               "(*Records prior to the current management are unavailable.)";
    }

    // A small stable (process-independent) hash so seeded dates are canon, not per-run noise — FNV-1a.
    private static uint StableHash(string s)
    {
        uint h = 2166136261u;
        foreach (char c in s)
        {
            h = (h ^ c) * 16777619u;
        }
        return h;
    }
}
