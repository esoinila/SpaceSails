using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #870 lane 6c · THE COUNTER STOOL, as the seat's own verbs — moved off <c>Map.Stool.cs</c>, byte for byte
/// except for the receiver.
///
/// <para>#756's grammar — one [E] per fixture, so a stool is not a second console but a POSTURE of the one
/// that is already there, and the keep goes on serving you seated because the menu never went anywhere — is
/// documented where it has always been documented: <c>Map.Stool.cs</c>'s own class summary, which stayed with
/// the <c>StoolSeat</c> record and the two dev cheats. What is here is taking one, getting off one, the moves
/// on the card and the beat you spend waiting to be spoken to.</para>
///
/// <para>The card underneath is the page's (<see cref="ISeatHost.BarMenu"/>) and so is the slot the keep
/// speaks into (<see cref="ISeatHost.BarNotice"/>): #736 was fought over one card owning one notice, and a
/// stool that grew a second one would be re-fighting it from a chair.</para>
/// </summary>
public partial class Map
{
    private sealed partial class Seating
    {
        /// <summary>The key this stool's beats and its one approach are filed under. Watch-scoped like every
        /// other fact about this room, so a new shift is a new evening at the bar.</summary>
        private static string StoolKey(SurfaceExcursion ex, int stool) =>
            $"stool:{ex.Floor}:{ex.CanteenWatch}:{stool}";

        // ── TAKING ONE, AND GETTING OFF ───────────────────────────────────────────────────────────────────

        /// <summary>Is there a stool to take right now? The card asks this to decide whether to draw the verb —
        /// and it draws it either way when the counter has stools at all, because #212's law is that an
        /// affordance never hides: a full row REFUSES OUT LOUD (<see cref="TheStools.RowIsFullLine"/>) rather
        /// than vanishing and leaving a captain to wonder whether stools exist.</summary>
        public bool CounterHasStools() =>
            _host.Surface is { Floor: < 0 } ex
            && _host.BarMenu is { } keep
            && CounterService.For(ex.Stop.Body.Id, UndergroundComplex.Comfort.UpperCanteen) is { } serves
            && serves.BodyId == keep.BodyId;

        /// <summary>
        /// #756 · TAKE A STOOL — one press, pick-or-default.
        ///
        /// <para>Which seat is free is Core's answer off the frozen watch (<see cref="TheStools.FirstFreeStool"/>),
        /// so the row a captain lands on is the row the room has. A full row is an answer with words on it and
        /// never a control that did nothing.</para>
        ///
        /// <para>#820 · AND THE CAPTAIN GOES UP ONTO IT. Owner's law, filed off a park bench and swept across
        /// every seat: sitting down puts the body on the seat. WHERE that stool is bolted down is the counter's
        /// own carve (<see cref="UndergroundComplex.FloorPlan.TheStoolRow"/>), read by ordinal — entry
        /// <c>s</c> is stool <c>s</c>, which is the same ordinal <see cref="TheStools.Taken"/> answers about, so
        /// the captain lands on the seat the deck drew free rather than in somebody's lap.</para>
        ///
        /// <para>There is no step-off to carry. A stool stands on the hall side of the desk on floor a captain
        /// could have walked to anyway (the row is laid at <c>HallStoolStandoffDu</c> off the counter's
        /// segments), so getting down leaves you standing exactly where the stool is — which is what getting
        /// down off a bar stool does.</para>
        /// </summary>
        public void TakeAStool()
        {
            if (_host.Surface is not { } ex || ex.Floor >= 0 || Stool is not null)
            {
                return;
            }

            if (TheStools.FirstFreeStool(ex.Stop.Body.Id, ex.Floor, ex.CanteenWatch) is not { } seat)
            {
                _host.BarNotice = TheStools.RowIsFullLine;
                return;
            }

            // The bound is a bound and not a decision: a serving counter publishes exactly TheStools.Count
            // seats (guarded in SittingSnapsYouOntoTheSeatTests), so the only way past this test is a carve
            // that has stopped agreeing with the row the verb is dealt from — in which case the captain keeps
            // their feet rather than being placed by arithmetic on a list that is not the room's.
            IReadOnlyList<(double X, double Y)> row =
                UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).TheStoolRow;
            if (seat < row.Count)
            {
                _host.SitCaptainOn(row[seat].X, row[seat].Y);
            }

            Stool = new StoolSeat
            {
                Index = seat,
                Key = StoolKey(ex, seat),
                Scene = TheStools.TheStool(seat),
            };
            _host.BarNotice = TheStools.TookAStoolLine;
            RendererInterop.PlayCue("reveal");
            _host.StateHasChanged();
        }

