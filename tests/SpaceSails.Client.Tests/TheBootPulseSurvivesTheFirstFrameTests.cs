using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1390 · A BOOT-TIME DEV PULSE IS STILL ON THE DECK AFTER THE FIRST FRAME.
///
/// <para>The boot's stages say their lines (the <c>?land=</c> refusals, the <c>?deflection=</c> test pulse)
/// before the client's clock has started, so the slot stamped them at 0 ms; the first real frame reads a clock
/// thousands of milliseconds old and its <c>Expire</c> cleared the line unseen. The guard boots past the gate,
/// runs the first frame's clock (<c>TakeTheFrameClock</c>) and the pulse's expiry as the tick does, and judges
/// what the slot says AFTER — plus that the line was filed in the ledger.</para>
/// </summary>
public sealed class TheBootPulseSurvivesTheFirstFrameTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const double FirstFrameMs = 20000.0;

    public static IEnumerable<object[]> TheDevBoots() =>
    [
        ["/map?dock=the-tilt&land=titan", "not landable from this berth"],
        ["/map?deflection=1", "deflection gig accepted"],
        // U01-F18 (#1415) · the oracle cheat names her corner; it is a DEV line and rides the same hold.
        ["/map?oracle=1&ashore=1", "Static Marsh has the port-back corner"],
    ];

    [Theory]
    [MemberData(nameof(TheDevBoots))]
    public async Task TheLineIsStillUpAfterTheFirstFrameAndFiledInTheLedger(string url, string fragment)
    {
        PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync(url);
        Assert.Null(booted.Threw);
        await Task.Delay(200); // the land cheat is fire-and-forget; its refusal is synchronous up to its first await

        Map page = booted.Page;
        Assert.Contains(booted.Read<List<string>>("_bootDevLines"), l => l.Contains(fragment));

        typeof(Map).GetMethod("TakeTheFrameClock", Hidden)!.Invoke(page, [FirstFrameMs]);
        PulseSlot after = booted.Read<PulseSlot>("_pulse").Expire(FirstFrameMs + 100);

        Assert.Contains(fragment, after.Message ?? "");

        var notes = (IEnumerable<FieldNote>)booted.Field("_fieldNotes")!;
        Assert.Contains(notes, n => n.Glyph == "🧪" && n.Text.Contains(fragment));
    }
}
