using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Kurtarma modu, ayni USB'yi "acilmayan Windows'u onarma" araci
/// haline getirir. Testler hem arac listesinin tutarliligini hem de
/// USB'ye yazilan dosyalarin dogrulugunu korur.
/// </summary>
public class KurtarmaServisiTestleri : IDisposable
{
    private readonly string _usb =
        Path.Combine(Path.GetTempPath(), "uwin-kurtarma-" + Guid.NewGuid().ToString("N"));

    public KurtarmaServisiTestleri() => Directory.CreateDirectory(_usb);

    public void Dispose()
    {
        if (Directory.Exists(_usb))
            Directory.Delete(_usb, recursive: true);
    }

    [Fact]
    public void AraclarListelenir()
    {
        var araclar = new KurtarmaServisi().AraclariGetir();

        Assert.NotEmpty(araclar);
    }

    [Fact]
    public void HerAracinKimligiBenzersizdir()
    {
        var araclar = new KurtarmaServisi().AraclariGetir();

        Assert.Equal(araclar.Count, araclar.Select(a => a.Kimlik).Distinct().Count());
    }

    /// <summary>
    /// Her aracin "ne zaman kullanilir" acikamasi olmali. Komut listesi
    /// vermek kolaydir; kullaniciya hangi durumda hangisini secmesi
    /// gerektigini soylemeyen bir liste ise ise yaramaz.
    /// </summary>
    [Fact]
    public void HerAracNeZamanKullanilacaginiSoyler()
    {
        var araclar = new KurtarmaServisi().AraclariGetir();

        Assert.All(araclar, a =>
        {
            Assert.NotEmpty(a.Ad);
            Assert.NotEmpty(a.NeZamanKullanilir);
            Assert.NotEmpty(a.Komut);
        });
    }

    /// <summary>
    /// En cok aranan uc onarim: onyukleme kaydi, sistem dosyasi
    /// butunlugu ve disk hatasi. Bunlar olmadan kurtarma modu eksik.
    /// </summary>
    [Theory]
    [InlineData("bootrec")]
    [InlineData("sfc")]
    [InlineData("chkdsk")]
    public void TemelOnarimAraclariBulunur(string kimlik)
    {
        var araclar = new KurtarmaServisi().AraclariGetir();

        Assert.Contains(araclar, a => a.Kimlik == kimlik);
    }

    /// <summary>
    /// Diske yazan araclar isaretli olmali: arayuz bunlari ayri
    /// gosterip kullaniciyi uyarabilsin.
    /// </summary>
    [Fact]
    public void DiskeYazanAraclarIsaretlidir()
    {
        var araclar = new KurtarmaServisi().AraclariGetir();

        Assert.Contains(araclar, a => a.DiskeYazar);
        Assert.Contains(araclar, a => !a.DiskeYazar);
    }

    [Fact]
    public async Task AraclarUsbyeYazilir()
    {
        var sonuc = await new KurtarmaServisi().UsbyeYazAsync(_usb);

        Assert.True(sonuc.Basarili);
        Assert.NotNull(sonuc.KlasorYolu);
        Assert.True(Directory.Exists(sonuc.KlasorYolu));
    }

    [Fact]
    public async Task HerAracIcinBatDosyasiYazilir()
    {
        var servis = new KurtarmaServisi();

        var sonuc = await servis.UsbyeYazAsync(_usb);

        foreach (var arac in servis.AraclariGetir())
        {
            var dosya = Path.Combine(sonuc.KlasorYolu!, $"{arac.Kimlik}.bat");
            Assert.True(File.Exists(dosya), $"{arac.Kimlik}.bat yazilmali");
        }
    }

    /// <summary>
    /// .bat dosyalari Komut Istemi tarafindan okunur ve cmd.exe satir
    /// sonu olarak CRLF bekler. LF ile yazilan bir .bat "komut taninmadi"
    /// hatasi verir - bu hata daha once yayinla.bat'ta yasandi.
    /// </summary>
    [Fact]
    public async Task BatDosyalariCrlfSatirSonuKullanir()
    {
        var servis = new KurtarmaServisi();
        var sonuc = await servis.UsbyeYazAsync(_usb);

        var ilk = servis.AraclariGetir()[0];
        var icerik = await File.ReadAllTextAsync(Path.Combine(sonuc.KlasorYolu!, $"{ilk.Kimlik}.bat"));

        Assert.Contains("\r\n", icerik);
        Assert.DoesNotContain(icerik.Replace("\r\n", ""), "\n");
    }

    /// <summary>
    /// .bat icerigi ASCII olmali: cmd.exe varsayilan olarak UTF-8
    /// okumaz ve Turkce karakterler bozuk gorunur.
    /// </summary>
    [Fact]
    public async Task BatDosyalariAsciiKarakterKullanir()
    {
        var servis = new KurtarmaServisi();
        var sonuc = await servis.UsbyeYazAsync(_usb);

        foreach (var arac in servis.AraclariGetir())
        {
            var bayt = await File.ReadAllBytesAsync(
                Path.Combine(sonuc.KlasorYolu!, $"{arac.Kimlik}.bat"));

            Assert.All(bayt, b => Assert.True(b < 128, $"{arac.Kimlik}.bat ASCII disi bayt tasiyor"));
        }
    }

    [Fact]
    public async Task OkunabilirRehberYazilir()
    {
        var sonuc = await new KurtarmaServisi().UsbyeYazAsync(_usb);

        var rehber = Path.Combine(sonuc.KlasorYolu!, "OKU-BENI.html");

        Assert.True(File.Exists(rehber));

        var icerik = await File.ReadAllTextAsync(rehber);
        Assert.Contains("<!DOCTYPE html>", icerik, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RehberTumAraclariAnlatir()
    {
        var servis = new KurtarmaServisi();
        var sonuc = await servis.UsbyeYazAsync(_usb);

        var icerik = await File.ReadAllTextAsync(Path.Combine(sonuc.KlasorYolu!, "OKU-BENI.html"));

        foreach (var arac in servis.AraclariGetir())
            Assert.Contains(arac.Ad, icerik, StringComparison.Ordinal);
    }

    [Fact]
    public async Task YazilamayanYolHataDoner()
    {
        var sonuc = await new KurtarmaServisi().UsbyeYazAsync(
            Path.Combine(_usb, "olmayan", "\u0000gecersiz"));

        Assert.False(sonuc.Basarili);
        Assert.NotNull(sonuc.Hata);
        Assert.NotEmpty(sonuc.Hata.NeYapmali);
    }

    [Fact]
    public async Task AyniUsbyeIkiKereYazilabilir()
    {
        var servis = new KurtarmaServisi();

        await servis.UsbyeYazAsync(_usb);
        var ikinci = await servis.UsbyeYazAsync(_usb);

        Assert.True(ikinci.Basarili);
    }
}
