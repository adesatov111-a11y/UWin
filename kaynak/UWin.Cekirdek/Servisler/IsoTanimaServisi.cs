using System.Text.RegularExpressions;
using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// ISO'yu acip icindeki dosyalara bakarak surumu belirler.
///
/// Neden ozet (SHA-256) degil: Microsoft ISO'lari her indirme isteginde
/// yeniden paketliyor, bu yuzden iki kisinin ayni surumu indirmesi ayni
/// ozeti vermiyor. Sabit bir ozet listesi tutmak, gecerli dosyalarin
/// "taninmadi" diye isaretlenmesinden baska bir sey uretmez. Ustelik
/// 7 GB'lik dosyanin tamamini okumak dakikalar suruyordu; icerik listesi
/// saniyeler icinde okunur.
/// </summary>
public sealed partial class IsoTanimaServisi(IIsoOkuyucu okuyucu) : IIsoTanimaServisi
{
    /// <summary>Kurulum dosyalarinin yaninda USB'de duracak ek yer payi.</summary>
    private const double GuvenlikPayi = 1.08;

    /// <summary>FAT32 siniri asilirsa WIM bolunur; bolme sirasinda gecici yer gerekir.</summary>
    private const double BolmePayi = 1.15;

    public IsoKimligi Tani(string isoYolu)
    {
        IsoIcerigi icerik;

        try
        {
            icerik = okuyucu.Oku(isoYolu);
        }
        catch (Exception e) when (e is IOException
                                       or UnauthorizedAccessException
                                       or InvalidDataException
                                       or NotSupportedException)
        {
            // Bozuk ya da ISO olmayan dosya beklenen bir durumdur.
            return new IsoKimligi(IsoTanimaDurumu.Okunamadi, null, null, null, 0);
        }

        if (!WindowsKurulumuMu(icerik))
            return new IsoKimligi(IsoTanimaDurumu.WindowsDegil, null, null, null, 0);

        var boyut = GerekenBoyut(icerik);
        var (ad, yapi) = SurumuCoz(isoYolu, icerik);
        var mimari = MimariCoz(icerik);

        var durum = ad is null
            ? IsoTanimaDurumu.WindowsAmaSurumBilinmiyor
            : IsoTanimaDurumu.Tanindi;

        return new IsoKimligi(durum, ad, yapi, mimari, boyut);
    }

    /// <summary>
    /// Windows kurulum diskinin degismeyen isareti: sources klasoru
    /// icinde install imaji ve yaninda bir onyukleyici.
    /// </summary>
    private static bool WindowsKurulumuMu(IsoIcerigi icerik)
    {
        var kurulumImaji = icerik.Dosyalar.Any(
            d => d.GoreliYol.StartsWith("sources/", StringComparison.OrdinalIgnoreCase)
                 && (d.GoreliYol.EndsWith("install.wim", StringComparison.OrdinalIgnoreCase)
                     || d.GoreliYol.EndsWith("install.esd", StringComparison.OrdinalIgnoreCase)));

        var baslatici = Var(icerik, "setup.exe")
                        || Var(icerik, "bootmgr")
                        || icerik.Dosyalar.Any(d => d.GoreliYol.StartsWith(
                            "efi/", StringComparison.OrdinalIgnoreCase));

        return kurulumImaji && baslatici;
    }

    /// <summary>
    /// USB'ye sigmasi icin gereken en kucuk boyut. Sabit 8 GB varsaymak
    /// yerine gercek dosya boyutlarindan hesaplanir: kullaniciya "en az
    /// su kadar" derken dogruyu soylemek gerekir.
    /// </summary>
    private static long GerekenBoyut(IsoIcerigi icerik)
    {
        var toplam = icerik.Dosyalar.Sum(d => d.BoyutBayt);
        var pay = icerik.WimBolunmeliMi ? BolmePayi : GuvenlikPayi;

        return (long)(toplam * pay);
    }

    private static (string? Ad, int? Yapi) SurumuCoz(string isoYolu, IsoIcerigi icerik)
    {
        // Once dosya adi: kullanicinin indirdigi dosya genelde surumu tasir
        // (ornegin Win11_25H2_Turkish_x64.iso). ISO icindeki surum dosyasini
        // acmak ek okuma demek; ad zaten yeterliyse ona bakariz.
        var addanGelen = AddanSurumCoz(Path.GetFileName(isoYolu));
        if (addanGelen is not null)
            return (addanGelen, null);

        // Ad bir sey soylemiyorsa icerikten kaba bir tahmin yapilir.
        return (IceriktenSurumCoz(icerik), null);
    }

    private static string? AddanSurumCoz(string dosyaAdi)
    {
        var eslesme = SurumDeseni().Match(dosyaAdi);

        if (!eslesme.Success)
            return null;

        // "8.1" ve "81" ayni surumu anlatir; noktayi atip tek bicime indiriyoruz.
        var numara = eslesme.Groups["numara"].Value.Replace(".", string.Empty);

        return numara switch
        {
            "11" => "Windows 11",
            "10" => "Windows 10",
            "8" => "Windows 8.1",
            "81" => "Windows 8.1",
            "7" => "Windows 7",
            _ => null
        };
    }

    /// <summary>
    /// Icerikten surum tahmini. Kesin degildir, bu yuzden yalnizca dosya
    /// adi bir sey soylemediginde kullanilir.
    /// </summary>
    private static string? IceriktenSurumCoz(IsoIcerigi icerik)
    {
        // Windows 7 ve oncesinde UEFI onyukleyicisi yoktur.
        var uefiVar = icerik.Dosyalar.Any(
            d => d.GoreliYol.StartsWith("efi/", StringComparison.OrdinalIgnoreCase));

        return uefiVar ? null : "Windows 7";
    }

    private static string? MimariCoz(IsoIcerigi icerik)
    {
        if (Var(icerik, "efi/boot/bootx64.efi"))
            return "x64";

        if (Var(icerik, "efi/boot/bootaa64.efi"))
            return "arm64";

        if (Var(icerik, "efi/boot/bootia32.efi"))
            return "x86";

        return null;
    }

    private static bool Var(IsoIcerigi icerik, string yol)
        => icerik.Dosyalar.Any(d => d.GoreliYol.Equals(yol, StringComparison.OrdinalIgnoreCase));

    /// <summary>"Win11", "Windows_10", "win 8.1" gibi yazimlari yakalar.</summary>
    [GeneratedRegex(@"win(?:dows)?[\s_\-]*(?<numara>11|10|8\.?1|8|7)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SurumDeseni();
}
