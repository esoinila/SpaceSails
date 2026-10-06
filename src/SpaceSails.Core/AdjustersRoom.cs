namespace SpaceSails.Core;

/// <summary>
/// #1332 slice E · <b>THE ADJUSTER'S ROOM</b> — The Deep's COLD ROOMS · BOOK AT THE DESK, cabin 4. Nebula Mutual's
/// field office, where #1151's claims are meant to be made (owner ruling 09-06: every loss but death is a CLAIM). The
/// same kit as slice C's Preservation office (<see cref="SideOffice"/>): a plate, a door that stands ajar one watch
/// in four, one paper on the desk, nothing that acts on the captain, no walker.
///
/// <para>Every word is Fable canon (2026-09-30), verbatim. The blank form is a FORM, not evidence: taking it files
/// nothing in the book — it is #1151's future seed, and holding it is what lets a claim be opened later (nothing
/// reads it yet).</para>
/// </summary>
public static class AdjustersRoom
{
    /// <summary>The haven: The Deep.</summary>
    public const string HavenId = "the-deep";

    /// <summary>Which of the hotel level's five cabins it is — one-based.</summary>
    public const int Cabin = 4;

    /// <summary>One watch in four the door stands ajar.</summary>
    public const int WatchesPerTurn = 4;

    /// <summary>The door's plate.</summary>
    public const string DoorPlate = "NEBULA MUTUAL · CLAIMS · KNOCK";

    /// <summary>[E] at the door while it is shut.</summary>
    public const string ShutLine = "The adjuster keeps hours. The hours are kept elsewhere.";

    /// <summary>The field book, the first time the plate is read (pin, once).</summary>
    public const string PlateReadLine =
        "A claims room with the light off. The policy says they pay for death; the door says they take appointments.";

    /// <summary>Entering while it stands ajar — told once per run.</summary>
    public const string AjarLine =
        "A desk, two chairs, one of them for you. The form on the desk has your kind of loss on it and no line for your name.";

    /// <summary>The paper on the desk.</summary>
    public const string SheetId = "claim-form-blank";

    /// <summary>…its title.</summary>
    public const string SheetTitle = "A claim form, blank";

    /// <summary>…and its document.</summary>
    public const string SheetDocument =
        "Loss (other than death): describe. Witness: name. Captain present: yes. Nebula Mutual pays what the form says, when the form is complete.";

    /// <summary>What the book files the plate's line under: Nebula Mutual + the haven. (The form itself is filed
    /// under nothing — it is not clippable as evidence.)</summary>
    public static string SubjectsFor(string? havenName) =>
        CaseSubjects.Line(CaseSubjects.Office(NebulaLore.TermsRefiledOffice), CaseSubjects.Place(havenName ?? ""));

    /// <summary>The room as the kit's data.</summary>
    internal static SideOffice Office => TheOffice;

    private static readonly SideOffice TheOffice = new()
    {
        Id = "adjuster",
        HavenId = HavenId,
        Cabin = Cabin,
        DoorPlate = DoorPlate,
        ShutLine = ShutLine,
        PlateReadLine = PlateReadLine,
        AjarLine = AjarLine,
        SheetId = SheetId,
        SheetTitle = SheetTitle,
        SheetDocument = SheetDocument,
        WatchesPerTurn = WatchesPerTurn,
        SeedTag = "adjuster:room",
        PlateReadTag = "adjuster:plate-read",
        SheetTakenTag = "adjuster:sheet-taken",
        AjarToldTag = "adjuster:two-chairs",
        FilesTheSheet = false,
        HasAClerk = false,
        Subjects = SubjectsFor,
    };

    /// <summary>Every player-facing string the room publishes — six.</summary>
    public static System.Collections.Generic.IEnumerable<string> AllProse() => Office.Prose();
}
