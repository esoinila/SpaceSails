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
/// <para><b>#997 wave 10 re-pinned every value again, and for exactly the same reason.</b> The dev door onto
/// the target dossier (<c>?target=</c>) is the THIRTY-SECOND field on the holder, this rendering walks every
/// public field of it, and so <c>TargetCheat = null</c> joins the text for all eighty-one URLs and all
/// eighty-one digests move. That is #975's lesson said a second time: a new BootQuery field moves every
/// digest in this file, and the values below are the failing run's own output, dumped and diffed, never
/// typed by hand. The proof that nothing BEHAVED differently is the world sweep next door, where exactly one
/// line moved — the new URL's, and it was an addition rather than a change.</para>
///
/// <para><b>#663 re-pinned every value a third time, for the third time for the same reason.</b> The dev
/// door onto the crew's deputation (<c>?crew=petition</c>) is the THIRTY-FOURTH field on the holder, so
/// <c>CrewCheat = null</c> joins the text for every URL and every digest moves — plus one row that is an
/// ADDITION rather than a change, the new URL's own. The values below are the failing run's own output,
/// lifted from its <c>read</c> lines and never typed by hand. The proof that nothing BEHAVED differently is
/// the world sweep next door, where exactly one line moved and it was that addition: <c>?crew=</c> grants
/// two counters on the crew sheet long after the gate that sweep stops at, so it builds the front door's
/// world to the byte.</para>
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
            ["/map"] = "cf9a2c35accf8f83063f7aeec7d43ff2",
            ["/map?archive=1&land=1&nerve=2"] = "52a599bce86088a55eeb4b91e44e1283",
            ["/map?ashore=1&kaamos=bounce"] = "d833daa9b419be15a4173a7b026557f8",
            ["/map?ashore=1&start=space-bar"] = "655d07da2c376f2eba9402030e03618d",
            ["/map?badge=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?barcase=1"] = "78c12331dc4516c0b68cde9940773ad3",
            ["/map?bond=1"] = "2313968ba450774bf5c738649b607bcd",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "e2cd58343608445fa8f93c06d50808d4",
            ["/map?converge=1"] = "d3fd54213530552cc96de800a9fb9e5f",
            ["/map?counter=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?counter=1&watch=2"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?counter=1&watch=5"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?credits=1234&fuel=7&simhours=9"] = "f45272cfecb6d77059b0e9998bafe2c2",
            ["/map?credits=50000"] = "20eccdc7a590cb759c33b62f903e5a56",
            // #1066 · the meeting's door. One new ROW and no moved ones: CrewCheat was already the 34th
            // field on the holder (#663 put it there), so every other URL's rendering is untouched and only
            // the value this key answers is new. It differs from ?crew=petition below by exactly that value,
            // which is the whole of what this file measures.
            ["/map?crew=meeting"] = "c65ee9a8f9755e37f23e4f5d217f500b",
            ["/map?crew=petition"] = "bf1842abb0822a4cc70009b135f2fa5b",
            ["/map?death=collector&dock=selene-gate"] = "ee3391b9363b93cc435540908991739d",
            ["/map?death=impact"] = "7cf26d3bf542170bdd3415fd0bd411bf",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "7e1b69ef1785b63dda77edf5aba1b05b",
            ["/map?deflection=1"] = "fd66b850f1a7c9ef1aaaba83e36ad992",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "fb745dbdf89d38a1a9fcae42d92ac33b",
            ["/map?designate=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "317d2fc91fa32103e9b601c6a734122d",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "93551e17ee1ff4600e04ff7e7e264fe7",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "425257f40a7dec9ef9161d0f468e7560",
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "0def363232ebd1a9c6dd07295c3685d2",
            ["/map?dock=the-space-bar"] = "009a18a3e9957c78a01112d811705069",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "009a18a3e9957c78a01112d811705069",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "009a18a3e9957c78a01112d811705069",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "009a18a3e9957c78a01112d811705069",
            ["/map?dock=the-tilt&site=0"] = "71cb2f03ddf7f11a8f82a881025ac6e2",
            ["/map?dock=the-tilt&site=0&land=1"] = "71cb2f03ddf7f11a8f82a881025ac6e2",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "d70164c907b90d6ca25e4604c2e722de",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "71cb2f03ddf7f11a8f82a881025ac6e2",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "71cb2f03ddf7f11a8f82a881025ac6e2",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "71cb2f03ddf7f11a8f82a881025ac6e2",
            ["/map?dock=the-tilt&site=1"] = "71cb2f03ddf7f11a8f82a881025ac6e2",
            ["/map?dock=the-tilt&start=space-bar"] = "98f1ac166e75bd2b8dff512f7a5683c8",
            ["/map?expedition=mining"] = "a177b0b9707fff0e2535f14a53b42ee9",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "1d116572b3c6b99fb37e45090b3cc12e",
            ["/map?found=1&land=1"] = "da92e4000fbf18c0f79f74e3e3b3b908",
            ["/map?found=1&land=1&floor=17&card=all"] = "da92e4000fbf18c0f79f74e3e3b3b908",
            ["/map?freight=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?frontdoor=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?goodscar=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?kaamos=all"] = "5543fac0be78e9c163950c705b01e6c3",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "9de5a89d365ed045691d41435301607c",
            ["/map?kaamos=hq&land=1"] = "9de5a89d365ed045691d41435301607c",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "f78aed09ac72b5f2622375cf51ee24f2",
            ["/map?nebula=all"] = "8fe3542da99e6d0bc2ecf72c30216d13",
            ["/map?oldcrew=1"] = "b6534bcb39df612577f2dc476eb1bbb0",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "cf9a2c35accf8f83063f7aeec7d43ff2",
            ["/map?park=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?park=1&spread=1"] = "f5fde16b447716d1f6915ff4ce809603",
            ["/map?parkback=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?parkwalk=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?patrol=2"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "79aeaa80ae31c14d00b79e8d1e427fb2",
            ["/map?ringoffice=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?rip=1"] = "f5fde16b447716d1f6915ff4ce809603",
            ["/map?scenario=..%2Foops"] = "cf9a2c35accf8f83063f7aeec7d43ff2",
            ["/map?scenario=sol-eu"] = "34058c6fd0d73164aa864a323706cf29",
            ["/map?secretlab=1"] = "da92e4000fbf18c0f79f74e3e3b3b908",
            ["/map?secretlab=deep&land=1&card=next"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?secretlab=deep&land=1&floor=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "91afeecd815de1d882847c2baf39c65a",
            // #841 · ?perf=1 is read where the DeckView is built, not into BootQuery — it changes nothing
            // the parse answers, and this row says exactly that.
            ["/map?secretlab=deep&land=1&floor=1&perf=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?secretlab=deep&land=1&floor=21"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?skim=saturn"] = "002c918b806821040739fb5e5a3558a8",
            ["/map?sling=jupiter"] = "ee17f68e02685bb8c5e05ee208f8cac9",
            ["/map?spread=1"] = "f5fde16b447716d1f6915ff4ce809603",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "cf9a2c35accf8f83063f7aeec7d43ff2",
            ["/map?start=wreck&fetch=active"] = "e394398d112cbc585956c2b16c804e66",
            ["/map?start=wreck&dest=saturn"] = "0530b1d19c4ef23960894b7e498b48f6",
            ["/map?start=wreck&target=collector"] = "36a6bd60acecf2921d80a6f4ce63609e",
            ["/map?stool=1&neighbour=0"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?stool=1&neighbour=1"] = "91afeecd815de1d882847c2baf39c65a",
            ["/map?tablescene=free&approach=1"] = "f5fde16b447716d1f6915ff4ce809603",
            // #973 L2 · …and the rep row hashes the SAME, which is correct and worth saying: this sweep
            // renders BootQuery's own public fields, and neither ?approach= nor ?rep= lives there — both are
            // read straight onto the page (_approachCheat, _repCheat). The query object really is identical;
            // what the two URLs build differently is pinned next door, in TheBootBuildsTheSameWorldTests.
            ["/map?tablescene=free&rep=1&approach=0"] = "f5fde16b447716d1f6915ff4ce809603",
            ["/map?tablescene=free&watch=5&approach=0"] = "f5fde16b447716d1f6915ff4ce809603",
            ["/map?threads=1"] = "f5fde16b447716d1f6915ff4ce809603",
            ["/map?threads=1&watch=5"] = "f5fde16b447716d1f6915ff4ce809603",
            ["/map?wreck=drivefailure&land=1"] = "362f0d93a4e6d0dd539452e2d647b18e",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "e9ea7cc4dda0d9ddfc1504803e358cc0",
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
        // …and one more again (#997 wave 10's ?target=, the dev door onto the target dossier — the card
        // three waves of the shell migration could measure and not walk to).
        // …and one more again (#663's ?crew=petition, the dev door onto the deputation: the beat's own edge
        // is the crew's STANDING, which nothing short of a lost gig and a poor honest ship can cross).
        Assert.Equal(34, TheQuery("/map").GetType()
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
