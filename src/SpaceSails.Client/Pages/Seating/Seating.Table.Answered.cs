using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #746/#680 · <b>THE ONE PLACE AN OUTCOME BECOMES WORDS</b> — part of the table scene
/// (<see cref="Seating"/>); the file's own class summary lives on <c>Seating.Table.cs</c>.
///
/// <para>It has a file to itself because it is a law rather than a section: every branch of every move,
/// every wait, every showing, every stand-up ends HERE, so there is exactly one method that can put #680's
/// rule back in the pulse, and one method a guard has to force a band through. #1016 kept it the one place
/// when there is no building at all — the excursion is nullable and each write into the ROOM's ledgers is
/// skipped, rather than a room-less copy of this becoming a second ending.</para>
/// </summary>
public partial class Map
{
    private sealed partial class Seating
    {
        /// <summary>
        /// #746/#680 · THE ONE PLACE AN OUTCOME BECOMES WORDS — and it puts them INSIDE the panel.
        ///
        /// <para>Applies everything the answer carries: the chit into the wallet, the name in somebody's book,
        /// the ask shutting, the table hardening, the fitter opening, the temp overhearing, the pips. Nothing
        /// about WHAT a band grants is decided here; a guard forces a band and pins the state that appears.</para>
        ///
        /// <para><b>#1016 · AND IT IS STILL THE ONE PLACE WHEN THERE IS NO BUILDING.</b> The seats that have
        /// no <c>SurfaceExcursion</c> behind them — a top in a docked station's bar, and the ship's own two —
        /// reach exactly one answer, the wait beat's silence, and it carries nothing but a line. Everything
        /// below that is written into the ROOM's ledgers (a move made this watch, an ask shut, a table
        /// hardened) and a room is what those seats do not have. So the excursion is nullable and each write
        /// is skipped rather than the caller being given a second ending: #680's law is that an outcome
        /// becomes words in ONE place, and a room-less copy of this method would be a second one.</para>
        /// </summary>
        private void TableAnswered(
            SurfaceExcursion? ex, TableTalk t, string moveId, CanteenTable.Answer said)
        {
            ex?.TableMoves.Add(MoveKey(t, moveId));
            // #749 · …and the conversation's own memory, which is what an ANSWER is allowed to answer. The room
            // keeps the first for the watch; this one stands up when you do.
            t.Said.Add(moveId);

            if (said.GrantsChit)
            {
                _host.Satchel = [.. Core.Satchel.Add(_host.Satchel, CanteenTable.Chit(said.UnderAnotherName))];
            }
            if (said.ClosesTheAsk)
            {
                ex?.TableAskShut.Add(t.Key);
            }
            if (said.HardensTable)
            {
                ex?.TableHardened.Add(t.Key);
            }
            if (said.OpensFitter && ex is not null)
            {
                ex.TableFitterOpen = true;
            }
            if (said.ArmsTheTemp && ex is not null)
            {
                ex.TableTempOverheard = true;
            }
            if (said.TeachesTheHouse && ex is not null)
            {
                ex.TableHouseWays = true;
            }
            if (said.NervePips > 0)
            {
                // Through the ordinary nerve system and nothing else — the same gauge the ground bleeds, which
                // is the sanity system's own lineage saying a scene going wrong down here is frightening.
                _host.ApplyNerveShock(said.NervePips * NervePips.PipUnit, "a table you cannot read");
            }
            if (said.Note is { Length: > 0 } note)
            {
                // FileNote and NOT ShowAndFile: the saying happens in the panel, and a pulse under an open
                // modal's blur is the exact bug #680 was filed on. The book still remembers (#686).
                _host.FileNote(note, CanteenRegulars.Glyph);
            }

            // #680 · Said HERE, in the one layer the backdrop cannot blur.
            t.Outcome = said.Line;
            _host.RequestVaultSave();
            _host.StateHasChanged();

            // #731 v2 · …AND THIS IS THE MOMENT SHE MIGHT STAND UP. Last, deliberately: the line she has just
            // said stays on the panel while she crosses the hall, which is exactly what somebody leaving in
            // the middle of a conversation leaves behind them.
            //
            // Nobody has stood up at a seat with no building round it, because nobody sat down opposite: the
            // one answer those seats reach is the silence of a wait nobody came to.
            if (ex is not null)
            {
                SheMightLeadYouIn(ex, t);
            }
        }
    }
}
