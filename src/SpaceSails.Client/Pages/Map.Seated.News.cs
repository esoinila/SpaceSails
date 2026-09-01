using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1052 (L2) · <b>READING THE NEWS IS A SEAT VERB, AND THE PAPER IS DOCKED.</b>
///
/// <para>Owner, 2026-09-01, on merging the boat's own bar: <i>"Just hope there is a way to have both the
/// newsfeed and pop-up-bar walkers visible UI at the same time. I think the news feed as pop up should be
/// exported to all the bars… Reading the news pop-up could be option when sitting at table, instead of
/// working the case, but we could get even new breadcrumbs from the news into our detective book."</i></para>
///
/// <h3>The three things this file is</h3>
///
/// <list type="number">
/// <item><b>The verb is SEAT-tied, never place-tied</b> (owner ruling 2026-08-30, the same ruling that took
/// the case register off the excursion). Any chair the captain is in offers the paper: a docked bar's top,
/// a stool at the boat's own counter, a moon canteen table on an excursion — the one place the ship galley
/// card at key <kbd>6</kbd> is rightly refused, and precisely the case
/// <c>TheNewsIsASeatVerbTests</c> drives. What the press hands you is <see cref="TheNewsPlaceHere"/>'s
/// answer and nothing else; there is no second question anywhere about which paper a room prints.</item>
/// <item><b>The panel is DOCKED, not modal.</b> It writes no <c>.view-object-backdrop</c> into the DOM at
/// all — the seated strip's own idiom (#784: <i>"the seated frame docks, it does not dim"</i>) — so the bar
/// floor, the walkers and an arriving rep's card are all still visible and still clickable while it is up.
/// That is the owner's hope answered structurally rather than promised: there is no scrim to be behind.</item>
/// <item><b>Reading files nothing. Clipping does.</b> <see cref="ClipThisStory"/> is the whole of what a
/// news line can do to the field book, and it is a press. #602's ammo-as-evidence philosophy, one system
/// over: what is in the satchel got there on purpose, and so does what is in the book.</item>
/// </list>
///
/// <h3>What is deliberately NOT here</h3>
///
/// <para>A second feed store. <c>NewsFeed</c> (Map.Alerts.cs) is the one ledger and this panel is its third
/// consumer; the masthead and the salt are L1's <c>NewsWire.ScopeAt</c>/<c>SaltFor</c>, asked of a place
/// this page already knows it is standing in. Nothing about a room leaks into Core beyond the four fields
/// of a <see cref="NewsWire.NewsPlace"/>.</para>
/// </summary>
public partial class Map
{
    /// <summary>Is the paper open? One gate, and it is the whole of this panel's state — the feed itself is
    /// computed on every draw off the one ledger, exactly as the galley card's is.</summary>
    private bool _seatedNewsOpen;

    /// <summary>
    /// #1052 · <b>WHAT THIS CHAIR READS</b> — the one place the client answers that, and the only thing it
    /// hands Core about a room.
    ///
    /// <para>Three cases and they are the three the design names. On an EXCURSION the site is the body the
    /// boots are on, and being below the surface (<c>Floor &lt; 0</c>) is being inside the facility — so a
    /// lab canteen table reads the company intranet. ASHORE at a berth it is that berth's own rag. And
    /// ABOARD — the boat's own stool row and the galley card behind key 6 — it is the system wire, which is
    /// what a wire read from nowhere in particular has always been.</para>
    ///
    /// <para><c>LabForced</c> is the GROUND'S OWN ANSWER rather than a second roll: <c>ex.Lab</c> is what
    /// <c>Map.SecretLab.cs</c> already carved this landing's door out of, cheat and head-office exception
    /// included. Re-deriving <c>SecretLab.Present</c> here would be a fourth-named-bug-class copy of a law —
    /// the noticeboard and the door could then disagree about whether there is a facility under your feet.
    /// </para>
    /// </summary>
    private NewsWire.NewsPlace TheNewsPlaceHere()
    {
        if (_surface is { } ex)
        {
            return new NewsWire.NewsPlace(
                AboardShip: false,
                SiteBodyId: ex.Stop.Body.Id,
                InsideSecretLab: ex.Floor < 0,
                LabForced: ex.Lab is { HasLab: true });
        }

        if (_ashore && _dockedHavenId is { } berth)
        {
            return new NewsWire.NewsPlace(AboardShip: false, SiteBodyId: berth);
        }

        return new NewsWire.NewsPlace(AboardShip: true, SiteBodyId: null);
    }

