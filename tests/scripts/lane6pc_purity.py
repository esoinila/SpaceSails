#!/usr/bin/env python3
"""#870 lane 6'c - THE MOVE IS PURE MODULO THE RECEIVER, proven mechanically.

Every non-blank line of the patrol family AT THE BASE has to still exist on the
branch, either exactly as it was (it stayed on the page) or with this lane's ONE
substitution set applied to it (it moved onto the round). Two declared
normalisations - indent depth, and the accessibility keyword - and nothing else.

    python purity.py [--prove-it-can-fail]
"""
import hashlib
import io
import subprocess
import sys
from collections import Counter

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

import os
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BASE = "ee8c6f1"

BASE_FAMILY = [
    "src/SpaceSails.Client/Pages/Map.Patrol.cs",
    "src/SpaceSails.Client/Pages/Map.Patrol.Challenge.cs",
    "src/SpaceSails.Client/Pages/Map.Patrol.Escort.cs",
    "src/SpaceSails.Client/Pages/Map.Patrol.Hide.cs",
    "src/SpaceSails.Client/Pages/Map.Patrol.Round.cs",
    "src/SpaceSails.Client/Pages/Map.Patrol.Run.cs",
    "src/SpaceSails.Client/Pages/Patrol/Patrol.cs",
    "src/SpaceSails.Client/Pages/Patrol/Guard.cs",
]

BRANCH_FAMILY = [
    "src/SpaceSails.Client/Pages/Map.Patrol.cs",
    "src/SpaceSails.Client/Pages/Map.Patrol.Challenge.cs",
    "src/SpaceSails.Client/Pages/Map.Patrol.Hide.cs",
    "src/SpaceSails.Client/Pages/Map.Patrol.Run.cs",
    "src/SpaceSails.Client/Pages/Map.PatrolHost.cs",
    "src/SpaceSails.Client/Pages/Map.Collaborators.cs",
    "src/SpaceSails.Client/Pages/Patrol/Patrol.cs",
    "src/SpaceSails.Client/Pages/Patrol/Guard.cs",
    "src/SpaceSails.Client/Pages/Patrol/IPatrolHost.cs",
    "src/SpaceSails.Client/Pages/Patrol/Patrol.Floor.cs",
    "src/SpaceSails.Client/Pages/Patrol/Patrol.Hide.cs",
    "src/SpaceSails.Client/Pages/Patrol/Patrol.Round.cs",
    "src/SpaceSails.Client/Pages/Patrol/Patrol.Challenge.cs",
    "src/SpaceSails.Client/Pages/Patrol/Patrol.Escort.cs",
    "src/SpaceSails.Client/Pages/Patrol/Patrol.Run.cs",
]

SUBS = [
    ("_patrol.", ""),
    ("_avatarHeading", "_host.AvatarHeading"),
    ("_avatarX", "_host.AvatarX"),
    ("_avatarY", "_host.AvatarY"),
    ("_surface", "_host.Surface"),
    ("_deckPlan", "_host.DeckPlan"),
    ("_viewObject", "_host.ViewObject"),
    ("_satchel", "_host.Satchel"),
    ("ShowPulseMessage(", "_host.ShowPulseMessage("),
    ("LogAutopilotEvent(", "_host.LogAutopilotEvent("),
    ("FileNote(", "_host.FileNote("),
    ("ApplyNerveShock(", "_host.ApplyNerveShock("),
    ("RequestVaultSave()", "_host.RequestVaultSave()"),
    ("StandCaptainAt(", "_host.StandCaptainAt("),
    ("CancelAutoWalk(", "_host.CancelAutoWalk("),
    ("RefreshAshore()", "_host.RefreshAshore()"),
    ("RideTheLiftTo(", "_host.RideTheLiftTo("),
    ("RebuildSurfaceDeck()", "_host.RebuildSurfaceDeck()"),
    ("SightBlockers()", "_host.SightBlockers()"),
    ("SeatedOnABenchInTheOpen", "_host.SeatedOnABenchInTheOpen"),
    ("TheCubicleTheCaptainIsShutIn(", "_host.TheCubicleTheCaptainIsShutIn("),
    ("NameOnYourOwnPapers", "_host.NameOnYourOwnPapers"),
]

