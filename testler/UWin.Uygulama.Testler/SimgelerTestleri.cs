using System.Reflection;

namespace UWin.Uygulama.Testler;

/// <summary>
/// Simgeler derlemeye gomulu olmali. Tek dosya yayininda exe'nin yanina
/// konan dosyalar cikti klasorune tasinmadigi icin, gomulu olmayan bir
/// simge yalnizca yayin surumunde ve sessizce kaybolurdu.
/// </summary>
public class SimgelerTestleri
{
    private static string[] KaynakAdlari() =>
        typeof(UWin.Uygulama.App).Assembly.GetManifestResourceNames();

    [Theory]
    [InlineData("uwin-64.png")]
    [InlineData("uwin.ico")]
    public void SimgeDerlemeyeGomulu(string dosyaAdi)
    {
        Assert.Contains(
            KaynakAdlari(),
            ad => ad.EndsWith(dosyaAdi, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("uwin-64.png")]
    [InlineData("uwin.ico")]
    public void SimgeOkunabilirVeBosDegil(string dosyaAdi)
    {
        var derleme = typeof(UWin.Uygulama.App).Assembly;

        var ad = Assert.Single(
            KaynakAdlari().Where(n => n.EndsWith(dosyaAdi, StringComparison.Ordinal)));

        using var akis = derleme.GetManifestResourceStream(ad);

        Assert.NotNull(akis);
        Assert.True(akis.Length > 0, $"{dosyaAdi} bos.");
    }

    [Fact]
    public void IkonGercektenIcoBicimindedir()
    {
        var derleme = typeof(UWin.Uygulama.App).Assembly;

        var ad = KaynakAdlari().Single(n => n.EndsWith("uwin.ico", StringComparison.Ordinal));

        using var akis = derleme.GetManifestResourceStream(ad)!;

        // ICO basligi: 0x00 0x00 0x01 0x00
        var bas = new byte[4];
        Assert.Equal(4, akis.ReadAtLeast(bas, 4, throwOnEndOfStream: false));
        Assert.Equal([0x00, 0x00, 0x01, 0x00], bas);
    }

    [Fact]
    public void BaslikSimgesiPngBicimindedir()
    {
        var derleme = typeof(UWin.Uygulama.App).Assembly;

        var ad = KaynakAdlari().Single(n => n.EndsWith("uwin-64.png", StringComparison.Ordinal));

        using var akis = derleme.GetManifestResourceStream(ad)!;

        var bas = new byte[4];
        Assert.Equal(4, akis.ReadAtLeast(bas, 4, throwOnEndOfStream: false));
        Assert.Equal([0x89, (byte)'P', (byte)'N', (byte)'G'], bas);
    }
}
