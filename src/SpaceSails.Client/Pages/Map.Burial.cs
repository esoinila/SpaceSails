using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// #1063 · THE BURIAL — the client half. Core carries the whole argument (see Burial.cs); this file owns
// three things and nothing else: the register that rides the vault, the ONE moment the event is evaluated,
// and the one cheerful line the rag prints afterwards.
public partial class Map
{
    // ── THE REGISTER ─────────────────────────────────────────────────────────────────────────────────────
    //
    // The grounds the neighbours have filled in. Persisted per-universe in the vault's ProgressSection, the
    // same idiom as _hallsOpened beside it and for the same reason one rung harder: a burial that forgot
    // across a reload would put a set of galleries BACK under a site the captain's own field book says were
    // filled in, and the book being the only witness is the whole feature. A world that could un-bury a
    // ground by reloading is a world where the book is wrong.
    private IReadOnlyList<string> _hallsBuried = [];

    /// <summary>#1063 · Hand Core the world's burial state — the ONE writer, called at the one moment the
    /// world is rebuilt around the captain. Everything downstream (the shaft that now ends at the listed
    /// bottom, the specimen, the ledger, the notice, the mason) reads this and nothing else.</summary>
    private void InstallBurialRegister() => Burial.Install(_hallsBuried, _hallsOpened);

    /// <summary>
    /// #1063 · <b>THE EVENT.</b> Between two visits, the grounds this captain opened get filled in.
    ///
    /// <para><b>The threshold, and its reason, per <see cref="DisclosureClock"/>'s own contract</b> (<i>"every
    /// beat that reads it chooses its own threshold and writes that threshold's reason down beside its own
    /// words"</i>): <b>one whole world window</b> since the opening — because filling, flooring and
    /// resurfacing a set of galleries is a SHIFT of work, and a shift is the shortest thing the world's own
    /// clock measures — <b>and the captain not on that body</b>, because the neighbours do not fill a hall
    /// while the captain is standing in it. Both live in <see cref="Burial.Fill"/>; this is the moment they
    /// are asked.</para>
    ///
    /// <para><b>Called from the descent, after the crossing's clock has been spent and before one wall of the
    /// arriving ground has been laid.</b> That is the only moment in the game when the world is about to be
    /// rebuilt and no excursion is standing on it, which makes "not while he is there" true by construction
    /// rather than by a check somebody has to remember: <c>_surface</c> is null here, so a ground can only
    /// ever be filled while the captain is in flight. It is also why the burial LANDS on a return — he comes
    /// back down to the site and the shaft ends at the listed bottom, and nothing at any point said so.</para>
    ///
    /// <para>Nothing is announced. No card, no pulse, no beat, no nerve shock, no marker on the map — the
    /// only things that ever say a word about it are three pieces of ordinary paperwork and a mason.</para>
    /// </summary>
    /// <summary>#1063 · <c>/map?buried=1</c> — see the class remarks on the query parser. It seeds the
    /// disclosure clock's register with an opening a whole window old and then gets out of the way.</summary>
    private bool _buriedCheat;

