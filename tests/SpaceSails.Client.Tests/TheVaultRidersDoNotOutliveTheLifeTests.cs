using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1374 · THE RESET AUDIT. <c>ResetLiveStateForNewGame</c> is "the exact inverse of BuildVault", and #1372/#1373
/// found the tag register missing from it. This is the same guard for every other field BuildVault writes that
/// the reset never named: one case per field, planted through the vault's own writer (<c>ApplyVault</c> of a lived
/// vault, so the plant is the load path and not a private poke), a control that the old life's vault carries it, the
/// reset, and then the first vault of the fresh thread must carry none of it.
/// </summary>
public sealed class TheVaultRidersDoNotOutliveTheLifeTests
{
    private static readonly FieldNote Note = new("a plate under the dust", 100.0, "luna", "N");

    /// <summary>name, the old life's vault, and whether a vault carries the old life's rider.</summary>
    public static IEnumerable<object[]> Riders()
    {
        static object[] Row(string name, Vault lived, Func<Vault, bool> carries) => [name, lived, carries];

        string thread = CaseThreads.Draw([], "alpha", "beta").Select(t => t.Stored).Single();
        string chip = Satchel.Add([], CompromisingChip.Found()).Single().Stored;
        string hut = GroundMemory.HutKey("luna", "s", SurfaceTiles.At(1, 1), GroundMemory.HutChange.Forced);
        string papers = new WalletChoice.Shown("badge:luna", "luna", -2, default).Stored;

        yield return Row("_fieldNotes", new Vault { FieldNotes = new FieldNotesSection { Notes = [Note] } }, v => v.FieldNotes is not null);
        yield return Row("_caseThreads", new Vault { CaseThreads = new CaseThreadsSection { Threads = [thread] } }, v => v.CaseThreads is not null);
        yield return Row("_satchel", new Vault { Satchel = new SatchelSection { Items = [chip] } }, v => v.Satchel is not null);
        yield return Row("_workedUp", new Vault { WorkedUp = new WorkedUpSection { Sheets = ["sheet:one"] } }, v => v.WorkedUp is not null);
        yield return Row("_oddBooksRead", new Vault { Progress = new ProgressSection { OddBooksRead = ["shelf-1"] } }, v => v.Progress!.OddBooksRead.Count > 0);
        yield return Row("_secretLabsFound", new Vault { Progress = new ProgressSection { SecretLabsFound = ["luna"] } }, v => v.Progress!.SecretLabsFound.Count > 0);
        yield return Row("_groundMemory", new Vault { Ground = new GroundSection { Changed = [hut] } }, v => v.Ground is not null);
        yield return Row("_hallsOpened", new Vault { Progress = new ProgressSection { HallsOpened = [new HallOpeningRecord("luna", 3)] } }, v => v.Progress!.HallsOpened is not null);
        yield return Row("_hallsBuried", new Vault { Progress = new ProgressSection { HallsBuried = ["luna"] } }, v => v.Progress!.HallsBuried is not null);
        yield return Row("_hallsDeclined", new Vault { Progress = new ProgressSection { HallsDeclined = [new HallDeclineRecord("luna", 3)] } }, v => v.Progress!.HallsDeclined is not null);
        yield return Row("_emptySealSpentOn", new Vault { Progress = new ProgressSection { EmptySealSpentOn = "luna:door" } }, v => v.Progress!.EmptySealSpentOn is not null);
        yield return Row("_observationWalkSpentOn", new Vault { Progress = new ProgressSection { ObservationWalkSpentOn = "luna:someone" } }, v => v.Progress!.ObservationWalkSpentOn is not null);
        yield return Row("_observationWalkSightingAt", new Vault { Progress = new ProgressSection { ObservationWalkSightingAt = "luna" } }, v => v.Progress!.ObservationWalkSightingAt is not null);
        yield return Row("_shuttle", new Vault { Progress = new ProgressSection { Shuttle = new ReturningShuttle.Row("luna", 50.0) } }, v => v.Progress!.Shuttle is not null);
        yield return Row("_workingStopsSinceShoreLeave", new Vault { Progress = new ProgressSection { WorkingStopsSinceShoreLeave = 4 } }, v => v.Progress!.WorkingStopsSinceShoreLeave is not null);
        yield return Row("_voidDeclaredDay", new Vault { Void = new VoidSection { DeclaredDay = 5, LastToldDay = 3 } }, v => v.Void is not null);

        // Not on the issue's list: the same class, found while reading the whole of BuildVault against the reset.
        yield return Row("_hallsHandled", new Vault { Progress = new ProgressSection { HallsHandled = [new QuietHandRecord("luna", 3, true)] } }, v => v.Progress!.HallsHandled is not null);
        yield return Row("_hallsStopped", new Vault { Progress = new ProgressSection { HallsStopped = ["luna"] } }, v => v.Progress!.HallsStopped is not null);
        yield return Row("_hallsPreserved", new Vault { Progress = new ProgressSection { HallsPreserved = ["luna"] } }, v => v.Progress!.HallsPreserved is not null);
        yield return Row("_shuttleSeen", new Vault { Progress = new ProgressSection { ShuttleSeen = ["luna"] } }, v => v.Progress!.ShuttleSeen is not null);
        yield return Row("_collarCleared", new Vault { Progress = new ProgressSection { CollarCleared = new ClearedCollarRecord("selene-gate", 4, [3, 5], "DeclaredOverload") } }, v => v.Progress!.CollarCleared is not null);
        yield return Row("_paperTrail", new Vault { PapersShown = new PapersShownSection { Shown = [papers] } }, v => v.PapersShown is not null);
        yield return Row("_walkInSetupsRevealed", new Vault { WalkIn = new WalkInSection { SetupsRevealed = ["job-1"] } }, v => v.WalkIn is not null);
        yield return Row("_weatherHeard", new Vault { InsuranceWeather = new InsuranceWeatherSection { Heard = ["line-a|2"] } }, v => v.InsuranceWeather is not null);
    }

