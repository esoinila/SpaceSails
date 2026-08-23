using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: the ship's black box, page side — reading what the LAST voyage left in localStorage, giving
// this voyage's notes somewhere durable to go, and handing the Captain's desk the sentence to show.
//
// Owner's playtest, 2026-08-22: two screenshots end at Blazor's "An unhandled error has occurred. Reload."
// and nothing else. The console had the text; the console died with the tab. CrashLog (Rendering) catches
// the exception wherever it escapes; this is the half that makes it OUTLIVE the reload and puts it where
// a captain will actually find it.
public partial class Map
{
    /// <summary>What the PREVIOUS page life left behind in localStorage, read once at boot. Null on a clean
    /// voyage — and null forever after the captain clears it.</summary>
    private CrashNote? _lastVoyageCrash;

    /// <summary>Set once the [copy] has been pressed, so the desk can say it took.</summary>
    private bool _crashNoteCopied;

    /// <summary>Boot wiring, run as soon as the JS module (and therefore localStorage) is reachable: pick up
    /// the note the last voyage left, then give this voyage's notes a durable place to land.</summary>
    private void WireTheBlackBox()
    {
        try
        {
            if (CrashNote.TryParse(RendererInterop.VaultRead(CrashLog.StorageKey), out CrashNote stored))
            {
                _lastVoyageCrash = stored;
            }
        }
        catch
        {
            // A storage that will not answer simply means no note from last time. Never a second crash.
        }

        // Static lambda on purpose: this hook outlives the page (the whole point is that a note survives
        // the crash that took the page with it), so it must not capture the component.
        CrashLog.Persist = static note =>
        {
            try
            {
                RendererInterop.VaultWrite(CrashLog.StorageKey, note.ToStorage());
            }
            catch
            {
                // quota / private mode — the note still lives in memory for this session's desk.
            }
        };
    }

    /// <summary>The note the Captain's desk shows: whatever went wrong THIS session outranks the one the
    /// last voyage left, because it is the fresher truth about the same ship.</summary>
    private CrashNote? CaptainCrashNote() => CrashLog.Latest ?? _lastVoyageCrash;

    /// <summary>[copy] — the whole note, frames and all, onto the clipboard so it can be pasted into an
    /// issue. That is the entire reason this feature exists.</summary>
    private void CopyCrashNote()
    {
        if (CaptainCrashNote() is not { } note)
        {
            return;
        }

        try
        {
            _crashNoteCopied = RendererInterop.CopyText(note.DescribeFully());
        }
        catch
        {
            _crashNoteCopied = false;
        }
    }

    /// <summary>Filed and dealt with — forget it, in memory and in storage, so the desk is quiet again.</summary>
    private void DismissCrashNote()
    {
        _lastVoyageCrash = null;
        _crashNoteCopied = false;
        CrashLog.Forget();

        try
        {
            RendererInterop.VaultClear(CrashLog.StorageKey);
        }
        catch
        {
            // Nothing to do — a storage that cannot clear also cannot have saved.
        }
    }
}
