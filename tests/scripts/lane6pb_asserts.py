# -*- coding: utf-8 -*-
"""
#870 lane 6'b - NOT ONE CLAIM MOVED.

Thirteen guards were touched by this lane. Every one of them was touched in one of two ways: its PATH was
re-pointed at the eight files the family now is, or a needle was RE-SPELLED from `_guards` onto
`_patrol.Guards`. Neither is allowed to change what any of them ASSERTS.

So: take every `Assert.`-bearing line of every touched guard, blank the string literals (the needles ARE
literals, and re-spelling one is the whole point), and hash what is left. A file whose hash is unchanged
did not move a claim. A file whose hash moved is printed line by line, base against branch, so the
difference is read rather than trusted.

    python tests/scripts/lane6pb_asserts.py [--base <sha>]
"""
import argparse
import collections
import hashlib
import io
import os
import re
import subprocess
import sys

TESTS = "tests/SpaceSails.Client.Tests"

TOUCHED = [
    ("ALockedCubicleBuysTimeTests.cs", "re-PATHED + 3 needles re-spelled"),
    ("AStandingGuardIsStandingAtSomethingTests.cs", "re-PATHED + 1 needle"),
    ("EveryFrameLeavesTheSameFingerprintTests.cs", "Read/Get/Set follow onto _patrol"),
    ("EveryRoundFingerprintsTheSameTests.cs", "Get/Set follow; the scalar census reads the round too"),
    ("TheEscortIsAWalkTests.cs", "re-PATHED + 1 needle"),
    ("TheGuardsCatchYouTests.cs", "re-PATHED + 9 needles"),
    ("TheParkBenchIsAGumshoeMoveTests.cs", "re-PATHED + 1 needle"),
    ("ThePlanIsMadeWhileHeStandsTests.cs", "re-PATHED only"),
    ("TheRoundIsNotStandingStillTests.cs", "re-PATHED only"),
    ("TheRoundIsWalkableTests.cs", "re-PATHED only"),
    ("TheWalletFansWhileHeWalksOverTests.cs", "re-PATHED + 3 needles"),
    ("TwoGripsOneWalkTests.cs", "Get/Set follow onto _patrol"),
    ("ThePatrolKeepsItsOwnStateTests.cs", "THE GUARD ITSELF - ratcheted, 3 facts to 5"),
]

LITERAL = re.compile(r'"(?:[^"\\]|\\.)*"')


def blank(line):
    return LITERAL.sub('""', line).strip()


def claims(lines):
    return [blank(l) for l in lines if "Assert." in l and not l.strip().startswith("///")]


def git_show(base, path):
    out = subprocess.run(["git", "show", "%s:%s" % (base, path)],
                         stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if out.returncode != 0:
        return None
    return out.stdout.decode("utf-8").replace("\r\n", "\n").split("\n")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="a5b32ec")
    args = ap.parse_args()
    os.chdir(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))

    print("")
    print("  GUARD                                        CLAIMS  base (sha1)        branch (sha1)")
    print("  " + "-" * 92)
    moved = []
    for f, why in TOUCHED:
        was = git_show(args.base, "%s/%s" % (TESTS, f))
        now = io.open(os.path.join(TESTS, f), encoding="utf-8").read().replace("\r\n", "\n").split("\n")
        a, b = claims(was) if was else [], claims(now)
        ha = hashlib.sha1("\n".join(a).encode("utf-8")).hexdigest()[:16]
        hb = hashlib.sha1("\n".join(b).encode("utf-8")).hexdigest()[:16]
        same = ha == hb
        print("  %-4s %-40s %4d    %s   %s" % ("OK" if same else "DIFF", f[:-3], len(b), ha, hb))
        if not same:
            moved.append((f, why, a, b))

    for f, why, a, b in moved:
        print("")
        print("  %s -- %s" % (f, why))
        ca, cb = collections.Counter(a), collections.Counter(b)
        only_a = sorted((ca - cb).elements())
        only_b = sorted((cb - ca).elements())
        for l in only_a:
            print("    - %s" % l[:118])
        for l in only_b:
            print("    + %s" % l[:118])

    print("")
    print("  %d of %d guards are claim-identical; %d printed above."
          % (len(TOUCHED) - len(moved), len(TOUCHED), len(moved)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
