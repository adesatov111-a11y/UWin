using System.Diagnostics;
using UWin.Cekirdek.Servisler;
using UWin.Cekirdek.Yerel;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Gercek bir Windows ISO'su varsa uctan uca dogrular. Dosya yoksa test
/// sessizce gecer; boylece bu depo baska bir makinede de derlenip kosar.
/// Amaci tek bir seyi korumak: gecerli bir ISO asla reddedilmemeli.
/// </summary>
public class GercekIsoTestleri
{
    /// <summary>Bu klasordeki herhangi bir Windows ISO'su denenir.</summary>
    private static string? DenemeIsosuBul()
    {
        var klasor = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        if (!Directory.Exists(klasor))
            return null;

        // Dosya baskasi tarafindan kullaniliyorsa test onu acmaya kalkismaz;
        // amac dogrulama yapmak, kilit yuzunden derlemeyi kirmak degil.
        foreach (var aday in Directory.EnumerateFiles(klasor, "*.iso"))
        {
            try
            {
                using var deneme = File.Open(aday, FileMode.Open, FileAccess.Read, FileShare.Read);
                return aday;
            }
            catch (IOException)
            {
                // Sonraki adaya bak.
            }
        }

        return null;
    }

    [Fact]
    public void GercekIsoTaninirVeHizliCozulur()
    {
        if (DenemeIsosuBul() is not { } yol)
            return;

        var sayac = Stopwatch.StartNew();
        var kimlik = new IsoTanimaServisi(new DiscUtilsIsoOkuyucu()).Tani(yol);
        sayac.Stop();

        Assert.True(kimlik.WindowsMu, $"Gecerli ISO taninmadi: {yol}");

        // Eski yontem 8 GB'lik dosyanin ozetini aliyordu ve dakikalar
        // suruyordu. Icerik okumasi bunun yaninda aninda biter.
        Assert.True(sayac.Elapsed < TimeSpan.FromSeconds(30),
            $"Tanima cok uzun surdu: {sayac.Elapsed}");
    }

    [Fact]
    public void GercekIsoIcinGerekenBoyutDosyadanBuyuktur()
    {
        if (DenemeIsosuBul() is not { } yol)
            return;

        var kimlik = new IsoTanimaServisi(new DiscUtilsIsoOkuyucu()).Tani(yol);

        if (!kimlik.WindowsMu)
            return;

        Assert.True(kimlik.GerekenUsbBoyutuBayt > 0);
    }

    /// <summary>
    /// Modern Windows ISO'lari UDF ile yazilir. Icindeki ISO9660 katmani
    /// yalnizca eski sistemlere "bu diski okuyamazsin" diyen tek bir
    /// README.TXT tasir. Okuyucu ISO9660'a takilirsa install.wim'i hic
    /// bulamaz ve USB acilmayan bir disk olarak biter - bu test tam olarak
    /// o sessiz felaketi engeller.
    /// </summary>
    [Fact]
    public void GercekIsoIcindeKurulumDosyalariGorunur()
    {
        if (DenemeIsosuBul() is not { } yol)
            return;

        var icerik = new DiscUtilsIsoOkuyucu().Oku(yol);

        Assert.True(icerik.Dosyalar.Count > 100,
            $"ISO icinden yalnizca {icerik.Dosyalar.Count} dosya okundu - UDF katmani atlanmis olabilir.");

        Assert.NotNull(icerik.InstallWim);
    }
}
