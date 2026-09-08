using System.Reflection;
using System.Text;
using System.Text.Json;
using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// BIOS rehberini gomulu JSON'dan okur. Icerik veri olarak durdugu icin
/// yeni marka eklemek kod degisikligi gerektirmez.
///
/// Rehber dosyasi her dil icin ayridir (bios-rehberleri.tr.json,
/// bios-rehberleri.en.json). Tek dosyada tutup metinleri kaynaktan
/// cekmek de olurdu, ama rehberin degeri tam olarak akici anlatimda:
/// "Ekranda MSI yazisi gorunurken basiyor olman gerekiyor" gibi bir
/// cumle parcalanip anahtarlara bolununce okunakligini kaybediyor.
/// </summary>
public sealed class RehberServisi : IRehberServisi
{
    /// <summary>Dil basina yuklenmis rehber dosyalari.</summary>
    private static readonly Dictionary<string, RehberDosyasi> Onbellek = [];
    private static readonly Lock Kilit = new();

    private readonly IMetinSaglayici _metinler;

    /// <param name="metinler">
    /// Hem hangi dilin rehberinin okunacagini hem de HTML'deki sabit
    /// basliklari belirler.
    /// </param>
    public RehberServisi(IMetinSaglayici? metinler = null)
        => _metinler = metinler ?? new MetinSaglayici();

    public BiosRehberi RehberGetir(string anakartUretici)
    {
        var dosya = Yukle(_metinler.AktifDil);

        var eslesme = dosya.Markalar.FirstOrDefault(
            m => m.Marka.Equals(anakartUretici, StringComparison.OrdinalIgnoreCase));

        return Cevir(eslesme ?? dosya.Genel);
    }

    public string HtmlUret(BiosRehberi rehber, DonanimRaporu donanim)
    {
        var s = new StringBuilder();

        s.AppendLine("<!DOCTYPE html>");
        s.AppendLine($"<html lang=\"{_metinler.AktifDil}\"><head><meta charset=\"utf-8\">");
        s.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        s.AppendLine($"<title>{Kacis(_metinler.Al("rehber.html.sayfa.basligi"))}</title>");
        s.AppendLine("<style>");
        s.AppendLine("  :root { color-scheme: light dark; }");
        s.AppendLine("  body { font-family: system-ui, -apple-system, 'Segoe UI', sans-serif;");
        s.AppendLine("         max-width: 42rem; margin: 0 auto; padding: 2rem 1.25rem; line-height: 1.6; }");
        s.AppendLine("  h1 { font-size: 1.5rem; margin-bottom: .25rem; }");
        s.AppendLine("  .makine { opacity: .7; font-size: .9rem; margin-bottom: 2rem; }");
        s.AppendLine("  .tus { display: inline-block; padding: .15em .5em; border: 1px solid currentColor;");
        s.AppendLine("         border-radius: .3em; font-family: ui-monospace, monospace; font-weight: 600; }");
        s.AppendLine("  ol { padding-left: 1.25rem; }");
        s.AppendLine("  li { margin-bottom: 1.25rem; }");
        s.AppendLine("  li b { display: block; margin-bottom: .25rem; }");
        s.AppendLine("  .not { background: rgba(127,127,127,.12); padding: 1rem;");
        s.AppendLine("         border-radius: .5rem; margin: 2rem 0; }");
        s.AppendLine("  footer { margin-top: 3rem; padding-top: 1rem;");
        s.AppendLine("           border-top: 1px solid rgba(127,127,127,.3); opacity: .7; font-size: .85rem; }");
        s.AppendLine("</style></head><body>");

        s.AppendLine($"<h1>{Kacis(_metinler.Al("rehber.html.baslik"))}</h1>");

        s.AppendLine($"<p class=\"makine\">{Kacis(_metinler.Al(
            "rehber.html.makine", donanim.AnakartUretici, donanim.AnakartModel))}</p>");

        s.AppendLine($"<p>{Kacis(_metinler.Al("rehber.html.giris"))} "
                   + $"<span class=\"tus\">{Kacis(rehber.GirisTusu)}</span></p>");

        if (rehber.BootMenuTusu is not null)
        {
            s.AppendLine($"<p>{Kacis(_metinler.Al("rehber.html.boot"))} "
                       + $"<span class=\"tus\">{Kacis(rehber.BootMenuTusu)}</span></p>");
        }

        s.AppendLine("<ol>");
        foreach (var adim in rehber.Adimlar.OrderBy(a => a.Sira))
            s.AppendLine($"<li><b>{Kacis(adim.Baslik)}</b>{Kacis(adim.Aciklama)}</li>");
        s.AppendLine("</ol>");

        if (rehber.SecureBootNotu is not null)
        {
            s.AppendLine($"<div class=\"not\"><b>{Kacis(_metinler.Al("rehber.html.secureboot"))}</b>"
                       + $"<br>{Kacis(rehber.SecureBootNotu)}</div>");
        }

        s.AppendLine($"<div class=\"not\"><b>{Kacis(_metinler.Al("rehber.html.kurulum.baslik"))}</b><br>");
        s.AppendLine(Kacis(_metinler.Al("rehber.html.kurulum")));
        s.AppendLine("</div>");

        s.AppendLine("<footer>UWin · ugilabs · ugilabs.com</footer>");
        s.AppendLine("</body></html>");

        return s.ToString();
    }

    /// <summary>
    /// Yalnizca isaretlemeyi bozan karakterleri kacirir. WebUtility.HtmlEncode
    /// kullanilmaz: o, aksanli harfleri de sayisal varliga cevirir ve
    /// "Önyükleme" kaynakta "&#214;ny&#252;kleme" olarak durur. Sayfa zaten
    /// UTF-8 bildirdigi icin bu ne gerekli ne de okunabilir.
    /// </summary>
    private static string Kacis(string metin) => metin
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);

    private static BiosRehberi Cevir(RehberGirisi g) => new(
        Marka: g.Marka,
        GirisTusu: g.GirisTusu,
        BootMenuTusu: g.BootMenuTusu,
        Adimlar: g.Adimlar.Select(a => new RehberAdimi(a.Sira, a.Baslik, a.Aciklama)).ToList(),
        SecureBootNotu: g.SecureBootNotu);

    /// <summary>
    /// Dilin rehber dosyasini okur. Bulunamazsa Turkce'ye duser -
    /// yanlis dilde bir rehber, hic rehber olmamasindan iyidir.
    /// </summary>
    private static RehberDosyasi Yukle(string dil)
    {
        lock (Kilit)
        {
            if (Onbellek.TryGetValue(dil, out var onbellekli))
                return onbellekli;

            var derleme = Assembly.GetExecutingAssembly();
            var adlar = derleme.GetManifestResourceNames();

            var ad = adlar.FirstOrDefault(
                         n => n.EndsWith($"bios-rehberleri.{dil}.json", StringComparison.Ordinal))
                     ?? adlar.First(
                         n => n.EndsWith("bios-rehberleri.tr.json", StringComparison.Ordinal));

            using var akis = derleme.GetManifestResourceStream(ad)!;

            var dosya = JsonSerializer.Deserialize<RehberDosyasi>(akis, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;

            Onbellek[dil] = dosya;
            return dosya;
        }
    }

    private sealed record RehberDosyasi(RehberGirisi Genel, List<RehberGirisi> Markalar);

    private sealed record RehberGirisi(
        string Marka,
        string GirisTusu,
        string? BootMenuTusu,
        string? SecureBootNotu,
        List<AdimGirisi> Adimlar);

    private sealed record AdimGirisi(int Sira, string Baslik, string Aciklama);
}
