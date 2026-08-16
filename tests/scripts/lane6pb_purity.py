# -*- coding: utf-8 -*-
"""
#870 lane 6'b - ONE SUBSTITUTION, AND NOTHING ELSE.

The claim this lane makes is that twenty-two fields and fifteen members moved from six partials of
`Map` onto one `Map.Patrol` object, and that the only thing that changed about the moved text is the
RECEIVER: `_guards` became `Guards`, `_escortDue` became `EscortDue`, and so on.

This script asks that question mechanically rather than by review. For every one of the fifteen it cuts
the member out of the base tree and out of `Pages/Patrol/Patrol.cs` on the branch, applies the twenty-two
renames to the base text, normalises the two things nesting a class changes about a line that are not its
content -- the indent, and the accessibility keyword -- and hashes the docblock and the body separately.
For every one of the twenty-two it does the same to the docblock alone, because a field becoming a
property legitimately changes the shape of its declaration and nothing else. `Guard` is asked the bluntest
question of all: its text is compared with NO substitution at all, because not one character of it moved.

Then the same question over the whole moved text at once: did every non-blank line survive, and did
anything unexplained appear.

    python tests/scripts/lane6pb_purity.py [--base <sha>] [--prove-it-can-fail]

`--prove-it-can-fail` plants a reworded docblock and a changed body on a scratch copy and watches the
measures go red. A proof that cannot fail is not a proof; this repo has paid for that lesson.
"""
import argparse
import hashlib
import io
import os
import re
import subprocess
import sys

PAGES = "src/SpaceSails.Client/Pages"
BASE_FILES = ["Map.Patrol.cs", "Map.Patrol.Hide.cs", "Map.Patrol.Challenge.cs",
              "Map.Patrol.Round.cs", "Map.Patrol.Escort.cs", "Map.Patrol.Run.cs"]

# raw name on the page  ->  what it is called on the round
NAMES = [
    ("_guards", "Guards"),
    ("_patrolReadables", "Readables"),
    ("_patrolBeat", "Beat"),
    ("_patrolFloorSeconds", "FloorSeconds"),
    ("_patrolHeardAgo", "HeardAgo"),
    ("_escort", "Escort"),
    ("_escortDue", "EscortDue"),
    ("_escortCar", "EscortCar"),
    ("_escortSeconds", "EscortSeconds"),
    ("_escortSaidPumps", "EscortSaidPumps"),
    ("_patrolWatch", "Watch"),
    ("_escortsThisWatch", "EscortsThisWatch"),
    ("_walkedAwayThisWatch", "WalkedAwayThisWatch"),
    ("_kickOutDue", "KickOutDue"),
    ("_kickOutRideDue", "KickOutRideDue"),
    ("_kickedOutPlateFor", "KickedOutPlateFor"),
    ("_paperInHand", "PaperInHand"),
    ("_walletFanOpen", "WalletFanOpen"),
    ("_shownBook", "ShownBook"),
    ("_walkedPastSaid", "WalkedPastSaid"),
    ("_patrolCheat", "RoundsCheat"),
    ("_badgeCheat", "BadgeCheat"),
]
RAW = re.compile(r"(?<![A-Za-z0-9_])(" + "|".join(sorted((n for n, _ in NAMES), key=len, reverse=True))
                 + r")(?![A-Za-z0-9_])")
SWAP = dict(NAMES)

# the fifteen questions that moved, and the file each was cut out of at the base
MOVED = [
    ("CaptainIsUnderEscort", "Map.Patrol.cs"),
    ("TheRoundOnFoot", "Map.Patrol.cs"),
    ("ForceTheRoundsTo", "Map.Patrol.cs"),
    ("TheQueryHasForcedARound", "Map.Patrol.cs"),
    ("ForceARoundIfNoneAsked", "Map.Patrol.cs"),
    ("MintTheSitePassAtTheLanding", "Map.Patrol.cs"),
    ("TheSitePassIsMintedAtTheLanding", "Map.Patrol.cs"),
    ("TheNextHideGetsItsOwnLine", "Map.Patrol.Hide.cs"),
    ("EverybodyForgetsTheCatch", "Map.Patrol.Hide.cs"),
    ("ThePaperInYourHandIs", "Map.Patrol.Challenge.cs"),
    ("TheBookOn", "Map.Patrol.Challenge.cs"),
    ("YourPaperTrail", "Map.Patrol.Challenge.cs"),
    ("ForgetThePaperTrail", "Map.Patrol.Challenge.cs"),
    ("RestoreAPaperTrailRow", "Map.Patrol.Challenge.cs"),
    ("CloseTheWalletFan", "Map.Patrol.Challenge.cs"),
]

