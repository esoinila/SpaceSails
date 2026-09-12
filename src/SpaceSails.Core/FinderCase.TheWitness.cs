namespace SpaceSails.Core;

/// <summary>
/// #417 slice 2a · <b>THE DRINK-LOOSENED LEAD — the witness is a person, and a person has to be bought a
/// glass.</b>
///
/// <para><b>What slice 1 shipped, and why it was wrong.</b> The rota witness filed his sentence into the
/// field book the moment the captain reached him: walk up, and the lead is yours. Three leads, and the one
/// that is a MAN answered exactly like the two that are a paper and a transponder record. #347's own owner
/// ruling is the correction — <i>"having a drink at a bar with somebody is a sign of trust and should open
/// up new business opportunities, or give access to information"</i> — so the man who works a rota at a port
/// gives up what he saw for the one thing a stranger can honestly offer him, and only if he takes it.</para>
///
/// <h3>The three laws in this file, and not one of them is a new roll</h3>
///
/// <list type="bullet">
/// <item><b>The glass is the shipped glass.</b> Whether he accepts is <see cref="ContactDrink.OfferDrink"/>
/// — the same salted 2D6, the same named modifiers, the same accept-before-a-credit-moves order, the same
/// printed math — asked at the one place the bar already asks it. Nothing in this file rolls anything:
/// <see cref="WhatTheGlassDoes"/> is handed the verdict the bar's own offer produced. A second roll here
/// would be two dice deciding one question, which is this house's one-source-of-truth law with pips
/// on.</item>
/// <item><b>One ask a watch, whatever the answer was.</b> The same idiom slice 1 keeps for Varga (she asks
/// once an evening and the answer does not buy a second ask), on the unit a bar regular is actually measured
/// in: <see cref="Interior.PatronRota.WatchIndex"/>, the four-sim-hour beat the whole seated cast shuffles
/// on. A refusal therefore costs the captain the watch and not the lead — he can stand the man a glass
/// again next watch, and the ordinary drink flow is untouched in the meantime.</item>
/// <item><b>He talks once.</b> Once the lead is in the book he is an ordinary regular again, and no further
/// glass gets anything more out of him about it.</item>
/// </list>
///
/// <para>Pure, like the rest of Core: a fold and two questions over it. The watch index is handed in rather
/// than read, because a clock read in here would be the one impurity in a file about a man who is only ever
/// in the room at certain hours.</para>
/// </summary>
public static partial class FinderCase
{
    /// <summary>What one offered glass did to the witness lead. Three arms and no fourth: he talks, he does
    /// not, or there was nothing he had to give this glass in the first place.</summary>
    public enum WitnessAnswer
    {
        /// <summary>Nothing at all — the case is not taken, the lead is already in the book, or his one ask
        /// this watch is spent. No line is said and nothing files; the glass is just a glass.</summary>
        Nothing = 0,

        /// <summary>He took it. He says <see cref="WitnessTakesTheGlass"/> and the lead files.</summary>
        Talks = 1,

        /// <summary>He waved it off. He says <see cref="WitnessStaysOnShift"/>, nothing files, and the
        /// captain may ask again on a later watch.</summary>
        StaysOnShift = 2,
    }

    /// <summary>
    /// <b>WHAT THIS WATCH HAS ALREADY HAD OUT OF HIM.</b> Two facts and the watch they are about, folded
    /// exactly as slice 1's visit fold is: a different watch is a different evening, so the fold is not
    /// cleared by anybody — it is simply asked about a watch it does not know and answers as fresh.
    ///
    /// <para>Deliberately NOT stored in the vault. A watch is four sim-hours of a running game, the same
    /// transient the salesman's and the walk-in's visit folds are, and a reload inside one costs the captain
    /// nothing he can tell from the ordinary rhythm — while a stored one would have to be versioned into the
    /// progress row and change a save format for a fact that expires.</para>
    /// </summary>
    /// <param name="Watch">The watch these two flags are about (<see cref="Interior.PatronRota.WatchIndex"/>).</param>
    /// <param name="Greeted">He has already said he does not work for you, this watch.</param>
    /// <param name="Asked">…and he has already been offered his one glass, this watch, whatever he did with it.</param>
    public readonly record struct WitnessWatch(long Watch, bool Greeted, bool Asked)
    {
        /// <summary>A fold about no watch at all — what a captain who has never found the man has. The watch
        /// it names is one the clock cannot reach (sim time starts at zero and runs forward), so the first
        /// <see cref="On"/> of a real watch always forgets it.</summary>
        public static WitnessWatch Fresh => new(long.MinValue, false, false);

        /// <summary>This fold, asked about <paramref name="watch"/>: itself when that is the watch it is
        /// about, and a fresh one when it is not. THE ONE PLACE FORGETTING HAPPENS.</summary>
        public WitnessWatch On(long watch) => watch == Watch ? this : new(watch, false, false);

        /// <summary>…and he has now said it.</summary>
        public WitnessWatch Greeting() => this with { Greeted = true };

        /// <summary>…and he has now been asked.</summary>
        public WitnessWatch Asking() => this with { Asked = true };
    }

    /// <summary>
    /// <b>DOES HE SAY HE DOESN'T WORK FOR YOU?</b> Only with a case actually taken, only while the lead is
    /// still his to give, and only once a watch.
    ///
    /// <para>The last clause is the whole of why it is a fold and not a flag on the progress: the captain
    /// pressing E at the man four times in a minute is one meeting, and a sentence repeated four times is a
    /// machine talking. The next watch he is there again and says it again, because he does not remember the
    /// captain and that is the point of him.</para>
    /// </summary>
    public static bool TheGreetingIsDue(in Progress p, in WitnessWatch w) =>
        p.Taken && !p.WitnessHeard && !w.Greeted;

    /// <summary>
    /// <b>WHAT AN OFFERED GLASS DOES TO THE TRAIL.</b> Handed the verdict the bar's own
    /// <see cref="ContactDrink.OfferDrink"/> already reached — never a roll of its own.
    ///
    /// <para><see cref="WitnessAnswer.Nothing"/> covers the three cases where the glass is only a glass: no
    /// case taken, the lead already in the book, and — the one this slice exists for — a watch whose one ask
    /// is spent. That last arm is what makes a refusal cost the WATCH rather than the LEAD, and it is also
    /// what stops a captain buying the man eleven drinks in ninety seconds until the dice say yes: the offer
    /// seed folds the sim-second, so without it the trail would be a slot machine with a bar tab.</para>
    /// </summary>
    /// <param name="p">How far down the trail the captain has got.</param>
    /// <param name="w">What this watch has already had out of him — ask it <see cref="WitnessWatch.On"/> the
    /// current watch first, or you are reading the flags of a watch that has gone.</param>
    /// <param name="accepted"><see cref="DrinkOfferResult.Accepted"/>, and nothing else.</param>
    public static WitnessAnswer WhatTheGlassDoes(in Progress p, in WitnessWatch w, bool accepted)
    {
        if (!p.Taken || p.WitnessHeard || w.Asked)
        {
            return WitnessAnswer.Nothing;
        }

        return accepted ? WitnessAnswer.Talks : WitnessAnswer.StaysOnShift;
    }

    /// <summary>What he says to that answer, or nothing at all when there was nothing in it for him to
    /// answer. One place, so the line and the filing can never come apart.</summary>
    public static string LineFor(WitnessAnswer answer) => answer switch
    {
        WitnessAnswer.Talks => WitnessTakesTheGlass,
        WitnessAnswer.StaysOnShift => WitnessStaysOnShift,
        _ => "",
    };
}