    /// <summary>
    /// <b>WHAT THE OLD LIFE CARRIED IS GONE AFTER A NEW VOYAGE, AND THE NEW THREAD'S FIRST VAULT CARRIES NONE OF IT.</b>
    /// </summary>
    [Theory]
    [MemberData(nameof(Riders))]
    public void ANewVoyageCarriesNoOldRiderIntoItsFirstVault(string field, Vault lived, Func<Vault, bool> carries)
    {
        Pages.Map map = Boot("rider-" + field.TrimStart('_'));
        Invoke(map, "ApplyVault", lived);
        Assert.True(carries((Vault)Invoke(map, "BuildVault", "", "")!), $"control: the old life's vault carries {field}");

        Invoke(map, "ResetLiveStateForNewGame");

        Assert.False(carries((Vault)Invoke(map, "BuildVault", "", "")!), $"{field} rode out of the old life into the new thread's first save");
    }

    /// <summary>The finder's case has no plain section to apply (it is a typed pair), so it is planted on the fields.</summary>
    [Fact]
    public void ANewVoyageHasNoFinderCaseInItsFirstVault()
    {
        Pages.Map map = Boot("rider-finder");
        Set(map, "_finderCase", default(FinderCase.Case));
        Set(map, "_finderProgress", new FinderCase.Progress(true, false, false, false, false, false, default, false));
        Assert.NotNull(((Vault)Invoke(map, "BuildVault", "", "")!).Finder);

        Invoke(map, "ResetLiveStateForNewGame");

        Assert.Null(((Vault)Invoke(map, "BuildVault", "", "")!).Finder);
    }

    /// <summary>
    /// The Core registers the Install* writers feed are STATIC: clearing the page's lists and forgetting to re-install
    /// would leave the old life's care order fencing the new voyage's ground.
    /// </summary>
    [Fact]
    public void ANewVoyageHandsCoreEmptyRegisters()
    {
        Pages.Map map = Boot("rider-core-registers");
        Invoke(map, "ApplyVault", new Vault
        {
            Progress = new ProgressSection { HallsStopped = ["luna"], HallsPreserved = ["luna"] },
        });
        Assert.True(PreservationZone.On("luna"));

        Invoke(map, "ResetLiveStateForNewGame");

        Assert.False(PreservationZone.On("luna"));
    }
}
