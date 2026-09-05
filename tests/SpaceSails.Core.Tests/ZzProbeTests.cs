namespace SpaceSails.Core.Tests;
public class ZzProbeTests
{
    [Fact]
    public void Probe()
    {
        ShipHistory h = ShipHistories.Hers;
        File.WriteAllText(@"C:\Users\ernos\AppData\Local\Temp\claude\probe426.txt",
            $"YARD={h.Yard}|YEAR={h.Year}|OWNERS={h.OwnersDeep}|FORMER={h.FormerNamesLine}|GLORY={h.GloryName}");
    }
}
