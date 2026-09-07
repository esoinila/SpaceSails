using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Components;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #992 · <b>GUARD 1 — THE COMPLETENESS GUARDS, READ OFF THE MARKUP AS TYPED</b>, and the two bookkeeping
/// laws that keep the register honest about itself.
///
/// <para>What this part owns is everything that can be asked of <see cref="EveryPopUpCanBeDismissedTests"/>'s
/// register without booting anything: that no surface in the source escapes it, that no surface built on the
/// OverlayShell sits outside the recogniser's sight, that every row is honest about whether it has a driver,
/// and that the undriven list only ever gets shorter.</para>
/// </summary>
public sealed partial class EveryPopUpCanBeDismissedTests
{
    // ── Guard 1 · the completeness guard, read off the markup as typed ────────────────────────────────

    /// <summary>
    /// EVERY OVERLAY ROOT IN THE SOURCE IS IN THE REGISTER.
    ///
    /// <para>This is the guard that makes the law survive the next feature. A pop-up joins the client by
    /// somebody typing a <c>class="…-backdrop"</c> into a razor file; from that moment this fails, naming the
    /// class and the file, until the surface is entered in <see cref="TheRegister"/> — at which point guards 2
    /// and 3 start asking it the real question.</para>
    /// </summary>
    [Fact]
    public void NoSurfaceInTheSourceEscapesTheRegister()
    {
        var registered = TheRegister.Select(p => p.RootClass).ToHashSet(StringComparer.Ordinal);
        var strangers = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (string file in RazorFiles())
        {
            foreach (string[] classes in ClassListsIn(File.ReadAllText(file)))
            {
                if (IsAnUnregisteredSurface(classes, registered))
                {
                    strangers.TryAdd(string.Join(' ', classes.Where(IsAPopUpRoot)), Path.GetFileName(file));
                }
            }
        }

        Assert.True(strangers.Count == 0,
            $"{strangers.Count} surface(s) in the markup look like a pop-up and are not in the register, so "
            + "nothing has ever asked them the owner's question (2026-08-24: \"there should not be a pop-up "
            + "that cannot be closed or minimized\"). Add a row to TheRegister — or, if it is not a pop-up at "
            + "all, a sentence to NotPopUpsAndWhy saying why:\n  - "
            + string.Join("\n  - ", strangers.Select(s => $"{s.Key}  ({s.Value})")));
    }

    /// <summary>
    /// EVERY SURFACE BUILT ON THE SHELL SITS UNDER A ROOT THIS LAW CAN SEE — the recogniser's own blind spot,
    /// closed from the tree rather than from a list.
    ///
    /// <para>Guard 1 finds a pop-up by the class its root wears: <c>*-backdrop</c>, <c>*-overlay</c>,
    /// <c>*-modal</c>, or a name in <see cref="TheFamiliesThatPredateTheNaming"/>. That is a good recogniser
    /// and it has one hole, which is the shape of every recogniser's hole: a surface that follows NEITHER the
    /// house naming NOR a name somebody remembered to write down is not reported as a stranger — it is not
    /// seen at all, and the law passes without having asked it anything.</para>
    ///
    /// <para>This guard closes that hole with the one fact about a surface that cannot be a matter of naming:
    /// <b>a file that renders an <see cref="OverlayShell"/> is a pop-up, because the shell exists for nothing
    /// else.</b> So every such file must also contain a class the recogniser SEES and the law KNOWS — its own
    /// root, or the backdrop it is drawn in, which is where the <c>.view-object</c> family and the convergence
    /// band carry theirs. Sixty-eight files render a shell today and every one of them is anchored; the day a
    /// crew extracts a sixty-ninth wearing a wholly new name, this fails naming the file, and the fix is a row
    /// in <see cref="TheRegister"/> or a name in the families list — not an edit here.</para>
    ///
    /// <para>It is derived from the tree on every run, so it counts surfaces that did not exist when it was
    /// written. That is deliberate: #992's audit was a count on a page, it went stale inside a fortnight
    /// (#1169 found the numbers it left in <c>OverlayDismiss</c>'s docblock wrong and could not correct them),
    /// and a number nobody re-derives is a number that quietly stops being true. This asks the tree instead.
    /// </para>
    /// </summary>
    [Fact]
    public void NoSurfaceOnTheShellSitsOutsideTheRecognisersSight()
    {
        var known = TheRegister.Select(p => p.RootClass)
            .Concat(NotPopUpsAndWhy.Keys)
            .ToHashSet(StringComparer.Ordinal);

        var unanchored = new SortedSet<string>(StringComparer.Ordinal);
        int anchored = 0;

        foreach (string file in RazorFiles())
        {
            string markup = File.ReadAllText(file);
            if (!markup.Contains("<OverlayShell", StringComparison.Ordinal)
                || Path.GetFileName(file) == "OverlayShell.razor")
            {
                continue;
            }

            if (ClassListsIn(markup).SelectMany(c => c).Any(c => IsAPopUpRoot(c) && known.Contains(c)))
            {
                anchored++;
            }
            else
            {
                unanchored.Add(Path.GetFileName(file));
            }
        }

        Assert.True(anchored > 0,
            "not one file in the client renders an OverlayShell, which cannot be true while the shell is the "
            + "one mechanism #997 made it. This guard has been handed the wrong tree and would pass on "
            + "anything — see ClientSource().");

        Assert.True(unanchored.Count == 0,
            $"{unanchored.Count} of {anchored + unanchored.Count} file(s) draw a pop-up on the OverlayShell "
            + "and wear no class this law's recogniser can see, so the completeness guards have never asked "
            + "them the owner's question (2026-08-24: \"there should not be a pop-up that cannot be closed or "
            + "minimized\"). Give the surface a `-backdrop`/`-overlay`/`-modal` root, or name its family in "
            + "TheFamiliesThatPredateTheNaming and enter it in TheRegister:\n  - "
            + string.Join("\n  - ", unanchored));
    }

