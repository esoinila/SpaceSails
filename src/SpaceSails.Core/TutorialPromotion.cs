namespace SpaceSails.Core;

/// <summary>
/// How a run began, as far as the new-player greeting is concerned (#292). The nav screen is not a
/// billboard: its tutorial promotions greet only the truly new, so the one thing the gate needs to
/// know is whether this run is a fresh cast-off from Earth, a fresh start somewhere else, or a saved
/// life picked back up.
/// </summary>
public enum TutorialStartMode
{
    /// <summary>A fresh new game cast off from Earth — the one place the Earth-anchored lessons fit.</summary>
    FreshFromEarth,

    /// <summary>A fresh start at some other locale (a gas giant, a docked bar). The lessons are
    /// Earth-anchored ("the Luna pod", "dock at Earth and sell"), so they are out of place here.</summary>
    FreshElsewhere,

    /// <summary>A saved life resumed (Continue from the vault, or an imported vault). Never a fresh
    /// captain — the greeting stays down.</summary>
    LoadedSave,
}

/// <summary>
/// The gating law for the nav-screen tutorial promotion (#292, owner 2026-07-18): "It is kind of out
/// of place on the nav-screen real estate, except when the player starts fresh from Earth, without
/// having played it." One pure predicate so the rule is trivially testable at the logic seam and the
/// Map component just asks it.
/// </summary>
public static class TutorialPromotion
{
    /// <summary>True when the nav screen may auto-raise the tutorial checklist: only on a fresh Earth
    /// start, and only for a captain who has not yet played (started or finished) the tutorial. Every
    /// other case — a non-Earth start, a loaded save, or a tutorial already played — keeps it down.</summary>
    public static bool ShouldPromote(TutorialStartMode mode, bool tutorialPlayed) =>
        mode == TutorialStartMode.FreshFromEarth && !tutorialPlayed;
}
