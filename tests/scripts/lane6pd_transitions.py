#!/usr/bin/env python3
"""#870 lane 6'd - EVERY TRANSITION OWNS EXACTLY THE ASSIGNMENTS IT REPLACED.

6'd moved every write to a `Guard` out of the six patrol partials and onto the man himself, as named
transitions.  Purity here cannot be proved by counting members - the bodies genuinely change shape, and a
transition takes a PARAMETER where the verb had an expression.  What can be proved, mechanically, is the
ACCOUNTING:

    for every field F and every operator op, the number of times a frame assigns `F op ...`
    is the same on the base tree and on this one.

The count on this tree is the sum, over every transition, of the assignments in its body times the number
of places that transition is CALLED.  So a transition that forgot one of the assignments it replaced comes
out one short for that field, named, with both counts - which is exactly the mistake this lane could make
and the fingerprints would then catch a frame later.

    python tests/scripts/lane6pd_transitions.py
    python tests/scripts/lane6pd_transitions.py --prove-it-can-fail

The second form takes one assignment out of one transition, in memory, and shows the script go red - a
guard that cannot fail is a guard that says nothing (SS house rule, five bug classes in).
"""
import re
import subprocess
import sys
from collections import Counter

BASE = "029641d"
DIR = "src/SpaceSails.Client/Pages/Patrol"
FILES = ["Guard.cs", "Patrol.cs", "Patrol.Challenge.cs", "Patrol.Escort.cs",
         "Patrol.Floor.cs", "Patrol.Hide.cs", "Patrol.Round.cs", "Patrol.Run.cs"]

MOTION = {"X", "Y", "Facing", "Vx", "Vy"}
INIT_ONLY = {"DeckName", "Plate"}
FIELDS = ("DeckName Plate X Y Facing Vx Vy Leg Standing SinceStop Route Planning Retries Seen Held "
          "WalkingUp WalkUpFor RePlanIn AfterYou Why AfterYouFor CallingIn WallSide SawYouShutIt "
          "Knocking Knocked SignedPoint CoverPoint CoverAt CoverFor").split()

# LONGEST FIRST, and it is not a tidy-up: regex alternation is ORDERED, so `AfterYou` would swallow the
# head of `AfterYouFor` and this script would report a field it had mis-read. The fifth bug class in one
# line - a guard handed the wrong world.
NAMES = "|".join(sorted(FIELDS, key=len, reverse=True))
OPS = r"\+=|-=|\?\?=|=(?!=)"

ASSIGN = re.compile(r"(?<![\w.])(?:g|man)\.(" + NAMES + r")\s*(" + OPS + r")")
BUMP = re.compile(r"\+\+(?:g|man)\.(" + "|".join(FIELDS) + r")\b")
SELF_ASSIGN = re.compile(r"^\s{12}(" + NAMES + r")\s*(" + OPS + r")")
SELF_BUMP = re.compile(r"\+\+(" + "|".join(FIELDS) + r")\b")
INITIALIZER = re.compile(r"^\s{20}(" + NAMES + r")\s*=")
LAMBDA = re.compile(r"public\s+\w[\w.?<>]*\s+(He\w+)\([^)]*\)\s*=>\s*(\+\+)?(" + NAMES
                    + r")\s*(" + OPS + r")?")
BLOCK = re.compile(r"^        public\s+\w[\w.?<>]*\s+(He\w+)\(")


def norm(op):
    """`x ??= e` writes x exactly when x is null, which is one assignment or none - and the shape it was
    rewritten into (`if (x is null) { x = e; }`) is the same statement with the same count."""
    return "=" if op == "??=" else op


def read_base(name):
    return subprocess.run(["git", "show", f"{BASE}:{DIR}/{name}"],
                          capture_output=True, text=True, encoding="utf-8", check=True).stdout


def read_now(name):
    return open(f"{DIR}/{name}", encoding="utf-8-sig").read()


def tally_verbs(text):
    """Every `g.Field op ...` a verb writes, plus the object initializer's own rows."""
    out = Counter()
    for line in text.split("\n"):
        for m in ASSIGN.finditer(line):
            out[(m.group(1), norm(m.group(2)))] += 1
        for m in BUMP.finditer(line):
            out[(m.group(1), "++")] += 1
        m = INITIALIZER.match(line)
        if m and m.group(1) not in INIT_ONLY:
            out[(m.group(1), "=")] += 1
    return out


def transitions(guard_src):
    """Every transition on the Guard, and the assignments its own body makes."""
    found = {}
    lines = guard_src.split("\n")
    for i, line in enumerate(lines):
        m = LAMBDA.search(line)
        if m:
            found[m.group(1)] = Counter({(m.group(3), norm(m.group(4) or "++")): 1})
            continue
        m = BLOCK.match(line)
        if not m:
            continue
        body = Counter()
        for row in lines[i + 1:]:
            if row.startswith("        }"):
                break
            hit = SELF_ASSIGN.match(row)
            if hit:
                body[(hit.group(1), norm(hit.group(2)))] += 1
        if body:
            found[m.group(1)] = body
    return found


def call_sites(name):
    n = 0
    for f in FILES:
        if f == "Guard.cs":
            continue
        n += len(re.findall(r"\.\b" + name + r"\(", read_now(f)))
    return n


def main():
    prove = "--prove-it-can-fail" in sys.argv

    was = Counter()
    for f in FILES:
        was += tally_verbs(read_base(f))

    moves = transitions(read_now("Guard.cs"))
    if prove:
        victim = "HeCallsItIn"
        (field, op), _ = sorted(moves[victim].items())[0]
        moves[victim][(field, op)] -= 1
        print(f"[--prove-it-can-fail] `{victim}` has been made to forget `{field} {op} ...`\n")

    now = Counter()
    stray = Counter()
    for name, body in moves.items():
        n = call_sites(name)
        if n == 0:
            print(f"  !! `{name}` is a transition nobody calls.")
        for key, count in body.items():
            now[key] += count * n
    for f in FILES:
        if f != "Guard.cs":
            stray += tally_verbs(read_now(f))

    bad = 0
    print("  field                op   base   branch")
    for key in sorted(set(was) | set(now) | set(stray)):
        field, op = key
        if field in MOTION:
            continue
        b, a = was[key], now[key] + stray[key]
        flag = "" if a == b else "   <-- THE ACCOUNTING DOES NOT ADD UP"
        bad += a != b
        print(f"  {field:<20} {op:<4} {b:>4}   {a:>6}{flag}")

    left = sum(v for k, v in stray.items() if k[0] not in MOTION)
    print(f"\n  non-motion assignments still spelled outside Guard.cs: {left}")
    print(f"  transitions: {len(moves)}   fields accounted for: {len(set(was)) }   mismatches: {bad}")
    if left:
        bad += 1
    if bad:
        print("\nRED.")
        return 1
    print("\nGREEN - every field is written exactly as many times a frame as it was, "
          "and nowhere but on the man.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
