using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// HER OWN CHARGES, AS LAWS. Owner: <i>"that ship also has the scuttling charges... let's have a captains
/// approval mechanic for that also on our ship. :-D ... it is the last defence against the Borg in Star Trek
/// :-D"</i>, then <i>"they only need to activate it for the borg to back down enough"</i>, and then the gap he
/// spotted himself: <i>"I guess we need the another opinion we just don't have a desk for that on the bridge
/// yet."</i>
/// </summary>
public sealed class ShipScuttleTests
{
    /// <summary>
    /// THE ONE AUTHORITY THAT CANNOT BE DELEGATED. The captain may hand damage control a standing order over
    /// her compartments; the charges are not covered by it and must never become covered by it. This mirrors
    /// the sentence already on the desk — <i>guns cleared ≠ boarding authorized</i> — because two acts of
    /// wildly different weight must never share one word.
    /// </summary>
    [Fact]
    public void AStandingOrderToDamageControlCanNeverArmTheCharges()
    {
        Assert.False(ShipScuttle.MayArm(captainSaidItByName: false, damageControlStandingOrder: true));
        Assert.True(ShipScuttle.MayArm(captainSaidItByName: true, damageControlStandingOrder: false));

        // And the standing order changes nothing about the captain's own half, either way.
        Assert.Equal(
            ShipScuttle.MayArm(captainSaidItByName: true, damageControlStandingOrder: false),
            ShipScuttle.MayArm(captainSaidItByName: true, damageControlStandingOrder: true));
    }

    /// <summary>The panel names the SHIP, the way the vent gate names the room — a word that names nothing can
    /// drift onto anything.</summary>
    [Fact]
    public void ThePanelNamesWhatIsOnTheTable()
    {
        Assert.Contains("SHIP OF MINE", ShipScuttle.AskFor("ship of mine"), StringComparison.Ordinal);
        Assert.Contains("ship of mine", ShipScuttle.ArmedFor("ship of mine"), StringComparison.Ordinal);
    }

    // ── The second key: the crew's ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE PROMISE OF THE CHARACTER DESIGN, MADE MECHANICAL (docs/features/the-captains-character.md §3): a
    /// monstrous captain with a devoted crew can turn both keys, and a decent captain nobody trusts finds that
    /// nobody moves. Character and cohesion are different axes, and this is where the second one bites.
    /// </summary>
    [Theory]
    [InlineData(CrewTemp.Standing.Solid, ShipScuttle.SecondKey.Turned)]
    [InlineData(CrewTemp.Standing.Grumbling, ShipScuttle.SecondKey.Turned)]
    [InlineData(CrewTemp.Standing.Petition, ShipScuttle.SecondKey.TurnedAndSaidSo)]
    [InlineData(CrewTemp.Standing.Ultimatum, ShipScuttle.SecondKey.Refused)]
    [InlineData(CrewTemp.Standing.Marooning, ShipScuttle.SecondKey.Refused)]
    public void TheCrewAnswerFromWhereTheyAlreadyStand(
        CrewTemp.Standing standing, ShipScuttle.SecondKey expected) =>
        Assert.Equal(expected, ShipScuttle.SecondVoice(standing));

    /// <summary>A crew past an ultimatum will not end the ship for him — which is the mechanic, not a bug, so
    /// the panel points at the sheet where the answer actually lives rather than explaining a rule.</summary>
    [Fact]
    public void ARefusalPointsAtTheCrewsOwnSheet()
    {
        Assert.Contains("Crew", ShipScuttle.RefusedPointerLine, StringComparison.Ordinal);
        Assert.Contains("two hands", ShipScuttle.RefusedPointerLine, StringComparison.Ordinal);
    }

    /// <summary>Every answer has a line, and none of them explains the arithmetic — the crew's behaviour IS the
    /// read-out, which is the law the whole character system is built on.</summary>
    [Theory]
    [InlineData(ShipScuttle.SecondKey.Turned)]
    [InlineData(ShipScuttle.SecondKey.TurnedAndSaidSo)]
    [InlineData(ShipScuttle.SecondKey.Refused)]
    public void EveryAnswerIsSpokenAsBehaviourRatherThanAsARule(ShipScuttle.SecondKey key)
    {
        string line = ShipScuttle.SecondVoiceLine(key);

        Assert.False(string.IsNullOrWhiteSpace(line));
        Assert.DoesNotContain("standing", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("satisfaction", line, StringComparison.OrdinalIgnoreCase);
    }

    // ── The deterrent, which is the point ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Owner: <i>"they only need to activate it for the borg to back down enough."</i> An armed hull is worth
    /// nothing to anybody — so the threat is the mechanic and the detonation is the failure case.
    /// </summary>
    [Fact]
    public void ArmingIsOnlyAThreatWhereSomebodyCanReadIt()
    {
        Assert.True(ShipScuttle.AnybodyToDeter(1));
        Assert.False(ShipScuttle.AnybodyToDeter(0));

        Assert.Contains("breaks off", ShipScuttle.DeterrentLine("THE LEDGER'S OWN"), StringComparison.Ordinal);
        Assert.Contains("THE LEDGER'S OWN", ShipScuttle.DeterrentLine("THE LEDGER'S OWN"), StringComparison.Ordinal);
    }

    /// <summary>The timing is the derelict's, unchanged — one law for both ships, so ninety seconds feels the
    /// same wherever the captain learned it. If these ever diverge, the transfer of skill this whole lane is
    /// built on quietly stops being true.</summary>
    [Fact]
    public void HerClockIsTheSameClockADerelictHas()
    {
        Assert.Equal(90.0, Scuttle.OverloadSeconds);
        Assert.True(Scuttle.CanAbort(Scuttle.OverloadSeconds));
        Assert.False(Scuttle.CanAbort(Scuttle.OverloadSeconds - Scuttle.AbortWindowSeconds - 0.01));
    }

    /// <summary>And the death has its own cause, because a captain who scuttled his own ship and stayed aboard
    /// did not die of anything else.</summary>
    [Fact]
    public void ScuttlingHasItsOwnDeathNarration()
    {
        Assert.Contains("scuttled her", DeathNarration.Headline(DeathCause.Scuttled), StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(DeathNarration.ArtFile(DeathCause.Scuttled)));

        string line = DeathNarration.Line(DeathCause.Scuttled, seed: 1, bodyName: "Enceladus");
        Assert.False(string.IsNullOrWhiteSpace(line));
        Assert.DoesNotContain("{", line, StringComparison.Ordinal);
    }
}
