using System.Reflection;
using System.Text.RegularExpressions;

namespace UWin.Uygulama.Testler;

/// <summary>
/// Koddaki her metin anahtarinin kaynakta karsiligi var mi?
///
/// MetinSaglayici, bulamadigi anahtarin kendisini dondurur - bu, hata
/// vermemek icin bilerek boyle. Ama sonuc olarak yazim hatasi yapilan
/// bir anahtar ekranda "kazanma.onay" diye ham haliyle gorunuyor ve
/// hicbir test bunu yakalamiyordu. Bu testler o boslugu kapatir.
/// </summary>
public class MetinKapsamiTestleri
{
    /// <summary>Kodda gecen ".Al(" cagrilarindaki sabit anahtarlar.</summary>
    private static readonly Regex AnahtarDeseni = new(
        """\.Al\(\s*"([^"]+)"\s*[,)]""", RegexOptions.Compiled);

    /// <summary>Kaynak agacinin kokunu derleme ciktisindan yukari cikarak bulur.</summary>
    private static DirectoryInfo KaynakKoku()
    {
        var klasor = new DirectoryInfo(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);

        while (klasor is not null)
        {
            var aday = Path.Combine(klasor.FullName, "kaynak");

            if (Directory.Exists(aday))
                return new DirectoryInfo(aday);

            klasor = klasor.Parent;
        }

        throw new DirectoryNotFoundException("kaynak klasoru bulunamadi.");
    }

    private static IReadOnlyList<(string Dosya, int Satir, string Anahtar)> KullanilanAnahtarlar()
    {
        List<(string, int, string)> bulunanlar = [];

        foreach (var dosya in KaynakKoku().EnumerateFiles("*.cs", SearchOption.AllDirectories))
        {
            // Derleme ciktisi ve uretilen kod taranmaz.
            if (dosya.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || dosya.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var satirlar = File.ReadAllLines(dosya.FullName);

            for (var i = 0; i < satirlar.Length; i++)
            {
                foreach (Match eslesme in AnahtarDeseni.Matches(satirlar[i]))
                    bulunanlar.Add((dosya.Name, i + 1, eslesme.Groups[1].Value));
            }
        }

        return bulunanlar;
    }

    [Fact]
    public void KoddakiHerAnahtarTurkceKaynaktaVar()
    {
        var saglayici = new UWin.Cekirdek.Servisler.MetinSaglayici();

        var eksikler = KullanilanAnahtarlar()
            // Al(anahtar) bulamayinca anahtarin kendisini dondurur.
            .Where(k => saglayici.Al(k.Anahtar) == k.Anahtar)
            .Select(k => $"{k.Dosya}:{k.Satir} -> {k.Anahtar}")
            .ToList();

        Assert.True(eksikler.Count == 0,
            "Kaynakta karsiligi olmayan anahtarlar:\n" + string.Join("\n", eksikler));
    }

    [Fact]
    public void KoddakiHerAnahtarIngilizceKaynaktaVar()
    {
        var saglayici = new UWin.Cekirdek.Servisler.MetinSaglayici();
        saglayici.DilDegistir("en");

        var eksikler = KullanilanAnahtarlar()
            .Where(k => saglayici.Al(k.Anahtar) == k.Anahtar)
            .Select(k => $"{k.Dosya}:{k.Satir} -> {k.Anahtar}")
            .ToList();

        Assert.True(eksikler.Count == 0,
            "Ingilizce kaynakta karsiligi olmayan anahtarlar:\n" + string.Join("\n", eksikler));
    }

    [Fact]
    public void TaramaGercektenAnahtarBuluyor()
    {
        // Tarama bozulursa diger iki test sessizce hep gecerdi.
        Assert.NotEmpty(KullanilanAnahtarlar());
    }
}
