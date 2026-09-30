namespace SpaceSails.Core;

// #618 / #582 slice 1 · THE MAN AT THE DOOR — one row per way he can be keeping the Hive's top-level door, spread
// into DevStarts.All straight after the canteen on B1, the floor he keeps. A method, not a field, so no static
// initializer depends on which file is compiled first (#1163); a partial of its own because DevStarts.cs is held
// under the size gate.
public static partial class DevStarts
{
    /// <summary>#618 · Posted, absent, on his round — the same ground and the same shaft each time, with
    /// <c>&amp;guard=</c> forcing how the door is kept. Everything else (whether the word works, the paper that
    /// passes, the tide) is the window's own.</summary>
    private static IReadOnlyList<Entry> TheManAtTheDoor() =>
    [
        new("👮🚪", "The man at the door — posted",
            "Set down at the lift head. Ride the cage down to B1: a man in a coat stands beside the car and his "
            + "card goes up as you step out. While he keeps the door no car on B1 goes lower. Show a pass that "
            + "works here, talk your way in (one window in three), or MAKE A NOISE and run for the surface — "
            + "at the mouth of the tube the tide decides (#618).",
            "/map?secretlab=1&land=1&guard=posted"),
        new("👮🪑", "The man at the door — absent",
            "The same shaft on a window he is not there. Ride down to B1: the chair by the car has a coat on it, "
            + "one line is said the first time you reach it, and the panel's way down is the building's own "
            + "(#618).",
            "/map?secretlab=1&land=1&guard=absent"),
        new("👮🚶", "The man at the door — on his round",
            "The same shaft with him walking a beat between the door and the canteen: a minute and a half at "
            + "the car, then six minutes away. The door is his while he is at it and nobody's while he is not "
            + "— watch him and time him (#618).",
            "/map?secretlab=1&land=1&guard=round"),
    ];
}