    /// <summary>The register may not carry a row nobody can act on: a row with no driver must say why, and a
    /// row with a driver must not pretend it has a reason not to.</summary>
    [Fact]
    public void EveryRowInTheRegisterIsHonestAboutItself()
    {
        var wrong = TheRegister
            .Where(p => (p.Raise is null) == (p.WhyNotDriven.Length == 0))
            .Select(p => p.Raise is null
                ? $"{p.Name}: no driver and no reason given"
                : $"{p.Name}: has a driver AND a reason not to be driven")
            .ToList();

        Assert.True(wrong.Count == 0, string.Join("\n  - ", wrong));
    }

    /// <summary>
    /// THE UNDRIVEN LIST ONLY EVER GETS SHORTER.
    ///
    /// <para>A register row with no driver is covered by the two completeness guards and by nothing else — it
    /// is named, but the ruling has not been PROVED of it. That is an honest state to be in and a dishonest
    /// one to drift in, so the count is written down. Anybody may make it smaller; making it bigger costs an
    /// edit to this number and a line in a diff.</para>
    /// </summary>
    [Fact]
    public void TheUndrivenListOnlyEverGetsShorter()
    {
        // #997 wave 7 · FIFTEEN BECAME FOURTEEN, and by a row LEAVING rather than by one being driven.
        // The front door was never a pop-up; it is the threshold, and it now says so in NotPopUpsAndWhy in
        // Fable's own words (Fable's ruling, wave 7 — see the entry there for the reason and for why the
        // class it is keyed on is still registered by the three real sheets that share it). Lowering a
        // ceiling is always allowed; this one is lowered because the row it counted is gone.
        //
        // #997 wave 10 · FOURTEEN BECAME TWELVE, and only ONE of those two steps is an achievement.
        //
        // The target dossier is driven now, and it is the first reason on this list that turned out to be a
        // missing DEV DOOR rather than a world the bench cannot build. Its row said "needs a tactical
        // target"; that was true, and it is answerable from a URL now (?target=, Map.Npc.SeedTargetCheat),
        // so the ruling is proved of that card by pressing rather than named and left.
        //
        // The other step is bookkeeping, and it is said out loud rather than pocketed: the ceiling has been
        // 14 with only 13 rows under it since wave 7, so it carried a spare notch nobody had used. A
        // ceiling with slack in it cannot catch the next row that creeps under it — which is this number's
        // whole job — so it is pulled down onto the count. It is TIGHT now: undrive any single row and this
        // goes red, which is the red proof wave 10's PR quotes.
        //
        // #997 wave 11 · TWELVE BECAME ELEVEN, and it is the same kind of step as the dossier's rather than
        // bookkeeping. The dice tray's reason named the right gate and the wrong obstacle: a DiceEvent is
        // pure Core data and #305 shipped one seam for handing one to the tray, so the road was there all
        // along and nobody had walked it. Still TIGHT — undrive any single row and this goes red.
        //
        // #997 wave 12 · ELEVEN BECAME ZERO, in one wave, and that is not eleven achievements. It is one
        // finding repeated eleven times: the dice tray's diagnosis was not special to the dice tray. Ten of
        // the eleven remaining reasons named the right gate and the wrong obstacle — Adrift is two fields,
        // LoudPlanAlarm is a string, the shuttle board draws on an EMPTY list, the keypad's hatch is a `??`
        // fallback in its own header — and the eleventh (the operating log) was simply true and still left
        // the row sitting, because a reason nobody re-reads is a reason nobody acts on. Every one of them
        // was found by opening the file the reason was about.
        //
        // ZERO IS THE STRONGEST THIS NUMBER HAS EVER BEEN AND THE EASIEST TO BREAK, which is the point.
        // There is no slack left at all: a single row added with a `null` Raise fails this on the day it is
        // typed, and the fix is either a driver or a deliberate edit to this number with the reason in the
        // commit. That is the trade wave 10 argued for when it pulled the ceiling down onto its count.
        int undriven = TheRegister.Count(p => p.Raise is null);
        Assert.True(undriven <= 0,
            $"{undriven} register row(s) have no driver, and the written-down ceiling is 0 — since #997 wave "
            + "12 every surface in this register is raised and pressed. If a row genuinely cannot be raised "
            + "off-browser, lowering the ceiling is wrong: raise it deliberately, say so in the commit, and "
            + "make the row's reason a sentence about the CODE somebody has just read — ten of the eleven "
            + "reasons this ceiling used to count were describing gates that had moved.");
    }