ACC = ("private ", "public ", "internal ")


# The lines this lane DELIBERATELY rewrote, and why. Anything vanished that is not
# matched by one of these is a real loss and the proof fails.
REWRITTEN = [
    ("// Subject: part of Map.Patrol (#870 split;", 5,
     "the five page partials subject notes: two of those files are gone; the rest are the PAGE half"),
    ("// page. They are here because all fifteen", 1, "6'b said 6'c would delete the forwarder block"),
    ("// is not the lane that rewrites those callers", 1, "...and it does not; the banner now says why"),
    ('/// moves (<see cref="TheRadioCall"/>)', 1, "the lane's own receiver rewrite, applied to a cref"),
    ('/// (<see cref="RunAfterTheCaptain"/>', 1, "same"),
    ('/// (<see cref="HeHasYou"/>)', 1, "same"),
    ('/// already know, which past the threshold', 1, "same (TheKickOut)"),
    ('/// <see cref="WalkUpToTheCaptain"/>)', 1, "same"),
    ('/// walk back is now a walk (<see cref="WalkTheEscort"/>)', 1, "same"),
    ('/// <see cref="SpendTheStride"/>', 1, "same"),
    ("/// is no <c>HitsTaken</c> in this file", 1, "'in this file' -> 'in this family': the run is a file along now"),
    ("/// <para>Twenty-two loose fields became one. Everything below", 1,
     "the _patrol docblock's 6'b paragraph said the verbs stayed on the page; they did not"),
    ("/// walking them, hailing, reading a wallet, walking a captain out", 1, "same paragraph"),
    ("/// world: the avatar, the deck plan, the excursion", 1, "same paragraph"),
    ("/// here and write <c>_patrol.Guards</c> where they used to", 1, "same paragraph"),
    ("<acc> readonly Patrol _patrol = new();", 1,
     "the round is handed the page now, and an instance field initialiser may not name `this`"),
    ("<acc> sealed class Patrol", 1, "the round is seven files now: `private sealed partial class Patrol`"),
]


def norm(line):
    line = line.strip()
    for a in ACC:
        if line.startswith(a):
            return "<acc> " + line[len(a):]
    return line


def receiver(line):
    for k, v in SUBS:
        line = line.replace(k, v)
    return norm(line.replace("_host._host.", "_host."))


def read(path, ref=None):
    if ref:
        out = subprocess.run(["git", "-C", REPO, "show", "%s:%s" % (ref, path)],
                             capture_output=True)
        if out.returncode:
            return []
        text = out.stdout.decode("utf-8")
    else:
        text = open("%s/%s" % (REPO, path), encoding="utf-8").read()
    return [l for l in text.replace("\r\n", "\n").split("\n") if l.strip()]


SCAFFOLD = ("using ", "namespace ", "<acc> sealed partial class Map", "<acc> partial class Map",
            "<acc> sealed partial class Patrol", "<acc> interface IPatrolHost", "// Subject:")


def is_scaffold(l):
    return l in ("{", "}") or any(l.startswith(p) for p in SCAFFOLD)


def is_wiring(l):
    return ("_patrol." in l or "IPatrolHost." in l or "_host = host" in l
            or "new Patrol(this)" in l or "new Seating(this)" in l
            or l.startswith("<acc> readonly IPatrolHost _host")
            or l.startswith("<acc> readonly Patrol _patrol")
            or l.startswith("<acc> Patrol(IPatrolHost host)")
            or l.startswith("<acc> Map()"))


def is_prose(raw):
    return raw.strip().startswith("//")


