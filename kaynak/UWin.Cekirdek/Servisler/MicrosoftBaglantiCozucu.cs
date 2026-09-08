using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// Microsoft'un kendi indirme sayfasinin arkasindaki uc noktadan ISO
/// baglantisi cozer.
///
/// ONEMLI: Bu resmi bir API degildir. Microsoft akisi zaman zaman
/// degistirir ve bot korumasi icerir. Bu yuzden her adim basarisizliga
/// hazirlidir: cozumleme yapilamazsa null doner, cagiran taraf kullaniciyi
/// kendi ISO dosyasini secmeye yonlendirir. Program hicbir kosulda cokmez.
/// </summary>
public sealed partial class MicrosoftBaglantiCozucu
{
    /// <summary>Reddedilme gecici olabildigi icin birkac kez denenir.</summary>
    private const int AzamiDeneme = 3;

    private const string OrgId = "y6jn8c31";
    private const string ProfilKimligi = "606624d44113";
    private const string OrnekKimligi = "560dc9f3-1aa5-4a2f-b63c-9e18f8d0e175";

    private const string OturumUcNoktasi = "https://vlscppe.microsoft.com/tags";
    private const string KorumaUcNoktasi = "https://ov-df.microsoft.com";
    private const string BaglantiUcNoktasi = "https://www.microsoft.com/software-download-connector/api";

    private const string Yonlendiren = "https://www.microsoft.com/software-download/windows11";

    /// <summary>Uc nokta tarayici disi istekleri reddedebilir.</summary>
    private const string TarayiciKimligi =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
        + "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    private readonly HttpClient _http;

    public MicrosoftBaglantiCozucu(HttpClient http) => _http = http;

    [GeneratedRegex(@"[?&]w=([A-F0-9]+)")]
    internal static partial Regex WDegeriDeseni { get; }

    [GeneratedRegex(@"rticks=?[""']?\s*\+?\s*(\d{6,})")]
    internal static partial Regex RticksDeseni { get; }

    /// <summary>Cozumleme denemesinin sonucu.</summary>
    public enum CozumDurumu
    {
        /// <summary>Baglanti alindi.</summary>
        Basarili,

        /// <summary>Microsoft istegi geri cevirdi. Genelde gecicidir.</summary>
        Reddedildi,

        /// <summary>
        /// IP adresi cok fazla indirme istegi nedeniyle engellenmis
        /// (Microsoft hata kodu 715-123130). Bir sure beklemek gerekir.
        /// </summary>
        AdresEngellendi,

        /// <summary>Uc noktaya ulasilamadi veya yanit anlasilamadi.</summary>
        Erisilemedi
    }

    public sealed record CozumSonucu(CozumDurumu Durum, string? Baglanti);

    /// <summary>
    /// Verilen surum icin dogrudan indirme baglantisini cozer.
    /// Reddedilme gecici olabildigi icin birkac kez yeniden denenir.
    /// </summary>
    public async Task<CozumSonucu> CozAsync(
        int urunSurumKimligi,
        string dil,
        CancellationToken iptal = default)
    {
        CozumSonucu sonSonuc = new(CozumDurumu.Erisilemedi, null);

        for (var deneme = 0; deneme < AzamiDeneme; deneme++)
        {
            iptal.ThrowIfCancellationRequested();

            if (deneme > 0)
                await Task.Delay(TimeSpan.FromSeconds(2 * deneme), iptal);

            sonSonuc = await TekDenemeAsync(urunSurumKimligi, dil, iptal);

            // IP engeli yeniden denemekle asilmaz; bosuna beklemeye gerek yok.
            if (sonSonuc.Durum is CozumDurumu.Basarili or CozumDurumu.AdresEngellendi)
                return sonSonuc;
        }

        return sonSonuc;
    }

