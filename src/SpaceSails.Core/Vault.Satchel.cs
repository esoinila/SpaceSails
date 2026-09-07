namespace SpaceSails.Core;

// ─── VAULT SECTIONS: THE SATCHEL AND EVERYTHING WORKED THROUGH IT (#225) ───
//
// What is in the bag, what has been worked up out of it, which rooms have been turned over, what is in
// the filing line, who walked in, what the finder has walked — and the three that are about people the
// captain used to know: old crew, crossings, and the memories being held on to.
// 
// Split out of Vault.cs under #251 — whole record types moved, nothing inside one re-ordered.

/// <summary>#590 · WHAT THE CAPTAIN IS CARRYING THAT OPENS SOMETHING. Owner: <i>"could there be like a
/// keycode etc that allows us access to the lab"</i>.
///
/// <para>Stored as the flat list of card ids (<c>UndergroundComplex.AuthorityCard.Id</c>) and nothing else,
/// so the save carries the FACT and the prose is rebuilt from the generator at read time — the same shape
/// KAAMOS uses, and for the same reason: a card's title is a seeded property of the world, and a file that
/// carried the words would go stale the day the words changed. Ids the current build cannot parse are
/// dropped on load rather than thrown over.</para>
///
/// <para>These are durable on purpose. A card is found eleven floors under a moon and must still be in the
/// captain's pocket a month and a world later, or it is not a possession — it is a mood.</para></summary>
/// <summary>#603 · THE POCKET. Stored as opaque item strings (<c>Satchel.Item.Stored</c>) so the save carries
/// the FACT and never the words — every label, every clue's certainty and every card's title is a seeded
/// property of the world, rebuilt at read time. A file that carried the prose would go stale the day the
/// prose changed, which is the shape of half the bugs in this repository's history.
///
/// <para>Supersedes <see cref="AuthoritiesSection"/>, which is still READ on load so an older save's cards
/// migrate in rather than being lost.</para></summary>
public sealed record SatchelSection
{
    /// <summary>Items, in whatever order they were written.</summary>
    public IReadOnlyList<string> Items { get; init; } = [];
}

/// <summary>
/// #1016 · <b>THE CASE'S OWN REGISTER: which sheets have been dug out at a table.</b>
///
/// <para>Owner, 2026-08-30, on finding "Work the case" dead at a bar top in a docked haven: <i>"Maybe it
/// might be good idea to refactor the working the case etc table options to not be tied to any location?
/// Kind of clean separation from the arriving random encounters that are more place tied events."</i></para>
///
/// <para>It lived on the <c>SurfaceExcursion</c> until that ruling, which made it a fact about a WALK: dig a
/// pay sheet on B1, fly home, and the book still held the entry while the register that knew the sheet was
/// worked had been thrown away with the shuttle. Worse, a captain sitting in a station bar had no excursion
/// at all, so every reader of it answered "no" and every writer of it dropped the write on the floor. A
/// register that belongs to the case belongs to the captain, and a captain's things ride the vault.</para>
///
/// <para>Stored as the opaque keys the register is keyed on (<c>Kind:Id</c>, built by the one key builder,
/// <c>Map.WrittenUpKey</c>) — the same shape <see cref="SatchelSection"/> and <see cref="FilingSection"/> use
/// and for the same reason: the file carries the FACT (this sheet is in the book in the captain's own hand)
/// and never a sentence, because every word the book prints about a sheet is rebuilt from the paper at read
/// time. A key this build cannot recognise costs nothing to carry, so nothing is dropped.</para></summary>
public sealed record WorkedUpSection
{
    /// <summary>One key per sheet already dug out. A set on the page, so the order this list happens to be
    /// in is not a fact about anything.</summary>
    public IReadOnlyList<string> Sheets { get; init; } = [];
}

/// <summary>
/// #615/#573 · <b>WHICH ROOMS THIS CAPTAIN HAS TURNED OVER, AND UNDER WHICH MOON.</b>
///
/// <para>The register lived on the <c>SurfaceExcursion</c> and died with the shuttle, which was invisible
/// while a find was an automatic pickup — flying away and coming back simply re-filled a facility, and the
/// only thing that cost was an exploit nobody had noticed. #615 makes it load-bearing: LEAVE's whole promise
/// is that <i>a captain can come back for the paper they walked past</i>, and a promise you cannot tell from
/// the world resetting itself is not a promise. With this section, a room the captain KEPT from is empty
/// when they return and a room they LEFT still holds its find — which is the difference the decision is
/// about, written down.</para>
///
/// <para>Stored as the opaque site-qualified keys the register is keyed on
/// (<see cref="KeepOrLeave.RoomKey"/>) — the same shape <see cref="SatchelSection"/> and
/// <see cref="WorkedUpSection"/> use and for the same reason: the file carries the FACT (this room has been
/// gone through) and never a sentence, because every word said about a room is rebuilt from the seed at read
/// time. A key this build cannot recognise costs nothing to carry, so nothing is dropped on load.</para>
/// </summary>
public sealed record TurnedOverSection
{
    /// <summary>One key per room already gone through. A set on the page, so the order this list happens to
    /// be in is not a fact about anything.</summary>
    public IReadOnlyList<string> Rooms { get; init; } = [];
}

