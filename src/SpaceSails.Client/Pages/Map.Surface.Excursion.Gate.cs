using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Surface.Excursion.Gate — #618 / #582 slice 1 · THE MAN AT THE DOOR, AS THIS VISIT REMEMBERS HIM.
//
// One row on the record, and it is the one the brief allows: the seeded state of the door for this excursion,
// with the few facts about him that last exactly as long as the trip (whether he has been talked or badged
// past, whether he is on your heels, whether the tide has him, which of his lines have been told). Null until
// the captain first stands on the floor he keeps, so a walk that never goes below the lid carries nothing.
//
// What is DURABLE about him is not here: the face he remembers and the book's once are tags on the turned-over
// register (GateGuard.FaceTag / PastTag), for the reason #794's chalk rides there — a page field would move
// every fingerprint the frame ledger keeps for a feature that is not on screen.
public partial class Map
{
    public sealed partial class SurfaceExcursion
    {
        /// <summary>#618 · The man at the top-level door on this excursion, or null before the captain has
        /// stood on the floor he keeps.</summary>
        public ManAtTheDoor? Gate { get; set; }
    }

    /// <summary>
    /// #618 · <b>ONE EXCURSION'S MAN AT THE DOOR.</b> The seeded answer (<see cref="GateGuard.For"/>) and what
    /// this trip has done to it. Everything that decides is Core's; this is where the answers are kept.
    /// </summary>
    public sealed class ManAtTheDoor
    {
        /// <summary>The ground the door is under, and the window the excursion first reached it in — the two
        /// things the state is seeded on.</summary>
        public required string Ground { get; init; }

        public required long Window { get; init; }

        /// <summary>How the door is kept this window, and whether the word works.</summary>
        public required GateGuard.Visit Visit { get; init; }

        /// <summary>The floor he keeps.</summary>
        public required int Floor { get; init; }

        /// <summary>Where he stands: beside the cage's landing, on the corridor side.</summary>
        public required DeckReachability.Point Post { get; init; }

        /// <summary>The canteen end of his round, or null on a floor whose canteen he cannot walk to.</summary>
        public DeckReachability.Point? Canteen { get; init; }

        /// <summary>Talked or badged past on this trip. The door is a door for the rest of it.</summary>
        public bool Passed { get; set; }

        /// <summary>He came off the wall at the noise and is on the captain's heels.</summary>
        public bool Following { get; set; }

        /// <summary>He is standing at the mouth of the tube in the light, and this is how long he has stood.
        /// Negative while he is not there.</summary>
        public double AtTheMouthFor { get; set; } = -1;

        /// <summary>The tide had him. The door is unmanned for the rest of the excursion.</summary>
        public bool Taken { get; set; }

        /// <summary>The approach line has gone up on his card this excursion.</summary>
        public bool ApproachTold { get; set; }

        /// <summary>The empty chair has been told.</summary>
        public bool AbsentTold { get; set; }

        /// <summary>The round has been told, the first time he walked away from the door.</summary>
        public bool RoundTold { get; set; }

        /// <summary>The captain is inside his reach and has been carded for it; cleared when they walk off
        /// past <see cref="GateGuard.LeftHimDu"/>.</summary>
        public bool AtHisElbow { get; set; }

        /// <summary>Where his feet are, so a walk that is interrupted (a shift turning over clears the walker
        /// band) begins again at them rather than at the door.</summary>
        public DeckReachability.Point? StandingAt { get; set; }

        /// <summary>The round's clock: seconds at the door, or seconds away from it.</summary>
        public double RoundClock { get; set; }

        /// <summary>…how long the walk out to the canteen took, which is how early the walk back sets off.</summary>
        public double WalkOutSeconds { get; set; }

        /// <summary>…and which leg of the round he is on.</summary>
        public RoundLeg Leg { get; set; }
    }

    /// <summary>#618 · The four legs of his round.</summary>
    public enum RoundLeg
    {
        AtTheDoor,
        WalkingOut,
        AtTheCanteen,
        WalkingBack,
    }
}
