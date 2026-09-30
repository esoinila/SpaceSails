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
/// <para><b>#1332 B re-pinned every value an eighth time, for the eighth time for the same reason.</b> The dev
/// door onto the garden behind glass (<c>?garden=1</c>) is the THIRTY-NINTH field on the holder, so
/// <c>GardenCheat = False</c> joins the text for every URL: dumped and diffed, <b>107 moved, 0 gone, 2 new</b>
/// (the two garden rows), nothing typed. The world sweep next door moved 0 and gained the same 2.</para>
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
            ["/map"] = "246c48d4ab7eddd073e8c28442f24a2a",
            ["/map?archive=1&land=1&nerve=2"] = "03cf032947a759b2a14295798bf1daa4",
            ["/map?ashore=1&kaamos=bounce"] = "38689cde9151112beaa4878f90e48509",
            ["/map?ashore=1&start=space-bar"] = "1745240cc837aedfb505514bf85fa0fe",
            ["/map?badge=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?barcase=1"] = "50c2020287e1d5406709b4a0a8270408",
            ["/map?bond=1"] = "5b6a28d81f39f9033407f62786fa78f8",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "e59ed1a1885570c42e75c88a7ebc094b",
            ["/map?converge=1"] = "6908ebd66995a11aede0c6abb71f7b00",
            ["/map?counter=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?counter=1&watch=2"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?counter=1&watch=5"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?credits=1234&fuel=7&simhours=9"] = "17bcffff0b14dbce8a333a5c9a0c2a86",
            ["/map?credits=50000"] = "076e41613b43640a87ac0f31240ed5f0",
            // #1066 · the meeting's door. One new ROW and no moved ones: CrewCheat was already the 34th
            // field on the holder (#663 put it there), so every other URL's rendering is untouched and only
            // the value this key answers is new. It differs from ?crew=petition below by exactly that value,
            // which is the whole of what this file measures.
            ["/map?crew=meeting"] = "9118f0f281e438fa68a811d7ef4e2022",
            ["/map?crew=petition"] = "fc928e9ebfd688c93cd672544831b391",
            ["/map?death=collector&dock=selene-gate"] = "d509581c70d1f8f4eef814b3f2b3cd69",
            ["/map?death=impact"] = "24813aa68bbbdc3a15500bd3c7e7dca0",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "067fc31a6e31e7cc9fdac70ebcbd321c",
            ["/map?deflection=1"] = "522d6d05255e93b985fa0dd14613ec46",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "a29e48a95e20dd44304b965013760498",
            ["/map?designate=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "4aad68ab10d9afdb1f03831361b740bc",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "ca398182aa500fff1aca0546004adf4c",
            ["/map?dock=selene-gate&ashore=1&havenfloor=-1"] = "0ef3c0c809e744eaa6e31cf64c78bf4f",
            // #1332 B · the garden behind glass, one pace outside its door — dumped, not typed.
            ["/map?dock=selene-gate&ashore=1&garden=1"] = "f1baf302c48156ee355638ca1b4ab445",
            // #1332 A · the six floors the other hubs grew — each its own reading (the berth differs). Dumped with
            // SPACESAILS_QUERY_FINGERPRINT_DUMP; 6 new, 0 moved, 0 gone.
            ["/map?dock=the-space-bar&ashore=1&havenfloor=-1"] = "adf62bd085786e3b860f5c0ab4455651",
            // #1332 B · the garden behind glass, one pace outside its door — dumped, not typed.
            ["/map?dock=the-space-bar&ashore=1&garden=1"] = "29ec965e1907ee11afc35bc6788005b8",
            ["/map?dock=cinder-roost&ashore=1&havenfloor=-1"] = "540a99672f1543542f72d5598d23e740",
            ["/map?dock=ringside-exchange&ashore=1&havenfloor=-1"] = "b59dc71633c101bd8c3cc14fa520c976",
            // #1332 C · the Preservation office, shut and on the clerk's watch: MEASURED with SPACESAILS_QUERY_FINGERPRINT_DUMP; 2 new, 0 moved.
            ["/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=open"] = "b59dc71633c101bd8c3cc14fa520c976",
            ["/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=shut"] = "b59dc71633c101bd8c3cc14fa520c976",
            ["/map?dock=the-tilt&ashore=1&havenfloor=-1"] = "ad55547bafb6319ff968e067aaf9cc19",
            ["/map?dock=red-eye&ashore=1&havenfloor=-1"] = "779a7327071affc43208a8192c688c74",
            ["/map?dock=the-deep&ashore=1&havenfloor=-1"] = "a3f987bb11fcecd41bc7a8424c62a205",
            // #794 slice 2 · the chalk mark's two dev rows, moved from ?park=1 to the gallery with the drop.
            // ?chalk= is claimed and answers nothing, so the up row and the wiped row read one and the same
            // line — the park's two rows went, these two came, and no other line moved.
            ["/map?dock=selene-gate&ashore=1&chalk=1"] = "200b62a77accbce0ca84cfbcdd05507c",
            ["/map?dock=selene-gate&ashore=1&chalk=wiped"] = "200b62a77accbce0ca84cfbcdd05507c",
            // #1202 slice 2 · the spike's two dev rows. ?spike= writes no world and nothing the parse answers (it is
            // read off the address bar once ashore) — the chalk rows' own reason, and their pin. No other line moved.
            ["/map?dock=selene-gate&ashore=1&spike=1"] = "200b62a77accbce0ca84cfbcdd05507c",
            // #1202 slice 3 · the spike composed with ?tailed=1. Measured: ?tailed= answers no query key this sweep
            // reads, so the row reads the spike=1 row's own line. No other line moved.
            ["/map?dock=selene-gate&ashore=1&spike=1&tailed=1"] = "200b62a77accbce0ca84cfbcdd05507c",
            ["/map?dock=selene-gate&ashore=1&spike=spiked"] = "200b62a77accbce0ca84cfbcdd05507c",
            // #1202 slice 4 · ?spike=paid — the same latch, read once ashore: measured, the spike rows' own line. No other line moved.
            ["/map?dock=selene-gate&ashore=1&spike=paid"] = "200b62a77accbce0ca84cfbcdd05507c",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "524994cbf01ffb7bd7258bba87cc6899",
            // #319 slice 2 · the geocache sale's two dev rows. ?geocache= writes no world and nothing the parse
            // answers (it is read off the address bar once the berth is clamped) — dumped and diffed: these two
            // lines are the only thing the dump added, and no other line moved.
            ["/map?dock=selene-gate&geocache=1"] = "524994cbf01ffb7bd7258bba87cc6899",
            ["/map?dock=selene-gate&geocache=lifted"] = "524994cbf01ffb7bd7258bba87cc6899",
            // #1202 · the stringer's two dev rows. ?press= writes no world and nothing the parse answers (it is
            // read off the address bar once the berth is clamped) — the geocache rows' own reason, and their pin.
            ["/map?dock=selene-gate&press=1"] = "524994cbf01ffb7bd7258bba87cc6899",
            ["/map?dock=selene-gate&press=filed"] = "524994cbf01ffb7bd7258bba87cc6899",
            // #1202 slice 2 QA · ?press=pending — the same latch, read after the clamp: the berth's own line, measured. No other line moved.
            ["/map?dock=selene-gate&press=pending"] = "524994cbf01ffb7bd7258bba87cc6899",
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "d3e3955eac10acc0dfb39538324c9d06",
            ["/map?dock=the-space-bar"] = "a4ebf599897e388819f8d35cca886866",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "a4ebf599897e388819f8d35cca886866",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "a4ebf599897e388819f8d35cca886866",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "a4ebf599897e388819f8d35cca886866",
            ["/map?dock=the-tilt&site=0"] = "33a7d81e14346d14193152d097bcffa4",
            ["/map?dock=the-tilt&site=0&land=1"] = "33a7d81e14346d14193152d097bcffa4",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "10e98cb9cc94c155f2a8d4429a53f891",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "33a7d81e14346d14193152d097bcffa4",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "33a7d81e14346d14193152d097bcffa4",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "33a7d81e14346d14193152d097bcffa4",
            ["/map?dock=the-tilt&site=1"] = "33a7d81e14346d14193152d097bcffa4",
            ["/map?dock=the-tilt&start=space-bar"] = "cd1e133988e53b6531157de338a97a9e",
            ["/map?expedition=mining"] = "f576ed8084d75af461229605025a959a",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "5ab5e7501ea1ffbd68abdfb3ce5e1dd0",
            ["/map?found=1&land=1"] = "ea2b32a0a5ef4ec8b1278a24c5cfd8c8",
            ["/map?found=1&land=1&floor=17&card=all"] = "ea2b32a0a5ef4ec8b1278a24c5cfd8c8",
            ["/map?freight=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?frontdoor=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?goodscar=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?kaamos=all"] = "83ca3975419eeecb1cd912ba0cd5957f",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "da26f51e48f11d8b61e720768ee17b7e",
            ["/map?kaamos=hq&land=1"] = "da26f51e48f11d8b61e720768ee17b7e",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "89ae27ed5eca0fe103422070e6b351ca",
            ["/map?nebula=all"] = "995ae40b265b4dd751af3d3dce2b8d82",
            ["/map?nopattern=1&death=impact"] = "de1d7fe06424a3acaed4ef9e3435c14e",
            ["/map?oldcrew=1"] = "14e9e5225022c53f1d21fe5fd5d8c222",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "12575242a05b64f8fb4ec020022e5f59",
            ["/map?park=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            // #759 · …and both of them read EXACTLY what ?park=1 reads, which is the honest answer and worth
            // the row rather than an exemption: `?parkphase=` writes two fields on the PAGE (which phase was
            // asked for, and whether the morning door was), and not one of the thirty BootQuery fields this
            // file renders. The park's clock is jumped where the site is known, long after the parse.
            ["/map?park=1&parkphase=morning"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?park=1&parkphase=night"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?park=1&spread=1"] = "e8cd1e5840d71f3a95c22195bd5b3472",
            ["/map?parkback=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?parkwalk=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?patrol=2"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "8fa5efd995b8134e2c9e4d174d45a746",
            ["/map?ringoffice=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?rip=1"] = "e8cd1e5840d71f3a95c22195bd5b3472",
            ["/map?scenario=..%2Foops"] = "246c48d4ab7eddd073e8c28442f24a2a",
            ["/map?scenario=sol-eu"] = "11e69e416b365f325d913c5776718522",
            ["/map?secretlab=1"] = "ea2b32a0a5ef4ec8b1278a24c5cfd8c8",
            // #618 · the man at the door, three windows: MEASURED with SPACESAILS_QUERY_FINGERPRINT_DUMP; 3 new, 0 moved.
            ["/map?secretlab=1&land=1&guard=absent"] = "ea2b32a0a5ef4ec8b1278a24c5cfd8c8",
            ["/map?secretlab=1&land=1&guard=posted"] = "ea2b32a0a5ef4ec8b1278a24c5cfd8c8",
            ["/map?secretlab=1&land=1&guard=round"] = "ea2b32a0a5ef4ec8b1278a24c5cfd8c8",
            ["/map?secretlab=deep&land=1&card=next"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?secretlab=deep&land=1&floor=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "d9743721b7668ce559ca2a1b12b5176c",
            // #841 · ?perf=1 is read where the DeckView is built, not into BootQuery — it changes nothing
            // the parse answers, and this row says exactly that.
            ["/map?secretlab=deep&land=1&floor=1&perf=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?secretlab=deep&land=1&floor=21"] = "d9743721b7668ce559ca2a1b12b5176c",
            // #619 · the fourth cheat rock's own row — an ADDITION, not a change.
            ["/map?secretlab=sealed&land=1&floor=3"] = "819a3d8c7326f7efa078cf72c28b8d7c",
            ["/map?skim=saturn"] = "055d38b2f40415d26e0055cfa20eaedd",
            ["/map?sling=jupiter"] = "24600a2ed3de2ce1705aa97d0a33c184",
            ["/map?spread=1"] = "e8cd1e5840d71f3a95c22195bd5b3472",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "12575242a05b64f8fb4ec020022e5f59",
            ["/map?start=wreck&fetch=active"] = "d2838c102fdddff49cf821376f472390",
            ["/map?start=wreck&dest=saturn"] = "e93e7a1bb6725f2d372d7668419b8823",
            ["/map?start=wreck&target=collector"] = "e70ffb189fe4651def534c7d7e8a28bb",
            ["/map?stool=1&neighbour=0"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?stool=1&neighbour=1"] = "d9743721b7668ce559ca2a1b12b5176c",
            ["/map?tablescene=free&approach=1"] = "e8cd1e5840d71f3a95c22195bd5b3472",
            // #973 L2 · …and the rep row hashes the SAME, which is correct and worth saying: this sweep
            // renders BootQuery's own public fields, and neither ?approach= nor ?rep= lives there — both are
            // read straight onto the page (_approachCheat, _repCheat). The query object really is identical;
            // what the two URLs build differently is pinned next door, in TheBootBuildsTheSameWorldTests.
            ["/map?tablescene=free&rep=1&approach=0"] = "e8cd1e5840d71f3a95c22195bd5b3472",
            ["/map?tablescene=free&watch=5&approach=0"] = "e8cd1e5840d71f3a95c22195bd5b3472",
            ["/map?threads=1"] = "e8cd1e5840d71f3a95c22195bd5b3472",
            ["/map?threads=1&watch=5"] = "e8cd1e5840d71f3a95c22195bd5b3472",
            ["/map?wreck=drivefailure&land=1"] = "6692f41baca643350fbdab9e07f67c29",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "f255abf3a3c19d206ed77798949640dc",
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
        // …and one more again (#1332 B's GardenCheat, the dev door onto the garden behind glass: a room off the
        // concourse behind an ashore walk an automated tab cannot make — and the number moves in the same commit
        // as the key, which is what this row is for).
        Assert.Equal(39, TheQuery("/map").GetType()
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
