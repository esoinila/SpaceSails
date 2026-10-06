namespace SpaceSails.Core;

/// <summary>
/// #1332 slice D · <b>THE FORWARDING DESK</b> — Cinder Roost's BERTH HOTEL · RESIDENTS ONLY, cabin 2. The labs'
/// logistics branch: the intranet's cold chain (<i>"Cold-chain capacity doubles again this quarter"</i>) needs a port
/// desk. The same kit as slice C's Preservation office (<see cref="SideOffice"/>): a plate, a door that stands ajar
/// one watch in five, one paper on the desk, nothing that acts on the captain, no walker.
///
/// <para>Every word is Fable canon (2026-09-30), verbatim — crews do not write canon. The office is a DESK and never
/// a name (#1074).</para>
/// </summary>
public static class ForwardingDesk
{
    /// <summary>The haven: Cinder Roost.</summary>
    public const string HavenId = "cinder-roost";

    /// <summary>Which of the hotel level's five cabins it is — one-based.</summary>
    public const int Cabin = 2;

    /// <summary>One watch in five the door stands ajar.</summary>
    public const int WatchesPerTurn = 5;

    /// <summary>The door's plate.</summary>
    public const string DoorPlate = "COLD-CHAIN FORWARDING · TRADE ONLY";

    /// <summary>[E] at the door while it is shut.</summary>
    public const string ShutLine = "Consignments by arrangement. Arrangements are not made here.";

    /// <summary>The field book, the first time the plate is read (pin, once).</summary>
    public const string PlateReadLine =
        "A forwarding desk for goods that must not get warm. The berth hotel is a strange place to keep it.";

    /// <summary>Entering while it stands ajar — told once per run.</summary>
    public const string AjarLine = "Cold. The room is colder than the corridor, and the corridor is a rock.";

    /// <summary>The paper on the desk.</summary>
    public const string SheetId = "forwarding-consignment";

    /// <summary>…its title.</summary>
    public const string SheetTitle = "A consignment note";

    /// <summary>…and its document. <i>Deep Storage</i> is the intranet's own phrase — the first time a port paper
    /// names it.</summary>
    public const string SheetDocument =
        "Reagent, forty kilos, cold. Origin withheld. Destination: Deep Storage. Charged to Plant.";

    /// <summary>The cost centre the paper prints, as a book subject: <i>Plant</i>, the intranet's office.</summary>
    public const string TheOfficeName = "Plant";

    /// <summary>What the book files the plate's line and the paper under: Plant + the haven.</summary>
    public static string SubjectsFor(string? havenName) =>
        CaseSubjects.Line(CaseSubjects.Office(TheOfficeName), CaseSubjects.Place(havenName ?? ""));

    /// <summary>The desk as the kit's data.</summary>
    internal static SideOffice Office => TheOffice;

    private static readonly SideOffice TheOffice = new()
    {
        Id = "forwarding",
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
        SeedTag = "forwarding:desk",
        PlateReadTag = "forwarding:plate-read",
        SheetTakenTag = "forwarding:sheet-taken",
        AjarToldTag = "forwarding:cold-room",
        AjarToldPersists = false,
        FilesTheSheet = true,
        HasAClerk = false,
        Subjects = SubjectsFor,
    };

    /// <summary>Every player-facing string the desk publishes — six.</summary>
    public static System.Collections.Generic.IEnumerable<string> AllProse() => Office.Prose();
}