    private async Task<CozumSonucu> TekDenemeAsync(
        int urunSurumKimligi, string dil, CancellationToken iptal)
    {
        try
        {
            var oturum = Guid.NewGuid().ToString();

            if (!await OturumuBeyazListeyeAlAsync(oturum, iptal))
                return new CozumSonucu(CozumDurumu.Erisilemedi, null);

            var skuKimligi = await SkuKimligiAlAsync(oturum, urunSurumKimligi, dil, iptal);
            if (skuKimligi is null)
                return new CozumSonucu(CozumDurumu.Erisilemedi, null);

            return await IndirmeBaglantisiAlAsync(oturum, skuKimligi, dil, iptal);
        }
        catch (OperationCanceledException) when (iptal.IsCancellationRequested)
        {
            // Kullanici iptal etti; bu bir hata degil, cagirana bildirilir.
            throw;
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or OperationCanceledException)
        {
            // Uc nokta degismis, erisilemiyor veya zaman asimina ugramis olabilir.
            // HttpClient zaman asiminda da OperationCanceledException firlatir -
            // bu, kullanici iptalinden yukaridaki kosulla ayrilir.
            return new CozumSonucu(CozumDurumu.Erisilemedi, null);
        }
    }

    /// <summary>
    /// Microsoft'un indirme korumasi, oturum kimliginin once beyaz listeye
    /// alinmasini ve bir dogrulama adiminin tamamlanmasini ister.
    /// </summary>
    private async Task<bool> OturumuBeyazListeyeAlAsync(string oturum, CancellationToken iptal)
    {
        using var etiketIstegi = Istek($"{OturumUcNoktasi}?org_id={OrgId}&session_id={oturum}");
        using var etiketYaniti = await _http.SendAsync(etiketIstegi, iptal);

        if (!etiketYaniti.IsSuccessStatusCode)
            return false;

        // Koruma betiginden dinamik dogrulama parametreleri okunur.
        using var betikIstegi = Istek(
            $"{KorumaUcNoktasi}/mdt.js?instanceId={OrnekKimligi}&PageId=si&session_id={oturum}");
        using var betikYaniti = await _http.SendAsync(betikIstegi, iptal);
        var betik = await betikYaniti.Content.ReadAsStringAsync(iptal);

        var w = WDegeriDeseni.Match(betik);
        var rticks = RticksDeseni.Match(betik);

        if (!w.Success || !rticks.Success)
            return false;

        var simdi = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        using var dogrulamaIstegi = Istek(
            $"{KorumaUcNoktasi}/?session_id={oturum}&CustomerId={OrnekKimligi}&PageId=si"
            + $"&w={Uri.EscapeDataString(w.Groups[1].Value)}&mdt={simdi}&rticks={rticks.Groups[1].Value}");
        using var dogrulama = await _http.SendAsync(dogrulamaIstegi, iptal);

        return dogrulama.IsSuccessStatusCode;
    }

    /// <summary>Surum ve dil kombinasyonuna karsilik gelen SKU kimligini bulur.</summary>
    private async Task<string?> SkuKimligiAlAsync(
        string oturum, int urunSurumKimligi, string dil, CancellationToken iptal)
    {
        var sorguDili = DilAdiCozumle(dil);

        using var istek = Istek(
            $"{BaglantiUcNoktasi}/getskuinformationbyproductedition"
            + $"?profile={ProfilKimligi}&productEditionId={urunSurumKimligi}"
            + $"&SKU=undefined&friendlyFileName=undefined&Locale={YerelAyar(dil)}&sessionID={oturum}");

        using var yanit = await _http.SendAsync(istek, iptal);
        if (!yanit.IsSuccessStatusCode)
            return null;

        using var belge = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync(iptal));

        if (!belge.RootElement.TryGetProperty("Skus", out var skular))
            return null;

        foreach (var sku in skular.EnumerateArray())
        {
            var skuDili = sku.TryGetProperty("Language", out var d) ? d.GetString() : null;

            if (skuDili is not null && skuDili.Contains(sorguDili, StringComparison.OrdinalIgnoreCase))
                return sku.TryGetProperty("Id", out var kimlik) ? kimlik.ToString() : null;
        }

