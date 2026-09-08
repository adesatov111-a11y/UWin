using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

public class SurumServisiTestleri
{
    [Fact]
    public async Task GomuluKatalogInternetsizListelenir()
    {
        var surumler = await new SurumServisi(httpIstemci: null).SurumleriGetirAsync();

        Assert.NotEmpty(surumler);
        Assert.Contains(surumler, s => s.Kimlik == "win11-25h2");
        Assert.Contains(surumler, s => s.Kimlik == "win10-22h2");
    }

    [Fact]
    public async Task HerSurumTurkceVeIngilizceIcerir()
    {
        var surumler = await new SurumServisi(httpIstemci: null).SurumleriGetirAsync();

        Assert.Contains(surumler, s => s.Dil == "tr-TR");
        Assert.Contains(surumler, s => s.Dil == "en-US");
    }

    [Fact]
    public async Task InternetYokkenBaglantiCozumuBasarisiz()
    {
        var sonuc = await new SurumServisi(httpIstemci: null).BaglantiCozAsync("win11-25h2", "tr-TR", "x64");

        Assert.False(sonuc.Basarili);
        Assert.Equal(BaglantiDurumu.Erisilemedi, sonuc.Durum);
        Assert.Null(sonuc.Surum);
    }

    [Fact]
    public void TanimayanOzetNullDoner()
    {
        var servis = new SurumServisi(httpIstemci: null);

        Assert.Null(servis.OzetIleTani(new string('0', 64)));
    }

    [Fact]
    public async Task GecersizKimlikBasarisizDoner()
    {
        var sonuc = await new SurumServisi(httpIstemci: null).BaglantiCozAsync("olmayan-surum", "tr-TR", "x64");

        Assert.False(sonuc.Basarili);
        Assert.Null(sonuc.Surum);
    }

    [Fact]
    public async Task TamAdOkunurBicimdedir()
    {
        var surumler = await new SurumServisi(httpIstemci: null).SurumleriGetirAsync();
        var win11 = surumler.First(s => s.Kimlik == "win11-25h2" && s.Dil == "tr-TR" && s.Mimari == "x64");

        Assert.Equal("Windows 11 25H2 - Turkce (64 bit)", win11.TamAd);
    }
}
