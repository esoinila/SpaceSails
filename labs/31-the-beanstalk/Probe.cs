// Lab 31 — The beanstalk: where in Sol does a space elevator work with mundane string?
//
// Teaching voice: everybody "knows" the space elevator is science fiction, and everybody is quoting
// EARTH. Earth is the hardest place in the inner system to build one, by a margin so wide that the
// answer is not "not yet" but "not with matter". The interesting question is the one the owner asked
// on a Friday morning — "a space elevator SOMEWHERE where materials stress would not be an issue" —
// and it has a real answer, several of them, and they are already places in this game.
//
// The whole lab is one exponential (Beanstalk.Taper) evaluated honestly per body. What it costs to
// stand a cable up is the EFFECTIVE-POTENTIAL CLIMB from the anchor to the point of maximum tension,
// divided by the material's specific strength. Weak gravity and a fast spin make that climb small;
// a tidally locked moon has no spin to speak of, so its cable is hung from its primary through L1
// instead. Nothing here is a new idea — Artsutanov 1960, Isaacs 1966, Pearson 1975, and Pearson's
// 2005 lunar study did all of it — and that is the point: the arithmetic is public and the answer
// has been sitting in it for fifty years.
//
// The verdicts in Section D are PRINTED FROM THE TABLE, never asserted: each one is
// Beanstalk.Verdict() applied to that body's own climb, and tests/SpaceSails.Core.Tests/
// Lab31BeanstalkTests.cs links this lab's Beanstalk.cs and holds every printed verdict to the
// numbers above it, and to this README's copy of them.
//
// IRONCLAD RULE: every number in labs/31-the-beanstalk/README.md came from running this probe.
// Change the code and the README goes stale — rerun and re-paste, never hand-edit a table.

using SpaceSails.Labs.Lab31;

IReadOnlyList<Climb> climbs = Beanstalk.MeasureAll();

// ===================================================================================
// Section A — the bodies, and where their constants come from
// ===================================================================================
Console.WriteLine("=== Section A: the bodies (constants, and whose they are) ===");
Console.WriteLine("The game's own ephemeris supplies every body it actually carries; Ceres and Deimos are not in");
Console.WriteLine("sol.json, so they are cited to the usual references instead. A lab that mixes the two without");
Console.WriteLine("saying so is quoting itself.");
Console.WriteLine();
Console.WriteLine($"{"body",-9}{"anchor",-14}{"mu m3/s2",13}{"radius km",12}{"day (h)",10}{"g m/s2",9}  source");
Console.WriteLine(new string('-', 96));
foreach (TetherBody b in Beanstalk.Bodies)
{
    double omega = b.Anchor == AnchorKind.Synchronous ? b.SpinRate : Beanstalk.MeanMotion(b);
    string anchor = b.Anchor == AnchorKind.Synchronous ? "synchronous" : "through L1";
    Console.WriteLine($"{b.Name,-9}{anchor,-14}{b.Mu,13:0.000e+00}{b.Radius / 1000,12:F1}" +
                      $"{2 * Math.PI / omega / 3600,10:F2}{b.SurfaceGravity,9:F3}  {b.Source}");
}

Console.WriteLine();
Console.WriteLine("Luna, Phobos and Deimos are tidally locked: their day IS their year, so their own synchronous");
Console.WriteLine("radius falls outside their Hill sphere and there is no cable to stand up. Those three are hung");
Console.WriteLine("from their primary through the interior Lagrange point instead — the tension peaks at L1.");
Console.WriteLine();
foreach (TetherBody b in Beanstalk.Bodies.Where(b => b.Anchor == AnchorKind.ThroughL1))
{
    double hill = Beanstalk.HillRadius(b);
    double l1 = Beanstalk.L1Distance(b);
    Console.WriteLine($"  {b.Name,-7} Hill radius {hill / 1000,10:F2} km   L1 at {l1 / 1000,10:F2} km   " +
                      $"= {l1 / b.Radius,5:F2} body radii, {(l1 - b.Radius) / 1000,9:F2} km of clear air");
}

Console.WriteLine();

