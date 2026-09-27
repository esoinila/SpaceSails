namespace SpaceSails.Core;

/// <summary>
/// #251 · WHAT HAPPENED TO HER — the <c>WreckCause</c> taxonomy and everything a cause decides on its own:
/// the headline, the art (as found and as cleared) and the evidence a boarding party reads.
///
/// <para>Split out of <c>Derelict.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no field.</para>
/// </summary>
public static partial class Derelict
{
    /// <summary>What happened to her. Owner: <i>"I guess there are many things that could have
    /// happened :-D"</i> — so the taxonomy is the content, and each one leaves a DIFFERENT wreck to walk
    /// through.</summary>
    public enum WreckCause
    {
        /// <summary>The drive quit mid-transfer. Nothing burned, nothing breached — she simply stopped
        /// being able to arrive, and everyone aboard had a long time to think about it.</summary>
        DriveFailure,

        /// <summary>The reactor ran away. The aft third is slag and the radiation is still interesting.</summary>
        ReactorCascade,

        /// <summary>Something small and fast went through her at closing speed. A hole you can see the
        /// stars through, and a decompression that took the crew before the alarm finished.</summary>
        HullBreach,

        /// <summary>The air plant failed and could not be fixed. The ship is INTACT — that is the horror of
        /// it. Everything works except the one thing that mattered.</summary>
        LifeSupportFailure,

        /// <summary>Somebody put a decimal in the wrong place and the arrival burn never came. She is
        /// exactly where the arithmetic said she would be, which is nowhere anyone was looking.</summary>
        NavigationalError,

        /// <summary>The crew fell out. Two factions, two barricades, and a ship that stopped being flown
        /// while they settled it.</summary>
        Mutiny,

        /// <summary>Boarded, stripped of what was easy, and left under way. Whoever did it was in a hurry —
        /// the valuable cargo is still in the deep hold.</summary>
        Piracy,

        /// <summary>SHE IS NOT EMPTY. Owner: <i>"there might be an infested ship where the cannons are
        /// needed also :-D … we should have a cannon in the airlock there to cover the retreat."</i>
        ///
        /// <para>Nothing failed aboard her — everything worked right up until something got in. The crew's
        /// barricades were built from the INSIDE, which is exactly what a mutiny looks like from a distance
        /// (see <see cref="MisreadsAs"/>), and that misreading is the most expensive one in the game: name
        /// it a mutiny and you file a report that sends the next crew in unarmed.</para></summary>
        Infested,

        /// <summary>She was LOST ON PURPOSE. The cargo was over-insured, the distress call was scripted, and
        /// the crew were taken off before she was set adrift. The most valuable thing aboard is the
        /// evidence (see <see cref="FraudBountyFraction"/>).</summary>
        InsuranceJob,

        /// <summary>SHE WAS VENTED FROM THE INSIDE, BY ONE OF HER OWN. Owner: <i>"one ship destiny might
        /// be that somebody went crazy due to an object and vented most of the ship and the survivors also
        /// fell to some other craziness after that."</i>
        ///
        /// <para>Someone aboard stood too long beside the thing in her hold, walked aft to the valve board,
        /// and blew her compartment by compartment with her crew inside. The doors were thrown from the
        /// SPINE side, not barricaded from within — and exactly ONE compartment was left with air in it,
        /// because <see cref="HullVenting.Readiness"/> will not blow the room you are standing in. The
        /// interlock the player knows from their own hands, read years later as a confession.</para>
        ///
        /// <para>Then the second madness: the few who lived through their own ship kept her log for months
        /// afterwards in an immaculate administrative hand — watch rotations, meal counts, forty names
        /// signing on and off a ship with nobody aboard. In the log, nothing ever happened.</para></summary>
        VentedByOneOfTheirOwn,
    }

    /// <summary>The headline the wreck reads as once the cause is known.</summary>
    public static string CauseHeadline(WreckCause cause) => cause switch
    {
        WreckCause.DriveFailure => "the drive quit and she never arrived",
        WreckCause.ReactorCascade => "the reactor ran away with her",
        WreckCause.HullBreach => "something small went through her at speed",
        WreckCause.LifeSupportFailure => "the air plant failed and everything else kept working",
        WreckCause.NavigationalError => "the arrival burn was never coming",
        WreckCause.Mutiny => "the crew stopped flying her to settle something",
        WreckCause.Piracy => "she was boarded, stripped in a hurry, and left under way",
        WreckCause.Infested => "she is not empty",
        WreckCause.InsuranceJob => "she was lost on purpose",
        WreckCause.VentedByOneOfTheirOwn => "one of her own opened her to space, compartment by compartment",
        _ => "",
    };

