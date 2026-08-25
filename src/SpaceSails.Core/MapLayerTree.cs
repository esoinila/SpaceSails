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

    /// <summary>One toggleable thing on the map — the atom the draw path and picker gate on.
    /// <para><paramref name="Hint"/> is the sentence the row hovers (#963: the owner met the 🛬 beside
    /// Ganymede and could not tell what it meant — "it should have some kind of text pop-up?"). A glyph
    /// drawn on a canvas cannot carry a tooltip, so the LEGEND has to: the layer that draws the mark is
    /// where its meaning is written down. Empty for the leaves whose label already says it all.</para></summary>
    public sealed record Leaf(string Key, string Label, string Icon, string Hint = "");

    /// <summary>A collapsible family of leaves with a cascading tri-state parent checkbox.</summary>
    public sealed record Group(
        string Key,
        string Label,
        string Icon,
        IReadOnlyList<Leaf> Leaves,
        bool Pinned = false,
        bool DefaultCollapsed = false);

    /// <summary>#953 · THE ROUTES FAMILY, MINUS ITS ARCHIVED MEMBER. <see cref="ShipLanes.Archived"/> is the
    /// one flag the owner's ruling ("we have never used them to find anything") is written on, and this is its
    /// one consumer. While it stands there is no Trade lanes row to tick — not a row that ticks nothing, which
    /// is the worse of the two ways to retire a layer. Flip the flag and the row is back in its old place.
    ///
    /// <para>Declared ABOVE <see cref="Groups"/> on purpose: static field initialisers run in declaration
    /// order, and a list read before it is built is an empty Routes family and a very quiet bug.</para></summary>
    private static readonly IReadOnlyList<Leaf> RouteLeaves = ShipLanes.Archived
        ?
        [
            new("routes.plan", "Flight plan & burns", "✦"),
            new("routes.rails", "Orbit rails / ellipses", "◯"),
        ]
        :
        [
            new("routes.lanes", "Trade lanes", "🛣"),
            new("routes.plan", "Flight plan & burns", "✦"),
            new("routes.rails", "Orbit rails / ellipses", "◯"),
        ];

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
        new("routes", "Routes", "🛣", RouteLeaves, DefaultCollapsed: true),
        new("sensors", "Sensors", "🔭",
        [
            new("sensors.scans", "Sensor overlays / scans", "🔭"),
            new("sensors.corridors", "Scan corridors", "▨"),
        ]),
        new("labels", "Labels", "🏷",
        [
            new("labels.bodies", "Body names", "🏷"),
            new("labels.minor", "Minor / depot labels", "·"),
            new("labels.landable", "Landable marks", "🛬", "🛬 landable — a surface you can go down to: ride the shuttle to the ground. Bright when the ground is in shuttle range right now, dim when it is landable only in principle."),
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

    /// <summary>The per-desk starting hidden set. #971 started every desk — the sensors chief included — with
    /// the trade lanes hidden, after the owner opened his sensors desk onto a sky "covered in faint lines with
    /// no intersection". #953 finished the thought: an overlay nobody ever ticked on is archived rather than
    /// merely defaulted off, so there is no lane key left to hide and every desk opens on the whole tree.
    /// The parameter stays because per-desk defaults are the shape of this call, not a historical accident —
    /// the next layer that needs one has a place to put it.</summary>
    public static HashSet<string> DefaultHidden(bool isSensorsDesk) =>
        ShipLanes.Archived ? [] : ["routes.lanes"];

    /// <summary>Where an old flat layer key (lanes / traffic / depots / scans) lands in the tree.
    /// Not called at runtime — the hidden sets are session-scoped, never persisted — but it pins the
    /// rename so a future save format (or a reader of old notes) has one authority, and it's tested.
    /// Note depots→{ports.depots, labels.minor}: the old "depots" key gated BOTH the depot markers and
    /// the #404 minor-station labels; the tree splits those responsibilities.
    /// <para>#953: "lanes" now lands NOWHERE while the ship lanes are archived. An old note asking for that
    /// layer is asking for a layer that no longer exists, and answering with a key the tree does not carry
    /// would hand a caller a hidden-set entry that gates nothing.</para></summary>
    public static IReadOnlyList<string> MigrateLegacyKey(string oldKey) => oldKey switch
    {
        "lanes" => ShipLanes.Archived ? [] : ["routes.lanes"],
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