    /// <summary>Which masthead this chair is under — L1's own pure call, never a second opinion.</summary>
    private NewsWire.NewsScope SeatedNewsScope => NewsWire.ScopeAt(TheNewsPlaceHere());

    /// <summary>The paper in front of the captain: the one ledger, read under this chair's masthead and on
    /// this site's own salted stream.</summary>
    private IReadOnlyList<NewsWire.NewsItem> SeatedNewsFeed()
    {
        NewsWire.NewsPlace place = TheNewsPlaceHere();
        return NewsFeed(SeatedNewsAmbientDays, NewsWire.ScopeAt(place), NewsWire.SaltFor(place));
    }

    /// <summary>Is the verb on offer? Being in a chair is the whole condition — that is what "seat-tied"
    /// means, and the reason it works at a moon canteen where the desk keys do not.</summary>
    private bool TheNewsIsOnOffer => CaptainIsSeated;

    /// <summary>
    /// Is the paper actually ON THE SCREEN — the gate AND the seat, in one member.
    ///
    /// <para>Derived rather than left as two conditions the markup and the Escape chain each spell for
    /// themselves, and the reason is #1027's own bug read one surface over: the chain would otherwise
    /// consume the cancel key on a panel a captain who had stood up could no longer SEE, spending a press
    /// on nothing while the thing he was looking at sat there. One member, one truth, both readers ask
    /// it — and <c>StandUpFromTable</c> clears the gate as well, beside the spread it already clears, so
    /// the paper does not come back on its own the next time somebody sits down.</para>
    /// </summary>
    private bool ThePaperIsOpen => _seatedNewsOpen && TheNewsIsOnOffer;

    /// <summary>Raise the paper. One door for now (the strip's own button) and named anyway, so a second
    /// door cannot arrive without going through the same place.</summary>
    private void OpenSeatedNews() => _seatedNewsOpen = true;

    /// <summary>Put the paper down. The one house closer — the panel's own ✕ and the Escape rung both end
    /// here, so "the news panel is shut" means exactly one thing.</summary>
    private void CloseSeatedNews() => _seatedNewsOpen = false;

    /// <summary>#688's law, applied: the control that opens it closes it.</summary>
    private void ToggleSeatedNews()
    {
        if (_seatedNewsOpen)
        {
            CloseSeatedNews();
            return;
        }

        OpenSeatedNews();
    }

    /// <summary>
    /// #1052 · <b>✂ CLIP — the story goes in the book.</b>
    ///
    /// <para>Through <c>FileNoteAbout</c> and its 📰, so a clipped headline is an entry like any other: it
    /// carries the place the captain was sitting in, the sim-day, and — for the kinds whose author declared
    /// them (<see cref="NewsWire.SubjectsFor"/>) — the subjects that put it on a red-pen thread. The
    /// planted-story hook the design asks for is exactly that: a line that arrives with a name on it hands
    /// the case a thread the captain CHOSE to keep.</para>
    ///
    /// <para><b>The dedupe is <c>FieldNotes.Append</c>'s and is not re-implemented here.</b> Pressing the
    /// scissors twice on one story files one note, because Append refuses a repeat of the entry it is
    /// standing on. A second register of "what has been clipped" would be this repository's fourth named
    /// bug class — one law, transcribed at its call site, free to drift.</para>
    /// </summary>
    private void ClipThisStory(NewsWire.NewsItem item)
    {
        FileNoteAbout(item.Headline, NewsClipGlyph, item.Subjects ?? "");
        RequestVaultSave();
    }

    /// <summary>The glyph a clipped story wears in the book. Named once: the row's label, the guard and the
    /// filing all read it here rather than each typing a scissors-shaped emoji of their own.</summary>
    private const string NewsClipGlyph = "📰";

    /// <summary>Is this story already in the book? A READ of the one book — there is no clipping register —
    /// so the row can say where the paper is (<see cref="SeatedSpread.ClippedLabel"/>) instead of going on
    /// offering work already done. The control stays pressable either way (#212/#603).</summary>
    private bool AlreadyClipped(NewsWire.NewsItem item)
    {
        foreach (Core.FieldNote note in _fieldNotes)
        {
            if (note.Glyph == NewsClipGlyph && string.Equals(note.Text, item.Headline, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
