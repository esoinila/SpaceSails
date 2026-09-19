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
/// <para><b>#323 re-pinned every value a fifth time, for the fifth time for the same reason.</b> The front
/// door's own question (<c>AskedForASituation</c> — did this URL ask the boot for anything but a sky?) is the
/// THIRTY-SIXTH field on the holder, this rendering walks every public field of it, and so all 88 digests
/// move. The values below are the failing run's own output, dumped and diffed, never typed by hand.</para>
///
/// <para><b>…and one collision SPLIT, which is the one interesting line in the re-pin.</b>
/// <c>/map?start=&amp;dock=&amp;fuel=&amp;nerve=&amp;site=&amp;land=</c> — every cheat key handed nothing —
/// used to read identically to the bare front door, because none of those empty values survives its reader's
/// own validation. It no longer does: the readers CLAIMED those pairs, so the URL asked for a bench boot even
/// though it asked badly, and it now boots straight in where it used to end at the picker. That is the rule
/// being honest about a URL nobody types rather than about one the game ships, and it is stated here because
/// a re-pin that silently merged or split a group is exactly the kind of thing this file exists to surface.
/// The distinct count rises from 45 to 46 accordingly.</para>
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
            ["/map"] = "3bd997d69e53e10eeb4cf6d38a7927f0",
            ["/map?archive=1&land=1&nerve=2"] = "1248a0afa4b77f4bba5b67c573d75c90",
            ["/map?ashore=1&kaamos=bounce"] = "a2e58da5dc724d488d6ddd3fa09e2592",
            ["/map?ashore=1&start=space-bar"] = "49631916dc2a7c1002a0e94496c56ffa",
            ["/map?badge=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?barcase=1"] = "3559dc666d0c65a6615007fc31d8f255",
            ["/map?bond=1"] = "f344e83e5bf8f8885978137292a8e37a",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "a9a792e8f07e4ff16d85f68859ad6e2f",
            ["/map?converge=1"] = "70ef44cfd2a526380d6cd7ed496b5251",
            ["/map?counter=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?counter=1&watch=2"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?counter=1&watch=5"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?credits=1234&fuel=7&simhours=9"] = "71470bd69e489a64cde7c2e44ba7b860",
            ["/map?credits=50000"] = "ee5b0c76e0a4dc8bbd9838ac32396f52",
            // #1066 · the meeting's door. One new ROW and no moved ones: CrewCheat was already the 34th
            // field on the holder (#663 put it there), so every other URL's rendering is untouched and only
            // the value this key answers is new. It differs from ?crew=petition below by exactly that value,
            // which is the whole of what this file measures.
            ["/map?crew=meeting"] = "73658003cebfe4002f5a1ed6b26b19ca",
            ["/map?crew=petition"] = "f77020effcfefe4316ddb7e1e2b58351",
            ["/map?death=collector&dock=selene-gate"] = "855d08c148151b544719cf942239b061",
            ["/map?death=impact"] = "8bd3d11f912ce49eee92567c09f9a296",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "cb7745d8338672418af75baba67c61ca",
            ["/map?deflection=1"] = "5ce73ad668e959fa8d80517b3384ffed",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "1fd1a6b5c4e419e0d3160172a00308b2",
            ["/map?designate=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "68081c7318f0dda25007f9c81b15a80d",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "25d4f55f36f341187a57e381bf646e4f",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "40b1cd7b316e165e4b40ef38acd77686",
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "117149c07f30981b3393cb75a8443a7f",
            ["/map?dock=the-space-bar"] = "cc185bab896a19e6fec7e888425311d0",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "cc185bab896a19e6fec7e888425311d0",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "cc185bab896a19e6fec7e888425311d0",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "cc185bab896a19e6fec7e888425311d0",
            ["/map?dock=the-tilt&site=0"] = "97879c663ba0aa4ddc8db65f1aef8d50",
            ["/map?dock=the-tilt&site=0&land=1"] = "97879c663ba0aa4ddc8db65f1aef8d50",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "a5e63f618879de4d0624c2285a25d797",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "97879c663ba0aa4ddc8db65f1aef8d50",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "97879c663ba0aa4ddc8db65f1aef8d50",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "97879c663ba0aa4ddc8db65f1aef8d50",
            ["/map?dock=the-tilt&site=1"] = "97879c663ba0aa4ddc8db65f1aef8d50",
            ["/map?dock=the-tilt&start=space-bar"] = "79fa9e3c6bb53423938587c070c2706a",
            ["/map?expedition=mining"] = "e52c9025cb7e230a06845740067db193",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "9a6e0883d3066acbffa061236ee401d5",
            ["/map?found=1&land=1"] = "1307005ec734f2005ba74a9003307ccf",
            ["/map?found=1&land=1&floor=17&card=all"] = "1307005ec734f2005ba74a9003307ccf",
            ["/map?freight=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?frontdoor=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?goodscar=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?kaamos=all"] = "94f6d95ce2d87b43c3d125b1a5b793a5",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "ce898c7bed97ec75119f3d6a98d4eb16",
            ["/map?kaamos=hq&land=1"] = "ce898c7bed97ec75119f3d6a98d4eb16",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "a2714737ed9cafb52d4b3af67f1ad7e7",
            ["/map?nebula=all"] = "839504527e97d8ff87576aa20e7e7bab",
            ["/map?nopattern=1&death=impact"] = "ae817cd67206c51ee4e34c2c9162fe09",
            ["/map?oldcrew=1"] = "16b3d410cd8bf795db6a989b842e7699",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "0c88523099e0f21dca75072389e2491c",
            ["/map?park=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            // #759 · …and both of them read EXACTLY what ?park=1 reads, which is the honest answer and worth
            // the row rather than an exemption: `?parkphase=` writes two fields on the PAGE (which phase was
            // asked for, and whether the morning door was), and not one of the thirty BootQuery fields this
            // file renders. The park's clock is jumped where the site is known, long after the parse.
            ["/map?park=1&parkphase=morning"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?park=1&parkphase=night"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?park=1&spread=1"] = "ff737ae79ff83a452eee7c9eb1b57021",
            ["/map?parkback=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?parkwalk=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?patrol=2"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "00dd7f609ec177dcd134964a68a1d6ad",
            ["/map?ringoffice=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?rip=1"] = "ff737ae79ff83a452eee7c9eb1b57021",
            ["/map?scenario=..%2Foops"] = "3bd997d69e53e10eeb4cf6d38a7927f0",
            ["/map?scenario=sol-eu"] = "fd06b1887ad998a3a11eaac02c7fe812",
            ["/map?secretlab=1"] = "1307005ec734f2005ba74a9003307ccf",
            ["/map?secretlab=deep&land=1&card=next"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?secretlab=deep&land=1&floor=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            // #841 · ?perf=1 is read where the DeckView is built, not into BootQuery — it changes nothing
            // the parse answers, and this row says exactly that.
            ["/map?secretlab=deep&land=1&floor=1&perf=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?secretlab=deep&land=1&floor=21"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?skim=saturn"] = "a7c6f36cd6d58947e2662d995f1b9318",
            ["/map?sling=jupiter"] = "5c43eb6bc8700dff0a67feceb613111a",
            ["/map?spread=1"] = "ff737ae79ff83a452eee7c9eb1b57021",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "0c88523099e0f21dca75072389e2491c",
            ["/map?start=wreck&fetch=active"] = "95b251debcf7d505b1b8ed40946e1e8a",
            ["/map?start=wreck&dest=saturn"] = "6b12854ed84b8972ce99e0a499da89b9",
            ["/map?start=wreck&target=collector"] = "60c7139a56fdab5063adfba260ff2952",
            ["/map?stool=1&neighbour=0"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?stool=1&neighbour=1"] = "db34e4ae4255e1b388d1cc5c010e9d6d",
            ["/map?tablescene=free&approach=1"] = "ff737ae79ff83a452eee7c9eb1b57021",
            // #973 L2 · …and the rep row hashes the SAME, which is correct and worth saying: this sweep
            // renders BootQuery's own public fields, and neither ?approach= nor ?rep= lives there — both are
            // read straight onto the page (_approachCheat, _repCheat). The query object really is identical;
            // what the two URLs build differently is pinned next door, in TheBootBuildsTheSameWorldTests.
            ["/map?tablescene=free&rep=1&approach=0"] = "ff737ae79ff83a452eee7c9eb1b57021",
            ["/map?tablescene=free&watch=5&approach=0"] = "ff737ae79ff83a452eee7c9eb1b57021",
            ["/map?threads=1"] = "ff737ae79ff83a452eee7c9eb1b57021",
            ["/map?threads=1&watch=5"] = "ff737ae79ff83a452eee7c9eb1b57021",
            ["/map?wreck=drivefailure&land=1"] = "0c67c91fe6345be2363555a3e95e6a43",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "973d2388f93d62ee60e0f9f67b1de517",
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
        // …and one more again (#323's AskedForASituation, which is not a cheat but THE question the front
        // door asks — and it belongs on the holder for the same reason the thirty-five above do: it is
        // something the parse ANSWERS, and a boolean kept anywhere else would be a second source for the one
        // fact that decides whether a captain is offered their saves).
        Assert.Equal(36, TheQuery("/map").GetType()
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

        var uri = new Uri("http://localhost" + url);
        object q = Call(map, "ReadEveryQueryKey", uri)!;
        Call(map, "DefaultABerthForTheCheatsThatNeedOne", q);

        // #323 · The door's raise reads BootQuery.AskedForASituation, which the reader chain above has
        // already answered. It is still called here for the reason it always was: this recorder stands
        // exactly where the old one-method boot's recorder stood, and a stage quietly dropped from this
        // chain is a stage whose effect on the parse nobody would notice.
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
