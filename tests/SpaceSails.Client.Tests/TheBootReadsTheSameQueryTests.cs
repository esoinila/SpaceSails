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
/// <para><b>#640 re-pinned every value a fourth time, for the fourth time for the same reason.</b> The dev
/// door onto the death where nobody comes (<c>?nopattern=1</c>) is the THIRTY-FIFTH field on the holder, so
/// <c>NoPatternCheat = False</c> joins the text for every URL and every digest moves — plus one row that is
/// an ADDITION rather than a change, the new URL's own. The values below are the failing run's own output,
/// lifted from its <c>read</c> lines and never typed by hand. The proof that nothing BEHAVED differently is
/// the world sweep next door, where exactly one line moved and it was that addition.</para>
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
            ["/map"] = "fd148a7b9398a8e099041739d88d88b0",
            ["/map?archive=1&land=1&nerve=2"] = "25bb3846ab69167ee7f9da7d6891af71",
            ["/map?ashore=1&kaamos=bounce"] = "b0ee854d2fbe6ee1a328127a249bb66a",
            ["/map?ashore=1&start=space-bar"] = "a39182d6fb48e2841c3625f5ed093082",
            ["/map?badge=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?barcase=1"] = "29d5b4145ec8ac093064bfd81c217696",
            ["/map?bond=1"] = "020ac4eb8ffe774a273af8bb429be3bd",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "bc776f00ea6da687569246b5b6559b1d",
            ["/map?converge=1"] = "b43247a87fa911c8270bdb3d2581aa22",
            ["/map?counter=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?counter=1&watch=2"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?counter=1&watch=5"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?credits=1234&fuel=7&simhours=9"] = "9f5282c8ca374138f1ce64af8a09da57",
            ["/map?credits=50000"] = "5be7669ccc5bfe6a54c1785c5fc8f1ae",
            // #1066 · the meeting's door. One new ROW and no moved ones: CrewCheat was already the 34th
            // field on the holder (#663 put it there), so every other URL's rendering is untouched and only
            // the value this key answers is new. It differs from ?crew=petition below by exactly that value,
            // which is the whole of what this file measures.
            ["/map?crew=meeting"] = "d3b77f45d14af401e2e71d3b49bcd99f",
            ["/map?crew=petition"] = "63cea96829be81a28ccb36c09bb79996",
            ["/map?death=collector&dock=selene-gate"] = "4f1754ed3af38141514bc396ce82832f",
            ["/map?death=impact"] = "6935c21edfd63f6100caf0df3be971d5",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "aab3827cbaa7db8dcbd8b36061d53aae",
            ["/map?deflection=1"] = "d0d87539c0ff6ef40d22a6c405fe1d56",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "eb6cbab9939c79810f1804902aa0f3d8",
            ["/map?designate=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "90295ca04292e07dec10caaced3f7ac7",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "8980aab36797bdd34d045e366c8a8aa8",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "097403adeceda163a8ee57517bcd83e5",
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "bf17c95e38e1c7535177e1d68dd90da9",
            ["/map?dock=the-space-bar"] = "66045eeae7ff3bb4ecbf384932a99abd",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "66045eeae7ff3bb4ecbf384932a99abd",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "66045eeae7ff3bb4ecbf384932a99abd",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "66045eeae7ff3bb4ecbf384932a99abd",
            ["/map?dock=the-tilt&site=0"] = "bc2f0f74590c9e792c7094117d216801",
            ["/map?dock=the-tilt&site=0&land=1"] = "bc2f0f74590c9e792c7094117d216801",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "94cdf84df0b4d4bb36595ef819aa66b8",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "bc2f0f74590c9e792c7094117d216801",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "bc2f0f74590c9e792c7094117d216801",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "bc2f0f74590c9e792c7094117d216801",
            ["/map?dock=the-tilt&site=1"] = "bc2f0f74590c9e792c7094117d216801",
            ["/map?dock=the-tilt&start=space-bar"] = "f000909a759723d6851ba5592f93ec3e",
            ["/map?expedition=mining"] = "11a21c5d2df8db73ff31cc54dba7d2f7",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "2f1951f2c34becac9d8db6da3ca01d7a",
            ["/map?found=1&land=1"] = "21515744e1bce710dc9520a45daa9767",
            ["/map?found=1&land=1&floor=17&card=all"] = "21515744e1bce710dc9520a45daa9767",
            ["/map?freight=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?frontdoor=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?goodscar=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?kaamos=all"] = "585a3a8fef3d85aab37882940f40fd2f",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "0eb6b12baf29ef56bcd3024098a281d3",
            ["/map?kaamos=hq&land=1"] = "0eb6b12baf29ef56bcd3024098a281d3",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "57e606175c4660dd345880d1617b2047",
            ["/map?nebula=all"] = "b2ebea1e14bc3d254d7ab05104499437",
            ["/map?nopattern=1&death=impact"] = "96f4eb2f66a6e99283ec64964159a1d1",
            ["/map?oldcrew=1"] = "b9691b512a29cb915a96656f49d03369",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "fd148a7b9398a8e099041739d88d88b0",
            ["/map?park=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?park=1&spread=1"] = "9f8da805410c2e720f2b91f66c8dd3b2",
            ["/map?parkback=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?parkwalk=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?patrol=2"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "81153c87256897edea90451ad52be90d",
            ["/map?ringoffice=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?rip=1"] = "9f8da805410c2e720f2b91f66c8dd3b2",
            ["/map?scenario=..%2Foops"] = "fd148a7b9398a8e099041739d88d88b0",
            ["/map?scenario=sol-eu"] = "a78324f793efc6939b607c9657c902e9",
            ["/map?secretlab=1"] = "21515744e1bce710dc9520a45daa9767",
            ["/map?secretlab=deep&land=1&card=next"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?secretlab=deep&land=1&floor=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            // #841 · ?perf=1 is read where the DeckView is built, not into BootQuery — it changes nothing
            // the parse answers, and this row says exactly that.
            ["/map?secretlab=deep&land=1&floor=1&perf=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?secretlab=deep&land=1&floor=21"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?skim=saturn"] = "4f98da74e1e65dd9f45c6ad09fce786d",
            ["/map?sling=jupiter"] = "8b9d8fc67f6e0df3e6553535ea2d64d0",
            ["/map?spread=1"] = "9f8da805410c2e720f2b91f66c8dd3b2",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "fd148a7b9398a8e099041739d88d88b0",
            ["/map?start=wreck&fetch=active"] = "3c4499e22e0700394e82e166bdc2d715",
            ["/map?start=wreck&dest=saturn"] = "ad626cc4881b34af349c593181afb8c0",
            ["/map?start=wreck&target=collector"] = "e754d0bb30b0630e6df60cc5fc9a878d",
            ["/map?stool=1&neighbour=0"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?stool=1&neighbour=1"] = "f2f1f4dc2eabc32e963942e7998c77ff",
            ["/map?tablescene=free&approach=1"] = "9f8da805410c2e720f2b91f66c8dd3b2",
            // #973 L2 · …and the rep row hashes the SAME, which is correct and worth saying: this sweep
            // renders BootQuery's own public fields, and neither ?approach= nor ?rep= lives there — both are
            // read straight onto the page (_approachCheat, _repCheat). The query object really is identical;
            // what the two URLs build differently is pinned next door, in TheBootBuildsTheSameWorldTests.
            ["/map?tablescene=free&rep=1&approach=0"] = "9f8da805410c2e720f2b91f66c8dd3b2",
            ["/map?tablescene=free&watch=5&approach=0"] = "9f8da805410c2e720f2b91f66c8dd3b2",
            ["/map?threads=1"] = "9f8da805410c2e720f2b91f66c8dd3b2",
            ["/map?threads=1&watch=5"] = "9f8da805410c2e720f2b91f66c8dd3b2",
            ["/map?wreck=drivefailure&land=1"] = "c9c612cb1712dbfa9c2c1c016b49cde9",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "e3716c5808054a61def28fbe72dbcbef",
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
        // …and one more again (#640's ?nopattern=1, the dev door onto the death where nobody comes: a
        // 1-in-20 resident behind a 1-in-3 hull behind a throw the captain pays nerve for, and THEN a
        // death — most of a career for one card).
        Assert.Equal(35, TheQuery("/map").GetType()
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
