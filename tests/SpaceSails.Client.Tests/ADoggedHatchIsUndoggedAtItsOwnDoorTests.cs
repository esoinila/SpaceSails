using System.Collections.Generic;
using System.Reflection;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1381 · <b>E AT A DOGGED HATCH OPENS THAT HATCH'S CARD</b> - not the loaded door's, ~400 px away, and not
/// nothing once the valve is cracked. The deck walls every <c>BlockedDoors()</c> doorway (loaded OR dogged by
/// hand); the press has to dispatch on the same set.
///
/// <para><b>Proven RED</b> by putting <c>DoorHeldByPressure</c> back in <c>OpenPressureDoorCard</c>: with
/// ENGINEERING loaded the press at BRIDGE chose ENGINEERING; with nothing loaded it chose nothing.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ADoggedHatchIsUndoggedAtItsOwnDoorTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static void Set(object o, string field, object? value) =>
        typeof(Map).GetField(field, Hidden)!.SetValue(o, value);

    private static Map Aboard(bool engineeringLoaded)
    {
        var map = new Map();
        Set(map, "_wreck", Derelict.SeededWithCause(Derelict.WreckCause.VentedByOneOfTheirOwn) ?? Derelict.Seeded("kestrel-3"));
        Set(map, "_spinePressurised", true);
        var spaces = (Dictionary<string, HullVenting.Space>)typeof(Map).GetField("_ventSpaces", Hidden)!.GetValue(map)!;
        foreach ((string name, float _, float _, bool _) in WreckLayout.Compartments)
        {
            bool dogged = name == "BRIDGE";
            bool loaded = engineeringLoaded && name == HullVenting.ValveCompartment;
            spaces[name] = new HullVenting.Space(
                Name: name, DoorShut: dogged, Vented: loaded, Infested: false, HoldsSurvivor: false,
                CaptainInside: false, VacuumSeconds: 0.0, Kind: default);
        }
        return map;
    }

    private static string? PressAtTheBridgeDoor(Map map)
    {
        (string _, float x0, float x1, bool top) = System.Array.Find(WreckLayout.Compartments, c => c.Name == "BRIDGE");
        float x = (float)typeof(Map).GetMethod("VentDoorX", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [x0, x1])!;
        Set(map, "_avatarX", (double)x);
        Set(map, "_avatarY", top ? -WreckLayout.SpineHalfHeight : WreckLayout.SpineHalfHeight);
        typeof(Map).GetMethod("OpenPressureDoorCard", Hidden)!.Invoke(map, null);
        return (string?)typeof(Map).GetField("_pressureDoor", Hidden)!.GetValue(map);
    }

    [Theory]
    [InlineData(true)]    // ENGINEERING is ten tonnes loaded 400 px away - the card must still be the BRIDGE's
    [InlineData(false)]   // nothing loaded at all (the valve was cracked) - the dogged hatch still answers
    public void PressingEAtADoggedHatchOpensThatHatch(bool engineeringLoaded)
    {
        Map map = Aboard(engineeringLoaded);
        Assert.Equal("BRIDGE", PressAtTheBridgeDoor(map));
    }
}
