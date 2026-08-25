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
            ["/map"] = "81050978c17944076167c727b9627418",
            ["/map?archive=1&land=1&nerve=2"] = "81545e46b6889d7d4cac3531f0fe72f2",
            ["/map?ashore=1&kaamos=bounce"] = "474263ef36bf6879ef784a4224f9d3e4",
            ["/map?ashore=1&start=space-bar"] = "641ea84c6ff694e570f2100a3e1fa791",
            ["/map?badge=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?bond=1"] = "d9b2f1c468710f58b4f5c014bda24ded",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "fb8a0aa4837f119785e10cae360e52e1",
            ["/map?converge=1"] = "971a4d049e223c642dc6a90d2cc57653",
            ["/map?counter=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?counter=1&watch=2"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?counter=1&watch=5"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?credits=1234&fuel=7&simhours=9"] = "2c6df7449be03e86e509f113626f2c47",
            ["/map?credits=50000"] = "3519d0f06fa1643b842617a0d570c6b6",
            ["/map?death=collector&dock=selene-gate"] = "ccb89a0de7bfa72974773796dabfcc64",
            ["/map?death=impact"] = "b3e01f0a48c21ef94c40a25ac1f6cb2a",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "ebd62db19051d3d4710cf430c80cd817",
            ["/map?deflection=1"] = "00eae461a05b8271c332109408e7284f",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "2ad9fdb2b743c9e3f9608a05cc27b6f6",
            ["/map?designate=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "8d3a0f2c4416a7cbb23e229c6bbedf17",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "9ba302a6fcfd129dc6fdf007ea928225",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "48bdc2c1178c16a326121fa023d7c462",
            ["/map?dock=selene-gate&target=collector"] = "4190f74f843758cd5076f25d760d9d68",
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "f69ff35739db0443797d05417b05694e",
            ["/map?dock=the-space-bar"] = "2666f2642f32cd644e5010caee8641e4",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "2666f2642f32cd644e5010caee8641e4",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "2666f2642f32cd644e5010caee8641e4",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "2666f2642f32cd644e5010caee8641e4",
            ["/map?dock=the-tilt&site=0"] = "aae88c7e37e0e20b071b399895981655",
            ["/map?dock=the-tilt&site=0&land=1"] = "aae88c7e37e0e20b071b399895981655",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "d76b1d3af1e6ad1f9a7339a474607ba1",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "aae88c7e37e0e20b071b399895981655",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "aae88c7e37e0e20b071b399895981655",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "aae88c7e37e0e20b071b399895981655",
            ["/map?dock=the-tilt&site=1"] = "aae88c7e37e0e20b071b399895981655",
            ["/map?dock=the-tilt&start=space-bar"] = "8dca6966a2f4ecbf1143f9dbc0442426",
            ["/map?expedition=mining"] = "f287632d04ec75061946761ba85ec39e",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "58a48f2df6fad2a016f5b74f98206a3c",
            ["/map?found=1&land=1"] = "bfaecef3f832dcc661701a0a255ef057",
            ["/map?found=1&land=1&floor=17&card=all"] = "bfaecef3f832dcc661701a0a255ef057",
            ["/map?freight=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?frontdoor=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?goodscar=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?kaamos=all"] = "1ad48e3276879dbdcb1a34fe8d4e71c9",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "ab91c7b3ddb50c80f7442833fba13c14",
            ["/map?kaamos=hq&land=1"] = "ab91c7b3ddb50c80f7442833fba13c14",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "d3273f074a5dd198543bba4776c647f5",
            ["/map?nebula=all"] = "7725ed11853120a211ba50b3457de44c",
            ["/map?oldcrew=1"] = "41df5d068e22b58dcb92f7ad0a1487f5",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "81050978c17944076167c727b9627418",
            ["/map?park=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?park=1&spread=1"] = "1d4f2e6f33cd10b113601e12e9d329f0",
            ["/map?parkback=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?parkwalk=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?patrol=2"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "4f3b7ada82b2f1adf200f76b6d341aa5",
            ["/map?ringoffice=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?rip=1"] = "1d4f2e6f33cd10b113601e12e9d329f0",
            ["/map?scenario=..%2Foops"] = "81050978c17944076167c727b9627418",
            ["/map?scenario=sol-eu"] = "60e736bce2581c1bdf870a484cde2149",
            ["/map?secretlab=1"] = "bfaecef3f832dcc661701a0a255ef057",
            ["/map?secretlab=deep&land=1&card=next"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?secretlab=deep&land=1&floor=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "5c45561aa3057f7f7f7114c146d83318",
            // #841 · ?perf=1 is read where the DeckView is built, not into BootQuery — it changes nothing
            // the parse answers, and this row says exactly that.
            ["/map?secretlab=deep&land=1&floor=1&perf=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?secretlab=deep&land=1&floor=21"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?skim=saturn"] = "eccd3f3944221f98d0943b21f6619bf4",
            ["/map?sling=jupiter"] = "11c9ef17724dba42f91962ce3b691958",
            ["/map?spread=1"] = "1d4f2e6f33cd10b113601e12e9d329f0",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "81050978c17944076167c727b9627418",
            ["/map?start=wreck&fetch=active"] = "c16ed06046a6c94c92d55f33f91e9b66",
            ["/map?stool=1&neighbour=0"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?stool=1&neighbour=1"] = "5c45561aa3057f7f7f7114c146d83318",
            ["/map?tablescene=free&approach=1"] = "1d4f2e6f33cd10b113601e12e9d329f0",
            // #973 L2 · …and the rep row hashes the SAME, which is correct and worth saying: this sweep
            // renders BootQuery's own public fields, and neither ?approach= nor ?rep= lives there — both are
            // read straight onto the page (_approachCheat, _repCheat). The query object really is identical;
            // what the two URLs build differently is pinned next door, in TheBootBuildsTheSameWorldTests.
            ["/map?tablescene=free&rep=1&approach=0"] = "1d4f2e6f33cd10b113601e12e9d329f0",
            ["/map?tablescene=free&watch=5&approach=0"] = "1d4f2e6f33cd10b113601e12e9d329f0",
            ["/map?threads=1"] = "1d4f2e6f33cd10b113601e12e9d329f0",
            ["/map?threads=1&watch=5"] = "1d4f2e6f33cd10b113601e12e9d329f0",
            ["/map?wreck=drivefailure&land=1"] = "ab6fe475edbb53c2f4f158e85d086894",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "c4cef400e14815ccbe6b734083c9501b",
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
        Assert.Equal(32, TheQuery("/map").GetType()
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
