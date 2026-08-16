using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 7a · THE BOOT READS THE SAME QUERY IT ALWAYS READ.
///
/// <para>The other half of the snapshot. <see cref="TheBootBuildsTheSameWorldTests"/> pins the WORLD the
/// boot builds, but it can only see the boot as far as the browser gate — and eighteen of the query keys
/// (<c>?start=</c>, <c>?credits=</c>, <c>?fuel=</c>, <c>?fetch=</c>, <c>?crack=</c>, <c>?tip=</c>,
/// <c>?hoard=</c>, <c>?sling=</c>, <c>?skim=</c>, <c>?backroom=</c>, <c>?simhours=</c>, <c>?death=</c>,
/// <c>?kaamos=</c>, <c>?nebula=</c>, <c>?converge=</c>, <c>?ashore=</c>, <c>?nerve=</c>, <c>?reveal=</c>)
/// write nothing but a LOCAL before that gate. Sixteen of the seventy-five URLs in that sweep therefore
/// share a world fingerprint with another URL. This file is what tells them apart: it pins what the
/// 1,150-line <c>?query</c> chain ANSWERED.</para>
///
/// <para><b>Where these numbers come from.</b> The old code, like every other number in this lane. The
/// thirty values were locals in a single method and no test could reach them, so they were read off a
/// THROWAWAY branch that added one statement to the old <c>BootTheWorldAsync</c> — a recorder, at the
/// exact point this guard measures (after the berth defaults, before the scenario fetch) — and the branch
/// was thrown away. The names, the order (ordinal, by name) and the rendering are the recorder's, so what
/// is compared here is the same text the old parse produced.</para>
///
/// <para><b>It can tell pass from fail.</b> Thirty-eight of the seventy-five URLs answer distinctly, and
/// no two URLs share BOTH fingerprints except the three that are supposed to build the bare world — the
/// front door itself, a query of keys this page has never heard of, and a <c>?scenario=</c> the slug check
/// rejects.</para>
///
/// <para><b>Red proof.</b> One implication line deleted from the <c>?tablescene=</c> branch —
/// <c>q.SecretlabDeep = true;</c>, which is exactly the kind of side effect a method split is most
/// likely to drop on the floor — reddens this guard AND the world sweep next door, on exactly the two
/// <c>?tablescene=</c> URLs and on nothing else. Verbatim in the PR body.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBootReadsTheSameQueryTests
{
    /// <summary>The line break the recorder used, and therefore the one this rendering must use.</summary>
    private const string Nl = "\n";

    private static readonly IReadOnlyDictionary<string, string> WhatEachUrlSaid =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["/map"] = "779b62d347d83ecf5ff1c04bd1c41c83",
            ["/map?archive=1&land=1&nerve=2"] = "3c34c7dc8543424eb7f1bb1b000e6fe2",
            ["/map?ashore=1&kaamos=bounce"] = "4cac614093e39b44164399ea7c91ef10",
            ["/map?ashore=1&start=space-bar"] = "b477882b5366ad14bf3fea98cb6f6c65",
            ["/map?badge=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?bond=1"] = "27efba56488efbb29a78656d2585b0f6",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "9082e8dcc2a7a6887268cc61a1484725",
            ["/map?converge=1"] = "4e82a376afa29b6585aee0b77139cd69",
            ["/map?counter=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?credits=1234&fuel=7&simhours=9"] = "e45e2435adfcdc8faa40e81f723dbb0d",
            ["/map?credits=50000"] = "cc6e4609cdd3fa37f99a4b32e7229af0",
            ["/map?death=collector&dock=selene-gate"] = "1b0759305685764b2ae0f2322e2adf69",
            ["/map?death=impact"] = "34744b712f0242aa51665b2686ace127",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "6a57b4f6fe9c72e30e8c8821bebc3e59",
            ["/map?deflection=1"] = "3459123707a8c2db5c6efb372a00a242",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "03f169fb64f968f000190c729cda3c07",
            ["/map?designate=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "332805e822e2f9385ec46acdd8220d87",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "ddb5c1ddadd46f8d2d6d04159cdca040",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "4917080f85c867d32f0d2476b17161ce",
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "26b95409bc91eaefddd014ad170f138d",
            ["/map?dock=the-space-bar"] = "580eb43673671a6393fcba90764ad794",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "580eb43673671a6393fcba90764ad794",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "580eb43673671a6393fcba90764ad794",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "580eb43673671a6393fcba90764ad794",
            ["/map?dock=the-tilt&site=0"] = "2e67490b4d189e27f34c89e39d77a2e8",
            ["/map?dock=the-tilt&site=0&land=1"] = "2e67490b4d189e27f34c89e39d77a2e8",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "f1453d2f96436ab3aa5b95e3af996c3d",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "2e67490b4d189e27f34c89e39d77a2e8",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "2e67490b4d189e27f34c89e39d77a2e8",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "2e67490b4d189e27f34c89e39d77a2e8",
            ["/map?dock=the-tilt&site=1"] = "2e67490b4d189e27f34c89e39d77a2e8",
            ["/map?dock=the-tilt&start=space-bar"] = "821841f76c4467aec4d0da4ac3232cc1",
            ["/map?expedition=mining"] = "398129f33bc62156dd0abcc3e48085dd",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "9e4afd3c3bbfbfb624ba319348ff4ba6",
            ["/map?found=1&land=1"] = "a28bdaad4a146bc96cbe9284cd52aa28",
            ["/map?found=1&land=1&floor=17&card=all"] = "a28bdaad4a146bc96cbe9284cd52aa28",
            ["/map?freight=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?frontdoor=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?goodscar=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?kaamos=all"] = "4d37ffe2047a6e3b9f075728a117d7a2",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "654f5bbaab22179014eca5208f25456f",
            ["/map?kaamos=hq&land=1"] = "654f5bbaab22179014eca5208f25456f",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "4ba3c0cca16a5138cd4cf147228b3431",
            ["/map?nebula=all"] = "570e4af933bc16703b74e0c6a650d7d4",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "779b62d347d83ecf5ff1c04bd1c41c83",
            ["/map?park=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?park=1&spread=1"] = "885cf71b8ed559cbe924480f9e08c2b3",
            ["/map?parkback=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?parkwalk=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?patrol=2"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "fb9c4e2511f41624545f09285c17fa49",
            ["/map?ringoffice=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?rip=1"] = "885cf71b8ed559cbe924480f9e08c2b3",
            ["/map?scenario=..%2Foops"] = "779b62d347d83ecf5ff1c04bd1c41c83",
            ["/map?scenario=sol-eu"] = "49c218918858b12cc6bc4382885fab22",
            ["/map?secretlab=1"] = "a28bdaad4a146bc96cbe9284cd52aa28",
            ["/map?secretlab=deep&land=1&card=next"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?secretlab=deep&land=1&floor=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?secretlab=deep&land=1&floor=21"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?skim=saturn"] = "a8f910fa21cce6eb4561866ee45865c4",
            ["/map?sling=jupiter"] = "97b45b2e29e5ec24db289580f150572f",
            ["/map?spread=1"] = "885cf71b8ed559cbe924480f9e08c2b3",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "779b62d347d83ecf5ff1c04bd1c41c83",
            ["/map?start=wreck&fetch=active"] = "352f75c1e6963fc6ac73a136d9ff3858",
            ["/map?stool=1&neighbour=0"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?stool=1&neighbour=1"] = "724bfab09e6e78001d448fae522dfe45",
            ["/map?tablescene=free&approach=1"] = "885cf71b8ed559cbe924480f9e08c2b3",
            ["/map?tablescene=free&watch=5&approach=0"] = "885cf71b8ed559cbe924480f9e08c2b3",
            ["/map?threads=1"] = "885cf71b8ed559cbe924480f9e08c2b3",
            ["/map?threads=1&watch=5"] = "885cf71b8ed559cbe924480f9e08c2b3",
            ["/map?wreck=drivefailure&land=1"] = "6aa8794014974d5c08a65f3cce1b30b3",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "8ac42a7bbeddc421728cbafdd24fb0e2",
        };

    [Fact]
    public void EveryBootUrlIsReadTheWayItAlwaysWas()
    {
        var wrong = new List<string>();
        foreach (string url in TheBootBuildsTheSameWorldTests.EveryBootUrl())
        {
            string said = WhatTheQuerySaid(url);
            string hash = TheBootBuildsTheSameWorldTests.Sha256(said);

            Assert.True(WhatEachUrlSaid.ContainsKey(url), $"{url} is not pinned.");
            if (!string.Equals(WhatEachUrlSaid[url], hash, StringComparison.Ordinal))
            {
                wrong.Add($"{url}{Nl}  pinned {WhatEachUrlSaid[url]}{Nl}  read   {hash}{Nl}{said}");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} boot URLs are no longer read the way the one-method boot read them:{Nl}{Nl}"
            + string.Join(Nl + Nl, wrong));
    }

    [Fact]
    public void TheQueryFingerprintCanTellTwoQUERIESApart()
    {
        // The fifth bug class again: a reading that answered the same thing for every URL would be green
        // and would pin nothing. Most of the sweep's URLs deliberately say the same thing to the PARSE
        // (?park=1 and ?rip=1 both leave every one of these thirty at its default and do their work in
        // fields instead) — but the keys this file exists for must each move it.
        int distinct = TheBootBuildsTheSameWorldTests.EveryBootUrl()
            .Select(url => TheBootBuildsTheSameWorldTests.Sha256(WhatTheQuerySaid(url)))
            .Distinct(StringComparer.Ordinal)
            .Count();

        Assert.True(distinct >= 30, $"only {distinct} distinct query readings across the whole sweep.");
        Assert.NotEqual(WhatTheQuerySaid("/map"), WhatTheQuerySaid("/map?credits=50000"));
        Assert.NotEqual(WhatTheQuerySaid("/map"), WhatTheQuerySaid("/map?kaamos=all"));
        Assert.NotEqual(WhatTheQuerySaid("/map?nerve=low"), WhatTheQuerySaid("/map?nerve=half"));
        // …and the words ARE spellings of the number, never a second parser (#784).
        Assert.Equal(WhatTheQuerySaid("/map?nerve=low"), WhatTheQuerySaid("/map?nerve=2"));
    }

    [Fact]
    public void TheHolderCarriesEveryLOCALTheOldMethodDeclared()
    {
        // Thirty locals went in; thirty public fields must come out, or something the parse answers is
        // being answered somewhere this guard cannot see it.
        Assert.Equal(30, TheQuery("/map").GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public).Length);
    }

    /// <summary>Read the URL through the SHIPPING parse — the reader chain and the berth defaults, which is
    /// exactly where the recorder stood in the old one-method boot — and render what it answered.</summary>
    private static string WhatTheQuerySaid(string url)
    {
        object q = TheQuery(url);
        return string.Join(Nl, q.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .Select(f => $"{f.Name} = {TheBootBuildsTheSameWorldTests.Render(f.GetValue(q))}"));
    }

    private static object TheQuery(string url)
    {
        var map = new Pages.Map();
        TheBootBuildsTheSameWorldTests.NeverRender(map);
        TheBootBuildsTheSameWorldTests.Hand(map, "Navigation", new TheBootBuildsTheSameWorldTests.Bench(url));

        object q = Call(map, "ReadEveryQueryKey", new Uri("http://localhost" + url))!;
        Call(map, "DefaultABerthForTheCheatsThatNeedOne", q);
        Call(map, "RaiseTheFrontDoorWhileTheReactorWarms", q);
        return q;
    }

    private static object? Call(Pages.Map map, string name, object argument)
    {
        MethodInfo method = typeof(Pages.Map).GetMethod(name, TheBootBuildsTheSameWorldTests.Hidden)
            ?? throw new InvalidOperationException($"Map has no {name} — the boot's stages have been renamed.");
        return method.Invoke(map, [argument]);
    }
}
