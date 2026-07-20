namespace SpaceSails.Core;

/// <summary>
/// #409 · DR. MIELOS VANTAR'S LOGS — the lore fragments a secret lab's consoles read out (owner: "his logs
/// — the mystery hook, KAAMOS-style: never fully explained"). An ORIGINAL reclusive cyberneticist/geneticist
/// (homage, never "Dr Soong" — trademark): a disgraced genius who vanished into the deep field and kept
/// working where no charter would look. Mad-science energy — half-finished synthetics, a brain-in-a-jar
/// backup rig (a wink at the game's OWN brain-backup fiction), things labelled DO NOT REVIVE — but the man
/// himself stays a shadow. The fragments imply; they never lecture.
///
/// <para>Pure Core copy, no dependencies — the client's lab consoles read these by index. This is our OWN
/// pool: it stays Vantar's own logs. KAAMOS (#411) owns a separate <c>KaamosLore</c> pool + docs; if a
/// fragment ever needs to name the ice-moon project, cross-link THERE rather than duplicating it here — see
/// the <see cref="KaamosHook"/> note.</para>
/// </summary>
public static class VantarLore
{
    /// <summary>The CORE-log fragment index (<see cref="Fragments"/>[^1]) — the deepest, worst truth, and the
    /// one the reveal roll fires on. <see cref="SecretLab.LabConsole.IsCoreLog"/> points its console here.</summary>
    public static int CoreIndex => Fragments.Length - 1;

    /// <summary>Dr. Vantar's log fragments, threshold-shallow to core-deep. Each is one console's worth; the
    /// last (<see cref="CoreIndex"/>) is the core log — the reveal. Verbatim, in his own dry, unravelling voice.</summary>
    public static readonly string[] Fragments =
    [
        // 0 — the threshold log: the disgrace, obliquely. Draws you in.
        "LOG — M. Vantar, personal. They struck my name from the register today. \"Ethics.\" As if the ethics " +
        "board ever built anything that outlived its funding. Let them keep their register. I have kept the work. " +
        "Out here the only committee is the dark, and the dark has never once told me no.",

        // 1 — the method, half-mad, half-brilliant.
        "LOG 44. The lattice takes a pattern now on the first pour — no annealing, no second bake. A mind is only " +
        "a standing wave; you do not need the meat if you have the shape of the wave. I have the shape. I have " +
        "several. Some of them ask, in the night, to be let out. I do not answer. Answering is a habit and habits " +
        "are how they learn your voice.",

        // 2 — the backup rig, the wink at the game's own brain-backup fiction.
        "LOG 61. The backup rig holds steady at last — a whole cortex, kept wet and dreaming in the jar, its wave " +
        "read out clean every ninety seconds. Whose cortex is not a question the register would like, so I have " +
        "not written it down. I have written instead, on the glass, in a hand I hope is firm enough: DO NOT REVIVE. " +
        "Not won't. DO NOT. There is a difference and by the time you read this you may know it.",

        // 3 — the synthetics, the bounded risk seeded.
        "LOG 78. Unit 9 walked three steps and turned to look at the camera. I did not build it to turn and look. " +
        "I have put the others down to sleep — a deep sleep, the current trickled to almost nothing — and I sleep " +
        "in the far room now, behind the good door. If you are hearing this you have opened the good door. Then you " +
        "have already made my mistake, and it is too late for me to un-teach it to you.",

        // 4 — the KAAMOS shadow (never named plainly; the cross-lane hook lives in the fiction, not the code).
        "LOG (undated). A ship came. It did not answer the berth code and it did not need to — I know the polar " +
        "silence of that hull, I have known it a long time. They did not come for me. They came for what I made, " +
        "and they left me the bill: a name off a register, a moon off the charts, a project that runs on in the " +
        "cold with the lights off and the manifest sealed. I signed nothing. That has never once stopped them.",

        // 5 — THE CORE LOG (CoreIndex): the reveal. What shouldn't exist, stated flat.
        "CORE LOG — last entry. It is not that I copied a mind. Anyone with the lattice can copy a mind. It is that " +
        "the copy and the original both wake, and both are certain — CERTAIN — that they are the one who was here " +
        "first. I have been on both sides of that glass now. I no longer know which of us is writing this. If you " +
        "are reading it, then one of us kept working, and I am so sorry, because that means it can be done, and now " +
        "you know it can be done, and knowing is the whole of the disease.",
    ];

    /// <summary>A safe fragment fetch — clamps the index into range so a stale/forced index never throws
    /// (the client passes <see cref="SecretLab.LabConsole.LoreIndex"/> straight through).</summary>
    public static string Fragment(int index) =>
        Fragments[System.Math.Clamp(index, 0, Fragments.Length - 1)];

    /// <summary>The core-log fragment — the reveal text.</summary>
    public static string CoreLog => Fragments[CoreIndex];

    /// <summary>KAAMOS cross-link hook (#411): fragment 4 already gestures at the sealed ice-moon project
    /// without naming it. When the KAAMOS lane wants a Vantar log to count as a KAAMOS intel FRAGMENT, it can
    /// register THIS pool's index 4 against its own quest-state — from that lane, reading its <c>KaamosLore</c>,
    /// not by editing this file. Left as a named seam so the two mysteries can meet later without a collision.</summary>
    public const int KaamosHook = 4;
}
