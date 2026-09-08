using System.Reflection;
using System.Text.Json;
using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Programin tamami duzgun Turkce yazilmali. Turkce harfleri atlanmis bir
/// metin ("Bilgisayari kapat") tek basina anlasilir olsa da, programin geri
/// kalanindaki duzgun yazimin yaninda ozensiz durur.
///
/// Bu testler yeni eklenen metinlerin de kurala uymasini saglar.
/// </summary>
public class TurkceHarfTestleri
{
    /// <summary>
    /// Bu yazimlar duzgun Turkce'de asla gecmez: her biri en az bir Turkce
    /// harfi eksik birakilmis bir kelimedir. Ornegin "tusu" yazan bir metinde
    /// mutlaka "tuşu" olmaliydi.
    ///
    /// Listeye kelime eklerken dikkat: "dosyalar" gibi Turkce harf icermeyen
    /// dogru yazimlar buraya girmemeli, yoksa test dogru metni reddeder.
    /// </summary>
    private static readonly string[] EksikYazimlar =
    [
        "tusu", "tusa", "tuslar",
        "gorun", "goster",
        "belleg",
        "acilir", "acilan", "acmak",
        "secenek", "secim", "secil",
        "baslat", "baslar",
        "degis", "degil",
        "kucuk", "buyuk",
        "calis", "cikar",
        "dogru", "yanlis",
        "surum", "surucu",
        "islem",
        "hazirla",
        "kullanici",
        "guvenli",
        "baglanti",
        "dosyasi",
        "Bilgisayari", "bilgisayari",
        "yukle", "onemli",
        "sifre"
    ];

    /// <summary>Ingilizce metinler bu kurala tabi degildir.</summary>
    private static Dictionary<string, string> TurkceMetinler()
    {
        var derleme = typeof(MetinSaglayici).Assembly;

        var ad = derleme.GetManifestResourceNames()
            .Single(n => n.EndsWith("Metinler.tr.json", StringComparison.Ordinal));

        using var akis = derleme.GetManifestResourceStream(ad)!;

        return JsonSerializer.Deserialize<Dictionary<string, string>>(akis)!;
    }

    /// <summary>
    /// Bilerek ASCII yazilan anahtarlar. Kurtarma araclarinin .bat
    /// dosyalari kurtarma ortamindaki Komut Istemi'nde calisir; orada
    /// Turkce kod sayfasi yuklu gelmez ve Turkce harfler anlamsiz
    /// isaretlere donusur. Bu metinlerin ASCII olmasi bir eksiklik
    /// degil, zorunluluktur.
    /// </summary>
    private static bool AsciiOlmali(string anahtar)
        => anahtar.StartsWith("kurtarma.bat.", StringComparison.Ordinal);

    [Fact]
    public void TurkceMetinlerdeEksikYazimYok()
    {
        var hatalar = new List<string>();

        foreach (var (anahtar, deger) in TurkceMetinler())
        {
            if (AsciiOlmali(anahtar))
                continue;

            foreach (var eksik in EksikYazimlar)
            {
                if (deger.Contains(eksik, StringComparison.Ordinal))
                    hatalar.Add($"{anahtar}: \"{deger}\" -> '{eksik}'");
            }
        }

        Assert.Empty(hatalar);
    }

    [Fact]
    public void HerTurkceMetinDoluDur()
    {
        Assert.All(TurkceMetinler(), c => Assert.False(string.IsNullOrWhiteSpace(c.Value)));
    }

    /// <summary>
    /// Yazma sirasinda gosterilen hata metinleri kaynak dosyada degil, kodun
    /// icinde duruyor; onlarin da kurala uymasi gerekir. Sistem diski hatasi
    /// bunlarin en kritigi: kullanici en cok bu uyariya guvenir.
    /// </summary>
    [Fact]
    public async Task YazmaHatalariTurkceHarfIcerir()
    {
        var sahteDisk = new Sahteler.SahteDiskErisimi();

        sahteDisk.DiskEkle(new Modeller.HamDiskGirisi(
            DiskNumarasi: 0, Model: "Samsung 980", SeriNumarasi: "SN-0",
            BoyutBayt: 512_000_000_000, CikarilabilirMi: false, VeriYolu: "NVMe",
            SistemDiskiMi: true, KullanilanBayt: 200_000_000_000));

        var diskServisi = new DiskServisi(sahteDisk);

        var servis = new YazmaServisi(
            sahteDisk, diskServisi,
            Sahteler.SahteIsoOkuyucu.KucukWimli(),
            new DurumIsaretleyici());

        var sistem = diskServisi.DiskleriListele(gelismisMod: true)
            .Single(d => d.Sinif == Modeller.DiskSinifi.SistemDiski);

        var sonuc = await servis.YazAsync(sistem, "test.iso", Modeller.BolumTablosuTipi.Gpt);

        var hata = Assert.IsType<Modeller.UWinHatasi>(sonuc.Hata);
        var tumMetin = hata.NeOldu + hata.Neden + hata.NeYapmali;

        foreach (var eksik in EksikYazimlar)
            Assert.DoesNotContain(eksik, tumMetin, StringComparison.Ordinal);
    }
}
