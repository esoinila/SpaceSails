using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Vault.NewVoyage — the second half of ResetLiveStateForNewGame (#1374). Split out because Map.Vault.cs sits
// near the size gate's line, and because this half is one idea: everything BuildVault writes that the first half
// never named. Each line here is the blank-vault answer ApplyVault already knows how to give for the same field.
public partial class Map
{
    /// <summary>
    /// #1374 · …and the rest of the life that rides the vault. #1372 found the tag register missing from a reset whose
    /// contract is "the exact inverse of BuildVault"; the audit that followed read every section BuildVault writes
    /// against the reset and found these unreset: a New voyage in the same session left each of them standing, and the
    /// fresh thread's first autosave wrote the old life's field book, satchel, ground marks, burials and clocks into
    /// itself — permanently, because nothing ever clears a thread's vault but a new thread.
    ///
    /// <para>Where a section has a <c>Restore*(null)</c> (a file that lacks the section), that is the blank answer and
    /// is used, so the reset and a load of an empty vault cannot drift. The Core registers the Install* writers feed are
    /// STATIC, so each is handed its empty list again.</para>
    /// </summary>
    private void ForgetTheLifeThatRidesTheVault()
    {
        _fieldNotes = [];     // #587 · the one book (the pendant's first-opening latch moved off it in #1356)
        _caseThreads = [];    // #741 · the red lines across that book
        _satchel = [];        // #603 · what was carried on foot
        _workedUp.Clear();    // #1016 · which sheets are already in the book
        _groundMemory = GroundMemory.Restore(null);   // #563 · the marks left on every moon
        ForgetThePaperTrail();                        // #836 · which identity was handed to which man

        _oddBooksRead = [];
        _secretLabsFound.Clear();
        _workingStopsSinceShoreLeave = 0;
        _emptySealSpentOn = null;
        _observationWalkSpentOn = null;
        _observationWalkSightingAt = null;

        // #677/#1063/#1068/#1074 · the disclosure clock and everything keyed on it, and Core's static copies.
        _hallsOpened = [];
        _hallsBuried = [];
        _hallsDeclined = [];
        _hallsHandled = [];
        _hallsStopped = [];
        _hallsPreserved = [];
        InstallBurialRegister();
        InstallDeclineRegister();
        InstallQuietHandsRegister();
        InstallStopRegister();
        InstallPreserveRegister();

        RestoreShuttle(null);          // #1074 beat 5 · the boat, the seen grounds and the on-the-board latch
        RestoreClearedCollar(null);    // #525
        RestoreWalkInSection(null);    // #973 L5b
        RestoreFinderSection(null);    // #417
        RestoreTheWeatherSection(null);

        // #638 · the void's countdown, and the sweep that feeds it, back to "no clock running".
        _voidDeclaredDay = VoidRule.ClockNotRunning;
        _voidLastToldDay = VoidRule.NothingToldYet;
        _voidSweptDay = long.MinValue;
        _voidHavenInReach = true;
    }

}
