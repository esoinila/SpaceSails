using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #585 · THE HIVE — the secret lab as a facility, not a two-door apartment.
///
/// <para>Owner, 2026-08-01: <i>"I expect a large space to be discoverable with long tunnels. Maybe we could go
/// underground so that we don't need to go out of the border on normal level. Instead ... show what is on our
/// current 'floor' level. On surface we would only need a camouflaged elevator. I think there are a lot of
/// movie references to masked elevators to underground sites (The Hive in Resident Evil for example). I just
/// don't want the secret lab to be puny 2 door apartment, but look like it could facilitate a large operation
/// with serious funding. We can again use the locked doors to give the illusion of much larger space. And say
/// corridors that lead to somewhere far away where we dare not venture too far into."</i></para>
///
/// <para>What it replaces: <c>SecretLab</c> appended ONE room, 16 x 14 du, for a find the code itself bills as
/// "the veterans' once-a-career" payoff worth five thousand credits. His phrase for it was exact.</para>
///
/// <h3>Why down is the right answer, and not only thematically</h3>
/// <para><b>Each floor reuses the surface's own coordinate envelope.</b> That makes the "don't walk past the
/// border" problem disappear rather than be fought: a complex the size of the whole field costs no new space,
/// because it is not beside the field, it is under it. The renderer shows one floor; the deck-plan swap that
/// does it is the same machinery the ship ↔ haven ↔ surface switch already uses.</para>
///
/// <h3>The three calls</h3>
/// <para>The owner said "go forward" without answering the three open questions, so they are decided here and
/// written down loudly enough to be overruled in one line each:</para>
/// <list type="number">
/// <item><b>You find it by LOOKING.</b> The lift head is a real structure on the surface — a squat blockhouse
/// that reads like any other ruin until you are close enough to see its door, which is
/// <see cref="BodyPalette.Imported"/> violet. On a moon where every hatch is local stone, that is the one door
/// that was flown here, and it is the best possible use of the #592 language. The metal-detector probe still
/// works and still pings; it is no longer the only way, because one square in a 310 x 260 field is a needle in
/// a haystack.</item>
/// <item><b>Three floors down, and the bottom is not a bottom.</b> −1, −2, −3, and the deepest ends at a
/// sealed corridor mouth with a distance painted on it. The world continues past where you are allowed to
/// walk; that is the whole feeling he asked for.</item>
/// <item><b>−1 still holds pressure. Below that it does not.</b> This is the one that decides how the place
/// FEELS, so it gets the answer with a beat in it: the first floor is a refuge — the tank stops, the nerve
/// steadies, you relax — and everything below is dead, so depth is paid for in air and every stair down is a
/// decision about getting back up. A complex that is uniformly safe is a museum; one that is uniformly hostile
/// is a corridor shooter. The lie is what makes it frightening.</item>
/// </list>
///
/// <para>Canon holds absolutely: nothing down here explains what the Old Ones are (owner ruling 2026-07-30).
/// A facility may be enormous, expensive and obviously state-backed, and may never say what it was for.</para>
///
/// <para>#251 · THIS FILE IS WHAT KIND OF PLACE IT IS, AND HOW FAR DOWN IT ADMITS TO GOING — the
/// <see cref="Kind"/> of a site and of each of its floors, the head office, the plate the lobby
/// shows, and the LISTED depth. Four more of the family's thirty-odd partials were cut out of it
/// under #251, each a contiguous run of what this file used to be:</para>
///
/// <list type="bullet">
/// <item><c>.Unlisted</c> — #592's band nobody listed, and the true depth that knows about it.</item>
/// <item><c>.Found</c> — #677's band nobody dug, one band below that, with a whole band of nothing
/// between them.</item>
/// <item><c>.Bands</c> — every floor a site actually has, the shaft arithmetic, and the walk
/// between floors.</item>
/// <item><c>.Conditions</c> — what a floor states about ITSELF: whether it holds pressure, whether
/// it is plumbed, and #708's ruling that darkness is a property of a floor.</item>
/// </list>
///
/// <para>The cut was free of the #1163 hazard by construction: this class declares no
/// <c>static readonly</c> field anywhere — every number in it is a <c>const</c>, folded into its
/// uses at compile time, so no file order can initialise one late. <c>Lerp</c> and <c>Frac</c>, the
/// two private helpers the whole family seeds off, stayed here. No member is renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>#585 · WHAT KIND OF PLACE THIS IS. Owner, extending the brief: <i>"feel free to upgrade the
    /// expanded section into proper literally underground lab space. We can have a lot of those in the sites,
    /// different clandestine sites in the spirit of world building."</i>
    ///
    /// <para>So this is not one rare lab any more — it is a CATEGORY. Clandestine sites are a thing that
    /// happens under moons, plural, and each kind is a different arm of the same unspeakable business. None
    /// of them ever explains the business; they only show you what it costs to run.</para></summary>
    public enum Kind
    {
        /// <summary>Dr Vantar's own — the original #409 find, now with a building around it.</summary>
        Laboratory,
        /// <summary>Where people were counted, graded and moved. The most bureaucratic and the worst.</summary>
        ProcessingDepot,
        /// <summary>Paper. Rooms and rooms of it, and somebody once thought that was the safe option.</summary>
        RecordsAnnex,
        /// <summary>A clinic with no name on the door and no register it appears in.</summary>
        BlackClinic,
        /// <summary>A transfer station: things came in, things went out, and the manifests do not match.</summary>
        TransitStation,

        /// <summary>#411/#635 · THE HEAD OFFICE. Owner ruling, 2026-08-03: <i>"The KAAMOS destination is the
        /// HEAD of the organization. Not another outpost, not a bigger wintering camp: the place everything
        /// else answers to … The Hive facilities are branch offices. HQ outclasses them, and it should
        /// outclass them IN THE SAME VOCABULARY, so a player who has crawled a Hive recognises the rank
        /// difference without being told it."</i>
        ///
        /// <para>It is <b>never rolled</b>. <see cref="KindFor"/> assigns it to exactly one body in the
        /// system and the die that picks the other five has never heard of it — a head office that a seed
        /// could produce twice would not be a head office.</para></summary>
        HeadOffice,
    }

    /// <summary>
    /// #411 · Is the building under this body THE head office? There is exactly one, it is under the ice
    /// moon, and the id comes from the arc that owns it (<see cref="KaamosLore.IceMoonBodyId"/>) rather than
    /// from a literal typed here — one source of truth, and the reason nothing in this file has to know what
    /// PROJEKTI KAAMOS is.
    ///
    /// <para>Everything below asks this ONE question and then answers in the branch-office vocabulary the
    /// player already reads. That is the whole of the ruling: not a new grammar, the same grammar at a rank
    /// nobody has to be told about.</para></summary>
    public static bool IsHeadOffice(string bodyId) =>
        string.Equals(bodyId, KaamosLore.IceMoonBodyId, StringComparison.Ordinal);

    /// <summary>
    /// #411 · IS THE HEAD OFFICE THERE? The only site in the game whose existence is a fact about the
    /// CAPTAIN rather than about the moon.
    ///
    /// <para>Every other clandestine site is a one-in-forty roll on a body id. This one is there when the
    /// berth-code has resolved and the hull is on the board (<see cref="KaamosLore.CanReachEnceladus"/>),
    /// and it is <b>not there</b> otherwise — not sealed, not refused, not hinted at. Featureless ice and a
    /// good view.</para>
    ///
    /// <para>That refusal-by-ABSENCE is the whole reason the arrival lands, and it is the honest reading of
    /// an arc every one of whose shards is about a filing, a window, a berth or a manifest and not one of
    /// which is about fuel: nobody is stopping a captain going to the ice moon. What nobody can do is be
    /// EXPECTED there.</para>
    ///
    /// <para>Pure and world-blind, so the client asks rather than deciding — a rule this load-bearing living
    /// as an <c>if</c> in a partial class is a rule no test can reach.</para></summary>
    public static bool HeadOfficePresent(string bodyId, bool onTheBoard) =>
        IsHeadOffice(bodyId) && onTheBoard;

    /// <summary>Which kind hides under this body. Seeded, so a moon has the site it has — except the one
    /// that is not a matter of chance.</summary>
    public static Kind KindFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (IsHeadOffice(bodyId))
        {
            return Kind.HeadOffice;
        }

        return (Kind)(DiceRule.Roll(DiceRule.Seed($"hive:kind:{bodyId}"), 5).Face - 1);
    }

    /// <summary>#592 · What kind of place THIS FLOOR is. The same as the site's own kind everywhere the
    /// building admits to, and something else entirely on the band nobody listed.
    ///
    /// <para>This is where the feature does its storytelling and it costs nothing but a different word list.
    /// A records annex whose bottom floor is a clinic tells you what the records were <i>of</i> without one
    /// line of narration — and, crucially, without ever saying it. The doors read MORTUARY and CONSENT FILES
    /// under twelve floors of RETENTION 40 YR and DESTRUCTION QUEUE, and the captain does the arithmetic
    /// themselves, or does not.</para>
    ///
    /// <para>Guaranteed DIFFERENT from the floors above: a hidden clinic under a clinic is a bigger clinic,
    /// which is the one outcome that makes the whole thing pointless.</para></summary>
    public static Kind KindOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        Kind above = KindFor(bodyId);
        if (!IsUnlisted(bodyId, level))
        {
            return above;
        }

        int kinds = Enum.GetValues<Kind>().Length;
        int step = DiceRule.Roll(DiceRule.Seed($"hive:unlisted-kind:{bodyId}"), kinds - 1).Face;
        return (Kind)(((int)above + step) % kinds);   // step is 1..kinds-1, so never `above`
    }

    /// <summary>What the place calls itself, if it calls itself anything.</summary>
    public static string TitleOf(Kind kind) => kind switch
    {
        Kind.Laboratory => "▣ THE LABORATORY",
        Kind.ProcessingDepot => "▣ THE PROCESSING DEPOT",
        Kind.RecordsAnnex => "▣ THE RECORDS ANNEX",
        Kind.BlackClinic => "▣ THE CLINIC",
        Kind.HeadOffice => "▣ THE HEAD OFFICE",
        _ => "▣ THE TRANSIT STATION",
    };

    /// <summary>#694 · DOES THIS FLOOR GET THE FACILITY PLATE — the sign beside the shaft that says
    /// <see cref="TitleOf"/> of <see cref="KindOn"/>?
    ///
    /// <para>Owner, standing on B11 of a deep site: <i>"every floor has the text 'The Clinic' on it. Some
    /// kind of artifact?"</i> It was not an artifact and it was not a leak — the plate simply drew on every
    /// floor, and a name repeated identically twenty floors deep stops being a name and becomes wallpaper.
    /// His reaction IS the spec.</para>
    ///
    /// <para><b>A building says its name where you ENTER it.</b> That is two floors and only two:</para>
    /// <list type="bullet">
    /// <item><b>B1</b> — the lobby. You came down from the surface and the plate tells you what you have
    /// walked into.</item>
    /// <item><b>The unlisted band's top floor</b>, where the site has one (#592) — its own lobby, reached by
    /// a card and a shaft nobody listed, and the one place in the game where the plate names a
    /// <i>different</i> Kind from everything above it. <c>▣ THE CLINIC</c> first seen under twelve floors of
    /// <c>RETENTION 40 YR</c> is that whole feature's arithmetic delivered by one sign.</item>
    /// </list>
    ///
    /// <para><b>Not every band head.</b> B5 and B9 are shaft heads too, and they get nothing: a captain
    /// stepping out there has not entered anything, they have gone deeper into the same place. What earns
    /// the plate is a Kind you have not been told yet — which is exactly B1 and the unlisted lobby, and the
    /// reason this cannot be simplified to "is this floor a band top".</para>
    ///
    /// <para>The head office needs no exception and gets none: it has twenty-four listed floors and, by
    /// <see cref="HasUnlistedBand"/>, nothing under them, so <c>▣ THE HEAD OFFICE</c> falls on B1 alone. HQ
    /// naming itself once, in its own lobby, is more in character than HQ naming itself twenty-four times —
    /// the head office does not have to keep telling you where you are.</para>
    ///
    /// <para>Every other floor is answered by the plate over the car (<c>B11 · LONG STORAGE</c>) and the
    /// department signage, which is floor identity and always was. Pure, so the law is testable without a
    /// renderer and the renderer only has to ask.</para></summary>
    public static bool ShowsFacilityPlate(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // Outdoors is not a floor of the facility.
        if (level >= 0)
        {
            return false;
        }

        // B1 — BandTop(0), and written as the head of the first shaft rather than as a typed −1, because
        // that is what makes it the LOBBY: the floor the surface's own car opens onto.
        if (level == BandTop(0))
        {
            return true;
        }

        return HasUnlistedBand(bodyId) && level == BandTop(UnlistedBandOf(bodyId));
    }

    /// <summary>#725 · IS THIS THE LOBBY OF THE BAND NOBODY LISTED — the one floor in the game where the
    /// plate beside the shaft names a different kind of building from every floor above it.
    ///
    /// <para>Written as <see cref="ShowsFacilityPlate"/> MINUS the entrance lobby, which is the whole of it:
    /// the plate law already answers "does a building say its name here", and it says yes in exactly two
    /// places. Take B1 away and what is left is the corrected wall. Nothing here re-derives a band — a second
    /// copy of that arithmetic is the named bug class this file opens with a table of, and #694's own doc
    /// comment is explicit that this cannot be simplified to "is this floor a band top" either.</para></summary>
    public static bool IsUnlistedLobby(string bodyId, int level) =>
        ShowsFacilityPlate(bodyId, level) && level != BandTop(0);

    /// <summary>#585 · DEPTH IS FREE. Owner, working out the architecture himself:
    ///
    /// <para><i>"since every secret lab can have a depth of it's own we do not need to worry about running out
    /// of space down there, since down there is unlimited amount of floors as far as we are concerned. So
    /// let's architect it to keep this in mind from the start. Well ok the lift shafts are the limiting
    /// factor, but besides those we have space."</i></para>
    ///
    /// <para>He is right, and it is the whole reason "down" was the correct answer. A floor costs no
    /// coordinate space because every floor reuses the surface's own envelope, so the only real budget is how
    /// far a captain will walk. Depth is therefore <b>a property of the site</b>, never a constant: a records
    /// annex might be three floors and a processing depot twenty, and the difference costs nothing.</para>
    ///
    /// <para>The bound below is a PERFORMANCE guard, not a design one — it exists so a seed cannot ask for a
    /// thousand floors. Nothing should ever read it as "how deep the game goes".</para></summary>
    public const int DeepestPossibleFloor = -24;

    /// <summary>How far down this site ADMITS to going. Seeded per body, weighted so most are modest and a
    /// rare one is a hole in the world worth telling people about.
    ///
    /// <para>#592: read this as the building's own account of itself — the bottom of the lift directory, the
    /// last floor on the plan in the lobby. On a rare site it is not the bottom of the hole. Anything asking
    /// "how far down can a captain actually walk" wants <see cref="TrueDepthOf"/>; anything asking "what does
    /// this place say about itself" wants this one, and the gap between them is the feature.</para></summary>
    public static int DepthOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // #411 · The head office takes the whole allowance, and LISTS all of it. A branch office's depth is
        // a roll; HQ's is the building. Note this is the DEPTH IT ADMITS TO, and for once that is also the
        // bottom of the hole — see HasUnlistedBand.
        if (IsHeadOffice(bodyId))
        {
            return DeepestPossibleFloor;
        }

        int roll = DiceRule.Roll(DiceRule.Seed($"hive:depth:{bodyId}"), 12).Face;
        int floors = roll switch
        {
            <= 6 => 2 + roll,      // 3–8, the common case
            <= 10 => 6 + roll,     // 13–16, a serious operation
            _ => 8 + roll,         // 19–20, the one you tell people about
        };
        return -Math.Min(floors, -DeepestPossibleFloor);
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private static double Frac(string bodyId, string tag) =>
        (DiceRule.Roll(DiceRule.Seed($"{bodyId}:{tag}"), 4096).Face - 1) / 4095.0;
}
