using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #313 · THE LONELY AUTOMATED KIOSK — a PLACE has shops. Pulse receipts in the #119 style, house voice,
/// last restocked before the war.
///
/// <para>Split out of <c>Map.Surface.Hud.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered. Its <c>KioskStock</c> is the family's one static field and reads nothing but literals, so
/// it travels with its only reader — see <c>NoPartialClassSpreadsItsStaticFieldsTests</c>.</para>
/// </summary>
public partial class Map
{
    // ── The lonely automated kiosk (#313 amenity): a PLACE has shops. Pulse receipts (#119 style),
    //    house voice — last restocked before the war. ──

    // Slot 0 is the souvenir tee — its item + gag are filled from the moon underfoot at buy time
    // (SurfaceSouvenir), so Ganymede sells a Ganymede shirt, not Miranda's (#379). The placeholder
    // strings below are never shown; they only hold slot 0's price and mark the seam.
    // Owner, 2026-07-28: "The T-shirts etc everywhere where they are missing." Every HAVEN gift shop has
    // painted a tee AND a magnet since #367; the GROUND kiosk sold both and showed neither — a pulse line
    // and nothing to look at. The art column closes that: what you bought now gets held up.
    private static readonly (string Item, int Price, string Line, string Art)[] KioskStock =
    [
        ("the local souvenir tee", 15, "(keyed to the walked body — see VisitKiosk)",
            "art/souvenir-surface-tshirt.jpg"),
        ("a fridge magnet", 8, "It clamps to your suit's chestplate and refuses to let go. Value: eternal.",
            "art/souvenir-surface-magnet.jpg"),
        ("a vacuum-sealed hot meal", 12, "The label promises 'MEAT-ADJACENT'. The heater still works. Mostly.",
            ""), // no art — it is a ration pouch, and the joke is funnier unseen
    ];

    private int _kioskPicks;

    /// <summary>What the kiosk just sold you, held up for a look — the ground's answer to the haven gift
    /// shops' view-object cards. Null when nothing is being inspected.</summary>
    public readonly record struct KioskBuy(string Item, string Line, string Art);

    private KioskBuy? _kioskCard;

    private void CloseKioskCard() => _kioskCard = null;

    private void VisitKiosk()
    {
        if (_surface is not { } ex)
        {
            return; // the kiosk only sells on the ground it stands on
        }
        int slot = _kioskPicks % KioskStock.Length;
        (string item, int price, string line, string art) = KioskStock[slot];
        _kioskPicks++;
        if (slot == 0)
        {
            // The souvenir tee, keyed to the moon actually underfoot (#379): Ganymede's kiosk prints a
            // Ganymede shirt; Miranda keeps its canon line. Copy is generated, so any landable body works.
            CelestialBody body = ex.Stop.Body;
            item = SurfaceSouvenir.TeeItem(body.Name);
            line = SurfaceSouvenir.TeeGag(body.Id, body.Name);
        }
        if (_credits < price)
        {
            ShowPulseMessage($"🛒 {item} — {price} cr. The slot blinks INSUFFICIENT FUNDS in a dead language. Empty pockets, captain.");
            return;
        }
        _credits -= price;
        RendererInterop.PlayCue("board");
        ShowPulseMessage($"🧾 Bought {item} for {price} cr. {line} (The kiosk was last restocked before the war.)");
        if (art.Length > 0)
        {
            // Hold it up. The img onerror-hides, so an unpainted slot still degrades to the caption alone.
            _kioskCard = new KioskBuy(item, line, art);
        }
    }
}
