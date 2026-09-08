using System.Reflection;
using System.Text.Json;
using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// Surum katalogunu gomulu JSON'dan okur; indirme baglantisini canli uc noktadan cozer.
/// Uc nokta erisilemezse katalog yine calisir - program cokmez, kullanici kendi ISO'sunu secer.
/// </summary>
public sealed class SurumServisi : ISurumServisi
{
    private readonly HttpClient? _http;
    private readonly MicrosoftBaglantiCozucu? _cozucu;
    private readonly Lazy<Katalog> _katalog;

    public SurumServisi(HttpClient? httpIstemci)
    {
        _http = httpIstemci;
        _cozucu = httpIstemci is null ? null : new MicrosoftBaglantiCozucu(httpIstemci);
        _katalog = new Lazy<Katalog>(KatalogYukle);
    }

    public Task<IReadOnlyList<WindowsSurumu>> SurumleriGetirAsync(CancellationToken iptal = default)
    {
        var liste = new List<WindowsSurumu>();

        foreach (var giris in _katalog.Value.Surumler)
            foreach (var dil in giris.Diller)
                foreach (var mimari in giris.Mimariler)
                    liste.Add(new WindowsSurumu(
                        Kimlik: giris.Kimlik,
                        GorunenAd: giris.GorunenAd,
                        Surum: giris.Surum,
                        Dil: dil,
                        Mimari: mimari,
                        IndirmeBaglantisi: null,
                        Sha256: null,
                        TahminiBoyutBayt: giris.TahminiBoyutBayt));

        return Task.FromResult<IReadOnlyList<WindowsSurumu>>(liste);
    }

    public async Task<SurumCozumSonucu> BaglantiCozAsync(
        string kimlik, string dil, string mimari, CancellationToken iptal = default)
    {
        var giris = _katalog.Value.Surumler.FirstOrDefault(s => s.Kimlik == kimlik);

        if (giris is null || _cozucu is null || giris.UrunSurumKimligi <= 0)
            return new SurumCozumSonucu(BaglantiDurumu.Erisilemedi, null);

        try
        {
            var cozum = await _cozucu.CozAsync(giris.UrunSurumKimligi, dil, iptal);

            var durum = cozum.Durum switch
            {
                MicrosoftBaglantiCozucu.CozumDurumu.Basarili => BaglantiDurumu.Basarili,
                MicrosoftBaglantiCozucu.CozumDurumu.Reddedildi => BaglantiDurumu.Reddedildi,
                MicrosoftBaglantiCozucu.CozumDurumu.AdresEngellendi => BaglantiDurumu.AdresEngellendi,
                _ => BaglantiDurumu.Erisilemedi
            };

            if (durum != BaglantiDurumu.Basarili || cozum.Baglanti is not { } baglanti)
                return new SurumCozumSonucu(durum, null);

            var surum = new WindowsSurumu(
                kimlik, giris.GorunenAd, giris.Surum, dil, mimari,
                baglanti, OzetBul(kimlik, dil, mimari), giris.TahminiBoyutBayt);

            return new SurumCozumSonucu(BaglantiDurumu.Basarili, surum);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return new SurumCozumSonucu(BaglantiDurumu.Erisilemedi, null);
        }
    }

    public string? OzetIleTani(string sha256)
        => _katalog.Value.BilinenOzetler.TryGetValue(sha256.ToLowerInvariant(), out var ad) ? ad : null;

    private string? OzetBul(string kimlik, string dil, string mimari)
    {
        var anahtar = $"{kimlik}|{dil}|{mimari}";

        return _katalog.Value.BilinenOzetler
            .FirstOrDefault(c => c.Value.Equals(anahtar, StringComparison.OrdinalIgnoreCase)).Key;
    }

    private static Katalog KatalogYukle()
    {
        var derleme = Assembly.GetExecutingAssembly();
        var ad = derleme.GetManifestResourceNames()
            .First(n => n.EndsWith("windows-surumleri.json", StringComparison.Ordinal));

        using var akis = derleme.GetManifestResourceStream(ad)!;

        return JsonSerializer.Deserialize<Katalog>(akis, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    private sealed record Katalog(List<KatalogGirisi> Surumler, Dictionary<string, string> BilinenOzetler);

    private sealed record KatalogGirisi(
        string Kimlik,
        string GorunenAd,
        string Surum,
        List<string> Diller,
        List<string> Mimariler,
        long TahminiBoyutBayt,
        int UrunSurumKimligi);
}
