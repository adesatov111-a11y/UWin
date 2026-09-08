using System.Text.Json;
using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// Kullanicinin kalici tercihlerini saklar. Su an yalnizca dil.
///
/// Ayar dosyasi kullanicinin AppData klasorunde durur; program
/// tasinabilir olmak icin kurulum yapmiyor, bu yuzden ayarlar
/// exe'nin yanina degil kullaniciya ait bir yere yazilir.
///
/// Hicbir islem istisna firlatmaz: ayar yazilamiyorsa program yine
/// calisir, secim yalnizca o oturum icin gecerli olur. Bir tercih
/// dosyasi yuzunden programin acilmamasi kabul edilemez.
/// </summary>
public sealed class AyarServisi : IAyarServisi
{
    public const string DosyaAdi = "ayarlar.json";

    private static readonly string[] DesteklenenDiller = ["tr", "en"];

    private readonly string _klasor;

    /// <param name="klasor">
    /// Verilmezse kullanicinin AppData klasoru kullanilir. Testler
    /// gecici bir klasor verir; boylece gercek ayarlar bozulmaz.
    /// </param>
    public AyarServisi(string? klasor = null)
        => _klasor = klasor ?? VarsayilanKlasor();

    private string DosyaYolu => Path.Combine(_klasor, DosyaAdi);

    public Ayarlar Oku()
    {
        try
        {
            if (!File.Exists(DosyaYolu))
                return new Ayarlar(null);

            var metin = File.ReadAllText(DosyaYolu);
            var okunan = JsonSerializer.Deserialize<Ayarlar>(metin);

            // Dosyada taninmayan bir dil yazsa bile kabul edilmez:
            // eski bir surumden kalmis olabilir.
            return okunan?.Dil is { } dil && DesteklenenDiller.Contains(dil)
                ? new Ayarlar(dil)
                : new Ayarlar(null);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            // Bozuk veya okunamayan ayar, "ayar yok" sayilir. Kullaniciyi
            // bir JSON hatasiyla karsilamak, dili tekrar sormaktan kotu.
            return new Ayarlar(null);
        }
    }

    public void DilKaydet(string dil)
    {
        if (!DesteklenenDiller.Contains(dil))
            return;

        try
        {
            Directory.CreateDirectory(_klasor);
            File.WriteAllText(DosyaYolu, JsonSerializer.Serialize(new Ayarlar(dil)));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                      or ArgumentException or NotSupportedException)
        {
            // Yazilamadi: secim yalnizca bu oturumda gecerli olur.
        }
    }

    /// <summary>
    /// Isletim sisteminin diline gore onerilecek dil. Kullaniciya bos
    /// bir secim ekrani gostermek yerine muhtemel cevabi onceden
    /// isaretlemek, ondan daha az is ister.
    /// </summary>
    public static string OnerilenDil(string sistemDili)
        => sistemDili.StartsWith("tr", StringComparison.OrdinalIgnoreCase) ? "tr" : "en";

    private static string VarsayilanKlasor()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UWin");
}