    /// <summary>
    /// EVERY WORLD IN THE REGISTER IS ONE SOMEBODY HAS SAID WHERE IT CAME FROM.
    ///
    /// <para><b>The drift this was written for.</b> The <c>World</c> docstring said "a URL from
    /// <see cref="EveryDeskBootsTests"/>'s matrix", and three rows had quietly stopped obeying it —
    /// <c>?oracle=1&amp;ashore=1</c>, <c>?barcase=1</c> and <c>?start=wreck&amp;target=collector</c> are in no
    /// matrix. Nothing was WRONG: all three boot, and guard 3 renders each of them. What was wrong is that
    /// the register's worlds had no owner, and a register's worlds are precisely where this repo's fifth
    /// named bug class lives — a guard handed a world it never reached runs every check it has and passes
    /// on nothing.</para>
    ///
    /// <para><b>So the law is that sentence, made checkable.</b> A row's world comes from the matrix, or it
    /// is one of the three in <see cref="TheWorldsBeyondTheMatrix"/> with a reason beside it; and that table
    /// may hold nothing mute, nothing the matrix already carries, nothing stale, and nothing no row DRIVES —
    /// because an off-matrix world reaches a renderer only by a driven row, and one sitting on an undriven
    /// row would be a world this suite names and never visits.</para>
    ///
    /// <para><b>Anti-vacuity.</b> The two tables are asked to actually meet: the matrix must hand over
    /// worlds, and <see cref="Docked"/> — a URL that is a matrix row character for character — must be found
    /// in it. A comparison against an empty or mis-keyed set would otherwise excuse everything.</para>
    /// </summary>
    [Fact]
    public void EveryWorldInTheRegisterComesFromTheMatrixOrSaysWhyNot()
    {
        var matrix = EveryDeskBootsTests.EveryWorldUrl().ToHashSet(StringComparer.Ordinal);

        Assert.True(matrix.Count > 0,
            "the desk matrix handed this guard no worlds at all, so every comparison below would pass on "
            + "nothing. EveryDeskBootsTests.EveryWorldUrl() has moved or emptied.");
        Assert.Contains(Docked, matrix);

        var beyond = TheWorldsBeyondTheMatrix.ToDictionary(w => w.Url, w => w.Why, StringComparer.Ordinal);

        var adrift = TheRegister.Select(p => p.World)
            .Distinct(StringComparer.Ordinal)
            .Where(w => !matrix.Contains(w) && !beyond.ContainsKey(w))
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.True(adrift.Count == 0,
            $"{adrift.Count} world(s) in the register are neither in EveryDeskBootsTests' matrix nor in "
            + "TheWorldsBeyondTheMatrix, so nothing says where they came from. Either use a matrix world, or "
            + "add a row to that table with the reason a desk sweep would learn nothing from it:\n  - "
            + string.Join("\n  - ", adrift));

        var mute = TheWorldsBeyondTheMatrix.Where(w => w.Why.Length == 0).Select(w => w.Url).ToList();
        Assert.True(mute.Count == 0,
            "an exception with no reason on it is not an exception, it is a hole:\n  - "
            + string.Join("\n  - ", mute));

        var alreadyThere = TheWorldsBeyondTheMatrix.Where(w => matrix.Contains(w.Url))
            .Select(w => w.Url).ToList();
        Assert.True(alreadyThere.Count == 0,
            $"{alreadyThere.Count} world(s) are listed as beyond the matrix and are IN it — the matrix has "
            + "grown a row and this table was not read again:\n  - " + string.Join("\n  - ", alreadyThere));

        var used = TheRegister.Select(p => p.World).ToHashSet(StringComparer.Ordinal);
        var stale = TheWorldsBeyondTheMatrix.Where(w => !used.Contains(w.Url)).Select(w => w.Url).ToList();
        Assert.True(stale.Count == 0,
            $"{stale.Count} world(s) are excused here and used by no row — an excuse nobody spends is an "
            + "excuse nobody re-reads:\n  - " + string.Join("\n  - ", stale));

        var neverBooted = TheWorldsBeyondTheMatrix
            .Where(w => !TheRegister.Any(p => string.Equals(p.World, w.Url, StringComparison.Ordinal)
                                              && p.Raise is not null))
            .Select(w => w.Url)
            .ToList();
        Assert.True(neverBooted.Count == 0,
            $"{neverBooted.Count} off-matrix world(s) sit only on rows with no driver, so no law in this "
            + "repository ever boots them: the matrix does not sweep them and guard 3 never raises them. "
            + "Drive the row, or put the world in the matrix:\n  - " + string.Join("\n  - ", neverBooted));
    }
}
