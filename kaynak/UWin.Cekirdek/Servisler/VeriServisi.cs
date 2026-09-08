using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// Bu bilgisayarda kurulumla birlikte gidecek verileri sayar. Salt
/// okuma: hicbir dosyaya dokunmaz, yalnizca boyut ve adet toplar.
///
/// Amac kullaniciyi korkutmak degil, dogru anda bilgilendirmek.
/// "Dosyalarin silinecek" cumlesi soyut kalir; "Masaustunde 3.400
/// dosya, 12 GB" cumlesi kullaniciyi yedek almaya gonderir.
/// </summary>
public sealed class VeriServisi : IVeriServisi
{
    private readonly IMetinSaglayici _metinler;

    /// <param name="metinler">
    /// Klasor adlari buradan gelir; verilmezse varsayilan saglayici
    /// kullanilir.
    /// </param>
    public VeriServisi(IMetinSaglayici? metinler = null)
        => _metinler = metinler ?? new MetinSaglayici();

    /// <summary>
    /// Kurulumda gidecek standart kullanici klasorleri. Hepsi C:
    /// altindadir ve bolum silindiginde veya bicimlendirildiginde yok olur.
    /// </summary>
    public static IReadOnlyList<(string Ad, string Yol)> VarsayilanKlasorler(
        IMetinSaglayici? metinler = null)
    {
        var m = metinler ?? new MetinSaglayici();

        return
        [
            (m.Al("klasor.masaustu"), Klasor(Environment.SpecialFolder.DesktopDirectory)),
            (m.Al("klasor.belgeler"), Klasor(Environment.SpecialFolder.MyDocuments)),
            (m.Al("klasor.indirilenler"), IndirilenlerYolu()),
            (m.Al("klasor.resimler"), Klasor(Environment.SpecialFolder.MyPictures)),
            (m.Al("klasor.videolar"), Klasor(Environment.SpecialFolder.MyVideos)),
            (m.Al("klasor.muzik"), Klasor(Environment.SpecialFolder.MyMusic))
        ];
    }

    public async Task<VeriRaporu> RaporAlAsync(
        IReadOnlyList<(string Ad, string Yol)>? klasorler = null,
        CancellationToken iptal = default)
    {
        var hedefler = klasorler ?? VarsayilanKlasorler(_metinler);

        List<KullaniciKlasoru> sonuclar = [];

        foreach (var (ad, yol) in hedefler)
        {
            iptal.ThrowIfCancellationRequested();

            sonuclar.Add(await Task.Run(() => KlasoruOlc(ad, yol, iptal), iptal));
        }

        return new VeriRaporu(sonuclar);
    }

    /// <summary>
    /// Tek bir klasoru olcer. Okunamayan alt klasorler atlanir: bir
    /// izin hatasi yuzunden butun raporu kaybetmek, eksik bir rakam
    /// gostermekten kotudur.
    /// </summary>
    private static KullaniciKlasoru KlasoruOlc(string ad, string yol, CancellationToken iptal)
    {
        if (string.IsNullOrEmpty(yol) || !Directory.Exists(yol))
            return new KullaniciKlasoru(ad, yol, 0, 0);

        long toplam = 0;
        var adet = 0;

        var secenekler = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        try
        {
            foreach (var dosya in new DirectoryInfo(yol).EnumerateFiles("*", secenekler))
            {
                iptal.ThrowIfCancellationRequested();

                try
                {
                    toplam += dosya.Length;
                    adet++;
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    // Dosya arada silinmis olabilir; sayimi bozmadan gecilir.
                }
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Klasorun tamami okunamadi; o ana kadar sayilanlar dondurulur.
        }

        return new KullaniciKlasoru(ad, yol, toplam, adet);
    }

    private static string Klasor(Environment.SpecialFolder klasor)
        => Environment.GetFolderPath(klasor);

    /// <summary>
    /// Indirilenler klasorunun .NET'te dogrudan bir karsiligi yok;
    /// kullanici profilinin altindan kurulur.
    /// </summary>
    private static string IndirilenlerYolu()
    {
        var profil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return string.IsNullOrEmpty(profil) ? string.Empty : Path.Combine(profil, "Downloads");
    }
}