def main():
    plant = "--prove-it-can-fail" in sys.argv

    base = Counter()
    for f in BASE_FAMILY:
        for l in read(f, BASE):
            base[norm(l)] += 1

    branch_raw = []
    door = set()
    for f in BRANCH_FAMILY:
        part = read(f)
        if f.endswith("Map.PatrolHost.cs") or f.endswith("IPatrolHost.cs"):
            door |= {norm(l) for l in part}
        # ...and a forwarder is two lines when it wraps: the signature and the hop.
        for i, l in enumerate(part):
            if l.rstrip().endswith("=>") and i + 1 < len(part) and "_patrol." in part[i + 1]:
                door.add(norm(l))
        branch_raw += part
    if plant:
        branch_raw = [
            l.replace("he walks over, knocks once, and waits", "he knocks and waits")
             .replace("g.Standing = PatrolBeat.StandSeconds;   // THE GAP",
                      "g.Standing = PatrolBeat.StandSeconds + 1;   // THE GAP")
             .replace("PaperInHand is { } chosen", "PaperInHand is not null")
            for l in branch_raw
        ]

    pool = Counter(norm(l) for l in branch_raw)

    stayed = moved = 0
    vanished = Counter()
    consumed = Counter()
    for line, n in base.items():
        take = min(n, pool[line] - consumed[line])
        consumed[line] += take
        stayed += take
        rest = n - take
        if rest:
            m = receiver(line)
            take2 = min(rest, pool[m] - consumed[m]) if m != line else 0
            consumed[m] += take2
            moved += take2
            if rest - take2:
                vanished[line] = rest - take2

    left = Counter()
    for l in branch_raw:
        left[norm(l)] += 1
    for k, n in consumed.items():
        left[k] -= n

    appeared = []
    seen = Counter()
    for l in branch_raw:
        k = norm(l)
        if left[k] - seen[k] > 0:
            seen[k] += 1
            appeared.append(l)

    scaffold = [l for l in appeared if is_scaffold(norm(l))]
    rest_a = [l for l in appeared if l not in scaffold]
    wiring = [l for l in rest_a if is_wiring(norm(l)) or norm(l) in door]
    rest_b = [l for l in rest_a if l not in wiring]
    prose = [l for l in rest_b if is_prose(l)]
    other = [l for l in rest_b if not is_prose(l)]

    print()
    print("  base family lines (non-blank, normalised)   %6d" % sum(base.values()))
    print("  branch family lines                         %6d" % len(branch_raw))
    print("      of the base lines: STAYED on the page   %6d" % stayed)
    print("                         MOVED onto the round %6d" % moved)
    print()
    undeclared = sum(n for l, n in vanished.items()
                     if not any(l.startswith(r[0]) for r in REWRITTEN))
    ok1 = undeclared == 0
    print("  %s EVERY LINE SURVIVES THE MOVE          vanished %5d"
          % ("OK  " if ok1 else "FAIL", undeclared))
    ok2 = len(other) == 0
    print("  %s new lines are scaffolding or one-hop  other    %5d"
          % ("OK  " if ok2 else "FAIL", len(other)))
    print("           file headers / class / braces               %5d" % len(scaffold))
    print("           forwarder + host wiring + the constructor   %5d" % len(wiring))
    print("           docblocks and banners of the new files      %5d" % len(prose))
    print()
    h = hashlib.sha256("\n".join(sorted(base.elements())).encode("utf-8")).hexdigest()[:16]
    print("  base line multiset  sha256 %s" % h)

    declared, real = [], Counter()
    for l, n in vanished.items():
        hit = next((r for r in REWRITTEN if l.startswith(r[0])), None)
        if hit:
            declared.append((l, n, hit[2]))
        else:
            real[l] = n

    print("\n  %d lines this lane DELIBERATELY rewrote, and why:"
          % sum(n for _, n, _ in declared))
    shown = set()
    for l, n, why in sorted(declared, key=lambda r: r[2]):
        if why in shown:
            continue
        shown.add(why)
        print("    OK %s%s" % ("x%d " % n if n > 1 else "", l[:100]))
        print("       %s" % why)

    if real:
        print("\n  VANISHED:")
        for l, n in sorted(real.items()):
            print("    x%d  %s" % (n, l[:112]))
    if other:
        print("\n  APPEARED, not scaffolding and not wiring:")
        for l in sorted(set(other)):
            print("    x%d  %s" % (other.count(l), l.strip()[:112]))

    print()
    print("PURE MOVE: " + ("PROVEN" if (ok1 and ok2) else "NOT PROVEN"))
    return 0 if (ok1 and ok2) else 1


sys.exit(main())
