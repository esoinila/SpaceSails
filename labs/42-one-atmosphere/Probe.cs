// Lab 42 — One atmosphere, however many rooms it is standing in.
//
// Teaching voice: the owner read the pump rules back to me and they were wrong.
//
//   "If we only want to evacuate it then the doors to it need to be sealed … but if we evacuate multiple
//    spaces then doors between those do not need to be sealed. The only check we need is to make sure we
//    don't evacuate a room by accident of leaving its door open."
//
// I had written that as THREE rules — a per-room interlock, a special case for the corridor, and a refusal
// if any hatch stood open — and all three were the same rule wearing different clothes. A pump does not
// empty a ROOM. It empties the volume it is plumbed into, and that volume is however many compartments are
// standing open to each other. The question was never "is this door shut". It is:
//
//   WHICH SPACES AM I ABOUT TO EMPTY, AND DID THE CAPTAIN MEAN ALL OF THEM?
//
// Ask it that way and the three rules collapse into one connectivity query and one warning. This lab is the
// study half of that collapse:
//
//   A — THE GRAPH, on the smallest honest example: two rooms and a corridor, one hatch at a time, printing
//       the volume that answers. The whole idea in one readable page.
//   B — THE ACCIDENT THE OWNER NAMED: one hatch left open turns "pump the forward locker" into pumping half
//       the ship. Printed both ways, with the cost of each.
//   C — EVERY CONFIGURATION SHE HAS. Eight hatches is 256 arrangements, so the properties are not sampled
//       here — they are EXHAUSTED. Partition, symmetry, and isolation, checked on every ship that exists.
//   D — WHAT A VOLUME COSTS, and the closed loop: pump her down and the flood is exactly affordable.
//   E — THE TRAP THAT HID A REAL BUG for a whole evening, drawn as the two numbers that made it invisible.
//
// Every number is the SAME Core code the valve board runs on: HullVenting.SharedAtmosphere flooding across
// the door graph, HullVenting.PumpJob pricing what it finds. The lab and the game are one thing.
//
// IRONCLAD RULE: every number in labs/42-one-atmosphere/README.md came from running this probe.

using System.Globalization;
using SpaceSails.Core;

var ci = CultureInfo.InvariantCulture;

Console.WriteLine("LAB 42 — ONE ATMOSPHERE, HOWEVER MANY ROOMS IT IS STANDING IN");
Console.WriteLine("A flood fill across open doors, so a pump prices what it will actually empty.");
Console.WriteLine();

static HullVenting.Space Room(string name, bool shut, bool vented = false) =>
    new(name, DoorShut: shut, Vented: vented, Infested: false, HoldsSurvivor: false);

static string Show(IReadOnlyList<string> volume) => string.Join(" + ", volume);

// ── A · the graph, on the smallest honest example ─────────────────────────────────────────────────────
//
// Two compartments, one corridor. Every compartment on this hull has exactly one hatch and it opens onto
// the spine — so the graph is a STAR, and the fill terminates in one hop. That is worth seeing plainly
// before the real ship, because it is why the answer looks obvious and why writing the answer instead of
// the search would have been a mistake.
Console.WriteLine("A — THE GRAPH, ON TWO ROOMS AND A CORRIDOR");
Console.WriteLine();

foreach ((bool bridgeShut, bool crewShut) in new[] { (true, true), (false, true), (false, false) })
{
    HullVenting.Space[] ship = [Room("BRIDGE", bridgeShut), Room("CREW SPACES", crewShut)];

    string state = $"BRIDGE {(bridgeShut ? "dogged" : "open  ")}, CREW SPACES {(crewShut ? "dogged" : "open  ")}";
    Console.WriteLine($"  {state}");
    Console.WriteLine($"      press BRIDGE      → {Show(HullVenting.SharedAtmosphere("BRIDGE", ship))}");
    Console.WriteLine($"      press THE SPINE   → {Show(HullVenting.SharedAtmosphere(HullVenting.SpineName, ship))}");
    Console.WriteLine();
}

Console.WriteLine("  A dogged hatch is its own little world. That is the whole point of dogging it.");
Console.WriteLine();

// ── B · the accident the owner named ──────────────────────────────────────────────────────────────────
//
// "The only check we need is to make sure we don't evacuate a room by accident of leaving its door open."
// Here is that accident, priced. Same press, same button, one hatch different.
Console.WriteLine("B — THE ACCIDENT: ONE HATCH LEFT OPEN");
Console.WriteLine();

var full = new List<HullVenting.Space>();
foreach ((string name, float _, float _, bool _) in WreckLayout.Compartments)
{
    full.Add(Room(name, shut: true));
}

