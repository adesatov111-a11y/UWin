using System.Reflection;
using System.Text.Json;
using UWin.Cekirdek.Arayuzler;

namespace UWin.Cekirdek.Servisler;

/// <summary>Gomulu JSON dosyalarindan arayuz metinlerini saglar.</summary>
public sealed class MetinSaglayici : IMetinSaglayici
{
    private static readonly string[] DesteklenenDiller = ["tr", "en"];
    private static readonly Dictionary<string, Dictionary<string, string>> Onbellek = [];
    private static readonly Lock Kilit = new();

    private Dictionary<string, string> _metinler;

    public string AktifDil { get; private set; } = "tr";

    /// <summary>
    /// Dil degistiginde tetiklenir. Arayuz bunu dinleyip kendini
    /// yeniler; olay olmadan ekrandaki metinler eski dilde kalirdi.
    /// </summary>
    public event EventHandler? DilDegisti;

    public MetinSaglayici() => _metinler = Yukle("tr");

    public string Al(string anahtar)
        => _metinler.TryGetValue(anahtar, out var metin) ? metin : anahtar;

    public string Al(string anahtar, params object[] degerler)
    {
        var kalip = Al(anahtar);

        try
        {
            return string.Format(kalip, degerler);
        }
        catch (FormatException)
        {
            // Kalip ile deger sayisi uyusmuyorsa ham kalibi dondurmek,
            // kullaniciya bir istisna gostermekten iyidir.
            return kalip;
        }
    }

    public void DilDegistir(string dil)
    {
        var secilen = DesteklenenDiller.Contains(dil) ? dil : "tr";

        // Ayni dile gecmek bir degisiklik degildir; olay tetiklenirse
        // arayuz bosuna kendini yeniden kurar.
        if (secilen == AktifDil)
            return;

        AktifDil = secilen;
        _metinler = Yukle(secilen);

        DilDegisti?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Arayuzun secim sunabilmesi icin desteklenen diller.</summary>
    public static IReadOnlyList<string> DilleriListele() => DesteklenenDiller;

    /// <summary>Test amacli: bir dilin tum anahtarlarini listeler.</summary>
    public static IReadOnlyCollection<string> AnahtarlariListele(string dil) => Yukle(dil).Keys;

    private static Dictionary<string, string> Yukle(string dil)
    {
        lock (Kilit)
        {
            if (Onbellek.TryGetValue(dil, out var onbellekli))
                return onbellekli;

            var derleme = Assembly.GetExecutingAssembly();
            var ad = derleme.GetManifestResourceNames()
                .First(n => n.EndsWith($"Metinler.{dil}.json", StringComparison.Ordinal));

            using var akis = derleme.GetManifestResourceStream(ad)!;
            var metinler = JsonSerializer.Deserialize<Dictionary<string, string>>(akis)!;

            Onbellek[dil] = metinler;
            return metinler;
        }
    }
}
