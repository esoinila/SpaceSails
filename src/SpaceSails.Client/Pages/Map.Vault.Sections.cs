using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Vault — the three section builders that are pure reads of one list apiece.
//
// #870 · MOVED HERE BY PURE MOTION, and the reason is the size gate rather than taste. `Map.Vault.cs` came
// within twenty-three lines of the 1500-line line, which reddens `NoSourceFileIsTooLongTests`' own
// self-check: a threshold resting on a real case is a threshold that reddens on a typo, and the gate says
// so in as many words. #948 had already split `Map.Logbook.cs` off this same file for this same reason.
//
// The block below is the block that was in `Map.Vault.cs`, character for character — three builders and the
// one const that belongs to the middle of them. Nothing was rewritten, nothing was renamed, and every
// source guard that reads `Map.Vault.cs` for a line of `BuildVault`'s or `ApplyVault`'s body still finds it
// there: none of them asserts about anything moved here.
public partial class Map
{
    private QuestsSection BuildQuestsSection()
    {
        var quests = _quests.Select(q =>
        {
            var fields = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(q.TargetShipId)) fields["targetShipId"] = q.TargetShipId;
            if (!string.IsNullOrEmpty(q.TargetCallsign)) fields["targetCallsign"] = q.TargetCallsign;
            if (q.DestBodyId is { } dest) fields["destBodyId"] = dest;
            if (q.SourceBodyId is { } src) fields["sourceBodyId"] = src;
            if (q.Pin is { } pin) fields["pin"] = pin;

            return new QuestRecord
            {
                Id = q.Id,
                Kind = q.Kind.ToString(),
                Status = q.State.ToString(),
                Title = q.Title,
                Detail = q.Blurb,
                GiverContactId = q.Giver,
                RewardCredits = q.Reward,
                Fields = fields,
            };
        }).ToList();

        return new QuestsSection { Quests = quests, Obligations = VaultMapper.ToRecords(_favorObligations) };
    }

    // The persistent dice items (TTRPG helpers). Today only the boarding-nets jammer exists (the
    // dice-helper seam, #222); it saves as a labelled +2 modifier so the section is future-proof.
    private const string NetJammerItemId = "boarding-nets-jammer";

    private DiceItemsSection BuildDiceItemsSection()
    {
        var items = new List<DiceItemRecord>();
        if (_hasNetJammer)
        {
            items.Add(new DiceItemRecord(NetJammerItemId, "Boarding-nets jammer", 2));
        }

        return new DiceItemsSection(items);
    }

    // The resume berth: docked haven if clamped, else the nearest dockable haven at save time (never a
    // trajectory). Positions are read at the current sim time so a load rebuilds the ship clamped at
    // the load-time ephemeris.
    private ResumeSection? BuildResumeSection()
    {
        if (_ephemeris is null)
        {
            return null;
        }

        var havens = _ephemeris.Bodies
            .Where(IsDockableHaven)
            .Select(b => new VaultResume.HavenLocus(b.Id, b.Name, _ephemeris.Position(b.Id, _ship.SimTime)))
            .ToList();

        return VaultResume.Select(_dockedHavenId, _ship.Position, havens);
    }
}
