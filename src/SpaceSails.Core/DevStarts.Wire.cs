namespace SpaceSails.Core;

// #251 · Split from DevStarts.cs, moved verbatim: the catalogue's rows for the faceless trade and the wire —
// the chalk mark (#794), the geocache sale (#319), carry the press, spike it and the tail at her table (#1202).
// DevStarts.All spreads them back in at the very place they stood, so the catalogue's order is unchanged;
// All, SectionTitle and SectionBlurb stay in DevStarts.cs. A method, not a field, so no static initializer
// depends on which file is compiled first (#1163).
public static partial class DevStarts
{
    /// <summary>The rows <see cref="All"/> spreads in after the park's morning and before the canteen's front door.</summary>
    private static IReadOnlyList<Entry> OnTheWire() =>
    [
        // #794 · THE CHALK MARK, at the gallery (slice 2, owner's ruling 2026-09-28). A paid delivery's return,
        // left under one of the observation walk's two tables on a schedule — and the schedule is the whole
        // difficulty of reaching it by play: a window is one watch in three, so a tester needs the clock put ON
        // one. Two rows, because the mark and the wipe are two scenes.
        new("🔭✚", "A chalk cross in the observation walk's gallery",
            "Ashore at Selene Gate and standing in the gallery at the end of the observation walk, with a paid "
            + "delivery's return on record and the clock at its window: the mark's line lands on the first "
            + "tick, a small chalk cross is drawn on the back-wall stone beside a vending machine, and the DEV "
            + "pulse names the table. Sit at that table alone and the card carries FEEL UNDER THE LIP. Take "
            + "it: a parcel in the satchel, one line in the book. At the other table the move is not there "
            + "at all (#794).",
            "/map?dock=selene-gate&ashore=1&chalk=1"),
        new("🔭🧽", "…and one watch later, the stone wiped",
            "The same return, one watch on: the mark was seen and the crew has wiped it. The wipe's line lands "
            + "once in the gallery, no cross is drawn, and the goods are still under the table for this one "
            + "watch (#794).",
            "/map?dock=selene-gate&ashore=1&chalk=wiped"),
        // #319 slice 2 · THE GEOCACHE SALE. The chip in the ground and its contract on the books, clamped at a
        // berth whose dark-web desk is open — the row, the quote and the sale in one URL; and the same with
        // the buyer already been, so the next desk opened pays.
        new("🗺", "Sell the location of a buried chip",
            "Clamped at Selene Gate with the data chip already in a chest of yours on a named ground and its "
            + "contract on the books. Open Comms → 🕸 Dark web market: the row SELL THE LOCATION, the chip and "
            + "the ground under it. Press 🗺 for the quote and the terms, then the credits to send the "
            + "coordinates (#319).",
            "/map?dock=selene-gate&geocache=1"),
        new("🗺💳", "…and the buyer has been",
            "The same chest, its location sold far enough back that the buyer's seeded lift has come. Open the "
            + "dark-web desk: the escrow lands on the payment pulse, once (#319).",
            "/map?dock=selene-gate&geocache=lifted"),
        // #1202 slice 1 · CARRY THE PRESS. A stringer aboard with her ground named, and the same trip made and paid
        // far enough back that her story is on the wire.
        new("📰", "Carry the press — she is aboard",
            "Clamped at Selene Gate with Rauha Lind's passage on the books: the contract row names her ground (one "
            + "the shuttle reaches from here). Board it: she comes down behind you, says where the source left her "
            + "word, walks your air with you, and the tin is under a probe near the pad. Lift off, clamp at a haven, "
            + "and she pays (#1202).",
            "/map?dock=selene-gate&press=1"),
        new("📰🗞", "…and her story ran",
            "The same trip made, the tin dug, and her fare paid four sim-days ago: her story is on the Galley wire "
            + "(6) and the Comms ticker, the floor's opinion is on a port's rag, and the book has filed that it ran "
            + "(#1202).",
            "/map?dock=selene-gate&press=filed"),
        new("📰⏳", "…and her story is pending",
            "The same trip made, the tin dug, and her fare paid this instant: her story is three sim-days off and "
            + "nothing is against it yet. Open Comms → dark web: the desk carries SPIKE IT with the client's terms; "
            + "take it and the row is gone (#1202).",
            "/map?dock=selene-gate&press=pending"),
        // #1202 slice 2 · SPIKE IT. Her story pending and a spike against it, the stringer writing at the gallery's far
        // table; and the same contract after a window that passed with her pages in the captain's satchel.
        new("📰✂", "Spike it — she writes at the far table",
            "Ashore at Selene Gate in the observation walk's gallery, her story a day from the wire and the client's "
            + "page in your satchel. Rauha Lind writes at the far table and, on her own clock, goes to feed the machine "
            + "behind it: sit at her table while she is away and the card carries TAKE THE PAGES; then LEAVE YOUR PAGE "
            + "(#1202).",
            "/map?dock=selene-gate&ashore=1&spike=1"),
        new("📰🕳", "…and the story did not run",
            "The same contract four sim-days on, her pages in your satchel: the book has the hole under the absence mark, the gallery "
            + "says the recorder is left on the table, and the dark-web desk's next pulse pays the purse on 💳 (#1202).",
            "/map?dock=selene-gate&ashore=1&spike=spiked"),
        // #1202 slice 4 · CHARGED TO PRESERVATION. A SPIKED window this instant and the desk already paid: the office's
        // receipt in the satchel, the rag's line a cycle off.
        new("📰📋", "…and the office has paid",
            "A SPIKED window this instant and the dark-web desk has already paid: the purse on 💳, a line item in your "
            + "satchel and in the book under the Authority and the story's body. Sit at a bar table a day on and the "
            + "port rag has noticed the hole (#1202).",
            "/map?dock=selene-gate&ashore=1&spike=paid"),
        // #1202 slice 3 · THE TAIL AT HER TABLE. The same pending spike with a grey coat behind the captain: he is left
        // at the bar so the man comes in after him, and a take the man still holds his band on is SEEN.
        new("🕵📰", "…and a grey coat behind you",
            "The same spike in hand, but ashore at the bar with a man following you. Walk out along the observation "
            + "walk with him behind you and take her pages while he is still on you: the book files the coat's line "
            + "beside the take, and her story runs on time whatever you leave on her stack. Lose him first and the take "
            + "is clean (#1202).",
            "/map?dock=selene-gate&ashore=1&spike=1&tailed=1"),
    ];
}