// ===================================================================================
// Section B — the climb: how far up the potential the cable's waist sits
// ===================================================================================
Console.WriteLine("=== Section B: the potential climb (the only thing the material has to survive) ===");
Console.WriteLine("dPhi is the effective-potential difference — gravity plus the centrifugal term of the frame the");
Console.WriteLine("cable co-rotates with — between the surface anchor and the point of maximum tension. The");
Console.WriteLine("characteristic length is that climb divided by the body's OWN surface gravity: how tall the");
Console.WriteLine("cable would be if gravity never weakened. Everything after this is exp(dPhi / specific strength).");
Console.WriteLine();
Console.WriteLine($"{"body",-9}{"tension peak",14}{"altitude km",14}{"dPhi J/kg",14}{"char. length km",18}");
Console.WriteLine(new string('-', 69));
foreach (Climb c in climbs)
{
    string peak = c.Body.Anchor == AnchorKind.Synchronous ? "synchronous" : "L1";
    Console.WriteLine($"{c.Body.Name,-9}{peak,14}{c.AnchorAltitude / 1000,14:F2}" +
                      $"{c.DeltaPotential,14:G5}{c.CharacteristicLength / 1000,18:F2}");
}

Console.WriteLine();
Console.WriteLine("Read the last two rows again. Lifting one kilogram the WHOLE HEIGHT of a Phobos beanstalk — from");
Console.WriteLine("the ground to the point where the cable's tension peaks — costs about ten joules. A dropped");
Console.WriteLine("teaspoon on Earth does more work than that.");

Console.WriteLine();

// ===================================================================================
// Section C — the material table: taper ratio per body per material
// ===================================================================================
Console.WriteLine("=== Section C: taper ratio (widest cross-section / narrowest) ===");
Console.WriteLine("taper = exp(dPhi / (sigma/rho)). A taper of 2 is a cable twice as fat at the waist as at the");
Console.WriteLine("anchor; a taper of 1e5 is not an engineering problem, it is a category error.");
Console.WriteLine();
Console.WriteLine($"{"material",-26}{"sigma GPa",10}{"rho",7}{"MJ/kg",8}  {"tier",-12}");
Console.WriteLine(new string('-', 65));
foreach (TetherMaterial m in Beanstalk.Materials)
{
    Console.WriteLine($"{m.Name,-26}{m.TensileGPa,10:F1}{m.DensityKgM3,7:F0}{m.SpecificStrength / 1e6,8:F2}  {m.Tier,-12}");
}

Console.WriteLine();
Console.Write($"{"body",-9}");
foreach (TetherMaterial m in Beanstalk.Materials)
{
    Console.Write($"{m.ShortName,13}");
}

