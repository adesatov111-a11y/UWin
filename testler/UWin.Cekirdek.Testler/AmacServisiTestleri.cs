using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Kurulum amaci, programin verdigi en kritik tavsiyeyi tersine
/// cevirebilir. Bu testler zit tavsiyelerin karismamasini korur.
/// </summary>
public class AmacServisiTestleri
{
    private static AmacTavsiyesi Tavsiye(KurulumAmaci amac)
        => new AmacServisi().TavsiyeGetir(amac);

    [Fact]
    public void HerAmacIcinTavsiyeUretilir()
    {
        foreach (var amac in Enum.GetValues<KurulumAmaci>())
        {
            var t = Tavsiye(amac);

            Assert.Equal(amac, t.Amac);
            Assert.NotEmpty(t.Baslik);
            Assert.NotEmpty(t.DiskEkraniTavsiyesi);
        }
    }

    /// <summary>
    /// Virus icin kuran biri bolumu silmelidir. Windows.old birakan bir
    /// kurulum zararliyi oldugu gibi tasir: kullanici dosyalarini oradan
    /// geri kopyalayinca virus de geri gelir.
    /// </summary>
    [Fact]
    public void VirusAmaciBolumunSilinmesiniIster()
    {
        var t = Tavsiye(KurulumAmaci.Virus);

        Assert.True(t.BolumSilinmeli);
    }

    /// <summary>
    /// Surum yukseltmede amac dosyalari korumaktir; silmek geri
    /// alinamaz bir kayip olur.
    /// </summary>
    [Fact]
    public void SurumYukseltmeBolumuSilmez()
    {
        Assert.False(Tavsiye(KurulumAmaci.SurumYukseltme).BolumSilinmeli);
    }

    [Fact]
    public void BelirtilmemisAmacTemkinliDavranir()
    {
        // Emin olmadigimizda veri silmeyi onermeyiz.
        Assert.False(Tavsiye(KurulumAmaci.Belirtilmemis).BolumSilinmeli);
    }

    /// <summary>
    /// Virus tavsiyesi diger bolumleri de hatirlatmali: C: silinse bile
    /// D:'deki bulasik dosya yeni Windows acilinca tekrar calisir.
    /// </summary>
    [Fact]
    public void VirusTavsiyesiDigerBolumleriDeAnlatir()
    {
        var t = Tavsiye(KurulumAmaci.Virus);

        Assert.NotEmpty(t.EkAdimlar);
        Assert.Contains(t.EkAdimlar, a => a.Contains("bölüm", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VirusTavsiyesiYedeklenenDosyalariTaramayiSoyler()
    {
        var t = Tavsiye(KurulumAmaci.Virus);

        Assert.Contains(t.EkAdimlar, a => a.Contains("tara", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void YeniDiskAmacindaSilinecekVeriUyarisiGereksizdir()
    {
        var t = Tavsiye(KurulumAmaci.YeniDisk);

        // Bos diskte veri kaybi yok; yine de bolum silmek normaldir.
        Assert.True(t.BolumSilinmeli);
    }

    /// <summary>
    /// Silme oneren her tavsiye, kullaniciyi once yedege yonlendirmeli.
    /// "Sil" demek kolaydir; ne kaybedecegini soylemeden demek degildir.
    /// </summary>
    [Fact]
    public void SilmeOnerenTavsiyelerYedegiHatirlatir()
    {
        foreach (var amac in Enum.GetValues<KurulumAmaci>())
        {
            var t = Tavsiye(amac);

            if (!t.BolumSilinmeli || amac == KurulumAmaci.YeniDisk)
                continue;

            Assert.True(
                t.EkAdimlar.Any(a => a.Contains("yedek", StringComparison.OrdinalIgnoreCase))
                || t.DiskEkraniTavsiyesi.Contains("yedek", StringComparison.OrdinalIgnoreCase),
                $"{amac} silmeyi oneriyor ama yedekten soz etmiyor");
        }
    }

    [Fact]
    public void TavsiyelerBirbirindenFarklidir()
    {
        var metinler = Enum.GetValues<KurulumAmaci>()
            .Select(a => Tavsiye(a).DiskEkraniTavsiyesi)
            .ToList();

        Assert.Equal(metinler.Count, metinler.Distinct().Count());
    }
}