    private void BuryWhatWasOpened()
    {
        // The cheat, and it does exactly one thing: it makes the clock say this ground was opened a shift
        // ago. Everything after this line is the ordinary game.
        //
        // #1074 · …and WHICH shift, because the shift decides which of the two outcomes this ground gets.
        // The split is seeded on the window the ground was opened in (StopOrder.TheOfficeGetsThisOne), so a
        // cheat that parked any old window would hand a tester a stop order half the time they asked for a
        // burial. It parks the ROCK and the WINDOW, which is the same move ?found=1 makes with a body id —
        // and it does it by choosing a real window rather than by overriding the outcome, so what a tester
        // walks is still exactly the code path a captain walks.
        //
        // #1074 beat 2 · …and HOW FAR back, because the office's paperwork hardens in stages: one whole
        // window closes the working, two fence it. A cheat that always parked the burial's own threshold
        // could never reach the second stage, so ?preserved=1 asks for a window deep enough that the order
        // and the fence both land on this descent — which is exactly what a captain who stayed away two
        // shifts comes home to.
        if ((_buriedCheat || _stoppedCheat || _preservedCheat)
            && DisclosureClock.OpeningOf(_hallsOpened, UndergroundComplex.FoundBandCheatSiteId) is null)
        {
            _hallsOpened = DisclosureClock.Note(_hallsOpened, new DisclosureClock.Opening(
                UndergroundComplex.FoundBandCheatSiteId,
                CheatOpeningWindow(
                    UndergroundComplex.FoundBandCheatSiteId,
                    wantTheOffice: _stoppedCheat || _preservedCheat,
                    windowsBack: _preservedCheat
                        ? PreservationZone.WindowsBeforePreserving
                        : Burial.WindowsBeforeFilling)));
        }

        IReadOnlyList<string> next = Burial.Fill(
            _hallsOpened, _hallsBuried, _surface?.Stop.Body.Id, SimTime);

        if (!ReferenceEquals(next, _hallsBuried))
        {
            // The rag, once per ground, at the moment the job finishes — which is the moment the register
            // grows. Pushed for each newly filled ground and never re-pushed, because the wire is a record of
            // things that happened and a burial happens once.
            foreach (string filled in next)
            {
                if (!_hallsBuried.Contains(filled))
                {
                    TheRagIsCheerfulAboutIt(filled);
                }
            }
            _hallsBuried = next;
            RequestVaultSave();
        }

        // Installed on every descent and not only on a change: a fresh voyage, a loaded save and a captain
        // who has buried nothing all share one static, and a world that inherited the last one's register
        // would be the worst bug this feature could have.
        InstallBurialRegister();

        // #1068 · …AND IN THE SAME BREATH, THE OTHER TWO CHANNELS. Not a second hook in the descent: it is
        // ONE moment, and both events want exactly the same three things out of it — the crossing's time
        // already spent, no excursion standing on the ground, and not one wall of the arriving floor laid
        // yet. #1068's threshold is #1063's threshold on purpose (the watchers act on the schedule the
        // neighbours do), so a second call site a few lines apart would be two places that have to be kept
        // agreeing about when a shift is. See Map.Decline.cs; it does nothing at all on almost every voyage.
        TheWorldDeclines();

        // #1068 · …and the THIRD channel, on the same breath and the same threshold, for the same reason
        // again. The people who do not know why are a harbour clerk and a pump attendant, and neither of
        // them is standing on this ground either. See Map.QuietHands.cs; like the other two, it does nothing
        // at all on almost every voyage.
        TheQuietHandsMove();
        // #1074 · …and the third outcome of the same moment: the office closes the working. It is the
        // BURIAL'S OWN trigger read the other way — one opened ground, one whole window, the captain off the
        // body — and the two are mutually exclusive by construction, so this may run beside Burial.Fill
        // rather than in place of it and neither has to know what the other decided. See Map.Stop.cs.
        TheOfficeClosesTheWorking();

        // #1074 beat 2 · …and the SECOND STAGE of that same outcome, on the same breath and one shift
        // further along: a working the office closed passes into official care — fenced, signed, and under
        // a study nobody has scheduled. It runs after the closure and never before it, because a zone stands
        // on a CLOSED working and reads the register the line above may have just written; a captain who
        // stayed away two shifts should come home to both at once rather than to a fence with no order
        // behind it. See Map.Preserve.cs; like the others, it does nothing at all on almost every voyage.
        TheSitePassesIntoCare();
    }

    /// <summary>#1074 · Which window a dev cheat should say this ground was opened in, so that the outcome
    /// the tester asked for is the outcome the ordinary game produces.
    ///
    /// <para>Walks back a shift at a time from the current window until the split
    /// (<see cref="StopOrder.TheOfficeGetsThisOne"/>) lands on the wanted side. It is a fair coin, so the
    /// first or second candidate almost always answers; the sweep is long enough that failing it is not a
    /// thing that happens, and the last window is returned rather than throwing because a cheat that crashed
    /// a boot would be worse than a cheat that showed the other beat.</para>
    ///
    /// <para>Every candidate is at least <paramref name="windowsBack"/> whole windows old — the walk starts
    /// one PAST the caller's threshold rather than at it, exactly as the pre-#1074 cheat's own arithmetic
    /// did. #1074 beat 2 · the threshold is a PARAMETER because the office's paperwork hardens in stages and
    /// the deeper stage needs a deeper window; the alternative was a second copy of this walk that differed
    /// by one number, which is the mirrored constant this ground keeps paying for.</para></summary>
    private long CheatOpeningWindow(string bodyId, bool wantTheOffice, long windowsBack)
    {
        long now = DisclosureClock.WindowAt(SimTime);
        long window = now - windowsBack - 1;
        for (int back = 0; back < 64; back++)
        {
            long candidate = window - back;
            if (StopOrder.TheOfficeGetsThisOne(new DisclosureClock.Opening(bodyId, candidate))
                == wantTheOffice)
            {
                return candidate;
            }
        }
        return window;
    }

    /// <summary>#1063 · <b>AFTER</b> — the rag, cheerful, once. The owner's own cherry on the cake: a ton of
    /// work to raise a street for no apparent purpose, reported as good news about drainage.
    ///
    /// <para>It goes on the wire as an arc break (#411/#663's kind, the one where <b>the subject IS the
    /// headline</b>) rather than as an ambient template, because an ambient line is a fact about a seeded day
    /// and this is a fact about something that happened. Filed under the site's own operator, so a captain
    /// who presses ✂ CLIP (#1052) files it beside every other paper that came out of that company — and the
    /// field book, which is the only witness, ends up holding the world's own cheerful account of the thing
    /// it disagrees with.</para></summary>
    private void TheRagIsCheerfulAboutIt(string bodyId) =>
        PushNewsEvent(NewsWire.NewsEventKind.ArcBeatBreaks, Burial.RagLine, Burial.RagOffice(bodyId));
}
