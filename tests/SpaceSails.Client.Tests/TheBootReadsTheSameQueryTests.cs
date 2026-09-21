using System;
using System.Collections.Generic;
using System.IO;
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
/// <para><b>#1253 re-pinned every value a seventh time, for the seventh time for the same reason.</b> The
/// dev door onto DOWN BELOW (<c>?havenfloor=-1</c>) is the THIRTY-EIGHTH field on the holder, so
/// <c>HavenFloorCheat = null</c> joins the text for every URL and all 89 digests move — plus one row that is
/// an ADDITION rather than a change, the new URL's own. <b>And this time nothing was typed at all:</b> the
/// seventh hand-transcription of eighty-eight digests is the point at which this file grew the dump door its
/// neighbour has always had (<see cref="DumpVariable"/>), so the table below is a generated body and the
/// re-pin is a diff rather than a paste. #1055 made that argument about the ledgers; it is the same
/// argument.</para>
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
            ["/map"] = "19cb31de6be77f6e45fd408a5c766fb6",
            ["/map?archive=1&land=1&nerve=2"] = "19f02feb0821bdcf0a9247c7c10ee305",
            ["/map?ashore=1&kaamos=bounce"] = "c994cefe9f9262d18cf94c2535f1bf59",
            ["/map?ashore=1&start=space-bar"] = "f0c0bb3855c64e9cc501cb537c084c03",
            ["/map?badge=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?barcase=1"] = "cfea6ce69e045d413faa9edf77128752",
            ["/map?bond=1"] = "bbcc2f27c43a073d9bc3391b8f89d7f5",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "aec4869ed367edf8954b8716e15d662a",
            ["/map?converge=1"] = "c794ea0deb1029ed2d23474349c6a975",
            ["/map?counter=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?counter=1&watch=2"] = "a0f9333661030ac36262796ec609223c",
            ["/map?counter=1&watch=5"] = "a0f9333661030ac36262796ec609223c",
            ["/map?credits=1234&fuel=7&simhours=9"] = "3c763561dd4b1ac8ed9c83233c8bc6c6",
            ["/map?credits=50000"] = "f68df529959ff8ecf9d876d4f4daa831",
            // #1066 · the meeting's door. One new ROW and no moved ones: CrewCheat was already the 34th
            // field on the holder (#663 put it there), so every other URL's rendering is untouched and only
            // the value this key answers is new. It differs from ?crew=petition below by exactly that value,
            // which is the whole of what this file measures.
            ["/map?crew=meeting"] = "ab28302486eb4b00924b3ae71a330c8f",
            ["/map?crew=petition"] = "1d13fad04b3885262b747ce4f4abfbae",
            ["/map?death=collector&dock=selene-gate"] = "164fc911f9ebe99fa81c95e8eddee520",
            ["/map?death=impact"] = "0a788556597c1a77fd73e16dcd487829",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "c0991103b34ed40fd010010f17e25c31",
            ["/map?deflection=1"] = "d1a65c7f738628d4a39e6211aefa8cd1",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "156a96ec993d36574d3839a766e92bbd",
            ["/map?designate=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "276ae3b2cdfef0dab9e5fa525c7c89e2",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "a2be7f813a92d5f21b59c4848434e17b",
            ["/map?dock=selene-gate&ashore=1&havenfloor=-1"] = "2c777c5c08ff990649c0d46697086b80",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "f6f40476b56761055989d9d129842c10",
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "400a44f8018d3c0043e46d57e0c8a818",
            ["/map?dock=the-space-bar"] = "020d40649293bfccdfac18b493fbe6ce",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "020d40649293bfccdfac18b493fbe6ce",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "020d40649293bfccdfac18b493fbe6ce",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "020d40649293bfccdfac18b493fbe6ce",
            ["/map?dock=the-tilt&site=0"] = "dddd1161ae724920a4a0b8e379b49179",
            ["/map?dock=the-tilt&site=0&land=1"] = "dddd1161ae724920a4a0b8e379b49179",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "eecdac0824f05cd168fc0621a5a73cf8",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "dddd1161ae724920a4a0b8e379b49179",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "dddd1161ae724920a4a0b8e379b49179",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "dddd1161ae724920a4a0b8e379b49179",
            ["/map?dock=the-tilt&site=1"] = "dddd1161ae724920a4a0b8e379b49179",
            ["/map?dock=the-tilt&start=space-bar"] = "ebc4b6e2f837cd2cc7efb8345943baaa",
            ["/map?expedition=mining"] = "88210743de17ab5d285d4410231c09d3",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "1075fd31e877987c076b893d13f912c5",
            ["/map?found=1&land=1"] = "ee7f19029e6fb54bb6463151a46120a5",
            ["/map?found=1&land=1&floor=17&card=all"] = "ee7f19029e6fb54bb6463151a46120a5",
            ["/map?freight=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?frontdoor=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?goodscar=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?kaamos=all"] = "6c9e6b7ccdd36e68c7425020a8fe6bad",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "ccda4e4be58c5c01179078bad83911be",
            ["/map?kaamos=hq&land=1"] = "ccda4e4be58c5c01179078bad83911be",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "67b656466fe4797e0f7686cf8b045467",
            ["/map?nebula=all"] = "d09a9a9ed8877b7f889f8ddd38d262f4",
            ["/map?nopattern=1&death=impact"] = "6210fa6f77afbdac3dea64a8d9e1e7c6",
            ["/map?oldcrew=1"] = "126c272fad8c10fc076755e7ea176949",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "3aeb324d9e6c54486b300ab75da2cd6a",
            ["/map?park=1"] = "a0f9333661030ac36262796ec609223c",
            // #759 · …and both of them read EXACTLY what ?park=1 reads, which is the honest answer and worth
            // the row rather than an exemption: `?parkphase=` writes two fields on the PAGE (which phase was
            // asked for, and whether the morning door was), and not one of the thirty BootQuery fields this
            // file renders. The park's clock is jumped where the site is known, long after the parse.
            ["/map?park=1&parkphase=morning"] = "a0f9333661030ac36262796ec609223c",
            ["/map?park=1&parkphase=night"] = "a0f9333661030ac36262796ec609223c",
            ["/map?park=1&spread=1"] = "9832ebfba4400289211f8be976b5fa08",
            ["/map?parkback=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?parkwalk=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?patrol=2"] = "a0f9333661030ac36262796ec609223c",
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "4c72edb032c64d32dcf6f3140eecff11",
            ["/map?ringoffice=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?rip=1"] = "9832ebfba4400289211f8be976b5fa08",
            ["/map?scenario=..%2Foops"] = "19cb31de6be77f6e45fd408a5c766fb6",
            ["/map?scenario=sol-eu"] = "711e345c35c783ca0930a7a6f3c0fe21",
            ["/map?secretlab=1"] = "ee7f19029e6fb54bb6463151a46120a5",
            ["/map?secretlab=deep&land=1&card=next"] = "a0f9333661030ac36262796ec609223c",
            ["/map?secretlab=deep&land=1&floor=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "a0f9333661030ac36262796ec609223c",
            // #841 · ?perf=1 is read where the DeckView is built, not into BootQuery — it changes nothing
            // the parse answers, and this row says exactly that.
            ["/map?secretlab=deep&land=1&floor=1&perf=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?secretlab=deep&land=1&floor=21"] = "a0f9333661030ac36262796ec609223c",
            // #619 · the fourth cheat rock's own row — an ADDITION, not a change.
            ["/map?secretlab=sealed&land=1&floor=3"] = "1f1a654f6fd1705a708357bc1259611d",
            ["/map?skim=saturn"] = "5a16bc225f08de9fffb911628a4f983e",
            ["/map?sling=jupiter"] = "6e209ed0d705be961dc88f129d058bd7",
            ["/map?spread=1"] = "9832ebfba4400289211f8be976b5fa08",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "3aeb324d9e6c54486b300ab75da2cd6a",
            ["/map?start=wreck&fetch=active"] = "20c01e619af80d155e6e3642c5b94efe",
            ["/map?start=wreck&dest=saturn"] = "922cd3bc537f3010e6bd653483f235a2",
            ["/map?start=wreck&target=collector"] = "41a6e6cda9412b47307849e8bf06135d",
            ["/map?stool=1&neighbour=0"] = "a0f9333661030ac36262796ec609223c",
            ["/map?stool=1&neighbour=1"] = "a0f9333661030ac36262796ec609223c",
            ["/map?tablescene=free&approach=1"] = "9832ebfba4400289211f8be976b5fa08",
            // #973 L2 · …and the rep row hashes the SAME, which is correct and worth saying: this sweep
            // renders BootQuery's own public fields, and neither ?approach= nor ?rep= lives there — both are
            // read straight onto the page (_approachCheat, _repCheat). The query object really is identical;
            // what the two URLs build differently is pinned next door, in TheBootBuildsTheSameWorldTests.
            ["/map?tablescene=free&rep=1&approach=0"] = "9832ebfba4400289211f8be976b5fa08",
            ["/map?tablescene=free&watch=5&approach=0"] = "9832ebfba4400289211f8be976b5fa08",
            ["/map?threads=1"] = "9832ebfba4400289211f8be976b5fa08",
            ["/map?threads=1&watch=5"] = "9832ebfba4400289211f8be976b5fa08",
            ["/map?wreck=drivefailure&land=1"] = "7877af40bed8c6c5ba66aeb9aeaae3b4",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "7a11db8db162c90844971ff8d9d1f4c0",
        };

    /// <summary>#1253 · The same dump door the world sweep next door has had since it was written
    /// (<c>SPACESAILS_BOOT_FINGERPRINT_DUMP</c>). Seven re-pins of this table have been done by lifting
    /// <c>read</c> lines out of a failing run's message and pasting them in — right every time, and the
    /// TRANSCRIPTION is the thing this repository has already decided cannot be trusted (#1055). Set it to a
    /// path and the run writes the whole dictionary body instead of asserting.</summary>
    private const string DumpVariable = "SPACESAILS_QUERY_FINGERPRINT_DUMP";

    [Fact]
    public void EveryBootUrlIsReadTheWayItAlwaysWas()
    {
        string? dump = Environment.GetEnvironmentVariable(DumpVariable);
        var dumped = new System.Text.StringBuilder();
        var wrong = new List<string>();
        foreach (string url in TheBootBuildsTheSameWorldTests.EveryBootUrl())
        {
            string said = WhatTheQuerySaid(url);
            string hash = TheBootBuildsTheSameWorldTests.Sha256(said);

            if (dump is not null)
            {
                dumped.Append("            [\"").Append(url).Append("\"] = \"").Append(hash).Append("\",\n");
                continue;
            }

            Assert.True(WhatEachUrlSaid.ContainsKey(url), $"{url} is not pinned.");
            if (!string.Equals(WhatEachUrlSaid[url], hash, StringComparison.Ordinal))
            {
                wrong.Add($"{url}{Nl}  pinned {WhatEachUrlSaid[url]}{Nl}  read   {hash}{Nl}{said}");
            }
        }

        if (dump is not null)
        {
            File.WriteAllText(dump, dumped.ToString());
            return;
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
        // …and one more again (#1253's HavenFloorCheat, the dev door onto DOWN BELOW: a haven has floors now,
        // and `?havenfloor=-1` is how a tester reaches the one that is not the concourse. It sits behind an
        // ashore walk an automated tab cannot make and then a press at the far side of a hall, which is
        // exactly the kind of scene this catalogue exists for — and the number moves in the same commit as
        // the key, which is what this row is for).
        Assert.Equal(38, TheQuery("/map").GetType()
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