        return null;
    }

    /// <summary>SKU icin 64 bit ISO baglantisini doner.</summary>
    private async Task<CozumSonucu> IndirmeBaglantisiAlAsync(
        string oturum, string skuKimligi, string dil, CancellationToken iptal)
    {
        using var istek = Istek(
            $"{BaglantiUcNoktasi}/GetProductDownloadLinksBySku"
            + $"?profile={ProfilKimligi}&productEditionId=undefined&SKU={skuKimligi}"
            + $"&friendlyFileName=undefined&Locale={YerelAyar(dil)}&sessionID={oturum}");

        using var yanit = await _http.SendAsync(istek, iptal);
        if (!yanit.IsSuccessStatusCode)
            return new CozumSonucu(CozumDurumu.Erisilemedi, null);

        using var belge = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync(iptal));

        // Bot korumasi istegi geri cevirdiyse 200 doner ama govdede hata olur.
        // Bu, uc noktanin kirildigi anlamina gelmez - genelde gecici bir engeldir.
        if (belge.RootElement.TryGetProperty("Errors", out var hatalar)
            && hatalar.ValueKind == JsonValueKind.Array
            && hatalar.GetArrayLength() > 0)
        {
            // Tip 9, Microsoft'un 715-123130 kodudur: bu IP adresi cok fazla
            // indirme istegi yaptigi icin engellenmis. Yeniden denemek fayda
            // etmez, kullaniciya beklemesi soylenmelidir.
            var ilkHata = hatalar[0];
            var tip = ilkHata.TryGetProperty("Type", out var t) && t.TryGetInt32(out var tipNo) ? tipNo : 0;

            return new CozumSonucu(
                tip == 9 ? CozumDurumu.AdresEngellendi : CozumDurumu.Reddedildi, null);
        }

        if (!belge.RootElement.TryGetProperty("ProductDownloadOptions", out var secenekler))
            return new CozumSonucu(CozumDurumu.Erisilemedi, null);

        // 64 bit tercih edilir; bulunamazsa ilk secenek dondurulur.
        string? ilkBaglanti = null;

        foreach (var secenek in secenekler.EnumerateArray())
        {
            if (!secenek.TryGetProperty("Uri", out var uri) || uri.GetString() is not { } baglanti)
                continue;

            ilkBaglanti ??= baglanti;

            if (baglanti.Contains("x64", StringComparison.OrdinalIgnoreCase))
                return new CozumSonucu(CozumDurumu.Basarili, baglanti);
        }

        return ilkBaglanti is null
            ? new CozumSonucu(CozumDurumu.Erisilemedi, null)
            : new CozumSonucu(CozumDurumu.Basarili, ilkBaglanti);
    }

    /// <summary>Uc noktanin bekledigi basliklarla bir GET istegi hazirlar.</summary>
    private static HttpRequestMessage Istek(string adres)
    {
        var istek = new HttpRequestMessage(HttpMethod.Get, adres);

        istek.Headers.TryAddWithoutValidation("User-Agent", TarayiciKimligi);
        istek.Headers.Referrer = new Uri(Yonlendiren);

        return istek;
    }

    /// <summary>
    /// Uc nokta Locale parametresinde bolgesel bir kod bekler. Desteklenmeyen
    /// bir deger gonderilirse istek reddedilebilir, bu yuzden bilinen degerlere
    /// sinirlanir.
    /// </summary>
    private static string YerelAyar(string dil) => dil is "tr-TR" or "en-US" ? dil : "en-US";

    /// <summary>Microsoft SKU listesi dilleri kendi adlariyla bildirir.</summary>
    private static string DilAdiCozumle(string dil) => dil switch
    {
        "tr-TR" => "Turkish",
        "en-US" => "English",
        _ => new CultureInfo(dil).EnglishName
    };
}
