using System.Reflection;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace UWin.Uygulama.Servisler;

/// <summary>
/// Uygulama simgelerini gomulu kaynaklardan okur.
///
/// Tek dosya yayininda exe'nin yaninda klasor olmadigi icin simgeler
/// derlemenin icine gomulur. Simge yuklenemezse program calismaya
/// devam eder - eksik bir ikon, acilmayan bir pencereden iyidir.
/// </summary>
internal static class Simgeler
{
    private const string BaslikKaynagi = "uwin-64.png";
    private const string IkonKaynagi = "uwin.ico";

    private static readonly Lazy<string?> IkonYolu = new(IkonuCikar);

    /// <summary>Baslik cubugundaki 18 piksellik marka simgesi.</summary>
    internal static BitmapImage? BaslikSimgesi()
    {
        var veri = KaynagiOku(BaslikKaynagi);
        if (veri is null)
            return null;

        try
        {
            var akis = new InMemoryRandomAccessStream();

            using (var yazici = new DataWriter(akis.GetOutputStreamAt(0)))
            {
                yazici.WriteBytes(veri);
                yazici.StoreAsync().AsTask().GetAwaiter().GetResult();
            }

            var resim = new BitmapImage();
            resim.SetSource(akis);
            return resim;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Gorev cubugu ikonu. AppWindow.SetIcon gercek bir dosya yolu ister,
    /// bu yuzden gomulu ICO gecici klasore bir kez yazilir.
    /// </summary>
    internal static string? PencereIkonuYolu() => IkonYolu.Value;

    private static string? IkonuCikar()
    {
        var veri = KaynagiOku(IkonKaynagi);
        if (veri is null)
            return null;

        try
        {
            var yol = Path.Combine(Path.GetTempPath(), "uwin-pencere.ico");

            // Ayni surumu her acilista yeniden yazmaya gerek yok.
            if (!File.Exists(yol) || new FileInfo(yol).Length != veri.Length)
                File.WriteAllBytes(yol, veri);

            return yol;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static byte[]? KaynagiOku(string dosyaAdi)
    {
        var derleme = Assembly.GetExecutingAssembly();

        var ad = derleme.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(dosyaAdi, StringComparison.Ordinal));

        if (ad is null)
            return null;

        using var akis = derleme.GetManifestResourceStream(ad);
        if (akis is null)
            return null;

        using var bellek = new MemoryStream();
        akis.CopyTo(bellek);
        return bellek.ToArray();
    }
}