Console.WriteLine();
Console.WriteLine(new string('-', 9 + 13 * Beanstalk.Materials.Count));
foreach (Climb c in climbs)
{
    Console.Write($"{c.Body.Name,-9}");
    foreach (TetherMaterial m in Beanstalk.Materials)
    {
        Console.Write($"{Beanstalk.Format(Beanstalk.Taper(c.DeltaPotential, m)),13}");
    }

    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine($"Threshold, stated before the data: a cable is PRACTICAL at taper <= {Beanstalk.PracticalTaper:F0}, and the material has");
Console.WriteLine($"stopped mattering at all at taper <= {Beanstalk.RoundingErrorTaper:F1}. 'Real' means somebody has made some.");
Console.WriteLine();

// ===================================================================================
// Section D — the verdicts, read off Section C and nothing else
// ===================================================================================
Console.WriteLine("=== Section D: the verdicts (computed from the table above) ===");
foreach (Climb c in climbs)
{
    Console.WriteLine("  " + Beanstalk.VerdictLine(c));
}

Console.WriteLine();

// ===================================================================================
// Section E — usefulness: what the cable saves, and which body is the flagship
// ===================================================================================
Console.WriteLine("=== Section E: what it saves (1 t parcel, surface to where the cable lets go) ===");
Console.WriteLine("The rocket's bill is IDEAL — no gravity losses, no drag, and the ground's own rotation counted as");
Console.WriteLine("free speed — so Earth's real figure is 1.5-2 km/s worse than the one below. The cable's bill is");
Console.WriteLine($"zero propellant: a climber runs on mains power. Propellant is quoted at Isp {Beanstalk.ComparisonIsp:F0} s.");
Console.WriteLine();
Console.WriteLine($"{"body",-9}{"release point",16}{"rocket dv m/s",15}{"propellant kg/t",17}{"stock material?",17}");
Console.WriteLine(new string('-', 74));
foreach (Climb c in climbs)
{
    string release = c.Body.Anchor == AnchorKind.Synchronous ? "sync orbit" : "L1";
    string stock = Beanstalk.BuildableWithStock(c.DeltaPotential) ? "yes" : "no";
    Console.WriteLine($"{c.Body.Name,-9}{release,16}{c.LaunchDeltaV,15:F1}{c.PropellantPerTonneKg,17:F1}{stock,17}");
}

Console.WriteLine();
Climb flagship = Beanstalk.Flagship();
Console.WriteLine("Flagship rule, stated before the data: of the bodies whose cable can be spun from material you");
Console.WriteLine("can order by the kilometre TODAY, the flagship is the one where building it saves the most");
Console.WriteLine("propellant per tonne. A laboratory record is a measurement, not a supply chain, so it does not");
Console.WriteLine("qualify a body — which is exactly what disqualifies the biggest prize on the board.");
Console.WriteLine();
Console.WriteLine($"  FLAGSHIP: {flagship.Body.Name} — {Beanstalk.Verdict(flagship.DeltaPotential)}, " +
                  $"saving {flagship.LaunchDeltaV:F0} m/s = {flagship.PropellantPerTonneKg:F0} kg of propellant per tonne shipped.");
Console.WriteLine();

Climb biggestPrize = climbs.MaxBy(c => c.PropellantPerTonneKg)!;
if (biggestPrize.Body.Name != flagship.Body.Name)
{
    Console.WriteLine($"  (The biggest prize is {biggestPrize.Body.Name} at {biggestPrize.PropellantPerTonneKg:F0} kg/t — and it is " +
                      $"{Beanstalk.Verdict(biggestPrize.DeltaPotential)}.");
    Console.WriteLine("  The board's whole shape is that the cable gets easy exactly where the saving gets small.)");
    Console.WriteLine();
}

// ===================================================================================
// Section F — the two things that would move a verdict
// ===================================================================================
Console.WriteLine("=== Section F: what a safety factor does, and what Mars is waiting for ===");
Console.WriteLine($"Working stress is not breaking stress. Halve the allowable ({Beanstalk.SafetyFactor:F0}x safety factor) and the");
Console.WriteLine("exponent doubles, which SQUARES the taper — 3 becomes 9, 13 becomes 167. This is where 'nearly'");
Console.WriteLine("stops being nearly.");
Console.WriteLine();
Console.WriteLine($"{"body",-9}{"best real taper",17}{"with SF " + Beanstalk.SafetyFactor.ToString("F0") + "x",14}{"verdict survives?",19}");
Console.WriteLine(new string('-', 59));
foreach (Climb c in climbs)
{
    double bare = Beanstalk.Taper(c.DeltaPotential, Beanstalk.BestReal);
    double derated = Beanstalk.Taper(c.DeltaPotential * Beanstalk.SafetyFactor, Beanstalk.BestReal);
    string survives = derated <= Beanstalk.PracticalTaper ? "yes" : bare <= Beanstalk.PracticalTaper ? "NO — falls out" : "n/a";
    Console.WriteLine($"{c.Body.Name,-9}{Beanstalk.Format(bare),17}{Beanstalk.Format(derated),14}{survives,19}");
}

Console.WriteLine();
Climb mars = climbs.First(c => c.Body.Name == "Mars");
Console.WriteLine($"Mars is the one the table argues about: it wants {Beanstalk.SpecificStrengthNeeded(mars.DeltaPotential) / 1e6:G3} MJ/kg for a taper of " +
                  $"{Beanstalk.PracticalTaper:F0}, and the best real");
Console.WriteLine($"fibre is {Beanstalk.BestReal.SpecificStrength / 1e6:F2}. It is not waiting on a breakthrough — it is waiting on a factory. And there");
Console.WriteLine("is a second Martian problem no material fixes:");
TetherBody phobos = Beanstalk.Bodies.First(b => b.Name == "Phobos");
Climb marsClimb = mars;
Console.WriteLine($"  areostationary sits at {marsClimb.AnchorRadius / 1000:F0} km from Mars's centre; Phobos orbits at {phobos.PrimaryDistance / 1000:F0} km.");
Console.WriteLine("  A Mars cable crosses Phobos's orbit twice a Martian day, forever. The moon that makes the");
Console.WriteLine("  easiest beanstalk in the system is standing in the way of the hardest one.");
Console.WriteLine();
Console.WriteLine("=== The finding, in one breath ===");
Console.WriteLine("Earth is not a hard engineering problem, it is the wrong planet. Luna through L1 is a cable you");
Console.WriteLine("could order this afternoon. And at Phobos the elevator is not an elevator: it is a long rope with");
Console.WriteLine("a rock on the end, and the only reason nobody has built one is that nobody has been there.");
