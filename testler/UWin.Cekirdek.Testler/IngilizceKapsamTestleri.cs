using System.Text.RegularExpressions;
using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Ingilizce arayuzun gercekten Ingilizce oldugunu dogrular.
///
/// Metin dosyalari iki dilde olmasi tek basina yetmiyordu: servisler
/// metinlerini koda gomulu tasidigi surece dil degistirmek yalnizca
/// dugme yazilarini degistiriyor, hata mesajlari ve tavsiyeler Turkce
/// kaliyordu. Bu testler o durumun geri gelmesini engeller.
/// </summary>
public class IngilizceKapsamTestleri
{
    /// <summary>Turkce'ye ozgu harfler; Ingilizce metinde bulunmamali.</summary>
    private static readonly Regex TurkceHarf = new("[ğüşıöçĞÜŞİÖÇ]", RegexOptions.Compiled);

    private static MetinSaglayici Ingilizce()
    {
        var metinler = new MetinSaglayici();
        metinler.DilDegistir("en");
        return metinler;
    }

    private static DonanimRaporu Donanim(
        string? tpm = null, bool uefi = true, bool secureBoot = false,
        long ram = 2L * 1024 * 1024 * 1024, long disk = 32L * 1024 * 1024 * 1024)
        => new("Gigabyte", "A520M K V2", "AMD Ryzen 5 5600", ram, tpm, uefi, secureBoot, disk);

    private static void TurkceIcermemeli(string metin, string nerede)
        => Assert.False(TurkceHarf.IsMatch(metin), $"{nerede} Türkçe harf taşıyor: {metin}");

    [Fact]
    public void UyumlulukRaporuIngilizceDoner()
    {
        var rapor = new UyumlulukServisi(Ingilizce()).Degerlendir(Donanim());

        foreach (var madde in rapor.Maddeler)
        {
            TurkceIcermemeli(madde.Ad, "Madde adı");
            TurkceIcermemeli(madde.Aciklama, $"{madde.Ad} açıklaması");

            if (madde.NeYapmali is { } yapmali)
                TurkceIcermemeli(yapmali, $"{madde.Ad} tavsiyesi");
        }
    }

    /// <summary>
    /// Uyumlu bir makinede de metinler Ingilizce olmali - "gecti"
    /// durumundaki aciklamalar da kullaniciya gorunuyor.
    /// </summary>
    [Fact]
    public void UyumluMakineninRaporuDaIngilizce()
    {
        var uyumlu = new DonanimRaporu(
            "MSI", "B550", "Intel Core i5", 16L * 1024 * 1024 * 1024,
            "2.0", true, true, 512L * 1000 * 1000 * 1000);

        var rapor = new UyumlulukServisi(Ingilizce()).Degerlendir(uyumlu);

        Assert.True(rapor.Uyumlu);

        foreach (var madde in rapor.Maddeler)
            TurkceIcermemeli(madde.Aciklama, madde.Ad);
    }

    [Theory]
    [InlineData(KurulumAmaci.Virus)]
    [InlineData(KurulumAmaci.Yavaslik)]
    [InlineData(KurulumAmaci.YeniDisk)]
    [InlineData(KurulumAmaci.SurumYukseltme)]
    [InlineData(KurulumAmaci.Belirtilmemis)]
    public void AmacTavsiyeleriIngilizceDoner(KurulumAmaci amac)
    {
        var tavsiye = new AmacServisi(Ingilizce()).TavsiyeGetir(amac);

        TurkceIcermemeli(tavsiye.Baslik, $"{amac} başlığı");
        TurkceIcermemeli(tavsiye.DiskEkraniTavsiyesi, $"{amac} disk tavsiyesi");

        foreach (var adim in tavsiye.EkAdimlar)
            TurkceIcermemeli(adim, $"{amac} ek adımı");
    }

    [Fact]
    public void AmacTavsiyeleriIngilizcedeDeBosDegil()
    {
        // Anahtar bulunamazsa Al() anahtarin kendisini doner; "tavsiye.virus.1"
        // gibi bir metin ekranda gorunurdu. Nokta iceren bir metin bunu ele verir.
        var tavsiye = new AmacServisi(Ingilizce()).TavsiyeGetir(KurulumAmaci.Virus);

        Assert.DoesNotContain("tavsiye.", tavsiye.DiskEkraniTavsiyesi, StringComparison.Ordinal);
        Assert.All(tavsiye.EkAdimlar,
            a => Assert.DoesNotContain("tavsiye.", a, StringComparison.Ordinal));
    }

