using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1455 · U04-F23 fixed the patrol pass title with <c>PatrolBeat.SiteName</c>; four more places still printed
/// <c>{Designation} SITE</c>, which on a secret-lab site id reads <c>SECRET LAB SITE SITE</c>. Every card line
/// that names a site now says it through the one helper.
/// RED PROOF: put <c>{BodyNames.Designation(..)} SITE</c> back in any of the four and the matching case fails.
/// </summary>
public sealed class TheSiteNameIsSaidOnceEverywhereTests
{
    public static TheoryData<string> Sites => new() { "secret-lab-site", "secret-lab-site-unlisted", "luna" };

    private static void Said(string line, string site)
    {
        Assert.Contains(PatrolBeat.SiteName(site), line);
        Assert.DoesNotContain("SITE SITE", line);
    }

    [Theory]
    [MemberData(nameof(Sites))]
    public void TheShaftCardTitleSaysTheSiteOnce(string site) =>
        Said(UndergroundComplex.CardTitle(new UndergroundComplex.AuthorityCard(site, 1)), site);

    [Theory]
    [MemberData(nameof(Sites))]
    public void WhatTheCardRunsSaysTheSiteOnce(string site) =>
        Said(CarriedObject.WhatItRuns(new UndergroundComplex.AuthorityCard(site, 1), "somewhere-else"), site);

    [Theory]
    [MemberData(nameof(Sites))]
    public void TheStandingHonouredLineSaysTheSiteOnce(string site) =>
        Said(UndergroundComplex.StandingHonouredLine(new UndergroundComplex.AuthorityCard(site, 1)), site);

    [Theory]
    [MemberData(nameof(Sites))]
    public void TheWrongHoleRefusalSaysTheSiteOnce(string site)
    {
        string wanted = new UndergroundComplex.AuthorityCard("phobos", 1).Id;
        var held = new Satchel.Item(Satchel.Kind.Authority, new UndergroundComplex.AuthorityCard(site, 1).Id);
        SatchelTry.Outcome o = SatchelTry.Offer(held, SatchelTry.Target.ShaftGate, wanted);
        Assert.False(o.Worked);
        Assert.Contains("somebody else's business", o.Line);
        Said(o.Line, site);
    }
}
