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
            ["/map"] = "7340ef68183cdc20efd2162f0667957a",
            ["/map?archive=1&land=1&nerve=2"] = "06bee9c2df60fc36b043e5a84477aedd",
            ["/map?ashore=1&kaamos=bounce"] = "073b0a2c8904b2f4753b9ce15ac01be8",
            ["/map?ashore=1&start=space-bar"] = "caa117814493b5a42acd71625c9cfa23",
            ["/map?badge=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?barcase=1"] = "bc354594c7c4ef2ce8f68e497e52683f",
            ["/map?bond=1"] = "54e148e6a3b6c1646b3dd96ae089c9d1",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "4da723e797d33350845960090030515d",
            ["/map?converge=1"] = "1ca918e96cbdff70dde4de5806edc8a4",
            ["/map?counter=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?counter=1&watch=2"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?counter=1&watch=5"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?credits=1234&fuel=7&simhours=9"] = "82b201bf278f1f89ba0e144b36d0c78d",
            ["/map?credits=50000"] = "c69b07284cc2e6beec139d6d9faad42d",
            // #1066 · the meeting's door. One new ROW and no moved ones: CrewCheat was already the 34th
            // field on the holder (#663 put it there), so every other URL's rendering is untouched and only
            // the value this key answers is new. It differs from ?crew=petition below by exactly that value,
            // which is the whole of what this file measures.
            ["/map?crew=meeting"] = "11a4002c253c8c009f7cef45d7522c7c",
            ["/map?crew=petition"] = "52bdfb7c33bfaf3f5ea1e78e6b9a72bf",
            ["/map?death=collector&dock=selene-gate"] = "02d4180d629a5d155fc85fdc8d0ec50e",
            ["/map?death=impact"] = "ed9fc4c8d47c9e16c80c9fdc4a651532",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "8914bbd8534448bf0b18aeb2c3cb686c",
            ["/map?deflection=1"] = "e7dea542ef94b5b7fd6887d9f727e49e",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "7449b01d89216446ed24971915233214",
            ["/map?designate=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "40abe835c5fe0749acbde1c915b37157",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "b152590aef8421c9dee5772dd5caa8be",
            ["/map?dock=selene-gate&ashore=1&havenfloor=-1"] = "de0cacaa1af96adcd85717a88308c3a1",
            // #1332 B · the garden behind glass, one pace outside its door — dumped, not typed.
            ["/map?dock=selene-gate&ashore=1&garden=1"] = "b2c968faa461e9be82ad1066efb7f7de",
            // #1332 A · the six floors the other hubs grew — each its own reading (the berth differs). Dumped with
            // SPACESAILS_QUERY_FINGERPRINT_DUMP; 6 new, 0 moved, 0 gone.
            ["/map?dock=the-space-bar&ashore=1&havenfloor=-1"] = "24927699ae2ecc6f6c8f14e80ebd606c",
            // #1332 B · the garden behind glass, one pace outside its door — dumped, not typed.
            ["/map?dock=the-space-bar&ashore=1&garden=1"] = "a6b016573534d2329700612710b603f3",
            ["/map?dock=cinder-roost&ashore=1&havenfloor=-1"] = "eb027ab996be15b6f21e8fe723bdf929",
            ["/map?dock=ringside-exchange&ashore=1&havenfloor=-1"] = "643010e988bda2224bb9cd641f359a34",
            // #1332 C · the Preservation office, shut and on the clerk's watch: MEASURED with SPACESAILS_QUERY_FINGERPRINT_DUMP; 2 new, 0 moved.
            ["/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=open"] = "643010e988bda2224bb9cd641f359a34",
            ["/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=shut"] = "643010e988bda2224bb9cd641f359a34",
            // #1332 D/E · the forwarding desk and the adjuster's room, shut and ajar: MEASURED with SPACESAILS_QUERY_FINGERPRINT_DUMP; 4 new, 0 moved.
            ["/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=open"] = "eb027ab996be15b6f21e8fe723bdf929",
            ["/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=shut"] = "eb027ab996be15b6f21e8fe723bdf929",
            ["/map?dock=the-deep&ashore=1&havenfloor=-1&office=open"] = "3d2ef20ec20df6b1616fe30b454e1ebb",
            ["/map?dock=the-deep&ashore=1&havenfloor=-1&office=shut"] = "3d2ef20ec20df6b1616fe30b454e1ebb",
            ["/map?dock=the-tilt&ashore=1&havenfloor=-1"] = "88112b3cdf4254091c5ef5996bc0fcbc",
            ["/map?dock=red-eye&ashore=1&havenfloor=-1"] = "adf2e87e97a2c1204bab86b1ea14ab6e",
            ["/map?dock=the-deep&ashore=1&havenfloor=-1"] = "3d2ef20ec20df6b1616fe30b454e1ebb",
            // #794 slice 2 · the chalk mark's two dev rows, moved from ?park=1 to the gallery with the drop.
            // ?chalk= is claimed and answers nothing, so the up row and the wiped row read one and the same
            // line — the park's two rows went, these two came, and no other line moved.
            ["/map?dock=selene-gate&ashore=1&chalk=1"] = "216ca741bcc368d077020cf5c7391ec7",
            ["/map?dock=selene-gate&ashore=1&chalk=wiped"] = "216ca741bcc368d077020cf5c7391ec7",
            // #1202 slice 2 · the spike's two dev rows. ?spike= writes no world and nothing the parse answers (it is
            // read off the address bar once ashore) — the chalk rows' own reason, and their pin. No other line moved.
            ["/map?dock=selene-gate&ashore=1&spike=1"] = "216ca741bcc368d077020cf5c7391ec7",
            // #1202 slice 3 · the spike composed with ?tailed=1. Measured: ?tailed= answers no query key this sweep
            // reads, so the row reads the spike=1 row's own line. No other line moved.
            ["/map?dock=selene-gate&ashore=1&spike=1&tailed=1"] = "216ca741bcc368d077020cf5c7391ec7",
            ["/map?dock=selene-gate&ashore=1&spike=spiked"] = "216ca741bcc368d077020cf5c7391ec7",
            // #1202 slice 4 · ?spike=paid — the same latch, read once ashore: measured, the spike rows' own line. No other line moved.
            ["/map?dock=selene-gate&ashore=1&spike=paid"] = "216ca741bcc368d077020cf5c7391ec7",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "c9dc1a2bef5794fe66f1143f6c35bab7",
            // #319 slice 2 · the geocache sale's two dev rows. ?geocache= writes no world and nothing the parse
            // answers (it is read off the address bar once the berth is clamped) — dumped and diffed: these two
            // lines are the only thing the dump added, and no other line moved.
            ["/map?dock=selene-gate&geocache=1"] = "c9dc1a2bef5794fe66f1143f6c35bab7",
            ["/map?dock=selene-gate&geocache=lifted"] = "c9dc1a2bef5794fe66f1143f6c35bab7",
            // #1202 · the stringer's two dev rows. ?press= writes no world and nothing the parse answers (it is
            // read off the address bar once the berth is clamped) — the geocache rows' own reason, and their pin.
            ["/map?dock=selene-gate&press=1"] = "c9dc1a2bef5794fe66f1143f6c35bab7",
            ["/map?dock=selene-gate&press=filed"] = "c9dc1a2bef5794fe66f1143f6c35bab7",
            // #1202 slice 2 QA · ?press=pending — the same latch, read after the clamp: the berth's own line, measured. No other line moved.
            ["/map?dock=selene-gate&press=pending"] = "c9dc1a2bef5794fe66f1143f6c35bab7",
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "17f06d123f92e3d86e550b66e8a39a56",
            ["/map?dock=the-space-bar"] = "10ea7b2637ed6d83b9f72c0c58592e9a",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "10ea7b2637ed6d83b9f72c0c58592e9a",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "10ea7b2637ed6d83b9f72c0c58592e9a",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "10ea7b2637ed6d83b9f72c0c58592e9a",
            ["/map?dock=the-tilt&site=0"] = "6ba831ba248a0c1aa9399654edb69bb1",
            ["/map?dock=the-tilt&site=0&land=1"] = "6ba831ba248a0c1aa9399654edb69bb1",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "807c3d0e85c7f05e310c486041f2cd57",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "6ba831ba248a0c1aa9399654edb69bb1",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "6ba831ba248a0c1aa9399654edb69bb1",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "6ba831ba248a0c1aa9399654edb69bb1",
            ["/map?dock=the-tilt&site=1"] = "6ba831ba248a0c1aa9399654edb69bb1",
            ["/map?dock=the-tilt&start=space-bar"] = "d1a0b0639d324111de4f03a994ed53d7",
            ["/map?expedition=mining"] = "e2e48e6767a1b99572b5deb50ca5216d",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "5c39b63aba4ee9e785f1000da1708ff1",
            ["/map?found=1&land=1"] = "fa8ce2446b53915f638c086431d8ba7b",
            ["/map?found=1&land=1&floor=17&card=all"] = "fa8ce2446b53915f638c086431d8ba7b",
            ["/map?freight=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?frontdoor=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?goodscar=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?kaamos=all"] = "70cd7436edfa7933b028a287581a93a6",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "efc1567aece6af9be5441932872dad41",
            ["/map?kaamos=hq&land=1"] = "efc1567aece6af9be5441932872dad41",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "2e606d577d96bc98757dc99bde61a745",
            ["/map?nebula=all"] = "3f70359be3a869e18552416ab4a755e3",
            ["/map?nopattern=1&death=impact"] = "5bd52e562cd1d3a815baba4c93514c8d",
            ["/map?oldcrew=1"] = "02a2612dff0fb2fef89150f1e723e828",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "36bee8e591da74bbcb9d8a55f56d71fb",
            ["/map?park=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            // #759 · …and both of them read EXACTLY what ?park=1 reads, which is the honest answer and worth
            // the row rather than an exemption: `?parkphase=` writes two fields on the PAGE (which phase was
            // asked for, and whether the morning door was), and not one of the thirty BootQuery fields this
            // file renders. The park's clock is jumped where the site is known, long after the parse.
            ["/map?park=1&parkphase=morning"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?park=1&parkphase=night"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?park=1&spread=1"] = "4ada70de5cae5abff721031c47bab393",
            ["/map?parkback=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?parkwalk=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?patrol=2"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "0916b5f71b2e11c3d4cea2c6c138606c",
            ["/map?ringoffice=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?rip=1"] = "4ada70de5cae5abff721031c47bab393",
            ["/map?scenario=..%2Foops"] = "7340ef68183cdc20efd2162f0667957a",
            ["/map?scenario=sol-eu"] = "df5e604721764606b5a05c6ee7ce9a74",
            ["/map?secretlab=1"] = "fa8ce2446b53915f638c086431d8ba7b",
            // #618 · the man at the door, three windows: MEASURED with SPACESAILS_QUERY_FINGERPRINT_DUMP; 3 new, 0 moved.
            ["/map?secretlab=1&land=1&guard=absent"] = "fa8ce2446b53915f638c086431d8ba7b",
            ["/map?secretlab=1&land=1&guard=posted"] = "fa8ce2446b53915f638c086431d8ba7b",
            ["/map?secretlab=1&land=1&guard=round"] = "fa8ce2446b53915f638c086431d8ba7b",
            ["/map?secretlab=deep&land=1&card=next"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?secretlab=deep&land=1&floor=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "127e5605c5a4998adb37a75eb9cb78c0",
            // #841 · ?perf=1 is read where the DeckView is built, not into BootQuery — it changes nothing
            // the parse answers, and this row says exactly that.
            ["/map?secretlab=deep&land=1&floor=1&perf=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?secretlab=deep&land=1&floor=21"] = "127e5605c5a4998adb37a75eb9cb78c0",
            // #619 · the fourth cheat rock's own row — an ADDITION, not a change.
            ["/map?secretlab=sealed&land=1&floor=3"] = "cf105423c9ac4aa039e5e11f473d1ecd",
            ["/map?shuttle=1&land=1"] = "fa8ce2446b53915f638c086431d8ba7b",
            ["/map?skim=saturn"] = "d67df16078c6405bb1ab8c11c585b31e",
            ["/map?sling=jupiter"] = "f939637b4ea7931770ce7dc56d36aca2",
            ["/map?spread=1"] = "4ada70de5cae5abff721031c47bab393",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "36bee8e591da74bbcb9d8a55f56d71fb",
            ["/map?start=wreck&fetch=active"] = "5426e9daa0c235229fa06e054e694ca9",
            ["/map?start=wreck&dest=saturn"] = "e4ebf94a90c68842dca3272c8fddb1f3",
            ["/map?start=wreck&target=collector"] = "cf7f8be1c254540a4c31eb80dd9a0671",
            // #653 · the dead station's dev door. ?station= is a NEW KEY ON THE HOLDER (StationCheat), so EVERY row of this
            // table moved with it — each hash reads every field, and the field list grew by one — and this row is new.
            // MEASURED with SPACESAILS_QUERY_FINGERPRINT_DUMP, never typed: 115 rows moved, 1 new, 0 gone.
            ["/map?station=1&land=1"] = "9fc9fe05c4613c815f3bcaaaabd6d089",
            ["/map?stool=1&neighbour=0"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?stool=1&neighbour=1"] = "127e5605c5a4998adb37a75eb9cb78c0",
            ["/map?tablescene=free&approach=1"] = "4ada70de5cae5abff721031c47bab393",
            // #973 L2 · …and the rep row hashes the SAME, which is correct and worth saying: this sweep
            // renders BootQuery's own public fields, and neither ?approach= nor ?rep= lives there — both are
            // read straight onto the page (_approachCheat, _repCheat). The query object really is identical;
            // what the two URLs build differently is pinned next door, in TheBootBuildsTheSameWorldTests.
            ["/map?tablescene=free&rep=1&approach=0"] = "4ada70de5cae5abff721031c47bab393",
            ["/map?tablescene=free&watch=5&approach=0"] = "4ada70de5cae5abff721031c47bab393",
            ["/map?threads=1"] = "4ada70de5cae5abff721031c47bab393",
            ["/map?threads=1&watch=5"] = "4ada70de5cae5abff721031c47bab393",
            ["/map?wreck=drivefailure&land=1"] = "2af3baecd746ad681c2b40c18907a1c9",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "4f8c60929b915f82dc116f68d8b221a4",
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
        // …and one more again (#653's StationCheat, the dev door onto the dead station: a pseudo-body a boot hangs off
        // the berth, which is the wreck cheat's own shape — so the key belongs on the holder beside WreckCheat, and the
        // number moves in the same commit as the key, which is what this row is for).
        Assert.Equal(40, TheQuery("/map").GetType()
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
