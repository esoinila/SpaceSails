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
            ["/map"] = "654ebea7920753c81221785c3e1fb2b7",
            ["/map?archive=1&land=1&nerve=2"] = "10e7638a84f06e897c65da704e1fc3b3",
            ["/map?ashore=1&kaamos=bounce"] = "a029acf9056db7718e5137fddf5597ed",
            ["/map?ashore=1&start=space-bar"] = "bddb1a1f622aa03a90540e757bcfe810",
            ["/map?badge=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?barcase=1"] = "5cbe8a17ef8885e1855a962b84de7a60",
            ["/map?bond=1"] = "2cea7dc87181b6667328df646b2cb42f",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "49e983e26cb299dd3202e2b76242f37b",
            ["/map?converge=1"] = "f3f1e29ead13a326f19c960d7e0205ca",
            ["/map?counter=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?counter=1&watch=2"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?counter=1&watch=5"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?credits=1234&fuel=7&simhours=9"] = "1d49206639778a527da671dfcc298c06",
            ["/map?credits=50000"] = "cb168002876ab0c06ffaebf00294ab0b",
            ["/map?death=collector&dock=selene-gate"] = "0661699456f219093d4ad5e85a1b300c",
            ["/map?death=impact"] = "22a30b3248cd9e9677d27a95ec0fbd69",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "51df02fcf462b7a7f07e09f957e7c117",
            ["/map?deflection=1"] = "3e446424ff66911137cbcdaec991ee0f",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "1bcec65384b3e915d1be9299b6585e29",
            ["/map?designate=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "3ec47f3b346b5d681025e287098db8c1",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "bd47f4e566e5ae34e01588b6a31e6e93",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "944cef4b26ca8f19a19a328e6b1738d2",
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "6775ec3a3f5f953517c72c9a79964461",
            ["/map?dock=the-space-bar"] = "dbcfa41c4b4c3057071c88c45b44ba29",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "dbcfa41c4b4c3057071c88c45b44ba29",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "dbcfa41c4b4c3057071c88c45b44ba29",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "dbcfa41c4b4c3057071c88c45b44ba29",
            ["/map?dock=the-tilt&site=0"] = "b5a898ac71cb8f7fae5318f0712e056b",
            ["/map?dock=the-tilt&site=0&land=1"] = "b5a898ac71cb8f7fae5318f0712e056b",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "dbf94c1e62b987114f88dfb2845ddb04",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "b5a898ac71cb8f7fae5318f0712e056b",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "b5a898ac71cb8f7fae5318f0712e056b",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "b5a898ac71cb8f7fae5318f0712e056b",
            ["/map?dock=the-tilt&site=1"] = "b5a898ac71cb8f7fae5318f0712e056b",
            ["/map?dock=the-tilt&start=space-bar"] = "b79d696c7e7c5b3bdd6643be2e85c69b",
            ["/map?expedition=mining"] = "b0956407ee2623b92c22dc63537a5cf7",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "4ff009b12280adb6cf5b654f2e56db45",
            ["/map?found=1&land=1"] = "de17304bc4e12606336e061037ea39c7",
            ["/map?found=1&land=1&floor=17&card=all"] = "de17304bc4e12606336e061037ea39c7",
            ["/map?freight=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?frontdoor=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?goodscar=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?kaamos=all"] = "7396b7fb912454dd7655bda99b491974",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "3d536497e61c0179ffceefabe7eaf848",
            ["/map?kaamos=hq&land=1"] = "3d536497e61c0179ffceefabe7eaf848",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "e8b94d27d7b17ed76692fd4c07902f68",
            ["/map?nebula=all"] = "8e333f30801104c0686d56f5961b289e",
            ["/map?oldcrew=1"] = "318521077b514fbb8d705e61a6361e60",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "654ebea7920753c81221785c3e1fb2b7",
            ["/map?park=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?park=1&spread=1"] = "5edaae2a838e6d3f25ac4c5eb7b4f91a",
            ["/map?parkback=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?parkwalk=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?patrol=2"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "8228baad046312450313435e4ae6c91a",
            ["/map?ringoffice=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?rip=1"] = "5edaae2a838e6d3f25ac4c5eb7b4f91a",
            ["/map?scenario=..%2Foops"] = "654ebea7920753c81221785c3e1fb2b7",
            ["/map?scenario=sol-eu"] = "5be1490582b3f54c5b545334281765c0",
            ["/map?secretlab=1"] = "de17304bc4e12606336e061037ea39c7",
            ["/map?secretlab=deep&land=1&card=next"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?secretlab=deep&land=1&floor=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "92f4fac2680f0d77dbc6bd78334161da",
            // #841 · ?perf=1 is read where the DeckView is built, not into BootQuery — it changes nothing
            // the parse answers, and this row says exactly that.
            ["/map?secretlab=deep&land=1&floor=1&perf=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?secretlab=deep&land=1&floor=21"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?skim=saturn"] = "5ef6e4a7d966f40984183ae909509b4e",
            ["/map?sling=jupiter"] = "ad17f8fdcd2b6ace268cf77551be0b9c",
            ["/map?spread=1"] = "5edaae2a838e6d3f25ac4c5eb7b4f91a",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "654ebea7920753c81221785c3e1fb2b7",
            ["/map?start=wreck&fetch=active"] = "c3495b3c45bbcaee9ca7cf4dee51a06f",
            ["/map?start=wreck&dest=saturn"] = "f853dafbafb0ebc1cf893a3765ac3f85",
            ["/map?start=wreck&target=collector"] = "ee81deda9e68cb7fbe70296863c4a32b",
            ["/map?stool=1&neighbour=0"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?stool=1&neighbour=1"] = "92f4fac2680f0d77dbc6bd78334161da",
            ["/map?tablescene=free&approach=1"] = "5edaae2a838e6d3f25ac4c5eb7b4f91a",
            // #973 L2 · …and the rep row hashes the SAME, which is correct and worth saying: this sweep
            // renders BootQuery's own public fields, and neither ?approach= nor ?rep= lives there — both are
            // read straight onto the page (_approachCheat, _repCheat). The query object really is identical;
            // what the two URLs build differently is pinned next door, in TheBootBuildsTheSameWorldTests.
            ["/map?tablescene=free&rep=1&approach=0"] = "5edaae2a838e6d3f25ac4c5eb7b4f91a",
            ["/map?tablescene=free&watch=5&approach=0"] = "5edaae2a838e6d3f25ac4c5eb7b4f91a",
            ["/map?threads=1"] = "5edaae2a838e6d3f25ac4c5eb7b4f91a",
            ["/map?threads=1&watch=5"] = "5edaae2a838e6d3f25ac4c5eb7b4f91a",
            ["/map?wreck=drivefailure&land=1"] = "80b2bdd54cdba3361d516c265cf1d076",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "c859c75d0218ff796a4fb01678cfeb70",
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
        Assert.Equal(33, TheQuery("/map").GetType()
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
