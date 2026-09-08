using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

public class MetinSaglayiciTestleri
{
    [Fact]
    public void VarsayilanDilTurkcedir()
    {
        Assert.Equal("tr", new MetinSaglayici().AktifDil);
    }

    [Fact]
    public void BilinenAnahtarMetinDoner()
    {
        var metin = new MetinSaglayici().Al("adim.karsilama.baslik");

        Assert.NotEmpty(metin);
        Assert.DoesNotContain("adim.karsilama", metin);
    }

    [Fact]
    public void DilDegistirinceIngilizceDoner()
    {
        var saglayici = new MetinSaglayici();
        var turkce = saglayici.Al("adim.karsilama.baslik");

        saglayici.DilDegistir("en");

        Assert.NotEqual(turkce, saglayici.Al("adim.karsilama.baslik"));
        Assert.Equal("en", saglayici.AktifDil);
    }

    [Fact]
    public void TanimayanAnahtarAnahtarinKendisiniDoner()
    {
        Assert.Equal("olmayan.anahtar", new MetinSaglayici().Al("olmayan.anahtar"));
    }

    [Fact]
    public void BicimlendirmeDegerleriYerlestirilir()
    {
        var metin = new MetinSaglayici().Al("disk.silinecek", "SanDisk Ultra 32 GB");

        Assert.Contains("SanDisk Ultra 32 GB", metin);
        Assert.DoesNotContain("{0}", metin);
    }

    [Fact]
    public void IkiDilAyniAnahtarlariIcerir()
    {
        var tr = MetinSaglayici.AnahtarlariListele("tr").OrderBy(a => a, StringComparer.Ordinal);
        var en = MetinSaglayici.AnahtarlariListele("en").OrderBy(a => a, StringComparer.Ordinal);

        Assert.Equal(tr, en);
    }

    [Fact]
    public void DesteklenmeyenDilTurkceyeDuser()
    {
        var saglayici = new MetinSaglayici();

        saglayici.DilDegistir("de");

        Assert.Equal("tr", saglayici.AktifDil);
    }

    [Fact]
    public void TurkceMetinlerTurkceKarakterIcerir()
    {
        // Arayuz metinleri duzgun Turkce olmali; asciye indirgenmemeli.
        var saglayici = new MetinSaglayici();
        char[] turkceHarfler = ['ç', 'ğ', 'ı', 'ö', 'ş', 'ü', 'İ', 'Ç', 'Ğ', 'Ö', 'Ş', 'Ü'];

        var tumMetin = string.Concat(
            MetinSaglayici.AnahtarlariListele("tr").Select(saglayici.Al));

        Assert.Contains(tumMetin, c => turkceHarfler.Contains(c));
    }

    [Theory]
    [InlineData("adim.karsilama.baslik")]
    [InlineData("dugme.iptal")]
    [InlineData("disk.silinecek")]
    [InlineData("hakkinda.baslik")]
    public void OnemliTurkceMetinlerDogruYazilmis(string anahtar)
    {
        var metin = new MetinSaglayici().Al(anahtar);

        Assert.NotEqual(anahtar, metin);
        Assert.NotEmpty(metin);
    }

    [Fact]
    public void EksikDegerVerilirseKalipCokmez()
    {
        // "{0}" bekleyen bir kalibi degersiz cagirmak istisna firlatmamali.
        var metin = new MetinSaglayici().Al("disk.silinecek");

        Assert.NotEmpty(metin);
    }

    /// <summary>
    /// Dil degisince arayuzun kendini yenilemesi gerekir; bunun icin
    /// bir haber lazim. Olaysiz bir degisimde ekrandaki metinler eski
    /// dilde kalirdi.
    /// </summary>
    [Fact]
    public void DilDegisinceOlayTetiklenir()
    {
        var saglayici = new MetinSaglayici();
        var sayac = 0;

        saglayici.DilDegisti += (_, _) => sayac++;
        saglayici.DilDegistir("en");

        Assert.Equal(1, sayac);
    }

    [Fact]
    public void AyniDileGecinceOlayTetiklenmez()
    {
        var saglayici = new MetinSaglayici();
        var sayac = 0;

        saglayici.DilDegisti += (_, _) => sayac++;
        saglayici.DilDegistir("tr");   // zaten Turkce

        Assert.Equal(0, sayac);
    }

    [Fact]
    public void DesteklenmeyenDildeOlayTetiklenmez()
    {
        var saglayici = new MetinSaglayici();
        var sayac = 0;

        saglayici.DilDegisti += (_, _) => sayac++;
        saglayici.DilDegistir("de");   // Turkce'ye duser, degisiklik yok

        Assert.Equal(0, sayac);
    }

    [Fact]
    public void DesteklenenDillerListelenir()
    {
        var diller = MetinSaglayici.DilleriListele();

        Assert.Contains("tr", diller);
        Assert.Contains("en", diller);
        Assert.Equal(2, diller.Count);
    }
}
