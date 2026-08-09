using System;
using System.Globalization;
using System.IO;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

public sealed class ZzDumpTests
{
    [Fact]
    public void Dump()
    {
        var sb = new StringBuilder();
        SurfaceLayout.Field f = SurfaceLayout.DefaultField;
        double margin = SurfaceLayout.EdgeMargin + 6;
        (double sx, double sy) = UndergroundComplex.ShaftAt(f);
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"field L{f.LeftX} R{f.RightX} B{f.BottomY} Land{f.LandingBandY} left={f.LeftX + margin} right={f.RightX - margin}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"shaft {sx},{sy}  service={UndergroundComplex.ServiceShaftAt(f)}");
        foreach ((int o, double x) in UndergroundComplex.RibColumnsOn(f))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"rib slot {o} x={x}");
        }
        sb.AppendLine(CultureInfo.InvariantCulture, $"RoomWidthDu={UndergroundComplex.RoomWidthDu} RoomHeightDu={UndergroundComplex.RoomHeightDu}");

        foreach (string body in new[] { "luna", "phobos", "europa", "titan", "probe-moon-3", "probe-moon-7" })
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }
            UndergroundComplex.FloorPlan p = UndergroundComplex.Build(body, level, f);
            sb.AppendLine(CultureInfo.InvariantCulture, $"--- {body} B{-level} rooms={p.RoomCentres.Count} ribs={p.Ribs.Count}");
            foreach (UndergroundComplex.Rib r in p.Ribs)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"   rib x={r.X:F1} down={r.Down}");
            }
            foreach (UndergroundComplex.Amenity a in p.Amenities)
            {
                if (a.Hall is { } h)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture,
                        $"   hall {a.Use} box=({h.X0:F1},{h.Y0:F1})-({h.X1:F1},{h.Y1:F1}) w={h.X1 - h.X0:F1} d={h.Y1 - h.Y0:F1} area={h.FloorDu2:F0} seats={h.SeatTarget} cabs={h.Cabinets.Count} doors={h.Openings.Count}");
                }
            }
            if (p.Park is { } pk)
            {
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"   park box=({pk.X0:F1},{pk.Y0:F1})-({pk.X1:F1},{pk.Y1:F1}) w={pk.X1 - pk.X0:F1} d={pk.Y1 - pk.Y0:F1} area={pk.FloorDu2:F0} gates={pk.Ways.Count} back={pk.Rooms.Count} beds={pk.Beds.Count} benches={pk.Benches.Count} masts={pk.Masts.Count}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"   ring={pk.Frontage.Count} windows={p.Windows!.Count}");
                int extra = 0;
                foreach (UndergroundComplex.Amenity am in p.Amenities)
                {
                    if (am.Hall is { } hh) { extra += hh.Openings.Count - 1; }
                }
                int places = p.RoomCentres.Count + p.Refuges.Count + p.Amenities.Count + extra + pk.Ways.Count;
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"   CONSERVATION doorways={p.Doorways.Count} rooms={p.RoomCentres.Count} refuges={p.Refuges.Count} amen={p.Amenities.Count} extraHall={extra} ways={pk.Ways.Count} places={places}");
                foreach ((double cx, double cy) in p.RoomCentres)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"     ROOM ({cx:F1},{cy:F1})");
                }
                foreach (UndergroundComplex.Amenity am2 in p.Amenities)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"     AMEN {am2.Use} ({am2.X:F1},{am2.Y:F1}) hall={am2.Hall is not null}");
                }
                foreach (SurfaceLayout.Doorway dd in p.Doorways)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"     door ({dd.X1:F1},{dd.Y1:F1})-({dd.X2:F1},{dd.Y2:F1})");
                }
                foreach (UndergroundComplex.RingRoom r in pk.Frontage)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture,
                        $"     ring {r.Number} {r.Side} ({r.X0:F1},{r.Y0:F1})-({r.X1:F1},{r.Y1:F1}) {r.FloorDu2:F0}du2 view={r.HasView} gate={r.Gate is not null} {r.Plate}");
                }
            }
        }
        File.WriteAllText(@"D:\repo12\wt-813\dump.txt", sb.ToString());
    }
}
