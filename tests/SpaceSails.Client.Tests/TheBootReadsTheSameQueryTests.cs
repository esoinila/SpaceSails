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
/// <para><b>#973 L5a re-pinned every value in this file, and the WORLD sweep next door is the proof that
/// nothing behaved differently.</b> The dev door onto the old crew's scene (<c>?oldcrew=1</c>) adds a
/// thirty-first field to the holder, and this rendering walks EVERY public field of it — so
/// <c>OldCrewCheat = False</c> joins the text for all seventy-nine URLs and every digest moves, including
/// the bare front door's. That is the rendering changing, not the parse. The world fingerprints in
/// <see cref="TheBootBuildsTheSameWorldTests"/> read the built world rather than the holder, and exactly
/// ONE of them moved: the new URL's. The new values here are the failing run's own output, never typed by
/// hand.</para>
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
            ["/map"] = "a6547abcd4389bea94e4de74ca93b634",
            ["/map?archive=1&land=1&nerve=2"] = "f7f7b0a135a1211c7012c8aaac739870",
            ["/map?ashore=1&kaamos=bounce"] = "8c686d7bec6b862d1c1e34dcda7040b3",
            ["/map?ashore=1&start=space-bar"] = "7ed5829495725beb89ef7e3307fd8823",
            ["/map?badge=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?bond=1"] = "8682220bcfc49d8431fce35fdde41798",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "3093252fa48a0c1185e4945147844a64",
            ["/map?converge=1"] = "033c033c7bffffd70794e3ca6481fd87",
            ["/map?counter=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?counter=1&watch=2"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?counter=1&watch=5"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?credits=1234&fuel=7&simhours=9"] = "b1676b8418a8d6b34d001e020d0ee77e",
            ["/map?credits=50000"] = "fd2bfc30c608df4c19908e5815d22a83",
            ["/map?death=collector&dock=selene-gate"] = "9e174a2018742acf109c9caedc1b5c3a",
            ["/map?death=impact"] = "8ad2670c81b9b3ccf8f8a2d49fa09e1a",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "4439ddaf6ad566014304f086a8aacd29",
            ["/map?deflection=1"] = "1d1c0acb60823c338e62a5e41d19e558",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "67ecf213e4c92943d35d0138f001771c",
            ["/map?designate=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "7f7d0b90d0ed499b0be515fca61f0424",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "4b70009ec5c9ef8dae8f6c2a1d73291b",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "bcb18ca64a0c991534886e93ae2e016f",
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "0f1160b994fda28ec93416204de33268",
            ["/map?dock=the-space-bar"] = "5ef8916f537a3110b6beeea69e9baadd",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "5ef8916f537a3110b6beeea69e9baadd",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "5ef8916f537a3110b6beeea69e9baadd",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "5ef8916f537a3110b6beeea69e9baadd",
            ["/map?dock=the-tilt&site=0"] = "df7fbc55d42777815a5ca3127277760e",
            ["/map?dock=the-tilt&site=0&land=1"] = "df7fbc55d42777815a5ca3127277760e",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "69c379a09134c16f72a979851de7e97a",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "df7fbc55d42777815a5ca3127277760e",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "df7fbc55d42777815a5ca3127277760e",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "df7fbc55d42777815a5ca3127277760e",
            ["/map?dock=the-tilt&site=1"] = "df7fbc55d42777815a5ca3127277760e",
            ["/map?dock=the-tilt&start=space-bar"] = "1d3049d07a9bf569bb0f438ace0aa22c",
            ["/map?expedition=mining"] = "29b78eccfde9d7c82b82dcab3313cc6f",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "01c6844141256950db7152fac097e15a",
            ["/map?found=1&land=1"] = "78a433461078ed72a0c5644af5d0f80c",
            ["/map?found=1&land=1&floor=17&card=all"] = "78a433461078ed72a0c5644af5d0f80c",
            ["/map?freight=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?frontdoor=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?goodscar=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?kaamos=all"] = "b497a93382ab2df7ff8638048a79f6e2",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "b7e640c580815bb9c7f4e73ccda393a4",
            ["/map?kaamos=hq&land=1"] = "b7e640c580815bb9c7f4e73ccda393a4",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "d2925231b936a795723915a7b286871c",
            ["/map?nebula=all"] = "51c3de696913da7a53598def39b484bd",
            ["/map?oldcrew=1"] = "f28e593c4e185d5bf76de005b9f3b805",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "a6547abcd4389bea94e4de74ca93b634",
            ["/map?park=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?park=1&spread=1"] = "21d1c2897d1445690a5aa09b1031a37c",
            ["/map?parkback=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?parkwalk=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?patrol=2"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "bc8d7bec0f17a43d11fb0e6269f6ddd0",
            ["/map?ringoffice=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?rip=1"] = "21d1c2897d1445690a5aa09b1031a37c",
            ["/map?scenario=..%2Foops"] = "a6547abcd4389bea94e4de74ca93b634",
            ["/map?scenario=sol-eu"] = "b3be984527aaf03fe8b8bf28ff0d0060",
            ["/map?secretlab=1"] = "78a433461078ed72a0c5644af5d0f80c",
            ["/map?secretlab=deep&land=1&card=next"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?secretlab=deep&land=1&floor=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "3642ff52160278c32d3ee08cca492f6f",
            // #841 · ?perf=1 is read where the DeckView is built, not into BootQuery — it changes nothing
            // the parse answers, and this row says exactly that.
            ["/map?secretlab=deep&land=1&floor=1&perf=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?secretlab=deep&land=1&floor=21"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?skim=saturn"] = "0c7405acf13d0839ee40681d8ca1c3d8",
            ["/map?sling=jupiter"] = "a40d37afee9d18a0adf448ea96521519",
            ["/map?spread=1"] = "21d1c2897d1445690a5aa09b1031a37c",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "a6547abcd4389bea94e4de74ca93b634",
            ["/map?start=wreck&fetch=active"] = "59b2699001e9455c9d2ceb3fbca0897e",
            ["/map?stool=1&neighbour=0"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?stool=1&neighbour=1"] = "3642ff52160278c32d3ee08cca492f6f",
            ["/map?tablescene=free&approach=1"] = "21d1c2897d1445690a5aa09b1031a37c",
            ["/map?tablescene=free&watch=5&approach=0"] = "21d1c2897d1445690a5aa09b1031a37c",
            ["/map?threads=1"] = "21d1c2897d1445690a5aa09b1031a37c",
            ["/map?threads=1&watch=5"] = "21d1c2897d1445690a5aa09b1031a37c",
            ["/map?wreck=drivefailure&land=1"] = "5fb4da6ba52cd74b7c79beb57c02f094",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "973fbd106e15b62aa6093846afd22147",
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
        // being answered somewhere this guard cannot see it. …and one more since (#973 L5a's ?oldcrew=,
        // the dev door onto the old crew's scene), which is what a key ADDED to the chain is supposed to
        // look like here: the number moves, deliberately, in the same commit as the key.
        Assert.Equal(31, TheQuery("/map").GetType()
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
