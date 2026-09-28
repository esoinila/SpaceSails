using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE KEEPING — the three vault sections the old crew writes, and restoring them.
///
/// <para>Split out of <c>Map.OldCrew.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening file.
/// <c>ForgetTheOldCrew</c>, the reset, stays home beside the fields it clears.</para>
/// </summary>
public partial class Map
{
    // ── THE KEEPING ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The seeding, as the vault stores it — opaque rows. Null when nothing is cast, so a thread
    /// that has never had a world written writes no section at all.</summary>
    private OldCrewSection? BuildOldCrewSection() =>
        _oldCrew.Count == 0
            ? null
            : new OldCrewSection
            {
                Shipmates = [.. _oldCrew.Select(OldCrew.Stored)],
                // …and which of them this captain has already faced. A reload that replayed the scene would
                // let the player answer the same question twice and write a second crossing for it.
                Explained = [.. _facesExplained],
            };

    /// <summary>The crossings.</summary>
    private CrossingsSection? BuildCrossingsSection() =>
        _crossings.Count == 0 ? null : new CrossingsSection { Crossings = [.. _crossings.Select(c => c.Stored)] };

    /// <summary>The held-memory sheets.</summary>
    private HeldMemoriesSection? BuildHeldMemoriesSection() =>
        _heldMemories.Count == 0 ? null : new HeldMemoriesSection { Sheets = [.. _heldMemories.Select(s => s.Stored)] };

    /// <summary>Read them all back. A row this build cannot parse is dropped rather than thrown over; a
    /// seeding that comes back empty or short is simply re-rolled from the thread id on first read, which
    /// costs nothing because the roll is deterministic and is what an old save does anyway.</summary>
    private void RestoreOldCrewSections(Vault vault)
    {
        var crew = new List<OldCrew.Seeded>();
        foreach (string stored in vault.OldCrew?.Shipmates ?? [])
        {
            if (OldCrew.TryParse(stored, out OldCrew.Seeded s))
            {
                crew.Add(s);
            }
        }

        var crossings = new List<CaptainCrossings.Crossing>();
        foreach (string stored in vault.Crossings?.Crossings ?? [])
        {
            if (CaptainCrossings.Crossing.TryParse(stored, out CaptainCrossings.Crossing c))
            {
                crossings.Add(c);
            }
        }

        var sheets = new List<HeldMemory.Sheet>();
        foreach (string stored in vault.HeldMemories?.Sheets ?? [])
        {
            if (HeldMemory.Sheet.TryParse(stored, out HeldMemory.Sheet sheet))
            {
                sheets.Add(sheet);
            }
        }

        _crossings = crossings;
        _heldMemories = sheets;
        _facesExplained.Clear();
        foreach (string giver in vault.OldCrew?.Explained ?? [])
        {
            _facesExplained.Add(giver);
        }

        _signerReportedFor.Clear();
        _knockedThisVisit.Clear();
        _crewVisitBerth = "";

        if (crew.Count == OldCrew.SeededPerThread)
        {
            _oldCrew = crew;
            _oldCrewSeededFor = _activeThreadId ?? "";
            foreach (OldCrew.Seeded s in crew)
            {
                OldCrew.Shipmate who = OldCrew.ById(s.Id)!.Value;
                _contacts.SeedOldShipmate(OldCrew.LedgerId(s.Id), who.Name, who.Warmth);
            }
        }
        else
        {
            _oldCrewSeededFor = "\0";   // seed on first read, exactly as a pre-#973 save does
        }
    }
}