ACCESS = re.compile(r"^(\s*)(private|public|internal|protected)(\s+)")


def sha(text):
    return hashlib.sha1(text.encode("utf-8")).hexdigest()[:16]


def git_show(base, path):
    out = subprocess.run(["git", "show", "%s:%s" % (base, path)],
                         stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if out.returncode != 0:
        sys.exit("cannot read %s at %s: %s" % (path, base, out.stderr.decode("utf-8", "replace")))
    return out.stdout.decode("utf-8").replace("\r\n", "\n").split("\n")


def read(path):
    with io.open(path, encoding="utf-8") as fh:
        return fh.read().replace("\r\n", "\n").split("\n")


def find_member(lines, name):
    """(doc, body) for `name` -- the contiguous /// block above it, and the declaration through its end."""
    decl = None
    for i, ln in enumerate(lines):
        s = ln.strip()
        if not ACCESS.match(ln):
            continue
        if re.search(r"[\s?>\]]" + re.escape(name) + r"\s*(\(|=>|;|$)", s):
            decl = i
            break
    if decl is None:
        return None

    doc = decl
    while doc > 0 and lines[doc - 1].strip().startswith("///"):
        doc -= 1

    indent = len(lines[decl]) - len(lines[decl].lstrip())
    end = decl
    if lines[decl].rstrip().endswith("{") or (
            decl + 1 < len(lines) and lines[decl + 1].strip() == "{"):
        while lines[end].strip() != "}" or (len(lines[end]) - len(lines[end].lstrip())) != indent:
            end += 1
    else:
        while not lines[end].rstrip().endswith(";"):
            end += 1
    return (lines[doc:decl], lines[decl:end + 1])


def find_field_doc(lines, name):
    """The /// block above the declaration of `name`."""
    for i, ln in enumerate(lines):
        if not ACCESS.match(ln):
            continue
        if re.search(r"[\s?>\]]" + re.escape(name) + r"\s*(\{|;|=)", ln):
            doc = i
            while doc > 0 and lines[doc - 1].strip().startswith("///"):
                doc -= 1
            return lines[doc:i]
    return None


def as_moved(block):
    """The base's own text, seen the way the round would have to write it: receiver swapped, one indent
    deeper, and the accessibility keyword normalised away (everything on Patrol is public)."""
    out = []
    for ln in block:
        ln = RAW.sub(lambda m: SWAP[m.group(1)], ln)
        ln = ln.replace('cref="_patrol.', 'cref="Patrol.')
        ln = ("    " + ln) if ln.strip() else ln
        out.append(ACCESS.sub(lambda m: m.group(1) + "public" + m.group(3), ln, count=1))
    return "\n".join(out)


def as_landed(block):
    out = []
    for ln in block:
        out.append(ACCESS.sub(lambda m: m.group(1) + "public" + m.group(3), ln, count=1))
    return "\n".join(out)


def run(base, branch_dir, quiet=False):
    say = (lambda *a: None) if quiet else (lambda *a: print(*a))
    reds = 0

    base_src = {f: git_show(base, "%s/%s" % (PAGES, f)) for f in BASE_FILES}
    landed = read(os.path.join(branch_dir, PAGES, "Patrol", "Patrol.cs"))
    guard_now = read(os.path.join(branch_dir, PAGES, "Patrol", "Guard.cs"))

    say("")
    say("  THE FIFTEEN QUESTIONS       PART   base (sha1)        Patrol (sha1)")
    say("  " + "-" * 84)
    for name, where in MOVED:
        was = find_member(base_src[where], name)
        now = find_member(landed, name)
        if was is None or now is None:
            say("  RED  %-27s %-6s %s" % (name, "-", "not found on the base" if was is None else "not on Patrol"))
            reds += 1
            continue
        for part, a, b in (("doc", was[0], now[0]), ("body", was[1], now[1])):
            ha, hb = sha(as_moved(a)), sha(as_landed(b))
            say("  %-4s %-27s %-6s %s   %s" % ("OK" if ha == hb else "RED", name, part, ha, hb))
            reds += (ha != hb)

    say("")
    say("  THE TWENTY-TWO FIELDS       PART   base (sha1)        Patrol (sha1)")
    say("  " + "-" * 84)
    for raw, on in NAMES:
        where = "Map.Patrol.Hide.cs" if raw == "_walkedPastSaid" else "Map.Patrol.cs"
        was = find_field_doc(base_src[where], raw)
        now = find_field_doc(landed, on)
        if raw == "_guards":
            say("  n/a  %-27s %-6s (it had NO docblock at the base -- one was written; see the PR body)"
                % ("_guards -> Guards", "doc"))
            continue
        if was is None or now is None:
            say("  RED  %-27s %-6s %s" % ("%s -> %s" % (raw, on), "doc", "declaration not found"))
            reds += 1
            continue
        ha, hb = sha(as_moved(was)), sha(as_landed(now))
        say("  %-4s %-27s %-6s %s   %s"
            % ("OK" if ha == hb else "RED", "%s -> %s" % (raw, on), "doc", ha, hb))
        reds += (ha != hb)

    # ── the Guard class: no substitution at all, because not one character of it moved ────────────────
    say("")
    was = find_class(base_src["Map.Patrol.cs"], "Guard")
    now = find_class(guard_now, "Guard")
    ha, hb = sha("\n".join(was)), sha("\n".join(now))
    say("  %-4s the Guard class, byte for byte, no substitution   %s   %s"
        % ("OK" if ha == hb else "RED", ha, hb))
    reds += (ha != hb)

    # ── and the blunt version, asked of the whole moved text at once ──────────────────────────────────
    moved_lines = []
    for name, where in MOVED:
        was = find_member(base_src[where], name)
        if was:
            moved_lines += was[0] + was[1]
    for raw, on in NAMES:
        where = "Map.Patrol.Hide.cs" if raw == "_walkedPastSaid" else "Map.Patrol.cs"
        doc = find_field_doc(base_src[where], raw)
        if doc and raw != "_guards":
            moved_lines += doc

    have = set(l.rstrip() for l in landed if l.strip())
    vanished = [l for l in moved_lines if l.strip() and as_moved([l]).rstrip() not in have]
    say("  %-4s every moved line survives                        base %-4d  vanished %d"
        % ("OK" if not vanished else "RED", len([l for l in moved_lines if l.strip()]), len(vanished)))
    for l in vanished[:8]:
        say("         %s" % l.strip()[:100])
    reds += (1 if vanished else 0)

    say("")
    say("ONE SUBSTITUTION, AND NOTHING ELSE: PROVEN" if reds == 0 else "%d measure(s) RED" % reds)
    return reds


def find_class(lines, name):
    for i, ln in enumerate(lines):
        if re.search(r"class\s+" + re.escape(name) + r"\b", ln) and not ln.strip().startswith("///"):
            doc = i
            while doc > 0 and lines[doc - 1].strip().startswith("///"):
                doc -= 1
            indent = len(ln) - len(ln.lstrip())
            end = i
            while lines[end].strip() != "}" or (len(lines[end]) - len(lines[end].lstrip())) != indent:
                end += 1
            return [l.strip() for l in lines[doc:end + 1] if l.strip()]
    return []


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="a5b32ec")
    ap.add_argument("--prove-it-can-fail", action="store_true")
    args = ap.parse_args()

    root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    os.chdir(root)

    if not args.prove_it_can_fail:
        sys.exit(1 if run(args.base, root) else 0)

    # Plant three things on a scratch copy and watch them redden.
    import shutil
    import tempfile
    scratch = tempfile.mkdtemp(prefix="lane6pb-")
    try:
        dst = os.path.join(scratch, PAGES, "Patrol")
        os.makedirs(dst)
        for f in ("Patrol.cs", "Guard.cs"):
            shutil.copy(os.path.join(root, PAGES, "Patrol", f), os.path.join(dst, f))

        p = os.path.join(dst, "Patrol.cs")
        t = io.open(p, encoding="utf-8").read()
        t = t.replace("Is the fan up? Only ever while somebody is walking over",
                      "Is the fan up? Only while somebody is walking over")           # a docblock reworded
        t = t.replace("public bool CaptainIsUnderEscort => Escort is not null;",
                      "public bool CaptainIsUnderEscort => Escort != null;")          # a body changed
        io.open(p, "w", encoding="utf-8", newline="\n").write(t)

        g = os.path.join(dst, "Guard.cs")
        t = io.open(g, encoding="utf-8").read()
        t = t.replace("public int WallSide = 1;", "public int WallSide = -1;")        # a moved field edited
        io.open(g, "w", encoding="utf-8", newline="\n").write(t)

        print("PLANTING: WalletFanOpen's docblock reworded, CaptainIsUnderEscort's body changed, and one "
              "of Guard's own fields given a different default.")
        reds = run(args.base, scratch)
        if reds == 0:
            sys.exit("THE SCRIPT COULD NOT FAIL. It proves nothing; fix it before trusting it.")
        print("\nthe script can fail: %d measure(s) went red on three plants." % reds)
    finally:
        shutil.rmtree(scratch, ignore_errors=True)


if __name__ == "__main__":
    main()