void PricePress(string pressed, IReadOnlyList<HullVenting.Space> ship)
{
    IReadOnlyList<string> volume = HullVenting.SharedAtmosphere(pressed, ship);
    (double seconds, int charges) = HullVenting.PumpJob(volume);

    Console.WriteLine($"      volume  : {Show(volume)}");
    Console.WriteLine(string.Create(ci, $"      cost    : {seconds:0}s, banks {charges} charges"));

    string warn = HullVenting.PumpReachesFurtherLine(pressed, volume);
    Console.WriteLine(warn.Length == 0
        ? "      warning : (none — it reaches nothing else)"
        : $"      warning : {warn}");
    Console.WriteLine();
}

Console.WriteLine("  Every hatch dogged, press FORWARD LOCKER:");
PricePress("FORWARD LOCKER", full);

var oneOpen = new List<HullVenting.Space>();
foreach (HullVenting.Space s in full)
{
    oneOpen.Add(s.Name == "FORWARD LOCKER" ? s with { DoorShut = false } : s);
}

Console.WriteLine("  The SAME press, with that one hatch left standing open:");
PricePress("FORWARD LOCKER", oneOpen);

var threeOpen = new List<HullVenting.Space>();
foreach (HullVenting.Space s in full)
{
    threeOpen.Add(s.Name is "FORWARD LOCKER" or "BRIDGE" or "CREW SPACES" ? s with { DoorShut = false } : s);
}

Console.WriteLine("  And with the forward half open — a captain evacuating the bow ON PURPOSE:");
PricePress("FORWARD LOCKER", threeOpen);

Console.WriteLine("  The rule does not refuse any of these. It NAMES what is going, because the accident");
Console.WriteLine("  being guarded against is a hatch forgotten, never a decision taken.");
Console.WriteLine();

// ── C · every configuration she has ───────────────────────────────────────────────────────────────────
//
// Eight compartments, one hatch each: 2^8 = 256 ships. Small enough to check ALL of them, which turns
// three properties from "tested" into "true".
Console.WriteLine("C — EVERY CONFIGURATION OF HER HATCHES (2^8 = 256 SHIPS)");
Console.WriteLine();

string[] names = new string[WreckLayout.Compartments.Length];
for (int i = 0; i < WreckLayout.Compartments.Length; i++)
{
    names[i] = WreckLayout.Compartments[i].Name;
}

var everySpace = new List<string>(names) { HullVenting.SpineName };
var volumeCounts = new Dictionary<int, int>();
int partitionFailures = 0, symmetryFailures = 0, isolationFailures = 0;
int biggest = 0;
string biggestState = "";

for (int mask = 0; mask < 1 << names.Length; mask++)
{
    var ship = new List<HullVenting.Space>(names.Length);
    for (int i = 0; i < names.Length; i++)
    {
        ship.Add(Room(names[i], shut: (mask & (1 << i)) != 0));
    }

    // PARTITION — every space lands in exactly one volume. If this ever failed, a pump could empty a room
    // twice or leave one unaccounted for, and the charge arithmetic would drift without anything erroring.
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var distinct = new HashSet<string>(StringComparer.Ordinal);
    foreach (string space in everySpace)
    {
        IReadOnlyList<string> volume = HullVenting.SharedAtmosphere(space, ship);
        distinct.Add(string.Join("+", volume));

        if (!volume.Contains(space, StringComparer.Ordinal))
        {
            partitionFailures++;
        }

        // SYMMETRY — press any member and the same volume answers. This is the property that makes it a
        // VOLUME rather than a direction, and it is the one a hand-written pair of ifs would break first.
        foreach (string member in volume)
        {
            if (string.Join("+", HullVenting.SharedAtmosphere(member, ship)) != string.Join("+", volume))
            {
                symmetryFailures++;
            }
        }

        if (volume.Count > biggest)
        {
            biggest = volume.Count;
            biggestState = $"{names.Length - System.Numerics.BitOperations.PopCount((uint)mask)} hatches open";
        }
    }

    // ISOLATION — a dogged hatch is alone, always, whatever the rest of the ship is doing.
    for (int i = 0; i < names.Length; i++)
    {
        if ((mask & (1 << i)) != 0 && HullVenting.SharedAtmosphere(names[i], ship).Count != 1)
        {
            isolationFailures++;
        }
    }

    volumeCounts.TryGetValue(distinct.Count, out int had);
    volumeCounts[distinct.Count] = had + 1;
    _ = seen;
}

Console.WriteLine($"      partition failures : {partitionFailures}");
Console.WriteLine($"      symmetry failures  : {symmetryFailures}");
Console.WriteLine($"      isolation failures : {isolationFailures}");
Console.WriteLine($"      largest atmosphere : {biggest} spaces ({biggestState})");
Console.WriteLine();
Console.WriteLine("      distinct atmospheres per ship:");
foreach (int count in volumeCounts.Keys.Order())
{
    Console.WriteLine($"        {count,2} volume(s) : {volumeCounts[count],4} of 256 configurations");
}
Console.WriteLine();
Console.WriteLine("  That is C(8,k) exactly — and it should be, because k dogged hatches make k volumes of");
Console.WriteLine("  one room each plus one for whatever is still open to the corridor. Nine volumes is the");
Console.WriteLine("  ship shut tight; one volume is every hatch standing open and the whole hull breathing");
Console.WriteLine("  together. The distribution falling out binomial is the model checking its own arithmetic.");
Console.WriteLine();

