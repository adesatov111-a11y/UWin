using UWin.Uygulama.Bicimlendirme;

namespace UWin.Uygulama.Testler;

public class BaytBicimlendiriciTestleri
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(999, "999 B")]
    [InlineData(32_000_000_000, "32,0 GB")]
    [InlineData(2_000_000_000_000, "2,00 TB")]
    [InlineData(12_400_000_000, "12,4 GB")]
    [InlineData(4_000_000_000, "4,0 GB")]
    public void BaytlariOkunurBicimeCevirir(long bayt, string beklenen)
    {
        Assert.Equal(beklenen, BaytBicimlendirici.Bicimle(bayt));
    }

    [Fact]
    public void NegatifDegerSifirGosterir()
    {
        Assert.Equal("0 B", BaytBicimlendirici.Bicimle(-5));
    }

    [Theory]
    [InlineData(17_179_869_184, "16 GB")]   // tam 16 GiB
    [InlineData(17_035_476_992, "16 GB")]   // entegre grafik dusulmus 16 GB
    [InlineData(8_589_934_592, "8 GB")]
    [InlineData(34_359_738_368, "32 GB")]
    public void BellekIkiliBirimleVeYuvarlanarakGosterilir(long bayt, string beklenen)
    {
        // RAM 2^30 ile olculur; disk birimi kullanilirsa 16 GB modul 17,2 GB gorunur.
        Assert.Equal(beklenen, BaytBicimlendirici.BellekBicimle(bayt));
    }

    [Fact]
    public void SifirBellekSifirGosterir()
    {
        Assert.Equal("0 GB", BaytBicimlendirici.BellekBicimle(0));
    }
}
