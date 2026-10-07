using System;
using System.Collections.Generic;
using SpaceSails.Core;
using SpaceSails.Core.Tests;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1376 · THE LAST DOOR. <c>ApplyVault</c> only overwrote a field when its saved section was PRESENT, and BuildVault
/// writes an empty list as null (the checksum rule), so loading or importing a thread whose list was empty kept the
/// PREVIOUS life's list, and re-installed it into the static Core registers. Plant in life A, apply a vault built from
/// a fresh life (every one of those sections absent), and everything must be clean, Core's readers included.
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class TheArrivingLifeArrivesAloneTests
{
    private const string Ground = "arrival-ground";

    private static Vault TheBlankLifesVault()
    {
        Pages.Map fresh = Boot("arrival-blank");
        return VaultSerializer.Load(VaultSerializer.Save((Vault)Invoke(fresh, "BuildVault", "", "")!))
            ?? throw new InvalidOperationException("the blank vault did not survive its own serializer.");
    }

    /// <summary>true: the blank life's vault read back through the serializer (sections present but empty); false: a vault
    /// with every section ABSENT, the shape a thread that never wrote the list arrives in.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ALoadedLifeWithEmptyListsDoesNotInheritTheLastLifesLists(bool throughTheSerializer)
    {
        try
        {
            Vault blank = throughTheSerializer ? TheBlankLifesVault() : new Vault();

            // The honest expectation, taken BEFORE life A so its register installs cannot be wiped: what a clean page answers
            // for the shore-leave count after this same vault (loading with no resume section docks, which counts a berth).
            Pages.Map reference = Boot("arrival-reference");
            Invoke(reference, "ApplyVault", blank);
            int? theBlankAnswer = ((Vault)Invoke(reference, "BuildVault", "", "")!).Progress?.WorkingStopsSinceShoreLeave;

            Pages.Map map = Boot("arrival-life-a");
            Invoke(map, "ApplyVault", new Vault
            {
                Progress = new ProgressSection
                {
                    OddBooksRead = ["shelf-1"],
                    SecretLabsFound = [Ground],
                    HallsOpened = [new HallOpeningRecord(Ground, 3)],
                    HallsBuried = [Ground],
                    HallsDeclined = [new HallDeclineRecord(Ground, 3)],
                    HallsHandled = [new QuietHandRecord(Ground, 3, true)],
                    HallsStopped = [Ground],
                    HallsPreserved = [Ground],
                    WorkingStopsSinceShoreLeave = 4,
                },
            });
            Set(map, "_finderCase", default(FinderCase.Case));
            Set(map, "_finderProgress", new FinderCase.Progress(true, false, false, false, false, false, default, false));
            var lifeA = (Vault)Invoke(map, "BuildVault", "", "")!;
            Assert.True(lifeA.Progress!.OddBooksRead.Count > 0, "control: odd books");
            Assert.True(lifeA.Progress.HallsOpened is { Count: > 0 });
            Assert.NotNull(lifeA.Finder);
            Assert.True(lifeA.Progress.WorkingStopsSinceShoreLeave >= 4, "control: shore-leave count");
            Assert.True(Burial.IsFilled(Ground), "control: burial register");
            Assert.True(PoliteDecline.On(Ground), "control: decline register");
            Assert.True(QuietHands.On(Ground), "control: quiet hands register");
            Assert.True(StopOrder.On(Ground), "control: stop register");
            Assert.True(PreservationZone.On(Ground), "control: preservation register");

            Invoke(map, "ApplyVault", blank);

            var after = (Vault)Invoke(map, "BuildVault", "", "")!;
            var carried = new List<string>();
            if (after.Progress?.OddBooksRead is { Count: > 0 }) { carried.Add("_oddBooksRead"); }
            if (after.Progress?.SecretLabsFound is { Count: > 0 }) { carried.Add("_secretLabsFound"); }
            if (after.Progress?.HallsOpened is { Count: > 0 }) { carried.Add("_hallsOpened"); }
            if (after.Progress?.HallsBuried is { Count: > 0 }) { carried.Add("_hallsBuried"); }
            if (after.Progress?.HallsDeclined is { Count: > 0 }) { carried.Add("_hallsDeclined"); }
            if (after.Progress?.HallsHandled is { Count: > 0 }) { carried.Add("_hallsHandled"); }
            if (after.Progress?.HallsStopped is { Count: > 0 }) { carried.Add("_hallsStopped"); }
            if (after.Progress?.HallsPreserved is { Count: > 0 }) { carried.Add("_hallsPreserved"); }
            if (after.Progress?.WorkingStopsSinceShoreLeave != theBlankAnswer) { carried.Add("_workingStopsSinceShoreLeave"); }
            if (after.Finder is not null) { carried.Add("_finderCase"); }
            if (Burial.IsFilled(Ground)) { carried.Add("core:burial"); }
            if (PoliteDecline.On(Ground)) { carried.Add("core:decline"); }
            if (QuietHands.On(Ground)) { carried.Add("core:quiet-hands"); }
            if (StopOrder.On(Ground)) { carried.Add("core:stop"); }
            if (PreservationZone.On(Ground)) { carried.Add("core:preservation"); }
            Assert.True(carried.Count == 0, "the last life rode into the arriving one: " + string.Join(", ", carried));
        }
        finally
        {
            Burial.Install(null, null);
            PoliteDecline.Install(null);
            QuietHands.Install(null);
            StopOrder.Install(null);
            PreservationZone.Install(null);
        }
    }
}
