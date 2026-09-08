using System.Reflection;
using System.Text.RegularExpressions;
using UWin.Cekirdek.Arayuzler;
using UWin.Uygulama.Servisler;

namespace UWin.Uygulama.Testler;

/// <summary>
/// Servislerin paylasilan metin saglayicisina bagli oldugunu dogrular.
///
/// Bu, gercek bir hatanin testi: metin saglayici servislere istege
/// bagli bir parametreydi ve ServisSaglayici hicbirine vermiyordu.
/// Her servis sessizce kendi saglayicisini kuruyor, o da varsayilan
/// Turkce'de kaliyordu. Sonuc: arayuz Ingilizce'ye geciyor ama hata
/// mesajlari, uyumluluk karnesi ve BIOS rehberi Turkce kaliyordu.
///
/// Ikinci bir tuzak da statik alan sirasiydi: Metinler asagida
/// bildirildigi icin yukaridaki servisler onu null goruyordu.
/// </summary>
public class ServisSaglayiciTestleri
{
    private static readonly Regex TurkceHarf = new("[ğüşıöçĞÜŞİÖÇ]", RegexOptions.Compiled);

    /// <summary>Aktif dili degistirip sonra geri alir.</summary>
    private static T DildeCalistir<T>(string dil, Func<T> is_)
    {
        var oncekiDil = ServisSaglayici.Metinler.AktifDil;

        try
        {
            ServisSaglayici.Metinler.DilDegistir(dil);
            return is_();
        }
        finally
        {
            ServisSaglayici.Metinler.DilDegistir(oncekiDil);
        }
    }

    [Fact]
    public void MetinSaglayiciDigerServislerdenOnceKurulur()
    {
        // Statik alan sirasi yanlissa Metinler null gelir ve servisler
        // kendi saglayicilarini kurar.
        Assert.NotNull(ServisSaglayici.Metinler);
    }

    [Fact]
    public void UyumlulukServisiPaylasilanDiliIzler()
    {
        var donanim = new UWin.Cekirdek.Modeller.DonanimRaporu(
            "Gigabyte", "A520M", "AMD Ryzen 5 5600", 16L * 1024 * 1024 * 1024,
            "2.0", true, true, 512L * 1000 * 1000 * 1000);

        var ingilizce = DildeCalistir("en",
            () => ServisSaglayici.UyumlulukServisi.Degerlendir(donanim).Maddeler[0].Aciklama);

        Assert.False(TurkceHarf.IsMatch(ingilizce),
            $"Uyumluluk açıklaması Türkçe kaldı: {ingilizce}");
    }

    [Fact]
    public void AmacServisiPaylasilanDiliIzler()
    {
        var ingilizce = DildeCalistir("en",
            () => ServisSaglayici.AmacServisi
                .TavsiyeGetir(UWin.Cekirdek.Modeller.KurulumAmaci.Virus)
                .DiskEkraniTavsiyesi);

        Assert.False(TurkceHarf.IsMatch(ingilizce), $"Amaç tavsiyesi Türkçe kaldı: {ingilizce}");
    }

    [Fact]
    public void KurtarmaServisiPaylasilanDiliIzler()
    {
        var ingilizce = DildeCalistir("en",
            () => ServisSaglayici.KurtarmaServisi.AraclariGetir()[0].NeZamanKullanilir);

        Assert.False(TurkceHarf.IsMatch(ingilizce), $"Kurtarma aracı Türkçe kaldı: {ingilizce}");
    }

    /// <summary>
    /// BIOS rehberi ayri bir JSON dosyasindan gelir; dil basina bir
    /// dosya vardir ve dogru olani secilmelidir.
    /// </summary>
    [Fact]
    public void RehberServisiPaylasilanDiliIzler()
    {
        var ingilizce = DildeCalistir("en",
            () => ServisSaglayici.RehberServisi.RehberGetir("Gigabyte").Adimlar[0].Aciklama);

        Assert.False(TurkceHarf.IsMatch(ingilizce), $"BIOS rehberi Türkçe kaldı: {ingilizce}");
    }

    [Fact]
    public void RehberSecureBootNotuDaCevrilir()
    {
        var not = DildeCalistir("en",
            () => ServisSaglayici.RehberServisi.RehberGetir("MSI").SecureBootNotu);

        Assert.NotNull(not);
        Assert.False(TurkceHarf.IsMatch(not), $"Secure Boot notu Türkçe kaldı: {not}");
    }

    [Fact]
    public void VeriServisiKlasorAdlariniCevirir()
    {
        var adlar = DildeCalistir("en",
            () => UWin.Cekirdek.Servisler.VeriServisi
                .VarsayilanKlasorler(ServisSaglayici.Metinler)
                .Select(k => k.Ad)
                .ToList());

        Assert.All(adlar, ad => Assert.False(
            TurkceHarf.IsMatch(ad), $"Klasör adı Türkçe kaldı: {ad}"));
    }

    /// <summary>
    /// Turkce'de her sey Turkce kalmali - Ingilizce'yi duzeltirken
    /// Turkce'yi bozmadigimizin karsi kontrolu.
    /// </summary>
    [Fact]
    public void TurkcedeRehberTurkceKalir()
    {
        var turkce = DildeCalistir("tr",
            () => ServisSaglayici.RehberServisi.RehberGetir("Gigabyte").Adimlar[0].Aciklama);

        Assert.True(TurkceHarf.IsMatch(turkce), "Türkçe rehber Türkçe harf içermeli");
    }

    /// <summary>
    /// Metin kullanan her servis paylasilan saglayiciyi almali. Yeni bir
    /// servis eklenip baglanmayi unutuldugunda bu test degil, yukaridaki
    /// dil testleri patlar - ama bu test hangi servisin unutuldugunu
    /// dogrudan soyler.
    /// </summary>
    [Fact]
    public void MetinKullananServislerAyniSaglayiciyiPaylasir()
    {
        var saglayici = ServisSaglayici.Metinler;

        var eksikler = new List<string>();

        foreach (var (ad, servis) in MetinKullananServisler())
        {
            var alan = servis.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.FieldType == typeof(IMetinSaglayici));

            if (alan is null)
                continue;

            if (!ReferenceEquals(alan.GetValue(servis), saglayici))
                eksikler.Add(ad);
        }

        Assert.True(eksikler.Count == 0,
            "Paylaşılan metin sağlayıcıyı almayan servisler: " + string.Join(", ", eksikler));
    }

    private static IEnumerable<(string Ad, object Servis)> MetinKullananServisler()
    {
        yield return (nameof(ServisSaglayici.YazmaServisi), ServisSaglayici.YazmaServisi);
        yield return (nameof(ServisSaglayici.GeriKazanmaServisi), ServisSaglayici.GeriKazanmaServisi);
        yield return (nameof(ServisSaglayici.KurtarmaServisi), ServisSaglayici.KurtarmaServisi);
        yield return (nameof(ServisSaglayici.SaglikServisi), ServisSaglayici.SaglikServisi);
        yield return (nameof(ServisSaglayici.UyumlulukServisi), ServisSaglayici.UyumlulukServisi);
        yield return (nameof(ServisSaglayici.AmacServisi), ServisSaglayici.AmacServisi);
        yield return (nameof(ServisSaglayici.VeriServisi), ServisSaglayici.VeriServisi);
        yield return (nameof(ServisSaglayici.IndirmeServisi), ServisSaglayici.IndirmeServisi);
        yield return (nameof(ServisSaglayici.RehberServisi), ServisSaglayici.RehberServisi);
    }
}
