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
/// <para><b>#619 re-pinned every value a sixth time, for the sixth time for the same reason.</b> The dev
/// door onto the refuge that failed (<c>?secretlab=sealed</c>) is the THIRTY-SEVENTH field on the holder, so
/// <c>SecretlabSealed = False</c> joins the text for every URL and all 88 digests move — plus one row that
/// is an ADDITION rather than a change, the new URL's own. Dumped and diffed: <b>88 moved, 0 gone, 1
/// new</b>, and not a value typed by hand. The proof that nothing BEHAVED differently is the world sweep
/// next door, where exactly one line moved and it was that same addition.</para>
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
            ["/map"] = "52e087ceed4f5af4e96464062315351f",
            ["/map?archive=1&land=1&nerve=2"] = "ba4499a2f22beb55d917f956faa581e7",
            ["/map?ashore=1&kaamos=bounce"] = "e542090855d9fb9bbe2688bacca7e24a",
            ["/map?ashore=1&start=space-bar"] = "4559d12a95747c2ef6c021f3f004a680",
            ["/map?badge=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?barcase=1"] = "d2bbe08f0f3de0c10a853e58f444370b",
            ["/map?bond=1"] = "6e721cb552a772170eeede83590c4ec9",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "c9ef6763355bc234fd1517f4f036b1db",
            ["/map?converge=1"] = "4458705a3dec139f52b7c5e67f221f6b",
            ["/map?counter=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?counter=1&watch=2"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?counter=1&watch=5"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?credits=1234&fuel=7&simhours=9"] = "405fe98752f7e5529d35f3b2396388eb",
            ["/map?credits=50000"] = "10692c4890f763eb84eb69a04e8d0ad5",
            // #1066 · the meeting's door. One new ROW and no moved ones: CrewCheat was already the 34th
            // field on the holder (#663 put it there), so every other URL's rendering is untouched and only
            // the value this key answers is new. It differs from ?crew=petition below by exactly that value,
            // which is the whole of what this file measures.
            ["/map?crew=meeting"] = "e580e011accc5528551dadc323c79bbc",
            ["/map?crew=petition"] = "ff816bd4363e2df0e16df2fce05cda16",
            ["/map?death=collector&dock=selene-gate"] = "a1aa1199c17a7b43ac7529a5f5d0da61",
            ["/map?death=impact"] = "e29c98a5105cfc9925817c8b9765bbb5",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "73bcd957d16501484b24e240286d2420",
            ["/map?deflection=1"] = "542c7dcb67e7670a5b1d95428c3a5fe1",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "c41ce8ddfa86a8daacac4c6c94ceebfe",
            ["/map?designate=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "29417baaf6958b43ce869bb826a47464",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "409a7504e3cc9e5b5468011932310a92",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "4a8106737d2f5f772d3b3272a2b9960e",
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "956da45aa96211d64ba18fd7b446bbaf",
            ["/map?dock=the-space-bar"] = "e9f5da3cc8e4a0b409f0cf48d9e8a00a",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "e9f5da3cc8e4a0b409f0cf48d9e8a00a",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "e9f5da3cc8e4a0b409f0cf48d9e8a00a",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "e9f5da3cc8e4a0b409f0cf48d9e8a00a",
            ["/map?dock=the-tilt&site=0"] = "0cee53f6d133b84ec0f1093a80b679ba",
            ["/map?dock=the-tilt&site=0&land=1"] = "0cee53f6d133b84ec0f1093a80b679ba",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "bcf237a9144aeab7440d388faa9e7c64",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "0cee53f6d133b84ec0f1093a80b679ba",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "0cee53f6d133b84ec0f1093a80b679ba",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "0cee53f6d133b84ec0f1093a80b679ba",
            ["/map?dock=the-tilt&site=1"] = "0cee53f6d133b84ec0f1093a80b679ba",
            ["/map?dock=the-tilt&start=space-bar"] = "d8b673e7f83385771eb0b76138725aa0",
            ["/map?expedition=mining"] = "46514c9dcfc9c9fe28d1c7ff795393d5",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "9559c2c61e6eb0aedec825db0707bb0f",
            ["/map?found=1&land=1"] = "0583206bff1f8c5e620e03f1c1bd7ad6",
            ["/map?found=1&land=1&floor=17&card=all"] = "0583206bff1f8c5e620e03f1c1bd7ad6",
            ["/map?freight=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?frontdoor=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?goodscar=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?kaamos=all"] = "8ce25e78496bf96d6350ffc640d95dfb",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "249a212780e57f0ce10be96626091db1",
            ["/map?kaamos=hq&land=1"] = "249a212780e57f0ce10be96626091db1",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "5f3e90b8311d1d150b87c3ec27ff78cc",
            ["/map?nebula=all"] = "047e0a2b1b29eb561f4e9464083f2d43",
            ["/map?nopattern=1&death=impact"] = "82cf5c228b51dd1f60e433624b3040a2",
            ["/map?oldcrew=1"] = "fbc46e4adff4b88ea33a2c069f708297",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "95a07cd4725fe16f76ca34bdc0d45896",
            ["/map?park=1"] = "2959f0b2231eeef1f542bba234294285",
            // #759 · …and both of them read EXACTLY what ?park=1 reads, which is the honest answer and worth
            // the row rather than an exemption: `?parkphase=` writes two fields on the PAGE (which phase was
            // asked for, and whether the morning door was), and not one of the thirty BootQuery fields this
            // file renders. The park's clock is jumped where the site is known, long after the parse.
            ["/map?park=1&parkphase=morning"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?park=1&parkphase=night"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?park=1&spread=1"] = "925858d50317f8efc6e0307d6827441b",
            ["/map?parkback=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?parkwalk=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?patrol=2"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "5bed083becc16076f4f0c331ee9b1604",
            ["/map?ringoffice=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?rip=1"] = "925858d50317f8efc6e0307d6827441b",
            ["/map?scenario=..%2Foops"] = "52e087ceed4f5af4e96464062315351f",
            ["/map?scenario=sol-eu"] = "17827c78e1cdfe86950157dbca4b74cc",
            ["/map?secretlab=1"] = "0583206bff1f8c5e620e03f1c1bd7ad6",
            ["/map?secretlab=deep&land=1&card=next"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?secretlab=deep&land=1&floor=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "2959f0b2231eeef1f542bba234294285",
            // #841 · ?perf=1 is read where the DeckView is built, not into BootQuery — it changes nothing
            // the parse answers, and this row says exactly that.
            ["/map?secretlab=deep&land=1&floor=1&perf=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?secretlab=deep&land=1&floor=21"] = "2959f0b2231eeef1f542bba234294285",
            // #619 · the fourth cheat rock's own row — an ADDITION, not a change.
            ["/map?secretlab=sealed&land=1&floor=3"] = "6ee5f902949d053f0eefbb9e6bf5b761",
            ["/map?skim=saturn"] = "76a0bf37d83a930f22626d14248f663e",
            ["/map?sling=jupiter"] = "e35aa4c478b8008f9a986af12c26dc37",
            ["/map?spread=1"] = "925858d50317f8efc6e0307d6827441b",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "95a07cd4725fe16f76ca34bdc0d45896",
            ["/map?start=wreck&fetch=active"] = "15e044fda3925b36837ae18f70704f6c",
            ["/map?start=wreck&dest=saturn"] = "a24ecc604e0d57a4a4ca679d64e92d76",
            ["/map?start=wreck&target=collector"] = "ddc3e67758bd564335c329b841a4f220",
            ["/map?stool=1&neighbour=0"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?stool=1&neighbour=1"] = "2959f0b2231eeef1f542bba234294285",
            ["/map?tablescene=free&approach=1"] = "925858d50317f8efc6e0307d6827441b",
            // #973 L2 · …and the rep row hashes the SAME, which is correct and worth saying: this sweep
            // renders BootQuery's own public fields, and neither ?approach= nor ?rep= lives there — both are
            // read straight onto the page (_approachCheat, _repCheat). The query object really is identical;
            // what the two URLs build differently is pinned next door, in TheBootBuildsTheSameWorldTests.
            ["/map?tablescene=free&rep=1&approach=0"] = "925858d50317f8efc6e0307d6827441b",
            ["/map?tablescene=free&watch=5&approach=0"] = "925858d50317f8efc6e0307d6827441b",
            ["/map?threads=1"] = "925858d50317f8efc6e0307d6827441b",
            ["/map?threads=1&watch=5"] = "925858d50317f8efc6e0307d6827441b",
            ["/map?wreck=drivefailure&land=1"] = "3e6d2ac6492c13f6224efa37b6d9ce62",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "b06c7e99d66e7c8a48082fe9aebe6bf8",
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
        // …and one more again (#619's SecretlabSealed, the fourth cheat rock: the refuge that failed is on
        // one floor of one site in four and none of the other three rocks happens to carry one, so the beat
        // had no URL at all. The number moves in the same commit as the key, which is what this row is for).
        Assert.Equal(37, TheQuery("/map").GetType()
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