        /// <summary>Get down. Free, always — and it leaves you STANDING at the counter rather than shutting the
        /// card, because getting off a stool is not leaving a bar.
        ///
        /// <para>#847 · THE STOOL'S STAND-UP PATH, and there is only this one. A movement key or a clicked route
        /// at the counter routes through here (<c>Map.Seated.StandUpBeforeWalking</c>) rather than carrying a
        /// copy of it, so the day getting off a stool costs a beat, a line or a watch, it costs it to the button
        /// and to WASD in the same breath.</para>
        ///
        /// <para>The step-off square is the stool's own: the row is bolted a standoff off the counter on floor a
        /// captain could have walked to anyway (see <see cref="TakeAStool"/>), and
        /// <c>EverySeatIsSomewhereYouCanSitTests</c> asserts that of every stool on every floor the generator
        /// lays. So getting down leaves you exactly where the stool is, which is what getting down off a bar
        /// stool does — no placement, and nothing for a nudge to rescue.</para></summary>
        private void GetDownFromStool()
        {
            Stool = null;
            _host.BarNotice = TheStools.GotDownLine;
            _host.StateHasChanged();
        }

        /// <summary>#870 lane 6a · …AND THE OTHER WAY THE SEAT ENDS, which is not a getting-down at all: the
        /// counter card itself closing (<c>CloseBarkeep</c> in <c>Map.Quests.Bar.cs</c>, #756). Walking away from
        /// a counter cannot leave the captain sitting at it, so the state goes — but SILENTLY, and that is the
        /// whole reason this is not <see cref="GetDownFromStool"/>: nobody got down off anything, so there is no
        /// <c>GotDownLine</c> to say, and the caller is already clearing the notice slot and re-rendering on its
        /// own way out. It sits here, one line under its sibling, so the difference between the two is a thing a
        /// reader trips over rather than a thing they have to go and find.</summary>
        public void LeaveTheStoolBehind() => Stool = null;

        // ── THE MOVES ─────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Can this move be made right now? Core's requirement check, plus the two facts about a LADDER that
        /// <see cref="Encounter.Available"/> does not decide for a scene whose rungs are all
        /// <c>Requirement.Free</c> replies.
        ///
        /// <para><b>A rung that costs coin needs the coin</b>, and <b>a rung is climbed once.</b> The second one
        /// was found by playing it: every rung here is a fixed outcome with a line on it, so
        /// <see cref="Encounter.Available"/> said yes to all of them for ever — and STAND HER ONE could be
        /// pressed as many times as you liked, debiting 7 cr and re-telling the same sentence each time. A
        /// conversation whose sentences can be re-said is not a conversation, and a paid one is a leak.</para>
        ///
        /// <para><see cref="TheStools.Wait"/> is the exception, and it is the whole verb of sitting there: you
        /// may wait as long as you like, and the room answers differently each beat.</para>
        ///
        /// <para>Both are DRAWN AND REFUSED, never silently missing — #212's affordances-never-hide, and #603's
        /// rule that a control which does nothing is worse than one that says why.</para>
        /// </summary>
        public bool StoolMoveOnOffer(Encounter.Move move) =>
            Stool is { } s
            && Encounter.Available(move, _host.Credits, _host.Satchel, s.Said, s.Said)
            && _host.Credits >= move.Credits
            && (move.Id == TheStools.Wait || !s.Said.Contains(move.Id));

        /// <summary>Why a drawn move is refused, in words — because a refusal a player cannot read is the one
        /// kind of "no" this game does not allow itself (#212/#603).</summary>
        public string StoolMoveRefusal(Encounter.Move move) =>
            Stool is { } s && s.Said.Contains(move.Id)
                ? "You have already said that."
                : $"{move.Credits} cr, and you have {_host.Credits}.";

        /// <summary>A move, pressed with the MOUSE — and the way home when it shuts the panel. The seam
        /// <c>Dismiss</c> documents: a click leaves focus on a button that has just stopped existing, and the
        /// deck goes deaf.</summary>
        public async Task StoolMoveClicked(string moveId)
        {
            StoolMove(moveId);
            if (Stool is null && _host.BarMenu is null)
            {
                await _host.RefocusMap();
            }
        }

