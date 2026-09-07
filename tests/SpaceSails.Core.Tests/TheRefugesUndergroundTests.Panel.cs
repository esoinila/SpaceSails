using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #608 · <b>ONE ANSWER, EVERYWHERE IT IS ASKED</b> — the fourth part of
/// <see cref="TheRefugesUndergroundTests"/>.
///
/// <para>What this part owns is the seal as the rest of the game sees it: the drawn ROOM agreeing with the
/// law about its own door, the same seal being found on every visit, and the two surfaces that quote it —
/// the panel, which says a refuge is THERE and never whether it holds, and the dead-air card, which stopped
/// promising what the building cannot keep.</para>
/// </summary>
public sealed partial class TheRefugesUndergroundTests
{
    [Fact]
    public void TheRoomAgreesWithTheLawAboutItsOwnDoor()
    {
        // One answer, everywhere. The suit asks StateOfTheRefugeOn, the tracker asks it, the panel asks it,
        // and the ROOM the renderer draws carries the answer it was built with — so nothing on screen can
        // report a seal the sim is not running. A room holding a second opinion about its own door is this
        // repo's oldest and most expensive shape.
        AuditEveryFloor((body, level, floor) =>
        {
            UndergroundComplex.RefugeState? law = UndergroundComplex.StateOfTheRefugeOn(body, level);
            if (law is null)
            {
                return floor.Refuges.Count == 0 ? null
                    : "the law says this floor has no refuge and the generator built one.";
            }
            foreach (UndergroundComplex.Refuge r in floor.Refuges)
            {
                if (r.State != law)
                {
                    return $"the room says {r.State} and the law says {law}.";
                }
            }
            return floor.Refuges.Count > 0 ? null
                : "the law marks a refuge on the plan and the generator built none.";
        }, "the room the renderer draws carries the seal the suit is reading");
    }

    [Fact]
    public void TheSameSealIsFoundOnEveryVisit()
    {
        // A captain is meant to LEARN a building (the same reason the refuge itself never moves). A seal
        // that was re-rolled per ride would be worse than a random one, because the captain would have
        // walked back to a room that worked yesterday.
        foreach (string body in new[] { "miranda", "titan", "generated-moon-7", "generated-moon-61" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                Assert.Equal(
                    UndergroundComplex.StateOfTheRefugeOn(body, level),
                    UndergroundComplex.StateOfTheRefugeOn(body, level));
            }
        }
    }

    [Fact]
    public void ThePanelSaysARefugeIsThereAndNeverWhetherItHolds()
    {
        // #608's hardest requirement is that the captain can learn about the air BEFORE they need it — "a
        // refuge you discover AFTER you needed it is a cruelty" — and the panel is where a captain looks
        // before a ride. So the row carries the plan's own fact, and the plan is a drawing made when the
        // building was new: it knows the room is there and it does not know whether the compressor turns.
        var shapesByState = new Dictionary<UndergroundComplex.RefugeState, HashSet<string>>();
        int tagged = 0, plain = 0;

        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.LiftStop row in UndergroundComplex.LiftPanel(
                    body, level, UndergroundComplex.ShaftKind.Cage, []))
                {
                    Assert.True(row.HasRefuge == UndergroundComplex.RefugeOnThePlan(body, row.Level),
                        $"{body} B{-row.Level}: the row says HasRefuge={row.HasRefuge} and the plan says "
                        + $"{UndergroundComplex.RefugeOnThePlan(body, row.Level)}.");

                    if (row.HasRefuge) { tagged++; } else { plain++; }

                    // …AND THE ROW LOOKS THE SAME IN ALL THREE STATES. Every drawable thing about the button
                    // is written down and grouped by the seal behind it; if any of it leaked the state, one
                    // state would own a shape the others do not have.
                    if (UndergroundComplex.StateOfTheRefugeOn(body, row.Level) is not { } state)
                    {
                        continue;
                    }
                    string shape = string.Join('|',
                        row.HasRefuge, row.Pressurised, row.IsCurrent,
                        row.Refusal is null, row.OpenedBy is null, row.OpenedByChit);
                    if (!shapesByState.TryGetValue(state, out HashSet<string>? set))
                    {
                        shapesByState[state] = set = [];
                    }
                    set.Add(shape);
                }
            }
        }

        Assert.True(tagged > 500, $"only {tagged} tagged row(s) — this sweep proved little.");
        Assert.True(plain > 100, $"only {plain} untagged row(s) — the negative case is untested.");
        Assert.Equal(3, shapesByState.Count);   // all three seals really appear on the panel

        foreach (UndergroundComplex.RefugeState state in shapesByState.Keys)
        {
            foreach (UndergroundComplex.RefugeState other in shapesByState.Keys)
            {
                foreach (string shape in shapesByState[state])
                {
                    Assert.True(shapesByState[other].Contains(shape),
                        $"a row shape ({shape}) occurs behind a {state} seal and never behind a {other} "
                        + "one — the panel has started telling the captain which refuges work, and the walk "
                        + "to find out is the whole feature.");
                }
            }
        }

        // The tag itself says the room and nothing about the room's state.
        foreach (string word in new[] { "AIR", "SEAL", "EMPTY", "FAILED", "WORK", "DEAD" })
        {
            Assert.DoesNotContain(word, UndergroundComplex.RefugeRowTag, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TheDeadAirCardStopsPromisingWhatTheBuildingCannotKeep()
    {
        // #608's last "Done when": the card said "there are no shelters down here" before the refuges
        // existed, and the day they landed that became the most dangerous sentence in the game. It now says
        // the opposite — and, since only a fifth of the racks have anything in them, it may not let the
        // captain read the good news as air either.
        int cards = 0;
        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.HoldsPressure(body, level))
                {
                    continue;
                }
                string card = UndergroundComplex.VacuumCard(body, level, 900);
                cards++;
                Assert.Contains(
                    "The plan marks a refuge on this band. Whether it still holds is not on the plan.",
                    card, StringComparison.Ordinal);
                Assert.DoesNotContain("no shelters", card, StringComparison.OrdinalIgnoreCase);
            }
        }
        Assert.True(cards > 500, $"only {cards} dead-air card(s) read — this sweep proved little.");
    }
}
