namespace SpaceSails.Core;

// #251 · Split from NewsWire.cs, moved verbatim: the event headlines — which masthead prints a pushed
// event, its sentence, and the subjects it names. Every static field and const stays in NewsWire.cs.
public static partial class NewsWire
{
    // ---- Event headlines: pirate-flavored narration of a small set of gameplay hooks ----

    /// <summary>#1202 · Does this pushed event print under this masthead? Every kind prints wherever the system
    /// wire does, except the floor's reaction to a stringer's story, which is a port's own gossip and prints
    /// on a <see cref="NewsScope.PortRag"/> only.</summary>
    public static bool PrintsIn(NewsEventKind kind, NewsScope scope) =>
        scope != NewsScope.CompanyIntranet
        && (kind != NewsEventKind.PressFloorReaction || scope == NewsScope.PortRag);

    /// <summary>Narrates a pushed <see cref="NewsEvent"/> — pure formatting, no randomness, so
    /// the same event always reads the same.</summary>
    public static string Headline(NewsEvent evt) => evt.Kind switch
    {
        NewsEventKind.RobberyCommitted =>
            $"Piracy alert: {evt.Subject} was boarded and cleaned out. The underwriters are already drafting angry letters.",
        NewsEventKind.HunterDispatched =>
            $"{evt.Subject} is fitting out at {evt.Detail ?? "a policed port"} — the hunt is on.",
        NewsEventKind.IntelPurchased =>
            $"Word on the wire: somebody just bought a fix on {evt.Subject}. Watch your six.",
        NewsEventKind.OrbitEnteredHaven =>
            $"A ship slipped quietly into orbit at {evt.Subject} — the regulars ask no names.",
        NewsEventKind.SlugHit =>
            $"Someone put a slug through {evt.Subject}'s sail — she's dead in the water and drifting{(evt.Detail is null ? "" : $" near {evt.Detail}")}.",
        NewsEventKind.SlugMissed =>
            $"A mass-driver round evaporated somewhere past {evt.Subject}'s wake. Warning, or bad gunnery — opinions differ.",
        NewsEventKind.HunterBrokeOff =>
            $"{evt.Subject} has broken off the chase — the contract, it seems, wasn't worth the hull. Someone's underwriters are furious.",
        NewsEventKind.LongHaulComplete =>
            $"A ship crossed the deep black to {evt.Subject} — weeks of void{(evt.Detail is null ? "" : $" ({evt.Detail})")}, one long silence, then a berth light on the scope.",
        NewsEventKind.AsteroidInbound =>
            $"⚠ COLLISION ALERT — an inbound rock is on a line for {evt.Subject}{(evt.Detail is null ? "" : $" ({evt.Detail})")}. Every hull with a drill and a death wish, the Exchange is paying.",
        NewsEventKind.AsteroidDeflected =>
            $"{evt.Subject} still stands: a crew rode the rock down and shoved it off the line before it arrived. The rings kept the orbit. Drinks are on the Exchange.",
        NewsEventKind.AsteroidStruck =>
            $"The rock reached {evt.Subject}. Heavy damage across the trade decks and the berths are a mess, but she held — the Exchange is already clearing wreckage and reopening dock by dock.",
        // #411/#663 — the subject IS the headline. An arc beat arrives already written in the voice of
        // whoever filed it, because the alternative is the wire explaining a plot to the player.
        NewsEventKind.ArcBeatBreaks => evt.Subject,
        // #1202 — her story and the floor's reaction are pass-throughs for the same reason: they are filed
        // in somebody's voice (CarryThePress, verbatim canon), and the wire does not rewrite a byline.
        NewsEventKind.PressStoryFiled or NewsEventKind.PressFloorReaction => evt.Subject,
        // #525 — the wire's own clerical headline for a hull lost inside a harbour, authored verbatim in
        // the canon pass of 2026-09-06. The two braces are the RECORD'S: {N} is Subject, the berth number
        // off the plate, and {PORT} is Detail. No cause is named beyond the declaration itself and nobody
        // is named at all — "the operator has a name for the master" is a harbour saying it has paperwork,
        // not a harbour saying who told it. The fallback for an absent port is HunterDispatched's own
        // existing words rather than a new sentence: this lane authors exactly one string.
        NewsEventKind.HullLostAtABerth =>
            $"Berth {evt.Subject}, {evt.Detail ?? "a policed port"}: hull lost to a declared reactor overload. The operator has a name for the master.",
        _ => "Static on the wire.",
    };

    /// <summary>
    /// #1052 (L2) · <b>WHAT A CLIPPED STORY IS ABOUT.</b> The subjects a pushed event's line carries into
    /// the field book when the captain presses ✂ CLIP — <see cref="CaseSubjects"/>'s own composed line, so
    /// a clipped headline stacks under the same headings the dossier's own entries do.
    ///
    /// <para><b>Two kinds today, and the design names them: the two that already touch the case.</b>
    /// <see cref="NewsEventKind.IntelPurchased"/> is somebody buying a fix on a hull — the hull is printed
    /// in the sentence, so it is what the note is about. <see cref="NewsEventKind.ArcBeatBreaks"/> is an
    /// arc landing on the wire in a filing clerk's voice, and what a filing is about is the OFFICE that
    /// filed it — carried in <c>Detail</c> by the push site rather than fished back out of the headline.
    /// Every other kind files a plain note with no thread, which is the honest answer for a line that names
    /// nothing the book has a heading for.</para>
    ///
    /// <para><b>Why the hull is a <see cref="CaseSubjects.Kind.Place"/>.</b> The three kinds are Office,
    /// Place and Person, and a hull is none of them cleanly — but in this game a hull is somewhere with a
    /// door in it: you board it, you rob it, you walk a wreck's hold. Person is ruled out by that kind's own
    /// law (it is a promise that a PERSON's printed name is in the sentence), and an Office it plainly is
    /// not. FLAGGED in the PR for the owner: a fourth kind for a hull is a Core change and a design call,
    /// not a crew's.</para>
    /// </summary>
    public static string SubjectsFor(NewsEvent evt) => evt.Kind switch
    {
        NewsEventKind.IntelPurchased when !string.IsNullOrWhiteSpace(evt.Subject) =>
            CaseSubjects.Line(CaseSubjects.Place(evt.Subject)),
        NewsEventKind.ArcBeatBreaks when !string.IsNullOrWhiteSpace(evt.Detail) =>
            CaseSubjects.Line(CaseSubjects.Office(evt.Detail!)),
        // #1202 — her story is about two things the sentence prints: the stringer whose byline is at its foot
        // (a PERSON, and the printed name is in the line), and the body it is about (a PLACE).
        NewsEventKind.PressStoryFiled when !string.IsNullOrWhiteSpace(evt.Detail) =>
            CaseSubjects.Line(CaseSubjects.Person(CarryThePress.Byline), CaseSubjects.Place(evt.Detail!)),
        NewsEventKind.PressFloorReaction when !string.IsNullOrWhiteSpace(evt.Detail) =>
            CaseSubjects.Line(CaseSubjects.Place(evt.Detail!)),
        _ => "",
    };
}