    [Fact]
    public void KurtarmaAraclariIngilizceDoner()
    {
        var araclar = new KurtarmaServisi(Ingilizce()).AraclariGetir();

        foreach (var arac in araclar)
        {
            TurkceIcermemeli(arac.Ad, $"{arac.Kimlik} adı");
            TurkceIcermemeli(arac.NeZamanKullanilir, $"{arac.Kimlik} açıklaması");

            Assert.DoesNotContain("kurtarma.", arac.Ad, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void KullaniciKlasorleriIngilizceAdlanir()
    {
        var klasorler = VeriServisi.VarsayilanKlasorler(Ingilizce());

        foreach (var (ad, _) in klasorler)
        {
            TurkceIcermemeli(ad, "Klasör adı");
            Assert.DoesNotContain("klasor.", ad, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Turkce secildiginde de metinler Turkce olmali - Ingilizce'yi
    /// duzeltirken Turkce'yi bozmadigimizin karsi kontrolu.
    /// </summary>
    [Fact]
    public void TurkceSecilinceTurkceDoner()
    {
        var metinler = new MetinSaglayici();
        var tavsiye = new AmacServisi(metinler).TavsiyeGetir(KurulumAmaci.Virus);

        Assert.True(TurkceHarf.IsMatch(tavsiye.DiskEkraniTavsiyesi),
            "Türkçe tavsiye Türkçe harf içermeli");
    }

    [Fact]
    public void KurtarmaHtmlIngilizceUretilir()
    {
        var klasor = Path.Combine(Path.GetTempPath(), "uwin-en-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(klasor);

        try
        {
            var sonuc = new KurtarmaServisi(Ingilizce()).UsbyeYazAsync(klasor).Result;

            Assert.True(sonuc.Basarili);

            var html = File.ReadAllText(Path.Combine(sonuc.KlasorYolu!, "OKU-BENI.html"));

            Assert.Contains("lang=\"en\"", html, StringComparison.Ordinal);

            // Stil ve baslik disindaki govde metni Turkce harf tasimamali.
            var govde = html[html.IndexOf("<body>", StringComparison.Ordinal)..];
            TurkceIcermemeli(govde, "Kurtarma rehberi gövdesi");
        }
        finally
        {
            Directory.Delete(klasor, recursive: true);
        }
    }

    /// <summary>
    /// Paylasilan saglayicida dil degisince servislerin ciktisi da
    /// degismeli.
    ///
    /// Bu, gercek bir hatanin testi: uygulama servisleri tek bir
    /// saglayiciyla kuruyor ve sayfa uyumluluk raporunu onbellege
    /// aliyordu. Dil Ingilizce'ye gecince arayuz Ingilizce oluyor
    /// ama karne kartlari Turkce kaliyordu.
    /// </summary>
    [Fact]
    public void PaylasilanSaglayicidaDilDegisimiCiktiyaYansir()
    {
        var metinler = new MetinSaglayici();
        var servis = new UyumlulukServisi(metinler);
        var donanim = Donanim(tpm: "2.0", secureBoot: true);

        var turkce = servis.Degerlendir(donanim).Maddeler[0].Aciklama;

        metinler.DilDegistir("en");
        var ingilizce = servis.Degerlendir(donanim).Maddeler[0].Aciklama;

        Assert.NotEqual(turkce, ingilizce);
        TurkceIcermemeli(ingilizce, "Dil degisiminden sonraki aciklama");
    }

    [Fact]
    public void PaylasilanSaglayicidaTavsiyeDeDegisir()
    {
        var metinler = new MetinSaglayici();
        var servis = new AmacServisi(metinler);

        var turkce = servis.TavsiyeGetir(KurulumAmaci.Virus).DiskEkraniTavsiyesi;

        metinler.DilDegistir("en");
        var ingilizce = servis.TavsiyeGetir(KurulumAmaci.Virus).DiskEkraniTavsiyesi;

        Assert.NotEqual(turkce, ingilizce);
        TurkceIcermemeli(ingilizce, "Dil degisiminden sonraki tavsiye");
    }

    [Fact]
    public void KurtarmaAraclariDilDegisiminiIzler()
    {
        var metinler = new MetinSaglayici();
        var servis = new KurtarmaServisi(metinler);

        var turkce = servis.AraclariGetir()[0].Ad;

        metinler.DilDegistir("en");
        var ingilizce = servis.AraclariGetir()[0].Ad;

        Assert.NotEqual(turkce, ingilizce);
        TurkceIcermemeli(ingilizce, "Dil degisiminden sonraki arac adi");
    }
}