/// <summary>#973 L1 · The filing line's marks. Stored as opaque row strings
/// (<c>FilingLine.Page.Stored</c>) — the same shape <see cref="SatchelSection"/> and
/// <see cref="CaseThreadsSection"/> use and for the same reason: the file carries the FACT (this row is grey;
/// this one came back wrong and really said <i>this</i>) and never the sentences, which are rebuilt from the
/// live ledger every render. A row this build cannot parse is dropped rather than thrown over.</summary>
public sealed record FilingSection
{
    /// <summary>One row per marked ledger entry, in ledger order.</summary>
    public IReadOnlyList<string> Pages { get; init; } = [];
}

/// <summary>#973 L5b · What the SPREAD has found out about a walk-in. Job ids and nothing else: the file
/// carries the FACT (this job is a setup and the captain knows it) and never the grey line, which
/// <see cref="WalkIn.SetupCardLine"/> rebuilds every render — the same shape <see cref="FilingSection"/> and
/// <see cref="SatchelSection"/> use, for the same reason. A knowing is never un-known, so this list only ever
/// grows.</summary>
public sealed record WalkInSection
{
    /// <summary>One job id per walk-in the SPREAD has read the same hand off. A set on the page, so the order
    /// this list happens to be in is not a fact about anything.</summary>
    public IReadOnlyList<string> SetupsRevealed { get; init; } = [];
}

/// <summary>#417 · THE FINDER'S CASE, as two opaque rows (<see cref="FinderCase.Stored(in FinderCase.Case)"/>
/// and its progress twin). The file carries the GRAPH — which port, which witness, which two hulls, which
/// berth — and how far down it the captain has got, and never one of Varga's sentences, which are rebuilt
/// from Core's own constants on every render.
///
/// <para>The graph is written down rather than re-derived because it is a function of the TRAFFIC as well as
/// of the thread, and a wave dealt afresh would move the hull and the berth under a captain who is halfway
/// along the trail. A row this build cannot parse is dropped and the captain simply has no case, which is the
/// same tolerance the filing line and the satchel keep.</para></summary>
public sealed record FinderSection
{
    /// <summary>The case Varga handed over, or empty when she never has.</summary>
    public string Case { get; init; } = "";

    /// <summary>…and what has been done about it.</summary>
    public string Progress { get; init; } = "";
}

/// <summary>#973 L5a · THE OLD CREW's seeding. Stored as opaque row strings so the file carries the FACT
/// (these four, bound like this, posted there) and never the words — every sentence the black book prints
/// about a shipmate is rebuilt from the pool at read time. A row this build cannot parse is dropped rather
/// than thrown over; a seeding that comes back short is re-rolled from the thread id.</summary>
public sealed record OldCrewSection
{
    /// <summary>One row per seeded shipmate, in pool order.</summary>
    public IReadOnlyList<string> Shipmates { get; init; } = [];

    /// <summary>#973 L5a · Which of them THIS captain has already explained his face to. Saved rather than
    /// recomputed for the reason the filing line's marks are: a scene is a fact about a life, and a reload
    /// that replayed it would let a player re-answer a question they have already answered — and write a
    /// second crossing for it. Emptied by a rebirth, because the next face has its own explaining to do.</summary>
    public IReadOnlyList<string> Explained { get; init; } = [];
}

/// <summary>#973 L5a · The captain's crossings, oldest first. Opaque rows (<c>CaptainCrossings.Crossing
/// .Stored</c>) for the reason every other book here uses them: the row carries which line, which situation
/// and who saw, and the sentence the desk prints is rebuilt from those three.</summary>
public sealed record CrossingsSection
{
    /// <summary>The crossings, oldest first.</summary>
    public IReadOnlyList<string> Crossings { get; init; } = [];
}

/// <summary>#973 · The held-memory sheets (<c>HeldMemory.Sheet.Stored</c>). These rows DO carry their text,
/// unlike most of the opaque books here, and deliberately: a held memory is authored prose or a person's own
/// words, not a sentence assembled out of a seeded world, and a sheet whose text was rebuilt could come back
/// saying something the captain never read.</summary>
public sealed record HeldMemoriesSection
{
    /// <summary>The sheets, in the order they entered the book.</summary>
    public IReadOnlyList<string> Sheets { get; init; } = [];
}
