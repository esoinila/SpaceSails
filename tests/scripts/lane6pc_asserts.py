#!/usr/bin/env python3
"""#870 lane 6'c - NOT ONE CLAIM MOVED.

Every `Assert.`-bearing line in every touched guard, base and branch, with every
string literal blanked. What is left is WHICH assertion, over WHICH variable, in
WHICH order - so a re-PATH or a re-spelled needle is invisible here and a changed
CLAIM is not.
"""
import hashlib
import io
import re
import subprocess
import sys

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

import os
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BASE = "ee8c6f1"
DIR = "tests/SpaceSails.Client.Tests/"

GUARDS = [
    "AJambIsNotASealedDoorTests.cs",
    "ALockedCubicleBuysTimeTests.cs",
    "AStandingGuardIsStandingAtSomethingTests.cs",
    "EveryRoundFingerprintsTheSameTests.cs",
    "PatrolState.cs",
    "RipItAndBinItIsAVerbTests.cs",
    "TheBootBuildsTheSameWorldTests.cs",
    "TheEscortIsAWalkTests.cs",
    "TheGuardsCatchYouTests.cs",
    "TheParkBenchIsAGumshoeMoveTests.cs",
    "ThePatrolKeepsItsOwnStateTests.cs",
    "ThePlanIsMadeWhileHeStandsTests.cs",
    "TheRoundIsNotStandingStillTests.cs",
    "TheRoundIsWalkableTests.cs",
    "TheSeatKeepsItsOwnStateTests.cs",
    "TheWalletFansWhileHeWalksOverTests.cs",
]


def read(path, ref=None):
    if ref:
        out = subprocess.run(["git", "-C", REPO, "show", "%s:%s" % (ref, path)],
                             capture_output=True)
        if out.returncode:
            return None
        return out.stdout.decode("utf-8")
    try:
        return open("%s/%s" % (REPO, path), encoding="utf-8").read()
    except OSError:
        return None


def claims(text):
    if text is None:
        return None
    out = []
    for line in text.replace("\r\n", "\n").split("\n"):
        if "Assert." not in line:
            continue
        s = line.strip()
        if s.startswith("//") or s.startswith("///"):
            continue
        s = re.sub(r'"(?:[^"\\]|\\.)*"', '""', s)          # blank every literal
        s = re.sub(r'@"(?:[^"]|"")*"', '""', s)
        s = re.sub(r"\$?\"\"", '""', s)
        out.append(s)
    return out


def sha(lines):
    return hashlib.sha256("\n".join(lines).encode("utf-8")).hexdigest()[:16]


print()
print("  guard                                         base branch   base sha         branch sha")
diffs = []
for g in GUARDS:
    b = claims(read(DIR + g, BASE))
    c = claims(read(DIR + g))
    if b is None:
        b = []
    if c is None:
        c = []
    ok = sha(b) == sha(c)
    print("  %-4s %-40s %4d  %4d   %s %s"
          % ("OK" if ok else "FAIL", g, len(b), len(c), sha(b), sha(c)))
    if not ok:
        diffs.append((g, b, c))

import difflib
for g, b, c in diffs:
    print("\n=== %s   (%d base / %d branch)" % (g, len(b), len(c)))
    for line in difflib.unified_diff(b, c, lineterm="", n=0):
        if line.startswith(("---", "+++", "@@")):
            continue
        print("  " + line[:150])