        /// <summary>
        /// #756 · One press on the stool's panel.
        ///
        /// <para>WAIT is its own branch, because what it says is decided by the room at the moment it is pressed.
        /// Everything else is a FIXED OUTCOME that speaks because the move CARRIES a line (#749) — never because
        /// somebody wrote its id into a switch down here, which is the bug #750 was filed for.</para>
        /// </summary>
        private void StoolMove(string moveId)
        {
            if (_host.Surface is not { } ex || Stool is not { } s)
            {
                return;
            }

            if (moveId == TheStools.Wait)
            {
                StoolWaited(ex, s);
                return;
            }

            Encounter.Move move = default;
            bool found = false;
            foreach (Encounter.Move m in s.Scene.Moves)
            {
                if (m.Id == moveId)
                {
                    (move, found) = (m, true);
                    break;
                }
            }
            if (!found || !StoolMoveOnOffer(move))
            {
                return;
            }

            if (moveId == TheStools.GetDown)
            {
                GetDownFromStool();
                return;
            }

            // Bought BEFORE the answer, because the answer IS the glass arriving. One number: the move's own,
            // which is CounterService.HouseRate, which is what the card under the glass charges for that pour.
            if (move.Credits > 0)
            {
                _host.Credits -= move.Credits;
                _host.RequestVaultSave();
            }

            s.Said.Add(moveId);
            if (move.Says is { Length: > 0 } said)
            {
                _host.BarNotice = said;
            }

            // …and the ONE rung the field book keeps. The note rides the move, so no author here has to remember
            // which sentence was worth writing down.
            if (move.Note is { Length: > 0 } note)
            {
                _host.FileNote(note, TheStools.Glyph);
                _host.RequestVaultSave();
            }

            // #757's wave-off, at a counter. Letting it lie ENDS THE VISIT rather than the sitting: she turns
            // back to her cup, the stool is yours again, and the panel never blinks — it was one occupation of
            // one seat all along. The outcome line stays exactly as it is, because what she said on the way out
            // is the last thing that happened and must not be wiped by the state change that follows it.
            if (moveId == TheStools.LetItLie)
            {
                BackToYourOwnStool(s);
                return;
            }

            _host.StateHasChanged();
        }

        /// <summary>#756 · She turns back to her cup, and the stool is just a stool again. Free, like every
        /// refusal in this game.</summary>
        private void BackToYourOwnStool(StoolSeat s)
        {
            s.WithNeighbour = false;
            s.Scene = TheStools.TheStool(s.Index);
            s.Said.Clear();
            _host.StateHasChanged();
        }

        // ── WAITING, AND WHETHER SHE TURNS ────────────────────────────────────────────────────────────────

        /// <summary>
        /// #756 · One beat of sitting there. Core decides whether the one beside you speaks; this counts the
        /// beats and applies the answer.
        ///
        /// <para>THE BEAT COUNTER IS THE ROOM'S (<c>ex.TableWaits</c>, the same dictionary the tops use, under a
        /// stool key), and that is the anti-abuse law #757 already wrote: the roll is seeded on the beat, so a
        /// captain who got down and back up again simply carries on from the beat they were on. There is no
        /// re-pressing your way into company at a bar either.</para>
        ///
        /// <para>ONE APPROACH PER STOOL PER WATCH. She turned to you, and whichever way that went, it went.</para>
        ///
        /// <para>AND THE WATCH IS NOT TOUCHED — a wait is a beat inside the frozen shift (#709), never a nudge to
        /// the clock. A wait that re-dated the room would make the drawn room and the pressed room two rooms.</para>
        /// </summary>
        private void StoolWaited(SurfaceExcursion ex, StoolSeat s)
        {
            ex.TableWaits.TryGetValue(s.Key, out int beat);
            ex.TableWaits[s.Key] = beat + 1;

            bool turns = !ex.TableApproached.Contains(s.Key)
                && (_host.NeighbourCheat
                    ?? TheStools.SomebodyTurns(ex.Stop.Body.Id, ex.Floor, s.Index, ex.CanteenWatch, beat));

            if (!turns)
            {
                // NOTHING HAPPENING IS AN ANSWER, and it is said on the card the captain pressed. Which silence
                // it is depends on whether anybody is actually beside you — Core's own question, asked of Core,
                // and the difference between "nobody could have spoken" and "somebody could, and did not".
                _host.BarNotice = TheStools.NobodySpoke(
                    ex.CanteenWatch, beat,
                    TheStools.HasNeighbour(ex.Stop.Body.Id, ex.Floor, s.Index, ex.CanteenWatch));
                _host.StateHasChanged();
                return;
            }

            ex.TableApproached.Add(s.Key);
            SheTurnsToYou(s);
        }

        /// <summary>
        /// #756 · SHE TURNS — and she does not ask to sit, because she is sitting.
        ///
        /// <para>THE SAME CARD, not a second one. Swapping the scene in place keeps the outcome the captain is
        /// mid-way through reading on the screen, and keeps the menu under it exactly where it was — which is
        /// what "the keep serves you seated" costs in code, and it is nothing.</para>
        /// </summary>
        private void SheTurnsToYou(StoolSeat s)
        {
            s.WithNeighbour = true;
            s.Scene = TheStools.TheNeighbour();
            s.Said.Clear();     // #749 · a new conversation: nothing has been said to HER yet.

            // #761 · Told clearly, on the surface the captain is looking at. She spoke; the game prints what she
            // said and does not leave her arrival to be inferred from a changed button row.
            _host.BarNotice = s.Scene.Opening;
            RendererInterop.PlayCue("reveal");
            _host.RequestVaultSave();
            _host.StateHasChanged();
        }
    }
}
