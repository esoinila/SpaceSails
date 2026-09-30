using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;
using Map = SpaceSails.Client.Pages.Map;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1202 (owner ruling 2026-09-29 evening) · <b>THE GROUND HERE GIVES THE WAY SHE SAID IT WOULD.</b> The tin's
/// search area stays tight — two squares round its seeded soft square — and the first time the captain stands
/// inside it on her ground, one told line says so, so the search is deliberate and never blind. Driven on a
/// booted page: outside the area nothing; never before her word about the tin; never over a line still in the
/// slot; told once; filed nowhere.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheGroundGivesTests
{
    private const string OnLuna = "/map?dock=selene-gate&body=luna&site=1&land=1";

    private static List<Map.Quest> Quests(DeskBench b) => (List<Map.Quest>)b.Peek("_quests")!;

    private static Map.Quest Hers(DeskBench b) =>
        Assert.Single(Quests(b), q => q.Kind == Map.QuestKind.CarryThePress);

    private static string? InTheSlot(DeskBench b) => ((PulseSlot)b.Peek("_pulse")!).Message;

    private static void StandOn(DeskBench b, int squareX, int squareY)
    {
        (double x, double y) = BeachComber.SquareCenter(squareX, squareY);
        b.Poke("_avatarX", x);
        b.Poke("_avatarY", y);
    }

    /// <summary>
    /// OUTSIDE THE AREA NOTHING; INSIDE IT, ONCE, AFTER HER WORD AND BEHIND A BUSY SLOT. Her contract on the
    /// captain's ground with her word not yet said. Five squares off the tin, with the slot free, frame after
    /// frame: her word, and never the ground's line. Inside the area with her word said but the slot still
    /// holding it: nothing. The slot free: the ground's line, the contract remembers it, and the book has no
    /// entry for it. Free again, frame after frame, standing right on the tin: never a second time.
    ///
    /// <para><b>RED</b> on revert (the told block switched off in <c>AdvanceTheStringer</c>): the ground's line
    /// was never in the slot. <b>RED</b> by dropping its <c>_pulse.Message is null</c> clause: the ground wrote
    /// over her word in the slot.</para>
    /// </summary>
    [Fact]
    public async Task TheGroundIsToldOnceInsideTheAreaAndNeverOutsideIt()
    {
        DeskBench b = await DeskBench.BootAsync(OnLuna);
        Assert.True(b.OnSurface, "premise: the land cheat put the captain on Luna");
        await b.RenderAsync();
        object ex = b.Peek("_surface")!;
        object siteObj = ex.GetType().GetProperty("Site")!.GetValue(ex)!;
        int site = (int)siteObj.GetType().GetProperty("Index")!.GetValue(siteObj)!;
        var hers = new Map.Quest("press-904", Map.QuestKind.CarryThePress, CarryThePress.Giver, "", "Luna",
            CarryThePress.CardTitle, "", 500, DestBodyId: "luna", SourceBodyId: "selene-gate",
            Pin: new CarryThePress.Passage(site).Write());
        Quests(b).Add(hers);
        var tin = ((int X, int Y)?)b.Call("TheTinsSquare", hers, ex);
        Assert.NotNull(tin);
        string landing = CarryThePress.Landing(CarryThePress.TheTin(hers.Id, "luna", site).BearingLine);

        // Outside: five squares off (the area is two), slot free every frame. Her word is said; the ground's never.
        int away = BeachComber.SquareCenter(tin!.Value.X, 0).X > Rendering.MoonSurface.SpawnX ? -5 : 5;
        StandOn(b, tin.Value.X + away, tin.Value.Y);
        Assert.False(CarryThePress.StandsWhereTheTinIs(tin.Value, (double)b.Peek("_avatarX")!, (double)b.Peek("_avatarY")!));
        var said = new List<string?>();
        for (int frame = 0; frame < 20; frame++)
        {
            b.Poke("_pulse", PulseSlot.Empty);
            b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
            said.Add(InTheSlot(b));
        }

        Assert.Contains(landing, said);
        Assert.DoesNotContain(CarryThePress.GroundLine, said);
        Assert.True(CarryThePress.Passage.Read(Hers(b).Pin).Landed);
        Assert.False(CarryThePress.Passage.Read(Hers(b).Pin).Near);

        // Inside, but her word still holds the slot: the ground waits its turn.
        StandOn(b, tin.Value.X - Math.Sign(away) * -2, tin.Value.Y - 2);
        b.Poke("_pulse", PulseSlot.Empty);
        b.CallOnTheDispatcher("ShowPulseMessage", landing, PulseRank.Status);
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        Assert.Equal(landing, InTheSlot(b));
        Assert.False(CarryThePress.Passage.Read(Hers(b).Pin).Near);

        // The slot comes free: it is told, remembered, and filed nowhere.
        b.Poke("_pulse", PulseSlot.Empty);
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        Assert.Equal(CarryThePress.GroundLine, InTheSlot(b));
        Assert.True(CarryThePress.Passage.Read(Hers(b).Pin).Near);
        Assert.DoesNotContain((IEnumerable<FieldNote>)b.Peek("_fieldNotes")!,
            n => n.Text.Contains(CarryThePress.GroundLine, StringComparison.Ordinal));

        // …and never twice, not even standing on the tin itself.
        StandOn(b, tin.Value.X, tin.Value.Y);
        for (int frame = 0; frame < 20; frame++)
        {
            b.Poke("_pulse", PulseSlot.Empty);
            b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
            Assert.NotEqual(CarryThePress.GroundLine, InTheSlot(b));
        }
    }

    /// <summary>
    /// NEVER BEFORE HER WORD. Standing inside the area from the first frame on her ground, with the slot free:
    /// the first thing said is her word about the tin, and the ground's line only on a later free slot.
    ///
    /// <para><b>RED</b> by dropping the told block's <c>_pulse.Message is null</c> clause: the ground's line
    /// wrote over her word on the very frame she said it.</para>
    /// </summary>
    [Fact]
    public async Task TheGroundNeverSpeaksBeforeHerWord()
    {
        DeskBench b = await DeskBench.BootAsync(OnLuna);
        await b.RenderAsync();
        object ex = b.Peek("_surface")!;
        object siteObj = ex.GetType().GetProperty("Site")!.GetValue(ex)!;
        int site = (int)siteObj.GetType().GetProperty("Index")!.GetValue(siteObj)!;
        var hers = new Map.Quest("press-905", Map.QuestKind.CarryThePress, CarryThePress.Giver, "", "Luna",
            CarryThePress.CardTitle, "", 500, DestBodyId: "luna", SourceBodyId: "selene-gate",
            Pin: new CarryThePress.Passage(site).Write());
        Quests(b).Add(hers);
        var tin = ((int X, int Y)?)b.Call("TheTinsSquare", hers, ex);
        Assert.NotNull(tin);
        string landing = CarryThePress.Landing(CarryThePress.TheTin(hers.Id, "luna", site).BearingLine);

        StandOn(b, tin!.Value.X, tin.Value.Y);
        b.Poke("_pulse", PulseSlot.Empty);
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        Assert.Equal(landing, InTheSlot(b));
        Assert.False(CarryThePress.Passage.Read(Hers(b).Pin).Near);

        b.Poke("_pulse", PulseSlot.Empty);
        b.CallOnTheDispatcher("AdvanceTheStringer", 0.05);
        Assert.Equal(CarryThePress.GroundLine, InTheSlot(b));
    }
}
