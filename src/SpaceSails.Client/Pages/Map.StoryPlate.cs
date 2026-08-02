namespace SpaceSails.Client.Pages;

// Map.StoryPlate — #528 · THE REVEAL CARD, GENERALISED.
//
// The owner, from the boat: "Let's add some cool gen-ai to places where we tell the story in the game. I
// think it makes a big difference to have that pop-up style we used in the reever vented room scenario.
// We have a lot of events that don't have that level of service yet."
//
// The vented-room card (Map.Venting.CheckVentPayoffUnderfoot) is the recipe, and it is four things, all
// load-bearing: a TITLE that names the place and the verb; ONE painted image of a consequence rather than
// an action shot; a CAPTION that describes evidence and stops; and it fires at the moment it explains the
// most. What it was NOT, until now, was reusable — it was a `WreckLook`, and only a wreck could raise it.
//
// This partial is the same card with the wreck taken out of it, so any beat in the game can raise one
// without inventing its own modal. It owns nothing but the state: the TEXT of every plate lives in Core
// beside the predicate that decides the beat happened (KaamosLore.PlateFor is the first tenant), which is
// #634's law — a sentence built in the client is a sentence that can drift away from the sim.
//
// The house degradation law applies unchanged: the code ships first, the <img> onerror-hides, and the JPG
// drops in behind it. A plate whose art is missing is a title and a caption, never a broken frame.
public partial class Map
{
    /// <summary>What the captain is being shown: a title, a painting, and a caption that stops. The most
    /// modal thing in the game — it opens without being asked for, so Esc takes it before anything else
    /// (TryDismissTopOverlay in Map.Sim).</summary>
    private readonly record struct StoryPlate(string Title, string Art, string Caption);

    private StoryPlate? _storyPlate;

    private void CloseStoryPlate() => _storyPlate = null;

    /// <summary>Raise the plate. Deliberately silent — the caller owns its own audio cue, because the beat
    /// knows whether it is a find ("board"), a reveal ("reveal") or something louder, and a card that
    /// always chirped the same note would flatten three different moments into one.</summary>
    private void ShowStoryPlate(string title, string art, string caption) =>
        _storyPlate = new StoryPlate(title, art, caption);
}
