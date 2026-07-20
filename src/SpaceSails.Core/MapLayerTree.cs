namespace SpaceSails.Core;

/// <summary>
/// The map's Layers filter, modelled as a small collapsible TREE (#405). The owner's worry was a
/// wall of checkboxes: the answer is parent families you can collapse to hide the clutter, each a
/// tri-state (on / off / mixed) that cascades to its leaf toggles.
///
/// <para>The leaf key is the single source of truth the draw path and the click-picker query
/// (<c>LayerVisible(leafKey)</c>); a hidden leaf neither draws nor answers clicks. This class is the
/// pure resolution behind that call — kept in Core, out of the razor, so the tri-state cascade, the
/// legacy-key migration, the per-desk defaults and the never-hide-a-threat invariant are unit-tested
/// (the same reason <see cref="MenuLayout"/> lives here).</para>
///
/// <para>THREATS ARE PINNED — a safety invariant: an inbound-rock / collision warning is never
/// hideable. A pinned leaf resolves visible no matter what the hidden set says, and cannot be added
/// to it. The render of the threat itself doesn't even consult this class — see DrawAsteroidThreat.</para>
/// </summary>
public static class MapLayerTree
{
    public enum TriState { Off, On, Mixed }

    /// <summary>One toggleable thing on the map — the atom the draw path and picker gate on.</summary>
    public sealed record Leaf(string Key, string Label, string Icon);

    /// <summary>A collapsible family of leaves with a cascading tri-state parent checkbox.</summary>
    public sealed record Group(
        string Key,
        string Label,
        string Icon,
        IReadOnlyList<Leaf> Leaves,
        bool Pinned = false,
        bool DefaultCollapsed = false);

    /// <summary>The tree, in display order (top of the corner panel to the bottom). Parent order is
    /// the owner's #405 comment; Routes rides collapsed by default (the owner's habit), Threats is
    /// pinned last as the always-legible safety family.</summary>
    public static readonly IReadOnlyList<Group> Groups =
    [
        new("traffic", "Traffic", "🛰",
        [
            new("traffic.live", "Live contacts", "•"),
            new("traffic.ghosts", "Last-seen ghosts", "◦"),
            new("traffic.beacons", "Beacons", "🎭"),
        ]),
        new("ports", "Ports & depots", "📦",
        [
            new("ports.havens", "Dock havens", "⚓"),
            new("ports.depots", "Cargo depots", "📦"),
        ]),
        new("routes", "Routes", "🛣",
        [
            new("routes.lanes", "Trade lanes", "🛣"),
            new("routes.plan", "Flight plan & burns", "✦"),
            new("routes.rails", "Orbit rails / ellipses", "◯"),
        ], DefaultCollapsed: true),
        new("sensors", "Sensors", "🔭",
        [
            new("sensors.scans", "Sensor overlays / scans", "🔭"),
            new("sensors.corridors", "Scan corridors", "▨"),
        ]),
        new("labels", "Labels", "🏷",
        [
            new("labels.bodies", "Body names", "🏷"),
            new("labels.minor", "Minor / depot labels", "·"),
            new("labels.landable", "Landable marks", "🛬"),
        ]),
        new("finds", "Ground finds", "⛏",
        [
            new("finds.treasure", "Treasure ✗", "✗"),
            new("finds.husks", "Husks", "☠"),
        ]),
        new("threats", "Threats", "⚠",
        [
            new("threats.rock", "Inbound rock / collision", "⚠"),
        ], Pinned: true),
    ];

    /// <summary>Every leaf key, in tree order.</summary>
    public static IEnumerable<string> AllLeafKeys => Groups.SelectMany(g => g.Leaves.Select(l => l.Key));

    /// <summary>The group a leaf key belongs to, or null for an unknown key.</summary>
    public static Group? GroupOf(string leafKey) =>
        Groups.FirstOrDefault(g => g.Leaves.Any(l => l.Key == leafKey));

    /// <summary>A pinned leaf can never be hidden (the threats safety invariant).</summary>
    public static bool IsPinnedLeaf(string leafKey) => GroupOf(leafKey) is { Pinned: true };

    /// <summary>The single source of truth every draw-path / picker call resolves through: a pinned
    /// leaf is ALWAYS visible; any other leaf is visible unless it sits in the hidden set.</summary>
    public static bool IsVisible(IReadOnlySet<string> hidden, string leafKey) =>
        IsPinnedLeaf(leafKey) || !hidden.Contains(leafKey);

    /// <summary>The per-desk starting hidden set. The sensors chief opens on the full working sky;
    /// every other desk starts with the trade lanes off — the clutter the owner flagged — after which
    /// each desk remembers its own picks. (Preserves the pre-tree lanes-off default.)</summary>
    public static HashSet<string> DefaultHidden(bool isSensorsDesk) =>
        isSensorsDesk ? new HashSet<string>() : new HashSet<string> { "routes.lanes" };

    /// <summary>Where an old flat layer key (lanes / traffic / depots / scans) lands in the tree.
    /// Not called at runtime — the hidden sets are session-scoped, never persisted — but it pins the
    /// rename so a future save format (or a reader of old notes) has one authority, and it's tested.
    /// Note depots→{ports.depots, labels.minor}: the old "depots" key gated BOTH the depot markers and
    /// the #404 minor-station labels; the tree splits those responsibilities.</summary>
    public static IReadOnlyList<string> MigrateLegacyKey(string oldKey) => oldKey switch
    {
        "lanes" => ["routes.lanes"],
        "traffic" => ["traffic.live", "traffic.ghosts", "traffic.beacons"],
        "depots" => ["ports.depots", "labels.minor"],
        "scans" => ["sensors.scans", "sensors.corridors"],
        _ => [oldKey],
    };

    /// <summary>A parent's tri-state, read off its children: all visible = On, none = Off, a mix =
    /// Mixed. A pinned group is always On (its children can't be hidden).</summary>
    public static TriState GroupStateOf(IReadOnlySet<string> hidden, Group group)
    {
        if (group.Pinned)
        {
            return TriState.On;
        }

        int visible = 0;
        foreach (Leaf leaf in group.Leaves)
        {
            if (!hidden.Contains(leaf.Key))
            {
                visible++;
            }
        }

        return visible == 0 ? TriState.Off
            : visible == group.Leaves.Count ? TriState.On
            : TriState.Mixed;
    }

    /// <summary>Click the parent checkbox: the standard tri-state convention — fully On turns the
    /// whole family Off; Off or Mixed turns it all On. A pinned group is inert (its safety children
    /// stay on). Mutates <paramref name="hidden"/> in place.</summary>
    public static void CascadeGroup(HashSet<string> hidden, Group group)
    {
        if (group.Pinned)
        {
            return;
        }

        bool turnOn = GroupStateOf(hidden, group) != TriState.On;
        foreach (Leaf leaf in group.Leaves)
        {
            if (turnOn)
            {
                hidden.Remove(leaf.Key);
            }
            else
            {
                hidden.Add(leaf.Key);
            }
        }
    }

    /// <summary>Toggle a single leaf's visibility. A pinned leaf is inert — it can never be added to
    /// the hidden set (the threats invariant, enforced here as well as in <see cref="IsVisible"/>).
    /// Mutates <paramref name="hidden"/> in place.</summary>
    public static void ToggleLeaf(HashSet<string> hidden, string leafKey)
    {
        if (IsPinnedLeaf(leafKey))
        {
            return;
        }

        if (!hidden.Remove(leafKey))
        {
            hidden.Add(leafKey);
        }
    }
}