    /// <summary>The wreck's portrait — a Grok canvas per cause, because eight ships that died eight
    /// different ways should not all look the same. Shown when the away team reads the cause's own station
    /// (you are looking at the thing) and again on the decision card (you are deciding about it).
    ///
    /// <para>The art shows the EVIDENCE, never the conclusion: empty lifeboat cradles, not a caption saying
    /// "fraud". Naming what it means is still the captain's job, and a wreck that lies still lies.</para>
    ///
    /// <para>Every slot degrades cleanly — the client's img onerror-hides — so an unpainted cause reads as
    /// text alone rather than a broken frame.</para></summary>
    public static string ArtFile(WreckCause cause) => cause switch
    {
        WreckCause.DriveFailure => "art/wreck-drive-failure.jpg",
        WreckCause.ReactorCascade => "art/wreck-reactor-cascade.jpg",
        WreckCause.HullBreach => "art/wreck-hull-breach.jpg",
        WreckCause.LifeSupportFailure => "art/wreck-life-support.jpg",
        WreckCause.NavigationalError => "art/wreck-navigational-error.jpg",
        WreckCause.Mutiny => "art/wreck-mutiny.jpg",
        WreckCause.Piracy => "art/wreck-piracy.jpg",
        WreckCause.Infested => "art/wreck-infested.jpg",
        WreckCause.InsuranceJob => "art/wreck-insurance-job.jpg",
        WreckCause.VentedByOneOfTheirOwn => "art/wreck-vented.jpg",
        _ => "",
    };

    /// <summary>
    /// THE SAME ROOM, AFTER THE VACUUM HAD IT. Owner: <i>"should we have a different pic after vent
    /// cycle… one with claw marks :-D"</i>
    ///
    /// <para>Which is the proof the soak has never been able to offer. The counter says a number and the
    /// instrument still refuses to name what was in there, so the only thing that ever CONFIRMED the
    /// vacuum did its job was the absence of a fight on the way out. Standing in the room afterwards and
    /// seeing what it left is the payoff — and it keeps the rule, because what you find is claw marks and
    /// a collapsed nest, never the thing that made them.</para>
    ///
    /// <para>Empty for causes with nothing to kill: venting an ordinary hold changes the pressure, not the
    /// story.</para>
    /// </summary>
    public static string ArtFileCleared(WreckCause cause) => cause switch
    {
        WreckCause.Infested => "art/wreck-infested-vented.jpg",
        _ => "",
    };

    /// <summary>What the cause's own station reads once the vacuum has finished with it.</summary>
    public static string EvidenceCleared(WreckCause cause) => cause switch
    {
        WreckCause.Infested =>
            "the nest is dried, collapsed and frost-shattered, the casings are frozen where they fell — and " +
            "the deck plating beside the sealed door is gouged with long parallel claw marks, deliberate and " +
            "unhurried, made by something that worked at that door for a very long time and is not working " +
            "at it now",
        _ => "",
    };

    /// <summary>What the away team actually FINDS aboard — the investigation's raw material. Deliberately
    /// physical: the report is written from what is bolted to the deck, not from a tooltip.</summary>
    public static string Evidence(WreckCause cause) => cause switch
    {
        WreckCause.Infested =>
            "every barricade aboard was built from the INSIDE, the arms lockers are open and spent, and something has been nesting in the deep hold for a very long time",
        WreckCause.DriveFailure =>
            "the drive bells are cold and clean, the fuel gauges read FULL, and the log's last hundred entries are all the same failed restart",
        WreckCause.ReactorCascade =>
            "the aft third is slag, the shielding is peeled outward, and the dosimeters by the door are still counting",
        WreckCause.HullBreach =>
            "a hole the width of a thumb through four decks in a straight line, and every locker on that line blown open from inside",
        WreckCause.LifeSupportFailure =>
            "the scrubber stacks are choked solid, every other system reads nominal, and the bunks were made up",
        WreckCause.NavigationalError =>
            "a flight plan pinned at the nav post with the arrival burn worked twice, in two different hands, to two different answers",
        WreckCause.Mutiny =>
            "two barricades facing each other down one corridor, and the arms locker opened with a cutting torch",
        WreckCause.Piracy =>
            "the near hold stripped to bare frames, the deep hold untouched, and the airlock cycled from outside",
        WreckCause.InsuranceJob =>
            "the lifeboat cradles are empty and their release logs predate the distress call, the manifest is countersigned twice, and the cargo seals were opened and re-set",
        WreckCause.VentedByOneOfTheirOwn =>
            "every door was thrown from the SPINE side and every compartment but one is frosted hard vacuum — and the log runs on for months after that, in one immaculate hand, signing forty names on and off watch",
        _ => "",
    };
}
