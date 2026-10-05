namespace SpaceSails.Core;

/// <summary>
/// #653 slice 2 · <b>THE TOLD-ONCE MEMORY.</b> One mechanism for "this line has been said": a keyed set where a key is
/// the line's name (and, when the line is owed per place, where — <see cref="Key"/>). Before it the game kept three
/// shapes of the same fact — a hash set keyed <c>"{line}:{berth}"</c> on the page, a <c>Told</c> set on the station's
/// visit, and three bare bools on the man at the door — each reset (or not) in its own way, by hand or by being built
/// afresh.
///
/// <para><b>THE CADENCE IS THE HOLDER'S LIFETIME, NEVER A RESET.</b> This class has no <c>Clear</c> and no scope
/// argument on purpose. A line owed once per RUN lives in the set the page holds for the run; a line owed once per
/// VISIT lives in the set the excursion holds, which is a new empty set the moment a new excursion is built — so
/// "told again on the next boarding" is true by construction, not by a reset someone has to remember to call in
/// <c>BeginSurfaceExcursion</c>. Nothing here is persisted (no vault, no field book): the book is a latch of its own
/// for the two lines that ride it, and stays so.</para>
///
/// <para>A key is added on the frame its line reaches the slot and never before (<see cref="Tell"/> after the caller's
/// own free-slot / in-reach checks), so a line whose slot was busy the whole time is still owed.</para>
/// </summary>
public sealed class ToldOnce
{
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

    /// <summary>The key a once-per-place line is remembered under: which line, and where.</summary>
    public static string Key(string line, string where) => $"{line}:{where}";

    /// <summary>The haven lift's first ride down (once per station, per run).</summary>
    public const string FirstRide = "ride";

    /// <summary>The haven garden's first visit (once per station, per run).</summary>
    public const string GardenVisit = "garden";

    /// <summary>The man at the door's approach line, on the first card of an excursion (once per visit).</summary>
    public const string GateApproach = "gate:approach";

    /// <summary>The man at the door's empty chair (once per visit).</summary>
    public const string GateAbsent = "gate:absent";

    /// <summary>The man at the door's walk away on his round (once per visit).</summary>
    public const string GateRound = "gate:round";

    /// <summary>The dead station's serviceable lock (once per visit).</summary>
    public const string StationLock = "lock";

    /// <summary>The dead station's cut face (once per visit).</summary>
    public const string StationCut = "cut";

    /// <summary>Has this line been told?</summary>
    public bool Has(string key) => _keys.Contains(key);

    /// <summary>Mark this line told. True the first time (the caller says it now); false if it already was.</summary>
    public bool Tell(string key) => _keys.Add(key);

    /// <summary>How many lines have been told.</summary>
    public int Count => _keys.Count;
}