// ── D · what a volume costs, and the loop that closes ─────────────────────────────────────────────────
Console.WriteLine("D — WHAT A VOLUME COSTS");
Console.WriteLine();
Console.WriteLine(string.Create(ci, $"      one room          : {HullVenting.PumpTotalSeconds(spine: false):0}s, banks {HullVenting.PumpYield(spine: false)}"));
Console.WriteLine(string.Create(ci, $"      the corridor      : {HullVenting.PumpTotalSeconds(spine: true):0}s, banks {HullVenting.PumpYield(spine: true)}"));

var wideOpen = new List<HullVenting.Space>();
foreach (string n in names)
{
    wideOpen.Add(Room(n, shut: false));
}
IReadOnlyList<string> whole = HullVenting.SharedAtmosphere(HullVenting.SpineName, wideOpen);
(double wholeSeconds, int wholeCharges) = HullVenting.PumpJob(whole);
Console.WriteLine(string.Create(ci, $"      the whole ship    : {wholeSeconds:0}s in one run, banks {wholeCharges}"));
Console.WriteLine();

int sealedSeparately = (names.Length * HullVenting.PumpYield(spine: false)) + HullVenting.PumpYield(spine: true);
int floodCost = HullVenting.WholeShipRefillCost(names.Length);
Console.WriteLine($"      pumped compartment by compartment, she banks : {sealedSeparately} charges");
Console.WriteLine($"      flooding her whole hull back costs           : {floodCost} charges");
Console.WriteLine($"      the away team carries                        : {HullVenting.RefillChargesPerBoarding} to start with");
Console.WriteLine();
Console.WriteLine("  THE LOOP CLOSES. You can put back exactly the air you banked — which is what makes the");
Console.WriteLine("  equalisation valve a real decision rather than a shortcut: valve air is gone forever,");
Console.WriteLine("  pumped air is in the tanks. And dogging her hatches first is not only safer, it is");
Console.WriteLine("  FASTER, because eight small pumps run at once while one big one runs end to end.");
Console.WriteLine();

// ── E · the trap that hid a real bug ──────────────────────────────────────────────────────────────────
//
// The owner: "I run out of air trying to fill the ship … I even had used the pumping on all so I should
// still have the air mostly plus the reserves." He should have. The rough mark — the countdown value where
// the air lands in the tanks — was one variable shared across every pump in a frame.
Console.WriteLine("E — THE TRAP: WHY A LOST CHARGE LOOKED LIKE NOTHING AT ALL");
Console.WriteLine();
Console.WriteLine(string.Create(ci, $"      a room's pump counts down from : {HullVenting.PumpTotalSeconds(spine: false):0}s"));
Console.WriteLine(string.Create(ci, $"      and banks when it crosses      : {HullVenting.PumpRoughMark(spine: false):0}s"));
Console.WriteLine(string.Create(ci, $"      the corridor banks when it hits: {HullVenting.PumpRoughMark(spine: true):0}s"));
Console.WriteLine();

bool roomCanEverCrossSpineMark = HullVenting.PumpTotalSeconds(spine: false) > HullVenting.PumpRoughMark(spine: true);
Console.WriteLine($"      can a room ever reach the corridor's mark? {(roomCanEverCrossSpineMark ? "yes" : "NO")}");
Console.WriteLine();
Console.WriteLine("  There it is. A room's whole run STARTS BELOW the corridor's mark, so measuring a room");
Console.WriteLine("  against the corridor's number does not throw — it silently never fires. The room empties,");
Console.WriteLine("  the pump finishes, the log line prints, and nothing arrives in the tanks. The mark is a");
Console.WriteLine("  pure function of one bool now (HullVenting.PumpRoughMark), so nothing can be left");
Console.WriteLine("  holding another pump's number.");
Console.WriteLine();

Console.WriteLine("WHAT CI HOLDS FROM THIS LAB");
Console.WriteLine("  HullVentingTests — a dogged hatch is its own atmosphere; an open one joins the corridor");
Console.WriteLine("  and every other open room; reaching is symmetric; a bigger volume takes longer and pays");
Console.WriteLine("  more; the board names what it will reach; and the 256-ship sweep above runs there too.");

int failures = partitionFailures + symmetryFailures + isolationFailures;
Console.WriteLine();
Console.WriteLine(failures == 0
    ? "ALL 256 SHIPS AGREE."
    : $"*** {failures} PROPERTY FAILURES — the door graph is lying somewhere. ***");
return failures == 0 ? 0 : 1;
