using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

public class RehberServisiTestleri
{
    private static DonanimRaporu OrnekDonanim(string uretici = "MSI") => new(
        AnakartUretici: uretici, AnakartModel: "B550-A PRO", Islemci: "AMD Ryzen 5 5600",
        RamBayt: 17_179_869_184, TpmSurumu: "2.0", UefiMi: true,
        SecureBootDestekli: true, SistemDiskiBoyutBayt: 512_110_190_592);

    [Theory]
    [InlineData("MSI", "DELETE")]
    [InlineData("ASUS", "DELETE veya F2")]
    [InlineData("HP", "F10")]
    [InlineData("Dell", "F2")]
    [InlineData("Lenovo", "F1 veya F2")]
    public void BilinenMarkaDogruGirisTusunuDoner(string marka, string beklenenTus)
    {
        Assert.Equal(beklenenTus, new RehberServisi().RehberGetir(marka).GirisTusu);
    }

    [Fact]
    public void TanimayanMarkaGenelRehbereDuser()
    {
        var rehber = new RehberServisi().RehberGetir("Tuhaf Marka");

        Assert.Equal("Genel", rehber.Marka);
        Assert.NotEmpty(rehber.Adimlar);
    }

    [Fact]
    public void HerBilinenMarkaninEksiksizRehberiVar()
    {
        var servis = new RehberServisi();
        string[] markalar =
        [
            "MSI", "ASUS", "Gigabyte", "ASRock", "HP", "Dell",
            "Lenovo", "Acer", "Casper", "Monster", "Toshiba", "Samsung"
        ];

        foreach (var marka in markalar)
        {
            var rehber = servis.RehberGetir(marka);

            Assert.Equal(marka, rehber.Marka);
            Assert.NotEmpty(rehber.GirisTusu);
            Assert.NotEmpty(rehber.Adimlar);
            Assert.All(rehber.Adimlar, a =>
            {
                Assert.NotEmpty(a.Baslik);
                Assert.NotEmpty(a.Aciklama);
            });
        }
    }

    [Fact]
    public void AdimlarSirayaGoreDonerVeBoslukYoktur()
    {
        var siralar = new RehberServisi().RehberGetir("MSI").Adimlar.Select(a => a.Sira).ToList();

        Assert.Equal(Enumerable.Range(1, siralar.Count), siralar);
    }

    [Fact]
    public void MarkaBuyukKucukHarfDuyarsizEslesir()
    {
        var servis = new RehberServisi();

        Assert.Equal("MSI", servis.RehberGetir("msi").Marka);
        Assert.Equal("ASUS", servis.RehberGetir("Asus").Marka);
    }

    [Fact]
    public void HtmlCiktisiRehberIceriginiTasir()
    {
        var servis = new RehberServisi();
        var rehber = servis.RehberGetir("MSI");

        var html = servis.HtmlUret(rehber, OrnekDonanim());

        Assert.Contains("UWin", html);
        Assert.Contains("ugilabs.com", html);
        Assert.Contains("DELETE", html);
        Assert.Contains("B550-A PRO", html);
        Assert.StartsWith("<!DOCTYPE html>", html);

        // Yalnizca isaretlemeyi bozan karakterler kacisli yazilir
        // (or. "Settings > Boot" -> "Settings &gt; Boot"). Aksanli
        // harfleri de sayisal varliga ceviren HtmlEncode kullanilmiyor:
        // sayfa UTF-8 bildirdigi icin gereksiz ve kaynagi okunmaz yapiyor.
        foreach (var adim in rehber.Adimlar)
            Assert.Contains(IsaretKacisi(adim.Baslik), html, StringComparison.Ordinal);
    }

    /// <summary>RehberServisi'ndeki kacis kuraliyla ayni.</summary>
    private static string IsaretKacisi(string metin) => metin
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);

    [Fact]
    public void HtmlCiktisiKullaniciMetniniKacisliYazar()
    {
        var servis = new RehberServisi();
        var rehber = servis.RehberGetir("MSI");
        var donanim = OrnekDonanim() with { AnakartModel = "<script>kotu()</script>" };

        var html = servis.HtmlUret(rehber, donanim);

        Assert.DoesNotContain("<script>kotu()", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void BootMenuTusuOlanMarkalardaGosterilir()
    {
        var servis = new RehberServisi();
        var rehber = servis.RehberGetir("Dell");

        var html = servis.HtmlUret(rehber, OrnekDonanim("Dell"));

        Assert.Equal("F12", rehber.BootMenuTusu);
        Assert.Contains("F12", html);
    }

    /// <summary>
    /// Rehber metinleri kullanicinin en cok okudugu icerik. Turkce harf
    /// olmadan yazilmis bir adim ("Bilgisayari kapat") programin geri
    /// kalanindaki duzgun Turkce'nin yaninda bozuk durur.
    /// </summary>
    [Fact]
    public void RehberAdimlariTurkceHarfKullanir()
    {
        var servis = new RehberServisi();
        string[] markalar =
        [
            "Genel", "MSI", "ASUS", "Gigabyte", "ASRock", "HP", "Dell",
            "Lenovo", "Acer", "Casper", "Monster", "Toshiba", "Samsung"
        ];

        var harfsiz = new List<string>();

        foreach (var marka in markalar)
        {
            foreach (var adim in servis.RehberGetir(marka).Adimlar)
            {
                // "tusuna", "gorunurken", "bellegi" gibi eksik yazimlar.
                foreach (var bozuk in new[]
                         { "tusu", "tusa", "gorun", "belleg", "Bilgisayari", "acilir", "basla", "sec" })
                {
                    if (adim.Aciklama.Contains(bozuk, StringComparison.Ordinal)
                        || adim.Baslik.Contains(bozuk, StringComparison.Ordinal))
                    {
                        harfsiz.Add($"{marka}: {adim.Baslik} / {adim.Aciklama}");
                    }
                }
            }
        }

        Assert.Empty(harfsiz);
    }

    [Fact]
    public void RehberMarkaAdlariVeTuslariOlduguGibiKalir()
    {
        var servis = new RehberServisi();

        // Tus adlari ve marka adlari cevrilmemeli: kullanici bunlari
        // ekranda Ingilizce gorecek.
        Assert.Equal("F10", servis.RehberGetir("HP").GirisTusu);
        Assert.Equal("DELETE", servis.RehberGetir("Gigabyte").GirisTusu);
        Assert.Equal("F12", servis.RehberGetir("Gigabyte").BootMenuTusu);

        // Marka adi adim metninde gectigi gibi durmali.
        var gigabyte = string.Concat(
            servis.RehberGetir("Gigabyte").Adimlar.Select(a => a.Aciklama));

        Assert.Contains("Gigabyte", gigabyte);
    }
}
